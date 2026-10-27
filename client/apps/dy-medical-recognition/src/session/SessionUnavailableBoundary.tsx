import { Component, type ReactNode } from 'react'
import { Button, Result } from 'antd'

/** 边界属性：children 为受保护的子树。 */
export interface SessionUnavailableBoundaryProps {
  children: ReactNode
}

/**
 * 会话失败边界：位于 `HostSessionProvider` 之上的不可操作状态出口。
 *
 * 位置由抛错点决定：Provider 自身在渲染期调用宿主会话钩子，包在其 children 内部的边界捕获不到
 * 该错误，因此本组件挂在 Provider 之上、主题提供者之内，并覆盖 Provider 之下全部消费者。
 *
 * 原始失败值只作为边界捕获结果保留：不进入界面文案，不转换为正常会话。
 */
export class SessionUnavailableBoundary extends Component<SessionUnavailableBoundaryProps, { failed: boolean }> {
  override state = { failed: false }

  static getDerivedStateFromError(): { failed: boolean } {
    return { failed: true }
  }

  override componentDidCatch(): void {
    // 承载状态已由 getDerivedStateFromError 标记；原始失败值不进入界面文案。
  }

  private readonly reloadPage = (): void => {
    window.location.reload()
  }

  override render(): ReactNode {
    if (!this.state.failed) return this.props.children
    return (
      <section role='alert' aria-live='assertive'>
        <Result
          status='warning'
          title='会话不可用'
          subTitle='页面已停止操作。请重新载入页面后继续使用。'
          extra={
            <Button type='primary' onClick={this.reloadPage}>
              重新载入
            </Button>
          }
        />
      </section>
    )
  }
}