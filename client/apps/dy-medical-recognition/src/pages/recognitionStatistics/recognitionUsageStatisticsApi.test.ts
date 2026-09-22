/**
 * 接收侧互认使用统计适配层的请求形状、分页收敛与读模型映射用例
 * （阶段 6 前端测试矩阵 C4 / C5 / C6 的适配层单元面）。
 *
 * 装配边界：纯映射函数用真实实现；I/O 出口用生成端同形的伪 Client 驱动，
 * 断言真实下发的请求体形状（平台版两组三级范围、本院版无本侧组织与医院字段、
 * 分页内嵌 page 对象、日期原样提交），不构造真实鉴权适配器、不替换任何模块。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  buildUsageDetailsQuery,
  buildUsageExportQuery,
  buildUsageSummaryQuery,
  exportUsageStatistics,
  queryRecognitionMatchRecord,
  queryUsageDetailsPage,
  queryUsageSummaryPage,
  toMatchRecordView,
  toUsageDetailRows,
  toUsageSummaryRows,
  type MedicalRecognitionClient,
  type PlatformUsageExportQuery,
  type RecognitionMatchRecordReadModel,
  type RecognitionUsageDetailReadModel,
  type RecognitionUsageSummaryReadModel,
} from './recognitionUsageStatisticsApi'

/**
 * 生成端的伪 Client：只保留被测适配层实际调用的端点，`post` 记录请求体。
 * 返回体保持生成端可空形态（`undefined` / `null`），由适配层负责归一。
 */
function stubClient(handlers: Record<string, unknown> = {}) {
  const post = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const client = {
    api: {
      medicalRecognitionReportQuery: {
        queryRecognitionUsageSummary: { post: post('summaryPlatform') },
        queryBranchRecognitionUsageSummary: { post: post('summaryBranch') },
        queryRecognitionUsageDetails: { post: post('detailsPlatform') },
        queryBranchRecognitionUsageDetails: { post: post('detailsBranch') },
        queryRecognitionMatchRecord: { post: post('matchRecord') },
      },
      v1: {
        statisticsExport: {
          platform: { post: post('exportPlatform') },
          branch: { post: post('exportBranch') },
        },
      },
    },
  }
  return client as unknown as MedicalRecognitionClient & typeof client
}

const bodyOf = (mock: { mock: { calls: unknown[][] } }): Record<string, unknown> =>
  mock.mock.calls[0][0] as Record<string, unknown>

/**
 * 拒绝面用例需要构造运行期非法取值；查询类型在编译期把合法取值收窄到了字面量域，
 * 这里用合并放宽类型，让构造函数自己的取值域判定来拒绝。
 */
function invalidQuery<T extends object>(query: T, overrides: Record<string, unknown>): T {
  return Object.assign({ ...query }, overrides)
}

/** 生成端反序列化后的十进制形态：`UntypedNode`（`value` + `getValue()`）。 */
const node = (value: unknown) => ({ value, getValue: () => value })

/** 平台版汇总的合法条件：日期、维度必填，两组范围三级齐全。 */
const platformSummaryQuery = {
  version: 'platform',
  startTime: '2026-02-01',
  endTime: '2026-02-28',
  groupDimension: 1,
  receiverOrganizationCode: ' ORG1 ',
  receiverHospitalCode: 'HOS1',
  receiverBranchCode: 'BR1',
  sourceOrganizationCode: 'ORG2',
  sourceHospitalCode: 'HOS2',
  sourceBranchCode: 'BR2',
  recognitionDeptId: 'DEPT1',
  itemType: 0,
  categoryName: '检验分类',
  groupName: '血液',
  standardProjectCode: 'A01',
} as const

const pageInput = { pageIndex: 2, pageSize: 50 }

const summaryPageResponse = {
  items: [],
  page: { pageIndex: 2, pageSize: 50, totalCount: 137 },
}

