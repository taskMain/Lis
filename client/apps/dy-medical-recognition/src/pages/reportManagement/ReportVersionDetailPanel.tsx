/**
 * 报告详情与只读历史版本区域（阶段 4 票 14、15 共用）。
 *
 * 该区域只读：展示报告公共信息与该报告的全部版本，按版本序号升序呈现，
 * 并明确标识当前有效版本、已被后续版本替代的历史版本与报告已作废状态。历史版本不提供
 * 差异比较、修改、删除、恢复、重新设为当前版本或重新启用，也不展示更正原因与版本变更原因。
 *
 * 失败面三类落在本区域：版本列表读取失败、版本详情读取失败与 PDF 下载失败；
 * 三者都保留已有数据与当前版本选择、结束加载态、提供重试，且不显示本地接口错误提示。
 */
import { DownloadOutlined } from '@ant-design/icons'
import { Alert, Button, Descriptions, Empty, Space, Spin, Table, Tag, Typography, type DescriptionsProps, type TableColumnsType } from 'antd'
import {
  toDisplayDate,
  toDisplayDateTime,
  EXAMINATION_REPORT_TYPE,
  LABORATORY_REPORT_TYPE,
  type ReportVersionDetail,
  type ReportVersionRow,
} from './reportsApi'
// 明细读模型类型从生成包公开入口取：适配层已定稿且不转出这些嵌套类型，页面按公开入口引用它们，
// 不依赖生成目录的深层路径，也不在本文件重复声明一份结构。
import type {
  ExaminationSiteReadModel,
  LaboratoryAntimicrobialSusceptibilityReadModel,
  LaboratoryBacteriaResultReadModel,
  LaboratoryResultItemReadModel,
} from '@dy/api-client-medical-recognition'

/** 缺失值统一展示为短横线，使「来源未提供」与「空串」在页面上是同一事实。 */
const MISSING_TEXT = '—'

/** 文本展示出口：缺失或空白一律展示占位符。 */
function text(value: string | null | undefined): string {
  const normalized = value?.trim() ?? ''
  return normalized.length > 0 ? normalized : MISSING_TEXT
}

/** 日期时间展示出口；无业务值时展示占位符。 */
function dateTimeText(value: Date | null | undefined): string {
  return toDisplayDateTime(value) || MISSING_TEXT
}

/** 布尔标志展示出口；来源未提供时展示占位符。 */
function flagText(value: boolean | null | undefined): string {
  if (value === true) return '是'
  if (value === false) return '否'
  return MISSING_TEXT
}

/** 把取值列表拼成描述项；空列表展示占位符。 */
function toDescriptionItems(values: readonly (readonly [string, string])[]): DescriptionsProps['items'] {
  return values.map(([label, value]) => ({ key: label, label, children: value }))
}

export interface ReportVersionDetailPanelProps {
  reportNo: string
  reportTypeText: string
  reportStatusText: string
  versions: ReportVersionRow[]
  versionsLoading: boolean
  versionFailure: null | 'load'
  selectedVersionId: string | null
  detail: ReportVersionDetail | null
  detailLoading: boolean
  detailFailure: null | 'load'
  downloadingVersionId: string | null
  downloadFailed: boolean
  onSelectVersion: (reportVersionId: string) => void
  onRetryVersions: () => void
  onRetryDetail: () => void
  onDownload: (row: ReportVersionRow) => void
  onClose: () => void
}

/** 版本行的状态标识：当前有效版本、已被后续版本替代、报告已作废三种事实各自成标签。 */
function versionTags(row: ReportVersionRow) {
  return <Space size={4} wrap>
    <Tag color={row.isCurrentVersion ? 'green' : 'default'}>{row.isCurrentVersion ? '当前有效版本' : '历史版本'}</Tag>
    {row.isSuperseded ? <Tag color='blue'>已被后续版本替代</Tag> : null}
    <Tag color={row.reportStatusText === '已作废' ? 'red' : 'default'}>{`报告${row.reportStatusText}`}</Tag>
  </Space>
}

