import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import { loadEnumMetadataOptions, useEnumMetadata, type EnumMetadataOption } from './useEnumMetadata'

const FALLBACK: readonly EnumMetadataOption[] = [
  { value: 1, name: 'Enabled', label: '启用' },
  { value: 2, name: 'Disabled', label: '停用' },
]

/** 被测 hook 从上下文取 client；测试为每条用例换一个只提供枚举元数据端点的 client。 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

vi.mock('../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

/** 测试用 client：保持与生成契约相同的形状，只实现 hook 用到的枚举元数据端点。 */
function clientWith(post: (body: { enumName?: string | null }) => Promise<unknown>): MedicalRecognitionClient {
  return { api: { enumMetadata: { getEnumMetadata: { post } } } } as unknown as MedicalRecognitionClient
}

beforeEach(() => {
  clientRef.current = {}
})

describe('enum metadata cache', () => {
  it('maps backend metadata and reuses the request for the same client and enum', async () => {
    const post = vi.fn().mockResolvedValue([{ value: 1, name: 'Enabled', description: '启用' }])
    const client = clientWith(post)

    const first = loadEnumMetadataOptions(client, 'ConfigurationStatus')
    const second = loadEnumMetadataOptions(client, 'ConfigurationStatus')

    await expect(first).resolves.toEqual([{ value: 1, name: 'Enabled', label: '启用' }])
    await expect(second).resolves.toEqual([{ value: 1, name: 'Enabled', label: '启用' }])
    expect(post).toHaveBeenCalledTimes(1)
    expect(post).toHaveBeenCalledWith({ enumName: 'ConfigurationStatus' })
  })

  it('evicts a failed request so a later call can retry', async () => {
    const post = vi.fn().mockRejectedValueOnce(new Error('failed')).mockResolvedValueOnce([{ value: 2, name: 'Disabled', description: '停用' }])
    const client = clientWith(post)

    await expect(loadEnumMetadataOptions(client, 'ConfigurationStatus')).rejects.toThrow('failed')
    await expect(loadEnumMetadataOptions(client, 'ConfigurationStatus')).resolves.toEqual([{ value: 2, name: 'Disabled', label: '停用' }])
    expect(post).toHaveBeenCalledTimes(2)
  })

  it('turns a synchronous client failure into a rejected promise instead of throwing at the call site', async () => {
    await expect(loadEnumMetadataOptions({} as unknown as MedicalRecognitionClient, 'ConfigurationStatus')).rejects.toThrow()
  })

  it('skips entries without a value or a member name instead of rendering an unusable option', async () => {
    const post = vi.fn().mockResolvedValue([
      { value: null, name: 'Enabled', description: '启用' },
      { value: 2, name: null, description: '停用' },
      { value: 3, name: 'Other' },
    ])
    const client = clientWith(post)

    // 第三条没有 description 时退回成员名称，保证 label 非空；前两条被丢弃。
    await expect(loadEnumMetadataOptions(client, 'ConfigurationStatus')).resolves.toEqual([
      { value: 3, name: 'Other', label: 'Other' },
    ])
  })
})

