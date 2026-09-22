/**
 * 来源医院被互认统计适配层的请求形状、分页收敛与读模型映射用例
 * （阶段 6 前端测试矩阵 C4 / C5 / C6 的来源侧适配层单元面）。
 *
 * 装配边界：与接收侧适配层测试一致——纯映射函数用真实实现；I/O 出口用生成端同形的伪 Client
 * 驱动，断言真实下发的请求体形状，不构造真实鉴权适配器、不替换任何模块。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  buildSourceDetailsQuery,
  buildSourceExportQuery,
  buildSourceSummaryQuery,
  exportSourceStatistics,
  querySourceDetailsPage,
  querySourceSummaryPage,
  toSourceDetailRows,
  toSourceSummaryRows,
  type MedicalRecognitionClient,
  type PlatformSourceExportQuery,
  type PlatformSourceSummaryQuery,
  type SourceRecognitionDetailReadModel,
  type SourceRecognitionSummaryReadModel,
} from './sourceRecognitionStatisticsApi'

function stubClient(handlers: Record<string, unknown> = {}) {
  const post = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const client = {
    api: {
      medicalRecognitionReportQuery: {
        querySourceRecognitionSummary: { post: post('summaryPlatform') },
        queryBranchSourceRecognitionSummary: { post: post('summaryBranch') },
        querySourceRecognitionDetails: { post: post('detailsPlatform') },
        queryBranchSourceRecognitionDetails: { post: post('detailsBranch') },
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

const pageInput = { pageIndex: 1, pageSize: 20 }

const summaryPageResponse = {
  items: [],
  page: { pageIndex: 1, pageSize: 20, totalCount: 3 },
}

describe('C4 平台版来源侧汇总请求：来源组与接收组三级范围都随请求提交', () => {
  it('请求体携带两组组织编码、日期原样、维度与内嵌分页', async () => {
    const built = buildSourceSummaryQuery(
      {
        version: 'platform',
        startTime: '2026-03-01',
        endTime: '2026-03-31',
        groupDimension: 1,
        sourceOrganizationCode: 'ORG2',
        sourceHospitalCode: 'HOS2',
        sourceBranchCode: 'BR2',
        receiverOrganizationCode: 'ORG1',
        receiverHospitalCode: 'HOS1',
        receiverBranchCode: 'BR1',
        standardProjectCode: ' A01 ',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ summaryPlatform: summaryPageResponse })
    await querySourceSummaryPage(client, built as NonNullable<typeof built>)
    const post = client.api.medicalRecognitionReportQuery.querySourceRecognitionSummary.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.sourceOrganizationCode).toBe('ORG2')
    expect(body.sourceHospitalCode).toBe('HOS2')
    expect(body.sourceBranchCode).toBe('BR2')
    expect(body.receiverOrganizationCode).toBe('ORG1')
    expect(body.receiverHospitalCode).toBe('HOS1')
    expect(body.receiverBranchCode).toBe('BR1')
    expect(body.standardProjectCode).toBe('A01')
    expect(body.startTime).toEqual({ year: 2026, month: 3, day: 1 })
    expect(body.endTime).toEqual({ year: 2026, month: 3, day: 31 })
    expect(body.groupDimension).toBe(1)
    expect(body.page).toEqual({ pageIndex: 1, pageSize: 20 })
  })
})

describe('C4 本院版来源侧请求：本侧来源组织与医院、接收组织都不提交', () => {
  it('汇总请求只提交本侧来源院区与接收组医院/院区', async () => {
    const built = buildSourceSummaryQuery(
      {
        version: 'branch',
        startTime: '2026-03-01',
        endTime: '2026-03-31',
        groupDimension: 2,
        sourceBranchCode: 'BR9',
        receiverHospitalCode: 'HOS1',
        receiverBranchCode: 'BR1',
      },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ summaryBranch: summaryPageResponse })
    await querySourceSummaryPage(client, built as NonNullable<typeof built>)
    const post = client.api.medicalRecognitionReportQuery.queryBranchSourceRecognitionSummary.post
    expect(post).toHaveBeenCalledTimes(1)
    const body = bodyOf(post)
    expect(body.sourceBranchCode).toBe('BR9')
    expect(body.receiverHospitalCode).toBe('HOS1')
    expect(body.receiverBranchCode).toBe('BR1')
    expect(body.page).toEqual({ pageIndex: 1, pageSize: 20 })
    expect(body).not.toHaveProperty('sourceOrganizationCode')
    expect(body).not.toHaveProperty('sourceHospitalCode')
    expect(body).not.toHaveProperty('receiverOrganizationCode')
  })

  it('明细请求走 queryBranchSourceRecognitionDetails，来源侧没有明细类型字段', async () => {
    const built = buildSourceDetailsQuery(
      { version: 'branch', startTime: '2026-03-01', endTime: '2026-03-31', receiverBranchCode: 'BR1' },
      pageInput,
    )
    expect(built).not.toBeNull()

    const client = stubClient({ detailsBranch: summaryPageResponse })
    await querySourceDetailsPage(client, built as NonNullable<typeof built>)
    const body = bodyOf(client.api.medicalRecognitionReportQuery.queryBranchSourceRecognitionDetails.post)
    expect(body.receiverBranchCode).toBe('BR1')
    expect(body).not.toHaveProperty('detailType')
    expect(body).not.toHaveProperty('recognitionDoctorId')
    expect(body).not.toHaveProperty('nonAdoptionReasonCode')
    expect(body).not.toHaveProperty('sourceOrganizationCode')
    expect(body).not.toHaveProperty('receiverOrganizationCode')
  })
})

describe('C4 来源侧构造拒绝面与导出请求', () => {
  it('日期、维度、项目类型或分页非法拒绝构造', () => {
    const base: PlatformSourceSummaryQuery = {
      version: 'platform',
      startTime: '2026-03-01',
      endTime: '2026-03-31',
      groupDimension: 1,
    }
    expect(buildSourceSummaryQuery({ ...base, startTime: '20260301' }, pageInput)).toBeNull()
    expect(buildSourceSummaryQuery(invalidQuery(base, { groupDimension: 5 }), pageInput)).toBeNull()
    expect(buildSourceSummaryQuery(invalidQuery(base, { itemType: 7 }), pageInput)).toBeNull()
    expect(buildSourceSummaryQuery(base, { pageIndex: 0, pageSize: 20 })).toBeNull()
    expect(buildSourceDetailsQuery(base, { pageIndex: 1, pageSize: 201 })).toBeNull()
  })

  it('来源侧导出只接受导出类型 6-7；平台版提交两组组织编码且无分页', async () => {
    const base: PlatformSourceExportQuery = {
      version: 'platform',
      exportType: 6,
      groupDimension: 1,
      startTime: '2026-03-01',
      endTime: '2026-03-31',
      sourceOrganizationCode: 'ORG2',
      receiverOrganizationCode: 'ORG1',
    }
    expect(buildSourceExportQuery(invalidQuery(base, { exportType: 1 }))).toBeNull()
    expect(buildSourceExportQuery(invalidQuery(base, { exportType: 5 }))).toBeNull()

    const built = buildSourceExportQuery(base)
    expect(built).not.toBeNull()
    const client = stubClient({ exportPlatform: new ArrayBuffer(4) })
    await exportSourceStatistics(client, built as NonNullable<typeof built>, '来源医院被互认汇总.xlsx')
    const body = bodyOf(client.api.v1.statisticsExport.platform.post)
    expect(body.exportType).toBe(6)
    expect(body.sourceOrganizationCode).toBe('ORG2')
    expect(body.receiverOrganizationCode).toBe('ORG1')
    expect(body).not.toHaveProperty('page')
  })

  it('本院版来源侧导出走 branch 端点且不提交两组组织编码', async () => {
    const built = buildSourceExportQuery({
      version: 'branch',
      exportType: 7,
      groupDimension: 4,
      startTime: '2026-03-01',
      endTime: '2026-03-31',
      sourceBranchCode: 'BR9',
      receiverHospitalCode: 'HOS1',
    })
    expect(built).not.toBeNull()
    const client = stubClient({ exportBranch: new ArrayBuffer(4) })
    await exportSourceStatistics(client, built as NonNullable<typeof built>, '来源医院被互认明细.xlsx')
    const body = bodyOf(client.api.v1.statisticsExport.branch.post)
    expect(body.exportType).toBe(7)
    // 导出请求的本侧院区是统一槽位 branchCode（查询请求里按来源侧命名为 sourceBranchCode，导出请求统一）。
    expect(body.branchCode).toBe('BR9')
    expect(body.receiverHospitalCode).toBe('HOS1')
    expect(body).not.toHaveProperty('sourceOrganizationCode')
    expect(body).not.toHaveProperty('receiverOrganizationCode')
  })
})

/** 来源医院维度的汇总行：标准项目与院区字段为 null。 */
const sourceHospitalRow = {
  groupDimension: 1,
  sourceOrganizationCode: 'ORG2',
  sourceOrganizationName: '来源组织',
  sourceHospitalCode: 'HOS2',
  sourceHospitalName: '来源医院',
  recognitionCount: 12,
} as unknown as SourceRecognitionSummaryReadModel

