import {
  type BranchRecognitionStatisticsExportRequest,
  type BranchSourceRecognitionDetailsQueryRequest,
  type BranchSourceRecognitionSummaryQueryRequest,
  type MedicalRecognitionClient,
  type RecognitionStatisticsExportRequest,
  type SourceRecognitionDetailReadModel,
  type SourceRecognitionDetailsQueryRequest,
  type SourceRecognitionSummaryQueryRequest,
  type SourceRecognitionSummaryReadModel,
} from '@dy/api-client-medical-recognition'
import { isMedicalItemTypeValue, medicalItemTypeText, type MedicalItemTypeValue } from '../../shared/medicalItemType'
import {
  assignStatisticsText,
  isStatisticsGroupDimensionValue,
  isStatisticsPageInput,
  isSourceExportTypeValue,
  normalizeStatisticsTexts,
  requireStatisticsDate,
  statisticsServerText,
  toScopeView,
  toStatisticsCount,
  toStatisticsDate,
  toStatisticsPage,
  toStatisticsRequestDate,
  type StatisticsGroupDimensionValue,
  type StatisticsOrganizationScopeView,
  type StatisticsPage,
  type StatisticsPageInput,
  type SourceExportTypeValue,
} from './recognitionStatisticsValues'
import { saveExportWorkbook } from './statisticsExportDownload'

/**
 * 生成的契约类型经本模块转出，页面与测试只从适配层取类型，不依赖生成包深层路径或
 * 生成包的传递依赖。
 */
export type {
  BranchRecognitionStatisticsExportRequest,
  BranchSourceRecognitionDetailsQueryRequest,
  BranchSourceRecognitionSummaryQueryRequest,
  MedicalRecognitionClient,
  RecognitionStatisticsExportRequest,
  SourceRecognitionDetailReadModel,
  SourceRecognitionDetailsQueryRequest,
  SourceRecognitionSummaryQueryRequest,
  SourceRecognitionSummaryReadModel,
}

/**
 * 来源医院被互认统计的适配层：来源侧只有「被互认次数」一个指标，没有金额、
 * 明细类型切换与不采纳原因区（阶段 6 前端设计「页面与交互」节）；
 * 平台/本院双版本请求分支与读模型映射收敛在本模块，页面组件不感知请求形态差异。
 *
 * 边界（遵循 Frontend API Client 第 2、5 节，口径与接收侧适配层一致）：
 * 1. 本模块不创建 Client、不读取 token、不解析平台错误响应、不新增错误拦截器；
 * 2. 双版本用可辨识联合表达：本院版条件的类型上不存在本侧来源组织与医院字段，也不存在
 *    接收组织字段（接收组织恒为可信组织），结构上保证这些字段不会被提交。
 */

// ========== 视图模型 ==========

/** 来源侧汇总行视图：每个来源侧汇总维度的一个分组值与被互认次数；未参与分组的维度字段保持 `null`。 */
export interface SourceSummaryRow {
  groupDimension: StatisticsGroupDimensionValue | null
  sourceOrganizationCode: string | null
  sourceOrganizationName: string | null
  sourceHospitalCode: string | null
  sourceHospitalName: string | null
  sourceBranchCode: string | null
  sourceBranchName: string | null
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  standardProjectCode: string | null
  standardProjectName: string | null
  categoryName: string | null
  groupName: string | null
  /** 被互认次数；只计采纳事实。 */
  recognitionCount: number
}

/** 来源侧明细行视图：来源与接收两组归属平铺在行上；患者字段按服务端原值承载（S6-D9）。 */
export interface SourceDetailRow {
  recognitionMatchRecordId: string
  recognitionMatchItemId: string
  source: StatisticsOrganizationScopeView
  receiver: StatisticsOrganizationScopeView
  standardProjectCode: string
  recognitionDeptId: string | null
  recognitionDeptName: string | null
  recognitionDoctorId: string | null
  recognitionDoctorName: string | null
  recognitionTime: Date | null
  patientName: string
  identityDocumentNo: string
}

// ========== 查询条件（页面稳定模型） ==========

