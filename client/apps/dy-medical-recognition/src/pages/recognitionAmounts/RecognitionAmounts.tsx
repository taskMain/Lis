/**
 * 「互认项目金额维护」平台管理员页（宿主路径 `/subApps/medical-recognition/recognition-amounts`）。
 *
 * 页面本身只做范围接入与数据源接线：组织、医院、院区三级取自范围选择器（受控保存，院区由页面按
 * 默认规则解析且恒定单值），查询与保存请求都携带这三个值；筛选区、四栏层级列表与金额录入主体由本
 * 文件的 `RecognitionAmountsPlatformBoard` 承载。页面不实现角色判断、不做权限二次确认，也不下推
 * 枚举筛选。医院管理员页的列表主体在其实现文件内承载，本文件不再保留共用主体。
 */
import { Alert, Button, Empty, Input, Modal, Segmented, Table, Tag, Typography, type TableColumnsType } from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type KeyboardEvent, type ReactNode } from 'react'
import { useAuth } from '@dy/auth-react'
import { BaseScopeSelector, type BaseScopeSelectorChangeMeta, type BaseScopeValue } from '@dy/components-base'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readTrustedBranchCode, readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  AMOUNT_LOAD_FAILURE_TEXT,
  AMOUNT_STATUS_OPTIONS,
  AMOUNT_VALIDATION_MESSAGE,
  CONFIGURATION_STATUS_OPTIONS,
  EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT,
  EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT,
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
  currentAmountText,
  filterRecognitionAmountRows,
  parseRecognitionAmount,
  queryRecognitionAmountRows,
  readAmountFilterFromLocation,
  recheckRecognitionAmountScope,
  saveOrganizationHospitalBranchRecognitionAmount,
  syncAmountFilterToLocation,
  toRecognitionAmountColumnView,
  type AmountConfigurationStatusFilter,
  type AmountStatusFilter,
  type RecognitionAmountColumnProjectRow,
  type RecognitionAmountListQuery,
  type RecognitionAmountRow,
} from './recognitionAmountsApi'
import './RecognitionAmounts.css'

/**
 * 列表读取失败态。`refreshAfterWrite` 表示写入**已经成功**、随后的重载失败：
 * 呈现与阻断规则与读取失败一致，但不得改判为保存失败。
 */
export type AmountLoadFailure = null | 'load' | 'refreshAfterWrite'

/** 金额必填提示；格式判据本身取适配层出口，页面不另写金额规则。 */
const AMOUNT_REQUIRED_MESSAGE = '金额必填'

/**
 * C34 提交前现值复核失败时的行内原因（S3-D28；Client/testPlan.md 第 42 行双场景共用一句）：
 * 一句话覆盖「已变化」与「现读不到」两种情形，随后给出零写结论与后续动作（切换到新可信范围）。
 * 该行由页面书写并就地展示；适配层交付的原因串服务于复核出口的用例，不随本页展示。
 */
const TRUSTED_SCOPE_SUBMIT_BLOCK_REASON = '可信范围已变化或读不到，本次不提交写入，请切换到新可信范围后重新提交。'

/**
 * 「可信院区」的渲染期现读接缝：token 变化即重新求值（与医院管理员页 `useTrustedBranchScope`
 * 同一手法），宿主重新签发 token 后页面在下一次渲染就读到新的可信院区。
 */
function useTrustedBranchCode(): string | null {
  const { token } = useAuth()
  // token 缺失时凭据不表达可信院区，直接判为不可用。
  if (!token) return null
  return readTrustedBranchCode()
}

/**
 * 院区按 S3-D22 三档规则解析：可信院区属于给定院区清单则选中它，可信院区缺失或不属于该清单时
 * 取清单第一项；清单为空时返回 `null`（不构造空院区请求）。
 */
function resolveBranchFromList(
  trustedBranchCode: string | null,
  branches: BaseScopeSelectorChangeMeta['branches'],
): string | null {
  const firstId = branches[0]?.id ?? null
  if (firstId === null) return null
  return trustedBranchCode !== null && branches.some((item) => item.id === trustedBranchCode)
    ? trustedBranchCode
    : firstId
}

/** 页面主体当前范围；`key` 是范围的稳定标识，变化即视为范围切换。 */
export interface RecognitionAmountsScope<Query> {
  key: string
  query: Query
}

/** 范围相关状态整体按范围键存放：写入时带上请求时的范围键，渲染只取当前范围键的数据。 */
interface ScopeData {
  key: string | null
  rows: RecognitionAmountRow[]
  failure: AmountLoadFailure
}

/** 平铺表格行：项目字段加上类型、分类、分组三个层级格的跨行信息。 */
interface RecognitionAmountTableRow {
  /** 页面行标识 = 项目编码 + 同范围出现序号；不作为业务字段提交或展示。 */
  rowIdentity: string
  project: RecognitionAmountColumnProjectRow
  typeText: string
  typeRowSpan: number
  typeFirst: boolean
  categoryName: string | null
  categoryRowSpan: number
  categoryFirst: boolean
  groupName: string | null
  groupRowSpan: number
  groupFirst: boolean
}

