/**
 * 统计页展示的纯文本规则：归属名称、固定侧院区列判定与各区域空态文案。
 * 使用方与提供方两侧的列表区共用这些取值，同一句文案只在本文维护一次。
 */
import type { StatisticsScopeSpec } from './statisticsBoard'

/** 归属名称：缺失时统一显示「未知」。 */
export function partyName(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '未知'
}

/** 标准项目名称：名称缺失时显示「未匹配标准项目」，明细列、卡头与汇总行共用这一处兜底。 */
export function projectNameText(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '未匹配标准项目'
}

/** 标准项目编码：缺失时显示「—」，明细列、卡头与汇总行共用这一处兜底。 */
export function projectCodeText(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '—'
}

/**
 * 固定侧院区列只在医院维度、平台页且该院区未被已生效筛选锁定时出现。
 * 使用方与提供方明细列共用同一维度判断，不各自重复写一遍。
 */
export function showFixedBranchColumn(
  dimension: number,
  scopeKind: StatisticsScopeSpec['kind'],
  fixedBranchLocked: boolean,
): boolean {
  return dimension === 1 && scopeKind === 'platform' && !fixedBranchLocked
}

/** 汇总列表空态文案：读取失败给出查询重试指引；本院页登录院区缺失时给出范围缺失文案。 */
export function statisticsSummaryEmptyText(failure: boolean, missingScopeText?: string): string {
  if (failure) return '汇总加载失败，请点击查询重试'
  return missingScopeText ?? '当前筛选条件没有汇总数据'
}

/** 明细卡空态文案：汇总读取失败时给不出可选分组，其余情况提示从左侧选行。 */
export function statisticsDetailCardEmptyText(summaryFailure: boolean): string {
  return summaryFailure ? '汇总加载失败，暂无可选明细' : '从左侧选择一行查看明细'
}

/** 明细表空态文案（使用方）：读取失败时经明细类型页签或重试恢复。 */
export function usageDetailTableEmptyText(failure: boolean, rowCount: number, missingScopeText?: string): string {
  if (failure && rowCount === 0) return '明细加载失败，请切换页签或重试'
  return missingScopeText ?? '当前筛选条件没有明细数据'
}

/** 明细表空态文案（提供方）：读取失败时经查询按钮重试。 */
export function sourceDetailTableEmptyText(failure: boolean, rowCount: number, missingScopeText?: string): string {
  if (failure && rowCount === 0) return '明细加载失败，请点击查询重试'
  return missingScopeText ?? '当前筛选条件没有明细数据'
}
