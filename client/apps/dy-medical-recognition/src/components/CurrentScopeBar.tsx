import { LoginUserManager } from '@dy/auth'
import { useAuth } from '@dy/auth-react'
import { Tooltip, Typography } from 'antd'
import type { CSSProperties } from 'react'
import { useApiClientContext } from '../contexts/ApiClientContext'
import { useScopeNames } from './scopeNameLookup'

/** 单行省略样式：超长省略号截断，全文经 Tooltip 展示（视觉宽度表现属宿主浏览器验收面）。 */
const SINGLE_LINE_ELLIPSIS: CSSProperties = {
  display: 'block',
  maxWidth: '100%',
  overflow: 'hidden',
  whiteSpace: 'nowrap',
  textOverflow: 'ellipsis',
}

/** 编码空白判定与既有可信范围接缝同一口径：trim 后长度 0 视为缺失。 */
function readScopeCode(value: string | null): string {
  return (value ?? '').trim()
}

/**
 * 页头当前范围条（共用）：单行纯文字「组织名 / 医院名 / 院区名」，超长单行省略并以 Tooltip 展示全文。
 *
 * - 编码取自 `@dy/auth` 的登录管理器，token 变化即重新求值（与各页可信范围接缝同一写法）；
 *   院区恒取登录院区（不提供院区选择），组件不提供任何范围选择。
 * - 只显名称（C1）：组织名取本项目客户端「可信范围查询」交付的 organizationName，医院、院区名经
 *   基础目录按编码反查；某级名称缺失或反查失败显示「未知」，绝不回退编码，页头不出现任何编码文本。
 */
export function CurrentScopeBar() {
  const client = useApiClientContext()
  const { token } = useAuth()
  // token 缺失时凭据不表达可信范围，三级一律按缺失处理，也不发起反查。
  const organizationCode = token ? readScopeCode(LoginUserManager.getOrgID()) : ''
  const hospitalCode = token ? readScopeCode(LoginUserManager.getHosID()) : ''
  const branchCode = token ? readScopeCode(LoginUserManager.getBranchID()) : ''
  const names = useScopeNames(client, { branchCode, hospitalCode, organizationCode })
  const scopeText = [
    names.organization ?? '未知',
    names.hospital ?? '未知',
    names.branch ?? '未知',
  ].join(' / ')
  return (
    <Tooltip title={scopeText}>
      <Typography.Text className='current-scope-bar' style={SINGLE_LINE_ELLIPSIS}>{scopeText}</Typography.Text>
    </Tooltip>
  )
}
