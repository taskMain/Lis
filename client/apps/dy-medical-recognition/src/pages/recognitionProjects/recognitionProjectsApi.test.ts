/**
 * 互认项目适配层的纯逻辑、请求体形状与契约用例（测试矩阵 C2 / C3 / C6 / C20 / C23 的非 Host 面）。
 *
 * 装配边界：纯函数用真实实现；I/O 出口用**生成端同形的伪 Client**驱动，断言真实下发的
 * 请求体形状（组织编码显式下发、批量创建不含组织与内部 ID、启停只带目标标识），
 * 不构造真实鉴权适配器、不替换任何模块。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  CONFIGURATION_STATUSES,
  CONFIGURATION_STATUS_METADATA_FALLBACK,
  CONFIGURATION_STATUS_TEXTS,
  EMPTY_RECOGNITION_PROJECT_FILTER,
  MAX_RECOGNITION_DURATION_DAYS,
  RECOGNITION_DURATION_RANGE_TEXT,
  buildBatchCreateConfigurationRequest,
  buildConfigurationListQuery,
  buildDurationUpdatePayload,
  configuredStandardProjectCodes,
  configurationStatusActionText,
  configurationStatusTagColor,
  configurationStatusText,
  countColumnRows,
  createRecognitionProjectConfigurations,
  excludeConfiguredStandardItems,
  filterRecognitionProjectColumnView,
  parseRecognitionDurationDays,
  queryRecognitionProjectConfigurations,
  recognitionProjectRowBadge,
  rowDisplayName,
  toConfigurationStatusFilterOptions,
  querySelectableStandardItems,
  setRecognitionProjectConfigurationEnabled,
  toConfigurationRows,
  toRecognitionProjectColumnView,
  toSelectableColumnView,
  toSelectableStandardItems,
  updateRecognitionDuration,
  type EffectiveMedicalStandardCatalogReadModel,
  type MedicalRecognitionClient,
  type RecognitionProjectColumnRow,
  type RecognitionProjectColumnView,
  type RecognitionProjectConfigurationRow,
  type SelectableStandardItem,
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
        createMutualRecognitionItems: { post: post('createBatch') },
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

  it('非法天数或空选择不构造批量创建请求，页面据此零写请求', () => {
    for (const input of ['', '0', '-3', '2.5', 'x']) {
      expect(buildBatchCreateConfigurationRequest(new Set(['A01']), input)).toBeNull()
    }
    expect(buildBatchCreateConfigurationRequest(new Set(), '30')).toBeNull()
  })

  // 设计条目：可互认时间上界与后端 `[Range(1, int.MaxValue)]` 同口径，必须在本地拦下，
  // 否则会在写请求上白跑一次服务端校验（旧用例名里的分组编号 B3 只在本文件内使用，无外部登记表）。
  it('接受上界 2147483647，超过 int 上界与超出安全整数的输入一律拒绝', () => {
    expect(MAX_RECOGNITION_DURATION_DAYS).toBe(2147483647)
    expect(parseRecognitionDurationDays(String(MAX_RECOGNITION_DURATION_DAYS))).toBe(MAX_RECOGNITION_DURATION_DAYS)
    expect(parseRecognitionDurationDays('2147483648')).toBeNull()
    expect(parseRecognitionDurationDays('99999999999999999999')).toBeNull()
    expect(buildBatchCreateConfigurationRequest(new Set(['A01']), '2147483648')).toBeNull()
    expect(buildDurationUpdatePayload('c1', '2147483648')).toBeNull()
    expect(buildBatchCreateConfigurationRequest(new Set(['A01']), String(MAX_RECOGNITION_DURATION_DAYS)))
      .toEqual({ standardProjectCodes: ['A01'], recognitionDurationDays: MAX_RECOGNITION_DURATION_DAYS })
  })

  it('C23 批量创建请求只含标准项目编码集合与统一天数，不含组织编码与内部标准项目 ID', () => {
    const request = buildBatchCreateConfigurationRequest(new Set(['A01', 'A02']), ' 30 ')
    expect(request).toEqual({ standardProjectCodes: ['A01', 'A02'], recognitionDurationDays: 30 })
    expect(Object.keys(request as object)).toEqual(['standardProjectCodes', 'recognitionDurationDays'])
  })

  it('批量创建请求的编码集合按选择顺序逐项提交，单个项目也走同一形状', () => {
    expect(buildBatchCreateConfigurationRequest(new Set(['A03', 'A01']), '7'))
      .toEqual({ standardProjectCodes: ['A03', 'A01'], recognitionDurationDays: 7 })
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
    expect(items[0]).toEqual({ code: 'A01', name: '血常规', itemType: 0, itemTypeText: null, categoryName: '检验分类', groupName: '血液' })
    expect(items[2].itemType).toBe(1)
    expect(items.map((item) => item.code)).not.toContain('检验分类')
    expect(items.map((item) => item.code)).not.toContain('血液')
  })

  it('可选集合的列视图按目录层级摊开，全部行都是未配置行', () => {
    const view = toSelectableColumnView(toSelectableStandardItems(CATALOG))
    expect(view.map((itemType) => [itemType.itemType, itemType.itemTypeText])).toEqual([[0, '检验'], [1, '检查']])
    expect(view[0].categories.map((category) => category.categoryName)).toEqual(['检验分类'])
    expect(view[0].categories[0].groups.map((group) => [group.groupName, group.rows.map((item) => item.standardProjectCode)])).toEqual([
      ['血液', ['A01', 'A02']],
    ])
    // 弹窗只列未配置项目：每一行都没有配置标识、可互认时间与配置状态。
    expect(view.flatMap((itemType) => itemType.categories).flatMap((category) => category.groups).flatMap((group) => group.rows))
      .toEqual([
        { rowIdentity: 'A01|血常规', standardProjectCode: 'A01', standardItemName: '血常规', configured: false, configurationId: null, recognitionDurationDays: null, configurationStatus: null, configurationStatusText: null, unavailableReason: null },
        { rowIdentity: 'A02|尿常规', standardProjectCode: 'A02', standardItemName: '尿常规', configured: false, configurationId: null, recognitionDurationDays: null, configurationStatus: null, configurationStatusText: null, unavailableReason: null },
        { rowIdentity: 'B01|胸部CT', standardProjectCode: 'B01', standardItemName: '胸部CT', configured: false, configurationId: null, recognitionDurationDays: null, configurationStatus: null, configurationStatusText: null, unavailableReason: null },
      ])
  })

  it('目录为空或读取失败时不产出任何可选项，可选集合的列视图同样为空', () => {
    expect(toSelectableStandardItems(null)).toEqual([])
    expect(toSelectableStandardItems({ itemTypes: null })).toEqual([])
    expect(toSelectableColumnView([])).toEqual([])
  })

  it('未知项目类型不默认成已知类型', () => {
    const items = toSelectableStandardItems({ itemTypes: [{ itemType: 7, categories: [{ categoryName: 'C', groups: [{ groupName: 'G', items: [{ code: 'X1', name: '未知类型项目' }] }] }] }] })
    expect(items).toEqual([{ code: 'X1', name: '未知类型项目', itemType: null, itemTypeText: null, categoryName: 'C', groupName: 'G' }])
  })

  // 契约字段只承载服务端交付的文本：若在此处补本地兜底文案，类型列会优先取到兜底值，
  // 服务端文案就永远显示不出来（列视图节点按「契约文本优先、兜底随后」取候选）。
  it('类型文案只承载契约交付的文本，字段缺失时留空而不补本地兜底', () => {
    const [withText] = toSelectableStandardItems({
      itemTypes: [{ itemType: 0, itemTypeText: '服务端类型文案', categories: [{ categoryName: 'C', groups: [{ groupName: 'G', items: [{ code: 'X1', name: '项目' }] }] }] }],
    })
    expect(withText.itemTypeText).toBe('服务端类型文案')

    const [withoutText] = toSelectableStandardItems(CATALOG)
    expect(withoutText.itemTypeText).toBeNull()
    expect(withoutText.itemTypeText).not.toBe('检验')
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

/**
 * 列表本地展示筛选的语义在列视图裁剪这一条路径上固定：页面只渲染列视图，
 * 因此不再为配置行单独保留一个筛选出口，避免两处判定各自漂移。
 */