export function ReportVersionDetailPanel({
  reportNo,
  reportTypeText,
  reportStatusText,
  versions,
  versionsLoading,
  versionFailure,
  selectedVersionId,
  detail,
  detailLoading,
  detailFailure,
  downloadingVersionId,
  downloadFailed,
  onSelectVersion,
  onRetryVersions,
  onRetryDetail,
  onDownload,
  onClose,
}: ReportVersionDetailPanelProps) {
  const versionColumns: TableColumnsType<ReportVersionRow> = [
    { title: '版本序号', dataIndex: 'versionSequence', width: 90 },
    { title: '状态', width: 300, render: (_: unknown, row) => versionTags(row) },
    { title: '源端报告修改时间', width: 170, render: (_: unknown, row) => dateTimeText(row.sourceModifiedTime) },
    { title: '平台接收时间', width: 170, render: (_: unknown, row) => dateTimeText(row.platformReceivedTime) },
    { title: '报告医生', width: 110, render: (_: unknown, row) => text(row.reportDoctorName) },
    { title: '审核医生', width: 110, render: (_: unknown, row) => text(row.reviewDoctorName) },
    { title: '来源报告备注', width: 200, ellipsis: true, render: (_: unknown, row) => text(row.sourceReportRemark) },
    { title: 'PDF 文件名', width: 200, ellipsis: true, render: (_: unknown, row) => text(row.pdfFileName) },
    {
      title: '操作',
      width: 160,
      fixed: 'right',
      render: (_: unknown, row) => <Space size={4}>
        <Button
          type='link'
          size='small'
          aria-label={`查看版本内容：第 ${row.versionSequence} 版`}
          onClick={() => onSelectVersion(row.reportVersionId)}
        >查看该版本</Button>
        <Button
          type='link'
          size='small'
          icon={<DownloadOutlined />}
          aria-label={`下载第 ${row.versionSequence} 版 PDF`}
          loading={downloadingVersionId === row.reportVersionId}
          disabled={downloadingVersionId !== null && downloadingVersionId !== row.reportVersionId}
          onClick={() => onDownload(row)}
        >下载</Button>
      </Space>,
    },
  ]

  return <div className='report-management-detail'>
    <div className='report-management-detail-header'>
      <div>
        <Typography.Title level={4}>报告详情与历史版本</Typography.Title>
        <Typography.Text type='secondary'>
          报告单号 {text(reportNo)}；报告类型 {text(reportTypeText)}；报告状态 {text(reportStatusText)}
        </Typography.Text>
      </div>
      <Button size='small' onClick={onClose}>关闭详情</Button>
    </div>

    {versionFailure === null ? null : <Alert
      type='warning'
      showIcon
      className='report-management-failure'
      title='报告历史版本待刷新'
      description='版本列表读取失败，已有数据保留；可重试读取。'
      action={<Button size='small' disabled={versionsLoading} onClick={onRetryVersions}>重试</Button>}
    />}

    <Table
      rowKey='reportVersionId'
      size='small'
      columns={versionColumns}
      dataSource={versions}
      loading={versionsLoading}
      scroll={{ x: 1300 }}
      pagination={false}
      locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该报告没有历史版本' /> }}
    />

    <div className='report-management-version-body'>
      <Typography.Title level={5}>
        版本内容{selectedVersionId === null ? '' : `（第 ${detail?.versionSequence ?? MISSING_TEXT} 版）`}
      </Typography.Title>

      {detailFailure === null ? null : <Alert
        type='warning'
        showIcon
        className='report-management-failure'
        title='报告版本内容待刷新'
        description='该版本内容读取失败，当前版本选择与已有内容保留；可重试读取。'
        action={<Button size='small' disabled={detailLoading} onClick={onRetryDetail}>重试</Button>}
      />}

      {downloadFailed ? <Alert
        type='warning'
        showIcon
        className='report-management-failure'
        title='报告 PDF 下载未完成'
        description='本次下载未成功，当前详情与版本选择保留；可重新下载。'
      /> : null}

      {detailLoading ? <Spin /> : detail === null ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='请选择要查看的版本' /> : <>
        <Typography.Text type='secondary'>
          平台接收时间 {dateTimeText(detail.platformReceivedTime)}；源端报告修改时间 {dateTimeText(detail.sourceModifiedTime)}；
          报告医生 {text(detail.reportDoctorName)}；审核医生 {text(detail.reviewDoctorName)}；
          PDF 文件名 {text(detail.pdfFileName)}
        </Typography.Text>

        <Descriptions
          className='report-management-descriptions'
          size='small'
          column={3}
          bordered
          title='报告公共信息'
          items={toDescriptionItems([
            ['患者姓名', text(detail.common?.patientName)],
            ['患者性别代码', text(detail.common?.patientGenderCode)],
            ['患者出生日期', toDisplayDate(detail.common?.patientBirthDate ?? null) || MISSING_TEXT],
            ['患者联系电话', text(detail.common?.patientPhoneNumber)],
            ['报告时年龄', text(detail.common?.ageAtReport)],
            ['证件类型代码', text(detail.common?.identityDocumentTypeCode)],
            ['证件号码', text(detail.common?.identityDocumentNo)],
            ['就诊类型', text(detail.common?.visitTypeText)],
            ['就诊流水号', text(detail.common?.visitSerialNo)],
            ['来源报告名称', text(detail.common?.sourceReportName)],
            ['申请科室', `${text(detail.common?.applicationDeptName)}（${text(detail.common?.applicationDeptId)}）`],
            ['申请医生', `${text(detail.common?.applicationDoctorName)}（${text(detail.common?.applicationDoctorId)}）`],
            ['执行科室', `${text(detail.common?.executionDeptName)}（${text(detail.common?.executionDeptId)}）`],
            ['报告科室', `${text(detail.common?.reportDeptName)}（${text(detail.common?.reportDeptId)}）`],
            ['报告医生', `${text(detail.common?.reportDoctorName)}（${text(detail.common?.reportDoctorId)}）`],
            ['审核医生', `${text(detail.common?.reviewDoctorName)}（${text(detail.common?.reviewDoctorId)}）`],
            ['审核时间', dateTimeText(detail.common?.reviewTime)],
            ['住院号', text(detail.common?.inpatientNo)],
            ['病区名称', text(detail.common?.wardName)],
            ['病房名称', text(detail.common?.roomName)],
            ['床位号', text(detail.common?.bedNo)],
            ['申请时间', dateTimeText(detail.common?.applicationTime)],
            ['报告时间', dateTimeText(detail.common?.reportTime)],
            ['来源保密标识', text(detail.common?.sourceConfidentialFlag)],
          ])}
        />

        {detail.reportType === LABORATORY_REPORT_TYPE ? <LaboratoryContentView detail={detail} /> : null}
        {detail.reportType === EXAMINATION_REPORT_TYPE ? <ExaminationContentView detail={detail} /> : null}
        {detail.reportType !== LABORATORY_REPORT_TYPE && detail.reportType !== EXAMINATION_REPORT_TYPE
          ? <Alert type='info' showIcon title='报告类型未知' description='服务端未交付可识别的报告类型，无法展示该版本的结构化内容。' />
          : null}
      </>}
    </div>
  </div>
}

