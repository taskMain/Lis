import { useEffect, useState } from 'react'
import { createBaseApiClient } from '@dy/api-client-base'
import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import { loadRuntimeConfig } from '../runtimeConfig'

/** 页头范围条的三级名称；`null` 表示该级名称缺失或反查尚未成功，展示层一律按「未知」处理。 */
export interface ScopeNames {
  organization: string | null
  hospital: string | null
  branch: string | null
}

/**
 * 去掉名称尾部与编码一致的「（编码）」后缀（C1）：目录数据里名称字段可能自带编码后缀，
 * 直接渲染会让编码文本重新出现在页头。全角与半角括号都处理；剔除后为空则保留原名兜底。
 */
function stripCodeSuffix(code: string, name: string): string {
  if (code && name.endsWith(`（${code}）`)) return name.slice(0, -(code.length + 2)).trim() || name
  if (code && name.endsWith(`(${code})`)) return name.slice(0, -(code.length + 2)).trim() || name
  return name
}

/** 名称只显规则（C1）：trim 后为空视为缺失；自带「（编码）」后缀时剔除；绝不回退编码。 */
function toDisplayName(code: string, name: string | null | undefined): string | null {
  const trimmed = name?.trim()
  if (!trimmed) return null
  return stripCodeSuffix(code, trimmed)
}

/*
 * 模块级按编码缓存**成功**的反查结果（Promise 形态）：组件卸载不丢缓存，同一编码整个会话只查一次；
 * 失败的请求不占缓存（只清理属于自己的条目，避免较早请求的 reject 覆盖并发新请求的缓存），
 * 下一次进入页面按同编码重新请求。
 */
const organizationNamePromises = new Map<string, Promise<string | null>>()
const hospitalNamePromises = new Map<string, Promise<string | null>>()
const branchNamePromises = new Map<string, Promise<string | null>>()

function keepCache(map: Map<string, Promise<string | null>>, key: string, promise: Promise<string | null>): void {
  map.set(key, promise)
  void promise.catch(() => {
    if (map.get(key) === promise) map.delete(key)
  })
}

/**
 * 反查组织名：取本项目客户端「可信范围查询」交付的 organizationName（与页面适配层同源、同一出口，
 * 组件侧自建等价调用，不改该适配层文件的既有导出）。回读编码与请求编码不一致说明凭证已换：
 * 按失败处理，不缓存也不交付名称，避免把别的组织名称留在页头。
 */
export function lookupOrganizationName(client: MedicalRecognitionClient, organizationCode: string): Promise<string | null> {
  const cached = organizationNamePromises.get(organizationCode)
  if (cached) return cached
  const current = (async () => {
    const scope = await client.api.medicalRecognitionReportQuery.queryTrustedOrganizationScope.post()
    if (scope?.organizationCode !== organizationCode) {
      throw new Error('可信范围查询回读的组织与请求不一致')
    }
    return toDisplayName(organizationCode, scope.organizationName ?? null)
  })()
  keepCache(organizationNamePromises, organizationCode, current)
  return current
}

/**
 * 经基础目录按编码反查医院名。`createBaseApiClient` 内部即鉴权适配器（TokenManager 共享存储），
 * 自动携带宿主登录 token，无需自行注入鉴权；`baseApiBaseUrl` 取运行时配置。
 */
export function lookupHospitalName(hospitalCode: string): Promise<string | null> {
  const cached = hospitalNamePromises.get(hospitalCode)
  if (cached) return cached
  const current = (async () => {
    const { baseApiBaseUrl } = await loadRuntimeConfig()
    const client = createBaseApiClient(baseApiBaseUrl)
    const hospital = await client.api.organization.getHospitalById.post({ id: hospitalCode })
    return toDisplayName(hospitalCode, hospital?.name ?? null)
  })()
  keepCache(hospitalNamePromises, hospitalCode, current)
  return current
}

/** 经基础目录按编码反查院区名（院区恒取登录院区，编码即唯一入参）。 */
export function lookupBranchName(branchCode: string): Promise<string | null> {
  const cached = branchNamePromises.get(branchCode)
  if (cached) return cached
  const current = (async () => {
    const { baseApiBaseUrl } = await loadRuntimeConfig()
    const client = createBaseApiClient(baseApiBaseUrl)
    const branch = await client.api.organization.getBranchById.post({ id: branchCode })
    return toDisplayName(branchCode, branch?.name ?? null)
  })()
  keepCache(branchNamePromises, branchCode, current)
  return current
}

/**
 * 页头范围条的三级名称解析。编码齐备才发起该级反查；反查失败保持 `null`
 * （错误由宿主统一提示，这里空 catch，页面不自行渲染错误）。
 * 竞态防护：effect 清理后到达的旧响应一律丢弃——编码变化后旧层级名称不得覆盖新状态。
 * 空白编码不在 effect 里写状态（避免同步 setState 的级联渲染），读取时按现编码掩蔽：
 * 该级缺失即按 `null`（「未知」）交付，编码恢复后由下一次 effect 重新解析。
 */
export function useScopeNames(
  client: MedicalRecognitionClient,
  codes: { organizationCode: string; hospitalCode: string; branchCode: string },
): ScopeNames {
  const { organizationCode, hospitalCode, branchCode } = codes
  const [names, setNames] = useState<ScopeNames>({ organization: null, hospital: null, branch: null })

  useEffect(() => {
    let latest = true
    // 写入前先比对现值：值未变化时返回原状态对象，让 React 跳过重渲染——否则不稳定的
    // client 引用（或重复的相同结果）会让 effect 每帧重跑并无限重排。
    const apply = (level: keyof ScopeNames, lookup: Promise<string | null>) => {
      void lookup.then(
        (name) => { if (latest) setNames((previous) => previous[level] === name ? previous : { ...previous, [level]: name }) },
        () => { if (latest) setNames((previous) => previous[level] === null ? previous : { ...previous, [level]: null }) },
      )
    }
    if (organizationCode.length > 0) apply('organization', lookupOrganizationName(client, organizationCode))
    if (hospitalCode.length > 0) apply('hospital', lookupHospitalName(hospitalCode))
    if (branchCode.length > 0) apply('branch', lookupBranchName(branchCode))
    return () => { latest = false }
  }, [branchCode, client, hospitalCode, organizationCode])

  return {
    organization: organizationCode.length > 0 ? names.organization : null,
    hospital: hospitalCode.length > 0 ? names.hospital : null,
    branch: branchCode.length > 0 ? names.branch : null,
  }
}
