/**
 * 互认统计两个适配层（接收侧 / 来源侧）共用的取值域、分页收敛与映射基础工具。
 *
 * 这些事实只服务统计页目录，放在特征内共享而不是某个适配层里，
 * 避免两个适配层互相依赖、各自复制一份取值域判定；
 * 跨统计页之外的复用（如项目类型）继续走 `src/shared/` 的既有模块。
 */
import { DateOnly, type PageInfoDto, type RecognitionStatisticsItemReadModel } from '@dy/api-client-medical-recognition'
import { isMedicalItemTypeValue, medicalItemTypeText, type MedicalItemTypeValue } from '../../shared/medicalItemType'
import { MAX_SELECTABLE_PAGE_SIZE } from '../../shared/tablePagination'

/** 组织归属（编码与名称）：来源侧与接收侧读模型同构，两个适配层共用一个视图结构。 */
export interface StatisticsOrganizationScopeView {
  organizationCode: string
  organizationName: string
  hospitalCode: string
  hospitalName: string
  branchCode: string
  branchName: string
}

/** 互认项目视图：项目类型取值与文本、标准目录分类、分组与标准项目资料。 */
export interface StatisticsItemView {
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  categoryName: string | null
  groupName: string | null
  standardProjectCode: string
  standardProjectName: string
}

/**
 * 汇总维度取值域；数值与后端 `RecognitionStatisticsGroupDimension` 一致
 * （1 医院、2 院区、3 互认科室、4 标准项目）。来源侧不接受互认科室维度，
 * 该业务约束由服务端拒绝，本模块不重复判定。
 */
const STATISTICS_GROUP_DIMENSIONS = Object.freeze([1, 2, 3, 4] as const)
export type StatisticsGroupDimensionValue = (typeof STATISTICS_GROUP_DIMENSIONS)[number]

export function isStatisticsGroupDimensionValue(value: unknown): value is StatisticsGroupDimensionValue {
  return typeof value === 'number' && (STATISTICS_GROUP_DIMENSIONS as readonly number[]).includes(value)
}

/**
 * 接收侧明细类型取值域；数值与后端 `RecognitionUsageDetailType` 一致
 * （1 提醒、2 采纳、3 不采纳、4 引用）。
 */
const USAGE_DETAIL_TYPES = Object.freeze([1, 2, 3, 4] as const)
export type UsageDetailTypeValue = (typeof USAGE_DETAIL_TYPES)[number]

export function isUsageDetailTypeValue(value: unknown): value is UsageDetailTypeValue {
  return typeof value === 'number' && (USAGE_DETAIL_TYPES as readonly number[]).includes(value)
}

/**
 * 统计导出类型取值域；数值与后端 `RecognitionStatisticsExportType` 一致
 * （1 接收侧互认使用汇总、2-5 接收侧四类明细、6 来源医院被互认汇总、7 来源医院被互认明细）。
 * 两个适配层各自只接受本侧取值：接收侧 1-5，来源侧 6-7。
 */
const USAGE_EXPORT_TYPES = Object.freeze([1, 2, 3, 4, 5] as const)
const SOURCE_EXPORT_TYPES = Object.freeze([6, 7] as const)
export type UsageExportTypeValue = (typeof USAGE_EXPORT_TYPES)[number]
export type SourceExportTypeValue = (typeof SOURCE_EXPORT_TYPES)[number]

export function isUsageExportTypeValue(value: unknown): value is UsageExportTypeValue {
  return typeof value === 'number' && (USAGE_EXPORT_TYPES as readonly number[]).includes(value)
}

export function isSourceExportTypeValue(value: unknown): value is SourceExportTypeValue {
  return typeof value === 'number' && (SOURCE_EXPORT_TYPES as readonly number[]).includes(value)
}

/**
 * 统计导出类型中文名；逐项镜像后端 `RecognitionStatisticsExportType` 枚举成员的描述文本，
 * 与服务端导出响应 `Content-Disposition` 的文件名首段同名。
 *
 * 用途边界：只用于下载文件名构造——生成端只透出字节体，不透出响应头，也不携带枚举描述的运行期取值；
 * 页面上的枚举**展示文本**不使用本表，一律取服务端随行返回的文本字段。
 */
const STATISTICS_EXPORT_TYPE_TEXTS: Readonly<Record<number, string>> = {
  1: '接收侧互认使用汇总',
  2: '接收侧提醒明细',
  3: '接收侧采纳明细',
  4: '接收侧不采纳明细',
  5: '接收侧引用明细',
  6: '来源医院被互认汇总',
  7: '来源医院被互认明细',
}

