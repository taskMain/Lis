/**
 * 「本院报告管理与历史版本」医院管理员页
 * （宿主路径 `/subApps/medical-recognition/branch-report-management`）。
 *
 * 与平台管理员页的差异只在取值来源与范围固定：组织与医院取自可信上下文并以只读方式固定展示
 * （对应级不渲染下拉、请求也不提交这两项），只让院区可选且必须属于可信医院；
 * 院区归属由服务端拒绝不属于可信医院的取值，页面不做本地归属判断。
 *
 * 可信组织或医院缺失时整页阻断：给出阻断提示、不渲染院区选择、不发任何业务请求，
 * 也不伪造组织或降级为空值。列表、分页、详情、只读历史版本与下载都复用同一套组件。
 */
import { Alert, Typography } from 'antd'
import { useCallback, useMemo, useState } from 'react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useTrustedOrganizationScope } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import { useTrustedHospitalScope } from './reportsTrustedScope'
import { ReportManagementBoard, type ReportManagementScope } from './ReportManagementBoard'
import {
  buildBranchReportListQuery,
  downloadReportVersionPdf,
  queryBranchReportPage,
  queryReportVersionDetail,
  queryReportVersions,
  type BranchReportListQuery,
  type ReportFilters,
  type ReportVersionRow,
} from './reportsApi'
import './ReportManagement.css'

/** 用户显式选择过的院区，以及在作出该选择时所处的可信范围键。 */
interface BranchSelection {
  scopeKey: string
  branchId: string | undefined
}

export function BranchReportManagement() {
  const client = useApiClientContext()
  const trustedOrganization = useTrustedOrganizationScope()
  const trustedHospital = useTrustedHospitalScope()
  const organizationCode = trustedOrganization.kind === 'ready' ? trustedOrganization.organizationCode : null
  const hospitalCode = trustedHospital.kind === 'ready' ? trustedHospital.hospitalCode : null
  const trustedBranchCode = trustedHospital.kind === 'ready' ? trustedHospital.branchCode : null

  /**
   * 院区取值：用户在**当前**可信范围内的显式选择优先，否则取可信院区默认值。
   *
   * 默认值随可信范围同步，不只在首屏取一次：可信院区在首屏之后才就绪（token 后到）时自动回填；
   * 可信组织或医院不可用或变更时，显式选择与默认值一并作废，不残留旧范围的值。
   * 可信院区缺失时留空，不默认选中，须用户显式选择后才构造请求（适配层对空院区返回 `null`）。
   */
  const trustedScopeKey = `${organizationCode ?? ''}|${hospitalCode ?? ''}`
  const [branchSelection, setBranchSelection] = useState<BranchSelection | null>(null)
  const ownSelection = branchSelection !== null && branchSelection.scopeKey === trustedScopeKey ? branchSelection : null
  const branchId = ownSelection !== null ? ownSelection.branchId : trustedBranchCode ?? undefined

  /** 范围键只由院区构成：院区变化即视为范围切换，主体据此清空旧范围的数据与筛选并回到第 1 页。 */
  const appliedScope = useMemo<ReportManagementScope<BranchReportListQuery> | null>(() => {
    const probe = buildBranchReportListQuery({ branchCode: branchId }, { pageIndex: 1, pageSize: 1 })
    if (probe === null) return null
    return { key: `branch|${probe.branchCode}`, query: probe }
  }, [branchId])

  /** 请求只提交院区与筛选条件；可信组织与医院不回填，由服务端从可信上下文注入。 */
  const queryPage = useCallback(
    (_query: BranchReportListQuery, filters: ReportFilters, page: { pageIndex: number; pageSize: number }) =>
      queryBranchReportPage(
        client,
        buildBranchReportListQuery({ ...filters, branchCode: branchId }, page) as BranchReportListQuery,
      ),
    [branchId, client],
  )
  const queryVersions = useCallback((reportId: string) => queryReportVersions(client, reportId), [client])
  const queryDetail = useCallback(
    (reportId: string, reportVersionId: string) => queryReportVersionDetail(client, reportId, reportVersionId),
    [client],
  )
  const downloadVersion = useCallback(
    async (reportId: string, version: ReportVersionRow) => {
      await downloadReportVersionPdf(client, reportId, version.reportVersionId, version.pdfFileName)
    },
    [client],
  )

  /** 传给选择器的受控值：组织与医院是固定值（同时以 `orgId`/`hosId` 传入，对应级不渲染下拉）。 */
  const scopeValue = useMemo<BaseScopeValue>(() => ({
    orgId: organizationCode ?? undefined,
    hosId: hospitalCode ?? undefined,
    branchId,
  }), [branchId, hospitalCode, organizationCode])

  if (organizationCode === null || hospitalCode === null) {
    return <div className='report-management-page'>
      <div className='report-management-header'>
        <div>
          <Typography.Title level={2}>本院报告管理与历史版本</Typography.Title>
          <Typography.Text type='secondary'>查看本院报告与历史版本，并下载原始 PDF</Typography.Text>
        </div>
      </div>
      <Alert
        type='warning'
        showIcon
        title='可信范围不可用'
        description='当前无法从登录凭证确定可信组织与医院，因此不能读取本院的报告与历史版本。请从宿主登录后重新进入本页面。'
      />
    </div>
  }

  return <ReportManagementBoard<BranchReportListQuery>
    title='本院报告管理与历史版本'
    subtitle={`可信范围（只读）：组织 ${organizationCode}，医院 ${hospitalCode}`}
    missingScopeText='请选择院区后查看报告'
    selector={<BaseScopeSelector
      level='branch'
      className='report-management-scope'
      orgId={organizationCode}
      hosId={hospitalCode}
      value={scopeValue}
      onChange={(next: BaseScopeValue) => setBranchSelection({ scopeKey: trustedScopeKey, branchId: next.branchId })}
    />}
    scope={appliedScope}
    queryPage={queryPage}
    queryVersions={queryVersions}
    queryDetail={queryDetail}
    downloadVersion={downloadVersion}
  />
}
