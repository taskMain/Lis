import type {
  BranchRecognitionAmountListQueryRequest,
  MedicalRecognitionClient,
  RecognitionAmountListQueryRequest,
  RecognitionAmountReadModel,
  SaveBranchRecognitionAmountRequest,
  SaveOrganizationHospitalBranchRecognitionAmountRequest,
} from '@dy/api-client-medical-recognition'
import {
  isMedicalItemTypeValue,
  medicalItemTypeText,
  type MedicalItemTypeValue,
} from '../../shared/medicalItemType'
import {
  CONFIGURATION_STATUSES,
  CONFIGURATION_STATUS_TEXTS,
  DISABLED_CONFIGURATION_STATUS,
  ENABLED_CONFIGURATION_STATUS,
  configurationStatusText,
  isConfigurationStatusValue,
  type ConfigurationStatusValue,
} from '../../shared/configurationStatus'

/**
 * 生成的契约类型经本模块转出，页面与测试只从适配层取类型，不依赖生成包深层路径或
 * 生成包的传递依赖（`@microsoft/kiota-abstractions` 不是本子应用的直接依赖）。
 */
export type {
  BranchRecognitionAmountListQueryRequest,
  MedicalRecognitionClient,
  RecognitionAmountListQueryRequest,
  RecognitionAmountReadModel,
  SaveBranchRecognitionAmountRequest,
  SaveOrganizationHospitalBranchRecognitionAmountRequest,
}

/**
 * 互认项目金额的适配层：把两个金额入口的 Request/ReadModel 契约与页面稳定视图模型隔开。
 *
 * 边界（遵循 Frontend API Client 第 2、5 节）：
 * 1. 本模块不创建 Client、不读取 token、不解析平台错误响应、不新增错误拦截器；
 * 2. 页面只依赖本模块导出的稳定类型与函数，不接触 Kiota 生成模型；
 * 3. 请求与响应的**契约类型直接引用生成类型**，不存在第二套契约来源；本模块只保留页面专属的
 *    视图模型、筛选模型与请求载荷。
 * 4. 生成契约的三处固有形态由本层吸收（生成物不得手改）：
 *    a) 组织、医院、院区与标准项目编码在后端是必填（`[Required]` + `[NonEmpty]`），但 Kiota 未把
 *       OpenAPI 的 `required` 映射为 TS 必填，生成成 `string | null` 可选。因此请求构造**逐字段显式赋值**，
 *       且三项范围值任一缺失即不构造请求，不依赖「省略即由服务端补」，也不退化为全局查询。
 *       两个入口的字段集合不同：平台管理员入口按请求携带组织、医院、院区；医院管理员入口的组织与医院
 *       由服务端从可信上下文注入，请求只提交院区，本层不得替它补上组织或医院。
 *    b) `itemType` / `configurationStatus` 在后端是数值枚举，契约按数值交互：服务端用 SourceGen 描述器
 *       在 OpenAPI 里声明 `enum` / `x-enumNames` / `x-enumDescriptions`，生成端据此把这两个属性映射为
 *       `number | null` 并用 `writeNumberValue` 写出数值。本层只保留最小数值映射：只接受已确认取值，
 *       未知一律映射为 `null`（安全展示），不让页面出现第二套枚举判定。
 *    c) `currentAmount` 在后端是 `decimal?`，OpenAPI 生成 `type: ["null","number","string"]` + `format: double`，
 *       生成端不支持该格式、把它生成为 `UntypedNode | null`（`{ value, getValue() }`，反序列化时按 JSON 实际
 *       类型包成数值节点或字符串节点），写入时经 `writeObjectValue` 取节点值。因此本层：
 *       - 读取时按结构化取值（`getValue()`），只接受有限数值，字符串节点与其它形态一律按「无金额」处理；
 *       - 提交时把金额包成同一节点形态，零元照常提交，**不得**提交裸数字（生成端会把它写成对象）。
 *       契约侧修复（把该属性生成为数值类型）属于生成管线的范围，本层不猜测兼容。
 */

/** 项目类型的取值域判定与兜底文案已上移到跨页面共享模块（阶段 1/2 使用同一份事实）。 */
export { isMedicalItemTypeValue, medicalItemTypeText }
export type { MedicalItemTypeValue }
export {
  CONFIGURATION_STATUSES,
  DISABLED_CONFIGURATION_STATUS,
  ENABLED_CONFIGURATION_STATUS,
  CONFIGURATION_STATUS_TEXTS,
  configurationStatusText,
  isConfigurationStatusValue,
}
export type { ConfigurationStatusValue }

/**
 * 生成端交付的金额读取形态：`decimal?` 被生成为未定型节点（见文件头第 4c 条）。
 *
 * 本层只声明读取需要的结构，不从生成包深层路径导入 `UntypedNode`：该类型由生成包的传递依赖
 * `@microsoft/kiota-abstractions` 提供，不是本子应用的直接依赖，而生成包的公开入口只转出契约类型。
 */
