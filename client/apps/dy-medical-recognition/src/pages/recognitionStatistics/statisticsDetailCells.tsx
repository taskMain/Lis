/**
 * 统计明细表的单元格组件：时间、患者、项目、归属方、科室/医生、反馈结果和行展开内容。
 * 明细表主体只负责列定义与分页，具体展示口径集中在此文件。
 */
import { Descriptions, Tag } from 'antd'
import type { ReactNode } from 'react'
import type { UsageDetailRow } from './recognitionUsageStatisticsApi'
import {
  formatStatisticsAmount,
  formatStatisticsDateTime,
  statisticsTextOrDash,
} from './statisticsDisplays'
import { RECOGNITION_DECISION } from './recognitionStatisticsValues'
import { partyName } from './statisticsDetailValues'

/** 归属方三行式单元格：组织名、医院名、院区名各占一行，只显名称不显示编码。 */
export function PartyCell(props: { organizationName?: string | null; hospitalName?: string | null; branchName?: string | null }) {
  const organization = partyName(props.organizationName)
  const hospital = partyName(props.hospitalName)
  const branch = partyName(props.branchName)
  return <div className='statistics-board-cell'>
    <span className='statistics-board-party-cell__organization' style={{ fontSize: 12 }} title={organization}>{organization}</span>
    <span className='statistics-board-party-cell__hospital' style={{ fontSize: 13 }} title={hospital}>{hospital}</span>
    <span className='statistics-board-party-cell__branch' style={{ fontSize: 12 }} title={branch}>{branch}</span>
  </div>
}

/** 明细单元格的单行文本：超出列宽时省略，并以 `title` 提供全文。 */
export function TextCell(props: { value: string | null | undefined }) {
  const text = statisticsTextOrDash(props.value)
  return <span className='statistics-board-cell__text' title={text}>{text}</span>
}

/** 明细单元格的次行文本：12px 次级色；证件号码等数字使用等宽数字。 */
function cellSecondary(value: string | null | undefined, numeric = false): ReactNode {
  const text = statisticsTextOrDash(value)
  return <span
    className={`statistics-board-cell__secondary${numeric ? ' statistics-board-cell__secondary--numeric' : ''}`}
    title={text}
  >{text}</span>
}

/** 时间列：业务时间主行，匹配生成时间次行。 */
export function TimeCell(props: { businessTime: Date | null; matchCreatedTime: Date | null }) {
  const business = formatStatisticsDateTime(props.businessTime)
  const matched = formatStatisticsDateTime(props.matchCreatedTime)
  return <div className='statistics-board-cell'>
    <span className='statistics-board-cell__text' title={business}>{business}</span>
    <span className='statistics-board-cell__secondary'>匹配 {matched}</span>
  </div>
}

/** 患者列：患者姓名主行，证件号码次行（服务端原值，S6-D9）。 */
export function PatientCell(props: { patientName: string; identityDocumentNo: string }) {
  return <div className='statistics-board-cell'>
    <TextCell value={props.patientName} />
    {cellSecondary(props.identityDocumentNo, true)}
  </div>
}

/** 项目列：标准项目名称为主行，编码为次行；名称缺失时给出明确文案。 */
export function ItemCell(props: { item: UsageDetailRow['item'] }) {
  const projectName = (props.item.standardProjectName ?? '').trim()
  const nameText = projectName.length > 0 ? projectName : '未匹配标准项目'
  return <div className='statistics-board-cell'>
    <span className='statistics-board-cell__text' title={nameText}>{nameText}</span>
    {cellSecondary(props.item.standardProjectCode)}
  </div>
}

/** 科室 / 医生列：互认科室主行，互认医生次行。 */
export function DeptDoctorCell(props: { deptName: string | null; doctorName: string | null }) {
  return <div className='statistics-board-cell'>
    <TextCell value={props.deptName} />
    {cellSecondary(props.doctorName)}
  </div>
}

/** 反馈结果列：未反馈使用固定提示，已反馈状态显示服务端返回的 decisionText。 */
export function FeedbackCell(props: { row: UsageDetailRow }) {
  const result = props.row.processingResult
  if (props.row.isUnprocessed || result === null) return <Tag>未反馈</Tag>
  if (result.decision === RECOGNITION_DECISION.nonAdopted) return <div className='statistics-board-cell'>
    <Tag color='red' className='statistics-board-cell__tag'>{result.decisionText}</Tag>
    {result.nonAdoptionReasonName === null ? null : cellSecondary(result.nonAdoptionReasonName)}
  </div>
  return <Tag color={result.decision === RECOGNITION_DECISION.adopted ? 'green' : undefined}>{result.decisionText}</Tag>
}

/** 使用方行展开：就诊、证件、互认时间与金额、引用信息、不采纳原因与补充说明、匹配记录标识。 */
export function UsageDetailExpanded(props: { row: UsageDetailRow }) {
  const result = props.row.processingResult
  const reason = result?.nonAdoptionReasonName ?? null
  const supplement = result?.nonAdoptionSupplementDescription ?? null
  const reasonText = reason === null
    ? '—'
    : supplement === null ? reason : `${reason}（${supplement}）`
  return <Descriptions
    size='small'
    bordered
    column={3}
    className='statistics-board-detail-expanded'
    items={[
      { key: 'visit', label: '就诊类型与流水号', children: `${props.row.visitTypeText} · ${props.row.visitSerialNo}` },
      { key: 'identity', label: '证件号码', children: statisticsTextOrDash(props.row.identityDocumentNo) },
      { key: 'recognitionTime', label: '互认时间', children: formatStatisticsDateTime(result?.recognitionTime ?? null) },
      { key: 'savingAmount', label: '预计节省金额', children: formatStatisticsAmount(result?.estimatedSavingAmount ?? null) },
      { key: 'referenceTime', label: '引用时间', children: formatStatisticsDateTime(props.row.reference?.referenceTime ?? null) },
      { key: 'referenceDept', label: '引用科室', children: statisticsTextOrDash(props.row.reference?.referenceDeptName) },
      { key: 'referenceDoctor', label: '引用医生', children: statisticsTextOrDash(props.row.reference?.referenceDoctorName) },
      { key: 'reason', label: '不采纳原因与补充说明', span: 2, children: reasonText },
      { key: 'matchRecord', label: '匹配记录标识', span: 3, children: statisticsTextOrDash(props.row.recognitionMatchRecordId) },
    ]}
  />
}
