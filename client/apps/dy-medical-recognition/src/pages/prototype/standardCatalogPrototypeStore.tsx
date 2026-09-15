import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import {
  initialData,
  nextId,
  type Category,
  type Group,
  type ItemType,
  type PrototypeData,
  type StandardItem,
} from './standardCatalogPrototypeData'

/**
 * 原型的内存数据容器。
 *
 * 三个变体共用同一份数据与同一套业务规则，切换变体时数据保持，
 * 便于在同一组状态上比较结构与交互组织。
 */
interface PrototypeStore {
  data: PrototypeData
  createCategory: (input: { itemType: ItemType; name: string; remark: string | null }) => void
  updateCategory: (id: string, input: { itemType: ItemType; name: string; remark: string | null }) => void
  setCategoryValid: (id: string, isValid: boolean) => void
  createGroup: (input: { categoryId: string; name: string; remark: string | null }) => void
  updateGroup: (id: string, input: { name: string; remark: string | null }) => void
  setGroupValid: (id: string, isValid: boolean) => void
  createItem: (input: { categoryId: string; groupId: string; code: string; name: string; remark: string | null }) => void
  updateItemRemark: (id: string, remark: string | null) => void
  setItemValid: (id: string, isValid: boolean) => void
  reset: () => void
}

const PrototypeStoreContext = createContext<PrototypeStore | null>(null)

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

export function usePrototypeStore(): PrototypeStore {
  const store = useContext(PrototypeStoreContext)
  if (!store) throw new Error('usePrototypeStore 必须在 PrototypeStoreProvider 内使用')
  return store
}

export type { Category, Group, StandardItem }
