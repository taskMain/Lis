/**
 * 「本院区互认项目金额」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-amounts`）。
 *
 * 组织与医院取可信上下文并固定展示；院区恒取 `LoginUserManager.getBranchID()` 的登录院区（D-05），
 * 不提供院区选择，也不做清单核对、首项回退等三档解析。查询与保存请求的院区参数恒为该登录院区，
 * 可信院区缺失属权限配置错误：页面不兜底、不本地补默认院区，适配层拒绝构造请求即零业务请求，
 * 页面以空态呈现，恢复正确的权限配置后按登录院区重读。
 * 查询只携带院区与可选关键字，保存载荷只含院区、标准项目编码与金额（组织与医院由服务端从可信上下文
 * 注入）。可信组织或医院缺失时整页阻断：统一接入阻断面板（整页只渲染标题与错误 Alert），不发任何
 * 业务请求，也不伪造组织或降级为空值。页头范围区由共用范围条承载：单行只显「组织 / 医院 / 院区」
 * 名称，无编码。筛选区、四栏层级列视图、空态与页脚计数与平台管理员页同款式。页面不实现角色判断、
 * 不做权限二次确认，也不下推枚举筛选。
 */
import { Alert, Button, Empty, Form, Input, message, Segmented, Space, Table, Tag, Typography, type TableColumnsType } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type KeyboardEvent, type ReactNode } from 'react'
import { LoginUserManager } from '@dy/auth'
import { useAuth } from '@dy/auth-react'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  AMOUNT_LOAD_FAILURE_TEXT,
  AMOUNT_STATUS_OPTIONS,
  AMOUNT_VALIDATION_MESSAGE,
  CONFIGURATION_STATUS_OPTIONS,
  EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT,
  EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT,
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  currentAmountText,
  filterRecognitionAmountRows,
  parseRecognitionAmount,
  queryBranchRecognitionAmountRows,
  readAmountFilterFromLocation,
  saveBranchRecognitionAmount,
  syncAmountFilterToLocation,
  toRecognitionAmountColumnView,
  type AmountConfigurationStatusFilter,
  type AmountStatusFilter,
  type BranchAmountListQuery,
  type RecognitionAmountColumnProjectRow,
  type RecognitionAmountRow,
} from './recognitionAmountsApi'
import type { AmountLoadFailure, RecognitionAmountsScope } from './RecognitionAmounts'
import './RecognitionAmounts.css'

/** 可信编码判定：缺失或纯空白一律视为不可用，不伪造、不静默使用空串。 */
function readTrustedCode(value: string | null): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

interface TrustedBranchScope {
  organizationCode: string | null
  hospitalCode: string | null
  branchCode: string | null
}

/**
 * 「可信当前范围」的唯一接缝：组织、医院、院区都取自 `@dy/auth` 的 `LoginUserManager`，
 * token 变化即重新求值（与既有的可信组织接缝同一口径）。页面不持有可选组织集合、不提供自由输入，
 * 也不从 URL、Storage、表单或列表构造范围。
 */
function useTrustedBranchScope(): TrustedBranchScope {
  const { token } = useAuth()
  // token 缺失时凭据不表达可信组织与医院，直接判为不可用。
  if (!token) return { organizationCode: null, hospitalCode: null, branchCode: null }
  return {
    organizationCode: readTrustedCode(LoginUserManager.getOrgID()),
    hospitalCode: readTrustedCode(LoginUserManager.getHosID()),
    branchCode: readTrustedCode(LoginUserManager.getBranchID()),
  }
}

/** 金额必填文案：行内编辑器的空值提示与「零写请求」同一口径（非法金额提示取适配层常量）。 */
const AMOUNT_REQUIRED_MESSAGE = '金额必填'

/** 金额保存成功提示（TC-T02-07）：写失败与读取失败都由宿主统一提示，这里只反馈「前端已确认的成功」。 */
const AMOUNT_SAVE_SUCCESS_MESSAGE = '金额保存成功'

/** C34 提交拦截提示（message.error 承载，TC-T02-07）：现读可信组织或医院有一层读不到，零写请求
 * （S3-D28「现读不到」仅指这两层；D-05 后院区恒随登录院区，不再参与提交前比对）。 */
const TRUSTED_SCOPE_UNREADABLE_REASON = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

/** 范围相关状态整体按范围键存放（与平台管理员页同一形状）：渲染只取当前范围键的数据。 */
interface BranchScopeData {
  key: string | null
  rows: RecognitionAmountRow[]
  failure: AmountLoadFailure
}

/** 平铺表格行：项目字段加上类型、分类、分组三个层级格的跨行信息（与平台管理员页同一形状）。 */
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

