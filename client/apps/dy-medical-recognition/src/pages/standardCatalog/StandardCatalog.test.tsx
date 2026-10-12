/**
 * 阶段 1 前端 Component 层用例（测试矩阵 C57 / C58 / C62 / C63 / C64 / C65 / C68 / C69 / C75，
 * 以及 C76～C84 的组件层可自动化部分）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./standardCatalogApi` 只替换 12 个 I/O 函数（读取与写入），其余纯函数
 *    （`visibleCatalogCategories` / `resolveTreeScope` / `filterItems` / `isItemEffective` /
 *     `ineffectiveReasons` / `toRemark` / 常量与类型）**使用真实实现**；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client —— 页面把 client 原样传给上面的
 *    适配层函数，故不构造真实鉴权适配器；被测对象仍是页面真实调用链。
 *
 * 仍属 Host 层、不在本文件覆盖：C66 / C67 / C31 多视口，以及 C62 与 C64 的 Host 分支；
 * C76～C84 的多视口核对（1440×960、768×1024、390×844）、像素级宽度截断与横向滚动核对、
 * 卡片撑满视口与 0.5 秒延迟的实机观感同样属于 Host 层，本文件只覆盖节点槽位结构、筛选条
 * 结构与本地语义、Tooltip 条件挂载与延迟、合并状态列、列头结构与汇总条等组件层断言。
 * 两处 `it.skip` 记录的是**已知且未获裁定不改代码**的缺口，原因写在各自用例内。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { StandardCatalog } from './StandardCatalog'
import type { CatalogData, StandardCategory, StandardGroup, StandardItem } from './standardCatalogApi'

const api = vi.hoisted(() => ({
  queryCategories: vi.fn(),
  queryGroups: vi.fn(),
  queryItems: vi.fn(),
  createCategory: vi.fn(),
  updateCategory: vi.fn(),
  createGroup: vi.fn(),
  updateGroup: vi.fn(),
  createItem: vi.fn(),
  updateItemRemark: vi.fn(),
  setCategoryEnabled: vi.fn(),
  setGroupEnabled: vi.fn(),
  setItemEnabled: vi.fn(),
}))

/** 稳定的占位 client（页面只把它透传给被测适配层函数）。 */
const stubClient = vi.hoisted(() => ({}))

vi.mock('./standardCatalogApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./standardCatalogApi')>()
  return { ...actual, ...api }
})

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  // 必须返回**稳定引用**：页面把 client 作为 reload 的 useCallback 依赖，
  // 每次返回新对象会让 effect 反复重跑并耗尽一次性 mock 队列。
  useApiClientContext: () => stubClient,
}))

/*
 * jsdom 没有布局引擎：`scrollWidth` / `clientWidth` 恒为 0，S1-D38 的截断量宽
 * （scrollWidth > clientWidth）在测试环境里无法区分长短文本。这里只对量宽关心的两类元素
 * （树节点名称、表格省略单元格）提供宽度替身：按文本长度给 scrollWidth、给固定 clientWidth；
 * 其余元素仍走 jsdom 原始取值（0），不改变任何其他组件的观测结果。
 * 这是环境替身而非断言放宽——真实量宽、截断观感与 0.5 秒延迟的实机行为由宿主验收核对。
 */
const originalScrollWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'scrollWidth')
  ?? Object.getOwnPropertyDescriptor(Element.prototype, 'scrollWidth')
const originalClientWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientWidth')
  ?? Object.getOwnPropertyDescriptor(Element.prototype, 'clientWidth')
Object.defineProperty(HTMLElement.prototype, 'clientWidth', {
  configurable: true,
  get(this: HTMLElement) {
    if (this.classList.contains('standard-catalog-node-name')) return 216
    if (this.classList.contains('standard-catalog-cell-text')) return 160
    return originalClientWidth?.get?.call(this) ?? 0
  },
})
Object.defineProperty(HTMLElement.prototype, 'scrollWidth', {
  configurable: true,
  get(this: HTMLElement) {
    if (this.classList.contains('standard-catalog-node-name') || this.classList.contains('standard-catalog-cell-text')) {
      return (this.textContent ?? '').length * 10
    }
    return originalScrollWidth?.get?.call(this) ?? 0
  },
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

interface PendingRead {
  categories: ReturnType<typeof deferred<StandardCategory[]>>
  groups: ReturnType<typeof deferred<StandardGroup[]>>
  items: ReturnType<typeof deferred<StandardItem[]>>
}

function pendingRead(): PendingRead {
  return {
    categories: deferred<StandardCategory[]>(),
    groups: deferred<StandardGroup[]>(),
    items: deferred<StandardItem[]>(),
  }
}

/** 排队一次完整读取（页面每次 reload 固定按 分类 → 分组 → 项目 顺序发起）。 */
function armRead(read: PendingRead) {
  api.queryCategories.mockReturnValueOnce(read.categories.promise)
  api.queryGroups.mockReturnValueOnce(read.groups.promise)
  api.queryItems.mockReturnValueOnce(read.items.promise)
  return read
}

async function settle(read: PendingRead, data: CatalogData) {
  await act(async () => {
    read.categories.resolve(data.categories)
    read.groups.resolve(data.groups)
    read.items.resolve(data.items)
  })
}

async function fail(read: PendingRead, reason = new Error('读取失败')) {
  await act(async () => {
    read.categories.reject(reason)
    read.groups.reject(reason)
    read.items.reject(reason)
  })
}

const cat = (
  id: string,
  name: string,
  itemType: 0 | 1 = 0,
  isValid = true,
  usageStatus: 0 | 1 = 0,
  /** 服务端随行枚举文案；不传即模拟契约字段缺失，用于验证页面回退到枚举元数据/本地兜底。 */
  texts: { itemTypeText?: string | null; usageStatusText?: string | null } = {},
): StandardCategory => ({ id, itemType, name, remark: null, isValid, usageStatus, itemTypeText: texts.itemTypeText ?? null, usageStatusText: texts.usageStatusText ?? null })

const grp = (
  id: string,
  categoryId: string,
  name: string,
  isValid = true,
  usageStatus: 0 | 1 = 0,
  /** 服务端随行使用情况文案；不传即模拟契约字段缺失。 */
  usageStatusText: string | null = null,
): StandardGroup => ({ id, categoryId, name, remark: null, isValid, usageStatus, usageStatusText })

const itm = (
  id: string,
  categoryId: string,
  groupId: string,
  code: string,
  name: string,
  /** 允许 null：用于装配「状态数据不完整」起点（矩阵 C83 的「未知」分支）。 */
  isValid: boolean | null = true,
  /** 服务端随行项目类型文案；不传即模拟契约字段缺失。 */
  itemTypeText: string | null = null,
): StandardItem => ({ id, categoryId, groupId, itemType: 0, code, name, remark: null, isValid, itemTypeText })

const EMPTY: CatalogData = { categories: [], groups: [], items: [] }
const CHAIN: CatalogData = {
  categories: [cat('c1', '甲分类')],
  groups: [grp('g1', 'c1', '甲分组')],
  items: [],
}

/** 与真实运行时对齐：宿主由 `SubAppThemeProvider` 提供 antd 的 zh-CN 语言包。 */
function renderPage() {
  return render(
    <ConfigProvider locale={zhCN}>
      <StandardCatalog />
    </ConfigProvider>,
  )
}

const click = (element: HTMLElement) =>  act(() => {
    fireEvent.click(element)
  })

/** 弹窗内查询：页面列表筛选用同一批 aria-label，必须限定到 dialog 作用域。 */
const modal = () => within(screen.getByRole('dialog'))

const change = (element: HTMLElement, value: string) =>
  act(async () => {
    fireEvent.change(element, { target: { value } })
  })

/**
 * 按行内唯一文本定位表格行：操作列改为可见文字按钮后不再携带行名 aria-label，
 * 跨行同名按钮（每行都有「修改备注」「更多」）一律收窄到行作用域内查询。
 */
function rowOf(rowText: string): HTMLElement {
  const row = screen.getByText(rowText).closest('tr')
  expect(row).not.toBeNull()
  return row as HTMLElement
}

async function renderLoaded(data: CatalogData) {
  const read = armRead(pendingRead())
  renderPage()
  await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(1))
  await settle(read, data)
  await waitFor(() => expect(screen.getByRole('button', { name: /新增项目/ })).toBeEnabled())
}

/** 「新增分组」在分类节点的下拉菜单里；按树顺序取第 index 个分类。 */
async function openCreateGroup(index = 0) {
  click(screen.getAllByLabelText('分类更多操作')[index])
  const menuItem = await screen.findByRole('menuitem', { name: /新增分组/ })
  click(menuItem)
  await screen.findByLabelText('分组名称')
}

beforeEach(() => {
  for (const fn of Object.values(api)) fn.mockReset()
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

describe('C58 组件加载与重载的乱序响应', () => {
  /**
   * 真实可达的重叠读取入口是失败横幅里的「重试」按钮：顶部「重新读取目录」按钮带
   * `loading`，读取进行中点击会被忽略（防重复提交），因此只有「重试」在读取进行中
   * 仍可点击。用例经它制造 A、B 两次重叠读取。
   */
  async function renderFailedThenBanner() {
    const first = armRead(pendingRead())
    renderPage()
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(1))
    await fail(first)
    await screen.findByText('目录数据待刷新')
  }

  it('只接纳最新发起的读取：迟到的 A 不覆盖 B 的数据', async () => {
    await renderFailedThenBanner()

    const a = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(2))

    const b = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(3))

    await settle(b, { categories: [cat('c-b', 'B分类')], groups: [], items: [] })
    await screen.findByText('B分类')

    await settle(a, { categories: [cat('c-a', 'A分类')], groups: [], items: [] })
    await waitFor(() => expect(screen.queryByText('A分类')).not.toBeInTheDocument())
    expect(screen.getByText('B分类')).toBeInTheDocument()
  })

  it('B 失败后，迟到的 A 成功也不能把页面改成已就绪', async () => {
    await renderFailedThenBanner()

    const a = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(2))

    const b = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(3))

    await fail(b)
    await screen.findByText('目录数据待刷新')
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeDisabled()

    await settle(a, { categories: [cat('c-a', 'A分类')], groups: [], items: [] })
    await waitFor(() => expect(screen.getByText('目录数据待刷新')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeDisabled()
    expect(screen.queryByText('A分类')).not.toBeInTheDocument()
  })

  it('读取未完成期间切换本地筛选不增加读取次数，且结果按当前筛选计算', async () => {
    await renderFailedThenBanner()

    const a = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(2))

    const b = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(3))

    await change(screen.getByLabelText('编码或名称'), 'AB')
    expect(api.queryCategories).toHaveBeenCalledTimes(3)
    expect(api.queryGroups).toHaveBeenCalledTimes(3)
    expect(api.queryItems).toHaveBeenCalledTimes(3)

    await settle(b, {
      categories: [cat('c-b', 'B分类')],
      groups: [grp('g-b', 'c-b', 'B分组')],
      items: [itm('i1', 'c-b', 'g-b', 'AB-1', '命中项'), itm('i2', 'c-b', 'g-b', 'ZZ-9', '未命中项')],
    })
    await settle(a, EMPTY)

    await screen.findByText('命中项')
    expect(screen.queryByText('未命中项')).not.toBeInTheDocument()
  })
})

