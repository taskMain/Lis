/**
 * 阶段 2「互认项目」页面 Component 层用例（测试矩阵 C1-C3、C6、C8、C10-C11、C13-C21、C23 的非 Host 面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./recognitionProjectsApi` 只替换 5 个 I/O 出口（两个读取、三个写入），其余纯函数
 *    （请求适配、读模型映射、排除集合、本地筛选、常量与类型）**使用真实实现**；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client——页面把 client 原样透传给适配层出口；
 * 3. 可信组织沿用既有两个包的边界：`@dy/auth` 的 `LoginUserManager.getOrgID()` 与
 *    `@dy/auth-react` 的 `useAuth()`。这里替换这两个包，并用外部 store + `useSyncExternalStore`
 *    模拟宿主重新签发 token，从而以生产同一路径驱动「组织变化」。
 *
 * 仍属 Host 层、不在本文件覆盖：C4 / C5 / C7 / C9 / C12 与 C17 / C21 的 Host 分支，
 * 以及真实宿主菜单进入和统一错误提示（Ticket 08）；
 * 另有弹窗的焦点移交/回落（打开后焦点进入弹窗、关闭后回到触发按钮），
 * 在 jsdom + 关闭 CSS 的用例环境里拿不到 rc-dialog 的动画完成回调，属宿主浏览器验收项（详见对应用例组说明）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { useSyncExternalStore } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  toSelectableOptionGroups,
  toSelectableStandardItems,
  type EffectiveMedicalStandardCatalogReadModel,
  type RecognitionProjectConfigurationRow,
} from './recognitionProjectsApi'
import { RecognitionProjects } from './RecognitionProjects'
import { CreateConfigurationModal, DurationModal, ToggleModal, type RecognitionProjectWriteGate } from './RecognitionProjectModals'

/** 已对齐且可写的门控：直接渲染弹窗组件的用例用它表达「组织已对齐、读取未失败、读取不在途、无提交期复核失败」。 */
const WRITABLE_GATE: RecognitionProjectWriteGate = {
  organizationReady: true,
  canWrite: true,
  loadFailure: null,
  organizationRecheckFailure: null,
}

/** 可信登录态（org claim + token）的外部 store；token 变化等价于宿主重新签发 token。 */
const authState = vi.hoisted(() => {
  type State = { organizationCode: string | null; token: string | null }
  const listeners = new Set<() => void>()
  const state: State = { organizationCode: 'ORG-A', token: 'token-1' }
  return {
    state,
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getToken: () => state.token,
    change(next: Partial<State>) {
      Object.assign(state, next)
      for (const listener of [...listeners]) listener()
    },
    /**
     * 只改组织编码、**不通知订阅者**：用于构造「宿主已换凭证、页面尚未重渲染」这一窗口。
     * 此时渲染期门控仍是上一帧的结论，只有提交期读现值才能发现组织已经不同。
     */
    changeOrganizationSilently(organizationCode: string | null) {
      state.organizationCode = organizationCode
    },
    reset() {
      state.organizationCode = 'ORG-A'
      state.token = 'token-1'
    },
  }
})

const api = vi.hoisted(() => ({
  queryRecognitionProjectConfigurations: vi.fn(),
  querySelectableStandardItems: vi.fn(),
  createRecognitionProjectConfiguration: vi.fn(),
  updateRecognitionDuration: vi.fn(),
  setRecognitionProjectConfigurationEnabled: vi.fn(),
}))

/**
 * 占位 client：显式提供枚举元数据端点，默认**拒绝**，使"元数据不可用 → 走兜底文案"成为用例的显式前提，
 * 而不是空对象属性访问的副作用。每条用例在 `beforeEach` 换成**新的 client 实例**：hook 的枚举元数据缓存
 * 以 client 实例为键，跨用例共用同一实例会让上一条用例的结果泄漏进来。
 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown, post: vi.fn() }))

/** 让本用例的枚举元数据请求返回给定选项（形状与生成契约 `EnumMetadataItemDto` 一致）；传 null 表示接口不可用（默认）。 */
function setMetadataOptions(options: { value: number; name: string; description: string }[] | null) {
  clientRef.post.mockImplementation(() =>
    options === null ? Promise.reject(new Error('元数据不可用')) : Promise.resolve(options),
  )
}

vi.mock('./recognitionProjectsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./recognitionProjectsApi')>()
  return { ...actual, ...api }
})

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  // 返回当前用例的 client：同一条用例内引用稳定（页面把 client 作为 load 的 useCallback 依赖），
  // 跨用例换新实例以隔离 hook 的按实例缓存。
  useApiClientContext: () => clientRef.current,
}))

vi.mock('@dy/auth', () => ({
  LoginUserManager: { getOrgID: () => authState.state.organizationCode },
}))

vi.mock('@dy/auth-react', () => ({
  useAuth: () => ({
    isAuthenticated: authState.state.token !== null,
    token: useSyncExternalStore(authState.subscribe, authState.getToken),
    login: () => {},
    logout: () => {},
  }),
}))

type Row = RecognitionProjectConfigurationRow

const CATALOG: EffectiveMedicalStandardCatalogReadModel = {
  itemTypes: [
    {
      itemType: 0,
      categories: [
        { categoryName: '检验分类', groups: [{ groupName: '血液', items: [{ code: 'A01', name: '血常规' }, { code: 'A02', name: '尿常规' }, { code: 'A03', name: '便常规' }] }] },
      ],
    },
  ],
}

/** 目标组织本次读取交付的目录：与 `CATALOG` 不同，用于判别可选集合确实来自新组织这一次读取。 */
const ORG_B_CATALOG: EffectiveMedicalStandardCatalogReadModel = {
  itemTypes: [
    {
      itemType: 1,
      categories: [
        { categoryName: '检查分类', groups: [{ groupName: '影像', items: [{ code: 'C01', name: '丙院项目' }] }] },
      ],
    },
  ],
}

/**
 * 行夹具：服务端交付的枚举中文直接写死（不从被测出口取），否则出口改错时夹具会跟着一起改，
 * 断言仍然通过，这类期望值就不具备判别力。
 */
const row = (id: string, code: string, name: string, status: 1 | 2 = 1, extra: Partial<Row> = {}): Row => ({
  configurationId: id,
  standardProjectCode: code,
  standardItemName: name,
  itemType: 0,
  itemTypeText: '检验',
  categoryName: '检验分类',
  groupName: '血液',
  recognitionDurationDays: 30,
  configurationStatus: status,
  configurationStatusText: status === 1 ? '启用' : '停用',
  unavailableReason: null,
  ...extra,
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

interface PendingLoad {
  configurations: ReturnType<typeof deferred<Row[]>>
  items: ReturnType<typeof deferred<EffectiveMedicalStandardCatalogReadModel | null>>
}

function pendingLoad(): PendingLoad {
  return { configurations: deferred<Row[]>(), items: deferred<EffectiveMedicalStandardCatalogReadModel | null>() }
}

/** 排队一次完整读取（页面每次 load 固定按 配置 → 标准目录 顺序发起）。 */
function armLoad(load: PendingLoad) {
  api.queryRecognitionProjectConfigurations.mockReturnValueOnce(load.configurations.promise)
  api.querySelectableStandardItems.mockReturnValueOnce(load.items.promise)
  return load
}

async function settle(load: PendingLoad, rows: Row[], catalog: EffectiveMedicalStandardCatalogReadModel | null = CATALOG) {
  await act(async () => {
    load.configurations.resolve(rows)
    load.items.resolve(catalog)
  })
}

async function failLoad(load: PendingLoad) {
  await act(async () => {
    load.configurations.reject(new Error('读取失败'))
    load.items.reject(new Error('读取失败'))
  })
}

const click = (element: HTMLElement) => act(() => {
  fireEvent.click(element)
})

const change = (element: HTMLElement, value: string) => act(async () => {
  fireEvent.change(element, { target: { value } })
})

/** 宿主重新签发 token 时组织一并更新，这是 C16/C18「切换组织」的真实触发路径。 */
const switchTrustedOrganization = async (organizationCode: string) => {
  await act(async () => {
    authState.change({ organizationCode, token: `token-${organizationCode}` })
  })
}

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><RecognitionProjects /></ConfigProvider>)
}

/**
 * 弹窗查询按标题文本定位：测试环境下 antd 的无 id 组件共用自动生成的 id，
 * 用 `getByRole('dialog', { name })` 会因 id 抢占而拿不到可访问名称。
 */
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
const modal = (title: string) => within(modalElement(title))
const expectModalClosed = (title: string) =>
  waitFor(() => expect(modalExists(title), `弹窗未关闭：${title}`).toBe(false), { timeout: 3000 })

/** antd 6 的关闭按钮在内部分层都带同一个 aria-label，故按容器类名定位唯一的关闭按钮。 */
function closeModal(title: string) {
  const button = modalElement(title).querySelector('.ant-modal-close')
  if (!button) throw new Error(`未找到关闭按钮：${title}`)
  click(button as HTMLElement)
}

const query = () => api.queryRecognitionProjectConfigurations
const addButton = () => screen.getByRole('button', { name: /新增配置/ })

