import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { HostSessionProvider } from './HostSessionProvider'
import { SessionUnavailableBoundary } from './SessionUnavailableBoundary'
import { readCurrentHostSession } from './hostSession'

/** MicroSDK.auth 替身：会话读取失败由各用例控制，不连接真实宿主。 */
const authMock = vi.hoisted(() => ({
  getSession: vi.fn(),
  subscribe: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({ MicroSDK: { auth: authMock } }))

/** 本文件用到的全局替身在该钩子里统一清理，避免跨用例泄漏。 */
function afterEachUnstub(): void {
  unstubHooks.push(() => { vi.unstubAllGlobals() })
}
const unstubHooks: Array<() => void> = []

afterEach(() => {
  while (unstubHooks.length > 0) unstubHooks.pop()?.()
  vi.restoreAllMocks()
})

beforeEach(() => {
  authMock.getSession.mockReset()
  authMock.subscribe.mockReset()
  authMock.subscribe.mockReturnValue(() => {})
  vi.restoreAllMocks()
})

describe('SessionUnavailableBoundary 会话不可用边界', () => {
  it('捕获子组件抛出的原始失败值，并进入不可操作状态', () => {
    const thrown = new Error('边界内抛出的原始失败')
    const caughtByRoot: unknown[] = []
    function Broken(): never {
      throw thrown
    }
    render(
      <SessionUnavailableBoundary>
        <Broken />
      </SessionUnavailableBoundary>,
      { onCaughtError: (error: unknown) => { caughtByRoot.push(error) } },
    )

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '重新载入' })).toBeInTheDocument()
    expect(caughtByRoot).toEqual([thrown])
  })

  it('会话读取被拒绝时页面进入不可操作状态，命令式身份为空，恢复入口触发整页重新载入', async () => {
    authMock.getSession.mockRejectedValue(new Error('宿主会话读取失败'))
    const reload = vi.fn()
    vi.stubGlobal('location', { ...window.location, reload })
    afterEachUnstub()

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <span data-testid='business-page'>业务页面</span>
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.queryByTestId('business-page')).not.toBeInTheDocument()
    expect(readCurrentHostSession()).toBeNull()

    screen.getByRole('button', { name: '重新载入' }).click()
    expect(reload).toHaveBeenCalledTimes(1)
  })

  it('会话读取同步抛出时同样进入不可操作状态，且不恢复旧身份', async () => {
    authMock.getSession.mockImplementation(() => {
      throw new Error('宿主会话读取同步失败')
    })

    render(
      <SessionUnavailableBoundary>
        <HostSessionProvider>
          <span data-testid='business-page'>业务页面</span>
        </HostSessionProvider>
      </SessionUnavailableBoundary>,
      { onCaughtError: () => {} },
    )

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(readCurrentHostSession()).toBeNull()
    expect(screen.queryByTestId('business-page')).not.toBeInTheDocument()
  })
})