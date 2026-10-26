/**
 * 「互认项目金额维护」平台管理员页（宿主路径 `/subApps/medical-recognition/recognition-amounts`）。
 *
 * 页面本身只做范围接入与数据源接线：组织、医院、院区三级取自范围选择器（受控保存，院区由页面按
 * 默认规则解析且恒定单值），查询与保存请求都携带这三个值；页头、查询区、四栏层级列表与金额行内
 * 编辑由两个金额页共用的 `RecognitionAmountListView` 承载（S3-D29），页面只把范围与数据出口接上去。
 * 本页专属的两道防线状态（可信范围变化的确认框与待切换提示态、保存前的现值复核）留在页面，
 * 经共用组件的挂起态与写前复核属性接入。平台金额导出经共用组件的导出承载接入：请求编排在页面完成
 * （点击时刻重读当前选定范围并要求与已应用范围一致），门控状态（可信范围已变化未应用）经导出承载
 * 的禁用属性传入。页面不实现角色判断、不做权限二次确认，也不下推枚举筛选。
 */
import { Button, Modal } from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { OrganizationScopeSelector, type OrganizationScopeChangeMeta, type OrganizationScopeValue } from '../../components/OrganizationScopeSelector'
import { findScopeCode } from '../../components/organizationScope'
import { useResolvedTrustedScope } from '../../components/currentOrganizationScope'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { saveExportWorkbook } from '../../shared/exportFileDownload'
import { useHostSessionSnapshot } from '../../session/hostSession'
import { readTrustedBranchCode, readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  buildRecognitionAmountExportDownloadName,
  buildRecognitionAmountExportPayload,
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
  exportRecognitionAmounts,
  queryRecognitionAmountRows,
  recheckRecognitionAmountScope,
  saveOrganizationHospitalBranchRecognitionAmount,
  type RecognitionAmountListQuery,
} from './recognitionAmountsApi'
import { RecognitionAmountListView, RecognitionAmountsHeader, type RecognitionAmountsScope } from './RecognitionAmountListView'
import './RecognitionAmounts.css'

/**
 * C34 提交前现值复核失败时的拦截提示（S3-D28；Client/testPlan.md 第 42 行双场景共用一句，
 * 经 message.error 承载）：一句话覆盖「已变化」与「现读不到」两种情形，随后给出
 * 零写结论与后续动作（切换到新可信范围）。该句由页面书写；适配层交付的原因串服务于复核出口的
 * 用例，不随本页展示。
 */
const TRUSTED_SCOPE_SUBMIT_BLOCK_REASON = '可信范围已变化或读不到，本次不提交写入，请切换到新可信范围后重新提交。'

/**
 * 院区按 S3-D22 规则解析：可信院区（业务编码）属于给定院区清单则选中它，可信院区缺失或不属于
 * 该清单时取清单第一项；清单为空时返回 `null`（不构造空院区请求）。
 * 返回的是院区实例标识——业务编码只用于核对归属，选择器受控值仍取标识。
 */
function resolveBranchFromList(
  trustedBranchCode: string | null,
  branches: OrganizationScopeChangeMeta['branches'],
): string | null {
  const firstId = branches[0]?.id ?? null
  if (firstId === null) return null
  const matched = trustedBranchCode === null
    ? undefined
    : branches.find((item) => item.code === trustedBranchCode)
  return matched?.id ?? firstId
}

/** 业务编码 → 该层实例标识；编码缺失或不在清单中返回 `undefined`（由重载后的下一次回调补齐）。 */
function idOfCode(
  options: OrganizationScopeChangeMeta['organizations'],
  code: string | null | undefined,
): string | undefined {
  const text = (code ?? '').trim()
  if (text.length === 0) return undefined
  return options.find((option) => option.code === text)?.id
}

/**
 * 三级实例标识组合 + 本次交付的选项清单 → 页面已解析的范围。
 *
 * 范围值同时携带实例标识与业务编码：标识交给选择器受控显示，业务编码才是请求提交的标识；
 * 标识不在清单中时该层编码为空，请求构造随即拒绝（不回落实例标识）。
 */
function toSelection(
  ids: { orgId: string; hosId: string; branchId: string },
  meta: ScopeSelection['meta'],
): ScopeSelection {
  return {
    scope: {
      orgId: ids.orgId,
      hosId: ids.hosId,
      branchId: ids.branchId,
      orgCode: findScopeCode(meta.organizations, ids.orgId) ?? undefined,
      hosCode: findScopeCode(meta.hospitals, ids.hosId) ?? undefined,
      branchCode: findScopeCode(meta.branches, ids.branchId) ?? undefined,
    },
    meta,
  }
}

