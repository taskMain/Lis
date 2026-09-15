import { createContext, useContext, useMemo, type ReactNode } from 'react'
import { createAuthenticatedAdapter } from '@dy/auth'
import {
  createMedicalRecognitionClient,
  type MedicalRecognitionClient,
} from '@dy/api-client-medical-recognition'

const ApiClientContext = createContext<MedicalRecognitionClient | null>(null)

export function ApiClientProvider({ baseUrl, children }: { baseUrl: string; children: ReactNode }) {
  const client = useMemo(() => {
    const adapter = createAuthenticatedAdapter(baseUrl)
    return createMedicalRecognitionClient(adapter)
  }, [baseUrl])

  return <ApiClientContext.Provider value={client}>{children}</ApiClientContext.Provider>
}

export function useApiClientContext(): MedicalRecognitionClient {
  const client = useContext(ApiClientContext)
  if (!client) throw new Error('useApiClientContext must be used within ApiClientProvider')
  return client
}
