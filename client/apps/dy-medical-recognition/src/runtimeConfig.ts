// 子应用 base 路径，独立运行时作为 BrowserRouter basename，也是配置文件的挂载路径
export const subAppBase = '/subApps/medical-recognition'

// 本项目在 IAM 的微应用编码：宿主按该编码签发业务请求令牌
export const subAppCode = 'medical-recognition'

// 配置文件读取失败时使用本地开发后端地址
const fallbackApiBaseUrl = 'http://localhost:15014'

export interface RuntimeConfig {
  apiBaseUrl: string
}

// 开发环境读取 config.development.json，生产环境读取 config.json
export const loadRuntimeConfig = async (): Promise<RuntimeConfig> => {
  const fallback: RuntimeConfig = { apiBaseUrl: fallbackApiBaseUrl }
  try {
    const configFile = import.meta.env.DEV
      ? `${subAppBase}/config.development.json`
      : `${subAppBase}/config.json`
    const response = await fetch(configFile)
    const config = await response.json()
    return { apiBaseUrl: config.apiBaseUrl || fallbackApiBaseUrl }
  } catch {
    return fallback
  }
}
