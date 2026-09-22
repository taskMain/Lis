/**
 * 四个统计页面（平台/本院 × 接收侧/来源侧）共享的页面主体（S6-D11）。
 *
 * 主体承载统计页的全部行为编排：筛选区、范围选择、汇总维度单选与汇总表、
 * 明细类型单选与明细表、导出入口与匹配记录弹窗；接收侧与来源侧的结构差异用 `side` 表达，
 * 平台/本院的范围形态差异用 `scopeSpec` 表达。数据源由四个页面注入（查询/导出入口以适配层为边界），
 * 主体不接触生成的 Kiota 类型，也不创建 API Client。
 *
 * 服务端分页口径（同报告管理主体先例）：
 * 1. 分页由服务端完成，页面不排序、不做本地过滤与本地分页；
 * 2. 筛选、汇总维度或明细类型变化回到第 1 页并保留页容量，并清空旧结果（C9）；
 * 3. 当页为空且总数为正时按服务端总数回退到最后一页一次（C19 越界自愈）；
 * 4. 同一条件下只采纳最后一次请求结果，过期响应直接丢弃（C9）。
 *
 * 失败面（汇总读取、明细读取、导出、弹窗读取）都只做本层恢复：
 * 保留已有数据、结束加载态、提供重试；不显示本地接口错误提示，错误提示由宿主统一展示。
 */
import { ReloadOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Empty,
  Input,
  Radio,
  Select,
  Space,
  Table,
  Tooltip,
  Typography,
  type TableColumnsType,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
import { useEnumMetadata, type EnumMetadataOption } from '../../hooks/useEnumMetadata'
import type { MedicalItemTypeValue } from '../../shared/medicalItemType'
import { createTablePagination } from '../../shared/tablePagination'
import { MatchRecordModal, type MatchRecordDialogState } from './matchRecordModal'
import type {
  MatchRecordView,
  UsageDetailRow,
  UsageSummaryRow,
} from './recognitionUsageStatisticsApi'
import type {
  SourceDetailRow,
  SourceSummaryRow,
} from './sourceRecognitionStatisticsApi'
import {
  buildStatisticsExportDownloadName,
  toStatisticsRequestDate,
  type StatisticsPage,
  type StatisticsPageInput,
  type StatisticsGroupDimensionValue,
  type UsageDetailTypeValue,
} from './recognitionStatisticsValues'
import {
  formatStatisticsAmount,
  formatStatisticsDateTime,
  formatStatisticsPercent,
  statisticsTextOrDash,
} from './statisticsDisplays'

/** 统计页侧别：接收侧（互认使用）或来源侧（被互认）。 */
export type StatisticsSide = 'usage' | 'source'

/** 主体内两侧行的联合：行的实际类型与 `side` 一致（数据源由页面按侧注入）。 */
export type StatisticsSummaryRow = UsageSummaryRow | SourceSummaryRow
export type StatisticsDetailRow = UsageDetailRow | SourceDetailRow

/**
 * 页面筛选状态的稳定模型：日期必填；范围字段按页面版本取用（平台页提交两组三级，
 * 本院页提交本侧院区与对侧组医院/院区）；科室与项目条件两侧共用；互认医生与不采纳原因只进明细查询。
 */
export interface StatisticsFilters {
  startTime: string
  endTime: string
  /** 本院页本侧院区（接收侧页为接收院区，来源侧页为来源院区）；为空按可信医院全院查询。 */
  ownBranchCode?: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  recognitionDeptId?: string
  itemType?: MedicalItemTypeValue
  standardProjectCode?: string
  recognitionDoctorId?: string
  nonAdoptionReasonCode?: string
}

/** 范围选择区的形态：平台页两组三级可选；本院页本侧固定展示、对侧组医院/院区可选（S6-D1）。 */
export type StatisticsScopeSpec =
  | { kind: 'platform' }
  | { kind: 'branch'; organizationCode: string; hospitalCode: string; trustedBranchCode: string | null }

/** 主体的数据源与装配边界；查询/导出入口返回 `null` 表示条件不完整，主体据此零发请求并阻断。 */
export interface StatisticsBoardProps {
  title: string
  subtitle: string
  side: StatisticsSide
  /** 范围键：可信范围变化即视为范围切换，主体清空全部条件与数据。 */
  scopeKey: string
  scopeSpec: StatisticsScopeSpec
  /** 初始筛选条件（含日期与本院页的本侧院区默认值）；范围切换时按此复位。 */
  initialFilters: StatisticsFilters
  querySummaryPage: (
    filters: StatisticsFilters,
    dimension: number,
    page: StatisticsPageInput,
  ) => Promise<StatisticsPage<StatisticsSummaryRow>> | null
  queryDetailsPage: (
    filters: StatisticsFilters,
    detailType: number,
    page: StatisticsPageInput,
  ) => Promise<StatisticsPage<StatisticsDetailRow>> | null
  exportWorkbook: (
    filters: StatisticsFilters,
    request: { exportType: number; groupDimension: number; detailType: number | null; downloadFileName: string },
  ) => Promise<void> | null
  /** 匹配记录查询出口；仅接收侧注入（来源侧明细不提供匹配记录入口，S6-D10）。 */
  queryMatchRecord?: (recognitionMatchRecordId: string) => Promise<MatchRecordView>
}

/** 汇总维度选项：固定页面文案（四个页面差异只取值来源与固定文案）；来源侧不含互认科室维度（S6-D2）。 */
const USAGE_DIMENSION_OPTIONS = [
  { value: 1, label: '医院' },
  { value: 2, label: '院区' },
  { value: 3, label: '互认科室' },
  { value: 4, label: '标准项目' },
]
const SOURCE_DIMENSION_OPTIONS = [
  { value: 1, label: '来源医院' },
  { value: 2, label: '来源院区' },
  { value: 4, label: '标准项目' },
]

/** 接收侧明细类型选项（S6-D2 明细类型枚举取值）。 */
const DETAIL_TYPE_OPTIONS = [
  { value: 1, label: '提醒' },
  { value: 2, label: '采纳' },
  { value: 3, label: '不采纳' },
  { value: 4, label: '引用' },
]

/** 项目类型筛选的兜底选项；取值与共享取值域一致，服务端枚举元数据可用时改取服务端声明。 */
const MEDICAL_ITEM_TYPE_FALLBACK: readonly EnumMetadataOption[] = [
  { value: 0, name: 'Laboratory', label: '检验' },
  { value: 1, name: 'Examination', label: '检查' },
]

/** 区块容器：带可访问名称的区域，供用例与读屏定位。 */
function Section(props: { label: string; children: ReactNode }) {
  return <section aria-label={props.label}>{props.children}</section>
}

/** 可空文本单元格。 */
function textCell(value: string | null | undefined): string {
  return statisticsTextOrDash(value)
}

/** 汇总行的稳定行标识：由维度相关字段合成；服务端同一维度内分组值唯一。 */
function usageSummaryRowKey(row: UsageSummaryRow): string {
  return [
    row.receiverOrganizationCode,
    row.receiverHospitalCode,
    row.receiverBranchCode,
    row.recognitionDeptId,
    row.itemType,
    row.standardProjectCode,
  ].map((part) => part ?? '').join('|')
}

/** 来源侧汇总行的稳定行标识，口径同接收侧。 */
function sourceSummaryRowKey(row: SourceSummaryRow): string {
  return [
    row.sourceOrganizationCode,
    row.sourceHospitalCode,
    row.sourceBranchCode,
    row.itemType,
    row.standardProjectCode,
  ].map((part) => part ?? '').join('|')
}

/** 接收侧汇总行下钻的筛选预填：按维度值落到对应范围字段（本院页用不到的字段由页面映射忽略）。 */
function usageDrilldownPatch(row: UsageSummaryRow): Partial<StatisticsFilters> {
  switch (row.groupDimension) {
    case 1: return { receiverHospitalCode: row.receiverHospitalCode ?? undefined }
    case 2: return { receiverBranchCode: row.receiverBranchCode ?? undefined, ownBranchCode: row.receiverBranchCode ?? undefined }
    case 3: return { recognitionDeptId: row.recognitionDeptId ?? undefined }
    case 4: return { itemType: row.itemType ?? undefined, standardProjectCode: row.standardProjectCode ?? undefined }
    default: return {}
  }
}

/** 来源侧汇总行下钻的筛选预填，口径同接收侧。 */
function sourceDrilldownPatch(row: SourceSummaryRow): Partial<StatisticsFilters> {
  switch (row.groupDimension) {
    case 1: return { sourceHospitalCode: row.sourceHospitalCode ?? undefined }
    case 2: return { sourceBranchCode: row.sourceBranchCode ?? undefined, ownBranchCode: row.sourceBranchCode ?? undefined }
    case 4: return { itemType: row.itemType ?? undefined, standardProjectCode: row.standardProjectCode ?? undefined }
    default: return {}
  }
}

/** 可下钻的指标数字按钮。 */
function metricDrillButton(ariaLabel: string, count: number, onClick: () => void): ReactNode {
  return <Button type='link' size='small' aria-label={ariaLabel} onClick={onClick}>{count}</Button>
}

/** 接收侧汇总列：维度列随所选维度出列（未参与分组的维度列不渲染），后接六项指标列。 */
function usageSummaryColumns(
  dimension: number,
  onMetricDrill: (row: UsageSummaryRow, detailType: UsageDetailTypeValue) => void,
): TableColumnsType<UsageSummaryRow> {
  const dimensionColumns: TableColumnsType<UsageSummaryRow> = []
  if (dimension === 1) {
    dimensionColumns.push(
      { title: '组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.receiverOrganizationName) },
      { title: '医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.receiverHospitalName) },
    )
  } else if (dimension === 2) {
    dimensionColumns.push(
      { title: '组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.receiverOrganizationName) },
      { title: '医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.receiverHospitalName) },
      { title: '院区', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiverBranchName) },
    )
  } else if (dimension === 3) {
    dimensionColumns.push(
      { title: '院区', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiverBranchName) },
      { title: '互认科室ID', width: 120, ellipsis: true, render: (_, row) => textCell(row.recognitionDeptId) },
      { title: '互认科室名称', width: 140, ellipsis: true, render: (_, row) => textCell(row.recognitionDeptName) },
    )
  } else {
    dimensionColumns.push(
      { title: '项目类型', width: 90, render: (_, row) => row.itemTypeText },
      { title: '分类名称', width: 120, ellipsis: true, render: (_, row) => textCell(row.categoryName) },
      { title: '分组名称', width: 120, ellipsis: true, render: (_, row) => textCell(row.groupName) },
      { title: '标准项目编码', width: 120, ellipsis: true, render: (_, row) => textCell(row.standardProjectCode) },
      { title: '标准项目名称', width: 140, ellipsis: true, render: (_, row) => textCell(row.standardProjectName) },
    )
  }
  return [
    ...dimensionColumns,
    { title: '提醒次数', width: 100, render: (_, row) => metricDrillButton('下钻提醒明细', row.reminderCount, () => onMetricDrill(row, 1)) },
    { title: '采纳次数', width: 100, render: (_, row) => metricDrillButton('下钻采纳明细', row.adoptionCount, () => onMetricDrill(row, 2)) },
    { title: '不采纳次数', width: 100, render: (_, row) => metricDrillButton('下钻不采纳明细', row.nonAdoptionCount, () => onMetricDrill(row, 3)) },
    { title: '引用次数', width: 100, render: (_, row) => metricDrillButton('下钻引用明细', row.referenceCount, () => onMetricDrill(row, 4)) },
    // 同期互认率未计算时展示「—」，该数字不可下钻（明细类型与互认率无对应关系）。
    { title: '同期互认率', width: 110, render: (_, row) => formatStatisticsPercent(row.samePeriodRecognitionRate) },
    {
      title: '预计节省金额',
      width: 120,
      render: (_, row) => row.estimatedSavingAmount === null
        ? '—'
        : <Button type='link' size='small' aria-label='下钻采纳明细（预计节省金额）' onClick={() => onMetricDrill(row, 2)}>
            {formatStatisticsAmount(row.estimatedSavingAmount)}
          </Button>,
    },
  ]
}

/** 来源侧汇总列：只计被互认次数，无金额、互认率与原因区（C22）。 */
function sourceSummaryColumns(
  dimension: number,
  onDrill: (row: SourceSummaryRow) => void,
): TableColumnsType<SourceSummaryRow> {
  const dimensionColumns: TableColumnsType<SourceSummaryRow> = []
  if (dimension === 1) {
    dimensionColumns.push(
      { title: '来源组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceOrganizationName) },
      { title: '来源医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceHospitalName) },
    )
  } else if (dimension === 2) {
    dimensionColumns.push(
      { title: '来源组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceOrganizationName) },
      { title: '来源医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceHospitalName) },
      { title: '来源院区', width: 120, ellipsis: true, render: (_, row) => textCell(row.sourceBranchName) },
    )
  } else {
    dimensionColumns.push(
      { title: '项目类型', width: 90, render: (_, row) => row.itemTypeText },
      { title: '分类名称', width: 120, ellipsis: true, render: (_, row) => textCell(row.categoryName) },
      { title: '分组名称', width: 120, ellipsis: true, render: (_, row) => textCell(row.groupName) },
      { title: '标准项目编码', width: 120, ellipsis: true, render: (_, row) => textCell(row.standardProjectCode) },
      { title: '标准项目名称', width: 140, ellipsis: true, render: (_, row) => textCell(row.standardProjectName) },
    )
  }
  return [
    ...dimensionColumns,
    { title: '被互认次数', width: 110, render: (_, row) => metricDrillButton('下钻被互认明细', row.recognitionCount, () => onDrill(row)) },
  ]
}

/** 不采纳原因单元格：未反馈行与无处理结果的行留空；已反馈行展示原因名称与补充说明。 */
function usageReasonCell(row: UsageDetailRow): ReactNode {
  const result = row.processingResult
  if (row.isUnprocessed || result === null) return ''
  const name = statisticsTextOrDash(result.nonAdoptionReasonName)
  return result.nonAdoptionSupplementDescription !== null
    ? <Space size={4}>{name}<Typography.Text type='secondary'>（{result.nonAdoptionSupplementDescription}）</Typography.Text></Space>
    : name
}

/** 接收侧明细列：未反馈行显示「未反馈」且处理结果列留空；末列为匹配记录入口（S6-D10）。 */
function usageDetailColumns(onOpenRecord: (recognitionMatchRecordId: string) => void): TableColumnsType<UsageDetailRow> {
  return [
    { title: '匹配生成时间', width: 130, render: (_, row) => formatStatisticsDateTime(row.matchCreatedTime) },
    { title: '就诊类型', width: 90, render: (_, row) => row.visitTypeText },
    { title: '就诊流水号', width: 130, ellipsis: true, render: (_, row) => row.visitSerialNo },
    { title: '来源组织', width: 120, ellipsis: true, render: (_, row) => textCell(row.source.organizationName) },
    { title: '来源医院', width: 120, ellipsis: true, render: (_, row) => textCell(row.source.hospitalName) },
    { title: '来源院区', width: 110, ellipsis: true, render: (_, row) => textCell(row.source.branchName) },
    { title: '接收组织', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiver.organizationName) },
    { title: '接收医院', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiver.hospitalName) },
    { title: '接收院区', width: 110, ellipsis: true, render: (_, row) => textCell(row.receiver.branchName) },
    { title: '项目类型', width: 90, render: (_, row) => row.item.itemTypeText },
    { title: '项目编码', width: 110, ellipsis: true, render: (_, row) => textCell(row.item.standardProjectCode) },
    { title: '项目名称', width: 130, ellipsis: true, render: (_, row) => textCell(row.item.standardProjectName) },
    { title: '患者姓名', width: 90, render: (_, row) => row.patientName },
    { title: '证件号码', width: 150, ellipsis: true, render: (_, row) => row.identityDocumentNo },
    { title: '互认科室', width: 110, ellipsis: true, render: (_, row) => textCell(row.recognitionDeptName) },
    { title: '互认医生', width: 90, ellipsis: true, render: (_, row) => textCell(row.recognitionDoctorName) },
    { title: '业务时间', width: 130, render: (_, row) => formatStatisticsDateTime(row.businessTime) },
    { title: '反馈状态', width: 90, render: (_, row) => (row.isUnprocessed ? '未反馈' : '已反馈') },
    { title: '处理结果', width: 110, render: (_, row) => (row.isUnprocessed ? '' : statisticsTextOrDash(row.processingResult?.decisionText)) },
    { title: '不采纳原因', width: 150, ellipsis: true, render: (_, row) => usageReasonCell(row) },
    { title: '互认时间', width: 130, render: (_, row) => formatStatisticsDateTime(row.processingResult?.recognitionTime ?? null) },
    { title: '预计节省金额', width: 110, render: (_, row) => formatStatisticsAmount(row.processingResult?.estimatedSavingAmount ?? null) },
    { title: '引用时间', width: 130, render: (_, row) => formatStatisticsDateTime(row.reference?.referenceTime ?? null) },
    {
      title: '操作',
      width: 150,
      fixed: 'right',
      render: (_, row) => <Button
        type='link'
        size='small'
        aria-label='查看互认匹配记录'
        onClick={() => onOpenRecord(row.recognitionMatchRecordId)}
      >查看互认匹配记录</Button>,
    },
  ]
}

/** 来源侧明细列：双方归属平铺；只反映被采纳事实，无匹配记录入口（S6-D10）。 */
function sourceDetailColumns(): TableColumnsType<SourceDetailRow> {
  return [
    { title: '来源组织', width: 120, ellipsis: true, render: (_, row) => textCell(row.source.organizationName) },
    { title: '来源医院', width: 120, ellipsis: true, render: (_, row) => textCell(row.source.hospitalName) },
    { title: '来源院区', width: 110, ellipsis: true, render: (_, row) => textCell(row.source.branchName) },
    { title: '接收组织', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiver.organizationName) },
    { title: '接收医院', width: 120, ellipsis: true, render: (_, row) => textCell(row.receiver.hospitalName) },
    { title: '接收院区', width: 110, ellipsis: true, render: (_, row) => textCell(row.receiver.branchName) },
    { title: '项目编码', width: 110, ellipsis: true, render: (_, row) => textCell(row.standardProjectCode) },
    { title: '互认科室', width: 110, ellipsis: true, render: (_, row) => textCell(row.recognitionDeptName) },
    { title: '互认医生', width: 90, ellipsis: true, render: (_, row) => textCell(row.recognitionDoctorName) },
    { title: '互认时间', width: 130, render: (_, row) => formatStatisticsDateTime(row.recognitionTime) },
    { title: '患者姓名', width: 90, render: (_, row) => row.patientName },
    { title: '证件号码', width: 150, ellipsis: true, render: (_, row) => row.identityDocumentNo },
  ]
}

/**
 * 列表空页自愈（C19）：当页为空且总数为正时按服务端总数回退最后一页一次；
 * 回退已发生过（守卫键相同）时不再重复触发，避免空页反复重取。
 */
function useEmptyPageHeal(args: {
  loading: boolean
  failure: string | null
  rowCount: number
  totalCount: number
  pageIndex: number
  pageSize: number
  scopeKey: string
  guardRef: { current: string | null }
  retake: () => void
}) {
  const { loading, failure, rowCount, totalCount, pageIndex, pageSize, scopeKey, guardRef, retake } = args
  useEffect(() => {
    if (loading || failure !== null) return
    if (rowCount > 0 || totalCount <= 0) return
    // 守卫键按换算后的末页记录：回退动作（含 setPage 与其引发的重读）对同一条件只执行一次，
    // 不因回退后页码变化而重新放行。
    const lastPage = Math.max(1, Math.ceil(totalCount / pageSize))
    const guard = `${scopeKey}|${lastPage}|${pageSize}|${totalCount}`
    if (guardRef.current === guard) return
    guardRef.current = guard
    // 推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(retake, 0)
    return () => window.clearTimeout(task)
  }, [failure, guardRef, loading, pageIndex, pageSize, retake, rowCount, scopeKey, totalCount])
}

export function StatisticsBoard(props: StatisticsBoardProps) {
  const {
    title,
    subtitle,
    side,
    scopeKey,
    scopeSpec,
    initialFilters,
    querySummaryPage,
    queryDetailsPage,
    exportWorkbook,
    queryMatchRecord,
  } = props

  // ===== 会话状态 =====
  const [filters, setFilters] = useState<StatisticsFilters>(initialFilters)
  const [dimension, setDimension] = useState<StatisticsGroupDimensionValue>(1)
  const [detailType, setDetailType] = useState<UsageDetailTypeValue>(1)
  const [summaryRows, setSummaryRows] = useState<StatisticsSummaryRow[]>([])
  const [summaryTotal, setSummaryTotal] = useState(0)
  const [summaryPage, setSummaryPage] = useState<StatisticsPageInput>({ pageIndex: 1, pageSize: 10 })
  const [summaryLoading, setSummaryLoading] = useState(false)
  const [summaryFailure, setSummaryFailure] = useState<null | 'load'>(null)
  const [detailRows, setDetailRows] = useState<StatisticsDetailRow[]>([])
  const [detailTotal, setDetailTotal] = useState(0)
  const [detailPage, setDetailPage] = useState<StatisticsPageInput>({ pageIndex: 1, pageSize: 10 })
  const [detailLoading, setDetailLoading] = useState(false)
  const [detailFailure, setDetailFailure] = useState<null | 'load'>(null)
  const [exporting, setExporting] = useState(false)
  const [recordState, setRecordState] = useState<MatchRecordDialogState | null>(null)

  /**
   * 在途读取的作废机制：按序号决定谁写入状态，不取消在途请求（生成客户端无取消入口，
   * 同报告管理主体先例）。复位与筛选变化经条件/页对象引用变化触发读取入口重新执行，
   * 读取入口在发起前递增序号，旧请求跑完后因序号不符而被丢弃。
   */
  const summarySeqRef = useRef(0)
  const detailSeqRef = useRef(0)
  const summaryHealGuardRef = useRef<string | null>(null)
  const detailHealGuardRef = useRef<string | null>(null)

  // ===== 渲染期复位 =====

  /**
   * 范围切换在渲染期复位：旧范围的条件、数据与弹窗先于任何请求被丢弃（同报告管理主体先例）。
   * 复位后条件状态的 `scopeMark` 恒等于当前范围键，晚到的旧范围响应既不改写数据也不改写失败态。
   */
  const [scopeMark, setScopeMark] = useState(scopeKey)
  if (scopeMark !== scopeKey) {
    setScopeMark(scopeKey)
    setFilters(initialFilters)
    setDimension(1)
    setDetailType(1)
    setSummaryRows([])
    setSummaryTotal(0)
    setSummaryPage({ pageIndex: 1, pageSize: summaryPage.pageSize })
    setSummaryFailure(null)
    setDetailRows([])
    setDetailTotal(0)
    setDetailPage({ pageIndex: 1, pageSize: detailPage.pageSize })
    setDetailFailure(null)
    setRecordState(null)
  }

  // 筛选/维度变化：汇总回到第 1 页并清空旧结果（C9，保留页容量）。
  const [summaryMark, setSummaryMark] = useState({ filters, dimension })
  if (summaryMark.filters !== filters || summaryMark.dimension !== dimension) {
    setSummaryMark({ filters, dimension })
    setSummaryPage((previous) => ({ ...previous, pageIndex: 1 }))
    setSummaryRows([])
    setSummaryTotal(0)
    setSummaryFailure(null)
  }
  // 筛选/维度/明细类型变化：明细同样回到第 1 页并清空旧结果。
  const [detailMark, setDetailMark] = useState({ filters, dimension, detailType })
  if (detailMark.filters !== filters || detailMark.dimension !== dimension || detailMark.detailType !== detailType) {
    setDetailMark({ filters, dimension, detailType })
    setDetailPage((previous) => ({ ...previous, pageIndex: 1 }))
    setDetailRows([])
    setDetailTotal(0)
    setDetailFailure(null)
  }

  // ===== 读取 =====

  const loadSummary = useCallback(async (
    targetFilters: StatisticsFilters,
    targetDimension: number,
    targetPage: StatisticsPageInput,
  ) => {
    const requestId = ++summarySeqRef.current
    setSummaryLoading(true)
    try {
      const task = querySummaryPage(targetFilters, targetDimension, targetPage)
      if (task === null) return // 条件不完整：零发请求并阻断，保留已有数据。
      const page = await task
      if (requestId !== summarySeqRef.current) return
      setSummaryRows(page.items)
      setSummaryTotal(page.totalCount)
      setSummaryFailure(null)
    } catch {
      if (requestId !== summarySeqRef.current) return
      // 读取失败保留已有数据，只进入待刷新状态；不把失败结果覆盖成空列表，也不本地报错。
      setSummaryFailure('load')
    } finally {
      if (requestId === summarySeqRef.current) setSummaryLoading(false)
    }
  }, [querySummaryPage])

  const loadDetails = useCallback(async (
    targetFilters: StatisticsFilters,
    targetDetailType: number,
    targetPage: StatisticsPageInput,
  ) => {
    const requestId = ++detailSeqRef.current
    setDetailLoading(true)
    try {
      const task = queryDetailsPage(targetFilters, targetDetailType, targetPage)
      if (task === null) return
      const page = await task
      if (requestId !== detailSeqRef.current) return
      setDetailRows(page.items)
      setDetailTotal(page.totalCount)
      setDetailFailure(null)
    } catch {
      if (requestId !== detailSeqRef.current) return
      setDetailFailure('load')
    } finally {
      if (requestId === detailSeqRef.current) setDetailLoading(false)
    }
  }, [queryDetailsPage])

  // 条件、维度、明细类型或页码变化时重新读取当页（自动加载入口）。
  useEffect(() => {
    const task = window.setTimeout(() => { void loadSummary(filters, dimension, summaryPage) }, 0)
    return () => window.clearTimeout(task)
  }, [dimension, filters, loadSummary, summaryPage])

  useEffect(() => {
    const task = window.setTimeout(() => { void loadDetails(filters, detailType, detailPage) }, 0)
    return () => window.clearTimeout(task)
  }, [detailPage, detailType, filters, loadDetails])

  // ===== 越界自愈（C19） =====

  const retakeSummary = useCallback(() => {
    const lastPage = Math.max(1, Math.ceil(summaryTotal / summaryPage.pageSize))
    if (lastPage !== summaryPage.pageIndex) {
      setSummaryPage((previous) => ({ ...previous, pageIndex: lastPage }))
      return
    }
    void loadSummary(filters, dimension, summaryPage)
  }, [dimension, filters, loadSummary, summaryPage, summaryTotal])

  const retakeDetails = useCallback(() => {
    const lastPage = Math.max(1, Math.ceil(detailTotal / detailPage.pageSize))
    if (lastPage !== detailPage.pageIndex) {
      setDetailPage((previous) => ({ ...previous, pageIndex: lastPage }))
      return
    }
    void loadDetails(filters, detailType, detailPage)
  }, [detailPage, detailTotal, detailType, filters, loadDetails])

  useEmptyPageHeal({
    loading: summaryLoading,
    failure: summaryFailure,
    rowCount: summaryRows.length,
    totalCount: summaryTotal,
    pageIndex: summaryPage.pageIndex,
    pageSize: summaryPage.pageSize,
    scopeKey,
    guardRef: summaryHealGuardRef,
    retake: retakeSummary,
  })
  useEmptyPageHeal({
    loading: detailLoading,
    failure: detailFailure,
    rowCount: detailRows.length,
    totalCount: detailTotal,
    pageIndex: detailPage.pageIndex,
    pageSize: detailPage.pageSize,
    scopeKey,
    guardRef: detailHealGuardRef,
    retake: retakeDetails,
  })

  // ===== 交互 =====

  const applyFilters = useCallback((patch: Partial<StatisticsFilters>) => {
    setFilters((previous) => ({ ...previous, ...patch }))
  }, [])

  /** 平台页接收组/来源组三级范围选择。 */
  const changeReceiverScope = useCallback((value: BaseScopeValue) => applyFilters({
    receiverOrganizationCode: value.orgId,
    receiverHospitalCode: value.hosId,
    receiverBranchCode: value.branchId,
  }), [applyFilters])
  const changeSourceScope = useCallback((value: BaseScopeValue) => applyFilters({
    sourceOrganizationCode: value.orgId,
    sourceHospitalCode: value.hosId,
    sourceBranchCode: value.branchId,
  }), [applyFilters])

  /** 本院页对侧组范围选择：对侧组织固定为可信组织，只提交医院与院区（S6-D1）。 */
  const changeOppositeScope = useCallback((value: BaseScopeValue) => {
    if (side === 'usage') applyFilters({ sourceHospitalCode: value.hosId, sourceBranchCode: value.branchId })
    else applyFilters({ receiverHospitalCode: value.hosId, receiverBranchCode: value.branchId })
  }, [applyFilters, side])

  /** 汇总数字下钻：预填维度值筛选并切换明细类型（C12）。 */
  const drillToUsageDetails = useCallback((row: UsageSummaryRow, targetDetailType: UsageDetailTypeValue) => {
    setDetailType(targetDetailType)
    applyFilters(usageDrilldownPatch(row))
  }, [applyFilters])
  const drillToSourceDetails = useCallback((row: SourceSummaryRow) => {
    applyFilters(sourceDrilldownPatch(row))
  }, [applyFilters])
  /** 原因行下钻：明细加原因代码筛选并切到不采纳明细（C12）。 */
  const drillToReasonDetails = useCallback((reasonCode: string) => {
    setDetailType(3)
    applyFilters({ nonAdoptionReasonCode: reasonCode })
  }, [applyFilters])

  /** 导出：沿用当前查询条件（不带分页）与下载名构造；导出中禁用重复提交（C15）。 */
  const runExport = useCallback(async (kind: 'summary' | 'details') => {
    if (exporting) return
    // 日期缺失（被用户清空）时条件不完整，不构造导出请求，也不构造下载名。
    if (toStatisticsRequestDate(filters.startTime) === null || toStatisticsRequestDate(filters.endTime) === null) return
    const exportType = kind === 'summary'
      ? (side === 'usage' ? 1 : 6)
      : (side === 'usage' ? detailType + 1 : 7)
    const task = exportWorkbook(filters, {
      exportType,
      groupDimension: dimension,
      detailType: kind === 'details' ? detailType : null,
      downloadFileName: buildStatisticsExportDownloadName(exportType, filters.startTime, filters.endTime),
    })
    if (task === null) return
    setExporting(true)
    try {
      await task
    } catch {
      // 导出失败由宿主统一提示；本层只结束加载态，不误报成功、不弹本地错误。
    } finally {
      setExporting(false)
    }
  }, [detailType, dimension, exportWorkbook, exporting, filters, side])

  /**
   * 打开匹配记录弹窗并读取记录（C14）。
   *
   * 竞态防护用纯状态守卫表达：迟到的响应只在弹窗仍展示同一记录时写入，
   * 弹窗已关闭（状态为 null）或已切换为其他记录时直接丢弃，失败面保留弹窗并允许重试。
   */
  const openMatchRecord = useCallback((recognitionMatchRecordId: string) => {
    if (queryMatchRecord === undefined) return
    setRecordState({ recognitionMatchRecordId, view: null, loading: true, failure: false })
    void queryMatchRecord(recognitionMatchRecordId).then((view) => {
      setRecordState((previous) => (previous !== null && previous.recognitionMatchRecordId === recognitionMatchRecordId
        ? { recognitionMatchRecordId, view, loading: false, failure: false }
        : previous))
    }, () => {
      setRecordState((previous) => (previous !== null && previous.recognitionMatchRecordId === recognitionMatchRecordId
        ? { ...previous, loading: false, failure: true }
        : previous))
    })
  }, [queryMatchRecord])

  const retryMatchRecord = useCallback(() => {
    if (recordState !== null) openMatchRecord(recordState.recognitionMatchRecordId)
  }, [openMatchRecord, recordState])

  // ===== 派生 =====

  const itemTypeOptions = useEnumMetadata('MedicalItemType', MEDICAL_ITEM_TYPE_FALLBACK)
  const dimensionOptions = side === 'usage' ? USAGE_DIMENSION_OPTIONS : SOURCE_DIMENSION_OPTIONS

  /**
   * 汇总表的原因区整表展开（内嵌原因区，设计「汇总展示」节）。
   * `as` 收窄依据的主体不变量：数据源由页面按侧注入，行的实际类型与 `side` 一致。
   */
  const expandedSummaryKeys = useMemo<string[]>(() => (
    side === 'usage'
      ? summaryRows.map((row) => usageSummaryRowKey(row as UsageSummaryRow))
      : []
  ), [side, summaryRows])

  const usageSummaryCols = useMemo(() => usageSummaryColumns(dimension, drillToUsageDetails), [dimension, drillToUsageDetails])
  const sourceSummaryCols = useMemo(() => sourceSummaryColumns(dimension, drillToSourceDetails), [dimension, drillToSourceDetails])
  const usageDetailCols = useMemo(() => usageDetailColumns(openMatchRecord), [openMatchRecord])
  const sourceDetailCols = useMemo(() => sourceDetailColumns(), [])

  const summaryPagination = createTablePagination({
    pageIndex: summaryPage.pageIndex,
    pageSize: summaryPage.pageSize,
    totalCount: summaryTotal,
    loading: summaryLoading,
    onChange: (nextPageIndex, nextPageSize) => setSummaryPage({ pageIndex: nextPageIndex, pageSize: nextPageSize }),
  })
  const detailPagination = createTablePagination({
    pageIndex: detailPage.pageIndex,
    pageSize: detailPage.pageSize,
    totalCount: detailTotal,
    loading: detailLoading,
    onChange: (nextPageIndex, nextPageSize) => setDetailPage({ pageIndex: nextPageIndex, pageSize: nextPageSize }),
  })

  /** 原因区内嵌展示：不采纳为零或无原因集合时展示「—」。 */
  const renderReasons = (row: UsageSummaryRow): ReactNode => {
    if (row.nonAdoptionCount === 0 || row.nonAdoptionReasons.length === 0) return '—'
    return <div className='statistics-board-reasons'>
      {row.nonAdoptionReasons.map((reason) => <Space key={reason.reasonCode} size={12}>
        <Button
          type='link'
          size='small'
          aria-label='下钻不采纳原因明细'
          onClick={() => drillToReasonDetails(reason.reasonCode)}
        >{statisticsTextOrDash(reason.reasonName)}</Button>
        <span>次数 {reason.count}</span>
        <span>占比 {formatStatisticsPercent(reason.ratio)}</span>
      </Space>)}
    </div>
  }

  return <div className='statistics-board'>
    <div className='statistics-board-header'>
      <div>
        <Typography.Title level={2}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      <Space wrap>
        <Tooltip title='重新读取当前条件下的汇总与明细'>
          <Button
            icon={<ReloadOutlined />}
            aria-label='重新读取统计数据'
            loading={summaryLoading || detailLoading}
            onClick={() => {
              void loadSummary(filters, dimension, summaryPage)
              void loadDetails(filters, detailType, detailPage)
            }}
          />
        </Tooltip>
      </Space>
    </div>

    {/* 范围选择区：形态由 scopeSpec 决定（平台两组三级可选；本院本侧固定、对侧组医院/院区可选）。 */}
    <div className='statistics-board-scope'>
      {scopeSpec.kind === 'platform' ? (
        side === 'usage'
          ? <Space wrap>
              <div role='group' aria-label='接收组范围'>
                <BaseScopeSelector level='branch' value={{
                  orgId: filters.receiverOrganizationCode,
                  hosId: filters.receiverHospitalCode,
                  branchId: filters.receiverBranchCode,
                }} onChange={changeReceiverScope} />
              </div>
              <div role='group' aria-label='来源组范围'>
                <BaseScopeSelector level='branch' value={{
                  orgId: filters.sourceOrganizationCode,
                  hosId: filters.sourceHospitalCode,
                  branchId: filters.sourceBranchCode,
                }} onChange={changeSourceScope} />
              </div>
            </Space>
          : <Space wrap>
              <div role='group' aria-label='来源组范围'>
                <BaseScopeSelector level='branch' value={{
                  orgId: filters.sourceOrganizationCode,
                  hosId: filters.sourceHospitalCode,
                  branchId: filters.sourceBranchCode,
                }} onChange={changeSourceScope} />
              </div>
              <div role='group' aria-label='接收组范围'>
                <BaseScopeSelector level='branch' value={{
                  orgId: filters.receiverOrganizationCode,
                  hosId: filters.receiverHospitalCode,
                  branchId: filters.receiverBranchCode,
                }} onChange={changeReceiverScope} />
              </div>
            </Space>
      ) : <Space wrap>
          <Typography.Text type='secondary'>
            可信范围（只读）：组织 {scopeSpec.organizationCode}，医院 {scopeSpec.hospitalCode}
          </Typography.Text>
          <div role='group' aria-label='本院院区'>
            <BaseScopeSelector
              level='branch'
              orgId={scopeSpec.organizationCode}
              hosId={scopeSpec.hospitalCode}
              value={{ branchId: filters.ownBranchCode }}
              onChange={(value: BaseScopeValue) => applyFilters({ ownBranchCode: value.branchId })}
            />
          </div>
          <div role='group' aria-label={side === 'usage' ? '来源医院范围' : '接收医院范围'}>
            <BaseScopeSelector
              level='branch'
              orgId={scopeSpec.organizationCode}
              value={side === 'usage'
                ? { hosId: filters.sourceHospitalCode, branchId: filters.sourceBranchCode }
                : { hosId: filters.receiverHospitalCode, branchId: filters.receiverBranchCode }}
              onChange={changeOppositeScope}
            />
          </div>
        </Space>}
    </div>

    {/* 筛选区：日期必填（页面不做跨度本地校验）与科室/项目条件。 */}
    <div className='statistics-board-filters'>
      <Space wrap>
        <label className='statistics-board-date-label'>
          统计期间
          <input
            type='date'
            aria-label='统计开始日期'
            className='statistics-board-date'
            value={filters.startTime}
            onChange={(event) => applyFilters({ startTime: event.target.value })}
          />
        </label>
        <label className='statistics-board-date-label'>
          至
          <input
            type='date'
            aria-label='统计结束日期'
            className='statistics-board-date'
            value={filters.endTime}
            onChange={(event) => applyFilters({ endTime: event.target.value })}
          />
        </label>
        <Input
          placeholder='互认科室ID'
          aria-label='互认科室筛选'
          allowClear
          style={{ width: 140 }}
          value={filters.recognitionDeptId ?? ''}
          onChange={(event) => applyFilters({ recognitionDeptId: event.target.value || undefined })}
        />
        <Select<MedicalItemTypeValue>
          allowClear
          placeholder='项目类型'
          aria-label='项目类型筛选'
          style={{ width: 120 }}
          value={filters.itemType}
          options={itemTypeOptions.map((option) => ({ value: option.value, label: option.label }))}
          onChange={(value) => applyFilters({ itemType: value ?? undefined })}
        />
        <Input
          placeholder='标准项目编码'
          aria-label='标准项目编码筛选'
          allowClear
          style={{ width: 140 }}
          value={filters.standardProjectCode ?? ''}
          onChange={(event) => applyFilters({ standardProjectCode: event.target.value || undefined })}
        />
      </Space>
    </div>

    {/* 汇总区 */}
    <Section label='汇总统计'>
      <Space wrap className='statistics-board-toolbar'>
        <span>汇总维度</span>
        <div role='group' aria-label='汇总维度'>
          <Radio.Group
            optionType='button'
            value={dimension}
            options={dimensionOptions}
            onChange={(event) => setDimension(event.target.value)}
          />
        </div>
        <Button
          aria-label='导出汇总'
          loading={exporting}
          disabled={exporting}
          onClick={() => { void runExport('summary') }}
        >导出汇总</Button>
      </Space>
      {side === 'usage' && dimension === 3 ? <Typography.Text type='secondary' className='statistics-board-note'>
        互认科室维度不含未反馈项：该维度的提醒次数与同期互认率仅统计已反馈的匹配项，跨维度的提醒合计不可直接对账。
      </Typography.Text> : null}
      {summaryFailure === null ? null : <Alert
        type='warning'
        showIcon
        className='statistics-board-failure'
        title='汇总数据待刷新'
        description='读取失败，已有数据保留；可重试读取。'
        action={<Button size='small' disabled={summaryLoading} onClick={() => { void loadSummary(filters, dimension, summaryPage) }}>重试</Button>}
      />}
      {side === 'usage' ? <Table<UsageSummaryRow>
        rowKey={(row) => usageSummaryRowKey(row)}
        size='small'
        columns={usageSummaryCols}
        dataSource={summaryRows as UsageSummaryRow[]}
        loading={summaryLoading && summaryRows.length === 0}
        scroll={{ x: 1100 }}
        expandable={{
          expandedRowKeys: expandedSummaryKeys,
          expandedRowRender: (row) => renderReasons(row),
        }}
        pagination={summaryPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='当前筛选条件没有汇总数据' /> }}
      /> : <Table<SourceSummaryRow>
        rowKey={(row) => sourceSummaryRowKey(row)}
        size='small'
        columns={sourceSummaryCols}
        dataSource={summaryRows as SourceSummaryRow[]}
        loading={summaryLoading && summaryRows.length === 0}
        scroll={{ x: 1100 }}
        pagination={summaryPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='当前筛选条件没有汇总数据' /> }}
      />}
    </Section>

    {/* 明细区 */}
    <Section label={side === 'usage' ? '使用明细' : '被互认明细'}>
      <Space wrap className='statistics-board-toolbar'>
        {side === 'usage' ? <>
          <span>明细类型</span>
          <div role='group' aria-label='明细类型'>
            <Radio.Group
              optionType='button'
              value={detailType}
              options={DETAIL_TYPE_OPTIONS}
              onChange={(event) => setDetailType(event.target.value)}
            />
          </div>
          <Input
            placeholder='互认医生ID'
            aria-label='互认医生筛选'
            allowClear
            style={{ width: 130 }}
            value={filters.recognitionDoctorId ?? ''}
            onChange={(event) => applyFilters({ recognitionDoctorId: event.target.value || undefined })}
          />
          <Input
            placeholder='不采纳原因代码'
            aria-label='不采纳原因筛选'
            allowClear
            style={{ width: 140 }}
            value={filters.nonAdoptionReasonCode ?? ''}
            onChange={(event) => applyFilters({ nonAdoptionReasonCode: event.target.value || undefined })}
          />
        </> : null}
        <Button
          aria-label='导出明细'
          loading={exporting}
          disabled={exporting}
          onClick={() => { void runExport('details') }}
        >导出明细</Button>
      </Space>
      {detailFailure === null ? null : <Alert
        type='warning'
        showIcon
        className='statistics-board-failure'
        title='明细数据待刷新'
        description='读取失败，已有数据保留；可重试读取。'
        action={<Button size='small' disabled={detailLoading} onClick={() => { void loadDetails(filters, detailType, detailPage) }}>重试</Button>}
      />}
      {side === 'usage' ? <Table<UsageDetailRow>
        rowKey='recognitionMatchItemId'
        size='small'
        columns={usageDetailCols}
        dataSource={detailRows as UsageDetailRow[]}
        loading={detailLoading && detailRows.length === 0}
        scroll={{ x: 2200 }}
        pagination={detailPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='当前筛选条件没有明细数据' /> }}
      /> : <Table<SourceDetailRow>
        rowKey='recognitionMatchItemId'
        size='small'
        columns={sourceDetailCols}
        dataSource={detailRows as SourceDetailRow[]}
        loading={detailLoading && detailRows.length === 0}
        scroll={{ x: 1500 }}
        pagination={detailPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='当前筛选条件没有明细数据' /> }}
      />}
    </Section>

    {recordState === null ? null : <MatchRecordModal state={recordState} onRetry={retryMatchRecord} onClose={() => setRecordState(null)} />}
  </div>
}
