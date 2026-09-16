# 阶段 3 后端设计

## 设计基线

聚合沿用 `MedicalRecognitionReportAggregate`。阶段 3 只新增互认项目金额的保存与查询能力，不提供金额版本、金额变更历史、历史金额重算、金额启停、批量保存或分页；不建立服务端角色权限模型。

当前代码事实：金额相关的生成态只有 `OrganizationHospitalBranchRecognitionAmount` 实体、同族 DTO、`OrganizationHospitalBranchRecognitionAmountDto` 映射声明、`Repository/MedicalRecognitionReportAggregate/OrganizationHospitalBranchRecognitionAmount.xml`（旧聚合 scope，只有 Columns 与无 where 的 `QueryAll`）与 `Repository/Scripts/organization_hospital_branch_recognition_amount.sql`（无 `mrec_` 前缀、`varchar(100)`、无注释、无唯一索引）；`SaveOrganizationHospitalBranchRecognitionAmountCommand`、对应事件、Manager 方法、仓储方法、`QueryRecognitionAmountList` 与应用服务入口**均不存在**。`Application.Contracts/ReadModels/RecognitionAmountReadModel.cs` 是提前生成的 16 字段版本，与 UML2 调整后的字段集不一致。以上都是待实现或待修正的现状，不是已验证结论。

高风险决策台账只维护在阶段根设计：两个入口与可信边界引用 S3-D1，权限口径引用 S3-D2，Command 收敛引用 S3-D3，列表字段引用 S3-D6，批量读取引用 S3-D7，不分页引用 S3-D8。本文件细化实现边界与 V1-V30，不复制台账、不把决策标签当测试结果。阶段根台账无 `Pending` 项；`DesignConfirmed` 只表示设计已确认，**不表示已实测**：S3-D15、S3-D16 与组织服务能力仍以批次 1 的实测 `restore/build`、`typecheck/build` 为闸门。

## 公共契约与分层

**两个保存入口。** `IMedicalRecognitionReportAppService` 新增两个方法：

- `SaveOrganizationHospitalBranchRecognitionAmountAsync(SaveOrganizationHospitalBranchRecognitionAmountRequest)`：平台管理员入口。Request 提交 `OrganizationCode`、`HospitalCode`、`BranchCode`、`StandardProjectCode`、`CurrentAmount`；组织、医院、院区三个值按请求使用，服务端校验其存在、启用与父子归属。
- `SaveBranchRecognitionAmountAsync(SaveBranchRecognitionAmountRequest)`：医院管理员入口。Request 只提交 `BranchCode`、`StandardProjectCode`、`CurrentAmount`；`OrganizationCode` 与 `HospitalCode` 由服务端从可信上下文注入，请求不提交也不得覆盖。

两个入口都从可信上下文 `HttpRequestInfo.UserId` 解析操作人为非空 `Guid`，无法解析或为 `Guid.Empty` 时拒绝；操作时间使用 `DateTimeOffset.UtcNow`。两个入口都要求当前组织已建立该标准项目编码的互认配置（SRS F10 前置条件 3 与备选流 3），未建立即拒绝保存。

**两个查询入口。** `IMedicalRecognitionReportQueryAppService` 新增两个方法，返回同一 `RecognitionAmountReadModel`：

- `QueryRecognitionAmountListAsync(RecognitionAmountListQueryRequest)`：平台管理员入口，Request 提交 `OrganizationCode`（必填）、`HospitalCode`（必填）、`BranchCode`（必填）、`StandardProjectCode`（可选，字面包含）。
- `QueryBranchRecognitionAmountListAsync(BranchRecognitionAmountListQueryRequest)`：医院管理员入口，Request 只提交 `BranchCode`（必填）与 `StandardProjectCode`（可选）；组织与医院来自可信上下文。

两个入口的**取值口径与保存一致**（S3-D1、S3-D18）：平台管理员入口的组织、医院、院区取自请求提交值，服务端校验三者存在、启用且父子归属正确；医院管理员入口的组织与医院取自可信上下文（缺失即拒绝，不使用默认值、不降级为空值），请求院区必须属于可信医院，不属于即拒绝，不降级为空集合。**不得要求平台管理员入口的查询组织等于可信上下文组织**——那会使平台管理员无法查询其他组织、医院、院区的金额，与 SRS「互认项目金额维护」页面要求 1、UML2 的 `QueryRecognitionAmountList` 说明及本阶段 S3-D1 冲突。`RecognitionAmountReadModel` 字段固定为：`StandardProjectCode`、`StandardProjectName`、`ItemType`、`CategoryName`、`GroupName`、`OrganizationName`、`HospitalName`、`BranchName`、`ConfigurationStatus`、可空 `UnavailableReason`、可空 `CurrentAmount`、`IsAmountConfigured`；不返回组织编码、医院编码、院区编码，不返回最后修改时间与最后修改人。按标准项目编码升序，不分页。

