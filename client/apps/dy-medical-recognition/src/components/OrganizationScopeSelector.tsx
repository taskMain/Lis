import { Alert, Button, Select, Space, Typography } from 'antd'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useApiClientContext } from '../contexts/ApiClientContext'
import { useHostSession } from '../session/HostSessionContext'
import {
  type OrganizationScopeBranchOption,
  type OrganizationScopeHospitalOption,
  type OrganizationScopeOption,
  type OrganizationScopeOptions,
  type OrganizationScopeOrganizationOption,
} from './organizationScope'
import { queryOrganizationScopeOptions } from './organizationScopeApi'

/**
 * 范围选择值：三级同时携带组织实例标识与业务编码。
 *
 * 实例标识只在本组件内部用于选项去重与三级联动、供页面受控保存；
 * 业务编码才是业务入口提交的组织标识，页面构造请求一律取 `orgCode`/`hosCode`/`branchCode`。
 */
export interface OrganizationScopeValue {
  orgId?: string
  hosId?: string
  branchId?: string
  orgCode?: string
  hosCode?: string
  branchCode?: string
}

/** 范围变化时随值交付的选项清单，供页面解析默认值与核对归属。 */
export interface OrganizationScopeChangeMeta {
  organizations: OrganizationScopeOption[]
  hospitals: OrganizationScopeOption[]
  branches: OrganizationScopeOption[]
  unconfiguredOrganizationCount: number
}

/** 范围选择器属性。 */
export interface OrganizationScopeSelectorProps {
  /**
   * 受控值；以实例标识优先定位选项，未给标识时按业务编码定位
   * （统计页等只保存编码的调用方据此显示已选范围）。
   */
  value?: OrganizationScopeValue
  /** 范围变化回调：值三级可能不齐（该层没有可选项），由页面按自身口径决定是否接受。 */
  onChange?: (value: OrganizationScopeValue, meta: OrganizationScopeChangeMeta) => void
  /** 固定医共体（本院入口）：给出时组织下拉不渲染，选项按该组织实例标识装载。 */
  orgId?: string
  /** 固定医院（本院入口）：给出时医院下拉不渲染，院区只在固定医院内可选。 */
  hosId?: string
  /** 整体禁用。 */
  disabled?: boolean
  /** 外层样式类。 */
  className?: string
  /** 选项装载完成后是否按首项组合回调一次（平台入口的初始范围解析依赖该回调）。 */
  selectFirstOption?: boolean
}

/** 空值：三级都未选择。 */
const EMPTY_VALUE: OrganizationScopeValue = {}

/**
 * 空选项清单：选项来自装载状态，未装载或该层没有可选项时按空清单展示。
 * 兜底用同一实例而不是就地写 `[]`，下游 Hook 的依赖引用才稳定，不因空值兜底而每次渲染重建。
 */
const NO_ORGANIZATIONS: readonly OrganizationScopeOrganizationOption[] = []
const NO_HOSPITALS: readonly OrganizationScopeHospitalOption[] = []
const NO_BRANCHES: readonly OrganizationScopeBranchOption[] = []

/** 范围值的比较键：实例标识与业务编码都参与，不看对象身份。 */
function selectionKey(value: OrganizationScopeValue): string {
  return `${value.orgId ?? ''}|${value.hosId ?? ''}|${value.branchId ?? ''}|${value.orgCode ?? ''}|${value.hosCode ?? ''}|${value.branchCode ?? ''}`
}

/** 按实例标识定位选项，标识未给或不在清单中时退回业务编码定位。 */
function matchOption<T extends OrganizationScopeOption>(
  options: readonly T[],
  id: string | undefined,
  code: string | undefined,
): T | undefined {
  if (id !== undefined) {
    const matched = options.find((option) => option.id === id)
    if (matched !== undefined) return matched
  }
  if (code !== undefined) return options.find((option) => option.code === code)
  return undefined
}

/** 选项组的扁平化：页面需要标识、业务编码与名称。 */
function toOptions(options: readonly OrganizationScopeOption[]): OrganizationScopeOption[] {
  return options.map((option) => ({ id: option.id, code: option.code, name: option.name }))
}

