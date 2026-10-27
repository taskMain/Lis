import { Component, StrictMode, type ReactNode } from 'react'
import { act, render, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionSnapshot } from '@dy/micro-sdk'
import type { HostSession } from './hostSession'

/** 会话模块类型：每个用例通过动态导入取得自己的实例。 */
type HostSessionModule = typeof import('./hostSession')

/**
 * 用例级隔离：先重置模块注册表再动态导入会话模块，
 * 使模块级生命周期代次、订阅集合、已接受快照与在途读取在每个用例开始时都从零开始。
 */
async function loadHostSession(): Promise<HostSessionModule> {
  vi.resetModules()
  return import('./hostSession')
}

/** 宿主 MicroSDK 替身：会话读取与订阅由各用例控制，不连接真实宿主。 */
const authMock = vi.hoisted(() => ({
  subscribe: vi.fn(),
  getSession: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({ MicroSDK: { auth: authMock } }))

/** 会话快照夹具：默认已认证身份，用例只覆盖需要变化的字段。 */
function snapshot(overrides: Partial<AuthSessionSnapshot> = {}): AuthSessionSnapshot {
  return {
    status: 'authenticated',
    sessionVersion: 'v1',
    user: { id: 'user-1', account: '191617', displayName: '杨康健', avatarUrl: null },
    loginContext: { organizationId: 'org-1', hospitalId: 'hospital-1', campusId: 'campus-1' },
    error: null,
    ...overrides,
  }
}

/** 工作身份快照：账号、会话版本与三层范围一起变化，用于区分新旧身份。 */
function workingIdentity(account: string, sessionVersion: string, organizationId: string): AuthSessionSnapshot {
  return snapshot({
    sessionVersion,
    user: { id: 'user-' + account, account, displayName: account, avatarUrl: null },
    loginContext: {
      organizationId,
      hospitalId: organizationId + '-hospital',
      campusId: organizationId + '-campus',
    },
  })
}

interface Deferred<T> {
  promise: Promise<T>
  resolve: (value: T) => void
  reject: (reason: unknown) => void
}

function deferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, resolve, reject }
}

/** 按调用顺序排队宿主读取，用于控制同一代次多笔读取的完成先后。 */
function queueHostReads(): Deferred<AuthSessionSnapshot>[] {
  const reads: Deferred<AuthSessionSnapshot>[] = []
  authMock.getSession.mockImplementation(() => {
    const read = deferred<AuthSessionSnapshot>()
    reads.push(read)
    return read.promise
  })
  return reads
}

/** 宿主事件回调：模块只保留一条底层订阅，这里取最后建立的那条。 */
function hostListener(): (value: AuthSessionSnapshot) => void {
  const listener = authMock.subscribe.mock.calls.at(-1)?.[0]
  if (typeof listener !== 'function') throw new Error('宿主会话模块未建立订阅')
  return listener as (value: AuthSessionSnapshot) => void
}

/** 直接消费共享快照的探针：与 Provider 内部使用同一钩子，钩子取自本用例加载的模块实例。 */
function createSnapshotProbe(hostSession: HostSessionModule) {
  return function SnapshotProbe() {
    const session = hostSession.useHostSessionSnapshot()
    const text = [
      session?.status ?? 'unavailable',
      session?.sessionVersion ?? '',
      session?.account ?? '',
      session?.organizationId ?? '',
    ].join('|')
    return <span data-testid='shared-snapshot'>{text}</span>
  }
}

/** 测试边界：只观测共享钩子在渲染期抛出的原始失败值，生产入口不新增边界。 */
function createSnapshotBoundary() {
  const caught: unknown[] = []
  class SnapshotBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
    state = { failed: false }
    static getDerivedStateFromError(): { failed: boolean } {
      return { failed: true }
    }
    componentDidCatch(failure: unknown): void {
      caught.push(failure)
    }
    render(): ReactNode {
      return this.state.failed ? <span data-testid='boundary'>failed</span> : this.props.children
    }
  }
  return { SnapshotBoundary, caught }
}

/**
 * 挂载测试边界并渲染探针。
 *
 * React 捕获到的渲染失败交给渲染选项的 onCaughtError 显式收集，默认的控制台渲染错误输出因此不再产生；
 * 控制台调用同时被记录并断言为零，用例不会把未接管的输出留给 stderr。
 */