**分层职责。** Host 绑定 Request；Application 校验公共输入、解析可信组织/医院/操作人、编排外部组织服务、把编码解析为 Command 并调用 Manager；Domain Manager 校验业务不变量、维护持久化协调与事件登记；Repository 负责金额读写、业务键读取、唯一冲突识别与查询 SQL；Contracts 不引用 Domain Entity。Request、Command、ReadModel、Entity、DomainEvent 分离。

组织服务契约已按字节级元数据核对（**尚未真实调用**）：`IOrganizationAppService` 提供 `QueryAllOrganizationAsync()`、`QueryAllValidHospitalByOrgIdAsync(OrgId)`、`QueryAllValidBranchByOrgIdAsync(OrgId)`、`QueryAllValidBranchByHosIdAsync(HosId)`、`GetOrganizationByIdAsync`/`GetHospitalByIdAsync`/`GetBranchByIdAsync`；组织、医院、院区的**业务编码由 `Id`（`String`）承载**、没有独立 `Code` 字段、启用状态由 `IsValid` 承载（以上为元数据级核对，**未逐类型绑定复核**）；**没有**父子归属的专用校验方法，需由调用方在内存比对 `HospitalDto.OrgId` 与 `BranchDto.OrgId`/`HosId`。本项目 `server/Dy.MedicalRecognition` 目前**未引用** `Dy.Base.Application.Contracts`，`appsettings.Development.json` 中的代理只有配置、没有调用代码；须在批次 1 新增该包引用与中央版本条目，并以实测 restore/build 确认（`Dy.Core.Abstractions` 为 `1.1.0.54` 而该包要求 `≥1.1.0.49`，本机无该组合的构建先例）。若实测 restore/build 失败，退路**须先经负责人确认后选定，不自行降级**：① 对齐 `Dy.Core.Abstractions` 到该包兼容的版本（**影响全解决方案的中央版本，须按 `repository-safety` 评估并取得负责人确认**）；② 改由 Host 侧在请求进入时提供组织路径数据，不新增服务端外部调用。无论选哪条，都不得降级为"不校验归属"。

### 层目录与类型归属

下表路径均相对 `server/`，类型表中的目录别名以此为准。

| 概念层 | 实际目录与别名 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | C = `Dy.MedicalRecognition.Application.Contracts/` | 共享枚举、框架契约 | Application、Domain Entity、Repository |
| Application | A = `Dy.MedicalRecognition.Application/` | Contracts、Domain 接口和类型、外部服务抽象 | Repository 实现、SQL、DataMapper |
| Domain | D = `Dy.MedicalRecognition.Domain/` | Domain.Share、框架领域抽象 | Request、HTTP、Application、数据库实现 |
| Domain.Shared | S = `Dy.MedicalRecognition.Domain.Share/` | 框架事件与稳定共享类型 | Application、Repository |
| Repository/Infrastructure | R = `Dy.MedicalRecognition.Repository/` | Domain 仓储接口、内部投影、DataMapper | AppService 实现、领域状态决策 |
| Host | H = `Dy.MedicalRecognition/` | 契约、框架装配 | 业务状态机、直接业务 SQL |

目录中 `MedicalRecognitionReportAggregate/` 以下简称 `Aggregate/`。以下是目标类型，不表示生成物已经符合设计；已有修改与新增明确分开。

