/**
 * 四个统计页面（平台/本院 × 使用方/提供方）共享的页面主体（S6-D11、S6-D19；查询区与报告管理列表页同构）。
 *
 * 主体承载统计页的全部行为编排：筛选区、范围选择、汇总维度单选与汇总表、
 * 明细类型单选与明细表、导出入口与匹配记录弹窗；使用方与提供方的结构差异用 `side` 表达，
 * 平台/本院的范围形态差异用 `scopeSpec` 表达。数据源由四个页面注入（查询/导出入口以适配层为边界），
 * 主体不接触生成的 Kiota 类型，也不创建 API Client。
 *
 * 查询区同构（S6-D19）：统一关键字（占位按侧区分，提供方语句无医生列故不含互认医生）+ 项目类型下拉
 * + 预设日期区间选择器（今天/近 7 天/近 30 天，不可清空，默认近 30 天）+ 主色查询按钮；不设重置，
 * 关键字与项目类型可清空、清空后经「查询」生效。筛选控件改动只进草稿、零请求；点「查询」把草稿
 * 提交为已生效条件，一次提交同时重读汇总与明细，同条件再次提交等价刷新。明细条件由「已生效条件 +
 * 选中汇总行」派生：分组键用精确字段补充，用户关键字保持原值，派生不回写已生效条件，
 * 左侧汇总不因下钻收窄。汇总维度、明细类型是视图切换，保持即时生效、不进草稿；翻页基于已生效
 * 条件自动读取。本院页页头为左右结构（左侧标题区、右侧登录范围条），平台页页头不承载范围条。
 *
 * 服务端分页口径（同报告管理主体先例）：
 * 1. 分页由服务端完成，页面不排序、不做本地过滤与本地分页；
 * 2. 已生效条件、维度或明细类型变化回到第 1 页并清空旧结果（C9，保留页容量）；
 * 3. 当页为空且总数为正时按服务端总数回退到最后一页一次（C19 越界自愈）；
 * 4. 同一条件下只采纳最后一次请求结果，过期响应直接丢弃（C9）。
 *
 * 失败面（汇总读取、明细读取、导出、弹窗读取）都只做本层恢复（既定口径：纯读取失败不渲染横幅）：
 * 查询读取失败不渲染错误横幅——静默保留已有数据、结束加载态；错误由宿主统一提示，重试经查询按钮；
 * 导出与弹窗读取的既有恢复形态保留，不显示本地接口错误提示。
 */
import { SearchOutlined } from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import {
  Button,
  Card,
  DatePicker,
  Empty,
  Form,
  Input,
  Select,
  Tag,
  Typography,
  type TableColumnsType,
  type TablePaginationConfig,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { OrganizationScopeSelector, type OrganizationScopeChangeMeta, type OrganizationScopeValue } from '../../components/OrganizationScopeSelector'
import { CurrentScopeBar } from '../../components/CurrentScopeBar'
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
  STATISTICS_EXPORT_TYPE,
  USAGE_DETAIL_EXPORT_TYPE,
  USAGE_DETAIL_TYPE,
  type StatisticsPage,
  type StatisticsPageInput,
  type StatisticsGroupDimensionValue,
  type UsageDetailTypeValue,
} from './recognitionStatisticsValues'
import {
  buildStatisticsDatePresets,
  defaultStatisticsFilters,
  formatStatisticsAmount,
  formatStatisticsPercent,
  missingScopeNameCodes,
} from './statisticsDisplays'
import {
  DeptDoctorCell,
  FeedbackCell,
  ItemCell,
  PatientCell,
  PartyCell,
  TextCell,
  TimeCell,
  UsageDetailExpanded,
} from './statisticsDetailCells'
import { StatisticsDetailPanel, type StatisticsDetailRow } from './statisticsDetailPanel'
import {
  showFixedBranchColumn,
  statisticsDetailCardEmptyText,
  statisticsSummaryEmptyText,
  usageDetailTableEmptyText,
} from './statisticsDetailValues'
import { SourceStatisticsBoard } from './sourceStatisticsBoard'
import { StatisticsDetailToolbar } from './statisticsDetailToolbar'
import {
  USAGE_DETAIL_TAB_ITEMS,
  createDetailTabsIdPrefix,
  usageDetailPanelId,
  usageDetailTabId,
  usageDetailTypeLabel,
} from './statisticsDetailTabs'
import { StatisticsMetricSummary, type StatisticsMetricSummaryItem } from './statisticsMetricSummary'
import { StatisticsSummaryList } from './statisticsSummaryList'
import {
  summaryRowKey,
  summaryRowPrimary,
  summaryRowSecondary,
  usageSummaryAccessibleName,
  usageSummaryPrimary,
  usageSummaryRowKey,
  usageSummarySecondary,
  type StatisticsSide,
  type StatisticsSummaryRow,
} from './statisticsSummaryValues'
import './statisticsBoard.css'

export type { StatisticsSide, StatisticsSummaryRow } from './statisticsSummaryValues'
export type { StatisticsDetailRow } from './statisticsDetailPanel'

/**
 * 页面筛选状态的稳定模型：日期必填；范围字段按页面版本取用（平台页提交两组三级，
 * 本院页提交本侧院区与对侧组医院/院区）；统一关键字承载互认科室、互认项目与互认医生条件（S6-D19）；
 * 不采纳原因代码只经原因行下钻写入，仅在不采纳明细查询与明细导出生效。
 */
export interface StatisticsFilters {
  startTime: string
  endTime: string
  /**
   * 本院页本侧院区（使用方页为使用方院区，提供方页为提供方院区）：恒为登录院区，
   * 页面不提供院区选择；缺失属权限配置错误，页面零业务请求、以空态呈现（`missingScopeText`）；
   * 服务端全院统计契约仅为接口面保留（S6-D1），前端已不按其触发。
   */
  ownBranchCode?: string
  /**
   * 范围标识都是 IAM 组织业务编码（字段名沿用后端接口的既有名称），直接作为请求的 Code 字段提交；
   * 选择器内部的实例标识不进这里，只在选择器受控值里使用。
   * 名称字段只服务承接范围展示，随选择器选项一并进入已生效条件，不参与请求体。
   */
  receiverOrganizationCode?: string
  receiverOrganizationName?: string
  receiverHospitalCode?: string
  receiverHospitalName?: string
  receiverBranchCode?: string
  receiverBranchName?: string
  sourceOrganizationCode?: string
  sourceOrganizationName?: string
  sourceHospitalCode?: string
  sourceHospitalName?: string
  sourceBranchCode?: string
  sourceBranchName?: string
  /** 统一关键字：互认科室、互认项目与互认医生条件的单一承载（S6-D19），输入原文经适配层去首尾空白提交。 */
  keyword?: string
  itemType?: MedicalItemTypeValue
  nonAdoptionReasonCode?: string
  /**
   * 明细专用：选中汇总行的互认科室 ID。由主体按选中行填充，只进入明细读取与明细导出；
   * 汇总查询与汇总导出不读取这两个字段。页签、原因与分页变化都不改写它。
   */
  recognitionDeptId?: string
  /** 明细专用：选中汇总行的标准项目编码，口径同 `recognitionDeptId`。 */
  standardProjectCode?: string
}

