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
  EMPTY_RECOGNITION_AMOUNT_FILTER,
  buildAmountFilterSearch,
  buildBranchAmountListQuery,
  buildBranchAmountSavePayload,
  buildBranchRecognitionAmountExportDownloadName,
  buildBranchRecognitionAmountExportPayload,
  buildRecognitionAmountExportDownloadName,
  buildRecognitionAmountExportPayload,
  buildRecognitionAmountListQuery,
  buildRecognitionAmountSavePayload,
  configurationStatusText,
  currentAmountText,
  exportBranchRecognitionAmounts,
  exportRecognitionAmounts,
  filterRecognitionAmountRows,
  isConfigurationStatusValue,
  isMedicalItemTypeValue,
  medicalItemTypeText,
  parseAmountFilterSearch,
  parseRecognitionAmount,
  queryBranchRecognitionAmountRows,
  queryRecognitionAmountRows,
  readAmountFilterFromLocation,
  recheckRecognitionAmountScope,
  saveBranchRecognitionAmount,
  saveOrganizationHospitalBranchRecognitionAmount,
  syncAmountFilterToLocation,
  toRecognitionAmountColumnView,
  toRecognitionAmountRows,
  type MedicalRecognitionClient,
  type RecognitionAmountReadModelLike,
  type RecognitionAmountRow,
  type RecognitionAmountScope,
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
      v1: {
        recognitionExport: {
          branchRecognitionAmounts: { post: post('exportBranchAmounts') },
          recognitionAmounts: { post: post('exportAmounts') },
        },
      },
    },
  }
  return client as unknown as MedicalRecognitionClient & typeof client
}

const bodyOf = (mock: { mock: { calls: unknown[][] } }): Record<string, unknown> =>
  mock.mock.calls[0][0] as Record<string, unknown>

/** 提交载荷里的金额取值：`decimal` 字段按 `number | null` 生成，直接写出数值。 */
const amountOf = (body: Record<string, unknown>): unknown => body.currentAmount

