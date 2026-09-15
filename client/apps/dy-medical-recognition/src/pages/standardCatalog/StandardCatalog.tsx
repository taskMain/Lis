import {
  CheckCircleOutlined,
  EditOutlined,
  InfoCircleOutlined,
  MoreOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
  StopOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Checkbox,
  Dropdown,
  Empty,
  Form,
  Input,
  Modal,
  Segmented,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Tooltip,
  Tree,
  Typography,
  type FormInstance,
  type TableColumnsType,
  type TreeDataNode,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type Key, type ReactNode } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import {
  DEFAULT_SCOPE,
  EMPTY_ITEM_FILTER,
  ITEM_TYPES,
  createCategory,
  createGroup,
  createItem,
  filterItems,
  ineffectiveReasons,
  isItemEffective,
  queryCategories,
  queryGroups,
  queryItems,
  resolveTreeScope,
  setCategoryEnabled,
  setGroupEnabled,
  setItemEnabled,
  toRemark,
  updateCategory,
  updateGroup,
  updateItemRemark,
  visibleCatalogCategories,
  type CatalogData,
  type ItemFilter,
  type ItemTypeValue,
  type StandardCategory,
  type StandardGroup,
  type StandardItem,
  type TreeScope,
} from './standardCatalogApi'
import './StandardCatalog.css'

type ModalState =
  | { kind: 'none' }
  | { kind: 'createCategory'; itemType: ItemTypeValue }
  | { kind: 'editCategory'; category: StandardCategory }
  | { kind: 'createGroup'; categoryId: string }
  | { kind: 'editGroup'; group: StandardGroup }
  | { kind: 'createItem'; categoryId: string | null }
  | { kind: 'editItemRemark'; item: StandardItem }
  | { kind: 'toggle'; kindName: '分类' | '分组' | '标准项目'; id: string; name: string; isValid: boolean }

interface CategoryFormValues { itemType?: ItemTypeValue; name: string; remark?: string }
interface GroupFormValues { categoryId: string; name: string; remark?: string }
interface ItemFormValues { categoryId: string; groupId: string; code: string; name: string; remark?: string }
interface ItemRemarkFormValues { remark?: string }

const ITEM_TYPE_OPTIONS = [
  { value: 0 as const, label: '检验' },
  { value: 1 as const, label: '检查' },
]

function itemTypeLabel(value: ItemTypeValue | null): string {
  if (value === 0) return '检验'
  if (value === 1) return '检查'
  return '未知类型'
}

function usageLabel(value: StandardCategory['usageStatus'] | StandardGroup['usageStatus']): string {
  if (value === 0) return '未被使用'
  if (value === 1) return '已被下级使用'
  return '未知'
}

function validTag(value: boolean | null) {
  if (value === true) return <Tag color='success'>启用</Tag>
  if (value === false) return <Tag>停用</Tag>
  return <Tag color='warning'>未知</Tag>
}

function validateForm<Values extends object>(form: FormInstance<Values>): Promise<boolean> {
  return form.validateFields().then(() => true).catch(() => false)
}

function useCatalogData(client: ReturnType<typeof useApiClientContext>) {
  const [data, setData] = useState<CatalogData>({ categories: [], groups: [], items: [] })
  const dataRef = useRef(data)
  const [loading, setLoading] = useState(true)
  const [failedSources, setFailedSources] = useState<Set<'categories' | 'groups' | 'items'>>(new Set())
  const requestId = useRef(0)

  const reload = useCallback(async () => {
    const currentRequestId = ++requestId.current
    setLoading(true)
    const results = await Promise.allSettled([queryCategories(client), queryGroups(client), queryItems(client)])
    if (currentRequestId !== requestId.current) return dataRef.current
    const nextData = {
      categories: results[0].status === 'fulfilled' ? results[0].value : dataRef.current.categories,
      groups: results[1].status === 'fulfilled' ? results[1].value : dataRef.current.groups,
      items: results[2].status === 'fulfilled' ? results[2].value : dataRef.current.items,
    }
    dataRef.current = nextData
    setData(nextData)
    setFailedSources(new Set(results.flatMap((result, index) => result.status === 'rejected' ? [['categories', 'groups', 'items'][index] as 'categories' | 'groups' | 'items'] : [])))
    setLoading(false)
    return nextData
  }, [client])

  useEffect(() => {
    const task = window.setTimeout(() => { void reload() }, 0)
    return () => window.clearTimeout(task)
  }, [reload])
  return { data, loading, failedSources, reload }
}

