/**
 * 阶段 3「互认项目金额维护」平台管理员页 Component 层用例
 * （测试矩阵 C1、C2、C5-C10、C12-C14、C16、C17、C21、C23、C27-C34、C35、C36 的组件面）。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节：mock 位于 Client/适配层边界，并保持真实形态）：
 * 1. `./recognitionAmountsApi` 替换 4 个 I/O 出口（两个查询、两个写入）与保存前的可信范围复核出口
 *    `recheckRecognitionAmountScope`（描述块缺省由替身给值、核对入参两侧——应用范围时记录的
 *    可信院区与保存发起前现读的可信院区；C34 描述块把该出口接到真实实现以断言整链，装配说明见
 *    该描述块；逐层比对与适配层交付的原因串由 recognitionAmountsApi.test.ts 的复核用例覆盖，
 *    message 拦截提示的文案由本页书写），其余纯函数
 *    （请求构造、金额解析、读模型 → 行视图模型映射、层级列视图、本地裁剪、地址栏读写、金额与
 *    枚举文案）**使用真实实现**：行夹具由真实 `toRecognitionAmountRows` 从读模型形态构造，
 *    表格行序由真实 `toRecognitionAmountColumnView` 决定，因此「未配置与零元的归一」「枚举文案」
 *    「层级平铺顺序」都出自适配层，页面的最终状态由适配层数据推导而来；
 * 2. `../../contexts/ApiClientContext` 只提供占位 client——页面把 client 原样透传给适配层出口；
 * 3. `OrganizationScopeSelector` 整组件替换为范围选择器替身：替身只复现页面接线依赖的组件行为——
 *    受控 `value`、初始选项加载完成后按 `selectFirstOption` 逐级取首项并回传完整范围值与选项清单
 *    （页头名称与默认范围解析的取值来源）、受控值与上次回传值不一致时按受控值重载下游选项并回传
 *    校正结果（清单内保持受控值、取不到即取第一项，与真实组件的初始数据装载同一口径）、用户改动
 *    按可选项级联回填下游首项并把完整范围值回传、选项读取失败时调用 antd 静态 `message.error`。
 *    替身的数据源是本文件的三级选项常量，不依赖任何 Base 包。真实选择器的行为属宿主层验收面
 *    （C1/C3 的 Host 层级）；
 * 4. 可信范围只来自宿主会话：本文件用外部会话 store 替身提供组织、医院、院区与身份状态，
 *    `resignTrustedToken` 按宿主切换工作身份的路径推进会话快照，页面据此重新求值可信范围；
 *    页面只把可信范围当作默认值，不把它记作用户的选择。
 *
 * 5. 金额维护的编辑形态是行内编辑器（C32）：入口是当前金额格的值按钮（值文本或「未配置」标记，
 *    S3-D27 2026-10-12 修订，与可互认时间值按钮同款），编辑态为「输入框＋保存＋取消」，
 *    回车与保存按钮提交、Esc 与取消按钮放弃，校验失败在编辑器内就地提示；
 *    测试只按可访问名称、文本与页面既有样式类定位，不断言编辑器的内部结构。
 *
 * 仍属 Host 层、不在本文件覆盖：真实宿主菜单进入、真实范围选择器交互、宿主统一错误提示
 * （写失败时的业务拒绝文案由宿主展示）、C11/C12/C13/C14/C20/C22/C26 的宿主面。
 */
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { ConfigProvider, message } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { OrganizationScopeSelectorProps, OrganizationScopeValue } from '../../components/OrganizationScopeSelector'
import { toRecognitionAmountColumnView, toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
import { RecognitionAmounts } from './RecognitionAmounts'

/** 范围选择器替身的数据源：本文件构造的三级选项读写桩（见文件头第 3 条）。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

const api = vi.hoisted(() => ({
  queryRecognitionAmountRows: vi.fn(),
  queryBranchRecognitionAmountRows: vi.fn(),
  saveOrganizationHospitalBranchRecognitionAmount: vi.fn(),
  saveBranchRecognitionAmount: vi.fn(),
  recheckRecognitionAmountScope: vi.fn(),
  exportRecognitionAmounts: vi.fn(),
}))

/** 共享下载模块在 Client/适配层边界替换：页面取得导出字节后经它触发保存。 */
const exportDownload = vi.hoisted(() => ({ saveExportWorkbook: vi.fn() }))

vi.mock('../../shared/exportFileDownload', async (importOriginal) => ({ ...(await importOriginal<typeof import('../../shared/exportFileDownload')>()), saveExportWorkbook: exportDownload.saveExportWorkbook }))

vi.mock('../../components/OrganizationScopeSelector', async () => {
  const { createElement, useEffect, useRef, useState } = await import('react')
  const { message: antdMessage } = await import('antd')

  type ScopeOption = { id?: string | null; code?: string | null; name?: string | null; hosId?: string | null }

  /** 按实例标识取该层业务编码：与真实组件的选项清单同形（标识 + 编码 + 名称）。 */
  const codeOf = (options: ScopeOption[], id: string | undefined): string | undefined =>
    id === undefined ? undefined : options.find((option) => option.id === id)?.code ?? undefined
  interface ScopeClient {
    api: {
      organization: {
        queryAllOrganization: { post: () => Promise<ScopeOption[] | undefined> }
        queryAllValidHospitalByOrgId: { post: (body: { orgId: string }) => Promise<ScopeOption[] | undefined> }
        queryAllValidBranchByOrgId: { post: (body: { orgId: string }) => Promise<ScopeOption[] | undefined> }
      }
    }
  }

  /** 替身回传的选项清单：只承载页头名称与默认范围解析用到的三级选项。 */
  interface StubScopeMeta {
    organizations: ScopeOption[]
    hospitals: ScopeOption[]
    branches: ScopeOption[]
    unconfiguredOrganizationCount: number
  }

  /**
   * 替身 props：在真实组件 props 上把 onChange 的选项清单换成替身自己的选项形状
   * （真实清单元素是 Kiota 生成的 DTO，替身只复现页面读取的 id 与 name 两个字段）。
   */
  type StubScopeSelectorProps = Omit<OrganizationScopeSelectorProps, 'onChange'> & {
    onChange?: (value: OrganizationScopeValue, meta: StubScopeMeta) => void
  }

  /** 范围选择器替身：接线行为与真实组件一致的部分见文件头第 3 条。 */
  function StubScopeSelector({ value = {}, disabled, className, selectFirstOption, onChange }: StubScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const initialLoadStartedRef = useRef(false)
    /** 上次回传页面的值：受控值与它一致即无需重载（与真实组件的回传值记忆同一口径）。 */
    const lastEmittedRef = useRef<OrganizationScopeValue | undefined>(undefined)

    useEffect(() => {
      /**
       * 按给定起点装载三级选项：清单内保持起点取值、取不到即取第一项（与真实组件的初始数据装载
       * 同一口径）；取值与起点不同且三级齐全时回传完整范围值与选项清单。
       */
      const loadValue = async (start: OrganizationScopeValue) => {
        try {
          const client = baseClientRef.current as ScopeClient
          const nextOrganizations = await client.api.organization.queryAllOrganization.post() ?? []
          const orgKept = start.orgId && nextOrganizations.some((option) => option.id === start.orgId)
          const orgId = orgKept ? start.orgId : nextOrganizations[0]?.id
          const nextHospitals = orgId
            ? await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId }) ?? []
            : []
          const hosKept = orgId !== undefined && start.orgId === orgId
            && start.hosId && nextHospitals.some((option) => option.id === start.hosId)
          const hosId = hosKept ? start.hosId : nextHospitals[0]?.id
          const allBranches = orgId
            ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId }) ?? []
            : []
          const nextBranches = allBranches.filter((branch) => branch.hosId === hosId)
          const branchKept = orgId !== undefined && start.orgId === orgId
            && start.branchId && nextBranches.some((option) => option.id === start.branchId)
          const branchId = branchKept ? start.branchId : nextBranches[0]?.id
          setOrganizations(nextOrganizations)
          setHospitals(nextHospitals)
          setBranches(nextBranches)
          // 受控 + selectFirstOption：三级取值齐全才回传，且回传值与本次起点一致时不打扰页面。
          if (selectFirstOption && orgId && hosId && branchId
            && (orgId !== start.orgId || hosId !== start.hosId || branchId !== start.branchId)) {
            const nextValue: OrganizationScopeValue = {
              orgId,
              hosId,
              branchId,
              orgCode: codeOf(nextOrganizations, orgId),
              hosCode: codeOf(nextHospitals, hosId),
              branchCode: codeOf(nextBranches, branchId),
            }
            lastEmittedRef.current = nextValue
            onChange?.(
              nextValue,
              { organizations: nextOrganizations, hospitals: nextHospitals, branches: nextBranches, unconfiguredOrganizationCount: 0 },
            )
          }
        } catch (error) {
          antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        }
      }

      if (!initialLoadStartedRef.current) {
        // 挂载时按真实组件的初始装载跑一次：受控值尚为空，逐级取第一项。
        initialLoadStartedRef.current = true
        void loadValue({})
        return
      }
      // 受控值与上次回传值不一致才按受控值重载（页面写入初始范围依赖的另一半接线行为）。
      const emitted = lastEmittedRef.current
      if (emitted === undefined) return
      if (emitted.orgId === value.orgId && emitted.hosId === value.hosId && emitted.branchId === value.branchId) return
      void loadValue(value)
    }, [onChange, selectFirstOption, value])

    /** 变更后按可选项级联回填下游首项（与真实组件同形），并把完整范围值与当前选项清单回传页面。 */
    const handleChange = async (changed: 'org' | 'hospital' | 'branch', next: string) => {
      const client = baseClientRef.current as ScopeClient
      const merged = { ...value }
      let nextHospitals = hospitals
      let nextBranches = branches
      try {
        if (changed === 'org') {
          merged.orgId = next.length > 0 ? next : undefined
          nextHospitals = merged.orgId
            ? await client.api.organization.queryAllValidHospitalByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          setHospitals(nextHospitals)
          merged.hosId = nextHospitals[0]?.id ?? undefined
          const allBranches = merged.orgId
            ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
          setBranches(nextBranches)
          merged.branchId = nextBranches[0]?.id ?? undefined
        } else if (changed === 'hospital') {
          merged.hosId = next.length > 0 ? next : undefined
          const allBranches = merged.orgId
            ? await client.api.organization.queryAllValidBranchByOrgId.post({ orgId: merged.orgId }) ?? []
            : []
          nextBranches = allBranches.filter((branch) => branch.hosId === merged.hosId)
          setBranches(nextBranches)
          merged.branchId = nextBranches[0]?.id ?? undefined
        } else {
          merged.branchId = next.length > 0 ? next : undefined
        }
      } catch (error) {
        antdMessage.error(error instanceof Error ? error.message : '加载机构选择数据失败')
        return
      }
      const nextValue: OrganizationScopeValue = {
        ...merged,
        orgCode: codeOf(organizations, merged.orgId),
        hosCode: codeOf(nextHospitals, merged.hosId),
        branchCode: codeOf(nextBranches, merged.branchId),
      }
      lastEmittedRef.current = nextValue
      onChange?.(nextValue, { organizations, hospitals: nextHospitals, branches: nextBranches, unconfiguredOrganizationCount: 0 })
    }

    const levelSelect = (
      label: string,
      options: ScopeOption[],
      selected: string | undefined,
      selectDisabled: boolean,
      onSelect: (next: string) => void,
    ) => createElement('select', {
      'aria-label': `范围-${label}`,
      value: selected ?? '',
      disabled: disabled === true || selectDisabled,
      onChange: (event: { target: { value: string } }) => onSelect(event.target.value),
    }, [
      createElement('option', { key: '__placeholder', value: '' }, `请选择${label}`),
      ...options.map((option, index) => createElement(
        'option',
        { key: option.id ?? index, value: option.id ?? '' },
        option.name ?? option.id ?? '',
      )),
    ])

    // 三个子节点按位置各占一个参数位；数组形式的子元素需要 key，按位置传入满足该要求，stderr 不出现缺 key 的告警淹没真实告警。
    return createElement(
      'div',
      { className },
      levelSelect('组织', organizations, value.orgId, false, (next) => { void handleChange('org', next) }),
      levelSelect('医院', hospitals, value.hosId, !value.orgId, (next) => { void handleChange('hospital', next) }),
      levelSelect('院区', branches, value.branchId, !value.hosId, (next) => { void handleChange('branch', next) }),
    )
  }

  return { OrganizationScopeSelector: StubScopeSelector }
})

