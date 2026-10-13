/**
 * 页头范围条名称反查模块的单元用例（三级名称渲染与名称反查失败的取数面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. `@dy/api-client-base` 只替换 `createBaseApiClient`：替身交付与生成契约同形的
 *    `HospitalDto` / `BranchDto`（含 id、name、hosId、orgId），并记录 baseApiBaseUrl 入参；
 * 2. `../runtimeConfig` 替换为即时完成的运行时配置，避免用例触达 jsdom 之外的配置文件读取。
 *
 * 模块级缓存按编码跨用例共享（组件卸载不丢缓存的真实语义），因此每条用例使用互不重复的编码，
 * 用例之间互不污染；「失败不占缓存」用同一条编码在同一用例内先后验证。
 */
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'

const baseRef = vi.hoisted(() => ({
  current: null as unknown,
  baseUrls: [] as string[],
}))

vi.mock('@dy/api-client-base', () => ({
  createBaseApiClient: (baseUrl: string) => {
    baseRef.baseUrls.push(baseUrl)
    return baseRef.current
  },
}))

vi.mock('../runtimeConfig', () => ({
  loadRuntimeConfig: async () => ({ apiBaseUrl: 'http://api.test', baseApiBaseUrl: 'http://base.test' }),
}))

const { lookupBranchName, lookupHospitalName, lookupOrganizationName } = await import('./scopeNameLookup')

/** 构造与生成契约同形的目录替身 client；条目缺省即该编码返回 `undefined`（目录查无）。端点为 vi.fn 便于计数。 */
function createDirectoryClient(entries: {
  hospital?: Record<string, string | null>
  branch?: Record<string, string | null>
  hospitalReject?: boolean
  branchReject?: boolean
}) {
  return {
    api: {
      organization: {
        getHospitalById: {
          post: vi.fn(async ({ id }: { id: string }) => {
            if (entries.hospitalReject) throw new Error('目录接口失败')
            return entries.hospital && entries.hospital.id === id ? entries.hospital : undefined
          }),
        },
        getBranchById: {
          post: vi.fn(async ({ id }: { id: string }) => {
            if (entries.branchReject) throw new Error('目录接口失败')
            return entries.branch && entries.branch.id === id ? entries.branch : undefined
          }),
        },
      },
    },
  }
}

/** 构造承载「可信范围查询」端点的本项目 client 替身（与生成契约同形）。 */
function createMedicalClient(scope: { organizationCode: string; organizationName: string | null }) {
  return {
    api: {
      medicalRecognitionReportQuery: {
        queryTrustedOrganizationScope: { post: async () => scope },
      },
    },
  }
}

/** 替身 client 以最小结构冒充生成 client：只在测试装配边界做一次收窄。 */
function asClient(value: unknown): MedicalRecognitionClient {
  return value as MedicalRecognitionClient
}

beforeEach(() => {
  baseRef.current = null
  baseRef.baseUrls = []
})

describe('医院名称反查（基础目录 GetHospitalById）', () => {
  it('按编码取名称并剔除与编码一致的「（编码）」后缀（全角与半角括号）', async () => {
    baseRef.current = createDirectoryClient({
      hospital: { id: '0101', name: '县人民医院（0101）', orgId: 'ORG-1' },
    })
    await expect(lookupHospitalName('0101')).resolves.toBe('县人民医院')

    baseRef.current = createDirectoryClient({
      hospital: { id: '0102', name: '总分院(0102)', orgId: 'ORG-1' },
    })
    await expect(lookupHospitalName('0102')).resolves.toBe('总分院')
  })

  it('目录查无或名称为空时返回 null，不回退编码', async () => {
    baseRef.current = createDirectoryClient({})
    await expect(lookupHospitalName('L-HOS-3')).resolves.toBeNull()

    baseRef.current = createDirectoryClient({ hospital: { id: '0104', name: '   ', orgId: 'ORG-1' } })
    await expect(lookupHospitalName('0104')).resolves.toBeNull()
  })

  it('同一编码复用缓存只反查一次，不同编码各自反查；baseApiBaseUrl 取运行时配置', async () => {
    const client = createDirectoryClient({ hospital: { id: '0105', name: '缓存医院', orgId: 'ORG-1' } })
    const post = client.api.organization.getHospitalById.post
    baseRef.current = client

    await expect(lookupHospitalName('0105')).resolves.toBe('缓存医院')
    await expect(lookupHospitalName('0105')).resolves.toBe('缓存医院')
    expect(post).toHaveBeenCalledTimes(1)
    expect(baseRef.baseUrls).toEqual(['http://base.test'])

    await expect(lookupHospitalName('L-HOS-6')).resolves.toBeNull()
    expect(baseRef.baseUrls).toEqual(['http://base.test', 'http://base.test'])
  })

  it('反查失败不占缓存：同编码的下一次调用重新请求并拿到名称', async () => {
    const client = createDirectoryClient({ hospital: { id: '0107', name: '重试医院', orgId: 'ORG-1' } })
    const post = client.api.organization.getHospitalById.post
    baseRef.current = createDirectoryClient({ hospitalReject: true })

    await expect(lookupHospitalName('0107')).rejects.toThrow('目录接口失败')

    baseRef.current = client
    await expect(lookupHospitalName('0107')).resolves.toBe('重试医院')
    expect(post).toHaveBeenCalledTimes(1)
  })
})

describe('院区名称反查（基础目录 GetBranchById）', () => {
  it('按编码取名称并剔除「（编码）」后缀；失败不占缓存', async () => {
    baseRef.current = createDirectoryClient({ branch: { id: '0101001', name: '总院（0101001）', hosId: '0101', orgId: 'ORG-1' } })
    await expect(lookupBranchName('0101001')).resolves.toBe('总院')

    baseRef.current = createDirectoryClient({ branchReject: true })
    await expect(lookupBranchName('L-BRA-2')).rejects.toThrow('目录接口失败')

    baseRef.current = createDirectoryClient({ branch: { id: '0101002', name: '分院', hosId: '0101', orgId: 'ORG-1' } })
    await expect(lookupBranchName('0101002')).resolves.toBe('分院')
  })
})

describe('组织名称反查（本项目客户端可信范围查询）', () => {
  it('取可信范围查询交付的名称并剔除「（编码）」后缀', async () => {
    baseRef.current = createDirectoryClient({})
    const client = createMedicalClient({ organizationCode: 'L-ORG-1', organizationName: '组织一（L-ORG-1）' })
    await expect(lookupOrganizationName(asClient(client), 'L-ORG-1')).resolves.toBe('组织一')
  })

  it('回读组织与请求组织不一致时按失败处理，不缓存、不交付名称', async () => {
    baseRef.current = createDirectoryClient({})
    const stale = createMedicalClient({ organizationCode: 'L-ORG-B', organizationName: '别的组织' })
    await expect(lookupOrganizationName(asClient(stale), 'L-ORG-A')).rejects.toThrow()

    const fresh = createMedicalClient({ organizationCode: 'L-ORG-A', organizationName: '组织甲' })
    await expect(lookupOrganizationName(asClient(fresh), 'L-ORG-A')).resolves.toBe('组织甲')
  })
})
