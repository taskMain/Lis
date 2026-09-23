/**
 * 阶段 4「报告管理与历史版本」平台管理员页 Component 层用例
 * （测试矩阵 C9、C10、C11、C12、C13、C14、C15、C16、C17、C18、C19、C20、C21、C22 的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./reportsApi` 只替换 4 个 I/O 出口（列表读取、版本列表读取、版本详情读取、PDF 下载），
 *    其余纯函数（请求构造、日期归一、读模型 → 视图模型映射与枚举文案兜底）**使用真实实现**：
 *    列表替身交付**生成端响应形态**（`items` 为生成端读模型 + `page` 分页信息），
 *    由真实适配层收敛为页面分页模型，因此分页收敛、日期归一与文本兜底仍由适配层决定，
 *    不是把页面最终状态直接喂给页面；
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
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import {
  toReportVersionDetail,
  toReportVersionRows,
  type ReportVersionDetail,
  type ReportVersionRow,
} from './reportsApi'
import { toReportPage } from './reportsApi'
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

  type ScopeOption = { id?: string | null; name?: string | null; hosId?: string | null }
  interface ScopeClient {
    api: {
      organization: {
        queryAllOrganization: { post: () => Promise<ScopeOption[] | undefined> }
        queryAllValidHospitalByOrgId: { post: (body: { orgId: string }) => Promise<ScopeOption[] | undefined> }
        queryAllValidBranchByOrgId: { post: (body: { orgId: string }) => Promise<ScopeOption[] | undefined> }
      }
    }
  }

  /** 范围选择器替身：受控值、级联回填与 `onChange` 回传完整范围值（与真实组件同形）。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, onChange }: BaseScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const loadedScopeKeyRef = useRef('')
    useEffect(() => {
      const loadKey = `${orgId ?? ''}|${hosId ?? ''}|${value.hosId ?? ''}`
      if (loadedScopeKeyRef.current === loadKey) return
      loadedScopeKeyRef.current = loadKey
      const client = baseClientRef.current as ScopeClient
      const load = async () => {
        if (!orgId) {
          setOrganizations(await client.api.organization.queryAllOrganization.post() ?? [])
          return
        }
        const targetHospital = hosId ?? value.hosId
        setHospitals(await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId }) ?? [])
        const allBranches = await client.api.organization.queryAllValidBranchByOrgId.post({ orgId }) ?? []
        setBranches(allBranches.filter((branch) => branch.hosId === targetHospital))
      }
      void load()
    }, [hosId, orgId, value.hosId])

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

/** 版本详情夹具：检验内容，由真实适配层出口归一。 */
const laboratoryDetail = (overrides: Record<string, unknown> = {}): ReportVersionDetail => toReportVersionDetail({
  reportType: 1,
  reportTypeText: '检验报告',
  reportNo: 'R-1',
  reportVersionId: 'VER-2',
  versionSequence: 2,
  sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  inspectorName: '检验人乙',
  detailInspectors: '检测人乙',
  sourceReportRemark: '来源备注乙',
  content: {
    common: {
      patientName: '张三',
      patientGenderCode: '1',
      patientBirthDate: new Date(1990, 0, 1),
      patientPhoneNumber: '13800001234',
      ageAtReport: '36岁',
      identityDocumentTypeCode: '01',
      identityDocumentNo: '110101199001011234',
      visitType: 1,
      visitTypeText: '门诊',
      visitSerialNo: 'VISIT-1',
      sourceReportName: '血常规报告',
      applicationDeptName: '申请科室',
      applicationDeptId: 'D-1',
      applicationDoctorName: '申请医生',
      applicationDoctorId: 'DOC-1',
      executionDeptName: '执行科室',
      executionDeptId: 'D-2',
      reportDeptName: '报告科室',
      reportDeptId: 'D-3',
      reportDoctorName: '报告医生乙',
      reportDoctorId: 'DOC-2',
      reviewDoctorName: '审核医生乙',
      reviewDoctorId: 'DOC-3',
      reviewTime: new Date(2026, 8, 20, 9, 0),
      inpatientNo: null,
      wardName: null,
      roomName: null,
      bedNo: null,
      applicationTime: new Date(2026, 8, 20, 8, 0),
      reportTime: new Date(2026, 8, 20, 9, 0),
      sourceConfidentialFlag: null,
    },
    laboratoryContent: {
      sourceSpecimenNo: 'SP-1',
      specimenTypeCode: 'SERUM',
      specimenTypeName: '血清',
      testingCompletedTime: new Date(2026, 8, 20, 9, 0),
      specimenCollectedTime: new Date(2026, 8, 20, 8, 10),
      specimenSubmittedTime: new Date(2026, 8, 20, 8, 20),
      laboratoryReceivedTime: new Date(2026, 8, 20, 8, 30),
      reportCategoryName: '生化',
      reportCategoryCode: 'BIO',
      reportRemark: '检验备注',
      overallAbnormalFlag: '有异常',
      sourceOrderSerialNo: 'ORD-1',
      inspectorId: 'INS-1',
      inspectorName: '检验人乙',
      results: [{
        displayOrder: 1,
        sourceProjectName: '白细胞',
        sourceProjectCode: 'WBC',
        standardProjectCode: 'STD-WBC',
        sourceResultText: '6.5',
        resultType: 1,
        resultTypeText: '数值型',
        unit: '10^9/L',
        referenceRange: '4-10',
        abnormalFlag: 1,
        abnormalFlagText: '正常',
        criticalValueFlag: false,
        testingMethod: '流式',
        instrumentName: '仪器甲',
        inspectorName: '检测人乙',
      }],
      bacteriaResults: [{
        detectionConclusion: '检出大肠埃希菌',
        sourceResultText: '检出',
        sourceOrganismCode: 'ECO',
        sourceOrganismName: '大肠埃希菌',
        colonyCount: '大量',
        cultureMedium: '血平板',
        cultureTime: '24h',
        cultureCondition: '需氧',
        detectionMethod: '质谱',
        instrumentName: '仪器乙',
        testPanelName: '试验板甲',
        inspectorName: '检测人乙',
        susceptibilities: [{
          displayOrder: 1,
          drugName: '阿莫西林',
          sourceConclusionText: '耐药',
          diskContent: '10ug',
          micValue: '>=32',
          inhibitionZoneDiameter: '6mm',
          referenceValue: '>=17',
          testingMethod: '纸片法',
          testPanelOrder: '1',
          inspectorName: '检测人乙',
        }],
      }],
    },
    examinationContent: null,
  },
  file: { fileName: 'R-1-v2.pdf' },
  ...overrides,
})

