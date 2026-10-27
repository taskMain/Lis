/**
 * 「本院区互认项目金额」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-amounts`）。
 *
 * 组织与医院取可信上下文并固定展示；院区恒取可信范围的院区业务编码，
 * 不提供院区选择，也不做清单核对、首项回退等解析。查询与保存请求的院区参数恒为该登录院区的业务编码，
 * 可信院区缺失属权限配置错误：页面不兜底、不本地补默认院区，适配层拒绝构造请求即零业务请求，
 * 页面以空态呈现，恢复正确的权限配置后按登录院区重读。
 * 查询只携带院区与可选关键字，保存载荷只含院区、标准项目编码与金额（组织与医院由服务端从可信上下文
 * 注入）。可信组织或医院缺失时整页阻断：统一接入阻断面板（整页只渲染标题与错误 Alert），不发任何
 * 业务请求，也不伪造组织或降级为空值。页头范围区经共用组件的页头插槽由共用范围条承载：单行只显
 * 「组织 / 医院 / 院区」名称，无编码。页头、查询区、四栏层级列表、金额行内编辑器与页脚计数由
 * 两个金额页共用的 `RecognitionAmountListView` 承载（S3-D29），本页只保留可信范围解析与数据接线。
 * 本院区金额导出经共用组件的导出承载接入：请求编排在页面完成（请求体只有登录院区与已生效关键字，
 * 点击时刻现读登录院区并要求与已应用一致），门控状态与两道防线共用既有状态——登录院区缺失时
 * 页面不整页阻断（与既有空态一致），按钮随范围未解析禁用；读取失败与写入后刷新失败由共用组件
 * 按既有写门控禁用。页面不实现角色判断、不做权限二次确认，也不下推枚举筛选。
 */
import { useCallback, useMemo, useRef, useState } from 'react'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
import { useResolvedTrustedScope } from '../../components/currentOrganizationScope'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { saveExportWorkbook } from '../../shared/exportFileDownload'
import { useHostSessionSnapshot } from '../../session/hostSession'
import { readTrustedBranchCode, readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  buildBranchRecognitionAmountExportDownloadName,
  buildBranchRecognitionAmountExportPayload,
  exportBranchRecognitionAmounts,
  queryBranchRecognitionAmountRows,
  saveBranchRecognitionAmount,
  type BranchAmountListQuery,
} from './recognitionAmountsApi'
import { RecognitionAmountListView, RecognitionAmountsHeader, type RecognitionAmountsScope } from './RecognitionAmountListView'
import './RecognitionAmounts.css'

/** 可信业务编码判定：缺失或纯空白一律视为不可用，不伪造、不静默使用空串、不回落实例标识。 */
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
 * 「可信当前范围」的唯一接缝：组织、医院、院区三层都取 IAM 组织业务编码，
 * 会话变化即重新求值（与互认项目页、金额页的可信范围口径一致）。页面不持有可选组织集合、不提供自由输入，
 * 也不从 URL、Storage、表单或列表构造范围；业务编码为空即该层不可用，不回落实例标识。
 */
function useTrustedBranchScope(): TrustedBranchScope {
  const client = useApiClientContext()
  const session = useHostSessionSnapshot()
  const resolved = useResolvedTrustedScope(client, session)
  if (resolved.status !== 'ready') return { organizationCode: null, hospitalCode: null, branchCode: null }
  return {
    organizationCode: readTrustedCode(resolved.organizationCode),
    hospitalCode: readTrustedCode(resolved.hospitalCode),
    branchCode: readTrustedCode(resolved.branchCode),
  }
}

/** C34 提交拦截提示：现读可信组织或医院有一层读不到，零写请求
 * （S3-D28「现读不到」仅指这两层；院区恒随登录院区，不再参与提交前比对）。 */
const TRUSTED_SCOPE_UNREADABLE_REASON = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

/**
 * 医院管理员页：组织与医院固定来自可信上下文；院区恒取登录院区的业务编码，不提供院区选择，
 * 查询与保存只携带登录院区业务编码与可选关键字。
 *
 * 院区不兜底、不本地补默认值：可信院区缺失属权限配置错误，适配层拒绝构造请求即零业务请求，
 * 页面以空态呈现，恢复正确的权限配置后按登录院区重读。可信组织或医院缺失时整页阻断，恢复后按
 * 新的可信范围重新取值；可信组织、医院或登录院区任一层变化即视为范围切换（范围键含三层），
 * 共用列表视图清空旧行集并按新范围重读，未保存输入随行集替换被清空（C32）——不存在用户
 * 选定的滞留范围，不再有切换确认框与待切换提示态（共用组件的挂起态与提示态插槽都为空实现）。
 * C34 的提交前现值复核只保留可信组织或医院现读不到的拦截，经共用组件的写前复核出口接入。
 */