describe('C4 平台版汇总请求：接收组与来源组三级范围都随请求提交', () => {
  it('请求体携带两组组织编码、日期按年月日原样提交、分页内嵌 page 对象', async () => {
    const built = buildUsageSummaryQuery(platformSummaryQuery, pageInput)
    expect(built).not.toBeNull()

    const client = stubClient({ summaryPlatform: summaryPageResponse })
    await queryUsageSummaryPage(client, built as NonNullable<typeof built>)

    const post = client.api.medicalRecognitionReportQuery.queryRecognitionUsageSummary.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    // 范围条件去首尾空白后按值下发；空白条件的处理在专用用例断言。
    expect(body.organizationCode).toBe('ORG1')
    expect(body.hospitalCode).toBe('HOS1')
    expect(body.branchCode).toBe('BR1')
    expect(body.sourceOrganizationCode).toBe('ORG2')
    expect(body.sourceHospitalCode).toBe('HOS2')
    expect(body.sourceBranchCode).toBe('BR2')
    expect(body.recognitionDeptId).toBe('DEPT1')
    expect(body.itemType).toBe(0)
    expect(body.categoryName).toBe('检验分类')
    expect(body.groupName).toBe('血液')
    expect(body.standardProjectCode).toBe('A01')
    // 日期原样提交：只按年月日构造 DateOnly，无时分秒、无时区偏移。
    expect(body.startTime).toEqual({ year: 2026, month: 2, day: 1 })
    expect(body.endTime).toEqual({ year: 2026, month: 2, day: 28 })
    expect(body.groupDimension).toBe(1)
    // 分页内嵌 page 对象。
    expect(body.page).toEqual({ pageIndex: 2, pageSize: 50 })
  })

  it('可选条件空白或缺失时不下发该字段', async () => {
    const built = buildUsageSummaryQuery(
      {
        version: 'platform',
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        groupDimension: 4,
        receiverOrganizationCode: '   ',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ summaryPlatform: summaryPageResponse })
    await queryUsageSummaryPage(client, built as NonNullable<typeof built>)
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryRecognitionUsageSummary.post)
    for (const key of [
      'organizationCode',
      'hospitalCode',
      'branchCode',
      'sourceOrganizationCode',
      'sourceHospitalCode',
      'sourceBranchCode',
      'recognitionDeptId',
      'itemType',
      'categoryName',
      'groupName',
      'standardProjectCode',
    ]) {
      expect(body).not.toHaveProperty(key)
    }
  })
})

describe('C4 本院版汇总请求：类型与请求体都不含本侧组织与医院', () => {
  it('请求体只提交本侧院区与来源组医院/院区，组织字段一律不出现', async () => {
    const built = buildUsageSummaryQuery(
      {
        version: 'branch',
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        groupDimension: 2,
        branchCode: 'BR9',
        sourceHospitalCode: 'HOS2',
        sourceBranchCode: 'BR2',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ summaryBranch: summaryPageResponse })
    await queryUsageSummaryPage(client, built as NonNullable<typeof built>)
    const post = client.api.medicalRecognitionReportQuery.queryBranchRecognitionUsageSummary.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.branchCode).toBe('BR9')
    expect(body.sourceHospitalCode).toBe('HOS2')
    expect(body.sourceBranchCode).toBe('BR2')
    expect(body.groupDimension).toBe(2)
    expect(body.page).toEqual({ pageIndex: 2, pageSize: 50 })
    // 本侧组织与医院、来源组织由服务端注入，请求体不得出现这些键。
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
    expect(body).not.toHaveProperty('sourceOrganizationCode')
  })
})