/** 版本详情夹具：检查内容。 */
const examinationDetail = (): ReportVersionDetail => toReportVersionDetail({
  reportType: 2,
  reportTypeText: '检查报告',
  reportNo: 'R-1',
  reportVersionId: 'VER-2',
  versionSequence: 2,
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  examinerName: '检查医生乙',
  content: {
    common: { patientName: '张三', identityDocumentNo: '110101199001011234' },
    laboratoryContent: null,
    examinationContent: {
      findings: '双肺纹理增粗',
      conclusion: '支气管炎',
      conditionDescription: '咳嗽三天',
      examinationPurpose: '明确诊断',
      sourceDiagnosisName: '支气管炎',
      sourceDiagnosisCode: 'J20',
      examinationTime: new Date(2026, 8, 20, 9, 0),
      examinerId: 'EX-1',
      examinerName: '检查医生乙',
      sourceImageStatus: 1,
      sourceImageStatusText: '有影像',
      imageAccessUrl: 'http://image/1',
      examinationMethod: '平扫',
      deviceName: 'CT',
      deviceCode: 'CT-1',
      items: [{
        sourceProjectName: '胸部CT',
        sourceProjectCode: 'CT-CHEST',
        standardProjectCode: 'STD-CT',
        sites: [{ siteName: '左肺', sourceSiteCode: 'S-L' }, { siteName: '右肺', sourceSiteCode: 'S-R' }],
      }],
    },
  },
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
const queryDetail = () => api.queryReportVersionDetail
const download = () => api.downloadReportVersionPdf

const scopeSelect = (level: '组织' | '医院' | '院区') => screen.getByLabelText(`范围-${level}`) as HTMLSelectElement

async function pickScopeOption(level: '组织' | '医院' | '院区', value: string) {
  const select = scopeSelect(level)
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await change(select, value)
}

/** 选组织即触发级联回填（医院、院区各取第一项），页面据此拿到三级完整范围。 */
const selectOrganization = () => pickScopeOption('组织', 'ORG-A')

const refreshButton = () => screen.getByLabelText('重新读取报告列表')
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

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><ReportManagement /></ConfigProvider>)
}

