/**
 * 统计页面共享主体（StatisticsBoard）Component 层用例
 * （阶段 6 前端测试矩阵 C9-C15、C17 与 C22 的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. 主体的数据源是四个页面注入的查询与导出入口，用例以内存替身直接实现这些出口，
 *    交付**适配层视图模型**形态的行数据——主体不接触生成契约，替身即真实契约形态；
 * 2. `@dy/components-base` 只替换整个包（该包 ESM 产物无法被 vitest 加载，理由同报告管理页用例），
 *    替身只复现主体依赖的组件契约：受控 `value`、固定 `orgId`/`hosId` 对应级不渲染、
 *    变化经 `onChange` 回传完整范围值；
 * 3. 枚举元数据钩子替换为直接返回兜底选项（元数据请求属 Client 边界 I/O，不属于主体行为面）。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、真实范围选择器交互、宿主统一错误提示（C23-C30）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import type {
  MatchRecordView,
  UsageDetailRow,
  UsageSummaryRow,
} from './recognitionUsageStatisticsApi'
import type {
  SourceDetailRow,
  SourceSummaryRow,
} from './sourceRecognitionStatisticsApi'
import { StatisticsBoard, type StatisticsBoardProps, type StatisticsFilters } from './statisticsBoard'
import type { StatisticsPage, StatisticsPageInput } from './recognitionStatisticsValues'

vi.mock('@dy/components-base', async () => {
  const { createElement } = await import('react')

  /** 范围选择器替身：固定 `orgId`/`hosId` 的对应级不渲染下拉，变化回传完整范围值。选项文案避开夹具名称。 */
  function StubScopeSelector({ value = {}, orgId, hosId, className, onChange }: BaseScopeSelectorProps) {
    const levels: Array<{ key: 'orgId' | 'hosId' | 'branchId'; label: string; options: [string, string][] }> = []
    if (orgId === undefined) levels.push({ key: 'orgId', label: '组织', options: [['ORG1', '组织甲'], ['ORG2', '组织乙']] })
    if (hosId === undefined) levels.push({ key: 'hosId', label: '医院', options: [['HOS1', '医院甲'], ['HOS2', '医院乙']] })
    levels.push({ key: 'branchId', label: '院区', options: [['BRA1', '院区甲'], ['BRA2', '院区乙']] })
    return createElement('div', { className },
      levels.map((level) => createElement('select',
        {
          key: level.key,
          'aria-label': level.label,
          value: value[level.key] ?? '',
          onChange: (event: { target: { value: string } }) => {
            const next = event.target.value
            onChange?.(
              { ...value, [level.key]: next.length > 0 ? next : undefined },
              { organizations: [], hospitals: [], branches: [], depts: [], users: [] },
            )
          },
        },
        createElement('option', { value: '' }, '不限'),
        level.options.map(([id, name]) => createElement('option', { key: id, value: id }, name)),
      )),
    )
  }

  return { configureBaseComponents: () => {}, BaseScopeSelector: StubScopeSelector }
})

vi.mock('../../hooks/useEnumMetadata', () => ({
  useEnumMetadata: (_enumName: string, fallback: readonly { value: number; label: string; name: string }[]) => fallback,
}))

// ========== 数据源替身 ==========

const summaryQuery = vi.fn<(filters: StatisticsFilters, dimension: number, page: StatisticsPageInput) => Promise<StatisticsPage<UsageSummaryRow | SourceSummaryRow>> | null>()
const detailsQuery = vi.fn<(filters: StatisticsFilters, detailType: number, page: StatisticsPageInput) => Promise<StatisticsPage<UsageDetailRow | SourceDetailRow>> | null>()
const exportQuery = vi.fn<(filters: StatisticsFilters, request: { exportType: number; groupDimension: number; detailType: number | null; downloadFileName: string }) => Promise<void> | null>()
const matchRecordQuery = vi.fn<(recognitionMatchRecordId: string) => Promise<MatchRecordView>>()

// ========== 视图模型夹具 ==========