/** 生成端反序列化后的金额形态：`number | null`。 */
const amount = (value: number | null) => value

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
  currentAmount: 12.5,
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
    const [row] = toRecognitionAmountRows([readModel({ currentAmount: amount(0), isAmountConfigured: true })])

    expect(row.currentAmount).toBe(0)
    expect(row.isAmountConfigured).toBe(true)
    expect(currentAmountText(row)).toBe('0.00')
    expect(currentAmountText(row)).not.toBe(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: null, isAmountConfigured: false })])[0]))
  })

  it('已配置金额按两位小数呈现；金额缺失时按未配置呈现', () => {
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: amount(12.5) })])[0])).toBe('12.50')
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: amount(1234) })])[0])).toBe('1234.00')
    expect(currentAmountText(toRecognitionAmountRows([readModel({ currentAmount: null })])[0])).toBe('未配置')
  })

  it('金额与已配置标记自相矛盾时按未配置归一，不呈现「未配置 + 数字」的混态', () => {
    const [configuredFlagOnly, amountOnly] = toRecognitionAmountRows([
      readModel({ currentAmount: null, isAmountConfigured: true }),
      readModel({ currentAmount: amount(5), isAmountConfigured: false }),
    ])

    expect(configuredFlagOnly.isAmountConfigured).toBe(false)
    expect(configuredFlagOnly.currentAmount).toBeNull()
    expect(amountOnly.isAmountConfigured).toBe(false)
    expect(amountOnly.currentAmount).toBeNull()
  })

  it('金额按数值合约读取：接受有限数值，非数值与缺失不产生伪金额', () => {
    const [fromNumber, fromString, fromInfinite, fromMissing] = toRecognitionAmountRows([
      readModel({ currentAmount: amount(7.25) }),
      readModel({ currentAmount: '7.25' as unknown as RecognitionAmountReadModelLike['currentAmount'] }),
      readModel({ currentAmount: Number.POSITIVE_INFINITY }),
      readModel({ currentAmount: undefined }),
    ])

    expect(fromNumber.currentAmount).toBe(7.25)
    expect(fromString.currentAmount).toBeNull()
    expect(fromInfinite.currentAmount).toBeNull()
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

  it('零元以数值 0 提交，金额属性直接承载数值', async () => {
    const client = stubClient()
    await saveBranchRecognitionAmount(client, buildBranchAmountSavePayload({ branchCode: 'B', amountInput: '0' }, 'A01')!)

    const body = bodyOf(client.api.medicalRecognitionReport.saveBranchRecognitionAmount.post)
    expect(body.currentAmount).toBe(0)
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
   * 越界与错误类型一律归入「未知」，兜底取值不产出空文本，也不允许被对象下标强制转换伪造成已知文案：
   * 字符串 `'1'` 与数字 `1` 在 `Record` 下标下都能取到「检查」，因此文案出口按取值域判定，
   * `null` 与其它越界取值同属取值域之外的兜底取值。
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

describe('关键字下推：去空白后为空不携带，非空按去空白后的值携带', () => {
  const platformBase = { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }

  it('平台管理员页查询的关键字非空才携带，空白与缺省都不携带该字段', () => {
    expect(buildRecognitionAmountListQuery({ ...platformBase, keyword: ' 血常规 ' })).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      keyword: '血常规',
    })
    expect(buildRecognitionAmountListQuery({ ...platformBase, keyword: '' })).not.toHaveProperty('keyword')
    expect(buildRecognitionAmountListQuery({ ...platformBase, keyword: '   ' })).not.toHaveProperty('keyword')
    expect(buildRecognitionAmountListQuery(platformBase)).not.toHaveProperty('keyword')
  })

  it('医院管理员页查询的关键字同样只在非空时携带，院区校验与标准项目编码行为不变', () => {
    expect(buildBranchAmountListQuery({ branchCode: ' BRA-1 ', standardProjectCode: ' A01 ', keyword: ' 血常规 ' })).toEqual({
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      keyword: '血常规',
    })
    expect(buildBranchAmountListQuery({ branchCode: 'BRA-1', keyword: '  ' })).toEqual({ branchCode: 'BRA-1' })
    expect(buildBranchAmountListQuery({ branchCode: '  ', keyword: '血常规' })).toBeNull()
    expect(buildBranchAmountListQuery({ branchCode: 'BRA-1' })).not.toHaveProperty('keyword')
  })

  it('平台管理员页查询请求体有值才写关键字', async () => {
    const client = stubClient()
    const request = buildRecognitionAmountListQuery({ ...platformBase, keyword: ' 血常规 ' })

    await queryRecognitionAmountRows(client, request!)
    expect(bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionAmountList.post)).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      keyword: '血常规',
    })
  })

  it('平台管理员页查询不带关键字时请求体没有该字段', async () => {
    const client = stubClient()

    await queryRecognitionAmountRows(client, buildRecognitionAmountListQuery(platformBase)!)
    expect(bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionAmountList.post)).not.toHaveProperty('keyword')
  })

  it('医院管理员页查询请求体同样只在有值时写关键字', async () => {
    const withKeyword = stubClient()
    await queryBranchRecognitionAmountRows(withKeyword, buildBranchAmountListQuery({ branchCode: 'BRA-1', keyword: ' 血常规 ' })!)
    expect(bodyOf(withKeyword.api.medicalRecognitionReportQuery.queryBranchRecognitionAmountList.post)).toEqual({
      branchCode: 'BRA-1',
      keyword: '血常规',
    })

    const withoutKeyword = stubClient()
    await queryBranchRecognitionAmountRows(withoutKeyword, buildBranchAmountListQuery({ branchCode: 'BRA-1' })!)
    expect(bodyOf(withoutKeyword.api.medicalRecognitionReportQuery.queryBranchRecognitionAmountList.post)).toEqual({ branchCode: 'BRA-1' })

    const blankKeyword = stubClient()
    await queryBranchRecognitionAmountRows(blankKeyword, { branchCode: 'BRA-1', keyword: '   ' })
    expect(bodyOf(blankKeyword.api.medicalRecognitionReportQuery.queryBranchRecognitionAmountList.post)).not.toHaveProperty('keyword')
  })
})