describe('C62 必填本地校验与写失败输入保留（Component 层）', () => {
  it('新增分类：名称缺失与纯空白均本地拦截、写请求为零、弹窗与其它输入保留', async () => {
    await renderLoaded(CHAIN)

    click(screen.getByLabelText('新增检验分类'))
    const name = (await screen.findByLabelText('分类名称')) as HTMLInputElement

    click(screen.getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('分类名称必填')
    expect(api.createCategory).not.toHaveBeenCalled()

    await change(name, '   ')
    await change(screen.getByLabelText('备注'), '保留的备注')
    click(screen.getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('分类名称必填')
    expect(api.createCategory).not.toHaveBeenCalled()

    expect(screen.getByLabelText('分类名称')).toBeInTheDocument()
    expect((screen.getByLabelText('备注') as HTMLTextAreaElement).value).toBe('保留的备注')
  })

  it('新增分组：名称缺失本地拦截且写请求为零', async () => {
    await renderLoaded(CHAIN)

    await openCreateGroup(0)
    click(screen.getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('分组名称必填')
    expect(api.createGroup).not.toHaveBeenCalled()
  })

  it('新增项目：编码与名称缺失本地拦截且写请求为零', async () => {
    await renderLoaded(CHAIN)

    click(screen.getByRole('button', { name: /新增项目/ }))
    await modal().findByLabelText('标准项目编码')
    click(modal().getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('编码必填')
    await screen.findByText('名称必填')
    expect(api.createItem).not.toHaveBeenCalled()
  })
})

describe('C63 脏表单关闭与切换对象', () => {
  const TWO: CatalogData = { categories: [cat('c1', '甲分类'), cat('c2', '乙分类')], groups: [], items: [] }

  it('关闭并重开同一对象：恢复已保存值且本地校验错误清除', async () => {
    await renderLoaded(TWO)

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    await change(screen.getByLabelText('分类名称'), '')
    click(screen.getByRole('button', { name: /保\s*存/ }))
    await screen.findByText('分类名称必填')

    click(screen.getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(screen.queryByLabelText('分类名称')).not.toBeInTheDocument())

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    expect((screen.getByLabelText('分类名称') as HTMLInputElement).value).toBe('甲分类')
    expect(screen.queryByText('分类名称必填')).not.toBeInTheDocument()
  })

  it('切换对象：只显示当前对象的值，无旧值残留', async () => {
    await renderLoaded(TWO)

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    await change(screen.getByLabelText('分类名称'), '甲-dirty')
    click(screen.getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(screen.queryByLabelText('分类名称')).not.toBeInTheDocument())

    click(screen.getByLabelText('编辑分类：乙分类'))
    await screen.findByLabelText('分类名称')
    expect((screen.getByLabelText('分类名称') as HTMLInputElement).value).toBe('乙分类')
  })

  it('新增弹窗填入脏值后取消，重开回到默认值', async () => {
    await renderLoaded(TWO)

    click(screen.getByLabelText('新增检验分类'))
    await screen.findByLabelText('分类名称')
    await change(screen.getByLabelText('分类名称'), '脏值')
    click(screen.getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(screen.queryByLabelText('分类名称')).not.toBeInTheDocument())

    click(screen.getByLabelText('新增检验分类'))
    await screen.findByLabelText('分类名称')
    expect((screen.getByLabelText('分类名称') as HTMLInputElement).value).toBe('')
  })
})

describe('C64 可信身份未就绪与失效（Component 层可覆盖部分）', () => {
  it('身份未就绪导致读取全部失败时，不放行写入入口', async () => {
    const read = armRead(pendingRead())
    renderPage()
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(1))
    await fail(read, new Error('身份未就绪'))

    await screen.findByText('目录数据待刷新')
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeDisabled()
    expect(api.createItem).not.toHaveBeenCalled()
    expect(api.createCategory).not.toHaveBeenCalled()
  })

  it('写请求因身份失效被拒绝时：不重放、不误关弹窗、不误报成功、不触发重载', async () => {
    /**
     * 写失败的拒绝只由宿主统一展示，页面不得自行捕获（总体设计 5.2）。本用例**局部**安装未处理
     * 拒绝的观测器、用例结束即移除：既不依赖也不改动 `vite.config.ts` 的全局设置，因此
     * 「页面未吞掉本次写失败、也未额外产生别的拒绝」在这里是可判红的（判定点见下）。
     */
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded({ categories: [cat('c1', '甲分类')], groups: [grp('g1', 'c1', '甲分组')], items: [itm('i1', 'c1', 'g1', 'A-1', '甲项目')] })
      const rejection = new Error('身份已失效')
      api.updateItemRemark.mockRejectedValueOnce(rejection)
      const readsBefore = api.queryCategories.mock.calls.length

      click(within(rowOf('甲项目')).getByRole('button', { name: '修改备注' }))
      await modal().findByLabelText('备注')
      await change(modal().getByLabelText('备注'), '保留的备注')
      click(modal().getByRole('button', { name: /保\s*存/ }))

      await waitFor(() => expect(api.updateItemRemark).toHaveBeenCalledTimes(1))
      // 页面不捕获该拒绝：观测必须恰好收到写失败这一笔；页面若自行捕获、改判成功或额外产生拒绝即判红。
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))
      await waitFor(() => expect(screen.getByRole('dialog')).toBeInTheDocument())
      expect((modal().getByLabelText('备注') as HTMLTextAreaElement).value).toBe('保留的备注')
      expect(api.queryCategories.mock.calls.length).toBe(readsBefore)
      // 收尾再判一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏判。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })

  it.skip('身份有效切换会清理选择/缓存/表单/弹窗 —— 页面无身份消费点，待负责人裁定归属', () => {
    // `StandardCatalog` 不消费可信身份：身份由 main.tsx 的 `AuthProvider` 与
    // contexts/ApiClientContext.tsx 的 `createAuthenticatedAdapter` 承担，页面内没有任何
    // 身份变更订阅。因此「切换即清理」在本组件内**不存在可测对象**；该期望实际由
    // 「宿主切换时卸载并重进子应用」满足，属 C64 的 Host 层。
  })
})

describe('C65 依赖未就绪与未知枚举防御', () => {
  it('依赖未就绪时写入口禁用并提供重试入口', async () => {
    const read = armRead(pendingRead())
    renderPage()
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(1))
    await fail(read)

    await screen.findByText('目录数据待刷新')
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /重\s*试/ })).toBeInTheDocument()
  })

  it('未知使用情况到达页面时显示未知，不默认映射为未使用', async () => {
    await renderLoaded({
      categories: [{ id: 'c1', itemType: 0, name: '甲分类', remark: null, isValid: true, usageStatus: null, itemTypeText: null, usageStatusText: null }],
      groups: [{ id: 'g1', categoryId: 'c1', name: '甲分组', remark: null, isValid: true, usageStatus: null, usageStatusText: null }],
      items: [],
    })

    click(screen.getByLabelText('编辑分组：甲分组'))
    await screen.findByLabelText('分组名称')
    expect(screen.getByText('未知')).toBeInTheDocument()
    expect(screen.queryByText('未使用')).not.toBeInTheDocument()
    expect(screen.queryByText('已使用')).not.toBeInTheDocument()
  })

  /**
   * 越界数值不得被渲染成空文本：适配层会把未知取值收窄为 `null`，但展示层仍需自己兜住取值域，
   * 否则任何绕过适配层的调用都会让「使用情况」显形为空白（既不是中文也不是「未知」）。
   * 这里直接以越界数值装配页面，锁定展示层自身的取值域判断。
   */
  it('越界使用情况数值显示未知，不渲染为空文本', async () => {
    await renderLoaded({
      categories: [{ id: 'c1', itemType: 0, name: '甲分类', remark: null, isValid: true, usageStatus: 7 as unknown as StandardCategory['usageStatus'], itemTypeText: null, usageStatusText: null }],
      groups: [],
      items: [],
    })

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    expect(modal().getByText('未知')).toBeInTheDocument()
    expect(screen.queryByText('未使用')).not.toBeInTheDocument()
    expect(screen.queryByText('已使用')).not.toBeInTheDocument()
  })

  it('适配层不把未知枚举值默认映射为已知值（真实映射函数）', async () => {
    const actual = await vi.importActual<typeof import('./standardCatalogApi')>('./standardCatalogApi')
    const client = {
      api: {
        medicalRecognitionReportQuery: {
          queryMedicalStandardCategoryList: {
            post: vi.fn().mockResolvedValue([
              { categoryId: 'c1', itemType: 7, name: '未知类型', remark: null, isValid: true, usageStatus: 5 },
            ]),
          },
          queryMedicalStandardGroupList: {
            post: vi.fn().mockResolvedValue([
              { groupId: 'g1', categoryId: 'c1', name: '未知使用情况', remark: null, isValid: true, usageStatus: 9 },
            ]),
          },
          queryMedicalStandardItemList: {
            post: vi.fn().mockResolvedValue([
              { itemId: 'i1', categoryId: 'c1', groupId: 'g1', itemType: 7, code: 'X', name: '未知类型项目', remark: null, isValid: true, itemTypeText: null },
            ]),
          },
        },
      },
    }

    const categories = await actual.queryCategories(client as never)
    const groups = await actual.queryGroups(client as never)
    const items = await actual.queryItems(client as never)

    expect(categories[0].itemType).toBeNull()
    expect(categories[0].usageStatus).toBeNull()
    expect(groups[0].usageStatus).toBeNull()
    expect(items[0].itemType).toBeNull()
  })

  it.skip('未知 ItemType 的分类应在目录树上可见（当前被静默隐藏）—— 已知缺口，待负责人裁定', () => {
    // 现状：`treeData` 按 ITEM_TYPES(0/1) 分组，且「全部」作用域同样只取 [0,1]，
    // 因此 itemType 映射为 null（未知）的分类在任何作用域下都不出现在树上，
    // 无法被查看或编辑。C65 要求「安全展示未知」，此处与实现不一致；
    // 未在未获裁定前改动页面代码，故以 skip 记录。
  })
})

