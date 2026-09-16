/**
 * 互认项目页面的三个弹窗（新增配置、修改可互认时间、启停确认）。
 *
 * 三个弹窗各有独立提交载荷，但共用同一套「可互认时间」字段规则与「校验 → 提交」流程，
 * 因此把这两处重复收敛为本文件的 `RecognitionDurationFormItem` 与 `RecognitionProjectModal`，
 * 页面只保留状态编排与弹窗开关。
 *
 * 边界：本文件只承载弹窗展示与本地校验，不发起请求、不读取组织，也不自行裁决组织对齐与读取状态
 * （两者由页面以 `writeGate` 传入）；写失败不改判也不提示（错误由宿主统一展示）。
 */
import {
  Alert,
  Descriptions,
  Form,
  Input,
  Modal,
  Select,
  Typography,
} from 'antd'
import { useEffect, type ReactNode } from 'react'
import {
  MAX_RECOGNITION_DURATION_DAYS,
  parseRecognitionDurationDays,
  rowDisplayName,
  rowScopeTexts,
  type RecognitionProjectConfigurationRow,
  type SelectableOptionGroup,
} from './recognitionProjectsApi'

/** 新增配置弹窗的表单值：标准项目编码与可互认时间输入（解析、校验分别由适配层出口与表单规则完成）。 */
export interface CreateFormValues { standardProjectCode?: string; recognitionDurationDays?: string }
/** 修改可互认时间弹窗的表单值：只含可互认时间输入，目标配置由页面持有的行决定。 */
export interface DurationFormValues { recognitionDurationDays?: string }

/**
 * 写路径门控：由页面裁决后传入三个写弹窗。
 *
 * 组织未对齐（可信组织已变化、页面尚未应用，或提交时刻复核现值失败）、读取失败或读取在途时，
 * 写弹窗禁用确定按钮并给出原因，使已经打开的弹窗不会把输入写进旧组织、也不会基于不完整数据提交；
 * 门控只影响按钮可用性与提示，不改变提交载荷，也不参与错误处理。
 */
export interface RecognitionProjectWriteGate {
  /** 页面实际展示的组织是否已与可信组织对齐。 */
  organizationReady: boolean
  /** 页面裁决的写能力：组织对齐、读取未失败、读取不在途且无在途提交。 */
  canWrite: boolean
  /** 读取失败态；`null` 表示本次读取未失败。 */
  loadFailure: null | 'load' | 'refreshAfterWrite'
  /**
   * 页面在**提交时刻**复核可信组织现值后留下的原因；`null` 表示提交期内没有发生过复核失败。
   * 与 `organizationReady` 同属组织类阻断，只由页面裁决，弹窗只负责显示。
   */
  organizationRecheckFailure: string | null
}

/**
 * 写弹窗被冻结时的可见原因，`null` 表示当前可提交。与页面顶部的阻断提示同口径，
 * 弹窗内只复述页面已裁决的事实，不自行判断组织或读取状态。
 *
 * 判定次序固定为「组织未对齐（含提交时刻复核失败）→ 读取失败（写入后的刷新失败单列）→ 读取在途」；
 * `submitting` 不是阻断原因，它只决定确定按钮保持可点（由页面去重）并用 `confirmLoading` 反馈，
 * 因此不再在函数入口提前返回——提交在途期间发生的组织变化、读取失败必须照常可见，不被掩盖。
 */
function writeBlockedReason(writeGate: RecognitionProjectWriteGate, submitting: boolean): string | null {
  // 提交期复核失败与渲染期「组织未对齐」同属组织类阻断，但它是更晚、更具体的事实
  // （渲染期门控可能还是上一帧的结论），因此先给出它。
  if (writeGate.organizationRecheckFailure !== null) {
    return writeGate.organizationRecheckFailure
  }
  if (!writeGate.organizationReady) {
    return '宿主组织已变化，页面尚未对齐到新组织。组织未对齐期间不能发起写入，请先放弃未保存内容并切换组织。'
  }
  if (writeGate.loadFailure === 'refreshAfterWrite') {
    return '写入已经成功，但重新读取配置与标准项目失败。维护操作暂不可用，请先重试读取。'
  }
  if (writeGate.loadFailure !== null) {
    return '读取失败，已有数据保留。维护操作暂不可用，请先重试读取。'
  }
  // `canWrite` 里还含有「提交在途」这一分量，它不是读取类阻断：只在非提交在途时给出读取在途的原因。
  if (!writeGate.canWrite && !submitting) {
    return '正在读取当前组织的配置与标准项目，完成前不能提交写入。'
  }
  return null
}

/**
 * 可互认时间校验：未填交给必填规则；填了必须是 1 与后端 int 上界之间的整数天数
 * （上界与后端 `[Range(1, int.MaxValue)]` 同口径，提示语随上界取值同步）。
 */