/** 医院维度汇总行：同期互认率未计算（null → 「—」）。 */
const usageSummaryRow: UsageSummaryRow = {
  groupDimension: 1,
  receiverOrganizationCode: 'ORG1',
  receiverOrganizationName: '组织一',
  receiverHospitalCode: 'HOS1',
  receiverHospitalName: '医院一',
  receiverBranchCode: null,
  receiverBranchName: null,
  recognitionDeptId: null,
  recognitionDeptName: null,
  itemType: null,
  itemTypeText: '未知类型',
  standardProjectCode: null,
  standardProjectName: null,
  categoryName: null,
  groupName: null,
  reminderCount: 10,
  adoptionCount: 6,
  nonAdoptionCount: 2,
  referenceCount: 4,
  samePeriodRecognitionRate: null,
  estimatedSavingAmount: 320.5,
  nonAdoptionReasons: [{ reasonCode: 'R1', reasonName: '结果互认', count: 2, ratio: 1 }],
}

/** 提醒口径的未反馈明细行：无处理结果与引用事实。 */
const usageDetailUnprocessed: UsageDetailRow = {
  recognitionMatchRecordId: 'RECORD-1',
  recognitionMatchItemId: 'ITEM-1',
  matchCreatedTime: new Date('2026-02-10T08:30:00'),
  businessTime: new Date('2026-02-10T08:30:00'),
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'MZ001',
  source: { organizationCode: 'ORG2', organizationName: '组织二', hospitalCode: 'HOS2', hospitalName: '医院二', branchCode: 'BRA2', branchName: '院区二' },
  receiver: { organizationCode: 'ORG1', organizationName: '组织一', hospitalCode: 'HOS1', hospitalName: '医院一', branchCode: 'BRA1', branchName: '院区一' },
  item: { itemType: 0, itemTypeText: '检验', categoryName: null, groupName: null, standardProjectCode: 'A01', standardProjectName: '血常规' },
  isUnprocessed: true,
  patientName: '张三',
  identityDocumentNo: '110101Y001',
  recognitionDeptId: 'DEPT1',
  recognitionDeptName: '检验科',
  recognitionDoctorId: 'DOC1',
  recognitionDoctorName: '王五',
  processingResult: null,
  reference: null,
}

/** 已反馈明细行：处理结果与引用事实齐全。 */
const usageDetailProcessed: UsageDetailRow = {
  ...usageDetailUnprocessed,
  recognitionMatchRecordId: 'RECORD-2',
  recognitionMatchItemId: 'ITEM-2',
  visitSerialNo: 'MZ002',
  visitType: 2,
  visitTypeText: '急诊',
  isUnprocessed: false,
  patientName: '李四',
  identityDocumentNo: '110101Y002',
  processingResult: {
    isProcessed: true,
    recognitionTime: new Date('2026-02-11T09:00:00'),
    decision: 2,
    decisionText: '不采纳（服务端）',
    nonAdoptionReasonCode: 'R1',
    nonAdoptionReasonName: '结果互认',
    nonAdoptionSupplementDescription: null,
    estimatedSavingAmount: 12.5,
  },
  reference: {
    isReferenced: true,
    referenceTime: new Date('2026-02-12T10:00:00'),
    referenceDeptId: 'D1',
    referenceDeptName: '检验科',
    referenceDoctorId: 'DOC2',
    referenceDoctorName: '赵六',
  },
}

/** 来源侧医院维度汇总行。 */
const sourceSummaryRow: SourceSummaryRow = {
  groupDimension: 1,
  sourceOrganizationCode: 'ORG2',
  sourceOrganizationName: '组织二',
  sourceHospitalCode: 'HOS2',
  sourceHospitalName: '医院二',
  sourceBranchCode: null,
  sourceBranchName: null,
  itemType: null,
  itemTypeText: '未知类型',
  standardProjectCode: null,
  standardProjectName: null,
  categoryName: null,
  groupName: null,
  recognitionCount: 7,
}

/** 来源侧明细行。 */
const sourceDetailRow: SourceDetailRow = {
  recognitionMatchRecordId: 'RECORD-1',
  recognitionMatchItemId: 'ITEM-1',
  source: { organizationCode: 'ORG2', organizationName: '组织二', hospitalCode: 'HOS2', hospitalName: '医院二', branchCode: 'BRA2', branchName: '院区二' },
  receiver: { organizationCode: 'ORG1', organizationName: '组织一', hospitalCode: 'HOS1', hospitalName: '医院一', branchCode: 'BRA1', branchName: '院区一' },
  standardProjectCode: 'A01',
  recognitionDeptId: 'DEPT1',
  recognitionDeptName: '检验科',
  recognitionDoctorId: 'DOC1',
  recognitionDoctorName: '王五',
  recognitionTime: new Date('2026-02-11T09:00:00'),
  patientName: '张三',
  identityDocumentNo: '110101Y001',
}