describe('C4 接收侧明细请求：明细类型必填，医生与原因只进明细', () => {
  it('平台版明细请求携带明细类型、互认医生与不采纳原因代码', async () => {
    const built = buildUsageDetailsQuery(
      {
        version: 'platform',
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        detailType: 3,
        receiverOrganizationCode: 'ORG1',
        recognitionDoctorId: 'DOC1',
        nonAdoptionReasonCode: 'R1',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ detailsPlatform: summaryPageResponse })
    await queryUsageDetailsPage(client, built as NonNullable<typeof built>)
    const post = client.api.medicalRecognitionReportQuery.queryRecognitionUsageDetails.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.detailType).toBe(3)
    expect(body.organizationCode).toBe('ORG1')
    expect(body.recognitionDoctorId).toBe('DOC1')
    expect(body.nonAdoptionReasonCode).toBe('R1')
    expect(body.page).toEqual({ pageIndex: 2, pageSize: 50 })
  })

  it('本院版明细请求不提交组织与医院字段', async () => {
    const built = buildUsageDetailsQuery(
      {
        version: 'branch',
        startTime: '2026-02-01',
        endTime: '2026-02-28',
        detailType: 1,
        sourceBranchCode: 'BR2',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ detailsBranch: summaryPageResponse })
    await queryUsageDetailsPage(client, built as NonNullable<typeof built>)
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryBranchRecognitionUsageDetails.post)
    expect(body.detailType).toBe(1)
    expect(body.sourceBranchCode).toBe('BR2')
    expect(body).not.toHaveProperty('organizationCode')
    expect(body).not.toHaveProperty('hospitalCode')
    expect(body).not.toHaveProperty('sourceOrganizationCode')
  })
})

describe('C4 请求构造拒绝面：条件不完整或越界返回 null，零发请求', () => {
  it('日期缺失、形态非法或越界维度都拒绝构造', () => {
    expect(buildUsageSummaryQuery({ ...platformSummaryQuery, startTime: '' }, pageInput)).toBeNull()
    expect(buildUsageSummaryQuery({ ...platformSummaryQuery, startTime: '2026/02/01' }, pageInput)).toBeNull()
    expect(buildUsageSummaryQuery({ ...platformSummaryQuery, endTime: '2026-13-01' }, pageInput)).toBeNull()
    // 汇总维度取值必须在 1-4 内（0 与 99 都越界）。
    expect(buildUsageSummaryQuery(invalidQuery(platformSummaryQuery, { groupDimension: 0 }), pageInput)).toBeNull()
    expect(buildUsageSummaryQuery(invalidQuery(platformSummaryQuery, { groupDimension: 99 }), pageInput)).toBeNull()
    expect(buildUsageSummaryQuery(invalidQuery(platformSummaryQuery, { itemType: 9 }), pageInput)).toBeNull()
  })

  it('页码小于 1、页容量越界或非整数都拒绝构造', () => {
    expect(buildUsageSummaryQuery(platformSummaryQuery, { pageIndex: 0, pageSize: 50 })).toBeNull()
    expect(buildUsageSummaryQuery(platformSummaryQuery, { pageIndex: 1, pageSize: 201 })).toBeNull()
    expect(buildUsageSummaryQuery(platformSummaryQuery, { pageIndex: 1.5, pageSize: 50 })).toBeNull()
  })

  it('明细类型缺失或越界拒绝构造明细查询', () => {
    expect(
      buildUsageDetailsQuery(
        invalidQuery({ version: 'platform', startTime: '2026-02-01', endTime: '2026-02-28', detailType: 1 }, { detailType: 9 }),
        pageInput,
      ),
    ).toBeNull()
  })

  it('导出条件缺失日期、导出类型或维度越界拒绝构造', () => {
    expect(buildUsageExportQuery(invalidQuery(platformExportQuery, { startTime: '' }))).toBeNull()
    expect(buildUsageExportQuery(invalidQuery(platformExportQuery, { exportType: 6 }))).toBeNull()
    expect(buildUsageExportQuery(invalidQuery(platformExportQuery, { groupDimension: 9 }))).toBeNull()
  })
})

