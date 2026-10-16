/**
 * T02「报告详情」组件层用例（测试矩阵 C02、C03、C04、C05、C06、C07、C08、C13、C14 与 D-04 的组件面，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md）。
 *
 * 装配边界（沿用报告页既有测试口径，Frontend Testing 第 2 节「mock 位于 Client/适配层边界」）：
 * 1. 直接挂载 `ReportDetailPage`，三个 I/O 出口（版本列表、版本详情、PDF 下载）以受控替身经 props 注入；
 * 2. 版本行与版本详情夹具经真实适配层出口（`toReportVersionRows`、`toReportVersionDetail`）归一，
 *    替身保持适配层出口的真实形状，页面拿到的对象与真实链路一致；
 * 3. 滚动容器以与 MainLayout Content 同类名的包裹层替身承载，scrollTop 受控读写。
 *
 * 仍属 Host 层或浏览器层、不在本文件覆盖：宿主统一错误提示、真实下载链路、真实浏览器布局
 * （备注溢出按滚动高度测量、Tooltip 定位属布局能力，组件层断言行为面与配套 CSS 静态证据）。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { MemoryRouter, useLocation, useNavigationType } from 'react-router-dom'
import { useEffect, type ReactNode } from 'react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  toReportRows,
  toReportVersionDetail,
  toReportVersionRows,
  type ExaminationReportContentViewReadModel,
  type LaboratoryReportContentViewReadModel,
  type ReportRow,
  type ReportVersionCommonReadModel,
  type ReportVersionDetail,
  type ReportVersionRow,
} from './reportsApi'
import { ReportDetailPage } from './ReportDetailPage'
import { IdNumberText } from './IdNumberText'
import { clearRowSummaries, setRowSummary } from './reportRowSummaryCache'

/** 三个 I/O 出口的受控替身：与入口容器注入的适配层出口同形。 */
const adapters = {
  queryVersions: vi.fn<(reportId: string) => Promise<ReportVersionRow[]>>(),
  queryDetail: vi.fn<(reportId: string, reportVersionId: string) => Promise<ReportVersionDetail>>(),
  downloadVersion: vi.fn<(reportId: string, reportVersionId: string, fileName: string) => Promise<void>>(),
}

/** 版本行夹具：V1 已被替代（默认形态），由真实适配层出口归一。 */
const versionRow = (overrides: Record<string, unknown> = {}): ReportVersionRow => toReportVersionRows([{
  reportVersionId: 'VER-1',
  versionSequence: 1,
  sourceModifiedTime: new Date(2026, 8, 19, 9, 0),
  platformReceivedTime: new Date(2026, 8, 19, 9, 1),
  reportDoctorName: '报告医生甲',
  reviewDoctorName: '审核医生甲',
  sourceReportRemark: '来源备注甲',
  pdfFileName: 'R-1-v1.pdf',
  isCurrentVersion: false,
  isSuperseded: true,
  reportStatus: 1,
  reportStatusText: '有效',
  ...overrides,
}])[0]

/** 当前有效版本 V2 的版本行。 */
const currentVersionRow = () => versionRow({
  reportVersionId: 'VER-2',
  versionSequence: 2,
  sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  sourceReportRemark: '来源备注乙',
  pdfFileName: 'R-1-v2.pdf',
  isCurrentVersion: true,
  isSuperseded: false,
})

/** 默认版本清单：V1 已被替代 + V2 当前有效。 */
const twoVersions = (): ReportVersionRow[] => [versionRow(), currentVersionRow()]

/** 版本详情夹具：默认与当前有效版本 V2 同形，由真实适配层出口归一。 */
const versionDetail = (overrides: Record<string, unknown> = {}): ReportVersionDetail => toReportVersionDetail({
  reportType: 1,
  reportTypeText: '检验报告',
  reportNo: 'R-1',
  reportVersionId: 'VER-2',
  versionSequence: 2,
  sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
  platformReceivedTime: new Date(2026, 8, 20, 9, 6),
  reportDoctorName: '报告医生乙',
  reviewDoctorName: '审核医生乙',
  sourceReportRemark: '来源备注乙',
  content: { common: null, laboratoryContent: null, examinationContent: null },
  file: { fileName: 'R-1-v2.pdf' },
  ...overrides,
})

/** V1 的版本详情夹具（切换历史版本时使用）。 */
const versionOneDetail = (overrides: Record<string, unknown> = {}): ReportVersionDetail => versionDetail({
  reportVersionId: 'VER-1',
  versionSequence: 1,
  sourceModifiedTime: new Date(2026, 8, 19, 9, 0),
  platformReceivedTime: new Date(2026, 8, 19, 9, 1),
  reportDoctorName: '报告医生甲',
  reviewDoctorName: '审核医生甲',
  sourceReportRemark: '来源备注甲',
  file: { fileName: 'R-1-v1.pdf' },
  ...overrides,
})

/** 行概要夹具：列表「查看详情」跳转经来源行概要缓存交付的行概要，经真实适配层出口归一。 */
const summaryRow = (overrides: Record<string, unknown> = {}): ReportRow => toReportRows([{
  reportId: 'REP-1',
  reportNo: 'R-1',
  reportType: 1,
  reportTypeText: '检验报告',
  reportTime: new Date(2026, 8, 20, 10, 30),
  organizationName: '组织一',
  hospitalName: '医院一',
  branchName: '院区一',
  patientName: '张三',
  identityDocumentNo: '110101199001011234',
  currentVersionSequence: 3,
  status: 1,
  statusText: '有效',
  ...overrides,
}])[0]

/**
 * 公共信息读模型夹具：24 字段齐全的形态，与版本详情经 `content.common` 交付的生成端读模型同形。
 */
const commonReadModel = (overrides: Record<string, unknown> = {}): ReportVersionCommonReadModel => ({
  reportDeptName: '检验科',
  reportDeptId: 'DEP-1',
  reportDoctorName: '报告医生甲',
  reportDoctorId: 'DOC-1',
  reviewDoctorName: '审核医生甲',
  reviewDoctorId: 'DOC-2',
  reviewTime: new Date(2026, 8, 20, 11, 0),
  reportTime: new Date(2026, 8, 20, 10, 30),
  executionDeptName: '执行科室甲',
  executionDeptId: 'DEP-2',
  sourceConfidentialFlag: '否',
  patientName: '张三',
  patientGenderCode: 'M',
  patientBirthDate: new Date(1980, 0, 1),
  ageAtReport: '46岁',
  identityDocumentTypeCode: '01',
  identityDocumentNo: '110101199001011234',
  patientPhoneNumber: '13800000000',
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'V-0001',
  inpatientNo: 'ZY-0001',
  wardName: '病区甲',
  roomName: '病房甲',
  bedNo: '床号12',
  sourceReportName: '来源报告甲',
  applicationDeptName: '申请科室甲',
  applicationDeptId: 'DEP-3',
  applicationDoctorName: '申请医生甲',
  applicationDoctorId: 'DOC-3',
  applicationTime: new Date(2026, 8, 19, 15, 0),
  ...overrides,
})

/** 手动控制完成时机的 Promise，用于断言在途与失败这类时序敏感行为。 */
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

/** 路由观测点：受控观测真实 Router 交付的路径、查询参数与导航方式（断言 URL 行为，不刺探路由内部）。 */
const locationRef = { current: { pathname: '', search: '', navigationType: 'POP' } }

function LocationProbe() {
  const location = useLocation()
  const navigationType = useNavigationType()
  useEffect(() => {
    locationRef.current = { pathname: location.pathname, search: location.search, navigationType }
  }, [location, navigationType])
  return null
}

/** 入口容器的条件渲染替身（D-01）：仅在详情子路由激活时挂载详情页，返回列表路由即卸载（与入口容器同口径）。 */
function DetailRouteBoundary({ children }: { children: ReactNode }) {
  const detailActive = useLocation().pathname.startsWith('/report-management/detail/')
  return <>{detailActive ? children : null}</>
}

function renderDetail(initialPath = '/report-management/detail/REP-1', row?: ReportRow) {
  // 来源行概要缓存：与列表「查看详情」跳转写入的会话级缓存同通道（键为路径 reportId）；
  // 缺省不写入即缓存未命中，按深链直开口径呈现。
  if (row !== undefined) setRowSummary('REP-1', row)
  return render(
    // 滚动容器替身：与应用 MainLayout 的 Content 同类名（overflow:auto 的整页滚动承载）。
    <div className='ant-layout-content'>
      <ConfigProvider locale={zhCN}>
        <MemoryRouter initialEntries={[initialPath]}>
          <LocationProbe />
          <DetailRouteBoundary>
            <ReportDetailPage
              reportId='REP-1'
              parentPath='/report-management'
              parentName='报告管理'
              queryVersions={adapters.queryVersions}
              queryDetail={adapters.queryDetail}
              downloadVersion={adapters.downloadVersion}
            />
          </DetailRouteBoundary>
        </MemoryRouter>
      </ConfigProvider>
    </div>,
  )
}

/** 滚动容器替身的受控 scrollTop 读写。 */
function controlScroll() {
  const container = document.querySelector('.ant-layout-content') as HTMLElement
  let stored = 0
  Object.defineProperty(container, 'scrollTop', { get: () => stored, set: (value: number) => { stored = value }, configurable: true })
  return container
}

/** Segmented 选项的可访问名称携带版本事实与可见标签：rc-segmented 的包裹 label 同时含两者，
 *  选项定位按事实文本的正则匹配（真实读屏只播报事实文本，可见标签已 aria-hidden）。 */
const versionOption = (fact: string) => screen.getByLabelText(new RegExp(`^${fact}`))

/** 版本信息卡范围：历史版本时间线也会呈现各版本的事实文本，元信息条断言一律圈定在该卡内。 */
const metaCard = () => screen.getByText('版本信息').closest('.ant-card') as HTMLElement

beforeEach(() => {
  adapters.queryVersions.mockReset()
  adapters.queryDetail.mockReset()
  adapters.downloadVersion.mockReset()
  adapters.queryVersions.mockResolvedValue(twoVersions())
  adapters.queryDetail.mockImplementation(async (_reportId: string, reportVersionId: string) =>
    reportVersionId === 'VER-1' ? versionOneDetail() : versionDetail())
  adapters.downloadVersion.mockResolvedValue(undefined)
  locationRef.current = { pathname: '', search: '', navigationType: 'POP' }
  // 来源行概要缓存是模块级状态：每个用例从空缓存开始（深链口径依赖缓存未命中）。
  clearRowSummaries()
})

describe('T02 版本切换控件三形态与默认选中（C03）', () => {
  it('2～6 版渲染 Segmented：默认选中当前有效版本并读取其详情，选项可访问名称携带版本事实', async () => {
    renderDetail()

    const current = await screen.findByLabelText(new RegExp('^第 2 版（当前有效版本）'))
    await waitFor(() => expect(current).toBeChecked())
    expect(versionOption('第 1 版（历史版本）')).not.toBeChecked()
    // 默认读取当前有效版本的详情。
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-2'))
    // 当前有效版本选项带圆点标记；历史版本选项没有。
    expect(current.closest('label')?.querySelector('.report-management-detail-version-dot')).not.toBeNull()
    expect(versionOption('第 1 版（历史版本）').closest('label')?.querySelector('.report-management-detail-version-dot')).toBeNull()
    // 2～6 版不渲染 Select，也不渲染静态 Tag。
    expect(screen.queryByRole('combobox', { name: '版本切换' })).toBeNull()
    expect(screen.queryByText('V2 · 当前版本')).toBeNull()
  })

  it('无当前有效版本时默认选中最大序号版本并读取其详情', async () => {
    adapters.queryVersions.mockResolvedValue([
      versionRow(),
      versionRow({ reportVersionId: 'VER-3', versionSequence: 3, pdfFileName: 'R-1-v3.pdf' }),
    ])
    renderDetail()

    await waitFor(() => expect(versionOption('第 3 版（历史版本）')).toBeChecked())
    expect(versionOption('第 1 版（历史版本）')).not.toBeChecked()
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-3'))
  })

  it('超过 6 版退化为 Select：选项文本携带版本事实，默认选中当前有效版本', async () => {
    adapters.queryVersions.mockResolvedValue(Array.from({ length: 7 }, (_, index) => {
      const sequence = index + 1
      return versionRow({
        reportVersionId: `VER-${sequence}`,
        versionSequence: sequence,
        isCurrentVersion: sequence === 4,
        isSuperseded: sequence !== 4,
        pdfFileName: `R-1-v${sequence}.pdf`,
      })
    }))
    renderDetail()

    const select = await screen.findByRole('combobox', { name: '版本切换' })
    expect(select).toBeInTheDocument()
    expect(screen.queryByRole('radiogroup', { name: '版本切换' })).toBeNull()
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-4'))
    // 默认选中当前有效版本 V4：选择器回显选中值文本（antd 6 的选中值承载元素由容器文本断言）。
    const selectRoot = document.querySelector('.report-management-detail-version-select')
    expect(selectRoot).not.toBeNull()
    expect(within(selectRoot as HTMLElement).getByText('V4 · 当前有效')).toBeInTheDocument()

    // 展开下拉：选项文本携带版本事实。
    fireEvent.mouseDown(selectRoot as HTMLElement)
    expect(await screen.findByText('V1 · 已被替代')).toBeInTheDocument()
    expect(screen.getByText('V3 · 已被替代')).toBeInTheDocument()
    // 选择器回显与下拉选项并存同一选中值文本，按集合断言。
    expect(screen.getAllByText('V4 · 当前有效').length).toBeGreaterThan(0)
  })

  it('单版本不渲染切换控件：渲染静态 Tag「V1 · 当前版本」并读取该版本详情', async () => {
    adapters.queryVersions.mockResolvedValue([versionRow({ isCurrentVersion: true, isSuperseded: false })])
    renderDetail()

    expect(await screen.findByText('V1 · 当前版本')).toBeInTheDocument()
    expect(screen.queryByRole('radiogroup', { name: '版本切换' })).toBeNull()
    expect(screen.queryByRole('combobox', { name: '版本切换' })).toBeNull()
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-1'))
    expect(await screen.findByText('报告医生甲')).toBeInTheDocument()
  })

  it('单版本作废报告：静态 Tag 呈现「V1 · 已作废」中性灰，不与元信息条「报告已作废」矛盾', async () => {
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      reportStatus: 2,
      reportStatusText: '已作废',
    })])
    renderDetail()

    // 静态 Tag 按版本状态分发：作废版本不再宣称「当前版本」，配色与元信息条同用中性灰。
    expect(await screen.findByText('V1 · 已作废')).toHaveClass('ant-tag-default')
    expect(screen.queryByText('V1 · 当前版本')).toBeNull()
    expect(await screen.findByText('报告已作废')).toBeInTheDocument()
  })
})