/** 可信院区注入点：缺省 `null` 表示可信院区缺失，用例按需给值（见文件头第 4 条）。 */
const trustedBranchRef = vi.hoisted(() => ({ current: null as string | null }))

/** 可信组织、可信医院注入点：缺省 `null` 表示该层缺失，与可信院区同一手法（见文件头第 4 条）。 */
const trustedOrganizationRef = vi.hoisted(() => ({ current: null as string | null }))
const trustedHospitalRef = vi.hoisted(() => ({ current: null as string | null }))

/**
 * 宿主会话的外部 store；`version` 是订阅快照：任何一次通知都推进它，让订阅方按新的会话
 * 重新求值。`token` 只表达是否已认证；推进它等价于宿主切换工作身份。
 */
const authState = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  return {
    token: 'token-1' as string | null,
    version: 0,
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
    /** 通知订阅者会话已变化。 */
    notify() {
      authState.version += 1
      for (const listener of listeners) listener()
    },
  }
})

/** 由当前替身状态构造宿主会话快照；未认证时三个范围字段都为 null。 */
function buildSession() {
  const authenticated = authState.token !== null
  return {
    status: authenticated ? 'authenticated' as const : 'anonymous' as const,
    sessionVersion: `v${authState.version}`,
    userId: authenticated ? 'user-1' : null,
    account: authenticated ? '191617' : null,
    displayName: authenticated ? '杨康健' : null,
    organizationId: authenticated ? trustedOrganizationRef.current : null,
    hospitalId: authenticated ? trustedHospitalRef.current : null,
    campusId: authenticated ? trustedBranchRef.current : null,
  }
}

vi.mock('../../session/hostSession', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../session/hostSession')>()
  const { useEffect, useReducer } = await import('react')
  return {
    ...actual,
    readCurrentHostSession: () => buildSession(),
    useHostSessionSnapshot: () => {
      const [, forceRender] = useReducer((value: number) => value + 1, 0)
      useEffect(() => authState.subscribe(forceRender), [])
      return buildSession()
    },
  }
})

/**
 * 当前范围接缝替身：可信范围的业务编码与实例标识由同一组替身取值给出（本文件不区分两者，
 * 标识与编码的区分由 currentOrganizationScope 与 reportsTrustedScope 的接缝用例承担）。
 */
vi.mock('../../components/currentOrganizationScope', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../components/currentOrganizationScope')>()
  const scope = () => {
    const authenticated = authState.token !== null
    return {
      organizationId: authenticated ? trustedOrganizationRef.current : null,
      organizationName: null,
      organizationCode: authenticated ? trustedOrganizationRef.current : null,
      hospitalId: authenticated ? trustedHospitalRef.current : null,
      hospitalName: null,
      hospitalCode: authenticated ? trustedHospitalRef.current : null,
      branchId: authenticated ? trustedBranchRef.current : null,
      branchName: null,
      branchCode: authenticated ? trustedBranchRef.current : null,
    }
  }
  return {
    ...actual,
    readCurrentOrganizationScope: () => scope(),
    useResolvedTrustedScope: () => {
      const value = scope()
      return {
        status: value.organizationCode === null ? 'unavailable' as const : 'ready' as const,
        organizationId: value.organizationId,
        hospitalId: value.hospitalId,
        branchId: value.branchId,
        organizationCode: value.organizationCode,
        hospitalCode: value.hospitalCode,
        branchCode: value.branchCode,
      }
    },
  }
})

vi.mock('./recognitionAmountsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./recognitionAmountsApi')>()
  return { ...actual, ...api }
})

/** 占位 client：页面把 API Client 原样交给适配层出口，自身不访问任何端点。 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

/** 伪 Base API 数据：组织、医院、院区三级（院区按医院归属，与组件过滤逻辑同形）。 */
// 范围选项夹具：实例标识带 `-ID` 后缀、业务编码不带——页面请求携带的必须是业务编码。
const ORGANIZATIONS = [
  { id: 'ORG-A-ID', code: 'ORG-A', name: '组织一' },
  { id: 'ORG-B-ID', code: 'ORG-B', name: '组织二' },
]
const HOSPITALS = [
  { id: 'HOS-1-ID', code: 'HOS-1', name: '医院一' },
  { id: 'HOS-2-ID', code: 'HOS-2', name: '医院二' },
]
const BRANCHES = [
  { id: 'BRA-1-ID', code: 'BRA-1', name: '院区一', hosId: 'HOS-1-ID' },
  { id: 'BRA-2-ID', code: 'BRA-2', name: '院区二', hosId: 'HOS-2-ID' },
  { id: 'BRA-3-ID', code: 'BRA-3', name: '院区三', hosId: 'HOS-2-ID' },
  // 同一医院下的第二个院区：用于「切换院区」这一范围变化（院区选项按医院过滤）。
  { id: 'BRA-4-ID', code: 'BRA-4', name: '院区四', hosId: 'HOS-1-ID' },
]

/**
 * 每个用例一个伪 Base Client 实例：`restoreMocks` 会让上一用例的 `vi.fn()` 实现失效，
 * 因此在 `beforeEach` 里重建，并由用例按需改成拒绝。
 */
function createBaseClient() {
  return {
    api: {
      organization: {
        queryAllOrganization: { post: vi.fn(async () => ORGANIZATIONS) },
        queryAllValidHospitalByOrgId: { post: vi.fn(async () => HOSPITALS) },
        queryAllValidBranchByOrgId: { post: vi.fn(async () => BRANCHES) },
      },
    },
  }
}

let baseClient: ReturnType<typeof createBaseClient>

/** 生成端反序列化后的金额形态：`number | null`。 */
const amount = (value: number | null) => value

/** 行夹具：由**真实**适配层出口从读模型形态归一而来（见文件头第 1 条）。 */
function amountRow(overrides: Partial<RecognitionAmountReadModelLike> = {}): RecognitionAmountRow {
  return toRecognitionAmountRows([{
    standardProjectCode: 'A01',
    standardProjectName: '血常规',
    itemType: 0,
    categoryName: '检验分类',
    groupName: '血液',
    organizationName: '组织一',
    hospitalName: '医院一',
    branchName: '院区一',
    configurationStatus: 1,
    unavailableReason: null,
    currentAmount: amount(12.5),
    isAmountConfigured: true,
    ...overrides,
  }])[0]
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

const click = (element: HTMLElement) => act(() => {
  fireEvent.click(element)
})

const change = (element: HTMLElement, value: string) => act(async () => {
  fireEvent.change(element, { target: { value } })
})

const query = () => api.queryRecognitionAmountRows
const save = () => api.saveOrganizationHospitalBranchRecognitionAmount

/** 排队一次列表读取；页面每次范围变化或手动刷新都会调用一次。 */
function armQuery() {
  const pending = deferred<RecognitionAmountRow[]>()
  query().mockReturnValueOnce(pending.promise)
  return pending
}

async function settle(pending: ReturnType<typeof deferred<RecognitionAmountRow[]>>, rows: RecognitionAmountRow[]) {
  await act(async () => {
    pending.resolve(rows)
  })
}

async function failQuery(pending: ReturnType<typeof deferred<RecognitionAmountRow[]>>) {
  await act(async () => {
    pending.reject(new Error('读取失败'))
  })
}

/** 三级下拉按可访问名称定位（替身与真实组件都用「请选择组织 / 请选择医院 / 请选择院区」文案）。 */
const scopeSelect = (level: '组织' | '医院' | '院区') => screen.getByLabelText(`范围-${level}`) as HTMLSelectElement

async function pickScopeOption(level: '组织' | '医院' | '院区', value: string) {
  const select = scopeSelect(level)
  // 选项由伪 Base Client 异步交付：等选项就绪再选，避免构造出「选了不存在的值」这种非真实路径。
  await waitFor(() => expect(Array.from(select.options).some((option) => option.value === value)).toBe(true))
  await act(async () => {
    fireEvent.change(select, { target: { value } })
  })
}

/**
 * 挂载后按宿主重新签发 token 的路径推进可信登录态并通知订阅者：可信院区经替身接缝同步给值，
 * 页面据此在下一次渲染现读新可信院区（与医院管理员页用例的 `changeTrustedScope` 同一构造）。
 */
async function resignTrustedToken(branchCode: string, token = 'token-2') {
  await act(async () => {
    trustedBranchRef.current = branchCode
    authState.token = token
    authState.notify()
  })
}

/** 弹窗按标题文本定位（与阶段 2 组件层用例同一手法：测试环境下 antd 无 id 组件共用自动生成的 id）。 */
function modalElement(title: string): HTMLElement {
  const found = Array.from(document.querySelectorAll<HTMLElement>('.ant-modal')).find(
    (node) => node.querySelector('.ant-modal-title')?.textContent?.trim() === title,
  )
  if (!found) throw new Error(`弹窗未找到：${title}`)
  return found
}

const modalExists = (title: string) =>
  Array.from(document.querySelectorAll('.ant-modal-title')).some((node) => node.textContent?.trim() === title)

const findModal = (title: string) => waitFor(() => modalElement(title))

/** 行内编辑器（C32）的金额输入：编辑态以可访问名称「金额」呈现，收起后即从文档消失。 */
const inlineAmountInput = () => screen.getByLabelText('金额') as HTMLInputElement
const queryInlineAmountInput = () => screen.queryByLabelText('金额')

/** 回车与 Esc 走异步 act：回车提交会启动写入与随后的整表重载，需要让微任务就地排空。 */
const pressKey = (element: HTMLElement, key: string) => act(async () => {
  fireEvent.keyDown(element, { key })
  // 真实键盘按键 keydown 与 keyup 成对出现；底层输入组件对 Enter 有键锁（keydown 置锁、keyup
  // 复位），只发 keyDown 会吞掉同一输入框里的第二次回车。
  fireEvent.keyUp(element, { key })
})
const pressEnter = (element: HTMLElement) => pressKey(element, 'Enter')
const pressEscape = (element: HTMLElement) => pressKey(element, 'Escape')

/** 目标行：按项目名称在当前行集里定位；行离开当前行集即视为编辑目标失效（C32）。 */
function rowOf(standardProjectName: string): HTMLElement {
  const row = dataRows().find((candidate) => candidate.textContent?.includes(standardProjectName))
  if (!row) throw new Error(`行未找到：${standardProjectName}`)
  return row
}

/** 当前金额格：值文本或「未配置」标记所在单元格，行内编辑的入口（C32）。 */
function amountValueCell(standardProjectName: string): HTMLElement {
  const cell = rowOf(standardProjectName).querySelector<HTMLElement>('.recognition-amounts-number')
  if (!cell) throw new Error(`当前金额格未找到：${standardProjectName}`)
  return cell
}

const editAmount = (standardProjectName: string) => click(amountValueCell(standardProjectName))

/** C33：可信范围变化确认框的标题；待切换提示态以页面既有样式类呈现（RecognitionAmounts.css）。 */
const CONFIRM_TRUSTED_SCOPE = '可信范围已变化'
const promptElement = () => document.querySelector<HTMLElement>('.recognition-amounts-prompt')

/** 框内查询图标按钮与关键字输入框：读取在途时图标按钮呈加载态，关键字只在提交时下推服务端。 */
const searchButton = () => screen.getByRole('button', { name: '查询' })
const keywordInput = () => screen.getByLabelText('关键字筛选') as HTMLInputElement

/**
 * 输入框内回车提交：antd Input 的 `onPressEnter` 由 keydown Enter 触发，与框内查询图标按钮的
 * 点击进入同一条 `submitKeyword` 提交链路。
 */
const submitByEnter = () => pressEnter(keywordInput())

/** 金额状态下拉选择把 aria-label 透传到 role=combobox；行内文字标签在 Select 外，不参与可访问名称。 */
const amountStatusFilter = () => screen.getByRole('combobox', { name: '金额状态筛选' })

/** 互认配置状态筛选为列表上沿页签（S3-D25 2026-10-13 修订），外层以 role=group 承载可访问名称。 */
const configurationStatusTabs = () => screen.getByRole('group', { name: '配置状态筛选' })

/** 当前配置状态筛选的选中项：Tabs 的选中页签带 .ant-tabs-tab-active。 */
const selectedStatusTabLabels = () =>
  [...configurationStatusTabs().querySelectorAll('.ant-tabs-tab-active')].map((node) => node.textContent?.trim())

/** 按名称点击一个配置状态页签：切换即生效、本地裁剪、不触发读取。 */
const clickConfigurationStatusTab = (name: string) => click(within(configurationStatusTabs()).getByRole('tab', { name }))

/** 下拉选择当前档位的展示文本（antd Select 的选中值格为 `.ant-select-content`）。 */
const selectedSelectValue = (combobox: HTMLElement) =>
  combobox.closest('.ant-select')?.querySelector('.ant-select-content')?.textContent?.trim()

/** 展开下拉并点击某个档位；档位只做本地裁剪，不触发读取。 */
const pickSelectOption = async (combobox: HTMLElement, option: string) => {
  fireEvent.mouseDown(combobox)
  // 归属判定排除收起中的残留节点：测试环境两个 Select 的 id 都退化为同一占位值，aria-controls
  // 不能区分归属；jsdom 不派发 transitionend，上一个下拉的 leave 动画类会残留，但正在展开
  // 的归属下拉不带 leave 标记。两个下拉的「全部」档位同名，归属判错会点到上一个下拉。
  const dropdown = await waitFor(() => {
    const open = [...document.querySelectorAll<HTMLElement>('.ant-select-dropdown')]
      .filter((node) => !node.classList.contains('ant-select-dropdown-hidden')
        && !node.className.includes('ant-slide-up-leave'))
    if (open.length !== 1) throw new Error('归属下拉未唯一展开')
    return open[0]
  })
  const item = [...dropdown.querySelectorAll<HTMLElement>('.ant-select-item-option')]
    .find((node) => node.textContent?.trim() === option)
  if (!item) throw new Error(`档位未找到：${option}`)
  click(item)
}

const dataRows = () =>
  screen.getAllByRole('row').filter((node) => node.classList.contains('ant-table-row'))

const columnHeaders = () => screen.getAllByRole('columnheader').map((node) => node.textContent)

/** 项目格里的编码文本：按页面实际渲染顺序取，列视图的平铺顺序即页面行序。 */
const renderedCodes = () =>
  Array.from(document.querySelectorAll<HTMLElement>('.recognition-amounts-project-code'))
    .map((node) => node.textContent)

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><RecognitionAmounts /></ConfigProvider>)
}