/**
 * 范围选择区的形态：平台页两组三级可选；本院页不渲染固定侧卡片，对侧组医院/院区可选
 * （本侧组织、医院与院区恒为登录范围）。本院页变体携带可信组织与可信医院的业务编码、名称，
 * 以及可信组织实例标识（只用于给选择器固定组织），供列表区上下文行表达承接范围。
 */
export type StatisticsScopeSpec =
  | { kind: 'platform' }
  | {
      kind: 'branch'
      /** 本院入口的可信组织业务编码；作为本侧组织提交与承接范围展示。 */
      organizationCode: string
      /** 本院入口的可信组织实例标识：只交给选择器的固定组织属性。 */
      organizationId: string
      /** 本院入口的可信医院业务编码。 */
      hospitalCode: string
      organizationName?: string | null
      hospitalName?: string | null
      branchName?: string | null
    }

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
  /**
   * 本院页登录院区缺失（权限配置错误）时的空态文案：缺失期间汇总与明细两表以此为空态描述，
   * 业务请求经页面查询出口的适配层构造返回 `null` 阻断，主体零发请求；`undefined` 表示范围完整，
   * 按常规空态文案呈现。文案由页面注入（平台页不注入，本院两页共用同款）。
   */
  missingScopeText?: string
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
    detailFilters: StatisticsFilters,
    request: { exportType: number; groupDimension: number; detailType: number | null; downloadFileName: string },
  ) => Promise<void> | null
  /** 匹配记录查询出口；仅使用方注入（提供方明细不提供匹配记录入口，S6-D10）。 */
  queryMatchRecord?: (recognitionMatchRecordId: string) => Promise<MatchRecordView>
}

/** 汇总维度选项：固定页面文案（四个页面差异只取值来源与固定文案）；提供方不含互认科室维度（S6-D2）。 */
const USAGE_DIMENSION_OPTIONS: { value: StatisticsGroupDimensionValue; label: string }[] = [
  { value: 1, label: '医院' },
  { value: 2, label: '院区' },
  { value: 3, label: '互认科室' },
  { value: 4, label: '标准项目' },
]
const SOURCE_DIMENSION_OPTIONS: { value: StatisticsGroupDimensionValue; label: string }[] = [
  { value: 1, label: '提供方医院' },
  { value: 2, label: '提供方院区' },
  { value: 4, label: '标准项目' },
]

/** 不采纳明细类型值；原因筛选只在该类型生效。 */
const NON_ADOPTION_DETAIL_TYPE = USAGE_DETAIL_TYPE.nonAdoption

/** 项目类型筛选的兜底选项；取值与共享取值域一致，服务端枚举元数据可用时改取服务端声明。 */
const MEDICAL_ITEM_TYPE_FALLBACK: readonly EnumMetadataOption[] = [
  { value: 0, name: 'Laboratory', label: '检验' },
  { value: 1, name: 'Examination', label: '检查' },
]

/** 使用方指标摘要的四项类型数量：值取选中汇总行，类型标签复用明细页签的唯一来源。 */
function usageMetricCards(row: UsageSummaryRow): { type: UsageDetailTypeValue; label: string; count: number }[] {
  const counts: Record<UsageDetailTypeValue, number> = {
    [USAGE_DETAIL_TYPE.reminder]: row.reminderCount,
    [USAGE_DETAIL_TYPE.adoption]: row.adoptionCount,
    [USAGE_DETAIL_TYPE.nonAdoption]: row.nonAdoptionCount,
    [USAGE_DETAIL_TYPE.reference]: row.referenceCount,
  }
  return USAGE_DETAIL_TAB_ITEMS.map((item) => ({ type: item.value, label: item.label, count: counts[item.value] }))
}

/** 使用方选中行在给定明细类型下的条数；提供方只有被互认计数。 */
function usageDetailCount(row: UsageSummaryRow, detailType: UsageDetailTypeValue): number {
  switch (detailType) {
    case USAGE_DETAIL_TYPE.reminder: return row.reminderCount
    case USAGE_DETAIL_TYPE.adoption: return row.adoptionCount
    case USAGE_DETAIL_TYPE.nonAdoption: return row.nonAdoptionCount
    default: return row.referenceCount
  }
}

/** 区块容器：带可访问名称的区域，供用例与读屏定位。 */
function Section(props: { label: string; children: ReactNode }) {
  return <section aria-label={props.label}>{props.children}</section>
}

/**
 * 选中汇总行携带的精确分组字段：两侧字段名不同，先归一为同一形状，避免维度分支在使用方与提供方各写一遍。
 * 提供方没有互认科室维度，`recognitionDeptId` 固定为 `null`。
 */
interface SummaryGroupFields {
  organizationCode: string | null
  hospitalCode: string | null
  branchCode: string | null
  recognitionDeptId: string | null
  itemType: MedicalItemTypeValue | null
  standardProjectCode: string | null
}

/** 取选中汇总行的精确分组字段；数据源由页面按侧注入，行的实际类型与 `side` 一致。 */
function selectedSummaryGroupFields(side: StatisticsSide, row: StatisticsSummaryRow): SummaryGroupFields {
  if (side === 'usage') {
    const usage = row as UsageSummaryRow
    return {
      organizationCode: usage.receiverOrganizationCode,
      hospitalCode: usage.receiverHospitalCode,
      branchCode: usage.receiverBranchCode,
      recognitionDeptId: usage.recognitionDeptId,
      itemType: usage.itemType,
      standardProjectCode: usage.standardProjectCode,
    }
  }
  const source = row as SourceSummaryRow
  return {
    organizationCode: source.sourceOrganizationCode,
    hospitalCode: source.sourceHospitalCode,
    branchCode: source.sourceBranchCode,
    recognitionDeptId: null,
    itemType: source.itemType,
    standardProjectCode: source.standardProjectCode,
  }
}

/**
 * 按选中汇总行派生明细条件：以已生效筛选为底，按维度把该行的精确分组字段补充进来，
 * 范围字段只在对应维度取行值，其余沿用已生效筛选；用户关键字保持原值。
 *
 * 派生结果只进入明细读取与明细导出，不回写已生效筛选，因此左侧汇总不会因下钻而收窄。
 * 明细专用字段每轮先移除，避免跨维度、跨行残留。
 */
function deriveDetailFilters(
  side: StatisticsSide,
  dimension: number,
  filters: StatisticsFilters,
  row: StatisticsSummaryRow,
): StatisticsFilters {
  const derived: StatisticsFilters = { ...filters }
  delete derived.recognitionDeptId
  delete derived.standardProjectCode
  delete derived.nonAdoptionReasonCode

  const group = selectedSummaryGroupFields(side, row)

  if (dimension === 4) {
    // 标准项目维度用项目类型与标准项目编码收窄，范围沿用已生效筛选。
    if (group.itemType !== null) derived.itemType = group.itemType
    derived.standardProjectCode = group.standardProjectCode ?? undefined
    return derived
  }

  // 医院/院区维度（以及使用方互认科室维度）取选中行的范围字段；提供方没有互认科室维度。
  const isUsageDeptDimension = side === 'usage' && dimension === 3
  const includesBranch = dimension === 2 || isUsageDeptDimension
  if (dimension !== 1 && !includesBranch) return derived
  if (side === 'usage') {
    derived.receiverOrganizationCode = group.organizationCode ?? undefined
    derived.receiverHospitalCode = group.hospitalCode ?? undefined
    if (includesBranch) derived.receiverBranchCode = group.branchCode ?? undefined
    if (isUsageDeptDimension) derived.recognitionDeptId = group.recognitionDeptId ?? undefined
    return derived
  }
  derived.sourceOrganizationCode = group.organizationCode ?? undefined
  derived.sourceHospitalCode = group.hospitalCode ?? undefined
  if (includesBranch) derived.sourceBranchCode = group.branchCode ?? undefined
  return derived
}

