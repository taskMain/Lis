/**
 * 金额录入弹窗（平台管理员页与医院管理员页共用）。
 *
 * 边界：本文件只承载弹窗展示与本地金额校验，不发起请求、不读取可信上下文，也不裁决范围与读取状态
 * （两者由页面以 `writeGate` 传入）；写失败不改判也不提示（错误由宿主统一展示）。
 */
import { Alert, Descriptions, Form, Input, Modal } from 'antd'
import { useEffect } from 'react'
import { currentAmountText, parseRecognitionAmount, type RecognitionAmountRow } from './recognitionAmountsApi'

/** 金额校验文案：本地校验与「零写请求」共用同一判据（适配层 `parseRecognitionAmount`）。 */
export const AMOUNT_VALIDATION_MESSAGE = '金额必须是不小于 0 且最多两位小数的数字'

/**
 * 写路径门控：由页面裁决后传入弹窗。
 *
 * 范围不完整、读取失败或读取在途时禁用确定按钮并给出原因，使已经打开的弹窗不会基于不完整数据写出；
 * 门控只影响按钮可用性与提示，不改变提交载荷，也不参与错误处理。
 */
export interface RecognitionAmountWriteGate {
  /** 页面裁决的写能力：范围完整、读取未失败、读取不在途且无在途提交。 */
  canWrite: boolean
  /** 读取失败态；`refreshAfterWrite` 表示写入已成功、随后的重载失败。 */
  loadFailure: null | 'load' | 'refreshAfterWrite'
}

/**
 * 写弹窗被冻结时的可见原因，`null` 表示当前可提交。与页面顶部的阻断提示同口径，
 * 弹窗内只复述页面已裁决的事实，不自行判断范围或读取状态。
 */
function writeBlockedReason(writeGate: RecognitionAmountWriteGate, submitting: boolean): string | null {
  if (writeGate.loadFailure === 'refreshAfterWrite') {
    return '写入已经成功，但重新读取金额列表失败。维护操作暂不可用，请先重试读取。'
  }
  if (writeGate.loadFailure !== null) {
    return '读取失败，已有数据保留。维护操作暂不可用，请先重试读取。'
  }
  // `canWrite` 里还含有「提交在途」这一分量，它不是阻断原因：提交在途只由确定按钮的 loading 表达。
  if (!writeGate.canWrite && !submitting) {
    return '正在读取当前范围的金额列表，完成前不能提交写入。'
  }
  return null
}

/**
 * 金额校验：未填交给必填规则；填了必须是非负且最多两位小数的十进制文本。
 * 判据只取适配层出口，页面不另写一份金额规则。
 */
function validateAmountRule(_rule: unknown, value: unknown): Promise<void> {
  if (typeof value !== 'string' || value.trim().length === 0) return Promise.resolve()
  return parseRecognitionAmount(value) === null
    ? Promise.reject(new Error(AMOUNT_VALIDATION_MESSAGE))
    : Promise.resolve()
}

const AMOUNT_FORM_RULES = [
  { required: true, whitespace: true, message: '金额必填' },
  { validator: validateAmountRule },
]

/** 金额录入弹窗的表单值：只有金额文本，目标标准项目由页面持有的当前行决定。 */
export interface AmountFormValues {
  amount?: string
}

async function isValid(form: { validateFields: () => Promise<unknown> }): Promise<boolean> {
  try {
    await form.validateFields()
    return true
  } catch {
    return false
  }
}

/**
 * 金额录入弹窗：回填取**当前行**的取值（后台刷新交付新金额后随之更新），
 * 未配置行留空，由用户显式录入；同值保存也照常提交，不做本地短路。
 */
export function AmountEntryModal({ row, open, writeGate, onCancel, onSubmit, submitting }: {
  /** 页面按所持标准项目编码从**当前行集**重取的当前行，不是打开弹窗那一刻的快照。 */
  row: RecognitionAmountRow
  open: boolean
  writeGate: RecognitionAmountWriteGate
  onCancel: () => void
  /** 返回的 Promise 由页面负责：拒绝时弹窗与输入保留（页面不刷新、不改判、不提示）。 */
  onSubmit: (values: AmountFormValues) => Promise<void>
  submitting: boolean
}) {
  const [form] = Form.useForm<AmountFormValues>()
  // 未配置行不呈现金额数值，回填空串，避免把「未配置」文本当成金额输入。
  const amountText = row.isAmountConfigured ? currentAmountText(row) : ''

  useEffect(() => {
    if (!open) return
    form.resetFields()
    form.setFieldsValue({ amount: amountText })
  }, [amountText, form, open])

  const submit = async () => {
    if (!(await isValid(form))) return
    await onSubmit(form.getFieldsValue())
  }

  const blockedReason = writeBlockedReason(writeGate, submitting)

  return <Modal
    title='维护互认项目金额'
    open={open}
    // 写请求在途时阻断全部关闭入口：否则用户可以在写入尚未返回时把弹窗关掉，
    // 失败既不改判为可见错误、又没有可回访的弹窗（与既有的写弹窗同一口径）。
    closable={!submitting}
    onCancel={() => {
      if (submitting) return
      onCancel()
    }}
    onOk={() => {
      void submit()
    }}
    okText='保存'
    cancelButtonProps={{ disabled: submitting }}
    okButtonProps={{ disabled: blockedReason !== null }}
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
    <Descriptions
      size='small'
      column={1}
      items={[
        { key: 'project', label: '标准项目', children: `${row.standardProjectName ?? '—'}（${row.standardProjectCode ?? '—'}）` },
        { key: 'scope', label: '组织 / 医院 / 院区', children: `${row.organizationName ?? '—'} / ${row.hospitalName ?? '—'} / ${row.branchName ?? '—'}` },
        { key: 'amount', label: '当前金额', children: currentAmountText(row) },
        { key: 'status', label: '互认配置状态', children: row.configurationStatusText },
        ...(row.unavailableReason === null
          ? []
          : [{ key: 'reason', label: '当前不可用原因', children: row.unavailableReason }]),
      ]}
    />
    <Form form={form} layout='vertical'>
      <Form.Item
        label='金额'
        name='amount'
        rules={AMOUNT_FORM_RULES}
        extra='单位为元，不小于 0 且最多两位小数；同值保存也会提交，保存成功后刷新列表。'
      >
        <Input inputMode='decimal' autoComplete='off' placeholder='不小于 0，最多两位小数…' />
      </Form.Item>
    </Form>
  </Modal>
}
