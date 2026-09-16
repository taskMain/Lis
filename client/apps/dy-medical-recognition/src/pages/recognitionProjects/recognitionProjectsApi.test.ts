/**
 * 互认项目适配层的纯逻辑、请求体形状与契约用例（测试矩阵 C2 / C3 / C6 / C20 / C23 的非 Host 面）。
 *
 * 装配边界：纯函数用真实实现；I/O 出口用**生成端同形的伪 Client**驱动，断言真实下发的
 * 请求体形状（组织编码显式下发、创建不含组织与内部 ID、启停只带目标标识），
 * 不构造真实鉴权适配器、不替换任何模块。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  CONFIGURATION_STATUSES,
  CONFIGURATION_STATUS_METADATA_FALLBACK,
  CONFIGURATION_STATUS_TEXTS,
  EMPTY_RECOGNITION_PROJECT_FILTER,
  MAX_RECOGNITION_DURATION_DAYS,
  buildConfigurationListQuery,
  buildCreateConfigurationRequest,
  buildDurationUpdatePayload,
  configuredStandardProjectCodes,
  configurationStatusActionText,
  configurationStatusTagColor,
  configurationStatusText,
  createRecognitionProjectConfiguration,
  excludeConfiguredStandardItems,
  filterConfigurationRows,
  parseRecognitionDurationDays,
  queryRecognitionProjectConfigurations,
  toConfigurationStatusFilterOptions,
  querySelectableStandardItems,
  setRecognitionProjectConfigurationEnabled,
  toConfigurationRows,
  toSelectableOptionGroups,
  toSelectableStandardItems,
  updateRecognitionDuration,
  type EffectiveMedicalStandardCatalogReadModel,
  type MedicalRecognitionClient,
  type RecognitionProjectConfigurationRow,
} from './recognitionProjectsApi'

/**
 * 生成端的伪 Client：只保留被测适配层实际调用的端点，`post` 记录请求体。
 * 返回体保持生成端可空形态（`undefined` / `null`），由适配层负责归一。
 */
function stubClient(handlers: Record<string, unknown> = {}) {
  const post = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const client = {
    api: {
      medicalRecognitionReportQuery: {
        queryRecognitionProjectConfigurationList: { post: post('queryList') },
        queryEffectiveMedicalStandardCatalog: { post: post('queryCatalog') },
      },
      medicalRecognitionReport: {
        createMutualRecognitionItem: { post: post('create') },
        updateMutualRecognitionItemConfiguration: { post: post('updateDuration') },
        enableMutualRecognitionItem: { post: post('enable') },
        disableMutualRecognitionItem: { post: post('disable') },
      },
    },
  }
  return client as unknown as MedicalRecognitionClient & typeof client
}

const bodyOf = (mock: { mock: { calls: unknown[][] } }): Record<string, unknown> =>
  mock.mock.calls[0][0] as Record<string, unknown>

const CATALOG: EffectiveMedicalStandardCatalogReadModel = {
  itemTypes: [
    {
      itemType: 0,
      categories: [
        {
          categoryName: '检验分类',
          groups: [
            { groupName: '血液', items: [{ code: 'A01', name: '血常规' }, { code: 'A02', name: '尿常规' }] },
          ],
        },
      ],
    },
    {
      itemType: 1,
      categories: [
        { categoryName: '检查分类', groups: [{ groupName: '影像', items: [{ code: 'B01', name: '胸部CT' }] }] },
      ],
    },
  ],
}

const row = (
  code: string,
  status: 1 | 2 | null = 1,
  id: string | null = `c-${code}`,
): RecognitionProjectConfigurationRow => ({
  configurationId: id,
  standardProjectCode: code,
  standardItemName: `${code} 名称`,
  itemType: 0,
  itemTypeText: '检验',
  categoryName: '检验分类',
  groupName: '血液',
  recognitionDurationDays: 30,
  configurationStatus: status,
  configurationStatusText: status === 1 ? '启用' : status === 2 ? '停用' : '未知',
  unavailableReason: null,
})