describe('C68 状态过滤隐藏唯一停用下级时的类型冻结', () => {
  it('隐藏停用下级后仍判为已使用、项目类型仍置灰、名称与备注仍可编辑', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 1)],
      groups: [grp('g1', 'c1', '唯一停用分组', false)],
      items: [],
    })
    expect(screen.getByText('唯一停用分组')).toBeInTheDocument()

    // 左右两侧各有一个「显示停用」复选框（S1-D36），本用例操作左树那一个，按目录区域消歧。
    click(within(screen.getByRole('region', { name: '标准目录' })).getByRole('checkbox', { name: '显示停用' }))
    await waitFor(() => expect(screen.queryByText('唯一停用分组')).not.toBeInTheDocument())
    expect(screen.getByText('甲分类')).toBeInTheDocument()

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    expect(screen.getByText('已使用')).toBeInTheDocument()
    expect(screen.getByText('该分类下已存在分组（含已停用），项目类型不可修改。')).toBeInTheDocument()
    expect(screen.getByLabelText('分类名称')).toBeEnabled()
    expect(screen.getByLabelText('备注')).toBeEnabled()
  })
})

describe('C75 枚举文案来源：服务端随行文案 → 枚举元数据 → 本地兜底', () => {
  it('列表与编辑弹窗取服务端随行枚举文案，不按数值自拼中文', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 1, { itemTypeText: '检验（随行）', usageStatusText: '已使用（随行）' })],
      groups: [grp('g1', 'c1', '甲分组', true, 1, '已使用（随行）')],
      items: [itm('i1', 'c1', 'g1', 'A-1', '甲项目', true, '检验（随行）')],
    })

    // 列表已按 S1-D40 移除「类型」列：项目随行文案不再进入列表呈现（表头与列内容由 C84 断言）。
    expect(screen.queryByText('检验（随行）')).not.toBeInTheDocument()

    // 分组编辑弹窗的使用情况取分组自身的随行文案。
    click(screen.getByLabelText('编辑分组：甲分组'))
    await screen.findByLabelText('分组名称')
    expect(modal().getByText('已使用（随行）')).toBeInTheDocument()

    // 树节点没有随行文案：本文件占位 Client 不提供枚举元数据端点，回退到本地兜底文案。
    expect(screen.getByText('检查')).toBeInTheDocument()
    expect(screen.queryByText('未知类型')).not.toBeInTheDocument()
  })

  it('随行文案缺失时回退到与后端同名的本地兜底文案，不显示成未知', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 1)],
      groups: [grp('g1', 'c1', '甲分组', true, 0)],
      items: [itm('i1', 'c1', 'g1', 'A-1', '甲项目')],
    })

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    expect(modal().getByText('已使用')).toBeInTheDocument()
  })
})