describe('列表本地展示筛选', () => {
  const rows = [row('A01', 1), row('A02', 2), row('B01', 2)]
  const view = toRecognitionProjectColumnView(rows, COLUMN_CATALOG)

  it('编码按包含匹配、状态按配置状态精确匹配，且不修改入参视图', () => {
    const snapshot = configuredCodesIn(view)

    // 编码匹配不区分大小写，且是包含匹配（`a0` 命中 `A01`）。
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: 'a0', status: 'all' }))).toEqual(['A01', 'A02'])
    // 状态是精确匹配：停用只留 `A02`、`B01`，启用只留 `A01`。
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: '', status: 2 }))).toEqual(['A02', 'B01'])
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: '', status: 1 }))).toEqual(['A01'])
    // 编码筛不到任何行时整份视图为空，层级节点一并被丢弃。
    expect(filterRecognitionProjectColumnView(view, { code: 'ZZ', status: 'all' })).toEqual([])

    // 裁剪是纯函数：不改写调用方持有的列视图。
    expect(configuredCodesIn(view)).toEqual(snapshot)
  })

  it('配置状态未知的行只在全部筛选下可见', () => {
    const unknownView = toRecognitionProjectColumnView([{ ...row('A03'), configurationStatus: null }], COLUMN_CATALOG)

    expect(configuredCodesIn(filterRecognitionProjectColumnView(unknownView, { code: '', status: 'all' }))).toEqual(['A03'])
    expect(filterRecognitionProjectColumnView(unknownView, { code: '', status: 1 })).toEqual([])
    expect(filterRecognitionProjectColumnView(unknownView, { code: '', status: 2 })).toEqual([])
  })
})

