import { act, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionSnapshot } from '@dy/micro-sdk'
import { HostSessionProvider } from './HostSessionProvider'
import { useHostSession } from './HostSessionContext'
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

  it('身份切换后迟到的初始会话不覆盖新身份', async () => {
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
