/**
 * 「来源医院被互认统计」平台管理员页与「本院被互认统计」医院管理员页（S6-D11）。
 *
 * 宿主路径：
 * - 平台页 `/subApps/medical-recognition/source-recognition-statistics`
 * - 本院页 `/subApps/medical-recognition/branch-source-recognition-statistics`
 *
 * 与接收侧页面同构（共享 StatisticsBoard），差异在数据口径与范围形态：
 * 只统计被互认次数，无金额、无明细类型切换与不采纳原因区（C22）；
 * 平台页来源组与接收组的组织、医院、院区三级都由页面选择并随请求提交；
 * 本院页来源组织与来源医院取可信上下文并固定展示，请求不提交这两项（由服务端注入），
 * 来源院区可选（为空按可信医院全部来源院区统计），接收组只提交医院与院区（接收组织恒为可信组织）。
 * 来源侧明细不提供匹配记录入口（S6-D10），页面不注入匹配记录查询出口。
 */
import { Alert, Typography } from 'antd'
import { useCallback, useState } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useTrustedOrganizationScope } from '../recognitionProjects/recognitionProjectsOrganizationScope'
import { useTrustedHospitalScope } from '../reportManagement/reportsTrustedScope'
import {
  buildSourceDetailsQuery,
  buildSourceExportQuery,
  buildSourceSummaryQuery,
  exportSourceStatistics,
  querySourceDetailsPage,
  querySourceSummaryPage,
  type SourceDetailsQuery,
  type SourceExportQuery,
  type SourceSummaryQuery,
} from './sourceRecognitionStatisticsApi'
import {
  isSourceExportTypeValue,
  isStatisticsGroupDimensionValue,
  type SourceExportTypeValue,
  type StatisticsGroupDimensionValue,
  type StatisticsPageInput,
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

/** 页面级筛选 → 平台/本院版来源侧汇总查询条件的装配。 */
function toSummaryQuery(version: StatisticsPageVersion, filters: StatisticsFilters, dimension: StatisticsGroupDimensionValue): SourceSummaryQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    groupDimension: dimension,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    sourceBranchCode: filters.ownBranchCode,
    receiverHospitalCode: filters.receiverHospitalCode,
    receiverBranchCode: filters.receiverBranchCode,
  }
}

/** 页面级筛选 → 平台/本院版来源侧明细查询条件的装配；来源侧没有明细类型与医生、原因筛选。 */
function toDetailsQuery(version: StatisticsPageVersion, filters: StatisticsFilters): SourceDetailsQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    sourceBranchCode: filters.ownBranchCode,
    receiverHospitalCode: filters.receiverHospitalCode,
    receiverBranchCode: filters.receiverBranchCode,
  }
}

/** 页面级筛选 → 来源侧导出查询条件的装配（沿用当前查询条件，不带分页）。 */
function toExportQuery(version: StatisticsPageVersion, filters: StatisticsFilters, request: {
  exportType: SourceExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
}): SourceExportQuery {
  const shared = {
    exportType: request.exportType,
    groupDimension: request.groupDimension,
    startTime: filters.startTime,
    endTime: filters.endTime,
    itemType: filters.itemType,
    standardProjectCode: filters.standardProjectCode,
  }
  if (version === 'platform') {
    return {
      ...shared,
      version: 'platform',
      sourceOrganizationCode: filters.sourceOrganizationCode,
      sourceHospitalCode: filters.sourceHospitalCode,
      sourceBranchCode: filters.sourceBranchCode,
      receiverOrganizationCode: filters.receiverOrganizationCode,
      receiverHospitalCode: filters.receiverHospitalCode,
      receiverBranchCode: filters.receiverBranchCode,
    }
  }
  return {
    ...shared,
    version: 'branch',
    sourceBranchCode: filters.ownBranchCode,
    receiverHospitalCode: filters.receiverHospitalCode,
    receiverBranchCode: filters.receiverBranchCode,
  }
}

function SourceStatisticsPage({ version }: { version: StatisticsPageVersion }) {
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
    const built = buildSourceSummaryQuery(toSummaryQuery(version, filters, dimension), page)
    return built === null ? null : querySourceSummaryPage(client, built)
  }, [client, version])

  const queryDetailsPage = useCallback((
    filters: StatisticsFilters,
    _detailType: number,
    page: StatisticsPageInput,
  ) => {
    const built = buildSourceDetailsQuery(toDetailsQuery(version, filters), page)
    return built === null ? null : querySourceDetailsPage(client, built)
  }, [client, version])

  const exportWorkbook = useCallback<NonNullable<StatisticsBoardProps['exportWorkbook']>>((filters, request) => {
    const { exportType, groupDimension } = request
    if (!isSourceExportTypeValue(exportType)) return null
    if (!isStatisticsGroupDimensionValue(groupDimension)) return null
    const built = buildSourceExportQuery(toExportQuery(version, filters, { exportType, groupDimension }))
    return built === null ? null : exportSourceStatistics(client, built, request.downloadFileName)
  }, [client, version])

  if (version === 'branch' && (organizationCode === null || hospitalCode === null)) {
    return <div className='statistics-board'>
      <div className='statistics-board-header'>
        <div>
          <Typography.Title level={2}>本院被互认统计</Typography.Title>
          <Typography.Text type='secondary'>查询本院被互认的汇总与明细；来源院区不选择时按可信医院全院统计</Typography.Text>
        </div>
      </div>
      <Alert
        type='warning'
        showIcon
        title='可信范围不可用'
        description='当前无法从登录凭证确定可信组织与医院，因此不能读取本院的统计数据。请从宿主登录后重新进入本页面。'
      />
    </div>
  }

  const scopeSpec: StatisticsScopeSpec = version === 'platform'
    ? { kind: 'platform' }
    : {
        kind: 'branch',
        organizationCode: organizationCode ?? '',
        hospitalCode: hospitalCode ?? '',
        trustedBranchCode,
      }

  return <StatisticsBoard
    title={version === 'platform' ? '来源医院被互认统计' : '本院被互认统计'}
    subtitle={version === 'platform'
      ? '按组织、医院与院区查询来源医院被互认汇总与明细'
      : '查询本院被互认的汇总与明细；来源院区不选择时按可信医院全院统计'}
    side='source'
    scopeKey={version === 'platform' ? 'platform-source' : `branch-source|${organizationCode ?? ''}|${hospitalCode ?? ''}`}
    scopeSpec={scopeSpec}
    initialFilters={initialFilters}
    querySummaryPage={querySummaryPage}
    queryDetailsPage={queryDetailsPage}
    exportWorkbook={exportWorkbook}
  />
}

/** 「来源医院被互认统计」平台管理员页（宿主路径 `/subApps/medical-recognition/source-recognition-statistics`）。 */
export function SourceRecognitionStatistics() {
  return <SourceStatisticsPage version='platform' />
}

/** 「本院被互认统计」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-source-recognition-statistics`）。 */
export function BranchSourceRecognitionStatistics() {
  return <SourceStatisticsPage version='branch' />
}
