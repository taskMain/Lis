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
 * 3. `@dy/components-base` 只替换**整个包**：该包 ESM 产物内部使用无扩展名相对导入
 *    （`dist/index.js` → `./base-scope/config`），vitest 默认把 node_modules 依赖交给 Node 解析，
 *    因此组件层无法加载真实实现（真实实现需把该包加入 `test.server.deps.inline`，而 `vite.config.ts`
 *    不属于本票所有权）。替身只复现页面接线依赖的组件行为：受控 `value`、初始选项加载完成后按
 *    `selectFirstOption` 逐级取首项并回传完整范围值与选项清单（页头名称与默认范围解析的取值来源）、
 *    受控值与上次回传值不一致时按受控值重载下游选项并回传校正结果（清单内保持受控值、取不到即取
 *    第一项，与真实组件的初始数据装载同一口径）、
 *    用户改动按可选项级联回填下游首项并把完整范围值回传、选项读取失败时调用 antd 静态
 *    `message.error`。真实选择器的行为属宿主层验收面（C1/C3 的 Host 层级）；
 * 4. 可信院区接缝 `readTrustedBranchCode`、可信医院接缝 `readTrustedHospitalCode` 与可信组织接缝
 *    `readTrustedOrganizationCode` 都由本文件替身给值（缺省 `null` 表示该层缺失），
 *    页面只把它当作默认值，不把它记作用户的选择。`@dy/auth-react` 的 `useAuth` 同样替换：替身
 *    订阅本文件的登录态并在通知后重新求值，`resignTrustedToken` 按宿主重新签发 token 的路径推进
 *    它，页面据此在渲染期现读新的可信院区（与医院管理员页用例的 `changeTrustedScope` 同一构造）。
 *
 * 5. 金额维护的编辑形态是行内编辑器（C32）：入口是当前金额格（值文本或「未配置」标记），
 *    回车提交、Esc 取消；测试只按可访问名称、文本与页面既有样式类定位，不断言编辑器的内部结构。
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
import { configureBaseComponents, type BaseComponentsConfig } from '@dy/components-base'
import type { BaseScopeSelectorProps, BaseScopeValue } from '@dy/components-base'
import { toRecognitionAmountColumnView, toRecognitionAmountRows, type RecognitionAmountReadModelLike, type RecognitionAmountRow } from './recognitionAmountsApi'
import { RecognitionAmounts } from './RecognitionAmounts'

/** 伪 Base API Client 的注入点：与真实包 `configureBaseComponents` 同形（替身见文件头第 3 条）。 */
const baseClientRef = vi.hoisted(() => ({ current: null as unknown }))

const api = vi.hoisted(() => ({
  queryRecognitionAmountRows: vi.fn(),
  queryBranchRecognitionAmountRows: vi.fn(),
  saveOrganizationHospitalBranchRecognitionAmount: vi.fn(),
  saveBranchRecognitionAmount: vi.fn(),
  recheckRecognitionAmountScope: vi.fn(),
}))

