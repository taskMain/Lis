import type { RouteObject } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { MainLayout } from '../layouts/MainLayout'
import { Home } from '../pages/Home'
import { StandardCatalog } from '../pages/standardCatalog/StandardCatalog'

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
      ...(prototypeRoute ? [prototypeRoute] : []),
    ],
  },
]
