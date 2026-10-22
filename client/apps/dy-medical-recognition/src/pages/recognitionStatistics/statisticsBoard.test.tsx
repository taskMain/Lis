/**
 * 统计页面共享主体（StatisticsBoard）Component 层用例
 * （阶段 6 前端测试矩阵 C9-C15、C17、C22、C31-C35 的组件面，T01 票据 04 主从骨架）。
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
    return createElement('div', { className, 'data-org-id': value.orgId ?? '' },
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

const scopeNameByCode = vi.hoisted(() => ({
  'ORG-A': '组织甲',
  'ORG-B': '组织乙',
  'HOS-1': '医院甲',
  'HOS-2': '医院乙',
  BRA1: '院区一',
  BRA2: '院区二',
} as Record<string, string>))

vi.mock('../../components/scopeNameLookup', () => ({
  /** 范围名称反查替身：按编码映射可读名称，未登记编码返回 null（展示层据此显示「未知」）。 */
  useScopeNames: (_client: unknown, codes: { organizationCode: string; hospitalCode: string; branchCode: string }) => ({
    organization: scopeNameByCode[codes.organizationCode] ?? null,
    hospital: scopeNameByCode[codes.hospitalCode] ?? null,
    branch: scopeNameByCode[codes.branchCode] ?? null,
  }),
}))

// ========== 数据源替身 ==========

const summaryQuery = vi.fn<(filters: StatisticsFilters, dimension: number, page: StatisticsPageInput) => Promise<StatisticsPage<UsageSummaryRow | SourceSummaryRow>> | null>()
const detailsQuery = vi.fn<(filters: StatisticsFilters, detailType: number, page: StatisticsPageInput) => Promise<StatisticsPage<UsageDetailRow | SourceDetailRow>> | null>()
const exportQuery = vi.fn<(filters: StatisticsFilters, detailFilters: StatisticsFilters, request: { exportType: number; groupDimension: number; detailType: number | null; downloadFileName: string }) => Promise<void> | null>()
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

/** 同一页的第 2 行：用于选中切换与重试。 */
const secondUsageSummaryRow: UsageSummaryRow = {
  ...usageSummaryRow,
  receiverHospitalCode: 'HOS9',
  receiverHospitalName: '医院九',
  reminderCount: 8,
  adoptionCount: 3,
  nonAdoptionCount: 1,
  referenceCount: 2,
}

/** 院区维度汇总行：组织与医院同医院维度行，院区取该行。 */
const branchUsageSummaryRow: UsageSummaryRow = {
  ...usageSummaryRow,
  groupDimension: 2,
  receiverBranchCode: 'BRA1',
  receiverBranchName: '院区一',
}

/** 互认科室维度汇总行：组织、医院、院区与互认科室 ID 都取该行。 */
const deptUsageSummaryRow: UsageSummaryRow = {
  ...branchUsageSummaryRow,
  groupDimension: 3,
  recognitionDeptId: 'DEPT1',
  recognitionDeptName: '检验科',
}

/** 标准项目维度汇总行：项目类型与标准项目编码取该行，医院与院区字段不参与分组。 */
const projectUsageSummaryRow: UsageSummaryRow = {
  ...usageSummaryRow,
  groupDimension: 4,
  receiverHospitalCode: null,
  receiverHospitalName: null,
  itemType: 1,
  itemTypeText: '检查',
  standardProjectCode: 'P100',
  standardProjectName: 'CT',
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

/** 提供方院区维度汇总行。 */
const branchSourceSummaryRow: SourceSummaryRow = {
  ...sourceSummaryRow,
  groupDimension: 2,
  sourceBranchCode: 'BR2',
  sourceBranchName: '院区二',
}

/** 提供方标准项目维度汇总行：标准项目维度下服务端不返回提供方组织与医院。 */
const projectSourceSummaryRow: SourceSummaryRow = {
  ...sourceSummaryRow,
  groupDimension: 4,
  sourceOrganizationCode: null,
  sourceOrganizationName: null,
  sourceHospitalCode: null,
  sourceHospitalName: null,
  itemType: 0,
  itemTypeText: '检验',
  standardProjectCode: 'P200',
  standardProjectName: '血常规',
}

/** 提供方明细行。 */
const sourceDetailRow: SourceDetailRow = {
  recognitionMatchRecordId: 'RECORD-1',
  recognitionMatchItemId: 'ITEM-1',
  source: { organizationCode: 'ORG2', organizationName: '组织二', hospitalCode: 'HOS2', hospitalName: '医院二', branchCode: 'BRA2', branchName: '院区二' },
  receiver: { organizationCode: 'ORG1', organizationName: '组织一', hospitalCode: 'HOS1', hospitalName: '医院一', branchCode: 'BRA1', branchName: '院区一' },
  standardProjectCode: 'A01',
  standardProjectName: '血常规',
  itemType: 0,
  itemTypeText: '检验',
  categoryName: '检验分类',
  groupName: '血液',
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
  ({ items, pageIndex, pageSize: 8, totalCount: items.length })
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

const summaryList = () => screen.getByRole('region', { name: '汇总列表' })
const detailPanel = () => screen.getByRole('region', { name: '明细' })

/** 右卡明细表：antd Table 附带隐藏的测量节点，取含列表头的那张。 */
function detailTable(): HTMLElement {
  const found = within(detailPanel()).getAllByRole('table')
    .find((table) => within(table).queryAllByRole('columnheader').length > 0)
  if (found === undefined) throw new Error('明细表不存在')
  return found
}

/** 等待左汇总列表出现给定文本（右侧明细可能同文，按卡片定位）。 */
const waitSummaryText = (text: string) =>
  waitFor(() => expect(within(summaryList()).getByText(text)).toBeInTheDocument(), { timeout: 3000 })

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
  fireEvent.mouseDown(select.closest('.ant-select') ?? select)
}

/** 从 portal 里按文本取下拉选项。 */
function portalOption(label: string): HTMLElement {
  const option = Array.from(document.querySelectorAll<HTMLElement>('.ant-select-item-option'))
    .find((node) => node.textContent === label)
  if (option === undefined) throw new Error(`下拉选项不存在：${label}`)
  return option
}

/** 切换汇总维度下拉。 */
async function chooseDimension(label: string) {
  openAntdSelect(screen.getByLabelText('汇总维度'))
  fireEvent.click(portalOption(label))
  await waitFor(() => expect(screen.getByLabelText('汇总维度').closest('.ant-select')?.textContent).toContain(label))
}

beforeEach(() => {
  vi.clearAllMocks()
  // once 队列跨用例残留会让后续用例首调命中上一用例的返回值；显式复位四个查询替身。
  summaryQuery.mockReset()
  detailsQuery.mockReset()
  exportQuery.mockReset()
  matchRecordQuery.mockReset()
  summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
  detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([usageDetailUnprocessed, usageDetailProcessed])))
  exportQuery.mockImplementation(() => Promise.resolve())
  matchRecordQuery.mockImplementation(() => Promise.resolve(matchRecordView))
})

describe('票据 04：主从双栏骨架与左汇总列表', () => {
  it('进入页面按每页 8 行读取汇总，左列表按服务端顺序渲染，右卡片保留独立明细区', async () => {
    renderBoard()

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledWith(
      expect.anything(), 1, { pageIndex: 1, pageSize: 8 }))
    const list = summaryList()
    expect(within(list).getByText('医院一')).toBeInTheDocument()
    expect(within(list).getByText('共 1 行')).toBeInTheDocument()
    expect(within(list).queryByRole('table')).toBeNull()
    expect(detailPanel()).toBeInTheDocument()
  })

  it('左列表行按维度显示主名称、次行与指标：使用方为提醒、采纳、不采纳、引用，标准项目行带项目类型标签', async () => {
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
    await waitFor(() => expect(within(summaryList()).getByText('共 1 行')).toBeInTheDocument())
    await chooseDimension('标准项目')

    await waitFor(() => expect(within(summaryList()).getByText('血常规')).toBeInTheDocument())
    const row = within(summaryList()).getByRole('button', { name: /血常规/ })
    expect(within(row).getByText('检验')).toBeInTheDocument()
    expect(within(row).getByText('A01 · 检验分类 / 血液')).toBeInTheDocument()
    expect(within(row).getByText('提醒 10')).toBeInTheDocument()
    expect(within(row).getByText('采纳 6')).toBeInTheDocument()
    expect(within(row).getByText('不采纳 2')).toBeInTheDocument()
    expect(within(row).getByText('引用 4')).toBeInTheDocument()
  })

  it('汇总维度下拉切换后按新维度与每页 8 行重读，行内容随维度更新', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([{
      ...usageSummaryRow,
      groupDimension: 3,
      receiverHospitalCode: null,
      receiverBranchCode: 'BRA1',
      receiverBranchName: '院区一',
      recognitionDeptId: 'DEPT1',
      recognitionDeptName: '检验科',
    }])))
    await chooseDimension('互认科室')

    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.anything(), 3, { pageIndex: 1, pageSize: 8 }))
    expect(within(summaryList()).getByText('检验科')).toBeInTheDocument()
    expect(within(summaryList()).getByText('DEPT1 · 院区一 · 医院一')).toBeInTheDocument()
  })

  it('左侧分页按服务端顺序读取新页，新页自动选中第 1 行', async () => {
    summaryQuery.mockImplementation((_filters, _dimension, page) => Promise.resolve({
      items: page.pageIndex === 1
        ? [{ ...usageSummaryRow, receiverHospitalName: '医院一' }]
        : [{ ...secondUsageSummaryRow, receiverHospitalName: '医院二十' }],
      pageIndex: page.pageIndex,
      pageSize: 8,
      totalCount: 20,
    }))
    renderBoard()
    await waitSummaryText('医院一')
    expect(within(summaryList()).getByRole('button', { name: /医院一/ })).toHaveAttribute('aria-pressed', 'true')

    fireEvent.click(within(summaryList()).getByTitle('2'))

    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.anything(), 1, { pageIndex: 2, pageSize: 8 }))
    await waitFor(() => expect(within(summaryList()).getByText('医院二十')).toBeInTheDocument())
    expect(within(summaryList()).getByRole('button', { name: /医院二十/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('汇总为空时左侧显示空态文案，右侧显示从左侧选择一行的明确空态', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([])))
    renderBoard()

    expect(await within(summaryList()).findByText('当前筛选条件没有汇总数据')).toBeInTheDocument()
    expect(within(detailPanel()).getByText('从左侧选择一行查看明细')).toBeInTheDocument()
    // 无选中行时不读取明细。
    expect(detailsQuery).not.toHaveBeenCalled()
  })
})

