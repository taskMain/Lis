/**
 * 「互认项目金额维护」平台管理员页（宿主路径 `/subApps/medical-recognition/recognition-amounts`）。
 *
 * 页面本身只做范围接入与数据源接线：组织、医院、院区三级都取自范围选择器（受控保存），
 * 查询与保存请求都携带这三个值；列表与金额录入主体由本文件的 `RecognitionAmountsBoard`
 * 承载，与医院管理员页共用（见下）。页面不实现角色判断、不做权限二次确认，也不下推枚举筛选。
 */
import { ReloadOutlined } from '@ant-design/icons'
import { Alert, Button, Empty, Space, Table, Tag, Tooltip, Typography, type TableColumnsType } from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import {
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
  currentAmountText,
  queryRecognitionAmountRows,
  saveOrganizationHospitalBranchRecognitionAmount,
  type RecognitionAmountListQuery,
  type RecognitionAmountRow,
} from './recognitionAmountsApi'
import { AmountEntryModal, type RecognitionAmountWriteGate } from './RecognitionAmountModals'
import './RecognitionAmounts.css'

/**
 * 列表读取失败态。`refreshAfterWrite` 表示写入**已经成功**、随后的重载失败：
 * 呈现与阻断规则与读取失败一致，但不得改判为保存失败。
 */
export type AmountLoadFailure = null | 'load' | 'refreshAfterWrite'

type ModalState = { kind: 'none' } | { kind: 'amount'; standardProjectCode: string }

/** 页面主体当前范围；`key` 是范围的稳定标识，变化即视为范围切换。 */
export interface RecognitionAmountsScope<Query> {
  key: string
  query: Query
}

export interface RecognitionAmountsBoardProps<Query> {
  title: string
  subtitle: ReactNode
  /** 范围尚未选全时的空态文案（两个入口的范围层级不同，文案由调用方给出）。 */
  missingScopeText: string
  /** 范围选择器节点；由调用方以受控值接入，主体只负责渲染，不参与选择器状态。 */
  selector: ReactNode
  /** `null` 表示范围不完整：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: RecognitionAmountsScope<Query> | null
  queryRows: (query: Query) => Promise<RecognitionAmountRow[]>
  /** 保存当前行的金额；返回 `false` 表示**未发出写请求**（载荷不合法），页面据此保留弹窗与输入。 */
  saveRow: (query: Query, standardProjectCode: string, amountInput: string) => Promise<boolean>
}

/** 表格行 = 查询交付的行模型 + 仅在页面内使用的稳定行标识（不作为业务字段提交或展示）。 */
interface TableRow extends RecognitionAmountRow {
  rowIdentity: string
}

/** 范围相关状态整体按范围键存放：写入时带上请求时的范围键，渲染只取当前范围键的数据。 */
interface ScopeData {
  key: string | null
  rows: RecognitionAmountRow[]
  failure: AmountLoadFailure
}

/**
 * 两个金额页共用的列表主体：范围选择器、范围状态机、列表、金额录入弹窗。
 *
 * 范围与数据源由调用方注入（平台管理员页提交组织、医院、院区；医院管理员页只提交院区，
 * 组织与医院取可信上下文），主体自身不读取可信上下文、不构造请求载荷，也不理解 Kiota 契约。
 */
