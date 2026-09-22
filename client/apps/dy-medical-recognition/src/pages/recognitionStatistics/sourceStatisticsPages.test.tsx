/**
 * 「来源医院被互认统计」「本院被互认统计」页面入口 Component 层用例
 * （阶段 6 前端测试矩阵 C15、C21、C22 的页面接线面）。
 *
 * 装配边界与接收侧页面用例一致（遵循 Frontend Testing 第 2 节）：来源侧适配层只替换 I/O 出口，
 * 请求构造等纯函数使用真实实现；占位 client 原样透传；范围选择器替换整个包；
 * 可信组织与可信医院接缝替换为可控值；枚举元数据钩子返回兜底选项。
 *
 * 主体行为（来源侧口径展示、下钻、分页）由 statisticsBoard.test.tsx 覆盖，本文件不重复。
 */
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseScopeSelectorProps } from '@dy/components-base'
import type {
  SourceDetailRow,
  SourceSummaryRow,
} from './sourceRecognitionStatisticsApi'
import { BranchSourceRecognitionStatistics, SourceRecognitionStatistics } from './sourceStatisticsPages'

const sourceApi = vi.hoisted(() => ({
  querySourceSummaryPage: vi.fn(),
  querySourceDetailsPage: vi.fn(),
  exportSourceStatistics: vi.fn(),
}))

vi.mock('./sourceRecognitionStatisticsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./sourceRecognitionStatisticsApi')>()
  return { ...actual, ...sourceApi }
})

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

const clientRef = vi.hoisted(() => ({ current: { marker: 'source-page-client' } }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

// ========== 视图模型夹具（适配层出口的页面分页模型行） ==========

const sourceSummaryRow: SourceSummaryRow = {
  groupDimension: 1,
  sourceOrganizationCode: 'ORG-B',
  sourceOrganizationName: '组织乙',
  sourceHospitalCode: 'HOS-2',
  sourceHospitalName: '医院乙',
  sourceBranchCode: null,
  sourceBranchName: null,
  itemType: null,
  itemTypeText: '未知类型',
  standardProjectCode: null,
  standardProjectName: null,
  categoryName: null,
  groupName: null,
  recognitionCount: 5,
}

const sourceDetailRow: SourceDetailRow = {
  recognitionMatchRecordId: 'RECORD-1',
  recognitionMatchItemId: 'ITEM-1',
  source: { organizationCode: 'ORG-A', organizationName: '组织甲', hospitalCode: 'HOS-1', hospitalName: '医院甲', branchCode: 'BRA-1', branchName: '院区一' },
  receiver: { organizationCode: 'ORG-B', organizationName: '组织乙', hospitalCode: 'HOS-2', hospitalName: '医院乙', branchCode: '', branchName: '' },
  standardProjectCode: 'A01',
  recognitionDeptId: null,
  recognitionDeptName: null,
  recognitionDoctorId: null,
  recognitionDoctorName: null,
  recognitionTime: new Date('2026-02-11T09:00:00'),
  patientName: '张三',
  identityDocumentNo: '110101Y001',
}

const pageOf = <Row,>(items: Row[]) => ({ items, pageIndex: 1, pageSize: 10, totalCount: items.length })

beforeEach(() => {
  vi.clearAllMocks()
  trustedScopeRef.organizationReady = true
  trustedScopeRef.hospitalReady = true
  sourceApi.querySourceSummaryPage.mockImplementation(async () => pageOf([sourceSummaryRow]))
  sourceApi.querySourceDetailsPage.mockImplementation(async () => pageOf([sourceDetailRow]))
  sourceApi.exportSourceStatistics.mockImplementation(async () => {})
})

function renderPage(component: React.ReactElement) {
  return render(<ConfigProvider locale={zhCN}>{component}</ConfigProvider>)
}

describe('来源医院被互认统计（平台页）', () => {
  it('进入自动加载：两路查询以占位 client 与平台版 built 查询发起；导出沿用条件且导出类型为来源侧汇总（C22）', async () => {
    renderPage(<SourceRecognitionStatistics />)

    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenCalledTimes(1))
    expect(sourceApi.querySourceDetailsPage).toHaveBeenCalledTimes(1)

    const [, summaryBuilt] = sourceApi.querySourceSummaryPage.mock.calls[0]
    expect(summaryBuilt).toMatchObject({ version: 'platform', groupDimension: 1, pageIndex: 1, pageSize: 10 })
    expect(String(summaryBuilt.startTime)).toMatch(/^\d{4}-\d{2}-\d{2}$/)

    fireEvent.click(screen.getByRole('button', { name: '导出汇总' }))
    await waitFor(() => expect(sourceApi.exportSourceStatistics).toHaveBeenCalledTimes(1))
    const [exportClient, exportQuery, exportName] = sourceApi.exportSourceStatistics.mock.calls[0]
    expect(exportClient).toBe(clientRef.current)
    expect(exportQuery).toMatchObject({ version: 'platform', exportType: 6, groupDimension: 1 })
    expect(exportQuery).not.toHaveProperty('pageIndex')
    expect(exportName).toMatch(/^来源医院被互认汇总-\d{8}-\d{8}\.xlsx$/)
  })

  it('来源组与接收组范围随查询提交（C10 同构面）', async () => {
    renderPage(<SourceRecognitionStatistics />)
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenCalled())

    const sourceGroup = within(screen.getByRole('group', { name: '来源组范围' }))
    fireEvent.change(sourceGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS-2' } })
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'platform', sourceHospitalCode: 'HOS-2' }),
    ))

    const receiverGroup = within(screen.getByRole('group', { name: '接收组范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '组织' }), { target: { value: 'ORG-B' } })
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'platform', receiverOrganizationCode: 'ORG-B' }),
    ))
  })

  it('明细导出为来源医院被互认明细（C15、C22）', async () => {
    renderPage(<SourceRecognitionStatistics />)
    await waitFor(() => expect(sourceApi.querySourceDetailsPage).toHaveBeenCalled())

    fireEvent.click(screen.getByRole('button', { name: '导出明细' }))
    await waitFor(() => expect(sourceApi.exportSourceStatistics).toHaveBeenCalledTimes(1))
    const [, exportQuery, exportName] = sourceApi.exportSourceStatistics.mock.calls[0]
    expect(exportQuery).toMatchObject({ version: 'platform', exportType: 7 })
    expect(exportName).toMatch(/^来源医院被互认明细-\d{8}-\d{8}\.xlsx$/)
  })
})

