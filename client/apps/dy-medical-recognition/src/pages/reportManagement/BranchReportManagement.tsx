/**
 * 「本院报告管理与历史版本」医院管理员页
 * （宿主路径 `/subApps/medical-recognition/branch-report-management`）。
 *
 * 组织与医院取可信上下文并固定展示；院区恒取 `LoginUserManager.getBranchID()` 的登录院区（D-05），
 * 不提供院区选择，查询请求只携带登录院区与筛选条件（组织与医院由服务端从可信上下文注入）。
 * 登录院区缺失属权限配置错误：页面不兜底、不伪造院区、不本地补默认，适配层对空院区拒绝构造请求
 * （`buildBranchReportListQuery` 返回 `null`，范围不可用），页面零业务请求并以空态呈现，
 * 恢复权限配置后按登录院区重读。可信组织或医院缺失时整页阻断：统一接入阻断面板（整页只渲染
 * 标题与错误 Alert）、不发任何业务请求，也不伪造组织或降级为空值。页头范围区由共用范围条承载：
 * 单行只显「组织 / 医院 / 院区」名称，无编码。列表、分页、详情、只读历史版本与下载都复用同一套组件。
 */
import { Typography } from 'antd'
import { useCallback, useMemo } from 'react'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
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

export function BranchReportManagement() {
  const client = useApiClientContext()
  const trustedOrganization = useTrustedOrganizationScope()
  const trustedHospital = useTrustedHospitalScope()
  const organizationCode = trustedOrganization.kind === 'ready' ? trustedOrganization.organizationCode : null
  const hospitalCode = trustedHospital.kind === 'ready' ? trustedHospital.hospitalCode : null
  const trustedBranchCode = trustedHospital.kind === 'ready' ? trustedHospital.branchCode : null
  /** 院区恒取登录院区（D-05）；缺失时为 `undefined`，由适配层拒绝构造请求实现零业务请求。 */
  const branchId = trustedBranchCode ?? undefined

  /**
   * 范围键含可信三层（组织、医院、院区）：任一层变化即视为范围切换，主体据此清空旧数据与未提交
   * 筛选、整表重读，不弹确认框。登录院区缺失时适配层拒绝构造请求（返回 `null`），范围不可用即
   * 零业务请求，页面以空态呈现。
   */
  const appliedScope = useMemo<ReportManagementScope<BranchReportListQuery> | null>(() => {
    const probe = buildBranchReportListQuery({ branchCode: branchId }, { pageIndex: 1, pageSize: 1 })
    if (probe === null) return null
    return { key: `branch|${organizationCode ?? ''}|${hospitalCode ?? ''}|${probe.branchCode}`, query: probe }
  }, [branchId, hospitalCode, organizationCode])

  /** 请求只提交登录院区与筛选条件；可信组织与医院不回填，由服务端从可信上下文注入。 */
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

  if (organizationCode === null || hospitalCode === null) {
    return <div className='report-management-page'>
      <ScopeUnavailablePanel
        title={
          <div className='report-management-header'>
            <div>
              <Typography.Title level={3}>本院报告管理与历史版本</Typography.Title>
              <Typography.Text type='secondary'>查看本院报告与历史版本，并下载原始 PDF</Typography.Text>
            </div>
          </div>
        }
      />
    </div>
  }

  return <ReportManagementBoard<BranchReportListQuery>
    title='本院报告管理与历史版本'
    subtitle={<CurrentScopeBar />}
    // D-05：本院页无范围选择器（院区恒取登录院区），选择器位置传空即不渲染。
    selector={null}
    // 登录院区缺失（权限配置错误）时的空态文案，与金额本院页同口径。
    missingScopeText='当前登录身份未携带院区，无法查询本院报告'
    scope={appliedScope}
    queryPage={queryPage}
    queryVersions={queryVersions}
    queryDetail={queryDetail}
    downloadVersion={downloadVersion}
  />
}