export interface RecognitionAmountReadModelLike {
  standardProjectCode?: string | null
  standardProjectName?: string | null
  itemType?: number | null
  /** 项目类型中文；由服务端按枚举声明返回，取值未登记时为 `null`。 */
  itemTypeText?: string | null
  categoryName?: string | null
  groupName?: string | null
  organizationName?: string | null
  hospitalName?: string | null
  branchName?: string | null
  configurationStatus?: number | null
  /** 配置状态中文；由服务端按枚举声明返回。 */
  configurationStatusText?: string | null
  unavailableReason?: string | null
  currentAmount?: unknown
  isAmountConfigured?: boolean | null
}

/** 生成端写入金额要求的节点形态；`writeObjectValue` 只在该形态下把金额写成数值。 */
export interface UntypedNumberNode {
  value: unknown
  getValue(): unknown
}

/** 金额行视图模型；只承载查询契约交付的字段，且各列都已完成可空归一与枚举安全映射。 */
export interface RecognitionAmountRow {
  standardProjectCode: string | null
  standardProjectName: string | null
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  categoryName: string | null
  groupName: string | null
  organizationName: string | null
  hospitalName: string | null
  branchName: string | null
  configurationStatus: ConfigurationStatusValue | null
  configurationStatusText: string
  /** `null` 表示标准目录三层全部启用；非 `null` 时是目录停用原因，不表达配置自身停用。 */
  unavailableReason: string | null
  /** 当前金额（元）；`null` 表示未配置，与已配置的 `0` 严格区分。 */
  currentAmount: number | null
  /** 金额已配置；与 `currentAmount` 同源归一，两者不会出现「未配置 + 有金额」的混态。 */
  isAmountConfigured: boolean
}

/**
 * 平台管理员入口（组织、医院、院区三级）的查询条件；三项范围值都是请求参数，与服务端字段一致。
 * 标准项目编码与关键字都可选，缺省表示不按该项过滤；关键字由构造入口去空白后承载。
 */
export interface RecognitionAmountListQuery {
  organizationCode: string
  hospitalCode: string
  branchCode: string
  standardProjectCode?: string
  /** 关键字；只承载去空白后仍有内容的值，空白与缺省都不携带该字段。 */
  keyword?: string
}

/** 医院管理员入口的查询条件；组织与医院取可信上下文，请求**不含**这两项。 */
export interface BranchAmountListQuery {
  branchCode: string
  standardProjectCode?: string
  /** 关键字；只承载去空白后仍有内容的值，空白与缺省都不携带该字段。 */
  keyword?: string
}

/** 平台管理员入口的保存载荷；组织、医院、院区、标准项目编码与金额都显式提交。 */
export interface RecognitionAmountSavePayload {
  organizationCode: string
  hospitalCode: string
  branchCode: string
  standardProjectCode: string
  currentAmount: number
}

/** 医院管理员入口的保存载荷；不含组织与医院（服务端从可信上下文注入）。 */
export interface BranchAmountSavePayload {
  branchCode: string
  standardProjectCode: string
  currentAmount: number
}

/** 十进制文本（整数或最多两位小数）；指数形式与其它形态都不是可比较的十进制文本。 */
const DECIMAL_AMOUNT_TEXT = /^\d+(?:\.\d{1,2})?$/

/**
 * 十进制文本 → 两位小数的**纯数字串**：`12.5` → `1250`、`12.34` → `1234`、
 * `9007199254740992` → `900719925474099200`。整数部分去前导零，小数部分缺失或只有一位时补足两位。
 * 判据只做字符串比较（不做数值比较：数值比较恒真，等于没判），不改变返回的数值。
 */
function toAmountDigits(text: string): string {
  const [integerPart, fractionPart = ''] = text.split('.')
  return `${integerPart.replace(/^0+(?=\d)/, '')}${fractionPart.padEnd(2, '0')}`
}

/**
 * 金额输入解析：只接受非负且最多两位小数的十进制文本，其余一律拒绝（含负数、超过两位小数、
 * 千分位、科学计数法与全角数字）。返回的数值最多两位小数，避免浮点尾差直接提交。
 *
 * 十进制文本还要过两道**等价性**判据，任一不成立即返回 `null`，按非法输入处理（与其它非法输入
 * 同样的行内提示与零写请求路径）。两道判据都只判定「该十进制能否被原样承载与写出」，
 * 不引入任何金额业务上限，也不用阈值近似：
 * 1. 内存面：把解析后的数值重新按两位小数格式化，与输入的规范化文本比较。双精度对较长的十进制会
 *    漂移（`Number('99999999999999.99')` 得到 `99999999999999.98`），不一致即说明内存里的值已不是
 *    用户输入的值。
 * 2. 序列化面：把数值的**最短往返序列化文本**（提交时生成端写出的就是该文本）按两位小数展开后与
 *    输入逐位比较。有些十进制在内存里可精确表示、却写不出等值文本（`2251799813685247.75` 的数值
 *    精确，但序列化文本是 `2251799813685247.8`），上线后服务端会落成另一个十进制且不报错；
 *    极大值的序列化文本还可能是指数形式（如 `1e+21`），不是可比较的十进制文本，同样直接拒绝。
 */
