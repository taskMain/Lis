/**
 * 互认项目金额适配层的请求形状、可空归一、枚举转换与金额两态用例
 * （测试矩阵 C2 / C4 的适配层契约面，以及 C24 的字段与可空性核对面的客户端侧）。
 *
 * 装配边界：纯函数用真实实现；I/O 出口用**生成端同形的伪 Client** 驱动，断言真实下发的
 * 请求体形状（平台管理员页携带组织、医院、院区；医院管理员页只携带院区与标准项目编码），
 * 不构造真实鉴权适配器、不替换任何模块。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  CONFIGURATION_STATUSES,
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
  configurationStatusText,
  currentAmountText,
  isConfigurationStatusValue,
  isMedicalItemTypeValue,
  medicalItemTypeText,
  parseRecognitionAmount,
  queryBranchRecognitionAmountRows,
  queryRecognitionAmountRows,
  saveBranchRecognitionAmount,
  saveOrganizationHospitalBranchRecognitionAmount,
  toRecognitionAmountRows,
  type MedicalRecognitionClient,
  type RecognitionAmountRow,
  type RecognitionAmountReadModelLike,
} from './recognitionAmountsApi'

/**
 * 生成端的伪 Client：只保留被测适配层实际调用的端点，`post` 记录请求体与请求配置。
 * 返回体保持生成端可空形态（`undefined` / `null`），由适配层负责归一。
 */
function stubClient(handlers: Record<string, unknown> = {}) {
  const post = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const client = {
    api: {
      medicalRecognitionReport: {
        saveBranchRecognitionAmount: { post: post('saveBranch') },
        saveOrganizationHospitalBranchRecognitionAmount: { post: post('savePlatform') },
      },
      medicalRecognitionReportQuery: {
        queryBranchRecognitionAmountList: { post: post('queryBranch') },
        queryRecognitionAmountList: { post: post('queryPlatform') },
      },
    },
  }
  return client as unknown as MedicalRecognitionClient & typeof client
}

const bodyOf = (mock: { mock: { calls: unknown[][] } }): Record<string, unknown> =>
  mock.mock.calls[0][0] as Record<string, unknown>

/** 提交载荷里的金额节点取值；生成端的 `writeObjectValue` 就是按这个取值写出数值的。 */
const amountOf = (body: Record<string, unknown>): unknown =>
  (body.currentAmount as { getValue: () => unknown }).getValue()

/** 生成端反序列化后的金额形态：`UntypedNode`（`value` + `getValue()`），不是裸数字。 */
const node = (value: unknown) => ({ value, getValue: () => value })

const readModel = (overrides: Partial<RecognitionAmountReadModelLike> = {}): RecognitionAmountReadModelLike => ({
  standardProjectCode: 'A01',
  standardProjectName: '血常规',
  itemType: 0,
  categoryName: '检验分类',
  groupName: '血液',
  organizationName: '组织一',
  hospitalName: '医院一',
  branchName: '院区一',
  configurationStatus: 1,
  unavailableReason: null,
  currentAmount: node(12.5),
  isAmountConfigured: true,
  ...overrides,
})

describe('C2 平台管理员页请求构造：组织、医院、院区三个值都要携带', () => {
  it('查询请求只含组织、医院、院区与标准项目编码，四处都按值下发', () => {
    const request = buildRecognitionAmountListQuery({
      organizationCode: ' ORG-A ',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: ' A01 ',
    })

    expect(request).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
    })
    expect(Object.keys(request as object)).toEqual([
      'organizationCode',
      'hospitalCode',
      'branchCode',
      'standardProjectCode',
    ])
  })

  it('标准项目编码缺省时不下发（表示全部），组织、医院、院区仍全部下发', () => {
    const request = buildRecognitionAmountListQuery({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })

    expect(request).not.toBeNull()
    expect(request).not.toHaveProperty('standardProjectCode')
    expect(Object.keys(request as object)).toEqual(['organizationCode', 'hospitalCode', 'branchCode'])
  })

  it('组织、医院、院区任一缺失或空白都不构造查询，不降级为全局查询或伪造组织', () => {
    const base = { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }
    expect(buildRecognitionAmountListQuery({ ...base, organizationCode: '' })).toBeNull()
    expect(buildRecognitionAmountListQuery({ ...base, organizationCode: '   ' })).toBeNull()
    expect(buildRecognitionAmountListQuery({ ...base, hospitalCode: '' })).toBeNull()
    expect(buildRecognitionAmountListQuery({ ...base, branchCode: '  ' })).toBeNull()
  })

  it('保存载荷含组织、医院、院区、标准项目编码与金额，金额以生成端可接受的形态承载', () => {
    const payload = buildRecognitionAmountSavePayload({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      amountInput: '12.50',
    }, 'A01')

    expect(payload).not.toBeNull()
    expect(payload).toMatchObject({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })
    expect(Object.keys(payload as object)).toEqual([
      'organizationCode',
      'hospitalCode',
      'branchCode',
      'standardProjectCode',
      'currentAmount',
    ])
  })
})

