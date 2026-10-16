/**
 * 阶段 4「报告管理与历史版本」平台管理员页 Component 层用例
 * （测试矩阵 C9、C10、C11、C12、C13、C14、C15、C16、C17、C18、C19、C20、C21、C22 的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./reportsApi` 只替换 I/O 出口（列表读取、版本列表读取、版本详情读取、PDF 下载），
 *    其余纯函数（请求构造、日期归一、读模型 → 视图模型映射与枚举文案兜底）**使用真实实现**：
 *    列表替身交付**生成端响应形态**（`items` 为生成端读模型 + `page` 分页信息），
 *    由真实适配层收敛为页面分页模型，因此分页收敛、日期归一与文本兜底仍由适配层决定，
 *    不是把页面最终状态直接喂给页面；本文件当前接线的出口为列表读取、版本列表读取与版本详情读取三类，
 *    版本详情读取已随详情路由页承接版本切换接线并消费；PDF 下载出口已接线（下载互斥与失败覆盖见工单 06）；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client——页面把 client 原样透传给适配层出口；
 * 3. `@dy/components-base` 只替换**整个包**：该包 ESM 产物内部使用无扩展名相对导入，
 *    vitest 默认把 node_modules 依赖交给 Node 解析，组件层无法加载真实实现
 *    （真实实现需把该包加入 `test.server.deps.inline`，而 `vite.config.ts` 不属于本票所有权）。
 *    替身只复现页面接线依赖的组件契约：受控 `value`、上游变化按可选项级联回填下游首项、
 *    变化经 `onChange` 回传完整范围值。真实选择器的行为属宿主层验收面（C27–C29 的宿主层级）。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、真实范围选择器交互、宿主统一错误提示
 * （接口失败时的业务拒绝文案由宿主展示）与真实后端链路（C27–C31）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import dayjs from 'dayjs'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { MemoryRouter, Route, Routes, useLocation, useNavigationType } from 'react-router-dom'
import { useEffect } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorChangeMeta, BaseScopeSelectorProps } from '@dy/components-base'
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import { toReportPage, toReportRows, toReportVersionDetail, toReportVersionRows, type ReportVersionRow } from './reportsApi'
import { clearRowSummaries, setRowSummary } from './reportRowSummaryCache'
import { ReportManagement } from './ReportManagement'

/** 伪 Base API Client 的注入点：与真实包 `configureBaseComponents` 同形（替身见文件头第 3 条）。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

const api = vi.hoisted(() => ({
  queryReportPage: vi.fn(),
  queryBranchReportPage: vi.fn(),
  queryReportVersions: vi.fn(),
  queryReportVersionDetail: vi.fn(),
  downloadReportVersionPdf: vi.fn(),
}))

vi.mock('@dy/components-base', async () => {
  const { createElement, useEffect, useRef, useState } = await import('react')

  // 范围选项直接采用 ChangeMeta 各层的元素类型：替身回传的 meta 不再需要双重断言。
  type ScopeOrganization = BaseScopeSelectorChangeMeta['organizations'][number]
  type ScopeHospital = BaseScopeSelectorChangeMeta['hospitals'][number]
  type ScopeBranch = BaseScopeSelectorChangeMeta['branches'][number]
  /** 范围选项的展示面（id + 名称）：三层 DTO 的公共子形，供下拉渲染。 */
  type ScopeOption = Pick<ScopeOrganization, 'id' | 'name'>
  interface ScopeClient {
    api: {
      organization: {
        queryAllOrganization: { post: () => Promise<ScopeOrganization[] | undefined> }
        queryAllValidHospitalByOrgId: { post: (body: { orgId: string }) => Promise<ScopeHospital[] | undefined> }
        queryAllValidBranchByOrgId: { post: (body: { orgId: string }) => Promise<ScopeBranch[] | undefined> }
      }
    }
  }

  /** 范围选择器替身：受控值、级联回填与 `onChange` 回传完整范围值（与真实组件同形）。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, selectFirstOption, onChange }: BaseScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOrganization[]>([])
    const [hospitals, setHospitals] = useState<ScopeHospital[]>([])
    const [branches, setBranches] = useState<ScopeBranch[]>([])

    const loadedScopeKeyRef = useRef('')
    useEffect(() => {
      const loadKey = `${orgId ?? ''}|${hosId ?? ''}|${value.orgId ?? ''}|${value.hosId ?? ''}`
      if (loadedScopeKeyRef.current === loadKey) return
      loadedScopeKeyRef.current = loadKey
      const client = baseClientRef.current as ScopeClient
      /**
       * 初始装载完成后按真实组件的 `selectFirstOption` 口径回调：受控值里仍在清单中的层保持不变，
       * 不在清单中的层取该层第一项；列表为空时不回调（无首项可交付）。
       */
      const deliver = (
        organizations: ScopeOrganization[],
        hospitals: ScopeHospital[],
        branches: ScopeBranch[],
      ) => {
        if (!selectFirstOption) return
        const nextOrg = value.orgId && organizations.some((item) => item.id === value.orgId) ? value.orgId : organizations[0]?.id
        const nextHos = value.hosId && hospitals.some((item) => item.id === value.hosId) ? value.hosId : hospitals[0]?.id
        const nextBranch = value.branchId && branches.some((item) => item.id === value.branchId) ? value.branchId : branches[0]?.id
        if (!nextOrg || !nextHos || !nextBranch) return
        onChange?.(
          { orgId: nextOrg, hosId: nextHos, branchId: nextBranch },
          {
            organizations,
            hospitals,
            branches,
            depts: [],
            users: [],
          },
        )
      }
      const load = async () => {
        if (!orgId) {
          const organizations = await client.api.organization.queryAllOrganization.post() ?? []
          setOrganizations(organizations)
          const firstOrg = value.orgId && organizations.some((item) => item.id === value.orgId) ? value.orgId : organizations[0]?.id
          if (!firstOrg) return
          const hospitals = await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId: firstOrg }) ?? []
          setHospitals(hospitals)
          const targetHospital = value.hosId && hospitals.some((item) => item.id === value.hosId) ? value.hosId : hospitals[0]?.id
          const allBranches = await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: firstOrg }) ?? []
          const branches = allBranches.filter((branch) => branch.hosId === targetHospital)
          setBranches(branches)
          deliver(organizations, hospitals, branches)
          return
        }
        const targetHospital = hosId ?? value.hosId
        const hospitals = await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId }) ?? []
        const allBranches = await client.api.organization.queryAllValidBranchByOrgId.post({ orgId }) ?? []
        const branches = allBranches.filter((branch) => branch.hosId === targetHospital)
        setHospitals(hospitals)
        setBranches(branches)
        deliver([], hospitals, branches)
      }
      void load()
    }, [hosId, onChange, orgId, selectFirstOption, value.branchId, value.hosId, value.orgId])

    const handleChange = async (changed: 'org' | 'hospital' | 'branch', next: string) => {
      const client = baseClientRef.current as ScopeClient
      const merged = { ...value }
      if (changed === 'org') {
        merged.orgId = next.length > 0 ? next : undefined
        const nextHospitals = merged.orgId
          ? await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId: merged.orgId }) ?? []
          : []
        setHospitals(nextHospitals)
        merged.hosId = hosId ?? nextHospitals[0]?.id ?? undefined
        const allBranches = merged.orgId
          ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
          : []
        const nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
        setBranches(nextBranches)
        merged.branchId = nextBranches[0]?.id ?? undefined
      } else if (changed === 'hospital') {
        merged.hosId = next.length > 0 ? next : undefined
        const allBranches = merged.orgId
          ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
          : []
        const nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
        setBranches(nextBranches)
        merged.branchId = nextBranches[0]?.id ?? undefined
      } else {
        merged.branchId = next.length > 0 ? next : undefined
      }
      onChange?.(merged, { organizations: [], hospitals: [], branches: [], depts: [], users: [] })
    }

    const levelSelect = (
      label: string,
      options: ScopeOption[],
      selected: string | undefined,
      selectDisabled: boolean,
      onSelect: (next: string) => void,
    ) => createElement('select', {
      'aria-label': `范围-${label}`,
      value: selected ?? '',
      disabled: disabled === true || selectDisabled,
      onChange: (event: { target: { value: string } }) => onSelect(event.target.value),
    }, [
      createElement('option', { key: '__placeholder', value: '' }, `请选择${label}`),
      ...options.map((option, index) => createElement(
        'option',
        { key: option.id ?? index, value: option.id ?? '' },
        option.name ?? option.id ?? '',
      )),
    ])

    return createElement(
      'div',
      { className },
      orgId ? null : levelSelect('组织', organizations, value.orgId, false, (next) => { void handleChange('org', next) }),
      hosId ? null : levelSelect('医院', hospitals, value.hosId, !value.orgId, (next) => { void handleChange('hospital', next) }),
      levelSelect('院区', branches, value.branchId, !(hosId ?? value.hosId), (next) => { void handleChange('branch', next) }),
    )
  }

  return {
    configureBaseComponents: (config: { client?: unknown }) => {
      baseClientRef.current = config.client ?? null
    },
    BaseScopeSelector: StubScopeSelector,
  }
})