export function parseRecognitionAmount(input: string): number | null {
  const text = input.trim()
  if (!DECIMAL_AMOUNT_TEXT.test(text)) return null
  const amount = Number(text)
  if (!Number.isFinite(amount) || amount < 0) return null
  const expected = toAmountDigits(text)
  const normalized = Math.round(amount * 100) / 100
  if (toAmountDigits(normalized.toFixed(2)) !== expected) return null
  const serialized = JSON.stringify(normalized)
  if (!DECIMAL_AMOUNT_TEXT.test(serialized)) return null
  return toAmountDigits(serialized) === expected ? normalized : null
}

/**
 * 构造平台管理员页查询请求；组织、医院、院区任一缺失或空白时返回 `null`，
 * 禁止退化为全局查询或硬编码默认范围。标准项目编码缺省时不下发该字段。
 */
export function buildRecognitionAmountListQuery(input: {
  organizationCode: string
  hospitalCode: string
  branchCode: string
  standardProjectCode?: string
  keyword?: string
}): RecognitionAmountListQuery | null {
  const organizationCode = input.organizationCode.trim()
  const hospitalCode = input.hospitalCode.trim()
  const branchCode = input.branchCode.trim()
  if (organizationCode.length === 0 || hospitalCode.length === 0 || branchCode.length === 0) return null
  const standardProjectCode = input.standardProjectCode?.trim() ?? ''
  const keyword = input.keyword?.trim() ?? ''
  return {
    organizationCode,
    hospitalCode,
    branchCode,
    ...(standardProjectCode.length === 0 ? {} : { standardProjectCode }),
    ...(keyword.length === 0 ? {} : { keyword }),
  }
}

/**
 * 构造医院管理员页查询请求；院区缺失或空白时返回 `null`，不用可信上下文回填、不发空院区请求。
 * 请求**不含组织与医院**：两者由服务端从可信上下文注入，前端提交即越权覆盖。
 */
export function buildBranchAmountListQuery(input: {
  branchCode: string
  standardProjectCode?: string
  keyword?: string
}): BranchAmountListQuery | null {
  const branchCode = input.branchCode.trim()
  if (branchCode.length === 0) return null
  const standardProjectCode = input.standardProjectCode?.trim() ?? ''
  const keyword = input.keyword?.trim() ?? ''
  return {
    branchCode,
    ...(standardProjectCode.length === 0 ? {} : { standardProjectCode }),
    ...(keyword.length === 0 ? {} : { keyword }),
  }
}

/**
 * 构造平台管理员页保存载荷；金额非法、标准项目编码空白或三层范围值缺失时返回 `null`，
 * 调用方必须据此**零写请求**。金额按非负数值提交，零元是有效配置。
 */
export function buildRecognitionAmountSavePayload(
  input: { organizationCode: string; hospitalCode: string; branchCode: string; amountInput: string },
  standardProjectCode: string,
): RecognitionAmountSavePayload | null {
  const query = buildRecognitionAmountListQuery({ ...input, standardProjectCode })
  const code = standardProjectCode.trim()
  const amount = parseRecognitionAmount(input.amountInput)
  if (query === null || code.length === 0 || amount === null) return null
  return {
    organizationCode: query.organizationCode,
    hospitalCode: query.hospitalCode,
    branchCode: query.branchCode,
    standardProjectCode: code,
    currentAmount: amount,
  }
}

/**
 * 构造医院管理员页保存载荷；院区、标准项目编码与金额任一非法时返回 `null`。
 * 载荷**不含组织与医院**，两者由服务端从可信上下文注入。
 */
export function buildBranchAmountSavePayload(
  input: { branchCode: string; amountInput: string },
  standardProjectCode: string,
): BranchAmountSavePayload | null {
  const branchCode = input.branchCode.trim()
  const code = standardProjectCode.trim()
  const amount = parseRecognitionAmount(input.amountInput)
  if (branchCode.length === 0 || code.length === 0 || amount === null) return null
  return { branchCode, standardProjectCode: code, currentAmount: amount }
}

/** 未知枚举一律映射为 `null`（安全展示），不默认成已知取值；判定走共享取值域守卫。 */
function toMedicalItemType(value: number | null | undefined): MedicalItemTypeValue | null {
  return isMedicalItemTypeValue(value) ? value : null
}

function toConfigurationStatus(value: number | null | undefined): ConfigurationStatusValue | null {
  return isConfigurationStatusValue(value) ? value : null
}

/**
 * 生成端交付的金额节点 → 数值。节点经 `getValue()` 取原始 JSON 值，只接受有限数值：
 * 字符串节点、缺失与其它形态一律按「无金额」处理，不用 `Number(value)` 把非数值伪造成金额。
 */
