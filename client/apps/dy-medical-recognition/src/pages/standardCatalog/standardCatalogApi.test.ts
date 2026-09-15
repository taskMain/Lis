import { describe, expect, it } from 'vitest'
import {
  createCategory,
  createGroup,
  createItem,
  filterItems,
  isItemEffective,
  resolveTreeScope,
  toRemark,
  updateCategory,
  updateGroup,
  updateItemRemark,
  visibleCatalogCategories,
  type CatalogData,
} from './standardCatalogApi'

const data: CatalogData = {
  categories: [{ id: 'c1', itemType: 0, name: '检验', remark: null, isValid: true, usageStatus: 1 }],
  groups: [{ id: 'g1', categoryId: 'c1', name: '血液', remark: null, isValid: true, usageStatus: 1 }],
  items: [
    { id: 'i1', categoryId: 'c1', groupId: 'g1', itemType: 0, code: 'A01', name: '血常规', remark: null, isValid: true },
    { id: 'i2', categoryId: 'c1', groupId: 'g1', itemType: 0, code: 'A02', name: '尿常规', remark: null, isValid: false },
  ],
}

describe('standardCatalogApi', () => {
  it('keeps category and group tree matches independent from each other', () => {
    const treeData: CatalogData = {
      categories: [
        { id: 'c1', itemType: 0, name: '检验分类', remark: null, isValid: true, usageStatus: 0 },
        { id: 'c2', itemType: 0, name: '其他分类', remark: null, isValid: true, usageStatus: 0 },
      ],
      groups: [
        { id: 'g1', categoryId: 'c1', name: '血液分组', remark: null, isValid: true, usageStatus: 0 },
        { id: 'g2', categoryId: 'c2', name: '其他分组', remark: null, isValid: true, usageStatus: 0 },
      ],
      items: [],
    }
    expect(visibleCatalogCategories(treeData, true, '检验').map(({ category }) => category.id)).toEqual(['c1'])
    expect(visibleCatalogCategories(treeData, true, '血液').map(({ groups }) => groups.map((group) => group.id))).toEqual([['g1']])
    expect(visibleCatalogCategories(treeData, true, '不存在')).toEqual([])
  })

  it('retains a selected group when only name search hides it', () => {
    expect(resolveTreeScope(data, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, true)).toEqual({ itemType: 0, categoryId: 'c1', groupId: 'g1' })
  })

  it('falls back from hidden-by-status nodes to the nearest valid ancestor or type root', () => {
    const disabledGroupData: CatalogData = {
      ...data,
      groups: [{ ...data.groups[0], isValid: false }],
    }
    expect(resolveTreeScope(disabledGroupData, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, false)).toEqual({ itemType: 0, categoryId: 'c1', groupId: null })

    const disabledCategoryData: CatalogData = {
      ...disabledGroupData,
      categories: [{ ...data.categories[0], isValid: false }],
    }
    expect(resolveTreeScope(disabledCategoryData, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, false)).toEqual({ itemType: 0, categoryId: null, groupId: null })
    expect(resolveTreeScope(disabledCategoryData, { itemType: 0, categoryId: 'c1', groupId: null }, true)).toEqual({ itemType: 0, categoryId: 'c1', groupId: null })

    const disabledAncestorData: CatalogData = {
      ...disabledCategoryData,
      groups: [{ ...data.groups[0], isValid: true }],
    }
    expect(resolveTreeScope(disabledAncestorData, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, false)).toEqual({ itemType: 0, categoryId: 'c1', groupId: 'g1' })
    expect(visibleCatalogCategories(disabledAncestorData, false, '血液')).toEqual([{ category: disabledAncestorData.categories[0], groups: [disabledAncestorData.groups[0]] }])
  })

  it('falls back after reload removes the selected node and keeps the scope after adding data', () => {
    const categoryOnlyData = { ...data, groups: [] }
    expect(resolveTreeScope(categoryOnlyData, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, true)).toEqual({ itemType: 0, categoryId: 'c1', groupId: null })

    const addedData = { ...data, items: [...data.items, { ...data.items[0], id: 'i3', code: 'A03', name: '血新增项目' }] }
    const scope = { itemType: 0 as const, categoryId: 'c1', groupId: 'g1' }
    const filter = { code: 'A', name: '血', status: 'enabled' as const }
    expect(resolveTreeScope(addedData, scope, true)).toEqual(scope)
    expect(filterItems(addedData, scope, filter).map((item) => item.id)).toEqual(['i1', 'i3'])
  })

  it('hides a disabled category and its groups when show-disabled is off and no search is active', () => {
    const disabledCategoryData: CatalogData = {
      categories: [
        { id: 'c1', itemType: 0, name: '启用分类', remark: null, isValid: true, usageStatus: 0 },
        { id: 'c2', itemType: 0, name: '停用分类', remark: null, isValid: false, usageStatus: 1 },
      ],
      groups: [
        { id: 'g1', categoryId: 'c1', name: '启用分组', remark: null, isValid: true, usageStatus: 0 },
        { id: 'g2', categoryId: 'c2', name: '停用分类下启用分组', remark: null, isValid: true, usageStatus: 0 },
      ],
      items: [],
    }
    const shown = visibleCatalogCategories(disabledCategoryData, true, '')
    expect(shown.map(({ category }) => category.id)).toEqual(['c1', 'c2'])
    expect(shown.map(({ groups }) => groups.map((group) => group.id))).toEqual([['g1'], ['g2']])

    expect(visibleCatalogCategories(disabledCategoryData, false, '').map(({ category }) => category.id)).toEqual(['c1'])
  })

  it('keeps a disabled category only as the necessary ancestor of a searched enabled group', () => {
    const ancestorData: CatalogData = {
      categories: [
        { id: 'c1', itemType: 0, name: '停用分类A', remark: null, isValid: false, usageStatus: 1 },
        { id: 'c2', itemType: 0, name: '停用分类B', remark: null, isValid: false, usageStatus: 1 },
      ],
      groups: [
        { id: 'g1', categoryId: 'c1', name: '血液分组', remark: null, isValid: true, usageStatus: 0 },
        { id: 'g2', categoryId: 'c1', name: '已停用分组', remark: null, isValid: false, usageStatus: 0 },
        { id: 'g3', categoryId: 'c2', name: '仅停用分组', remark: null, isValid: false, usageStatus: 0 },
      ],
      items: [],
    }
    const searched = visibleCatalogCategories(ancestorData, false, '血液')
    expect(searched.map(({ category }) => category.id)).toEqual(['c1'])
    expect(searched[0].groups.map((group) => group.id)).toEqual(['g1'])

    expect(visibleCatalogCategories(ancestorData, false, '仅停用分组').map(({ category }) => category.id)).toEqual([])
    expect(visibleCatalogCategories(ancestorData, false, '已停用分组').map(({ category }) => category.id)).toEqual([])
    expect(visibleCatalogCategories(ancestorData, false, '不存在的关键字')).toEqual([])
  })

  it('keeps an enabled category matched by name with its visible groups', () => {
    const enabledData: CatalogData = {
      categories: [{ id: 'c1', itemType: 0, name: '启用分类', remark: null, isValid: true, usageStatus: 0 }],
      groups: [
        { id: 'g1', categoryId: 'c1', name: '启用分组', remark: null, isValid: true, usageStatus: 0 },
        { id: 'g2', categoryId: 'c1', name: '停用分组', remark: null, isValid: false, usageStatus: 0 },
      ],
      items: [],
    }
    const searched = visibleCatalogCategories(enabledData, false, '启用分类')
    expect(searched.map(({ category }) => category.id)).toEqual(['c1'])
    expect(searched[0].groups.map((group) => group.id)).toEqual(['g1'])
    expect(visibleCatalogCategories(enabledData, true, '').map(({ groups }) => groups.map((group) => group.id))).toEqual([['g1', 'g2']])
  })

  it('filters locally by scope, text and own status', () => {
    expect(filterItems(data, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, { code: 'a0', name: '血', status: 'enabled' }).map((item) => item.id)).toEqual(['i1'])
    expect(filterItems(data, { itemType: 0, categoryId: 'c1', groupId: 'g1' }, { code: '', name: '', status: 'disabled' }).map((item) => item.id)).toEqual(['i2'])
  })

  it('derives current effective state from all three enabled records', () => {
    expect(isItemEffective(data, data.items[0])).toBe(true)
    expect(isItemEffective(data, data.items[1])).toBe(false)
  })

  it('keeps remark text unchanged and uses null only for an empty input', () => {
    expect(toRemark('  note  ')).toBe('  note  ')
    expect(toRemark('')).toBeNull()
    expect(toRemark(undefined)).toBeNull()
  })

})

