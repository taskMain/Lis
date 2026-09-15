# 阶段计划

本目录维护本项目的阶段范围与执行证据。业务事实以项目 SRS、UML 和 CONTEXT 为准，目录规则见 [Planning And Evidence](../../.agents/instructions/planning-and-evidence.md)。

## 阶段入口

下表只登记已落盘的阶段文档；未开始的阶段不预登记，也不把模板或描述当作当前证据。

| 阶段 | 范围 | 设计 | 实施 | 测试报告 |
|---|---|---|---|---|
| 001 总体计划 | 阶段划分、计划原则与前后端协作口径 | [design.md](001-总体计划/design.md) | [impl.md](001-总体计划/impl.md) | 不适用（非实现阶段） |
| 002 阶段 0 现状对齐 | 进入开发前的基线确认，不实现业务功能 | [design.md](002-阶段0-现状对齐/design.md) | [impl.md](002-阶段0-现状对齐/impl.md) | 不适用（纯规范阶段仅保留根级文档） |
| 003 阶段 1 标准项目目录维护（F01） | 标准医疗项目分类、分组、标准项目维护与当前有效目录查询；跨端 | [design.md](003-阶段1-标准项目目录维护/design.md)、[Server](003-阶段1-标准项目目录维护/Server/design.md)、[Client](003-阶段1-标准项目目录维护/Client/design.md) | [根](003-阶段1-标准项目目录维护/impl.md)、[Server](003-阶段1-标准项目目录维护/Server/impl.md)、[Client](003-阶段1-标准项目目录维护/Client/impl.md) | [testReport.md](003-阶段1-标准项目目录维护/testReport.md)（含原型与静态检查证据，正式业务测试 NotRun） |
| 004 阶段 2 标准项目互认配置（F02） | 按组织维护标准项目互认配置、可互认时间和启停状态；跨端 | [design.md](004-阶段2-标准项目互认配置/design.md)、[Server](004-阶段2-标准项目互认配置/Server/design.md)、[Client](004-阶段2-标准项目互认配置/Client/design.md) | [根](004-阶段2-标准项目互认配置/impl.md)、[Server](004-阶段2-标准项目互认配置/Server/impl.md)、[Client](004-阶段2-标准项目互认配置/Client/impl.md) | [testReport.md](004-阶段2-标准项目互认配置/testReport.md)（设计完成，验证未开始） |

阶段 1 当前处于**设计阶段**：A2 页面布局与交互已获负责人确认（DesignConfirmed），阶段整体仍待审查，正式业务实施未开始、业务测试为 NotRun；原型与静态检查结果单独记录，不替代正式验收。其前端矩阵见 [Client/testPlan.md](003-阶段1-标准项目目录维护/Client/testPlan.md)。

阶段清单与顺序见 [总体计划设计](001-总体计划/design.md)。后续阶段（阶段 1 标准项目目录维护起）在各自开始时按模板创建文档并在此登记。

## 阶段模板

从 [stage-template](./stage-template/) 按需复制：

```text
NNN-阶段名称/
  design.md
  impl.md
  testReport.md
  Server/              # 仅后端或跨端阶段
    design.md
    impl.md
  Client/              # 仅前端或跨端阶段
    design.md
    impl.md
    testPlan.md
```

模板包含章节、表格和填写要求，不是空文件，也不代表设计已确认或测试已执行。只复制涉及的端；纯规范阶段只保留根级文档。

后端测试前矩阵在 `Server/design.md`，前端矩阵在 `Client/testPlan.md`，实际执行结果统一写入阶段根 `testReport.md`。项目环境值引用环境规范，不在阶段重复维护。
