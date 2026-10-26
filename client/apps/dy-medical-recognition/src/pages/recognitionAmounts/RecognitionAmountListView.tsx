/**
 * 两个金额页共用的列表视图组件（S3-D29）：「互认项目金额维护」平台管理员页与「本院区互认项目金额」
 * 医院管理员页的界面实现集中在本文件，两个页面只保留各自的数据接线外壳。
 *
 * 集中承载的内容：页头（标题、副标题与可选右侧插槽）、范围选择器插槽、写后刷新门禁横幅、
 * 查询区（关键字框＋框内查询图标按钮＋金额状态下拉）、待切换提示态插槽、互认配置状态页签
 * （页签行右侧以可选承载放置导出按钮，传入导出参数才渲染）、
 * 四栏层级列表（列集合、列宽、样式覆写与页脚完全一致）、金额行内编辑器、页脚计数、四态空态
 * （范围未解析、读取失败、条件未命中、当前范围零配置，互不混用），
 * 以及关键字与状态筛选、读取与提交状态机、未保存输入的清空时机与写门控。界面口径变化只修改
 * 本文件一处，两个页面不再分叉。
 *
 * 两页差异以属性承接：数据来源（`queryRows` 与 `saveRow`，两页请求形状不同）、在当前范围上叠加
 * 已生效关键字的查询构造出口（`buildQuery`）、页头右侧插槽（`headerExtra`，医院管理员页传当前登录
 * 范围条）、范围选择器插槽（`selector`，平台管理员页传受控选择器区块）、范围变化防线
 * （`scopePrompt` 与 `writeSuspended`，平台管理员页的两道防线；医院管理员页传空实现）与写前复核
 * （`preSubmitBlockReason`，两页各自的比对与拦截提示）。
 */
import { Alert, Button, Empty, Input, message, Select, Table, Tabs, Tag, Tooltip, Typography, type TableColumnsType } from 'antd'
import { CheckOutlined, CloseOutlined, SearchOutlined } from '@ant-design/icons'
import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type ReactNode } from 'react'
import { configurationStatusTagColor } from '../recognitionProjects/recognitionProjectsApi'
import {
  AMOUNT_LOAD_FAILURE_TEXT,
  AMOUNT_STATUS_OPTIONS,
  AMOUNT_VALIDATION_MESSAGE,
  CONFIGURATION_STATUS_OPTIONS,
  EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT,
  EMPTY_LOAD_FAILED_RECOGNITION_AMOUNT_TEXT,
  EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT,
  RECOGNITION_AMOUNT_COUNT_PLACEHOLDER,
  currentAmountText,
  filterRecognitionAmountRows,
  parseRecognitionAmount,
  readAmountFilterFromLocation,
  syncAmountFilterToLocation,
  toRecognitionAmountColumnView,
  type AmountConfigurationStatusFilter,
  type AmountStatusFilter,
  type RecognitionAmountColumnProjectRow,
  type RecognitionAmountRow,
} from './recognitionAmountsApi'
import './RecognitionAmounts.css'

/**
 * 列表读取失败态。`refreshAfterWrite` 表示写入**已经成功**、随后的重载失败：
 * 两种失败都保留已有数据并禁用写入，但只有 `refreshAfterWrite` 渲染「写入已成功，数据待刷新」
 * 门禁横幅；纯读取失败（`load`）不渲染横幅，错误由宿主统一提示，重试经框内查询图标按钮。
 */
type AmountLoadFailure = null | 'load' | 'refreshAfterWrite'

/** 金额必填提示；格式判据本身取适配层出口，组件不另写金额规则。 */
const AMOUNT_REQUIRED_MESSAGE = '金额必填'

/** 金额保存成功提示：写失败与读取失败都由宿主统一提示，这里只反馈「前端已确认的成功」。 */
const AMOUNT_SAVE_SUCCESS_MESSAGE = '金额保存成功'

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

/**
 * 导出承载（可选）：传入才渲染导出按钮，两个金额页共用一处实现；不传就不渲染
 * （不出现无能力的死按钮）。门控分工：范围未解析（组织、医院、院区任一缺失）与读取失败或
 * 写入后刷新失败由组件按自身状态禁用、零请求；调用方判定的额外禁用（平台页「可信范围已变化
 * 且页面尚未应用」的待切换提示态）经 `disabled` 传入；导出在途的 loading 形态与防重复点击由
 * `exporting` 承载。点击把**已生效关键字**交回调用方，请求编排在页面层完成（点击时刻的范围
 * 复核由页面保证）。
 */