function renderWithinBoundary(hostSession: HostSessionModule) {
  const { SnapshotBoundary, caught } = createSnapshotBoundary()
  const SnapshotProbe = createSnapshotProbe(hostSession)
  const caughtByRoot: unknown[] = []
  const consoleCalls: unknown[][] = []
  const consoleError = vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
    consoleCalls.push(args)
  })
  try {
    render(
      <SnapshotBoundary>
        <SnapshotProbe />
      </SnapshotBoundary>,
      { onCaughtError: (failure: unknown) => { caughtByRoot.push(failure) } },
    )
  } finally {
    consoleError.mockRestore()
  }

  return { caught, caughtByRoot, consoleCalls }
}

beforeEach(() => {
  authMock.subscribe.mockReset()
  authMock.getSession.mockReset()
  authMock.subscribe.mockReturnValue(() => {})
  authMock.getSession.mockResolvedValue(snapshot())
})

describe('useHostSessionSnapshot 订阅稳定性', () => {
  it('宿主回放快照与会话更新只触发一次订阅和一次重渲染', async () => {
    const hostSession = await loadHostSession()
    let subscribeCalls = 0
    const unsubscribe = vi.fn()
    authMock.subscribe.mockImplementation((listener: (value: AuthSessionSnapshot) => void) => {
      subscribeCalls += 1
      if (subscribeCalls > 3) throw new Error('session subscribe loop')
      listener(snapshot())
      return unsubscribe
    })

    let renders = 0
    const { result } = renderHook(() => {
      renders += 1
      return hostSession.useHostSessionSnapshot()
    })

    await waitFor(() => expect(result.current?.sessionVersion).toBe('v1'))
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(subscribeCalls).toBe(1)

    const listener = authMock.subscribe.mock.calls[0][0] as (value: AuthSessionSnapshot) => void
    renders = 0
    act(() => {
      listener(snapshot({ sessionVersion: 'v2' }))
    })

    expect(result.current?.sessionVersion).toBe('v2')
    expect(renders).toBe(1)
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(subscribeCalls).toBe(1)
    expect(unsubscribe).not.toHaveBeenCalled()
  })
})

describe('C01 初始化与同步回放', () => {
  it('无回放时初始化期间不给出伪造身份，首次读取完成后多种入口取值一致', async () => {
    const hostSession = await loadHostSession()
    const initial = deferred<AuthSessionSnapshot>()
    authMock.getSession.mockReturnValue(initial.promise)
    const published: HostSession[] = []
    const releaseSubscribe = hostSession.subscribeHostSession((session) => {
      published.push(session)
    })

    try {
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())

      expect(result.current).toBeNull()
      expect(hostSession.readCurrentHostSession()).toBeNull()

      await act(async () => {
        initial.resolve(snapshot())
        await initial.promise
      })

      expect(result.current?.account).toBe('191617')
      expect(result.current?.organizationId).toBe('org-1')
      expect(hostSession.readCurrentHostSession()).toBe(result.current)
      expect(published.at(-1)).toBe(result.current)
      expect(authMock.subscribe).toHaveBeenCalledTimes(1)
      expect(authMock.getSession).toHaveBeenCalledTimes(1)
    } finally {
      releaseSubscribe()
    }
  })

  it('宿主同步回放直接成为已接受快照，不额外读取也不重复订阅', async () => {
    const hostSession = await loadHostSession()
    authMock.subscribe.mockImplementation((listener: (value: AuthSessionSnapshot) => void) => {
      listener(snapshot())
      return () => {}
    })

    const { result } = renderHook(() => hostSession.useHostSessionSnapshot())

    await waitFor(() => expect(result.current?.account).toBe('191617'))
    expect(hostSession.readCurrentHostSession()).toBe(result.current)
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(authMock.getSession).not.toHaveBeenCalled()
  })
})

describe('C02 迟到初始读取', () => {
  it('旧身份读取在途时广播新身份，迟到的旧结果不覆盖共享快照与读取返回值', async () => {
    const hostSession = await loadHostSession()
    const initial = deferred<AuthSessionSnapshot>()
    authMock.getSession.mockReturnValue(initial.promise)
    const published: HostSession[] = []
    const releaseSubscribe = hostSession.subscribeHostSession((session) => {
      published.push(session)
    })

    try {
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
      const lateRead = hostSession.readHostSession()

      act(() => {
        hostListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
      })
      expect(hostSession.readCurrentHostSession()?.account).toBe('lisadmin')
      expect(result.current?.account).toBe('lisadmin')

      await act(async () => {
        initial.resolve(snapshot())
        await initial.promise
      })

      await expect(lateRead).resolves.toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      expect(published.at(-1)).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    } finally {
      releaseSubscribe()
    }
  })
})

