import { Tooltip } from 'antd'
import { MISSING_TEXT, missingText, truncateIdNumber } from './reportDetailShared'

/** 证件号码的展示组件：标准长度内原样呈现，超过身份证标准长度中段省略（保留前 6 后 4）并以 Tooltip 展示全文；阈值口径见 reportDetailShared。 */
export function IdNumberText(props: { value: string | null | undefined }) {
  const value = missingText(props.value)
  if (value === MISSING_TEXT) return <span>{value}</span>
  const { text, truncated } = truncateIdNumber(value)
  if (!truncated) return <span>{value}</span>
  return (
    <Tooltip title={value}>
      <span className='tnum'>{text}</span>
    </Tooltip>
  )
}
