import {
  DateOnly,
  type BranchReportListQueryRequest,
  type CompleteReportSubmissionRequest,
  type ExaminationReportContentViewReadModel,
  type LaboratoryReportContentViewReadModel,
  type MedicalRecognitionClient,
  type MedicalReportListReadModel,
  type MedicalReportVersionDetailQueryReadModel,
  type MedicalReportVersionListReadModel,
  type MedicalReportVoidRequest,
  type PageInfoDto,
  type ReportListQueryRequest,
  type ReportVersionCommonReadModel,
} from '@dy/api-client-medical-recognition'
import { MAX_SELECTABLE_PAGE_SIZE } from '../../shared/tablePagination'

/**
 * 报告管理端的适配层：把生成的 Request 与 ReadModel 转换为稳定视图模型。
 *
 * 边界（遵循 Frontend API Client 第 2、5 节与阶段 4 前端设计）：
 * 1. 本模块不创建 Client、不读取 token、不解析平台错误响应、不新增错误拦截器；
 * 2. 页面只依赖本模块导出的稳定类型与函数，不接触 Kiota 生成模型；
 * 3. 请求与响应直接引用生成类型，不存在第二套契约来源；本模块只保留页面专属的视图模型、
 *    筛选模型与请求载荷。
 * 4. 生成契约的固有形态由本层吸收：
 *    a) 后端必填字段在 Kiota 下仍是可选（`string | null`），因此请求构造逐字段显式赋值，
 *       范围值缺失或不合法时不构造请求，不依赖「省略即由服务端补」，也不退化为全局查询；
 *    b) 分页字段在生成端是 `number | null`，收敛时做安全整数与值域判定，非法一律按约定错误抛出，
 *       不伪造分页状态；
 *    c) 日期筛选字段生成端为 `DateOnly`，请求侧只提交年月日（不含时分秒、不写本地时区偏移），
 *       响应侧统一归一为本地日期文本供页面展示。
 * 5. 患者证件号码与姓名按用户输入原样提交（不做本地大小写或空白改写），规范化由服务端完成。
 */

export type {
  BranchReportListQueryRequest,
  CompleteReportSubmissionRequest,
  ExaminationReportContentViewReadModel,
  LaboratoryReportContentViewReadModel,
  MedicalRecognitionClient,
  MedicalReportListReadModel,
  MedicalReportVersionDetailQueryReadModel,
  MedicalReportVersionListReadModel,
  MedicalReportVoidRequest,
  PageInfoDto,
  ReportListQueryRequest,
  ReportVersionCommonReadModel,
}

/** 报告类型取值域：1=检验报告、2=检查报告；与后端枚举数值一致。 */
export const LABORATORY_REPORT_TYPE = 1
export const EXAMINATION_REPORT_TYPE = 2

/** 报告生命周期状态取值域：1=有效、2=已作废；与后端枚举数值一致。 */
export const EFFECTIVE_REPORT_STATUS = 1
export const VOIDED_REPORT_STATUS = 2

/** 服务端页容量取值域上限；与后端契约声明一致，取值来自共享分页控件的可选值上限。 */
export const MAX_PAGE_SIZE: number = MAX_SELECTABLE_PAGE_SIZE

/** 报告类型取值域守卫。 */
export function isReportTypeValue(value: number | null | undefined): value is typeof LABORATORY_REPORT_TYPE | typeof EXAMINATION_REPORT_TYPE {
  return value === LABORATORY_REPORT_TYPE || value === EXAMINATION_REPORT_TYPE
}

/** 报告状态取值域守卫。 */
export function isReportStatusValue(value: number | null | undefined): value is typeof EFFECTIVE_REPORT_STATUS | typeof VOIDED_REPORT_STATUS {
  return value === EFFECTIVE_REPORT_STATUS || value === VOIDED_REPORT_STATUS
}

/**
 * 报告类型中文兜底文案。
 *
 * 页面展示优先使用服务端随行返回的 `reportTypeText`；只有服务端未交付该字段时才回退到这里，
 * 因此本函数只覆盖页面真正用到的两种取值，不构成第二份文案来源。
 */