vi.mock('@dy/components-base', async () => {
  const { createElement, useEffect, useRef, useState } = await import('react')
  const { message: antdMessage } = await import('antd')

  type ScopeOption = { id?: string | null; name?: string | null; hosId?: string | null }
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
    depts: ScopeOption[]
    users: ScopeOption[]
  }

  /**
   * 替身 props：在真实组件 props 上把 onChange 的选项清单换成替身自己的选项形状
   * （真实清单元素是 Kiota 生成的 DTO，替身只复现页面读取的 id 与 name 两个字段）。
   */
  type StubScopeSelectorProps = Omit<BaseScopeSelectorProps, 'onChange'> & {
    onChange?: (value: BaseScopeValue, meta: StubScopeMeta) => void
  }

  /** 范围选择器替身：接线行为与真实组件一致的部分见文件头第 3 条。 */
  function StubScopeSelector({ value = {}, disabled, className, selectFirstOption, onChange }: StubScopeSelectorProps) {
    const [organizations, setOrganizations] = useState<ScopeOption[]>([])
    const [hospitals, setHospitals] = useState<ScopeOption[]>([])
    const [branches, setBranches] = useState<ScopeOption[]>([])

    const initialLoadStartedRef = useRef(false)
    /** 上次回传页面的值：受控值与它一致即无需重载（与真实组件的回传值记忆同一口径）。 */
    const lastEmittedRef = useRef<BaseScopeValue | undefined>(undefined)

    useEffect(() => {
      /**
       * 按给定起点装载三级选项：清单内保持起点取值、取不到即取第一项（与真实组件的初始数据装载
       * 同一口径）；取值与起点不同且三级齐全时回传完整范围值与选项清单。
       */
      const loadValue = async (start: BaseScopeValue) => {
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
            const nextValue: BaseScopeValue = { orgId, hosId, branchId }
            lastEmittedRef.current = nextValue
            onChange?.(
              nextValue,
              { organizations: nextOrganizations, hospitals: nextHospitals, branches: nextBranches, depts: [], users: [] },
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
      lastEmittedRef.current = merged
      onChange?.(merged, { organizations, hospitals: nextHospitals, branches: nextBranches, depts: [], users: [] })
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

  return {
    configureBaseComponents: (config: { client?: unknown }) => {
      baseClientRef.current = config.client ?? null
    },
    BaseScopeSelector: StubScopeSelector,
  }
})

/** 可信院区注入点：缺省 `null` 表示可信院区缺失，用例按需给值（见文件头第 4 条）。 */
const trustedBranchRef = vi.hoisted(() => ({ current: null as string | null }))

/** 可信组织、可信医院注入点：缺省 `null` 表示该层缺失，与可信院区同一手法（见文件头第 4 条）。 */
const trustedOrganizationRef = vi.hoisted(() => ({ current: null as string | null }))
const trustedHospitalRef = vi.hoisted(() => ({ current: null as string | null }))

/**
 * 可信登录态（token）的外部 store；`version` 是订阅快照：任何一次通知都推进它，让订阅方按新的
 * 可信范围重新求值。推进 token 等价于宿主重新签发 token（与医院管理员页用例同一构造）。
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
    /** 通知订阅者登录态已变化（真实包在登录、登出与凭据变更时发出同类通知）。 */
    notify() {
      authState.version += 1
      for (const listener of listeners) listener()
    },
  }
})

vi.mock('@dy/auth-react', async () => {
  const { useSyncExternalStore } = await import('react')
  return {
    /** 与真实包同形：订阅登录态，通知后重新求值；token 仍是页面判定可信范围是否可用的依据。 */
    useAuth: () => {
      useSyncExternalStore(authState.subscribe, () => authState.version)
      return {
        isAuthenticated: authState.token !== null,
        token: authState.token,
        login: () => {},
        logout: () => {},
      }
    },
  }
})

/** `readTrustedBranchCode` 的现读计数：token 缺失期间页面若反复现读，就是渲染重排在持续的直接证据。 */
const trustedBranchReadCount = vi.hoisted(() => ({ count: 0 }))

vi.mock('../reportManagement/reportsTrustedScope', () => ({
  readTrustedBranchCode: () => {
    trustedBranchReadCount.count += 1
    return trustedBranchRef.current
  },
  readTrustedHospitalCode: () => trustedHospitalRef.current,
}))

/** 可信组织接缝替身：页面只在初始范围解析时现读它，缺省与其余两层同为 `null`。 */
vi.mock('../recognitionProjects/recognitionProjectsOrganizationScope', () => ({
  readTrustedOrganizationCode: () => trustedOrganizationRef.current,
}))

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
const ORGANIZATIONS = [{ id: 'ORG-A', name: '组织一' }, { id: 'ORG-B', name: '组织二' }]
const HOSPITALS = [{ id: 'HOS-1', name: '医院一' }, { id: 'HOS-2', name: '医院二' }]
const BRANCHES = [
  { id: 'BRA-1', name: '院区一', hosId: 'HOS-1' },
  { id: 'BRA-2', name: '院区二', hosId: 'HOS-2' },
  { id: 'BRA-3', name: '院区三', hosId: 'HOS-2' },
  // 同一医院下的第二个院区：用于「切换院区」这一范围变化（院区选项按医院过滤）。
  { id: 'BRA-4', name: '院区四', hosId: 'HOS-1' },
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

/** 生成端反序列化后的金额形态：未定型节点（`value` + `getValue()`），不是裸数字。 */
const node = (value: unknown) => ({ value, getValue: () => value })

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
    currentAmount: node(12.5),
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

/** 查询主按钮与关键字输入框：读取在途时主按钮呈加载态，关键字只在提交时下推服务端。 */
const searchButton = () => screen.getByRole('button', { name: /查\s*询/ })
const keywordInput = () => screen.getByLabelText('关键字') as HTMLInputElement

/**
 * 在查询区表单上触发 submit 事件（等价模拟输入框内回车提交）：jsdom 不实现文本框内回车的
 * 隐式提交（键盘默认动作不在模拟范围），真实浏览器的回车经表单隐式提交进入同一条 Form
 * onFinish 提交链路，这里以 submit 事件表达同一条链路。
 */
const submitByEnter = () => {
  const form = keywordInput().closest('form')
  if (!form) throw new Error('查询区未以 Form 承载')
  fireEvent.submit(form)
}

/** 两个状态 Segmented 把外部 aria-label 透传到 role=radiogroup（与互认项目配置页同一手法）。 */
const amountStatusFilter = () => screen.getByRole('radiogroup', { name: '金额状态筛选' })
const configurationStatusFilter = () => screen.getByRole('radiogroup', { name: '互认配置状态筛选' })

const selectedSegmentLabels = (group: HTMLElement) =>
  [...group.querySelectorAll('.ant-segmented-item-selected')].map((node) => node.textContent?.trim())

/** 点击 Segmented 的某个档位；档位只做本地裁剪，不触发读取。 */
const pickSegment = (group: HTMLElement, label: string) => click(within(group).getByText(label))

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
  clientRef.current = {}
  baseClient = createBaseClient()
  // 地址栏与可信三层都回到缺省状态：上一个用例的已生效条件不得泄漏进本用例。
  window.history.replaceState(null, '', '/')
  trustedBranchRef.current = null
  authState.token = 'token-1'
  trustedBranchRef.current = null
  // 可信组织与医院缺省在值（组织 ORG-A、医院 HOS-1，与伪 Base Client 首项组合一致）：
  // 阻断判定之下页面只有可信组织、医院齐备才进入选择器解析；缺失场景由阻断用例显式改写。
  trustedOrganizationRef.current = 'ORG-A'
  trustedHospitalRef.current = 'HOS-1'
  trustedBranchReadCount.count = 0
  // 组件的 Base API Client 走它声明的注入点，不发起真实请求。
  configureBaseComponents({ client: baseClient as unknown as BaseComponentsConfig['client'] })
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

  it('C1：依次切换组织、医院、院区都按新范围重载，且不自动发起写请求', async () => {
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])
    await screen.findByText('血常规')

    const second = armQuery()
    await pickScopeOption('组织', 'ORG-B')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow({ standardProjectCode: 'B01', standardProjectName: '尿常规' })])

    const third = armQuery()
    await pickScopeOption('医院', 'HOS-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(third, [amountRow({ standardProjectCode: 'C01', standardProjectName: '便常规' })])

    const fourth = armQuery()
    await pickScopeOption('院区', 'BRA-3')
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
  it('列为类型、分类、分组、项目与四个业务列，不展示三级名称与编码、项目类型、最后修改信息', async () => {
    await renderLoaded([amountRow()])

    expect(columnHeaders()).toEqual([
      '类型',
      '分类',
      '分组',
      '项目',
      '当前金额',
      '金额状态',
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
      amountRow({ standardProjectCode: 'A01', standardProjectName: '零元项目', currentAmount: node(0), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    const [zeroRow, unconfiguredRow] = dataRows()

    // 零元：金额列是已配置的金额文本，金额状态为已配置，行内不出现「未配置」。
    expect(within(zeroRow).getByText('0.00')).toBeInTheDocument()
    expect(within(zeroRow).getByText('已配置')).toBeInTheDocument()
    expect(within(zeroRow).queryByText('未配置')).toBeNull()

    // 未配置：金额列与金额状态都表达「未配置」，且整行不出现任何数值金额。
    expect(within(unconfiguredRow).getAllByText('未配置')).toHaveLength(2)
    expect(within(unconfiguredRow).queryByText('0.00')).toBeNull()
    expect(within(unconfiguredRow).queryByText('已配置')).toBeNull()
    expect((unconfiguredRow.textContent ?? '').match(/\d+\.\d{2}/)).toBeNull()
  })
})

describe('C8 金额输入校验', () => {
  it('负数、三位小数、双精度无法精确表示的大额与空值都以 message.error 提示，单元格内不再渲染校验红字，零写请求且保留编辑态与输入', async () => {
    await renderLoaded([amountRow()])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    // 本循环的非法判据只取适配层出口 `parseRecognitionAmount`（页面不另写金额规则），其中
    // `99999999999999.99` 由该出口的「内存面 / 序列化面」等价性判据决定：该出口一旦放宽或收紧，
    // 本循环的输入集合必须同步（见 recognitionAmountsApi.test.ts 的金额解析用例）。
    for (const invalid of ['-1', '1.234', '99999999999999.99']) {
      await change(inlineAmountInput(), invalid)
      await pressEnter(inlineAmountInput())
      // 校验失败以 message.error 反馈（TC-T02-07）：同一文案可叠加多条提示，按「出现至少一条」断言。
      expect((await screen.findAllByText('金额必须是不小于 0 且最多两位小数的数字')).length).toBeGreaterThan(0)
      // 表格单元格内不再渲染任何校验红字。
      expect(within(rowOf('血常规')).queryByText('金额必须是不小于 0 且最多两位小数的数字')).toBeNull()
      // `invalid` 标记：校验在字段变更后重新求值，标记随之更新。
      await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
      expect(save()).not.toHaveBeenCalled()
      expect(inlineAmountInput()).toBeInTheDocument()
      expect(inlineAmountInput().value).toBe(invalid)
    }

    await change(inlineAmountInput(), '')
    await pressEnter(inlineAmountInput())
    expect((await screen.findAllByText('金额必填')).length).toBeGreaterThan(0)
    expect(within(rowOf('血常规')).queryByText('金额必填')).toBeNull()
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
    expect(inlineAmountInput()).toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('')
  })
})

describe('C9 / C12 金额保存成功', () => {
  it('回填当前值后不改直接提交仍发出写请求，成功后收起编辑态并整表重载', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    expect(inlineAmountInput().value).toBe('12.50')

    save().mockResolvedValueOnce(undefined)
    const reload = armQuery()
    await pressEnter(inlineAmountInput())

    await waitFor(() => expect(save()).toHaveBeenCalledTimes(1))
    // 保存成功给 message.success 反馈（TC-T02-07）；现状保存后没有任何成功提示。
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
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

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
      // 写失败不是成功：保存成功的 message 反馈同样不得出现（TC-T02-07 的分流口径）。
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
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

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
    await settle(retry, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    await screen.findByText('20.00')
    expect(screen.queryByText('写入已成功，数据待刷新')).not.toBeInTheDocument()
    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
  })
})

describe('C14 读取失败形态（D-06、TC-T02-11）', () => {
  it('查询失败不渲染错误横幅：旧数据保留、写入仍禁用，点查询可重试成功', async () => {
    await renderLoaded([amountRow()])

    const failing = armQuery()
    // 空条件提交等价于刷新：搜索主按钮重新读取当前范围。
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))

    // 加载期间禁用写入（搜索主按钮呈加载态、当前金额格点击不进入编辑态）。
    // 加载态的提交晚于读取计数的观测，等它真正落到按钮上再断言。
    await waitFor(() => expect(searchButton()).toHaveClass('ant-btn-loading'))
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()

    await failQuery(failing)
    // 失败落地的观测点改为加载态结束：D-06 下读取失败不再渲染任何错误横幅，
    // 错误由宿主统一提示，重试经查询按钮。
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    expect(screen.queryByText('互认项目金额数据待刷新')).not.toBeInTheDocument()
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()

    expect(screen.getByText('血常规')).toBeInTheDocument()
    expect(amountValueCell('血常规')).toBeDisabled()
    click(amountValueCell('血常规'))
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()

    // 重试经查询按钮：重读成功后恢复可写。
    const retry = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(retry, [amountRow()])

    await waitFor(() => expect(amountValueCell('血常规')).toBeEnabled())
    expect(screen.queryByText(/待刷新|读取失败/)).not.toBeInTheDocument()
  })
})

describe('C16 排序与不分页', () => {
  it('C16：服务端交付的编码升序在列视图中原样渲染，页面不分页', async () => {
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
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: node(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: node(20) }),
    ])

    pickSegment(amountStatusFilter(), '未配置')

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
    await pickScopeOption('院区', 'BRA-4')
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
    expect(screen.queryAllByRole('button', { name: /金额/ })).toHaveLength(0)
    // 范围未解析：给出范围未选全的空态，页面不进入可操作状态。
    expect(screen.getByText('请选择组织、医院与院区后查看互认项目金额')).toBeInTheDocument()
  })
})

