import { useMemo, type ReactNode } from 'react'
import { createAuthenticatedAdapter } from '@dy/auth'
import { createMedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import { ApiClientContext } from './ApiClientContext'

/**
 * API Client 的 Provider：单独成文件，使 `ApiClientContext.tsx` 只导出 hook、
 * 本文件只导出组件（react-refresh 的 fast refresh 要求每个文件要么全是组件要么全非组件）。
 * 组件消费方从这里导入 Provider，hook 消费方导入路径不变。
 */
export function ApiClientProvider({ baseUrl, children }: { baseUrl: string; children: ReactNode }) {
  const client = useMemo(() => {
    const adapter = createAuthenticatedAdapter(baseUrl)
    return createMedicalRecognitionClient(adapter)
  }, [baseUrl])

  return <ApiClientContext.Provider value={client}>{children}</ApiClientContext.Provider>
}