describe('T02 主标题可访问名称（焦点处读屏播报）', () => {
  it('焦点所在主标题 h1 的可访问名称携带页面名「报告详情」：深链单号占位时播报页面名', async () => {
    renderDetail()
    expect(await screen.findByRole('heading', { name: '报告详情 —' })).toBeInTheDocument()
  })

  it('行概要在位时主标题可访问名称播报页面名加单号', async () => {
    renderDetail('/report-management/detail/REP-1', summaryRow())
    expect(await screen.findByRole('heading', { name: '报告详情 R-1' })).toBeInTheDocument()
  })
})

describe('T02 版本切换读取与 ?v= 写回解析（C04）', () => {
  it('深链 ?v= 命中版本序号时选中该版本并读取其详情', async () => {
    renderDetail('/report-management/detail/REP-1?v=1')

    await waitFor(() => expect(versionOption('第 1 版（历史版本）')).toBeChecked())
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-1'))
    expect(await screen.findByText('报告医生甲')).toBeInTheDocument()
  })

  it('深链 ?v= 无匹配时回退当前有效版本', async () => {
    renderDetail('/report-management/detail/REP-1?v=99')

    await waitFor(() => expect(versionOption('第 2 版（当前有效版本）')).toBeChecked())
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-2'))
  })

  it.each([
    ['非数字文本', '?v=abc'],
    ['小数序号', '?v=1.5'],
    ['零序号', '?v=0'],
  ])('深链 ?v= 非法形态（%s）：无匹配回退当前有效版本且不改写 URL', async (_label, search) => {
    renderDetail(`/report-management/detail/REP-1${search}`)

    // 非法形态不匹配任何版本序号：选中与详情读取都回退当前有效版本。
    await waitFor(() => expect(versionOption('第 2 版（当前有效版本）')).toBeChecked())
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-2'))
    // 初始解析不触发 URL 写回：查询参数保持原样。
    expect(locationRef.current.search).toBe(search)
    expect(locationRef.current.navigationType).toBe('POP')
  })

  it('点击历史版本：读取该版本详情、URL 以 replace 写回 ?v= 序号、播报版本已加载', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    fireEvent.click(versionOption('第 1 版（历史版本）'))
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-1'))
    expect(await screen.findByText('报告医生甲')).toBeInTheDocument()
    await waitFor(() => expect(locationRef.current.search).toBe('?v=1'))
    expect(locationRef.current.navigationType).toBe('REPLACE')
    expect(screen.getByRole('status')).toHaveTextContent('第 1 版详情已加载')
  })

  it('切换完成后内容滚动容器回到顶部', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')
    const container = controlScroll()
    container.scrollTop = 320

    fireEvent.click(versionOption('第 1 版（历史版本）'))
    await screen.findByText('报告医生甲')

    expect(container.scrollTop).toBe(0)
  })

  it('切换版本在途时返回列表：卸载后迟到的版本详情响应不得把路由拉回详情页', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const pending = deferred<ReportVersionDetail>()
    adapters.queryDetail.mockReturnValueOnce(pending.promise)
    fireEvent.click(versionOption('第 1 版（历史版本）'))
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledTimes(2))

    // 返回列表：详情页随子路由退出而卸载（入口容器条件渲染的同一行为）。
    fireEvent.click(screen.getByLabelText('返回报告列表'))
    await waitFor(() => expect(locationRef.current.pathname).toBe('/report-management'))
    expect(screen.queryByLabelText('返回报告列表')).toBeNull()

    // 切换的响应晚到：不得执行 URL 写回与滚动清零，路由停留在列表。
    await act(async () => { pending.resolve(versionOneDetail()) })
    expect(locationRef.current.pathname).toBe('/report-management')
  })
})

describe('T02 切换在途压暗与播报（C13 组件面）', () => {
  it('读取在途时旧内容压暗并播报加载中，完成后解除压暗并播报版本已加载', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const pending = deferred<ReportVersionDetail>()
    adapters.queryDetail.mockReturnValueOnce(pending.promise)
    fireEvent.click(versionOption('第 1 版（历史版本）'))

    // 在途：压暗类挂载、Spin 文案在位（可见层）、加载播报（读屏层）。
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledTimes(2))
    expect(document.querySelector('.report-management-detail-versioned-cards'))
      .toHaveClass('report-management-detail-versioned-cards-loading')
    const overlay = document.querySelector('.report-management-detail-loading-overlay')
    expect(overlay).not.toBeNull()
    expect(overlay).toHaveTextContent('正在加载版本详情…')
    expect(screen.getByRole('status')).toHaveTextContent('正在加载版本详情…')
    // 旧内容原样保留（切换到 V1 完成前仍显示 V2 的元信息）。
    expect(screen.getByText('报告医生乙')).toBeInTheDocument()

    await act(async () => { pending.resolve(versionOneDetail()) })

    expect(await screen.findByText('报告医生甲')).toBeInTheDocument()
    expect(document.querySelector('.report-management-detail-versioned-cards'))
      .not.toHaveClass('report-management-detail-versioned-cards-loading')
    expect(screen.queryByText('正在加载版本详情…')).toBeNull()
    expect(screen.getByRole('status')).toHaveTextContent('第 1 版详情已加载')
  })

  it('快速连点只采纳最后一次：过期响应被丢弃，选中与内容停留在最新版本', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const first = deferred<ReportVersionDetail>()
    const second = deferred<ReportVersionDetail>()
    adapters.queryDetail.mockReturnValueOnce(first.promise)
    fireEvent.click(versionOption('第 1 版（历史版本）'))
    adapters.queryDetail.mockReturnValueOnce(second.promise)
    fireEvent.click(versionOption('第 2 版（当前有效版本）'))

    await act(async () => { second.resolve(versionDetail()) })
    expect(await screen.findByText('报告医生乙')).toBeInTheDocument()
    await act(async () => { first.resolve(versionOneDetail()) })

    // 迟到的 V1 响应被丢弃：内容、选中与 URL 都停留在 V2。
    expect(screen.getByText('报告医生乙')).toBeInTheDocument()
    expect(screen.queryByText('报告医生甲')).toBeNull()
    expect(versionOption('第 2 版（当前有效版本）')).toBeChecked()
    await waitFor(() => expect(locationRef.current.search).toBe('?v=2'))
  })
})

describe('T02 版本详情读取失败静默回退（D-04）', () => {
  it('切换读取失败：选中态回退上一成功版本、内容与选中一致、无横幅、URL 不写回', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const failing = deferred<ReportVersionDetail>()
    adapters.queryDetail.mockReturnValueOnce(failing.promise)
    fireEvent.click(versionOption('第 1 版（历史版本）'))
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-1'))
    await act(async () => { failing.reject(new Error('版本详情读取失败')) })

    // 选中态静默回退到上一成功版本 V2，内容与选中一致。
    await waitFor(() => expect(versionOption('第 2 版（当前有效版本）')).toBeChecked())
    expect(screen.getByText('报告医生乙')).toBeInTheDocument()
    // 不渲染横幅、不播报错误细节（错误由宿主统一提示）。
    expect(screen.queryByText(/读取失败|加载失败/)).toBeNull()
    expect(screen.getByRole('status')).toHaveTextContent('第 2 版详情已加载')
    // 失败不写回 URL。
    expect(locationRef.current.search).toBe('')

    // 回退后再次切换仍可成功：读取、写回与内容更新恢复正常。
    const retry = deferred<ReportVersionDetail>()
    adapters.queryDetail.mockReturnValueOnce(retry.promise)
    fireEvent.click(versionOption('第 1 版（历史版本）'))
    await act(async () => { retry.resolve(versionOneDetail()) })
    expect(await screen.findByText('报告医生甲')).toBeInTheDocument()
    await waitFor(() => expect(locationRef.current.search).toBe('?v=1'))
  })

  it('首次详情读取失败：无上一成功版本可回退，保留选中与空内容，不崩溃也不报错', async () => {
    adapters.queryDetail.mockRejectedValueOnce(new Error('首次读取失败'))
    renderDetail()

    await waitFor(() => expect(versionOption('第 2 版（当前有效版本）')).toBeChecked())
    // 选中保留、无错误横幅、无完成播报（没有成功过的版本）。
    expect(screen.queryByText(/读取失败|加载失败/)).toBeNull()
    expect(screen.queryByRole('status')).toBeNull()
  })
})

describe('T02 版本列表读取失败静默保留（C14 组件面）', () => {
  it('版本列表读取失败：无横幅无红色错误态，空态保留、返回列表入口在位，可刷新或经列表重进', async () => {
    adapters.queryVersions.mockRejectedValueOnce(new Error('版本列表读取失败'))
    renderDetail()

    // 失败相位稳定：加载播报与骨架退场，版本切换控件位与版本内容不再呈现（保留空态）。
    await waitFor(() => expect(screen.queryByRole('status')).toBeNull())
    expect(document.querySelector('.ant-skeleton')).toBeNull()
    expect(document.querySelector('.report-management-detail-version-segmented')).toBeNull()
    expect(document.querySelector('.report-management-detail-meta')).toBeNull()
    expect(document.querySelector('.report-management-detail-timeline-node')).toBeNull()
    // 静默口径（根 design.md 6.9「版本列表读取失败」行）：不渲染横幅、不出现红色，错误由宿主统一提示。
    expect(screen.queryByRole('alert')).toBeNull()
    expect(document.querySelector('.ant-alert-error')).toBeNull()
    expect(screen.queryByText(/读取失败|加载失败/)).toBeNull()
    // 重进路径保留：返回按钮与面包屑父级按钮在位（刷新或经列表重进）。
    expect(screen.getByLabelText('返回报告列表')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '报告管理' })).toBeInTheDocument()
  })
})

