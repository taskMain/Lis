# T01 后端实施

依据 [后端设计](design.md) 中的验证矩阵安排批次，不另建或复制后端测试矩阵。

| 批次 | 目标 | 主责与文件范围 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 1 | 三个导出 Request、查询契约 `.Export.cs` 分部、应用层导出实现（范围校验复用、取数复用、防御校验、ClosedXML 组装）、导出控制器、静态守卫同批登记 | 执行代理；`server/Dy.MedicalRecognition.Application.Contracts`、`server/Dy.MedicalRecognition.Application`、`server/Dy.MedicalRecognition`、`server/Dy.MedicalRecognition.Tests` | 批次 0 设计确认；既有修改 symbol 完成 GitNexus upstream impact；T01-V1~V10 的目标失败证据先行（契约断言与应用层行为测试先写、确认因能力缺失而失败） | T01-V1~V10 | NotRun |
| 2 | 导出文件响应与宿主链路取证（与阶段根批次 4、前端宿主验收同轮执行） | 执行代理；`server/Dy.MedicalRecognition`（Host） | 批次 1 完成；宿主环境就绪 | T01-V11 | NotRun |

本阶段零新增 SQL、零迁移，无负责人执行的数据库动作。结果统一记录在 [阶段测试报告](../testReport.md)。
