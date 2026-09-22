/**
 * 「接收医院互认使用统计」「本院互认使用统计」页面入口 Component 层用例
 * （阶段 6 前端测试矩阵 C10、C13、C15、C20 的页面接线面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. 接收侧适配层只替换 I/O 出口（汇总/明细查询、匹配记录读取、导出），请求构造等纯函数
 *    使用真实实现——用例断言的是**经真实构造校验的 built 查询**；本院页「不提交本侧组织与医院」
 *    由可辨识联合类型与构造函数共同保证，用例再以字段缺失断言冻结该口径；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client，页面把 client 原样透传给适配层出口；
 * 3. `@dy/components-base` 只替换整个包（该包 ESM 产物无法被 vitest 加载，理由同报告管理页用例），
 *    替身复现页面接线依赖的组件契约：受控 `value`、固定 `orgId`/`hosId` 对应级不渲染、
 *    变化经 `onChange` 回传完整范围值；
 * 4. 可信组织与可信医院两个接缝替换为可控值，构造「可信范围就绪」与「可信范围不可用」两类场景；
 * 5. 枚举元数据钩子替换为直接返回兜底选项（元数据请求属 Client 边界 I/O，不属于页面行为面）。
 *
 * 主体行为（复位、竞态、分页、口径展示）由 statisticsBoard.test.tsx 覆盖，本文件不重复。
 * 仍属 Host 层：真实宿主菜单进入、真实范围选择器交互与宿主统一错误提示（C23-C30）。
 */
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import type {
  MatchRecordView,
  UsageDetailRow,
  UsageSummaryRow,
} from './recognitionUsageStatisticsApi'
import { BranchRecognitionUsageStatistics, RecognitionUsageStatistics } from './usageStatisticsPages'

const usageApi = vi.hoisted(() => ({
  queryUsageSummaryPage: vi.fn(),
  queryUsageDetailsPage: vi.fn(),
  queryRecognitionMatchRecord: vi.fn(),
  exportUsageStatistics: vi.fn(),
}))

vi.mock('./recognitionUsageStatisticsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./recognitionUsageStatisticsApi')>()
  return { ...actual, ...usageApi }
})

/** 可信范围接缝的可控值：组织、医院与可信院区，缺失场景在用例内改写。 */
const trustedScopeRef = vi.hoisted(() => ({
  organizationReady: true,
  hospitalReady: true,
}))

vi.mock('../recognitionProjects/recognitionProjectsOrganizationScope', () => ({
  useTrustedOrganizationScope: () => (trustedScopeRef.organizationReady
    ? { kind: 'ready', organizationCode: 'ORG-A' }
    : { kind: 'unavailable' }),
}))

vi.mock('../reportManagement/reportsTrustedScope', () => ({
  useTrustedHospitalScope: () => (trustedScopeRef.hospitalReady
    ? { kind: 'ready', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }
    : { kind: 'unavailable' }),
  readTrustedHospitalCode: () => (trustedScopeRef.hospitalReady ? 'HOS-1' : null),
  readTrustedBranchCode: () => null,
}))

vi.mock('@dy/components-base', async () => {
  const { createElement } = await import('react')

  /** 范围选择器替身：固定 `orgId`/`hosId` 的对应级不渲染下拉，变化回传完整范围值。 */
  function StubScopeSelector({ value = {}, orgId, hosId, className, onChange }: BaseScopeSelectorProps) {
    const levels: Array<{ key: 'orgId' | 'hosId' | 'branchId'; label: string; options: string[] }> = []
    if (orgId === undefined) levels.push({ key: 'orgId', label: '组织', options: ['ORG-A', 'ORG-B'] })
    if (hosId === undefined) levels.push({ key: 'hosId', label: '医院', options: ['HOS-1', 'HOS-2'] })
    levels.push({ key: 'branchId', label: '院区', options: ['BRA-1', 'BRA-2'] })
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
        level.options.map((id) => createElement('option', { key: id, value: id }, id)),
      )),
    )
  }

  return { configureBaseComponents: () => {}, BaseScopeSelector: StubScopeSelector }
})

vi.mock('../../hooks/useEnumMetadata', () => ({
  useEnumMetadata: (_enumName: string, fallback: readonly unknown[]) => fallback,
}))

