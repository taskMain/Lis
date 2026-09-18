#!/usr/bin/env node
/**
 * 准备 Kiota 输入文档：
 * 1. 从后端 OpenAPI 端点下载原始文档
 * 2. 修正 ASP.NET 生成形状与 Kiota 消费方式不一致的两处兼容问题：
 *    - 整数联合类型：`["integer","string"]` -> `"integer"`
 *    - 可空引用：`oneOf [null, $ref]` -> `$ref`（Kiota 对 oneOf 会退化成对象类型，
 *      把可空枚举的请求体写成 `{}`；与 Dy.LisCenter 的生成入口保持同一口径）
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

const defaultUrl = process.env.MEDICAL_RECOGNITION_OPENAPI_URL ?? 'http://localhost:15014/openapi/v1.json'
const sourceUrl = process.argv[2] ?? defaultUrl
const packageRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const outputPath = path.join(packageRoot, 'openapi', 'medical-recognition.openapi.json')

/**
 * 归一化 OpenAPI 中生成端无法正确消费的形状。
 * 整数联合类型：ASP.NET 会把 int32 写成 ["integer","string"]，Kiota 无法据此生成 number。
 * 可空引用：ASP.NET 会把可空枚举/模型写成 oneOf [null, $ref]，Kiota 会退化成对象类型，
 * 使可空枚举在线上被序列化为 `{}`；归一为 `$ref` 后按数值正常读写。
 * 调用方以 counter 收集各类修正次数，便于生成时核对是否命中预期。
 */
function normalizeSchema(value, counter) {
  if (Array.isArray(value)) {
    const isIntegerUnion =
      value.length === 2 && value.includes('integer') && value.includes('string')
    if (isIntegerUnion) {
      counter.integerUnions += 1
      return 'integer'
    }
    return value.map((item) => normalizeSchema(item, counter))
  }
  if (value && typeof value === 'object') {
    if (Array.isArray(value.oneOf) && value.oneOf.length === 2) {
      const reference = value.oneOf.find((item) => typeof item?.$ref === 'string')
      const nullable = value.oneOf.some((item) => item?.type === 'null')
      if (reference && nullable) {
        delete value.oneOf
        value.$ref = reference.$ref
        counter.nullableReferences += 1
      }
    }
    const result = {}
    for (const [key, item] of Object.entries(value)) result[key] = normalizeSchema(item, counter)
    return result
  }
  return value
}

async function main() {
  const response = await fetch(sourceUrl)
  if (!response.ok) throw new Error(`下载 OpenAPI 失败: ${response.status} ${response.statusText} (${sourceUrl})`)
  const document = await response.json()
  const counter = { integerUnions: 0, nullableReferences: 0 }
  const fixed = normalizeSchema(document, counter)
  await mkdir(path.dirname(outputPath), { recursive: true })
  await writeFile(outputPath, JSON.stringify(fixed, null, 2), 'utf8')
  const paths = Object.keys(fixed.paths ?? {}).length
  console.log(`来源: ${sourceUrl}`)
  console.log(`修正整数联合类型: ${counter.integerUnions} 处`)
  console.log(`修正可空引用: ${counter.nullableReferences} 处`)
  console.log(`path 数量: ${paths}`)
  console.log(`已写入: ${outputPath}`)
}

main().catch((error) => {
  process.stderr.write(`ERROR: ${error.message}\n`)
  process.exitCode = 1
})
