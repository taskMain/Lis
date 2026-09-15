import { useEffect, useMemo, useState } from 'react'
import { App, Button, Card, Empty, Form, Input, Select, Space, Table, Tooltip, Tree, Typography, type TableColumnsType } from 'antd'
import { CheckCircleOutlined, PlusOutlined, SaveOutlined, StopOutlined } from '@ant-design/icons'
import {
  categoryName,
  categoryUsage,
  defaultScope,
  emptyItemFilter,
  groupUsage,
  ineffectiveReasons,
  isCategoryTypeFrozen,
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
  ItemRemarkModal,
  ToggleConfirmModal,
  UsageTag,
  ValidTag,
} from './standardCatalogPrototypeModals'

const ITEM_TYPES: ItemType[] = ['检验', '检查']

type Selected =
  | { kind: 'none' }
  | { kind: 'category'; category: Category }
  | { kind: 'group'; group: Group }

type ModalState =
  | { kind: 'none' }
  | { kind: 'createCategory'; type: ItemType }
  | { kind: 'createGroup'; categoryId: string }
  | { kind: 'createItem'; categoryId: string | null }
  | { kind: 'itemRemark'; item: StandardItem }
  | { kind: 'toggle'; targetKind: string; id: string; name: string; isValid: boolean }

/**
 * 变体 C：左侧窄树 + 右侧就地编辑面板 + 项目表格；
 * 分类与分组在右侧面板直接改，项目仍用弹窗。
 *
 * 与变体 A、B 共用同一份内存数据与同一套业务规则，差异只在结构与交互组织。
 */
