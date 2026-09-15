// === 第一部分：Re-export Kiota 生成的内容 ===
export { createMedicalRecognitionClient, type MedicalRecognitionClient } from './medicalRecognitionClient.js';
export * from './api/index.js';

// Models - Kiota 1.34 把全部请求模型集中生成在 models/index.ts
export * from './models/index.js';

// === 第二部分：便捷创建函数 ===
import { createMedicalRecognitionClient, type MedicalRecognitionClient } from './medicalRecognitionClient.js';
import { createAuthenticatedAdapter } from '@dy/auth';

/**
 * 创建 API Client 实例（自动注入 Bearer Token，并上报 Kiota 非 2xx 响应）
 * @param baseUrl - API 基础 URL
 */
export function createMedicalRecognitionApiClient(baseUrl: string): MedicalRecognitionClient {
  const adapter = createAuthenticatedAdapter(baseUrl);
  return createMedicalRecognitionClient(adapter);
}