/** 两个筛选条件的取值是否一致（忽略值为 `undefined` 的键）：决定一次提交走「条件变化复位」还是「同条件重读」。 */
function sameFilters(left: StatisticsFilters, right: StatisticsFilters): boolean {
  const keys = new Set([...Object.keys(left), ...Object.keys(right)] as (keyof StatisticsFilters)[])
  for (const key of keys) {
    if (left[key] === undefined && right[key] === undefined) continue
    if (left[key] !== right[key]) return false
  }
  return true
}

/** 空页自愈守卫的条件片段：忽略 `undefined` 并按字段名排序，避免对象键顺序造成守卫误判。 */
function filtersGuardKey(filters: StatisticsFilters): string {
  return JSON.stringify(
    Object.entries(filters)
      .filter(([, value]) => value !== undefined)
      .sort(([left], [right]) => left.localeCompare(right)),
  )
}

/**
 * 使用方明细列：固定侧为使用方。
 * 医院维度且平台页未锁院区时保留「使用方院区」；其他维度或本院页已锁院区时不再重复固定侧。
 */
function usageDetailColumns(
  onOpenRecord: (recognitionMatchRecordId: string) => void,
  dimension: number,
  scopeKind: StatisticsScopeSpec['kind'],
  fixedBranchLocked: boolean,
): TableColumnsType<UsageDetailRow> {
  const columns: TableColumnsType<UsageDetailRow> = [
    { title: '时间', width: 130, render: (_, row) => <TimeCell businessTime={row.businessTime} matchCreatedTime={row.matchCreatedTime} /> },
    { title: '患者', width: 120, render: (_, row) => <PatientCell patientName={row.patientName} identityDocumentNo={row.identityDocumentNo} /> },
    { title: '项目类型', width: 80, render: (_, row) => <Tag color='blue'>{row.item.itemTypeText || '—'}</Tag> },
    { title: '项目', width: 140, render: (_, row) => <ItemCell item={row.item} /> },
    // 对侧使用三行式；固定侧只在医院维度保留院区字段。
    { title: '提供方', width: 150, render: (_, row) => <PartyCell {...row.source} /> },
  ]
  if (showFixedBranchColumn(dimension, scopeKind, fixedBranchLocked)) {
    columns.push({ title: '使用方院区', width: 96, render: (_, row) => <TextCell value={row.receiver.branchName} code={row.receiver.branchCode} /> })
  }
  columns.push(
    { title: '科室 / 医生', width: 110, render: (_, row) => <DeptDoctorCell deptName={row.recognitionDeptName} doctorName={row.recognitionDoctorName} /> },
    { title: '反馈结果', width: 112, render: (_, row) => <FeedbackCell row={row} /> },
    {
      title: '操作',
      width: 88,
      render: (_, row) => <Button
        type='link'
        size='small'
        aria-label='查看互认匹配记录'
        title='查看互认匹配记录'
        onClick={() => onOpenRecord(row.recognitionMatchRecordId)}
      >匹配记录</Button>,
    },
  )
  return columns
}

/**
 * 列表空页自愈（C19）：当页为空且总数为正时按服务端总数回退最后一页一次；
 * 回退已发生过（守卫键相同）时不再重复触发，避免空页反复重取。守卫键包含范围、条件、
 * 维度、选中与明细类型，任一变化后允许重新自愈一次。
 */
function useEmptyPageHeal(args: {
  loading: boolean
  failure: string | null
  rowCount: number
  totalCount: number
  pageIndex: number
  pageSize: number
  guardKey: string
  guardRef: { current: string | null }
  retake: () => void
}) {
  const { loading, failure, rowCount, totalCount, pageIndex, pageSize, guardKey, guardRef, retake } = args
  useEffect(() => {
    if (loading || failure !== null) return
    if (rowCount > 0 || totalCount <= 0) return
    // 守卫键按换算后的末页记录：回退动作（含 setPage 与其引发的重读）对同一条件只执行一次，
    // 不因回退后页码变化而重新放行。
    const lastPage = Math.max(1, Math.ceil(totalCount / pageSize))
    const guard = `${guardKey}|${lastPage}|${pageSize}|${totalCount}`
    if (guardRef.current === guard) return
    guardRef.current = guard
    // 推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(retake, 0)
    return () => window.clearTimeout(task)
  }, [failure, guardKey, guardRef, loading, pageIndex, pageSize, retake, rowCount, totalCount])
}

/** 明细表分页配置：在 Table 分页字段之外携带普通分页控件的可访问名称。 */
type DetailPaginationConfig = TablePaginationConfig & { 'aria-label': string }