export function reportTypeText(value: number | null | undefined): string {
  if (value === LABORATORY_REPORT_TYPE) return '检验报告'
  if (value === EXAMINATION_REPORT_TYPE) return '检查报告'
  return '未知类型'
}

/** 报告状态中文兜底文案；口径同 {@link reportTypeText}。 */
export function reportStatusText(value: number | null | undefined): string {
  if (value === EFFECTIVE_REPORT_STATUS) return '有效'
  if (value === VOIDED_REPORT_STATUS) return '已作废'
  return '未知状态'
}

/** 报告列表行的视图模型；各列都完成可空归一与枚举安全映射。 */
export interface ReportRow {
  reportId: string
  reportNo: string
  reportType: number | null
  reportTypeText: string
  reportTime: Date | null
  organizationName: string
  hospitalName: string
  branchName: string
  patientName: string
  identityDocumentNo: string
  currentVersionSequence: number
  status: number | null
  statusText: string
}

/** 报告版本的视图模型。 */
export interface ReportVersionRow {
  reportVersionId: string
  versionSequence: number
  sourceModifiedTime: Date | null
  platformReceivedTime: Date | null
  reportDoctorName: string | null
  reviewDoctorName: string | null
  inspectorName: string | null
  detailInspectors: string | null
  examinerName: string | null
  sourceReportRemark: string | null
  pdfFileName: string
  isCurrentVersion: boolean
  isSuperseded: boolean
  reportStatus: number | null
  reportStatusText: string
}

/** 版本详情的视图模型：公共信息、检验或检查内容之一、文件信息。 */
export interface ReportVersionDetail {
  reportType: number | null
  reportTypeText: string
  reportNo: string
  reportVersionId: string
  versionSequence: number
  sourceModifiedTime: Date | null
  platformReceivedTime: Date | null
  reportDoctorName: string | null
  reviewDoctorName: string | null
  inspectorName: string | null
  detailInspectors: string | null
  examinerName: string | null
  sourceReportRemark: string | null
  common: ReportVersionCommonReadModel | null
  laboratoryContent: LaboratoryReportContentViewReadModel | null
  examinationContent: ExaminationReportContentViewReadModel | null
  pdfFileName: string
}

/** 页面分页模型：由服务端分页信息收敛而来，非法响应不进入该模型。 */
export interface ReportPage {
  items: ReportRow[]
  pageIndex: number
  pageSize: number
  totalCount: number
}

/** 列表筛选条件；全部可选，缺省表示不施加该条件。 */
export interface ReportFilters {
  organizationCode?: string
  hospitalCode?: string
  branchCode?: string
  reportDateFrom?: string
  reportDateTo?: string
  reportType?: number
  reportNo?: string
  identityDocumentNo?: string
  patientName?: string
}

/** 平台管理员入口的查询：组织、医院、院区随请求提交。 */
export interface ReportListQuery extends ReportFilters {
  organizationCode: string
  hospitalCode: string
  branchCode: string
  pageIndex: number
  pageSize: number
}

/** 医院管理员入口的查询：不提交组织与医院，两者由服务端从可信上下文注入。 */
export interface BranchReportListQuery extends ReportFilters {
  branchCode: string
  pageIndex: number
  pageSize: number
}

/**
 * 把起止日期文本转成请求侧日历日期；空值返回 `null` 表示不下发该字段。
 *
 * 只接受 `YYYY-MM-DD` 形态，直接按年月日构造 `DateOnly`，因此不拼接时分秒、不写时区偏移；
 * 服务端按左闭右开处理区间边界，边界由服务端按日历日计算。
 */
export function toRequestDate(value: string | undefined): DateOnly | null {
  const text = value?.trim() ?? ''
  if (text.length === 0) return null
  const matched = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text)
  if (matched === null) return null
  const year = Number(matched[1])
  const month = Number(matched[2])
  const day = Number(matched[3])
  // DateOnly 构造会拒绝非法年月日，与解析失败一并按不下发处理。
  try {
    return new DateOnly({ year, month, day })
  } catch {
    return null
  }
}

/** 把日期归一为本地 `YYYY-MM-DD` 文本；无业务值返回空串。 */
export function toDisplayDate(value: Date | null | undefined): string {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) return ''
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')
  return `${value.getFullYear()}-${month}-${day}`
}