describe('T02 版本信息卡（元信息条与备注展开）', () => {
  it('元信息字段齐全：版本状态 Tag、两个时间、两医生、PDF 文件名与下载入口', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const meta = document.querySelector('.report-management-detail-meta')
    expect(meta).not.toBeNull()
    expect(meta).toHaveTextContent('当前有效版本')
    expect(meta).toHaveTextContent('2026-09-20 09:05')
    expect(meta).toHaveTextContent('2026-09-20 09:06')
    expect(within(meta as HTMLElement).getByText('报告医生乙')).toBeInTheDocument()
    expect(within(meta as HTMLElement).getByText('审核医生乙')).toBeInTheDocument()
    expect(within(meta as HTMLElement).getByText('R-1-v2.pdf')).toBeInTheDocument()
    // 时间线节点同样持有每版下载入口；此处的下载入口断言圈定在版本信息卡内（按中文 aria-label 定位）。
    expect(within(meta as HTMLElement).getByLabelText('下载第 2 版 PDF')).toBeInTheDocument()
  })

  it('切换到历史版本后元信息卡呈现「已被后续版本替代」状态', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    fireEvent.click(versionOption('第 1 版（历史版本）'))
    const meta = await waitFor(() => {
      const node = document.querySelector('.report-management-detail-meta')
      expect(node).not.toBeNull()
      expect(node).toHaveTextContent('报告医生甲')
      return node as HTMLElement
    })
    expect(meta).toHaveTextContent('已被后续版本替代')
    expect(meta).toHaveTextContent('2026-09-19 09:00')
    expect(meta).toHaveTextContent('R-1-v1.pdf')
    expect(within(meta).getByLabelText('下载第 1 版 PDF')).toBeInTheDocument()
  })

  it('作废报告：版本状态 Tag 为「报告已作废」，优先于当前有效表述', async () => {
    adapters.queryVersions.mockResolvedValue([
      versionRow({ isCurrentVersion: true, isSuperseded: false, reportStatus: 2, reportStatusText: '已作废' }),
    ])
    adapters.queryDetail.mockImplementation(async () => versionOneDetail({ reportNo: 'R-1' }))
    renderDetail()

    expect(await screen.findByText('报告已作废')).toBeInTheDocument()
  })

  it('人员与时间字段缺失时显示「—」', async () => {
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      reportDoctorName: null,
      reviewDoctorName: null,
      sourceModifiedTime: null,
      platformReceivedTime: null,
    })])
    adapters.queryDetail.mockImplementation(async () => versionOneDetail())
    renderDetail()

    await screen.findByText('V1 · 当前版本')
    const meta = await waitFor(() => {
      const node = document.querySelector('.report-management-detail-meta')
      expect(node).not.toBeNull()
      return node as HTMLElement
    })
    expect(within(meta).getAllByText('—').length).toBeGreaterThanOrEqual(4)
  })

  it('备注展开入口切换 aria-expanded 与截断类名', async () => {
    const longRemark = '备注内容'.repeat(100)
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      sourceReportRemark: longRemark,
    })])
    adapters.queryDetail.mockImplementation(async () => versionOneDetail({ sourceReportRemark: longRemark }))
    renderDetail()

    const expand = await screen.findByRole('button', { name: '展开' })
    expect(expand).toHaveAttribute('aria-expanded', 'false')
    // 收起态挂两行截断类（截断视觉由配套 CSS 承载）。
    expect(document.querySelector('.report-management-detail-remark')).not.toBeNull()

    fireEvent.click(expand)
    expect(screen.getByRole('button', { name: '收起' })).toHaveAttribute('aria-expanded', 'true')
    expect(document.querySelector('.report-management-detail-remark-expanded')).not.toBeNull()

    fireEvent.click(screen.getByRole('button', { name: '收起' }))
    expect(screen.getByRole('button', { name: '展开' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('PDF 文件名未截断：不挂 Tooltip，悬停不出现浮层', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    // 时间线节点同样呈现文件名，悬停目标圈定在版本信息卡内。
    fireEvent.mouseEnter(within(metaCard()).getByText('R-1-v2.pdf'))
    // 未发生中段截断即无隐藏信息：等满一个浮层挂载窗口（findBy 的等待上限）后仍不出现 Tooltip。
    await expect(screen.findByRole('tooltip')).rejects.toThrow(/Unable to find/)
  })

  it('PDF 文件名超长时中段截断：保留头部与扩展名，Tooltip 呈现完整文件名', async () => {
    const longName = 'very-long-report-filename-20260920-final.pdf'
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      pdfFileName: longName,
    })])
    adapters.queryDetail.mockImplementation(async () => versionOneDetail())
    renderDetail()

    // 静态文本为中段截断形态（头部 + … + 扩展名），完整文件名不作为静态文本出现；
    // 时间线节点同样呈现截断名，断言圈定在版本信息卡内。
    await waitFor(() => expect(within(metaCard()).getByText('very-lon….pdf')).toBeInTheDocument())
    expect(screen.queryByText(longName)).toBeNull()
    // Tooltip 仍呈现完整文件名。
    fireEvent.mouseEnter(within(metaCard()).getByText('very-lon….pdf'))
    expect(await screen.findByRole('tooltip')).toHaveTextContent(longName)
  })

  it('扩展名过长时对扩展名截尾保留末段：截断输出恒短于原文', async () => {
    // 点号靠前且扩展名过长（头部 8 + 「…」 + 扩展名 17 会得到比原文更长的输出）。
    const longName = 'abcdefgh.abcdefghijklmnop'
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      pdfFileName: longName,
    })])
    adapters.queryDetail.mockImplementation(async () => versionOneDetail())
    renderDetail()

    // 扩展名超预算按末段截尾：头部 + 「…」 + 扩展名末段，输出不比原文更长。
    await waitFor(() => expect(within(metaCard()).getByText('abcdefgh…bcdefghijklmnop')).toBeInTheDocument())
    expect(screen.queryByText(longName)).toBeNull()
    // Tooltip 呈现完整文件名。
    fireEvent.mouseEnter(within(metaCard()).getByText('abcdefgh…bcdefghijklmnop'))
    expect(await screen.findByRole('tooltip')).toHaveTextContent(longName)
  })
})

describe('T02 详情内 PDF 下载（C14 组件面）', () => {
  it('点击下载：按报告与版本标识和保存名触发，在途文案与 aria-busy，成功后复位', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    // 时间线接线后每版有第二处下载入口；本用例操作元信息条入口（DOM 序次在前），在途互斥联动由时间线用例覆盖。
    // 按钮带中文 aria-label：按可访问名称属性定位（getByRole 的可访问名计算在 jsdom 下代价过高）。
    const downloadButton = () => screen.getAllByLabelText('下载第 2 版 PDF')[0]
    const pending = deferred<void>()
    adapters.downloadVersion.mockReturnValueOnce(pending.promise)
    fireEvent.click(downloadButton())
    expect(adapters.downloadVersion).toHaveBeenCalledWith('REP-1', 'VER-2', 'R-1-v2.pdf')

    const button = downloadButton()
    expect(button).toHaveClass('ant-btn-loading')
    expect(button).toHaveAttribute('aria-busy', 'true')
    expect(button.textContent).toContain('下载中…')
    // 在途互斥：再次点击不重复发起。
    fireEvent.click(button)
    expect(adapters.downloadVersion).toHaveBeenCalledTimes(1)

    await act(async () => { pending.resolve(undefined) })
    await waitFor(() => expect(downloadButton()).not.toHaveClass('ant-btn-loading'))
    expect(downloadButton()).not.toHaveAttribute('aria-busy')
    expect(downloadButton().textContent).toContain('下载 PDF')
  })

  it('下载失败复位为可重试：再次点击重新发起', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const downloadButton = () => screen.getAllByLabelText('下载第 2 版 PDF')[0]
    const failing = deferred<void>()
    adapters.downloadVersion.mockReturnValueOnce(failing.promise)
    fireEvent.click(downloadButton())
    await act(async () => { failing.reject(new Error('下载失败')) })

    const button = await waitFor(() => {
      const node = downloadButton()
      expect(node).not.toHaveClass('ant-btn-loading')
      expect(node).not.toHaveAttribute('aria-busy')
      return node
    })
    expect(button.textContent).toContain('下载 PDF')

    adapters.downloadVersion.mockResolvedValueOnce(undefined)
    fireEvent.click(button)
    await waitFor(() => expect(adapters.downloadVersion).toHaveBeenCalledTimes(2))
  })
})

describe('T02 详情视觉规则静态证据', () => {
  it('压暗过渡、备注两行截断与 PDF 文件名截断由配套 CSS 提供（jsdom 无布局）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    const dim = css.match(/\.report-management-detail-versioned-cards-loading\s*\{[^}]*\}/)?.[0] ?? ''
    expect(dim).toContain('opacity: 0.5')
    expect(dim).toContain('pointer-events: none')
    const cards = css.match(/\.report-management-detail-versioned-cards\s*\{[^}]*\}/)?.[0] ?? ''
    expect(cards).toContain('transition: opacity')
    // 动效尊重 reduced-motion（根 design.md 6.10）：过渡时长归零。
    expect(css).toMatch(/@media \(prefers-reduced-motion: reduce\)/)
    const remark = css.match(/\.report-management-detail-remark\s*\{[^}]*\}/)?.[0] ?? ''
    expect(remark).toContain('-webkit-line-clamp: 2')
    const pdfName = css.match(/\.report-management-detail-pdf-name\s*\{[^}]*\}/)?.[0] ?? ''
    expect(pdfName).toContain('max-width: 180px')
  })

  it('详情页两层结构与内容行动效由配套 CSS 提供：外壳整宽、内容行 max-width 居中粘性顶栏之外，动效只动 transform 与 opacity（jsdom 无动效）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    // 两层结构（根 design.md 5.2）：整宽粘性条——规则自身不携带 max-width（整宽由外壳交付），
    // 内容行另行 max-width 1080 水平居中；粘性条不再嵌在 max-width 容器内。
    const topbar = css.match(/\.report-management-detail-topbar\s*\{[^}]*\}/)?.[0] ?? ''
    expect(topbar).not.toBe('')
    expect(topbar).toContain('position: sticky')
    expect(topbar).not.toContain('max-width')
    const content = css.match(/\.report-management-detail-content\s*\{[^}]*\}/)?.[0] ?? ''
    expect(content).toContain('max-width: 1080px')
    expect(content).toContain('margin: 0 auto')
    // 页面进入动效（根 design.md 6.10）：起止帧齐全，动画属性只在 keyframes 内显式声明。
    const keyframes = css.match(/@keyframes report-management-detail-content-enter\s*\{[\s\S]*?\n\}/)?.[0] ?? ''
    expect(keyframes).toContain('from { opacity: 0; transform: translateY(8px); }')
    expect(keyframes).toContain('to { opacity: 1; transform: translateY(0); }')
    // 只动 transform 与 opacity：keyframes 块内不声明其他动画属性。
    expect(keyframes).not.toMatch(/(width|height|margin|padding|color|background|border|box-shadow)\s*:/)
    // 动画挂到详情内容行，200ms ease-out 曲线（与压暗过渡同一曲线形制），fill both 防首帧闪现。
    expect(content).toContain('animation: report-management-detail-content-enter 200ms cubic-bezier(0, 0, 0.2, 1) both')
    // 悬停与选中的底色过渡（根 design.md 6.10）：150ms ease-out。
    expect(css).toContain('transition: background-color 150ms ease-out')
    // reduced-motion 归零规则覆盖新动画（根 design.md 6.10）：与既有压暗过渡归零同块声明。
    const reduced = css.match(/@media \(prefers-reduced-motion: reduce\)\s*\{[\s\S]*?\n\}/)?.[0] ?? ''
    expect(reduced).toContain('.report-management-detail-content { animation: none; }')
    expect(reduced).toContain('transition: none')
  })

  it('详情页 DOM 两层结构：顶栏为外壳直接子级（整宽粘性），概要与版本相关卡经内容行容器呈现', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const page = document.querySelector('.report-management-detail-page') as HTMLElement
    const childClasses = Array.from(page.children).map((node) => node.className)
    expect(childClasses).toContain('report-management-detail-topbar')
    const content = Array.from(page.children).find((node) => node.classList.contains('report-management-detail-content')) as HTMLElement
    expect(content).not.toBeUndefined()
    expect(content.querySelector('.report-management-detail-summary-header')).not.toBeNull()
    expect(content.querySelector('.report-management-detail-versioned-cards')).not.toBeNull()
  })

  it('概要标题行与公共信息的排版规则由配套 CSS 提供：单号 16px/600/tabular-nums、容器查询两列、ID 次行小字（jsdom 无布局）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    // 概要头部第 1 行单号（根 design.md 6.1）：16px、字重 600、tabular-nums。
    const summaryNo = css.match(/\.report-management-detail-summary-title \.report-management-detail-summary-no\s*\{[^}]*\}/)?.[0] ?? ''
    expect(summaryNo).toContain('font-size: 16px')
    expect(summaryNo).toContain('font-weight: 600')
    expect(summaryNo).toContain('font-variant-numeric: tabular-nums')
    // 公共信息容器是容器查询的容器（Client 设计 §7：container-type: inline-size）。
    const publicWrap = css.match(/\.report-management-detail-public\s*\{[^}]*\}/)?.[0] ?? ''
    expect(publicWrap).toContain('container-type: inline-size')
    // 窄容器（<900px）每组降两列：容器查询块内以两列网格承载 Descriptions。
    const containerQuery = css.match(/@container \(max-width: 899\.9px\)\s*\{[\s\S]*?\n\}/)?.[0] ?? ''
    expect(containerQuery).toContain('grid-template-columns: repeat(2')
    // 「名称（ID）」对次行小字（根 design.md 6.6）：12px tertiary、tabular-nums。
    const codeLine = css.match(/\.report-management-detail-name-id-code\s*\{[^}]*\}/)?.[0] ?? ''
    expect(codeLine).toContain('font-size: 12px')
    expect(codeLine).toContain('var(--ant-color-text-tertiary')
    expect(codeLine).toContain('font-variant-numeric: tabular-nums')
  })
})

/**
 * T02 概要头部两行（测试矩阵 C02「详情概要头部渲染」，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md；根 design.md 6.1）。
 *
 * 概要头部由「概要」卡完整呈现：第 1 行单号（copyable）+ 类型/状态 Tag，第 2 行五字段；
 * 数据来自来源行概要缓存（列表「查看详情」写入），缓存未命中（深链直开）时字段一律「—」。
 */