export function StatisticsBoard(props: StatisticsBoardProps) {
  const {
    title,
    subtitle,
    side,
    scopeKey,
    scopeSpec,
    initialFilters,
    missingScopeText,
    querySummaryPage,
    queryDetailsPage,
    exportWorkbook,
    queryMatchRecord,
  } = props

  // ===== 会话状态 =====
  const [filters, setFilters] = useState<StatisticsFilters>(initialFilters)
  /**
   * 提交式筛选（D-02、S6-D19）：草稿承载未提交的筛选与范围改动，已生效条件在 `filters` 里驱动
   * 汇总与明细读取。控件 onChange 只写草稿；「查询」把草稿提交为已生效条件，一次提交经各自的
   * mark 复位同时重读两个区；不设重置，关键字与项目类型可清空、清空后经「查询」生效。
   */
  const [draftFilters, setDraftFilters] = useState<StatisticsFilters>(initialFilters)
  const [dimension, setDimension] = useState<StatisticsGroupDimensionValue>(1)
  const [detailType, setDetailType] = useState<UsageDetailTypeValue>(1)
  /** 不采纳原因筛选：只在不采纳明细生效，取值来自选中汇总行的原因分布。 */
  const [reasonCode, setReasonCode] = useState<string | null>(null)
  const [summaryRows, setSummaryRows] = useState<StatisticsSummaryRow[]>([])
  const [summaryTotal, setSummaryTotal] = useState(0)
  const [summaryPage, setSummaryPage] = useState<StatisticsPageInput>({ pageIndex: 1, pageSize: 8 })
  /** 左列表选中键；汇总重读后按稳定键保留，不存在时自动落到当前页首行。 */
  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [summaryLoading, setSummaryLoading] = useState(false)
  const [summaryFailure, setSummaryFailure] = useState<null | 'load'>(null)
  const [detailRows, setDetailRows] = useState<StatisticsDetailRow[]>([])
  const [detailTotal, setDetailTotal] = useState(0)
  const [detailPage, setDetailPage] = useState<StatisticsPageInput>({ pageIndex: 1, pageSize: 10 })
  const [detailLoading, setDetailLoading] = useState(false)
  const [detailFailure, setDetailFailure] = useState<null | 'load'>(null)
  const [exporting, setExporting] = useState(false)
  const [recordState, setRecordState] = useState<MatchRecordDialogState | null>(null)
  /** 明细页签/面板 id 的实例前缀：每次挂载生成全局唯一值（跨独立 React root 也不重复），并贯穿页签、面板与焦点定位。 */
  const [detailTabsIdPrefix] = useState(createDetailTabsIdPrefix)

  /**
   * 在途读取的作废机制：按序号决定谁写入状态，不取消在途请求（生成客户端无取消入口，
   * 同报告管理主体先例）。复位与筛选变化经条件/页对象引用变化触发读取入口重新执行，
   * 读取入口在发起前递增序号，旧请求跑完后因序号不符而被丢弃。
   */
  const summarySeqRef = useRef(0)
  const detailSeqRef = useRef(0)
  const summaryHealGuardRef = useRef<string | null>(null)
  const detailHealGuardRef = useRef<string | null>(null)
  /**
   * 同条件重读推进号（与金额页先例同一手法）：条件、维度、明细类型与页对象引用都不变时
   * （同条件再次提交，等价于刷新），靠它让读取效果重新执行；此时不清空旧结果，失败也不覆盖。
   */
  const [reloadNonce, setReloadNonce] = useState(0)
  const boardRef = useRef<HTMLDivElement | null>(null)
  const userSelectedRef = useRef(false)

  // 条件/范围/页码变化先作废在途响应；读取效果随后才发起新请求。
  useEffect(() => {
    summarySeqRef.current += 1
    detailSeqRef.current += 1
  }, [dimension, filters, scopeKey, summaryPage])

  useEffect(() => {
    detailSeqRef.current += 1
  }, [detailPage, detailType, reasonCode, selectedKey])

  // ===== 渲染期复位 =====

  /**
   * 范围切换在渲染期复位：旧范围的条件、数据与弹窗先于任何请求被丢弃（同报告管理主体先例）。
   * 复位后条件状态的 `scopeMark` 恒等于当前范围键，晚到的旧范围响应既不改写数据也不改写失败态。
   */
  const [scopeMark, setScopeMark] = useState(scopeKey)
  if (scopeMark !== scopeKey) {
    setScopeMark(scopeKey)
    // 复位条件含按复位时刻重算的默认近 30 天窗（与报告管理页跨天语义同构）：
    // 新窗口覆盖日期两字段，initialFilters 其余字段（如本院页 ownBranchCode 注入）保留。
    const resetFilters = { ...initialFilters, ...defaultStatisticsFilters() }
    setFilters(resetFilters)
    setDraftFilters({ ...resetFilters })
    setDimension(1)
    setDetailType(USAGE_DETAIL_TYPE.reminder)
    setReasonCode(null)
    setSummaryRows([])
    setSummaryTotal(0)
    setSelectedKey(null)
    setSummaryPage({ pageIndex: 1, pageSize: summaryPage.pageSize })
    setSummaryFailure(null)
    setDetailRows([])
    setDetailTotal(0)
    setDetailPage({ pageIndex: 1, pageSize: detailPage.pageSize })
    setDetailFailure(null)
    setDetailLoading(false)
    setRecordState(null)
  }

  // 筛选/维度变化：汇总回到第 1 页并清空旧结果（C9，保留页容量），明细类型回提醒。
  const [summaryMark, setSummaryMark] = useState({ filters, dimension })
  if (summaryMark.filters !== filters || summaryMark.dimension !== dimension) {
    setSummaryMark({ filters, dimension })
    setSummaryPage((previous) => ({ ...previous, pageIndex: 1 }))
    setSummaryRows([])
    setSummaryTotal(0)
    setSelectedKey(null)
    setSummaryFailure(null)
    setDetailType(USAGE_DETAIL_TYPE.reminder)
    setReasonCode(null)
  }
  /**
   * 筛选或维度变化：明细回到第 1 页并清空旧结果。
   * 明细类型变化只重置页码、失败态与原因，保留上一组行作为加载期间的占位，
   * 避免切换页签时表格区域先塌缩再恢复。
   */
  const [detailMark, setDetailMark] = useState({ filters, dimension, detailType })
  if (detailMark.filters !== filters || detailMark.dimension !== dimension) {
    setDetailMark({ filters, dimension, detailType })
    setDetailPage((previous) => ({ ...previous, pageIndex: 1 }))
    setDetailRows([])
    setDetailTotal(0)
    setDetailFailure(null)
    setDetailLoading(false)
    setReasonCode(null)
  } else if (detailMark.detailType !== detailType) {
    setDetailMark({ filters, dimension, detailType })
    setDetailPage((previous) => ({ ...previous, pageIndex: 1 }))
    setDetailFailure(null)
    // 原因只在原页签内保留，页签变化即清空。
    setReasonCode(null)
  }
  /**
   * 选中行变化：明细类型回提醒、明细页回到第一页并清空旧明细。
   * 以渲染期复位表达，避免 effect 级联；同一行重复点击走重试路径，不经过这里。
   */
  const [selectionMark, setSelectionMark] = useState<string | null>(selectedKey)
  if (selectionMark !== selectedKey) {
    setSelectionMark(selectedKey)
    setDetailType(USAGE_DETAIL_TYPE.reminder)
    setReasonCode(null)
    setDetailPage((previous) => ({ ...previous, pageIndex: 1 }))
    setDetailRows([])
    setDetailTotal(0)
    setDetailFailure(null)
    setDetailLoading(false)
  }
  /**
   * 原因变化：右页回 1 并清空旧明细；原因只影响明细读取，左侧汇总条件不变。
   * 与页签、选行共用渲染期复位，避免 effect 级联。
   */
  const [reasonMark, setReasonMark] = useState<string | null>(reasonCode)
  if (reasonMark !== reasonCode) {
    setReasonMark(reasonCode)
    setDetailPage((previous) => ({ ...previous, pageIndex: 1 }))
    setDetailRows([])
    setDetailTotal(0)
    setDetailFailure(null)
  }

  // ===== 明细条件派生 =====

  /** 当前选中行：键由维度相关字段合成，汇总重读后仍按稳定键定位到新列表里的同一行。 */
  const selectedSummaryRow = useMemo(() => {
    if (selectedKey === null) return null
    return summaryRows.find((row) => summaryRowKey(side, row) === selectedKey) ?? null
  }, [selectedKey, side, summaryRows])
  /** 明细条件：已生效筛选 + 选中行的精确分组字段；未选中时不发请求，此值与筛选同形。 */
  const derivedDetailFilters = useMemo(
    () => (selectedSummaryRow === null
      ? filters
      : deriveDetailFilters(side, dimension, filters, selectedSummaryRow)),
    [dimension, filters, selectedSummaryRow, side],
  )
  /**
   * 不采纳原因只在不采纳明细（类型 3）随明细条件提交；其他类型与未选择原因时不携带该字段。
   * 原因筛选只改变明细读取，汇总条件与左侧列表保持不变。
   */
  const reasonDetailFilters = useMemo(
    () => (side === 'usage' && detailType === NON_ADOPTION_DETAIL_TYPE && reasonCode !== null
      ? { ...derivedDetailFilters, nonAdoptionReasonCode: reasonCode }
      : derivedDetailFilters),
    [derivedDetailFilters, detailType, reasonCode, side],
  )
  /**
   * 明细条件的取值比较：汇总重读会换新行对象但分组取值不变，明细读取只按取值变化触发。
   * 用字段相等判断代替序列化签名，避免键顺序变化影响读取触发。
   */
  const [detailCondition, setDetailCondition] = useState(reasonDetailFilters)
  if (!sameFilters(detailCondition, reasonDetailFilters)) {
    setDetailCondition(reasonDetailFilters)
  }
  const detailFilters = detailCondition

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
      setSelectedKey((previous) => {
        const keys = page.items.map((item) => summaryRowKey(side, item))
        if (previous !== null && keys.includes(previous)) return previous
        return keys[0] ?? null
      })
      setSummaryFailure(null)
    } catch {
      if (requestId !== summarySeqRef.current) return
      // 读取失败保留已有数据，只进入待刷新状态；不把失败结果覆盖成空列表，也不本地报错。
      setSummaryFailure('load')
    } finally {
      if (requestId === summarySeqRef.current) setSummaryLoading(false)
    }
  }, [querySummaryPage, side])

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

  // 条件、维度、明细类型、页码或同条件重读推进号变化时重新读取当页（自动加载入口）。
  useEffect(() => {
    const task = window.setTimeout(() => { void loadSummary(filters, dimension, summaryPage) }, 0)
    return () => {
      window.clearTimeout(task)
      summarySeqRef.current += 1
    }
  }, [dimension, filters, loadSummary, reloadNonce, summaryPage])

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
    void loadDetails(detailFilters, detailType, detailPage)
  }, [detailFilters, detailPage, detailTotal, detailType, loadDetails])

  /** 空页自愈守卫键：条件、维度、选中与明细类型任一变化后允许重新回退末页一次。 */
  const summaryHealGuardKey = `${scopeKey}|${filtersGuardKey(filters)}|${dimension}|${selectedKey ?? ''}|${detailType}`
  const detailHealGuardKey = `${scopeKey}|${filtersGuardKey(detailFilters)}|${dimension}|${selectedKey ?? ''}|${detailType}`

  useEmptyPageHeal({
    loading: summaryLoading,
    failure: summaryFailure,
    rowCount: summaryRows.length,
    totalCount: summaryTotal,
    pageIndex: summaryPage.pageIndex,
    pageSize: summaryPage.pageSize,
    guardKey: summaryHealGuardKey,
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
    guardKey: detailHealGuardKey,
    guardRef: detailHealGuardRef,
    retake: retakeDetails,
  })

  // ===== 交互 =====

  /** 草稿条件变化：只改草稿，不发请求、不动分页（D-02）。 */
  const applyDraft = useCallback((patch: Partial<StatisticsFilters>) => {
    setDraftFilters((previous) => ({ ...previous, ...patch }))
  }, [])

  /**
   * 提交：把给定条件同时写为草稿与已生效条件。条件取值相对已生效条件有变化时换新对象，
   * 经 mark 复位回第 1 页并清空旧结果；取值未变化（同条件再次提交）时保留旧条件引用与旧结果，
   * 只推进重读号再取一次——失败也不覆盖已有数据（C18）。两种路径都保证汇总与明细各重读一次。
   */
  const submitFilters = useCallback((next: StatisticsFilters) => {
    setDraftFilters(next)
    setFilters((previous) => (sameFilters(previous, next) ? previous : { ...next }))
    setReloadNonce((nonce) => nonce + 1)
  }, [])

  /** 查询：把当前草稿提交为已生效条件（inline Form 的提交入口）。 */
  const submitDraft = useCallback(() => {
    submitFilters({ ...draftFilters })
  }, [draftFilters, submitFilters])

  /** 范围选项名称：只用于承接范围展示；选项缺失时返回 undefined，展示层显示「未知」。 */
  const scopeName = useCallback((options: { id: string; name: string }[], id: string | undefined) =>
    id === undefined ? undefined : options.find((option) => option.id === id)?.name, [])

  /**
   * 平台页接收组/来源组三级范围选择（改动进草稿）：范围字段写业务编码（请求的 Code 字段），
   * 名称随选项一并保存用于承接范围展示；选择器内部的实例标识不进草稿。
   */
  const changeReceiverScope = useCallback((value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => applyDraft({
    receiverOrganizationCode: value.orgCode,
    receiverOrganizationName: scopeName(meta.organizations, value.orgId),
    receiverHospitalCode: value.hosCode,
    receiverHospitalName: scopeName(meta.hospitals, value.hosId),
    receiverBranchCode: value.branchCode,
    receiverBranchName: scopeName(meta.branches, value.branchId),
  }), [applyDraft, scopeName])
  const changeSourceScope = useCallback((value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => applyDraft({
    sourceOrganizationCode: value.orgCode,
    sourceOrganizationName: scopeName(meta.organizations, value.orgId),
    sourceHospitalCode: value.hosCode,
    sourceHospitalName: scopeName(meta.hospitals, value.hosId),
    sourceBranchCode: value.branchCode,
    sourceBranchName: scopeName(meta.branches, value.branchId),
  }), [applyDraft, scopeName])

  /** 本院页对侧组范围选择：对侧组织固定为可信组织，只提交医院与院区的业务编码（S6-D1；改动进草稿）。 */
  const changeOppositeScope = useCallback((value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => {
    const hospitalName = scopeName(meta.hospitals, value.hosId)
    const branchName = scopeName(meta.branches, value.branchId)
    if (side === 'usage') applyDraft({
      sourceHospitalCode: value.hosCode,
      sourceHospitalName: hospitalName,
      sourceBranchCode: value.branchCode,
      sourceBranchName: branchName,
    })
    else applyDraft({
      receiverHospitalCode: value.hosCode,
      receiverHospitalName: hospitalName,
      receiverBranchCode: value.branchCode,
      receiverBranchName: branchName,
    })
  }, [applyDraft, scopeName, side])

  /** 左列表选行：切到另一行时只更新选中键（复位由选中标记承担）；重复点击当前行重试当前明细。 */
  const selectSummaryRow = useCallback((rowKey: string) => {
    if (rowKey === selectedKey) {
      void loadDetails(detailFilters, detailType, detailPage)
      return
    }
    userSelectedRef.current = true
    setSelectedKey(rowKey)
  }, [detailFilters, detailPage, detailType, loadDetails, selectedKey])

  /** 导出：沿用当前查询条件（不带分页）与下载名构造；导出中禁用重复提交（C15）。 */
  const runExport = useCallback(async (kind: 'summary' | 'details') => {
    if (exporting) return
    // 时间区间不可清空（allowClear={false}），此守卫为契约面防御：缺日期时条件不完整，
    // 不构造导出请求，也不构造下载名（查询出口返回 null，主体零发请求）。
    if (toStatisticsRequestDate(filters.startTime) === null || toStatisticsRequestDate(filters.endTime) === null) return
    const exportType = kind === 'summary'
      ? (side === 'usage' ? STATISTICS_EXPORT_TYPE.usageSummary : STATISTICS_EXPORT_TYPE.sourceSummary)
      : (side === 'usage' ? USAGE_DETAIL_EXPORT_TYPE[detailType] : STATISTICS_EXPORT_TYPE.sourceDetail)
    // 明细导出必须带选中行派生条件、当前类型与原因；汇总导出只带已生效筛选与维度。
    if (kind === 'details' && selectedKey === null) return
    const task = exportWorkbook(filters, reasonDetailFilters, {
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
  }, [detailType, dimension, exportWorkbook, exporting, filters, reasonDetailFilters, selectedKey, side])

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
  // 时间预设按挂载当天的日历构造（S6-D19：今天/近 7 天/近 30 天），引用稳定、不随读取重算。
  const datePresets = useMemo(() => buildStatisticsDatePresets(dayjs()), [])
  /** 草稿时间区间的受控值：来自草稿的年月日文本（区间不可清空，两端恒有值）。 */
  const draftRangeValue: [Dayjs, Dayjs] = [dayjs(draftFilters.startTime), dayjs(draftFilters.endTime)]
  const dimensionOptions = side === 'usage' ? USAGE_DIMENSION_OPTIONS : SOURCE_DIMENSION_OPTIONS
  // 本院页只保留对侧机构筛选：使用方页筛提供方，提供方页筛使用方；固定侧由页头范围条表达。
  const oppositeScopeLabel = side === 'usage' ? '提供方筛选' : '使用方筛选'
  // 固定组织必须写入受控值，否则范围选择器的医院与院区会因缺少上级组织保持禁用：
  // 组织层用可信组织的实例标识（选择器受控值），医院与院区层用草稿里的业务编码（选择器按编码定位）。
  const oppositeScopeValue = side === 'usage'
    ? {
        orgId: scopeSpec.kind === 'branch' ? scopeSpec.organizationId : undefined,
        hosCode: draftFilters.sourceHospitalCode,
        branchCode: draftFilters.sourceBranchCode,
      }
    : {
        orgId: scopeSpec.kind === 'branch' ? scopeSpec.organizationId : undefined,
        hosCode: draftFilters.receiverHospitalCode,
        branchCode: draftFilters.receiverBranchCode,
      }

  /** `as` 收窄依据的主体不变量：数据源由页面按侧注入，行的实际类型与 `side` 一致。 */
  const selectedUsageRow = side === 'usage' && selectedSummaryRow !== null
    ? selectedSummaryRow as UsageSummaryRow
    : null
  const selectedPrimary = selectedSummaryRow === null
    ? ''
    : summaryRowPrimary(side, selectedSummaryRow, dimension)
  const selectedSecondary = selectedSummaryRow === null
    ? ''
    : summaryRowSecondary(side, selectedSummaryRow, dimension)
  // 明细只跟随选中行读取：未选中时不发请求，右卡呈现空态（C44）。
  // 条件取派生后的精确分组字段；页签与页码变化按各自依赖重读。
  useEffect(() => {
    if (selectedKey === null) return undefined
    const effectSeq = detailSeqRef.current
    // 推迟到微任务，避免在 effect 内同步写入加载态；条件已变化时不再发起旧读取。
    void Promise.resolve().then(() => {
      if (effectSeq !== detailSeqRef.current) return
      void loadDetails(detailFilters, detailType, detailPage)
    })
    return () => {
      // 依赖变化先作废当前明细序号，旧响应不能写进新条件。
      detailSeqRef.current += 1
    }
  }, [detailFilters, detailPage, detailType, loadDetails, reloadNonce, selectedKey])
  const detailTypeText = side === 'source' ? '被互认' : usageDetailTypeLabel(detailType)
  const summaryMetrics: StatisticsMetricSummaryItem[] = selectedUsageRow !== null
    ? [
        ...usageMetricCards(selectedUsageRow).map((item) => ({ label: item.label, value: String(item.count) })),
        { label: '同期互认率', value: formatStatisticsPercent(selectedUsageRow.samePeriodRecognitionRate) },
        { label: '预计节省金额', value: formatStatisticsAmount(selectedUsageRow.estimatedSavingAmount), compact: true },
      ]
    : []
  /**
   * 不采纳原因选项只取选中汇总行的原因分布，不额外请求；行内没有原因时隐藏原因筛选。
   * 原因代码只在不采纳明细提交（见 `reasonDetailFilters`）。
   */
  const reasonOptions = selectedUsageRow === null
    ? []
    : [
        { value: '', label: `全部原因（${selectedUsageRow.nonAdoptionCount} 次）` },
        ...selectedUsageRow.nonAdoptionReasons.map((reason) => ({
          value: reason.reasonCode,
          label: `${reason.reasonName}（${reason.count} 次，${formatStatisticsPercent(reason.ratio)}）`,
        })),
      ]
  const showReasonFilter = side === 'usage'
    && detailType === NON_ADOPTION_DETAIL_TYPE
    && selectedUsageRow !== null
    && selectedUsageRow.nonAdoptionReasons.length > 0
  const selectedReasonName = reasonCode === null
    ? null
    : selectedUsageRow?.nonAdoptionReasons.find((reason) => reason.reasonCode === reasonCode)?.reasonName ?? null

  // 固定侧院区已被查询条件锁定时，不再需要在明细表重复显示该院区。
  const usageBranchLocked = (filters.receiverBranchCode ?? '').trim().length > 0
  const sourceBranchLocked = (filters.sourceBranchCode ?? '').trim().length > 0
  const usageDetailCols = useMemo(
    () => usageDetailColumns(openMatchRecord, dimension, scopeSpec.kind, usageBranchLocked),
    [dimension, openMatchRecord, scopeSpec.kind, usageBranchLocked],
  )
  // 提供方明细的固定侧院区列按同一规则求值，由提供方列表区主体据此装配列。
  const showSourceBranchColumn = showFixedBranchColumn(dimension, scopeSpec.kind, sourceBranchLocked)
  /**
   * 提供方列表区上下文行的承接范围取值：平台页取范围区所选的组织、医院与院区（仅院区维度带院区）；
   * 本院页取可信组织、可信医院与登录院区（始终带院区）。取值来自已生效筛选，不读汇总行字段。
   */
  const sourceScopeValues = scopeSpec.kind === 'branch'
    ? {
        organizationCode: scopeSpec.organizationCode,
        organizationName: scopeSpec.organizationName,
        hospitalCode: scopeSpec.hospitalCode,
        hospitalName: scopeSpec.hospitalName,
        branchCode: filters.ownBranchCode,
        branchName: (filters.ownBranchCode ?? '').trim().length > 0 ? scopeSpec.branchName : undefined,
      }
    : {
        organizationCode: filters.sourceOrganizationCode,
        organizationName: filters.sourceOrganizationName,
        hospitalCode: filters.sourceHospitalCode,
        hospitalName: filters.sourceHospitalName,
        branchCode: dimension === 2 ? filters.sourceBranchCode : undefined,
        branchName: dimension === 2 && (filters.sourceBranchCode ?? '').trim().length > 0
          ? filters.sourceBranchName
          : undefined,
      }

  /** 左列表换页：换页清空选中，新页读取完成后由汇总读取自动选中首行。 */
  const changeSummaryPage = (nextPageIndex: number) => {
    setSelectedKey(null)
    setSummaryPage({ pageIndex: nextPageIndex, pageSize: 8 })
  }
  const detailPagination: DetailPaginationConfig = {
    ...createTablePagination({
      pageIndex: detailPage.pageIndex,
      pageSize: 10,
      totalCount: detailTotal,
      loading: detailLoading,
      pageSizeOptions: [10],
      showSizeChanger: false,
      onChange: (nextPageIndex) => setDetailPage({ pageIndex: nextPageIndex, pageSize: 10 }),
    }),
    'aria-label': '明细列表分页',
  }

  const selectedContextText = selectedSummaryRow === null
    ? ''
    : `统计时间 ${filters.startTime} 至 ${filters.endTime} · 承接范围 ${[selectedPrimary, selectedSecondary].filter((part) => part.length > 0).join(' · ')} · 共 ${detailTotal} 条${reasonCode === null ? '' : ` · 原因：${selectedReasonName ?? reasonCode}`}`
  const liveDetailCount = selectedSummaryRow === null
    ? 0
    : side === 'source'
      ? (selectedSummaryRow as SourceSummaryRow).recognitionCount
      : usageDetailCount(selectedSummaryRow as UsageSummaryRow, detailType)
  const summaryEmptyText = statisticsSummaryEmptyText(summaryFailure !== null, missingScopeText)
  const detailEmptyText = statisticsDetailCardEmptyText(summaryFailure !== null)
  const detailTableEmptyText = usageDetailTableEmptyText(detailFailure !== null, detailRows.length, missingScopeText)

  // 明细面板主体：只渲染表格与行展开；原因筛选归入上方工具行。
  const renderDetailPanel = () => <StatisticsDetailPanel<UsageDetailRow>
    missingNameCodes={missingScopeNameCodes((detailRows as UsageDetailRow[]).flatMap((row) => [
      { code: row.source.organizationCode, name: row.source.organizationName },
      { code: row.source.hospitalCode, name: row.source.hospitalName },
      { code: row.source.branchCode, name: row.source.branchName },
      { code: row.receiver.organizationCode, name: row.receiver.organizationName },
      { code: row.receiver.hospitalCode, name: row.receiver.hospitalName },
      { code: row.receiver.branchCode, name: row.receiver.branchName },
    ]))}
    rows={detailRows as UsageDetailRow[]}
    columns={usageDetailCols}
    loading={detailLoading}
    pagination={detailPagination}
    emptyText={detailTableEmptyText}
    expandable={{
      columnWidth: 36,
      expandedRowRender: (row) => <UsageDetailExpanded row={row} />,
    }}
  />

  // 容器宽度进入单列后，选中行把明细卡滚入视口；减少动态偏好下用即时滚动。
  // 明细卡位于各侧列表区主体内部，按锚点类名取本实例内的那一张，避免把 ref 穿过组件边界。
  useEffect(() => {
    if (selectedKey === null || !userSelectedRef.current) return
    const board = boardRef.current
    const card = board === null ? null : board.querySelector<HTMLElement>('.statistics-board-detail-anchor')
    // 只有容器进入单列（<1200px）时才需要把明细卡滚入视口；两列并排时不跳动。
    if (board === null || card === null || board.getBoundingClientRect().width >= 1200) return
    if (typeof card.scrollIntoView !== 'function') return
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    card.scrollIntoView({ block: 'start', behavior: reduced ? 'auto' : 'smooth' })
    userSelectedRef.current = false
  }, [selectedKey])

  return <div className='statistics-board' ref={boardRef}>
    <div className='statistics-board-live statistics-board-sr-only' role='status' aria-live='polite'>
      {selectedSummaryRow === null
        ? '未选择汇总行'
        : side === 'usage' && selectedUsageRow !== null
          ? `已选择${selectedPrimary}，提醒 ${selectedUsageRow.reminderCount} 条，采纳 ${selectedUsageRow.adoptionCount} 条，当前 ${detailTypeText} ${liveDetailCount} 条，明细共 ${detailTotal} 条`
          : `已选择${selectedPrimary}，被互认 ${liveDetailCount} 条，明细共 ${detailTotal} 条`}
    </div>
    <div className='statistics-board-header'>
      <div>
        {/* 页面大标题层级与 margin 口径全站统一（标题层级口径：大标题与阻断态标题同层级），margin 归零由配套 CSS 承载。 */}
        <Typography.Title level={3}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      {/* 本院页页头左右结构（S6-D19，与本院报告管理页同构）：登录范围条承载在页头右侧；平台页页头不承载范围条。 */}
      {scopeSpec.kind === 'branch' ? <CurrentScopeBar /> : null}
    </div>

    {/* 范围区（S6-D23）：平台页保留使用方与提供方左右并排、中间方向列；
        本院页不渲染固定侧卡片与方向列，只保留一行对侧机构筛选，固定侧由页头范围条表达。
        两页改动都进草稿，点「查询」生效。 */}
    <div className='statistics-board-scope'>
      {scopeSpec.kind === 'platform'
        ? <div className='statistics-board-duo'>
            <section className='statistics-board-party statistics-board-party--using' aria-label='使用方范围'>
              <div className='statistics-board-party__head'>
                <span className='statistics-board-party__name'>使用方</span>
                <span className='statistics-board-party__desc'>调阅并使用他院报告</span>
              </div>
              <div className='statistics-board-party__body'>
                <OrganizationScopeSelector
                  value={{
                    orgCode: draftFilters.receiverOrganizationCode,
                    hosCode: draftFilters.receiverHospitalCode,
                    branchCode: draftFilters.receiverBranchCode,
                  }}
                  onChange={changeReceiverScope}
                />
              </div>
            </section>

            <div className='statistics-board-flow' role='img' aria-label='报告流向：提供方 → 使用方'>
              <span className='statistics-board-flow__arrow' aria-hidden='true'>←</span>
            </div>

            <section className='statistics-board-party statistics-board-party--providing' aria-label='提供方范围'>
              <div className='statistics-board-party__head'>
                <span className='statistics-board-party__name'>提供方</span>
                <span className='statistics-board-party__desc'>出具并提供报告</span>
              </div>
              <div className='statistics-board-party__body'>
                <OrganizationScopeSelector
                  value={{
                    orgCode: draftFilters.sourceOrganizationCode,
                    hosCode: draftFilters.sourceHospitalCode,
                    branchCode: draftFilters.sourceBranchCode,
                  }}
                  onChange={changeSourceScope}
                />
              </div>
            </section>
          </div>
        : <section className='statistics-board-opposite-filter' aria-label={oppositeScopeLabel}>
            <span className='statistics-board-opposite-filter__label'>{oppositeScopeLabel}</span>
            <OrganizationScopeSelector
              orgId={scopeSpec.organizationId}
              value={oppositeScopeValue}
              onChange={changeOppositeScope}
            />
          </section>}
    </div>

    {/* 查询区（D-02、S6-D19）与报告管理列表页同构：inline Form 承载草稿控件；统一关键字（占位按侧区分）
        + 项目类型 + 预设时间区间，由「查询」提交，一次提交同时作用于汇总与明细；不设重置。 */}
    <div className='statistics-board-filters'>
      <Form layout='inline' onFinish={submitDraft}>
        <Form.Item>
          <Input
            placeholder={side === 'usage' ? '互认科室 / 互认项目 / 互认医生' : '互认科室 / 互认项目'}
            aria-label='关键字筛选'
            allowClear
            style={{ width: 240 }}
            value={draftFilters.keyword ?? ''}
            onChange={(event) => applyDraft({ keyword: event.target.value || undefined })}
          />
        </Form.Item>
        <Form.Item>
          <Select<MedicalItemTypeValue>
            allowClear
            placeholder='项目类型'
            aria-label='项目类型筛选'
            style={{ width: 120 }}
            value={draftFilters.itemType}
            options={itemTypeOptions.map((option) => ({ value: option.value, label: option.label }))}
            onChange={(value) => applyDraft({ itemType: value ?? undefined })}
          />
        </Form.Item>
        <Form.Item>
          {/*
            统计时间使用 antd RangePicker（与报告管理列表页同一控件与预设形态，S6-D19）：受控值来自草稿的
            年月日文本，只提交起止日期，不拼接时分秒、不写时区偏移；区间不可清空，默认近 30 天，
            预设提供今天、近 7 天与近 30 天三个快捷区间。
          */}
          <DatePicker.RangePicker
            allowClear={false}
            aria-label='统计时间范围'
            placeholder={['开始日期', '结束日期']}
            presets={datePresets}
            value={draftRangeValue}
            onChange={(_dates, dateStrings) => applyDraft({
              startTime: dateStrings[0] || '',
              endTime: dateStrings[1] || '',
            })}
          />
        </Form.Item>
        <Form.Item>
          {/*
            查询按钮逐字符合全站统一规格（框内查询图标按钮与查询区结构规格）：`aria-label` 声明可访问名称——
            `SearchOutlined` 自带英文 `aria-label`，会参与按钮名称计算，不声明就会读成「search 查询」；
            `autoInsertSpace={false}` 关闭 antd 对两个汉字之间自动插入的空格；颜色继承宿主主题 token，
            不覆盖样式。一次提交同时重读汇总与明细，任一读取在途时按钮进入 `loading`（与列表页同款，
            在途期间 antd loading 阻止点击提交、不会重复发起读取）；交叠请求仍由序号作废机制收敛为
            「只采纳最后一次」。不设重置：关键字与项目类型可清空，清空后经「查询」生效。
          */}
          <Button
            type='primary'
            icon={<SearchOutlined />}
            aria-label='查询'
            autoInsertSpace={false}
            htmlType='submit'
            loading={summaryLoading || detailLoading}
          >查询</Button>
        </Form.Item>
      </Form>
    </div>

    {/* 主从区：左列表区 + 右明细区。提供方两页由自己的列表区主体承载（S6-D24），
        使用方两页保留主从双栏的汇总列表与明细卡。 */}
    {side === 'source' ? <SourceStatisticsBoard
      dimension={dimension}
      dimensionOptions={dimensionOptions}
      summary={{
        rows: summaryRows as SourceSummaryRow[],
        totalCount: summaryTotal,
        pageIndex: summaryPage.pageIndex,
        selectedKey,
        loading: summaryLoading,
        failure: summaryFailure !== null,
      }}
      detail={{
        rows: detailRows as SourceDetailRow[],
        loading: detailLoading,
        failure: detailFailure !== null,
        pagination: detailPagination,
        timeRangeText: `${filters.startTime} 至 ${filters.endTime}`,
        detailTotal,
        showBranchColumn: showSourceBranchColumn,
      }}
      scopeValues={sourceScopeValues}
      includeScopeBranch={scopeSpec.kind === 'branch' || dimension === 2}
      selectedRow={selectedSummaryRow as SourceSummaryRow | null}
      missingScopeText={missingScopeText}
      exporting={exporting}
      onDimensionChange={(value) => setDimension(value)}
      onSummaryPageChange={changeSummaryPage}
      onSelectRow={selectSummaryRow}
      onExportSummary={() => { void runExport('summary') }}
      onExportDetails={() => { void runExport('details') }}
    /> : <div className='statistics-board-master'>
      <Section label='汇总列表'>
        <StatisticsSummaryList<UsageSummaryRow>
          rows={summaryRows as UsageSummaryRow[]}
          dimension={dimension}
          dimensionOptions={dimensionOptions}
          totalCount={summaryTotal}
          selectedKey={selectedKey}
          loading={summaryLoading}
          exporting={exporting}
          emptyText={summaryEmptyText}
          pageIndex={summaryPage.pageIndex}
          rowKey={usageSummaryRowKey}
          accessibleName={(row) => usageSummaryAccessibleName(row, dimension)}
          renderName={(row) => <>
            {dimension === 4 ? <Tag color='blue' className='statistics-board-group-row__tag'>{row.itemTypeText}</Tag> : null}
            {usageSummaryPrimary(row, dimension)}
          </>}
          renderSecondary={(row) => usageSummarySecondary(row, dimension)}
          renderMetrics={(row) => <span className='statistics-board-group-row__metrics'>
            <span>提醒 {row.reminderCount}</span>
            <span>采纳 {row.adoptionCount}</span>
            <span>不采纳 {row.nonAdoptionCount}</span>
            <span>引用 {row.referenceCount}</span>
          </span>}
          renderNote={() => (dimension === 3 ? <Typography.Text type='secondary' className='statistics-board-note'>
            互认科室维度不含未反馈项：该维度的提醒次数与同期互认率仅统计已反馈的匹配项，跨维度的提醒合计不可直接对账。
          </Typography.Text> : null)}
          onDimensionChange={(value) => setDimension(value)}
          onPageChange={changeSummaryPage}
          onSelectRow={selectSummaryRow}
          onExport={() => { void runExport('summary') }}
        />
      </Section>

      <Section label='明细'>
        <div className='statistics-board-detail-anchor' aria-label='明细'>
        <Card
          size='small'
          className='statistics-board-detail-card'
          title={selectedSummaryRow === null ? '明细' : <div className='statistics-board-detail-title'>
            <span className='statistics-board-detail-title__name'>{selectedPrimary}</span>
            <span className='statistics-board-detail-title__secondary'>{selectedSecondary}</span>
          </div>}
          extra={<Button
            size='small'
            aria-label='导出明细'
            loading={exporting}
            disabled={exporting || selectedKey === null}
            onClick={() => { void runExport('details') }}
          >导出明细</Button>}
        >
          {selectedSummaryRow === null ? <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={detailEmptyText}
          /> : <>
            <StatisticsMetricSummary items={summaryMetrics} />

            <StatisticsDetailToolbar
              idPrefix={detailTabsIdPrefix}
              contextText={selectedContextText}
              showReasonFilter={showReasonFilter}
              reasonCode={reasonCode}
              reasonOptions={reasonOptions}
              detailType={detailType}
              onReasonChange={setReasonCode}
              onDetailTypeChange={setDetailType}
            />

            {USAGE_DETAIL_TAB_ITEMS.map((item) => (
              <div
                key={item.value}
                id={usageDetailPanelId(detailTabsIdPrefix, String(item.value))}
                role='tabpanel'
                aria-labelledby={usageDetailTabId(detailTabsIdPrefix, String(item.value))}
                hidden={item.value !== detailType}
              >
                {item.value === detailType ? renderDetailPanel() : null}
              </div>
            ))}

          </>}
        </Card>
        </div>
      </Section>
    </div>}

    {recordState === null ? null : <MatchRecordModal state={recordState} onRetry={retryMatchRecord} onClose={() => setRecordState(null)} />}
  </div>
}