/** 检验报告结构化内容：标本信息、检验时间线、普通结果与细菌鉴定结果（含药敏结果）。 */
function LaboratoryContentView({ detail }: { detail: ReportVersionDetail }) {
  const content = detail.laboratoryContent
  if (content === null) return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检验结构化内容' />

  const resultColumns: TableColumnsType<LaboratoryResultItemReadModel> = [
    { title: '展示序号', dataIndex: 'displayOrder', width: 90 },
    { title: '来源项目名称', width: 160, render: (_: unknown, row) => text(row.sourceProjectName) },
    { title: '来源项目编码', width: 130, render: (_: unknown, row) => text(row.sourceProjectCode) },
    { title: '互认项目编码', width: 130, render: (_: unknown, row) => text(row.standardProjectCode) },
    { title: '来源结果原文', width: 140, render: (_: unknown, row) => text(row.sourceResultText) },
    { title: '结果类型', width: 100, render: (_: unknown, row) => text(row.resultTypeText) },
    { title: '单位', width: 90, render: (_: unknown, row) => text(row.unit) },
    { title: '参考范围', width: 130, render: (_: unknown, row) => text(row.referenceRange) },
    { title: '异常标志', width: 100, render: (_: unknown, row) => text(row.abnormalFlagText) },
    { title: '危急值标志', width: 100, render: (_: unknown, row) => flagText(row.criticalValueFlag) },
    { title: '检测方法', width: 120, render: (_: unknown, row) => text(row.testingMethod) },
    { title: '检测仪器', width: 140, render: (_: unknown, row) => text(row.instrumentName) },
    { title: '检测人', width: 100, render: (_: unknown, row) => text(row.inspectorName) },
  ]

  const susceptibilityColumns: TableColumnsType<LaboratoryAntimicrobialSusceptibilityReadModel> = [
    { title: '展示序号', dataIndex: 'displayOrder', width: 90 },
    { title: '受试药物名称', width: 140, render: (_: unknown, row) => text(row.drugName) },
    { title: '来源结论', width: 120, render: (_: unknown, row) => text(row.sourceConclusionText) },
    { title: '纸片含药量', width: 110, render: (_: unknown, row) => text(row.diskContent) },
    { title: 'MIC', width: 100, render: (_: unknown, row) => text(row.micValue) },
    { title: '抑菌环直径', width: 110, render: (_: unknown, row) => text(row.inhibitionZoneDiameter) },
    { title: '参考值', width: 100, render: (_: unknown, row) => text(row.referenceValue) },
    { title: '检测方法', width: 120, render: (_: unknown, row) => text(row.testingMethod) },
    { title: '试验板序号', width: 110, render: (_: unknown, row) => text(row.testPanelOrder) },
    { title: '检测人', width: 100, render: (_: unknown, row) => text(row.inspectorName) },
  ]

  const bacteriaColumns: TableColumnsType<LaboratoryBacteriaResultReadModel> = [
    { title: '检测结论', width: 140, render: (_: unknown, row) => text(row.detectionConclusion) },
    { title: '来源结果原文', width: 160, render: (_: unknown, row) => text(row.sourceResultText) },
    { title: '来源菌种编码', width: 130, render: (_: unknown, row) => text(row.sourceOrganismCode) },
    { title: '来源菌种名称', width: 130, render: (_: unknown, row) => text(row.sourceOrganismName) },
    { title: '菌落计数', width: 110, render: (_: unknown, row) => text(row.colonyCount) },
    { title: '培养基', width: 110, render: (_: unknown, row) => text(row.cultureMedium) },
    { title: '培养时间', width: 110, render: (_: unknown, row) => text(row.cultureTime) },
    { title: '培养条件', width: 110, render: (_: unknown, row) => text(row.cultureCondition) },
    { title: '检测方法', width: 120, render: (_: unknown, row) => text(row.detectionMethod) },
    { title: '仪器', width: 130, render: (_: unknown, row) => text(row.instrumentName) },
    { title: '试验板', width: 130, render: (_: unknown, row) => text(row.testPanelName) },
    { title: '检测人', width: 100, render: (_: unknown, row) => text(row.inspectorName) },
    {
      title: '药敏结果',
      width: 110,
      render: (_: unknown, row) => `${row.susceptibilities?.length ?? 0} 条`,
    },
  ]

  return <div className='report-management-content'>
    <Descriptions
      className='report-management-descriptions'
      size='small'
      column={3}
      bordered
      title='标本信息与检验时间线'
      items={toDescriptionItems([
        ['院内标本号', text(content.sourceSpecimenNo)],
        ['标本类型', `${text(content.specimenTypeName)}（${text(content.specimenTypeCode)}）`],
        ['检测完成时间', dateTimeText(content.testingCompletedTime)],
        ['标本采集时间', dateTimeText(content.specimenCollectedTime)],
        ['标本送检时间', dateTimeText(content.specimenSubmittedTime)],
        ['检验科接收时间', dateTimeText(content.laboratoryReceivedTime)],
        ['报告类别', `${text(content.reportCategoryName)}（${text(content.reportCategoryCode)}）`],
        ['报告备注', text(content.reportRemark)],
        ['整体异常标识', text(content.overallAbnormalFlag)],
        ['来源医嘱流水号', text(content.sourceOrderSerialNo)],
        ['检验人', `${text(content.inspectorName)}（${text(content.inspectorId)}）`],
      ])}
    />

    <Typography.Title level={5}>普通检验结果</Typography.Title>
    <Table
      rowKey={(row) => `result-${row.displayOrder ?? 0}-${row.sourceDetailKey ?? ''}`}
      size='small'
      columns={resultColumns}
      dataSource={content.results ?? []}
      scroll={{ x: 1600 }}
      pagination={false}
      locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有普通检验结果' /> }}
    />

    <Typography.Title level={5}>细菌鉴定结果与药敏结果</Typography.Title>
    {(content.bacteriaResults ?? []).length === 0
      ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有细菌鉴定结果' />
      : (content.bacteriaResults ?? []).map((bacteria, index) => <div className='report-management-bacteria' key={`bacteria-${index}-${bacteria.sourceDetailKey ?? ''}`}>
        <Table
          rowKey={() => `bacteria-${index}`}
          size='small'
          columns={bacteriaColumns}
          dataSource={[bacteria]}
          scroll={{ x: 1600 }}
          pagination={false}
        />
        <Table
          rowKey={(row) => `susceptibility-${row.displayOrder ?? 0}-${row.drugCode ?? ''}`}
          size='small'
          columns={susceptibilityColumns}
          dataSource={bacteria.susceptibilities ?? []}
          scroll={{ x: 1200 }}
          pagination={false}
          locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该细菌没有药敏结果' /> }}
        />
      </div>)}
  </div>
}