export interface RecognitionAmountExportBinding {
  /** 导出是否在途：按钮进入 loading 形态并禁用，防重复点击。 */
  exporting: boolean
  /** 调用方判定的额外禁用：平台页在「可信范围已变化且页面尚未应用」时禁用、零请求。 */
  disabled?: boolean
  /** 点击导出：组件把已生效关键字交给调用方；请求编排在页面层完成。 */
  onExport: (keywordApplied: string) => void
}

export interface RecognitionAmountListViewProps<Query> {
  title: string
  subtitle: string
  /** 范围尚不可用时的空态文案：平台管理员页是「未选全范围」，医院管理员页是「登录身份未携带院区」。 */
  missingScopeText: string
  /** 页头右侧插槽：医院管理员页传当前登录范围条，平台管理员页不传。 */
  headerExtra?: ReactNode
  /** 范围选择器插槽：平台管理员页传受控选择器区块，医院管理员页不传。 */
  selector?: ReactNode
  /** 待切换提示态节点（平台管理员页 C33）；不存在待切换范围时为 `null`。 */
  scopePrompt?: ReactNode
  /** `null` 表示范围未解析：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: RecognitionAmountsScope<Query> | null
  /** 行内编辑目标行的项目编码；未保存内容的有无由页面统一裁定。 */
  editTarget: string | null
  /** 换绑挂起中（平台管理员页的确认框或待切换提示态）：期间入口禁用、回车零写返回，行内输入仍可编辑。 */
  writeSuspended?: boolean
  /** 导出承载：传入才渲染导出按钮（页签行右侧，与互认项目页同款式），不传不渲染。 */
  exportBinding?: RecognitionAmountExportBinding
  /** 进入或退出行内编辑；目标行离开当前行集时由本组件同步清空。 */
  onEditTargetChange: (standardProjectCode: string | null) => void
  /**
   * 在当前范围查询上叠加已生效关键字的查询构造出口（两页的请求形状不同）；构造不合法时返回 `null`。
   * 调用方必须传稳定引用：本组件以它的返回值身份驱动读取效果，逐次渲染新建闭包会重复发起读取。
   */
  buildQuery: (query: Query, keywordApplied: string) => Query | null
  queryRows: (query: Query) => Promise<RecognitionAmountRow[]>
  /** 保存当前行的金额；返回 `false` 表示**未发出写请求**（载荷不合法），组件据此保留编辑态与输入。 */
  saveRow: (query: Query, standardProjectCode: string, amountInput: string) => Promise<boolean>
  /**
   * 写前复核（两页各自的口径）：返回拦截原因时**零写请求**并以 `message.error` 提示该原因，
   * 返回 `null` 表示放行。平台管理员页比对可信院区记录值与现读值，医院管理员页现读可信组织或医院。
   */
  preSubmitBlockReason: () => string | null
}

/**
 * 页头标题块（共用）：大标题、副标题与可选的页头右侧插槽。列表视图的页头与两个页面阻断态
 * 面板的标题都经本组件渲染，`recognition-amounts-header` + `Typography.Title level={3}` +
 * `Typography.Text type='secondary'` 的结构与配套 CSS 只有这一处。
 */
export function RecognitionAmountsHeader({ title, subtitle, extra }: {
  title: string
  subtitle: string
  /** 页头右侧插槽：医院管理员页传当前登录范围条；平台管理员页与阻断态不传。 */
  extra?: ReactNode
}) {
  return <div className='recognition-amounts-header'>
    <div>
      <Typography.Title level={3}>{title}</Typography.Title>
      <Typography.Text type='secondary'>{subtitle}</Typography.Text>
    </div>
    {extra}
  </div>
}

/**
 * 两个金额页共用的列表视图：范围状态机、关键字与状态筛选、四栏层级列表、金额行内编辑。
 *
 * 读取时机只有三类：范围变化、关键字提交（含输入框清空与空条件提交）与读取失败后的重试；
 * 两个状态筛选只在服务端交付的结果上本地裁剪，不参与读取时机，也不下推请求。
 */
