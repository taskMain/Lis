/**
 * 报告管理端两个页面共用的列表主体（提交式筛选、状态页签、默认时间窗与来源方三行式）。
 *
 * 取值来源与范围固定由调用方注入：平台管理员页把组织、医院、院区作为请求条件提交，
 * 医院管理员页只提交院区、组织与医院由服务端从可信上下文注入。主体自身不读取可信上下文、
 * 不构造请求载荷，也不接触生成的 Kiota 类型。详情呈现由详情路由页承载，主体只负责：
 * 「查看详情」跳转详情子路由（行概要经会话级缓存传递，键为 reportId）与来源行高亮标记，
 * 以及返回后的滚动位置还原与来源行焦点还原（列表保持挂载由入口容器执行）。
 *
 * 本文件承载的服务端分页口径：
 * 1. 分页由服务端完成，页面不排序、不做本地过滤、不做本地分页；
 * 2. 提交式筛选（D-02）：筛选控件改动只进草稿、零请求，点「查询」把草稿提交为已生效条件并回到第 1 页；
 *    不设「重置」；关键字与报告类型可清空、清空后经「查询」生效，报告时间区间不可清空（默认窗口见第 5 条）；
 *    列表上沿报告状态页签不进草稿，改动即提交；翻页基于已生效条件自动读取；
 * 3. 当页为空且总数为正时按服务端总数回退到最后一页一次，避免越界页长期空转；
 * 4. 同一筛选条件下的连续操作只采纳最后一次请求结果，过期响应直接丢弃；
 * 5. 默认条件（2026-10-15 修订）：初始与范围切换复位的已生效条件与草稿都携带默认时间窗（近 30 天，与「近30天」预设同义），
 *    因此每次读取恒带时间边界；范围解析由调用方完成，主体只在范围齐备后读取。
 *
 * 失败面（列表读取失败）只做本层恢复（既定口径：纯读取失败不渲染横幅）：
 * 读取失败静默保留已有数据与旧总数、结束加载态；错误由宿主统一提示，重试经查询按钮。
 * 不解析错误响应体、不新增请求拦截器。
 */
import { SearchOutlined } from '@ant-design/icons'
import {
  Button,
  DatePicker,
  Empty,
  Form,
  Input,
  Select,
  Table,
  Tabs,
  Tag,
  Typography,
  type TableColumnsType,
} from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useEnumMetadata, type EnumMetadataOption } from '../../hooks/useEnumMetadata'
import { createTablePagination } from '../../shared/tablePagination'
import {
  isReportStatusValue,
  MAX_PAGE_SIZE,
  toDisplayDate,
  VOIDED_REPORT_STATUS,
  type ReportFilters,
  type ReportPage,
  type ReportRow,
} from './reportsApi'
import { setRowSummary } from './reportRowSummaryCache'
import { scrollContainerOf } from './reportDetailShared'
import { REPORT_DETAIL_SEGMENT } from './reportManagementRoutes'
import { IdNumberText } from './IdNumberText'
import './ReportManagement.css'

/** 报告类型筛选的本地兜底选项；服务端枚举元数据可用时改取服务端声明。 */
const REPORT_TYPE_METADATA_FALLBACK: readonly EnumMetadataOption[] = [
  { value: 1, name: 'Laboratory', label: '检验报告' },
  { value: 2, name: 'Examination', label: '检查报告' },
]

/** 报告状态页签的本地兜底选项；服务端枚举元数据可用时改取服务端声明。 */
const REPORT_STATUS_METADATA_FALLBACK: readonly EnumMetadataOption[] = [
  { value: 1, name: 'Effective', label: '有效' },
  { value: 2, name: 'Voided', label: '已作废' },
]

/** 状态页签「全部」的键；不携带 reportStatus 字段。 */
const ALL_STATUS_KEY = 'all'

/**
 * 默认报告时间窗（2026-10-15 修订）：近 30 天（今天往前 29 天到今天的滚动窗），与「近30天」预设同义；
 * 报告时间区间不可清空，进入页面与范围切换复位的初始条件都携带该窗口。
 */