interface RecognitionAmountsPlatformBoardProps {
  title: string
  subtitle: string
  /** 范围尚未解析时的空态文案（选项读取失败等场景）。 */
  missingScopeText: string
  /** 范围选择器节点；由调用方以受控值接入，主体只负责渲染，不参与选择器状态。 */
  selector: ReactNode
  /** 页头右侧的三级范围展示节点；范围未解析时为 `null`。 */
  scopeDisplay: ReactNode
  /** `null` 表示范围未解析：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: RecognitionAmountsScope<RecognitionAmountListQuery> | null
  /** 待切换提示态节点（C33）；不存在待切换范围时为 `null`。 */
  scopePrompt: ReactNode
  /** 页面应用范围时记录的可信院区（C34 提交前现值复核的比对基准，与 C33 挂起判定取同一记录值）。 */
  appliedTrustedBranch: string | null | undefined
  /** 行内编辑目标行的项目编码；未保存内容的有无由页面统一裁定（C33）。 */
  editTarget: string | null
  /** 换绑挂起中（确认框打开或待切换提示态）：期间入口禁用、回车零写返回，行内输入仍可编辑。 */
  writeSuspended: boolean
  /** 进入或退出行内编辑；目标行离开当前行集时由主体同步清空。 */
  onEditTargetChange: (standardProjectCode: string | null) => void
  queryRows: (query: RecognitionAmountListQuery) => Promise<RecognitionAmountRow[]>
  /** 保存当前行的金额；返回 `false` 表示**未发出写请求**（载荷不合法），主体据此保留编辑态与输入。 */
  saveRow: (query: RecognitionAmountListQuery, standardProjectCode: string, amountInput: string) => Promise<boolean>
}

/**
 * 平台管理员页的列表主体：范围状态机、关键字与状态筛选、四栏层级列表、金额行内编辑。
 *
 * 读取时机只有三类：范围变化、关键字提交（含输入框清空与空条件提交）与失败后的重试；
 * 两个状态 Segmented 只在服务端交付的结果上本地裁剪，不参与读取时机，也不下推请求。
 */