describe('C03 多次读取顺序', () => {
  it('同代次多个消费者同时初始化只发起一次宿主读取', async () => {
    const hostSession = await loadHostSession()
    const reads = queueHostReads()
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const first = renderHook(() => hostSession.useHostSessionSnapshot())
      const second = renderHook(() => hostSession.useHostSessionSnapshot())

      expect(reads).toHaveLength(1)
      expect(authMock.subscribe).toHaveBeenCalledTimes(1)

      await act(async () => {
        reads[0].resolve(snapshot())
        await reads[0].promise
      })

      expect(first.result.current?.account).toBe('191617')
      expect(second.result.current?.account).toBe('191617')
      expect(authMock.getSession).toHaveBeenCalledTimes(1)
    } finally {
      releaseSubscribe()
    }
  })

  it('多笔读取反序完成时只接受最新启动的结果，不按 sessionVersion 字符串排新旧', async () => {
    const hostSession = await loadHostSession()
    const reads = queueHostReads()
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
      const staleRead = hostSession.readHostSession()
      const latestRead = hostSession.readHostSession()

      await act(async () => {
        reads[2].resolve(workingIdentity('lisadmin', 'v2', 'org-2'))
        await reads[2].promise
      })
      await act(async () => {
        reads[1].resolve(workingIdentity('191617', 'v10', 'org-1'))
        await reads[1].promise
      })
      await act(async () => {
        reads[0].resolve(workingIdentity('191617', 'v10', 'org-1'))
        await reads[0].promise
      })

      await expect(latestRead).resolves.toMatchObject({ account: 'lisadmin', sessionVersion: 'v2' })
      await expect(staleRead).resolves.toMatchObject({ account: 'lisadmin', sessionVersion: 'v2' })
      expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', sessionVersion: 'v2' })
      expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    } finally {
      releaseSubscribe()
    }
  })
})

describe('C04 多订阅者生命周期', () => {
  it('多个消费者共用一条底层订阅，单个卸载不影响其他消费者，最后卸载释放一次', async () => {
    const hostSession = await loadHostSession()
    const unsubscribes: ReturnType<typeof vi.fn>[] = []
    authMock.subscribe.mockImplementation(() => {
      const unsubscribe = vi.fn()
      unsubscribes.push(unsubscribe)
      return unsubscribe
    })

    const first = renderHook(() => hostSession.useHostSessionSnapshot())
    const second = renderHook(() => hostSession.useHostSessionSnapshot())
    const third = renderHook(() => hostSession.useHostSessionSnapshot())

    await waitFor(() => expect(third.result.current?.account).toBe('191617'))
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(authMock.getSession).toHaveBeenCalledTimes(1)

    first.unmount()
    expect(unsubscribes[0]).not.toHaveBeenCalled()
    expect(second.result.current?.account).toBe('191617')

    second.unmount()
    third.unmount()
    expect(unsubscribes).toHaveLength(1)
    expect(unsubscribes[0]).toHaveBeenCalledTimes(1)
  })

  it('重复相同快照不产生多余发布，已接受快照保持同一对象引用', async () => {
    const hostSession = await loadHostSession()
    const published: HostSession[] = []
    const releaseSubscribe = hostSession.subscribeHostSession((session) => {
      published.push(session)
    })

    try {
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
      await waitFor(() => expect(result.current?.account).toBe('191617'))

      const acceptedSession = result.current
      const publishCount = published.length

      act(() => {
        hostListener()(snapshot())
      })
      expect(published).toHaveLength(publishCount)
      expect(result.current).toBe(acceptedSession)
      expect(hostSession.readCurrentHostSession()).toBe(acceptedSession)

      act(() => {
        hostListener()(snapshot({
          sessionVersion: 'v2',
          loginContext: { organizationId: 'org-2', hospitalId: 'hospital-1', campusId: 'campus-1' },
        }))
      })
      expect(published).toHaveLength(publishCount + 1)
      expect(result.current?.organizationId).toBe('org-2')
    } finally {
      releaseSubscribe()
    }
  })
})