/** 匹配记录集合视图夹具：组级信息与一个匹配项。 */
const matchRecordView: MatchRecordView = {
  recognitionMatchRecordId: 'RECORD-1',
  matchCreatedTime: new Date('2026-02-10T08:30:00'),
  receiver: { organizationCode: 'ORG1', organizationName: '组织一', hospitalCode: 'HOS1', hospitalName: '医院一', branchCode: 'BRA1', branchName: '院区一' },
  patientName: '张三',
  identityDocumentNo: '110101Y001',
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'MZ001',
  isProcessed: true,
  recognitionTime: new Date('2026-02-11T09:00:00'),
  recognitionDeptId: 'DEPT1',
  recognitionDeptName: '检验科',
  recognitionDoctorId: 'DOC1',
  recognitionDoctorName: '王五',
  matchItems: [
    {
      recognitionMatchItemId: 'ITEM-1',
      item: { itemType: 0, itemTypeText: '检验', categoryName: null, groupName: null, standardProjectCode: 'A01', standardProjectName: '血常规' },
      source: { organizationCode: 'ORG2', organizationName: '组织二', hospitalCode: 'HOS2', hospitalName: '医院二', branchCode: 'BRA2', branchName: '院区二' },
      reportId: 'REPORT-1',
      reportVersionId: 'VERSION-1',
      isProcessed: true,
      decision: 1,
      decisionText: '采纳',
      nonAdoptionReasonCode: null,
      nonAdoptionReasonName: null,
    },
  ],
}

// ========== 装配 ==========

const pageOf = (items: (UsageSummaryRow | SourceSummaryRow)[], pageIndex = 1): StatisticsPage<UsageSummaryRow | SourceSummaryRow> =>
  ({ items, pageIndex, pageSize: 10, totalCount: items.length })
const detailPageOf = (items: (UsageDetailRow | SourceDetailRow)[], pageIndex = 1): StatisticsPage<UsageDetailRow | SourceDetailRow> =>
  ({ items, pageIndex, pageSize: 10, totalCount: items.length })

/** 手动控制完成时机的 Promise，用于断言加载中与失败态这类时序敏感行为。 */
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

function boardProps(overrides: Partial<StatisticsBoardProps> = {}): StatisticsBoardProps {
  return {
    title: '接收医院互认使用统计',
    subtitle: '按组织、医院与院区查询接收侧互认使用汇总与明细',
    side: 'usage',
    scopeKey: 'platform',
    scopeSpec: { kind: 'platform' },
    initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    querySummaryPage: summaryQuery,
    queryDetailsPage: detailsQuery,
    exportWorkbook: exportQuery,
    queryMatchRecord: matchRecordQuery,
    ...overrides,
  }
}

function renderBoard(props = boardProps()) {
  return render(<ConfigProvider locale={zhCN}><StatisticsBoard {...props} /></ConfigProvider>)
}

const summarySection = () => screen.getByRole('region', { name: '汇总统计' })
const detailsSection = () => screen.getByRole('region', { name: '使用明细' })

/** 等待汇总区出现给定文本（汇总行与明细行的名称可能同文，按区块定位）。 */
const waitSummaryText = (text: string) =>
  waitFor(() => expect(within(summarySection()).getByText(text)).toBeInTheDocument())

/** 按标题取 antd Modal 容器（antd 弹窗无 `dialog` 可访问角色，口径同金额维护页用例）。 */
async function findModal(title: string) {
  await screen.findByText(title)
  const found = Array.from(document.querySelectorAll<HTMLElement>('.ant-modal')).find(
    (node) => node.querySelector('.ant-modal-title')?.textContent?.trim() === title,
  )
  if (found === undefined) throw new Error(`弹窗不存在：${title}`)
  return found
}

/** 打开 antd Select 下拉（antd 6 不产出原生 select，口径同报告管理页用例）。 */
function openAntdSelect(select: HTMLElement) {
  fireEvent.mouseDown(select.querySelector('.ant-select-selector') ?? select)
}

/** 从 portal 里按文本取下拉选项。 */
function portalOption(label: string): HTMLElement {
  const option = Array.from(document.querySelectorAll<HTMLElement>('.ant-select-item-option'))
    .find((node) => node.textContent === label)
  if (option === undefined) throw new Error(`下拉选项不存在：${label}`)
  return option
}

