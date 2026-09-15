# Backend Architecture

本文是可复制到其他项目的后端分层规范，只定义稳定的架构边界。AI 的设计交付物和停止条件见 [Backend Design Gates](backend-design-gates.md)，Command、Query、DomainEvent 和事务规则见 [Backend Command Query Event](backend-command-query-event.md)。

目标项目可以使用不同的程序集和目录名，但必须先映射到本文的 `Contracts`、`Application`、`Domain`、`Domain.Shared`、`Repository/Infrastructure` 和 `Host` 概念层。项目明确业务决策可以改变聚合边界，不能改变依赖方向和类型职责。

本项目业务身份与授权边界见 [Project Context](project-context.md)。下文的权限检查指复用权限系统的既有授权边界并保护可信组织和资源归属，不要求重复建立角色授权模型；可信登录主体与医院请求提供的业务科室/人员 ID/名称必须区分。

## 1. 依赖方向

典型写操作链路：

```text
Controller/Host
-> IAppService (Contracts)
-> AppService (Application)
-> Command + Manager (Domain)
-> IRepository (Domain)
-> Repository/ORM/SQL (Repository/Infrastructure)
```

依赖规则：

- `Contracts` 定义公共应用契约，只能引用稳定共享类型，禁止引用 Application 实现、Domain Entity 或 Repository。
- `Application` 可以引用 Contracts 和 Domain，禁止依赖 Repository 具体类型、SQL、ORM 或 DataMapper。
- `Domain` 可以引用 Domain.Shared，禁止引用公共 Request、Application Service、HTTP 或数据库实现。
- `Repository/Infrastructure` 实现 Domain 仓储、查询端口和外部适配端口，不做领域决策。
- `Host/Controller` 负责传输绑定和依赖装配，不承载业务状态机。

## 2. 类型归属

DTO 不是独立层，而是边界角色。字段相似不构成复用同一类型的理由。

| 类型 | 概念层 | 职责 |
|---|---|---|
| 公共 Request | Contracts | 调用方能够提交的字段和格式约束 |
| 公共 Response/Dto | Contracts | 稳定的应用服务返回契约 |
| `IXxxAppService` | Contracts | 调用方可见的应用能力 |
| `XxxAppService` | Application | 完整用例的校验、可信上下文和编排 |
| 内部应用 Service | Application | 复用应用层编排，默认不公开接口 |
| Command/FacadeCommand | Domain | 表达有副作用的领域意图 |
| CommandDto/Item | Domain/Commands | Command 内部复杂参数 |
| AggregateRoot/Entity/ValueObject | Domain | 领域状态和局部不变量 |
| `XxxManager`/DomainService | Domain | 聚合规则、状态流转、持久化协调和事件登记 |
| `IXxxRepository` | Domain | Domain 所需的持久化抽象 |
| Repository/ORM/SQL | Repository/Infrastructure | 查询、锁、映射和持久化实现 |
| DomainEvent | Domain 或 Domain.Shared | 已经发生的领域事实 |
| 稳定共享枚举和常量 | Domain.Shared | 跨层共享的业务语义 |
| Query Request/ReadModel | Contracts/Queries | 纯查询输入和输出 |
| DataRequest/内部查询投影 | Query Repository 边界 | 数据库查询参数和内部结果 |
| 外部系统端口接口 | Application 所有的 Abstractions（通常位于 Application 或其 Contracts 程序集） | 对外部能力的本系统抽象 |
| TransportDto | 外部适配器旁 | 第三方 HTTP、文件或消息协议 |
| 外部适配器实现 | Infrastructure/Integrations | 具体协议和技术实现 |
| multipart/流式 Request | Host/Controller | 文件绑定、响应头和资源释放 |

必须保持：

```text
Request != Command != CommandDto != ReadModel != Entity != DomainEvent != TransportDto
```

类型必须分离，但分层不能成为随意改名的理由。Request 与 Command 中表达同一业务事实、语义相同、基数相同且类型兼容的属性，必须使用相同名称；仅因位于不同层、由不同类型承载或希望显得更“领域化”而改名，不构成例外。

只有发生真实边界转换时才允许名称或结构不同，例如：

- 调用方业务编码转换为领域内部 ID，如 `PackageCode -> PackageId`。
- 调用方提交的编码集合转换为经过服务端校验或补充事实的 Command Items，如 `MedicalStandardItemCodes -> Items`。
- Transport 格式转换为领域值、ValueObject 或不同基数的结构。

