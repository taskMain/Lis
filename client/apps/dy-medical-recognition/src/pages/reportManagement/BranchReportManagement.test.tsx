/**
 * 阶段 4「本院报告管理与历史版本」医院管理员页 Component 层用例
 * （测试矩阵 C23、C24、C25、C26 的组件面）。
 *
 * 装配边界与平台管理员页用例一致（遵循 Frontend Testing 第 2 节）：
 * 1. `./reportsApi` 只替换 4 个 I/O 出口，其余纯函数使用真实实现，行夹具经真实适配层 `toReportPage` 归一；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client；
 * 3. 可信组织与可信医院两个接缝被替换为可控值，用于构造「可信范围就绪」与「可信范围不可用」两类场景。
 *    页面无范围选择器（院区恒取会话院区、不提供院区选择），测试不替换任何 Base 包。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、宿主统一错误提示与真实后端链路（C27–C31）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { useEffect } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { toReportPage } from './reportsApi'
import { clearRowSummaries } from './reportRowSummaryCache'
import type { TrustedHospitalScope } from './reportsTrustedScope'
import { BranchReportManagement } from './BranchReportManagement'

const api = vi.hoisted(() => ({
  queryReportPage: vi.fn(),
  queryBranchReportPage: vi.fn(),
  queryReportVersions: vi.fn(),
  queryReportVersionDetail: vi.fn(),
  downloadReportVersionPdf: vi.fn(),
}))

/** 可信范围接缝的可控值：本用例只覆盖本院页，因此只控制医院与院区两层。 */
const trustedScopeRef = vi.hoisted(() => ({
  hospital: { kind: 'ready', hospitalCode: 'HOS-1', branchCode: 'BRA-1' } as TrustedHospitalScope,
  organizationCode: 'ORG-A' as string | null,
}))

vi.mock('./reportsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./reportsApi')>()
  return { ...actual, ...api }
})

/** 页头范围条只读宿主会话：本文件不覆盖宿主链路，按匿名会话渲染「未知」且零业务请求。 */
vi.mock('../../session/HostSessionContext', () => ({
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

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => ({}),
}))

vi.mock('../recognitionProjects/recognitionProjectsOrganizationScope', () => ({
  useTrustedOrganizationScope: () => (trustedScopeRef.organizationCode === null
    ? { kind: 'unavailable' }
    : { kind: 'ready', organizationCode: trustedScopeRef.organizationCode }),
}))

vi.mock('./reportsTrustedScope', () => ({
  useTrustedHospitalScope: () => trustedScopeRef.hospital,
  readTrustedHospitalCode: () => null,
  readTrustedBranchCode: () => null,
}))

/** 列表读取出口的返回形状：页面分页模型，行经真实适配层归一。 */
const listResponse = (
  items: readonly Record<string, unknown>[],
  pageIndex: number,
  pageSize: number,
  totalCount: number,
) => toReportPage(items as never[], { pageIndex, pageSize, totalCount })

/** 一行列表读模型夹具。 */
function generatedRow(overrides: Record<string, unknown> = {}) {
  return {
    reportId: 'REP-1',
    organizationCode: 'ORG-A',
    hospitalCode: 'HOS-1',
    branchCode: 'BRA-1',
    organizationName: '组织一',
    hospitalName: '医院一',
    branchName: '院区一',
    reportType: 1,
    reportTypeText: '检验报告',
    reportNo: 'R-1',
    reportTime: '2026-09-18T01:02:00',
    currentVersionSequence: 1,
    patientName: '张三',
    identityDocumentNo: '110101199001011234',
    status: 1,
    statusText: '有效',
    ...overrides,
  }
}