const platformExportQuery: PlatformUsageExportQuery = {
  version: 'platform',
  exportType: 1,
  groupDimension: 1,
  startTime: '2026-02-01',
  endTime: '2026-02-28',
  receiverOrganizationCode: 'ORG1',
  sourceOrganizationCode: 'ORG2',
}

describe('C4 导出请求：平台/本院双版本映射，导出与匹配记录无分页对象', () => {
  it('平台版导出提交两组组织编码与导出类型，请求体无分页', async () => {
    const built = buildUsageExportQuery(platformExportQuery)
    expect(built).not.toBeNull()

    const client = stubClient({ exportPlatform: new ArrayBuffer(8) })
    await exportUsageStatistics(client, built as NonNullable<typeof built>, '接收侧互认使用汇总.xlsx')
    const post = client.api.v1.statisticsExport.platform.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.exportType).toBe(1)
    expect(body.groupDimension).toBe(1)
    expect(body.receiverOrganizationCode).toBe('ORG1')
    expect(body.sourceOrganizationCode).toBe('ORG2')
    expect(body.startTime).toEqual({ year: 2026, month: 2, day: 1 })
    expect(body).not.toHaveProperty('page')
  })

  it('本院版导出不提交本侧组织与医院，走本院导出端点', async () => {
    const built = buildUsageExportQuery({
      version: 'branch',
      exportType: 2,
      groupDimension: 4,
      startTime: '2026-02-01',
      endTime: '2026-02-28',
      branchCode: 'BR9',
      sourceHospitalCode: 'HOS2',
      receiverBranchCode: 'BR1',
    })
    expect(built).not.toBeNull()

    const client = stubClient({ exportBranch: new ArrayBuffer(8) })
    await exportUsageStatistics(client, built as NonNullable<typeof built>, '接收侧提醒明细.xlsx')
    const post = client.api.v1.statisticsExport.branch.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.exportType).toBe(2)
    expect(body.branchCode).toBe('BR9')
    expect(body.sourceHospitalCode).toBe('HOS2')
    expect(body.receiverBranchCode).toBe('BR1')
    expect(body).not.toHaveProperty('receiverOrganizationCode')
    expect(body).not.toHaveProperty('sourceOrganizationCode')
    expect(body).not.toHaveProperty('page')
  })
})

describe('C5 分页响应收敛：安全整数与值域校验', () => {
  it('合法响应收敛为页面分页模型，总数直传', async () => {
    const built = buildUsageSummaryQuery(platformSummaryQuery, pageInput) as BuiltSummaryQuery
    const client = stubClient({
      summaryPlatform: { items: [hospitalDimensionRow], page: { pageIndex: 2, pageSize: 50, totalCount: 137 } },
    })
    const result = await queryUsageSummaryPage(client, built)
    expect(result).toEqual({ items: expect.any(Array), pageIndex: 2, pageSize: 50, totalCount: 137 })
    expect(result.items).toHaveLength(1)
  })

  it('当页集合缺失按空集合处理', async () => {
    const built = buildUsageSummaryQuery(platformSummaryQuery, pageInput) as BuiltSummaryQuery
    const client = stubClient({ summaryPlatform: { page: { pageIndex: 1, pageSize: 50, totalCount: 0 } } })
    const result = await queryUsageSummaryPage(client, built)
    expect(result.items).toEqual([])
    expect(result.totalCount).toBe(0)
  })

  it.each([
    ['页码非法', { items: [], page: { pageIndex: 0, pageSize: 50, totalCount: 1 } }, '统计分页响应的页码非法。'],
    ['页容量越界', { items: [], page: { pageIndex: 1, pageSize: 201, totalCount: 1 } }, '统计分页响应的页容量非法。'],
    ['总数为负', { items: [], page: { pageIndex: 1, pageSize: 50, totalCount: -1 } }, '统计分页响应的总数非法。'],
    [
      '总数非安全整数',
      { items: [], page: { pageIndex: 1, pageSize: 50, totalCount: 1.5 } },
      '统计分页响应的总数非法。',
    ],
    ['分页对象缺失', { items: [] }, '统计分页响应的页码非法。'],
  ])('%s按契约错误抛出', async (_name, response, message) => {
    const built = buildUsageSummaryQuery(platformSummaryQuery, pageInput) as BuiltSummaryQuery
    const client = stubClient({ summaryPlatform: response })
    await expect(queryUsageSummaryPage(client, built)).rejects.toThrow(message)
  })
})

