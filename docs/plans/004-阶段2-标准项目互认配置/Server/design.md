# 阶段 2 后端设计

## 设计基线

聚合沿用 `MedicalRecognitionReportAggregate`。阶段2只新增互认项目配置能力，不提供真实删除、删除恢复、配置版本或分页；`StandardItemId` 不创建数据库外键。

当前代码事实：四个互认 Command、四个 DomainEvent、Manager、Repository 和 SqlMap 已由 UML 生成，现有 Repository 使用 `MedicalRecognitionReport` 作用域，SqlMap 与 SQL 使用无 `mrec_` 前缀表名；这些是待实施整改项，不是已验证结论。

高风险决策台账仅维护在阶段根设计：组织来源和选择传递链引用 S2-D12（Pending），无WorkUnit事件机制引用 S2-D13（Pending），目录原因优先级引用 S2-D14（分类 → 分组 → 项目）。本文件细化实现边界和V1-V27，不复制台账、不将Pending记作测试结果。

## 公共契约与分层

创建 `CreateMutualRecognitionItemRequest` 只提交 `StandardProjectCode`、`RecognitionDurationDays`，不含 `OrganizationCode`；修改 Request 为 `Id`、`RecognitionDurationDays`，启停 Request 只有 `Id`。组织编码由可信组织上下文注入四个 Command，操作人由可信 `HttpRequestInfo.UserId` 解析为非空 Guid，无法解析或为 `Guid.Empty` 则拒绝，操作时间使用 `DateTimeOffset.UtcNow`。

组织上下文具体服务接口、字段、授权组织集合，以及页面组织选择如何经宿主到达服务端并成为可信当前组织，均为 `Pending`。不能因为创建 Request 不含组织就假定组织选择已传递，也不能猜测 claim、Header、SDK 消息或会话字段。实施前须核对组织服务代理、同类 AppService 和实际宿主请求，记录选中组织的传输、认证、授权校验、服务端取值及缺失/越权拒绝链路。仅阻断依赖这些事实的组织解析与接入实现，不阻断独立契约、DDL设计及静态检查；业务接口授权仍由权限系统负责，不重复建角色模型。

静态配置核对（2026-09-14）：`server/Dy.MedicalRecognition/appsettings.Development.json` 已注册 `Dy.Base.Application.Contracts.OrganizationAggregate.IOrganizationAppService` 代理，基础 `appsettings.json` 的 `HttpServiceConfigs` 只有 Default。这只证明开发配置存在代理项，不证明该接口提供当前组织或授权集合。非开发环境须单独核对最终合并配置、已批准的服务目标及代理可用性；不复制开发地址作为生产配置。本轮不修改这些配置或访问组织服务，S2-D12保持Pending。

