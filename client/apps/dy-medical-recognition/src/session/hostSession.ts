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
 * 应用内唯一的宿主会话状态：已接受快照与读取失败记录。
 *
 * 失败记录独立于身份字段，因此能承载任意拒绝值，也不会被伪装成宿主 `status: 'error'` 快照。
 * 外部存储取值使用状态对象本身，失败记录出现或消失时同样触发渲染，页面据此按设计暴露失败。
 */
interface HostSessionState {
  session: HostSession | null
  failure: { value: unknown } | null
}

/** 尚未取得任何快照的稳定空状态：服务端快照与全部释放后的状态共用，引用保持稳定。 */
const UNAVAILABLE_STATE: HostSessionState = { session: null, failure: null }

/** 一次读取启动时的时序凭据：生命周期代次、宿主事件修订与读取序号。 */
interface HostSessionReadTicket {
  generation: number
  revision: number
  sequence: number
}

/** 应用内生命周期代次：最后一个消费者释放时递增，旧代次的读取与失败一律失效。 */
let lifecycleGeneration = 0

/** 宿主事件修订计数：每次收到宿主事件递增，读取期间收到事件即令该次读取失效。 */
let eventRevision = 0

/** 读取序号：每次读取递增，只有最新启动且未被事件取代的读取可以发布结果。 */
let readSequence = 0

/** 已发布的会话快照或失败记录；快照未变化时保持同一对象引用。 */
let currentState: HostSessionState = UNAVAILABLE_STATE

/** React 外部存储订阅者：快照或失败记录变化都需要通知，以保证失败进入渲染期。 */
const stateListeners = new Set<() => void>()

/** 显式会话订阅者：只在取得可用会话快照时收到值。 */
const sessionListeners = new Set<(session: HostSession) => void>()

/** 共用的底层宿主订阅；没有消费者时为 null。 */
let hostUnsubscribe: (() => void) | null = null

/** 建立宿主订阅期间的重入保护：同步回放不得再建立第二条订阅。 */
let hostSubscribing = false

/** 同代次在途的初始化读取；成功与失败都在内部消费，不保留无处理的 `void readHostSession()`。 */
let initialRead: Promise<HostSession> | null = null

/** 逐字段比较，用于在快照未变化时跳过发布并保持对象引用稳定。 */
function sameSession(left: HostSession, right: HostSession): boolean {
  return left.status === right.status
    && left.sessionVersion === right.sessionVersion
    && left.userId === right.userId
    && left.account === right.account
    && left.displayName === right.displayName
    && left.organizationId === right.organizationId
    && left.hospitalId === right.hospitalId
    && left.campusId === right.campusId
}

/** 先更新命令式读取值，再通知订阅者：事件回调返回后现读即可取得新快照。 */
function publish(session: HostSession | null, failure: { value: unknown } | null): void {
  currentState = { session, failure }
  for (const listener of [...stateListeners]) listener()
  if (session !== null) {
    for (const listener of [...sessionListeners]) listener(session)
  }
}

/** 接受一份新的宿主快照；与当前值逐字段相同时不重复发布，失败记录同时被清除。 */
function applySession(session: HostSession): void {
  const unchanged = currentState.session !== null && sameSession(currentState.session, session)
  if (unchanged && currentState.failure === null) return
  publish(session, null)
}

/** 记录一次有效读取的失败：清空可用身份，通知订阅者，原失败值原样保留。 */
function applyFailure(value: unknown): void {
  publish(null, { value })
}

/** 宿主事件：先推进事件修订，再按既有映射发布快照。 */
function handleHostSnapshot(snapshot: AuthSessionSnapshot): void {
  eventRevision += 1
  applySession(toHostSession(snapshot))
}

/** 建立共用的底层宿主订阅；同步回放发生在订阅过程中也不会重复订阅。 */
function attachHostSubscription(): void {
  if (hostUnsubscribe === null && !hostSubscribing) {
    hostSubscribing = true
    try {
      hostUnsubscribe = MicroSDK.auth.subscribe(handleHostSnapshot)
    } finally {
      hostSubscribing = false
    }
  }
  startInitialReadIfNeeded()
}

/** 尚无快照且没有失败记录时发起一次初始化读取；同代次只保留一笔在途读取。 */
function startInitialReadIfNeeded(): void {
  if (initialRead !== null) return
  if (currentState.session !== null || currentState.failure !== null) return
  const read = readHostSession()
  initialRead = read
  void read.then(
    () => clearInitialRead(read),
    () => clearInitialRead(read),
  )
}

function clearInitialRead(read: Promise<HostSession>): void {
  if (initialRead === read) initialRead = null
}

/** 最后一个消费者释放时取消宿主订阅、推进代次并清空快照与失败记录。 */
function releaseHostSubscriptionIfIdle(): void {
  if (stateListeners.size > 0 || sessionListeners.size > 0) return
  const unsubscribe = hostUnsubscribe
  hostUnsubscribe = null
  if (unsubscribe !== null) unsubscribe()
  lifecycleGeneration += 1
  initialRead = null
  currentState = UNAVAILABLE_STATE
}