/** 进入页面：范围按默认规则解析后自动发起一次读取，让列表成功交付给定行（空集合同样是读取成功的正常结果）。 */
async function renderLoaded(rows: RecognitionAmountRow[]) {
  const pending = armQuery()
  renderPage()
  await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
  await settle(pending, rows)
  await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
}

beforeEach(() => {
  for (const fn of Object.values(api)) fn.mockReset()
  // 保存前的可信范围复核缺省通过；需要失败结论的用例（C34）按次覆盖返回值。
  api.recheckRecognitionAmountScope.mockReturnValue({ ok: true, reason: null })
  exportDownload.saveExportWorkbook.mockReset()
  clientRef.current = {}
  baseClient = createBaseClient()
  baseClientRef.current = baseClient
  // 地址栏与可信三层都回到缺省状态：上一个用例的已生效条件不得泄漏进本用例。
  window.history.replaceState(null, '', '/')
  trustedBranchRef.current = null
  authState.token = 'token-1'
  // 可信组织与医院缺省在值（组织 ORG-A、医院 HOS-1，与伪 Base Client 首项组合一致）：
  // 阻断判定之下页面只有可信组织、医院齐备才进入选择器解析；缺失场景由阻断用例显式改写。
  trustedOrganizationRef.current = 'ORG-A'
  trustedHospitalRef.current = 'HOS-1'
  // 选择器替身直接从本文件的数据源取选项，不发起真实请求。
})

/**
 * 用例内未处理拒绝观测器的兜底登记：用例挂死或超时时用例内的 `finally` 不会执行，而
 * `unhandledRejection` 是进程级监听，残留后会让 vitest 以「监听器数 > 1」判定调用方自行处理、
 * 不再上报自己的未处理拒绝，等于静默关掉该 worker 内后续用例的检测。因此除用例内 `finally`
 * 摘除外，再于 `afterEach` 无条件兜底摘一次。
 *
 * 该登记是**文件级单值**：装有观测器的用例之间不得并发（不得加 `.concurrent`、也不得开启
 * `sequence.concurrent`），否则先结束者的 `afterEach` 会摘掉后结束者仍在使用的监听。
 * 这个禁用条件随本条登记一起失效——将来若需要并发，先把这里换成一个 `Set`。
 */
let activeUnhandledRejectionObserver: ((reason: unknown) => void) | null = null

afterEach(() => {
  if (activeUnhandledRejectionObserver === null) return
  process.off('unhandledRejection', activeUnhandledRejectionObserver)
  activeUnhandledRejectionObserver = null
})

describe('C1 范围选择与首次加载', () => {
  it('三级下拉都渲染，进入页面即按默认范围自动发起一次读取', async () => {
    const first = armQuery()

    renderPage()

    expect(scopeSelect('组织')).toBeInTheDocument()
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    // 默认范围逐级取第一项（可信组织或可信医院缺失）：无需任何人工选择，进入页面即自动读取。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
    })
    expect(save()).not.toHaveBeenCalled()
    expect(api.saveBranchRecognitionAmount).not.toHaveBeenCalled()

    await settle(first, [amountRow()])
    await screen.findByText('血常规')
  })

  it('机构选项为空时保留范围选择器且不发起金额查询或导出', async () => {
    baseClient.api.organization.queryAllOrganization.post.mockResolvedValueOnce([])
    renderPage()

    expect(scopeSelect('组织')).toBeInTheDocument()
    await waitFor(() => expect(baseClient.api.organization.queryAllOrganization.post).toHaveBeenCalledOnce())
    expect(scopeSelect('组织').options).toHaveLength(1)
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
    expect(queryExportButton()).toBeDisabled()
    await expectNoAmountExportRequest()
  })

  it('C1：依次切换组织、医院、院区都按新范围重载，且不自动发起写请求', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await screen.findByText('血常规')

    const second = armQuery()
    await pickScopeOption('组织', 'ORG-B-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow({ standardProjectCode: 'B01', standardProjectName: '尿常规' })])

    const third = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(third, [amountRow({ standardProjectCode: 'C01', standardProjectName: '便常规' })])

    const fourth = armQuery()
    await pickScopeOption('院区', 'BRA-3-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(4))
    await settle(fourth, [amountRow({ standardProjectCode: 'D01', standardProjectName: '血沉' })])

    // C2 的组件面：每次查询都携带组织、医院、院区三个值，且随范围变化按新值重发。
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      { organizationCode: 'ORG-B', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      { organizationCode: 'ORG-B', hospitalCode: 'HOS-2', branchCode: 'BRA-2' },
      { organizationCode: 'ORG-B', hospitalCode: 'HOS-2', branchCode: 'BRA-3' },
    ])
    expect(save()).not.toHaveBeenCalled()
    expect(api.saveBranchRecognitionAmount).not.toHaveBeenCalled()
  })
})

describe('C5 列表字段集合', () => {
  it('列为类型、分类、分组、项目与三个业务列，不展示三级名称与编码、项目类型、最后修改信息', async () => {
    await renderLoaded([amountRow()])

    // 金额状态列随 S3-D24（2026-10-12 修订）删除：当前金额列的「未配置」标记与数值已完整表达配置与否。
    expect(columnHeaders()).toEqual([
      '类型',
      '分类',
      '分组',
      '项目',
      '当前金额',
      '互认配置状态',
      '当前不可用原因',
    ])
    for (const absent of [
      '组织名称',
      '医院名称',
      '院区名称',
      '组织编码',
      '医院编码',
      '院区编码',
      '项目类型',
      '最后修改时间',
      '最后修改人',
      '标准项目编码',
      '标准项目名称',
      // 金额状态列已删除（S3-D24 2026-10-12 修订），表头不再出现。
      '金额状态',
      // 每行金额即行内编辑入口，不再设操作列。
      '操作',
    ]) {
      expect(columnHeaders()).not.toContain(absent)
    }
  })

  it('项目名称取服务端返回值，三级名称不再进表格，编码只出现一次', async () => {
    await renderLoaded([amountRow({
      standardProjectName: '服务端项目名',
      organizationName: '服务端组织名',
      hospitalName: '服务端医院名',
      branchName: '服务端院区名',
      unavailableReason: '所属分组已停用',
    })])

    expect(screen.getByText('服务端项目名')).toBeInTheDocument()
    // 三级名称不进表格：页头范围展示取范围选择器的选项名称，服务端行内三级名称一律不展示。
    expect(screen.queryByText('服务端组织名')).not.toBeInTheDocument()
    expect(screen.queryByText('服务端医院名')).not.toBeInTheDocument()
    expect(screen.queryByText('服务端院区名')).not.toBeInTheDocument()
    // 当前不可用原因直接取服务端字段。
    expect(screen.getByText('所属分组已停用')).toBeInTheDocument()
    // 项目格内编码不得重复出现：编码只出现在项目格。
    expect(screen.getAllByText('A01')).toHaveLength(1)
  })
})

describe('C6 / C7 未配置与零元的呈现差异', () => {
  it('未配置行显示「未配置」且无数值，零元按已配置金额呈现 0.00', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '零元项目', currentAmount: amount(0), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    const [zeroRow, unconfiguredRow] = dataRows()

    // 零元：金额列是已配置的金额文本 0.00，行内不出现「未配置」；金额状态徽标列已删除，
    // 「已配置」不再以徽标出现在行内（S3-D24 2026-10-12 修订）。
    expect(within(zeroRow).getByText('0.00')).toBeInTheDocument()
    expect(within(zeroRow).queryByText('未配置')).toBeNull()
    expect(within(zeroRow).queryByText('已配置')).toBeNull()

    // 未配置：金额列一处「未配置」标记（行内编辑入口），整行不出现任何数值金额与「已配置」徽标。
    expect(within(unconfiguredRow).getAllByText('未配置')).toHaveLength(1)
    expect(within(unconfiguredRow).queryByText('0.00')).toBeNull()
    expect(within(unconfiguredRow).queryByText('已配置')).toBeNull()
    expect((unconfiguredRow.textContent ?? '').match(/\d+\.\d{2}/)).toBeNull()
  })
})

describe('C8 金额输入校验', () => {
  it('负数、三位小数、双精度无法精确表示的大额与空值都在编辑器内就地提示，零写请求且保留编辑态与输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    // 本循环的非法判据只取适配层出口 `parseRecognitionAmount`（页面不另写金额规则），其中
    // `99999999999999.99` 由该出口的「内存面 / 序列化面」等价性判据决定：该出口一旦放宽或收紧，
    // 本循环的输入集合必须同步（见 recognitionAmountsApi.test.ts 的金额解析用例）。
    for (const invalid of ['-1', '1.234', '99999999999999.99']) {
      await change(inlineAmountInput(), invalid)
      await pressEnter(inlineAmountInput())
      // 校验失败在编辑器内就地提示（S3-D27 2026-10-12 修订，与可互认时间编辑器的就地提示同款）：
      // 提示渲染在当前行内，不再经页面消息承载。
      expect(await within(rowOf('血常规')).findByText('金额必须是不小于 0 且最多两位小数的数字')).toBeInTheDocument()
      // `invalid` 标记：校验在字段变更后重新求值，标记随之更新。
      await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
      expect(save()).not.toHaveBeenCalled()
      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe(invalid)
    }

    await change(inlineAmountInput(), '')
    await pressEnter(inlineAmountInput())
    expect(await within(rowOf('血常规')).findByText('金额必填')).toBeInTheDocument()
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('')
  })
})