describe('C6 可互认时间解析与创建请求适配', () => {
  it('只接受 1 与 int 上界之间的整数字符串', () => {
    expect(parseRecognitionDurationDays('30')).toBe(30)
    expect(parseRecognitionDurationDays(' 7 ')).toBe(7)
    expect(parseRecognitionDurationDays('007')).toBe(7)
    expect(parseRecognitionDurationDays('1')).toBe(1)
  })

  it('空、0、负数、小数、非数字与全角数字一律拒绝', () => {
    for (const input of ['', '   ', '0', '0.0', '-1', '+1', '1.5', 'abc', '1e2', '30 天', '３０']) {
      expect(parseRecognitionDurationDays(input)).toBeNull()
    }
  })

  it('非法天数或空编码不构造创建请求，页面据此零写请求', () => {
    for (const input of ['', '0', '-3', '2.5', 'x']) {
      expect(buildCreateConfigurationRequest('A01', input)).toBeNull()
    }
    expect(buildCreateConfigurationRequest('   ', '30')).toBeNull()
  })

  // 设计条目：可互认时间上界与后端 `[Range(1, int.MaxValue)]` 同口径，必须在本地拦下，
  // 否则会在写请求上白跑一次服务端校验（旧用例名里的分组编号 B3 只在本文件内使用，无外部登记表）。
  it('接受上界 2147483647，超过 int 上界与超出安全整数的输入一律拒绝', () => {
    expect(MAX_RECOGNITION_DURATION_DAYS).toBe(2147483647)
    expect(parseRecognitionDurationDays(String(MAX_RECOGNITION_DURATION_DAYS))).toBe(MAX_RECOGNITION_DURATION_DAYS)
    expect(parseRecognitionDurationDays('2147483648')).toBeNull()
    expect(parseRecognitionDurationDays('99999999999999999999')).toBeNull()
    expect(buildCreateConfigurationRequest('A01', '2147483648')).toBeNull()
    expect(buildDurationUpdatePayload('c1', '2147483648')).toBeNull()
    expect(buildCreateConfigurationRequest('A01', String(MAX_RECOGNITION_DURATION_DAYS)))
      .toEqual({ standardProjectCode: 'A01', recognitionDurationDays: MAX_RECOGNITION_DURATION_DAYS })
  })

  it('C23 创建请求只含标准项目编码与正整数天数，不含组织编码与内部标准项目 ID', () => {
    const request = buildCreateConfigurationRequest(' A01 ', ' 30 ')
    expect(request).toEqual({ standardProjectCode: 'A01', recognitionDurationDays: 30 })
    expect(Object.keys(request as object)).toEqual(['standardProjectCode', 'recognitionDurationDays'])
  })

  it('修改请求只含目标标识与天数；同值不做短路，缺失标识或非法天数不构造请求', () => {
    expect(buildDurationUpdatePayload('c1', '30')).toEqual({ id: 'c1', recognitionDurationDays: 30 })
    expect(Object.keys(buildDurationUpdatePayload('c1', '30') as object)).toEqual(['id', 'recognitionDurationDays'])
    expect(buildDurationUpdatePayload(null, '30')).toBeNull()
    expect(buildDurationUpdatePayload('', '30')).toBeNull()
    expect(buildDurationUpdatePayload('c1', '0')).toBeNull()
    expect(buildDurationUpdatePayload('c1', '30 天')).toBeNull()
  })
})

describe('C23 查询请求适配', () => {
  it('提交所选组织编码；未筛选时不下发可选字段', () => {
    expect(buildConfigurationListQuery(' ORG-A ')).toEqual({ organizationCode: 'ORG-A' })
    expect(Object.keys(buildConfigurationListQuery('ORG-A') as object)).toEqual(['organizationCode'])
    expect(buildConfigurationListQuery('ORG-A', EMPTY_RECOGNITION_PROJECT_FILTER)).toEqual({ organizationCode: 'ORG-A' })
  })

  it('编码筛选按值下发、空白编码不下发；配置状态筛选不进入查询载荷', () => {
    expect(buildConfigurationListQuery('ORG-A', { code: 'A01', status: 2 })).toEqual({
      organizationCode: 'ORG-A',
      standardProjectCode: 'A01',
    })
    // 状态筛选只作用于本地展示：下推会缩小读取范围、破坏新增排除集合的完整性（业务口径，非契约限制）。
    expect(buildConfigurationListQuery('ORG-A', { code: '   ', status: 1 })).toEqual({
      organizationCode: 'ORG-A',
    })
  })

  it('组织编码缺失或空白时不构造查询，不退化为全局查询或硬编码组织', () => {
    expect(buildConfigurationListQuery('')).toBeNull()
    expect(buildConfigurationListQuery('   ')).toBeNull()
  })
})

