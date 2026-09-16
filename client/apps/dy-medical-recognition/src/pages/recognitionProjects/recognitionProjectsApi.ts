import type {
  EffectiveMedicalStandardCatalogReadModel,
  MedicalRecognitionClient,
  RecognitionProjectConfigurationListQueryRequest,
  RecognitionProjectConfigurationReadModel,
} from '@dy/api-client-medical-recognition'

/**
 * 生成的契约类型经本模块转出，页面与测试只从适配层取类型，不依赖生成包深层路径或
 * 生成包的传递依赖（`@microsoft/kiota-abstractions` 不是本子应用的直接依赖）。
 */
export type {
  EffectiveMedicalStandardCatalogReadModel,
  MedicalRecognitionClient,
  RecognitionProjectConfigurationListQueryRequest,
  RecognitionProjectConfigurationReadModel,
}

/**
 * 互认项目配置的适配层：把查询与写入的 Request/ReadModel 契约与页面稳定视图模型隔开。
 *
 * 边界（遵循 Frontend API Client 第 2、5 节）：
 * 1. 本模块不创建 Client、不读取 token、不解析平台错误响应、不新增错误拦截器；
 * 2. 页面只依赖本模块导出的稳定类型与函数，不接触 Kiota 生成模型；
 * 3. 请求与响应的**契约类型直接引用生成类型**（`RecognitionProjectConfigurationListQueryRequest` /
 *    `RecognitionProjectConfigurationReadModel` / `EffectiveMedicalStandardCatalogReadModel` 等），
 *    不存在第二套契约来源；本模块只保留页面专属的视图模型与筛选模型。
 * 4. 生成契约的两处固有形态由本层吸收（生成物不得手改）：
 *    a) `organizationCode` 在后端是必填（`[Required]`），但 Kiota 未把 OpenAPI 的 `required`
 *       映射为 TS 必填，生成成 `string | null` 可选。因此查询**显式赋值**组织编码，
 *       不得依赖「省略即由服务端补」；创建请求不含该字段，组织由服务端可信上下文注入。
 *    b) `itemType` / `configurationStatus` 在后端是数值枚举，契约按数值交互：服务端用 SourceGen 描述器
 *       在 OpenAPI 里声明 `enum` / `x-enumNames` / `x-enumDescriptions`，生成端据此把这两个属性映射为
 *       `number | null` 并用 `writeNumberValue` 写出数值。缺 `enum` 时生成端会退化为空对象类型、
 *       线上形状写成 `{}`，这类退化只在契约侧修复，本层不猜测兼容。
 *       本层只保留最小数值映射：只接受已确认取值，未知一律映射为 `null`（安全展示并阻断依赖已知状态的写操作），
 *       不让页面出现第二套枚举判定。
 *    c) 状态筛选仍**不下推**服务端：这是业务口径而非契约限制——服务端筛选会缩小读取范围，
 *       破坏新增排除集合的完整性（新增选择器必须隐藏当前组织已配置的全部项目）。
 *    d) 枚举中文由服务端交付（与 `Dy.LisCenter` 同口径）：列表与只读资料的展示文本取只读模型的
 *       `itemTypeText` / `configurationStatusText`，下拉选项取 `useEnumMetadata` 的枚举元数据接口。
 *       本模块的本地文案表只是"契约字段缺失或元数据接口不可用"时的兜底出口，页面不得再自拼中文。
 */

/** 项目类型取值，与后端 `MedicalItemType` 数值一致（0 检验、1 检查）；契约按数值交互、生成端不产出 TS 枚举，故在此声明取值集合。 */
export const MEDICAL_ITEM_TYPES = [0, 1] as const
export type MedicalItemTypeValue = (typeof MEDICAL_ITEM_TYPES)[number]

/**
 * 项目类型兜底文案；服务端只读模型已交付 `itemTypeText`（枚举中文的唯一来源），
 * 本表只在该契约字段缺失时兜底展示，页面不得直接用它覆盖服务端文案。
 */
