/**
 * 阶段 3「本院区互认项目金额」医院管理员页 Component 层用例
 * （测试矩阵 C3-C10、C12-C13、C15-C17、C21、C27-C36 的组件面）。
 *
 * 装配边界（与平台管理员页用例同一口径）：
 * 1. `./recognitionAmountsApi` 只替换 4 个 I/O 出口，其余纯函数使用真实实现；行夹具由真实
 *    `toRecognitionAmountRows` 从读模型形态构造，表格行序由真实 `toRecognitionAmountColumnView` 决定；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client；
 * 3. `@dy/components-base` 只替换整个包：该包 ESM 产物内部使用无扩展名相对导入，vitest 默认把
 *    node_modules 依赖交给 Node 解析，组件层无法加载真实实现（需 `test.server.deps.inline`，
 *    而 `vite.config.ts` 不属于本票所有权）。替身复现页面接线依赖的组件行为：受控 `value`、
 *    `orgId`/`hosId` 固定后对应级不渲染、初始选项加载完成后按 `selectFirstOption` 在院区取值为空时
 *    取首项并回传完整范围值与选项清单（页头名称与首项默认的取值来源）、用户改动按可选项级联回填
 *    下游首项并回传完整范围值、选项读取失败时调用 antd 静态 `message.error`。组织固定时组件不再
 *    读取组织清单，组织名称即其编码（与真实组件同一交付）。真实选择器的行为属宿主层验收面
 *    （C3 的 Host 层级）；
 * 4. 可信范围沿用既有包：`@dy/auth` 的 `LoginUserManager.getOrgID()`/`getHosID()`/`getBranchID()`
 *    与 `@dy/auth-react` 的 `useAuth()` token。这里替换这两个包以构造四份身份上下文：
 *    组织+医院+院区齐全、可信院区缺失、可信组织或医院缺失（另含 token 缺失）。
 *    替身的 `useAuth` 订阅本文件的登录态并在通知后重新求值（与真实包同形），因此**挂载之后**的可信
 *    范围变化可观察：token 后到、可信医院换绑、登录态失效都按真实路径触发页面重新求值。
 *
 * 列表字段集合、未配置与零元的呈现差异、层级列视图、排序、空态、状态筛选本地裁剪、地址栏参数同步、
 * 页脚计数与读取在途写门控都与平台管理员页同款式，本文件按同一写法分别覆盖医院管理员页的实现
 * （查询只携带院区与可选关键字，保存只携带院区、标准项目编码与金额）；金额行内编辑（C32）、可信范围
 * 变化确认框（C33）、提交前现值复核（C34）、金额校验与保存成功、拒绝、写成功但刷新失败都改用与平台
 * 管理员页同一套行内锚点，断言语义按 Client/testPlan.md 的 C8-C10、C12、C13、C32-C34 条逐条覆盖。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import type { BaseScopeSelectorProps, BaseScopeValue } from '@dy/components-base'
import { toRecognitionAmountColumnView, toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
import { BranchRecognitionAmounts } from './BranchRecognitionAmounts'

/** 伪 Base API Client 的注入点：与真实包 `configureBaseComponents` 同形（替身见文件头第 3 条）。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

/**
 * 可信登录态（组织、医院、院区、token）；每条用例按需改成缺失上下文，或在挂载后变更。
 * `version` 是订阅快照：登录态任何一次通知都推进它，让订阅方按新的可信范围重新求值。
 */
