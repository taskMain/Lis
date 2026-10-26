import { useMemo, type ReactNode } from 'react'
import { createMedicalRecognitionApiClient } from '@dy/api-client-medical-recognition'
import { ApiClientContext } from './ApiClientContext'

/**
 * API Client 的 Provider：单独成文件，使 `ApiClientContext.tsx` 只导出 hook、
 * 本文件只导出组件（react-refresh 的 fast refresh 要求每个文件要么全是组件要么全非组件）。
 * 组件消费方从这里导入 Provider，hook 消费方导入路径不变。
 *
 * 请求适配器由宿主 MicroSDK 生成：401 上报与通用请求行为留在宿主适配器，
 * Authorization 统一取自宿主应用令牌，子应用不解析令牌、不读写浏览器存储、不跳转登录页。
 */
export function ApiClientProvider({ baseUrl, children }: { baseUrl: string; children: ReactNode }) {
  const client = useMemo(() => createMedicalRecognitionApiClient(baseUrl), [baseUrl])

  return <ApiClientContext.Provider value={client}>{children}</ApiClientContext.Provider>
}
