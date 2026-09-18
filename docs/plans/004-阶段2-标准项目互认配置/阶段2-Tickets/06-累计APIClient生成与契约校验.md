# 06: 累计 API Client 生成与契约校验

**What to build:** 将稳定的阶段2后端契约加入累计 API Client，保留阶段1已交付接口，排除后续功能和登录接口，并确保生成入口、锁文件和手写入口可追溯。

**Blocked by:** 02 完成公共契约基线；04 完成查询字段和语句清单（不依赖 Ticket 05 的数据库物理映射）

**Status:** done（2026-09-15 交付；页面接线由 Ticket 07 完成，枚举契约与中文来源由批次 10/11 追加修复并重生成）

- [x] 保留阶段1已交付 F01 接口，加入阶段2稳定契约，不提前纳入后续阶段功能。累计范围对比（该轮时点值；批次10/11 后冻结文档为 23 path）：生成前 21 path（20 业务 + `/auth/login`）→ 生成后 22，**净增 1 个查询端点**（`/Api/MedicalRecognitionReportQuery/QueryRecognitionProjectConfigurationList`）；16 个写端点与阶段1 四个目录查询端点全部保留，无丢失、无后续阶段端点。
- [x] 保留 `/auth/login` 排除规则。生成命令固定带 `--exclude-path /auth/login`；生成物无 `api/auth` 目录；锁文件 `excludePatterns` 仍为 `/auth/login`。
- [x] 生成前核对累计接口清单、稳定 OpenAPI 来源和锁文件；生成后核对公开入口。来源 `http://localhost:15014/openapi/v1.json`（后端实测 22 path，该轮时点值；批次10/11 后为 23 path），`prepare-openapi` 修正整数联合类型 3 处；生成后 `src/api/medicalRecognitionReportQuery/index.ts` 已含 `queryRecognitionProjectConfigurationList`，`src/index.ts` 经 `export *` 覆盖，dist d.ts 已导出。
- [x] 核对创建/查询 Request 的组织字段差异、ReadModel 可空性、枚举和排除字段。`organizationCode` 由生成前 4 处收敛为仅查询 Request 的 3 处（`CreateMutualRecognitionItemRequest` 已清除）；查询请求 `organizationCode` 与 `standardProjectCode` 可选、`configurationStatus` 可选；ReadModel `unavailableReason?: string|null` 可空；`itemType`/`configurationStatus` 生成为 `number|null`。**批次日 2026-09-16**：两个枚举已在 OpenAPI 声明 `enum`/`x-enumNames`/`x-enumDescriptions`，生成端按数值读写且不再产出空对象类型（生成端不产出 TypeScript 枚举），本项与阶段1 ReadModel 形态一致，见阶段 `testReport.md` 的枚举契约修复与中文来源两节。
- [x] 不手改 Kiota 生成模型；按保护约定恢复并核对手写 `src/index.ts`。未手改生成目录；`src/index.ts` 生成前后 SHA256 一致（未被覆盖，无需恢复）；未使用 `--clean-output`。

**Verification and evidence:**

- [x] 先建立 OpenAPI/生成范围失败证据，再生成并精确复核。失败证据：生成前稳定 OpenAPI 与 `src/models/index.ts` 仍带已被后端移除的 `organizationCode`（`:359/:361/:464/:1169`）、且缺少阶段2查询端点。
- [x] 覆盖 C22、C24；生成后执行 API Client package `typecheck` 和 `build`，证据归档到阶段 `testReport.md`。`typecheck` exit 0；`build` exit 0（esm 72.12KB / cjs 87.65KB / dts 98.89KB）；子应用 `build` exit 0（3273 modules）；主线程独立复跑包 `typecheck`/`build` 结果一致。
- [x] 完成适用检查；本票不等待 Ticket 05。`git diff --check` exit 0；变更集中于生成物与稳定 OpenAPI，无新增日志、临时文件或本机路径。