/** 页面装配：与应用路由同构（详情子路由注册在列表路由之下，元素由入口容器条件渲染）。 */
function pageElement(initialPath = '/branch-report-management') {
  return (
    <ConfigProvider locale={zhCN}>
      <MemoryRouter initialEntries={[initialPath]}>
        <LocationProbe />
        <Routes>
          <Route path='/branch-report-management' element={<BranchReportManagement />}>
            <Route path='detail/:reportId' element={null} />
          </Route>
        </Routes>
      </MemoryRouter>
    </ConfigProvider>
  )
}

/** 路由观测点：受控观测真实 Router 交付的路径（断言 URL 行为，不刺探路由内部实现）。 */
const locationRef = { current: '' }

function LocationProbe() {
  const location = useLocation()
  useEffect(() => {
    locationRef.current = location.pathname
  }, [location])
  return null
}

function renderPage(initialPath = '/branch-report-management') {
  return render(pageElement(initialPath))
}

/** 手动控制完成时机的 Promise，用于断言加载中与失败态这类时序敏感行为。 */
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

/** 渲染页面并交付当页数据；返回渲染结果，供后续 `rerender` 构造可信范围变化。 */
async function renderLoaded(items = [generatedRow()], page = { totalCount: 1 }) {
  api.queryBranchReportPage.mockResolvedValue(listResponse(items, 1, 10, page.totalCount))
  const view = renderPage()
  await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalled())
  return view
}

beforeEach(() => {
  vi.clearAllMocks()
  trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }
  trustedScopeRef.organizationCode = 'ORG-A'
  api.queryReportVersions.mockResolvedValue([])
  api.queryReportVersionDetail.mockResolvedValue(null)
  api.downloadReportVersionPdf.mockResolvedValue(undefined)
  // 来源行概要缓存是模块级状态：每个用例从空缓存开始（深链口径依赖缓存未命中）。
  clearRowSummaries()
})

/** 本用例环境的范围条按匿名会话渲染：三级名称显示「未知」的既定口径。 */
const SCOPE_BAR_FALLBACK_TEXT = '未知 / 未知 / 未知'