const authState = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  return {
    organizationCode: 'ORG-A' as string | null,
    hospitalCode: 'HOS-1' as string | null,
    branchCode: 'BRA-1' as string | null,
    token: 'token-1' as string | null,
    version: 0,
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
    /** 通知订阅者登录态已变化（真实包在登录、登出与凭据变更时发出同类通知）。 */
    notify() {
      authState.version += 1
      for (const listener of listeners) listener()
    },
    reset() {
      authState.organizationCode = 'ORG-A'
      authState.hospitalCode = 'HOS-1'
      authState.branchCode = 'BRA-1'
      authState.token = 'token-1'
      authState.notify()
    },
  }
})

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

  /** 替身回传的选项清单：只承载页头名称与首项默认解析用到的三级选项。 */
  interface StubScopeMeta {
    organizations: ScopeOption[]
    hospitals: ScopeOption[]
    branches: ScopeOption[]
    depts: ScopeOption[]
    users: ScopeOption[]
  }

  /**
   * 替身 props：在真实组件 props 上把 onChange 的选项清单换成替身自己的选项形状
   * （真实清单元素是 Kiota 生成的 DTO，替身只复现页面读取的 id 与 name 两个字段）。
   */
  type StubScopeSelectorProps = Omit<BaseScopeSelectorProps, 'onChange'> & {
    onChange?: (value: BaseScopeValue, meta: StubScopeMeta) => void
  }

  /** 范围选择器替身：接线行为与真实组件一致的部分见文件头第 3 条。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, selectFirstOption, onChange }: StubScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const loadedScopeKeyRef = useRef('')
    useEffect(() => {
      // 挂载时读取一次选项；固定的组织或医院变化时也重新读取（与真实组件同口径）：可信范围换绑后
      // 必须交付新范围内的医院与院区，否则替身会一直展示旧范围的选项。用 ref 守卫守住加载键的单次
      // 语义，依赖数组保持完整。
      const loadKey = `${orgId ?? ''}|${hosId ?? ''}|${value.hosId ?? ''}`
      if (loadedScopeKeyRef.current === loadKey) return
      loadedScopeKeyRef.current = loadKey
      const client = baseClientRef.current as ScopeClient
      const load = async () => {
        try {
          if (!orgId) {
            // 未固定组织：初始只加载组织清单，医院与院区在变更时按级联加载（与真实组件同形）。
            setOrganizations(await client.api.organization.queryAllOrganization.post() ?? [])
            return
          }
          // 组织固定：与真实组件同一交付——组织清单即该组织自身（名称取编码），医院清单按组织读取，
          // 院区清单按固定的医院过滤。
          setOrganizations([{ id: orgId, name: orgId }])
          const nextHospitals = await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId }) ?? []
          setHospitals(nextHospitals)
          const allBranches = await client.api.organization.queryAllValidBranchByOrgId.post({ orgId }) ?? []
          const nextBranches = allBranches.filter((branch) => branch.hosId === (hosId ?? value.hosId))
          setBranches(nextBranches)
          const resolvedHosId = hosId ?? value.hosId
          // selectFirstOption：院区取值为空时取首项并回传完整范围值与选项清单（页头名称与首项默认
          // 的取值来源）；院区取值非空时组件不越权改写受控值。
          if (selectFirstOption && !value.branchId && resolvedHosId && nextBranches[0]?.id) {
            onChange?.(
              { orgId, hosId: resolvedHosId, branchId: nextBranches[0].id },
              { organizations: [{ id: orgId, name: orgId }], hospitals: nextHospitals, branches: nextBranches, depts: [], users: [] },
            )
          }
        } catch (error) {
          antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        }
      }
      void load()
    }, [hosId, onChange, orgId, selectFirstOption, value.branchId, value.hosId])

    /** 变更后按可选项级联回填下游首项（与真实组件同形），并把完整范围值与当前选项清单回传页面。 */
    const handleChange = async (changed: 'org' | 'hospital' | 'branch', next: string) => {
      const client = baseClientRef.current as ScopeClient
      const merged = { ...value }
      let nextHospitals = hospitals
      let nextBranches = branches
      try {
        if (changed === 'org') {
          merged.orgId = next.length > 0 ? next : undefined
          nextHospitals = merged.orgId
            ? await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          setHospitals(nextHospitals)
          merged.hosId = hosId ?? nextHospitals[0]?.id ?? undefined
          const allBranches = merged.orgId
            ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
          setBranches(nextBranches)
          merged.branchId = nextBranches[0]?.id ?? undefined
        } else if (changed === 'hospital') {
          merged.hosId = next.length > 0 ? next : undefined
          const allBranches = merged.orgId
            ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
          setBranches(nextBranches)
          merged.branchId = nextBranches[0]?.id ?? undefined
        } else {
          merged.branchId = next.length > 0 ? next : undefined
        }
      } catch (error) {
        antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        return
      }
      onChange?.(merged, { organizations, hospitals: nextHospitals, branches: nextBranches, depts: [], users: [] })
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

    // 三个子节点按位置各占一个参数位；数组形式的子元素需要 key，按位置传入满足该要求，stderr 上不出现缺 key 的告警淹没真实告警。
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

const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

vi.mock('@dy/auth', () => ({
  LoginUserManager: {
    getOrgID: () => authState.organizationCode,
    getHosID: () => authState.hospitalCode,
    getBranchID: () => authState.branchCode,
  },
}))

vi.mock('@dy/auth-react', async () => {
  const { useSyncExternalStore } = await import('react')
  return {
    /** 与真实包同形：订阅登录态，通知后重新求值；token 仍是页面判定可信范围是否可用的依据。 */
    useAuth: () => {
      useSyncExternalStore(authState.subscribe, () => authState.version)
      return {
        isAuthenticated: authState.token !== null,
        token: authState.token,
        login: () => {},
        logout: () => {},
      }
    },
  }
})

const HOSPITALS = [{ id: 'HOS-1', name: '医院一' }, { id: 'HOS-2', name: '医院二' }]
const BRANCHES = [
  { id: 'BRA-1', name: '院区一', hosId: 'HOS-1' },
  { id: 'BRA-2', name: '院区二', hosId: 'HOS-1' },
  { id: 'BRA-3', name: '院区三', hosId: 'HOS-1' },
]

function createBaseClient(branchList: typeof BRANCHES = BRANCHES) {
  return {
    api: {
      organization: {
        queryAllOrganization: { post: vi.fn(async () => [{ id: 'ORG-A', name: '组织一' }]) },
        queryAllValidHospitalByOrgId: { post: vi.fn(async () => HOSPITALS) },
        queryAllValidBranchByOrgId: { post: vi.fn(async () => branchList) },
      },
    },
  }
}

let baseClient: ReturnType<typeof createBaseClient>

const node = (value: unknown) => ({ value, getValue: () => value })

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

const query = () => api.queryBranchRecognitionAmountRows
const save = () => api.saveBranchRecognitionAmount

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

/** 院区下拉是医院管理员页唯一渲染的范围下拉。 */
const branchSelect = () => screen.getByLabelText('范围-院区') as HTMLSelectElement

async function pickBranch(value: string) {
  const select = branchSelect()
  // 选项由伪 Base Client 异步交付：等选项就绪再选，避免构造出「选了不存在的值」这种非真实路径。
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await change(select, value)
}

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

/** 行内编辑器（C32）的金额输入：编辑态以可访问名称「金额」呈现，收起后即从文档消失。 */
const inlineAmountInput = () => screen.getByLabelText('金额') as HTMLInputElement
const queryInlineAmountInput = () => screen.queryByLabelText('金额')

/** 回车与 Esc 走异步 act：回车提交会启动写入与随后的整表重载，需要让微任务就地排空。 */
const pressKey = (element: HTMLElement, key: string) => act(async () => {
  fireEvent.keyDown(element, { key })
})
const pressEnter = (element: HTMLElement) => pressKey(element, 'Enter')
const pressEscape = (element: HTMLElement) => pressKey(element, 'Escape')

/** 目标行：按项目名称在当前行集里定位；行离开当前行集即视为编辑目标失效（C32）。 */
function rowOf(standardProjectName: string): HTMLElement {
  const row = dataRows().find((candidate) => candidate.textContent?.includes(standardProjectName))
  if (!row) throw new Error(`行未找到：${standardProjectName}`)
  return row
}

/** 当前金额格：值文本或「未配置」标记所在按钮，行内编辑的入口（C32）。 */
function amountValueCell(standardProjectName: string): HTMLElement {
  const cell = rowOf(standardProjectName).querySelector<HTMLElement>('.recognition-amounts-number')
  if (!cell) throw new Error(`当前金额格未找到：${standardProjectName}`)
  return cell
}

const editAmount = (standardProjectName: string) => click(amountValueCell(standardProjectName))

/** C33：可信范围变化确认框的标题；待切换提示态以页面样式类呈现（RecognitionAmounts.css）。 */
const CONFIRM_TRUSTED_SCOPE = '可信范围已变化'
const promptElement = () => document.querySelector<HTMLElement>('.recognition-amounts-prompt')

/** 搜索主按钮与关键字输入框：读取在途时主按钮呈加载态，关键字只在提交时下推服务端。 */
const searchButton = () => screen.getByRole('button', { name: /搜\s*索/ })
const keywordInput = () => screen.getByLabelText('关键字') as HTMLInputElement

/** 两个状态 Segmented 把外部 aria-label 透传到 role=radiogroup（与互认项目配置页同一手法）。 */
const amountStatusFilter = () => screen.getByRole('radiogroup', { name: '金额状态筛选' })
const configurationStatusFilter = () => screen.getByRole('radiogroup', { name: '互认配置状态筛选' })

const selectedSegmentLabels = (group: HTMLElement) =>
  [...group.querySelectorAll('.ant-segmented-item-selected')].map((node) => node.textContent?.trim())

/** 点击 Segmented 的某个档位；档位只做本地裁剪，不触发读取。 */
const pickSegment = (group: HTMLElement, label: string) => click(within(group).getByText(label))

const dataRows = () =>
  screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))

const columnHeaders = () => screen.getAllByRole('columnheader').map((node) => node.textContent)

/** 项目格里的编码文本：按页面实际渲染顺序取，列视图的平铺顺序即页面行序。 */
const renderedCodes = () =>
  Array.from(document.querySelectorAll<HTMLElement>('.recognition-amounts-project-code'))
    .map((node) => node.textContent)

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><BranchRecognitionAmounts /></ConfigProvider>)
}

/** 进入页面并让列表成功交付给定行（读取结束后搜索主按钮恢复可用）。 */
async function renderLoaded(rows: RecognitionAmountRow[]) {
  const pending = armQuery()
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(pending, rows)
  await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
}