| 具体类型 | 状态/角色 | 层/目录 | 调用方与职责 | 禁止职责 |
|---|---|---|---|---|
| `SaveOrganizationHospitalBranchRecognitionAmountRequest` | 新增 Request | C/Aggregate/Requests | Host 绑定组织、医院、院区、标准项目编码与金额 | 接受操作人、状态或内部 ID |
| `SaveBranchRecognitionAmountRequest` | 新增 Request | C/Aggregate/Requests | Host 绑定院区、标准项目编码与金额 | 接受组织、医院、操作人 |
| `RecognitionAmountListQueryRequest` | 新增 Query Request | C/Queries | 平台管理员入口输入组织、医院、院区与可选编码 | 把输入当授权结论 |
| `BranchRecognitionAmountListQueryRequest` | 新增 Query Request | C/Queries | 医院管理员入口输入院区与可选编码 | 接受组织与医院 |
| `RecognitionAmountReadModel` | 修改并迁入 Queries | C/Queries；现位于 C/ReadModels | 两个查询入口共用的返回契约 | 作用域编码、最后修改字段、派生状态字段 |
| `IMedicalRecognitionReportAppService` | 扩展既有写契约 | C/Aggregate | Host 的两个金额保存入口 | 通用查询 |
| `MedicalRecognitionReportAppService` | 修改 AppService | A/Aggregate | 输入校验、可信字段解析、组织服务编排、调用 Manager | 直接仓储写入、复制领域规则 |
| `IMedicalRecognitionReportQueryAppService` | 扩展既有查询契约 | C/Queries | 增加两个金额查询方法 | 新建同义接口 |
| `MedicalRecognitionReportQueryAppService` | 扩展既有 QueryAppService | A/Queries | 组织范围校验、原因派生、名称字典填充、投影转 ReadModel | 写入或事件 |
| `SaveOrganizationHospitalBranchRecognitionAmountCommand` | 新增 Command | D/Aggregate/Commands | AppService 传入完整业务键、金额与操作字段 | HTTP、展示字段、暴露入口差异 |
| `OrganizationHospitalBranchRecognitionAmountSavedEvent` | 新增 Event | S/Aggregate/Events | Command 工厂创建"金额已保存"事件 | Request、展示文本、入口标识 |
| `OrganizationHospitalBranchRecognitionAmount` | 核对 Entity 并修正注释 | D/Aggregate | Manager 读写当前金额与操作字段 | 累加运算、金额版本 |
| `MedicalRecognitionReportManager` | 修改既有 Manager | D/Aggregate | 业务键读取、互认配置存在校验、条件写入、影响行数、事件登记 | 控制事务、公共 DTO、外部调用 |
| `IMedicalRecognitionReportRepository` | 扩展既有仓储接口 | D/Aggregate | 金额按业务键读取、新建与覆盖写入 | Provider 类型 |
| `MedicalRecognitionReportRepository` | 修改既有仓储 | R/Aggregate | 实现上述接口、影响行数检查、唯一冲突识别 | 决定金额是否可保存 |
| `OrganizationPathResolver` | 新增应用内部服务 | A/Validation | 一次性批量读取组织、医院、院区，内存校验父子归属并返回名称三元组 | 公共契约、角色授权判断、按行查询 |
| `TrustedOrganizationResolver` | 扩展既有内部解析点 | A | 增加可信医院解析（与既有可信组织解析同一去空白与拒绝口径） | 判断调用方是否有权 |
| `IMedicalRecognitionReportQueryRepository` | 扩展既有查询仓储接口 | D/Queries | 金额列表内部投影查询 | 公共 ReadModel、授权判断 |
| `MedicalRecognitionReportQueryRepository` | 扩展既有查询仓储 | R/Queries | SQL 关联互认配置、标准目录与金额，排序返回投影 | 领域写入 |
| `RecognitionAmountListItem` | 新增内部投影 | D/Queries | 仓储返回金额行及目录三层状态，供 Application 派生原因与填充名称 | 充当公共契约 |

复用而非新增：`MedicalItemType`、`ConfigurationStatus` 复用 `S/Enums` 既有枚举；金额查询复用 `D/Queries/IMedicalRecognitionReportQueryRepository` 与既有 `R/Queries/MedicalRecognitionReportQuery.xml`；金额写入使用 `R/Aggregate/OrganizationHospitalBranchRecognitionAmount.xml`。不为本阶段增加 Manager 接口、独立金额查询服务接口或独立金额仓储类，也不新增第二套组织服务抽象（框架已经过 `HttpServiceConfigs` 代理注入 `IOrganizationAppService`）。

### 调用链与转换

**保存链（平台管理员入口）**：Host → `SaveOrganizationHospitalBranchRecognitionAmountRequest` → `MedicalRecognitionReportAppService.SaveOrganizationHospitalBranchRecognitionAmountAsync` → 解析可信操作人 → `OrganizationPathResolver` 一次性批量读取并校验组织/医院/院区（存在、启用、父子）→ 读取该组织该标准项目的互认配置（不存在即拒绝）→ 组装 `SaveOrganizationHospitalBranchRecognitionAmountCommand`（业务键 + 金额 + `OperId`/`OperTime`）→ `MedicalRecognitionReportManager` → `IMedicalRecognitionReportRepository` → `OrganizationHospitalBranchRecognitionAmount.xml` → 写入成功后 Manager 调用 Command 事件工厂并 `AddEvent`。

**保存链（医院管理员入口）**：Host → `SaveBranchRecognitionAmountRequest` → `MedicalRecognitionReportAppService.SaveBranchRecognitionAmountAsync` → 从可信上下文解析组织与医院（缺失即拒绝）与操作人 → 读取请求院区并校验其存在、启用且属于可信医院 → 其余与上一入口完全相同，映射到**同一个 Command**。

