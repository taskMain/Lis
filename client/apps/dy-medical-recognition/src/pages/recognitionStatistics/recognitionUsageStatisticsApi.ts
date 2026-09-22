import {
  type BranchRecognitionStatisticsExportRequest,
  type BranchRecognitionUsageDetailsQueryRequest,
  type BranchRecognitionUsageSummaryQueryRequest,
  type MedicalRecognitionClient,
  type RecognitionMatchRecordItemReadModel,
  type RecognitionMatchRecordReadModel,
  type RecognitionStatisticsExportRequest,
  type RecognitionStatisticsItemReadModel,
  type RecognitionUsageDetailReadModel,
  type RecognitionUsageDetailsQueryRequest,
  type RecognitionUsageSummaryQueryRequest,
  type RecognitionUsageSummaryReadModel,
} from '@dy/api-client-medical-recognition'
import { isMedicalItemTypeValue, medicalItemTypeText, type MedicalItemTypeValue } from '../../shared/medicalItemType'
import {
  assignStatisticsText,
  isStatisticsGroupDimensionValue,
  isStatisticsPageInput,
  isUsageDetailTypeValue,
  isUsageExportTypeValue,
  normalizeStatisticsTexts,
  requireStatisticsDate,
  statisticsServerText,
  toItemView,
  toNodeNumber,
  toScopeView,
  toStatisticsCount,
  toStatisticsDate,
  toStatisticsPage,
  toStatisticsRequestDate,
  type StatisticsGroupDimensionValue,
  type StatisticsItemView,
  type StatisticsOrganizationScopeView,
  type StatisticsPage,
  type StatisticsPageInput,
  type UsageDetailTypeValue,
  type UsageExportTypeValue,
} from './recognitionStatisticsValues'
import { saveExportWorkbook } from './statisticsExportDownload'

/**
 * 生成的契约类型与两个适配层共享的视图结构经本模块转出，
 * 页面与测试只从适配层取类型，不依赖生成包深层路径或生成包的传递依赖。
 */
export type {
  BranchRecognitionStatisticsExportRequest,
  BranchRecognitionUsageDetailsQueryRequest,
  BranchRecognitionUsageSummaryQueryRequest,
  MedicalRecognitionClient,
  RecognitionMatchRecordItemReadModel,
  RecognitionMatchRecordReadModel,
  RecognitionStatisticsExportRequest,
  RecognitionStatisticsItemReadModel,
  RecognitionUsageDetailReadModel,
  RecognitionUsageSummaryQueryRequest,
  RecognitionUsageSummaryReadModel,
}
export type { StatisticsItemView, StatisticsOrganizationScopeView } from './recognitionStatisticsValues'

/**
 * 接收侧互认使用统计的适配层：平台/本院双版本请求分支、分页收敛与读模型映射都收敛在本模块，
 * 页面组件不感知请求形态差异（阶段 6 前端设计「API 与组件」节）。
 *
 * 边界（遵循 Frontend API Client 第 2、5 节）：
 * 1. 本模块不创建 Client、不读取 token、不解析平台错误响应、不新增错误拦截器；
 * 2. 页面只依赖本模块导出的稳定类型与函数，不接触 Kiota 生成模型；
 * 3. 双版本用可辨识联合表达：本院版查询条件的类型上不存在本侧组织与医院字段，
 *    结构上保证「本院版不提交本侧组织与医院」，不依赖运行时校验兜底。
 * 4. 生成契约的固有形态由本层吸收：
 *    a) 后端必填字段在 Kiota 下仍是可选（`string | null`），请求构造逐字段显式赋值；
 *    b) 分页请求内嵌 `page` 对象，响应分页经安全整数与值域校验收敛（非法按契约错误抛出）；
 *    c) `decimal` 字段（同期互认率、预计节省金额、原因占比）生成为 `UntypedNode`，
 *       按 `getValue()` 取值且只接受有限数值；
 *    d) 日期只按 `YYYY-MM-DD` 构造 `DateOnly` 原样提交，不拼接时分秒、不写时区偏移。
 */

// ========== 视图模型 ==========

/** 不采纳原因行视图：占比在行内不采纳次数为零时服务端不计算，视图保持 `null`。 */
export interface NonAdoptionReasonView {
  reasonCode: string
  reasonName: string
  count: number
  ratio: number | null
}

