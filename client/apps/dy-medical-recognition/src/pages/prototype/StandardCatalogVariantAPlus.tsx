import { useState, type Key } from 'react'
import { Button, Checkbox, Dropdown, Empty, Input, Segmented, Space, Table, Tooltip, Tree, Typography, type TableColumnsType } from 'antd'
import { CheckCircleOutlined, EditOutlined, InfoCircleOutlined, MoreOutlined, PlusOutlined, ReloadOutlined, SearchOutlined, StopOutlined } from '@ant-design/icons'
import {
  categoryName, categoryUsage, defaultScope, emptyItemFilter, groupName, groupUsage,
  ineffectiveReasons, isItemEffective, matchesText, usageLabel, visibleItems,
  type Category, type Group, type ItemFilter, type ItemType, type StandardItem, type TreeScope,
} from './standardCatalogPrototypeData'
import { usePrototypeStore } from './standardCatalogPrototypeStore'
import {
  CreateCategoryModal, CreateGroupModal, CreateItemModal, EditCategoryModal, EditGroupModal,
  ItemRemarkModal, ToggleConfirmModal, ValidTag,
} from './standardCatalogPrototypeModals'
import './StandardCatalogVariantAPlus.css'

const ITEM_TYPES: ItemType[] = ['检验', '检查']

type ModalState =
  | { kind: 'none' }
  | { kind: 'createCategory'; type: ItemType }
  | { kind: 'editCategory'; category: Category }
  | { kind: 'createGroup'; categoryId: string }
  | { kind: 'editGroup'; group: Group }
  | { kind: 'createItem'; categoryId: string | null }
  | { kind: 'itemRemark'; item: StandardItem }
  | { kind: 'toggle'; targetKind: '分类' | '分组' | '标准项目'; id: string; name: string; isValid: boolean }

