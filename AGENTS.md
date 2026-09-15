# MedicalRecognition Agent Rules

本文件只保留规则优先级、执行闸门、最小读取和规范导航；细节按任务加载 `.agents/instructions/`。项目技术事实见 [Project Context](.agents/instructions/project-context.md)，环境值见 [Test Environment](.agents/instructions/test-environment.md)。技能不在本次迁移范围内，不依赖技能或其脚本才能执行这些规则。

## 1. 规则归属与优先级

| 内容 | 权威位置 |
|---|---|
| 执行闸门、优先级、规范入口 | `AGENTS.md` |
| 跨阶段实现与测试规范 | `.agents/instructions/` |
| 长期业务规则、领域事实和公共业务契约 | 当前 SRS、UML 和 `CONTEXT.md`；入口见 Project Context |
| 项目技术栈、目录映射与启动命令 | `.agents/instructions/project-context.md` |
| 端口、宿主、子系统编码和测试身份配置状态 | `.agents/instructions/test-environment.md` |
| 阶段范围、设计和验证矩阵 | 当前阶段 `design.md`、`impl.md` 和适用测试矩阵 |
| 阶段实际结果、问题和残余风险 | 当前阶段根 `testReport.md` |

冲突依次遵循：用户本轮明确要求、`AGENTS.md`、本轮适用规范、总体计划、阶段设计、实施与测试矩阵。业务事实以已确认决策及本项目 SRS/UML/CONTEXT 为准；迁入的通用规范、角色提示、技能和示例不能暗中覆盖业务契约。发现未确认冲突时，只暂停受影响范围并请求负责人决策。

复盘和参考资料不能自动成为新规则。阶段特例只有在负责人确认长期适用后才能进入通用规范；涉及业务事实或公共契约时同步权威文档。

## 2. 不可跳过的闸门

1. 开始前执行 `git status --short --branch`，保护已有修改；不回退、覆盖或清理非本轮内容。
2. 需求、业务、交互、公共契约、架构、数据结构或阶段范围变化时，先更新并确认对应设计，再实现验证；普通等价修复无需制造文档改动。
3. 修改既有代码 symbol 前执行 GitNexus upstream impact；新增 symbol 和尚无代码的初始化任务按 [GitNexus Workflow](.agents/instructions/gitnexus-workflow.md) 处理。`HIGH/CRITICAL` 先说明影响与验证方案，只有命中停止条件才暂停。
4. 新功能、修复和行为变化先获得准确失败测试；等价重构先建立通过的特征/契约基线。静态 finding 使用静态证据，不伪造行为 RED；无法自动化时记录替代证据及残余风险。
5. 迁移由负责人执行时，完成后端、最终建表脚本和迁移文件后暂停相关集成测试，收到迁移完成确认后继续。
6. 测试状态只使用 [Testing Baseline](.agents/instructions/testing-baseline.md) 的定义；无实际证据不得写 `Passed`、`Closed`、`Ready`、`Complete` 或“验收通过”。
7. 完成时执行适用测试、构建、静态检查和 `git diff --check`；代码 symbol 变化或用户要求提交时执行 GitNexus `detect-changes`。页面能力必须完成真实宿主验收，不固定浏览器、Profile 或控制工具。
8. 不创建分支、commit、push 或额外 worktree，除非本轮明确授权；迁移、进程和文件安全见 [Repository Safety](.agents/instructions/repository-safety.md)。
9. 未配置的端口、子系统编码、账号及凭据不得猜测或沿用其他项目。仅暂停依赖该值的操作，不阻止独立设计、文档和不依赖环境的测试。

## 3. 最小读取

- 只加载本轮实际命中的规范和相关章节，不预读相邻主题，不无故重复读取。
- 局部缺陷和等价重构不默认通读总体计划；阶段任务只读相关设计、实施、矩阵与证据。
- 历史报告和参考资料仅在需要背景或历史证据时读取。
- 普通文档任务不加载后端、前端及环境实现规范；引用某文档不等于必须读取全文。

## 4. 工作流程

1. 明确任务、受影响文件、阶段、公共契约和验证面，按导航加载必要规则。
2. 保护工作区；代码任务核对索引和影响边界，文档任务不重建代码索引。
3. 需要设计确认时先完成设计；代码任务按测试基线实施。
4. 检查差异、生成物、迁移、未完成项和本轮进程，报告真实证据及残余风险。

## 5. 规范导航

| 任务 | 必读规范 |
|---|---|
| 项目事实、目录、技术栈或命令 | [Project Context](.agents/instructions/project-context.md) |
| 测试或行为变化 | [Testing Baseline](.agents/instructions/testing-baseline.md) |
| 后端新功能设计 | [Backend Design Gates](.agents/instructions/backend-design-gates.md) |
| 聚合、分层、类型、Repository、SQL/XML | [Backend Architecture](.agents/instructions/backend-architecture.md) |
| Command、Query、事件、事务 | [Backend Command Query Event](.agents/instructions/backend-command-query-event.md) |
| Dy Framework 工作单元与事件顺序 | [Dy Framework WorkUnit](.agents/instructions/dy-framework-workunit.md) |
| C# 实现 | [C# Backend Style](.agents/instructions/csharp-backend-style.md) |
| C# 注释、复用、新增抽象 | [C# Documentation And Reuse](.agents/instructions/csharp-documentation-and-reuse.md) |
| C# 测试 | [C# Backend Testing](.agents/instructions/csharp-backend-testing.md) |
| React / TypeScript | [Frontend Style](.agents/instructions/frontend-style.md)、[Frontend Application](.agents/instructions/frontend-application.md) |
| OpenAPI、Kiota、API 适配 | [Frontend API Client](.agents/instructions/frontend-api-client.md) |
| 前端测试 | [Frontend Testing](.agents/instructions/frontend-testing.md) |
| 跨端联调、宿主验收、阶段报告 | [Testing](.agents/instructions/testing.md) |
| 地址、端口、子系统编码、账号、登录、浏览器和 token | [Test Environment](.agents/instructions/test-environment.md) |
| 代码 symbol 修改 | [GitNexus Workflow](.agents/instructions/gitnexus-workflow.md) |
| 计划、阶段文档、测试报告 | [Planning And Evidence](.agents/instructions/planning-and-evidence.md) |
| Git、迁移、进程、App Goal、子代理或技能配置 | [Repository Safety](.agents/instructions/repository-safety.md) |
| 子代理任务拆分、所有权、交接或独立验收 | [协作规范](.codex/agents/COLLABORATION.md) |

普通单智能体任务不加载协作规范。GitNexus 仓库选择与串行要求只按对应规范执行，不使用其他项目的索引。
