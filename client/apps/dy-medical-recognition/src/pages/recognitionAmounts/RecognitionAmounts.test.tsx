/**
 * 阶段 3「互认项目金额维护」平台管理员页 Component 层用例
 * （测试矩阵 C1、C2、C5-C10、C13、C14、C16、C17、C21、C23 的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./recognitionAmountsApi` 只替换 4 个 I/O 出口（两个查询、两个写入），其余纯函数
 *    （请求构造、金额解析、读模型 → 行视图模型映射、金额与枚举文案）**使用真实实现**：
 *    行夹具由真实 `toRecognitionAmountRows` 从读模型形态构造，因此「未配置与零元的归一」「枚举文案」
 *    仍由适配层决定，不是把页面最终状态直接喂给页面；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client——页面把 client 原样透传给适配层出口；
 * 3. `@dy/components-base` 只替换**整个包**：该包 ESM 产物内部使用无扩展名相对导入
 *    （`dist/index.js` → `./base-scope/config`），vitest 默认把 node_modules 依赖交给 Node 解析，
 *    因此组件层无法加载真实实现（真实实现需把该包加入 `test.server.deps.inline`，而 `vite.config.ts`
 *    不属于本票所有权）。替身只复现页面接线依赖的组件契约：受控 `value`、`orgId`/`hosId` 固定后
 *    对应级不渲染、上游变化按可选项级联回填下游首项、变化经 `onChange` 回传完整范围值、
 *    选项读取失败时调用 antd 静态 `message.error`。真实选择器的行为属宿主层验收面（C1/C3 的 Host 层级）。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、真实范围选择器交互、宿主统一错误提示
 * （写失败时的业务拒绝文案由宿主展示）、C11/C12/C13/C14/C20/C22/C26 的宿主面。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider, message } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import { toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
import { RecognitionAmounts } from './RecognitionAmounts'

/** 伪 Base API Client 的注入点：与真实包 `configureBaseComponents` 同形（替身见文件头第 3 条）。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

const api = vi.hoisted(() => ({
  queryRecognitionAmountRows: vi.fn(),
  queryBranchRecognitionAmountRows: vi.fn(),
  saveOrganizationHospitalBranchRecognitionAmount: vi.fn(),
  saveBranchRecognitionAmount: vi.fn(),
}))

vi.mock('@dy/components-base', async () => {
  const { createElement, useEffect, useRef, useState } = await import('react')
  const { message: antdMessage } = await import('antd')

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

  /** 范围选择器替身：契约与真实组件一致的部分见文件头第 3 条。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, onChange }: BaseScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const initialLoadStartedRef = useRef(false)
    useEffect(() => {
      // 只在挂载时读取一次选项（与真实组件的初始加载同口径）；用 ref 守卫而不是省略依赖，
      // 让依赖数组保持完整。
      if (initialLoadStartedRef.current) return
      initialLoadStartedRef.current = true
      const client = baseClientRef.current as ScopeClient
      const load = async () => {
        try {
          if (!orgId) {
            // 未固定组织：初始只加载组织列表，医院与院区在变更时按级联加载（与真实组件同形）。
            setOrganizations(await client.api.organization.queryAllOrganization.post() ?? [])
            return
          }
          const targetHospital = hosId ?? value.hosId
          setHospitals(await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId }) ?? [])
          const allBranches = await client.api.organization.queryAllValidBranchByOrgId.post({ orgId }) ?? []
          setBranches(allBranches.filter((branch) => branch.hosId === targetHospital))
        } catch (error) {
          antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        }
      }
      void load()
    }, [hosId, orgId, value.hosId])

    /** 变更后按可选项级联回填下游首项（与真实组件同形），并把完整范围值回传页面。 */
    const handleChange = async (changed: 'org' | 'hospital' | 'branch', next: string) => {
      const client = baseClientRef.current as ScopeClient
      const merged = { ...value }
      try {
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
      } catch (error) {
        antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        return
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

    // 三个子节点按位置传入而不是放进数组：数组元素没有 key 会让 React 在 stderr 打告警、淹没真实告警。
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

vi.mock('./recognitionAmountsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./recognitionAmountsApi')>()
  return { ...actual, ...api }
})

