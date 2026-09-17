import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import { MicroSDK } from '@dy/micro-sdk'
import { SubAppThemeProvider, setupSubAppDateLocale } from '@dy/micro-sdk/react'
import { AuthProvider } from '@dy/auth-react'
import { configureBaseComponents } from '@dy/components-base'
import { AppRoutes } from './router'
import { ApiClientProvider } from './contexts/ApiClientContext'
import { loadRuntimeConfig, subAppBase } from './runtimeConfig'
import './index.css'

// 初始化应用
const initApp = async () => {
  setupSubAppDateLocale(dayjs)

  // 微前端环境下使用主应用注入的基础路由，独立运行时使用固定路径
  const basename = window.__MICRO_APP_BASE_ROUTE__ || subAppBase

  const runtimeConfig = await loadRuntimeConfig()

  // 组件库的 Base API 地址只在此初始化一次
  configureBaseComponents({ baseUrl: runtimeConfig.baseApiBaseUrl })

  // micro-app 生命周期钩子
  window.mount = () => {
    MicroSDK.mount()
  }
  window.unmount = () => {
    MicroSDK.unmount()
  }

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <SubAppThemeProvider>
        <AuthProvider>
          <ApiClientProvider baseUrl={runtimeConfig.apiBaseUrl}>
            <BrowserRouter basename={basename}>
              <AppRoutes />
            </BrowserRouter>
          </ApiClientProvider>
        </AuthProvider>
      </SubAppThemeProvider>
    </StrictMode>,
  )
}

initApp()