describe('票据 04：自动选中与右卡指标摘要', () => {
  it('汇总加载完成后自动选中第 1 行，右侧显示标题、四项指标摘要、互认率与金额，并读取提醒明细', async () => {
    renderBoard()

    await waitSummaryText('医院一')
    const row = within(summaryList()).getByRole('button', { name: /医院一/ })
    expect(row).toHaveAttribute('aria-pressed', 'true')
    expect(detailPanel().querySelector('.statistics-board-detail-title__name')).toHaveTextContent('医院一')
    const summary = within(detailPanel()).getByRole('group', { name: '指标摘要' })
    expect(summary).toHaveTextContent('提醒')
    expect(summary).toHaveTextContent('10')
    expect(summary).toHaveTextContent('采纳')
    expect(summary).toHaveTextContent('6')
    expect(summary).toHaveTextContent('同期互认率')
    expect(summary).toHaveTextContent('—')
    expect(summary).toHaveTextContent('预计节省金额')
    expect(summary).toHaveTextContent('320.50')
    expect(within(detailPanel()).getByText(/统计时间 2026-02-01 至 2026-02-28/)).toBeInTheDocument()
    expect(within(detailPanel()).queryByText(/明细类型 提醒/)).toBeNull()
    expect(within(detailPanel()).getByRole('tab', { name: '提醒' })).toHaveAttribute('aria-selected', 'true')
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.anything(), 1, { pageIndex: 1, pageSize: 10 }))
  })

  it('同期互认率已计算时显示百分比；互认科室维度显示未反馈项口径说明', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(
      pageOf([{ ...usageSummaryRow, samePeriodRecognitionRate: 0.6 }])))
    renderBoard()

    await waitSummaryText('医院一')
    expect(within(detailPanel()).getByText('60.00%')).toBeInTheDocument()

    await chooseDimension('互认科室')
    await waitFor(() => expect(within(summaryList()).getByText(/互认科室维度不含未反馈项/)).toBeInTheDocument())
  })

  it('左列表按服务端返回顺序渲染，不按指标本地重排', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(
      pageOf([secondUsageSummaryRow, usageSummaryRow])))
    renderBoard()

    await waitSummaryText('医院九')
    const rows = within(summaryList()).getAllByRole('button', { name: /医院/ })
    expect(rows[0]).toHaveAccessibleName(/医院九/)
    expect(rows[1]).toHaveAccessibleName(/医院一/)
  })

  it('点击另一行更新标题与明细，明细类型回提醒、右页回 1；点击已选中行重试当前明细', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    detailsQuery.mockImplementation((_filters, _type, page) => Promise.resolve({
      items: [usageDetailUnprocessed, usageDetailProcessed],
      pageIndex: page.pageIndex,
      pageSize: 10,
      totalCount: 20,
    }))
    renderBoard()
    await waitSummaryText('医院一')

    // 初始明细按自动选中首行的精确分组条件读取。
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverOrganizationCode: 'ORG1', receiverHospitalCode: 'HOS1' }),
      1,
      { pageIndex: 1, pageSize: 10 },
    ))

    // 先切到采纳并把明细翻到第 2 页。
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 1, pageSize: 10 }))
    fireEvent.click(within(detailPanel()).getByTitle('2'))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 2, pageSize: 10 }))

    // 点击另一行：标题更新，明细类型回提醒、右页回 1。
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, { pageIndex: 1, pageSize: 10 }))
    expect(within(detailPanel()).getByText('医院九')).toBeInTheDocument()
    expect(within(summaryList()).getByRole('button', { name: /医院九/ })).toHaveAttribute('aria-pressed', 'true')
    expect(within(summaryList()).getByRole('button', { name: /医院一/ })).toHaveAttribute('aria-pressed', 'false')
    expect(within(detailPanel()).getByRole('tab', { name: '提醒' })).toHaveAttribute('aria-selected', 'true')
    // 明细条件随选中行收窄到医院九；用户关键字与其余已生效条件保持不变。
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS9',
    })

    // 已选中行重试当前明细：切到不采纳后点击同一行，明细类型保持不变。
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 3, expect.anything()))
    const detailCallCount = detailsQuery.mock.calls.length
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(detailCallCount + 1))
    expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 3, { pageIndex: 1, pageSize: 10 })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({ receiverHospitalCode: 'HOS9' })
  })

  it('点击明细类型页签只重读对应明细类型，不重读汇总，也不改写已生效筛选', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    expect(summaryQuery).toHaveBeenCalledTimes(1)

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 1, pageSize: 10 }))
    expect(within(detailPanel()).getByRole('tab', { name: '采纳' })).toHaveAttribute('aria-selected', 'true')
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toEqual(expect.objectContaining({
      startTime: '2026-02-01',
      endTime: '2026-02-28',
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS1',
    }))

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '引用' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 4, expect.anything()))
    expect(summaryQuery).toHaveBeenCalledTimes(1)
  })

  it('切换明细类型时保留上一组行并用加载遮罩覆盖，避免表格区域先塌缩', async () => {
    const pending = deferred<StatisticsPage<UsageDetailRow | SourceDetailRow>>()
    renderBoard()
    await screen.findByText('张三')
    detailsQuery.mockImplementation(() => pending.promise)

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    // 新类型读取在途：上一组行仍在，加载遮罩覆盖表格，表格区域不先变空。
    expect(within(detailPanel()).getByText('张三')).toBeInTheDocument()
    await waitFor(() => expect(detailPanel().querySelector('.ant-spin-spinning')).not.toBeNull())

    await act(async () => { pending.resolve(detailPageOf([usageDetailProcessed])) })
    await waitFor(() => expect(within(detailPanel()).getByText('李四')).toBeInTheDocument())
    expect(within(detailPanel()).queryByText('张三')).toBeNull()
  })

  it('提供方右卡为承接方核查版式：卡头即归属链（医院维度两行）并内联被互认次数与导出明细，不渲染指标摘要条与独立工具行（C60）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([
      { ...sourceSummaryRow, sourceBranchCode: 'BRA2', sourceBranchName: '院区二' },
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceOrganizationCode: 'ORG-B',
        sourceHospitalCode: 'HOS-2',
      },
    }))

    await waitSummaryText('医院二')
    await screen.findByText('张三')
    const card = detailPanel().querySelector<HTMLElement>('.statistics-board-detail-card')
    const head = card?.querySelector<HTMLElement>('.ant-card-head') ?? null
    expect(head).not.toBeNull()
    // 卡头即归属链：医院维度只显示组织与医院两行，不适用层级不显示「未知」。
    expect(head?.querySelector('.statistics-board-source-chain__organization')).toHaveTextContent('组织二')
    expect(head?.querySelector('.statistics-board-source-chain__hospital')).toHaveTextContent('医院二')
    expect(head?.querySelector('.statistics-board-source-chain__branch')).toBeNull()
    expect(head).not.toHaveTextContent('未知')
    // 右侧内联被互认次数与导出明细。
    expect(head).toHaveTextContent('被互认 7 次')
    expect(within(head as HTMLElement).getByRole('button', { name: '导出明细' })).toBeInTheDocument()
    // 卡头下只有一行上下文；不渲染指标摘要条与独立工具行。
    expect(within(detailPanel()).queryByRole('group', { name: '指标摘要' })).toBeNull()
    expect(detailPanel().querySelector('.statistics-board-detail-toolbar')).toBeNull()
    const contexts = detailPanel().querySelectorAll('.statistics-board-detail-context')
    expect(contexts).toHaveLength(1)
    expect(contexts[0]).toHaveTextContent('统计时间 2026-02-01 至 2026-02-28 · 承接范围 组织乙 · 医院乙 · 共 1 条')
    // 承接范围只显名称，不出现编码。
    expect(contexts[0]).not.toHaveTextContent('ORG-B')
    expect(contexts[0]).not.toHaveTextContent('HOS-2')
    const live = document.querySelector('.statistics-board-live')
    expect(live?.textContent).toContain('已选择医院二')
    expect(live?.textContent).toContain('被互认 7 条')
    expect(live?.textContent).toContain('明细共 1 条')
    expect(live?.textContent).not.toContain('当前 被互认')
    expect(within(detailPanel()).queryByRole('tab')).toBeNull()
    expect(within(detailPanel()).queryByText('同期互认率')).toBeNull()
    expect(within(detailPanel()).queryByText('预计节省金额')).toBeNull()
    expect(within(detailPanel()).queryByText(/明细类型 被互认/)).toBeNull()
    expect(screen.queryByRole('button', { name: '查看互认匹配记录' })).toBeNull()
  })

  it('卡头归属链按维度取字段：院区维度三行，标准项目维度换为项目名称与编码 · 分类 / 分组两行（C60）', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 2
        ? branchSourceSummaryRow
        : dimension === 4
          ? projectSourceSummaryRow
          : { ...sourceSummaryRow, sourceBranchCode: 'BRA2', sourceBranchName: '院区二' },
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceOrganizationCode: 'ORG-B',
        sourceHospitalCode: 'HOS-2',
        sourceBranchCode: 'BRA2',
      },
    }))
    await waitSummaryText('医院二')

    await chooseDimension('提供方院区')
    await waitSummaryText('院区二')
    let head = detailPanel().querySelector<HTMLElement>('.ant-card-head')
    expect(head?.querySelector('.statistics-board-source-chain__organization')).toHaveTextContent('组织二')
    expect(head?.querySelector('.statistics-board-source-chain__hospital')).toHaveTextContent('医院二')
    expect(head?.querySelector('.statistics-board-source-chain__branch')).toHaveTextContent('院区二')
    // 院区维度的承接范围由已生效筛选派生，再带院区。
    expect(detailPanel().querySelector('.statistics-board-detail-context')).toHaveTextContent('承接范围 组织乙 · 医院乙 · 院区二')

    await chooseDimension('标准项目')
    await waitSummaryText('血常规')
    head = detailPanel().querySelector<HTMLElement>('.ant-card-head')
    // 标准项目维度不显示三级归属链，改为项目名称主行与编码 · 分类 / 分组次行。
    expect(head?.querySelector('.statistics-board-source-chain__organization')).toBeNull()
    expect(head?.querySelector('.statistics-board-source-chain__hospital')).toBeNull()
    expect(head?.querySelector('.statistics-board-source-chain__branch')).toBeNull()
    expect(head?.querySelector('.statistics-board-source-chain__project')).toHaveTextContent('血常规')
    expect(head?.querySelector('.statistics-board-source-chain__secondary')).toHaveTextContent('P200')
  })

  it('标准项目维度名称为空时汇总主名称显示「未匹配标准项目」，项目类型缺失时不渲染类型标签（C60）', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 4
        ? { ...projectSourceSummaryRow, standardProjectName: null, itemType: null, itemTypeText: '未知类型' }
        : sourceSummaryRow,
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')

    await waitSummaryText('未匹配标准项目')
    const row = within(summaryList()).getByRole('button', { name: /未匹配标准项目/ })
    expect(row.querySelector('.statistics-board-group-row__name')).toHaveTextContent('未匹配标准项目')
    // 项目类型文本缺失时不渲染类型标签，也不显示「未知类型」。
    expect(row.querySelector('.ant-tag')).toBeNull()
    expect(within(row as HTMLElement).queryByText('未知类型')).toBeNull()
    // 卡头同样使用「未匹配标准项目」。
    const head = detailPanel().querySelector<HTMLElement>('.ant-card-head')
    expect(head?.querySelector('.statistics-board-source-chain__project')).toHaveTextContent('未匹配标准项目')
  })
});