/** 占位 client：页面把 API Client 原样交给适配层出口，自身不访问任何端点。 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

/** 伪 Base API 数据：组织、医院、院区三级（院区按医院归属，与组件过滤逻辑同形）。 */
const ORGANIZATIONS = [{ id: 'ORG-A', name: '组织一' }]
const HOSPITALS = [{ id: 'HOS-1', name: '医院一' }, { id: 'HOS-2', name: '医院二' }]
const BRANCHES = [
  { id: 'BRA-1', name: '院区一', hosId: 'HOS-1' },
  { id: 'BRA-2', name: '院区二', hosId: 'HOS-2' },
  { id: 'BRA-3', name: '院区三', hosId: 'HOS-2' },
  // 同一医院下的第二个院区：用于「切换院区」这一范围变化（院区选项按医院过滤）。
  { id: 'BRA-4', name: '院区四', hosId: 'HOS-1' },
]

/**
 * 每个用例一个伪 Base Client 实例：`restoreMocks` 会让上一用例的 `vi.fn()` 实现失效，
 * 因此在 `beforeEach` 里重建，并由用例按需改成拒绝。
 */
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

/** 生成端反序列化后的金额形态：未定型节点（`value` + `getValue()`），不是裸数字。 */
const node = (value: unknown) => ({ value, getValue: () => value })

/** 行夹具：由**真实**适配层出口从读模型形态归一而来（见文件头第 1 条）。 */
function amountRow(overrides: Partial<RecognitionAmountReadModelLike> = {}): RecognitionAmountRow {
  return toRecognitionAmountRows([{
    standardProjectCode: 'A01',
    standardProjectName: '血常规',
    itemType: 0,
    categoryName: '检验分类',
    groupName: '血液',
    organizationName: '组织一',
    hospitalName: '医院一',
    branchName: '院区一',
    configurationStatus: 1,
    unavailableReason: null,
    currentAmount: node(12.5),
    isAmountConfigured: true,
    ...overrides,
  }])[0]
}

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

const query = () => api.queryRecognitionAmountRows
const save = () => api.saveOrganizationHospitalBranchRecognitionAmount

/** 排队一次列表读取；页面每次范围变化或手动刷新都会调用一次。 */
function armQuery() {
  const pending = deferred<RecognitionAmountRow[]>()
  query().mockReturnValueOnce(pending.promise)
  return pending
}

async function settle(pending: ReturnType<typeof deferred<RecognitionAmountRow[]>>, rows: RecognitionAmountRow[]) {
  await act(async () => {
    pending.resolve(rows)
  })
}

async function failQuery(pending: ReturnType<typeof deferred<RecognitionAmountRow[]>>) {
  await act(async () => {
    pending.reject(new Error('读取失败'))
  })
}

/** 三级下拉按可访问名称定位（替身与真实组件都用「请选择组织 / 请选择医院 / 请选择院区」文案）。 */
const scopeSelect = (level: '组织' | '医院' | '院区') => screen.getByLabelText(`范围-${level}`) as HTMLSelectElement

async function pickScopeOption(level: '组织' | '医院' | '院区', value: string) {
  const select = scopeSelect(level)
  // 选项由伪 Base Client 异步交付：等选项就绪再选，避免构造出「选了不存在的值」这种非真实路径。
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await act(async () => {
    fireEvent.change(select, { target: { value } })
  })
}

/** 选组织即触发级联回填（医院、院区各取第一项），平台页据此拿到三级完整范围。 */
const selectOrganization = () => pickScopeOption('组织', 'ORG-A')

