/**
 * 统计导出的下载处理：沿用阶段 4 报告 PDF 下载的既有模式。
 *
 * 已鉴权客户端取回工作簿字节后，按给定下载名触发浏览器保存，并在保存后释放对象地址；
 * 失败不产生本地错误提示（前端异常由宿主统一处理，本模块只按契约抛出）。
 *
 * 下载名说明：导出响应的文件名位于响应头 Content-Disposition，生成端（Kiota）只透出字节体、
 * 不透出响应头，因此下载名由调用方按发起时的导出能力与统计区间给定——
 * 与报告 PDF 先例一致（版本的下载名同样由调用方从查询元数据传入）。
 */

/** 导出工作簿的内容类型，与服务端导出响应声明一致。 */
const EXCEL_CONTENT_TYPE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'

/** 二进制片段类型别名：只用于把 `ArrayBuffer` 交给 `Blob`，不引入额外依赖。 */
type ArrayBufferPart = ArrayBuffer

/**
 * 把生成端返回的工作簿字节转为浏览器保存。
 *
 * 字节缺失或为 0 字节按契约错误抛出（服务端在零记录时按业务拒绝、不生成空文件，
 * 合法的 xlsx 工作簿也不会是 0 字节，因此空内容是契约破坏，保存只会得到打不开的文件）；
 * 对象地址在保存动作结束后释放，保存失败同样释放，不残留引用。
 */
export function saveExportWorkbook(content: ArrayBuffer | null | undefined, downloadFileName: string): void {
  if (content === undefined || content === null || content.byteLength === 0) throw new Error('统计导出未返回文件内容。')

  const objectUrl = URL.createObjectURL(new Blob([content as ArrayBufferPart], { type: EXCEL_CONTENT_TYPE }))
  try {
    const anchor = document.createElement('a')
    anchor.href = objectUrl
    anchor.download = downloadFileName
    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    URL.revokeObjectURL(objectUrl)
  }
}