describe('C23 读模型到行视图模型', () => {
  it('保留 null 目录原因、映射生成枚举数值、归一 Guid 标识，未知枚举不退化', () => {
    const [enabled, disabled, unknown] = toConfigurationRows([
      {
        configurationId: '3f2504e0-4f89-11d3-9a0c-0305e82c3301', standardProjectCode: 'A01', standardItemName: '血常规', itemType: 0,
        categoryName: '检验分类', groupName: '血液', recognitionDurationDays: 30, configurationStatus: 1, unavailableReason: null,
      },
      {
        configurationId: 'c2', standardProjectCode: 'B01', standardItemName: '胸部CT', itemType: 1,
        categoryName: '检查分类', groupName: '影像', recognitionDurationDays: 90, configurationStatus: 2, unavailableReason: '所属分组已停用',
      },
      {
        configurationId: null, standardProjectCode: null, standardItemName: null, itemType: 7,
        categoryName: null, groupName: null, recognitionDurationDays: null, configurationStatus: 9, unavailableReason: null,
      },
    ])

    expect(enabled.unavailableReason).toBeNull()
    expect(enabled.configurationStatus).toBe(1)
    expect(enabled.configurationId).toBe('3f2504e0-4f89-11d3-9a0c-0305e82c3301')
    expect(disabled.unavailableReason).toBe('所属分组已停用')
    expect(disabled.configurationStatus).toBe(2)
    expect(disabled.itemType).toBe(1)
    expect(unknown.itemType).toBeNull()
    expect(unknown.configurationStatus).toBeNull()
    expect(unknown.configurationId).toBeNull()
  })

  it('生成端缺省的字段一律归一为 null，不伪造已知状态；枚举中文回退本地兜底文案', () => {
    const [mapped] = toConfigurationRows([{}])
    expect(mapped).toEqual({
      configurationId: null, standardProjectCode: null, standardItemName: null, itemType: null, itemTypeText: '未知类型',
      categoryName: null, groupName: null, recognitionDurationDays: null, configurationStatus: null,
      configurationStatusText: '未知', unavailableReason: null,
    })
  })

  it('枚举中文取服务端契约文本，字段缺失时才回退本地兜底文案', () => {
    const [fromContract] = toConfigurationRows([
      { itemType: 1, itemTypeText: '服务端类型文案', configurationStatus: 2, configurationStatusText: '服务端停用文案' },
    ])
    expect(fromContract.itemTypeText).toBe('服务端类型文案')
    expect(fromContract.configurationStatusText).toBe('服务端停用文案')

    const [fromFallback] = toConfigurationRows([{ itemType: 0, configurationStatus: 1 }])
    expect(fromFallback.itemTypeText).toBe('检验')
    expect(fromFallback.configurationStatusText).toBe('启用')
  })

  it('行视图模型不引入创建/修改信息，也不引入已删除的派生状态字段', () => {
    const [mapped] = toConfigurationRows([{ configurationId: 'c1', standardProjectCode: 'A01', configurationStatus: 1 }])
    expect(Object.keys(mapped)).toEqual([
      'configurationId', 'standardProjectCode', 'standardItemName', 'itemType', 'itemTypeText', 'categoryName',
      'groupName', 'recognitionDurationDays', 'configurationStatus', 'configurationStatusText', 'unavailableReason',
    ])
    for (const removed of ['isStandardCatalogValid', 'isAvailableForNewMatch', 'creationTime', 'creator', 'lastModifiedTime', 'lastModifier']) {
      expect(mapped).not.toHaveProperty(removed)
    }
  })

  // 契约只交付字符串标识（生成端把后端 `Guid` 生成为 `type Guid = string`，反序列化走 `parseGuidString`）。
  it('标识只在契约交付非空字符串时使用，非字符串不得被归一成伪标识', () => {
    const [fromObject, fromNumber, fromEmpty, fromString] = toConfigurationRows([
      { configurationId: { value: 'c1' } as unknown as string, standardProjectCode: 'A01' },
      { configurationId: 42 as unknown as string, standardProjectCode: 'A02' },
      { configurationId: '', standardProjectCode: 'A03' },
      { configurationId: 'c4', standardProjectCode: 'A04' },
    ])

    // `String(value)` 会把它们变成 "[object Object]"、"42"，伪标识随后会被当成可写目标提交出去。
    expect(fromObject.configurationId).toBeNull()
    expect(fromNumber.configurationId).toBeNull()
    expect(fromEmpty.configurationId).toBeNull()
    expect(fromString.configurationId).toBe('c4')
  })
})

