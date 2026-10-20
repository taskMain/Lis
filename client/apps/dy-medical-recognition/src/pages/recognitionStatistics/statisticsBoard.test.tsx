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
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import dayjs from 'dayjs'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
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

vi.mock('../../contexts/ApiClientContext', () => ({
  // 本院页的范围条经该 Context 取本项目 client 反查组织名：占位 client 不含该端点 → 组织按「未知」。
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => ({}),
}))

vi.mock('@dy/auth-react', () => ({
  /** 与真实包同形的最小替身：token 在值（范围条据此读取三级编码）。 */
  useAuth: () => ({ isAuthenticated: true, token: 'token-1', login: () => {}, logout: () => {} }),
}))

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

/** 提供方医院维度汇总行。 */
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

/** 提供方明细行。 */
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
    title: '使用方互认使用统计',
    subtitle: '按组织、医院与院区查询使用方互认使用汇总与明细',
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
    // 统计时间以预设区间选择器承载（S6-D19）：初始窗口反映初始条件，原生 date 输入无残留。
    expect(screen.getByPlaceholderText('开始日期')).toHaveValue('2026-02-01')
    expect(screen.getByPlaceholderText('结束日期')).toHaveValue('2026-02-28')
    expect(screen.queryByLabelText('统计开始日期')).toBeNull()
    expect(screen.queryByLabelText('统计结束日期')).toBeNull()
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

describe('统计主体：明细表三行式', () => {
  it('使用方明细表提供方、使用方各一列，单元格三行只显名称，院区行为灰色小字，无归属编码列', async () => {
    renderBoard()

    expect(await screen.findByText('张三')).toBeInTheDocument()
    const detailTable = within(detailsSection()).getAllByRole('table')[0]
    const headers = within(detailTable).getAllByRole('columnheader').map((node) => node.textContent)
    expect(headers).toContain('提供方')
    expect(headers).toContain('使用方')
    for (const absent of ['提供方组织', '提供方医院', '提供方院区', '使用方组织', '使用方医院', '使用方院区']) {
      expect(headers).not.toContain(absent)
    }

    // 提供方单元格三行：组织名、医院名、灰色小字院区名（D-04）；两行明细的提供方相同，取第一格断言。
    const sourceCell = within(detailTable).getAllByText('组织二')[0]?.closest('td') ?? null
    expect(sourceCell).not.toBeNull()
    expect(within(sourceCell ?? document.body).getByText('医院二')).toBeInTheDocument()
    const branchText = within(sourceCell ?? document.body).getByText('院区二')
    expect(branchText).toHaveStyle({ fontSize: '12px' })
    // 只显名称不显示编码。
    expect(screen.queryByText('ORG2')).toBeNull()
    expect(screen.queryByText('HOS2')).toBeNull()
  })

  it('提供方明细表同款：提供方、使用方各一列三行式', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))

    await waitSummaryText('医院二')
    const detailTable = within(screen.getByRole('region', { name: '被互认明细' })).getAllByRole('table')[0]
    const headers = within(detailTable).getAllByRole('columnheader').map((node) => node.textContent)
    expect(headers).toContain('提供方')
    expect(headers).toContain('使用方')
    expect(headers).not.toContain('提供方院区')

    const receiverCell = within(detailTable).getByText('医院一').closest('td')
    expect(receiverCell).not.toBeNull()
    expect(within(receiverCell ?? document.body).getByText('组织一')).toBeInTheDocument()
    expect(within(receiverCell ?? document.body).getByText('院区一')).toBeInTheDocument()
  })
})

describe('统计主体：下钻联动与导出入口（C12、C15、下钻经提交路径重读）', () => {
  it('点击汇总指标数字预填筛选草稿并自动提交一次：汇总与明细各重读一次，明细类型切换即时生效', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    expect(detailsQuery).toHaveBeenCalledTimes(1)

    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻提醒明细' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverHospitalCode: 'HOS1' }),
      1,
      { pageIndex: 1, pageSize: 10 },
    ))
    // 下钻走提交路径：一次提交同时重读汇总（提交重读口径）。
    expect(summaryQuery).toHaveBeenCalledTimes(2)
    expect(detailsQuery).toHaveBeenCalledTimes(2)

    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻采纳明细（预计节省金额）' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    expect(within(detailsSection()).getByRole('radio', { name: '采纳' })).toBeChecked()
  })

  it('点击原因行下钻：明细加原因代码筛选并切到不采纳明细，走同一条提交路径', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' }))

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' }),
      3,
      expect.anything(),
    ))
    // 提交路径同时重读汇总（提交重读口径）。
    expect(summaryQuery).toHaveBeenCalledTimes(2)
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
      { exportType: 1, groupDimension: 1, detailType: null, downloadFileName: '使用方互认使用汇总-20260201-20260228.xlsx' },
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
      { exportType: 5, groupDimension: 1, detailType: 4, downloadFileName: '使用方引用明细-20260201-20260228.xlsx' },
    ))
  })
})