describe('C9 / C12 金额保存成功', () => {
  it('回填当前值后不改直接提交仍发出写请求，成功后收起编辑态并整表重载', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    // 保存成功经 message.success 反馈。
    expect(await screen.findByText('金额保存成功')).toBeInTheDocument()
    // C2：保存请求同样携带组织、医院、院区三个值；载荷只经适配层出口构造。
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
    expect(screen.queryByText('12.50')).not.toBeInTheDocument()
  })
})

describe('C10 金额保存被拒绝', () => {
  it('保留编辑态与输入、不重载、不误报成功，提交中状态归零，且写失败的拒绝不被页面吞掉', async () => {
    /**
     * 未处理拒绝的观测在本用例内**局部**安装、用例结束即移除（`finally`）：既不依赖也不改动
     * `vite.config.ts` 的全局忽略，因此「页面不得自行捕获写失败」在本用例内是可判红的。
     * `process` 的 `unhandledRejection` 是 Node 层的判定点，jsdom 的 `window` 事件不承接它；
     * vitest 检测到用户监听器后不再自行上报该事件（视为用户自行处理），所以本用例的观测就是这笔
     * 拒绝的唯一判据，用例内的断言必须自己承担判红责任。
     */
    const unhandled: unknown[] = []
    const onUnhandledRejection = (reason: unknown) => { unhandled.push(reason) }
    activeUnhandledRejectionObserver = onUnhandledRejection
    process.on('unhandledRejection', onUnhandledRejection)
    try {
      await renderLoaded([amountRow()])

      editAmount('血常规')
      await change(inlineAmountInput(), '18.00')

      // 写失败会让本次提交的 promise 进入拒绝态；按 Frontend API Client 第 5 节页面**不得**
      // 捕获写失败或自行提示（拒绝文案由宿主统一展示），所以这里期待的正是「本次提交的拒绝未被
      // 页面吞掉」：观测必须恰好收到写失败这一笔。页面若自行捕获、改判为成功或额外产生别的拒绝，
      // 下面的断言即判红。
      const rejection = new Error('业务拒绝')
      save().mockRejectedValueOnce(rejection)
      await pressEnter(inlineAmountInput())
      await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
      await act(async () => {})
      await waitFor(() => expect(unhandled).toEqual([rejection]))

      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe('18.00')
      // 不刷新列表、不出现写入成功的提示，页面也不自建拒绝文案（拒绝由宿主展示）。
      expect(query()).toHaveBeenCalledTimes(1)
      expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
      expect(screen.queryByText('业务拒绝')).not.toBeInTheDocument()
      // 写失败与保存成功分流：保存成功的 message 反馈不得出现。
      expect(screen.queryByText('金额保存成功')).not.toBeInTheDocument()
      // 提交中状态归零：编辑态仍然可用，可以继续输入并重试。
      await waitFor(() => expect(inlineAmountInput()).toBeEnabled())
      // 收尾再判一次：观测窗口到用例结束为止，断言之后到达的杂散拒绝同样不得漏判。
      expect(unhandled).toEqual([rejection])
    } finally {
      process.off('unhandledRejection', onUnhandledRejection)
      // 仅当登记的仍是本次观测器时才清空：超时用例迟到的 finally 不得解除后续用例的兜底。
      if (activeUnhandledRejectionObserver === onUnhandledRejection) activeUnhandledRejectionObserver = null
    }
  })
})

describe('C13 写成功但随后的刷新失败', () => {
  it('按写成功呈现并提示「写入已成功，数据待刷新」，提供重试且不改判为保存失败', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '20.00')

    save().mockResolvedValueOnce(undefined)
    const failing = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failQuery(failing)

    await screen.findByText('写入已成功，数据待刷新')
    // 写入已成功：编辑态收起，未出现任何保存失败提示，已有数据保留。
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.queryByText(/保存失败|提交失败/)).not.toBeInTheDocument()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(amountValueCell('血常规')).toBeDisabled()
    expect(save()).toHaveBeenCalledTimes(1)

    const retry = armQuery()
    click(screen.getByRole('button', { name: /重\s*试/ }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

    await screen.findByText('20.00')
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
  })
})

describe('C14 读取失败形态（纯读取失败不渲染横幅、错误由宿主统一提示）', () => {
  it('查询失败不渲染错误横幅：旧数据保留、写入仍禁用，点查询可重试成功', async () => {
    await renderLoaded([amountRow()])

    const failing = armQuery()
    // 空条件提交等价于刷新：框内查询图标按钮重新读取当前范围。
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 加载期间禁用写入（框内查询图标按钮呈加载态、当前金额格点击不进入编辑态）。
    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()

    await failQuery(failing)
    // 失败后的观测点改为加载态结束：读取失败不渲染任何错误横幅，
    // 错误由宿主统一提示，重试经框内查询图标按钮。
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()

    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    // 重试经框内查询图标按钮：重读成功后恢复可写。
    const retry = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow()])

    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
  })
})

describe('C16 排序与不分页', () => {
  it('C16：服务端交付的层级升序在列视图中原样渲染，页面不分页', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二' }),
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三' }),
    ])

    expect(renderedCodes()).toEqual(['A01', 'A02', 'B01'])
    expect(document.querySelector('.ant-pagination')).toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('页面按列视图的层级顺序平铺，不自行重排服务端交付的行', async () => {
    const rows = [
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', categoryName: '甲分类', groupName: '分组一' }),
      amountRow({ standardProjectCode: 'B01', standardProjectName: '项目三', categoryName: '乙分类', groupName: '分组二' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', categoryName: '甲分类', groupName: '分组一' }),
    ]
    await renderLoaded(rows)

    // 期望顺序出自真实适配层出口 `toRecognitionAmountColumnView`：页面只按该视图平铺成表格行。
    const expectedCodes = toRecognitionAmountColumnView(rows).types.flatMap((type) =>
      type.categories.flatMap((category) =>
        category.groups.flatMap((group) => group.rows.map((row) => row.standardProjectCode)),
      ),
    )
    expect(expectedCodes).not.toEqual(rows.map((row) => row.standardProjectCode))
    expect(renderedCodes()).toEqual(expectedCodes)
  })
})

describe('C17 空态', () => {
  it('查询成功但当前范围零互认配置时显示零配置空态文案，且只读取一次', async () => {
    await renderLoaded([])

    expect(screen.getByText('当前范围没有互认项目金额配置')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(dataRows()).toHaveLength(0)
  })

  it('有条件但可见行未命中时显示未命中空态文案，且不追加读取', async () => {
    // 两行都是已配置：按「未配置」档裁剪后可见行为空，服务端结果本身非空。
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: amount(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: amount(20) }),
    ])

    await pickSelectOption(amountStatusFilter(), '未配置')

    expect(screen.getByText('没有符合当前筛选条件的互认项目金额')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C21 范围切换与未保存输入', () => {
  it('切换院区时未保存的金额输入与旧范围数据一并清空，不带旧范围输入提交，也不弹确认框', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    const next = armQuery()
    await pickScopeOption('院区', 'BRA-4-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })

    // 未保存输入随编辑态收起被清空，旧范围数据不再展示，且没有任何写请求；用户主动切换不弹确认框，
    // 也不进入待切换提示态。
    expect(queryInlineAmountInput()).toBeNull()
    expect(screen.queryByText('血常规')).not.toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新院区项目' })])
    await screen.findByText('新院区项目')

    // 重新进入编辑时回填的是新范围当前值，切换前未提交的输入已清空。
    editAmount('新院区项目')
    expect(inlineAmountInput().value).toBe('12.50')
  })
})

describe('C23 组件自身加载失败提示', () => {
  it('选项读取失败时组件给出自己的提示，页面不重复提示也不进入可写状态', async () => {
    const messageError = vi.spyOn(message, 'error')
    baseClient.api.organization.queryAllOrganization.post.mockRejectedValueOnce(new Error('组织选项读取失败'))

    renderPage()

    // 组件自身的 antd 静态 message.error 生效，且只提示一次（页面不重复提示同一失败）。
    await waitFor(() => expect(messageError).toHaveBeenCalledTimes(1))
    expect(messageError).toHaveBeenCalledWith('组织选项读取失败')

    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    // 操作列删除后写入口就是金额格：此时范围未解析、行集为空，页面没有任何可写入口（由上面的零请求与空态共同体现）。
    // 导出按钮（可访问名称含「金额」）不是写入口：范围未解析时它呈禁用态，不在此断言面内。
    expect(screen.queryAllByRole('button', { name: /修改金额/ })).toHaveLength(0)
    // 范围未解析：给出范围未选全的空态，页面不进入可操作状态。
    expect(screen.getByText('请选择组织、医院与院区后查看互认项目金额')).toBeInTheDocument()
  })
})

/**
 * 用例名只描述行为：平台管理员页的查询入口为关键字输入框内嵌的查询图标按钮（S3-D25 修订，
 * Client/design.md「筛选区」）。规格属性逐项断言；`autoInsertSpace={false}` 使纯图标按钮不插入
 * 多余空格；本页不再有独立查询主按钮与「重置」按钮。
 */
describe('查询图标按钮规格', () => {
  it('关键字输入框内嵌查询图标按钮：文本型按钮、搜索图标、可访问名称精确为「查询」，查询区无独立查询主按钮与重置按钮', async () => {
    await renderLoaded([amountRow()])

    const button = searchButton()
    expect(button).toHaveClass('ant-btn-text')
    expect(button).not.toHaveClass('ant-btn-primary')
    expect(button.querySelector('.anticon-search')).not.toBeNull()
    expect(button.getAttribute('aria-label')).toBe('查询')
    // 查询入口位于关键字输入框内（suffix 槽位），不是查询区的独立按钮。
    expect(keywordInput().closest('.ant-input-affix-wrapper')?.contains(button)).toBe(true)
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
  })
})

/**
 * 用例名只描述行为：平台管理员页查询区为「关键字输入框 + 金额状态下拉选择」的单行结构，
 * 互认配置状态筛选为贴在列表上沿的页签（S3-D25 修订，Client/design.md「筛选区」）：不以 inline Form
 * 承载，不渲染独立查询主按钮与「重置」按钮；金额状态下拉默认档「全部」，配置状态页签默认选中「全部」。
 */
describe('查询区结构', () => {
  it('查询区为关键字框加金额状态下拉选择，配置状态为列表上沿页签，不以 Form 承载，不渲染独立查询主按钮与重置按钮', async () => {
    await renderLoaded([amountRow()])

    const filterRegion = document.querySelector('.recognition-amounts-filter') as HTMLElement
    expect(filterRegion).not.toBeNull()
    expect(filterRegion.querySelector('form')).toBeNull()
    // 行内文字标签在下拉选择之外；配置状态不在查询区内，由列表上沿页签承载。
    expect(within(filterRegion).getByText('金额状态')).toBeInTheDocument()
    expect(within(filterRegion).queryAllByRole('combobox')).toHaveLength(1)
    expect(selectedSelectValue(amountStatusFilter())).toBe('全部')
    expect(selectedStatusTabLabels()).toEqual(['全部'])
    expect(screen.queryByRole('button', { name: '重置' })).toBeNull()
  })
})

describe('C27 关键字搜索', () => {
  it('提交下推关键字、服务端返回的行原样展示、清空后草稿与已生效关键字一并清空并按空条件重读', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '便常规' }),
    ])

    // 关键字只在提交时下推：输入草稿本身不追加读取，回车提交后请求体带上已生效关键字。
    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      keyword: '尿常规',
    })

    // 行编码与关键字字面无交集的行仍全部可见：本地不再按关键字裁剪，行集原样按列视图平铺。
    await settle(second, [
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '便常规' }),
    ])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(renderedCodes()).toEqual(['A01', 'A02'])
    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(screen.getByText('便常规')).toBeInTheDocument()

    // 清空输入框：草稿与已生效关键字同时清空，并触发一次不带关键字的空条件读取。
    const third = armQuery()
    await change(keywordInput(), '')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(keywordInput().value).toBe('')
    expect(query().mock.calls[2][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
    })
    expect(query().mock.calls[2][1]).not.toHaveProperty('keyword')
    await settle(third, [amountRow()])
  })
})

