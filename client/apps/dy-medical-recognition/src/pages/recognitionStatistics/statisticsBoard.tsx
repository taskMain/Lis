/**
 * 四个统计页面（平台/本院 × 接收侧/来源侧）共享的页面主体（S6-D11、S6-D19；查询区与报告管理列表页同构）。
 *
 * 主体承载统计页的全部行为编排：筛选区、范围选择、汇总维度单选与汇总表、
 * 明细类型单选与明细表、导出入口与匹配记录弹窗；接收侧与来源侧的结构差异用 `side` 表达，
 * 平台/本院的范围形态差异用 `scopeSpec` 表达。数据源由四个页面注入（查询/导出入口以适配层为边界），
 * 主体不接触生成的 Kiota 类型，也不创建 API Client。
 *
 * 查询区同构（S6-D19）：统一关键字（占位按侧区分，来源侧语句无医生列故不含互认医生）+ 项目类型下拉
 * + 预设日期区间选择器（今天/近 7 天/近 30 天，不可清空，默认近 30 天）+ 主色查询按钮；不设重置，
 * 关键字与项目类型可清空、清空后经「查询」生效。筛选控件改动只进草稿、零请求；点「查询」把草稿
 * 提交为已生效条件，一次提交同时重读汇总与明细，同条件再次提交等价刷新。汇总数字下钻把维度值
 * 预填进草稿并自动提交一次（互认科室/标准项目维度写入关键字草稿）；原因行下钻写入精确原因代码
 * 并在明细区工具行显示可移除筛选标记（移除同走提交路径）。汇总维度、明细类型是视图切换，
 * 保持即时生效、不进草稿；翻页基于已生效条件自动读取。本院页页头为左右结构（左侧标题区、
 * 右侧登录范围条），平台页页头不承载范围条。
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
  DatePicker,
  Empty,
  Form,
  Input,
  Radio,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  type TableColumnsType,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { BaseScopeSelector, type BaseScopeValue } from '@dy/components-base'
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
  type StatisticsPage,
  type StatisticsPageInput,
  type StatisticsGroupDimensionValue,
  type UsageDetailTypeValue,
} from './recognitionStatisticsValues'
import {
  buildStatisticsDatePresets,
  defaultStatisticsFilters,
  formatStatisticsAmount,
  formatStatisticsDateTime,
  formatStatisticsPercent,
  statisticsTextOrDash,
} from './statisticsDisplays'
import './statisticsBoard.css'

/** 统计页侧别：接收侧（互认使用）或来源侧（被互认）。 */
export type StatisticsSide = 'usage' | 'source'

/** 主体内两侧行的联合：行的实际类型与 `side` 一致（数据源由页面按侧注入）。 */
export type StatisticsSummaryRow = UsageSummaryRow | SourceSummaryRow
export type StatisticsDetailRow = UsageDetailRow | SourceDetailRow

/**
 * 页面筛选状态的稳定模型：日期必填；范围字段按页面版本取用（平台页提交两组三级，
 * 本院页提交本侧院区与对侧组医院/院区）；统一关键字承载互认科室、互认项目与互认医生条件（S6-D19）；
 * 不采纳原因代码只经原因行下钻写入，仅在不采纳明细查询与明细导出生效。
 */
export interface StatisticsFilters {
  startTime: string
  endTime: string
  /**
   * 本院页本侧院区（接收侧页为使用方院区，来源侧页为提供方院区）：恒为登录院区，
   * 页面不提供院区选择；缺失属权限配置错误，页面零业务请求、以空态呈现（`missingScopeText`）；
   * 服务端全院统计契约仅为接口面保留（S6-D1），前端已不按其触发。
   */
  ownBranchCode?: string
  receiverOrganizationCode?: string
  receiverHospitalCode?: string
  receiverBranchCode?: string
  sourceOrganizationCode?: string
  sourceHospitalCode?: string
  sourceBranchCode?: string
  /** 统一关键字：互认科室、互认项目与互认医生条件的单一承载（S6-D19），输入原文经适配层去首尾空白提交。 */
  keyword?: string
  itemType?: MedicalItemTypeValue
  nonAdoptionReasonCode?: string
}