describe('四栏层级列视图：类型 → 分类 → 分组 → 项目行', () => {
  const amountRow = (overrides: Partial<RecognitionAmountReadModelLike>): RecognitionAmountRow =>
    toRecognitionAmountRows([readModel(overrides)])[0]

  it('按输入行序折叠：类型、分类、分组的格次序与组内项目行序都与输入一致，层级归属与行数不变', () => {
    const view = toRecognitionAmountColumnView([
      amountRow({ standardProjectCode: 'B02', itemType: 1, categoryName: 'CHECK-CAT', groupName: 'G2' }),
      amountRow({ standardProjectCode: 'A03', itemType: 0, categoryName: 'LAB-CAT', groupName: 'G2' }),
      amountRow({ standardProjectCode: 'A01', itemType: 0, categoryName: 'LAB-CAT', groupName: 'G1' }),
      amountRow({ standardProjectCode: 'A02', itemType: 0, categoryName: 'LAB-CAT', groupName: 'G1' }),
      amountRow({ standardProjectCode: 'A00', itemType: 0, categoryName: 'B-CAT', groupName: 'G9' }),
    ])

    // 输入以「检查」行开头：类型格的次序与输入首次出现的次序一致（行序由服务端交付，本层只折叠）。
    expect(view.types.map((type) => type.itemType)).toEqual([1, 0])
    const [checkType, labType] = view.types
    expect(checkType.itemTypeText).toBe('检查')
    expect(checkType.rowSpan).toBe(1)
    expect(labType.itemTypeText).toBe('检验')
    expect(labType.rowSpan).toBe(4)

    // 分类格按该类型内首次出现的次序排列：LAB-CAT 先于 B-CAT。
    expect(labType.categories.map((category) => category.categoryName)).toEqual(['LAB-CAT', 'B-CAT'])
    expect(labType.categories[0].rowSpan).toBe(3)
    expect(labType.categories[1].rowSpan).toBe(1)

    // 分组格按该分类内首次出现的次序排列，组内项目行保持输入行序。
    const labCategory = labType.categories[0]
    expect(labCategory.groups.map((group) => group.groupName)).toEqual(['G2', 'G1'])
    expect(labCategory.groups[0].rowSpan).toBe(1)
    expect(labCategory.groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['A03'])
    expect(labCategory.groups[1].rowSpan).toBe(2)
    expect(labCategory.groups[1].rows.map((row) => row.standardProjectCode)).toEqual(['A01', 'A02'])

    const checkCategory = checkType.categories[0]
    expect(checkCategory.rowSpan).toBe(1)
    expect(checkCategory.groups[0].groupName).toBe('G2')
    expect(checkCategory.groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['B02'])
  })

  it('同名行归入同一格：未知类型的位置随输入行序，组内项目行保持输入行序', () => {
    const view = toRecognitionAmountColumnView([
      amountRow({ standardProjectCode: 'A05', itemType: 7, categoryName: 'CAT', groupName: 'G' }),
      amountRow({ standardProjectCode: 'A02', itemType: 0, categoryName: 'CAT', groupName: 'G' }),
      amountRow({ standardProjectCode: 'A01', itemType: 0, categoryName: 'CAT', groupName: 'G' }),
    ])

    // 未知类型在输入中最先出现，其类型格就排在最前，类型数值没有单独的排序规则。
    expect(view.types.map((type) => type.itemType)).toEqual([null, 0])
    expect(view.types[0].itemTypeText).toBe('未知类型')
    expect(view.types[1].itemTypeText).toBe('检验')
    expect(view.types[0].categories[0].groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['A05'])
    expect(view.types[1].categories[0].groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['A02', 'A01'])
  })

  it('分类名与分组名为 null 时按独立键折叠，格次序随输入首次出现的次序', () => {
    const view = toRecognitionAmountColumnView([
      amountRow({ standardProjectCode: 'A04', categoryName: 'B分类', groupName: 'G2' }),
      amountRow({ standardProjectCode: 'A03', categoryName: 'B分类', groupName: null }),
      amountRow({ standardProjectCode: 'A02', categoryName: null, groupName: 'G1' }),
      amountRow({ standardProjectCode: 'A01', categoryName: null, groupName: null }),
    ])

    const [labType] = view.types
    expect(labType.rowSpan).toBe(4)
    // 分类级：B分类 先出现排在前，null 分类在后；同名行的 `null` 归入同一分类格。
    expect(labType.categories.map((category) => category.categoryName)).toEqual(['B分类', null])
    expect(labType.categories[0].rowSpan).toBe(2)
    expect(labType.categories[1].rowSpan).toBe(2)

    // 分组级：null 与具名分组各自成格，格次序随输入。
    const bCategory = labType.categories[0]
    expect(bCategory.groups.map((group) => group.groupName)).toEqual(['G2', null])
    expect(bCategory.groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['A04'])
    expect(bCategory.groups[1].rows.map((row) => row.standardProjectCode)).toEqual(['A03'])

    const nullCategory = labType.categories[1]
    expect(nullCategory.groups.map((group) => group.groupName)).toEqual(['G1', null])
    expect(nullCategory.groups[0].rows.map((row) => row.standardProjectCode)).toEqual(['A02'])
    expect(nullCategory.groups[1].rows.map((row) => row.standardProjectCode)).toEqual(['A01'])
  })

  it('项目行承载编码、名称、当前金额、已配置标记、配置状态值与其中文、不可用原因', () => {
    const view = toRecognitionAmountColumnView([
      amountRow({ standardProjectCode: 'A01', currentAmount: amount(12.5), isAmountConfigured: true, configurationStatus: 2, unavailableReason: '所属分组已停用' }),
    ])

    expect(view.types).toHaveLength(1)
    const [row] = view.types[0].categories[0].groups[0].rows
    expect(Object.keys(row)).toEqual([
      'standardProjectCode',
      'standardProjectName',
      'currentAmount',
      'isAmountConfigured',
      'configurationStatus',
      'configurationStatusText',
      'unavailableReason',
    ])
    expect(row).toMatchObject({
      standardProjectCode: 'A01',
      standardProjectName: '血常规',
      currentAmount: 12.5,
      isAmountConfigured: true,
      configurationStatus: 2,
      configurationStatusText: '停用',
      unavailableReason: '所属分组已停用',
    })
  })

  it('空集合返回空结构', () => {
    expect(toRecognitionAmountColumnView([])).toEqual({ types: [] })
  })
})

