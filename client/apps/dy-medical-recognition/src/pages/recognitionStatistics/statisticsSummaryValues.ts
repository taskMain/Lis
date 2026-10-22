/**
 * 统计页汇总行的展示取值：侧别与行联合类型、稳定行键、主次行文本与可访问名称。
 * 汇总列表组件与页面主体共用这里的口径，避免行的展示规则在页面编排里重复。
 */
import type { UsageSummaryRow } from './recognitionUsageStatisticsApi'
import type { SourceSummaryRow } from './sourceRecognitionStatisticsApi'
import { partyName, projectCodeText, projectNameText } from './statisticsDetailValues'

/** 统计页侧别：使用方（互认使用）或提供方（被互认）。 */
export type StatisticsSide = 'usage' | 'source'

/** 主体内两侧行的联合：行的实际类型与 `side` 一致（数据源由页面按侧注入）。 */
export type StatisticsSummaryRow = UsageSummaryRow | SourceSummaryRow

/** 多段文本拼接：去掉空段后以「 · 」连接，全空返回「—」；汇总行与承接范围共用。 */
export function joinSummaryParts(parts: readonly (string | null | undefined)[]): string {
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

/** 提供方汇总行主名称；标准项目维度按项目名称展示，名称缺失时给出明确文案。 */
export function sourceSummaryPrimary(row: SourceSummaryRow, dimension: number): string {
  if (dimension === 2) return partyName(row.sourceBranchName)
  if (dimension === 4) return projectNameText(row.standardProjectName)
  return partyName(row.sourceHospitalName)
}

/** 标准项目分类与分组：两者都为空时整段省略，有值时以「/」连接。 */
function projectCategoryGroupText(categoryName: string | null | undefined, groupName: string | null | undefined): string {
  return [categoryName, groupName]
    .map((part) => (part ?? '').trim())
    .filter((part) => part.length > 0)
    .join(' / ')
}

/**
 * 提供方汇总行次行。标准项目维度为「编码 · 分类 / 分组」：
 * 编码缺失补「—」，分类与分组同时为空时只保留编码，不追加占位分段。
 */
export function sourceSummarySecondary(row: SourceSummaryRow, dimension: number): string {
  if (dimension === 2) return joinSummaryParts([row.sourceHospitalName, row.sourceOrganizationName])
  if (dimension === 4) {
    const codeText = projectCodeText(row.standardProjectCode)
    const categoryGroup = projectCategoryGroupText(row.categoryName, row.groupName)
    return categoryGroup.length > 0 ? `${codeText} · ${categoryGroup}` : codeText
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

/** 使用方汇总行的稳定行标识：由维度相关字段合成；服务端同一维度内分组值唯一。 */
export function usageSummaryRowKey(row: UsageSummaryRow): string {
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
export function sourceSummaryRowKey(row: SourceSummaryRow): string {
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

/** 使用方左列表行的可访问名称：名称、次行与四项指标一起进入按钮名称。 */
export function usageSummaryAccessibleName(row: UsageSummaryRow, dimension: number): string {
  return [
    usageSummaryPrimary(row, dimension),
    usageSummarySecondary(row, dimension),
    `提醒 ${row.reminderCount}`,
    `采纳 ${row.adoptionCount}`,
    `不采纳 ${row.nonAdoptionCount}`,
    `引用 ${row.referenceCount}`,
  ].join('，')
}