Command 可以增加 Request 不应提交的可信身份、组织范围、目录事实、操作人和操作时间。不得为了统一属性集合或方便自动映射，把内部 ID、服务端可信字段或派生状态暴露到公共 Request；也不得让 Command 保留尚未解析的 Transport 标识来逃避必要转换。

当前功能存在不同名称或结构的 Request 到 Command 转换时，设计必须逐项说明来源属性、目标属性、转换原因和执行层；没有真实语义、身份或可信来源变化时，必须先统一命名再进入实现。

DomainEvent 只在 Domain 内使用时放 Domain；需要被其他模块或订阅者稳定引用时放 Domain.Shared。禁止把 CommandDto、TransportDto 或 ReadModel 放进 Domain.Shared 方便跨层引用。

## 3. AppService

AppService 实现一个应用用例，标准流程为：

```text
Request 校验
-> 可信身份/组织/权限解析
-> 跨聚合或外部能力编排
-> Request 映射 Command 并补充可信字段
-> 调用 Manager
-> 映射公共结果
```

AppService 负责：

- 公共 Request 校验和权限范围。
- 当前用户、组织、操作时间等可信字段。
- 跨聚合、平台服务和外部系统编排。
- 工作单元入口和公共 DTO 映射。

AppService 禁止：

- 使用 SQL、ORM/DataMapper 或 Repository 具体实现。
- 直接写仓储绕过 Manager。
- 复制 Manager 的状态机和聚合不变量。
- 操作 DomainEvent 队列。
- 实现具体 HttpClient、文件或消息协议。
- 调用另一个 concrete AppService 复用逻辑。

## 4. Manager 和领域对象

本规范采用 Manager 中心的领域实现方式。Manager 可以通过 Domain 仓储接口读取和持久化聚合，但不能知道数据库实现。

Manager 负责：

- 接收 Command，不接收公共 Request。
- 加载聚合根和必要的内部实体。
- 校验所有入口都必须遵守的领域不变量。
- 执行状态流转和聚合内实体协调。
- 通过 `IXxxRepository` 保存变化。
- 成功后登记对应 DomainEvent。
- 返回领域 ID、影响数量或 Domain Result，不返回公共 DTO。

只依赖单个实体状态的规则优先放实体方法；需要仓储查询、多个聚合成员或复杂协调时放 Manager。

Manager 禁止依赖 HTTP 上下文、公共 Request/Response、其他模块的 Application Service、HttpClient、ORM/DataMapper、Repository 具体实现、手工事务 API 和事件总线当前队列。

单实现 Manager 默认不增加接口。存在替换实现或明确跨程序集端口时才引入抽象。

## 5. Repository 和外部适配器

Repository 负责查询、锁、映射和持久化，不负责判断业务状态是否允许变化。特殊 SQL、并发策略和兼容语义必须有测试。

### 5.1 数据库 Provider 中立

- Contracts、Application、Domain、Query Repository 端口和 DataRequest 只能使用项目业务类型、基础值类型和框架中立查询参数，禁止引用 Npgsql、SqlClient、Oracle、MySql 等具体 Provider 类型或方言对象。
- 共享 SQL/XML Statement 必须保持数据库 Provider 和版本中立，使用项目确认的公共 SQL 子集；不得包含绑定某一数据库、驱动或版本的类型、函数、分页语法（`LIMIT/OFFSET`、`TOP`）、空值排序（`NULLS FIRST/LAST`）、行值 `COUNT(DISTINCT (...))`、标识符写法、系统表、查询提示、类型转换、JSON 运算符或其他方言特征。
- 分页、参数绑定、标识符和其他数据库方言优先交给 ORM/DataMapper/Earthrace 的当前 Provider 适配层生成。业务 SQL 必须自行控制窗口时，只能使用已确认的公共 SQL 窗口方案；公共 Statement 无法表达目标语义时，先回到设计确认，并在 Repository/Infrastructure 内建立显式 Provider/Dialect 适配器；不得把 Provider 判断写入公共 XML、Contracts、Application 或 DataRequest。
- 为跨 Provider 移除 SQL 投影方言时，可以在 Repository 映射或 Application 映射中处理不影响筛选、数据范围和排序业务含义的派生展示字段；不得把数据库过滤后移为加载全量数据后的内存过滤。
- 当前 Provider 和版本的运行证据只证明该 Provider 和版本。只有在目标 Provider 上重跑同一验证矩阵后，才能声明对应数据库受支持；不得从单一 Provider 的测试结果推导跨数据库兼容。

