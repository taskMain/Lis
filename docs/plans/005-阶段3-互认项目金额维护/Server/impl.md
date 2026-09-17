# 阶段 3 后端实施

**本阶段已收口（阶段状态 `Complete`，2026-09-17 负责人裁定）**，详见阶段根 [impl.md](../impl.md) 与 [testReport.md](../testReport.md)。本文件只登记顺序与前置。

设计见 [design.md](design.md)。

## 批次与依赖

| 批次 | 范围 | 主责与文件所有权 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 1 | 外部服务接入与组织路径解析：新增 `Dy.Base.Application.Contracts` 包引用与中央版本条目、`OrganizationPathResolver`、`TrustedScopeResolver`（阶段 3 四个金额入口共用的可信组织与医院解析：令牌优先、缺失层由当前登录用户档案补齐、冲突即拒绝，院区不参与） | 后端主责：`server/Directory.Packages.props`、`Dy.MedicalRecognition.Application/` | 设计确认；实测 restore/build 通过 | V8-V10、V13-V14、V19 | 已完成：包引用与中央版本条目已按 S3-D15 添加并实测 restore/build；解析与三值校验（存在、启用、父子归属）已实现，拒绝文案由应用层断言覆盖；外部服务消费口径见 [NuGet 契约接入准则](../../001-总体计划/dy-nuget-contract-guideline.md) |
| 2 | 金额保存：契约、命令、事件、Manager、仓储与写入 SqlMap | 后端主责：`Application.Contracts/Aggregate`、`Application/`、`Domain/`、`Repository/Aggregate` | 批次 1；先建立失败证据 | V1-V7、V11-V12、V15-V16、V25-V26、V28 | 已完成：同一 Command 由两个入口调用、共用事件；按业务键新建/覆盖、零元与同值重复按成功处理；金额精度与负值拒绝、缺省金额必填校验均有断言 |
| 3 | 金额查询：契约、内部投影、查询仓储与查询 SQL | 后端主责：`Application.Contracts/Queries`、`Domain/Queries`、`Repository/Queries` | 批次 1；可与批次 2 并行设计 | V17-V22、V26、V29（应用层证据；真实宿主面按 S3-D20 记 `Blocked`）、V30 | 已完成：查询由互认配置驱动、金额表按业务键 LEFT JOIN；只读模型按 S3-D21 交付 `ItemTypeText`/`ConfigurationStatusText`；V29 的跨组织真实宿主面按 S3-D20 接受 |
| 4 | 最终 DDL 与 SqlMap 映射核对 | 后端主责：`Repository/Scripts/`、`Repository/` | 设计确认；独立脚本可先交付；建表由负责人执行 | V23-V24 | 已完成：脚本已交付并由负责人执行建表；SqlMap 与实体映射经静态核对与真实读写验证（金额表当前 11 行） |
| 5 | 真实数据库与宿主验收 | 测试主责：阶段 `testReport.md` | 批次 4 完成建表、批次 1 组织服务实测可用、宿主身份与菜单就绪 | V1-V3、V7、V11、V15-V17、V20-V21、V23-V24、V27 | 已完成：两身份、两院区的真实写入与只读回读一致；8 条不可构造项按环境边界登记 |

矩阵编号可跨静态与真实验证批次引用，但按完整用例只统计一次；只完成部分层级不得把整条用例记为通过。

## 执行记录

- [x] 阶段设计确认（含阶段根 `design.md` 的高风险决策台账）：台账内决策均为 `DesignConfirmed` 或按 `AcceptedRisk` 登记，无待裁定项。
- [x] 批次 1：新增包引用并实测 restore/build，确认「`1.0.0.147` + `Dy.Core.Abstractions` `1.1.0.54`」组合在本机可构建。
- [x] 建立各批次的目标失败证据或等价基线，完成适用影响分析：写路径与查询面均先取得断言级失败证据（含金额可空化修复与 S3-D21 重开轮）。
- [x] 完成各批次实现与直接影响验证。
- [x] 完成最终 DDL 交付并交负责人执行建表。
- [x] 完成真实数据库与宿主验收，更新阶段根 `testReport.md`。

## 待办与交接

- **交付物尚未提交**：提交需负责人授权。
- `server/Directory.Packages.props` 在 `project-context.md` 的再生成保护清单内；后续阶段新增条目仍须按其要求核实维护方式，不降低保护级别。
- 环境**无第二个组织**：V29 的跨组织真实宿主面不可构造，降级为应用层证据 + `AcceptedRisk`、宿主面记 `Blocked`（S3-D20）。
- 目标表 `mrec_organization_hospital_branch_recognition_amount` 已由负责人建表并执行；建表脚本与实体、SqlMap 映射保持一致。开发数据保留不清理。
- 契约面遗留：`currentAmount` 在生成客户端为未定型节点，属生成管线范围（见阶段根 [testReport.md](../testReport.md)「未覆盖面与残余风险」）。
