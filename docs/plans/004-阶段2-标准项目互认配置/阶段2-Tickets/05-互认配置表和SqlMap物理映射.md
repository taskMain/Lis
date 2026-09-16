# 05: 互认配置表和 SqlMap 物理映射

**What to build:** 交付互认项目配置的最终 PostgreSQL 表定义和持久化映射，使配置写入只访问平台 `mrec_` 表，并具备唯一性、列注释和可审查的 SQL 语句。

**Blocked by:** 02 完成公共契约基线；03/04 完成影响字段和语句清单（不要求真实运行验证完成）

**Status:** done（2026-09-15 交付脚本与映射；建表由负责人（迁移执行方）执行，2026-09-16 完成并只读核实）

- [x] 交付 `mrec_mutual_recognition_item` 最终 DDL：列序、类型、可空性、无数据库默认值和中文表/列注释明确。8 列列序与类型逐项对齐设计，全部 NOT NULL 且无数据库默认值。
- [x] 建立覆盖启用与停用配置的唯一索引 `ux_mrec_mutual_recognition_org_project`。覆盖 `(organization_code, standard_project_code)`，无状态过滤。
- [x] 不创建 `StandardItemId` 外键，不操作其他系统表或数据。
- [x] 仅写入、修改和启停语句使用 `MutualRecognitionItem` 实体 scope，DataMapper 显式传 scope，不使用 `SetContext`；查询语句和查询 scope由Ticket 04负责。已随 Ticket 03 交付并复核：`MutualRecognitionItem.xml` 的 `Scope` 与仓储 `MutualRecognitionItemScope` 常量同为 `MutualRecognitionItem`，四个写方法与新增读取方法均逐调用显式传 `scope`/`sqlId`，无 `SetContext`；查询语句落在 `MedicalRecognitionReportQuery.xml` 并注册在 `MedicalRecognitionReportQuery` scope。
- [x] SQL 显式引用 `mrec_mutual_recognition_item`，补齐 Statement 和字段中文注释。互认配置语句引用的物理表经核对只有 `mrec_mutual_recognition_item`；新增的按编码读取语句引用 `mrec_medical_standard_item` 且带注释。
- [x] 为唯一索引补 `COMMENT ON INDEX`，说明同一组织标准项目配置唯一且包含停用配置。
- [x] 本票只交付脚本与映射，不代执行目标数据库建表：目标库/schema 由负责人（迁移执行方）确认后在 `DysoftHIS.public` 执行建表，执行完成后以只读元数据逐项核实通过（V18/V25 记 `Passed`）。

**Verification and evidence:**

- [x] 先建立现有脚本/SqlMap不符合设计的静态失败证据，再交付最终脚本和映射。旧脚本缺 `mrec_` 前缀、用 `varchar(100)`、无注释与唯一索引；旧 SqlMap 为聚合 scope、无前缀表名、3 处 `current_timestamp`、启停无条件。
- [x] 覆盖 V17-V18、V23、V25 的静态面；数据库执行证据由 Ticket 08 归档。V17/V18/V23(静态)/V25 静态面已核对；V18/V25 的真实列类型与约束已随建表完成，以只读元数据核实（见阶段 `testReport.md`）；V17/V23 的运行查找键已在宿主链路中执行（写入、查询解析成功，见阶段 `testReport.md` 的宿主验收章节）。
- [x] 按项目现行规范维护生成物并完成必要检查，证据归档到阶段 `testReport.md`（"本轮执行结果"章节）。
