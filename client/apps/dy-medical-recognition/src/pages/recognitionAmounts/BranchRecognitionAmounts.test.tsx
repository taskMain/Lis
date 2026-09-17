/**
 * 阶段 3「本院区互认项目金额」医院管理员页 Component 层用例
 * （测试矩阵 C3×2、C4、C5、C15、C21 的组件面）。
 *
 * 装配边界（与平台管理员页用例同一口径）：
 * 1. `./recognitionAmountsApi` 只替换 4 个 I/O 出口，其余纯函数使用真实实现；行夹具由真实
 *    `toRecognitionAmountRows` 从读模型形态构造；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client；
 * 3. `@dy/components-base` 只替换整个包：该包 ESM 产物内部使用无扩展名相对导入，vitest 默认把
 *    node_modules 依赖交给 Node 解析，组件层无法加载真实实现（需 `test.server.deps.inline`，
 *    而 `vite.config.ts` 不属于本票所有权）。替身复现页面接线依赖的组件契约：受控 `value`、
 *    `orgId`/`hosId` 固定后对应级不渲染、变更经 `onChange` 回传范围值、选项读取失败时调用
 *    antd 静态 `message.error`。真实选择器的行为属宿主层验收面（C3 的 Host 层级）；
 * 4. 可信范围沿用既有包：`@dy/auth` 的 `LoginUserManager.getOrgID()`/`getHosID()`/`getBranchID()`
 *    与 `@dy/auth-react` 的 `useAuth()` token。这里替换这两个包以构造三份身份上下文：
 *    组织+医院+院区齐全、可信院区缺失、可信组织或医院缺失（另含 token 缺失）。
 *    替身的 `useAuth` 订阅本文件的登录态并在通知后重新求值（与真实包同形），因此**挂载之后**的可信
 *    范围变化可观察：token 后到、可信医院换绑、登录态失效都按真实路径触发页面重新求值。
 *
 * 列表字段集合、未配置与零元呈现、金额校验、保存成功/失败、写成功但刷新失败、读取失败重试、
 * 升序与空态由平台管理员页用例覆盖（两页共用同一列表主体组件 `RecognitionAmountsBoard`），
 * 本文件只补医院管理员页独有的范围、请求内容与阻断行为，以及跨页一致的字段集合。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import { toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
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

  /** 范围选择器替身：契约与真实组件一致的部分见文件头第 3 条。 */
  function StubScopeSelector({ value = {}, orgId, hosId, disabled, className, onChange }: BaseScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const loadedScopeKeyRef = useRef('')
    useEffect(() => {
      // 挂载时读取一次选项；固定的组织或医院变化时也重新读取（与真实组件同口径）：可信范围换绑后
      // 必须交付新范围内的医院与院区，否则替身会一直展示旧范围的选项。用 ref 守卫而不是省略依赖，
      // 让依赖数组保持完整。
      const loadKey = `${orgId ?? ''}|${hosId ?? ''}|${value.hosId ?? ''}`
      if (loadedScopeKeyRef.current === loadKey) return
      loadedScopeKeyRef.current = loadKey
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

const HOSPITALS = [{ id: 'HOS-1', name: '医院一' }]
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

/** 院区下拉是医院管理员页唯一渲染的范围下拉。 */
const branchSelect = () => screen.getByLabelText('范围-院区') as HTMLSelectElement

async function pickBranch(value: string) {
  const select = branchSelect()
  // 选项由伪 Base Client 异步交付：等选项就绪再选，避免构造出「选了不存在的值」这种非真实路径。
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await change(select, value)
}

const AMOUNT_MODAL = '维护互认项目金额'

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

const amountEntry = (standardProjectName: string) => screen.getByLabelText(`维护金额：${standardProjectName}`)
const amountInput = (container: HTMLElement) => within(container).getByLabelText('金额') as HTMLInputElement
const okButton = (container: HTMLElement) => within(container).getByRole('button', { name: /保\s*存/ })
const refreshButton = () => screen.getByLabelText('重新读取金额列表')

const dataRows = () => screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><BranchRecognitionAmounts /></ConfigProvider>)
}

/** 进入页面并让列表成功交付给定行（读取结束后刷新入口恢复可用）。 */
async function renderLoaded(rows: RecognitionAmountRow[]) {
  const pending = armQuery()
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(pending, rows)
  await waitFor(() => expect(refreshButton()).toBeEnabled())
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
  clientRef.current = {}
  baseClient = createBaseClient()
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
})