describe('C28 状态筛选本地裁剪', () => {
  it('金额状态下拉与配置状态页签都只在服务端结果上本地裁剪：切换即生效、不追加读取、不下推请求', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', isAmountConfigured: true, currentAmount: amount(12.5), configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', isAmountConfigured: false, currentAmount: null, configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A03', standardProjectName: '项目三', isAmountConfigured: true, currentAmount: amount(3), configurationStatus: 2 }),
    ])
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(query()).toHaveBeenCalledTimes(1)

    await pickSelectOption(amountStatusFilter(), '未配置')
    expect(renderedCodes()).toEqual(['A02'])
    expect(selectedSelectValue(amountStatusFilter())).toBe('未配置')

    await pickSelectOption(amountStatusFilter(), '已配置')
    expect(renderedCodes()).toEqual(['A01', 'A03'])

    clickConfigurationStatusTab('停用')
    expect(renderedCodes()).toEqual(['A03'])
    expect(selectedStatusTabLabels()).toEqual(['停用'])

    await pickSelectOption(amountStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A03'])

    clickConfigurationStatusTab('全部')
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(selectedStatusTabLabels()).toEqual(['全部'])

    // 本地裁剪全程不追加读取，也不把两个状态条件写进请求。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
    ])
  })
})

describe('C29 地址栏参数同步', () => {
  it('进入按参数初始化，变化替换当前历史项，草稿不写入地址栏', async () => {
    window.history.replaceState(null, '', '/?keyword=血常规&amountStatus=unconfigured&configurationStatus=enabled')
    const first = armQuery()
    renderPage()

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '血常规' },
    ])
    await settle(first, [amountRow({ standardProjectCode: 'A01', isAmountConfigured: false, currentAmount: null })])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    // 进入按参数初始化：草稿取已生效关键字，金额状态下拉与配置状态页签取地址栏档位。
    expect(keywordInput().value).toBe('血常规')
    expect(selectedSelectValue(amountStatusFilter())).toBe('未配置')
    expect(selectedStatusTabLabels()).toEqual(['启用'])
    expect(renderedCodes()).toEqual(['A01'])

    // 档位变化按替换当前历史项写回，且不追加读取。
    const historyLength = window.history.length
    await pickSelectOption(amountStatusFilter(), '已配置')
    expect(new URLSearchParams(window.location.search).get('amountStatus')).toBe('configured')
    expect(window.history.length).toBe(historyLength)
    expect(query()).toHaveBeenCalledTimes(1)

    // 草稿不写入地址栏：未提交的关键字只留在输入框。
    await change(keywordInput(), '尿常规')
    expect(new URLSearchParams(window.location.search).get('keyword')).toBe('血常规')
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('提交才下推关键字并同步地址栏，清空动作清掉已生效关键字并触发一次空条件读取', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)

    // 回车提交：草稿成为已生效关键字，随请求下推并替换写入地址栏。
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '尿常规' },
    ])
    expect(new URLSearchParams(window.location.search).get('keyword')).toBe('尿常规')
    await settle(second, [amountRow()])

    // 输入框清空动作同时清空草稿与已生效关键字，并触发一次空条件读取（等价于刷新）。
    const third = armQuery()
    await change(keywordInput(), '')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(keywordInput().value).toBe('')
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '尿常规' },
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
    ])
    expect(new URLSearchParams(window.location.search).get('keyword')).toBeNull()
    await settle(third, [amountRow()])
  })
})

describe('C30 院区恒定单值', () => {
  it('可信组织、医院、院区齐全时初始范围即为可信范围', async () => {
    trustedOrganizationRef.current = 'ORG-B'
    trustedHospitalRef.current = 'HOS-2'
    trustedBranchRef.current = 'BRA-3'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 首次读取按可信范围发起：组织、医院、院区三值都取可信值，不取首项组合（首项组合是 ORG-A / HOS-1 / BRA-1）。
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-B', hospitalCode: 'HOS-2', branchCode: 'BRA-3' },
    ])
    expect(scopeSelect('组织')).toHaveValue('ORG-B-ID')
    expect(scopeSelect('医院')).toHaveValue('HOS-2-ID')
    expect(scopeSelect('院区')).toHaveValue('BRA-3-ID')
    await settle(first, [amountRow()])
  })

  it('可信院区不属于当前组织、医院时取该院第一项', async () => {
    trustedOrganizationRef.current = 'ORG-A'
    trustedHospitalRef.current = 'HOS-2'
    trustedBranchRef.current = 'BRA-9'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 当前组织、医院取可信值；可信院区 BRA-9 不属于该院（清单是 BRA-2、BRA-3），院区取第一项 BRA-2。
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' },
    ])
    expect(scopeSelect('组织')).toHaveValue('ORG-A-ID')
    expect(scopeSelect('医院')).toHaveValue('HOS-2-ID')
    expect(scopeSelect('院区')).toHaveValue('BRA-2-ID')
    await settle(first, [amountRow()])
  })

  it('可信院区缺失时取当前组织、医院第一项', async () => {
    trustedOrganizationRef.current = 'ORG-A'
    trustedHospitalRef.current = 'HOS-2'
    trustedBranchRef.current = null
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    // 可信院区缺失：院区取当前组织、医院（可信 ORG-A / HOS-2）下的第一项 BRA-2。
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' },
    ])
    expect(scopeSelect('院区')).toHaveValue('BRA-2-ID')
    await settle(first, [amountRow()])
  })

  it('可信组织缺失时仍展示范围选择器并按可选范围读取', async () => {
    trustedOrganizationRef.current = null
    trustedBranchRef.current = 'BRA-3'
    const first = armQuery()
    renderPage()

    expect(await screen.findByRole('heading', { name: '互认项目金额维护' })).toBeInTheDocument()
    expect(scopeSelect('组织')).toBeInTheDocument()
    expect(scopeSelect('医院')).toBeInTheDocument()
    expect(scopeSelect('院区')).toBeInTheDocument()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
    })
    expect(save()).not.toHaveBeenCalled()
    await settle(first, [amountRow()])
  })

  it('可信医院缺失时仍展示范围选择器并按可选范围读取', async () => {
    trustedHospitalRef.current = null
    trustedBranchRef.current = 'BRA-3'
    const first = armQuery()
    renderPage()

    expect(scopeSelect('医院')).toBeInTheDocument()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    expect(query().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
    })
    await settle(first, [amountRow()])
  })

  it('切换组织或医院后院区自动重取：可信院区属于新范围则选中，否则新范围第一项', async () => {
    trustedBranchRef.current = 'BRA-4'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])

    const second = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    await settle(second, [amountRow()])

    // 切回原医院：可信院区重新属于当前范围，选中值重新取可信院区（该范围首项是 BRA-1）。
    const third = armQuery()
    await pickScopeOption('医院', 'HOS-1-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await settle(third, [amountRow()])

    const fourth = armQuery()
    await pickScopeOption('组织', 'ORG-B-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(4))
    expect(query().mock.calls[3][1]).toEqual({ organizationCode: 'ORG-B', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await settle(fourth, [amountRow()])
  })

  it('用户显式改选的院区被接受，清空院区被拒绝且保持原受控值', async () => {
    trustedOrganizationRef.current = 'ORG-A'
    trustedHospitalRef.current = 'HOS-1'
    trustedBranchRef.current = 'BRA-4'
    await renderLoaded([amountRow()])
    expect(query().mock.calls.map((call) => call[1])).toEqual([
      { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' },
    ])

    const second = armQuery()
    await pickScopeOption('院区', 'BRA-1-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    await settle(second, [amountRow()])

    // 组件的清空动作不被接受：保持原受控值，不发起空院区请求。
    await pickScopeOption('院区', '')
    expect(scopeSelect('院区')).toHaveValue('BRA-1-ID')
    expect(query()).toHaveBeenCalledTimes(2)
  })
})

describe('C31 页头范围展示移除', () => {
  it('页头不渲染范围展示：不挂载共用范围条、不出现三级名称与编码（S3-D23 修订）', async () => {
    await renderLoaded([amountRow()])

    const header = document.querySelector('.recognition-amounts-header') as HTMLElement
    expect(header).not.toBeNull()
    // 范围条（含「未知」占位形态）不再挂载；页头只保留标题与副标题。
    expect(screen.queryByText('未知 / 未知 / 未知')).toBeNull()
    expect(document.querySelector('.recognition-amounts-header-scope')).toBeNull()
    for (const code of ['ORG-A', 'HOS-1', 'BRA-1', 'ORG-B', 'HOS-2', 'BRA-2']) {
      expect(header.textContent).not.toContain(code)
    }
    expect(within(header).queryByText('组织', { exact: true })).toBeNull()
  })

  it('切换组织、医院选择后页头仍无范围展示：范围只由正文选择器承载（S3-D23 修订）', async () => {
    await renderLoaded([amountRow()])

    const second = armQuery()
    await pickScopeOption('组织', 'ORG-B-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])

    expect(screen.queryByText('未知 / 未知 / 未知')).toBeNull()
    const header = document.querySelector('.recognition-amounts-header') as HTMLElement
    expect(header.textContent).not.toContain('ORG-B')
  })
})

describe('认证状态不可用（平台页阻断）', () => {
  it('token 缺失时整页只渲染标题与错误 Alert，零业务请求', async () => {
    authState.token = null
    renderPage()

    expect(await screen.findByRole('heading', { name: '互认项目金额维护', level: 3 })).toBeInTheDocument()
    const alert = within(document.querySelector('.recognition-amounts-page') as HTMLElement).getByRole('alert')
    expect(alert).toHaveClass('ant-alert-error')
    expect(alert.textContent).toBe('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByLabelText('范围-组织')).not.toBeInTheDocument()
  })
})

describe('C32 行内编辑金额', () => {
  it('点击当前金额值与未配置标记进入编辑，Esc 取消后编辑态收起且金额不变', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: amount(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')
    await change(inlineAmountInput(), '99')
    await pressEscape(inlineAmountInput())
    expect(queryInlineAmountInput()).toBeNull()
    expect(screen.getByText('12.50')).toBeInTheDocument()
    expect(save()).not.toHaveBeenCalled()

    // 「未配置」标记同样是入口，进入编辑时回填空值。
    editAmount('未配置项目')
    expect(inlineAmountInput().value).toBe('')
    await pressEscape(inlineAmountInput())
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('回车提交当前值（同值不改）仍发出写请求并收起编辑态', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 12.5,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.getByText('12.50')).toBeInTheDocument()
  })

  it('后台刷新改变同一行金额后编辑器以当前金额值为键重挂并回填新值', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '999')

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])

    // 目标行仍在当前行集：编辑态保留，但以新金额值重挂，回填值随当前行数据更新。
    await waitFor(() => expect(inlineAmountInput().value).toBe('20.00'))
    expect(save()).not.toHaveBeenCalled()
  })

  it('状态档把目标行裁出当前行集后不渲染编辑态', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: amount(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    await pickSelectOption(amountStatusFilter(), '未配置')
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
  })

  it('切换配置状态页签即收起进行中的行内编辑并放弃未保存输入（目标行仍在新档位行集内）', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: amount(12.5), isAmountConfigured: true, configurationStatus: 1 }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()
    await change(inlineAmountInput(), '99')

    // 目标行属于新档位的可见行集，但切换页签本身就收起编辑态、放弃未保存输入（S3-D25 2026-10-13 修订）。
    clickConfigurationStatusTab('启用')
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(renderedCodes()).toEqual(['A01'])
    expect(screen.getByText('12.50')).toBeInTheDocument()
  })

  it('搜索把目标行移出当前行集后不渲染编辑态', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    await change(keywordInput(), 'ZZZ')
    const pending = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(pending, [amountRow({ standardProjectCode: 'B02', standardProjectName: '其他项目' })])
    await screen.findByText('其他项目')

    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
  })

  it('金额只接受非负且最多两位小数的数字输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    await change(inlineAmountInput(), '-1')
    await pressEnter(inlineAmountInput())
    // 就地提示渲染在当前行内（S3-D27 2026-10-12 修订）。
    await within(rowOf('血常规')).findByText('金额必须是不小于 0 且最多两位小数的数字')
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)

    // 改成合法值后行内标记随之清除，编辑态继续保留。
    await change(inlineAmountInput(), '1.5')
    await waitFor(() => expect(inlineAmountInput()).not.toHaveAttribute('aria-invalid', 'true'))
    expect(inlineAmountInput().value).toBe('1.5')
  })

  it('保存按钮与回车同一条提交路径，取消按钮与 Esc 同一条放弃路径', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    // 取消按钮：收起编辑态、丢弃输入，与 Esc 同路径。
    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    click(screen.getByRole('button', { name: '取消修改金额：血常规' }))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    // 保存按钮：与回车同一条提交路径，成功后收起编辑态并整表重载。
    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')
    await change(inlineAmountInput(), '15')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    click(screen.getByRole('button', { name: '保存金额：血常规' }))

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 15,
    })
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(15), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('15.00')
  })

  it('保存与取消为图标按钮，悬停提示与可访问名称同一文案', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')

    // 保存为图标主按钮、取消为图标默认按钮（S3-D27 2026-10-13 修订）：可访问名仍由 aria-label
    // 组成（上面的按钮路径用例已覆盖点击行为），按钮文本不再出现文字。
    const saveButton = screen.getByRole('button', { name: '保存金额：血常规' })
    expect(saveButton).toHaveClass('ant-btn-primary', 'ant-btn-sm')
    expect(saveButton.querySelector('.anticon-check')).not.toBeNull()
    expect(saveButton.textContent).not.toContain('保存')

    const cancelButton = screen.getByRole('button', { name: '取消修改金额：血常规' })
    expect(cancelButton).toHaveClass('ant-btn-sm')
    expect(cancelButton).not.toHaveClass('ant-btn-primary')
    expect(cancelButton.querySelector('.anticon-close')).not.toBeNull()
    expect(cancelButton.textContent).not.toContain('取消')

    // 悬停提示与可访问名称同一文案；离开保存按钮再悬停取消按钮，离开动画在 jsdom 没有
    // transitionend、残影可能滞留，按文案在任一 tooltip 节点中出现判定。
    fireEvent.mouseEnter(saveButton)
    expect(await screen.findByRole('tooltip')).toHaveTextContent('保存金额：血常规')
    fireEvent.mouseLeave(saveButton)
    fireEvent.mouseEnter(cancelButton)
    await waitFor(() =>
      expect(screen.getAllByRole('tooltip').some((node) => node.textContent?.includes('取消修改金额：血常规'))).toBe(true),
    )
  })

  it('保存按钮禁用态下悬停仍出现同一文案的悬停提示', async () => {
    await renderLoaded([amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    const pending = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 读取在途即写门控禁用保存按钮；真实浏览器里 disabled 按钮不派发悬停事件，悬停提示
    // 须由包裹元素承接，文案仍与可访问名称一致（S3-D27 2026-10-13 修订）。
    const saveButton = screen.getByRole('button', { name: '保存金额：血常规' })
    await waitFor(() => expect(saveButton).toBeDisabled())
    fireEvent.mouseEnter(saveButton)
    expect(await screen.findByRole('tooltip')).toHaveTextContent('保存金额：血常规')

    await settle(pending, [amountRow({ currentAmount: amount(12.5), isAmountConfigured: true })])
  })
})