describe('统计主体：提交式筛选与查询区（提交式筛选、提交重读、查询区结构）', () => {
  it('范围与筛选改动只进草稿零请求；点查询后汇总与明细各重读一次并携带全部已生效条件', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    const receiverGroup = within(screen.getByRole('region', { name: '使用方范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS2' } })
    const sourceGroup = within(screen.getByRole('region', { name: '提供方范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '组织' }), { target: { value: 'ORG2' } })
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DEPT9' } })
    const itemTypeSelect = screen.getByLabelText('项目类型筛选')
    openAntdSelect(itemTypeSelect)
    fireEvent.click(portalOption('检查'))

    // 草稿改动不发起任何请求（提交式筛选口径）。
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    expect(detailsQuery).toHaveBeenCalledTimes(1)

    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({
        receiverHospitalCode: 'HOS2',
        sourceOrganizationCode: 'ORG2',
        keyword: 'DEPT9',
        itemType: 1,
      }), 1, expect.anything()))
    // 一次提交同时重读汇总与明细（提交重读口径）；统一关键字同时进两路查询。
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({
        receiverHospitalCode: 'HOS2',
        keyword: 'DEPT9',
      }), expect.anything(), expect.anything()))
    expect(summaryQuery).toHaveBeenCalledTimes(2)
    expect(detailsQuery).toHaveBeenCalledTimes(2)
  })