describe('C69 写成功后部分集合刷新失败', () => {
  it('写成功即关弹窗；分类集合刷新失败标待刷新、禁用写入口、写请求总计一次、重试只读', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 0), cat('c2', '乙分类', 0, true, 1)],
      groups: [grp('g1', 'c2', '乙分组')],
      items: [itm('i1', 'c2', 'g1', 'B-1', '乙项目')],
    })

    api.createGroup.mockResolvedValueOnce(undefined)
    const after = armRead(pendingRead())

    await openCreateGroup(0)
    await change(screen.getByLabelText('分组名称'), '新分组')
    click(screen.getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(api.createGroup).toHaveBeenCalledTimes(1))
    await act(async () => {
      after.groups.resolve([grp('g1', 'c2', '乙分组'), grp('g2', 'c1', '新分组')])
      after.items.resolve([itm('i1', 'c2', 'g1', 'B-1', '乙项目')])
      after.categories.reject(new Error('分类集合读取失败'))
    })

    await waitFor(() => expect(screen.queryByLabelText('分组名称')).not.toBeInTheDocument())
    await screen.findByText('目录数据待刷新')
    expect(
      screen.getByText('部分目录读取失败，已有数据保留；依赖未就绪的维护操作暂不可用。'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeDisabled()
    expect(api.createGroup).toHaveBeenCalledTimes(1)

    const retry = armRead(pendingRead())
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(api.queryCategories).toHaveBeenCalledTimes(3))
    expect(api.createGroup).toHaveBeenCalledTimes(1)

    await settle(retry, {
      categories: [cat('c1', '甲分类', 0, true, 1), cat('c2', '乙分类', 0, true, 1)],
      groups: [grp('g1', 'c2', '乙分组'), grp('g2', 'c1', '新分组')],
      items: [itm('i1', 'c2', 'g1', 'B-1', '乙项目')],
    })

    await waitFor(() => expect(screen.queryByText('目录数据待刷新')).not.toBeInTheDocument())
    expect(screen.getByRole('button', { name: /新增项目/ })).toBeEnabled()
  })
})

describe('C57 新增空备注与编辑原值/清空的表单转换（Component 层）', () => {
  const WITH_ITEM: CatalogData = {
    categories: [cat('c1', '甲分类')],
    groups: [grp('g1', 'c1', '甲分组')],
    items: [itm('i1', 'c1', 'g1', 'A01', '血常规')],
  }

  /** 适配层被替换后写入即刻成功，页面随后固定重载一次，故先排队一次读取再提交。 */
  async function submitAndReload(button: HTMLElement, data: CatalogData = WITH_ITEM) {
    const read = armRead(pendingRead())
    click(button)
    await settle(read, data)
  }

  /** antd Select 需先 mousedown 展开，再从下拉（portal）里按文本点选项。 */
  async function pickOption(label: string, optionText: string) {
    const combo = await modal().findByLabelText(label)
    await act(async () => {
      fireEvent.mouseDown(combo)
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

  it('三类新增不填备注：入参均为显式 remark:null，并保留各自合法创建字段', async () => {
    await renderLoaded(WITH_ITEM)

    click(screen.getByLabelText('新增检验分类'))
    await change(await screen.findByLabelText('分类名称'), '甲新增分类')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createCategory).toHaveBeenCalledTimes(1))
    expect(api.createCategory.mock.calls[0][1]).toEqual({ itemType: 0, name: '甲新增分类', remark: null })

    await openCreateGroup(0)
    await change(await screen.findByLabelText('分组名称'), '甲新增分组')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createGroup).toHaveBeenCalledTimes(1))
    expect(api.createGroup.mock.calls[0][1]).toEqual({ categoryId: 'c1', name: '甲新增分组', remark: null })

    click(screen.getByRole('button', { name: /新增项目/ }))
    await pickOption('所属分组', '甲分组')
    await change(modal().getByLabelText('标准项目编码'), 'A02')
    await change(modal().getByLabelText('标准项目名称'), '尿常规')
    await submitAndReload(modal().getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.createItem).toHaveBeenCalledTimes(1))
    expect(api.createItem.mock.calls[0][1]).toEqual({ categoryId: 'c1', groupId: 'g1', code: 'A02', name: '尿常规', remark: null })
  })

  it('编辑分类：未改保留原值、主动清空为显式 null、含首尾空白原样发送，三次均发出写请求', async () => {
    await renderLoaded(WITH_ITEM)

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateCategory).toHaveBeenCalledTimes(1))
    expect(api.updateCategory.mock.calls[0][1]).toEqual({ id: 'c1', itemType: 0, name: '甲分类', remark: null })

    click(screen.getByLabelText('编辑分类：甲分类'))
    await change(await screen.findByLabelText('备注'), '  原文  ')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateCategory).toHaveBeenCalledTimes(2))
    expect(api.updateCategory.mock.calls[1][1]).toEqual({ id: 'c1', itemType: 0, name: '甲分类', remark: '  原文  ' })

    click(screen.getByLabelText('编辑分类：甲分类'))
    await change(await screen.findByLabelText('备注'), '')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateCategory).toHaveBeenCalledTimes(3))
    expect(api.updateCategory.mock.calls[2][1]).toEqual({ id: 'c1', itemType: 0, name: '甲分类', remark: null })
  })

  it('编辑分组入参不含 categoryId；项目修改备注只含目标标识与备注', async () => {
    await renderLoaded(WITH_ITEM)

    click(screen.getByLabelText('编辑分组：甲分组'))
    await screen.findByLabelText('分组名称')
    await submitAndReload(screen.getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateGroup).toHaveBeenCalledTimes(1))
    const groupInput = api.updateGroup.mock.calls[0][1] as Record<string, unknown>
    expect(groupInput).toEqual({ id: 'g1', name: '甲分组', remark: null })
    expect('categoryId' in groupInput).toBe(false)

    click(within(rowOf('血常规')).getByRole('button', { name: '修改备注' }))
    await screen.findByLabelText('备注')
    await submitAndReload(modal().getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateItemRemark).toHaveBeenCalledTimes(1))
    const itemInput = api.updateItemRemark.mock.calls[0][1] as Record<string, unknown>
    expect(itemInput).toEqual({ id: 'i1', remark: null })
    expect(Object.keys(itemInput)).toEqual(['id', 'remark'])
  })
})

/** 悬停触发元素后断言某条 Tooltip 展开了指定全文（被截断文本按 S1-D38 延迟 0.5 秒出现）。 */
async function expectTooltipShows(trigger: HTMLElement, fullText: string) {
  fireEvent.mouseOver(trigger)
  await waitFor(() => {
    // antd 6 的弹层结构：title 直接位于 role="tooltip" 的容器内，不再有 .ant-tooltip-inner。
    const overlays = document.querySelectorAll('[role="tooltip"]')
    expect(Array.from(overlays).some((node) => node.textContent === fullText)).toBe(true)
  }, { timeout: 3000 })
}

