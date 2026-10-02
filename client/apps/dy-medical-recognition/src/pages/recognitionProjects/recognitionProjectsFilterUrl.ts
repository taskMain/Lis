/**
 * 已生效筛选与地址栏查询参数的一致性（S2-D27）。
 *
 * 地址栏只承载**经提交生效**的编码筛选与配置状态筛选，不承载编码输入原文：输入原文是草稿，
 * 把它写进地址栏会让地址栏表达的"当前列表按什么条件裁出来"与实际裁剪条件不一致。
 *
 * 两个纯函数（解析与生成）与两个浏览器入口分开：纯函数承载全部口径，入口只负责读写当前位置，
 * 因此组件层可以用纯函数验证参数口径，不必依赖真实地址栏。
 *
 * 写入用替换当前历史项而不是新增：筛选是同一页面的视图状态，写入历史项会让宿主工作区的返回行为
 * 穿过一串筛选状态。读写都在失败时静默降级——筛选照常生效，只是地址栏不同步，不因地址栏问题阻断页面。
 */
import { type ConfigurationStatusValue, type RecognitionProjectFilter, EMPTY_RECOGNITION_PROJECT_FILTER } from './recognitionProjectsApi'

/** 地址栏里的配置状态取值：`all` 或枚举数值的十进制写法。 */
const STATUS_PARAM = 'status'
const KEYWORD_PARAM = 'keyword'

/** 把地址栏查询串解析为已生效筛选；参数缺失或取值不可识别时按该条件不生效处理。 */
export function parseFilterSearch(search: string): RecognitionProjectFilter {
  let params: URLSearchParams
  try {
    params = new URLSearchParams(search)
  } catch {
    return { ...EMPTY_RECOGNITION_PROJECT_FILTER }
  }
  const statusText = params.get(STATUS_PARAM)
  const status: 'all' | ConfigurationStatusValue = statusText === '1' || statusText === '2'
    ? (Number(statusText) as ConfigurationStatusValue)
    : 'all'
  return { keyword: params.get(KEYWORD_PARAM) ?? '', status }
}

/**
 * 在既有查询串上写入已生效筛选，返回新的查询串（含前导 `?`，无参数时返回空串）。
 * 其他参数原样保留：地址栏可能同时承载宿主或子应用自己的参数，页面只改自己这两个。
 */
export function buildFilterSearch(search: string, filter: RecognitionProjectFilter): string {
  let params: URLSearchParams
  try {
    params = new URLSearchParams(search)
  } catch {
    params = new URLSearchParams()
  }
  if (filter.keyword.length === 0) params.delete(KEYWORD_PARAM)
  else params.set(KEYWORD_PARAM, filter.keyword)
  if (filter.status === 'all') params.delete(STATUS_PARAM)
  else params.set(STATUS_PARAM, String(filter.status))
  const text = params.toString()
  return text.length === 0 ? '' : `?${text}`
}

/** 读取当前地址栏上的已生效筛选；读不到时返回空筛选（页面按无条件展示）。 */
export function readFilterFromLocation(): RecognitionProjectFilter {
  try {
    return parseFilterSearch(window.location.search)
  } catch {
    return { ...EMPTY_RECOGNITION_PROJECT_FILTER }
  }
}

/**
 * 把已生效筛选写回地址栏；内容未变化时不触达历史 API，避免每次渲染都写一次。
 */
export function syncFilterToLocation(filter: RecognitionProjectFilter): void {
  try {
    const search = buildFilterSearch(window.location.search, filter)
    if (search === window.location.search) return
    const url = `${window.location.pathname}${search}${window.location.hash}`
    window.history.replaceState(window.history.state, '', url)
  } catch {
    // 地址栏不可写（宿主或沙箱限制）时不阻断页面：筛选仍按本地状态生效。
  }
}
