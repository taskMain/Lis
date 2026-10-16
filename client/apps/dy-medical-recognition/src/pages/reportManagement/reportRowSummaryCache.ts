import type { ReportRow } from './reportsApi'

/**
 * 来源行概要的会话级缓存（D-03 返回还原的行概要通道）。
 *
 * 宿主 MicroSDK 接管 BrowserRouter 的 history，`navigate(path, { state })` 携带的 state
 * 在 SDK 包装的 push 中被丢弃，列表行概要改经本缓存传递：键为路径参数 `reportId`，
 * 详情页按路径取用。缓存未命中即深链直开口径（缺失字段按「—」呈现，D-02 语义不变；
 * 浏览器刷新后模块状态随页面会话重建，同样按深链口径）。
 *
 * 仅普通 Map 存取，不设过期与容量机制：条目只在同一次页面会话内服务报告详情往返，
 * 同键重新进入详情时被新行覆盖。
 */
const rowSummaries = new Map<string, ReportRow>()

/** 记录进入详情前的来源行概要；同键重进覆盖为新行。 */
export function setRowSummary(reportId: string, row: ReportRow): void {
  rowSummaries.set(reportId, row)
}

/** 读取某个报告的来源行概要；未命中返回 `undefined`（按深链直开口径呈现）。 */
export function getRowSummary(reportId: string): ReportRow | undefined {
  return rowSummaries.get(reportId)
}

/** 清空全部来源行概要；供测试隔离使用（模块级状态不得跨用例残留）。 */
export function clearRowSummaries(): void {
  rowSummaries.clear()
}