describe('C76 目录树节点固定槽位与超长截断（Component 层可覆盖部分）', () => {
  const LONG_CATEGORY = '超长分类名称'.repeat(24)
  const LONG_GROUP = '超长分组名称'.repeat(24)
  const LONG_TREE: CatalogData = {
    categories: [cat('c1', LONG_CATEGORY)],
    groups: [grp('g1', 'c1', LONG_GROUP)],
    items: [],
  }

  it('名称按宽度截断的元素悬停经 Tooltip 展示全名', async () => {
    await renderLoaded(LONG_TREE)
    const tree = screen.getByLabelText('分类分组目录树')
    for (const fullName of [LONG_CATEGORY, LONG_GROUP]) {
      const nameEl = within(tree).getByText(fullName)
      expect(nameEl.className).toContain('standard-catalog-node-name')
      await expectTooltipShows(nameEl, fullName)
    }
  })

  it('状态标记与操作图标位于最右固定槽位，名称元素不携带它们', async () => {
    await renderLoaded(LONG_TREE)
    const tree = screen.getByLabelText('分类分组目录树')
    for (const fullName of [LONG_CATEGORY, LONG_GROUP]) {
      const nameEl = within(tree).getByText(fullName)
      const row = nameEl.closest('.standard-catalog-node')
      expect(row).not.toBeNull()
      const side = row!.querySelector('.standard-catalog-node-side')
      expect(side).not.toBeNull()
      expect(side!.querySelector('.ant-tag')).not.toBeNull()
      expect(side!.querySelector('.standard-catalog-node-actions')).not.toBeNull()
      expect(nameEl.querySelector('.ant-tag')).toBeNull()
      expect(nameEl.querySelector('.standard-catalog-node-actions')).toBeNull()
    }
    // 树滚动区挂点存在：行内不换行、无横向滚动与高度齐平由 StandardCatalog.css 承担，宿主验收核对。
    expect(document.querySelector('.standard-catalog-tree-scroll')).not.toBeNull()
  })

  it('样式静态断言：槽位操作按钮尺寸规则叠加 antd 图标按钮类，特异性高于 antd 自身规则', () => {
    // CSS 在 jsdom 中不参与计算，按静态证据断言规则本身（Testing Baseline §1.4）。
    // 宿主实测：`.standard-catalog-node-side .ant-btn` 与 antd 的 `:where(.css-…).ant-btn-sm.ant-btn-icon-only`
    // 在 micro-app 作用域前缀下特异性相同，antd 运行时注入的样式在 source order 上取胜，
    // 只赢下 height、width 落回 24px；故尺寸规则必须同时带上 .ant-btn 与 .ant-btn-icon-only 两个类。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/standardCatalog/StandardCatalog.css'), 'utf8')
    const buttonRule = css.match(/\.standard-catalog-node-side\s+\.ant-btn\.ant-btn-icon-only\s*\{[^}]*\}/)?.[0] ?? ''
    expect(buttonRule).toContain('width: 28px')
    expect(buttonRule).toContain('height: 28px')
  })
})

describe('C77 树节点状态标记全显示', () => {
  it('启用与停用节点均显示状态标记，标记与操作图标同在最右槽位，使用情况不上树', async () => {
    await renderLoaded({
      categories: [cat('c-on', '启用分类', 0, true, 1), cat('c-off', '停用分类', 0, false, 1)],
      groups: [grp('g-on', 'c-on', '启用分组', true, 1), grp('g-off', 'c-on', '停用分组', false, 1)],
      items: [],
    })
    const tree = screen.getByLabelText('分类分组目录树')

    const enabledTexts = within(tree).getAllByText('启用')
    const disabledTexts = within(tree).getAllByText('停用')
    expect(enabledTexts.length).toBeGreaterThanOrEqual(1)
    expect(disabledTexts.length).toBeGreaterThanOrEqual(1)
    for (const text of [...enabledTexts, ...disabledTexts]) {
      expect(text.closest('.ant-tag')).not.toBeNull()
      const side = text.closest('.standard-catalog-node-side')
      expect(side).not.toBeNull()
      expect(side!.querySelector('.standard-catalog-node-actions')).not.toBeNull()
    }
    for (const text of enabledTexts) {
      expect((text.closest('.ant-tag') as HTMLElement).className).toContain('ant-tag-success')
    }
    // 使用情况仍在编辑弹窗口径（S1-D21），不在节点展示。
    expect(within(tree).queryByText('已使用')).not.toBeInTheDocument()
    expect(within(tree).queryByText('未使用')).not.toBeInTheDocument()
  })
})

describe('C78 列表超长列统一单行省略（Component 层可覆盖部分）', () => {
  const LONG_CODE = 'CODE'.repeat(40)
  const LONG_NAME = '项目名称'.repeat(40)
  const LONG_CATEGORY = '分类名称'.repeat(40)
  const LONG_GROUP = '分组名称'.repeat(40)
  const LONG_REMARK = '备注内容'.repeat(40)
  const LONG_ITEMS: CatalogData = {
    categories: [cat('c1', LONG_CATEGORY)],
    groups: [grp('g1', 'c1', LONG_GROUP)],
    items: [{ ...itm('i1', 'c1', 'g1', LONG_CODE, LONG_NAME), remark: LONG_REMARK }],
  }
  const FIVE_COLUMNS = [LONG_CODE, LONG_NAME, LONG_CATEGORY, LONG_GROUP, LONG_REMARK]

  /** 分类名与分组名同时出现在树里：定位取该文本且落在表格单元格内的元素。 */
  function cellText(text: string): HTMLElement {
    const cell = screen.getAllByText(text).map((el) => el.closest('td')).find((td) => td !== null)
    expect(cell).not.toBeNull()
    return cell as HTMLElement
  }

  it('编码、名称、分类、分组、备注五列单元格挂单行省略样式类', async () => {
    await renderLoaded(LONG_ITEMS)
    for (const text of FIVE_COLUMNS) {
      const cell = cellText(text)
      expect(cell.className).toContain('standard-catalog-cell-ellipsis')
      const span = cell.querySelector('.standard-catalog-cell-text')
      expect(span).not.toBeNull()
      expect(span!.textContent).toBe(text)
    }
  })

  it('五列悬停经 Tooltip 展示完整值', async () => {
    await renderLoaded(LONG_ITEMS)
    for (const text of FIVE_COLUMNS) {
      const span = cellText(text).querySelector('.standard-catalog-cell-text')
      expect(span).not.toBeNull()
      await expectTooltipShows(span as HTMLElement, text)
    }
  })
})

describe('C79 结果汇总条位置', () => {
  const DATA: CatalogData = {
    categories: [cat('c1', '甲分类')],
    groups: [grp('g1', 'c1', '甲分组')],
    items: [itm('i1', 'c1', 'g1', 'A-1', '血常规'), itm('i2', 'c1', 'g1', 'A-2', '尿常规')],
  }

  it('范围与计数在列表区域底部汇总条，标题行只保留标题', async () => {
    await renderLoaded(DATA)

    const header = document.querySelector('.standard-catalog-list-header')
    expect(header).not.toBeNull()
    expect(header!.textContent).toContain('标准项目列表')
    expect(header!.textContent).not.toContain('范围：')

    const card = document.querySelector('.standard-catalog-list-card')
    expect(card).not.toBeNull()
    const summary = card!.querySelector('.standard-catalog-list-summary')
    expect(summary).not.toBeNull()
    expect(summary!.className).toContain('standard-catalog-list-summary')
    expect(summary!.textContent).toContain('范围：')
    expect(summary!.textContent).toContain('共 2 项')
    // 汇总条固定在列表卡片底部（右下角样式由 StandardCatalog.css 承担）。
    expect(card!.lastElementChild).toBe(summary)
  })

  it('行数变化与空态时汇总条位置不变、计数为 0', async () => {
    await renderLoaded(DATA)
    const card = document.querySelector('.standard-catalog-list-card')
    expect(card).not.toBeNull()

    await change(screen.getByLabelText('编码或名称'), 'A-1')
    const one = card!.querySelector('.standard-catalog-list-summary')
    expect(one).not.toBeNull()
    expect(one!.textContent).toContain('共 1 项')
    expect(card!.lastElementChild).toBe(one)

    await change(screen.getByLabelText('编码或名称'), 'ZZZ-NO-MATCH')
    const zero = card!.querySelector('.standard-catalog-list-summary')
    expect(zero).not.toBeNull()
    expect(zero!.textContent).toContain('共 0 项')
    expect(card!.lastElementChild).toBe(zero)
    expect(screen.getByText('没有符合条件的标准项目')).toBeInTheDocument()
  })
})

/** 列表区域与目录区域的作用域查询：两侧各有一个「显示停用」复选框，按 region 消歧。 */
const listRegion = () => screen.getByRole('region', { name: '标准项目列表' })
const directoryRegion = () => screen.getByRole('region', { name: '标准目录' })
const listShowDisabled = () => within(listRegion()).getByRole('checkbox', { name: '显示停用' })
const directoryShowDisabled = () => within(directoryRegion()).getByRole('checkbox', { name: '显示停用' })
const keywordInput = () => within(listRegion()).getByLabelText('编码或名称')

