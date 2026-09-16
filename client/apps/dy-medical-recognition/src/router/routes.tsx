import type { RouteObject } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { MainLayout } from '../layouts/MainLayout'
import { Home } from '../pages/Home'
import { StandardCatalog } from '../pages/standardCatalog/StandardCatalog'
import { RecognitionProjects } from '../pages/recognitionProjects/RecognitionProjects'

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
      // 「互认项目」维护页：在**当前可信组织**内展示并维护该组织的标准项目互认配置
      // （新增、修改可互认时间、启用/停用）；供平台管理员与医院管理员使用，数据范围仅限
      // 登录凭证 `org` claim 对应的组织，页面不提供组织输入、不跨组织读取，也不额外过滤响应。
      { path: 'recognition-projects', element: <RecognitionProjects /> },
      ...(prototypeRoute ? [prototypeRoute] : []),
    ],
  },
]
