/**
 * 阶段 1 前端 Component 层用例（测试矩阵 C57 / C58 / C62 / C63 / C64 / C65 / C68 / C69）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `./standardCatalogApi` 只替换 12 个 I/O 函数（读取与写入），其余纯函数
 *    （`visibleCatalogCategories` / `resolveTreeScope` / `filterItems` / `isItemEffective` /
 *     `ineffectiveReasons` / `toRemark` / 常量与类型）**使用真实实现**；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client —— 页面把 client 原样传给上面的
 *    适配层函数，故不构造真实鉴权适配器；被测对象仍是页面真实调用链。
 *
 * 仍属 Host 层、不在本文件覆盖：C66 / C67 / C31 多视口，以及 C62 与 C64 的 Host 分支。
 * 两处 `it.skip` 记录的是**已知且未获裁定不改代码**的缺口，原因写在各自用例内。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
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
): StandardCategory => ({ id, itemType, name, remark: null, isValid, usageStatus })

const grp = (
  id: string,
  categoryId: string,
  name: string,
  isValid = true,
  usageStatus: 0 | 1 = 0,
): StandardGroup => ({ id, categoryId, name, remark: null, isValid, usageStatus })

const itm = (
  id: string,
  categoryId: string,
  groupId: string,
  code: string,
  name: string,
  isValid = true,
): StandardItem => ({ id, categoryId, groupId, itemType: 0, code, name, remark: null, isValid })

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

    await change(screen.getByLabelText('标准项目编码'), 'AB')
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
    await renderLoaded({ categories: [cat('c1', '甲分类')], groups: [grp('g1', 'c1', '甲分组')], items: [itm('i1', 'c1', 'g1', 'A-1', '甲项目')] })
    api.updateItemRemark.mockRejectedValueOnce(new Error('身份已失效'))
    const readsBefore = api.queryCategories.mock.calls.length

    click(screen.getByLabelText('修改备注：甲项目'))
    await modal().findByLabelText('备注')
    await change(modal().getByLabelText('备注'), '保留的备注')
    click(modal().getByRole('button', { name: /保\s*存/ }))

    await waitFor(() => expect(api.updateItemRemark).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(screen.getByRole('dialog')).toBeInTheDocument())
    expect((modal().getByLabelText('备注') as HTMLTextAreaElement).value).toBe('保留的备注')
    expect(api.queryCategories.mock.calls.length).toBe(readsBefore)
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

  it('未知使用情况到达页面时显示未知，不默认映射为未被使用', async () => {
    await renderLoaded({
      categories: [{ id: 'c1', itemType: 0, name: '甲分类', remark: null, isValid: true, usageStatus: null }],
      groups: [{ id: 'g1', categoryId: 'c1', name: '甲分组', remark: null, isValid: true, usageStatus: null }],
      items: [],
    })

    click(screen.getByLabelText('编辑分组：甲分组'))
    await screen.findByLabelText('分组名称')
    expect(screen.getByText('未知')).toBeInTheDocument()
    expect(screen.queryByText('未被使用')).not.toBeInTheDocument()
    expect(screen.queryByText('已被下级使用')).not.toBeInTheDocument()
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
              { itemId: 'i1', categoryId: 'c1', groupId: 'g1', itemType: 7, code: 'X', name: '未知类型项目', remark: null, isValid: true },
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
  it('隐藏停用下级后仍判为已被下级使用、项目类型仍置灰、名称与备注仍可编辑', async () => {
    await renderLoaded({
      categories: [cat('c1', '甲分类', 0, true, 1)],
      groups: [grp('g1', 'c1', '唯一停用分组', false)],
      items: [],
    })
    expect(screen.getByText('唯一停用分组')).toBeInTheDocument()

    click(screen.getByRole('checkbox', { name: '显示停用' }))
    await waitFor(() => expect(screen.queryByText('唯一停用分组')).not.toBeInTheDocument())
    expect(screen.getByText('甲分类')).toBeInTheDocument()

    click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    expect(screen.getByText('已被下级使用')).toBeInTheDocument()
    expect(screen.getByText('该分类下已存在分组（含已停用），项目类型不可修改。')).toBeInTheDocument()
    expect(screen.getByLabelText('分类名称')).toBeEnabled()
    expect(screen.getByLabelText('备注')).toBeEnabled()
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

    click(screen.getByLabelText('修改备注：血常规'))
    await screen.findByLabelText('备注')
    await submitAndReload(modal().getByRole('button', { name: /保\s*存/ }))
    await waitFor(() => expect(api.updateItemRemark).toHaveBeenCalledTimes(1))
    const itemInput = api.updateItemRemark.mock.calls[0][1] as Record<string, unknown>
    expect(itemInput).toEqual({ id: 'i1', remark: null })
    expect(Object.keys(itemInput)).toEqual(['id', 'remark'])
  })
})