describe('T02 概要头部两行（C02）', () => {
  it('行概要就绪：第 1 行单号可复制并带类型与状态 Tag，第 2 行五字段齐全', async () => {
    renderDetail('/report-management/detail/REP-1', summaryRow())
    await screen.findByText('报告医生乙')

    // 第 1 行（标题行）：单号文本 + copyable 复制按钮（zhCN 下可访问名称「复制」）。
    const title = document.querySelector('.report-management-detail-summary-title')
    expect(title).not.toBeNull()
    expect(within(title as HTMLElement).getAllByText('R-1').length).toBeGreaterThan(0)
    expect(within(title as HTMLElement).getByRole('button', { name: '复制' })).toBeInTheDocument()
    // 类型 Tag processing；状态 Tag 有效为 success。
    expect(within(title as HTMLElement).getByText('检验报告')).toHaveClass('ant-tag-processing')
    expect(within(title as HTMLElement).getByText('有效')).toHaveClass('ant-tag-success')
    // 第 2 行（概要字段行）：患者、证件号码、报告时间（YYYY-MM-DD）、来源方三行式、当前版本。
    const summary = document.querySelector('.report-management-detail-summary')
    expect(summary).not.toBeNull()
    const fields = within(summary as HTMLElement)
    expect(fields.getByText('张三')).toBeInTheDocument()
    expect(fields.getByText('110101199001011234')).toBeInTheDocument()
    expect(fields.getByText('2026-09-20')).toBeInTheDocument()
    expect(fields.getByText('组织一 医院一')).toBeInTheDocument()
    expect(fields.getByText('院区一')).toBeInTheDocument()
    expect(fields.getByText('V3')).toBeInTheDocument()
  })

  it('作废报告：概要状态 Tag 为中性灰（default），不使用 success', async () => {
    renderDetail('/report-management/detail/REP-1', summaryRow({ status: 2, statusText: '已作废' }))
    await screen.findByText('报告医生乙')

    const title = document.querySelector('.report-management-detail-summary-title') as HTMLElement
    expect(within(title).getByText('已作废')).toHaveClass('ant-tag-default')
    expect(within(title).getByText('已作废')).not.toHaveClass('ant-tag-success')
  })

  it('深链直开无行概要：单号占位可复制、无类型与状态 Tag、五字段全部「—」', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const title = document.querySelector('.report-management-detail-summary-title') as HTMLElement
    expect(within(title).getByRole('button', { name: '复制' })).toBeInTheDocument()
    expect(within(title).queryByText('检验报告')).toBeNull()
    expect(within(title).queryByText('有效')).toBeNull()
    // 五字段全部按「—」呈现；当前版本不渲染任何「V+序号」。
    const summary = document.querySelector('.report-management-detail-summary') as HTMLElement
    for (const label of ['患者姓名', '证件号码', '报告时间', '来源方', '当前版本']) {
      expect(within(summary).getByText(label).parentElement?.textContent).toContain('—')
    }
    expect(within(summary).queryByText(/^V\d+$/)).toBeNull()
  })
})

/**
 * T02 公共信息四组（测试矩阵 C05「公共信息四组渲染」，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md；
 * 根 design.md 6.5 分组与字段顺序、6.6 编码 ID 次行小字）。
 */
describe('T02 公共信息四组（C05）', () => {
  /** 装配公共信息卡：当前有效版本的详情携带给定公共信息读模型。 */
  const renderWithCommon = (common: ReportVersionCommonReadModel | null) => {
    adapters.queryDetail.mockImplementation(async () => versionDetail({
      content: { common, laboratoryContent: null, examinationContent: null },
    }))
    renderDetail()
  }

  /** 公共信息卡内的四组 section。 */
  const publicGroups = () => Array.from(document.querySelectorAll('.report-management-detail-public-group'))

  /**
   * 定位某 label 所在项的内容单元格：无框形态下 label 与 content 同属一个项容器
   * （`.ant-descriptions-item-container` 为 antd 无框形态的既有 DOM 出口），
   * 断言因此绑定 label 与值的配对关系，组内任意一处值错位都会失败。
   */
  const contentCellOf = (label: string): HTMLElement => {
    const labels = Array.from(document.querySelectorAll('.report-management-detail-public .ant-descriptions-item-label'))
    const labelNode = labels.find((node) => node.textContent === label)
    expect(labelNode).not.toBeUndefined()
    const container = (labelNode as HTMLElement).closest('.ant-descriptions-item-container')
    expect(container).not.toBeNull()
    const content = (container as HTMLElement).querySelector('.ant-descriptions-item-content')
    expect(content).not.toBeNull()
    return content as HTMLElement
  }

  /** 断言文本字段的 label 与值配对。 */
  const expectTextValue = (label: string, value: string) => {
    expect(contentCellOf(label).textContent).toBe(value)
  }

  /** 断言「名称（ID）」对字段的 label 与名称主行、ID 次行分别配对。 */
  const expectIdValue = (label: string, name: string, id: string) => {
    const cell = contentCellOf(label)
    expect(cell.querySelector('.report-management-detail-name-id-name')?.textContent).toBe(name)
    expect(cell.querySelector('.report-management-detail-name-id-code')?.textContent).toBe(id)
  }

  it('四组标题按设计顺序齐全，24 个字段标签按组内顺序覆盖', async () => {
    renderWithCommon(commonReadModel())
    await screen.findByText('报告医生乙')

    const titles = document.querySelectorAll('.report-management-detail-public-group-title')
    expect(Array.from(titles).map((node) => node.textContent)).toEqual(['报告信息', '患者信息', '就诊信息', '申请信息'])
    // 24 个字段的分组与组内顺序严格按根 design.md 6.5 表格（label 类名为 antd 无框形态的既有 DOM 出口）。
    const labelGroups = publicGroups().map(
      (group) => Array.from(group.querySelectorAll('.ant-descriptions-item-label')).map((node) => node.textContent),
    )
    expect(labelGroups).toEqual([
      ['报告科室', '报告医生', '审核医生', '审核时间', '报告时间', '执行科室', '来源保密标识'],
      ['患者姓名', '性别代码', '出生日期', '报告时年龄', '证件类型代码', '证件号码', '联系电话'],
      ['就诊类型', '就诊流水号', '住院号', '病区名称', '病房名称', '床位号'],
      ['来源报告名称', '申请科室', '申请医生', '申请时间'],
    ])
  })

  it('字段取值正确呈现：24 字段按 label 与值配对逐一核对，任何一处错位都会失败', async () => {
    renderWithCommon(commonReadModel())
    await screen.findByText('报告医生乙')

    // 报告信息组：名称/ID 对按名称主行与 ID 次行分别配对。
    expectIdValue('报告科室', '检验科', 'DEP-1')
    expectIdValue('报告医生', '报告医生甲', 'DOC-1')
    expectIdValue('审核医生', '审核医生甲', 'DOC-2')
    expectTextValue('审核时间', '2026-09-20 11:00')
    expectTextValue('报告时间', '2026-09-20 10:30')
    expectIdValue('执行科室', '执行科室甲', 'DEP-2')
    expectTextValue('来源保密标识', '否')
    // 患者信息组。
    expectTextValue('患者姓名', '张三')
    expectTextValue('性别代码', 'M')
    expectTextValue('出生日期', '1980-01-01')
    expectTextValue('报告时年龄', '46岁')
    expectTextValue('证件类型代码', '01')
    expectTextValue('证件号码', '110101199001011234')
    expectTextValue('联系电话', '13800000000')
    // 就诊信息组。
    expectTextValue('就诊类型', '门诊')
    expectTextValue('就诊流水号', 'V-0001')
    expectTextValue('住院号', 'ZY-0001')
    expectTextValue('病区名称', '病区甲')
    expectTextValue('病房名称', '病房甲')
    expectTextValue('床位号', '床号12')
    // 申请信息组。
    expectTextValue('来源报告名称', '来源报告甲')
    expectIdValue('申请科室', '申请科室甲', 'DEP-3')
    expectIdValue('申请医生', '申请医生甲', 'DOC-3')
    expectTextValue('申请时间', '2026-09-19 15:00')
  })

  it('科室与医生「名称（ID）」对按次行小字降级：名称主行、ID 次行；ID 缺失只有名称行、名称缺失主行占位', async () => {
    renderWithCommon(commonReadModel({ reportDeptId: null, applicationDeptName: null }))
    await screen.findByText('报告医生乙')

    // 六个「名称（ID）」对：报告科室、报告医生、审核医生、执行科室、申请科室、申请医生。
    const pairs = document.querySelectorAll('.report-management-detail-name-id')
    expect(pairs).toHaveLength(6)
    // 已交付的 ID 全部在次行小字（报告科室缺 ID 不占位）。
    const codes = Array.from(document.querySelectorAll('.report-management-detail-name-id-code')).map((node) => node.textContent)
    expect(codes).toEqual(['DOC-1', 'DOC-2', 'DEP-2', 'DEP-3', 'DOC-3'])
    // 名称缺失沿用「—」，同对已交付的 ID 保留在次行。
    const pairTexts = Array.from(pairs).map((node) => node.textContent)
    expect(pairTexts).toContain('—DEP-3')
  })

  it('来源保密标识非「否」且非空：warning「保密」Tag 加原文；「否」按常规文本无 Tag', async () => {
    renderWithCommon(commonReadModel({ sourceConfidentialFlag: '机构内部' }))
    await screen.findByText('报告医生乙')

    const publicCard = document.querySelector('.report-management-detail-public') as HTMLElement
    expect(within(publicCard).getByText('保密')).toHaveClass('ant-tag-warning')
    expect(within(publicCard).getByText('机构内部')).toBeInTheDocument()
  })

  it('来源保密标识为「否」：常规文本，不渲染保密 Tag', async () => {
    renderWithCommon(commonReadModel())
    await screen.findByText('报告医生乙')

    const publicCard = document.querySelector('.report-management-detail-public') as HTMLElement
    expect(within(publicCard).getByText('否')).toBeInTheDocument()
    expect(within(publicCard).queryByText('保密')).toBeNull()
  })

  it('来源保密标识缺失时显示「—」', async () => {
    renderWithCommon(commonReadModel({ sourceConfidentialFlag: null }))
    await screen.findByText('报告医生乙')

    const publicCard = document.querySelector('.report-management-detail-public') as HTMLElement
    expect(within(publicCard).queryByText('保密')).toBeNull()
    const label = within(publicCard).getByText('来源保密标识')
    expect(label.parentElement?.textContent).toContain('—')
  })

  it('字段缺失或空白按「—」呈现，已交付字段照常', async () => {
    renderWithCommon(commonReadModel({ patientName: null, identityDocumentNo: '   ', reviewTime: null }))
    await screen.findByText('报告医生乙')

    const publicCard = document.querySelector('.report-management-detail-public') as HTMLElement
    expect(within(publicCard).queryByText('张三')).toBeNull()
    expect(within(publicCard).getByText('46岁')).toBeInTheDocument()
    expect(within(publicCard).getAllByText('—').length).toBeGreaterThanOrEqual(3)
  })

  it('该版本未交付公共信息（common 为 null）：四组照常呈现、值全部「—」', async () => {
    renderWithCommon(null)
    await screen.findByText('报告医生乙')

    // 四组结构稳定：组名与 24 个字段标签在位。
    const titles = document.querySelectorAll('.report-management-detail-public-group-title')
    expect(Array.from(titles).map((node) => node.textContent)).toEqual(['报告信息', '患者信息', '就诊信息', '申请信息'])
    const labels = document.querySelectorAll('.report-management-detail-public .ant-descriptions-item-label')
    expect(labels).toHaveLength(24)
    const contents = document.querySelectorAll('.report-management-detail-public .ant-descriptions-item-content')
    expect(contents).toHaveLength(24)
    for (const content of contents) expect(content.textContent).toBe('—')
  })
})

/**
 * T02「检验内容」卡（测试矩阵 C06、C07、C08 组件面；根 design.md 6.6、6.7、6.9）。
 *
 * 检验内容读模型由适配层 `toReportVersionDetail` 原样交付（不归一），夹具按生成端读模型形态
 * 直接构造并经真实适配层出口进入页面，与真实链路一致；断言绑定 label 与值的配对关系、
 * 表格列序、三级编码标签与行类名、折叠行为与空态文案等用户可见行为。
 */
/** 检验结果行、菌种行与药敏行类型：从版本详情交付的检验内容读模型上派生，不在测试里重新声明字段形状。 */
type LabResultItem = NonNullable<LaboratoryReportContentViewReadModel['results']>[number]
type LabBacteriaItem = NonNullable<LaboratoryReportContentViewReadModel['bacteriaResults']>[number]
type LabSusceptibilityItem = NonNullable<LabBacteriaItem['susceptibilities']>[number]

/** 检验结果行夹具：默认为 L1 正常行形态。 */
const labResult = (overrides: Partial<LabResultItem> = {}): LabResultItem => ({
  sourceDetailKey: 'R-KEY-1',
  displayOrder: 1,
  sourceProjectName: '白细胞计数',
  sourceProjectCode: 'WBC-SRC',
  standardProjectCode: 'WBC-STD',
  sourceResultText: '6.2',
  resultType: 1,
  resultTypeText: '定量结果',
  unit: '10^9/L',
  abnormalFlag: 1,
  abnormalFlagText: '正常',
  criticalValueFlag: false,
  referenceRange: '4.0-10.0',
  testingMethod: '阻抗法',
  instrumentName: '血球仪甲',
  inspectorName: '检测人甲',
  ...overrides,
})