/** 占位 client：页面把 API Client 原样交给适配层出口，自身不访问任何端点。 */
const clientRef = vi.hoisted(() => ({ current: { marker: 'usage-page-client' } }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

// ========== 视图模型夹具（适配层出口的页面分页模型行） ==========

const usageSummaryRow: UsageSummaryRow = {
  groupDimension: 1,
  receiverOrganizationCode: 'ORG-A',
  receiverOrganizationName: '组织甲',
  receiverHospitalCode: 'HOS-1',
  receiverHospitalName: '医院甲',
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
  reminderCount: 3,
  adoptionCount: 2,
  nonAdoptionCount: 1,
  referenceCount: 1,
  samePeriodRecognitionRate: null,
  estimatedSavingAmount: null,
  nonAdoptionReasons: [],
}

const usageDetailRow: UsageDetailRow = {
  recognitionMatchRecordId: 'RECORD-1',
  recognitionMatchItemId: 'ITEM-1',
  matchCreatedTime: new Date('2026-02-10T08:30:00'),
  businessTime: new Date('2026-02-10T08:30:00'),
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'MZ001',
  source: { organizationCode: 'ORG-B', organizationName: '组织乙', hospitalCode: 'HOS-2', hospitalName: '医院乙', branchCode: '', branchName: '' },
  receiver: { organizationCode: 'ORG-A', organizationName: '组织甲', hospitalCode: 'HOS-1', hospitalName: '医院甲', branchCode: 'BRA-1', branchName: '院区一' },
  item: { itemType: 0, itemTypeText: '检验', categoryName: null, groupName: null, standardProjectCode: 'A01', standardProjectName: '血常规' },
  isUnprocessed: true,
  patientName: '张三',
  identityDocumentNo: '110101Y001',
  recognitionDeptId: null,
  recognitionDeptName: null,
  recognitionDoctorId: null,
  recognitionDoctorName: null,
  processingResult: null,
  reference: null,
}

const matchRecordView: MatchRecordView = {
  recognitionMatchRecordId: 'RECORD-1',
  matchCreatedTime: new Date('2026-02-10T08:30:00'),
  receiver: { organizationCode: 'ORG-A', organizationName: '组织甲', hospitalCode: 'HOS-1', hospitalName: '医院甲', branchCode: 'BRA-1', branchName: '院区一' },
  patientName: '张三',
  identityDocumentNo: '110101Y001',
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'MZ001',
  isProcessed: false,
  recognitionTime: null,
  recognitionDeptId: null,
  recognitionDeptName: null,
  recognitionDoctorId: null,
  recognitionDoctorName: null,
  matchItems: [],
}

const pageOf = <Row,>(items: Row[]) => ({ items, pageIndex: 1, pageSize: 10, totalCount: items.length })

beforeEach(() => {
  vi.clearAllMocks()
  trustedScopeRef.organizationReady = true
  trustedScopeRef.hospitalReady = true
  usageApi.queryUsageSummaryPage.mockImplementation(async () => pageOf([usageSummaryRow]))
  usageApi.queryUsageDetailsPage.mockImplementation(async () => pageOf([usageDetailRow]))
  usageApi.queryRecognitionMatchRecord.mockImplementation(async () => matchRecordView)
  usageApi.exportUsageStatistics.mockImplementation(async () => {})
})

function renderPage(component: React.ReactElement) {
  return render(<ConfigProvider locale={zhCN}>{component}</ConfigProvider>)
}

describe('接收医院互认使用统计（平台页）', () => {
  it('进入自动加载：两路查询以占位 client 与平台版 built 查询发起，默认当月日期与默认维度、默认明细类型', async () => {
    renderPage(<RecognitionUsageStatistics />)

    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenCalledTimes(1))
    expect(usageApi.queryUsageDetailsPage).toHaveBeenCalledTimes(1)

    const [summaryClient, summaryBuilt] = usageApi.queryUsageSummaryPage.mock.calls[0]
    expect(summaryClient).toBe(clientRef.current)
    expect(summaryBuilt).toMatchObject({ version: 'platform', groupDimension: 1, pageIndex: 1, pageSize: 10 })
    expect(String(summaryBuilt.startTime)).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(String(summaryBuilt.endTime)).toMatch(/^\d{4}-\d{2}-\d{2}$/)

    const [detailsClient, detailsBuilt] = usageApi.queryUsageDetailsPage.mock.calls[0]
    expect(detailsClient).toBe(clientRef.current)
    expect(detailsBuilt).toMatchObject({ version: 'platform', detailType: 1, pageIndex: 1, pageSize: 10 })
  })

  it('两组范围与科室、项目条件随查询提交（C10）', async () => {
    renderPage(<RecognitionUsageStatistics />)

    const receiverGroup = within(screen.getByRole('group', { name: '接收组范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS-2' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'platform', receiverHospitalCode: 'HOS-2' }),
    ))

    const sourceGroup = within(screen.getByRole('group', { name: '来源组范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '组织' }), { target: { value: 'ORG-B' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'platform', sourceOrganizationCode: 'ORG-B' }),
    ))

    fireEvent.change(screen.getByLabelText('互认科室筛选'), { target: { value: 'DEPT-9' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ recognitionDeptId: 'DEPT-9' }),
    ))

    fireEvent.change(screen.getByLabelText('标准项目编码筛选'), { target: { value: 'A01' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ standardProjectCode: 'A01' }),
    ))
  })

  it('导出汇总与导出明细分别沿用当前条件构造服务端同名下载名（C15）', async () => {
    renderPage(<RecognitionUsageStatistics />)
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenCalled())

    fireEvent.click(screen.getByRole('button', { name: '导出汇总' }))
    await waitFor(() => expect(usageApi.exportUsageStatistics).toHaveBeenCalledTimes(1))
    const [summaryClient, summaryQuery, summaryName] = usageApi.exportUsageStatistics.mock.calls[0]
    expect(summaryClient).toBe(clientRef.current)
    expect(summaryQuery).toMatchObject({ version: 'platform', exportType: 1, groupDimension: 1 })
    expect(summaryQuery).not.toHaveProperty('pageIndex')
    expect(summaryQuery).not.toHaveProperty('pageSize')
    expect(summaryName).toMatch(/^接收侧互认使用汇总-\d{8}-\d{8}\.xlsx$/)

    fireEvent.click(within(screen.getByRole('region', { name: '使用明细' })).getByRole('radio', { name: '引用' }))
    fireEvent.click(screen.getByRole('button', { name: '导出明细' }))
    await waitFor(() => expect(usageApi.exportUsageStatistics).toHaveBeenCalledTimes(2))
    const [, detailsQuery, detailsName] = usageApi.exportUsageStatistics.mock.calls[1]
    expect(detailsQuery).toMatchObject({ version: 'platform', exportType: 5, groupDimension: 1 })
    expect(detailsName).toMatch(/^接收侧引用明细-\d{8}-\d{8}\.xlsx$/)
  })

  it('明细行提供匹配记录出口：以 client 与行记录标识发起单记录查询（C13、C14 接线面）', async () => {
    renderPage(<RecognitionUsageStatistics />)
    await screen.findByText('MZ001')

    fireEvent.click(screen.getAllByRole('button', { name: '查看互认匹配记录' })[0])

    await waitFor(() => expect(usageApi.queryRecognitionMatchRecord).toHaveBeenCalledWith(clientRef.current, 'RECORD-1'))
  })

  it('日期被清空后条件不完整：适配层查询出口零新增调用（C10 阻断面）', async () => {
    renderPage(<RecognitionUsageStatistics />)
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenCalledTimes(1))

    fireEvent.change(screen.getByLabelText('统计开始日期'), { target: { value: '' } })

    // 页面出口构造 built 失败返回 null：主体零发请求，适配层两个查询出口均不新增调用。
    await waitFor(() => expect(screen.getByText('当前筛选条件没有汇总数据')).toBeInTheDocument())
    expect(usageApi.queryUsageSummaryPage).toHaveBeenCalledTimes(1)
    expect(usageApi.queryUsageDetailsPage).toHaveBeenCalledTimes(1)
  })
})