/**
 * 列视图夹具：目录交付类型中文（服务端文案的唯一来源），四层结构与 `CATALOG` 同形而多一组分组，
 * 另有一个目录读不到的编码由配置行在第二遍补齐。
 */
const COLUMN_CATALOG: SelectableStandardItem[] = [
  { code: 'A01', name: '血常规', itemType: 0, itemTypeText: '服务端检验文案', categoryName: '检验分类', groupName: '血液' },
  { code: 'A02', name: '尿常规', itemType: 0, itemTypeText: '服务端检验文案', categoryName: '检验分类', groupName: '血液' },
  { code: 'A03', name: '便常规', itemType: 0, itemTypeText: '服务端检验文案', categoryName: '检验分类', groupName: '体液' },
  { code: 'B01', name: '胸部CT', itemType: 1, itemTypeText: '服务端检查文案', categoryName: '检查分类', groupName: '影像' },
]

/** 目录读不到的配置行（目录已停用）：自己的类型、分类与分组由第二遍按行内字段建立。 */
const disabledCatalogRow: RecognitionProjectConfigurationRow = {
  ...row('C99', 2),
  categoryName: '历史分类',
  groupName: '历史分组',
  unavailableReason: '所属分组已停用',
}

const columnRows = (view: RecognitionProjectColumnView): RecognitionProjectColumnRow[] =>
  view.flatMap((itemType) => itemType.categories.flatMap((category) => category.groups.flatMap((group) => group.rows)))

const configuredCodesIn = (view: RecognitionProjectColumnView): (string | null)[] =>
  columnRows(view).filter((value) => value.configured).map((value) => value.standardProjectCode)