/** 药敏行夹具：默认为耐药结论形态。 */
const susceptibility = (overrides: Partial<LabSusceptibilityItem> = {}): LabSusceptibilityItem => ({
  sourceDetailKey: 'S-KEY-1',
  displayOrder: 1,
  drugName: '庆大霉素',
  drugCode: 'GEN',
  sourceConclusionText: '耐药(R)',
  micValue: '>=16',
  inhibitionZoneDiameter: '6',
  diskContent: '10μg',
  referenceValue: '>=15',
  testingMethod: '纸片扩散法',
  testPanelOrder: 'GN13',
  inspectorName: '检测人甲',
  ...overrides,
})

/** 菌种行夹具：默认携带覆盖三色编码与未命中兜底的四个药敏行。 */
const bacteria = (overrides: Partial<LabBacteriaItem> = {}): LabBacteriaItem => ({
  sourceDetailKey: 'B-KEY-1',
  sourceOrganismName: '大肠埃希菌',
  sourceOrganismCode: 'ECO-01',
  colonyCount: '1.2×10^5 CFU/mL',
  detectionConclusion: '检出目标菌',
  cultureMedium: '血琼脂平板',
  cultureTime: '18-24小时',
  cultureCondition: '35°C需氧',
  detectionMethod: '质谱法',
  instrumentName: '质谱仪甲',
  testPanelName: 'GN13板',
  inspectorName: '检测人乙',
  sourceResultText: '检出革兰阴性杆菌，氧化酶阴性。',
  susceptibilities: [
    susceptibility(),
    susceptibility({
      sourceDetailKey: 'S-KEY-2',
      displayOrder: 2,
      drugName: '亚胺培南',
      sourceConclusionText: '敏感(S)',
      micValue: '<=1',
      inhibitionZoneDiameter: '24',
    }),
    susceptibility({
      sourceDetailKey: 'S-KEY-3',
      displayOrder: 3,
      drugName: '头孢吡肟',
      sourceConclusionText: '中介(I)',
      micValue: '8',
      inhibitionZoneDiameter: '15',
    }),
    susceptibility({
      sourceDetailKey: 'S-KEY-4',
      displayOrder: 4,
      drugName: '氨苄西林',
      sourceConclusionText: '无效结果',
      micValue: '32',
      inhibitionZoneDiameter: '11',
    }),
  ],
  ...overrides,
})

/** 检验内容读模型夹具：标本带五字段加四时间、正常/异常/危急三类结果共存、两菌（其一无药敏）。 */
const laboratoryContent = (overrides: Partial<LaboratoryReportContentViewReadModel> = {}): LaboratoryReportContentViewReadModel => ({
  sourceSpecimenNo: 'SP-2026-001',
  specimenTypeName: '静脉血',
  specimenTypeCode: 'XZ-01',
  reportCategoryName: '常规检验',
  reportCategoryCode: 'CG-01',
  sourceOrderSerialNo: 'ORD-889',
  inspectorName: '检验人甲',
  inspectorId: 'INS-1',
  overallAbnormalFlag: '是',
  specimenCollectedTime: new Date(2026, 8, 19, 7, 30),
  specimenSubmittedTime: new Date(2026, 8, 19, 8, 0),
  laboratoryReceivedTime: new Date(2026, 8, 19, 8, 20),
  testingCompletedTime: new Date(2026, 8, 19, 12, 0),
  reportRemark: '报告备注内容甲',
  results: [
    labResult(),
    labResult({
      sourceDetailKey: 'R-KEY-2',
      displayOrder: 2,
      sourceProjectName: 'C反应蛋白',
      sourceProjectCode: 'CRP-SRC',
      standardProjectCode: 'CRP-STD',
      sourceResultText: '58.3',
      unit: 'mg/L',
      abnormalFlagText: '升高(↑)',
      referenceRange: '<10.0',
      testingMethod: '免疫比浊法',
      instrumentName: '生化仪甲',
      inspectorName: '检测人乙',
    }),
    labResult({
      sourceDetailKey: 'R-KEY-3',
      displayOrder: 3,
      sourceProjectName: '血钾',
      sourceProjectCode: 'K-SRC',
      standardProjectCode: 'K-STD',
      sourceResultText: '8.1',
      unit: 'mmol/L',
      abnormalFlagText: '极高(↑)',
      criticalValueFlag: true,
      referenceRange: '3.5-5.5',
      testingMethod: '离子选择电极法',
      instrumentName: '生化仪乙',
      inspectorName: '检测人丙',
    }),
  ],
  bacteriaResults: [
    bacteria(),
    bacteria({
      sourceDetailKey: 'B-KEY-2',
      sourceOrganismName: '金黄色葡萄球菌',
      sourceOrganismCode: 'SAU-02',
      colonyCount: '5.0×10^4 CFU/mL',
      detectionConclusion: '待药敏确认',
      sourceResultText: '血平板上生长，凝固酶阳性。',
      susceptibilities: null,
    }),
  ],
  ...overrides,
})

/** 装配指定检验内容的详情：当前有效版本为检验报告类型。 */
const renderWithLaboratory = (content: LaboratoryReportContentViewReadModel | null, overrides: Record<string, unknown> = {}) => {
  adapters.queryDetail.mockImplementation(async () => versionDetail({
    content: { common: null, laboratoryContent: content, examinationContent: null },
    ...overrides,
  }))
  return renderDetail()
}

/** 标本信息带内按 label 定位字段值文本（绑定 label 与值的配对，任何一处错位都会失败）。 */
const specimenValueOf = (band: HTMLElement, label: string): string => {
  const labels = Array.from(band.querySelectorAll('.report-management-detail-lab-field-label'))
  const labelNode = labels.find((node) => node.textContent === label)
  expect(labelNode).not.toBeUndefined()
  const field = (labelNode as HTMLElement).closest('.report-management-detail-lab-field')
  expect(field).not.toBeNull()
  return (field as HTMLElement).querySelector('.report-management-detail-lab-field-value')?.textContent ?? ''
}

/** 表格内按可见文本定位所在单元格（绑定文本与单元格的配对关系）。 */
const cellOf = (scope: HTMLElement, text: string): HTMLElement => {
  const node = within(scope).getByText(text)
  const cell = node.closest('td')
  expect(cell).not.toBeNull()
  return cell as HTMLElement
}

/** 菌种卡培养条件带内按 label 定位相邻值文本。 */
const cultureValueOf = (card: HTMLElement, label: string): string => {
  const labels = Array.from(card.querySelectorAll('.report-management-detail-lab-culture-label'))
  const labelNode = labels.find((node) => node.textContent === label)
  expect(labelNode).not.toBeUndefined()
  return (labelNode as HTMLElement).nextElementSibling?.textContent ?? ''
}

/** 检查项目行与检查部位行类型：从版本详情交付的检查内容读模型上派生，不在测试里重新声明字段形状。 */
type ExamItem = NonNullable<ExaminationReportContentViewReadModel['items']>[number]
type ExamSite = NonNullable<ExamItem['sites']>[number]

/** 检查部位夹具。 */
const examSite = (overrides: Partial<ExamSite> = {}): ExamSite => ({
  siteName: '胸部',
  sourceSiteCode: 'SITE-01',
  ...overrides,
})

/** 检查项目夹具：默认带两个部位。 */
const examItem = (overrides: Partial<ExamItem> = {}): ExamItem => ({
  sourceProjectName: '胸部正位摄影',
  sourceProjectCode: 'PROJ-SRC-01',
  standardProjectCode: 'PROJ-STD-01',
  sites: [examSite(), examSite({ siteName: '纵隔', sourceSiteCode: 'SITE-02' })],
  ...overrides,
})

/** 病情描述夹具文本（超过 40 字，用于卡头摘要截断断言）。 */
const examConditionText = '患者既往体健，否认高血压、糖尿病、冠心病等慢性病史，否认药物过敏史，本次为常规体检申请胸部检查，无特殊不适主诉。'

/** 检查内容读模型夹具：六层字段齐全的形态，与版本详情经 `content.examinationContent` 交付的生成端读模型同形。 */
const examinationContent = (overrides: Partial<ExaminationReportContentViewReadModel> = {}): ExaminationReportContentViewReadModel => ({
  conclusion: '双侧胸廓对称，肺内未见明显实变，建议随访复查。',
  overallAbnormalFlag: '是',
  findings: '两肺纹理清晰，走行自然，双肺门不大，心影大小形态未见明显异常，双侧膈面光整。',
  sourceDiagnosisName: '双肺未见明显活动性病变',
  sourceDiagnosisCode: 'R91-01',
  conditionDescription: examConditionText,
  examinationPurpose: '常规体检',
  sourceExaminationTypeName: '胸部正侧位DR',
  sourceExaminationTypeCode: 'ETYPE-01',
  examinationTime: new Date(2026, 8, 19, 8, 30),
  examinerName: '检查医生甲',
  examinerId: 'EXA-1',
  examinationMethod: 'DR摄影',
  deviceName: 'DR机甲',
  deviceCode: 'DEV-01',
  sourceImageStatus: 1,
  sourceImageStatusText: '已归档',
  imageAccessUrl: 'https://pacs.example.com/study/1001',
  items: [examItem()],
  reportRemark: null,
  ...overrides,
})

/** 装配指定检查内容的详情：当前有效版本为检查报告类型。 */
const renderWithExamination = (content: ExaminationReportContentViewReadModel | null, overrides: Record<string, unknown> = {}) => {
  adapters.queryDetail.mockImplementation(async () => versionDetail({
    reportType: 2,
    reportTypeText: '检查报告',
    content: { common: null, laboratoryContent: null, examinationContent: content },
    ...overrides,
  }))
  return renderDetail()
}

describe('T02 第四卡按报告类型分发（工单承接）', () => {
  it('检验报告版本呈现「检验内容」卡：标题改名且标本信息带在位，无占位卡名与检查内容卡名', async () => {
    renderWithLaboratory(laboratoryContent())

    expect(await screen.findByText('检验内容')).toBeInTheDocument()
    expect(screen.queryByText('报告内容')).toBeNull()
    expect(screen.queryByText('检查内容')).toBeNull()
    expect(screen.getByText('SP-2026-001')).toBeInTheDocument()
  })

  it('检查报告版本呈现「检查内容」卡：与检验卡互斥，无占位卡名与检验结构化空态', async () => {
    renderWithExamination(examinationContent())

    expect(await screen.findByText('检查内容')).toBeInTheDocument()
    expect(screen.queryByText('报告内容')).toBeNull()
    expect(screen.queryByText('检验内容')).toBeNull()
    expect(screen.queryByText('该版本没有检验结构化内容')).toBeNull()
    expect(document.querySelector('.report-management-detail-lab-specimen')).toBeNull()
  })

  it('检查报告版本无检查结构化内容：呈现「该版本没有检查结构化内容」空态', async () => {
    renderWithExamination(null)

    expect(await screen.findByText('检查内容')).toBeInTheDocument()
    expect(screen.getByText('该版本没有检查结构化内容')).toBeInTheDocument()
  })

  it('报告类型未知：卡身呈现 info 级提示，不按检验或检查渲染', async () => {
    renderWithLaboratory(null, { reportType: null, reportTypeText: '未知类型' })

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveClass('ant-alert-info')
    expect(alert).toHaveTextContent('报告类型未知：服务端未交付可识别的报告类型，无法展示该版本的结构化内容。')
    expect(screen.queryByText('检验内容')).toBeNull()
    expect(screen.queryByText('SP-2026-001')).toBeNull()
  })
})

describe('T02 检验标本信息带（C06）', () => {
  it('五字段齐全且编码按次行小字呈现，四个时间并列展示', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const band = document.querySelector('.report-management-detail-lab-specimen')
    expect(band).not.toBeNull()
    expect(specimenValueOf(band as HTMLElement, '院内标本号')).toBe('SP-2026-001')
    expect(specimenValueOf(band as HTMLElement, '标本类型')).toBe('静脉血XZ-01')
    expect(specimenValueOf(band as HTMLElement, '报告类别')).toBe('常规检验CG-01')
    expect(specimenValueOf(band as HTMLElement, '来源医嘱流水号')).toBe('ORD-889')
    expect(specimenValueOf(band as HTMLElement, '检验人')).toBe('检验人甲INS-1')
    // 编码在名称下次行小字元素内（根 design.md 6.6）。
    const subs = Array.from((band as HTMLElement).querySelectorAll('.report-management-detail-lab-sub')).map((node) => node.textContent)
    expect(subs).toEqual(['XZ-01', 'CG-01', 'INS-1'])
    // 第 2 行四个时间并列（tabular-nums 由配套 CSS 承载）。
    expect(specimenValueOf(band as HTMLElement, '标本采集时间')).toBe('2026-09-19 07:30')
    expect(specimenValueOf(band as HTMLElement, '标本送检时间')).toBe('2026-09-19 08:00')
    expect(specimenValueOf(band as HTMLElement, '检验科接收时间')).toBe('2026-09-19 08:20')
    expect(specimenValueOf(band as HTMLElement, '检测完成时间')).toBe('2026-09-19 12:00')
  })

  it('整体异常标识非「否」且非空时呈现 warning「整体异常」Tag', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const band = document.querySelector('.report-management-detail-lab-specimen') as HTMLElement
    expect(within(band).getByText('整体异常')).toHaveClass('ant-tag-warning')
  })

  it('整体异常标识为「否」或缺失时不渲染整体异常 Tag', async () => {
    const first = renderWithLaboratory(laboratoryContent({ overallAbnormalFlag: '否' }))
    await screen.findByText('检验内容')
    expect(screen.queryByText('整体异常')).toBeNull()
    first.unmount()

    renderWithLaboratory(laboratoryContent({ overallAbnormalFlag: null }))
    await screen.findByText('检验内容')
    expect(screen.queryByText('整体异常')).toBeNull()
  })

  it('报告备注独立行呈现；备注缺失时不占位', async () => {
    const first = renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')
    const remark = document.querySelector('.report-management-detail-lab-remark')
    expect(remark).not.toBeNull()
    expect(remark).toHaveTextContent('报告备注：报告备注内容甲')
    first.unmount()

    renderWithLaboratory(laboratoryContent({ reportRemark: null }))
    await screen.findByText('检验内容')
    expect(document.querySelector('.report-management-detail-lab-remark')).toBeNull()
  })
})

