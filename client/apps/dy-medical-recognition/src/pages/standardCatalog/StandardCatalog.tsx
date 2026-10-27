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
import { useEnumMetadata } from '../../hooks/useEnumMetadata'
import {
  DEFAULT_SCOPE,
  EMPTY_ITEM_FILTER,
  ITEM_TYPES,
  ITEM_TYPE_METADATA_FALLBACK,
  UNKNOWN_MEDICAL_ITEM_TYPE_TEXT,
  USAGE_STATUS_TEXTS,
  createCategory,
  createGroup,
  createItem,
  filterItems,
  ineffectiveReasons,
  isItemEffective,
  isMedicalItemTypeValue,
  isUsageStatusValue,
  queryCategories,
  queryGroups,
  queryItems,
  resolveTreeScope,
  setCategoryEnabled,
  setGroupEnabled,
  setItemEnabled,
  toItemTypeOptions,
  toRemark,
  updateCategory,
  updateGroup,
  updateItemRemark,
  visibleCatalogCategories,
  type CatalogData,
  type EnumMetadataOptionLike,
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
  | { kind: 'createItem'; scope: TreeScope }
  | { kind: 'editItemRemark'; item: StandardItem }
  | { kind: 'toggle'; kindName: '分类' | '分组' | '标准项目'; id: string; name: string; isValid: boolean }

interface CategoryFormValues { itemType?: ItemTypeValue; name: string; remark?: string }
interface GroupFormValues { categoryId: string; name: string; remark?: string }
interface ItemFormValues { categoryId: string; groupId: string; code: string; name: string; remark?: string }
interface ItemRemarkFormValues { remark?: string }

/**
 * 项目类型文案取值顺序：服务端随行文案（`itemTypeText`）→ 枚举元数据（服务端枚举声明）
 * → 本地兜底选项；未知与缺失取值显示「未知类型」。
 *
 * 页面按数值判定类型，中文一律来自服务端：不再由本文件自持有文案表。
 * `options` 由 `useItemTypeOptions` 提供，不设默认值：省略元数据层会让同一屏出现两种中文
 * （有随行文案的行显示服务端文案，缺随行文案的行显示本地兜底文案）。
 */
function itemTypeLabel(value: ItemTypeValue | null, serverText: string | null | undefined, options: readonly Pick<EnumMetadataOptionLike, 'value' | 'label'>[]): string {
  const text = serverText?.trim()
  if (text) return text
  return options.find((option) => option.value === value)?.label ?? UNKNOWN_MEDICAL_ITEM_TYPE_TEXT
}

/**
 * 使用情况文案取值顺序同上；该状态在本页只作展示，不进入表单与筛选，
 * 因此不读取枚举元数据，缺失或未知取值按取值域判定回退到与后端同名的本地文案。
 */
function usageLabel(value: StandardCategory['usageStatus'] | StandardGroup['usageStatus'] | null, serverText?: string | null): string {
  const text = serverText?.trim()
  if (text) return text
  return isUsageStatusValue(value) ? USAGE_STATUS_TEXTS[value] : '未知'
}

/**
 * 项目类型下拉与筛选项：取枚举元数据（服务端枚举声明），接口不可用或尚未到达时用兜底选项。
 *
 * 返回值**引用稳定**（随元数据引用与模块级兜底常量变化），因此可安全作为 `useMemo` 依赖与组件 props。
 * 该请求按「API Client 实例 + 枚举名」在子应用级缓存，多处调用不产生额外请求。
 * 注意：稳定的只是本 hook 的返回值；调用方若把它与字面量组合（如 `[{ value: 'all', … }, ...options]`）
 * 仍是每次渲染新建数组，那类组合要单独 `useMemo` 才有稳定引用。
 */
function useItemTypeOptions(): { value: ItemTypeValue; label: string }[] {
  const metadata = useEnumMetadata('MedicalItemType', ITEM_TYPE_METADATA_FALLBACK)
  return useMemo(() => toItemTypeOptions(metadata), [metadata])
}

function validTag(value: boolean | null) {
  if (value === true) return <Tag color='success'>启用</Tag>
  if (value === false) return <Tag>停用</Tag>
  return <Tag color='warning'>未知</Tag>
}

/**
 * 按宽度截断判断的悬停全文（S1-D33、S1-D38）：量宽（scrollWidth > clientWidth）决定是否挂
 * Tooltip，仅被截断的文本才挂气泡，完整可见的文本不产生气泡；气泡延迟 0.5 秒出现，
 * 避免扫过即弹与遮挡邻行。截断只是展示层行为，文本原值仍完整参与本地过滤、搜索命中、
 * 选择、写操作与派生判断。
 *
 * `title` 只收非空字符串；空占位（「—」等）不出气泡的判断只在 `ellipsisCell` 表达。
 * 量宽时机：元素挂接时执行一次、ResizeObserver 在容器尺寸变化时复测、`text` 变化时经
 * measureRef 补测一次——同一挂载实例内改名变长时容器宽度不变、RO 不触发，必须随文本复测。
 * 包裹状态切换会重建子树，回调 ref 在每次挂接时重建观测，解挂依赖 React 19
 * 的 ref cleanup 语义断开（react 19.2 已核对）。`clientWidth` 为 0 表示元素瞬时
 * 不可见（重排、缩放进行中），该次量宽作废，等待 ResizeObserver 的下一次通知。
 * jsdom 无布局引擎，测试环境用宽度替身（见测试文件头；类名改动由 `StandardCatalog.test.tsx` 的节点骨架用例兜底）。
 */
function TruncatedText({ text, title, className }: { text: string; title: string; className: string }) {
  const [truncated, setTruncated] = useState(false)
  const measureRef = useRef<() => void>(() => {})
  const attach = useCallback((element: HTMLSpanElement | null) => {
    if (!element) return undefined
    const measure = () => {
      if (element.clientWidth === 0) return
      setTruncated(element.scrollWidth > element.clientWidth)
    }
    measureRef.current = measure
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(element)
    return () => observer.disconnect()
  }, [])
  // 文本变化但元素复用时容器宽度往往不变（RO 不触发），这里随 text 补一次复测。
  useEffect(() => { measureRef.current() }, [text])
  const span = <span ref={attach} className={className}>{text}</span>
  if (!truncated) return span
  return <Tooltip title={title} mouseEnterDelay={0.5}>{span}</Tooltip>
}

/**
 * 超长列的单行省略单元格（S1-D33）：文本按列宽截断省略；
 * 被截断时悬停经 Tooltip 展示完整值、延迟 0.5 秒（S1-D38）。
 * 空值渲染占位「—」且不挂气泡——占位判断只在这一处表达。
 */
function ellipsisCell(value: string | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return <span className='standard-catalog-cell-text'>{value ?? '—'}</span>
  }
  return <TruncatedText text={value} title={value} className='standard-catalog-cell-text' />
}

