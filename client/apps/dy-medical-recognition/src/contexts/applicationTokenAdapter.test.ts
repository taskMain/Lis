import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApplicationTokenAuthenticationProvider } from '@dy/api-client-medical-recognition'

/** 宿主 MicroSDK 替身：只观察应用令牌签发与会话版本，不发出真实请求。 */
const mocks = vi.hoisted(() => ({
  getApplicationToken: vi.fn(),
  getSession: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({
  MicroSDK: { auth: { getApplicationToken: mocks.getApplicationToken, getSession: mocks.getSession } },
}))

const baseUrl = 'http://localhost:15014'

function buildRequest(url: string) {
  const add = vi.fn()
  return {
    add,
    request: {
      URL: url,
      headers: { add, has: vi.fn(() => false), delete: vi.fn() },
    },
  }
}

beforeEach(() => {
  mocks.getApplicationToken.mockReset()
  mocks.getSession.mockReset()
})

describe('宿主应用令牌请求鉴权', () => {
  it('宿主签发令牌且会话版本一致时为请求附加 Bearer', async () => {
    mocks.getApplicationToken.mockResolvedValue({ status: 'issued', token: 'token-1', sessionVersion: 3 })
    mocks.getSession.mockResolvedValue({ status: 'authenticated', sessionVersion: 3 })
    const { add, request } = buildRequest(`${baseUrl}/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope`)

    await createApplicationTokenAuthenticationProvider().authenticateRequest(request as never)

    expect(add).toHaveBeenCalledWith('Authorization', 'Bearer token-1')
  })

  it('宿主未签发令牌时以可判定错误失败，不附加鉴权头', async () => {
    mocks.getApplicationToken.mockResolvedValue({ status: 'unavailable' })
    const { add, request } = buildRequest(`${baseUrl}/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope`)

    await expect(createApplicationTokenAuthenticationProvider().authenticateRequest(request as never))
      .rejects.toMatchObject({ code: 'APPLICATION_TOKEN_UNAVAILABLE' })
    expect(add).not.toHaveBeenCalled()
  })

  it('令牌签发后会话版本已变化时丢弃请求，不附加鉴权头', async () => {
    mocks.getApplicationToken.mockResolvedValue({ status: 'issued', token: 'token-1', sessionVersion: 2 })
    mocks.getSession.mockResolvedValue({ status: 'authenticated', sessionVersion: 3 })
    const { add, request } = buildRequest(`${baseUrl}/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope`)

    await expect(createApplicationTokenAuthenticationProvider().authenticateRequest(request as never))
      .rejects.toThrow('IAM session changed; discard this request')
    expect(add).not.toHaveBeenCalled()
  })

  it('请求目标越出配置的后端时拒绝，不取得令牌', async () => {
    const { add, request } = buildRequest('http://evil.example.com/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope')
    const guard = (url: string) => {
      const base = new URL(baseUrl)
      const destination = new URL(url)
      const prefix = base.pathname.replace(/\/$/, '')
      if (destination.origin !== base.origin
        || (destination.pathname !== prefix && !destination.pathname.startsWith(prefix + '/'))) {
        throw new Error('API request is outside the configured backend')
      }
    }

    await expect(createApplicationTokenAuthenticationProvider(guard).authenticateRequest(request as never))
      .rejects.toThrow('API request is outside the configured backend')
    expect(mocks.getApplicationToken).not.toHaveBeenCalled()
    expect(add).not.toHaveBeenCalled()
  })
})