describe('T02 检验结果表三级编码（C07）', () => {
  it('结果表 8 列列序齐全：序号右对齐，「项目」「结果」两列固定左侧', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const section = document.querySelector('.report-management-detail-lab-results')
    expect(section).not.toBeNull()
    const headers = within(section as HTMLElement).getAllByRole('columnheader')
    expect(headers.map((node) => node.textContent)).toEqual(['序号', '项目', '结果', '单位', '异常', '危急', '参考范围', '检测条件'])
    // 序号列右对齐由表格列 align 交付（antd 6 经单元格内联 textAlign 呈现，不产出对齐类名）。
    expect(headers[0]).toHaveStyle({ textAlign: 'right' })
    // 「项目」「结果」固定左侧（antd 6 的固定列经 fix + 逻辑方向 fix-start 类呈现）。
    expect(headers[1]).toHaveClass('ant-table-cell-fix', 'ant-table-cell-fix-start')
    expect(headers[2]).toHaveClass('ant-table-cell-fix', 'ant-table-cell-fix-start')
  })

  it('项目列主行加双编码次行、结果列原文主行加类型次行，与行内其余列配对正确', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const section = document.querySelector('.report-management-detail-lab-results') as HTMLElement
    const projectCell = cellOf(section, '白细胞计数')
    const subs = Array.from(projectCell.querySelectorAll('.report-management-detail-lab-sub')).map((node) => node.textContent)
    expect(subs).toEqual(['来源 WBC-SRC', '互认 WBC-STD'])
    const row = projectCell.closest('tr')
    expect(row).not.toBeNull()
    // 结果列：原文 14px/600 主行（视觉由配套 CSS 承载）+ 结果类型次行。
    expect((row as HTMLElement).querySelector('.report-management-detail-lab-result-text')?.textContent).toBe('6.2')
    const resultCell = cellOf(section, '6.2')
    expect(resultCell.querySelector('.report-management-detail-lab-sub')?.textContent).toBe('定量结果')
    // 单位与参考范围列取值。
    expect(within(row as HTMLElement).getByText('10^9/L')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('4.0-10.0')).toBeInTheDocument()
    // 检测条件列：法/仪/人 三行。
    expect(within(row as HTMLElement).getByText('法')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('阻抗法')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('仪')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('血球仪甲')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('人')).toBeInTheDocument()
    expect(within(row as HTMLElement).getByText('检测人甲')).toBeInTheDocument()
  })

  it('L1 正常行：异常与危急列灰「—」，不渲染「否」标签', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const section = document.querySelector('.report-management-detail-lab-results') as HTMLElement
    const row = cellOf(section, '白细胞计数').closest('tr') as HTMLElement
    const muted = row.querySelectorAll('.report-management-detail-lab-muted')
    expect(muted).toHaveLength(2)
    expect(row.textContent).not.toContain('否')
    expect(row.querySelectorAll('.ant-tag')).toHaveLength(0)
  })

  it('L2 异常行：异常标志原文以 error Tag 呈现', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const section = document.querySelector('.report-management-detail-lab-results') as HTMLElement
    const row = cellOf(section, 'C反应蛋白').closest('tr') as HTMLElement
    const tags = row.querySelectorAll('.ant-tag-error')
    expect(tags).toHaveLength(1)
    expect(tags[0].textContent).toBe('升高(↑)')
    expect(row).not.toHaveClass('report-management-detail-lab-row-critical')
  })

  it('L3 危急行：实心危急 Tag 加整行染底类，危急行异常列仍按 L2 呈现原文', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const section = document.querySelector('.report-management-detail-lab-results') as HTMLElement
    const row = cellOf(section, '血钾').closest('tr') as HTMLElement
    expect(row).toHaveClass('report-management-detail-lab-row-critical')
    const criticalTag = within(row).getByLabelText('危急值')
    expect(criticalTag).toHaveClass('ant-tag')
    expect(criticalTag.textContent).toContain('危急')
    // 实心形态由配套 CSS 类承载（根 design.md 6.7）：底 colorError、白字、字重 600 不走内联样式；图标在位。
    expect(criticalTag).toHaveClass('report-management-detail-lab-critical-tag')
    expect(criticalTag.getAttribute('style')).toBeNull()
    expect(criticalTag.querySelector('svg')).not.toBeNull()
    // 危急行异常列仍按 L2 呈现原文。
    const abnormalTag = row.querySelector('.ant-tag-error')
    expect(abnormalTag?.textContent).toBe('极高(↑)')
  })

  it('普通检验结果为空：呈现既有空态文案，标本带照常', async () => {
    renderWithLaboratory(laboratoryContent({ results: [] }))
    await screen.findByText('检验内容')

    expect(screen.getByText('该版本没有普通检验结果')).toBeInTheDocument()
    expect(screen.getByText('SP-2026-001')).toBeInTheDocument()
  })
})

describe('T02 菌种卡与药敏表（C08）', () => {
  it('菌种卡卡头三要素齐全、培养条件带七项默认展开可见', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const cards = Array.from(document.querySelectorAll('.report-management-detail-lab-bacteria-card'))
    expect(cards).toHaveLength(2)
    const first = cards[0] as HTMLElement
    // 卡头：菌名主行 + 编码次行、colonyCount 计数、检测结论。
    expect(within(first).getByText('大肠埃希菌')).toBeInTheDocument()
    expect(within(first).getByText('ECO-01')).toBeInTheDocument()
    expect(within(first).getByText('1.2×10^5 CFU/mL')).toBeInTheDocument()
    expect(within(first).getByText('检出目标菌')).toBeInTheDocument()
    // 默认展开：培养条件带七项无需点击即在位。
    expect(cultureValueOf(first, '培养基')).toBe('血琼脂平板')
    expect(cultureValueOf(first, '培养时间')).toBe('18-24小时')
    expect(cultureValueOf(first, '培养条件')).toBe('35°C需氧')
    expect(cultureValueOf(first, '检测方法')).toBe('质谱法')
    expect(cultureValueOf(first, '仪器')).toBe('质谱仪甲')
    expect(cultureValueOf(first, '试验板')).toBe('GN13板')
    expect(cultureValueOf(first, '检测人')).toBe('检测人乙')
  })

  it('菌种卡可折叠：默认展开，收起后卡身不渲染，再展开恢复', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const card = document.querySelector('.report-management-detail-lab-bacteria-card') as HTMLElement
    const toggle = within(card).getByRole('button', { name: '收起大肠埃希菌菌种卡' })
    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(within(card).getByText('血琼脂平板')).toBeInTheDocument()

    fireEvent.click(toggle)
    const expandToggle = within(card).getByRole('button', { name: '展开大肠埃希菌菌种卡' })
    expect(expandToggle).toHaveAttribute('aria-expanded', 'false')
    expect(within(card).queryByText('血琼脂平板')).toBeNull()

    fireEvent.click(expandToggle)
    expect(within(card).getByRole('button', { name: '收起大肠埃希菌菌种卡' })).toBeInTheDocument()
    expect(within(card).getByText('血琼脂平板')).toBeInTheDocument()
  })

  it('原始结果文本默认收起，展开后可见原文并可再次收起', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const card = document.querySelector('.report-management-detail-lab-bacteria-card') as HTMLElement
    const header = within(card).getByRole('button', { name: '原始结果文本' })
    expect(header).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('检出革兰阴性杆菌，氧化酶阴性。')).toBeNull()

    fireEvent.click(header)
    expect(within(card).getByRole('button', { name: '原始结果文本' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('检出革兰阴性杆菌，氧化酶阴性。')).toBeInTheDocument()
  })

  it('药敏表 7 列列序齐全，结论三色编码与未命中兜底正确', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const card = document.querySelector('.report-management-detail-lab-bacteria-card') as HTMLElement
    const headers = within(card).getAllByRole('columnheader')
    expect(headers.map((node) => node.textContent)).toEqual(['序号', '受试药物', '结论', 'MIC', '抑菌环', '含药量/参考值', '检测条件'])
    // 结论三色编码：耐药 error、敏感 success、中介 warning。
    expect(within(card).getByText('耐药(R)')).toHaveClass('ant-tag-error')
    expect(within(card).getByText('敏感(S)')).toHaveClass('ant-tag-success')
    expect(within(card).getByText('中介(I)')).toHaveClass('ant-tag-warning')
    // 未命中：中性 Tag 呈现原文，无色彩修饰类。
    const neutral = within(card).getByText('无效结果')
    expect(neutral).toHaveClass('ant-tag')
    expect(neutral.className).not.toMatch(/ant-tag-(error|success|warning)/)
  })

  it('药敏行取值与检测条件三行、含药量/参考值主次行配对正确', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const card = document.querySelector('.report-management-detail-lab-bacteria-card') as HTMLElement
    const row = cellOf(card, '庆大霉素').closest('tr') as HTMLElement
    expect(within(row).getByText('耐药(R)')).toBeInTheDocument()
    expect(within(row).getByText('>=16')).toBeInTheDocument()
    expect(within(row).getByText('6')).toBeInTheDocument()
    // 含药量主行 + 参考值次行。
    expect(within(row).getByText('10μg')).toBeInTheDocument()
    const subs = Array.from(row.querySelectorAll('.report-management-detail-lab-sub')).map((node) => node.textContent)
    expect(subs).toEqual(['参考 >=15'])
    // 检测条件：法/板/人 三行。
    expect(within(row).getByText('纸片扩散法')).toBeInTheDocument()
    expect(within(row).getByText('GN13')).toBeInTheDocument()
    expect(within(row).getByText('检测人甲')).toBeInTheDocument()
  })

  it('无药敏细菌呈现既有空态文案「该细菌没有药敏结果」', async () => {
    renderWithLaboratory(laboratoryContent())
    await screen.findByText('检验内容')

    const cards = Array.from(document.querySelectorAll('.report-management-detail-lab-bacteria-card'))
    const second = cards[1] as HTMLElement
    expect(within(second).getByText('金黄色葡萄球菌')).toBeInTheDocument()
    expect(within(second).getByText('该细菌没有药敏结果')).toBeInTheDocument()
    expect(within(second).queryByRole('table')).toBeNull()
  })
})

describe('T02 检验内容空态与静态证据（C06～C08、根 design.md 6.9）', () => {
  it('laboratoryContent 为 null：呈现「该版本没有检验结构化内容」，不渲染标本带与结果表', async () => {
    renderWithLaboratory(null)
    await screen.findByText('检验内容')

    expect(screen.getByText('该版本没有检验结构化内容')).toBeInTheDocument()
    expect(document.querySelector('.report-management-detail-lab-specimen')).toBeNull()
    expect(document.querySelector('.report-management-detail-lab table')).toBeNull()
  })

  it('细菌鉴定结果为空：呈现既有空态文案「该版本没有细菌鉴定结果」', async () => {
    renderWithLaboratory(laboratoryContent({ bacteriaResults: [] }))
    await screen.findByText('检验内容')

    expect(screen.getByText('该版本没有细菌鉴定结果')).toBeInTheDocument()
    expect(screen.queryByText('该细菌没有药敏结果')).toBeNull()
  })

  it('标本带、危急行染底与左竖条、数字列排版由配套 CSS 提供（jsdom 无布局）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    // 标本信息带（根 design.md 6.7）：colorFillQuaternary 底、圆角 8px、padding 12px 16px。
    const band = css.match(/\.report-management-detail-lab-specimen\s*\{[^}]*\}/)?.[0] ?? ''
    expect(band).toContain('var(--ant-color-fill-quaternary')
    expect(band).toContain('border-radius: 8px')
    expect(band).toContain('padding: 12px 16px')
    // 危急行：整行染底 colorErrorBg、左内嵌 3px colorError 竖条、悬停 colorErrorBgHover。
    const criticalRow = css.match(/\.report-management-detail-lab-row-critical > td\s*\{[^}]*\}/)?.[0] ?? ''
    expect(criticalRow).toContain('var(--ant-color-error-bg')
    const criticalBar = css.match(/\.report-management-detail-lab-row-critical > td:first-child\s*\{[^}]*\}/)?.[0] ?? ''
    expect(criticalBar).toContain('inset 3px 0 0 var(--ant-color-error')
    const criticalHover = css.match(/\.report-management-detail-lab-row-critical > \.ant-table-cell-row-hover\s*\{[^}]*\}/)?.[0] ?? ''
    expect(criticalHover).toContain('var(--ant-color-error-bg-hover')
    // L3 危急 Tag 实心形态（根 design.md 6.7）：底 colorError、白字、字重 600 由配套类承载。
    const criticalTagRule = css.match(/\.report-management-detail-lab \.report-management-detail-lab-critical-tag\s*\{[^}]*\}/)?.[0] ?? ''
    expect(criticalTagRule).toContain('background: var(--ant-color-error')
    expect(criticalTagRule).toContain('color: var(--ant-color-text-light-solid')
    expect(criticalTagRule).toContain('font-weight: 600')
    // 数字列 tabular-nums；结果原文 14px/600；备注与参考范围 break-words；结果表字号 13px。
    const numCell = css.match(/\.report-management-detail-lab-num\s*\{[^}]*\}/)?.[0] ?? ''
    expect(numCell).toContain('font-variant-numeric: tabular-nums')
    const resultText = css.match(/\.report-management-detail-lab-result-text\s*\{[^}]*\}/)?.[0] ?? ''
    expect(resultText).toContain('font-size: 14px')
    expect(resultText).toContain('font-weight: 600')
    expect(resultText).toContain('font-variant-numeric: tabular-nums')
    const remark = css.match(/\.report-management-detail-lab-remark\s*\{[^}]*\}/)?.[0] ?? ''
    expect(remark).toContain('word-break: break-word')
    const table = css.match(/\.report-management-detail-lab \.ant-table\s*\{[^}]*\}/)?.[0] ?? ''
    expect(table).toContain('font-size: 13px')
  })
})

