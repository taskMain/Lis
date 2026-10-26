/**
 * IAM 组织范围选择器的 Component 层用例（C4、C5、C6、C7：三级联动、选中值同时携带组织实例标识与
 * IAM 组织业务编码、身份切换重算、缺失类型与读取失败重试）。
 *
 * 选项的实例标识与业务编码刻意不同名（编码带 `-CODE` 后缀）：选择值若取错层，断言立刻失败。
 *
 * 装配边界（遵循 Frontend Testing 第 2 节「mock 位于 Client/适配层边界，并保持真实契约形态」）：
 * 1. 本项目客户端只提供「组织范围选项」查询端点替身，替身交付与生成契约同形的读模型；
 * 2. 宿主会话替换为外部 store + `useSyncExternalStore`：`sessionVersion` 变化等价于身份或工作身份切换；
 * 3. 组件不直接读取登录管理器、不直连外部目录，选项全部来自范围查询。
 */
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { useState } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { HostSession } from '../session/hostSession'
import { OrganizationScopeSelector, type OrganizationScopeChangeMeta, type OrganizationScopeValue } from './OrganizationScopeSelector'

const ANONYMOUS_SESSION: HostSession = {
  status: 'anonymous',
  sessionVersion: 'anonymous',
  userId: null,
  account: null,
  displayName: null,
  organizationId: null,
  hospitalId: null,
  campusId: null,
}

function authenticatedSession(sessionVersion: string): HostSession {
  return {
    status: 'authenticated',
    sessionVersion,
    userId: 'user-1',
    account: '191617',
    displayName: '杨康健',
    organizationId: 'ORG-A',
    hospitalId: 'HOS-A1',
    campusId: 'BRA-A1',
  }
}

const sessionStore = vi.hoisted(() => {
  const listeners = new Set<() => void>()
  return {
    current: null as unknown,
    set(next: unknown) {
      sessionStore.current = next
      for (const listener of [...listeners]) listener()
    },
    subscribe(listener: () => void) {
      listeners.add(listener)
      return () => { listeners.delete(listener) }
    },
  }
})

vi.mock('../session/HostSessionContext', async () => {
  const { useSyncExternalStore } = await import('react')
  return {
    useHostSession: () => {
      useSyncExternalStore(sessionStore.subscribe, () => sessionStore.current)
      return sessionStore.current
    },
  }
})

const clientRef = vi.hoisted(() => ({ current: null as unknown }))

