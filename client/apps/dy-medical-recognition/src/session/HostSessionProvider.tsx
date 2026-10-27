import type { ReactNode } from 'react'
import { HostSessionContext } from './HostSessionContext'
import { INITIAL_HOST_SESSION, useHostSessionSnapshot } from './hostSession'

/**
 * 宿主会话 Provider：会话唯一来源是共享模块的快照。
 *
 * Provider 自身不持有副本、不单独读取：Context 值直接取自 `useHostSessionSnapshot`，
 * 与不依赖 Provider 的页面读取同一份已接受快照。首次尚无快照时使用稳定的初始值，
 * 不伪造身份；读取失败由本模块在渲染期抛出原始失败值，依赖上层 React 错误处理。
 */
export function HostSessionProvider({ children }: { children: ReactNode }) {
  const session = useHostSessionSnapshot() ?? INITIAL_HOST_SESSION
  return <HostSessionContext.Provider value={session}>{children}</HostSessionContext.Provider>
}
