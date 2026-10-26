/**
 * 平台参数维护页 Component 层用例（票 10、C9）：
 * 列表展示、读取失败重试、提交前非法值阻断、合法保存后重新拉取并显示新值、停用状态切换。
 *
 * 装配边界：`./platformParametersApi` 只替换列表查询与保存两个 I/O 出口，取值校验使用真实实现；
 * `../../contexts/ApiClientContext` 只提供占位 client，页面不自行创建 Client。
 */
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ConfigProvider } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PlatformParameterRow } from './platformParametersApi'
import { PlatformParameters } from './PlatformParameters'

const api = vi.hoisted(() => ({
  queryPlatformParameters: vi.fn(),
  savePlatformParameter: vi.fn(),
}))

vi.mock('./platformParametersApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./platformParametersApi')>()
  return { ...actual, ...api }
})

/** 稳定的占位 client：与真实 Provider 的稳定引用一致，避免页面读取效果因对象身份变化重复触发。 */
const clientRef = vi.hoisted(() => ({ current: {} as unknown }))

/** 宿主会话替身：默认已登录；匿名场景由用例改写。 */
const sessionRef = vi.hoisted(() => ({
  current: {
    status: 'authenticated',
    sessionVersion: 'v1',
    userId: 'user-1',
    account: '191617',
    displayName: '杨康健',
    organizationId: 'ORG-A',
    hospitalId: 'HOS-1',
    campusId: 'BRA-1',
  } as unknown,
}))

vi.mock('../../session/HostSessionContext', () => ({
  useHostSession: () => sessionRef.current,
}))

vi.mock('../../contexts/ApiClientContext', () => ({
  ApiClientProvider: ({ children }: { children: unknown }) => children,
  useApiClientContext: () => clientRef.current,
}))

function row(overrides: Partial<PlatformParameterRow> = {}): PlatformParameterRow {
  return {
    code: 'own_hospital_match_switch',
    name: '本院报告匹配开关',
    value: '1',
    description: '本院报告匹配开关说明',
    isConfigured: true,
    isValid: true,
    operId: '11111111-1111-1111-1111-111111111111',
    operTime: new Date('2026-10-23T08:00:00Z'),
    ...overrides,
  }
}

const ROWS: PlatformParameterRow[] = [
  row(),
  row({ code: 'own_hospital_exclude_hours', name: '本院报告排除时长', value: '24', description: '排除时长说明' }),
  row({ code: 'citation_detail_valid_hours', name: '引用详情有效时长', value: '48', description: null }),
]

function renderPage() {
  return render(<ConfigProvider locale={zhCN}><PlatformParameters /></ConfigProvider>)
}

async function renderLoaded(rows: PlatformParameterRow[] = ROWS) {
  api.queryPlatformParameters.mockResolvedValue(rows)
  renderPage()
  await screen.findByText('本院报告匹配开关')
}

async function openEditor(name: string) {
  fireEvent.click(screen.getByRole('button', { name: `编辑参数：${name}` }))
  return screen.findByRole('dialog')
}

const saveButton = (dialog: HTMLElement) =>
  within(dialog).getByRole('button', { name: /保\s*存/ })

beforeEach(() => {
  vi.clearAllMocks()
  api.savePlatformParameter.mockResolvedValue(true)
  sessionRef.current = {
    status: 'authenticated',
    sessionVersion: 'v1',
    userId: 'user-1',
    account: '191617',
    displayName: '杨康健',
    organizationId: 'ORG-A',
    hospitalId: 'HOS-1',
    campusId: 'BRA-1',
  }
})

describe('C9 参数列表', () => {
  it('展示三项参数的编码、名称、当前值、说明、状态、操作人与时间', async () => {
    await renderLoaded()

    expect(screen.getByRole('columnheader', { name: '参数编码' })).toBeInTheDocument()
    expect(screen.getByText('own_hospital_match_switch')).toBeInTheDocument()
    expect(screen.getByText('本院报告排除时长')).toBeInTheDocument()
    expect(screen.getByText('引用详情有效时长')).toBeInTheDocument()
    expect(screen.getByText('24')).toBeInTheDocument()
    // 两个启用行 + 一个缺少说明的展示
    expect(screen.getAllByText('启用').length).toBeGreaterThanOrEqual(2)
    expect(screen.getAllByText('11111111-1111-1111-1111-111111111111').length).toBeGreaterThanOrEqual(1)
  })

  it('缺少记录的参数显示「未配置」，不发编辑以外的请求', async () => {
    await renderLoaded([
      row(),
      row({ code: 'own_hospital_exclude_hours', name: '本院报告排除时长', value: null, isConfigured: false, isValid: false }),
      row({ code: 'citation_detail_valid_hours', name: '引用详情有效时长', value: null, isConfigured: false, isValid: false }),
    ])

    expect(screen.getAllByText('未配置').length).toBeGreaterThanOrEqual(2)
    expect(api.queryPlatformParameters).toHaveBeenCalledTimes(1)
  })

  it('未登录时不发起列表请求，页面进入不可操作状态', async () => {
    sessionRef.current = {
      status: 'anonymous',
      sessionVersion: '',
      userId: null,
      account: null,
      displayName: null,
      organizationId: null,
      hospitalId: null,
      campusId: null,
    }
    renderPage()

    expect(await screen.findByText('当前未登录')).toBeInTheDocument()
    expect(api.queryPlatformParameters).not.toHaveBeenCalled()
  })

  it('列表读取失败时给出重试入口，重试成功后展示参数', async () => {
    api.queryPlatformParameters
      .mockRejectedValueOnce(new Error('读取失败'))
      .mockResolvedValueOnce(ROWS)
    renderPage()

    expect(await screen.findByText('参数列表读取失败')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /重\s*试/ }))

    expect(await screen.findByText('本院报告匹配开关')).toBeInTheDocument()
    expect(api.queryPlatformParameters).toHaveBeenCalledTimes(2)
  })
})

