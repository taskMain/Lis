import type { MedicalRecognitionClient, PlatformParameterReadModel } from '@dy/api-client-medical-recognition'

/** 平台参数维护行：编码、名称、当前值、说明、启用状态与最近操作信息。 */
export interface PlatformParameterRow {
  code: string
  name: string
  value: string | null
  description: string | null
  isConfigured: boolean
  isValid: boolean
  operId: string | null
  operTime: Date | null
}

/** 保存载荷：参数编码定位记录，值、说明与启用状态随请求提交。 */
export interface PlatformParameterSavePayload {
  code: string
  value: string
  description: string | null
  isValid: boolean
}

/** 参数定义：编码、显示名称与取值提示；页面按固定业务顺序展示这三项。 */
export interface PlatformParameterDefinition {
  code: string
  name: string
  valueHint: string
}

/** 本应用维护的三项平台参数，顺序与业务说明一致。 */
export const PLATFORM_PARAMETER_DEFINITIONS: readonly PlatformParameterDefinition[] = [
  { code: 'own_hospital_match_switch', name: '本院报告匹配开关', valueHint: '0 表示关闭，1 表示开启' },
  { code: 'own_hospital_exclude_hours', name: '本院报告排除时长', valueHint: '非负整数小时，0 表示不排除' },
  { code: 'citation_detail_valid_hours', name: '引用详情有效时长', valueHint: '正整数小时' },
]

/** 后端取值按 int32 解析；超出范围的值在提交前同样阻断，避免与服务端判定不一致。 */
const INT32_MAX = 2147483647

/** 非负整数文本：只接受十进制数字，且不超过 int32 上限。 */
function isNonNegativeIntegerText(value: string): boolean {
  if (!/^\d+$/.test(value)) return false
  const parsed = Number(value)
  return Number.isSafeInteger(parsed) && parsed <= INT32_MAX
}

/** 正整数文本：在非负整数基础上要求大于零。 */
function isPositiveIntegerText(value: string): boolean {
  return isNonNegativeIntegerText(value) && Number(value) > 0
}

/**
 * 提交前校验：按参数编码执行与后端一致的取值规则。
 * 返回错误文案表示不允许提交；返回 `null` 表示可以提交。
 */
export function validatePlatformParameterValue(parameterCode: string, value: string): string | null {
  switch (parameterCode) {
    case 'own_hospital_match_switch':
      return value === '0' || value === '1' ? null : '本院报告匹配开关只能填写 0 或 1。'
    case 'own_hospital_exclude_hours':
      return isNonNegativeIntegerText(value) ? null : '本院报告排除时长必须是非负整数小时。'
    case 'citation_detail_valid_hours':
      return isPositiveIntegerText(value) ? null : '引用详情有效时长必须是正整数小时。'
    default:
      return '参数编码不存在。'
  }
}

/** 操作人标识按非空文本承载；缺失归为 `null`。 */
function readOperId(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 读模型列表 → 维护行。
 *
 * 只保留三项已登记参数，按固定业务顺序输出；缺少记录的参数标为未配置，不伪造参数值与启用状态。
 * 后端返回的未登记编码不进入维护列表，避免页面出现无法按规则校验的行。
 */
export function toPlatformParameterRows(
  values: readonly PlatformParameterReadModel[] | undefined,
): PlatformParameterRow[] {
  const stored = new Map<string, PlatformParameterReadModel>()
  for (const value of values ?? []) {
    if (typeof value.parameterCode === 'string' && value.parameterCode.length > 0) {
      stored.set(value.parameterCode, value)
    }
  }

  return PLATFORM_PARAMETER_DEFINITIONS.map((definition) => {
    const parameter = stored.get(definition.code)
    return {
      code: definition.code,
      name: definition.name,
      value: parameter?.parameterValue ?? null,
      description: parameter?.description ?? null,
      isConfigured: parameter !== undefined,
      isValid: parameter?.isValid === true,
      operId: readOperId(parameter?.operId),
      operTime: parameter?.operTime ?? null,
    }
  })
}

/** 读取全部平台参数并归一为维护行；每次调用都走服务端，页面不缓存旧值。 */
export async function queryPlatformParameters(client: MedicalRecognitionClient): Promise<PlatformParameterRow[]> {
  const values = await client.api.v1.platformParameters.get()
  return toPlatformParameterRows(values)
}

/** 按参数编码保存单条参数；返回端点是否明确确认成功，供页面决定是否呈现保存成功。 */
export async function savePlatformParameter(
  client: MedicalRecognitionClient,
  payload: PlatformParameterSavePayload,
): Promise<boolean> {
  const result = await client.api.v1.platformParameters.byParameterCode(payload.code).put({
    parameterValue: payload.value,
    description: payload.description,
    isValid: payload.isValid,
  })
  return result === true
}