function CatalogFormModal({ title, open, onCancel, onSubmit, submitting, children }: { title: string; open: boolean; onCancel: () => void; onSubmit: () => Promise<void>; submitting: boolean; children: ReactNode }) {
  return <Modal title={title} open={open} onCancel={onCancel} onOk={onSubmit} okText='保存' confirmLoading={submitting} destroyOnHidden>{children}</Modal>
}

function CategoryModal({ modal, data, open, onCancel, onSaved }: { modal: Extract<ModalState, { kind: 'createCategory' | 'editCategory' }>; data: CatalogData; open: boolean; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [form] = Form.useForm<CategoryFormValues>()
  const [submitting, setSubmitting] = useState(false)
  const category = modal.kind === 'editCategory' ? modal.category : null
  const frozen = category?.id !== null && category?.id !== undefined ? data.groups.some((group) => group.categoryId === category.id) : false

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ itemType: modal.kind === 'createCategory' ? modal.itemType : category?.itemType ?? undefined, name: modal.kind === 'createCategory' ? '' : category?.name ?? '', remark: modal.kind === 'createCategory' ? '' : category?.remark ?? undefined })
  }, [category, form, modal, open])

  const submit = async () => {
    if (!(await validateForm(form))) return
    const values = form.getFieldsValue()
    if (values.itemType !== 0 && values.itemType !== 1) return
    const itemType = values.itemType
    if (itemType !== 0 && itemType !== 1) return
    setSubmitting(true)
    try {
      const categoryId = category?.id
      if (modal.kind === 'editCategory' && categoryId !== null && categoryId !== undefined) await onSaved(() => updateCategory(client, { id: categoryId, itemType, name: values.name.trim(), remark: toRemark(values.remark) }))
      else await onSaved(() => createCategory(client, { itemType, name: values.name.trim(), remark: toRemark(values.remark) }))
    } finally {
      setSubmitting(false)
    }
  }

  return <CatalogFormModal title={modal.kind === 'createCategory' ? '新增分类' : '编辑分类'} open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      {modal.kind === 'editCategory' && <Form.Item label='使用情况'><Typography.Text>{usageLabel(category?.usageStatus ?? null)}</Typography.Text></Form.Item>}
      <Form.Item label='项目类型' name='itemType' rules={[{ required: true, message: '项目类型必填' }]} extra={frozen ? '该分类下已存在分组（含已停用），项目类型不可修改。' : undefined}><Select disabled={frozen || (modal.kind === 'editCategory' && category?.itemType === null)} options={ITEM_TYPE_OPTIONS} /></Form.Item>
      <Form.Item label='分类名称' name='name' rules={[{ required: true, whitespace: true, message: '分类名称必填' }]}><Input placeholder='全平台唯一' /></Form.Item>
      <Form.Item label='备注' name='remark'><Input.TextArea rows={3} placeholder='选填' /></Form.Item>
      {modal.kind === 'editCategory' && <Typography.Text type='secondary'>启停状态由节点上的独立操作改变，不在此表单内。</Typography.Text>}
    </Form>
  </CatalogFormModal>
}

