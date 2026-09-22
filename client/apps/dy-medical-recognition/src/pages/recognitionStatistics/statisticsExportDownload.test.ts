/**
 * 统计导出下载处理的用例（阶段 6 前端测试矩阵 C8 的保存尾部单元面）。
 *
 * 装配边界与阶段 4 PDF 下载用例一致：jsdom 未实现对象地址与锚点保存，
 * 用受控替身记录「交给浏览器的内容、下载名与地址释放」；失败面只断言契约错误抛出，
 * 不验证宿主提示（宿主统一提示走浏览器验收）。
 */
import { describe, expect, it, vi } from 'vitest'
import { saveExportWorkbook } from './statisticsExportDownload'

describe('C8 导出保存：按响应字节构造工作簿 Blob，按下载名触发保存并释放对象地址', () => {
  it('字节交给 Blob（xlsx 内容类型），下载名透传，保存后释放对象地址', () => {
    const workbook = new ArrayBuffer(16)
    const createObjectURL = vi.fn((content: Blob | MediaSource) => {
      void content
      return 'blob:statistics-export'
    })
    const revokeObjectURL = vi.fn()
    const originalCreate = URL.createObjectURL
    const originalRevoke = URL.revokeObjectURL
    URL.createObjectURL = createObjectURL
    URL.revokeObjectURL = revokeObjectURL
    const clicked: HTMLAnchorElement[] = []
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicked.push(this)
    })

    try {
      saveExportWorkbook(workbook, '接收侧互认使用汇总.xlsx')

      expect(createObjectURL).toHaveBeenCalledTimes(1)
      expect(createObjectURL.mock.calls[0][0]).toBeInstanceOf(Blob)
      expect((createObjectURL.mock.calls[0][0] as Blob).type).toBe(
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      )
      expect(clicked).toHaveLength(1)
      expect(clicked[0].download).toBe('接收侧互认使用汇总.xlsx')
      expect(clicked[0].href).toBe('blob:statistics-export')
      // 对象地址在保存动作结束后释放，不残留引用。
      expect(revokeObjectURL).toHaveBeenCalledWith('blob:statistics-export')
      expect(document.querySelector('a[download="接收侧互认使用汇总.xlsx"]')).toBeNull()
    } finally {
      clickSpy.mockRestore()
      URL.createObjectURL = originalCreate
      URL.revokeObjectURL = originalRevoke
    }
  })

  it('生成端未返回字节时按契约错误抛出，不触发保存', () => {
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    try {
      expect(() => saveExportWorkbook(undefined, '导出.xlsx')).toThrow('统计导出未返回文件内容。')
      expect(() => saveExportWorkbook(null, '导出.xlsx')).toThrow('统计导出未返回文件内容。')
      expect(clickSpy).not.toHaveBeenCalled()
    } finally {
      clickSpy.mockRestore()
    }
  })

  it('返回 0 字节内容时按契约错误抛出，不产出打不开的空工作簿', () => {
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    try {
      expect(() => saveExportWorkbook(new ArrayBuffer(0), '导出.xlsx')).toThrow('统计导出未返回文件内容。')
      expect(clickSpy).not.toHaveBeenCalled()
    } finally {
      clickSpy.mockRestore()
    }
  })
})