describe('C6 来源侧汇总读模型映射', () => {
  it('来源组维度字段保留，未参与分组字段为 null，被互认次数映射', () => {
    const rows = toSourceSummaryRows([sourceHospitalRow])
    expect(rows[0].groupDimension).toBe(1)
    expect(rows[0].sourceOrganizationCode).toBe('ORG2')
    expect(rows[0].sourceHospitalName).toBe('来源医院')
    expect(rows[0].sourceBranchCode).toBeNull()
    expect(rows[0].itemType).toBeNull()
    expect(rows[0].standardProjectCode).toBeNull()
    expect(rows[0].recognitionCount).toBe(12)
  })

  it('标准项目维度行映射项目类型与文本，服务端文本优先', () => {
    const rows = toSourceSummaryRows([
      {
        ...sourceHospitalRow,
        groupDimension: 4,
        itemType: 1,
        itemTypeText: '检查',
        standardProjectCode: 'B02',
        standardProjectName: 'CT',
        categoryName: '检查分类',
        groupName: '影像',
      } as unknown as SourceRecognitionSummaryReadModel,
    ])
    expect(rows[0].itemType).toBe(1)
    expect(rows[0].itemTypeText).toBe('检查')
    expect(rows[0].standardProjectName).toBe('CT')
  })
})