查询 Request 为 `OrganizationCode`（必填）、`StandardProjectCode`（可选）、`ConfigurationStatus`（可选）。查询组织编码必须属于当前账号授权组织范围，越权拒绝，不降级为空集合。ReadModel 字段为：`ConfigurationId`、`StandardProjectCode`、`StandardItemName`、`ItemType`、`CategoryName`、`GroupName`、`RecognitionDurationDays`、`ConfigurationStatus`、可空 `UnavailableReason`；不返回创建/修改信息及已删除的两个派生状态字段。按标准项目编码升序排序；同一组织编码唯一，不设计同编码或相同 ID 的排序分支。阶段2不分页，登记为 `AcceptedRisk`。

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
| `UpdateMutualRecognitionItemConfigurationRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID、正整数天数 | 改编码、组织、状态 |
| `EnableMutualRecognitionItemRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID | 接受可信组织 |
| `DisableMutualRecognitionItemRequest` | 核对 Request | C/ Aggregate/Requests | Host 绑定ID | 接受可信组织 |
| `RecognitionProjectConfigurationListQueryRequest` | 新增 Query Request | C/Queries | 查询服务输入组织、可选编码和状态 | 将组织输入直接当授权结论 |
| `RecognitionProjectConfigurationReadModel` | 修改并迁入 Queries | C/Queries；现位于 C/ReadModels | 查询服务返回列表契约 | 操作字段、两个已删除派生状态 |
| `IMedicalRecognitionReportAppService` | 核对既有写契约 | C/Aggregate | Host 的四个写入口 | 通用查询 |
| `MedicalRecognitionReportAppService` | 修改 AppService | A/Aggregate | 输入校验、可信字段、编码转换、调用 Manager | 直接仓储写入 |
| `IMedicalRecognitionReportQueryAppService` | 扩展既有查询契约 | C/Queries | 增加 `QueryRecognitionProjectConfigurationListAsync` | 新建同义接口 |
| `MedicalRecognitionReportQueryAppService` | 扩展既有 QueryAppService | A/Queries | 组织范围校验、内部投影转 ReadModel | 写入或事件 |
| `CreateMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入可信组织、目录ID、编码、天数、操作字段 | HTTP、Request |
| `UpdateMutualRecognitionItemConfigurationCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、天数、操作字段 | 修改配置归属 |
| `EnableMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、操作字段 | 接受调用方伪造的原状态 |
| `DisableMutualRecognitionItemCommand` | 修改 Command | D/Aggregate/Commands | AppService 传入ID、可信组织、操作字段 | 接受调用方伪造的原状态 |
| `MutualRecognitionItem` | 核对 Entity | D/Aggregate | Manager 保存配置和操作字段 | 复制目录展示资料 |
| `MedicalRecognitionReportManager` | 修改既有 Manager | D/Aggregate | 目录有效性、归属、状态、影响行数、事件登记 | 控制事务、公共 DTO |
| `IMedicalRecognitionReportRepository` | 扩展既有仓储接口 | D/Aggregate | AppService 必要目录读取、Manager 配置读取和写入 | Provider 类型 |
| `MedicalRecognitionReportRepository` | 修改既有仓储 | R/Aggregate | 实现上述接口、条件更新返回行数、唯一冲突识别 | 决定是否允许启用 |
| `IMedicalRecognitionReportQueryRepository` | 扩展既有查询仓储接口 | D/Queries | QueryAppService 获取组织过滤后的投影 | 公共 ReadModel、授权判断 |
| `MedicalRecognitionReportQueryRepository` | 扩展既有查询仓储 | R/Queries | SQL 关联、过滤、排序、返回内部投影 | 领域写入 |
| `RecognitionProjectConfigurationListItem` | 新增内部投影 | D/Queries/RecognitionProjectConfigurationListItem.cs | 仓储返回配置字段及目录三层状态，供 Application 映射 | 充当公共契约 |
| `MutualRecognitionItemDto` | 生成遗留 DTO | C/Aggregate/Dtos；仅被既有生成映射声明引用 | 不作为阶段2公共 Request/Response，不新增调用方 | 后续按生成器所有权决定删除或继续排除 |
| `MutualRecognitionItemCreatedEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已创建”事件 | Request、展示文本 |
| `MutualRecognitionItemConfigurationUpdatedEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目配置已修改”事件 | Request、展示文本 |
| `MutualRecognitionItemEnabledEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已启用”事件 | 重复幂等事件 |
| `MutualRecognitionItemDisabledEvent` | 修改 Event | S/Aggregate/Events | Command 工厂创建“互认项目已停用”事件 | 重复幂等事件 |

`MedicalItemType`、`ConfigurationStatus` 复用 S/Enums 既有枚举，不新增替代枚举。A/Aggregate/`MedicalRecognitionReportDataMaps.cs` 维护既有映射声明，不新增映射服务。

### 调用链与转换

写链：Host → `IMedicalRecognitionReportAppService` → `MedicalRecognitionReportAppService` → 四个 Command → `MedicalRecognitionReportManager` → `IMedicalRecognitionReportRepository` → `MedicalRecognitionReportRepository` / `MutualRecognitionItem.xml` → 写入成功后 Manager 调用 Command 事件工厂并 `AddEvent`。生产入口保留无 `[WorkUnit]` 路径，事件后续如何调度见下节待核实边界。

创建时 Application 通过既有仓储接口新增按编码读取能力 `GetMedicalStandardItemByCodeAsync`，把 Request 的 `StandardProjectCode` 解析为服务端 `StandardItemId`，编码仍保持同名；Manager 复用 `GetMedicalStandardItemByIdAsync`、`GetMedicalStandardCategoryByIdAsync`、`GetMedicalStandardGroupByIdAsync` 校验三层当前状态和关联。四个 Command 的 `OrganizationCode`、`OperId`、`OperTime` 均由 Application 注入。Manager 用新增 `GetMutualRecognitionItemByIdAsync(id, organizationCode)` 在可信组织范围读取；既有配置编码、目录ID从持久化配置取得，不由修改/启停请求补入，不另建目录服务接口。