describe('列视图合并：目录结构与当前组织配置行合并为四层视图', () => {
  it('按目录顺序建立层级，目录读不到的配置行追加在各层末尾，类型按取值升序', () => {
    const view = toRecognitionProjectColumnView(
      [row('A01', 1), row('B01', 2), disabledCatalogRow],
      COLUMN_CATALOG,
    )

    expect(view.map((itemType) => itemType.itemType)).toEqual([0, 1])
    expect(view[0].categories.map((category) => category.categoryName)).toEqual(['检验分类', '历史分类'])
    expect(view[0].categories[0].groups.map((group) => group.groupName)).toEqual(['血液', '体液'])
    expect(view[1].categories[0].groups[0].rows.map((value) => value.standardProjectCode)).toEqual(['B01'])
  })

  it('第二遍按配置行顺序补齐，编码缺失的行同样进入列视图而不是被丢弃', () => {
    // 同一编码的两行在两处，中间隔着一行别的编码：按配置行顺序补齐时「乙分类」先于第二行「甲分类」出现，
    // 按编码分组后再补齐（先排完同一编码的全部行）会得到相反结果。
    const first: RecognitionProjectConfigurationRow = { ...row('C99', 2), categoryName: '乙分类', groupName: '乙分组' }
    const interleaved: RecognitionProjectConfigurationRow = { ...row('D99', 2), categoryName: '甲分类', groupName: '甲分组' }
    const second: RecognitionProjectConfigurationRow = { ...row('C99', 2), configurationId: 'c-C99-b', categoryName: '丙分类', groupName: '丙分组' }
    // 编码缺失的行在目录里没有对应项目，只能在第二遍出现；丢弃它会连同它的目录停用原因一起消失。
    const withoutCode: RecognitionProjectConfigurationRow = { ...row('', 2), standardProjectCode: null, categoryName: '无编码分类', groupName: '无编码分组' }

    const view = toRecognitionProjectColumnView([first, interleaved, second, withoutCode], COLUMN_CATALOG)

    expect(view[0].categories.map((category) => category.categoryName))
      .toEqual(['检验分类', '乙分类', '甲分类', '丙分类', '无编码分类'])
    expect(columnRows(view).filter((value) => value.configured).map((value) => value.standardProjectCode))
      .toEqual(['C99', 'D99', 'C99', null])
  })

  it('未配置的目录项目进入列视图，配置字段一律为空且计数包含它们', () => {
    const view = toRecognitionProjectColumnView([row('A01', 1), row('B01', 2), disabledCatalogRow], COLUMN_CATALOG)
    const [a02] = columnRows(view).filter((value) => value.standardProjectCode === 'A02')

    expect(a02.configured).toBe(false)
    expect(a02.standardItemName).toBe('尿常规')
    expect(a02.configurationId).toBeNull()
    expect(a02.recognitionDurationDays).toBeNull()
    expect(a02.configurationStatus).toBeNull()
    expect(a02.configurationStatusText).toBeNull()
    expect(a02.unavailableReason).toBeNull()
    expect(countColumnRows(view)).toBe(5)
  })

  it('已配置行取配置行的配置标识、可互认时间、状态与目录停用原因', () => {
    const view = toRecognitionProjectColumnView([row('A01', 1), disabledCatalogRow], COLUMN_CATALOG)
    const [a01] = columnRows(view).filter((value) => value.standardProjectCode === 'A01')
    const [c99] = columnRows(view).filter((value) => value.standardProjectCode === 'C99')

    expect(a01.configured).toBe(true)
    expect(a01.configurationId).toBe('c-A01')
    expect(a01.recognitionDurationDays).toBe(30)
    expect(a01.configurationStatus).toBe(1)
    expect(a01.configurationStatusText).toBe('启用')
    expect(a01.unavailableReason).toBeNull()

    // 目录读不到的配置行必须仍然出现：目录停用原因只由这一步带出来。
    expect(c99.configured).toBe(true)
    expect(c99.configurationId).toBe('c-C99')
    expect(c99.unavailableReason).toBe('所属分组已停用')
  })

  // 项目类型中文的唯一来源是服务端枚举声明，目录与配置行交付的是同一份声明，任一来源有值就应采用。
  // 目录未交付类型中文、而第一遍就匹配上目录的配置行交付了文本时，这条路径最容易漏掉候选：
  // 类型格会回落到本地兜底「检验」，页面上的服务端文案随之永远显示不出来。
  it('第一遍匹配到目录的配置行同样补充类型文案候选，本地兜底不出现', () => {
    const catalogWithoutTypeText: SelectableStandardItem[] = [{ ...COLUMN_CATALOG[0], itemTypeText: null }]
    const view = toRecognitionProjectColumnView(
      [{ ...row('A01', 1), itemTypeText: '配置行交付文案' }],
      catalogWithoutTypeText,
    )

    expect(view[0].itemType).toBe(0)
    expect(view[0].itemTypeText).toBe('配置行交付文案')
    expect(view[0].itemTypeText).not.toBe('检验')
    // 该行在第一遍取用目录项目，配置字段仍来自配置行。
    expect(columnRows(view)[0].configured).toBe(true)
  })

  it('三个来源都为空时才用本地兜底，目录交付优先于配置行', () => {
    const catalogWithoutTypeText: SelectableStandardItem[] = [{ ...COLUMN_CATALOG[0], itemTypeText: null }]

    // 目录与配置行都有文本时取目录交付的文本。
    const bothSources = toRecognitionProjectColumnView(
      [{ ...row('A01', 1), itemTypeText: '配置行交付文案' }],
      COLUMN_CATALOG,
    )
    expect(bothSources[0].itemTypeText).toBe('服务端检验文案')

    // 目录与配置行都为空时用本地兜底。
    const noSource = toRecognitionProjectColumnView(
      [{ ...row('A01', 1), itemTypeText: '' }],
      catalogWithoutTypeText,
    )
    expect(noSource[0].itemTypeText).toBe('检验')
  })

  it('类型节点中文取第一个非空候选：目录交付优先于配置行，都没有时用本地兜底', () => {
    const fromCatalog = toRecognitionProjectColumnView([row('A01', 1)], COLUMN_CATALOG)
    expect(fromCatalog[0].itemTypeText).toBe('服务端检验文案')

    // 目录未交付类型中文时，改取第二遍补齐的配置行交付的文本。
    const fromRow = toRecognitionProjectColumnView(
      [{ ...row('C99', 1), itemTypeText: '配置行交付文案' }],
      [{ ...COLUMN_CATALOG[0], itemTypeText: null }],
    )
    expect(fromRow[0].itemType).toBe(0)
    expect(fromRow[0].itemTypeText).toBe('配置行交付文案')

    // 两处都没有候选时用本地兜底，未知取值不默认成已知类型。
    const fallback = toRecognitionProjectColumnView([], [{ ...COLUMN_CATALOG[0], itemTypeText: null }])
    expect(fallback[0].itemTypeText).toBe('检验')

    const unknownType = toRecognitionProjectColumnView([], [
      { code: 'X1', name: '未知类型项目', itemType: null, itemTypeText: null, categoryName: '其他分类', groupName: '其他分组' },
      COLUMN_CATALOG[3],
    ])
    expect(unknownType.map((itemType) => itemType.itemType)).toEqual([1, null])
    expect(unknownType[1].itemTypeText).toBe('未知类型')
  })

  it('类型中文经目录摊平从生成响应贯穿到列视图', () => {
    const items = toSelectableStandardItems({
      itemTypes: [{
        itemType: 0,
        itemTypeText: '服务端交付的类型中文',
        categories: [{ categoryName: '检验分类', groups: [{ groupName: '血液', items: [{ code: 'A01', name: '血常规' }] }] }],
      }],
    })

    expect(toRecognitionProjectColumnView([row('A01', 1)], items)[0].itemTypeText).toBe('服务端交付的类型中文')
  })

  it('行标识在整份合并结果内唯一：业务键优先取配置标识，缺失时用编码与名称组成', () => {
    const view = toRecognitionProjectColumnView([row('A01', 1), row('B01', 2), disabledCatalogRow], COLUMN_CATALOG)
    const identities = columnRows(view).map((value) => value.rowIdentity)

    expect(new Set(identities).size).toBe(identities.length)
    // 契约唯一键存在时标识就是它，不引入本次行集的数组下标。
    expect(identities).toContain('c-A01')
    expect(identities).toContain('c-B01')
    expect(identities).toContain('c-C99')
    // 配置标识缺失的未配置行按编码与名称组成业务键。
    expect(identities).toContain('A02|尿常规')
  })

  it('业务键完全相同的多行只在重复处追加出现序号，序号不进入业务字段', () => {
    // 契约唯一键缺失且业务字段完全相同的行在查询结果里不可区分：名称与目录同名，标识只由出现次序区分。
    const duplicate = { ...row('A01'), configurationId: null, standardItemName: '血常规' }
    const view = toRecognitionProjectColumnView([duplicate, { ...duplicate }], COLUMN_CATALOG)
    const a01Rows = columnRows(view).filter((value) => value.standardProjectCode === 'A01')

    expect(a01Rows.map((value) => value.rowIdentity)).toEqual(['A01|血常规', 'A01|血常规#1'])
    expect(a01Rows.map((value) => value.configured)).toEqual([true, true])
    expect(a01Rows.map((value) => value.standardItemName)).toEqual(['血常规', '血常规'])
  })

  it('空目录与空行集都产出空列视图，不伪造层级', () => {
    expect(toRecognitionProjectColumnView([], [])).toEqual([])
    expect(countColumnRows([])).toBe(0)
  })
})