/**
 * 用例名只描述行为：全站查询按钮逐字符合统一规格（T02 Client/design.md「查询按钮与查询区」、
 * testPlan.md TC-T02-06）。规格属性逐项断言；`autoInsertSpace={false}` 的效果以文本精确等于
 * 「查询」表达；`htmlType` 经 DOM 属性断言；条件为空提交等价于刷新由 C27 与读取时序用例覆盖。
 */
describe('TC-T02-06 查询按钮规格', () => {
  it('筛选区查询按钮：主按钮、搜索图标、文本精确为「查询」、htmlType 为 submit', async () => {
    await renderLoaded([amountRow()])

    const button = searchButton()
    expect(button).toHaveClass('ant-btn-primary')
    expect(button.querySelector('.anticon-search')).not.toBeNull()
    expect(button.textContent).toBe('查询')
    expect(button.getAttribute('type')).toBe('submit')
  })
})

/**
 * 用例名只描述行为：查询区以 inline Form 承载，查询与重置按钮位于行内最后一个 Form.Item 内并排
 * （T02 Client/design.md「查询按钮与查询区」、testPlan.md TC-T02-16）。重置恒命名「重置」、默认样式。
 * 重置口径与批 3 看板页一致：草稿与已生效条件一并恢复默认（关键字清空、两个状态档回「全部」），
 * 随后按默认条件重读一次；地址栏参数按默认值回写。两个状态档是即时生效控件、不进草稿：
 * 重置只是把它们恢复默认档，不改变「切换即生效、本地裁剪、不触发读取」的既有语义。
 */