function GroupModal({ modal, data, open, onCancel, onSaved }: { modal: Extract<ModalState, { kind: 'createGroup' | 'editGroup' }>; data: CatalogData; open: boolean; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [form] = Form.useForm<GroupFormValues>()
  const [submitting, setSubmitting] = useState(false)
  const group = modal.kind === 'editGroup' ? modal.group : null
  const defaultCategoryId = modal.kind === 'createGroup' ? modal.categoryId : group?.categoryId ?? ''

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ categoryId: defaultCategoryId, name: modal.kind === 'createGroup' ? '' : group?.name ?? '', remark: modal.kind === 'createGroup' ? '' : group?.remark ?? undefined })
  }, [defaultCategoryId, form, group, modal, open])

  const submit = async () => {
    if (!(await validateForm(form))) return
    const values = form.getFieldsValue()
    setSubmitting(true)
    try {
      if (modal.kind === 'createGroup') await onSaved(() => createGroup(client, { categoryId: values.categoryId, name: values.name.trim(), remark: toRemark(values.remark) }))
      else if (group?.id !== null && group?.id !== undefined) { const groupId = group.id; await onSaved(() => updateGroup(client, { id: groupId, name: values.name.trim(), remark: toRemark(values.remark) })) }
    } finally {
      setSubmitting(false)
    }
  }

  const category = data.categories.find((value) => value.id === defaultCategoryId)
  const categoryOptions = data.categories.map((value) => ({ value: value.id ?? '', label: `${itemTypeLabel(value.itemType)} · ${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))

  return <CatalogFormModal title={modal.kind === 'createGroup' ? '新增分组' : '编辑分组'} open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      {modal.kind === 'editGroup' && <Form.Item label='使用情况'><Typography.Text>{usageLabel(group?.usageStatus ?? null)}</Typography.Text></Form.Item>}
      <Form.Item label='所属分类' name='categoryId' rules={[{ required: true, message: '所属分类必填' }]} extra={modal.kind === 'createGroup' ? '只能挂到启用状态的分类下。' : '分组创建后归属不可修改，不会提交该字段。'}>{modal.kind === 'createGroup' ? <Select options={categoryOptions} placeholder='选择启用状态的分类' /> : <Input value={category ? `${itemTypeLabel(category.itemType)} · ${category.name ?? '未知名称'}${category.isValid === false ? '（已停用）' : ''}` : '未知分类'} disabled />}</Form.Item>
      <Form.Item label='分组名称' name='name' rules={[{ required: true, whitespace: true, message: '分组名称必填' }]}><Input placeholder='同一分类下唯一' /></Form.Item>
      <Form.Item label='备注' name='remark'><Input.TextArea rows={3} placeholder='选填' /></Form.Item>
      {modal.kind === 'editGroup' && <Typography.Text type='secondary'>启停状态由节点上的独立操作改变，不在此表单内。</Typography.Text>}
    </Form>
  </CatalogFormModal>
}

function CreateItemModal({ open, categoryId, data, onCancel, onSaved }: { open: boolean; categoryId: string | null; data: CatalogData; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [form] = Form.useForm<ItemFormValues>()
  const [submitting, setSubmitting] = useState(false)
  const selectedCategoryId = Form.useWatch('categoryId', form)

  useEffect(() => {
    if (!open) return
    form.resetFields()
    const firstCategory = data.categories.find((value) => value.id === categoryId && value.isValid === true) ?? data.categories.find((value) => value.isValid === true)
    form.setFieldsValue({ categoryId: firstCategory?.id ?? '', groupId: '', code: '', name: '', remark: '' })
  }, [categoryId, data.categories, form, open])

  const submit = async () => {
    if (!(await validateForm(form))) return
    const values = form.getFieldsValue()
    setSubmitting(true)
    try {
      await onSaved(() => createItem(client, { categoryId: values.categoryId, groupId: values.groupId, code: values.code.trim(), name: values.name.trim(), remark: toRemark(values.remark) }))
    } finally {
      setSubmitting(false)
    }
  }

  const categoryOptions = data.categories.map((value) => ({ value: value.id ?? '', label: `${itemTypeLabel(value.itemType)} · ${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))
  const groupOptions = data.groups.filter((value) => value.categoryId === selectedCategoryId).map((value) => ({ value: value.id ?? '', label: `${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))

  return <CatalogFormModal title='新增标准项目' open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      <Form.Item label='所属分类' name='categoryId' rules={[{ required: true, message: '所属分类必填' }]}><Select options={categoryOptions} placeholder='选择启用状态的分类' onChange={() => form.setFieldsValue({ groupId: '' })} /></Form.Item>
      <Form.Item label='所属分组' name='groupId' rules={[{ required: true, message: '所属分组必填' }]} extra='只列出所选分类下的分组；切换分类会清空已选分组。'><Select options={groupOptions} placeholder='选择启用状态的分组' notFoundContent='该分类下暂无分组' /></Form.Item>
      <div className='standard-catalog-form-grid'>
        <Form.Item label='标准项目编码' name='code' rules={[{ required: true, whitespace: true, message: '编码必填' }]}><Input placeholder='全平台唯一' /></Form.Item>
        <Form.Item label='标准项目名称' name='name' rules={[{ required: true, whitespace: true, message: '名称必填' }]}><Input /></Form.Item>
      </div>
      <Form.Item label='备注' name='remark'><Input.TextArea rows={3} placeholder='选填' /></Form.Item>
    </Form>
  </CatalogFormModal>
}

function ItemRemarkModal({ item, open, onCancel, onSaved }: { item: StandardItem | null; open: boolean; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [form] = Form.useForm<ItemRemarkFormValues>()
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ remark: item?.remark ?? undefined })
  }, [form, item, open])

  const submit = async () => {
    if (item?.id === null || item?.id === undefined) return
    const values = form.getFieldsValue()
    setSubmitting(true)
    const itemId = item.id
    try {
      await onSaved(() => updateItemRemark(client, { id: itemId, remark: toRemark(values.remark) }))
    } finally {
      setSubmitting(false)
    }
  }

  return <CatalogFormModal title='修改备注' open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      <Form.Item label='标准项目'><Input value={`${item?.name ?? '未知名称'}（${item?.code ?? '未知编码'}）`} disabled /></Form.Item>
      <Form.Item label='备注' name='remark' extra='清空即表示无备注；编码、名称、分类、分组不可修改。'><Input.TextArea rows={4} placeholder='选填' /></Form.Item>
    </Form>
  </CatalogFormModal>
}

function ToggleModal({ modal, open, onCancel, onSaved }: { modal: Extract<ModalState, { kind: 'toggle' }>; open: boolean; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [submitting, setSubmitting] = useState(false)
  const action = modal.isValid ? '停用' : '启用'

  const submit = async () => {
    setSubmitting(true)
    try {
      if (modal.kindName === '分类') await onSaved(() => setCategoryEnabled(client, modal.id, !modal.isValid))
      else if (modal.kindName === '分组') await onSaved(() => setGroupEnabled(client, modal.id, !modal.isValid))
      else await onSaved(() => setItemEnabled(client, modal.id, !modal.isValid))
    } finally {
      setSubmitting(false)
    }
  }

  return <Modal title={`${action}${modal.kindName}`} open={open} onCancel={onCancel} onOk={submit} okText={`确认${action}`} confirmLoading={submitting} okButtonProps={modal.isValid ? { danger: true } : undefined} destroyOnHidden>
    <Typography.Paragraph>即将{action}{modal.kindName} <Typography.Text strong>{modal.name}</Typography.Text>。</Typography.Paragraph>
    <Typography.Paragraph type='secondary' style={{ marginBottom: 0 }}>该操作只修改当前数据状态，不会级联修改下级状态。</Typography.Paragraph>
  </Modal>
}

function nodeActions(target: StandardCategory | StandardGroup, kindName: '分类' | '分组', setModal: (state: ModalState) => void) {
  return <span className='standard-catalog-node-actions' onClick={(event) => event.stopPropagation()}>
    <Tooltip title={`编辑${kindName}`}><Button type='text' size='small' icon={<EditOutlined />} aria-label={`编辑${kindName}：${target.name ?? '未知名称'}`} onClick={() => setModal('itemType' in target ? { kind: 'editCategory', category: target } : { kind: 'editGroup', group: target })} /></Tooltip>
    <Dropdown trigger={['click']} menu={{
      items: [
        ...(kindName === '分类' ? [{ key: 'createGroup', label: '新增分组', icon: <PlusOutlined />, disabled: target.isValid !== true }] : []),
        { key: 'toggle', label: `${target.isValid === true ? '停用' : '启用'}${kindName}`, icon: target.isValid === true ? <StopOutlined /> : <CheckCircleOutlined />, danger: target.isValid === true, disabled: target.isValid === null || target.id === null },
      ],
      onClick: ({ key, domEvent }) => {
        domEvent.stopPropagation()
        if (key === 'createGroup' && target.id !== null) setModal({ kind: 'createGroup', categoryId: target.id })
        if (key === 'toggle' && target.id !== null && target.isValid !== null) setModal({ kind: 'toggle', kindName, id: target.id, name: target.name ?? '未知名称', isValid: target.isValid })
      },
    }}><Button type='text' size='small' icon={<MoreOutlined />} aria-label={`${kindName}更多操作`} /></Dropdown>
  </span>
}

export function StandardCatalog() {
  const client = useApiClientContext()
  const { data, loading, failedSources, reload } = useCatalogData(client)
  const [scope, setScope] = useState<TreeScope>(DEFAULT_SCOPE)
  const [treeKeyword, setTreeKeyword] = useState('')
  const [showDisabled, setShowDisabled] = useState(true)
  const [collapsedKeys, setCollapsedKeys] = useState<Key[]>([])
  const [filter, setFilter] = useState<ItemFilter>(EMPTY_ITEM_FILTER)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })

  const categoryById = useMemo(() => new Map(data.categories.map((value) => [value.id, value])), [data.categories])
  const groupById = useMemo(() => new Map(data.groups.map((value) => [value.id, value])), [data.groups])
  const activeScope = useMemo(() => resolveTreeScope(data, scope, showDisabled), [data, scope, showDisabled])
  const selectedCategory = activeScope.categoryId === null ? null : categoryById.get(activeScope.categoryId) ?? null
  const selectedGroup = activeScope.groupId === null ? null : groupById.get(activeScope.groupId) ?? null
  const visibleItems = useMemo(() => filterItems(data, activeScope, filter), [activeScope, data, filter])
  const visibleCategories = useMemo(() => visibleCatalogCategories(data, showDisabled, treeKeyword), [data, showDisabled, treeKeyword])

  const treeData = useMemo<TreeDataNode[]>(() => {
    const types = activeScope.itemType === null ? ITEM_TYPES : [activeScope.itemType]
    return types.map((itemType) => ({
      key: `t:${itemType}`,
      title: <span className='standard-catalog-node'><span className='standard-catalog-node-name'>{itemTypeLabel(itemType)}</span><span className='standard-catalog-node-actions'><Tooltip title={`新增${itemTypeLabel(itemType)}分类`}><Button type='text' size='small' icon={<PlusOutlined />} aria-label={`新增${itemTypeLabel(itemType)}分类`} onClick={(event) => { event.stopPropagation(); setModal({ kind: 'createCategory', itemType }) }} /></Tooltip></span></span>,
      children: visibleCategories.filter(({ category }) => category.itemType === itemType).map(({ category, groups }) => ({
        key: `c:${category.id ?? `unknown-${category.name ?? 'category'}`}`,
        title: <span className='standard-catalog-node'><span className='standard-catalog-node-name' title={category.name ?? undefined}>{category.name ?? '未知名称'}</span>{validTag(category.isValid)}{nodeActions(category, '分类', setModal)}</span>,
        children: groups.map((group) => ({ key: `g:${group.id ?? `unknown-${group.name ?? 'group'}`}`, title: <span className='standard-catalog-node'><span className='standard-catalog-node-name' title={group.name ?? undefined}>{group.name ?? '未知名称'}</span>{validTag(group.isValid)}{nodeActions(group, '分组', setModal)}</span> })),
      })),
    }))
  }, [activeScope.itemType, visibleCategories])

  const expandedKeys = useMemo(() => [...ITEM_TYPES.map((itemType) => `t:${itemType}`), ...data.categories.map((category) => `c:${category.id ?? `unknown-${category.name ?? 'category'}`}`)].filter((key) => !collapsedKeys.includes(key)), [collapsedKeys, data.categories])
  const reloadAndResolve = useCallback(async () => {
    const nextData = await reload()
    setScope((current) => resolveTreeScope(nextData, current, showDisabled))
  }, [reload, showDisabled])
  const saveAndReload = async (request: () => Promise<void>) => { await request(); setModal({ kind: 'none' }); await reloadAndResolve() }
  const dependencyReady = failedSources.size === 0 && !loading

  const columns: TableColumnsType<StandardItem> = [
    { title: '编码', dataIndex: 'code', width: 180, render: (value: string | null) => value ?? '—' },
    { title: '名称', dataIndex: 'name', width: 180, render: (value: string | null) => value ?? '—' },
    { title: '类型', width: 80, render: (_, item) => itemTypeLabel(item.itemType) },
    { title: '分类', width: 150, render: (_, item) => categoryById.get(item.categoryId)?.name ?? '—' },
    { title: '分组', width: 150, render: (_, item) => groupById.get(item.groupId)?.name ?? '—' },
    { title: '状态', width: 80, render: (_, item) => validTag(item.isValid) },
    { title: '当前有效', width: 105, render: (_, item) => { const effective = isItemEffective(data, item); if (effective === true) return <Tag color='success'>有效</Tag>; const reasons = ineffectiveReasons(data, item); return <Tooltip title={reasons.join('；') || '当前有效性未知'}><span tabIndex={0} aria-label={reasons.join('；') || '当前有效性未知'}><Tag color={effective === null ? 'warning' : undefined}>{effective === null ? '未知' : '无效'} <InfoCircleOutlined /></Tag></span></Tooltip> } },
    { title: '备注', dataIndex: 'remark', width: 180, ellipsis: true, render: (value: string | null) => value ?? '—' },
    { title: '操作', width: 92, fixed: 'right', render: (_, item) => <Space size={2}><Tooltip title='修改备注'><Button type='text' size='small' icon={<EditOutlined />} aria-label={`修改备注：${item.name ?? '未知名称'}`} disabled={item.id === null} onClick={() => setModal({ kind: 'editItemRemark', item })} /></Tooltip><Tooltip title={item.isValid === true ? '停用项目' : item.isValid === false ? '启用项目' : '状态未知'}><Button type='text' size='small' danger={item.isValid === true} icon={item.isValid === true ? <StopOutlined /> : <CheckCircleOutlined />} aria-label={`${item.isValid === true ? '停用' : '启用'}项目：${item.name ?? '未知名称'}`} disabled={item.id === null || item.isValid === null} onClick={() => item.id !== null && item.isValid !== null && setModal({ kind: 'toggle', kindName: '标准项目', id: item.id, name: item.name ?? '未知名称', isValid: item.isValid })} /></Tooltip></Space> },
  ]

  const scopeText = [activeScope.itemType === null ? null : itemTypeLabel(activeScope.itemType), selectedCategory?.name, selectedGroup?.name].filter((value): value is string => Boolean(value)).join(' / ') || '全部'
  const selectedKeys = activeScope.groupId !== null ? [`g:${activeScope.groupId}`] : activeScope.categoryId !== null ? [`c:${activeScope.categoryId}`] : activeScope.itemType !== null ? [`t:${activeScope.itemType}`] : []

  return <div className='standard-catalog-page'>
    <div className='standard-catalog-header'><div><Typography.Title level={2}>标准项目目录维护</Typography.Title><Typography.Text type='secondary'>分类、分组与标准项目</Typography.Text></div><Space><Tooltip title='重新读取目录'><Button icon={<ReloadOutlined />} aria-label='重新读取目录' loading={loading} onClick={() => void reloadAndResolve()} /></Tooltip><Button type='primary' icon={<PlusOutlined />} disabled={!dependencyReady || (selectedCategory !== null && selectedCategory.isValid !== true)} onClick={() => setModal({ kind: 'createItem', categoryId: activeScope.categoryId })}>新增项目</Button></Space></div>
    {failedSources.size > 0 && <Alert type='warning' showIcon message='目录数据待刷新' description='部分目录读取失败，已有数据保留；依赖未就绪的维护操作暂不可用。' action={<Button size='small' onClick={() => void reloadAndResolve()}>重试</Button>} />}
    <div className='standard-catalog-layout'>
      <section className='standard-catalog-directory' aria-label='标准目录'>
        <div className='standard-catalog-section-header'><Typography.Title level={5}>目录</Typography.Title><Checkbox checked={showDisabled} onChange={(event) => { const checked = event.target.checked; setShowDisabled(checked); setScope((current) => resolveTreeScope(data, current, checked)) }}>显示停用</Checkbox></div>
        <Input prefix={<SearchOutlined />} placeholder='分类或分组名称' aria-label='搜索分类或分组名称' allowClear value={treeKeyword} onChange={(event) => setTreeKeyword(event.target.value)} />
        <Segmented<string | ItemTypeValue> block aria-label='目录项目类型' value={activeScope.itemType ?? 'all'} options={[{ value: 'all', label: '全部' }, ...ITEM_TYPE_OPTIONS]} onChange={(value) => setScope({ itemType: value === 'all' ? null : value === 0 || value === 1 ? value : null, categoryId: null, groupId: null })} />
        <div className='standard-catalog-tree-scroll'><Spin spinning={loading && data.categories.length === 0}><Tree aria-label='分类分组目录树' blockNode treeData={treeData} expandedKeys={expandedKeys} selectedKeys={selectedKeys} autoExpandParent={false} onExpand={(_, info) => setCollapsedKeys((keys) => info.expanded ? keys.filter((key) => key !== info.node.key) : [...keys, info.node.key])} onSelect={(keys) => { const key = String(keys[0] ?? ''); if (key.startsWith('t:')) setScope({ itemType: Number(key.slice(2)) === 0 ? 0 : 1, categoryId: null, groupId: null }); if (key.startsWith('c:')) { const category = data.categories.find((value) => value.id === key.slice(2)); if (category?.id !== null && category?.id !== undefined && category?.itemType !== null) setScope({ itemType: category.itemType, categoryId: category.id, groupId: null }) } if (key.startsWith('g:')) { const group = data.groups.find((value) => value.id === key.slice(2)); const category = group ? categoryById.get(group.categoryId) : null; if (group?.id !== null && group?.id !== undefined && category?.id !== null && category?.id !== undefined && category?.itemType !== null) setScope({ itemType: category.itemType, categoryId: category.id, groupId: group.id }) } }} /></Spin>{treeData.every((node) => !node.children || node.children.length === 0) && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的分类或分组' />}</div>
      </section>
      <section className='standard-catalog-list' aria-label='标准项目列表'>
        <div className='standard-catalog-list-header'><div><Typography.Title level={5}>标准项目列表</Typography.Title><Typography.Text type='secondary'>范围：{scopeText} · {visibleItems.length} 项</Typography.Text></div></div>
        <div className='standard-catalog-filters'><Input placeholder='编码' aria-label='标准项目编码' allowClear value={filter.code} onChange={(event) => setFilter({ ...filter, code: event.target.value })} /><Input placeholder='名称' aria-label='标准项目名称' allowClear value={filter.name} onChange={(event) => setFilter({ ...filter, name: event.target.value })} /><Segmented<ItemFilter['status']> aria-label='标准项目自身状态' value={filter.status} options={[{ value: 'all', label: '全部' }, { value: 'enabled', label: '启用' }, { value: 'disabled', label: '停用' }]} onChange={(status) => setFilter({ ...filter, status })} /><Button icon={<ReloadOutlined />} aria-label='重置列表筛选' onClick={() => setFilter(EMPTY_ITEM_FILTER)}>重置</Button></div>
        <Table rowKey={(item) => item.id ?? `${item.code ?? 'item'}-${item.name ?? 'unknown'}`} size='small' columns={columns} dataSource={visibleItems} loading={loading && data.items.length === 0} scroll={{ x: 1320 }} pagination={false} locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }} />
      </section>
    </div>
    {modal.kind === 'createCategory' || modal.kind === 'editCategory' ? <CategoryModal modal={modal} data={data} open onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'createGroup' || modal.kind === 'editGroup' ? <GroupModal modal={modal} data={data} open onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'createItem' ? <CreateItemModal open categoryId={modal.categoryId} data={data} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'editItemRemark' ? <ItemRemarkModal open item={modal.item} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'toggle' ? <ToggleModal open modal={modal} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
  </div>
}