/** 已展开下拉的选项文本；antd Select 的选项渲染在 portal 里，只能按容器类名取。 */
const optionLabels = () =>
  Array.from(document.querySelectorAll('.ant-select-item-option')).map((node) => node.textContent ?? '')

/** 当前配置状态筛选的选中项；Segmented 把外部 aria-label 透传到 role=radiogroup。 */
const selectedStatusLabels = () =>
  [...screen.getByRole('radiogroup', { name: '配置状态筛选' }).querySelectorAll('.ant-segmented-item-selected')]
    .map((node) => node.textContent?.trim())

/**
 * 未点名读取的兜底 mock：实现若放行了写操作，写完成后的重载（含随后被应用的组织切换重载）
 * 会拿到空结果而不是 `undefined`，使用例以断言失败结束，而不是先撞上渲染异常。
 */
function stubUnarmedLoads() {
  api.queryRecognitionProjectConfigurations.mockResolvedValue([])
  api.querySelectableStandardItems.mockResolvedValue(CATALOG)
}

async function renderLoaded(rows: Row[], catalog: EffectiveMedicalStandardCatalogReadModel | null = CATALOG) {
  const load = armLoad(pendingLoad())
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(load, rows, catalog)
  await waitFor(() => expect(addButton()).toBeEnabled())
}

/** 打开新增弹窗并填入合法内容（不提交）。 */
async function openCreateForm(code: string, duration: string) {
  click(addButton())
  const create = await findModal('新增互认项目配置')
  await pickOption(within(create).getByLabelText('标准项目'), code)
  await change(within(create).getByLabelText('可互认时间（天）'), duration)
  return create
}

/** antd Select 需先 mousedown 展开，再从下拉（portal）里按文本点选项。 */
async function pickOption(combobox: HTMLElement, optionText: string) {
  await act(async () => {
    fireEvent.mouseDown(combobox)
  })
  const option = await waitFor(() => {
    const found = Array.from(document.querySelectorAll('.ant-select-item-option')).find((node) =>
      (node.textContent ?? '').includes(optionText),
    )
    if (!found) throw new Error(`选项未展开：${optionText}`)
    return found as HTMLElement
  })
  click(option)
}