两个入口解析完成后交给 Manager 的领域数据完全相同，入口差异只存在于 Application 的取值来源与校验顺序，因此不建立第二个 Command；S3-D3 记录了该收敛及其理由。事件不携带入口标识。

**查询链**：Host → 查询 Request → `MedicalRecognitionReportQueryAppService` 解析可信组织（医院管理员入口另解析可信医院）→ `OrganizationPathResolver` 一次性批量读取并校验路径、同时取得组织/医院/院区名称 → 既有 `IMedicalRecognitionReportQueryRepository` 的新增方法 → `MedicalRecognitionReportQueryRepository` / `MedicalRecognitionReportQuery.xml`，以 `mrec_mutual_recognition_item` 为行集、按业务键 LEFT JOIN `mrec_organization_hospital_branch_recognition_amount`、关联标准目录取得名称/类型/分类/分组与三层启用状态，按标准项目编码升序 → `RecognitionAmountListItem` → Application 派生 `UnavailableReason`、填充名称与 `IsAmountConfigured`、映射 `RecognitionAmountReadModel`。查询不得产生写入、审计事件或消息。

内部投影携带分类/分组/项目三层启用状态供派生原因，公共返回不泄漏这三层原始字段，只返回派生后的 `UnavailableReason`。金额未配置时 `CurrentAmount` 为 `null`；不以 `0` 冒充未配置。

## Command、事件与事务

| Command | 主要事件 | 成功条件 | 关键事件字段 | 登记位置 | 失败/幂等语义 |
|---|---|---|---|---|---|
| `SaveOrganizationHospitalBranchRecognitionAmountCommand` | `OrganizationHospitalBranchRecognitionAmountSavedEvent` | 成功返回 `Boolean=true`（`docs/uml/命令出参整改矩阵.md` B 组）；业务键无记录时插入恰好 1 行，有记录时按业务键条件更新恰好 1 行 | `Id`、`OrganizationCode`、`HospitalCode`、`BranchCode`、`StandardProjectCode`、`CurrentAmount` | `MedicalRecognitionReportManager` | 校验失败、互认配置不存在、唯一冲突、0 行或大于 1 行均不登记事件；同值也保存并登记 |

该事件继承框架 `DomainEvent`，保留既有 `AggregateId = MedicalRecognitionReportConst.AggregateId` 与 `EventType`；金额记录标识使用 `Id`。事件时间与操作人不另造字段，沿用 Command 事件工厂的 `MapTo...Event(eventCreator: OperId, eventCreatedTime: OperTime)` 映射，即 `EventCreator ← OperId`、`EventCreatedTime ← OperTime`；这是源码映射证据，不是运行投递证据。事件不携带调用入口、页面名称或提示文案。

两个 Application 入口共用同一个 Command 与同一个主要事件，符合"一个 Command 对应一个主要 DomainEvent"；不因入口不同建立第二个 Command 或第二套事件，也不使用含糊的 `ChangedEvent`。

### 写用例事务表

| 写用例 / AppService公开入口 | Repository数据库操作 | 显式事务/框架机制 | 原子性与并发兜底 | 事件口径 | 外部调用顺序 | 失败语义 / 验证 |
|---|---|---|---|---|---|---|
| 平台管理员保存 / `SaveOrganizationHospitalBranchRecognitionAmountAsync` | 互认配置读取后，按业务键读取 1 次；命中则 1 条 `UPDATE`，未命中则 1 条 `INSERT` | 否；不加 `[WorkUnit]` | 单条写语句依赖语句级原子性；四个业务键列唯一索引兜底并发首插 | 恰好 1 行才登记保存事件 | 组织服务批量读取在第一次 Repository 访问**之前**完成 | 校验失败/配置缺失/唯一冲突/0 行或大于 1 行均不登记事件；V1-V12、V15-V16、V27 |
| 医院管理员保存 / `SaveBranchRecognitionAmountAsync` | 同上 | 否；不加 `[WorkUnit]` | 同上 | 同上 | 同上 | 可信组织或医院缺失即拒绝；请求院区不属于可信医院即拒绝；其余同上；V12-V16、V27 |

**事务决策。** 每个写用例最多执行一条写语句（`INSERT` 或 `UPDATE` 二选一），因此依靠数据库语句级原子性，不声明 `[WorkUnit]`、不手工开启事务，沿用阶段 2 的 S2-D6 口径（只有跨表原子写入或 FacadeCommand 才使用框架工作单元）。本项目不使用 PostgreSQL 的 `ON CONFLICT`/`UPSERT` 方言写法（`backend-architecture` 第 5.1 节要求共享 SQL 保持 Provider 中立），因此采用"先按业务键读取、再决定插入或更新"的两步式，事务边界仍只有一条写语句。

