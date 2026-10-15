import {
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import {
  Alert,
  Button,
  Empty,
  Input,
  Modal,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import { ScopeUnavailablePanel } from '../../components/ScopeUnavailablePanel'
import { useEnumMetadata } from '../../hooks/useEnumMetadata'
import {
  isCategoryHighlighted,
  isGroupHighlighted,
  isItemTypeHighlighted,
  type RecognitionProjectHighlight,
} from './recognitionProjectsHighlight'
import { RecognitionProjectDurationCell, RecognitionProjectToggleCell } from './RecognitionProjectRow'
import { RecognitionProjectBatchCreateModal, ToggleModal } from './RecognitionProjectModals'
import { type RecognitionProjectWriteGate } from './recognitionProjectWriteGate'
import { readFilterFromLocation, syncFilterToLocation } from './recognitionProjectsFilterUrl'
import {
  EMPTY_RECOGNITION_PROJECT_FILTER,
  CONFIGURATION_STATUS_METADATA_FALLBACK,
  DISABLED_CONFIGURATION_STATUS,
  ENABLED_CONFIGURATION_STATUS,
  RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS,
  buildBatchCreateConfigurationRequest,
  buildConfigurationListQuery,
  buildDurationUpdatePayload,
  buildRecognitionProjectExportDownloadName,
  buildRecognitionProjectExportPayload,
  configurationStatusActionText,
  countColumnRows,
  createRecognitionProjectConfigurations,
  exportRecognitionProjectConfigurations,
  filterRecognitionProjectColumnView,
  queryRecognitionProjectConfigurations,
  querySelectableStandardItems,
  recognitionProjectRowBadge,
  rowDisplayName,
  setRecognitionProjectConfigurationEnabled,
  toConfigurationStatusFilterOptions,
  toRecognitionProjectColumnView,
  toSelectableColumnView,
  toSelectableStandardItems,
  updateRecognitionDuration,
  type RecognitionProjectColumnRow,
  type RecognitionProjectColumnView,
  type RecognitionProjectConfigurationRow,
  type RecognitionProjectFilter,
  type MedicalItemTypeValue,
  type SelectableStandardItem,
} from './recognitionProjectsApi'
import { readTrustedOrganizationCode, useTrustedOrganizationScope } from './recognitionProjectsOrganizationScope'
import { saveExportWorkbook } from '../../shared/exportFileDownload'
import './RecognitionProjects.css'

type ModalState =
  | { kind: 'none' }
  | { kind: 'create' }
  /** 启停确认弹窗只持目标配置标识，行数据与目标状态每次渲染都按当前列视图重取（见 `toggleRow`）。 */
  | { kind: 'toggle'; configurationId: string }

/**
 * 读取失败态。`refreshAfterWrite` 表示写入**已经成功**、随后的重载失败，
 * 展示与阻断规则必须与读取失败一致，但不能改判为写入失败。
 */
type LoadFailure = null | 'load' | 'refreshAfterWrite'

/** 层级单元格的样式类：高亮类施加在单元格（td）上，格内按钮只承载点击与可访问名称。 */
function levelCellClassName(highlighted: boolean): string {
  return highlighted
    ? 'recognition-projects-level-cell recognition-projects-level-highlighted'
    : 'recognition-projects-level-cell'
}

/** 项目列单元格的样式类：所属分组格被高亮时它一并高亮（下级链路的最后一层）。 */
function itemCellClassName(highlighted: boolean): string {
  return highlighted
    ? 'recognition-projects-item-cell recognition-projects-level-highlighted'
    : 'recognition-projects-item-cell'
}

/**
 * 列视图里的全部项目行，按渲染顺序摊平。两个写界面的目标行查找都基于它，查找范围由调用方决定：
 * 启停弹窗查整份列视图，行内编辑查裁剪后的可见列视图。
 */
function flattenColumnRows(view: RecognitionProjectColumnView): RecognitionProjectColumnRow[] {
  return view.flatMap((itemType) =>
    itemType.categories.flatMap((category) => category.groups.flatMap((group) => group.rows)),
  )
}

/**
 * 列表表格行：每个项目行一行，层级三列的跨行信息（`rowSpan` 取值与是否首行）随行携带。
 * antd Table 的行跨越按项目行计数，与列视图的分组结构在这里换算一次，列定义因此只读行字段。
 */
interface RecognitionProjectTableRow {
  row: RecognitionProjectColumnRow
  itemType: MedicalItemTypeValue | null
  itemTypeText: string
  categoryName: string
  groupName: string
  typeFirst: boolean
  typeSpan: number
  categoryFirst: boolean
  categorySpan: number
  groupFirst: boolean
  groupSpan: number
}

/** 把列视图摊平成表格行：行序即层级展示顺序，空分组不产生行（裁剪后的列视图不含空层级）。 */
function toRecognitionProjectTableRows(view: RecognitionProjectColumnView): RecognitionProjectTableRow[] {
  const rows: RecognitionProjectTableRow[] = []
  for (const itemType of view) {
    const typeSpan = itemType.categories.reduce(
      (total, category) => total + category.groups.reduce((groupTotal, group) => groupTotal + group.rows.length, 0),
      0,
    )
    let typeFirst = true
    for (const category of itemType.categories) {
      const categorySpan = category.groups.reduce((total, group) => total + group.rows.length, 0)
      let categoryFirst = true
      for (const group of category.groups) {
        let groupFirst = true
        for (const row of group.rows) {
          rows.push({
            row,
            itemType: itemType.itemType,
            itemTypeText: itemType.itemTypeText,
            categoryName: category.categoryName,
            groupName: group.groupName,
            typeFirst,
            typeSpan,
            categoryFirst,
            categorySpan,
            groupFirst,
            groupSpan: group.rows.length,
          })
          typeFirst = false
          categoryFirst = false
          groupFirst = false
        }
      }
    }
  }
  return rows
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
  /** 弹窗数据源（可新增标准项目）的读取是否在途；与页面列表的 loading 相互独立。 */
  const [selectableLoading, setSelectableLoading] = useState(false)
  /**
   * 页面读取是否在途。初值为 `true`：页面一挂载就必然排一次读取，这一帧"还没发起"仍属未就绪，
   * 否则新增入口会在数据落地前先放行一瞬。
   */
  const [loading, setLoading] = useState(true)
  const [loadFailure, setLoadFailure] = useState<LoadFailure>(null)
  /**
   * 已生效筛选。初值取自地址栏查询参数（S2-D27）：带条件的链接进入后列表就按该条件裁剪，
   * 解析只认 `code` 与 `status` 两个参数，取值不可识别时按该条件不生效处理。
   */
  const [filter, setFilter] = useState<RecognitionProjectFilter>(() => readFilterFromLocation())
  /**
   * 关键字条件的输入原文。它是**草稿**，与生效筛选分开：输入过程不改变列表，
   * 只有提交动作（查询按钮或输入框内回车）才把原文写进 `filter.keyword` 并作为查询条件下推服务端（S2-D21、S2-D29）。
   * 分开持有是为了让「已输入但未提交」与「已生效」成为两个可分辨的事实，
   * 页脚计数只按生效条件计算，不会跟着输入逐字跳动。初值取路由地址上的已生效关键字，
   * 使带条件的链接进入后输入框与列表指向同一条件。
   */
  const [keywordDraft, setKeywordDraft] = useState(() => readFilterFromLocation().keyword)
  const [modal, setModal] = useState<ModalState>({ kind: 'none' })
  const [submitting, setSubmitting] = useState(false)
  /** 导出是否在途：按钮进入 loading 形态；导出是只读链，不参与既有写门控。 */
  const [exporting, setExporting] = useState(false)
  /** 被点中的层级格；`null` 表示没有高亮。高亮只标识位置，不携带任何写操作。 */
  const [highlight, setHighlight] = useState<RecognitionProjectHighlight | null>(null)
  /**
   * 行内可互认时间编辑态**只持行标识**：目标行每次渲染都在可见列视图里重取，
   * 因此后台刷新交付新值后编辑态回填的是当前行取值，也不停留在打开编辑那一刻的行快照。
   */
  const [durationEdit, setDurationEdit] = useState<{ rowIdentity: string } | null>(null)
  /**
   * 批量新增弹窗的选择与统一可互认时间由页面持有。
   *
   * 放在页面而不是弹窗内：弹窗按 `modal.kind` 条件渲染，关闭即卸载；若选择随弹窗一起消失，
   * 「提交失败保留选择」就只能靠弹窗不卸载来保证。由页面持有后，失败保留是天然的，
   * 关闭与组织变化时的清空也集中在同一处（见 `openBatchCreate` 与 `applyOrganization`）。
   */
  const [batchSelection, setBatchSelection] = useState<ReadonlySet<string>>(new Set())
  const [batchDurationInput, setBatchDurationInput] = useState('')
  /**
   * 上一次重读从选择中剔除的条数（S2-D25）。只用于弹窗内的一句可见说明，不参与任何业务判定；
   * 计数在每次打开弹窗与手动改变选择时清零，避免把上一次重读的结论留到下一次操作上。
   */
  const [prunedSelectionCount, setPrunedSelectionCount] = useState(0)
  /** 弹窗当前生效的查询关键字；空串表示列出全部可新增项目。 */
  const [batchKeyword, setBatchKeyword] = useState('')
  /**
   * `batchSelection` 的同步镜像：读取完成后的收敛判定发生在异步回调里，读不到那一帧的 state；
   * 由 effect 在每次选择变化后同步，因此它永远等于最近一次渲染的选择，不构成第二个事实来源。
   */
  const batchSelectionRef = useRef<ReadonlySet<string>>(new Set())
  useEffect(() => {
    batchSelectionRef.current = batchSelection
  }, [batchSelection])
  /**
   * 按重读得到的最新可新增集合收敛选择：已不在集合中的编码从选择里移除，并记录剔除条数。
   * 剔除不静默进行——用户勾选的数量与提交的数量必须对得上，否则整批拒绝时无法自查。
   */
  const pruneBatchSelection = useCallback((selectableCodes: ReadonlySet<string>) => {
    if (batchSelectionRef.current.size === 0) return
    const kept = new Set([...batchSelectionRef.current].filter((code) => selectableCodes.has(code)))
    if (kept.size === batchSelectionRef.current.size) return
    setBatchSelection(kept)
    setPrunedSelectionCount(batchSelectionRef.current.size - kept.size)
  }, [])
  /**
   * 已生效筛选的同步镜像：`load` 在异步回调里要读"当前生效关键字"，而 `load` 自身不随筛选重建
   * （见它的说明），因此这里经 ref 读现值；由 effect 在每次筛选变化后同步，不构成第二个事实来源。
   */
  const filterRef = useRef<RecognitionProjectFilter>(EMPTY_RECOGNITION_PROJECT_FILTER)
  useEffect(() => {
    filterRef.current = filter
  }, [filter])
  /** 页面实际展示的组织；与可信组织不一致时说明组织变化尚未被确认。 */
  const [appliedOrganizationCode, setAppliedOrganizationCode] = useState<string | null>(trustedOrganizationCode)
  /** 用户对「组织变化」确认框点过取消的那一次可信组织编码；仅用于不自动重复弹窗。 */
  const [dismissedOrganizationCode, setDismissedOrganizationCode] = useState<string | null>(null)
  /**
   * 提交期组织复核失败的可见原因；`null` 表示提交期内没有发生过复核失败。
   *
   * 渲染期门控（写界面的 `writeGate`）在宿主重新签发 token 后必然重算，但「宿主已换凭证、
   * 页面尚未重渲染」这个窗口里它仍是上一帧的结论。提交期复核在这里留下一条可见原因，
   * 使用户看到「点了保存却什么都没发生」的原因；原因在组织重新一致或用户主动关闭写界面时清除。
   *
   * 用对象而不是字符串承载：连续两次内容相同的复核失败也必须各自触发一次重渲染
   * （字符串取值相等时 React 会跳过更新，写界面内就不会出现原因）。
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
  /** 弹窗数据源读取的请求序号；与页面读取分开，两条读取相互独立、互不作废对方的结果。 */
  const selectableRequestIdRef = useRef(0)
  /**
   * `appliedOrganizationCode` 的**同步镜像**，只在一处（`applyOrganization`）与它一起赋值，
   * 不构成第二个状态源：两者永远表达同一个「页面已应用组织」。保留它的唯一用途是让
   * 「写在途期间组织被切换」这类跨越渲染的判定读到当前值（见 `submitWrite` 末尾的重载守卫）；
   * 它不再按可信组织是否可用来单独改写——那会让它与门控所看的 state 永久不一致，
   * 出现「按钮可点但提交无反应」。
   */
  const appliedOrganizationRef = useRef<string | null>(trustedOrganizationCode)
  const submittingRef = useRef(false)
  /** `exporting` 的同步镜像：同一动作内的再次点击在状态落地前就由它拦下（与 `submittingRef` 同一口径）。 */
  const exportingRef = useRef(false)

  /**
   * 配置状态选项来自枚举元数据接口（服务端声明的中文），不可用时回退到适配层的兜底选项。
   * 收窄与兜底口径都由适配层持有（页面不再自建"取值 + 中文"这份业务事实）。
   *
   * 请求时机：只在**可信组织范围可用**时才读取。该接口本身不是组织范围数据（服务端只按 `enumName`
   * 返回进程级静态枚举表，见 `useEnumMetadata` 的缓存口径说明），但可信组织或医院不可用时本页面
   * 整体渲染统一阻断面板，不渲染任何需要枚举文案的区域，此时发起读取只会产生一次
   * 必定被丢弃的请求。因此以页面已有的可信组织判定作为请求门控，不新增状态源、也不改数据口径。
   */
  const statusOptions = useEnumMetadata('ConfigurationStatus', CONFIGURATION_STATUS_METADATA_FALLBACK, {
    enabled: trustedOrganizationCode !== null,
  })
  const statusFilterOptions = useMemo(() => toConfigurationStatusFilterOptions(statusOptions), [statusOptions])
  const statusTabOptions = useMemo(
    () => [{ value: 'all' as const, label: '全部' }, ...statusFilterOptions],
    [statusFilterOptions],
  )
  /**
   * 启停动作标签：目标状态的标签取枚举元数据（不可用或尚未到达时回退到适配层动作文案出口）。
   * 展示文本直接取契约交付的 `row.configurationStatusText`（与元数据同源于枚举成员上的 `[Description]`），
   * 因此同屏并列不会出现两种中文；未登记取值不进入筛选器（后端该枚举集合封闭）。
   * 元数据在途期间用兜底文案（与后端同为「启用/停用」），到达后同一界面会更新为服务端文案。
   * 本函数只在渲染体内被直接调用（不向下传引用），无需 `useCallback` 包裹。
   */
  const actionLabel = (nextEnabled: boolean) => {
    const targetStatus = nextEnabled ? ENABLED_CONFIGURATION_STATUS : DISABLED_CONFIGURATION_STATUS
    return statusFilterOptions.find((option) => option.value === targetStatus)?.label ?? configurationStatusActionText(nextEnabled)
  }

  /**
   * 读取指定组织的配置列表。调用方在应用新组织前已推进请求序号，因此迟到的旧组织响应不会覆盖新组织；
   * 同一组织的重载失败只保留已有数据并进入阻断态，不覆盖成空列表。
   *
   * `pendingKeyword` 由搜索提交传入：它既是**本次查询条件**（下推服务端，S2-D29），读取成功后才写成生效筛选，
   * 因此列表里看到的结果一定来自这一次查询，不会先拿旧条件的结果凑一帧。
   * 读取失败时不写筛选，页面停在阻断态并保留旧条件，用户看到的是「这次搜索没生效」。
   *
   * 未传 `pendingKeyword` 时按**当前已生效关键字**查询（重载、重试、写后刷新都走这一口径）。
   * 关键字经 `filterRef` 读现值而不是闭包里的 `filter`：`load` 不随筛选变化重建，
   * 否则「配置状态切换」这类只改本地裁剪的动作会连带触发一次多余读取。
   *
   * 页面不再读取标准目录：行集的名称、类型、分类、分组由配置列表读模型实时带出（S2-D29），
   * 可新增标准项目由弹窗自己那次查询交付。页头不展示三级范围，页面读取只有配置列表 1 次（S2-D22）。
   */
  const load = useCallback(async (targetOrganizationCode: string, failureKind: Exclude<LoadFailure, null>, pendingKeyword?: string) => {
    const keyword = pendingKeyword ?? filterRef.current.keyword
    const query = buildConfigurationListQuery(targetOrganizationCode, { keyword, status: 'all' })
    if (query === null) {
      setLoading(false)
      setLoadFailure(failureKind)
      return
    }
    const requestId = ++requestIdRef.current
    setLoading(true)
    const result = await queryRecognitionProjectConfigurations(client, query)
      .then((value) => ({ ok: true as const, value }))
      .catch(() => ({ ok: false as const, value: null as null }))
    if (requestId !== requestIdRef.current) return

    setLoading(false)
    if (result.ok) {
      setRows(result.value)
      if (pendingKeyword !== undefined) {
        setFilter((previous) => (previous.keyword === pendingKeyword ? previous : { ...previous, keyword: pendingKeyword }))
        // 可见行集随之变化，编辑态一并清除（与 `changeFilter` 同一理由）。
        setDurationEdit(null)
      }
    }
    setLoadFailure(result.ok ? null : failureKind)
  }, [client])

  /**
   * 读取**可新增标准项目**（弹窗的数据源）。这是与页面列表相互独立的一次查询：服务端按
   * 「三级均启用的有效目录 − 本组织已配置」返回结果（S2-D29），因此页面不必持有全量配置，
   * 客户端也不再做差集。`keyword` 为空表示列出全部可新增项目。
   *
   * 收敛判定（S2-D25）就建立在这份结果上：只有它给出的是真实的可新增集合。
   *
   * 失败**不写页面级阻断态**：弹窗数据源读不到只让弹窗内提示「数据待刷新」并保留重读入口，
   * 页面列表与它的行内操作不受影响（两者是不同的数据、不同的失败面）。成功也不清页面级失败。
   * 请求序号与页面读取分开：两条读取相互独立，任一条在途都不该作废另一条的结果。
   */
  const loadSelectableItems = useCallback(async (keyword: string) => {
    const requestId = ++selectableRequestIdRef.current
    setSelectableLoading(true)
    const result = await querySelectableStandardItems(client, keyword)
      .then((catalog) => ({ ok: true as const, catalog }))
      .catch(() => ({ ok: false as const, catalog: null }))
    if (requestId !== selectableRequestIdRef.current) return

    setSelectableLoading(false)
    if (result.ok) {
      const items = toSelectableStandardItems(result.catalog)
      setCatalogItems(items)
      setCatalogLoaded(true)
      // 收敛判定只在**全量可新增集合**这次读取上做（S2-D25）：关键字搜索的结果是用户主动收窄的子集，
      // 不代表被搜掉的项目已不可新增；按它剔除会把仍然有效的勾选误删。
      if (keyword.trim().length === 0) pruneBatchSelection(new Set(items.map((item) => item.code)))
    } else {
      setCatalogLoaded(false)
    }
  }, [client, pruneBatchSelection])

  /**
   * 应用目标组织：让旧组织的在途请求失效，清空列表、选择数据、筛选（含编码输入原文）、
   * 高亮与写界面，然后由加载 effect 读取目标组织。目标组织读取失败时停留在阻断态，
   * 不回退复用旧组织数据。
   */
  const applyOrganization = useCallback((targetOrganizationCode: string) => {
    requestIdRef.current += 1
    // 弹窗数据源的在途读取一并作废：它属于旧组织，不能落在新组织的可选集合上。
    selectableRequestIdRef.current += 1
    appliedOrganizationRef.current = targetOrganizationCode
    setRows([])
    setCatalogItems([])
    setCatalogLoaded(false)
    setFilter(EMPTY_RECOGNITION_PROJECT_FILTER)
    setKeywordDraft('')
    setModal({ kind: 'none' })
    setDurationEdit(null)
    setHighlight(null)
    // 新增弹窗的选择、天数与关键字属于旧组织：切换后不得带到新组织的可选集合上。
    setBatchSelection(new Set())
    setBatchDurationInput('')
    setBatchKeyword('')
    setPrunedSelectionCount(0)
    setLoadFailure(null)
    setDismissedOrganizationCode(null)
    // 页面已应用新组织：上一次提交期复核留下的原因不再成立。
    setOrganizationRecheckFailure(null)
    // 目标组织的读取已排队未发起：这一帧仍属未就绪，新增入口不得先放行。
    setLoading(true)
    setAppliedOrganizationCode(targetOrganizationCode)
  }, [])

  const columnView = useMemo(() => toRecognitionProjectColumnView(rows, catalogItems), [rows, catalogItems])
  const visibleColumnView = useMemo(() => filterRecognitionProjectColumnView(columnView, filter), [columnView, filter])
  /** 列视图里的项目行总数；列表计数与空态文案都按它判定。 */
  const totalRowCount = useMemo(() => countColumnRows(columnView), [columnView])
  /** 裁剪后仍然可见的项目行总数。 */
  const visibleRowCount = useMemo(() => countColumnRows(visibleColumnView), [visibleColumnView])
  /** 列表表格行：行序与跨行信息由列视图换算，列定义只读行字段。 */
  const tableRows = useMemo(() => toRecognitionProjectTableRows(visibleColumnView), [visibleColumnView])

  /**
   * 启停弹窗的目标行：只持配置标识，行数据每次渲染都按**整份列视图**重取，
   * 因此后台刷新（重试读取、写后刷新）交付新值后，弹窗展示与动作目标取的都是当前行。
   * 查找范围与旧实现查当前行集一致：本地筛选不参与启停目标的判定。
   * 列视图里找不到该配置（配置已不在本组织视图中）时目标行为 `null`，调用方据此不渲染弹窗，
   * 避免对一个未知行提交写入。
   */
  const toggleRow = modal.kind === 'toggle'
    ? flattenColumnRows(columnView).find((candidate) => candidate.configurationId === modal.configurationId) ?? null
    : null
  /**
   * 行内编辑的目标行：只持行标识，且只在**可见列视图**里查找。编辑态必须在真正渲染出来的行里存在，
   * 否则本地筛选把该行藏起来之后，页面上留着看不见的编辑态，同时它还会让组织变化多弹一次确认框。
   * 找不到时不渲染编辑态（该行也不再承载未保存内容）。
   */
  const durationEditRow = durationEdit === null
    ? null
    : flattenColumnRows(visibleColumnView).find((candidate) => candidate.rowIdentity === durationEdit.rowIdentity) ?? null
  /**
   * 启停弹窗的动作目标：按目标行的**当前**状态取反，与目标行在同一处派生。
   * 当前状态未知（`null`）时返回 `null`：无法判定动作目标，调用方不渲染弹窗。
   */
  const toggleNextEnabled = toggleRow === null || toggleRow.configurationStatus === null
    ? null
    : toggleRow.configurationStatus !== ENABLED_CONFIGURATION_STATUS
  /**
   * 「未保存内容」判定与**实际渲染**一致：新增弹窗按打开状态判定，行内编辑按目标行是否仍在可见列视图中判定
   * （见 `durationEditRow`），目标行被筛掉或刷新掉时它不承载任何内容。若只按 `durationEdit` 判定，
   * 行集刷新掉该行后会留下不实的未保存内容，组织变化会多弹一次确认框，而用户无法主动清除它。
   */
  const hasUnsavedContent = modal.kind === 'create' || durationEditRow !== null
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
      // 展示由「可信组织或医院不可用」的统一阻断面板接管，因此这里只清理不可见状态，不改动渲染状态。
      // 注意这里**不动** `appliedOrganizationRef`：它只镜像 `appliedOrganizationCode`，
      // 单独把它置空会让它与门控所看的 state 永久不一致（「可用 → 不可用 → 同一组织」之后
      // 按钮可点而写路径静默失效）。作废在途请求由上面的序号推进完成。
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

  /**
   * 新增弹窗的可选集合列视图：`catalogItems` 已是服务端按「有效目录 − 本组织已配置」交付的结果
   * （S2-D29），客户端不再做差集，只把它按层级摊开。
   */
  const selectableColumnView = useMemo(() => toSelectableColumnView(catalogItems), [catalogItems])

  const organizationReady = appliedOrganizationCode !== null && appliedOrganizationCode === trustedOrganizationCode
  /**
   * 提交期复核的原因只在它依据的事实仍然成立时生效：页面与可信组织重新一致（现值又等于页面已应用组织）后，
   * 上一次的结论自动失效。这里按派生值失效，不用 effect 把它改回 `null`——那会在 effect 里同步写入状态。
   */
  const writeRecheckFailure = organizationReady ? null : organizationRecheckFailure?.reason ?? null

  /**
   * 提交写操作，两道防线各管一段：
   *
   * 第一道是渲染期门控（写界面的 `writeGate`，由 `organizationReady` 裁决），它只表达
   * **本帧渲染读到**的可信组织；第二道是这里的提交期复核——发请求前用
   * `readTrustedOrganizationCode()` 读**现值**，覆盖「宿主重新签发 token 到页面重渲染之间」
   * 这个窗口：宿主已换凭证、页面还是上一帧时第一道门控全绿，请求却会带着新凭证出去，可跨组织写入。
   *
   * 现值与页面已应用组织不一致、或读不到可信组织时**不发写请求**，并且不静默返回：
   * 把原因写进 `organizationRecheckFailure`（由 `writeGate` 传给弹窗与行内编辑器显示），
   * 写界面与输入保留，用户能看到「点了保存但没有发请求」的原因。
   *
   * 通过复核后同步去重防止连续点击产生多次写请求；失败保留写界面与输入且不重载。
   *
   * `exitSurface` 由调用方给出：请求成功后由它收起**发起这次写入的那个界面**（弹窗或行内编辑器），
   * 使「写完就收起」与「哪一种界面发起的」一致，页面不必按界面种类分支。
   */
  const submitWrite = async (request: () => Promise<void>, exitSurface: () => void) => {
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
    exitSurface()
    // 写完成后若页面已经切到别的组织，就不再重载，避免把旧组织的写入结果刷进新组织视图。
    if (appliedOrganizationRef.current !== organizationCodeAtSubmit) return
    await load(organizationCodeAtSubmit, 'refreshAfterWrite')
  }

  /** 关闭写弹窗：同时清掉上一次提交期复核留下的原因，避免它跟随到下一次打开。 */
  const closeWriteModal = useCallback(() => {
    setModal({ kind: 'none' })
    setOrganizationRecheckFailure(null)
  }, [])

  /**
   * 弹窗内的数据重读：按当前弹窗关键字再查一次可新增标准项目（S2-D25 的「重新读取」与 S2-D26 的搜索共用）。
   * 页面已应用组织缺失时不发起请求；请求本身不带组织入参——服务端只按可信上下文排除本组织的配置。
   */
  const reloadSelectableItems = useCallback((keyword: string) => {
    if (appliedOrganizationRef.current === null) return
    void loadSelectableItems(keyword)
  }, [loadSelectableItems])

  /**
   * 弹窗内的关键字搜索：先记录生效条件（弹窗据此区分「没有匹配项」与「没有可新增项」），
   * 再按它查询一次可新增项目。清空关键字等价于"列出全部可新增项目"。
   */
  const searchSelectableItems = useCallback((keyword: string) => {
    setBatchKeyword(keyword)
    reloadSelectableItems(keyword)
  }, [reloadSelectableItems])

  /**
   * 打开新增弹窗：清空上一次的选择、天数、关键字与剔除说明，并**立即查一次可新增项目**（S2-D25），
   * 使可选集合是打开那一刻的真实集合，而不是页面最近一次读取的快照。
   * 清空放在打开这一侧而不是关闭那一侧：写入成功与用户取消都会关闭弹窗，
   * 两条路径因此共用同一处复位，不需要各自记得清一次。
   */
  const openBatchCreate = useCallback(() => {
    setBatchSelection(new Set())
    setBatchDurationInput('')
    setPrunedSelectionCount(0)
    setBatchKeyword('')
    setOrganizationRecheckFailure(null)
    setModal({ kind: 'create' })
    reloadSelectableItems('')
  }, [reloadSelectableItems])

  /** 收起行内编辑，与 `closeWriteModal` 对称：清空编辑态与提交期复核原因。 */
  const closeDurationEdit = useCallback(() => {
    setDurationEdit(null)
    setOrganizationRecheckFailure(null)
  }, [])

  /**
   * 改变本地展示筛选。可见行集随之变化，因此编辑态一并清除：
   * 目标行可能被筛掉，筛选恢复后编辑态若"复活"，用户会看到一个自己没有打开过的输入框。
   */
  const changeFilter = useCallback((update: (previous: RecognitionProjectFilter) => RecognitionProjectFilter) => {
    setFilter(update)
    setDurationEdit(null)
  }, [])

  /**
   * 提交搜索：以输入原文作为**服务端查询条件**查一次配置列表（S2-D29），读取成功后把它应用为生效筛选。
   * 条件为空时提交等价于刷新——查询不带关键字，结果是本组织当前全部配置。
   * 读取失败时不写筛选，页面停在阻断态并保留旧条件。
   */
  const submitKeywordFilter = useCallback(() => {
    const targetOrganizationCode = appliedOrganizationRef.current
    if (targetOrganizationCode === null) return
    void load(targetOrganizationCode, loadFailure ?? 'load', keywordDraft)
  }, [load, loadFailure, keywordDraft])

  /**
   * 关键字输入框的输入原文变化。**清空动作**（输入框自带的清除入口，或手动删空）同时清掉生效条件并
   * 重读一次：关键字已下推服务端，只清状态不重读会让列表停在上一次关键字的结果上、
   * 而输入框与地址栏都已显示"无条件"；重读口径与"空条件提交等价于刷新"（S2-D24）一致。
   * 非空原文只更新草稿，等提交动作再进入查询。
   */
  const changeKeywordDraft = useCallback((value: string) => {
    setKeywordDraft(value)
    if (value !== '') return
    changeFilter((previous) => (previous.keyword === '' ? previous : { ...previous, keyword: '' }))
    const targetOrganizationCode = appliedOrganizationRef.current
    if (targetOrganizationCode !== null) void load(targetOrganizationCode, loadFailure ?? 'load', '')
  }, [changeFilter, load, loadFailure])

  /**
   * 已生效筛选与地址栏保持一致（S2-D27）：条件变化后回写地址栏，写入方式见 `syncFilterToLocation`
   * （替换当前历史项、失败静默降级）。依赖只取 `filter`，因此输入原文不参与，地址栏只反映真正生效的条件。
   */
  useEffect(() => {
    syncFilterToLocation(filter)
  }, [filter])

  /**
   * 发起项目导出。请求体只有当前组织与已生效关键字（配置状态页签档位不参与，导出与档位解耦）：
   * 组织在点击时刻取宿主认证上下文**现值**，并要求与页面已应用组织一致——两者不一致说明页面展示的
   * 还是上一个组织的列表（组织变化尚未应用），此时导出所取范围与页面展示不一致，由按钮禁用态与
   * 这里的复核共同保证零请求（与写路径的提交期复核同一窗口，导出侧的选择是整段放弃而不是换组织提交）。
   * 取得字节后经共享下载模块触发保存，下载名按发起时刻由前端给定；
   * 失败（含共享模块对 0 字节内容的契约错误）由宿主统一展示，本层只结束加载态并保留列表与筛选。
   */
  const runExport = useCallback(async () => {
    if (exportingRef.current) return
    const organizationCode = readTrustedOrganizationCode()
    if (organizationCode === null || organizationCode !== appliedOrganizationRef.current) return
    const payload = buildRecognitionProjectExportPayload(organizationCode, filterRef.current.keyword)
    if (payload === null) return
    exportingRef.current = true
    setExporting(true)
    try {
      const downloadName = buildRecognitionProjectExportDownloadName()
      const bytes = await exportRecognitionProjectConfigurations(client, payload)
      saveExportWorkbook(bytes, downloadName)
    } catch {
      // 导出失败不误报成功也不误刷新；页面不解析错误响应体、不新增提示层（与统计页导出同款恢复形态）。
    } finally {
      exportingRef.current = false
      setExporting(false)
    }
  }, [client])

  const canWrite = organizationReady && loadFailure === null && !loading && !submitting
  /**
   * 新增入口只按 `canWrite` 判定：页面读取成功即允许打开弹窗。原先"目录未读到就禁用"的前提已经不存在——
   * 可新增项目由弹窗自己那次查询交付（S2-D29），读取失败或为空都在弹窗内就地表达，
   * 不再用一个页面级的目录读取当前提去禁用入口。
   *
   * `catalogReady` 只给弹窗内区分「数据待刷新」与「确实没有可新增项」用，不参与入口门控。
   */
  const catalogReady = catalogLoaded
  /**
   * 写界面门控：与 `canWrite` 同源（组织一致、读取未失败、读取不在途、无在途提交），
   * 由写界面禁用提交并给出与页面顶部提示同口径的原因；「写请求在途」交给提交反馈表达，
   * 因此界面内不再重复一条暂停说明。`organizationRecheckFailure` 是提交期复核留下的原因，
   * 与「组织未一致」同属组织类阻断，在写界面内优先于读取类原因显示。
   */
  const writeGate: RecognitionProjectWriteGate = { organizationReady, canWrite, loadFailure, organizationRecheckFailure: writeRecheckFailure }
  /**
   * 导出按钮禁用只取与既有写门控同口径的两态：可信组织已变化且页面尚未应用（导出所取范围与页面展示
   * 不一致）、读取失败或写入后刷新失败（页面数据不可靠，恢复以重试为先），两态下均零请求。
   * 列表读取在途与行内编辑进行中**不**限制导出（导出是只读链，不产生写冲突，既有写门控不因导出改变）；
   * 导出在途的防重复由按钮 loading 与动作内复核共同承载。
   */
  const exportDisabled = organizationChanged || loadFailure !== null
  /** 是否存在生效的本地筛选：用于把「本组织没有配置」与「筛选后没有匹配项」两种空态分开。 */
  const filtering = filter.keyword.trim().length > 0 || filter.status !== 'all'
  const retry = () => {
    if (appliedOrganizationCode === null) return
    void load(appliedOrganizationCode, loadFailure ?? 'load')
  }

  if (trustedOrganizationCode === null) {
    return <div className='recognition-projects-page'>
      <ScopeUnavailablePanel
        title={
          <div className='recognition-projects-header'>
            <div><Typography.Title level={3}>互认项目</Typography.Title><Typography.Text type='secondary'>标准项目互认配置</Typography.Text></div>
          </div>
        }
      />
    </div>
  }

  return <div
    className='recognition-projects-page'
    // 点击页面其它区域清除高亮：层级格在自己的点击处理里阻止冒泡，因此这里不会立刻清掉刚设置的高亮。
    onClick={() => setHighlight(null)}
  >
    <div className='recognition-projects-header'>
      <div><Typography.Title level={3}>互认项目</Typography.Title><Typography.Text type='secondary'>标准项目互认配置</Typography.Text></div>
      {/* 新增入口只按 `canWrite` 门控：可新增集合由弹窗自己查询交付，读取失败在弹窗内表达。 */}
      <Button type='primary' icon={<PlusOutlined />} disabled={!canWrite} onClick={openBatchCreate}>新增互认项目</Button>
    </div>

    {/*
      页面级提示都放在常驻的 live region 里：容器先于内容存在，读取失败、写入成功待刷新这类
      异步出现的状态变化才读得到，而不是只在视觉上出现。
    */}
    <div className='recognition-projects-live' aria-live='polite'>
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
    </div>

    {/*
      查询区（S2-D21，2026-10-12 修订）：只承载关键字一个查询条件。关键字输入原文是草稿，
      提交动作（框内查询图标按钮或输入框内回车，两者都进 `submitKeywordFilter`）才把原文下推服务端。
      查询图标按钮显式给出可访问名称——`SearchOutlined` 自带英文 `aria-label`，它会参与按钮的
      名称计算，不声明就会读成「search 查询」。提交会触发重新读取（S2-D24），因此按钮在读取在途时
      进入 `loading`：图标换成旋转指示并禁用，「点了查询之后正在取数」这件事因此在用户刚点击的位置可见。
    */}
    <div className='recognition-projects-filters'>
      <Input
        placeholder='标准项目编码/名称/分类/分组关键字'
        aria-label='关键字筛选'
        autoComplete='off'
        allowClear
        suffix={<Button
          type='text'
          size='small'
          icon={<SearchOutlined />}
          aria-label='查询'
          autoInsertSpace={false}
          loading={loading}
          onClick={submitKeywordFilter}
        />}
        value={keywordDraft}
        onChange={(event) => changeKeywordDraft(event.target.value)}
        onPressEnter={() => submitKeywordFilter()}
      />
    </div>

    {/*
      配置状态页签（S2-D21）：贴在列表上沿，是「视图切换」而非查询条件——
      切换即生效、只在服务端结果上本地裁剪、不触发读取。
      页签行右侧承载项目导出入口：与页签同一工具行、随 DOM 顺序在页签之后（键盘焦点顺序随之）；
      请求体只有当前组织与已生效关键字（页签档位不参与导出），门控与在途形态见 `exportDisabled` 与 `runExport`。
    */}
    <div className='recognition-projects-status-toolbar'>
      <div role='group' aria-label='配置状态筛选'>
        <Tabs
          className='recognition-projects-status-tabs'
          activeKey={String(filter.status)}
          items={statusTabOptions.map((option) => ({ key: String(option.value), label: option.label }))}
          onChange={(key) => {
            const option = statusTabOptions.find((item) => String(item.value) === key)
            if (option) changeFilter((previous) => ({ ...previous, status: option.value }))
          }}
        />
      </div>
      <Button
        className='recognition-projects-export'
        aria-label='导出互认项目'
        disabled={exportDisabled || exporting}
        loading={exporting}
        onClick={() => { void runExport() }}
      >导出</Button>
    </div>

    {/*
      列表（S2-D16，2026-10-13 第二次修订）：antd Table 承载，与「互认项目金额维护」页同一承载机制与样式口径。
      八列恒定全显：类型｜分类｜分组｜项目｜可互认时间｜互认配置状态｜当前不可用原因｜操作；
      类型格与分类格经 `rowSpan` 纵向跨其名下全部项目行表达从属；层级格是整格可点的按钮，
      点击只高亮其整条下级链路（含项目格），不携带写操作；层级格的点击阻止冒泡——页面根上的
      处理器负责「点击其他区域清除高亮」，让它收到这次点击会立刻清掉刚设置的高亮；
      项目格不阻止冒泡，点项目格或页面空白即清除高亮。
      后四列的承载与「互认项目金额维护」页对应列一致：时间列为值按钮与行内编辑器，状态列为徽标，
      原因列为目录停用原因文本（超长省略），操作列为启停图标按钮。
      两种空态事实不同：本组织还没有任何互认项目配置（此时没有任何生效条件），
      与「有条件但没命中」——关键字或状态把结果裁空了。服务端按关键字过滤后行集为空时
      无法只看行数区分两者，必须结合生效条件判定。
    */}
    <Table<RecognitionProjectTableRow>
      className='recognition-projects-table'
      rowKey={(record) => record.row.rowIdentity}
      dataSource={tableRows}
      loading={loading && tableRows.length === 0}
      scroll={{ x: 1316 }}
      pagination={false}
      locale={{
        emptyText: <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description={totalRowCount === 0 && !filtering ? '本组织还没有互认项目配置' : '没有匹配的配置'}
        />,
      }}
      columns={[
        {
          title: '类型',
          width: RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.itemType,
          onCell: (record) => ({
            rowSpan: record.typeFirst ? record.typeSpan : 0,
            className: levelCellClassName(isItemTypeHighlighted(highlight, record.itemType)),
          }),
          render: (_, record) => record.typeFirst
            ? <button
              type='button'
              className='recognition-projects-level'
              aria-label={`高亮类型「${record.itemTypeText}」及其下级`}
              onClick={(event) => {
                event.stopPropagation()
                setHighlight({ level: 'itemType', itemType: record.itemType })
              }}
            >{record.itemTypeText}</button>
            : null,
        },
        {
          title: '分类',
          width: RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.category,
          onCell: (record) => ({
            rowSpan: record.categoryFirst ? record.categorySpan : 0,
            className: levelCellClassName(isCategoryHighlighted(highlight, record.itemType, record.categoryName)),
          }),
          render: (_, record) => record.categoryFirst
            ? <button
              type='button'
              className='recognition-projects-level'
              aria-label={`高亮分类「${record.categoryName}」及其下级`}
              onClick={(event) => {
                event.stopPropagation()
                setHighlight({ level: 'category', itemType: record.itemType, categoryName: record.categoryName })
              }}
            >{record.categoryName}</button>
            : null,
        },
        {
          title: '分组',
          width: RECOGNITION_PROJECT_LEVEL_COLUMN_WIDTHS.group,
          onCell: (record) => ({
            rowSpan: record.groupFirst ? record.groupSpan : 0,
            className: levelCellClassName(isGroupHighlighted(highlight, record.itemType, record.categoryName, record.groupName)),
          }),
          render: (_, record) => record.groupFirst
            ? <button
              type='button'
              className='recognition-projects-level'
              aria-label={`高亮分组「${record.groupName}」及其下级`}
              onClick={(event) => {
                event.stopPropagation()
                setHighlight({
                  level: 'group',
                  itemType: record.itemType,
                  categoryName: record.categoryName,
                  groupName: record.groupName,
                })
              }}
            >{record.groupName}</button>
            : null,
        },
        {
          // 项目列弹性占满剩余宽度：设计只为其余七列给定列宽，本列不设宽度。
          title: '项目',
          onCell: (record) => ({
            className: itemCellClassName(isGroupHighlighted(highlight, record.itemType, record.categoryName, record.groupName)),
          }),
          render: (_, record) => {
            const columnRow = record.row
            const name = rowDisplayName(columnRow)
            return <span className='recognition-projects-row-name'>
              <span className='recognition-projects-row-name-text' title={name}>{name}</span>
              <span className='recognition-projects-row-code' title={columnRow.standardProjectCode ?? undefined}>
                {columnRow.standardProjectCode ?? '—'}
              </span>
            </span>
          },
        },
        {
          // 可互认时间列：非编辑态是值按钮、编辑态是「输入框＋保存＋取消」，两者在本单元格内切换。
          title: '可互认时间',
          width: 190,
          render: (_, record) => <RecognitionProjectDurationCell
            row={record.row}
            writeGate={writeGate}
            submitting={submitting}
            canWrite={canWrite}
            editing={durationEditRow !== null && durationEditRow.rowIdentity === record.row.rowIdentity}
            onStartEdit={() => setDurationEdit({ rowIdentity: record.row.rowIdentity })}
            onCancelEdit={closeDurationEdit}
            onSubmitDuration={(durationInput) => {
              const request = buildDurationUpdatePayload(record.row.configurationId, durationInput)
              if (request === null) return Promise.resolve()
              return submitWrite(() => updateRecognitionDuration(client, request), closeDurationEdit)
            }}
          />,
        },
        {
          // 互认配置状态列：行摘要徽标（含「目录已停用」），颜色取适配层同一推导出口。
          title: '互认配置状态',
          width: 130,
          render: (_, record) => {
            const badge = recognitionProjectRowBadge(record.row)
            return <Tag color={badge.color}>{badge.text}</Tag>
          },
        },
        {
          // 当前不可用原因列：目录三层启用时服务端返回 null，此时该单元格为空；超长文本按列省略。
          title: '当前不可用原因',
          width: 190,
          ellipsis: true,
          render: (_, record) => record.row.unavailableReason,
        },
        {
          // 操作列：启停图标按钮，只改配置自身状态。
          title: '操作',
          width: 64,
          render: (_, record) => <RecognitionProjectToggleCell
            row={record.row}
            canWrite={canWrite}
            actionText={actionLabel}
            onToggle={() => {
              // 只把配置标识交给弹窗：弹窗渲染时按当前列视图重取该行与它的当前状态，
              // 不持有打开那一刻的行快照，也不持有打开那一刻的目标状态。
              if (record.row.configurationId === null) return
              setModal({ kind: 'toggle', configurationId: record.row.configurationId })
            }}
          />,
        },
      ]}
    />
    <Typography.Text type='secondary' className='recognition-projects-summary'>
      {/* 页脚只报当前显示条数：列表响应没有总数来源，而页面行集就是本组织的配置集合（S2-D24）。 */}
      共 {visibleRowCount} 项配置
    </Typography.Text>

    {modal.kind === 'create' ? <RecognitionProjectBatchCreateModal
      open
      view={selectableColumnView}
      catalogReady={catalogReady}
      keyword={batchKeyword}
      onKeywordSearch={searchSelectableItems}
      selection={batchSelection}
      durationInput={batchDurationInput}
      writeGate={writeGate}
      onCancel={closeWriteModal}
      submitting={submitting}
      reloading={selectableLoading}
      prunedCount={prunedSelectionCount}
      onReload={() => reloadSelectableItems(batchKeyword)}
      // 手动改变选择即表示用户已经看过当前集合，上一次重读的剔除说明不再适用。
      onSelectionChange={(next) => {
        setPrunedSelectionCount(0)
        setBatchSelection(next)
      }}
      onDurationChange={setBatchDurationInput}
      onSubmit={() => {
        const request = buildBatchCreateConfigurationRequest(batchSelection, batchDurationInput)
        // 载荷构造失败（未勾选或天数非法）时零写请求：弹窗内已就地给出原因，这里只负责不放行请求。
        if (request === null) return Promise.resolve()
        return submitWrite(() => createRecognitionProjectConfigurations(client, request), closeWriteModal)
      }}
    /> : null}

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
          closeWriteModal,
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
        宿主已把当前组织切换为 <Typography.Text strong>{trustedOrganizationCode}</Typography.Text>，但新增弹窗或行内编辑中还有未保存的内容。
        继续切换会放弃这些内容，并清空当前组织的列表、筛选和写入界面状态。
      </Typography.Paragraph>
      <Typography.Paragraph type='secondary' style={{ marginBottom: 0 }}>选择取消可保留当前内容，之后再决定何时切换。</Typography.Paragraph>
    </Modal> : null}
  </div>
}