/** React 外部存储订阅：模块级稳定引用，多组件共用一条底层宿主订阅。 */
function subscribeToHostSessionState(listener: () => void): () => void {
  stateListeners.add(listener)
  attachHostSubscription()
  return () => {
    if (!stateListeners.delete(listener)) return
    releaseHostSubscriptionIfIdle()
  }
}

/** React 外部存储取值：模块级稳定引用。 */
function readHostSessionState(): HostSessionState {
  return currentState
}

/** 服务端与水合取值：与全部释放后的状态同一对象。 */
function getServerHostSessionState(): HostSessionState {
  return UNAVAILABLE_STATE
}

/** 命令式读取最近一次会话快照；尚未取得快照或最近一次读取失败时返回 null。 */
export function readCurrentHostSession(): HostSession | null {
  return currentState.session
}

/**
 * 读取当前宿主会话快照并映射为视图模型。
 *
 * 读取启动时记录生命周期代次、宿主事件修订与读取序号，结果只按这三个凭据判定是否仍然有效：
 * 期间收到宿主事件或出现更新的读取，旧结果即失效；生命周期代次变化表示已跨代次。
 * 失效读取在有有效快照的同代次返回当前快照，否则以模块自身的 `AbortError` 结束；
 * 跨代次一律取消，不返回新代次身份。有效读取的失败保留原始拒绝值并清空可用身份。
 */
export function readHostSession(): Promise<HostSession> {
  const ticket: HostSessionReadTicket = {
    generation: lifecycleGeneration,
    revision: eventRevision,
    sequence: (readSequence += 1),
  }
  return requestHostSnapshot().then(
    (snapshot) => acceptSnapshot(ticket, snapshot),
    (failure: unknown) => refuseRead(ticket, failure),
  )
}

/** 请求宿主快照；同步异常与异步拒绝进入同一个 Promise 失败流程。 */
function requestHostSnapshot(): Promise<AuthSessionSnapshot> {
  return new Promise<AuthSessionSnapshot>((resolve) => {
    resolve(MicroSDK.auth.getSession())
  })
}

function acceptSnapshot(ticket: HostSessionReadTicket, snapshot: AuthSessionSnapshot): HostSession {
  if (!isReadAccepted(ticket)) return settleStaleRead(ticket)
  const session = toHostSession(snapshot)
  applySession(session)
  return session
}

function refuseRead(ticket: HostSessionReadTicket, failure: unknown): HostSession {
  if (!isReadAccepted(ticket)) return settleStaleRead(ticket)
  applyFailure(failure)
  throw failure
}

function isReadAccepted(ticket: HostSessionReadTicket): boolean {
  return ticket.generation === lifecycleGeneration
    && ticket.revision === eventRevision
    && ticket.sequence === readSequence
}

/** 失效读取的收尾：同代次且已有有效快照时返回当前快照，否则以模块自身的取消结束。 */
function settleStaleRead(ticket: HostSessionReadTicket): HostSession {
  const sameLifecycle = ticket.generation === lifecycleGeneration
  const session = currentState.session
  if (sameLifecycle && session !== null) return session
  throw createLifecycleCancellation()
}

/** 本模块产生的取消对象集合：调用端据此判定取消来源，不按错误名称或消息识别。 */
const lifecycleCancellations = new WeakSet<object>()

/**
 * 模块自身判定的读取失效。
 *
 * 只有本函数产生的对象属于正常生命周期取消；SDK 原始错误即使同样命名为 AbortError
 * 也按真实失败保存并抛出，不按名称或消息吞掉。
 */
function createLifecycleCancellation(): DOMException {
  const cancellation = new DOMException('宿主会话读取已失效', 'AbortError')
  lifecycleCancellations.add(cancellation)
  return cancellation
}

/**
 * 判定一个拒绝值是否为本模块的生命周期取消。
 *
 * 判定依据是对象归属而非名称：SDK 自身抛出的 `AbortError` 不在此集合内，仍按真实失败处理。
 */
export function isLifecycleCancellation(value: unknown): boolean {
  return typeof value === 'object' && value !== null && lifecycleCancellations.has(value)
}

/**
 * 以外部存储方式订阅宿主会话快照，供不依赖 Provider 的页面直接读取。
 *
 * 会话唯一来源仍是宿主运行时：本钩子与 Provider 共用同一份快照与同一条底层宿主订阅。
 * 存在有效失败记录时直接抛出原始失败值，由渲染期错误处理暴露给调用方；订阅通知本身不抛出。
 */
export function useHostSessionSnapshot(): HostSession | null {
  const state = useSyncExternalStore(
    subscribeToHostSessionState,
    readHostSessionState,
    getServerHostSessionState,
  )
  if (state.failure !== null) throw state.failure.value
  return state.session
}

/**
 * 订阅宿主会话变化；返回原样取消订阅函数，由调用方在卸载时释放。
 *
 * 与其他消费入口共用同一条底层宿主订阅，回调只在取得可用快照时收到值。
 */
export function subscribeHostSession(listener: (session: HostSession) => void): () => void {
  sessionListeners.add(listener)
  attachHostSubscription()
  return () => {
    if (!sessionListeners.delete(listener)) return
    releaseHostSubscriptionIfIdle()
  }
}