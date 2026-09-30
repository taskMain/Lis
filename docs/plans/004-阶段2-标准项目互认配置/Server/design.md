# 阶段 2 后端设计

## 设计基线

聚合沿用 `MedicalRecognitionReportAggregate`。阶段2只新增互认项目配置能力，不提供真实删除、删除恢复、配置版本或分页；`StandardItemId` 不创建数据库外键。

当前代码事实：四个互认 Command、四个 DomainEvent、Manager、Repository 和 SqlMap 由 UML 生成并按本设计交付——仓储显式使用实体作用域 `MutualRecognitionItem`，SqlMap 与 SQL 指向 `mrec_` 前缀表 `mrec_mutual_recognition_item`，四个写方法与新增读取方法逐调用显式传 `scope`/`sqlId` 且不调用 `SetContext`（见"数据库、SQL 与 scope"节）。重开轮（2026-09-29，决策 S2-D16～S2-D20，见阶段根设计）在此基础上新增批量创建：`CreateMutualRecognitionItemsCommand` 与 `MutualRecognitionItemsCreatedEvent` 为受保护手工维护类型，不进 UML，形状以本文件为准。

高风险决策台账仅维护在阶段根设计：组织来源和选择传递链引用 S2-D12（`DesignConfirmed`），无WorkUnit事件机制引用 S2-D13（`DesignConfirmed`），目录原因优先级引用 S2-D14（分类 → 分组 → 项目），批量创建契约引用 S2-D19，UML 豁免引用 S2-D20（均为 `DesignConfirmed`）。本文件细化实现边界和V1-V33，不复制台账、不把决策状态记作测试结果。

## 公共契约与分层

创建 `CreateMutualRecognitionItemRequest` 只提交 `StandardProjectCode`、`RecognitionDurationDays`，不含 `OrganizationCode`；修改 Request 为 `Id`、`RecognitionDurationDays`，启停 Request 只有 `Id`。组织编码由可信组织上下文注入四个 Command，操作人由可信 `HttpRequestInfo.UserId` 解析为非空 Guid，无法解析或为 `Guid.Empty` 则拒绝，操作时间使用 `DateTimeOffset.UtcNow`。

批量创建 `CreateMutualRecognitionItemsRequest` 将公共属性上提：批次级唯一一份 `RecognitionDurationDays`（`[Range(1, int.MaxValue)]` 的 `int`，整批共用），元素为 `IReadOnlyList<string> StandardProjectCodes`，逐项只携带标准项目编码，不携带天数；契约同样不提交 `OrganizationCode` 与内部 ID。集合口径：非空（空集合拒绝）、单批最多 1000 条（上限防御，错误中明示）、元素非空白、批次内不允许重复编码（重复即整批拒绝并报告重复编码）。S2-D18 的"统一可互认时间"由此从页面约束上升为契约形状。

五个互认写入口（`CreateMutualRecognitionItemAsync`、`CreateMutualRecognitionItemsAsync`、`UpdateMutualRecognitionItemConfigurationAsync`、`EnableMutualRecognitionItemAsync`、`DisableMutualRecognitionItemAsync`）的返回与异常口径固定为：成功返回 `Task<bool>` 的 `true`，**不返回 `false`**；入参校验失败、可信组织或操作人不可解析、业务拒绝与影响行数不为预期值一律以异常结束（`ValidationException` 或 `InvalidOperationException`），调用方不需要按返回值判失败。批量创建的业务拒绝错误必须携带按原因分组的违规编码集合（空/超上限/批次内重复/目录无效/与库内配置重复），具体传输形状在实现时按框架错误契约落地，设计锁定"携带分组编码集合"这一事实；并发窗口内的唯一冲突使整批 INSERT 失败，数据库异常按原样向外传播，不翻译为业务拒绝、不逐码定位、不部分写入。启停入口在条件更新影响 0 行时按同一可信组织**复读一次**，复读结果分为三种：复读已是目标状态按**幂等成功**返回 `true` 且不登记事件；复读仍是反向状态按**并发冲突**拒绝、提示刷新后重试；复读读不到配置按**配置不存在**拒绝（与归属校验同一口径，不泄漏其他组织配置）。该三分支与后文"启停并发与归属"一节及 `MedicalRecognitionReportManager` 的复读口径一致，两个启停入口的 XML 文档即按此三出口描述。

组织上下文已取证：组织编码为 `String`，可信当前组织取自框架请求上下文的 `HttpRequestInfo.OrgId`（值来自 Bearer token 的 `org` claim）；页面所选组织经宿主登录入口选择 → 宿主签发 token → `Authorization: Bearer` → 框架 `WorkUnitEndpointFilter` 构造上下文到达服务端，业务请求不提交也不得覆盖该字段。证据见 [取证-组织与事件机制](取证-组织与事件机制.md)。授权组织集合在平台上没有契约来源（`IOrganizationAppService` 只提供全局或按入参查询，不判定调用方是否有权访问该组织），因此**跨组织判定采用"只信可信当前组织"口径**：服务端只接受与 `HttpRequestInfo.OrgId` 相等的组织，不等即拒绝；组织切换由宿主重新签发 token 完成，服务端不另建授权集合，也不重复判断操作权限（SRS 业务规则 298）。业务接口授权仍由权限系统负责，不重复建角色模型；非开发环境的最终合并配置与代理可用性另行核对，不复制开发地址作为生产配置。

静态配置核对（2026-09-14）：`server/Dy.MedicalRecognition/appsettings.Development.json` 已注册 `Dy.Base.Application.Contracts.OrganizationAggregate.IOrganizationAppService` 代理，基础 `appsettings.json` 的 `HttpServiceConfigs` 只有 Default。这只证明开发配置存在代理项，不证明该接口提供当前组织或授权集合。非开发环境须单独核对最终合并配置、已批准的服务目标及代理可用性；不复制开发地址作为生产配置。该次核对未修改这些配置、未访问组织服务；S2-D12 的"只信可信当前组织"口径已结案（`DesignConfirmed`，见阶段根设计），本项不阻断实现。