function toAmountNumber(value: unknown): number | null {
  if (typeof value !== 'object' || value === null) return null
  const getValue = (value as { getValue?: unknown }).getValue
  if (typeof getValue !== 'function') return null
  const raw = (getValue as () => unknown).call(value)
  return typeof raw === 'number' && Number.isFinite(raw) ? raw : null
}

/**
 * 服务端枚举中文：只接受非空白文本，空白视为未交付，交由兜底出口处理。
 */
function serverEnumText(value: string | null | undefined): string | null {
  const text = value?.trim()
  return text ? text : null
}

/**
 * 读模型 → 金额行视图模型。当前金额与「已配置」标记按同一判定归一：
 * 只有同时满足「已配置为真」且「金额是有限数值」时才呈现金额，其余一律归为未配置。
 * 这样未配置行不会带着一个残留数字展示，已配置的零元也不会被并成未配置（设计「未配置与零元必须可区分」）。
 * 两个枚举中文以服务端返回的字段为准（S3-D21、总体设计 5.3），服务端未交付时才回退到本模块的兜底出口；
 * 本模块不依赖兄弟页面的模块，使票 07 的页面组件与适配层可以独立演进。
 */
export function toRecognitionAmountRows(
  values: readonly RecognitionAmountReadModelLike[],
): RecognitionAmountRow[] {
  return values.map((value) => {
    const itemType = toMedicalItemType(value.itemType)
    const configurationStatus = toConfigurationStatus(value.configurationStatus)
    const amount = toAmountNumber(value.currentAmount)
    const isAmountConfigured = value.isAmountConfigured === true && amount !== null
    return {
      standardProjectCode: value.standardProjectCode ?? null,
      standardProjectName: value.standardProjectName ?? null,
      itemType,
      itemTypeText: serverEnumText(value.itemTypeText) ?? medicalItemTypeText(itemType),
      categoryName: value.categoryName ?? null,
      groupName: value.groupName ?? null,
      organizationName: value.organizationName ?? null,
      hospitalName: value.hospitalName ?? null,
      branchName: value.branchName ?? null,
      configurationStatus,
      configurationStatusText: serverEnumText(value.configurationStatusText) ?? configurationStatusText(configurationStatus),
      unavailableReason: value.unavailableReason ?? null,
      currentAmount: isAmountConfigured ? amount : null,
      isAmountConfigured,
    }
  })
}

/**
 * 金额列展示：未配置与已配置的零元必须可区分，未配置不显示为 0.00；
 * 已配置金额统一按两位小数呈现（页面列与弹窗取值共用同一出口）。
 */
export function currentAmountText(row: Pick<RecognitionAmountRow, 'currentAmount' | 'isAmountConfigured'>): string {
  return row.isAmountConfigured && row.currentAmount !== null ? row.currentAmount.toFixed(2) : '未配置'
}

/** 四栏层级列视图中的项目行；只承载表格项目列需要的字段。 */
export interface RecognitionAmountColumnProjectRow {
  /** 标准项目编码；组内按该编码升序排列。 */
  standardProjectCode: string | null
  standardProjectName: string | null
  /** 当前金额（元）；`null` 表示未配置，与已配置的 `0` 严格区分。 */
  currentAmount: number | null
  isAmountConfigured: boolean
  configurationStatus: ConfigurationStatusValue | null
  configurationStatusText: string
  unavailableReason: string | null
}

/** 分组栏：`rowSpan` 是该分组格纵向跨越的项目行数。 */
export interface RecognitionAmountColumnGroup {
  groupName: string | null
  rowSpan: number
  rows: RecognitionAmountColumnProjectRow[]
}

/** 分类栏：同名分类格纵向跨其名下全部项目行，`rowSpan` 即这些行的行数。 */
export interface RecognitionAmountColumnCategory {
  categoryName: string | null
  rowSpan: number
  groups: RecognitionAmountColumnGroup[]
}

/** 类型栏：同名类型格纵向跨其名下全部项目行，`rowSpan` 即这些行的行数。 */
export interface RecognitionAmountColumnType {
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  rowSpan: number
  categories: RecognitionAmountColumnCategory[]
}

/** 四栏层级列视图：类型 → 分类 → 分组 → 项目行，页面按该结构渲染层级表格。 */
export interface RecognitionAmountColumnView {
  types: RecognitionAmountColumnType[]
}

/** 金额状态的本地取值域：全部、已配置、未配置；按字符串字面量承载，不引入枚举类型。 */
export type AmountStatusFilter = 'all' | 'configured' | 'unconfigured'

/** 互认配置状态的本地取值域：全部、启用、停用（对应数值 1、2）。 */
export type AmountConfigurationStatusFilter = 'all' | 'enabled' | 'disabled'

/**
 * 本地裁剪条件：金额状态取 `isAmountConfigured`、互认配置状态取 `configurationStatus`，
 * 两个条件组合时取交集；关键字由查询请求下推给服务端，不参与本地裁剪。
 */