describe('列视图裁剪与计数', () => {
  const rows = [row('A01', 1), row('B01', 2)]
  const view = toRecognitionProjectColumnView(rows, COLUMN_CATALOG)

  it('裁剪与配置行筛选共用同一条判定：编码包含匹配、状态精确匹配', () => {
    // `a0` 只命中 A 段编码；B 段编码不在结果里，说明编码是包含匹配而不是前缀或全等。
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: 'a0', status: 'all' })))
      .toEqual(['A01'])
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: '0', status: 'all' })))
      .toEqual(['A01', 'B01'])
    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: '', status: 2 })))
      .toEqual(['B01'])
    expect(filterRecognitionProjectColumnView(view, { code: 'ZZ', status: 'all' })).toEqual([])
    expect(filterRecognitionProjectColumnView(view, { code: '', status: 1 }).map((itemType) => itemType.itemType)).toEqual([0])
  })

  it('裁剪先筛项目行，再丢弃没有可见项目的分组、分类与类型', () => {
    const byCode = filterRecognitionProjectColumnView(view, { code: 'B01', status: 'all' })

    expect(byCode.map((itemType) => itemType.itemType)).toEqual([1])
    expect(byCode[0].categories.map((category) => category.categoryName)).toEqual(['检查分类'])
    expect(byCode[0].categories[0].groups.map((group) => group.groupName)).toEqual(['影像'])
    expect(countColumnRows(byCode)).toBe(1)

    // 裁剪只筛选可见行，不改写入参持有的列视图。
    expect(countColumnRows(view)).toBe(4)
    expect(configuredCodesIn(view)).toEqual(['A01', 'B01'])
  })

  it('未配置行按编码匹配同样保留，状态筛选把它们排除在可见行之外', () => {
    const byCode = filterRecognitionProjectColumnView(view, { code: 'A02', status: 'all' })
    expect(columnRows(byCode).map((value) => [value.standardProjectCode, value.configured])).toEqual([['A02', false]])

    expect(configuredCodesIn(filterRecognitionProjectColumnView(view, { code: '', status: 2 }))).toEqual(['B01'])
  })
})

