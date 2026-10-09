/**
 * 「本院区互认项目金额」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-amounts`）。
 *
 * 组织与医院取可信上下文并固定展示（对应级不渲染下拉），只有院区可选。院区由页面**受控**解析：
 * 当前可信范围内尚无取值时，采用选择器按 `selectFirstOption` 交付的首项与选项清单解析默认值——
 * 可信院区属于该院区清单时优先取可信院区，可信院区缺失或不属于该清单时取该院下第一项；组件回调
 * 的空院区一律拒绝并保持原受控值，因此院区恒定单值、永不为空发请求。查询只携带院区与可选关键字，
 * 保存载荷只含院区、标准项目编码与金额（组织与医院由服务端从可信上下文注入）。可信组织或医院缺失时
 * 整页阻断：不渲染选择器、不发任何业务请求，也不伪造组织或降级为空值。页头固定展示可信范围的名称
 * 与编码；筛选区、四栏层级列视图、空态与页脚计数与平台管理员页同款式。页面不实现角色判断、不做
 * 权限二次确认，也不下推枚举筛选。
 */
import { Alert, Button, Empty, Input, Modal, Segmented, Table, Tag, Typography, type TableColumnsType } from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type KeyboardEvent, type ReactNode } from 'react'
import { LoginUserManager } from '@dy/auth'
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

/** 选择器交付后的页面受控状态：所属可信范围键、解析出的院区与页头展示所需的选项清单。 */
interface BranchScopeSelection {
  scopeKey: string
  branchId: string
  meta: {
    organizations: BaseScopeSelectorChangeMeta['organizations']
    hospitals: BaseScopeSelectorChangeMeta['hospitals']
    branches: BaseScopeSelectorChangeMeta['branches']
  }
}

/** 页面应用范围时记录的可信范围（`applySelection` 同一帧写入）：范围键承载组织与医院，`branchCode` 是当时的可信院区。 */
interface RecordedTrustedScope {
  scopeKey: string
  branchCode: string | null
}

/** 金额必填文案：行内编辑器的空值提示与「零写请求」同一口径（非法金额提示取适配层常量）。 */
const AMOUNT_REQUIRED_MESSAGE = '金额必填'

/** C34 行内拦截原因：现读可信组织或医院有一层读不到，零写请求（S3-D28「现读不到」仅指这两层）。 */
const TRUSTED_SCOPE_UNREADABLE_REASON = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

/** C34 行内拦截原因：记录的可信范围与现读不一致、页面尚未应用新可信范围，零写请求（S3-D28）。 */
const TRUSTED_SCOPE_CHANGED_REASON = '可信范围已变化，页面尚未应用新可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

/** 层级取值归一：`null`、空串与纯空白都按缺失（可信院区缺失属 S3-D22 档二正常形态）。 */
function normalizeTrustedLayerValue(value: string | null): string {
  return (value ?? '').trim()
}

/**
 * C34 提交前现值复核（S3-D28）：比对「页面应用范围时记录的可信范围」与「提交发起时现读的可信三层」，
 * 与 C33 的可信范围变化派生取同一组事实——记录侧是 `applySelection` 写下的范围键与可信院区，
 * 现读侧由调用方在提交动作发起时经命令式出口（`readTrustedOrganizationCode`、`readTrustedHospitalCode`、
 * `readTrustedBranchCode`）读入，不经渲染期闭包：宿主重新签发 token 之后、页面重渲染之前回车提交，
 * 渲染期取值可能仍是旧可信范围，只有现读才兜得住。比对口径：可信组织或医院现读为空即「读取不到
 * 完整可信范围」（S3-D28 的「现读不到」仅指这两层）；范围键不一致即已变化；院区层按缺失值参与
 * 比对，记录值与现读值同为缺失即通过，一侧缺失另一侧有值即已变化、页面尚未应用新可信范围。
 * 页面尚未应用过范围时没有可提交的行、不产生原因。返回行内就地展示的中文原因，`null` 表示可以提交。
 */