export interface RecognitionAmountLocalFilter {
  amountStatus: AmountStatusFilter
  configurationStatus: AmountConfigurationStatusFilter
}

/**
 * 地址栏承载的三个**已生效**条件；关键字栏承载的是提交生效的关键字，
 * 输入原文是草稿，草稿不写入地址栏（由页面保证，本层只负责解析与回写）。
 */
export interface RecognitionAmountUrlFilter extends RecognitionAmountLocalFilter {
  keyword: string
}

/** 地址栏三个参数都缺省或不可识别时的默认条件。 */
export const EMPTY_RECOGNITION_AMOUNT_FILTER: RecognitionAmountUrlFilter = {
  keyword: '',
  amountStatus: 'all',
  configurationStatus: 'all',
}

/** 金额状态筛选档位；平台管理员页与医院管理员页共用同一份定义，取值域即 `AmountStatusFilter`，档位名逐字一致。 */
export const AMOUNT_STATUS_OPTIONS: { value: AmountStatusFilter; label: string }[] = [
  { value: 'all', label: '全部' },
  { value: 'configured', label: '已配置' },
  { value: 'unconfigured', label: '未配置' },
]

/** 互认配置状态筛选档位；两个页面共用同一份定义，取值域即 `AmountConfigurationStatusFilter`，档位名逐字一致。 */
export const CONFIGURATION_STATUS_OPTIONS: { value: AmountConfigurationStatusFilter; label: string }[] = [
  { value: 'all', label: '全部' },
  { value: 'enabled', label: '启用' },
  { value: 'disabled', label: '停用' },
]

/** 空态文案：有生效条件但可见行未命中时展示（两个页面共用，中文逐字一致）。 */
export const EMPTY_FILTERED_RECOGNITION_AMOUNT_TEXT = '没有符合当前筛选条件的互认项目金额'

/** 空态文案：无生效条件、当前范围零配置时展示（两个页面共用，中文逐字一致）。 */
export const EMPTY_UNFILTERED_RECOGNITION_AMOUNT_TEXT = '当前范围没有互认项目金额配置'

/** 金额校验文案：行内提示与「零写请求」共用同一取值依据（本层 `parseRecognitionAmount`，两个页面共用）。 */
export const AMOUNT_VALIDATION_MESSAGE = '金额必须是不小于 0 且最多两位小数的数字'

/**
 * 列表读取失败告警的中文文案（两个页面共用，中文逐字一致）：
 * `refresh*` 面向写入成功后的重载失败；纯读取失败不渲染横幅（D-06）——错误由宿主统一提示、
 * 重试经查询按钮，已有数据保留与维护操作禁用的口径由页面状态承载，无需横幅文案。
 */
export const AMOUNT_LOAD_FAILURE_TEXT = {
  refreshTitle: '写入已成功，数据待刷新',
  refreshDescription: '写入已经成功，但重新读取金额列表失败；已有数据保留，维护操作暂不可用。',
}

/** 文本比较：空值按空串参与升序，比较只按字符码位，不依赖区域设置。 */
function compareNullableText(a: string | null, b: string | null): number {
  const left = a ?? ''
  const right = b ?? ''
  return left < right ? -1 : left > right ? 1 : 0
}

/** 类型比较：数值升序，未知类型（`null`）排在已知类型之后。 */
function compareItemTypeForView(a: MedicalItemTypeValue | null, b: MedicalItemTypeValue | null): number {
  if (a === null) return b === null ? 0 : 1
  if (b === null) return -1
  return a - b
}

/** 项目行裁出表格项目列需要的字段；类型、分类与分组的归属由外层三栏承载，行内不再重复。 */
function toColumnProjectRow(row: RecognitionAmountRow): RecognitionAmountColumnProjectRow {
  return {
    standardProjectCode: row.standardProjectCode,
    standardProjectName: row.standardProjectName,
    currentAmount: row.currentAmount,
    isAmountConfigured: row.isAmountConfigured,
    configurationStatus: row.configurationStatus,
    configurationStatusText: row.configurationStatusText,
    unavailableReason: row.unavailableReason,
  }
}

/**
 * 把金额行组织成「类型 → 分类 → 分组 → 项目行」的四栏层级列视图，供页面渲染表格：
 * 组间按 `itemType`（数值升序，未知类型排在已知类型之后）、`categoryName`、`groupName` 归类排序，
 * 组内按 `standardProjectCode` 升序；同名类型格与分类格记录跨其名下全部行的 `rowSpan`，
 * 分组格同样记录 `rowSpan`，页面据此合并同名格。空集合返回空结构。
 */