/**
 * 用例名不再带审查方编号（如旧名中的 B1）：编号没有可查的登记表，脱离报告即失去依据，
 * 这里改为直接写明所守的设计规则——配置状态的中文与色板只有本模块一个出口。
 */
describe('配置状态文案与色板的唯一出口', () => {
  it('每个已确认状态都有唯一文案，标签与弹窗取自同一出口', () => {
    // 期望值一律硬编码：用被测出口构造期望会让同源错误互相抵消（出口改错时断言跟着一起改，仍然通过）。
    expect(CONFIGURATION_STATUS_TEXTS).toEqual({ 1: '启用', 2: '停用' })
    expect(CONFIGURATION_STATUSES).toEqual([1, 2])
    expect(configurationStatusText(1)).toBe('启用')
    expect(configurationStatusText(2)).toBe('停用')
  })

  it('未知状态安全展示为未知且不加亮，不默认成已确认状态', () => {
    expect(configurationStatusText(null)).toBe('未知')
    expect(configurationStatusTagColor(null)).toBe('warning')
    expect(configurationStatusTagColor(1)).toBe('success')
    expect(configurationStatusTagColor(2)).toBeUndefined()
  })

  it('启停动作文案由目标状态推导', () => {
    // 目标为启用时动作是「启用」、目标为停用时动作是「停用」；同样硬编码，不引用被测出口互证。
    expect(configurationStatusActionText(true)).toBe('启用')
    expect(configurationStatusActionText(false)).toBe('停用')
  })
})

/** 用例名同样去掉旧名里的分组编号（B1b），只描述所守的口径：收窄与兜底由适配层唯一持有。 */
describe('枚举元数据选项的收窄与兜底由适配层持有', () => {
  it('兜底选项覆盖全部已确认状态，取值与文案与枚举声明一致', () => {
    expect(CONFIGURATION_STATUS_METADATA_FALLBACK).toEqual([
      { value: 1, name: 'Enabled', label: '启用' },
      { value: 2, name: 'Disabled', label: '停用' },
    ])
  })

  it('只保留已确认取值，未登记取值不进入筛选器', () => {
    const options = toConfigurationStatusFilterOptions([
      { value: 2, label: '服务端停用文案' },
      { value: 1, label: '服务端启用文案' },
      { value: 7, label: '未登记取值' },
    ])

    expect(options).toEqual([
      { value: 2, label: '服务端停用文案' },
      { value: 1, label: '服务端启用文案' },
    ])
  })

  it('保留服务端文案而不是本地文案', () => {
    const [first] = toConfigurationStatusFilterOptions([{ value: 1, label: '已启用' }])

    expect(first).toEqual({ value: 1, label: '已启用' })
    expect(first.label).not.toBe('启用')
  })
})

describe('C2 目录选择器只允许最底层当前有效标准项目', () => {
  it('只产出最底层标准项目，分类与分组不作为可选项', () => {
    const items = toSelectableStandardItems(CATALOG)
    expect(items.map((item) => item.code)).toEqual(['A01', 'A02', 'B01'])
    expect(items[0]).toEqual({ code: 'A01', name: '血常规', itemType: 0, categoryName: '检验分类', groupName: '血液' })
    expect(items[2].itemType).toBe(1)
    expect(items.map((item) => item.code)).not.toContain('检验分类')
    expect(items.map((item) => item.code)).not.toContain('血液')
  })

  it('选择器选项按分类 / 分组分组，选项值始终是最底层项目编码', () => {
    expect(toSelectableOptionGroups(toSelectableStandardItems(CATALOG))).toEqual([
      { label: '检验分类 / 血液', options: [{ value: 'A01', label: '血常规（A01）' }, { value: 'A02', label: '尿常规（A02）' }] },
      { label: '检查分类 / 影像', options: [{ value: 'B01', label: '胸部CT（B01）' }] },
    ])
  })

  it('目录为空或读取失败时不产出任何可选项', () => {
    expect(toSelectableStandardItems(null)).toEqual([])
    expect(toSelectableStandardItems({ itemTypes: null })).toEqual([])
    expect(toSelectableOptionGroups([])).toEqual([])
  })

  it('未知项目类型不默认成已知类型', () => {
    const items = toSelectableStandardItems({ itemTypes: [{ itemType: 7, categories: [{ categoryName: 'C', groups: [{ groupName: 'G', items: [{ code: 'X1', name: '未知类型项目' }] }] }] }] })
    expect(items).toEqual([{ code: 'X1', name: '未知类型项目', itemType: null, categoryName: 'C', groupName: 'G' }])
  })
})

