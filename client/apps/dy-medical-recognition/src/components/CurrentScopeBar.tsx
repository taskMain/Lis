import { Tooltip, Typography } from 'antd'
import type { CSSProperties } from 'react'
import { useApiClientContext } from '../contexts/ApiClientContext'
import { useHostSession } from '../session/HostSessionContext'
import { useCurrentOrganizationScope } from './currentOrganizationScope'

/** 单行省略样式：超长省略号截断，全文经 Tooltip 展示（视觉宽度表现属宿主浏览器验收面）。 */
const SINGLE_LINE_ELLIPSIS: CSSProperties = {
  display: 'block',
  maxWidth: '100%',
  overflow: 'hidden',
  whiteSpace: 'nowrap',
  textOverflow: 'ellipsis',
}

/**
 * 页头当前范围条（共用）：单行纯文字「医共体名 / 医院名 / 院区名」，超长单行省略并以 Tooltip 展示全文。
 *
 * - 三级标识与名称都来自本项目「当前组织范围」查询，按宿主会话的 `sessionVersion` 重算：
 *   身份或工作身份切换后名称随之更新，旧身份范围不残留。
 * - 只显名称：某一级名称缺失时显示「未知」，绝不回退显示组织实例标识或编码，页头不出现任何标识文本。
 * - 未登录与初始化状态不发起业务请求，三级一律显示「未知」。
 */
export function CurrentScopeBar() {
  const client = useApiClientContext()
  const session = useHostSession()
  const { scope } = useCurrentOrganizationScope(client, session)
  const scopeText = [
    scope.organizationName ?? '未知',
    scope.hospitalName ?? '未知',
    scope.branchName ?? '未知',
  ].join(' / ')
  return (
    <Tooltip title={scopeText}>
      <Typography.Text className='current-scope-bar' style={SINGLE_LINE_ELLIPSIS}>{scopeText}</Typography.Text>
    </Tooltip>
  )
}