查询 Request 为 `OrganizationCode`（必填）、`StandardProjectCode`（可选）、`ConfigurationStatus`（可选）。查询组织编码必须等于服务端可信当前组织（`HttpRequestInfo.OrgId`），不等即为越权并拒绝，不降级为空集合。该入参由 UML2 明确声明，只作为**查询目标组织**存在，不作为可信范围声明：前端提交的是它从宿主认证上下文读到的当前组织，服务端一律以 token 推导的可信组织为准并拒绝不一致值，因此不违反 [frontend-api-client](../../../../.agents/instructions/frontend-api-client.md) 第 4 节"不得把可信范围交给调用方决定"的约束。ReadModel 字段为：`ConfigurationId`、`StandardProjectCode`、`StandardItemName`、`ItemType`、`ItemTypeText`、`CategoryName`、`GroupName`、`RecognitionDurationDays`、`ConfigurationStatus`、`ConfigurationStatusText`、可空 `UnavailableReason`；其中两个 `…Text` 是 2026-09-16 按 S2-D15 新增的服务端枚举中文（只读计算属性，`ItemTypeText` 在取值未登记时可为 null）；不返回创建/修改信息及已删除的两个派生状态字段。按标准项目编码升序排序；同一组织编码唯一，不设计同编码或相同 ID 的排序分支。阶段2不分页，登记为 `AcceptedRisk`。

分层职责固定为：Host 绑定 Request；Application 校验公共输入、解析可信组织和操作人、按编码读取当前有效标准项目并组装 Command；Domain Manager 校验目录规则、状态变化和事件登记；Repository 负责互认配置读写、目录查询端口实现、SQL、唯一约束和影响行数；Contracts 不引用 Domain Entity。Request、Command、ReadModel、Entity、DomainEvent 分离，编码到服务端目录 ID 是真实边界转换。

### 层目录与类型归属

下表路径均相对 `server/`，类型表中的目录别名以此为准。

| 概念层 | 实际目录与别名 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | C = `Dy.MedicalRecognition.Application.Contracts/` | 共享枚举、框架契约 | Application、Domain Entity、Repository |
| Application | A = `Dy.MedicalRecognition.Application/` | Contracts、Domain 接口和类型 | Repository 实现、SQL、DataMapper |
| Domain | D = `Dy.MedicalRecognition.Domain/` | Domain.Share、框架领域抽象 | Request、HTTP、Application、数据库实现 |
| Domain.Shared | S = `Dy.MedicalRecognition.Domain.Share/` | 框架事件与稳定共享类型 | Application、Repository |
| Repository/Infrastructure | R = `Dy.MedicalRecognition.Repository/` | Domain 仓储接口、内部投影、DataMapper | AppService 实现、领域状态决策 |
| Host | H = `Dy.MedicalRecognition/` | 契约、框架装配 | 业务状态机、直接业务 SQL |

目录中 `MedicalRecognitionReportAggregate/` 以下简称 `Aggregate/`。以下是目标类型，不表示生成物已经符合设计；新增与既有修改明确分开，不为一个实现增加 Manager 接口或独立互认查询服务接口。