export function RecognitionAmountListView<Query>({
  title,
  subtitle,
  missingScopeText,
  headerExtra,
  selector,
  scopePrompt,
  scope,
  editTarget,
  writeSuspended = false,
  exportBinding,
  onEditTargetChange,
  buildQuery,
  queryRows,
  saveRow,
  preSubmitBlockReason,
}: RecognitionAmountListViewProps<Query>) {
  const scopeKey = scope?.key ?? null
  const [data, setData] = useState<ScopeData>({ key: scopeKey, rows: [], failure: null })
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  /**
   * message 走 `useMessage()` context 实例：前端可预判的输入问题（本地校验失败、
   * 写前复核拦截）与操作成功经它提示，不用 antd 静态方法；接口错误仍由宿主统一提示，
   * message 不承载。holder 挂在列表视图根部 JSX。
   */
  const [messageApi, messageContextHolder] = message.useMessage()

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
    return buildQuery(scope.query, keywordApplied)
  }, [buildQuery, keywordApplied, scope])

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

  /**
   * 导出按钮禁用只取与既有写门控同口径的状态（仅在传入导出承载时有意义）：
   * 范围未解析（组织、医院、院区任一缺失）与读取失败或写入后刷新失败（页面数据不可靠，
   * 恢复以重试为先）两态禁用、零请求；调用方传入的额外禁用（平台页「可信范围已变化且页面
   * 尚未应用」的待切换提示态）同样零请求。列表读取在途与行内编辑进行中**不**限制导出
   * （导出是只读链，不产生写冲突，既有写门控不因导出改变）；导出在途的禁用与 loading
   * 形态由按钮属性共同承载。
   */
  const exportDisabled = exportBinding !== undefined
    && (scope === null || loadFailure !== null || exportBinding.disabled === true)

  /** 写后刷新门禁的重试入口：只服务 `refreshAfterWrite` 态；纯读取失败的重试经框内查询图标按钮。 */
  const retry = () => {
    if (appliedQuery === null || loadFailure !== 'refreshAfterWrite') return
    void load(appliedQuery, scopeKey, 'refreshAfterWrite')
  }

  /**
   * 提交一笔金额：换绑挂起中（确认框打开或待切换提示态）零写返回（平台管理员页 C33 的写门控，
   * 第一道）；随后发起调用方给定的写前复核（平台管理员页第二道现读比对的接入点）——复核返回原因
   * 即零写请求并以 `message.error` 就近提示；随后由适配层构造载荷（范围或金额不合法同样零写请求），
   * 成功后经 message.success 反馈并收起编辑态、整表重载；写失败保留编辑态与输入、不重载也不误报
   * 成功（拒绝文案由宿主统一展示）。提交期同步去重，防止连续回车产生多次写请求。
   */
  const submitAmount = async (standardProjectCode: string, amountInput: string) => {
    if (writeSuspended) return
    if (appliedQuery === null || scope === null) return
    if (submittingRef.current) return
    const blockReason = preSubmitBlockReason()
    if (blockReason !== null) {
      messageApi.error(blockReason)
      return
    }
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
    // 保存成功的反馈在重载之前给出：即使随后的整表重载失败，「写入已成功」也有可见结论。
    messageApi.success(AMOUNT_SAVE_SUCCESS_MESSAGE)
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
      width: 88,
      // 层级格纵向跨其名下全部行；非首行的层级格按 rowSpan 0 跳过渲染。
      onCell: (row) => ({ rowSpan: row.typeFirst ? row.typeRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.typeText,
    },
    {
      title: '分类',
      width: 186,
      onCell: (row) => ({ rowSpan: row.categoryFirst ? row.categoryRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.categoryName ?? '—',
    },
    {
      title: '分组',
      width: 208,
      onCell: (row) => ({ rowSpan: row.groupFirst ? row.groupRowSpan : 0, className: 'recognition-amounts-level-cell' }),
      render: (_, row) => row.groupName ?? '—',
    },
    {
      // 项目列弹性填充（S3-D24 2026-10-13 修订，与「互认项目」页同一列宽口径），不设固定宽度。
      title: '项目',
      render: (_, row) => (
        <span className='recognition-amounts-project'>
          <span className='recognition-amounts-project-code'>{row.project.standardProjectCode ?? '—'}</span>
          <span className='recognition-amounts-project-name'>{row.project.standardProjectName ?? '—'}</span>
        </span>
      ),
    },
    {
      title: '当前金额',
      // 190px：编辑态「输入框＋两个图标按钮」下输入框能完整显示 8 位整数加两位小数的金额。
      width: 190,
      // 未配置与已配置的零元都走适配层的金额出口：「未配置」与 0.00 必须可区分；
      // 每行金额即行内编辑入口（C32），编辑态以当前行金额值为键重新挂载并回填新值。
      render: (_, row) => {
        const standardProjectCode = row.project.standardProjectCode
        if (standardProjectCode !== null && standardProjectCode === editTarget) {
          return <AmountInlineEditor
            key={`${standardProjectCode}|${currentAmountText(row.project)}`}
            name={row.project.standardProjectName ?? standardProjectCode}
            defaultValue={row.project.isAmountConfigured ? currentAmountText(row.project) : ''}
            disabled={!canWrite}
            submitting={submitting}
            onSubmit={(value) => { void submitAmount(standardProjectCode, value) }}
            onCancel={() => onEditTargetChange(null)}
          />
        }
        // 非编辑态是可点的值按钮（S3-D27 2026-10-12 修订，与「互认项目」页可互认时间值按钮同款），
        // 不再是链接按钮；悬停显现边框与底色，禁用态降灰。
        return <button
          type='button'
          className='recognition-amounts-number recognition-amounts-amount-value'
          aria-label={`修改金额：${row.project.standardProjectName ?? '—'}`}
          disabled={!canWrite || writeSuspended || standardProjectCode === null}
          onClick={() => {
            if (standardProjectCode === null) return
            onEditTargetChange(standardProjectCode)
          }}
        >{currentAmountText(row.project)}</button>
      },
    },
    // 徽标颜色取「互认项目」页同一推导出口（S3-D24 2026-10-13 修订）：启用 success、停用默认、未知 warning。
    { title: '互认配置状态', width: 130, render: (_, row) => <Tag color={configurationStatusTagColor(row.project.configurationStatus)}>{row.project.configurationStatusText}</Tag> },
    // 目录三层启用时服务端返回 null：此时不显示原因，不占用该列。
    { title: '当前不可用原因', width: 190, ellipsis: true, render: (_, row) => row.project.unavailableReason },
  ]

  /** 生效条件：关键字或任一状态档非默认即视为有条件，用于区分零配置与未命中两种空态。 */
  const hasConditions = keywordApplied !== '' || amountStatus !== 'all' || configurationStatus !== 'all'
  /**
   * 读取失败且一行数据都没有：此时**不知道**当前范围有没有配置，与「零配置」「未命中」是三件
   * 不同的事实。空态文案与页脚计数都必须按这一态改写，否则读取失败会被伪装成零配置空态
   * （令牌获取或请求发出阶段失败时后端根本没被问到）。失败态的重试沿用既有入口，不新增交互。
   */
  const loadFailedWithoutRows = loadFailure !== null && rows.length === 0
  const emptyDescription = () => {
    if (scope === null) return missingScopeText
    // 范围已解析、读取失败且无数据：先说读取失败，不得说「没有…配置」。
    if (loadFailedWithoutRows) return EMPTY_LOAD_FAILED_RECOGNITION_AMOUNT_TEXT
    if (hasConditions) return EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT
    return EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT
  }

  return <div className='recognition-amounts-page'>
    {/* message context 实例的 holder 挂列表视图根部：局部校验与保存结果经它提示，不用静态方法。 */}
    {messageContextHolder}
    <RecognitionAmountsHeader title={title} subtitle={subtitle} extra={headerExtra} />

    {selector}

    {/*
      写后刷新门禁横幅：写入已成功、随后的整表重载失败时呈现，承载维护操作暂不可用
      的门禁状态与恢复入口。纯读取失败不渲染横幅：静默保留上一次成功数据，错误由宿主统一
      提示，重试经框内查询图标按钮。
    */}
    {loadFailure !== 'refreshAfterWrite' ? null : <Alert
      type='warning'
      showIcon
      className='recognition-amounts-failure'
      title={AMOUNT_LOAD_FAILURE_TEXT.refreshTitle}
      description={AMOUNT_LOAD_FAILURE_TEXT.refreshDescription}
      action={<Button size='small' disabled={loading} onClick={retry}>重试</Button>}
    />}

    {/*
      查询区（S3-D25 2026-10-13 修订）：关键字输入框 + 金额状态下拉选择，单行承载、超宽自动换行；
      控件由本组件状态受控。关键字沿用草稿—提交模型：框内查询图标按钮
      逐字符合规格（`type="text"` + `SearchOutlined` + `aria-label="查询"` + `autoInsertSpace={false}`，
      `SearchOutlined` 自带英文 `aria-label`，不声明就会参与按钮名称计算），点击图标或输入框内
      回车进入同一条 `submitKeyword` 提交链路；读取在途时图标按钮进入 `loading`。
      金额状态下拉选择是即时生效控件、不进草稿，切换即生效、本地裁剪、不触发读取；
      两个页面都不设独立查询主按钮与「重置」按钮。
    */}
    <div className='recognition-amounts-filter'>
      <Input
        className='recognition-amounts-keyword'
        aria-label='关键字筛选'
        placeholder='标准项目编码或名称'
        allowClear
        value={keywordDraft}
        onChange={handleKeywordChange}
        onPressEnter={submitKeyword}
        suffix={<Button
          type='text'
          size='small'
          icon={<SearchOutlined />}
          aria-label='查询'
          autoInsertSpace={false}
          loading={loading}
          onClick={submitKeyword}
        />}
      />
      <span className='recognition-amounts-filter-group'>
        <span className='recognition-amounts-filter-label'>金额状态</span>
        <Select<AmountStatusFilter>
          aria-label='金额状态筛选'
          value={amountStatus}
          options={AMOUNT_STATUS_OPTIONS}
          onChange={setAmountStatus}
        />
      </span>
    </div>

    {scopePrompt}

    {/*
      互认配置状态页签（S3-D25 2026-10-13 修订）：贴在列表上沿的视图切换控件——
      切换即生效、只在服务端结果上本地裁剪、不触发读取；切换时收起进行中的行内编辑并放弃
      未保存输入（与「互认项目」页切换页签清编辑态同一口径）。
      页签行右侧承载导出入口（三页统一承载位置，与互认项目页同款式）：与页签同一工具行、
      随 DOM 顺序在页签之后（键盘焦点顺序随之）；门控与在途形态见 `exportDisabled` 与
      `exportBinding`。未传导出参数（页面不提供导出能力）时按钮不渲染，不留死按钮。
    */}
    <div className='recognition-amounts-status-toolbar'>
      <div role='group' aria-label='配置状态筛选'>
        <Tabs
          className='recognition-amounts-status-tabs'
          activeKey={configurationStatus}
          items={CONFIGURATION_STATUS_OPTIONS.map((option) => ({ key: option.value, label: option.label }))}
          onChange={(key) => {
            const option = CONFIGURATION_STATUS_OPTIONS.find((item) => item.value === key)
            if (!option) return
            onEditTargetChange(null)
            setConfigurationStatus(option.value)
          }}
        />
      </div>
      {exportBinding === undefined ? null : <Button
        className='recognition-amounts-export'
        aria-label='导出互认项目金额'
        disabled={exportDisabled || exportBinding.exporting}
        loading={exportBinding.exporting}
        onClick={() => exportBinding.onExport(keywordApplied)}
      >导出</Button>}
    </div>

    <Table
      className='recognition-amounts-table'
      rowKey='rowIdentity'
      columns={columns}
      dataSource={tableRows}
      loading={loading && tableRows.length === 0}
      scroll={{ x: 1252 }}
      pagination={false}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          // 范围未解析、读取失败、条件未命中与范围零配置是四件不同的事实，空态文案必须区分。
          description={emptyDescription()}
        />,
      }}
    />

    {/* 页脚计数：读取失败且无数据时不为 0（失败数据不可信，「共 0 项」会被读成零配置），
        以占位符呈现；成功读取仍按可见行数计数，两个页面共用同一口径。 */}
    <div className='recognition-amounts-count'>
      {loadFailedWithoutRows ? `共 ${RECOGNITION_AMOUNT_COUNT_PLACEHOLDER} 项` : `共 ${tableRows.length} 项`}
    </div>
  </div>
}

