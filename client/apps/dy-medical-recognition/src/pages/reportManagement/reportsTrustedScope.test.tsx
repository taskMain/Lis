import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { ResolvedTrustedScope } from '../../components/currentOrganizationScope'
import { readTrustedBranchCode, readTrustedHospitalCode, useTrustedHospitalScope } from './reportsTrustedScope'

/**
 * 当前范围替身：宿主只交付实例标识，业务编码由当前范围接口交付，
 * 本文件按「接口已交付编码」的最小形态提供，用于验证报告接缝只取业务编码。
 *
 * 编码与实例标识刻意不同名（编码带 `-CODE`）：接缝若回落实例标识，断言立刻失败。
 */
const scopeStore = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  function build(version: string, hospitalCode: string | null, branchCode: string | null): ResolvedTrustedScope {
    const suffix = version.replace('v', '')
    return {
      status: 'ready',
      organizationId: `ORG-${suffix}-ID`,
      hospitalId: hospitalCode === null ? null : `HOS-${suffix}-ID`,
      branchId: branchCode === null ? null : `BRA-${suffix}-ID`,
      organizationCode: `ORG-${suffix}-CODE`,
      hospitalCode,
      branchCode,
    }
  }
  let current = build('v1', 'HOS-1-CODE', 'BRA-1-CODE')
  return {
    getScope: () => current,
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
    set(next: ResolvedTrustedScope) {
      current = next
      for (const listener of [...listeners]) listener()
    },
    setSilently(next: ResolvedTrustedScope) {
      current = next
    },
    reset() {
      current = build('v1', 'HOS-1-CODE', 'BRA-1-CODE')
      for (const listener of [...listeners]) listener()
    },
  }
})

vi.mock('../../session/hostSession', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../session/hostSession')>()
  return {
    ...actual,
    useHostSessionSnapshot: () => ({ status: 'authenticated', sessionVersion: 'v1', userId: 'user-1' }),
  }
})

/** 共享当前范围接口替身：渲染期与命令式读取都取同一份当前范围（编码 + 实例标识）。 */
vi.mock('../../components/currentOrganizationScope', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../components/currentOrganizationScope')>()
  const { useSyncExternalStore } = await import('react')
  return {
    ...actual,
    readCurrentOrganizationScope: () => scopeStore.getScope(),
    useResolvedTrustedScope: () => {
      useSyncExternalStore(scopeStore.subscribe, scopeStore.getScope)
      return scopeStore.getScope()
    },
  }
})

vi.mock('../../contexts/ApiClientContext', () => ({
  useApiClientContext: () => ({}),
}))

beforeEach(() => {
  scopeStore.reset()
})

describe('报告链路可信医院与院区取 IAM 组织业务编码', () => {
  it('命令式读取取当前范围的医院与院区业务编码，不回落实例标识', () => {
    expect(readTrustedHospitalCode()).toBe('HOS-1-CODE')
    expect(readTrustedBranchCode()).toBe('BRA-1-CODE')
  })

  it('工作身份切换后按新范围重算，命令式读取同步取到现值', () => {
    const { result } = renderHook(() => useTrustedHospitalScope())
    expect(result.current).toEqual({ kind: 'ready', hospitalCode: 'HOS-1-CODE', branchCode: 'BRA-1-CODE' })

    act(() => {
      scopeStore.set({
        status: 'ready',
        organizationId: 'ORG-2-ID',
        hospitalId: 'HOS-2-ID',
        branchId: 'BRA-2-ID',
        organizationCode: 'ORG-2-CODE',
        hospitalCode: 'HOS-2-CODE',
        branchCode: 'BRA-2-CODE',
      })
    })

    expect(result.current).toEqual({ kind: 'ready', hospitalCode: 'HOS-2-CODE', branchCode: 'BRA-2-CODE' })
    expect(readTrustedHospitalCode()).toBe('HOS-2-CODE')
    expect(readTrustedBranchCode()).toBe('BRA-2-CODE')
  })

  it('匿名与医院缺失范围返回不可用，不伪造医院或院区', () => {
    act(() => {
      scopeStore.set({
        status: 'unavailable',
        organizationId: null,
        hospitalId: null,
        branchId: null,
        organizationCode: null,
        hospitalCode: null,
        branchCode: null,
      })
    })
    const anonymous = renderHook(() => useTrustedHospitalScope())
    expect(anonymous.result.current).toEqual({ kind: 'unavailable' })
    expect(readTrustedHospitalCode()).toBeNull()

    act(() => {
      scopeStore.set({
        status: 'ready',
        organizationId: 'ORG-3-ID',
        hospitalId: null,
        branchId: null,
        organizationCode: 'ORG-3-CODE',
        hospitalCode: null,
        branchCode: null,
      })
    })
    const missing = renderHook(() => useTrustedHospitalScope())
    expect(missing.result.current).toEqual({ kind: 'unavailable' })
  })

  it('医院实例标识存在但业务编码为空时按不可用处理，不回落实例标识', () => {
    act(() => {
      scopeStore.set({
        status: 'ready',
        organizationId: 'ORG-4-ID',
        hospitalId: 'HOS-4-ID',
        branchId: 'BRA-4-ID',
        organizationCode: 'ORG-4-CODE',
        hospitalCode: null,
        branchCode: null,
      })
    })
    const missingCode = renderHook(() => useTrustedHospitalScope())
    expect(missingCode.result.current).toEqual({ kind: 'unavailable' })
    expect(readTrustedHospitalCode()).toBeNull()
    expect(readTrustedBranchCode()).toBeNull()
  })

  it('命令式读取取最近一次快照，不依赖渲染期闭包', () => {
    const { result } = renderHook(() => useTrustedHospitalScope())
    expect(result.current).toEqual({ kind: 'ready', hospitalCode: 'HOS-1-CODE', branchCode: 'BRA-1-CODE' })

    scopeStore.setSilently({
      status: 'ready',
      organizationId: 'ORG-2-ID',
      hospitalId: 'HOS-2-ID',
      branchId: 'BRA-2-ID',
      organizationCode: 'ORG-2-CODE',
      hospitalCode: 'HOS-2-CODE',
      branchCode: 'BRA-2-CODE',
    })

    expect(readTrustedHospitalCode()).toBe('HOS-2-CODE')
    expect(result.current).toEqual({ kind: 'ready', hospitalCode: 'HOS-1-CODE', branchCode: 'BRA-1-CODE' })
  })
})
