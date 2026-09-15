# Dy Framework WorkUnit

本文保留同架构项目的 Dy Framework 工作单元、事务和事件队列约束，作为本项目实现时的核对基线。通用业务事务原则见 [Backend Command Query Event](backend-command-query-event.md)。本项目后端工程已建立，目录与版本依据见 [Project Context](project-context.md) 及其环境引用；具体工作单元机制与证据边界见 [阶段 1 后端设计](../../docs/plans/003-阶段1-标准项目目录维护/Server/design.md) 的框架机制映射。工程和包版本已知不等于实际拦截入口、回滚及事件顺序已验证，不能把迁入文字当作本项目的源码或运行证据。

## 1. 事务入口

需要显式事务的公开 AppService 方法使用框架现有声明：

```csharp
[WorkUnit(UseTransaction = true)]
public async Task<Result> ExecuteAsync(...)
```

- 事务边界仍由写用例的原子性决定，不因使用 Dy Framework 而机械加 Attribute。
- AppService 只声明边界；Manager、Repository、Entity 和内部辅助方法不控制事务生命周期。
- 不得因框架限制新增内部事务服务、`TransactionExecutor`、自定义 UnitOfWork 或手工事务 API。
- Repository 异常必须继续传播到 WorkUnit 失败路径，不得吞掉后返回成功或默认值。

## 2. 外部调用

公开 AppService 进入 WorkUnit 后，外部调用可能处于工作单元生命周期内。设计必须标明外部调用相对第一次 Repository 访问的位置、超时和重试由谁负责、幂等和失败结果。

- 尽可能在第一次 Repository 读取或写入前完成不受控外部调用。
- 通过 Dy Framework 服务代理调用的平台接口，统一使用 Host `HttpConfig.HttpServiceConfigs` 中对应接口的 `Timeout` 等配置；Application Service 和内部应用服务不得再使用 `CancellationTokenSource`、`CancelAfter`、`Task.WaitAsync`、`Task.WhenAny`、`HttpClient.Timeout`、Polly 或自定义包装器叠加超时或重试。
- 用例层不得自行重试 WorkUnit 生命周期内的外部调用；框架是否重试以及如何调整，只按已核实的框架机制和集中配置处理。
- 外部调用失败时不得进入领域写入，不得登记成功事件。
- 不得在持有数据库锁时等待外部系统。
- 不经过框架服务代理的自有外部适配器是独立边界；确需配置超时或重试时，只能在适配器层按已确认设计实现，不能把该例外复制到平台服务代理调用。

## 3. Flush、Commit 与事件风险

同架构参考实现曾识别事件队列 `FlushAsync` 可能先于数据库事务 Commit。本项目包版本已有登记，但真实生命周期行为仍须按当前版本和实际入口取证；这里只保留待核实风险，不把参考项目观察记录为本项目的 `SourceConfirmed` 或运行通过证据：

1. 事件登记、`FlushAsync`、事件处理器观察、事务 Commit 和外部可靠投递是不同事实。
2. WorkUnit Commit 失败时，本地事件处理器可能已经观察到事件；不得默认“事务提交后才发布”。
3. 为事件新增外部推送、跨聚合写入或其他副作用前，必须评估实际顺序、Outbox 或其他可靠事件方案。
4. 业务代码不得手工清理、恢复或重新发布框架事件队列。

涉及回滚的设计，必须用真实数据库、真实 `[WorkUnit]` 入口和失败注入验证零提交。涉及事件可见性的设计，只有在项目拥有可控的事件消费者、持久化记录或专用观察点时，才要求验证事件观察结果；框架负责事件队列且项目没有安全、专用观察点时，该验证面标记为 `N/A`，并记录框架边界，不得通过 mock、源码或反编译虚构事件结论。风险被接受时另加 `AcceptedRisk` 标签，但测试结果仍保持事实状态。

## 4. 变更检查

框架升级或工作单元行为相关实现变化时，至少重新核对：

- `WorkUnitAttribute` 的事务启用语义和实际拦截入口。
- Flush、Commit、Rollback 和事件处理器的真实顺序。
- Repository 异常与 Commit 异常的传播路径。
- 是否已有框架 Outbox、幂等或补偿能力可复用。
- 事务声明的架构测试以及真实数据库失败路径测试。