/** 取统计导出类型的中文名；取值越界属于调用方契约破坏，就地失败，不产出错误文件名。 */
export function statisticsExportTypeText(value: number): string {
  const text = STATISTICS_EXPORT_TYPE_TEXTS[value]
  if (text === undefined) throw new Error(`统计导出类型取值越界：${value}。`)
  return text
}

/** 下载名日期段：`YYYY-MM-DD` → `YYYYMMDD`；形态非法属于调用方契约破坏，就地失败。 */
function toExportDownloadDate(value: string): string {
  if (/^\d{4}-\d{2}-\d{2}$/.test(value) === false) throw new Error(`统计导出下载名的日期非法：${value}。`)
  return value.replaceAll('-', '')
}

/**
 * 按服务端命名规则构造导出下载文件名：`导出类型中文名-起始日-结束日.xlsx`
 * （日期为 `yyyyMMdd` 压缩形态，与服务端 `FileName` 的构造一致，例如
 * 「接收侧提醒明细-20260901-20260930.xlsx」）。
 */
export function buildStatisticsExportDownloadName(exportType: number, startTime: string, endTime: string): string {
  return `${statisticsExportTypeText(exportType)}-${toExportDownloadDate(startTime)}-${toExportDownloadDate(endTime)}.xlsx`
}

/**
 * 页容量取值上限：取值来自共享分页控件可选值的末位上限，与服务端公共请求声明的 1-200 同一事实，
 * 统计适配层不另写一份上限数字。
 */
export const STATISTICS_MAX_PAGE_SIZE: number = MAX_SELECTABLE_PAGE_SIZE

/** 页面分页输入：页码从 1 起，页容量在服务端取值域内。 */
export interface StatisticsPageInput {
  pageIndex: number
  pageSize: number
}

/** 统计查询的页面分页模型：由服务端公共分页响应收敛而来，非法响应不进入该模型。 */
export interface StatisticsPage<Row> {
  items: Row[]
  pageIndex: number
  pageSize: number
  totalCount: number
}

/** 页码与页容量的值域判定；与服务端契约一致（页码 ≥1，页容量 1-200）。 */
export function isStatisticsPageInput(page: StatisticsPageInput): boolean {
  return (
    Number.isInteger(page.pageIndex) &&
    page.pageIndex >= 1 &&
    Number.isInteger(page.pageSize) &&
    page.pageSize >= 1 &&
    page.pageSize <= STATISTICS_MAX_PAGE_SIZE
  )
}

/**
 * 生成端交付的分页响应 → 页面分页模型。
 *
 * 页码小于 1、页容量越界、总数为负数或非安全整数一律按契约错误抛出，
 * 不伪造分页状态；当页集合缺失时按空集合处理，行映射由调用方提供。
 */
export function toStatisticsPage<TRaw, TRow>(
  rawItems: readonly TRaw[] | null | undefined,
  page: PageInfoDto | null | undefined,
  toRow: (raw: TRaw) => TRow,
): StatisticsPage<TRow> {
  const pageIndex = page?.pageIndex
  const pageSize = page?.pageSize
  const totalCount = page?.totalCount

  if (typeof pageIndex !== 'number' || !Number.isInteger(pageIndex) || pageIndex < 1) {
    throw new Error('统计分页响应的页码非法。')
  }
  if (typeof pageSize !== 'number' || !Number.isInteger(pageSize) || pageSize < 1 || pageSize > STATISTICS_MAX_PAGE_SIZE) {
    throw new Error('统计分页响应的页容量非法。')
  }
  if (typeof totalCount !== 'number' || !Number.isSafeInteger(totalCount) || totalCount < 0) {
    throw new Error('统计分页响应的总数非法。')
  }

  return { items: (rawItems ?? []).map(toRow), pageIndex, pageSize, totalCount }
}

/**
 * 起止日期文本 → 请求侧日历日期。
 *
 * 只接受 `YYYY-MM-DD` 形态并按年月日直接构造 `DateOnly`，因此不拼接时分秒、不写时区偏移；
 * 生成端的 `DateOnly` 构造不校验月日值域，日历合法性在这里用 UTC 日历复核（2026-13-01 会被拒绝）；
 * 解析失败返回 `null`，由调用方按「条件不完整、不构造请求」或「契约错误」处理。
 */
