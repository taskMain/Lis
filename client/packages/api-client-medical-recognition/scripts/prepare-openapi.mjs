#!/usr/bin/env node
/**
 * 准备 Kiota 输入文档：
 * 1. 从后端 OpenAPI 端点下载原始文档
 * 2. 修正 ASP.NET 生成的 int32 兼容问题（["integer","string"] -> "integer"）
 * 3. 写入 openapi/medical-recognition.openapi.json，作为 Kiota 的稳定输入
 *
 * 用法：
 *   node scripts/prepare-openapi.mjs [openapi-url]
 * 默认地址可用环境变量覆盖：MEDICAL_RECOGNITION_OPENAPI_URL
 */
import { mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const defaultUrl = process.env.MEDICAL_RECOGNITION_OPENAPI_URL ?? 'http://localhost:5008/openapi/v1.json'
const sourceUrl = process.argv[2] ?? defaultUrl
const packageRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const outputPath = path.join(packageRoot, 'openapi', 'medical-recognition.openapi.json')

/**
 * 修正 OpenAPI 中的整数联合类型。
 * ASP.NET 会把可空 int? 之外的 int32 写成 ["integer","string"]，Kiota 无法据此生成 number。
 */
function fixIntegerUnions(value, counter) {
  if (Array.isArray(value)) {
    const isIntegerUnion =
      value.length === 2 && value.includes('integer') && value.includes('string')
    if (isIntegerUnion) {
      counter.count += 1
      return 'integer'
    }
    return value.map((item) => fixIntegerUnions(item, counter))
  }
  if (value && typeof value === 'object') {
    const result = {}
    for (const [key, item] of Object.entries(value)) result[key] = fixIntegerUnions(item, counter)
    return result
  }
  return value
}

async function main() {
  const response = await fetch(sourceUrl)
  if (!response.ok) throw new Error(`下载 OpenAPI 失败: ${response.status} ${response.statusText} (${sourceUrl})`)
  const document = await response.json()
  const counter = { count: 0 }
  const fixed = fixIntegerUnions(document, counter)
  await mkdir(path.dirname(outputPath), { recursive: true })
  await writeFile(outputPath, JSON.stringify(fixed, null, 2), 'utf8')
  const paths = Object.keys(fixed.paths ?? {}).length
  console.log(`来源: ${sourceUrl}`)
  console.log(`修正整数联合类型: ${counter.count} 处`)
  console.log(`path 数量: ${paths}`)
  console.log(`已写入: ${outputPath}`)
}

main().catch((error) => {
  process.stderr.write(`ERROR: ${error.message}\n`)
  process.exitCode = 1
})
