import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionSnapshot } from '@dy/micro-sdk'
import { useHostSessionSnapshot } from './hostSession'

/** 宿主 MicroSDK 替身：订阅时立即回放当前快照，模拟宿主运行时的初始订阅行为。 */
const authMock = vi.hoisted(() => ({
  subscribe: vi.fn(),
  getSession: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({ MicroSDK: { auth: authMock } }))

function snapshot(sessionVersion: string): AuthSessionSnapshot {
  return {
    status: 'authenticated',
    sessionVersion,
    user: { id: 'user-1', account: '191617', displayName: '杨康健', avatarUrl: null },
    loginContext: { organizationId: 'org-1', hospitalId: 'hospital-1', campusId: 'campus-1' },
    error: null,
  }
}

beforeEach(() => {
  authMock.subscribe.mockReset()
  authMock.getSession.mockReset()
  authMock.getSession.mockResolvedValue(snapshot('v1'))
})

describe('useHostSessionSnapshot 订阅稳定性', () => {
  it('宿主回放快照与会话更新只触发一次订阅和一次重渲染', async () => {
    let subscribeCalls = 0
    const unsubscribe = vi.fn()
    authMock.subscribe.mockImplementation((listener: (value: AuthSessionSnapshot) => void) => {
      subscribeCalls += 1
      if (subscribeCalls > 3) throw new Error('session subscribe loop')
      listener(snapshot('v1'))
      return unsubscribe
    })

    let renders = 0
    const { result } = renderHook(() => {
      renders += 1
      return useHostSessionSnapshot()
    })

    await waitFor(() => expect(result.current?.sessionVersion).toBe('v1'))
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(subscribeCalls).toBe(1)

    const listener = authMock.subscribe.mock.calls[0][0] as (value: AuthSessionSnapshot) => void
    renders = 0
    act(() => {
      listener(snapshot('v2'))
    })

    expect(result.current?.sessionVersion).toBe('v2')
    expect(renders).toBe(1)
    expect(authMock.subscribe).toHaveBeenCalledTimes(1)
    expect(subscribeCalls).toBe(1)
    expect(unsubscribe).not.toHaveBeenCalled()
  })
})
