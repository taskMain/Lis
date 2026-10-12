import { Alert } from 'antd'
import type { ReactNode } from 'react'

/** 阻断文案恒定，以「已阻断」收尾：查询与操作都被门禁拦下，不是普通警示。 */
const BLOCKED_TEXT = '无法读取当前登录组织或医院，已阻断本页查询与操作。'

/**
 * 可信范围不可用时的整页阻断面板（共用）：整页只渲染调用方传入的大标题与一条错误 Alert。
 * 标题由调用方按各页现有写法传入（本组件不约定标题层级）；阻断期间页面不渲染任何
 * 可发起查询的控件，查询与操作的阻断由调用方以早退分支保证。
 */
export function ScopeUnavailablePanel({ title }: { title: ReactNode }) {
  return (
    <>
      {title}
      <Alert type='error' showIcon title={BLOCKED_TEXT} />
    </>
  )
}
