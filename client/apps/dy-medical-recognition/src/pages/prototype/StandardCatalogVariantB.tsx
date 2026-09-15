import { useMemo, useState } from 'react'
import { Button, Card, Empty, Input, Select, Space, Table, Tag, Tooltip, Typography, type TableColumnsType } from 'antd'
import { CheckCircleOutlined, EditOutlined, PlusOutlined, SearchOutlined, StopOutlined } from '@ant-design/icons'
import {
  categoryName,
  categoryUsage,
  defaultScope,
  emptyItemFilter,
  groupUsage,
  ineffectiveReasons,
  isItemEffective,
  visibleItems,
  type Category,
  type Group,
  type ItemType,
  type ItemFilter,
  type StandardItem,
  type TreeScope,
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
 * 变体 B：目录横向铺开为分类卡片与分组标签，项目表格占满全宽；
 * 分类与分组用弹窗，项目备注改为表格行内编辑。
 *
 * 与变体 A 共用同一份内存数据与同一套业务规则，差异只在结构与交互组织。
 */
export function StandardCatalogVariantB() {
  const store = usePrototypeStore()
  const [scope, setScope] = useState<TreeScope>({ ...defaultScope, itemType: null })
  const [typeFilter, setTypeFilter] = useState<'all' | ItemType>('all')
  const [filter, setFilter] = useState<ItemFilter>(emptyItemFilter)
  const [editingRemarkId, setEditingRemarkId] = useState<string | null>(null)
  const [remarkDraft, setRemarkDraft] = useState('')
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const close = () => setModal({ kind: 'none' })

  const { data } = store
  const rows = useMemo(() => visibleItems(data, scope, filter), [data, scope, filter])
  const categories = useMemo(
    () => data.categories.filter((category) => typeFilter === 'all' || category.itemType === typeFilter),
    [data.categories, typeFilter],
  )

  const columns: TableColumnsType<StandardItem> = [
    { title: '编码', dataIndex: 'code', width: 210 },
    { title: '名称', dataIndex: 'name', width: 240 },
    {
      title: '类型',
      width: 76,
      render: (_, item) => data.categories.find((category) => category.id === item.categoryId)?.itemType ?? '—',
    },
    { title: '分类', width: 130, render: (_, item) => categoryName(data, item.categoryId) },
    { title: '分组', width: 130, render: (_, item) => data.groups.find((group) => group.id === item.groupId)?.name ?? '—' },
    { title: '状态', width: 78, render: (_, item) => <ValidTag isValid={item.isValid} /> },
    {
      title: '当前有效',
      width: 104,
      render: (_, item) =>
        isItemEffective(data, item) ? (
          <ValidTag isValid />
        ) : (
          <Tooltip title={ineffectiveReasons(data, item).join('；')}>
            <span>
              <ValidTag isValid={false} /> <Tag color='warning'>原因</Tag>
            </span>
          </Tooltip>
        ),
    },
    {
      title: '备注（行内编辑）',
      width: 220,
      render: (_, item) =>
        editingRemarkId === item.id ? (
          <Space.Compact style={{ width: '100%' }}>
            <Input
              size='small'
              value={remarkDraft}
              autoFocus
              onChange={(event) => setRemarkDraft(event.target.value)}
              onPressEnter={() => {
                store.updateItemRemark(item.id, remarkDraft.trim() === '' ? null : remarkDraft.trim())
                setEditingRemarkId(null)
              }}
            />
            <Button
              size='small'
              type='primary'
              onClick={() => {
                store.updateItemRemark(item.id, remarkDraft.trim() === '' ? null : remarkDraft.trim())
                setEditingRemarkId(null)
              }}
            >
              存
            </Button>
            <Button size='small' onClick={() => setEditingRemarkId(null)}>
              取消
            </Button>
          </Space.Compact>
        ) : (
          <Tooltip title={item.remark ?? '无备注'}>
            <span className='sc-proto__inline-remark'>
              {item.remark ?? '—'}
              <Button
                type='text'
                size='small'
                icon={<EditOutlined />}
                onClick={() => {
                  setEditingRemarkId(item.id)
                  setRemarkDraft(item.remark ?? '')
                }}
              />
            </span>
          </Tooltip>
        ),
    },
    {
      title: '操作',
      width: 78,
      fixed: 'right',
      render: (_, item) => (
        <Tooltip title={item.isValid ? '停用' : '启用'}>
          <Button
            type='text'
            size='small'
            danger={item.isValid}
            icon={item.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
            onClick={() => setModal({ kind: 'toggle', targetKind: '标准项目', id: item.id, name: item.name, isValid: item.isValid })}
          />
        </Tooltip>
      ),
    },
  ]

  return (
    <div className='sc-proto sc-proto--b'>
      <Card size='small' styles={{ body: { padding: 12 } }}>
        <Space wrap style={{ marginBottom: 12 }}>
          <Input
            prefix={<SearchOutlined />}
            placeholder='筛选分类或分组（本地）'
            allowClear
            style={{ width: 220 }}
            value={filter.name}
            onChange={(event) => setFilter({ ...filter, name: event.target.value })}
          />
          <Select
            style={{ width: 130 }}
            value={typeFilter}
            onChange={setTypeFilter}
            options={[
              { value: 'all', label: '全部类型' },
              { value: '检验', label: '检验' },
              { value: '检查', label: '检查' },
            ]}
          />
        </Space>
        <div className='sc-proto__cards'>
          {categories.map((category) => {
            const groups = data.groups.filter((group) => group.categoryId === category.id)
            return (
              <div className='sc-proto__category-card' key={category.id}>
                <div className='sc-proto__category-head'>
                  <Space size={6} wrap>
                    <Typography.Text strong>{category.name}</Typography.Text>
                    <Tag color={category.itemType === '检验' ? 'geekblue' : 'cyan'}>{category.itemType}</Tag>
                    <ValidTag isValid={category.isValid} />
                    <UsageTag usage={categoryUsage(data, category.id)} />
                  </Space>
                  <Space size={2}>
                    <Tooltip title='新增分组'>
                      <Button type='text' size='small' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createGroup', categoryId: category.id })} />
                    </Tooltip>
                    <Tooltip title='编辑分类'>
                      <Button type='text' size='small' icon={<EditOutlined />} onClick={() => setModal({ kind: 'editCategory', category })} />
                    </Tooltip>
                    <Tooltip title={category.isValid ? '停用分类' : '启用分类'}>
                      <Button
                        type='text'
                        size='small'
                        danger={category.isValid}
                        icon={category.isValid ? <StopOutlined /> : <CheckCircleOutlined />}
                        onClick={() => setModal({ kind: 'toggle', targetKind: '分类', id: category.id, name: category.name, isValid: category.isValid })}
                      />
                    </Tooltip>
                  </Space>
                </div>
                <div className='sc-proto__group-tags'>
                  {groups.length === 0 ? (
                    <Typography.Text type='secondary'>暂无分组</Typography.Text>
                  ) : (
                    groups.map((group) => (
                      <Tag
                        key={group.id}
                        className={scope.groupId === group.id ? 'sc-proto__group-tag is-selected' : 'sc-proto__group-tag'}
                        onClick={() => setScope({ itemType: null, categoryId: null, groupId: group.id })}
                      >
                        {group.name}
                        {!group.isValid && '（停用）'}
                        {groupUsage(data, group.id) === 1 && ' ·已被使用'}
                      </Tag>
                    ))
                  )}
                </div>
              </div>
            )
          })}
        </div>
      </Card>

      <Card
        size='small'
        style={{ marginTop: 12 }}
        title={<Typography.Text strong>标准项目列表</Typography.Text>}
        extra={
          <Space>
            <Input
              placeholder='编码'
              allowClear
              style={{ width: 170 }}
              value={filter.code}
              onChange={(event) => setFilter({ ...filter, code: event.target.value })}
            />
            <Input
              placeholder='名称'
              allowClear
              style={{ width: 170 }}
              value={filter.name}
              onChange={(event) => setFilter({ ...filter, name: event.target.value })}
            />
            <Select
              style={{ width: 120 }}
              value={filter.isValid}
              onChange={(value) => setFilter({ ...filter, isValid: value })}
              options={[
                { value: 'all', label: '全部状态' },
                { value: 'enabled', label: '启用' },
                { value: 'disabled', label: '停用' },
              ]}
            />
            <Button onClick={() => setFilter(emptyItemFilter)}>重置</Button>
            <Button type='primary' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createItem', categoryId: null })}>
              新增项目
            </Button>
          </Space>
        }
        styles={{ body: { padding: 12 } }}
      >
        <Table
          rowKey='id'
          size='small'
          columns={columns}
          dataSource={rows}
          scroll={{ x: 1240 }}
          pagination={false}
          locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }}
        />
      </Card>

      <CreateCategoryModal open={modal.kind === 'createCategory'} onClose={close} type={modal.kind === 'createCategory' ? modal.type : '检验'} />
      <EditCategoryModal open={modal.kind === 'editCategory'} onClose={close} data={data} category={modal.kind === 'editCategory' ? modal.category : null} />
      <CreateGroupModal open={modal.kind === 'createGroup'} onClose={close} data={data} defaultCategoryId={modal.kind === 'createGroup' ? modal.categoryId : null} />
      <EditGroupModal open={modal.kind === 'editGroup'} onClose={close} data={data} group={modal.kind === 'editGroup' ? modal.group : null} />
      <CreateItemModal open={modal.kind === 'createItem'} onClose={close} data={data} defaultCategoryId={modal.kind === 'createItem' ? modal.categoryId : null} />
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
