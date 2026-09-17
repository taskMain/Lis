/**
 * 项目类型的取值域与兜底文案（跨页面共享的单一事实来源）。
 *
 * 契约按数值交互：生成端不产出 TS 枚举，数值与后端 `MedicalItemType` 一致（0 检验、1 检查）。
 * 枚举中文的唯一来源是服务端交付的随行文案（只读模型的 `itemTypeText`）与枚举元数据接口；
 * 本模块只提供「契约字段缺失或元数据接口不可用」时的兜底文案，页面不得用它覆盖服务端文案。
 */
export const MEDICAL_ITEM_TYPES = Object.freeze([0, 1] as const)
export type MedicalItemTypeValue = (typeof MEDICAL_ITEM_TYPES)[number]

/**
 * 取值域判定：只认已确认的数值取值。字符串、`NaN`、越界数字、`null`/`undefined` 一律为假，
 * 使映射、筛选收窄与文案出口共用同一份取值域事实，不必各自重写 `value === 0 || value === 1`。
 */
export function isMedicalItemTypeValue(value: unknown): value is MedicalItemTypeValue {
  return typeof value === 'number' && (MEDICAL_ITEM_TYPES as readonly number[]).includes(value)
}

/**
 * 项目类型兜底文案；键集合与 `MEDICAL_ITEM_TYPES` 严格一致（TS 的映射类型会让两者缺一即编译失败）。
 * 只读且运行时冻结：三个页面共享同一份引用，不得被任一页面就地改写。
 */
export const MEDICAL_ITEM_TYPE_TEXTS: Readonly<Record<MedicalItemTypeValue, string>> = Object.freeze({ 0: '检验', 1: '检查' })

/** 未知项目类型的展示文案；不默认成任一已确认类型。 */
export const UNKNOWN_MEDICAL_ITEM_TYPE_TEXT = '未知类型'

/**
 * 项目类型兜底文案出口：只接受已确认取值，其余（缺失、越界、字符串等）一律显示「未知类型」。
 * 判定走 `isMedicalItemTypeValue` 而不是「是否等于 null」，否则越界取值会取到 `undefined`（渲染成空文本），
 * 而字符串键（如 `'1'`）还会被对象下标强制转换、凭空造出已知类型文案。
 */
export function medicalItemTypeText(value: unknown): string {
  return isMedicalItemTypeValue(value) ? MEDICAL_ITEM_TYPE_TEXTS[value] : UNKNOWN_MEDICAL_ITEM_TYPE_TEXT
}