describe('本地状态裁剪：金额状态与互认配置状态取交集，只裁剪不发请求', () => {
  const rows = toRecognitionAmountRows([
    readModel({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: amount(10), configurationStatus: 1 }),
    readModel({ standardProjectCode: 'A02', isAmountConfigured: false, currentAmount: null, configurationStatus: 1 }),
    readModel({ standardProjectCode: 'A03', isAmountConfigured: true, currentAmount: amount(20), configurationStatus: 2 }),
    readModel({ standardProjectCode: 'A04', isAmountConfigured: false, currentAmount: null, configurationStatus: 2 }),
    readModel({ standardProjectCode: 'A05', isAmountConfigured: false, currentAmount: null, configurationStatus: 9 }),
  ])
  const codesOf = (filter: { amountStatus: 'all' | 'configured' | 'unconfigured'; configurationStatus: 'all' | 'enabled' | 'disabled' }): Array<string | null> =>
    filterRecognitionAmountRows(rows, filter).map((row) => row.standardProjectCode)

  it('金额状态三档：全部保留全部行，已配置与未配置各自只留对应行', () => {
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'all' })).toEqual(['A01', 'A02', 'A03', 'A04', 'A05'])
    expect(codesOf({ amountStatus: 'configured', configurationStatus: 'all' })).toEqual(['A01', 'A03'])
    expect(codesOf({ amountStatus: 'unconfigured', configurationStatus: 'all' })).toEqual(['A02', 'A04', 'A05'])
  })

  it('互认配置状态三档：启用与停用按行上的配置状态数值取值', () => {
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'all' })).toEqual(['A01', 'A02', 'A03', 'A04', 'A05'])
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'enabled' })).toEqual(['A01', 'A02'])
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'disabled' })).toEqual(['A03', 'A04'])
  })

  it('两个条件取交集', () => {
    expect(codesOf({ amountStatus: 'configured', configurationStatus: 'enabled' })).toEqual(['A01'])
    expect(codesOf({ amountStatus: 'configured', configurationStatus: 'disabled' })).toEqual(['A03'])
    expect(codesOf({ amountStatus: 'unconfigured', configurationStatus: 'enabled' })).toEqual(['A02'])
    expect(codesOf({ amountStatus: 'unconfigured', configurationStatus: 'disabled' })).toEqual(['A04'])
  })

  it('配置状态未知的行在全部档下保留、在启用与停用档下都不出现', () => {
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'all' })).toContain('A05')
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'enabled' })).not.toContain('A05')
    expect(codesOf({ amountStatus: 'all', configurationStatus: 'disabled' })).not.toContain('A05')
  })

  it('裁剪保持原有行序，不改变行本身', () => {
    expect(filterRecognitionAmountRows(rows, { amountStatus: 'all', configurationStatus: 'all' })).toEqual(rows)
  })
})

