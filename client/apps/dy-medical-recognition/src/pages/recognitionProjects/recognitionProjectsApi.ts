import type {
  EffectiveMedicalStandardCatalogReadModel,
  MedicalRecognitionClient,
  RecognitionProjectConfigurationListQueryRequest,
  RecognitionProjectConfigurationReadModel,
  SelectableStandardItemListQueryRequest,
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
  EffectiveMedicalStandardCatalogReadModel,
  MedicalRecognitionClient,
  RecognitionProjectConfigurationListQueryRequest,
  RecognitionProjectConfigurationReadModel,
  SelectableStandardItemListQueryRequest,
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

/** 项目类型的取值域判定与兜底文案已上移到跨页面共享模块 `src/shared/medicalItemType.ts`（阶段 1、阶段 3 使用同一份事实）。 */
export { isMedicalItemTypeValue, medicalItemTypeText }
export type { MedicalItemTypeValue }

/** 配置状态的取值域判定与兜底文案已上移到跨页面共享模块 `src/shared/configurationStatus.ts`（阶段 3 使用同一份事实）。 */
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
 *
 * 入参只要求承载名称与编码：配置行与列视图行都满足这一结构子集，因此两种行都能直接传入。
 */
export interface RecognitionProjectRowIdentityFields {
  standardProjectCode: string | null
  standardItemName: string | null
}

export function rowDisplayName(row: RecognitionProjectRowIdentityFields): string {
  return row.standardItemName ?? row.standardProjectCode ?? '未知项目'
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

/**
 * 列表筛选条件。`keyword` 是**服务端查询条件**（S2-D29）：按字面包含同时匹配标准项目编码、名称、
 * 所属分类名称与所属分组名称；`status` 仍只在本地裁剪（离散二选一、切换即生效，下推会让每次切换都走一次网络）。
 */
export interface RecognitionProjectFilter {
  keyword: string
  status: 'all' | ConfigurationStatusValue
}

export const EMPTY_RECOGNITION_PROJECT_FILTER: RecognitionProjectFilter = { keyword: '', status: 'all' }

/**
 * 查询请求载荷：组织编码必填，关键字缺省时不下发。
 * 字段集合是生成类型 `RecognitionProjectConfigurationListQueryRequest` 的**窄化子集**（生成类型把
 * `organizationCode` 生成为可选，这里以必填形式冻结本地构造）。**不含 `configurationStatus`**：
 * 配置状态只在本地裁剪（S2-D24）。契约面已可下发该枚举（生成端为 `number | null`、走 `writeNumberValue`）。
 */
export interface ConfigurationListQuery {
  organizationCode: string
  keyword?: string
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
  /** 服务端交付的项目类型中文；契约字段缺失时为 `null`，展示兜底由调用方决定。 */
  itemTypeText: string | null
  categoryName: string
  groupName: string
}

/**
 * 批量创建请求载荷：只含标准项目编码集合与批次上统一的一份可互认时间。
 * 不含组织编码（由服务端可信组织上下文注入）、不含内部标准项目 ID、也不含启用状态（创建后即为启用）。
 */
export interface BatchCreateConfigurationPayload {
  standardProjectCodes: string[]
  recognitionDurationDays: number
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
 * 构造批量创建请求；编码集合为空或天数非法时返回 `null`，调用方必须据此**零写请求**。
 * 组织编码由服务端可信组织上下文补入，这里不提交；单条与批量走同一条形状，不存在逐元素天数。
 */
export function buildBatchCreateConfigurationRequest(
  standardProjectCodes: ReadonlySet<string>,
  durationInput: string,
): BatchCreateConfigurationPayload | null {
  const codes = [...standardProjectCodes]
  const days = parseRecognitionDurationDays(durationInput)
  if (codes.length === 0 || days === null) return null
  return { standardProjectCodes: codes, recognitionDurationDays: days }
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
  const keyword = filter.keyword.trim()
  return {
    organizationCode: code,
    ...(keyword.length === 0 ? {} : { keyword }),
  }
}

/** 未知枚举一律映射为 `null`（安全展示并阻断依赖已知状态的写操作），不默认成已知值；判定走共享取值域守卫。 */
function toMedicalItemType(value: number | null | undefined): MedicalItemTypeValue | null {
  return isMedicalItemTypeValue(value) ? value : null
}

function toConfigurationStatus(value: number | null | undefined): ConfigurationStatusValue | null {
  return isConfigurationStatusValue(value) ? value : null
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
    // 类型中文只承载契约交付的文本：这里补本地兜底会让类型列优先取到兜底文案，服务端文案永远显示不出来。
    const itemTypeText = type.itemTypeText ?? null
    for (const category of type.categories ?? []) {
      const categoryName = category.categoryName ?? ''
      for (const group of category.groups ?? []) {
        const groupName = group.groupName ?? ''
        for (const item of group.items ?? []) {
          if (!item.code) continue
          items.push({ code: item.code, name: item.name ?? '', itemType, itemTypeText, categoryName, groupName })
        }
      }
    }
  }
  return items
}

/**
 * 本地展示筛选的最小字段集：配置行与列视图行都满足。
 * 两种行共用同一条判定，页面列表与列视图不会出现两套筛选语义。
 */
interface FilterableConfigurationRow {
  standardProjectCode: string | null
  configurationStatus: ConfigurationStatusValue | null
}

/**
 * 本地展示筛选的判定体。编码按不区分大小写的包含匹配，与服务端查询条件的"字面包含（区分大小写，`%`/`_` 按普通字符转义）"语义不同：
 * 服务端筛选会缩小读取范围，从而破坏新增排除集合的完整性，因此筛选只在本地生效、不下推为读取范围。
 *
 * 列视图裁剪是它唯一的调用方：页面展示的行集来自列视图，配置行的筛选不再单独暴露出口，
 * 因此页面上只有一条筛选路径，不存在两处语义漂移的可能。
 */
/**
 * 本地展示筛选的判定体：**只按配置状态裁剪**。
 *
 * 关键字不在这里判定：它已作为查询条件下推服务端（S2-D29），服务端返回的结果就是"任一列命中关键字"的集合；
 * 若本地再按编码判一次，按名称/分类/分组命中的行会被错误裁掉。
 */
function matchesRecognitionProjectFilter(row: FilterableConfigurationRow, filter: RecognitionProjectFilter): boolean {
  if (filter.status !== 'all' && row.configurationStatus !== filter.status) return false
  return true
}

/** 可互认时间输入提示语：与解析上界同源，弹窗与行内编辑器共用同一份文案。 */
export const RECOGNITION_DURATION_RANGE_TEXT = `可互认时间为 1 到 ${MAX_RECOGNITION_DURATION_DAYS} 之间的整数天数`

/**
 * 列视图中的项目行：由当前组织的配置行与当前有效标准目录合并而成。
 */
export interface RecognitionProjectColumnRow {
  /** 行标识；在本次行集内唯一，只用于渲染定位与行内操作目标，不参与业务判定也不作为字段提交。 */
  rowIdentity: string
  standardProjectCode: string | null
  standardItemName: string | null
  /** 该行在当前组织是否已建立互认配置；未配置行没有配置标识、可互认时间与配置状态。 */
  configured: boolean
  configurationId: string | null
  recognitionDurationDays: number | null
  configurationStatus: ConfigurationStatusValue | null
  /** 配置状态中文；未配置行取 `null`。 */
  configurationStatusText: string | null
  /** 目录停用原因；`null` 表示标准目录三层全部启用。 */
  unavailableReason: string | null
}

export interface RecognitionProjectColumnGroup { groupName: string; rows: RecognitionProjectColumnRow[] }
export interface RecognitionProjectColumnCategory { categoryName: string; groups: RecognitionProjectColumnGroup[] }
export interface RecognitionProjectColumnItemType {
  itemType: MedicalItemTypeValue | null
  /** 类型中文：优先取契约交付的文本，取不到时用本地兜底。 */
  itemTypeText: string
  categories: RecognitionProjectColumnCategory[]
}
export type RecognitionProjectColumnView = RecognitionProjectColumnItemType[]

/** 项目行的状态徽标：文案、颜色与目录停用原因。 */
export interface RecognitionProjectRowBadge {
  text: string
  color: 'success' | 'warning' | 'error' | 'default' | undefined
  /** 目录停用原因；仅目录停用时非 `null`。 */
  reason: string | null
}

/** 类型节点的中间结构：文案候选与下级分类。候选按到达顺序收集，取第一个非空值。 */
interface ItemTypeNode {
  itemType: MedicalItemTypeValue | null
  itemTypeTextCandidates: string[]
  categories: CategoryNode[]
}

interface CategoryNode {
  categoryName: string
  groups: GroupNode[]
}

/** 尚未分配行标识的项目行：标识在整份合并结果上统一分配，中途不持占位值。 */
type UnidentifiedColumnRow = Omit<RecognitionProjectColumnRow, 'rowIdentity'>

interface GroupNode {
  groupName: string
  rows: UnidentifiedColumnRow[]
}

/** 按类型／分类／分组逐级查找节点，找不到时就地建立并追加在该层末尾（保持插入顺序）。 */
function findOrCreateItemType(nodes: ItemTypeNode[], itemType: MedicalItemTypeValue | null): ItemTypeNode {
  const existing = nodes.find((node) => node.itemType === itemType)
  if (existing) return existing
  const created: ItemTypeNode = { itemType, itemTypeTextCandidates: [], categories: [] }
  nodes.push(created)
  return created
}

function findOrCreateCategory(node: ItemTypeNode, categoryName: string): CategoryNode {
  const existing = node.categories.find((category) => category.categoryName === categoryName)
  if (existing) return existing
  const created: CategoryNode = { categoryName, groups: [] }
  node.categories.push(created)
  return created
}

function findOrCreateGroup(node: CategoryNode, groupName: string): GroupNode {
  const existing = node.groups.find((group) => group.groupName === groupName)
  if (existing) return existing
  const created: GroupNode = { groupName, rows: [] }
  node.groups.push(created)
  return created
}

/**
 * 把当前组织的配置行与当前有效标准目录合并为四层列视图（类型 → 分类 → 分组 → 项目行）。
 *
 * 两遍合并，顺序决定结果稳定：
 * 1. 先按**目录顺序**建立层级并取出同编码的配置行，因此有配置的项目保持目录顺序、未配置项目照常出现；
 * 2. 再按**配置行顺序**补齐目录读不到的配置行（目录停用），追加在各层末尾——这些行只存在于配置读取里，
 *    不补进来就会在页面上凭空消失，其目录停用原因也失去展示位置。
 *
 * 同一编码有多行配置时按队列依次取用：多出来的行进入第二遍，不会覆盖已匹配的行。
 *
 * 类型节点的文案候选按到达顺序收集，来源有三处：目录交付的 `itemTypeText`、第一遍匹配成功的配置行、
 * 第二遍补齐的配置行，取第一个非空值。项目类型中文的唯一来源是服务端枚举声明，目录与配置行交付的是同一份声明，
 * 任一来源有值就应当采用；只有三处都没有时才用本地兜底，否则服务端文案永远显示不出来。
 */
export function toRecognitionProjectColumnView(
  rows: readonly RecognitionProjectConfigurationRow[],
  catalogItems: readonly SelectableStandardItem[],
): RecognitionProjectColumnView {
  return buildColumnView(rows, catalogItems, false)
}

/**
 * 合并目录结构与配置行的实现。`includeUnconfigured` 决定目录中未被本组织配置的项目是否进入结果：
 * 页面列视图传 `false`（S2-D16：行集是本组织已配置项，未配置项目在页面上没有动作，进列表即成死胡同）；
 * 新增弹窗的可选集合传 `true`（S2-D17：弹窗只列未配置项，因此必须由目录一侧产生行）。
 */
function buildColumnView(
  rows: readonly RecognitionProjectConfigurationRow[],
  catalogItems: readonly SelectableStandardItem[],
  includeUnconfigured: boolean,
): RecognitionProjectColumnView {
  // 按标准项目编码排队，并记住每行在 `rows` 里的位置；编码缺失的行在目录里没有对应项目，直接留给第二遍。
  const queues = new Map<string, { index: number; row: RecognitionProjectConfigurationRow }[]>()
  for (const [index, row] of rows.entries()) {
    const code = row.standardProjectCode
    if (code === null || code.length === 0) continue
    const queue = queues.get(code)
    if (queue) queue.push({ index, row })
    else queues.set(code, [{ index, row }])
  }

  const itemTypes: ItemTypeNode[] = []
  /** 第一遍已取用的配置行位置；第二遍据此跳过，保证每行只出现一次，重复的行同样只算一次。 */
  const matched = new Set<number>()

  // 第一遍：按目录顺序建立层级，同时取用同编码的配置行。
  for (const item of catalogItems) {
    const taken = queues.get(item.code)?.shift()
    // 目录里这个项目没有被本组织配置：页面行集到此为止，不为它建层级，也不留空行。
    if (taken === undefined && !includeUnconfigured) continue
    const itemTypeNode = findOrCreateItemType(itemTypes, item.itemType)
    if (item.itemTypeText !== null && item.itemTypeText.length > 0) itemTypeNode.itemTypeTextCandidates.push(item.itemTypeText)
    const groupNode = findOrCreateGroup(findOrCreateCategory(itemTypeNode, item.categoryName), item.groupName)
    if (taken === undefined) {
      groupNode.rows.push({
        standardProjectCode: item.code,
        standardItemName: item.name,
        configured: false,
        configurationId: null,
        recognitionDurationDays: null,
        configurationStatus: null,
        configurationStatusText: null,
        unavailableReason: null,
      })
      continue
    }
    matched.add(taken.index)
    // 已匹配的配置行同样补充文案候选：目录这一遍没有交付文本时，这里的候选是类型格取到服务端文案的唯一机会。
    if (taken.row.itemTypeText.length > 0) itemTypeNode.itemTypeTextCandidates.push(taken.row.itemTypeText)
    groupNode.rows.push(toUnidentifiedConfiguredRow(taken.row))
  }

  // 第二遍：按配置行顺序补齐目录读不到的行（目录停用），层级按行内字段建立。
  for (const [index, row] of rows.entries()) {
    if (matched.has(index)) continue
    const itemTypeNode = findOrCreateItemType(itemTypes, row.itemType)
    if (row.itemTypeText.length > 0) itemTypeNode.itemTypeTextCandidates.push(row.itemTypeText)
    const groupNode = findOrCreateGroup(findOrCreateCategory(itemTypeNode, row.categoryName ?? ''), row.groupName ?? '')
    groupNode.rows.push(toUnidentifiedConfiguredRow(row))
  }

  // 类型按取值升序、`null` 排最后：同一组织的重复读取因此得到相同顺序。
  const ordered = [...itemTypes].sort((left, right) => {
    if (left.itemType === right.itemType) return 0
    if (left.itemType === null) return 1
    if (right.itemType === null) return -1
    return left.itemType - right.itemType
  })

  /**
   * 行标识在最后一遍按「类型 → 分类 → 分组 → 行」统一分配。业务键取配置标识，缺失时用编码与名称组成；
   * 同一业务键在整份合并结果内重复出现时只追加出现序号（首次不加后缀），保证行标识唯一、
   * 行内操作控件不串位。不使用数组下标：行在列表中移动位置时标识保持不变，本序号也不参与业务判定。
   */
  const occurrences = new Map<string, number>()
  return ordered.map((node) => ({
    itemType: node.itemType,
    itemTypeText: node.itemTypeTextCandidates[0] ?? medicalItemTypeText(node.itemType),
    categories: node.categories.map((category) => ({
      categoryName: category.categoryName,
      groups: category.groups.map((group) => ({
        groupName: group.groupName,
        rows: group.rows.map((row) => {
          const businessIdentity = row.configurationId
            ?? [row.standardProjectCode, row.standardItemName].map((value) => value ?? '').join('|')
          const occurrence = occurrences.get(businessIdentity) ?? 0
          occurrences.set(businessIdentity, occurrence + 1)
          return { ...row, rowIdentity: occurrence === 0 ? businessIdentity : `${businessIdentity}#${occurrence}` }
        }),
      })),
    })),
  }))
}

/** 配置行 → 列视图行（行标识随后统一分配）；未配置行不经过这里，两者的字段差异因此集中在一处。 */
function toUnidentifiedConfiguredRow(row: RecognitionProjectConfigurationRow): UnidentifiedColumnRow {
  return {
    standardProjectCode: row.standardProjectCode,
    standardItemName: row.standardItemName,
    configured: true,
    configurationId: row.configurationId,
    recognitionDurationDays: row.recognitionDurationDays,
    configurationStatus: row.configurationStatus,
    configurationStatusText: row.configurationStatusText,
    unavailableReason: row.unavailableReason,
  }
}

/**
 * 按本地展示筛选裁剪列视图：先筛项目行，再丢弃没有可见项目的分组、分类与类型，
 * 使裁剪后的每一层都只承载有内容的节点，不留下空壳。判定与配置行筛选共用同一条出口。
 */
export function filterRecognitionProjectColumnView(
  view: RecognitionProjectColumnView,
  filter: RecognitionProjectFilter,
): RecognitionProjectColumnView {
  return view.flatMap((itemType) => {
    const categories = itemType.categories.flatMap((category) => {
      const groups = category.groups.flatMap((group) => {
        const rows = group.rows.filter((row) => matchesRecognitionProjectFilter(row, filter))
        return rows.length === 0 ? [] : [{ groupName: group.groupName, rows }]
      })
      return groups.length === 0 ? [] : [{ categoryName: category.categoryName, groups }]
    })
    return categories.length === 0 ? [] : [{ ...itemType, categories }]
  })
}

/** 列视图中的项目行总数，用于列表计数展示。 */
export function countColumnRows(view: RecognitionProjectColumnView): number {
  let count = 0
  for (const itemType of view) {
    for (const category of itemType.categories) {
      for (const group of category.groups) count += group.rows.length
    }
  }
  return count
}

/**
 * 可选集合的列视图：供新增弹窗按目录层级展示可勾选的标准项目。
 *
 * 配置行为空，因此每一行都是「未配置」行，没有配置标识、可互认时间与配置状态；
 * 目录停用项目不会出现在这里——可选集合来自当前有效目录，停用项目本就不在其中。
 */
export function toSelectableColumnView(items: readonly SelectableStandardItem[]): RecognitionProjectColumnView {
  return buildColumnView([], items, true)
}

/**
 * 单批创建的上限（与 S2-D19 的服务端口径同值）。弹窗在本地按它拦截，避免把必然被拒绝的批次发出去。
 */
export const BATCH_CREATE_LIMIT = 1000

/** 选择条数是否超过单批上限；超过时由弹窗就地说明并不发请求。 */
export function exceedsBatchCreateLimit(selectedCount: number): boolean {
  return selectedCount > BATCH_CREATE_LIMIT
}

/**
 * 状态徽标口径：目录停用优先于配置自身状态，未配置行单独表达，配置自身状态取服务端交付的中文。
 * 目录停用只表达标准目录的当前状态，不代表配置自身停用。
 */
export function recognitionProjectRowBadge(row: RecognitionProjectColumnRow): RecognitionProjectRowBadge {
  if (row.unavailableReason !== null) return { text: '目录已停用', color: 'error', reason: row.unavailableReason }
  if (!row.configured) return { text: '未配置', color: 'default', reason: null }
  return { text: row.configurationStatusText ?? '未知', color: configurationStatusTagColor(row.configurationStatus), reason: null }
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
  if (request.keyword !== undefined) body.keyword = request.keyword
  const values = await client.api.medicalRecognitionReportQuery.queryRecognitionProjectConfigurationList.post(body)
  return toConfigurationRows(values ?? [])
}

/**
 * 读取**可新增**的标准项目目录（服务端已按「三级均启用的有效目录 − 本组织已配置」过滤），用于新增弹窗。
 *
 * 关键字是服务端查询条件（S2-D29）：与页面筛选同一口径，按字面包含同时匹配标准项目编码、名称、
 * 所属分类名称与所属分组名称。组织不在入参里——服务端只按可信上下文排除本组织的配置。
 */
export async function querySelectableStandardItems(
  client: MedicalRecognitionClient,
  keyword?: string,
): Promise<EffectiveMedicalStandardCatalogReadModel | null> {
  const body: SelectableStandardItemListQueryRequest = {}
  const trimmed = keyword?.trim() ?? ''
  if (trimmed.length > 0) body.keyword = trimmed
  const catalog = await client.api.medicalRecognitionReportQuery.querySelectableStandardItemList.post(body)
  return catalog ?? null
}

/** 批量新增互认配置：一次请求建立全部所选项目，全部默认启用；载荷不含组织编码与内部标准项目 ID。 */
export async function createRecognitionProjectConfigurations(
  client: MedicalRecognitionClient,
  request: BatchCreateConfigurationPayload,
): Promise<void> {
  await client.api.medicalRecognitionReport.createMutualRecognitionItems.post({
    standardProjectCodes: request.standardProjectCodes,
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