describe('C4 医院管理员页请求构造：只携带院区与标准项目编码', () => {
  it('查询请求只有院区与标准项目编码，不含组织与医院', () => {
    const request = buildBranchAmountListQuery({ branchCode: ' BRA-1 ', standardProjectCode: ' A01 ' })

    expect(request).toEqual({ branchCode: 'BRA-1', standardProjectCode: 'A01' })
    expect(Object.keys(request as object)).toEqual(['branchCode', 'standardProjectCode'])
  })

  it('标准项目编码缺省时只下发院区', () => {
    const request = buildBranchAmountListQuery({ branchCode: 'BRA-1' })

    expect(request).toEqual({ branchCode: 'BRA-1' })
    expect(Object.keys(request as object)).toEqual(['branchCode'])
  })

  it('院区缺失或空白不构造查询，不退回可信上下文、不发空院区请求', () => {
    expect(buildBranchAmountListQuery({ branchCode: '' })).toBeNull()
    expect(buildBranchAmountListQuery({ branchCode: '   ' })).toBeNull()
  })

  it('保存载荷只有院区、标准项目编码与金额，不含组织与医院', () => {
    const payload = buildBranchAmountSavePayload({ branchCode: 'BRA-1', amountInput: '0' }, 'A01')

    expect(payload).not.toBeNull()
    expect(Object.keys(payload as object)).toEqual(['branchCode', 'standardProjectCode', 'currentAmount'])
    expect(payload).not.toHaveProperty('organizationCode')
    expect(payload).not.toHaveProperty('hospitalCode')
    expect(payload).toHaveProperty('currentAmount', 0)
  })
})