查询链：Host → 既有 `IMedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync` → `MedicalRecognitionReportQueryAppService` 校验 Request 组织属于授权集合 → 既有 `IMedicalRecognitionReportQueryRepository` 的同名方法 → `MedicalRecognitionReportQueryRepository` / `MedicalRecognitionReportQuery.xml` 在数据库按组织、可选编码和状态过滤并稳定排序 → `RecognitionProjectConfigurationListItem` → Application 映射 `RecognitionProjectConfigurationReadModel`。内部投影携带分类/分组/项目状态供派生原因，公共返回不泄漏这些内部字段；查询不得产生写入、审计事件或消息。

## Command、事件与事务

| Command | 主要事件 | 事件最小字段 | 登记规则 |
|---|---|---|---|
| `CreateMutualRecognitionItemCommand` | `MutualRecognitionItemCreatedEvent` | `Id`、`OrganizationCode`、`StandardProjectCode`、`RecognitionDurationDays`、`IsValid=true` | 三层目录有效且插入恰好1行后由 Manager 登记；失败不登记 |
| `UpdateMutualRecognitionItemConfigurationCommand` | `MutualRecognitionItemConfigurationUpdatedEvent` | `Id`、`OrganizationCode`、`StandardProjectCode`、修改后 `RecognitionDurationDays` | 组织内配置更新恰好1行后由 Manager 登记；同值也保存并登记；失败不登记 |
| `EnableMutualRecognitionItemCommand` | `MutualRecognitionItemEnabledEvent` | `Id`、`OrganizationCode`、`StandardProjectCode`、`IsValid=true` | 目录有效且条件更新恰好1行后由 Manager 登记；幂等不登记 |
| `DisableMutualRecognitionItemCommand` | `MutualRecognitionItemDisabledEvent` | `Id`、`OrganizationCode`、`StandardProjectCode`、`IsValid=false` | 条件更新恰好1行后由 Manager 登记；幂等不登记 |

四个事件继承框架 `DomainEvent`，保留既有 `AggregateId = MedicalRecognitionReportConst.AggregateId`、`EventType`；业务配置ID使用 `Id`。事件时间/操作人不另造字段：实际 D/Aggregate/Commands 四个命令的 `Create...Event` 工厂已调用 `MapTo...Event(eventCreator: OperId, eventCreatedTime: OperTime)`，即 `EventCreator ← OperId`、`EventCreatedTime ← OperTime`。这是源码映射证据，不是运行投递证据。修改/启停的组织与编码取 Manager 已校验归属的配置记录传给工厂，创建状态显式为 true；公共 Request 不参与事件封装。

### 写用例事务表

| 写用例 / AppService公开入口 | Repository数据库操作 | 显式事务/框架机制 | 原子性与并发兜底 | 事件口径 | 外部调用顺序 | 失败语义 / 验证 |
|---|---|---|---|---|---|---|
| 创建 / `CreateMutualRecognitionItemAsync` | 目录读取后1条 INSERT | 否；不加 `[WorkUnit]` | 语句级原子性；组织+编码唯一索引 | INSERT恰好1行才登记创建事件 | 如组织校验需服务代理，在第一次Repository访问前完成 | 校验/唯一冲突/写入失败无成功事件；V1-V4、V15-V16、V24 |
| 修改 / `UpdateMutualRecognitionItemConfigurationAsync` | 组织范围读取后1条按ID+可信组织 UPDATE | 否；不加 `[WorkUnit]` | 语句级原子性；保留归属、编码、目录ID和状态 | 恰好1行登记修改事件，同值仍保存 | 同上 | 0行拒绝，不伪造成功；V5-V6、V11、V16、V24、V27 |
| 启用 / `EnableMutualRecognitionItemAsync` | 组织范围读取、必要目录校验后1条条件 UPDATE | 否；不加 `[WorkUnit]` | ID+可信组织+原状态；检查0/1/N行 | 恰好1行登记启用事件 | 同上 | 幂等无事件；反向竞争冲突拒绝；V7-V11、V16、V24、V26-V27 |
| 停用 / `DisableMutualRecognitionItemAsync` | 组织范围读取后1条条件 UPDATE | 否；不加 `[WorkUnit]` | ID+可信组织+原状态；检查0/1/N行 | 恰好1行登记停用事件 | 同上 | 幂等无事件；反向竞争冲突拒绝；V9-V11、V16、V24、V26-V27 |