describe('地址栏三个查询参数：解析与回写', () => {
  const appliedFilter = { keyword: 'CBC', amountStatus: 'configured', configurationStatus: 'disabled' } as const

  it('按三个参数初始化，参数缺失或取值不可识别时该条件回退默认值', () => {
    expect(parseAmountFilterSearch('keyword=%E8%A1%80%E5%B8%B8%E8%A7%84&amountStatus=unconfigured&configurationStatus=enabled')).toEqual({
      keyword: '血常规',
      amountStatus: 'unconfigured',
      configurationStatus: 'enabled',
    })
    expect(parseAmountFilterSearch('amountStatus=whatever&configurationStatus=7&keyword=')).toEqual({
      ...EMPTY_RECOGNITION_AMOUNT_FILTER,
    })
    expect(parseAmountFilterSearch('')).toEqual({ ...EMPTY_RECOGNITION_AMOUNT_FILTER })
  })

  it('回写只写非默认档与非空关键字，其它参数原样保留', () => {
    expect(buildAmountFilterSearch('?tab=summary', { ...EMPTY_RECOGNITION_AMOUNT_FILTER })).toBe('?tab=summary')
    expect(buildAmountFilterSearch('', { ...EMPTY_RECOGNITION_AMOUNT_FILTER })).toBe('')
    expect(buildAmountFilterSearch('?tab=summary', appliedFilter)).toBe(
      '?tab=summary&keyword=CBC&amountStatus=configured&configurationStatus=disabled',
    )
  })

  it('回写后可以按同一口径解析回来（关键字含中文也按原值往返）', () => {
    const search = buildAmountFilterSearch('?tab=summary', { ...appliedFilter, keyword: '血常规' })
    expect(parseAmountFilterSearch(search.startsWith('?') ? search.slice(1) : search)).toEqual({
      keyword: '血常规',
      amountStatus: 'configured',
      configurationStatus: 'disabled',
    })
  })

  it('读取当前地址栏上的已生效条件', () => {
    window.history.replaceState(null, '', '/subApps/medical-recognition/amounts?tab=summary&keyword=CBC&amountStatus=configured&configurationStatus=disabled')

    expect(readAmountFilterFromLocation()).toEqual({
      keyword: 'CBC',
      amountStatus: 'configured',
      configurationStatus: 'disabled',
    })
  })

  it('地址栏没有这三个参数时读到默认条件', () => {
    window.history.replaceState(null, '', '/subApps/medical-recognition/amounts')

    expect(readAmountFilterFromLocation()).toEqual({ ...EMPTY_RECOGNITION_AMOUNT_FILTER })
  })

  it('条件变化时用替换当前历史项的方式回写地址栏，其它参数原样保留', () => {
    window.history.replaceState(null, '', '/subApps/medical-recognition/amounts?tab=summary')
    const replaceState = vi.spyOn(window.history, 'replaceState')

    syncAmountFilterToLocation({ ...appliedFilter })

    expect(replaceState).toHaveBeenCalledTimes(1)
    const url = String(replaceState.mock.calls[0][2])
    expect(url).toContain('tab=summary')
    expect(url).toContain('keyword=CBC')
    expect(url).toContain('amountStatus=configured')
    expect(url).toContain('configurationStatus=disabled')
    replaceState.mockRestore()
  })

  it('内容未变化时不触达历史 API', () => {
    window.history.replaceState(null, '', '/subApps/medical-recognition/amounts?tab=summary&keyword=CBC&amountStatus=configured&configurationStatus=disabled')
    const replaceState = vi.spyOn(window.history, 'replaceState')

    syncAmountFilterToLocation({ ...appliedFilter })

    expect(replaceState).not.toHaveBeenCalled()
    replaceState.mockRestore()
  })

  it('默认条件不往地址栏写自己这三个参数', () => {
    window.history.replaceState(null, '', '/subApps/medical-recognition/amounts?tab=summary')

    syncAmountFilterToLocation({ ...EMPTY_RECOGNITION_AMOUNT_FILTER })

    expect(window.location.search).toBe('?tab=summary')
  })
})

