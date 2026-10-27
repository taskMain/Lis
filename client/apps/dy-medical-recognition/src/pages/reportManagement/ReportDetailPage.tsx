/**
 * 「报告详情」详情路由页（D-01 独立详情路由页、D-03 保持挂载、D-05 深链）。
 *
 * 两个入口（`report-management`、`branch-report-management`）各挂一个实例，由入口容器在详情子路由
 * 激活时渲染；彼时列表组件保持挂载、仅切换可见性（返回还原由列表主体承担）。
 *
 * 行概要经列表主体的会话级缓存传递（`reportRowSummaryCache`，键为路径 `reportId`）：宿主 MicroSDK
 * 接管 BrowserRouter 的 history，`navigate` 的 state 会被 SDK 包装的 push 丢弃，故改用框架中立的
 * 缓存通道；缓存未命中（深链直开、刷新、粘贴、外发链接）缺失字段一律显示「—」，不额外发请求补数
 * （D-02 深链首开口径不变）。
 *
 * 本页只读。打开时按路径 `reportId` 读取版本列表：在途渲染骨架与加载播报；版本清单为空时呈现
 * 页面级空态与「返回列表」。版本列表到达后确定初始选中（URL `?v=` 序号优先，其次当前有效版本，
 * 再否则最大序号）并读取该版本详情；切换经共享控件（`ReportDetailContent`）进行，成功后以
 * `?v={versionSequence}` replace 写回 URL（返回列表路由时参数自然消失），完成后内容滚动容器回到顶部。
 * 概要头部（`SummaryHeader`）、公共信息四组（`PublicInfoGroups`）、检验内容卡（`LaboratoryContent`）
 * 与检查内容卡（`ExaminationContent`）由共享内容组件承接，第四卡标题按报告类型分发
 * （检验「检验内容」、检查「检查内容」，版本详情未到达或类型未知保持占位）；历史版本时间线
 * （`VersionTimeline`）承接第五卡，其「查看此版本」与版本切换控件共用同一选择入口。
 *
 * 失败口径（既定）：版本列表读取失败不渲染横幅、错误由宿主统一提示，保留空态与重进路径
 * （刷新或经列表重进）；版本详情读取失败（D-04）选中态静默回退到上一个成功版本，
 * 旧内容原样保留、不渲染横幅、不播报错误细节，错误由宿主统一提示。
 */
import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, Breadcrumb, Button, Card, Empty, Skeleton, Spin } from 'antd'
import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ExaminationContent, LaboratoryContent, PublicInfoGroups, SummaryHeader, SummaryStateTags, VersionMetaBar, VersionSegmented, VersionTimeline } from './ReportDetailContent'
import {
  EXAMINATION_REPORT_TYPE,
  LABORATORY_REPORT_TYPE,
  type ReportVersionDetail,
  type ReportVersionRow,
} from './reportsApi'
import { getRowSummary } from './reportRowSummaryCache'
import { missingText, scrollContainerOf } from './reportDetailShared'

/** 第四卡的占位标题：版本详情未到达或报告类型未知时保持「报告内容」口径。 */
const CONTENT_PLACEHOLDER_TITLE = '报告内容'

/**
 * 第四卡按报告类型一次分发：标题与卡身同源判定——
 * 检验报告为「检验内容」卡、检查报告为「检查内容」卡，两者按类型互斥呈现；
 * 类型未知维持「报告内容」占位标题与 info 级提示（信息蓝，非错误红）。
 */
function contentCard(detail: ReportVersionDetail): { title: string; body: ReactNode } {
  if (detail.reportType === LABORATORY_REPORT_TYPE) {
    return { title: '检验内容', body: <LaboratoryContent content={detail.laboratoryContent} /> }
  }
  if (detail.reportType === EXAMINATION_REPORT_TYPE) {
    return { title: '检查内容', body: <ExaminationContent content={detail.examinationContent} /> }
  }
  return {
    title: CONTENT_PLACEHOLDER_TITLE,
    body: <Alert type='info' showIcon title='报告类型未知：服务端未交付可识别的报告类型，无法展示该版本的结构化内容。' />,
  }
}

export interface ReportDetailPageProps {
  /** 路径参数中的报告标识。 */
  reportId: string
  /** 详情所属入口的列表父路由：深链直开返回时以 replace 跳回该路由，不产生空历史层。 */
  parentPath: string
  /** 面包屑父级名称（平台入口「报告管理」、医院入口「本院报告管理」）。 */
  parentName: string
  /** 读取某个报告的全部版本（与列表页同一适配层出口，由入口容器注入）。 */
  queryVersions: (reportId: string) => Promise<ReportVersionRow[]>
  /** 读取某个报告版本的完整内容（与列表页同一适配层出口，由入口容器注入）。 */
  queryDetail: (reportId: string, reportVersionId: string) => Promise<ReportVersionDetail>
  /** 下载某个报告版本的原始 PDF（与列表页同一适配层出口，由入口容器注入）。 */
  downloadVersion: (reportId: string, reportVersionId: string, downloadFileName: string) => Promise<void>
}