describe('C33 可信范围变化确认框', () => {
  /** 可信范围变化用受控构造：编辑期间让页面读到的可信院区值改变，生产代码无需提供注入点。 */
  it('有未保存内容时可信范围变化弹出确认框，放弃并切换即清空输入并按新范围加载', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    trustedBranchRef.current = 'BRA-4'
    const next = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // 确认框存续期间：既不加载新范围，也不发起写入，更不进入待切换提示态。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()
    expect(promptElement()).toBeNull()

    click(within(dialog).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })

    // 放弃即清空未保存输入，直接按新范围加载，没有残留的确认框与提示态。
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('确认框选取消后保留输入并进入待切换提示态，再次确认后按新范围加载', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    trustedBranchRef.current = 'BRA-4'
    const next = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // antd 对恰为两个汉字的按钮文案自动插入空格（与既有「保 存」同一手法），匹配时容忍该空格。
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))

    // 取消：保留未保存输入与编辑态，进入待切换提示态，范围尚未切换。
    expect(inlineAmountInput().value).toBe('99')
    expect(promptElement()).not.toBeNull()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()

    // 提示态里再次确认：清空输入并按新范围加载，确认框不重复出现。
    const prompt = promptElement()
    expect(prompt).not.toBeNull()
    click(within(prompt as HTMLElement).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    await waitFor(() => expect(promptElement()).toBeNull())
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(queryInlineAmountInput()).toBeNull()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('待切换提示态下未保存输入消失即自动按新范围加载', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    trustedBranchRef.current = 'BRA-4'
    const next = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    expect(inlineAmountInput().value).toBe('99')

    // 用户用 Esc 收起编辑：未保存内容消失，提示态自动完成切换。
    await pressEscape(inlineAmountInput())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    await waitFor(() => expect(promptElement()).toBeNull())
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('没有未保存内容时可信范围变化直接切换，不弹确认框', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    trustedBranchRef.current = 'BRA-4'
    const next = armQuery()
    await pickScopeOption('医院', 'HOS-2-ID')

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })

  it('确认框与待切换提示态期间回车提交零写请求，入口金额按钮禁用', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([
      amountRow(),
      amountRow({ standardProjectCode: 'B02', standardProjectName: '便常规' }),
    ])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    trustedBranchRef.current = 'BRA-4'
    await pickScopeOption('医院', 'HOS-2-ID')

    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    // 预排一次读取：写入路径若真走到整表重载，让它停在在途，失败现场只留下写调用本身。
    armQuery()
    // 确认框挂起期间：回车零写返回，未在编辑的行其入口金额按钮禁用；行内输入仍可编辑。
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
    expect(promptElement()).toBeNull()

    // 转入待切换提示态后同一口径继续生效。
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()
    armQuery()
    await pressEnter(inlineAmountInput())
    expect(save()).not.toHaveBeenCalled()
    expect(amountValueCell('便常规')).toBeDisabled()
    expect(inlineAmountInput()).not.toBeDisabled()
  })

  it('只推进 token、不碰选择器：有未保存内容时渲染期即弹出可信范围变化确认框', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只按宿主重新签发 token 的路径推进可信院区（Client/design.md 第 34 行），全程没有任何选择器交互。
    await resignTrustedToken('BRA-4')

    await findModal(CONFIRM_TRUSTED_SCOPE)
    // 确认框由渲染期派生弹出：期间既不加载新范围，也不发起写入，更不进入待切换提示态。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(save()).not.toHaveBeenCalled()
    expect(promptElement()).toBeNull()
    // 受控范围仍停在原院区——确认框出现之前没有发生过任何选择器交互。
    expect(scopeSelect('院区')).toHaveValue('BRA-1-ID')
  })

  it('只推进 token、不碰选择器：无未保存内容时按 S3-D22 重取院区且不弹确认框', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 只按宿主重新签发 token 的路径推进可信院区，全程没有任何选择器交互；重载读取先排队再触发。
    const next = armQuery()
    await resignTrustedToken('BRA-4')

    // 当前组织、医院不变；可信院区 BRA-4 属于该院区清单：直接选中它并据此重载（S3-D22）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    expect(scopeSelect('院区')).toHaveValue('BRA-4-ID')
    expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow()])
    await screen.findByText('血常规')
  })

  it('token 缺失且管理器仍有院区值：整页阻断且页面稳定——单次读取、不弹确认框、不进入无限重排', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 宿主清掉 token，管理器里缓存的院区值还在：订阅值为 null，凭据不再表达可信范围，整页阻断。
    await act(async () => {
      authState.token = null
      authState.notify()
    })
    expect(await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')).toBeInTheDocument()

    // 让出若干轮宏任务：渲染重排若在持续，每一轮的 setTimeout(0) 都会被观察到。
    // 阻断面（面板、零新增读取）与稳定面（无确认框、无选择器）用 expect.soft 并列报告，
    // 任一面的失败都不被前一个失败掩盖。
    await act(async () => {
      for (let round = 0; round < 5; round += 1) {
        await new Promise((resolve) => { window.setTimeout(resolve, 0) })
      }
    })
    expect.soft(query()).toHaveBeenCalledTimes(1)
    expect.soft(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect.soft(screen.queryByLabelText('范围-组织')).not.toBeInTheDocument()
    expect.soft(save()).not.toHaveBeenCalled()
  })

  it('工作身份切换到另一组织与医院：无未保存内容时按新会话重算三级范围', async () => {
    trustedOrganizationRef.current = 'ORG-A'
    trustedHospitalRef.current = 'HOS-1'
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    const next = armQuery()
    await act(async () => {
      trustedOrganizationRef.current = 'ORG-B'
      trustedHospitalRef.current = 'HOS-2'
      trustedBranchRef.current = 'BRA-2'
      authState.notify()
    })

    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-B', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    await settle(next, [amountRow()])
  })

  it('只推进 token、有未保存内容：确认框里不碰选择器直接放弃并切换，院区按 S3-D22 现解析', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进 token：确认框由渲染期派生弹出，全程没有任何选择器交互，挂起的目标范围为空。
    const next = armQuery()
    await resignTrustedToken('BRA-4')
    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)

    click(within(dialog).getByRole('button', { name: '放弃并切换' }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))

    // 放弃并切换：按 S3-D22 现解析院区——可信 BRA-4 属于当前组织医院的清单，选中它并据此读取。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await waitFor(() => expect(scopeSelect('院区')).toHaveValue('BRA-4-ID'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(promptElement()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(next, [amountRow({ standardProjectCode: 'C01', standardProjectName: '新范围项目' })])
    await screen.findByText('新范围项目')
  })
})

