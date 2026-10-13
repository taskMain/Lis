/**
 * 「接收医院互认使用统计」平台管理员页与「本院互认使用统计」医院管理员页（S6-D11）。
 *
 * 宿主路径：
 * - 平台页 `/subApps/medical-recognition/recognition-usage-statistics`
 * - 本院页 `/subApps/medical-recognition/branch-recognition-usage-statistics`
 *
 * 两页共享同一套统计页主体（StatisticsBoard）与同一套查询/导出接线，差异只在范围形态与固定文案：
 * 平台页接收组与来源组的组织、医院、院区三级都由页面选择并随请求提交（服务端校验取值归属）；
 * 本院页本侧接收组织与医院取可信上下文并固定展示，请求不提交这两项（由服务端从可信上下文注入），
 * 本侧院区恒为登录院区、不提供院区选择（缺失属权限配置错误，页面零业务请求、以空态呈现，
 * 服务端全院统计契约仅为接口面保留（S6-D1），前端已不按其触发），
 * 来源组只提交医院与院区（来源组织恒为可信组织）。
 *
 * 页面不实现角色判断、不做权限二次确认；日期必填，页面默认当月 1 日至当天；
 * 查询条件不完整时适配层构造出口返回 `null`，主体据此零发请求并阻断。
 * 敏感查询条件只随请求提交，不写入地址栏；失败面由宿主统一提示，页面只结束加载态并保留已有数据。
 */
import { Typography } from 'antd'
import { useCallback, useMemo, useState } from 'react'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useTrustedOrganizationScope } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import { useTrustedHospitalScope } from '../reportManagement/reportsTrustedScope'
import {
  buildUsageDetailsQuery,
  buildUsageExportQuery,
  buildUsageSummaryQuery,
  exportUsageStatistics,
  queryRecognitionMatchRecord,
  queryUsageDetailsPage,
  queryUsageSummaryPage,
  type UsageDetailsQuery,
  type UsageExportQuery,
  type UsageSummaryQuery,
} from './recognitionUsageStatisticsApi'
import {
  isStatisticsGroupDimensionValue,
  isUsageDetailTypeValue,
  isUsageExportTypeValue,
  type StatisticsGroupDimensionValue,
  type StatisticsPageInput,
  type UsageDetailTypeValue,
  type UsageExportTypeValue,
} from './recognitionStatisticsValues'
import {
  StatisticsBoard,
  type StatisticsBoardProps,
  type StatisticsFilters,
  type StatisticsScopeSpec,
} from './statisticsBoard'
import { defaultStatisticsFilters } from './statisticsDisplays'

/** 页面版本：平台管理员入口或医院管理员入口；双版本请求分支由适配层按 version 收敛。 */
type StatisticsPageVersion = 'platform' | 'branch'

/** 页面级筛选 → 平台/本院版汇总查询条件的装配。 */
function toSummaryQuery(version: StatisticsPageVersion, filters: StatisticsFilters, dimension: StatisticsGroupDimensionValue): UsageSummaryQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    groupDimension: dimension,
    recognitionDeptId: filters.recognitionDeptId,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    branchCode: filters.ownBranchCode,
    sourceHospitalCode: filters.sourceHospitalCode,
    sourceBranchCode: filters.sourceBranchCode,
  }
}

/** 页面级筛选 → 平台/本院版明细查询条件的装配；互认医生与不采纳原因只进明细查询。 */
function toDetailsQuery(version: StatisticsPageVersion, filters: StatisticsFilters, detailType: UsageDetailTypeValue): UsageDetailsQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    detailType,
    recognitionDeptId: filters.recognitionDeptId,
    recognitionDoctorId: filters.recognitionDoctorId,
    nonAdoptionReasonCode: filters.nonAdoptionReasonCode,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    branchCode: filters.ownBranchCode,
    sourceHospitalCode: filters.sourceHospitalCode,
    sourceBranchCode: filters.sourceBranchCode,
  }
}

/**
 * 页面级筛选 → 导出查询条件的装配。
 *
 * 导出沿用页面当前查询条件（不带分页）：汇总导出只带汇总侧条件；明细导出额外交织
 * 互认医生与不采纳原因两项明细条件（与明细查询口径一致）；导出类型由主体按侧与明细类型换算。
 */
function toExportQuery(version: StatisticsPageVersion, filters: StatisticsFilters, request: {
  exportType: UsageExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
  detailType: number | null
}): UsageExportQuery {
  const isDetailsExport = request.detailType !== null
  const shared = {
    exportType: request.exportType,
    groupDimension: request.groupDimension,
    startTime: filters.startTime,
    endTime: filters.endTime,
    recognitionDeptId: filters.recognitionDeptId,
    recognitionDoctorId: isDetailsExport ? filters.recognitionDoctorId : undefined,
    nonAdoptionReasonCode: isDetailsExport ? filters.nonAdoptionReasonCode : undefined,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    branchCode: filters.ownBranchCode,
    sourceHospitalCode: filters.sourceHospitalCode,
    sourceBranchCode: filters.sourceBranchCode,
    receiverHospitalCode: filters.receiverHospitalCode,
    receiverBranchCode: filters.receiverBranchCode,
  }
}