/** 弹窗按标题文本定位（与阶段 2 组件层用例同一手法：测试环境下 antd 无 id 组件共用自动生成的 id）。 */
function modalElement(title: string): HTMLElement {
  const found = Array.from(document.querySelectorAll<HTMLElement>('.ant-modal')).find(
    (node) => node.querySelector('.ant-modal-title')?.textContent?.trim() === title,
  )
  if (!found) throw new Error(`弹窗未找到：${title}`)
  return found
}

const modalExists = (title: string) =>
  Array.from(document.querySelectorAll('.ant-modal-title')).some((node) => node.textContent?.trim() === title)

const findModal = (title: string) => waitFor(() => modalElement(title))
const expectModalClosed = (title: string) =>
  waitFor(() => expect(modalExists(title), `弹窗未关闭：${title}`).toBe(false), { timeout: 3000 })

const AMOUNT_MODAL = '维护互认项目金额'

const amountInput = (container: HTMLElement) => within(container).getByLabelText('金额') as HTMLInputElement

const amountEntry = (standardProjectName: string) => screen.getByLabelText(`维护金额：${standardProjectName}`)
const okButton = (container: HTMLElement) => within(container).getByRole('button', { name: /保\s*存/ })
const refreshButton = () => screen.getByLabelText('重新读取金额列表')

const dataRows = () =>
  screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))

const columnHeaders = () => screen.getAllByRole('columnheader').map((node) => node.textContent)

/** 每行的第一个单元格文本（标准项目编码列）。 */
const renderedCodes = () =>
  dataRows().map((row) => within(row).getAllByRole('cell')[0].textContent)

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><RecognitionAmounts /></ConfigProvider>)
}

/** 进入页面并选定三级范围，让列表成功交付给定行（空集合同样是读取成功的正常结果）。 */
async function renderLoaded(rows: RecognitionAmountRow[]) {
  const pending = armQuery()
  renderPage()
  await selectOrganization()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(pending, rows)
  await waitFor(() => expect(refreshButton()).toBeEnabled())
}

beforeEach(() => {
  for (const fn of Object.values(api)) fn.mockReset()
  clientRef.current = {}
  baseClient = createBaseClient()
  // 组件的 Base API Client 走它声明的注入点，不发起真实请求。
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
})

/**
 * 用例内未处理拒绝观测器的兜底登记：用例挂死或超时时用例内的 `finally` 不会执行，而
 * `unhandledRejection` 是进程级监听，残留后会让 vitest 以「监听器数 > 1」判定调用方自行处理、
 * 不再上报自己的未处理拒绝，等于静默关掉该 worker 内后续用例的检测。因此除用例内 `finally`
 * 摘除外，再于 `afterEach` 无条件兜底摘一次。
 *
 * 该登记是**文件级单值**：装有观测器的用例之间不得并发（不得加 `.concurrent`、也不得开启
 * `sequence.concurrent`），否则先结束者的 `afterEach` 会摘掉后结束者仍在使用的监听。
 * 这个禁用条件随本条登记一起失效——将来若需要并发，先把这里换成一个 `Set`。
 */
let activeUnhandledRejectionObserver: ((reason: unknown) => void) | null = null

afterEach(() => {
  if (activeUnhandledRejectionObserver === null) return
  process.off('unhandledRejection', activeUnhandledRejectionObserver)
  activeUnhandledRejectionObserver = null
})