describe('C9 编辑与提交前校验', () => {
  it('开关填 2 时在提交前阻断：显示明确提示、不发保存请求、弹窗与输入保留', async () => {
    await renderLoaded()
    const dialog = await openEditor('本院报告匹配开关')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '2' } })
    fireEvent.click(saveButton(dialog))

    expect(await screen.findByText('本院报告匹配开关只能填写 0 或 1。')).toBeInTheDocument()
    expect(api.savePlatformParameter).not.toHaveBeenCalled()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect((within(dialog).getByLabelText('参数值') as HTMLInputElement).value).toBe('2')
  })

  it('排除时长填 -1、引用详情时长填 0 时同样在提交前阻断', async () => {
    await renderLoaded()

    let dialog = await openEditor('本院报告排除时长')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '-1' } })
    fireEvent.click(saveButton(dialog))
    expect(await screen.findByText('本院报告排除时长必须是非负整数小时。')).toBeInTheDocument()

    fireEvent.click(within(dialog).getByRole('button', { name: /取\s*消/ }))
    dialog = await openEditor('引用详情有效时长')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '0' } })
    fireEvent.click(saveButton(dialog))
    expect(await screen.findByText('引用详情有效时长必须是正整数小时。')).toBeInTheDocument()
    expect(api.savePlatformParameter).not.toHaveBeenCalled()
  })

  it('合法值保存后重新拉取列表并显示新值，不保留旧值', async () => {
    api.queryPlatformParameters
      .mockResolvedValueOnce(ROWS)
      .mockResolvedValueOnce([row({ value: '0' }), ...ROWS.slice(1)])
    renderPage()
    await screen.findByText('本院报告匹配开关')

    const dialog = await openEditor('本院报告匹配开关')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '0' } })
    fireEvent.click(saveButton(dialog))

    await waitFor(() => expect(api.savePlatformParameter).toHaveBeenCalledTimes(1))
    expect(api.savePlatformParameter).toHaveBeenCalledWith(expect.anything(), {
      code: 'own_hospital_match_switch',
      value: '0',
      description: '本院报告匹配开关说明',
      isValid: true,
    })
    await waitFor(() => expect(api.queryPlatformParameters).toHaveBeenCalledTimes(2))
    expect(await screen.findByText('0')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument(), { timeout: 3000 })
  })

  it('停用状态可切换：保存时提交 isValid=false，刷新后状态显示停用', async () => {
    api.queryPlatformParameters
      .mockResolvedValueOnce(ROWS)
      .mockResolvedValueOnce([row({ isValid: false }), ...ROWS.slice(1)])
    renderPage()
    await screen.findByText('本院报告匹配开关')

    const dialog = await openEditor('本院报告匹配开关')
    fireEvent.click(within(dialog).getByRole('switch', { name: '启用状态' }))
    fireEvent.click(saveButton(dialog))

    await waitFor(() => expect(api.savePlatformParameter).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({
      code: 'own_hospital_match_switch',
      isValid: false,
    })))
    expect(await screen.findByText('停用')).toBeInTheDocument()
  })

  it('保存未确认成功时保留弹窗与输入，不误报成功、不重新拉取列表', async () => {
    api.savePlatformParameter.mockResolvedValue(false)
    await renderLoaded()

    const dialog = await openEditor('本院报告匹配开关')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '0' } })
    fireEvent.click(saveButton(dialog))

    await waitFor(() => expect(api.savePlatformParameter).toHaveBeenCalledTimes(1))
    expect(await screen.findByText('参数保存未确认成功，请重试。')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect((within(dialog).getByLabelText('参数值') as HTMLInputElement).value).toBe('0')
    expect(api.queryPlatformParameters).toHaveBeenCalledTimes(1)
  })

  it('保存失败时保留弹窗与输入，不误报成功、不重新拉取列表', async () => {
    api.savePlatformParameter.mockRejectedValue(new Error('保存失败'))
    await renderLoaded()

    const dialog = await openEditor('本院报告匹配开关')
    fireEvent.change(within(dialog).getByLabelText('参数值'), { target: { value: '0' } })
    fireEvent.click(saveButton(dialog))

    await waitFor(() => expect(api.savePlatformParameter).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(saveButton(dialog)).not.toHaveClass('ant-btn-loading'))
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect((within(dialog).getByLabelText('参数值') as HTMLInputElement).value).toBe('0')
    expect(api.queryPlatformParameters).toHaveBeenCalledTimes(1)
  })
})
