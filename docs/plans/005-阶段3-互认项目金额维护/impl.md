# 阶段 3：互认项目金额维护实施

设计见 [design.md](design.md)，各端细化见 [Server/impl.md](Server/impl.md) 与 [Client/impl.md](Client/impl.md)，结果见 [testReport.md](testReport.md)。

## 批次与依赖

| 批次 | 范围 | 主责 | 文件所有权 | 前置条件 | 矩阵编号 | 当前状态 |
|---|---|---|---|---|---|---|
| 1 | 后端外部服务接入与组织路径解析 | 后端主责 | `server/Directory.Packages.props`、`Dy.MedicalRecognition.Application/` | 设计确认；实测 restore/build | V8-V10、V13-V14、V19 | 未开始 |
| 2 | 后端金额保存与查询 | 后端主责 | `Application.Contracts`、`Application`、`Domain`、`Repository` | 批次 1 | V1-V22、V25-V26、V28-V30 | 未开始 |
| 3 | 最终 DDL 与建表 | 后端主责交付脚本，负责人执行建表 | `Repository/Scripts/` | 设计确认；脚本可先交付 | V23-V24 | 未开始 |
| 4 | 前端依赖与运行时配置、API Client 累计生成 | 前端主责 | `client/packages/api-client-medical-recognition/`、`client/apps/dy-medical-recognition/src/runtimeConfig.ts`、`client/apps/dy-medical-recognition/public/config.development.json`、`client/pnpm-workspace.yaml` | 设计确认；依赖安装可先行，生成部分需后端契约稳定 | C24、C25 | 未开始 |
| 5 | 两个页面、路由与适配层 | 前端主责 | `client/apps/dy-medical-recognition/` | 批次 1、4 | C1-C23 | 未开始 |
| 6 | 宿主验收与阶段报告 | 测试主责 | 阶段 `testReport.md` | 批次 3、5；菜单与角色授权 | V1-V3、V7、V11、V15-V17、V20-V21、V23-V24、V27；C1-C26 | 未开始 |

本表是**跨端顺序批次**：各端 [Server/impl.md](Server/impl.md)、[Client/impl.md](Client/impl.md) 的批次编号是本端顺序，与本表编号不一一对应（例如本表批次 2「后端金额保存与查询」覆盖 Server 批次 2 与批次 3）。矩阵编号可跨批次引用，按完整用例只统计一次。

后端先行：批次 1-3 完成并通过测试后，才进入批次 4 的契约生成与批次 5 的页面实现。

## 执行记录

- [ ] 完成适用设计及确认（含阶段根高风险决策台账）。
- [ ] 建立目标失败证据或等价基线，完成适用影响分析。
- [ ] 完成当前批次实现与直接影响验证。
- [ ] 完成最终 DDL 交付、负责人建表、真实数据库与宿主验收。
- [ ] 完成适用最终检查并更新阶段根 `testReport.md`。

## 待办与交接

- 金额保存与查询以互认配置数据为前提。
- 阶段 3 表 `mrec_organization_hospital_branch_recognition_amount` 未建立；建表由负责人确认目标数据库与 schema 后执行。
- 后端 `Dy.Base.Application.Contracts` 包引用未新增，组织服务目前只有配置、没有调用代码；批次 1 必须先完成引用并实测构建。
- 两个菜单子项与角色授权由负责人在权限系统配置；本阶段只提供菜单名称与路由。
- 生产配置与生产地址不在本阶段处理。
