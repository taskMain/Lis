import { useEffect } from 'react'
import { Form, Input, Modal, Select, Tag, Tooltip, Typography, type FormInstance } from 'antd'
import {
  categoryItemType,
  isCategoryTypeFrozen,
  usageLabel,
  UsageStatus,
  type Category,
  type Group,
  type ItemType,
  type PrototypeData,
  type StandardItem,
  type UsageStatusValue,
} from './standardCatalogPrototypeData'
import { usePrototypeStore } from './standardCatalogPrototypeStore'

const ITEM_TYPE_OPTIONS = [
  { value: '检验' as ItemType, label: '检验' },
  { value: '检查' as ItemType, label: '检查' },
]

interface CategoryFormValues {
  itemType: ItemType
  name: string
  remark: string
}

function toRemark(value: string | undefined): string | null {
  const trimmed = (value ?? '').trim()
  return trimmed === '' ? null : trimmed
}

function fromRemark(value: string | null): string {
  return value ?? ''
}

const FORM_GRID_STYLE = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 } as const

/**
 * 触发一次表单校验，返回是否全部通过。
 *
 * 校验失败时由 antd 自行渲染字段错误，避免在本文件手工拼装字段错误结构
 * （`FormInstance.setFields` 的字段名类型是条件类型，难以在辅助函数里正确表达）。
 */
async function validate<Values extends object>(form: FormInstance<Values>): Promise<boolean> {
  try {
    await form.validateFields()
    return true
  } catch {
    return false
  }
}

/** 新增分类弹窗。 */
export function CreateCategoryModal({ open, onClose, type }: { open: boolean; onClose: () => void; type: ItemType }) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<CategoryFormValues>()

  useEffect(() => {
    if (open) form.setFieldsValue({ itemType: type, name: '', remark: '' })
  }, [open, type, form])

  const submit = async () => {
    if (!(await validate(form))) return
    const values = form.getFieldsValue()
    store.createCategory({ itemType: values.itemType, name: values.name.trim(), remark: toRemark(values.remark) })
    onClose()
  }

  return (
    <Modal title='新增分类' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        <Form.Item label='项目类型' name='itemType' rules={[{ required: true, message: '项目类型必填' }]}>
          <Select options={ITEM_TYPE_OPTIONS} />
        </Form.Item>
        <Form.Item label='分类名称' name='name' rules={[{ required: true, message: '分类名称必填' }]}>
          <Input placeholder='全平台唯一' />
        </Form.Item>
        <Form.Item label='备注' name='remark'>
          <Input.TextArea rows={2} placeholder='选填' />
        </Form.Item>
      </Form>
    </Modal>
  )
}

/**
 * 编辑分类弹窗。
 *
 * 冻结口径：分类存在任何分组（含已停用）时，仅「项目类型」置灰并说明原因；
 * 名称与备注始终可编辑，不受自身启停或已有下级限制。
 * 编辑表单不含启用开关，启停由独立操作改变。
 */
export function EditCategoryModal({
  open,
  onClose,
  data,
  category,
  usage,
}: {
  open: boolean
  onClose: () => void
  data: PrototypeData
  category: Category | null
  usage?: string
}) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<CategoryFormValues>()
  const frozen = category ? isCategoryTypeFrozen(data, category.id) : false

  useEffect(() => {
    if (open && category) {
      form.setFieldsValue({ itemType: category.itemType, name: category.name, remark: fromRemark(category.remark) })
    }
  }, [open, category, form])

  const submit = async () => {
    if (!category) return
    if (!(await validate(form))) return
    const values = form.getFieldsValue()
    store.updateCategory(category.id, {
      itemType: values.itemType,
      name: values.name.trim(),
      remark: toRemark(values.remark),
    })
    onClose()
  }

  return (
    <Modal title='编辑分类' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        {usage !== undefined && <Form.Item label='使用情况'><Typography.Text>{usage}</Typography.Text></Form.Item>}
        <Form.Item
          label='项目类型'
          name='itemType'
          extra={frozen ? '该分类下已存在分组（含已停用），项目类型不可修改。' : undefined}
        >
          <Select disabled={frozen} options={ITEM_TYPE_OPTIONS} />
        </Form.Item>
        <Form.Item label='分类名称' name='name' rules={[{ required: true, message: '分类名称必填' }]}>
          <Input placeholder='全平台唯一，修改时排除自身' />
        </Form.Item>
        <Form.Item label='备注' name='remark'>
          <Input.TextArea rows={2} placeholder='选填' />
        </Form.Item>
        <Typography.Text type='secondary'>启停状态由节点上的独立启用/停用操作改变，不在此表单内。</Typography.Text>
      </Form>
    </Modal>
  )
}

