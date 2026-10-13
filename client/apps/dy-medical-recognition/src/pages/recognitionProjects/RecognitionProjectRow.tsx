/**
 * 列视图表格的两个行级单元格出口：可互认时间单元格与操作单元格，以及可互认时间的行内就地编辑器。
 *
 * 边界：这些出口只渲染各自列的内容并按 props 回调上报用户动作，不读取组织、不发起请求、不做业务判断。
 * 写能力由页面裁决后以 `canWrite` 传入；写被冻结时的可见原因由共享模块 `recognitionProjectWriteGate`
 * 给出，因此行内编辑器与三个弹窗对同一个阻断给出同一种说法，不存在第二份判定次序与文案。
 * 项目列的名称与编码、状态列徽标与原因列文本由页面在列定义内渲染，不经过本文件。
 */
import { CheckCircleOutlined, CheckOutlined, CloseOutlined, StopOutlined } from '@ant-design/icons'
import { Alert, Button, Input, Tooltip, type InputRef } from 'antd'
import { useEffect, useRef, useState } from 'react'
import {
  ENABLED_CONFIGURATION_STATUS,
  RECOGNITION_DURATION_RANGE_TEXT,
  parseRecognitionDurationDays,
  rowDisplayName,
  type RecognitionProjectColumnRow,
} from './recognitionProjectsApi'
import { writeBlockedReason, type RecognitionProjectWriteGate } from './recognitionProjectWriteGate'

export interface RecognitionProjectDurationCellProps {
  /** 本行数据；已配置行的可互认时间入口与编辑器都以它为准。 */
  row: RecognitionProjectColumnRow
  /** 页面裁决的写路径门控；行内编辑的可用性与冻结原因都由它派生。 */
  writeGate: RecognitionProjectWriteGate
  /** 是否有写请求在途。 */
  submitting: boolean
  /** 写能力：组织已对齐、读取未失败、读取不在途、无在途提交。 */
  canWrite: boolean
  /** 本行是否处于可互认时间编辑态；编辑目标由页面按行标识持有。 */
  editing: boolean
  /** 进入可互认时间编辑态。 */
  onStartEdit: () => void
  /** 放弃本次编辑，单元格继续显示当前值。 */
  onCancelEdit: () => void
  /** 提交可互认时间输入。 */
  onSubmitDuration: (durationInput: string) => Promise<void>
}

/**
 * 可互认时间的行内编辑器。
 *
 * 草稿以**当前行集**为准：调用方按当前行的可互认时间给出 `key`，后台刷新交付新值或该行被替换时
 * 编辑器随之重新挂载，输入框取到的是当前行的天数，因此输入框与单元格上的数值不会分别来自两个时刻。
 * 代价是后台刷新会覆盖尚未提交的输入，这与「页面持有的当前行是唯一事实来源」的口径一致。
 *
 * 解析失败时不下发请求、保留用户输入，并就地显示取值范围提示，使用户能对照提示自行修改；
 * 回车与保存按钮走同一条提交路径，`Escape` 与取消按钮都只放弃本次编辑。
 * 保存与取消为图标按钮，悬停提示与可访问名同为「保存可互认时间：…」「取消修改可互认时间：…」，
 * 禁用态的悬停提示由包裹的 span 承接，与「互认项目金额维护」页行内编辑器同款式。
 */
function RecognitionDurationEditor({ name, currentDays, submitting, blockedReason, onSubmit, onCancel }: {
  /** 行名称；编辑控件的可访问名称由它组成，与单元格上的可互认时间入口指向同一行。 */
  name: string
  /** 当前行的可互认时间；编辑器挂载时作为输入初值。 */
  currentDays: number | null
  submitting: boolean
  /** 写被冻结时的可见原因；`null` 表示当前可提交。 */
  blockedReason: string | null
  onSubmit: (durationInput: string) => void
  onCancel: () => void
}) {
  const [durationInput, setDurationInput] = useState(currentDays === null ? '' : String(currentDays))
  /** 本次输入被拒绝的原因；`null` 表示尚未被拒绝。 */
  const [durationError, setDurationError] = useState<string | null>(null)
  const durationInputRef = useRef<InputRef>(null)

  /** 进入编辑态时把焦点放进输入框：鼠标与键盘都从同一个位置开始，不必再点一次。 */
  useEffect(() => {
    durationInputRef.current?.focus()
  }, [])

  const submitDuration = () => {
    // 写被冻结时回车与保存按钮表现一致：界面已经把原因显示在单元格内，这里不再产生任何提交。
    if (blockedReason !== null) return
    if (parseRecognitionDurationDays(durationInput) === null) {
      setDurationError(RECOGNITION_DURATION_RANGE_TEXT)
      return
    }
    setDurationError(null)
    onSubmit(durationInput)
  }

  return <>
    <span className='recognition-projects-days-editor'>
      <Input
        ref={durationInputRef}
        className='recognition-projects-days-input'
        inputMode='numeric'
        autoComplete='off'
        aria-label='可互认时间（天）'
        value={durationInput}
        status={durationError === null ? undefined : 'error'}
        // 写请求在途时输入框与取消按钮一并禁用：与写弹窗同一口径，
        // 提交尚未返回时不允许收起这个界面，失败否则既无处可见、也无从回访。
        disabled={submitting}
        onChange={(event) => {
          setDurationInput(event.target.value)
          setDurationError(null)
        }}
        onPressEnter={submitDuration}
        onKeyDown={(event) => {
          if (event.key !== 'Escape') return
          event.preventDefault()
          onCancel()
        }}
      />
      {/* disabled 按钮不派发悬停事件，Tooltip 以 span 为直接子元素承接悬停（antd 官方模式），
          禁用与否结构恒定，悬停提示在写门控与提交期间同样出现；款式与「互认项目金额维护」页行内编辑器一致。 */}
      <Tooltip title={`保存可互认时间：${name}`}>
        <span>
          <Button
            type='primary'
            size='small'
            icon={<CheckOutlined />}
            aria-label={`保存可互认时间：${name}`}
            disabled={blockedReason !== null}
            loading={submitting}
            onClick={submitDuration}
          />
        </span>
      </Tooltip>
      <Tooltip title={`取消修改可互认时间：${name}`}>
        <span>
          <Button
            size='small'
            icon={<CloseOutlined />}
            aria-label={`取消修改可互认时间：${name}`}
            disabled={submitting}
            onClick={onCancel}
          />
        </span>
      </Tooltip>
    </span>
    {/* 异步提示放在常驻的 live region 里：容器先于内容存在，读屏软件才收得到这次变化。 */}
    <div aria-live='polite'>
      {durationError === null ? null : <Alert
        className='recognition-projects-days-error'
        type='warning'
        showIcon
        title='可互认时间输入不合法'
        description={durationError}
      />}
      {blockedReason === null ? null : <Alert
        className='recognition-projects-days-error'
        type='warning'
        showIcon
        title='写入已暂停'
        description={blockedReason}
      />}
    </div>
  </>
}

