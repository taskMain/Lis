/**
 * 阶段 3「本院区互认项目金额」医院管理员页 Component 层用例
 * （测试矩阵 C3-C10、C12-C13、C15-C17、C21、C27-C36 的组件面，院区恒取登录院区的固定口径）。
 *
 * 装配边界（与平台管理员页用例同一口径）：
 * 1. `./recognitionAmountsApi` 只替换 4 个 I/O 出口，其余纯函数使用真实实现；行夹具由真实
 *    `toRecognitionAmountRows` 从读模型形态构造，表格行序由真实 `toRecognitionAmountColumnView` 决定；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client；
 * 3. 可信范围只来自宿主会话：这里用外部会话 store 替身构造多份身份上下文——组织+医院+院区齐全、
 *    可信组织或医院缺失（另含未认证）。会话通知后页面重新求值，因此**挂载之后**的可信范围变化可观察：
 *    身份后到、可信医院换绑、登录态失效都按真实路径触发页面重新求值。页面不再渲染范围选择器
 *    页面不渲染范围选择器，院区恒取会话院区，无三档解析与显式改选。
 *
 * 列表字段集合、未配置与零元的呈现差异、层级列视图、排序、空态、状态筛选本地裁剪、地址栏参数同步、
 * 页脚计数与读取在途写门控都与平台管理员页同一形态（S3-D29 起由两个金额页共用的列表视图组件承载）：
 * 查询区为关键字输入框（框内查询图标按钮）加金额状态下拉选择，互认配置状态为列表上沿页签，
 * 金额编辑器为「输入框＋图标保存＋图标取消」并在编辑器内就地提示校验原因；本文件按同一写法分别
 * 覆盖医院管理员页的实现（查询只携带院区与可选关键字，保存只携带院区、标准项目编码与金额）。
 * 提交前现值复核（C34，只保留可信组织或医院现读不到的拦截）与平台管理员页保持各自口径，
 * 断言语义按 Client/testPlan.md 逐条覆盖。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { toRecognitionAmountColumnView, toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
import { BranchRecognitionAmounts } from './BranchRecognitionAmounts'

/**
 * 宿主会话状态（组织、医院、院区与认证标记）；每条用例按需改成缺失上下文，或在挂载后变更。
 * `version` 是订阅快照：会话任何一次通知都推进它，让订阅方按新的可信范围重新求值。
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
  exportBranchRecognitionAmounts: vi.fn(),
}))

/** 共享下载模块在 Client/适配层边界替换：页面取得导出字节后经它触发保存。 */
const exportDownload = vi.hoisted(() => ({ saveExportWorkbook: vi.fn() }))

vi.mock('../../shared/exportFileDownload', async (importOriginal) => ({ ...(await importOriginal<typeof import('../../shared/exportFileDownload')>()), saveExportWorkbook: exportDownload.saveExportWorkbook }))

vi.mock('../../runtimeConfig', () => ({
  loadRuntimeConfig: async () => ({ apiBaseUrl: 'http://api.test' }),
}))

/**
 * 当前范围接缝替身：可信范围的业务编码与实例标识取同一组替身值
 * （本文件不区分两者，区分由 currentOrganizationScope 与 reportsTrustedScope 的接缝用例承担）。
 */
vi.mock('../../components/currentOrganizationScope', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../components/currentOrganizationScope')>()
  const scope = () => {
    const authenticated = authState.token !== null
    return {
      organizationId: authenticated ? authState.organizationCode : null,
      organizationName: null,
      organizationCode: authenticated ? authState.organizationCode : null,
      hospitalId: authenticated ? authState.hospitalCode : null,
      hospitalName: null,
      hospitalCode: authenticated ? authState.hospitalCode : null,
      branchId: authenticated ? authState.branchCode : null,
      branchName: null,
      branchCode: authenticated ? authState.branchCode : null,
    }
  }
  return {
    ...actual,
    readCurrentOrganizationScope: () => scope(),
    useResolvedTrustedScope: () => {
      const value = scope()
      return {
        status: value.organizationCode === null ? 'unavailable' as const : 'ready' as const,
        organizationId: value.organizationId,
        hospitalId: value.hospitalId,
        branchId: value.branchId,
        organizationCode: value.organizationCode,
        hospitalCode: value.hospitalCode,
        branchCode: value.branchCode,
      }
    },
  }
})

vi.mock('./recognitionAmountsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./recognitionAmountsApi')>()
  return { ...actual, ...api }
})

const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

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
  useApiClientContext: () => clientRef.current,
}))

/** 由当前替身状态构造宿主会话快照；未认证时三个范围字段都为 null。 */
function buildSession() {
  const authenticated = authState.token !== null
  return {
    status: authenticated ? 'authenticated' as const : 'anonymous' as const,
    sessionVersion: `v${authState.version}`,
    userId: authenticated ? 'user-1' : null,
    account: authenticated ? '191617' : null,
    displayName: authenticated ? '杨康健' : null,
    organizationId: authenticated ? authState.organizationCode : null,
    hospitalId: authenticated ? authState.hospitalCode : null,
    campusId: authenticated ? authState.branchCode : null,
  }
}

vi.mock('../../session/hostSession', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../session/hostSession')>()
  const { useEffect, useReducer } = await import('react')
  return {
    ...actual,
    readCurrentHostSession: () => buildSession(),
    useHostSessionSnapshot: () => {
      const [, forceRender] = useReducer((value: number) => value + 1, 0)
      useEffect(() => authState.subscribe(forceRender), [])
      return buildSession()
    },
  }
})

const amount = (value: number | null) => value

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
    currentAmount: amount(12.5),
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

/** 行内编辑器（C32）的金额输入：编辑态以可访问名称「金额」呈现，收起后即从文档消失。 */
const inlineAmountInput = () => screen.getByLabelText('金额') as HTMLInputElement
const queryInlineAmountInput = () => screen.queryByLabelText('金额')