beforeEach(() => {
  vi.clearAllMocks()
  summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
  detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([usageDetailUnprocessed, usageDetailProcessed])))
  exportQuery.mockImplementation(() => Promise.resolve())
  matchRecordQuery.mockImplementation(() => Promise.resolve(matchRecordView))
})

describe('统计主体：进入加载与汇总口径（C10、C11）', () => {
  it('进入自动加载：按初始日期与默认维度、默认明细类型发起两路查询，汇总行按维度出列', async () => {
    renderBoard()

    await waitSummaryText('医院一')
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    expect(detailsQuery).toHaveBeenCalledTimes(1)
    // 日期输入反映初始条件。
    expect(screen.getByLabelText('统计开始日期')).toHaveValue('2026-02-01')
    // 六项指标、未计算互认率与金额。
    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    // antd 表格附带隐藏的测量节点，指标数字按可下钻按钮断言，表头按 th 选择器断言。
    expect(within(summaryTable).getByRole('button', { name: '下钻提醒明细' })).toHaveTextContent('10')
    expect(within(summaryTable).getByRole('button', { name: '下钻采纳明细' })).toHaveTextContent('6')
    expect(within(summaryTable).getByRole('button', { name: '下钻不采纳明细' })).toHaveTextContent('2')
    expect(within(summaryTable).getByRole('button', { name: '下钻引用明细' })).toHaveTextContent('4')
    expect(within(summaryTable).getByText('—')).toBeInTheDocument()
    expect(within(summaryTable).getByText('320.50')).toBeInTheDocument()
    expect(screen.getByText(/共 1 条/)).toBeInTheDocument()
  })

  it('同期互认率已计算时按百分比展示', async () => {
    summaryQuery.mockImplementation(() =>
      Promise.resolve(pageOf([{ ...usageSummaryRow, samePeriodRecognitionRate: 0.6 }])))
    renderBoard()

    expect(await screen.findByText('60.00%')).toBeInTheDocument()
  })

  it('维度切换按新维度出列，互认科室维度展示不含未反馈项的口径说明（C11）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([
      {
        ...usageSummaryRow,
        groupDimension: 3,
        receiverHospitalCode: null,
        receiverHospitalName: null,
        receiverBranchCode: 'BRA1',
        receiverBranchName: '院区一',
        recognitionDeptId: 'DEPT1',
        recognitionDeptName: '检验科',
      },
    ])))
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '互认科室' }))

    await waitSummaryText('检验科')
    expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 3, { pageIndex: 1, pageSize: 10 })
    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    expect(within(summaryTable).getByText('互认科室ID', { selector: 'th' })).toBeInTheDocument()
    expect(within(summaryTable).getByText('DEPT1')).toBeInTheDocument()
    expect(screen.getByText(/互认科室维度不含未反馈项/)).toBeInTheDocument()

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([
      {
        ...usageSummaryRow,
        groupDimension: 4,
        receiverHospitalCode: null,
        receiverHospitalName: null,
        itemType: 0,
        itemTypeText: '检验',
        categoryName: '检验分类',
        groupName: '血液',
        standardProjectCode: 'A01',
        standardProjectName: '血常规',
      },
    ])))
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '标准项目' }))

    await waitSummaryText('血常规')
    const itemTable = within(summarySection()).getAllByRole('table')[0]
    expect(within(itemTable).getByText('标准项目编码', { selector: 'th' })).toBeInTheDocument()
    expect(within(itemTable).getByText('A01')).toBeInTheDocument()
  })
})