describe('票据 05：选中行派生的明细条件（C46、C48）', () => {
  it('快速切换选中行时明细只采纳最后一次响应，迟到的旧响应不覆盖明细表（C46）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    const pending: Array<{ resolve: (page: StatisticsPage<UsageDetailRow | SourceDetailRow>) => void }> = []
    detailsQuery.mockImplementation(() => {
      const task = deferred<StatisticsPage<UsageDetailRow | SourceDetailRow>>()
      pending.push(task)
      return task.promise
    })
    renderBoard()
    await waitFor(() => expect(pending).toHaveLength(1))

    // 首行明细仍在途时切到第二行：第二次明细读取按第二行条件发起。
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(pending).toHaveLength(2))
    expect(detailsQuery.mock.calls[1]?.[0]).toMatchObject({ receiverHospitalCode: 'HOS9' })

    // 判别性顺序：先落地最新响应，再让过期旧响应到达；有序号守卫时旧响应被丢弃。
    await act(async () => { pending[1]?.resolve(detailPageOf([usageDetailProcessed])) })
    expect(await screen.findByText('李四')).toBeInTheDocument()
    await act(async () => { pending[0]?.resolve(detailPageOf([usageDetailUnprocessed])) })

    expect(screen.getByText('李四')).toBeInTheDocument()
    expect(screen.queryByText('张三')).toBeNull()
  })

  it('使用方医院维度：提交该行组织与医院，院区沿用已生效筛选，关键字保留且不带科室与项目字段', async () => {
    renderBoard(boardProps({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        receiverBranchCode: 'BRA-APPLIED',
        keyword: 'DOC-9',
      },
    }))
    await waitSummaryText('医院一')
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, expect.anything()))

    // 平台页未选组织时按选中行组织补齐；医院取该行，院区不被选中行覆盖。
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS1',
      receiverBranchCode: 'BRA-APPLIED',
      keyword: 'DOC-9',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('recognitionDeptId')
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('standardProjectCode')
  })

  it('使用方院区维度：提交该行组织、医院与院区，不带互认科室与项目字段', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 2 ? branchUsageSummaryRow : usageSummaryRow]),
    ))
    renderBoard(boardProps({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        receiverOrganizationCode: 'ORG-STALE',
        receiverBranchCode: 'BRA-APPLIED',
        keyword: 'DOC-9',
      },
    }))
    await waitSummaryText('医院一')
    await chooseDimension('院区')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverBranchCode: 'BRA1' }), 1, expect.anything()))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS1',
      receiverBranchCode: 'BRA1',
      keyword: 'DOC-9',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('recognitionDeptId')
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('standardProjectCode')
  })

  it('使用方互认科室维度：额外提交该行 RecognitionDeptId，不带标准项目字段', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 3 ? deptUsageSummaryRow : usageSummaryRow]),
    ))
    renderBoard(boardProps({
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', keyword: 'DOC-9' },
    }))
    await waitSummaryText('医院一')
    await chooseDimension('互认科室')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ recognitionDeptId: 'DEPT1' }), 1, expect.anything()))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS1',
      receiverBranchCode: 'BRA1',
      recognitionDeptId: 'DEPT1',
      keyword: 'DOC-9',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('standardProjectCode')
  })

  it('使用方标准项目维度：提交该行项目类型与标准项目编码，范围沿用已生效筛选', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 4 ? projectUsageSummaryRow : usageSummaryRow]),
    ))
    renderBoard(boardProps({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        receiverBranchCode: 'BRA-APPLIED',
        keyword: 'DOC-9',
      },
    }))
    await waitSummaryText('医院一')
    await chooseDimension('标准项目')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ standardProjectCode: 'P100' }), 1, expect.anything()))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      itemType: 1,
      standardProjectCode: 'P100',
      receiverBranchCode: 'BRA-APPLIED',
      keyword: 'DOC-9',
    })
    const filters = detailsQuery.mock.calls.at(-1)?.[0]
    expect(filters).not.toHaveProperty('receiverHospitalCode')
    expect(filters).not.toHaveProperty('recognitionDeptId')
  })

  it('提供方医院维度：提交该行组织与医院，院区沿用已生效筛选，不带标准项目编码', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceBranchCode: 'BR-APPLIED',
        keyword: 'P200',
      },
    }))
    await waitSummaryText('医院二')
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, expect.anything()))

    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      sourceOrganizationCode: 'ORG2',
      sourceHospitalCode: 'HOS2',
      sourceBranchCode: 'BR-APPLIED',
      keyword: 'P200',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('standardProjectCode')
  })

  it('提供方院区维度：提交该行组织、医院与院区', async () => {
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 2 ? branchSourceSummaryRow : sourceSummaryRow]),
    ))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', keyword: 'P200' },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('提供方院区')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ sourceBranchCode: 'BR2' }), 1, expect.anything()))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      sourceOrganizationCode: 'ORG2',
      sourceHospitalCode: 'HOS2',
      sourceBranchCode: 'BR2',
      keyword: 'P200',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('standardProjectCode')
  })

  it('提供方标准项目维度：提交该行项目类型与标准项目编码，范围沿用已生效筛选', async () => {
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 4 ? projectSourceSummaryRow : sourceSummaryRow]),
    ))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceBranchCode: 'BR-APPLIED',
        keyword: 'P200',
      },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ standardProjectCode: 'P200' }), 1, expect.anything()))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).toMatchObject({
      itemType: 0,
      standardProjectCode: 'P200',
      sourceBranchCode: 'BR-APPLIED',
      keyword: 'P200',
    })
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('sourceHospitalCode')
  })
})

describe('票据 05：查询、维度与页签复位（C53）', () => {
  it('筛选条件变化后选中清空并自动选新页第 1 行，明细类型回提醒、右页回 1', async () => {
    detailsQuery.mockImplementation((_filters, _type, page) => Promise.resolve({
      items: [usageDetailUnprocessed],
      pageIndex: page.pageIndex,
      pageSize: 10,
      totalCount: 20,
    }))
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    fireEvent.click(within(detailPanel()).getByTitle('2'))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 2, pageSize: 10 }))

    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DOC-NEW' } })
    fireEvent.click(screen.getByRole('button', { name: '查询' }))

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ keyword: 'DOC-NEW' }), 1, { pageIndex: 1, pageSize: 10 }))
    expect(within(summaryList()).getByRole('button', { name: /医院一/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('同条件再次提交保留左页、选中、明细类型与右页，两区各重读一次', async () => {
    detailsQuery.mockImplementation((_filters, _type, page) => Promise.resolve({
      items: [usageDetailUnprocessed],
      pageIndex: page.pageIndex,
      pageSize: 10,
      totalCount: 20,
    }))
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, expect.anything()))
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    fireEvent.click(within(detailPanel()).getByTitle('2'))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 2, pageSize: 10 }))

    const summaryCalls = summaryQuery.mock.calls.length
    const detailCalls = detailsQuery.mock.calls.length
    await waitFor(() => expect(screen.getByRole('button', { name: '查询' })).not.toHaveClass('ant-btn-loading'))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(summaryCalls + 1), { timeout: 3000 })
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(detailCalls + 1), { timeout: 3000 })
    expect(detailsQuery).toHaveBeenCalledTimes(detailCalls + 1)
    expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverHospitalCode: 'HOS9' }), 2, { pageIndex: 2, pageSize: 10 })
    expect(within(summaryList()).getByRole('button', { name: /医院九/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('切换汇总维度清空选中并自动选新维度第 1 行，明细类型回提醒、右页回 1', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 2 ? branchUsageSummaryRow : usageSummaryRow]),
    ))
    renderBoard()
    await waitSummaryText('医院一')
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))

    await chooseDimension('院区')

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ receiverBranchCode: 'BRA1' }), 1, { pageIndex: 1, pageSize: 10 }))
    expect(within(summaryList()).getByRole('button', { name: /院区一/ })).toHaveAttribute('aria-pressed', 'true')
  })
})

describe('票据 06：不采纳原因筛选（C49）', () => {
  it('不采纳页签的原因下拉显示全部原因与各原因次数、占比；选择后只重读不采纳明细、右页回 1、上下文追加原因；清除恢复全部', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 3, { pageIndex: 1, pageSize: 10 }))
    const summaryCalls = summaryQuery.mock.calls.length

    const reasonSelect = within(detailPanel()).getByLabelText('不采纳原因')
    openAntdSelect(reasonSelect)
    expect(portalOption('全部原因（2 次）')).toBeInTheDocument()
    expect(portalOption('结果互认（2 次，100.00%）')).toBeInTheDocument()
    fireEvent.click(portalOption('结果互认（2 次，100.00%）'))

    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' }), 3, { pageIndex: 1, pageSize: 10 }))
    // 只重读明细：左列表与汇总条件不变。
    expect(summaryQuery).toHaveBeenCalledTimes(summaryCalls)
    expect(within(detailPanel()).getByText(/原因：结果互认/)).toBeInTheDocument()

    // 清除原因：选回全部原因后只重读不提原因代码的不采纳明细。
    openAntdSelect(reasonSelect)
    fireEvent.click(portalOption('全部原因（2 次）'))
    await waitFor(() => {
      const filters = detailsQuery.mock.calls.at(-1)?.[0]
      expect(filters).not.toHaveProperty('nonAdoptionReasonCode')
    })
    expect(detailsQuery.mock.calls.at(-1)?.[1]).toBe(3)
    expect(detailsQuery.mock.calls.at(-1)?.[2]).toEqual({ pageIndex: 1, pageSize: 10 })
    expect(within(detailPanel()).queryByText(/原因：/)).toBeNull()
  })

  it('切换页签与切换选中行都清空原因；回到不采纳页签按全部原因读取', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 3, expect.anything()))
    openAntdSelect(within(detailPanel()).getByLabelText('不采纳原因'))
    fireEvent.click(portalOption('结果互认（2 次，100.00%）'))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[0]).toEqual(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' })))

    // 切到采纳：原因清空，不采纳原因工具行消失。
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    expect(within(detailPanel()).queryByLabelText('不采纳原因')).toBeNull()

    // 回到不采纳：下拉回到全部原因，请求不带原因代码。
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[1]).toBe(3))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('nonAdoptionReasonCode')
    openAntdSelect(within(detailPanel()).getByLabelText('不采纳原因'))
    expect(portalOption('全部原因（2 次）')).toBeInTheDocument()
    fireEvent.click(portalOption('结果互认（2 次，100.00%）'))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[0]).toEqual(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' })))

    // 切换选中行：原因随选中行清空，新行回到不采纳页签时不带原因代码。
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, expect.anything()))
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[1]).toBe(3))
    expect(detailsQuery.mock.calls.at(-1)?.[0]).not.toHaveProperty('nonAdoptionReasonCode')
  })
})