/** 挂载后变更可信登录态并通知订阅者：构造 token 后到、可信医院换绑、登录态失效等真实路径。 */
async function changeTrustedScope(
  patch: Partial<Record<'organizationCode' | 'hospitalCode' | 'branchCode' | 'token', string | null>>,
) {
  await act(async () => {
    Object.assign(authState, patch)
    authState.notify()
  })
}

beforeEach(() => {
  authState.reset()
  for (const fn of Object.values(api)) fn.mockReset()
  // 列表读取缺省给出明确拒绝：用例须先 armQuery 排队，未排队的读取以可读的失败暴露，避免以 undefined 静默通过。
  query().mockRejectedValue(new Error('用例未排队列表读取：先调用 armQuery'))
  clientRef.current = {}
  baseClient = createBaseClient()
  // 地址栏回到缺省状态：上一个用例的已生效条件不得泄漏进本用例。
  window.history.replaceState(null, '', '/')
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
})

/**
 * 用例内未处理拒绝观测器的兜底登记：用例卡住或超时时用例内的 `finally` 不会执行，而
 * `unhandledRejection` 是进程级监听，残留后会让 vitest 以「监听器数 > 1」认定调用方自行处理、
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

describe('C3 可信范围与院区选择', () => {
  it('组织与医院只读固定且不渲染下拉，只渲染院区下拉，院区默认取可信院区', async () => {
    const pending = armQuery()
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 只渲染院区一级：固定传入的组织与医院不渲染对应下拉。
    expect(screen.queryByLabelText('范围-组织')).toBeNull()
    expect(screen.queryByLabelText('范围-医院')).toBeNull()
    expect(screen.getByLabelText('范围-院区')).toBeInTheDocument()
    // 查询只按可信上下文里的院区构造，不提交组织与医院。
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })

    await settle(pending, [amountRow()])

    await waitFor(() => expect(branchSelect().value).toBe('BRA-1'))
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区一')
  })

  it('C4：查询只提交院区与可选关键字，不含组织与医院', async () => {
    await renderLoaded([amountRow()])

    const firstBody = query().mock.calls[0][1] as Record<string, unknown>
    expect(firstBody).toEqual({ branchCode: 'BRA-1' })
    expect(firstBody).not.toHaveProperty('organizationCode')
    expect(firstBody).not.toHaveProperty('hospitalCode')
    // 平台管理员入口在该页不被使用。
    expect(api.queryRecognitionAmountRows).not.toHaveBeenCalled()

    // 关键字提交后请求只多带关键字字段，仍然不含组织与医院。
    const second = armQuery()
    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    const secondBody = query().mock.calls[1][1] as Record<string, unknown>
    expect(secondBody).toEqual({ branchCode: 'BRA-1', keyword: '尿常规' })
    expect(secondBody).not.toHaveProperty('organizationCode')
    expect(secondBody).not.toHaveProperty('hospitalCode')
    await settle(second, [amountRow()])
  })

  it('C4：保存只提交院区、标准项目编码与金额，可信组织与医院不回填进请求体', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    const payload = save().mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ branchCode: 'BRA-1', standardProjectCode: 'A01', currentAmount: 12.5 })
    expect(Object.keys(payload)).toEqual(['branchCode', 'standardProjectCode', 'currentAmount'])
    expect(payload).not.toHaveProperty('organizationCode')
    expect(payload).not.toHaveProperty('hospitalCode')
    expect(api.saveOrganizationHospitalBranchRecognitionAmount).not.toHaveBeenCalled()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })

  it('C3 可信范围在首屏之后才就绪（token 后到）：院区默认值随可信院区回填并据此查询', async () => {
    // 挂载时凭据尚无 token：可信范围不可用，整页阻断、零业务请求、不渲染院区下拉。
    authState.token = null
    authState.organizationCode = null
    authState.hospitalCode = null
    authState.branchCode = null
    renderPage()
    await screen.findByText('可信范围不可用')
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    expect(query()).not.toHaveBeenCalled()

    // token 后到，可信范围随后才可用：默认院区随可信范围同步回填，首屏的空值随之被取代。
    const pending = armQuery()
    await changeTrustedScope({ token: 'token-1', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-1'))
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区一')

    await settle(pending, [amountRow()])
    await screen.findByText('血常规')
  })

  it('C3 可信医院变更：院区不残留旧范围的值，按新可信院区查询', async () => {
    // 新可信医院下的院区由伪 Base Client 交付：替身按固定的组织与医院范围读取选项。
    baseClient = createBaseClient([...BRANCHES, { id: 'BRA-9', name: '院区九', hosId: 'HOS-2' }])
    configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })

    await renderLoaded([amountRow()])
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-1'))

    // 可信医院换绑，并交付该医院下的可信院区：旧范围的院区与旧范围的行都不得残留。
    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-9'))
    expect(branchSelect().value).not.toBe('BRA-1')
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区九')

    await settle(next, [amountRow({ standardProjectCode: 'B01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
  })

  it('C3 可信范围变为不可用：院区清空、整页阻断，恢复后按新可信院区重新取值', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await waitFor(() => expect(branchSelect().value).toBe('BRA-1'))

    // 登录态失效：可信组织与医院不可用，整页阻断，院区不残留旧范围的值。
    await changeTrustedScope({ token: null, organizationCode: null, hospitalCode: null, branchCode: null })

    await screen.findByText('可信范围不可用')
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)

    // 恢复登录态且可信院区换为另一个值：必须按新可信范围取值，不回填失效前的院区。
    const next = armQuery()
    await changeTrustedScope({ token: 'token-2', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-2' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-2'))
    await settle(next, [amountRow()])
    await screen.findByText('血常规')
  })

  it('C3 用户显式选择的院区不被可信默认值覆盖', async () => {
    await renderLoaded([amountRow()])
    expect(branchSelect().value).toBe('BRA-1')

    // 用户显式选择院区二：此后同一可信范围内不得被默认可信院区盖回。
    const selected = armQuery()
    await pickBranch('BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })
    await settle(selected, [amountRow()])

    // 宿主重新签发 token 但可信院区未变（仍在同一可信组织与医院内）：显式选择保持不变，也不额外
    // 发起请求；默认可信院区只用于「用户尚未选择」时的回填，不得被当成用户的选择，也不得反过来
    // 盖回用户选择。可信院区真的变化时按 C33 走确认框或直接切换，不再静默保持（见 C33 用例）。
    await changeTrustedScope({ branchCode: 'BRA-1' })
    expect(branchSelect().value).toBe('BRA-2')
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区二')
    expect(query()).toHaveBeenCalledTimes(2)
    expect(save()).not.toHaveBeenCalled()
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
  })
})

describe('C15 可信组织或医院缺失', () => {
  it('阻断提示、不渲染院区下拉、零业务请求，也不伪造组织或降级为空值', async () => {
    authState.organizationCode = null
    const missingOrganization = renderPage()
    await screen.findByText('可信范围不可用')
    expect(screen.getByText(/不能读取或维护本院区互认项目金额/)).toBeInTheDocument()
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByRole('button', { name: /维护金额/ })).not.toBeInTheDocument()
    missingOrganization.unmount()

    authState.reset()
    authState.hospitalCode = '   '
    const missingHospital = renderPage()
    await screen.findByText('可信范围不可用')
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    missingHospital.unmount()

    authState.reset()
    authState.token = null
    renderPage()
    await screen.findByText('可信范围不可用')
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
  })
})

describe('C5 医院管理员页的列表字段集合', () => {
  it('与平台管理员页同源：列为类型、分类、分组、项目与四个业务列，不展示三级名称与编码等字段', async () => {
    await renderLoaded([amountRow()])

    expect(columnHeaders()).toEqual([
      '类型',
      '分类',
      '分组',
      '项目',
      '当前金额',
      '金额状态',
      '互认配置状态',
      '当前不可用原因',
    ])
    for (const absent of [
      '标准项目编码',
      '标准项目名称',
      '组织名称',
      '医院名称',
      '院区名称',
      '组织编码',
      '医院编码',
      '院区编码',
      '项目类型',
      '最后修改时间',
      '最后修改人',
      // 每行金额即行内编辑入口，不再设操作列。
      '操作',
    ]) {
      expect(columnHeaders()).not.toContain(absent)
    }
  })

  it('项目名称取服务端返回值，三级名称不进表格，编码只出现一次', async () => {
    await renderLoaded([amountRow({
      standardProjectName: '服务端项目名',
      organizationName: '服务端组织名',
      hospitalName: '服务端医院名',
      branchName: '服务端院区名',
      unavailableReason: '所属分组已停用',
    })])

    expect(screen.getByText('服务端项目名')).toBeInTheDocument()
    // 三级名称不进表格：页头范围展示取范围选择器的选项名称，服务端行内三级名称一律不展示。
    expect(screen.queryByText('服务端组织名')).not.toBeInTheDocument()
    expect(screen.queryByText('服务端医院名')).not.toBeInTheDocument()
    expect(screen.queryByText('服务端院区名')).not.toBeInTheDocument()
    expect(screen.getByText('所属分组已停用')).toBeInTheDocument()
    // 项目格内编码不得重复出现：编码只出现在项目格。
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
  it('负数、三位小数、双精度无法精确表示的大额与空值都只给出行内提示，零写请求且保留编辑态与输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    // 本循环的非法输入集合只取适配层出口 `parseRecognitionAmount`（页面不另写金额规则），其中
    // `99999999999999.99` 由该出口的「内存面 / 序列化面」等价性规则决定：该出口一旦放宽或收紧，
    // 本循环的输入集合必须同步（见 recognitionAmountsApi.test.ts 的金额解析用例）。
    for (const invalid of ['-1', '1.234', '99999999999999.99']) {
      await change(inlineAmountInput(), invalid)
      await pressEnter(inlineAmountInput())
      await screen.findByText('金额必须是不小于 0 且最多两位小数的数字')
      // 行内提示与 `invalid` 标记：校验在字段变更后重新求值，标记随之更新。
      await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
      expect(save()).not.toHaveBeenCalled()
      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe(invalid)
    }

    await change(inlineAmountInput(), '')
    await pressEnter(inlineAmountInput())
    await screen.findByText('金额必填')
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('')
  })
})

describe('C9 / C12 金额保存成功', () => {
  it('回填当前值后不改直接提交仍发出写请求，成功后收起编辑态并整表重载', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    // C4：保存请求同样只携带院区与标准项目编码、金额；载荷只经适配层出口构造。
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
    expect(screen.queryByText('12.50')).not.toBeInTheDocument()
  })
})

describe('C10 金额保存被拒绝', () => {
  it('保留编辑态与输入、不重载、不误报成功，提交中状态归零，且写失败的拒绝不被页面吞掉', async () => {
    /**
     * 未处理拒绝的观测在本用例内**局部**安装、用例结束即移除（`finally`）：既不依赖也不改动
     * `vite.config.ts` 的全局忽略，因此「页面不得自行捕获写失败」在本用例内是可失败的。
     * `process` 的 `unhandledRejection` 是 Node 层的观测点，jsdom 的 `window` 事件不承接它；
     * vitest 检测到用户监听器后不再自行上报该事件（视为用户自行处理），所以本用例的观测就是这笔
     * 拒绝的唯一依据，用例内的断言必须自己承担失败责任。
     */
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded([amountRow()])

      editAmount('血常规')
      await change(inlineAmountInput(), '18.00')

      // 写失败会让本次提交的 promise 进入拒绝态；按 Frontend API Client 第 5 节页面**不得**
      // 捕获写失败或自行提示（拒绝文案由宿主统一展示），所以这里期待的正是「本次提交的拒绝未被
      // 页面吞掉」：观测必须恰好收到写失败这一笔。页面若自行捕获、改按成功处理或额外产生别的拒绝，
      // 下面的断言即失败。
      const rejection = new Error('业务拒绝')
      save().mockRejectedValueOnce(rejection)
      await pressEnter(inlineAmountInput())
      await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe('18.00')
      // 不刷新列表、不出现写入成功的提示，页面也不自建拒绝文案（拒绝由宿主展示）。
      expect(query()).toHaveBeenCalledTimes(1)
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('业务拒绝')).not.toBeInTheDocument()
      // 提交中状态归零：编辑态仍然可用，可以继续输入并重试。
      await waitFor(() => expect(inlineAmountInput()).toBeEnabled())
      // 收尾再确认一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏掉。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })
})

