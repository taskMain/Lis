/**
 * 「报告管理与历史版本」平台管理员页（入口容器）
 * （宿主路径 `/subApps/medical-recognition/report-management`）。
 *
 * 容器按当前路径是否匹配详情子路由（`detail/:reportId`）条件渲染「列表」或「详情页」：
 * 列表组件保持挂载（详情激活时以 `display:none` 包裹层承载），范围、筛选草稿、已生效条件、
 * 分页与行数据因此跨导航存活（D-03 返回还原的挂载策略）。
 *
 * 列表侧只做范围接入与数据源接线：组织、医院、院区三级都取自范围选择器（受控），三级都以 IAM 组织
 * 业务编码随列表查询请求提交（实例标识只留在选择器内部与受控值里）；列表与服务端分页由两个页面共用的
 * `ReportManagementBoard` 承载。平台管理员入口按请求使用组织、医院与院区，服务端校验三者存在、启用与父子归属。
 *
 * 范围默认值（2026-10-15 修订）：选择器以 `selectFirstOption` 在初始选项加载完成后回调首项组合与选项清单，
 * 页面按可信优先解析——组织取可信组织（不属于清单时取清单第一项），医院与院区同口径逐级解析；
 * 选定组织或医院与本次交付组合不一致时只应用已比对成立的上层、下层留空，
 * 选择器按受控值重载新清单后由下一次回调完成解析（此期间范围不齐，不构造查询、不退化）。
 * 初始解析完成后用户的切换按其选择应用，不覆盖。
 *
 * 页面不实现角色判断、不做权限二次确认，也不在页面创建 API Client；
 * 证件号码与姓名等敏感查询条件只随请求提交，不写入地址栏。
 */
import { useCallback, useMemo, useState } from 'react'
import { OrganizationScopeSelector, type OrganizationScopeChangeMeta, type OrganizationScopeValue } from '../../components/OrganizationScopeSelector'
import { findScopeCode } from '../../components/organizationScope'
import { useMatch } from 'react-router-dom'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readTrustedOrganizationCode } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import { ReportDetailPage } from './ReportDetailPage'
import { ReportManagementBoard, type ReportManagementScope } from './ReportManagementBoard'
import { readTrustedBranchCode, readTrustedHospitalCode } from './reportsTrustedScope'
import { REPORT_MANAGEMENT_DETAIL_PATTERN, REPORT_MANAGEMENT_ROUTE } from './reportManagementRoutes'
import { useReportDetailApi } from './useReportDetailApi'
import {
  buildReportListQuery,
  queryReportPage,
  type ReportFilters,
  type ReportListQuery,
} from './reportsApi'
import './ReportManagement.css'

/**
 * 三级选择值：按实例标识从本次交付的选项清单取该层业务编码，请求侧只取业务编码。
 * 标识不在清单中（范围正在重载）时该层编码为 null，页面据此不构造查询，不回落实例标识。
 */
function toScopeValue(
  ids: { orgId: string; hosId: string; branchId: string },
  meta: OrganizationScopeChangeMeta,
): OrganizationScopeValue {
  return {
    orgId: ids.orgId,
    hosId: ids.hosId,
    branchId: ids.branchId,
    orgCode: findScopeCode(meta.organizations, ids.orgId) ?? undefined,
    hosCode: findScopeCode(meta.hospitals, ids.hosId) ?? undefined,
    branchCode: findScopeCode(meta.branches, ids.branchId) ?? undefined,
  }
}

/**
 * 初始范围解析（可信优先）：组织取可信组织（按业务编码在本次交付清单中核对），
 * 不属于清单时取清单第一项；医院与院区按同一口径逐级解析，归属以本次交付的清单核对。
 * 选定组织或医院与本次交付组合不一致时，下层清单尚未按它装载，只应用已比对成立的上层、下层留空，
 * 由重载后的下一次回调完成解析。
 */
