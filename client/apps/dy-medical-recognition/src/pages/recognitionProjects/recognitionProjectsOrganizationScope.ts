import { useApiClientContext } from '../../contexts/ApiClientContext'
import { readCurrentOrganizationScope, useResolvedTrustedScope } from '../../components/currentOrganizationScope'
import { useHostSessionSnapshot } from '../../session/hostSession'

/**
 * 可信当前组织范围。
 *
 * `organizationCode` 是业务请求提交的组织标识（IAM 组织业务编码）；
 * `organizationId` 是组织实例标识，只用于选择器受控取值与宿主会话映射，不进入业务请求。
 */
export type TrustedOrganizationScope =
  | { kind: 'ready'; organizationCode: string; organizationId: string }
  | { kind: 'unavailable' }

/** 业务编码缺失或纯空白按缺失处理；不把实例标识文本当作编码。 */
function readTrustedCode(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 命令式读取当前可信组织的业务编码，不经过任何 React 状态或渲染期闭包。
 *
 * 取值只来自当前范围接口的组织业务编码，trim 后长度为 0（空值或纯空白）一律返回 null
 * 表示不可用：不伪造组织、不静默使用空串、不退化为全局查询，也不回落到组织实例标识。
 *
 * 用途是提交前复核：渲染期门控只表达它渲染那一帧读到的组织，宿主切换工作身份之后、
 * 页面重渲染之前，两者可能已经不是同一个值；提交动作因此需要在发请求前读一次现值。
 */
export function readTrustedOrganizationCode(): string | null {
  return readTrustedCode(readCurrentOrganizationScope()?.organizationCode)
}

/**
 * 可信当前组织的唯一接缝。
 *
 * 组织业务编码来自当前范围接口（IAM 组织业务编码），组织实例标识同源取得并只交给选择器；
 * 组织切换由宿主切换工作身份完成，服务端只接受与它相等的组织，不建授权集合。
 * 因此页面不持有可选组织集合、不提供自由输入，也不从 URL、Storage、表单或列表构造范围。
 *
 * 读取只走当前范围接口：匿名、初始化与错误快照不携带身份，返回不可用，页面进入阻断态。
 * 取值与判定只走 readTrustedOrganizationCode，渲染期与提交期同一口径。
 */
export function useTrustedOrganizationScope(): TrustedOrganizationScope {
  const client = useApiClientContext()
  const session = useHostSessionSnapshot()
  const resolved = useResolvedTrustedScope(client, session)
  if (resolved.status !== 'ready') return { kind: 'unavailable' }
  const organizationCode = readTrustedCode(resolved.organizationCode)
  const organizationId = readTrustedCode(resolved.organizationId)
  if (organizationCode === null || organizationId === null) return { kind: 'unavailable' }
  return { kind: 'ready', organizationCode, organizationId }
}