/** 范围选择区的形态：平台页两组三级可选；本院页本侧固定展示（院区恒为登录院区、不提供院区选择）、对侧组医院/院区可选。 */
export type StatisticsScopeSpec =
  | { kind: 'platform' }
  | { kind: 'branch'; organizationCode: string }

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
  { value: 1, label: '提供方医院' },
  { value: 2, label: '提供方院区' },
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

/**
 * 归属名称的单元格文本（D-04）：只显名称、不回退编码；名称缺失显示「未知」。
 */
function partyName(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '未知'
}

/**
 * 归属方三行式单元格（D-04）：组织名、医院名、灰色小字的院区名各占一行，只显名称不显示编码。
 * 名称直接取读模型交付字段（明细行 `source.*` / `receiver.*`），不做前端反查。
 */
function PartyCell(props: { organizationName?: string | null; hospitalName?: string | null; branchName?: string | null }) {
  return <div>
    <div>{partyName(props.organizationName)}</div>
    <div>{partyName(props.hospitalName)}</div>
    <Typography.Text type='secondary' style={{ fontSize: 12 }}>{partyName(props.branchName)}</Typography.Text>
  </div>
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

/** 接收侧汇总行下钻的筛选预填：按维度值落到对应条件字段（本院页用不到的字段由页面映射忽略）。 */
function usageDrilldownPatch(row: UsageSummaryRow): Partial<StatisticsFilters> {
  switch (row.groupDimension) {
    case 1: return { receiverHospitalCode: row.receiverHospitalCode ?? undefined }
    // 本侧院区恒为登录院区（不提供院区选择），院区维度的下钻不再回写 ownBranchCode。
    case 2: return { receiverBranchCode: row.receiverBranchCode ?? undefined }
    // 互认科室与标准项目维度的维度值写入关键字草稿（S6-D19）：科室值/项目编码经统一关键字提交。
    case 3: return { keyword: row.recognitionDeptId ?? undefined }
    case 4: return { itemType: row.itemType ?? undefined, keyword: row.standardProjectCode ?? undefined }
    default: return {}
  }
}

/** 来源侧汇总行下钻的筛选预填，口径同接收侧（来源侧无互认科室维度）。 */
function sourceDrilldownPatch(row: SourceSummaryRow): Partial<StatisticsFilters> {
  switch (row.groupDimension) {
    case 1: return { sourceHospitalCode: row.sourceHospitalCode ?? undefined }
    // 本侧院区恒为登录院区（不提供院区选择），院区维度的下钻不再回写 ownBranchCode。
    case 2: return { sourceBranchCode: row.sourceBranchCode ?? undefined }
    case 4: return { itemType: row.itemType ?? undefined, keyword: row.standardProjectCode ?? undefined }
    default: return {}
  }
}

/** 可下钻的指标数字按钮。 */
function metricDrillButton(ariaLabel: string, count: number, onClick: () => void): ReactNode {
  return <Button type='link' size='small' aria-label={ariaLabel} onClick={onClick}>{count}</Button>
}

/** 两个筛选条件的取值是否一致（浅比较全部键）：决定一次提交走「条件变化复位」还是「同条件重读」。 */
function sameFilters(left: StatisticsFilters, right: StatisticsFilters): boolean {
  const leftKeys = Object.keys(left) as (keyof StatisticsFilters)[]
  const rightKeys = Object.keys(right) as (keyof StatisticsFilters)[]
  if (leftKeys.length !== rightKeys.length) return false
  return leftKeys.every((key) => left[key] === right[key])
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
      { title: '提供方组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceOrganizationName) },
      { title: '提供方医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceHospitalName) },
    )
  } else if (dimension === 2) {
    dimensionColumns.push(
      { title: '提供方组织', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceOrganizationName) },
      { title: '提供方医院', width: 140, ellipsis: true, render: (_, row) => textCell(row.sourceHospitalName) },
      { title: '提供方院区', width: 120, ellipsis: true, render: (_, row) => textCell(row.sourceBranchName) },
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
    // 提供方、使用方三行式（D-04）：提供方组织/医院/院区与使用方组织/医院/院区各合并为一列。
    { title: '提供方', width: 170, ellipsis: true, render: (_, row) => <PartyCell {...row.source} /> },
    { title: '使用方', width: 170, ellipsis: true, render: (_, row) => <PartyCell {...row.receiver} /> },
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

/** 来源侧明细列：双方归属三行式；只反映被采纳事实，无匹配记录入口（S6-D10、D-04）。 */
function sourceDetailColumns(): TableColumnsType<SourceDetailRow> {
  return [
    { title: '提供方', width: 170, ellipsis: true, render: (_, row) => <PartyCell {...row.source} /> },
    { title: '使用方', width: 170, ellipsis: true, render: (_, row) => <PartyCell {...row.receiver} /> },
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
  /**
   * 同条件重读推进号（与金额页先例同一手法）：条件、维度、明细类型与页对象引用都不变时
   * （同条件再次提交，等价于刷新），靠它让读取效果重新执行；此时不清空旧结果，失败也不覆盖。
   */
  const [reloadNonce, setReloadNonce] = useState(0)

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

  // 条件、维度、明细类型、页码或同条件重读推进号变化时重新读取当页（自动加载入口）。
  useEffect(() => {
    const task = window.setTimeout(() => { void loadSummary(filters, dimension, summaryPage) }, 0)
    return () => window.clearTimeout(task)
  }, [dimension, filters, loadSummary, reloadNonce, summaryPage])

  useEffect(() => {
    const task = window.setTimeout(() => { void loadDetails(filters, detailType, detailPage) }, 0)
    return () => window.clearTimeout(task)
  }, [detailPage, detailType, filters, loadDetails, reloadNonce])

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

  /**
   * 原因筛选标记的移除（比照报告页状态页签的即时提交语义，S6-D19）：只从已生效条件与草稿各移除
   * 原因代码一个字段，其余未提交草稿保持不动；不走会把草稿整体覆写为已生效条件的提交入口。
   * 已生效条件换引用触发既有的 mark 复位与两路重读并回第 1 页；同条件重复移除保持旧条件引用、
   * 仅推进重读号等价刷新（与既有提交路径同一口径）。
   */
  const removeReasonFilter = useCallback(() => {
    setFilters((previous) => {
      const next = { ...previous, nonAdoptionReasonCode: undefined }
      return sameFilters(previous, next) ? previous : next
    })
    setDraftFilters((previous) => ({ ...previous, nonAdoptionReasonCode: undefined }))
    setReloadNonce((nonce) => nonce + 1)
  }, [])

  /** 平台页接收组/来源组三级范围选择（改动进草稿）。 */
  const changeReceiverScope = useCallback((value: BaseScopeValue) => applyDraft({
    receiverOrganizationCode: value.orgId,
    receiverHospitalCode: value.hosId,
    receiverBranchCode: value.branchId,
  }), [applyDraft])
  const changeSourceScope = useCallback((value: BaseScopeValue) => applyDraft({
    sourceOrganizationCode: value.orgId,
    sourceHospitalCode: value.hosId,
    sourceBranchCode: value.branchId,
  }), [applyDraft])

  /** 本院页对侧组范围选择：对侧组织固定为可信组织，只提交医院与院区（S6-D1；改动进草稿）。 */
  const changeOppositeScope = useCallback((value: BaseScopeValue) => {
    if (side === 'usage') applyDraft({ sourceHospitalCode: value.hosId, sourceBranchCode: value.branchId })
    else applyDraft({ receiverHospitalCode: value.hosId, receiverBranchCode: value.branchId })
  }, [applyDraft, side])

  /** 汇总数字下钻：维度值预填进筛选草稿并自动提交一次（走同一条提交路径，C12/D-02）；明细类型切换即时生效。 */
  const drillToUsageDetails = useCallback((row: UsageSummaryRow, targetDetailType: UsageDetailTypeValue) => {
    setDetailType(targetDetailType)
    const patch = usageDrilldownPatch(row)
    // 平台页未选组织时按该行所属组织补齐（本院页的组织由服务端按登录上下文注入，不提交）。
    if (scopeSpec.kind === 'platform' && !draftFilters.receiverOrganizationCode && row.receiverOrganizationCode) {
      patch.receiverOrganizationCode = row.receiverOrganizationCode
    }
    submitFilters({ ...draftFilters, ...patch })
  }, [draftFilters, scopeSpec.kind, submitFilters])
  const drillToSourceDetails = useCallback((row: SourceSummaryRow) => {
    const patch = sourceDrilldownPatch(row)
    if (scopeSpec.kind === 'platform' && !draftFilters.sourceOrganizationCode && row.sourceOrganizationCode) {
      patch.sourceOrganizationCode = row.sourceOrganizationCode
    }
    submitFilters({ ...draftFilters, ...patch })
  }, [draftFilters, scopeSpec.kind, submitFilters])
  /** 原因行下钻：明细加原因代码筛选并切到不采纳明细（C12），同走提交路径。 */
  const drillToReasonDetails = useCallback((reasonCode: string) => {
    setDetailType(3)
    submitFilters({ ...draftFilters, nonAdoptionReasonCode: reasonCode })
  }, [draftFilters, submitFilters])

  /** 导出：沿用当前查询条件（不带分页）与下载名构造；导出中禁用重复提交（C15）。 */
  const runExport = useCallback(async (kind: 'summary' | 'details') => {
    if (exporting) return
    // 时间区间不可清空（allowClear={false}），此守卫为契约面防御：缺日期时条件不完整，
    // 不构造导出请求，也不构造下载名（查询出口返回 null，主体零发请求）。
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
  // 时间预设按挂载当天的日历构造（S6-D19：今天/近 7 天/近 30 天），引用稳定、不随读取重算。
  const datePresets = useMemo(() => buildStatisticsDatePresets(dayjs()), [])
  /** 草稿时间区间的受控值：来自草稿的年月日文本（区间不可清空，两端恒有值）。 */
  const draftRangeValue: [Dayjs, Dayjs] = [dayjs(draftFilters.startTime), dayjs(draftFilters.endTime)]
  /**
   * 原因筛选标记的展示名（S6-D19）：取当前汇总行原因区内该代码的原因名；行内未交付该代码时
   * 兜底展示代码原文，标记的移除行为不受名称来源影响。
   */
  const reasonFilterName = useMemo(() => {
    const code = filters.nonAdoptionReasonCode
    if (code === undefined) return null
    const found = (summaryRows as UsageSummaryRow[])
      .flatMap((row) => row.nonAdoptionReasons)
      .find((reason) => reason.reasonCode === code)
    return found?.reasonName ?? code
  }, [filters.nonAdoptionReasonCode, summaryRows])
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
        {/* 页面大标题层级与 margin 口径全站统一（标题层级口径：大标题与阻断态标题同层级），margin 归零由配套 CSS 承载。 */}
        <Typography.Title level={3}>{title}</Typography.Title>
        <Typography.Text type='secondary'>{subtitle}</Typography.Text>
      </div>
      {/* 本院页页头左右结构（S6-D19，与本院报告管理页同构）：登录范围条承载在页头右侧；平台页页头不承载范围条。 */}
      {scopeSpec.kind === 'branch' ? <CurrentScopeBar /> : null}
    </div>

    {/* 范围区（T01-D6）：使用方与提供方左右并排，中间方向列表达「使用方的报告来自提供方」。
        平台页两列的组织、医院、院区都可选；本院页固定侧只读（取登录范围），另一侧的组织固定为可信组织、
        只可选医院与院区。改动进草稿，点「查询」生效。 */}
    <div className='statistics-board-scope'>
      <div className='statistics-board-duo'>
        <section className='statistics-board-party statistics-board-party--using' aria-label='使用方范围'>
          <div className='statistics-board-party__head'>
            <span className='statistics-board-party__name'>使用方</span>
            <span className='statistics-board-party__desc'>调阅并使用他院报告</span>
          </div>
          <div className='statistics-board-party__body'>
            {scopeSpec.kind === 'branch' && side === 'usage'
              ? <span className='statistics-board-party__locked'>固定为登录范围，不可修改</span>
              : <BaseScopeSelector
                  level='branch'
                  orgId={scopeSpec.kind === 'branch' ? scopeSpec.organizationCode : undefined}
                  value={scopeSpec.kind === 'branch'
                    ? { hosId: draftFilters.receiverHospitalCode, branchId: draftFilters.receiverBranchCode }
                    : {
                        orgId: draftFilters.receiverOrganizationCode,
                        hosId: draftFilters.receiverHospitalCode,
                        branchId: draftFilters.receiverBranchCode,
                      }}
                  onChange={scopeSpec.kind === 'branch' ? changeOppositeScope : changeReceiverScope}
                />}
          </div>
        </section>

        <div className='statistics-board-flow' role='img' aria-label='方向标记：使用方的报告来自提供方'>
          <span className='statistics-board-flow__arrow' aria-hidden='true'>←</span>
          <span className='statistics-board-flow__text'>报告来自</span>
        </div>

        <section className='statistics-board-party statistics-board-party--providing' aria-label='提供方范围'>
          <div className='statistics-board-party__head'>
            <span className='statistics-board-party__name'>提供方</span>
            <span className='statistics-board-party__desc'>出具并提供报告</span>
          </div>
          <div className='statistics-board-party__body'>
            {scopeSpec.kind === 'branch' && side === 'source'
              ? <span className='statistics-board-party__locked'>固定为登录范围，不可修改</span>
              : <BaseScopeSelector
                  level='branch'
                  orgId={scopeSpec.kind === 'branch' ? scopeSpec.organizationCode : undefined}
                  value={scopeSpec.kind === 'branch'
                    ? { hosId: draftFilters.sourceHospitalCode, branchId: draftFilters.sourceBranchCode }
                    : {
                        orgId: draftFilters.sourceOrganizationCode,
                        hosId: draftFilters.sourceHospitalCode,
                        branchId: draftFilters.sourceBranchCode,
                      }}
                  onChange={scopeSpec.kind === 'branch' ? changeOppositeScope : changeSourceScope}
                />}
          </div>
        </section>
      </div>
    </div>

    {/* 查询区（D-02、S6-D19）与报告管理列表页同构：inline Form 承载草稿控件；统一关键字（占位按侧区分）
        + 项目类型 + 预设时间区间，由「查询」提交，一次提交同时作用于汇总与明细；不设重置。 */}
    <div className='statistics-board-filters'>
      <Form layout='inline' onFinish={submitDraft}>
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
      {/* 汇总读取失败不渲染横幅（既定口径）：已有数据静默保留，错误由宿主统一提示，重试经查询按钮。 */}
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
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={missingScopeText ?? '当前筛选条件没有汇总数据'} /> }}
      /> : <Table<SourceSummaryRow>
        rowKey={(row) => sourceSummaryRowKey(row)}
        size='small'
        columns={sourceSummaryCols}
        dataSource={summaryRows as SourceSummaryRow[]}
        loading={summaryLoading && summaryRows.length === 0}
        scroll={{ x: 1100 }}
        pagination={summaryPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={missingScopeText ?? '当前筛选条件没有汇总数据'} /> }}
      />}
    </Section>

    {/* 明细区：明细类型是视图切换（即时生效、不进草稿）；互认医生与不采纳原因已并入页面查询区统一提交。 */}
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
        </> : null}
        {/* 原因下钻的精确筛选标记（S6-D19，仅接收侧明细区）：只在原因代码已生效时出现，移除按即时提交语义生效。 */}
        {side === 'usage' && reasonFilterName !== null
          ? <Tag closable onClose={removeReasonFilter}>不采纳原因：{reasonFilterName}</Tag>
          : null}
        <Button
          aria-label='导出明细'
          loading={exporting}
          disabled={exporting}
          onClick={() => { void runExport('details') }}
        >导出明细</Button>
      </Space>
      {/* 明细读取失败同样不渲染横幅（既定口径）：已有数据静默保留，错误由宿主统一提示，重试经查询按钮。 */}
      {side === 'usage' ? <Table<UsageDetailRow>
        rowKey='recognitionMatchItemId'
        size='small'
        columns={usageDetailCols}
        dataSource={detailRows as UsageDetailRow[]}
        loading={detailLoading && detailRows.length === 0}
        // 提供方/使用方三列并两列后横向宽度收窄（D-04）。
        scroll={{ x: 1900 }}
        pagination={detailPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={missingScopeText ?? '当前筛选条件没有明细数据'} /> }}
      /> : <Table<SourceDetailRow>
        rowKey='recognitionMatchItemId'
        size='small'
        columns={sourceDetailCols}
        dataSource={detailRows as SourceDetailRow[]}
        loading={detailLoading && detailRows.length === 0}
        // 提供方/使用方三列并两列后横向宽度收窄（D-04）。
        scroll={{ x: 1150 }}
        pagination={detailPagination}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={missingScopeText ?? '当前筛选条件没有明细数据'} /> }}
      />}
    </Section>

    {recordState === null ? null : <MatchRecordModal state={recordState} onRetry={retryMatchRecord} onClose={() => setRecordState(null)} />}
  </div>
}
