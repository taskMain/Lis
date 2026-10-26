import { useSyncExternalStore } from 'react'
import { MicroSDK, type AuthSessionSnapshot } from '@dy/micro-sdk'

/** 宿主会话状态取值，与宿主会话快照一致。 */
export type HostSessionStatus = AuthSessionSnapshot['status']

/**
 * 宿主会话的稳定视图模型：页面与页头只依赖本类型，不直接读取宿主快照。
 * 身份、组织、医院与院区缺失时一律为 null，不伪造范围。
 */
export interface HostSession {
  status: HostSessionStatus
  sessionVersion: string
  userId: string | null
  account: string | null
  displayName: string | null
  organizationId: string | null
  hospitalId: string | null
  campusId: string | null
}

/** 尚未取得会话快照时的初始值：不伪造身份，也不进入匿名态。 */
export const INITIAL_HOST_SESSION: HostSession = {
  status: 'initializing',
  sessionVersion: '',
  userId: null,
  account: null,
  displayName: null,
  organizationId: null,
  hospitalId: null,
  campusId: null,
}

/** 空白文本按缺失处理；组织与院区标识不做编码转换。 */
function readText(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 将宿主会话快照映射为视图模型。
 * 只有已认证与刷新中的快照携带身份；匿名、初始化与错误快照不保留任何旧身份字段。
 */
export function toHostSession(snapshot: AuthSessionSnapshot): HostSession {
  const hasIdentity = snapshot.status === 'authenticated' || snapshot.status === 'refreshing'
  const user = hasIdentity ? snapshot.user : null
  const context = hasIdentity ? snapshot.loginContext : null
  return {
    status: snapshot.status,
    sessionVersion: readText(snapshot.sessionVersion) ?? '',
    userId: readText(user?.id),
    account: readText(user?.account),
    displayName: readText(user?.displayName),
    organizationId: readText(context?.organizationId),
    hospitalId: readText(context?.hospitalId),
    campusId: readText(context?.campusId),
  }
}

/**
 * 最近一次取得的宿主会话快照。
 *
 * 提交期复核等命令式读取不能依赖渲染期闭包：宿主重新签发会话后、页面重渲染之前，
 * 闭包里的值已经不是当前值，因此这里保留一份由读取与订阅共同更新的镜像。
 */
let latestSession: HostSession | null = null

/** useSyncExternalStore 的快照订阅者：初始读取完成时也要通知，Provider 不参与该集合。 */
const snapshotListeners = new Set<() => void>()

function notifySnapshotListeners(): void {
  for (const listener of [...snapshotListeners]) listener()
}

/** 命令式读取最近一次会话快照；尚未取得任何快照时返回 null，不伪造身份。 */
export function readCurrentHostSession(): HostSession | null {
  return latestSession
}

/** 读取当前宿主会话快照并映射为视图模型；结果同时推送给已订阅的页面。 */
export function readHostSession(): Promise<HostSession> {
  return Promise.resolve(MicroSDK.auth.getSession()).then((snapshot) => {
    latestSession = toHostSession(snapshot)
    notifySnapshotListeners()
    return latestSession
  })
}

/**
 * useSyncExternalStore 的订阅入口：模块级稳定引用，不能每次渲染重新创建。
 * 不依赖 Provider 的页面也在这里建立宿主订阅并触发一次初始读取；订阅回放只更新快照，
 * 不会重新创建 subscribe 引用。
 */
function subscribeToHostSession(listener: () => void): () => void {
  snapshotListeners.add(listener)
  const unsubscribeHost = subscribeHostSession(() => {})
  if (latestSession === null) void readHostSession()
  return () => {
    snapshotListeners.delete(listener)
    unsubscribeHost()
  }
}

/** 服务端/水合快照：模块级稳定引用，未取得会话时返回 null。 */
function getServerHostSession(): null {
  return null
}

/**
 * 以外部存储方式订阅宿主会话快照，供不依赖 Provider 的页面直接读取。
 *
 * 会话唯一来源仍是宿主运行时：本钩子只订阅同一份镜像，不引入第二处身份来源，
 * 也不在未取得快照时伪造身份（返回 null 由调用方按不可用处理）。
 */
export function useHostSessionSnapshot(): HostSession | null {
  return useSyncExternalStore(subscribeToHostSession, readCurrentHostSession, getServerHostSession)
}

/** 订阅宿主会话变化；返回原样取消订阅函数，由调用方在卸载时释放。 */
export function subscribeHostSession(listener: (session: HostSession) => void): () => void {
  return MicroSDK.auth.subscribe((snapshot) => {
    latestSession = toHostSession(snapshot)
    notifySnapshotListeners()
    listener(latestSession)
  })
}
