import { useEffect, useState, type ReactNode } from 'react'
import { HostSessionContext } from './HostSessionContext'
import { INITIAL_HOST_SESSION, readHostSession, subscribeHostSession, type HostSession } from './hostSession'

/**
 * 宿主会话 Provider：会话唯一来源是 `MicroSDK.auth.getSession` 与 `subscribe`。
 *
 * 先订阅再读取初始快照：身份切换事件先到达时丢弃迟到的初始结果，旧身份的响应不覆盖新状态。
 * 订阅在卸载时释放；认证异常由宿主流程处理，本模块不转换为页面文案。
 */
export function HostSessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<HostSession>(INITIAL_HOST_SESSION)

  useEffect(() => {
    let active = true
    let eventArrived = false
    const unsubscribe = subscribeHostSession((next) => {
      eventArrived = true
      if (active) setSession(next)
    })
    void readHostSession().then((initial) => {
      if (active && !eventArrived) setSession(initial)
    })
    return () => {
      active = false
      unsubscribe()
    }
  }, [])

  return <HostSessionContext.Provider value={session}>{children}</HostSessionContext.Provider>
}