describe('项目行状态徽标', () => {
  const rowOf = (view: RecognitionProjectColumnView, code: string) =>
    columnRows(view).filter((value) => value.standardProjectCode === code)[0]

  it('目录停用优先于配置自身状态，并带出目录停用原因', () => {
    const badge = recognitionProjectRowBadge(rowOf(toRecognitionProjectColumnView([disabledCatalogRow], COLUMN_CATALOG), 'C99'))
    expect(badge).toEqual({ text: '目录已停用', color: 'error', reason: '所属分组已停用' })
  })

  it('未配置行单独表达，不带配置状态色板', () => {
    const badge = recognitionProjectRowBadge(rowOf(toRecognitionProjectColumnView([], COLUMN_CATALOG), 'A02'))
    expect(badge).toEqual({ text: '未配置', color: 'default', reason: null })
  })

  it('已配置行取服务端交付的状态中文与状态色板，未知状态安全展示', () => {
    const view = toRecognitionProjectColumnView([row('A01', 1), row('B01', 2), row('A03', null)], COLUMN_CATALOG)

    expect(recognitionProjectRowBadge(rowOf(view, 'A01'))).toEqual({ text: '启用', color: 'success', reason: null })
    expect(recognitionProjectRowBadge(rowOf(view, 'B01'))).toEqual({ text: '停用', color: undefined, reason: null })
    expect(recognitionProjectRowBadge(rowOf(view, 'A03'))).toEqual({ text: '未知', color: 'warning', reason: null })
  })
})

