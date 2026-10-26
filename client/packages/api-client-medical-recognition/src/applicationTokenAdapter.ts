import type { AuthenticationProvider } from '@microsoft/kiota-abstractions'
import { FetchRequestAdapter, KiotaClientFactory } from '@microsoft/kiota-http-fetchlibrary'
import { MicroSDK } from '@dy/micro-sdk'
import { ErrorHandlerMiddleware, setHostApiErrorHandler } from '@dy/kiota-middleware'

/** 本应用在 IAM 中的微应用编码；宿主据此签发目标应用令牌。 */
export const MEDICAL_RECOGNITION_APP_CODE = 'medical-recognition'

/** 宿主未签发目标应用令牌：请求不发送，错误交宿主边界处理。 */
export class ApplicationTokenUnavailableError extends Error {
  readonly code = 'APPLICATION_TOKEN_UNAVAILABLE'

  constructor() {
    super('IAM application token unavailable')
    this.name = 'ApplicationTokenUnavailableError'
  }
}

/** 业务 API 的鉴权与目标地址配置。 */
export interface MedicalRecognitionApiOptions {
  baseUrl: string
  /** 是否安装宿主全局 API 异常拦截；只有明确关闭时才传 false。 */
  interceptErrors?: boolean
}

/** 目标地址守卫：请求只能落在配置的后端 origin 与路径前缀内。 */
function createDestinationGuard(base: URL): (url: string) => void {
  const prefix = base.pathname.replace(/\/$/, '')
  return (url: string) => {
    const destination = new URL(url)
    if (
      destination.origin !== base.origin
      || destination.username
      || destination.password
      || (destination.pathname !== prefix && !destination.pathname.startsWith(prefix + '/'))
    ) {
      throw new Error('API request is outside the configured backend')
    }
  }
}

/**
 * 应用令牌鉴权提供者：Authorization 的唯一来源。
 *
 * 逐请求执行目标地址校验、清除调用方自带的 Authorization、校验令牌签发与会话版本，
 * 避免宿主会话在请求在途期间切换后仍用旧令牌发出请求。
 */
export function createApplicationTokenAuthenticationProvider(
  checkDestination: (url: string) => void = () => {},
): AuthenticationProvider {
  return {
    async authenticateRequest(request) {
      checkDestination(request.URL)
      request.headers.delete('Authorization')
      const issued = await MicroSDK.auth.getApplicationToken({ targetAppCode: MEDICAL_RECOGNITION_APP_CODE })
      if (issued.status !== 'issued' || !issued.token) throw new ApplicationTokenUnavailableError()
      const session = await MicroSDK.auth.getSession()
      if (session.status !== 'authenticated' || session.sessionVersion !== issued.sessionVersion) {
        throw new Error('IAM session changed; discard this request')
      }
      request.headers.add('Authorization', 'Bearer ' + issued.token)
    },
  }
}

/**
 * 业务请求适配器：应用令牌鉴权 + 目标地址守卫 + 宿主全局 API 异常拦截。
 *
 * 与基座约定的默认能力一致：默认安装 ErrorHandlerMiddleware，并把错误事件交给宿主统一呈现；
 * 不跟随重定向、不携带凭据 Cookie，避免把令牌带到配置之外的目标。
 */
export function createMedicalRecognitionRequestAdapter(
  baseUrl: string,
  options: Omit<MedicalRecognitionApiOptions, 'baseUrl'> = {},
): FetchRequestAdapter {
  const { interceptErrors = true } = options
  const base = new URL(baseUrl)
  if (!['http:', 'https:'].includes(base.protocol) || base.username || base.password || base.search || base.hash) {
    throw new Error('Invalid API baseUrl')
  }

  const checkDestination = createDestinationGuard(base)

  if (interceptErrors) setHostApiErrorHandler((error) => MicroSDK.reportApiError(error))

  const safeFetch = (url: string, init: RequestInit): Promise<Response> => {
    checkDestination(url)
    return fetch(url, { ...init, redirect: 'error', credentials: 'omit' })
  }

  // preview.102 中空链等价于默认链（含重试）；关闭拦截时显式给一条直通链，避免误用默认重试。
  const httpClient = KiotaClientFactory.create(
    safeFetch,
    interceptErrors ? [new ErrorHandlerMiddleware()] : [{ next: undefined, execute: safeFetch }],
  )
  const adapter = new FetchRequestAdapter(
    createApplicationTokenAuthenticationProvider(checkDestination),
    undefined,
    undefined,
    httpClient,
  )
  adapter.baseUrl = base.href.replace(/\/$/, '')
  return adapter
}