beforeEach(() => {
  authState.reset()
  for (const fn of Object.values(api)) fn.mockReset()
  // 每条用例一个 client 实例（隔离 hook 的按实例缓存），枚举元数据端点默认不可用。
  clientRef.post.mockReset()
  clientRef.current = { api: { enumMetadata: { getEnumMetadata: { post: clientRef.post } } } }
  setMetadataOptions(null)
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

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C13/C14 与 C1 行；首次加载与可信组织的实现位置见该表。
 */
describe('C13 / C1 页面首次加载与可信组织', () => {
  it('进入页面自动加载当前组织的配置与选择数据，加载期间禁止写操作', async () => {
    const load = armLoad(pendingLoad())
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(api.querySelectableStandardItems).toHaveBeenCalledTimes(1)
    expect(addButton()).toBeDisabled()

    await settle(load, [row('c1', 'A01', '血常规')])
    await waitFor(() => expect(addButton()).toBeEnabled())
    expect(screen.getByText('血常规')).toBeInTheDocument()
  })

  it('C1：查询只提交 token 携带的可信组织编码，不带筛选、不取 URL 或本地存储', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])
    // 页面把 API Client 原样交给适配层出口：组织与请求形状都不在页面里二次包装。
    expect(query().mock.calls[0][0]).toBe(clientRef.current)
    expect(query().mock.calls[0][1]).toEqual({ organizationCode: 'ORG-A' })
    expect(Object.keys(query().mock.calls[0][1] as object)).toEqual(['organizationCode'])
    expect(screen.getByText('组织编码：ORG-A')).toBeInTheDocument()
  })

  it('只展示查询契约交付的列，不展示创建修改信息与已删除的派生状态', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])
    expect(screen.getAllByRole('columnheader').map((node) => node.textContent)).toEqual([
      '编码', '名称', '类型', '分类', '分组', '可互认时间（天）', '配置状态', '目录停用原因', '操作',
    ])
  })

  it('可信组织缺失或为空白时进入不可操作状态，且不发起任何请求', async () => {
    authState.change({ organizationCode: null, token: null })
    renderPage()
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
    expect(api.querySelectableStandardItems).not.toHaveBeenCalled()
    expect(screen.queryByRole('button', { name: /新增配置/ })).not.toBeInTheDocument()

    await act(async () => {
      authState.change({ organizationCode: '   ', token: 'token-x' })
    })
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
  })

  /**
   * 设计条目：组织范围不可用时页面整体渲染阻断提示，不渲染任何需要枚举文案的区域，
   * 因此此时也不应发起枚举元数据读取（该请求的唯一用途是给这些区域提供中文）。
   * （旧用例名里的分组编号 B1-05 只在本文件内使用，无外部登记表。）
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「API 与组件」的枚举元数据段，
   * 与 `Client/Pages/RecognitionProjects/RecognitionProjects.md` 第 5 节「已定实现边界」的
   * 「枚举元数据不是组织范围数据」条（请求时机由 `enabled` 门控）。
   */
  it('组织范围不可用时不读取枚举元数据，恢复可信组织后才读取', async () => {
    stubUnarmedLoads()
    setMetadataOptions([{ value: 1, name: 'Enabled', description: '元数据启用文案' }])
    authState.change({ organizationCode: null, token: null })
    renderPage()

    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()
    await act(async () => { await Promise.resolve() })
    expect(clientRef.post).not.toHaveBeenCalled()

    // 恢复可信组织后，同一页面实例必须重新取枚举元数据（门控只在上下文不可用时抑制请求）。
    await switchTrustedOrganization('ORG-A')
    await waitFor(() => expect(clientRef.post).toHaveBeenCalledWith({ enumName: 'ConfigurationStatus' }))
  })

  /*
   * 设计条目：首帧与切换组织窗口内 `rows=[]` / 目录为空，此时新增入口与空选项提示都会误导用户，
   * 就绪必须按「本次读取是否成功」判定。（旧用例名里的分组编号 B2 只在本文件内使用，无外部登记表。）
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」补充规则（新增入口的就绪判定单独跟踪 `catalogLoaded`，与目录内容是否为空解耦）。
   */
  it('读取未完成的新增入口保持禁用，完成后才可用', async () => {
    const load = armLoad(pendingLoad())
    renderPage()

    // 断言点选在「读取尚未发起」这一帧：此处 loading 仍为 false、loadFailure 为 null、目录尚未读到。
    // 旧判据只用 `loadFailure === null` 判就绪，这一帧新增入口是放行的；新判据要求目录已读到，必须禁用。
    expect(query()).not.toHaveBeenCalled()
    expect(addButton()).toBeDisabled()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(addButton()).toBeDisabled()

    await settle(load, [row('c1', 'A01', '血常规')])
    await waitFor(() => expect(addButton()).toBeEnabled())
  })

  it('切换组织后目标目录就绪前新增入口保持禁用', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')

    // 组织切换后、目标组织查询发起前：列表与目录已被清空（loading=false、catalogLoaded=false、
    // loadFailure=null），旧判据会用旧组织的就绪态放行入口，此处必须已禁用。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(addButton()).toBeDisabled()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(addButton()).toBeDisabled()

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await waitFor(() => expect(addButton()).toBeEnabled())
  })

  it('仅目录读取失败同样进入阻断态且新增入口关闭，不靠旧目录数据放行', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const partial = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => {
      partial.configurations.resolve([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
      partial.items.reject(new Error('标准目录读取失败'))
    })

    await screen.findByText('互认配置数据待刷新')
    expect(addButton()).toBeDisabled()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeDisabled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 2 节「状态机」补充规则（「没有可新增的标准项目」只在目录已读到、可选集合确实为空时给出）
 * 与第 1 节「页面结构」的新增弹窗行。
 */
describe('新增弹窗的目录就绪提示分支', () => {
  /**
   * 直接渲染弹窗组件：页面层在新增入口可用时必然已 `catalogReady`（入口被它门控），
   * 因此「目录未就绪却打开新增弹窗」无法通过页面交互到达，只能在此固定该分支的口径：
   * 旧实现以 `loadFailure === null` 判就绪，会把未刷新的目录误报成「没有可新增的标准项目」。
   */
  it('目录未就绪时提示“标准目录数据待刷新”，不报“没有可新增的标准项目”', async () => {
    render(<CreateConfigurationModal open optionGroups={[]} catalogReady={false} writeGate={WRITABLE_GATE} onCancel={() => {}} onSubmit={async () => {}} submitting={false} />)

    const select = screen.getByLabelText('标准项目')
    await act(async () => {
      fireEvent.mouseDown(select)
    })

    await screen.findByText('标准目录数据待刷新')
    expect(screen.queryByText('没有可新增的标准项目')).not.toBeInTheDocument()
  })

  it('目录已就绪且无剩余可选项时提示“没有可新增的标准项目”', async () => {
    render(<CreateConfigurationModal open optionGroups={[]} catalogReady writeGate={WRITABLE_GATE} onCancel={() => {}} onSubmit={async () => {}} submitting={false} />)

    const select = screen.getByLabelText('标准项目')
    await act(async () => {
      fireEvent.mouseDown(select)
    })

    await screen.findByText('没有可新增的标准项目')
    expect(screen.queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
  })

  it('页面在目录确实没有可新增项时给出同一提示，确认文案出口一致', async () => {
    // 目录已就绪但其中的项目全部已配置：可选集合为空，提示必须来自「目录已就绪」这一分支。
    await renderLoaded([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规'), row('c3', 'A03', '便常规')])

    click(addButton())
    const create = await findModal('新增互认项目配置')
    await act(async () => {
      fireEvent.mouseDown(within(create).getByLabelText('标准项目'))
    })
    await screen.findByText('没有可新增的标准项目')
    expect(screen.queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
  })

  it('读取成功但目录为空时新增入口仍可用，并提示没有可新增项', async () => {
    const load = armLoad(pendingLoad())
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(addButton()).toBeDisabled()

    // 目录读取成功但内容为空：属正常结果，入口必须可用；否则会永久禁用且既无提示也无重试入口。
    await settle(load, [row('c1', 'A01', '血常规')], null)
    await waitFor(() => expect(addButton()).toBeEnabled())

    click(addButton())
    const create = await findModal('新增互认项目配置')
    await act(async () => {
      fireEvent.mouseDown(within(create).getByLabelText('标准项目'))
    })
    await screen.findByText('没有可新增的标准项目')
    expect(screen.queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
  })
})

/**
 * 用例名只描述行为：行标识必须稳定且唯一（旧用例名里的分组编号 B4 只在本文件内使用，无外部登记表）。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 5 节「已定实现边界」的「行标识规则」条（唯一性断言按 `data-row-key` 实际值），以及
 * `Client/design.md`「页面与交互」的行标识规则段。
 *
 * 判别方式（顺序敏感的可观察结果，不依赖 React 的告警文案）：两行的**业务标识字段完全相同**，
 * 因此只能靠出现序号补齐唯一 key；两行的可互认时间与配置状态不同，于是每行的操作按钮可访问名称
 * 与数值单元格都能指认「这是第几行的数据」。key 冲突导致行错位、串位或只渲染部分行时，
 * 这些按行定位的断言会失败。
 *
 * **防御性验证**：正常业务路径下 `configurationId` 必为非空 Guid（契约唯一键），本夹具刻意构造
 * 标识全缺的两个相同行，只为验证行标识回退规则本身的健壮性（Testing Baseline §1.8：
 * 正常业务不可达的数据可以构造，但必须标注目的，且不用于验证正常业务行为）。
 */
describe('表格行标识稳定唯一', () => {
  it('业务字段完全相同的多行互不串位，操作与数值都指向各自那一行', async () => {
    // 防御性夹具（见本组说明）：两行的 configurationId 与 standardProjectCode 都缺失。
    await renderLoaded([
      row('', '', '匿名项目', 1, { configurationId: null, standardProjectCode: null }),
      row('', '', '匿名项目', 2, { configurationId: null, standardProjectCode: null, recognitionDurationDays: 45 }),
    ])

    // 两行都必须渲染出来（下面的按行断言都以两行为前提）。
    expect(screen.getAllByText('匿名项目')).toHaveLength(2)

    const dataRows = screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))
    expect(dataRows).toHaveLength(2)

    // 判别 key 是否唯一不依赖 React 的告警文本：rc-table 把实际使用的 key 直接渲染为行的 `data-row-key`，
    // 两行必须互不相同；重复 key 会触发 React 告警并可能让同行数据错配，可观察的差异就是 `data-row-key` 重复。
    const rowKeys = dataRows.map((node) => node.getAttribute('data-row-key'))
    expect(new Set(rowKeys).size).toBe(2)

    // 第 1 行：时间 30、状态启用 → 动作目标为停用。
    expect(within(dataRows[0]).getByText('30')).toBeInTheDocument()
    expect(within(dataRows[0]).getByLabelText('停用配置：匿名项目（当前启用）')).toBeDisabled()
    expect(within(dataRows[0]).queryByLabelText('启用配置：匿名项目（当前停用）')).toBeNull()

    // 第 2 行：时间 45、状态停用 → 动作目标为启用。串位时这两行会互换。
    expect(within(dataRows[1]).getByText('45')).toBeInTheDocument()
    expect(within(dataRows[1]).getByLabelText('启用配置：匿名项目（当前停用）')).toBeDisabled()
    expect(within(dataRows[1]).queryByLabelText('停用配置：匿名项目（当前启用）')).toBeNull()

    // 标识缺失的行本就不具备写能力（没有配置标识），禁用态与 key 稳定无关，是既有行为。
    expect(within(dataRows[0]).getByLabelText('修改可互认时间：匿名项目')).toBeDisabled()
    expect(within(dataRows[1]).getByLabelText('修改可互认时间：匿名项目')).toBeDisabled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C14 行与第 2 节「状态机」的「加载中（手动刷新）」行。
 */
describe('C14 手动刷新', () => {
  it('重新加载配置与选择数据，刷新期间禁止写操作', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const refresh = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(api.querySelectableStandardItems).toHaveBeenCalledTimes(2)
    expect(addButton()).toBeDisabled()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeDisabled()

    await settle(refresh, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await screen.findByText('尿常规')
    await waitFor(() => expect(addButton()).toBeEnabled())
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C11/C15 行与第 2 节「状态机」的「读取失败」行（保留旧数据、可重试）。
 */
describe('C11 / C15 同组织读取失败与重试恢复', () => {
  it('读取失败保留旧数据并进入不可操作态，重试成功后恢复操作', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const failing = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failLoad(failing)

    await screen.findByText('互认配置数据待刷新')
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(addButton()).toBeDisabled()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeDisabled()
    expect(screen.getByLabelText('停用配置：血常规（当前启用）')).toBeDisabled()

    const retry = armLoad(pendingLoad())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])

    await screen.findByText('尿常规')
    await waitFor(() => expect(addButton()).toBeEnabled())
    expect(screen.queryByText('互认配置数据待刷新')).not.toBeInTheDocument()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeEnabled()
  })

  it('部分读取失败同样阻断写操作，且失败来源不覆盖已成功来源的数据', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const failing = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => {
      failing.configurations.resolve([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
      failing.items.reject(new Error('标准目录读取失败'))
    })

    await screen.findByText('互认配置数据待刷新')
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(addButton()).toBeDisabled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C10/C16/C17 行、第 2 节「状态机」的组织待切换行，以及
 * `docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」的组织变化段。
 */
describe('C10 / C16 / C17 组织变化', () => {
  it('C16：宿主组织变化后清空旧列表与弹窗、复位筛选，只加载目标组织', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    // 切换前布置筛选并记录旧组织目录交付的可选集合。
    await change(screen.getByLabelText('标准项目编码筛选'), 'A')
    click(within(screen.getByRole('radiogroup', { name: '配置状态筛选' })).getByText('启用'))
    click(screen.getByRole('button', { name: /新增配置/ }))
    const before = await findModal('新增互认项目配置')
    await act(async () => {
      fireEvent.mouseDown(within(before).getByLabelText('标准项目'))
    })
    await waitFor(() => expect(optionLabels()).toEqual(['尿常规（A02）', '便常规（A03）']))
    // 关闭弹窗，制造“没有未保存内容”的切换前提；筛选保持生效。
    click(within(before).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('组织编码：ORG-B')).toBeInTheDocument()
    // applyOrganization 的复位：编码与状态筛选回到默认，否则旧筛选会隐藏新组织的配置。
    expect((screen.getByLabelText('标准项目编码筛选') as HTMLInputElement).value).toBe('')
    expect(selectedStatusLabels()).toEqual(['全部'])

    // 可选集合必须来自目标组织本次读取的目录，而不是旧组织残留的目录数据。
    await settle(target, [row('c9', 'B01', '乙院项目')], ORG_B_CATALOG)
    await screen.findByText('乙院项目')
    click(addButton())
    const after = await findModal('新增互认项目配置')
    await act(async () => {
      fireEvent.mouseDown(within(after).getByLabelText('标准项目'))
    })
    await waitFor(() => expect(optionLabels()).toEqual(['丙院项目（C01）']))
  })

  // 设计条目：写在途期间页面已经切到别的组织时，写完成后的重载必须被放弃，不得把旧组织结果刷进新视图。
  // （旧用例名里的分组编号 B3-02 只在本文件内使用，无外部登记表。）
  // 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
  // 第 2 节「状态机」补充规则（写完成后的重载前校验「提交时的组织仍是页面当前组织」，否则不重载）。
  it('写在途时确认切换组织，写完成后不重载也不展示旧组织行', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfiguration.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))

    // 写在途时宿主切换组织：页面弹出待切换确认框，用户确认放弃未保存内容。
    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    const target = armLoad(pendingLoad())
    click(within(confirm).getByRole('button', { name: /放弃并切换/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })

    // 写请求此刻才返回：页面已经是 ORG-B，守卫必须放弃这次重载，旧组织行不得出现。
    await act(async () => {
      write.resolve()
    })
    await settle(target, [row('c9', 'B01', '乙院项目')])

    expect(query()).toHaveBeenCalledTimes(2)
    expect(screen.getByText('乙院项目')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
  })

  it('C10：组织变化后迟到的旧组织响应不得污染新组织', async () => {
    const first = armLoad(pendingLoad())
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')

    await settle(first, [row('c1', 'A01', '血常规')])
    await waitFor(() => expect(screen.queryByText('血常规')).not.toBeInTheDocument())
    expect(screen.getByText('乙院项目')).toBeInTheDocument()
  })

  it('C17：目标组织加载失败时不进入可操作状态，也不展示旧组织数据', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failLoad(target)

    await screen.findByText('互认配置数据待刷新')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(addButton()).toBeDisabled()
  })

  it('C18：组织变化遇未保存内容先确认；取消保留内容与旧组织视图，确认才清理并切换', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '45')
    await switchTrustedOrganization('ORG-B')

    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')

    // 取消分支：不重载、不清理，未保存内容与旧组织列表都保留，并以提示条说明待切换。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(screen.getByText('宿主组织已变化，尚有未保存内容')).toBeInTheDocument()
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('45')
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('组织编码：ORG-A')).toBeInTheDocument()
    expect(addButton()).toBeDisabled()

    // 确认分支：清空旧组织的列表与弹窗，加载目标组织。
    const target = armLoad(pendingLoad())
    click(screen.getByRole('button', { name: '放弃并切换' }))
    const again = await findModal('切换组织将放弃未保存内容')
    click(within(again).getByRole('button', { name: /放弃并切换/ }))

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })
    await expectModalClosed('新增互认项目配置')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('组织编码：ORG-B')).toBeInTheDocument()

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
  })

  /**
   * 设计条目：组织变化推迟到「未保存内容消失」之后的自动切换路径。
   * （旧用例名里的分组编号 B3-10 只在本文件内使用，无外部登记表。）
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」的「组织待切换（deferred）」行（未保存内容消失后自动进入加载）。
   *
   * 覆盖 取消确认 → deferred 分支的出口：用户不点「放弃并切换」，而是关闭写弹窗放弃未保存内容，
   * 此时自动切换必须发生。若实现只在显式确认时切换，用户会停在旧组织视图却已无内容可放弃。
   */
  it('取消组织切换确认后关闭写弹窗，随即自动切换并加载目标组织', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')
    await screen.findByText('宿主组织已变化，尚有未保存内容')
    expect(query()).toHaveBeenCalledTimes(1)

    // 放弃未保存内容：关闭写弹窗后，「未保存内容」不再阻断切换。
    const target = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })
    expect(screen.getByText('组织编码：ORG-B')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.queryByText('宿主组织已变化，尚有未保存内容')).not.toBeInTheDocument()

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
  })
})

/**
 * 用例名只描述行为：组织未对齐期间已打开的写弹窗不得提交。
 * （旧用例名里的分组编号 B1-02 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 2 节「状态机」的「写弹窗门控与提交前复核」段与第 3 节「C 矩阵对应」的 C18 行。
 */
describe('组织未对齐期间已打开的写弹窗不得提交', () => {
  /**
   * 组织未对齐（可信组织已变化、页面尚未应用）期间不允许对旧组织发起写入。
   * 断言用户可观察结果：写请求为零、不重载、弹窗与输入保留、确定按钮不可用。
   */
  it('组织待切换确认框弹出期间，新增弹窗提交为零写请求且保留输入', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    const create = await openCreateForm('尿常规', '30')

    // 宿主重新签发 token 把组织切到 ORG-B：还有未保存内容，页面停在「组织待切换」确认框。
    await switchTrustedOrganization('ORG-B')
    await findModal('切换组织将放弃未保存内容')

    // 万一实现放行了写入，这次刷新会被消费；组织未对齐时它不应被用到。
    armLoad(pendingLoad())
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    const save = within(create).getByRole('button', { name: /保\s*存/ })
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
    expect(save).toBeDisabled()
  })

  it('取消组织切换确认后（deferred），写弹窗提交仍为零写请求', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    const create = await openCreateForm('尿常规', '30')

    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')
    await screen.findByText('宿主组织已变化，尚有未保存内容')

    armLoad(pendingLoad())
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    const save = within(create).getByRole('button', { name: /保\s*存/ })
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect(save).toBeDisabled()
  })

  it('修改时间弹窗在组织未对齐时同样零写请求', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const duration = await findModal('修改可互认时间')

    await switchTrustedOrganization('ORG-B')
    await findModal('切换组织将放弃未保存内容')

    armLoad(pendingLoad())
    api.updateRecognitionDuration.mockResolvedValueOnce(undefined)
    const save = within(duration).getByRole('button', { name: /保\s*存/ })
    click(save)
    await act(async () => {})

    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists('修改可互认时间')).toBe(true)
    expect(save).toBeDisabled()
  })

  /**
   * 读取失败与读取在途时页面的写入口本身是禁用的，因此「弹窗已打开但读取失败/在途」无法通过页面
   * 交互到达，只能直接渲染弹窗固定该口径：页面以 `writeGate` 冻结提交按钮并给出暂停原因，
   * 已打开的弹窗不会基于不完整数据写出。三个写弹窗都必须守住同一条规则。
   */
  it('读取失败或组织未对齐时三个写弹窗的确定按钮禁用且不触发提交', async () => {
    const createSubmit = vi.fn(async () => {})
    const createProps = { open: true, optionGroups: [], catalogReady: true, onCancel: () => {}, submitting: false }
    const readFailureGate: RecognitionProjectWriteGate = { organizationReady: true, canWrite: false, loadFailure: 'load', organizationRecheckFailure: null }

    const create = render(<CreateConfigurationModal {...createProps} onSubmit={createSubmit} writeGate={readFailureGate} />)
    const createDialog = modal('新增互认项目配置')
    const createOk = createDialog.getByRole('button', { name: /保\s*存/ })
    expect(createOk).toBeDisabled()
    expect(createDialog.getByText('写入已暂停')).toBeInTheDocument()
    expect(createDialog.getByText('读取失败，已有数据保留。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    click(createOk)
    await act(async () => {})
    expect(createSubmit).not.toHaveBeenCalled()
    create.unmount()

    // 写入成功后的刷新失败与一般读取失败提示口径不同，同一弹窗必须区分。
    const refreshGate: RecognitionProjectWriteGate = { organizationReady: true, canWrite: false, loadFailure: 'refreshAfterWrite', organizationRecheckFailure: null }
    const duration = render(<DurationModal open row={row('c1', 'A01', '血常规')} writeGate={refreshGate} onCancel={() => {}} onSubmit={async () => {}} submitting={false} />)
    const durationDialog = modal('修改可互认时间')
    expect(durationDialog.getByRole('button', { name: /保\s*存/ })).toBeDisabled()
    expect(durationDialog.getByText('写入已经成功，但重新读取配置与标准项目失败。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    duration.unmount()

    // 启停弹窗同样被冻结；门控放行后同一弹窗恢复提交，说明禁用来自门控而不是弹窗自身。
    const toggleSubmit = vi.fn(async () => {})
    const toggleProps = { open: true, row: row('c1', 'A01', '血常规'), nextEnabled: false, actionText: '停用', onCancel: () => {}, submitting: false }
    const toggle = render(<ToggleModal {...toggleProps} onSubmit={toggleSubmit} writeGate={{ organizationReady: false, canWrite: false, loadFailure: null, organizationRecheckFailure: null }} />)
    const toggleDialog = modal('停用互认项目配置')
    const toggleOk = toggleDialog.getByRole('button', { name: /确认停用/ })
    expect(toggleOk).toBeDisabled()
    expect(toggleDialog.getByText('宿主组织已变化，页面尚未对齐到新组织。组织未对齐期间不能发起写入，请先放弃未保存内容并切换组织。')).toBeInTheDocument()
    click(toggleOk)
    await act(async () => {})
    expect(toggleSubmit).not.toHaveBeenCalled()
    toggle.unmount()

    render(<ToggleModal {...toggleProps} onSubmit={toggleSubmit} writeGate={WRITABLE_GATE} />)
    click(modal('停用互认项目配置').getByRole('button', { name: /确认停用/ }))
    await waitFor(() => expect(toggleSubmit).toHaveBeenCalledTimes(1))
  })

  /**
   * 阻断原因的判定次序固定为「组织未对齐（含提交时刻复核失败）→ 读取失败 → 读取在途」，
   * `submitting` 只决定确定按钮保持可点并用 `confirmLoading` 反馈，不得掩盖更强的阻断原因。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」的「写弹窗门控与提交前复核」段（三个写弹窗统一禁用确定按钮并说明原因）。
   *
   * 判别方式：以下三段的门控都带 `submitting`，差异只在其它分量；旧实现（提交在途时直接返回无原因）
   * 会让第一、二段的暂停说明消失，断言失败。
   */
  it('提交在途不掩盖更强的阻断原因，读取在途则不是阻断原因', async () => {
    // 组织未对齐 + 读取失败 + 提交在途：组织类原因优先。
    const organizationBlocked = render(<ToggleModal
      open
      row={row('c1', 'A01', '血常规')}
      nextEnabled={false}
      actionText='停用'
      writeGate={{ organizationReady: false, canWrite: false, loadFailure: 'load', organizationRecheckFailure: null }}
      onCancel={() => {}}
      onSubmit={async () => {}}
      submitting
    />)
    const organizationDialog = modal('停用互认项目配置')
    expect(organizationDialog.getByText('宿主组织已变化，页面尚未对齐到新组织。组织未对齐期间不能发起写入，请先放弃未保存内容并切换组织。')).toBeInTheDocument()
    expect(organizationDialog.getByRole('button', { name: /确认停用/ })).toBeDisabled()
    organizationBlocked.unmount()

    // 组织已对齐 + 读取失败 + 提交在途：读取失败原因优先于提交在途。
    const readBlocked = render(<DurationModal
      open
      row={row('c1', 'A01', '血常规')}
      writeGate={{ organizationReady: true, canWrite: false, loadFailure: 'load', organizationRecheckFailure: null }}
      onCancel={() => {}}
      onSubmit={async () => {}}
      submitting
    />)
    const readDialog = modal('修改可互认时间')
    expect(readDialog.getByText('读取失败，已有数据保留。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    expect(readDialog.getByRole('button', { name: /保\s*存/ })).toBeDisabled()
    readBlocked.unmount()

    // 组织已对齐、读取未失败，`canWrite` 为假只因页面把 submitting 计入门控：这不是阻断原因，
    // 确定按钮保持可点并由 `confirmLoading` 反馈，弹窗内不出现暂停说明。
    const submittingOnly = render(<DurationModal
      open
      row={row('c1', 'A01', '血常规')}
      writeGate={{ organizationReady: true, canWrite: false, loadFailure: null, organizationRecheckFailure: null }}
      onCancel={() => {}}
      onSubmit={async () => {}}
      submitting
    />)
    const submittingDialog = modal('修改可互认时间')
    const ok = submittingDialog.getByRole('button', { name: /保\s*存/ })
    expect(ok).toBeEnabled()
    expect(ok).toHaveClass('ant-btn-loading')
    expect(submittingDialog.queryByText('写入已暂停')).not.toBeInTheDocument()
    submittingOnly.unmount()

    // 提交期复核留下的原因与组织未对齐同属组织类阻断：即使页面渲染期门控已放行也必须显示。
    const rechecked = render(<CreateConfigurationModal
      open
      optionGroups={[]}
      catalogReady
      writeGate={{ ...WRITABLE_GATE, organizationRecheckFailure: '提交时复核发现当前可信组织与页面已应用组织不一致。' }}
      onCancel={() => {}}
      onSubmit={async () => {}}
      submitting={false}
    />)
    const recheckedDialog = modal('新增互认项目配置')
    expect(recheckedDialog.getByText('提交时复核发现当前可信组织与页面已应用组织不一致。')).toBeInTheDocument()
    expect(recheckedDialog.getByRole('button', { name: /保\s*存/ })).toBeDisabled()
    rechecked.unmount()
  })
})

/**
 * 提交前的组织复核必须读**现值**，而不是渲染期闭包值：宿主重新签发 token 之后、页面重渲染之前，
 * 渲染期门控（`writeGate`）仍是上一帧的结论，此时提交会带着新凭证出去。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」
 * （组织未对齐时写弹窗禁用确定按钮，提交动作在发起请求前再以可信组织复核一次）与
 * `Client/Pages/RecognitionProjects/RecognitionProjects.md` 第 4 节「组织范围唯一接缝（S2-D12 已结案）」。
 *
 * 判别方式：以下用例把 mock 层可变值改成别的组织编码而**不通知订阅者**（宿主已换凭证、页面尚未重渲染），
 * 渲染期门控因此仍打开；只要提交改回用渲染期闭包值复核（或去掉现值复核），「零写请求」就会失败。
 */
describe('提交前组织复核取现值', () => {
  it('宿主换组织凭证但页面尚未重渲染时，提交零写请求、保留弹窗与输入并给出原因', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    const create = await openCreateForm('尿常规', '30')

    // 组织编码变了，但不通知 token 订阅者：页面不重渲染，第一道门控仍是上一帧的「已对齐」。
    authState.changeOrganizationSilently('ORG-B')

    // 万一实现放行了写入，这次调用（以及下面的排队读取）会被消费；提交期复核必须让它保持为零。
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    armLoad(pendingLoad())
    const save = within(create).getByRole('button', { name: /保\s*存/ })
    expect(save).toBeEnabled()
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    // 不静默返回：弹窗与输入保留，弹窗内给出原因，并进入既有组织变化确认流程。
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
    expect(within(create).getByText('写入已暂停')).toBeInTheDocument()
    expect(within(create).getByText(/提交时复核发现当前可信组织与页面已应用组织不一致/)).toBeInTheDocument()
    await findModal('切换组织将放弃未保存内容')
  })

  it('提交时读不到可信组织范围时同样零写请求并由页面给出阻断原因', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    await openCreateForm('尿常规', '30')

    authState.changeOrganizationSilently(null)
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    armLoad(pendingLoad())
    click(within(modalElement('新增互认项目配置')).getByRole('button', { name: /保\s*存/ }))
    await act(async () => {})

    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    // 复核失败后的重渲染让页面读到「可信组织不可用」，由既有阻断分支给出原因。
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()
  })

  /**
   * 判别方式：「页面已应用组织」只有一个状态源（`appliedOrganizationCode` 与它的同步镜像 ref
   * 在同一处赋值）。旧实现会在可信组织不可用分支把 ref 置空并让它在恢复后停在 null，
   * 门控（只看 state）全绿而四条写路径静默失效——本用例会停在「已点保存但零写请求」。
   */
  it('可信组织经历「可用、不可用、同一组织」后提交仍会发出写请求', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    await openCreateForm('尿常规', '30')

    await act(async () => {
      authState.change({ organizationCode: null, token: null })
    })
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()

    // 宿主重新签发**同一组织**的凭证：页面已应用组织与可信组织相同，无需重新对齐，但会重载一次数据。
    const restored = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-A')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(restored, [row('c1', 'A01', '血常规')])

    // 写弹窗在不可用分支被卸载、恢复后以新实例挂载：重新填写并提交。
    const reopened = await findModal('新增互认项目配置')
    await pickOption(within(reopened).getByLabelText('标准项目'), '便常规')
    await change(within(reopened).getByLabelText('可互认时间（天）'), '30')
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    const reload = armLoad(pendingLoad())
    click(within(reopened).getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
    expect(api.createRecognitionProjectConfiguration.mock.calls[0][1]).toEqual({ standardProjectCode: 'A03', recognitionDurationDays: 30 })
    await settle(reload, [row('c1', 'A01', '血常规'), row('c3', 'A03', '便常规')])
    await expectModalClosed('新增互认项目配置')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C2/C3 行与第 5 节「已定实现边界」的「查询只提交组织编码」条
 * （读取全量配置后在本地算排除集合）。
 */
describe('C2 / C3 新增选择器', () => {
  it('只提供未配置的最底层标准项目，包含停用配置在内的已配置项目全部隐藏', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    click(addButton())
    const create = await findModal('新增互认项目配置')
    const combobox = within(create).getByLabelText('标准项目')
    await act(async () => {
      fireEvent.mouseDown(combobox)
    })

    const labels = await waitFor(() => {
      const nodes = Array.from(document.querySelectorAll('.ant-select-item-option'))
      if (nodes.length === 0) throw new Error('选项未展开')
      return nodes.map((node) => node.textContent ?? '')
    })
    expect(labels).toEqual(['便常规（A03）'])
  })

  it('C4 的组件面：非法天数本地拦截且零写请求，弹窗与输入保留', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '0')
    click(within(create).getByRole('button', { name: /保\s*存/ }))

    await screen.findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('0')

    await change(within(create).getByLabelText('可互认时间（天）'), '1.5')
    click(within(create).getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.createRecognitionProjectConfiguration).not.toHaveBeenCalled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C6/C20 行与第 1 节「页面结构」的修改弹窗行（同值也照常提交）。
 */
describe('C6 / C20 修改可互认时间', () => {
  it('修改弹窗回填当前值，非法输入零写且弹窗保留，同值也照常提交', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const duration = await findModal('修改可互认时间')
    const input = () => within(duration).getByLabelText('可互认时间（天）') as HTMLInputElement
    expect(input().value).toBe('30')

    await change(input(), '-5')
    click(within(duration).getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(modalExists('修改可互认时间')).toBe(true)

    await change(input(), '30')
    const reload = armLoad(pendingLoad())
    click(within(duration).getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(1))
    expect(api.updateRecognitionDuration.mock.calls[0][1]).toEqual({ id: 'c1', recognitionDurationDays: 30 })

    await settle(reload, [row('c1', 'A01', '血常规', 1)])
    await expectModalClosed('修改可互认时间')
  })

  it('停用配置仍可修改时间，且表格与弹窗显示目录停用原因', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 2, { unavailableReason: '所属分组已停用' })])

    expect(screen.getByText('所属分组已停用')).toBeInTheDocument()
    click(screen.getByLabelText('修改可互认时间：血常规'))
    const duration = await findModal('修改可互认时间')
    expect(within(duration).getByText('停用')).toBeInTheDocument()
    expect(within(duration).getByText('所属分组已停用')).toBeInTheDocument()

    const reload = armLoad(pendingLoad())
    click(within(duration).getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(1))
    expect(api.updateRecognitionDuration.mock.calls[0][1]).toEqual({ id: 'c1', recognitionDurationDays: 30 })
    await settle(reload, [row('c1', 'A01', '血常规', 2, { unavailableReason: '所属分组已停用' })])
  })

  /**
   * 设计条目：修改弹窗只持配置标识，行数据按当前行集重取，不得停留在打开那一刻的快照。
   * （旧用例名里的分组编号 B1-09 只在本文件内使用，无外部登记表。）
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」
   * （弹窗只持标识、行数据按当前行集重取）与 `Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」补充规则的同一条口径。
   *
   * 判别方式：弹窗打开期间发起一次后台刷新，服务端交付的行取值与状态都变了；
   * 旧实现（弹窗持行快照）下弹窗里的取值仍是打开时的 30 与「启用」，本条断言会失败。
   */
  it('弹窗打开期间后台刷新交付新值时，弹窗取当前行取值而不是打开时的快照', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 30 })])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const duration = await findModal('修改可互认时间')
    const input = () => within(duration).getByLabelText('可互认时间（天）') as HTMLInputElement
    expect(input().value).toBe('30')
    expect(within(duration).getByText('启用')).toBeInTheDocument()

    const reload = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规', 2, { recognitionDurationDays: 60 })])

    expect(input().value).toBe('60')
    expect(within(duration).getByText('停用')).toBeInTheDocument()
    expect(within(duration).queryByText('启用')).not.toBeInTheDocument()
  })

  /**
   * 目标行离开当前行集后，修改弹窗不再渲染，它也就不再承载「未保存内容」：组织变化不得因此
   * 多弹一次确认框（用户无法主动清除这种不实状态）。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」补充规则（「未保存内容」判定与弹窗实际渲染同口径）与
   * `Client/design.md`「页面与交互」（组织变化遇未保存时间修改要先确认放弃）。
   *
   * 判别方式：只按 `modal.kind === 'duration'` 判定未保存内容时，本条会停在「待切换确认框」，
   * 「组织编码：ORG-B」与重载计数断言都失败。
   */
  it('后台刷新使修改弹窗的目标行消失后，组织变化不再出现未保存内容确认', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    await findModal('修改可互认时间')

    const reload = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c2', 'A02', '尿常规', 1)])
    await expectModalClosed('修改可互认时间')

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')

    // 没有未保存内容：不弹确认框，直接清理并加载目标组织。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-B' })
    expect(screen.queryByText('切换组织将放弃未保存内容')).not.toBeInTheDocument()
    expect(screen.getByText('组织编码：ORG-B')).toBeInTheDocument()
    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C8 行与第 1 节「页面结构」的启停确认框行（只改配置自身状态）。
 */
describe('C8 启停确认', () => {
  it('取消与右上角关闭都零写请求，确认只提交目标配置与目标状态', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])
    // 行内动作文案由适配层唯一出口构造（页面不得自拼「状态 → 中文」）。可访问名称用硬编码字面量断言：
    // 该行当前为「启用」，动作目标为「停用」，名称必须同时表达目标状态与当前状态。
    // 不从被测出口拼期望值（用出口拼期望会让同源错误互相抵消）；「元数据可用时取服务端文案」
    // 由同组的「枚举元数据可用时…」用例承担。
    const toggle = () => screen.getByLabelText('停用配置：血常规（当前启用）')

    click(toggle())
    let confirm = await findModal('停用互认项目配置')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('停用互认项目配置')
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()

    click(toggle())
    confirm = await findModal('停用互认项目配置')
    closeModal('停用互认项目配置')
    await expectModalClosed('停用互认项目配置')
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()

    click(toggle())
    confirm = await findModal('停用互认项目配置')
    const reload = armLoad(pendingLoad())
    click(within(confirm).getByRole('button', { name: /确认停用/ }))

    await waitFor(() => expect(api.setRecognitionProjectConfigurationEnabled).toHaveBeenCalledTimes(1))
    expect(api.setRecognitionProjectConfigurationEnabled.mock.calls[0][1]).toEqual({ id: 'c1', enabled: false })

    await settle(reload, [row('c1', 'A01', '血常规', 2)])
    await expectModalClosed('停用互认项目配置')
    expect(screen.getByLabelText('启用配置：血常规（当前停用）')).toBeEnabled()
  })

  /**
   * 启停弹窗与修改弹窗同一口径：只持配置标识，行数据与动作目标每次渲染都按**当前行集**重取
   * （见 `RecognitionProjects.md` 第 2 节「状态机」补充规则里「修改可互认时间弹窗只持配置标识」
   * 的同一条口径，启停弹窗保持一致）。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」
   * （配置停用后仍可修改可互认时间；启停独立确认）与上文同一条补充规则。
   *
   * 判别方式：弹窗打开期间后台刷新把该行改成停用。若启停弹窗仍持打开那一刻的行快照，
   * 动作目标会停在「停用」，本条的弹窗标题、确认按钮名与写载荷三处断言都会失败。
   */
  it('弹窗打开期间后台刷新改变该行状态时，启停弹窗按当前行重取动作目标', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('停用配置：血常规（当前启用）'))
    await findModal('停用互认项目配置')

    const reload = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规', 2)])

    // 当前行已停用：弹窗动作目标随之变为「启用」，不再停留在打开那一刻的「停用」。
    const dialog = await findModal('启用互认项目配置')
    expect(modalExists('停用互认项目配置')).toBe(false)

    const reloadAfterWrite = armLoad(pendingLoad())
    click(within(dialog).getByRole('button', { name: /确认启用/ }))
    await waitFor(() => expect(api.setRecognitionProjectConfigurationEnabled).toHaveBeenCalledTimes(1))
    expect(api.setRecognitionProjectConfigurationEnabled.mock.calls[0][1]).toEqual({ id: 'c1', enabled: true })
    await settle(reloadAfterWrite, [row('c1', 'A01', '血常规', 1)])
    await expectModalClosed('启用互认项目配置')
  })

  it('弹窗打开期间该行离开当前行集时，启停弹窗不再渲染也不再提交', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('停用配置：血常规（当前启用）'))
    await findModal('停用互认项目配置')

    const reload = armLoad(pendingLoad())
    click(screen.getByLabelText('重新读取互认配置'))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c2', 'A02', '尿常规', 1)])

    // 目标配置已不在本组织视图中：不对未知行渲染弹窗，也不提交写入。
    await expectModalClosed('停用互认项目配置')
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()
  })
})

/**
 * 设计条目：写弹窗的关闭入口与重开语义。
 * （旧用例名里的分组编号 B3-09 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 2 节「状态机」的「写操作中」行（重复点击不再发请求）与第 1 节「页面结构」的新增/修改弹窗行；
 * 新增表单在重开后回到默认值见 `docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」。
 *
 * 已覆盖：提交中点击遮罩与按 ESC 都不关闭（三条关闭入口同一条守卫）；
 * 关闭后再次打开时，新增弹窗表单回到默认值、修改弹窗回填当前行。
 *
 * 未覆盖（宿主浏览器验收项）：打开后焦点移入弹窗、关闭后焦点回到触发按钮。
 * rc-dialog 的焦点移交挂在下落动画完成的回调上（`onDialogVisibleChanged`），
 * 而本用例环境（jsdom + 关掉 CSS 的 vitest 配置）不产生该动画回调，实测弹窗打开后
 * `document.activeElement` 仍是页面背景，因此这里不构造「无判别力」的断言；
 * 该行为须在真实宿主浏览器里验收。本文件只守弹窗容器的对话框标记（见组内最后一条用例）。
 */
describe('写弹窗的关闭入口与重开语义', () => {
  /** 遮罩点击与 ESC：rc-dialog 需要先 mousedown 在遮罩上再 click，ESC 走 window 的 keydown。 */
  const clickMask = (title: string) => {
    const wrap = modalElement(title).closest('.ant-modal-wrap')
    if (!wrap) throw new Error(`未找到遮罩：${title}`)
    fireEvent.mouseDown(wrap)
    fireEvent.click(wrap)
  }

  it('提交中点击遮罩不关闭弹窗，提交结束后仍按正常路径关闭', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfiguration.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
    expect(create.querySelector('.ant-modal-close')).toBeNull()

    await act(async () => {
      clickMask('新增互认项目配置')
    })
    // 关掉弹窗会让这次尚未返回的写入失去可见反馈：遮罩点击必须被守卫拦下。
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await expectModalClosed('新增互认项目配置')
  })

  it('提交中按 ESC 不关闭弹窗', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfiguration.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))

    await act(async () => {
      fireEvent.keyDown(window, { key: 'Escape' })
    })
    expect(modalExists('新增互认项目配置')).toBe(true)

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await expectModalClosed('新增互认项目配置')
  })

  /**
   * 「关闭后重开回到默认值」必须在**同一挂载实例**内切换 `open` 才具备判别力：
   * 页面按 `modal.kind` 条件渲染弹窗，关闭时整个组件被卸载、重开必然是新实例（`useForm` 也是新实例），
   * 此时删掉弹窗里的复位 effect 用例照旧通过——它只能证明「新实例是空的」，不能证明复位发生。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 1 节「页面结构」（新增弹窗只提交标准项目编码与正整数天数）与
   * `Client/design.md`「页面与交互」（新增成功后清空新增表单）。
   *
   * 判别方式（变异验证）：删掉 `CreateConfigurationModal` 里 `open` 变化时的复位 effect，
   * `useForm` 的 store 会保留上一次输入，重开后下拉仍是「尿常规（A02）」、天数仍是 45，本条失败。
   */
  it('同一挂载实例内关闭再打开时，新增弹窗表单回到默认值', async () => {
    // 选择项直接由适配层的真实出口构造：本组只关心重开语义，不重复 C2/C3 的可选集合断言。
    const optionGroups = toSelectableOptionGroups(toSelectableStandardItems(CATALOG))
    const props = {
      optionGroups,
      catalogReady: true,
      writeGate: WRITABLE_GATE,
      onCancel: () => {},
      onSubmit: async () => {},
      submitting: false,
    }
    const view = render(<CreateConfigurationModal open {...props} />)

    const first = await findModal('新增互认项目配置')
    // 关闭前先确认表单确实持有用户输入，否则「重开后为空」可能只是因为压根没填进去。
    await pickOption(within(first).getByLabelText('标准项目'), '尿常规')
    await change(within(first).getByLabelText('可互认时间（天）'), '45')
    // antd 6 把已选项渲染在 `.ant-select-content`：有选中项时带 `ant-select-content-has-value` 且文本是所选标签，
    // 无选中项时同一节点只承载占位文案（与本文件其它用例按容器类名定位弹窗内部节点的做法一致）。
    const selectContent = (container: HTMLElement) => container.querySelector('.ant-select-content')
    expect(selectContent(first)?.textContent).toBe('尿常规（A02）')
    expect((within(first).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('45')

    // 同一实例内关闭再打开：只切换 `open`，不重新挂载组件。
    // （本用例环境是 jsdom 且关闭了 CSS，rc-dialog 的下落动画回调不产生，容器 DOM 不保证被移除，
    // 因此这里不断言「弹窗消失」，断言落在重开时表单的实际取值上。）
    view.rerender(<CreateConfigurationModal open={false} {...props} />)
    await act(async () => {})
    view.rerender(<CreateConfigurationModal open {...props} />)
    const second = await findModal('新增互认项目配置')
    expect(selectContent(second)?.classList.contains('ant-select-content-has-value') ?? false).toBe(false)
    expect((within(second).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('')
    expect(within(second).queryByText('可互认时间必填')).not.toBeInTheDocument()
  })

  it('关闭后再次打开时，修改弹窗回填当前行取值而不是上次输入', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 30 })])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const first = await findModal('修改可互认时间')
    const firstInput = within(first).getByLabelText('可互认时间（天）') as HTMLInputElement
    expect(firstInput.value).toBe('30')

    await change(firstInput, '7')
    click(within(first).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('修改可互认时间')
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const second = await findModal('修改可互认时间')
    expect((within(second).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
  })

  it('弹窗容器是标记完整的可聚焦对话框', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    click(addButton())
    const create = await findModal('新增互认项目配置')

    // 焦点进出弹窗本身不在组件层断言（原因见本组说明）：这里守的是 rc-dialog 交给宿主的对话框标记，
    // 缺了它们宿主浏览器也无法把焦点交给弹窗、也无法向读屏软件声明这是模态内容。
    expect(create).toHaveAttribute('role', 'dialog')
    expect(create).toHaveAttribute('aria-modal', 'true')
    expect(create).toHaveAttribute('tabindex', '-1')
    expect(create.getAttribute('aria-labelledby')).toBeTruthy()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C19 行（`submittingRef` 同步去重）与第 2 节「状态机」的「写操作中」行。
 */
describe('C19 写入期间防重复', () => {
  it('连续点击保存只产生一次写请求，按钮保持提交中状态', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfiguration.mockReturnValueOnce(write.promise)
    const save = within(create).getByRole('button', { name: /保\s*存/ })

    await act(async () => {
      fireEvent.click(save)
      fireEvent.click(save)
      fireEvent.click(save)
    })

    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
    expect(save).toHaveClass('ant-btn-loading')
    expect(modalExists('新增互认项目配置')).toBe(true)

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1)

    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await screen.findByText('尿常规')
  })

  it('启停确认连续点击只产生一次写请求', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('停用配置：血常规（当前启用）'))
    const confirm = await findModal('停用互认项目配置')
    const write = deferred<void>()
    api.setRecognitionProjectConfigurationEnabled.mockReturnValueOnce(write.promise)
    const ok = within(confirm).getByRole('button', { name: /确认停用/ })

    await act(async () => {
      fireEvent.click(ok)
      fireEvent.click(ok)
    })

    await waitFor(() => expect(api.setRecognitionProjectConfigurationEnabled).toHaveBeenCalledTimes(1))
    expect(ok).toHaveClass('ant-btn-loading')

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规', 2)])
  })
})