describe('C1 范围选择与首次加载', () => {
  it('三级下拉都渲染，范围未选全时不发起请求、不进入可写状态', async () => {
    renderPage()

    expect(scopeSelect('组织')).toBeInTheDocument()
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    // 范围不完整：适配层会拒绝构造请求，页面据此不下发部分范围查询，也不退化为全局查询。
    expect(query()).not.toHaveBeenCalled()
    expect(screen.getByText('请选择组织、医院与院区后查看互认项目金额')).toBeInTheDocument()
    expect(refreshButton()).toBeDisabled()
    expect(screen.queryByRole('button', { name: /维护金额/ })).not.toBeInTheDocument()
  })

  it('C1：依次切换组织、医院、院区都按新范围重载，且不自动发起写请求', async () => {
    const first = armQuery()
    renderPage()
    await selectOrganization()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await screen.findByText('血常规')

    const second = armQuery()
    await pickScopeOption('医院', 'HOS-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow({ standardProjectCode: 'B01', standardProjectName: '尿常规' })])

    const third = armQuery()
    await pickScopeOption('院区', 'BRA-3')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(third, [amountRow({ standardProjectCode: 'C01', standardProjectName: '便常规' })])

    // C2 的组件面：每次查询都携带组织、医院、院区三个值，且随范围变化按新值重发。
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' },
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-3' },
    ])
    expect(save()).not.toHaveBeenCalled()
    expect(api.saveBranchRecognitionAmount).not.toHaveBeenCalled()
  })
})

describe('C5 列表字段集合', () => {
  it('列只含契约交付的九项与操作列，不展示三级编码与最后修改信息', async () => {
    await renderLoaded([amountRow()])

    expect(columnHeaders()).toEqual([
      '标准项目编码',
      '标准项目名称',
      '组织名称',
      '医院名称',
      '院区名称',
      '当前金额',
      '金额状态',
      '互认配置状态',
      '当前不可用原因',
      '操作',
    ])
    for (const absent of ['组织编码', '医院编码', '院区编码', '最后修改时间', '最后修改人']) {
      expect(columnHeaders()).not.toContain(absent)
    }
  })

  it('名称一律取服务端返回值，不用编码冒充名称', async () => {
    await renderLoaded([amountRow({
      standardProjectName: '服务端项目名',
      organizationName: '服务端组织名',
      hospitalName: '服务端医院名',
      branchName: '服务端院区名',
      unavailableReason: '所属分组已停用',
    })])

    expect(screen.getByText('服务端项目名')).toBeInTheDocument()
    expect(screen.getByText('服务端组织名')).toBeInTheDocument()
    expect(screen.getByText('服务端医院名')).toBeInTheDocument()
    expect(screen.getByText('服务端院区名')).toBeInTheDocument()
    // 当前不可用原因直接取服务端字段。
    expect(screen.getByText('所属分组已停用')).toBeInTheDocument()
    // 名称列不得用编码顶替：编码只出现在标准项目编码列。
    expect(screen.getAllByText('A01')).toHaveLength(1)
  })
})

describe('C6 / C7 未配置与零元的呈现差异', () => {
  it('未配置行显示「未配置」且无数值，零元按已配置金额呈现 0.00', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '零元项目', currentAmount: node(0), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    const [zeroRow, unconfiguredRow] = dataRows()

    // 零元：金额列是已配置的金额文本，金额状态为已配置，行内不出现「未配置」。
    expect(within(zeroRow).getByText('0.00')).toBeInTheDocument()
    expect(within(zeroRow).getByText('已配置')).toBeInTheDocument()
    expect(within(zeroRow).queryByText('未配置')).toBeNull()

    // 未配置：金额列与金额状态都表达「未配置」，且整行不出现任何数值金额。
    expect(within(unconfiguredRow).getAllByText('未配置')).toHaveLength(2)
    expect(within(unconfiguredRow).queryByText('0.00')).toBeNull()
    expect(within(unconfiguredRow).queryByText('已配置')).toBeNull()
    expect((unconfiguredRow.textContent ?? '').match(/\d+\.\d{2}/)).toBeNull()
  })
})

