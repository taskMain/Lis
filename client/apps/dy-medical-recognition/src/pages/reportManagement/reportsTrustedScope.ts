import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useResolvedTrustedScope, readCurrentOrganizationScope } from '../../components/currentOrganizationScope'
import { useHostSessionSnapshot } from '../../session/hostSession'

/** 可信医院与可信院区范围：两层都是 IAM 组织业务编码。 */
export type TrustedHospitalScope =
  | { kind: 'ready'; hospitalCode: string; branchCode: string | null }
  | { kind: 'unavailable' }

/** 业务编码缺失或纯空白按缺失处理；不把实例标识文本当作编码。 */
function readTrustedCode(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 命令式读取当前可信医院的业务编码，不经过任何 React 状态或渲染期闭包。
 *
 * 取值只来自当前范围接口的医院业务编码，trim 后为空一律返回 null 表示不可用；
 * 宿主会话只给医院实例标识，因此不再以其兜底——业务请求的 Code 字段不得回落实例标识。
 * 与渲染期读取共用同一判定，因此提交期复核与页面展示不会得到两种结论。
 */
export function readTrustedHospitalCode(): string | null {
  return readTrustedCode(readCurrentOrganizationScope()?.hospitalCode)
}

/** 命令式读取当前可信院区的业务编码；可信院区是可选层，取不到时返回 null。 */
export function readTrustedBranchCode(): string | null {
  return readTrustedCode(readCurrentOrganizationScope()?.branchCode)
}

/**
 * 可信当前医院与院区的唯一接缝。
 *
 * 可信医院取当前范围接口的医院业务编码，可信院区取院区业务编码；院区是可选层，
 * 在页面上仍可由用户在当前可信医院内改选，因此这里只把可信院区当作默认值，不视为用户的选择。
 * 工作身份变化即重新求值，页面据此识别医院变化。
 * 医院编码缺失时返回不可用，页面进入阻断态：不伪造医院、不静默使用空串、不退化为全局查询、
 * 也不回落到医院实例标识。
 */
export function useTrustedHospitalScope(): TrustedHospitalScope {
  const client = useApiClientContext()
  const session = useHostSessionSnapshot()
  const resolved = useResolvedTrustedScope(client, session)
  if (resolved.status !== 'ready') return { kind: 'unavailable' }
  const hospitalCode = readTrustedCode(resolved.hospitalCode)
  if (hospitalCode === null) return { kind: 'unavailable' }
  return { kind: 'ready', hospitalCode, branchCode: readTrustedCode(resolved.branchCode) }
}
