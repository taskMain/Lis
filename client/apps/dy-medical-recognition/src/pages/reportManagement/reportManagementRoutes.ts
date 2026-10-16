/**
 * 「报告管理与历史版本」两个入口的父路由与详情子路由路径常量。
 *
 * 路由注册（`routes.tsx`）、入口容器的详情激活判定（`useMatch`）与详情页返回跳转
 * （`parentPath`）共用同一字面量来源，消除分散复写的路径字符串。
 */

/** 平台入口父路由段（相对形态，用于路由注册）。 */
export const REPORT_MANAGEMENT_PATH = 'report-management'
/** 医院入口父路由段（相对形态，用于路由注册）。 */
export const BRANCH_REPORT_MANAGEMENT_PATH = 'branch-report-management'
/** 详情子路由段（相对父路由）：报告标识取路径参数 `reportId`。 */
export const REPORT_DETAIL_SUBPATH = 'detail/:reportId'
/** 详情子路由的固定首段：列表行「查看详情」按它拼接报告标识跳转（与子路由注册同源）。 */
export const REPORT_DETAIL_SEGMENT = 'detail'

/** 平台入口列表路由（绝对路径）：详情页返回跳转的父路由。 */
export const REPORT_MANAGEMENT_ROUTE = `/${REPORT_MANAGEMENT_PATH}`
/** 医院入口列表路由（绝对路径）：详情页返回跳转的父路由。 */
export const BRANCH_REPORT_MANAGEMENT_ROUTE = `/${BRANCH_REPORT_MANAGEMENT_PATH}`
/** 平台入口详情子路由匹配模式：入口容器按它判定详情激活。 */
export const REPORT_MANAGEMENT_DETAIL_PATTERN = `${REPORT_MANAGEMENT_ROUTE}/${REPORT_DETAIL_SUBPATH}`
/** 医院入口详情子路由匹配模式：口径同平台入口。 */
export const BRANCH_REPORT_MANAGEMENT_DETAIL_PATTERN = `${BRANCH_REPORT_MANAGEMENT_ROUTE}/${REPORT_DETAIL_SUBPATH}`
