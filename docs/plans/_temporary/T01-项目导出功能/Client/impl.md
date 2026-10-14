# T01 前端实施

依据 [前端设计](design.md) 和 [测试矩阵](testPlan.md) 安排实施。

| 批次 | 页面或能力 | 主责与文件范围 | 接口及前置依赖 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 2 | API Client 累计重生成与核对：三个导出端点纳入、OpenAPI 快照与 `kiota-lock.json` 同步更新并核对来源与排除规则、已交付接口保留、`/auth/login` 排除不变 | 执行代理；`client/packages/api-client-medical-recognition` | 批次 1 稳定 OpenAPI | T01-C9 | NotRun |
| 3 | 三页导出按钮、适配层导出函数、共享下载模块迁移归并（既有统计页下载模块迁至 `shared/` 并改两个统计适配层引用）与索引登记 | 执行代理；`client/apps/dy-medical-recognition/src`（`pages/recognitionProjects`、`pages/recognitionAmounts`、`pages/recognitionStatistics` 引用迁移、`shared/`） | 批次 2 | T01-C1~C7、T01-C10 | NotRun |
| 4 | 宿主验收：三页从宿主登录与菜单进入完成导出成功与零记录拒绝（输入不命中关键字后导出）链路 | 执行代理；宿主环境 | 批次 3 | T01-C8 | NotRun |

接口、宿主或账号未就绪时记录受影响范围，继续独立工作；不使用本地页面或 mock 替代真实宿主验收。结果统一写入 [阶段测试报告](../testReport.md)。
