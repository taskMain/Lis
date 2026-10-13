/**
 * 「互认项目金额维护」平台管理员页（宿主路径 `/subApps/medical-recognition/recognition-amounts`）。
 *
 * 页面本身只做范围接入与数据源接线：组织、医院、院区三级取自范围选择器（受控保存，院区由页面按
 * 默认规则解析且恒定单值），查询与保存请求都携带这三个值；页头、查询区、四栏层级列表与金额行内
 * 编辑由两个金额页共用的 `RecognitionAmountListView` 承载（S3-D29），页面只把范围与数据出口接上去。
 * 本页专属的两道防线状态（可信范围变化的确认框与待切换提示态、保存前的现值复核）留在页面，
 * 经共用组件的挂起态与写前复核属性接入。页面不实现角色判断、不做权限二次确认，也不下推枚举筛选。
 */
import { Button, Modal } from 'antd'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuth } from '@dy/auth-react'
import { BaseScopeSelector, type BaseScopeSelectorChangeMeta, type BaseScopeValue } from '@dy/components-base'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readTrustedBranchCode, readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
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
 * 院区按 S3-D22 规则解析：可信院区属于给定院区清单则选中它，可信院区缺失或不属于该清单时
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
 * 平台管理员页的初始范围解析：只在可信组织与可信医院**齐备**时执行——任一缺失时页面整页阻断、
 * 选择器不挂载，本函数不会被调用（缺失场景不存在「级联取首项」的回退解析）。解析口径：
 * 组织取可信组织（不属于选项清单时取本次交付的第一项），医院取可信医院且须属于选定组织
 * （不属于清单时取该院第一项），院区取可信院区且须属于当前组织、医院（缺失或不属于时取新范围第一项）。
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
 * 平台管理员页：组织、医院可切换，院区恒定单值，三个值随查询与保存请求提交。
 * 页头只保留标题与副标题，不展示组织、医院、院区范围（S3-D23 修订：正文自带三级范围
 * 选择器，页头重复展示同一信息属冗余）。
 *
 * 可信组织或医院任一缺失（含 token 缺失）即整页阻断：整页只渲染标题与错误 Alert，
 * 不渲染选择器、不发起任何请求，也不伪造组织或退化为全局查询。
 * 范围值由页面**受控**保存并按默认规则解析：`BaseScopeSelector` 以 `selectFirstOption` 在初始
 * 选项加载完成后回调首项组合与选项清单，页面按 S3-D22 完成初始解析——组织、医院、院区各自
 * 缺失或不属于上级清单时取该层第一项，选定组织或医院与交付组合不一致时先留空院区、等选择器
 * 按受控值重载新清单后完成第二轮解析；在此之上页面还做三件事——三值任一缺失的回调保持原受控值（院区永不为
 * 空发请求）、切换组织医院时把属于新范围的可信院区作为默认值（同一组织医院内的显式改选不被
 * 覆盖），以及在可信范围变化时按未保存内容的有无决定直接切换还是
 * 先弹确认（C33）；C33 的变化判定在**渲染期**现读可信院区派生，宿主单独重新签发 token、用户
 * 不碰选择器时同样触发，选择器回调里的现读比对只作第二道。
 * 写请求只在用户回车提交行内编辑时发出。
 */
export function RecognitionAmounts() {
  const client = useApiClientContext()
  const { token } = useAuth()
  /** 现读可信院区（渲染期接缝）：宿主重新签发 token 后由订阅推进，在渲染期现读比对。 */
  const trustedBranch = useTrustedBranchCode()
  // 阻断判定与既有可信范围接缝同一口径：token 缺失时凭据不表达可信范围；
  // 组织、医院编码 trim 后为空白即不可用（院区缺失不阻断）。
  const trustedOrganizationCode = token ? readTrustedOrganizationCode() : null
  const trustedHospitalCode = token ? readTrustedHospitalCode() : null
  const scopeBlocked = trustedOrganizationCode === null || trustedHospitalCode === null

  const [selection, setSelection] = useState<ScopeSelection | null>(null)
  /** 行内编辑目标行的项目编码；共用列表视图负责在目标行离开当前行集时清空它。
   *  未保存内容消失后待切换提示态的自动完成由上面的可信变化效果按派生状态处理（C33）。 */
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
   * 医院不变，按 S3-D22 规则重取院区（可信院区属于当前组织医院则选中它，否则取该院区清单
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
   * 可信院区」与「保存发起前现读的可信院区」；记录与现读不一致即零写请求（院区层两侧同为缺失
   * 视为一致），返回的原因经共用列表视图以 message.error 就近提示，
   * 单元格内不再渲染校验原因。组织、医院两层本页没有对应的可信现读接缝，按 null 传入且不参与
   * 比较（layers 只含院区一层）；比对对象是可信范围的变化，用户选定的 `scope.query` 三值不参与。
   */
  const preSubmitBlockReason = useCallback(() => {
    const recheck = recheckRecognitionAmountScope({
      applied: { organizationCode: null, hospitalCode: null, branchCode: appliedTrustedBranch ?? null },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: readTrustedBranchCode() },
      layers: ['branch'],
    })
    // 拦截原因由页面书写并经 message.error 提示，不透传适配层交付的原因串。
    return recheck.ok ? null : TRUSTED_SCOPE_SUBMIT_BLOCK_REASON
  }, [appliedTrustedBranch])

  const organizationCode = selection?.scope.orgId ?? null
  const hospitalCode = selection?.scope.hosId ?? null
  const branchCode = selection?.scope.branchId ?? null
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
      selector={<BaseScopeSelector
        level='branch'
        className='recognition-amounts-scope'
        selectFirstOption
        value={selection?.scope ?? {}}
        onChange={handleSelectorChange}
      />}
      scopePrompt={scopePrompt}
      scope={appliedScope}
      editTarget={editTarget}
      writeSuspended={switchPending}
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
      onCancel={() => setDismissedTrustedBranch(trustedBranch)}
    >
      <p>可信范围已变化，切换范围将放弃未保存的金额输入。</p>
    </Modal> : null}
  </>
}