/** 回车与 Esc 走异步 act：回车提交会启动写入与随后的整表重载，需要让微任务就地排空。 */
const pressKey = (element: HTMLElement, key: string) => act(async () => {
  fireEvent.keyDown(element, { key })
  // 真实键盘按键 keydown 与 keyup 成对出现；底层输入组件对 Enter 有键锁（keydown 置锁、keyup
  // 复位），只发 keyDown 会吞掉同一输入框里的第二次回车。
  fireEvent.keyUp(element, { key })
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

/** 框内查询图标按钮与关键字输入框：读取在途时图标按钮呈加载态，关键字只在提交时下推服务端。 */
const searchButton = () => screen.getByRole('button', { name: '查询' })
const keywordInput = () => screen.getByLabelText('关键字筛选') as HTMLInputElement

/**
 * 输入框内回车提交：antd Input 的 `onPressEnter` 由 keydown Enter 触发，与框内查询图标按钮的
 * 点击进入同一条 `submitKeyword` 提交链路。
 */
const submitByEnter = () => pressEnter(keywordInput())

/** 金额状态下拉选择把 aria-label 透传到 role=combobox；行内文字标签在 Select 外，不参与可访问名称。 */
const amountStatusFilter = () => screen.getByRole('combobox', { name: '金额状态筛选' })

/** 互认配置状态筛选为列表上沿页签（S3-D25 2026-10-13 修订），外层以 role=group 承载可访问名称。 */
const configurationStatusTabs = () => screen.getByRole('group', { name: '配置状态筛选' })

/** 当前配置状态筛选的选中项：Tabs 的选中页签带 .ant-tabs-tab-active。 */
const selectedStatusTabLabels = () =>
  [...configurationStatusTabs().querySelectorAll('.ant-tabs-tab-active')].map((node) => node.textContent?.trim())

/** 按名称点击一个配置状态页签：切换即生效、本地裁剪、不触发读取。 */
const clickConfigurationStatusTab = (name: string) => click(within(configurationStatusTabs()).getByRole('tab', { name }))

/** 下拉选择当前档位的展示文本（antd Select 的选中值格为 `.ant-select-content`）。 */
const selectedSelectValue = (combobox: HTMLElement) =>
  combobox.closest('.ant-select')?.querySelector('.ant-select-content')?.textContent?.trim()

/** 展开下拉并点击某个档位；档位只做本地裁剪，不触发读取。 */
const pickSelectOption = async (combobox: HTMLElement, option: string) => {
  fireEvent.mouseDown(combobox)
  // 归属判定排除收起中的残留下拉：测试环境 Select 的 id 都退化为同一占位值，aria-controls 不能
  // 区分归属；jsdom 不派发 transitionend，上一个下拉的 leave 动画类会残留，但正在展开的下拉
  // 不带 leave 标记，残留节点会因此被排除。
  const dropdown = await waitFor(() => {
    const open = [...document.querySelectorAll<HTMLElement>('.ant-select-dropdown')]
      .filter((node) => !node.classList.contains('ant-select-dropdown-hidden')
        && !node.className.includes('ant-slide-up-leave'))
    if (open.length !== 1) throw new Error('归属下拉未唯一展开')
    return open[0]
  })
  const item = [...dropdown.querySelectorAll<HTMLElement>('.ant-select-item-option')]
    .find((node) => node.textContent?.trim() === option)
  if (!item) throw new Error(`档位未找到：${option}`)
  click(item)
}

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
  exportDownload.saveExportWorkbook.mockReset()
  clientRef.current = {}
  // 地址栏回到缺省状态：上一个用例的已生效条件不得泄漏进本用例。
  window.history.replaceState(null, '', '/')
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

describe('C3 可信范围与院区固定（院区恒取登录院区）', () => {
  it('整页不渲染范围选择器：查询院区恒取登录院区，请求不含组织与医院（随登录上下文取数）', async () => {
    const pending = armQuery()
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 院区选择器移除，页面不渲染任何范围下拉，院区恒取登录院区。
    expect(screen.queryByLabelText('范围-组织')).toBeNull()
    expect(screen.queryByLabelText('范围-医院')).toBeNull()
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    // 统一形态下查询区唯一的可交互下拉是金额状态筛选（S3-D29）。
    expect(screen.getAllByRole('combobox')).toHaveLength(1)
    // 查询只按可信上下文里的登录院区构造，不提交组织与医院。
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })

    await settle(pending, [amountRow()])
    await screen.findByText('血常规')
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
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })

  it('C3 可信范围在首屏之后才就绪（token 后到）：按新的登录院区发起查询', async () => {
    // 挂载时凭据尚无 token：可信范围不可用，整页阻断、零业务请求、不渲染任何范围下拉。
    authState.token = null
    authState.organizationCode = null
    authState.hospitalCode = null
    authState.branchCode = null
    renderPage()
    await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    expect(screen.queryByLabelText('范围-院区')).toBeNull()
    expect(query()).not.toHaveBeenCalled()

    // token 后到，可信范围随后才可用：查询按登录院区发起。
    const pending = armQuery()
    await changeTrustedScope({ token: 'token-1', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })

    await settle(pending, [amountRow()])
    await screen.findByText('血常规')
  })

  it('C3 可信医院变更：院区恒随登录院区，不残留旧范围的行', async () => {
    await renderLoaded([amountRow()])
    expect(query().mock.calls[0][1]).toEqual({ branchCode: 'BRA-1' })

    // 可信医院换绑并携带新的登录院区：旧范围的行不得残留，整表按新范围重读。
    const next = armQuery()
    await changeTrustedScope({ hospitalCode: 'HOS-2', branchCode: 'BRA-9' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-9' })

    await settle(next, [amountRow({ standardProjectCode: 'B01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
  })

  it('C3 可信范围变为不可用：整页阻断，恢复后按新的登录院区取值', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await screen.findByText('血常规')

    // 登录态失效：可信组织与医院不可用，整页阻断。
    await changeTrustedScope({ token: null, organizationCode: null, hospitalCode: null, branchCode: null })

    await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    expect(screen.queryByRole('combobox')).toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)

    // 恢复登录态且登录院区换为另一个值：必须按新可信范围取值，不残留失效前的院区。
    const next = armQuery()
    await changeTrustedScope({ token: 'token-2', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-2' })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })
    await settle(next, [amountRow()])
    await screen.findByText('血常规')
  })
})

describe('C15 可信组织或医院缺失', () => {
  it('阻断提示、不渲染范围选择器、零业务请求，也不伪造组织或降级为空值', async () => {
    authState.organizationCode = null
    const missingOrganization = renderPage()
    const alert = await screen.findByRole('alert')
    expect(alert).toHaveClass('ant-alert-error')
    expect(alert.textContent).toBe('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    // 阻断态标题与正常态同一层级。
    expect(screen.getByRole('heading', { name: '本院区互认项目金额', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('combobox')).toBeNull()
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByRole('button', { name: /维护金额/ })).not.toBeInTheDocument()
    missingOrganization.unmount()

    authState.reset()
    authState.hospitalCode = '   '
    const missingHospital = renderPage()
    await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    expect(screen.queryByRole('combobox')).toBeNull()
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    missingHospital.unmount()

    authState.reset()
    authState.token = null
    renderPage()
    await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
  })
})

describe('C5 医院管理员页的列表字段集合', () => {
  it('与平台管理员页同源：列为类型、分类、分组、项目与三个业务列，不展示三级名称与编码等字段', async () => {
    await renderLoaded([amountRow()])

    // 金额状态列随 S3-D24（2026-10-12 修订）与两页统一（S3-D29）删除：当前金额列的「未配置」标记
    // 与数值已完整表达配置与否。
    expect(columnHeaders()).toEqual([
      '类型',
      '分类',
      '分组',
      '项目',
      '当前金额',
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
      // 金额状态列已删除（S3-D24 2026-10-12 修订），表头不再出现。
      '金额状态',
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
      amountRow({ standardProjectCode: 'A01', standardProjectName: '零元项目', currentAmount: amount(0), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    const [zeroRow, unconfiguredRow] = dataRows()

    // 零元：金额列是已配置的金额文本 0.00，行内不出现「未配置」；金额状态徽标列已删除，
    // 「已配置」不再以徽标出现在行内（S3-D24 2026-10-12 修订）。
    expect(within(zeroRow).getByText('0.00')).toBeInTheDocument()
    expect(within(zeroRow).queryByText('未配置')).toBeNull()
    expect(within(zeroRow).queryByText('已配置')).toBeNull()

    // 未配置：金额列一处「未配置」标记（行内编辑入口），整行不出现任何数值金额与「已配置」徽标。
    expect(within(unconfiguredRow).getAllByText('未配置')).toHaveLength(1)
    expect(within(unconfiguredRow).queryByText('0.00')).toBeNull()
    expect(within(unconfiguredRow).queryByText('已配置')).toBeNull()
    expect((unconfiguredRow.textContent ?? '').match(/\d+\.\d{2}/)).toBeNull()
  })
})

describe('C8 金额输入校验', () => {
  it('负数、三位小数、双精度无法精确表示的大额与空值都在编辑器内就地提示，零写请求且保留编辑态与输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    // 本循环的非法输入集合只取适配层出口 `parseRecognitionAmount`（页面不另写金额规则），其中
    // `99999999999999.99` 由该出口的「内存面 / 序列化面」等价性规则决定：该出口一旦放宽或收紧，
    // 本循环的输入集合必须同步（见 recognitionAmountsApi.test.ts 的金额解析用例）。
    for (const invalid of ['-1', '1.234', '99999999999999.99']) {
      await change(inlineAmountInput(), invalid)
      await pressEnter(inlineAmountInput())
      // 校验失败在编辑器内就地提示（S3-D27 2026-10-13 修订（第三轮），医院管理员页同步接入列内就地
      // 提示，原 message.error 口径作废）：提示渲染在当前行内。
      expect(await within(rowOf('血常规')).findByText('金额必须是不小于 0 且最多两位小数的数字')).toBeInTheDocument()
      // `invalid` 标记：校验在字段变更后重新求值，标记随之更新。
      await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
      expect(save()).not.toHaveBeenCalled()
      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe(invalid)
    }

    await change(inlineAmountInput(), '')
    await pressEnter(inlineAmountInput())
    expect(await within(rowOf('血常规')).findByText('金额必填')).toBeInTheDocument()
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('')
  })
})

describe('C9 / C12 金额保存成功', () => {
  it('回填当前值后不改直接提交仍发出写请求，成功后收起编辑态并整表重载', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    // 保存成功经 message.success 反馈。
    expect(await screen.findByText('金额保存成功')).toBeInTheDocument()
    // C4：保存请求同样只携带院区与标准项目编码、金额；载荷只经适配层出口构造。
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

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
      // 写失败与保存成功分流：保存成功的 message 反馈不得出现。
      expect(screen.queryByText('金额保存成功')).not.toBeInTheDocument()
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
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

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
    await settle(retry, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

    await screen.findByText('20.00')
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
  })
})

describe('C14 读取失败形态（纯读取失败不渲染横幅、错误由宿主统一提示）', () => {
  it('查询失败不渲染错误横幅：旧数据保留、写入仍禁用，点查询可重试成功', async () => {
    await renderLoaded([amountRow()])

    const failing = armQuery()
    // 空条件提交等价于刷新：查询主按钮重新读取当前范围。
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    await failQuery(failing)

    // 失败后的观测点改为加载态结束：读取失败不渲染任何错误横幅，
    // 错误由宿主统一提示，重试经查询按钮。
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()

    expect(screen.getByText('血常规')).toBeInTheDocument()
    // 失败后写入仍保持禁用（写门禁不依赖横幅），点击不进入编辑态。
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    // 重试经查询按钮：重读成功后恢复可写。
    const retry = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow()])

    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
  })
})

describe('C16 排序与不分页', () => {
  it('C16：服务端交付的层级升序在列视图中原样渲染，页面不分页', async () => {
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
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: amount(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: amount(20) }),
    ])

    await pickSelectOption(amountStatusFilter(), '未配置')

    expect(screen.getByText('没有符合当前筛选条件的互认项目金额')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

/**
 * 读取失败与零配置空态必须可区分：读取失败（例如令牌获取阶段就失败、请求根本没发出）不是
 * 「当前范围没有配置」，把失败渲染成零配置空态会让用户以为配置真的不存在。
 *
 * 失败态的重试沿用既有入口（框内查询图标按钮），不新增交互；本组用例锁定三件事：
 * a 失败且无数据 → 失败文案、无「没有…配置」、页脚不报共 0 项；b 成功零行 → 原零配置空态；
 * c 成功有行 → 页脚计数正确。
 */
describe('读取失败不得渲染成零配置空态', () => {
  it('读取失败且无数据：显示失败文案，不出现「没有…配置」，页脚不报共 0 项', async () => {
    const failing = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await failQuery(failing)

    await screen.findByText('读取失败，请重试')
    // 失败不得伪装成零配置空态：两种事实的文案互斥。
    expect(screen.queryByText('当前范围没有互认项目金额配置')).toBeNull()
    expect(screen.queryByText('没有符合当前筛选条件的互认项目金额')).toBeNull()
    expect(screen.queryByText(/没有.*配置/)).toBeNull()
    // 页脚在失败态不报 0：失败数据不可信，「共 0 项」会被读成零配置。
    expect(screen.queryByText(/共 0 项/)).toBeNull()
    expect(screen.getByText('共 — 项')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    // 失败态仍可重试：既有入口（框内查询图标按钮）保持可用，且不新增失败横幅。
    expect(searchButton()).toBeEnabled()
    expect(screen.queryByText(/待刷新|写入已成功/)).toBeNull()
  })

  it('读取成功但确实零行：仍渲染原零配置空态文案与共 0 项', async () => {
    await renderLoaded([])

    expect(screen.getByText('当前范围没有互认项目金额配置')).toBeInTheDocument()
    expect(screen.queryByText('读取失败，请重试')).toBeNull()
    expect(screen.getByText('共 0 项')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('读取成功有行：页脚按可见行数计数，不出现失败文案', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', isAmountConfigured: true, currentAmount: amount(12.5) }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '尿常规', isAmountConfigured: true, currentAmount: amount(20) }),
    ])

    expect(screen.getByText('共 2 项')).toBeInTheDocument()
    expect(screen.queryByText('读取失败，请重试')).toBeNull()
    expect(screen.queryByText(/共 — 项/)).toBeNull()
    expect(dataRows()).toHaveLength(2)
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C21 登录院区变化与未保存输入（院区恒取登录院区）', () => {
  it('登录院区变化时整表按新院区重读，未保存的金额输入随行集替换清空', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    const next = armQuery()
    await changeTrustedScope({ branchCode: 'BRA-2' })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-2' })

    // 未保存输入随编辑态收起被清空，旧院区数据不再展示，且没有任何写请求；
    // 不存在用户选定的滞留范围，不再有确认框或待切换提示态。
    expect(queryInlineAmountInput()).toBeNull()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'B01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')

    // 重新进入编辑时回填的是新范围当前值，切换前未提交的输入已清空。
    editAmount('新院区项目')
    expect(inlineAmountInput().value).toBe('12.50')
  })
})

/**
 * 用例名只描述行为：医院管理员页查询入口为关键字输入框内嵌的查询图标按钮（S3-D25 2026-10-13
 * 修订（第三轮），与平台管理员页同一形态）：`type="text"` + `SearchOutlined` +
 * `aria-label="查询"` + `autoInsertSpace={false}`；本页不再有独立查询主按钮与「重置」按钮。
 */
describe('查询图标按钮规格', () => {
  it('关键字输入框内嵌查询图标按钮：文本型按钮、搜索图标、可访问名称精确为「查询」，查询区无独立查询主按钮与重置按钮', async () => {
    await renderLoaded([amountRow()])

    const button = searchButton()
    expect(button).toHaveClass('ant-btn-text')
    expect(button).not.toHaveClass('ant-btn-primary')
    expect(button.querySelector('.anticon-search')).not.toBeNull()
    expect(button.getAttribute('aria-label')).toBe('查询')
    // 查询入口位于关键字输入框内（suffix 槽位），不是查询区的独立按钮。
    expect(keywordInput().closest('.ant-input-affix-wrapper')?.contains(button)).toBe(true)
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
  })
})

/**
 * 用例名只描述行为：医院管理员页查询区为「关键字输入框 + 金额状态下拉选择」的单行结构，
 * 互认配置状态筛选为贴在列表上沿的页签（S3-D25 2026-10-13 修订（第三轮），与平台管理员页同一
 * 形态）：不以 inline Form 承载，不渲染独立查询主按钮与「重置」按钮（原「重置」口径作废）；
 * 金额状态下拉默认档「全部」，配置状态页签默认选中「全部」。
 */
describe('查询区结构', () => {
  it('查询区为关键字框加金额状态下拉选择，配置状态为列表上沿页签，不以 Form 承载，不渲染独立查询主按钮与重置按钮', async () => {
    await renderLoaded([amountRow()])

    const filterRegion = document.querySelector('.recognition-amounts-filter') as HTMLElement
    expect(filterRegion).not.toBeNull()
    expect(filterRegion.querySelector('form')).toBeNull()
    // 行内文字标签在下拉选择之外；配置状态不在查询区内，由列表上沿页签承载。
    expect(within(filterRegion).getByText('金额状态')).toBeInTheDocument()
    expect(within(filterRegion).queryAllByRole('combobox')).toHaveLength(1)
    expect(selectedSelectValue(amountStatusFilter())).toBe('全部')
    expect(selectedStatusTabLabels()).toEqual(['全部'])
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
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
    await submitByEnter()
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
  it('金额状态下拉与配置状态页签都只在服务端结果上本地裁剪：切换即生效、不追加读取、不下推请求', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', isAmountConfigured: true, currentAmount: amount(12.5), configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', isAmountConfigured: false, currentAmount: null, configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A03', standardProjectName: '项目三', isAmountConfigured: true, currentAmount: amount(3), configurationStatus: 2 }),
    ])
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(query()).toHaveBeenCalledTimes(1)

    await pickSelectOption(amountStatusFilter(), '未配置')
    expect(renderedCodes()).toEqual(['A02'])
    expect(selectedSelectValue(amountStatusFilter())).toBe('未配置')

    await pickSelectOption(amountStatusFilter(), '已配置')
    expect(renderedCodes()).toEqual(['A01', 'A03'])

    clickConfigurationStatusTab('停用')
    expect(renderedCodes()).toEqual(['A03'])
    expect(selectedStatusTabLabels()).toEqual(['停用'])

    await pickSelectOption(amountStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A03'])

    clickConfigurationStatusTab('全部')
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(selectedStatusTabLabels()).toEqual(['全部'])

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

    // 进入按参数初始化：草稿取已生效关键字，金额状态下拉与配置状态页签取地址栏档位。
    expect(keywordInput().value).toBe('血常规')
    expect(selectedSelectValue(amountStatusFilter())).toBe('未配置')
    expect(selectedStatusTabLabels()).toEqual(['启用'])
    expect(renderedCodes()).toEqual(['A01'])

    // 档位变化按替换当前历史项写回，且不追加读取。
    const historyLength = window.history.length
    await pickSelectOption(amountStatusFilter(), '已配置')
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
    await submitByEnter()
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

describe('C30 院区恒取登录院区', () => {
  it('登录院区不是清单首项时仍取登录院区，不做首项回退（清单解析已随院区选择控件移除）', async () => {
    // 登录院区 BRA-2 不在任何可选清单里参与解析：查询恒用登录院区，不回退首项。
    authState.branchCode = 'BRA-2'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { branchCode: 'BRA-2' },
    ])
    await settle(first, [amountRow()])
  })
})

describe('C31 页头范围条', () => {
  it('页头由共用范围条承载：名称缺失显示「未知」，全程无编码文本', async () => {
    await renderLoaded([amountRow()])

    // 本文件按匿名会话替换宿主会话、占位 client 不含当前范围查询端点：三级名称都按缺失处理，
    // 页头只显示「未知」，不读取登录管理器、不回退显示编码。
    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    const header = document.querySelector('.recognition-amounts-header') as HTMLElement
    for (const code of ['ORG-A', 'HOS-1', 'BRA-1']) {
      expect(header.textContent).not.toContain(code)
    }
    expect(document.querySelector('.recognition-amounts-header-scope')).toBeNull()
  })
})

describe('C32 行内编辑金额', () => {
  it('点击当前金额值与未配置标记进入编辑，Esc 取消后编辑态收起且金额不变', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: amount(12.5), isAmountConfigured: true }),
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
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

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
    await settle(reload, [amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.getByText('12.50')).toBeInTheDocument()
  })

  it('保存与取消为图标按钮，悬停提示与可访问名称同一文案，保存按钮与回车同一条提交路径', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    // 取消按钮：收起编辑态、丢弃输入，与 Esc 同路径（S3-D27 2026-10-13 修订（第三轮））。
    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    click(screen.getByRole('button', { name: '取消修改金额：血常规' }))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    // 保存为图标主按钮、取消为图标默认按钮：可访问名由 aria-label 组成，按钮文本不再出现文字。
    editAmount('血常规')
    const saveButton = screen.getByRole('button', { name: '保存金额：血常规' })
    expect(saveButton).toHaveClass('ant-btn-primary', 'ant-btn-sm')
    expect(saveButton.querySelector('.anticon-check')).not.toBeNull()
    expect(saveButton.textContent).not.toContain('保存')

    const cancelButton = screen.getByRole('button', { name: '取消修改金额：血常规' })
    expect(cancelButton).toHaveClass('ant-btn-sm')
    expect(cancelButton).not.toHaveClass('ant-btn-primary')
    expect(cancelButton.querySelector('.anticon-close')).not.toBeNull()
    expect(cancelButton.textContent).not.toContain('取消')

    // 悬停提示与可访问名称同一文案；离开保存按钮再悬停取消按钮，离开动画在 jsdom 没有
    // transitionend、残影可能滞留，按文案在任一 tooltip 节点中出现判定。
    fireEvent.mouseEnter(saveButton)
    expect(await screen.findByRole('tooltip')).toHaveTextContent('保存金额：血常规')
    fireEvent.mouseLeave(saveButton)
    fireEvent.mouseEnter(cancelButton)
    await waitFor(() =>
      expect(screen.getAllByRole('tooltip').some((node) => node.textContent?.includes('取消修改金额：血常规'))).toBe(true),
    )

    // 保存按钮：与回车同一条提交路径，成功后收起编辑态并整表重载。
    await change(inlineAmountInput(), '15')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    click(screen.getByRole('button', { name: '保存金额：血常规' }))

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 15,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(15), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('15.00')
  })

  it('后台刷新改变同一行金额后编辑器以当前金额值为键重挂并回填新值', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '999')

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

    // 目标行仍在当前行集：编辑态保留，但以新金额值重挂，回填值随当前行数据更新。
    await waitFor(() => expect(inlineAmountInput().value).toBe('20.00'))
    expect(save()).not.toHaveBeenCalled()
  })

  it('状态档把目标行裁出当前行集后不渲染编辑态', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: amount(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    await pickSelectOption(amountStatusFilter(), '未配置')
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

describe('C34 提交前现值复核（只保留可信组织或医院现读不到的拦截）', () => {
  /**
   * message 拦截提示（S3-D28；以 message.error 承载，不再渲染在单元格内）：
   * 一句中文须含发生了什么、结果与下一步三段。院区恒随登录院区、不存在用户选定的
   * 滞留范围，「可信范围已变化」的拦截随范围选择器一并移除，只保留现读可信组织或医院读不到的拦截。
   */
  const REASON_UNREADABLE = '读取不到完整可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。'

  it('现读可信组织缺失时提交：零写请求，以 message.error 给出读取不到完整可信范围的提示', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进命令式现读的组织取值：「现读不到」的结论只由可信组织或医院的缺失给出。
    authState.organizationCode = null
    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截提示以 message.error 反馈，单元格内不再渲染任何拦截原因。
    expect((await screen.findAllByText(REASON_UNREADABLE)).length).toBeGreaterThan(0)
    expect(within(rowOf('血常规')).queryByText(REASON_UNREADABLE)).toBeNull()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })

  it('现读可信医院缺失时提交：零写请求，以 message.error 给出读取不到完整可信范围的提示', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进命令式现读的医院取值：「现读不到」的结论只由可信组织或医院的缺失给出。
    authState.hospitalCode = null
    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截提示以 message.error 反馈，单元格内不再渲染任何拦截原因。
    expect((await screen.findAllByText(REASON_UNREADABLE)).length).toBeGreaterThan(0)
    expect(within(rowOf('血常规')).queryByText(REASON_UNREADABLE)).toBeNull()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })

  it('可信范围现读完整时提交：照常发出保存请求并整表重载，行内不出现拦截原因', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    // 记录侧概念已随范围选择器移除：现读可信组织与医院齐全即放行，载荷用页面生效的登录院区。
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
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
    expect(screen.queryByText(/本次不提交写入/)).not.toBeInTheDocument()
  })

  it('登录院区现值在提交时已变化：保存以提交时的生效院区发出，随后的渲染按新登录院区切换重读', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    /**
     * 只推进命令式读取的现值、不通知订阅者（不走 `changeTrustedScope` 的 notify 路径）：
     * 提交发起时页面仍展示登录院区 BRA-1 的行集，没有「页面应用范围未跟上可信范围」的
     * 滞留拦截，保存以提交时页面生效的登录院区发出、与所编辑行集一致；随后保存成功复位编辑目标
     * 触发页面重渲染，渲染期现读读到新登录院区 BRA-3，范围键变化即视为范围切换，主体清空旧行集
     * 并按 BRA-3 重读。
     */
    authState.branchCode = 'BRA-3'
    save().mockResolvedValueOnce(undefined)
    const writeReload = armQuery()
    const switchReload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 99,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[1][1]).toEqual({ branchCode: 'BRA-1' })
    expect(query().mock.calls[2][1]).toEqual({ branchCode: 'BRA-3' })
    // BRA-1 的写后重读因范围键已切换被丢弃，行集以 BRA-3 的重读为准。
    await settle(writeReload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])
    await settle(switchReload, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(queryInlineAmountInput()).toBeNull()
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
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: amount(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: amount(20) }),
      amountRow({ standardProjectCode: 'A03', isAmountConfigured: false, currentAmount: null }),
    ])

    expect(screen.getByText('共 3 项')).toBeInTheDocument()

    await pickSelectOption(amountStatusFilter(), '未配置')
    expect(screen.getByText('共 1 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)

    await pickSelectOption(amountStatusFilter(), '全部')
    expect(screen.getByText('共 3 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C36 读取在途写门控', () => {
  it('读取在途时框内查询图标按钮呈加载态、金额格不进入编辑态，读取结束才恢复', async () => {
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

describe('页面标题层级', () => {
  it('页面大标题为 level 3（正常态与阻断态同层级，配套 CSS 与平台页共用）', async () => {
    await renderLoaded([amountRow()])

    expect(screen.getByRole('heading', { name: '本院区互认项目金额', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '本院区互认项目金额', level: 2 })).toBeNull()
  })
})

/**
 * 列表版面与「互认项目」页及平台管理员页统一（S3-D24、S3-D29 2026-10-13 修订，Client/design.md
 * 「列表」）：列宽按渲染出的 col 内联宽度断言，表格样式类与横向滚动基准按页面实际渲染断言
 * （原「维持 antd Table 默认样式」口径作废）。
 */
describe('列表版面与两页统一', () => {
  it('类型、分类、分组、当前金额、配置状态、原因列宽为 88/186/208/190/130/190，项目列弹性填充不带内联宽度', async () => {
    await renderLoaded([amountRow()])

    expect(document.querySelector('.recognition-amounts-table')).not.toBeNull()
    const widths = [...document.querySelectorAll<HTMLElement>('.recognition-amounts-table colgroup col')]
      .map((col) => col.style.width)
    // col 按列序渲染：类型、分类、分组、项目、当前金额、互认配置状态、当前不可用原因。
    expect(widths).toEqual(['88px', '186px', '208px', '', '190px', '130px', '190px'])
    // 横向滚动基准与平台管理员页同值：rc-table 把它写成表格元素的内联宽度。
    const scrollTable = document.querySelector<HTMLElement>('.recognition-amounts-table .ant-table-content > table')
    expect(scrollTable?.style.width).toBe('1252px')
  })

  it('互认配置状态徽标颜色与「互认项目」页同一推导：启用 success、停用默认色', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '启用项目', configurationStatus: 1, configurationStatusText: '启用' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '停用项目', configurationStatus: 2, configurationStatusText: '停用' }),
    ])

    expect(rowOf('启用项目').querySelector('.ant-tag')?.classList.contains('ant-tag-success')).toBe(true)
    const disabledTag = rowOf('停用项目').querySelector('.ant-tag')
    expect(disabledTag?.classList.contains('ant-tag-success')).toBe(false)
    expect(disabledTag?.classList.contains('ant-tag-warning')).toBe(false)
  })
})

/*
 * 本院区金额页导出链的组件层用例（页面与交互口径见项目导出阶段 Client 设计「导出按钮门控与状态」
 * 与「点击导出的行为」两节，形态与平台金额页同款；差异只在范围来源与门控来源）：
 * 1. 导出按钮由共用列表视图组件承载在配置状态页签行右侧，可访问名称与平台金额页一致；
 * 2. 请求体只有登录院区与已生效关键字（组织与医院由服务端从可信上下文解析，页面不提交；
 *    配置状态页签与金额状态档位不参与）；
 * 3. 门控：登录院区缺失随空态禁用（本页不整页阻断，与平台页「范围未选全」不同源——本页没有
 *    范围选择器）、读取失败或写入后刷新失败由共用组件按既有写门控禁用，均零请求；导出在途
 *    loading 防重复；列表读取在途与行内编辑进行中不限制导出，既有写门控零改动；
 * 4. 点击时刻现读登录院区并要求与已应用一致；成功经共享下载模块保存（下载名按发起时刻构造）；
 *    失败仅结束 loading、保留列表与筛选、不新增页面级提示层；导出不写本页路由查询参数。
 */
/** 导出按钮按可访问名称定位：名称表达页面与动作（可见文本为「导出」）。 */
const exportButton = () => screen.getByRole('button', { name: '导出互认项目金额' })
const queryExportButton = () => screen.queryByRole('button', { name: '导出互认项目金额' })

/**
 * 「不新增页面级错误层」按页面根容器断言（与平台金额页同款）：页面自身的 message holder 挂在
 * 列表视图根部，而同文件更早用例经 antd 静态 message 弹出的提示渲染在 body 上、jsdom 无动画
 * 不消失，按 document 断言会命中与本用例无关的残留节点。
 */
const pageRoot = () => document.querySelector('.recognition-amounts-page') as HTMLElement
const queryPageAlert = () => within(pageRoot()).queryByRole('alert')

/** 排队一次导出交付，并断言当前没有任何导出请求被发起（禁用态零请求用例的公共收尾）。 */
async function expectNoBranchExportRequest() {
  api.exportBranchRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))
  click(exportButton())
  await act(async () => {})
  expect(api.exportBranchRecognitionAmounts).not.toHaveBeenCalled()
}

describe('本院区金额页导出的承载位置与请求体', () => {
  it('导出按钮与配置状态页签同处一个工具行、随 DOM 顺序在页签之后，页面其余区块不变', async () => {
    await renderLoaded([amountRow()])

    const button = exportButton()
    // 标准按钮语义：承载元素就是 button（可见文本「导出」，可访问名称补充页面语境）。
    expect(button.tagName).toBe('BUTTON')
    // 同一行承载：按钮与页签容器同处一个工具行（页签行右侧由该容器的两端布局承载）。
    const toolbar = button.closest('.recognition-amounts-status-toolbar')
    expect(toolbar).not.toBeNull()
    const group = within(toolbar as HTMLElement).getByRole('group', { name: '配置状态筛选' })
    // DOM 顺序：页签在前、按钮在后（键盘焦点顺序随 DOM 顺序）。
    expect(group.compareDocumentPosition(button) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0)
    // 页面其余区块不因导出能力变化：七列列头与页脚计数保持原样。
    expect(columnHeaders()).toEqual(['类型', '分类', '分组', '项目', '当前金额', '互认配置状态', '当前不可用原因'])
    expect(screen.getByText(/共 \d+ 项/)).toBeInTheDocument()
  })

  it('请求体只有登录院区与已生效关键字；配置状态页签与金额状态档位不参与请求', async () => {
    await renderLoaded([amountRow()])
    await change(keywordInput(), '血')
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])

    // 两个档位只是本地裁剪：在「停用」页签与金额状态「已配置」档位下导出，档位都不进入请求。
    clickConfigurationStatusTab('停用')
    await pickSelectOption(amountStatusFilter(), '已配置')
    api.exportBranchRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))

    // 页面把 client 原样交给适配层出口；请求体恰为登录院区与已生效关键字，
    // 组织与医院不提交（由服务端从可信上下文解析）。
    expect(api.exportBranchRecognitionAmounts.mock.calls[0][0]).toBe(clientRef.current)
    const payload = api.exportBranchRecognitionAmounts.mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(payload)).toEqual(['branchCode', 'keyword'])
    expect(payload).not.toHaveProperty('organizationCode')
    expect(payload).not.toHaveProperty('hospitalCode')
  })

  it('已生效关键字为空时请求体只有登录院区', async () => {
    await renderLoaded([amountRow()])
    api.exportBranchRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))

    const payload = api.exportBranchRecognitionAmounts.mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ branchCode: 'BRA-1' })
    expect(Object.keys(payload)).toEqual(['branchCode'])
  })

  it('导出在途按钮进入 loading 且再次点击不发起新请求，完成后恢复可用', async () => {
    await renderLoaded([amountRow()])
    const payload = deferred<ArrayBuffer>()
    api.exportBranchRecognitionAmounts.mockReturnValueOnce(payload.promise)

    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))
    expect(exportButton()).toHaveClass('ant-btn-loading')
    expect(exportButton()).toBeDisabled()

    click(exportButton())
    await act(async () => {})
    expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1)

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())
  })
})

