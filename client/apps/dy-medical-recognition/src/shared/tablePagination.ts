import type { TablePaginationConfig } from 'antd'

/** 共享 Table 受控分页所需的页面状态与事件边界。 */
export interface TablePaginationInput {
  pageIndex: number
  pageSize: number
  totalCount: number
  loading: boolean
  onChange: (pageIndex: number, pageSize: number) => void
}

/**
 * 服务端分页的页容量可选值；末位上限与服务端公共请求声明的取值域一致（1 到 200）。
 *
 * 该数组是本项目页容量可选值的唯一来源：页面不再各自声明一份，
 * 后端口径变化时只改这里，避免多处取值域漂移。
 */
export const PAGE_SIZE_OPTIONS = Object.freeze([10, 20, 50, 100, 200] as const)

/** 页容量可选值中的最大取值；与服务端声明的上限同一事实。 */
export const MAX_SELECTABLE_PAGE_SIZE: number = PAGE_SIZE_OPTIONS[PAGE_SIZE_OPTIONS.length - 1]

const tablePaginationPresentation: TablePaginationConfig = {
  size: 'small',
  placement: ['bottomEnd'],
  pageSizeOptions: [...PAGE_SIZE_OPTIONS],
  showSizeChanger: true,
  showQuickJumper: true,
  showTotal: (total) => `共 ${total} 条`,
}

/**
 * 把页面分页状态映射为无副作用的 Ant Design Table 受控分页配置。
 *
 * 分页控件位于表格下方、页容量可选、显示记录总数；总数来自服务端公共响应，不补造也不截断。
 * 改变页容量时回到第 1 页，翻页保持当前页容量，两者都由本函数统一收敛，页面不再各自实现。
 */
export function createTablePagination({
  pageIndex,
  pageSize,
  totalCount,
  loading,
  onChange,
}: TablePaginationInput): TablePaginationConfig {
  return {
    ...tablePaginationPresentation,
    current: pageIndex,
    pageSize,
    total: totalCount,
    disabled: loading,
    onChange: (nextPageIndex, nextPageSize) => {
      if (nextPageSize !== pageSize) {
        onChange(1, nextPageSize)
        return
      }

      onChange(nextPageIndex, pageSize)
    },
  }
}