describe('本院被互认统计（本院页）', () => {
  it('来源范围固定；built 查询为本院版且不含来源组织、来源医院与接收组织（C21）', async () => {
    renderPage(<BranchSourceRecognitionStatistics />)

    expect(await screen.findByText(/可信范围（只读）：组织 ORG-A，医院 HOS-1/)).toBeInTheDocument()
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenCalled())

    const built = sourceApi.querySourceSummaryPage.mock.calls[0][1]
    expect(built).toMatchObject({ version: 'branch' })
    expect(built).not.toHaveProperty('sourceOrganizationCode')
    expect(built).not.toHaveProperty('sourceHospitalCode')
    expect(built).not.toHaveProperty('receiverOrganizationCode')
    // 来源院区不选择即全院：院区条件未提供（请求不提交该层过滤）。
    expect((built as { sourceBranchCode?: string }).sourceBranchCode).toBeUndefined()
  })

  it('本院来源院区与接收组医院选择随查询提交，组织字段仍不提交（C21）', async () => {
    renderPage(<BranchSourceRecognitionStatistics />)
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenCalled())

    const ownGroup = within(screen.getByRole('group', { name: '本院院区' }))
    expect(ownGroup.getAllByRole('combobox')).toHaveLength(1)
    fireEvent.change(ownGroup.getByRole('combobox', { name: '院区' }), { target: { value: 'BRA-2' } })
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'branch', sourceBranchCode: 'BRA-2' }),
    ))

    const receiverGroup = within(screen.getByRole('group', { name: '接收医院范围' }))
    fireEvent.change(receiverGroup.getByRole('combobox', { name: '医院' }), { target: { value: 'HOS-2' } })
    await waitFor(() => expect(sourceApi.querySourceSummaryPage).toHaveBeenLastCalledWith(
      clientRef.current,
      expect.objectContaining({ version: 'branch', receiverHospitalCode: 'HOS-2' }),
    ))

    const lastBuilt = sourceApi.querySourceSummaryPage.mock.calls.at(-1)?.[1] as Record<string, unknown>
    expect(lastBuilt).not.toHaveProperty('sourceOrganizationCode')
    expect(lastBuilt).not.toHaveProperty('sourceHospitalCode')
    expect(lastBuilt).not.toHaveProperty('receiverOrganizationCode')
  })

  it('可信上下文缺失时整页阻断：不发请求、不渲染范围选择器（C21）', async () => {
    trustedScopeRef.organizationReady = false
    trustedScopeRef.hospitalReady = false
    renderPage(<BranchSourceRecognitionStatistics />)

    expect(await screen.findByRole('heading', { name: '本院被互认统计' })).toBeInTheDocument()
    expect(screen.getByText('可信范围不可用')).toBeInTheDocument()
    expect(screen.queryByRole('combobox')).toBeNull()
    expect(sourceApi.querySourceSummaryPage).not.toHaveBeenCalled()
    expect(sourceApi.querySourceDetailsPage).not.toHaveBeenCalled()
  })
})
