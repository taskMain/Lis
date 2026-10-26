/**
 * 「平台参数维护」平台管理员页（宿主路径 `/subApps/medical-recognition/platform-parameters`）。
 *
 * 页面提供三项平台参数的列表与逐条编辑：列表展示参数编码、名称、当前值、说明、启用状态、
 * 最近操作人与时间；编辑按参数编码对应的取值规则在提交前校验，非法值不发保存请求。
 * 保存成功后重新查询列表，页面显示服务端新值，不在前端缓存旧值或按返回值改写行状态。
 * 页面不实现角色判断，错误提示由宿主统一处理，保存失败时保留弹窗与输入。
 */
import { Alert, Button, Card, Form, Input, Modal, Space, Switch, Table, Tag, Typography, message } from 'antd'
import type { TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useHostSession } from '../../session/HostSessionContext'
import {
  PLATFORM_PARAMETER_DEFINITIONS,
  queryPlatformParameters,
  savePlatformParameter,
  validatePlatformParameterValue,
  type PlatformParameterRow,
} from './platformParametersApi'

/** 编辑表单字段。 */
interface ParameterFormValues {
  parameterValue: string
  isValid: boolean
}

/** 参数定义按编码索引，用于编辑弹窗的取值提示。 */
const DEFINITION_BY_CODE = new Map(PLATFORM_PARAMETER_DEFINITIONS.map((definition) => [definition.code, definition]))

/** 最近操作时间展示：服务端为空时显示破折号。 */
function formatOperTime(value: Date | null): string {
  return value === null ? '—' : dayjs(value).format('YYYY-MM-DD HH:mm:ss')
}