describe('统计主体：明细表与匹配记录弹窗（C13、C14、C16、C17）', () => {
  it('明细行按服务端随行文本展示，未反馈行显示「未反馈」且处理结果列留空，患者信息原值展示', async () => {
    renderBoard()

    expect(await screen.findByText('张三')).toBeInTheDocument()
    // 患者信息原值（S6-D9）。
    expect(screen.getByText('110101Y001')).toBeInTheDocument()
    expect(screen.getByText('李四')).toBeInTheDocument()
    // 枚举文本取视图模型的服务端随行字段。
    expect(screen.getByText('门诊')).toBeInTheDocument()
    expect(screen.getByText('急诊')).toBeInTheDocument()
    expect(screen.getAllByText('检验').length).toBeGreaterThan(0)
    // 未反馈行与已反馈行。
    const unprocessedRow = screen.getByText('MZ001').closest('tr')
    expect(unprocessedRow).not.toBeNull()
    expect(within(unprocessedRow ?? document.body).getByText('未反馈')).toBeInTheDocument()
    expect(within(unprocessedRow ?? document.body).queryByText(/采纳/)).toBeNull()
    const processedRow = screen.getByText('MZ002').closest('tr')
    expect(processedRow).not.toBeNull()
    expect(within(processedRow ?? document.body).getByText('不采纳（服务端）')).toBeInTheDocument()
    expect(within(processedRow ?? document.body).getByText('结果互认')).toBeInTheDocument()
    expect(within(processedRow ?? document.body).getByText('12.50')).toBeInTheDocument()
    expect(screen.getByText('2026-02-11 09:00')).toBeInTheDocument()
  })

  it('匹配记录弹窗：从明细行进入，展示组级信息、就诊、状态、决定主体与全部匹配项，无报告、PDF 或影像入口', async () => {
    renderBoard()
    await screen.findByText('张三')

    fireEvent.click(screen.getAllByRole('button', { name: '查看互认匹配记录' })[0])

    const dialog = await findModal('互认匹配记录')
    expect(within(dialog).getByText('张三')).toBeInTheDocument()
    expect(within(dialog).getByText('MZ001')).toBeInTheDocument()
    expect(within(dialog).getByText('医院一')).toBeInTheDocument()
    expect(within(dialog).getByText('血常规')).toBeInTheDocument()
    expect(within(dialog).getByText('采纳')).toBeInTheDocument()
    // 弹窗不提供报告内容、PDF 或影像入口。
    expect(within(dialog).queryByText(/PDF|影像|报告内容/)).toBeNull()
    // 弹窗读取只发一次记录查询。
    expect(matchRecordQuery).toHaveBeenCalledWith('RECORD-1')
  })
})

describe('统计主体：下钻联动与导出入口（C12、C15）', () => {
  it('点击汇总指标数字预填明细类型与维度值并查询明细；金额随采纳明细展示', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻提醒明细' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverHospitalCode: 'HOS1' }),
      1,
      { pageIndex: 1, pageSize: 10 },
    ))

    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻采纳明细（预计节省金额）' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    expect(within(detailsSection()).getByRole('radio', { name: '采纳' })).toBeChecked()
  })

  it('点击原因行下钻：明细加原因代码筛选并切到不采纳明细', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' }))

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' }),
      3,
      expect.anything(),
    ))
  })

  it('导出入口沿用当前条件与明细类型构造服务端同名下载名，导出中禁用重复提交', async () => {
    const pending = deferred<void>()
    exportQuery.mockImplementation(() => pending.promise)
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(screen.getByRole('button', { name: '导出汇总' }))
    await waitFor(() => expect(exportQuery).toHaveBeenCalledTimes(1))
    expect(exportQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ startTime: '2026-02-01', endTime: '2026-02-28' }),
      { exportType: 1, groupDimension: 1, detailType: null, downloadFileName: '接收侧互认使用汇总-20260201-20260228.xlsx' },
    )
    expect(screen.getByRole('button', { name: '导出汇总' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '导出明细' })).toBeDisabled()

    await act(async () => { pending.resolve() })
    await waitFor(() => expect(screen.getByRole('button', { name: '导出汇总' })).toBeEnabled())

    fireEvent.click(within(detailsSection()).getByRole('radio', { name: '引用' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 4, expect.anything()))
    fireEvent.click(screen.getByRole('button', { name: '导出明细' }))
    await waitFor(() => expect(exportQuery).toHaveBeenLastCalledWith(
      expect.anything(),
      { exportType: 5, groupDimension: 1, detailType: 4, downloadFileName: '接收侧引用明细-20260201-20260228.xlsx' },
    ))
  })
})