describe('C13 写成功但随后的刷新失败', () => {
  it('按写成功呈现并提示「写入已成功，数据待刷新」，提供重试且不按保存失败呈现', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '20.00')

    save().mockResolvedValueOnce(undefined)
    const failing = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failQuery(failing)

    await screen.findByText('写入已成功，数据待刷新')
    // 写入已成功：编辑态收起，未出现任何保存失败提示，已有数据保留。
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.queryByText(/保存失败|提交失败/)).not.toBeInTheDocument()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(amountValueCell('血常规')).toBeDisabled()
    expect(save()).toHaveBeenCalledTimes(1)

    const retry = armQuery()
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    await screen.findByText('20.00')
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
  })
})

describe('C16 排序与不分页', () => {
  it('C16：服务端交付的编码升序在列视图中原样渲染，页面不分页', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二' }),
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三' }),
    ])

    expect(renderedCodes()).toEqual(['A01', 'A02', 'B01'])
    expect(document.querySelector('.ant-pagination')).toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('页面按列视图的层级顺序平铺，不自行重排服务端交付的行', async () => {
    const rows = [
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', categoryName: '甲分类', groupName: '分组一' }),
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三', categoryName: '乙分类', groupName: '分组二' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', categoryName: '甲分类', groupName: '分组一' }),
    ]
    await renderLoaded(rows)

    // 期望顺序出自真实适配层出口 `toRecognitionAmountColumnView`：页面只按该视图平铺成表格行。
    const expectedCodes = toRecognitionAmountColumnView(rows).types.flatMap((type) =>
      type.categories.flatMap((category) =>
        category.groups.flatMap((group) => group.rows.map((row) => row.standardProjectCode)),
      ),
    )
    expect(expectedCodes).not.toEqual(rows.map((row) => row.standardProjectCode))
    expect(renderedCodes()).toEqual(expectedCodes)
  })
})