describe('C05 宿主状态映射', () => {
  it('refreshing 保留身份字段，anonymous、initializing 与 error 清除身份', async () => {
    const hostSession = await loadHostSession()
    const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
    await waitFor(() => expect(result.current?.account).toBe('191617'))

    act(() => {
      hostListener()(snapshot({ status: 'refreshing' }))
    })
    expect(result.current).toMatchObject({ status: 'refreshing', account: '191617', organizationId: 'org-1' })

    act(() => {
      hostListener()(snapshot({ status: 'anonymous', user: null, loginContext: null }))
    })
    expect(result.current).toMatchObject({ status: 'anonymous', account: null, hospitalId: null, campusId: null })

    act(() => {
      hostListener()(snapshot({ status: 'initializing', user: null, loginContext: null }))
    })
    expect(result.current).toMatchObject({ status: 'initializing', account: null, organizationId: null })

    act(() => {
      hostListener()(snapshot({ status: 'error', user: null, loginContext: null, error: null }))
    })
    expect(result.current).toMatchObject({ status: 'error', account: null, organizationId: null })
    expect(hostSession.readCurrentHostSession()?.status).toBe('error')
  })
})

describe('C05 有效读取失败', () => {
  it('同步异常进入同一失败流程：读取拒绝原值，命令式值为空，共享钩子抛出原值', async () => {
    const hostSession = await loadHostSession()
    const failure = new Error('宿主会话读取同步失败')
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      authMock.getSession.mockImplementation(() => {
        throw failure
      })

      const rejection = await hostSession.readHostSession().then(() => null, (reason: unknown) => reason)
      expect(rejection).toBe(failure)
      expect(hostSession.readCurrentHostSession()).toBeNull()

      const rendered = renderWithinBoundary(hostSession)
      expect(rendered.caught).toEqual([failure])
      expect(rendered.caughtByRoot).toEqual([failure])
      expect(rendered.consoleCalls).toEqual([])
    } finally {
      releaseSubscribe()
    }
  })

  it('异步拒绝保留原始失败值：命令式值为空，共享钩子抛出同一原值', async () => {
    const hostSession = await loadHostSession()
    const failure = new Error('宿主会话读取被拒绝')
    authMock.getSession.mockRejectedValue(failure)
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const rejection = await hostSession.readHostSession().then(() => null, (reason: unknown) => reason)
      expect(rejection).toBe(failure)
      expect(hostSession.readCurrentHostSession()).toBeNull()

      const rendered = renderWithinBoundary(hostSession)
      expect(rendered.caught).toEqual([failure])
      expect(rendered.caughtByRoot).toEqual([failure])
      expect(rendered.consoleCalls).toEqual([])
    } finally {
      releaseSubscribe()
    }
  })

  it('SDK 自身的 AbortError 属于真实失败，不按正常取消吞掉', async () => {
    const hostSession = await loadHostSession()
    const sdkAbort = new DOMException('宿主认证请求被中断', 'AbortError')
    authMock.getSession.mockRejectedValue(sdkAbort)
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const rejection = await hostSession.readHostSession().then(() => null, (reason: unknown) => reason)
      expect(rejection).toBe(sdkAbort)
      expect(hostSession.readCurrentHostSession()).toBeNull()

      const rendered = renderWithinBoundary(hostSession)
      expect(rendered.caught).toEqual([sdkAbort])
      expect(rendered.caughtByRoot).toEqual([sdkAbort])
      expect(rendered.consoleCalls).toEqual([])
    } finally {
      releaseSubscribe()
    }
  })

  it('广播新身份后旧请求的失败不清除新快照，也不写入当前代次', async () => {
    const hostSession = await loadHostSession()
    const reads = queueHostReads()
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
      const lateOutcome = hostSession.readHostSession().then(
        (session) => ({ kind: 'session' as const, session }),
        (reason: unknown) => ({ kind: 'cancelled' as const, reason }),
      )

      act(() => {
        hostListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
      })

      const failure = new Error('旧身份读取失败')
      await act(async () => {
        reads[1].reject(failure)
        await reads[1].promise.catch(() => {})
      })

      const outcome = await lateOutcome
      expect(outcome.kind).toBe('session')
      if (outcome.kind === 'session') expect(outcome.session).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })

      const remounted = renderHook(() => hostSession.useHostSessionSnapshot())
      expect(remounted.result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    } finally {
      releaseSubscribe()
    }
  })

  it('仍有订阅者时，有效宿主事件清除失败记录', async () => {
    const hostSession = await loadHostSession()
    const failure = new Error('宿主会话读取失败')
    authMock.getSession.mockRejectedValue(failure)
    const releaseSubscribe = hostSession.subscribeHostSession(() => {})

    try {
      const rejection = await hostSession.readHostSession().then(() => null, (reason: unknown) => reason)
      expect(rejection).toBe(failure)
      expect(hostSession.readCurrentHostSession()).toBeNull()

      act(() => {
        hostListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
      })

      expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
      const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
      expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    } finally {
      releaseSubscribe()
    }
  })
})