export const MEDICAL_ITEM_TYPE_TEXTS: Record<MedicalItemTypeValue, string> = { 0: '检验', 1: '检查' }

/** 项目类型兜底文案出口：只接受已确认取值，未知与缺失一律显示「未知类型」。 */
export function medicalItemTypeText(value: MedicalItemTypeValue | null): string {
  return value === null ? '未知类型' : MEDICAL_ITEM_TYPE_TEXTS[value]
}

/** 配置状态取值，与后端 `ConfigurationStatus` 数值一致（1 启用、2 停用）；契约按数值交互、生成端不产出 TS 枚举，故在此声明取值集合。 */
export const CONFIGURATION_STATUSES = [1, 2] as const
export type ConfigurationStatusValue = (typeof CONFIGURATION_STATUSES)[number]

/**
 * 已确认配置状态的具名取值：调用方按名引用「启用 / 停用」，
 * 不再按取值集合的下标读取（下标语义只在本声明处体现一次）。
 */
export const ENABLED_CONFIGURATION_STATUS: ConfigurationStatusValue = CONFIGURATION_STATUSES[0]
export const DISABLED_CONFIGURATION_STATUS: ConfigurationStatusValue = CONFIGURATION_STATUSES[1]

/**
 * 配置状态兜底文案：键集合与 `CONFIGURATION_STATUSES` 严格一致（TS 的映射类型会让两者缺一即编译失败）。
 * 服务端只读模型已交付 `configurationStatusText`，本表只在该契约字段缺失时兜底，不再作为展示主来源。
 */
export const CONFIGURATION_STATUS_TEXTS: Record<ConfigurationStatusValue, string> = { 1: '启用', 2: '停用' }

/** 未知状态（`null`）的安全展示文案；不默认成任一已确认状态。 */
export const UNKNOWN_CONFIGURATION_STATUS_TEXT = '未知'

/** 配置状态兜底文案；未知状态返回「未知」，不伪造为启用或停用。 */
export function configurationStatusText(value: ConfigurationStatusValue | null): string {
  return value === null ? UNKNOWN_CONFIGURATION_STATUS_TEXT : CONFIGURATION_STATUS_TEXTS[value]
}

/**
 * 启停动作兜底文案：目标为启用时是「启用」、目标为停用时是「停用」。
 * 页面在枚举元数据可用时改取服务端标签（见 `useEnumMetadata`），本函数只作兜底，保证动作文案与状态文案同源。
 */
export function configurationStatusActionText(nextEnabled: boolean): string {
  return CONFIGURATION_STATUS_TEXTS[nextEnabled ? ENABLED_CONFIGURATION_STATUS : DISABLED_CONFIGURATION_STATUS]
}

/**
 * 枚举元数据选项的最小形状；与 `useEnumMetadata` 的 `EnumMetadataOption` 结构一致。
 * 本模块只声明结构而不从 hooks 反向导入，保持"页面 → 适配层 → 生成契约"的依赖方向。
 */
export interface EnumMetadataOptionLike {
  value: number
  label: string
  name: string
}

/**
 * 配置状态筛选的兜底选项：枚举元数据接口不可用或返回空集合时仍可筛选。
 * 取值集合与中文都取自本模块的枚举出口，页面不再自己持有这份业务事实。
 */
export const CONFIGURATION_STATUS_METADATA_FALLBACK: readonly (EnumMetadataOptionLike & { value: ConfigurationStatusValue })[] = [
  { value: ENABLED_CONFIGURATION_STATUS, name: 'Enabled', label: configurationStatusText(ENABLED_CONFIGURATION_STATUS) },
  { value: DISABLED_CONFIGURATION_STATUS, name: 'Disabled', label: configurationStatusText(DISABLED_CONFIGURATION_STATUS) },
]

/**
 * 把枚举元数据选项收窄为配置状态筛选项：只保留已确认取值，未登记取值不进入筛选器。
 * 后端 `ConfigurationStatus` 由布尔派生、取值集合封闭，未登记取值不可达；此处保留防御是为了让
 * 筛选器不出现语义无法判定的项（契约若新增取值，需要先在本模块声明再放开）。
 */
