/**
 * 统计页默认统计期间与时间预设的纯函数用例（C32 的取值面）：
 * 日期数学以固定「今天」冻结断言，页面挂载的真实当天窗口由调用方传入。
 */
import { describe, expect, it } from 'vitest'
import dayjs from 'dayjs'
import { buildStatisticsDatePresets, defaultStatisticsFilters } from './statisticsDisplays'

describe('C32 默认统计期间与时间预设（近 30 天滚动窗，S6-D19）', () => {
  it('默认统计期间为近 30 天：今天往前 29 天到今天，含当天共 30 天', () => {
    const filters = defaultStatisticsFilters(dayjs('2026-10-20'))
    expect(filters.startTime).toBe('2026-09-21')
    expect(filters.endTime).toBe('2026-10-20')
  })

  it('时间预设为今天、近7天、近30天；「近30天」与默认窗口同一口径', () => {
    const presets = buildStatisticsDatePresets(dayjs('2026-10-20'))
    expect(presets.map((preset) => preset.label)).toEqual(['今天', '近7天', '近30天'])
    expect(presets[0].value.map((value) => value.format('YYYY-MM-DD'))).toEqual(['2026-10-20', '2026-10-20'])
    expect(presets[1].value.map((value) => value.format('YYYY-MM-DD'))).toEqual(['2026-10-14', '2026-10-20'])
    expect(presets[2].value.map((value) => value.format('YYYY-MM-DD'))).toEqual(['2026-09-21', '2026-10-20'])
  })
})