describe('金额解析：只接受非负且最多两位小数', () => {
  it('接受整数、一位与两位小数，前后空白被忽略', () => {
    expect(parseRecognitionAmount('0')).toBe(0)
    expect(parseRecognitionAmount('12')).toBe(12)
    expect(parseRecognitionAmount(' 12.5 ')).toBe(12.5)
    expect(parseRecognitionAmount('12.50')).toBe(12.5)
    expect(parseRecognitionAmount('0.05')).toBe(0.05)
  })

  it('空、负数、超过两位小数与非数字一律拒绝', () => {
    for (const input of ['', '   ', '-1', '-0.01', '1.234', 'abc', '1e2', '1e3', '1,5', '1,234.56', '12.5 元', '３０']) {
      expect(parseRecognitionAmount(input)).toBeNull()
    }
  })

  it('双精度无法精确表示的金额被拒绝，不静默写入被漂移的值', () => {
    // `99999999999999.99` 有 16 位有效数字，双精度读进来已是 `99999999999999.98`：
    // 数值重新按两位小数格式化后与输入不一致，按非法输入拒绝（与其它非法输入同样的零写请求路径）。
    expect(Number('99999999999999.99')).toBe(99999999999999.98)
    expect(parseRecognitionAmount('99999999999999.99')).toBeNull()
    // 2^53 + 1 同样无法表示：整数形态的漂移也一并拒绝。
    expect(parseRecognitionAmount('9007199254740993')).toBeNull()
  })

  it('序列化文本与输入不同的金额被拒绝：内存里可精确表示，上线却写成另一个十进制', () => {
    // `2251799813685247.75` 位于 [2^50, 2^51)，该区间双精度的粒度恰为 0.25，因此这个十进制在内存里
    // **可精确表示**：数值按两位小数回格式化与输入一致（内存面判据放行，本用例第一行即是证据）。
    // 但提交时金额由生成端写成 JSON 数值，JS 写出的是双精度的最短往返文本 `2251799813685247.8`：
    // 服务端会落成 `...247.80`，与用户输入差 0.05 且不报错，因此必须按非法输入拒绝。
    expect(Number('2251799813685247.75').toFixed(2)).toBe('2251799813685247.75')
    expect(JSON.stringify(Number('2251799813685247.75'))).toBe('2251799813685247.8')
    expect(parseRecognitionAmount('2251799813685247.75')).toBeNull()
    // 同量级但序列化文本与输入一致（即上线文本就是用户输入的十进制）的值仍通过：判据拒的是序列化面
    // 的漂移，不是量级本身。
    expect(parseRecognitionAmount('4503599627370495.5')).toBe(4503599627370495.5)
    expect(JSON.stringify(4503599627370495.5)).toBe('4503599627370495.5')
  })

  it('边界内大额但可精确表示的金额仍通过（内存面与序列化面两道判据都放行）', () => {
    // 取值理由：`1234567890123.25` 的整数部分 1234567890123 远小于 2^53（整数精确），小数部分
    // `.25 = 1/4` 是二进制可精确表示的分数；解析值乘 100 得 123456789012325，仍小于 2^53，
    // 因此「按两位小数重新格式化」与输入逐位相同，其最短往返序列化文本也逐位相同。
    const amount = parseRecognitionAmount('1234567890123.25')
    expect(amount).toBe(1234567890123.25)
    expect(amount?.toFixed(2)).toBe('1234567890123.25')
    expect(JSON.stringify(amount)).toBe('1234567890123.25')
    // 同一量级的一位小数与 2^53 本身（可精确表示且写得出等值文本）仍通过。
    expect(parseRecognitionAmount('1234567890123.5')).toBe(1234567890123.5)
    expect(parseRecognitionAmount('9007199254740992')).toBe(9007199254740992)
    // 既有行为不受两道判据影响：整数、两位小数与合法的零元。
    expect(parseRecognitionAmount('12.34')).toBe(12.34)
    expect(parseRecognitionAmount('12.5')).toBe(12.5)
    expect(parseRecognitionAmount('0')).toBe(0)
    expect(parseRecognitionAmount('0.00')).toBe(0)
  })

  it('非法金额或空标准项目编码不构造保存载荷，页面据此零写请求', () => {
    expect(buildBranchAmountSavePayload({ branchCode: 'BRA-1', amountInput: '1.234' }, 'A01')).toBeNull()
    expect(buildBranchAmountSavePayload({ branchCode: 'BRA-1', amountInput: '12' }, '   ')).toBeNull()
    expect(buildBranchAmountSavePayload({ branchCode: '   ', amountInput: '12' }, 'A01')).toBeNull()
    expect(buildRecognitionAmountSavePayload({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', amountInput: 'x' }, 'A01')).toBeNull()
  })
})

describe('读模型到行视图模型：可空金额与枚举归一', () => {
  it('未配置的当前金额归一为 null 且不呈现为 0，未配置标记为假', () => {
    const [row] = toRecognitionAmountRows([readModel({ currentAmount: null, isAmountConfigured: false })])

    expect(row.currentAmount).toBeNull()
    expect(row.isAmountConfigured).toBe(false)
    expect(currentAmountText(row)).toBe('未配置')
  })

  it('已配置的零元按数值 0 呈现，不与未配置合并', () => {
    const [row] = toRecognitionAmountRows([readModel({ currentAmount: node(0), isAmountConfigured: true })])

    expect(row.currentAmount).toBe(0)
    expect(row.isAmountConfigured).toBe(true)
    expect(currentAmountText(row)).toBe('0.00')
    expect(currentAmountText(row)).not.toBe(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: null, isAmountConfigured: false })])[0]))
  })

  it('已配置金额按两位小数呈现；金额缺失时按未配置呈现', () => {
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: node(12.5) })])[0])).toBe('12.50')
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: node(1234) })])[0])).toBe('1234.00')
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: null })])[0])).toBe('未配置')
  })

  it('金额与已配置标记自相矛盾时按未配置归一，不呈现「未配置 + 数字」的混态', () => {
    const [configuredFlagOnly, amountOnly] = toRecognitionAmountRows([
      readModel({ currentAmount: null, isAmountConfigured: true }),
      readModel({ currentAmount: node(5), isAmountConfigured: false }),
    ])

    expect(configuredFlagOnly.isAmountConfigured).toBe(false)
    expect(configuredFlagOnly.currentAmount).toBeNull()
    expect(amountOnly.isAmountConfigured).toBe(false)
    expect(amountOnly.currentAmount).toBeNull()
  })

  it('生成端把金额生成为未定型节点：接受节点形态，非数值形态不产生伪金额', () => {
    const [fromNode, fromString, fromObject, fromMissing] = toRecognitionAmountRows([
      readModel({ currentAmount: node(7.25) }),
      readModel({ currentAmount: '7.25' as unknown as RecognitionAmountReadModelLike['currentAmount'] }),
      readModel({ currentAmount: { value: {} } }),
      readModel({ currentAmount: undefined }),
    ])

    expect(fromNode.currentAmount).toBe(7.25)
    expect(fromString.currentAmount).toBeNull()
    expect(fromObject.currentAmount).toBeNull()
    expect(fromMissing.currentAmount).toBeNull()
  })

  it('已知枚举映射为数值取值并给出中文，未知与缺失一律安全兜底', () => {
    const [known, unknown, missing] = toRecognitionAmountRows([
      readModel({ itemType: 1, configurationStatus: 2 }),
      readModel({ itemType: 7, configurationStatus: 9 }),
      readModel({ itemType: null, configurationStatus: undefined }),
    ])

    expect(known.itemType).toBe(1)
    expect(known.itemTypeText).toBe('检查')
    expect(known.configurationStatus).toBe(2)
    expect(known.configurationStatusText).toBe('停用')

    expect(unknown.itemType).toBeNull()
    expect(unknown.itemTypeText).toBe('未知类型')
    expect(unknown.configurationStatus).toBeNull()
    expect(unknown.configurationStatusText).toBe('未知')

    expect(missing.itemType).toBeNull()
    expect(missing.configurationStatus).toBeNull()
  })

  it('枚举中文以服务端字段为准，服务端未交付时才回退本模块兜底', () => {
    const [fromServer, blank, absent] = toRecognitionAmountRows([
      readModel({ itemType: 1, configurationStatus: 2, itemTypeText: '服务端类型', configurationStatusText: '服务端状态' }),
      readModel({ itemType: 1, configurationStatus: 2, itemTypeText: '   ', configurationStatusText: '' }),
      readModel({ itemType: 1, configurationStatus: 2 }),
    ])

    // 服务端交付时以服务端为准，不在前端再映射一份中文。
    expect(fromServer.itemTypeText).toBe('服务端类型')
    expect(fromServer.configurationStatusText).toBe('服务端状态')
    // 空白视为未交付，回退到本模块兜底出口。
    expect(blank.itemTypeText).toBe('检查')
    expect(blank.configurationStatusText).toBe('停用')
    expect(absent.itemTypeText).toBe('检查')
    expect(absent.configurationStatusText).toBe('停用')
  })

  it('行视图模型只承载查询契约交付的字段，保留 null 不可用原因', () => {
    const [row] = toRecognitionAmountRows([readModel({ unavailableReason: '所属分组已停用' })])

    expect(Object.keys(row)).toEqual([
      'standardProjectCode',
      'standardProjectName',
      'itemType',
      'itemTypeText',
      'categoryName',
      'groupName',
      'organizationName',
      'hospitalName',
      'branchName',
      'configurationStatus',
      'configurationStatusText',
      'unavailableReason',
      'currentAmount',
      'isAmountConfigured',
    ])
    expect(row.unavailableReason).toBe('所属分组已停用')
    const [empty] = toRecognitionAmountRows([readModel({ unavailableReason: null })])
    expect(empty.unavailableReason).toBeNull()
  })

  it('生成端缺省的字段一律归一为 null，不伪造已知状态', () => {
    const [row] = toRecognitionAmountRows([{}])

    expect(row).toEqual({
      standardProjectCode: null,
      standardProjectName: null,
      itemType: null,
      itemTypeText: '未知类型',
      categoryName: null,
      groupName: null,
      organizationName: null,
      hospitalName: null,
      branchName: null,
      configurationStatus: null,
      configurationStatusText: '未知',
      unavailableReason: null,
      currentAmount: null,
      isAmountConfigured: false,
    })
  })
})