describe('C34 提交前现值复核', () => {
  /**
   * 复核出口在本描述块接到真实实现（`vi.importActual`；文件头第 1 条的替身仍适用于其余描述块）：
   * C34 三场景断言的是「页面入参两侧 → 出口结论 → 是否零写」这条整链，替身给值会把出口结论
   * 一并测空；逐层比对与适配层原因串的完整覆盖仍归 recognitionAmountsApi.test.ts 的复核用例。
   * 行内展示的原因由本页书写、不透传出口交付的原因串。
   */
  beforeEach(async () => {
    const actual = await vi.importActual<typeof import('./recognitionAmountsApi')>('./recognitionAmountsApi')
    api.recheckRecognitionAmountScope.mockImplementation(actual.recheckRecognitionAmountScope)
  })

  /** 拦截提示必须齐备的三要素（Client/testPlan.md 第 42 行 C34 三场景）；提示以 message.error 承载。 */
  const REASON_PATTERN = /可信范围已变化.*本次不提交写入.*切换到新可信范围/

  it('场景一：可信范围已变化且页面未应用（记录 BRA-1、现读缺失）：零写请求，以 message.error 给出切换到新可信范围的原因', async () => {
    // 应用范围时记录的可信院区为 BRA-1；随后现读接缝推进到缺失并通知订阅者，记录值与现读值不再一致。
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    await act(async () => {
      trustedBranchRef.current = null
      authState.notify()
    })

    editAmount('血常规')
    await change(inlineAmountInput(), '18.00')
    // 复核出口按真实实现给出失败结论：入参两侧由页面提供，失败时行内原因由页面书写。
    await pressEnter(inlineAmountInput())

    // 零写请求；复核入参两侧分别是记录的可信院区与现读可信院区，用户选定的三值不参与比对。
    expect(save()).not.toHaveBeenCalled()
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledTimes(1)
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledWith({
      applied: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      trusted: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: null },
      layers: ['organization', 'hospital', 'branch'],
    })
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截原因以 message.error 反馈，且不再渲染在单元格内；旧版「页面范围为…请刷新页面后重试」不再出现。
    expect(await screen.findByText(REASON_PATTERN)).toBeInTheDocument()
    expect(within(rowOf('血常规')).queryByText(REASON_PATTERN)).toBeNull()
    expect(within(rowOf('血常规')).queryByText(/请刷新页面后重试/)).not.toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('18.00')
  })

  it('场景二：可信院区缺失已应用——记录的可信院区与现读同为缺失：不触发拦截，照常提交一次', async () => {
    // S3-D22 可信院区缺失情形：可信组织与医院在、可信院区缺失；页面按当前组织医院下的首项选定院区（BRA-1）。
    trustedOrganizationRef.current = 'ORG-A'
    trustedHospitalRef.current = 'HOS-1'
    trustedBranchRef.current = null
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    // 复核入参两侧同为缺失：院区层按缺失值参与比对，用户选定的首项院区不写入记录侧。
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledTimes(1)
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledWith({
      applied: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: null },
      trusted: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: null },
      layers: ['organization', 'hospital', 'branch'],
    })
    // 不触发拦截：保存照常提交一次，载荷用页面选定的首项院区。
    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-1',
      standardProjectCode: 'A01',
      currentAmount: 99,
    })
    expect(screen.queryByText(REASON_PATTERN)).not.toBeInTheDocument()
    // 提交成功后收起编辑态并整表重载。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: amount(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })

  it('场景三：用户选定院区不同于可信默认值、记录与现读一致：不触发拦截，照常提交一次', async () => {
    // 可信院区 BRA-1 即初始默认值；页面应用范围时把它记录下来。
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 用户跨院区维护：选定 BRA-4，应用范围时记录的可信院区仍是 BRA-1（用户选定值不写入记录）。
    const second = armQuery()
    await pickScopeOption('院区', 'BRA-4-ID')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await settle(second, [amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '8')
    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    // 记录与现读一致：不触发拦截，保存照常提交一次，载荷用用户选定的三值。
    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    expect(save().mock.calls[0][1]).toEqual({
      organizationCode: 'ORG-A',
      hospitalCode: 'HOS-1',
      branchCode: 'BRA-4',
      standardProjectCode: 'A01',
      currentAmount: 8,
    })
    expect(screen.queryByText(REASON_PATTERN)).not.toBeInTheDocument()
    // 复核入参两侧仍分别是记录的可信院区与现读可信院区（同为 BRA-1），用户选定的 BRA-4 不参与。
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledTimes(1)
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledWith({
      applied: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      trusted: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' },
      layers: ['organization', 'hospital', 'branch'],
    })
    // 提交成功后收起编辑态并整表重载。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [amountRow({ currentAmount: amount(8), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('8.00')
  })

  it('记录的可信院区缺失而现读已有值（可信院区缺失后范围变化、页面未应用）：零写请求，以 message.error 给出已变化的原因', async () => {
    // 可信院区缺失已应用：记录侧为缺失；随后宿主给出新的可信院区，页面尚未应用新可信范围。
    trustedBranchRef.current = null
    await renderLoaded([amountRow()])

    editAmount('血常规')
    await change(inlineAmountInput(), '99')

    // 只推进命令式现读的取值、不通知订阅者：渲染期比对毫无察觉，C33 不弹，只有提交时的现读读到新值。
    trustedBranchRef.current = 'BRA-3'
    armQuery()
    await pressEnter(inlineAmountInput())

    expect(save()).not.toHaveBeenCalled()
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledTimes(1)
    expect(api.recheckRecognitionAmountScope).toHaveBeenCalledWith({
      applied: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: null },
      trusted: { organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-3' },
      layers: ['organization', 'hospital', 'branch'],
    })
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截原因以 message.error 反馈，不再渲染在单元格内。
    expect(await screen.findByText(REASON_PATTERN)).toBeInTheDocument()
    expect(within(rowOf('血常规')).queryByText(REASON_PATTERN)).toBeNull()
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('99')
  })
})

describe('C35 页脚计数', () => {
  it('空列表时页脚显示共 0 项', async () => {
    await renderLoaded([])

    expect(screen.getByText('共 0 项')).toBeInTheDocument()
    expect(dataRows()).toHaveLength(0)
  })
  it('页脚按生效筛选后的可见行数显示共 N 项', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: amount(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: amount(20) }),
      amountRow({ standardProjectCode: 'A03', isAmountConfigured: false, currentAmount: null }),
    ])

    expect(screen.getByText('共 3 项')).toBeInTheDocument()

    await pickSelectOption(amountStatusFilter(), '未配置')
    expect(screen.getByText('共 1 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)

    await pickSelectOption(amountStatusFilter(), '全部')
    expect(screen.getByText('共 3 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C36 读取在途写门控', () => {
  it('读取在途时框内查询图标按钮呈加载态、金额格不进入编辑态，读取结束才恢复', async () => {
    await renderLoaded([amountRow()])

    const pending = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    await settle(pending, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeEnabled()
    click(amountValueCell('血常规'))
    expect(inlineAmountInput()).toBeInTheDocument()
  })
})

describe('页面标题层级', () => {
  it('页面大标题为 level 3', async () => {
    await renderLoaded([amountRow()])

    expect(screen.getByRole('heading', { name: '互认项目金额维护', level: 3 })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: '互认项目金额维护', level: 2 })).toBeNull()
  })

  it('标题 margin 归零：配套 CSS 的归零选择器随标题层级改为 h3', async () => {
    await renderLoaded([amountRow()])

    // jsdom 无布局：margin 归零按配套 CSS 的静态规则断言（Testing Baseline §1.4 静态证据）。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/recognitionAmounts/RecognitionAmounts.css'), 'utf8')
    const rule = css.match(/\.recognition-amounts-header\s+h3\.ant-typography\s*\{[^}]*\}/)?.[0] ?? ''
    expect(rule).toContain('margin: 0')
    expect(css).not.toMatch(/\.recognition-amounts-header\s+h2\.ant-typography/)
  })
})

describe('表格样式覆写提级', () => {
  it('覆写 antd 默认样式的选择器提级到 .ant-table-wrapper 根', () => {
    // jsdom 无布局：间距与表头底色按配套 CSS 的静态规则断言（Testing Baseline §1.4 静态证据）。
    // antd 运行时注入的样式与未提级的覆写选择器同优先级且注入顺序靠后，未提级时宿主计算值仍是
    // antd 默认值（宿主验收实测证据），提级后本页声明的间距与底色才生效。
    const css = readFileSync(resolve(process.cwd(), 'src/pages/recognitionAmounts/RecognitionAmounts.css'), 'utf8')
    const headCellRule = css.match(/\.recognition-amounts-table\.ant-table-wrapper \.ant-table-thead > tr > th\s*\{[^}]*\}/)?.[0] ?? ''
    expect(headCellRule).toContain('padding: 10px 12px')
    expect(headCellRule).toContain('background: #f5f5f5')
    const bodyCellRule = css.match(/\.recognition-amounts-table\.ant-table-wrapper \.ant-table-tbody > tr > td\s*\{[^}]*\}/)?.[0] ?? ''
    expect(bodyCellRule).toContain('padding: 10px 12px')
  })
})

/**
 * 列宽与配套版面与「互认项目」页统一（S3-D24 2026-10-13 修订，Client/design.md「列表」）：
 * 列宽按渲染出的 col 内联宽度断言；外边距、字号、颜色等 jsdom 无布局的取值按配套 CSS 与组件
 * 文本的静态规则断言（Testing Baseline §1.4 静态证据，与「表格样式覆写提级」同一机制）。
 */
describe('列表版面与互认项目页统一', () => {
  it('类型、分类、分组列宽为 88/186/208，项目列弹性填充不带内联宽度', async () => {
    await renderLoaded([amountRow()])

    const widths = [...document.querySelectorAll<HTMLElement>('.recognition-amounts-table colgroup col')]
      .map((col) => col.style.width)
    // col 按列序渲染：类型、分类、分组、项目、当前金额、互认配置状态、当前不可用原因。
    expect(widths).toEqual(['88px', '186px', '208px', '', '190px', '130px', '190px'])
  })

  it('互认配置状态徽标颜色与「互认项目」页同一推导：启用 success、停用默认色', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '启用项目', configurationStatus: 1, configurationStatusText: '启用' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '停用项目', configurationStatus: 2, configurationStatusText: '停用' }),
    ])

    expect(rowOf('启用项目').querySelector('.ant-tag')?.classList.contains('ant-tag-success')).toBe(true)
    const disabledTag = rowOf('停用项目').querySelector('.ant-tag')
    expect(disabledTag?.classList.contains('ant-tag-success')).toBe(false)
    expect(disabledTag?.classList.contains('ant-tag-warning')).toBe(false)
  })

  it('横向滚动基准与配套 CSS 版面规则取「互认项目」页同值', async () => {
    await renderLoaded([amountRow()])

    // 横向滚动基准按页面实际渲染断言（S3-D29 后列表经共用组件渲染，与医院管理员页先例同一手法）：
    // rc-table 把该基准写成表格元素的内联宽度。
    const scrollTable = document.querySelector<HTMLElement>('.recognition-amounts-table .ant-table-content > table')
    expect(scrollTable?.style.width).toBe('1252px')

    const css = readFileSync(resolve(process.cwd(), 'src/pages/recognitionAmounts/RecognitionAmounts.css'), 'utf8')
    expect(css).toMatch(/\.recognition-amounts-table\.ant-table-wrapper\s*\{\s*margin-top: 14px;?\s*\}/)
    // 页签贴合规则带 .ant-tabs 提级（与表格覆写提级同一根因：antd cssinjs 注入顺序靠后时，
    // 未提级的同优先级覆写会被 antd 默认样式压住；导航切换路径下宿主实测过失效）。
    expect(css).toMatch(/\.recognition-amounts-status-tabs\.ant-tabs\s*\{\s*margin-bottom: -1px;?\s*\}/)
    expect(css).toMatch(/\.recognition-amounts-status-tabs\.ant-tabs > \.ant-tabs-nav\s*\{\s*margin-bottom: 0;?\s*\}/)
    expect(css).toMatch(/\.recognition-amounts-filter\s*\{[^}]*gap: 8px/)
    expect(css).toMatch(/\.recognition-amounts-filter\s*\{[^}]*margin: 16px 0 12px/)
    // 关键字框宽度规则同样带命名空间提级（与表格覆写、页签贴合同一根因）：antd 的
    // `:where(...).ant-input-affix-wrapper { width: 100% }` 与未提级的同优先级覆写并存时，
    // 后注入的 antd 样式获胜，宿主实测关键字框被压成整行宽（1770px）而设计值为 320px。
    expect(css).toMatch(/\.recognition-amounts-filter \.recognition-amounts-keyword\s*\{[^}]*width: 320px/)
    expect(css).toMatch(/\.recognition-amounts-project-code\s*\{[^}]*color: #6b7280/)
    expect(css).toMatch(/\.recognition-amounts-project-code\s*\{[^}]*font-size: 12px/)
    const countRule = css.match(/\.recognition-amounts-count\s*\{[^}]*\}/)?.[0] ?? ''
    expect(countRule).toContain('margin-top: 12px')
    expect(countRule).toContain('tabular-nums')
    expect(countRule).not.toContain('text-align: right')
  })
})