vi.mock('./reportsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./reportsApi')>()
  return { ...actual, ...api }
})

/**
 * 可信范围接缝（`@dy/auth` 的 `LoginUserManager`）：平台页初始解析按可信优先读取三层，
 * 用例可改写本引用模拟「可信范围缺失」与「可信医院/院区不属于首项组合」两类分支。
 */
const trustedRef = vi.hoisted(() => ({
  orgId: 'ORG-A' as string | null,
  hosId: 'HOS-1' as string | null,
  branchId: 'BRA-1' as string | null,
}))

vi.mock('@dy/auth', () => ({
  LoginUserManager: {
    getOrgID: () => trustedRef.orgId,
    getHosID: () => trustedRef.hosId,
    getBranchID: () => trustedRef.branchId,
  },
}))

/** 占位 client：页面把 API Client 原样交给适配层出口，自身不访问任何端点。 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

const ORGANIZATIONS = [{ id: 'ORG-A', name: '组织一' }]
const HOSPITALS = [{ id: 'HOS-1', name: '医院一' }, { id: 'HOS-2', name: '医院二' }]
const BRANCHES = [
  { id: 'BRA-1', name: '院区一', hosId: 'HOS-1' },
  { id: 'BRA-2', name: '院区二', hosId: 'HOS-1' },
  { id: 'BRA-3', name: '院区三', hosId: 'HOS-2' },
]

function createBaseClient() {
  return {
    api: {
      organization: {
        queryAllOrganization: { post: vi.fn(async () => ORGANIZATIONS) },
        queryAllValidHospitalByOrgId: { post: vi.fn(async () => HOSPITALS) },
        queryAllValidBranchByOrgId: { post: vi.fn(async () => BRANCHES) },
      },
    },
  }
}

let baseClient: ReturnType<typeof createBaseClient>

/**
 * 列表读取替身交付的**页面分页模型**。
 *
 * 组件用例替换的是适配层的列表读取出口本身（文件头第 1 条），因此该出口在本文件里的返回值
 * 就是页面主体拿到的分页结果：形状必须与适配层收敛后的页面分页模型一致，
 * 否则测试连「页面拿到的分页状态」这一层都不成立。
 * 行夹具经真实适配层出口 `toReportPage` 归一，因此枚举文本兜底、可空归一与页码校验都由适配层决定，
 * 测试无法用「页面最终状态」绕过适配逻辑。
 * 适配层把生成端形状（`items` + `page`）收敛为页面分页模型的过程由 `reportsApi.test.ts` 的 C5 用例冻结。
 */
const listResponse = (
  items: readonly Record<string, unknown>[],
  pageIndex: number,
  pageSize: number,
  totalCount: number,
) => toReportPage(items as never, { pageIndex, pageSize, totalCount })

/** 生成端读模型形态的列表行。 */
const generatedRow = (overrides: Record<string, unknown> = {}) => ({
  reportId: 'REP-1',
  reportNo: 'R-1',
  reportType: 1,
  reportTypeText: '检验报告',
  reportTime: new Date(2026, 8, 20, 10, 30),
  organizationName: '组织一',
  hospitalName: '医院一',
  branchName: '院区一',
  patientName: '张三',
  identityDocumentNo: '110101199001011234',
  currentVersionSequence: 2,
  status: 1,
  statusText: '有效',
  ...overrides,
})

/** 版本行夹具：由真实适配层出口归一。 */
const versionRow = (overrides: Record<string, unknown> = {}) => toReportVersionRows([{
  reportVersionId: 'VER-2',
  versionSequence: 2,
  sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  inspectorName: '检验人乙',
  detailInspectors: '检测人乙',
  sourceReportRemark: '来源备注乙',
  pdfFileName: 'R-1-v2.pdf',
  isCurrentVersion: true,
  isSuperseded: false,
  reportStatus: 1,
  reportStatusText: '有效',
  ...overrides,
}])[0]

/** 版本详情夹具：详情路由页读取版本详情出口的真实形状替身（经真实适配层出口归一）。 */
const versionDetailRow = () => toReportVersionDetail({
  reportType: 1,
  reportTypeText: '检验报告',
  reportNo: 'R-1',
  reportVersionId: 'VER-2',
  versionSequence: 2,
  sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  sourceReportRemark: '来源备注乙',
  content: { common: null, laboratoryContent: null, examinationContent: null },
  file: { fileName: 'R-1-v2.pdf' },
})

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

const click = (element: HTMLElement) => act(() => {
  fireEvent.click(element)
})

const change = (element: HTMLElement, value: string) => act(async () => {
  fireEvent.change(element, { target: { value } })
})

const query = () => api.queryReportPage
const queryVersions = () => api.queryReportVersions

const scopeSelect = (level: '组织' | '医院' | '院区') => screen.getByLabelText(`范围-${level}`) as HTMLSelectElement

async function pickScopeOption(level: '组织' | '医院' | '院区', value: string) {
  const select = scopeSelect(level)
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await change(select, value)
}

/** 查询主按钮（提交式筛选的唯一重读入口）；读取在途时呈加载态。 */
const searchButton = () => screen.getByRole('button', { name: '查询' })
const dataRows = () => screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))
const columnHeaders = () => screen.getAllByRole('columnheader').map((node) => node.textContent)
const viewDetail = (reportNo: string) => screen.getByLabelText(`查看报告详情：${reportNo}`)
/**
 * 分页控件中的第 `page` 项。
 *
 * 按分页项自身的标题文本定位，不依赖 antd 内部生成的类名后缀：该后缀在 antd 版本之间会变化，
 * 按类名定位会把「版本升级」误判成「分页不可用」。调用方先等它出现，再点击。
 */
