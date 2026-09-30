/**
 * 互认项目页面的写弹窗（批量新增、启停确认）。
 *
 * 两个弹窗各有独立提交载荷，共用 `RecognitionProjectModal` 外壳的「提交在途时禁止关闭 →
 * 门控冻结确定按钮并给出原因」规则；可互认时间的修改在项目行内就地完成（见 `RecognitionProjectRow`），
 * 不由弹窗承担。
 *
 * 批量新增弹窗复用页面的四栏列视图（`RecognitionProjectCascade`），把层级格用作整组勾选：
 * 选择与统一可互认时间都由页面持有，弹窗只按 props 渲染并上报用户动作，因此提交成功与失败
 * 都不需要在弹窗内保存草稿，页面也不会因为弹窗卸载而丢掉用户的选择。
 *
 * 边界：本文件只承载弹窗展示与本地校验，不发起请求、不读取组织，也不自行裁决组织一致与读取状态
 * （两者由页面以 `writeGate` 传入）；写失败不改判也不提示（错误由宿主统一展示）。
 */
import {
  Alert,
  Checkbox,
  Input,
  Modal,
  Typography,
} from 'antd'
import { useState, type ReactNode } from 'react'
import { RecognitionProjectCascade, type RecognitionProjectHighlight } from './RecognitionProjectCascade'
import {
  RECOGNITION_DURATION_RANGE_TEXT,
  parseRecognitionDurationDays,
  rowDisplayName,
  type RecognitionProjectColumnRow,
  type RecognitionProjectColumnView,
  type RecognitionProjectRowIdentityFields,
} from './recognitionProjectsApi'
import { writeBlockedReason, type RecognitionProjectWriteGate } from './recognitionProjectWriteGate'

/** 层级格勾选态：一个都没选、选了一部分、全部选中，三态在控件上分别表现为未选中、半选与选中。 */
type LevelSelectionState = 'none' | 'partial' | 'all'

/**
 * 层级格包含的全部标准项目编码。层级格点击与层级格复选框都按这个范围整组勾选，
 * 因此两者对同一格给出同一个结果，不存在两套范围判定。
 */
function levelScopeCodes(view: RecognitionProjectColumnView, target: RecognitionProjectHighlight): string[] {
  const codes: string[] = []
  for (const itemTypeBlock of view) {
    if (itemTypeBlock.itemType !== target.itemType) continue
    for (const category of itemTypeBlock.categories) {
      if (target.level !== 'itemType' && category.categoryName !== target.categoryName) continue
      for (const group of category.groups) {
        if (target.level === 'group' && group.groupName !== target.groupName) continue
        for (const row of group.rows) {
          if (row.standardProjectCode !== null && row.standardProjectCode.length > 0) codes.push(row.standardProjectCode)
        }
      }
    }
  }
  return codes
}

/** 层级格的勾选态：按该层级下的编码集合与当前已选集合比对，部分选中即半选。 */
function levelSelectionState(codes: readonly string[], selection: ReadonlySet<string>): LevelSelectionState {
  if (codes.length === 0) return 'none'
  const selectedCount = codes.filter((code) => selection.has(code)).length
  if (selectedCount === 0) return 'none'
  return selectedCount === codes.length ? 'all' : 'partial'
}

/**
 * 弹窗里的项目行：只承载复选与项目名称、编码。
 * 页面的行内操作（修改可互认时间、启停）属于已配置项目，这些项目不进入可选集合，因此这里不重复它们。
 */
function BatchSelectableRow({ row, selected, onToggle }: {
  row: RecognitionProjectColumnRow
  selected: boolean
  onToggle: () => void
}) {
  const code = row.standardProjectCode ?? ''
  const name = rowDisplayName(row)
  return <div className='recognition-projects-row-content'>
    <Checkbox
      aria-label={`选择标准项目：${name}（${code}）`}
      checked={selected}
      onChange={onToggle}
    />
    <span className='recognition-projects-row-name'>
      <span className='recognition-projects-row-name-text' title={name}>{name}</span>
      <span className='recognition-projects-row-code' title={code}>{code}</span>
    </span>
  </div>
}