describe('可互认时间范围提示与行显示名', () => {
  it('范围提示与解析上界同源，文案与既有提示完全一致', () => {
    expect(RECOGNITION_DURATION_RANGE_TEXT).toBe('可互认时间为 1 到 2147483647 之间的整数天数')
    expect(RECOGNITION_DURATION_RANGE_TEXT).toContain(String(MAX_RECOGNITION_DURATION_DAYS))
  })

  // 入参放宽为结构子集是编译期事实：这里的调用由 `tsc -b` 校验，页面的配置行与列视图行都可直接传入。
  it('行显示名接受只含编码与名称的结构子集', () => {
    expect(rowDisplayName({ standardProjectCode: 'A01', standardItemName: '血常规' })).toBe('血常规')
    expect(rowDisplayName({ standardProjectCode: 'A01', standardItemName: null })).toBe('A01')
    expect(rowDisplayName({ standardProjectCode: null, standardItemName: null })).toBe('未知项目')
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

  it('C23 批量新增只提交编码集合与统一天数：不含组织编码、不含内部标准项目 ID', async () => {
    const client = stubClient()
    const request = buildBatchCreateConfigurationRequest(new Set(['A01', 'A02']), '30')
    expect(request).not.toBeNull()

    await createRecognitionProjectConfigurations(client, request!)
    const body = bodyOf(client.api.medicalRecognitionReport.createMutualRecognitionItems.post)
    expect(body).toEqual({ standardProjectCodes: ['A01', 'A02'], recognitionDurationDays: 30 })
    expect(Object.keys(body)).toEqual(['standardProjectCodes', 'recognitionDurationDays'])
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('id')
    expect(body).not.toHaveProperty('standardItemId')
    // 批量入口是唯一的新增通道：单条新增接口不再被页面调用。
    expect(client.api.medicalRecognitionReport.createMutualRecognitionItem.post).not.toHaveBeenCalled()
  })

  it('非法天数或空选择不构造批量创建与修改请求，调用方据此零写请求', () => {
    for (const input of ['', '0', '-3', '2.5', 'abc']) {
      expect(buildBatchCreateConfigurationRequest(new Set(['A01']), input)).toBeNull()
      expect(buildDurationUpdatePayload('c1', input)).toBeNull()
    }
    expect(buildBatchCreateConfigurationRequest(new Set(), '30')).toBeNull()
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