/** 来源侧明细行：患者字段原值、双方归属平铺。 */
const sourceDetailRow = {
  recognitionMatchRecordId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c011',
  recognitionMatchItemId: '6ef0d691-0d1e-4f0f-9f2c-3bd6f4d5c012',
  sourceOrganizationCode: 'ORG2',
  sourceOrganizationName: '来源组织',
  sourceHospitalCode: 'HOS2',
  sourceHospitalName: '来源医院',
  sourceBranchCode: 'BR2',
  sourceBranchName: '来源院区',
  receiverOrganizationCode: 'ORG1',
  receiverOrganizationName: '组织一',
  receiverHospitalCode: 'HOS1',
  receiverHospitalName: '医院一',
  receiverBranchCode: 'BR1',
  receiverBranchName: '院区一',
  standardProjectCode: 'A01',
  recognitionDeptId: 'DEPT1',
  recognitionDeptName: '检验科',
  recognitionDoctorId: 'DOC1',
  recognitionDoctorName: '李四',
  recognitionTime: new Date('2026-03-05T10:00:00'),
  patientName: ' 王五',
  identityDocumentNo: 'id-raw',
} as unknown as SourceRecognitionDetailReadModel

describe('C6 来源侧明细读模型映射', () => {
  it('患者姓名与证件号码原样承载，双方归属与科室医生映射', () => {
    const rows = toSourceDetailRows([sourceDetailRow])
    expect(rows[0].patientName).toBe(' 王五')
    expect(rows[0].identityDocumentNo).toBe('id-raw')
    expect(rows[0].source.hospitalCode).toBe('HOS2')
    expect(rows[0].source.branchName).toBe('来源院区')
    expect(rows[0].receiver.hospitalName).toBe('医院一')
    expect(rows[0].standardProjectCode).toBe('A01')
    expect(rows[0].recognitionDeptName).toBe('检验科')
    expect(rows[0].recognitionDoctorName).toBe('李四')
    expect(rows[0].recognitionTime).toEqual(new Date('2026-03-05T10:00:00'))
  })

  it('科室与医生缺失时保持 null', () => {
    const rows = toSourceDetailRows([
      { ...sourceDetailRow, recognitionDeptId: null, recognitionDeptName: null, recognitionDoctorId: null, recognitionDoctorName: null },
    ])
    expect(rows[0].recognitionDeptId).toBeNull()
    expect(rows[0].recognitionDoctorName).toBeNull()
  })
})
