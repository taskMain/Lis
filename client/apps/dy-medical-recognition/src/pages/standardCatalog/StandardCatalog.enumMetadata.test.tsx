/**
 * 阶段 1 前端 Component 层用例（测试矩阵 `C75` 的枚举元数据分支）。
 *
 * 为什么单独成文件：`useEnumMetadata` 按「API Client 实例 + 枚举名」在模块级缓存且成功后不失效，
 * 同一文件内先解析成功的用例会让后续用例读到那份元数据，无法再验证兜底分支。因此本文件只固定
 * 「枚举元数据可用」这一条路径；兜底路径由 `StandardCatalog.test.tsx` 覆盖——该文件的占位 Client
 * 不提供枚举元数据端点，其树节点、筛选项与表单选项的既有断言即为回退到本地兜底文案的证据。
 *
 * 装配边界同 `StandardCatalog.test.tsx`：`./standardCatalogApi` 只替换 I/O 函数，其余使用真实实现；
 * `../../contexts/ApiClientContext` 提供带枚举元数据端点的稳定占位 Client。
 */
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { StandardCatalog } from './StandardCatalog'

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
  getEnumMetadata: vi.fn(),
}))

/** 稳定占位 Client：只补上页面会用到的枚举元数据端点，其余由上面的 I/O mock 接管。 */
const stubClient = vi.hoisted(() => ({
  api: { enumMetadata: { getEnumMetadata: { post: (body: { enumName: string }) => api.getEnumMetadata(body) } } },
}))

const enumMetadataPost = api.getEnumMetadata

vi.mock('./standardCatalogApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./standardCatalogApi')>()
  return { ...actual, ...api }
})

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  // 稳定引用：页面把 client 作为 reload 的 useCallback 依赖，返回新对象会让 effect 反复重跑。
  useApiClientContext: () => stubClient,
}))

function renderPage() {
  return render(
    <ConfigProvider locale={zhCN}>
      <StandardCatalog />
    </ConfigProvider>,
  )
}

const category = {
  id: 'c1',
  itemType: 0,
  itemTypeText: null,
  name: '甲分类',
  remark: null,
  isValid: true,
  usageStatus: 0,
  usageStatusText: null,
}

const group = {
  id: 'g1',
  categoryId: 'c1',
  name: '甲分组',
  remark: null,
  isValid: true,
  usageStatus: 0,
  usageStatusText: null,
}

/** 项目行同样不带随行文案：列表「类型」列必须回退到枚举元数据，而不是本地兜底常量。 */
const item = {
  id: 'i1',
  categoryId: 'c1',
  groupId: 'g1',
  itemType: 0,
  itemTypeText: null,
  code: 'A-1',
  name: '甲项目',
  remark: null,
  isValid: true,
}

beforeEach(() => {
  for (const fn of Object.values(api)) fn.mockReset()
  // 元数据默认可用：本文件固定的就是这一条路径（兜底路径由 StandardCatalog.test.tsx 覆盖）。
  enumMetadataPost.mockResolvedValue([
    { value: 0, name: 'Laboratory', description: '检验（元数据）' },
    { value: 1, name: 'Examination', description: '检查（元数据）' },
  ])
  api.queryCategories.mockResolvedValue([category])
  api.queryGroups.mockResolvedValue([group])
  api.queryItems.mockResolvedValue([item])
})

describe('C75 枚举元数据驱动的表单与筛选文案', () => {
  it('表单项目类型下拉、目录树与列表「类型」列的文案取枚举元数据，而非页面本地常量', async () => {
    renderPage()

    // 树节点、筛选分段器与列表「类型」列都只拿得到枚举数值（该行随行文案缺失，契约允许），
    // 三处都应换成元数据文案；本地兜底文案「检验」不得再出现在页面上。
    await waitFor(() => {
      expect(screen.getAllByText('检验（元数据）')).toHaveLength(3)
    })
    expect(screen.queryByText('检验')).not.toBeInTheDocument()
    expect(screen.getByLabelText('分类分组目录树').textContent).toContain('检验（元数据）')
    expect(screen.getByText(/^范围：/).textContent).toContain('检验（元数据）')
    // 列表「类型」列单独再判一次：该行随行文案缺失，回退到本地兜底常量时这里会渲染成整串「检验」。
    const table = within(screen.getByRole('table'))
    expect(table.getByText('检验（元数据）')).toBeInTheDocument()
    expect(table.queryByText('检验')).not.toBeInTheDocument()

    // 表单下拉的选项同样来自元数据：编辑分类弹窗的已选项文案即为元数据文案。
    await within(screen.getByLabelText('分类分组目录树')).findByText('甲分类')
    fireEvent.click(screen.getByLabelText('编辑分类：甲分类'))
    await screen.findByLabelText('分类名称')
    const dialog = within(screen.getByRole('dialog'))
    expect(dialog.getByText('检验（元数据）')).toBeInTheDocument()
    // 使用情况不进入表单与筛选，取行内随行文案，缺失时回退到与后端同名的本地兜底文案。
    expect(dialog.getByText('未使用')).toBeInTheDocument()
    expect(enumMetadataPost).toHaveBeenCalledWith({ enumName: 'MedicalItemType' })
  })
})
