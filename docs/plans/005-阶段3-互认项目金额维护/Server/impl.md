# 阶段 3 后端实施

设计见 [design.md](design.md)。本文件只登记顺序与前置。

## 批次与依赖

| 批次 | 范围 | 主责与文件所有权 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 1 | 外部服务接入与组织路径解析：新增 `Dy.Base.Application.Contracts` 包引用与中央版本条目、`OrganizationPathResolver`、`TrustedOrganizationResolver` 的可信医院解析 | 后端主责：`server/Directory.Packages.props`、`Dy.MedicalRecognition.Application/` | 设计确认；实测 restore/build 通过 | V8-V10、V13-V14、V19 | 未开始 |
| 2 | 金额保存：契约、命令、事件、Manager、仓储与写入 SqlMap | 后端主责：`Application.Contracts/Aggregate`、`Application/`、`Domain/`、`Repository/Aggregate` | 批次 1；先建立失败证据 | V1-V7、V11-V12、V15-V16、V25-V26、V28 | 未开始 |
| 3 | 金额查询：契约、内部投影、查询仓储与查询 SQL | 后端主责：`Application.Contracts/Queries`、`Domain/Queries`、`Repository/Queries` | 批次 1；可与批次 2 并行设计 | V17-V22、V26、V29（应用层证据；真实宿主面按 S3-D20 记 `Blocked`）、V30 | 未开始 |
| 4 | 最终 DDL 与 SqlMap 映射核对 | 后端主责：`Repository/Scripts/`、`Repository/` | 设计确认；独立脚本可先交付；建表由负责人执行 | V23-V24 | 未开始 |
| 5 | 真实数据库与宿主验收 | 测试主责：阶段 `testReport.md` | 批次 4 完成建表、批次 1 组织服务实测可用、宿主身份与菜单就绪 | V1-V3、V7、V11、V15-V17、V20-V21、V23-V24、V27 | 未开始 |

矩阵编号可跨静态与真实验证批次引用，但按完整用例只统计一次；只完成部分层级不得把整条用例记为通过。

## 执行记录

- [ ] 阶段设计确认（含阶段根 `design.md` 的高风险决策台账）。
- [ ] 批次 1：新增包引用并实测 restore/build，确认「`1.0.0.147` + `Dy.Core.Abstractions` `1.1.0.54`」组合在本机可构建。
- [ ] 建立各批次的目标失败证据或等价基线，完成适用影响分析。
- [ ] 完成各批次实现与直接影响验证。
- [ ] 完成最终 DDL 交付并交负责人执行建表。
- [ ] 完成真实数据库与宿主验收，更新阶段根 `testReport.md`。

## 待办与交接

- 批次 1 必须先完成 `Dy.Base.Application.Contracts` 的包引用与中央版本条目，并实测 restore/build；该包与 `Dy.Core.Abstractions` `1.1.0.54` 的组合本机没有构建先例，只有元数据级 API 存在性核对。
- `server/Directory.Packages.props` 在 `project-context.md` 的再生成保护清单内；新增条目须按其要求核实维护方式，不降低保护级别。
- 环境**无第二个组织**：V29 的跨组织真实宿主面不可构造，降级为应用层证据 + `AcceptedRisk`、宿主面记 `Blocked`。
- 目标表 `mrec_organization_hospital_branch_recognition_amount` 尚未建立；建表由负责人按确认的目标数据库与 schema 执行。**状态口径与阶段 `testReport.md` 一致**：表未建成前相关用例记 `NotRun`；实际执行验证时若表仍未建成，受影响用例的数据库面才记 `Blocked`。
- 阶段根决策台账中的全部决策均已确认或按 `AcceptedRisk` 登记，无待裁定项。