function compareRecordedTrustedScope(
  recorded: RecordedTrustedScope | null,
  current: TrustedBranchScope,
): string | null {
  if (recorded === null) return null
  const organizationCode = normalizeTrustedLayerValue(current.organizationCode)
  const hospitalCode = normalizeTrustedLayerValue(current.hospitalCode)
  if (organizationCode.length === 0 || hospitalCode.length === 0) {
    return TRUSTED_SCOPE_UNREADABLE_REASON
  }
  if (recorded.scopeKey !== `${organizationCode}|${hospitalCode}`) {
    return TRUSTED_SCOPE_CHANGED_REASON
  }
  // 院区层：两侧同为缺失即放行；一侧缺失另一侧有值同样按不一致给出已变化。
  const recordedBranch = normalizeTrustedLayerValue(recorded.branchCode)
  const currentBranch = normalizeTrustedLayerValue(current.branchCode)
  if (recordedBranch !== currentBranch) {
    return TRUSTED_SCOPE_CHANGED_REASON
  }
  return null
}

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
  /** 范围尚未解析时的空态文案（院区选项尚未交付等场景）。 */
  missingScopeText: string
  /** 范围选择器节点；由调用方以受控值接入，主体只负责渲染，不参与选择器状态。 */
  selector: ReactNode
  /** 页头右侧的三级范围展示节点；范围尚未解析时为 `null`。 */
  scopeDisplay: ReactNode
  /** `null` 表示范围未解析：不发起请求、不进入可操作状态，也不退化为全局查询。 */
  scope: RecognitionAmountsScope<BranchAmountListQuery> | null
  /** 待切换提示态节点（C33）；不存在待切换范围时为 `null`。 */
  scopePrompt: ReactNode
  /** 行内编辑目标行的项目编码；未保存内容的有无由页面统一裁定（C33）。 */
  editTarget: string | null
  /** 换绑挂起中（确认框打开或待切换提示态）：期间入口禁用、回车零写返回，行内输入仍可编辑。 */
  writeSuspended: boolean
  /** C34 的比对记录侧：页面应用范围时记录的可信范围；页面尚未应用过范围时为 `null`。 */
  recordedTrustedScope: RecordedTrustedScope | null
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
 * 结果上本地裁剪，不参与读取时机，也不下推请求。区别只在请求内容：本主体只携带院区与可选关键字。
 */
