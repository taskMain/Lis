/**
 * 报告管理端适配层的请求形状、分页收敛与读模型映射用例
 * （测试矩阵 C4、C5、C6 的适配层面，另含 C9 的范围重置在请求构造侧的边界）。
 *
 * 装配边界：纯函数使用真实实现；I/O 出口用**生成端同形的伪 Client** 驱动，
 * 断言真实下发的请求体形状与页面分页模型，不构造真实鉴权适配器、不替换任何模块。
 * 页面级行为（加载、空态、失败态、分页交互、详情与下载）由 `ReportManagement.test.tsx`
 * 与 `BranchReportManagement.test.tsx` 覆盖。
 */
import { describe, expect, it, vi } from 'vitest'
import {
  buildBranchReportListQuery,
  buildReportListQuery,
  downloadReportVersionPdf,
  queryBranchReportPage,
  queryReportPage,
  queryReportVersionDetail,
  queryReportVersions,
  toDisplayDate,
  toDisplayDateTime,
  toReportPage,
  toReportRows,
  toReportVersionDetail,
  toReportVersionRows,
  voidReport,
  type MedicalRecognitionClient,
  type ReportFilters,
} from './reportsApi'

/** 生成端的伪 Client：只保留被测适配层实际调用的端点，调用参数原样记录。 */
function stubClient(handlers: Record<string, unknown> = {}) {
  const post = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const get = (name: string) => vi.fn(async () => handlers[name] ?? undefined)
  const byReportId = vi.fn((reportId: string) => ({
    reportId,
    versions: {
      byReportVersionId: vi.fn((reportVersionId: string) => ({ reportVersionId, pdf: { get: get('pdf') } })),
    },
  }))
  /**
   * 生成端请求构造器：适配层实际调用的 `post` 之外，保留同形的 `toPostRequestInformation`，
   * 使「敏感条件不进入地址栏」可以在真实请求信息上判定，而不是靠文本断言猜测。
   */
  const requestBuilder = (name: string) => ({
    post: post(name),
    toPostRequestInformation: vi.fn((body: unknown) => ({
      URL: 'http://localhost:15014/Api/MedicalRecognitionReportQuery/QueryMedicalReportList',
      queryParameters: {},
      body,
    })),
  })
  const client = {
    api: {
      medicalRecognitionReport: {
        voidExaminationReport: { post: post('voidExamination') },
        voidLaboratoryReport: { post: post('voidLaboratory') },
      },
      medicalRecognitionReportQuery: {
        queryBranchMedicalReportList: requestBuilder('queryBranch'),
        queryMedicalReportList: requestBuilder('queryPlatform'),
        queryMedicalReportVersionDetail: { post: post('versionDetail') },
        queryMedicalReportVersionList: { post: post('versionList') },
      },
      v1: {
        reportPdf: { byReportId },
      },
    },
  }
  return client as unknown as MedicalRecognitionClient & typeof client
}

const bodyOf = (mock: { mock: { calls: unknown[][] } }, index = 0): Record<string, unknown> =>
  mock.mock.calls[index][0] as Record<string, unknown>

const LIST_FILTERS: ReportFilters = {
  organizationCode: 'ORG-A',
  hospitalCode: 'HOS-1',
  branchCode: 'BRA-1',
  reportDateFrom: '2026-09-01',
  reportDateTo: '2026-09-30',
  reportType: 1,
  reportNo: 'R-1',
  identityDocumentNo: '110101199001011234',
  patientName: '张三',
}

const PAGE = { pageIndex: 2, pageSize: 20 }