describe('统计主体：范围筛选与本院形态（C10、C20 形态面）', () => {
  it('平台页两组范围与科室、项目条件随查询提交', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    const receiverGroup = within(screen.getByRole('group', { name: '接收组范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS2' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverHospitalCode: 'HOS2' }), 1, expect.anything()))

    const sourceGroup = within(screen.getByRole('group', { name: '来源组范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '组织' }), { target: { value: 'ORG2' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ sourceOrganizationCode: 'ORG2' }), 1, expect.anything()))

    fireEvent.change(screen.getByLabelText('互认科室筛选'), { target: { value: 'DEPT9' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ recognitionDeptId: 'DEPT9' }), 1, expect.anything()))

    const itemTypeSelect = screen.getByLabelText('项目类型筛选')
    openAntdSelect(itemTypeSelect)
    fireEvent.click(portalOption('检查'))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ itemType: 1 }), 1, expect.anything()))

    fireEvent.change(screen.getByLabelText('标准项目编码筛选'), { target: { value: 'A01' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ standardProjectCode: 'A01' }), 1, expect.anything()))
  })

  it('本院形态：本侧组织与医院固定展示，院区与对侧组医院、院区选择随查询提交', async () => {
    renderBoard(boardProps({
      scopeKey: 'ORG-A|HOS-1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A', hospitalCode: 'HOS-1', trustedBranchCode: 'BRA1' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))
    await waitSummaryText('医院一')

    expect(screen.getByText(/可信范围（只读）：组织 ORG-A，医院 HOS-1/)).toBeInTheDocument()
    expect(summaryQuery).toHaveBeenCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA1' }), 1, expect.anything())

    const ownGroup = within(screen.getByRole('group', { name: '本院院区' }))
    expect(ownGroup.getAllByRole('combobox')).toHaveLength(1)
    fireEvent.change(ownGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA2' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA2' }), 1, expect.anything()))

    const oppositeGroup = within(screen.getByRole('group', { name: '来源医院范围' }))
    expect(oppositeGroup.getAllByRole('combobox')).toHaveLength(2)
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS2' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ sourceHospitalCode: 'HOS2' }), 1, expect.anything()))
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA2' } })
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ sourceHospitalCode: 'HOS2', sourceBranchCode: 'BRA2' }), 1, expect.anything()))
  })
})

describe('统计主体：来源侧口径（C22）', () => {
  it('来源侧只计被互认次数：无明细类型切换、无原因区、无金额与互认率，明细无匹配记录入口', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '来源医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))

    await waitSummaryText('医院二')
    expect(within(summarySection()).getByText('被互认次数', { selector: 'th' })).toBeInTheDocument()
    expect(within(summarySection()).queryByText('提醒次数', { selector: 'th' })).toBeNull()
    expect(within(summarySection()).queryByText('同期互认率', { selector: 'th' })).toBeNull()
    expect(within(summarySection()).queryByText('预计节省金额', { selector: 'th' })).toBeNull()
    expect(screen.queryByRole('group', { name: '明细类型' })).toBeNull()
    expect(screen.queryByRole('button', { name: '查看互认匹配记录' })).toBeNull()
    // 汇总数字下钻预填来源医院。
    fireEvent.click(within(summarySection()).getByRole('button', { name: '下钻被互认明细' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ sourceHospitalCode: 'HOS2' }), expect.anything(), expect.anything()))
  })
})