export function BranchRecognitionAmounts() {
  const client = useApiClientContext()
  const trusted = useTrustedBranchScope()
  /** 行内编辑目标行的项目编码；共用列表视图负责在目标行离开当前行集时清空它。 */
  const [editTarget, setEditTarget] = useState<string | null>(null)
  /** 导出是否在途：按钮进入 loading 形态；导出是只读链，不参与既有写门控。 */
  const [exporting, setExporting] = useState(false)
  /** `exporting` 的同步镜像：同一动作内的再次点击在状态落地前就由它拦下。 */
  const exportingRef = useRef(false)

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
   * 共用列表视图的查询构造出口：在当前院区之上叠加已生效关键字，请求构造只经适配层出口。
   * 引用恒定，列表视图以它的返回值身份驱动读取效果。
   */
  const buildQuery = useCallback(
    (query: BranchAmountListQuery, keywordApplied: string) => buildBranchAmountListQuery({ ...query, keyword: keywordApplied }),
    [],
  )

  /**
   * 写前复核（C34）：在构造载荷之前现读——现读走命令式出口（组织、医院两层），不经渲染期闭包：
   * 宿主切换工作身份之后、页面重渲染之前回车提交，渲染期取值可能仍是旧可信范围，只有现读才
   * 兜得住。有一层读不到即零写请求，原因经共用列表视图以 message.error 就近提示。
   */
  const preSubmitBlockReason = useCallback(() => {
    if (readTrustedOrganizationCode() === null || readTrustedHospitalCode() === null) {
      return TRUSTED_SCOPE_UNREADABLE_REASON
    }
    return null
  }, [])

  /**
   * 登录院区 → 查询条件：院区恒取可信登录院区，组织与医院不进请求，由服务端从可信上下文
   * 注入；请求构造只经适配层出口。范围键含可信三层：任一层变化即视为范围切换，共用列表视图清空
   * 旧行集重读。登录院区缺失（权限配置错误）时适配层拒绝构造请求，返回 `null` 即零业务请求、
   * 以空态呈现。
   */
  const appliedScope = useMemo<RecognitionAmountsScope<BranchAmountListQuery> | null>(() => {
    const query = buildBranchAmountListQuery({ branchCode: trusted.branchCode ?? '' })
    if (query === null) return null
    return {
      key: `branch|${trusted.organizationCode ?? ''}|${trusted.hospitalCode ?? ''}|${query.branchCode}`,
      query,
    }
  }, [trusted.branchCode, trusted.hospitalCode, trusted.organizationCode])

  /**
   * 发起本院区金额导出。请求体只有登录院区与已生效关键字（组织与医院由服务端从可信上下文解析，
   * 页面不提交；配置状态页签与金额状态档位不参与，导出与档位解耦）：点击时刻**现读**登录院区
   * （命令式出口，不经渲染期闭包——与写前复核同一手法，兜住宿主切换工作身份到页面重渲染之间的
   * 窗口），并要求它与已应用（正在驱动列表读取）的登录院区一致，不一致即整段放弃、零请求。
   * 登录院区缺失不构造导出请求（共用组件的门控已随空态禁用按钮，这里兜住残余路径）。
   * 取得字节后经共享下载模块触发保存，下载名按**发起时刻**由前端给定（在请求发出之前构造）；
   * 失败（含共享模块对 0 字节内容的契约错误）由宿主统一展示，本层只结束加载态并保留列表与筛选，
   * 不误刷新、不新增提示层。导出动作与条件不写入路由查询参数（导出不是页面视图状态）。
   */
  const runExport = useCallback(async (keywordApplied: string) => {
    if (exportingRef.current) return
    const applied = appliedScope
    if (applied === null) return
    if (readTrustedBranchCode() !== applied.query.branchCode) return
    const payload = buildBranchRecognitionAmountExportPayload({ branchCode: applied.query.branchCode }, keywordApplied)
    if (payload === null) return
    exportingRef.current = true
    setExporting(true)
    try {
      const downloadName = buildBranchRecognitionAmountExportDownloadName()
      const bytes = await exportBranchRecognitionAmounts(client, payload)
      saveExportWorkbook(bytes, downloadName)
    } catch {
      // 导出失败不误报成功也不误刷新；页面不解析错误响应体、不新增提示层（与平台金额页导出同款恢复形态）。
    } finally {
      exportingRef.current = false
      setExporting(false)
    }
  }, [appliedScope, client])

  // 可信组织或医院缺失即整页阻断：阻断面板不渲染列表视图，因此零业务请求。
  if (trusted.organizationCode === null || trusted.hospitalCode === null) {
    return <div className='recognition-amounts-page'>
      <ScopeUnavailablePanel
        title={<RecognitionAmountsHeader
          title='本院区互认项目金额'
          subtitle='维护本院区的互认项目金额'
        />}
      />
    </div>
  }

  return <RecognitionAmountListView
    title='本院区互认项目金额'
    subtitle='维护本院区的互认项目金额'
    missingScopeText='当前登录身份未携带院区，无法查询互认项目金额'
    headerExtra={<CurrentScopeBar />}
    scope={appliedScope}
    editTarget={editTarget}
    exportBinding={{
      exporting,
      // 登录院区缺失时 `appliedScope` 为 `null`，共用组件按范围未解析禁用按钮（本页不整页阻断，
      // 与既有空态一致）；读取失败与写入后刷新失败由共用组件按既有写门控禁用，均零请求。
      onExport: (keywordApplied) => { void runExport(keywordApplied) },
    }}
    onEditTargetChange={setEditTarget}
    buildQuery={buildQuery}
    queryRows={queryRows}
    saveRow={saveRow}
    preSubmitBlockReason={preSubmitBlockReason}
  />
}
