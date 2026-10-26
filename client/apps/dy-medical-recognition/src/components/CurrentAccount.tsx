import { Space, Typography } from 'antd'
import { useHostSession } from '../session/HostSessionContext'

/**
 * 当前登录账号展示：只读取宿主会话，不发起任何业务请求。
 * 匿名、初始化、刷新中与错误状态不显示上一次身份。
 */
export function CurrentAccount() {
  const session = useHostSession()
  const authenticated = session.userId !== null
  return (
    <Space orientation='vertical' size={4}>
      <Typography.Text>当前账号：{authenticated ? session.account ?? '未知' : '未登录'}</Typography.Text>
      {authenticated ? <Typography.Text>显示名：{session.displayName ?? '未知'}</Typography.Text> : null}
    </Space>
  )
}