const pageItem = (page: number) => Array.from(document.querySelectorAll<HTMLElement>('li[title]'))
  .find((node) => node.getAttribute('title') === `${page}` && node.className.includes('ant-pagination-item')) ?? null
/**
 * 分页控件中的页容量选择控件。
 *
 * 本项目使用 antd 6，其 Select 由 `@rc-component/select` 渲染，**不产出原生 `<select>`**，
 * 因此只能按可访问角色定位容器内的 combobox（与 `RecognitionProjects.test.tsx`、`StandardCatalog.test.tsx`
 * 的既有取值口径一致）；按原生 select 定位会把「组件库升级」误判成「页容量控件缺失」。
 */
const pageSizeSelect = () =>
  document.querySelector<HTMLElement>('.ant-pagination-options .ant-select')

/** 展开页容量下拉并取回全部选项文本；选项渲染在 portal 里，只能按容器类名取。 */
const pageSizeOptions = () => {
  const select = pageSizeSelect()
  if (select === null) return []
  fireEvent.mouseDown(select.querySelector('.ant-select-selector') ?? select)
  return Array.from(document.querySelectorAll('.ant-select-item-option'))
    .map((node) => node.textContent ?? '')
    .filter((text) => text.length > 0)
}

/**
 * 页容量选项的数值取值。
 *
 * antd 按当前语言在选项后追加单位文案（如「10 条/页」），因此只取前导整数字符串：
 * 这样断言的是页面交给分页控件的**取值**本身，而不是本地化文案。
 */
const pageSizeOptionValues = () =>
  pageSizeOptions()
    .map((text) => /^\s*(\d+)/.exec(text)?.[1])
    .filter((value): value is string => value !== undefined)
    .map(Number)

/** 路由观测点：受控观测真实 Router 交付的路径与导航方式（断言 URL 与历史行为，不刺探路由内部实现）。 */
const locationRef = { current: '' }
const navigationTypeRef = { current: 'POP' }

function LocationProbe() {
  const location = useLocation()
  const navigationType = useNavigationType()
  useEffect(() => {
    locationRef.current = location.pathname
    navigationTypeRef.current = navigationType
  }, [location, navigationType])
  return null
}

function renderPage(initialPath = '/report-management') {
  return render(
    // 滚动容器替身：与应用 MainLayout 的 Content 同类名（overflow:auto 的整页滚动承载），
    // 列表主体的滚动位置记录与还原按该容器断言。
    <div className='ant-layout-content'>
      <ConfigProvider locale={zhCN}>
        <MemoryRouter initialEntries={[initialPath]}>
          <LocationProbe />
          <Routes>
            {/* 与应用路由同构：详情子路由注册在列表路由之下，页面元素由入口容器条件渲染。 */}
            <Route path='/report-management' element={<ReportManagement />}>
              <Route path='detail/:reportId' element={null} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ConfigProvider>
    </div>,
  )
}

/** 默认报告时间窗（近 30 天）：与页面同一口径，断言时按当天计算。 */
const defaultDateWindow = () => ({
  reportDateFrom: dayjs().subtract(29, 'day').format('YYYY-MM-DD'),
  reportDateTo: dayjs().format('YYYY-MM-DD'),
})

/**
 * 进入页面并让列表成功交付给定当页结果（生成端响应形态）。
 *
 * 范围由选择器初始装载自动解析（可信优先、缺失取首项），三级齐备后页面自动读取第 1 页；
 * 首次请求携带默认近 30 天时间窗，本替身按生成端形状交付响应，由真实适配层收敛。
 */
async function renderLoaded(
  items: readonly Record<string, unknown>[],
  page: { pageIndex?: number; pageSize?: number; totalCount?: number } = {},
) {
  const pending = deferred<unknown>()
  query().mockReturnValueOnce(pending.promise)
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await act(async () => {
    pending.resolve(listResponse(items, page.pageIndex ?? 1, page.pageSize ?? 10, page.totalCount ?? items.length))
  })
  await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
}

/**
 * 断言第 `index` 次列表请求携带的筛选条件与分页参数。
 *
 * 页面把「当次筛选条件」与「分页参数」合并后交给列表读取出口（三级范围取自页面已选定的组织、医院与院区），
 * 由该出口构造请求体；因此这里冻结的是「页面提供的筛选与分页」这一层事实，
 * 请求体形状（含三级范围、日期归一与证件号码原样提交）由 `reportsApi.test.ts` 的 C4 用例单独冻结。
 * 筛选断言用子集匹配：页面同时携带当次筛选、分页与入口自带的范围，用例只声明自己关心的那一部分。
 */
function expectPageFilters(
  index: number,
  filters: Record<string, unknown>,
  page: { pageIndex: number; pageSize: number },
) {
  const request = query().mock.calls[index][1] as Record<string, unknown>
  expect(request).toMatchObject(filters)
  expect(request).toMatchObject(page)
}

beforeEach(() => {
  for (const fn of Object.values(api)) fn.mockReset()
  // 版本详情出口随详情路由页承接版本切换后恢复接线：替身保持适配层出口的真实形状（经真实适配层归一）。
  api.queryReportVersionDetail.mockResolvedValue(versionDetailRow())
  clientRef.current = {}
  trustedRef.orgId = 'ORG-A'
  trustedRef.hosId = 'HOS-1'
  trustedRef.branchId = 'BRA-1'
  baseClient = createBaseClient()
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
  locationRef.current = ''
  navigationTypeRef.current = 'POP'
  // 来源行概要缓存是模块级状态：每个用例从空缓存开始，避免跨用例残留（深链口径依赖缓存未命中）。
  clearRowSummaries()
})

describe('C11 首次进入自动加载第一页与筛选区形态', () => {
  it('组织清单为空时不发起请求并以缺范围空态呈现；页头无刷新按钮', async () => {
    baseClient.api.organization.queryAllOrganization.post.mockResolvedValueOnce([])
    renderPage()

    expect(scopeSelect('组织')).toBeInTheDocument()
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    // 无首项可交付：范围不完整时不构造请求，也不退化为全局查询。
    await act(async () => {})
    expect(query()).not.toHaveBeenCalled()
    expect(screen.getByText('请选择组织、医院与院区后查看报告')).toBeInTheDocument()
    // 页头刷新按钮已移除：查询是重读唯一入口（提交式筛选）。
    expect(screen.queryByLabelText('重新读取报告列表')).toBeNull()
    expect(screen.getByRole('button', { name: '查询' })).toBeInTheDocument()
  })

  it('进入页面自动按可信优先解析三级范围并自动读取第 1 页，表格列与设计一致（来源方合并列）', async () => {
    await renderLoaded([generatedRow()])

    // 可信范围三层都在清单内：初始解析结果即可信范围，无需用户操作。
    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(scopeSelect('医院')).toHaveValue('HOS-1')
    expect(scopeSelect('院区')).toHaveValue('BRA-1')
    // 初始请求：默认近 30 天时间窗、分页为第 1 页与默认页容量 10；三级范围由范围键携带。
    expect(query()).toHaveBeenCalledTimes(1)
    expectPageFilters(0, {
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      ...defaultDateWindow(),
    }, { pageIndex: 1, pageSize: 10 })

    expect(columnHeaders()).toEqual([
      '报告单号',
      '报告类型',
      '报告时间',
      '来源方',
      '患者姓名',
      '证件号码',
      '当前版本序号',
      '报告状态',
      '操作',
    ])
    expect(dataRows()).toHaveLength(1)
    // 报告时间按日期展示，不拼接时分秒。
    expect(within(dataRows()[0]).getByText('2026-09-20')).toBeInTheDocument()
  })
})