describe('C80 列表筛选条整改（Component 层可覆盖部分）', () => {
  const DATA: CatalogData = {
    categories: [cat('c1', '甲分类'), cat('c2', '停用分类', 0, false)],
    groups: [grp('g1', 'c1', '甲分组'), grp('g2', 'c2', '停用分类下分组')],
    items: [
      itm('i1', 'c1', 'g1', 'A01', '血常规', true),
      itm('i2', 'c1', 'g1', 'B02', '尿常规', false),
      itm('i3', 'c2', 'g2', 'C03', '父停用项目', true),
    ],
  }

  it('单一关键字框「编码或名称」同时按编码与名称字面匹配，且不发筛选请求', async () => {
    await renderLoaded(DATA)
    const reads = api.queryItems.mock.calls.length

    expect(within(listRegion()).getByLabelText('编码或名称')).toBeInTheDocument()
    // 旧的编码、名称双输入与状态 Segmented 不复存在（S1-D36）。
    expect(within(listRegion()).queryByLabelText('标准项目名称')).not.toBeInTheDocument()
    expect(within(listRegion()).queryByLabelText('标准项目自身状态')).not.toBeInTheDocument()

    // 编码命中：大小写不敏感的字面包含。
    await change(keywordInput(), 'b02')
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(screen.queryByText('父停用项目')).not.toBeInTheDocument()

    // 名称命中：同一个关键字框按 Name 再匹配。
    await change(keywordInput(), '血')
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.queryByText('尿常规')).not.toBeInTheDocument()

    // 全程本地过滤，不发起任何读取。
    expect(api.queryItems.mock.calls.length).toBe(reads)
    expect(api.queryCategories.mock.calls.length).toBe(1)
    expect(api.queryGroups.mock.calls.length).toBe(1)
  })

  it('「显示停用」默认勾选；不勾仅显示自身启用项目，并与左树复选框相互独立', async () => {
    await renderLoaded(DATA)
    expect(listShowDisabled()).toBeChecked()
    // 默认显示全部：停用项目与父级停用但自身启用的项目都在列。
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    expect(screen.getByText('父停用项目')).toBeInTheDocument()
    const reads = api.queryItems.mock.calls.length

    await act(async () => { fireEvent.click(listShowDisabled()) })
    expect(listShowDisabled()).not.toBeChecked()
    // 仅按项目自身 IsValid：自身停用隐藏，自身启用即使父级停用仍显示（S1-D36）。
    expect(screen.queryByText('尿常规')).not.toBeInTheDocument()
    expect(screen.getByText('父停用项目')).toBeInTheDocument()
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(api.queryItems.mock.calls.length).toBe(reads)

    // 左树「显示停用」不受右侧复选框影响。
    expect(directoryShowDisabled()).toBeChecked()

    await act(async () => { fireEvent.click(listShowDisabled()) })
    expect(listShowDisabled()).toBeChecked()
    expect(screen.getByText('尿常规')).toBeInTheDocument()
  })

  it('重置为 text 小号文字按钮：清空关键字、恢复勾选，不动左侧任何状态', async () => {
    await renderLoaded(DATA)
    const reads = api.queryItems.mock.calls.length

    // 左侧先改变一个状态，验证重置不触碰它。
    await act(async () => { fireEvent.click(directoryShowDisabled()) })
    expect(directoryShowDisabled()).not.toBeChecked()

    await change(keywordInput(), '尿')
    await act(async () => { fireEvent.click(listShowDisabled()) })
    expect(keywordInput()).toHaveValue('尿')

    const reset = within(listRegion()).getByRole('button', { name: '重置列表筛选' })
    // text 类型小号文字按钮，且紧跟条件组（同一 filters 容器内）。
    expect(reset).toHaveClass('ant-btn-variant-text')
    expect(reset).toHaveClass('ant-btn-sm')
    expect(reset.closest('.standard-catalog-filters')).not.toBeNull()
    expect(keywordInput().closest('.standard-catalog-filters')).not.toBeNull()
    // 条件组内元素顺序：关键字在前、显示停用居中、重置紧跟其后。
    const filters = reset.closest('.standard-catalog-filters') as HTMLElement
    expect(Array.from(filters.children).indexOf(keywordInput().closest('.ant-input-affix-wrapper') as HTMLElement)).toBeLessThan(Array.from(filters.children).indexOf(reset))

    await click(reset)
    expect(keywordInput()).toHaveValue('')
    expect(listShowDisabled()).toBeChecked()
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('尿常规')).toBeInTheDocument()
    // 左侧状态保持重置前的样子，且重置不发任何请求。
    expect(directoryShowDisabled()).not.toBeChecked()
    expect(api.queryItems.mock.calls.length).toBe(reads)
    expect(api.queryCategories.mock.calls.length).toBe(1)
  })
})

describe('C81 列表固定高度与边框（Component 层结构 + 样式静态断言）', () => {
  const DATA: CatalogData = {
    categories: [cat('c1', '甲分类')],
    groups: [grp('g1', 'c1', '甲分组')],
    items: [itm('i1', 'c1', 'g1', 'A-1', '血常规')],
  }

  it('列表卡片包住表格与汇总条，表体为带 maxHeight 的内部滚动容器', async () => {
    await renderLoaded(DATA)
    const card = document.querySelector('.standard-catalog-list-card')
    expect(card).not.toBeNull()
    // 固定表头形态下 rc-table 会渲染表头、表体两张 table，这里按「卡片内存在表格」断言。
    expect(within(card as HTMLElement).getAllByRole('table').length).toBeGreaterThan(0)
    expect(card!.querySelector('.standard-catalog-list-summary')).not.toBeNull()
    expect(card!.lastElementChild).toHaveClass('standard-catalog-list-summary')

    // scroll.y 生效后 rc-table 才会渲染独立的 .ant-table-body 滚动容器（S1-D37）。
    const body = card!.querySelector('.ant-table-body')
    expect(body).not.toBeNull()
    const style = body!.getAttribute('style') ?? ''
    expect(style).toContain('max-height')
    expect(style).toContain('var(--standard-catalog-table-body-y)')
  })

  it('样式静态断言：卡片固定高度 + 外边框、树滚动区同一高度表达式、筛选组左对齐、表体滚动条溢出才显示', () => {
    // CSS 在 jsdom 中不参与计算，按静态证据断言规则本身（Testing Baseline §1.4）。
    // vitest 以子应用根目录为工作目录，源码路径相对它解析。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/standardCatalog/StandardCatalog.css'), 'utf8')
    const cardRule = css.match(/\.standard-catalog-list-card\s*\{[^}]*\}/)?.[0] ?? ''
    expect(cardRule).toContain('border: 1px solid')
    expect(cardRule).toContain('height: var(--standard-catalog-panel-height)')
    const treeRule = css.match(/\.standard-catalog-tree-scroll\s*\{[^}]*\}/)?.[0] ?? ''
    expect(treeRule).toContain('height: var(--standard-catalog-panel-height)')
    const filtersRule = css.match(/\.standard-catalog-filters\s*\{[^}]*\}/)?.[0] ?? ''
    expect(filtersRule).toContain('justify-content: flex-start')
    // S1-D41：antd 表体在 scroll.y 下内联 overflow-y: scroll 恒显滚动条，须覆盖为 auto（仅溢出显示）。
    const bodyRule = css.match(/\.standard-catalog-list\s+\.ant-table-body\s*\{[^}]*\}/)?.[0] ?? ''
    expect(bodyRule).toContain('overflow-y: auto')
  })
})

describe('C82 树悬停气泡条件显示（Component 层可覆盖部分）', () => {
  const LONG_NAME = '超长悬停名称'.repeat(30)

  it('完整可见的名称悬停不出现气泡', async () => {
    await renderLoaded({ categories: [cat('c1', '短名')], groups: [grp('g1', 'c1', '短分组')], items: [] })
    const nameEl = within(screen.getByLabelText('分类分组目录树')).getByText('短名')
    fireEvent.mouseOver(nameEl)
    // 等待窗口覆盖 mouseEnterDelay（0.5s）之后仍不得出现气泡；负向断言需要一个有界等待。
    await new Promise((resolve) => setTimeout(resolve, 800))
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
  })

  it('被截断的名称悬停约 0.5 秒后才出现气泡并展示全文', async () => {
    await renderLoaded({ categories: [cat('c1', LONG_NAME)], groups: [], items: [] })
    const nameEl = within(screen.getByLabelText('分类分组目录树')).getByText(LONG_NAME)
    fireEvent.mouseOver(nameEl)
    // 延迟未到（0.5 秒）时气泡尚未出现：默认 0.1 秒延迟的实现在此刻已弹出，判红。
    await new Promise((resolve) => setTimeout(resolve, 200))
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
    await waitFor(() => {
      const overlays = Array.from(document.querySelectorAll('[role="tooltip"]'))
      expect(overlays.some((node) => node.textContent === LONG_NAME)).toBe(true)
    }, { timeout: 3000 })
  })
})