describe('票据 06：明细表、行展开与匹配记录（C50、C51）', () => {
  it('使用方明细表为 9 列：时间主次行、患者姓名与证件号码、项目类型独立列、项目名称/编码、反馈标签与原因、匹配记录入口', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await screen.findByText('张三')

    const table = detailTable()
    const headers = within(table).getAllByRole('columnheader')
      .map((node) => node.textContent)
      .filter((text) => text !== null && text.length > 0)
    expect(headers).toEqual(['时间', '患者', '项目类型', '项目', '提供方', '使用方院区', '科室 / 医生', '反馈结果', '操作'])
    expect(table.getAttribute('style')).toContain('table-layout: fixed')

    const unprocessedRow = screen.getByText('张三').closest('tr')
    expect(unprocessedRow).not.toBeNull()
    // 患者姓名与证件号码原值同格展示。
    expect(within(unprocessedRow ?? document.body).getByText('110101Y001')).toBeInTheDocument()
    // 时间主行与匹配时间次行。
    expect(within(unprocessedRow ?? document.body).getByText('匹配 2026-02-10 08:30')).toBeInTheDocument()
    // 项目类型标签、标准项目名称与编码。
    expect(within(unprocessedRow ?? document.body).getByText('检验')).toBeInTheDocument()
    expect(within(unprocessedRow ?? document.body).getByText('血常规')).toBeInTheDocument()
    expect(within(unprocessedRow ?? document.body).getByText('A01')).toBeInTheDocument()
    // 未反馈行：标签为未反馈，无原因。
    expect(within(unprocessedRow ?? document.body).getByText('未反馈')).toBeInTheDocument()
    expect(within(unprocessedRow ?? document.body).queryByText('不采纳')).toBeNull()
    expect(within(unprocessedRow ?? document.body).getByRole('button', { name: '查看互认匹配记录' })).toBeInTheDocument()

    const processedRow = screen.getByText('李四').closest('tr')
    expect(processedRow).not.toBeNull()
    expect(within(processedRow ?? document.body).getByText('不采纳（服务端）')).toBeInTheDocument()
    expect(within(processedRow ?? document.body).getByText('结果互认')).toBeInTheDocument()
    expect(within(processedRow ?? document.body).getByText('110101Y002')).toBeInTheDocument()
    expect(within(processedRow ?? document.body).getByText('王五')).toBeInTheDocument()

    // 长文本省略并以 title 提供全文：归属名称与项目编码都带 title。
    expect(within(table).getAllByTitle('组织二').length).toBeGreaterThan(0)
    expect(within(table).getAllByTitle('A01').length).toBeGreaterThan(0)
  })

  it('平台使用方页已锁接收院区时移除使用方院区列，保留提供方列', async () => {
    renderBoard(boardProps({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        receiverBranchCode: 'BRA1',
      },
    }))
    await waitSummaryText('医院一')
    const headers = within(detailTable()).getAllByRole('columnheader')
      .map((node) => node.textContent)
      .filter((text) => text !== null && text.length > 0)
    expect(headers).not.toContain('使用方院区')
    expect(headers).toContain('提供方')
  })

  it('使用方页院区维度移除固定侧列，保留提供方列', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await chooseDimension('院区')
    await waitFor(() => {
      const headers = within(detailTable()).getAllByRole('columnheader')
        .map((node) => node.textContent)
        .filter((text) => text !== null && text.length > 0)
      expect(headers).not.toContain('使用方院区')
      expect(headers).toContain('提供方')
    })
  })

  it('标准项目名称为空时项目列显示「未匹配标准项目」，项目类型仍在独立列', async () => {
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([{
      ...usageDetailUnprocessed,
      item: { ...usageDetailUnprocessed.item, standardProjectName: '' },
    }])))
    renderBoard()
    await screen.findByText('未匹配标准项目')
    const row = screen.getByText('未匹配标准项目').closest('tr')
    expect(row).not.toBeNull()
    expect(within(row ?? document.body).getByText('检验')).toBeInTheDocument()
    expect(within(row ?? document.body).getByText('A01')).toBeInTheDocument()
  })

  it('使用方行展开展示就诊、证件、互认时间、金额、引用信息、不采纳原因与补充说明、匹配记录标识', async () => {
    renderBoard()
    await screen.findByText('李四')

    const table = detailTable()
    const expandIcons = table.querySelectorAll('.ant-table-row-expand-icon')
    expect(expandIcons).toHaveLength(2)
    fireEvent.click(expandIcons[1])

    const reasonLabel = await screen.findByText('不采纳原因与补充说明')
    const expanded = reasonLabel.closest<HTMLElement>('.ant-table-expanded-row')
    expect(expanded).not.toBeNull()
    const panel = within(expanded ?? document.body)
    expect(panel.getByText('就诊类型与流水号')).toBeInTheDocument()
    expect(panel.getByText('急诊 · MZ002')).toBeInTheDocument()
    expect(panel.getByText('110101Y002')).toBeInTheDocument()
    expect(panel.getByText('互认时间')).toBeInTheDocument()
    expect(panel.getByText('2026-02-11 09:00')).toBeInTheDocument()
    expect(panel.getByText('预计节省金额')).toBeInTheDocument()
    expect(panel.getByText('12.50')).toBeInTheDocument()
    expect(panel.getByText('引用时间')).toBeInTheDocument()
    expect(panel.getByText('2026-02-12 10:00')).toBeInTheDocument()
    expect(panel.getByText('引用科室')).toBeInTheDocument()
    expect(panel.getByText('检验科')).toBeInTheDocument()
    expect(panel.getByText('引用医生')).toBeInTheDocument()
    expect(panel.getByText('赵六')).toBeInTheDocument()
    expect(panel.getByText('结果互认')).toBeInTheDocument()
    expect(panel.getByText('匹配记录标识')).toBeInTheDocument()
    expect(panel.getByText('RECORD-2')).toBeInTheDocument()
  })

  it('匹配记录弹窗：从明细行进入，展示组级信息、就诊、状态、决定主体与全部匹配项，无报告、PDF 或影像入口', async () => {
    renderBoard()
    await screen.findByText('张三')

    fireEvent.click(screen.getAllByRole('button', { name: '查看互认匹配记录' })[0])

    const dialog = await findModal('互认匹配记录')
    expect(within(dialog).getByText('匹配记录标识')).toBeInTheDocument()
    expect(within(dialog).getByText('RECORD-1')).toBeInTheDocument()
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

  it('匹配记录读取失败保留弹窗并可重试，失败提示不替换为页面横幅', async () => {
    const pending = deferred<MatchRecordView>()
    matchRecordQuery
      .mockImplementationOnce(() => pending.promise)
      .mockImplementationOnce(() => Promise.resolve(matchRecordView))
    renderBoard()
    await screen.findByText('张三')

    fireEvent.click(screen.getAllByRole('button', { name: '查看互认匹配记录' })[0])
    const dialog = await findModal('互认匹配记录')
    expect(within(dialog).getByText('正在读取匹配记录')).toBeInTheDocument()

    await act(async () => { pending.reject(new Error('network down')) })
    expect(await within(dialog).findByText('匹配记录读取失败，请重试。')).toBeInTheDocument()
    expect(dialog.querySelector('.ant-alert')).toBeNull()

    fireEvent.click(within(dialog).getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(matchRecordQuery).toHaveBeenCalledTimes(2))
    expect(await within(dialog).findByText('MZ001')).toBeInTheDocument()
    expect(within(dialog).queryByText('匹配记录读取失败，请重试。')).toBeNull()
  })

  it('使用方明细表提供方、使用方院区各一列，单元格三行只显名称，院区行为灰色小字，无归属编码列', async () => {
    renderBoard()
    await screen.findByText('张三')

    const table = detailTable()
    const headers = within(table).getAllByRole('columnheader').map((node) => node.textContent)
    expect(headers).toContain('提供方')
    expect(headers).toContain('使用方院区')
    expect(headers).not.toContain('使用方')
    for (const absent of ['提供方组织', '提供方医院', '提供方院区', '使用方组织', '使用方医院']) {
      expect(headers).not.toContain(absent)
    }

    // 提供方单元格三行：组织名、医院名、灰色小字院区名（D-04）；两行明细的提供方相同，取第一格断言。
    const sourceCell = within(table).getAllByText('组织二')[0]?.closest('td') ?? null
    expect(sourceCell).not.toBeNull()
    expect(within(sourceCell ?? document.body).getByText('医院二')).toBeInTheDocument()
    const branchText = within(sourceCell ?? document.body).getByText('院区二')
    expect(branchText).toHaveStyle({ fontSize: '12px' })
    // 只显名称不显示编码。
    expect(screen.queryByText('ORG2')).toBeNull()
    expect(screen.queryByText('HOS2')).toBeNull()
  })

  it('提供方明细表按承接方核查口径排列：使用方首列、标准项目名称与编码、项目类型、分类、分组、互认时间、患者、科室/医生，提供方院区为末位条件列；无反馈结果、无操作、无行展开（C61）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))

    await waitSummaryText('医院二')
    await screen.findByText('张三')
    const table = detailTable()
    const headers = within(table).getAllByRole('columnheader')
      .map((node) => node.textContent)
      .filter((text) => text !== null && text.length > 0)
    expect(headers).toEqual(['使用方', '标准项目', '项目类型', '分类', '分组', '互认时间', '患者', '科室 / 医生', '提供方院区'])
    expect(table.getAttribute('style')).toContain('table-layout: fixed')
    // 标准项目名称主行、编码次行；项目类型、分类与分组各自成列，四字段取服务端返回值。
    expect(within(table).getByText('血常规')).toBeInTheDocument()
    expect(within(table).getByText('A01')).toBeInTheDocument()
    expect(within(table).getByText('检验')).toBeInTheDocument()
    expect(within(table).getByText('检验分类')).toBeInTheDocument()
    expect(within(table).getByText('血液')).toBeInTheDocument()
    // 无反馈结果、无操作、无行展开。
    expect(within(table).queryByText('未反馈')).toBeNull()
    expect(within(table).queryByRole('button', { name: '查看互认匹配记录' })).toBeNull()
    expect(table.querySelectorAll('.ant-table-row-expand-icon')).toHaveLength(0)

    const receiverCell = within(table).getByText('医院一').closest('td')
    expect(receiverCell).not.toBeNull()
    expect(within(receiverCell ?? document.body).getByText('组织一')).toBeInTheDocument()
    expect(within(receiverCell ?? document.body).getByText('院区一')).toBeInTheDocument()

    // 一次提交同时重读明细（提交重读口径）：初始一次 + 提交一次。
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(2))
  })

  it('提供方明细四字段缺失时：标准项目名称显示「未匹配标准项目」、编码显示「—」、分类与分组整段省略（C61）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([
      { ...sourceDetailRow, standardProjectName: null, standardProjectCode: '', categoryName: null, groupName: null },
    ])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))

    await waitSummaryText('医院二')
    await screen.findByText('未匹配标准项目')
    const table = detailTable()
    const dataRow = within(table).getAllByRole('row')[1]
    const cells = within(dataRow).getAllByRole('cell')
    expect(within(cells[1]).getByText('未匹配标准项目')).toBeInTheDocument()
    expect(within(cells[1]).getByText('—')).toBeInTheDocument()
    // 分类与分组缺失时整段省略：单元格为空，不以破折号或其他占位文字冒充分类值。
    expect(cells[3].textContent).toBe('')
    expect(cells[4].textContent).toBe('')
  })

  it('平台提供方页已锁来源院区时移除提供方院区列，保留使用方列', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceBranchCode: 'BRA2',
      },
    }))
    await waitSummaryText('医院二')
    const headers = within(detailTable()).getAllByRole('columnheader')
      .map((node) => node.textContent)
      .filter((text) => text !== null && text.length > 0)
    expect(headers).not.toContain('提供方院区')
    expect(headers).toContain('使用方')
  })

  it('提供方页院区维度移除固定侧列，保留使用方列', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('提供方院区')
    await waitFor(() => {
      const headers = within(detailTable()).getAllByRole('columnheader')
        .map((node) => node.textContent)
        .filter((text) => text !== null && text.length > 0)
      expect(headers).not.toContain('提供方院区')
      expect(headers).toContain('使用方')
    })
  })

  it('右明细每页 10 条：翻页只重读明细，不重读汇总', async () => {
    detailsQuery.mockImplementation((_filters, _type, page) => Promise.resolve({
      items: [usageDetailUnprocessed],
      pageIndex: page.pageIndex,
      pageSize: 10,
      totalCount: 20,
    }))
    renderBoard()
    await waitSummaryText('医院一')
    expect(summaryQuery).toHaveBeenCalledTimes(1)
    await waitFor(() => expect(within(detailPanel()).getByTitle('2')).toBeInTheDocument())

    fireEvent.click(within(detailPanel()).getByTitle('2'))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, { pageIndex: 2, pageSize: 10 }))
    expect(summaryQuery).toHaveBeenCalledTimes(1)
  })
})

