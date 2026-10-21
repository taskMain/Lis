/** 统计明细展示的纯文本规则：归属名称缺失时统一显示「未知」。 */
export function partyName(value: string | null | undefined): string {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : '未知'
}
