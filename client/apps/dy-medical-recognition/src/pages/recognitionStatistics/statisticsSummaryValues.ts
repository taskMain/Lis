/**
 * 统计页汇总行的展示取值：侧别与行联合类型、稳定行键、主次行文本与可访问名称。
 * 汇总列表组件与页面主体共用这里的口径，避免行的展示规则在页面编排里重复。
 */
import type { UsageSummaryRow } from './recognitionUsageStatisticsApi'
import type { SourceSummaryRow } from './sourceRecognitionStatisticsApi'
import { partyName } from './statisticsDetailValues'

/** 统计页侧别：使用方（互认使用）或提供方（被互认）。 */
export type StatisticsSide = 'usage' | 'source'

/** 主体内两侧行的联合：行的实际类型与 `side` 一致（数据源由页面按侧注入）。 */
export type StatisticsSummaryRow = UsageSummaryRow | SourceSummaryRow

/** 汇总行次行文本：去掉空段，保留设计的三段式分隔。 */
function joinSummaryParts(parts: readonly (string | null | undefined)[]): string {
  const values = parts.map((part) => (part ?? '').trim()).filter((part) => part.length > 0)
  return values.length === 0 ? '—' : values.join(' · ')
}

/** 使用方汇总行主名称：维度决定展示的字段。 */
export function usageSummaryPrimary(row: UsageSummaryRow, dimension: number): string {
  if (dimension === 2) return partyName(row.receiverBranchName)
  if (dimension === 3) return partyName(row.recognitionDeptName)
  if (dimension === 4) return partyName(row.standardProjectName)
  return partyName(row.receiverHospitalName)
}

/** 使用方汇总行次行：医院为组织名，院区为医院与组织，科室为科室 ID、院区与医院，标准项目为编码与分类 / 分组。 */
export function usageSummarySecondary(row: UsageSummaryRow, dimension: number): string {
  if (dimension === 2) return joinSummaryParts([row.receiverHospitalName, row.receiverOrganizationName])
  if (dimension === 3) {
    return joinSummaryParts([row.recognitionDeptId, row.receiverBranchName, row.receiverHospitalName])
  }
  if (dimension === 4) {
    const categoryGroup = joinSummaryParts([row.categoryName, row.groupName]).replaceAll(' · ', ' / ')
    return joinSummaryParts([row.standardProjectCode, categoryGroup])
  }
  return partyName(row.receiverOrganizationName)
}

/** 提供方汇总行主名称。 */
export function sourceSummaryPrimary(row: SourceSummaryRow, dimension: number): string {
  if (dimension === 2) return partyName(row.sourceBranchName)
  if (dimension === 4) return partyName(row.standardProjectName)
  return partyName(row.sourceHospitalName)
}

/** 提供方汇总行次行。 */
export function sourceSummarySecondary(row: SourceSummaryRow, dimension: number): string {
  if (dimension === 2) return joinSummaryParts([row.sourceHospitalName, row.sourceOrganizationName])
  if (dimension === 4) {
    const categoryGroup = joinSummaryParts([row.categoryName, row.groupName]).replaceAll(' · ', ' / ')
    return joinSummaryParts([row.standardProjectCode, categoryGroup])
  }
  return partyName(row.sourceOrganizationName)
}

/** 按侧取汇总行主名称。 */
export function summaryRowPrimary(side: StatisticsSide, row: StatisticsSummaryRow, dimension: number): string {
  return side === 'usage'
    ? usageSummaryPrimary(row as UsageSummaryRow, dimension)
    : sourceSummaryPrimary(row as SourceSummaryRow, dimension)
}

/** 按侧取汇总行次行。 */
export function summaryRowSecondary(side: StatisticsSide, row: StatisticsSummaryRow, dimension: number): string {
  return side === 'usage'
    ? usageSummarySecondary(row as UsageSummaryRow, dimension)
    : sourceSummarySecondary(row as SourceSummaryRow, dimension)
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

/** 提供方汇总行的稳定行标识，口径同使用方。 */
function sourceSummaryRowKey(row: SourceSummaryRow): string {
  return [
    row.sourceOrganizationCode,
    row.sourceHospitalCode,
    row.sourceBranchCode,
    row.itemType,
    row.standardProjectCode,
  ].map((part) => part ?? '').join('|')
}

/** 按侧取汇总行的稳定键。 */
export function summaryRowKey(side: StatisticsSide, row: StatisticsSummaryRow): string {
  return side === 'usage'
    ? usageSummaryRowKey(row as UsageSummaryRow)
    : sourceSummaryRowKey(row as SourceSummaryRow)
}

/** 左列表行的可访问名称：名称、次行与全部指标一起进入按钮名称。 */
export function summaryRowAccessibleName(side: StatisticsSide, row: StatisticsSummaryRow, dimension: number): string {
  if (side === 'usage') {
    const usage = row as UsageSummaryRow
    return [
      usageSummaryPrimary(usage, dimension),
      usageSummarySecondary(usage, dimension),
      `提醒 ${usage.reminderCount}`,
      `采纳 ${usage.adoptionCount}`,
      `不采纳 ${usage.nonAdoptionCount}`,
      `引用 ${usage.referenceCount}`,
    ].join('，')
  }
  const source = row as SourceSummaryRow
  return [
    sourceSummaryPrimary(source, dimension),
    sourceSummarySecondary(source, dimension),
    `被互认 ${source.recognitionCount}`,
  ].join('，')
}