describe('本院互认使用统计（本院页）', () => {
  it('可信范围固定展示；built 查询为本院版且不含本侧组织与医院；本侧院区默认全院（C20）', async () => {
    renderPage(<BranchRecognitionUsageStatistics />)

    expect(await screen.findByText(/可信范围（只读）：组织 ORG-A，医院 HOS-1/)).toBeInTheDocument()
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenCalled())

    const built = usageApi.queryUsageSummaryPage.mock.calls[0][1]
    expect(built).toMatchObject({ version: 'branch' })
    expect(built).not.toHaveProperty('organizationCode')
    expect(built).not.toHaveProperty('hospitalCode')
    // 本侧院区不选择即全院：院区条件未提供（请求不提交该层过滤）。
    expect((built as { branchCode?: string }).branchCode).toBeUndefined()

    const detailsBuilt = usageApi.queryUsageDetailsPage.mock.calls[0][1]
    expect(detailsBuilt).toMatchObject({ version: 'branch' })
    expect(detailsBuilt).not.toHaveProperty('organizationCode')
    expect(detailsBuilt).not.toHaveProperty('hospitalCode')
  })

  it('本院院区与来源组医院、院区选择随查询提交，组织字段仍不提交（C20）', async () => {
    renderPage(<BranchRecognitionUsageStatistics />)
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenCalled())

    const ownGroup = within(screen.getByRole('group', { name: '本院院区' }))
    expect(ownGroup.getAllByRole('combobox')).toHaveLength(1)
    fireEvent.change(ownGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA-2' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'branch', branchCode: 'BRA-2' }),
    ))

    const sourceGroup = within(screen.getByRole('group', { name: '来源医院范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS-2' } })
    await waitFor(() => expect(usageApi.queryUsageSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'branch', sourceHospitalCode: 'HOS-2' }),
    ))

    const lastBuilt = usageApi.queryUsageSummaryPage.mock.calls.at(-1)?.[1] as Record<string, unknown>
    expect(lastBuilt).not.toHaveProperty('organizationCode')
    expect(lastBuilt).not.toHaveProperty('hospitalCode')
    expect(lastBuilt).not.toHaveProperty('sourceOrganizationCode')
  })

  it('可信上下文缺失时整页阻断：不发请求、不渲染范围选择器（C20）', async () => {
    trustedScopeRef.organizationReady = false
    trustedScopeRef.hospitalReady = false
    renderPage(<BranchRecognitionUsageStatistics />)

    expect(await screen.findByRole('heading', { name: '本院互认使用统计' })).toBeInTheDocument()
    expect(screen.getByText('可信范围不可用')).toBeInTheDocument()
    expect(screen.queryByRole('combobox')).toBeNull()
    expect(usageApi.queryUsageSummaryPage).not.toHaveBeenCalled()
    expect(usageApi.queryUsageDetailsPage).not.toHaveBeenCalled()
  })
})