/** 新增分组弹窗：上级分类必须启用。 */
export function CreateGroupModal({
  open,
  onClose,
  data,
  defaultCategoryId,
}: {
  open: boolean
  onClose: () => void
  data: PrototypeData
  defaultCategoryId: string | null
}) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<{ categoryId: string; name: string; remark: string }>()

  useEffect(() => {
    if (open) form.setFieldsValue({ categoryId: defaultCategoryId ?? '', name: '', remark: '' })
  }, [open, defaultCategoryId, form])

  const submit = async () => {
    if (!(await validate(form))) return
    const values = form.getFieldsValue()
    store.createGroup({ categoryId: values.categoryId, name: values.name.trim(), remark: toRemark(values.remark) })
    onClose()
  }

  const categoryOptions = data.categories.map((category) => ({
    value: category.id,
    label: category.isValid ? category.name : `${category.name}（已停用）`,
    disabled: !category.isValid,
  }))

  return (
    <Modal title='新增分组' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        <Form.Item
          label='所属分类'
          name='categoryId'
          rules={[{ required: true, message: '所属分类必填' }]}
          extra='只能挂到启用状态的分类下。'
        >
          <Select options={categoryOptions} placeholder='选择启用状态的分类' />
        </Form.Item>
        <Form.Item
          label='分组名称'
          name='name'
          rules={[{ required: true, whitespace: true, message: '分组名称必填' }]}
        >
          <Input placeholder='同一分类下唯一' />
        </Form.Item>
        <Form.Item label='备注' name='remark'>
          <Input.TextArea rows={2} placeholder='选填' />
        </Form.Item>
      </Form>
    </Modal>
  )
}

/**
 * 编辑分组弹窗。
 *
 * 归属口径：所属分类只读展示，创建后不可改，更新请求也不提交 `CategoryId`；
 * 不提供拖拽或改挂。名称与备注始终可编辑，不受自身、父级启停或已有项目限制。
 */
export function EditGroupModal({
  open,
  onClose,
  data,
  group,
  usage,
}: {
  open: boolean
  onClose: () => void
  data: PrototypeData
  group: Group | null
  usage?: string
}) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<{ name: string; remark: string }>()

  useEffect(() => {
    if (open && group) form.setFieldsValue({ name: group.name, remark: fromRemark(group.remark) })
  }, [open, group, form])

  const submit = async () => {
    if (!group) return
    if (!(await validate(form))) return
    const values = form.getFieldsValue()
    store.updateGroup(group.id, { name: values.name.trim(), remark: toRemark(values.remark) })
    onClose()
  }

  const category = data.categories.find((entry) => entry.id === group?.categoryId)

  return (
    <Modal title='编辑分组' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        {usage !== undefined && <Form.Item label='使用情况'><Typography.Text>{usage}</Typography.Text></Form.Item>}
        <Form.Item label='所属分类' extra='分组创建后归属不可修改，因此不在保存内容中提交。'>
          <Input value={category ? `${category.name}${category.isValid ? '' : '（已停用）'}` : '—'} readOnly disabled />
        </Form.Item>
        <Form.Item label='分组名称' name='name' rules={[{ required: true, message: '分组名称必填' }]}>
          <Input placeholder='同一分类下唯一，修改时排除自身' />
        </Form.Item>
        <Form.Item label='备注' name='remark'>
          <Input.TextArea rows={2} placeholder='选填' />
        </Form.Item>
        <Typography.Text type='secondary'>启停状态由节点上的独立启用/停用操作改变，不在此表单内。</Typography.Text>
      </Form>
    </Modal>
  )
}

/** 新增标准项目弹窗：分类与分组均须启用，切换分类必须清空已选分组。 */
export function CreateItemModal({
  open,
  onClose,
  data,
  defaultCategoryId,
}: {
  open: boolean
  onClose: () => void
  data: PrototypeData
  defaultCategoryId: string | null
}) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<{ categoryId: string; groupId: string; code: string; name: string; remark: string }>()
  // 用表单实际值驱动分组选项，避免额外维护一份镜像 state。
  const categoryId = Form.useWatch('categoryId', form)

  useEffect(() => {
    if (open) {
      const firstEnabled = data.categories.find((category) => category.isValid && category.id === defaultCategoryId)
        ?? data.categories.find((category) => category.isValid)
      form.setFieldsValue({ categoryId: firstEnabled?.id ?? '', groupId: '', code: '', name: '', remark: '' })
    }
  }, [open, defaultCategoryId, data.categories, form])

  const submit = async () => {
    if (!(await validate(form))) return
    const values = form.getFieldsValue()
    store.createItem({
      categoryId: values.categoryId,
      groupId: values.groupId,
      code: values.code.trim(),
      name: values.name.trim(),
      remark: toRemark(values.remark),
    })
    onClose()
  }

  const categoryOptions = data.categories.map((category) => ({
    value: category.id,
    label: category.isValid ? `${category.itemType} · ${category.name}` : `${category.itemType} · ${category.name}（已停用）`,
    disabled: !category.isValid,
  }))
  const groupOptions = data.groups
    .filter((group) => group.categoryId === categoryId)
    .map((group) => ({
      value: group.id,
      label: group.isValid ? group.name : `${group.name}（已停用）`,
      disabled: !group.isValid,
    }))

  return (
    <Modal title='新增标准项目' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        <Form.Item label='所属分类' name='categoryId' rules={[{ required: true, message: '所属分类必填' }]}>
          <Select
            options={categoryOptions}
            placeholder='选择启用状态的分类'
            onChange={() => form.setFieldsValue({ groupId: '' })}
          />
        </Form.Item>
        <Form.Item
          label='所属分组'
          name='groupId'
          rules={[{ required: true, message: '所属分组必填' }]}
          extra='只列出所选分类下的分组；切换分类会清空已选分组。'
        >
          <Select options={groupOptions} placeholder='选择启用状态的分组' notFoundContent='该分类下暂无分组' />
        </Form.Item>
        <div style={FORM_GRID_STYLE}>
          <Form.Item label='标准项目编码' name='code' rules={[{ required: true, message: '编码必填' }]}>
            <Input placeholder='全平台唯一' />
          </Form.Item>
          <Form.Item label='标准项目名称' name='name' rules={[{ required: true, message: '名称必填' }]}>
            <Input />
          </Form.Item>
        </div>
        <Form.Item label='备注' name='remark'>
          <Input.TextArea rows={2} placeholder='选填' />
        </Form.Item>
      </Form>
    </Modal>
  )
}