export function toRecognitionAmountColumnView(rows: readonly RecognitionAmountRow[]): RecognitionAmountColumnView {
  const typeBuckets = new Map<
    MedicalItemTypeValue | null,
    { itemTypeText: string; categories: Map<string | null, Map<string | null, RecognitionAmountColumnProjectRow[]>> }
  >()
  for (const row of rows) {
    let typeBucket = typeBuckets.get(row.itemType)
    if (typeBucket === undefined) {
      // 同类型格的展示中文取该类型下首个行携带的文本。
      typeBucket = { itemTypeText: row.itemTypeText, categories: new Map() }
      typeBuckets.set(row.itemType, typeBucket)
    }
    let groups = typeBucket.categories.get(row.categoryName)
    if (groups === undefined) {
      groups = new Map()
      typeBucket.categories.set(row.categoryName, groups)
    }
    let groupRows = groups.get(row.groupName)
    if (groupRows === undefined) {
      groupRows = []
      groups.set(row.groupName, groupRows)
    }
    groupRows.push(toColumnProjectRow(row))
  }

  const types = [...typeBuckets.entries()]
    .map(([itemType, typeBucket]) => {
      const categories = [...typeBucket.categories.entries()]
        .map(([categoryName, groups]) => {
          const groupColumns = [...groups.entries()]
            .map(([groupName, groupRows]) => ({
              groupName,
              rowSpan: groupRows.length,
              rows: [...groupRows].sort((left, right) => compareNullableText(left.standardProjectCode, right.standardProjectCode)),
            }))
            .sort((left, right) => compareNullableText(left.groupName, right.groupName))
          const categoryRowSpan = groupColumns.reduce((total, group) => total + group.rowSpan, 0)
          return { categoryName, rowSpan: categoryRowSpan, groups: groupColumns }
        })
        .sort((left, right) => compareNullableText(left.categoryName, right.categoryName))
      const typeRowSpan = categories.reduce((total, category) => total + category.rowSpan, 0)
      return { itemType, itemTypeText: typeBucket.itemTypeText, rowSpan: typeRowSpan, categories }
    })
    .sort((left, right) => compareItemTypeForView(left.itemType, right.itemType))
  return { types }
}

/**
 * 把服务端返回的金额行按本地状态条件裁剪；两个条件取交集，`'all'` 档不施加该条件的限制。
 * 只做本地裁剪，不发起任何请求；未知配置状态（`null`）在启用/停用档下都被排除，
 * 在全部档下保留，与行视图模型「未知一律安全兜底」的口径一致。
 */
export function filterRecognitionAmountRows(
  rows: readonly RecognitionAmountRow[],
  filter: RecognitionAmountLocalFilter,
): RecognitionAmountRow[] {
  return rows.filter((row) => {
    const amountMatches =
      filter.amountStatus === 'all' ||
      (filter.amountStatus === 'configured' ? row.isAmountConfigured : !row.isAmountConfigured)
    const configurationMatches =
      filter.configurationStatus === 'all' ||
      (filter.configurationStatus === 'enabled'
        ? row.configurationStatus === ENABLED_CONFIGURATION_STATUS
        : row.configurationStatus === DISABLED_CONFIGURATION_STATUS)
    return amountMatches && configurationMatches
  })
}

/** 地址栏里的三个参数名：关键字、金额状态与互认配置状态。 */
const KEYWORD_PARAM = 'keyword'
const AMOUNT_STATUS_PARAM = 'amountStatus'
const CONFIGURATION_STATUS_PARAM = 'configurationStatus'

/**
 * 把地址栏查询串解析为已生效条件；参数缺失或取值不可识别时该条件按默认值处理
 * （金额状态回退 `'all'`，互认配置状态回退 `'all'`，关键字回退空串）。
 */
export function parseAmountFilterSearch(search: string): RecognitionAmountUrlFilter {
  let params: URLSearchParams
  try {
    params = new URLSearchParams(search)
  } catch {
    return { ...EMPTY_RECOGNITION_AMOUNT_FILTER }
  }
  const amountStatusText = params.get(AMOUNT_STATUS_PARAM)
  const amountStatus: AmountStatusFilter =
    amountStatusText === 'configured' || amountStatusText === 'unconfigured' ? amountStatusText : 'all'
  const configurationStatusText = params.get(CONFIGURATION_STATUS_PARAM)
  const configurationStatus: AmountConfigurationStatusFilter =
    configurationStatusText === 'enabled' || configurationStatusText === 'disabled' ? configurationStatusText : 'all'
  return { keyword: params.get(KEYWORD_PARAM) ?? '', amountStatus, configurationStatus }
}

/**
 * 在既有查询串上写入已生效条件，返回新的查询串（含前导 `?`，无参数时返回空串）。
 * 默认档与空关键字从查询串里删除；其它参数原样保留，页面只改自己这三个。
 */
