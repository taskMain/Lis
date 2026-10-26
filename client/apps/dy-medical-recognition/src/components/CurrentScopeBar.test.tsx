/**
 * 页头当前范围条 CurrentScopeBar 的 Component 层用例（C3、C6：三级名称渲染、缺失与失败展示、身份切换重算）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. 本项目客户端只提供「当前组织范围」查询端点替身，替身交付与生成契约同形的读模型；
 * 2. 宿主会话替换为外部 store + `useSyncExternalStore`：`sessionVersion` 变化等价于身份或工作身份切换；
 * 3. 范围条只依赖会话与范围查询，不读取登录管理器、不直连外部目录。
 *
 * 视觉上的真实截断宽度属宿主浏览器验收面；本文件断言省略样式与 Tooltip 全文（jsdom 无布局）。
 */
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { HostSession } from '../session/hostSession'
import { CurrentScopeBar } from './CurrentScopeBar'

/** 匿名会话：未登录时不发业务请求。 */
const ANONYMOUS_SESSION: HostSession = {
  status: 'anonymous',
  sessionVersion: 'anonymous',
  userId: null,
  account: null,
  displayName: null,
  organizationId: null,
  hospitalId: null,
  campusId: null,
}

/** 已登录会话：只有会话键参与范围查询的重算。 */
function authenticatedSession(sessionVersion: string): HostSession {
  return {
    status: 'authenticated',
    sessionVersion,
    userId: 'user-1',
    account: '191617',
    displayName: '杨康健',
    organizationId: 'ORG-1',
    hospitalId: 'HOS-1',
    campusId: 'BRA-1',
  }
}

/** 宿主会话的外部 store：通知即代表宿主推进会话快照。 */
const sessionStore = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  return {
    current: null as unknown,
    set(next: unknown) {
      sessionStore.current = next
      for (const listener of [...listeners]) listener()
    },
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
  }
})

vi.mock('../session/HostSessionContext', async () => {
  const { useSyncExternalStore } = await import('react')
  return {
    useHostSession: () => {
      useSyncExternalStore(sessionStore.subscribe, () => sessionStore.current)
      return sessionStore.current
    },
  }
})

/** 本项目客户端注入点：承载「当前组织范围」查询端点替身。 */
const clientRef = vi.hoisted(() => ({ current: null as unknown }))