export function toConfigurationStatusFilterOptions(
  options: readonly Pick<EnumMetadataOptionLike, 'value' | 'label'>[],
): { value: ConfigurationStatusValue; label: string }[] {
  return options.flatMap((option) =>
    option.value === ENABLED_CONFIGURATION_STATUS || option.value === DISABLED_CONFIGURATION_STATUS
      ? [{ value: option.value, label: option.label }]
      : [],
  )
}

/** Tag 色板也由状态推导，避免组件里再写一遍数值判定。 */
export function configurationStatusTagColor(value: ConfigurationStatusValue | null): 'success' | 'warning' | undefined {
  if (value === null) return 'warning'
  return value === ENABLED_CONFIGURATION_STATUS ? 'success' : undefined
}

/**
 * 行内可识别的业务名称；未知时退回编码，再退回占位文案。
 * 表格、弹窗与操作按钮的可访问名称共用同一出口，不各自重复 `??` 链。
 */
export function rowDisplayName(row: RecognitionProjectConfigurationRow): string {
  return row.standardItemName ?? row.standardProjectCode ?? '未知项目'
}

/**
 * 只读资料里的编码、分类与分组文案；缺失时统一在这里给出兜底表述。
 * 表格用 `—` 表示缺失，弹窗需要可读文案，两处措辞集中在适配层，组件不再各写一份中文。
 */
export function rowScopeTexts(row: RecognitionProjectConfigurationRow): {
  standardProjectCode: string
  categoryName: string
  groupName: string
} {
  return {
    standardProjectCode: row.standardProjectCode ?? '未知编码',
    categoryName: row.categoryName ?? '未知分类',
    groupName: row.groupName ?? '未知分组',
  }
}

/** 互认配置行视图模型；只承载查询契约交付的字段。 */
export interface RecognitionProjectConfigurationRow {
  configurationId: string | null
  standardProjectCode: string | null
  standardItemName: string | null
  itemType: MedicalItemTypeValue | null
  /** 项目类型中文；服务端枚举声明的文案优先，字段缺失时由本地兜底表补。 */
  itemTypeText: string
  categoryName: string | null
  groupName: string | null
  recognitionDurationDays: number | null
  configurationStatus: ConfigurationStatusValue | null
  /** 配置状态中文；服务端枚举声明的文案优先，字段缺失时由本地兜底表补。 */
  configurationStatusText: string
  /** `null` 表示标准目录三层全部启用；非 `null` 时是目录停用原因，不表达配置自身停用。 */
  unavailableReason: string | null
}

/** 列表展示筛选；只作用于本地已加载数据，不下推为读取范围。 */
export interface RecognitionProjectFilter {
  code: string
  status: 'all' | ConfigurationStatusValue
}

export const EMPTY_RECOGNITION_PROJECT_FILTER: RecognitionProjectFilter = { code: '', status: 'all' }

/**
 * 查询请求载荷：组织编码必填，编码筛选缺省时不下发。
 * 字段集合是生成类型 `RecognitionProjectConfigurationListQueryRequest` 的**窄化子集**（生成类型把
 * `organizationCode` 生成为可选，这里以必填形式冻结本地构造）。**不含 `configurationStatus`**：
 * 这是业务口径而非契约限制——下推会缩小读取范围，破坏新增排除集合的完整性（见文件头第 4c 条），
 * 配置状态只在本地筛选。契约面已可下发该枚举（生成端为 `number | null`、走 `writeNumberValue`）。
 */
export interface ConfigurationListQuery {
  organizationCode: string
  standardProjectCode?: string
}

/** 创建请求载荷：只含标准项目编码与正整数天数，不含组织编码与内部标准项目 ID。 */
export interface CreateConfigurationPayload {
  standardProjectCode: string
  recognitionDurationDays: number
}

export interface DurationUpdatePayload {
  id: string
  recognitionDurationDays: number
}