/** 三级选项组合 → 选择值（实例标识与业务编码同帧交付，编码一律取选项自身）。 */
function toValue(
  organization: OrganizationScopeOrganizationOption | null | undefined,
  hospital: OrganizationScopeHospitalOption | null | undefined,
  branch: OrganizationScopeBranchOption | null | undefined,
): OrganizationScopeValue {
  return {
    orgId: organization?.id,
    hosId: hospital?.id,
    branchId: branch?.id,
    orgCode: organization?.code,
    hosCode: hospital?.code,
    branchCode: branch?.code,
  }
}

/** 给定组合的选项清单。 */
function buildMeta(
  organizations: readonly OrganizationScopeOrganizationOption[],
  organization: OrganizationScopeOrganizationOption | null,
  hospital: OrganizationScopeHospitalOption | null,
  unconfiguredOrganizationCount: number,
): OrganizationScopeChangeMeta {
  return {
    organizations: toOptions(organizations),
    hospitals: toOptions(organization?.hospitals ?? []),
    branches: toOptions(hospital?.branches ?? []),
    unconfiguredOrganizationCount,
  }
}

/**
 * IAM 组织范围选择器：医共体 → 医院 → 院区三级联动，选择值同时携带组织实例标识与业务编码。
 *
 * - 选项来自本项目范围查询（IAM 组织树与组织类型），组件不直连外部目录、不做权限判定。
 * - 组织业务编码未配置的节点不进选项：业务请求的组织标识只能取业务编码，不得回落实例标识。
 * - 匿名与初始化状态不发起业务请求；会话切换后选项按新会话重新装载，迟到响应不覆盖新会话。
 * - 组织类型未配置或已停用的节点由服务端排除在选项之外，数量单独提示；读取失败给出重试入口。
 * - 组件只产出选择值，不持有页面数据；切换范围后的结果清理由页面承担。
 */