describe('票据 04：查询区、范围区与提交式筛选', () => {
  it('范围与筛选改动只进草稿零请求；点查询后汇总与明细各重读一次并携带全部已生效条件', async () => {
    // 汇总按提交的医院条件返回对应行：明细精确条件随选中行取该医院。
    summaryQuery.mockImplementation((filters) => Promise.resolve(pageOf([{
      ...usageSummaryRow,
      receiverHospitalCode: filters.receiverHospitalCode ?? 'HOS1',
      receiverHospitalName: filters.receiverHospitalCode === 'HOS2' ? '医院二' : '医院一',
    }])))
    renderBoard()
    await waitSummaryText('医院一')

    const receiverGroup = within(screen.getByRole('region', { name: '使用方范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS2' } })
    const sourceGroup = within(screen.getByRole('region', { name: '提供方范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '组织' }), { target: { value: 'ORG2' } })
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: 'DEPT9' } })
    openAntdSelect(screen.getByLabelText('项目类型筛选'))
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

  it('同条件再次提交保留选中行与明细类型，两区各重读一次', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 2, expect.anything()))
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 1, expect.anything()))

    const summaryCalls = summaryQuery.mock.calls.length
    const detailCalls = detailsQuery.mock.calls.length
    // 草稿写入后清空一个从未生效的控件：next 携带 keyword: undefined，不应被当作条件变化而清空选中/页码。
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: '临时条件' } })
    fireEvent.change(screen.getByLabelText('关键字筛选'), { target: { value: '' } })
    const refreshedDetail: UsageDetailRow = {
      ...usageDetailProcessed,
      recognitionMatchItemId: 'ITEM-REFRESH',
      patientName: '刷新患者',
      identityDocumentNo: '110101Y999',
    }
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([refreshedDetail])))
    await waitFor(() => expect(screen.getByRole('button', { name: '查询' })).not.toHaveClass('ant-btn-loading'))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))

    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(summaryCalls + 1), { timeout: 3000 })
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(detailCalls + 1), { timeout: 3000 })
    // 同条件刷新不能把随后启动的明细重读判为过期：可见行必须换成新响应。
    await waitFor(() => expect(within(detailPanel()).getByText('刷新患者')).toBeInTheDocument())
    expect(within(detailPanel()).queryByText('李四')).toBeNull()
    expect(within(summaryList()).getByRole('button', { name: /医院九/ })).toHaveAttribute('aria-pressed', 'true')
  })

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
    // 顺序断言：关键字、项目类型、统计时间、查询按钮。
    const itemOrder = Array.from(formItems).map((item) => {
      if (item.querySelector('[aria-label="关键字筛选"]') !== null) return 'keyword'
      if (item.querySelector('[aria-label="项目类型筛选"]') !== null) return 'itemType'
      if (item.querySelector('[aria-label="统计时间范围"]') !== null) return 'time'
      if (item.querySelector('[aria-label="查询"]') !== null) return 'search'
      return 'unknown'
    })
    expect(itemOrder).toEqual(['keyword', 'itemType', 'time', 'search'])
  })

  it('配套 CSS 静态证据：既有区间距保留，主从网格、选中态、指标摘要与窄屏堆叠齐备', () => {
    // vitest jsdom 环境的 import.meta.url 非 file 协议：按应用根解析文件路径读取静态证据。
    const cssPath = resolve(process.cwd(), 'src/pages/recognitionStatistics/statisticsBoard.css')
    const boardPath = resolve(process.cwd(), 'src/pages/recognitionStatistics/statisticsBoard.tsx')
    const css = readFileSync(cssPath, 'utf8')
    expect(css).toMatch(/\.statistics-board-header\s*\{[^}]*justify-content:\s*space-between[^}]*margin-bottom:\s*20px/s)
    expect(css).toMatch(/\.statistics-board-scope\s*\{[^}]*margin-bottom:\s*16px/s)
    // 平台页双列卡片与本院页单行对侧筛选各有配套布局。
    expect(css).toMatch(/\.statistics-board-duo\s*\{[^}]*grid-template-columns/s)
    expect(css).toMatch(/\.statistics-board-opposite-filter\s*\{[^}]*display:\s*flex/s)
    expect(css).toMatch(/\.statistics-board-filters\s*\{[^}]*margin-bottom:\s*4px/s)
    // 主从网格：默认两列 400px + 剩余宽度；容器 1200–1279px 左列 360px；容器 <1200px 单列堆叠。
    expect(css).toMatch(/\.statistics-board\s*\{[^}]*container-type:\s*inline-size/s)
    expect(css).toMatch(/\.statistics-board-master\s*\{[^}]*grid-template-columns:\s*400px\s+minmax\(0,\s*1fr\)/s)
    expect(css).toMatch(/@container \(max-width:\s*1279px\)[\s\S]*?\.statistics-board-master\s*\{[^}]*grid-template-columns:\s*360px/s)
    expect(css).toMatch(/@container \(max-width:\s*1199px\)[\s\S]*?\.statistics-board-master\s*\{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\)/s)
    // 左列表内部滚动与选中态：底色 + 左侧竖条 + aria-pressed；高度同时受视口约束，分页不被推出首屏。
    expect(css).toMatch(/\.statistics-board-list\s*\{[^}]*max-height:\s*min\(560px,\s*calc\(100vh - 540px\)\)[^}]*overflow-y:\s*auto/s)
    expect(css).toMatch(/\.statistics-board-detail-table \.ant-table-content\s*\{[^}]*overflow-y:\s*auto\s*!important/s)
    // 明细行数变化引起纵向滚动条出现或消失时，保留滚动条槽位，表格内容区宽度不跳变。
    expect(css).toMatch(/\.statistics-board-detail-table \.ant-table-content\s*\{[^}]*scrollbar-gutter:\s*stable/s)
    expect(css).toMatch(/\.statistics-board-detail-table \.ant-table-thead > tr > th\s*\{[^}]*position:\s*sticky\s*!important/s)
    // 明细锚点填满右侧列，避免明细卡按表格最小宽度收缩。
    expect(css).toMatch(/\.statistics-board-detail-anchor\s*\{[^}]*width:\s*100%/s)
    expect(css).toMatch(/\.statistics-board-detail-anchor\s*\{[^}]*flex:\s*1 1 auto/s)
    expect(css).toMatch(/\.statistics-board-group-row\[aria-pressed='true'\]\s*\{[^}]*border-left-color/s)
    // 反馈结果列三种标签形态一致：不采纳标签在纵向单元格内按内容宽度收缩，不拉伸占满列宽。
    expect(css).toMatch(/\.statistics-board-cell__tag\s*\{[^}]*align-self:\s*flex-start/s)
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

  it('平台页两方范围卡并排：使用方在左、提供方在右，各带称谓、角色说明与方向标记（提供方同序）', () => {
    renderBoard()
    expect(screen.getByRole('region', { name: '使用方范围' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: '提供方范围' })).toBeInTheDocument()
    expect(screen.getByText('调阅并使用他院报告')).toBeInTheDocument()
    expect(screen.getByText('出具并提供报告')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: '报告流向：提供方 → 使用方' })).toBeInTheDocument()
    expect(screen.queryByText('报告来自')).toBeNull()
    expect(screen.queryByRole('region', { name: '提供方筛选' })).toBeNull()

    cleanup()
    // 提供方平台页：两方沿用同一顺序，使用方在左。
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      side: 'source',
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    }))
    expect(screen.getByRole('region', { name: '使用方范围' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: '提供方范围' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: '报告流向：提供方 → 使用方' })).toBeInTheDocument()
    expect(screen.queryByText('报告来自')).toBeNull()
    expect(screen.queryByRole('region', { name: '使用方筛选' })).toBeNull()
  })

  it('本院页页头左右结构：范围条在页头右侧，范围区只保留提供方筛选（C20、C39）', () => {
    renderBoard(boardProps({
      scopeKey: 'ORG-A|HOS-1|BRA1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A', hospitalCode: 'HOS-1' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))

    const header = document.querySelector('.statistics-board-header')
    expect(header).not.toBeNull()
    // 范围条（本用例环境无登录编码，三级按「未知」占位）渲染在页头容器内。
    expect(within(header as HTMLElement).getByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: '使用方范围' })).toBeNull()
    expect(screen.queryByRole('region', { name: '提供方范围' })).toBeNull()
    expect(screen.queryByRole('img', { name: '报告流向：提供方 → 使用方' })).toBeNull()
    expect(screen.queryByText('固定为登录范围，不可修改')).toBeNull()
    const oppositeFilter = within(screen.getByRole('region', { name: '提供方筛选' }))
    expect(oppositeFilter.getAllByRole('combobox')).toHaveLength(2)
    expect(oppositeFilter.queryByRole('combobox', { name: '组织' })).toBeNull()
    // 范围选择行不承载范围条。
    const scopeRow = document.querySelector('.statistics-board-scope')
    expect(scopeRow).not.toBeNull()
    expect(scopeRow?.textContent).not.toContain('未知 / 未知 / 未知')
  })
})

