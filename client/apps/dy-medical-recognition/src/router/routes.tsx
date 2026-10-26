import type { RouteObject } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { MainLayout } from '../layouts/MainLayout'
import { Home } from '../pages/Home'
import { StandardCatalog } from '../pages/standardCatalog/StandardCatalog'
import { RecognitionProjects } from '../pages/recognitionProjects/RecognitionProjects'
import { RecognitionAmounts } from '../pages/recognitionAmounts/RecognitionAmounts'
import { BranchRecognitionAmounts } from '../pages/recognitionAmounts/BranchRecognitionAmounts'
import { ReportManagement } from '../pages/reportManagement/ReportManagement'
import { BranchReportManagement } from '../pages/reportManagement/BranchReportManagement'
import {
  BRANCH_REPORT_MANAGEMENT_PATH,
  REPORT_DETAIL_SUBPATH,
  REPORT_MANAGEMENT_PATH,
} from '../pages/reportManagement/reportManagementRoutes'
import { RecognitionUsageStatistics } from '../pages/recognitionStatistics/usageStatisticsPages'
import { BranchRecognitionUsageStatistics } from '../pages/recognitionStatistics/usageStatisticsPages'
import { SourceRecognitionStatistics } from '../pages/recognitionStatistics/sourceStatisticsPages'
import { BranchSourceRecognitionStatistics } from '../pages/recognitionStatistics/sourceStatisticsPages'
import { PlatformParameters } from '../pages/platformParameters/PlatformParameters'

const Prototype = import.meta.env.DEV
  ? lazy(() => import('../pages/prototype/StandardCatalogPrototype').then((module) => ({ default: module.StandardCatalogPrototype })))
  : null

const prototypeRoute = Prototype
  ? {
      path: 'prototype/standard-catalog',
      element: (
        <Suspense fallback={null}>
          <Prototype />
        </Suspense>
      ),
    }
  : null

export const routes: RouteObject[] = [
  {
    path: '/',
    element: <MainLayout />,
    children: [
      { index: true, element: <Home /> },
      { path: 'standard-catalog', element: <StandardCatalog /> },
      // 「平台参数维护」平台管理员页：维护本院报告匹配开关、排除时长与引用详情有效时长三项平台参数；
      // 页面只做取值规则的前端校验，保存校验仍由服务端执行；不实现角色判断，菜单名称与授权由权限系统配置。
      { path: 'platform-parameters', element: <PlatformParameters /> },
      // 「互认项目」维护页：在**当前可信组织**内展示并维护该组织的标准项目互认配置
      // （新增、修改可互认时间、启用/停用）；供平台管理员与医院管理员使用，数据范围仅限
      // 登录凭证 `org` claim 对应的组织，页面不提供组织输入、不跨组织读取，也不额外过滤响应。
      { path: 'recognition-projects', element: <RecognitionProjects /> },
      // 「互认项目金额维护」平台管理员页：组织、医院、院区三级由页面选择并随查询与保存请求提交，
      // 数据范围就是所选组织、医院与院区（服务端校验其存在、启用与父子归属）；页面不实现角色判断，
      // 也不做权限二次确认，菜单名称与授权由权限系统配置。
      { path: 'recognition-amounts', element: <RecognitionAmounts /> },
      // 「本院区互认项目金额」医院管理员页：组织与医院取可信上下文并以只读方式固定展示，只有院区可选；
      // 请求只提交院区与标准项目编码，组织与医院由服务端从可信上下文注入，前端不回填。
      { path: 'branch-recognition-amounts', element: <BranchRecognitionAmounts /> },
      // 「报告管理与历史版本」平台管理员页：组织、医院、院区三级由页面选择并随列表查询请求提交，
      // 数据范围就是所选组织、医院与院区（服务端校验其存在、启用与父子归属）；页面为只读页，
      // 只提供列表、历史版本查看与 PDF 下载，不实现角色判断，菜单名称与授权由权限系统配置。
      {
        path: REPORT_MANAGEMENT_PATH,
        element: <ReportManagement />,
        // 「报告详情」平台入口子路由：报告标识取路径 `reportId`，经列表行「查看详情」进入或深链直开；
        // 详情页由入口容器条件渲染（激活时列表保持挂载、仅隐藏），此处只注册路径供 URL 匹配，
        // 不渲染独立元素。数据范围同列表入口，页面只读。
        children: [{ path: REPORT_DETAIL_SUBPATH }],
      },
      // 「本院报告管理与历史版本」医院管理员页：组织与医院取可信上下文并以只读方式固定展示，
      // 只有院区可选且必须属于可信医院；请求不提交组织与医院两项，由服务端从可信上下文注入。
      {
        path: BRANCH_REPORT_MANAGEMENT_PATH,
        element: <BranchReportManagement />,
        // 「本院报告详情」医院入口子路由：口径同平台入口详情子路由，数据范围同本院列表入口（只读）。
        children: [{ path: REPORT_DETAIL_SUBPATH }],
      },
      // 「使用方互认使用统计」平台管理员页：使用方与提供方的组织、医院、院区三级都由页面选择
      // 并随汇总、明细与导出请求提交（服务端校验取值的组织归属）；页面为只读统计页，
      // 不实现角色判断，菜单名称与授权由权限系统配置。
      { path: 'recognition-usage-statistics', element: <RecognitionUsageStatistics /> },
      // 「本院互认使用统计」医院管理员页：本侧使用方组织与医院取可信上下文并固定展示，
      // 本侧院区可选（为空按可信医院全院统计）；来源组只提交医院与院区（提供方组织恒为可信组织），
      // 请求不提交本侧组织与医院两项，由服务端从可信上下文注入。
      { path: 'branch-recognition-usage-statistics', element: <BranchRecognitionUsageStatistics /> },
      // 「提供方医院被互认统计」平台管理员页：使用方与提供方的组织、医院、院区三级都由页面选择
      // 并随请求提交；只统计被互认次数，无金额、明细类型切换与不采纳原因区。
      { path: 'source-recognition-statistics', element: <SourceRecognitionStatistics /> },
      // 「本院被互认统计」医院管理员页：提供方组织与提供方医院取可信上下文并固定展示，
      // 提供方院区可选（为空按可信医院全部提供方院区统计）；接收组只提交医院与院区。
      { path: 'branch-source-recognition-statistics', element: <BranchSourceRecognitionStatistics /> },
      ...(prototypeRoute ? [prototypeRoute] : []),
    ],
  },
]
