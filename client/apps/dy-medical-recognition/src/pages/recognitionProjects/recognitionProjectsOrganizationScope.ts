import { LoginUserManager } from '@dy/auth'
import { useAuth } from '@dy/auth-react'

/** 可信当前组织范围。 */
export type TrustedOrganizationScope =
  | { kind: 'ready'; organizationCode: string }
  | { kind: 'unavailable' }

/**
 * 命令式读取**当前**可信组织编码，不经过任何 React 状态或渲染期闭包。
 *
 * 取值与判定和 `useTrustedOrganizationScope` 共用同一份实现：编码取自
 * `@dy/auth` 的 `LoginUserManager.getOrgID()`，trim 后长度为 0（空值或纯空白）一律返回 `null`
 * 表示不可用——不伪造组织、不静默使用空串、不退化为全局查询。
 *
 * 用途是**提交前复核**：渲染期门控只表达它渲染那一帧读到的组织，宿主重新签发 token 之后、
 * 页面重渲染之前，两者可能已经不是同一个值；提交动作因此需要在发请求前读一次现值。
 */
export function readTrustedOrganizationCode(): string | null {
  const organizationCode = (LoginUserManager.getOrgID() ?? '').trim()
  return organizationCode.length > 0 ? organizationCode : null
}

/**
 * 「可信当前组织」的唯一接缝。
 *
 * 可信当前组织来自 Bearer token 的 `org` claim（服务端 `HttpRequestInfo.OrgId`），服务端只接受与它相等的组织；
 * 组织切换由**宿主重新签发 token** 完成，服务端不建授权集合。因此页面不持有可选组织集合、
 * 不提供自由输入，也不从 URL、Storage、表单或列表构造范围。
 *
 * 读取沿用既有包：`@dy/auth` 的 `LoginUserManager.getOrgID()` 给出当前组织编码，
 * `@dy/auth-react` 的 `useAuth()` 给出 token；token 变化即重新求值，页面据此识别组织变化。
 * 组织缺失或为空白时返回不可用，页面进入阻断态：不伪造组织、不静默使用空串、不退化为全局查询。
 * 取值与判定只走 `readTrustedOrganizationCode`，渲染期与提交期同一口径。
 */
export function useTrustedOrganizationScope(): TrustedOrganizationScope {
  const { token } = useAuth()
  // token 缺失时凭据不表达可信组织，直接判为不可用；其余情况一律按命令式读取的同一口径判定。
  const organizationCode = token ? readTrustedOrganizationCode() : null
  return organizationCode === null ? { kind: 'unavailable' } : { kind: 'ready', organizationCode }
}
