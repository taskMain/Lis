# 06: 累计 API Client 生成与契约校验

**What to build:** 将稳定的阶段2后端契约加入累计 API Client，保留阶段1已交付接口，排除后续功能和登录接口，并确保生成入口、锁文件和手写入口可追溯。

**Blocked by:** 02 完成公共契约基线；04 完成查询字段和语句清单（不依赖 Ticket 05 的数据库物理映射）

**Status:** ready-for-agent

- [ ] 保留阶段1已交付 F01 接口，加入阶段2稳定契约，不提前纳入后续阶段功能。
- [ ] 保留 `/auth/login` 排除规则。
- [ ] 生成前核对累计接口清单、稳定 OpenAPI 来源和锁文件；生成后核对公开入口。
- [ ] 核对创建/查询 Request 的组织字段差异、ReadModel 可空性、枚举和排除字段。
- [ ] 不手改 Kiota 生成模型；按保护约定恢复并核对手写 `src/index.ts`。

**Verification and evidence:**

- [ ] 先建立 OpenAPI/生成范围失败证据，再生成并精确复核。
- [ ] 覆盖 C22、C24；生成后执行 API Client package `typecheck` 和 `build`，证据归档到阶段 `testReport.md`。
- [ ] 完成适用检查；本票不等待 Ticket 05。