/**
 * 用例名只描述行为：重置筛选的可访问名称与实际重置范围一致。
 * （旧用例名里的分组编号 B7 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 1 节「页面结构」的筛选区行（编码输入框、配置状态 Segmented、重置按钮只影响本地展示）。
 */
describe('重置筛选的可访问名称与实际范围一致', () => {
  it('重置按钮同时清空编码与状态两项，名称如实说明范围', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    const codeFilter = () => screen.getByLabelText('标准项目编码筛选') as HTMLInputElement
    // Segmented 把外部 aria-label 透传到 role=radiogroup（已核对 @rc-component/segmented@1.3 的
    // `_extends({ ...默认值 }, divProps)` 顺序与宿主可访问性树），故按可访问名称定位并顺带守卫该名称。
    const statusFilter = () => screen.getByRole('radiogroup', { name: '配置状态筛选' })
    const selectedStatus = () =>
      [...statusFilter().querySelectorAll('.ant-segmented-item-selected')].map((node) => node.textContent?.trim())

    await change(codeFilter(), 'A0')
    click(within(statusFilter()).getByText('停用'))
    expect(selectedStatus()).toEqual(['停用'])
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()

    // 名称必须表达「编码 + 状态」两项，否则读屏用户会以为只重置了一项。
    click(screen.getByRole('button', { name: '重置编码与状态筛选' }))

    expect(codeFilter().value).toBe('')
    expect(selectedStatus()).toEqual(['全部'])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C21 行与第 2 节「状态机」的「刷新失败（写入成功后）」行（不改判为写失败）。
 */
describe('C21 写成功但刷新失败', () => {
  it('新增写入成功后刷新失败不改判为写失败，页面进入阻断态并可重试', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openCreateForm('尿常规', '30')
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    const failing = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failLoad(failing)

    await screen.findByText('写入已成功，数据待刷新')
    await expectModalClosed('新增互认项目配置')
    expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1)
    expect(addButton()).toBeDisabled()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeDisabled()

    const retry = armLoad(pendingLoad())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])

    await screen.findByText('尿常规')
    await waitFor(() => expect(addButton()).toBeEnabled())
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
  })

  it('写入失败保留弹窗与输入，不重载也不进入刷新阻断态', async () => {
    // 写失败的拒绝只由宿主统一展示，页面不得自行捕获（总体设计 5.2）。本用例**局部**安装未处理拒绝
    // 的观测器、用例结束即移除：既不依赖也不改动 `vite.config.ts` 的全局设置，因此
    // 「页面未吞掉写失败、也未额外产生别的拒绝」在这里是可判红的。
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded([row('c1', 'A01', '血常规')])

      const create = await openCreateForm('尿常规', '30')
      const rejection = new Error('写入失败')
      api.createRecognitionProjectConfiguration.mockRejectedValueOnce(rejection)
      click(within(create).getByRole('button', { name: /保\s*存/ }))

      await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      expect(modalExists('新增互认项目配置')).toBe(true)
      expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
      expect(query()).toHaveBeenCalledTimes(1)
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('互认配置数据待刷新')).not.toBeInTheDocument()

      // 失败后提交中状态必须归零，与成功路径的 loading 断言同一口径（写法见 C19 成功用例）。
      expect(within(create).getByRole('button', { name: /保\s*存/ })).not.toHaveClass('ant-btn-loading')
      await waitFor(() => expect(addButton()).toBeEnabled())
      expect(screen.getByLabelText('修改可互认时间：血常规')).toBeEnabled()
      // 收尾再判一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏判。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })

  it('写入在途时禁止取消与关闭，失败仍留在弹窗并在提交结束后恢复可关闭', async () => {
    // 本条同样把拒绝态 promise 交给 antd Modal 的 `onOk`；页面不得自行捕获（总体设计 5.2），
    // 故在本用例内局部观测该拒绝、用例结束即移除监听，使「恰好一笔且未被吞掉」在本条可判红。
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded([row('c1', 'A01', '血常规')])

      const create = await openCreateForm('尿常规', '30')
      const write = deferred<void>()
      api.createRecognitionProjectConfiguration.mockReturnValueOnce(write.promise)
      const save = within(create).getByRole('button', { name: /保\s*存/ })
      click(save)

      await waitFor(() => expect(api.createRecognitionProjectConfiguration).toHaveBeenCalledTimes(1))
      expect(modalExists('新增互认项目配置')).toBe(true)

      // 设计条目：写请求在途时全部关闭入口不可用，用户不能把一次尚未返回的写入从界面上抹掉
      // （旧用例内编号 B6 只在本文件内使用，无外部登记表）。
      // 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
      // 第 2 节「状态机」的「写操作中」行（全部禁用；写失败保留弹窗与输入）。
      expect(within(create).getByRole('button', { name: /取\s*消/ })).toBeDisabled()
      expect(create.querySelector('.ant-modal-close')).toBeNull()
      expect(within(create).getByRole('button', { name: /保\s*存/ })).toHaveClass('ant-btn-loading')
      expect(modalExists('新增互认项目配置')).toBe(true)

      const rejection = new Error('写入失败')
      await act(async () => {
        write.reject(rejection)
      })
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      // 失败结果仍可见：弹窗与输入保留，提交中状态归零后取消/关闭恢复可用（零重载）。
      expect(modalExists('新增互认项目配置')).toBe(true)
      expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
      expect(query()).toHaveBeenCalledTimes(1)
      await waitFor(() => expect(within(create).getByRole('button', { name: /取\s*消/ })).toBeEnabled())
      // 收尾再判一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏判。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」
 * （新增成功后保留组织和筛选条件、清空新增表单、刷新列表与选择数据）。
 */
describe('新增成功后的列表与选择数据刷新', () => {
  it('写入成功后关闭弹窗、保留筛选条件并刷新，新配置不再可选', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    await change(screen.getByLabelText('标准项目编码筛选'), 'A')
    const create = await openCreateForm('尿常规', '30')
    api.createRecognitionProjectConfiguration.mockResolvedValueOnce(undefined)
    const reload = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])

    await screen.findByText('尿常规')
    expect((screen.getByLabelText('标准项目编码筛选') as HTMLInputElement).value).toBe('A')
    await expectModalClosed('新增互认项目配置')

    // 刷新后的排除集合必须包含刚新增的配置：重开弹窗时它不再出现（同文件 C2 的取选项手法）。
    click(addButton())
    const again = await findModal('新增互认项目配置')
    await act(async () => {
      fireEvent.mouseDown(within(again).getByLabelText('标准项目'))
    })
    await waitFor(() => expect(optionLabels()).toEqual(['便常规（A03）']))
  })
})

/**
 * 用例名只描述行为：列表空态文案与计数口径。
 * （旧用例名里的分组编号 B1-07 / B3-05 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 5 节「已定实现边界」的「空态与计数口径」条。
 */
describe('列表空态与计数', () => {
  it('查询成功但本组织没有配置时显示空态文案且计数为 0', async () => {
    await renderLoaded([])

    expect(screen.getByText('当前组织没有互认项目配置')).toBeInTheDocument()
    expect(screen.getByText('本组织共 0 项配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('筛选后为空时区分空态文案，并同时给出本组织条数与筛选后条数', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    expect(screen.getByText('本组织共 2 项配置')).toBeInTheDocument()

    await change(screen.getByLabelText('标准项目编码筛选'), 'ZZ')

    // 本组织确实有配置，只是被本地筛掉：不得报成「当前组织没有互认项目配置」。
    expect(screen.getByText('没有匹配的配置')).toBeInTheDocument()
    expect(screen.queryByText('当前组织没有互认项目配置')).not.toBeInTheDocument()
    expect(screen.getByText('本组织共 2 项配置，筛选后 0 项')).toBeInTheDocument()
  })

  it('状态筛选同样进入筛选后为空的计数口径', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(within(screen.getByRole('radiogroup', { name: '配置状态筛选' })).getByText('停用'))

    expect(screen.getByText('没有匹配的配置')).toBeInTheDocument()
    expect(screen.getByText('本组织共 1 项配置，筛选后 0 项')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
  })
})

/**
 * 用例名只描述行为：行内动作的门控与列表规模下的渲染。
 * （旧用例名里的分组编号 B3-06 / B3-11 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 5 节「已定实现边界」的「不分页（S2-D7）」条与第 1 节「页面结构」的表格行操作行
 * （停用配置仍可修改时间；配置状态未知时不给出启停动作）。
 */
describe('行内动作门控与列表规模', () => {
  it('多行结果全部渲染，读取只发起一次', async () => {
    const rows = Array.from({ length: 25 }, (_, index) => {
      const code = `A${String(index + 1).padStart(2, '0')}`
      return row(`c${index + 1}`, code, `项目${index + 1}`)
    })
    await renderLoaded(rows)

    expect(screen.getAllByLabelText(/^修改可互认时间：项目/)).toHaveLength(25)
    // 页面按设计不分页：一次读取交付全部行，也不因渲染而追加读取。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(api.querySelectableStandardItems).toHaveBeenCalledTimes(1)
  })

  it('配置状态未知的行行内按钮禁用且零写请求', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1, { configurationStatus: null, configurationStatusText: '未知' })])

    // 未知状态不得被当作「启用」或「停用」：两个行内动作都不可用。
    const edit = screen.getByLabelText('修改可互认时间：血常规')
    const toggle = screen.getByLabelText('启用配置：血常规（当前未知）')
    expect(edit).toBeDisabled()
    expect(toggle).toBeDisabled()
    expect(screen.getByText('未知')).toBeInTheDocument()

    click(edit)
    click(toggle)
    await act(async () => {})

    expect(modalExists('修改可互认时间')).toBe(false)
    expect(modalExists('启用互认项目配置')).toBe(false)
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「API 与组件」的枚举中文来源段
 * 与 `Client/Pages/RecognitionProjects/RecognitionProjects.md` 第 5 节「已定实现边界」的
 * 「枚举元数据不是组织范围数据」条。
 */
describe('枚举中文来源', () => {
  it('表格显示服务端契约交付的枚举中文，不被本地兜底文案覆盖', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 2, {
      itemTypeText: '服务端类型文案',
      configurationStatusText: '服务端停用文案',
    })])

    expect(screen.getByText('服务端类型文案')).toBeInTheDocument()
    expect(screen.getByText('服务端停用文案')).toBeInTheDocument()
    // 契约文本存在时它就是唯一展示来源：该行 itemType 为 0，本地兜底文案「检验」不得再出现。
    expect(screen.queryByText('检验')).not.toBeInTheDocument()
    // 行内按钮的可访问名称里的"当前状态"同样取自契约文本，不得退回本地兜底文案（该行已停用，动作为启用）。
    expect(screen.getByLabelText('启用配置：血常规（当前服务端停用文案）')).toBeInTheDocument()
  })

  it('只读资料弹窗与表格同源，显示服务端契约文本', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 2, {
      itemTypeText: '服务端类型文案',
      configurationStatusText: '服务端停用文案',
    })])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const dialog = await screen.findByRole('dialog')

    expect(within(dialog).getByText('服务端类型文案')).toBeInTheDocument()
    expect(within(dialog).getByText('服务端停用文案')).toBeInTheDocument()
  })

  it('枚举元数据可用时，筛选标签、行内动作名称与启停弹窗标题都取服务端文案', async () => {
    // 故意给与本地兜底完全不同的中文：只有真的走元数据路径才会渲染出这些字。
    setMetadataOptions([
      { value: 1, name: 'Enabled', description: '元数据启用文案' },
      { value: 2, name: 'Disabled', description: '元数据停用文案' },
    ])
    await renderLoaded([row('c1', 'A01', '血常规')])

    const statusFilter = await screen.findByRole('radiogroup', { name: '配置状态筛选' })
    await waitFor(() => expect(within(statusFilter).getByText('元数据停用文案')).toBeInTheDocument())
    expect(within(statusFilter).queryByText('停用')).not.toBeInTheDocument()
    // 页面下发的枚举名必须与后端白名单一致：写错名称时服务端会拒绝，页面只会静默退回兜底文案。
    expect(clientRef.post).toHaveBeenCalledWith({ enumName: 'ConfigurationStatus' })

    const toggle = await screen.findByLabelText('元数据停用文案配置：血常规（当前启用）')
    click(toggle)

    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText('元数据停用文案互认项目配置')).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: '确认元数据停用文案' })).toBeInTheDocument()
  })
})
