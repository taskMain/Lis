/**
 * 「本院报告管理与历史版本」医院管理员页（入口容器）
 * （宿主路径 `/subApps/medical-recognition/branch-report-management`）。
 *
 * 容器按当前路径是否匹配详情子路由（`detail/:reportId`）条件渲染「列表」或「详情页」：
 * 列表组件保持挂载（详情激活时以 `display:none` 包裹层承载），范围、筛选草稿、已生效条件、
 * 分页与行数据因此跨导航存活（D-03 返回还原的挂载策略）。
 *
 * 组织与医院取可信上下文并固定展示；院区恒取 `LoginUserManager.getBranchID()` 的登录院区，
 * 不提供院区选择，查询请求只携带登录院区与筛选条件（组织与医院由服务端从可信上下文注入）。
 * 登录院区缺失属权限配置错误：页面不兜底、不伪造院区、不本地补默认，适配层对空院区拒绝构造请求
 * （`buildBranchReportListQuery` 返回 `null`，范围不可用），页面零业务请求并以空态呈现，
 * 恢复权限配置后按登录院区重读。可信组织或医院缺失时整页阻断：统一接入阻断面板（整页只渲染
 * 标题、描述副标题与错误 Alert）、不发任何业务请求，也不伪造组织或降级为空值。页头为左右结构：当前登录范围条
 * 由页头右侧插槽承载（与「本院区互认项目金额」页同一页头结构，S4-D46），单行只显「组织 / 医院 / 院区」
 * 名称，无编码。列表、分页与只读历史版本复用同一套列表主体；
 * 详情由共用的详情路由页承载，数据范围同本院列表入口。
 */
import { Typography } from 'antd'
import { useCallback, useMemo } from 'react'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useMatch } from 'react-router-dom'
import { useTrustedOrganizationScope } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import { useTrustedHospitalScope } from './reportsTrustedScope'
import { BRANCH_REPORT_MANAGEMENT_DETAIL_PATTERN, BRANCH_REPORT_MANAGEMENT_ROUTE } from './reportManagementRoutes'
import { ReportDetailPage } from './ReportDetailPage'
import { ReportManagementBoard, type ReportManagementScope } from './ReportManagementBoard'
import { useReportDetailApi } from './useReportDetailApi'
import {
  buildBranchReportListQuery,
  queryBranchReportPage,
  type BranchReportListQuery,
  type ReportFilters,
} from './reportsApi'
import './ReportManagement.css'

/** 页头文案：正常态列表页头与阻断态面板共用同一标题与描述副标题（S4-D46 页头左右结构）。 */
const PAGE_TITLE = '本院报告管理与历史版本'
const PAGE_SUBTITLE = '查看本院报告与历史版本，并下载原始 PDF'

export function BranchReportManagement() {
  const client = useApiClientContext()
  const trustedOrganization = useTrustedOrganizationScope()
  const trustedHospital = useTrustedHospitalScope()
  const organizationCode = trustedOrganization.kind === 'ready' ? trustedOrganization.organizationCode : null
  const hospitalCode = trustedHospital.kind === 'ready' ? trustedHospital.hospitalCode : null
  const trustedBranchCode = trustedHospital.kind === 'ready' ? trustedHospital.branchCode : null
  /** 院区恒取登录院区（不提供院区选择）；缺失时为 `undefined`，由适配层拒绝构造请求实现零业务请求。 */
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
  /** 详情页三个 I/O 出口的共享接线（版本列表、版本详情、PDF 下载），注入详情路由页使用。 */
  const { queryVersions, queryDetail, downloadVersion } = useReportDetailApi()

  /** 详情子路由激活判定（D-01）：容器按 URL 条件渲染「列表」或「详情页」，列表保持挂载。 */
  const detailMatch = useMatch(BRANCH_REPORT_MANAGEMENT_DETAIL_PATTERN)
  const detailReportId = detailMatch?.params.reportId

  if (organizationCode === null || hospitalCode === null) {
    return <div className='report-management-page'>
      <ScopeUnavailablePanel
        title={
          <div className='report-management-header'>
            <div>
              <Typography.Title level={3}>{PAGE_TITLE}</Typography.Title>
              <Typography.Text type='secondary'>{PAGE_SUBTITLE}</Typography.Text>
            </div>
          </div>
        }
      />
    </div>
  }

  const board = <ReportManagementBoard<BranchReportListQuery>
    title={PAGE_TITLE}
    subtitle={PAGE_SUBTITLE}
    headerExtra={<CurrentScopeBar />}
    // 本院页无范围选择器（院区恒取登录院区、不提供院区选择），选择器位置传空即不渲染。
    selector={null}
    // 登录院区缺失（权限配置错误）时的空态文案，与金额本院页同口径。
    missingScopeText='当前登录身份未携带院区，无法查询本院报告'
    scope={appliedScope}
    detailActive={detailMatch !== null}
    queryPage={queryPage}
  />

  return <>
    {/* 详情激活时列表仅隐藏（display:none）不卸载：范围、筛选草稿、分页与数据跨导航存活（D-03）。 */}
    <div style={detailMatch === null ? undefined : { display: 'none' }}>
      {board}
    </div>
    {detailMatch === null || detailReportId === undefined ? null : (
      <ReportDetailPage
        key={detailReportId}
        reportId={detailReportId}
        parentPath={BRANCH_REPORT_MANAGEMENT_ROUTE}
        parentName='本院报告管理'
        queryVersions={queryVersions}
        queryDetail={queryDetail}
        downloadVersion={downloadVersion}
      />
    )}
  </>
}
