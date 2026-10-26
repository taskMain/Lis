import { createContext, useContext } from 'react'
import type { HostSession } from './hostSession'

/** 供 Provider 文件注入宿主会话的 Context；消费方使用 `useHostSession`。 */
export const HostSessionContext = createContext<HostSession | null>(null)

/** 获取当前宿主会话；不在 Provider 内时立即失败，避免页面静默使用伪造身份。 */
export function useHostSession(): HostSession {
  const session = useContext(HostSessionContext)
  if (!session) throw new Error('useHostSession must be used within HostSessionProvider')
  return session
}