describe('useEnumMetadata', () => {
  it('replaces the fallback with server declarations once loaded', async () => {
    const post = vi.fn().mockResolvedValue([
      { value: 1, name: 'Enabled', description: '已启用' },
      { value: 2, name: 'Disabled', description: '已停用' },
    ])
    clientRef.current = clientWith(post)

    const { result } = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))

    expect(result.current).toEqual([...FALLBACK])
    await waitFor(() => expect(result.current).toEqual([
      { value: 1, name: 'Enabled', label: '已启用' },
      { value: 2, name: 'Disabled', label: '已停用' },
    ]))
  })

  it('keeps the fallback when the server returns an empty list', async () => {
    const post = vi.fn().mockResolvedValue([])
    clientRef.current = clientWith(post)

    const { result } = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))
    await waitFor(() => expect(post).toHaveBeenCalledTimes(1))
    await act(async () => { await Promise.resolve() })

    expect(result.current).toEqual([...FALLBACK])
  })

  it('keeps the fallback and retries on a later mount after a failed request', async () => {
    // 失败降级的可判别断言：换一个 client 重新挂载时必须重新发请求（缓存已清），而不是永久沿用失败结果。
    const failing = vi.fn().mockRejectedValue(new Error('网络不可用'))
    clientRef.current = clientWith(failing)

    const first = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))
    await waitFor(() => expect(failing).toHaveBeenCalledTimes(1))
    expect(first.result.current).toEqual([...FALLBACK])
    first.unmount()

    const recovering = vi.fn().mockResolvedValue([{ value: 1, name: 'Enabled', description: '已启用' }])
    clientRef.current = clientWith(recovering)
    const second = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))

    await waitFor(() => expect(second.result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
    expect(recovering).toHaveBeenCalledTimes(1)
    expect(failing).toHaveBeenCalledTimes(1)
  })

  it('reuses the cached request for two hook instances instead of calling the endpoint again', async () => {
    const post = vi.fn().mockResolvedValue([{ value: 1, name: 'Enabled', description: '已启用' }])
    clientRef.current = clientWith(post)

    const first = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))
    const second = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))

    await waitFor(() => expect(first.result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
    await waitFor(() => expect(second.result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
    expect(post).toHaveBeenCalledTimes(1)
  })

  it('does not keep the previous enum options after the enum name changes, and returns to fallback when switching back', async () => {
    clientRef.current = clientWith(vi.fn().mockImplementation((body: { enumName?: string | null }) =>
      Promise.resolve(
        body.enumName === 'ConfigurationStatus'
          ? [{ value: 1, name: 'Enabled', description: '已启用' }]
          : [{ value: 9, name: 'Other', description: '其它枚举文案' }],
      ),
    ))

    const { result, rerender } = renderHook(({ enumName }) => useEnumMetadata(enumName, FALLBACK), {
      initialProps: { enumName: 'ConfigurationStatus' },
    })
    await waitFor(() => expect(result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))

    rerender({ enumName: 'MedicalItemType' })
    // 换名瞬间回退到兜底选项，不得继续展示上一个枚举的选项；新枚举结果到达后按新枚举展示。
    expect(result.current).toEqual([...FALLBACK])
    await waitFor(() => expect(result.current).toEqual([{ value: 9, name: 'Other', label: '其它枚举文案' }]))

    // 换回已加载过的枚举名：先经过一帧兜底（state 按 client + 枚举名键控），再由缓存结果回填。
    rerender({ enumName: 'ConfigurationStatus' })
    expect(result.current).toEqual([...FALLBACK])
    await waitFor(() => expect(result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
  })

  it('keeps the enum that is currently displayed when an earlier request resolves after the switch', async () => {
    // 卸载/切换守卫的承重场景：A 在途 → 切到 B 且 B 已显示 → A 迟到，不得把已显示的 B 回退成兜底。
    let resolveFirst!: (items: unknown) => void
    const post = vi.fn()
      .mockReturnValueOnce(new Promise((resolve) => { resolveFirst = resolve }))
      .mockResolvedValueOnce([{ value: 9, name: 'Other', description: 'B 的文案' }])
    clientRef.current = clientWith(post)

    const { result, rerender } = renderHook(({ enumName }) => useEnumMetadata(enumName, FALLBACK), {
      initialProps: { enumName: 'ConfigurationStatus' },
    })
    await waitFor(() => expect(post).toHaveBeenCalledTimes(1))

    rerender({ enumName: 'MedicalItemType' })
    await waitFor(() => expect(result.current).toEqual([{ value: 9, name: 'Other', label: 'B 的文案' }]))

    await act(async () => {
      resolveFirst([{ value: 1, name: 'Enabled', description: 'A 的文案' }])
      await Promise.resolve()
    })

    expect(result.current).toEqual([{ value: 9, name: 'Other', label: 'B 的文案' }])
  })

  it('tolerates a response that arrives after unmount', async () => {
    // 迟到的响应不得把结果泄漏到后续挂载：卸载后重新挂载会按新状态重新取值（缓存仍复用同一请求）。
    let resolveRequest!: (items: unknown) => void
    const post = vi.fn().mockReturnValue(new Promise((resolve) => { resolveRequest = resolve }))
    clientRef.current = clientWith(post)

    const first = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))
    await waitFor(() => expect(post).toHaveBeenCalledTimes(1))
    first.unmount()

    await act(async () => {
      resolveRequest([{ value: 1, name: 'Enabled', description: '已启用' }])
      await Promise.resolve()
    })

    const second = renderHook(() => useEnumMetadata('ConfigurationStatus', FALLBACK))
    await waitFor(() => expect(second.result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
    expect(post).toHaveBeenCalledTimes(1)
  })

  it('does not request the endpoint while the caller reports the options are unusable', async () => {
    // 调用方在上下文不可用时传 `enabled: false`：请求时机被抑制，直接给兜底选项。
    const post = vi.fn().mockResolvedValue([{ value: 1, name: 'Enabled', description: '已启用' }])
    clientRef.current = clientWith(post)

    const { result, rerender } = renderHook(
      ({ enabled }) => useEnumMetadata('ConfigurationStatus', FALLBACK, { enabled }),
      { initialProps: { enabled: false } },
    )

    await act(async () => { await Promise.resolve() })
    expect(post).not.toHaveBeenCalled()
    expect(result.current).toEqual([...FALLBACK])

    // 上下文恢复可用后必须真正发起读取（门控只抑制请求时机，不改缓存口径）。
    rerender({ enabled: true })
    await waitFor(() => expect(result.current).toEqual([{ value: 1, name: 'Enabled', label: '已启用' }]))
    expect(post).toHaveBeenCalledTimes(1)

    // 再次不可用时立刻回到兜底选项，不残留已加载结果。
    rerender({ enabled: false })
    expect(result.current).toEqual([...FALLBACK])
    expect(post).toHaveBeenCalledTimes(1)
  })
})
