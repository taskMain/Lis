/**
 * 路由接线的回归：`recognition-projects`、`recognition-amounts` 与 `branch-recognition-amounts`
 * 必须挂在子应用根路由下，与既有 `standard-catalog` 同级，且原型路由保持
 * `import.meta.env.DEV` 条件写法不变。
 *
 * 页面组件的能力与宿主菜单进入仍属宿主验收；本文件只验证这些路径确实能挂载到页面组件，
 * 因此在这里以最小替身提供页面依赖的 API Client 上下文、范围选择器与可信上下文来源。
 * 本文件是 `.ts`（无 JSX 语法），渲染调用用 `createElement` 表达。
 */
import { createElement } from 'react'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, useRoutes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { routes } from './routes'

/** 稳定的占位 client：平台参数页挂载时会读取列表，空列表按「未配置」呈现，不制造读取失败。 */
const clientRef = vi.hoisted(() => ({
  current: { api: { v1: { platformParameters: { get: async () => [] } } } } as unknown,
}))

vi.mock('../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  // 路由用例只关心页面是否被挂载：占位 client 只提供平台参数列表读取。
  useApiClientContext: () => clientRef.current,
}))

/** 范围选择器替换成最小替身：路由用例只验证页面挂载，不验证选择器行为与范围查询。 */
vi.mock('../components/OrganizationScopeSelector', () => ({
  OrganizationScopeSelector: () => createElement('div', { className: 'stub-scope-selector' }),
}))

/** 页头范围条只读宿主会话：按匿名会话渲染，页面挂载不需要真实认证运行时。 */
vi.mock('../session/HostSessionContext', () => ({
  useHostSession: () => ({
    status: 'anonymous',
    sessionVersion: '',
    userId: null,
    account: null,
    displayName: null,
    organizationId: null,
    hospitalId: null,
    campusId: null,
  }),
}))

/** 可信范围接缝只读宿主会话：路由用例给固定已认证会话，页面按正常路径挂载。 */
vi.mock('../session/hostSession', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../session/hostSession')>()
  const session = {
    status: 'authenticated',
    sessionVersion: 'v1',
    userId: 'user-1',
    account: '191617',
    displayName: '杨康健',
    organizationId: 'ORG-A',
    hospitalId: 'HOS-1',
    campusId: 'BRA-1',
  }
  return {
    ...actual,
    readCurrentHostSession: () => session,
    useHostSessionSnapshot: () => session,
  }
})

const children = routes[0].children ?? []
const paths = children.map((route) => route.path)

/** 按真实路由表渲染给定路径。 */
function renderPath(path: string) {
  const RouteView = () => useRoutes(routes)
  return render(createElement(MemoryRouter, { initialEntries: [path] }, createElement(RouteView)))
}

describe('子应用路由表', () => {
  it('保留既有首页与标准目录路由，并登记两个金额页路由', () => {
    expect(children.some((route) => route.index === true)).toBe(true)
    expect(paths).toEqual(expect.arrayContaining([
      'standard-catalog',
      'platform-parameters',
      'recognition-projects',
      'recognition-amounts',
      'branch-recognition-amounts',
    ]))
  })

  it('在子应用路径 platform-parameters 下挂载平台参数维护页面（宿主路径 /subApps/medical-recognition/platform-parameters）', async () => {
    renderPath('/platform-parameters')

    expect(await screen.findByRole('heading', { name: '平台参数维护' })).toBeInTheDocument()
    expect(document.querySelector('.platform-parameters-page')).not.toBeNull()
  })

  it('在子应用路径 recognition-projects 下挂载互认项目页面（宿主路径 /subApps/medical-recognition/recognition-projects）', async () => {
    renderPath('/recognition-projects')

    // 页面组件被真正挂载，而不是路由表里有一个非空的 element。
    expect(await screen.findByRole('heading', { name: '互认项目' })).toBeInTheDocument()
    expect(document.querySelector('.recognition-projects-page')).not.toBeNull()
  })

  it('在子应用路径 recognition-amounts 下挂载互认项目金额维护页面（宿主路径 /subApps/medical-recognition/recognition-amounts）', async () => {
    renderPath('/recognition-amounts')

    expect(await screen.findByRole('heading', { name: '互认项目金额维护' })).toBeInTheDocument()
    expect(document.querySelector('.recognition-amounts-page')).not.toBeNull()
  })

  it('在子应用路径 branch-recognition-amounts 下挂载本院区互认项目金额页面（宿主路径 /subApps/medical-recognition/branch-recognition-amounts）', async () => {
    renderPath('/branch-recognition-amounts')

    expect(await screen.findByRole('heading', { name: '本院区互认项目金额' })).toBeInTheDocument()
    expect(document.querySelector('.recognition-amounts-page')).not.toBeNull()
  })

  it('原型路由只在 DEV 下注册，正规路径不被同名路由占用', () => {
    const prototypePaths = paths.filter((path) => typeof path === 'string' && path.startsWith('prototype/'))
    // 正式构建下 `Prototype` 为 null，原型路由整体不出现在路由表里。
    expect(prototypePaths).toEqual(import.meta.env.DEV ? ['prototype/standard-catalog'] : [])
    expect(paths).not.toContain('recognition-projects/')
    expect(paths).not.toContain('recognition-amounts/')
    expect(paths).not.toContain('branch-recognition-amounts/')
  })

  it('登记四个统计页路由，与既有页面路由同级', () => {
    expect(paths).toEqual(expect.arrayContaining([
      'recognition-usage-statistics',
      'branch-recognition-usage-statistics',
      'source-recognition-statistics',
      'branch-source-recognition-statistics',
    ]))
  })

  it('在子应用路径 recognition-usage-statistics 下挂载使用方互认使用统计页面（宿主路径 /subApps/medical-recognition/recognition-usage-statistics）', async () => {
    renderPath('/recognition-usage-statistics')

    expect(await screen.findByRole('heading', { name: '使用方互认使用统计' })).toBeInTheDocument()
    expect(document.querySelector('.statistics-board')).not.toBeNull()
  })

  it('在子应用路径 branch-recognition-usage-statistics 下挂载本院互认使用统计页面（宿主路径 /subApps/medical-recognition/branch-recognition-usage-statistics）', async () => {
    renderPath('/branch-recognition-usage-statistics')

    expect(await screen.findByRole('heading', { name: '本院互认使用统计' })).toBeInTheDocument()
    expect(document.querySelector('.statistics-board')).not.toBeNull()
  })

  it('在子应用路径 source-recognition-statistics 下挂载提供方医院被互认统计页面（宿主路径 /subApps/medical-recognition/source-recognition-statistics）', async () => {
    renderPath('/source-recognition-statistics')

    expect(await screen.findByRole('heading', { name: '提供方医院被互认统计' })).toBeInTheDocument()
    expect(document.querySelector('.statistics-board')).not.toBeNull()
  })

  it('在子应用路径 branch-source-recognition-statistics 下挂载本院被互认统计页面（宿主路径 /subApps/medical-recognition/branch-source-recognition-statistics）', async () => {
    renderPath('/branch-source-recognition-statistics')

    expect(await screen.findByRole('heading', { name: '本院被互认统计' })).toBeInTheDocument()
    expect(document.querySelector('.statistics-board')).not.toBeNull()
  })
})