describe('C31 查询区同构与间距（S6-D19）', () => {
  it('关键字占位按侧区分：使用侧含互认医生、提供方不含（提供方语句无医生列）', () => {
    renderBoard()
    expect(screen.getByPlaceholderText('互认科室 / 互认项目 / 互认医生')).toBeInTheDocument()
  })

  it('提供方关键字占位不含互认医生', () => {
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    expect(screen.getByPlaceholderText('互认科室 / 互认项目')).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('互认科室 / 互认项目 / 互认医生')).toBeNull()
  })

  it('重置按钮与原生 date 输入无残留（不设重置，可清空条件经查询生效）', () => {
    renderBoard()
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
    expect(screen.queryByLabelText('统计开始日期')).toBeNull()
    expect(screen.queryByLabelText('统计结束日期')).toBeNull()
  })

  it('查询按钮逐字规格保留：主色、可访问名与 submit 提交，独占行内最后一个 Form.Item', () => {
    renderBoard()

    const form = document.querySelector('.statistics-board-filters form')
    expect(form).not.toBeNull()
    expect(form).toHaveClass('ant-form-inline')
    const search = screen.getByRole('button', { name: '查询' })
    expect(search).toHaveClass('ant-btn-primary')
    expect(search.getAttribute('type')).toBe('submit')
    // 位置断言：查询按钮所在 Form.Item 是 inline Form 的最后一个 Form.Item（独占行尾）。
    const formItems = (form as HTMLElement).querySelectorAll('.ant-form-item')
    expect(formItems.length).toBeGreaterThan(0)
    expect(search.closest('.ant-form-item')).toBe(formItems[formItems.length - 1])
  })

  it('配套 CSS 静态证据：页头左右结构与四段间距同报告管理页（20/16/4/8）', () => {
    // vitest jsdom 环境的 import.meta.url 非 file 协议：按应用根解析文件路径读取静态证据。
    const cssPath = resolve(process.cwd(), 'src/pages/recognitionStatistics/statisticsBoard.css')
    const boardPath = resolve(process.cwd(), 'src/pages/recognitionStatistics/statisticsBoard.tsx')
    const css = readFileSync(cssPath, 'utf8')
    expect(css).toMatch(/\.statistics-board-header\s*\{[^}]*justify-content:\s*space-between[^}]*margin-bottom:\s*20px/s)
    expect(css).toMatch(/\.statistics-board-scope\s*\{[^}]*margin-bottom:\s*16px/s)
    expect(css).toMatch(/\.statistics-board-filters\s*\{[^}]*margin-bottom:\s*4px/s)
    expect(css).toMatch(/\.statistics-board-toolbar\s*\{[^}]*margin-bottom:\s*8px/s)
    // 主体引入配套 CSS，页头标题 margin 归零由 CSS 承载（内联 margin 等价物移除）。
    const board = readFileSync(boardPath, 'utf8')
    expect(board).toContain("import './statisticsBoard.css'")
    expect(board).not.toContain("style={{ margin: 0 }}")
    // 阻断态标题（页面入口）的 margin 归零同由配套 CSS 承载，两个页面入口无内联 margin 等价物。
    for (const pageFile of ['usageStatisticsPages.tsx', 'sourceStatisticsPages.tsx']) {
      const pageSource = readFileSync(resolve(process.cwd(), 'src/pages/recognitionStatistics', pageFile), 'utf8')
      expect(pageSource).not.toContain('style={{ margin: 0 }}')
    }
  })

  it('平台页两方范围卡并排：使用方在左、提供方在右，各带称谓与角色说明（提供方同序）', () => {
    renderBoard()
    expect(screen.getByRole('region', { name: '使用方范围' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: '提供方范围' })).toBeInTheDocument()
    expect(screen.getByText('调阅并使用他院报告')).toBeInTheDocument()
    expect(screen.getByText('出具并提供报告')).toBeInTheDocument()

    cleanup()
    // 提供方平台页：两方沿用同一顺序，使用方在左。
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    expect(screen.getByRole('region', { name: '使用方范围' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: '提供方范围' })).toBeInTheDocument()
  })

  it('本院页页头左右结构：范围条在页头右侧，范围选择行只留对侧组选择器（C20 修订面）', () => {
    renderBoard(boardProps({
      scopeKey: 'ORG-A|HOS-1|BRA1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))

    const header = document.querySelector('.statistics-board-header')
    expect(header).not.toBeNull()
    // 范围条（本用例环境无登录编码，三级按「未知」占位）渲染在页头容器内。
    expect(within(header as HTMLElement).getByText('未知 / 未知 / 未知')).toBeInTheDocument()
    // 范围选择行不再承载范围条，只留对侧组选择器。
    const scopeRow = document.querySelector('.statistics-board-scope')
    expect(scopeRow).not.toBeNull()
    expect(scopeRow?.textContent).not.toContain('未知 / 未知 / 未知')
  })
})

describe('C32 时间预设与默认窗（S6-D19）', () => {
  it('统计时间使用预设区间选择器：预设改动进草稿零请求，点查询按新区间提交', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    const startInput = screen.getByPlaceholderText('开始日期')
    fireEvent.focus(startInput)
    fireEvent.mouseDown(startInput)
    fireEvent.click(startInput)
    const preset = await waitFor(() => {
      const found = Array.from(document.querySelectorAll<HTMLElement>('.ant-picker-presets li'))
        .find((node) => node.textContent === '近7天')
      expect(found).toBeDefined()
      return found as HTMLElement
    })
    fireEvent.click(preset)

    // 预设改动只进草稿，零请求。
    expect(summaryQuery).toHaveBeenCalledTimes(1)

    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({
        startTime: dayjs().subtract(6, 'day').format('YYYY-MM-DD'),
        endTime: dayjs().format('YYYY-MM-DD'),
      }), 1, expect.anything()))
    // 一次提交同时重读明细（提交重读口径）。
    expect(detailsQuery).toHaveBeenCalledTimes(2)
  })

  it('范围切换复位的默认窗按复位时刻重算（与报告管理页跨天语义同构）', async () => {
    // 只冻结 Date（不动定时器）：读取仍走真实 setTimeout，断言按可控「今天」计算。
    vi.useFakeTimers({ toFake: ['Date'] })
    try {
      const startOfToday = dayjs()
      const view = render(<ConfigProvider locale={zhCN}><StatisticsBoard {...boardProps({ scopeKey: 'scope-a' })} /></ConfigProvider>)
      await waitSummaryText('医院一')
      // 初始窗口来自注入的 initialFilters（夹具固定值）。
      expect(screen.getByPlaceholderText('开始日期')).toHaveValue('2026-02-01')

      // 复位时刻前移一天后变更范围键：复位的起止日期是新时刻的近 30 天窗。
      vi.setSystemTime(startOfToday.add(1, 'day').toDate())
      view.rerender(<ConfigProvider locale={zhCN}><StatisticsBoard {...boardProps({ scopeKey: 'scope-b' })} /></ConfigProvider>)
      await waitFor(() => {
        expect(screen.getByPlaceholderText('开始日期')).toHaveValue(startOfToday.add(1, 'day').subtract(29, 'day').format('YYYY-MM-DD'))
        expect(screen.getByPlaceholderText('结束日期')).toHaveValue(startOfToday.add(1, 'day').format('YYYY-MM-DD'))
      })
    } finally {
      vi.useRealTimers()
    }
  })
})

