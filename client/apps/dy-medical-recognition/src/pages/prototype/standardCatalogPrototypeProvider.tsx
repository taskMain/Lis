import { useCallback, useMemo, useState, type ReactNode } from 'react'
import {
  initialData,
  nextId,
  type PrototypeData,
} from './standardCatalogPrototypeData'
import { PrototypeStoreContext, type PrototypeStore } from './standardCatalogPrototypeStore'

/**
 * 原型内存数据的 Provider：单独成文件，使 `standardCatalogPrototypeStore.tsx`
 * 只导出 hook、类型与 Context，本文件只导出组件（react-refresh 的 fast refresh
 * 要求每个文件要么全是组件要么全非组件）。原型页面从这里导入 Provider。
 */
export function PrototypeStoreProvider({ children }: { children: ReactNode }) {
  const [data, setData] = useState<PrototypeData>(initialData)

  const createCategory = useCallback<PrototypeStore['createCategory']>((input) => {
    setData((current) => ({
      ...current,
      categories: [...current.categories, { id: nextId('c'), ...input, isValid: true }],
    }))
  }, [])

  const updateCategory = useCallback<PrototypeStore['updateCategory']>((id, input) => {
    setData((current) => ({
      ...current,
      categories: current.categories.map((category) => (category.id === id ? { ...category, ...input } : category)),
    }))
  }, [])

  const setCategoryValid = useCallback<PrototypeStore['setCategoryValid']>((id, isValid) => {
    setData((current) => ({
      ...current,
      categories: current.categories.map((category) => (category.id === id ? { ...category, isValid } : category)),
    }))
  }, [])

  const createGroup = useCallback<PrototypeStore['createGroup']>((input) => {
    setData((current) => ({
      ...current,
      groups: [...current.groups, { id: nextId('g'), ...input, isValid: true }],
    }))
  }, [])

  const updateGroup = useCallback<PrototypeStore['updateGroup']>((id, input) => {
    setData((current) => ({
      ...current,
      groups: current.groups.map((group) => (group.id === id ? { ...group, ...input } : group)),
    }))
  }, [])

  const setGroupValid = useCallback<PrototypeStore['setGroupValid']>((id, isValid) => {
    setData((current) => ({
      ...current,
      groups: current.groups.map((group) => (group.id === id ? { ...group, isValid } : group)),
    }))
  }, [])

  const createItem = useCallback<PrototypeStore['createItem']>((input) => {
    setData((current) => ({
      ...current,
      items: [...current.items, { id: nextId('i'), ...input, isValid: true }],
    }))
  }, [])

  const updateItemRemark = useCallback<PrototypeStore['updateItemRemark']>((id, remark) => {
    setData((current) => ({
      ...current,
      items: current.items.map((item) => (item.id === id ? { ...item, remark } : item)),
    }))
  }, [])

  const setItemValid = useCallback<PrototypeStore['setItemValid']>((id, isValid) => {
    setData((current) => ({
      ...current,
      items: current.items.map((item) => (item.id === id ? { ...item, isValid } : item)),
    }))
  }, [])

  const reset = useCallback(() => setData(initialData), [])

  const value = useMemo<PrototypeStore>(
    () => ({
      data,
      createCategory,
      updateCategory,
      setCategoryValid,
      createGroup,
      updateGroup,
      setGroupValid,
      createItem,
      updateItemRemark,
      setItemValid,
      reset,
    }),
    [data, createCategory, updateCategory, setCategoryValid, createGroup, updateGroup, setGroupValid, createItem, updateItemRemark, setItemValid, reset],
  )

  return <PrototypeStoreContext.Provider value={value}>{children}</PrototypeStoreContext.Provider>
}