describe('本院区金额页导出成功后的保存与失败恢复', () => {
  it('取得字节后经共享下载模块触发保存，文件名为「本院区互认项目金额 + 发起时刻 14 位时间戳 + .xlsx」', async () => {
    await renderLoaded([amountRow()])
    const bytes = new ArrayBuffer(8)
    api.exportBranchRecognitionAmounts.mockResolvedValueOnce(bytes)

    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))

    // 字节原样交给共享模块；下载名按发起时刻构造（时间戳精确格式由适配层用例固定）。
    expect(exportDownload.saveExportWorkbook.mock.calls[0][0]).toBe(bytes)
    expect(exportDownload.saveExportWorkbook.mock.calls[0][1]).toMatch(/^本院区互认项目金额\d{14}\.xlsx$/)
    await waitFor(() => expect(exportButton()).toBeEnabled())
  })

  it('导出请求失败：结束加载、保留列表与已生效筛选，不重读、不新增页面级错误层', async () => {
    await renderLoaded([amountRow()])
    await change(keywordInput(), '血')
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])
    api.exportBranchRecognitionAmounts.mockRejectedValueOnce(new Error('业务拒绝：当前条件下无可导出数据。'))

    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())

    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('血')
    // 导出失败不触发列表重读：读取只有进入页面与关键字提交两次。
    expect(query()).toHaveBeenCalledTimes(2)
    // 页面不解析错误响应体、不新增提示层：页面根容器内不出现 Alert。
    expect(queryPageAlert()).not.toBeInTheDocument()
  })

  it('共享下载模块对 0 字节内容按契约错误抛出时走同一失败路径，不产生保存成功假象', async () => {
    await renderLoaded([amountRow()])
    api.exportBranchRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(0))
    exportDownload.saveExportWorkbook.mockRejectedValueOnce(new Error('导出未返回文件内容。'))

    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())

    expect(exportDownload.saveExportWorkbook.mock.calls[0][0].byteLength).toBe(0)
    expect(screen.getByText('血常规')).toBeInTheDocument()
    // 契约错误同走失败路径：不重读、不新增页面级错误层。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(queryPageAlert()).not.toBeInTheDocument()
  })
})

