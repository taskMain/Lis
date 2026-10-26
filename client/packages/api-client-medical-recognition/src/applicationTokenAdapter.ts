import {
  AllowedHostsValidator,
  BaseBearerTokenAuthenticationProvider,
  type AccessTokenProvider,
  type AuthenticationProvider,
  type RequestAdapter,
} from '@microsoft/kiota-abstractions'
import { FetchRequestAdapter, KiotaClientFactory } from '@microsoft/kiota-http-fetchlibrary'
import { MicroSDK } from '@dy/micro-sdk'

/** 本应用在 IAM 中的微应用编码；宿主据此校验并签发目标应用令牌。 */
export const MEDICAL_RECOGNITION_APP_CODE = 'medical-recognition'

/** 宿主未签发目标应用令牌：请求不发送，错误交宿主边界处理。 */
export class ApplicationTokenUnavailableError extends Error {
  readonly code = 'APPLICATION_TOKEN_UNAVAILABLE'

  constructor() {
    super()
    this.name = 'ApplicationTokenUnavailableError'
  }
}

/** 应用令牌提供者：Authorization 的唯一来源；宿主未签发时让请求在发送前失败。 */
const applicationTokenProvider: AccessTokenProvider = {
  async getAuthorizationToken(): Promise<string> {
    const applicationToken = await MicroSDK.auth.getApplicationToken({
      targetAppCode: MEDICAL_RECOGNITION_APP_CODE,
    })
    if (applicationToken.status !== 'issued') {
      throw new ApplicationTokenUnavailableError()
    }
    return applicationToken.token
  },
  getAllowedHostsValidator(): AllowedHostsValidator {
    return new AllowedHostsValidator()
  },
}

/** 应用令牌鉴权提供者：供适配器与测试直接断言 Authorization 头。 */
export function createApplicationTokenAuthenticationProvider(): AuthenticationProvider {
  return new BaseBearerTokenAuthenticationProvider(applicationTokenProvider)
}

/**
 * 业务请求适配器：Authorization 统一取自宿主应用令牌，不使用宿主 authenticateRequest
 * 的跨 Origin 签名路径（当前宿主在该路径返回空 headers，会发出注定 401 的请求）。
 * 401 仍上报宿主认证流程，错误由宿主边界处理。
 */
export function createMedicalRecognitionRequestAdapter(baseUrl: string): RequestAdapter {
  const customFetch = async (url: string, init: RequestInit): Promise<Response> => {
    const response = await fetch(url, init)
    if (response.status === 401) {
      await MicroSDK.auth.reportUnauthorized({
        url,
        method: init.method ?? 'GET',
        status: 401,
      })
    }
    return response
  }
  const adapter = new FetchRequestAdapter(
    createApplicationTokenAuthenticationProvider(),
    undefined,
    undefined,
    KiotaClientFactory.create(customFetch, []),
  )
  adapter.baseUrl = baseUrl
  return adapter
}