/** 版本列表的相位：挂载即在途；终态为空清单、有版本、读取失败三种（失败保留空态与重进路径）。 */
type VersionsPhase = 'loading' | 'empty' | 'ready' | 'failed'

export function ReportDetailPage({ reportId, parentPath, parentName, queryVersions, queryDetail, downloadVersion }: ReportDetailPageProps) {
  const location = useLocation()
  const navigate = useNavigate()
  /** 在途作废序号：详情按 reportId 重挂载，序号兜住同挂载期内快速重进的迟到响应。 */
  const versionsRequestIdRef = useRef(0)
  // 详情按 reportId 重挂载（入口容器设 key），挂载即在途，effect 只在异步完成后写入状态、不重复置位。
  const [phase, setPhase] = useState<VersionsPhase>('loading')
  const titleRef = useRef<HTMLHeadingElement>(null)

  /** 版本清单（含各版本的元信息事实），初始选中与呈现中的版本都从这里查找。 */
  const [versions, setVersions] = useState<ReportVersionRow[]>([])
  /** 当前浏览（控件选中）的版本标识；读取在途时指向目标版本，读取失败按 D-04 回退。 */
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null)
  /** 最近一次成功加载的版本详情；内容区呈现的唯一事实来源（选中与内容保持一致）。 */
  const [versionDetail, setVersionDetail] = useState<ReportVersionDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  /** 单在途互斥的下载版本标识；`null` 表示当前没有下载在途。 */
  const [downloadingVersionId, setDownloadingVersionId] = useState<string | null>(null)

  /** 版本详情在途读取的作废序号：快速连点只采纳最后一次（沿用列表页口径）。 */
  const detailRequestIdRef = useRef(0)
  /** 最近一次成功加载的版本标识：版本详情读取失败（D-04）时选中态静默回退到这里。 */
  const lastSuccessVersionIdRef = useRef<string | null>(null)
  /** 页面根节点：切换完成后内容滚动容器定位从这里出发。 */
  const pageRootRef = useRef<HTMLDivElement>(null)

  /**
   * 深链 `?v=` 的版本序号只在版本列表首达时解析一次（详情按 reportId 重挂载）；
   * 后续切换经 replace 写回参数，不再反向触发版本列表重读。
   */
  const [requestedSequence] = useState<number | null>(() => {
    const raw = new URLSearchParams(location.search).get('v')
    if (raw === null || !/^\d+$/.test(raw)) return null
    return Number(raw)
  })

  /** 行概要来自列表主体的会话级缓存（键为路径 reportId）；缓存未命中即深链直开，字段按「—」呈现。 */
  const rowSummary = getRowSummary(reportId)

  // 进入详情页焦点置于页面主标题，读屏播报单号。
  useEffect(() => {
    titleRef.current?.focus()
  }, [])

  // 卸载即作废全部在途响应：切换在途时经「返回」离开详情页，迟到的版本详情响应不得再执行
  // URL 写回与滚动清零（序号守卫只收敛同挂载期内的快速连点，防不住卸载后的迟到响应）。
  useEffect(() => () => {
    detailRequestIdRef.current += 1
    versionsRequestIdRef.current += 1
  }, [])

  /**
   * 读取指定版本详情（在途按序号作废）。
   * 成功：记录最近成功版本、更新内容；切换触发的读取在成功后写回 `?v={versionSequence}`
   * （replace 语义，返回列表路由时参数自然消失）并把内容滚动容器回到顶部。
   * 失败（D-04）：选中态静默回退到上一个成功版本，旧内容原样保留，不渲染横幅、不播报错误细节，
   * 错误由宿主统一提示。
   */
  const loadDetail = useCallback((version: ReportVersionRow, fromSwitch: boolean) => {
    const requestId = ++detailRequestIdRef.current
    setDetailLoading(true)
    queryDetail(reportId, version.reportVersionId).then((detail) => {
      if (requestId !== detailRequestIdRef.current) return
      lastSuccessVersionIdRef.current = version.reportVersionId
      setVersionDetail(detail)
      setDetailLoading(false)
      if (fromSwitch) {
        navigate({ pathname: location.pathname, search: `v=${version.versionSequence}` }, { replace: true })
        const container = scrollContainerOf(pageRootRef.current)
        if (container !== null) container.scrollTop = 0
      }
    }).catch(() => {
      if (requestId !== detailRequestIdRef.current) return
      setDetailLoading(false)
      const fallbackId = lastSuccessVersionIdRef.current
      if (fallbackId !== null && fallbackId !== version.reportVersionId) setSelectedVersionId(fallbackId)
    })
  }, [location.pathname, navigate, queryDetail, reportId])

  useEffect(() => {
    const requestId = ++versionsRequestIdRef.current
    queryVersions(reportId).then((rows) => {
      if (requestId !== versionsRequestIdRef.current) return
      setVersions(rows)
      if (rows.length === 0) {
        setPhase('empty')
        return
      }
      setPhase('ready')
      // 初始选中：URL `?v=` 序号能匹配则选之，其次当前有效版本，再否则最大序号。
      const matched = requestedSequence !== null
        ? rows.find((row) => row.versionSequence === requestedSequence)
        : undefined
      const current = rows.find((row) => row.isCurrentVersion)
      const initial = matched ?? current ?? rows.reduce((max, row) => (row.versionSequence > max.versionSequence ? row : max))
      setSelectedVersionId(initial.reportVersionId)
      loadDetail(initial, false)
    }).catch(() => {
      if (requestId !== versionsRequestIdRef.current) return
      // 版本列表读取失败保留空态与重进路径；错误由宿主统一提示（既定口径：不渲染横幅）。
      setPhase('failed')
    })
    // `requestedSequence` 是挂载期固化的深链初值（useState 初始化器只执行一次），值恒定不会触发重跑。
  }, [loadDetail, queryVersions, reportId, requestedSequence])

  /** 切换版本：控件立即响应选中，读取成功后才写回 URL（读取失败由 D-04 口径回退选中态）。 */
  const switchToVersion = (reportVersionId: string) => {
    if (reportVersionId === selectedVersionId) return
    const version = versions.find((row) => row.reportVersionId === reportVersionId)
    if (version === undefined) return
    setSelectedVersionId(reportVersionId)
    loadDetail(version, true)
  }

  /** 单在途互斥：同一时刻只允许一个下载在途；失败复位为可重试，错误由宿主统一提示（不渲染横幅）。 */
  const handleDownloadVersion = (version: ReportVersionRow) => {
    if (downloadingVersionId !== null) return
    setDownloadingVersionId(version.reportVersionId)
    downloadVersion(reportId, version.reportVersionId, version.pdfFileName).then(() => {
      setDownloadingVersionId((current) => (current === version.reportVersionId ? null : current))
    }).catch(() => {
      setDownloadingVersionId((current) => (current === version.reportVersionId ? null : current))
    })
  }

  /**
   * 呈现中的版本（元信息条事实来源）：优先取已成功加载的详情对应版本（选中与内容保持一致，
   * 切换在途时旧元信息随旧内容一起保留），初始详情在途时取选中版本。
   */
  const displayedVersionId = versionDetail?.reportVersionId ?? selectedVersionId
  const displayedVersion = versions.find((row) => row.reportVersionId === displayedVersionId) ?? null

  /** 第四卡呈现（按报告类型一次分发）：版本详情未到达时为 `null`，标题退回占位口径。 */
  const contentCardView = versionDetail === null ? null : contentCard(versionDetail)

  /** 详情内容区的在途与完成播报；失败不播报错误细节（静默口径的可达性表达）。 */
  const detailAnnouncement = detailLoading
    ? '正在加载版本详情…'
    : versionDetail !== null ? `第 ${versionDetail.versionSequence} 版详情已加载` : ''

  /**
   * 返回动作（确定性导航）：统一 replace 回列表父路由，不依赖历史回退——宿主 MicroSDK 接管
   * 路由历史，刷新与工作区页签恢复后的历史记录里可能没有列表项可退（`navigate(-1)` 停留详情页）。
   * 深链直开本就走 replace（不留下空历史层）；站内进入返回后由列表主体按来源行概要还原滚动与焦点。
   */
  const goBack = () => {
    // 同步作废在途响应：react-router 把导航更新包进 startTransition，点击到卸载提交之间存在
    // 在途响应先行执行的毫秒窗口（卸载清理只兜住提交之后的路径）。
    detailRequestIdRef.current += 1
    versionsRequestIdRef.current += 1
    navigate(parentPath, { replace: true })
  }

  return <div className='report-management-detail-page' ref={pageRootRef}>
    <div className='report-management-detail-topbar'>
      <Button type='text' aria-label='返回报告列表' icon={<ArrowLeftOutlined />} onClick={goBack} />
      <Breadcrumb
        items={[
          { title: <Button type='link' size='small' onClick={goBack}>{parentName}</Button> },
          { title: '报告详情' },
        ]}
      />
      <div className='report-management-detail-heading'>
        {/* 可访问名称携带页面名：焦点置于主标题时读屏播报「报告详情 + 单号」，深链单号占位时仍播报页面名。 */}
        <h1
          className='report-management-detail-no'
          tabIndex={-1}
          ref={titleRef}
          aria-label={`报告详情 ${missingText(rowSummary?.reportNo)}`}
        >{missingText(rowSummary?.reportNo)}</h1>
        {rowSummary === undefined ? null : <SummaryStateTags row={rowSummary} />}
      </div>
      {/* 版本切换控件位：版本列表在途为骨架，到达后由三形态控件承接。 */}
      <div className='report-management-detail-version-slot'>
        {phase === 'loading' ? <Skeleton.Button active shape='round' style={{ width: 240, height: 24 }} />
          : selectedVersionId !== null ? (
            <VersionSegmented versions={versions} selectedVersionId={selectedVersionId} onChange={switchToVersion} />
          ) : null}
      </div>
    </div>
    {/* 在途播报：视觉隐藏 + aria-live，加载开始宣告、结束移除。 */}
    <div className='report-management-detail-content'>
      {phase === 'loading' ? <span role='status' className='report-management-detail-visually-hidden'>正在加载版本列表…</span> : null}
      {/* 详情内容区的在途与完成播报：加载开始宣告「正在加载版本详情…」、完成宣告「第 N 版详情已加载」。 */}
      {detailAnnouncement !== '' ? <span role='status' className='report-management-detail-visually-hidden'>{detailAnnouncement}</span> : null}

      {/* 空版本清单：页面级 Empty 替换五卡壳；「返回列表」与返回动作同口径。 */}
      {phase === 'empty' ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description='该报告没有历史版本'>
        <Button type='primary' onClick={goBack}>返回列表</Button>
      </Empty> : <>
        <Card title='概要' styles={{ body: { padding: 20 } }}>
          <SummaryHeader row={rowSummary} />
        </Card>

        {/* 版本相关卡（版本信息、公共信息、报告内容、历史版本）：切换在途时整体压暗并居中加载
            （布局不跳动）；概要卡与版本无关，不参与压暗。 */}
        <div className='report-management-detail-versioned'>
          <div className={detailLoading
            ? 'report-management-detail-versioned-cards report-management-detail-versioned-cards-loading'
            : 'report-management-detail-versioned-cards'}
          >
            <Card title='版本信息' styles={{ body: { padding: 20 } }}>
              {phase === 'loading' ? <>
                <Skeleton.Input active block style={{ marginBottom: 8 }} />
                <Skeleton.Input active block />
              </> : displayedVersion !== null ? (
                <VersionMetaBar
                  version={displayedVersion}
                  downloadingVersionId={downloadingVersionId}
                  onDownload={handleDownloadVersion}
                />
              ) : null}
            </Card>

            {/* 公共信息四组随版本详情交付：随版本切换参与压暗，版本详情在途时保留旧内容。 */}
            <Card title='公共信息' styles={{ body: { padding: 20 } }}>
              {versionDetail === null ? null : <PublicInfoGroups common={versionDetail.common} />}
            </Card>

            {/* 第四卡按报告类型一次分发：检验内容卡 / 检查内容卡 / 类型未知提示；
                版本详情未到达时保持「报告内容」占位标题。 */}
            <Card
              title={contentCardView?.title ?? CONTENT_PLACEHOLDER_TITLE}
              styles={{ body: { padding: 20 } }}
            >
              {phase === 'loading' ? <Skeleton active title paragraph={{ rows: 6 }} />
                : contentCardView !== null ? contentCardView.body : null}
            </Card>

            {/* 历史版本时间线：版本列表在途渲染骨架；到达后折叠头「全部版本（N）」
                默认展开可收起，节点动作分别接线版本切换选择入口与单在途下载互斥。 */}
            <Card title='历史版本' styles={{ body: { padding: 20 } }}>
              {phase === 'loading' ? <Skeleton active title={false} paragraph={{ rows: 4 }} />
                : phase === 'ready' ? (
                  <VersionTimeline
                    versions={versions}
                    downloadingVersionId={downloadingVersionId}
                    onView={switchToVersion}
                    onDownload={handleDownloadVersion}
                  />
                ) : null}
            </Card>
          </div>
          {detailLoading ? <div className='report-management-detail-loading-overlay'>
            <Spin description='正在加载版本详情…' />
          </div> : null}
        </div>
      </>}
    </div>
  </div>
}