export function RecognitionAmountsBoard<Query>({
  title,
  subtitle,
  missingScopeText,
  selector,
  scope,
  queryRows,
  saveRow,
}: RecognitionAmountsBoardProps<Query>) {
  const scopeKey = scope?.key ?? null
  const [data, setData] = useState<ScopeData>({ key: scopeKey, rows: [], failure: null })
  const [loading, setLoading] = useState(false)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const [submitting, setSubmitting] = useState(false)

  /**
   * 范围切换在**渲染期**复位（React 的「props 变化时调整 state」写法）：等到 effect 再复位，
   * 旧范围的行与未保存输入会先渲染一帧。复位后 `data.key` 恒等于当前范围键，因此读取只能写回与
   * 请求同范围的数据，迟到的旧范围响应既不改写新范围的行，也不改写新范围的失败态。
   * 未保存的金额输入随弹窗卸载被丢弃，不带进新范围。
   */
  if (data.key !== scopeKey) {
    setData({ key: scopeKey, rows: [], failure: null })
    setModal({ kind: 'none' })
  }
  const rows = data.rows
  const loadFailure = data.failure

  /**
   * 在途读取的作废机制：**按序号决定谁结束 loading，不取消在途请求**。生成客户端与鉴权适配层都没有
   * 取消入口（`RequestConfiguration` 无 `AbortSignal`，适配层也没有暴露取消能力），页面不得为此自建
   * 底层 HTTP 调用或新增拦截器（Frontend API Client 第 2、5 节）。旧请求仍会在网络上跑完，
   * 但它的结果因范围键不符而不写入状态。
   */
  const requestIdRef = useRef(0)
  const submittingRef = useRef(false)

  /** 读取指定范围；失败只进入不可写状态并保留已有数据，不用失败结果覆盖成空列表。 */
  const load = useCallback(async (query: Query, requestScopeKey: string | null, failureKind: Exclude<AmountLoadFailure, null>) => {
    const requestId = ++requestIdRef.current
    setLoading(true)
    try {
      const next = await queryRows(query)
      setData((previous) => (previous.key === requestScopeKey ? { key: requestScopeKey, rows: next, failure: null } : previous))
    } catch {
      setData((previous) => (previous.key === requestScopeKey ? { ...previous, failure: failureKind } : previous))
    } finally {
      // 只有最新一次读取结束才结束加载态，避免先发起的旧响应提前放行写入。
      if (requestId === requestIdRef.current) setLoading(false)
    }
  }, [queryRows])

  useEffect(() => {
    if (scope === null) return
    // 推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(() => {
      void load(scope.query, scopeKey, 'load')
    }, 0)
    return () => window.clearTimeout(task)
  }, [load, scope, scopeKey])

  /**
   * 金额弹窗的目标：只持标准项目编码，行数据每次渲染都按**当前行集**重取，
   * 因此后台刷新交付新值后，弹窗展示与回填取的都是当前行；行已不在当前范围视图时不渲染弹窗，
   * 避免对一个未知行提交写入。
   */
  const targetCode = modal.kind === 'amount' ? modal.standardProjectCode : null
  const targetRow = targetCode === null
    ? null
    : rows.find((candidate) => candidate.standardProjectCode === targetCode) ?? null

  /** 写能力：范围完整、读取未失败、读取不在途且无在途提交（加载期间禁用写入）。 */
  const canWrite = scope !== null && loadFailure === null && !loading && !submitting
  const writeGate: RecognitionAmountWriteGate = { canWrite, loadFailure }

  const retry = () => {
    if (scope === null) return
    void load(scope.query, scopeKey, loadFailure ?? 'load')
  }

  /**
   * 提交一笔金额：先由适配层构造载荷（范围或金额不合法即零写请求），成功后才关闭弹窗并整表重载；
   * 写失败保留弹窗与输入、不重载也不误报成功（错误由宿主统一展示）。
   * 提交期同步去重，防止连续点击产生多次写请求。
   */
  const submitAmount = async (standardProjectCode: string, amountInput: string) => {
    if (scope === null) return
    if (submittingRef.current) return
    submittingRef.current = true
    setSubmitting(true)
    let sent = false
    try {
      sent = await saveRow(scope.query, standardProjectCode, amountInput)
    } finally {
      submittingRef.current = false
      setSubmitting(false)
    }
    if (!sent) return
    setModal({ kind: 'none' })
    await load(scope.query, scopeKey, 'refreshAfterWrite')
  }

  /**
   * 表格数据源：标准项目编码是本范围内的业务唯一键；编码缺失时退回出现序号补齐唯一标识，
   * 避免表格 key 重复、操作控件串位。该序号不参与任何业务判定，也不作为字段提交或展示。
   */
  const tableRows = useMemo<TableRow[]>(() => {
    const occurrences = new Map<string, number>()
    return rows.map((row) => {
      const businessIdentity = row.standardProjectCode ?? ''
      const occurrence = occurrences.get(businessIdentity) ?? 0
      occurrences.set(businessIdentity, occurrence + 1)
      return { ...row, rowIdentity: occurrence === 0 ? businessIdentity : `${businessIdentity}#${occurrence}` }
    })
  }, [rows])

  const columns: TableColumnsType<TableRow> = [
    { title: '标准项目编码', dataIndex: 'standardProjectCode', width: 150, render: (value: string | null) => value ?? '—' },
    { title: '标准项目名称', dataIndex: 'standardProjectName', width: 180, render: (value: string | null) => value ?? '—' },
    { title: '组织名称', dataIndex: 'organizationName', width: 160, render: (value: string | null) => value ?? '—' },
    { title: '医院名称', dataIndex: 'hospitalName', width: 160, render: (value: string | null) => value ?? '—' },
    { title: '院区名称', dataIndex: 'branchName', width: 140, render: (value: string | null) => value ?? '—' },
    {
      title: '当前金额',
      width: 120,
      // 未配置与已配置的零元必须可区分：未配置显示「未配置」标记且不出现 0.00，
      // 已配置（含零元）统一按适配层的金额出口呈现两位小数。
      render: (_, row) => (row.isAmountConfigured
        ? <span className='recognition-amounts-number'>{currentAmountText(row)}</span>
        : <Tag>未配置</Tag>),
    },
    {
      title: '金额状态',
      width: 110,
      render: (_, row) => <Tag color={row.isAmountConfigured ? 'green' : 'default'}>{row.isAmountConfigured ? '已配置' : '未配置'}</Tag>,
    },
    { title: '互认配置状态', width: 130, render: (_, row) => <Tag>{row.configurationStatusText}</Tag> },
    // 目录三层启用时服务端返回 null：此时不显示原因，不占用该列。
    { title: '当前不可用原因', dataIndex: 'unavailableReason', width: 170, ellipsis: true, render: (value: string | null) => value },
    {
      title: '操作',
      width: 110,
      fixed: 'right',
      render: (_, row) => {
        const standardProjectCode = row.standardProjectCode
        return <Button
          type='link'
          size='small'
          disabled={!canWrite || standardProjectCode === null}
          aria-label={`维护金额：${row.standardProjectName ?? standardProjectCode ?? '该行'}`}
          onClick={() => {
            if (standardProjectCode === null) return
            setModal({ kind: 'amount', standardProjectCode })
          }}
        >维护金额</Button>
      },
    },
  ]

  return <div className='recognition-amounts-page'>
    <div className='recognition-amounts-header'>
      <div>
        <Typography.Title level={2}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      <Space wrap>
        <Tooltip title='重新读取当前范围的金额列表'>
          <Button
            icon={<ReloadOutlined />}
            aria-label='重新读取金额列表'
            loading={loading}
            disabled={loading || scope === null}
            onClick={retry}
          />
        </Tooltip>
      </Space>
    </div>

    {selector}

    {loadFailure === null ? null : <Alert
      type='warning'
      showIcon
      className='recognition-amounts-failure'
      title={loadFailure === 'refreshAfterWrite' ? '写入已成功，数据待刷新' : '互认项目金额数据待刷新'}
      description={loadFailure === 'refreshAfterWrite'
        ? '写入已经成功，但重新读取金额列表失败；已有数据保留，维护操作暂不可用。'
        : '读取失败，已有数据保留；维护操作暂不可用，可重试读取。'}
      action={<Button size='small' disabled={loading} onClick={retry}>重试</Button>}
    />}

    <Table
      rowKey='rowIdentity'
      size='small'
      columns={columns}
      dataSource={tableRows}
      loading={loading && rows.length === 0}
      scroll={{ x: 1400 }}
      pagination={false}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          // 范围未选全与「读取成功但没有配置」是两件不同的事实，空态文案必须区分。
          description={scope === null ? missingScopeText : '当前范围没有互认项目金额配置'}
        />,
      }}
    />

    {targetRow === null || targetCode === null ? null : <AmountEntryModal
      open
      row={targetRow}
      writeGate={writeGate}
      submitting={submitting}
      onCancel={() => setModal({ kind: 'none' })}
      onSubmit={(values) => submitAmount(targetCode, values.amount ?? '')}
    />}
  </div>
}

