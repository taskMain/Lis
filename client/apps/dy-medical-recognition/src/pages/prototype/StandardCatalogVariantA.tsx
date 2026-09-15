import { useMemo, useState, type ReactNode } from 'react'
import { Button, Card, Empty, Input, Select, Space, Table, Tooltip, Tree, Typography, type TableColumnsType } from 'antd'
import {
  DeleteOutlined,
  EditOutlined,
  InfoCircleOutlined,
  PlusOutlined,
  SearchOutlined,
  StopOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons'
import {
  categoryName,
  categoryUsage,
  defaultScope,
  emptyItemFilter,
  groupName,
  groupUsage,
  ineffectiveReasons,
  isItemEffective,
  matchesText,
  UsageStatus,
  visibleItems,
  type Category,
  type Group,
  type ItemType,
  type StandardItem,
  type TreeScope,
  type ItemFilter,
} from './standardCatalogPrototypeData'
import { usePrototypeStore } from './standardCatalogPrototypeStore'
import {
  CreateCategoryModal,
  CreateGroupModal,
  CreateItemModal,
  EditCategoryModal,
  EditGroupModal,
  ItemRemarkModal,
  ToggleConfirmModal,
  UsageTag,
  ValidTag,
} from './standardCatalogPrototypeModals'

const ITEM_TYPES: ItemType[] = ['检验', '检查']

type ModalState =
  | { kind: 'none' }
  | { kind: 'createCategory'; type: ItemType }
  | { kind: 'editCategory'; category: Category }
  | { kind: 'createGroup'; categoryId: string }
  | { kind: 'editGroup'; group: Group }
  | { kind: 'createItem'; categoryId: string | null }
  | { kind: 'itemRemark'; item: StandardItem }
  | { kind: 'toggle'; targetKind: string; id: string; name: string; isValid: boolean }

/**
 * 变体 A：左侧固定宽度目录树 + 右侧项目列表，分类、分组与项目备注均用弹窗。
 *
 * 所有筛选均为本地计算，不发起请求；派生判断取自完整集合，不依据筛选后可见行。
 */
export function StandardCatalogVariantA() {
  const store = usePrototypeStore()
  const [scope, setScope] = useState<TreeScope>(defaultScope)
  const [typeFilter, setTypeFilter] = useState<'all' | ItemType>('all')
  const [treeKeyword, setTreeKeyword] = useState('')
  const [filter, setFilter] = useState<ItemFilter>(emptyItemFilter)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const close = () => setModal({ kind: 'none' })

  const { data } = store
  const rows = useMemo(() => visibleItems(data, scope, filter), [data, scope, filter])

  const treeData = useMemo(() => {
    const types = typeFilter === 'all' ? ITEM_TYPES : [typeFilter]
    return types
      .map((itemType) => {
        const categories = data.categories.filter((category) => category.itemType === itemType)
        const categoryNodes = categories
          .map((category) => {
            const groups = data.groups.filter((group) => group.categoryId === category.id)
            const groupNodes = groups
              .filter((group) => matchesText(group.name, treeKeyword))
              .map((group) => ({
                key: `g:${group.id}`,
                title: (
                  <span className='sc-proto__node'>
                    <span className='sc-proto__node-label'>{group.name}</span>
                    <ValidTag isValid={group.isValid} />
                    <UsageTag usage={groupUsage(data, group.id)} />
                    <span className='sc-proto__node-actions'>
                      <Tooltip title='编辑分组'>
                        <Button type='text' size='small' icon={<EditOutlined />} onClick={stop(() => setModal({ kind: 'editGroup', group }))} />
                      </Tooltip>
                      <Tooltip title={group.isValid ? '停用分组' : '启用分组'}>
                        <Button
                          type='text'
                          size='small'
                          danger={group.isValid}
                          icon={group.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
                          onClick={stop(() =>
                            setModal({ kind: 'toggle', targetKind: '分组', id: group.id, name: group.name, isValid: group.isValid }),
                          )}
                        />
                      </Tooltip>
                    </span>
                  </span>
                ),
              }))
            const categoryMatched = matchesText(category.name, treeKeyword)
            if (!categoryMatched && groupNodes.length === 0) return null
            return {
              key: `c:${category.id}`,
              title: (
                <span className='sc-proto__node'>
                  <span className='sc-proto__node-label'>{category.name}</span>
                  <ValidTag isValid={category.isValid} />
                  <UsageTag usage={categoryUsage(data, category.id)} />
                  <span className='sc-proto__node-actions'>
                    <Tooltip title='新增分组'>
                      <Button type='text' size='small' icon={<PlusOutlined />} onClick={stop(() => setModal({ kind: 'createGroup', categoryId: category.id }))} />
                    </Tooltip>
                    <Tooltip title='编辑分类'>
                      <Button type='text' size='small' icon={<EditOutlined />} onClick={stop(() => setModal({ kind: 'editCategory', category }))} />
                    </Tooltip>
                    <Tooltip title={category.isValid ? '停用分类' : '启用分类'}>
                      <Button
                        type='text'
                        size='small'
                        danger={category.isValid}
                        icon={category.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
                        onClick={stop(() =>
                          setModal({ kind: 'toggle', targetKind: '分类', id: category.id, name: category.name, isValid: category.isValid }),
                        )}
                      />
                    </Tooltip>
                  </span>
                </span>
              ),
              children: categoryMatched ? groups.map((group) => ({
                key: `g:${group.id}`,
                title: (
                  <span className='sc-proto__node'>
                    <span className='sc-proto__node-label'>{group.name}</span>
                    <ValidTag isValid={group.isValid} />
                    <UsageTag usage={groupUsage(data, group.id)} />
                    <span className='sc-proto__node-actions'>
                      <Tooltip title='编辑分组'>
                        <Button type='text' size='small' icon={<EditOutlined />} onClick={stop(() => setModal({ kind: 'editGroup', group }))} />
                      </Tooltip>
                    </span>
                  </span>
                ),
              })) : groupNodes,
            }
          })
          .filter((node) => node !== null)
        if (categoryNodes.length === 0) return null
        return {
          key: `t:${itemType}`,
          title: (
            <span className='sc-proto__node'>
              <span className='sc-proto__node-label'>{itemType}</span>
              <span className='sc-proto__node-actions'>
                <Tooltip title='新增分类'>
                  <Button type='text' size='small' icon={<PlusOutlined />} onClick={stop(() => setModal({ kind: 'createCategory', type: itemType }))} />
                </Tooltip>
              </span>
            </span>
          ),
          children: categoryNodes,
        }
      })
      .filter((node) => node !== null)
  }, [data, treeKeyword, typeFilter])

  const expandedKeys = useMemo(
    () => [...data.categories.map((category) => `c:${category.id}`), ...ITEM_TYPES.map((itemType) => `t:${itemType}`)],
    [data.categories],
  )

  const columns: TableColumnsType<StandardItem> = [
    { title: '编码', dataIndex: 'code', width: 210 },
    { title: '名称', dataIndex: 'name', width: 230 },
    {
      title: '类型',
      width: 76,
      render: (_, item) => data.categories.find((category) => category.id === item.categoryId)?.itemType ?? '—',
    },
    { title: '分类', width: 130, render: (_, item) => categoryName(data, item.categoryId) },
    { title: '分组', width: 130, render: (_, item) => groupName(data, item.groupId) },
    { title: '状态', width: 78, render: (_, item) => <ValidTag isValid={item.isValid} /> },
    {
      title: '当前有效',
      width: 104,
      render: (_, item) => {
        const effective = isItemEffective(data, item)
        if (effective) return <ValidTag isValid />
        return (
          <Tooltip title={ineffectiveReasons(data, item).join('；')}>
            <span>
              <ValidTag isValid={false} /> <InfoCircleOutlined />
            </span>
          </Tooltip>
        )
      },
    },
    { title: '备注', dataIndex: 'remark', ellipsis: true, render: (value: string | null) => value ?? '' },
    {
      title: '操作',
      width: 118,
      fixed: 'right',
      render: (_, item) => (
        <Space size={2}>
          <Tooltip title='修改备注'>
            <Button type='text' size='small' icon={<EditOutlined />} onClick={() => setModal({ kind: 'itemRemark', item })} />
          </Tooltip>
          <Tooltip title={item.isValid ? '停用' : '启用'}>
            <Button
              type='text'
              size='small'
              danger={item.isValid}
              icon={item.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
              onClick={() => setModal({ kind: 'toggle', targetKind: '标准项目', id: item.id, name: item.name, isValid: item.isValid })}
            />
          </Tooltip>
        </Space>
      ),
    },
  ]

  const scopeText = scope.groupId
    ? `分组：${groupName(data, scope.groupId)}`
    : scope.categoryId
      ? `分类：${categoryName(data, scope.categoryId)}`
      : scope.itemType
        ? `类型：${scope.itemType}`
        : '全部'

  return (
    <div className='sc-proto sc-proto--a'>
      <div className='sc-proto__split'>
        <Card
          className='sc-proto__tree-panel'
          size='small'
          title={<Typography.Text strong>目录</Typography.Text>}
          styles={{ body: { padding: 12 } }}
        >
          <Space orientation='vertical' style={{ width: '100%' }} size={8}>
            <Input
              prefix={<SearchOutlined />}
              placeholder='分类或分组名称'
              allowClear
              value={treeKeyword}
              onChange={(event) => setTreeKeyword(event.target.value)}
            />
            <Select
              value={typeFilter}
              onChange={setTypeFilter}
              style={{ width: '100%' }}
              options={[
                { value: 'all', label: '全部类型' },
                { value: '检验', label: '检验' },
                { value: '检查', label: '检查' },
              ]}
            />
            <div className='sc-proto__tree-scroll'>
              {treeData.length ? (
                <Tree
                  blockNode
                  defaultExpandAll
                  expandedKeys={expandedKeys}
                  selectedKeys={scope.groupId ? [`g:${scope.groupId}`] : scope.categoryId ? [`c:${scope.categoryId}`] : scope.itemType ? [`t:${scope.itemType}`] : []}
                  treeData={treeData}
                  onSelect={(keys) => {
                    const key = String(keys[0] ?? '')
                    if (key.startsWith('g:')) setScope({ itemType: null, categoryId: null, groupId: key.slice(2) })
                    else if (key.startsWith('c:')) setScope({ itemType: null, categoryId: key.slice(2), groupId: null })
                    else if (key.startsWith('t:')) setScope({ itemType: key.slice(2) as ItemType, categoryId: null, groupId: null })
                    else setScope({ itemType: null, categoryId: null, groupId: null })
                  }}
                />
              ) : (
                <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='无匹配节点' />
              )}
            </div>
          </Space>
        </Card>

        <Card
          className='sc-proto__list-panel'
          size='small'
          title={<Typography.Text strong>标准项目列表</Typography.Text>}
          extra={
            <Space>
              <Typography.Text type='secondary'>范围：{scopeText}</Typography.Text>
              <Button icon={<DeleteOutlined />} onClick={() => setFilter(emptyItemFilter)}>
                重置
              </Button>
              <Button type='primary' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createItem', categoryId: scope.categoryId })}>
                新增项目
              </Button>
            </Space>
          }
          styles={{ body: { padding: 12 } }}
        >
          <Space wrap style={{ marginBottom: 12 }}>
            <Input
              placeholder='编码'
              allowClear
              style={{ width: 190 }}
              value={filter.code}
              onChange={(event) => setFilter({ ...filter, code: event.target.value })}
            />
            <Input
              placeholder='名称'
              allowClear
              style={{ width: 200 }}
              value={filter.name}
              onChange={(event) => setFilter({ ...filter, name: event.target.value })}
            />
            <Select
              style={{ width: 130 }}
              value={filter.isValid}
              onChange={(value) => setFilter({ ...filter, isValid: value })}
              options={[
                { value: 'all', label: '全部状态' },
                { value: 'enabled', label: '启用' },
                { value: 'disabled', label: '停用' },
              ]}
            />
          </Space>
          <Table
            rowKey='id'
            size='small'
            columns={columns}
            dataSource={rows}
            scroll={{ x: 1120 }}
            pagination={false}
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }}
          />
        </Card>
      </div>

      <CreateCategoryModal open={modal.kind === 'createCategory'} onClose={close} type={modal.kind === 'createCategory' ? modal.type : '检验'} />
      <EditCategoryModal open={modal.kind === 'editCategory'} onClose={close} data={data} category={modal.kind === 'editCategory' ? modal.category : null} />
      <CreateGroupModal
        open={modal.kind === 'createGroup'}
        onClose={close}
        data={data}
        defaultCategoryId={modal.kind === 'createGroup' ? modal.categoryId : null}
      />
      <EditGroupModal open={modal.kind === 'editGroup'} onClose={close} data={data} group={modal.kind === 'editGroup' ? modal.group : null} />
      <CreateItemModal
        open={modal.kind === 'createItem'}
        onClose={close}
        data={data}
        defaultCategoryId={modal.kind === 'createItem' ? modal.categoryId : null}
      />
      <ItemRemarkModal open={modal.kind === 'itemRemark'} onClose={close} item={modal.kind === 'itemRemark' ? modal.item : null} />
      <ToggleConfirmModal
        open={modal.kind === 'toggle'}
        onClose={close}
        targetKind={modal.kind === 'toggle' ? modal.targetKind : ''}
        targetName={modal.kind === 'toggle' ? modal.name : ''}
        targetValid={modal.kind === 'toggle' ? modal.isValid : true}
        onConfirm={() => {
          if (modal.kind !== 'toggle') return
          const next = !modal.isValid
          if (modal.targetKind === '分类') store.setCategoryValid(modal.id, next)
          else if (modal.targetKind === '分组') store.setGroupValid(modal.id, next)
          else store.setItemValid(modal.id, next)
        }}
      />
    </div>
  )
}

/** 阻止节点操作按钮的点击冒泡，避免改变右侧列表范围。 */
function stop(action: () => void) {
  return (event: React.MouseEvent) => {
    event.stopPropagation()
    action()
  }
}

export type { ReactNode }
export { UsageStatus }