export function buildAmountFilterSearch(search: string, filter: RecognitionAmountUrlFilter): string {
  let params: URLSearchParams
  try {
    params = new URLSearchParams(search)
  } catch {
    params = new URLSearchParams()
  }
  if (filter.keyword.length === 0) params.delete(KEYWORD_PARAM)
  else params.set(KEYWORD_PARAM, filter.keyword)
  if (filter.amountStatus === 'all') params.delete(AMOUNT_STATUS_PARAM)
  else params.set(AMOUNT_STATUS_PARAM, filter.amountStatus)
  if (filter.configurationStatus === 'all') params.delete(CONFIGURATION_STATUS_PARAM)
  else params.set(CONFIGURATION_STATUS_PARAM, filter.configurationStatus)
  const text = params.toString()
  return text.length === 0 ? '' : `?${text}`
}

/** 读取当前地址栏上的已生效条件；读不到时返回默认条件（页面按无条件展示）。 */
export function readAmountFilterFromLocation(): RecognitionAmountUrlFilter {
  try {
    return parseAmountFilterSearch(window.location.search)
  } catch {
    return { ...EMPTY_RECOGNITION_AMOUNT_FILTER }
  }
}

/**
 * 把已生效条件按**替换当前历史项**的方式写回地址栏；内容未变化时不触达历史 API，
 * 避免每次渲染都写一次。地址栏不可写时静默降级，条件仍按本地状态生效。
 */
export function syncAmountFilterToLocation(filter: RecognitionAmountUrlFilter): void {
  try {
    const search = buildAmountFilterSearch(window.location.search, filter)
    if (search === window.location.search) return
    const url = `${window.location.pathname}${search}${window.location.hash}`
    window.history.replaceState(window.history.state, '', url)
  } catch {
    // 地址栏不可写（宿主或沙箱限制）时不阻断页面：条件仍按本地状态生效。
  }
}

/**
 * 保存前的可信范围复核输入：页面应用范围时记录的可信范围与现读的可信范围，两侧都是可信范围。
 *
 * 复核次序固定为组织、医院、院区逐层；`layers` 控制本次比较哪些层级——医院管理员页比组织、
 * 医院、院区三层，平台管理员页比院区一层。
 */
export interface RecognitionAmountScopeRecheckInput {
  /** 页面应用范围时记录的可信范围；页面尚未应用范围时为 `null`。 */
  applied: RecognitionAmountScope | null
  /** 现读的可信范围；读取失败或读不到时为 `null`。 */
  trusted: RecognitionAmountScope | null
  /** 本次复核比较的层级；比较次序按组织、医院、院区的固定次序，与入参书写次序无关。 */
  layers: readonly RecognitionAmountScopeLayer[]
}

/** 保存前的可信范围复核结论：通过时 `reason` 为 `null`，失败时是一行可直接展示的中文原因。 */
export interface RecognitionAmountScopeRecheckResult {
  /** 是否可以提交写入；`false` 时调用方零写请求并在行内展示 `reason`。 */
  ok: boolean
  /** 失败原因；单行中文文案，通过时为 `null`。 */
  reason: string | null
}

/** 一次查询涉及的范围值；医院管理员页的组织与医院由服务端注入，对应字段为 `null`。 */
export interface RecognitionAmountScope {
  organizationCode: string | null
  hospitalCode: string | null
  branchCode: string | null
}

/** 可信范围复核比较的层级：组织、医院、院区。 */
export type RecognitionAmountScopeLayer = 'organization' | 'hospital' | 'branch'

/** 复核的固定比较次序与各层字段、中文名；数组次序即复核次序，不随入参书写次序变化。 */
const SCOPE_LAYER_CHECKS: ReadonlyArray<{
  layer: RecognitionAmountScopeLayer
  name: string
  field: keyof RecognitionAmountScope
}> = [
  { layer: 'organization', name: '组织', field: 'organizationCode' },
  { layer: 'hospital', name: '医院', field: 'hospitalCode' },
  { layer: 'branch', name: '院区', field: 'branchCode' },
]

/** 层级取值进入原因串时的呈现：`null`、空串与纯空白归一后都是缺失，写作「缺失」；有值即取值本身。 */
function presentScopeLayerValue(value: string): string {
  return value.length === 0 ? '缺失' : value
}

/**
 * 保存前的可信范围复核：比较页面应用范围时记录的可信范围与现读的可信范围，任一层不一致、
 * 组织医院层现读为空或记录侧缺值即返回失败结论与一行中文原因，调用方据此零写请求。
 *
 * 比较次序固定为组织 → 医院 → 院区：多层同时不一致时只给出最上层的原因，与入参 `layers`
 * 的书写次序无关；比较两侧都取去空白后的值，避免两侧对同一编码的空白写法差异造成误报。
 * 院区层只做一致性比对：可信院区缺失属 S3-D22 档二正常形态，`null`、空串与纯空白归一为缺失，
 * 记录值与现读值同为缺失即通过，一侧缺失另一侧有值即「已变化」；「现读不到」只用于组织、医院
 * 两层的现读空值。医院管理员页比组织、医院、院区三层，平台管理员页提交前只传院区一层。
 */