describe('默认范围解析与默认时间窗（2026-10-15 修订）', () => {
  it('可信医院与院区不属于首项组合时按受控值重载清单完成解析（第二轮）', async () => {
    trustedRef.hosId = 'HOS-2'
    trustedRef.branchId = 'BRA-3'
    await renderLoaded([generatedRow()])

    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(scopeSelect('医院')).toHaveValue('HOS-2')
    expect(scopeSelect('院区')).toHaveValue('BRA-3')
    expectPageFilters(0, {
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-2',
      branchCode: 'BRA-3',
      ...defaultDateWindow(),
    }, { pageIndex: 1, pageSize: 10 })
  })

  it('可信范围缺失时初始解析取各层第一项', async () => {
    trustedRef.orgId = null
    trustedRef.hosId = null
    trustedRef.branchId = null
    await renderLoaded([generatedRow()])

    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(scopeSelect('医院')).toHaveValue('HOS-1')
    expect(scopeSelect('院区')).toHaveValue('BRA-1')
    expectPageFilters(0, { ...defaultDateWindow() }, { pageIndex: 1, pageSize: 10 })
  })

  it('三级缺项的回调不被接受：清空某一层保持原范围，不发起空范围请求', async () => {
    await renderLoaded([generatedRow()])
    expect(query()).toHaveBeenCalledTimes(1)

    await pickScopeOption('组织', '')
    // 缺项回调被拒绝：受控组织保持原值、原数据保留、不发新请求（不退化为不完整范围）。
    // 医院/院区控件在本替身里随组织清空而失去选项清单（原生 select 的渲染局限），不在此断言显示值；
    // 其受控值未变由「不发新请求 + 原数据保留」间接冻结。
    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(query()).toHaveBeenCalledTimes(1)
    expect(screen.getByText('R-1')).toBeInTheDocument()
  })

  it('时间区间呈现默认近 30 天窗口且不可清空：悬停不出现清除按钮', async () => {
    await renderLoaded([generatedRow()])

    const range = document.querySelector('.report-management-filters .ant-picker-range')
    expect(range).not.toBeNull()
    const inputs = within(range as HTMLElement).getAllByRole('textbox')
    expect((inputs[0] as HTMLInputElement).value).toBe(defaultDateWindow().reportDateFrom)
    expect((inputs[1] as HTMLInputElement).value).toBe(defaultDateWindow().reportDateTo)

    fireEvent.mouseEnter(range as HTMLElement)
    expect(document.querySelector('.report-management-filters .ant-picker-clear')).toBeNull()
  })

  it('删空区间文本不生效：提交仍携带默认窗口（不可清空）', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    const range = document.querySelector('.report-management-filters .ant-picker-range') as HTMLElement
    const inputs = within(range).getAllByRole('textbox')
    await change(inputs[0], '')
    fireEvent.keyDown(inputs[0], { key: 'Enter' })
    await change(inputs[1], '')
    fireEvent.keyDown(inputs[1], { key: 'Enter' })
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { ...defaultDateWindow() }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([generatedRow()], 1, 10, 1)) })
  })
})

describe('提交式筛选', () => {
  it('筛选改动只进草稿零请求；点查询按已生效条件与第 1 页读取', async () => {
    await renderLoaded([generatedRow()])
    expect(query()).toHaveBeenCalledTimes(1)

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    // 草稿改动不发起请求。
    expect(query()).toHaveBeenCalledTimes(1)

    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, { keyword: 'R-9' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([generatedRow()], 1, 10, 1)) })
  })

  it('查询区不设重置按钮；清空关键字后提交按默认条件（近 30 天窗口）回到第 1 页读取', async () => {
    await renderLoaded([generatedRow()])

    const filtered = deferred<unknown>()
    query().mockReturnValueOnce(filtered.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { filtered.resolve(listResponse([], 1, 10, 0)) })

    const cleared = deferred<unknown>()
    query().mockReturnValueOnce(cleared.promise)
    await change(screen.getByLabelText('关键字筛选'), '')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    // 关键字清空后以空值交给适配层（请求体侧按空值不下发，形状由适配层用例冻结）；
    // 默认时间窗属于已生效条件，清空关键字不影响它。
    const request = query().mock.calls[2][1] as Record<string, unknown>
    expect(request.keyword ?? '').toBe('')
    expect(request).toMatchObject(defaultDateWindow())
    expectPageFilters(2, { ...defaultDateWindow() }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { cleared.resolve(listResponse([generatedRow()], 1, 10, 1)) })
  })

  it('查询区为 inline Form：关键字、报告类型与时间区间加行尾查询按钮，不设重置（查询区结构口径）', async () => {
    await renderLoaded([generatedRow()])

    const form = document.querySelector('.report-management-filters form')
    expect(form).not.toBeNull()
    expect(form).toHaveClass('ant-form-inline')
    const search = searchButton()
    expect(search).toHaveClass('ant-btn-primary')
    expect(search.querySelector('.anticon-search')).not.toBeNull()
    expect(search.textContent).toBe('查询')
    expect(search.getAttribute('type')).toBe('submit')
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
    expect(screen.getByLabelText('关键字筛选')).toBeInTheDocument()
    expect(screen.getByLabelText('报告类型筛选')).toBeInTheDocument()
    expect(document.querySelector('.report-management-filters .ant-picker-range')).not.toBeNull()
  })

  it('来源方一列三行式：组织、医院、灰色小字院区，无来源组织/医院/院区独立列（三行式单元格口径）', async () => {
    await renderLoaded([generatedRow()])

    const table = screen.getByRole('table')
    const cell = within(table).getByText('组织一').closest('td')
    expect(cell).not.toBeNull()
    expect(within(cell ?? document.body).getByText('医院一')).toBeInTheDocument()
    const branchText = within(cell ?? document.body).getByText('院区一')
    expect(branchText).toHaveStyle({ fontSize: '12px' })
  })
})

describe('C12 筛选组合与提交回第一页', () => {
  it('翻页保持页容量；提交筛选回到第 1 页并保留当前页容量', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })

    // 切到第 2 页，确认翻页保持页容量。
    const secondPage = deferred<unknown>()
    query().mockReturnValueOnce(secondPage.promise)
    await waitFor(() => expect(pageItem(2)).not.toBeNull())
    click(pageItem(2) as HTMLElement)
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, {}, { pageIndex: 2, pageSize: 10 })
    await act(async () => { secondPage.resolve(listResponse([generatedRow()], 2, 10, 60)) })

    // 提交筛选只重置页码，页容量保持 10。
    const filtered = deferred<unknown>()
    query().mockReturnValueOnce(filtered.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    expectPageFilters(2, { keyword: 'R-9' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { filtered.resolve(listResponse([], 1, 10, 0)) })
  })

  it('关键字改动进草稿，一次提交进入同一次请求体', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    expect(query()).toHaveBeenCalledTimes(1)

    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { keyword: 'R-9' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([], 1, 10, 0)) })
  })

  it('关键字按用户输入原样提交，不做本地大小写或空白改写', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('关键字筛选'), ' 11010119900101123x ')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { keyword: ' 11010119900101123x ' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([], 1, 10, 0)) })
  })
})