function validateDurationRule(_rule: unknown, value: unknown): Promise<void> {
  if (typeof value !== 'string' || value.trim().length === 0) return Promise.resolve()
  return parseRecognitionDurationDays(value) === null
    ? Promise.reject(new Error(`可互认时间为 1 到 ${MAX_RECOGNITION_DURATION_DAYS} 之间的整数天数`))
    : Promise.resolve()
}

const DURATION_FORM_RULES = [
  { required: true, whitespace: true, message: '可互认时间必填' },
  { validator: validateDurationRule },
]

async function isValid(form: { validateFields: () => Promise<unknown> }): Promise<boolean> {
  try {
    await form.validateFields()
    return true
  } catch {
    return false
  }
}

/**
 * 两个弹窗重复的「可互认时间」字段：规则、单位、提示与输入形态只在这里定义一次。
 * 两者的差异只有说明文字，因此以 `extra` 参数表达，其余保持完全一致。
 */
function RecognitionDurationFormItem({ extra }: { extra: string }) {
  return <Form.Item label='可互认时间（天）' name='recognitionDurationDays' rules={DURATION_FORM_RULES} extra={extra}>
    <Input inputMode='numeric' autoComplete='off' placeholder='正整数，如 30…' />
  </Form.Item>
}

/**
 * 三个弹窗共用的外壳：统一「校验 → 提交」流程挂起、写请求在途时的取消/关闭禁用，
 * 以及组织未对齐（含提交时刻复核失败）、读取失败或读取在途时对确定按钮的冻结与原因说明。
 */
function RecognitionProjectModal({ title, open, onCancel, onSubmit, submitting, writeGate, okText = '保存', okButtonProps, children }: {
  title: string
  open: boolean
  onCancel: () => void
  onSubmit: () => Promise<void>
  submitting: boolean
  writeGate: RecognitionProjectWriteGate
  okText?: string
  okButtonProps?: { danger?: boolean }
  children: ReactNode
}) {
  const blockedReason = writeBlockedReason(writeGate, submitting)
  // 写请求在途时阻断全部关闭入口（取消按钮、右上角关闭、遮罩、ESC）：
  // 否则用户可以在写入尚未返回时把弹窗关掉，失败既不改判为可见错误、又没有可回访的弹窗，
  // 只能凭猜测判断这次写入是否生效。禁用后失败仍留在原弹窗内可见，是否重试由用户显式决定。
  // 不用 Modal 的 `loading`：它会把弹窗内容替换成骨架屏，输入值随之丢失、表单重新挂载，
  // 属于超出“提交中禁止关闭”的行为变化，因此只禁关闭入口并保留 `confirmLoading` 的按钮反馈。
  return <Modal
    title={title}
    open={open}
    closable={!submitting}
    onCancel={() => {
      if (submitting) return
      onCancel()
    }}
    onOk={() => { void onSubmit() }}
    okText={okText}
    cancelButtonProps={{ disabled: submitting }}
    okButtonProps={{ ...okButtonProps, disabled: blockedReason !== null }}
    confirmLoading={submitting}
    destroyOnHidden
  >
    {blockedReason === null ? null : <Alert
      type='warning'
      showIcon
      title='写入已暂停'
      description={blockedReason}
      style={{ marginBottom: 12 }}
    />}
    {children}
  </Modal>
}

/** 新增配置弹窗：只提交标准项目编码与正整数天数；组织编码由服务端可信上下文补入。 */
export function CreateConfigurationModal({ open, optionGroups, catalogReady, writeGate, onCancel, onSubmit, submitting }: {
  open: boolean
  optionGroups: SelectableOptionGroup[]
  /** 标准目录是否已读取就绪；未就绪与目录确实为空必须给出不同提示。 */
  catalogReady: boolean
  writeGate: RecognitionProjectWriteGate
  onCancel: () => void
  onSubmit: (values: CreateFormValues) => Promise<void>
  submitting: boolean
}) {
  const [form] = Form.useForm<CreateFormValues>()

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ standardProjectCode: undefined, recognitionDurationDays: '' })
  }, [form, open])

  const submit = async () => {
    if (!(await isValid(form))) return
    await onSubmit(form.getFieldsValue())
  }

  return <RecognitionProjectModal title='新增互认项目配置' open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting} writeGate={writeGate}>
    <Form form={form} layout='vertical'>
      <Form.Item
        label='标准项目'
        name='standardProjectCode'
        rules={[{ required: true, message: '标准项目必填' }]}
        extra='只列出当前有效标准目录的最底层标准项目；当前组织已配置的项目（含停用配置）不可选。'
      >
        <Select
          showSearch
          optionFilterProp='label'
          options={optionGroups}
          placeholder='按分类、分组或项目名称选择…'
          // 目录未就绪时不能报「没有可新增的标准项目」：那是另一个事实的结论。
          notFoundContent={catalogReady ? '没有可新增的标准项目' : '标准目录数据待刷新'}
        />
      </Form.Item>
      <RecognitionDurationFormItem extra='单位为天，最小 1 天；从报告时间起按连续 24 小时计算，边界包含。' />
    </Form>
  </RecognitionProjectModal>
}

