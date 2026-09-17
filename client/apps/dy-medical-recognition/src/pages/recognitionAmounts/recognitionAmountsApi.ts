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
 * 平台管理员入口（组织、医院、院区三级）的查询条件；三项范围值都是请求参数，与服务端契约一致。
 * 标准项目编码可选，缺省表示不按编码过滤。
 */
export interface RecognitionAmountListQuery {
  organizationCode: string
  hospitalCode: string
  branchCode: string
  standardProjectCode?: string
}

/** 医院管理员入口的查询条件；组织与医院取可信上下文，请求**不含**这两项。 */
export interface BranchAmountListQuery {
  branchCode: string
  standardProjectCode?: string
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
}): RecognitionAmountListQuery | null {
  const organizationCode = input.organizationCode.trim()
  const hospitalCode = input.hospitalCode.trim()
  const branchCode = input.branchCode.trim()
  if (organizationCode.length === 0 || hospitalCode.length === 0 || branchCode.length === 0) return null
  const standardProjectCode = input.standardProjectCode?.trim() ?? ''
  return {
    organizationCode,
    hospitalCode,
    branchCode,
    ...(standardProjectCode.length === 0 ? {} : { standardProjectCode }),
  }
}

/**
 * 构造医院管理员页查询请求；院区缺失或空白时返回 `null`，不用可信上下文回填、不发空院区请求。
 * 请求**不含组织与医院**：两者由服务端从可信上下文注入，前端提交即越权覆盖。
 */
export function buildBranchAmountListQuery(input: {
  branchCode: string
  standardProjectCode?: string
}): BranchAmountListQuery | null {
  const branchCode = input.branchCode.trim()
  if (branchCode.length === 0) return null
  const standardProjectCode = input.standardProjectCode?.trim() ?? ''
  return {
    branchCode,
    ...(standardProjectCode.length === 0 ? {} : { standardProjectCode }),
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

/**
 * 读取平台管理员页的金额列表。
 * 组织、医院、院区**显式下发**（生成类型把三者在内的必填字段都生成为可选，见文件头第 4a 条）；
 * 标准项目编码按值下发，缺省不下发。
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
  const values = await client.api.medicalRecognitionReportQuery.queryRecognitionAmountList.post(body)
  return toRecognitionAmountRows(values ?? [])
}

/** 读取医院管理员页的本院区金额列表；请求只下发院区与标准项目编码。 */
export async function queryBranchRecognitionAmountRows(
  client: MedicalRecognitionClient,
  request: BranchAmountListQuery,
): Promise<RecognitionAmountRow[]> {
  const body: BranchRecognitionAmountListQueryRequest = { branchCode: request.branchCode }
  if (request.standardProjectCode !== undefined) body.standardProjectCode = request.standardProjectCode
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
