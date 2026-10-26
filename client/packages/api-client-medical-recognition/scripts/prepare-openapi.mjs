#!/usr/bin/env node
/**
 * 准备 Kiota 输入文档：
 * 1. 从后端 OpenAPI 端点下载原始文档
 * 2. 修正 ASP.NET 生成形状与 Kiota 消费方式不一致的兼容问题：
 *    - 整数联合类型：`["integer","string"]` -> `"integer"`
 *    - 单引用 oneOf：`type` + `oneOf: [$ref]` + `nullable` -> `$ref`（Kiota 对 oneOf 会退化成对象
 *      类型，把可空枚举的请求体写成 `{}`）
 *    - 可空标量：`type: X` + `nullable: true` -> `type: ["null", X]`
 *    - 两元素可空引用：`oneOf [null, $ref]` -> `$ref`
 *      以上与冻结快照和 Dy.LisCenter 的生成入口保持同一口径
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
 * 归一化 OpenAPI 中生成端无法正确消费的形状，使 Kiota 输入与冻结快照保持同一形态。
 *
 * 1. 整数联合类型：ASP.NET 会把 int32 写成 ["integer","string"]，Kiota 无法据此生成 number。
 * 2. 单引用 oneOf：ASP.NET 会把可空枚举/模型写成 `type` + `oneOf: [$ref]` + `nullable`，
 *    Kiota 会退化成对象类型，使可空枚举在线上被序列化为 `{}`；归一为 `$ref` 后按声明类型正常读写。
 * 3. 可空标量：ASP.NET 写成 `type: X` + `nullable: true`，归一为 JSON Schema 的 `type: ["null", X]`，
 *    与冻结快照一致，Kiota 据此生成 `X | null`。
 * 4. 两元素可空引用：旧形态的 `oneOf: [null, $ref]` 同样归并为 `$ref`。
 *
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
    if (Array.isArray(value.oneOf) && value.oneOf.length === 1 && typeof value.oneOf[0]?.$ref === 'string') {
      // 单引用 oneOf：保留描述等注解，类型信息由引用表达。
      const normalized = {}
      if (typeof value.description === 'string') normalized.description = value.description
      normalized.$ref = value.oneOf[0].$ref
      counter.nullableReferences += 1
      return normalizeSchema(normalized, counter)
    }
    if (Array.isArray(value.oneOf) && value.oneOf.length === 2) {
      const reference = value.oneOf.find((item) => typeof item?.$ref === 'string')
      const nullable = value.oneOf.some((item) => item?.type === 'null')
      if (reference && nullable) {
        delete value.oneOf
        delete value.nullable
        value.$ref = reference.$ref
        counter.nullableReferences += 1
      }
    }
    if (value.nullable === true && typeof value.type === 'string') {
      value.type = ['null', value.type]
      delete value.nullable
      counter.nullableTypes += 1
    } else if (value.nullable === true && Array.isArray(value.type)) {
      if (!value.type.includes('null')) value.type = ['null', ...value.type]
      delete value.nullable
      counter.nullableTypes += 1
    } else if (value.nullable === true) {
      // 引用上的 nullable 不改变引用语义：引用目标自身允许为空时由调用方按缺失处理。
      delete value.nullable
      counter.nullableTypes += 1
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
  const counter = { integerUnions: 0, nullableReferences: 0, nullableTypes: 0 }
  const fixed = normalizeSchema(document, counter)
  // 稳定输入统一声明为 OpenAPI 3.1.1：上述可空形态使用 JSON Schema 的 `type` 数组表达，
  // 3.0 文档中的同形写法会被 Kiota 判为无效并退化成 UntypedNode。
  if (fixed.openapi !== '3.1.1') {
    counter.openApiVersion = 1
    fixed.openapi = '3.1.1'
  }
  await mkdir(path.dirname(outputPath), { recursive: true })
  await writeFile(outputPath, JSON.stringify(fixed, null, 2), 'utf8')
  const paths = Object.keys(fixed.paths ?? {}).length
  console.log(`来源: ${sourceUrl}`)
  console.log(`修正整数联合类型: ${counter.integerUnions} 处`)
  console.log(`修正可空引用: ${counter.nullableReferences} 处`)
  console.log(`修正可空标量: ${counter.nullableTypes} 处`)
  console.log(`规整 OpenAPI 版本: ${counter.openApiVersion ?? 0} 处`)
  console.log(`path 数量: ${paths}`)
  console.log(`已写入: ${outputPath}`)
}

main().catch((error) => {
  process.stderr.write(`ERROR: ${error.message}\n`)
  process.exitCode = 1
})
