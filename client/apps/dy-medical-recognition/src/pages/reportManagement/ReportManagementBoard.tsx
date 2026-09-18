/**
 * 报告管理端两个页面共用的列表与详情主体（阶段 4 票 14、15）。
 *
 * 取值来源与范围固定由调用方注入：平台管理员页把组织、医院、院区作为请求条件提交，
 * 医院管理员页只提交院区、组织与医院由服务端从可信上下文注入。主体自身不读取可信上下文、
 * 不构造请求载荷，也不接触生成的 Kiota 类型。
 *
 * 本文件承载的服务端分页口径：
 * 1. 分页由服务端完成，页面不排序、不做本地过滤、不做本地分页；
 * 2. 筛选条件变化回到第 1 页并保留当前页容量；
 * 3. 当页为空且总数为正时按服务端总数回退到最后一页一次，避免越界页长期空转；
 * 4. 同一筛选条件下的连续操作只采纳最后一次请求结果，过期响应直接丢弃。
 *
 * 失败面四类（列表读取、版本列表读取、版本详情读取、PDF 下载）都只做本层恢复：
 * 保留已有数据与当前版本选择、结束加载态、提供重试；不显示本地接口错误提示、不解析错误响应体、
 * 不新增请求拦截器，错误提示由宿主统一展示。
 */
import { ReloadOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Empty,
  Input,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  type TableColumnsType,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useEnumMetadata, type EnumMetadataOption } from '../../hooks/useEnumMetadata'
import { createTablePagination } from '../../shared/tablePagination'
import {
  MAX_PAGE_SIZE,
  toDisplayDate,
  VOIDED_REPORT_STATUS,
  type ReportFilters,
  type ReportPage,
  type ReportRow,
  type ReportVersionDetail,
  type ReportVersionRow,
} from './reportsApi'
import { ReportVersionDetailPanel } from './ReportVersionDetailPanel'
import './ReportManagement.css'

/** 报告类型筛选的本地兜底选项；服务端枚举元数据可用时改取服务端声明。 */
const REPORT_TYPE_METADATA_FALLBACK: readonly EnumMetadataOption[] = [
  { value: 1, name: 'Laboratory', label: '检验报告' },
  { value: 2, name: 'Examination', label: '检查报告' },
]

/** 空筛选条件。 */
const EMPTY_FILTERS: ReportFilters = {}

/** 列表读取失败态。 */
type ListFailure = null | 'load'

/** 版本列表读取失败态。 */
type VersionFailure = null | 'load'

/** 版本详情读取失败态。 */
type DetailFailure = null | 'load'

/** 页面主体的查询范围；`key` 是范围的稳定标识，变化即视为范围切换。 */
export interface ReportManagementScope<Query> {
  key: string
  query: Query
}

export interface ReportManagementBoardProps<Query> {
  title: string
  subtitle: ReactNode
  /** 范围尚未选全时的空态文案（两个入口的范围层级不同，文案由调用方给出）。 */
  missingScopeText: string
  /** 范围选择器节点；由调用方以受控值接入，主体只负责渲染。 */
  selector: ReactNode
  /** `null` 表示范围不完整：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: ReportManagementScope<Query> | null
  /** 按筛选条件与分页参数读取当页；请求体由调用方经适配层构造。 */
  queryPage: (query: Query, filters: ReportFilters, page: { pageIndex: number; pageSize: number }) => Promise<ReportPage>
  /** 读取某个报告的全部版本。 */
  queryVersions: (reportId: string) => Promise<ReportVersionRow[]>
  /** 读取某个报告版本的完整内容。 */
  queryDetail: (reportId: string, reportVersionId: string) => Promise<ReportVersionDetail>
  /** 下载某个报告版本的原始 PDF；下载名取该版本保存的下载名。 */
  downloadVersion: (reportId: string, version: ReportVersionRow) => Promise<void>
}

/** 表格行 = 列表行模型 + 仅在页面内使用的稳定行标识（不作为业务字段提交或展示）。 */
interface TableRow extends ReportRow {
  rowIdentity: string
}