describe('C8 金额输入校验', () => {
  it('负数、三位小数、双精度无法精确表示的大额与空值都只给出行内提示，零写请求且保留弹窗与输入', async () => {
    await renderLoaded([amountRow()])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    expect(amountInput(dialog).value).toBe('12.50')

    // 本循环的非法判据只取适配层出口 `parseRecognitionAmount`（页面不另写金额规则），其中
    // `99999999999999.99` 由该出口的「内存面 / 序列化面」等价性判据决定：该出口一旦放宽或收紧，
    // 本循环的输入集合必须同步（见 recognitionAmountsApi.test.ts 的金额解析用例）。
    for (const invalid of ['-1', '1.234', '99999999999999.99']) {
      await change(amountInput(dialog), invalid)
      click(okButton(dialog))
      await screen.findByText('金额必须是不小于 0 且最多两位小数的数字')
      // 行内提示与 `invalid` 标记：校验在字段变更后重新求值，标记随之更新。
      await waitFor(() => expect(amountInput(dialog)).toHaveAttribute('aria-invalid', 'true'))
      expect(save()).not.toHaveBeenCalled()
      expect(modalExists(AMOUNT_MODAL)).toBe(true)
      expect(amountInput(dialog).value).toBe(invalid)
    }

    await change(amountInput(dialog), '')
    click(okButton(dialog))
    await screen.findByText('金额必填')
    await waitFor(() => expect(amountInput(dialog)).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists(AMOUNT_MODAL)).toBe(true)
    expect(amountInput(dialog).value).toBe('')
  })
})

describe('C9 / C12 金额保存成功', () => {
  it('回填当前值后不改直接提交仍发出写请求，成功后关闭弹窗并整表重载', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    expect(amountInput(dialog).value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    click(okButton(dialog))

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    // C2：保存请求同样携带组织、医院、院区三个值；载荷只经适配层出口构造。
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    await expectModalClosed(AMOUNT_MODAL)
    await screen.findByText('20.00')
    expect(screen.queryByText('12.50')).not.toBeInTheDocument()
  })
})

describe('C10 金额保存被拒绝', () => {
  it('保留弹窗与输入、不重载、不误报成功，提交中状态归零，且写失败的拒绝不被页面吞掉', async () => {
    /**
     * 未处理拒绝的观测在本用例内**局部**安装、用例结束即移除（`finally`）：既不依赖也不改动
     * `vite.config.ts` 的全局忽略，因此「页面不得自行捕获写失败」在本用例内是可判红的。
     * `process` 的 `unhandledRejection` 是 Node 层的判定点，jsdom 的 `window` 事件不承接它；
     * vitest 检测到用户监听器后不再自行上报该事件（视为用户自行处理），所以本用例的观测就是这笔
     * 拒绝的唯一判据，用例内的断言必须自己承担判红责任。
     */
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded([amountRow()])

      click(amountEntry('血常规'))
      const dialog = await findModal(AMOUNT_MODAL)
      await change(amountInput(dialog), '18.00')

      // 写失败会让提交 promise 进入拒绝态；该 promise 交给 antd Modal 的 `onOk`，antd 不消费返回值，
      // 于是产生 unhandled rejection。按 Frontend API Client 第 5 节页面**不得**为此新增捕获或提示
      // （错误由宿主统一展示），所以这里期待的正是「本次提交的拒绝未被页面吞掉」：观测必须恰好收到
      // 写失败这一笔。页面若自行捕获、改判为成功或额外产生别的拒绝，下面的断言即判红。
      const rejection = new Error('业务拒绝')
      save().mockRejectedValueOnce(rejection)
      click(okButton(dialog))
      await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      expect(modalExists(AMOUNT_MODAL)).toBe(true)
      expect(amountInput(dialog).value).toBe('18.00')
      // 不刷新列表、不出现写入成功的提示。
      expect(query()).toHaveBeenCalledTimes(1)
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
      await waitFor(() => expect(okButton(dialog)).not.toHaveClass('ant-btn-loading'))
      expect(okButton(dialog)).toBeEnabled()
      // 收尾再判一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏判。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })
})

describe('C13 写成功但随后的刷新失败', () => {
  it('按写成功呈现并提示「写入已成功，数据待刷新」，提供重试且不改判为保存失败', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    await change(amountInput(dialog), '20.00')

    save().mockResolvedValueOnce(undefined)
    const failing = armQuery()
    click(okButton(dialog))

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failQuery(failing)

    await screen.findByText('写入已成功，数据待刷新')
    // 写入已成功：弹窗关闭，未出现任何保存失败提示，已有数据保留。
    await expectModalClosed(AMOUNT_MODAL)
    expect(screen.queryByText(/保存失败|提交失败/)).not.toBeInTheDocument()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(screen.getByLabelText('维护金额：血常规')).toBeDisabled()
    expect(save()).toHaveBeenCalledTimes(1)

    const retry = armQuery()
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    await screen.findByText('20.00')
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText('维护金额：血常规')).toBeEnabled())
  })
})