事务决策遵循 S2-D6 的负责人明确要求。本阶段四个用例各最多一条写语句，不锁定目录，依靠单语句原子性，不为事件增加 WorkUnit。通用规范要求同表多写全成全败时也考虑事务，与负责人“仅跨表或外观命令使用”的范围不同；本阶段不涉及该分歧，不将其扩张成后续阶段通用规则。若将来出现同表多写，先交负责人修订设计，不能自行套用任一口径。目录停用不改写配置状态，查询/匹配仍检查当时的三层有效性。

组织代理契约尚未核实；确认需要外部调用时，复用 Host `HttpConfig.HttpServiceConfigs` 集中超时/重试机制，应用层不再包装或自动重试。外部校验失败不得进入写入、不得登记事件。

### 启停并发与归属

启用/停用先按 `Id + 可信 OrganizationCode` 读取目标配置，先验证归属再判断幂等；目标不存在或越组织统一拒绝，不泄漏其他组织配置。已是目标状态直接幂等，不更新操作字段、不登记事件，即使目录已停用也不把重复启用当新启用。真实启用才校验标准项目、分类、分组当前有效；创建始终默认启用并校验三层目录。

启用 UPDATE 条件为 `id = $Id AND organization_code = $OrganizationCode AND is_valid = false`，停用为相同ID/组织条件且 `is_valid = true`。原状态由 Manager 已读取状态决定，不接受客户端状态；只更新 `is_valid`、`oper_time`、`oper_id`。恰好1行才登记对应事件；0行按同一可信组织复读一次：不存在/越组织拒绝，复读已是目标状态则幂等成功无事件，仍为反向状态则返回并发冲突，提示刷新后重试，不自动无限重试。大于1行视为持久化不变量异常，不返回成功、不登记事件。

无交错反向操作时，并发同方向请求只有一条条件更新成功、只登记一次事件；不宣称仅靠布尔原状态能识别所有 ABA 交错，也不增加版本字段。本阶段反向并发只承诺单次条件更新和上述复读/冲突结果，不替用户循环重放意图。修改天数也必须按ID+可信组织读取和更新，越组织ID无论状态相同与否都拒绝；唯一约束冲突统一翻译为重复配置业务错误。

### 无 WorkUnit 事件证据边界

事件登记、事件处理器观察、数据库提交、外部可靠投递是不同事实。当前代码可确认无 `[WorkUnit]` 的四个生产 AppService 方法和 Manager 的 `AddEvent`，不能据此确认无 WorkUnit 时队列由谁 Flush、处理时点或可靠投递。此映射为 `Pending`，只阻断依赖该机制的写入/事件接入实现与验收，不阻断独立查询、契约及DDL静态工作。

B1必须沿实际 Host → 无 WorkUnit 生产 AppService → Manager → Repository 取证，分别记录正常调用、写入失败、事件处理失败时的数据库提交与事件可见性、异常传播、是否可重试；没有显式事务时不得声称事件失败会回滚已提交写入。不得借新增带 `[WorkUnit]` 的探针入口替代，也不得手动 Flush、清理或重新发布事件队列以制造证据。框架自身的事务 Commit 失败路径若另行观察，必须标明不代表本阶段无 WorkUnit 生产路径。

V24当前为 `Blocked`：实际入口调度和可控观察点未核实，不得仅因尚未找到消费者就写 `N/A`。核实后若确无安全观察点，按 `.agents/instructions/dy-framework-workunit.md` 仅把事件可见性子面记 `N/A` 并记录边界，数据库写入及失败响应仍须验证；这不自动证明事件机制满足业务要求或解除S2-D13。源码只能标记 `SourceConfirmed`；若实际机制不能满足设计，交负责人确认后修订，不自建事件总线或 Outbox。

## 数据库、SQL 与 scope

目标表为 `mrec_mutual_recognition_item`，表注释为“互认项目配置”。最终脚本位置为 `server/Dy.MedicalRecognition.Repository/Scripts/mutual_recognition_item.sql`，严格按以下列序交付；这里只定义DDL，不执行建表或迁移。

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

唯一索引固定为 `ux_mrec_mutual_recognition_org_project`，覆盖 `(organization_code, standard_project_code)`，无状态过滤，停用配置也占用唯一键；索引注释为“同一组织标准项目互认配置唯一（含停用配置）”。脚本包含 `COMMENT ON TABLE`、每列 `COMMENT ON COLUMN` 和 `COMMENT ON INDEX`。不创建 `StandardItemId` 外键，不迁移或触碰其他系统对象；负责人确认目标数据库/schema并执行最终DDL后，才继续依赖新表的集成验证。