describe('C34 原因下钻筛选标记（S6-D19）', () => {
  it('原因行下钻后明细区工具行显示可移除标记，移除即时提交并回到第 1 页', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' }), 3, expect.anything()))

    // 工具行出现「不采纳原因：{原因名}」可移除标记。
    const marker = within(detailsSection()).getByText('不采纳原因：结果互认')
    expect(marker.closest('.ant-tag')).not.toBeNull()

    // 移除标记走查询提交路径：原因条件清空、两路各自重读、明细回第 1 页。
    const closeIcon = detailsSection().querySelector('.ant-tag-close-icon')
    expect(closeIcon).not.toBeNull()
    fireEvent.click(closeIcon as HTMLElement)
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.not.objectContaining({ nonAdoptionReasonCode: 'R1' }), expect.anything(), { pageIndex: 1, pageSize: 10 }))
    // 初始加载 + 原因下钻提交 + 移除提交，汇总共重读三次。
    expect(summaryQuery).toHaveBeenCalledTimes(3)
    // 移除后标记消失。
    await waitFor(() => expect(within(detailsSection()).queryByText('不采纳原因：结果互认')).toBeNull())
  })

  it('原因标记移除只动原因代码字段：未提交草稿保留、明细按无原因条件重读', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    // 原因下钻：预填草稿并自动提交一次（草稿基底与已生效条件同步）。
    fireEvent.click(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' }), 3, expect.anything()))
    expect(within(detailsSection()).getByText('不采纳原因：结果互认')).toBeInTheDocument()

    // 关键字输入未提交（只进草稿）。
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: '未提交关键字' } })

    // 点击标记关闭：已生效条件移除原因代码并重读，未提交的关键字草稿保持不动。
    fireEvent.click(detailsSection().querySelector('.ant-tag-close-icon') as HTMLElement)
    expect(screen.getByLabelText('关键字筛选')).toHaveValue('未提交关键字')
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.not.objectContaining({ nonAdoptionReasonCode: 'R1' }), expect.anything(), { pageIndex: 1, pageSize: 10 }))
    // 未提交的关键字不进本次读取；标记消失。
    expect(detailsQuery.mock.calls.at(-1)?.[0]?.keyword).toBeUndefined()
    await waitFor(() => expect(within(detailsSection()).queryByText('不采纳原因：结果互认')).toBeNull())
  })
})

