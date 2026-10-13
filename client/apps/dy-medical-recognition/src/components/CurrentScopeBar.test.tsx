/**
 * 页头当前范围条 CurrentScopeBar 的 Component 层用例（三级名称渲染、名称反查失败、随登录上下文取数的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `@dy/api-client-base` 只替换 `createBaseApiClient`，替身交付与生成契约同形的
 *    `HospitalDto` / `BranchDto`（含 id、name、hosId、orgId）；
 * 2. 本项目客户端只提供「可信范围查询」端点替身（组织名的取数来源），经 ApiClientContext 注入；
 * 3. `@dy/auth` / `@dy/auth-react` 替换为外部 store + `useSyncExternalStore`，token 变化等价于
 *    宿主重新签发 token（照互认项目页用例的同一构造）；
 * 4. `../runtimeConfig` 替换为即时完成的运行时配置，用例不触达配置文件读取。
 *
 * 模块级名称缓存按编码跨用例共享（组件卸载不丢缓存的真实语义），因此各用例使用互不重复的编码。
 * 视觉上的真实截断宽度属宿主浏览器验收面；本文件断言省略样式与 Tooltip 全文（jsdom 无布局）。
 */
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CurrentScopeBar } from './CurrentScopeBar'

/** 可信登录态（token + 三级编码）的外部 store；notify 等价于宿主重新签发 token。 */
const authState = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  const state = {
    token: 'token-1' as string | null,
    organizationCode: 'ORG-A' as string | null,
    hospitalCode: 'HOS-1' as string | null,
    branchCode: 'BRA-1' as string | null,
  }
  return {
    state,
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
    getVersion: () => versionRef.version,
    change(next: Partial<typeof state>) {
      Object.assign(state, next)
      versionRef.version += 1
      for (const listener of [...listeners]) listener()
    },
  }
})

/** 订阅快照：任何一次登录态通知都推进它，让订阅方重新求值。 */
const versionRef = vi.hoisted(() => ({ version: 0 }))

/** 本项目客户端注入点：承载「可信范围查询」端点替身。 */
const clientRef = vi.hoisted(() => ({ current: null as unknown }))

/** 基础目录替身 client 注入点：承载 getHospitalById / getBranchById 端点。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

vi.mock('@dy/auth', () => ({
  LoginUserManager: {
    getOrgID: () => authState.state.organizationCode,
    getHosID: () => authState.state.hospitalCode,
    getBranchID: () => authState.state.branchCode,
  },
}))

vi.mock('@dy/auth-react', async () => {
  const { useSyncExternalStore: useStore } = await import('react')
  return {
    /** 与真实包同形：订阅登录态，通知后重新求值；token 是页面判定可信范围的依据。 */
    useAuth: () => {
      useStore(authState.subscribe, authState.getVersion)
      return {
        isAuthenticated: authState.state.token !== null,
        token: authState.state.token,
        login: () => {},
        logout: () => {},
      }
    },
  }
})