/** 启停载荷：只表达目标配置与目标状态，不改动其它字段。 */
export interface EnabledUpdatePayload {
  id: string
  enabled: boolean
}

/** 可新增的标准项目；只来自当前有效标准目录的最底层项目。 */
export interface SelectableStandardItem {
  code: string
  name: string
  itemType: MedicalItemTypeValue | null
  categoryName: string
  groupName: string
}

export interface SelectableOptionGroup {
  label: string
  options: { value: string; label: string }[]
}

/**
 * 可互认时间上界：与后端 `[Range(1, int.MaxValue)]` 同口径（C# `int` 上界）。
 * 超出该值的输入必须在本地拦截，否则会在写请求上白跑一次服务端校验。
 */
export const MAX_RECOGNITION_DURATION_DAYS = 2147483647

/** 可互认时间解析：只接受 1 与 `MAX_RECOGNITION_DURATION_DAYS` 之间的整数字符串，其余一律拒绝。 */
export function parseRecognitionDurationDays(input: string): number | null {
  const text = input.trim()
  if (!/^\d+$/.test(text)) return null
  const days = Number(text)
  return Number.isSafeInteger(days) && days >= 1 && days <= MAX_RECOGNITION_DURATION_DAYS ? days : null
}

/**
 * 构造新增请求；编码空白或天数非法时返回 `null`，调用方必须据此**零写请求**。
 * 组织编码由服务端可信组织上下文补入，这里不提交。
 */
export function buildCreateConfigurationRequest(standardProjectCode: string, durationInput: string): CreateConfigurationPayload | null {
  const code = standardProjectCode.trim()
  const days = parseRecognitionDurationDays(durationInput)
  if (code.length === 0 || days === null) return null
  return { standardProjectCode: code, recognitionDurationDays: days }
}

/** 构造修改请求；只提交目标标识与可互认时间，同值也照常提交（不做同值短路）。 */
export function buildDurationUpdatePayload(configurationId: string | null, durationInput: string): DurationUpdatePayload | null {
  const days = parseRecognitionDurationDays(durationInput)
  if (configurationId === null || configurationId.length === 0 || days === null) return null
  return { id: configurationId, recognitionDurationDays: days }
}

/**
 * 构造查询请求；组织编码缺失或空白时返回 `null`，禁止退化为全局查询或硬编码默认组织。
 * 编码筛选缺省时不下发该字段，使"未筛选"和"筛选为空"可区分。
 * `filter.status` 只用于本地展示筛选，不进入请求（状态筛只作用于本地已加载数据，不下推服务端；业务口径见文件头第 4c 条）。
 */
export function buildConfigurationListQuery(
  organizationCode: string,
  filter: RecognitionProjectFilter = EMPTY_RECOGNITION_PROJECT_FILTER,
): ConfigurationListQuery | null {
  const code = organizationCode.trim()
  if (code.length === 0) return null
  const projectCode = filter.code.trim()
  return {
    organizationCode: code,
    ...(projectCode.length === 0 ? {} : { standardProjectCode: projectCode }),
  }
}

/** 未知枚举一律映射为 `null`（安全展示并阻断依赖已知状态的写操作），不默认成已知值。 */
function toMedicalItemType(value: number | null | undefined): MedicalItemTypeValue | null {
  return value === 0 || value === 1 ? value : null
}

function toConfigurationStatus(value: number | null | undefined): ConfigurationStatusValue | null {
  return value === 1 || value === 2 ? value : null
}

/**
 * 契约标识归一：只在取值为**非空字符串**时把它当作标识，其余一律按「标识未知」处理为 `null`。
 *
 * 后端 `Guid` 被生成成 `@microsoft/kiota-abstractions` 的抽象别名 `type Guid = string`，
 * 生成端反序列化走 `parseGuidString`（内部用 GUID 正则校验），交付的就是普通字符串，
 * 因此「字符串」是当前契约的唯一合法形态。这里不收 `String(value)` 的兜底：
 * 对象、数组、数字等非字符串取值会被 `String` 转成 `[object Object]`/`1` 之类的伪标识，
 * 伪标识会经表格行标识与写请求标识两条路径外溢（例如用 `[object Object]` 作为启停目标）。
 */