function resolveInitialReportScope(next: OrganizationScopeValue, meta: OrganizationScopeChangeMeta): OrganizationScopeValue {
  const firstOrgId = next.orgId ?? ''
  const trustedOrganizationCode = readTrustedOrganizationCode()
  const trustedOrganizationId = trustedOrganizationCode !== null
    ? meta.organizations.find((item) => item.code === trustedOrganizationCode)?.id
    : undefined
  const organizationId = trustedOrganizationId ?? firstOrgId
  if (organizationId !== firstOrgId) {
    return toScopeValue({ orgId: organizationId, hosId: '', branchId: '' }, meta)
  }
  const firstHosId = next.hosId ?? ''
  const trustedHospitalCode = readTrustedHospitalCode()
  const trustedHospitalId = trustedHospitalCode !== null
    ? meta.hospitals.find((item) => item.code === trustedHospitalCode)?.id
    : undefined
  const hospitalId = trustedHospitalId ?? firstHosId
  if (hospitalId !== firstHosId) {
    return toScopeValue({ orgId: organizationId, hosId: hospitalId, branchId: '' }, meta)
  }
  const trustedBranchCode = readTrustedBranchCode()
  const trustedBranchId = trustedBranchCode !== null
    ? meta.branches.find((item) => item.code === trustedBranchCode)?.id
    : undefined
  const branchId = trustedBranchId ?? (next.branchId ?? '')
  return toScopeValue({ orgId: organizationId, hosId: hospitalId, branchId }, meta)
}

export function ReportManagement() {
  const client = useApiClientContext()
  const [scope, setScope] = useState<OrganizationScopeValue>({})

  /**
   * 范围变化：初始解析阶段（三级尚未齐备）按可信优先解析；解析完成后的切换按用户选择应用。
   * 三级任一缺失的回调不被接受，保持原受控值——避免退化为不完整范围或空范围请求。
   */
  const handleScopeChange = useCallback((next: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => {
    if (!next.orgId || !next.hosId || !next.branchId) return
    const resolving = !scope.orgId || !scope.hosId || !scope.branchId
    setScope(resolving ? resolveInitialReportScope(next, meta) : next)
  }, [scope.branchId, scope.hosId, scope.orgId])

  /** 页容量 1 只用于判断范围是否已选全：范围不完整时返回 `null`，不发起部分范围请求。 */
  const appliedScope = useMemo<ReportManagementScope<ReportListQuery> | null>(() => {
    const probe = buildReportListQuery(
      { organizationCode: scope.orgCode, hospitalCode: scope.hosCode, branchCode: scope.branchCode },
      { pageIndex: 1, pageSize: 1 },
    )
    if (probe === null) return null
    return {
      key: `${probe.organizationCode}|${probe.hospitalCode}|${probe.branchCode}`,
      query: probe,
    }
  }, [scope.branchCode, scope.hosCode, scope.orgCode])

  /**
   * 请求构造只经适配层出口：三级范围取页面已选定的组织、医院与院区的业务编码，当次筛选条件与分页参数一并交给该出口。
   * 三级范围不完整时 `appliedScope` 为 `null`，主体据此不发起任何请求，因此这里只在范围齐备后才会被调用。
   */
  const queryPage = useCallback(
    (_query: ReportListQuery, filters: ReportFilters, page: { pageIndex: number; pageSize: number }) => {
      const built = buildReportListQuery(
        {
          ...filters,
          organizationCode: scope.orgCode,
          hospitalCode: scope.hosCode,
          branchCode: scope.branchCode,
        },
        page,
      )

      return queryReportPage(client, built as ReportListQuery)
    },
    [client, scope.branchCode, scope.hosCode, scope.orgCode],
  )
  /** 详情页三个 I/O 出口的共享接线（版本列表、版本详情、PDF 下载），注入详情路由页使用。 */
  const { queryVersions, queryDetail, downloadVersion } = useReportDetailApi()

  /** 详情子路由激活判定（D-01）：容器按 URL 条件渲染「列表」或「详情页」，列表保持挂载。 */
  const detailMatch = useMatch(REPORT_MANAGEMENT_DETAIL_PATTERN)
  const detailReportId = detailMatch?.params.reportId

  const board = <ReportManagementBoard<ReportListQuery>
    title='报告管理与历史版本'
    subtitle='按组织、医院与院区查询报告，查看任一版本的完整内容并下载原始 PDF'
    missingScopeText='请选择组织、医院与院区后查看报告'
    selector={<OrganizationScopeSelector
      className='report-management-scope'
      selectFirstOption
      value={scope}
      onChange={handleScopeChange}
    />}
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
        parentPath={REPORT_MANAGEMENT_ROUTE}
        parentName='报告管理'
        queryVersions={queryVersions}
        queryDetail={queryDetail}
        downloadVersion={downloadVersion}
      />
    )}
  </>
}