describe('保存前的可信范围复核：不一致即失败，院区层按缺失值参与比对', () => {
  const platformScope = (overrides: Partial<RecognitionAmountScope> = {}): RecognitionAmountScope => ({
    organizationCode: 'ORG-A',
    hospitalCode: 'HOS-1',
    branchCode: 'BRA-1',
    ...overrides,
  })
  const platformLayers = ['organization', 'hospital', 'branch'] as const

  it('组织、医院、院区逐层一致时给出通过结论', () => {
    expect(recheckRecognitionAmountScope({ applied: platformScope(), trusted: platformScope(), layers: platformLayers })).toEqual({
      ok: true,
      reason: null,
    })
  })

  it('layers 为空数组表示本次不比较任何层级：两侧范围都存在即通过', () => {
    // 空集语义：本次不比较任何层级，逐层比较一次都不进入，两侧范围都存在时直接给出通过结论。
    expect(recheckRecognitionAmountScope({ applied: platformScope(), trusted: platformScope(), layers: [] })).toEqual({
      ok: true,
      reason: null,
    })

    // 同一空集下两侧层级取值不一致也不构成失败：没有任何层级参与比较，就没有可比出的差异。
    const mismatched = recheckRecognitionAmountScope({
      applied: platformScope({ organizationCode: 'ORG-B' }),
      trusted: platformScope(),
      layers: [],
    })
    expect(mismatched).toEqual({ ok: true, reason: null })
  })

  it('可信范围读不到时给出失败结论与一行中文原因', () => {
    const result = recheckRecognitionAmountScope({ applied: platformScope(), trusted: null, layers: platformLayers })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('读取')
    expect(result.reason).not.toContain('\n')
  })

  it('页面尚未应用范围时给出失败结论', () => {
    const result = recheckRecognitionAmountScope({ applied: null, trusted: platformScope(), layers: platformLayers })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('尚未应用')
  })

  it('组织、医院、院区任一层不一致都失败，原因指出具体层级', () => {
    const organizationMismatch = recheckRecognitionAmountScope({
      applied: platformScope({ organizationCode: 'ORG-B' }),
      trusted: platformScope(),
      layers: platformLayers,
    })
    expect(organizationMismatch.ok).toBe(false)
    expect(organizationMismatch.reason).toContain('组织')

    const hospitalMismatch = recheckRecognitionAmountScope({
      applied: platformScope({ hospitalCode: 'HOS-2' }),
      trusted: platformScope(),
      layers: platformLayers,
    })
    expect(hospitalMismatch.ok).toBe(false)
    expect(hospitalMismatch.reason).toContain('医院')

    const branchMismatch = recheckRecognitionAmountScope({
      applied: platformScope({ branchCode: 'BRA-2' }),
      trusted: platformScope(),
      layers: platformLayers,
    })
    expect(branchMismatch.ok).toBe(false)
    expect(branchMismatch.reason).toContain('院区')
  })

  it('多层同时不一致时按组织、医院、院区的固定次序给出第一个原因', () => {
    const result = recheckRecognitionAmountScope({
      applied: platformScope({ organizationCode: 'ORG-B', hospitalCode: 'HOS-2', branchCode: 'BRA-2' }),
      trusted: platformScope(),
      layers: platformLayers,
    })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('组织')
  })

  it('医院管理员页只比较院区：组织与医院没有已应用值也不阻断', () => {
    const applied: RecognitionAmountScope = { organizationCode: null, hospitalCode: null, branchCode: 'BRA-1' }

    expect(recheckRecognitionAmountScope({ applied, trusted: platformScope(), layers: ['branch'] })).toEqual({
      ok: true,
      reason: null,
    })
    const fullLayers = recheckRecognitionAmountScope({ applied, trusted: platformScope(), layers: platformLayers })
    expect(fullLayers.ok).toBe(false)
    expect(fullLayers.reason).toContain('组织')
  })

  it('医院管理员页院区不一致时同样失败', () => {
    const result = recheckRecognitionAmountScope({
      applied: { organizationCode: null, hospitalCode: null, branchCode: 'BRA-2' },
      trusted: platformScope(),
      layers: ['branch'],
    })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('院区')
  })

  it('院区层：记录与现读同为缺失即通过（可信院区缺失属 S3-D22 正常形态）', () => {
    // 两侧都为 null：缺失归一后相等，给出通过结论（旧口径在这里把现读侧给成读不到）。
    const bothNull = recheckRecognitionAmountScope({
      applied: platformScope({ branchCode: null }),
      trusted: platformScope({ branchCode: null }),
      layers: ['branch'],
    })
    expect(bothNull).toEqual({ ok: true, reason: null })

    // 空串与纯空白同样归一为缺失：记录侧写空、现读侧写 null 也相等。
    const blankAndNull = recheckRecognitionAmountScope({
      applied: platformScope({ branchCode: '  ' }),
      trusted: platformScope({ branchCode: '' }),
      layers: ['branch'],
    })
    expect(blankAndNull).toEqual({ ok: true, reason: null })
  })

  it('院区层：记录有值而现读缺失时失败，原因为已变化', () => {
    const result = recheckRecognitionAmountScope({
      applied: platformScope(),
      trusted: platformScope({ branchCode: null }),
      layers: ['branch'],
    })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('已变化')
    expect(result.reason).toContain('院区')
    // 院区层不再以「读不到」「尚未应用」收场：缺失只按一致性比对给出结论。
    expect(result.reason).not.toContain('读取')
    expect(result.reason).not.toContain('尚未应用')
  })

  it('院区层：记录缺失而现读有值时失败，原因为已变化', () => {
    const result = recheckRecognitionAmountScope({
      applied: platformScope({ branchCode: null }),
      trusted: platformScope(),
      layers: ['branch'],
    })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('已变化')
    expect(result.reason).toContain('院区')
    expect(result.reason).not.toContain('尚未应用')
    expect(result.reason).not.toContain('读取')
  })

  it('组织层：现读为空仍返回读取不到的结论（与医院层同一口径）', () => {
    const result = recheckRecognitionAmountScope({
      applied: platformScope(),
      trusted: platformScope({ organizationCode: '  ' }),
      layers: platformLayers,
    })

    expect(result.ok).toBe(false)
    expect(result.reason).toContain('读取')
  })

  it('比较两侧都取去空白后的值，医院层现读缺值仍按读不到处理', () => {
    const trimmedEqual = recheckRecognitionAmountScope({
      applied: platformScope({ branchCode: ' BRA-1 ' }),
      trusted: platformScope(),
      layers: platformLayers,
    })
    expect(trimmedEqual).toEqual({ ok: true, reason: null })

    const trustedBlank = recheckRecognitionAmountScope({
      applied: platformScope(),
      trusted: platformScope({ hospitalCode: '  ' }),
      layers: platformLayers,
    })
    expect(trustedBlank.ok).toBe(false)
    expect(trustedBlank.reason).toContain('读取')
  })
})