describe('C06 卸载、重新挂载与 StrictMode', () => {
  it('最后释放后命令式值为空，旧代次读取以模块自身 AbortError 结束，旧结果不进入新代次', async () => {
    const hostSession = await loadHostSession()
    const reads = queueHostReads()
    const { result, unmount } = renderHook(() => hostSession.useHostSessionSnapshot())

    await act(async () => {
      reads[0].resolve(workingIdentity('191617', 'v1', 'org-1'))
      await reads[0].promise
    })
    expect(result.current?.account).toBe('191617')

    const staleOutcome = hostSession.readHostSession().then(
      (session) => ({ kind: 'session' as const, session }),
      (reason: unknown) => ({ kind: 'cancelled' as const, reason }),
    )
    unmount()
    await act(async () => {
      reads[1].resolve(workingIdentity('191617', 'v1', 'org-1'))
      await reads[1].promise
    })

    expect(hostSession.readCurrentHostSession()).toBeNull()
    const outcome = await staleOutcome
    expect(outcome.kind).toBe('cancelled')
    if (outcome.kind === 'cancelled') {
      expect(hostSession.isLifecycleCancellation(outcome.reason)).toBe(true)
      expect(outcome.reason).toBeInstanceOf(DOMException)
      expect((outcome.reason as DOMException).name).toBe('AbortError')
      // 同名但非本模块产生的对象不属于生命周期取消。
      expect(hostSession.isLifecycleCancellation(new DOMException('宿主认证请求被中断', 'AbortError'))).toBe(false)
      expect(hostSession.isLifecycleCancellation(new Error('宿主会话读取失败'))).toBe(false)
    }

    const remounted = renderHook(() => hostSession.useHostSessionSnapshot())
    expect(remounted.result.current).toBeNull()

    await act(async () => {
      reads[2].resolve(workingIdentity('lisadmin', 'v2', 'org-2'))
      await reads[2].promise
    })
    expect(remounted.result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(authMock.subscribe).toHaveBeenCalledTimes(2)
  })

  it('内部初始化读取在释放后失效并被消费，不产生未处理拒绝', async () => {
    const hostSession = await loadHostSession()
    const reads = queueHostReads()
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => {
      unhandled.push(reason)
    }
    process.on('unhandledRejection', onUnhandledRejection)

    try {
      const { unmount } = renderHook(() => hostSession.useHostSessionSnapshot())
      unmount()

      await act(async () => {
        reads[0].resolve(workingIdentity('191617', 'v1', 'org-1'))
        await reads[0].promise
      })
      await act(async () => {})

      expect(unhandled).toEqual([])
      expect(hostSession.readCurrentHostSession()).toBeNull()
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
    }
  })

  it('StrictMode 的订阅与释放成对，旧代次初始化响应不进入新代次', async () => {
    const hostSession = await loadHostSession()
    const unsubscribes: ReturnType<typeof vi.fn>[] = []
    authMock.subscribe.mockImplementation(() => {
      const unsubscribe = vi.fn()
      unsubscribes.push(unsubscribe)
      return unsubscribe
    })
    const reads = queueHostReads()

    const { result, unmount } = renderHook(() => hostSession.useHostSessionSnapshot(), { wrapper: StrictMode })

    const latestRead = reads.at(-1)
    if (!latestRead) throw new Error('StrictMode 挂载后没有发起初始化读取')
    await act(async () => {
      latestRead.resolve(workingIdentity('lisadmin', 'v2', 'org-2'))
      await latestRead.promise
    })
    const staleReads = reads.filter((read) => read !== latestRead)
    for (const staleRead of staleReads) {
      await act(async () => {
        staleRead.resolve(workingIdentity('191617', 'v1', 'org-1'))
        await staleRead.promise
      })
    }

    expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })

    unmount()
    expect(unsubscribes).toHaveLength(authMock.subscribe.mock.calls.length)
    for (const unsubscribe of unsubscribes) expect(unsubscribe).toHaveBeenCalledTimes(1)
  })
})

describe('C07 页面与提交复核一致性', () => {
  it('旧渲染闭包保留时，宿主事件后命令式读取立即取得新身份与新组织范围', async () => {
    const hostSession = await loadHostSession()
    const { result } = renderHook(() => hostSession.useHostSessionSnapshot())
    await waitFor(() => expect(result.current?.account).toBe('191617'))
    const renderedClosure = result.current
    if (!renderedClosure) throw new Error('初次读取未产生共享快照')

    act(() => {
      hostListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
    })

    expect(renderedClosure.account).toBe('191617')
    expect(renderedClosure.organizationId).toBe('org-1')
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
  })
})