/** 修改可互认时间弹窗：标准项目资料只读，配置停用后仍可修改时间。 */
export function DurationModal({ row, open, writeGate, onCancel, onSubmit, submitting }: {
  /**
   * 目标配置的**当前行**：由页面按所持配置标识从当前行集里重取，不是打开弹窗那一刻的快照。
   * 后台刷新交付新值后，这里的只读资料与表单初始值都取当前行，不会继续显示旧值。
   */
  row: RecognitionProjectConfigurationRow
  open: boolean
  writeGate: RecognitionProjectWriteGate
  onCancel: () => void
  onSubmit: (values: DurationFormValues) => Promise<void>
  submitting: boolean
}) {
  const [form] = Form.useForm<DurationFormValues>()
  const currentDays = row.recognitionDurationDays

  /**
   * 弹窗打开时按当前行回填；行取值在打开期间变化（后台刷新交付了新值）时同步回到当前行取值，
   * 保证输入框与只读资料都以当前事实为准。代价是后台刷新同时会覆盖用户尚未提交的输入，
   * 这是「弹窗不得展示旧快照」的直接结果：输入框不是独立数据源，页面持有的当前行才是。
   */
  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ recognitionDurationDays: currentDays === null ? '' : String(currentDays) })
  }, [currentDays, form, open])

  const submit = async () => {
    if (!(await isValid(form))) return
    await onSubmit(form.getFieldsValue())
  }

  const scopeTexts = rowScopeTexts(row)

  return <RecognitionProjectModal title='修改可互认时间' open={open} onCancel={onCancel} onSubmit={submit} submitting={submitting} writeGate={writeGate}>
    <Descriptions
      size='small'
      column={1}
      items={[
        { key: 'project', label: '标准项目', children: `${rowDisplayName(row)}（${scopeTexts.standardProjectCode}）` },
        { key: 'itemType', label: '项目类型', children: row.itemTypeText },
        { key: 'scope', label: '分类 / 分组', children: `${scopeTexts.categoryName} / ${scopeTexts.groupName}` },
        // 枚举中文取自服务端交付的行文本（与表格 Tag 同源），弹窗不再重复一份数值判定与中文。
        { key: 'status', label: '配置状态', children: row.configurationStatusText },
        ...(row.unavailableReason === null
          ? []
          : [{ key: 'reason', label: '目录停用原因', children: row.unavailableReason }]),
      ]}
    />
    <Form form={form} layout='vertical'>
      <RecognitionDurationFormItem extra='同值保存也会提交；保存成功后刷新列表。配置停用不影响此处修改。' />
    </Form>
  </RecognitionProjectModal>
}

/**
 * 启停确认框：取消与关闭都不得产生写请求，确认只修改配置自身状态。
 * `actionText` 由页面按目标状态给出（枚举元数据可用时为服务端声明），弹窗不再自算动作文案。
 * `row` 是页面按所持配置标识从**当前行集**重取的当前行（不是打开弹窗那一刻的快照），
 * `nextEnabled` 同样由页面按该行的**当前**状态取反给出：后台刷新改变该行状态后，
 * 弹窗的动作目标随之更新，不会继续对旧状态提交。
 */
export function ToggleModal({ row, nextEnabled, actionText, open, writeGate, onCancel, onSubmit, submitting }: {
  row: RecognitionProjectConfigurationRow
  nextEnabled: boolean
  actionText: string
  open: boolean
  writeGate: RecognitionProjectWriteGate
  onCancel: () => void
  onSubmit: () => Promise<void>
  submitting: boolean
}) {
  return <RecognitionProjectModal
    title={`${actionText}互认项目配置`}
    open={open}
    onCancel={onCancel}
    onSubmit={onSubmit}
    submitting={submitting}
    writeGate={writeGate}
    okText={`确认${actionText}`}
    okButtonProps={nextEnabled ? undefined : { danger: true }}
  >
    <Typography.Paragraph>即将{actionText}互认项目配置 <Typography.Text strong>{rowDisplayName(row)}</Typography.Text>。</Typography.Paragraph>
    <Typography.Paragraph type='secondary' style={{ marginBottom: 0 }}>该操作只修改这条配置的启用状态，不影响其它配置。</Typography.Paragraph>
  </RecognitionProjectModal>
}