/**
 * 平台金额页导出适配（调用形态与互认项目页导出同款：经生成端点取回二进制字节，保存交共享下载模块）。
 * 导出只承载页面可产生的服务端查询条件——组织、医院、院区三值加已生效关键字；
 * 配置状态页签与金额状态档位不参与请求。
 */
describe('平台金额页导出适配', () => {
  it('构造导出请求：组织、医院、院区三值必填去空白，关键字按值下发、空白不下发', () => {
    expect(buildRecognitionAmountExportPayload(
      { organizationCode: ' ORG-A ', hospitalCode: ' HOS-1 ', branchCode: ' BRA-1 ' },
      ' 血 ',
    )).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(buildRecognitionAmountExportPayload(
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      '   ',
    ) as object)).toEqual(['organizationCode', 'hospitalCode', 'branchCode'])
    // 三值任一缺失或空白都不构造导出请求，不退化为全局导出或硬编码范围。
    expect(buildRecognitionAmountExportPayload({ organizationCode: '', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }, '血')).toBeNull()
    expect(buildRecognitionAmountExportPayload({ organizationCode: 'ORG-A', hospitalCode: '   ', branchCode: 'BRA-1' }, '血')).toBeNull()
    expect(buildRecognitionAmountExportPayload({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: '' }, '血')).toBeNull()
  })

  it('下载名由前端给定：固定前缀「互认项目金额维护」+ 发起时刻 14 位时间戳 + .xlsx', () => {
    expect(buildRecognitionAmountExportDownloadName(new Date(2026, 9, 14, 15, 30, 45))).toBe('互认项目金额维护20261014153045.xlsx')
    // 个位月、日、时、分、秒一律补零。
    expect(buildRecognitionAmountExportDownloadName(new Date(2026, 0, 2, 3, 4, 5))).toBe('互认项目金额维护20260102030405.xlsx')
  })

  it('导出请求体恰为范围三值加关键字（页签与金额状态档位不参与），字节按生成端原样返回', async () => {
    const content = new ArrayBuffer(8)
    const client = stubClient({ exportAmounts: content })
    const bytes = await exportRecognitionAmounts(
      client,
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '血' },
    )

    const body = bodyOf(client.api.v1.recognitionExport.recognitionAmounts.post)
    expect(body).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(body)).toEqual(['organizationCode', 'hospitalCode', 'branchCode', 'keyword'])
    expect(bytes).toBe(content)
  })

  it('关键字为空时请求体只有范围三值；生成端未返回字节归一为 0 长度，交共享模块按契约错误抛出', async () => {
    const client = stubClient()
    const bytes = await exportRecognitionAmounts(
      client,
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
    )

    const body = bodyOf(client.api.v1.recognitionExport.recognitionAmounts.post)
    expect(body).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    expect(Object.keys(body)).toEqual(['organizationCode', 'hospitalCode', 'branchCode'])
    expect(bytes.byteLength).toBe(0)
  })
})