/** 已展开报告的会话状态：报告身份与版本列表。 */
interface DetailState {
  reportId: string
  reportNo: string
  reportTypeText: string
  reportStatusText: string
  versions: ReportVersionRow[]
}

export function ReportManagementBoard<Query>({
  title,
  subtitle,
  missingScopeText,
  selector,
  scope,
  queryPage,
  queryVersions,
  queryDetail,
  downloadVersion,
}: ReportManagementBoardProps<Query>) {
  const scopeKey = scope?.key ?? null
  const [pageState, setPageState] = useState<{
    key: string | null
    filters: ReportFilters
    pageIndex: number
    pageSize: number
  }>({ key: scopeKey, filters: EMPTY_FILTERS, pageIndex: 1, pageSize: 10 })
  const [rows, setRows] = useState<ReportRow[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [listFailure, setListFailure] = useState<ListFailure>(null)
  const [detail, setDetail] = useState<DetailState | null>(null)
  const [versionsLoading, setVersionsLoading] = useState(false)
  const [versionFailure, setVersionFailure] = useState<VersionFailure>(null)
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null)
  const [versionDetail, setVersionDetail] = useState<ReportVersionDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detailFailure, setDetailFailure] = useState<DetailFailure>(null)
  const [downloadingVersionId, setDownloadingVersionId] = useState<string | null>(null)
  const [downloadFailed, setDownloadFailed] = useState(false)

  /**
   * 范围切换在渲染期复位：等到 effect 再复位，旧范围的行、筛选、页码与详情会先渲染一帧。
   * 复位后页码状态的 `key` 恒等于当前范围键，晚到的旧范围响应既不改写新范围的数据，也不改写失败态。
   */
  if (pageState.key !== scopeKey) {
    setPageState({ key: scopeKey, filters: EMPTY_FILTERS, pageIndex: 1, pageSize: pageState.pageSize })
    setRows([])
    setTotalCount(0)
    setListFailure(null)
    setDetail(null)
    setSelectedVersionId(null)
    setVersionDetail(null)
    setVersionFailure(null)
    setDetailFailure(null)
    setDownloadFailed(false)
  }

  const filters = pageState.filters
  const pageIndex = pageState.pageIndex
  const pageSize = pageState.pageSize

  /**
   * 在途读取的作废机制：按序号决定谁写入状态，不取消在途请求。
   * 生成客户端与鉴权适配层都没有取消入口，页面不得为此自建底层 HTTP 调用或新增拦截器；
   * 旧请求仍会在网络上跑完，但它的结果因序号不符而被丢弃。
   */
  const listRequestIdRef = useRef(0)
  const versionRequestIdRef = useRef(0)
  const detailRequestIdRef = useRef(0)
  /** 越界回退只做一次：记录已经回退过的范围键与页码，避免空页反复触发回退。 */
  const fallbackGuardRef = useRef<string | null>(null)

  const reportTypeOptions = useEnumMetadata('MedicalReportType', REPORT_TYPE_METADATA_FALLBACK, { enabled: scope !== null })
  const reportTypeSelectOptions = useMemo(
    () => reportTypeOptions.map((option) => ({ value: option.value, label: option.label })),
    [reportTypeOptions],
  )

  const load = useCallback(async (
    query: Query,
    targetFilters: ReportFilters,
    targetPage: { pageIndex: number; pageSize: number },
  ) => {
    const requestId = ++listRequestIdRef.current
    setLoading(true)
    try {
      const page = await queryPage(query, targetFilters, targetPage)
      if (requestId !== listRequestIdRef.current) return
      setRows(page.items)
      setTotalCount(page.totalCount)
      setListFailure(null)
    } catch {
      if (requestId !== listRequestIdRef.current) return
      // 读取失败保留已有数据，只进入待刷新状态；不把失败结果覆盖成空列表。
      setListFailure('load')
    } finally {
      if (requestId === listRequestIdRef.current) setLoading(false)
    }
  }, [queryPage])

  /** 按当前筛选、页码与页容量读取当页。 */
  const reload = useCallback(() => {
    if (scope === null) return
    void load(scope.query, filters, { pageIndex, pageSize })
  }, [filters, load, pageIndex, pageSize, scope])

  useEffect(() => {
    if (scope === null) return
    // 推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(() => {
      void load(scope.query, filters, { pageIndex, pageSize })
    }, 0)
    return () => window.clearTimeout(task)
  }, [filters, load, pageIndex, pageSize, scope])

  /**
   * 页码越界自愈：当页为空且总数为正时按服务端交付的总数回退到最后一页一次。
   * 数据收缩后停留在空页没有意义；当页已等于按总数算出的最后一页时同样重取一次，
   * 因为空页与正总数并存说明当页快照已过期，重取是恢复的唯一手段；回退由 guard 限制为一次，避免反复重取。
   */
  useEffect(() => {
    if (loading || listFailure !== null) return
    if (rows.length > 0 || totalCount <= 0) return
    const lastPage = Math.max(1, Math.ceil(totalCount / pageSize))
    const guard = `${scopeKey ?? ''}|${pageIndex}|${pageSize}|${totalCount}`
    if (fallbackGuardRef.current === guard) return
    fallbackGuardRef.current = guard
    // 越界或空页都推迟到下一个宏任务处理：既避免在 effect 内同步级联渲染，也让同一路径统一表达"只做一次"。
    // 当页已等于按总数算出的最后一页时，空页与正总数并存说明当页快照已过期，按同一页码重取一次。
    const task = window.setTimeout(() => {
      if (lastPage !== pageIndex) {
        setPageState((previous) => (previous.pageIndex === pageIndex ? { ...previous, pageIndex: lastPage } : previous))
        return
      }
      reload()
    }, 0)
    return () => window.clearTimeout(task)
  }, [listFailure, loading, pageIndex, pageSize, reload, rows.length, scopeKey, totalCount])

  /** 读取某个版本的内容；失败保留当前版本选择，只进入待刷新状态。 */
  const loadVersionDetail = useCallback(async (reportId: string, reportVersionId: string) => {
    const requestId = ++detailRequestIdRef.current
    setSelectedVersionId(reportVersionId)
    setDetailLoading(true)
    setDetailFailure(null)
    setDownloadFailed(false)
    try {
      const value = await queryDetail(reportId, reportVersionId)
      if (requestId !== detailRequestIdRef.current) return
      setVersionDetail(value)
      setDetailLoading(false)
    } catch {
      if (requestId !== detailRequestIdRef.current) return
      setDetailLoading(false)
      // 详情读取失败保留当前版本选择与上一个版本的已有内容，只标记待刷新。
      setDetailFailure('load')
    }
  }, [queryDetail])

  /**
   * 载入被展开报告的版本列表，并默认选中平台形成顺序的最新版本。
   *
   * 版本列表与详情分两次读取：版本列表失败时详情区不可用但列表仍可见，可单独重试；
   * 两次读取的响应都按序号作废，关闭详情或展开另一份报告后迟到的响应不会写回状态。
   */
  const loadDetail = useCallback(async (reportId: string, reportNo: string, reportTypeText: string, reportStatusText: string) => {
    const requestId = ++versionRequestIdRef.current
    detailRequestIdRef.current += 1
    setDetail({ reportId, reportNo, reportTypeText, reportStatusText, versions: [] })
    setSelectedVersionId(null)
    setVersionDetail(null)
    setDetailFailure(null)
    setDownloadFailed(false)
    setVersionsLoading(true)
    setVersionFailure(null)
    try {
      const versions = await queryVersions(reportId)
      if (requestId !== versionRequestIdRef.current) return
      setDetail({ reportId, reportNo, reportTypeText, reportStatusText, versions })
      setVersionsLoading(false)
      if (versions.length === 0) return
      void loadVersionDetail(reportId, versions[versions.length - 1].reportVersionId)
    } catch {
      if (requestId !== versionRequestIdRef.current) return
      setVersionsLoading(false)
      setVersionFailure('load')
    }
  }, [loadVersionDetail, queryVersions])

  /** 关闭详情：作废在途的版本与详情响应，避免关闭后迟到的响应重新写回已清空的详情。 */
  const closeDetail = () => {
    versionRequestIdRef.current += 1
    detailRequestIdRef.current += 1
    setDetail(null)
    setSelectedVersionId(null)
    setVersionDetail(null)
    setVersionFailure(null)
    setDetailFailure(null)
    setDownloadFailed(false)
  }

  /**
   * 下载指定版本的 PDF：下载名取该版本保存的下载名。
   * 下载失败保留当前详情与版本选择、不重载列表、不把失败路径当成功。
   */
  const download = async (row: ReportVersionRow) => {
    if (detail === null) return
    if (downloadingVersionId !== null) return
    setDownloadingVersionId(row.reportVersionId)
    setDownloadFailed(false)
    try {
      await downloadVersion(detail.reportId, row)
    } catch {
      setDownloadFailed(true)
    } finally {
      setDownloadingVersionId(null)
    }
  }

  /** 筛选条件变化：回到第 1 页并保留当前页容量。 */
  const applyFilters = (patch: ReportFilters) => {
    setPageState((previous) => ({ ...previous, filters: { ...previous.filters, ...patch }, pageIndex: 1 }))
  }

  const tableRows = useMemo<TableRow[]>(() => {
    const occurrences = new Map<string, number>()
    return rows.map((row) => {
      const occurrence = occurrences.get(row.reportId) ?? 0
      occurrences.set(row.reportId, occurrence + 1)
      return { ...row, rowIdentity: occurrence === 0 ? row.reportId : `${row.reportId}#${occurrence}` }
    })
  }, [rows])

  const columns: TableColumnsType<TableRow> = [
    { title: '报告单号', dataIndex: 'reportNo', width: 160 },
    { title: '报告类型', dataIndex: 'reportTypeText', width: 100 },
    { title: '报告时间', width: 120, render: (_: unknown, row) => toDisplayDate(row.reportTime) || '—' },
    { title: '来源组织', dataIndex: 'organizationName', width: 140, ellipsis: true },
    { title: '医院', dataIndex: 'hospitalName', width: 140, ellipsis: true },
    { title: '院区', dataIndex: 'branchName', width: 120, ellipsis: true },
    { title: '患者姓名', dataIndex: 'patientName', width: 100 },
    { title: '证件号码', dataIndex: 'identityDocumentNo', width: 170 },
    { title: '当前版本序号', dataIndex: 'currentVersionSequence', width: 110 },
    {
      title: '报告状态',
      width: 100,
      render: (_: unknown, row) => <Tag color={row.status === VOIDED_REPORT_STATUS ? 'default' : 'green'}>{row.statusText}</Tag>,
    },
    {
      title: '操作',
      width: 100,
      fixed: 'right',
      render: (_: unknown, row) => <Button
        type='link'
        size='small'
        aria-label={`查看报告详情：${row.reportNo}`}
        onClick={() => {
          void loadDetail(row.reportId, row.reportNo, row.reportTypeText, row.statusText)
        }}
      >查看详情</Button>,
    },
  ]

  return <div className='report-management-page'>
    <div className='report-management-header'>
      <div>
        <Typography.Title level={2}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      <Space wrap>
        <Tooltip title='重新读取当前筛选条件下的报告列表'>
          <Button
            icon={<ReloadOutlined />}
            aria-label='重新读取报告列表'
            loading={loading}
            disabled={loading || scope === null}
            onClick={reload}
          />
        </Tooltip>
      </Space>
    </div>

    {selector}

    <div className='report-management-filters'>
      <Space wrap>
        {/*
          报告时间筛选使用浏览器原生日期输入：受控值是 `YYYY-MM-DD` 文本，只提交起止日期，
          因此不拼接时分秒、也不把本地时区偏移写进请求值；区间边界由服务端按左闭右开处理。
        */}
        <label className='report-management-date-label'>
          报告时间
          <input
            type='date'
            aria-label='报告时间起始日'
            className='report-management-date'
            value={filters.reportDateFrom ?? ''}
            onChange={(event) => applyFilters({ reportDateFrom: event.target.value || undefined })}
          />
        </label>
        <label className='report-management-date-label'>
          至
          <input
            type='date'
            aria-label='报告时间结束日'
            className='report-management-date'
            value={filters.reportDateTo ?? ''}
            onChange={(event) => applyFilters({ reportDateTo: event.target.value || undefined })}
          />
        </label>
        <Select<number>
          allowClear
          placeholder='报告类型'
          aria-label='报告类型筛选'
          style={{ width: 140 }}
          value={filters.reportType}
          options={reportTypeSelectOptions}
          onChange={(value) => applyFilters({ reportType: value ?? undefined })}
        />
        <Input
          placeholder='报告单号'
          aria-label='报告单号筛选'
          autoComplete='off'
          allowClear
          value={filters.reportNo ?? ''}
          onChange={(event) => applyFilters({ reportNo: event.target.value })}
        />
        <Input
          placeholder='患者证件号码'
          aria-label='患者证件号码筛选'
          autoComplete='off'
          allowClear
          value={filters.identityDocumentNo ?? ''}
          onChange={(event) => applyFilters({ identityDocumentNo: event.target.value })}
        />
        <Input
          placeholder='患者姓名'
          aria-label='患者姓名筛选'
          autoComplete='off'
          allowClear
          value={filters.patientName ?? ''}
          onChange={(event) => applyFilters({ patientName: event.target.value })}
        />
      </Space>
    </div>

    {listFailure === null ? null : <Alert
      type='warning'
      showIcon
      className='report-management-failure'
      title='报告列表数据待刷新'
      description='读取失败，已有数据保留；可重试读取。'
      action={<Button size='small' disabled={loading} onClick={reload}>重试</Button>}
    />}

    <Table
      rowKey='rowIdentity'
      size='small'
      columns={columns}
      dataSource={tableRows}
      loading={loading && rows.length === 0}
      scroll={{ x: 1400 }}
      // 受控分页：由共享分页配置提供页容量可选值、总数展示与「改变页容量回到第 1 页」的收敛规则。
      pagination={createTablePagination({
        pageIndex,
        pageSize,
        totalCount,
        loading,
        onChange: (nextPageIndex, nextPageSize) => {
          setPageState((previous) => (nextPageSize !== previous.pageSize
            ? { ...previous, pageIndex: 1, pageSize: nextPageSize }
            : { ...previous, pageIndex: nextPageIndex }))
        },
      })}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description={scope === null ? missingScopeText : '当前筛选条件没有报告'}
        />,
      }}
    />

    <Typography.Text type='secondary' className='report-management-summary'>
      当前筛选共 {totalCount} 份报告；第 {pageIndex} 页，每页 {pageSize} 条（服务端分页上限 {MAX_PAGE_SIZE} 条）
    </Typography.Text>

    {detail === null ? null : <ReportVersionDetailPanel
      reportNo={detail.reportNo}
      reportTypeText={detail.reportTypeText}
      reportStatusText={detail.reportStatusText}
      versions={detail.versions}
      versionsLoading={versionsLoading}
      versionFailure={versionFailure}
      selectedVersionId={selectedVersionId}
      detail={versionDetail}
      detailLoading={detailLoading}
      detailFailure={detailFailure}
      downloadingVersionId={downloadingVersionId}
      downloadFailed={downloadFailed}
      onSelectVersion={(reportVersionId) => {
        void loadVersionDetail(detail.reportId, reportVersionId)
      }}
      onRetryVersions={() => {
        void loadDetail(detail.reportId, detail.reportNo, detail.reportTypeText, detail.reportStatusText)
      }}
      onRetryDetail={() => {
        if (selectedVersionId === null) return
        void loadVersionDetail(detail.reportId, selectedVersionId)
      }}
      onDownload={download}
      onClose={closeDetail}
    />}
  </div>
}
