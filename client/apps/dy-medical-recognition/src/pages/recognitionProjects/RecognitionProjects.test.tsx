/**
 * 阶段 2「互认项目」页面 Component 层用例（测试矩阵 C1-C3、C6、C8、C10-C11、C13-C21、C23、C26-C28 的非 Host 面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./recognitionProjectsApi` 只替换 5 个 I/O 出口（两个读取、三个写入），其余纯函数
 *    （请求适配、读模型映射、列视图合并与裁剪、排除集合、本地筛选、常量与类型）**使用真实实现**；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client——页面把 client 原样透传给适配层出口；
 * 3. 可信组织沿用既有两个包的边界：`@dy/auth` 的 `LoginUserManager.getOrgID()` 与
 *    `@dy/auth-react` 的 `useAuth()`。这里替换这两个包，并用外部 store + `useSyncExternalStore`
 *    模拟宿主重新签发 token，从而以生产同一路径驱动「组织变化」。
 *
 * 页面主体是四栏列视图（类型｜分类｜分组｜项目）：列表断言按层级格的可访问名称、列头类名与
 * 行的 `data-row-key` 定位；可互认时间的修改在项目行内就地完成，编辑控件按
 * `修改可互认时间：{名称}`／`可互认时间（天）`／`保存可互认时间：{名称}` 定位。
 *
 * 仍属 Host 层、不在本文件覆盖：C4 / C5 / C7 / C9 / C12 与 C17 / C21 的 Host 分支，
 * 以及真实宿主菜单进入和统一错误提示（Ticket 08）；窄视口下的真实横向滚动与列宽表现，
 * 以及弹窗的焦点移交/回落（打开后焦点进入弹窗、关闭后回到触发按钮），
 * 在 jsdom + 关闭 CSS 的用例环境里拿不到布局与 rc-dialog 动画完成回调，属宿主浏览器验收项
 * （详见对应用例组说明）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { useSyncExternalStore } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buildFilterSearch, parseFilterSearch } from './recognitionProjectsFilterUrl'
import {
  type EffectiveMedicalStandardCatalogReadModel,
  type MedicalItemTypeValue,
  type RecognitionProjectColumnRow,
  type RecognitionProjectConfigurationRow,
  type TrustedOrganizationScope,
} from './recognitionProjectsApi'
import { RecognitionProjects } from './RecognitionProjects'
import { RecognitionProjectBatchCreateModal, ToggleModal } from './RecognitionProjectModals'
import { RecognitionProjectRow } from './RecognitionProjectRow'
import { type RecognitionProjectWriteGate } from './recognitionProjectWriteGate'

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
  queryTrustedOrganizationScope: vi.fn(),
  createRecognitionProjectConfigurations: vi.fn(),
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
 * 按给定项目构造一次性目录：列视图把「目录里的项目」与「本组织的配置」合并展示，
 * 因此需要按目录内容确定分母的用例（例如 25 行全部已配置）必须同时给出目录，
 * 否则这些编码会多出一批「未配置」行，分母就不再是配置行数。
 */
function catalogOf(items: { code: string; name: string }[], groupName = '血液'): EffectiveMedicalStandardCatalogReadModel {
  return { itemTypes: [{ itemType: 0, categories: [{ categoryName: '检验分类', groups: [{ groupName, items }] }] }] }
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
  scope: ReturnType<typeof deferred<TrustedOrganizationScope>>
}

/** 弹窗数据源（可新增标准项目）的在途读取；与页面读取相互独立。 */
interface PendingSelectableLoad {
  items: ReturnType<typeof deferred<EffectiveMedicalStandardCatalogReadModel | null>>
}

/**
 * 已知可信组织的展示名称。名称由范围查询交付，与组织编码同源；
 * 用固定映射而不是让夹具照抄被测实现，避免断言失去判别力。
 */
const ORGANIZATION_NAMES: Record<string, string> = { 'ORG-A': '组织甲', 'ORG-B': '组织乙' }

/** 构造范围查询的返回：编码取给定组织，名称按同一编码派生。 */
function scopeFor(organizationCode: string): TrustedOrganizationScope {
  return { organizationCode, organizationName: ORGANIZATION_NAMES[organizationCode] ?? `${organizationCode}名称` }
}

/**
 * 模拟服务端的可新增标准项目查询（S2-D29）：返回「有效目录 − 本组织已配置」，
 * 且按关键字在编码/名称/分类/分组四列上做不区分大小写的字面包含匹配。
 * 排除与关键字过滤都由**服务端**完成，客户端不再做差集或本地裁剪。
 */
function selectableCatalog(
  catalog: EffectiveMedicalStandardCatalogReadModel | null,
  rows: Row[],
  keyword = '',
): EffectiveMedicalStandardCatalogReadModel | null {
  if (catalog === null) return null
  const configured = new Set(rows.map((row) => row.standardProjectCode).filter((code): code is string => !!code))
  const needle = keyword.trim().toLocaleLowerCase()
  const hit = (code: string, name: string, categoryName: string, groupName: string) =>
    needle.length === 0 || [code, name, categoryName, groupName].some((field) => field.toLocaleLowerCase().includes(needle))
  const itemTypes = (catalog.itemTypes ?? []).map((type) => ({
    ...type,
    categories: (type.categories ?? []).map((category) => ({
      ...category,
      groups: (category.groups ?? []).map((group) => ({
        ...group,
        items: (group.items ?? []).filter((item) => !configured.has(item.code ?? '')
          && hit(item.code ?? '', item.name ?? '', category.categoryName ?? '', group.groupName ?? '')),
      })).filter((group) => (group.items ?? []).length > 0),
    })).filter((category) => (category.groups ?? []).length > 0),
  })).filter((type) => (type.categories ?? []).length > 0)
  return { ...catalog, itemTypes }
}

function pendingLoad(): PendingLoad {
  return {
    configurations: deferred<Row[]>(),
    items: deferred<EffectiveMedicalStandardCatalogReadModel | null>(),
    scope: deferred<TrustedOrganizationScope>(),
  }
}

function pendingSelectableLoad(): PendingSelectableLoad {
  return { items: deferred<EffectiveMedicalStandardCatalogReadModel | null>() }
}

/**
 * 排队一次**页面**读取：页面只读配置列表与可信范围两次（S2-D29 之后页面不再读标准目录），
 * 因此这里只把这两个出口排成一次性返回；可新增项目由弹窗另一次读取交付（见 `armSelectableLoad`）。
 */
function armLoad(load: PendingLoad) {
  api.queryRecognitionProjectConfigurations.mockReturnValueOnce(load.configurations.promise)
  api.queryTrustedOrganizationScope.mockReturnValueOnce(load.scope.promise)
  return load
}

/** 排队一次**弹窗**读取（可新增标准项目），用于显式控制弹窗数据源的交付时机。 */
function armSelectableLoad(load: PendingSelectableLoad) {
  api.querySelectableStandardItems.mockReturnValueOnce(load.items.promise)
  return load
}

async function settle(load: PendingLoad, rows: Row[], catalog: EffectiveMedicalStandardCatalogReadModel | null = CATALOG) {
  // 记下本次结算的数据：随后未点名的读取（打开弹窗、写入后重载）按这一份返回。
  currentRows = rows
  currentCatalog = catalog
  await act(async () => {
    load.configurations.resolve(rows)
    // 范围查询返回当前可信组织与按同一编码派生的名称，使名称与编码同源。
    load.scope.resolve(scopeFor(authState.state.organizationCode ?? ''))
  })
}

/** 结算一次弹窗读取：交付的目录就是服务端会返回的可新增集合。 */
async function settleSelectable(load: PendingSelectableLoad, catalog: EffectiveMedicalStandardCatalogReadModel | null) {
  await act(async () => {
    load.items.resolve(catalog)
  })
}