describe('C83 状态合并列（Component 层可覆盖部分）', () => {
  const DATA: CatalogData = {
    categories: [cat('c-ok', '启用分类'), cat('c-off', '停用分类', 0, false)],
    groups: [grp('g-ok', 'c-ok', '启用分组'), grp('g-off', 'c-off', '停用分类下分组')],
    items: [
      itm('i-ok', 'c-ok', 'g-ok', 'E01', '全链路启用项目', true),
      itm('i-bad', 'c-off', 'g-off', 'E02', '上级停用项目', true),
      itm('i-off', 'c-ok', 'g-ok', 'E03', '自身停用项目', false),
      itm('i-unknown', 'c-ok', 'g-ok', 'E04', '状态未知项目', null),
    ],
  }

  it('列头为编码、名称、分类、分组、状态、备注、操作，不再有「当前有效」列', async () => {
    await renderLoaded(DATA)
    // 固定表头形态下表头与表体是两张 table，统一在列表区域作用域内断言；
    // 表头另有 antd 的隐藏无障碍副本，按 selector: 'th' 只取真实表头单元格。
    const scope = within(listRegion())
    for (const header of ['编码', '名称', '分类', '分组', '状态', '备注', '操作']) {
      expect(scope.getByText(header, { selector: 'th' })).toBeInTheDocument()
    }
    expect(scope.queryByText('当前有效')).not.toBeInTheDocument()
    expect(scope.queryByText('类型')).not.toBeInTheDocument()
  })

  it('全链路启用显示绿色「有效」；上级停用显示「无效」并悬停给原因；状态不完整显示「未知」', async () => {
    await renderLoaded(DATA)
    // 合并列以链路派生呈现：表体不再出现项目自身启停标签（S1-D39）。
    const scope = within(listRegion())
    expect(scope.queryByText('启用')).not.toBeInTheDocument()
    expect(scope.queryByText('停用')).not.toBeInTheDocument()

    const okRow = screen.getByText('全链路启用项目').closest('tr') as HTMLElement
    const okTag = within(okRow).getByText('有效')
    expect(okTag).toHaveClass('ant-tag-success')

    const badRow = screen.getByText('上级停用项目').closest('tr') as HTMLElement
    expect(within(badRow).getByText('无效')).toBeInTheDocument()
    const reason = within(badRow).getByLabelText('所属分类已停用')
    fireEvent.mouseOver(reason)
    await waitFor(() => {
      const overlays = Array.from(document.querySelectorAll('[role="tooltip"]'))
      expect(overlays.some((node) => node.textContent === '所属分类已停用')).toBe(true)
    }, { timeout: 3000 })

    const selfOffRow = screen.getByText('自身停用项目').closest('tr') as HTMLElement
    expect(within(selfOffRow).getByText('无效')).toBeInTheDocument()
    expect(within(selfOffRow).getByLabelText('标准项目自身已停用')).toBeInTheDocument()

    const unknownRow = screen.getByText('状态未知项目').closest('tr') as HTMLElement
    expect(within(unknownRow).getByText('未知')).toBeInTheDocument()
  })
})

describe('C84 类型列移除（Component 层可覆盖部分）', () => {
  it('列表无「类型」列，类型信息由树范围与分类列表达', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 1, { itemTypeText: '检验（随行）' })],
      groups: [grp('g1', 'c1', '甲分组')],
      items: [itm('i1', 'c1', 'g1', 'A-1', '甲项目', true, '检验（随行）')],
    })

    const table = within(listRegion())
    expect(table.queryByText('类型')).not.toBeInTheDocument()
    expect(table.queryByText('检验（随行）')).not.toBeInTheDocument()

    // 类型能力不受影响：树类型根、类型单选与范围文案仍在表达类型。
    expect(within(screen.getByLabelText('分类分组目录树')).getByText('检验')).toBeInTheDocument()
    expect(screen.getByRole('radiogroup', { name: '目录项目类型' })).toBeInTheDocument()
    expect(screen.getByText(/^范围：/).textContent).toContain('检验')
  })
})

/**
 * 操作列统一形态（阶段操作列口径）：`type="link" size="small"` 文字按钮横排；修改备注为
 * 高频主操作直接横排，停用/启用低频且具破坏性面收进「更多」Dropdown；停用为 danger 且
 * 既有二次确认弹窗（ToggleModal，停用时确认按钮 danger）保留、确认后行为不变。
 */