interface BranchRecognitionAmountsBoardProps {
  title: string
  subtitle: string
  /** 范围不可用（登录院区缺失，权限配置错误）时的空态文案。 */
  missingScopeText: string
  /** 页头右侧的三级范围展示节点；由共用范围条承载。 */
  scopeDisplay: ReactNode
  /** `null` 表示范围不可用：不发起请求、不进入可操作状态，也不伪造院区。 */
  scope: RecognitionAmountsScope<BranchAmountListQuery> | null
  /** 行内编辑目标行的项目编码；未保存内容的有无由页面统一裁定（C32）。 */
  editTarget: string | null
  /** 进入或退出行内编辑；目标行离开当前行集时由主体同步清空。 */
  onEditTargetChange: (standardProjectCode: string | null) => void
  queryRows: (query: BranchAmountListQuery) => Promise<RecognitionAmountRow[]>
  /** 保存当前行的金额；返回 `false` 表示**未发出写请求**（载荷不合法），主体据此保留编辑态与输入。 */
  saveRow: (query: BranchAmountListQuery, standardProjectCode: string, amountInput: string) => Promise<boolean>
}

/**
 * 医院管理员页的列表主体：范围状态机、关键字与状态筛选、四栏层级列表、金额行内编辑。
 *
 * 与平台管理员页的 `RecognitionAmountsPlatformBoard` 同款式：读取时机只有三类——范围变化、
 * 关键字提交（含输入框清空与空条件提交）与失败后的重试；两个状态 Segmented 只在服务端交付的
 * 结果上本地裁剪，不参与读取时机，也不下推请求。区别只在请求内容：本主体只携带登录院区与可选关键字。
 */