describe('C35 下钻写关键字（S6-D19）', () => {
  it('互认科室维度下钻把科室值写入关键字草稿并自动提交一次', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([{
      ...usageSummaryRow,
      groupDimension: 3,
      receiverHospitalCode: null,
      receiverHospitalName: null,
      receiverBranchCode: 'BRA1',
      receiverBranchName: '院区一',
      recognitionDeptId: 'DEPT1',
      recognitionDeptName: '检验科',
    }])))
    renderBoard()
    // 夹具行是互认科室维度行：切到互认科室维度后汇总表出科室名称列。
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '互认科室' }))
    await waitSummaryText('检验科')

    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻提醒明细' }))

    // 走既有提交路径：明细与汇总各重读一次，科室值落在关键字。
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DEPT1' }), 1, expect.anything()))
    // 挂载即切维度会取消初始读取（同 effect 清理）：汇总共两次——维度读取与下钻提交；
    // 下钻提交携带关键字并回第 1 页。
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DEPT1' }), 3, { pageIndex: 1, pageSize: 10 })
  })

  it('标准项目维度下钻把项目编码写入关键字并保留项目类型（使用侧）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([{
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
    }])))
    renderBoard()
    // 夹具行是标准项目维度行：切到标准项目维度后汇总表出项目编码与名称列。
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '标准项目' }))
    await waitSummaryText('血常规')

    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻提醒明细' }))

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'A01', itemType: 0 }), 1, expect.anything()))
  })

  it('提供方标准项目维度下钻把项目编码写入关键字', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([{
      ...sourceSummaryRow,
      groupDimension: 4,
      sourceHospitalCode: null,
      sourceHospitalName: null,
      itemType: 1,
      itemTypeText: '检查',
      standardProjectCode: 'B02',
      standardProjectName: 'CT',
    }])))
    // 提供方明细表列按提供方行取值：明细替身交付提供方行，避免列取值形态错配。
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    // 夹具行是标准项目维度行：切到标准项目维度后汇总表出项目编码与名称列。
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '标准项目' }))
    await waitFor(() => expect(within(screen.getByRole('region', { name: '汇总统计' })).getByText('CT')).toBeInTheDocument())
    const sourceSummaryTable = within(screen.getByRole('region', { name: '汇总统计' })).getAllByRole('table')[0]
    fireEvent.click(within(sourceSummaryTable).getByRole('button', { name: '下钻被互认明细' }))

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'B02', itemType: 1 }), expect.anything(), { pageIndex: 1, pageSize: 10 }))
  })
})

  it('视图切换即时生效，翻页基于已生效条件读取', async () => {
    summaryQuery.mockImplementation(() =>
      Promise.resolve({ items: [usageSummaryRow], pageIndex: 1, pageSize: 10, totalCount: 40 }))
    renderBoard()
    await waitSummaryText('医院一')

    // 草稿改动零请求；提交后按已生效条件读取。
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DEPT9' } })
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DEPT9' }), 1, { pageIndex: 1, pageSize: 10 }))

    // 翻页沿用已生效条件。
    fireEvent.click(screen.getByTitle('2'))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DEPT9' }), 1, { pageIndex: 2, pageSize: 10 }))

    // 汇总维度是视图切换：即时生效、不进草稿，页码回到第 1 页。
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '院区' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.anything(), 2, { pageIndex: 1, pageSize: 10 }))
  })

  it('明细类型切换即时生效且不进草稿', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(detailsSection()).getByRole('radio', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 3, expect.anything()))
    expect(summaryQuery).toHaveBeenCalledTimes(1)
  })
})

describe('统计主体：范围筛选与本院形态（范围随登录上下文取数、提交式筛选）', () => {
  it('本院形态：无本院院区选择，请求院区恒为登录院区；对侧医院、院区选择经草稿提交', async () => {
    renderBoard(boardProps({
      scopeKey: 'ORG-A|HOS-1|BRA1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))
    await waitSummaryText('医院一')

    // 本院页范围区的只读编码副文案由共用范围条取代：本用例环境无登录编码，三级按「未知」占位。
    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(screen.queryByText(/可信范围（只读）/)).not.toBeInTheDocument()
    // 院区恒取登录院区：本院页不再渲染本院院区选择，请求院区参数恒为登录院区。
    expect(screen.queryByRole('group', { name: '本院院区' })).toBeNull()
    expect(summaryQuery).toHaveBeenCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA1' }), 1, expect.anything())

    const callCountAfterInitial = summaryQuery.mock.calls.length
    const oppositeGroup = within(screen.getByRole('region', { name: '提供方范围' }))
    expect(oppositeGroup.getAllByRole('combobox')).toHaveLength(2)
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS2' } })
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA2' } })
    // 草稿改动零请求（提交式筛选口径）。
    expect(summaryQuery.mock.calls.length).toBe(callCountAfterInitial)

    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA1', sourceHospitalCode: 'HOS2', sourceBranchCode: 'BRA2' }),
      1, expect.anything()))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA1', sourceHospitalCode: 'HOS2', sourceBranchCode: 'BRA2' }),
      expect.anything(), expect.anything()))
  })
})

