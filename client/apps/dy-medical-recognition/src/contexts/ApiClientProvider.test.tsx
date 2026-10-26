import { render } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiClientProvider } from './ApiClientProvider'

/** 生成 Client 工厂替身：只观察 Provider 的装配参数，不发出真实请求。 */
const mocks = vi.hoisted(() => ({
  createClient: vi.fn(),
}))

vi.mock('@dy/api-client-medical-recognition', () => ({
  createMedicalRecognitionApiClient: mocks.createClient,
}))

beforeEach(() => {
  mocks.createClient.mockReset()
})

describe('ApiClientProvider 宿主请求适配器', () => {
  it('业务 Client 由宿主应用令牌工厂按 API 基地址创建', () => {
    const client = { name: 'medical-recognition-client' }
    mocks.createClient.mockReturnValue(client)

    render(
      <ApiClientProvider baseUrl='http://localhost:15014'>
        <span>页面内容</span>
      </ApiClientProvider>,
    )

    expect(mocks.createClient).toHaveBeenCalledTimes(1)
    expect(mocks.createClient).toHaveBeenCalledWith('http://localhost:15014')
  })
})