function buildDefaultReportDateWindow(now: Dayjs): { reportDateFrom: string; reportDateTo: string } {
  return {
    reportDateFrom: now.subtract(29, 'day').format('YYYY-MM-DD'),
    reportDateTo: now.format('YYYY-MM-DD'),
  }
}

/** 默认筛选条件：报告时间取默认窗口；其余条件未施加。 */
function buildDefaultFilters(now: Dayjs = dayjs()): ReportFilters {
  return buildDefaultReportDateWindow(now)
}

/** 报告时间快捷预设：今天、近 7 天与近 30 天；「近30天」与默认窗口同一口径（含当天共 30 天）。 */
function buildReportDatePresets(today: Dayjs): { label: string; value: [Dayjs, Dayjs] }[] {
  return [
    { label: '今天', value: [today, today] },
    { label: '近7天', value: [today.subtract(6, 'day'), today] },
    { label: '近30天', value: [today.subtract(29, 'day'), today] },
  ]
}

/**
 * 归属名称的单元格文本（D-04）：只显名称、绝不回退编码；名称缺失显示「未知」。
 */
function partyName(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '未知'
}

/**
 * 来源方三行式单元格（D-04）：组织名、医院名、灰色小字的院区名各占一行，只显名称不显示编码。
 * 名称直接取读模型交付字段，不做前端反查。
 */
function PartyCell(props: { organizationName?: string | null; hospitalName?: string | null; branchName?: string | null }) {
  return <div>
    <div>{partyName(props.organizationName)}</div>
    <div>{partyName(props.hospitalName)}</div>
    <Typography.Text type='secondary' style={{ fontSize: 12 }}>{partyName(props.branchName)}</Typography.Text>
  </div>
}

/**
 * 列表读取失败态（既定口径）：不渲染横幅，状态仅用于挂起「空页自愈」（失败时旧数据与旧总数
 * 不是服务端事实，自愈不得基于它们触发）。
 */
type ListFailure = null | 'load'

/** 页面主体的查询范围；`key` 是范围的稳定标识，变化即视为范围切换。 */
export interface ReportManagementScope<Query> {
  key: string
  query: Query
}

export interface ReportManagementBoardProps<Query> {
  title: string
  subtitle: ReactNode
  /** 页头右侧插槽（可选）：医院管理员入口传当前登录范围条（S4-D46 页头左右结构），平台入口不传。 */
  headerExtra?: ReactNode
  /** 范围尚未选全时的空态文案（两个入口的范围层级不同，文案由调用方给出）。 */
  missingScopeText: string
  /** 范围选择器节点；由调用方以受控值接入，主体只负责渲染。 */
  selector: ReactNode
  /** `null` 表示范围不完整：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: ReportManagementScope<Query> | null
  /** 详情子路由是否激活（由入口容器按 URL 判定）：激活时列表被隐藏承载，返回非激活时主体还原滚动与来源行焦点。 */
  detailActive: boolean
  /** 按筛选条件与分页参数读取当页；请求体由调用方经适配层构造。 */
  queryPage: (query: Query, filters: ReportFilters, page: { pageIndex: number; pageSize: number }) => Promise<ReportPage>
}

/** 表格行 = 列表行模型 + 仅在页面内使用的稳定行标识（不作为业务字段提交或展示）。 */
interface TableRow extends ReportRow {
  rowIdentity: string
}

