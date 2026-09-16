# 04: 建表脚本与SqlMap物理映射

**What to build:** 交付目标表「组织医院院区互认项目金额」（表名 `mrec_organization_hospital_branch_recognition_amount`）的最终建表脚本：列序与类型严格按设计（业务键列 `organization_code`、`hospital_code`、`branch_code`、`standard_project_code`）、全部列无数据库默认值、表与每列与唯一索引注释齐备、四个业务键列上的唯一索引 `ux_mrec_org_hos_brh_project` 无状态过滤；并把金额读写用的 SqlMap 与物理表、实体 scope、每个调用点的语句 Id 逐一对齐（按业务键读单行、插入、按主键与业务键条件更新；查询语句与既有查询同 scope 同文件）。最终脚本位置为 `Repository/Scripts/organization_hospital_branch_recognition_amount.sql`（列序、类型、注释逐字口径以 `Server/design.md` 的数据库、SQL 与 scope 章节为准）。本票起点为 `Server/design.md` 设计基线段登记的两份生成态文件（金额脚本与金额 SqlMap）；本票的脚本与静态映射面可立即开始，SqlMap 映射对写语句的核对在票 02、03 交付后完成。建表由负责人按已确认的目标库与 schema 执行。**闸门：负责人确认建表完成前，依赖新表的集成验证不执行。**

**Blocked by:** 02（金额保存写入与事件）、03（金额查询读模型）——仅 V24 的完整核对；脚本与 DDL 静态面可立即开始

**Status:** ready-for-agent

**外部前置：** 负责人确认目标库与 schema 并执行建表；写语句映射核对依赖票 02、03 交付

- [ ] 脚本的列序、类型、可空性、注释与唯一索引与设计逐字一致；无 `varchar(n)`、无数据库默认值、无跨系统外键（V23 静态面）
- [ ] 表名、表注释、唯一索引名与四个业务键列名与 `Server/design.md` 的 DDL 章节逐字一致
- [ ] SqlMap 只访问 `mrec_` 前缀表；scope 与每个调用点的语句 Id 一致；不调用 `SetContext`；查询语句无方言特征（V24 静态面）
- [ ] 原无业务调用点的无 where 查询语句已移除或被业务键定位语句取代
- [ ] 负责人确认建表完成，真实库的表结构、注释、索引与脚本一致（V23、V24 真实库面）
- [ ] 建表完成并确认后，才继续依赖新表的集成测试；未建成前相关用例记 `NotRun`，实际执行时若仍未建成才记 `Blocked`
- [ ] 脚本与映射的改动不波及既有三张表与其他系统对象
- [ ] `git diff --check` 干净；证据记入阶段测试报告

**口径：** 本票覆盖层级为 DDL 静态面与 SqlMap 映射面；本票只覆盖所引用矩阵用例在该层级的证据，真实数据库面由验收票承接；矩阵编号可跨批次引用，按完整用例只统计一次，只完成部分层级不记为通过。