describe('C23 医院管理员页可信范围（院区恒取登录院区）', () => {
  it('组织与医院固定展示，页面无院区选择器', async () => {
    await renderLoaded()

    // 页头范围区由共用范围条承载，且页头不出现任何组织、医院编码文本（页头范围条名称渲染口径）。
    expect(await screen.findByText(SCOPE_BAR_FALLBACK_TEXT)).toBeInTheDocument()
    expect(document.querySelector('.report-management-header')?.textContent).not.toContain('ORG-A')
    expect(document.querySelector('.report-management-header')?.textContent).not.toContain('HOS-1')
    // 院区恒取登录院区：页面不再提供院区选择，不渲染任何院区选择项。
    expect(screen.queryByRole('combobox', { name: '院区' })).not.toBeInTheDocument()
  })

  it('请求不提交组织与医院，只提交登录院区与筛选条件', async () => {
    await renderLoaded()

    const query = api.queryBranchReportPage.mock.calls[0]?.[1] as { branchCode?: string; organizationCode?: string; hospitalCode?: string }
    expect(query.branchCode).toBe('BRA-1')
    expect(query).not.toHaveProperty('organizationCode')
    expect(query).not.toHaveProperty('hospitalCode')
    // 平台管理员入口在本页不被使用。
    expect(api.queryReportPage).not.toHaveBeenCalled()
  })

  it('页头为左右结构：左侧标题与描述副标题，右侧插槽承载当前登录范围条', async () => {
    await renderLoaded()

    // 左侧：大标题与描述副标题。
    expect(screen.getByRole('heading', { name: '本院报告管理与历史版本', level: 3 })).toBeInTheDocument()
    expect(screen.getByText('查看本院报告与历史版本，并下载原始 PDF')).toBeInTheDocument()
    // 右侧：范围条由页头行直接承载（页头插槽），不嵌在标题块内，且按 DOM 顺序位于标题块之后；
    // 选择器未命中时各断言以 undefined/0 干净失败。实际视觉位置由宿主验收取证。
    const scopeBar = await screen.findByText(SCOPE_BAR_FALLBACK_TEXT)
    const header = document.querySelector('.report-management-header')
    const titleBlock = header?.firstElementChild ?? null
    expect(titleBlock?.contains(scopeBar)).toBe(false)
    expect(header?.contains(scopeBar)).toBe(true)
    expect(titleBlock?.compareDocumentPosition(scopeBar) ?? 0).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
  })

  it('可信组织或医院缺失时阻断页面且不发任何业务请求', async () => {
    trustedScopeRef.organizationCode = null
    renderPage()

    expect(await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveClass('ant-alert-error')
    // 阻断态标题与正常态同一层级（标题层级口径）；页头维持标题、描述副标题与错误 Alert（S4-D46）。
    expect(screen.getByRole('heading', { name: '本院报告管理与历史版本', level: 3 })).toBeInTheDocument()
    expect(screen.getByText('查看本院报告与历史版本，并下载原始 PDF')).toBeInTheDocument()
    // 阻断态不渲染院区选择项，也不发起列表查询。
    expect(screen.queryByRole('combobox', { name: '院区' })).not.toBeInTheDocument()
    expect(api.queryBranchReportPage).not.toHaveBeenCalled()
  })
})

describe('C24 医院管理员页登录院区与范围重置', () => {
  it('登录院区缺失属权限配置错误：零业务请求并以空态呈现', async () => {
    trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-1', branchCode: null }
    renderPage()

    // 适配层对空院区拒绝构造请求：页面不兜底、不伪造院区、不本地补默认，以空态呈现。
    expect(await screen.findByText('当前登录身份未携带院区，无法查询本院报告')).toBeInTheDocument()
    expect(api.queryBranchReportPage).not.toHaveBeenCalled()
  })

  it('院区恒取登录院区：登录院区变化即清空旧数据并整表重读', async () => {
    const first = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(first.promise)
    const view = renderPage()
    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(1))
    await act(async () => { first.resolve(listResponse([generatedRow()], 1, 10, 1)) })
    expect(await screen.findByText('R-1')).toBeInTheDocument()

    // 登录院区变化（宿主重新签发凭据）：范围键含院区，主体清空旧数据与未提交筛选并整表重读。
    const second = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(second.promise)
    trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-1', branchCode: 'BRA-2' }
    view.rerender(pageElement())

    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(2))
    const secondQuery = api.queryBranchReportPage.mock.calls[1]?.[1] as { branchCode?: string }
    expect(secondQuery.branchCode).toBe('BRA-2')
    // 范围切换先清空旧范围数据，旧行不再渲染。
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()

    await act(async () => { second.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    expect(await screen.findByText('R-2')).toBeInTheDocument()
  })

  it('可信医院变化即范围切换：清空未提交筛选与旧数据并整表重读（范围键含三层）', async () => {
    const view = await renderLoaded()
    const { fireEvent } = await import('@testing-library/react')
    // 只设置草稿，不提交：范围切换把草稿与旧数据一并清空。
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'R-9' } })

    const switched = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(switched.promise)
    // 可信医院变化而登录院区编码不变：范围键含三层（组织、医院、院区），同样整表重读。
    trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-2', branchCode: 'BRA-1' }
    view.rerender(pageElement())

    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(2))
    const switchedQuery = api.queryBranchReportPage.mock.calls[1]?.[1] as { branchCode?: string }
    expect(switchedQuery.branchCode).toBe('BRA-1')
    // 旧范围数据与未提交筛选一并清空。
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()
    expect(screen.getByLabelText('关键字筛选')).toHaveValue('')

    await act(async () => { switched.resolve(listResponse([generatedRow({ reportNo: 'R-3', hospitalCode: 'HOS-2' })], 1, 10, 1)) })
    expect(await screen.findByText('R-3')).toBeInTheDocument()
  })
})