/**
 * 写请求契约用例（矩阵 `C42` 与 `C57` 的 Contract 层）。
 *
 * 装配边界：只替换 Client 的 `post` 出口并记录入参，保留适配层真实实现；
 * 不构造真实鉴权适配器，也不断言页面提示（那属 Host 层，见 `C62`）。
 */
type StubClient = Parameters<typeof createItem>[0]

function stubClient() {
  const calls: Record<string, unknown> = {}
  const record = (name: string) => (body: unknown) => {
    calls[name] = body
    return Promise.resolve(true)
  }
  const client = {
    api: {
      medicalRecognitionReport: {
        createMedicalStandardCategory: { post: record('createCategory') },
        updateMedicalStandardCategory: { post: record('updateCategory') },
        createMedicalStandardGroup: { post: record('createGroup') },
        updateMedicalStandardGroup: { post: record('updateGroup') },
        createMedicalStandardItem: { post: record('createItem') },
        changeMedicalStandardItemRemark: { post: record('updateItemRemark') },
      },
    },
  } as unknown as StubClient
  return { client, calls }
}

describe('standardCatalogApi 写请求契约（C42 / C57 Contract 层）', () => {
  it('C42：新增标准项目按原样透传归属标识与完整创建字段', async () => {
    const { client, calls } = stubClient()
    await createItem(client, { categoryId: 'c1', groupId: 'g1', code: 'A01', name: '血常规', remark: null })
    expect(calls.createItem).toEqual({ categoryId: 'c1', groupId: 'g1', code: 'A01', name: '血常规', remark: null })
    expect(Object.keys(calls.createItem as object)).toEqual(['categoryId', 'groupId', 'code', 'name', 'remark'])
  })

  it('C42：不篡改归属标识（传入什么就提交什么）', async () => {
    const { client, calls } = stubClient()
    await createItem(client, { categoryId: 'c-other', groupId: 'g-other', code: 'A01', name: '血常规', remark: null })
    expect(calls.createItem).toMatchObject({ categoryId: 'c-other', groupId: 'g-other' })
  })

  it('C42：契约错误原样抛给调用方，既不吞错也不伪造成功', async () => {
    const { client } = stubClient()
    const sentinel = new Error('归属不一致：分组不属于所选分类')
    ;(client as unknown as { api: { medicalRecognitionReport: { createMedicalStandardItem: { post: () => Promise<never> } } } })
      .api.medicalRecognitionReport.createMedicalStandardItem.post = () => Promise.reject(sentinel)
    await expect(createItem(client, { categoryId: 'c1', groupId: 'gX', code: 'A01', name: '血常规', remark: null })).rejects.toBe(sentinel)
  })

  it('C57：三类新增把空备注映射为显式 null，并各自保留合法创建字段', async () => {
    const { client, calls } = stubClient()
    await createCategory(client, { itemType: 1, name: '检查分类', remark: toRemark('') })
    await createGroup(client, { categoryId: 'c1', name: '血液', remark: toRemark(undefined) })
    await createItem(client, { categoryId: 'c1', groupId: 'g1', code: 'A01', name: '血常规', remark: toRemark('') })
    expect(calls.createCategory).toEqual({ itemType: 1, name: '检查分类', remark: null })
    expect(calls.createGroup).toEqual({ categoryId: 'c1', name: '血液', remark: null })
    expect(calls.createItem).toEqual({ categoryId: 'c1', groupId: 'g1', code: 'A01', name: '血常规', remark: null })
  })

  it('C57：编辑链路不 trim——原值保留、主动清空为显式 null、含首尾空白原样发送', async () => {
    const { client, calls } = stubClient()
    await updateCategory(client, { id: 'c1', itemType: 0, name: '甲分类', remark: toRemark('原备注') })
    expect(calls.updateCategory).toEqual({ id: 'c1', itemType: 0, name: '甲分类', remark: '原备注' })

    await updateGroup(client, { id: 'g1', name: '甲分组', remark: toRemark('') })
    expect(calls.updateGroup).toEqual({ id: 'g1', name: '甲分组', remark: null })
    expect('categoryId' in (calls.updateGroup as object)).toBe(false)

    await updateGroup(client, { id: 'g1', name: '甲分组', remark: toRemark('  原文  ') })
    expect(calls.updateGroup).toEqual({ id: 'g1', name: '甲分组', remark: '  原文  ' })

    await updateItemRemark(client, { id: 'i1', remark: toRemark('') })
    expect(calls.updateItemRemark).toEqual({ id: 'i1', remark: null })
    expect(Object.keys(calls.updateItemRemark as object)).toEqual(['id', 'remark'])
  })
})