export function PlatformParameters() {
  const client = useApiClientContext()
  const session = useHostSession()
  /** 匿名、初始化与错误快照不发起业务请求；登录后按会话版本读取列表。 */
  const authenticated = session.userId !== null
    && (session.status === 'authenticated' || session.status === 'refreshing')
  const sessionKey = `${session.sessionVersion}|${session.userId ?? ''}`
  const [rows, setRows] = useState<PlatformParameterRow[]>([])
  const [loading, setLoading] = useState(true)
  const [loaded, setLoaded] = useState(false)
  const [failed, setFailed] = useState(false)
  const [retryNonce, setRetryNonce] = useState(0)
  const [editing, setEditing] = useState<PlatformParameterRow | null>(null)
  const [saving, setSaving] = useState(false)
  const [form] = Form.useForm<ParameterFormValues>()
  /** 读取序号：只采纳最后一次读取的结果，旧列表响应不覆盖新状态。 */
  const requestSeq = useRef(0)

  useEffect(() => {
    if (!authenticated) return undefined
    const seq = requestSeq.current + 1
    requestSeq.current = seq
    // 推迟到下一个宏任务再进入加载态，避免在 effect 内同步写入渲染状态。
    const task = window.setTimeout(() => {
      setLoading(true)
      void queryPlatformParameters(client).then(
        (next) => {
          if (seq !== requestSeq.current) return
          setRows(next)
          setLoaded(true)
          setFailed(false)
          setLoading(false)
        },
        () => {
          if (seq !== requestSeq.current) return
          // 失败不覆盖已有有效数据；首次失败只显示读取失败与重试入口。
          setFailed(true)
          setLoading(false)
        },
      )
    }, 0)
    return () => window.clearTimeout(task)
  }, [authenticated, client, retryNonce, sessionKey])

  /** 打开编辑弹窗：表单按当前行值初始化（弹窗关闭时销毁内容，重开取新值）；未配置行默认启用。 */
  const openEditor = useCallback((row: PlatformParameterRow) => {
    setEditing(row)
  }, [])

  /** 提交保存：表单规则已在提交前阻断非法值；保存成功后重新查询列表再关闭弹窗。 */
  const handleFinish = useCallback(async (values: ParameterFormValues) => {
    if (editing === null) return
    const validationError = validatePlatformParameterValue(editing.code, values.parameterValue)
    if (validationError !== null) {
      message.error(validationError)
      return
    }

    setSaving(true)
    try {
      const saved = await savePlatformParameter(client, {
        code: editing.code,
        value: values.parameterValue,
        description: editing.description,
        isValid: values.isValid,
      })
      if (!saved) {
        message.warning('参数保存未确认成功，请重试。')
        return
      }
      const seq = requestSeq.current + 1
      requestSeq.current = seq
      const next = await queryPlatformParameters(client)
      if (seq !== requestSeq.current) return
      setRows(next)
      setLoaded(true)
      setFailed(false)
      setEditing(null)
      message.success('参数已保存，下一次读取即生效。')
    } catch {
      // 错误由宿主统一提示；保留弹窗与输入，不误报成功、不重新拉取列表。
    } finally {
      setSaving(false)
    }
  }, [client, editing])

  const columns: TableColumnsType<PlatformParameterRow> = [
    { title: '参数编码', dataIndex: 'code', key: 'code' },
    { title: '参数名称', dataIndex: 'name', key: 'name' },
    {
      title: '当前值',
      key: 'value',
      render: (_value, row) => row.isConfigured && row.value !== null
        ? <Typography.Text code>{row.value}</Typography.Text>
        : <Typography.Text type='secondary'>未配置</Typography.Text>,
    },
    {
      title: '说明',
      key: 'description',
      render: (_value, row) => row.description ?? '—',
    },
    {
      title: '状态',
      key: 'status',
      render: (_value, row) => row.isConfigured
        ? <Tag color={row.isValid ? 'green' : 'red'}>{row.isValid ? '启用' : '停用'}</Tag>
        : <Tag>未配置</Tag>,
    },
    {
      title: '操作人',
      key: 'operId',
      render: (_value, row) => row.operId ?? '—',
    },
    {
      title: '操作时间',
      key: 'operTime',
      render: (_value, row) => formatOperTime(row.operTime),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_value, row) => <Button
        type='link'
        aria-label={`编辑参数：${row.name}`}
        onClick={() => openEditor(row)}
      >编辑</Button>,
    },
  ]

  const editingDefinition = editing === null ? null : DEFINITION_BY_CODE.get(editing.code) ?? null

  return <Card className='platform-parameters-page'>
    <Space orientation='vertical' size='middle' style={{ width: '100%' }}>
      <div>
        <Typography.Title level={3}>平台参数维护</Typography.Title>
        <Typography.Text type='secondary'>维护本院报告匹配开关、排除时长与引用详情有效时长</Typography.Text>
      </div>

      {!authenticated ? <Alert
        type='warning'
        showIcon
        title='当前未登录'
        description='登录后可查看和维护平台参数。'
      /> : null}

      {authenticated && failed ? <Alert
        type='error'
        showIcon
        title='参数列表读取失败'
        description='无法读取平台参数列表，请重试。'
        action={<Button size='small' onClick={() => setRetryNonce((nonce) => nonce + 1)}>重试</Button>}
      /> : null}

      {authenticated && (!failed || loaded) ? <Table<PlatformParameterRow>
        rowKey='code'
        columns={columns}
        dataSource={rows}
        loading={loading}
        pagination={false}
        locale={{ emptyText: '没有平台参数记录' }}
      /> : null}
    </Space>

    {editing === null ? null : <Modal
      open
      title={`编辑参数：${editing.name}`}
      okText='保存'
      cancelText='取消'
      confirmLoading={saving}
      onOk={() => form.submit()}
      onCancel={() => { if (!saving) setEditing(null) }}
      cancelButtonProps={{ disabled: saving }}
    >
      <Form
        key={editing.code}
        form={form}
        layout='vertical'
        initialValues={{
          parameterValue: editing.value ?? '',
          isValid: editing.isConfigured ? editing.isValid : true,
        }}
        onFinish={(values) => { void handleFinish(values) }}
      >
        <Form.Item
          name='parameterValue'
          label='参数值'
          extra={editingDefinition === null ? undefined : `取值：${editingDefinition.valueHint}。`}
          rules={[{
            validator: async (_rule, value: string | undefined) => {
              const error = validatePlatformParameterValue(editing.code, value ?? '')
              if (error !== null) throw new Error(error)
            },
          }]}
        >
          <Input aria-label='参数值' autoComplete='off' />
        </Form.Item>
        <Form.Item name='isValid' label='启用' valuePropName='checked'>
          <Switch aria-label='启用状态' />
        </Form.Item>
      </Form>
    </Modal>}
  </Card>
}