vi.mock('../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

/** 构造「当前组织范围」端点替身；返回 post 便于断言请求次数与迟到响应。 */
function givenCurrentScope(post: () => Promise<unknown>) {
  const query = vi.fn(post)
  clientRef.current = {
    api: { organizationScopeQuery: { queryCurrentOrganizationScope: { post: query } } },
  }
  return query
}

/** 与生成契约同形的当前范围读模型。 */
function scopeModel(organizationName: string | null, hospitalName: string | null, branchName: string | null) {
  return {
    organizationId: organizationName === null ? null : '11111111-1111-1111-1111-111111111111',
    organizationName,
    hospitalId: hospitalName === null ? null : '22222222-2222-2222-2222-222222222222',
    hospitalName,
    branchId: branchName === null ? null : '33333333-3333-3333-3333-333333333333',
    branchName,
  }
}

function renderBar() {
  return render(<ConfigProvider locale={zhCN}><CurrentScopeBar /></ConfigProvider>)
}

/** 悬停范围条后断言某条 Tooltip 展开了全文（照标准目录页用例的同一手法）。 */
async function expectTooltipShows(trigger: HTMLElement, fullText: string) {
  fireEvent.mouseOver(trigger)
  await waitFor(() => {
    const overlays = document.querySelectorAll('[role="tooltip"]')
    expect(Array.from(overlays).some((node) => node.textContent === fullText)).toBe(true)
  }, { timeout: 3000 })
}

beforeEach(() => {
  sessionStore.set(ANONYMOUS_SESSION)
  clientRef.current = null
})

describe('C3 页头范围条三级名称', () => {
  it('三级名称齐全时渲染单行「医共体名 / 医院名 / 院区名」，不含任何组织实例标识文本', async () => {
    sessionStore.set(authenticatedSession('v1'))
    const query = givenCurrentScope(async () => scopeModel('县医共体', '测试医院一', '测试院区1-1'))
    renderBar()

    expect(await screen.findByText('县医共体 / 测试医院一 / 测试院区1-1')).toBeInTheDocument()
    expect(query).toHaveBeenCalledTimes(1)
    const bar = document.querySelector('.current-scope-bar')?.textContent ?? ''
    expect(bar).not.toContain('11111111-1111-1111-1111-111111111111')
    expect(bar).not.toContain('22222222-2222-2222-2222-222222222222')
    expect(bar).not.toContain('33333333-3333-3333-3333-333333333333')
  })

  it('某级名称缺失时该级显示「未知」，其余层级正常', async () => {
    sessionStore.set(authenticatedSession('v2'))
    givenCurrentScope(async () => scopeModel('县医共体', '测试医院一', null))
    renderBar()

    expect(await screen.findByText('县医共体 / 测试医院一 / 未知')).toBeInTheDocument()
  })

  it('范围查询失败时三级显示「未知」，页面不自行渲染错误提示', async () => {
    sessionStore.set(authenticatedSession('v3'))
    givenCurrentScope(async () => { throw new Error('范围查询失败') })
    renderBar()

    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('超长名称单行省略并经 Tooltip 展示全文', async () => {
    const longOrganization = '超长组织名称'.repeat(12)
    sessionStore.set(authenticatedSession('v4'))
    givenCurrentScope(async () => scopeModel(longOrganization, '医院五', '院区五'))
    renderBar()

    const fullText = `${longOrganization} / 医院五 / 院区五`
    const bar = await screen.findByText(fullText)
    expect(bar.style.whiteSpace).toBe('nowrap')
    expect(bar.style.textOverflow).toBe('ellipsis')
    await expectTooltipShows(bar, fullText)
  })
})

describe('C6 身份或工作身份切换后的范围重算', () => {
  it('会话推进后按新会话重新查询并展示新范围名称', async () => {
    sessionStore.set(authenticatedSession('v5'))
    givenCurrentScope(async () => scopeModel('医共体甲', '医院甲', '院区甲'))
    renderBar()
    expect(await screen.findByText('医共体甲 / 医院甲 / 院区甲')).toBeInTheDocument()

    givenCurrentScope(async () => scopeModel('医共体乙', '医院乙', '院区乙'))
    await act(async () => {
      sessionStore.set(authenticatedSession('v6'))
    })

    expect(await screen.findByText('医共体乙 / 医院乙 / 院区乙')).toBeInTheDocument()
  })

  it('旧会话的迟到响应不覆盖新会话的范围名称', async () => {
    sessionStore.set(authenticatedSession('v7'))
    let resolveLate!: (value: unknown) => void
    const late = new Promise<unknown>((resolve) => { resolveLate = resolve })
    const query = givenCurrentScope(() => late)
    renderBar()
    await waitFor(() => expect(query).toHaveBeenCalledTimes(1))

    // 身份切换：新会话查询立即返回新名称，随后旧查询才返回旧名称。
    givenCurrentScope(async () => scopeModel('医共体新', '医院新', '院区新'))
    await act(async () => {
      sessionStore.set(authenticatedSession('v8'))
    })
    expect(await screen.findByText('医共体新 / 医院新 / 院区新')).toBeInTheDocument()

    await act(async () => {
      resolveLate(scopeModel('医共体旧', '医院旧', '院区旧'))
    })
    expect(screen.getByText('医共体新 / 医院新 / 院区新')).toBeInTheDocument()
    expect(screen.queryByText('医共体旧 / 医院旧 / 院区旧')).not.toBeInTheDocument()
  })

  it('未登录时不发起范围查询，三级均为「未知」', async () => {
    sessionStore.set(ANONYMOUS_SESSION)
    const query = givenCurrentScope(async () => scopeModel('不应出现', '不应出现', '不应出现'))
    renderBar()

    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(query).not.toHaveBeenCalled()
  })
})
