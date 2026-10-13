/**
 * 「本院区互认项目金额」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-amounts`）。
 *
 * 组织与医院取可信上下文并固定展示；院区恒取 `LoginUserManager.getBranchID()` 的登录院区，
 * 不提供院区选择，也不做清单核对、首项回退等解析。查询与保存请求的院区参数恒为该登录院区，
 * 可信院区缺失属权限配置错误：页面不兜底、不本地补默认院区，适配层拒绝构造请求即零业务请求，
 * 页面以空态呈现，恢复正确的权限配置后按登录院区重读。
 * 查询只携带院区与可选关键字，保存载荷只含院区、标准项目编码与金额（组织与医院由服务端从可信上下文
 * 注入）。可信组织或医院缺失时整页阻断：统一接入阻断面板（整页只渲染标题与错误 Alert），不发任何
 * 业务请求，也不伪造组织或降级为空值。页头范围区经共用组件的页头插槽由共用范围条承载：单行只显
 * 「组织 / 医院 / 院区」名称，无编码。页头、查询区、四栏层级列表、金额行内编辑器与页脚计数由
 * 两个金额页共用的 `RecognitionAmountListView` 承载（S3-D29），本页只保留可信范围解析与数据接线。
 * 页面不实现角色判断、不做权限二次确认，也不下推枚举筛选。
 */
import { useCallback, useMemo, useState } from 'react'
import { useAuth } from '@dy/auth-react'
import { LoginUserManager } from '@dy/auth'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readTrustedHospitalCode } from '../reportManagement/reportsTrustedScope'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import {
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  queryBranchRecognitionAmountRows,
  saveBranchRecognitionAmount,
  type BranchAmountListQuery,
} from './recognitionAmountsApi'
import { RecognitionAmountListView, RecognitionAmountsHeader, type RecognitionAmountsScope } from './RecognitionAmountListView'
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

/** C34 提交拦截提示：现读可信组织或医院有一层读不到，零写请求
 * （S3-D28「现读不到」仅指这两层；院区恒随登录院区，不再参与提交前比对）。 */
const TRUSTED_SCOPE_UNREADABLE_REASON = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

/**
 * 医院管理员页：组织与医院固定来自可信上下文；院区恒取登录院区，不提供院区选择，
 * 查询与保存只携带登录院区与可选关键字。
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
   * 宿主重新签发 token 之后、页面重渲染之前回车提交，渲染期取值可能仍是旧可信范围，只有现读才
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
    onEditTargetChange={setEditTarget}
    buildQuery={buildQuery}
    queryRows={queryRows}
    saveRow={saveRow}
    preSubmitBlockReason={preSubmitBlockReason}
  />
}