export function OrganizationScopeSelector({
  value = EMPTY_VALUE,
  onChange,
  orgId,
  hosId,
  disabled = false,
  className,
  selectFirstOption = false,
}: OrganizationScopeSelectorProps) {
  const client = useApiClientContext()
  const session = useHostSession()
  const authenticated = session.userId !== null
    && (session.status === 'authenticated' || session.status === 'refreshing')
  const sessionKey = `${session.sessionVersion}|${session.userId ?? ''}`

  const [loadState, setLoadState] = useState<{
    key: string
    options: OrganizationScopeOptions | null
    failed: boolean
  } | null>(null)
  const [retryNonce, setRetryNonce] = useState(0)

  useEffect(() => {
    if (!authenticated) return undefined
    const key = sessionKey
    let active = true
    void queryOrganizationScopeOptions(client).then(
      (options) => {
        if (active) setLoadState({ key, options, failed: false })
      },
      () => {
        if (active) setLoadState({ key, options: null, failed: true })
      },
    )
    return () => {
      active = false
    }
  }, [authenticated, client, retryNonce, sessionKey])

  const current = loadState !== null && loadState.key === sessionKey ? loadState : null
  const options = current?.options ?? null
  const failed = current?.failed === true
  const loading = authenticated && current === null

  const organizations = options?.organizations ?? NO_ORGANIZATIONS
  const unconfiguredOrganizationCount = options?.unconfiguredOrganizationCount ?? 0

  // 固定组织优先于受控值；固定值不在选项中时不回退到其他组织，避免把本院入口静默换成别家机构。
  const selectedOrganization = useMemo(
    () => matchOption(organizations, orgId ?? value.orgId, value.orgCode) ?? null,
    [organizations, orgId, value.orgCode, value.orgId],
  )

  const hospitals = selectedOrganization?.hospitals ?? NO_HOSPITALS
  const selectedHospital = useMemo(
    () => matchOption(hospitals, hosId ?? value.hosId, value.hosCode) ?? null,
    [hospitals, hosId, value.hosCode, value.hosId],
  )

  const branches = selectedHospital?.branches ?? NO_BRANCHES
  const selectedBranch = useMemo(
    () => matchOption(branches, value.branchId, value.branchCode) ?? null,
    [branches, value.branchCode, value.branchId],
  )

  /** 选项装载完成后按首项组合回调的默认值：固定层保持不变，其余层取该层第一项。 */
  const defaultSelection = useMemo(() => {
    if (!selectFirstOption || organizations.length === 0) return null
    const organization = selectedOrganization ?? (orgId === undefined ? organizations[0] : null)
    if (organization === null) return null
    const hospital = selectedHospital
      ?? (hosId === undefined
        ? matchOption(organization.hospitals, value.hosId, value.hosCode) ?? organization.hospitals[0]
        : null)
    if (hospital === null || hospital === undefined) return null
    const branch = matchOption(hospital.branches, value.branchId, value.branchCode) ?? hospital.branches[0]
    if (branch === undefined) return null
    return {
      value: toValue(organization, hospital, branch),
      meta: buildMeta(organizations, organization, hospital, unconfiguredOrganizationCount),
    }
  }, [hosId, organizations, orgId, selectFirstOption, selectedHospital, selectedOrganization, unconfiguredOrganizationCount, value.branchCode, value.branchId, value.hosCode, value.hosId])

  const valueKey = selectionKey(value)
  const lastEmittedRef = useRef('')

  useEffect(() => {
    if (defaultSelection === null) return
    const key = selectionKey(defaultSelection.value)
    if (key === valueKey || key === lastEmittedRef.current) return
    lastEmittedRef.current = key
    onChange?.(defaultSelection.value, defaultSelection.meta)
  }, [defaultSelection, onChange, valueKey])

  /** 用户选择某一级：下游层按新清单取首项，值三级不齐时照样回传，由页面决定是否接受。 */
  const changeOrganization = useCallback((nextId: string) => {
    const organization = organizations.find((item) => item.id === nextId) ?? null
    if (organization === null) return
    const hospital = organization.hospitals[0] ?? null
    const branch = hospital?.branches[0] ?? null
    const next = toValue(organization, hospital, branch)
    lastEmittedRef.current = selectionKey(next)
    onChange?.(next, buildMeta(organizations, organization, hospital, unconfiguredOrganizationCount))
  }, [onChange, organizations, unconfiguredOrganizationCount])

  const changeHospital = useCallback((nextId: string) => {
    const organization = selectedOrganization
    const hospital = organization?.hospitals.find((item) => item.id === nextId) ?? null
    if (organization === null || organization === undefined || hospital === null) return
    const branch = hospital.branches[0] ?? null
    const next = toValue(organization, hospital, branch)
    lastEmittedRef.current = selectionKey(next)
    onChange?.(next, buildMeta(organizations, organization, hospital, unconfiguredOrganizationCount))
  }, [onChange, organizations, selectedOrganization, unconfiguredOrganizationCount])

  const changeBranch = useCallback((nextId: string) => {
    const organization = selectedOrganization
    const hospital = selectedHospital
    const branch = hospital?.branches.find((item) => item.id === nextId) ?? null
    if (organization === null || organization === undefined || hospital === null || hospital === undefined) return
    const next = toValue(organization, hospital, branch)
    lastEmittedRef.current = selectionKey(next)
    onChange?.(next, buildMeta(organizations, organization, hospital, unconfiguredOrganizationCount))
  }, [onChange, organizations, selectedHospital, selectedOrganization, unconfiguredOrganizationCount])

  if (failed) {
    return <Alert
      type='error'
      showIcon
      title='组织范围读取失败'
      description='无法读取 IAM 组织树，请重试。'
      action={<Button size='small' onClick={() => { setLoadState(null); setRetryNonce((nonce) => nonce + 1) }}>重试</Button>}
    />
  }

  return <Space className={className} wrap>
    {orgId === undefined ? <Select
      aria-label='范围-医共体'
      placeholder='请选择医共体'
      style={{ minWidth: 160 }}
      loading={loading}
      disabled={disabled}
      value={selectedOrganization?.id}
      options={organizations.map((organization) => ({ value: organization.id, label: organization.name }))}
      onChange={changeOrganization}
    /> : null}
    {hosId === undefined ? <Select
      aria-label='范围-医院'
      placeholder='请选择医院'
      style={{ minWidth: 160 }}
      loading={loading}
      disabled={disabled || selectedOrganization === null}
      value={selectedHospital?.id}
      options={hospitals.map((hospital) => ({ value: hospital.id, label: hospital.name }))}
      onChange={changeHospital}
    /> : null}
    <Select
      aria-label='范围-院区'
      placeholder='请选择院区'
      style={{ minWidth: 160 }}
      loading={loading}
      disabled={disabled || selectedHospital === null}
      value={selectedBranch?.id}
      options={branches.map((branch) => ({ value: branch.id, label: branch.name }))}
      onChange={changeBranch}
    />
    {unconfiguredOrganizationCount > 0
      ? <Typography.Text type='secondary'>有 {unconfiguredOrganizationCount} 个组织未配置类型，未列入选项</Typography.Text>
      : null}
  </Space>
}
