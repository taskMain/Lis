import { Component, type ReactNode } from 'react'
import { act, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionSnapshot } from '@dy/micro-sdk'
import { HostSessionProvider } from './HostSessionProvider'
import { useHostSession } from './HostSessionContext'
import { readCurrentHostSession, useHostSessionSnapshot } from './hostSession'
import { CurrentAccount } from '../components/CurrentAccount'

/** MicroSDK.auth 替身：会话读取与订阅由各用例控制，不连接真实宿主。 */
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

/** 新工作身份快照：账号、版本与三层范围一起变化。 */
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

/** 会话字段探针：逐项输出视图模型，避免只断言账号而漏掉组织层级映射。 */
function SessionProbe() {
  const session = useHostSession()
  return (
    <dl>
      <dd data-testid='status'>{session.status}</dd>
      <dd data-testid='version'>{session.sessionVersion}</dd>
      <dd data-testid='organization'>{session.organizationId ?? ''}</dd>
      <dd data-testid='hospital'>{session.hospitalId ?? ''}</dd>
      <dd data-testid='campus'>{session.campusId ?? ''}</dd>
    </dl>
  )
}

/** 不经过 Provider 的直接快照消费者：与 Provider 内部使用同一份共享快照。 */
function SharedSnapshotProbe() {
  const session = useHostSessionSnapshot()
  const text = [
    session?.sessionVersion ?? 'unavailable',
    session?.account ?? '',
    session?.organizationId ?? '',
  ].join('|')
  return <span data-testid='shared-snapshot'>{text}</span>
}

/** 测试边界：只观测 Provider 在渲染期抛出的原始失败值，生产入口不新增边界。 */
function createSessionBoundary() {
  const caught: unknown[] = []
  class SessionBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
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
  return { SessionBoundary, caught }
}

function subscribeListener(): (session: AuthSessionSnapshot) => void {
  const listener = authMock.subscribe.mock.calls[0]?.[0]
  if (!listener) throw new Error('HostSessionProvider 未注册会话订阅')
  return listener as (session: AuthSessionSnapshot) => void
}

