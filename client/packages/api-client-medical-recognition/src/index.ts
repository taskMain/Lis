// === 第一部分：Re-export Kiota 生成的内容 ===
export { createMedicalRecognitionClient, type MedicalRecognitionClient } from './medicalRecognitionClient.js';
export * from './api/index.js';

// Models - Kiota 1.34 把全部请求模型集中生成在 models/index.ts
export * from './models/index.js';

// 生成模型把 DateOnly 按类型引入，调用方需要构造该值时从本包取值，避免再声明底层依赖。
export { DateOnly } from '@microsoft/kiota-abstractions';

// === 第二部分：人工维护的便捷创建入口 ===
import { createMedicalRecognitionClient, type MedicalRecognitionClient } from './medicalRecognitionClient.js';
import {
  ApplicationTokenUnavailableError,
  MEDICAL_RECOGNITION_APP_CODE,
  createApplicationTokenAuthenticationProvider,
  createMedicalRecognitionRequestAdapter,
} from './applicationTokenAdapter.js';

export {
  ApplicationTokenUnavailableError,
  MEDICAL_RECOGNITION_APP_CODE,
  createApplicationTokenAuthenticationProvider,
  createMedicalRecognitionRequestAdapter,
};

/**
 * 创建 API Client 实例：Authorization 统一取自宿主应用令牌，401 上报宿主认证流程；
 * 令牌未签发时不发送无鉴权请求，错误交宿主边界处理。
 * @param baseUrl - API 基础 URL
 */
export function createMedicalRecognitionApiClient(baseUrl: string): MedicalRecognitionClient {
  return createMedicalRecognitionClient(createMedicalRecognitionRequestAdapter(baseUrl));
}
