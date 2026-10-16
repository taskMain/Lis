/**
 * 「报告详情」三个 I/O 出口的共享接线（两个入口容器逐字重复实现的收敛）。
 *
 * hook 直接取用 `./reportsApi` 适配层出口并注入当前 ApiClient：不创建 Client、不接触
 * 生成类型，请求构造与响应收敛仍全部由适配层承担（Frontend Application 第 1 节分层）。
 */
import { useCallback } from 'react'
import { useApiClientContext } from '../../contexts/ApiClientContext'
import {
  downloadReportVersionPdf,
  queryReportVersionDetail,
  queryReportVersions,
} from './reportsApi'

/**
 * 详情路由页注入用的三个出口：版本列表读取、版本详情读取与 PDF 下载，
 * 与列表页使用同一适配层出口，回调引用随 client 变化而更新。
 */
export function useReportDetailApi() {
  const client = useApiClientContext()
  /** 读取某个报告的全部版本；由详情路由页注入使用。 */
  const queryVersions = useCallback((reportId: string) => queryReportVersions(client, reportId), [client])
  /** 读取某个报告版本的完整内容；由详情路由页注入使用。 */
  const queryDetail = useCallback(
    (reportId: string, reportVersionId: string) => queryReportVersionDetail(client, reportId, reportVersionId),
    [client],
  )
  /** 下载某个报告版本的原始 PDF；由详情路由页注入使用。 */
  const downloadVersion = useCallback(
    (reportId: string, reportVersionId: string, downloadFileName: string) =>
      downloadReportVersionPdf(client, reportId, reportVersionId, downloadFileName),
    [client],
  )
  return { queryVersions, queryDetail, downloadVersion }
}
