import { useEffect, useMemo, useState } from 'react'
import type { EnumMetadataItemDto, MedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import { useApiClientContext } from '../contexts/ApiClientContext'

/** 枚举元数据选项；`name` 是成员名称（用于排查），`label` 是服务端声明的中文。 */
export type EnumMetadataOption = { value: number; label: string; name: string }

const clientCaches = new WeakMap<MedicalRecognitionClient, Map<string, Promise<EnumMetadataOption[]>>>()

/**
 * 子应用级枚举元数据缓存：同一 API Client 与同一枚举名只请求一次。
 *
 * 中文的唯一来源是服务端枚举声明；本模块只取用，不在前端维护第二份文案。
 * 同步异常（如客户端未提供该端点）同样并入失败路径，保证调用方始终能拿到兜底选项。
 *
 * 缓存口径（业务理由，自足）：枚举元数据**不是组织范围数据**——服务端 `GetEnumMetadata` 只接受
 * `enumName`（无组织入参），返回的是启动时由 SourceGen 描述器投影出的进程级静态表
 * （枚举成员取值 + `[Description]` 中文），不读业务数据、不按调用方组织过滤。因此缓存身份是
 * 「API Client 实例 + 枚举名」：同一 client 换组织时数据完全相同，按组织再分维度只会重复请求同一份数据。
 * 若将来该接口改为按组织交付文案，缓存键与失效时机必须同步加入身份维度，不能沿用当前口径。
 */
export function loadEnumMetadataOptions(client: MedicalRecognitionClient, enumName: string): Promise<EnumMetadataOption[]> {
  let cache = clientCaches.get(client)
  if (!cache) {
    cache = new Map()
    clientCaches.set(client, cache)
  }

  const cached = cache.get(enumName)
  if (cached) return cached

  const request = Promise.resolve()
    .then(() => client.api.enumMetadata.getEnumMetadata.post({ enumName }))
    .then((items) => (items ?? []).flatMap(toOption))
  cache.set(enumName, request)
  // 失败请求不留在缓存里，否则一次网络抖动会让本次会话永久退回兜底文案。
  // 这里按身份校验是防御性写法（当前缓存项只会是该请求本身），不声称覆盖了并发替换场景。
  request.catch(() => {
    // 只清除仍然是本次请求的那一项（缓存项当前只会是该请求本身，故该判等是防御性写法）。
    if (cache.get(enumName) === request) cache.delete(enumName)
  })
  return request
}

/**
 * 把服务端条目映射为选项；数值或成员名称缺失的条目不产出选项。
 * 入参直接用生成契约交付的类型 `EnumMetadataItemDto`，不在此重复声明一份结构。
 */
function toOption(item: EnumMetadataItemDto): EnumMetadataOption[] {
  if (item.value === null || item.value === undefined || !item.name) return []
  return [{ value: item.value, name: item.name, label: item.description?.trim() || item.name }]
}

/** `useEnumMetadata` 的可选参数。 */
export interface UseEnumMetadataOptions {
  /**
   * 是否允许在本次挂载中读取枚举元数据。默认 `true`。
   *
   * 传 `false` 时不发请求、直接返回兜底选项；调用方在上下文不可用（页面整体不渲染、拿不到任何需要
   * 枚举文案的区域）时传 `false`，避免明知用不到还发起一次请求。该开关只改请求时机，
   * 不参与缓存口径，也不引入新的数据来源。
   */
  enabled?: boolean
}

/**
 * 枚举元数据选项：加载成功时返回服务端声明，加载失败或返回空集合时保留本地兜底选项。
 * 加载失败不改判为页面错误，API 错误仍由宿主统一展示；调用方始终拿到可直接渲染的选项。
 *
 * `fallback` 需要是**稳定引用**（模块级常量或 `useMemo` 结果）：返回值会直接进入调用方的 `useMemo`
 * 与列表组件的 props，每次渲染传新数组会放大重渲染。
 *
 * `enumName` 或 API Client 在同一次挂载内变化时立即回到兜底选项，不残留上一个枚举或上一个客户端的结果；
 * 换回一个此前已加载过的枚举名时，会先经过一帧兜底选项再显示缓存结果。
 *
 * `options.enabled` 变为 `false` 时同样立即回到兜底选项，且不再发起新请求（已在途的请求按下面的边界丢弃结果）。
 *
 * 返回只读数组：已加载路径返回的是会话级缓存里那一份，兜底路径每次复制数组，
 * 但两条路径的**选项对象都是共享引用**；调用方不得就地修改数组或元素属性。
 *
 * 规范边界（已核对，同 `RecognitionProjects` 的在途请求作废机制）：生成客户端不支持取消——
 * `@microsoft/kiota-abstractions` 的 `RequestConfiguration<T>` 没有 `AbortSignal` 入口，
 * 生成方法也只能原样调用适配器。因此本 hook 只能**作废结果**（卸载或换枚举后丢弃迟到响应），
 * 不能中止在途请求；这是客户端契约的限制，不在本层自建底层 HTTP 调用来规避。
 */
export function useEnumMetadata(
  enumName: string,
  fallback: readonly EnumMetadataOption[],
  options: UseEnumMetadataOptions = {},
): readonly EnumMetadataOption[] {
  const client = useApiClientContext()
  const enabled = options.enabled ?? true
  const [loaded, setLoaded] = useState<{ client: MedicalRecognitionClient; enumName: string; options: EnumMetadataOption[] } | null>(null)
  // 复制一次兜底选项并保持引用稳定：返回值直接进 `useMemo`/`Segmented`，每次渲染新建数组会放大重渲染。
  const fallbackOptions = useMemo(() => [...fallback], [fallback])

  useEffect(() => {
    if (!enabled) return
    let cancelled = false
    // `cancelled` 只作废本次挂载的结果：无法中止在途请求（见上方的规范边界说明）。
    loadEnumMetadataOptions(client, enumName)
      .then((items) => { if (!cancelled && items.length > 0) setLoaded({ client, enumName, options: items }) })
      .catch(() => undefined)
    return () => { cancelled = true }
  }, [client, enabled, enumName])

  return enabled && loaded !== null && loaded.client === client && loaded.enumName === enumName ? loaded.options : fallbackOptions
}