export function recheckRecognitionAmountScope(
  input: RecognitionAmountScopeRecheckInput,
): RecognitionAmountScopeRecheckResult {
  if (input.applied === null) {
    return { ok: false, reason: '页面尚未应用查询范围，本次不提交写入，请先完成查询。' }
  }
  if (input.trusted === null) {
    return { ok: false, reason: '读取不到可信范围，本次不提交写入，请放弃未保存内容并切换到新可信范围。' }
  }
  const applied = input.applied
  const trusted = input.trusted
  for (const check of SCOPE_LAYER_CHECKS) {
    if (!input.layers.includes(check.layer)) continue
    const appliedValue = applied[check.field]?.trim() ?? ''
    const trustedValue = trusted[check.field]?.trim() ?? ''
    if (check.layer !== 'branch') {
      // 组织、医院两层维持既有口径：现读空值即读不到（拦），记录侧空值即尚未应用（拦）；
      // 院区层不进这两支——缺失按下面的一致性比对参与，两侧同为缺失即通过。
      if (trustedValue.length === 0) {
        return { ok: false, reason: `读取不到可信${check.name}，本次不提交写入，请放弃未保存内容并切换到新可信范围。` }
      }
      if (appliedValue.length === 0) {
        return { ok: false, reason: `页面尚未应用${check.name}范围，本次不提交写入，请先完成查询。` }
      }
    }
    if (appliedValue !== trustedValue) {
      // 院区层的缺失一侧从这里给出「已变化」，缺失写作「缺失」；两侧都非空时文案与既有逐层比对一致。
      return {
        ok: false,
        reason: `可信${check.name}已变化：应用时记录为「${presentScopeLayerValue(appliedValue)}」，现读为「${presentScopeLayerValue(trustedValue)}」，本次不提交写入，请放弃未保存内容并切换到新可信范围。`,
      }
    }
  }
  return { ok: true, reason: null }
}

/**
 * 读取平台管理员页的金额列表。
 * 组织、医院、院区**显式下发**（生成类型把三者在内的必填字段都生成为可选，见文件头第 4a 条）；
 * 标准项目编码按值下发，缺省不下发；关键字去空白后非空才下发，空白与缺省都不携带该字段。
 */
export async function queryRecognitionAmountRows(
  client: MedicalRecognitionClient,
  request: RecognitionAmountListQuery,
): Promise<RecognitionAmountRow[]> {
  const body: RecognitionAmountListQueryRequest = {
    organizationCode: request.organizationCode,
    hospitalCode: request.hospitalCode,
    branchCode: request.branchCode,
  }
  if (request.standardProjectCode !== undefined) body.standardProjectCode = request.standardProjectCode
  const keyword = request.keyword?.trim()
  if (keyword !== undefined && keyword.length > 0) body.keyword = keyword
  const values = await client.api.medicalRecognitionReportQuery.queryRecognitionAmountList.post(body)
  return toRecognitionAmountRows(values ?? [])
}

/** 读取医院管理员页的本院区金额列表；请求只下发院区、标准项目编码与非空关键字。 */
export async function queryBranchRecognitionAmountRows(
  client: MedicalRecognitionClient,
  request: BranchAmountListQuery,
): Promise<RecognitionAmountRow[]> {
  const body: BranchRecognitionAmountListQueryRequest = { branchCode: request.branchCode }
  if (request.standardProjectCode !== undefined) body.standardProjectCode = request.standardProjectCode
  const keyword = request.keyword?.trim()
  if (keyword !== undefined && keyword.length > 0) body.keyword = keyword
  const values = await client.api.medicalRecognitionReportQuery.queryBranchRecognitionAmountList.post(body)
  return toRecognitionAmountRows(values ?? [])
}

/** 平台管理员页保存金额；组织、医院、院区、标准项目编码与金额全部显式提交，不提交已变更属性集。 */
export async function saveOrganizationHospitalBranchRecognitionAmount(
  client: MedicalRecognitionClient,
  payload: RecognitionAmountSavePayload,
): Promise<void> {
  const body: SaveOrganizationHospitalBranchRecognitionAmountRequest = {
    organizationCode: payload.organizationCode,
    hospitalCode: payload.hospitalCode,
    branchCode: payload.branchCode,
    standardProjectCode: payload.standardProjectCode,
    currentAmount: { value: payload.currentAmount, getValue: () => payload.currentAmount },
  }
  await client.api.medicalRecognitionReport.saveOrganizationHospitalBranchRecognitionAmount.post(body)
}

/** 医院管理员页保存金额；只提交院区、标准项目编码与金额，组织与医院由服务端从可信上下文注入。 */
export async function saveBranchRecognitionAmount(
  client: MedicalRecognitionClient,
  payload: BranchAmountSavePayload,
): Promise<void> {
  const body: SaveBranchRecognitionAmountRequest = {
    branchCode: payload.branchCode,
    standardProjectCode: payload.standardProjectCode,
    currentAmount: { value: payload.currentAmount, getValue: () => payload.currentAmount },
  }
  await client.api.medicalRecognitionReport.saveBranchRecognitionAmount.post(body)
}