async function failLoad(load: PendingLoad) {
  await act(async () => {
    load.configurations.reject(new Error('读取失败'))
    load.scope.reject(new Error('读取失败'))
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

/**
 * 模拟服务端的关键字过滤（S2-D29）：按字面包含同时匹配标准项目编码、名称、所属分类名称、
 * 所属分组名称，任一列命中即返回，比较不区分大小写。客户端不再做这一步裁剪。
 */
function keywordRows(rows: Row[], keyword: string): Row[] {
  const needle = keyword.trim().toLocaleLowerCase()
  if (needle.length === 0) return rows
  return rows.filter((row) => [
    row.standardProjectCode ?? '', row.standardItemName ?? '', row.categoryName ?? '', row.groupName ?? '',
  ].some((field) => field.toLocaleLowerCase().includes(needle)))
}

/**
 * 布置一个**已生效**的关键字筛选：输入原文只是草稿，必须经搜索按钮或回车提交（S2-D21）。
 * 按 S2-D29 提交会把关键字下推服务端查询，因此这里排队并结算一次读取——交付的是服务端
 * 按关键字过滤后的行集；`currentRows`（"全集"夹具）不被这次结算改写，清空条件后重读才能拿到全量。
 * 需要「筛选确实在起作用」的用例统一走这里；验证草稿、提交时机与读取本身的用例才分步操作。
 */
async function applyKeywordFilter(value: string) {
  await change(screen.getByLabelText('关键字筛选'), value)
  const load = armLoad(pendingLoad())
  submitSearch()
  await act(async () => {
    load.configurations.resolve(keywordRows(currentRows, value))
    load.scope.resolve(scopeFor(authState.state.organizationCode ?? ''))
  })
  await settleUnarmedReload()
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
/** 主操作按钮；名称与页面标题、宿主菜单项同为「互认项目」（S2-D23）。 */
const addButton = () => screen.getByRole('button', { name: /新增互认项目/ })

/**
 * 直接渲染行内编辑器：读取失败与读取在途时页面的编辑入口本身是禁用的，
 * 「编辑器已挂载但写被冻结」无法通过页面交互到达，只能这样固定门控口径。
 * 调用方只给出与门控相关的差异，其余 props 取可写行的常用值。
 */
function renderInlineEditor(overrides: Partial<Parameters<typeof RecognitionProjectRow>[0]> = {}) {
  const columnRow: RecognitionProjectColumnRow = {
    rowIdentity: 'c1',
    standardProjectCode: 'A01',
    standardItemName: '血常规',
    configured: true,
    configurationId: 'c1',
    recognitionDurationDays: 30,
    configurationStatus: 1,
    configurationStatusText: '启用',
    unavailableReason: null,
  }
  const props = {
    row: columnRow,
    writeGate: WRITABLE_GATE,
    submitting: false,
    canWrite: true,
    actionText: () => '停用',
    editing: true,
    onStartEdit: () => {},
    onCancelEdit: () => {},
    onSubmitDuration: async () => {},
    onToggle: () => {},
    ...overrides,
  }
  return render(<RecognitionProjectRow {...props} />)
}

/** 列视图表头文本：类型｜分类｜分组｜项目四栏恒定全显，因此这里始终是四项。 */
const cascadeHeads = () =>
  Array.from(document.querySelectorAll<HTMLElement>('.recognition-projects-cascade-head'))

/** 层级格按可访问名称定位：名称同时表达被点中的层级与它的下级范围。 */
const levelButton = (name: string) => screen.getByRole('button', { name })

/** 层级格是否处于高亮态：高亮只改背景，可观察的差异就是这个类名。 */
const isHighlighted = (element: HTMLElement) => element.classList.contains('recognition-projects-level-highlighted')

/**
 * 项目格的高亮态，按 DOM 顺序返回，与 `GROUP_CELLS` 一一对应（每个分组一个项目格）。
 * 项目层是下级链路的最后一层：只断言层级格会漏掉它，页面在真实宿主里就会出现
 * 「分组亮了、它名下的项目行没亮」，看不出项目行归属于哪一次点击。
 */
const itemCellHighlightStates = (): boolean[] =>
  Array.from(document.querySelectorAll<HTMLElement>('.recognition-projects-cascade-items'))
    .map((node) => isHighlighted(node))

/**
 * 当前高亮态：以层级格的可访问名称为键，一次比对整份高亮状态。
 * 用映射而不是按渲染顺序的数组，断言因此只表达「哪一格高亮」，不额外约束渲染顺序。
 */
const levelHighlightStates = (): Record<string, boolean> =>
  Object.fromEntries(Array.from(document.querySelectorAll<HTMLElement>('.recognition-projects-level')).map((node) => [
    node.getAttribute('aria-label') ?? '',
    isHighlighted(node),
  ]))

/** 列视图行外壳：行标识渲染在 `data-row-key` 上，按它定位到具体那一行。 */
function rowElement(rowIdentity: string): HTMLElement {
  const found = Array.from(document.querySelectorAll<HTMLElement>('[data-row-key]')).find(
    (node) => node.getAttribute('data-row-key') === rowIdentity,
  )
  if (!found) throw new Error(`未找到行：${rowIdentity}`)
  return found
}

/**
 * 批量新增弹窗里的项目复选框：可访问名称同时给出标准项目名称与编码，按名称逐项定位。
 * 层级格的复选框是本组唯一的整组勾选控件，同一范围内两者名称相同、按角色区分。
 */
const batchItemCheckbox = (create: HTMLElement, item: string) =>
  within(create).getByRole('checkbox', { name: `选择标准项目：${item}` })

/** 弹窗内当前可选的标准项目；按可访问名称取，顺序即目录顺序；无可选项时返回空数组。 */
const batchItemLabels = (create: HTMLElement) =>
  within(create).queryAllByRole('checkbox')
    .map((node) => node.getAttribute('aria-label') ?? '')
    .filter((label) => label.startsWith('选择标准项目：'))

/** 层级格的整组勾选控件：类型格、分类格与分组格共用同一名称模板。 */
const batchLevelCheckbox = (create: HTMLElement, name: string) => within(create).getByRole('checkbox', { name })

/** 弹窗底部的已选计数文本。 */
const batchSelectedCount = (create: HTMLElement) =>
  within(create).getByText(/^已选 \d+ 项$/).textContent

/** 当前配置状态筛选的选中项；Segmented 把外部 aria-label 透传到 role=radiogroup。 */
const selectedStatusLabels = () =>
  [...screen.getByRole('radiogroup', { name: '配置状态筛选' }).querySelectorAll('.ant-segmented-item-selected')]
    .map((node) => node.textContent?.trim())

/**
 * 最近一次结算的页面数据。打开新增弹窗会触发一次重读（S2-D25），写入成功后也会重载；
 * 这些未点名的读取按这一份返回，使既有装配不被额外读取扰动，同时保持「读到的是本组织当前数据」。
 */
let currentRows: Row[] = []
let currentCatalog: EffectiveMedicalStandardCatalogReadModel | null = CATALOG

/**
 * 未点名读取的兜底 mock：按调用时点的当前可信组织与最近一次结算的数据返回。
 * 实现若放行了写操作，写完成后的重载（含随后被应用的组织切换重载）因此拿到真实形状的结果，
 * 使用例以断言失败结束，而不是先撞上渲染异常。
 */
function stubUnarmedLoads() {
  api.queryRecognitionProjectConfigurations.mockImplementation(async () => currentRows)
  // 未点名的弹窗读取按服务端口径返回可新增集合（有效目录 − 本组织已配置，并按关键字过滤），
  // 使弹窗展示的就是"还能新增哪些"。
  api.querySelectableStandardItems.mockImplementation(async (_client, keyword) => selectableCatalog(currentCatalog, currentRows, keyword))
  api.queryTrustedOrganizationScope.mockImplementation(async () => scopeFor(authState.state.organizationCode ?? ''))
}

/**
 * 页头组织范围的展示形态。名称与编码各占一行，因此按元素分别取，
 * 同时断言不存在「组织」文字标签——标签的有无是 S2-D22 的判定点之一。
 */
const scopeLines = () => ({
  name: document.querySelector('.recognition-projects-scope-name')?.textContent ?? null,
  code: document.querySelector('.recognition-projects-scope-code')?.textContent ?? null,
  label: document.querySelector('.recognition-projects-scope-label')?.textContent ?? null,
})

/**
 * 触发一次重新读取。按 S2-D24，读取由搜索提交发起；输入框为空时提交等价于刷新，
 * 因此需要「刷新」语义的用例统一走这里，不再有独立的刷新按钮。
 */
const submitSearch = () => click(screen.getByRole('button', { name: '搜索' }))

/** 等未点名装配的那次重读落地：兜底实现立即返回，冲刷微任务即可（弹窗读取多一层 then，多冲几拍）。 */
async function settleUnarmedReload() {
  for (let i = 0; i < 4; i += 1) {
    await act(async () => { await Promise.resolve() })
  }
}

async function renderLoaded(rows: Row[], catalog: EffectiveMedicalStandardCatalogReadModel | null = CATALOG) {
  const load = armLoad(pendingLoad())
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(load, rows, catalog)
  await waitFor(() => expect(addButton()).toBeEnabled())
}

/**
 * 用另一行的启停写入触发一次后台重载。这条路径不改筛选条件（S2-D24），
 * 正在渲染的行内编辑态因此保留，可以单独观察「编辑态是否随本次读取交付的行集重取」；
 * 走搜索提交则会在应用条件时一并清掉编辑态，观察不到这一点。
 */
async function reloadByTogglingOtherRow(accessibleName: string, rows: Row[]) {
  click(screen.getByLabelText(accessibleName))
  const confirm = await findModal('停用互认项目配置')
  const reload = armLoad(pendingLoad())
  click(within(confirm).getByRole('button', { name: /确认停用/ }))
  await waitFor(() => expect(api.setRecognitionProjectConfigurationEnabled).toHaveBeenCalledTimes(1))
  await settle(reload, rows)
  await expectModalClosed('停用互认项目配置')
}

/** 打开批量新增弹窗，勾选给定标准项目并填写统一天数（不提交）。 */
async function openBatchCreate(items: string[], duration: string) {
  click(addButton())
  const create = await findModal('新增互认项目配置')
  // 打开弹窗会触发一次重读（S2-D25）：等它落地，否则确定按钮仍停在读取在途的禁用态。
  await settleUnarmedReload()
  for (const item of items) click(batchItemCheckbox(create, item))
  await change(within(create).getByLabelText('可互认时间（天）'), duration)
  return create
}

beforeEach(() => {
  authState.reset()
  // 页面按地址栏查询参数初始化已生效筛选（S2-D27），而 jsdom 在整份用例间共用同一个地址：
  // 上一条用例回写的条件会带进下一条，因此每条用例开始前清掉查询串。
  window.history.replaceState(null, '', window.location.pathname)
  for (const fn of Object.values(api)) fn.mockReset()
  // 每条用例一个 client 实例（隔离 hook 的按实例缓存），枚举元数据端点默认不可用。
  clientRef.post.mockReset()
  clientRef.current = { api: { enumMetadata: { getEnumMetadata: { post: clientRef.post } } } }
  setMetadataOptions(null)
  // 未点名读取的兜底数据按用例隔离：上一条用例结算过的行集与目录不得泄漏进来。
  currentRows = []
  currentCatalog = CATALOG
  stubUnarmedLoads()
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
  it('进入页面自动加载当前组织的配置列表与组织范围，加载期间禁止写操作；可新增项目由弹窗打开时再读', async () => {
    const load = armLoad(pendingLoad())
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 页面不再读标准目录（S2-D29）：可新增项目由弹窗自己那次查询交付，页面加载只读配置与组织范围。
    expect(api.querySelectableStandardItems).not.toHaveBeenCalled()
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
    expect(scopeLines()).toEqual({ name: '组织甲', code: 'ORG-A', label: null })
  })

  it('只展示查询契约交付的列，不展示创建修改信息与已删除的派生状态', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    // 四栏列视图没有表格语义，列头是网格里的文本节点：按列头类名取，顺序即四栏顺序。
    expect(cascadeHeads().map((node) => node.textContent)).toEqual(['类型', '分类', '分组', '项目'])
    // 契约没有交付的字段名一律不出现在页面上（与旧断言同一口径，只是换了定位方式）。
    for (const removed of ['IsStandardCatalogValid', 'IsAvailableForNewMatch', '创建人', '修改时间', '最后修改']) {
      expect(screen.queryByText(removed)).not.toBeInTheDocument()
    }
  })

  it('可信组织缺失或为空白时进入不可操作状态，且不发起任何请求', async () => {
    authState.change({ organizationCode: null, token: null })
    renderPage()
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
    expect(api.querySelectableStandardItems).not.toHaveBeenCalled()
    expect(api.queryTrustedOrganizationScope).not.toHaveBeenCalled()
    expect(screen.queryByRole('button', { name: /新增互认项目/ })).not.toBeInTheDocument()

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

  it('弹窗数据源读取失败只在弹窗内提示数据待刷新，页面列表与行内操作不受影响', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    // 弹窗数据源与页面列表是两条独立读取（S2-D29）：它的失败只影响弹窗内提示与重读入口，
    // 不把页面拖进阻断态，也不禁用列表里的行内操作。
    api.querySelectableStandardItems.mockRejectedValueOnce(new Error('可新增项目读取失败'))
    click(addButton())
    const create = await findModal('新增互认项目配置')

    await within(create).findByText('标准目录数据待刷新')
    expect(addButton()).toBeEnabled()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeEnabled()
    expect(screen.queryByText('互认配置数据待刷新')).not.toBeInTheDocument()
    expect(within(create).getByRole('button', { name: /重新读取/ })).toBeInTheDocument()
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
   * 未读到目录时把空的可选集合误报成「没有可新增的标准项目」，会让用户以为目录里确实没有可选项。
   */
  const renderBatchModal = (catalogReady: boolean) =>
    render(<RecognitionProjectBatchCreateModal
      open
      view={[]}
      catalogReady={catalogReady}
      keyword=''
      onKeywordSearch={() => {}}
      selection={new Set<string>()}
      durationInput=''
      writeGate={WRITABLE_GATE}
      submitting={false}
      reloading={false}
      prunedCount={0}
      onReload={() => {}}
      onSelectionChange={() => {}}
      onDurationChange={() => {}}
      onCancel={() => {}}
      onSubmit={async () => {}}
    />)

  it('目录未就绪时提示“标准目录数据待刷新”，不报“没有可新增的标准项目”', async () => {
    renderBatchModal(false)

    await screen.findByText('标准目录数据待刷新')
    expect(screen.queryByText('没有可新增的标准项目')).not.toBeInTheDocument()
  })

  it('目录已就绪且无剩余可选项时提示“没有可新增的标准项目”', async () => {
    renderBatchModal(true)

    await screen.findByText('没有可新增的标准项目')
    expect(screen.queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
  })

  it('页面在目录确实没有可新增项时给出同一提示，确认文案出口一致', async () => {
    // 目录已就绪但其中的项目全部已配置：可选集合为空，提示必须来自「目录已就绪」这一分支。
    await renderLoaded([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规'), row('c3', 'A03', '便常规')])

    click(addButton())
    const create = await findModal('新增互认项目配置')
    await within(create).findByText('没有可新增的标准项目')
    expect(within(create).queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
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
    await within(create).findByText('没有可新增的标准项目')
    expect(within(create).queryByText('标准目录数据待刷新')).not.toBeInTheDocument()
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
 * 因此只能靠出现序号补齐唯一行标识；两行的可互认时间与配置状态不同，于是每行的操作按钮可访问名称
 * 与时间数值都能指认「这是第几行的数据」。标识冲突导致行错位、串位或只渲染部分行时，
 * 这些按行定位的断言会失败。
 *
 * **防御性验证**：正常业务路径下 `configurationId` 必为非空 Guid（契约唯一键），本夹具刻意构造
 * 标识全缺的两个相同行，只为验证行标识回退规则本身的健壮性（Testing Baseline §1.8：
 * 正常业务不可达的数据可以构造，但必须标注目的，且不用于验证正常业务行为）。
 */
describe('列视图行标识稳定唯一', () => {
  it('业务字段完全相同的多行互不串位，操作与数值都指向各自那一行', async () => {
    // 防御性夹具（见本组说明）：两行的 configurationId 与 standardProjectCode 都缺失。
    // 目录按空内容交付：被测事实是这两行的行标识，目录里的项目只会引入额外行、干扰分母。
    await renderLoaded([
      row('', '', '匿名项目', 1, { configurationId: null, standardProjectCode: null }),
      row('', '', '匿名项目', 2, { configurationId: null, standardProjectCode: null, recognitionDurationDays: 45 }),
    ], catalogOf([]))

    // 两行都必须渲染出来（下面的按行断言都以两行为前提）。
    expect(screen.getAllByText('匿名项目')).toHaveLength(2)

    const dataRows = Array.from(document.querySelectorAll<HTMLElement>('[data-row-key]'))
    expect(dataRows).toHaveLength(2)

    // 判别标识是否唯一不依赖 React 的告警文本：列视图把实际使用的行标识渲染为行的 `data-row-key`，
    // 两行必须互不相同；重复标识会触发 React 告警并可能让同行数据错配，可观察的差异就是 `data-row-key` 重复。
    const rowKeys = dataRows.map((node) => node.getAttribute('data-row-key'))
    expect(new Set(rowKeys).size).toBe(2)
    // 标识由业务键与出现序号组成（两行的业务字段完全相同，只能靠出现序号区分），不是数组下标。
    expect(rowKeys).toEqual(['|匿名项目', '|匿名项目#1'])

    // 第 1 行：时间 30、状态启用 → 动作目标为停用。
    expect(within(dataRows[0]).getByText('30')).toBeInTheDocument()
    expect(within(dataRows[0]).getByLabelText('停用配置：匿名项目（当前启用）')).toBeDisabled()
    expect(within(dataRows[0]).queryByLabelText('启用配置：匿名项目（当前停用）')).toBeNull()

    // 第 2 行：时间 45、状态停用 → 动作目标为启用。串位时这两行会互换。
    expect(within(dataRows[1]).getByText('45')).toBeInTheDocument()
    expect(within(dataRows[1]).getByLabelText('启用配置：匿名项目（当前停用）')).toBeDisabled()
    expect(within(dataRows[1]).queryByLabelText('停用配置：匿名项目（当前启用）')).toBeNull()

    // 标识缺失的行本就不具备写能力（没有配置标识），禁用态与标识稳定无关，是既有行为。
    expect(within(dataRows[0]).getByLabelText('修改可互认时间：匿名项目')).toBeDisabled()
    expect(within(dataRows[1]).getByLabelText('修改可互认时间：匿名项目')).toBeDisabled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C14 行与第 2 节「状态机」的「加载中（手动刷新）」行。
 */
describe('C14 手动刷新', () => {
  it('重新加载配置列表，刷新期间禁止写操作', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const refresh = armLoad(pendingLoad())
    submitSearch()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    // 搜索只重读配置列表与组织范围；可新增项目由弹窗自己查询，页面搜索不牵动它。
    expect(api.querySelectableStandardItems).not.toHaveBeenCalled()
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
    submitSearch()
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
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    // 配置列表成功、组织范围失败：整次读取判定为失败，但成功来源交付的行仍展示出来。
    await act(async () => {
      failing.configurations.resolve([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
      failing.scope.reject(new Error('组织范围读取失败'))
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
    await applyKeywordFilter('A')
    click(within(screen.getByRole('radiogroup', { name: '配置状态筛选' })).getByText('启用'))
    click(addButton())
    const before = await findModal('新增互认项目配置')
    await waitFor(() => expect(batchItemLabels(before)).toEqual(['选择标准项目：尿常规（A02）', '选择标准项目：便常规（A03）']))
    // 关闭弹窗，制造“没有未保存内容”的切换前提；筛选保持生效。
    click(within(before).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')

    // 读取次序：进入页面 1 次、搜索提交 1 次、切换组织 1 次（打开弹窗只查可新增项目，不进配置列表读取）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-B' })
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    // 组织编码随可信令牌立即更新；名称来自本次范围查询，读取返回前只展示编码（S2-D22）。
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-B', label: null })
    // applyOrganization 的复位：编码与状态筛选回到默认，否则旧筛选会隐藏新组织的配置。
    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('')
    expect(selectedStatusLabels()).toEqual(['全部'])

    // 可选集合必须来自目标组织本次读取的目录，而不是旧组织残留的目录数据。
    await settle(target, [row('c9', 'B01', '乙院项目')], ORG_B_CATALOG)
    await screen.findByText('乙院项目')
    expect(scopeLines()).toEqual({ name: '组织乙', code: 'ORG-B', label: null })
    click(addButton())
    const after = await findModal('新增互认项目配置')
    await waitFor(() => expect(batchItemLabels(after)).toEqual(['选择标准项目：丙院项目（C01）']))
  })

  // 设计条目：写在途期间页面已经切到别的组织时，写完成后的重载必须被放弃，不得把旧组织结果刷进新视图。
  // （旧用例名里的分组编号 B3-02 只在本文件内使用，无外部登记表。）
  // 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
  // 第 2 节「状态机」补充规则（写完成后的重载前校验「提交时的组织仍是页面当前组织」，否则不重载）。
  it('写在途时确认切换组织，写完成后不重载也不展示旧组织行', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfigurations.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /确定新增/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))

    // 写在途时宿主切换组织：页面弹出待切换确认框，用户确认放弃未保存内容。
    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    const target = armLoad(pendingLoad())
    click(within(confirm).getByRole('button', { name: /放弃并切换/ }))
    // 读取次序：进入页面 1 次、确认切换组织 1 次（打开弹窗只查可新增项目，不进配置列表读取）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })

    // 写请求此刻才返回：页面已经是 ORG-B，守卫必须放弃这次重载，旧组织行不得出现。
    await act(async () => {
      write.resolve()
    })
    await settle(target, [row('c9', 'B01', '乙院项目')])

    expect(query()).toHaveBeenCalledTimes(2)
    expect(screen.getByText('乙院项目')).toBeInTheDocument()
    // 旧组织的配置行不得出现：页面行集就是本组织已配置项（S2-D16），因此按行是否仍在来判定。
    expect(screen.queryByLabelText('修改可互认时间：血常规')).toBeNull()
    expect(screen.queryByLabelText('停用配置：血常规（当前启用）')).toBeNull()
  })

  it('C10：组织变化后迟到的旧组织响应不得污染新组织', async () => {
    const first = armLoad(pendingLoad())
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    await settle(target, [row('c9', 'B01', '乙院项目')], catalogOf([{ code: 'B01', name: '乙院项目' }]))
    await screen.findByText('乙院项目')

    await settle(first, [row('c1', 'A01', '血常规')])
    // 旧组织的**配置**行不得进入新组织视图：迟到的旧响应若被应用，血常规会带着配置标识与操作控件一起出现。
    await waitFor(() => expect(screen.queryByLabelText('修改可互认时间：血常规')).toBeNull())
    expect(screen.queryByLabelText('停用配置：血常规（当前启用）')).toBeNull()
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

    const create = await openBatchCreate(['尿常规（A02）'], '45')
    await switchTrustedOrganization('ORG-B')

    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')

    // 取消分支：不重载、不清理，未保存内容与旧组织列表都保留，并以提示条说明待切换。
    // 读取只有进入页面与打开弹窗两次。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(screen.getByText('宿主组织已变化，尚有未保存内容')).toBeInTheDocument()
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('45')
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(scopeLines()).toEqual({ name: '组织甲', code: 'ORG-A', label: null })
    expect(addButton()).toBeDisabled()

    // 确认分支：清空旧组织的列表与弹窗，加载目标组织。
    const target = armLoad(pendingLoad())
    click(screen.getByRole('button', { name: '放弃并切换' }))
    const again = await findModal('切换组织将放弃未保存内容')
    click(within(again).getByRole('button', { name: /放弃并切换/ }))

    // 读取次序：进入页面 1 次、确认切换组织 1 次（打开弹窗只查可新增项目，不进配置列表读取）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })
    await expectModalClosed('新增互认项目配置')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    // 组织编码随可信令牌立即更新；名称来自本次范围查询，读取返回前只展示编码（S2-D22）。
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-B', label: null })

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
    expect(scopeLines()).toEqual({ name: '组织乙', code: 'ORG-B', label: null })
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

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')
    await screen.findByText('宿主组织已变化，尚有未保存内容')
    // 读取只有进入页面与打开弹窗两次；待切换期间不加载目标组织。
    expect(query()).toHaveBeenCalledTimes(1)

    // 放弃未保存内容：关闭写弹窗后，「未保存内容」不再阻断切换。
    const target = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    // 读取次序：进入页面 1 次、放弃未保存内容后的自动切换 1 次（打开弹窗只查可新增项目）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B' })
    // 组织编码随可信令牌立即更新；名称来自本次范围查询，读取返回前只展示编码（S2-D22）。
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-B', label: null })
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.queryByText('宿主组织已变化，尚有未保存内容')).not.toBeInTheDocument()

    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
    expect(scopeLines()).toEqual({ name: '组织乙', code: 'ORG-B', label: null })
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
    const create = await openBatchCreate(['尿常规（A02）'], '30')

    // 宿主重新签发 token 把组织切到 ORG-B：还有未保存内容，页面停在「组织待切换」确认框。
    await switchTrustedOrganization('ORG-B')
    await findModal('切换组织将放弃未保存内容')

    // 万一实现放行了写入，这次刷新会被消费；组织未对齐时它不应被用到。
    armLoad(pendingLoad())
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    const save = within(create).getByRole('button', { name: /确定新增/ })
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    // 读取只有进入页面与打开弹窗两次：组织未对齐期间不加载目标组织。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
    expect(save).toBeDisabled()
  })

  it('取消组织切换确认后（deferred），写弹窗提交仍为零写请求', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    const create = await openBatchCreate(['尿常规（A02）'], '30')

    await switchTrustedOrganization('ORG-B')
    const confirm = await findModal('切换组织将放弃未保存内容')
    click(within(confirm).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('切换组织将放弃未保存内容')
    await screen.findByText('宿主组织已变化，尚有未保存内容')

    armLoad(pendingLoad())
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    const save = within(create).getByRole('button', { name: /确定新增/ })
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    // 读取只有进入页面与打开弹窗两次：待切换期间不加载目标组织。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect(save).toBeDisabled()
  })

  it('修改时间入口在组织尚未一致时同样零写请求', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    // 在组织仍然一致、写能力可用时进入行内编辑：编辑器已挂载，随后组织被切换。
    click(screen.getByLabelText('修改可互认时间：血常规'))
    const editor = await screen.findByLabelText('可互认时间（天）')

    await switchTrustedOrganization('ORG-B')
    await findModal('切换组织将放弃未保存内容')

    armLoad(pendingLoad())
    api.updateRecognitionDuration.mockResolvedValueOnce(undefined)
    const save = screen.getByLabelText('保存可互认时间：血常规')
    // 组织未一致时保存被冻结，编辑器与输入保留，用户能看到原因。
    expect(save).toBeDisabled()
    expect(screen.getByText('写入已暂停')).toBeInTheDocument()
    expect((editor as HTMLInputElement).value).toBe('30')
    click(save)
    await act(async () => {})

    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(screen.getByLabelText('可互认时间（天）')).toBeInTheDocument()
  })

  /**
   * 读取失败与读取在途时页面的写入口本身是禁用的，因此「写界面已打开但读取失败/在途」无法通过页面
   * 交互到达，只能直接渲染写界面固定该口径：页面以 `writeGate` 冻结提交按钮并给出暂停原因，
   * 已打开的写界面不会基于不完整数据写出。两个写弹窗与行内编辑器都必须守住同一条规则。
   */
  it('读取失败或组织尚未一致时两个写弹窗与行内编辑器的提交按钮禁用且不触发提交', async () => {
    const createSubmit = vi.fn(async () => {})
    const createProps = { open: true, view: [], catalogReady: true, keyword: '', onKeywordSearch: () => {}, selection: new Set<string>(), durationInput: '30', onSelectionChange: () => {}, onDurationChange: () => {}, onCancel: () => {}, submitting: false, reloading: false, prunedCount: 0, onReload: () => {} }
    const readFailureGate: RecognitionProjectWriteGate = { organizationReady: true, canWrite: false, loadFailure: 'load', organizationRecheckFailure: null }

    const create = render(<RecognitionProjectBatchCreateModal {...createProps} onSubmit={createSubmit} writeGate={readFailureGate} />)
    const createDialog = modal('新增互认项目配置')
    const createOk = createDialog.getByRole('button', { name: /确定新增/ })
    expect(createOk).toBeDisabled()
    expect(createDialog.getByText('写入已暂停')).toBeInTheDocument()
    expect(createDialog.getByText('读取失败，已有数据保留。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    click(createOk)
    await act(async () => {})
    expect(createSubmit).not.toHaveBeenCalled()
    create.unmount()

    // 写入成功后的刷新失败与一般读取失败提示口径不同，同一写界面必须区分。
    // 行内编辑器与写弹窗取同一份门控文案：这里直接渲染编辑器，固定该口径。
    const refreshGate: RecognitionProjectWriteGate = { organizationReady: true, canWrite: false, loadFailure: 'refreshAfterWrite', organizationRecheckFailure: null }
    const refreshBlocked = vi.fn(async () => {})
    const editor = renderInlineEditor({ writeGate: refreshGate, onSubmitDuration: refreshBlocked })
    expect(editor.getByRole('button', { name: '保存可互认时间：血常规' })).toBeDisabled()
    expect(editor.getByText('写入已经成功，但重新读取配置与标准项目失败。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    click(editor.getByRole('button', { name: '保存可互认时间：血常规' }))
    await act(async () => {})
    expect(refreshBlocked).not.toHaveBeenCalled()
    editor.unmount()

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
   * 阻断原因的判定次序固定为「组织未一致（含提交时刻复核失败）→ 读取失败 → 读取在途」，
   * `submitting` 只决定确定按钮保持可点并用 `confirmLoading` 反馈，不得掩盖更强的阻断原因。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」的「写弹窗门控与提交前复核」段（写弹窗与行内编辑器统一禁用提交并说明原因）。
   *
   * 判别方式：以下四段的门控都带 `submitting`，差异只在其它分量；旧实现（提交在途时直接返回无原因）
   * 会让前两段的暂停说明消失，断言失败。
   */
  it('提交在途不掩盖更强的阻断原因，读取在途则不是阻断原因', async () => {
    // 组织未一致 + 读取失败 + 提交在途：组织类原因优先。
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

    // 组织已一致 + 读取失败 + 提交在途：读取失败原因优先于提交在途。
    const readBlocked = renderInlineEditor({
      writeGate: { organizationReady: true, canWrite: false, loadFailure: 'load', organizationRecheckFailure: null },
      submitting: true,
    })
    expect(readBlocked.getByText('读取失败，已有数据保留。维护操作暂不可用，请先重试读取。')).toBeInTheDocument()
    expect(readBlocked.getByRole('button', { name: '保存可互认时间：血常规' })).toBeDisabled()
    readBlocked.unmount()

    // 组织已一致、读取未失败，`canWrite` 为假只因页面把 submitting 计入门控：这不是阻断原因，
    // 保存按钮保持可点并由提交中状态反馈，界面内不出现暂停说明。
    const submittingOnly = renderInlineEditor({
      writeGate: { organizationReady: true, canWrite: false, loadFailure: null, organizationRecheckFailure: null },
      submitting: true,
    })
    const saveDuration = submittingOnly.getByRole('button', { name: '保存可互认时间：血常规' })
    expect(saveDuration).toBeEnabled()
    expect(saveDuration).toHaveClass('ant-btn-loading')
    expect(submittingOnly.queryByText('写入已暂停')).not.toBeInTheDocument()
    submittingOnly.unmount()

    // 提交期复核留下的原因与组织未一致同属组织类阻断：即使页面渲染期门控已放行也必须显示。
    const rechecked = render(<RecognitionProjectBatchCreateModal
      open
      view={[]}
      catalogReady
      keyword=''
      onKeywordSearch={() => {}}
      selection={new Set<string>()}
      durationInput='30'
      writeGate={{ ...WRITABLE_GATE, organizationRecheckFailure: '提交时复核发现当前可信组织与页面已应用组织不一致。' }}
      onSelectionChange={() => {}}
      onDurationChange={() => {}}
      onCancel={() => {}}
      onSubmit={async () => {}}
      submitting={false}
      reloading={false}
      prunedCount={0}
      onReload={() => {}}
    />)
    const recheckedDialog = modal('新增互认项目配置')
    expect(recheckedDialog.getByText('提交时复核发现当前可信组织与页面已应用组织不一致。')).toBeInTheDocument()
    expect(recheckedDialog.getByRole('button', { name: /确定新增/ })).toBeDisabled()
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
    const create = await openBatchCreate(['尿常规（A02）'], '30')

    // 组织编码变了，但不通知 token 订阅者：页面不重渲染，第一道门控仍是上一帧的「已对齐」。
    authState.changeOrganizationSilently('ORG-B')

    // 万一实现放行了写入，这次调用（以及下面的排队读取）会被消费；提交期复核必须让它保持为零。
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    armLoad(pendingLoad())
    const save = within(create).getByRole('button', { name: /确定新增/ })
    expect(save).toBeEnabled()
    click(save)
    await act(async () => {})

    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    // 读取只有进入页面与打开弹窗两次：提交期复核拦下时不再发起读取。
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
    await openBatchCreate(['尿常规（A02）'], '30')

    authState.changeOrganizationSilently(null)
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    armLoad(pendingLoad())
    click(within(modalElement('新增互认项目配置')).getByRole('button', { name: /确定新增/ }))
    await act(async () => {})

    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    // 读取只有进入页面与打开弹窗两次。
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
    await openBatchCreate(['尿常规（A02）'], '30')

    await act(async () => {
      authState.change({ organizationCode: null, token: null })
    })
    expect(screen.getByText('组织范围不可用')).toBeInTheDocument()

    // 宿主重新签发**同一组织**的凭证：页面已应用组织与可信组织相同，无需重新对齐，但会重载一次数据。
    const restored = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-A')
    // 读取次序：进入页面 1 次、打开弹窗 1 次、同一组织凭证恢复后的重载 1 次。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(restored, [row('c1', 'A01', '血常规')])

    /*
     * 恢复后重新提交：先清掉不可用之前已勾选的项目，只留本次要提交的一个。
     * 选择由页面持有，因此它在「组织范围不可用」期间不会随弹窗一起消失——这正是
     * 「提交失败保留选择」所依赖的同一条事实，本用例顺带固定它不会在组织恢复后被悄悄清空。
     */
    const reopened = await findModal('新增互认项目配置')
    click(batchItemCheckbox(reopened, '尿常规（A02）'))
    click(batchItemCheckbox(reopened, '便常规（A03）'))
    await change(within(reopened).getByLabelText('可互认时间（天）'), '30')
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    const reload = armLoad(pendingLoad())
    click(within(reopened).getByRole('button', { name: /确定新增/ }))

    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
    expect(api.createRecognitionProjectConfigurations.mock.calls[0][1]).toEqual({ standardProjectCodes: ['A03'], recognitionDurationDays: 30 })
    await settle(reload, [row('c1', 'A01', '血常规'), row('c3', 'A03', '便常规')])
    await expectModalClosed('新增互认项目配置')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C2/C3 行与第 5 节「已定实现边界」的「查询只提交组织编码」条
 * （读取全量配置后在本地算排除集合）。
 */
describe('C2 / C3 新增弹窗的可选集合', () => {
  it('只提供未配置的最底层标准项目，包含停用配置在内的已配置项目全部隐藏', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    // 页面本体不出现复选框：选择只发生在弹窗里（S2-D17）。
    expect(screen.queryAllByRole('checkbox')).toEqual([])

    click(addButton())
    const create = await findModal('新增互认项目配置')

    expect(await waitFor(() => batchItemLabels(create))).toEqual(['选择标准项目：便常规（A03）'])
    // 已配置的两个编码（含停用配置）既不作为可选项出现，也不作为文本出现在弹窗里。
    expect(within(create).queryByText('血常规')).not.toBeInTheDocument()
    expect(within(create).queryByText('尿常规')).not.toBeInTheDocument()
  })

  it('C4 的组件面：非法天数本地拦截且零写请求，弹窗与输入保留', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openBatchCreate(['尿常规（A02）'], '0')
    click(within(create).getByRole('button', { name: /确定新增/ }))

    await within(create).findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    expect(modalExists('新增互认项目配置')).toBe(true)
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('0')
    // 选择同样保留：用户改好天数就能直接提交，不必重新勾选。
    expect(batchSelectedCount(create)).toBe('已选 1 项')

    await change(within(create).getByLabelText('可互认时间（天）'), '1.5')
    // 输入改变后上一次的拒绝原因随之清除，提示不会停在已经不成立的结论上。
    expect(within(create).queryByText('可互认时间为 1 到 2147483647 之间的整数天数')).not.toBeInTheDocument()
    click(within(create).getByRole('button', { name: /确定新增/ }))
    await within(create).findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/testPlan.md` 的 C29 与 C30 行、
 * `Client/design.md`「页面与交互」的新增互认项目段（弹窗多选、统一可互认时间、整组勾选）。
 *
 * 判别方式：勾选范围按目录层级判定，因此用例跨类型、跨分类、跨分组布置目录；
 * 半选态取复选框的 `indeterminate` 属性（不是只看外观类名），全选态取 `checked`。
 */
describe('C29 弹窗多选批量新增', () => {
  /** 只留一个已配置项，使弹窗的可选集合为 A02、A03、B01、C01 四个编码。 */
  const renderMultiLevel = () => renderLoaded([row('c1', 'A01', '血常规', 1, { categoryName: '血液学', groupName: '血常规组' })], MULTI_LEVEL_CATALOG)

  it('确定后一次提交全部所选项目与统一天数，成功关闭弹窗并刷新，已创建项目不再可选', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])
    await applyKeywordFilter('A')

    const create = await openBatchCreate(['尿常规（A02）', '便常规（A03）'], '30')
    const reload = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /确定新增/ }))

    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
    // 提交的是编码集合与统一天数这一条批量请求：不提交组织编码、不提交内部标准项目 ID、也不带启用状态。
    expect(api.createRecognitionProjectConfigurations.mock.calls[0][1]).toEqual({
      standardProjectCodes: ['A02', 'A03'],
      recognitionDurationDays: 30,
    })

    // 读取次序：进入页面 1 次、搜索提交 1 次、打开弹窗 1 次、写入成功后的重载 1 次。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规'), row('c3', 'A03', '便常规')])

    await expectModalClosed('新增互认项目配置')
    // 保留当前组织与筛选条件。
    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('A')
    // 新创建的配置默认启用（提交载荷里没有状态字段，启用由服务端决定）。
    expect(within(rowElement('c2')).getByText('启用')).toBeInTheDocument()
    expect(within(rowElement('c3')).getByText('启用')).toBeInTheDocument()

    // 重开弹窗：三个编码都已配置，可选集合为空，刚创建的项目不再可选。
    click(addButton())
    const again = await findModal('新增互认项目配置')
    await within(again).findByText('没有可新增的标准项目')
    expect(batchItemLabels(again)).toEqual([])
  })

  it('点分组格整组勾选该分组的全部项目，再点一次取消', async () => {
    await renderMultiLevel()
    const create = await openBatchCreate([], '30')
    expect(batchSelectedCount(create)).toBe('已选 0 项')

    // 血常规组只剩 A02 可选（A01 已配置），整组勾选因此只勾上 A02。
    click(within(create).getByRole('button', { name: '整组勾选分组「血常规组」及其下级' }))
    expect(batchSelectedCount(create)).toBe('已选 1 项')
    expect(batchItemCheckbox(create, '尿常规（A02）')).toBeChecked()
    expect(batchLevelCheckbox(create, '整组勾选分组「血常规组」及其下级')).toBeChecked()

    // 再点一次同一格：整组取消，勾选范围回到 0。
    click(within(create).getByRole('button', { name: '整组勾选分组「血常规组」及其下级' }))
    expect(batchSelectedCount(create)).toBe('已选 0 项')
    expect(batchItemCheckbox(create, '尿常规（A02）')).not.toBeChecked()
  })

  it('点分类格与类型格按整类整型勾选，部分选中用半选态表达', async () => {
    await renderMultiLevel()
    const create = await openBatchCreate([], '30')
    const hematology = () => batchLevelCheckbox(create, '整组勾选分类「血液学」及其下级')
    const laboratory = () => batchLevelCheckbox(create, '整组勾选类型「检验」及其下级')
    const biochemistry = () => batchLevelCheckbox(create, '整组勾选分类「生化」及其下级')

    // 只勾一个项目：分类与类型进入半选态（各自还有未勾选的项目），而不是被当成全选。
    click(batchItemCheckbox(create, '尿常规（A02）'))
    expect(hematology()).toBePartiallyChecked()
    expect(laboratory()).toBePartiallyChecked()
    expect(biochemistry()).not.toBeChecked()

    // 点类型格：该类型下的全部可选项目（A02、A03、B01）一次勾上，类型格与两个分类都转成全选。
    click(within(create).getByRole('button', { name: '整组勾选类型「检验」及其下级' }))
    expect(batchSelectedCount(create)).toBe('已选 3 项')
    expect(laboratory()).toBeChecked()
    expect(hematology()).toBeChecked()
    expect(biochemistry()).toBeChecked()
    expect(batchItemCheckbox(create, '丙氨酸氨基转移酶（B01）')).toBeChecked()
    // 另一个项目类型不受影响。
    expect(batchItemCheckbox(create, '胸部正位片（C01）')).not.toBeChecked()

    // 点分类格：只影响该分类下的项目，类型格因此回到半选态。
    click(within(create).getByRole('button', { name: '整组勾选分类「生化」及其下级' }))
    expect(batchSelectedCount(create)).toBe('已选 2 项')
    expect(laboratory()).toBePartiallyChecked()
    expect(biochemistry()).not.toBeChecked()
    expect(hematology()).toBeChecked()
  })

  it('层级格的复选框与层级格本身承载同一个整组勾选范围', async () => {
    await renderMultiLevel()
    const create = await openBatchCreate([], '30')

    click(batchLevelCheckbox(create, '整组勾选分类「影像」及其下级'))
    expect(batchSelectedCount(create)).toBe('已选 1 项')
    expect(batchItemCheckbox(create, '胸部正位片（C01）')).toBeChecked()

    click(batchLevelCheckbox(create, '整组勾选分类「影像」及其下级'))
    expect(batchSelectedCount(create)).toBe('已选 0 项')
    expect(batchItemCheckbox(create, '胸部正位片（C01）')).not.toBeChecked()
  })

  it('未勾选任何项目时确定不发出写请求，并给出可见原因', async () => {
    await renderMultiLevel()
    const create = await openBatchCreate([], '30')

    click(within(create).getByRole('button', { name: /确定新增/ }))
    await within(create).findByText('请先勾选要新增的标准项目')
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    expect(modalExists('新增互认项目配置')).toBe(true)

    // 勾选后原因随之清除，可以直接提交。
    click(batchItemCheckbox(create, '尿常规（A02）'))
    expect(within(create).queryByText('请先勾选要新增的标准项目')).not.toBeInTheDocument()
  })

  it('关闭弹窗再打开时选择与天数都回到初始值', async () => {
    await renderMultiLevel()
    const create = await openBatchCreate(['尿常规（A02）', '丙氨酸氨基转移酶（B01）'], '45')
    expect(batchSelectedCount(create)).toBe('已选 2 项')

    click(within(create).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    click(addButton())
    const reopened = await findModal('新增互认项目配置')
    expect(batchSelectedCount(reopened)).toBe('已选 0 项')
    expect((within(reopened).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/testPlan.md` 的 C30 行与
 * 阶段根 `design.md` 的 S2-D19（整批原子、失败零写入、错误携带违规编码集合）。
 *
 * 边界：违规编码集合由后端在业务错误里给出，页面不解析错误内容；本层只固定
 * 「整批一次请求、失败不部分写入、弹窗与选择保留、列表不因失败刷新」这几条可观察事实。
 */
describe('C30 批量新增失败口径', () => {
  it('整批失败时只发出一次写请求、弹窗与选择保留、列表不因失败刷新', async () => {
    // 读取桩走默认成功值：写失败后若实现仍发起重载，读取次数会变成 2，本用例据此判红。
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])
    const create = await openBatchCreate(['尿常规（A02）', '便常规（A03）'], '30')

    // 写失败只由宿主统一展示，本层用例在用例内局部观测这笔拒绝，避免它逃逸到进程。
    const rejections: unknown[] = []
    const observe = (reason: unknown) => { rejections.push(reason) }
    process.on('unhandledRejection', observe)
    try {
      api.createRecognitionProjectConfigurations.mockRejectedValueOnce(new Error('标准项目目录不可用于新建互认项目配置：所属分组已停用：A02'))
      click(within(create).getByRole('button', { name: /确定新增/ }))

      await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
      await waitFor(() => expect(rejections).toHaveLength(1))

      // 整批一次请求：选中多个项目也只发一次，不存在部分写入的中间态。
      expect(api.createRecognitionProjectConfigurations.mock.calls[0][1]).toEqual({
        standardProjectCodes: ['A02', 'A03'],
        recognitionDurationDays: 30,
      })
      // 失败不刷新列表：停在进入页面与打开弹窗这两次读取上，失败本身不追加读取。
      expect(query()).toHaveBeenCalledTimes(1)
      // 弹窗与选择保留，用户可以直接重试。
      expect(modalExists('新增互认项目配置')).toBe(true)
      expect(batchSelectedCount(create)).toBe('已选 2 项')
      expect(batchItemCheckbox(create, '尿常规（A02）')).toBeChecked()
      expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
      // 失败不改判为「刷新失败」阻断态。
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    } finally {
      process.off('unhandledRejection', observe)
    }
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C6/C20 行与 `Client/design.md`「页面与交互」的可互认时间行内修改段
 * （同值也照常提交）。
 */
describe('C6 / C20 修改可互认时间', () => {
  it('行内编辑回填当前值，非法输入零写且保留输入，同值也照常提交', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const input = () => screen.getByLabelText('可互认时间（天）') as HTMLInputElement
    // 编辑取值以当前行为准：输入框初值就是行上的可互认时间。
    expect(input().value).toBe('30')

    await change(input(), '-5')
    click(screen.getByLabelText('保存可互认时间：血常规'))
    await screen.findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    // 非法输入被拒绝时输入保留，用户能对照提示自行修改。
    expect(input().value).toBe('-5')

    await change(input(), '30')
    const reload = armLoad(pendingLoad())
    click(screen.getByLabelText('保存可互认时间：血常规'))

    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(1))
    expect(api.updateRecognitionDuration.mock.calls[0][1]).toEqual({ id: 'c1', recognitionDurationDays: 30 })

    // 保存成功后编辑态收起，行上继续显示当前行取值。
    await settle(reload, [row('c1', 'A01', '血常规', 1)])
    await waitFor(() => expect(screen.queryByLabelText('可互认时间（天）')).toBeNull())
  })

  it('停用配置仍可修改时间，行上显示状态徽标与目录停用原因', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 2, { unavailableReason: '所属分组已停用' })])

    const dataRow = rowElement('c1')
    expect(within(dataRow).getByText('目录已停用')).toBeInTheDocument()
    expect(within(dataRow).getByText('所属分组已停用')).toBeInTheDocument()

    click(within(dataRow).getByLabelText('修改可互认时间：血常规'))
    // 编辑态不改变行上的状态徽标与目录停用原因：它们与列表同源，仍取自当前行。
    expect(within(dataRow).getByText('目录已停用')).toBeInTheDocument()
    expect(within(dataRow).getByText('所属分组已停用')).toBeInTheDocument()

    const reload = armLoad(pendingLoad())
    click(within(dataRow).getByLabelText('保存可互认时间：血常规'))
    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(1))
    expect(api.updateRecognitionDuration.mock.calls[0][1]).toEqual({ id: 'c1', recognitionDurationDays: 30 })
    await settle(reload, [row('c1', 'A01', '血常规', 2, { unavailableReason: '所属分组已停用' })])
  })

  /**
   * 设计条目：行内编辑只持行标识，行数据按当前可见列视图重取，不得停留在进入编辑那一刻的快照。
   * （旧用例名里的分组编号 B1-09 只在本文件内使用，无外部登记表。）
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」
   * （编辑取值与回填以当前行集为准）与 `Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」补充规则的同一条口径。
   *
   * 判别方式：编辑期间发起一次后台刷新，服务端交付的行取值与状态都变了；
   * 旧实现（编辑器持行快照）下输入框仍是进入编辑时的 30、行上徽标仍是「启用」，本条断言会失败。
   */
  it('编辑期间后台刷新交付新值时，编辑器取当前行取值而不是进入编辑时的快照', async () => {
    await renderLoaded([
      row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 30 }),
      row('c2', 'A02', '尿常规', 1),
    ])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    expect((screen.getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
    expect(within(rowElement('c1')).getByText('启用')).toBeInTheDocument()

    // 另一行的启停写入带来一次后台重载：血常规的编辑态仍在渲染，本次读取交付了新的取值与状态。
    await reloadByTogglingOtherRow('停用配置：尿常规（当前启用）', [
      row('c1', 'A01', '血常规', 2, { recognitionDurationDays: 60 }),
      row('c2', 'A02', '尿常规', 2),
    ])

    expect((screen.getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('60')
    expect(within(rowElement('c1')).getByText('停用')).toBeInTheDocument()
    expect(within(rowElement('c1')).queryByText('启用')).not.toBeInTheDocument()
  })

  /**
   * 目标行离开当前可见列视图后，行内编辑不再渲染，它也就不再承载「未保存内容」：组织变化不得因此
   * 多弹一次确认框（用户无法主动清除这种不实状态）。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 2 节「状态机」补充规则（「未保存内容」判定与实际渲染同口径）与
   * `Client/design.md`「页面与交互」（组织变化遇未保存时间修改要先确认放弃）。
   *
   * 判别方式：只按编辑态是否为非空判定未保存内容时，本条会停在「待切换确认框」，
   * 「组织 组织乙（ORG-B）」与重载计数断言都失败。
   */
  it('后台刷新使行内编辑的目标行消失后，组织变化不再出现未保存内容确认', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 1)])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    await screen.findByLabelText('可互认时间（天）')

    // 另一行的启停写入带来一次后台重载：本次交付里已经没有血常规这一行，
    // 编辑态因此不再渲染，它也就不再承载「未保存内容」。
    await reloadByTogglingOtherRow('停用配置：尿常规（当前启用）', [row('c2', 'A02', '尿常规', 2)])
    await waitFor(() => expect(screen.queryByLabelText('可互认时间（天）')).toBeNull())

    const target = armLoad(pendingLoad())
    await switchTrustedOrganization('ORG-B')

    // 没有未保存内容：不弹确认框，直接清理并加载目标组织。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-B' })
    expect(screen.queryByText('切换组织将放弃未保存内容')).not.toBeInTheDocument()
    // 组织编码随可信令牌立即更新；名称来自本次范围查询，读取返回前只展示编码（S2-D22）。
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-B', label: null })
    await settle(target, [row('c9', 'B01', '乙院项目')])
    await screen.findByText('乙院项目')
    expect(scopeLines()).toEqual({ name: '组织乙', code: 'ORG-B', label: null })
  })

  /**
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/design.md`「页面与交互」的可互认时间
   * 行内修改段（行集里找不到该行时不渲染编辑态）与筛选区段（生效筛选改变时清空编辑态，S2-D21）。
   *
   * 判别方式：筛选把目标行藏起来后编辑态必须消失（否则留下一个用户看不见、却仍算「未保存内容」的输入框），
   * 筛选恢复后也不得「复活」。若改变筛选时不清空编辑态，本条的中间两处断言都会失败。
   */
  it('筛选把编辑中的行藏起来时编辑态一并消失，恢复筛选后不复活', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 1)])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    await screen.findByLabelText('可互认时间（天）')

    await applyKeywordFilter('A02')
    // 目标行已被筛掉：编辑态随之消失，页面上不再有看不见的未保存内容。
    await waitFor(() => expect(screen.queryByLabelText('可互认时间（天）')).toBeNull())
    expect(screen.queryByLabelText('修改可互认时间：血常规')).toBeNull()
    expect(screen.getByLabelText('修改可互认时间：尿常规')).toBeInTheDocument()

    // 清空筛选让目标行重新可见：编辑态不得复活。
    await change(screen.getByLabelText('关键字筛选'), '')
    await screen.findByLabelText('修改可互认时间：血常规')
    expect(screen.queryByLabelText('可互认时间（天）')).toBeNull()
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
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
    submitSearch()
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
    submitSearch()
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

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfigurations.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /确定新增/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
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
    // 第三次读取是写入成功后的重载；第二次读取来自打开弹窗即重读（S2-D25）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await expectModalClosed('新增互认项目配置')
  })

  it('提交中按 ESC 不关闭弹窗', async () => {
    stubUnarmedLoads()
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfigurations.mockReturnValueOnce(write.promise)
    click(within(create).getByRole('button', { name: /确定新增/ }))
    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))

    await act(async () => {
      fireEvent.keyDown(window, { key: 'Escape' })
    })
    expect(modalExists('新增互认项目配置')).toBe(true)

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    // 第三次读取是写入成功后的重载；第二次读取来自打开弹窗即重读（S2-D25）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])
    await expectModalClosed('新增互认项目配置')
  })

  /**
   * 「关闭后重开回到默认值」的判别点在于选择与天数的持有位置：页面在关闭弹窗后仍挂载，
   * 选择与天数都是页面状态，因此**只有页面在打开弹窗时清空它们**，重开才会回到初始值；
   * 若把清空交给弹窗卸载（或依赖新实例），重开仍会带着上一次的选择与天数。
   *
   * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
   * 第 1 节「页面结构」与 `Client/design.md`「页面与交互」（新增成功后清空弹窗选择）。
   *
   * 变异验证：删掉页面打开弹窗时对选择与天数的清空，本条会停在「已选 2 项」与天数 45。
   */
  it('关闭再打开弹窗时，选择与统一天数回到初始值', async () => {
    // 本条断言的是弹窗选择与天数的复位，因此需要一个仍有可新增项目的装配：只配置 A01。
    await renderLoaded([row('c1', 'A01', '血常规')], MULTI_LEVEL_CATALOG)

    const first = await openBatchCreate(['尿常规（A02）', '丙氨酸氨基转移酶（B01）'], '45')
    // 关闭前先确认页面状态确实持有用户输入，否则「重开后为空」可能只是因为压根没填进去。
    expect(batchSelectedCount(first)).toBe('已选 2 项')
    expect((within(first).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('45')

    click(within(first).getByRole('button', { name: /取\s*消/ }))
    await expectModalClosed('新增互认项目配置')

    click(addButton())
    const second = await findModal('新增互认项目配置')
    expect(batchSelectedCount(second)).toBe('已选 0 项')
    expect(batchItemCheckbox(second, '尿常规（A02）')).not.toBeChecked()
    expect((within(second).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('')
  })

  it('关闭后再次进入编辑时，编辑器回填当前行取值而不是上次输入', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 30 })])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    const firstInput = screen.getByLabelText('可互认时间（天）') as HTMLInputElement
    expect(firstInput.value).toBe('30')

    await change(firstInput, '7')
    click(screen.getByLabelText('取消修改可互认时间：血常规'))
    await waitFor(() => expect(screen.queryByLabelText('可互认时间（天）')).toBeNull())
    // 取消只放弃本次编辑，不发出任何写请求。
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()

    click(screen.getByLabelText('修改可互认时间：血常规'))
    expect((screen.getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
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

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    const write = deferred<void>()
    api.createRecognitionProjectConfigurations.mockReturnValueOnce(write.promise)
    const save = within(create).getByRole('button', { name: /确定新增/ })

    await act(async () => {
      fireEvent.click(save)
      fireEvent.click(save)
      fireEvent.click(save)
    })

    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
    expect(save).toHaveClass('ant-btn-loading')
    expect(modalExists('新增互认项目配置')).toBe(true)

    const reload = armLoad(pendingLoad())
    await act(async () => {
      write.resolve()
    })
    // 第三次读取是写入成功后的重载；第二次读取来自打开弹窗即重读（S2-D25）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1)

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
 * 用例名只描述行为：搜索提交会重新读取，裁剪建立在这一次读取返回的数据上，条件为空时提交等价于刷新。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 1 节「页面结构」的筛选区行与第 5 节「已定实现边界」的筛选区交互行（S2-D21、S2-D24）。
 */
describe('C31 搜索重新读取并在新结果上裁剪', () => {
  it('输入过程不改变列表；提交时重新读取，读取完成后才按输入原文裁剪；条件为空时提交等价于刷新', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    const codeFilter = () => screen.getByLabelText('关键字筛选') as HTMLInputElement
    const summary = () => screen.getByText(/^共 \d+ 项配置$/).textContent?.trim()

    await change(codeFilter(), 'A01')
    // 输入原文只是草稿：两行都还在，页脚不出现筛选后计数，也没有因此发起读取。
    expect(codeFilter().value).toBe('A01')
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(summary()).toBe('共 2 项配置')
    expect(query()).toHaveBeenCalledTimes(1)

    // 提交即重新读取：读取在途时搜索按钮进入载入态，且旧行集不被提前裁掉。
    const load = armLoad(pendingLoad())
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(screen.getByRole('button', { name: '搜索' })).toHaveClass('ant-btn-loading')
    expect(screen.getByText('尿常规')).toBeInTheDocument()

    // 读取返回后才裁剪：交付的就是服务端按关键字过滤后的行集（S2-D29），本地不再二次裁剪。
    await settle(load, [row('c1', 'A01', '血常规', 1)])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.queryByText('尿常规')).not.toBeInTheDocument()
    expect(summary()).toBe('共 1 项配置')

    // 回车与点击走同一条提交路径。
    await change(codeFilter(), 'A02')
    const byEnter = armLoad(pendingLoad())
    act(() => {
      fireEvent.keyDown(codeFilter(), { key: 'Enter' })
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(byEnter, [row('c2', 'A02', '尿常规', 2)])
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()

    // 结果来自本次读取而不是旧行集：换一批数据后列表跟着换（关键字命中的是名称列，四列口径）。
    await change(codeFilter(), 'A01')
    const replaced = armLoad(pendingLoad())
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(4))
    await settle(replaced, [row('c5', 'A05', 'A01命中名称', 1)])
    expect(screen.getByText('A01命中名称')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(summary()).toBe('共 1 项配置')

    // 清空输入自带的清除入口：清空即触发一次重读——关键字已下推服务端，只清状态不重读会停在旧结果上。
    const cleared = armLoad(pendingLoad())
    await change(codeFilter(), '')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(5))
    await settle(cleared, [row('c1', 'A01', '血常规', 1), row('c3', 'A03', '便常规', 1)])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('便常规')).toBeInTheDocument()
    expect(summary()).toBe('共 2 项配置')

    // 条件为空时提交同样等价于刷新：再读一次，结果是本组织当前全部配置。
    const refreshed = armLoad(pendingLoad())
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(6))
    await settle(refreshed, [row('c1', 'A01', '血常规', 1), row('c3', 'A03', '便常规', 1)])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('便常规')).toBeInTheDocument()
    expect(summary()).toBe('共 2 项配置')

    // 查询请求只提交组织编码与关键字：配置状态不下推服务端，始终只在本地裁剪。
    for (const call of query().mock.calls) {
      const body = call[1] as Record<string, unknown>
      expect(Object.keys(body).every((key) => key === 'organizationCode' || key === 'keyword')).toBe(true)
      expect(body).not.toHaveProperty('configurationStatus')
      expect(body.organizationCode).toBe('ORG-A')
    }
  })
})

/**
 * 用例名只描述行为：筛选区只有编码输入框、搜索按钮与配置状态选择；页头只有主按钮；
 * 清除编码走输入框自带的清除入口，配置状态切换不触发读取。
 *
 * 文档锚点：同上的筛选区行、标题区行与已定实现边界（S2-D21、S2-D23、S2-D24）。
 */
describe('C32 筛选区控件构成、清除动作与读写在途门控', () => {
  it('无重置与独立刷新控件、页头只有主按钮；清除只清编码；配置状态切换不触发读取', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    const codeFilter = () => screen.getByLabelText('关键字筛选') as HTMLInputElement
    // Segmented 把外部 aria-label 透传到 role=radiogroup（已核对 @rc-component/segmented@1.3 的
    // `_extends({ ...默认值 }, divProps)` 顺序与宿主可访问性树），故按可访问名称定位并顺带守卫该名称。
    const statusFilter = () => screen.getByRole('radiogroup', { name: '配置状态筛选' })
    const selectedStatus = () =>
      [...statusFilter().querySelectorAll('.ant-segmented-item-selected')].map((node) => node.textContent?.trim())
    const summary = () => screen.getByText(/^共 \d+ 项配置$/).textContent?.trim()
    // 清除入口由输入框的 allowClear 提供，渲染出来的是 ant-input-clear-icon 按钮。
    const clearIcon = () => {
      const found = document.querySelector<HTMLButtonElement>('.ant-input-clear-icon')
      if (!found) throw new Error('编码输入框没有清除入口')
      return found
    }

    // 控件构成：筛选区是编码输入框、搜索按钮与配置状态三件；页头只有主按钮，没有纯图标刷新。
    expect(screen.getByRole('button', { name: '搜索' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /重置/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /重新读取/ })).not.toBeInTheDocument()
    const headerButtons = document.querySelectorAll('.recognition-projects-header button')
    expect([...headerButtons].map((node) => node.textContent?.trim())).toEqual(['新增互认项目'])

    await applyKeywordFilter('A01')
    click(within(statusFilter()).getByText('停用'))
    // 编码条件与状态条件同时生效：A01 是启用配置，被状态条件排除后为空。
    expect(codeFilter().value).toBe('A01')
    expect(selectedStatus()).toEqual(['停用'])
    expect(summary()).toBe('共 0 项配置')
    // 搜索提交读取了一次；状态切换只在本地裁剪，不新增读取。
    expect(query()).toHaveBeenCalledTimes(2)

    // 清除动作清编码：输入原文与生效关键字一起清掉，状态条件保持不变；
    // 关键字已下推服务端，清空同时重读一次才能让列表回到全量（等价于空条件提交）。
    click(clearIcon())
    expect(codeFilter().value).toBe('')
    expect(selectedStatus()).toEqual(['停用'])
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settleUnarmedReload()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(summary()).toBe('共 1 项配置')

    // 状态切回全部：只在本地裁剪，不新增读取。
    click(within(statusFilter()).getByText('全部'))
    expect(selectedStatus()).toEqual(['全部'])
    expect(summary()).toBe('共 2 项配置')

    // 读取只在「搜索提交」与「清空关键字」时发生；状态切换与二次清空都不触发读取，全过程零写请求。
    expect(query()).toHaveBeenCalledTimes(3)
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
  })

  it('读取在途期间列表内的行内操作与新增入口一并禁用，旧数据只能查看', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    const load = armLoad(pendingLoad())
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 旧数据仍在，但读写在途：行内入口与新增入口都不可用，不会出现「看着旧数据还能改」。
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeDisabled()
    expect(addButton()).toBeDisabled()

    await settle(load, [row('c1', 'A01', '血常规', 1)])
    expect(screen.getByLabelText('修改可互认时间：血常规')).toBeEnabled()
    expect(addButton()).toBeEnabled()
  })
})

/**
 * 用例名只描述行为：页头组织为上下两行且不设文字标签，页头只保留一个主按钮。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 1 节「页面结构」的标题区行与第 5 节「已定实现边界」的页头组织范围行（S2-D22、S2-D23）。
 */
describe('C33 页头组织展示与动作区', () => {
  it('组织按上下两行展示且无文字标签，名称与编码同一来源，名称缺失时只展示编码一行', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    expect(scopeLines()).toEqual({ name: '组织甲', code: 'ORG-A', label: null })
    // 两行是纵向排列：名称元素在编码元素之前，容器内不存在标签元素。
    const scope = document.querySelector('.recognition-projects-scope') as HTMLElement
    expect([...scope.children].map((node) => node.className))
      .toEqual(['recognition-projects-scope-name', 'recognition-projects-scope-code'])
    // 范围查询不提交任何入参：组织只取自可信上下文。
    expect(api.queryTrustedOrganizationScope).toHaveBeenCalledTimes(1)
    expect(api.queryTrustedOrganizationScope.mock.calls[0][1]).toBeUndefined()

    // 名称缺失（服务端未交付）时只展示编码一行，不留空行也不渲染空括号。
    const reload = armLoad(pendingLoad())
    submitSearch()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await act(async () => {
      reload.configurations.resolve([row('c1', 'A01', '血常规')])
      reload.items.resolve(CATALOG)
      reload.scope.resolve({ organizationCode: 'ORG-A', organizationName: null })
    })
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-A', label: null })
    expect([...scope.children].map((node) => node.className)).toEqual(['recognition-projects-scope-code'])
  })

  it('页头只有「新增互认项目」一个动作，点击即打开新增弹窗', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const headerButtons = document.querySelectorAll('.recognition-projects-header button')
    expect([...headerButtons].map((node) => node.textContent?.trim())).toEqual(['新增互认项目'])

    click(addButton())
    await findModal('新增互认项目配置')
  })

  it('范围查询失败并入读取失败阻断态，重试成功后页头恢复名称', async () => {
    const load = armLoad(pendingLoad())
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await failLoad(load)

    // 三次读取同口径：任一次失败即整页不可写、保留已有数据并给出重试入口。
    expect(screen.getByText('互认配置数据待刷新')).toBeInTheDocument()
    expect(addButton()).toBeDisabled()
    expect(scopeLines()).toEqual({ name: null, code: 'ORG-A', label: null })

    const retryLoad = armLoad(pendingLoad())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(retryLoad, [row('c1', 'A01', '血常规')])

    expect(screen.queryByText('互认配置数据待刷新')).not.toBeInTheDocument()
    expect(scopeLines()).toEqual({ name: '组织甲', code: 'ORG-A', label: null })
    expect(addButton()).toBeEnabled()
  })

  it('范围查询回读的组织与本次读取的目标组织不一致时不接受该名称', async () => {
    const load = armLoad(pendingLoad())
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await act(async () => {
      load.configurations.resolve([row('c1', 'A01', '血常规')])
      load.items.resolve(CATALOG)
      // 凭证已换：范围查询回读的是另一个组织。
      load.scope.resolve(scopeFor('ORG-B'))
    })

    expect(scopeLines()).toEqual({ name: null, code: 'ORG-A', label: null })
  })
})

/**
 * 用例名只描述行为：弹窗打开即重读，弹窗内可手动重读，重读后失效的勾选被剔除并说明。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 1 节「页面结构」的新增弹窗行与第 5 节「已定实现边界」的弹窗就地重读行（S2-D25）。
 */
describe('C34 新增弹窗就地重读与失效勾选剔除', () => {
  it('打开弹窗即查询可新增项目，可选集合按本次查询计算，读取在途时确定按钮冻结', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const selectable = armSelectableLoad(pendingSelectableLoad())
    click(addButton())
    const create = await findModal('新增互认项目配置')
    // 打开弹窗触发一次可新增项目查询；页面配置列表不因此重读（S2-D29 之后两条读取相互独立）。
    await waitFor(() => expect(api.querySelectableStandardItems).toHaveBeenCalledTimes(1))
    expect(query()).toHaveBeenCalledTimes(1)
    expect(within(create).getByRole('button', { name: /确定新增/ })).toBeDisabled()

    // 本次查询交付的可新增集合决定弹窗内容：A01、A02 已配置，因此只剩 A03 可选。
    await settleSelectable(selectable, selectableCatalog(CATALOG, [row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 1)]))
    await waitFor(() => expect(batchItemLabels(create)).toEqual(['选择标准项目：便常规（A03）']))
    expect(within(create).getByRole('button', { name: /重新读取/ })).toBeInTheDocument()
    expect(within(create).getByRole('button', { name: /确定新增/ })).toBeEnabled()
  })

  it('重读后已不在可选集合中的勾选被剔除并给出条数说明，不静默剔除', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openBatchCreate(['尿常规（A02）', '便常规（A03）'], '30')
    expect(batchSelectedCount(create)).toBe('已选 2 项')

    // 期间其他页面把 A02 配置了：弹窗内手动重读后 A02 不再在可选集合中。
    const selectable = armSelectableLoad(pendingSelectableLoad())
    click(within(create).getByRole('button', { name: /重新读取/ }))
    await waitFor(() => expect(api.querySelectableStandardItems).toHaveBeenCalledTimes(2))
    await settleSelectable(selectable, selectableCatalog(CATALOG, [row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 1)]))

    await waitFor(() => expect(batchSelectedCount(create)).toBe('已选 1 项'))
    expect(within(create).getByText(/有 1 个已选项目已不在当前可新增集合中/)).toBeInTheDocument()

    // 剔除只影响选择，弹窗与天数输入都保留，用户可以直接重试提交。
    expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
    expect(within(create).queryByText('没有可新增的标准项目')).not.toBeInTheDocument()
  })
})

/**
 * 用例名只描述行为：弹窗是本页唯一的选择面，因此它自带发现能力——关键字搜索与全选当前结果。
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 1 节「页面结构」的新增弹窗行、第 5 节「已定实现边界」的新增弹窗条与 `Client/design.md`「页面与交互」
 * 的弹窗段（S2-D26、S2-D29）。
 *
 * 判别方式：搜索断在「只发起一次可新增项目查询、不重读配置列表」上——若把搜索实现成一次配置列表查询，
 * 配置列表读取次数断言失败；命中区断在"点名称即可勾选"上，旧实现只把复选框本身作为命中区。
 */
describe('C35 弹窗发现能力：关键字搜索与全选当前结果', () => {
  it('关键字搜索按服务端查询可新增项目，不重读配置列表；清空后恢复全部可新增项目', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')], MULTI_LEVEL_CATALOG)

    const create = await openBatchCreate([], '30')
    expect(batchItemLabels(create)).toEqual([
      '选择标准项目：尿常规（A02）',
      '选择标准项目：凝血酶原时间（A03）',
      '选择标准项目：丙氨酸氨基转移酶（B01）',
      '选择标准项目：胸部正位片（C01）',
    ])
    const configReadsBeforeSearch = query().mock.calls.length
    const selectableReadsBeforeSearch = api.querySelectableStandardItems.mock.calls.length

    await change(within(create).getByLabelText('弹窗关键字搜索'), 'B01')
    // 输入原文只是草稿：未提交前列表不变。
    expect(batchItemLabels(create)).toHaveLength(4)

    click(within(create).getByRole('button', { name: '搜索' }))
    await waitFor(() => expect(batchItemLabels(create)).toEqual(['选择标准项目：丙氨酸氨基转移酶（B01）']))
    // 搜索只查一次可新增项目，不重读配置列表。
    expect(api.querySelectableStandardItems.mock.calls.length).toBe(selectableReadsBeforeSearch + 1)
    expect(query().mock.calls.length).toBe(configReadsBeforeSearch)

    // 搜索无命中时给的是"没有匹配的标准项目"，不是"没有可新增项"——两者事实不同。
    await change(within(create).getByLabelText('弹窗关键字搜索'), 'ZZ')
    click(within(create).getByRole('button', { name: '搜索' }))
    await screen.findByText('没有匹配的标准项目')

    // 清空输入原文同时清掉生效条件，可选集合恢复。
    await change(within(create).getByLabelText('弹窗关键字搜索'), '')
    await waitFor(() => expect(batchItemLabels(create)).toHaveLength(4))
    expect(query().mock.calls.length).toBe(configReadsBeforeSearch)
  })

  it('全选当前结果一次勾选当前搜索结果，已有选择不受影响，再点一次不取消', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')], MULTI_LEVEL_CATALOG)

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    expect(batchSelectedCount(create)).toBe('已选 1 项')

    // 「全选当前结果」按当前搜索结果补齐勾选，不清除已有选择。
    expect(batchItemLabels(create)).toHaveLength(4)
    expect(within(create).getByRole('button', { name: '全选当前结果' })).toBeEnabled()
    click(within(create).getByRole('button', { name: '全选当前结果' }))
    expect(batchItemCheckbox(create, '凝血酶原时间（A03）')).toBeChecked()
    expect(batchItemCheckbox(create, '胸部正位片（C01）')).toBeChecked()
    await waitFor(() => expect(batchSelectedCount(create)).toBe('已选 4 项'))

    // 已被搜索裁掉的项目不在"当前结果"里：全选只补齐当前可见的项目，不清除此前已选。
    await change(within(create).getByLabelText('弹窗关键字搜索'), 'C01')
    click(within(create).getByRole('button', { name: '搜索' }))
    await waitFor(() => expect(batchItemLabels(create)).toEqual(['选择标准项目：胸部正位片（C01）']))
    click(within(create).getByRole('button', { name: '全选当前结果' }))
    await waitFor(() => expect(batchSelectedCount(create)).toBe('已选 4 项'))
    // 清空搜索后可以看到：此前勾上的项目仍在选择里。
    await change(within(create).getByLabelText('弹窗关键字搜索'), '')
    await waitFor(() => expect(batchItemCheckbox(create, '尿常规（A02）')).toBeChecked())

    // 全选是补齐动作而不是开关：再点一次不改变已选数量。
    click(within(create).getByRole('button', { name: '全选当前结果' }))
    await act(async () => {})
    expect(batchSelectedCount(create)).toBe('已选 4 项')
  })

  it('项目行整行都是勾选命中区：点名称与点复选框等价', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')], MULTI_LEVEL_CATALOG)

    const create = await openBatchCreate([], '30')
    const checkbox = batchItemCheckbox(create, '尿常规（A02）')
    expect(checkbox).not.toBeChecked()

    // 点行内的名称文本即可勾选：旧实现只把复选框本身作为命中区，这里不会勾上。
    click(within(create).getByText('尿常规'))
    await waitFor(() => expect(batchItemCheckbox(create, '尿常规（A02）')).toBeChecked())
    expect(batchSelectedCount(create)).toBe('已选 1 项')

    // 再点名称取消勾选。
    click(within(create).getByText('尿常规'))
    await waitFor(() => expect(batchItemCheckbox(create, '尿常规（A02）')).not.toBeChecked())
    expect(batchSelectedCount(create)).toBe('已选 0 项')
  })

  it('异步提示与常驻读屏区域：剔除说明与本地拦截都在 aria-live 容器内', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')], MULTI_LEVEL_CATALOG)

    const create = await openBatchCreate([], '30')
    // 读屏区域先于内容存在：容器在未提交时就已在版面上。
    expect(create.querySelectorAll('[aria-live="polite"]').length).toBeGreaterThan(0)

    // 未勾选即确定：本地拦截，不发写请求。
    click(within(create).getByRole('button', { name: /确定新增/ }))
    await screen.findByText('请先勾选要新增的标准项目')
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    expect(create.querySelector('[aria-live="polite"]')?.textContent).toContain('请先勾选要新增的标准项目')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 3 节「C 矩阵对应」的 C21 行与第 2 节「状态机」的「刷新失败（写入成功后）」行（不改判为写失败）。
 */
describe('C21 写成功但刷新失败', () => {
  it('新增写入成功后刷新失败不改判为写失败，页面进入阻断态并可重试', async () => {
    await renderLoaded([row('c1', 'A01', '血常规')])

    const create = await openBatchCreate(['尿常规（A02）'], '30')
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    const failing = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /确定新增/ }))

    await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
    // 第二次读取是写入成功后的重载（本次读取失败）；打开弹窗只查可新增项目，不进配置列表读取。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failLoad(failing)

    await screen.findByText('写入已成功，数据待刷新')
    await expectModalClosed('新增互认项目配置')
    expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1)
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

      const create = await openBatchCreate(['尿常规（A02）'], '30')
      const rejection = new Error('写入失败')
      api.createRecognitionProjectConfigurations.mockRejectedValueOnce(rejection)
      click(within(create).getByRole('button', { name: /确定新增/ }))

      await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      expect(modalExists('新增互认项目配置')).toBe(true)
      expect((within(create).getByLabelText('可互认时间（天）') as HTMLInputElement).value).toBe('30')
      // 第二次读取来自打开弹窗即重读（S2-D25）；写失败不产生重载。
      expect(query()).toHaveBeenCalledTimes(1)
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('互认配置数据待刷新')).not.toBeInTheDocument()

      // 失败后提交中状态必须归零，与成功路径的 loading 断言同一口径（写法见 C19 成功用例）。
      expect(within(create).getByRole('button', { name: /确定新增/ })).not.toHaveClass('ant-btn-loading')
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

      const create = await openBatchCreate(['尿常规（A02）'], '30')
      const write = deferred<void>()
      api.createRecognitionProjectConfigurations.mockReturnValueOnce(write.promise)
      const save = within(create).getByRole('button', { name: /确定新增/ })
      click(save)

      await waitFor(() => expect(api.createRecognitionProjectConfigurations).toHaveBeenCalledTimes(1))
      expect(modalExists('新增互认项目配置')).toBe(true)

      // 设计条目：写请求在途时全部关闭入口不可用，用户不能把一次尚未返回的写入从界面上抹掉
      // （旧用例内编号 B6 只在本文件内使用，无外部登记表）。
      // 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
      // 第 2 节「状态机」的「写操作中」行（全部禁用；写失败保留弹窗与输入）。
      expect(within(create).getByRole('button', { name: /取\s*消/ })).toBeDisabled()
      expect(create.querySelector('.ant-modal-close')).toBeNull()
      expect(within(create).getByRole('button', { name: /确定新增/ })).toHaveClass('ant-btn-loading')
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
      // 第二次读取来自打开弹窗即重读（S2-D25）；写失败不产生重载。
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

    await applyKeywordFilter('A')
    const create = await openBatchCreate(['尿常规（A02）'], '30')
    api.createRecognitionProjectConfigurations.mockResolvedValueOnce(undefined)
    const reload = armLoad(pendingLoad())
    click(within(create).getByRole('button', { name: /确定新增/ }))

    // 第四次读取是写入成功后的重载（第二次是搜索提交，第三次是打开弹窗即重读）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')])

    await screen.findByText('尿常规')
    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('A')
    await expectModalClosed('新增互认项目配置')

    // 刷新后的排除集合必须包含刚新增的配置：重开弹窗时它不再出现。
    click(addButton())
    const again = await findModal('新增互认项目配置')
    await waitFor(() => expect(batchItemLabels(again)).toEqual(['选择标准项目：便常规（A03）']))
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
  it('本组织还没有配置时显示本组织空态且计数为 0，目录为空同样报这一种空态', async () => {
    await renderLoaded([], null)

    // 页面行集是本组织已配置项（S2-D16）：行集为空只可能是本组织零配置，目录读不到任何项目也一样。
    expect(screen.getByText('本组织还没有互认项目配置')).toBeInTheDocument()
    expect(cascadeHeads()).toEqual([])
    expect(screen.getByText('共 0 项配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('目录非空但本组织没有配置时同样报本组织空态：目录里的未配置项目不进列表', async () => {
    await renderLoaded([])

    // 目录里有三个可展示项目，但本组织一条配置都没有：这些项目不构成列表行，页面报本组织空态。
    expect(screen.getAllByText('本组织还没有互认项目配置')).toHaveLength(1)
    expect(cascadeHeads()).toEqual([])
    expect(document.querySelectorAll('[data-row-key]')).toHaveLength(0)
    expect(screen.queryByText('未配置')).not.toBeInTheDocument()
    // 计数与列表行数同源。
    expect(screen.getByText('共 0 项配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('筛选后为空时区分空态文案，并同时给出本组织条数与筛选后条数', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    expect(screen.getByText('共 2 项配置')).toBeInTheDocument()

    await applyKeywordFilter('ZZ')

    // 本组织确实有配置，只是被本地筛掉：不得报成本组织空态。
    expect(screen.getByText('没有匹配的配置')).toBeInTheDocument()
    expect(screen.queryByText('本组织还没有互认项目配置')).not.toBeInTheDocument()
    // 列视图裁剪丢弃没有可见项目的层级，裁剪后连表头与网格都不渲染。
    expect(cascadeHeads()).toEqual([])
    expect(screen.getByText('共 0 项配置')).toBeInTheDocument()
  })

  it('状态筛选同样进入筛选后为空的计数口径', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)])

    click(within(screen.getByRole('radiogroup', { name: '配置状态筛选' })).getByText('停用'))

    // 该行是启用，状态筛到停用后为空。
    expect(screen.getByText('没有匹配的配置')).toBeInTheDocument()
    expect(screen.getByText('共 0 项配置')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
  })

  it('地址栏带条件进入时按该条件初始化筛选，列表与页脚都按生效条件计算', async () => {
    // 深链进入：编码与配置状态都来自查询参数，输入框与列表指向同一条件（S2-D27）。
    window.history.replaceState(null, '', `${window.location.pathname}?keyword=A02&status=2`)
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('A02')
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.getByText('共 1 项配置')).toBeInTheDocument()
  })
})

/**
 * 已生效筛选与地址栏的一致性（S2-D27）：地址栏只承载经提交生效的条件。
 */
describe('筛选与地址栏查询参数一致', () => {
  it('提交搜索与切换状态都回写地址栏，输入原文不写入，清空条件后参数不残留', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1), row('c2', 'A02', '尿常规', 2)])

    expect(window.location.search).toBe('')

    // 输入原文只是草稿：地址栏不因输入而改变。
    await change(screen.getByLabelText('关键字筛选'), 'A02')
    expect(window.location.search).toBe('')

    // 提交后写入生效条件。
    const load = armLoad(pendingLoad())
    submitSearch()
    await settle(load, currentRows, currentCatalog)
    expect(parseFilterSearch(window.location.search)).toEqual({ keyword: 'A02', status: 'all' })

    // 配置状态切换即生效，同样进入地址栏。
    click(within(screen.getByRole('radiogroup', { name: '配置状态筛选' })).getByText('停用'))
    expect(parseFilterSearch(window.location.search)).toEqual({ keyword: 'A02', status: 2 })

    // 清空编码条件后该参数不残留。
    await change(screen.getByLabelText('关键字筛选'), '')
    expect(parseFilterSearch(window.location.search)).toEqual({ keyword: '', status: 2 })
  })

  it('参数口径：缺失或不可识别的取值按该条件不生效，其他参数原样保留', async () => {
    expect(parseFilterSearch('')).toEqual({ keyword: '', status: 'all' })
    expect(parseFilterSearch('?keyword=0025&status=1')).toEqual({ keyword: '0025', status: 1 })
    expect(parseFilterSearch('?status=9')).toEqual({ keyword: '', status: 'all' })
    expect(buildFilterSearch('?keep=1', { keyword: 'A01', status: 'all' })).toBe('?keep=1&keyword=A01')
    expect(buildFilterSearch('?keyword=A01&keep=1', { keyword: '', status: 'all' })).toBe('?keep=1')
  })
})

/**
 * 用例名只描述行为：行内动作的门控与列表规模下的渲染。
 * （旧用例名里的分组编号 B3-06 / B3-11 只在本文件内使用，无外部登记表。）
 *
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/Pages/RecognitionProjects/RecognitionProjects.md`
 * 第 5 节「已定实现边界」的「不分页（S2-D7）」条与 `Client/design.md`「页面与交互」的项目行段
 * （停用配置仍可修改时间；配置状态未知时不给出启停动作）。
 */
describe('行内动作门控与列表规模', () => {
  it('多行结果全部渲染，读取只发起一次', async () => {
    const rows = Array.from({ length: 25 }, (_, index) => {
      const code = `A${String(index + 1).padStart(2, '0')}`
      return row(`c${index + 1}`, code, `项目${index + 1}`)
    })
    // 目录同时交付这 25 个编码：列视图才不会有额外的「未配置」行，分母因此确定为 25。
    await renderLoaded(rows, catalogOf(rows.map((value) => ({ code: value.standardProjectCode ?? '', name: value.standardItemName ?? '' }))))

    expect(screen.getAllByLabelText(/^修改可互认时间：项目/)).toHaveLength(25)
    expect(document.querySelectorAll('[data-row-key]')).toHaveLength(25)
    // 页面按设计不分页：一次读取交付全部行，也不因渲染而追加读取；
    // 可新增项目不在页面读取里（S2-D29），渲染不牵动它。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(api.querySelectableStandardItems).not.toHaveBeenCalled()
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

    // 编辑入口禁用，点它不进入编辑态，也不产生任何写请求。
    expect(screen.queryByLabelText('可互认时间（天）')).toBeNull()
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
  it('列表显示服务端契约交付的枚举中文，不被本地兜底文案覆盖', async () => {
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

  it('进入行内编辑后，状态徽标与类型列仍显示服务端契约文本', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 2, {
      itemTypeText: '服务端类型文案',
      configurationStatusText: '服务端停用文案',
    })])

    click(screen.getByLabelText('修改可互认时间：血常规'))
    await screen.findByLabelText('可互认时间（天）')

    // 编辑态不改变徽标与类型列的文案来源：两者与列表同源，仍取服务端交付的枚举中文。
    expect(within(rowElement('c1')).getByText('服务端停用文案')).toBeInTheDocument()
    expect(screen.getByText('服务端类型文案')).toBeInTheDocument()
    // 该行不许退回本地兜底文案：类型兜底「检验」与状态兜底「停用」都不得出现在这一行里
    // （筛选区在元数据不可用时用自己的兜底标签，与本行无关，因此断言限定在本行范围内）。
    expect(within(rowElement('c1')).queryByText('检验')).not.toBeInTheDocument()
    expect(within(rowElement('c1')).queryByText('停用')).not.toBeInTheDocument()
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

/**
 * C26-C28 使用的多层级目录：两种类型、三个分类、四个分组，共五个项目。
 * 层级规模由本夹具固定，因此层级格的跨行声明与四栏表头都有确定的期望值：
 * 检验名下三个分组行（网格第 2、3、4 行）、检查名下一个分组行（第 5 行）。
 */
const MULTI_LEVEL_CATALOG: EffectiveMedicalStandardCatalogReadModel = {
  itemTypes: [
    {
      itemType: 0,
      itemTypeText: '检验',
      categories: [
        {
          categoryName: '血液学',
          groups: [
            { groupName: '血常规组', items: [{ code: 'A01', name: '血常规' }, { code: 'A02', name: '尿常规' }] },
            { groupName: '凝血组', items: [{ code: 'A03', name: '凝血酶原时间' }] },
          ],
        },
        { categoryName: '生化', groups: [{ groupName: '肝功能', items: [{ code: 'B01', name: '丙氨酸氨基转移酶' }] }] },
      ],
    },
    {
      itemType: 1,
      itemTypeText: '检查',
      categories: [{ categoryName: '影像', groups: [{ groupName: '放射', items: [{ code: 'C01', name: '胸部正位片' }] }] }],
    },
  ],
}

/**
 * 多层级目录的全部项目各建一条配置行。S2-D16 之后页面行集只含本组织已配置项，
 * 未配置的目录项目不再产生层级格，因此断言"完整层级结构"的用例必须让整份目录都已成为本组织配置。
 */
function multiLevelConfiguredRows(): Row[] {
  const rows: Row[] = []
  // 生成端把层级数组声明为可空，这里按夹具的完整形状读取并给出空值兜底。
  for (const itemType of MULTI_LEVEL_CATALOG.itemTypes ?? []) {
    for (const category of itemType.categories ?? []) {
      for (const group of category.groups ?? []) {
        for (const item of group.items ?? []) {
          // 夹具只使用检验（0）与检查（1）两种项目类型，取值收窄后再构造行；
          // 类型中文取目录交付的那份，否则两栏类型格的可访问名称会同名而无法分别定位。
          const itemTypeValue: MedicalItemTypeValue | null =
            itemType.itemType === 0 || itemType.itemType === 1 ? itemType.itemType : null
          rows.push(row(`c-${item.code ?? ''}`, item.code ?? '', item.name ?? '', 1, {
            itemType: itemTypeValue,
            itemTypeText: itemType.itemTypeText ?? '',
            categoryName: category.categoryName ?? '',
            groupName: group.groupName ?? '',
          }))
        }
      }
    }
  }
  return rows
}

/** 层级格的可访问名称：名称同时表达层级与下级范围，是点击定位的唯一入口。 */
const ITEM_TYPE_CELLS = ['高亮类型「检验」及其下级', '高亮类型「检查」及其下级']
const CATEGORY_CELLS = ['高亮分类「血液学」及其下级', '高亮分类「生化」及其下级', '高亮分类「影像」及其下级']
const GROUP_CELLS = ['高亮分组「血常规组」及其下级', '高亮分组「凝血组」及其下级', '高亮分组「肝功能」及其下级', '高亮分组「放射」及其下级']

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/testPlan.md` 的 C26 行
 * （列视图全平铺）与 `Client/design.md`「页面与交互」的横向四栏段（S2-D16），
 * 以及 `docs/plans/004-阶段2-标准项目互认配置/design.md` 的 S2-D16（设计决策编号）。
 *
 * 判别方式：四栏表头、单一层格与跨行声明三处断言都以真实渲染结果为据。
 * 把类型格或分类格改成「每个分组行各渲染一格」时，同一层格按钮的计数会变成多个；
 * 把跨行声明改掉时，层级格与分组行会错位，`grid-row` 断言失败。
 * 窄视口横向滚动按外层滚动容器与内部网格的类名断言：jsdom 没有布局引擎，
 * 这里只能守住承载滚动的结构与最小宽度声明，真实滚动行为属宿主浏览器验收项。
 */
describe('C26 列视图全平铺', () => {
  it('类型｜分类｜分组｜项目四栏恒定全显，且不存在任何折叠或展开控件', async () => {
    await renderLoaded([row('c1', 'A01', '血常规'), row('c2', 'A02', '尿常规')], MULTI_LEVEL_CATALOG)

    expect(cascadeHeads().map((node) => node.textContent)).toEqual(['类型', '分类', '分组', '项目'])

    // 折叠或展开控件在可访问名称里必然表达「展开／折叠／收起」，同时会带 `aria-expanded`。
    const collapsible = screen.getAllByRole('button').filter((node) => {
      const name = node.getAttribute('aria-label') ?? node.textContent ?? ''
      return /展开|折叠|收起/.test(name)
    })
    expect(collapsible).toEqual([])
    expect(document.querySelectorAll('[aria-expanded]')).toHaveLength(0)
  })

  it('类型格与分类格各自只出现一处，并纵向跨过自己的全部分组行', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    // 同一类型／分类只有一处层格：多渲染一处就意味着版面变成逐分组重复层级。
    for (const name of ITEM_TYPE_CELLS) expect(screen.getAllByRole('button', { name })).toHaveLength(1)
    for (const name of CATEGORY_CELLS) expect(screen.getAllByRole('button', { name })).toHaveLength(1)

    // 跨行声明按夹具的层级规模给出：检验跨 3 个分组行、血液学跨 2 个、生化与影像各跨 1 个。
    expect(levelButton(ITEM_TYPE_CELLS[0]).style.gridRow).toBe('2 / span 3')
    expect(levelButton(ITEM_TYPE_CELLS[1]).style.gridRow).toBe('5 / span 1')
    expect(levelButton(CATEGORY_CELLS[0]).style.gridRow).toBe('2 / span 2')
    expect(levelButton(CATEGORY_CELLS[1]).style.gridRow).toBe('4 / span 1')
    expect(levelButton(CATEGORY_CELLS[2]).style.gridRow).toBe('5 / span 1')

    // 分组格一行一格、逐行推进：类型格与分类格的跨行高度因此与它们的下级严格同高。
    expect(GROUP_CELLS.map((name) => levelButton(name).style.gridRow)).toEqual(['2', '3', '4', '5'])
  })

  it('窄视口下四栏由外层容器承载横向滚动，列宽不被压缩', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    const wrap = document.querySelector('.recognition-projects-cascade-wrap')
    expect(wrap).not.toBeNull()
    // 网格必须在外层滚动容器内部：两者互换时窄视口就没有承载横向滚动的元素。
    expect(wrap?.querySelector('.recognition-projects-cascade')).not.toBeNull()

    // jsdom 没有布局引擎，横向滚动与列宽按静态样式证据断言（Testing Baseline §1.4）：
    // 外层容器承载横向滚动、网格自身有最小宽度，因此窄视口下四栏靠滚动容纳，不压缩列宽。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/recognitionProjects/RecognitionProjects.css'), 'utf8')
    const wrapRule = css.match(/\.recognition-projects-cascade-wrap\s*\{[^}]*\}/)?.[0] ?? ''
    const gridRule = css.match(/\.recognition-projects-cascade\s*\{[^}]*\}/)?.[0] ?? ''
    expect(wrapRule).toContain('overflow-x: auto')
    expect(gridRule).toContain('min-width:')
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/testPlan.md` 的 C27 行（层级高亮）与
 * `Client/design.md`「页面与交互」的层级格高亮段。
 *
 * 判别方式：每次点击后一次性比对**全部**层级格的高亮状态，断言因此不只看「该亮的是否亮」，
 * 还看「不该亮的是否没亮」；高亮判定若漏掉类型或分类这一级，或把同级的其它格一起点亮，本条失败。
 * 全程零写请求：高亮只标识位置，三个写出口在整个交互过程中一次都不得被调用。
 */
describe('C27 层级高亮', () => {
  const noHighlight = Object.fromEntries([
    ...ITEM_TYPE_CELLS, ...CATEGORY_CELLS, ...GROUP_CELLS,
  ].map((name) => [name, false]))

  /** 三个写出口在整条用例里都必须保持零调用。 */
  const expectNoWriteRequest = () => {
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()
  }

  it('点击类型格高亮该类型格与它的全部分类、分组格；点击项目行或页面空白清除', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    click(levelButton(ITEM_TYPE_CELLS[0]))
    expect(levelHighlightStates()).toEqual({
      ...noHighlight,
      [ITEM_TYPE_CELLS[0]]: true,
      [CATEGORY_CELLS[0]]: true,
      [CATEGORY_CELLS[1]]: true,
      [GROUP_CELLS[0]]: true,
      [GROUP_CELLS[1]]: true,
      [GROUP_CELLS[2]]: true,
    })

    // 点项目行：行外壳不拦截点击，清除高亮。
    click(rowElement('c-A01'))
    expect(levelHighlightStates()).toEqual(noHighlight)

    // 再点亮一次，然后点页面空白处：整页回到未高亮状态。
    click(levelButton(ITEM_TYPE_CELLS[0]))
    expect(isHighlighted(levelButton(ITEM_TYPE_CELLS[0]))).toBe(true)
    click(screen.getByText('标准项目互认配置'))
    expect(levelHighlightStates()).toEqual(noHighlight)

    expectNoWriteRequest()
  })

  it('点击分类格只高亮该分类格与它名下的分组格', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    click(levelButton(CATEGORY_CELLS[0]))
    expect(levelHighlightStates()).toEqual({
      ...noHighlight,
      [CATEGORY_CELLS[0]]: true,
      [GROUP_CELLS[0]]: true,
      [GROUP_CELLS[1]]: true,
    })

    // 换点同类型的另一个分类：上一个分类连同它的分组一起复原。
    click(levelButton(CATEGORY_CELLS[1]))
    expect(levelHighlightStates()).toEqual({
      ...noHighlight,
      [CATEGORY_CELLS[1]]: true,
      [GROUP_CELLS[2]]: true,
    })

    expectNoWriteRequest()
  })

  it('点击分组格只高亮该分组格', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    click(levelButton(GROUP_CELLS[1]))
    expect(levelHighlightStates()).toEqual({ ...noHighlight, [GROUP_CELLS[1]]: true })

    expectNoWriteRequest()
  })

  /**
   * 项目层随所属分组一起高亮：四级都要亮才是设计要求的「整条下级链路（类型→分类→分组→项目）」。
   * 判别方式：项目格与分组格一一对应，因此按 DOM 顺序比对整份项目格高亮状态；
   * 若实现只点亮层级格而漏掉项目格（真实宿主曾如此），本条失败。
   */
  it('项目格随所属分组格一起高亮，未点中的分组项目格保持不高亮', async () => {
    await renderLoaded(multiLevelConfiguredRows(), MULTI_LEVEL_CATALOG)

    // 初始全不高亮（四个分组各一个项目格，顺序与 GROUP_CELLS 一致）。
    expect(itemCellHighlightStates()).toEqual([false, false, false, false])

    // 点第一个分组格：只有它名下的项目格高亮，另外三个分组的项目格不受影响。
    click(levelButton(GROUP_CELLS[0]))
    expect(itemCellHighlightStates()).toEqual([true, false, false, false])

    // 点分类格：该分类下的两个分组项目格都高亮，另一个类型下的项目格不高亮。
    click(levelButton(CATEGORY_CELLS[0]))
    expect(itemCellHighlightStates()).toEqual([true, true, false, false])

    // 点整个类型格：该类型下全部分组项目格高亮，另一个类型的项目格不高亮。
    click(levelButton(ITEM_TYPE_CELLS[0]))
    expect(itemCellHighlightStates()).toEqual([true, true, true, false])

    // 点另一个类型格：高亮整条链路随之切换。
    click(levelButton(ITEM_TYPE_CELLS[1]))
    expect(itemCellHighlightStates()).toEqual([false, false, false, true])

    // 点项目行清除高亮：项目格连同层级格一起回到未高亮。
    click(rowElement('c-A01'))
    expect(itemCellHighlightStates()).toEqual([false, false, false, false])

    expectNoWriteRequest()
  })
})

/**
 * 文档锚点：`docs/plans/004-阶段2-标准项目互认配置/Client/testPlan.md` 的 C28 行
 * （状态与时间行内展示）与 `Client/design.md`「页面与交互」的可互认时间行内修改段。
 *
 * 判别方式：四种状态徽标按行定位断言，目录停用原因与徽标同源；行内编辑取**当前行**的取值，
 * 非法输入零写且保留输入，合法与同值保存都发出请求，保存成功后编辑态收起并按当前行集重取值。
 */
describe('C28 状态徽标与可互认时间行内展示', () => {
  /** 三条状态行：启用、停用与目录停用（配置仍在，但有效目录里读不到该编码）。 */
  const statusRows = () => [
    row('c1', 'A01', '血常规', 1),
    row('c2', 'A02', '尿常规', 2),
    row('c99', 'C99', '历史项目', 2, { unavailableReason: '所属分组已停用' }),
  ]

  it('启用、停用与目录停用三种状态徽标与目录停用原因各自正确，未配置行不出现', async () => {
    await renderLoaded(statusRows(), MULTI_LEVEL_CATALOG)

    expect(within(rowElement('c1')).getByText('启用')).toBeInTheDocument()
    expect(within(rowElement('c2')).getByText('停用')).toBeInTheDocument()
    // 目录读不到的配置行按行内字段补进列视图，徽标表达目录状态并带出停用原因。
    expect(within(rowElement('c99')).getByText('目录已停用')).toBeInTheDocument()
    expect(within(rowElement('c99')).getByText('所属分组已停用')).toBeInTheDocument()
    // 目录里有、本组织没有配置的项目不进列表（S2-D16），因此不存在「未配置」徽标。
    expect(screen.queryByText('未配置')).not.toBeInTheDocument()
    expect(document.querySelectorAll('[data-row-key]')).toHaveLength(3)

    await act(async () => {})
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
  })

  it('每一行都是已配置行：可互认时间都可就地修改，不存在纯文本占位行', async () => {
    await renderLoaded(statusRows(), MULTI_LEVEL_CATALOG)

    // 行集只含已配置项，因此每行都有可互认时间入口，没有「—」占位行。
    expect(screen.getAllByLabelText(/^修改可互认时间：/)).toHaveLength(3)
    expect(within(rowElement('c1')).getByLabelText('修改可互认时间：血常规')).toBeInTheDocument()
  })

  it('点击时间进入行内编辑：非法输入零写并保留输入，合法与同值保存都发出请求', async () => {
    await renderLoaded([row('c1', 'A01', '血常规', 1)], MULTI_LEVEL_CATALOG)

    const editEntry = () => within(rowElement('c1')).getByLabelText('修改可互认时间：血常规')
    // 编辑入口打开前先确认它带着当前行取值。
    expect(editEntry().textContent).toBe('30天')

    click(editEntry())
    const durationInput = () => within(rowElement('c1')).getByLabelText('可互认时间（天）') as HTMLInputElement
    expect(durationInput().value).toBe('30')

    // 非法输入：本地拦下，不发请求、保留输入并就地给出取值范围提示。
    await change(durationInput(), '0')
    click(within(rowElement('c1')).getByLabelText('保存可互认时间：血常规'))
    await screen.findByText('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(api.updateRecognitionDuration).not.toHaveBeenCalled()
    expect(durationInput().value).toBe('0')

    // 合法输入：发出请求，载荷是目标配置标识与本次输入；成功后收起编辑态并按当前行集重取值。
    await change(durationInput(), '45')
    const reload = armLoad(pendingLoad())
    click(within(rowElement('c1')).getByLabelText('保存可互认时间：血常规'))
    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(1))
    expect(api.updateRecognitionDuration.mock.calls[0][1]).toEqual({ id: 'c1', recognitionDurationDays: 45 })

    await settle(reload, [row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 45 })])
    await waitFor(() => expect(within(rowElement('c1')).queryByLabelText('可互认时间（天）')).toBeNull())
    expect(within(rowElement('c1')).getByLabelText('修改可互认时间：血常规').textContent).toBe('45天')

    // 同值保存同样发出请求（不做同值短路），载荷与当前行取值一致。
    click(within(rowElement('c1')).getByLabelText('修改可互认时间：血常规'))
    const again = armLoad(pendingLoad())
    click(within(rowElement('c1')).getByLabelText('保存可互认时间：血常规'))
    await waitFor(() => expect(api.updateRecognitionDuration).toHaveBeenCalledTimes(2))
    expect(api.updateRecognitionDuration.mock.calls[1][1]).toEqual({ id: 'c1', recognitionDurationDays: 45 })
    await settle(again, [row('c1', 'A01', '血常规', 1, { recognitionDurationDays: 45 })])
    expect(api.createRecognitionProjectConfigurations).not.toHaveBeenCalled()
    expect(api.setRecognitionProjectConfigurationEnabled).not.toHaveBeenCalled()
  })
})