/*
 * 平台金额页导出链的组件层用例（页面与交互口径见项目导出阶段 Client 设计「导出按钮门控与状态」
 * 与「点击导出的行为」两节）：
 * 1. 导出按钮承载在配置状态页签行右侧（与互认项目页同款式，经两个金额页共用的列表视图组件一次落地），
 *    标准按钮语义与可访问名称，键盘焦点顺序随 DOM 顺序；
 * 2. 门控：范围未选全、可信范围已变化且页面尚未应用（待切换提示态）、读取失败或写入后刷新失败
 *    禁用且零请求；导出在途 loading 防重复；列表读取在途与行内编辑进行中不限制导出，既有写门控零改动；
 * 3. 请求体为选定范围三值加已生效关键字（配置状态页签与金额状态档位不参与）；取得字节后经共享
 *    下载模块保存，下载名按发起时刻给定；失败结束加载、保留列表与筛选、不新增页面级提示层；
 *    导出不写本页路由查询参数。
 */
/** 导出按钮按可访问名称定位：名称表达页面与动作（可见文本为「导出」）。 */
const exportButton = () => screen.getByRole('button', { name: '导出互认项目金额' })
const queryExportButton = () => screen.queryByRole('button', { name: '导出互认项目金额' })

/**
 * 「不新增页面级错误层」按页面根容器断言：页面自身的 message holder 挂在列表视图根部，
 * 而同文件更早用例经 antd 静态 message 弹出的提示渲染在 body 上、jsdom 无动画不消失，
 * 按 document 断言会命中与本用例无关的残留节点。
 */
const pageRoot = () => document.querySelector('.recognition-amounts-page') as HTMLElement
const queryPageAlert = () => within(pageRoot()).queryByRole('alert')

/** 排队一次导出交付，并断言当前没有任何导出请求被发起（禁用态零请求用例的公共收尾）。 */
async function expectNoAmountExportRequest() {
  api.exportRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))
  click(exportButton())
  await act(async () => {})
  expect(api.exportRecognitionAmounts).not.toHaveBeenCalled()
}

describe('平台金额页导出的承载位置与请求体', () => {
  it('导出按钮与配置状态页签同处一个工具行、随 DOM 顺序在页签之后，页面其余区块不变', async () => {
    await renderLoaded([amountRow()])

    const button = exportButton()
    // 标准按钮语义：承载元素就是 button（可见文本「导出」，可访问名称补充页面语境）。
    expect(button.tagName).toBe('BUTTON')
    // 同一行承载：按钮与页签容器同处一个工具行（页签行右侧由该容器的两端布局承载）。
    const toolbar = button.closest('.recognition-amounts-status-toolbar')
    expect(toolbar).not.toBeNull()
    const group = within(toolbar as HTMLElement).getByRole('group', { name: '配置状态筛选' })
    // DOM 顺序：页签在前、按钮在后（键盘焦点顺序随 DOM 顺序）。
    expect(group.compareDocumentPosition(button) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0)
    // 页面其余区块不因导出能力变化：七列列头与页脚计数保持原样。
    expect(columnHeaders()).toEqual(['类型', '分类', '分组', '项目', '当前金额', '互认配置状态', '当前不可用原因'])
    expect(screen.getByText(/共 \d+ 项/)).toBeInTheDocument()
  })

  it('请求体为选定范围三值加已生效关键字；配置状态页签与金额状态档位不参与请求', async () => {
    await renderLoaded([amountRow()])
    await change(keywordInput(), '血')
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])

    // 两个档位只是本地裁剪：在「停用」页签与金额状态「已配置」档位下导出，档位都不进入请求。
    clickConfigurationStatusTab('停用')
    await pickSelectOption(amountStatusFilter(), '已配置')
    api.exportRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))

    // 页面把 client 原样交给适配层出口；请求体恰为范围三值与已生效关键字。
    expect(api.exportRecognitionAmounts.mock.calls[0][0]).toBe(clientRef.current)
    const payload = api.exportRecognitionAmounts.mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1', keyword: '血' })
    expect(Object.keys(payload)).toEqual(['organizationCode', 'hospitalCode', 'branchCode', 'keyword'])
  })

  it('已生效关键字为空时请求体只有范围三值', async () => {
    await renderLoaded([amountRow()])
    api.exportRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))

    const payload = api.exportRecognitionAmounts.mock.calls[0][1] as Record<string, unknown>
    expect(payload).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    expect(Object.keys(payload)).toEqual(['organizationCode', 'hospitalCode', 'branchCode'])
  })

  it('范围未选全（院区清单为空致三值不齐）导出按钮禁用且零请求', async () => {
    // 院区清单为空：选择器解析不出院区，页面不接受三值不齐的回调，列表停留在范围空态。
    baseClient = createBaseClient()
    baseClient.api.organization.queryAllValidBranchByOrgId.post = vi.fn(async () => [])
    baseClientRef.current = baseClient

    renderPage()
    await screen.findByText('请选择组织、医院与院区后查看互认项目金额')

    expect(exportButton()).toBeDisabled()
    await expectNoAmountExportRequest()
  })

  it('导出在途按钮进入 loading 且再次点击不发起新请求，完成后恢复可用', async () => {
    await renderLoaded([amountRow()])
    const payload = deferred<ArrayBuffer>()
    api.exportRecognitionAmounts.mockReturnValueOnce(payload.promise)

    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))
    expect(exportButton()).toHaveClass('ant-btn-loading')
    expect(exportButton()).toBeDisabled()

    click(exportButton())
    await act(async () => {})
    expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1)

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())
  })
})

describe('平台金额页导出成功后的保存与失败恢复', () => {
  it('取得字节后经共享下载模块触发保存，文件名为「互认项目金额维护 + 发起时刻 14 位时间戳 + .xlsx」', async () => {
    await renderLoaded([amountRow()])
    const bytes = new ArrayBuffer(8)
    api.exportRecognitionAmounts.mockResolvedValueOnce(bytes)

    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))

    // 字节原样交给共享模块；下载名按发起时刻构造（时间戳精确格式由适配层用例固定）。
    expect(exportDownload.saveExportWorkbook.mock.calls[0][0]).toBe(bytes)
    expect(exportDownload.saveExportWorkbook.mock.calls[0][1]).toMatch(/^互认项目金额维护\d{14}\.xlsx$/)
    await waitFor(() => expect(exportButton()).toBeEnabled())
  })

  it('导出请求失败：结束加载、保留列表与已生效筛选，不重读、不新增页面级错误层', async () => {
    await renderLoaded([amountRow()])
    await change(keywordInput(), '血')
    const second = armQuery()
    await submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])
    api.exportRecognitionAmounts.mockRejectedValueOnce(new Error('业务拒绝：当前条件下无可导出数据。'))

    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())

    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect((screen.getByLabelText('关键字筛选') as HTMLInputElement).value).toBe('血')
    // 导出失败不触发列表重读：读取只有进入页面与关键字提交两次。
    expect(query()).toHaveBeenCalledTimes(2)
    // 页面不解析错误响应体、不新增提示层：页面根容器内不出现 Alert。
    expect(queryPageAlert()).not.toBeInTheDocument()
  })

  it('共享下载模块对 0 字节内容按契约错误抛出时走同一失败路径，不产生保存成功假象', async () => {
    await renderLoaded([amountRow()])
    api.exportRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(0))
    exportDownload.saveExportWorkbook.mockRejectedValueOnce(new Error('导出未返回文件内容。'))

    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(exportButton()).toBeEnabled())

    expect(exportDownload.saveExportWorkbook.mock.calls[0][0].byteLength).toBe(0)
    expect(screen.getByText('血常规')).toBeInTheDocument()
    // 契约错误同走失败路径：不重读、不新增页面级错误层。
    expect(query()).toHaveBeenCalledTimes(1)
    expect(queryPageAlert()).not.toBeInTheDocument()
  })
})

describe('平台金额页导出与页面其他状态的共存', () => {
  it('列表读取在途与行内编辑进行中不限制导出，导出在途也不阻塞列表读取与金额写入口', async () => {
    await renderLoaded([amountRow()])

    // 列表读取在途：导出保持可用（导出是只读链，不与读取在途互斥）。
    const refresh = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(exportButton()).toBeEnabled()
    await settle(refresh, [amountRow()])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    // 行内编辑进行中（无范围变化）：导出不受限制。
    editAmount('血常规')
    await screen.findByLabelText('金额')
    expect(exportButton()).toBeEnabled()

    // 导出在途：既有写门控不因导出改变——提交查询仍会发起一次列表读取。
    const payload = deferred<ArrayBuffer>()
    api.exportRecognitionAmounts.mockReturnValueOnce(payload.promise)
    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [amountRow()])

    // 收起行内编辑后写入口按既有门控恢复：导出在途不禁用它。
    click(screen.getByRole('button', { name: '取消修改金额：血常规' }))
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
  })

  it('导出在途时切换配置状态页签不被阻塞，也不发起第二次导出', async () => {
    await renderLoaded([amountRow()])
    const payload = deferred<ArrayBuffer>()
    api.exportRecognitionAmounts.mockReturnValueOnce(payload.promise)

    click(exportButton())
    await waitFor(() => expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1))

    clickConfigurationStatusTab('停用')
    expect(selectedStatusTabLabels()).toEqual(['停用'])
    expect(api.exportRecognitionAmounts).toHaveBeenCalledTimes(1)

    await act(async () => {
      payload.resolve(new ArrayBuffer(4))
    })
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
  })

  it('导出动作与条件不写入本页路由查询参数', async () => {
    await renderLoaded([amountRow()])
    api.exportRecognitionAmounts.mockResolvedValueOnce(new ArrayBuffer(4))

    expect(window.location.search).toBe('')
    click(exportButton())
    await waitFor(() => expect(exportDownload.saveExportWorkbook).toHaveBeenCalledTimes(1))
    expect(window.location.search).toBe('')
  })

  it('可信范围已变化且页面尚未应用（待切换提示态）导出按钮禁用且零请求', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 行内编辑输入未保存：可信院区变化后页面停在确认框，取消后进入待切换提示态，
    // 页面仍展示已应用范围的列表（导出所取范围与页面展示不一致）。
    editAmount('血常规')
    await change(inlineAmountInput(), '99')
    await resignTrustedToken('BRA-4')
    const dialog = await findModal(CONFIRM_TRUSTED_SCOPE)
    click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    await waitFor(() => expect(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false))
    expect(promptElement()).not.toBeNull()

    expect(query()).toHaveBeenCalledTimes(1)
    expect(exportButton()).toBeDisabled()
    await expectNoAmountExportRequest()
  })

  it('读取失败导出按钮禁用且零请求', async () => {
    await renderLoaded([amountRow()])

    const failing = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failQuery(failing)
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))

    expect(exportButton()).toBeDisabled()
    await expectNoAmountExportRequest()
    expect(screen.getByText('血常规')).toBeInTheDocument()
  })

  it('写入后刷新失败导出按钮禁用且零请求', async () => {
    await renderLoaded([amountRow()])

    // 金额保存成功、随后的整表重载失败：页面提示「写入已成功，数据待刷新」，门控与读取失败一致。
    editAmount('血常规')
    await change(inlineAmountInput(), '20.00')
    save().mockResolvedValueOnce(undefined)
    const failing = armQuery()
    await pressEnter(inlineAmountInput())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await failQuery(failing)
    await screen.findByText('写入已成功，数据待刷新')

    expect(exportButton()).toBeDisabled()
    await expectNoAmountExportRequest()
  })
})

describe('平台金额页整页阻断态导出不可达', () => {
  it('认证状态不可用时整页阻断态不渲染导出按钮', async () => {
    trustedOrganizationRef.current = null
    authState.token = null
    renderPage()
    expect(await screen.findByRole('heading', { name: '互认项目金额维护', level: 3 })).toBeInTheDocument()
    expect(queryExportButton()).not.toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
  })
})