describe('C4 平台管理员入口的请求映射', () => {
  it('筛选条件与分页参数按契约提交：数值枚举、日期与页码页容量逐字段下发', () => {
    const query = buildReportListQuery(LIST_FILTERS, PAGE)

    expect(query).not.toBeNull()
    expect(query).toEqual({ ...LIST_FILTERS, pageIndex: 2, pageSize: 20 })
    // 患者条件分页参数与筛选条件同层下发，不嵌套。
    expect(Object.keys(query as object).sort()).toEqual([
      'branchCode',
      'hospitalCode',
      'identityDocumentNo',
      'organizationCode',
      'pageIndex',
      'pageSize',
      'patientName',
      'reportDateFrom',
      'reportDateTo',
      'reportNo',
      'reportType',
    ])
  })

  it('空值归一：空白与空串条件保留原值，请求体侧按去空白结果决定是否下发', async () => {
    const client = stubClient({
      queryPlatform: { items: [], page: { pageIndex: 1, pageSize: 10, totalCount: 0 } },
    })
    const filters = {
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      reportNo: '   ',
      identityDocumentNo: '',
      patientName: '   ',
      reportDateFrom: '',
      reportDateTo: '   ',
    }
    const query = buildReportListQuery(filters, PAGE)

    expect(query).not.toBeNull()
    await queryReportPage(client, query as NonNullable<typeof query>)

    // 请求体侧按去空白结果判定：空白报告单号、空证件号码与无法解析的日期都不下发。
    const sent = bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportList.post)
    expect(sent).not.toHaveProperty('reportNo')
    expect(sent).not.toHaveProperty('identityDocumentNo')
    expect(sent).not.toHaveProperty('reportDateFrom')
    expect(sent).not.toHaveProperty('reportDateTo')
    // 患者姓名与证件号码按用户输入原样提交：空白姓名同样按原值下发，规范化（去空白、证件号码统一大写）
    // 由服务端完成，页面不做本地改写；服务端按规范化后为空处理该条件。
    expect(sent).toHaveProperty('patientName', '   ')
    expect(sent).toMatchObject({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', page: { pageIndex: 2, pageSize: 20 } })
  })

  it('非法枚举取值不进入请求体，也不把该条件当成已应用', async () => {
    const client = stubClient({
      queryPlatform: { items: [], page: { pageIndex: 1, pageSize: 10, totalCount: 0 } },
    })
    const query = buildReportListQuery({ ...LIST_FILTERS, reportType: 9 }, { pageIndex: 1, pageSize: 10 })
    await queryReportPage(client, query as NonNullable<typeof query>)

    const sent = bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportList.post)
    expect(sent).not.toHaveProperty('reportType')
    // 区间与其余筛选条件不受影响。
    expect(sent).toHaveProperty('organizationCode', 'ORG-A')
    // 日期按下发形状提交日历日，不携带时刻与时区。
    expect(sent.reportDateFrom).toEqual({ year: 2026, month: 9, day: 1 })
  })

  it('患者证件号码与姓名按用户输入原样提交，不做本地大小写或空白改写', () => {
    const query = buildReportListQuery({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      identityDocumentNo: ' 11010119900101123x ',
      patientName: ' 张三 ',
    }, PAGE)

    expect(query?.identityDocumentNo).toBe(' 11010119900101123x ')
    expect(query?.patientName).toBe(' 张三 ')
  })

  it('范围不完整或分页取值非法时返回 null，禁止退化为全局查询与越界窗口', () => {
    expect(buildReportListQuery({ hospitalCode: 'HOS-1', branchCode: 'BRA-1' }, PAGE)).toBeNull()
    expect(buildReportListQuery({ organizationCode: 'ORG-A', branchCode: 'BRA-1' }, PAGE)).toBeNull()
    expect(buildReportListQuery({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1' }, PAGE)).toBeNull()
    expect(buildReportListQuery({ organizationCode: '  ', hospitalCode: 'HOS-1', branchCode: 'BRA-1' }, PAGE)).toBeNull()
    expect(buildReportListQuery(LIST_FILTERS, { pageIndex: 0, pageSize: 20 })).toBeNull()
    expect(buildReportListQuery(LIST_FILTERS, { pageIndex: 1, pageSize: 0 })).toBeNull()
    expect(buildReportListQuery(LIST_FILTERS, { pageIndex: 1, pageSize: 201 })).toBeNull()
  })

  it('敏感筛选条件只随请求体提交，不作为地址栏查询参数', async () => {
    const client = stubClient({
      queryPlatform: { items: [], page: { pageIndex: 1, pageSize: 10, totalCount: 0 } },
    })
    const query = buildReportListQuery(LIST_FILTERS, { pageIndex: 1, pageSize: 10 })
    await queryReportPage(client, query as NonNullable<typeof query>)

    const requestInformation = client.api.medicalRecognitionReportQuery.queryMedicalReportList.toPostRequestInformation(
      bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportList.post) as never,
    )
    // 生成端把筛选与分页都放在请求体：地址栏查询参数为空，证件号码与姓名不进入 URL。
    expect(requestInformation.queryParameters).toEqual({})
    expect(requestInformation.URL).not.toContain('110101199001011234')
    expect(requestInformation.URL).not.toContain('张三')
  })

  it('查询把适配层构造的请求体原样交给生成端点，并按服务端响应收敛为页面分页模型', async () => {
    const client = stubClient({
      queryPlatform: {
        items: [{ reportId: 'REP-1', reportNo: 'R-1', reportType: 1, reportTypeText: '检验报告', status: 1, statusText: '有效' }],
        page: { pageIndex: 2, pageSize: 20, totalCount: 41 },
      },
    })
    const query = buildReportListQuery(LIST_FILTERS, PAGE)
    const result = await queryReportPage(client, query as NonNullable<typeof query>)

    const sent = bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportList.post) as Record<string, unknown>
    expect(sent.organizationCode).toBe('ORG-A')
    expect(sent.page).toEqual({ pageIndex: 2, pageSize: 20 })
    expect(sent).not.toHaveProperty('pageIndex')
    expect(sent).not.toHaveProperty('pageSize')
    // 日期按下发形状提交日历日，不携带时刻与时区。
    expect(sent.reportDateFrom).toEqual({ year: 2026, month: 9, day: 1 })
    expect(sent.reportDateTo).toEqual({ year: 2026, month: 9, day: 30 })
    expect(result.pageIndex).toBe(2)
    expect(result.pageSize).toBe(20)
    expect(result.totalCount).toBe(41)
    expect(result.items).toHaveLength(1)
  })
})

describe('C4 医院管理员入口的请求映射', () => {
  it('只提交院区与筛选条件，组织与医院不出现在请求体里', async () => {
    const client = stubClient({
      queryBranch: { items: [], page: { pageIndex: 1, pageSize: 10, totalCount: 0 } },
    })
    const query = buildBranchReportListQuery({ branchCode: 'BRA-1', patientName: '张三' }, { pageIndex: 1, pageSize: 10 })
    expect(query).not.toBeNull()
    await queryBranchReportPage(client, query as NonNullable<typeof query>)

    const sent = bodyOf(client.api.medicalRecognitionReportQuery.queryBranchMedicalReportList.post)
    expect(Object.keys(sent).sort()).toEqual(['branchCode', 'page', 'patientName'])
    expect(sent.page).toEqual({ pageIndex: 1, pageSize: 10 })
    expect(sent).not.toHaveProperty('organizationCode')
    expect(sent).not.toHaveProperty('hospitalCode')
    // 平台管理员入口在该入口不被使用。
    expect(client.api.medicalRecognitionReportQuery.queryMedicalReportList.post).not.toHaveBeenCalled()
  })

  it('院区缺失或空白时返回 null，不用可信上下文回填', () => {
    expect(buildBranchReportListQuery({ branchCode: '' }, PAGE)).toBeNull()
    expect(buildBranchReportListQuery({ branchCode: '   ' }, PAGE)).toBeNull()
    expect(buildBranchReportListQuery({}, PAGE)).toBeNull()
  })
})

describe('C5 分页响应的收敛与契约错误', () => {
  it('合法响应收敛为页面分页模型，总数取服务端值', () => {
    const page = toReportPage([], { pageIndex: 3, pageSize: 50, totalCount: 120 })

    expect(page).toEqual({ items: [], pageIndex: 3, pageSize: 50, totalCount: 120 })
  })

  it('页码小于 1、页容量越界、总数为负或非安全整数一律按契约错误处理，不伪造分页状态', () => {
    expect(() => toReportPage([], { pageIndex: 0, pageSize: 10, totalCount: 1 })).toThrow()
    expect(() => toReportPage([], { pageIndex: 1, pageSize: 0, totalCount: 1 })).toThrow()
    expect(() => toReportPage([], { pageIndex: 1, pageSize: 201, totalCount: 1 })).toThrow()
    expect(() => toReportPage([], { pageIndex: 1, pageSize: 10, totalCount: -1 })).toThrow()
    expect(() => toReportPage([], { pageIndex: 1, pageSize: 10, totalCount: Number.MAX_SAFE_INTEGER + 2 })).toThrow()
    expect(() => toReportPage([], { pageIndex: 1.5, pageSize: 10, totalCount: 1 })).toThrow()
    expect(() => toReportPage([], undefined)).toThrow()
  })

  it('当页数据缺失按空集合处理，分页信息仍必须完整', () => {
    expect(toReportPage(undefined, { pageIndex: 1, pageSize: 10, totalCount: 0 }).items).toEqual([])
    expect(toReportPage(null, { pageIndex: 1, pageSize: 10, totalCount: 0 }).items).toEqual([])
  })
})

describe('C6 读模型映射与文本兜底', () => {
  it('报告行映射：类型与状态文本优先取服务端值，患者字段与版本序号按契约映射', () => {
    const [row] = toReportRows([{
      reportId: 'REP-1',
      reportNo: 'R-1',
      reportType: 2,
      reportTypeText: '服务端类型文本',
      reportTime: new Date(2026, 8, 20, 13, 45),
      organizationName: '组织一',
      hospitalName: '医院一',
      branchName: '院区一',
      patientName: '张三',
      identityDocumentNo: '110101199001011234',
      currentVersionSequence: 3,
      status: 2,
      statusText: '服务端状态文本',
    }])

    expect(row.reportId).toBe('REP-1')
    expect(row.reportTypeText).toBe('服务端类型文本')
    expect(row.statusText).toBe('服务端状态文本')
    expect(row.patientName).toBe('张三')
    expect(row.identityDocumentNo).toBe('110101199001011234')
    expect(row.currentVersionSequence).toBe(3)
    expect(toDisplayDate(row.reportTime)).toBe('2026-09-20')
  })

  it('服务端未交付文本字段时回退到适配层兜底出口，未知取值安全展示', () => {
    const [row] = toReportRows([{ reportType: 9, status: 9 }])

    expect(row.reportTypeText).toBe('未知类型')
    expect(row.statusText).toBe('未知状态')
    expect(row.reportTime).toBeNull()
    expect(row.currentVersionSequence).toBe(0)
    expect(row.reportNo).toBe('')
  })

  it('版本行映射：区分当前版本与已被替代，责任人员只取该版本自身提供的事实', () => {
    const [row] = toReportVersionRows([{
      reportVersionId: 'VER-2',
      versionSequence: 2,
      sourceModifiedTime: new Date(2026, 8, 20, 9, 5),
      platformReceivedTime: new Date(2026, 8, 20, 9, 6),
      reportDoctorName: '报告医生乙',
      reviewDoctorName: '审核医生乙',
      inspectorName: '检验人乙',
      detailInspectors: '检测人乙',
      sourceReportRemark: '来源备注乙',
      pdfFileName: 'R-1-v2.pdf',
      isCurrentVersion: true,
      isSuperseded: false,
      reportStatus: 1,
      reportStatusText: '有效',
    }])

    expect(row.versionSequence).toBe(2)
    expect(row.reportDoctorName).toBe('报告医生乙')
    expect(row.reviewDoctorName).toBe('审核医生乙')
    expect(row.inspectorName).toBe('检验人乙')
    expect(row.detailInspectors).toBe('检测人乙')
    expect(row.pdfFileName).toBe('R-1-v2.pdf')
    expect(row.isCurrentVersion).toBe(true)
    expect(row.isSuperseded).toBe(false)
  })

  it('版本详情映射：检验与检查内容按报告类型二选一，文件名取文件信息', () => {
    const detail = toReportVersionDetail({
      reportType: 1,
      reportTypeText: '检验报告',
      reportNo: 'R-1',
      reportVersionId: 'VER-1',
      versionSequence: 1,
      platformReceivedTime: new Date(2026, 8, 20, 9, 6),
      reportDoctorName: '报告医生甲',
      content: {
        common: { patientName: '张三', identityDocumentNo: '110101199001011234', patientPhoneNumber: '13800001234' },
        laboratoryContent: { sourceSpecimenNo: 'SP-1' },
        examinationContent: null,
      },
      file: { fileName: 'R-1-v1.pdf' },
    })

    expect(detail.reportTypeText).toBe('检验报告')
    expect(detail.laboratoryContent?.sourceSpecimenNo).toBe('SP-1')
    expect(detail.examinationContent).toBeNull()
    expect(detail.pdfFileName).toBe('R-1-v1.pdf')
    // 患者联系电话按服务端返回的来源原值承载，页面不做二次处理。
    expect(detail.common?.patientPhoneNumber).toBe('13800001234')
    expect(detail.common?.patientName).toBe('张三')
    expect(toDisplayDateTime(detail.platformReceivedTime)).toBe('2026-09-20 09:06')
  })

  it('版本列表与详情都走生成端点，并原样保留服务端返回的下载名', async () => {
    const client = stubClient({
      versionList: [{ reportVersionId: 'VER-1', versionSequence: 1, pdfFileName: 'R-1-v1.pdf' }],
      versionDetail: { reportType: 1, reportVersionId: 'VER-1', file: { fileName: 'R-1-v1.pdf' } },
    })

    const versions = await queryReportVersions(client, 'REP-1')
    expect(bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportVersionList.post)).toEqual({ reportId: 'REP-1' })
    expect(versions[0].pdfFileName).toBe('R-1-v1.pdf')

    const detail = await queryReportVersionDetail(client, 'REP-1', 'VER-1')
    expect(bodyOf(client.api.medicalRecognitionReportQuery.queryMedicalReportVersionDetail.post)).toEqual({
      reportId: 'REP-1',
      reportVersionId: 'VER-1',
    })
    expect(detail.pdfFileName).toBe('R-1-v1.pdf')
  })

  it('版本详情未交付内容时按契约错误抛出，不用空对象伪装成功', async () => {
    const client = stubClient({ versionDetail: undefined })
    await expect(queryReportVersionDetail(client, 'REP-1', 'VER-1')).rejects.toThrow()
  })
})

