# Backend Design Gates

本文是 AI 设计新后端功能时的执行入口。它只规定前置证据、设计交付物和停止条件，不重复架构细节。

开始前必须读取：

- [Backend Architecture](backend-architecture.md)
- 涉及写入、查询、事件或事务时读取 [Backend Command Query Event](backend-command-query-event.md)
- 目标项目使用 Dy Framework 且涉及事务或事件时读取 [Dy Framework WorkUnit](dy-framework-workunit.md)
- 目标项目的 `AGENTS.md`、SRS 和 UML

编码风格和测试规范在进入实现阶段时按目标项目路由加载，设计阶段不要求预读。

## 1. 前置证据

1. 检查 Git 工作区并保护非本任务修改。
2. 使用项目代码索引、调用图或搜索工具查找既有实现和执行流。
3. 如果项目使用 GitNexus，先执行 `query`、`context`；修改 symbol 前执行 upstream `impact`。
4. 明确功能属于现有聚合还是新聚合，并列出必须原子保持的不变量。
5. 涉及写入时，查明目标框架现有工作单元机制，禁止从其他项目复制事务实现。
6. 完成设计确认，并按 [Testing Baseline](testing-baseline.md) 建立准确 RED、等价重构基线或静态失败证据后才能实现。

## 2. 项目层映射

目标项目目录不同于规范示例时必须填写：

| 概念层 | 实际项目/目录 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | 待填写 | 待填写 | 待填写 |
| Application | 待填写 | 待填写 | 待填写 |
| Domain | 待填写 | 待填写 | 待填写 |
| Domain.Shared | 待填写 | 待填写 | 待填写 |
| Repository/Infrastructure | 待填写 | 待填写 | 待填写 |
| Host | 待填写 | 待填写 | 待填写 |

## 3. 功能和聚合判断

设计必须说明：

- 功能目标、入口和参与者。
- 所属聚合及业务理由。
- 必须原子保持的业务不变量。
- 涉及的其他聚合和外部系统。
- 公共契约、数据库和兼容性变化。

### 3.1 高风险决策台账

存在架构争议、框架限制、项目负责人裁决或尚待验证的关键假设时，当前功能的主设计文档必须维护高风险决策台账；项目使用阶段目录时，完整台账只存放在阶段根 `design.md`。

| ID | 决策 | 状态 | 依据 | 适用范围 | 是否仍需运行验证 |
|---|---|---|---|---|---|
| 待填写 | 待填写 | `DesignConfirmed / Rejected / AcceptedRisk / Pending` | 待填写 | 待填写 | 待填写 |

表中状态是决策状态或标签，不是测试结果：`DesignConfirmed` 表示设计已确认，`Rejected` 表示方案已否决，`AcceptedRisk` 表示已知风险由项目负责人接受，`Pending` 表示仍待决策。`Rejected` 和 `Pending` 均不得计入测试统计，`Pending` 涉及的范围不得进入实现。普通字段和无争议实现细节不进入台账；子设计、`impl.md` 和 `testReport.md` 只引用决策 ID，不复制整张台账。

## 4. 类型归属表

只列出本功能实际新增或修改的类型，不得使用“等”“相关类”省略。下表行是格式示例，不是要求功能必须具备这些角色；未涉及的 DTO、Service、Entity、Query、ReadModel、适配器或其他类型不得为了填表而新增。

| 类型名 | 角色 | 所在层/目录 | 调用方 | 主要职责 | 禁止职责 |
|---|---|---|---|---|---|
| `ExampleRequest` | Request | Contracts | API/UI | 调用方输入 | 可信用户字段、状态流转 |
| `ExampleAppService` | AppService | Application | Controller | 用例编排 | SQL、领域不变量 |
| `ExampleCommand` | Command | Domain | AppService | 领域意图 | HTTP、展示字段 |
| `ExampleManager` | DomainService | Domain | AppService | 状态流转 | Request、HttpClient、ORM |

表中角色必须与实际调用链一致；没有新增或修改的角色无需占位。

## 5. 调用链和数据来源

必须描述完整链路，并标明每次转换由谁完成：

```text
Request
-> AppService
-> 外部端口（可选）
-> Command
-> Manager
-> IRepository
-> Repository
-> DomainEvent
```

每个关键字段必须说明来自调用方、可信上下文、数据库还是外部系统。

## 6. 事务和验证

每个写用例必须在设计中占一行：

| 写用例 | Application Service 入口 | 主要 Repository 数据库语句/操作 | 是否开启事务 | 框架机制 | 原子性依据 | 并发兜底 | DomainEvent 口径 | 外部调用及顺序 | 失败语义 | 验证方式 |
|---|---|---|---|---|---|---|---|---|---|---|
| `ApproveOrder` | `ApproveOrderAsync` | 1 条条件 `UPDATE` | 否 | 无显式 WorkUnit 事务 | 单条条件更新并检查影响行数 | 状态条件和影响行数 | 更新成功后登记 `OrderApprovedEvent`；可靠投递另行判断 | 无 | 零行或异常不形成审核成功结果 | Repository 集成测试和事件测试 |

设计还必须说明：

- 目标框架、工作单元声明方式，以及已经核实的 WorkUnit 生命周期和事务/事件顺序。
- 未开启事务的写用例及其框架或持久化依据。
- 外部调用相对 WorkUnit 生命周期和第一次 Repository 访问的顺序。
- 并发、重复提交、唯一性、幂等和补偿。
- 数据库写入、事务提交和事件发布失败后的结果。
- Request、Manager、Repository、AppService、适配器、事件和公共契约测试。

事务声明能够通过反射、架构测试或静态分析识别时，必须增加机械检查。目标框架没有可静态识别的声明方式时，必须通过集成测试证明原子提交和失败回滚。禁止事务 API 应优先通过 Banned API、Analyzer、架构测试或 CI 检查落实，不能只依赖评审人员阅读代码。

涉及 Command 时，还必须提交 [Command-Event 评审表](backend-command-query-event.md#5-command-event-评审表)。

## 7. 停止条件

出现以下任一情况时必须停止实现并修正设计：

- 类型归属或项目依赖方向不清。
- Request、Command、ReadModel、Entity、Event 或 TransportDto 混用。
- AppService 直接写仓储或复制 Manager 规则。
- Domain 依赖 HTTP、Application Service 或数据库实现。
- Repository 做领域状态决策。
- Query 产生副作用。
- Command 没有主要 DomainEvent。
- FacadeCommand 调用 FacadeCommand。
- 未完成事务机制映射或写用例事务表。
- 目标框架已有工作单元机制，却自行实现事务、UnitOfWork 或事务包装器。
- Manager、Repository、Entity 或内部辅助方法控制事务生命周期。
- WorkUnit 生命周期内存在外部调用，却未说明调用顺序、框架或适配器已有的超时与重试责任、失败结果，或者在框架已有统一机制时仍计划自行包装超时或重试。
- 仅因文件或实体较多就拆聚合。
- 未取得实际测试证据却声明完成。

## 8. 实现前确认

- [ ] 第 2-6 节要求的交付物已全部完成（层映射、聚合与不变量、类型归属表、调用链、事务表、Command-Event 评审表）。
- [ ] 未命中第 7 节任何停止条件。
- [ ] 影响分析已完成，并已按 [Testing Baseline](testing-baseline.md) 准备准确 RED、等价重构基线或静态失败证据。
