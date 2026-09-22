/**
 * 统计取值域与映射助手的补充用例：导出类型中文名与下载文件名构造。
 *
 * 下载名是导出入口在点击期就地构造的：生成端只透出字节体、不透出响应头，
 * 因此「导出类型中文名-起止日期.xlsx」的中文名部分取自后端
 * `RecognitionStatisticsExportType` 枚举描述的镜像（服务端 Content-Disposition 同名），
 * 日期部分取页面当前查询条件的起止日期（yyyyMMdd-yyyyMMdd）。
 */
import { describe, expect, it } from 'vitest'
import { buildStatisticsExportDownloadName, statisticsExportTypeText } from './recognitionStatisticsValues'

describe('统计导出类型中文名', () => {
  it('七个取值与后端枚举描述逐一同名', () => {
    expect(statisticsExportTypeText(1)).toBe('接收侧互认使用汇总')
    expect(statisticsExportTypeText(2)).toBe('接收侧提醒明细')
    expect(statisticsExportTypeText(3)).toBe('接收侧采纳明细')
    expect(statisticsExportTypeText(4)).toBe('接收侧不采纳明细')
    expect(statisticsExportTypeText(5)).toBe('接收侧引用明细')
    expect(statisticsExportTypeText(6)).toBe('来源医院被互认汇总')
    expect(statisticsExportTypeText(7)).toBe('来源医院被互认明细')
  })

  it('取值越界时就地失败，不产出错误文件名', () => {
    expect(() => statisticsExportTypeText(0)).toThrow()
    expect(() => statisticsExportTypeText(8)).toThrow()
  })
})

describe('统计导出下载文件名构造', () => {
  it('按服务端命名规则构造：导出类型中文名-起止日期（yyyyMMdd-yyyyMMdd）.xlsx', () => {
    expect(buildStatisticsExportDownloadName(2, '2026-09-01', '2026-09-30')).toBe(
      '接收侧提醒明细-20260901-20260930.xlsx',
    )
    expect(buildStatisticsExportDownloadName(6, '2026-02-01', '2026-02-28')).toBe(
      '来源医院被互认汇总-20260201-20260228.xlsx',
    )
  })

  it('日期形态非法时就地失败，不构造下载名', () => {
    expect(() => buildStatisticsExportDownloadName(1, '2026/09/01', '2026-09-30')).toThrow()
    expect(() => buildStatisticsExportDownloadName(1, '2026-09-01', '')).toThrow()
  })
})
