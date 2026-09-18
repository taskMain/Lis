import { LoginUserManager } from '@dy/auth'
import { useAuth } from '@dy/auth-react'

/** 可信医院与可信院区范围。 */
export type TrustedHospitalScope =
  | { kind: 'ready'; hospitalCode: string; branchCode: string | null }
  | { kind: 'unavailable' }

/**
 * 读取业务编码：缺失或纯空白一律视为不可用，不伪造、不静默使用空串。
 *
 * 与 `recognitionProjectsOrganizationScope` 的 `readTrustedOrganizationCode` 同一口径，但那个出口
 * 只承载组织一层；医院与院区两层在本文件按同一判定重新实现，不从组织出口推断医院。
 */
function readTrustedCode(value: string | null): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 命令式读取**当前**可信医院编码，不经过任何 React 状态或渲染期闭包。
 *
 * 取值取自 `@dy/auth` 的 `LoginUserManager.getHosID()`，trim 后为空一律返回 `null` 表示不可用。
 * 与渲染期读取共用同一判定，因此提交期复核与页面展示不会得到两种结论。
 */
export function readTrustedHospitalCode(): string | null {
  return readTrustedCode(LoginUserManager.getHosID())
}

/**
 * 命令式读取**当前**可信院区编码；可信院区是可选层，取不到时返回 `null`。
 */
export function readTrustedBranchCode(): string | null {
  return readTrustedCode(LoginUserManager.getBranchID())
}

/**
 * 「可信当前医院与院区」的唯一接缝。
 *
 * 可信医院来自 Bearer token 的医院声明（服务端 `HttpRequestInfo.HosId`），可信院区来自院区声明
 * （`HttpRequestInfo.BranchId`）；院区是可选层，在页面上仍可由用户在当前可信医院内改选，
 * 因此这里只把可信院区当作默认值，不视为用户的选择。token 变化即重新求值，页面据此识别医院变化。
 * 医院缺失或为空白时返回不可用，页面进入阻断态：不伪造医院、不静默使用空串、不退化为全局查询。
 */
export function useTrustedHospitalScope(): TrustedHospitalScope {
  const { token } = useAuth()
  // token 缺失时凭据不表达可信医院，直接判为不可用；其余情况一律按命令式读取的同一口径判定。
  if (!token) return { kind: 'unavailable' }
  const hospitalCode = readTrustedHospitalCode()
  if (hospitalCode === null) return { kind: 'unavailable' }
  return { kind: 'ready', hospitalCode, branchCode: readTrustedBranchCode() }
}