/**
 * 两个弹窗共用的外壳：统一「提交在途时禁止关闭」与「门控冻结确定按钮并给出原因」两条规则。
 */
function RecognitionProjectModal({ title, open, onCancel, onSubmit, submitting, writeGate, okText = '保存', okButtonProps, width, children }: {
  title: string
  open: boolean
  onCancel: () => void
  onSubmit: () => Promise<void>
  submitting: boolean
  writeGate: RecognitionProjectWriteGate
  okText?: string
  okButtonProps?: { danger?: boolean }
  /** 弹窗宽度；列视图需要容纳四栏，不传时用框架默认宽度。 */
  width?: number
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
    width={width}
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

/**
 * 批量新增互认配置弹窗：在四栏列视图上多选标准项目，一次提交全部所选项目。
 *
 * `view` 只包含当前组织未配置、且目录有效的标准项目；已配置项目（含停用配置）与目录停用项目
 * 都由页面在构造 `view` 时排除，弹窗不做过滤判断。
 * 选择与天数由页面持有：弹窗被卸载重开时选择仍在页面状态里，因此清空时机由页面在打开弹窗时掌握。
 */
export function RecognitionProjectBatchCreateModal({
  open,
  view,
  catalogReady,
  selection,
  durationInput,
  writeGate,
  submitting,
  onSelectionChange,
  onDurationChange,
  onCancel,
  onSubmit,
}: {
  open: boolean
  /** 可选集合的列视图；空视图由 `catalogReady` 区分「目录待刷新」与「确实没有可新增项」。 */
  view: RecognitionProjectColumnView
  /** 标准目录是否已读取就绪；未就绪与目录确实为空必须给出不同提示。 */
  catalogReady: boolean
  /** 当前已选的标准项目编码集合。 */
  selection: ReadonlySet<string>
  /** 统一可互认时间的输入原文；解析失败时页面据此零写请求。 */
  durationInput: string
  writeGate: RecognitionProjectWriteGate
  submitting: boolean
  /** 选择变化时上报新的编码集合；弹窗不自行保存选择。 */
  onSelectionChange: (selection: ReadonlySet<string>) => void
  /** 天数输入变化时上报原文；弹窗不自行保存输入。 */
  onDurationChange: (durationInput: string) => void
  onCancel: () => void
  onSubmit: () => Promise<void>
}) {
  /**
   * 本次点击确定被拒绝的本地原因：未勾选任何项目、或统一天数不合法。
   * 只作弹窗内的即时反馈，不参与请求构造——真正的零写请求由页面按载荷构造结果拦截。
   */
  const [inputError, setInputError] = useState<string | null>(null)

  const changeSelection = (next: ReadonlySet<string>) => {
    setInputError(null)
    onSelectionChange(next)
  }

  const toggleCode = (code: string) => {
    const next = new Set(selection)
    if (next.has(code)) next.delete(code)
    else next.add(code)
    changeSelection(next)
  }

  /** 整组勾选：该层级已全选则整组取消，否则整组补齐。 */
  const toggleLevel = (target: RecognitionProjectHighlight) => {
    const codes = levelScopeCodes(view, target)
    if (codes.length === 0) return
    const next = new Set(selection)
    const clearAll = codes.every((code) => next.has(code))
    for (const code of codes) {
      if (clearAll) next.delete(code)
      else next.add(code)
    }
    changeSelection(next)
  }

  /**
   * 层级格的整组勾选控件：与层级格共用同一个可访问名称，键盘可达。
   * 外层 `span` 只负责阻断点击冒泡——层级格本身也是整组勾选的入口，
   * 不阻断会让一次点击同时触发复选框与层级格，勾选结果被翻转两次而回到原状。
   */
  const renderLevelCheckbox = (target: RecognitionProjectHighlight, levelName: string) => {
    const codes = levelScopeCodes(view, target)
    if (codes.length === 0) return null
    const state = levelSelectionState(codes, selection)
    return <span
      className='recognition-projects-level-check'
      onClick={(event) => event.stopPropagation()}
    >
      <Checkbox
        aria-label={levelName}
        checked={state === 'all'}
        indeterminate={state === 'partial'}
        onChange={() => toggleLevel(target)}
      />
    </span>
  }

  const submit = async () => {
    // 两条本地拦截都保留输入与选择，且不发出写请求：未勾选时无处提交，天数非法时提交必被服务端拒绝。
    if (selection.size === 0) {
      setInputError('请先勾选要新增的标准项目')
      return
    }
    if (parseRecognitionDurationDays(durationInput) === null) {
      setInputError(RECOGNITION_DURATION_RANGE_TEXT)
      return
    }
    setInputError(null)
    await onSubmit()
  }

  return <RecognitionProjectModal
    title='新增互认项目配置'
    open={open}
    // 四栏列视图需要横向空间：宽度按列视图的最小宽度给出，避免列被压缩。
    width={1180}
    onCancel={onCancel}
    onSubmit={submit}
    submitting={submitting}
    writeGate={writeGate}
    okText='确定新增'
  >
    <div className='recognition-projects-batch-body'>
      <div className='recognition-projects-cascade-wrap'>
        <RecognitionProjectCascade
          view={view}
          highlight={null}
          onLevelClick={toggleLevel}
          levelActionText='整组勾选'
          renderLevelExtra={renderLevelCheckbox}
          renderRow={(row) => <BatchSelectableRow
            row={row}
            selected={row.standardProjectCode !== null && selection.has(row.standardProjectCode)}
            onToggle={() => {
              if (row.standardProjectCode !== null) toggleCode(row.standardProjectCode)
            }}
          />}
          // 目录未就绪时不能报「没有可新增的标准项目」：那是另一个事实的结论。
          emptyText={catalogReady ? '没有可新增的标准项目' : '标准目录数据待刷新'}
        />
      </div>
    </div>

    {inputError === null ? null : <Alert
      className='recognition-projects-batch-error'
      type='warning'
      showIcon
      title='无法提交'
      description={inputError}
    />}

    <div className='recognition-projects-batch-footer'>
      <span className='recognition-projects-batch-count'>已选 {selection.size} 项</span>
      <span className='recognition-projects-batch-days'>
        <span>可互认时间（天）</span>
        <Input
          aria-label='可互认时间（天）'
          inputMode='numeric'
          autoComplete='off'
          placeholder='正整数，如 30…'
          value={durationInput}
          disabled={submitting}
          onChange={(event) => {
            setInputError(null)
            onDurationChange(event.target.value)
          }}
        />
      </span>
      <Typography.Text type='secondary'>
        本次新增的全部配置共用这一个可互认时间，单位为天、最小 1 天；从报告时间起按连续 24 小时计算，边界包含。
      </Typography.Text>
    </div>
  </RecognitionProjectModal>
}

/**
 * 启停确认框：取消与关闭都不得产生写请求，确认只修改配置自身状态。
 * `actionText` 由页面按目标状态给出（枚举元数据可用时为服务端声明），弹窗不再自算动作文案。
 * `row` 是页面按所持配置标识从**当前列视图**重取的当前行（不是打开弹窗那一刻的快照）；
 * 本弹窗只用到行的名称与编码，因此入参只要求这一结构子集，配置行与列视图行都能直接传入。
 * `nextEnabled` 同样由页面按该行的**当前**状态取反给出：后台刷新改变该行状态后，
 * 弹窗的动作目标随之更新，不会继续对旧状态提交。
 */
export function ToggleModal({ row, nextEnabled, actionText, open, writeGate, onCancel, onSubmit, submitting }: {
  row: RecognitionProjectRowIdentityFields
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