describe('报告状态页签', () => {
  it('页签改动即提交：立即按该状态回到第 1 页读取，请求体携带报告状态；切回全部不携带该字段', async () => {
    await renderLoaded([generatedRow()])
    expect(query()).toHaveBeenCalledTimes(1)

    const effective = deferred<unknown>()
    query().mockReturnValueOnce(effective.promise)
    click(screen.getByRole('tab', { name: '有效' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, { reportStatus: 1 }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { effective.resolve(listResponse([generatedRow()], 1, 10, 1)) })

    const voided = deferred<unknown>()
    query().mockReturnValueOnce(voided.promise)
    click(screen.getByRole('tab', { name: '已作废' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expectPageFilters(2, { reportStatus: 2 }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { voided.resolve(listResponse([], 1, 10, 0)) })

    const all = deferred<unknown>()
    query().mockReturnValueOnce(all.promise)
    click(screen.getByRole('tab', { name: '全部' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(4))
    const request = query().mock.calls[3][1] as Record<string, unknown>
    // 「全部」不施加状态条件：请求体侧由适配层跳过未应用字段，这里的生效条件值必须为未应用。
    expect(request.reportStatus).toBeUndefined()
    await act(async () => { all.resolve(listResponse([generatedRow()], 1, 10, 1)) })
  })
})

describe('C13 时间范围取值', () => {
  it('区间日期改动进草稿，点查询后以起止日期提交，页面不拼接时分秒也不写本地时区偏移', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    // 区间选择器的两个输入框直接输入年月日并回车确认，值以文本形态进入草稿。
    const rangePicker = document.querySelector('.report-management-filters .ant-picker-range')
    expect(rangePicker).not.toBeNull()
    const rangeInputs = within(rangePicker as HTMLElement).getAllByRole('textbox')
    expect(rangeInputs.length).toBe(2)
    await change(rangeInputs[0]!, '2026-09-01')
    fireEvent.keyDown(rangeInputs[0]!, { key: 'Enter' })
    await change(rangeInputs[1]!, '2026-09-30')
    fireEvent.keyDown(rangeInputs[1]!, { key: 'Enter' })
    // 草稿改动不发起请求。
    expect(query()).toHaveBeenCalledTimes(1)

    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { reportDateFrom: '2026-09-01', reportDateTo: '2026-09-30' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([generatedRow()], 1, 10, 1)) })
  })
})

describe('C14 空结果', () => {
  it('读取成功但没有匹配报告时显示空态，不报错也不出现本地接口错误提示', async () => {
    await renderLoaded([], { totalCount: 0 })

    expect(screen.getByText('当前筛选条件没有报告')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    expect(screen.queryByText('报告列表数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/接口错误|请求失败|网络错误/)).not.toBeInTheDocument()
  })
})

describe('C15 列表加载失败与重试', () => {
  it('查询失败不渲染错误横幅：旧数据保留、结束加载态，点查询可重试成功（读取失败形态：纯读取失败不渲染横幅）', async () => {
    await renderLoaded([generatedRow()])

    const failing = deferred<unknown>()
    query().mockReturnValueOnce(failing.promise)
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { failing.reject(new Error('读取失败')) })

    // 既定口径：读取失败不渲染错误横幅（错误由宿主统一提示），已有数据保留。
    expect(screen.getByText('R-1')).toBeInTheDocument()
    expect(screen.queryByText('报告列表数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
    // 不弹本地错误提示。
    expect(screen.queryByText(/接口错误|请求失败|网络错误/)).not.toBeInTheDocument()

    // 重试经查询按钮：重读成功后按新结果呈现。
    const retry = deferred<unknown>()
    query().mockReturnValueOnce(retry.promise)
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await act(async () => { retry.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    await screen.findByText('R-2')
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
  })
})

describe('C16 分页交互', () => {
  it('翻页只发一次请求并保持页容量，改变页容量回到第 1 页', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })

    const next = deferred<unknown>()
    query().mockReturnValueOnce(next.promise)
    await waitFor(() => expect(pageItem(2)).not.toBeNull())
    click(pageItem(2) as HTMLElement)
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, {}, { pageIndex: 2, pageSize: 10 })
    await act(async () => { next.resolve(listResponse([generatedRow()], 2, 10, 60)) })

    const sized = deferred<unknown>()
    query().mockReturnValueOnce(sized.promise)
    // 改变页容量回到第 1 页：按 antd 6 的 combobox 口径展开下拉并选中 20。
    await act(async () => {
      const select = pageSizeSelect()
      expect(select).not.toBeNull()
      fireEvent.mouseDown((select as HTMLElement).querySelector('.ant-select-selector') ?? (select as HTMLElement))
    })
    const option20 = Array.from(document.querySelectorAll('.ant-select-item-option'))
      .find((node) => (node.textContent ?? '').includes('20'))
    expect(option20).toBeDefined()
    await act(async () => {
      fireEvent.click(option20 as HTMLElement)
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expectPageFilters(2, {}, { pageIndex: 1, pageSize: 20 })
    await act(async () => { sized.resolve(listResponse([generatedRow()], 1, 20, 60)) })

    // 总数来自服务端，不补造也不截断。
    expect(screen.getByText(/共 60 条/)).toBeInTheDocument()
  })

  it('页容量可选值都在服务端声明的取值域内', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })

    expect(pageSizeSelect()).not.toBeNull()
    const values = pageSizeOptionValues()
    expect(values.length).toBeGreaterThan(0)
    for (const value of values) {
      expect(Number.isInteger(value)).toBe(true)
      expect(value).toBeGreaterThanOrEqual(1)
      expect(value).toBeLessThanOrEqual(200)
    }
  })
})

describe('C17 页码越界自愈', () => {
  it('当页为空且总数大于零时按服务端总数回退到最后一页一次，并保持页容量', async () => {
    // 总数 25、页容量 10 时有 3 页：请求第 3 页拿到空结果，服务端总数仍是 25，
    // 页面据此按总数回退到最后有数据的那一页（第 3 页本身越界时目标即按总数算出的最后一页）。
    await renderLoaded([generatedRow()], { totalCount: 25 })

    // 服务端把第 3 页返回空，但总数仍报 25（数据收缩的典型形态）。
    // 回退请求的替身必须在空结果交付**之前**入队：回退由 effect 的宏任务触发，
    // 若在空结果交付后再入队，负载较高时宏任务可能先于入队执行、回退会拿到 undefined。
    const emptyThird = deferred<unknown>()
    const fallback = deferred<unknown>()
    query().mockReturnValueOnce(emptyThird.promise)
    query().mockReturnValueOnce(fallback.promise)
    await waitFor(() => expect(pageItem(3)).not.toBeNull())
    click(pageItem(3) as HTMLElement)
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, {}, { pageIndex: 3, pageSize: 10 })
    await act(async () => { emptyThird.resolve(listResponse([], 3, 10, 25)) })

    // 回退请求按服务端总数与页容量算出最后一页；页容量保持不变。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expectPageFilters(2, {}, { pageIndex: 3, pageSize: 10 })
    await act(async () => { fallback.resolve(listResponse([generatedRow()], 3, 10, 25)) })
    await screen.findByText('R-1')
  })
})

/**
 * T02「查看详情」进入详情路由（测试矩阵 C01「列表行「查看详情」进入详情路由」，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md）。
 *
 * 列表行「查看详情」从页面底部追加详情区改为跳转详情子路由：URL 进入 `detail/{reportId}`、
 * 行概要经来源行概要缓存（`reportRowSummaryCache`）随导航传递、列表组件保持挂载（仅隐藏）；
 * 来源行以高亮类标记（可见样式由配套 CSS 承载）。
 */
describe('T02 查看详情进入详情路由（C01）', () => {
  it('点击查看详情跳转 detail/{reportId}：列表隐藏保持挂载，行概要经概要缓存即时呈现', async () => {
    await renderLoaded([generatedRow()])
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)

    click(viewDetail('R-1'))

    // URL 进入详情子路由，历史以 push 方式新增条目（矩阵 C01：新增历史条目）。
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    expect(navigationTypeRef.current).toBe('PUSH')
    // 列表保持挂载：筛选区仍在文档中且处于隐藏态（范围、草稿、分页与数据跨导航存活）。
    expect(screen.getByLabelText('关键字筛选')).not.toBeVisible()
    // 行概要经概要缓存即时呈现为概要头部完整两行（根 design.md 6.1）：粘性条单号主标题之外，
    // 概要卡第 1 行单号（copyable）+ 类型/状态 Tag、第 2 行患者/证件号码/报告时间/来源方/当前版本五字段。
    expect(screen.getByRole('heading', { name: '报告详情 R-1' })).toBeInTheDocument()
    const summaryHeader = document.querySelector('.report-management-detail-summary-header')
    expect(summaryHeader).not.toBeNull()
    const header = within(summaryHeader as HTMLElement)
    expect(header.getAllByText('R-1').length).toBeGreaterThan(0)
    expect(header.getByRole('button', { name: '复制' })).toBeInTheDocument()
    expect(header.getByText('检验报告')).toBeInTheDocument()
    expect(header.getByText('有效')).toBeInTheDocument()
    expect(header.getByText('张三')).toBeInTheDocument()
    expect(header.getByText('110101199001011234')).toBeInTheDocument()
    expect(header.getByText('2026-09-20')).toBeInTheDocument()
    expect(header.getByText('组织一 医院一')).toBeInTheDocument()
    expect(header.getByText('院区一')).toBeInTheDocument()
    expect(header.getByText('V2')).toBeInTheDocument()
    // 按行的报告标识读取版本列表（适配层出口首参为 client，第二参为报告标识）。
    expect(queryVersions()).toHaveBeenCalledTimes(1)
    expect(queryVersions().mock.calls[0][1]).toBe('REP-1')

    await act(async () => { versionsPending.resolve([versionRow()]) })
  })

  it('点击后该行获得来源行高亮类；仅命中来源行', async () => {
    await renderLoaded([generatedRow({ reportId: 'REP-1', reportNo: 'R-1' }), generatedRow({ reportId: 'REP-2', reportNo: 'R-2' })], { totalCount: 2 })
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)

    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))

    // 行元素仍挂载（隐藏）：只有来源行命中高亮类，可见样式与左竖条由配套 CSS 承载（静态证据）。
    const highlighted = document.querySelectorAll('.report-management-detail-row-source')
    expect(highlighted).toHaveLength(1)
    expect(highlighted[0]?.textContent).toContain('R-1')

    await act(async () => { versionsPending.resolve([versionRow()]) })
  })

  it('同页先后打开两份不同报告：概要各取自身缓存', async () => {
    await renderLoaded([
      generatedRow({ reportId: 'REP-1', reportNo: 'R-1' }),
      generatedRow({ reportId: 'REP-2', reportNo: 'R-2', patientName: '李四', identityDocumentNo: '110101199001011235' }),
    ], { totalCount: 2 })
    api.queryReportVersions.mockResolvedValue([versionRow()])

    // 第一份：概要取 R-1 自身缓存。
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    expect(screen.getByRole('heading', { name: '报告详情 R-1' })).toBeInTheDocument()
    const summary1 = document.querySelector('.report-management-detail-summary-header')
    expect(summary1).not.toBeNull()
    expect(within(summary1 as HTMLElement).getByText('张三')).toBeInTheDocument()

    // 返回列表后打开第二份：概要按路径 reportId 取 R-2 自身缓存，不残留第一份。
    click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))
    click(viewDetail('R-2'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-2'))
    expect(screen.getByRole('heading', { name: '报告详情 R-2' })).toBeInTheDocument()
    const summary2 = document.querySelector('.report-management-detail-summary-header')
    expect(summary2).not.toBeNull()
    expect(within(summary2 as HTMLElement).getByText('李四')).toBeInTheDocument()
    expect(within(summary2 as HTMLElement).queryByText('张三')).toBeNull()
  })
})