### 5.2 SQL/XML 注释

#### Statement 功能注释

每个手写 `<Statement Id="...">` 前必须使用 XML 注释说明该 SQL 的完整职责：

- 查询语句说明查询对象、业务范围和返回结果。
- 新增、修改、删除语句说明操作对象、变更内容和影响结果。
- Count、Exists、汇总等辅助语句也必须独立说明，不能只依赖相邻查询的注释。
- 注释使用业务语言，不只重复 Statement Id 或 SQL 关键字。

```xml
<!--
  功能：查询符合条件的业务对象分页列表。
  结果：返回业务对象摘要，并按创建时间和标识稳定排序。
-->
<Statement Id="QueryEntitySummaryList">
```

#### 字段中文注释

- SELECT 投影、INSERT 字段清单和 UPDATE 赋值字段必须使用 XML 注释标明字段的中文业务名称。
- 计算列、聚合值和 CASE 结果必须说明最终业务含义，不能只描述计算表达式。
- `count(*)`、固定值或框架技术字段没有歧义时，可以在 Statement 功能注释中统一说明，不要求制造重复注释。

```xml
select
  id,                         <!-- 业务对象标识 -->
  entity_name,                <!-- 业务对象名称 -->
  created_time,               <!-- 创建时间 -->
  status                      <!-- 业务状态 -->
```

#### 复杂查询注释

- 子查询、动态条件分支、CASE、EXISTS/NOT EXISTS、UNION、分组过滤、特殊 JOIN、窗口计算或其他不能直接看出业务目的的查询块，必须在相邻位置增加 XML 注释。
- 注释必须写清这段逻辑做什么、为什么需要这样处理，以及最终保留或产生什么结果。
- 注释不能用来豁免数据库可移植性要求；已经解释的非公共 SQL 仍不得进入共享 XML。
- 统一使用 XML 注释 `<!-- -->`，不使用可能传入数据库的 `--`、`/* */` 或查询提示承担文档职责。

```xml
<!--
  处理：先按两个业务维度分组，再统计分组数量。
  原因：避免依赖部分数据库不支持的多字段 COUNT DISTINCT 写法。
  结果：得到两个维度组合去重后的总数。
-->
```

规则覆盖所有手写 Statement。新增或修改的 Statement 必须立即执行；未触及的历史 Statement 通过独立治理批次逐步补齐，禁止借规则接入无边界改写业务 SQL。

外部系统采用端口和适配器：

```text
AppService -> 外部端口 -> HTTP/文件/消息适配器 -> 外部系统
```

- 端口使用本系统业务语义，不能泄漏第三方协议。
- 端口由 Application 一侧拥有，Application 只依赖端口接口；Infrastructure/Integrations 引用该抽象并提供实现，Host 只负责装配。不得让 Application 引用基础设施项目来取得接口。
- 适配器负责认证、序列化、状态码和 TransportDto。
- AppService 决定调用时机并转换为 CommandDto。
- 协议完整性由适配器校验，影响领域状态的业务完整性由 Manager 校验。

## 6. 聚合边界和代码拆分

聚合依据一致性、生命周期和事务边界划分，不依据文件、表或实体数量。

同一聚合文件较多时，可以：

- 按调用方和用例拆 AppService。
- 按业务能力拆 Manager partial 文件。
- 按相同能力拆 Repository 和 IRepository partial 文件。
- 按用例组织 Command、Event、Request 和 DTO 目录。

拆文件或 AppService 不等于拆聚合。只有出现独立生命周期、独立事务提交、允许最终一致性或明确跨聚合引用时，才重新讨论聚合边界。

## 7. 接口和 Service 命名

接口位于定义抽象的一侧，不能仅凭 `I` 前缀决定层级。

```text
IOrderAppService / OrderAppService
IOrderQueryAppService / OrderQueryAppService
OrderValidationService
OrderManager
IOrderRepository / OrderRepository
IBarcodeGenerator / HttpBarcodeGenerator
QueryOrderDataRequest
OrderDetailReadModel
OrderDetailDto
```

禁止使用无法判断职责的 `IService`、`Service`、`Helper`、`Processor`、`Handler` 或 `Util`。确需使用时，设计必须说明所属层、调用方、输入输出和禁止职责。