/**
 * 修改备注弹窗。
 *
 * 标准项目创建后只允许修改备注；编码、名称、分类、分组均不可通过普通修改改变。
 * 清空备注归一为显式 `null`。
 */
export function ItemRemarkModal({
  open,
  onClose,
  item,
}: {
  open: boolean
  onClose: () => void
  item: StandardItem | null
}) {
  const store = usePrototypeStore()
  const [form] = Form.useForm<{ remark: string }>()

  useEffect(() => {
    if (open && item) form.setFieldsValue({ remark: fromRemark(item.remark) })
  }, [open, item, form])

  const submit = () => {
    if (!item) return
    store.updateItemRemark(item.id, toRemark(form.getFieldsValue().remark))
    onClose()
  }

  return (
    <Modal title='修改备注' open={open} onCancel={onClose} onOk={submit} okText='保存' destroyOnHidden>
      <Form form={form} layout='vertical'>
        <Form.Item label='标准项目'>
          <Input value={item ? `${item.name}（${item.code}）` : ''} readOnly disabled />
        </Form.Item>
        <Form.Item label='备注' name='remark' extra='清空即表示无备注；编码、名称、分类、分组不可修改。'>
          <Input.TextArea rows={3} placeholder='选填' />
        </Form.Item>
      </Form>
    </Modal>
  )
}

/** 启停确认框：明确不级联修改下级状态。 */
export function ToggleConfirmModal({
  open,
  onClose,
  onConfirm,
  targetName,
  targetKind,
  targetValid,
}: {
  open: boolean
  onClose: () => void
  onConfirm: () => void
  targetName: string
  targetKind: string
  targetValid: boolean
}) {
  const action = targetValid ? '停用' : '启用'
  return (
    <Modal
      title={`${action}${targetKind}`}
      open={open}
      onCancel={onClose}
      onOk={() => {
        onConfirm()
        onClose()
      }}
      okText={`确认${action}`}
      okButtonProps={targetValid ? { danger: true } : undefined}
      destroyOnHidden
    >
      <Typography.Paragraph>
        即将{action}
        {targetKind}
        <Typography.Text strong>{targetName}</Typography.Text>。
      </Typography.Paragraph>
      <Typography.Paragraph type='secondary' style={{ marginBottom: 0 }}>
        该操作只修改当前数据状态，<Typography.Text strong>不会级联修改下级状态</Typography.Text>
        。下级对象自身的启停状态保持不变；上级停用后，下级对象的「当前有效」会随之失效。
      </Typography.Paragraph>
    </Modal>
  )
}

/** 使用情况标签，取值来自是否存在下级的派生判断（含已停用下级）。 */
export function UsageTag({ usage }: { usage: UsageStatusValue }) {
  if (usage === UsageStatus.InUse) return <Tag color='blue'>{usageLabel(usage)}</Tag>
  return <Tag>{usageLabel(usage)}</Tag>
}

/** 启停状态标签。 */
export function ValidTag({ isValid }: { isValid: boolean }) {
  return isValid ? <Tag color='success'>启用</Tag> : <Tag>停用</Tag>
}

/** 分类的项目类型标签；不存在分组时提示可改，存在时提示已冻结。 */
export function CategoryTypeTag({ data, category }: { data: PrototypeData; category: Category }) {
  const frozen = isCategoryTypeFrozen(data, category.id)
  return (
    <Tooltip title={frozen ? '该分类下已存在分组（含已停用），项目类型不可修改' : '尚无分组，项目类型仍可修改'}>
      <Tag color={categoryItemType(data, category.id) === '检验' ? 'geekblue' : 'cyan'}>{category.itemType}</Tag>
    </Tooltip>
  )
}