**顺序与外部调用。** 组织服务批量读取位于第一次 Repository 访问之前，不进入写语句之间的窗口；超时与重试沿用 Host `HttpConfig.HttpServiceConfigs` 的集中机制，应用层不再包装、不叠加局部超时或自动重试。外部校验失败不得进入写入、不得登记事件。

### 保存归属与并发

平台管理员入口按请求使用组织、医院、院区，并校验三者存在、启用且父子归属正确；不校验调用方是否有权访问该组织，权限由权限系统负责（S3-D2）。医院管理员入口的组织与医院只来自可信上下文，请求不提交；缺少任一层即拒绝，不使用默认值、不降级为空值；请求院区必须存在、启用且属于可信医院，否则拒绝。医院管理员只能在本医院（可信上下文所属医院）范围内选择院区（S3-D19）；**不引入"院区级数据权限"概念**，服务端不做该层校验，权限由权限系统负责（S3-D2）。

可信组织与医院的解析沿用既有 `TrustedOrganizationResolver` 的去空白与拒绝口径，新增 `ResolveHospitalOrThrow(string? hospitalCode, string missingHospitalMessage)`，不新建解析类。两个入口都以"当前组织已建立该标准项目编码的互认配置"为保存前提，不因互认配置停用或标准目录停用而阻断金额维护（SRS F10 业务规则 5）。

按业务键读取后：无记录则 `INSERT` 一条新行（`Id` 由 Manager 生成，操作字段取 Command）；有记录则按 `id` 与四个业务键列做条件 `UPDATE`，只更新 `current_amount`、`oper_time`、`oper_id`。`UPDATE` 影响 0 行时按同一业务键复读一次：记录已被删除（正常业务不可达）即拒绝，金额已被并发改为相同值即按成功处理并登记事件，仍无法解释即返回并发冲突并提示刷新后重试，不自动无限重试、不替用户循环重放意图。影响行数大于 1 视为持久化不变量异常，不返回成功、不登记事件。

并发首次保存同一业务键时，两个会话都可能读不到记录并各自 `INSERT`，由唯一索引 `ux_mrec_org_hos_brh_project` 兜底；冲突识别后统一翻译为"该医院院区标准项目金额已被并发保存，请刷新后重试"的业务拒绝，不登记事件、不自动改用更新路径（S3-D12）。不增加版本字段，也不锁定读取。

### 无 WorkUnit 事件证据边界

事件登记、事件处理器观察、数据库提交与外部可靠投递是不同事实。本阶段生产入口同样不声明 `[WorkUnit]`，框架行为沿用阶段 2 已取证结论：Flush 主体为框架 `EventBusActionUnitFilter.OnCompletedAsync` 调用 `IEventQueue.FlushAsync()`，该过滤器经 `MapControllers()` + `AddEndpointFilter` 无条件挂载，与是否声明 `[WorkUnit]` 无关；时点在业务方法返回成功后、响应产出前；无 `[WorkUnit]` 时 `TransactionInfo == null`，不 Begin/Commit/Rollback，写入失败或处理器失败均不回滚，失败走 `Clear()`、队列已清空不可重试。以上为 `SourceConfirmed`，见阶段 2 的 [取证-组织与事件机制](../../004-阶段2-标准项目互认配置/Server/取证-组织与事件机制.md)。

本项目零 `IEventHandler` 实现、无事件持久化表、无专用观察点，事件可见性子面记 `N/A` 并加 `AcceptedRisk`；V27 的**数据库写入与失败响应面**仍须在真实库与真实入口下验证，不得因该子面记 `N/A` 而把整条 V27 记为通过。不新增带 `[WorkUnit]` 的探针入口，也不手工 Flush、清理或重新发布事件队列以制造证据。

## 数据库、SQL 与 scope

目标表为 `mrec_organization_hospital_branch_recognition_amount`，表注释为"组织医院院区互认项目金额"。最终脚本位置为 `server/Dy.MedicalRecognition.Repository/Scripts/organization_hospital_branch_recognition_amount.sql`，严格按以下列序交付；这里只定义 DDL，不执行建表或迁移。

