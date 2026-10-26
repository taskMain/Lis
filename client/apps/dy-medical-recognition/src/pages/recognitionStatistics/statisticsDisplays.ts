/**
 * 统计页的展示与页面支持纯助手：只做取值到文本或初始条件的纯转换，不包含任何业务判定。
 * 主体组件、匹配记录弹窗与四个页面入口共用，避免各处复制一份日期/百分比格式或默认期间构造。
 */
import dayjs, { type Dayjs } from 'dayjs'
import type { StatisticsFilters } from './statisticsBoard'

/**
 * 页面默认统计期间（S6-D19）：近 30 天（今天往前 29 天到今天的滚动窗，含当天共 30 天，与「近30天」
 * 预设同义），保证进入页面即可按有效日期自动加载；范围切换复位的初始条件同此口径。
 */
export function defaultStatisticsFilters(now: Dayjs = dayjs()): StatisticsFilters {
  return { startTime: now.subtract(29, 'day').format('YYYY-MM-DD'), endTime: now.format('YYYY-MM-DD') }
}

/** 时间快捷预设（S6-D19）：今天、近 7 天与近 30 天；「近30天」与默认统计期间同一口径。 */
export function buildStatisticsDatePresets(today: Dayjs): { label: string; value: [Dayjs, Dayjs] }[] {
  return [
    { label: '今天', value: [today, today] },
    { label: '近7天', value: [today.subtract(6, 'day'), today] },
    { label: '近30天', value: [today.subtract(29, 'day'), today] },
  ]
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

/** 名称缺失的组织维度编码：有名称的跳过，无名称但编码非空的收进去，去重后按出现顺序返回。 */
export function missingScopeNameCodes(
  pairs: ReadonlyArray<{ code: string | null | undefined; name: string | null | undefined }>,
): string[] {
  const codes = new Set<string>()
  for (const pair of pairs) {
    if ((pair.name ?? '').trim().length > 0) continue
    const code = (pair.code ?? '').trim()
    if (code.length > 0) codes.add(code)
  }
  return [...codes]
}