/** A 的局部比较原型：只调整目录操作和范围联动，沿用共享内存规则与弹窗。 */
export function StandardCatalogVariantAPlus() {
  const store = usePrototypeStore()
  const { data } = store
  const [selection, setSelection] = useState<TreeScope>(defaultScope)
  const [treeKeyword, setTreeKeyword] = useState('')
  const [showDisabled, setShowDisabled] = useState(true)
  const [collapsedKeys, setCollapsedKeys] = useState<Key[]>([])
  const [filter, setFilter] = useState<ItemFilter>(emptyItemFilter)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const close = () => setModal({ kind: 'none' })

  // 类型从选中对象的实际归属派生，编辑空分类类型后也不会遗留旧类型范围。
  const selectedGroup = data.groups.find((group) => group.id === selection.groupId)
  const selectedCategory = data.categories.find((category) => category.id === (selectedGroup?.categoryId ?? selection.categoryId))
  const scope: TreeScope = {
    itemType: selectedCategory?.itemType ?? selection.itemType,
    categoryId: selectedCategory?.id ?? null,
    groupId: selectedGroup?.id ?? null,
  }
  const rows = visibleItems(data, scope, filter)
  const expandedKeys = [
    ...ITEM_TYPES.map((type) => `t:${type}`),
    ...data.categories.map((category) => `c:${category.id}`),
  ].filter((key) => !collapsedKeys.includes(key))

  // 只裁剪树的显示投影；停用分类仍可作为启用分组的必要上级。
  const getVisibleCategories = (includeDisabled: boolean, source = data) => source.categories.flatMap((category) => {
    const categoryMatched = matchesText(category.name, treeKeyword)
    const groups = source.groups.filter((group) =>
      group.categoryId === category.id
      && (includeDisabled || group.isValid)
      && (categoryMatched || matchesText(group.name, treeKeyword)),
    )
    return ((includeDisabled || category.isValid) && categoryMatched) || groups.length > 0
      ? [{ category, groups }]
      : []
  })
  const visibleCategories = getVisibleCategories(showDisabled)
  const visibleScope = (entries: typeof visibleCategories): TreeScope => {
    const parent = entries.find(({ category }) => category.id === scope.categoryId)
    if (parent && (!scope.groupId || parent.groups.some((group) => group.id === scope.groupId))) return scope
    return { itemType: scope.itemType, categoryId: parent?.category.id ?? null, groupId: null }
  }

  const nodeTitle = (target: Category | Group, targetKind: '分类' | '分组') => (
    <span className='aplus-node'>
      <span className='aplus-node-name' title={target.name}>{target.name}</span>
      {!target.isValid && <ValidTag isValid={false} />}
      <span className='aplus-node-actions' onClick={(event) => event.stopPropagation()} onKeyDown={(event) => event.stopPropagation()}>
        <Tooltip title={`编辑${targetKind}`}>
          <Button
            type='text' size='small' icon={<EditOutlined />} aria-label={`编辑${targetKind}：${target.name}`}
            onClick={() => setModal('itemType' in target ? { kind: 'editCategory', category: target } : { kind: 'editGroup', group: target })}
          />
        </Tooltip>
        <Dropdown
          trigger={['click']}
          menu={{
            items: [
              ...(targetKind === '分类' ? [{
                key: 'createGroup', label: '新增分组', icon: <PlusOutlined />,
                disabled: !target.isValid,
              }] : []),
              {
                key: 'toggle', label: `${target.isValid ? '停用' : '启用'}${targetKind}`,
                icon: target.isValid ? <StopOutlined /> : <CheckCircleOutlined />, danger: target.isValid,
              },
            ],
            onClick: ({ key, domEvent }) => {
              domEvent.stopPropagation()
              if (key === 'createGroup') setModal({ kind: 'createGroup', categoryId: target.id })
              else setModal({ kind: 'toggle', targetKind, id: target.id, name: target.name, isValid: target.isValid })
            },
          }}
        >
          <Button type='text' size='small' icon={<MoreOutlined />} aria-label={`${targetKind}更多操作：${target.name}`} title={`${targetKind}更多操作`} />
        </Dropdown>
      </span>
    </span>
  )

  const treeData = (scope.itemType ? [scope.itemType] : ITEM_TYPES).map((itemType) => ({
    key: `t:${itemType}`,
    title: (
      <span className='aplus-node'>
        <span className='aplus-node-name'>{itemType}</span>
        <span className='aplus-node-actions' onClick={(event) => event.stopPropagation()} onKeyDown={(event) => event.stopPropagation()}>
          <Tooltip title={`新增${itemType}分类`}>
            <Button type='text' size='small' icon={<PlusOutlined />} aria-label={`新增${itemType}分类`} onClick={() => setModal({ kind: 'createCategory', type: itemType })} />
          </Tooltip>
        </span>
      </span>
    ),
    children: visibleCategories.filter(({ category }) => category.itemType === itemType).map(({ category, groups }) => ({
        key: `c:${category.id}`, title: nodeTitle(category, '分类'),
        children: groups.map((group) => ({ key: `g:${group.id}`, title: nodeTitle(group, '分组') })),
    })),
  }))

  const columns: TableColumnsType<StandardItem> = [
    { title: '编码', dataIndex: 'code', width: 210 },
    { title: '名称', dataIndex: 'name', width: 230 },
    { title: '类型', width: 76, render: (_, item) => data.categories.find((category) => category.id === item.categoryId)?.itemType ?? '—' },
    { title: '分类', width: 130, render: (_, item) => categoryName(data, item.categoryId) },
    { title: '分组', width: 130, render: (_, item) => groupName(data, item.groupId) },
    { title: '状态', width: 78, render: (_, item) => <ValidTag isValid={item.isValid} /> },
    {
      title: '当前有效', width: 104,
      render: (_, item) => isItemEffective(data, item) ? <ValidTag isValid /> : (
        <Tooltip title={ineffectiveReasons(data, item).join('；')}>
          <span tabIndex={0} aria-label={ineffectiveReasons(data, item).join('；')}><ValidTag isValid={false} /><InfoCircleOutlined /></span>
        </Tooltip>
      ),
    },
    { title: '备注', dataIndex: 'remark', width: 180, ellipsis: true },
    {
      title: '操作', width: 90, fixed: 'right',
      render: (_, item) => (
        <Space size={2}>
          <Tooltip title='修改备注'>
            <Button type='text' size='small' icon={<EditOutlined />} aria-label={`修改备注：${item.name}`} onClick={() => setModal({ kind: 'itemRemark', item })} />
          </Tooltip>
          <Tooltip title={item.isValid ? '停用项目' : '启用项目'}>
            <Button
              type='text' size='small' danger={item.isValid}
              icon={item.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
              aria-label={`${item.isValid ? '停用' : '启用'}项目：${item.name}`}
              onClick={() => setModal({ kind: 'toggle', targetKind: '标准项目', id: item.id, name: item.name, isValid: item.isValid })}
            />
          </Tooltip>
        </Space>
      ),
    },
  ]

  const scopeText = [scope.itemType, selectedCategory?.name, selectedGroup?.name].filter(Boolean).join(' / ') || '全部'

  return (
    <div
      className='sc-proto sc-proto--aplus'
      onKeyDown={(event) => {
        // 目录与分段控件的方向键不能触发页面级原型切换。
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') event.stopPropagation()
      }}
    >
      <div className='aplus-layout'>
        <section className='aplus-directory' aria-label='标准目录'>
          <div className='aplus-directory-header'>
            <Typography.Title level={5}>目录</Typography.Title>
            <Checkbox
              checked={showDisabled}
              onChange={(event) => {
                const checked = event.target.checked
                setShowDisabled(checked)
                if (!checked && visibleScope(visibleCategories) === scope) {
                  setSelection(visibleScope(getVisibleCategories(false)))
                }
              }}
            >显示停用</Checkbox>
          </div>
          <Input
            prefix={<SearchOutlined />} placeholder='分类或分组名称' aria-label='搜索分类或分组名称' allowClear
            value={treeKeyword} onChange={(event) => setTreeKeyword(event.target.value)}
          />
          <Segmented<'all' | ItemType>
            block aria-label='目录项目类型' value={scope.itemType ?? 'all'}
            options={[{ value: 'all', label: '全部' }, ...ITEM_TYPES]}
            onChange={(value) => setSelection({ itemType: value === 'all' ? null : value, categoryId: null, groupId: null })}
          />
          <div className='aplus-tree-scroll'>
            <Tree
              aria-label='分类分组目录树' blockNode treeData={treeData} expandedKeys={expandedKeys}
              autoExpandParent={false}
              onExpand={(_, { expanded, node }) => setCollapsedKeys((keys) => expanded ? keys.filter((key) => key !== node.key) : [...keys, node.key])}
              selectedKeys={scope.groupId ? [`g:${scope.groupId}`] : scope.categoryId ? [`c:${scope.categoryId}`] : scope.itemType ? [`t:${scope.itemType}`] : []}
              onSelect={(keys) => {
                const key = String(keys[0] ?? '')
                if (!key) return
                const group = key.startsWith('g:') ? data.groups.find((entry) => entry.id === key.slice(2)) : undefined
                const category = data.categories.find((entry) => entry.id === (group?.categoryId ?? (key.startsWith('c:') ? key.slice(2) : null)))
                const itemType = category?.itemType ?? ITEM_TYPES.find((type) => key === `t:${type}`) ?? null
                setSelection({ itemType, categoryId: category?.id ?? null, groupId: group?.id ?? null })
              }}
            />
            {(treeKeyword.trim() || !showDisabled) && treeData.every((node) => node.children.length === 0) && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='无符合条件的分类或分组' />}
          </div>
        </section>
        <section className='aplus-list' aria-label='标准项目列表'>
          <header className='aplus-list-header'>
            <div>
              <Typography.Title level={5}>标准项目列表</Typography.Title>
              <Typography.Text type='secondary'>范围：{scopeText} · {rows.length} 项</Typography.Text>
            </div>
            <Tooltip title={selectedCategory && !selectedCategory.isValid ? '所属分类已停用，请先启用分类' : undefined}>
              <span>
                <Button
                  type='primary' aria-label='新增项目' icon={<PlusOutlined />} disabled={Boolean(selectedCategory && !selectedCategory.isValid)}
                  onClick={() => setModal({ kind: 'createItem', categoryId: scope.categoryId })}
                >新增项目</Button>
              </span>
            </Tooltip>
          </header>
          <div className='aplus-filters'>
            <Input placeholder='编码' aria-label='标准项目编码' allowClear value={filter.code} onChange={(event) => setFilter({ ...filter, code: event.target.value })} />
            <Input placeholder='名称' aria-label='标准项目名称' allowClear value={filter.name} onChange={(event) => setFilter({ ...filter, name: event.target.value })} />
            <Segmented<ItemFilter['isValid']>
              block aria-label='标准项目自身状态' value={filter.isValid}
              onChange={(isValid) => setFilter({ ...filter, isValid })}
              options={[{ value: 'all', label: '全部' }, { value: 'enabled', label: '启用' }, { value: 'disabled', label: '停用' }]}
            />
            <Button aria-label='重置' icon={<ReloadOutlined />} onClick={() => setFilter(emptyItemFilter)}>重置</Button>
          </div>
          <Table
            rowKey='id' size='small' columns={columns} dataSource={rows} scroll={{ x: 1328 }} pagination={false}
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }}
          />
        </section>
      </div>
      <CreateCategoryModal open={modal.kind === 'createCategory'} onClose={close} type={modal.kind === 'createCategory' ? modal.type : '检验'} />
      <EditCategoryModal
        open={modal.kind === 'editCategory'} onClose={close} data={data}
        category={modal.kind === 'editCategory' ? modal.category : null}
        usage={modal.kind === 'editCategory' ? usageLabel(categoryUsage(data, modal.category.id)) : undefined}
      />
      <CreateGroupModal open={modal.kind === 'createGroup'} onClose={close} data={data} defaultCategoryId={modal.kind === 'createGroup' ? modal.categoryId : null} />
      <EditGroupModal
        open={modal.kind === 'editGroup'} onClose={close} data={data}
        group={modal.kind === 'editGroup' ? modal.group : null}
        usage={modal.kind === 'editGroup' ? usageLabel(groupUsage(data, modal.group.id)) : undefined}
      />
      <CreateItemModal open={modal.kind === 'createItem'} onClose={close} data={data} defaultCategoryId={modal.kind === 'createItem' ? modal.categoryId : null} />
      <ItemRemarkModal open={modal.kind === 'itemRemark'} onClose={close} item={modal.kind === 'itemRemark' ? modal.item : null} />
      <ToggleConfirmModal
        open={modal.kind === 'toggle'} onClose={close}
        targetKind={modal.kind === 'toggle' ? modal.targetKind : ''}
        targetName={modal.kind === 'toggle' ? modal.name : ''}
        targetValid={modal.kind === 'toggle' ? modal.isValid : true}
        onConfirm={() => {
          if (modal.kind !== 'toggle') return
          // 隐藏停用期间执行停用，同样回退选择；再次显示或启用不恢复旧选择。
          if (!showDisabled && modal.isValid && modal.targetKind !== '标准项目' && visibleScope(visibleCategories) === scope) {
            const nextData = {
              ...data,
              categories: data.categories.map((category) =>
                modal.targetKind === '分类' && category.id === modal.id ? { ...category, isValid: false } : category),
              groups: data.groups.map((group) =>
                modal.targetKind === '分组' && group.id === modal.id ? { ...group, isValid: false } : group),
            }
            setSelection(visibleScope(getVisibleCategories(false, nextData)))
          }
          if (modal.targetKind === '分类') store.setCategoryValid(modal.id, !modal.isValid)
          else if (modal.targetKind === '分组') store.setGroupValid(modal.id, !modal.isValid)
          else store.setItemValid(modal.id, !modal.isValid)
        }}
      />
    </div>
  )
}
