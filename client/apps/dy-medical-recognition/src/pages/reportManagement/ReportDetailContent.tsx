/**
 * 「报告详情」共享内容组件：概要头部、版本切换控件、版本信息、公共信息、检验内容、检查内容与历史版本各区域的呈现规则集中在本文件。
 *
 * 两个入口的详情路由页共用的纯展示组件；本文件当前承载概要头部（`SummaryHeader`）、
 * 公共信息四组（`PublicInfoGroups`）、版本切换控件（`VersionSegmented`）、版本元信息条
 * （`VersionMetaBar`）、检验内容卡（`LaboratoryContent`）、检查内容卡（`ExaminationContent`）
 * 与历史版本时间线（`VersionTimeline`）。
 *
 * 组件只经 props 接收视图模型与回调，不发业务请求；`PublicInfoGroups` 的 `common`、
 * `LaboratoryContent` 的 `content` 与 `ExaminationContent` 的 `content` 为适配层 `reportsApi`
 * 原样交付的读模型出口。颜色一律引用 antd token，不使用脱离主题的硬编码色。
 */
import { DownloadOutlined, FileImageOutlined, ThunderboltFilled } from '@ant-design/icons'
import { Button, Card, Collapse, Descriptions, Empty, Segmented, Select, Table, Tag, Timeline, Tooltip, Typography, type DescriptionsProps, type TableProps } from 'antd'
import { useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import { MISSING_TEXT, missingText, treatedAsNone } from './reportDetailShared'
import { IdNumberText } from './IdNumberText'
import {
  toDisplayDate,
  toDisplayDateTime,
  VOIDED_REPORT_STATUS,
  type ExaminationReportContentViewReadModel,
  type LaboratoryReportContentViewReadModel,
  type ReportRow,
  type ReportVersionCommonReadModel,
  type ReportVersionRow,
} from './reportsApi'

/** 版本状态的短文案（Select 选项文本）：作废是报告级事实，优先于当前有效表述。 */
type VersionState = '当前有效' | '已被替代' | '已作废'

function versionState(version: ReportVersionRow): VersionState {
  if (version.reportStatus === VOIDED_REPORT_STATUS) return '已作废'
  if (version.isCurrentVersion) return '当前有效'
  return '已被替代'
}

/** 元信息条里版本状态 Tag 的完整文案与配色：当前有效用 success，其余中性灰（红色保留给异常与危急值）。 */
const VERSION_STATE_TAG: Record<VersionState, { text: string; color: 'success' | 'default' }> = {
  当前有效: { text: '当前有效版本', color: 'success' },
  已被替代: { text: '已被后续版本替代', color: 'default' },
  已作废: { text: '报告已作废', color: 'default' },
}

/**
 * PDF 文件名中段截断：保留文件名头部与扩展名、中段以「…」占位；
 * 全长不超限时原样展示。是否挂 Tooltip 呈现完整名由 `PdfFileName` 按是否发生截断决定。
 */
const PDF_NAME_MAX_CHARS = 24
const PDF_NAME_HEAD_CHARS = 8
/** 扩展名的长度预算：总长上限减头部与中段「…」；超限截尾保证输出恒短于原文且不超过上限。 */
const PDF_NAME_EXTENSION_BUDGET = PDF_NAME_MAX_CHARS - PDF_NAME_HEAD_CHARS - 1

function middleTruncate(fileName: string): string {
  if (fileName.length <= PDF_NAME_MAX_CHARS) return fileName
  const dotIndex = fileName.lastIndexOf('.')
  let extension = dotIndex > 0 ? fileName.slice(dotIndex) : ''
  // 点号靠前且扩展名过长时，「头部 + … + 全扩展名」会反而比原文更长：对扩展名按末段截尾。
  if (extension.length > PDF_NAME_EXTENSION_BUDGET) extension = extension.slice(-PDF_NAME_EXTENSION_BUDGET)
  return `${fileName.slice(0, PDF_NAME_HEAD_CHARS)}…${extension}`
}

/**
 * PDF 文件名：中段截断呈现，仅发生截断时包 Tooltip 呈现完整名——
 * 未截断没有隐藏信息，不挂浮层；缺失按占位符呈现，同样不挂浮层。
 */
function PdfFileName({ fileName }: { fileName: string }) {
  const full = missingText(fileName)
  const display = middleTruncate(full)
  if (display === full) return <span className='report-management-detail-pdf-name'>{display}</span>
  return <Tooltip title={full}>
    <span className='report-management-detail-pdf-name'>{display}</span>
  </Tooltip>
}

/** 概要头部的单个字段：label 在上、值在下；值缺失显示「—」。 */
function SummaryField(props: { label: string; value: ReactNode; secondary?: string }) {
  return <div className='report-management-detail-summary-item'>
    <div className='report-management-detail-summary-label'>{props.label}</div>
    <div className='report-management-detail-summary-value'>
      {props.value}
      {props.secondary === undefined ? null : (
        <span className='report-management-detail-summary-secondary'>{props.secondary}</span>
      )}
    </div>
  </div>
}

/** 提供方主行文本：组织名与医院名并列；两者都缺失时展示占位符。 */
function sourceMainText(row: ReportRow): string {
  const organization = row.organizationName.trim()
  const hospital = row.hospitalName.trim()
  if (organization === '' && hospital === '') return MISSING_TEXT
  return [organization, hospital].filter((part) => part !== '').join(' ')
}

/** 提供方次行文本：院区名；缺失时展示占位符。 */
function sourceBranchText(row: ReportRow): string {
  const branch = row.branchName.trim()
  return branch === '' ? MISSING_TEXT : branch
}

export interface SummaryHeaderProps {
  /** 经会话级缓存交付的来源行概要；深链直开没有该数据，缺失字段一律按「—」呈现。 */
  row: ReportRow | undefined
}

/**
 * 概要标题行的类型与状态 Tag：类型 processing；状态有效 success、
 * 已作废中性灰（红色保留给异常与危急值）。详情顶栏与概要卡标题行共用同一呈现。
 */
export function SummaryStateTags({ row }: { row: ReportRow }) {
  return <>
    <Tag color='processing'>{row.reportTypeText}</Tag>
    <Tag color={row.status === VOIDED_REPORT_STATUS ? 'default' : 'success'}>{row.statusText}</Tag>
  </>
}

/**
 * 概要头部两行：标题行为报告单号（copyable、`tabular-nums`）与
 * 报告类型/状态 Tag（类型 processing、有效 success、已作废中性灰——红色保留给异常与危急值）；
 * 字段行为患者姓名、证件号码、报告时间（YYYY-MM-DD）、提供方（组织与医院主行加院区次行）、
 * 当前版本（「V3」形式）五项。
 */
export function SummaryHeader({ row }: SummaryHeaderProps) {
  return <div className='report-management-detail-summary-header'>
    <div className='report-management-detail-summary-title'>
      <Typography.Text copyable className='report-management-detail-summary-no'>
        {missingText(row?.reportNo)}
      </Typography.Text>
      {row === undefined ? null : <SummaryStateTags row={row} />}
    </div>
    <div className='report-management-detail-summary'>
      <SummaryField label='患者姓名' value={missingText(row?.patientName)} />
      <SummaryField label='证件号码' value={<IdNumberText value={row?.identityDocumentNo} />} />
      <SummaryField label='报告时间' value={row === undefined ? MISSING_TEXT : missingText(toDisplayDate(row.reportTime))} />
      <SummaryField
        label='提供方'
        value={row === undefined ? MISSING_TEXT : sourceMainText(row)}
        secondary={row === undefined ? undefined : sourceBranchText(row)}
      />
      <SummaryField label='当前版本' value={row === undefined ? MISSING_TEXT : `V${row.currentVersionSequence}`} />
    </div>
  </div>
}

/** 公共信息单项类型（antd Descriptions items 的元素形态）。 */
type PublicInfoItem = NonNullable<DescriptionsProps['items']>[number]

/** 公共信息的文本字段单项：缺失值归一为「—」。 */
function textItem(label: string, value: string | null | undefined): PublicInfoItem {
  return { key: label, label, children: missingText(value) }
}

/** 「名称（ID）」对的降级呈现：名称主行 + ID 次行小字；名称缺失沿用「—」，ID 缺失不占次行。 */
function NameWithId(props: { name: string | null | undefined; id: string | null | undefined }) {
  const id = props.id?.trim() ?? ''
  return <span className='report-management-detail-name-id'>
    <span className='report-management-detail-name-id-name'>{missingText(props.name)}</span>
    {id.length === 0 ? null : <span className='report-management-detail-name-id-code'>{id}</span>}
  </span>
}

/** 公共信息的「名称（ID）」对单项：科室与医生六对统一按名称主行加标识次行小字降级。 */
function itemWithId(label: string, name: string | null | undefined, id: string | null | undefined): PublicInfoItem {
  return { key: label, label, children: <NameWithId name={name} id={id} /> }
}

/**
 * 来源保密标识：原文非空且非「否」时渲染 warning「保密」Tag 加原文，
 * 「否」与缺失按常规文本（缺失沿用「—」）。
 */
function ConfidentialText({ flag }: { flag: string | null | undefined }) {
  if (treatedAsNone(flag)) return <>{missingText(flag?.trim())}</>
  return <span className='report-management-detail-public-confidential'>
    <Tag color='warning'>保密</Tag>
    {flag?.trim()}
  </span>
}

/** 公共信息子组：组名 + 三列无框 Descriptions（组名 13px/600 secondary、行距 8px 由配套 CSS 承载）。 */
function PublicInfoGroup(props: { title: string; items: NonNullable<DescriptionsProps['items']> }) {
  return <section className='report-management-detail-public-group'>
    <h3 className='report-management-detail-public-group-title'>{props.title}</h3>
    <Descriptions size='small' bordered={false} column={3} items={props.items} />
  </section>
}

export interface PublicInfoGroupsProps {
  /** 版本详情交付的公共信息读模型；`null`（该版本未交付公共信息）时四组照常呈现、值全部「—」。 */
  common: ReportVersionCommonReadModel | null
}

/**
 * 公共信息四组：报告信息、患者信息、就诊信息、申请信息，共 24 字段，
 * 组内顺序与设计表格一致。每组为 `Descriptions size="small" bordered={false} column=3`；
 * 容器宽度不足 900px 时每组降两列，列数由配套 CSS 的容器查询承载（不感知视口断点）。
 */
export function PublicInfoGroups({ common }: PublicInfoGroupsProps) {
  return <div className='report-management-detail-public'>
    <PublicInfoGroup title='报告信息' items={[
      itemWithId('报告科室', common?.reportDeptName, common?.reportDeptId),
      itemWithId('报告医生', common?.reportDoctorName, common?.reportDoctorId),
      itemWithId('审核医生', common?.reviewDoctorName, common?.reviewDoctorId),
      textItem('审核时间', toDisplayDateTime(common?.reviewTime)),
      textItem('报告时间', toDisplayDateTime(common?.reportTime)),
      itemWithId('执行科室', common?.executionDeptName, common?.executionDeptId),
      { key: '来源保密标识', label: '来源保密标识', children: <ConfidentialText flag={common?.sourceConfidentialFlag} /> },
    ]} />
    <PublicInfoGroup title='患者信息' items={[
      textItem('患者姓名', common?.patientName),
      textItem('性别代码', common?.patientGenderCode),
      textItem('出生日期', toDisplayDate(common?.patientBirthDate)),
      textItem('报告时年龄', common?.ageAtReport),
      { key: '证件号码', label: '证件号码', children: <IdNumberText value={common?.identityDocumentNo} /> },
      textItem('联系电话', common?.patientPhoneNumber),
    ]} />
    <PublicInfoGroup title='就诊信息' items={[
      textItem('就诊类型', common?.visitTypeText),
      textItem('就诊流水号', common?.visitSerialNo),
      textItem('住院号', common?.inpatientNo),
      textItem('病区名称', common?.wardName),
      textItem('病房名称', common?.roomName),
      textItem('床位号', common?.bedNo),
    ]} />
    <PublicInfoGroup title='申请信息' items={[
      textItem('来源报告名称', common?.sourceReportName),
      itemWithId('申请科室', common?.applicationDeptName, common?.applicationDeptId),
      itemWithId('申请医生', common?.applicationDoctorName, common?.applicationDoctorId),
      textItem('申请时间', toDisplayDateTime(common?.applicationTime)),
    ]} />
  </div>
}

/** 元信息条的单个字段：label 在上、值在下。 */
function MetaField(props: { label: string; children: ReactNode }) {
  return <div className='report-management-detail-meta-item'>
    <div className='report-management-detail-meta-label'>{props.label}</div>
    <div className='report-management-detail-meta-value'>{props.children}</div>
  </div>
}

export interface VersionSegmentedProps {
  /** 全部版本（按版本序号交付）。 */
  versions: ReportVersionRow[]
  /** 当前浏览的版本标识（受控值）。 */
  selectedVersionId: string
  /** 切换版本回调（选中态与读取编排由页面承载）。 */
  onChange: (reportVersionId: string) => void
}

/**
 * 版本切换控件三形态：
 * 2～6 版渲染 `Segmented`（当前有效版本选项带圆点、可访问名称携带版本事实）；
 * 超过 6 版退化为 `Select`（选项文本携带版本事实）；单版本渲染静态 Tag，不渲染控件——
 * Tag 文案按版本状态分发（作废显「已作废」、配色同元信息条的中性灰），不与元信息条的作废事实冲突。
 */
export function VersionSegmented({ versions, selectedVersionId, onChange }: VersionSegmentedProps) {
  if (versions.length === 1) {
    const onlyVersion = versions[0]
    const voided = onlyVersion.reportStatus === VOIDED_REPORT_STATUS
    return <Tag
      color={voided ? 'default' : 'processing'}
      className='report-management-detail-version-tag'
    >{`V${onlyVersion.versionSequence} · ${voided ? '已作废' : '当前版本'}`}</Tag>
  }
  if (versions.length > 6) {
    return <Select
      aria-label='版本切换'
      className='report-management-detail-version-select'
      style={{ width: 160 }}
      value={selectedVersionId}
      options={versions.map((version) => ({
        value: version.reportVersionId,
        label: `V${version.versionSequence} · ${versionState(version)}`,
      }))}
      onChange={(value) => onChange(value)}
    />
  }
  return <Segmented
    aria-label='版本切换'
    className='report-management-detail-version-segmented'
    value={selectedVersionId}
    options={versions.map((version) => ({
      value: version.reportVersionId,
      // rc-segmented 不支持按选项 aria-label：选项的可访问名称由视觉隐藏的版本事实承载，
      // 可见标签标记 aria-hidden，避免读屏重复播报同一事实。
      label: <VersionOptionLabel version={version} />,
    }))}
    onChange={(value) => onChange(String(value))}
  />
}

/** Segmented 选项标签：视觉隐藏的版本事实 + 当前有效版本的圆点标记 + 可见的版本序号。 */
function VersionOptionLabel({ version }: { version: ReportVersionRow }) {
  const fact = version.isCurrentVersion
    ? `第 ${version.versionSequence} 版（当前有效版本）`
    : `第 ${version.versionSequence} 版（历史版本）`
  return <>
    <span className='report-management-detail-visually-hidden'>{fact}</span>
    {version.isCurrentVersion ? <span className='report-management-detail-version-dot' aria-hidden='true' /> : null}
    <span aria-hidden='true'>{`V${version.versionSequence}`}</span>
  </>
}

export interface VersionMetaBarProps {
  /** 呈现中的版本行（元信息的事实来源）。 */
  version: ReportVersionRow
  /** 正在下载的版本标识：与本版本一致时按钮进入在途态，存在其他在途时本按钮禁用（单在途互斥）。 */
  downloadingVersionId: string | null
  /** 下载回调（互斥与失败复位由页面编排）。 */
  onDownload: (version: ReportVersionRow) => void
}

interface VersionDownloadButtonProps {
  /** 本按钮对应的版本行。 */
  version: ReportVersionRow
  /** 正在下载的版本标识：与本版本一致时进入在途态，存在其他在途时禁用（单在途互斥）。 */
  downloadingVersionId: string | null
  /** 下载回调（互斥与失败复位由页面编排）。 */
  onDownload: (version: ReportVersionRow) => void
}

/**
 * 版本 PDF 下载按钮：版本信息卡元信息条与历史版本时间线节点共用——
 * 在途 `loading` 且文案「下载中…」、其余版本在途时禁用（单在途互斥）、`aria-busy` 与中文可访问名称。
 */
function VersionDownloadButton({ version, downloadingVersionId, onDownload }: VersionDownloadButtonProps) {
  const downloading = downloadingVersionId === version.reportVersionId
  const otherDownloading = downloadingVersionId !== null && !downloading
  return <Button
    size='small'
    icon={<DownloadOutlined />}
    aria-label={`下载第 ${version.versionSequence} 版 PDF`}
    aria-busy={downloading ? true : undefined}
    disabled={otherDownloading}
    loading={downloading}
    onClick={() => onDownload(version)}
  >{downloading ? '下载中…' : '下载 PDF'}</Button>
}

/**
 * 版本元信息条：版本状态 Tag、源端修改时间、平台接收时间、报告与审核医生、
 * PDF 文件名（截断加 Tooltip）与下载按钮；来源报告备注独立次行，两行截断加「展开/收起」。
 */
export function VersionMetaBar({ version, downloadingVersionId, onDownload }: VersionMetaBarProps) {
  const state = versionState(version)
  return <div className='report-management-detail-meta'>
    <MetaField label='版本状态'>
      <Tag color={VERSION_STATE_TAG[state].color}>{VERSION_STATE_TAG[state].text}</Tag>
    </MetaField>
    <MetaField label='源端修改时间'>{toDisplayDateTime(version.sourceModifiedTime) || '—'}</MetaField>
    <MetaField label='平台接收时间'>{toDisplayDateTime(version.platformReceivedTime) || '—'}</MetaField>
    <MetaField label='报告医生'>{missingText(version.reportDoctorName)}</MetaField>
    <MetaField label='审核医生'>{missingText(version.reviewDoctorName)}</MetaField>
    <MetaField label='PDF'>
      <span className='report-management-detail-meta-pdf'>
        <PdfFileName fileName={version.pdfFileName} />
        <VersionDownloadButton version={version} downloadingVersionId={downloadingVersionId} onDownload={onDownload} />
      </span>
    </MetaField>
    {version.sourceReportRemark === null ? null : (
      <RemarkLine key={version.sourceReportRemark} remark={version.sourceReportRemark} />
    )}
  </div>
}

/**
 * 来源报告备注：两行截断（line-clamp 2），溢出时出现「展开/收起」文字按钮（`aria-expanded`）。
 * 溢出按滚动高度测量：挂载后测一次，并经 `ResizeObserver`
 * 随备注行尺寸变化（如窗口收窄引起换行重排）重新测量；无 `ResizeObserver` 的环境（jsdom）
 * 保持挂载单次测量的现行为，不为此新增依赖。无布局环境（clientHeight 为 0）时不测量、
 * 保留展开入口，真实浏览器中的可见性由挂载测量与重测修正。
 */
function RemarkLine({ remark }: { remark: string }) {
  const remarkRef = useRef<HTMLDivElement>(null)
  /** `null` 表示尚未完成测量（或无布局环境）：保留展开入口，避免无布局环境下能力缺失。 */
  const [overflow, setOverflow] = useState<boolean | null>(null)
  const [expanded, setExpanded] = useState(false)
  /**
   * 展开态在 ref 上镜像（点击时同事件拍翻转，先于类名变化进入布局）：展开会移除两行截断、
   * 行高随之变化，RO 回调若照常测量会把「展开后无截断」误判为「无溢出」并卸载切换入口，
   * 展开态将无法返回收起态，因此展开期间跳过重测。
   */
  const expandedRef = useRef(false)
  useLayoutEffect(() => {
    const node = remarkRef.current
    // clientHeight 为 0 说明没有布局能力（如 jsdom）：不测量、保留展开入口。
    if (node === null || node.clientHeight === 0) return
    const measure = () => {
      if (expandedRef.current) return
      setOverflow(node.scrollHeight > node.clientHeight + 1)
    }
    measure()
    if (typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(measure)
    observer.observe(node)
    return () => observer.disconnect()
  }, [remark])
  return <div className='report-management-detail-meta-remark'>
    <div
      ref={remarkRef}
      className={expanded ? 'report-management-detail-remark-expanded' : 'report-management-detail-remark'}
    >{remark}</div>
    {overflow === false ? null : (
      <Button
        type='link'
        size='small'
        className='report-management-detail-remark-toggle'
        aria-expanded={expanded}
        onClick={() => {
          expandedRef.current = !expandedRef.current
          setExpanded((previous) => !previous)
        }}
      >{expanded ? '收起' : '展开'}</Button>
    )}
  </div>
}

/** 检验结果行、菌种行与药敏行类型：从版本详情交付的检验内容读模型上派生，不在组件内重新声明字段形状。 */
type LaboratoryResultItem = NonNullable<LaboratoryReportContentViewReadModel['results']>[number]
type LaboratoryBacteriaItem = NonNullable<LaboratoryReportContentViewReadModel['bacteriaResults']>[number]
type LaboratorySusceptibilityItem = NonNullable<LaboratoryBacteriaItem['susceptibilities']>[number]

/** 结果异常标志的否定原文；空白与「正常」都按 L1 灰「—」呈现（三级视觉编码的第一级）。 */
const NORMAL_ABNORMAL_FLAG = '正常'

/** 次行小字：编码、类型等降级文本，12px tertiary、tabular-nums，空白不占行。 */
function LabSub(props: { prefix?: string; text: string | null | undefined }) {
  const text = props.text?.trim() ?? ''
  if (text.length === 0) return null
  return <span className='report-management-detail-lab-sub'>{props.prefix === undefined ? text : `${props.prefix} ${text}`}</span>
}

/** 标本信息带字段：label 在上、值在下（与版本元信息条同一形态）。 */
function LabField(props: { label: string; children: ReactNode }) {
  return <div className='report-management-detail-lab-field'>
    <div className='report-management-detail-lab-field-label'>{props.label}</div>
    <div className='report-management-detail-lab-field-value'>{props.children}</div>
  </div>
}

/** 标本信息带的「名称（编码）」字段：名称主行 + 编码次行小字。 */
function LabFieldWithCode(props: { label: string; name: string | null | undefined; code: string | null | undefined }) {
  return <LabField label={props.label}>
    {missingText(props.name)}
    <LabSub text={props.code} />
  </LabField>
}

/**
 * 标本信息带：浅底信息带，首行院内标本号、标本类型、报告类别、
 * 来源医嘱流水号、检验人（编码与 ID 按次行小字），次行四个时间并列；右上角整体异常标识
 * 非「否」且非空时渲染 warning Tag（提示级，红色保留给结果级异常与危急）；报告备注独立行 break-words。
 */
function SpecimenBand({ content }: { content: LaboratoryReportContentViewReadModel }) {
  const showOverallAbnormal = !treatedAsNone(content.overallAbnormalFlag)
  const remark = content.reportRemark?.trim() ?? ''
  return <>
    <section className='report-management-detail-lab-specimen'>
      {showOverallAbnormal ? <Tag color='warning' className='report-management-detail-lab-specimen-flag'>整体异常</Tag> : null}
      <div className='report-management-detail-lab-specimen-row'>
        <LabField label='院内标本号'>{missingText(content.sourceSpecimenNo)}</LabField>
        <LabFieldWithCode label='标本类型' name={content.specimenTypeName} code={content.specimenTypeCode} />
        <LabFieldWithCode label='报告类别' name={content.reportCategoryName} code={content.reportCategoryCode} />
        <LabField label='来源医嘱流水号'>{missingText(content.sourceOrderSerialNo)}</LabField>
        <LabFieldWithCode label='检验人' name={content.inspectorName} code={content.inspectorId} />
      </div>
      <div className='report-management-detail-lab-specimen-row'>
        <LabField label='标本采集时间'>{toDisplayDateTime(content.specimenCollectedTime) || MISSING_TEXT}</LabField>
        <LabField label='标本送检时间'>{toDisplayDateTime(content.specimenSubmittedTime) || MISSING_TEXT}</LabField>
        <LabField label='检验科接收时间'>{toDisplayDateTime(content.laboratoryReceivedTime) || MISSING_TEXT}</LabField>
        <LabField label='检测完成时间'>{toDisplayDateTime(content.testingCompletedTime) || MISSING_TEXT}</LabField>
      </div>
    </section>
    {remark.length === 0 ? null : <div className='report-management-detail-lab-remark'>报告备注：{remark}</div>}
  </>
}

/** 异常列（L1/L2）：异常标志空或「正常」灰「—」不渲染「否」，其余原文 error Tag。 */
function AbnormalCell({ flag }: { flag: string | null | undefined }) {
  if (treatedAsNone(flag, NORMAL_ABNORMAL_FLAG)) {
    return <span className='report-management-detail-lab-muted'>{MISSING_TEXT}</span>
  }
  return <Tag color='error'>{flag?.trim()}</Tag>
}

/**
 * 危急列（L3）：危急值标志为 true 呈现实心「危急」Tag（底 colorError、
 * 白字、字重 600 由配套 CSS 类承载，ThunderboltFilled 图标、aria-label「危急值」），其余灰「—」。
 */
function CriticalCell({ flag }: { flag: boolean | null | undefined }) {
  if (flag !== true) return <span className='report-management-detail-lab-muted'>{MISSING_TEXT}</span>
  return <Tag
    aria-label='危急值'
    className='report-management-detail-lab-critical-tag'
    icon={<ThunderboltFilled />}
  >危急</Tag>
}

/** 检测条件单元格：三行 12px 堆叠，每行前置 4px tertiary 小标签（结果表第 8 列的形制）。 */
function ConditionCell(props: { lines: Array<{ tag: string; text: string | null | undefined }> }) {
  return <span className='report-management-detail-lab-cond'>
    {props.lines.map((line) => (
      <span key={line.tag} className='report-management-detail-lab-cond-line'>
        <span className='report-management-detail-lab-cond-tag'>{line.tag}</span>
        {missingText(line.text)}
      </span>
    ))}
  </span>
}

/**
 * 来源明细稳定行键：来源明细键优先（来源系统的稳定明细标识），空白时退回调用方给定的兜底串，
 * 兜底串以「序号 + 名称等业务字段 + 序次」组合收尾保唯一（同文件列表键先例），不单独使用数组索引
 * （Frontend Application 第 5 节）。
 */
function sourceKeyOf(key: string | null | undefined, fallback: string): string {
  const text = key?.trim() ?? ''
  return text.length > 0 ? text : fallback
}

/** 检验结果表 8 列：总宽约 884px，窄容器横向滚动并固定「项目」「结果」两列。 */
const resultColumns: TableProps<LaboratoryResultItem>['columns'] = [
  { title: '序号', width: 44, align: 'right', className: 'report-management-detail-lab-num', dataIndex: 'displayOrder', render: (value) => value ?? MISSING_TEXT },
  {
    title: '项目',
    width: 200,
    fixed: 'left',
    render: (_, record) => <span>
      {missingText(record.sourceProjectName)}
      <LabSub prefix='来源' text={record.sourceProjectCode} />
      <LabSub prefix='互认' text={record.standardProjectCode} />
    </span>,
  },
  {
    title: '结果',
    width: 130,
    fixed: 'left',
    render: (_, record) => <span>
      <span className='report-management-detail-lab-result-text'>{missingText(record.sourceResultText)}</span>
      <LabSub text={record.resultTypeText} />
    </span>,
  },
  { title: '单位', width: 64, render: (_, record) => missingText(record.unit) },
  { title: '异常', width: 88, render: (_, record) => <AbnormalCell flag={record.abnormalFlagText} /> },
  { title: '危急', width: 88, render: (_, record) => <CriticalCell flag={record.criticalValueFlag} /> },
  { title: '参考范围', width: 120, className: 'report-management-detail-lab-wrap', render: (_, record) => missingText(record.referenceRange) },
  {
    title: '检测条件',
    width: 150,
    render: (_, record) => <ConditionCell lines={[
      { tag: '法', text: record.testingMethod },
      { tag: '仪', text: record.instrumentName },
      { tag: '人', text: record.inspectorName },
    ]} />,
  },
]

/** 检验结果表：`size="small"`、无分页，危急行染底与左竖条由 rowClassName 命中。 */
function ResultTable({ results }: { results: LaboratoryResultItem[] }) {
  return <Table<LaboratoryResultItem>
    size='small'
    rowKey={(record, index) => sourceKeyOf(record.sourceDetailKey, `result-${record.displayOrder ?? ''}-${record.sourceProjectName ?? ''}-${record.sourceResultText ?? ''}-${index ?? ''}`)}
    rowClassName={(record) => record.criticalValueFlag === true ? 'report-management-detail-lab-row-critical' : ''}
    columns={resultColumns}
    dataSource={results}
    pagination={false}
    scroll={{ x: 884 }}
  />
}

/** 培养条件带单项：label 12px tertiary、值 13px，横向 wrap。 */
function CultureItem(props: { label: string; value: string | null | undefined }) {
  return <span className='report-management-detail-lab-culture-item'>
    <span className='report-management-detail-lab-culture-label'>{props.label}</span>
    <span className='report-management-detail-lab-culture-value'>{missingText(props.value)}</span>
  </span>
}

/** 药敏结论三色编码：含「耐药/R」error、含「敏感/S」success、含「中介/I」warning、未命中中性原文。 */
function SusceptibilityConclusionCell({ text }: { text: string | null | undefined }) {
  const value = text?.trim() ?? ''
  if (value.length === 0) return <span className='report-management-detail-lab-muted'>{MISSING_TEXT}</span>
  if (/耐药|\(R\)|（R）/.test(value)) return <Tag color='error'>{value}</Tag>
  if (/敏感|\(S\)|（S）/.test(value)) return <Tag color='success'>{value}</Tag>
  if (/中介|\(I\)|（I）/.test(value)) return <Tag color='warning'>{value}</Tag>
  return <Tag>{value}</Tag>
}

/** 药敏表 7 列：总宽约 712px，检测条件三行同结果表第 8 列形制。 */
const susceptibilityColumns: TableProps<LaboratorySusceptibilityItem>['columns'] = [
  { title: '序号', width: 44, align: 'right', className: 'report-management-detail-lab-num', dataIndex: 'displayOrder', render: (value) => value ?? MISSING_TEXT },
  { title: '受试药物', width: 150, render: (_, record) => missingText(record.drugName) },
  { title: '结论', width: 88, render: (_, record) => <SusceptibilityConclusionCell text={record.sourceConclusionText} /> },
  { title: 'MIC', width: 90, className: 'report-management-detail-lab-num', render: (_, record) => missingText(record.micValue) },
  { title: '抑菌环', width: 90, className: 'report-management-detail-lab-num', render: (_, record) => missingText(record.inhibitionZoneDiameter) },
  {
    title: '含药量/参考值',
    width: 110,
    render: (_, record) => <span>
      {missingText(record.diskContent)}
      <LabSub prefix='参考' text={record.referenceValue} />
    </span>,
  },
  {
    title: '检测条件',
    width: 140,
    render: (_, record) => <ConditionCell lines={[
      { tag: '法', text: record.testingMethod },
      { tag: '板', text: record.testPanelOrder },
      { tag: '人', text: record.inspectorName },
    ]} />,
  },
]

/** 药敏表：`size="small"`、无分页，窄容器横向滚动。 */
function SusceptibilityTable({ susceptibilities }: { susceptibilities: LaboratorySusceptibilityItem[] }) {
  return <Table<LaboratorySusceptibilityItem>
    className='report-management-detail-lab-susceptibility'
    size='small'
    rowKey={(record, index) => sourceKeyOf(record.sourceDetailKey, `susceptibility-${record.displayOrder ?? ''}-${record.drugName ?? ''}-${index ?? ''}`)}
    columns={susceptibilityColumns}
    dataSource={susceptibilities}
    pagination={false}
    scroll={{ x: 712 }}
  />
}

/** 菌种卡卡身：培养条件带、来源结果原文折叠（默认收起）与药敏表（无药敏沿用既有空态文案）。 */
function BacteriaCardBody({ bacteria }: { bacteria: LaboratoryBacteriaItem }) {
  const susceptibilities = bacteria.susceptibilities ?? []
  return <>
    <div className='report-management-detail-lab-culture'>
      <CultureItem label='培养基' value={bacteria.cultureMedium} />
      <CultureItem label='培养时间' value={bacteria.cultureTime} />
      <CultureItem label='培养条件' value={bacteria.cultureCondition} />
      <CultureItem label='检测方法' value={bacteria.detectionMethod} />
      <CultureItem label='仪器' value={bacteria.instrumentName} />
      <CultureItem label='试验板' value={bacteria.testPanelName} />
      <CultureItem label='检测人' value={bacteria.inspectorName} />
    </div>
    <Collapse ghost items={[{
      key: 'raw',
      label: '原始结果文本',
      children: <div className='report-management-detail-lab-raw'>{missingText(bacteria.sourceResultText)}</div>,
    }]} />
    {susceptibilities.length === 0
      ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该细菌没有药敏结果' />
      : <SusceptibilityTable susceptibilities={susceptibilities} />}
  </>
}

/**
 * 菌种卡：每菌一张 `Card size="small"`，卡头菌名主行（编码次行）加 colonyCount
 * 计数与检测结论；卡头右侧开关控制卡身折叠，默认展开（antd Card 无内建可折叠属性，开关为文字按钮，
 * `aria-expanded` 与含菌名的 `aria-label` 携带状态与对象）。
 */
function BacteriaCard({ bacteria }: { bacteria: LaboratoryBacteriaItem }) {
  const [collapsed, setCollapsed] = useState(false)
  const organismName = bacteria.sourceOrganismName?.trim() ?? ''
  return <Card
    size='small'
    className='report-management-detail-lab-bacteria-card'
    title={
      <span className='report-management-detail-lab-bacteria-head'>
        <span className='report-management-detail-lab-bacteria-name'>
          {missingText(bacteria.sourceOrganismName)}
          <LabSub text={bacteria.sourceOrganismCode} />
        </span>
        <span className='report-management-detail-lab-bacteria-colony'>{missingText(bacteria.colonyCount)}</span>
        <span className='report-management-detail-lab-bacteria-conclusion'>{missingText(bacteria.detectionConclusion)}</span>
      </span>
    }
    extra={
      <Button
        type='text'
        size='small'
        className='report-management-detail-lab-bacteria-toggle'
        aria-expanded={!collapsed}
        aria-label={`${collapsed ? '展开' : '收起'}${organismName === '' ? '菌种' : organismName}菌种卡`}
        onClick={() => setCollapsed((previous) => !previous)}
      >{collapsed ? '展开' : '收起'}</Button>
    }
  >
    {collapsed ? null : <BacteriaCardBody bacteria={bacteria} />}
  </Card>
}

export interface LaboratoryContentProps {
  /** 版本详情交付的检验结构化内容读模型；`null`（该版本没有检验结构化内容）时呈现既有空态文案。 */
  content: LaboratoryReportContentViewReadModel | null
}

/**
 * 检验内容卡内容：标本信息带（整体异常标识与报告备注）、普通检验结果表
 * 三级视觉编码（L1 灰「—」/ L2 异常 error Tag / L3 危急实心 Tag 加整行染底）、细菌鉴定菌种卡
 * 与药敏表；三个层级的子表为空沿用既有空态文案。
 */
export function LaboratoryContent({ content }: LaboratoryContentProps) {
  if (content === null) {
    return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检验结构化内容' />
  }
  const results = content.results ?? []
  const bacteriaResults = content.bacteriaResults ?? []
  return <div className='report-management-detail-lab'>
    <SpecimenBand content={content} />
    <section className='report-management-detail-lab-results'>
      <h4 className='report-management-detail-lab-group-title'>普通检验结果</h4>
      {results.length === 0
        ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有普通检验结果' />
        : <ResultTable results={results} />}
    </section>
    <section className='report-management-detail-lab-bacteria'>
      <h4 className='report-management-detail-lab-group-title'>细菌鉴定结果与药敏结果</h4>
      {bacteriaResults.length === 0
        ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有细菌鉴定结果' />
        : bacteriaResults.map((item, index) => (
          <BacteriaCard
            key={sourceKeyOf(item.sourceDetailKey, `bacteria-${item.sourceOrganismCode ?? ''}-${item.sourceOrganismName ?? ''}-${index}`)}
            bacteria={item}
          />
        ))}
    </section>
  </div>
}

/** 检查项目行与检查部位行类型：从版本详情交付的检查内容读模型上派生，不在组件内重新声明字段形状。 */
type ExaminationItem = NonNullable<ExaminationReportContentViewReadModel['items']>[number]

/** 检查内容节：节标题与检验内容卡同一形制（13px/600 secondary），标题下承载该层内容。 */
function ExamSection(props: { title: string; children: ReactNode }) {
  return <section>
    <h4 className='report-management-detail-exam-group-title'>{props.title}</h4>
    {props.children}
  </section>
}

/**
 * 第 1 层检查结论 callout（结论优先置顶）：块头行为「检查结论」节标题加
 * 整体异常标识 Tag（非「否」且非空时 warning，与检验整体异常同一提示级），正文为结论原文。
 */
function ConclusionCallout({ content }: { content: ExaminationReportContentViewReadModel }) {
  const showOverallAbnormal = !treatedAsNone(content.overallAbnormalFlag)
  return <section className='report-management-detail-exam-conclusion'>
    <div className='report-management-detail-exam-conclusion-head'>
      <h4 className='report-management-detail-exam-group-title'>检查结论</h4>
      {showOverallAbnormal ? <Tag color='warning'>整体异常</Tag> : null}
    </div>
    <div className='report-management-detail-exam-conclusion-text'>{missingText(content.conclusion)}</div>
  </section>
}

/** 临床背景卡头的病情描述摘要字数（取病情描述前 40 字作卡头摘要）。 */
const CLINICAL_SUMMARY_CHARS = 40

/**
 * 第 4 层临床背景：`Collapse`「临床背景」默认收起，卡头显示病情描述前 40 字
 * 摘要（12px tertiary）；展开后为病情描述、检查目的两行（break-words 由配套 CSS 承载）。
 */
function ClinicalBackground({ content }: { content: ExaminationReportContentViewReadModel }) {
  const condition = content.conditionDescription?.trim() ?? ''
  const summary = condition.slice(0, CLINICAL_SUMMARY_CHARS)
  return <Collapse
    ghost
    items={[{
      key: 'clinical',
      label: <span className='report-management-detail-exam-clinic-head'>
        临床背景
        {summary === '' ? null : <span className='report-management-detail-exam-clinic-summary'>{summary}</span>}
      </span>,
      children: <Descriptions bordered={false} column={1} items={[
        { key: '病情描述', label: '病情描述', children: <span className='report-management-detail-exam-clinic-text'>{missingText(content.conditionDescription)}</span> },
        { key: '检查目的', label: '检查目的', children: <span className='report-management-detail-exam-clinic-text'>{missingText(content.examinationPurpose)}</span> },
      ]} />,
    }]}
  />
}

/** 来源影像状态：中性 Tag 呈现原文（与列表页现状一致，不引入语义色），原文空白按缺失占位。 */
function ImageStatusTag({ text }: { text: string | null | undefined }) {
  const value = text?.trim() ?? ''
  if (value.length === 0) return <>{MISSING_TEXT}</>
  return <Tag>{value}</Tag>
}

/**
 * 影像调阅地址外链：新窗口打开、不带 opener 引用、中文可访问名称并带图标；
 * 地址缺失按缺失占位符呈现。
 */
function ImageAccessLink({ url }: { url: string | null | undefined }) {
  const value = url?.trim() ?? ''
  if (value.length === 0) return <>{MISSING_TEXT}</>
  return <Typography.Link
    href={value}
    target='_blank'
    rel='noreferrer noopener'
    aria-label='在新窗口打开影像调阅地址'
  ><FileImageOutlined /> 打开影像</Typography.Link>
}

/** 检查信息七项（第 5 层）：字段顺序为来源检查类型、实际检查时间、检查医生、检查方法、设备、来源影像状态与影像调阅地址，「名称（编码）」对按名称主行加标识次行小字呈现。 */
function examInfoItems(content: ExaminationReportContentViewReadModel): NonNullable<DescriptionsProps['items']> {
  return [
    itemWithId('来源检查类型', content.sourceExaminationTypeName, content.sourceExaminationTypeCode),
    textItem('实际检查时间', toDisplayDateTime(content.examinationTime)),
    itemWithId('检查医生', content.examinerName, content.examinerId),
    textItem('检查方法', content.examinationMethod),
    itemWithId('设备', content.deviceName, content.deviceCode),
    { key: '来源影像状态', label: '来源影像状态', children: <ImageStatusTag text={content.sourceImageStatusText} /> },
    { key: '影像调阅地址', label: '影像调阅地址', children: <ImageAccessLink url={content.imageAccessUrl} /> },
  ]
}

/** 部位编码次行小字（11px tertiary，随第 6 层检查项目与部位呈现）；空白不占位。 */
function SiteCode({ code }: { code: string | null | undefined }) {
  const value = code?.trim() ?? ''
  if (value.length === 0) return null
  return <span className='report-management-detail-exam-site-code'>{value}</span>
}

/**
 * 第 6 层单个检查项目块：左 2px colorBorderSecondary 竖线 + 左 padding 的
 * 轻量块，首行项目名主行加双编码次行小字，次行部位 chips（部位名 13px + 编码 11px）；
 * 无部位沿用既有文案「该项目没有明确检查部位」。
 */
function ExaminationItemBlock({ item }: { item: ExaminationItem }) {
  const sites = item.sites ?? []
  return <div className='report-management-detail-exam-item'>
    <div className='report-management-detail-exam-item-name'>
      {missingText(item.sourceProjectName)}
      <LabSub prefix='来源' text={item.sourceProjectCode} />
      <LabSub prefix='互认' text={item.standardProjectCode} />
    </div>
    <div className='report-management-detail-exam-item-sites'>
      {sites.length === 0 ? <span className='report-management-detail-exam-site-empty'>该项目没有明确检查部位</span>
        : sites.map((site, index) => (
          <Tag
            // 稳定键：来源部位编码优先，空白时退回部位名，两者皆空白才以序次兜底（静态只读列表，仅为避免重复键）。
            key={sourceKeyOf(site.sourceSiteCode, sourceKeyOf(site.siteName, `site-${index}`))}
            className='report-management-detail-exam-site'
          >
            <span className='report-management-detail-exam-site-name'>{missingText(site.siteName)}</span>
            <SiteCode code={site.sourceSiteCode} />
          </Tag>
        ))}
    </div>
  </div>
}

export interface ExaminationContentProps {
  /** 版本详情交付的检查结构化内容读模型；`null`（该版本没有检查结构化内容）时呈现既有空态文案。 */
  content: ExaminationReportContentViewReadModel | null
}

/**
 * 检查内容卡内容：六个层次自上而下单列纵排——检查结论 callout、检查所见
 * 正文（72ch 行宽）、来源诊断（编码次行小字）、临床背景折叠（默认收起）、检查信息 Descriptions
 * （影像调阅外链新窗口打开）与检查项目与部位 chips；子层空态沿用「该项目没有明确检查部位」
 * 「该版本没有检查项目」。
 */
export function ExaminationContent({ content }: ExaminationContentProps) {
  if (content === null) {
    return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检查结构化内容' />
  }
  const items = content.items ?? []
  return <div className='report-management-detail-exam'>
    <ConclusionCallout content={content} />
    <ExamSection title='检查所见'>
      <p className='report-management-detail-exam-findings'>{missingText(content.findings)}</p>
    </ExamSection>
    <ExamSection title='来源诊断'>
      <NameWithId name={content.sourceDiagnosisName} id={content.sourceDiagnosisCode} />
    </ExamSection>
    <ClinicalBackground content={content} />
    <ExamSection title='检查信息'>
      <Descriptions bordered={false} column={3} items={examInfoItems(content)} />
    </ExamSection>
    <ExamSection title='检查项目与部位'>
      {items.length === 0
        ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该版本没有检查项目' />
        : <div className='report-management-detail-exam-items'>
          {items.map((item, index) => (
            <ExaminationItemBlock
              // 稳定键：来源项目编码优先，空白时退回项目名，两者皆空白才以序次兜底。
              key={sourceKeyOf(item.sourceProjectCode, sourceKeyOf(item.sourceProjectName, `item-${index}`))}
              item={item}
            />
          ))}
        </div>}
    </ExamSection>
  </div>
}

/** 时间线节点圆点的三态类名：当前实心、已被替代空心、已作废空心。 */
const TIMELINE_DOT_CLASS: Record<VersionState, string> = {
  当前有效: 'report-management-detail-timeline-dot-current',
  已被替代: 'report-management-detail-timeline-dot-superseded',
  已作废: 'report-management-detail-timeline-dot-voided',
}

/** 时间线节点版本状态 Tag 配色：当前有效 success，其余中性灰（红色保留给异常与危急值）。 */
const TIMELINE_STATE_TAG_COLOR: Record<VersionState, 'success' | 'default'> = {
  当前有效: 'success',
  已被替代: 'default',
  已作废: 'default',
}

/**
 * 时间线节点的事实行：「label 值」紧凑同行呈现；完整的 label/值形制由版本信息卡元信息条承载，
 * 节点行保持清单式密度。备注行叠加单行截断。
 */
function TimelineFact(props: { label: string; value: string; remark?: boolean }) {
  return <span
    className={props.remark === true
      ? 'report-management-detail-timeline-row report-management-detail-timeline-row-remark'
      : 'report-management-detail-timeline-row'}
  >{props.label} {props.value}</span>
}

/** 时间线单个节点：版本序号 + 版本状态 Tag、事实行、PDF 文件名与两个动作。 */
function TimelineNode(props: {
  version: ReportVersionRow
  downloadingVersionId: string | null
  onView: (reportVersionId: string) => void
  onDownload: (version: ReportVersionRow) => void
}) {
  const { version, downloadingVersionId, onView, onDownload } = props
  const state = versionState(version)
  return <div className='report-management-detail-timeline-node'>
    <div className='report-management-detail-timeline-node-head'>
      <span className='report-management-detail-timeline-version'>{`V${version.versionSequence}`}</span>
      <Tag color={TIMELINE_STATE_TAG_COLOR[state]}>{state}</Tag>
    </div>
    <div className='report-management-detail-timeline-rows'>
      <TimelineFact label='源端修改时间' value={toDisplayDateTime(version.sourceModifiedTime) || MISSING_TEXT} />
      <TimelineFact label='平台接收时间' value={toDisplayDateTime(version.platformReceivedTime) || MISSING_TEXT} />
      <TimelineFact label='报告医生' value={missingText(version.reportDoctorName)} />
      <TimelineFact label='审核医生' value={missingText(version.reviewDoctorName)} />
      <TimelineFact label='来源备注' value={missingText(version.sourceReportRemark)} remark />
    </div>
    <PdfFileName fileName={version.pdfFileName} />
    <div className='report-management-detail-timeline-actions'>
      {/* 查看此版本与版本切换控件共用同一选择入口（选中与读取编排由页面承载）。 */}
      <Button
        type='link'
        size='small'
        aria-label={`查看第 ${version.versionSequence} 版内容`}
        onClick={() => onView(version.reportVersionId)}
      >查看此版本</Button>
      {/* 下载接线页面级单在途互斥：在途按钮 loading，其余版本按钮禁用（与版本信息卡的下载入口同一防重复规则）。 */}
      <VersionDownloadButton version={version} downloadingVersionId={downloadingVersionId} onDownload={onDownload} />
    </div>
  </div>
}

export interface VersionTimelineProps {
  /** 全部版本（按版本序号交付）。 */
  versions: ReportVersionRow[]
  /** 正在下载的版本标识：与节点版本一致时按钮进入在途态，存在其他在途时本节点按钮禁用。 */
  downloadingVersionId: string | null
  /** 查看此版本回调（与版本切换控件同一选择入口）。 */
  onView: (reportVersionId: string) => void
  /** 下载回调（单在途互斥与失败复位由页面编排）。 */
  onDownload: (version: ReportVersionRow) => void
}

/**
 * 历史版本时间线：`Collapse` 折叠头「全部版本（N）」默认展开、可手动收起；
 * `Timeline` 每节点自上而下呈现版本序号（tabular-nums/600）与版本状态 Tag、源端修改与平台接收
 * 时间、报告与审核医生、来源备注（单行截断、缺失「—」）、PDF 文件名（中段截断加 Tooltip）与
 * 「查看此版本」「下载 PDF」两个动作；节点圆点三态由配套 CSS 按状态类呈现。
 */
export function VersionTimeline({ versions, downloadingVersionId, onView, onDownload }: VersionTimelineProps) {
  return <Collapse
    className='report-management-detail-timeline-collapse'
    defaultActiveKey={['all']}
    items={[{
      key: 'all',
      label: `全部版本（${versions.length}）`,
      children: <Timeline
        className='report-management-detail-timeline'
        items={versions.map((version) => ({
          key: version.reportVersionId,
          icon: <span
            className={`report-management-detail-timeline-dot ${TIMELINE_DOT_CLASS[versionState(version)]}`}
            aria-hidden='true'
          />,
          content: (
            <TimelineNode
              version={version}
              downloadingVersionId={downloadingVersionId}
              onView={onView}
              onDownload={onDownload}
            />
          ),
        }))}
      />,
    }]}
  />
}