| 列（按顺序） | PostgreSQL类型 | 可空 | DB默认值 | 中文列注释 / 值来源 |
|---|---|---|---|---|
| `id` | `uuid` | NOT NULL，主键 | 无 | 主键ID；Manager 生成 |
| `organization_code` | `text` | NOT NULL | 无 | 组织编码；平台管理员入口取请求，医院管理员入口取可信上下文 |
| `hospital_code` | `text` | NOT NULL | 无 | 医院编码；来源同上 |
| `branch_code` | `text` | NOT NULL | 无 | 院区编码；两个入口均取请求，且须已校验归属 |
| `standard_project_code` | `text` | NOT NULL | 无 | 标准项目编码；请求提供，须已建立互认配置 |
| `current_amount` | `numeric(18,2)` | NOT NULL | 无 | 当前金额；请求提供，非负且最多两位小数 |
| `oper_time` | `timestamptz` | NOT NULL | 无 | 操作时间；Command.OperTime |
| `oper_id` | `uuid` | NOT NULL | 无 | 操作人；Command.OperId |

该实体没有作废、逻辑删除或启用等生命周期状态标志，因此不存在需要前置到操作字段之前的状态列，列序即 `id`、业务列、`oper_time`、`oper_id`，符合项目 DDL 脚本规则。字符串没有已确认业务长度上限，使用 PostgreSQL `text`，不添加 `varchar(n)`、Request 最大长度或截断规则。全部列无数据库默认值，INSERT/UPDATE 显式绑定操作时间，不使用 `current_timestamp` 代替 Command 时间。金额物理类型为 `numeric(18,2)`，业务校验仍保留"非负且最多两位小数"，不因物理精度放宽或收紧业务范围。

唯一索引固定为 `ux_mrec_org_hos_brh_project`，覆盖 `(organization_code, hospital_code, branch_code, standard_project_code)`，无状态过滤；索引注释为"同一组织医院院区标准项目金额唯一"。脚本包含 `COMMENT ON TABLE`、每列 `COMMENT ON COLUMN` 和 `COMMENT ON INDEX`。不创建跨系统外键，不继承历史三表索引，不迁移或触碰其他系统对象；负责人确认目标数据库与 schema 并执行最终 DDL 后，才继续依赖新表的集成验证。

SqlMap 的 `Scope` 与每个 DataMapper 调用点统一为实体名 `OrganizationHospitalBranchRecognitionAmount`，不调用 `SetContext`，不按实体拆仓储类；语句 Id 保持业务化命名并与调用点显式 `sqlId` 对齐。手写 Statement 为：`OrganizationHospitalBranchRecognitionAmountColumns`、`GetOrganizationHospitalBranchRecognitionAmountByBusinessKey`（按四个业务键列读取单行）、`InsertOrganizationHospitalBranchRecognitionAmount`、`UpdateOrganizationHospitalBranchRecognitionAmount`（按 `id` 与四个业务键列条件更新金额与操作字段）。原有的无 where `QueryAllOrganizationHospitalBranchRecognitionAmount` 不保留：它没有业务调用点，也不满足业务键定位语义。金额列表查询的 Statement 加入既有 `R/Queries/MedicalRecognitionReportQuery.xml`，与阶段 2 的 `QueryRecognitionProjectConfigurationList` 同一 scope 与同一文件。每个手写 Statement 补业务功能 XML 注释；SELECT、INSERT、UPDATE 字段补中文注释；LEFT JOIN、三层状态投影与排序补处理、原因、结果注释。

金额列表 SQL 以 `mrec_mutual_recognition_item` 为行集，按 `(organization_code, hospital_code, branch_code, standard_project_code)` LEFT JOIN 金额表；标准目录通过既有 `mrec_medical_standard_item`、`mrec_medical_standard_group`、`mrec_medical_standard_category` 关联取得名称、类型、分类、分组与三层启用状态。**行集不会放大**：阶段 2 的唯一索引覆盖 `mrec_mutual_recognition_item(organization_code, standard_project_code)` 且含停用配置，同一组织的同一标准项目至多一行配置——这是 V17「行数等于该组织已配置标准项目数」的依据。SQL 必须保持 Provider 中立：不使用 `ON CONFLICT`、`LIMIT/OFFSET`、`NULLS FIRST/LAST`、`COUNT(DISTINCT (...))`、标识符引号写法、系统表或类型转换等方言特征。

目录停用原因在 Application 层按阶段 2 的既有规则派生，只处理停用不处理删除：按"分类 → 分组 → 标准项目"优先级返回第一条原因，文案依次为"所属分类已停用""所属分组已停用""标准项目已停用"；三层全部启用时为 `null`，即使互认配置自身停用也如此。互认配置自身状态只由 `ConfigurationStatus` 表达，不占用 `UnavailableReason`。

## 生成维护边界