SqlMap 的 `Scope` 和每个 DataMapper 调用点统一改为 `MutualRecognitionItem`，不调用 `SetContext`；SQL 显式指向 `mrec_mutual_recognition_item`。每个手写 Statement 补业务功能 XML 注释；SELECT、INSERT、UPDATE 字段补中文注释，复杂 CASE/JOIN/条件补处理、原因、结果注释。作用域运行探测尚未完成，实施前输出“实际查找键/已注册语句键”清单，区分未注册与注册在其他键下；未完成前相关集成验证为 `Blocked`。

目录停用原因采用根设计 S2-D14 固定的规则，只处理停用，不处理删除；按“分类 → 分组 → 项目”优先级返回第一条原因，具体文案依次为“所属分类已停用”“所属分组已停用”“标准项目已停用”。全部目录有效时 `UnavailableReason = null`；配置自身停用只由 `ConfigurationStatus` 表达，不能抢占目录原因。查询使用配置组织过滤后关联全局标准目录，不复制名称、类型、分类或分组；上级恢复时实时重算，不改写配置自身状态。目录资料缺失不是正常业务可达状态，不建立测试夹具或直接写库构造该数据。

## 生成维护边界

后续实施采用受保护的手工维护，不依赖重新生成全后端：设计确认后可修改 Request、ReadModel、Command、Event、Entity、DataMap、Repository接口/实现、XML、DDL，并实现 Application、Manager、查询投影和Host接入。创建Request从Command镜像生成造成的组织字段差异，在该Request手工移除并列入保护清单；UML中的Command保留组织字段。

生成文件与人工维护文件的边界按项目仓库规范处理；本阶段只要求最终公共契约、SQL映射和数据库脚本保持一致，`MutualRecognitionItemDto` 不作为阶段2公共契约。

## 验证矩阵

V1-V25 编号保留，V26-V27补充并发和跨组织写入；V11验证命令可信组织，V19验证查询组织授权；V13验证单层停用，V20验证多层停用优先级，不合并或挪号。

### 测试数据边界

所有业务数据和状态只通过正常业务接口（公开 AppService/API）或页面形成：先创建分类、分组、标准项目和互认配置，再通过独立启停、修改和并发请求形成适用状态。禁止直接执行 SQL、数据库工具、脚本或测试夹具写库，禁止伪造重复主键、孤儿关联、删除后残留或其他违反业务约束的数据。

不可由正常业务流程形成的数据状态不作为普通业务用例前置。请求边界的防御性校验可以使用无持久化数据的输入验证，但不得借此制造数据库脏数据。并发、唯一约束和跨组织验证使用真实接口或页面建立合法前置数据后再发起请求。

真实数据库验证需负责人确认目标数据库/schema、阶段1目录三表可用、阶段2最终DDL执行完成，且scope实际查找键/注册键取证完成；身份/组织验证需真实宿主身份、已核实选中组织传递链和授权组织集合。无上述证据的受影响运行项为 `Blocked`，其余未执行项为 `NotRun`，不把决策标签 Pending 当测试状态。

V16单元验证事件构造/登记规则，不能替代V24真实生产调度；V26要取得事件观察/计数证据，不能仅凭最终状态推断一次事件。表内“真实WorkUnit：否”表示按设计无WorkUnit生产入口验证，不表示事件机制已证实。V18/V25先做无需数据库的静态检查，真实类型/约束/注释仍待建表确认；本次文档任务不执行该运行矩阵。