/** 进入页面并选定三级范围，让列表成功交付给定当页结果（生成端响应形态）。 */
async function renderLoaded(
  items: readonly Record<string, unknown>[],
  page: { pageIndex?: number; pageSize?: number; totalCount?: number } = {},
) {
  const pending = deferred<unknown>()
  query().mockReturnValueOnce(pending.promise)
  renderPage()
  await selectOrganization()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  /**
   * 交付**生成端响应形态**：分页信息在 `page` 之下，由适配层出口收敛为页面分页模型。
   * 页面因此只能通过适配层拿到分页状态，测试也无法用「页面最终状态」绕过适配逻辑。
   */
  await act(async () => {
    pending.resolve(listResponse(items, page.pageIndex ?? 1, page.pageSize ?? 10, page.totalCount ?? items.length))
  })
  await waitFor(() => expect(refreshButton()).toBeEnabled())
}

/** 展开某份报告的详情并让版本列表与首个版本内容成功交付。 */
async function openDetail(reportNo: string, versions: ReportVersionRow[], detail: ReportVersionDetail) {
  const versionsPending = deferred<ReportVersionRow[]>()
  const detailPending = deferred<ReportVersionDetail>()
  queryVersions().mockReturnValueOnce(versionsPending.promise)
  queryDetail().mockReturnValueOnce(detailPending.promise)

  click(viewDetail(reportNo))
  await waitFor(() => expect(queryVersions()).toHaveBeenCalledTimes(1))
  await act(async () => {
    versionsPending.resolve(versions)
  })
  await waitFor(() => expect(queryDetail()).toHaveBeenCalledTimes(1))
  await act(async () => {
    detailPending.resolve(detail)
  })
  await screen.findByText('报告公共信息')
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
  clientRef.current = {}
  baseClient = createBaseClient()
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
})

