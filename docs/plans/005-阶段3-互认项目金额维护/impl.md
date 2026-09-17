# 阶段 3：互认项目金额维护实施

**阶段 3 已完成并于 2026-09-17 收口（负责人裁定，阶段状态 `Complete`）**：设计与实施均已完成，真实宿主验收已执行并记录在 [testReport.md](testReport.md)（`Passed` 48 / `Failed` 0 / `Blocked` 4 / `NotRun` 4，合计 56；8 条不可构造项已按环境边界逐条登记，矩阵内无可继续取证的条目）；S3-D21 重开轮（读模型枚举中文）与前端枚举事实收编已完成。交付物尚未提交。

设计见 [design.md](design.md)，各端细化见 [Server/impl.md](Server/impl.md) 与 [Client/impl.md](Client/impl.md)，结果见 [testReport.md](testReport.md)；外部服务消费口径见 [NuGet 契约接入准则](../001-总体计划/dy-nuget-contract-guideline.md)。

## 批次与依赖

| 批次 | 范围 | 主责 | 文件所有权 | 前置条件 | 矩阵编号 | 当前状态 |
|---|---|---|---|---|---|---|
| 1 | 后端外部服务接入与组织路径解析 | 后端主责 | `server/Directory.Packages.props`、`Dy.MedicalRecognition.Application/` | 设计确认；实测 restore/build | V8-V10、V13-V14、V19 | 已完成：`Dy.Base.Application.Contracts` 引用与中央版本条目按 S3-D15 添加；组织、用户档案的解析与三值校验（存在、启用、父子归属）已实现，冲突与拒绝均返回业务文案；证据见 [testReport.md](testReport.md) 对应行与 `Stage3OrganizationPathTests` |
| 2 | 后端金额保存与查询 | 后端主责 | `Application.Contracts`、`Application`、`Domain`、`Repository` | 批次 1 | V1-V22、V25-V26、V28-V30 | 已完成：保存按业务键新建/覆盖、零元与同值重复按成功处理；查询由互认配置驱动、金额表按业务键 LEFT JOIN；契约字段、可空性、拒绝文案与原因派生均有断言；证据见 [testReport.md](testReport.md) 结果表与「本轮修复与回归事实」节 |
| 3 | 最终 DDL 与建表 | 后端主责交付脚本，负责人执行建表 | `Repository/Scripts/` | 设计确认；脚本可先交付 | V23-V24 | 已完成：`mrec_organization_hospital_branch_recognition_amount` 脚本已交付并由负责人执行建表；本轮只读回读该表结构与数据（当前 11 行），未在库上做任何写入 |
| 4 | 前端依赖与运行时配置、API Client 累计生成 | 前端主责 | `client/packages/api-client-medical-recognition/`、`client/apps/dy-medical-recognition/src/runtimeConfig.ts`、`client/apps/dy-medical-recognition/public/config.development.json`、`client/pnpm-workspace.yaml` | 设计确认；依赖安装可先行，生成部分需后端契约稳定 | C24、C25 | 已完成：`@dy/components-base`、`@dy/api-client-base` 与 kiota override 按 S3-D10 引入；`runtimeConfig.ts` 按 S3-D16 交付并在入口配置一次；API Client 累计生成（27 条 path、`kiota-lock.json` 已更新） |
| 5 | 两个页面、路由与适配层 | 前端主责 | `client/apps/dy-medical-recognition/` | 批次 1、4 | C1-C23 | 已完成：「互认项目金额维护」与「本院区互认项目金额」两个页面、路由与适配层已实现；枚举取值域、取值域判定与兜底文案已收编到跨页面共享模块（`src/shared/medicalItemType.ts`、`src/shared/configurationStatus.ts`）；证据见 [testReport.md](testReport.md) 结果表与重开轮节 |
| 6 | 宿主验收与阶段报告 | 测试主责 | 阶段 `testReport.md` | 批次 3、5；菜单与角色授权 | V1-V3、V7、V11、V15-V17、V20-V21、V23-V24、V27；C1-C26 | 已完成：两个身份经宿主登录页进入两个页面完成读写与只读回读核对；8 条不可构造项按环境边界登记；报告含「补充取证」与 S3-D21 重开轮记录 |

本表是**跨端顺序批次**：各端 [Server/impl.md](Server/impl.md)、[Client/impl.md](Client/impl.md) 的批次编号是本端顺序，与本表编号不一一对应（例如本表批次 2「后端金额保存与查询」覆盖 Server 批次 2 与批次 3）。矩阵编号可跨批次引用，按完整用例只统计一次。

后端先行：批次 1-3 已完成并通过测试，批次 4 的契约生成与批次 5 的页面实现均在其后进行，实际执行顺序与该约束一致。

## 执行记录

- [x] 完成适用设计及确认（含阶段根高风险决策台账）：台账内全部决策均为 `DesignConfirmed` 或按 `AcceptedRisk` 登记，无 `Pending` 项。
- [x] 建立目标失败证据或等价基线，完成适用影响分析：六项生产代码修复与 S3-D21 重开轮均先取得可复现的失败证据（断言失败或静态失败基线），再由实现转正。
- [x] 完成当前批次实现与直接影响验证：批次 1-5 的实现与直接影响面均有测试或真实链路证据（见批次表与 [testReport.md](testReport.md)）。
- [x] 完成最终 DDL 交付、负责人建表、真实数据库与宿主验收：三处启动配置与前端地址同端口；两个身份、两个页面的真实读写与只读回读一致；开发数据保留不清理。
- [x] 完成适用最终检查并更新阶段根 `testReport.md`：后端与前端全量测试、构建、静态检查与文档检查均已执行；报告含结果表、证据复用条件、未覆盖面与残余风险、补充取证与重开轮记录。

## 待办与交接

- **交付物尚未提交**：本次全部改动未提交，提交需负责人授权；提交时需自行取舍未跟踪文件（生成物、开发原型与其他非本阶段产物）。
- `public/config.json` 的 `apiBaseUrl` 是部署占位地址（`http://183.224.180.166:35014`，尚未部署），正式部署前需按实际环境替换；生产配置按 S3-D13 不在本阶段处理。
- 契约面遗留：`currentAmount` 在生成客户端里是未定型节点（`UntypedNode`），适配层按结构取值与写值；该属性的契约类型修复属生成管线范围。
- 矩阵内无可继续取证的条目：V16、V29、V30、C3、C11、C15、C22、C23 共 8 条按环境边界逐条登记（见 [testReport.md](testReport.md) 的「已确认的验证降级」节），其中 V29 的跨组织真实宿主面按 S3-D20 接受。
- 不分页按 S3-D8 记 `AcceptedRisk`，重审条件为出现可复现的卡顿、超时或内存问题；`BaseScopeSelector` 的既有行为与受控注入还原链脆弱的处置见 [testReport.md](testReport.md) 的「未覆盖面与残余风险」节。
- 开发库现有测试数据（金额表 11 行、组织 `01` 的 3 条互认配置）保留不清理；需要恢复空态时由负责人按正常业务路径调整配置。
- 生产菜单与角色授权由负责人在权限系统配置（S3-D2）；本阶段只交付菜单名称与路由。