/**
 * 树节点标题骨架（S1-D33）：名称在左按宽度截断省略（被截断时悬停全文，S1-D38），
 * 状态标记与操作图标作为 `side` 放进最右固定槽位，常显且不随名称长度推移。
 */
function catalogNodeTitle(name: string, side: ReactNode) {
  return (
    <span className='standard-catalog-node'>
      <TruncatedText className='standard-catalog-node-name' text={name} title={name} />
      <span className='standard-catalog-node-side'>{side}</span>
    </span>
  )
}

/**
 * 列表「状态」合并列（S1-D39）：全链路启用为绿色「有效」；否则「无效」并悬停给出原因；
 * 状态数据不完整为「未知」。派生仍走 isItemEffective，口径不变。
 */
function renderItemStatus(data: CatalogData, item: StandardItem) {
  const effective = isItemEffective(data, item)
  if (effective === true) return <Tag color='success'>有效</Tag>
  const hint = ineffectiveReasons(data, item).join('；') || '当前有效性未知'
  return (
    <Tooltip title={hint}>
      <span tabIndex={0} aria-label={hint}>
        <Tag color={effective === null ? 'warning' : undefined}>{effective === null ? '未知' : '无效'} <InfoCircleOutlined /></Tag>
      </span>
    </Tooltip>
  )
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
  // 项目类型选项取枚举元数据（服务端枚举声明），不可用或尚未到达时用兜底选项；该请求按枚举名在子应用级缓存。
  const itemTypeOptions = useItemTypeOptions()

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ itemType: modal.kind === 'createCategory' ? modal.itemType : category?.itemType ?? undefined, name: modal.kind === 'createCategory' ? '' : category?.name ?? '', remark: modal.kind === 'createCategory' ? '' : category?.remark ?? undefined })
  }, [category, form, modal, open])

  const submit = async () => {
    if (!(await validateForm(form))) return
    const values = form.getFieldsValue()
    // 取值域判定走共享守卫：越界或缺失取值一律不提交，不把未知类型写进契约。
    if (!isMedicalItemTypeValue(values.itemType)) return
    const itemType = values.itemType
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
      {modal.kind === 'editCategory' && <Form.Item label='使用情况'><Typography.Text>{usageLabel(category?.usageStatus ?? null, category?.usageStatusText)}</Typography.Text></Form.Item>}
      <Form.Item label='项目类型' name='itemType' rules={[{ required: true, message: '项目类型必填' }]} extra={frozen ? '该分类下已存在分组（含已停用），项目类型不可修改。' : undefined}><Select disabled={frozen || (modal.kind === 'editCategory' && category?.itemType === null)} options={itemTypeOptions} /></Form.Item>
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
  const itemTypeOptions = useItemTypeOptions()

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
  const categoryOptions = data.categories.map((value) => ({ value: value.id ?? '', label: `${itemTypeLabel(value.itemType, value.itemTypeText, itemTypeOptions)} · ${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))

  return <CatalogFormModal title={modal.kind === 'createGroup' ? '新增分组' : '编辑分组'} open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      {modal.kind === 'editGroup' && <Form.Item label='使用情况'><Typography.Text>{usageLabel(group?.usageStatus ?? null, group?.usageStatusText)}</Typography.Text></Form.Item>}
      <Form.Item label='所属分类' name='categoryId' rules={[{ required: true, message: '所属分类必填' }]} extra={modal.kind === 'createGroup' ? '只能挂到启用状态的分类下。' : '分组创建后归属不可修改，不会提交该字段。'}>{modal.kind === 'createGroup' ? <Select options={categoryOptions} placeholder='选择启用状态的分类' /> : <Input value={category ? `${itemTypeLabel(category.itemType, category.itemTypeText, itemTypeOptions)} · ${category.name ?? '未知名称'}${category.isValid === false ? '（已停用）' : ''}` : '未知分类'} disabled />}</Form.Item>
      <Form.Item label='分组名称' name='name' rules={[{ required: true, whitespace: true, message: '分组名称必填' }]}><Input placeholder='同一分类下唯一' /></Form.Item>
      <Form.Item label='备注' name='remark'><Input.TextArea rows={3} placeholder='选填' /></Form.Item>
      {modal.kind === 'editGroup' && <Typography.Text type='secondary'>启停状态由节点上的独立操作改变，不在此表单内。</Typography.Text>}
    </Form>
  </CatalogFormModal>
}

function CreateItemModal({ open, scope, data, onCancel, onSaved }: { open: boolean; scope: TreeScope; data: CatalogData; onCancel: () => void; onSaved: (request: () => Promise<void>) => Promise<void> }) {
  const client = useApiClientContext()
  const [form] = Form.useForm<ItemFormValues>()
  const [submitting, setSubmitting] = useState(false)
  const { categoryId, groupId, itemType } = scope
  const selectedCategoryId = Form.useWatch('categoryId', form)
  // 分类选项文案与目录树同源：随行文案缺失时取枚举元数据，而不是本地兜底，避免同屏两种中文。
  const itemTypeOptions = useItemTypeOptions()

  useEffect(() => {
    if (!open) return
    form.resetFields()
    /*
     * 预选口径：优先取左侧范围传入的分类；分类为空（类型根/全部范围打开）或该分类已停用时，
     * 退回「当前类型下第一个启用分类」——只有类型也为空（全部范围）才取全局第一个启用分类，
     * 避免在检查范围打开时把项目默认落到检验分类下。
     */
    const firstCategory =
      data.categories.find((value) => value.id === categoryId && value.isValid === true) ??
      data.categories.find((value) => value.isValid === true && (itemType === null || value.itemType === itemType))
    // 左侧选到分组时把该分组一并预选；分组停用或不属于预选分类时不预选（停用分组在下拉中禁选）。
    const initialGroup = data.groups.find((value) => value.id === groupId && value.categoryId === firstCategory?.id && value.isValid === true)
    form.setFieldsValue({ categoryId: firstCategory?.id ?? '', groupId: initialGroup?.id ?? '', code: '', name: '', remark: '' })
  }, [categoryId, data.categories, data.groups, form, groupId, itemType, open])

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

  const categoryOptions = data.categories.map((value) => ({ value: value.id ?? '', label: `${itemTypeLabel(value.itemType, value.itemTypeText, itemTypeOptions)} · ${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))
  const groupOptions = data.groups.filter((value) => value.categoryId === selectedCategoryId).map((value) => ({ value: value.id ?? '', label: `${value.name ?? '未知名称'}${value.isValid === false ? '（已停用）' : ''}`, disabled: value.id === null || value.isValid !== true }))
  // 所选分类有分组但全部停用时给出提示，区分「暂无分组」与「有分组但没有启用的」两种阻塞原因。
  const categoryGroups = selectedCategoryId ? data.groups.filter((value) => value.categoryId === selectedCategoryId) : []
  const hintNoEnabledGroup = categoryGroups.length > 0 && categoryGroups.every((value) => value.isValid !== true)
  const groupExtra = '只列出所选分类下的分组；切换分类会清空已选分组。'

  return <CatalogFormModal title='新增标准项目' open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting}>
    <Form form={form} layout='vertical'>
      <Form.Item label='所属分类' name='categoryId' rules={[{ required: true, message: '所属分类必填' }]}><Select options={categoryOptions} placeholder='选择启用状态的分类' onChange={() => form.setFieldsValue({ groupId: '' })} /></Form.Item>
      <Form.Item label='所属分组' name='groupId' rules={[{ required: true, message: '所属分组必填' }]} extra={hintNoEnabledGroup ? `${groupExtra}该分类下没有启用状态的分组，暂无法新增项目。` : groupExtra}><Select options={groupOptions} placeholder='选择启用状态的分组' notFoundContent='该分类下暂无分组' /></Form.Item>
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
  // 树侧显示停用：控制节点可见性与选中回退；与列表侧 ItemFilter.showDisabled 正交——
  // 后者只按项目自身 IsValid 过滤右表。两侧 UI 文案同为「显示停用」（S1-D36），代码层以名字区分。
  const [showDisabledNodes, setShowDisabledNodes] = useState(true)
  const [collapsedKeys, setCollapsedKeys] = useState<Key[]>([])
  const [filter, setFilter] = useState<ItemFilter>(EMPTY_ITEM_FILTER)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })

  const categoryById = useMemo(() => new Map(data.categories.map((value) => [value.id, value])), [data.categories])
  const groupById = useMemo(() => new Map(data.groups.map((value) => [value.id, value])), [data.groups])
  /*
   * 目录树节点与范围文案只拿得到枚举数值，没有随行文案，因此中文取枚举元数据
   * （服务端枚举声明）；元数据不可用或尚未到达时用兜底选项。列表「类型」列已按 S1-D40 移除，
   * 枚举文案来源规则不再涉及列表列。
   */
  const itemTypeOptions = useItemTypeOptions()
  // 与 itemTypeOptions 组合的 options 每次渲染新建会让 Segmented 全量子树重渲染
  // （见 useItemTypeOptions 返回值注释）。
  const typeSegmentedOptions = useMemo<{ value: string | ItemTypeValue; label: string }[]>(() => [{ value: 'all', label: '全部' }, ...itemTypeOptions], [itemTypeOptions])
  const activeScope = useMemo(() => resolveTreeScope(data, scope, showDisabledNodes), [data, scope, showDisabledNodes])
  const selectedCategory = activeScope.categoryId === null ? null : categoryById.get(activeScope.categoryId) ?? null
  const selectedGroup = activeScope.groupId === null ? null : groupById.get(activeScope.groupId) ?? null
  const visibleItems = useMemo(() => filterItems(data, activeScope, filter), [activeScope, data, filter])
  const visibleCategories = useMemo(() => visibleCatalogCategories(data, showDisabledNodes, treeKeyword), [data, showDisabledNodes, treeKeyword])

  /*
   * 已知边界：目录树按 ITEM_TYPES(0/1) 建根，`itemType` 为未知值的分类在任何作用域下都不上树，
   * 因而既不可查看也不可编辑。适配层已把非法枚举映射为 null 并保留该行，数据不会丢失，只是无位置呈现。
   * 该值只能由外部系统或直接写库产生（应用自身保存前校验为 0/1，表上无 CHECK 约束），
   * 属已登记的已知边界而非待修缺陷；若将来接入其他来源的数据，需在此处补「未知类型」根节点。
   */
  const treeData = useMemo<TreeDataNode[]>(() => {
    const types = activeScope.itemType === null ? ITEM_TYPES : [activeScope.itemType]
    /*
     * 节点标题为固定槽位单行（S1-D33）：名称在左按宽度截断省略、被截断时悬停 Tooltip 全名
     * 且延迟 0.5 秒（S1-D38）；状态标记与操作图标放进最右 `.standard-catalog-node-side`
     * 槽位，常显且不随名称长度推移。
     */
    return types.map((itemType) => {
      const typeLabel = itemTypeLabel(itemType, null, itemTypeOptions)
      return {
        key: `t:${itemType}`,
        title: catalogNodeTitle(typeLabel, <Tooltip title={`新增${typeLabel}分类`}><Button type='text' size='small' icon={<PlusOutlined />} aria-label={`新增${typeLabel}分类`} onClick={(event) => { event.stopPropagation(); setModal({ kind: 'createCategory', itemType }) }} /></Tooltip>),
        children: visibleCategories.filter(({ category }) => category.itemType === itemType).map(({ category, groups }) => ({
          key: `c:${category.id ?? `unknown-${category.name ?? 'category'}`}`,
          title: catalogNodeTitle(category.name ?? '未知名称', <>{validTag(category.isValid)}{nodeActions(category, '分类', setModal)}</>),
          children: groups.map((group) => ({ key: `g:${group.id ?? `unknown-${group.name ?? 'group'}`}`, title: catalogNodeTitle(group.name ?? '未知名称', <>{validTag(group.isValid)}{nodeActions(group, '分组', setModal)}</>) })),
        })),
      }
    })
  }, [activeScope.itemType, itemTypeOptions, visibleCategories])

  // 展开键保留全部类型根（含当前范围未渲染的另一类型根）：键集与 rc-tree 展开动画的对比行为耦合，
  // 收窄键集会使其产生空的 motion 节点组、子节点在 jsdom 测试环境挂载为空。
  const expandedKeys = useMemo(() => [...ITEM_TYPES.map((itemType) => `t:${itemType}`), ...data.categories.map((category) => `c:${category.id ?? `unknown-${category.name ?? 'category'}`}`)].filter((key) => !collapsedKeys.includes(key)), [collapsedKeys, data.categories])
  const reloadAndResolve = useCallback(async () => {
    const nextData = await reload()
    setScope((current) => resolveTreeScope(nextData, current, showDisabledNodes))
  }, [reload, showDisabledNodes])
  const saveAndReload = async (request: () => Promise<void>) => { await request(); setModal({ kind: 'none' }); await reloadAndResolve() }
  const dependencyReady = failedSources.size === 0 && !loading

  const columns = useMemo<TableColumnsType<StandardItem>>(() => [
    // 编码、名称、分类、分组、备注五列统一单行省略 + 被截断时悬停 Tooltip 完整值（S1-D33、S1-D38）。
    { title: '编码', dataIndex: 'code', width: 180, className: 'standard-catalog-cell-ellipsis', render: ellipsisCell },
    { title: '名称', dataIndex: 'name', width: 180, className: 'standard-catalog-cell-ellipsis', render: ellipsisCell },
    { title: '分类', width: 150, className: 'standard-catalog-cell-ellipsis', render: (_, item) => ellipsisCell(categoryById.get(item.categoryId)?.name) },
    { title: '分组', width: 150, className: 'standard-catalog-cell-ellipsis', render: (_, item) => ellipsisCell(groupById.get(item.groupId)?.name) },
    // 状态合并列（S1-D39）。
    { title: '状态', width: 105, render: (_, item) => renderItemStatus(data, item) },
    { title: '备注', dataIndex: 'remark', width: 180, className: 'standard-catalog-cell-ellipsis', render: ellipsisCell },
    /*
     * 操作列统一 link 小号文字按钮：修改备注是高频主操作，直接横排；停用/启用低频且具破坏性面，
     * 收进「更多」下拉。停用保持 danger 红色，二次确认仍由既有 ToggleModal 承担（停用时确认按钮
     * danger），操作列不重复确认逻辑；菜单项禁用口径与旧按钮一致（无标识或状态未知不可启停）。
     */
    {
      title: '操作',
      width: 130,
      fixed: 'right',
      render: (_, item) => <Space size={2}>
        <Button type='link' size='small' disabled={item.id === null} onClick={() => setModal({ kind: 'editItemRemark', item })}>修改备注</Button>
        <Dropdown trigger={['click']} menu={{
          items: [{ key: 'toggle', label: item.isValid === true ? '停用' : '启用', danger: item.isValid === true, disabled: item.id === null || item.isValid === null }],
          onClick: ({ key }) => {
            if (key === 'toggle' && item.id !== null && item.isValid !== null) setModal({ kind: 'toggle', kindName: '标准项目', id: item.id, name: item.name ?? '未知名称', isValid: item.isValid })
          },
        }}><Button type='link' size='small'>更多</Button></Dropdown>
      </Space>,
    },
  ], [categoryById, data, groupById])

  // 横向滚动宽度取列宽之和：加列只改列定义一处。
  const tableScrollX = useMemo(() => columns.reduce((total, column) => total + (typeof column.width === 'number' ? column.width : 0), 0), [columns])

  const scopeText = [activeScope.itemType === null ? null : itemTypeLabel(activeScope.itemType, null, itemTypeOptions), selectedCategory?.name, selectedGroup?.name].filter((value): value is string => Boolean(value)).join(' / ') || '全部'
  const selectedKeys = activeScope.groupId !== null ? [`g:${activeScope.groupId}`] : activeScope.categoryId !== null ? [`c:${activeScope.categoryId}`] : activeScope.itemType !== null ? [`t:${activeScope.itemType}`] : []

  return <div className='standard-catalog-page'>
    <div className='standard-catalog-header'><div><Typography.Title level={3}>标准项目目录维护</Typography.Title><Typography.Text type='secondary'>分类、分组与标准项目</Typography.Text></div><Space><Tooltip title='重新读取目录'><Button icon={<ReloadOutlined />} aria-label='重新读取目录' loading={loading} onClick={() => void reloadAndResolve()} /></Tooltip><Button type='primary' icon={<PlusOutlined />} disabled={!dependencyReady || (selectedCategory !== null && selectedCategory.isValid !== true)} onClick={() => setModal({ kind: 'createItem', scope: activeScope })}>新增项目</Button></Space></div>
    {failedSources.size > 0 && <Alert type='warning' showIcon message='目录数据待刷新' description='部分目录读取失败，已有数据保留；依赖未就绪的维护操作暂不可用。' action={<Button size='small' onClick={() => void reloadAndResolve()}>重试</Button>} />}
    <div className='standard-catalog-layout'>
      <section className='standard-catalog-directory' aria-label='标准目录'>
        <div className='standard-catalog-section-header'><Typography.Title level={5}>目录</Typography.Title><Checkbox checked={showDisabledNodes} onChange={(event) => { const checked = event.target.checked; setShowDisabledNodes(checked); setScope((current) => resolveTreeScope(data, current, checked)) }}>显示停用</Checkbox></div>
        <Input prefix={<SearchOutlined />} placeholder='分类或分组名称' aria-label='搜索分类或分组名称' allowClear value={treeKeyword} onChange={(event) => setTreeKeyword(event.target.value)} />
        <Segmented<string | ItemTypeValue> block aria-label='目录项目类型' value={activeScope.itemType ?? 'all'} options={typeSegmentedOptions} onChange={(value) => setScope({ itemType: value === 'all' ? null : isMedicalItemTypeValue(value) ? value : null, categoryId: null, groupId: null })} />
        <div className='standard-catalog-tree-scroll'><Spin spinning={loading && data.categories.length === 0}><Tree aria-label='分类分组目录树' blockNode treeData={treeData} expandedKeys={expandedKeys} selectedKeys={selectedKeys} autoExpandParent={false} onExpand={(_, info) => setCollapsedKeys((keys) => info.expanded ? keys.filter((key) => key !== info.node.key) : [...keys, info.node.key])} onSelect={(keys) => { const key = String(keys[0] ?? ''); if (key.startsWith('t:')) setScope({ itemType: Number(key.slice(2)) === 0 ? 0 : 1, categoryId: null, groupId: null }); if (key.startsWith('c:')) { const category = data.categories.find((value) => value.id === key.slice(2)); if (category?.id !== null && category?.id !== undefined && category?.itemType !== null) setScope({ itemType: category.itemType, categoryId: category.id, groupId: null }) } if (key.startsWith('g:')) { const group = data.groups.find((value) => value.id === key.slice(2)); const category = group ? categoryById.get(group.categoryId) : null; if (group?.id !== null && group?.id !== undefined && category?.id !== null && category?.id !== undefined && category?.itemType !== null) setScope({ itemType: category.itemType, categoryId: category.id, groupId: group.id }) } }} /></Spin>{treeData.every((node) => !node.children || node.children.length === 0) && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的分类或分组' />}</div>
      </section>
      <section className='standard-catalog-list' aria-label='标准项目列表'>
        {/* 标题行只保留标题；范围与计数移到列表区域底部右下角汇总条（S1-D35）。 */}
        <div className='standard-catalog-list-header'><Typography.Title level={5}>标准项目列表</Typography.Title></div>
        {/* 筛选条（S1-D36）：单一关键字框 + 「显示停用」+ 紧跟条件组的 text 小号重置，一行左对齐。 */}
        <div className='standard-catalog-filters'>
          <Input placeholder='编码或名称' aria-label='编码或名称' allowClear value={filter.keyword} onChange={(event) => setFilter({ ...filter, keyword: event.target.value })} />
          <Checkbox checked={filter.showDisabled} onChange={(event) => setFilter({ ...filter, showDisabled: event.target.checked })}>显示停用</Checkbox>
          <Button type='text' size='small' aria-label='重置列表筛选' onClick={() => setFilter(EMPTY_ITEM_FILTER)}>重置</Button>
        </div>
        {/* 列表卡片（S1-D37）：高度与树区域齐平并撑满剩余视口、有外边框，表体内部滚动；汇总条钉右下角。 */}
        <div className='standard-catalog-list-card'>
          <Table rowKey={(item) => item.id ?? `${item.code ?? 'item'}-${item.name ?? 'unknown'}`} size='small' columns={columns} dataSource={visibleItems} loading={loading && data.items.length === 0} scroll={{ x: tableScrollX, y: 'var(--standard-catalog-table-body-y)' }} tableLayout='fixed' pagination={false} locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='没有符合条件的标准项目' /> }} />
          <div className='standard-catalog-list-summary'>范围：{scopeText} · 共 {visibleItems.length} 项</div>
        </div>
      </section>
    </div>
    {modal.kind === 'createCategory' || modal.kind === 'editCategory' ? <CategoryModal modal={modal} data={data} open onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'createGroup' || modal.kind === 'editGroup' ? <GroupModal modal={modal} data={data} open onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'createItem' ? <CreateItemModal open scope={modal.scope} data={data} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'editItemRemark' ? <ItemRemarkModal open item={modal.item} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
    {modal.kind === 'toggle' ? <ToggleModal open modal={modal} onCancel={() => setModal({ kind: 'none' })} onSaved={saveAndReload} /> : null}
  </div>
}
