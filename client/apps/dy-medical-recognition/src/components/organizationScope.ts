import type {
  CurrentOrganizationScopeReadModel,
  OrganizationScopeOptionsReadModel,
} from '@dy/api-client-medical-recognition'

/**
 * 组织范围选项：组织实例标识、IAM 组织业务编码与展示名称。
 *
 * 实例标识只服务选择器内部去重与三级联动、宿主会话映射，不进入业务请求；
 * 业务编码才是业务入口提交的组织标识，取 IAM 组织节点配置的编码。
 */
export interface OrganizationScopeOption {
  /** IAM 组织实例标识：选择器内部去重、三级联动与会话映射使用。 */
  id: string
  /** IAM 组织业务编码：业务请求提交的组织标识，节点未配置业务编码时该节点不进选项。 */
  code: string
  /** 展示名称。 */
  name: string
}

/** 院区选项。 */
export type OrganizationScopeBranchOption = OrganizationScopeOption

/** 医院选项及其院区。 */
export interface OrganizationScopeHospitalOption extends OrganizationScopeOption {
  branches: OrganizationScopeBranchOption[]
}

/** 医共体选项及其医院。 */
export interface OrganizationScopeOrganizationOption extends OrganizationScopeOption {
  hospitals: OrganizationScopeHospitalOption[]
}

/** 范围选择器的三级选项与未配置类型的节点数量。 */
export interface OrganizationScopeOptions {
  organizations: OrganizationScopeOrganizationOption[]
  unconfiguredOrganizationCount: number
}

/** 当前工作身份的三级组织范围：组织实例标识、名称与业务编码同时承载。 */
export interface CurrentOrganizationScope {
  organizationId: string | null
  organizationName: string | null
  /** 组织业务编码，即业务请求提交的组织标识；组织未配置编码时为空。 */
  organizationCode: string | null
  hospitalId: string | null
  hospitalName: string | null
  /** 医院业务编码，即业务请求提交的医院标识；医院未配置编码时为空。 */
  hospitalCode: string | null
  branchId: string | null
  branchName: string | null
  /** 院区业务编码，即业务请求提交的院区标识；院区未配置编码时为空。 */
  branchCode: string | null
}

/** 标识缺失或纯空白按缺失处理；组织实例标识原样保留，不做编码转换。 */
function readId(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/**
 * 业务编码缺失或纯空白按缺失处理。
 *
 * 编码为空表示该组织在 IAM 上未配置业务编码，此时既不用实例标识文本补齐，也不用名称补齐：
 * 调用方按「该层不可用」处理，不构造携带该层 Code 字段的业务请求。
 */
function readCode(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/** 名称缺失或纯空白按缺失处理；没有名称的节点无法安全展示，按不可选处理。 */
function readName(value: string | null | undefined): string | null {
  const text = (value ?? '').trim()
  return text.length > 0 ? text : null
}

/** 当前范围查询结果 → 视图模型；标识、名称与业务编码同源成对归一，不跨级复用。 */
export function toCurrentOrganizationScope(
  model: CurrentOrganizationScopeReadModel | undefined,
): CurrentOrganizationScope {
  return {
    organizationId: readId(model?.organizationId),
    organizationName: readName(model?.organizationName),
    organizationCode: readCode(model?.organizationCode),
    hospitalId: readId(model?.hospitalId),
    hospitalName: readName(model?.hospitalName),
    hospitalCode: readCode(model?.hospitalCode),
    branchId: readId(model?.branchId),
    branchName: readName(model?.branchName),
    branchCode: readCode(model?.branchCode),
  }
}

/**
 * 组织范围查询结果 → 选择器选项。
 *
 * 只保留标识、名称与业务编码都齐备的节点：
 * - 缺名称的节点无法展示；
 * - 缺标识的节点无法作为选择值；
 * - 缺业务编码的节点无法作为业务请求的组织标识，且不得回落实例标识文本。
 * 三者一律不进选项，也不按层级顺序或名称推断补齐。
 */
export function toOrganizationScopeOptions(
  model: OrganizationScopeOptionsReadModel | undefined,
): OrganizationScopeOptions {
  const organizations: OrganizationScopeOrganizationOption[] = []
  for (const organization of model?.organizations ?? []) {
    const id = readId(organization.organizationId)
    const name = readName(organization.organizationName)
    const code = readCode(organization.organizationCode)
    if (id === null || name === null || code === null) continue

    const hospitals: OrganizationScopeHospitalOption[] = []
    for (const hospital of organization.hospitals ?? []) {
      const hospitalId = readId(hospital.organizationId)
      const hospitalName = readName(hospital.organizationName)
      const hospitalCode = readCode(hospital.organizationCode)
      if (hospitalId === null || hospitalName === null || hospitalCode === null) continue

      const branches: OrganizationScopeBranchOption[] = []
      for (const branch of hospital.branches ?? []) {
        const branchId = readId(branch.organizationId)
        const branchName = readName(branch.organizationName)
        const branchCode = readCode(branch.organizationCode)
        if (branchId !== null && branchName !== null && branchCode !== null) {
          branches.push({ id: branchId, code: branchCode, name: branchName })
        }
      }
      hospitals.push({ id: hospitalId, code: hospitalCode, name: hospitalName, branches })
    }

    organizations.push({ id, code, name, hospitals })
  }

  const count = model?.unconfiguredOrganizationCount
  return {
    organizations,
    unconfiguredOrganizationCount:
      typeof count === 'number' && Number.isInteger(count) && count > 0 ? count : 0,
  }
}

/**
 * 按实例标识在选项清单中取该层业务编码。
 *
 * 选项清单与选择值同帧交付，因此清单里有该标识即有它的业务编码；
 * 标识缺失或不在清单中返回 null，调用方按「该层范围不可用」处理，不回落实例标识。
 */
export function findScopeCode(
  options: readonly OrganizationScopeOption[],
  id: string | undefined,
): string | null {
  if (id === undefined || id.length === 0) return null
  return options.find((option) => option.id === id)?.code ?? null
}