describe('C11 首次进入自动加载第一页与筛选区形态', () => {
  it('范围未选全时不发起请求，空态提示区分「范围未选全」与「筛选无匹配」', async () => {
    renderPage()

    expect(scopeSelect('组织')).toBeInTheDocument()
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    // 范围不完整：适配层会拒绝构造请求，页面据此不下发部分范围查询，也不退化为全局查询。
    expect(query()).not.toHaveBeenCalled()
    expect(screen.getByText('请选择组织、医院与院区后查看报告')).toBeInTheDocument()
    expect(refreshButton()).toBeDisabled()
  })

  it('选定三级范围后自动加载第 1 页，表格列与设计一致', async () => {
    await renderLoaded([generatedRow()])

    // 初始请求的筛选条件为空、分页为第 1 页与默认页容量 10；三级范围由范围键携带。
    expect(query()).toHaveBeenCalledTimes(1)
    expectPageFilters(0, {}, { pageIndex: 1, pageSize: 10 })

    expect(columnHeaders()).toEqual([
      '报告单号',
      '报告类型',
      '报告时间',
      '来源组织',
      '医院',
      '院区',
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

describe('C12 筛选组合与筛选变化回第一页', () => {
  it('筛选变化回到第 1 页并保留当前页容量', async () => {
    await renderLoaded([generatedRow()], { totalCount: 60 })

    // 切到第 2 页，确认翻页保持页容量。
    const secondPage = deferred<unknown>()
    query().mockReturnValueOnce(secondPage.promise)
    await waitFor(() => expect(pageItem(2)).not.toBeNull())
    click(pageItem(2) as HTMLElement)
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expectPageFilters(1, {}, { pageIndex: 2, pageSize: 10 })
    await act(async () => { secondPage.resolve(listResponse([generatedRow()], 2, 10, 60)) })

    // 筛选变化只重置页码，页容量保持 10。
    const filtered = deferred<unknown>()
    query().mockReturnValueOnce(filtered.promise)
    await change(screen.getByLabelText('报告单号筛选'), 'R-9')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    expectPageFilters(2, { reportNo: 'R-9' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { filtered.resolve(listResponse([], 1, 10, 0)) })
  })

  it('报告单号、报告类型与患者条件都进入同一次请求体', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('报告单号筛选'), 'R-9')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    const withPatient = deferred<unknown>()
    query().mockReturnValueOnce(withPatient.promise)
    await change(screen.getByLabelText('患者姓名筛选'), ' 张三 ')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    // 患者姓名按用户输入原样提交，由服务端规范化。
    expectPageFilters(2, { reportNo: 'R-9', patientName: ' 张三 ' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { withPatient.resolve(listResponse([], 1, 10, 0)) })
  })

  it('患者证件号码按用户输入原样提交，不做本地大小写或空白改写', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('患者证件号码筛选'), ' 11010119900101123x ')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    expectPageFilters(1, { identityDocumentNo: ' 11010119900101123x ' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { pending.resolve(listResponse([], 1, 10, 0)) })
  })
})

describe('C13 时间范围取值', () => {
  it('日期范围以起止日期提交，页面不拼接时分秒也不写本地时区偏移', async () => {
    await renderLoaded([generatedRow()])

    const pending = deferred<unknown>()
    query().mockReturnValueOnce(pending.promise)
    await change(screen.getByLabelText('报告时间起始日'), '2026-09-01')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { pending.resolve(listResponse([generatedRow()], 1, 10, 1)) })

    const endPending = deferred<unknown>()
    query().mockReturnValueOnce(endPending.promise)
    await change(screen.getByLabelText('报告时间结束日'), '2026-09-30')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    expectPageFilters(2, { reportDateFrom: '2026-09-01', reportDateTo: '2026-09-30' }, { pageIndex: 1, pageSize: 10 })
    await act(async () => { endPending.resolve(listResponse([generatedRow()], 1, 10, 1)) })
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
  it('保留已有数据、结束加载态、提供重试，且不出现本地错误提示', async () => {
    await renderLoaded([generatedRow()])

    const failing = deferred<unknown>()
    query().mockReturnValueOnce(failing.promise)
    click(refreshButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    // 加载中刷新入口不可用。
    expect(refreshButton()).toBeDisabled()
    await act(async () => { failing.reject(new Error('读取失败')) })

    await screen.findByText('报告列表数据待刷新')
    // 已有数据保留，加载态结束。
    expect(screen.getByText('R-1')).toBeInTheDocument()
    await waitFor(() => expect(refreshButton()).toBeEnabled())
    // 不弹本地错误提示。
    expect(screen.queryByText(/接口错误|请求失败|网络错误/)).not.toBeInTheDocument()

    const retry = deferred<unknown>()
    query().mockReturnValueOnce(retry.promise)
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await act(async () => { retry.resolve(listResponse([generatedRow()], 1, 10, 1)) })
    await waitFor(() => expect(screen.queryByText('报告列表数据待刷新')).not.toBeInTheDocument())
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

describe('C18 报告详情与历史版本展示', () => {
  it('展示报告公共信息与全部版本，区分当前版本、历史版本与已被替代', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [
      versionRow({ reportVersionId: 'VER-1', versionSequence: 1, isCurrentVersion: false, isSuperseded: true, pdfFileName: 'R-1-v1.pdf' }),
      versionRow(),
    ], laboratoryDetail())

    // 报告级信息与版本列表同时在位。
    expect(screen.getByText('报告详情与历史版本')).toBeInTheDocument()
    expect(screen.getByText(/报告单号 R-1/)).toBeInTheDocument()
    expect(screen.getByText('当前有效版本')).toBeInTheDocument()
    expect(screen.getByText('历史版本')).toBeInTheDocument()
    expect(screen.getByText('已被后续版本替代')).toBeInTheDocument()
    // 每个版本展示版本序号、两个时间、报告医生、审核医生、来源备注与 PDF 文件名。
    expect(screen.getAllByText('报告医生乙').length).toBeGreaterThan(0)
    expect(screen.getAllByText('审核医生乙').length).toBeGreaterThan(0)
    expect(screen.getAllByText('来源备注乙').length).toBeGreaterThan(0)
    expect(screen.getAllByText('R-1-v2.pdf').length).toBeGreaterThan(0)
    expect(screen.getAllByText('2026-09-20 09:06').length).toBeGreaterThan(0)
    expect(screen.getAllByText('2026-09-20 09:05').length).toBeGreaterThan(0)
  })

  it('报告已作废时版本列表标识作废状态，历史版本仍可下载', async () => {
    await renderLoaded([generatedRow({ status: 2, statusText: '已作废' })])
    await openDetail('R-1', [
      versionRow({ isCurrentVersion: false, isSuperseded: true, reportStatus: 2, reportStatusText: '已作废' }),
    ], laboratoryDetail())

    expect(screen.getByText(/报告状态 已作废/)).toBeInTheDocument()
    expect(screen.getByText('报告已作废')).toBeInTheDocument()
    expect(screen.getByLabelText('下载第 2 版 PDF')).toBeEnabled()
  })
})

describe('C19 版本切换与内容呈现', () => {
  it('切换版本后按该版本重新读取内容，检验内容含标本、普通结果与细菌鉴定含药敏', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [
      versionRow({ reportVersionId: 'VER-1', versionSequence: 1, isCurrentVersion: false, isSuperseded: true, pdfFileName: 'R-1-v1.pdf' }),
      versionRow(),
    ], laboratoryDetail())

    expect(screen.getByText('标本信息与检验时间线')).toBeInTheDocument()
    expect(screen.getByText('SP-1')).toBeInTheDocument()
    expect(screen.getByText('白细胞')).toBeInTheDocument()
    expect(screen.getByText('大肠埃希菌')).toBeInTheDocument()
    expect(screen.getByText('阿莫西林')).toBeInTheDocument()

    // 切到另一个版本：内容按该版本重新读取，请求带报告标识与版本标识。
    const nextDetail = deferred<ReportVersionDetail>()
    queryDetail().mockReturnValueOnce(nextDetail.promise)
    click(screen.getByLabelText('查看版本内容：第 1 版'))
    await waitFor(() => expect(queryDetail()).toHaveBeenCalledTimes(2))
    expect(queryDetail().mock.calls[1][1]).toBe('REP-1')
    expect(queryDetail().mock.calls[1][2]).toBe('VER-1')
    await act(async () => {
      nextDetail.resolve(laboratoryDetail({ reportVersionId: 'VER-1', versionSequence: 1 }))
    })
    await waitFor(() => expect(screen.getByText(/第 1 版/)).toBeInTheDocument())
  })

  it('检查报告展示所见结论、临床背景、检查信息与影像状态、检查项目与部位', async () => {
    await renderLoaded([generatedRow({ reportType: 2, reportTypeText: '检查报告' })])
    await openDetail('R-1', [versionRow()], examinationDetail())

    expect(screen.getByText('检查所见与结论')).toBeInTheDocument()
    expect(screen.getByText('双肺纹理增粗')).toBeInTheDocument()
    expect(screen.getByText('支气管炎')).toBeInTheDocument()
    expect(screen.getByText('临床背景')).toBeInTheDocument()
    expect(screen.getByText('咳嗽三天')).toBeInTheDocument()
    expect(screen.getByText('检查信息与影像状态')).toBeInTheDocument()
    expect(screen.getByText('有影像')).toBeInTheDocument()
    expect(screen.getByText('http://image/1')).toBeInTheDocument()
    expect(screen.getByText('胸部CT')).toBeInTheDocument()
    expect(screen.getByText('左肺')).toBeInTheDocument()
    expect(screen.getByText('右肺')).toBeInTheDocument()
  })

  it('历史版本只读：不提供比较、修改、删除、恢复或重新设为当前版本', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [versionRow()], laboratoryDetail())

    for (const absent of ['比较', '修改', '删除', '恢复', '重新设为当前版本']) {
      expect(screen.queryByRole('button', { name: new RegExp(absent) })).toBeNull()
    }
    for (const absent of ['更正原因', '版本变更原因']) {
      expect(screen.queryByText(absent)).toBeNull()
    }
  })
})

describe('C20 详情内 PDF 下载', () => {
  it('按报告标识与版本标识请求，文件名取该版本保存的下载名', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [versionRow()], laboratoryDetail())

    const pending = deferred<void>()
    download().mockReturnValueOnce(pending.promise)
    click(screen.getByLabelText('下载第 2 版 PDF'))
    await waitFor(() => expect(download()).toHaveBeenCalledTimes(1))
    // 下载出口签名为 (client, reportId, reportVersionId, fileName)：按报告标识与版本标识定位，文件名取该版本保存的下载名。
    expect(download().mock.calls[0][1]).toBe('REP-1')
    expect(download().mock.calls[0][2]).toBe('VER-2')
    expect(download().mock.calls[0][3]).toBe('R-1-v2.pdf')
    await act(async () => { pending.resolve() })
  })

  it('下载失败保留当前详情与版本选择、不重载列表、不误报成功', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [versionRow()], laboratoryDetail())

    download().mockRejectedValueOnce(new Error('下载失败'))
    click(screen.getByLabelText('下载第 2 版 PDF'))

    await screen.findByText('报告 PDF 下载未完成')
    // 详情与版本选择保留、列表不重载、无本地错误提示。
    expect(screen.getByText('报告公共信息')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(screen.queryByText(/接口错误|请求失败/)).not.toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText('下载第 2 版 PDF')).toBeEnabled())
  })
})