/** 接收侧汇总行视图：每个汇总维度的一个分组值；未参与分组的维度字段保持 `null`，页面据此不渲染该列。 */
export interface UsageSummaryRow {
  groupDimension: StatisticsGroupDimensionValue | null
  receiverOrganizationCode: string | null
  receiverOrganizationName: string | null
  receiverHospitalCode: string | null
  receiverHospitalName: string | null
  receiverBranchCode: string | null
  receiverBranchName: string | null
  recognitionDeptId: string | null
  recognitionDeptName: string | null
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  standardProjectCode: string | null
  standardProjectName: string | null
  categoryName: string | null
  groupName: string | null
  reminderCount: number
  adoptionCount: number
  nonAdoptionCount: number
  referenceCount: number
  /** 同期互认率；服务端声明未计算（提醒为零）或取值非法时为 `null`，页面展示「—」。 */
  samePeriodRecognitionRate: number | null
  estimatedSavingAmount: number | null
  nonAdoptionReasons: NonAdoptionReasonView[]
}

/** 处理结果事实视图；未反馈项 `isProcessed` 为假，页面不把未反馈推断为不采纳。 */
export interface UsageProcessingResultView {
  isProcessed: boolean
  recognitionTime: Date | null
  decision: number | null
  decisionText: string
  nonAdoptionReasonCode: string | null
  nonAdoptionReasonName: string | null
  nonAdoptionSupplementDescription: string | null
  estimatedSavingAmount: number | null
}

/** 引用事实视图；未引用项为 `null`。 */
export interface UsageReferenceView {
  isReferenced: boolean
  referenceTime: Date | null
  referenceDeptId: string | null
  referenceDeptName: string | null
  referenceDoctorId: string | null
  referenceDoctorName: string | null
}

/** 接收侧明细行视图；患者姓名与证件号码按服务端原值承载，不做二次处理（S6-D9）。 */
export interface UsageDetailRow {
  recognitionMatchRecordId: string
  recognitionMatchItemId: string
  matchCreatedTime: Date | null
  businessTime: Date | null
  visitType: number | null
  visitTypeText: string
  visitSerialNo: string
  source: StatisticsOrganizationScopeView
  receiver: StatisticsOrganizationScopeView
  item: StatisticsItemView
  isUnprocessed: boolean
  patientName: string
  identityDocumentNo: string
  recognitionDeptId: string | null
  recognitionDeptName: string | null
  recognitionDoctorId: string | null
  recognitionDoctorName: string | null
  processingResult: UsageProcessingResultView | null
  reference: UsageReferenceView | null
}

/** 匹配项视图：组级匹配记录内的单个互认匹配项。 */
export interface MatchRecordItemView {
  recognitionMatchItemId: string
  item: StatisticsItemView
  source: StatisticsOrganizationScopeView
  reportId: string
  reportVersionId: string
  isProcessed: boolean
  decision: number | null
  decisionText: string
  nonAdoptionReasonCode: string | null
  nonAdoptionReasonName: string | null
}

/** 匹配记录集合视图：组级信息、患者与就诊、处理状态、决定主体与全部匹配项。 */
export interface MatchRecordView {
  recognitionMatchRecordId: string
  matchCreatedTime: Date | null
  receiver: StatisticsOrganizationScopeView
  patientName: string
  identityDocumentNo: string
  visitType: number | null
  visitTypeText: string
  visitSerialNo: string
  isProcessed: boolean
  recognitionTime: Date | null
  recognitionDeptId: string | null
  recognitionDeptName: string | null
  recognitionDoctorId: string | null
  recognitionDoctorName: string | null
  matchItems: MatchRecordItemView[]
}

// ========== 查询条件（页面稳定模型） ==========

