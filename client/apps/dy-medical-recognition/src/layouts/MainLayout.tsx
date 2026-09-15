import { MicroSDK } from '@dy/micro-sdk'
import { Layout, theme } from 'antd'
import { useEffect } from 'react'
import { Outlet } from 'react-router-dom'

const { Content } = Layout

export function MainLayout() {
  const { token } = theme.useToken()

  useEffect(() => {
    return MicroSDK.setupMainLayout()
  }, [])

  return (
    <Layout className='micro-layout'>
      <Content style={{ padding: 24, background: token.colorBgLayout, overflow: 'auto' }}>
        <Outlet />
      </Content>
    </Layout>
  )
}