function RecognitionAmountsPlatformBoard({
  title,
  subtitle,
  missingScopeText,
  selector,
  scopeDisplay,
  scope,
  scopePrompt,
  appliedTrustedBranch,
  editTarget,
  writeSuspended,
  onEditTargetChange,
  queryRows,
  saveRow,
}: RecognitionAmountsPlatformBoardProps) {
  const scopeKey = scope?.key ?? null
  const [data, setData] = useState<ScopeData>({ key: scopeKey, rows: [], failure: null })
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  /** 提交前复核失败的一行原因；每次提交尝试整体替换，编辑目标变化时清空。 */
  const [recheckReason, setRecheckReason] = useState<string | null>(null)

  /**
   * 范围切换在**渲染期**复位（React 的「props 变化时调整 state」写法）：等到 effect 再复位，
   * 旧范围的行会先渲染一帧。复位后 `data.key` 恒等于当前范围键，因此读取只能写回与
   * 请求同范围的数据，迟到的旧范围响应既不改写新范围的行，也不改写新范围的失败态。
   * 未保存的金额输入随目标行离开当前行集被清空，不带进新范围。
   */
  if (data.key !== scopeKey) {
    setData({ key: scopeKey, rows: [], failure: null })
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
  const load = useCallback(async (query: RecognitionAmountListQuery, requestScopeKey: string | null, failureKind: Exclude<AmountLoadFailure, null>) => {
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

  // ── 筛选状态：已生效条件在挂载时按地址栏初始化，草稿只承载输入原文 ──
  const [initialFilter] = useState(readAmountFilterFromLocation)
  const [keywordDraft, setKeywordDraft] = useState(initialFilter.keyword)
  const [keywordApplied, setKeywordApplied] = useState(initialFilter.keyword)
  const [amountStatus, setAmountStatus] = useState<AmountStatusFilter>(initialFilter.amountStatus)
  const [configurationStatus, setConfigurationStatus] = useState<AmountConfigurationStatusFilter>(initialFilter.configurationStatus)
  /**
   * 空条件提交与输入框清空也要各读取一次：范围与关键字都没变时，读取效果的其它依赖不动，
   * 靠这个推进号让效果重新执行（空条件提交等价于刷新）。
   */
  const [reloadNonce, setReloadNonce] = useState(0)

  const submitKeyword = () => {
    setKeywordApplied(keywordDraft)
    setReloadNonce((nonce) => nonce + 1)
  }

  const handleKeywordChange = (event: ChangeEvent<HTMLInputElement>) => {
    const next = event.target.value
    if (next.length > 0) {
      setKeywordDraft(next)
      return
    }
    // 输入框清空动作（清除入口或手动删空）同时清空草稿与已生效关键字，并触发一次空条件读取。
    setKeywordDraft('')
    setKeywordApplied('')
    setReloadNonce((nonce) => nonce + 1)
  }

  // 已生效条件同步地址栏（替换当前历史项）；内容未变化时适配层不触达历史 API。
  useEffect(() => {
    syncAmountFilterToLocation({ keyword: keywordApplied, amountStatus, configurationStatus })
  }, [amountStatus, configurationStatus, keywordApplied])

  /** 当前范围 + 已生效关键字的读取请求；范围与关键字任一变化才换新对象，驱动下面的读取效果。 */
  const appliedQuery = useMemo(() => {
    if (scope === null) return null
    return buildRecognitionAmountListQuery({ ...scope.query, keyword: keywordApplied })
  }, [keywordApplied, scope])

  useEffect(() => {
    if (appliedQuery === null) return
    // 推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(() => {
      void load(appliedQuery, scopeKey, 'load')
    }, 0)
    return () => window.clearTimeout(task)
  }, [appliedQuery, load, reloadNonce, scopeKey])

  /** 写能力：范围完整、读取未失败、读取不在途且无在途提交（加载期间禁用写入）。 */
  const canWrite = scope !== null && loadFailure === null && !loading && !submitting

  const retry = () => {
    if (appliedQuery === null) return
    void load(appliedQuery, scopeKey, loadFailure ?? 'load')
  }

  /**
   * 提交一笔金额：换绑挂起中（确认框打开或待切换提示态）零写返回（C33 的写门控，第一道）；
   * 随后发起提交前现值复核（C34，S3-D28，第二道，兜住用户未触发挂起路径的场景）——比对
   * 「页面应用范围时记录的可信院区」与「保存发起前现读的可信院区」，比对对象是可信范围的
   * 变化，用户选定的 `scope.query` 三值不参与；记录与现读不一致即零写请求（院区层两侧同为
   * 缺失视为一致，S3-D28 档二），原因在行内就地
   * 展示（页面书写的一行中文）；随后由适配层构造载荷（范围或金额不合法同样零写请求），成功
   * 后才收起编辑态并整表重载；写失败保留编辑态与输入、不重载也不误报成功（拒绝文案由宿主
   * 统一展示）。提交期同步去重，防止连续回车产生多次写请求。
   */
  const submitAmount = async (standardProjectCode: string, amountInput: string) => {
    if (writeSuspended) return
    if (appliedQuery === null || scope === null) return
    if (submittingRef.current) return
    // 复核两侧都是范围对象：院区一层取「记录值」与「保存发起前现读值」；组织、医院两层本页
    // 没有对应的可信现读接缝，按 null 传入且不参与比较（layers 只含院区一层）。
    const recheck = recheckRecognitionAmountScope({
      applied: { organizationCode: null, hospitalCode: null, branchCode: appliedTrustedBranch ?? null },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: readTrustedBranchCode() },
      layers: ['branch'],
    })
    if (!recheck.ok) {
      // 行内原因整体替换且由页面书写，不透传适配层交付的原因串。
      setRecheckReason(TRUSTED_SCOPE_SUBMIT_BLOCK_REASON)
      return
    }
    setRecheckReason(null)
    if (submittingRef.current) return
    submittingRef.current = true
    setSubmitting(true)
    let sent = false
    try {
      sent = await saveRow(appliedQuery, standardProjectCode, amountInput)
    } finally {
      submittingRef.current = false
      setSubmitting(false)
    }
    if (!sent) return
    onEditTargetChange(null)
    await load(appliedQuery, scopeKey, 'refreshAfterWrite')
  }

  /** 本地裁剪：两个状态条件只在服务端交付的结果上取交集；关键字已由服务端裁剪，不参与本地裁剪。 */
  const visibleRows = useMemo(
    () => filterRecognitionAmountRows(rows, { amountStatus, configurationStatus }),
    [amountStatus, configurationStatus, rows],
  )

  /**
   * 四栏层级列视图平铺成表格行：类型、分类与分组的跨行信息随行携带，`first` 标出该层级格所在的
   * 首行，其余行的层级格按 `rowSpan` 0 交给单元格出口跳过渲染，形成纵向合并的层级列。
   */
  const tableRows = useMemo<RecognitionAmountTableRow[]>(() => {
    const occurrences = new Map<string, number>()
    const flattened: RecognitionAmountTableRow[] = []
    for (const type of toRecognitionAmountColumnView(visibleRows).types) {
      let typeFirstRow = true
      for (const category of type.categories) {
        let categoryFirstRow = true
        for (const group of category.groups) {
          let groupFirstRow = true
          for (const project of group.rows) {
            const businessIdentity = project.standardProjectCode ?? ''
            const occurrence = occurrences.get(businessIdentity) ?? 0
            occurrences.set(businessIdentity, occurrence + 1)
            flattened.push({
              rowIdentity: occurrence === 0 ? businessIdentity : `${businessIdentity}#${occurrence}`,
              project,
              typeText: type.itemTypeText,
              typeRowSpan: type.rowSpan,
              typeFirst: typeFirstRow,
              categoryName: category.categoryName,
              categoryRowSpan: category.rowSpan,
              categoryFirst: categoryFirstRow,
              groupName: group.groupName,
              groupRowSpan: group.rowSpan,
              groupFirst: groupFirstRow,
            })
            groupFirstRow = false
            categoryFirstRow = false
            typeFirstRow = false
          }
        }
      }
    }
    return flattened
  }, [visibleRows])

  /**
   * 目标行离开当前行集即清空编辑目标：搜索交付、状态档裁剪与范围切换都经过这条同步，
   * 「未保存内容」因此始终等于「编辑中的行仍在当前行集」（C32、C33 的口径）。
   */
  useEffect(() => {
    if (editTarget === null) return
    if (visibleRows.some((candidate) => candidate.standardProjectCode === editTarget)) return
    onEditTargetChange(null)
  }, [editTarget, onEditTargetChange, visibleRows])

  const columns: TableColumnsType<RecognitionAmountTableRow> = [
    {
      title: '类型',
      width: 110,
      // 层级格纵向跨其名下全部行；非首行的层级格按 rowSpan 0 跳过渲染。
      onCell: (row) => ({ rowSpan: row.typeFirst ? row.typeRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.typeText,
    },
    {
      title: '分类',
      width: 150,
      onCell: (row) => ({ rowSpan: row.categoryFirst ? row.categoryRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.categoryName ?? '—',
    },
    {
      title: '分组',
      width: 130,
      onCell: (row) => ({ rowSpan: row.groupFirst ? row.groupRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.groupName ?? '—',
    },
    {
      title: '项目',
      width: 220,
      render: (_, row) => (
        <span className='recognition-amounts-project'>
          <span className='recognition-amounts-project-code'>{row.project.standardProjectCode ?? '—'}</span>
          <span className='recognition-amounts-project-name'>{row.project.standardProjectName ?? '—'}</span>
        </span>
      ),
    },
    {
      title: '当前金额',
      width: 110,
      // 未配置与已配置的零元都走适配层的金额出口：「未配置」与 0.00 必须可区分；
      // 每行金额即行内编辑入口（C32），编辑态以当前行金额值为键重新挂载并回填新值。
      render: (_, row) => {
        const standardProjectCode = row.project.standardProjectCode
        if (standardProjectCode !== null && standardProjectCode === editTarget) {
          return <AmountInlineEditor
            key={`${standardProjectCode}|${currentAmountText(row.project)}`}
            defaultValue={row.project.isAmountConfigured ? currentAmountText(row.project) : ''}
            disabled={!canWrite}
            submitting={submitting}
            recheckReason={recheckReason}
            onSubmit={(value) => { void submitAmount(standardProjectCode, value) }}
            onCancel={() => {
              setRecheckReason(null)
              onEditTargetChange(null)
            }}
          />
        }
        return <Button
          type='link'
          size='small'
          className='recognition-amounts-number'
          disabled={!canWrite || writeSuspended || standardProjectCode === null}
          onClick={() => {
            if (standardProjectCode === null) return
            // 每次进入编辑都从干净状态开始：上一次的复核原因不带进新一次编辑。
            setRecheckReason(null)
            onEditTargetChange(standardProjectCode)
          }}
        >{currentAmountText(row.project)}</Button>
      },
    },
    {
      title: '金额状态',
      width: 100,
      render: (_, row) => (
        <Tag color={row.project.isAmountConfigured ? 'green' : 'default'}>
          {row.project.isAmountConfigured ? '已配置' : '未配置'}
        </Tag>
      ),
    },
    { title: '互认配置状态', width: 130, render: (_, row) => <Tag>{row.project.configurationStatusText}</Tag> },
    // 目录三层启用时服务端返回 null：此时不显示原因，不占用该列。
    { title: '当前不可用原因', width: 170, ellipsis: true, render: (_, row) => row.project.unavailableReason },
  ]

  /** 生效条件：关键字或任一状态档非默认即视为有条件，用于区分零配置与未命中两种空态。 */
  const hasConditions = keywordApplied !== '' || amountStatus !== 'all' || configurationStatus !== 'all'
  const emptyDescription = () => {
    if (scope === null) return missingScopeText
    if (hasConditions) return EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT
    return EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT
  }

  return <div className='recognition-amounts-page'>
    <div className='recognition-amounts-header'>
      <div>
        <Typography.Title level={2}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      {scopeDisplay}
    </div>

    {selector}

    {loadFailure === null ? null : <Alert
      type='warning'
      showIcon
      className='recognition-amounts-failure'
      title={loadFailure === 'refreshAfterWrite' ? AMOUNT_LOAD_FAILURE_TEXT.refreshTitle : AMOUNT_LOAD_FAILURE_TEXT.loadTitle}
      description={loadFailure === 'refreshAfterWrite'
        ? AMOUNT_LOAD_FAILURE_TEXT.refreshDescription
        : AMOUNT_LOAD_FAILURE_TEXT.loadDescription}
      action={<Button size='small' disabled={loading} onClick={retry}>重试</Button>}
    />}

    <div className='recognition-amounts-filter'>
      <Input
        className='recognition-amounts-keyword'
        aria-label='关键字'
        placeholder='标准项目编码或名称'
        allowClear
        value={keywordDraft}
        onChange={handleKeywordChange}
        onPressEnter={submitKeyword}
      />
      <Button type='primary' loading={loading} onClick={submitKeyword}>搜索</Button>
      <Segmented<AmountStatusFilter>
        aria-label='金额状态筛选'
        value={amountStatus}
        options={AMOUNT_STATUS_OPTIONS}
        onChange={setAmountStatus}
      />
      <Segmented<AmountConfigurationStatusFilter>
        aria-label='互认配置状态筛选'
        value={configurationStatus}
        options={CONFIGURATION_STATUS_OPTIONS}
        onChange={setConfigurationStatus}
      />
    </div>

    {scopePrompt}

    <Table
      rowKey='rowIdentity'
      size='small'
      columns={columns}
      dataSource={tableRows}
      loading={loading && tableRows.length === 0}
      scroll={{ x: 1260 }}
      pagination={false}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          // 范围未解析、范围零配置与条件未命中是三件不同的事实，空态文案必须区分。
          description={emptyDescription()}
        />,
      }}
    />

    <div className='recognition-amounts-count'>共 {tableRows.length} 项</div>
  </div>
}

/**
 * 金额行内编辑器（C32）：进入编辑即回填当前行金额值，回车提交、Esc 取消；
 * 金额判据只取适配层出口 `parseRecognitionAmount`，提示就地展示，`aria-invalid`
 * 随每次输入重新求值。调用方以「行 + 当前金额值」为键挂载本组件：后台刷新交付
 * 新值时组件重挂并回填新值。
 */
function AmountInlineEditor({ defaultValue, disabled, submitting, recheckReason, onSubmit, onCancel }: {
  /** 回填值：已配置金额的展示文本，未配置时为空串。 */
  defaultValue: string
  /** 写门控：读取在途、失败或范围不完整时不可编辑。 */
  disabled: boolean
  /** 在途提交：输入暂时禁用，提交结束（无论成败）后恢复。 */
  submitting: boolean
  /** 提交前复核失败的一行原因；页面在每次提交尝试后整体替换。 */
  recheckReason: string | null
  /** 回车提交；拒绝不在此处捕获，错误由宿主统一展示（C10）。 */
  onSubmit: (value: string) => void
  /** Esc 取消：收起编辑态并丢弃输入。 */
  onCancel: () => void
}) {
  const [value, setValue] = useState(defaultValue)
  const [message, setMessage] = useState<string | null>(null)
  const trimmed = value.trim()
  const invalid = trimmed === '' || parseRecognitionAmount(value) === null

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const next = event.target.value
    setValue(next)
    // 行内提示随输入重新求值：改回合法值后提示一并撤下。
    if (message !== null && next.trim() !== '' && parseRecognitionAmount(next) !== null) {
      setMessage(null)
    }
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      onCancel()
      return
    }
    if (event.key !== 'Enter') return
    event.preventDefault()
    if (trimmed.length === 0) {
      setMessage(AMOUNT_REQUIRED_MESSAGE)
      return
    }
    if (invalid) {
      setMessage(AMOUNT_VALIDATION_MESSAGE)
      return
    }
    setMessage(null)
    onSubmit(value)
  }

  return <div className='recognition-amounts-editor'>
    <Input
      aria-label='金额'
      autoFocus
      disabled={disabled || submitting}
      value={value}
      aria-invalid={invalid}
      onChange={handleChange}
      onKeyDown={handleKeyDown}
    />
    {message === null ? null : <div><Typography.Text type='danger'>{message}</Typography.Text></div>}
    {recheckReason === null ? null : <div><Typography.Text type='danger'>{recheckReason}</Typography.Text></div>}
  </div>
}

/** 页面已解析的范围：三值恒定齐全；选项清单与取值同帧交付，页头展示据此取名称。 */
interface ScopeSelection {
  scope: { orgId: string; hosId: string; branchId: string }
  meta: {
    organizations: BaseScopeSelectorChangeMeta['organizations']
    hospitals: BaseScopeSelectorChangeMeta['hospitals']
    branches: BaseScopeSelectorChangeMeta['branches']
  }
}

/**
 * 平台管理员页的初始范围解析（S3-D22）：可信组织或可信医院任一缺失时组织、医院、院区整条级联
 * 取第一项（该档仅适用平台管理员页，仍存在的那个可信值一并不采用）；两者都在时组织取可信组织
 * （缺失或不属于选项清单时取本次交付的第一项），医院取可信医院且须属于选定组织（缺失或不属于
 * 清单时取该院第一项），院区取可信院区且须属于当前组织、医院（缺失或不属于时取新范围第一项）。
 *
 * 本次交付的选项清单由选择器按其受控值装载：选定组织或医院与交付组合不一致时，交付的医院与
 * 院区清单仍属于首项组合，院区归属无从核对。此时只应用组织与医院、院区留空——三值不齐不构造
 * 查询（`buildRecognitionAmountListQuery` 对空白即返回 `null`），选择器按受控值重载下游选项并
 * 回传新清单后，由院区仍为空的第二轮解析完成取值（受控值与回传值不一致即重载，与真实
 * `BaseScopeSelector` 的初始数据装载同一口径）。
 */
function resolveInitialScopeSelection(
  next: BaseScopeValue,
  meta: BaseScopeSelectorChangeMeta,
  trusted: { organizationCode: string | null; hospitalCode: string | null; branchCode: string | null },
): ScopeSelection {
  const trustedOrganizationCode = trusted.organizationCode
  const trustedHospitalCode = trusted.hospitalCode
  const trustedBranchCode = trusted.branchCode
  const firstOrgId = next.orgId ?? ''
  const firstHosId = next.hosId ?? ''
  const firstBranchId = next.branchId ?? ''
  const metaSelection: ScopeSelection['meta'] = {
    organizations: meta.organizations,
    hospitals: meta.hospitals,
    branches: meta.branches,
  }
  // 可信组织或可信医院任一缺失：组织、医院、院区整条级联取第一项，交付值即首项组合。
  if (trustedOrganizationCode === null || trustedHospitalCode === null) {
    return { scope: { orgId: firstOrgId, hosId: firstHosId, branchId: firstBranchId }, meta: metaSelection }
  }
  // 组织取可信组织；缺失或不属于选项清单时取本次交付的第一项。
  const organizationCode = trustedOrganizationCode !== null
    && meta.organizations.some((item) => item.id === trustedOrganizationCode)
    ? trustedOrganizationCode
    : firstOrgId
  if (organizationCode !== firstOrgId) {
    // 选定组织并非交付时的组织：医院清单尚未按它装载，先应用组织与可信医院、院区留空等待重载。
    return { scope: { orgId: organizationCode, hosId: trustedHospitalCode ?? '', branchId: '' }, meta: metaSelection }
  }
  // 医院取可信医院且须属于选定组织；缺失或不属于清单时取该院第一项。
  const hospitalCode = trustedHospitalCode !== null
    && meta.hospitals.some((item) => item.id === trustedHospitalCode)
    ? trustedHospitalCode
    : firstHosId
  if (hospitalCode !== firstHosId) {
    // 选定医院并非交付时的医院：院区清单属于首项组合，先应用组织与医院、院区留空等待重载。
    return { scope: { orgId: organizationCode, hosId: hospitalCode, branchId: '' }, meta: metaSelection }
  }
  // 院区取可信院区且须属于当前组织、医院；缺失或不属于清单时取新范围第一项。
  const branchCode = trustedBranchCode !== null
    && meta.branches.some((item) => item.id === trustedBranchCode)
    ? trustedBranchCode
    : firstBranchId
  return { scope: { orgId: organizationCode, hosId: hospitalCode, branchId: branchCode }, meta: metaSelection }
}

/**
 * 页头三级范围展示：上行名称（正文色、字重稍重）、下行编码（次要色、小一号）整体右对齐，
 * 不设「组织」这类文字标签；名称未就绪时该行不展示，只可能展示编码。
 */
function buildScopeDisplay(selection: ScopeSelection | null): ReactNode {
  if (selection === null) return null
  const { scope: selectedScope, meta } = selection
  const levels = [
    {
      code: selectedScope.orgId,
      name: meta.organizations.find((item) => item.id === selectedScope.orgId)?.name ?? null,
    },
    {
      code: selectedScope.hosId,
      name: meta.hospitals.find((item) => item.id === selectedScope.hosId)?.name ?? null,
    },
    {
      code: selectedScope.branchId,
      name: meta.branches.find((item) => item.id === selectedScope.branchId)?.name ?? null,
    },
  ]
  const namedLevels = levels.filter((level): level is { code: string; name: string } =>
    level.name !== null && level.name.length > 0)
  return <div className='recognition-amounts-header-scope'>
    {namedLevels.length === 0 ? null : <div className='recognition-amounts-scope-names'>
      {namedLevels.map((level) => (
        <span className='recognition-amounts-scope-name' key={level.code}>{level.name}</span>
      ))}
    </div>}
    <div className='recognition-amounts-scope-codes'>
      {levels.map((level) => (
        <span className='recognition-amounts-scope-code' key={level.code}>{level.code}</span>
      ))}
    </div>
  </div>
}

/**
 * 平台管理员页：组织、医院可切换，院区恒定单值，三个值随查询与保存请求提交。
 *
 * 范围值由页面**受控**保存并按默认规则解析：`BaseScopeSelector` 以 `selectFirstOption` 在初始
 * 选项加载完成后回调首项组合与选项清单，页面按 S3-D22 完成初始解析——可信组织或可信医院任一
 * 缺失时组织、医院、院区整条级联取第一项，两者都在时三值逐级取可信值（组织、医院、院区各自
 * 缺失或不属于上级清单时取该层第一项），选定组织或医院与交付组合不一致时先留空院区、等选择器
 * 按受控值重载新清单后完成第二轮解析；在此之上页面还做四件事——三值任一缺失的回调保持原受控值（院区永不为
 * 空发请求）、切换组织医院时把属于新范围的可信院区作为默认值（同一组织医院内的显式改选不被
 * 覆盖）、从选项清单取名称供页头展示，以及在可信范围变化时按未保存内容的有无决定直接切换还是
 * 先弹确认（C33）；C33 的变化判定在**渲染期**现读可信院区派生，宿主单独重新签发 token、用户
 * 不碰选择器时同样触发，选择器回调里的现读比对只作第二道。
 * 写请求只在用户回车提交行内编辑时发出。
 */
export function RecognitionAmounts() {
  const client = useApiClientContext()
  /** 现读可信院区（渲染期接缝）：宿主重新签发 token 后由订阅推进，在渲染期现读比对。 */
  const trustedBranch = useTrustedBranchCode()
  const [selection, setSelection] = useState<ScopeSelection | null>(null)
  /** 行内编辑目标行的项目编码；列表主体负责在目标行离开当前行集时清空它。 */
  const [editTarget, setEditTarget] = useState<string | null>(null)
  /** 上次应用范围时记录的可信院区；`undefined` 表示页面尚未应用过一次范围（渲染期比对用，不放 ref）。 */
  const [appliedTrustedBranch, setAppliedTrustedBranch] = useState<string | null | undefined>(undefined)
  /** 取消确认框时针对的可信院区：与现读可信院区一致即处于待切换提示态（C33，渲染期派生）。 */
  const [dismissedTrustedBranch, setDismissedTrustedBranch] = useState<string | null | undefined>(undefined)
  /** 挂起期间选择器回调交付的目标范围：确认或提示态再次确认时按它完成切换。 */
  const [switchTarget, setSwitchTarget] = useState<ScopeSelection | null>(null)

  /** 应用一次范围切换：记录现读可信院区，清空编辑目标与挂起状态。 */
  const applySelection = useCallback((next: ScopeSelection, trustedBranchCode: string | null) => {
    setAppliedTrustedBranch(trustedBranchCode)
    setSelection(next)
    setEditTarget(null)
    setDismissedTrustedBranch(undefined)
    setSwitchTarget(null)
  }, [])

  /**
   * 可信范围变化（C33）的渲染期派生：宿主重新签发 token 后，现读可信院区已不同于应用范围时
   * 记录的取值，而页面仍展示已应用范围的列表。token 缺失时订阅值为 `null`，凭据不表达可信院区，
   * 不记作已变化；确认框、待切换提示态与写门控都从这一份比对派生，不在 effect 里写入渲染状态
   * （照阶段 2「互认项目」页先例）；未保存内容的判定口径为行内编辑正在编辑的行仍在当前行集
   * （Client/design.md 第 34 行）。
   */
  const hasUnsavedContent = editTarget !== null
  const trustedBranchChanged = selection !== null
    && appliedTrustedBranch !== undefined
    && trustedBranch !== null
    && appliedTrustedBranch !== trustedBranch
  const switchPending = trustedBranchChanged && hasUnsavedContent
  const switchConfirming = switchPending && dismissedTrustedBranch !== trustedBranch
  const switchDeferred = switchPending && dismissedTrustedBranch === trustedBranch

  /**
   * 完成一次可信范围变化的切换（C33）：挂起期间选择器交付过目标范围就按它应用；否则当前组织、
   * 医院不变，按 S3-D22 三档规则重取院区（可信院区属于当前组织医院则选中它，否则取该院区清单
   * 第一项）。院区取值取订阅的可信院区，与渲染期比对同源。
   */
  const completeSwitch = useCallback(() => {
    if (selection === null) return
    if (switchTarget !== null) {
      applySelection(switchTarget, trustedBranch)
      return
    }
    const branchId = resolveBranchFromList(trustedBranch, selection.meta.branches)
    if (branchId === null) return
    applySelection({ scope: { ...selection.scope, branchId }, meta: selection.meta }, trustedBranch)
  }, [applySelection, selection, switchTarget, trustedBranch])

  /**
   * 无未保存内容时的直接切换：可信院区变化来自宿主重新签发 token，属于外部系统变化，不在 effect
   * 内同步级联渲染状态，同样推迟到下一个宏任务（照阶段 2「互认项目」页先例）。有未保存内容时
   * 等待确认框或待切换提示态，不清理、不加载。
   */
  useEffect(() => {
    if (!trustedBranchChanged) return
    if (hasUnsavedContent) return
    const task = window.setTimeout(() => {
      completeSwitch()
    }, 0)
    return () => window.clearTimeout(task)
  }, [completeSwitch, hasUnsavedContent, trustedBranchChanged])

  const handleSelectorChange = useCallback((next: BaseScopeValue, meta: BaseScopeSelectorChangeMeta) => {
    // 三级任一缺失的回调不被接受：保持原受控值（院区恒定单值，也不发空范围请求）。
    if (!next.orgId || !next.hosId || !next.branchId) return
    const current = selection?.scope ?? null
    if (current === null || current.branchId === '') {
      // 初始解析（S3-D22）：第一轮按本次交付的清单解析；选定组织或医院与交付组合不一致时只应用
      // 组织与医院、院区留空，选择器重载新清单后由院区仍为空的第二轮完成取值。现读可信三层与
      // 渲染期接缝同一来源（`LoginUserManager` 的既有出口），不另建第二套读取途径。
      const trustedNow = {
        organizationCode: readTrustedOrganizationCode(),
        hospitalCode: readTrustedHospitalCode(),
        branchCode: readTrustedBranchCode(),
      }
      applySelection(resolveInitialScopeSelection(next, meta, trustedNow), trustedNow.branchCode)
      return
    }
    const scopeLayerChanged = current.orgId !== next.orgId || current.hosId !== next.hosId
    // 可信院区只作默认值：切换组织医院时按新范围解析，归属以本回调携带的院区清单核对。
    const trustedBranchCode = scopeLayerChanged ? readTrustedBranchCode() : null
    const branchId = trustedBranchCode !== null && meta.branches.some((branch) => branch.id === trustedBranchCode)
      ? trustedBranchCode
      : next.branchId
    const nextSelection: ScopeSelection = {
      scope: { orgId: next.orgId, hosId: next.hosId, branchId },
      meta: { organizations: meta.organizations, hospitals: meta.hospitals, branches: meta.branches },
    }
    // 可信范围变化检测（C33）第二道：事件内现读可信院区，与渲染期派生同一口径——宿主重签 token
    // 到页面重渲染之间的窗口由这里兜住。变化且存在未保存内容时只记录目标、不应用本次切换（当前
    // 受控值保持不动）；用户主动切换且可信院区未变化时按原规则静默应用，未保存输入随目标行离开
    // 当前行集被清空（C21）。
    const trustedNow = readTrustedBranchCode()
    const trustedChanged = appliedTrustedBranch !== undefined && trustedNow !== appliedTrustedBranch
    if (trustedChanged && editTarget !== null) {
      setSwitchTarget(nextSelection)
      return
    }
    applySelection(nextSelection, trustedNow)
  }, [applySelection, appliedTrustedBranch, editTarget, selection])

  /**
   * 编辑目标改道：目标行离开当前行集时由主体同步清空；未保存内容消失后待切换提示态的自动完成
   * 交由上面的可信变化效果按派生状态处理（C33），这里不再直接触发切换。
   */
  const handleEditTargetChange = useCallback((target: string | null) => {
    setEditTarget(target)
  }, [])

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

  const organizationCode = selection?.scope.orgId ?? null
  const hospitalCode = selection?.scope.hosId ?? null
  const branchCode = selection?.scope.branchId ?? null
  /**
   * 已解析的范围 → 查询条件：三值恒定齐全，请求构造只经适配层出口。依赖只取三个编码字符串，
   * 选择器因选项清单刷新而重复回调时不重建条件对象，读取效果不会因此重复执行。
   */
  const appliedScope = useMemo(() => {
    if (organizationCode === null || hospitalCode === null || branchCode === null) return null
    const query = buildRecognitionAmountListQuery({ organizationCode, hospitalCode, branchCode })
    return query === null
      ? null
      : { key: `${query.organizationCode}|${query.hospitalCode}|${query.branchCode}`, query }
  }, [branchCode, hospitalCode, organizationCode])

  const scopePrompt = switchDeferred ? (
    <div className='recognition-amounts-prompt'>
      可信范围已变化，存在未保存的金额输入；确认后放弃输入并按新范围加载。
      <Button size='small' onClick={completeSwitch}>放弃并切换</Button>
    </div>
  ) : null

  return <>
    <RecognitionAmountsPlatformBoard
      title='互认项目金额维护'
      subtitle='按组织、医院与院区维护互认项目金额'
      missingScopeText='请选择组织、医院与院区后查看互认项目金额'
      selector={<BaseScopeSelector
        level='branch'
        className='recognition-amounts-scope'
        selectFirstOption
        value={selection?.scope ?? {}}
        onChange={handleSelectorChange}
      />}
      scopeDisplay={buildScopeDisplay(selection)}
      scope={appliedScope}
      scopePrompt={scopePrompt}
      appliedTrustedBranch={appliedTrustedBranch}
      editTarget={editTarget}
      writeSuspended={switchPending}
      onEditTargetChange={handleEditTargetChange}
      queryRows={queryRows}
      saveRow={saveRow}
    />
    {switchConfirming ? <Modal
      open
      title='可信范围已变化'
      okText='放弃并切换'
      cancelText='取消'
      onOk={completeSwitch}
      onCancel={() => setDismissedTrustedBranch(trustedBranch)}
    >
      <p>可信范围已变化，切换范围将放弃未保存的金额输入。</p>
    </Modal> : null}
  </>
}