describe('C17 空态', () => {
  it('查询成功但当前范围零互认配置时显示零配置空态文案，且只读取一次', async () => {
    await renderLoaded([])

    expect(screen.getByText('当前范围没有互认项目金额配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(dataRows()).toHaveLength(0)
  })

  it('有条件但可见行未命中时显示未命中空态文案，且不追加读取', async () => {
    // 两行都是已配置：按「未配置」档裁剪后可见行为空，服务端结果本身非空。
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: node(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: node(20) }),
    ])

    pickSegment(amountStatusFilter(), '未配置')

    expect(screen.getByText('没有符合当前筛选条件的互认项目金额')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C21 院区切换与未保存输入', () => {
  it('切换院区时未保存的金额输入与旧院区数据一并清空', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    const next = armQuery()
    await pickBranch('BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })

    // 未保存输入随编辑态收起被清空，旧院区数据不再展示，且没有任何写请求；用户主动切换不弹确认框，
    // 也不进入待切换提示态。
    expect(queryInlineAmountInput()).toBeNull()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()

    await settle(next, [amountRow({ standardProjectCode: 'B01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')

    // 重新进入编辑时回填的是新范围当前值，切换前未提交的输入已清空。
    editAmount('新院区项目')
    expect(inlineAmountInput().value).toBe('12.50')
  })
})

describe('C27 关键字搜索', () => {
  it('提交下推关键字、服务端返回的行原样展示、清空后草稿与已生效关键字一并清空并按空条件重读', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '便常规' }),
    ])

    // 关键字只在提交时下推：输入草稿本身不追加读取，回车提交后请求体带上已生效关键字。
    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)
    const second = armQuery()
    fireEvent.keyDown(keywordInput(), { key: 'Enter' })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1', keyword: '尿常规' })

    // 行编码与关键字字面无交集的行仍全部可见：本地不再按关键字裁剪，行集原样按列视图平铺。
    await settle(second, [
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '便常规' }),
    ])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(renderedCodes()).toEqual(['A01', 'A02'])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('便常规')).toBeInTheDocument()

    // 清空输入框：草稿与已生效关键字同时清空，并触发一次不带关键字的空条件读取。
    const third = armQuery()
    await change(keywordInput(), '')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(keywordInput().value).toBe('')
    expect(query().mock.calls[2][1]).toEqual({ branchCode: 'BRA-1' })
    expect(query().mock.calls[2][1]).not.toHaveProperty('keyword')
    await settle(third, [amountRow()])
  })
})

describe('C28 状态筛选本地裁剪', () => {
  it('两个 Segmented 只在服务端结果上本地裁剪：切换即生效、不追加读取、不下推请求', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', isAmountConfigured: true, currentAmount: node(12.5), configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', isAmountConfigured: false, currentAmount: null, configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A03', standardProjectName: '项目三', isAmountConfigured: true, currentAmount: node(3), configurationStatus: 2 }),
    ])
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(query()).toHaveBeenCalledTimes(1)

    pickSegment(amountStatusFilter(), '未配置')
    expect(renderedCodes()).toEqual(['A02'])
    expect(selectedSegmentLabels(amountStatusFilter())).toEqual(['未配置'])

    pickSegment(amountStatusFilter(), '已配置')
    expect(renderedCodes()).toEqual(['A01', 'A03'])

    pickSegment(configurationStatusFilter(), '停用')
    expect(renderedCodes()).toEqual(['A03'])

    pickSegment(amountStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A03'])

    pickSegment(configurationStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])

    // 本地裁剪全程不追加读取，也不把两个状态条件写进请求。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
    ])
  })
})

describe('C29 地址栏参数同步', () => {
  it('进入按参数初始化，变化替换当前历史项，草稿不写入地址栏', async () => {
    window.history.replaceState(null, '', '/?keyword=血常规&amountStatus=unconfigured&configurationStatus=enabled')
    const first = armQuery()
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1', keyword: '血常规' },
    ])
    await settle(first, [amountRow({ standardProjectCode: 'A01', isAmountConfigured: false, currentAmount: null })])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    // 进入按参数初始化：草稿取已生效关键字，两个 Segmented 取地址栏档位。
    expect(keywordInput().value).toBe('血常规')
    expect(selectedSegmentLabels(amountStatusFilter())).toEqual(['未配置'])
    expect(selectedSegmentLabels(configurationStatusFilter())).toEqual(['启用'])
    expect(renderedCodes()).toEqual(['A01'])

    // 档位变化按替换当前历史项写回，且不追加读取。
    const historyLength = window.history.length
    pickSegment(amountStatusFilter(), '已配置')
    expect(new URLSearchParams(window.location.search).get('amountStatus')).toBe('configured')
    expect(window.history.length).toBe(historyLength)
    expect(query()).toHaveBeenCalledTimes(1)

    // 草稿不写入地址栏：未提交的关键字只留在输入框。
    await change(keywordInput(), '尿常规')
    expect(new URLSearchParams(window.location.search).get('keyword')).toBe('血常规')
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('提交才下推关键字并同步地址栏，清空动作清掉已生效关键字并触发一次空条件读取', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)

    // 回车提交：草稿成为已生效关键字，随请求下推并替换写入地址栏。
    const second = armQuery()
    fireEvent.keyDown(keywordInput(), { key: 'Enter' })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
      { branchCode: 'BRA-1', keyword: '尿常规' },
    ])
    expect(new URLSearchParams(window.location.search).get('keyword')).toBe('尿常规')
    await settle(second, [amountRow()])

    // 输入框清空动作同时清空草稿与已生效关键字，并触发一次空条件读取（等价于刷新）。
    const third = armQuery()
    await change(keywordInput(), '')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(keywordInput().value).toBe('')
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
      { branchCode: 'BRA-1', keyword: '尿常规' },
      { branchCode: 'BRA-1' },
    ])
    expect(new URLSearchParams(window.location.search).get('keyword')).toBeNull()
    await settle(third, [amountRow()])
  })
})

describe('C30 院区恒定单值与默认取值', () => {
  it('默认档一：可信院区属于当前院区清单时选中可信院区，不被首项覆盖', async () => {
    // 该院下首项是 BRA-1；可信院区 BRA-2 属于该清单，因此取可信院区。
    authState.branchCode = 'BRA-2'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-2' },
    ])
    await waitFor(() => expect(branchSelect()).toHaveValue('BRA-2'))
    await settle(first, [amountRow()])
  })

  it('默认档二：可信院区缺失时取该院下第一项，选项交付前不发任何请求', async () => {
    authState.branchCode = null
    const first = armQuery()
    renderPage()

    // 选项尚未交付：范围未解析，不发起请求，更不以空院区发起请求。
    expect(query()).not.toHaveBeenCalled()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
    ])
    await waitFor(() => expect(branchSelect()).toHaveValue('BRA-1'))
    await settle(first, [amountRow()])
  })

  it('默认档二：可信院区不在回调清单时取该院下第一项', async () => {
    // 可信院区有值但不属于选择器交付的院区清单（清单只有 BRA-1 ~ BRA-3），归属核对不上即取首项。
    authState.branchCode = 'BRA-9'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
    ])
    await waitFor(() => expect(branchSelect()).toHaveValue('BRA-1'))
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区一')
    await settle(first, [amountRow()])
  })

  it('用户显式改选的院区被接受，清空院区被拒绝且保持原受控值', async () => {
    await renderLoaded([amountRow()])
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-1' },
    ])

    const second = armQuery()
    await pickBranch('BRA-3')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-3' })
    await settle(second, [amountRow()])

    // 组件的清空动作不被接受：保持原受控值，不发起空院区请求。
    await pickBranch('')
    expect(branchSelect()).toHaveValue('BRA-3')
    expect(query()).toHaveBeenCalledTimes(2)
  })
})