beforeEach(() => {
  authMock.getSession.mockReset()
  authMock.subscribe.mockReset()
  authMock.subscribe.mockReturnValue(() => {})
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('HostSessionProvider 宿主会话', () => {
  it('已认证时显示当前账号与显示名', async () => {
    authMock.getSession.mockResolvedValue(snapshot())

    render(
      <HostSessionProvider>
        <CurrentAccount />
      </HostSessionProvider>,
    )

    expect(await screen.findByText('当前账号：191617')).toBeInTheDocument()
    expect(screen.getByText('显示名：杨康健')).toBeInTheDocument()
  })

  it('会话快照的组织、医院与院区按工作身份映射', async () => {
    authMock.getSession.mockResolvedValue(snapshot())

    render(
      <HostSessionProvider>
        <SessionProbe />
      </HostSessionProvider>,
    )

    await waitFor(() => expect(screen.getByTestId('organization')).toHaveTextContent('org-1'))
    expect(screen.getByTestId('hospital')).toHaveTextContent('hospital-1')
    expect(screen.getByTestId('campus')).toHaveTextContent('campus-1')
    expect(screen.getByTestId('version')).toHaveTextContent('v1')
  })

  it('匿名状态不显示旧身份，也不发起业务请求', async () => {
    const fetchSpy = vi.fn()
    vi.stubGlobal('fetch', fetchSpy)
    authMock.getSession.mockResolvedValue(snapshot())

    render(
      <HostSessionProvider>
        <CurrentAccount />
      </HostSessionProvider>,
    )
    expect(await screen.findByText('当前账号：191617')).toBeInTheDocument()

    act(() => {
      subscribeListener()(
        snapshot({ status: 'anonymous', sessionVersion: 'v2', user: null, loginContext: null }),
      )
    })

    expect(screen.getByText('当前账号：未登录')).toBeInTheDocument()
    expect(screen.queryByText('显示名：杨康健')).not.toBeInTheDocument()
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('身份切换后迟到的初始会话不覆盖新身份，命令式读取同样不回到旧身份', async () => {
    let resolveInitial!: (value: AuthSessionSnapshot) => void
    authMock.getSession.mockReturnValue(new Promise<AuthSessionSnapshot>((resolve) => { resolveInitial = resolve }))

    render(
      <HostSessionProvider>
        <CurrentAccount />
      </HostSessionProvider>,
    )

    const listener = subscribeListener()
    act(() => {
      listener(snapshot({
        sessionVersion: 'v2',
        user: { id: 'user-2', account: 'lisadmin', displayName: '管理员', avatarUrl: null },
      }))
    })
    expect(screen.getByText('当前账号：lisadmin')).toBeInTheDocument()

    await act(async () => {
      resolveInitial(snapshot({ sessionVersion: 'v1' }))
      await Promise.resolve()
    })

    expect(screen.getByText('当前账号：lisadmin')).toBeInTheDocument()
    expect(screen.queryByText('当前账号：191617')).not.toBeInTheDocument()
    expect(readCurrentHostSession()).toMatchObject({ account: 'lisadmin', sessionVersion: 'v2' })
  })

  it('组件卸载时释放宿主会话订阅', async () => {
    const unsubscribe = vi.fn()
    authMock.subscribe.mockReturnValue(unsubscribe)
    authMock.getSession.mockResolvedValue(snapshot())

    const { unmount } = render(
      <HostSessionProvider>
        <CurrentAccount />
      </HostSessionProvider>,
    )
    expect(await screen.findByText('当前账号：191617')).toBeInTheDocument()

    unmount()

    expect(unsubscribe).toHaveBeenCalledTimes(1)
  })
})

describe('C01 Provider 与直接快照消费者共用一份快照', () => {
  it('Context 使用方与不经过 Provider 的消费者取值一致，只建立一条底层订阅', async () => {
    authMock.getSession.mockResolvedValue(snapshot())

    render(
      <HostSessionProvider>
        <SessionProbe />
        <SharedSnapshotProbe />
      </HostSessionProvider>,
    )

    await waitFor(() => expect(screen.getByTestId('organization')).toHaveTextContent('org-1'))
    expect(screen.getByTestId('shared-snapshot')).toHaveTextContent('v1|191617|org-1')
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(authMock.getSession).toHaveBeenCalledTimes(1)
    expect(readCurrentHostSession()).toMatchObject({ sessionVersion: 'v1', organizationId: 'org-1' })
  })

  it('Provider 外使用 useHostSession 立即失败', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    try {
      expect(() => render(<SessionProbe />)).toThrow('useHostSession must be used within HostSessionProvider')
    } finally {
      consoleError.mockRestore()
    }
  })
})

describe('C02 迟到初始读取在 Context 中的表现', () => {
  it('广播新身份后迟到的旧读取不改变 Context 的组织范围与命令式值', async () => {
    let resolveInitial!: (value: AuthSessionSnapshot) => void
    authMock.getSession.mockReturnValue(new Promise<AuthSessionSnapshot>((resolve) => { resolveInitial = resolve }))

    render(
      <HostSessionProvider>
        <SessionProbe />
      </HostSessionProvider>,
    )

    act(() => {
      subscribeListener()(workingIdentity('lisadmin', 'v2', 'org-2'))
    })
    expect(screen.getByTestId('organization')).toHaveTextContent('org-2')

    await act(async () => {
      resolveInitial(workingIdentity('191617', 'v1', 'org-1'))
      await Promise.resolve()
    })

    expect(screen.getByTestId('organization')).toHaveTextContent('org-2')
    expect(screen.getByTestId('version')).toHaveTextContent('v2')
    expect(readCurrentHostSession()).toMatchObject({ account: 'lisadmin', organizationId: 'org-2' })
  })
})

describe('C05 读取失败在 Context 与共享钩子中的表现', () => {
  it('有效读取失败时 Provider 使用方从渲染期捕获同一原始失败', async () => {
    const failure = new Error('宿主会话读取失败')
    authMock.getSession.mockRejectedValue(failure)
    const { SessionBoundary, caught } = createSessionBoundary()
    // React 捕获到的渲染失败由渲染选项的 onCaughtError 显式收集，控制台调用同时被记录并断言为零。
    const caughtByRoot: unknown[] = []
    const consoleCalls: unknown[][] = []
    const consoleError = vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      consoleCalls.push(args)
    })

    try {
      render(
        <SessionBoundary>
          <HostSessionProvider>
            <SessionProbe />
          </HostSessionProvider>
        </SessionBoundary>,
        { onCaughtError: (error: unknown) => { caughtByRoot.push(error) } },
      )
    } finally {
      consoleError.mockRestore()
    }

    await waitFor(() => expect(caught).toEqual([failure]))
    expect(caughtByRoot).toEqual([failure])
    expect(consoleCalls).toEqual([])
    expect(screen.getByTestId('boundary')).toHaveTextContent('failed')
    expect(readCurrentHostSession()).toBeNull()
  })
})