/**
 * T02「检查内容」卡（测试矩阵 C09「检查内容排版」，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md；
 * 根 design.md 6.6、6.8、6.9）。
 *
 * 检查内容读模型由适配层 `toReportVersionDetail` 原样交付（不归一），夹具按生成端读模型形态
 * 直接构造并经真实适配层出口进入页面；断言六层顺序、label 与值配对、外链属性、折叠行为、
 * 部位 chips 与空态文案等用户可见行为。
 */
describe('T02 检查内容六层（C09）', () => {
  /** 检查信息 Descriptions 内按 label 定位内容单元格（绑定 label 与值的配对，任何一处错位都会失败）。 */
  const infoCellOf = (label: string): HTMLElement => {
    const labels = Array.from(document.querySelectorAll('.report-management-detail-exam .ant-descriptions-item-label'))
    const labelNode = labels.find((node) => node.textContent === label)
    expect(labelNode).not.toBeUndefined()
    const container = (labelNode as HTMLElement).closest('.ant-descriptions-item-container')
    expect(container).not.toBeNull()
    const content = (container as HTMLElement).querySelector('.ant-descriptions-item-content')
    expect(content).not.toBeNull()
    return content as HTMLElement
  }

  it('六层齐全：结论 callout 置顶含整体异常 Tag，所见、诊断、临床背景、检查信息、项目部位依次呈现', async () => {
    renderWithExamination(examinationContent())
    await screen.findByText('检查内容')

    const exam = document.querySelector('.report-management-detail-exam')
    expect(exam).not.toBeNull()
    // 第 1 层结论 callout 置顶（容器首个子块）：块头「检查结论」+ 整体异常 warning Tag + 结论正文。
    const first = (exam as HTMLElement).firstElementChild
    expect(first).toHaveClass('report-management-detail-exam-conclusion')
    expect(within(first as HTMLElement).getByText('检查结论')).toBeInTheDocument()
    expect(within(first as HTMLElement).getByText('整体异常')).toHaveClass('ant-tag-warning')
    expect(within(first as HTMLElement).getByText('双侧胸廓对称，肺内未见明显实变，建议随访复查。')).toBeInTheDocument()
    // 第 2 层检查所见正文。
    expect(screen.getByText('两肺纹理清晰，走行自然，双肺门不大，心影大小形态未见明显异常，双侧膈面光整。')).toBeInTheDocument()
    // 第 3 层来源诊断：名称主行 + 编码次行小字。
    const diagnosis = screen.getByText('双肺未见明显活动性病变').closest('.report-management-detail-name-id')
    expect(diagnosis?.querySelector('.report-management-detail-name-id-code')?.textContent).toBe('R91-01')
    // 第 4 层临床背景折叠头在位。
    expect(screen.getByRole('button', { name: /临床背景/ })).toBeInTheDocument()
    // 第 5、6 层节标题在位。
    expect(screen.getByText('检查信息')).toBeInTheDocument()
    expect(screen.getByText('检查项目与部位')).toBeInTheDocument()
  })

  it('整体异常标识为「否」或缺失时不渲染整体异常 Tag', async () => {
    const first = renderWithExamination(examinationContent({ overallAbnormalFlag: '否' }))
    await screen.findByText('检查内容')
    expect(screen.queryByText('整体异常')).toBeNull()
    first.unmount()

    renderWithExamination(examinationContent({ overallAbnormalFlag: null }))
    await screen.findByText('检查内容')
    expect(screen.queryByText('整体异常')).toBeNull()
  })

  it('临床背景默认收起：卡头携带病情描述前 40 字摘要，展开后病情描述与检查目的可见', async () => {
    renderWithExamination(examinationContent())
    await screen.findByText('检查内容')

    const header = screen.getByRole('button', { name: /临床背景/ })
    expect(header).toHaveAttribute('aria-expanded', 'false')
    // 卡头摘要只取病情描述前 40 字，尾部不出现在折叠头。
    expect(header.textContent).toContain(examConditionText.slice(0, 40))
    expect(header.textContent).not.toContain('无特殊不适主诉')
    // 默认收起：检查目的不可见。
    expect(screen.queryByText('常规体检')).not.toBeInTheDocument()

    fireEvent.click(header)
    const expanded = screen.getByRole('button', { name: /临床背景/ })
    expect(expanded).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText(examConditionText)).toBeInTheDocument()
    expect(screen.getByText('常规体检')).toBeInTheDocument()
  })

  it('检查信息字段与取值按 label 配对：类型与设备编码次行、检查医生 ID 次行、时间规范格式、影像状态中性 Tag', async () => {
    renderWithExamination(examinationContent())
    await screen.findByText('检查内容')

    expect(infoCellOf('来源检查类型').querySelector('.report-management-detail-name-id-name')?.textContent).toBe('胸部正侧位DR')
    expect(infoCellOf('来源检查类型').querySelector('.report-management-detail-name-id-code')?.textContent).toBe('ETYPE-01')
    expect(infoCellOf('实际检查时间').textContent).toBe('2026-09-19 08:30')
    const doctorCell = infoCellOf('检查医生')
    expect(doctorCell.querySelector('.report-management-detail-name-id-name')?.textContent).toBe('检查医生甲')
    expect(doctorCell.querySelector('.report-management-detail-name-id-code')?.textContent).toBe('EXA-1')
    expect(infoCellOf('检查方法').textContent).toBe('DR摄影')
    expect(infoCellOf('设备').querySelector('.report-management-detail-name-id-name')?.textContent).toBe('DR机甲')
    expect(infoCellOf('设备').querySelector('.report-management-detail-name-id-code')?.textContent).toBe('DEV-01')
    // 来源影像状态：中性 Tag（无语义色修饰类）。
    const statusTag = within(infoCellOf('来源影像状态')).getByText('已归档')
    expect(statusTag).toHaveClass('ant-tag')
    expect(statusTag.className).not.toMatch(/ant-tag-(error|success|warning|processing)/)
  })

  it('影像调阅地址渲染为外链：target=_blank、rel 安全、中文 aria 名称并带图标；缺失时占位「—」', async () => {
    const first = renderWithExamination(examinationContent())
    await screen.findByText('检查内容')

    const link = screen.getByRole('link', { name: '在新窗口打开影像调阅地址' })
    expect(link).toHaveAttribute('href', 'https://pacs.example.com/study/1001')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noreferrer noopener')
    expect(link.querySelector('svg')).not.toBeNull()
    first.unmount()

    renderWithExamination(examinationContent({ imageAccessUrl: null }))
    await screen.findByText('检查内容')
    expect(screen.queryByRole('link', { name: '在新窗口打开影像调阅地址' })).toBeNull()
    expect(infoCellOf('影像调阅地址').textContent).toBe('—')
  })

  it('检查信息字段缺失按「—」呈现，名称缺失时已交付的编码保留次行', async () => {
    renderWithExamination(examinationContent({
      sourceExaminationTypeName: null,
      examinationTime: null,
      examinerName: null,
      examinationMethod: null,
      deviceName: null,
      sourceImageStatusText: null,
    }))
    await screen.findByText('检查内容')

    expect(infoCellOf('实际检查时间').textContent).toBe('—')
    expect(infoCellOf('检查方法').textContent).toBe('—')
    expect(infoCellOf('来源影像状态').textContent).toBe('—')
    expect(infoCellOf('来源检查类型').textContent).toBe('—ETYPE-01')
  })

  it('检查项目与部位：项目名主行加双编码次行，部位 chips 含部位名与编码', async () => {
    renderWithExamination(examinationContent())
    await screen.findByText('检查内容')

    const item = document.querySelector('.report-management-detail-exam-item') as HTMLElement
    expect(item).not.toBeNull()
    expect(item.querySelector('.report-management-detail-exam-item-name')?.textContent).toContain('胸部正位摄影')
    const subs = Array.from(item.querySelectorAll('.report-management-detail-lab-sub')).map((node) => node.textContent)
    expect(subs).toEqual(['来源 PROJ-SRC-01', '互认 PROJ-STD-01'])
    const chips = Array.from(item.querySelectorAll('.report-management-detail-exam-site')).map((node) => node.textContent)
    expect(chips).toEqual(['胸部SITE-01', '纵隔SITE-02'])
    expect(item.textContent).not.toContain('该项目没有明确检查部位')
  })

  it('项目无部位：呈现「该项目没有明确检查部位」，不渲染部位 chips', async () => {
    renderWithExamination(examinationContent({ items: [examItem({ sites: [] })] }))
    await screen.findByText('检查内容')

    expect(screen.getByText('该项目没有明确检查部位')).toBeInTheDocument()
    expect(document.querySelector('.report-management-detail-exam-site')).toBeNull()
  })

  it('检查项目为空：呈现「该版本没有检查项目」', async () => {
    renderWithExamination(examinationContent({ items: [] }))
    await screen.findByText('检查内容')

    expect(screen.getByText('该版本没有检查项目')).toBeInTheDocument()
  })

  it('检查内容排版与时间线圆点规则由配套 CSS 提供（jsdom 无布局）', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/pages/reportManagement/ReportManagement.css'), 'utf8')
    // 结论 callout（根 design.md 6.8 第 1 层）：colorFillQuaternary 底、左 3px colorPrimary、圆角 8px、padding 12px 16px。
    const callout = css.match(/\.report-management-detail-exam-conclusion\s*\{[^}]*\}/)?.[0] ?? ''
    expect(callout).toContain('var(--ant-color-fill-quaternary')
    expect(callout).toContain('border-left: 3px solid var(--ant-color-primary')
    expect(callout).toContain('border-radius: 8px')
    expect(callout).toContain('padding: 12px 16px')
    // 结论正文 15px、行高 1.7。
    const conclusionText = css.match(/\.report-management-detail-exam-conclusion-text\s*\{[^}]*\}/)?.[0] ?? ''
    expect(conclusionText).toContain('font-size: 15px')
    expect(conclusionText).toContain('line-height: 1.7')
    // 检查所见：14px、行高 1.7、最大行宽 72ch。
    const findings = css.match(/\.report-management-detail-exam-findings\s*\{[^}]*\}/)?.[0] ?? ''
    expect(findings).toContain('font-size: 14px')
    expect(findings).toContain('line-height: 1.7')
    expect(findings).toContain('max-width: 72ch')
    // 项目块左 2px colorBorderSecondary 竖线 + 左 padding 12px。
    const item = css.match(/\.report-management-detail-exam-item\s*\{[^}]*\}/)?.[0] ?? ''
    expect(item).toContain('border-left: 2px solid var(--ant-color-border-secondary')
    expect(item).toContain('padding-left: 12px')
    // 部位编码 11px tertiary。
    const siteCode = css.match(/\.report-management-detail-exam-site-code\s*\{[^}]*\}/)?.[0] ?? ''
    expect(siteCode).toContain('font-size: 11px')
    expect(siteCode).toContain('var(--ant-color-text-tertiary')
    // 时间线三态圆点（根 design.md 6.4）：当前实心 colorPrimary、已被替代空心 colorBorder、已作废空心 colorTextQuaternary。
    const dotCurrent = css.match(/\.report-management-detail-timeline-dot-current\s*\{[^}]*\}/)?.[0] ?? ''
    expect(dotCurrent).toContain('var(--ant-color-primary')
    expect(dotCurrent).not.toContain('border')
    const dotSuperseded = css.match(/\.report-management-detail-timeline-dot-superseded\s*\{[^}]*\}/)?.[0] ?? ''
    expect(dotSuperseded).toContain('border: 2px solid var(--ant-color-border')
    const dotVoided = css.match(/\.report-management-detail-timeline-dot-voided\s*\{[^}]*\}/)?.[0] ?? ''
    expect(dotVoided).toContain('border: 2px solid var(--ant-color-text-quaternary')
    // 版本序号 tabular-nums；备注单行截断。
    const versionNo = css.match(/\.report-management-detail-timeline-version\s*\{[^}]*\}/)?.[0] ?? ''
    expect(versionNo).toContain('font-variant-numeric: tabular-nums')
    const remarkRow = css.match(/\.report-management-detail-timeline-row-remark\s*\{[^}]*\}/)?.[0] ?? ''
    expect(remarkRow).toContain('text-overflow: ellipsis')
  })
})