describe('本院区金额页导出与页面其他状态的共存', () => {
  it('列表读取在途与行内编辑进行中不限制导出，导出在途也不阻塞列表读取与金额写入口', async () => {
    await renderLoaded([amountRow()])

    // 列表读取在途：导出保持可用（导出是只读链，不与读取在途互斥）。
    const refresh = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(exportButton()).toBeEnabled()
    await settle(refresh, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    // 行内编辑进行中（无范围变化）：导出不受限制。
    editAmount('血常规')
    await screen.findByLabelText('金额')
    expect(exportButton()).toBeEnabled()

    // 导出在途：既有写门控不因导出改变——提交查询仍会发起一次列表读取。
    const payload = deferred<ArrayBuffer>()
    api.exportBranchRecognitionAmounts.mockReturnValueOnce(payload.promise)
    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [amountRow()])

    // 收起行内编辑后写入口按既有门控恢复：导出在途不禁用它。
    click(screen.getByRole('button', { name: '取消修改金额：血常规' }))
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
  })

  it('导出在途时切换配置状态页签不被阻塞，也不发起第二次导出', async () => {
    await renderLoaded([amountRow()])
    const payload = deferred<ArrayBuffer>()
    api.exportBranchRecognitionAmounts.mockReturnValueOnce(payload.promise)

    click(exportButton())
    await waitFor(() => expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1))

    clickConfigurationStatusTab('停用')
    expect(selectedStatusTabLabels()).toEqual(['停用'])
    expect(api.exportBranchRecognitionAmounts).toHaveBeenCalledTimes(1)

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
  })

  it('导出动作与条件不写入本页路由查询参数', async () => {
    await renderLoaded([amountRow()])
    api.exportBranchRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    expect(window.location.search).toBe('')
    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    expect(window.location.search).toBe('')
  })

  it('点击时刻现读登录院区与已应用不一致（宿主改绑、页面尚未应用）零请求', async () => {
    await renderLoaded([amountRow()])

    // 只推进命令式现读的登录院区、不通知订阅者：页面仍展示已应用院区 BRA-1 的行集，
    // 点击导出时现读 BRA-3 与已应用不一致，整段放弃（零请求）。
    authState.branchCode = 'BRA-3'
    await expectNoBranchExportRequest()
  })
})