function UsageStatisticsPage({ version }: { version: StatisticsPageVersion }) {
  const client = useApiClientContext()
  const trustedOrganization = useTrustedOrganizationScope()
  const trustedHospital = useTrustedHospitalScope()
  // 默认统计期间只在挂载时求值一次，作为主体复位与首次自动加载的初始条件。
  const [initialFilters] = useState(defaultStatisticsFilters)

  const organizationCode = trustedOrganization.kind === 'ready' ? trustedOrganization.organizationCode : null
  const hospitalCode = trustedHospital.kind === 'ready' ? trustedHospital.hospitalCode : null
  const trustedBranchCode = trustedHospital.kind === 'ready' ? trustedHospital.branchCode : null

  const querySummaryPage = useCallback((
    filters: StatisticsFilters,
    dimension: number,
    page: StatisticsPageInput,
  ) => {
    if (!isStatisticsGroupDimensionValue(dimension)) return null
    const built = buildUsageSummaryQuery(toSummaryQuery(version, filters, dimension), page)
    return built === null ? null : queryUsageSummaryPage(client, built)
  }, [client, version])

  const queryDetailsPage = useCallback((
    filters: StatisticsFilters,
    detailType: number,
    page: StatisticsPageInput,
  ) => {
    if (!isUsageDetailTypeValue(detailType)) return null
    const built = buildUsageDetailsQuery(toDetailsQuery(version, filters, detailType), page)
    return built === null ? null : queryUsageDetailsPage(client, built)
  }, [client, version])

  const exportWorkbook = useCallback<NonNullable<StatisticsBoardProps['exportWorkbook']>>((filters, request) => {
    const { exportType, groupDimension } = request
    if (!isUsageExportTypeValue(exportType)) return null
    if (!isStatisticsGroupDimensionValue(groupDimension)) return null
    const built = buildUsageExportQuery(toExportQuery(version, filters, {
      exportType,
      groupDimension,
      detailType: request.detailType,
    }))
    return built === null ? null : exportUsageStatistics(client, built, request.downloadFileName)
  }, [client, version])

  const queryMatchRecord = useCallback(
    (recognitionMatchRecordId: string) => queryRecognitionMatchRecord(client, recognitionMatchRecordId),
    [client],
  )

  // 本院页：本侧院区恒为登录院区、不提供院区选择，作为初始条件交给主体；缺失属权限配置错误，查询出口经
  // 适配层构造返回 null 阻断，主体零业务请求并以 missingScopeText 空态呈现（文案与金额、报告页同款）。
  const branchInitialFilters = useMemo<StatisticsFilters>(
    () => ({ ...initialFilters, ownBranchCode: trustedBranchCode ?? undefined }),
    [initialFilters, trustedBranchCode],
  )
  // 登录院区缺失只发生在本院页（平台页院区是可选筛选）：主体保持挂载，权限配置恢复后 scopeKey
  // 随登录院区变化，主体清空条件与数据并按新院区重读。
  const missingBranchScope = version === 'branch' && (trustedBranchCode ?? '').trim() === ''

  if (version === 'branch' && (organizationCode === null || hospitalCode === null)) {
    return <div className='statistics-board'>
      <ScopeUnavailablePanel
        title={
          <div className='statistics-board-header'>
            <div>
              <Typography.Title level={3} style={{ margin: 0 }}>本院互认使用统计</Typography.Title>
              <Typography.Text type='secondary'>查询本院互认使用汇总与明细</Typography.Text>
            </div>
          </div>
        }
      />
    </div>
  }

  const scopeSpec: StatisticsScopeSpec = version === 'platform'
    ? { kind: 'platform' }
    : { kind: 'branch', organizationCode: organizationCode ?? '' }

  return <StatisticsBoard
    title={version === 'platform' ? '接收医院互认使用统计' : '本院互认使用统计'}
    subtitle={version === 'platform'
      ? '按组织、医院与院区查询接收侧互认使用汇总与明细'
      : '查询本院互认使用汇总与明细'}
    side='usage'
    // 范围键含登录院区：院区随可信范围变化即视为范围切换，主体清空条件与数据后按新院区复位。
    scopeKey={version === 'platform'
      ? 'platform-usage'
      : `branch-usage|${organizationCode ?? ''}|${hospitalCode ?? ''}|${trustedBranchCode ?? ''}`}
    scopeSpec={scopeSpec}
    initialFilters={version === 'platform' ? initialFilters : branchInitialFilters}
    missingScopeText={missingBranchScope ? '当前登录身份未携带院区，无法查询本院统计' : undefined}
    querySummaryPage={querySummaryPage}
    queryDetailsPage={queryDetailsPage}
    exportWorkbook={exportWorkbook}
    queryMatchRecord={queryMatchRecord}
  />
}

/** 「接收医院互认使用统计」平台管理员页（宿主路径 `/subApps/medical-recognition/recognition-usage-statistics`）。 */
export function RecognitionUsageStatistics() {
  return <UsageStatisticsPage version='platform' />
}

/** 「本院互认使用统计」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-recognition-usage-statistics`）。 */
export function BranchRecognitionUsageStatistics() {
  return <UsageStatisticsPage version='branch' />
}