| 具体类型 | 状态/角色 | 层/目录 | 调用方与职责 | 禁止职责 |
|---|---|---|---|---|
| `CreateMutualRecognitionItemRequest` | 修改 Request | C/ Aggregate/Requests | Host 绑定编码、正整数天数 | 接受组织、状态、内部目录ID |
| `CreateMutualRecognitionItemsRequest` | 重开轮新增 Request | C/ Aggregate/Requests | Host 绑定批次级正整数天数、标准项目编码集合 | 接受组织、内部目录ID、逐元素天数、超过 1000 条的批次 |
| `UpdateMutualRecognitionItemConfigurationRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID、正整数天数 | 改编码、组织、状态 |
| `EnableMutualRecognitionItemRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID | 接受可信组织 |
| `DisableMutualRecognitionItemRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID | 接受可信组织 |
| `RecognitionProjectConfigurationListQueryRequest` | 新增 Query Request | C/Queries | 查询服务输入组织、可选编码和状态 | 将组织输入直接当授权结论 |
| `RecognitionProjectConfigurationReadModel` | 修改并迁入 Queries | C/Queries；现位于 C/ReadModels | 查询服务返回列表契约 | 操作字段、两个已删除派生状态 |
| `IMedicalRecognitionReportAppService` | 重开轮扩展写契约 | C/Aggregate | Host 的五个写入口（含 `CreateMutualRecognitionItemsAsync`） | 通用查询 |
| `MedicalRecognitionReportAppService` | 修改 AppService | A/Aggregate | 输入校验、可信字段、编码转换、调用 Manager | 直接仓储写入 |
| `IMedicalRecognitionReportQueryAppService` | 扩展既有查询契约 | C/Queries | 增加 `QueryRecognitionProjectConfigurationListAsync` | 新建同义接口 |
| `MedicalRecognitionReportQueryAppService` | 扩展既有 QueryAppService | A/Queries | 组织范围校验、内部投影转 ReadModel | 写入或事件 |
| `TrustedOrganizationResolver` | 2026-09-16 整改新增（共享解析点） | A（`Application/TrustedOrganizationResolver.cs`） | 读取框架认证上下文中的可信组织编码：统一去除首尾空白、空白即拒绝，供五个写入口与查询入口共用；拒绝文案由调用方传入 | 授权判定、默认值或空串续用、从请求或存储构造组织 |
| `MedicalRecognitionEnumDescriptorRegistry` | 2026-09-16 立项新增（对外枚举契约） | A（`Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs`） | 集中登记对外枚举的 SourceGen 描述器（`MedicalItemType`、`ConfigurationStatus`），供 OpenAPI 转换器与后续元数据查询共用；新增对外枚举必须在此登记 | 承载业务逻辑、按运行时反射枚举取值 |
| `MedicalRecognitionEnumOpenApiDocumentTransformer` | 2026-09-16 立项新增（枚举契约修复） | A（`Application/MedicalRecognitionEnumOpenApiDocumentTransformer.cs`） | 文档转换器把登记枚举的取值、名称与中文说明写进 OpenAPI（`enum`、`x-enumNames`、`x-enumDescriptions`），使生成端把枚举按数值读写；未知架构跳过、不新增架构 | 改写未登记枚举、创建或删除架构、在服务端做中文展示转换 |
| `EnumDescriptorText` | 2026-09-16 立项新增（枚举中文来源，同 `Dy.LisCenter` 口径） | C（`Queries/EnumMetadata/EnumDescriptorText.cs`） | 按 SourceGen 描述列表把枚举值解析为服务端声明的中文：`Get` 严格解析（未定义值抛出 `ExtensionException`）、`GetOrNull` 安全解析（未定义值返回 `null`）；只读模型的中文文本属性统一由此解析 | 运行时反射枚举、页面展示口径（颜色、排序、动作文案） |
| `EnumMetadataItemDto` / `QueryEnumMetadataRequest` / `IEnumMetadataAppService` | 2026-09-16 立项新增（枚举元数据查询契约） | C（`Queries/EnumMetadata/`） | 对外交付白名单枚举的稳定数值、成员名称与中文说明；请求只带枚举名称 | 暴露未登记枚举、返回可变集合、承载业务逻辑 |
| `EnumMetadataAppService` | 2026-09-16 立项新增（枚举元数据查询） | A（`Application/Queries/EnumMetadata/EnumMetadataAppService.cs`） | 复用 `MedicalRecognitionEnumDescriptorRegistry` 的静态描述器，按枚举名称返回只读元数据；未登记或名称大小写不符时按请求校验拒绝 | 直接引用 `XxxDescriptorList`、运行时反射枚举、在应用层再写一份中文 |
| `CreateMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入可信组织、目录ID、编码、天数、操作字段 | HTTP、Request |
| `CreateMutualRecognitionItemsCommand` | 重开轮新增 Command | D/Aggregate/Commands | AppService 传入可信组织、编码集合、统一天数、操作字段；Manager 校验后展开为实体集合写入 | HTTP、Request、逐元素天数 |
| `UpdateMutualRecognitionItemConfigurationCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、天数、操作字段 | 修改配置归属 |
| `EnableMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、操作字段 | 接受调用方伪造的原状态 |
| `DisableMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、操作字段 | 接受调用方伪造的原状态 |
| `MutualRecognitionItem` | 核对 Entity | D/Aggregate | Manager 保存配置和操作字段 | 复制目录展示资料 |
| `MedicalRecognitionReportManager` | 修改既有 Manager | D/Aggregate | 目录有效性、归属、状态、批量整批原子校验、影响行数、事件登记 | 控制事务、公共 DTO |
| `MedicalStandardItemCatalogState` | 重开轮新增内部读投影 | D/Aggregate/MedicalStandardItemCatalogState.cs | 仓储按编码集合一次返回标准项目标识与所属分类、所属分组标识及三层启用状态，供 Manager 在内存中解析标识并逐编码判定目录有效性 | 充当公共契约、承载配置展示资料 |
| `IMedicalRecognitionReportRepository` | 重开轮扩展既有仓储接口 | D/Aggregate | AppService 单条目录读取、Manager 目录读取（单条与按编码集合批量）与配置读取（单条与按编码集合批量查重）和写入（单条） | Provider 类型 |
| `MedicalRecognitionReportRepository` | 修改既有仓储 | R/Aggregate | 实现上述接口、条件更新返回行数、并发冲突由唯一索引兜底 | 决定是否允许启用 |
| `IMedicalRecognitionReportQueryRepository` | 扩展既有查询仓储接口 | D/Queries | QueryAppService 获取组织过滤后的投影 | 公共 ReadModel、授权判断 |
| `MedicalRecognitionReportQueryRepository` | 扩展既有查询仓储 | R/Queries | SQL 关联、过滤、排序、返回内部投影 | 领域写入 |
| `RecognitionProjectConfigurationListItem` | 新增内部投影 | D/Queries/RecognitionProjectConfigurationListItem.cs | 仓储返回配置字段及目录三层状态，供 Application 映射 | 充当公共契约 |
| `MutualRecognitionItemDto` | 生成遗留 DTO | C/Aggregate/Dtos；仅被既有生成映射声明引用 | 不作为阶段2公共 Request/Response，不新增调用方 | 后续按生成器所有权决定删除或继续排除 |
| `MutualRecognitionItemCreatedEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已创建”事件 | Request、展示文本 |
| `MutualRecognitionItemsCreatedEvent` | 重开轮新增 Event | S/Aggregate/Events | Command 工厂创建“互认项目批量已创建”事件，携带本批全部创建项快照 | Request、展示文本、与单条事件混用 |
| `MutualRecognitionItemCreatedSnapshot` | 重开轮新增事件快照 | S/Aggregate/Events | 批量事件内嵌快照：各创建项 `Id`、`StandardProjectCode`、`RecognitionDurationDays`、`IsValid=true` | 充当公共 DTO、Request |
| `MutualRecognitionItemConfigurationUpdatedEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目配置已修改”事件 | Request、展示文本 |
| `MutualRecognitionItemEnabledEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已启用”事件 | 重复幂等事件 |
| `MutualRecognitionItemDisabledEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已停用”事件 | 重复幂等事件 |

`MedicalItemType`、`ConfigurationStatus` 复用 S/Enums 既有枚举，不新增替代枚举。A/Aggregate/`MedicalRecognitionReportDataMaps.cs` 维护既有映射声明，不新增映射服务。

两个枚举在 2026-09-16 按立项口径、参照同组织 `Dy.LisCenter` 的既有口径补为**对外契约枚举**：声明处加 `[EnumDescriptor]` 与每个成员的 `[Description]`（中文），A 层的 `MedicalRecognitionEnumOpenApiDocumentTransformer` 用 SourceGen 描述器为 OpenAPI 补齐 `enum`、`x-enumNames`、`x-enumDescriptions`；客户端生成入口再把 ASP.NET 的 `oneOf [null, $ref]` 归一为 `$ref`，使生成端把枚举属性按数值读写。枚举的取值与中文说明以服务端声明为唯一来源。

同日进一步确定：**枚举中文的对外交付方式与 `Dy.LisCenter` 完全同口径**，即服务端是中文的唯一来源，前端不再自持文案映射。覆盖范围为阶段2 的互认配置契约与生效目录的项目类型；阶段1 只读模型上的枚举（如 `MedicalStandardUsageStatus`）与阶段1 页面文案仍为前端本地实现，**不纳入本轮**（按残余登记，待阶段1 页面下次改动时一并设计）。落地为三条交付路径：

1. **契约文本字段**：只读模型按需暴露中文计算属性（本阶段为 `RecognitionProjectConfigurationReadModel.ItemTypeText` 与 `ConfigurationStatusText`），由 `EnumDescriptorText` 从 SourceGen 描述列表解析；页面直接展示该文本，前端本地文案表降级为接口不可用时的兜底。
2. **枚举元数据查询**：新增 `IEnumMetadataAppService.GetEnumMetadataAsync`，按白名单枚举名称返回 `{ Value, Name, Description }` 只读集合，供前端下拉选项取用（本阶段用于配置状态筛选）；未登记名称与大小写不符按请求校验拒绝，不降级为空集合。
3. **元数据与校验同源**：`EnumMetadataAppService` 与 OpenAPI 转换器共用 `MedicalRecognitionEnumDescriptorRegistry`，不各自引用 `XxxDescriptorList`，也不在应用层重写中文。

与服务端既有约定的两点差异，均为本阶段事实约束，不是口径偏离：

- `ConfigurationStatus` 由 `is_valid` 布尔派生，取值集合封闭，文本按**严格解析**（未定义值抛出），与 `Dy.LisCenter` 一致；
- `MedicalItemType` 来自 `mrec_medical_standard_category.item_type`，该列无 `CHECK` 约束，故文本按**安全解析**（未定义值返回 `null`）。页面既有约定是"未知取值安全展示、不默认成已知类型"，若沿用严格解析，一条越界数据会让整张列表查询失败，与既有约定冲突。该差异在 `EnumDescriptorText.GetOrNull` 的注释与本表登记。

契约形状的平台行为（增量复审登记，与 `Dy.LisCenter` 同构）：两个 `…Text` 是 **get-only 计算属性**，ASP.NET 生成的 OpenAPI 把二者声明为可空（`["null","string"]`，全文档无 `readOnly`），生成端因此是 `string | null` 的可写字段；C# 侧 `ConfigurationStatusText` 仍是非空声明。`RecognitionProjectConfigurationReadModel` 只作 200 响应体（契约中 5 处 `$ref` 全在 `responses` 下），当前不存在把计算文案写回请求的路径；若将来复用该模型作请求体，必须先处理该差异。

### 调用链与转换

写链：Host → `IMedicalRecognitionReportAppService` → `MedicalRecognitionReportAppService` → 五个 Command → `MedicalRecognitionReportManager` → `IMedicalRecognitionReportRepository` → `MedicalRecognitionReportRepository` / `MutualRecognitionItem.xml` → 写入成功后 Manager 调用 Command 事件工厂并 `AddEvent`。生产入口保留无 `[WorkUnit]` 路径，事件后续如何调度见下节待核实边界。

创建时 Application 通过既有仓储接口新增按编码读取能力 `GetMedicalStandardItemByCodeAsync`，把 Request 的 `StandardProjectCode` 解析为服务端 `StandardItemId`，编码仍保持同名；Manager 复用 `GetMedicalStandardItemByIdAsync`、`GetMedicalStandardCategoryByIdAsync`、`GetMedicalStandardGroupByIdAsync` 校验三层当前状态和关联。四个 Command 的 `OrganizationCode`、`OperId`、`OperTime` 均由 Application 注入。Manager 用新增 `GetMutualRecognitionItemByIdAsync(id, organizationCode)` 在可信组织范围读取；既有配置编码、目录ID从持久化配置取得，不由修改/启停请求补入，不另建目录服务接口。

批量创建时 `CreateMutualRecognitionItemsCommand` 只承载可信组织、标准项目编码集合、统一天数、操作字段与操作时间，不接受逐元素天数；编码到 `StandardItemId` 的解析由 Manager 在批量读取结果上完成，Application 不另行读取标准目录。Manager 批量创建路径：先校验集合口径（非空、≤1000、元素非空白、批次内无重复），再按编码集合**一次批量读取**标准项目及其分类、分组标识与三层启用状态（`GetMedicalStandardItemsByCodesAsync`，先对编码集合去重再读取，调用次数只随筛选维度增长、不随行数增长，见 [backend-architecture](../../../../.agents/instructions/backend-architecture.md) 第 5.3 节），在内存映射中同时完成逐编码 `StandardItemId` 解析与三层有效性校验（编码不存在按请求编码集合与返回结果的差集判定），收集**全部**违规编码并按原因分组（编码不存在、所属分类停用、所属分组停用、标准项目停用），随后用新增的 `GetMutualRecognitionItemsByCodesAsync(organizationCode, codes)` 一次性查重（含停用配置占用的唯一键）；任一命中即整批拒绝、零行写入、零事件。全部通过后 Manager 构造实体集合（逐项生成 `Id`、`is_valid=true`、绑定统一天数与操作字段），按提交顺序**逐条**插入（复用既有单条创建语句 `MutualRecognitionItem.CreateMutualRecognitionItem`，不新增语句、不新增注册键），累加各次影响行数；合计等于批次数才登记一次 `MutualRecognitionItemsCreatedEvent`；行数合计不符视为持久化不变量异常，不返回成功、不登记事件。整批全成全败由应用入口声明的工作单元事务保证。批量路径的目录读取、配置查重各一次，与验证矩阵 V33 一致。

查询链：Host → 既有 `IMedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync` → `MedicalRecognitionReportQueryAppService` 校验 Request 组织等于可信当前组织（两侧按去首尾空白比较，不另建授权组织集合） → 既有 `IMedicalRecognitionReportQueryRepository` 的同名方法 → `MedicalRecognitionReportQueryRepository` / `MedicalRecognitionReportQuery.xml` 在数据库按组织、可选编码和状态过滤并稳定排序 → `RecognitionProjectConfigurationListItem` → Application 映射 `RecognitionProjectConfigurationReadModel`。内部投影携带分类/分组/项目状态供派生原因，公共返回不泄漏这些内部字段；查询不得产生写入、审计事件或消息。

## Command、事件与事务

| Command/FacadeCommand | 主要 DomainEvent | 成功条件 | 关键事件字段 | 登记位置 | 失败/幂等语义 |
|---|---|---|---|---|---|
| `CreateMutualRecognitionItemCommand` | `MutualRecognitionItemCreatedEvent` | 三层目录有效且插入恰好 1 行 | `Id`、`OrganizationCode`、`StandardProjectCode`、`RecognitionDurationDays`、`IsValid=true` | `MedicalRecognitionReportManager` | 校验、写入失败或行数不为 1 不登记；重复配置由写入前查重拒绝，并发窗口内的唯一索引冲突由数据库拒绝并按原样向外传播 |
| `CreateMutualRecognitionItemsCommand` | `MutualRecognitionItemsCreatedEvent` | 全部编码通过集合口径、三层有效性与组织内查重，且逐条写入的影响行数合计等于批次数 | `OrganizationCode`、`Items` 快照（各 `Id`、`StandardProjectCode`、`RecognitionDurationDays`、`IsValid=true`）、`EventCreator`、`EventCreatedTime` | `MedicalRecognitionReportManager` | 任一预校验命中整批拒绝且不登记，错误携带按原因分组的违规编码集合；写入由工作单元事务保证整批全成全败，并发窗口内的唯一索引冲突使整批回滚、数据库异常按原样向外传播；行数合计 ≠ 批次数为持久化不变量异常 |
| `UpdateMutualRecognitionItemConfigurationCommand` | `MutualRecognitionItemConfigurationUpdatedEvent` | 组织范围内按 ID 更新恰好 1 行 | `Id`、`OrganizationCode`、`StandardProjectCode`、修改后 `RecognitionDurationDays` | `MedicalRecognitionReportManager` | 同值也保存并登记；0 行拒绝且不登记 |
| `EnableMutualRecognitionItemCommand` | `MutualRecognitionItemEnabledEvent` | 目录有效且条件更新恰好 1 行 | `Id`、`OrganizationCode`、`StandardProjectCode`、`IsValid=true` | `MedicalRecognitionReportManager` | 已是目标状态时幂等成功且不登记；反向竞争冲突拒绝 |
| `DisableMutualRecognitionItemCommand` | `MutualRecognitionItemDisabledEvent` | 条件更新恰好 1 行 | `Id`、`OrganizationCode`、`StandardProjectCode`、`IsValid=false` | `MedicalRecognitionReportManager` | 已是目标状态时幂等成功且不登记；反向竞争冲突拒绝 |

五个事件继承框架 `DomainEvent`，保留既有 `AggregateId = MedicalRecognitionReportConst.AggregateId`、`EventType`；业务配置ID使用 `Id`。事件时间/操作人不另造字段：实际 D/Aggregate/Commands 五个命令的 `Create...Event` 工厂已调用或按同口径调用 `MapTo...Event(eventCreator: OperId, eventCreatedTime: OperTime)`，即 `EventCreator ← OperId`、`EventCreatedTime ← OperTime`。这是源码映射证据，不是运行投递证据。修改/启停的组织与编码取 Manager 已校验归属的配置记录传给工厂，创建状态显式为 true；公共 Request 不参与事件封装。批量事件是与单条创建事实不同的事件类型，不与 `MutualRecognitionItemCreatedEvent` 共用，也不为每个创建项重复登记单条事件。

### 写用例事务表

| 写用例 | Application Service 入口 | 主要 Repository 数据库语句/操作 | 是否开启事务 | 框架机制 | 原子性依据 | 并发兜底 | DomainEvent 口径 | 外部调用及顺序 | 失败语义 | 验证方式 |
|---|---|---|---|---|---|---|---|---|---|---|
| 创建 | `CreateMutualRecognitionItemAsync` | 目录读取后 1 条 INSERT | 否 | 不加 `[WorkUnit]`；无显式事务，由框架 `EventBusActionUnitFilter` 在请求完成时统一 `FlushAsync` | 单条 INSERT 的语句级原子性 | 写入前按组织编码与标准项目编码查重；并发窗口内的唯一索引冲突由数据库拒绝并按原样向外传播 | INSERT 恰好 1 行才登记创建事件 | 本阶段无外部调用 | 校验、唯一冲突或写入失败均无成功事件 | V1-V4、V15-V16、V24 |
| 批量创建 | `CreateMutualRecognitionItemsAsync` | 集合口径校验后：1 次批量目录读取 + 1 次批量配置查重读取 + 按批次数逐条 INSERT | 是 | `[WorkUnit(UseTransaction = true)]`，与同文件新建分组、新建标准项目两个入口同形 | 框架工作单元事务：任一条写入失败即整批回滚 | 写入前按组织编码与编码集合批量查重；并发窗口内的唯一索引冲突使整批回滚并按原样向外传播，无部分写入 | 逐条写入的影响行数合计等于批次数才登记一次批量事件 | 本阶段无外部调用 | 任一预校验命中零行零事件，错误携带分组违规编码；行数合计数 ≠ 批次数为持久化不变量异常 | V28-V33、V24 |
| 修改 | `UpdateMutualRecognitionItemConfigurationAsync` | 组织范围读取后 1 条按 ID + 可信组织 UPDATE | 否 | 不加 `[WorkUnit]`；无显式事务，同上框架 Flush | 单条条件 UPDATE 的语句级原子性 | `ID + 可信组织` 定位并检查影响行数 | 恰好 1 行登记修改事件，同值仍保存并登记 | 本阶段无外部调用 | 0 行或异常拒绝，不伪造成功 | V5-V6、V11、V16、V24、V27 |
| 启用 | `EnableMutualRecognitionItemAsync` | 组织范围读取、必要目录校验后 1 条条件 UPDATE | 否 | 不加 `[WorkUnit]`；无显式事务，同上框架 Flush | 单条条件 UPDATE 的语句级原子性 | `ID + 可信组织 + 原状态`；检查 0/1/N 行 | 恰好 1 行登记启用事件，幂等不登记 | 本阶段无外部调用 | 幂等无事件；反向竞争冲突拒绝 | V7-V11、V16、V24、V26-V27 |
| 停用 | `DisableMutualRecognitionItemAsync` | 组织范围读取后 1 条条件 UPDATE | 否 | 不加 `[WorkUnit]`；无显式事务，同上框架 Flush | 单条条件 UPDATE 的语句级原子性 | `ID + 可信组织 + 原状态`；检查 0/1/N 行 | 恰好 1 行登记停用事件，幂等不登记 | 本阶段无外部调用 | 幂等无事件；反向竞争冲突拒绝 | V9-V11、V16、V24、V26-V27 |

事务决策遵循 S2-D6 的口径：单条写语句的用例依靠语句级原子性，不声明事务；批量创建一次请求要写入多条同一张表的记录且业务要求整批全成全败，因此按框架提供的工作单元声明事务边界（`[WorkUnit(UseTransaction = true)]`），与同文件新建分组、新建标准项目两个入口的既有写法一致，不自建事务、不使用手工事务 API。该结论只回答"本用例需要事务"，不把 S2-D6 的范围扩张成后续阶段通用规则；将来出现同表多条独立写语句的用例，仍须先修订设计再实现。目录停用不改写配置状态，查询/匹配仍检查当时的三层有效性。

组织代理契约尚未核实；确认需要外部调用时，复用 Host `HttpConfig.HttpServiceConfigs` 集中超时/重试机制，应用层不再包装或自动重试。外部校验失败不得进入写入、不得登记事件。

### 启停并发与归属

启用/停用先按 `Id + 可信 OrganizationCode` 读取目标配置，先验证归属再判断幂等；目标不存在或越组织统一拒绝，不泄漏其他组织配置。已是目标状态直接幂等，不更新操作字段、不登记事件，即使目录已停用也不把重复启用当新启用。真实启用才校验标准项目、分类、分组当前有效；创建始终默认启用并校验三层目录。

启用 UPDATE 条件为 `id = $Id AND organization_code = $OrganizationCode AND is_valid = false`，停用为相同ID/组织条件且 `is_valid = true`。原状态由 Manager 已读取状态决定，不接受客户端状态；只更新 `is_valid`、`oper_time`、`oper_id`。恰好1行才登记对应事件；0行按同一可信组织复读一次：不存在/越组织拒绝，复读已是目标状态则幂等成功无事件，仍为反向状态则返回并发冲突，提示刷新后重试，不自动无限重试。大于1行视为持久化不变量异常，不返回成功、不登记事件。

无交错反向操作时，并发同方向请求只有一条条件更新成功、只登记一次事件；不宣称仅靠布尔原状态能识别所有 ABA 交错，也不增加版本字段。本阶段反向并发只承诺单次条件更新和上述复读/冲突结果，不替用户循环重放意图。修改天数也必须按ID+可信组织读取和更新，越组织ID无论状态相同与否都拒绝；写入前按组织编码与标准项目编码查重命中即按重复配置拒绝，并发窗口内的唯一约束冲突由数据库拒绝并按原样向外传播。

### 无 WorkUnit 事件证据边界

事件登记、事件处理器观察、数据库提交、外部可靠投递是不同事实。已按实际 Host → 无 WorkUnit 生产 AppService → Manager → Repository 取证（见 [取证-组织与事件机制](取证-组织与事件机制.md)）：Flush 主体为框架 `EventBusActionUnitFilter.OnCompletedAsync` 调 `IEventQueue.FlushAsync()`，该过滤器经 `MapControllers()+AddEndpointFilter` 无条件挂载，与是否声明 `[WorkUnit]` 无关；时点在业务方法返回成功后、响应产出前；无 `[WorkUnit]` 时 `TransactionInfo == null`，不 Begin/Commit/Rollback，写入失败或处理器失败均不回滚，失败走 `Clear()`、队列已清空不可重试。以上为 `SourceConfirmed`。

B1取证已完成机制面；运行面仍须在真实库与真实入口下分别记录正常调用、写入失败时的数据库提交与异常传播；不得借新增带 `[WorkUnit]` 的探针入口替代，也不得手动 Flush、清理或重新发布事件队列以制造证据。框架自身的事务 Commit 失败路径若另行观察，必须标明不代表本阶段无 WorkUnit 生产路径。

V24 的运行面须在真实库与真实入口下分别记录正常调用、写入失败时的数据库提交与异常传播（**已于 2026-09-16 完成，见下方"运行面状态"**）。本项目零 `IEventHandler` 实现、无事件持久化表、无专用观察点，因此**事件可见性子面无观察点**，按 `.agents/instructions/dy-framework-workunit.md` 第 3 节处理；**数据库写入与失败响应仍须在 V24 验证**，不得因该子面无观察点而把整条 V24 记为通过，也不自动证明事件机制满足业务要求或解除 S2-D13 的运行面。若实际机制不能满足设计，先修订设计再实现，不自建事件总线或 Outbox。

> 运行面状态（2026-09-16）：表已建立、真实入口已验证，V24 的**数据库写入与失败响应面**实测通过（正常写入 `200` 并回读一致；重复写失败返回 `500` 且无脏写）。事件可见性子面无观察点，按 Testing Baseline §3 **整条记 `Blocked`**，受阻原因登记为决策标签（接受口径待负责人确认），不改状态、不计入统计。逐条证据见阶段 `testReport.md`。

## 数据库、SQL 与 scope

目标表为 `mrec_mutual_recognition_item`，表注释为“互认项目配置”。最终脚本位置为 `server/Dy.MedicalRecognition.Repository/Scripts/mutual_recognition_item.sql`，严格按以下列序交付；这里只定义DDL，不执行建表或迁移。重开轮批量创建不新增列、不新增索引、不改动唯一索引，复用既有 INSERT 语句（多行参数展开）。

| 列（按顺序） | PostgreSQL类型 | 可空 | DB默认值 | 中文列注释 / 值来源 |
|---|---|---|---|---|
| `id` | `uuid` | NOT NULL，主键 | 无 | 主键ID；Manager通过既有仓储生成ID |
| `organization_code` | `text` | NOT NULL | 无 | 组织编码；可信组织上下文 |
| `standard_item_id` | `uuid` | NOT NULL | 无 | 标准医疗项目ID；服务端按编码解析 |
| `standard_project_code` | `text` | NOT NULL | 无 | 标准项目编码；经校验的目录编码 |
| `recognition_duration_days` | `integer` | NOT NULL | 无 | 可互认时间天数；请求正整数 |
| `is_valid` | `boolean` | NOT NULL | 无 | 是否启用；创建显式true，启停命令变更 |
| `oper_time` | `timestamptz` | NOT NULL | 无 | 操作时间；Command.OperTime |
| `oper_id` | `uuid` | NOT NULL | 无 | 操作人；Command.OperId |

字符串没有已确认业务长度上限，使用 PostgreSQL `text`，不添加 `varchar(n)`、Request最大长度或截断规则；物理设计不代表新增业务上限，也不承诺数据库无限容量。全部列无数据库默认值，INSERT/UPDATE显式绑定操作时间，不使用 `current_timestamp` 代替 Command 时间。保留业务正整数校验，不因物理类型擅自引入新的业务范围。

唯一索引固定为 `ux_mrec_mutual_recognition_org_project`，覆盖 `(organization_code, standard_project_code)`，无状态过滤，停用配置也占用唯一键；索引注释为“同一组织标准项目互认配置唯一（含停用配置）”。脚本包含 `COMMENT ON TABLE`、每列 `COMMENT ON COLUMN` 和 `COMMENT ON INDEX`。不创建 `StandardItemId` 外键，不迁移或触碰其他系统对象；目标数据库/schema 确认并执行最终 DDL 后，才继续依赖新表的集成验证。

SqlMap 的 `Scope` 和每个 DataMapper 调用点统一改为 `MutualRecognitionItem`，不调用 `SetContext`；SQL 显式指向 `mrec_mutual_recognition_item`。每个手写 Statement 补业务功能 XML 注释；SELECT、INSERT、UPDATE 字段补中文注释，复杂 CASE/JOIN/条件补处理、原因、结果注释。作用域查找键/注册键已由 `Stage1SqlMapProbeTests` 的不连库探针核实并在测试中断言（注册面）；运行面见宿主证据（阶段 `testReport.md` 的宿主验收与"结果"节）。批量写入复用 `MutualRecognitionItem.CreateMutualRecognitionItem` 注册键逐条插入，不新增 Statement；批量读取（目录按编码集合、配置按组织+编码集合）各新增一个 Statement，同样补功能与字段注释。

目录停用原因采用根设计 S2-D14 固定的规则，只处理停用，不处理删除；按“分类 → 分组 → 项目”优先级返回第一条原因，具体文案依次为“所属分类已停用”“所属分组已停用”“标准项目已停用”。全部目录有效时 `UnavailableReason = null`；配置自身停用只由 `ConfigurationStatus` 表达，不能抢占目录原因。查询使用配置组织过滤后关联全局标准目录，不复制名称、类型、分类或分组；上级恢复时实时重算，不改写配置自身状态。目录资料缺失不是正常业务可达状态，不建立测试夹具或直接写库构造该数据。

## 生成维护边界

后续实施采用受保护的手工维护，不依赖重新生成全后端：设计确认后可修改 Request、ReadModel、Command、Event、Entity、DataMap、Repository接口/实现、XML、DDL，并实现 Application、Manager、查询投影和Host接入。创建Request从Command镜像生成造成的组织字段差异，在该Request手工移除并列入保护清单；UML中的Command保留组织字段。

生成文件与人工维护文件的边界按项目仓库规范处理；本阶段只要求最终公共契约、SQL映射和数据库脚本保持一致，`MutualRecognitionItemDto` 不作为阶段2公共契约。重开轮新增的批量 Request、批量 Command、批量事件与事件快照为手工维护类型，不进 UML（S2-D20），与既有四个 UML 生成命令并存；后续若重新生成后端，必须把批量类型列入保护清单，防止生成器覆盖。

## 验证矩阵

V1-V27 编号保留，V26-V27补充并发和跨组织写入；V11验证命令可信组织，V19验证查询组织授权；V13验证单层停用，V20验证多层停用优先级，不合并或挪号。重开轮新增 V28-V33（批量创建），沿用同一编号空间。

### 测试数据边界

所有业务数据和状态只通过正常业务接口（公开 AppService/API）或页面形成：先创建分类、分组、标准项目和互认配置，再通过独立启停、修改和并发请求形成适用状态。禁止直接执行 SQL、数据库工具、脚本或测试夹具写库，禁止伪造重复主键、孤儿关联、删除后残留或其他违反业务约束的数据。

不可由正常业务流程形成的数据状态不作为普通业务用例前置。请求边界的防御性校验可以使用无持久化数据的输入验证，但不得借此制造数据库脏数据。并发、唯一约束和跨组织验证使用真实接口或页面建立合法前置数据后再发起请求。

真实数据库验证需目标数据库/schema 已确认、阶段1目录三表可用、阶段2最终DDL执行完成，且scope实际查找键/注册键取证完成；身份/组织验证需真实宿主身份与已取证的选中组织传递链（`HttpRequestInfo.OrgId`）。无上述证据的受影响运行项为 `Blocked`，其余未执行项为 `NotRun`，不把决策标签当测试状态。

V16单元验证事件构造/登记规则，不能替代V24真实生产调度；V26要取得事件观察/计数证据，不能仅凭最终状态推断一次事件。表内“真实WorkUnit：否”表示按设计无WorkUnit生产入口验证，不表示事件机制已证实。V18/V25 的静态检查与建表完成后的真实库只读元数据均已核实；本矩阵的真实运行面、受阻项与登记口径见阶段 `testReport.md` 的"结果"与"门禁终值"节。批量创建的单批上限 1000 是防御性约束，用边界值（0、1、1000、1001）与代表批量验证，不构造超过真实目录规模的超大夹具。

| 编号 | 用例与边界 | 层级 | 预期 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| V1 | 新增有效配置 | Domain/Application | 成功、默认启用、登记创建事件 | 否 | 否 | 授权组织；三层目录启用 |
| V2 | 重复组织+项目编码 | Domain/Repository | 业务拒绝，唯一约束兜底 | 是 | 否 | 同一组织通过正常接口先创建配置，再由第二个正常请求重复提交 |
| V3 | 目录项目/分类/分组停用新增 | Domain | 拒绝且无事件 | 否 | 否 | 对应层停用 |
| V4 | 时间为空、0、负数；小数与"永久有效"在 `int` 契约下不可构造 | Contract/Domain | 空、0 与负数拒绝（`CreateMutualRecognitionItemRequest.RecognitionDurationDays` 为 `[Range(1, int.MaxValue)]` 的 `int`）；下界 `1` 与上界 `int.MaxValue` 经写链路可达；小数与"永久有效"记 `N/A`（整型字段提交不了小数、没有表示"永久有效"的专用取值，不伪造数据构造该前置） | 否 | 否 | 非法输入；`N/A` 项不构造前置 |
| V5 | 修改时间及同值保存 | Domain/Repository | 成功、登记修改事件 | 是 | 否 | 已有配置 |
| V6 | 停用后修改时间 | Domain | 成功，不改变状态 | 否 | 否 | 配置已停用 |
| V7 | 启用有效配置 | Domain/Repository | 状态改变并登记事件 | 是 | 否 | 配置停用；三层目录启用 |
| V8 | 启用无效目录配置 | Domain | 拒绝，无状态变化 | 否 | 否 | 配置停用；目录层停用 |
| V9 | 重复启用/停用 | Domain | 幂等成功，不登记事件 | 否 | 否 | 目标已是目标状态 |
| V10 | 不存在配置 ID | N/A | 不属于正常业务可达数据，不建立测试数据或直接写库构造；测试报告记 `N/A` | 否 | 否 | 无 |
| V11 | 命令可信组织注入与伪造组织输入 | Application/Contract | 创建Request无组织；五个Command只用可信组织，不接受伪造字段覆盖 | 否 | 否 | 可信组织A；伪造组织B输入；已核实组织解析契约 |
| V12 | 配置列表组织过滤与目录关联 | Query | 只返回当前组织，资料实时关联 | 是 | 否 | 两个组织；有效目录 |
| V13 | `UnavailableReason` 空值与单层停用原因 | Query | 三层启用为null；仅分类/仅分组/仅项目停用时分别返回对应具体文案；配置自身停用不产生目录原因 | 是 | 否 | 有效目录与配置；分别独立停用一层，其余两层自身状态启用 |
| V14 | 查询排序和空结果 | Query | 标准项目编码升序，空集合成功；不构造同编码或相同 ID 数据测试附加排序 | 是 | 否 | 同一组织通过正常接口创建多个不同编码配置，或无匹配配置 |
| V15 | 数据库唯一约束冲突 | Repository/Application | 数据库唯一索引拒绝，异常按原样向外传播，不部分写入、不登记事件 | 是 | 否 | 两个正常创建请求并发提交同一组织、同一标准项目编码；不直接写库预置冲突行 |
| V16 | 事件字段和失败不登记事件 | Domain | 五事件字段及EventCreator/EventCreatedTime正确；创建IsValid=true；批量事件Items快照完整；修改含组织/编码；0行或异常不登记 | 否 | 否 | 成功、0行、异常、幂等命令；可信操作字段与已加载配置记录 |
| V17 | SQL物理表映射 | Repository | 仅访问 `mrec_` 表 | 是 | 否 | 目标表已建立 |
| V18 | DDL列序、类型、注释、索引 | Static/DB | 与设计一致 | 是 | 否 | 最终 DDL |
| V19 | 查询越权组织 | Application/Query | 请求组织不等于可信当前组织时拒绝，不返回其他组织数据、不降级为空集合 | 否 | 否 | 已取证的 `HttpRequestInfo.OrgId`；与请求组织不同的组织编码 |
| V20 | 多层目录停用原因优先级 | Query | 分类优先于分组，分组优先于项目；恢复上层后显露下一原因，全恢复为null | 是 | 否 | 配置先正常创建；分类+分组、分类+项目、分组+项目及三层同时停用，随后逐层恢复 |
| V21 | Request/ReadModel 契约 | Contract | 字段、枚举、可空性与设计一致（含批量 Request 的天数上提与编码集合形状） | 否 | 否 | 稳定契约 |
| V22 | 可信组织与操作人来源、组织选择传递 | Application/Host | 选中组织到服务端取值链可追溯（token `org` claim → `HttpRequestInfo.OrgId`）；缺失/越权组织及无法解析/空Guid操作人拒绝 | 否 | 否 | 实际宿主身份与真实 token；组织编码来源与传递链已取证，运行面待验 |
| V23 | SqlMap scope 查找键 | Repository/Static | 实际查找键与注册键一致（批量写入复用既有 `CreateMutualRecognitionItem` 键逐条插入，不新增键） | 是 | 否 | 作用域探测结果 |
| V24 | 无WorkUnit生产入口事件调度与提交结果 | Framework/Integration | 正常与写失败分别记录登记/数据库提交/异常结果；事件可见性子面无观察点，须按 Testing Baseline §3 处理且不得整条记为通过（执行状态见"无 WorkUnit 事件证据边界"一节的"运行面状态"说明） | 是 | 否；真实无WorkUnit生产入口 | 实际Host入口与失败注入；机制面已 `SourceConfirmed`，运行面（写入与失败响应）已实测通过；不得用带WorkUnit探针替代 |
| V25 | DDL和SQL注释/短索引 | Static/DB | 注释、列序、类型、唯一索引符合设计 | 是 | 否 | 最终脚本和目标库 |
| V26 | 并发同方向启用/停用及0行复读 | Domain/Repository/Integration | 无交错反向操作时仅1行状态变化、只登记一次事件，其余幂等；补反向竞争0行复读仍反向时冲突且无自动无限重试 | 是 | 否 | 通过正常接口创建配置，再经正常启停接口形成目标反向状态；启用时三层有效；同步屏障并发请求；V24观察点；反向竞争分独立场景 |
| V27 | 跨组织配置ID写入拒绝 | Application/Domain/Repository | 组织A修改/启用/停用组织B配置均拒绝，含已是目标状态；B配置、操作字段不变且无事件 | 是 | 否 | A、B均通过正常接口形成各自配置；A请求引用B的配置ID，仅验证资源范围边界，不构造异常数据库数据 |
| V28 | 批量新增成功 | Domain/Repository/Application | 全部编码合法时按提交顺序逐条写入、影响行数合计等于批次数、全部默认启用、登记一次 `MutualRecognitionItemsCreatedEvent`，事件 Items 快照含各编码、天数与 IsValid=true | 是 | 否 | 授权组织；三层目录启用；批次含跨分类、跨分组编码 |
| V29 | 批量入参口径 | Contract/Domain | 空集合、单批超 1000、元素空白、批次内重复编码均整批拒绝并报告对应违规编码；不进入目录校验与写入 | 否 | 否 | 非法输入；边界值 0、1、1000、1001 |
| V30 | 批量目录有效性逐码校验 | Domain | 混合合法与停用层编码时收集全部违规编码、按原因分组（编码不存在/分类停用/分组停用/项目停用）随错误返回，零行写入、零事件 | 否 | 否 | 对应层独立停用的目录数据 |
| V31 | 批量与库内配置查重 | Domain/Repository | 选中集合含已配置编码（含停用配置占用唯一键）即整批拒绝并报告编码，零行写入、零事件 | 是 | 否 | 同组织经正常接口先创建部分配置，再批量提交含相同编码的批次 |
| V32 | 批量并发唯一冲突 | Repository/Application | 预校验通过后另一请求抢先创建同编码配置，本批写入因唯一索引整批回滚（工作单元事务），数据库异常按原样向外传播，无脏写、无部分写入、不登记事件 | 是 | 否 | 两请求并发提交同组织重叠编码批次；不直接写库预置冲突行 |
| V33 | 批量读取无 N+1 | Application/Domain/Repository | 目录按编码集合、配置按组织+编码集合各一次读取完成校验与查重，调用次数不随行数增长 | 否 | 否 | 计数替身断言批量读取方法调用次数 |