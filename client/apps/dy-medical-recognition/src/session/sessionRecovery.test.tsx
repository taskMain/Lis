import { act, cleanup, render, renderHook, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionSnapshot } from '@dy/micro-sdk'

/**
 * 会话失败后的恢复验证：按真实组件包含关系（失败边界在上、Provider 在下）与真实会话生命周期。
 *
 * 证据边界：测试环境不执行整页重载，因此把恢复入口的实际效果展开为「释放旧树 + 用新的应用初始化
 * 重新挂载」，由后者证明重新读取当前身份、业务恢复与再次失败仍不可操作；reload() 只被断言为
 * 恢复入口的动作，不当作完整恢复证据。
 */

const authMock = vi.hoisted(() => ({
  getSession: vi.fn(),
  subscribe: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({ MicroSDK: { auth: authMock } }))

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
  const promise = new Promise<T>((ok, fail) => {
    resolve = ok
    reject = fail
  })
  return { promise, resolve, reject }
}

async function loadAppModules() {
  vi.resetModules()
  const hostSession = await import('./hostSession')
  const { HostSessionProvider } = await import('./HostSessionProvider')
  const { SessionUnavailableBoundary } = await import('./SessionUnavailableBoundary')
  return { hostSession, HostSessionProvider, SessionUnavailableBoundary }
}

/** 最近一次注册的宿主订阅回调：用于广播宿主身份事件。 */
function hostListener(): (snapshot: AuthSessionSnapshot) => void {
  const listener = authMock.subscribe.mock.calls[0]?.[0]
  if (!listener) throw new Error('会话模块未注册宿主订阅')
  return listener as (snapshot: AuthSessionSnapshot) => void
}

function BusinessPage() {
  return <span data-testid='business-page'>业务页面</span>
}

afterEach(() => {
  vi.restoreAllMocks()
})

beforeEach(() => {
  authMock.getSession.mockReset()
  authMock.subscribe.mockReset()
  authMock.subscribe.mockReturnValue(() => {})
})

describe('会话失败后的恢复', () => {
  it('已有身份后发生有效读取失败：旧身份被清除，业务操作停止', async () => {
    const { hostSession, HostSessionProvider, SessionUnavailableBoundary } = await loadAppModules()
    const reads = [deferred<AuthSessionSnapshot>(), deferred<AuthSessionSnapshot>()]
    authMock.getSession.mockReturnValueOnce(reads[0].promise).mockReturnValueOnce(reads[1].promise)

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )

    await act(async () => {
      reads[0].resolve(workingIdentity('191617', 'v1', 'org-1'))
      await reads[0].promise
    })
    await waitFor(() => expect(screen.getByTestId('business-page')).toBeInTheDocument())
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: '191617', organizationId: 'org-1' })

    // 有效读取失败：来自共享模块自身的读取，不是宿主事件。
    const failure = new Error('宿主会话读取失败')
    const failedRead = hostSession.readHostSession().catch((reason: unknown) => reason)
    await act(async () => {
      reads[1].reject(failure)
      await failedRead
    })

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(hostSession.readCurrentHostSession()).toBeNull()
    expect(screen.queryByTestId('business-page')).not.toBeInTheDocument()
  })

  it('通过恢复入口重新初始化：读取并使用当时的当前身份，业务操作恢复', async () => {
    const { hostSession, HostSessionProvider, SessionUnavailableBoundary } = await loadAppModules()
    const failed = deferred<AuthSessionSnapshot>()
    const recovered = deferred<AuthSessionSnapshot>()
    authMock.getSession.mockReturnValueOnce(failed.promise).mockReturnValue(recovered.promise)

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )
    await act(async () => {
      failed.reject(new Error('宿主会话读取失败'))
      await failed.promise.catch(() => undefined)
    })
    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())

    // 恢复入口：释放旧树后由新的应用初始化重新读取。
    cleanup()
    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )
    expect(screen.getByTestId('business-page')).toBeInTheDocument()

    await act(async () => {
      recovered.resolve(workingIdentity('lisadmin', 'v2', 'org-2'))
      await recovered.promise
    })

    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByTestId('business-page')).toBeInTheDocument()
  })

  it('重新初始化再次失败：仍显示不可操作页面与恢复入口', async () => {
    const { HostSessionProvider, SessionUnavailableBoundary } = await loadAppModules()
    const failing = deferred<AuthSessionSnapshot>()
    // 立即挂处理器，避免自动初始化读取的拒绝成为未处理拒绝。
    const observed = failing.promise.catch(() => undefined)
    authMock.getSession.mockReturnValue(failing.promise)

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )
    await act(async () => {
      failing.reject(new Error('宿主会话读取失败'))
      await observed
    })
    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())

    cleanup()
    const caughtByRoot: unknown[] = []
    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: (error: unknown) => { caughtByRoot.push(error) } },
    )
    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(caughtByRoot).toHaveLength(1)
    expect(screen.getByRole('button', { name: '重新载入' })).toBeInTheDocument()
    expect(screen.queryByTestId('business-page')).not.toBeInTheDocument()
  })
  it('恢复过程中的旧读取不会恢复旧身份', async () => {
    const { hostSession, HostSessionProvider, SessionUnavailableBoundary } = await loadAppModules()
    const first = deferred<AuthSessionSnapshot>()
    const recovered = deferred<AuthSessionSnapshot>()
    authMock.getSession.mockReturnValueOnce(first.promise).mockReturnValue(recovered.promise)

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )
    await waitFor(() => expect(authMock.getSession).toHaveBeenCalledTimes(1))

    // 旧读取仍在途时释放旧树，再用新的应用初始化重新挂载。
    cleanup()
    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <BusinessPage />
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )
    await waitFor(() => expect(authMock.getSession).toHaveBeenCalledTimes(2))

    await act(async () => {
      recovered.resolve(workingIdentity('lisadmin', 'v2', 'org-2'))
      await recovered.promise
    })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })

    // 旧读取此时才返回旧身份：不得覆盖当前身份，也不得重新渲染出业务内容之外的状态。
    await act(async () => {
      first.resolve(workingIdentity('191617', 'v1', 'org-1'))
      await first.promise
    })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(screen.getByTestId('business-page')).toBeInTheDocument()
  })

  it('在途旧读取的修订已过期：不覆盖新身份，也不重新进入当前页面', async () => {
    const { hostSession } = await loadAppModules()
    const first = deferred<AuthSessionSnapshot>()
    const stale = deferred<AuthSessionSnapshot>()
    authMock.getSession.mockReturnValueOnce(first.promise).mockReturnValueOnce(stale.promise)

    const consumer = renderHook(() => hostSession.useHostSessionSnapshot())
    await act(async () => {
      first.resolve(workingIdentity('191617', 'v1', 'org-1'))
      await first.promise
    })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: '191617' })

    // 旧读取在途：它启动后宿主广播新身份，该读取的事件修订随即过期。
    const staleOutcome = hostSession.readHostSession().then(
      (session) => ({ kind: 'session' as const, session }),
      (reason: unknown) => ({ kind: 'cancelled' as const, reason }),
    )
    await act(async () => {
      hostListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
    })
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })

    // 过期读取此刻才返回旧身份：不得覆盖当前身份。
    await act(async () => {
      stale.resolve(workingIdentity('191617', 'v1', 'org-1'))
      await stale.promise
    })
    const settled = await staleOutcome
    expect(settled.kind).toBe('session')
    // 设计：失效读取在有有效快照的同代次返回当前快照，不返回旧身份。
    if (settled.kind === 'session') {
      expect(settled.session).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    }
    expect(hostSession.readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
    expect(consumer.result.current).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
  })
})