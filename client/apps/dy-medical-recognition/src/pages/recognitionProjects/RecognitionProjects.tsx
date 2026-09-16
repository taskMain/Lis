import {
  CheckCircleOutlined,
  EditOutlined,
  PlusOutlined,
  ReloadOutlined,
  StopOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Empty,
  Input,
  Modal,
  Segmented,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  type TableColumnsType,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { useEnumMetadata } from '../../hooks/useEnumMetadata'
import {
  CreateConfigurationModal,
  DurationModal,
  ToggleModal,
  type RecognitionProjectWriteGate,
} from './RecognitionProjectModals'
import {
  EMPTY_RECOGNITION_PROJECT_FILTER,
  CONFIGURATION_STATUS_METADATA_FALLBACK,
  DISABLED_CONFIGURATION_STATUS,
  ENABLED_CONFIGURATION_STATUS,
  buildConfigurationListQuery,
  buildCreateConfigurationRequest,
  buildDurationUpdatePayload,
  configurationStatusActionText,
  configurationStatusTagColor,
  configuredStandardProjectCodes,
  createRecognitionProjectConfiguration,
  excludeConfiguredStandardItems,
  filterConfigurationRows,
  queryRecognitionProjectConfigurations,
  querySelectableStandardItems,
  rowDisplayName,
  setRecognitionProjectConfigurationEnabled,
  toConfigurationStatusFilterOptions,
  toSelectableOptionGroups,
  toSelectableStandardItems,
  updateRecognitionDuration,
  type ConfigurationStatusValue,
  type RecognitionProjectConfigurationRow,
  type RecognitionProjectFilter,
  type SelectableStandardItem,
} from './recognitionProjectsApi'
import { readTrustedOrganizationCode, useTrustedOrganizationScope } from './recognitionProjectsOrganizationScope'
import './RecognitionProjects.css'

type ModalState =
  | { kind: 'none' }
  | { kind: 'create' }
  /** 修改可互认时间弹窗只持目标配置标识，行数据每次渲染都按当前行集重取（见 `durationRow`）。 */
  | { kind: 'duration'; configurationId: string }
  /** 启停确认弹窗只持目标配置标识，行数据与目标状态每次渲染都按当前行集重取（见 `toggleRow`）。 */
  | { kind: 'toggle'; configurationId: string }

/**
 * 读取失败态。`refreshAfterWrite` 表示写入**已经成功**、随后的重载失败，
 * 展示与阻断规则必须与读取失败一致，但不能改判为写入失败。
 */
type LoadFailure = null | 'load' | 'refreshAfterWrite'

/** 配置状态 Tag：色板取自适配层出口，文案由调用方给出（服务端契约文本优先）。 */
function configurationStatusTag(value: ConfigurationStatusValue | null, text: string) {
  return <Tag color={configurationStatusTagColor(value)}>{text}</Tag>
}

/** 表格行 = 查询交付的行模型 + 仅在页面内使用的稳定行标识（不作为业务字段提交或展示）。 */
interface TableRow extends RecognitionProjectConfigurationRow {
  rowIdentity: string
}

export function RecognitionProjects() {
  const client = useApiClientContext()
  const trustedOrganization = useTrustedOrganizationScope()
  const trustedOrganizationCode = trustedOrganization.kind === 'ready' ? trustedOrganization.organizationCode : null

  const [rows, setRows] = useState<RecognitionProjectConfigurationRow[]>([])
  /**
   * 标准目录在页面侧的形态是**已摊平的视图模型**（`SelectableStandardItem[]`，由适配层摊平）。
   * 生成读模型 `EffectiveMedicalStandardCatalogReadModel` 只作为读取响应经过 `load`，
   * 不进页面状态：契约形状的变化留在适配层吸收，页面只按视图模型渲染。
   */
  const [catalogItems, setCatalogItems] = useState<SelectableStandardItem[]>([])
  /**
   * 本次读取的标准目录是否已经成功返回。与 `catalogItems` 分开表达：
   * 服务端契约允许"读取成功但目录为空"，此时 `catalogItems` 为空数组属正常结果，
   * 不能与"还没读到"混为一谈，否则新增入口会永久禁用且既无提示也无重试入口。
   */
  const [catalogLoaded, setCatalogLoaded] = useState(false)
  const [loading, setLoading] = useState(false)
  const [loadFailure, setLoadFailure] = useState<LoadFailure>(null)
  const [filter, setFilter] = useState<RecognitionProjectFilter>(EMPTY_RECOGNITION_PROJECT_FILTER)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const [submitting, setSubmitting] = useState(false)
  /** 页面实际展示的组织；与可信组织不一致时说明组织变化尚未被确认。 */
  const [appliedOrganizationCode, setAppliedOrganizationCode] = useState<string | null>(trustedOrganizationCode)
  /** 用户对「组织变化」确认框点过取消的那一次可信组织编码；仅用于不自动重复弹窗。 */
  const [dismissedOrganizationCode, setDismissedOrganizationCode] = useState<string | null>(null)
  /**
   * 提交期组织复核失败的可见原因；`null` 表示提交期内没有发生过复核失败。
   *
   * 渲染期门控（写弹窗的 `writeGate`）在宿主重新签发 token 后必然重算，但「宿主已换凭证、
   * 页面尚未重渲染」这个窗口里它仍是上一帧的结论。提交期复核在这里留下一条可见原因，
   * 使用户看到「点了保存却什么都没发生」的原因；原因在组织重新对齐或用户主动关闭弹窗时清除。
   *
   * 用对象而不是字符串承载：连续两次内容相同的复核失败也必须各自触发一次重渲染
   * （字符串取值相等时 React 会跳过更新，弹窗内就不会出现原因）。
   */
  const [organizationRecheckFailure, setOrganizationRecheckFailure] = useState<{ reason: string } | null>(null)

  /**
   * 在途读取的作废机制：**按序号丢弃迟到响应，不取消在途请求**。这是一条已核对的规范边界，
   * 不是可以就地补上的实现缺口。
   *
   * 依据：生成客户端与方法层都没有取消入口——`@microsoft/kiota-abstractions` 的
   * `RequestConfiguration<T>` 只有 `headers` / `options` / `queryParameters`，没有 `AbortSignal`
   * （`RecognitionProjectConfigurationListQueryRequestBuilder.post`、`EnumMetadata` 等同形），
   * `kiota-abstractions` 与 `kiota-http-fetchlibrary` 的类型与实现里都不存在 `AbortSignal`/`signal`，
   * 适配层（`@dy/auth` 的 `createAuthenticatedAdapter`）也没有暴露取消能力。
   * 页面只能把生成方法原样调用，因此**无法把取消信号透传到查询或写请求**；按 Frontend API Client
   * 第 2、5 节，页面不得为此自建底层 HTTP 调用或新增拦截器。
   *
   * 由此产生的行为：旧请求仍会在网络上跑完（占用连接、服务端照常执行），但它**不写入任何状态**——
   * 序号在应用新组织、可信组织失效与每次新读取前推进，晚到的响应在 `load` 的序号判定处被丢弃
   * （可观察证据见用例「组织变化后迟到的旧组织响应不得污染新组织」与「可信组织缺失…不发起任何请求」）。
   * 该边界与 `useEnumMetadata` 的卸载守卫同一口径。
   */
  const requestIdRef = useRef(0)
  /**
   * `appliedOrganizationCode` 的**同步镜像**，只在一处（`applyOrganization`）与它一起赋值，
   * 不构成第二个状态源：两者永远表达同一个「页面已应用组织」。保留它的唯一用途是让
   * 「写在途期间组织被切换」这类跨越渲染的判定读到当前值（见 `submitWrite` 末尾的重载守卫）；
   * 它不再按可信组织是否可用来单独改写——那会让它与门控所看的 state 永久不一致，
   * 出现「按钮可点但提交无反应」。
   */
  const appliedOrganizationRef = useRef<string | null>(trustedOrganizationCode)
  const submittingRef = useRef(false)

  /**
   * 配置状态选项来自枚举元数据接口（服务端声明的中文），不可用时回退到适配层的兜底选项。
   * 收窄与兜底口径都由适配层持有（页面不再自建"取值 + 中文"这份业务事实）。
   *
   * 请求时机：只在**可信组织范围可用**时才读取。该接口本身不是组织范围数据（服务端只按 `enumName`
   * 返回进程级静态枚举表，见 `useEnumMetadata` 的缓存口径说明），但组织范围不可用时本页面整体
   * 渲染「组织范围不可用」阻断提示，不渲染任何需要枚举文案的区域，此时发起读取只会产生一次
   * 必定被丢弃的请求。因此以页面已有的可信组织判定作为请求门控，不新增状态源、也不改数据口径。
   */
  const statusOptions = useEnumMetadata('ConfigurationStatus', CONFIGURATION_STATUS_METADATA_FALLBACK, {
    enabled: trustedOrganizationCode !== null,
  })
  const statusFilterOptions = useMemo(() => toConfigurationStatusFilterOptions(statusOptions), [statusOptions])
  const statusSegmentedOptions = useMemo(
    () => [{ value: 'all' as const, label: '全部' }, ...statusFilterOptions],
    [statusFilterOptions],
  )
  /**
   * 启停动作标签：目标状态的标签取枚举元数据（不可用或尚未到达时回退到适配层动作文案出口）。
   * 展示文本直接取契约交付的 `row.configurationStatusText`（与元数据同源于枚举成员上的 `[Description]`），
   * 因此同屏并列不会出现两种中文；未登记取值不进入筛选器（后端该枚举集合封闭）。
   * 元数据在途期间用兜底文案（与后端同为「启用/停用」），到达后同一弹窗会更新为服务端文案。
   * 本函数只在渲染体内被直接调用（不向下传引用），无需 `useCallback` 包裹。
   */
  const actionLabel = (nextEnabled: boolean) => {
    const targetStatus = nextEnabled ? ENABLED_CONFIGURATION_STATUS : DISABLED_CONFIGURATION_STATUS
    return statusFilterOptions.find((option) => option.value === targetStatus)?.label ?? configurationStatusActionText(nextEnabled)
  }

  /**
   * 读取指定组织的配置与标准项目选择数据。
   * 调用方在应用新组织前已推进请求序号，因此迟到的旧组织响应不会覆盖新组织；
   * 同一组织的重载失败只保留已有数据并进入阻断态，不覆盖成空列表。
   */
  const load = useCallback(async (targetOrganizationCode: string, failureKind: Exclude<LoadFailure, null>) => {
    const query = buildConfigurationListQuery(targetOrganizationCode)
    if (query === null) {
      setLoading(false)
      setLoadFailure(failureKind)
      return
    }
    const requestId = ++requestIdRef.current
    setLoading(true)
    const results = await Promise.allSettled([
      queryRecognitionProjectConfigurations(client, query),
      querySelectableStandardItems(client),
    ])
    if (requestId !== requestIdRef.current) return

    const [configurations, standardCatalog] = results
    setLoading(false)
    if (configurations.status === 'fulfilled') setRows(configurations.value)
    // 读取响应在进入页面状态前先经适配层摊平为视图模型（见 `catalogItems` 的说明）。
    if (standardCatalog.status === 'fulfilled') setCatalogItems(toSelectableStandardItems(standardCatalog.value))
    // "已读到目录"按请求是否成功判定，与目录内容是否为空无关。
    setCatalogLoaded(standardCatalog.status === 'fulfilled')
    setLoadFailure(configurations.status === 'rejected' || standardCatalog.status === 'rejected' ? failureKind : null)
  }, [client])

  /**
   * 应用目标组织：让旧组织的在途请求失效，清空列表、选择数据、筛选与弹窗，
   * 然后由加载 effect 读取目标组织。目标组织读取失败时停留在阻断态，不回退复用旧组织数据。
   */
  const applyOrganization = useCallback((targetOrganizationCode: string) => {
    requestIdRef.current += 1
    appliedOrganizationRef.current = targetOrganizationCode
    setRows([])
    setCatalogItems([])
    setCatalogLoaded(false)
    setFilter(EMPTY_RECOGNITION_PROJECT_FILTER)
    setModal({ kind: 'none' })
    setLoadFailure(null)
    setDismissedOrganizationCode(null)
    // 页面已对齐到新组织：上一次提交期复核留下的原因不再成立。
    setOrganizationRecheckFailure(null)
    setAppliedOrganizationCode(targetOrganizationCode)
  }, [])

  /**
   * 修改可互认时间弹窗的目标行与启停弹窗的目标行：两个弹窗都**只持配置标识**，
   * 行数据每次渲染都按**当前行集**重取，因此后台刷新（重试读取、写后刷新）交付新值后，
   * 弹窗展示与表单回填取的都是当前行，不再停留在打开那一刻的行快照。
   * 行集里找不到该配置（配置已不在本组织视图中）时目标行为 `null`，调用方据此不渲染弹窗，
   * 避免对一个未知行提交写入。
   */
  const durationRow = modal.kind === 'duration'
    ? rows.find((candidate) => candidate.configurationId === modal.configurationId) ?? null
    : null
  const toggleRow = modal.kind === 'toggle'
    ? rows.find((candidate) => candidate.configurationId === modal.configurationId) ?? null
    : null
  /**
   * 启停弹窗的动作目标：按目标行的**当前**状态取反，与目标行在同一处派生。
   * 当前状态未知（`null`）时返回 `null`：无法判定动作目标，调用方不渲染弹窗。
   */
  const toggleNextEnabled = toggleRow === null || toggleRow.configurationStatus === null
    ? null
    : toggleRow.configurationStatus !== ENABLED_CONFIGURATION_STATUS
  /**
   * 「未保存内容」判定与**实际渲染**一致：新增弹窗与修改弹窗都按打开状态判定，但修改弹窗在
   * 目标行离开当前行集时并不渲染（见 `durationRow`），此时它不承载任何内容。若只按
   * `modal.kind === 'duration'` 判定，行集刷新掉该配置后会留下不实的未保存内容，
   * 组织变化会多弹一次确认框，而用户无法主动清除它。
   */
  const hasUnsavedContent = modal.kind === 'create' || durationRow !== null
  /** 宿主重新签发 token 后组织已变化，但页面仍展示上一个组织的配置。 */
  const organizationChanged = appliedOrganizationCode !== null
    && trustedOrganizationCode !== null
    && appliedOrganizationCode !== trustedOrganizationCode
  const switchPending = organizationChanged && hasUnsavedContent
  const switchConfirming = switchPending && dismissedOrganizationCode !== trustedOrganizationCode
  const switchDeferred = switchPending && dismissedOrganizationCode === trustedOrganizationCode

  useEffect(() => {
    if (trustedOrganizationCode === null) {
      // 可信组织缺失或失效：作废在途请求，不发起新请求、不退化为全局查询或硬编码组织；
      // 展示由「组织范围不可用」分支接管，因此这里只清理不可见状态，不改动渲染状态。
      // 注意这里**不动** `appliedOrganizationRef`：它只镜像 `appliedOrganizationCode`，
      // 单独把它置空会让它与门控所看的 state 永久不一致（「可用 → 不可用 → 同一组织」之后
      // 按钮可点而四条写路径静默失效）。作废在途请求由上面的序号推进完成。
      requestIdRef.current += 1
      return
    }
    if (appliedOrganizationCode === trustedOrganizationCode) return
    // 还有未保存内容时等待用户确认，不清理、不加载、也不回退到旧组织范围。
    if (appliedOrganizationCode !== null && hasUnsavedContent) return
    // 同样推迟到下一个宏任务：组织变化来自宿主重新签发 token，属于外部系统变化，
    // 不在 effect 内同步级联渲染状态。
    const task = window.setTimeout(() => {
      applyOrganization(trustedOrganizationCode)
    }, 0)
    return () => window.clearTimeout(task)
  }, [appliedOrganizationCode, applyOrganization, hasUnsavedContent, trustedOrganizationCode])

  useEffect(() => {
    if (appliedOrganizationCode === null || appliedOrganizationCode !== trustedOrganizationCode) return
    // 把读取推迟到下一个宏任务，避免在 effect 内同步级联渲染。
    const task = window.setTimeout(() => {
      void load(appliedOrganizationCode, 'load')
    }, 0)
    return () => window.clearTimeout(task)
  }, [appliedOrganizationCode, load, trustedOrganizationCode])

  const configuredCodes = useMemo(() => configuredStandardProjectCodes(rows), [rows])
  // 剔除已配置项目：摊平结果与剔除分开，目录读取只交付摊平后的候选集，排除集合随行集变化重算。
  const selectableItems = useMemo(
    () => excludeConfiguredStandardItems(catalogItems, configuredCodes),
    [catalogItems, configuredCodes],
  )
  const optionGroups = useMemo(() => toSelectableOptionGroups(selectableItems), [selectableItems])
  const visibleRows = useMemo(() => filterConfigurationRows(rows, filter), [filter, rows])
  /**
   * 表格数据源：为每行补一个稳定唯一的行标识。契约唯一键（`configurationId`）优先；
   * 缺失时用业务字段组合，仍不引入本次行集内的位置下标（同一行换位置不改变标识）。
   * 业务字段完全相同的多行在契约上不可区分，此时只在一组相同的行之间回退到出现序号，
   * 以保证表格 key 唯一、操作控件不串位；该序号不参与任何业务判定，也不作为字段提交或展示。
   */
  const tableRows = useMemo<TableRow[]>(() => {
    const occurrences = new Map<string, number>()
    return visibleRows.map((row) => {
      const businessIdentity = row.configurationId
        ?? [row.standardProjectCode, row.itemType, row.standardItemName, row.categoryName, row.groupName]
          .map((value) => value ?? '')
          .join('|')
      const occurrence = occurrences.get(businessIdentity) ?? 0
      occurrences.set(businessIdentity, occurrence + 1)
      return { ...row, rowIdentity: occurrence === 0 ? businessIdentity : `${businessIdentity}#${occurrence}` }
    })
  }, [visibleRows])

  const organizationReady = appliedOrganizationCode !== null && appliedOrganizationCode === trustedOrganizationCode
  /**
   * 提交期复核的原因只在它依据的事实仍然成立时生效：页面与可信组织重新对齐（现值又等于页面已应用组织）后，
   * 上一次的结论自动失效。这里按派生值失效，不用 effect 把它改回 `null`——那会在 effect 里同步写入状态。
   */
  const writeRecheckFailure = organizationReady ? null : organizationRecheckFailure?.reason ?? null

  /**
   * 提交写操作，两道防线各管一段：
   *
   * 第一道是渲染期门控（写弹窗的 `writeGate`，由 `organizationReady` 裁决），它只表达
   * **本帧渲染读到**的可信组织；第二道是这里的提交期复核——发请求前用
   * `readTrustedOrganizationCode()` 读**现值**，覆盖「宿主重新签发 token 到页面重渲染之间」
   * 这个窗口：宿主已换凭证、页面还是上一帧时第一道门控全绿，请求却会带着新凭证出去，可跨组织落库。
   *
   * 现值与页面已应用组织不一致、或读不到可信组织时**不发写请求**，并且不静默返回：
   * 把原因写进 `organizationRecheckFailure`（由 `writeGate` 传给三个写弹窗显示），
   * 弹窗与输入保留，用户能看到「点了保存但没有发请求」的原因。
   *
   * 通过复核后同步去重防止连续点击产生多次写请求；失败保留弹窗与输入且不重载。
   */
  const submitWrite = async (request: () => Promise<void>) => {
    const organizationCodeAtSubmit = readTrustedOrganizationCode()
    if (organizationCodeAtSubmit === null || organizationCodeAtSubmit !== appliedOrganizationRef.current) {
      setOrganizationRecheckFailure({ reason: organizationCodeAtSubmit === null
        ? '提交时读不到可信组织范围，本次写入已放弃。组织范围不可用期间不能维护互认项目配置，请从宿主登录后重新进入本页面。'
        : '宿主组织已变化，页面尚未对齐到新组织；提交时复核发现当前可信组织与页面已应用组织不一致，本次写入已放弃。请先放弃未保存内容并切换组织。' })
      return
    }
    if (submittingRef.current) return
    submittingRef.current = true
    setSubmitting(true)
    try {
      await request()
    } finally {
      submittingRef.current = false
      setSubmitting(false)
    }
    setModal({ kind: 'none' })
    // 写完成后若页面已经切到别的组织，就不再重载，避免把旧组织的写入结果刷进新组织视图。
    if (appliedOrganizationRef.current !== organizationCodeAtSubmit) return
    await load(organizationCodeAtSubmit, 'refreshAfterWrite')
  }

  /** 关闭写弹窗：同时清掉上一次提交期复核留下的原因，避免它跟随到下一次打开。 */
  const closeWriteModal = useCallback(() => {
    setModal({ kind: 'none' })
    setOrganizationRecheckFailure(null)
  }, [])

  const canWrite = organizationReady && loadFailure === null && !loading && !submitting
  /**
   * 标准项目选择数据是否可用于新增：一次读取里配置与标准目录同时发起，两者都成功才允许新增。
   * 就绪与否按**本次读取是否成功**判定（`catalogLoaded`），而不是按目录内容是否为空：
   * 首帧与切换组织后的窗口里读取尚未成功，`rows=[]`/`catalogItems=[]` 若被旧口径判成可写，
   * 新增弹窗就会把「还没读到目录」误报成「没有可新增的标准项目」；而"读取成功但目录为空"是正常结果，
   * 此时入口必须可用、由弹窗提示没有可新增项，否则入口会永久禁用且无提示、无重试入口。
   * `loading` 覆盖同组织重载在途的窗口，因此这里同时判定读取状态与数据。
   * 目录只影响新增：修改可互认时间与启停不依赖目录，仍只按 `canWrite` 判定。
   */
  const catalogReady = organizationReady && !loading && loadFailure === null && catalogLoaded
  /**
   * 写弹窗门控：与 `canWrite` 同源（组织对齐、读取未失败、读取不在途、无在途提交），
   * 由弹窗禁用确定按钮并给出与页面顶部提示同口径的原因；「写请求在途」交给弹窗的提交反馈表达，
   * 因此弹窗内不再重复一条暂停说明。`organizationRecheckFailure` 是提交期复核留下的原因，
   * 与「组织未对齐」同属组织类阻断，在弹窗内优先于读取类原因显示。
   */
  const writeGate: RecognitionProjectWriteGate = { organizationReady, canWrite, loadFailure, organizationRecheckFailure: writeRecheckFailure }
  /** 是否存在生效的本地筛选：用于把「本组织没有配置」与「筛选后没有匹配项」两种空态分开。 */
  const filtering = filter.code.trim().length > 0 || filter.status !== 'all'
  const retry = () => {
    if (appliedOrganizationCode === null) return
    void load(appliedOrganizationCode, loadFailure ?? 'load')
  }

  const columns: TableColumnsType<TableRow> = [
    { title: '编码', dataIndex: 'standardProjectCode', width: 170, render: (value: string | null) => value ?? '—' },
    { title: '名称', dataIndex: 'standardItemName', width: 180, render: (value: string | null) => value ?? '—' },
    { title: '类型', width: 80, render: (_, row) => row.itemTypeText },
    { title: '分类', dataIndex: 'categoryName', width: 150, render: (value: string | null) => value ?? '—' },
    { title: '分组', dataIndex: 'groupName', width: 150, render: (value: string | null) => value ?? '—' },
    {
      title: '可互认时间（天）',
      dataIndex: 'recognitionDurationDays',
      width: 140,
      className: 'recognition-projects-number',
      render: (value: number | null) => value ?? '—',
    },
    { title: '配置状态', width: 100, render: (_, row) => configurationStatusTag(row.configurationStatus, row.configurationStatusText) },
    { title: '目录停用原因', dataIndex: 'unavailableReason', width: 170, ellipsis: true, render: (value: string | null) => value ?? '—' },
    {
      title: '操作',
      width: 96,
      fixed: 'right',
      render: (_, row) => {
        const writable = canWrite && row.configurationId !== null && row.configurationStatus !== null
        const stopsConfiguration = row.configurationStatus === ENABLED_CONFIGURATION_STATUS
        // 动作文案与状态文案都从同一个状态标签出口取（元数据可用时为服务端声明），页面不再自拼「状态 → 中文」。
        const actionText = actionLabel(!stopsConfiguration)
        const statusText = row.configurationStatusText
        return <Space size={2}>
          <Tooltip title='修改可互认时间'>
            <Button
              type='text'
              size='small'
              icon={<EditOutlined />}
              aria-label={`修改可互认时间：${rowDisplayName(row)}`}
              disabled={!writable}
              onClick={() => {
                // 只把配置标识交给弹窗：弹窗渲染时按当前行集重取该行，不持有打开那一刻的行快照。
                if (row.configurationId === null) return
                setModal({ kind: 'duration', configurationId: row.configurationId })
              }}
            />
          </Tooltip>
          <Tooltip title={row.configurationStatus === null ? `配置状态${statusText}` : `${actionText}配置`}>
            <Button
              type='text'
              size='small'
              danger={stopsConfiguration}
              icon={stopsConfiguration ? <StopOutlined /> : <CheckCircleOutlined />}
              aria-label={`${actionText}配置：${rowDisplayName(row)}（当前${statusText}）`}
              disabled={!writable}
              onClick={() => {
                // 只把配置标识交给弹窗：弹窗渲染时按当前行集重取该行与它的当前状态，
                // 不持有打开那一刻的行快照，也不持有打开那一刻的目标状态。
                if (row.configurationId === null) return
                setModal({ kind: 'toggle', configurationId: row.configurationId })
              }}
            />
          </Tooltip>
        </Space>
      },
    },
  ]

  if (trustedOrganizationCode === null) {
    return <div className='recognition-projects-page'>
      <div className='recognition-projects-header'>
        <div><Typography.Title level={2}>互认项目</Typography.Title><Typography.Text type='secondary'>标准项目互认配置</Typography.Text></div>
      </div>
      <Alert
        type='warning'
        showIcon
        title='组织范围不可用'
        description='当前无法从登录凭证确定可信组织范围，因此不能读取或维护互认项目配置。请从宿主登录后重新进入本页面。'
      />
    </div>
  }

  return <div className='recognition-projects-page'>
    <div className='recognition-projects-header'>
      <div><Typography.Title level={2}>互认项目</Typography.Title><Typography.Text type='secondary'>标准项目互认配置</Typography.Text></div>
      <Space wrap>
        <Typography.Text type='secondary'>组织编码：{appliedOrganizationCode ?? '—'}</Typography.Text>
        <Tooltip title='重新读取当前组织的配置与标准项目'>
          <Button icon={<ReloadOutlined />} aria-label='重新读取互认配置' loading={loading} disabled={loading || !organizationReady} onClick={retry} />
        </Tooltip>
        <Button type='primary' icon={<PlusOutlined />} disabled={!canWrite || !catalogReady} onClick={() => setModal({ kind: 'create' })}>新增配置</Button>
      </Space>
    </div>

    {switchDeferred ? <Alert
      type='info'
      showIcon
      title='宿主组织已变化，尚有未保存内容'
      description={`宿主已把组织切换为 ${trustedOrganizationCode}，当前页面仍显示组织 ${appliedOrganizationCode} 的配置，维护操作已暂停。放弃未保存内容后即可切换。`}
      action={<Button size='small' onClick={() => setDismissedOrganizationCode(null)}>放弃并切换</Button>}
    /> : null}

    {loadFailure === null ? null : <Alert
      type='warning'
      showIcon
      title={loadFailure === 'refreshAfterWrite' ? '写入已成功，数据待刷新' : '互认配置数据待刷新'}
      description={loadFailure === 'refreshAfterWrite'
        ? '写入已经成功，但重新读取配置与标准项目失败；已有数据保留，维护操作暂不可用。'
        : '读取失败，已有数据保留；维护操作暂不可用，可重试读取。'}
      action={<Button size='small' onClick={retry}>重试</Button>}
    />}

    <div className='recognition-projects-filters'>
      <Input
        placeholder='标准项目编码'
        aria-label='标准项目编码筛选'
        autoComplete='off'
        allowClear
        value={filter.code}
        onChange={(event) => setFilter((previous) => ({ ...previous, code: event.target.value }))}
      />
      <Segmented<RecognitionProjectFilter['status']>
        aria-label='配置状态筛选'
        value={filter.status}
        options={statusSegmentedOptions}
        onChange={(status) => setFilter((previous) => ({ ...previous, status }))}
      />
      <Button icon={<ReloadOutlined />} aria-label='重置编码与状态筛选' onClick={() => setFilter(EMPTY_RECOGNITION_PROJECT_FILTER)}>重置</Button>
    </div>

    <Table
      // 三者皆空的行在列表里可能同时出现多行：业务字段组合完全相同时退回出现序号补齐唯一标识，
      // 避免使用 antd 已弃用的 `rowKey` 索引参数（该参数不保证按预期工作），也避免这些行的操作控件串位。
      rowKey='rowIdentity'
      size='small'
      columns={columns}
      dataSource={tableRows}
      loading={loading && rows.length === 0}
      scroll={{ x: 1300 }}
      pagination={false}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          // 两种空态事实不同：本组织确实没有配置，与本地筛选把这些配置全滤掉了。
          description={rows.length === 0 ? '当前组织没有互认项目配置' : '没有匹配的配置'}
        />,
      }}
    />
    <Typography.Text type='secondary' className='recognition-projects-summary'>
      本组织共 {rows.length} 项配置{filtering ? `，筛选后 ${visibleRows.length} 项` : ''}
    </Typography.Text>

    {modal.kind === 'create' ? <CreateConfigurationModal
      open
      optionGroups={optionGroups}
      catalogReady={catalogReady}
      writeGate={writeGate}
      onCancel={closeWriteModal}
      submitting={submitting}
      onSubmit={(values) => {
        const request = buildCreateConfigurationRequest(values.standardProjectCode ?? '', values.recognitionDurationDays ?? '')
        if (request === null) return Promise.resolve()
        return submitWrite(() => createRecognitionProjectConfiguration(client, request))
      }}
    /> : null}

    {durationRow === null ? null : <DurationModal
      open
      row={durationRow}
      writeGate={writeGate}
      onCancel={closeWriteModal}
      submitting={submitting}
      onSubmit={(values) => {
        const request = buildDurationUpdatePayload(durationRow.configurationId, values.recognitionDurationDays ?? '')
        if (request === null) return Promise.resolve()
        return submitWrite(() => updateRecognitionDuration(client, request))
      }}
    />}

    {toggleRow === null || toggleNextEnabled === null ? null : <ToggleModal
      open
      row={toggleRow}
      nextEnabled={toggleNextEnabled}
      actionText={actionLabel(toggleNextEnabled)}
      writeGate={writeGate}
      onCancel={closeWriteModal}
      submitting={submitting}
      onSubmit={() => {
        const configurationId = toggleRow.configurationId
        if (configurationId === null) return Promise.resolve()
        return submitWrite(
          () => setRecognitionProjectConfigurationEnabled(client, { id: configurationId, enabled: toggleNextEnabled }),
        )
      }}
    />}

    {switchConfirming ? <Modal
      title='切换组织将放弃未保存内容'
      open
      onCancel={() => setDismissedOrganizationCode(trustedOrganizationCode)}
      okText='放弃并切换'
      okButtonProps={{ danger: true }}
      onOk={() => {
        if (trustedOrganizationCode !== null) applyOrganization(trustedOrganizationCode)
      }}
      destroyOnHidden
    >
      <Typography.Paragraph>
        宿主已把当前组织切换为 <Typography.Text strong>{trustedOrganizationCode}</Typography.Text>，但新增或修改弹窗中还有未保存的内容。
        继续切换会放弃这些内容，并清空当前组织的列表、筛选和弹窗状态。
      </Typography.Paragraph>
      <Typography.Paragraph type='secondary' style={{ marginBottom: 0 }}>选择取消可保留当前内容，之后再决定何时切换。</Typography.Paragraph>
    </Modal> : null}
  </div>
}