describe('TC-T02-16 查询区 Form 化与重置', () => {
  it('查询区为 inline Form，查询与重置在行内最后一个 Form.Item 内并排', async () => {
    await renderLoaded([amountRow()])

    const form = document.querySelector('.recognition-amounts-filter form')
    expect(form).not.toBeNull()
    expect(form).toHaveClass('ant-form-inline')
    const search = searchButton()
    expect(search.getAttribute('type')).toBe('submit')
    const reset = screen.getByRole('button', { name: '重置' })
    expect(reset).not.toHaveClass('ant-btn-primary')
    // 查询与重置并排：同属行内最后一个 Form.Item。
    const sharedItem = search.closest('.ant-form-item')
    expect(sharedItem).not.toBeNull()
    expect(sharedItem).toBe(reset.closest('.ant-form-item'))
  })

  it('重置把关键字草稿与两个状态档恢复默认，按默认条件重读一次并回写地址栏', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '尿常规' }),
    ])

    // 布置：关键字提交生效（第 2 次读取），两个状态档切到非默认（即时生效，不追加读取）。
    await change(keywordInput(), '尿常规')
    expect(query()).toHaveBeenCalledTimes(1)
    const applied = armQuery()
    submitByEnter()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(applied, [
      amountRow({ standardProjectCode: 'A02', standardProjectName: '尿常规' }),
    ])
    await waitFor(() => expect(searchButton()).not.toHaveClass('ant-btn-loading'))
    pickSegment(amountStatusFilter(), '已配置')
    pickSegment(configurationStatusFilter(), '停用')
    expect(new URLSearchParams(window.location.search).get('keyword')).toBe('尿常规')
    expect(new URLSearchParams(window.location.search).get('amountStatus')).toBe('configured')
    expect(new URLSearchParams(window.location.search).get('configurationStatus')).toBe('disabled')

    // 重置：草稿与已生效条件一并恢复默认，按空条件重读一次。
    const resetLoad = armQuery()
    click(screen.getByRole('button', { name: '重置' }))
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    await settle(resetLoad, [
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规' }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '尿常规' }),
    ])

    // 草稿清空、两个状态档回「全部」。
    expect(keywordInput().value).toBe('')
    expect(selectedSegmentLabels(amountStatusFilter())).toEqual(['全部'])
    expect(selectedSegmentLabels(configurationStatusFilter())).toEqual(['全部'])
    // 地址栏参数按默认值回写（三个参数删除）。
    expect(new URLSearchParams(window.location.search).get('keyword')).toBeNull()
    expect(new URLSearchParams(window.location.search).get('amountStatus')).toBeNull()
    expect(new URLSearchParams(window.location.search).get('configurationStatus')).toBeNull()
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
    submitByEnter()
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
  it('两个 Segmented 只在服务端结果上本地裁剪：切换即生效、不追加读取、不下推请求', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '项目一', isAmountConfigured: true, currentAmount: node(12.5), configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '项目二', isAmountConfigured: false, currentAmount: null, configurationStatus: 1 }),
      amountRow({ standardProjectCode: 'A03', standardProjectName: '项目三', isAmountConfigured: true, currentAmount: node(3), configurationStatus: 2 }),
    ])
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])
    expect(query()).toHaveBeenCalledTimes(1)

    pickSegment(amountStatusFilter(), '未配置')
    expect(renderedCodes()).toEqual(['A02'])
    expect(selectedSegmentLabels(amountStatusFilter())).toEqual(['未配置'])

    pickSegment(amountStatusFilter(), '已配置')
    expect(renderedCodes()).toEqual(['A01', 'A03'])

    pickSegment(configurationStatusFilter(), '停用')
    expect(renderedCodes()).toEqual(['A03'])

    pickSegment(amountStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A03'])

    pickSegment(configurationStatusFilter(), '全部')
    expect(renderedCodes()).toEqual(['A01', 'A02', 'A03'])

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

    // 进入按参数初始化：草稿取已生效关键字，两个 Segmented 取地址栏档位。
    expect(keywordInput().value).toBe('血常规')
    expect(selectedSegmentLabels(amountStatusFilter())).toEqual(['未配置'])
    expect(selectedSegmentLabels(configurationStatusFilter())).toEqual(['启用'])
    expect(renderedCodes()).toEqual(['A01'])

    // 档位变化按替换当前历史项写回，且不追加读取。
    const historyLength = window.history.length
    pickSegment(amountStatusFilter(), '已配置')
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
    submitByEnter()
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

describe('C30 院区恒定单值与三档默认', () => {
  it('默认档一：可信组织、医院、院区齐全时初始范围即为可信范围', async () => {
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
    expect(scopeSelect('组织')).toHaveValue('ORG-B')
    expect(scopeSelect('医院')).toHaveValue('HOS-2')
    expect(scopeSelect('院区')).toHaveValue('BRA-3')
    await settle(first, [amountRow()])
  })

  it('默认档二：可信院区不属于当前组织、医院时取该院第一项', async () => {
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
    expect(scopeSelect('组织')).toHaveValue('ORG-A')
    expect(scopeSelect('医院')).toHaveValue('HOS-2')
    expect(scopeSelect('院区')).toHaveValue('BRA-2')
    await settle(first, [amountRow()])
  })

  it('默认档二：可信院区缺失时取当前组织、医院第一项', async () => {
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
    expect(scopeSelect('院区')).toHaveValue('BRA-2')
    await settle(first, [amountRow()])
  })

  it('可信组织缺失时整页阻断：只渲染标题与错误 Alert，不进入逐级取首项的解析', async () => {
    trustedOrganizationRef.current = null
    trustedBranchRef.current = 'BRA-3'
    renderPage()

    expect(await screen.findByRole('heading', { name: '互认项目金额维护' })).toBeInTheDocument()
    const alert = within(document.querySelector('.recognition-amounts-page') as HTMLElement).getByRole('alert')
    expect(alert).toHaveClass('ant-alert-error')
    expect(alert.textContent).toBe('无法读取当前登录组织或医院，已阻断本页查询与操作。')
    // 阻断态零业务请求，也不渲染范围选择器（不伪造组织、不退化为全局查询）。
    expect(query()).not.toHaveBeenCalled()
    expect(save()).not.toHaveBeenCalled()
    expect(screen.queryByLabelText('范围-组织')).not.toBeInTheDocument()
  })

  it('可信医院缺失时同样整页阻断', async () => {
    trustedHospitalRef.current = null
    trustedBranchRef.current = 'BRA-3'
    renderPage()

    expect(await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')).toBeInTheDocument()
    expect(query()).not.toHaveBeenCalled()
    expect(screen.queryByLabelText('范围-医院')).not.toBeInTheDocument()
  })

  it('切换组织或医院后院区自动重取：可信院区属于新范围则选中，否则新范围第一项', async () => {
    trustedBranchRef.current = 'BRA-4'
    const first = armQuery()
    renderPage()
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(1))
    await settle(first, [amountRow()])

    const second = armQuery()
    await pickScopeOption('医院', 'HOS-2')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-2', branchCode: 'BRA-2' })
    await settle(second, [amountRow()])

    // 切回原医院：可信院区重新属于当前范围，选中值重新取可信院区（该范围首项是 BRA-1）。
    const third = armQuery()
    await pickScopeOption('医院', 'HOS-1')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    expect(query().mock.calls[2][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await settle(third, [amountRow()])

    const fourth = armQuery()
    await pickScopeOption('组织', 'ORG-B')
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
    await pickScopeOption('院区', 'BRA-1')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-1' })
    await settle(second, [amountRow()])

    // 组件的清空动作不被接受：保持原受控值，不发起空院区请求。
    await pickScopeOption('院区', '')
    expect(scopeSelect('院区')).toHaveValue('BRA-1')
    expect(query()).toHaveBeenCalledTimes(2)
  })
})

describe('C31 页头范围条', () => {
  it('页头由共用范围条承载：单行只显名称、不出现任何编码文本（TC-T02-02）', async () => {
    await renderLoaded([amountRow()])

    // 本用例环境的登录态（真实 LoginUserManager）没有三级编码，范围条按「未知」占位：
    // 名称解析属组件层与目录反查用例的面，页面级此处固定「页头无编码」与范围条挂载两个事实。
    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()
    const header = document.querySelector('.recognition-amounts-header') as HTMLElement
    for (const code of ['ORG-A', 'HOS-1', 'BRA-1', 'ORG-B', 'HOS-2', 'BRA-2']) {
      expect(header.textContent).not.toContain(code)
    }
    // 范围条不设「组织」这类文字标签，也不含名称行/编码行的旧双行结构。
    expect(document.querySelector('.recognition-amounts-header-scope')).toBeNull()
    expect(within(header).queryByText('组织', { exact: true })).toBeNull()
  })

  it('切换组织、医院选择后页头范围条不变：范围条指登录范围而非选定范围（D-05 接缝）', async () => {
    await renderLoaded([amountRow()])
    expect(await screen.findByText('未知 / 未知 / 未知')).toBeInTheDocument()

    const second = armQuery()
    await pickScopeOption('组织', 'ORG-B')
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(second, [amountRow()])

    // 范围条只反映登录可信范围，不随选择器的显式改选变化，也不出现新选定范围的编码。
    expect(screen.getByText('未知 / 未知 / 未知')).toBeInTheDocument()
    const header = document.querySelector('.recognition-amounts-header') as HTMLElement
    expect(header.textContent).not.toContain('ORG-B')
  })
})

describe('TC-T02-04 可信范围不可用（平台页阻断）', () => {
  it('token 缺失时整页只渲染标题与错误 Alert，零业务请求（TC-T02-04）', async () => {
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
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: node(12.5), isAmountConfigured: true }),
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
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

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
    await settle(reload, [amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    expect(screen.getByText('12.50')).toBeInTheDocument()
  })

  it('后台刷新改变同一行金额后编辑器以当前金额值为键重挂并回填新值', async () => {
    await renderLoaded([amountRow({ currentAmount: node(12.5), isAmountConfigured: true })])

    editAmount('血常规')
    await change(inlineAmountInput(), '999')

    const reload = armQuery()
    click(searchButton())
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])

    // 目标行仍在当前行集：编辑态保留，但以新金额值重挂，回填值随当前行数据更新。
    await waitFor(() => expect(inlineAmountInput().value).toBe('20.00'))
    expect(save()).not.toHaveBeenCalled()
  })

  it('状态档把目标行裁出当前行集后不渲染编辑态', async () => {
    await renderLoaded([
      amountRow({ standardProjectCode: 'A01', standardProjectName: '血常规', currentAmount: node(12.5), isAmountConfigured: true }),
      amountRow({ standardProjectCode: 'A02', standardProjectName: '未配置项目', currentAmount: null, isAmountConfigured: false }),
    ])

    editAmount('血常规')
    expect(inlineAmountInput()).toBeInTheDocument()

    pickSegment(amountStatusFilter(), '未配置')
    expect(queryInlineAmountInput()).toBeNull()
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)
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
    await screen.findByText('金额必须是不小于 0 且最多两位小数的数字')
    await waitFor(() => expect(inlineAmountInput()).toHaveAttribute('aria-invalid', 'true'))
    expect(save()).not.toHaveBeenCalled()
    expect(query()).toHaveBeenCalledTimes(1)

    // 改成合法值后行内标记随之清除，编辑态继续保留。
    await change(inlineAmountInput(), '1.5')
    await waitFor(() => expect(inlineAmountInput()).not.toHaveAttribute('aria-invalid', 'true'))
    expect(inlineAmountInput().value).toBe('1.5')
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
    await pickScopeOption('医院', 'HOS-2')

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
    await pickScopeOption('医院', 'HOS-2')

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
    await pickScopeOption('医院', 'HOS-2')

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
    await pickScopeOption('医院', 'HOS-2')

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
    await pickScopeOption('医院', 'HOS-2')

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
    expect(scopeSelect('院区')).toHaveValue('BRA-1')
  })

  it('只推进 token、不碰选择器：无未保存内容时按三档规则重取院区且不弹确认框', async () => {
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 只按宿主重新签发 token 的路径推进可信院区，全程没有任何选择器交互；重载读取先排队再触发。
    const next = armQuery()
    await resignTrustedToken('BRA-4')

    // 当前组织、医院不变；可信院区 BRA-4 属于该院区清单：按档一直接选中它并据此重载（S3-D22）。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    expect(scopeSelect('院区')).toHaveValue('BRA-4')
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
    const readsAtTokenLoss = trustedBranchReadCount.count
    expect(await screen.findByText('无法读取当前登录组织或医院，已阻断本页查询与操作。')).toBeInTheDocument()

    // 让出若干轮宏任务：渲染重排若在持续，每一轮的 setTimeout(0) 与其中的现读都会被观察到。
    // 阻断面（面板、零新增读取）与稳定面（无确认框、无选择器、现读计数停住）
    // 用 expect.soft 并列报告，任一面的失败都不被前一个失败掩盖。
    await act(async () => {
      for (let round = 0; round < 5; round += 1) {
        await new Promise((resolve) => { window.setTimeout(resolve, 0) })
      }
    })
    expect.soft(query()).toHaveBeenCalledTimes(1)
    expect.soft(trustedBranchReadCount.count).toBe(readsAtTokenLoss)
    expect.soft(modalExists(CONFIRM_TRUSTED_SCOPE)).toBe(false)
    expect.soft(screen.queryByLabelText('范围-组织')).not.toBeInTheDocument()
    expect.soft(save()).not.toHaveBeenCalled()
  })

  it('只推进 token、有未保存内容：确认框里不碰选择器直接放弃并切换，院区按三档规则现解析', async () => {
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

    // 放弃并切换：按 S3-D22 三档规则现解析院区——可信 BRA-4 属于当前组织医院的清单，选中它并据此读取。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(2))
    expect(query().mock.calls[1][1]).toEqual({ organizationCode: 'ORG-A', hospitalCode: 'HOS-1', branchCode: 'BRA-4' })
    await waitFor(() => expect(scopeSelect('院区')).toHaveValue('BRA-4'))
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

  /** 拦截提示必须齐备的三要素（Client/testPlan.md 第 42 行 C34 三场景）；提示以 message.error 承载（TC-T02-07）。 */
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
      applied: { organizationCode: null, hospitalCode: null, branchCode: 'BRA-1' },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: null },
      layers: ['branch'],
    })
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截原因以 message.error 反馈（TC-T02-07），且不再渲染在单元格内；旧版「页面范围为…请刷新页面后重试」不再出现。
    expect(await screen.findByText(REASON_PATTERN)).toBeInTheDocument()
    expect(within(rowOf('血常规')).queryByText(REASON_PATTERN)).toBeNull()
    expect(within(rowOf('血常规')).queryByText(/请刷新页面后重试/)).not.toBeInTheDocument()
    expect(inlineAmountInput().value).toBe('18.00')
  })

  it('场景二：档二已应用——记录的可信院区与现读同为缺失：不触发拦截，照常提交一次', async () => {
    // S3-D22 档二：可信组织与医院在、可信院区缺失；页面按当前组织医院下的首项选定院区（BRA-1）。
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
      applied: { organizationCode: null, hospitalCode: null, branchCode: null },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: null },
      layers: ['branch'],
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
    await settle(reload, [amountRow({ currentAmount: node(20), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('20.00')
  })

  it('场景三：用户选定院区不同于可信默认值、记录与现读一致：不触发拦截，照常提交一次', async () => {
    // 可信院区 BRA-1 即初始默认值；页面应用范围时把它记录下来。
    trustedBranchRef.current = 'BRA-1'
    await renderLoaded([amountRow()])

    // 用户跨院区维护：选定 BRA-4，应用范围时记录的可信院区仍是 BRA-1（用户选定值不写入记录）。
    const second = armQuery()
    await pickScopeOption('院区', 'BRA-4')
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
      applied: { organizationCode: null, hospitalCode: null, branchCode: 'BRA-1' },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: 'BRA-1' },
      layers: ['branch'],
    })
    // 提交成功后收起编辑态并整表重载。
    await waitFor(() => expect(query()).toHaveBeenCalledTimes(3))
    await settle(reload, [amountRow({ currentAmount: node(8), isAmountConfigured: true })])
    await waitFor(() => expect(queryInlineAmountInput()).toBeNull())
    await screen.findByText('8.00')
  })

  it('记录的可信院区缺失而现读已有值（档二后范围变化、页面未应用）：零写请求，以 message.error 给出已变化的原因', async () => {
    // 档二已应用：记录侧为缺失；随后宿主给出新的可信院区，页面尚未应用新可信范围。
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
      applied: { organizationCode: null, hospitalCode: null, branchCode: null },
      trusted: { organizationCode: null, hospitalCode: null, branchCode: 'BRA-3' },
      layers: ['branch'],
    })
    expect(query()).toHaveBeenCalledTimes(1)
    // 拦截原因以 message.error 反馈（TC-T02-07），不再渲染在单元格内。
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
      amountRow({ standardProjectCode: 'A01', isAmountConfigured: true, currentAmount: node(12.5) }),
      amountRow({ standardProjectCode: 'A02', isAmountConfigured: true, currentAmount: node(20) }),
      amountRow({ standardProjectCode: 'A03', isAmountConfigured: false, currentAmount: null }),
    ])

    expect(screen.getByText('共 3 项')).toBeInTheDocument()

    pickSegment(amountStatusFilter(), '未配置')
    expect(screen.getByText('共 1 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)

    pickSegment(amountStatusFilter(), '全部')
    expect(screen.getByText('共 3 项')).toBeInTheDocument()
    expect(query()).toHaveBeenCalledTimes(1)
  })
})

describe('C36 读取在途写门控', () => {
  it('读取在途时搜索主按钮呈加载态、金额格不进入编辑态，读取结束才恢复', async () => {
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

describe('TC-T02-14 页面标题层级', () => {
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