/*
 * 导出按钮的可达与禁用面（本页已接线导出，工单 04 的「本页不接线导出不可达」守卫按新事实改写：
 * 正常态按钮存在且可用；阻断态不可达（页面早退路径上，共用组件不挂载）；登录院区缺失态本页
 * 不整页阻断，按钮随空态禁用、零请求——原「空态不渲染按钮」的断言随接线翻转）。
 */
describe('本院区金额页导出按钮的可达与禁用面', () => {
  it('正常态（列表已加载）导出按钮存在且可用', async () => {
    await renderLoaded([amountRow()])
    await screen.findByText('血常规')

    expect(queryExportButton()).toBeInTheDocument()
    expect(exportButton()).toBeEnabled()
  })

  it('可信组织缺失的整页阻断态不渲染导出按钮，也不发起业务请求', async () => {
    authState.organizationCode = null
    renderPage()
    await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')

    expect(queryExportButton()).not.toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
  })

  it('登录院区缺失的空态导出按钮禁用且零请求（本页不整页阻断）', async () => {
    authState.branchCode = null
    renderPage()
    await screen.findByText('当前登录身份未携带院区，无法查询互认项目金额')

    expect(queryExportButton()).toBeInTheDocument()
    expect(exportButton()).toBeDisabled()
    await expectNoBranchExportRequest()
    expect(query()).not.toHaveBeenCalled()
  })
})
