# 阶段计划

本文是**如何编写阶段文档**的说明，不是阶段清单，也不登记任何阶段的完成情况、测试状态或当前进度。

- 阶段清单、划分顺序与范围以 [总体计划设计](001-总体计划/design.md) 为准。
- 每个阶段的实际状态与证据写在**该阶段自己的文档**里：设计见 `design.md`，实施批次见 `impl.md`，执行结果与状态见 `testReport.md`。
- 本文与 `stage-template/` 只描述文档结构、章节与填写要求；模板不代表设计已确认或测试已执行。
- 业务事实以项目 SRS、UML 和 CONTEXT 为准，目录与证据规则见 [Planning And Evidence](../../.agents/instructions/planning-and-evidence.md)。

任何"哪个阶段已完成/某阶段测试为 NotRun/某阶段待收口"之类的状态记录都不属于本文，避免与总体计划及各阶段文档职责重叠。

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