vi.mock('../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

/** 与生成契约同形的三级选项读模型：两个医共体，医院与院区各两层；业务编码由实例标识加 `-CODE` 构成。 */
function optionsModel() {
  return {
    organizations: [
      {
        organizationId: 'ORG-A',
        organizationName: '医共体甲',
        organizationCode: 'ORG-A-CODE',
        hospitals: [
          {
            organizationId: 'HOS-A1',
            organizationName: '医院甲一',
            organizationCode: 'HOS-A1-CODE',
            branches: [
              { organizationId: 'BRA-A1', organizationName: '院区甲一', organizationCode: 'BRA-A1-CODE' },
              { organizationId: 'BRA-A2', organizationName: '院区甲二', organizationCode: 'BRA-A2-CODE' },
            ],
          },
          {
            organizationId: 'HOS-A2',
            organizationName: '医院甲二',
            organizationCode: 'HOS-A2-CODE',
            branches: [{ organizationId: 'BRA-A3', organizationName: '院区甲三', organizationCode: 'BRA-A3-CODE' }],
          },
        ],
      },
      {
        organizationId: 'ORG-B',
        organizationName: '医共体乙',
        organizationCode: 'ORG-B-CODE',
        hospitals: [
          {
            organizationId: 'HOS-B1',
            organizationName: '医院乙一',
            organizationCode: 'HOS-B1-CODE',
            branches: [{ organizationId: 'BRA-B1', organizationName: '院区乙一', organizationCode: 'BRA-B1-CODE' }],
          },
        ],
      },
    ],
    unconfiguredOrganizationCount: 0,
  }
}

function givenOptions(post: () => Promise<unknown>) {
  const query = vi.fn(post)
  clientRef.current = {
    api: { organizationScopeQuery: { queryOrganizationScopeOptions: { post: query } } },
  }
  return query
}

/** 受控包装：模拟页面保存选择值，并把每次回调交给用例断言。 */
function ControlledSelector({
  initialValue,
  onChange,
  orgId,
  hosId,
}: {
  initialValue?: OrganizationScopeValue
  onChange: (value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => void
  orgId?: string
  hosId?: string
}) {
  const [value, setValue] = useState<OrganizationScopeValue>(initialValue ?? {})
  return <OrganizationScopeSelector
    value={value}
    orgId={orgId}
    hosId={hosId}
    onChange={(next, meta) => {
      setValue(next)
      onChange(next, meta)
    }}
  />
}

function renderSelector(props: {
  initialValue?: OrganizationScopeValue
  orgId?: string
  hosId?: string
}) {
  const onChange = vi.fn<(value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => void>()
  render(<ConfigProvider locale={zhCN}><ControlledSelector
    initialValue={props.initialValue}
    orgId={props.orgId}
    hosId={props.hosId}
    onChange={onChange}
  /></ConfigProvider>)
  return onChange
}

function selectByLabel(label: string): HTMLElement {
  return screen.getByLabelText(label)
}

/** 当前可见下拉里的选项文本：每个用例只展开一个下拉，避免把其他下拉的选项算进来。 */
function visibleOptions(): string[] {
  const dropdowns = Array.from(document.querySelectorAll('.ant-select-dropdown'))
    .filter((node) => !node.classList.contains('ant-select-dropdown-hidden'))
  const dropdown = dropdowns.at(-1)
  if (dropdown === undefined) return []
  return Array.from(dropdown.querySelectorAll('.ant-select-item-option'))
    .map((node) => node.textContent ?? '')
    .filter((text) => text.length > 0)
}

/** 展开某个下拉，返回可见选项文本。 */
async function openSelectOptions(label: string): Promise<string[]> {
  const input = selectByLabel(label)
  // 下游下拉在上级选项装载完成前处于禁用态：等它可用再展开。
  await waitFor(() => expect(input).not.toBeDisabled())
  const container = input.closest('.ant-select')
  expect(container).not.toBeNull()
  fireEvent.mouseDown(container!.querySelector('.ant-select-selector, .ant-select-content') ?? container!)
  await waitFor(() => expect(visibleOptions().length).toBeGreaterThan(0))
  return visibleOptions()
}

/** 选中下拉中文本等于给定名称的选项。 */
async function pickOption(label: string, optionText: string) {
  await openSelectOptions(label)
  const option = Array.from(document.querySelectorAll('.ant-select-item-option'))
    .find((node) => node.textContent === optionText)
  expect(option).toBeDefined()
  fireEvent.click(option!)
}

beforeEach(() => {
  sessionStore.set(authenticatedSession('v1'))
  clientRef.current = null
})

describe('C4 医共体、医院、院区三级联动', () => {
  it('选项来自范围查询；选中医共体后按新组织装载医院并回传组织实例标识与业务编码', async () => {
    const query = givenOptions(async () => optionsModel())
    const onChange = renderSelector({})

    expect(await openSelectOptions('范围-医共体')).toEqual(['医共体甲', '医共体乙'])
    expect(query).toHaveBeenCalledTimes(1)

    await pickOption('范围-医共体', '医共体乙')

    await waitFor(() => expect(onChange).toHaveBeenCalled())
    const [value, meta] = onChange.mock.calls[0] as [OrganizationScopeValue, OrganizationScopeChangeMeta]
    // 选中值同时携带实例标识与业务编码（提交用编码，联动用标识），且下游自动取该组织下第一组医院与院区。
    expect(value).toEqual({
      orgId: 'ORG-B',
      hosId: 'HOS-B1',
      branchId: 'BRA-B1',
      orgCode: 'ORG-B-CODE',
      hosCode: 'HOS-B1-CODE',
      branchCode: 'BRA-B1-CODE',
    })
    expect(meta.organizations.map((option) => option.id)).toEqual(['ORG-A', 'ORG-B'])
    expect(meta.hospitals.map((option) => option.id)).toEqual(['HOS-B1'])
    expect(meta.branches.map((option) => option.id)).toEqual(['BRA-B1'])
  })

  it('医院下拉只装载选定医共体下的医院', async () => {
    givenOptions(async () => optionsModel())
    renderSelector({ initialValue: { orgId: 'ORG-B', hosId: 'HOS-B1', branchId: 'BRA-B1' } })

    expect(await openSelectOptions('范围-医院')).toEqual(['医院乙一'])
  })

  it('切换医院后按新医院装载院区并回传完整范围', async () => {
    givenOptions(async () => optionsModel())
    const onChange = renderSelector({ initialValue: { orgId: 'ORG-A', hosId: 'HOS-A1', branchId: 'BRA-A1' } })
    await waitFor(() => expect(selectByLabel('范围-医院')).toBeInTheDocument())

    await pickOption('范围-医院', '医院甲二')

    await waitFor(() => expect(onChange).toHaveBeenCalled())
    expect(onChange.mock.calls.at(-1)?.[0]).toEqual({
      orgId: 'ORG-A',
      hosId: 'HOS-A2',
      branchId: 'BRA-A3',
      orgCode: 'ORG-A-CODE',
      hosCode: 'HOS-A2-CODE',
      branchCode: 'BRA-A3-CODE',
    })
  })

  it('院区下拉只装载选定医院下的院区', async () => {
    givenOptions(async () => optionsModel())
    renderSelector({ initialValue: { orgId: 'ORG-A', hosId: 'HOS-A2', branchId: 'BRA-A3' } })

    expect(await openSelectOptions('范围-院区')).toEqual(['院区甲三'])
  })

  it('selectFirstOption 在选项装载完成后回传首项组合', async () => {
    givenOptions(async () => optionsModel())
    const onChange = vi.fn()
    render(<ConfigProvider locale={zhCN}><OrganizationScopeSelector selectFirstOption onChange={onChange} /></ConfigProvider>)

    await waitFor(() => expect(onChange).toHaveBeenCalled())
    expect(onChange.mock.calls[0][0]).toEqual({
      orgId: 'ORG-A',
      hosId: 'HOS-A1',
      branchId: 'BRA-A1',
      orgCode: 'ORG-A-CODE',
      hosCode: 'HOS-A1-CODE',
      branchCode: 'BRA-A1-CODE',
    })
  })
})

describe('C5 本院入口的固定组织', () => {
  it('固定医共体不渲染组织下拉，医院只来自该医共体', async () => {
    givenOptions(async () => optionsModel())
    renderSelector({ orgId: 'ORG-B' })

    await waitFor(() => expect(screen.queryByLabelText('范围-医共体')).not.toBeInTheDocument())
    expect(await openSelectOptions('范围-医院')).toEqual(['医院乙一'])
  })

  it('固定医院时院区只在该医院内可选', async () => {
    givenOptions(async () => optionsModel())
    renderSelector({ orgId: 'ORG-B', hosId: 'HOS-B1', initialValue: { orgId: 'ORG-B', hosId: 'HOS-B1', branchId: 'BRA-B1' } })

    await waitFor(() => expect(screen.queryByLabelText('范围-医院')).not.toBeInTheDocument())
    expect(await openSelectOptions('范围-院区')).toEqual(['院区乙一'])
  })
})

describe('C7 组织类型缺失与读取失败', () => {
  it('未配置类型的节点不进入选项，数量单独提示', async () => {
    givenOptions(async () => ({ ...optionsModel(), unconfiguredOrganizationCount: 2 }))
    renderSelector({})

    expect(await screen.findByText('有 2 个组织未配置类型，未列入选项')).toBeInTheDocument()
    expect(await openSelectOptions('范围-医共体')).toEqual(['医共体甲', '医共体乙'])
  })

  it('未配置业务编码的节点不进入选项：编码为空时不回落实例标识', async () => {
    givenOptions(async () => ({
      organizations: [
        {
          organizationId: 'ORG-NO-CODE',
          organizationName: '未配编码医共体',
          organizationCode: '',
          hospitals: [{
            organizationId: 'HOS-NO-CODE',
            organizationName: '未配编码医院',
            organizationCode: '',
            branches: [{ organizationId: 'BRA-NO-CODE', organizationName: '未配编码院区', organizationCode: '' }],
          }],
        },
        {
          organizationId: 'ORG-A',
          organizationName: '医共体甲',
          organizationCode: 'ORG-A-CODE',
          hospitals: [{
            organizationId: 'HOS-A1',
            organizationName: '医院甲一',
            organizationCode: 'HOS-A1-CODE',
            branches: [{ organizationId: 'BRA-A1', organizationName: '院区甲一', organizationCode: 'BRA-A1-CODE' }],
          }],
        },
      ],
      unconfiguredOrganizationCount: 0,
    }))
    renderSelector({})

    // 未配置业务编码的组织整棵子树不进选项，也不以实例标识或名称补齐。
    expect(await openSelectOptions('范围-医共体')).toEqual(['医共体甲'])
  })

  it('读取失败时给出重试入口，重试成功后装载选项', async () => {
    const query = givenOptions(async () => { throw new Error('组织树读取失败') })
    renderSelector({})

    expect(await screen.findByText('组织范围读取失败')).toBeInTheDocument()
    expect(query).toHaveBeenCalledTimes(1)

    // 第二次请求成功：重试按钮触发重新装载。
    query.mockImplementation(async () => optionsModel())
    fireEvent.click(screen.getByRole('button', { name: /重\s*试/ }))

    expect(await openSelectOptions('范围-医共体')).toEqual(['医共体甲', '医共体乙'])
    expect(query).toHaveBeenCalledTimes(2)
  })
})

describe('C6 身份或工作身份切换后的选项重算', () => {
  it('会话推进后重新装载选项，旧会话的迟到响应不覆盖新选项', async () => {
    let resolveOld!: (value: unknown) => void
    const oldRequest = new Promise<unknown>((resolve) => { resolveOld = resolve })
    const query = givenOptions(() => oldRequest)
    renderSelector({})
    await waitFor(() => expect(query).toHaveBeenCalledTimes(1))

    givenOptions(async () => ({
      organizations: [{
        organizationId: 'ORG-N',
        organizationName: '新身份医共体',
        organizationCode: 'ORG-N-CODE',
        hospitals: [{
          organizationId: 'HOS-N1',
          organizationName: '新身份医院',
          organizationCode: 'HOS-N1-CODE',
          branches: [{ organizationId: 'BRA-N1', organizationName: '新身份院区', organizationCode: 'BRA-N1-CODE' }],
        }],
      }],
      unconfiguredOrganizationCount: 0,
    }))
    await act(async () => {
      sessionStore.set(authenticatedSession('v2'))
    })
    expect(await openSelectOptions('范围-医共体')).toEqual(['新身份医共体'])

    await act(async () => {
      resolveOld(optionsModel())
    })
    // 迟到的旧会话选项被丢弃：仍只有新身份的选项。
    expect(await openSelectOptions('范围-医共体')).toEqual(['新身份医共体'])
  })

  it('未登录时不发起选项查询，选择器保持不可用', async () => {
    sessionStore.set(ANONYMOUS_SESSION)
    const query = givenOptions(async () => optionsModel())
    renderSelector({})

    await waitFor(() => expect(screen.getByLabelText('范围-医共体')).toBeInTheDocument())
    expect(query).not.toHaveBeenCalled()
  })
})