/** 把日期归一为本地日期时间文本；无业务值返回空串。 */
export function toDisplayDateTime(value: Date | null | undefined): string {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) return ''
  const hour = `${value.getHours()}`.padStart(2, '0')
  const minute = `${value.getMinutes()}`.padStart(2, '0')
  return `${toDisplayDate(value)} ${hour}:${minute}`
}

/**
 * 构造平台管理员入口的列表查询。
 *
 * 组织、医院、院区任一缺失或空白时返回 `null`，禁止退化为全局查询；页码小于 1 或页容量越界同样返回 `null`。
 */
export function buildReportListQuery(filters: ReportFilters, page: { pageIndex: number; pageSize: number }): ReportListQuery | null {
  const organizationCode = filters.organizationCode?.trim() ?? ''
  const hospitalCode = filters.hospitalCode?.trim() ?? ''
  const branchCode = filters.branchCode?.trim() ?? ''
  if (organizationCode.length === 0 || hospitalCode.length === 0 || branchCode.length === 0) return null
  if (!isValidPage(page.pageIndex, page.pageSize)) return null
  return { ...filters, organizationCode, hospitalCode, branchCode, pageIndex: page.pageIndex, pageSize: page.pageSize }
}

/** 构造医院管理员入口的列表查询；院区缺失或空白时返回 `null`，不用可信上下文回填。 */
export function buildBranchReportListQuery(filters: ReportFilters, page: { pageIndex: number; pageSize: number }): BranchReportListQuery | null {
  const branchCode = filters.branchCode?.trim() ?? ''
  if (branchCode.length === 0) return null
  if (!isValidPage(page.pageIndex, page.pageSize)) return null
  return { ...filters, branchCode, pageIndex: page.pageIndex, pageSize: page.pageSize }
}

/** 页码与页容量的值域判定；与后端契约一致。 */
function isValidPage(pageIndex: number, pageSize: number): boolean {
  return Number.isInteger(pageIndex) && pageIndex >= 1 && Number.isInteger(pageSize) && pageSize >= 1 && pageSize <= MAX_PAGE_SIZE
}

