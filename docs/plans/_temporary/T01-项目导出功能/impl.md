# T01 阶段实施

设计见 [design.md](design.md)，结果见 [testReport.md](testReport.md)；后端批次见 [Server/impl.md](Server/impl.md)，前端批次见 [Client/impl.md](Client/impl.md)。

## 批次与依赖

| 批次 | 范围 | 主责 | 文件所有权 | 前置条件 | 矩阵编号 | 当前状态 |
|---|---|---|---|---|---|---|
| 0 | 阶段文档创建、审查整改与设计确认（本批次）：创建本目录 8 份文档，经多路审查与整改后提交确认 | 执行代理 | 本目录全部文档 | 阶段 2/3 已收口；阶段 6 导出先例已交付 | 无 | 整改完成，待确认 |
| 1 | 后端导出：三个导出 Request、查询契约 `.Export.cs` 分部、应用层导出实现、导出控制器、静态守卫同批 | 执行代理 | `server/` 契约、应用、宿主与测试工程 | 批次 0 确认；既有修改 symbol 完成 GitNexus upstream impact；目标 RED 先行 | T01-V1~V10 | NotRun |
| 2 | API Client 累计重生成与核对：三个导出端点纳入、OpenAPI 快照与 `kiota-lock.json` 同步更新并核对来源与排除规则 | 执行代理 | `client/packages/api-client-medical-recognition` | 批次 1 稳定 OpenAPI | T01-C9 | NotRun |
| 3 | 前端三页导出按钮、适配层导出函数、共享下载模块迁移归并（T01-D9：既有统计页下载模块迁至 `shared/` 并改两个统计适配层引用）与索引登记 | 执行代理 | `client/apps/dy-medical-recognition/src`（三页、`shared/` 与 `pages/recognitionStatistics` 引用迁移） | 批次 2 | T01-C1~C7、T01-C10 | NotRun |
| 4 | 测试执行与报告回填：后端矩阵、前端矩阵与宿主验收 | 执行代理 | [testReport.md](testReport.md) | 批次 1~3 完成 | T01-V11、T01-C8 | NotRun |
| 5 | 同步正式阶段文档（见下方详单），同步前先汇报具体差异清单并取得确认 | 执行代理 | `docs/plans/001-总体计划/`、`docs/plans/004-*`、`docs/plans/005-*`、`docs/需求规约SRS.md`、`docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd` | 批次 4 完成 | 无（文档节点） | NotRun |
| 6 | 收口检查与删除申请：正式文档与总体计划零本目录引用核对、矩阵终值核对；删除由项目负责人确认执行 | 执行代理 | 本目录 | 批次 5 完成 | 无 | NotRun |

## 批次 5 同步详单

三个页面与三个端点同批交付、共用同一后端批次，上游同步在批次 5 一次完成。

- `docs/plans/004-阶段2-标准项目互认配置/design.md`：范围「纳入」补导出能力及其权限授权边界；新增导出设计决策行（编号 S2-D30 起顺延追加，除 S2-D21 的页面设计表述与依据条款按最终状态修订外不改动其他既有决策行；S2-D21 依据中「页面要求 4」的条款位移一并修正为「通用界面要求 4」），页签行承载导出按钮与导出门控的版面口径在该行内修订。
- `docs/plans/004-阶段2-标准项目互认配置/Server/design.md`：追加导出契约、调用链与验证矩阵行（编号 V36 起顺延追加）；静态守卫清单按冻结 OpenAPI 路径集合/计数断言、手写控制器冻结集合断言、方法集合与分部清单、分页禁用字段清单逐项同步。
- `docs/plans/004-阶段2-标准项目互认配置/Client/design.md` 补页签行承载导出按钮的版面与导出门控口径，`Client/testPlan.md` 编号 C38 起顺延追加；`Server/impl.md`、`Client/impl.md` 与根 `impl.md` 批次表补记。
- `docs/plans/005-阶段3-互认项目金额维护/design.md`：范围与权限边界同步；新增导出设计决策行（编号 S3-D30 起顺延追加，不改动既有行）；S3-D25 查询区与页签形态、S3-D29 共用列表视图组件职责（含导出按钮承载与门控）按最终状态修订。
- `docs/plans/005-阶段3-互认项目金额维护/Server/design.md`：追加导出契约与验证矩阵行（编号 V34 起顺延追加）；静态守卫清单同 004 口径。
- `docs/plans/005-阶段3-互认项目金额维护/Client/design.md` 补页签行承载导出按钮的版面与导出门控口径（经 S3-D29 共用列表视图组件承载），`Client/testPlan.md` 编号 C37 起顺延追加；`Server/impl.md`、`Client/impl.md` 与根 `impl.md` 批次表补记。
- `docs/plans/004-*`、`docs/plans/005-*` 的 `testReport.md`：仅追加导出用例证据行并按新增条目重算终值统计，不改写既有证据行。
- `docs/plans/001-总体计划/design.md`：§4.4、§4.5 阶段 2/3 范围行补导出能力表述（范围表述的承载位置）。
- `docs/plans/001-总体计划/impl.md`：004/005 对应行仅追加本轮轮次记录，不改写既有文字。
- `docs/需求规约SRS.md`：F02、F10 补导出能力表述及其权限授权边界（通用界面要求 1 的导出口径由该同步承接）。
- `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd`：补三个导出请求与导出文件读模型形态；一并修正互认项目查询入参未随关键字口径同步的既有漂移。
- 前端共享下载模块的索引表登记随批次 3 同批完成（执行时单独汇报），批次 5 核对登记已完成并回填索引行「设计出处」的导出能力出处（004/005）。
- 全部同步内容不引用 `docs/plans/_temporary/` 的路径、编号或文件名。

## 执行记录

- [ ] 完成适用设计及确认。
- [ ] 批次开工前执行 `git status --short --branch` 并登记工作区归属；不创建分支、commit、push 或额外 worktree。
- [ ] 建立目标失败证据或等价基线，完成适用影响分析。
- [ ] 完成当前批次实现与直接影响验证；测试数据按正常业务路径准备，不直接写库。
- [ ] 完成适用最终检查并更新根测试报告。

## 待办与交接

- 设计确认是批次 1~6 的共同前置；T01-D9 为下载模块迁移归并，随批次 3 执行。
- 批次 5 动正式文档前单独汇报拟改文件与差异要点；同步完成后才能进入批次 6。
- 环境值、账号与宿主入口按测试环境规范执行，不在本目录复制。
