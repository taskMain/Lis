import { createContext, useContext } from 'react'
import {
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
export interface PrototypeStore {
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

/** 供 Provider 文件注入数据的 Context；消费方使用 `usePrototypeStore`。 */
export const PrototypeStoreContext = createContext<PrototypeStore | null>(null)

export function usePrototypeStore(): PrototypeStore {
  const store = useContext(PrototypeStoreContext)
  if (!store) throw new Error('usePrototypeStore 必须在 PrototypeStoreProvider 内使用')
  return store
}

export type { Category, Group, StandardItem }
