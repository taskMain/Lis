/**
 * 共享分页控件的契约用例（C7 的组件层分页配置面）。
 *
 * 本模块是跨页面共享的分页配置出口，页面只传状态与回调，不再各自拼装 antd 的受控分页字段；
 * 因此这里固定「取值域与服务端一致」「总数直传不补造」「改变页容量回到第 1 页」三条事实，
 * 防止后续阶段各自重写一套收敛规则。
 */
import { describe, expect, it, vi } from 'vitest'
import { createTablePagination, PAGE_SIZE_OPTIONS } from './tablePagination'

describe('共享分页控件', () => {
  it('页容量取值域与服务端公共请求声明一致，且上限不超过 200', () => {
    expect([...PAGE_SIZE_OPTIONS]).toEqual([10, 20, 50, 100, 200])
    for (const size of PAGE_SIZE_OPTIONS) {
      expect(Number.isInteger(size)).toBe(true)
      expect(size).toBeGreaterThanOrEqual(1)
      expect(size).toBeLessThanOrEqual(200)
    }
  })

  it('总数直传服务端返回值，不补造也不截断', () => {
    for (const totalCount of [0, 1, 60, 137, Number.MAX_SAFE_INTEGER]) {
      const config = createTablePagination({ pageIndex: 2, pageSize: 20, totalCount, loading: false, onChange: vi.fn() })
      expect(config.total).toBe(totalCount)
      expect(config.current).toBe(2)
      expect(config.pageSize).toBe(20)
    }
  })

  it('加载中禁用分页控件，加载结束恢复可用', () => {
    const loadingConfig = createTablePagination({ pageIndex: 1, pageSize: 20, totalCount: 1, loading: true, onChange: vi.fn() })
    expect(loadingConfig.disabled).toBe(true)

    const idleConfig = createTablePagination({ pageIndex: 1, pageSize: 20, totalCount: 1, loading: false, onChange: vi.fn() })
    expect(idleConfig.disabled).toBe(false)
  })

  it('改变页容量时回到第 1 页，翻页时保持当前页容量', () => {
    const onChange = vi.fn()
    const config = createTablePagination({ pageIndex: 3, pageSize: 20, totalCount: 137, loading: false, onChange })

    // 翻页：页码取新值，页容量不变。
    config.onChange?.(4, 20)
    expect(onChange).toHaveBeenLastCalledWith(4, 20)

    // 改变页容量：页码强制回到第 1 页。
    config.onChange?.(4, 50)
    expect(onChange).toHaveBeenLastCalledWith(1, 50)

    // 页容量相同（antd 在翻页时也会带上当前页容量）按翻页处理，不误判成改变页容量。
    onChange.mockClear()
    config.onChange?.(2, 20)
    expect(onChange).toHaveBeenLastCalledWith(2, 20)
  })
})
