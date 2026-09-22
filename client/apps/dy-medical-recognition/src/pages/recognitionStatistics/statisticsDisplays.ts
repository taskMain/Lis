/**
 * 统计页的展示与页面支持纯助手：只做取值到文本或初始条件的纯转换，不包含任何业务判定。
 * 主体组件、匹配记录弹窗与四个页面入口共用，避免各处复制一份日期/百分比格式或默认期间构造。
 */
import type { StatisticsFilters } from './statisticsBoard'

/** 页面默认统计期间：当月 1 日至当天（本地日期，`yyyy-MM-dd`），保证进入页面即可按有效日期自动加载。 */
export function defaultStatisticsFilters(): StatisticsFilters {
  const now = new Date()
  const pad = (part: number) => String(part).padStart(2, '0')
  const year = now.getFullYear()
  const month = pad(now.getMonth() + 1)
  return { startTime: `${year}-${month}-01`, endTime: `${year}-${month}-${pad(now.getDate())}` }
}

/** 时间字段的页面展示文本（`yyyy-MM-dd HH:mm`）；缺失显示「—」。 */
export function formatStatisticsDateTime(value: Date | null): string {
  if (value === null) return '—'
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())} ${pad(value.getHours())}:${pad(value.getMinutes())}`
}

/** 比例字段的页面展示文本（百分比，两位小数）；未计算显示「—」而非零或百分之百。 */
export function formatStatisticsPercent(value: number | null): string {
  return value === null ? '—' : `${(value * 100).toFixed(2)}%`
}

/** 金额字段的页面展示文本（两位小数）；缺失显示「—」。 */
export function formatStatisticsAmount(value: number | null): string {
  return value === null ? '—' : value.toFixed(2)
}

/** 可空文本的单元格展示；空白显示「—」。 */
export function statisticsTextOrDash(value: string | null | undefined): string {
  return value === undefined || value === null || value.length === 0 ? '—' : value
}