/**
 * 写目标的公共派生：两个列级出口都按同一口径取行名称与可写性。
 * 可写性除页面裁决的写能力外，还要求本行确实有配置标识与已知配置状态，否则写请求没有目标；
 * 未配置行不渲染这两个出口的任何控件，走不到这里。
 */
function resolveRowWriteTarget(row: RecognitionProjectColumnRow, canWrite: boolean): { name: string; writable: boolean } {
  return {
    name: rowDisplayName(row),
    writable: canWrite && row.configurationId !== null && row.configurationStatus !== null,
  }
}

/**
 * 可互认时间单元格：编辑态下渲染编辑器，其余时候渲染值按钮。
 * 编辑态同时要求本行已配置：未配置行没有可互认时间，也没有可提交的配置标识。
 */
export function RecognitionProjectDurationCell({ row, writeGate, submitting, canWrite, editing, onStartEdit, onCancelEdit, onSubmitDuration }: RecognitionProjectDurationCellProps) {
  const { name, writable } = resolveRowWriteTarget(row, canWrite)
  const currentDays = row.recognitionDurationDays
  const blockedReason = writeBlockedReason(writeGate, submitting)

  if (editing && row.configured) {
    return <RecognitionDurationEditor
      key={currentDays === null ? 'empty' : String(currentDays)}
      name={name}
      currentDays={currentDays}
      submitting={submitting}
      blockedReason={blockedReason}
      onSubmit={onSubmitDuration}
      onCancel={onCancelEdit}
    />
  }

  return row.configured ? <button
    type='button'
    className='recognition-projects-days-value'
    aria-label={`修改可互认时间：${name}`}
    disabled={!writable}
    onClick={onStartEdit}
  >
    <span>{currentDays ?? '—'}</span><span className='unit'>天</span>
  </button> : <span className='recognition-projects-days-value'>—</span>
}

export interface RecognitionProjectToggleCellProps {
  /** 本行数据；启停按钮的目标状态与可访问名称都按它派生。 */
  row: RecognitionProjectColumnRow
  /** 写能力：组织一致、读取未失败、读取不在途、无在途提交。 */
  canWrite: boolean
  /** 启停动作标签，由页面按枚举元数据给出。 */
  actionText: (nextEnabled: boolean) => string
  /** 请求启停确认。 */
  onToggle: () => void
}

/**
 * 操作单元格：启停图标按钮。
 * 未配置行不渲染按钮——启停没有明确的目标状态，也没有可提交的配置标识。
 */
export function RecognitionProjectToggleCell({ row, canWrite, actionText, onToggle }: RecognitionProjectToggleCellProps) {
  const { name, writable } = resolveRowWriteTarget(row, canWrite)
  const stopsConfiguration = row.configurationStatus === ENABLED_CONFIGURATION_STATUS
  const statusText = row.configurationStatusText ?? '未知'

  if (!row.configured) return null

  return <Tooltip title={row.configurationStatus === null ? `配置状态${statusText}` : `${actionText(!stopsConfiguration)}配置`}>
    <Button
      type='text'
      size='small'
      danger={stopsConfiguration}
      icon={stopsConfiguration ? <StopOutlined /> : <CheckCircleOutlined />}
      aria-label={`${actionText(!stopsConfiguration)}配置：${name}（当前${statusText}）`}
      disabled={!writable}
      onClick={onToggle}
    />
  </Tooltip>
}