describe('统计主体：判定面替身取证（C9、C13、C18、C19）', () => {
  it('只采纳最后一次请求：交叠请求中迟到的旧响应不写入行数据（C9）', async () => {
    const pending: Array<{ resolve: (page: StatisticsPage<UsageSummaryRow | SourceSummaryRow>) => void }> = []
    summaryQuery.mockImplementation(() => {
      const task = deferred<StatisticsPage<UsageSummaryRow | SourceSummaryRow>>()
      pending.push(task)
      return task.promise
    })
    renderBoard()

    await waitFor(() => expect(pending).toHaveLength(1))
    fireEvent.change(screen.getByLabelText('统计开始日期'), { target: { value: '2026-03-01' } })
    await waitFor(() => expect(pending).toHaveLength(2))

    // 旧响应先完成：数据因序号不符被丢弃，不写入行。
    await act(async () => { pending[0]?.resolve(pageOf([usageSummaryRow])) })
    await act(async () => { pending[1]?.resolve(pageOf([{ ...usageSummaryRow, receiverHospitalName: '医院九' }])) })

    expect(within(summarySection()).getByText('医院九')).toBeInTheDocument()
    expect(within(summarySection()).queryByText('医院一')).toBeNull()
  })

  it('汇总读取失败：已有数据保留、加载态结束、出现待刷新与重试、重试重新发起、无本地错误文案（C18）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    // 同一条件下重读失败：手动刷新触发，旧数据不被失败结果覆盖。
    summaryQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    fireEvent.click(screen.getByRole('button', { name: '重新读取统计数据' }))
    await waitFor(() => expect(within(summarySection()).getByText('汇总数据待刷新')).toBeInTheDocument())

    // 已有数据保留，具体错误文本不进入页面。
    expect(within(summarySection()).getByText('医院一')).toBeInTheDocument()
    expect(screen.queryByText('network down')).toBeNull()

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
    fireEvent.click(within(summarySection()).getByRole('button', { name: /^重\s*试$/ }))

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(3))
    expect(within(summarySection()).getByText('医院一')).toBeInTheDocument()
    expect(within(summarySection()).queryByText('汇总数据待刷新')).toBeNull()
  })

  it('空结果显示空表文案且不报错（C18）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([])))
    renderBoard()

    expect(await screen.findByText('当前筛选条件没有汇总数据')).toBeInTheDocument()
    expect(await screen.findByText('当前筛选条件没有明细数据')).toBeInTheDocument()
  })

  it('当页为空且总数收缩为正：按服务端总数回退末页一次，守卫防止重复自愈（C19）', async () => {
    // 首次加载 100 条（10 页）；翻到第 10 页后服务端总数收缩为 12 并交付空页（越界）。
    const firstPageRows = Array.from({ length: 10 }, (_, index) => ({
      ...usageSummaryRow,
      receiverHospitalCode: `HOS-${index}`,
      receiverHospitalName: `医院${index}`,
    }))
    const lastPageRows = Array.from({ length: 2 }, (_, index) => ({
      ...usageSummaryRow,
      receiverHospitalCode: `HOS-${index + 10}`,
      receiverHospitalName: `医院${index + 10}`,
    }))
    let callCount = 0
    summaryQuery.mockImplementation(() => {
      callCount += 1
      if (callCount === 1) return Promise.resolve({ items: firstPageRows, pageIndex: 1, pageSize: 10, totalCount: 100 })
      if (callCount === 2) return Promise.resolve({ items: [], pageIndex: 10, pageSize: 10, totalCount: 12 })
      return Promise.resolve({ items: lastPageRows, pageIndex: 2, pageSize: 10, totalCount: 12 })
    })
    renderBoard()
    await waitSummaryText('医院0')

    fireEvent.click(screen.getByTitle('10'))

    // 自愈：按服务端新总数换算末页（ceil(12/10)=2），由第 10 页回退到第 2 页并渲染末页两行；
    // 调用次数稳定为 3，同一条件不反复自愈。
    await waitFor(() => expect(within(summarySection()).getByText('医院10')).toBeInTheDocument())
    expect(within(summarySection()).getByText('医院11')).toBeInTheDocument()
    expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 1, { pageIndex: 2, pageSize: 10 })
    expect(summaryQuery).toHaveBeenCalledTimes(3)
  })

  it('互认医生筛选随明细查询请求提交（C13）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.change(screen.getByLabelText('互认医生筛选'), { target: { value: 'DOC-9' } })

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ recognitionDoctorId: 'DOC-9' }), expect.anything(), expect.anything()))
  })

  it('日期被清空后条件不完整：查询出口返回 null，主体零发请求并保留空态（C10 阻断面）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    summaryQuery.mockImplementation(() => null)
    detailsQuery.mockImplementation(() => null)
    fireEvent.change(screen.getByLabelText('统计开始日期'), { target: { value: '' } })
    fireEvent.change(screen.getByLabelText('统计结束日期'), { target: { value: '' } })

    // 复位触发读取入口，出口返回 null 即被阻断：加载态结束、无数据写入、无失败提示。
    await waitFor(() => expect(screen.getByText('当前筛选条件没有汇总数据')).toBeInTheDocument())
    expect(within(summarySection()).queryByText('汇总数据待刷新')).toBeNull()
    expect(within(detailsSection()).queryByText('明细数据待刷新')).toBeNull()
    // 恢复日期后读取恢复。
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([usageDetailUnprocessed, usageDetailProcessed])))
    fireEvent.change(screen.getByLabelText('统计开始日期'), { target: { value: '2026-02-01' } })
    fireEvent.change(screen.getByLabelText('统计结束日期'), { target: { value: '2026-02-28' } })
    await waitSummaryText('医院一')
  })

  it('不采纳原因区展示各原因的次数与占比文本（C11）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    expect(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' })).toHaveTextContent('结果互认')
    expect(within(summarySection()).getByText('次数 2')).toBeInTheDocument()
    expect(within(summarySection()).getByText('占比 100.00%')).toBeInTheDocument()
  })
})