function toIdentifier(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null
}

/**
 * 读模型 → 行视图模型。不引入创建/修改人与时间，也不引入已删除的
 * `IsStandardCatalogValid` / `IsAvailableForNewMatch` 派生字段。
 * 枚举中文优先取服务端交付的 `itemTypeText` / `configurationStatusText`；契约字段为 `null` 或缺失时
 * 用本地兜底表补齐（空字符串视为有效文案原样展示，服务端两条解析路径都不产生空串），
 * 未知取值仍按「未知」展示，不默认成已知值。
 */
export function toConfigurationRows(values: readonly RecognitionProjectConfigurationReadModel[]): RecognitionProjectConfigurationRow[] {
  return values.map((value) => {
    const itemType = toMedicalItemType(value.itemType)
    const configurationStatus = toConfigurationStatus(value.configurationStatus)
    return {
      configurationId: toIdentifier(value.configurationId),
      standardProjectCode: value.standardProjectCode ?? null,
      standardItemName: value.standardItemName ?? null,
      itemType,
      itemTypeText: value.itemTypeText ?? medicalItemTypeText(itemType),
      categoryName: value.categoryName ?? null,
      groupName: value.groupName ?? null,
      recognitionDurationDays: value.recognitionDurationDays ?? null,
      configurationStatus,
      configurationStatusText: value.configurationStatusText ?? configurationStatusText(configurationStatus),
      unavailableReason: value.unavailableReason ?? null,
    }
  })
}

/** 已配置的标准项目编码集合；**包含停用配置**，用于新增选择器的排除。 */
export function configuredStandardProjectCodes(rows: readonly RecognitionProjectConfigurationRow[]): Set<string> {
  const codes = new Set<string>()
  for (const row of rows) {
    if (row.standardProjectCode !== null && row.standardProjectCode.length > 0) codes.add(row.standardProjectCode)
  }
  return codes
}

/**
 * 把契约交付的有效标准目录**摊平为页面视图模型**：只产出最底层标准项目，
 * 分类与分组不作为可选项，因此不存在“可选到非最底层节点”的路径。
 *
 * 摊平放在适配层，页面状态因此不再持有生成读模型（`EffectiveMedicalStandardCatalogReadModel`）：
 * 生成契约的形状变化只在本层吸收，页面只按 `SelectableStandardItem` 渲染。
 */
export function toSelectableStandardItems(catalog: EffectiveMedicalStandardCatalogReadModel | null): SelectableStandardItem[] {
  const items: SelectableStandardItem[] = []
  for (const type of catalog?.itemTypes ?? []) {
    const itemType = toMedicalItemType(type.itemType)
    for (const category of type.categories ?? []) {
      const categoryName = category.categoryName ?? ''
      for (const group of category.groups ?? []) {
        const groupName = group.groupName ?? ''
        for (const item of group.items ?? []) {
          if (!item.code) continue
          items.push({ code: item.code, name: item.name ?? '', itemType, categoryName, groupName })
        }
      }
    }
  }
  return items
}

/**
 * 从摊平结果中剔除**当前组织已配置**的标准项目（含停用配置）。
 *
 * 与摊平分开：摊平结果由目录读取交付、进页面状态；剔除依赖配置读取交付的编码集合，
 * 两者在页面按行集变化重新组合，因此新增选择器始终隐藏全部已配置项目。
 */
export function excludeConfiguredStandardItems(
  items: readonly SelectableStandardItem[],
  configuredCodes: ReadonlySet<string>,
): SelectableStandardItem[] {
  return items.filter((item) => !configuredCodes.has(item.code))
}

/** 新增选择器的分组选项：分类 / 分组，空分组不下发。 */
export function toSelectableOptionGroups(items: readonly SelectableStandardItem[]): SelectableOptionGroup[] {
  const groups = new Map<string, { value: string; label: string }[]>()
  for (const item of items) {
    const key = `${item.categoryName} / ${item.groupName}`
    const options = groups.get(key)
    const option = { value: item.code, label: `${item.name}（${item.code}）` }
    if (options) options.push(option)
    else groups.set(key, [option])
  }
  return [...groups].map(([label, options]) => ({ label, options }))
}

