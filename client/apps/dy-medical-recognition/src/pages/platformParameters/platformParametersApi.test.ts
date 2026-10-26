/**
 * 平台参数维护适配层用例（票 10、C9 的适配层面）：
 * 列表读模型归一、缺失参数的「未配置」呈现、三项参数的提交前取值校验与保存请求形状。
 */
import { describe, expect, it, vi } from 'vitest'
import type { MedicalRecognitionClient, PlatformParameterReadModel } from '@dy/api-client-medical-recognition'
import {
  queryPlatformParameters,
  savePlatformParameter,
  toPlatformParameterRows,
  validatePlatformParameterValue,
} from './platformParametersApi'

/** 生成契约形态的一行参数记录。 */
function stored(overrides: Partial<PlatformParameterReadModel> = {}): PlatformParameterReadModel {
  return {
    parameterCode: 'own_hospital_match_switch',
    parameterValue: '1',
    description: '本院报告匹配开关说明',
    isValid: true,
    operId: '11111111-1111-1111-1111-111111111111',
    operTime: new Date('2026-10-23T08:00:00Z'),
    ...overrides,
  }
}

function stubClient(get: () => Promise<PlatformParameterReadModel[] | undefined>) {
  const put = vi.fn(async (): Promise<boolean | undefined> => true)
  const byParameterCode = vi.fn(() => ({ put }))
  const client = {
    api: { v1: { platformParameters: { get: vi.fn(get), byParameterCode } } },
  } as unknown as MedicalRecognitionClient & { api: { v1: { platformParameters: { get: ReturnType<typeof vi.fn>, byParameterCode: ReturnType<typeof vi.fn> } } } }
  return { client, get: client.api.v1.platformParameters.get, byParameterCode, put }
}

describe('C9 平台参数列表归一', () => {
  it('三项参数按固定业务顺序呈现；缺少记录的参数标为未配置而不是伪造值', () => {
    const rows = toPlatformParameterRows([
      stored({ parameterCode: 'citation_detail_valid_hours', parameterValue: '24', description: null }),
      stored({ parameterCode: 'own_hospital_match_switch', parameterValue: '1' }),
    ])

    expect(rows.map((row) => row.code)).toEqual([
      'own_hospital_match_switch',
      'own_hospital_exclude_hours',
      'citation_detail_valid_hours',
    ])
    expect(rows[0]).toMatchObject({ name: '本院报告匹配开关', value: '1', isConfigured: true, isValid: true })
    expect(rows[1]).toMatchObject({ name: '本院报告排除时长', value: null, isConfigured: false })
    expect(rows[2]).toMatchObject({ name: '引用详情有效时长', value: '24', isConfigured: true })
  })

  it('未登记编码不进入维护列表，说明与操作信息按缺失归一', () => {
    const rows = toPlatformParameterRows([
      stored({ parameterCode: 'unknown_code', parameterValue: 'x' }),
      stored({ parameterCode: 'own_hospital_exclude_hours', parameterValue: '0', description: null, operId: undefined as unknown as string, operTime: undefined as unknown as Date }),
    ])

    expect(rows).toHaveLength(3)
    expect(rows.some((row) => row.code === 'unknown_code')).toBe(false)
    expect(rows[1]).toMatchObject({ value: '0', description: null, operId: null, operTime: null })
  })
})

describe('C9 提交前取值校验', () => {
  it('本院报告匹配开关只接受 0 与 1', () => {
    expect(validatePlatformParameterValue('own_hospital_match_switch', '1')).toBeNull()
    expect(validatePlatformParameterValue('own_hospital_match_switch', '0')).toBeNull()
    expect(validatePlatformParameterValue('own_hospital_match_switch', '2')).toBe('本院报告匹配开关只能填写 0 或 1。')
    expect(validatePlatformParameterValue('own_hospital_match_switch', '')).not.toBeNull()
    expect(validatePlatformParameterValue('own_hospital_match_switch', 'true')).not.toBeNull()
  })

  it('本院报告排除时长只接受非负整数小时', () => {
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '0')).toBeNull()
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '24')).toBeNull()
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '-1')).toBe('本院报告排除时长必须是非负整数小时。')
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '1.5')).not.toBeNull()
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '')).not.toBeNull()
  })

  it('引用详情有效时长只接受正整数小时', () => {
    expect(validatePlatformParameterValue('citation_detail_valid_hours', '1')).toBeNull()
    expect(validatePlatformParameterValue('citation_detail_valid_hours', '0')).toBe('引用详情有效时长必须是正整数小时。')
    expect(validatePlatformParameterValue('citation_detail_valid_hours', '-2')).not.toBeNull()
    expect(validatePlatformParameterValue('citation_detail_valid_hours', 'abc')).not.toBeNull()
  })

  it('超出 int32 范围的时长按非法值处理，避免与后端判定不一致', () => {
    expect(validatePlatformParameterValue('own_hospital_exclude_hours', '2147483648')).not.toBeNull()
    expect(validatePlatformParameterValue('citation_detail_valid_hours', '2147483648')).not.toBeNull()
  })
})

describe('C9 请求出口', () => {
  it('列表查询只调用平台参数列表端点并按适配层模型返回', async () => {
    const { client, get } = stubClient(async () => [stored()])
    const rows = await queryPlatformParameters(client)

    expect(get).toHaveBeenCalledTimes(1)
    expect(rows[0]).toMatchObject({ code: 'own_hospital_match_switch', value: '1' })
  })

  it('保存按参数编码定位并显式提交值、说明与启用状态', async () => {
    const { client, byParameterCode, put } = stubClient(async () => [])
    const result = await savePlatformParameter(client, {
      code: 'citation_detail_valid_hours',
      value: '24',
      description: '引用详情有效时长说明',
      isValid: false,
    })

    expect(result).toBe(true)
    expect(byParameterCode).toHaveBeenCalledWith('citation_detail_valid_hours')
    expect(put).toHaveBeenCalledWith({
      parameterValue: '24',
      description: '引用详情有效时长说明',
      isValid: false,
    })
  })

  it('保存端点未返回布尔真时返回未确认成功，不按成功处理', async () => {
    const { client, put } = stubClient(async () => [])
    put.mockResolvedValue(undefined)
    await expect(savePlatformParameter(client, {
      code: 'own_hospital_match_switch',
      value: '1',
      description: null,
      isValid: true,
    })).resolves.toBe(false)
  })
})