describe('C3 可信范围与院区选择', () => {
  it('组织与医院只读展示且不渲染下拉，只渲染院区下拉，院区默认取可信院区', async () => {
    const pending = armQuery()
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 只渲染院区一级：固定传入的组织与医院不渲染对应下拉。
    expect(screen.queryByLabelText('范围-组织')).toBeNull()
    expect(screen.queryByLabelText('范围-医院')).toBeNull()
    expect(screen.getByLabelText('范围-院区')).toBeInTheDocument()
    // 组织与医院以只读方式固定展示。
    expect(screen.getByText('可信范围（只读）：组织 ORG-A，医院 HOS-1')).toBeInTheDocument()

    await settle(pending, [amountRow()])

    await waitFor(() => expect(branchSelect().value).toBe('BRA-1'))
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区一')
  })

  it('C4：查询只提交院区与标准项目编码，不含组织与医院', async () => {
    await renderLoaded([amountRow()])

    const body = query().mock.calls[0][1] as Record<string, unknown>
    expect(body).toEqual({ branchCode: 'BRA-1' })
    expect(Object.keys(body)).toEqual(['branchCode'])
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
    // 平台管理员入口在该页不被使用。
    expect(api.queryRecognitionAmountRows).not.toHaveBeenCalled()
  })

  it('C4：保存只提交院区、标准项目编码与金额，可信组织与医院不回填进请求体', async () => {
    await renderLoaded([amountRow()])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    expect(amountInput(dialog).value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    click(okButton(dialog))

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    const payload = save().mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ branchCode: 'BRA-1', standardProjectCode: 'A01', currentAmount: 12.5 })
    expect(Object.keys(payload)).toEqual(['branchCode', 'standardProjectCode', 'currentAmount'])
    expect(payload).not.toHaveProperty('organizationCode')
    expect(payload).not.toHaveProperty('hospitalCode')
    expect(api.saveOrganizationHospitalBranchRecognitionAmount).not.toHaveBeenCalled()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await expectModalClosed(AMOUNT_MODAL)
    await screen.findByText('20.00')
  })

  it('C3 可信院区缺失：不下拉默认选中、不发起任何请求，显式选择后才按院区查询', async () => {
    authState.branchCode = null
    renderPage()

    await waitFor(() => expect(branchSelect().options).toHaveLength(BRANCHES.length + 1))
    // 未默认选中：只显示占位文案，受控值保持为空。
    expect(branchSelect().value).toBe('')
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('请选择院区')
    // 不把空值作为请求参数发出：范围不完整即零业务请求。
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()

    const pending = armQuery()
    await pickBranch('BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    const body = query().mock.calls[0][1] as Record<string, unknown>
    expect(body).toEqual({ branchCode: 'BRA-2' })
    expect(Object.keys(body)).toEqual(['branchCode'])
    await settle(pending, [amountRow()])
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

    // token 后到，可信范围随后才可用：默认院区必须同步回填，而不是停留在首屏的空值。
    const pending = armQuery()
    await changeTrustedScope({ token: 'token-1', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })
    expect(screen.getByText('可信范围（只读）：组织 ORG-A，医院 HOS-1')).toBeInTheDocument()
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
    expect(branchSelect().value).toBe('BRA-1')

    // 可信医院换绑，并交付该医院下的可信院区：旧范围的院区与旧范围的行都不得残留。
    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })
    expect(branchSelect().value).toBe('BRA-9')
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

    // 可信院区取值变化（仍在同一可信组织与医院内）：显式选择保持不变，也不额外发起请求；
    // 默认可信院区只用于「用户尚未选择」时的回填，不得被当成用户的选择，也不得反过来盖回用户选择。
    await changeTrustedScope({ branchCode: 'BRA-3' })
    expect(branchSelect().value).toBe('BRA-2')
    expect((branchSelect().selectedOptions[0]?.textContent) ?? '').toBe('院区二')
    expect(query()).toHaveBeenCalledTimes(2)
    expect(save()).not.toHaveBeenCalled()
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
  it('与平台管理员页同源：列只含契约交付的九项与操作列', async () => {
    await renderLoaded([amountRow({ currentAmount: null, isAmountConfigured: false })])

    expect(screen.getAllByRole('columnheader').map((node) => node.textContent)).toEqual([
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
    expect(dataRows()).toHaveLength(1)
  })
})

describe('C21 院区切换与未保存输入', () => {
  it('切换院区时未保存的金额输入与旧院区数据一并清空', async () => {
    await renderLoaded([amountRow()])

    click(amountEntry('血常规'))
    const dialog = await findModal(AMOUNT_MODAL)
    await change(amountInput(dialog), '99')

    const next = armQuery()
    await pickBranch('BRA-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })

    expect(modalExists(AMOUNT_MODAL)).toBe(false)
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'B01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')

    click(amountEntry('新院区项目'))
    const reopened = await findModal(AMOUNT_MODAL)
    expect(amountInput(reopened).value).toBe('12.50')
  })
})