/**
 * 本地展示筛选。编码按不区分大小写的包含匹配，与服务端查询条件的"字面包含（区分大小写，`%`/`_` 按普通字符转义）"语义不同：
 * 服务端筛选会缩小读取范围，从而破坏新增排除集合的完整性，因此筛选只在本地生效、不下推为读取范围。
 */
export function filterConfigurationRows(
  rows: readonly RecognitionProjectConfigurationRow[],
  filter: RecognitionProjectFilter,
): RecognitionProjectConfigurationRow[] {
  const keyword = filter.code.trim().toLocaleLowerCase()
  return rows.filter((row) => {
    if (keyword.length > 0 && !(row.standardProjectCode ?? '').toLocaleLowerCase().includes(keyword)) return false
    if (filter.status !== 'all' && row.configurationStatus !== filter.status) return false
    return true
  })
}

/**
 * 读取指定组织的互认配置列表。
 * 组织编码**显式下发**（生成类型把它生成为可选，见文件头第 4a 条）；编码筛选按值下发，缺省不下发。
 * 配置状态**不下发**：这是业务口径而非契约限制——下推会缩小读取范围，破坏新增排除集合的完整性
 * （见文件头第 4c 条），页面按设计在本地筛选状态。
 */
export async function queryRecognitionProjectConfigurations(
  client: MedicalRecognitionClient,
  request: ConfigurationListQuery,
): Promise<RecognitionProjectConfigurationRow[]> {
  const body: RecognitionProjectConfigurationListQueryRequest = { organizationCode: request.organizationCode }
  if (request.standardProjectCode !== undefined) body.standardProjectCode = request.standardProjectCode
  const values = await client.api.medicalRecognitionReportQuery.queryRecognitionProjectConfigurationList.post(body)
  return toConfigurationRows(values ?? [])
}

/** 读取当前有效标准目录，用于新增选择器；目录是平台级资料，不随组织变化。 */
export async function querySelectableStandardItems(
  client: MedicalRecognitionClient,
): Promise<EffectiveMedicalStandardCatalogReadModel | null> {
  const catalog = await client.api.medicalRecognitionReportQuery.queryEffectiveMedicalStandardCatalog.post({})
  return catalog ?? null
}

/** 新增互认配置；载荷不含组织编码，也不含内部标准项目 ID。 */
export async function createRecognitionProjectConfiguration(
  client: MedicalRecognitionClient,
  request: CreateConfigurationPayload,
): Promise<void> {
  await client.api.medicalRecognitionReport.createMutualRecognitionItem.post({
    standardProjectCode: request.standardProjectCode,
    recognitionDurationDays: request.recognitionDurationDays,
  })
}

/** 修改可互认时间；停用配置同样允许调用，同值也照常提交。 */
export async function updateRecognitionDuration(client: MedicalRecognitionClient, request: DurationUpdatePayload): Promise<void> {
  // 生成类型把后端 `Guid` 生成为抽象类型，页面持有的标识来自查询响应的字符串；HTTP 上同为 UUID 字符串。
  await client.api.medicalRecognitionReport.updateMutualRecognitionItemConfiguration.post({
    id: request.id,
    recognitionDurationDays: request.recognitionDurationDays,
  })
}

/** 独立启用或停用配置；载荷只表达目标配置与目标状态（目标状态由所选方法表达）。 */
export async function setRecognitionProjectConfigurationEnabled(
  client: MedicalRecognitionClient,
  request: EnabledUpdatePayload,
): Promise<void> {
  const body = { id: request.id }
  if (request.enabled) await client.api.medicalRecognitionReport.enableMutualRecognitionItem.post(body)
  else await client.api.medicalRecognitionReport.disableMutualRecognitionItem.post(body)
}
