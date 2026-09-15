import { Button, Card, Space, Typography } from 'antd'
import { MicroSDK } from '@dy/micro-sdk'

export function Home() {
  const inMicroApp = MicroSDK.isInMicroApp()
  return (
    <Card>
      <Space orientation="vertical" size="middle">
        <Typography.Title level={2}>检验检查结果互认平台</Typography.Title>
        <Typography.Text>微应用 code：<Typography.Text code>medical-recognition</Typography.Text></Typography.Text>
        <Typography.Text>运行模式：{inMicroApp ? '嵌入宿主' : '独立运行'}</Typography.Text>
        <Button type="primary" onClick={() => MicroSDK.navigate('/')}>通知宿主导航到首页</Button>
      </Space>
    </Card>
  )
}