function BranchRecognitionAmountsBoard({
  title,
  subtitle,
  missingScopeText,
  selector,
  scopeDisplay,
  scope,
  scopePrompt,
  editTarget,
  writeSuspended,
  recordedTrustedScope,
  onEditTargetChange,
  queryRows,
  saveRow,
}: BranchRecognitionAmountsBoardProps) {
  const scopeKey = scope?.key ?? null
  const [data, setData] = useState<BranchScopeData>({ key: scopeKey, rows: [], failure: null })
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  /** C34 提交前现值复核失败的一行原因；每次提交尝试整体替换，进入编辑时清空（与平台管理员页同一位置）。 */
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

  const retry = () => {
    if (appliedQuery === null) return
    void load(appliedQuery, scopeKey, loadFailure ?? 'load')
  }

  /**
   * 提交一笔金额：构造载荷之前先做 C34 提交前现值复核——可信组织医院现读有层读不到、或记录的
   * 可信范围与现读不一致（院区层两侧同为缺失算一致）即零写请求，原因在行内就地展示；复核通过才
   * 走换绑挂起门控（确认框打开或待切换提示态零写返回），随后由适配层构造载荷（范围或金额不合法
   * 即零写请求），成功后才收起编辑态并整表重载；写失败保留编辑态与输入、不重载也不误报成功
   * （拒绝文案由宿主统一展示，提交处不捕获拒绝）。提交期同步去重，防止连续回车产生多次写请求。
   */
  const submitAmount = async (standardProjectCode: string, amountInput: string) => {
    /**
     * C34：在构造载荷之前比对——比对对象是可信范围的变化（记录的可信范围对提交发起时现读的可信
     * 三层）。现读走命令式出口（组织、医院、院区三层），与平台管理员页 `submitAmount` 同一判定
     * 来源：渲染期取值只表达它渲染那一帧读到的范围，宿主重新签发 token 之后、页面重渲染之前回车，
     * 两侧可能已经不是同一取值。
     */
    const recheckFailure = compareRecordedTrustedScope(recordedTrustedScope, {
      organizationCode: readTrustedOrganizationCode(),
      hospitalCode: readTrustedHospitalCode(),
      branchCode: readTrustedBranchCode(),
    })
    if (recheckFailure !== null) {
      setRecheckReason(recheckFailure)
      return
    }
    setRecheckReason(null)
    if (writeSuspended) return
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
 * 金额取值只用适配层出口 `parseRecognitionAmount`，提示就地展示，`aria-invalid`
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
  /** C34 提交前现值复核失败的一行原因；页面在每次提交尝试后整体替换。 */
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

/**
 * 页头三级范围展示：上行名称（正文色、字重稍重）、下行编码（次要色、小一号）整体右对齐，
 * 不设「组织」这类文字标签；选择器尚未交付选项清单时该块不出现，只可能随解析结果展示。
 * 组织固定后组件不再读取组织清单，组织名称即其编码（与真实组件同一交付）。
 */
function buildBranchScopeDisplay(
  organizationCode: string,
  hospitalCode: string,
  selection: BranchScopeSelection | null,
): ReactNode {
  if (selection === null) return null
  const levels = [
    {
      code: organizationCode,
      name: selection.meta.organizations.find((item) => item.id === organizationCode)?.name ?? null,
    },
    {
      code: hospitalCode,
      name: selection.meta.hospitals.find((item) => item.id === hospitalCode)?.name ?? null,
    },
    {
      code: selection.branchId,
      name: selection.meta.branches.find((item) => item.id === selection.branchId)?.name ?? null,
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
 * 医院管理员页：组织与医院固定来自可信上下文，院区恒定单值，只有院区与可选关键字随查询请求提交。
 *
 * 院区取值由页面**受控**解析：`BaseScopeSelector` 以 `orgId`/`hosId` 固定组织与医院（对应级不渲染
 * 下拉）并以 `selectFirstOption` 在院区取值为空时交付首项与选项清单，页面在此之上只做三件事——
 * 空院区的回调保持原受控值（永不为空发请求）、当前可信范围尚无取值时把属于该院区清单的可信院区作为
 * 默认值（可信院区缺失或不属于清单时取首项）、以及从选项清单取名称供页头展示。
 * 可信组织或医院缺失时整页阻断，期间既有选择一并作废，恢复后按新的可信范围重新解析。
 * 行内编辑目标（C32）与可信范围变化确认框（C33）由页面统一裁定：未保存内容的有无等于
 * 「编辑中的行仍在当前行集」；C33 的可信三层变化判定在**渲染期**现读派生，宿主单独重新签发
 * token、用户不碰选择器时同样触发，选择器回调里的现读比对只作第二道。C34 的提交前现值复核与
 * 它取同一组事实（记录的可信范围对现读可信三层），由列表主体在提交动作发起时比对。
 */
export function BranchRecognitionAmounts() {
  const client = useApiClientContext()
  const trusted = useTrustedBranchScope()
  const [selection, setSelection] = useState<BranchScopeSelection | null>(null)
  /** 行内编辑目标行的项目编码；列表主体负责在目标行离开当前行集时清空它。 */
  const [editTarget, setEditTarget] = useState<string | null>(null)
  /** 上次应用范围时记录的可信院区；`undefined` 表示页面尚未应用过一次范围（渲染期比对用，不放 ref）。 */
  const [appliedTrustedBranch, setAppliedTrustedBranch] = useState<string | null | undefined>(undefined)
  /** 取消确认框时针对的可信范围键：与现读可信范围键一致即处于待切换提示态（C33，渲染期派生）。 */
  const [dismissedTrustedRangeKey, setDismissedTrustedRangeKey] = useState<string | null>(null)
  /** 挂起期间选择器回调交付的目标范围：确认或提示态再次确认时按它完成切换。 */
  const [switchTarget, setSwitchTarget] = useState<BranchScopeSelection | null>(null)

  // 可信组织或医院缺失即整页阻断：既有选择与挂起的换绑一并作废，恢复后按新的可信范围重新取值。
  if (trusted.organizationCode === null || trusted.hospitalCode === null) {
    if (selection !== null) setSelection(null)
    if (dismissedTrustedRangeKey !== null) setDismissedTrustedRangeKey(null)
    if (switchTarget !== null) setSwitchTarget(null)
    if (editTarget !== null) setEditTarget(null)
  }

  const trustedScopeKey = `${trusted.organizationCode ?? ''}|${trusted.hospitalCode ?? ''}`
  /** 可信范围键连同可信院区：「取消」针对这一个整体，任一层再变化都重新弹确认框（C33）。 */
  const trustedRangeKey = `${trustedScopeKey}|${trusted.branchCode ?? ''}`

  /**
   * 可信范围变化（C33）的渲染期派生：宿主重新签发 token 后，范围键或现读可信院区已不同于应用
   * 范围时记录的取值，而页面仍展示已应用范围——范围键就是应用范围时记录的组织医院，院区一层比对
   * 应用范围时记录的可信院区。确认框、待切换提示态与写门控都从这一份比对派生，不在 effect 里
   * 写入渲染状态（照阶段 2「互认项目」页先例）；未保存内容的判定口径为行内编辑正在编辑的行仍在
   * 当前行集（Client/design.md 第 34 行）。可信组织或医院缺失时整页阻断，不参与挂起。
   */
  const hasUnsavedContent = editTarget !== null
  const trustedRangeChanged = selection !== null
    && appliedTrustedBranch !== undefined
    && trusted.organizationCode !== null
    && trusted.hospitalCode !== null
    && (selection.scopeKey !== trustedScopeKey || appliedTrustedBranch !== trusted.branchCode)
  const switchPending = trustedRangeChanged && hasUnsavedContent
  const switchConfirming = switchPending && dismissedTrustedRangeKey !== trustedRangeKey
  const switchDeferred = switchPending && dismissedTrustedRangeKey === trustedRangeKey
  /** 换绑挂起期间（确认框或待切换提示态）：既有选择暂不作废，选择器与页头仍按已应用范围取值。 */
  const switchSuspended = switchPending
  /** 显式或解析出的取值只在与当前可信范围一致时有效；范围换绑即视为旧范围，退回等待重新解析。 */
  const ownSelection = selection !== null && (selection.scopeKey === trustedScopeKey || switchSuspended)
    ? selection
    : null

  /**
   * C34 提交前现值复核的记录侧：页面应用范围时写下的范围键与可信院区（两者都在 `applySelection`
   * 同一帧记录）；页面尚未应用过范围时为 `null`，该状态下没有可提交的行。
   */
  const recordedTrustedScope: RecordedTrustedScope | null = selection !== null && appliedTrustedBranch !== undefined
    ? { scopeKey: selection.scopeKey, branchCode: appliedTrustedBranch }
    : null

  /** 应用一次范围切换：记录现读可信院区，清空编辑目标与挂起状态。 */
  const applySelection = useCallback((next: BranchScopeSelection, trustedNow: TrustedBranchScope) => {
    setAppliedTrustedBranch(trustedNow.branchCode)
    setSelection(next)
    setEditTarget(null)
    setDismissedTrustedRangeKey(null)
    setSwitchTarget(null)
  }, [])

  const handleSelectorChange = useCallback((next: BaseScopeValue, meta: BaseScopeSelectorChangeMeta) => {
    // 院区恒定单值：组件回调的空院区不被接受，保持原受控值（也不发空院区请求）。
    if (!next.branchId) return
    /**
     * 可信院区只作默认值：当前可信范围尚无取值（首次解析或可信范围换绑）时采用；归属以本回调
     * 携带的院区清单核对——可信院区缺失或不属于该院区清单时取该院下第一项。同一范围内的后续
     * 回调（用户的显式改选）不被可信默认值覆盖。
     */
    const current = selection !== null && selection.scopeKey === trustedScopeKey ? selection : null
    const trustedBranchCode = current === null ? trusted.branchCode : null
    const branchId = trustedBranchCode !== null && meta.branches.some((item) => item.id === trustedBranchCode)
      ? trustedBranchCode
      : next.branchId
    // 范围键取回调自带的组织与医院：非挂起时它与可信范围键是同一取值；换绑挂起期间它标识选择器
    // 正在展示的旧范围，完成切换时由范围键比对把这份旧范围目标排除，等选择器按新可信范围交付。
    const nextSelection: BranchScopeSelection = { scopeKey: `${next.orgId}|${next.hosId}`, branchId, meta }
    /**
     * 可信范围变化检测（C33）第二道：事件内复用渲染期派生的同一判定（应用范围时记录的取值对
     * 现读可信三层，Client/design.md 第 34 行），口径同源、不另立第二套挂起条件。变化且存在
     * 未保存内容时只记录目标、不应用本次切换——当前受控值保持不动，确认框由渲染期派生弹出；
     * 用户主动切换且可信范围未变化时按原规则静默应用，未保存输入随目标行离开当前行集被清空（C21）。
     */
    if (trustedRangeChanged && editTarget !== null) {
      setSwitchTarget(nextSelection)
      return
    }
    applySelection(nextSelection, trusted)
  }, [applySelection, editTarget, selection, trusted, trustedRangeChanged, trustedScopeKey])

  /**
   * 完成一次可信范围变化的切换（C33）：挂起期间选择器交付过目标范围就按它应用；组织医院层没有
   * 目标时既有选择随范围键失配作废，由选择器按新可信范围重新解析院区（与无未保存内容的换绑同一
   * 路径，是该层已有的等价实现）；院区层没有目标时按 S3-D22 三档规则现解析——可信院区属于该院区
   * 清单则选中它，否则取清单第一项。应用同时清空编辑目标与挂起状态。
   */
  const completeSwitch = useCallback(() => {
    if (selection === null) return
    const target = switchTarget !== null && switchTarget.scopeKey === trustedScopeKey ? switchTarget : null
    if (target !== null) {
      applySelection(target, trusted)
      return
    }
    if (selection.scopeKey !== trustedScopeKey) {
      // 组织医院层：新范围的院区清单尚不在页面手上，只清挂起状态，等选择器按新可信范围交付。
      setEditTarget(null)
      setDismissedTrustedRangeKey(null)
      setSwitchTarget(null)
      return
    }
    const branchId = resolveBranchFromList(trusted.branchCode, selection.meta.branches)
    if (branchId === null) return
    applySelection({ scopeKey: trustedScopeKey, branchId, meta: selection.meta }, trusted)
  }, [applySelection, selection, switchTarget, trusted, trustedScopeKey])

  /**
   * 院区层无未保存内容时的直接切换：可信院区变化来自宿主重新签发 token，属于外部系统变化，不在
   * effect 内同步级联渲染状态，推迟到下一个宏任务（照阶段 2「互认项目」页先例）。有未保存内容时
   * 等待确认框或待切换提示态；组织医院层不走这里——既有选择随范围键失配作废后由选择器按新可信
   * 范围重新解析，是该层已有的等价实现。
   */
  useEffect(() => {
    if (selection === null) return
    if (selection.scopeKey !== trustedScopeKey) return
    if (appliedTrustedBranch === undefined || appliedTrustedBranch === trusted.branchCode) return
    if (hasUnsavedContent) return
    const task = window.setTimeout(() => {
      completeSwitch()
    }, 0)
    return () => window.clearTimeout(task)
  }, [appliedTrustedBranch, completeSwitch, hasUnsavedContent, selection, trusted.branchCode, trustedScopeKey])

  /**
   * 编辑目标改道：目标行离开当前行集时由主体同步清空；未保存内容消失后待切换提示态的自动完成
   * 交由上面的可信变化效果按派生状态处理（C33），这里不再直接触发切换。
   */
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
   * 已解析院区 → 查询条件：三值中的组织与医院不进请求，只携带院区，请求构造只经适配层出口；
   * 院区未解析即为 `null`（零业务请求），绝不以空院区发起请求。
   */
  const appliedScope = useMemo<RecognitionAmountsScope<BranchAmountListQuery> | null>(() => {
    if (ownSelection === null) return null
    const query = buildBranchAmountListQuery({ branchCode: ownSelection.branchId })
    return query === null ? null : { key: `branch|${query.branchCode}`, query }
  }, [ownSelection])

  if (trusted.organizationCode === null || trusted.hospitalCode === null) {
    return <div className='recognition-amounts-page'>
      <div className='recognition-amounts-header'>
        <div>
          <Typography.Title level={2}>本院区互认项目金额</Typography.Title>
          <Typography.Text type='secondary'>维护本院区的互认项目金额</Typography.Text>
        </div>
      </div>
      <Alert
        type='warning'
        showIcon
        title='可信范围不可用'
        description='当前无法从登录凭证确定可信组织与医院，因此不能读取或维护本院区互认项目金额。请从宿主登录后重新进入本页面。'
      />
    </div>
  }

  // 换绑挂起期间：选择器与页头仍按范围键记录的已应用组织医院取值，等挂起结束新的可信值才流过。
  const [appliedOrgId, appliedHosId] = (selection?.scopeKey ?? '|').split('|')
  const selectorOrgId = switchSuspended && appliedOrgId !== '' ? appliedOrgId : trusted.organizationCode
  const selectorHosId = switchSuspended && appliedHosId !== '' ? appliedHosId : trusted.hospitalCode

  const scopePrompt = switchDeferred ? (
    <div className='recognition-amounts-prompt'>
      可信范围已变化，存在未保存的金额输入；确认后放弃输入并按新范围加载。
      <Button size='small' onClick={completeSwitch}>放弃并切换</Button>
    </div>
  ) : null

  return <>
    <BranchRecognitionAmountsBoard
      title='本院区互认项目金额'
      subtitle='维护本院区的互认项目金额'
      missingScopeText='请选择院区后查看互认项目金额'
      selector={<BaseScopeSelector
        level='branch'
        className='recognition-amounts-scope'
        orgId={selectorOrgId}
        hosId={selectorHosId}
        selectFirstOption
        value={{
          orgId: selectorOrgId,
          hosId: selectorHosId,
          branchId: ownSelection?.branchId,
        }}
        onChange={handleSelectorChange}
      />}
      scopeDisplay={buildBranchScopeDisplay(selectorOrgId, selectorHosId, ownSelection)}
      scope={appliedScope}
      scopePrompt={scopePrompt}
      editTarget={editTarget}
      writeSuspended={switchSuspended}
      recordedTrustedScope={recordedTrustedScope}
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
      onCancel={() => setDismissedTrustedRangeKey(trustedRangeKey)}
    >
      <p>可信范围已变化，切换范围将放弃未保存的金额输入。</p>
    </Modal> : null}
  </>
}