/** 页面已解析的范围：三级标识与业务编码恒定齐全；选项清单与取值同帧交付，页头展示据此取名称。 */
interface ScopeSelection {
  scope: OrganizationScopeValue
  meta: {
    organizations: OrganizationScopeChangeMeta['organizations']
    hospitals: OrganizationScopeChangeMeta['hospitals']
    branches: OrganizationScopeChangeMeta['branches']
  }
}

/** 可信当前范围：三层都是 IAM 组织业务编码，缺失层为 null（不回落实例标识）。 */
interface TrustedScope {
  organizationCode: string | null
  hospitalCode: string | null
  branchCode: string | null
}

/** 可信范围比较：逐层按去空白后的值比较，缺失与空白同义。 */
function sameTrustedScope(left: TrustedScope, right: TrustedScope): boolean {
  const normalize = (value: string | null) => (value ?? '').trim()
  return normalize(left.organizationCode) === normalize(right.organizationCode)
    && normalize(left.hospitalCode) === normalize(right.hospitalCode)
    && normalize(left.branchCode) === normalize(right.branchCode)
}

/**
 * 平台管理员页的初始范围解析：只在可信组织与可信医院**齐备**时执行——任一缺失时页面整页阻断、
 * 选择器不挂载，本函数不会被调用（缺失场景不存在「级联取首项」的回退解析）。解析口径：
 * 组织取可信组织（不属于选项清单时取本次交付的第一项），医院取可信医院且须属于选定组织
 * （不属于清单时取该院第一项），院区取可信院区且须属于当前组织、医院（缺失或不属于时取新范围第一项）。
 *
 * 本次交付的选项清单由选择器按其受控值装载：选定组织或医院与交付组合不一致时，交付的医院与
 * 院区清单仍属于首项组合，院区归属无从核对。此时只应用组织与医院、院区留空——三值不齐不构造
 * 查询（`buildRecognitionAmountListQuery` 对空白即返回 `null`），选择器按受控值重载下游选项并
 * 回传新清单后，由院区仍为空的第二轮解析完成取值（受控值与回传值不一致即重载，与真实
 * 受控范围选择器的初始数据装载同一口径）。
 */
function resolveInitialScopeSelection(
  next: OrganizationScopeValue,
  meta: OrganizationScopeChangeMeta,
  trusted: { organizationCode: string | null; hospitalCode: string | null; branchCode: string | null },
): ScopeSelection {
  const trustedOrganizationCode = trusted.organizationCode
  const trustedHospitalCode = trusted.hospitalCode
  const firstOrgId = next.orgId ?? ''
  const firstHosId = next.hosId ?? ''
  const firstBranchId = next.branchId ?? ''
  const metaSelection: ScopeSelection['meta'] = {
    organizations: meta.organizations,
    hospitals: meta.hospitals,
    branches: meta.branches,
  }
  // 组织取可信组织（按业务编码核对）；缺失或不属于选项清单时取本次交付的第一项。
  const organizationId = idOfCode(meta.organizations, trustedOrganizationCode) ?? firstOrgId
  if (organizationId !== firstOrgId) {
    // 选定组织并非交付时的组织：医院清单尚未按它装载，先应用组织、医院与院区留空等待重载。
    return toSelection({ orgId: organizationId, hosId: '', branchId: '' }, metaSelection)
  }
  // 医院取可信医院且须属于选定组织；缺失或不属于清单时取该院第一项。
  const hospitalId = idOfCode(meta.hospitals, trustedHospitalCode) ?? firstHosId
  if (hospitalId !== firstHosId) {
    // 选定医院并非交付时的医院：院区清单属于首项组合，先应用组织与医院、院区留空等待重载。
    return toSelection({ orgId: organizationId, hosId: hospitalId, branchId: '' }, metaSelection)
  }
  // 院区取可信院区且须属于当前组织、医院；缺失或不属于清单时取新范围第一项。
  const branchId = idOfCode(meta.branches, trusted.branchCode) ?? firstBranchId
  return toSelection({ orgId: organizationId, hosId: hospitalId, branchId }, metaSelection)
}

