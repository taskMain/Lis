/**
 * 互认项目页面的写路径门控：由页面裁决后传给全部写界面（弹窗与行内编辑器）。
 *
 * 独立成模块的原因：写界面分散在弹窗与项目行两处，而「写被冻结时的可见原因」只应有一份判定，
 * 否则两处各写一遍次序与文案，同一个阻断会给出两种说法。
 *
 * 边界：本模块只把页面已裁决的事实转成可见原因，不读取组织、不判断读取状态、不发起请求，
 * 也不参与错误处理。
 */

/**
 * 写路径门控：由页面裁决后传入各写界面。
 *
 * 组织未对齐（可信组织已变化、页面尚未应用，或提交时刻复核现值失败）、读取失败或读取在途时，
 * 写界面禁用提交并给出原因，使已经打开的界面不会把输入写进旧组织、也不会基于不完整数据提交；
 * 门控只影响提交可用性与提示，不改变提交载荷，也不参与错误处理。
 */
export interface RecognitionProjectWriteGate {
  /** 页面实际展示的组织是否已与可信组织对齐。 */
  organizationReady: boolean
  /** 页面裁决的写能力：组织对齐、读取未失败、读取不在途且无在途提交。 */
  canWrite: boolean
  /** 读取失败态；`null` 表示本次读取未失败。 */
  loadFailure: null | 'load' | 'refreshAfterWrite'
  /**
   * 页面在**提交时刻**复核可信组织现值后留下的原因；`null` 表示提交期内没有发生过复核失败。
   * 与 `organizationReady` 同属组织类阻断，只由页面裁决，写界面只负责显示。
   */
  organizationRecheckFailure: string | null
}

/**
 * 写界面被冻结时的可见原因，`null` 表示当前可提交。与页面顶部的阻断提示同口径，
 * 写界面内只复述页面已裁决的事实，不自行判断组织或读取状态。
 *
 * 判定次序固定为「组织未对齐（含提交时刻复核失败）→ 读取失败（写入后的刷新失败单列）→ 读取在途」；
 * `submitting` 不是阻断原因，它只决定提交按钮保持可点（由页面去重）并用提交中状态反馈，
 * 因此不再在函数入口提前返回——提交在途期间发生的组织变化、读取失败必须照常可见，不被掩盖。
 */
export function writeBlockedReason(writeGate: RecognitionProjectWriteGate, submitting: boolean): string | null {
  // 提交期复核失败与渲染期「组织未对齐」同属组织类阻断，但它是更晚、更具体的事实
  // （渲染期门控可能还是上一帧的结论），因此先给出它。
  if (writeGate.organizationRecheckFailure !== null) {
    return writeGate.organizationRecheckFailure
  }
  if (!writeGate.organizationReady) {
    return '宿主组织已变化，页面尚未对齐到新组织。组织未对齐期间不能发起写入，请先放弃未保存内容并切换组织。'
  }
  if (writeGate.loadFailure === 'refreshAfterWrite') {
    return '写入已经成功，但重新读取配置与标准项目失败。维护操作暂不可用，请先重试读取。'
  }
  if (writeGate.loadFailure !== null) {
    return '读取失败，已有数据保留。维护操作暂不可用，请先重试读取。'
  }
  // `canWrite` 里还含有「提交在途」这一分量，它不是读取类阻断：只在非提交在途时给出读取在途的原因。
  if (!writeGate.canWrite && !submitting) {
    return '正在读取当前组织的配置与标准项目，完成前不能提交写入。'
  }
  return null
}
