/**
 * 「报告详情」跨文件共用的展示出口：缺失占位、否定标识判定与滚动容器定位。
 *
 * 详情路由页（`ReportDetailPage`）、共享内容组件（`ReportDetailContent`）与列表主体
 * （`ReportManagementBoard`）之间逐字重复的事实收敛到这里；只承载纯函数与常量，
 * 不承载组件、状态或请求。
 */

/** 缺失值统一展示为短横线，使「来源未提供」与「深链直开没有该数据」在页面上是同一事实。 */
export const MISSING_TEXT = '—'

/** 文本展示出口：缺失或空白一律展示占位符。 */
export function missingText(value: string | null | undefined): string {
  const normalized = value?.trim() ?? ''
  return normalized.length > 0 ? normalized : MISSING_TEXT
}

/** 「空白或『否』视为无」的否定原文：来源保密标识与检验、检查的整体异常标识共用同一条否定判定，原文为「否」或空白时按「无」处理。 */
const NEGATIVE_FLAG = '否'

/** 判定原文是否按「无」处理：空白或等于给定否定原文（默认「否」）。 */
export function treatedAsNone(value: string | null | undefined, flag: string = NEGATIVE_FLAG): boolean {
  const normalized = value?.trim() ?? ''
  return normalized.length === 0 || normalized === flag
}

/**
 * 整页滚动容器定位：MainLayout 的 Content（overflow:auto）承载整页滚动，
 * `ant-layout-content` 类名是 antd 布局的既有 DOM 出口，字符串集中一处声明。
 */
export function scrollContainerOf(root: HTMLElement | null): HTMLElement | null {
  return root?.closest<HTMLElement>('.ant-layout-content') ?? null
}

/** 证件号码展示截断的触发长度：中国居民身份证号（含校验位）为 18 位；阈值只作展示触发线，不作数据有效性判断。 */
export const ID_NUMBER_DISPLAY_MAX = 18

const ID_NUMBER_HEAD = 6
const ID_NUMBER_TAIL = 4

/** 证件号码的展示截断：超过身份证标准长度即中段省略（保留前 6 后 4），全文经 Tooltip 呈现。 */
export function truncateIdNumber(value: string): { text: string; truncated: boolean } {
  if (value.length <= ID_NUMBER_DISPLAY_MAX) return { text: value, truncated: false }
  return { text: `${value.slice(0, ID_NUMBER_HEAD)}…${value.slice(-ID_NUMBER_TAIL)}`, truncated: true }
}