describe('C31 页头三级范围展示', () => {
  it('选择器加载完成前不展示范围名称，完成后名称在上、编码在下且不设文字标签', async () => {
    const first = armQuery()

    renderPage()

    // 选择器加载完成前名称无从取值：页头整个范围展示块不出现。
    expect(document.querySelector('.recognition-amounts-header-scope')).toBeNull()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])

    await waitFor(() => expect(document.querySelector('.recognition-amounts-header-scope')).not.toBeNull())
    const block = document.querySelector('.recognition-amounts-header-scope') as HTMLElement
    const lines = Array.from(block.children) as HTMLElement[]
    expect(lines).toHaveLength(2)
    // 上下两行：名称在上、编码在下，整体右对齐由页头样式承载。
    expect(lines[0].className).toContain('recognition-amounts-scope-names')
    expect(lines[1].className).toContain('recognition-amounts-scope-codes')
    // 组织固定后组件不再读取组织清单，组织名称即其编码（与真实组件同一交付）；医院与院区取选项名称。
    for (const name of ['ORG-A', '医院一', '院区一']) {
      expect(within(lines[0]).getByText(name)).toBeInTheDocument()
    }
    for (const code of ['ORG-A', 'HOS-1', 'BRA-1']) {
      expect(within(lines[1]).getByText(code)).toBeInTheDocument()
    }
    // 不设「组织」这类文字标签：三级各自只有名称与编码。
    for (const label of ['组织', '医院', '院区']) {
      expect(within(block).queryByText(label, { exact: true })).toBeNull()
    }
  })

  it('切换院区后名称行与编码行仍指同一范围，读取在途期间页头不渲染过期名称', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await waitFor(() => expect(document.querySelector('.recognition-amounts-header-scope')).not.toBeNull())

    const second = armQuery()
    await pickBranch('BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 读取在途：搜索按钮呈加载态，页头两行已整体切到新院区，旧院区名称与编码都不出现。
    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言（与平台管理员页同一写法）。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    const inFlightBlock = document.querySelector('.recognition-amounts-header-scope') as HTMLElement
    const inFlightLines = Array.from(inFlightBlock.children) as HTMLElement[]
    expect(within(inFlightLines[0]).queryByText('院区一')).toBeNull()
    expect(within(inFlightLines[1]).queryByText('BRA-1')).toBeNull()
    expect(within(inFlightLines[0]).getByText('院区二')).toBeInTheDocument()
    expect(within(inFlightLines[1]).getByText('BRA-2')).toBeInTheDocument()

    await settle(second, [amountRow()])

    // 读取结束后两行仍指同一范围：名称行与编码行都取当前选定项（院区二 / BRA-2 收尾）。
    const block = document.querySelector('.recognition-amounts-header-scope') as HTMLElement
    const lines = Array.from(block.children) as HTMLElement[]
    expect(within(lines[0]).queryByText('院区一')).toBeNull()
    expect(within(lines[1]).queryByText('BRA-1')).toBeNull()
    for (const name of ['ORG-A', '医院一', '院区二']) {
      expect(within(lines[0]).getByText(name)).toBeInTheDocument()
    }
    for (const code of ['ORG-A', 'HOS-1', 'BRA-2']) {
      expect(within(lines[1]).getByText(code)).toBeInTheDocument()
    }
  })
})

describe('C32 行内编辑金额', () => {
  it('点击当前金额值与未配置标记进入编辑，Esc 取消后编辑态收起且金额不变', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: node(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')
    await change(inlineAmountInput(), '99')
    await pressEscape(inlineAmountInput())
    expect(queryInlineAmountInput()).toBeNull()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()

    // 「未配置」标记同样是入口，进入编辑时回填空值。
    editAmount('未配置项目')
    expect(inlineAmountInput().value).toBe('')
    await pressEscape(inlineAmountInput())
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('回车提交当前值（同值不改）仍发出写请求并收起编辑态', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.getByText('12.50')).toBeInTheDocument()
  })

  it('后台刷新改变同一行金额后编辑器以当前金额值为键重挂并回填新值', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '999')

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    // 目标行仍在当前行集：编辑态保留，但以新金额值重挂，回填值随当前行数据更新。
    await waitFor(() => expect(inlineAmountInput().value).toBe('20.00'))
    expect(save()).not.toHaveBeenCalled()
  })

  it('状态档把目标行裁出当前行集后不渲染编辑态', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: node(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    pickSegment(amountStatusFilter(), '未配置')
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('搜索把目标行移出当前行集后不渲染编辑态', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    await change(keywordInput(), 'ZZZ')
    const pending = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(pending, [amountRow({ standardProjectCode: 'B02', standardProjectName: '其他项目' })])
    await screen.findByText('其他项目')

    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
  })

  it('金额只接受非负且最多两位小数的数字输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    await change(inlineAmountInput(), '-1')
    await pressEnter(inlineAmountInput())
    await screen.findByText('金额必须是不小于 0 且最多两位小数的数字')
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)

    // 改成合法值后行内标记随之清除，编辑态继续保留。
    await change(inlineAmountInput(), '1.5')
    await waitFor(() => expect(inlineAmountInput()).not.toHaveAttribute('aria-invalid', 'true'))
    expect(inlineAmountInput().value).toBe('1.5')
  })
})