type BuiltSummaryQuery = NonNullable<ReturnType<typeof buildUsageSummaryQuery>>
type BuiltDetailsQuery = NonNullable<ReturnType<typeof buildUsageDetailsQuery>>

/** 医院维度汇总行：维度字段齐全，标准项目与科室字段为 null。 */
const hospitalDimensionRow = {
  groupDimension: 1,
  receiverOrganizationCode: 'ORG1',
  receiverOrganizationName: '组织一',
  receiverHospitalCode: 'HOS1',
  receiverHospitalName: '医院一',
  reminderCount: 10,
  adoptionCount: 6,
  nonAdoptionCount: 2,
  referenceCount: 4,
  samePeriodRecognitionRateCalculated: true,
  samePeriodRecognitionRate: node(0.6),
  estimatedSavingAmount: node(320.5),
  nonAdoptionReasons: [{ reasonCode: 'R1', reasonName: '结果互认', count: 2, ratio: node(1) }],
} as unknown as RecognitionUsageSummaryReadModel

describe('C6 汇总读模型映射：维度字段、枚举文本、十进制与原因集合', () => {
  it('医院维度行保留接收组维度字段，未参与分组的维度字段为 null', () => {
    const rows = toUsageSummaryRows([hospitalDimensionRow])
    expect(rows).toHaveLength(1)
    const row = rows[0]
    expect(row.groupDimension).toBe(1)
    expect(row.receiverOrganizationCode).toBe('ORG1')
    expect(row.receiverOrganizationName).toBe('组织一')
    expect(row.receiverHospitalCode).toBe('HOS1')
    expect(row.receiverHospitalName).toBe('医院一')
    // 未参与分组的维度字段保持 null，页面据此不渲染对应列。
    expect(row.receiverBranchCode).toBeNull()
    expect(row.recognitionDeptId).toBeNull()
    expect(row.itemType).toBeNull()
    expect(row.standardProjectCode).toBeNull()
    expect(row.categoryName).toBeNull()
    // 六项指标与金额、互认率、原因嵌套集合。
    expect(row.reminderCount).toBe(10)
    expect(row.adoptionCount).toBe(6)
    expect(row.nonAdoptionCount).toBe(2)
    expect(row.referenceCount).toBe(4)
    expect(row.samePeriodRecognitionRate).toBe(0.6)
    expect(row.estimatedSavingAmount).toBe(320.5)
    expect(row.nonAdoptionReasons).toEqual([{ reasonCode: 'R1', reasonName: '结果互认', count: 2, ratio: 1 }])
  })

  it('同期互认率未计算时为 null，页面按「—」展示；十进制节点缺失同样为 null', () => {
    const rows = toUsageSummaryRows([
      { ...hospitalDimensionRow, samePeriodRecognitionRateCalculated: false } as RecognitionUsageSummaryReadModel,
    ])
    expect(rows[0].samePeriodRecognitionRate).toBeNull()

    const missing = toUsageSummaryRows([
      { ...hospitalDimensionRow, estimatedSavingAmount: null } as RecognitionUsageSummaryReadModel,
    ])
    expect(missing[0].estimatedSavingAmount).toBeNull()
  })

  it('项目类型随行映射数值与文本：服务端文本优先，未知取值兜底「未知类型」', () => {
    const rows = toUsageSummaryRows([
      {
        ...hospitalDimensionRow,
        groupDimension: 4,
        itemType: 1,
        itemTypeText: '检查（服务端）',
        standardProjectCode: 'A01',
        standardProjectName: '血常规',
        categoryName: '检验分类',
        groupName: '血液',
      } as unknown as RecognitionUsageSummaryReadModel,
    ])
    expect(rows[0].itemType).toBe(1)
    expect(rows[0].itemTypeText).toBe('检查（服务端）')

    const unknown = toUsageSummaryRows([
      {
        ...hospitalDimensionRow,
        itemType: 9,
        itemTypeText: null,
      } as unknown as RecognitionUsageSummaryReadModel,
    ])
    expect(unknown[0].itemType).toBeNull()
    expect(unknown[0].itemTypeText).toBe('未知类型')
  })
})