/**
 * 平台管理员页：组织、医院、院区三级均可切换，三个值随查询与保存请求提交。
 *
 * 范围值由页面**受控**保存：`BaseScopeSelector` 在非受控（或显式 `selectFirstOption`）时才会
 * 自动选中首项并回调，受控时不回调，因此不因自动选中触发写入；写请求只在用户提交弹窗时发出。
 */
export function RecognitionAmounts() {
  const client = useApiClientContext()
  const [scope, setScope] = useState<BaseScopeValue>({})

  const queryRows = useCallback(
    (query: RecognitionAmountListQuery) => queryRecognitionAmountRows(client, query),
    [client],
  )
  const saveRow = useCallback(async (
    query: RecognitionAmountListQuery,
    standardProjectCode: string,
    amountInput: string,
  ) => {
    const payload = buildRecognitionAmountSavePayload({
      organizationCode: query.organizationCode,
      hospitalCode: query.hospitalCode,
      branchCode: query.branchCode,
      amountInput,
    }, standardProjectCode)
    // 载荷不合法（范围或金额）：不构造请求，也不按成功处理（本地校验与写门控已先行拦截）。
    if (payload === null) return false
    await saveOrganizationHospitalBranchRecognitionAmount(client, payload)
    return true
  }, [client])

  /**
   * 已选全的范围：组织、医院、院区任一缺失即为 `null`（不发起部分范围请求、不退化为全局查询）。
   * 请求构造只经适配层出口，页面不自拼请求体。
   */
  const appliedScope = useMemo(() => {
    const query = buildRecognitionAmountListQuery({
      organizationCode: scope.orgId ?? '',
      hospitalCode: scope.hosId ?? '',
      branchCode: scope.branchId ?? '',
    })
    return query === null
      ? null
      : { key: `${query.organizationCode}|${query.hospitalCode}|${query.branchCode}`, query }
  }, [scope.branchId, scope.hosId, scope.orgId])

  return <RecognitionAmountsBoard<RecognitionAmountListQuery>
    title='互认项目金额维护'
    subtitle='按组织、医院与院区维护互认项目金额'
    missingScopeText='请选择组织、医院与院区后查看互认项目金额'
    selector={<BaseScopeSelector
      level='branch'
      className='recognition-amounts-scope'
      value={scope}
      onChange={(next) => setScope(next)}
    />}
    scope={appliedScope}
    queryRows={queryRows}
    saveRow={saveRow}
  />
}