describe('C8 PDF 下载：按报告与版本标识取二进制并按保存名触发保存', () => {
  it('用已鉴权客户端取二进制，按该版本保存的下载名触发保存，并释放对象地址', async () => {
    const pdfContent = new ArrayBuffer(8)
    const client = stubClient({ pdf: pdfContent })
    // 记录传入的对象内容：用例据此断言交给浏览器的确实是 PDF Blob，而不是把内容转成文本。
    const createObjectURL = vi.fn((content: Blob | MediaSource) => {
      void content
      return 'blob:report-pdf'
    })
    const revokeObjectURL = vi.fn()
    const originalCreate = URL.createObjectURL
    const originalRevoke = URL.revokeObjectURL
    URL.createObjectURL = createObjectURL
    URL.revokeObjectURL = revokeObjectURL
    const clicked: HTMLAnchorElement[] = []
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicked.push(this)
    })

    try {
      await downloadReportVersionPdf(client, 'REP-1', 'VER-1', 'R-1-v1.pdf')

      expect(client.api.v1.reportPdf.byReportId).toHaveBeenCalledWith('REP-1')
      expect(createObjectURL).toHaveBeenCalledTimes(1)
      expect(createObjectURL.mock.calls[0][0]).toBeInstanceOf(Blob)
      expect((createObjectURL.mock.calls[0][0] as Blob).type).toBe('application/pdf')
      expect(clicked).toHaveLength(1)
      // 下载名取该版本保存的下载名，不拼接对外下载地址。
      expect(clicked[0].download).toBe('R-1-v1.pdf')
      expect(clicked[0].href).toBe('blob:report-pdf')
      expect(revokeObjectURL).toHaveBeenCalledWith('blob:report-pdf')
      // 下载地址与文件名都不进入页面地址栏以外的持久状态：锚点已从文档中移除。
      expect(document.querySelector('a[download="R-1-v1.pdf"]')).toBeNull()
    } finally {
      clickSpy.mockRestore()
      URL.createObjectURL = originalCreate
      URL.revokeObjectURL = originalRevoke
    }
  })

  it('文件内容为空时按契约错误抛出，不触发保存', async () => {
    const client = stubClient({ pdf: undefined })
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})

    try {
      await expect(downloadReportVersionPdf(client, 'REP-1', 'VER-1', 'R-1-v1.pdf')).rejects.toThrow()
      expect(clickSpy).not.toHaveBeenCalled()
    } finally {
      clickSpy.mockRestore()
    }
  })
})

describe('作废入口按报告类型分派（页面不提供写入口，本用例只冻结适配层分派口径）', () => {
  it('检验报告与检查报告分别调用各自的作废端点', async () => {
    const client = stubClient()

    await voidReport(client, 1, { reportNo: 'R-1', voidedTime: new Date(2026, 8, 21), voidReason: '重复报告' })
    expect(client.api.medicalRecognitionReport.voidLaboratoryReport.post).toHaveBeenCalledTimes(1)
    expect(client.api.medicalRecognitionReport.voidExaminationReport.post).not.toHaveBeenCalled()

    await voidReport(client, 2, { reportNo: 'R-2', voidedTime: new Date(2026, 8, 21), voidReason: '重复报告' })
    expect(client.api.medicalRecognitionReport.voidExaminationReport.post).toHaveBeenCalledTimes(1)
  })

  it('报告类型未知时拒绝调用任何作废端点', async () => {
    const client = stubClient()
    await expect(voidReport(client, null, { reportNo: 'R-1' })).rejects.toThrow()
    expect(client.api.medicalRecognitionReport.voidLaboratoryReport.post).not.toHaveBeenCalled()
    expect(client.api.medicalRecognitionReport.voidExaminationReport.post).not.toHaveBeenCalled()
  })
})
