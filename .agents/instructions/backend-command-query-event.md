# Backend Command Query Event

本文定义可跨项目复用的 Command、FacadeCommand、Query、DomainEvent、事务和事件发布原则。分层归属见 [Backend Architecture](backend-architecture.md)；MedicalRecognition 使用 Dy Framework 时的具体映射见 [Dy Framework WorkUnit](dy-framework-workunit.md)，不得把框架细节当作通用结论复制到其他项目。

## 1. Command 和 DomainEvent

本规范采用 **一个 Command 对应一个主要 DomainEvent** 的约定。所有表达副作用的 Command 和 FacadeCommand 都必须在类型级别找到对应事件。

```text
CreateOrderCommand  -> OrderCreatedEvent
UpdateOrderCommand  -> OrderUpdatedEvent
ApproveOrderCommand -> OrderApprovedEvent
RejectOrderCommand  -> OrderRejectedEvent
CancelOrderCommand  -> OrderCancelledEvent
```

强制规则：

1. 每个 Command/FacadeCommand 必须有唯一的主要 DomainEvent。
2. Command 使用祈使语态表达意图；Event 使用过去式表达已发生事实。
3. Manager 或聚合根在领域变化成功后登记事件。
4. 校验失败或状态不允许时不得登记成功事件；Repository 数据库写入失败、WorkUnit 事务提交失败和事件处理失败后的结果，必须按目标框架已核实的机制分别说明，不得把“登记事件”写成“事件已经发布”。
5. Command 可以因多个独立事实产生附加事件，但不能缺少主要事件。
6. 禁止不同命令共用含糊的 `ChangedEvent`，也禁止用技术事件代替业务事实。
7. FacadeCommand 编排的普通 Command 各自保留对应事件；FacadeCommand 还必须有整体完成事件。
8. FacadeCommand 禁止调用另一个 FacadeCommand。
9. 不依赖普通 Command 事件与 FacadeCommand 整体事件的实际处理顺序；事件处理器必须能在任意顺序下工作。
10. 普通 Command 事件与 FacadeCommand 整体事件必须表达不同业务事实；不得让同一业务副作用因同时订阅两类事件而重复执行。
11. 幂等重放没有产生新事实时不得重复发布事件。
12. 成功后永远不产生业务事实的“Command”必须重新判断是否其实是 Query 或应用层计算；例外需要明确批准。

## 2. Event 内容

主要 DomainEvent 至少应包含：

- 聚合 ID 和必要业务标识。
- 变化后的关键状态。
- 事件发生时间。
- 必要的操作人和组织标识。
- 订阅者继续处理所需的最小业务快照。

Event 禁止携带公共 Request、页面 DTO、TransportDto、数据库连接、异常对象或仅用于显示的临时文本。

事件可以由 Command 的 `CreateXxxEvent` 工厂或聚合根创建，但目标项目必须统一方式。无论采用哪种方式，事件都由 Domain 登记；事件登记、事件处理或对外发布、数据库事务提交是不同事实，必须按目标框架的实际能力分别设计和验证。

标准顺序：

```text
AppService 构造 Command
-> Manager 校验并执行变化
-> Repository 执行数据库操作
-> Domain 构造并登记 Event
-> 工作单元按照已核实的框架顺序处理事件队列和数据库事务
```

## 3. Query 和 ReadModel

只有纯查询使用 Query 和 ReadModel。执行后产生数据库写入、事件、提醒、审计、消息或外部副作用的入口不是 Query。

- 公开 Query Service、Query Request 和 ReadModel 位于 Contracts 的 Queries 区域。
- SQL、ORM/DataMapper、DataRequest 和内部投影属于 Query Repository。
- 简单单数据源投影可以在目标项目明确允许时由 Repository 直接实现公共 Query Service；这是例外。
- 需要组织范围、权限、名称补齐、平台服务、多数据源或复杂组装时，保留 QueryAppService。
- Command AppService 只保留写操作和完成写操作必需的读取，不扩大为通用查询服务。
- 查询必须定义范围、空值语义、稳定唯一排序和分页策略。
- 带副作用的查询入口必须改为 Command/FacadeCommand，内部返回使用 CommandDto，不使用 ReadModel 表达写流程。

## 4. 事务和并发

### 4.1 通用规则