vi.mock('../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

vi.mock('@dy/api-client-base', () => ({
  createBaseApiClient: () => baseClientRef.current,
}))

vi.mock('../runtimeConfig', () => ({
  loadRuntimeConfig: async () => ({ apiBaseUrl: 'http://api.test', baseApiBaseUrl: 'http://base.test' }),
}))

/** 构造「可信范围查询」端点替身：交付给定组织编码与名称。 */
function givenOrganizationName(organizationCode: string, organizationName: string | null) {
  clientRef.current = {
    api: {
      medicalRecognitionReportQuery: {
        queryTrustedOrganizationScope: { post: vi.fn(async () => ({ organizationCode, organizationName })) },
      },
    },
  }
}

interface DirectoryOptions {
  hospital?: { id: string; name: string | null; orgId: string }
  branch?: { id: string; name: string | null; hosId: string; orgId: string }
}

/** 构造基础目录替身：post 为 vi.fn，便于断言请求入参确实携带编码。 */
function givenDirectory(options: DirectoryOptions = {}) {
  const getHospitalById = vi.fn(async () => options.hospital)
  const getBranchById = vi.fn(async () => options.branch)
  baseClientRef.current = {
    api: { organization: { getHospitalById: { post: getHospitalById }, getBranchById: { post: getBranchById } } },
  }
  return { getHospitalById, getBranchById }
}

function renderBar() {
  return render(<ConfigProvider locale={zhCN}><CurrentScopeBar /></ConfigProvider>)
}

/** 悬停范围条后断言某条 Tooltip 展开了全文（照标准目录页用例的同一手法）。 */
async function expectTooltipShows(trigger: HTMLElement, fullText: string) {
  fireEvent.mouseOver(trigger)
  await waitFor(() => {
    // antd 6 的弹层结构：title 直接位于 role="tooltip" 的容器内。
    const overlays = document.querySelectorAll('[role="tooltip"]')
    expect(Array.from(overlays).some((node) => node.textContent === fullText)).toBe(true)
  }, { timeout: 3000 })
}

beforeEach(() => {
  authState.state.token = 'token-1'
  authState.state.organizationCode = 'ORG-A'
  authState.state.hospitalCode = 'HOS-1'
  authState.state.branchCode = 'BRA-1'
  clientRef.current = null
  baseClientRef.current = null
})

describe('页头范围条三级名称渲染', () => {
  it('三级名称齐全时渲染单行「组织名 / 医院名 / 院区名」，不含任何编码文本', async () => {
    givenOrganizationName('ORG-A', '组织甲')
    const { getHospitalById, getBranchById } = givenDirectory({
      hospital: { id: 'HOS-1', name: '医院一', orgId: 'ORG-A' },
      branch: { id: 'BRA-1', name: '院区一', hosId: 'HOS-1', orgId: 'ORG-A' },
    })
    renderBar()

    // 目录反查按编码发起：请求入参仍携带编码，编码只作查询参数不进页面文本。
    expect(await screen.findByText('组织甲 / 医院一 / 院区一')).toBeInTheDocument()
    expect(getHospitalById).toHaveBeenCalledWith({ id: 'HOS-1' })
    expect(getBranchById).toHaveBeenCalledWith({ id: 'BRA-1' })
    expect(document.querySelector('.current-scope-bar')?.textContent).not.toContain('ORG-A')
    expect(document.querySelector('.current-scope-bar')?.textContent).not.toContain('HOS-1')
    expect(document.querySelector('.current-scope-bar')?.textContent).not.toContain('BRA-1')
  })

  it('某级编码缺失时该级显示「未知」，且不发起该级目录反查', async () => {
    authState.state.organizationCode = 'ORG-2A'
    authState.state.hospitalCode = 'HOS-2A'
    authState.state.branchCode = null
    givenOrganizationName('ORG-2A', '组织乙')
    const { getBranchById } = givenDirectory({
      hospital: { id: 'HOS-2A', name: '医院二', orgId: 'ORG-2A' },
    })
    renderBar()

    expect(await screen.findByText('组织乙 / 医院二 / 未知')).toBeInTheDocument()
    expect(getBranchById).not.toHaveBeenCalled()
  })

  it('目录查无名称（空结果）的层级显示「未知」，不回退编码', async () => {
    authState.state.organizationCode = 'ORG-3A'
    authState.state.hospitalCode = 'HOS-3A'
    authState.state.branchCode = 'BRA-3A'
    givenOrganizationName('ORG-3A', '组织丙')
    givenDirectory({
      // 目录接口返回空（undefined）：按「未知」处理。
      hospital: undefined,
      branch: { id: 'BRA-3A', name: '院区三', hosId: 'HOS-3A', orgId: 'ORG-3A' },
    })
    renderBar()

    expect(await screen.findByText('组织丙 / 未知 / 院区三')).toBeInTheDocument()
    expect(screen.queryByText(/HOS-3A/)).not.toBeInTheDocument()
  })

  it('名称自带与编码一致的「（编码）」后缀时剔除后展示', async () => {
    authState.state.organizationCode = 'ORG-4A'
    authState.state.hospitalCode = '0101'
    authState.state.branchCode = '0101001'
    givenOrganizationName('ORG-4A', '组织四')
    givenDirectory({
      hospital: { id: '0101', name: '县人民医院（0101）', orgId: 'ORG-4A' },
      branch: { id: '0101001', name: '总院(0101001)', hosId: '0101', orgId: 'ORG-4A' },
    })
    renderBar()

    expect(await screen.findByText('组织四 / 县人民医院 / 总院')).toBeInTheDocument()
  })

  it('超长名称单行省略并经 Tooltip 展示全文', async () => {
    const longOrganization = '超长组织名称'.repeat(12)
    authState.state.organizationCode = 'ORG-5A'
    authState.state.hospitalCode = 'HOS-5A'
    authState.state.branchCode = 'BRA-5A'
    givenOrganizationName('ORG-5A', longOrganization)
    givenDirectory({
      hospital: { id: 'HOS-5A', name: '医院五', orgId: 'ORG-5A' },
      branch: { id: 'BRA-5A', name: '院区五', hosId: 'HOS-5A', orgId: 'ORG-5A' },
    })
    renderBar()

    const fullText = `${longOrganization} / 医院五 / 院区五`
    const bar = await screen.findByText(fullText)
    // 省略由单行样式承载（视觉宽度表现属宿主验收面），全文经 Tooltip 可读。
    expect(bar.style.whiteSpace).toBe('nowrap')
    expect(bar.style.textOverflow).toBe('ellipsis')
    await expectTooltipShows(bar, fullText)
  })
})

describe('名称反查失败', () => {
  it('目录反查 reject 的层级显示「未知」，不渲染错误提示，其余层级正常', async () => {
    authState.state.organizationCode = 'ORG-6A'
    authState.state.hospitalCode = 'HOS-6A'
    authState.state.branchCode = 'BRA-6A'
    givenOrganizationName('ORG-6A', '组织六')
    // 医院端点拒绝：错误由宿主统一提示，范围条只把该级显示为「未知」。
    const failing = givenDirectory({
      branch: { id: 'BRA-6A', name: '院区六', hosId: 'HOS-6A', orgId: 'ORG-6A' },
    })
    failing.getHospitalById.mockRejectedValue(new Error('目录接口失败'))
    renderBar()

    expect(await screen.findByText('组织六 / 未知 / 院区六')).toBeInTheDocument()
    // 页面不自行渲染错误：无错误横幅（alert）出现。
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('token 缺失时凭据不表达可信范围：三级均为「未知」且零反查', async () => {
    authState.state.token = null
    givenOrganizationName('ORG-7A', '组织七')
    const { getHospitalById, getBranchById } = givenDirectory()
    renderBar()

    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(getHospitalById).not.toHaveBeenCalled()
    expect(getBranchById).not.toHaveBeenCalled()
  })
})

describe('范围条随登录上下文（名称来自可信上下文与目录反查）', () => {
  it('token 重新签发且三级编码换绑后，范围条重新求值并展示新范围名称', async () => {
    authState.state.organizationCode = 'ORG-8A'
    authState.state.hospitalCode = 'HOS-8A'
    authState.state.branchCode = 'BRA-8A'
    givenOrganizationName('ORG-8A', '组织八')
    givenDirectory({
      hospital: { id: 'HOS-8A', name: '医院八', orgId: 'ORG-8A' },
      branch: { id: 'BRA-8A', name: '院区八', hosId: 'HOS-8A', orgId: 'ORG-8A' },
    })
    renderBar()
    expect(await screen.findByText('组织八 / 医院八 / 院区八')).toBeInTheDocument()

    // 宿主重新签发 token（三级编码全部换绑）：范围条按新编码重新反查。
    givenOrganizationName('ORG-8B', '组织九')
    givenDirectory({
      hospital: { id: 'HOS-8B', name: '医院九', orgId: 'ORG-8B' },
      branch: { id: 'BRA-8B', name: '院区九', hosId: 'HOS-8B', orgId: 'ORG-8B' },
    })
    await act(async () => {
      authState.change({ token: 'token-2', organizationCode: 'ORG-8B', hospitalCode: 'HOS-8B', branchCode: 'BRA-8B' })
    })

    expect(await screen.findByText('组织九 / 医院九 / 院区九')).toBeInTheDocument()
  })

  it('编码变化后旧层级响应迟到也不得覆盖新状态', async () => {
    authState.state.organizationCode = 'ORG-9A'
    authState.state.hospitalCode = 'HOS-9A'
    authState.state.branchCode = 'BRA-9A'
    givenOrganizationName('ORG-9A', '组织十')
    let resolveLateHospital!: (value: { id: string; name: string }) => void
    const lateHospital = new Promise<{ id: string; name: string }>((resolve) => { resolveLateHospital = resolve })
    const getHospitalById = vi.fn()
      .mockImplementationOnce(async () => lateHospital)
      .mockResolvedValue({ id: 'HOS-9B', name: '医院新' })
    baseClientRef.current = {
      api: {
        organization: {
          getHospitalById: { post: getHospitalById },
          getBranchById: { post: vi.fn(async () => ({ id: 'BRA-9A', name: '院区十', hosId: 'HOS-9A', orgId: 'ORG-9A' })) },
        },
      },
    }
    renderBar()
    await screen.findByText('组织十 / 未知 / 院区十')

    // 换绑到新医院并等新名称落地，再让旧医院的迟到响应返回。
    await act(async () => {
      authState.change({ token: 'token-2', hospitalCode: 'HOS-9B' })
    })
    expect(await screen.findByText('组织十 / 医院新 / 院区十')).toBeInTheDocument()
    await act(async () => {
      resolveLateHospital({ id: 'HOS-9A', name: '医院旧' })
    })

    // 迟到的旧名称被丢弃：范围条仍指向新范围。
    expect(await screen.findByText('组织十 / 医院新 / 院区十')).toBeInTheDocument()
  })
})
