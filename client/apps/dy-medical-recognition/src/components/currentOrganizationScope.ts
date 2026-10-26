import { useEffect, useState } from 'react'
import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import { readCurrentHostSession, type HostSession } from '../session/hostSession'
import type { CurrentOrganizationScope } from './organizationScope'
import { queryCurrentOrganizationScope } from './organizationScopeApi'

export type { CurrentOrganizationScope } from './organizationScope'

/** 当前范围的状态：加载中、读取完成或当前会话不可用。 */
export interface CurrentOrganizationScopeState {
  status: 'loading' | 'ready' | 'unavailable'
  scope: CurrentOrganizationScope
}

/**
 * 当前工作身份的可信三层范围。
 *
 * 实例标识与业务编码同时承载、来源同源：实例标识只用于选择器取值与宿主会话映射，
 * 业务请求的 Code 字段一律取业务编码；业务编码为空表示该层在 IAM 上未配置编码，
 * 此时该层不可提交，既不用实例标识补齐，也不降级为空串。
 */
export interface ResolvedTrustedScope {
  status: 'loading' | 'ready' | 'unavailable'
  organizationId: string | null
  hospitalId: string | null
  branchId: string | null
  organizationCode: string | null
  hospitalCode: string | null
  branchCode: string | null
}

/** 空范围：任何一级都没有可信值。 */
export const EMPTY_CURRENT_ORGANIZATION_SCOPE: CurrentOrganizationScope = {
  organizationId: null,
  organizationName: null,
  organizationCode: null,
  hospitalId: null,
  hospitalName: null,
  hospitalCode: null,
  branchId: null,
  branchName: null,
  branchCode: null,
}

/** 最近一次范围查询结果；命令式读取按当前会话键校验，避免旧身份范围残留。 */
let latestScope: { key: string; scope: CurrentOrganizationScope } | null = null

/** 会话键：身份切换后旧范围不再命中。 */
function sessionKeyOf(session: HostSession | null): string {
  return `${session?.sessionVersion ?? ''}|${session?.userId ?? ''}`
}

/** 命令式读取当前会话已取得的三层范围；尚未查询或会话不一致时返回 null。 */
export function readCurrentOrganizationScope(): CurrentOrganizationScope | null {
  const session = readCurrentHostSession()
  if (session === null || latestScope === null || latestScope.key !== sessionKeyOf(session)) return null
  return latestScope.scope
}

/** 会话是否携带身份；匿名、初始化与错误快照不发起范围查询。 */
function hasIdentity(session: HostSession | null): boolean {
  return session !== null
    && session.userId !== null
    && (session.status === 'authenticated' || session.status === 'refreshing')
}

/**
 * 读取当前工作身份的组织范围。
 *
 * - 只经共享范围适配出口调用；匿名与初始化状态不发业务请求，范围为空。
 * - 结果按会话键（`sessionVersion` 与用户标识）记录：身份切换后键不一致，旧身份的范围不会残留，
 *   迟到的旧响应也不会写入新会话。
 * - 失败时范围为空（展示层显示「未知」），错误由宿主统一提示，本层不新增错误提示。
 * - `enabled=false` 时不发请求，用于调用方已从会话取得完整三层、无需再查的场景。
 */
export function useCurrentOrganizationScope(
  client: MedicalRecognitionClient,
  session: HostSession | null,
  options: { enabled?: boolean } = {},
): CurrentOrganizationScopeState {
  const enabled = options.enabled ?? true
  const authenticated = enabled && hasIdentity(session)
  const sessionKey = sessionKeyOf(session)
  const [result, setResult] = useState<{
    key: string
    scope: CurrentOrganizationScope
    failed: boolean
  } | null>(null)

  useEffect(() => {
    if (!authenticated) return undefined
    const key = sessionKey
    let active = true
    void queryCurrentOrganizationScope(client).then(
      (scope) => {
        if (!active) return
        latestScope = { key, scope }
        setResult({ key, scope, failed: false })
      },
      () => {
        if (!active) return
        // 读取失败按范围缺失处理：展示层显示「未知」，重试用下一次会话或页面重载承担。
        latestScope = { key, scope: EMPTY_CURRENT_ORGANIZATION_SCOPE }
        setResult({ key, scope: EMPTY_CURRENT_ORGANIZATION_SCOPE, failed: true })
      },
    )
    return () => {
      active = false
    }
  }, [authenticated, client, sessionKey])

  if (!authenticated) return { status: 'unavailable', scope: EMPTY_CURRENT_ORGANIZATION_SCOPE }
  if (result === null || result.key !== sessionKey) return { status: 'loading', scope: EMPTY_CURRENT_ORGANIZATION_SCOPE }
  return { status: result.failed ? 'unavailable' : 'ready', scope: result.scope }
}

/**
 * 页面可信范围：三级范围的实例标识与业务编码。
 *
 * 业务编码只由本项目当前范围接口交付，宿主会话只给组织叶子与实例标识，因此这里始终经该接口取值：
 * 会话已带完整医院层时也照常查询，否则请求侧拿不到业务编码，只能回落实例标识（禁止）。
 * 接口未完成或不可用时保留会话已有的实例标识，业务编码为空——页面据此按「可信范围不可用」阻断，
 * 不伪造编码、也不退化为实例标识。
 */
export function useResolvedTrustedScope(
  client: MedicalRecognitionClient,
  session: HostSession | null,
): ResolvedTrustedScope {
  const current = useCurrentOrganizationScope(client, session)
  if (current.status === 'ready') {
    return {
      status: 'ready',
      organizationId: current.scope.organizationId,
      hospitalId: current.scope.hospitalId,
      branchId: current.scope.branchId,
      organizationCode: current.scope.organizationCode,
      hospitalCode: current.scope.hospitalCode,
      branchCode: current.scope.branchCode,
    }
  }
  if (session !== null && session.userId !== null && session.organizationId !== null) {
    return {
      status: 'ready',
      organizationId: session.organizationId,
      hospitalId: session.hospitalId,
      branchId: session.campusId,
      organizationCode: null,
      hospitalCode: null,
      branchCode: null,
    }
  }
  return {
    status: current.status,
    organizationId: null,
    hospitalId: null,
    branchId: null,
    organizationCode: null,
    hospitalCode: null,
    branchCode: null,
  }
}