describe('适配层 I/O 出口的真实请求体形状', () => {
  it('C2 平台管理员页查询显式下发组织、医院、院区三项', async () => {
    const client = stubClient()
    const request = buildRecognitionAmountListQuery({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })

    expect(await queryRecognitionAmountRows(client, request!)).toEqual([])
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionAmountList.post)
    expect(body).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
  })

  it('C4 医院管理员页查询只下发院区与标准项目编码', async () => {
    const client = stubClient({ queryBranch: [readModel()] })
    const request = buildBranchAmountListQuery({ branchCode: 'BRA-1', standardProjectCode: 'A01' })

    const rows = await queryBranchRecognitionAmountRows(client, request!)
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryBranchRecognitionAmountList.post)
    expect(body).toEqual({ branchCode: 'BRA-1', standardProjectCode: 'A01' })
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
    expect(rows[0].standardProjectCode).toBe('A01')
    expect(rows[0].currentAmount).toBe(12.5)
  })

  it('查询返回生成端可空形态时归一为空列表，不伪造行', async () => {
    expect(await queryRecognitionAmountRows(stubClient(), buildRecognitionAmountListQuery({ organizationCode: 'O', hospitalCode: 'H', branchCode: 'B' })!)).toEqual([])
    expect(await queryBranchRecognitionAmountRows(stubClient(), buildBranchAmountListQuery({ branchCode: 'B' })!)).toEqual([])
  })

  it('C2 平台管理员页保存下发组织、医院、院区、标准项目编码与金额，不提交已变更属性集', async () => {
    const client = stubClient()
    const payload = buildRecognitionAmountSavePayload({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', amountInput: '12.50' }, 'A01')

    await saveOrganizationHospitalBranchRecognitionAmount(client, payload!)
    const body = bodyOf(client.api.medicalRecognitionReport.saveOrganizationHospitalBranchRecognitionAmount.post)
    expect(body).toMatchObject({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
    })
    expect(Object.keys(body)).toEqual(['organizationCode', 'hospitalCode', 'branchCode', 'standardProjectCode', 'currentAmount'])
    expect(body).not.toHaveProperty('changedProperties')
    expect(amountOf(body)).toBe(12.5)
  })

  it('C4 医院管理员页保存只下发院区、标准项目编码与金额，不含组织与医院', async () => {
    const client = stubClient()
    const payload = buildBranchAmountSavePayload({ branchCode: 'BRA-1', amountInput: '0' }, 'A01')

    await saveBranchRecognitionAmount(client, payload!)
    const body = bodyOf(client.api.medicalRecognitionReport.saveBranchRecognitionAmount.post)
    expect(Object.keys(body)).toEqual(['branchCode', 'standardProjectCode', 'currentAmount'])
    expect(body).toMatchObject({ branchCode: 'BRA-1', standardProjectCode: 'A01' })
    expect(amountOf(body)).toBe(0)
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
  })

  it('零元以数值 0 提交，且金额按生成端要求的未定型节点形态承载（不是裸数字）', async () => {
    const client = stubClient()
    await saveBranchRecognitionAmount(client, buildBranchAmountSavePayload({ branchCode: 'B', amountInput: '0' }, 'A01')!)

    const body = bodyOf(client.api.medicalRecognitionReport.saveBranchRecognitionAmount.post)
    // 生成端把该属性交给 `writeObjectValue`：裸数字会被序列化成对象；节点形态才会写成数值。
    expect(typeof body.currentAmount).not.toBe('number')
    expect((body.currentAmount as { getValue: () => unknown }).getValue()).toBe(0)
  })
})

