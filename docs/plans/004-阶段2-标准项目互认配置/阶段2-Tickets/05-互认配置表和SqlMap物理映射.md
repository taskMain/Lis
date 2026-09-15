# 05: 互认配置表和 SqlMap 物理映射

**What to build:** 交付互认项目配置的最终 PostgreSQL 表定义和持久化映射，使配置写入只访问平台 `mrec_` 表，并具备唯一性、列注释和可审查的 SQL 语句。

**Blocked by:** 02 完成公共契约基线；03/04 完成影响字段和语句清单（不要求真实运行验证完成）

**Status:** ready-for-agent

- [ ] 交付 `mrec_mutual_recognition_item` 最终 DDL：列序、类型、可空性、无数据库默认值和中文表/列注释明确。
- [ ] 建立覆盖启用与停用配置的唯一索引 `ux_mrec_mutual_recognition_org_project`。
- [ ] 不创建 `StandardItemId` 外键，不操作其他系统表或数据。
- [ ] 仅写入、修改和启停语句使用 `MutualRecognitionItem` 实体 scope，DataMapper 显式传 scope，不使用 `SetContext`；查询语句和查询 scope由Ticket 04负责。
- [ ] SQL 显式引用 `mrec_mutual_recognition_item`，补齐 Statement 和字段中文注释。
- [ ] 为唯一索引补 `COMMENT ON INDEX`，说明同一组织标准项目配置唯一且包含停用配置。
- [ ] 只交付脚本和映射，不执行目标数据库建表；负责人确认目标库/schema后再进行集成验证。

**Verification and evidence:**

- [ ] 先建立现有脚本/SqlMap不符合设计的静态失败证据，再交付最终脚本和映射。
- [ ] 覆盖 V17-V18、V23、V25 的静态面；数据库执行证据由 Ticket 08 归档。
- [ ] 按项目现行规范维护生成物并完成必要检查，证据归档到阶段 `testReport.md`。
