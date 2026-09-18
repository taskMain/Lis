/**
 * 阶段 4「本院报告管理与历史版本」医院管理员页 Component 层用例
 * （测试矩阵 C23、C24、C25、C26 的组件面）。
 *
 * 装配边界与平台管理员页用例一致（遵循 Frontend Testing 第 2 节）：
 * 1. `./reportsApi` 只替换 4 个 I/O 出口，其余纯函数使用真实实现，行夹具经真实适配层 `toReportPage` 归一；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client；
 * 3. `@dy/components-base` 只替换整个包（理由同平台管理员页用例）；
 * 4. 可信组织与可信医院两个接缝被替换为可控值，用于构造「可信范围就绪」与「可信范围不可用」两类场景。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、真实范围选择器交互、宿主统一错误提示与真实后端链路（C27–C31）。
 */
import { render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import { toReportPage } from './reportsApi'
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

vi.mock('@dy/components-base', async () => {
  const { createElement } = await import('react')

  /** 范围选择器替身：只渲染院区一级，`orgId`/`hosId` 为固定值时对应级不渲染下拉。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, onChange }: BaseScopeSelectorProps) {
    return createElement('div', { className, 'data-testid': 'scope-selector' },
      createElement('span', { 'data-testid': 'fixed-org' }, orgId ?? ''),
      createElement('span', { 'data-testid': 'fixed-hos' }, hosId ?? ''),
      createElement('select', {
        'data-testid': 'branch-select',
        'aria-label': '院区',
        disabled,
        value: value.branchId ?? '',
        onChange: (event: { target: { value: string } }) => {
          const next = event.target.value
          onChange?.({ ...value, branchId: next.length > 0 ? next : undefined },
            { organizations: [], hospitals: [], branches: [], depts: [], users: [] })
        },
      },
        createElement('option', { value: '' }, ''),
        createElement('option', { value: 'BRA-1' }, '院区一'),
        createElement('option', { value: 'BRA-2' }, '院区二'),
      ),
    )
  }

  return {
    configureBaseComponents: vi.fn(),
    BaseScopeSelector: StubScopeSelector,
  }
})

vi.mock('./reportsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./reportsApi')>()
  return { ...actual, ...api }
})

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

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><BranchReportManagement /></ConfigProvider>)
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

/** 渲染页面并交付当页数据。 */
async function renderLoaded(items = [generatedRow()], page = { totalCount: 1 }) {
  api.queryBranchReportPage.mockResolvedValue(listResponse(items, 1, 10, page.totalCount))
  renderPage()
  await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalled())
}

beforeEach(() => {
  vi.clearAllMocks()
  trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }
  trustedScopeRef.organizationCode = 'ORG-A'
  api.queryReportVersions.mockResolvedValue([])
  api.queryReportVersionDetail.mockResolvedValue(null)
  api.downloadReportVersionPdf.mockResolvedValue(undefined)
})

describe('C23 医院管理员页可信范围', () => {
  it('组织与医院以只读方式固定展示，范围选择器只渲染院区一级', async () => {
    await renderLoaded()

    expect(screen.getByText(/可信范围（只读）：组织 ORG-A，医院 HOS-1/)).toBeInTheDocument()
    const selector = screen.getByTestId('scope-selector')
    expect(within(selector).getByTestId('fixed-org')).toHaveTextContent('ORG-A')
    expect(within(selector).getByTestId('fixed-hos')).toHaveTextContent('HOS-1')
    // 只渲染院区下拉，不渲染组织与医院的输入项。
    expect(within(selector).getAllByRole('combobox')).toHaveLength(1)
    expect(within(selector).getByTestId('branch-select')).toBeInTheDocument()
  })

  it('请求不提交组织与医院，只提交院区与筛选条件', async () => {
    await renderLoaded()

    const query = api.queryBranchReportPage.mock.calls[0]?.[1] as { branchCode?: string; organizationCode?: string; hospitalCode?: string }
    expect(query.branchCode).toBe('BRA-1')
    expect(query).not.toHaveProperty('organizationCode')
    expect(query).not.toHaveProperty('hospitalCode')
    // 平台管理员入口在本页不被使用。
    expect(api.queryReportPage).not.toHaveBeenCalled()
  })

  it('可信组织或医院缺失时阻断页面且不发任何业务请求', async () => {
    trustedScopeRef.organizationCode = null
    renderPage()

    expect(await screen.findByText('可信范围不可用')).toBeInTheDocument()
    // 阻断态不渲染院区选择项，也不发起列表查询。
    expect(screen.queryByTestId('scope-selector')).not.toBeInTheDocument()
    expect(api.queryBranchReportPage).not.toHaveBeenCalled()
  })
})

describe('C24 医院管理员页院区可选与范围重置', () => {
  it('院区是页面上唯一可选层级，未选院区时不发起列表查询', async () => {
    trustedScopeRef.hospital = { kind: 'ready', hospitalCode: 'HOS-1', branchCode: null }
    renderPage()

    // 可信院区缺失时留空、不默认选中，因此不构造请求。
    expect(await screen.findByText('请选择院区后查看报告')).toBeInTheDocument()
    expect(api.queryBranchReportPage).not.toHaveBeenCalled()
  })

  it('切换院区时服务端归属仍由后端判定，页面不做本地归属过滤', async () => {
    await renderLoaded()
    const selector = screen.getByTestId('scope-selector')
    const select = within(selector).getByTestId('branch-select')

    // 选择一个不在可信医院内的院区：页面只把它提交给服务端，不做本地拒绝。
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.change(select, { target: { value: 'BRA-2' } })

    await waitFor(() => {
      const lastQuery = api.queryBranchReportPage.mock.calls.at(-1)?.[1] as { branchCode?: string }
      expect(lastQuery.branchCode).toBe('BRA-2')
    })
  })
})

describe('C25 医院管理员页列表与详情', () => {
  it('复用同一套列表主体，列与行按取值来源渲染', async () => {
    await renderLoaded()

    expect(screen.getByText('本院报告管理与历史版本')).toBeInTheDocument()
    expect(screen.getByText('R-1')).toBeInTheDocument()
    expect(screen.getByText('检验报告')).toBeInTheDocument()
    expect(screen.getByText('有效')).toBeInTheDocument()
    // 来源组织与医院名称由服务端回填后按行渲染；院区名同时出现在范围选择器选项里，
    // 因此按表格范围断言，避免与选择器选项重名。
    expect(screen.getByText('组织一')).toBeInTheDocument()
    expect(screen.getByText('医院一')).toBeInTheDocument()
    expect(within(screen.getByRole('table')).getByText('院区一')).toBeInTheDocument()
    expect(screen.getByText(/共 1 条/)).toBeInTheDocument()
  })
})

describe('C26 医院管理员页接口失败', () => {
  it('列表读取失败时保留数据、结束加载、可重试、无本地错误提示', async () => {
    await renderLoaded([generatedRow()])

    const failing = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(failing.promise)
    const { fireEvent } = await import('@testing-library/react')
    fireEvent.click(screen.getByRole('button', { name: /重新读取报告列表/ }))
    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(2))

    const { act } = await import('@testing-library/react')
    await act(async () => { failing.reject(new Error('读取失败')) })

    // 失败态：给出待刷新提示与重试入口，已有数据保留。
    await screen.findByText('报告列表数据待刷新')
    expect(screen.getByText('R-1')).toBeInTheDocument()
    // 不弹本地错误提示，错误文案由宿主统一展示。
    expect(screen.queryByText(/接口错误|请求失败|网络错误/)).not.toBeInTheDocument()
    // 刷新入口在加载结束后恢复可用。
    await waitFor(() => expect(screen.getByRole('button', { name: /重新读取报告列表/ })).toBeEnabled())

    const retry = deferred<unknown>()
    api.queryBranchReportPage.mockReturnValueOnce(retry.promise)
    fireEvent.click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryBranchReportPage).toHaveBeenCalledTimes(3))
    await act(async () => { retry.resolve(listResponse([generatedRow()], 1, 10, 1)) })
    await waitFor(() => expect(screen.queryByText('报告列表数据待刷新')).not.toBeInTheDocument())
  })
})