/** 生成端交付的列表请求 → 服务端请求体：业务筛选平级，分页放在内嵌 page 对象里。 */
function toReportListBody(query: ReportListQuery): ReportListQueryRequest {
  const body: ReportListQueryRequest = {
    organizationCode: query.organizationCode,
    hospitalCode: query.hospitalCode,
    branchCode: query.branchCode,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyFilters(body, query)
  return body
}

/** 生成端交付的医院管理员入口请求 → 服务端请求体：不含组织与医院。 */
function toBranchReportListBody(query: BranchReportListQuery): BranchReportListQueryRequest {
  const body: BranchReportListQueryRequest = {
    branchCode: query.branchCode,
    page: { pageIndex: query.pageIndex, pageSize: query.pageSize },
  }
  applyFilters(body, query)
  return body
}

/** 把可选筛选条件写入请求体；空值不下发该字段。 */
function applyFilters(
  body: ReportListQueryRequest | BranchReportListQueryRequest,
  filters: ReportFilters,
): void {
  const reportType = filters.reportType
  if (typeof reportType === 'number' && isReportTypeValue(reportType)) body.reportType = reportType

  const reportNo = filters.reportNo?.trim() ?? ''
  if (reportNo.length > 0) body.reportNo = reportNo

  // 患者条件按用户输入原样提交：规范化（去空白、证件号码统一大写）由服务端完成，页面不改写。
  const identityDocumentNo = filters.identityDocumentNo ?? ''
  if (identityDocumentNo.length > 0) body.identityDocumentNo = identityDocumentNo

  const patientName = filters.patientName ?? ''
  if (patientName.length > 0) body.patientName = patientName

  const reportDateFrom = toRequestDate(filters.reportDateFrom)
  if (reportDateFrom !== null) body.reportDateFrom = reportDateFrom

  const reportDateTo = toRequestDate(filters.reportDateTo)
  if (reportDateTo !== null) body.reportDateTo = reportDateTo
}

/** 查询平台管理员入口的报告列表。 */
export async function queryReportPage(client: MedicalRecognitionClient, query: ReportListQuery): Promise<ReportPage> {
  const response = await client.api.medicalRecognitionReportQuery.queryMedicalReportList.post(toReportListBody(query))

  return toReportPage(response?.items, response?.page)
}

/** 查询医院管理员入口的报告列表（本院可信范围内）。 */
export async function queryBranchReportPage(client: MedicalRecognitionClient, query: BranchReportListQuery): Promise<ReportPage> {
  const response = await client.api.medicalRecognitionReportQuery.queryBranchMedicalReportList.post(toBranchReportListBody(query))
  return toReportPage(response?.items, response?.page)
}

/**
 * 生成端交付的分页响应 → 页面分页模型。
 *
 * 页码小于 1、页容量小于 1 或超过上限、总数为负数或非安全整数一律按约定错误抛出，
 * 不伪造分页状态；当页数据缺失时按空集合处理。
 */
export function toReportPage(
  items: readonly MedicalReportListReadModel[] | null | undefined,
  page: PageInfoDto | null | undefined,
): ReportPage {
  const pageIndex = page?.pageIndex
  const pageSize = page?.pageSize
  const totalCount = page?.totalCount

  if (typeof pageIndex !== 'number' || !Number.isInteger(pageIndex) || pageIndex < 1) throw new Error('报告列表分页信息的页码非法。')
  if (typeof pageSize !== 'number' || !Number.isInteger(pageSize) || pageSize < 1 || pageSize > MAX_PAGE_SIZE) throw new Error('报告列表分页信息的页容量非法。')
  if (typeof totalCount !== 'number' || !Number.isSafeInteger(totalCount) || totalCount < 0) throw new Error('报告列表分页信息的总数非法。')

  return { items: toReportRows(items ?? []), pageIndex, pageSize, totalCount }
}

/** 读模型 → 报告行视图模型；服务端未交付的文本字段回退到本模块兜底出口。 */
export function toReportRows(values: readonly MedicalReportListReadModel[]): ReportRow[] {
  return values.map((value) => {
    const reportType = typeof value.reportType === 'number' ? value.reportType : null
    const status = typeof value.status === 'number' ? value.status : null
    return {
      reportId: value.reportId ?? '',
      reportNo: value.reportNo ?? '',
      reportType,
      reportTypeText: serverText(value.reportTypeText) ?? reportTypeText(reportType),
      reportTime: toDate(value.reportTime),
      organizationName: value.organizationName ?? '',
      hospitalName: value.hospitalName ?? '',
      branchName: value.branchName ?? '',
      patientName: value.patientName ?? '',
      identityDocumentNo: value.identityDocumentNo ?? '',
      currentVersionSequence: typeof value.currentVersionSequence === 'number' ? value.currentVersionSequence : 0,
      status,
      statusText: serverText(value.statusText) ?? reportStatusText(status),
    }
  })
}

/** 版本读模型 → 版本视图模型；责任人员只取该版本自身提供的事实，不做拼接与优先级取值。 */
export function toReportVersionRows(values: readonly MedicalReportVersionListReadModel[]): ReportVersionRow[] {
  return values.map((value) => {
    const reportStatus = typeof value.reportStatus === 'number' ? value.reportStatus : null
    return {
      reportVersionId: value.reportVersionId ?? '',
      versionSequence: typeof value.versionSequence === 'number' ? value.versionSequence : 0,
      sourceModifiedTime: toDate(value.sourceModifiedTime),
      platformReceivedTime: toDate(value.platformReceivedTime),
      reportDoctorName: serverText(value.reportDoctorName),
      reviewDoctorName: serverText(value.reviewDoctorName),
      inspectorName: serverText(value.inspectorName),
      detailInspectors: serverText(value.detailInspectors),
      examinerName: serverText(value.examinerName),
      sourceReportRemark: serverText(value.sourceReportRemark),
      pdfFileName: value.pdfFileName ?? '',
      isCurrentVersion: value.isCurrentVersion === true,
      isSuperseded: value.isSuperseded === true,
      reportStatus,
      reportStatusText: serverText(value.reportStatusText) ?? reportStatusText(reportStatus),
    }
  })
}

/** 版本详情读模型 → 详情视图模型；检验与检查内容按报告类型二选一返回。 */
export function toReportVersionDetail(value: MedicalReportVersionDetailQueryReadModel): ReportVersionDetail {
  const reportType = typeof value.reportType === 'number' ? value.reportType : null
  return {
    reportType,
    reportTypeText: serverText(value.reportTypeText) ?? reportTypeText(reportType),
    reportNo: value.reportNo ?? '',
    reportVersionId: value.reportVersionId ?? '',
    versionSequence: typeof value.versionSequence === 'number' ? value.versionSequence : 0,
    sourceModifiedTime: toDate(value.sourceModifiedTime),
    platformReceivedTime: toDate(value.platformReceivedTime),
    reportDoctorName: serverText(value.reportDoctorName),
    reviewDoctorName: serverText(value.reviewDoctorName),
    inspectorName: serverText(value.inspectorName),
    detailInspectors: serverText(value.detailInspectors),
    examinerName: serverText(value.examinerName),
    sourceReportRemark: serverText(value.sourceReportRemark),
    common: value.content?.common ?? null,
    laboratoryContent: value.content?.laboratoryContent ?? null,
    examinationContent: value.content?.examinationContent ?? null,
    pdfFileName: value.file?.fileName ?? '',
  }
}

/** 查询某个报告的全部历史版本。 */
export async function queryReportVersions(client: MedicalRecognitionClient, reportId: string): Promise<ReportVersionRow[]> {
  const values = await client.api.medicalRecognitionReportQuery.queryMedicalReportVersionList.post({ reportId })
  return toReportVersionRows(values ?? [])
}

/** 查询某个报告版本的完整内容。 */
export async function queryReportVersionDetail(
  client: MedicalRecognitionClient,
  reportId: string,
  reportVersionId: string,
): Promise<ReportVersionDetail> {
  const value = await client.api.medicalRecognitionReportQuery.queryMedicalReportVersionDetail.post({ reportId, reportVersionId })
  if (value === undefined || value === null) throw new Error('报告版本详情未返回内容。')
  return toReportVersionDetail(value)
}

/** 作废一份报告；报告类型决定调用哪一个作废入口。 */
export async function voidReport(
  client: MedicalRecognitionClient,
  reportType: number | null,
  request: MedicalReportVoidRequest,
): Promise<void> {
  if (reportType === EXAMINATION_REPORT_TYPE) {
    await client.api.medicalRecognitionReport.voidExaminationReport.post(request)
    return
  }
  if (reportType === LABORATORY_REPORT_TYPE) {
    await client.api.medicalRecognitionReport.voidLaboratoryReport.post(request)
    return
  }
  throw new Error('报告类型未知，不能作废。')
}

/**
 * 下载某个报告版本的原始 PDF。
 *
 * 使用已鉴权客户端取二进制（生成端已声明返回 `ArrayBuffer`），按该版本保存的下载名触发浏览器保存，
 * 并在保存后释放对象地址；失败不产生本地错误提示，按既有失败态口径由宿主统一展示。
 */
export async function downloadReportVersionPdf(
  client: MedicalRecognitionClient,
  reportId: string,
  reportVersionId: string,
  downloadFileName: string,
): Promise<void> {
  const content = await client.api.v1.reportPdf
    .byReportId(reportId)
    .versions.byReportVersionId(reportVersionId)
    .pdf.get()
  if (content === undefined || content === null) throw new Error('报告版本 PDF 未返回文件内容。')

  const objectUrl = URL.createObjectURL(new Blob([content as ArrayBufferPart], { type: 'application/pdf' }))
  try {
    const anchor = document.createElement('a')
    anchor.href = objectUrl
    anchor.download = downloadFileName
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    URL.revokeObjectURL(objectUrl)
  }
}

/** 二进制片段类型别名：只用于把 `ArrayBuffer` 交给 `Blob`，不引入额外依赖。 */
type ArrayBufferPart = ArrayBuffer

/** 服务端文本字段：只接受非空白文本，空白视为未交付。 */
function serverText(value: string | null | undefined): string | null {
  const text = value?.trim()
  return text ? text : null
}

/** 生成端交付的日期 → `Date`；缺失或非法返回 `null`。 */
function toDate(value: Date | null | undefined): Date | null {
  return value instanceof Date && !Number.isNaN(value.getTime()) ? value : null
}