| 编号 | 用例与边界 | 层级 | 预期 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| V1 | 新增有效配置 | Domain/Application | 成功、默认启用、登记创建事件 | 否 | 否 | 授权组织；三层目录启用 |
| V2 | 重复组织+项目编码 | Domain/Repository | 业务拒绝，唯一约束兜底 | 是 | 否 | 同一组织通过正常接口先创建配置，再由第二个正常请求重复提交 |
| V3 | 目录项目/分类/分组停用新增 | Domain | 拒绝且无事件 | 否 | 否 | 对应层停用 |
| V4 | 时间为空、0、负数、小数、永久有效 | Contract/Domain | 拒绝 | 否 | 否 | 非法输入 |
| V5 | 修改时间及同值保存 | Domain/Repository | 成功、登记修改事件 | 是 | 否 | 已有配置 |
| V6 | 停用后修改时间 | Domain | 成功，不改变状态 | 否 | 否 | 配置已停用 |
| V7 | 启用有效配置 | Domain/Repository | 状态改变并登记事件 | 是 | 否 | 配置停用；三层目录启用 |
| V8 | 启用无效目录配置 | Domain | 拒绝，无状态变化 | 否 | 否 | 配置停用；目录层停用 |
| V9 | 重复启用/停用 | Domain | 幂等成功，不登记事件 | 否 | 否 | 目标已是目标状态 |
| V10 | 不存在配置 ID | N/A | 不属于正常业务可达数据，不建立测试数据或直接写库构造；测试报告记 `N/A` | 否 | 否 | 无 |
| V11 | 命令可信组织注入与伪造组织输入 | Application/Contract | 创建Request无组织；四个Command只用可信组织，不接受伪造字段覆盖 | 否 | 否 | 可信组织A；伪造组织B输入；已核实组织解析契约 |
| V12 | 配置列表组织过滤与目录关联 | Query | 只返回当前组织，资料实时关联 | 是 | 否 | 两个组织；有效目录 |
| V13 | `UnavailableReason` 空值与单层停用原因 | Query | 三层启用为null；仅分类/仅分组/仅项目停用时分别返回对应具体文案；配置自身停用不产生目录原因 | 是 | 否 | 有效目录与配置；分别独立停用一层，其余两层自身状态启用 |
| V14 | 查询排序和空结果 | Query | 标准项目编码升序，空集合成功；不构造同编码或相同 ID 数据测试附加排序 | 是 | 否 | 同一组织通过正常接口创建多个不同编码配置，或无匹配配置 |
| V15 | 数据库唯一约束冲突翻译 | Repository/Application | 返回统一业务错误 | 是 | 否 | 两个正常创建请求并发提交同一组织、同一标准项目编码；不直接写库预置冲突行 |
| V16 | 事件字段和失败不登记事件 | Domain | 四事件字段及EventCreator/EventCreatedTime正确；创建IsValid=true；修改含组织/编码；0行或异常不登记 | 否 | 否 | 成功、0行、异常、幂等命令；可信操作字段与已加载配置记录 |
| V17 | SQL物理表映射 | Repository | 仅访问 `mrec_` 表 | 是 | 否 | 目标表已由负责人建立 |
| V18 | DDL列序、类型、注释、索引 | Static/DB | 与设计一致 | 是 | 否 | 最终 DDL |
| V19 | 查询越权组织 | Application/Query | 拒绝，不返回其他组织数据 | 否 | 否 | 非授权组织；组织服务授权集合 |
| V20 | 多层目录停用原因优先级 | Query | 分类优先于分组，分组优先于项目；恢复上层后显露下一原因，全恢复为null | 是 | 否 | 配置先正常创建；分类+分组、分类+项目、分组+项目及三层同时停用，随后逐层恢复 |
| V21 | Request/ReadModel 契约 | Contract | 字段、枚举、可空性与设计一致 | 否 | 否 | 稳定契约 |
| V22 | 可信组织与操作人来源、组织选择传递 | Application/Host | 选中组织到服务端取值链可追溯；缺失/越权组织及无法解析/空Guid操作人拒绝 | 否 | 否 | 实际宿主身份、组织服务和授权集合；不猜claim；当前来源Pending |
| V23 | SqlMap scope 查找键 | Repository/Static | 实际查找键与注册键一致 | 是 | 否 | 作用域探测结果 |
| V24 | 无WorkUnit生产入口事件调度与提交结果 | Framework/Integration | 正常、写失败、处理失败分别记录登记/处理/数据库提交/异常结果；不得用带WorkUnit探针替代 | 是 | 否；真实无WorkUnit生产入口 | 实际Host入口、可控观察点与失败注入；当前Blocked，未核实不得写N/A |
| V25 | DDL和SQL注释/短索引 | Static/DB | 注释、列序、类型、唯一索引符合设计 | 是 | 否 | 最终脚本和目标库 |
| V26 | 并发同方向启用/停用及0行复读 | Domain/Repository/Integration | 无交错反向操作时仅1行状态变化、只登记一次事件，其余幂等；补反向竞争0行复读仍反向时冲突且无自动无限重试 | 是 | 否 | 通过正常接口创建配置，再经正常启停接口形成目标反向状态；启用时三层有效；同步屏障并发请求；V24观察点；反向竞争分独立场景 |
| V27 | 跨组织配置ID写入拒绝 | Application/Domain/Repository | 组织A修改/启用/停用组织B配置均拒绝，含已是目标状态；B配置、操作字段不变且无事件 | 是 | 否 | A、B均通过正常接口形成各自配置；A请求引用B的配置ID，仅验证资源范围边界，不构造异常数据库数据 |