后续生成维护采用受保护的手工维护，不依赖重新生成全后端：设计确认后可修改 Request、ReadModel、Command、Event、Entity、DataMap、Repository 接口/实现、XML、DDL，并实现 Application、Manager、查询投影与 Host 接入。`ReadModels/RecognitionAmountReadModel.cs` 迁移到 `C/Queries` 并按 UML2 调整字段；`ReadModels/RecognitionProjectConfigurationReadModel.cs` 已在阶段 2 完成同类迁移，沿用同一位置约定。

新增 `Dy.Base.Application.Contracts` 包引用与中央版本条目属于实现动作，位于 `server/Directory.Packages.props` 与使用它的项目文件；该文件在 `project-context.md` 的再生成保护清单内，改动前须按其要求核实维护方式，不因本阶段需要而降低保护级别。生成文件与人工维护文件的边界按项目仓库规范处理；本阶段只要求最终公共契约、SQL 映射与数据库脚本保持一致。

## 验证矩阵

V1-V16 覆盖金额保存与归属，V17-V22 覆盖查询，V23-V25 覆盖数据库静态与真实库面，V26-V28 覆盖契约、事件与语义修正，V29-V30 覆盖平台管理员跨组织口径与医院管理员院区范围；不合并、不挪号。

### 测试数据边界

所有业务数据和状态只通过正常业务接口（公开 AppService/API）或页面形成：先通过阶段 1 能力建立分类、分组与标准项目，再通过阶段 2 能力建立互认配置，之后才形成金额数据。禁止直接执行 SQL、使用数据库工具、脚本或测试夹具写库，禁止伪造重复主键、孤儿关联或其他违反业务约束的数据。

不可由正常业务流程形成的数据状态不作为普通业务用例前置。请求边界的防御性校验可以使用无持久化数据的输入验证，但不得借此制造数据库脏数据。并发与唯一约束验证必须在正常接口建立合法前置数据后再发起请求。

真实数据库验证需负责人确认目标数据库与 schema、阶段 1 三表与阶段 2 表可用、阶段 3 最终 DDL 执行完成；组织服务相关验证需先完成包引用并实测 restore/build。无上述证据的受影响运行项记 `Blocked`，其余未执行项记 `NotRun`，不把决策标签当测试状态。

V25 单元验证事件构造与登记规则，不能替代 V27 的真实生产调度；V23/V24 先做无需数据库的静态检查，真实类型、约束与注释仍待建表确认。本阶段文档任务不执行该矩阵。