/**
 * T02「历史版本时间线」（测试矩阵 C10「历史版本时间线」，
 * docs/plans/_temporary/T02-报告管理与历史版本页面查询详情整改/Client/testPlan.md；
 * 根 design.md 6.3、6.4、6.9）。
 *
 * 时间线节点的事实行以「label 值」紧凑同行呈现（版本信息卡元信息条承载完整的 label/值形制），
 * 因此节点内医生等文本不与元信息条的单值文本冲突；断言默认展开、节点元信息、圆点三态、
 * 「查看此版本」联动切换与下载单在途互斥等用户可见行为。
 */
describe('T02 历史版本时间线（C10）', () => {
  /** 历史版本卡范围（卡标题定位卡片容器）。 */
  const timelineCard = () => screen.getByText('历史版本').closest('.ant-card') as HTMLElement
  /** 时间线节点范围：按版本序号文本定位节点容器。 */
  const nodeOf = (sequence: number) => {
    const versionNode = Array.from(document.querySelectorAll('.report-management-detail-timeline-node'))
      .find((node) => node.querySelector('.report-management-detail-timeline-version')?.textContent === `V${sequence}`)
    expect(versionNode).not.toBeUndefined()
    return versionNode as HTMLElement
  }
  /** 时间线条目范围（节点与圆点的共同祖先）：按版本序号定位所在条目，圆点与节点在该范围内配对。 */
  const itemOf = (sequence: number) => {
    const item = nodeOf(sequence).closest('li')
    expect(item).not.toBeNull()
    return item as HTMLElement
  }

  it('历史版本卡承接时间线：折叠头「全部版本（2）」，默认展开呈现全部版本节点', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const header = within(timelineCard()).getByRole('button', { name: '全部版本（2）' })
    expect(header).toHaveAttribute('aria-expanded', 'true')
    // 默认展开：两个版本的节点无需操作即在位。
    expect(nodeOf(1)).not.toBeNull()
    expect(nodeOf(2)).not.toBeNull()
  })

  it('节点元信息齐全：V{n} 加版本状态 Tag、两个时间、两医生、备注、PDF 文件名与两个动作', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const first = nodeOf(1)
    expect(within(first).getByText('V1')).toBeInTheDocument()
    expect(within(first).getByText('已被替代')).toHaveClass('ant-tag-default')
    expect(first.textContent).toContain('源端修改时间 2026-09-19 09:00')
    expect(first.textContent).toContain('平台接收时间 2026-09-19 09:01')
    expect(first.textContent).toContain('报告医生 报告医生甲')
    expect(first.textContent).toContain('审核医生 审核医生甲')
    expect(first.textContent).toContain('来源备注 来源备注甲')
    expect(within(first).getByText('R-1-v1.pdf')).toBeInTheDocument()
    // 节点按钮带中文 aria-label：按可访问名称属性定位（与读屏播报同一载体）。
    expect(within(first).getByLabelText('查看第 1 版内容')).toBeInTheDocument()
    expect(within(first).getByLabelText('下载第 1 版 PDF')).toBeInTheDocument()

    const second = nodeOf(2)
    expect(within(second).getByText('V2')).toBeInTheDocument()
    expect(within(second).getByText('当前有效')).toHaveClass('ant-tag-success')
    expect(second.textContent).toContain('源端修改时间 2026-09-20 09:05')
    expect(within(second).getByLabelText('查看第 2 版内容')).toBeInTheDocument()
    expect(within(second).getByLabelText('下载第 2 版 PDF')).toBeInTheDocument()
  })

  it('节点事实缺失按「—」呈现', async () => {
    adapters.queryVersions.mockResolvedValue([versionRow({
      isCurrentVersion: true,
      isSuperseded: false,
      sourceModifiedTime: null,
      platformReceivedTime: null,
      reportDoctorName: null,
      reviewDoctorName: null,
      sourceReportRemark: null,
    })])
    renderDetail()

    await screen.findByText('V1 · 当前版本')
    const node = nodeOf(1)
    expect(node.textContent).toContain('源端修改时间 —')
    expect(node.textContent).toContain('平台接收时间 —')
    expect(node.textContent).toContain('报告医生 —')
    expect(node.textContent).toContain('审核医生 —')
    expect(node.textContent).toContain('来源备注 —')
  })

  it('节点圆点三态：当前实心、已被替代空心、已作废空心；作废状态 Tag 优先于当前有效表述', async () => {
    adapters.queryVersions.mockResolvedValue([
      versionRow(),
      currentVersionRow(),
      versionRow({
        reportVersionId: 'VER-3',
        versionSequence: 3,
        isCurrentVersion: false,
        isSuperseded: false,
        reportStatus: 2,
        reportStatusText: '已作废',
        pdfFileName: 'R-1-v3.pdf',
      }),
    ])
    renderDetail()
    await screen.findByText('报告医生乙')

    // 圆点与节点同属一个条目（li）：按条目配对断言三态类。
    expect(itemOf(1).querySelector('.report-management-detail-timeline-dot')).toHaveClass('report-management-detail-timeline-dot-superseded')
    expect(itemOf(2).querySelector('.report-management-detail-timeline-dot')).toHaveClass('report-management-detail-timeline-dot-current')
    expect(itemOf(3).querySelector('.report-management-detail-timeline-dot')).toHaveClass('report-management-detail-timeline-dot-voided')
    expect(within(nodeOf(3)).getByText('已作废')).toBeInTheDocument()
  })

  it('「查看此版本」联动版本切换：读取该版本详情、选中态与内容更新、URL 以 replace 写回 ?v= 序号', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    fireEvent.click(within(nodeOf(1)).getByLabelText('查看第 1 版内容'))
    await waitFor(() => expect(adapters.queryDetail).toHaveBeenCalledWith('REP-1', 'VER-1'))
    // 选中态更新：版本切换控件第 1 版选中；版本信息卡切换为 V1 的事实。
    await waitFor(() => expect(versionOption('第 1 版（历史版本）')).toBeChecked())
    await waitFor(() => expect(within(metaCard()).getByText('R-1-v1.pdf')).toBeInTheDocument())
    await waitFor(() => expect(locationRef.current.search).toBe('?v=1'))
    expect(locationRef.current.navigationType).toBe('REPLACE')
  })

  it('「下载 PDF」接线单在途互斥：在途节点按钮 loading 且其余版本按钮（含元信息条）禁用，完成后复位', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const pending = deferred<void>()
    adapters.downloadVersion.mockReturnValueOnce(pending.promise)
    fireEvent.click(within(nodeOf(1)).getByLabelText('下载第 1 版 PDF'))
    expect(adapters.downloadVersion).toHaveBeenCalledWith('REP-1', 'VER-1', 'R-1-v1.pdf')

    // 在途：V1 节点按钮 loading + aria-busy；其余版本（时间线 V2 与元信息条 V2）禁用。
    const downloadingButton = within(nodeOf(1)).getByLabelText('下载第 1 版 PDF')
    expect(downloadingButton).toHaveClass('ant-btn-loading')
    expect(downloadingButton).toHaveAttribute('aria-busy', 'true')
    expect(within(nodeOf(2)).getByLabelText('下载第 2 版 PDF')).toBeDisabled()
    expect(within(metaCard()).getByLabelText('下载第 2 版 PDF')).toBeDisabled()

    await act(async () => { pending.resolve(undefined) })
    await waitFor(() => expect(within(nodeOf(1)).getByLabelText('下载第 1 版 PDF')).not.toHaveClass('ant-btn-loading'))
    expect(within(nodeOf(2)).getByLabelText('下载第 2 版 PDF')).not.toBeDisabled()
    expect(within(metaCard()).getByLabelText('下载第 2 版 PDF')).not.toBeDisabled()
  })

  it('时间线入口下载失败同样复位为可重试：按钮退出在途态，其余版本解除禁用，可再次发起', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const failing = deferred<void>()
    adapters.downloadVersion.mockReturnValueOnce(failing.promise)
    fireEvent.click(within(nodeOf(1)).getByLabelText('下载第 1 版 PDF'))
    expect(within(nodeOf(1)).getByLabelText('下载第 1 版 PDF')).toHaveClass('ant-btn-loading')
    expect(within(nodeOf(2)).getByLabelText('下载第 2 版 PDF')).toBeDisabled()

    // 失败（根 design.md 6.9「PDF 下载失败」行）：按钮复位为可点，错误由宿主统一提示。
    await act(async () => { failing.reject(new Error('下载失败')) })
    const reset = await waitFor(() => {
      const node = within(nodeOf(1)).getByLabelText('下载第 1 版 PDF')
      expect(node).not.toHaveClass('ant-btn-loading')
      return node
    })
    expect(reset).not.toHaveAttribute('aria-busy')
    expect(reset.textContent).toContain('下载 PDF')
    expect(within(nodeOf(2)).getByLabelText('下载第 2 版 PDF')).not.toBeDisabled()

    // 复位后重试：经时间线入口再次发起成功。
    adapters.downloadVersion.mockResolvedValueOnce(undefined)
    fireEvent.click(within(nodeOf(1)).getByLabelText('下载第 1 版 PDF'))
    await waitFor(() => expect(adapters.downloadVersion).toHaveBeenCalledTimes(2))
  })

  it('折叠头可收起：收起后内容面板进入收起形制，再展开恢复', async () => {
    renderDetail()
    await screen.findByText('报告医生乙')

    const header = () => within(timelineCard()).getByRole('button', { name: '全部版本（2）' })
    expect(within(timelineCard()).getByText('R-1-v1.pdf')).toBeInTheDocument()
    // 展开态：内容面板为 active 形制。
    expect(timelineCard().querySelector('.ant-collapse-panel-active')).not.toBeNull()
    expect(timelineCard().querySelector('.ant-collapse-panel-inactive')).toBeNull()

    fireEvent.click(header())
    expect(header()).toHaveAttribute('aria-expanded', 'false')
    // 收起态：内容面板为 inactive 形制（真实浏览器中由折叠动效隐藏）。
    expect(timelineCard().querySelector('.ant-collapse-panel-inactive')).not.toBeNull()
    expect(timelineCard().querySelector('.ant-collapse-panel-active')).toBeNull()

    fireEvent.click(header())
    expect(header()).toHaveAttribute('aria-expanded', 'true')
    expect(within(timelineCard()).getByText('R-1-v1.pdf')).toBeInTheDocument()
  })

  it('版本列表在途：时间线区渲染骨架（沿用既有加载相位）', async () => {
    const pending = deferred<ReportVersionRow[]>()
    adapters.queryVersions.mockReturnValueOnce(pending.promise)
    renderDetail()

    await screen.findByRole('status')
    expect(timelineCard().querySelector('.ant-skeleton')).not.toBeNull()
    await act(async () => { pending.resolve(twoVersions()) })
  })
})

describe('T02 证件号码展示截断（长值证件号列表与详情的呈现兜底）', () => {
  const renderId = (value: string | null | undefined) => render(<IdNumberText value={value} />)

  it('标准长度（≤18 位）原样呈现全文，不挂 Tooltip', () => {
    renderId('330105198103120011')
    expect(screen.getByText('330105198103120011')).toBeTruthy()
    expect(screen.queryByRole('tooltip')).toBeNull()
  })

  it('超长证件号中段省略（保留前 6 后 4），Tooltip 呈现全文', async () => {
    const long = 'X'.repeat(30) + '0012'
    renderId(long)
    expect(screen.getByText('XXXXXX…0012')).toBeTruthy()
    expect(screen.queryByText(long)).toBeNull()
    fireEvent.mouseEnter(screen.getByText('XXXXXX…0012'))
    expect(await screen.findByRole('tooltip')).toHaveTextContent(long)
  })

  it('缺失值呈现「—」', () => {
    renderId(null)
    expect(screen.getByText('—')).toBeTruthy()
  })

  it('列表证件号码列：超长值截断呈现且不撑爆行高形态（渲染走同一组件）', () => {
    const long = 'X'.repeat(30) + '0012'
    const { container } = render(
      <table><tbody><tr><td><IdNumberText value={long} /></td></tr></tbody></table>,
    )
    expect(container.textContent).toContain('XXXXXX…0012')
    expect(container.textContent?.length ?? 0).toBeLessThan(long.length)
  })
})