describe('票据 04：时间预设与范围复位', () => {
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
    // 一次提交同时重读明细（提交重读口径）：明细重读在汇总之后经效果排队执行，按到达条件等待。
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(2))
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

describe('票据 04：读取竞态、失败恢复与越界自愈', () => {
  it('只采纳最后一次汇总请求：读取在途时查询按钮禁用、再次点击不发请求，迟到旧响应不写入行数据（C9）', async () => {
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
    await chooseDimension('院区')
    await waitFor(() => expect(pending).toHaveLength(2))

    // 判别性顺序：先让最新响应到达并落地，再让过期旧响应到达——
    // 若无序号守卫，迟到的旧响应会覆盖页面数据；有序号守卫则新结果保留、迟到旧响应被丢弃。
    await act(async () => { pending[1]?.resolve(pageOf([{ ...usageSummaryRow, receiverBranchName: '院区九' }])) })
    await waitSummaryText('院区九')
    await act(async () => { pending[0]?.resolve(pageOf([usageSummaryRow])) })

    expect(within(summaryList()).getByText('院区九')).toBeInTheDocument()
    expect(within(summaryList()).queryByText('医院一')).toBeNull()
  })

  it('汇总读取失败不渲染错误横幅：已有数据保留、加载态结束，点查询重试成功（C18）', async () => {
    renderBoard()
    await waitSummaryText('医院一')

    // 提交触发重读失败：旧数据不被失败结果覆盖。
    summaryQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    // 让失败落地：拒绝在微任务即被读取入口吸收，一轮宏任务后加载态必然结束。
    await act(async () => {})

    // 既定口径：读取失败不渲染错误横幅（错误由宿主统一提示）；已有数据保留，具体错误文本不进入页面。
    expect(within(summaryList()).getByText('医院一')).toBeInTheDocument()
    expect(screen.queryByText(/待刷新|network down/)).toBeNull()

    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow])))
    // 重试经查询按钮。
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(3))
    expect(within(summaryList()).getByText('医院一')).toBeInTheDocument()
  })

  it('空结果显示左列表空态与右卡选择空态，不报错（C18）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([])))
    renderBoard()

    expect(await within(summaryList()).findByText('当前筛选条件没有汇总数据')).toBeInTheDocument()
    expect(within(detailPanel()).getByText('从左侧选择一行查看明细')).toBeInTheDocument()
    expect(detailsQuery).not.toHaveBeenCalled()
  })

  it('左列表当页为空且总数收缩为正：按服务端总数回退末页一次；维度变化后仍可再自愈一次（C19）', async () => {
    // 首次加载 80 条（10 页）；翻到第 10 页后服务端总数收缩为 12 并交付空页（越界）。
    const firstPageRows = Array.from({ length: 8 }, (_, index) => ({
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
    summaryQuery.mockImplementation((_filters, dimensionValue, page) => {
      callCount += 1
      if (callCount === 1) return Promise.resolve({ items: firstPageRows, pageIndex: 1, pageSize: 8, totalCount: 80 })
      if (callCount === 2) return Promise.resolve({ items: [], pageIndex: 10, pageSize: 8, totalCount: 12 })
      if (callCount === 3) return Promise.resolve({ items: lastPageRows, pageIndex: 2, pageSize: 8, totalCount: 12 })
      // 维度切换后的第 1 页仍为空、总数仍为 12：守卫键包含维度，允许再次回退末页。
      if (dimensionValue === 2 && page.pageIndex === 1) {
        return Promise.resolve({ items: [], pageIndex: 1, pageSize: 8, totalCount: 12 })
      }
      return Promise.resolve({ items: [branchUsageSummaryRow], pageIndex: 2, pageSize: 8, totalCount: 12 })
    })
    renderBoard()
    await waitSummaryText('医院0')

    fireEvent.click(within(summaryList()).getByTitle('10'))

    // 自愈：按服务端新总数换算末页（ceil(12/8)=2），由第 10 页回退到第 2 页并渲染末页两行；
    // 调用次数稳定为 3，同一条件不反复自愈。
    await waitFor(() => expect(within(summaryList()).getByText('医院10')).toBeInTheDocument())
    expect(within(summaryList()).getByText('医院11')).toBeInTheDocument()
    expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 1, { pageIndex: 2, pageSize: 8 })
    expect(summaryQuery).toHaveBeenCalledTimes(3)

    // 切换维度后第 1 页为空且总数仍为 12：守卫键不同，必须再次回退末页并渲染新维度行。
    await chooseDimension('院区')
    await waitFor(() => expect(within(summaryList()).getByText('院区一')).toBeInTheDocument())
    expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 2, { pageIndex: 2, pageSize: 8 })
    expect(summaryQuery).toHaveBeenCalledTimes(5)
  })

  it('关键字筛选改动只进草稿，点查询后随明细查询请求提交（C13）', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(1))

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
    expect(within(summaryList()).getByText('医院一')).toBeInTheDocument()
    expect(screen.queryByText(/待刷新/)).toBeNull()
  })

  it('本院使用方形态：仅提供方筛选，请求院区恒为登录院区；使用方医院、院区条件经草稿提交', async () => {
    renderBoard(boardProps({
      scopeKey: 'ORG-A|HOS-1|BRA1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A', hospitalCode: 'HOS-1' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))
    await waitSummaryText('医院一')

    // 本院页范围区的只读编码副文案由共用范围条取代：本用例环境无登录编码，三级按「未知」占位。
    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(screen.queryByText(/可信范围（只读）/)).not.toBeInTheDocument()
    // 院区恒取登录院区：本院页不渲染本院院区选择，请求院区参数恒为登录院区。
    expect(screen.queryByRole('group', { name: '本院院区' })).toBeNull()
    const initialFilters = summaryQuery.mock.calls[0]?.[0]
    expect(initialFilters).toEqual(expect.objectContaining({ ownBranchCode: 'BRA1' }))
    expect(initialFilters).not.toHaveProperty('sourceHospitalCode')
    expect(initialFilters).not.toHaveProperty('sourceBranchCode')

    const callCountAfterInitial = summaryQuery.mock.calls.length
    const oppositeFilter = screen.getByRole('region', { name: '提供方筛选' })
    const oppositeGroup = within(oppositeFilter)
    // 固定组织写入受控值，下游医院与院区选择器据此装载；组织级下拉不渲染。
    expect(oppositeFilter.querySelector('[data-org-id]')).toHaveAttribute('data-org-id', 'ORG-A')
    expect(oppositeGroup.getAllByRole('combobox')).toHaveLength(2)
    expect(oppositeGroup.queryByRole('combobox', { name: '组织' })).toBeNull()
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
    const submittedFilters = summaryQuery.mock.calls.at(-1)?.[0]
    expect(submittedFilters).not.toHaveProperty('sourceOrganizationCode')
  })

  it('本院提供方形态：仅使用方筛选，请求院区恒为登录院区；使用方医院、院区条件经草稿提交', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(boardProps({
      title: '本院被互认统计',
      side: 'source',
      scopeKey: 'ORG-A|HOS-1|BRA1',
      scopeSpec: { kind: 'branch', organizationCode: 'ORG-A', hospitalCode: 'HOS-1' },
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', ownBranchCode: 'BRA1' },
    }))
    await waitSummaryText('医院二')

    // 本院页的承接范围取可信组织、可信医院与登录院区：医院维度同样带院区。
    expect(detailPanel().querySelector('.statistics-board-detail-context')).toHaveTextContent('承接范围 组织甲 · 医院甲 · 院区一')

    const header = document.querySelector('.statistics-board-header')
    expect(header).not.toBeNull()
    expect(within(header as HTMLElement).getByText('未知 / 未知 / 未知')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: '使用方范围' })).toBeNull()
    expect(screen.queryByRole('region', { name: '提供方范围' })).toBeNull()
    expect(screen.queryByRole('img', { name: '报告流向：提供方 → 使用方' })).toBeNull()

    const initialFilters = summaryQuery.mock.calls[0]?.[0]
    expect(initialFilters).toEqual(expect.objectContaining({ ownBranchCode: 'BRA1' }))
    expect(initialFilters).not.toHaveProperty('receiverHospitalCode')
    expect(initialFilters).not.toHaveProperty('receiverBranchCode')

    const callCountAfterInitial = summaryQuery.mock.calls.length
    const oppositeFilter = screen.getByRole('region', { name: '使用方筛选' })
    const oppositeGroup = within(oppositeFilter)
    // 固定组织写入受控值，下游医院与院区选择器据此装载；组织级下拉不渲染。
    expect(oppositeFilter.querySelector('[data-org-id]')).toHaveAttribute('data-org-id', 'ORG-A')
    expect(oppositeGroup.getAllByRole('combobox')).toHaveLength(2)
    expect(oppositeGroup.queryByRole('combobox', { name: '组织' })).toBeNull()
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS1' } })
    fireEvent.change(oppositeGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA1' } })
    // 草稿改动零请求（提交式筛选口径）。
    expect(summaryQuery.mock.calls.length).toBe(callCountAfterInitial)

    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ ownBranchCode: 'BRA1', receiverHospitalCode: 'HOS1', receiverBranchCode: 'BRA1' }),
      1, expect.anything()))
    const submittedFilters = summaryQuery.mock.calls.at(-1)?.[0]
    expect(submittedFilters).not.toHaveProperty('receiverOrganizationCode')
    // 一次提交同时重读明细（提交重读口径）：初始读取一次，提交后再读一次。
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(2))
  })
})

describe('票据 04：导出入口保持既有条件', () => {
  it('汇总导出沿用已生效条件与维度；导出中禁用两个入口，明细导出随类型切换', async () => {
    const pending = deferred<void>()
    exportQuery.mockImplementation(() => pending.promise)
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(screen.getByRole('button', { name: '导出汇总' }))
    await waitFor(() => expect(exportQuery).toHaveBeenCalledTimes(1))
    expect(exportQuery).toHaveBeenLastCalledWith(
      expect.objectContaining({ startTime: '2026-02-01', endTime: '2026-02-28' }),
      expect.anything(),
      { exportType: 1, groupDimension: 1, detailType: null, downloadFileName: '使用方互认使用汇总-20260201-20260228.xlsx' },
    )
    expect(screen.getByRole('button', { name: '导出汇总' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '导出明细' })).toBeDisabled()

    await act(async () => { pending.resolve() })
    await waitFor(() => expect(screen.getByRole('button', { name: '导出汇总' })).toBeEnabled())

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '引用' }))
    await waitFor(() => expect(detailsQuery).toHaveBeenLastCalledWith(expect.anything(), 4, expect.anything()))
    fireEvent.click(screen.getByRole('button', { name: '导出明细' }))
    await waitFor(() => expect(exportQuery).toHaveBeenLastCalledWith(
      expect.anything(),
      expect.anything(),
      { exportType: 5, groupDimension: 1, detailType: 4, downloadFileName: '使用方引用明细-20260201-20260228.xlsx' },
    ))
  })

  it('无选中行时导出明细禁用', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([])))
    renderBoard()

    expect(await within(detailPanel()).findByText('从左侧选择一行查看明细')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '导出明细' })).toBeDisabled()
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

describe('票据 07：导出条件（C55）', () => {
  it('汇总导出只取已生效筛选与维度；明细导出叠加选中行精确条件、当前明细类型与原因；两者不带分页也不写地址栏（C55）', async () => {
    // 互认科室维度：选中行派生的明细条件比已生效筛选多出 RecognitionDeptId，用于区分两个入口的条件。
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(
      pageOf([dimension === 3 ? deptUsageSummaryRow : usageSummaryRow]),
    ))
    renderBoard(boardProps({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        receiverBranchCode: 'BRA-APPLIED',
        keyword: 'DOC-9',
      },
    }))
    await waitSummaryText('医院一')
    await chooseDimension('互认科室')
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[0]).toEqual(
      expect.objectContaining({ recognitionDeptId: 'DEPT1' })))
    const locationBefore = window.location.href

    fireEvent.click(screen.getByRole('button', { name: '导出汇总' }))
    await waitFor(() => expect(exportQuery).toHaveBeenCalledTimes(1))
    const [summaryFilters, , summaryRequest] = exportQuery.mock.calls[0]
    expect(summaryFilters).toEqual(expect.objectContaining({
      startTime: '2026-02-01',
      endTime: '2026-02-28',
      receiverBranchCode: 'BRA-APPLIED',
      keyword: 'DOC-9',
    }))
    // 汇总导出不带选中行的精确分组字段与非采纳原因。
    expect(summaryFilters).not.toHaveProperty('recognitionDeptId')
    expect(summaryFilters).not.toHaveProperty('standardProjectCode')
    expect(summaryFilters).not.toHaveProperty('nonAdoptionReasonCode')
    expect(summaryRequest).toEqual({
      exportType: 1,
      groupDimension: 3,
      detailType: null,
      downloadFileName: '使用方互认使用汇总-20260201-20260228.xlsx',
    })

    // 切到不采纳并选择原因：明细导出条件同时携带精确分组字段、明细类型与原因。
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[1]).toBe(3))
    openAntdSelect(within(detailPanel()).getByLabelText('不采纳原因'))
    fireEvent.click(portalOption('结果互认（2 次，100.00%）'))
    await waitFor(() => expect(detailsQuery.mock.calls.at(-1)?.[0]).toEqual(
      expect.objectContaining({ nonAdoptionReasonCode: 'R1' })))

    fireEvent.click(screen.getByRole('button', { name: '导出明细' }))
    await waitFor(() => expect(exportQuery).toHaveBeenCalledTimes(2))
    const [appliedFilters, derivedFilters, detailRequest] = exportQuery.mock.calls[1]
    // 已生效筛选保持原值；派生条件取选中行的组织、医院、院区与科室，并保留用户关键字与原因。
    expect(appliedFilters).toEqual(expect.objectContaining({
      receiverBranchCode: 'BRA-APPLIED',
      keyword: 'DOC-9',
    }))
    expect(derivedFilters).toEqual(expect.objectContaining({
      receiverOrganizationCode: 'ORG1',
      receiverHospitalCode: 'HOS1',
      receiverBranchCode: 'BRA1',
      recognitionDeptId: 'DEPT1',
      nonAdoptionReasonCode: 'R1',
      keyword: 'DOC-9',
    }))
    expect(detailRequest).toEqual({
      exportType: 4,
      groupDimension: 3,
      detailType: 3,
      downloadFileName: '使用方不采纳明细-20260201-20260228.xlsx',
    })
    // 导出请求不带分页字段，也不写入地址栏。
    expect(detailRequest).not.toHaveProperty('pageIndex')
    expect(detailRequest).not.toHaveProperty('pageSize')
    expect(window.location.href).toBe(locationBefore)
    expect(window.location.search).toBe('')
  })
})