/**
 * T02 返回还原（测试矩阵 C11「返回还原」，docs/plans/_temporary/T02-.../Client/testPlan.md；
 * 根 design.md 5.3 D-02/D-03：仅「从详情页返回列表」保证还原）。
 *
 * 还原清单六项：查询范围、已生效筛选与草稿、状态页签、分页位置、滚动位置、来源行标识（高亮 + 焦点）。
 * 前四项由列表保持挂载天然存活；滚动与焦点是本组断言的重心：
 * `display:none` 期间浏览器会重置滚动容器位置，跳转前记录、返回后先恢复 scrollTop 再聚焦来源行按钮。
 */
describe('T02 返回还原与来源行焦点（C11）', () => {
  /** 滚动容器替身（与 MainLayout Content 同类名）的受控 scrollTop 读写。 */
  const scrollContainer = () => document.querySelector('.ant-layout-content') as HTMLElement
  const controlScroll = () => {
    const container = scrollContainer()
    let stored = 0
    Object.defineProperty(container, 'scrollTop', { get: () => stored, set: (value: number) => { stored = value }, configurable: true })
    return container
  }

  it('进入详情前记录滚动位置，返回后先恢复 scrollTop 再聚焦来源行「查看详情」按钮', async () => {
    await renderLoaded([generatedRow()])
    const container = controlScroll()
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)

    container.scrollTop = 500
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    await act(async () => { versionsPending.resolve([versionRow()]) })

    // 模拟隐藏期间浏览器重置滚动容器位置。
    container.scrollTop = 0
    click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))

    // 滚动位置还原为跳转前记录值；焦点还原到来源行「查看详情」按钮且不引发滚动（preventScroll）。
    expect(container.scrollTop).toBe(500)
    expect(viewDetail('R-1')).toHaveFocus()
  })

  it('返回后筛选草稿与已生效条件、状态页签、页码页容量原样保持，来源行高亮呈现', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })
    const container = controlScroll()

    // 施加已生效筛选（提交式）。
    const filtered = deferred<unknown>()
    query().mockReturnValueOnce(filtered.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { filtered.resolve(listResponse([generatedRow()], 1, 10, 60)) })

    // 状态页签改动即提交。
    const statusPending = deferred<unknown>()
    query().mockReturnValueOnce(statusPending.promise)
    click(screen.getByRole('tab', { name: '有效' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await act(async () => { statusPending.resolve(listResponse([generatedRow()], 1, 10, 60)) })

    // 页容量切到 20（改变页容量回到第 1 页）。
    const sized = deferred<unknown>()
    query().mockReturnValueOnce(sized.promise)
    await act(async () => {
      const select = pageSizeSelect()
      expect(select).not.toBeNull()
      fireEvent.mouseDown((select as HTMLElement).querySelector('.ant-select-selector') ?? (select as HTMLElement))
    })
    const option20 = Array.from(document.querySelectorAll('.ant-select-item-option'))
      .find((node) => (node.textContent ?? '').includes('20'))
    expect(option20).toBeDefined()
    await act(async () => { fireEvent.click(option20 as HTMLElement) })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(4))
    await act(async () => { sized.resolve(listResponse([generatedRow()], 1, 20, 60)) })

    // 进入详情再返回。
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)
    container.scrollTop = 320
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    await act(async () => { versionsPending.resolve([versionRow()]) })
    container.scrollTop = 0
    click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))

    // 六项还原：筛选草稿与已生效条件、状态页签、页码页容量、滚动位置、来源行高亮与焦点。
    expect(screen.getByLabelText('关键字筛选')).toHaveValue('R-9')
    expect(screen.getByRole('tab', { name: '有效' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByText(/第 1 页，每页 20 条/)).toBeInTheDocument()
    expect(container.scrollTop).toBe(320)
    expect(dataRows()).toHaveLength(1)
    expect(dataRows()[0]).toHaveClass('report-management-detail-row-source')
    expect(viewDetail('R-1')).toHaveFocus()
  })

  it('面包屑父级链接与返回按钮同返回动作：确定性 replace 回列表父路由', async () => {
    await renderLoaded([generatedRow()])
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    await act(async () => { versionsPending.resolve([versionRow()]) })

    click(screen.getByRole('button', { name: '报告管理' }))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))
    expect(navigationTypeRef.current).toBe('REPLACE')
    expect(screen.getByLabelText('关键字筛选')).toBeVisible()
  })

  it('范围切换复位清除来源行高亮', async () => {
    await renderLoaded([generatedRow()])
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    await act(async () => { versionsPending.resolve([versionRow()]) })
    click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))
    expect(dataRows()[0]).toHaveClass('report-management-detail-row-source')

    // 切换院区：范围切换复位分支清除来源行标识，重读后同标识行不再高亮。
    const switched = deferred<unknown>()
    query().mockReturnValueOnce(switched.promise)
    await pickScopeOption('院区', 'BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { switched.resolve(listResponse([generatedRow()], 1, 10, 1)) })

    expect(dataRows()).toHaveLength(1)
    expect(dataRows()[0]).not.toHaveClass('report-management-detail-row-source')
  })

  it('翻到第 2 页进入详情再返回仍停第 2 页，查询范围取值保持', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })

    // 翻到第 2 页。
    const secondPage = deferred<unknown>()
    query().mockReturnValueOnce(secondPage.promise)
    await waitFor(() => expect(pageItem(2)).not.toBeNull())
    click(pageItem(2) as HTMLElement)
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { secondPage.resolve(listResponse([generatedRow()], 2, 10, 60)) })

    // 进入详情再返回。
    const versionsPending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(versionsPending.promise)
    click(viewDetail('R-1'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management/detail/REP-1'))
    await act(async () => { versionsPending.resolve([versionRow()]) })
    click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))

    // 分页位置保持第 2 页（还原清单第 4 项）；查询范围（组织、医院、院区）取值保持（还原清单第 1 项）。
    expect(screen.getByText(/第 2 页，每页 10 条/)).toBeInTheDocument()
    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(scopeSelect('医院')).toHaveValue('HOS-1')
    expect(scopeSelect('院区')).toHaveValue('BRA-1')
  })

  it('来源行高亮样式由配套 CSS 提供：colorPrimaryBg 底、悬停同色、左 2px colorPrimary 竖条（静态证据）', () => {
    // jsdom 无布局：高亮视觉规则按配套 CSS 的静态规则断言（Testing Baseline §1.4 静态证据）。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    const base = css.match(/\.ant-table-tbody > tr\.report-management-detail-row-source > td\s*\{[^}]*\}/)?.[0] ?? ''
    expect(base).toContain('var(--ant-color-primary-bg')
    // 悬停来源行时保持高亮底色：该规则压过 antd 的行悬停底色（F2）。
    const hover = css.match(/\.ant-table \.ant-table-tbody > \.ant-table-row\.report-management-detail-row-source > \.ant-table-cell-row-hover\s*\{[^}]*\}/)?.[0] ?? ''
    expect(hover).toContain('var(--ant-color-primary-bg')
    const bar = css.match(/\.ant-table-tbody > tr\.report-management-detail-row-source > td:first-child\s*\{[^}]*\}/)?.[0] ?? ''
    expect(bar).toContain('inset 2px 0 0')
    expect(bar).toContain('var(--ant-color-primary,')
  })
})