/**
 * 平台管理员页：组织、医院可切换，院区恒定单值，三个值随查询与保存请求提交。
 * 页头只保留标题与副标题，不展示组织、医院、院区范围（S3-D23 修订：正文自带三级范围
 * 选择器，页头重复展示同一信息属冗余）。
 *
 * 可信组织或医院任一缺失（含匿名与初始化会话）即整页阻断：整页只渲染标题与错误 Alert，
 * 不渲染选择器、不发起任何请求，也不伪造组织或退化为全局查询。
 * 范围值由页面**受控**保存并按默认规则解析：范围选择器以 `selectFirstOption` 在初始
 * 选项加载完成后回调首项组合与选项清单，页面按 S3-D22 完成初始解析——组织、医院、院区各自
 * 缺失或不属于上级清单时取该层第一项，选定组织或医院与交付组合不一致时先留空院区、等选择器
 * 按受控值重载新清单后完成第二轮解析；在此之上页面还做三件事——三值任一缺失的回调保持原受控值（院区永不为
 * 空发请求）、切换组织医院时把属于新范围的可信院区作为默认值（同一组织医院内的显式改选不被
 * 覆盖），以及在可信范围变化时按未保存内容的有无决定直接切换还是
 * 先弹确认（C33）；C33 的变化判定在**渲染期**由会话快照派生，宿主切换工作身份、用户
 * 不碰选择器时同样触发，选择器回调里的现读比对只作第二道。
 * 写请求只在用户回车提交行内编辑时发出。
 */
