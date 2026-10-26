import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApplicationTokenAuthenticationProvider } from '@dy/api-client-medical-recognition'

/** 宿主 MicroSDK 替身：只观察应用令牌签发，不发出真实请求。 */
const mocks = vi.hoisted(() => ({
  getApplicationToken: vi.fn(),
}))

vi.mock('@dy/micro-sdk', () => ({
  MicroSDK: { auth: { getApplicationToken: mocks.getApplicationToken } },
}))

beforeEach(() => {
  mocks.getApplicationToken.mockReset()
})

describe('宿主应用令牌请求鉴权', () => {
  it('宿主签发令牌时为请求附加 Bearer', async () => {
    mocks.getApplicationToken.mockResolvedValue({ status: 'issued', token: 'token-1' })
    const provider = createApplicationTokenAuthenticationProvider()
    const add = vi.fn()
    const request = {
      URL: 'http://localhost:15014/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope',
      headers: { add, has: vi.fn(() => false), delete: vi.fn() },
    }

    await provider.authenticateRequest(request as never)

    expect(add).toHaveBeenCalledWith('Authorization', 'Bearer token-1')
  })

  it('宿主未签发令牌时以可判定错误失败，不附加鉴权头', async () => {
    mocks.getApplicationToken.mockResolvedValue({ status: 'unavailable' })
    const provider = createApplicationTokenAuthenticationProvider()
    const add = vi.fn()
    const request = {
      URL: 'http://localhost:15014/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope',
      headers: { add, has: vi.fn(() => false), delete: vi.fn() },
    }

    await expect(provider.authenticateRequest(request as never))
      .rejects.toMatchObject({ code: 'APPLICATION_TOKEN_UNAVAILABLE' })
    expect(add).not.toHaveBeenCalled()
  })
})
