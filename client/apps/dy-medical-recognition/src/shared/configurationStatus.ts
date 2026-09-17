/**
 * 配置状态的取值域与兜底文案（跨页面共享的单一事实来源）。
 *
 * 契约按数值交互：生成端不产出 TS 枚举，数值与后端 `ConfigurationStatus` 一致（1 启用、2 停用）。
 * 枚举中文的唯一来源是服务端交付的随行文案（只读模型的 `configurationStatusText`）与枚举元数据接口；
 * 本模块只提供「契约字段缺失或元数据接口不可用」时的兜底文案，页面不得用它覆盖服务端文案。
 */
export const CONFIGURATION_STATUSES = Object.freeze([1, 2] as const)
export type ConfigurationStatusValue = (typeof CONFIGURATION_STATUSES)[number]

/**
 * 取值域判定：只认已确认的数值取值。字符串、`NaN`、越界数字、`null`/`undefined` 一律为假，
 * 使映射、筛选收窄与文案出口共用同一份取值域事实，不必各自重写 `value === 1 || value === 2`。
 */
export function isConfigurationStatusValue(value: unknown): value is ConfigurationStatusValue {
  return typeof value === 'number' && (CONFIGURATION_STATUSES as readonly number[]).includes(value)
}

/**
 * 已确认配置状态的具名取值：调用方按名引用「启用 / 停用」，
 * 不再按取值集合的下标读取（下标语义只在本声明处体现一次）。
 */
export const ENABLED_CONFIGURATION_STATUS: ConfigurationStatusValue = CONFIGURATION_STATUSES[0]
export const DISABLED_CONFIGURATION_STATUS: ConfigurationStatusValue = CONFIGURATION_STATUSES[1]

/**
 * 配置状态兜底文案；键集合与 `CONFIGURATION_STATUSES` 严格一致（TS 的映射类型会让两者缺一即编译失败）。
 * 只读且运行时冻结：多个页面共享同一份引用，不得被任一页面就地改写。
 */
export const CONFIGURATION_STATUS_TEXTS: Readonly<Record<ConfigurationStatusValue, string>> = Object.freeze({ 1: '启用', 2: '停用' })

/** 未知状态（`null`）的安全展示文案；不默认成任一已确认状态。 */
export const UNKNOWN_CONFIGURATION_STATUS_TEXT = '未知'

/**
 * 配置状态兜底文案出口；只接受已确认取值，其余（缺失、越界、字符串等）一律返回「未知」，
 * 不伪造为启用或停用。判定走 `isConfigurationStatusValue`，理由同 `medicalItemTypeText`。
 */
export function configurationStatusText(value: unknown): string {
  return isConfigurationStatusValue(value) ? CONFIGURATION_STATUS_TEXTS[value] : UNKNOWN_CONFIGURATION_STATUS_TEXT
}