/** 明细行：提醒口径的未反馈行（处理结果与引用事实缺省）。 */
const reminderDetailRow = {
  recognitionMatchRecordId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001',
  recognitionMatchItemId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c002',
  matchCreatedTime: new Date('2026-02-10T08:30:00'),
  businessTime: new Date('2026-02-10T08:30:00'),
  visitType: 1,
  visitTypeText: '门诊',
  visitSerialNo: 'MZ20260210-001',
  source: {
    organizationCode: 'ORG2',
    organizationName: '来源组织',
    hospitalCode: 'HOS2',
    hospitalName: '来源医院',
    branchCode: 'BR2',
    branchName: '来源院区',
  },
  receiver: {
    organizationCode: 'ORG1',
    organizationName: '组织一',
    hospitalCode: 'HOS1',
    hospitalName: '医院一',
    branchCode: 'BR1',
    branchName: '院区一',
  },
  item: { itemType: 0, itemTypeText: '检验', standardProjectCode: 'A01', standardProjectName: '血常规' },
  isUnprocessed: true,
  patientName: '张三 ',
  identityDocumentNo: 'id-lower',
} as unknown as RecognitionUsageDetailReadModel

describe('C6 明细读模型映射：患者字段原样、未反馈行、处理结果与引用事实', () => {
  it('患者姓名与证件号码按服务端原值承载，不做改写', () => {
    const rows = toUsageDetailRows([reminderDetailRow])
    expect(rows[0].patientName).toBe('张三 ')
    expect(rows[0].identityDocumentNo).toBe('id-lower')
  })

  it('未反馈行保留 isUnprocessed 标记，就诊类型文本服务端优先', () => {
    const rows = toUsageDetailRows([{ ...reminderDetailRow, processingResult: null, reference: null }])
    expect(rows[0].isUnprocessed).toBe(true)
    expect(rows[0].processingResult).toBeNull()
    expect(rows[0].reference).toBeNull()
    expect(rows[0].visitType).toBe(1)
    expect(rows[0].visitTypeText).toBe('门诊')
    expect(rows[0].businessTime).toEqual(new Date('2026-02-10T08:30:00'))
  })

  it('处理结果与引用事实映射：决定文本服务端优先，未知决定兜底', () => {
    const rows = toUsageDetailRows([
      {
        ...reminderDetailRow,
        isUnprocessed: false,
        processingResult: {
          isProcessed: true,
          recognitionTime: new Date('2026-02-11T09:00:00'),
          decision: 2,
          decisionText: '不采纳（服务端）',
          nonAdoptionReasonCode: 'R1',
          nonAdoptionReasonName: '结果互认',
          nonAdoptionSupplementDescription: '补充说明',
          estimatedSavingAmount: node(0),
        },
        reference: {
          isReferenced: true,
          referenceTime: new Date('2026-02-12T10:00:00'),
          referenceDeptId: 'D1',
          referenceDeptName: '检验科',
          referenceDoctorId: 'DOC1',
          referenceDoctorName: '李四',
        },
      } as unknown as RecognitionUsageDetailReadModel,
    ])
    expect(rows[0].processingResult?.decision).toBe(2)
    expect(rows[0].processingResult?.decisionText).toBe('不采纳（服务端）')
    expect(rows[0].processingResult?.estimatedSavingAmount).toBe(0)
    expect(rows[0].reference?.referenceDeptName).toBe('检验科')

    const unknownDecision = toUsageDetailRows([
      {
        ...reminderDetailRow,
        processingResult: { isProcessed: true, decision: 9, decisionText: null },
      } as unknown as RecognitionUsageDetailReadModel,
    ])
    // 未登记的决定值按原值直传（只读展示，不据此执行写操作），文案兜底「未知状态」。
    expect(unknownDecision[0].processingResult?.decision).toBe(9)
    expect(unknownDecision[0].processingResult?.decisionText).toBe('未知状态')
  })
})