/** 平台管理员入口的来源侧汇总条件：来源组与接收组三级范围都随请求提交。 */
export interface PlatformSourceSummaryQuery {
  version: 'platform'
  startTime: string
  endTime: string
  groupDimension: StatisticsGroupDimensionValue
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/**
 * 医院管理员入口的来源侧汇总条件：来源组织与来源医院固定为可信上下文，类型上不存在对应字段；
 * 本侧来源院区可选；接收组织恒为可信组织，只提交接收医院与院区。
 */
export interface BranchSourceSummaryQuery {
  version: 'branch'
  startTime: string
  endTime: string
  groupDimension: StatisticsGroupDimensionValue
  sourceBranchCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type SourceSummaryQuery = PlatformSourceSummaryQuery | BranchSourceSummaryQuery

/** 平台管理员入口的来源侧明细条件：来源侧明细只反映被采纳事实，没有明细类型与医生、原因筛选。 */
export interface PlatformSourceDetailsQuery {
  version: 'platform'
  startTime: string
  endTime: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/** 医院管理员入口的来源侧明细条件：本侧来源院区可选，接收组医院/院区可选。 */
export interface BranchSourceDetailsQuery {
  version: 'branch'
  startTime: string
  endTime: string
  sourceBranchCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type SourceDetailsQuery = PlatformSourceDetailsQuery | BranchSourceDetailsQuery

/** 平台管理员入口的来源侧导出条件：两组组织编码同时提交，供服务端做一致性校验。 */
export interface PlatformSourceExportQuery {
  version: 'platform'
  exportType: SourceExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
  startTime: string
  endTime: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/** 医院管理员入口的来源侧导出条件：不含本侧来源组织与医院，也不含接收组织。 */
export interface BranchSourceExportQuery {
  version: 'branch'
  exportType: SourceExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
  startTime: string
  endTime: string
  sourceBranchCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type SourceExportQuery = PlatformSourceExportQuery | BranchSourceExportQuery

/** 已通过构造校验的查询：携带值域内的分页输入。 */
export type BuiltSourceSummaryQuery = SourceSummaryQuery & StatisticsPageInput
export type BuiltSourceDetailsQuery = SourceDetailsQuery & StatisticsPageInput

// ========== 请求构造与请求体映射 ==========

/** 可选文本条件键集合：构造请求前统一去首尾空白。 */
const SOURCE_SCOPE_TEXT_KEYS = [
  'sourceOrganizationCode',
  'sourceHospitalCode',
  'sourceBranchCode',
  'receiverOrganizationCode',
  'receiverHospitalCode',
  'receiverBranchCode',
  'categoryName',
  'groupName',
  'standardProjectCode',
] as const

/** 平台版共享筛选写入：来源组与接收组三级范围、项目筛选。 */
function applyPlatformSourceFilters(
  body: SourceRecognitionSummaryQueryRequest | SourceRecognitionDetailsQueryRequest,
  query: PlatformSourceSummaryQuery | PlatformSourceDetailsQuery,
): void {
  assignStatisticsText(body, 'sourceOrganizationCode', query.sourceOrganizationCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'receiverOrganizationCode', query.receiverOrganizationCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
}

/** 本院版共享筛选写入：本侧来源院区可选，接收组只提交医院与院区。 */
function applyBranchSourceFilters(
  body: BranchSourceRecognitionSummaryQueryRequest | BranchSourceRecognitionDetailsQueryRequest,
  query: BranchSourceSummaryQuery | BranchSourceDetailsQuery,
): void {
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
}

/** 构造来源侧汇总查询；校验口径与接收侧一致（日期、维度、项目类型与分页）。 */
export function buildSourceSummaryQuery(query: SourceSummaryQuery, page: StatisticsPageInput): BuiltSourceSummaryQuery | null {
  if (!isStatisticsPageInput(page)) return null
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (!isStatisticsGroupDimensionValue(query.groupDimension)) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts({ ...query, ...page }, [...SOURCE_SCOPE_TEXT_KEYS])
}

/** 构造来源侧明细查询；来源侧没有明细类型、互认医生与不采纳原因筛选。 */
export function buildSourceDetailsQuery(query: SourceDetailsQuery, page: StatisticsPageInput): BuiltSourceDetailsQuery | null {
  if (!isStatisticsPageInput(page)) return null
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts({ ...query, ...page }, [...SOURCE_SCOPE_TEXT_KEYS])
}

/** 构造来源侧导出条件：导出类型只接受来源侧取值（6-7），无分页对象。 */
export function buildSourceExportQuery(query: SourceExportQuery): SourceExportQuery | null {
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (!isSourceExportTypeValue(query.exportType)) return null
  if (!isStatisticsGroupDimensionValue(query.groupDimension)) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts(query, [...SOURCE_SCOPE_TEXT_KEYS])
}

/** 平台版来源侧汇总请求体：两组三级范围 + 汇总维度 + 内嵌分页。 */
function toPlatformSourceSummaryBody(query: PlatformSourceSummaryQuery & StatisticsPageInput): SourceRecognitionSummaryQueryRequest {
  const body: SourceRecognitionSummaryQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    groupDimension: query.groupDimension,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyPlatformSourceFilters(body, query)
  return body
}

/** 本院版来源侧汇总请求体：类型上不存在本侧来源组织与医院、接收组织字段。 */
function toBranchSourceSummaryBody(
  query: BranchSourceSummaryQuery & StatisticsPageInput,
): BranchSourceRecognitionSummaryQueryRequest {
  const body: BranchSourceRecognitionSummaryQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    groupDimension: query.groupDimension,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyBranchSourceFilters(body, query)
  return body
}

/** 平台版来源侧明细请求体：结构与汇总一致，无明细类型与医生、原因筛选。 */
function toPlatformSourceDetailsBody(query: PlatformSourceDetailsQuery & StatisticsPageInput): SourceRecognitionDetailsQueryRequest {
  const body: SourceRecognitionDetailsQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyPlatformSourceFilters(body, query)
  return body
}

/** 本院版来源侧明细请求体：本侧来源院区可选，接收组医院/院区可选。 */
function toBranchSourceDetailsBody(query: BranchSourceDetailsQuery & StatisticsPageInput): BranchSourceRecognitionDetailsQueryRequest {
  const body: BranchSourceRecognitionDetailsQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyBranchSourceFilters(body, query)
  return body
}

/** 平台版来源侧导出请求体：两组组织编码同时提交，供服务端做一致性校验；无分页对象。 */
function toPlatformSourceExportBody(query: PlatformSourceExportQuery): RecognitionStatisticsExportRequest {
  const body: RecognitionStatisticsExportRequest = {
    exportType: query.exportType,
    groupDimension: query.groupDimension,
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
  }
  assignStatisticsText(body, 'sourceOrganizationCode', query.sourceOrganizationCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'receiverOrganizationCode', query.receiverOrganizationCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
  return body
}

/** 本院版来源侧导出请求体：不含本侧来源组织与医院，也不含接收组织；无分页对象。 */
function toBranchSourceExportBody(query: BranchSourceExportQuery): BranchRecognitionStatisticsExportRequest {
  const body: BranchRecognitionStatisticsExportRequest = {
    exportType: query.exportType,
    groupDimension: query.groupDimension,
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
  }
  assignStatisticsText(body, 'branchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
  return body
}

// ========== 查询出口 ==========

/** 查询来源医院被互认汇总（平台或本院入口由查询条件的 version 决定）。 */
export async function querySourceSummaryPage(
  client: MedicalRecognitionClient,
  query: BuiltSourceSummaryQuery,
): Promise<StatisticsPage<SourceSummaryRow>> {
  const response =
    query.version === 'platform'
      ? await client.api.medicalRecognitionReportQuery.querySourceRecognitionSummary.post(toPlatformSourceSummaryBody(query))
      : await client.api.medicalRecognitionReportQuery.queryBranchSourceRecognitionSummary.post(toBranchSourceSummaryBody(query))
  return toStatisticsPage(response?.items, response?.page, toSourceSummaryRow)
}

/** 查询来源医院被互认明细（平台或本院入口由查询条件的 version 决定）。 */
export async function querySourceDetailsPage(
  client: MedicalRecognitionClient,
  query: BuiltSourceDetailsQuery,
): Promise<StatisticsPage<SourceDetailRow>> {
  const response =
    query.version === 'platform'
      ? await client.api.medicalRecognitionReportQuery.querySourceRecognitionDetails.post(toPlatformSourceDetailsBody(query))
      : await client.api.medicalRecognitionReportQuery.queryBranchSourceRecognitionDetails.post(toBranchSourceDetailsBody(query))
  return toStatisticsPage(response?.items, response?.page, toSourceDetailRow)
}

/** 导出来源侧统计：已鉴权取字节、按给定下载名保存、失败交宿主统一提示。 */
export async function exportSourceStatistics(
  client: MedicalRecognitionClient,
  query: SourceExportQuery,
  downloadFileName: string,
): Promise<void> {
  if (query.version === 'platform') {
    const content = await client.api.v1.statisticsExport.platform.post(toPlatformSourceExportBody(query))
    saveExportWorkbook(content, downloadFileName)
    return
  }
  const content = await client.api.v1.statisticsExport.branch.post(toBranchSourceExportBody(query))
  saveExportWorkbook(content, downloadFileName)
}

// ========== 读模型映射 ==========

/** 来源侧汇总读模型 → 汇总行视图模型：维度字段保持 `null` 兜底，枚举文本服务端优先。 */
function toSourceSummaryRow(value: SourceRecognitionSummaryReadModel): SourceSummaryRow {
  const itemType = isMedicalItemTypeValue(value.itemType) ? value.itemType : null
  return {
    groupDimension: isStatisticsGroupDimensionValue(value.groupDimension) ? value.groupDimension : null,
    sourceOrganizationCode: value.sourceOrganizationCode ?? null,
    sourceOrganizationName: value.sourceOrganizationName ?? null,
    sourceHospitalCode: value.sourceHospitalCode ?? null,
    sourceHospitalName: value.sourceHospitalName ?? null,
    sourceBranchCode: value.sourceBranchCode ?? null,
    sourceBranchName: value.sourceBranchName ?? null,
    itemType,
    itemTypeText: statisticsServerText(value.itemTypeText) ?? medicalItemTypeText(itemType),
    standardProjectCode: value.standardProjectCode ?? null,
    standardProjectName: value.standardProjectName ?? null,
    categoryName: value.categoryName ?? null,
    groupName: value.groupName ?? null,
    recognitionCount: toStatisticsCount(value.recognitionCount),
  }
}

/** 来源侧明细读模型 → 明细行视图模型：双方归属从平铺字段收拢为两个组织视图，患者字段原样承载（S6-D9）。 */
function toSourceDetailRow(value: SourceRecognitionDetailReadModel): SourceDetailRow {
  return {
    recognitionMatchRecordId: value.recognitionMatchRecordId ?? '',
    recognitionMatchItemId: value.recognitionMatchItemId ?? '',
    source: toScopeView({
      organizationCode: value.sourceOrganizationCode,
      organizationName: value.sourceOrganizationName,
      hospitalCode: value.sourceHospitalCode,
      hospitalName: value.sourceHospitalName,
      branchCode: value.sourceBranchCode,
      branchName: value.sourceBranchName,
    }),
    receiver: toScopeView({
      organizationCode: value.receiverOrganizationCode,
      organizationName: value.receiverOrganizationName,
      hospitalCode: value.receiverHospitalCode,
      hospitalName: value.receiverHospitalName,
      branchCode: value.receiverBranchCode,
      branchName: value.receiverBranchName,
    }),
    standardProjectCode: value.standardProjectCode ?? '',
    recognitionDeptId: value.recognitionDeptId ?? null,
    recognitionDeptName: value.recognitionDeptName ?? null,
    recognitionDoctorId: value.recognitionDoctorId ?? null,
    recognitionDoctorName: value.recognitionDoctorName ?? null,
    recognitionTime: toStatisticsDate(value.recognitionTime),
    patientName: value.patientName ?? '',
    identityDocumentNo: value.identityDocumentNo ?? '',
  }
}

/** 来源侧汇总读模型集合 → 汇总行视图模型集合。 */
export function toSourceSummaryRows(values: readonly SourceRecognitionSummaryReadModel[]): SourceSummaryRow[] {
  return values.map(toSourceSummaryRow)
}

/** 来源侧明细读模型集合 → 明细行视图模型集合。 */
export function toSourceDetailRows(values: readonly SourceRecognitionDetailReadModel[]): SourceDetailRow[] {
  return values.map(toSourceDetailRow)
}