describe('C22 枚举文本展示', () => {
  it('报告类型与状态使用服务端随行返回的文本，缺失时才回退到适配层兜底文案', async () => {
    // 第一行由服务端交付文本字段；第二行把文本字段置空，模拟服务端未交付（生成端字段缺失），
    // 由适配层按枚举取值回退到兜底出口。
    const rows = [
      generatedRow({ reportId: 'REP-1', reportNo: 'R-1' }),
      generatedRow({ reportId: 'REP-2', reportNo: 'R-2', reportType: 2, reportTypeText: null, status: 2, statusText: null }),
    ]
    await renderLoaded(rows, { totalCount: 2 })

    // 服务端文本优先：类型与状态各在列与筛选选项中可能出现多处，因此按集合断言存在。
    expect(screen.getAllByText('检验报告').length).toBeGreaterThan(0)
    // 服务端未交付文本时按枚举取值回退到适配层兜底出口。
    expect(screen.getAllByText('检查报告').length).toBeGreaterThan(0)
    expect(screen.getAllByText('有效').length).toBeGreaterThan(0)
    expect(screen.getAllByText('已作废').length).toBeGreaterThan(0)
  })

  it('未知枚举取值安全展示，不默认成已知类型或状态', async () => {
    await renderLoaded([generatedRow({ reportType: 9, status: 9, reportTypeText: null, statusText: null })])

    // 页面不出现本地中文映射表：未知取值不落到任何已知中文上。
    // 断言圈定在表格内——列表上沿状态页签持有「有效/已作废」页签文案，不属于行内枚举兜底。
    const table = screen.getByRole('table')
    expect(screen.getAllByText('未知类型').length).toBeGreaterThan(0)
    expect(screen.getAllByText('未知状态').length).toBeGreaterThan(0)
    expect(within(table).queryByText('检验报告')).toBeNull()
    expect(within(table).queryByText('有效')).toBeNull()
  })
})

/**
 * T02「报告详情」详情路由页（测试矩阵 C12「深链首开」，docs/plans/_temporary/T02-.../Client/testPlan.md）。
 *
 * 深链直开（刷新、粘贴、外发链接）来源行概要缓存未命中：详情按路径 reportId 读取版本列表，
 * 缺失字段显示「—」，不额外发请求补数；返回动作跳回父列表路由。
 */