- 是否开启显式事务依据业务原子边界、独立数据库语句数量和并发要求判断，不依据涉及表数量。
- 单条 `INSERT`、`UPDATE` 或 `DELETE` 通常依赖数据库语句级原子性，不因只操作一张表或登记一个 DomainEvent 就机械开启显式事务。
- 多条独立数据库语句即使只操作同一张表，只要业务要求全成全败，就必须开启事务。
- 一次修改多张表通常需要事务；如果实际只有一条原子 SQL，或业务明确采用最终一致性，可以例外，但必须说明原子性依据、失败结果和补偿方式。
- 批量逐条写入且要求整批全成全败时必须开启事务。
- 先读后写不自动要求事务。设计必须说明并发由数据库唯一约束、条件更新、版本检查、影响行数检查还是锁定快照兜底；要求同一锁定快照时，必须明确事务和实际可实现的锁 SQL。
- 数据库事务原子性与 DomainEvent 可靠投递分开判断。不得仅因存在 DomainEvent 就开启显式事务，也不得把显式事务当成提交后可靠发布保证。
- 一个写用例的事务边界定义在公开 Application Service 入口。例外必须说明框架限制和替代边界，并在实现前取得明确批准。
- 必须优先使用目标框架已经提供的工作单元或事务声明机制。AI 不得因为历史代码未显式声明事务，就推导新用例也不需要事务。
- Application Service 只声明事务边界，不手工开始、提交或回滚事务。Domain Service、Manager、Repository、Entity 和内部辅助方法均不得控制事务生命周期。
- 禁止通过调用另一个公开 Application Service 来复用事务边界。复用应用编排应提取内部 Application Service，复用领域规则应调用 Manager 或领域对象。
- 异常必须继续传播给工作单元机制，不得吞掉异常后返回成功、空值或默认值。
- 禁止在持有数据库锁或长事务时等待不受控外部调用。
- 外部调用必须说明位于事务前、事务内还是事务后，以及超时由框架、适配器还是基础设施配置负责，并说明重试、幂等和补偿策略。目标框架已有统一超时或重试机制时必须直接复用，Application Service 不得再叠加局部超时、重试包装或同类基础设施。
- 跨进程和跨机器唯一性依靠数据库唯一约束或等价持久化机制，不能只用进程内锁。
- Repository 可以实现锁定读取和并发控制；是否允许状态变化仍由 Domain 判断。
- 跨系统可靠投递使用框架能力或 Outbox，不手工维护事件总线队列。
- 事件重试必须幂等，并区分“数据库已提交但消息暂未投递”和“业务事务已回滚”。
- 文件保存与数据库写入必须定义成功、失败和补偿顺序。数据库失败时清理本次新文件；数据库提交后的旧文件清理失败不得回滚当前引用，应记录可追踪补偿。

### 4.2 框架机制映射

设计前必须根据目标项目实际包版本、源码、正式文档和既有用法，填写工作单元声明方式、公开事务入口、事件处理顺序及已验证边界。找不到既有机制或无法确认关键顺序时必须停止受影响设计并请求确认，不能直接复制其他项目的 Attribute、事务代码或自建基础设施。

框架源码或反编译只能形成 `SourceConfirmed` 证据；回滚、提交顺序和事件对外可见性仍须按风险使用真实数据库与真实工作单元失败路径验证。设计必须把“事件已登记”“事件处理器已观察”“数据库已提交”和“外部消息已可靠投递”分别陈述。

未经明确批准，禁止：

- 使用 `TransactionScope`。
- 直接使用 `IDbTransaction`、`DbTransaction`、`BeginTransaction`、`Commit` 或 `Rollback` 控制事务。
- 新增自定义 UnitOfWork、TransactionExecutor 或事务包装器。
- 在 Domain Service、Manager、Repository、Entity 或内部辅助方法中开始、提交或回滚事务。
- 手工清理、恢复或重新发布工作单元中的 DomainEvent 队列。

确实无法使用框架机制时，设计必须说明框架机制不能满足的原因、涉及的数据库或外部资源、隔离级别、超时、事件一致性、幂等、补偿和测试方案，并在实现前取得明确批准。

## 5. Command-Event 评审表

设计中每个 Command/FacadeCommand 必须占一行：

| Command/FacadeCommand | 主要 DomainEvent | 成功条件 | 关键事件字段 | 登记位置 | 失败/幂等语义 |
|---|---|---|---|---|---|
| `ApproveOrderCommand` | `OrderApprovedEvent` | 待审核订单审核成功 | OrderId、FinalState、ReviewerId、OccurredTime | `OrderManager` | 失败不发事件；重复请求返回既有结果 |

未完成该表、存在无事件 Command 或事件发布顺序不清时，不得进入实现。
