import { createContext, useContext } from 'react'
import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'

/** 供 Provider 文件注入 Client 的 Context；消费方使用 `useApiClientContext`。 */
export const ApiClientContext = createContext<MedicalRecognitionClient | null>(null)

export function useApiClientContext(): MedicalRecognitionClient {
  const client = useContext(ApiClientContext)
  if (!client) throw new Error('useApiClientContext must be used within ApiClientProvider')
  return client
}
