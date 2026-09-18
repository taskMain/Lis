/**
 * 「报告管理与历史版本」平台管理员页
 * （宿主路径 `/subApps/medical-recognition/report-management`）。
 *
 * 页面只做范围接入与数据源接线：组织、医院、院区三级都取自范围选择器（受控），三个值随列表查询请求提交；
 * 列表、服务端分页与详情区域由两个页面共用的 `ReportManagementBoard` 承载
 * （票 14、15 复用同一套列表与详情组件）。平台管理员入口按请求使用组织、医院与院区，
 * 服务端校验三者存在、启用与父子归属。
 *
 * 页面不实现角色判断、不做权限二次确认，也不在页面创建 API Client；
 * 证件号码与姓名等敏感查询条件只随请求提交，不写入地址栏。
 */
import { useCallback, useMemo, useState } from 'react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { ReportManagementBoard, type ReportManagementScope } from './ReportManagementBoard'
import {
  buildReportListQuery,
  downloadReportVersionPdf,
  queryReportPage,
  queryReportVersionDetail,
  queryReportVersions,
  type ReportFilters,
  type ReportListQuery,
  type ReportVersionRow,
} from './reportsApi'
import './ReportManagement.css'

export function ReportManagement() {
  const client = useApiClientContext()
  const [scope, setScope] = useState<BaseScopeValue>({})

  /** 页容量 1 只用于判断范围是否已选全：范围不完整时返回 `null`，不发起部分范围请求。 */
  const appliedScope = useMemo<ReportManagementScope<ReportListQuery> | null>(() => {
    const probe = buildReportListQuery(
      { organizationCode: scope.orgId, hospitalCode: scope.hosId, branchCode: scope.branchId },
      { pageIndex: 1, pageSize: 1 },
    )
    if (probe === null) return null
    return {
      key: `${probe.organizationCode}|${probe.hospitalCode}|${probe.branchCode}`,
      query: probe,
    }
  }, [scope.branchId, scope.hosId, scope.orgId])

  /**
   * 请求构造只经适配层出口：三级范围取页面已选定的组织、医院与院区，当次筛选条件与分页参数一并交给该出口。
   * 三级范围不完整时 `appliedScope` 为 `null`，主体据此不发起任何请求，因此这里只在范围齐备后才会被调用。
   */
  const queryPage = useCallback(
    (_query: ReportListQuery, filters: ReportFilters, page: { pageIndex: number; pageSize: number }) => {
      const built = buildReportListQuery(
        {
          ...filters,
          organizationCode: scope.orgId,
          hospitalCode: scope.hosId,
          branchCode: scope.branchId,
        },
        page,
      )

      return queryReportPage(client, built as ReportListQuery)
    },
    [client, scope.branchId, scope.hosId, scope.orgId],
  )
  const queryVersions = useCallback((reportId: string) => queryReportVersions(client, reportId), [client])
  const queryDetail = useCallback(
    (reportId: string, reportVersionId: string) => queryReportVersionDetail(client, reportId, reportVersionId),
    [client],
  )
  /** 下载名取该版本保存的下载名，由适配层按该名触发浏览器保存。 */
  const downloadVersion = useCallback(
    async (reportId: string, version: ReportVersionRow) => {
      await downloadReportVersionPdf(client, reportId, version.reportVersionId, version.pdfFileName)
    },
    [client],
  )

  return <ReportManagementBoard<ReportListQuery>
    title='报告管理与历史版本'
    subtitle='按组织、医院与院区查询报告，查看任一版本的完整内容并下载原始 PDF'
    missingScopeText='请选择组织、医院与院区后查看报告'
    selector={<BaseScopeSelector
      level='branch'
      className='report-management-scope'
      value={scope}
      onChange={(next: BaseScopeValue) => setScope(next)}
    />}
    scope={appliedScope}
    queryPage={queryPage}
    queryVersions={queryVersions}
    queryDetail={queryDetail}
    downloadVersion={downloadVersion}
  />
}