describe('C21 患者信息展示', () => {
  it('姓名与证件号码完整展示，联系电话按服务端返回值展示且页面不做二次处理', async () => {
    await renderLoaded([generatedRow()])
    await openDetail('R-1', [versionRow()], laboratoryDetail())

    expect(screen.getAllByText('110101199001011234').length).toBeGreaterThan(0)
    expect(screen.getAllByText('张三').length).toBeGreaterThan(0)
    // 服务端返回的患者联系电话原值原样展示，页面不做二次处理。
    expect(screen.getByText('13800001234')).toBeInTheDocument()
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

    expect(screen.getAllByText('未知类型').length).toBeGreaterThan(0)
    expect(screen.getAllByText('未知状态').length).toBeGreaterThan(0)
    // 页面不出现本地中文映射表：未知取值不落到任何已知中文上。
    expect(screen.queryByText('检验报告')).toBeNull()
    expect(screen.queryByText('有效')).toBeNull()
  })
})

describe('C9 范围切换重置', () => {
  it('切换院区时清空旧数据与未提交筛选并回到第 1 页', async () => {
    await renderLoaded([generatedRow()])

    // 先设置一个筛选并让它在位。
    const filtered = deferred<unknown>()
    query().mockReturnValueOnce(filtered.promise)
    await change(screen.getByLabelText('报告单号筛选'), 'R-9')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => { filtered.resolve(listResponse([generatedRow()], 1, 10, 1)) })
    await screen.findByText('R-1')

    // 切换院区：旧数据与旧筛选一并清空，请求回到第 1 页。
    const switched = deferred<unknown>()
    query().mockReturnValueOnce(switched.promise)
    await pickScopeOption('院区', 'BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    expectPageFilters(2, {}, { pageIndex: 1, pageSize: 10 })
    expect(screen.getByLabelText('报告单号筛选')).toHaveValue('')
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()
    await act(async () => { switched.resolve(listResponse([], 1, 10, 0)) })
  })
})