describe('C33 可信范围变化确认框', () => {
  /**
   * 可信范围变化用受控构造：编辑期间让页面现读的可信三层取值改变（医院页的可信三层取自
   * `LoginUserManager`，这里按宿主重签 token 的路径通知订阅者），生产代码无需提供注入点。
   * 变化判定与平台管理员页同一结构：渲染期派生为主、选择器回调为第二道。
   */
  it('有未保存内容时可信范围变化弹出确认框，放弃并切换即清空输入并按新范围加载', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    await changeTrustedScope({ branchCode: 'BRA-3' })
    const next = armQuery()
    await pickBranch('BRA-2')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // 确认框存续期间：既不加载新范围，也不发起写入，更不进入待切换提示态。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()
    expect(promptElement()).toBeNull()

    click(within(dialog).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })

    // 放弃即清空未保存输入，直接按新范围加载，没有残留的确认框与提示态。
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('确认框选取消后保留输入并进入待切换提示态，再次确认后按新范围加载', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    await changeTrustedScope({ branchCode: 'BRA-3' })
    const next = armQuery()
    await pickBranch('BRA-2')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // antd 对恰为两个汉字的按钮文案自动插入空格（与既有「保 存」同一手法），匹配时容忍该空格。
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))

    // 取消：保留未保存输入与编辑态，进入待切换提示态，范围尚未切换。
    expect(inlineAmountInput().value).toBe('99')
    expect(promptElement()).not.toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()

    // 提示态里再次确认：清空输入并按新范围加载，确认框不重复出现。
    const prompt = promptElement()
    expect(prompt).not.toBeNull()
    click(within(prompt as HTMLElement).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })
    await waitFor(() => expect(promptElement()).toBeNull())
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(queryInlineAmountInput()).toBeNull()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('待切换提示态下未保存输入消失即自动按新范围加载', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    await changeTrustedScope({ branchCode: 'BRA-3' })
    const next = armQuery()
    await pickBranch('BRA-2')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    expect(inlineAmountInput().value).toBe('99')

    // 用户用 Esc 收起编辑：未保存内容消失，提示态自动完成切换。
    await pressEscape(inlineAmountInput())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })
    await waitFor(() => expect(promptElement()).toBeNull())
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('没有未保存内容时可信范围变化直接切换，不弹确认框', async () => {
    await renderLoaded([amountRow()])

    await changeTrustedScope({ branchCode: 'BRA-3' })
    // 自动切换先行完成：等它的读取排队并交付，再做用户改选，两个时序不交叠。
    const auto = armQuery()
    await waitFor(() => expect(branchSelect().value).toBe('BRA-3'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(auto, [amountRow()])

    const next = armQuery()
    await pickBranch('BRA-2')

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ branchCode: 'BRA-2' })
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('可信组织或医院换绑且有未保存内容时弹确认框，放弃并切换即按新范围加载', async () => {
    // 新可信医院下的院区由伪 Base Client 交付（与 C3 可信医院变更同一构造）。
    baseClient = createBaseClient([...BRANCHES, { id: 'BRA-9', name: '院区九', hosId: 'HOS-2' }])
    configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 宿主重签 token 换绑可信医院：在既有选择被清空之前挂起本次切换（Client/design.md 第 34 行）。
    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // 确认框存续期间：既不加载新范围，也不发起写入，更不进入待切换提示态，未保存输入与行集都保留。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()
    expect(promptElement()).toBeNull()
    expect(inlineAmountInput().value).toBe('99')

    click(within(dialog).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    // 放弃即清空未保存输入，按新可信范围重新解析院区并加载。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('可信组织或医院换绑且没有未保存内容时直接切换，不弹确认框', async () => {
    baseClient = createBaseClient([...BRANCHES, { id: 'BRA-9', name: '院区九', hosId: 'HOS-2' }])
    configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
    await renderLoaded([amountRow()])

    // 先进入过编辑又用 Esc 收起：此后没有未保存内容。
    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    await pressEscape(inlineAmountInput())
    expect(queryInlineAmountInput()).toBeNull()

    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('院区层挂起：确认框与待切换提示态期间回车提交零写请求，入口金额按钮禁用', async () => {
    await renderLoaded([
      amountRow(),
      amountRow({ standardProjectCode: 'B02', standardProjectName: '便常规' }),
    ])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 院区层换绑：可信院区变化且存在未保存内容，选择器回调挂起本次切换并弹确认框。
    await changeTrustedScope({ branchCode: 'BRA-3' })
    await pickBranch('BRA-2')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // 预排一次读取：写入路径若真走到整表重载，让它停在在途，失败现场只留下写调用本身。
    armQuery()
    // 确认框挂起期间：回车零写返回，未在编辑的行其入口金额按钮禁用；行内输入仍可编辑。
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
    expect(promptElement()).toBeNull()

    // 转入待切换提示态后同一口径继续生效。
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    armQuery()
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
  })

  it('组织医院换绑挂起：确认框与待切换提示态期间回车提交零写请求，入口金额按钮禁用', async () => {
    // 新可信医院下的院区由伪 Base Client 交付（与换绑确认框用例同一构造）。
    baseClient = createBaseClient([...BRANCHES, { id: 'BRA-9', name: '院区九', hosId: 'HOS-2' }])
    configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
    await renderLoaded([
      amountRow(),
      amountRow({ standardProjectCode: 'B02', standardProjectName: '便常规' }),
    ])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 宿主重签 token 换绑可信医院：在既有选择被清空之前挂起本次切换（Client/design.md 第 34 行）。
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    armQuery()
    // 确认框挂起期间：回车零写返回，未在编辑的行其入口金额按钮禁用；行内输入仍可编辑。
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
    expect(promptElement()).toBeNull()

    // 转入待切换提示态后同一口径继续生效。
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    armQuery()
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
  })

  it('组织医院换绑挂起中在旧院区下拉改选：放弃并切换后按新可信范围解析的院区读取，请求体不含旧医院院区', async () => {
    // 新可信医院下的院区由伪 Base Client 交付（与换绑确认框用例同一构造）。
    baseClient = createBaseClient([...BRANCHES, { id: 'BRA-9', name: '院区九', hosId: 'HOS-2' }])
    configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 宿主重签 token 换绑可信医院弹出确认框；取消后转入待切换提示态（无遮罩）。
    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })
    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()

    // 提示态下选择器仍展示旧医院的院区清单：在这里改选一个旧医院院区。
    await pickBranch('BRA-2')

    // 放弃并切换：读取按新可信范围解析出的院区发起，请求体不含旧医院的院区。
    click(within(promptElement() as HTMLElement).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('院区层只推进 token、不碰选择器：有未保存内容时渲染期即弹出可信范围变化确认框', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只按宿主重签 token 的路径推进可信院区（Client/design.md 第 34 行），全程没有任何选择器交互。
    await changeTrustedScope({ branchCode: 'BRA-3' })

    await findModal(CONFIRM_TRUSTED_SCOPE)
    // 确认框由渲染期派生弹出：期间既不加载新范围，也不发起写入，更不进入待切换提示态。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()
    expect(promptElement()).toBeNull()
    // 受控值仍停在原院区——确认框出现之前没有发生过任何选择器交互。
    expect(branchSelect().value).toBe('BRA-1')
  })

  it('院区层只推进 token、不碰选择器：无未保存内容时按三档规则重取院区且不弹确认框', async () => {
    await renderLoaded([amountRow()])
    expect(branchSelect().value).toBe('BRA-1')

    // 只按宿主重签 token 的路径推进可信院区，全程没有任何选择器交互；重载读取先排队再触发。
    const next = armQuery()
    await changeTrustedScope({ branchCode: 'BRA-3' })

    // 可信院区 BRA-3 属于当前医院的院区清单：组织、医院不变，按档一直接选中它并据此重载（S3-D22）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-3' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-3'))
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow()])
    await screen.findByText('血常规')
  })

  it('院区层只推进 token、有未保存内容：确认框里不碰选择器直接放弃并切换，院区按三档规则现解析', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进 token：确认框由渲染期派生弹出，全程没有任何选择器交互，挂起的目标范围为空。
    const next = armQuery()
    await changeTrustedScope({ branchCode: 'BRA-3' })
    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)

    click(within(dialog).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))

    // 放弃并切换：按 S3-D22 三档规则现解析院区——可信 BRA-3 属于当前院区清单，选中它并据此读取。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-3' })
    await waitFor(() => expect(branchSelect().value).toBe('BRA-3'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })
})