describe('操作列 link 文字按钮与「更多」下拉', () => {
  const DATA: CatalogData = {
    categories: [cat('c1', '甲分类')],
    groups: [grp('g1', 'c1', '甲分组')],
    items: [
      itm('i-on', 'c1', 'g1', 'A-1', '启用样例项目', true),
      itm('i-off', 'c1', 'g1', 'A-2', '停用样例项目', false),
    ],
  }

  it('操作列为 link 小号文字按钮：主操作「修改备注」直接可见，不再有纯图标按钮', async () => {
    await renderLoaded(DATA)
    const row = rowOf('启用样例项目')
    const remark = within(row).getByRole('button', { name: '修改备注' })
    expect(remark).toHaveClass('ant-btn-variant-link')
    expect(remark).toHaveClass('ant-btn-sm')
    // 旧形态的纯图标按钮（aria-label 形如「修改备注：xxx」「xx项目：xxx」）不复存在。
    expect(within(row).queryByLabelText(/修改备注：/)).not.toBeInTheDocument()
    expect(within(row).queryByLabelText(/项目：/)).not.toBeInTheDocument()
    // 行内只保留「修改备注」与「更多」两个文字按钮，停用/启用不再横排图标按钮。
    expect(within(row).getAllByRole('button').map((button) => button.textContent)).toEqual(['修改备注', '更多'])
  })

  it('启用行的次级操作收进「更多」下拉：停用项为 danger', async () => {
    await renderLoaded(DATA)

    click(within(rowOf('启用样例项目')).getByRole('button', { name: '更多' }))
    const stop = await screen.findByRole('menuitem', { name: '停用' })
    expect(stop).toHaveClass('ant-dropdown-menu-item-danger')
  })

  it('停用行的次级操作收进「更多」下拉：启用项非 danger', async () => {
    await renderLoaded(DATA)

    click(within(rowOf('停用样例项目')).getByRole('button', { name: '更多' }))
    const enable = await screen.findByRole('menuitem', { name: '启用' })
    expect(enable).not.toHaveClass('ant-dropdown-menu-item-danger')
  })

  it('停用仍弹既有二次确认：danger 确认按钮，确认后按原口径调用停用接口并刷新', async () => {
    await renderLoaded(DATA)
    api.setItemEnabled.mockResolvedValueOnce(undefined)
    const reload = armRead(pendingRead())

    click(within(rowOf('启用样例项目')).getByRole('button', { name: '更多' }))
    click(await screen.findByRole('menuitem', { name: '停用' }))

    // 二次确认仍由既有 ToggleModal 承担：确认按钮 danger，正文与文案不变。
    const dialog = within(screen.getByRole('dialog'))
    expect(dialog.getByText('停用标准项目')).toBeInTheDocument()
    const confirm = dialog.getByRole('button', { name: /确认停用/ })
    expect(confirm).toHaveClass('ant-btn-dangerous')

    click(confirm)
    await waitFor(() => expect(api.setItemEnabled).toHaveBeenCalledTimes(1))
    expect(api.setItemEnabled.mock.calls[0].slice(1)).toEqual(['i-on', false])
    await settle(reload, DATA)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

/**
 * 新增项目弹窗的预选口径（缺陷修复）：
 * 1. 左侧选到分组时，弹窗同时预选该分组及其分类（此前只预选分类、分组留空）；
 * 2. 从类型根打开时，预选分类取**当前类型**下第一个启用分类（此前取全局第一个启用分类，
 *    检查范围下会预选到检验分类）；「全部」范围仍取全局第一个启用分类。
 * 切换分类清空分组、停用分类/分组禁选的既有口径不变（Client/design.md「上级有效性提示」行）。
 */
describe('新增项目弹窗按左侧范围与类型预选', () => {
  const DATA: CatalogData = {
    categories: [
      // 全局第一个启用分类刻意放在检验类型下：类型根预选若不按类型过滤会落到它身上。
      cat('c-first0', '甲类型分类', 0, true),
      cat('c-first1', '乙类型分类', 1, true),
      cat('c-scope', '范围分类', 0, true),
    ],
    groups: [grp('g-scope', 'c-scope', '范围分组'), grp('g1', 'c-first1', '乙类型分组')],
    items: [],
  }

  /** 读弹窗内某个下拉的已选标题（antd 6 选中值挂在 .ant-select-content 的 title 上）。 */
  function selectTitle(label: string): string | null {
    const input = modal().getByLabelText(label) as HTMLInputElement
    return input.closest('.ant-select')?.querySelector('.ant-select-content')?.getAttribute('title') ?? null
  }

  async function openCreateItem() {
    click(screen.getByRole('button', { name: /新增项目/ }))
    await screen.findByLabelText('所属分类')
  }

  async function closeCreateItem() {
    click(modal().getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  }

  it('左侧选到分组时，弹窗预选该分组及其分类', async () => {
    await renderLoaded(DATA)
    click(within(screen.getByLabelText('分类分组目录树')).getByText('范围分组'))
    await waitFor(() => expect(screen.getByText(/^范围：/).textContent).toContain('范围分类'))

    await openCreateItem()
    expect(selectTitle('所属分类')).toBe('检验 · 范围分类')
    expect(selectTitle('所属分组')).toBe('范围分组')
    await closeCreateItem()
  })

  it('类型根打开时预选当前类型下第一个启用分类；全部范围退回全局第一个', async () => {
    await renderLoaded(DATA)
    click(screen.getByRole('radio', { name: '检查' }))
    await waitFor(() => expect(screen.getByText(/^范围：/).textContent).toContain('检查'))

    await openCreateItem()
    // 检查范围：应预选检查类型的启用分类，而不是全局第一个（检验的甲类型分类）。
    expect(selectTitle('所属分类')).toBe('检查 · 乙类型分类')
    expect(selectTitle('所属分组')).toBe('')
    await closeCreateItem()

    click(screen.getByRole('radio', { name: '全部' }))
    await waitFor(() => expect(screen.getByText(/^范围：/).textContent).toContain('全部'))
    await openCreateItem()
    expect(selectTitle('所属分类')).toBe('检验 · 甲类型分类')
    await closeCreateItem()
  })
})

/**
 * 可选提示的补充口径：所选分类存在分组但全部停用时，「所属分组」给出明确提示，
 * 区分「该分类下暂无分组」与「有分组但没有启用的」两种阻塞原因；
 * 有启用分组时不出现该提示（不改变停用分组禁选的既定行为）。
 */
describe('新增项目弹窗分组可用性提示', () => {
  const DATA: CatalogData = {
    categories: [cat('c-off-only', '仅停用分组分类', 0, true), cat('c-ok', '有启用分组分类', 0, true)],
    groups: [grp('g-off', 'c-off-only', '唯一停用分组', false), grp('g-on', 'c-ok', '启用分组', true)],
    items: [],
  }

  it('所选分类只有停用分组时给出提示；切到有启用分组的分类后提示消失', async () => {
    await renderLoaded(DATA)
    click(screen.getByRole('button', { name: /新增项目/ }))
    await screen.findByLabelText('所属分类')
    // 默认范围下预选全局第一个启用分类（仅停用分组分类）：提示应出现。
    expect(modal().getByText(/该分类下没有启用状态的分组/)).toBeInTheDocument()

    // 切换到有启用分组的分类：提示消失，且分组可选（既有「切换分类清空分组」口径不变）。
    const combo = modal().getByLabelText('所属分类')
    await act(async () => { fireEvent.mouseDown(combo) })
    const option = await waitFor(() => {
      const found = Array.from(document.querySelectorAll('.ant-select-item-option')).find((node) => (node.textContent ?? '').includes('有启用分组分类'))
      if (!found) throw new Error('分类选项未展开')
      return found as HTMLElement
    })
    click(option)
    await waitFor(() => expect(modal().queryByText(/该分类下没有启用状态的分组/)).not.toBeInTheDocument())
    expect(modal().queryByText(/该分类下暂无分组/)).not.toBeInTheDocument()

    click(modal().getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })
})

/**
 * 审查问题 R2-01：截断状态必须随文本复测。量宽只在元素挂接与 ResizeObserver 通知时执行，
 * 同一挂载实例内把分类改名变长时容器宽度不变、RO 不触发——若 text 变化不复测，
 * 状态停留在旧文本的结果，新文本真截断却不挂 Tooltip（悬停全文缺失）。
 */
describe('审查 R2-01：TruncatedText 随文本复测', () => {
  it('同一挂载实例内分类改长名后重新量宽并挂 Tooltip', async () => {
    const LONG = '改名后超长分类名称'.repeat(20)
    await renderLoaded({ categories: [cat('c1', '短名')], groups: [], items: [] })

    // 模拟「编辑分类改名 + 写后重载」：同一组件实例、同一 DOM 节点上文本变长、容器宽度不变。
    api.queryCategories.mockResolvedValueOnce([cat('c1', LONG)])
    api.queryGroups.mockResolvedValueOnce([])
    api.queryItems.mockResolvedValueOnce([])
    click(screen.getByLabelText('重新读取目录'))
    const tree = screen.getByLabelText('分类分组目录树')
    await waitFor(() => expect(within(tree).getByText(LONG)).toBeInTheDocument())

    const nameEl = within(tree).getByText(LONG)
    expect(nameEl.className).toContain('standard-catalog-node-name')
    await expectTooltipShows(nameEl, LONG)
  })
})

/**
 * 审查问题 R2-18 的守卫（即时通过，非 RED）：jsdom 无布局引擎，宽度替身按类名识别元素；
 * 类名若被改动，替身会静默失效（量宽恒为 0 → 永不截断，Tooltip 用例全体空转）。
 * 这条断言让失效在第一时间报错，而不是悄悄退化。
 */
describe('审查 R2-18：宽度替身生效守卫', () => {
  it('截断类元素能量得非零宽度', () => {
    for (const className of ['standard-catalog-node-name', 'standard-catalog-cell-text']) {
      const el = document.createElement('span')
      el.className = className
      el.textContent = '守卫'
      document.body.append(el)
      expect(el.clientWidth).toBeGreaterThan(0)
      expect(el.scrollWidth).toBeGreaterThan(0)
      el.remove()
    }
  })
})

describe('TC-T02-14 页面标题层级', () => {
  it('页面大标题为 level 3', async () => {
    await renderLoaded({ categories: [cat('c-1', '启用分类')], groups: [], items: [] })

    expect(screen.getByRole('heading', { name: '标准项目目录维护', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '标准项目目录维护', level: 2 })).toBeNull()
  })

  it('标题 margin 归零：配套 CSS 的归零选择器随标题层级改为 h3', () => {
    // jsdom 无布局：margin 归零按配套 CSS 的静态规则断言（Testing Baseline §1.4 静态证据）。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/standardCatalog/StandardCatalog.css'), 'utf8')
    const rule = css.match(/\.standard-catalog-header\s+h3\.ant-typography[^{]*\{[^}]*\}/)?.[0] ?? ''
    expect(rule).toContain('margin: 0')
    expect(css).not.toContain('h2.ant-typography')
  })
})