describe('T02 深链直开详情路由（C12）', () => {
  it('深链直开按路径 reportId 读取版本列表，无行概要时单号与来源方字段显示「—」', async () => {
    queryVersions().mockResolvedValueOnce([versionRow()])
    renderPage('/report-management/detail/REP-1')

    // 详情页粘性条呈现：返回按钮与页面主标题（无 state，单号占位）。
    expect(await screen.findByLabelText('返回报告列表')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: '报告详情 —' })).toBeInTheDocument()
    // 概要卡缺失字段按「—」呈现（不只单号）：来源方字段以占位符展示，不额外发请求补数。
    const summary = document.querySelector('.report-management-detail-summary')
    expect(summary).not.toBeNull()
    const sourceLabel = within(summary as HTMLElement).getByText('来源方')
    expect(sourceLabel.parentElement?.textContent).toContain('—')
    // 按路径 reportId 读取版本列表，不按其他来源补数（适配层出口首参为 client，第二参为报告标识）。
    expect(queryVersions()).toHaveBeenCalledTimes(1)
    expect(queryVersions().mock.calls[0][1]).toBe('REP-1')
  })

  it('版本列表在途渲染骨架并播报加载中；空版本清单呈现页面级空态与「返回列表」按钮', async () => {
    const pending = deferred<ReportVersionRow[]>()
    queryVersions().mockReturnValueOnce(pending.promise)
    renderPage('/report-management/detail/REP-1')

    // 在途：辅助文案（视觉隐藏 + aria-live）与骨架在位。
    expect(await screen.findByRole('status')).toHaveTextContent('正在加载版本列表…')
    expect(document.querySelector('.ant-skeleton')).not.toBeNull()

    await act(async () => { pending.resolve([]) })
    // 加载结束后播报消失；空版本清单渲染页面级 Empty 与「返回列表」（根 design.md 5.4/6.9），
    // 页面级空态替换五卡壳：概要、版本信息、公共信息、检验内容、历史版本卡不再渲染。
    await waitFor(() => expect(screen.queryByRole('status')).toBeNull())
    expect(screen.getByText('该报告没有历史版本')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '返回列表' })).toBeInTheDocument()
    for (const cardTitle of ['概要', '版本信息', '公共信息', '检验内容', '历史版本']) {
      expect(screen.queryByText(cardTitle)).toBeNull()
    }
  })

  it('进入详情页焦点置于页面主标题', async () => {
    queryVersions().mockResolvedValueOnce([versionRow()])
    renderPage('/report-management/detail/REP-1')

    expect(await screen.findByRole('heading', { name: '报告详情 —' })).toHaveFocus()
  })

  it('版本列表交付后五卡布局齐备：概要、版本信息、公共信息、检验内容、历史版本', async () => {
    queryVersions().mockResolvedValueOnce([versionRow()])
    renderPage('/report-management/detail/REP-1')

    expect(await screen.findByText('概要')).toBeInTheDocument()
    for (const cardTitle of ['版本信息', '公共信息', '检验内容', '历史版本']) {
      expect(screen.getByText(cardTitle)).toBeInTheDocument()
    }
  })

  it('深链直开返回：以 replace 方式跳回父列表路由并呈现列表', async () => {
    queryVersions().mockResolvedValueOnce([versionRow()])
    renderPage('/report-management/detail/REP-1')

    click(await screen.findByLabelText('返回报告列表'))
    // replace 语义（矩阵 C12：返回走父路由 replace，不产生空历史层）。
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))
    expect(navigationTypeRef.current).toBe('REPLACE')
    // 列表重新可见（隐藏承载解除），可按默认口径继续操作。
    expect(screen.getByLabelText('关键字筛选')).toBeVisible()
  })

  it('刷新恢复后（无可回退历史）返回仍回到列表：不依赖历史回退，确定性 replace 回父路由', async () => {
    // 宿主恢复场景：会话内曾从列表进入详情（缓存持有来源行概要），但路由历史已无列表项可退——
    // 组件层以单条目历史模拟「没有可回退列表项」的历史。
    setRowSummary('REP-1', toReportRows([generatedRow()])[0])
    queryVersions().mockResolvedValueOnce([versionRow()])
    renderPage('/report-management/detail/REP-1')

    click(await screen.findByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current).toBe('/report-management'))
    expect(navigationTypeRef.current).toBe('REPLACE')
    // 列表呈现，来源行还原链路不受返回方式影响（滚动与焦点还原由列表主体承接）。
    expect(screen.getByLabelText('关键字筛选')).toBeVisible()
  })
})

describe('C9 范围切换重置', () => {
  it('切换院区时清空旧数据与未提交筛选（草稿）并回到第 1 页；已生效条件复位为默认时间窗', async () => {
    await renderLoaded([generatedRow()])

    // 只设置草稿，不提交：范围切换把草稿与已生效条件一并复位。
    await change(screen.getByLabelText('关键字筛选'), 'R-9')
    expect(query()).toHaveBeenCalledTimes(1)

    // 切换院区：旧数据与旧筛选一并复位，请求回到第 1 页；时间窗复位为新的默认窗口。
    const switched = deferred<unknown>()
    query().mockReturnValueOnce(switched.promise)
    await pickScopeOption('院区', 'BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { branchCode: 'BRA-2', ...defaultDateWindow() }, { pageIndex: 1, pageSize: 10 })
    const request = query().mock.calls[1][1] as Record<string, unknown>
    expect(request.keyword).toBeUndefined()
    expect(screen.getByLabelText('关键字筛选')).toHaveValue('')
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()
    await act(async () => { switched.resolve(listResponse([], 1, 10, 0)) })
  })
})

describe('C10 过期响应丢弃与提交在途门禁', () => {
  it('查询提交在途时按钮进入加载并禁用，再次点击不重复发请求', async () => {
    await renderLoaded([generatedRow()])

    const first = deferred<unknown>()
    query().mockReturnValueOnce(first.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-1')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    // 提交时在途即 loading（与列表页统一）：按钮进入加载态。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))

    // 在途期间再次点击不发新请求（antd loading 在途阻止点击提交）。
    await change(screen.getByLabelText('关键字筛选'), 'R-2')
    click(searchButton())
    expect(query()).toHaveBeenCalledTimes(2)

    // 响应到达后加载态结束。
    await act(async () => { first.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    await screen.findByText('R-2')
  })

  it('交叠请求只采纳最后一次：查询在途时切换范围，迟到的旧响应被丢弃', async () => {
    await renderLoaded([generatedRow()])

    const first = deferred<unknown>()
    query().mockReturnValueOnce(first.promise)
    await change(screen.getByLabelText('关键字筛选'), 'R-1')
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 查询按钮在途已禁用，交叠改为经范围切换构造（范围切换不受按钮门禁）：
    const second = deferred<unknown>()
    query().mockReturnValueOnce(second.promise)
    await pickScopeOption('院区', 'BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    // 先让最新结果到达，再让过期结果到达：过期结果不得覆盖页面数据。
    await act(async () => { second.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    await screen.findByText('R-2')
    await act(async () => { first.resolve(listResponse([generatedRow({ reportNo: 'R-1' })], 1, 10, 1)) })

    expect(screen.getByText('R-2')).toBeInTheDocument()
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()
  })
})

describe('页面标题层级', () => {
  it('页面大标题为 level 3', async () => {
    await renderLoaded([generatedRow()])

    expect(screen.getByRole('heading', { name: '报告管理与历史版本', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '报告管理与历史版本', level: 2 })).toBeNull()
  })

  it('标题 margin 归零：配套 CSS 的归零选择器随标题层级改为 h3', async () => {
    await renderLoaded([generatedRow()])

    // jsdom 无布局：margin 归零按配套 CSS 的静态规则断言（Testing Baseline §1.4 静态证据）。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    const rule = css.match(/\.report-management-header\s+h3\.ant-typography\s*\{[^}]*\}/)?.[0] ?? ''
    expect(rule).toContain('margin: 0')
    expect(css).not.toMatch(/\.report-management-header\s+h2\.ant-typography/)
  })
})