describe('票据 07：失败面与异步（C54）', () => {
  it('汇总读取失败保留旧列表、旧选中与旧明细，结束加载且不渲染横幅（C54）', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await screen.findByText('张三')
    const summaryCalls = summaryQuery.mock.calls.length

    summaryQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    fireEvent.click(screen.getByRole('button', { name: '查询' }))
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(summaryCalls + 1))
    // 让拒绝落地：读取入口在微任务内吸收失败，加载态随后结束。
    await act(async () => {})
    await waitFor(() => expect(screen.getByRole('button', { name: '查询' })).not.toHaveClass('ant-btn-loading'))

    expect(within(summaryList()).getByText('医院一')).toBeInTheDocument()
    expect(within(summaryList()).getByRole('button', { name: /医院一/ })).toHaveAttribute('aria-pressed', 'true')
    expect(within(detailPanel()).getByText('张三')).toBeInTheDocument()
    // 既定口径：失败不渲染横幅，错误由宿主统一提示；具体错误文本不进入页面。
    expect(screen.queryByText(/待刷新|network down/)).toBeNull()
  })

  it('明细读取失败保留旧明细与选中行；提供方无页签时给出查询重试空态（C54）', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await screen.findByText('张三')
    const detailCalls = detailsQuery.mock.calls.length

    detailsQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    // 点击已选中行等价于重试当前明细：条件不变，旧明细不被清空。
    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院一/ }))
    await waitFor(() => expect(detailsQuery).toHaveBeenCalledTimes(detailCalls + 1))
    await act(async () => {})
    await waitFor(() => expect(screen.getByRole('button', { name: '查询' })).not.toHaveClass('ant-btn-loading'))

    expect(within(detailPanel()).getByText('张三')).toBeInTheDocument()
    expect(within(summaryList()).getByRole('button', { name: /医院一/ })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.queryByText(/待刷新|network down/)).toBeNull()

    // 提供方没有页签可切换：失败空态必须给出可执行的查询重试指引。
    cleanup()
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.reject(new Error('network down')))
    renderBoard(boardProps({
      title: '提供方医院被互认统计',
      subtitle: '按组织、医院与院区查询提供方被互认汇总与明细',
      side: 'source',
    }))
    await waitSummaryText('医院二')
    await waitFor(() => expect(within(detailPanel()).getByText('明细加载失败，请点击查询重试')).toBeInTheDocument())
    expect(within(detailPanel()).queryByText('明细加载失败，请切换页签或重试')).toBeNull()
  })

  it('汇总重读后迟到的旧明细响应不写回新汇总的明细区（C54）', async () => {
    const pendingDetail = deferred<StatisticsPage<UsageDetailRow | SourceDetailRow>>()
    let detailCalls = 0
    detailsQuery.mockImplementation(() => {
      detailCalls += 1
      if (detailCalls === 1) return pendingDetail.promise
      return Promise.resolve(detailPageOf([usageDetailProcessed]))
    })
    renderBoard()
    await waitSummaryText('医院一')
    await waitFor(() => expect(detailCalls).toBe(1))

    // 首笔明细仍在途时切换维度：汇总重读开始。
    await chooseDimension('院区')
    await waitFor(() => expect(summaryQuery).toHaveBeenCalledTimes(2))
    await screen.findByText('李四')

    // 旧明细响应迟到：属于旧分组，不写回新汇总的明细区。
    await act(async () => { pendingDetail.resolve(detailPageOf([usageDetailUnprocessed])) })

    expect(within(detailPanel()).queryByText('张三')).toBeNull()
    expect(within(detailPanel()).getByText('李四')).toBeInTheDocument()
    await waitFor(() => expect(detailPanel().querySelector('.ant-spin-spinning')).toBeNull())
  })
})

describe('票据 07：响应式与窄屏滚动（C56）', () => {
  /** jsdom 不做布局：按给定形态伪造主从区宽度与明细区顶边，单列堆叠时明细区在下方。 */
  function stubLayout(stacked: boolean) {
    const rectOf = (top: number, width: number) => ({
      top,
      bottom: top,
      left: 0,
      right: width,
      width,
      height: 0,
      x: 0,
      y: 0,
      toJSON: () => ({}),
    }) as DOMRect
    return vi.spyOn(Element.prototype, 'getBoundingClientRect').mockImplementation(function (this: Element) {
      const boardWidth = this.classList.contains('statistics-board') ? (stacked ? 1000 : 1600) : 0
      return rectOf(stacked && this.getAttribute('aria-label') === '明细' ? 720 : 0, boardWidth)
    })
  }

  /** jsdom 未实现滚动：用可观测替身记录滚动调用参数。 */
  function stubScrollIntoView() {
    const scrollIntoView = vi.fn()
    Object.defineProperty(Element.prototype, 'scrollIntoView', {
      configurable: true,
      writable: true,
      value: scrollIntoView,
    })
    return scrollIntoView
  }

  /** 与浏览器同形的 `matchMedia` 替身：只对减少动态偏好查询返回真。 */
  function stubReducedMotion() {
    return vi.spyOn(window, 'matchMedia').mockImplementation((query: string) => ({
      matches: query.includes('prefers-reduced-motion'),
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }) as MediaQueryList)
  }

  it('容器窄于 1200px 的单列堆叠下，选中行后把明细卡滚入视口；自动选中不滚动（C56）', async () => {
    const scrollIntoView = stubScrollIntoView()
    stubLayout(true)
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')

    // 首屏自动选中不滚动：滚动只由用户点选行触发。
    expect(scrollIntoView).not.toHaveBeenCalled()

    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    expect(scrollIntoView).toHaveBeenLastCalledWith({ behavior: 'smooth', block: 'start' })
  })

  it('prefers-reduced-motion 下使用即时滚动（C56）', async () => {
    const scrollIntoView = stubScrollIntoView()
    stubLayout(true)
    stubReducedMotion()
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    expect(scrollIntoView).toHaveBeenLastCalledWith({ behavior: 'auto', block: 'start' })
  })

  it('两列并排时不滚动明细卡（C56）', async () => {
    const scrollIntoView = stubScrollIntoView()
    stubLayout(false)
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([usageSummaryRow, secondUsageSummaryRow])))
    renderBoard()
    await waitSummaryText('医院一')

    fireEvent.click(within(summaryList()).getByRole('button', { name: /医院九/ }))
    expect(scrollIntoView).not.toHaveBeenCalled()
  })
})

describe('票据 07：可访问性（C57）', () => {
  it('左行是原生按钮并带 aria-pressed；指标摘要为静态文本；视觉隐藏 aria-live 播报选中行与明细总数（C57）', async () => {
    renderBoard()
    await waitSummaryText('医院一')
    await screen.findByText('张三')

    const row = within(summaryList()).getByRole('button', { name: /医院一/ })
    expect(row.tagName).toBe('BUTTON')
    expect(row).toHaveAttribute('aria-pressed', 'true')
    // 可访问名称包含名称与四个指标。
    expect(row).toHaveAccessibleName(/医院一.*提醒 10.*采纳 6.*不采纳 2.*引用 4/)

    const summary = within(detailPanel()).getByRole('group', { name: '指标摘要' })
    expect(summary.querySelector('button')).toBeNull()
    expect(summary).toHaveTextContent('采纳')

    const live = document.querySelector('.statistics-board-live')
    expect(live).not.toBeNull()
    expect(live).toHaveAttribute('role', 'status')
    expect(live).toHaveAttribute('aria-live', 'polite')
    expect(live?.textContent).toContain('已选择医院一')
    expect(live?.textContent).toContain('提醒 10 条')
    await waitFor(() => expect(live?.textContent).toContain('明细共 2 条'))

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '采纳' }))
    await waitFor(() => expect(live?.textContent).toContain('当前 采纳 6 条'))
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(live?.textContent).toContain('当前 不采纳 2 条'))
    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '引用' }))
    await waitFor(() => expect(live?.textContent).toContain('当前 引用 4 条'))
  })

  it('页签、左右分页、行展开按钮与原因选择器都有可访问名称（C57）', async () => {
    renderBoard()
    await screen.findByText('张三')

    for (const name of ['提醒', '采纳', '不采纳', '引用']) {
      expect(within(detailPanel()).getByRole('tab', { name })).toBeInTheDocument()
    }
    expect(within(summaryList()).getByRole('list', { name: '汇总列表分页' })).toBeInTheDocument()
    expect(within(detailPanel()).getByRole('list', { name: '明细列表分页' })).toBeInTheDocument()
    expect(within(detailPanel()).getAllByRole('button', { name: '展开行' })).toHaveLength(2)

    fireEvent.click(within(detailPanel()).getByRole('tab', { name: '不采纳' }))
    await waitFor(() => expect(within(detailPanel()).getByLabelText('不采纳原因')).toBeInTheDocument())
  })

  it('多实例页签/面板 id 唯一（同 tree 与独立 React root），aria-controls 指向各自真实 tabpanel，当前页签内容与表格在同一面板内（C57）', async () => {
    const { container } = render(
      <ConfigProvider locale={zhCN}>
        <StatisticsBoard {...boardProps()} />
        <StatisticsBoard {...boardProps()} />
      </ConfigProvider>,
    )
    await waitFor(() => expect(within(container).getAllByText('张三')).toHaveLength(2), { timeout: 3000 })

    const boards = Array.from(container.querySelectorAll<HTMLElement>('.statistics-board'))
    expect(boards).toHaveLength(2)
    const firstBoard = boards[0]
    const secondBoard = boards[1]
    if (firstBoard === undefined || secondBoard === undefined) throw new Error('统计主体实例不存在')

    const allTabIds: string[] = []
    for (const board of [firstBoard, secondBoard]) {
      const tabs = within(board).getAllByRole('tab')
      expect(tabs).toHaveLength(4)
      for (const tab of tabs) {
        allTabIds.push(tab.id)
        const controls = tab.getAttribute('aria-controls')
        expect(controls).not.toBeNull()
        const panel = controls === null ? null : document.getElementById(controls)
        if (panel === null) throw new Error(`页签对应的 tabpanel 不存在：${controls ?? ''}`)
        expect(panel).toHaveAttribute('role', 'tabpanel')
        expect(panel).toHaveAttribute('aria-labelledby', tab.id)
        // 面板必须属于当前主体的实例前缀，不能落到另一实例。
        expect(board.contains(panel)).toBe(true)
      }
      const active = tabs.find((tab) => tab.getAttribute('aria-selected') === 'true')
      expect(active).toBeDefined()
      const activePanel = document.getElementById(active?.getAttribute('aria-controls') ?? '')
      expect(activePanel?.querySelector('.statistics-board-detail-table')).not.toBeNull()
    }
    // 多实例宿主：两个主体的页签/面板 id 不得重复。
    expect(allTabIds).toHaveLength(8)
    expect(new Set(allTabIds).size).toBe(8)

    // 键盘切换后的焦点必须落在当前实例的页签，而不是另一实例的同名页签。
    const firstBoardTabs = within(firstBoard).getAllByRole('tab')
    const secondBoardTabs = within(secondBoard).getAllByRole('tab')
    fireEvent.keyDown(firstBoardTabs[0], { key: 'ArrowRight' })
    await waitFor(() => expect(firstBoardTabs[1]).toHaveAttribute('aria-selected', 'true'))
    await waitFor(() => expect(document.activeElement).toBe(firstBoardTabs[1]))
    expect(secondBoardTabs[1]).toHaveAttribute('aria-selected', 'false')

    // 真实宿主的两个 micro-app 各自是独立 React root：useId 会在每个 root 内从头开始，前缀必须仍全局唯一。
    cleanup()
    const firstRoot = render(<ConfigProvider locale={zhCN}><StatisticsBoard {...boardProps()} /></ConfigProvider>)
    const secondRoot = render(<ConfigProvider locale={zhCN}><StatisticsBoard {...boardProps()} /></ConfigProvider>)
    await waitFor(() => {
      expect(within(firstRoot.container).getAllByText('张三')).toHaveLength(1)
      expect(within(secondRoot.container).getAllByText('张三')).toHaveLength(1)
    }, { timeout: 3000 })

    const rootTabIds: string[] = []
    for (const rootContainer of [firstRoot.container, secondRoot.container]) {
      const tabs = within(rootContainer).getAllByRole('tab')
      expect(tabs).toHaveLength(4)
      for (const tab of tabs) {
        rootTabIds.push(tab.id)
        const controls = tab.getAttribute('aria-controls')
        expect(controls).not.toBeNull()
        const panel = controls === null ? null : document.getElementById(controls)
        if (panel === null) throw new Error(`独立 root 的 tabpanel 不存在：${controls ?? ''}`)
        expect(rootContainer.contains(panel)).toBe(true)
        expect(panel).toHaveAttribute('aria-labelledby', tab.id)
      }
    }
    expect(rootTabIds).toHaveLength(8)
    expect(new Set(rootTabIds).size).toBe(8)
  })
})