describe('适配层枚举与文案出口的唯一性', () => {
  it('枚举取值与后端声明一致，未知一律安全兜底', () => {
    expect(CONFIGURATION_STATUSES).toEqual([1, 2])
    expect(configurationStatusText(1)).toBe('启用')
    expect(configurationStatusText(2)).toBe('停用')
    expect(configurationStatusText(null)).toBe('未知')
    expect(medicalItemTypeText(0)).toBe('检验')
    expect(medicalItemTypeText(1)).toBe('检查')
    expect(medicalItemTypeText(null)).toBe('未知类型')
  })

  /**
   * 越界与错误类型必须落到「未知」而不是空文本，也不能被对象下标强制转换伪造成已知文案：
   * 字符串 `'1'` 与数字 `1` 在 `Record` 下标下都能取到「检查」，因此文案出口按取值域判定而不是按「是否等于 null」。
   */
  it('越界取值与字符串下标都显示未知，不伪造已知文案', () => {
    expect(medicalItemTypeText(7 as never)).toBe('未知类型')
    expect(medicalItemTypeText(undefined as never)).toBe('未知类型')
    expect(medicalItemTypeText(Number.NaN as never)).toBe('未知类型')
    expect(medicalItemTypeText('1' as never)).toBe('未知类型')

    expect(configurationStatusText(7 as never)).toBe('未知')
    expect(configurationStatusText(undefined as never)).toBe('未知')
    expect(configurationStatusText('1' as never)).toBe('未知')
  })

  it('取值域守卫只认已确认的数值取值', () => {
    expect([1, 2, 0, null, undefined, Number.NaN, '1', '01'].map((value) => isConfigurationStatusValue(value))).toEqual([true, true, false, false, false, false, false, false])
    expect([0, 1, 2, null, undefined, Number.NaN, '0', true].map((value) => isMedicalItemTypeValue(value))).toEqual([true, true, false, false, false, false, false, false])
  })

  it('行视图模型类型只暴露可空归一后的取值', () => {
    const row: RecognitionAmountRow = toRecognitionAmountRows([readModel({ itemType: 0, configurationStatus: 1 })])[0]
    expect(row.configurationStatus).toBe(1)
  })
})
