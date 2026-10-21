/**
 * 匹配记录弹窗（阶段 6 前端设计「明细与分页」节，S6-D10）：
 * 从使用方明细行进入，展示组级信息、就诊与反馈状态、决定主体与全部匹配项；
 * 弹窗不提供报告内容、PDF 或影像入口。
 *
 * 读取失败只做本层恢复：保留弹窗、结束加载并提供重试，不弹本地接口错误（宿主统一提示）。
 */
import { Button, Descriptions, Modal, Space, Spin, Table, Typography, type TableColumnsType } from 'antd'
import type { MatchRecordItemView, MatchRecordView } from './recognitionUsageStatisticsApi'
import { formatStatisticsDateTime, statisticsTextOrDash } from './statisticsDisplays'

/** 弹窗的会话状态：记录标识、视图数据与加载/失败态。 */
export interface MatchRecordDialogState {
  recognitionMatchRecordId: string
  view: MatchRecordView | null
  loading: boolean
  failure: boolean
}

export interface MatchRecordModalProps {
  state: MatchRecordDialogState
  onRetry: () => void
  onClose: () => void
}

/** 匹配项表列：项目资料、来源归属、反馈状态与处理决定。 */
const matchItemColumns: TableColumnsType<MatchRecordItemView> = [
  { title: '项目类型', width: 90, render: (_, item) => item.item.itemTypeText },
  { title: '项目编码', width: 110, render: (_, item) => statisticsTextOrDash(item.item.standardProjectCode) },
  { title: '项目名称', width: 130, render: (_, item) => statisticsTextOrDash(item.item.standardProjectName) },
  { title: '提供方组织', width: 120, render: (_, item) => statisticsTextOrDash(item.source.organizationName) },
  { title: '提供方医院', width: 120, render: (_, item) => statisticsTextOrDash(item.source.hospitalName) },
  { title: '提供方院区', width: 110, render: (_, item) => statisticsTextOrDash(item.source.branchName) },
  { title: '反馈状态', width: 90, render: (_, item) => (item.isProcessed ? '已反馈' : '未反馈') },
  { title: '处理结果', width: 100, render: (_, item) => (item.isProcessed ? statisticsTextOrDash(item.decisionText) : '') },
  { title: '不采纳原因', width: 120, render: (_, item) => statisticsTextOrDash(item.nonAdoptionReasonName) },
]

export function MatchRecordModal({ state, onRetry, onClose }: MatchRecordModalProps) {
  const view = state.view
  return <Modal
    open
    title='互认匹配记录'
    width={920}
    onCancel={onClose}
    footer={<Button onClick={onClose}>关闭</Button>}
  >
    <Space direction='vertical' style={{ width: '100%' }}>
      {state.loading ? <Space><Spin size='small' /><span>正在读取匹配记录</span></Space> : null}
      {!state.loading && state.failure ? <Space size={8} wrap role='alert'>
        <Typography.Text type='danger'>匹配记录读取失败，请重试。</Typography.Text>
        <Button size='small' onClick={onRetry}>重试</Button>
      </Space> : null}
      {!state.loading && view !== null ? <>
        <Descriptions size='small' column={2} bordered>
          <Descriptions.Item label='匹配记录标识' span={2}>{statisticsTextOrDash(view.recognitionMatchRecordId)}</Descriptions.Item>
          <Descriptions.Item label='匹配生成时间'>{formatStatisticsDateTime(view.matchCreatedTime)}</Descriptions.Item>
          <Descriptions.Item label='使用方医院'>{statisticsTextOrDash(view.receiver.hospitalName)}</Descriptions.Item>
          <Descriptions.Item label='患者姓名'>{view.patientName}</Descriptions.Item>
          <Descriptions.Item label='证件号码'>{view.identityDocumentNo}</Descriptions.Item>
          <Descriptions.Item label='就诊类型'>{view.visitTypeText}</Descriptions.Item>
          <Descriptions.Item label='就诊流水号'>{view.visitSerialNo}</Descriptions.Item>
          <Descriptions.Item label='反馈状态'>{view.isProcessed ? '已反馈' : '未反馈'}</Descriptions.Item>
          <Descriptions.Item label='互认时间'>{formatStatisticsDateTime(view.recognitionTime)}</Descriptions.Item>
          <Descriptions.Item label='互认科室'>{statisticsTextOrDash(view.recognitionDeptName)}</Descriptions.Item>
          <Descriptions.Item label='互认医生'>{statisticsTextOrDash(view.recognitionDoctorName)}</Descriptions.Item>
        </Descriptions>
        <Table<MatchRecordItemView>
          rowKey='recognitionMatchItemId'
          size='small'
          columns={matchItemColumns}
          dataSource={view.matchItems}
          pagination={false}
        />
      </> : null}
    </Space>
  </Modal>
}
