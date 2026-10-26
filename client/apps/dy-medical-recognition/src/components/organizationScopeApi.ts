import type { MedicalRecognitionClient } from '@dy/api-client-medical-recognition'
import {
  toCurrentOrganizationScope,
  toOrganizationScopeOptions,
  type CurrentOrganizationScope,
  type OrganizationScopeOptions,
} from './organizationScope'

/**
 * 组织范围查询的共享适配出口：组件只消费归一后的视图模型，不直接接触生成客户端。
 */

/** 读取三级范围选项（IAM 组织树与组织类型筛选后的结果）。 */
export async function queryOrganizationScopeOptions(
  client: MedicalRecognitionClient,
): Promise<OrganizationScopeOptions> {
  return toOrganizationScopeOptions(
    await client.api.organizationScopeQuery.queryOrganizationScopeOptions.post(),
  )
}

/** 读取当前工作身份的组织范围。 */
export async function queryCurrentOrganizationScope(
  client: MedicalRecognitionClient,
): Promise<CurrentOrganizationScope> {
  return toCurrentOrganizationScope(
    await client.api.organizationScopeQuery.queryCurrentOrganizationScope.post(),
  )
}