describe('C3 已配置项目排除集合（含停用配置）', () => {
  it('启用与停用配置都进入排除集合，选择器据此隐藏', () => {
    const rows = [row('A01', 1), row('A02', 2)]
    const configured = configuredStandardProjectCodes(rows)
    expect(configured).toEqual(new Set(['A01', 'A02']))
    expect(excludeConfiguredStandardItems(toSelectableStandardItems(CATALOG), configured).map((item) => item.code)).toEqual(['B01'])
  })

  it('编码缺失的行不进入排除集合，也不影响其它编码', () => {
    expect(configuredStandardProjectCodes([{ ...row('A01'), standardProjectCode: null }, { ...row('A02') }])).toEqual(new Set(['A02']))
  })

  it('剔除只按编码移除，不改动候选集元素、不改变顺序', () => {
    const items = toSelectableStandardItems(CATALOG)
    const snapshot = items.map((item) => ({ ...item }))

    expect(excludeConfiguredStandardItems(items, new Set(['A02'])).map((item) => item.code)).toEqual(['A01', 'B01'])
    expect(excludeConfiguredStandardItems(items, new Set())).toEqual(items)
    // 剔除是纯函数：入参不被就地改写（页面每次行集变化都基于同一份摊平结果重算）。
    expect(items).toEqual(snapshot)
  })
})

describe('列表本地展示筛选', () => {
  it('编码按包含匹配、状态按配置状态精确匹配，且不修改入参数组', () => {
    const rows = [row('A01', 1), row('A02', 2), row('B01', 2)]
    const snapshot = rows.map((value) => ({ ...value }))

    expect(filterConfigurationRows(rows, { code: 'a0', status: 'all' }).map((value) => value.standardProjectCode)).toEqual(['A01', 'A02'])
    expect(filterConfigurationRows(rows, { code: '', status: 2 }).map((value) => value.standardProjectCode)).toEqual(['A02', 'B01'])
    expect(filterConfigurationRows(rows, { code: '', status: 1 }).map((value) => value.standardProjectCode)).toEqual(['A01'])
    expect(filterConfigurationRows(rows, { code: 'ZZ', status: 'all' })).toEqual([])
    // 同一引用在调用前后逐元素比对：筛选只读入参，不就地改写调用方持有的行。
    expect(rows).toEqual(snapshot)
  })

  it('配置状态未知的行只在全部筛选下可见', () => {
    const unknown = { ...row('A03'), configurationStatus: null }
    expect(filterConfigurationRows([unknown], { code: '', status: 'all' })).toHaveLength(1)
    expect(filterConfigurationRows([unknown], { code: '', status: 1 })).toEqual([])
  })
})