| 编号 | 用例与边界 | 层级 | 预期 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| V1 | 首次保存金额（无记录） | Domain/Repository | 新建 1 行、返回成功、登记保存事件 | 是 | 否 | 互认配置已存在；组织/医院/院区有效 |
| V2 | 覆盖已有金额 | Domain/Repository | 按业务键更新 1 行，金额取提交值，不累加 | 是 | 否 | 同一业务键已有金额 |
| V3 | 保存零元 | Domain/Repository | 成功；`IsAmountConfigured=true`、金额为 `0.00`，与未配置可区分 | 是 | 否 | 互认配置已存在；从未保存过金额 |
| V4 | 金额为负数 | Contract/Domain | 拒绝，无写入、无事件 | 否 | 否 | 非法输入 |
| V5 | 金额超过两位小数 | Contract/Domain | 拒绝，无写入、无事件 | 否 | 否 | 非法输入 |
| V6 | 金额缺省或非数值 | Contract | 拒绝 | 否 | 否 | 非法输入 |
| V7 | 同值重复保存 | Domain/Repository | 成功，更新操作字段并登记事件（无无变更短路） | 是 | 否 | 已有金额 |
| V8 | 保存的组织不存在或已停用（平台管理员入口） | Application | 拒绝，无写入、无事件 | 否 | 否 | 组织服务返回中无该启用组织 |
| V9 | 保存的医院不存在、已停用或不属于该组织 | Application | 拒绝，无写入、无事件 | 否 | 否 | 组织服务返回中该医院父归属不匹配 |
| V10 | 保存的院区不存在、已停用或不属于该医院 | Application | 拒绝，无写入、无事件 | 否 | 否 | 组织服务返回中该院区父归属不匹配 |
| V11 | 当前组织未建立该标准项目的互认配置 | Domain/Application | 拒绝，无写入、无事件 | 是 | 否 | 标准项目存在但无互认配置 |
| V12 | 平台管理员入口取值来源 | Application/Contract | 请求的组织、医院、院区被采用；操作人只来自可信上下文；Request 不含操作人 | 否 | 否 | 可信身份 + 三个请求值 |
| V13 | 医院管理员入口取值来源与越权院区 | Application/Contract | Request 不含组织与医院；组织与医院取可信上下文；请求院区不属于可信医院时拒绝 | 否 | 否 | 可信组织/医院；本医院院区与它医院院区 |
| V14 | 医院管理员入口可信组织或医院缺失 | Application | 拒绝，不使用默认值、不降级为空值 | 否 | 否 | 缺少 `org` 或 `hos` 的身份上下文 |
| V15 | 标准目录或互认配置停用后仍可保存金额 | Domain/Application | 成功，不因停用被阻断 | 是 | 否 | 目录/配置停用但互认配置存在 |
| V16 | 并发首次保存同一业务键 | Repository/Application | 唯一约束兜底；冲突翻译为业务拒绝；库中恰 1 行、无脏写、无事件 | 是 | 否 | 正常接口建立互认配置后同步屏障并发提交 |
| V17 | 行集由互认配置驱动、金额 LEFT JOIN | Query | 行数等于该组织已配置标准项目数；未配置行 `IsAmountConfigured=false`、`CurrentAmount` 为 `null` | 是 | 否 | 同一组织多个互认配置，其中部分已保存金额 |
| V18 | 返回字段集合 | Query/Contract | 只返回设计字段；不含组织/医院/院区编码，不含最后修改时间与最后修改人；返回四个名称 | 否 | 否 | 已保存与未保存金额各一行 |
| V19 | 名称与归属读取次数与行数无关 | Application | 组织服务调用次数不随返回行数增长（计数替身断言） | 否 | 否 | 10 行与 100 行两组数据下的调用计数 |
| V20 | 排序与空结果 | Query | 标准项目编码升序；无互认配置时返回空集合且成功 | 是 | 否 | 同一组织多个不同编码配置；或无配置 |
| V21 | `UnavailableReason` 与 `ConfigurationStatus` | Query | 三层启用为 `null`；仅分类/仅分组/仅项目停用分别返回对应文案；多层时按分类→分组→项目取首条；配置自身停用不产生目录原因 | 是 | 否 | 逐层独立停用，其余两层自身启用 |
| V22 | 医院管理员查询越权院区 | Application/Query | 请求院区不属于可信医院时拒绝，不返回其他医院数据、不降级为空集合 | 否 | 否 | 可信医院；它医院院区编码 |
| V23 | DDL 列序、类型、注释、唯一索引 | Static/DB | 与设计一致；无 `varchar(n)`、无默认值、无跨系统外键 | 是 | 否 | 最终 DDL 与目标库 |
| V24 | SqlMap scope 与物理表映射 | Repository/Static | 仅访问 `mrec_` 表；scope 与调用点 `sqlId` 一致；不调用 `SetContext`；查询 SQL 无方言特征 | 是 | 否 | 目标表已由负责人建立 |
| V25 | 事件字段与失败不登记 | Domain | 事件含四个业务键与金额；`EventCreator`/`EventCreatedTime` 正确；校验失败、0 行、大于 1 行、唯一冲突均不登记 | 否 | 否 | 成功、0 行、并发冲突三类命令 |
| V26 | Request/ReadModel 契约 | Contract | 字段、可空性与设计一致：`CurrentAmount` 可空、无作用域编码、无最后修改字段、两个 Request 的字段集合正确 | 否 | 否 | 稳定契约 |
| V27 | 无 WorkUnit 生产入口的写入与失败响应 | Framework/Integration | 正常保存返回成功且回读一致；失败返回业务错误且无脏写；事件可见性子面记 `N/A` + `AcceptedRisk` | 是 | 否；真实无 WorkUnit 生产入口 | 实际 Host 入口、目标表与失败注入；不得用带 WorkUnit 探针替代 |
| V28 | 实体语义与注释修正 | Static/Domain | 实体注释与 UML1 实体、SRS F10 及同族 DTO 一致；写入路径无累加运算 | 否 | 否 | 修正后的实体与仓储 |
| V29 | 平台管理员入口跨组织保存与查询 | Application（宿主面降级） | 请求组织与可信上下文组织不同时，保存与查询仍按请求的组织、医院、院区执行；不因 token 组织不同而拒绝或静默过滤；返回的金额与名称属于该请求范围。**跨组织的真实宿主面降级（S3-D20）**：以应用层证据为准并记 `AcceptedRisk`，宿主面记 `Blocked` | 否 | 否 | 环境**无第二个组织**；应用层以替身构造第二组织路径。该降级同时意味着 **SRS F10 前置条件 2 中"按平台管理员授权选择组织"这一分支在本环境无法取得真实证据**，由该降级口径覆盖 |
| V30 | 医院管理员入口的院区范围 | Application | 组织与医院取可信上下文；请求院区属于可信医院时允许，属于其他医院即拒绝，不降级为空集合；不引入院区级数据权限校验 | 是 | 否 | 可信医院；本医院院区与它医院院区各一个 |