describe('统计主体：提供方口径（C22）', () => {
  it('提供方只计被互认次数：无明细类型切换、无原因区、无金额与互认率，明细无匹配记录入口', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
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
  it('只采纳最后一次请求：读取在途时查询按钮禁用、再次点击不发请求，迟到旧响应不写入行数据（C9）', async () => {
    const pending: Array<{ resolve: (page: StatisticsPage<UsageSummaryRow | SourceSummaryRow>) => void }> = []
    summaryQuery.mockImplementation(() => {
      const task = deferred<StatisticsPage<UsageSummaryRow | SourceSummaryRow>>()
      pending.push(task)
      return task.promise
    })
    renderBoard()

    await waitFor(() => expect(pending).toHaveLength(1))
    // 初始读取在途：查询按钮进入加载态，再点不发新请求（antd loading 在途阻止点击提交）。
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DEPT9' } })
    await waitFor(() => expect(screen.getByRole('button', { name: '查询' })).toHaveClass('ant-btn-loading'))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    expect(pending).toHaveLength(1)

    // 交叠改为经汇总维度切换构造：维度切换即时生效并发起第二次读取。
    fireEvent.click(within(screen.getByRole('group', { name: '汇总维度' })).getByRole('radio', { name: '院区' }))
    await waitFor(() => expect(pending).toHaveLength(2))

    // 判别性顺序（同报告页 C10 第二条）：先让最新响应到达并落地，再让过期旧响应到达——
    // 若无序号守卫，迟到的旧响应会覆盖页面数据；有序号守卫则新结果保留、迟到旧响应被丢弃。
    await act(async () => { pending[1]?.resolve(pageOf([{ ...usageSummaryRow, receiverHospitalName: '医院九' }])) })
    await waitSummaryText('医院九')
    await act(async () => { pending[0]?.resolve(pageOf([usageSummaryRow])) })

    expect(within(summarySection()).getByText('医院九')).toBeInTheDocument()
    expect(within(summarySection()).queryByText('医院一')).toBeNull()
  })

  it('汇总读取失败不渲染错误横幅：已有数据保留、加载态结束，点查询重试成功（C18、读取失败形态：纯读取失败不渲染横幅）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    // 提交触发重读失败：旧数据不被失败结果覆盖（页头刷新按钮已移除，查询是重读唯一入口）。
    summaryQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    // 让失败落地：拒绝在微任务即被读取入口吸收，一轮宏任务后加载态必然结束。
    await act(async () => {})

    // 既定口径：读取失败不渲染错误横幅（错误由宿主统一提示）；已有数据保留，具体错误文本不进入页面。
    expect(within(summarySection()).getByText('医院一')).toBeInTheDocument()
    expect(within(summarySection()).queryByText('汇总数据待刷新')).toBeNull()
    expect(within(detailsSection()).queryByText('明细数据待刷新')).toBeNull()
    expect(screen.queryByText(/待刷新|network down/)).toBeNull()

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
    // 重试经查询按钮。
    fireEvent.click(screen.getByRole('button', { name: '查询' }))

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

  it('关键字筛选改动只进草稿，点查询后随明细查询请求提交（C13、提交式筛选口径）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DOC-9' } })
    // 草稿零请求。
    expect(detailsQuery).toHaveBeenCalledTimes(1)

    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DOC-9' }), expect.anything(), expect.anything()))
  })

  it('查询出口返回 null（条件不完整阻断）时零数据写入并保留已有数据（C10 阻断面）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    // 出口返回 null 表示条件不完整（如本院页登录院区缺失）：主体零写入、不进失败态、保留已有数据。
    summaryQuery.mockImplementation(() => null)
    detailsQuery.mockImplementation(() => null)
    fireEvent.click(screen.getByRole('button', { name: '查询' }))

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    expect(detailsQuery).toHaveBeenCalledTimes(2)
    expect(within(summarySection()).getByText('医院一')).toBeInTheDocument()
    expect(within(summarySection()).queryByText('汇总数据待刷新')).toBeNull()
    expect(within(detailsSection()).queryByText('明细数据待刷新')).toBeNull()
  })

  it('不采纳原因区展示各原因的次数与占比文本（C11）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    expect(within(summarySection()).getByRole('button', { name: '下钻不采纳原因明细' })).toHaveTextContent('结果互认')
    expect(within(summarySection()).getByText('次数 2')).toBeInTheDocument()
    expect(within(summarySection()).getByText('占比 100.00%')).toBeInTheDocument()
  })
})

describe('页面标题层级', () => {
  it('页面大标题为 level 3，无 level 2；标题 margin 归零由配套 CSS 承载（C31 静态证据）', () => {
    renderBoard()

    const heading = screen.getByRole('heading', { name: '使用方互认使用统计', level: 3 })
    expect(heading).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '使用方互认使用统计', level: 2 })).toBeNull()
  })
})

describe('统计主体：平台页下钻补齐组织', () => {
  it('未选组织时点汇总数字下钻，提交条件带上该行组织，不发出组织为空的下钻请求', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    const summaryTable = within(summarySection()).getAllByRole('table')[0]
    fireEvent.click(within(summaryTable).getByRole('button', { name: '下钻提醒明细' }))

    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverOrganizationCode: 'ORG1' }), expect.anything(), expect.anything()))
  })
})
