import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import { MicroSDK } from '@dy/micro-sdk'
import { SubAppThemeProvider, setupSubAppDateLocale } from '@dy/micro-sdk/react'
import { AuthProvider } from '@dy/auth-react'
import { AppRoutes } from './router'
import { ApiClientProvider } from './contexts/ApiClientContext'
import './index.css'

// 子应用 base 路径，独立运行时作为 BrowserRouter basename
const subAppBase = '/subApps/medical-recognition'

// 配置文件读取失败时使用本地开发后端地址
const fallbackApiBaseUrl = 'http://localhost:5008'

// 开发环境读取 config.development.json，生产环境读取 config.json
const loadConfig = async () => {
  try {
    const configFile = import.meta.env.DEV
      ? `${subAppBase}/config.development.json`
      : `${subAppBase}/config.json`
    const response = await fetch(configFile)
    const config = await response.json()
    return config.apiBaseUrl || fallbackApiBaseUrl
  } catch {
    return fallbackApiBaseUrl
  }
}

// 初始化应用
const initApp = async () => {
  setupSubAppDateLocale(dayjs)

  // 微前端环境下使用主应用注入的基础路由，独立运行时使用固定路径
  const basename = window.__MICRO_APP_BASE_ROUTE__ || subAppBase

  const apiBaseUrl = await loadConfig()

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
          <ApiClientProvider baseUrl={apiBaseUrl}>
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