describe('票 03：提供方承接方核查版式（C60、C61）', () => {
  /** 提供方平台页主体入参：标题、副标题与侧别固定，条件只覆盖本组用例关心的部分。 */
  const sourceBoard = (overrides: Partial<StatisticsBoardProps> = {}) => boardProps({
    title: '提供方医院被互认统计',
    subtitle: '按组织、医院与院区查询提供方医院被互认汇总与明细',
    side: 'source',
    initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28' },
    ...overrides,
  })

  it('左栏为两行式分组清单：主名称与归属次行各占一行，行尾计数块为等宽数字加「被互认」小字，不绘制比较条（C60）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([
      { ...sourceSummaryRow, sourceBranchCode: 'BRA2', sourceBranchName: '院区二' },
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard())
    await waitSummaryText('医院二')

    const row = within(summaryList()).getByRole('button', { name: /医院二/ })
    expect(row).toHaveAttribute('aria-pressed', 'true')
    expect(row.querySelector('.statistics-board-group-row__name')).toHaveTextContent('医院二')
    expect(row.querySelector('.statistics-board-group-row__secondary')).toHaveTextContent('组织二')
    expect(row.querySelector('.statistics-board-group-row__count-value')).toHaveTextContent('7')
    expect(row.querySelector('.statistics-board-group-row__count-label')).toHaveTextContent('被互认')
    // 不绘制比较条：行内没有任何进度语义元素。
    expect(row.querySelectorAll('[role="progressbar"]').length).toBe(0)
  })

  it('明细首列为使用方三行式并固定在左侧，其后各列按承接方核查口径排列（C60、C61）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard({
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', sourceBranchCode: 'BRA2' },
    }))
    await waitSummaryText('医院二')
    await screen.findByText('张三')

    const table = detailTable()
    const headerCells = within(table).getAllByRole('columnheader')
    expect(headerCells.map((node) => node.textContent)).toEqual([
      '使用方', '标准项目', '项目类型', '分类', '分组', '互认时间', '患者', '科室 / 医生',
    ])
    // antd 6 的固定列类名为逻辑方向写法（fix-start），两种写法都表达同一固定语义。
    expect(headerCells[0].className).toContain('ant-table-cell-fix')
    const receiverCell = within(table).getAllByText('组织一')[0]?.closest('td') ?? null
    expect(receiverCell).not.toBeNull()
    expect(within(receiverCell as HTMLElement).getByText('医院一')).toBeInTheDocument()
    expect(within(receiverCell as HTMLElement).getByText('院区一')).toBeInTheDocument()
    expect(receiverCell?.className).toContain('ant-table-cell-fix')
  })

  it('提供方院区列仅在医院维度且平台页未锁院区时出现，并排在明细末列（C61）', async () => {
    summaryQuery.mockImplementation(() => Promise.resolve(pageOf([sourceSummaryRow])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard())
    await waitSummaryText('医院二')
    const unlocked = within(detailTable()).getAllByRole('columnheader').map((node) => node.textContent)
    expect(unlocked.at(-1)).toBe('提供方院区')
    cleanup()

    renderBoard(sourceBoard({
      initialFilters: { startTime: '2026-02-01', endTime: '2026-02-28', sourceBranchCode: 'BRA2' },
    }))
    await waitSummaryText('医院二')
    const locked = within(detailTable()).getAllByRole('columnheader').map((node) => node.textContent)
    expect(locked).not.toContain('提供方院区')
  })

  it('标准项目维度卡头与左栏次行：编码缺失补「—」，分类与分组同时为空时整段省略（C61）', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 4
        ? { ...projectSourceSummaryRow, standardProjectCode: '', categoryName: null, groupName: null }
        : sourceSummaryRow,
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard())
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')

    await waitSummaryText('血常规')
    const row = within(summaryList()).getByRole('button', { name: /血常规/ })
    expect(row.querySelector('.statistics-board-group-row__secondary')?.textContent).toBe('—')
    const head = detailPanel().querySelector<HTMLElement>('.ant-card-head')
    expect(head?.querySelector('.statistics-board-source-chain__secondary')?.textContent).toBe('—')
    cleanup()

    // 编码存在、分类与分组同时为空：只显示编码，不追加占位分段。
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 4
        ? { ...projectSourceSummaryRow, standardProjectCode: 'P200', categoryName: null, groupName: null }
        : sourceSummaryRow,
    ])))
    renderBoard(sourceBoard())
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')
    await waitSummaryText('血常规')
    const codedRow = within(summaryList()).getByRole('button', { name: /血常规/ })
    expect(codedRow.querySelector('.statistics-board-group-row__secondary')?.textContent).toBe('P200')
  })

  it('标准项目维度卡头下的上下文行按已生效筛选显示承接范围，不读汇总行空值、不重复项目名称与编码；左栏不渲染类型标签（C60、C61）', async () => {
    // 标准项目维度的真实形态：服务端把提供方组织与医院置空，上下文行的承接范围必须来自筛选。
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 4 ? projectSourceSummaryRow : sourceSummaryRow,
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceOrganizationCode: 'ORG-B',
        sourceHospitalCode: 'HOS-2',
      },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')
    await waitSummaryText('血常规')
    // 维度切换后明细按新选中行重读，等上下文行的总数到位再断言文案。
    await waitFor(() => expect(detailPanel().querySelector('.statistics-board-detail-context')).toHaveTextContent('共 1 条'))

    const row = within(summaryList()).getByRole('button', { name: /血常规/ })
    expect(row.querySelector('.ant-tag')).toBeNull()

    const contexts = detailPanel().querySelectorAll('.statistics-board-detail-context')
    expect(contexts).toHaveLength(1)
    expect(contexts[0]).toHaveTextContent('统计时间 2026-02-01 至 2026-02-28 · 承接范围 组织乙 · 医院乙 · 共 1 条')
    expect(contexts[0]).not.toHaveTextContent('承接范围 —')
    expect(contexts[0]).not.toHaveTextContent('ORG-B')
    expect(contexts[0]).not.toHaveTextContent('血常规')
    expect(contexts[0]).not.toHaveTextContent('P200')
  })

  it('承接范围反查失败时显示「未知」且不回退编码（C60）', async () => {
    summaryQuery.mockImplementation((_filters, dimension) => Promise.resolve(pageOf([
      dimension === 4 ? projectSourceSummaryRow : sourceSummaryRow,
    ])))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard({
      initialFilters: {
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        sourceOrganizationCode: 'ORG-X',
        sourceHospitalCode: 'HOS-X',
      },
    }))
    await waitSummaryText('医院二')
    await chooseDimension('标准项目')
    await waitSummaryText('血常规')
    await waitFor(() => expect(detailPanel().querySelector('.statistics-board-detail-context')).toHaveTextContent('共 1 条'))

    const context = detailPanel().querySelector('.statistics-board-detail-context')
    expect(context).toHaveTextContent('承接范围 未知 · 未知')
    // 不回退编码。
    expect(context).not.toHaveTextContent('ORG-X')
    expect(context).not.toHaveTextContent('HOS-X')
  })

  it('左栏换页清空选中并在新页读取完成后自动选中第 1 行，维度切换回第 1 页（C60）', async () => {
    summaryQuery.mockImplementation((_filters, _dimension, page) => Promise.resolve({
      items: page.pageIndex === 2
        ? [{ ...sourceSummaryRow, sourceHospitalCode: 'HOS9', sourceHospitalName: '医院九' }]
        : [sourceSummaryRow],
      pageIndex: page.pageIndex,
      pageSize: 8,
      totalCount: 9,
    }))
    detailsQuery.mockImplementation(() => Promise.resolve(detailPageOf([sourceDetailRow])))
    renderBoard(sourceBoard())
    await waitSummaryText('医院二')

    fireEvent.click(within(summaryList()).getByTitle('2'))
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 1, { pageIndex: 2, pageSize: 8 }))
    await waitSummaryText('医院九')
    // 换页清空旧选中，新页读取完成后自动选中第 1 行。
    expect(within(summaryList()).getByRole('button', { name: /医院九/ })).toHaveAttribute('aria-pressed', 'true')

    await chooseDimension('标准项目')
    await waitFor(() => expect(summaryQuery).toHaveBeenLastCalledWith(expect.anything(), 4, { pageIndex: 1, pageSize: 8 }))
  })

  it('配套 CSS 承载左栏三档宽度、行尾计数块、归属链卡头与 1200px 以下的横向分组选择带（C60 静态面）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/recognitionStatistics/statisticsBoard.css'), 'utf8')
    // 左栏宽度：默认 400px，容器 ≥1600px 取 440px，1200–1279px 取 360px，<1200px 单列。
    // 提供方列表区沿用基类网格与区块规则，只覆盖栏宽档位，不重复基类的网格、区块与卡片宽度。
    expect(css).not.toMatch(/\.statistics-board-master--source\s*\{\s*display:\s*grid/s)
    expect(css).not.toMatch(/\.statistics-board-master--source \.ant-card\s*\{/s)
    expect(css).toMatch(/@container \(min-width:\s*1600px\)[\s\S]*?\.statistics-board-master--source\s*\{[^}]*grid-template-columns:\s*440px/s)
    expect(css).toMatch(/@container \(max-width:\s*1279px\)[\s\S]*?\.statistics-board-master--source\s*\{[^}]*grid-template-columns:\s*360px/s)
    expect(css).toMatch(/@container \(max-width:\s*1199px\)[\s\S]*?\.statistics-board-master--source\s*\{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\)/s)
    // 小于 1200px：分组清单退化为横向选择带（单格 168×112、单行横向滚动、分页压到带右端）。
    expect(css).toMatch(/@container \(max-width:\s*1199px\)[\s\S]*?\.statistics-board-master--source \.statistics-board-list\s*\{[^}]*display:\s*flex[^}]*overflow-x:\s*auto/s)
    expect(css).toMatch(/@container \(max-width:\s*1199px\)[\s\S]*?\.statistics-board-master--source \.statistics-board-group-row\s*\{[^}]*flex:\s*0 0 auto[^}]*min-height:\s*112px[^}]*width:\s*168px/s)
    expect(css).toMatch(/@container \(max-width:\s*1199px\)[\s\S]*?\.statistics-board-master--source \.statistics-board-list__pager\s*\{[^}]*flex:\s*0 0 auto/s)
    // 行尾计数块：等宽数字加「被互认」小字。
    expect(css).toMatch(/\.statistics-board-group-row__count-value\s*\{[^}]*font-variant-numeric:\s*tabular-nums/s)
    expect(css).toMatch(/\.statistics-board-group-row__count-label\s*\{[^}]*font-size:\s*12px/s)
    // 归属链卡头三行：医院 14px 加粗，组织与院区 12px。
    expect(css).toMatch(/\.statistics-board-source-chain__hospital\s*\{[^}]*font-size:\s*14px[^}]*font-weight:\s*600/s)
    expect(css).toMatch(/\.statistics-board-source-chain__organization\s*\{[^}]*font-size:\s*12px/s)
    expect(css).toMatch(/\.statistics-board-source-chain__branch\s*\{[^}]*font-size:\s*12px/s)
  })
})