describe('C34 提交前现值复核', () => {
  /**
   * 行内拦截原因（S3-D28、Client/design.md 第 34 行）：一句中文须含发生了什么、结果与下一步
   * 三段；旧口径那句「页面范围为…可信范围为…请刷新页面后重试」指向用户选定范围，已作废。
   */
  const REASON_CHANGED = '可信范围已变化，页面尚未应用新可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'
  const REASON_UNREADABLE = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

  /**
   * 可信范围变化用受控构造（与 C33 同一构造）：编辑期间按宿主重签 token 的路径推进现读可信三层；
   * 比对的记录侧是页面应用范围时写下的可信范围（`applySelection` 同一帧记录），生产代码无需注入点。
   */
  it('可信范围已变化（记录的可信范围与现读不一致）时提交：零写请求，行内出现指向切换新可信范围的可见原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 现读可信院区改变、页面尚未应用新可信范围：记录侧仍是应用范围时的 BRA-1。
    await changeTrustedScope({ branchCode: 'BRA-3' })
    await findModal(CONFIRM_TRUSTED_SCOPE)

    // 写入路径若真走到整表重载，让这次读取停在在途：失败现场只留下写调用本身。
    armQuery()
    await pressEnter(inlineAmountInput())

    // 零写请求、不重载，编辑态与输入原样保留。
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
    // 行内就地给出可见原因：发生了什么、结果与下一步三段齐全。
    expect(within(rowOf('血常规')).getByText(REASON_CHANGED)).toBeInTheDocument()
    // 旧口径文案（用户选定范围那套）不得出现。
    expect(screen.queryByText(/请刷新页面后重试/)).not.toBeInTheDocument()

    // 转入待切换提示态后同一原因仍在行内呈现，回车依旧零写。
    click(within(modalElement(CONFIRM_TRUSTED_SCOPE)).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    armQuery()
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(within(rowOf('血常规')).getByText(REASON_CHANGED)).toBeInTheDocument()
  })

  it('渲染期可信范围未变化、提交时现读值已变化：零写请求，行内出现指向切换新可信范围的可见原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    /**
     * 只推进命令式读取的现值、不通知订阅者（不走 `changeTrustedScope` 的 notify 路径）：
     * 渲染期 prop 停留在上次渲染读到的可信院区，记录侧同为 BRA-1，两侧仍相等；现值只在
     * 提交发起时经命令式出口读到——构造「宿主改写可信值后同一同步块内回车」的真实宿主路径，
     * 父层渲染期比对（C33 的 `trustedRangeChanged` 派生）对这次变化毫无察觉。
     */
    authState.branchCode = 'BRA-3'
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()

    // 写入路径若真走到整表重载，让这次读取停在在途：失败现场只留下写调用本身。
    armQuery()
    await pressEnter(inlineAmountInput())

    // 零写请求、不重载，编辑态与输入原样保留。
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
    // 行内就地给出可见原因，与渲染期已察觉变化的用例同一句文案。
    expect(within(rowOf('血常规')).getByText(REASON_CHANGED)).toBeInTheDocument()
    // 旧口径文案不得出现。
    expect(screen.queryByText(/请刷新页面后重试/)).not.toBeInTheDocument()
  })

  it('可信范围一致时提交：照常发出保存请求并整表重载，行内不出现拦截原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    // 记录的可信范围与现读三层一致（组织 ORG-A、医院 HOS-1、院区 BRA-1 都未变化）：复核通过。
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
    expect(screen.queryByText(/本次不提交写入/)).not.toBeInTheDocument()
  })

  it('院区现读缺失而记录有值（可信范围已变化、页面未应用）时提交：零写请求，行内给出已变化的可见原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 现读院区缺失、记录侧仍是应用范围时的 BRA-1：院区层按一致性比对给出已变化，缺失不作读不到。
    await changeTrustedScope({ branchCode: null })
    await findModal(CONFIRM_TRUSTED_SCOPE)

    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(within(rowOf('血常规')).getByText(REASON_CHANGED)).toBeInTheDocument()
    expect(within(rowOf('血常规')).queryByText(REASON_UNREADABLE)).not.toBeInTheDocument()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })

  it('档二已应用（记录与现读的可信院区同为缺失）时提交：不拦截，保存照常提交并整表重载', async () => {
    // S3-D22 档二：可信院区缺失，页面按当前组织医院下的首项选定院区（BRA-1），记录侧仍为缺失。
    authState.branchCode = null
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    // 记录值与现读值同为缺失：一致性比对放行，保存照常提交一次，载荷用页面选定的首项院区。
    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 99,
    })
    expect(screen.queryByText(/本次不提交写入/)).not.toBeInTheDocument()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })

  it('记录的可信院区缺失而现读已有值（档二后范围变化、页面未应用）时提交：零写请求，行内给出已变化的可见原因', async () => {
    authState.branchCode = null
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进命令式现读的取值、不通知订阅者：渲染期比对毫无察觉、C33 不弹，只有提交时的现读读到新值。
    authState.branchCode = 'BRA-3'
    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(within(rowOf('血常规')).getByText(REASON_CHANGED)).toBeInTheDocument()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })

  it('现读可信医院缺失时提交：零写请求，行内给出读取不到完整可信范围的可见原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进命令式现读的医院取值：「现读不到」的结论只由可信组织或医院的缺失给出。
    authState.hospitalCode = null
    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(within(rowOf('血常规')).getByText(REASON_UNREADABLE)).toBeInTheDocument()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })

  it('用户改选院区不同于可信默认值、记录与现读一致：不拦截，保存照常提交', async () => {
    await renderLoaded([amountRow()])

    // 用户改选 BRA-3（可信默认仍是 BRA-1）：先按新范围重载行，随后编辑并提交。
    const second = armQuery()
    await pickBranch('BRA-3')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-3' })
    await settle(second, [amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '8')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    // 记录侧与现读侧都取可信 BRA-1（用户选定值不参与比对）：比对放行，载荷用用户选定的 BRA-3。
    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-3',
      standardProjectCode: 'A01',
      currentAmount: 8,
    })
    expect(screen.queryByText(/本次不提交写入/)).not.toBeInTheDocument()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })
})

describe('C35 页脚计数', () => {
  it('空列表时页脚显示共 0 项', async () => {
    await renderLoaded([])

    expect(screen.getByText('共 0 项')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
  })
  it('页脚按生效筛选后的可见行数显示共 N 项', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: node(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: node(20) }),
      amountRow({ standardProjectCode: 'A03', isAmountConfigured: false, currentAmount: null }),
    ])

    expect(screen.getByText('共 3 项')).toBeInTheDocument()

    pickSegment(amountStatusFilter(), '未配置')
    expect(screen.getByText('共 1 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)

    pickSegment(amountStatusFilter(), '全部')
    expect(screen.getByText('共 3 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C36 读取在途写门控', () => {
  it('读取在途时搜索主按钮呈加载态、金额格不进入编辑态，读取结束才恢复', async () => {
    await renderLoaded([amountRow()])

    const pending = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(pending, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeEnabled()
    click(amountValueCell('血常规'))
    expect(inlineAmountInput()).toBeInTheDocument()
  })
})