export function StandardCatalogVariantC() {
  const store = usePrototypeStore()
  const { message } = App.useApp()
  const [scope, setScope] = useState<TreeScope>({ ...defaultScope, itemType: null })
  const [selected, setSelected] = useState<Selected>({ kind: 'none' })
  const [filter, setFilter] = useState<ItemFilter>(emptyItemFilter)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const [form] = Form.useForm<{ itemType: ItemType; name: string; remark: string }>()
  const close = () => setModal({ kind: 'none' })

  const { data } = store
  const rows = useMemo(() => visibleItems(data, scope, filter), [data, scope, filter])

  // 选中对象变化时重置就地编辑表单，避免残留上一个对象的值与冻结状态。
  useEffect(() => {
    if (selected.kind === 'category') {
      form.setFieldsValue({ itemType: selected.category.itemType, name: selected.category.name, remark: selected.category.remark ?? '' })
    } else if (selected.kind === 'group') {
      form.setFieldsValue({ itemType: '检验', name: selected.group.name, remark: selected.group.remark ?? '' })
    } else {
      form.resetFields()
    }
  }, [selected, form])

  const treeData = useMemo(
    () =>
      ITEM_TYPES.map((itemType) => ({
        key: `t:${itemType}`,
        title: <Typography.Text strong>{itemType}</Typography.Text>,
        children: data.categories
          .filter((category) => category.itemType === itemType)
          .map((category) => ({
            key: `c:${category.id}`,
            title: (
              <span className='sc-proto__node'>
                <span className='sc-proto__node-label'>{category.name}</span>
                <ValidTag isValid={category.isValid} />
              </span>
            ),
            children: data.groups
              .filter((group) => group.categoryId === category.id)
              .map((group) => ({
                key: `g:${group.id}`,
                title: (
                  <span className='sc-proto__node'>
                    <span className='sc-proto__node-label'>{group.name}</span>
                    <ValidTag isValid={group.isValid} />
                  </span>
                ),
              })),
          })),
      })),
    [data.categories, data.groups],
  )

  const columns: TableColumnsType<StandardItem> = [
    { title: '编码', dataIndex: 'code', width: 200 },
    { title: '名称', dataIndex: 'name', width: 220 },
    { title: '分类', width: 120, render: (_, item) => categoryName(data, item.categoryId) },
    { title: '分组', width: 120, render: (_, item) => data.groups.find((group) => group.id === item.groupId)?.name ?? '—' },
    { title: '状态', width: 78, render: (_, item) => <ValidTag isValid={item.isValid} /> },
    {
      title: '当前有效',
      width: 100,
      render: (_, item) =>
        isItemEffective(data, item) ? (
          <ValidTag isValid />
        ) : (
          <Tooltip title={ineffectiveReasons(data, item).join('；')}>
            <span>
              <ValidTag isValid={false} /> ？
            </span>
          </Tooltip>
        ),
    },
    { title: '备注', dataIndex: 'remark', ellipsis: true, render: (value: string | null) => value ?? '' },
    {
      title: '操作',
      width: 118,
      fixed: 'right',
      render: (_, item) => (
        <Space size={2}>
          <Button type='text' size='small' onClick={() => setModal({ kind: 'itemRemark', item })}>
            备注
          </Button>
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

  const typeFrozen = selected.kind === 'category' ? isCategoryTypeFrozen(data, selected.category.id) : false

  const saveInPlace = () => {
    const values = form.getFieldsValue()
    if (!values.name?.trim()) {
      form.setFields([{ name: 'name', errors: ['名称必填'] }])
      return
    }
    const remark = values.remark.trim() === '' ? null : values.remark.trim()
    if (selected.kind === 'category') {
      store.updateCategory(selected.category.id, { itemType: values.itemType, name: values.name.trim(), remark })
    } else if (selected.kind === 'group') {
      // 归属不可改，不提交 CategoryId
      store.updateGroup(selected.group.id, { name: values.name.trim(), remark })
    }
    void message.success('已保存在内存原型数据中')
  }

  return (
    <div className='sc-proto sc-proto--c'>
      <div className='sc-proto__split sc-proto__split--c'>
        <Card size='small' title={<Typography.Text strong>目录</Typography.Text>} styles={{ body: { padding: 8 } }}>
          <div className='sc-proto__tree-scroll'>
            <Tree
              blockNode
              defaultExpandAll
              selectedKeys={
                selected.kind === 'category'
                  ? [`c:${selected.category.id}`]
                  : selected.kind === 'group'
                    ? [`g:${selected.group.id}`]
                    : []
              }
              treeData={treeData}
              onSelect={(keys) => {
                const key = String(keys[0] ?? '')
                if (key.startsWith('c:')) {
                  const id = key.slice(2)
                  const category = data.categories.find((entry) => entry.id === id)
                  if (category) {
                    setSelected({ kind: 'category', category })
                    setScope({ itemType: null, categoryId: id, groupId: null })
                  }
                } else if (key.startsWith('g:')) {
                  const id = key.slice(2)
                  const group = data.groups.find((entry) => entry.id === id)
                  if (group) {
                    setSelected({ kind: 'group', group })
                    setScope({ itemType: null, categoryId: null, groupId: id })
                  }
                } else if (key.startsWith('t:')) {
                  setSelected({ kind: 'none' })
                  setScope({ itemType: key.slice(2) as ItemType, categoryId: null, groupId: null })
                }
              }}
            />
          </div>
        </Card>

        <Card
          size='small'
          title={<Typography.Text strong>就地编辑</Typography.Text>}
          extra={
            <Space size={2}>
              <Tooltip title='新增分类'>
                <Button type='text' size='small' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createCategory', type: scope.itemType ?? '检验' })} />
              </Tooltip>
              {selected.kind === 'category' && (
                <Tooltip title='新增分组'>
                  <Button type='text' size='small' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createGroup', categoryId: selected.category.id })} />
                </Tooltip>
              )}
              {selected.kind !== 'none' && (
                <Tooltip title={selected.kind === 'category' ? (selected.category.isValid ? '停用分类' : '启用分类') : selected.group.isValid ? '停用分组' : '启用分组'}>
                  <Button
                    type='text'
                    size='small'
                    icon={<StopOutlined />}
                    onClick={() => {
                      if (selected.kind === 'category') {
                        setModal({ kind: 'toggle', targetKind: '分类', id: selected.category.id, name: selected.category.name, isValid: selected.category.isValid })
                      } else if (selected.kind === 'group') {
                        setModal({ kind: 'toggle', targetKind: '分组', id: selected.group.id, name: selected.group.name, isValid: selected.group.isValid })
                      }
                    }}
                  />
                </Tooltip>
              )}
            </Space>
          }
          styles={{ body: { padding: 12 } }}
        >
          {selected.kind === 'none' ? (
            <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='在左侧选择分类或分组后就地编辑' />
          ) : (
            <Form form={form} layout='vertical'>
              {selected.kind === 'category' ? (
                <Form.Item
                  label='项目类型'
                  name='itemType'
                  extra={typeFrozen ? '该分类下已存在分组（含已停用），项目类型不可修改。' : '尚无分组，项目类型仍可修改。'}
                >
                  <Select disabled={typeFrozen} options={ITEM_TYPES.map((value) => ({ value, label: value }))} />
                </Form.Item>
              ) : (
                <>
                  <Form.Item label='所属分类' extra='归属不可修改，不提交 CategoryId。'>
                    <Input
                      disabled
                      readOnly
                      value={categoryName(data, selected.group.categoryId)}
                    />
                  </Form.Item>
                  <Form.Item label='使用情况'>
                    <UsageTag usage={groupUsage(data, selected.group.id)} />
                    <Typography.Text type='secondary' style={{ marginLeft: 8 }}>
                      名称与备注不受使用情况限制
                    </Typography.Text>
                  </Form.Item>
                </>
              )}
              {selected.kind === 'category' && (
                <Form.Item label='使用情况'>
                  <UsageTag usage={categoryUsage(data, selected.category.id)} />
                  <Typography.Text type='secondary' style={{ marginLeft: 8 }}>
                    名称与备注不受使用情况限制
                  </Typography.Text>
                </Form.Item>
              )}
              <Form.Item label={selected.kind === 'category' ? '分类名称' : '分组名称'} name='name' rules={[{ required: true, message: '名称必填' }]}>
                <Input />
              </Form.Item>
              <Form.Item label='备注' name='remark'>
                <Input.TextArea rows={2} placeholder='选填' />
              </Form.Item>
              <Button type='primary' icon={<SaveOutlined />} onClick={saveInPlace}>
                保存
              </Button>
              <Typography.Paragraph type='secondary' style={{ marginTop: 8, marginBottom: 0 }}>
                启停状态不在此面板内，使用上方的独立操作改变；该操作不会级联修改下级状态。
              </Typography.Paragraph>
            </Form>
          )}
        </Card>
      </div>

      <Card
        size='small'
        style={{ marginTop: 12 }}
        title={<Typography.Text strong>标准项目列表</Typography.Text>}
        extra={
          <Space>
            <Input
              placeholder='编码'
              allowClear
              style={{ width: 160 }}
              value={filter.code}
              onChange={(event) => setFilter({ ...filter, code: event.target.value })}
            />
            <Input
              placeholder='名称'
              allowClear
              style={{ width: 160 }}
              value={filter.name}
              onChange={(event) => setFilter({ ...filter, name: event.target.value })}
            />
            <Button onClick={() => setFilter(emptyItemFilter)}>重置</Button>
            <Button type='primary' icon={<PlusOutlined />} onClick={() => setModal({ kind: 'createItem', categoryId: scope.categoryId })}>
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
          scroll={{ x: 1100 }}
          pagination={false}
          locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }}
        />
      </Card>

      <CreateCategoryModal open={modal.kind === 'createCategory'} onClose={close} type={modal.kind === 'createCategory' ? modal.type : '检验'} />
      <CreateGroupModal open={modal.kind === 'createGroup'} onClose={close} data={data} defaultCategoryId={modal.kind === 'createGroup' ? modal.categoryId : null} />
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