describe('C25 医院管理员页列表与详情', () => {
  it('复用同一套列表主体，列与行按取值来源渲染', async () => {
    await renderLoaded()

    expect(screen.getByText('本院报告管理与历史版本')).toBeInTheDocument()
    expect(screen.getByText('R-1')).toBeInTheDocument()
    // 状态文案按表格范围断言：列表上沿状态页签也持有「有效」等页签文案，行内断言圈定在表格内。
    expect(within(screen.getByRole('table')).getByText('有效')).toBeInTheDocument()
    // 来源组织与医院名称由服务端回填后按行渲染；按表格范围断言行内归属名称。
    expect(screen.getByText('组织一')).toBeInTheDocument()
    expect(screen.getByText('医院一')).toBeInTheDocument()
    expect(within(screen.getByRole('table')).getByText('院区一')).toBeInTheDocument()
    expect(screen.getByText(/共 1 条/)).toBeInTheDocument()
  })
})

describe('C26 医院管理员页接口失败', () => {
  it('列表读取失败不渲染错误横幅：保留数据、结束加载、点查询可重试、无本地错误提示（读取失败形态：纯读取失败不渲染横幅）', async () => {
    await renderLoaded([generatedRow()])

    const failing = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(failing.promise)
    const { fireEvent } = await import('@testing-library/react')
    // 页头刷新按钮已移除：查询按钮是重读唯一入口（提交式筛选）。
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(2))

    const { act: actFn } = await import('@testing-library/react')
    await actFn(async () => { failing.reject(new Error('读取失败')) })

    // 既定口径：读取失败不渲染错误横幅（错误由宿主统一提示），已有数据保留。
    expect(screen.getByText('R-1')).toBeInTheDocument()
    expect(screen.queryByText('报告列表数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
    // 不弹本地错误提示，错误文案由宿主统一展示。
    expect(screen.queryByText(/接口错误|请求失败|网络错误/)).not.toBeInTheDocument()

    // 重试经查询按钮：重读成功后按新结果呈现。
    const retry = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(retry.promise)
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(3))
    await actFn(async () => { retry.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    await screen.findByText('R-2')
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
  })
})

describe('页面标题层级', () => {
  it('页面大标题为 level 3（正常态由共用主体渲染，阻断态断言见阻断用例）', async () => {
    await renderLoaded([generatedRow()])

    expect(screen.getByRole('heading', { name: '本院报告管理与历史版本', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '本院报告管理与历史版本', level: 2 })).toBeNull()
  })
})

/**
 * T02 医院入口详情路由（测试矩阵 C01「列表行「查看详情」进入详情路由」与 C12「深链首开」的医院入口同构面，
 * docs/plans/_temporary/T02-.../Client/testPlan.md；宿主同构验收见 C16，不在此覆盖）。
 */
describe('T02 医院入口详情路由（C01/C12 医院入口同构）', () => {
  it('点击查看详情跳转本院详情子路由：URL 变更、列表隐藏保持挂载、面包屑为本院入口', async () => {
    await renderLoaded()
    api.queryReportVersions.mockResolvedValueOnce([])

    fireEvent.click(screen.getByLabelText('查看报告详情：R-1'))

    await waitFor(() => expect(locationRef.current).toBe('/branch-report-management/detail/REP-1'))
    // 详情页呈现，面包屑父级为本院入口名称。
    expect(screen.getByLabelText('返回报告列表')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '本院报告管理' })).toBeInTheDocument()
    // 列表保持挂载且处于隐藏态。
    expect(screen.getByLabelText('关键字筛选')).not.toBeVisible()
  })

  it('深链直开返回：跳回本院列表父路由', async () => {
    api.queryReportVersions.mockResolvedValueOnce([])
    renderPage('/branch-report-management/detail/REP-1')

    fireEvent.click(await screen.findByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/branch-report-management'))
    expect(screen.getByLabelText('关键字筛选')).toBeVisible()
  })
})