describe('C1 / C23 接线后的真实请求体形状（适配层 I/O 出口）', () => {
  it('查询显式下发组织编码；未确认的筛选不下发、不写死默认值', async () => {
    const client = stubClient()
    expect(await queryRecognitionProjectConfigurations(client, { organizationCode: 'ORG-A' })).toEqual([])

    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionProjectConfigurationList.post)
    expect(body.organizationCode).toBe('ORG-A')
    expect(body).not.toHaveProperty('standardProjectCode')
    expect(body).not.toHaveProperty('configurationStatus')
    expect(Object.keys(body)).toEqual(['organizationCode'])
  })

  it('编码筛选按值下发；配置状态筛选只作用于本地、绝不下推为读取范围', async () => {
    const client = stubClient({
      queryList: [{
        configurationId: 'c1', standardProjectCode: 'A01', standardItemName: '血常规', itemType: 0,
        categoryName: '检验分类', groupName: '血液', recognitionDurationDays: 30, configurationStatus: 2, unavailableReason: null,
      }],
    })
    const query = buildConfigurationListQuery('ORG-A', { code: 'A01', status: 2 })
    expect(query).not.toBeNull()

    const rows = await queryRecognitionProjectConfigurations(client, query!)
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionProjectConfigurationList.post)
    // 状态筛选只作用于本地展示（业务口径：下推会缩小读取范围、破坏新增排除集合完整性），故此处断言"不下发"。
    expect(body).toEqual({ organizationCode: 'ORG-A', standardProjectCode: 'A01' })
    expect(body).not.toHaveProperty('configurationStatus')
    expect(rows[0].configurationId).toBe('c1')
    expect(rows[0].configurationStatus).toBe(2)
  })

  it('C23 新增只提交标准项目编码与天数：不含组织编码、不含内部标准项目 ID', async () => {
    const client = stubClient()
    const request = buildCreateConfigurationRequest(' A01 ', '30')
    expect(request).not.toBeNull()

    await createRecognitionProjectConfiguration(client, request!)
    const body = bodyOf(client.api.medicalRecognitionReport.createMutualRecognitionItem.post)
    expect(body).toEqual({ standardProjectCode: 'A01', recognitionDurationDays: 30 })
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('id')
    expect(body).not.toHaveProperty('standardItemId')
  })

  it('非法天数或空编码不构造创建与修改请求，调用方据此零写请求', () => {
    for (const input of ['', '0', '-3', '2.5', 'abc']) {
      expect(buildCreateConfigurationRequest('A01', input)).toBeNull()
      expect(buildDurationUpdatePayload('c1', input)).toBeNull()
    }
    expect(buildCreateConfigurationRequest('   ', '30')).toBeNull()
  })

  it('C6 / C20 修改只提交配置标识与天数，同值照常提交', async () => {
    const client = stubClient()
    const request = buildDurationUpdatePayload('c1', '30')
    expect(request).not.toBeNull()

    await updateRecognitionDuration(client, request!)
    const body = bodyOf(client.api.medicalRecognitionReport.updateMutualRecognitionItemConfiguration.post)
    expect(body).toEqual({ id: 'c1', recognitionDurationDays: 30 })
    expect(Object.keys(body)).toEqual(['id', 'recognitionDurationDays'])
  })

  it('C8 启停只提交目标配置标识，且目标状态由所选方法表达', async () => {
    const enabledClient = stubClient()
    await setRecognitionProjectConfigurationEnabled(enabledClient, { id: 'c1', enabled: true })
    const enableBody = bodyOf(enabledClient.api.medicalRecognitionReport.enableMutualRecognitionItem.post)
    expect(enableBody).toEqual({ id: 'c1' })
    expect(enabledClient.api.medicalRecognitionReport.disableMutualRecognitionItem.post).not.toHaveBeenCalled()

    const disabledClient = stubClient()
    await setRecognitionProjectConfigurationEnabled(disabledClient, { id: 'c1', enabled: false })
    const disableBody = bodyOf(disabledClient.api.medicalRecognitionReport.disableMutualRecognitionItem.post)
    expect(disableBody).toEqual({ id: 'c1' })
    expect(disabledClient.api.medicalRecognitionReport.enableMutualRecognitionItem.post).not.toHaveBeenCalled()
  })

  it('C2 目录读取返回生成端可空形态时归一为 null，不伪造空目录数据', async () => {
    const empty = stubClient()
    expect(await querySelectableStandardItems(empty)).toBeNull()
    expect(toSelectableStandardItems(await querySelectableStandardItems(empty))).toEqual([])
    expect(bodyOf(empty.api.medicalRecognitionReportQuery.queryEffectiveMedicalStandardCatalog.post)).toEqual({})

    const filled = stubClient({ queryCatalog: CATALOG })
    expect(toSelectableStandardItems(await querySelectableStandardItems(filled)).map((item) => item.code))
      .toEqual(['A01', 'A02', 'B01'])
  })

  it('C3 排除集合来自全量查询结果：停用配置同样阻止重复新增', async () => {
    const client = stubClient({
      queryList: [
        { standardProjectCode: 'A01', configurationStatus: 1 },
        { standardProjectCode: 'A02', configurationStatus: 2 },
      ],
    })
    const query = buildConfigurationListQuery('ORG-A')
    const rows = await queryRecognitionProjectConfigurations(client, query!)
    const selectable = excludeConfiguredStandardItems(toSelectableStandardItems(CATALOG), configuredStandardProjectCodes(rows))
    expect(selectable.map((item) => item.code)).toEqual(['B01'])
  })
})