/**
 * 本院区金额页导出适配（与平台金额页导出同款调用形态；差异只在范围来源——登录院区一个值，
 * 组织与医院由服务端从可信上下文解析，页面不提交）。导出只承载页面可产生的服务端查询条件
 * ——登录院区加已生效关键字；配置状态页签与金额状态档位不参与请求。
 */
describe('本院区金额页导出适配', () => {
  it('构造导出请求：登录院区必填去空白，关键字按值下发、空白不下发；院区缺失不构造请求', () => {
    expect(buildBranchRecognitionAmountExportPayload({ branchCode: ' BRA-1 ' }, ' 血 '))
      .toEqual({ branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(buildBranchRecognitionAmountExportPayload({ branchCode: 'BRA-1' }, '   ') as object))
      .toEqual(['branchCode'])
    // 登录院区缺失或空白都不构造导出请求（权限配置错误零请求，不用空院区或默认值兜底）。
    expect(buildBranchRecognitionAmountExportPayload({ branchCode: '' }, '血')).toBeNull()
    expect(buildBranchRecognitionAmountExportPayload({ branchCode: '   ' }, '血')).toBeNull()
  })

  it('下载名由前端给定：固定前缀「本院区互认项目金额」+ 发起时刻 14 位时间戳 + .xlsx', () => {
    expect(buildBranchRecognitionAmountExportDownloadName(new Date(2026, 9, 14, 15, 30, 45)))
      .toBe('本院区互认项目金额20261014153045.xlsx')
    // 个位月、日、时、分、秒一律补零。
    expect(buildBranchRecognitionAmountExportDownloadName(new Date(2026, 0, 2, 3, 4, 5)))
      .toBe('本院区互认项目金额20260102030405.xlsx')
  })

  it('导出请求体恰为登录院区加关键字（组织与医院不提交，档位不参与），字节按生成端原样返回', async () => {
    const content = new ArrayBuffer(8)
    const client = stubClient({ exportBranchAmounts: content })
    const bytes = await exportBranchRecognitionAmounts(client, { branchCode: 'BRA-1', keyword: '血' })

    const body = bodyOf(client.api.v1.recognitionExport.branchRecognitionAmounts.post)
    expect(body).toEqual({ branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(body)).toEqual(['branchCode', 'keyword'])
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
    expect(bytes).toBe(content)
  })

  it('关键字为空时请求体只有登录院区；生成端未返回字节归一为 0 长度，交共享模块按契约错误抛出', async () => {
    const client = stubClient()
    const bytes = await exportBranchRecognitionAmounts(client, { branchCode: 'BRA-1' })

    const body = bodyOf(client.api.v1.recognitionExport.branchRecognitionAmounts.post)
    expect(body).toEqual({ branchCode: 'BRA-1' })
    expect(Object.keys(body)).toEqual(['branchCode'])
    expect(bytes.byteLength).toBe(0)
  })
})