function BranchRecognitionAmountsBoard({
  title,
  subtitle,
  missingScopeText,
  scopeDisplay,
  scope,
  editTarget,
  onEditTargetChange,
  queryRows,
  saveRow,
}: BranchRecognitionAmountsBoardProps) {
  const scopeKey = scope?.key ?? null
  const [data, setData] = useState<BranchScopeData>({ key: scopeKey, rows: [], failure: null })
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  /**
   * message 走 `useMessage()` context 实例（TC-T02-07）：前端可预判的输入问题（本地校验失败、
   * 提交前复核失败）与操作成功经它提示，不用 antd 静态方法；接口错误仍由宿主统一提示，
   * message 不承载。holder 挂在页面根部 JSX。
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
  const load = useCallback(async (query: BranchAmountListQuery, requestScopeKey: string | null, failureKind: Exclude<AmountLoadFailure, null>) => {
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

  /**
   * 重置（TC-T02-16）：草稿与已生效条件一并恢复默认（关键字清空、两个状态档回「全部」），
   * 并按默认条件重读一次——重置口径与报告、统计看板页一致，条件已等于默认值时重置仍等价于一次刷新
   * （推进号保证重读）。两个状态档是即时生效控件、不进草稿，重置只是把它们恢复默认档，
   * 不改变「切换即生效、本地裁剪、不触发读取」的语义。地址栏经既有同步按默认值回写（三个参数删除）。
   */
  const resetFilters = () => {
    setKeywordDraft('')
    setKeywordApplied('')
    setAmountStatus('all')
    setConfigurationStatus('all')
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
    return buildBranchAmountListQuery({ ...scope.query, keyword: keywordApplied })
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

  /** 写后刷新门禁的重试入口：只服务 `refreshAfterWrite` 态；纯读取失败的重试经查询按钮（D-06）。 */
  const retry = () => {
    if (appliedQuery === null || loadFailure !== 'refreshAfterWrite') return
    void load(appliedQuery, scopeKey, 'refreshAfterWrite')
  }

  /**
   * 提交一笔金额：构造载荷之前先做 C34 提交前现值复核——可信组织或医院现读有一层读不到即零写请求
   * （S3-D28；D-05 后院区恒随登录院区、页面无滞留范围可 dangling，院区层不再参与比对），原因以
   * message.error 就近提示（TC-T02-07，单元格内不再渲染校验原因）；随后由适配层构造载荷（范围或
   * 金额不合法即零写请求），成功后经 message.success 反馈（TC-T02-07）并收起编辑态、整表重载；
   * 写失败保留编辑态与输入、不重载也不误报成功（拒绝文案由宿主统一展示，提交处不捕获拒绝）。
   * 提交期同步去重，防止连续回车产生多次写请求。
   */
  const submitAmount = async (standardProjectCode: string, amountInput: string) => {
    /**
     * C34：在构造载荷之前现读——现读走命令式出口（组织、医院两层），不经渲染期闭包：宿主重新签发
     * token 之后、页面重渲染之前回车提交，渲染期取值可能仍是旧可信范围，只有现读才兜得住。
     */
    if (readTrustedOrganizationCode() === null || readTrustedHospitalCode() === null) {
      messageApi.error(TRUSTED_SCOPE_UNREADABLE_REASON)
      return
    }
    if (appliedQuery === null) return
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
   * 目标行离开当前行集即清空编辑目标：搜索交付、状态档裁剪与范围切换都经过这条同步，
   * 「未保存内容」因此始终等于「编辑中的行仍在当前行集」（C32、C33 的口径）。
   */
  useEffect(() => {
    if (editTarget === null) return
    if (visibleRows.some((candidate) => candidate.standardProjectCode === editTarget)) return
    onEditTargetChange(null)
  }, [editTarget, onEditTargetChange, visibleRows])

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
            // 本地校验失败经 message.error 反馈（TC-T02-07），单元格内不再渲染校验红字。
            onValidationError={(text) => messageApi.error(text)}
            onSubmit={(value) => { void submitAmount(standardProjectCode, value) }}
            onCancel={() => onEditTargetChange(null)}
          />
        }
        return <Button
          type='link'
          size='small'
          className='recognition-amounts-number'
          disabled={!canWrite || standardProjectCode === null}
          onClick={() => {
            if (standardProjectCode === null) return
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
    {/* message context 实例的 holder 挂页面根部（TC-T02-07）：局部校验与保存结果经它提示，不用静态方法。 */}
    {messageContextHolder}
    <div className='recognition-amounts-header'>
      <div>
        <Typography.Title level={3}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      {scopeDisplay}
    </div>

    {/*
      写后刷新门禁横幅（TC-T02-12）：写入已成功、随后的整表重载失败时呈现，承载维护操作暂不可用
      的门禁状态与恢复入口。纯读取失败不渲染横幅（D-06）：静默保留上一次成功数据，错误由宿主统一
      提示，重试经查询按钮。
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
      查询区（TC-T02-16）：inline Form 只承载布局与提交语义，控件不注册 name、仍由页面状态受控；
      两个状态 Segmented 是即时生效控件、不进草稿，保持切换即生效、本地裁剪、不触发读取；
      查询与重置位于行内最后一个 Form.Item 内并排（与平台管理员页同一款式）。
    */}
    <div className='recognition-amounts-filter'>
      <Form layout='inline' onFinish={submitKeyword}>
        <Form.Item>
          <Input
            className='recognition-amounts-keyword'
            aria-label='关键字'
            placeholder='标准项目编码或名称'
            allowClear
            value={keywordDraft}
            onChange={handleKeywordChange}
          />
        </Form.Item>
        <Form.Item>
          <Segmented<AmountStatusFilter>
            aria-label='金额状态筛选'
            value={amountStatus}
            options={AMOUNT_STATUS_OPTIONS}
            onChange={setAmountStatus}
          />
        </Form.Item>
        <Form.Item>
          <Segmented<AmountConfigurationStatusFilter>
            aria-label='互认配置状态筛选'
            value={configurationStatus}
            options={CONFIGURATION_STATUS_OPTIONS}
            onChange={setConfigurationStatus}
          />
        </Form.Item>
        <Form.Item>
          <Space wrap>
            {/*
              查询按钮逐字符合全站统一规格（TC-T02-06/16）：`aria-label` 声明可访问名称——`SearchOutlined`
              自带英文 `aria-label`，会参与按钮名称计算，不声明就会读成「search 查询」；`autoInsertSpace={false}`
              关闭 antd 对两个汉字之间自动插入的空格，使按钮文本与可访问名称都是「查询」；颜色继承宿主
              主题 token，不覆盖样式。`htmlType='submit'` 使提交经查询区 Form 的 onFinish 进入
              `submitKeyword`，输入框内回车由表单隐式提交走同一条链路。提交触发重新读取，按钮在读取在途时
              进入 `loading`。重置恒命名「重置」、默认样式（无 type）。
            */}
            <Button
              type='primary'
              icon={<SearchOutlined />}
              aria-label='查询'
              autoInsertSpace={false}
              htmlType='submit'
              loading={loading}
            >查询</Button>
            <Button autoInsertSpace={false} onClick={resetFilters}>重置</Button>
          </Space>
        </Form.Item>
      </Form>
    </div>

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
 * 金额取值只用适配层出口 `parseRecognitionAmount`，校验失败经 `onValidationError` 以 message.error
 * 反馈（TC-T02-07，单元格内不再渲染校验红字），`aria-invalid` 随每次输入重新求值。
 * 调用方以「行 + 当前金额值」为键挂载本组件：后台刷新交付新值时组件重挂并回填新值。
 */
function AmountInlineEditor({ defaultValue, disabled, submitting, onValidationError, onSubmit, onCancel }: {
  /** 回填值：已配置金额的展示文本，未配置时为空串。 */
  defaultValue: string
  /** 写门控：读取在途、失败或范围不完整时不可编辑。 */
  disabled: boolean
  /** 在途提交：输入暂时禁用，提交结束（无论成败）后恢复。 */
  submitting: boolean
  /** 本地校验失败（金额必填、格式不合法）的提示出口；由页面经 message.error 承载（TC-T02-07）。 */
  onValidationError: (text: string) => void
  /** 回车提交；拒绝不在此处捕获，错误由宿主统一展示（C10）。 */
  onSubmit: (value: string) => void
  /** Esc 取消：收起编辑态并丢弃输入。 */
  onCancel: () => void
}) {
  const [value, setValue] = useState(defaultValue)
  const trimmed = value.trim()
  const invalid = trimmed === '' || parseRecognitionAmount(value) === null

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    setValue(event.target.value)
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
      onValidationError(AMOUNT_REQUIRED_MESSAGE)
      return
    }
    if (invalid) {
      onValidationError(AMOUNT_VALIDATION_MESSAGE)
      return
    }
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
  </div>
}

/**
 * 医院管理员页：组织与医院固定来自可信上下文；院区恒取登录院区（D-05），不提供院区选择，
 * 查询与保存只携带登录院区与可选关键字。
 *
 * 院区不兜底、不本地补默认值：可信院区缺失属权限配置错误，适配层拒绝构造请求即零业务请求，
 * 页面以空态呈现，恢复正确的权限配置后按登录院区重读。可信组织或医院缺失时整页阻断，恢复后按
 * 新的可信范围重新取值；可信组织、医院或登录院区任一层变化即视为范围切换（范围键含三层），
 * 主体清空旧行集并按新范围重读，未保存输入随行集替换被清空（C32）——D-05 后不存在用户选定的
 * 滞留范围，不再有切换确认框与待切换提示态。C34 的提交前现值复核只保留可信组织或医院现读不到
 * 的拦截，由列表主体在提交动作发起时现读比对。
 */
export function BranchRecognitionAmounts() {
  const client = useApiClientContext()
  const trusted = useTrustedBranchScope()
  /** 行内编辑目标行的项目编码；列表主体负责在目标行离开当前行集时清空它。 */
  const [editTarget, setEditTarget] = useState<string | null>(null)

  /** 编辑目标改道：目标行离开当前行集时由主体同步清空。 */
  const handleEditTargetChange = useCallback((target: string | null) => {
    setEditTarget(target)
  }, [])

  const queryRows = useCallback(
    (query: BranchAmountListQuery) => queryBranchRecognitionAmountRows(client, query),
    [client],
  )
  const saveRow = useCallback(async (query: BranchAmountListQuery, standardProjectCode: string, amountInput: string) => {
    const payload = buildBranchAmountSavePayload({ branchCode: query.branchCode, amountInput }, standardProjectCode)
    if (payload === null) return false
    // 载荷只含院区、标准项目编码与金额；可信组织与医院不回填，由服务端从可信上下文注入。
    await saveBranchRecognitionAmount(client, payload)
    return true
  }, [client])

  /**
   * 登录院区 → 查询条件（D-05）：院区恒取可信登录院区，组织与医院不进请求，由服务端从可信上下文
   * 注入；请求构造只经适配层出口。范围键含可信三层：任一层变化即视为范围切换，主体清空旧行集重读。
   * 登录院区缺失（权限配置错误）时适配层拒绝构造请求，返回 `null` 即零业务请求、以空态呈现。
   */
  const appliedScope = useMemo<RecognitionAmountsScope<BranchAmountListQuery> | null>(() => {
    const query = buildBranchAmountListQuery({ branchCode: trusted.branchCode ?? '' })
    if (query === null) return null
    return {
      key: `branch|${trusted.organizationCode ?? ''}|${trusted.hospitalCode ?? ''}|${query.branchCode}`,
      query,
    }
  }, [trusted.branchCode, trusted.hospitalCode, trusted.organizationCode])

  if (trusted.organizationCode === null || trusted.hospitalCode === null) {
    return <div className='recognition-amounts-page'>
      <ScopeUnavailablePanel
        title={
          <div className='recognition-amounts-header'>
            <div>
              <Typography.Title level={3}>本院区互认项目金额</Typography.Title>
              <Typography.Text type='secondary'>维护本院区的互认项目金额</Typography.Text>
            </div>
          </div>
        }
      />
    </div>
  }

  return <BranchRecognitionAmountsBoard
    title='本院区互认项目金额'
    subtitle='维护本院区的互认项目金额'
    missingScopeText='当前登录身份未携带院区，无法查询互认项目金额'
    scopeDisplay={<CurrentScopeBar />}
    scope={appliedScope}
    editTarget={editTarget}
    onEditTargetChange={handleEditTargetChange}
    queryRows={queryRows}
    saveRow={saveRow}
  />
}