describe('C14 读取失败与重试', () => {
  it('保留已有数据、加载期间与失败后都禁用写入，重试成功后恢复', async () => {
    await renderLoaded([amountRow()])

    const failing = armQuery()
    click(refreshButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 加载期间禁用写入（刷新按钮与行内金额维护入口都不可用）。
    expect(refreshButton()).toBeDisabled()
    expect(screen.getByLabelText('维护金额：血常规')).toBeDisabled()

    await failQuery(failing)
    await screen.findByText('互认项目金额数据待刷新')

    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByLabelText('维护金额：血常规')).toBeDisabled()
    click(screen.getByLabelText('维护金额：血常规'))
    expect(modalExists(AMOUNT_MODAL)).toBe(false)
    expect(save()).not.toHaveBeenCalled()

    const retry = armQuery()
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow()])

    await waitFor(() => expect(screen.getByLabelText('维护金额：血常规')).toBeEnabled())
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
  })
})

describe('C16 排序与不分页', () => {
  it('C16：服务端交付的标准项目编码升序顺序原样渲染，页面不分页', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二' }),
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三' }),
    ])

    expect(renderedCodes()).toEqual(['A01', 'A02', 'B01'])
    expect(document.querySelector('.ant-pagination')).toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('页面不重排服务端交付的行顺序（升序由服务端保证）', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三' }),
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一' }),
    ])

    expect(renderedCodes()).toEqual(['B01', 'A01'])
  })
})

describe('C17 空态', () => {
  it('查询成功但当前范围没有互认配置时显示空态文案，且只读取一次', async () => {
    await renderLoaded([])

    expect(screen.getByText('当前范围没有互认项目金额配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(dataRows()).toHaveLength(0)
  })
})

describe('C21 范围切换与未保存输入', () => {
  it('切换院区时未保存的金额输入与旧范围数据一并清空，不带旧输入提交', async () => {
    await renderLoaded([amountRow()])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    await change(amountInput(dialog), '99')

    const next = armQuery()
    await pickScopeOption('院区', 'BRA-4')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })

    // 未保存输入随弹窗卸载被清空，旧范围数据不再展示，且没有任何写请求。
    expect(modalExists(AMOUNT_MODAL)).toBe(false)
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')

    // 重新打开时回填的是新范围当前值，不是切换前未提交的输入。
    click(amountEntry('新院区项目'))
    const reopened = await findModal(AMOUNT_MODAL)
    expect(amountInput(reopened).value).toBe('12.50')
  })
})

describe('C23 组件自身加载失败提示', () => {
  it('选项读取失败时组件给出自己的提示，页面不重复提示也不进入可写状态', async () => {
    const messageError = vi.spyOn(message, 'error')
    baseClient.api.organization.queryAllOrganization.post.mockRejectedValueOnce(new Error('组织选项读取失败'))

    renderPage()

    // 组件自身的 antd 静态 message.error 生效，且只提示一次（页面不重复提示同一失败）。
    await waitFor(() => expect(messageError).toHaveBeenCalledTimes(1))
    expect(messageError).toHaveBeenCalledWith('组织选项读取失败')

    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /维护金额/ })).not.toBeInTheDocument()
    expect(refreshButton()).toBeDisabled()
  })
})
