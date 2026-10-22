/**
 * 「提供方医院被互认统计」平台管理员页与「本院被互认统计」医院管理员页（S6-D11）。
 *
 * 宿主路径：
 * - 平台页 `/subApps/medical-recognition/source-recognition-statistics`
 * - 本院页 `/subApps/medical-recognition/branch-source-recognition-statistics`
 *
 * 与使用方页面同构（共享 StatisticsBoard），差异在数据口径与范围形态：
 * 只统计被互认次数，无金额、无明细类型切换与不采纳原因区（C22）；
 * 平台页来源组与接收组的组织、医院、院区三级都由页面选择并随请求提交；
 * 本院页提供方组织与提供方医院取可信上下文并固定展示，请求不提交这两项（由服务端注入），
 * 提供方院区恒为登录院区（不提供院区选择；缺失属权限配置错误，页面零业务请求、以空态呈现，
 * 服务端全院统计契约仅为接口面保留（S6-D1），前端已不按其触发），
 * 接收组只提交医院与院区（使用方组织恒为可信组织）。
 * 提供方明细不提供匹配记录入口（S6-D10），页面不注入匹配记录查询出口。
 */
import { Typography } from 'antd'
import { useCallback, useMemo, useState } from 'react'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
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
  STATISTICS_EXPORT_TYPE,
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

/** 页面级筛选 → 平台/本院版提供方汇总查询条件的装配；统一关键字承载互认科室、互认项目条件（S6-D19）。 */
function toSummaryQuery(version: StatisticsPageVersion, filters: StatisticsFilters, dimension: StatisticsGroupDimensionValue): SourceSummaryQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    groupDimension: dimension,
    keyword: filters.keyword,
    itemType: filters.itemType,
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

/** 页面级筛选 → 平台/本院版提供方明细查询条件的装配；提供方没有明细类型与医生、原因筛选。 */
function toDetailsQuery(version: StatisticsPageVersion, filters: StatisticsFilters): SourceDetailsQuery {
  const shared = {
    startTime: filters.startTime,
    endTime: filters.endTime,
    keyword: filters.keyword,
    itemType: filters.itemType,
    // 明细专用精确分组字段由主体按选中行填充；汇总条件装配不读取它。
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

/** 页面级筛选 → 提供方导出查询条件的装配（沿用当前查询条件，不带分页）。 */
function toExportQuery(version: StatisticsPageVersion, filters: StatisticsFilters, request: {
  exportType: SourceExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
}): SourceExportQuery {
  const shared = {
    exportType: request.exportType,
    groupDimension: request.groupDimension,
    startTime: filters.startTime,
    endTime: filters.endTime,
    keyword: filters.keyword,
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

  const exportWorkbook = useCallback<NonNullable<StatisticsBoardProps['exportWorkbook']>>((filters, detailFilters, request) => {
    const { exportType, groupDimension } = request
    if (!isSourceExportTypeValue(exportType)) return null
    if (!isStatisticsGroupDimensionValue(groupDimension)) return null
    const exportFilters = exportType === STATISTICS_EXPORT_TYPE.sourceDetail ? detailFilters : filters
    const built = buildSourceExportQuery(toExportQuery(version, exportFilters, { exportType, groupDimension }))
    return built === null ? null : exportSourceStatistics(client, built, request.downloadFileName)
  }, [client, version])

  // 本院页：本侧院区恒为登录院区（不提供院区选择），作为初始条件交给主体；缺失属权限配置错误，查询出口经
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
              <Typography.Title level={3}>本院被互认统计</Typography.Title>
              <Typography.Text type='secondary'>查询本院被互认的汇总与明细</Typography.Text>
            </div>
          </div>
        }
      />
    </div>
  }

  const scopeSpec: StatisticsScopeSpec = version === 'platform'
    ? { kind: 'platform' }
    : { kind: 'branch', organizationCode: organizationCode ?? '', hospitalCode: hospitalCode ?? '' }

  return <StatisticsBoard
    title={version === 'platform' ? '提供方医院被互认统计' : '本院被互认统计'}
    subtitle={version === 'platform'
      ? '按组织、医院与院区查询提供方医院被互认汇总与明细'
      : '查询本院被互认的汇总与明细'}
    side='source'
    // 范围键含登录院区：院区随可信范围变化即视为范围切换，主体清空条件与数据后按新院区复位。
    scopeKey={version === 'platform'
      ? 'platform-source'
      : `branch-source|${organizationCode ?? ''}|${hospitalCode ?? ''}|${trustedBranchCode ?? ''}`}
    scopeSpec={scopeSpec}
    initialFilters={version === 'platform' ? initialFilters : branchInitialFilters}
    missingScopeText={missingBranchScope ? '当前登录身份未携带院区，无法查询本院统计' : undefined}
    querySummaryPage={querySummaryPage}
    queryDetailsPage={queryDetailsPage}
    exportWorkbook={exportWorkbook}
  />
}

/** 「提供方医院被互认统计」平台管理员页（宿主路径 `/subApps/medical-recognition/source-recognition-statistics`）。 */
export function SourceRecognitionStatistics() {
  return <SourceStatisticsPage version='platform' />
}

/** 「本院被互认统计」医院管理员页（宿主路径 `/subApps/medical-recognition/branch-source-recognition-statistics`）。 */
export function BranchSourceRecognitionStatistics() {
  return <SourceStatisticsPage version='branch' />
}