/** 平台管理员入口的接收侧汇总条件：接收组与来源组三级范围都随请求提交，任一为空不附加该层过滤。 */
export interface PlatformUsageSummaryQuery {
  version: 'platform'
  startTime: string
  endTime: string
  groupDimension: StatisticsGroupDimensionValue
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/**
 * 医院管理员入口的接收侧汇总条件：本侧组织与医院由服务端从可信上下文注入，
 * 类型上不存在对应字段；来源组织恒为可信组织，只提交来源组医院与院区。
 */
export interface BranchUsageSummaryQuery {
  version: 'branch'
  startTime: string
  endTime: string
  groupDimension: StatisticsGroupDimensionValue
  branchCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type UsageSummaryQuery = PlatformUsageSummaryQuery | BranchUsageSummaryQuery

/** 平台管理员入口的接收侧明细条件：明细类型必填；互认医生与不采纳原因代码只进明细查询。 */
export interface PlatformUsageDetailsQuery {
  version: 'platform'
  startTime: string
  endTime: string
  detailType: UsageDetailTypeValue
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  recognitionDoctorId?: string
  nonAdoptionReasonCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/** 医院管理员入口的接收侧明细条件：本侧院区可选，为空按可信医院全院查询。 */
export interface BranchUsageDetailsQuery {
  version: 'branch'
  startTime: string
  endTime: string
  detailType: UsageDetailTypeValue
  branchCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  recognitionDoctorId?: string
  nonAdoptionReasonCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type UsageDetailsQuery = PlatformUsageDetailsQuery | BranchUsageDetailsQuery

/** 平台管理员入口的接收侧导出条件：两组组织编码同时提交，供服务端做一致性校验。 */
export interface PlatformUsageExportQuery {
  version: 'platform'
  exportType: UsageExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
  startTime: string
  endTime: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  recognitionDoctorId?: string
  nonAdoptionReasonCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

/** 医院管理员入口的接收侧导出条件：不含本侧组织与医院；来源组与接收组的医院/院区为可选筛选。 */
export interface BranchUsageExportQuery {
  version: 'branch'
  exportType: UsageExportTypeValue
  groupDimension: StatisticsGroupDimensionValue
  startTime: string
  endTime: string
  branchCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  recognitionDeptId?: string
  recognitionDoctorId?: string
  nonAdoptionReasonCode?: string
  itemType?: MedicalItemTypeValue
  categoryName?: string
  groupName?: string
  standardProjectCode?: string
}

export type UsageExportQuery = PlatformUsageExportQuery | BranchUsageExportQuery

/** 已通过构造校验的查询：携带值域内的分页输入。 */
export type BuiltUsageSummaryQuery = UsageSummaryQuery & StatisticsPageInput
export type BuiltUsageDetailsQuery = UsageDetailsQuery & StatisticsPageInput

// ========== 请求构造 ==========

/** 可选文本条件键集合：构造请求前统一去首尾空白。 */
const USAGE_SCOPE_TEXT_KEYS = [
  'receiverOrganizationCode',
  'receiverHospitalCode',
  'receiverBranchCode',
  'sourceOrganizationCode',
  'sourceHospitalCode',
  'sourceBranchCode',
  'branchCode',
  'recognitionDeptId',
  'recognitionDoctorId',
  'nonAdoptionReasonCode',
  'categoryName',
  'groupName',
  'standardProjectCode',
] as const

/** 构造接收侧汇总查询：日期缺失或形态非法、汇总维度越界、项目类型越界或分页越界时返回 `null`，调用方据此零发请求。 */
export function buildUsageSummaryQuery(query: UsageSummaryQuery, page: StatisticsPageInput): BuiltUsageSummaryQuery | null {
  if (!isStatisticsPageInput(page)) return null
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (!isStatisticsGroupDimensionValue(query.groupDimension)) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts({ ...query, ...page }, [...USAGE_SCOPE_TEXT_KEYS])
}

/** 构造接收侧明细查询；明细类型必填且取值必须在取值域内，其余口径同汇总构造。 */
export function buildUsageDetailsQuery(query: UsageDetailsQuery, page: StatisticsPageInput): BuiltUsageDetailsQuery | null {
  if (!isStatisticsPageInput(page)) return null
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (!isUsageDetailTypeValue(query.detailType)) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts({ ...query, ...page }, [...USAGE_SCOPE_TEXT_KEYS])
}

/** 构造接收侧导出条件：导出无分页对象；日期、导出类型、汇总维度与项目类型全部校验，任一非法返回 `null`。 */
export function buildUsageExportQuery(query: UsageExportQuery): UsageExportQuery | null {
  if (toStatisticsRequestDate(query.startTime) === null || toStatisticsRequestDate(query.endTime) === null) return null
  if (!isUsageExportTypeValue(query.exportType)) return null
  if (!isStatisticsGroupDimensionValue(query.groupDimension)) return null
  if (query.itemType !== undefined && !isMedicalItemTypeValue(query.itemType)) return null
  return normalizeStatisticsTexts(query, [...USAGE_SCOPE_TEXT_KEYS])
}

// ========== 请求体映射（生成契约字段名与页面条件名的差异都在本节吸收） ==========

/** 平台版共享的筛选写入：接收组与来源组三级范围、互认科室与项目筛选。 */
function applyPlatformUsageFilters(
  body: RecognitionUsageSummaryQueryRequest | RecognitionUsageDetailsQueryRequest,
  query: PlatformUsageSummaryQuery | PlatformUsageDetailsQuery,
): void {
  assignStatisticsText(body, 'organizationCode', query.receiverOrganizationCode)
  assignStatisticsText(body, 'hospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'branchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'sourceOrganizationCode', query.sourceOrganizationCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'recognitionDeptId', query.recognitionDeptId)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
}

/** 本院版共享的筛选写入：本侧院区可选，来源组只提交医院与院区（组织由服务端注入）。 */
function applyBranchUsageFilters(
  body: BranchRecognitionUsageSummaryQueryRequest | BranchRecognitionUsageDetailsQueryRequest,
  query: BranchUsageSummaryQuery | BranchUsageDetailsQuery,
): void {
  assignStatisticsText(body, 'branchCode', query.branchCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'recognitionDeptId', query.recognitionDeptId)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
}

/** 平台版汇总请求体：两组三级范围 + 项目筛选 + 汇总维度 + 内嵌分页。 */
function toPlatformUsageSummaryBody(query: PlatformUsageSummaryQuery & StatisticsPageInput): RecognitionUsageSummaryQueryRequest {
  const body: RecognitionUsageSummaryQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    groupDimension: query.groupDimension,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyPlatformUsageFilters(body, query)
  return body
}

/** 本院版汇总请求体：类型上不存在本侧组织与医院字段，请求体不会出现对应键。 */
function toBranchUsageSummaryBody(
  query: BranchUsageSummaryQuery & StatisticsPageInput,
): BranchRecognitionUsageSummaryQueryRequest {
  const body: BranchRecognitionUsageSummaryQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    groupDimension: query.groupDimension,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyBranchUsageFilters(body, query)
  return body
}

/** 平台版明细请求体：在平台筛选之上提交明细类型、互认医生与不采纳原因代码。 */
function toPlatformUsageDetailsBody(query: PlatformUsageDetailsQuery & StatisticsPageInput): RecognitionUsageDetailsQueryRequest {
  const body: RecognitionUsageDetailsQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    detailType: query.detailType,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyPlatformUsageFilters(body, query)
  assignStatisticsText(body, 'recognitionDoctorId', query.recognitionDoctorId)
  assignStatisticsText(body, 'nonAdoptionReasonCode', query.nonAdoptionReasonCode)
  return body
}

/** 本院版明细请求体：本侧院区可选（为空按可信医院全院查询），不提交本侧组织与医院。 */
function toBranchUsageDetailsBody(query: BranchUsageDetailsQuery & StatisticsPageInput): BranchRecognitionUsageDetailsQueryRequest {
  const body: BranchRecognitionUsageDetailsQueryRequest = {
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
    detailType: query.detailType,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyBranchUsageFilters(body, query)
  assignStatisticsText(body, 'recognitionDoctorId', query.recognitionDoctorId)
  assignStatisticsText(body, 'nonAdoptionReasonCode', query.nonAdoptionReasonCode)
  return body
}

/** 平台版导出请求体：接收组与来源组两组组织编码同时提交，供服务端做一致性校验；无分页对象。 */
function toPlatformUsageExportBody(query: PlatformUsageExportQuery): RecognitionStatisticsExportRequest {
  const body: RecognitionStatisticsExportRequest = {
    exportType: query.exportType,
    groupDimension: query.groupDimension,
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
  }
  assignStatisticsText(body, 'receiverOrganizationCode', query.receiverOrganizationCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'sourceOrganizationCode', query.sourceOrganizationCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'recognitionDeptId', query.recognitionDeptId)
  assignStatisticsText(body, 'recognitionDoctorId', query.recognitionDoctorId)
  assignStatisticsText(body, 'nonAdoptionReasonCode', query.nonAdoptionReasonCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
  return body
}

/** 本院版导出请求体：不含本侧组织与医院；来源组与接收组的医院/院区为可选筛选；无分页对象。 */
function toBranchUsageExportBody(query: BranchUsageExportQuery): BranchRecognitionStatisticsExportRequest {
  const body: BranchRecognitionStatisticsExportRequest = {
    exportType: query.exportType,
    groupDimension: query.groupDimension,
    startTime: requireStatisticsDate(query.startTime, '开始日期'),
    endTime: requireStatisticsDate(query.endTime, '结束日期'),
  }
  assignStatisticsText(body, 'branchCode', query.branchCode)
  assignStatisticsText(body, 'sourceHospitalCode', query.sourceHospitalCode)
  assignStatisticsText(body, 'sourceBranchCode', query.sourceBranchCode)
  assignStatisticsText(body, 'receiverHospitalCode', query.receiverHospitalCode)
  assignStatisticsText(body, 'receiverBranchCode', query.receiverBranchCode)
  assignStatisticsText(body, 'recognitionDeptId', query.recognitionDeptId)
  assignStatisticsText(body, 'recognitionDoctorId', query.recognitionDoctorId)
  assignStatisticsText(body, 'nonAdoptionReasonCode', query.nonAdoptionReasonCode)
  assignStatisticsText(body, 'categoryName', query.categoryName)
  assignStatisticsText(body, 'groupName', query.groupName)
  assignStatisticsText(body, 'standardProjectCode', query.standardProjectCode)
  if (query.itemType !== undefined) body.itemType = query.itemType
  return body
}

// ========== 查询出口 ==========

/** 查询接收侧互认使用汇总（平台或本院入口由查询条件的 version 决定）。 */
export async function queryUsageSummaryPage(
  client: MedicalRecognitionClient,
  query: BuiltUsageSummaryQuery,
): Promise<StatisticsPage<UsageSummaryRow>> {
  const response =
    query.version === 'platform'
      ? await client.api.medicalRecognitionReportQuery.queryRecognitionUsageSummary.post(toPlatformUsageSummaryBody(query))
      : await client.api.medicalRecognitionReportQuery.queryBranchRecognitionUsageSummary.post(toBranchUsageSummaryBody(query))
  return toStatisticsPage(response?.items, response?.page, toUsageSummaryRow)
}

/** 查询接收侧互认使用明细（平台或本院入口由查询条件的 version 决定）。 */
export async function queryUsageDetailsPage(
  client: MedicalRecognitionClient,
  query: BuiltUsageDetailsQuery,
): Promise<StatisticsPage<UsageDetailRow>> {
  const response =
    query.version === 'platform'
      ? await client.api.medicalRecognitionReportQuery.queryRecognitionUsageDetails.post(toPlatformUsageDetailsBody(query))
      : await client.api.medicalRecognitionReportQuery.queryBranchRecognitionUsageDetails.post(toBranchUsageDetailsBody(query))
  return toStatisticsPage(response?.items, response?.page, toUsageDetailRow)
}

/** 查看互认匹配记录集合视图（接收侧明细行的弹窗数据源）。 */
export async function queryRecognitionMatchRecord(
  client: MedicalRecognitionClient,
  recognitionMatchRecordId: string,
): Promise<MatchRecordView> {
  const recordId = recognitionMatchRecordId.trim()
  if (recordId.length === 0) throw new Error('互认匹配记录标识不能为空白。')
  const value = await client.api.medicalRecognitionReportQuery.queryRecognitionMatchRecord.post({ recognitionMatchRecordId: recordId })
  if (value === undefined || value === null) throw new Error('互认匹配记录未返回内容。')
  return toMatchRecordView(value)
}

/** 导出接收侧统计（汇总或明细由导出类型决定）：已鉴权取字节、按给定下载名保存、失败交宿主统一提示。 */
export async function exportUsageStatistics(
  client: MedicalRecognitionClient,
  query: UsageExportQuery,
  downloadFileName: string,
): Promise<void> {
  if (query.version === 'platform') {
    const content = await client.api.v1.statisticsExport.platform.post(toPlatformUsageExportBody(query))
    saveExportWorkbook(content, downloadFileName)
    return
  }
  const content = await client.api.v1.statisticsExport.branch.post(toBranchUsageExportBody(query))
  saveExportWorkbook(content, downloadFileName)
}

// ========== 读模型映射 ==========

/** 不采纳原因集合映射；行内不采纳为零时服务端不计算占比，视图保持 `null`。 */
function toNonAdoptionReasonViews(
  values:
    | readonly { reasonCode?: string | null; reasonName?: string | null; count?: number | null; ratio?: unknown }[]
    | null
    | undefined,
): NonAdoptionReasonView[] {
  return (values ?? []).map((value) => ({
    reasonCode: value.reasonCode ?? '',
    reasonName: value.reasonName ?? '',
    count: toStatisticsCount(value.count),
    ratio: toNodeNumber(value.ratio),
  }))
}

/** 汇总读模型 → 汇总行视图模型：维度字段保持 `null` 兜底，枚举文本服务端优先。 */
function toUsageSummaryRow(value: RecognitionUsageSummaryReadModel): UsageSummaryRow {
  const itemType = isMedicalItemTypeValue(value.itemType) ? value.itemType : null
  return {
    groupDimension: isStatisticsGroupDimensionValue(value.groupDimension) ? value.groupDimension : null,
    receiverOrganizationCode: value.receiverOrganizationCode ?? null,
    receiverOrganizationName: value.receiverOrganizationName ?? null,
    receiverHospitalCode: value.receiverHospitalCode ?? null,
    receiverHospitalName: value.receiverHospitalName ?? null,
    receiverBranchCode: value.receiverBranchCode ?? null,
    receiverBranchName: value.receiverBranchName ?? null,
    recognitionDeptId: value.recognitionDeptId ?? null,
    recognitionDeptName: value.recognitionDeptName ?? null,
    itemType,
    itemTypeText: statisticsServerText(value.itemTypeText) ?? medicalItemTypeText(itemType),
    standardProjectCode: value.standardProjectCode ?? null,
    standardProjectName: value.standardProjectName ?? null,
    categoryName: value.categoryName ?? null,
    groupName: value.groupName ?? null,
    reminderCount: toStatisticsCount(value.reminderCount),
    adoptionCount: toStatisticsCount(value.adoptionCount),
    nonAdoptionCount: toStatisticsCount(value.nonAdoptionCount),
    referenceCount: toStatisticsCount(value.referenceCount),
    // 服务端声明未计算（提醒为零）或十进制节点取值失败时保持 null，页面按「—」展示，不伪造成 0。
    samePeriodRecognitionRate:
      value.samePeriodRecognitionRateCalculated === true ? toNodeNumber(value.samePeriodRecognitionRate) : null,
    estimatedSavingAmount: toNodeNumber(value.estimatedSavingAmount),
    nonAdoptionReasons: toNonAdoptionReasonViews(value.nonAdoptionReasons),
  }
}

/** 处理结果读模型 → 处理结果视图；决定文本服务端优先，未登记决定按「未知状态」兜底。 */
function toProcessingResultView(value: NonNullable<RecognitionUsageDetailReadModel['processingResult']>): UsageProcessingResultView {
  const decision = typeof value.decision === 'number' ? value.decision : null
  return {
    isProcessed: value.isProcessed === true,
    recognitionTime: toStatisticsDate(value.recognitionTime),
    decision,
    decisionText: statisticsServerText(value.decisionText) ?? recognitionResultText(decision),
    nonAdoptionReasonCode: value.nonAdoptionReasonCode ?? null,
    nonAdoptionReasonName: value.nonAdoptionReasonName ?? null,
    nonAdoptionSupplementDescription: value.nonAdoptionSupplementDescription ?? null,
    estimatedSavingAmount: toNodeNumber(value.estimatedSavingAmount),
  }
}

/** 引用事实读模型 → 引用视图。 */
function toReferenceView(value: NonNullable<RecognitionUsageDetailReadModel['reference']>): UsageReferenceView {
  return {
    isReferenced: value.isReferenced === true,
    referenceTime: toStatisticsDate(value.referenceTime),
    referenceDeptId: value.referenceDeptId ?? null,
    referenceDeptName: value.referenceDeptName ?? null,
    referenceDoctorId: value.referenceDoctorId ?? null,
    referenceDoctorName: value.referenceDoctorName ?? null,
  }
}

/** 明细读模型 → 明细行视图模型：患者姓名与证件号码按服务端原值承载（S6-D9），不做二次处理。 */
function toUsageDetailRow(value: RecognitionUsageDetailReadModel): UsageDetailRow {
  const visitType = typeof value.visitType === 'number' ? value.visitType : null
  return {
    recognitionMatchRecordId: value.recognitionMatchRecordId ?? '',
    recognitionMatchItemId: value.recognitionMatchItemId ?? '',
    matchCreatedTime: toStatisticsDate(value.matchCreatedTime),
    businessTime: toStatisticsDate(value.businessTime),
    visitType,
    visitTypeText: statisticsServerText(value.visitTypeText) ?? visitTypeText(visitType),
    visitSerialNo: value.visitSerialNo ?? '',
    source: toScopeView(value.source),
    receiver: toScopeView(value.receiver),
    item: toItemView(value.item),
    isUnprocessed: value.isUnprocessed === true,
    patientName: value.patientName ?? '',
    identityDocumentNo: value.identityDocumentNo ?? '',
    recognitionDeptId: value.recognitionDeptId ?? null,
    recognitionDeptName: value.recognitionDeptName ?? null,
    recognitionDoctorId: value.recognitionDoctorId ?? null,
    recognitionDoctorName: value.recognitionDoctorName ?? null,
    processingResult: value.processingResult ? toProcessingResultView(value.processingResult) : null,
    reference: value.reference ? toReferenceView(value.reference) : null,
  }
}

/** 匹配项读模型 → 匹配项视图。 */
function toMatchRecordItemView(value: RecognitionMatchRecordItemReadModel): MatchRecordItemView {
  const decision = typeof value.decision === 'number' ? value.decision : null
  return {
    recognitionMatchItemId: value.recognitionMatchItemId ?? '',
    item: toItemView(value.item),
    source: toScopeView(value.source),
    reportId: value.reportId ?? '',
    reportVersionId: value.reportVersionId ?? '',
    isProcessed: value.isProcessed === true,
    decision,
    decisionText: statisticsServerText(value.decisionText) ?? recognitionResultText(decision),
    nonAdoptionReasonCode: value.nonAdoptionReasonCode ?? null,
    nonAdoptionReasonName: value.nonAdoptionReasonName ?? null,
  }
}

/** 汇总读模型集合 → 汇总行视图模型集合。 */
export function toUsageSummaryRows(values: readonly RecognitionUsageSummaryReadModel[]): UsageSummaryRow[] {
  return values.map(toUsageSummaryRow)
}

/** 明细读模型集合 → 明细行视图模型集合。 */
export function toUsageDetailRows(values: readonly RecognitionUsageDetailReadModel[]): UsageDetailRow[] {
  return values.map(toUsageDetailRow)
}

/** 匹配记录读模型 → 集合视图模型。 */
export function toMatchRecordView(value: RecognitionMatchRecordReadModel): MatchRecordView {
  const visitType = typeof value.visitType === 'number' ? value.visitType : null
  return {
    recognitionMatchRecordId: value.recognitionMatchRecordId ?? '',
    matchCreatedTime: toStatisticsDate(value.matchCreatedTime),
    receiver: toScopeView(value.receiver),
    patientName: value.patientName ?? '',
    identityDocumentNo: value.identityDocumentNo ?? '',
    visitType,
    visitTypeText: statisticsServerText(value.visitTypeText) ?? visitTypeText(visitType),
    visitSerialNo: value.visitSerialNo ?? '',
    isProcessed: value.isProcessed === true,
    recognitionTime: toStatisticsDate(value.recognitionTime),
    recognitionDeptId: value.recognitionDeptId ?? null,
    recognitionDeptName: value.recognitionDeptName ?? null,
    recognitionDoctorId: value.recognitionDoctorId ?? null,
    recognitionDoctorName: value.recognitionDoctorName ?? null,
    matchItems: (value.matchItems ?? []).map(toMatchRecordItemView),
  }
}

// ========== 枚举兜底文案（只用于服务端随行文本缺失，页面优先展示服务端文本） ==========

/** 就诊类型取值文案；数值与后端 `VisitType` 一致。 */
const VISIT_TYPE_TEXTS: Readonly<Record<number, string>> = { 1: '门诊', 2: '急诊', 3: '住院', 4: '体检', 5: '其他' }

/** 互认结果取值文案；数值与后端 `RecognitionResult` 一致。 */
const RECOGNITION_RESULT_TEXTS: Readonly<Record<number, string>> = { 1: '采纳', 2: '不采纳' }

function visitTypeText(value: number | null): string {
  return value !== null && VISIT_TYPE_TEXTS[value] !== undefined ? VISIT_TYPE_TEXTS[value] : '未知类型'
}

function recognitionResultText(value: number | null): string {
  return value !== null && RECOGNITION_RESULT_TEXTS[value] !== undefined ? RECOGNITION_RESULT_TEXTS[value] : '未知状态'
}