/**
 * 金额行内编辑器（C32，S3-D27）：编辑态结构与「互认项目」页可互认时间编辑器同渊源——
 * 「输入框＋保存＋取消」、回车与保存按钮走同一条提交路径、Esc 与取消按钮都只放弃本次编辑。
 * S3-D27 2026-10-13 修订后本编辑器的保存与取消为图标按钮（`CheckOutlined` / `CloseOutlined`，
 * 可访问名与悬停提示同为「保存金额：…」「取消修改金额：…」；disabled 按钮不派发悬停事件，
 * 两个按钮按 antd 官方模式以 span 包裹，禁用态的悬停提示由 span 承接）；「互认项目」页
 * 可互认时间编辑器同日修订为同一图标款式，两页行内编辑器款式一致。
 * 进入编辑即回填当前行金额值。金额判据只取适配层出口 `parseRecognitionAmount`，校验失败在编辑器内
 * 就地提示（常驻 live region 承载 Alert），零写请求、输入保留，`aria-invalid` 随每次输入重新求值。
 * 调用方以「行 + 当前金额值」为键挂载本组件：后台刷新交付新值时组件重挂并回填新值。
 */
function AmountInlineEditor({ name, defaultValue, disabled, submitting, onSubmit, onCancel }: {
  /** 行名称；编辑控件的可访问名称由它组成，与行上的金额入口指向同一行。 */
  name: string
  /** 回填值：已配置金额的展示文本，未配置时为空串。 */
  defaultValue: string
  /** 写门控：读取在途、失败或范围不完整时不可编辑。 */
  disabled: boolean
  /** 在途提交：输入暂时禁用，提交结束（无论成败）后恢复。 */
  submitting: boolean
  /** 回车与保存按钮提交；拒绝不在此处捕获，错误由宿主统一展示（C10）。 */
  onSubmit: (value: string) => void
  /** Esc 与取消按钮：收起编辑态并丢弃输入。 */
  onCancel: () => void
}) {
  const [value, setValue] = useState(defaultValue)
  /** 本次输入被拒绝的原因；`null` 表示尚未被拒绝。 */
  const [error, setError] = useState<string | null>(null)
  const trimmed = value.trim()
  const invalid = trimmed === '' || parseRecognitionAmount(value) === null

  /** 回车与保存按钮共用的提交路径：本地校验不通过就地提示、零写请求、输入保留。 */
  const submit = () => {
    // 写门控禁用期间不产生任何提交（界面已禁用输入与保存按钮，这里兜住残余键盘路径）。
    if (disabled) return
    if (trimmed.length === 0) {
      setError(AMOUNT_REQUIRED_MESSAGE)
      return
    }
    if (invalid) {
      setError(AMOUNT_VALIDATION_MESSAGE)
      return
    }
    setError(null)
    onSubmit(value)
  }

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    setValue(event.target.value)
    setError(null)
  }

  return <>
    <span className='recognition-amounts-editor'>
      <Input
        className='recognition-amounts-editor-input'
        aria-label='金额'
        autoFocus
        disabled={disabled || submitting}
        value={value}
        status={error === null ? undefined : 'error'}
        aria-invalid={invalid}
        onChange={handleChange}
        onPressEnter={submit}
        onKeyDown={(event) => {
          if (event.key !== 'Escape') return
          event.preventDefault()
          onCancel()
        }}
      />
      {/* disabled 按钮不派发悬停事件，Tooltip 以 span 为直接子元素承接悬停（antd 官方模式），
          禁用与否结构恒定，悬停提示在写门控与提交期间同样出现。 */}
      <Tooltip title={`保存金额：${name}`}>
        <span>
          <Button
            type='primary'
            size='small'
            icon={<CheckOutlined />}
            aria-label={`保存金额：${name}`}
            disabled={disabled}
            loading={submitting}
            onClick={submit}
          />
        </span>
      </Tooltip>
      <Tooltip title={`取消修改金额：${name}`}>
        <span>
          <Button
            size='small'
            icon={<CloseOutlined />}
            aria-label={`取消修改金额：${name}`}
            disabled={submitting}
            onClick={onCancel}
          />
        </span>
      </Tooltip>
    </span>
    {/* 异步提示放在常驻的 live region 里：容器先于内容存在，读屏软件才收得到这次变化。 */}
    <div aria-live='polite'>
      {error === null ? null : <Alert
        className='recognition-amounts-editor-error'
        type='warning'
        showIcon
        title='金额输入不合法'
        description={error}
      />}
    </div>
  </>
}