describe('C10 过期响应丢弃', () => {
  it('连续操作时只采纳最后一次请求结果，过期响应被丢弃', async () => {
    await renderLoaded([generatedRow()])

    const first = deferred<unknown>()
    query().mockReturnValueOnce(first.promise)
    await change(screen.getByLabelText('报告单号筛选'), 'R-1')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    const second = deferred<unknown>()
    query().mockReturnValueOnce(second.promise)
    await change(screen.getByLabelText('报告单号筛选'), 'R-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))

    // 两次筛选各自成一次请求：页面在筛选变化时只重发一次，不做防抖合并；筛选变化回到第 1 页。
    expectPageFilters(1, { reportNo: 'R-1' }, { pageIndex: 1, pageSize: 10 })
    expectPageFilters(2, { reportNo: 'R-2' }, { pageIndex: 1, pageSize: 10 })

    // 先让最新结果到达，再让过期结果到达：过期结果不得覆盖页面数据。
    await act(async () => { second.resolve(listResponse([generatedRow({ reportNo: 'R-2' })], 1, 10, 1)) })
    await screen.findByText('R-2')
    await act(async () => { first.resolve(listResponse([generatedRow({ reportNo: 'R-1' })], 1, 10, 1)) })

    expect(screen.getByText('R-2')).toBeInTheDocument()
    expect(screen.queryByText('R-1')).not.toBeInTheDocument()
  })
})