describe('C6 匹配记录集合视图映射', () => {
  const matchRecord = {
    recognitionMatchRecordId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001',
    matchCreatedTime: new Date('2026-02-10T08:30:00'),
    receiver: { organizationCode: 'ORG1', hospitalCode: 'HOS1', branchCode: 'BR1' },
    patientName: '张三',
    identityDocumentNo: 'ID-UPPER',
    visitType: 2,
    visitTypeText: '急诊',
    visitSerialNo: 'JZ20260210-001',
    isProcessed: true,
    recognitionTime: new Date('2026-02-11T09:00:00'),
    recognitionDeptId: 'DEPT1',
    recognitionDeptName: '检验科',
    matchItems: [
      {
        recognitionMatchItemId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c002',
        item: { itemType: 0, standardProjectCode: 'A01', standardProjectName: '血常规' },
        source: { organizationCode: 'ORG2', hospitalCode: 'HOS2' },
        reportId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c003',
        reportVersionId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c004',
        isProcessed: true,
        decision: 1,
        decisionText: '采纳',
      },
    ],
  } as unknown as RecognitionMatchRecordReadModel

  it('组级信息、患者就诊与全部匹配项映射', () => {
    const view = toMatchRecordView(matchRecord)
    expect(view.recognitionMatchRecordId).toBe('6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001')
    expect(view.patientName).toBe('张三')
    expect(view.identityDocumentNo).toBe('ID-UPPER')
    expect(view.visitTypeText).toBe('急诊')
    expect(view.receiver.hospitalCode).toBe('HOS1')
    expect(view.matchItems).toHaveLength(1)
    expect(view.matchItems[0].reportId).toBe('6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c003')
    expect(view.matchItems[0].decision).toBe(1)
    expect(view.matchItems[0].item?.standardProjectCode).toBe('A01')
  })

  it('生成端未返回记录时按契约错误抛出', async () => {
    const client = stubClient()
    await expect(queryRecognitionMatchRecord(client, '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001')).rejects.toThrow(
      '互认匹配记录未返回内容。',
    )
  })

  it('弹窗查询提交匹配记录标识', async () => {
    const client = stubClient({ matchRecord })
    await queryRecognitionMatchRecord(client, '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001')
    const post = client.api.medicalRecognitionReportQuery.queryRecognitionMatchRecord.post
    expect(post).toHaveBeenCalledTimes(1)
    expect(bodyOf(post)).toEqual({ recognitionMatchRecordId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c001' })
  })
})

describe('C4 明细分页查询收敛（本院版走本院端点）', () => {
  it('本院明细请求走 queryBranchRecognitionUsageDetails 并收敛分页', async () => {
    const built = buildUsageDetailsQuery(
      { version: 'branch', startTime: '2026-02-01', endTime: '2026-02-28', detailType: 1 },
      pageInput,
    ) as BuiltDetailsQuery
    const client = stubClient({ detailsBranch: summaryPageResponse })
    const result = await queryUsageDetailsPage(client, built)
    expect(result.pageIndex).toBe(2)
    expect(result.totalCount).toBe(137)
  })
})