/** 检查报告结构化内容：检查所见与结论、临床背景、检查信息、影像状态与调阅地址、检查项目与部位。 */
function ExaminationContentView({ detail }: { detail: ReportVersionDetail }) {
  const content = detail.examinationContent
  if (content === null) return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检查结构化内容' />

  const siteColumns: TableColumnsType<ExaminationSiteReadModel> = [
    { title: '部位名称', width: 160, render: (_: unknown, row) => text(row.siteName) },
    { title: '来源部位编码', width: 140, render: (_: unknown, row) => text(row.sourceSiteCode) },
  ]

  return <div className='report-management-content'>
    <Descriptions
      className='report-management-descriptions'
      size='small'
      column={2}
      bordered
      title='检查所见与结论'
      items={toDescriptionItems([
        ['检查所见', text(content.findings)],
        ['检查结论', text(content.conclusion)],
        ['来源诊断', `${text(content.sourceDiagnosisName)}（${text(content.sourceDiagnosisCode)}）`],
        ['报告备注', text(content.reportRemark)],
        ['整体异常标识', text(content.overallAbnormalFlag)],
      ])}
    />

    <Descriptions
      className='report-management-descriptions'
      size='small'
      column={2}
      bordered
      title='临床背景'
      items={toDescriptionItems([
        ['病情描述', text(content.conditionDescription)],
        ['检查目的', text(content.examinationPurpose)],
      ])}
    />

    <Descriptions
      className='report-management-descriptions'
      size='small'
      column={2}
      bordered
      title='检查信息与影像状态'
      items={toDescriptionItems([
        ['来源检查类型', `${text(content.sourceExaminationTypeName)}（${text(content.sourceExaminationTypeCode)}）`],
        ['实际检查时间', dateTimeText(content.examinationTime)],
        ['检查医生', `${text(content.examinerName)}（${text(content.examinerId)}）`],
        ['检查方法', text(content.examinationMethod)],
        ['设备', `${text(content.deviceName)}（${text(content.deviceCode)}）`],
        ['来源影像状态', text(content.sourceImageStatusText)],
        ['影像调阅地址', text(content.imageAccessUrl)],
      ])}
    />

    <Typography.Title level={5}>检查项目与检查部位</Typography.Title>
    {(content.items ?? []).length === 0
      ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检查项目' />
      : (content.items ?? []).map((item, index) => <div className='report-management-examination-item' key={`item-${index}-${item.sourceProjectCode ?? ''}`}>
        <Descriptions
          className='report-management-descriptions'
          size='small'
          column={3}
          bordered
          items={toDescriptionItems([
            ['来源项目名称', text(item.sourceProjectName)],
            ['来源项目编码', text(item.sourceProjectCode)],
            ['互认项目编码', text(item.standardProjectCode)],
          ])}
        />
        <Table
          rowKey={(row) => `site-${row.siteName ?? ''}-${row.sourceSiteCode ?? ''}`}
          size='small'
          columns={siteColumns}
          dataSource={item.sites ?? []}
          pagination={false}
          locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该项目没有明确检查部位' /> }}
        />
      </div>)}
  </div>
}
