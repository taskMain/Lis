# LisCenter Agent Rules

本文件只负责项目级规则优先级、不可跳过的执行闸门和规范入口，不承载具体后端架构、页面实现、接口契约或阶段测试矩阵。跨阶段实现与测试规范位于 `.agents/instructions/`，业务与领域事实位于 SRS/UML，阶段范围、决策、状态和证据位于 `docs/plans/`。同一规则只保留一个权威来源。

## 1. 规则归属

| 内容 | 权威位置 | 说明 |
|---|---|---|
| 项目级执行闸门、优先级、规范入口 | `AGENTS.md` | 只写跨任务都必须遵守的治理规则 |
| 跨阶段后端、前端、测试、环境和 GitNexus 规范 | `.agents/instructions/` | 按主题维护，不在阶段文档重复全文 |
| 长期业务规则、公共契约和领域模型 | SRS、UML | 不记录类、目录、DI 或测试执行细节 |
| 当前阶段架构、接口、路由、DTO、分页、数据和停止条件 | 当前阶段 `design.md`、`impl.md`、迁移矩阵和测试矩阵 | 阶段特例不得直接提升为全局规范 |
| 实际执行结果、问题和残余风险 | 当前阶段根目录 `testReport.md` | 只记录真实证据，不由计划代替 |
| 复盘、参考实现和经验说明 | 当前计划内的参考文档 | 仅供理解，不得成为新的规则来源或覆盖上述文件 |

阶段特例只有在项目负责人确认其长期适用于多个阶段后，才能归入对应 `.agents/instructions/`；属于长期业务或公共契约的内容还必须同步 SRS/UML。不得因为一次实现采用了某种 Repository、AppService、Controller、分页或目录方案，就把它写成所有后续任务的默认架构。

## 2. 规则优先级

发生冲突时依次遵循：用户本轮明确要求、`AGENTS.md`、本轮适用的 `.agents/instructions/`、总体计划、当前阶段 `design.md`、当前阶段 `impl.md` / `Client/testPlan.md`。SRS/UML 提供业务和领域事实；历史阶段文档与复盘材料只作为证据或背景，不覆盖当前规则。

## 3. 不可跳过的闸门

1. 开始前运行 `git status --short --branch`，识别并保护现有修改；默认认为非本轮改动属于用户或前序任务，不得回退、覆盖或顺手清理。
2. 需求、业务规则、交互、公共契约或数据结构变化时，先确认阶段归属并更新对应设计文档，再写失败测试和实现；不得把变化无痕塞入其他已结束阶段。
3. 修改代码 symbol 前执行 GitNexus upstream impact。同一原子批次可集中分析；`HIGH/CRITICAL` 必须在编辑前说明影响和验证方案，只有命中停止条件才暂停。
4. 新功能、缺陷修复、重构和行为变化使用 TDD：先获得因目标行为缺失而失败的测试，再写最小实现。无法自动化时，先记录替代证据和残余风险。
5. 数据库迁移需要项目负责人执行时，完成后端、最终建表脚本和迁移文件后必须暂停；收到迁移完成确认后才能继续集成测试。
6. 未取得实际证据，不得写 `Passed`、`Closed`、`Ready`、`Complete` 或“验收通过”。数据不足或规定验证未执行时如实记录 `NotRun`、`Blocked` 或 `PendingRetest`。
7. 完成前执行适用测试、构建、静态检查、`git diff --check` 和 GitNexus `detect_changes`；页面能力还必须完成规定的宿主浏览器验收。

## 4. 标准工作流

1. 读取本文件，并按任务类型只加载本轮适用的 instruction。
2. 阶段工作读取总体计划、当前阶段设计与实施文档、测试矩阵和已有测试报告。
3. 建立范围、阶段归属、受影响文件、公共契约和验证面；检查工作区与 GitNexus 索引。
4. 文档先行；代码任务继续执行 impact、RED、最小实现、目标测试和影响范围测试。
5. 每轮真实执行后先更新当前阶段 `testReport.md`，再同步阶段 `impl.md` 和总体计划进度。
6. 检查差异、生成物、迁移、未完成项和本轮自启进程，最后报告完成证据与残余风险。

## 5. 计划与证据

- 计划目录使用 `docs/plans/NNN-名称/` ，使用中文编写，需要有利于AI阅读，每个计划至少包含 `design.md` 和 `impl.md`。
- 具体实现阶段还必须包含 `testReport.md`、`Client/testPlan.md`、`Client/design.md`、`Client/impl.md`、`Server/design.md`、`Server/impl.md`。
- 复杂页面或组件按需拆到 `Client/Pages/<Name>/`、`Client/Components/<Name>/`；复杂服务端设计拆到 `Server/<Name>/`。
- `Client/testPlan.md` 是测试前矩阵，阶段根目录 `testReport.md` 是执行结果和问题台账，`impl.md` 不能替代测试报告。
- SRS、UML、总体计划、阶段设计、实现和测试证据必须保持一致；不得改写已经形成的历史证据。

## 6. 规范路由

| 任务 | 必读规范 |
|---|---|
| 所有测试或行为修改 | `.agents/instructions/testing-baseline.md` |
| C# 实现 | `.agents/instructions/csharp-backend-style.md` |
| C# 测试 | `.agents/instructions/csharp-backend-testing.md` |
| React / TypeScript | `.agents/instructions/frontend-style.md`、`.agents/instructions/frontend-application.md` |
| OpenAPI / Kiota / API 适配层 | `.agents/instructions/frontend-api-client.md` |
| 前端测试 | `.agents/instructions/frontend-testing.md` |
| 跨端联调、宿主验收、阶段报告 | `.agents/instructions/lis-center-testing.md` |
| 固定测试账号、登录返回上下文、端口、Profile 和本地 token | `.agents/instructions/lis-center-test-environment.md` |
| 任何代码 symbol 修改 | `.agents/instructions/gitnexus-workflow.md` |

只读取与本轮范围相关的详细规范；不要因为修改单一文档而加载全部实现规范。具体架构方案以相关 instruction 的通用约束和当前阶段设计共同决定，参考代码或复盘文档不能单独授权实现。

## 7. Git、数据库与进程安全

- 不自行创建分支、commit 或 push。用户明确要求提交时，提交信息使用 `类型: 中文描述`，例如 `fix: 修复报告人员来源`。
- 不修改无关代码，不执行破坏性 Git 命令，不删除来源不明的文件或进程。
- `Scripts` 下建表脚本表达最终表结构，不包含 `ALTER TABLE`；增量迁移只放 `Scripts/Migrations`，不得自行执行迁移。
- 只关闭本轮自行启动且已核实 PID、命令行、工作目录和端口的进程；不得关闭 PushCenter、公网穿透或无关宿主服务。
- App Goal 只有在项目负责人明确授权，或当前阶段 `impl.md` 明确要求且实时启动条件满足时才允许创建。设计、文档调整、普通测试和“目标测试”均不构成创建授权。

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **Dy.LisCenter** (17011 symbols, 31027 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/Dy.LisCenter/context` | Codebase overview, check index freshness |
| `gitnexus://repo/Dy.LisCenter/clusters` | All functional areas |
| `gitnexus://repo/Dy.LisCenter/processes` | All execution flows |
| `gitnexus://repo/Dy.LisCenter/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