export function toStatisticsRequestDate(value: string | undefined): DateOnly | null {
  const text = value?.trim() ?? ''
  if (text.length === 0) return null
  const matched = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text)
  if (matched === null) return null
  const year = Number(matched[1])
  const month = Number(matched[2])
  const day = Number(matched[3])
  const calendar = new Date(Date.UTC(year, month - 1, day))
  if (calendar.getUTCFullYear() !== year || calendar.getUTCMonth() !== month - 1 || calendar.getUTCDate() !== day) {
    return null
  }
  try {
    return new DateOnly({ year, month, day })
  } catch {
    return null
  }
}

/**
 * 生成端交付的十进制节点 → 数值（`decimal` 字段被生成为 `UntypedNode`，见生成契约的 format double 警告）。
 * 只接受有限数值；字符串节点、缺失与其它形态一律返回 `null`，不用 `Number()` 把非数值伪造成数字。
 */
export function toNodeNumber(value: unknown): number | null {
  if (typeof value !== 'object' || value === null) return null
  const getValue = (value as { getValue?: unknown }).getValue
  if (typeof getValue !== 'function') return null
  const raw = (getValue as () => unknown).call(value)
  return typeof raw === 'number' && Number.isFinite(raw) ? raw : null
}

/** 服务端随行文本字段：只接受非空白文本，空白视为未交付，交由兜底出口处理。 */
export function statisticsServerText(value: string | null | undefined): string | null {
  const text = value?.trim()
  return text ? text : null
}

/** 组织归属读模型 → 组织归属视图；字段缺失按空串承载，不做补查。 */
export function toScopeView(
  value:
    | { organizationCode?: string | null; organizationName?: string | null; hospitalCode?: string | null; hospitalName?: string | null; branchCode?: string | null; branchName?: string | null }
    | null
    | undefined,
): StatisticsOrganizationScopeView {
  return {
    organizationCode: value?.organizationCode ?? '',
    organizationName: value?.organizationName ?? '',
    hospitalCode: value?.hospitalCode ?? '',
    hospitalName: value?.hospitalName ?? '',
    branchCode: value?.branchCode ?? '',
    branchName: value?.branchName ?? '',
  }
}

/** 互认项目读模型 → 项目视图；服务端随行文本优先，未登记取值按「未知类型」兜底。 */
export function toItemView(value: RecognitionStatisticsItemReadModel | null | undefined): StatisticsItemView {
  const itemType = isMedicalItemTypeValue(value?.itemType) ? value.itemType : null
  return {
    itemType,
    itemTypeText: statisticsServerText(value?.itemTypeText) ?? medicalItemTypeText(itemType),
    categoryName: value?.categoryName ?? null,
    groupName: value?.groupName ?? null,
    standardProjectCode: value?.standardProjectCode ?? '',
    standardProjectName: value?.standardProjectName ?? '',
  }
}

/** 行内计数字段：后端 `int` 非空，生成端缺失或非法时按零承载，不伪造其他数值。 */
export function toStatisticsCount(value: number | null | undefined): number {
  return typeof value === 'number' && Number.isInteger(value) && value >= 0 ? value : 0
}

/** 生成端交付的时间字段 → `Date`；缺失或非法返回 `null`。 */
export function toStatisticsDate(value: Date | null | undefined): Date | null {
  return value instanceof Date && !Number.isNaN(value.getTime()) ? value : null
}

/**
 * 查询条件的可选文本字段归一：去首尾空白，空白从结果中剔除该键。
 * 双版本的键集合是两个变体字段的并集，因此键列表按字符串受理（调用方传编译期字面量数组），
 * 归一结果的类型保持查询条件原形；归一只发生在构造期，原始对象不被改写。
 */
export function normalizeStatisticsTexts<T extends object>(query: T, keys: readonly string[]): T {
  const normalized = { ...query } as Record<string, unknown>
  for (const key of keys) {
    const value = normalized[key]
    if (typeof value !== 'string') continue
    const text = value.trim()
    if (text.length === 0) delete normalized[key]
    else normalized[key] = text
  }
  return normalized as T
}

/** 把可选文本写入请求体字段；未提供的条件不写入该键。 */
export function assignStatisticsText<T extends object, K extends keyof T & string>(
  body: T,
  key: K,
  value: string | undefined,
): void {
  if (value !== undefined) (body as Record<string, unknown>)[key] = value
}

/** 请求侧日期构造：构造期已校验的日期在此二次解析，失败属于调用方契约破坏，就地抛出。 */
export function requireStatisticsDate(value: string, fieldName: string): DateOnly {
  const parsed = toStatisticsRequestDate(value)
  if (parsed === null) throw new Error(`统计查询的${fieldName}非法，不能构造请求。`)
  return parsed
}