export function ReportManagementBoard<Query>({
  title,
  subtitle,
  headerExtra,
  missingScopeText,
  selector,
  scope,
  detailActive,
  queryPage,
}: ReportManagementBoardProps<Query>) {
  const navigate = useNavigate()
  const scopeKey = scope?.key ?? null
  /** 本次挂载的默认筛选条件；预设按同一窗口的结束日派生，因此默认状态与点一次「近30天」预设等价。 */
  const [defaultFilters] = useState<ReportFilters>(() => buildDefaultFilters())
  const datePresets = useMemo(() => buildReportDatePresets(dayjs(defaultFilters.reportDateTo)), [defaultFilters])
  const [pageState, setPageState] = useState<{
    key: string | null
    filters: ReportFilters
    pageIndex: number
    pageSize: number
  }>({ key: scopeKey, filters: defaultFilters, pageIndex: 1, pageSize: 10 })
  /**
   * 提交式筛选（D-02）：草稿承载未提交的控件改动（零请求），已生效条件在 `pageState.filters` 里
   * 驱动读取。两份状态分离是提交式的核心：控件 onChange 只写草稿，「查询」才把草稿提交为已生效条件。
   */
  const [draftFilters, setDraftFilters] = useState<ReportFilters>(defaultFilters)
  const [rows, setRows] = useState<ReportRow[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [listFailure, setListFailure] = useState<ListFailure>(null)
  /**
   * 来源行标识（D-03）：刚从该行进入详情。返回列表后该行渲染高亮样式并作为焦点还原目标；
   * 范围切换复位时清除，重新进入任一详情时更新为新的来源行。
   */
  const [highlightReportId, setHighlightReportId] = useState<string | null>(null)

  /**
   * 范围切换在渲染期复位：等到 effect 再复位，旧范围的行、筛选与页码会先渲染一帧。
   * 复位后页码状态的 `key` 恒等于当前范围键，晚到的旧范围响应既不改写新范围的数据，也不改写失败态。
   */
  if (pageState.key !== scopeKey) {
    // 范围切换复位为新的默认条件：默认时间窗按复位时刻计算（跨天后切换范围得到新的窗口）。
    const defaultFiltersAtReset = buildDefaultFilters()
    setPageState({ key: scopeKey, filters: defaultFiltersAtReset, pageIndex: 1, pageSize: pageState.pageSize })
    setDraftFilters(defaultFiltersAtReset)
    setRows([])
    setTotalCount(0)
    setListFailure(null)
    // 范围切换复位时清除来源行标识（D-03）：旧范围的高亮不带入新范围。
    setHighlightReportId(null)
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
  /** 越界回退只做一次：记录已经回退过的范围键与页码，避免空页反复触发回退。 */
  const fallbackGuardRef = useRef<string | null>(null)
  /** 主体根节点：滚动容器定位与来源行按钮定位都从这里出发。 */
  const pageRootRef = useRef<HTMLDivElement>(null)
  /** 跳转详情前记录的滚动容器位置；深链直开没有该记录，返回路径据此跳过还原。 */
  const savedScrollRef = useRef<number | null>(null)
  /** 上一渲染的详情激活态：由激活转为非激活即「从详情返回」。 */
  const wasDetailActiveRef = useRef(false)

  const reportTypeOptions = useEnumMetadata('MedicalReportType', REPORT_TYPE_METADATA_FALLBACK, { enabled: scope !== null })
  const reportTypeSelectOptions = useMemo(
    () => reportTypeOptions.map((option) => ({ value: option.value, label: option.label })),
    [reportTypeOptions],
  )

  const reportStatusOptions = useEnumMetadata('MedicalReportLifecycleStatus', REPORT_STATUS_METADATA_FALLBACK, { enabled: scope !== null })
  const statusTabItems = useMemo(
    () => [{ key: ALL_STATUS_KEY, label: '全部' }, ...reportStatusOptions.map((option) => ({ key: String(option.value), label: option.label }))],
    [reportStatusOptions],
  )
  const activeStatusKey = filters.reportStatus === undefined ? ALL_STATUS_KEY : String(filters.reportStatus)

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

  /**
   * 进入详情（D-01/D-03）：记录来源行概要与来源行标识并跳转详情子路由。行概要经会话级缓存
   * 传递（宿主 MicroSDK 接管 history，`navigate` 的 state 会被 SDK 包装的 push 丢弃），
   * 供详情页概要头部即时呈现；列表保持挂载，可见性由入口容器切换。
   */
  const showDetail = (row: ReportRow) => {
    // display:none 会重置滚动容器位置：跳转前记录 MainLayout Content（overflow:auto）的 scrollTop。
    const scrollContainer = scrollContainerOf(pageRootRef.current)
    savedScrollRef.current = scrollContainer?.scrollTop ?? null
    setRowSummary(row.reportId, row)
    setHighlightReportId(row.reportId)
    navigate(`${REPORT_DETAIL_SEGMENT}/${row.reportId}`)
  }

  /**
   * 返回列表后的还原（D-03）：详情激活转为非激活时，先恢复跳转前记录的 scrollTop
   * （display:none 期间浏览器已重置滚动位置），再聚焦来源行「查看详情」按钮；
   * `preventScroll` 避免焦点动作再次改变已恢复的滚动位置。深链直开没有记录值，返回路径跳过还原。
   */
  useEffect(() => {
    const returning = wasDetailActiveRef.current && !detailActive
    wasDetailActiveRef.current = detailActive
    if (!returning) return
    const saved = savedScrollRef.current
    savedScrollRef.current = null
    if (saved === null) return
    const scrollContainer = scrollContainerOf(pageRootRef.current)
    if (scrollContainer !== null) scrollContainer.scrollTop = saved
    if (highlightReportId === null) return
    const sourceButton = pageRootRef.current?.querySelector<HTMLButtonElement>(`button[data-report-id="${CSS.escape(highlightReportId)}"]`) ?? null
    sourceButton?.focus({ preventScroll: true })
  }, [detailActive, highlightReportId])

  /** 草稿条件变化：只改草稿，不发请求、不动分页（D-02）。 */
  const applyDraft = (patch: ReportFilters) => {
    setDraftFilters((previous) => ({ ...previous, ...patch }))
  }

  /** 查询：把草稿提交为已生效条件并回到第 1 页；条件为空的提交等价于刷新。 */
  const submitDraft = () => {
    setPageState((previous) => ({ ...previous, filters: { ...draftFilters }, pageIndex: 1 }))
  }

  /**
   * 报告状态页签改动即提交（列表上沿页签口径）：不进草稿，直接写已生效条件并回到第 1 页；
   * 「全部」不下发状态字段。其余草稿控件不随页签提交，保持各自的草稿值。
   */
  const changeReportStatus = (key: string) => {
    if (key === ALL_STATUS_KEY) {
      setPageState((previous) => ({ ...previous, filters: { ...previous.filters, reportStatus: undefined }, pageIndex: 1 }))
      return
    }
    const option = reportStatusOptions.find((item) => String(item.value) === key)
    if (!option || !isReportStatusValue(option.value)) return
    setPageState((previous) => ({ ...previous, filters: { ...previous.filters, reportStatus: option.value }, pageIndex: 1 }))
  }

  /** 草稿时间区间的受控值：来自草稿的年月日文本；两端都为空时为未选择。 */
  const draftRangeValue: [Dayjs | null, Dayjs | null] | null = (draftFilters.reportDateFrom ?? '') !== '' || (draftFilters.reportDateTo ?? '') !== ''
    ? [
        draftFilters.reportDateFrom ? dayjs(draftFilters.reportDateFrom) : null,
        draftFilters.reportDateTo ? dayjs(draftFilters.reportDateTo) : null,
      ]
    : null

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
    // 来源方三行式（D-04）：组织、医院、院区三列合并，名称取读模型交付字段。
    { title: '来源方', width: 180, ellipsis: true, render: (_: unknown, row) => <PartyCell {...row} /> },
    { title: '患者姓名', dataIndex: 'patientName', width: 100 },
    {
      title: '证件号码',
      dataIndex: 'identityDocumentNo',
      width: 170,
      render: (value: string | null) => <IdNumberText value={value} />,
    },
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
        // 来源行按钮的定位锚点：返回列表后按该标识还原焦点（preventScroll）。
        data-report-id={row.reportId}
        onClick={() => showDetail(row)}
      >查看详情</Button>,
    },
  ]

  return <div className='report-management-page' ref={pageRootRef}>
    <div className='report-management-header'>
      <div>
        <Typography.Title level={3}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      {headerExtra}
    </div>

    {selector}

    {/* 查询区（提交式）：inline Form 承载草稿控件；控件改动只进草稿零请求，「查询」把草稿提交为已生效条件并回到第 1 页。
        不设「重置」：各控件可清空，清空后经「查询」生效。报告状态不进草稿，页签改动即提交。 */}
    <div className='report-management-filters'>
      <Form layout='inline' onFinish={submitDraft}>
        <Form.Item>
          {/*
            统一关键字：报告单号、患者证件号码与患者姓名合并为单一条件，服务端按单号精确、证件前缀
            （大小写不敏感）与姓名前缀三列任一命中匹配；输入按用户原文提交，规范化由服务端完成。
          */}
          <Input
            placeholder='报告单号 / 证件号码 / 患者姓名'
            aria-label='关键字筛选'
            autoComplete='off'
            allowClear
            style={{ width: 320 }}
            value={draftFilters.keyword ?? ''}
            onChange={(event) => applyDraft({ keyword: event.target.value })}
          />
        </Form.Item>
        <Form.Item>
          <Select<number>
            allowClear
            placeholder='报告类型'
            aria-label='报告类型筛选'
            style={{ width: 140 }}
            value={draftFilters.reportType}
            options={reportTypeSelectOptions}
            onChange={(value) => applyDraft({ reportType: value })}
          />
        </Form.Item>
        <Form.Item>
          {/*
            报告时间筛选使用 antd RangePicker：受控值来自草稿的年月日文本，只提交起止日期，
            不拼接时分秒、也不把本地时区偏移写进请求值；区间边界由服务端按左闭右开处理。
            区间不可清空（默认近 30 天窗口，与「近30天」预设同义）：报告列表查询恒带时间边界；
            预设提供今天、近 7 天与近 30 天三个快捷区间。
          */}
          <DatePicker.RangePicker
            allowClear={false}
            aria-label='报告时间范围'
            placeholder={['开始日期', '结束日期']}
            presets={datePresets}
            value={draftRangeValue}
            onChange={(_dates, dateStrings) => applyDraft({
              reportDateFrom: dateStrings[0] || undefined,
              reportDateTo: dateStrings[1] || undefined,
            })}
          />
        </Form.Item>
        <Form.Item>
          {/*
            查询按钮：`aria-label` 声明可访问名称——`SearchOutlined` 自带英文 `aria-label`，会参与按钮名称计算，
            不声明就会读成「search 查询」；`autoInsertSpace={false}` 关闭 antd 对两个汉字之间自动插入的空格；
            颜色继承宿主主题 token，不覆盖样式。提交触发重新读取，读取在途时按钮进入 `loading`
            （在途期间 antd loading 阻止点击提交、不会重复发起读取）；交叠请求仍由序号作废机制收敛为「只采纳最后一次」。
          */}
          <Button
            type='primary'
            icon={<SearchOutlined />}
            aria-label='查询'
            autoInsertSpace={false}
            htmlType='submit'
            loading={loading}
          >查询</Button>
        </Form.Item>
      </Form>
    </div>

    {/* 报告状态页签（列表上沿，与互认项目页配置状态页签同款交互）：改动即提交为已生效条件并回到第 1 页。 */}
    <div className='report-management-status-toolbar'>
      <div role='group' aria-label='报告状态筛选'>
        <Tabs
          className='report-management-status-tabs'
          activeKey={activeStatusKey}
          items={statusTabItems}
          onChange={changeReportStatus}
        />
      </div>
    </div>

    {/* 读取失败不渲染错误横幅（既定口径）：失败静默保留旧数据，错误由宿主统一提示，重试经查询按钮。 */}
    <Table
      rowKey='rowIdentity'
      size='small'
      columns={columns}
      dataSource={tableRows}
      loading={loading && rows.length === 0}
      // 来源行高亮（D-03）：rowClassName 仅命中当前页数据中的来源行，样式由配套 CSS 承载。
      rowClassName={(row) => (row.reportId === highlightReportId ? 'report-management-detail-row-source' : '')}
      // 来源方三列并一列后横向宽度收窄（D-04）：各列宽度合计 1140。
      scroll={{ x: 1140 }}
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
  </div>
}
