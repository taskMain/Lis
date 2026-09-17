/**
 * 「本院区互认项目金额」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-amounts`）。
 *
 * 组织与医院取可信上下文并以只读方式固定展示（对应级不渲染下拉），只有院区可选；请求只提交院区与
 * 标准项目编码，组织与医院由服务端从可信上下文注入。可信组织或医院缺失时整页阻断：不渲染院区下拉、
 * 不允许提交、不发任何业务请求，也不伪造组织或降级为空值。页面不实现角色判断。
 */
import { Alert, Typography } from 'antd'
import { useCallback, useMemo, useState } from 'react'
import { LoginUserManager } from '@dy/auth'
import { useAuth } from '@dy/auth-react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import {
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  queryBranchRecognitionAmountRows,
  saveBranchRecognitionAmount,
  type BranchAmountListQuery,
} from './recognitionAmountsApi'
import { RecognitionAmountsBoard } from './RecognitionAmounts'
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

/** 用户显式选择过的院区，以及在作出该选择时所处的可信范围键。 */
interface BranchSelection {
  scopeKey: string
  branchId: string | undefined
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

export function BranchRecognitionAmounts() {
  const client = useApiClientContext()
  const trusted = useTrustedBranchScope()
  /**
   * 院区取值：用户在**当前**可信范围内的显式选择优先，否则取可信院区默认值。
   *
   * 默认值随可信范围同步，不只在首屏取一次：可信院区在首屏之后才就绪（token 后到）时自动回填；
   * 可信组织或医院不可用或变更时，显式选择与默认值一并作废，不残留旧范围的值。默认可信院区**不**被
   * 记为显式选择，因此用户显式选择后，只有在同一可信范围内才不被覆盖。可信院区缺失时留空，
   * **不**下拉默认选中，须用户显式选择后才构造请求，且不把空值作为请求参数发出（适配层对空院区
   * 返回 `null`）。
   */
  const trustedScopeKey = `${trusted.organizationCode ?? ''}|${trusted.hospitalCode ?? ''}`
  const [branchSelection, setBranchSelection] = useState<BranchSelection | null>(null)
  /** 显式选择只在作出它的可信范围内有效；组织或医院变化即视为旧范围，取值退回可信院区默认值。 */
  const ownSelection = branchSelection !== null && branchSelection.scopeKey === trustedScopeKey
    ? branchSelection
    : null
  const branchId = ownSelection !== null ? ownSelection.branchId : trusted.branchCode ?? undefined

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

  /** 只按院区构成的查询范围；院区缺失即为 `null`（零业务请求）。 */
  const appliedScope = useMemo(() => {
    const query = buildBranchAmountListQuery({ branchCode: branchId ?? '' })
    return query === null ? null : { key: `branch|${query.branchCode}`, query }
  }, [branchId])

  /** 传给选择器的受控值：组织与医院是固定值（同时以 `orgId`/`hosId` 传入，对应级不渲染下拉）。 */
  const scopeValue = useMemo<BaseScopeValue>(() => ({
    orgId: trusted.organizationCode ?? undefined,
    hosId: trusted.hospitalCode ?? undefined,
    branchId,
  }), [branchId, trusted.hospitalCode, trusted.organizationCode])

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

  return <RecognitionAmountsBoard<BranchAmountListQuery>
    title='本院区互认项目金额'
    subtitle={`可信范围（只读）：组织 ${trusted.organizationCode}，医院 ${trusted.hospitalCode}`}
    missingScopeText='请选择院区后查看互认项目金额'
    selector={<BaseScopeSelector
      level='branch'
      className='recognition-amounts-scope'
      orgId={trusted.organizationCode}
      hosId={trusted.hospitalCode}
      value={scopeValue}
      onChange={(next) => setBranchSelection({ scopeKey: trustedScopeKey, branchId: next.branchId ?? undefined })}
    />}
    scope={appliedScope}
    queryRows={queryRows}
    saveRow={saveRow}
  />
}