export function RecognitionAmounts() {
  const client = useApiClientContext()
  /** 宿主会话快照：工作身份切换后由订阅推进，可信范围与院区默认值随新会话重新求值。 */
  const session = useHostSessionSnapshot()
  // 宿主会话只给组织叶子时由本项目当前范围接口补齐医院与院区；会话已带完整医院层时直接使用。
  const resolvedScope = useResolvedTrustedScope(client, session)
  // 可信范围一律取业务编码：这是请求侧提交的组织、医院与院区标识，实例标识不进入请求。
  const trustedOrganizationCode = resolvedScope.status === 'ready' ? resolvedScope.organizationCode : null
  const trustedHospitalCode = resolvedScope.status === 'ready' ? resolvedScope.hospitalCode : null
  const trustedBranch = resolvedScope.status === 'ready' ? resolvedScope.branchCode : null
  const scopeBlocked = resolvedScope.status !== 'ready'
    || trustedOrganizationCode === null
    || trustedHospitalCode === null

  const [selection, setSelection] = useState<ScopeSelection | null>(null)
  /**
   * 已应用范围选择的**同步镜像**，只在 `applySelection` 一处与它一起赋值，不构成第二个状态源；
   * 保留它的唯一用途是让导出的点击时刻复核读到现值（见 `runExport`）。
   */
  const selectionRef = useRef<ScopeSelection | null>(null)
  /** 行内编辑目标行的项目编码；共用列表视图负责在目标行离开当前行集时清空它。
   *  未保存内容消失后待切换提示态的自动完成由上面的可信变化效果按派生状态处理（C33）。 */
  const [editTarget, setEditTarget] = useState<string | null>(null)
  /** 上次应用范围时记录的可信范围；`undefined` 表示页面尚未应用过一次范围（渲染期比对用，不放 ref）。 */
  const [appliedTrustedScope, setAppliedTrustedScope] = useState<TrustedScope | undefined>(undefined)
  /** 取消确认框时针对的可信范围：与现读会话一致即处于待切换提示态（C33，渲染期派生）。 */
  const [dismissedTrustedScope, setDismissedTrustedScope] = useState<TrustedScope | undefined>(undefined)
  /** 挂起期间选择器回调交付的目标范围：确认或提示态再次确认时按它完成切换。 */
  const [switchTarget, setSwitchTarget] = useState<ScopeSelection | null>(null)
  /** 导出是否在途：按钮进入 loading 形态；导出是只读链，不参与既有写门控。 */
  const [exporting, setExporting] = useState(false)
  /** `exporting` 的同步镜像：同一动作内的再次点击在状态落地前就由它拦下。 */
  const exportingRef = useRef(false)

  /** 应用一次范围切换：记录现读可信范围，清空编辑目标与挂起状态。 */
  const applySelection = useCallback((next: ScopeSelection, trusted: TrustedScope) => {
    selectionRef.current = next
    setAppliedTrustedScope(trusted)
    setSelection(next)
    setEditTarget(null)
    setDismissedTrustedScope(undefined)
    setSwitchTarget(null)
  }, [])

  /**
   * 可信范围变化（C33）的渲染期派生：宿主切换工作身份后，会话可信范围已不同于应用范围时
   * 记录的取值，而页面仍展示已应用范围的列表。匿名与初始化会话不携带可信院区，
   * 不记作已变化；确认框、待切换提示态与写门控都从这一份比对派生，不在 effect 里写入渲染状态
   * （照阶段 2「互认项目」页先例）；未保存内容的判定口径为行内编辑正在编辑的行仍在当前行集
   * （Client/design.md 第 34 行）。
   */
  const trustedScope = useMemo<TrustedScope>(() => ({
    organizationCode: trustedOrganizationCode,
    hospitalCode: trustedHospitalCode,
    branchCode: trustedBranch,
  }), [trustedBranch, trustedHospitalCode, trustedOrganizationCode])
  const hasUnsavedContent = editTarget !== null
  const trustedScopeChanged = selection !== null
    && appliedTrustedScope !== undefined
    && trustedScope.organizationCode !== null
    && trustedScope.hospitalCode !== null
    && trustedScope.branchCode !== null
    && !sameTrustedScope(appliedTrustedScope, trustedScope)
  const switchPending = trustedScopeChanged && hasUnsavedContent
  const switchConfirming = switchPending
    && (dismissedTrustedScope === undefined || !sameTrustedScope(dismissedTrustedScope, trustedScope))
  const switchDeferred = switchPending
    && dismissedTrustedScope !== undefined
    && sameTrustedScope(dismissedTrustedScope, trustedScope)

  /**
   * 完成一次可信范围变化的切换（C33）：挂起期间选择器交付过目标范围就按它应用；否则按新会话
   * 重算范围——可信组织或医院变化时先应用上层并留空院区，选择器按新受控值重载后的下一次回调
   * 完成院区解析；组织医院不变时按 S3-D22 规则重取院区（可信院区属于当前组织医院则选中它，
   * 否则取该院区清单第一项）。
   */
  const completeSwitch = useCallback(() => {
    if (selection === null) return
    if (switchTarget !== null) {
      applySelection(switchTarget, trustedScope)
      return
    }
    if (trustedScope.organizationCode === null || trustedScope.hospitalCode === null) return
    if (trustedScope.organizationCode !== selection.scope.orgCode) {
      applySelection(
        toSelection({ orgId: idOfCode(selection.meta.organizations, trustedScope.organizationCode) ?? '', hosId: '', branchId: '' }, selection.meta),
        trustedScope,
      )
      return
    }
    if (trustedScope.hospitalCode !== selection.scope.hosCode) {
      applySelection(
        toSelection({
          orgId: selection.scope.orgId ?? '',
          hosId: idOfCode(selection.meta.hospitals, trustedScope.hospitalCode) ?? '',
          branchId: '',
        }, selection.meta),
        trustedScope,
      )
      return
    }
    const branchId = resolveBranchFromList(trustedScope.branchCode, selection.meta.branches)
    if (branchId === null) return
    applySelection(
      toSelection({ orgId: selection.scope.orgId ?? '', hosId: selection.scope.hosId ?? '', branchId }, selection.meta),
      trustedScope,
    )
  }, [applySelection, selection, switchTarget, trustedScope])

  /**
   * 无未保存内容时的直接切换：可信范围变化来自宿主切换工作身份，属于外部系统变化，不在 effect
   * 内同步级联渲染状态，同样推迟到下一个宏任务（照阶段 2「互认项目」页先例）。有未保存内容时
   * 等待确认框或待切换提示态，不清理、不加载。
   */
  useEffect(() => {
    if (!trustedScopeChanged) return
    if (hasUnsavedContent) return
    const task = window.setTimeout(() => {
      completeSwitch()
    }, 0)
    return () => window.clearTimeout(task)
  }, [completeSwitch, hasUnsavedContent, trustedScopeChanged])

  const handleSelectorChange = useCallback((next: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => {
    // 三级任一缺失的回调不被接受：保持原受控值（院区恒定单值，也不发空范围请求）。
    if (!next.orgId || !next.hosId || !next.branchId) return
    const current = selection?.scope ?? null
    if (current === null || current.branchId === '') {
      // 初始解析（S3-D22）：第一轮按本次交付的清单解析；选定组织或医院与交付组合不一致时只应用
      // 组织与医院、院区留空，选择器重载新清单后由院区仍为空的第二轮完成取值。现读可信三层与
      // 渲染期接缝同一来源（宿主会话），不另建第二套读取途径。
      const trustedNow = {
        organizationCode: readTrustedOrganizationCode(),
        hospitalCode: readTrustedHospitalCode(),
        branchCode: readTrustedBranchCode(),
      }
      applySelection(resolveInitialScopeSelection(next, meta, trustedNow), trustedNow)
      return
    }
    const scopeLayerChanged = current.orgId !== next.orgId || current.hosId !== next.hosId
    // 可信院区只作默认值：切换组织医院时按新范围解析（按业务编码核对归属），清单里没有它时取回调交付的院区。
    const trustedBranchCode = scopeLayerChanged ? readTrustedBranchCode() : null
    const trustedBranchId = trustedBranchCode !== null ? idOfCode(meta.branches, trustedBranchCode) : undefined
    const branchId = trustedBranchId ?? next.branchId
    const nextSelection = toSelection(
      { orgId: next.orgId, hosId: next.hosId, branchId },
      { organizations: meta.organizations, hospitals: meta.hospitals, branches: meta.branches },
    )
    // 可信范围变化检测（C33）第二道：事件内现读可信范围，与渲染期派生同一口径——宿主切换工作身份
    // 到页面重渲染之间的窗口由这里兜住。变化且存在未保存内容时只记录目标、不应用本次切换（当前
    // 受控值保持不动）；用户主动切换且可信院区未变化时按原规则静默应用，未保存输入随目标行离开
    // 当前行集被清空（C21）。
    const trustedNow = {
      organizationCode: readTrustedOrganizationCode(),
      hospitalCode: readTrustedHospitalCode(),
      branchCode: readTrustedBranchCode(),
    }
    const trustedChanged = appliedTrustedScope !== undefined && !sameTrustedScope(trustedNow, appliedTrustedScope)
    if (trustedChanged && editTarget !== null) {
      setSwitchTarget(nextSelection)
      return
    }
    applySelection(nextSelection, trustedNow)
  }, [applySelection, appliedTrustedScope, editTarget, selection])

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
   * 共用列表视图的查询构造出口：在当前范围三值之上叠加已生效关键字，请求构造只经适配层出口。
   * 引用恒定，列表视图以它的返回值身份驱动读取效果。
   */
  const buildQuery = useCallback(
    (query: RecognitionAmountListQuery, keywordApplied: string) => buildRecognitionAmountListQuery({ ...query, keyword: keywordApplied }),
    [],
  )

  /**
   * 写前复核（C34，S3-D28 第二道，兜住用户未触发挂起路径的场景）：比对「页面应用范围时记录的
   * 可信范围」与「保存发起前现读的可信范围」；任一层记录与现读不一致即零写请求（两侧同为缺失
   * 视为一致），返回的原因经共用列表视图以 message.error 就近提示，单元格内不再渲染校验原因。
   * 比对对象是可信范围的变化，用户选定的 `scope.query` 三值不参与。
   */
  const preSubmitBlockReason = useCallback(() => {
    const trusted = {
      organizationCode: readTrustedOrganizationCode(),
      hospitalCode: readTrustedHospitalCode(),
      branchCode: readTrustedBranchCode(),
    }
    const recheck = recheckRecognitionAmountScope({
      applied: appliedTrustedScope ?? null,
      trusted,
      layers: ['organization', 'hospital', 'branch'],
    })
    // 拦截原因由页面书写并经 message.error 提示，不透传适配层交付的原因串。
    return recheck.ok ? null : TRUSTED_SCOPE_SUBMIT_BLOCK_REASON
  }, [appliedTrustedScope])

  // 查询与保存都只提交业务编码：实例标识留在选择器受控值里，不进入请求。
  const organizationCode = selection?.scope.orgCode ?? null
  const hospitalCode = selection?.scope.hosCode ?? null
  const branchCode = selection?.scope.branchCode ?? null
  /**
   * 已解析的范围 → 查询条件：三值恒定齐全，请求构造只经适配层出口。依赖只取三个编码字符串，
   * 选择器因选项清单刷新而重复回调时不重建条件对象，读取效果不会因此重复执行。
   */
  const appliedScope = useMemo<RecognitionAmountsScope<RecognitionAmountListQuery> | null>(() => {
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

  /**
   * 发起平台金额导出。请求体只有当前选定的组织、医院、院区与已生效关键字（配置状态页签与
   * 金额状态档位不参与，导出与档位解耦）：点击时刻经镜像重读当前选定范围，并要求它与已应用
   * （正在驱动列表读取）的范围一致——页面尚未应用新范围或三值不齐即零请求（与互认项目页
   * 点击时刻复核同口径，导出侧整段放弃而不是换范围提交）。
   * 取得字节后经共享下载模块触发保存，下载名按**发起时刻**由前端给定（在请求发出之前构造）；
   * 失败（含共享模块对 0 字节内容的契约错误）由宿主统一展示，本层只结束加载态并保留列表与筛选，
   * 不误刷新、不新增提示层。导出动作与条件不写入路由查询参数（导出不是页面视图状态）。
   */
  const runExport = useCallback(async (keywordApplied: string) => {
    if (exportingRef.current) return
    const current = selectionRef.current
    if (current === null || appliedScope === null) return
    const currentOrganizationCode = current.scope.orgCode
    const currentHospitalCode = current.scope.hosCode
    const currentBranchCode = current.scope.branchCode
    if (currentOrganizationCode === undefined || currentHospitalCode === undefined || currentBranchCode === undefined) return
    if (currentOrganizationCode !== appliedScope.query.organizationCode
      || currentHospitalCode !== appliedScope.query.hospitalCode
      || currentBranchCode !== appliedScope.query.branchCode) return
    const payload = buildRecognitionAmountExportPayload(
      { organizationCode: currentOrganizationCode, hospitalCode: currentHospitalCode, branchCode: currentBranchCode },
      keywordApplied,
    )
    // 范围三值不齐不构造导出请求（与查询构造同一防御）；共用组件的门控已禁用按钮，这里兜住残余路径。
    if (payload === null) return
    exportingRef.current = true
    setExporting(true)
    try {
      const downloadName = buildRecognitionAmountExportDownloadName()
      const bytes = await exportRecognitionAmounts(client, payload)
      saveExportWorkbook(bytes, downloadName)
    } catch {
      // 导出失败不误报成功也不误刷新；页面不解析错误响应体、不新增提示层（与互认项目页导出同款恢复形态）。
    } finally {
      exportingRef.current = false
      setExporting(false)
    }
  }, [appliedScope, client])

  // 可信组织或医院缺失即整页阻断：阻断面板不渲染选择器与列表视图，选择器不装载选项、
  // 列表视图不挂载，因此零业务请求（早退在全部钩子之后，钩子挂载顺序不随阻断变化）。
  if (scopeBlocked) {
    return <div className='recognition-amounts-page'>
      <ScopeUnavailablePanel
        title={<RecognitionAmountsHeader
          title='互认项目金额维护'
          subtitle='按组织、医院与院区维护互认项目金额'
        />}
      />
    </div>
  }

  return <>
    <RecognitionAmountListView
      title='互认项目金额维护'
      subtitle='按组织、医院与院区维护互认项目金额'
      missingScopeText='请选择组织、医院与院区后查看互认项目金额'
      selector={<OrganizationScopeSelector
        className='recognition-amounts-scope'
        selectFirstOption
        value={selection?.scope ?? {}}
        onChange={handleSelectorChange}
      />}
      scopePrompt={scopePrompt}
      scope={appliedScope}
      editTarget={editTarget}
      writeSuspended={switchPending}
      exportBinding={{
        exporting,
        // 可信范围已变化且页面尚未应用（含确认框存续期间与待切换提示态）：导出所取范围与页面展示
        // 不一致，按钮禁用、零请求（与既有写门控同口径）。
        disabled: trustedScopeChanged,
        onExport: (keywordApplied) => { void runExport(keywordApplied) },
      }}
      onEditTargetChange={setEditTarget}
      buildQuery={buildQuery}
      queryRows={queryRows}
      saveRow={saveRow}
      preSubmitBlockReason={preSubmitBlockReason}
    />
    {switchConfirming ? <Modal
      open
      title='可信范围已变化'
      okText='放弃并切换'
      cancelText='取消'
      onOk={completeSwitch}
      onCancel={() => setDismissedTrustedScope(trustedScope)}
    >
      <p>可信范围已变化，切换范围将放弃未保存的金额输入。</p>
    </Modal> : null}
  </>
}
