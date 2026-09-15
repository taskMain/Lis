# 阶段 1 后端设计

本文件是阶段 1 的后端设计与测试前矩阵。总体范围与关键决策见 [阶段设计](../design.md)。实施批次见 [Server/impl.md](impl.md)，实际执行结果见 [阶段测试报告](../testReport.md)。

## 领域与契约

### 聚合边界与实体

本阶段实体属于唯一聚合 `MedicalRecognitionReportAggregate`，本阶段涉及三个实体：

| 实体 | 表 | 主键 | 层级 |
|---|---|---|---|
| `MedicalStandardCategory` | `mrec_medical_standard_category` | `Id` | 一级（分类） |
| `MedicalStandardGroup` | `mrec_medical_standard_group` | `Id` | 二级（分组，归属分类） |
| `MedicalStandardItem` | `mrec_medical_standard_item` | `Id` | 三级（标准项目，归属分类与分组） |

三者均为 `table` 持久化、`public` 暴露，属性使用 `partial` 并标注 `IPropertyChangedAware`。实体公共属性按 `OperTime`（`DateTimeOffset`）、`OperId`（`Guid`）排列，与 DDL 末尾 `oper_time`、`oper_id` 一致；SQL 参数和结果列也按该顺序显式映射。

### 业务不变量

分类、分组修改及整份保存语义已按 S1-D26 确认。以下为待实施的设计契约，不表示生成代码已经同步或测试通过。

以下不变量在领域层实现，违反时抛 `InvalidOperationException` 并携带中文业务消息（S1-D18）：

| 编号 | 不变量 | SRS 依据 |
|---|---|---|
| INV-1 | 分类名称在全平台标准目录内唯一，**创建与修改路径均适用**（修改时排除自身） | 业务规则 2 |
| INV-2 | 分组名称在同一分类下唯一，**创建与修改路径均适用**；修改时从已有分组读取所属分类，查重排除自身 | 业务规则 2 |
| INV-3 | 标准项目编码全平台唯一 | 业务规则 2 |
| INV-4 | 项目类型固定为检验或检查，不提供自定义类型维护 | 业务规则 1 |
| INV-5 | 新建分组必须归属存在且**启用**的分类；已有分组修改名称备注不要求上级启用 | 备选流 2 |
| INV-6 | 新建的标准项目必须归属**启用**的分类与分组，且分组的所属分类与项目一致 | 备选流 2 |
| INV-7 | 分类无下级分组时可修改项目类型；存在下级分组（含已停用）时类型不可改。名称备注可修改，不受启停及下级使用情况限制，名称仍须唯一 | 备选流 3 |
| INV-8 | 分组所属分类创建后始终不可改，无论有无下级项目。名称备注可修改，不受自身、上级启停或下级使用情况限制，名称在既有分类内唯一 | 备选流 3 |
| INV-9 | 标准项目创建后只允许修改备注；编码、名称、分类、分组均不可通过普通修改改变 | 备选流 4 |
| INV-10 | 创建后默认启用，普通修改不改变启停状态 | 业务规则 3 |
| INV-11 | 已启用对象再次启用、已停用对象再次停用时按幂等成功处理：不改状态、不调用仓储、不收集事件 | 业务规则 6 |
| INV-12 | 分类、分组不提供物理删除能力 | 备选流 3 |
| INV-13 | 资料整份保存，即使内容相同仍校验、写入、更新操作字段并登记修改事件；备注 `null` 清空，不以省略字段保留原值；不影响启停幂等 | 业务规则 7 |

**跨行不变量**（保留领域校验；本 MVP 不增加专门锁定读协议）：

| 编号 | 不变量 | SRS/UML 依据 |
|---|---|---|
| CI-1 | 标准项目的所属分类必须等于其所属分组的所属分类 | INV-6、UML「分组必须属于分类」 |
| CI-2 | 分类存在下级分组时，项目类型不可改变；分组归属始终固定，不依赖下级使用情况 | INV-7、INV-8 |

**「使用情况」的派生口径**（S1-D21）：分类与分组均取「是否存在下级」，含已停用下级。分类使用此值限制项目类型修改；分组仅展示此值，不用于限制名称备注修改。两个列表查询均返回该派生值。

**"实际有效"是派生值，不落库**：当前有效标准项目须同时满足自身、所属分组和所属分类均启用（业务规则 5）。此判断只在查询侧表达，不写入任何状态列。

### 写用例事务边界

本阶段 12 个写命令的原子性判定如下。判定依据是事务原子边界与独立数据库语句数量，不是涉及表数量。

> **2026-09-15 机制口径修订（负责人确认）**：`[WorkUnit]` 特性声明的是**事务**，工作单元生命周期由框架请求管线统一管理，不需要靠特性开启。因此**不开启事务的写入口不再声明该特性**（原表写的裸 `[WorkUnit]` 已撤销）；只有确实需要事务的两个入口保留 `[WorkUnit(UseTransaction = true)]`。依据：反编译本项目包版本确认裸 `[WorkUnit]` 的 `UseTransaction` 保持 `null`、`IsActionUnitTransaction()` 返回 `false`、事务开闭入口全部以它为门；参照项目 `Dy.LisCenter` 中存在「写入口不声明 `[WorkUnit]`、Manager 内照常 `AddEvent`」的既有用例（`ReceiverBranchLabItemCapabilityAppService.BranchCatalog.cs` 对应其 Manager 的 10 处 `AddEvent`）。

| 写用例 | Application Service 入口 | 主要 Repository 数据库语句/操作 | 是否开启事务 | 框架机制 | 原子性依据 | 并发兜底 | DomainEvent 口径 | 外部调用及顺序 | 失败语义 | 验证方式 |
|---|---|---|---|---|---|---|---|---|---|---|
| 创建分类 | `CreateMedicalStandardCategoryAsync` | 1 条查重 `SELECT count(1)` → 1 条 `INSERT` | 否 | 不声明 `[WorkUnit]` | 查重为建议性；最终只有 1 条写语句 | 唯一索引 `ux_mrec_medical_standard_category_name` | 创建成功后登记 `MedicalStandardCategoryCreatedEvent` | 无 | 查重命中抛业务异常；并发冲突由唯一索引拒绝 | V1/V2/V29 |
| 修改分类 | `UpdateMedicalStandardCategoryAsync` | 按 ID 读取分类 → 名称查重排除自身 → 类型变化时查下级分组 → 1 条 `UPDATE` | 否 | 不声明 `[WorkUnit]` | 单条写入，影响行数大于 0 才成功 | 唯一索引、影响行数；类型限制按正常业务校验 | 保存成功后登记 `MedicalStandardCategoryUpdatedEvent`，同值保存亦登记 | 无 | 存在下级且项目类型改变时拒绝；名称重复或记录不存在亦拒绝。类型未变时允许改名和备注，不要求启用 | V3/V4/V5/V31/V35/V40/V52 |
| 启用分类 | `EnableMedicalStandardCategoryAsync` | 1 条按 ID 读取当前状态 → 已是目标状态则不写；否则 1 条条件 `UPDATE` | 否 | 不声明 `[WorkUnit]` | 幂等判断在领域层；非幂等路径只有 1 条写语句 | 条件 `UPDATE` 加当前状态条件 | 非幂等路径登记 `MedicalStandardCategoryEnabledEvent`；幂等路径不登记 | 无 | 记录不存在抛业务异常；幂等路径返回成功 | V16 |
| 停用分类 | `DisableMedicalStandardCategoryAsync` | 同上（方向相反） | 否 | 不声明 `[WorkUnit]` | 同上；下级状态不被改写，不产生跨行写入 | 同上 | 非幂等路径登记 `MedicalStandardCategoryDisabledEvent` | 无 | 同上 | V18 |
| 创建分组 | `CreateMedicalStandardGroupAsync` | 1 条上级分类启用状态 `SELECT` → 1 条查重 `SELECT count(1)` → 1 条 `INSERT` | **是** | `[WorkUnit(UseTransaction = true)]` | 事务的正当用途不是「语句多」，而是让**父级启用校验的前提在写入时仍然成立**，避免挂到刚被停用的分类下 | 唯一索引 `ux_mrec_medical_standard_group_category_name`；跨行竞态按 S1-D23 接受 | 创建成功后登记 `MedicalStandardGroupCreatedEvent` | 无 | 读取时上级停用或重名则抛业务异常 | V6/V7/V8/V9 |
| 修改分组 | `UpdateMedicalStandardGroupAsync` | 按 ID 读取分组及既有分类 → 该分类内查重排除自身 → 1 条 `UPDATE`（名称、备注、操作字段） | 否 | 不声明 `[WorkUnit]` | 单条写入；不修改所属分类或启停状态 | 唯一索引和影响行数 | 保存成功后登记 `MedicalStandardGroupUpdatedEvent`，同值保存亦登记 | 无 | 名称重复或记录不存在拒绝；不检查下级存在性或父级启用状态 | V32/V33/V34/V41/V52 |
| 启用分组 | `EnableMedicalStandardGroupAsync` | 同「启用分类」 | 否 | 不声明 `[WorkUnit]` | 同「启用分类」 | 同「启用分类」 | 非幂等路径登记 `MedicalStandardGroupEnabledEvent` | 无 | 同「启用分类」 | V20 |
| 停用分组 | `DisableMedicalStandardGroupAsync` | 同「停用分类」 | 否 | 不声明 `[WorkUnit]` | 同「停用分类」 | 同「停用分类」 | 非幂等路径登记 `MedicalStandardGroupDisabledEvent` | 无 | 同「停用分类」 | V20 |
| 创建标准项目 | `CreateMedicalStandardItemAsync` | 事务内普通读取分组与分类 → 校验启用状态和归属 → 查重 → 1 条 `INSERT` | **是** | `[WorkUnit(UseTransaction = true)]` | 同「创建分组」：事务用于保证**父级启用与归属校验的前提在写入时仍然成立** | 唯一索引 `ux_mrec_medical_standard_item_code`；跨行竞态按 S1-D23 接受 | 创建成功后登记 `MedicalStandardItemCreatedEvent` | 无 | 读取时分组/分类停用、归属不符或编码重复则抛业务异常 | V10/V11/V12/V13/V43 |
| 修改项目备注 | `ChangeMedicalStandardItemRemarkAsync` | 1 条条件 `UPDATE`（只改 `remark`、`oper_id`、`oper_time`） | 否 | 不声明 `[WorkUnit]` | 单条条件更新，不允许改变编码/名称/分类/分组/启停状态 | 条件 `UPDATE` 影响行数 | 更新成功后登记 `MedicalStandardItemRemarkChangedEvent` | 无 | 记录不存在抛业务异常 | V14/V36/V44 |
| 启用标准项目 | `EnableMedicalStandardItemAsync` | 同「启用分类」 | 否 | `[WorkUnit]` | 同「启用分类」 | 同「启用分类」 | 非幂等路径登记 `MedicalStandardItemEnabledEvent` | 无 | 同「启用分类」 | V45 |
| 停用标准项目 | `DisableMedicalStandardItemAsync` | 同「停用分类」 | 否 | `[WorkUnit]` | 同「停用分类」 | 同「停用分类」 | 非幂等路径登记 `MedicalStandardItemDisabledEvent` | 无 | 同「停用分类」 | V45/V46 |

### 跨行不变量与必要的一致性保护

单条写语句原子或普通工作单元事务不能保证前置读取所依据的不变量在并发写入期间仍成立。以下规则保留正常路径校验，跨行竞态按 S1-D23 接受，不建设额外串行化协议：

- **CI-1 归属一致**：标准项目的所属分类必须等于其所属分组的所属分类（INV-6）。
- **CI-2 类型限制与固定归属**：分类存在下级分组时项目类型不可改变；分组所属分类始终不可改变（INV-7、INV-8）。

**危险交错（本 MVP 仅记录为 AcceptedRisk，不作为交付测试）**：

| 交错 | 序列 | 后果 |
|---|---|---|
| I-1 | 分类改类型读到「无分组」→ 另一请求创建分组 → 分类类型写入 | 分类类型修改的前置判断可能失效 |
| I-2 | 新增分组或项目读到父级启用 → 另一请求停用父级 → 新增写入 | 新建对象自身启用但立即不属于当前有效目录 |

上述为分组不可改挂后的剩余风险；平台无分组移动写入口，不再保留改挂导致项目分类不一致的交错场景。

**MVP 处理方式**：

1. 保留业务层的归属、启用状态和冻结字段校验。
2. 创建分组、创建标准项目保留 `[WorkUnit(UseTransaction = true)]`；该事务负责失败回滚，不据此宣称普通读取和写入获得不变的父级快照。
3. 不新增锁定读、交错注入或专门并发启停测试。唯一索引和条件更新继续承担数据库层基本保护，但不宣称覆盖所有跨行竞态。

**其余 10 个写用例不开显式事务**。分类修改仍校验项目类型限制，但不保证并发期间下级存在性不变；分组修改不涉及归属写入。单条写入、条件更新与唯一约束的保证不扩大到跨行串行化。

**未开启事务的写用例及其并发兜底**：见上表各行；重名类并发由唯一索引拒绝，启停类并发由「条件 `UPDATE` 加当前状态条件」拒绝，项目备注并发由条件 `UPDATE` 影响行数判定。

启停读取时已达到目标状态即幂等成功，不写入、不登记事件；实际条件更新成功才登记事件。条件更新影响零行按业务冲突失败返回，不增设再次读取、自动重试或专门并发测试。该边界不承诺并发同方向请求全部成功。

**仍须在实施阶段验证的项**：

- 创建分组、创建标准项目的事务声明是否符合实际多语句原子边界。
- `[WorkUnit(UseTransaction = true)]` 的默认事务语义、拦截入口与失败回滚结果（见本文「框架机制映射」的未知项）。
- **事件口径**：本阶段只在工作单元内登记，不接通发布订阅；未开启显式事务的用例也可登记事件。事务回滚时已登记事件是否随工作单元丢弃，列为实施验证门，不把工作单元与数据库事务混为一谈。

**事务声明的机械检查**：`[WorkUnit]` 是可反射识别的声明，实施时应增加架构测试，断言 12 个写 AppService 入口的 `WorkUnit` 标注（含 `UseTransaction` 取值）与本表一致；`TransactionScope`、`IDbTransaction`、`BeginTransaction`、`Commit`、`Rollback` 与自定义 UnitOfWork 的新增使用按禁止项检查。

### Command-Event 评审表

| Command/FacadeCommand | 主要 DomainEvent | 成功条件 | 关键事件字段 | 登记位置 | 失败/幂等语义 |
|---|---|---|---|---|---|
| `CreateMedicalStandardCategoryCommand` | `MedicalStandardCategoryCreatedEvent` | 名称全平台唯一且写入成功 | `Id`、`ItemType`、`Name`、`OperId`、`OperTime` | `MedicalRecognitionReportManager` | 重名或写入失败不发事件 |
| `UpdateMedicalStandardCategoryCommand` | `MedicalStandardCategoryUpdatedEvent` | 类型不变或无下级分组，名称唯一且保存成功 | `Id`、`ItemType`、`Name`、`OperId`、`OperTime` | 同上 | 拒绝保存不登记事件；同值保存仍更新操作字段并登记事件 |
| `EnableMedicalStandardCategoryCommand` | `MedicalStandardCategoryEnabledEvent` | 由停用转为启用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 已是启用则返回成功且**不登记事件**（INV-11） |
| `DisableMedicalStandardCategoryCommand` | `MedicalStandardCategoryDisabledEvent` | 由启用转为停用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 已是停用则返回成功且不登记事件；不级联改写下级 |
| `CreateMedicalStandardGroupCommand` | `MedicalStandardGroupCreatedEvent` | 上级分类启用、分类内名称唯一且写入成功 | `Id`、`CategoryId`、`Name`、`OperId`、`OperTime` | 同上 | 上级停用或重名不发事件 |
| `UpdateMedicalStandardGroupCommand` | `MedicalStandardGroupUpdatedEvent` | 名称在既有分类内唯一且保存成功，不要求自身或上级启用 | `Id`、`CategoryId`（从已有分组取）、`Name`、`OperId`、`OperTime` | 同上 | 拒绝保存不登记事件；同值保存仍更新操作字段并登记事件 |
| `EnableMedicalStandardGroupCommand` | `MedicalStandardGroupEnabledEvent` | 由停用转为启用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 幂等路径返回成功且不登记事件 |
| `DisableMedicalStandardGroupCommand` | `MedicalStandardGroupDisabledEvent` | 由启用转为停用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 幂等路径返回成功且不登记事件；不级联改写下级 |
| `CreateMedicalStandardItemCommand` | `MedicalStandardItemCreatedEvent` | 上级分类与分组启用且一致、编码唯一、写入成功 | `Id`、`CategoryId`、`GroupId`、`Code`、`Name`、`OperId`、`OperTime` | 同上 | 上级不一致/停用或编码重名不发事件 |
| `ChangeMedicalStandardItemRemarkCommand` | `MedicalStandardItemRemarkChangedEvent` | 写入成功，备注同值亦写入 | `Id`、`Remark`、`OperId`、`OperTime` | 同上 | 记录不存在不发事件；同值保存仍更新操作字段并登记事件；不改动编码/名称/分类/分组/启停 |
| `EnableMedicalStandardItemCommand` | `MedicalStandardItemEnabledEvent` | 由停用转为启用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 幂等路径返回成功且不登记事件 |
| `DisableMedicalStandardItemCommand` | `MedicalStandardItemDisabledEvent` | 由启用转为停用且写入成功 | `Id`、`OperId`、`OperTime` | 同上 | 幂等路径返回成功且不登记事件 |

**12 个 Command 均有对应的主要 DomainEvent，无无事件命令。** 事件本阶段只登记不发布（S1-D11）；事件字段以实际生成的领域事件类型为准，实施时须核对字段名与上表一致。

### Command 与 Event

本阶段已有 16 个生成命令中的 12 个属于本模块（分类 4、分组 4、项目 4），另有 4 个属于互认配置（F02，不在本阶段）。命令与事件的逐条对应关系见本文「Command-Event 评审表」。

**领域事件本阶段只做收集，不接通发布与订阅**（S1-D11）。Manager 沿用 `AddEvent` 收集，重复启停的幂等路径不收集事件以满足 INV-11；资料同值保存不是该幂等路径。

### 资料修改输入契约

分类、分组无业务 Code，稳定 Guid ID 用于定位。普通修改是整份可编辑资料替换，不是局部补丁：

| 用例 | Request / 业务 Command 输入 | 不可修改字段 |
|---|---|---|
| 修改分类 | `Id`、`ItemType`、`Name`、`Remark` | 有下级分组时 `ItemType` 必须等于原值；`IsValid` 不在输入中 |
| 修改分组 | `Id`、`Name`、`Remark` | `CategoryId` 不在更新 Request/Command 中，始终使用数据库已有归属；`IsValid` 不在输入中 |
| 修改项目备注 | `Id`、`Remark` | 编码、名称、分类、分组、启停均不在输入中 |

`OperId/OperTime` 由 AppService 按可信上下文补入，不允许调用方赋值。前端每次保存都发送完整可编辑字段，备注保留时传原值、清空时显式传 `null`；后端按可空值直接赋值，若缺失反序列化为 `null` 也表示清空，绝不表示保留原值。不新增 Optional 包装或字段存在性追踪；客户端序列化必须保留显式 `null`，并验证保存后读回。

内容相同仍校验、调用仓储、更新操作字段并登记修改事件，不做整份资料前后比较或无变更返回；分类项目类型限制所需的原类型读取和比较仍保留。分组修改查重及事件所需 `CategoryId` 从已有分组取，SQL 更新列仅为 `name/remark/oper_id/oper_time`。未声明的归属字段即使由请求额外携带，也不得进入赋值链。

### 输出契约

所有写命令的成功出参为布尔 `true`，失败抛业务异常，不以 `false` 表示失败。查询返回 UML 定义的 ReadModel，按下表修正（修正性质见[阶段设计](../design.md)的「ReadModel 契约修正」）：

| 查询 | 返回 ReadModel | 修正内容 | 性质 |
|---|---|---|---|
| `QueryMedicalStandardCategoryList` | `MedicalStandardCategoryListReadModel` | `ItemType` 由 `object` 改为 `MedicalItemType`；`Remark` 改为可空 | 修复退化 + 有意改变契约 |
| `QueryMedicalStandardGroupList` | `MedicalStandardGroupListReadModel` | `Remark` 改为可空 | 有意改变契约 |
| `QueryMedicalStandardItemList` | `MedicalStandardItemListReadModel` | `ItemType` 改为 `MedicalItemType`；`Remark` 改为可空 | 修复退化 + 有意改变契约 |
| `QueryEffectiveMedicalStandardCatalog` | `EffectiveMedicalStandardCatalogReadModel` | `ItemTypes` 改为嵌套 ReadModel 集合；`Categories`、`Groups`、`Items` 同理；层级内各明细 ReadModel 的 `Remark` 改为可空 | 修复退化 + 有意改变契约 |

**分类与分组 List ReadModel 新增「使用情况」派生字段**（S1-D22）：

| 项 | 定义 |
|---|---|
| 字段名 | `UsageStatus`，类型 `MedicalStandardUsageStatus`（枚举：`Unused = 0`、`InUse = 1`），由 UML2 定义 |
| 语义 | 是否存在下级：分类看是否存在分组，分组看是否存在标准项目；**含已停用下级**。仅分类用此值限制项目类型修改，分组只用于展示 |
| 来源 | 按分类 ID / 分组 ID 的相关子查询或左连接聚合，在查询语句内派生；**不落库** |
| 契约性质 | 相对原生成基线为**有意扩大公共契约**（S1-D22），现已同步 UML2，须与 `Remark` 可空一并核对实际生成与序列化结果 |

标准项目的使用情况本阶段**既不展示也不返回**，两个项目相关 ReadModel 不新增该字段（S1-D21）。

### 查询语义

按 [Backend Command Query Event](../../../../.agents/instructions/backend-command-query-event.md) 第 3 节，每个查询必须定义范围、空值语义、稳定唯一排序和分页策略。入参一律以 UML 声明为准。

| 查询 | 范围与入参 | 文本匹配语义 | 空值语义 | 稳定唯一排序 | 分页 |
|---|---|---|---|---|---|
| `QueryMedicalStandardCategoryList` | 全平台分类；入参 `ItemType MedicalItemType?` | 无文本入参 | `null` 或未传时**不加该条件**（等价"全部"） | `item_type asc, name asc, id asc`（`id` 唯一，保证全序） | 不分页（S1-D7） |
| `QueryMedicalStandardGroupList` | 入参 `CategoryId Guid?`；未传时返回全部分组 | 无文本入参 | `null` 或未传时不过滤 | `category_id asc, name asc, id asc` | 不分页 |
| `QueryMedicalStandardItemList` | 入参 `CategoryId Guid?`、`GroupId Guid?`、`Code String?`、`Name String?`、`IsValid Boolean?` | `Code`、`Name` 按**字面包含匹配**，输入中的 `%`、`_` 为普通字符，不开放模式输入；大小写按数据库默认排序规则 | `null` 或未传时不过滤；`IsValid = false` **只匹配停用**，不匹配 `null` | `category_id asc, group_id asc, code asc, id asc` | 不分页 |
| `QueryEffectiveMedicalStandardCatalog` | 入参 `ItemType MedicalItemType?`、`CategoryName String?`；范围为自身、所属分组、所属分类**均启用**的标准项目 | `CategoryName` 按**字面包含匹配**，输入中的 `%`、`_` 为普通字符；实现须采用 Provider 中立的转义/谓词方式，实施时核实；若共享 XML 无可行中立写法，回到设计确认 | `null` 或未传时不过滤；不返回任何停用对象 | 类型层按 `ItemType` 枚举数值升序；各类型内分类、各分类内分组、各分组内项目分别按 `name asc, id asc` | 不分页 |

**入参的拒绝边界**：`Guid?` 入参为 `null`/未传表示不过滤；为空 `Guid` 视为非法输入并拒绝。`String?` 入参为 `null`/未传表示不过滤；**空字符串与纯空白视为非法输入并拒绝**（返回参数校验失败），不解释为"不过滤"，也不解释为"匹配空值"。此为公共 API 调用方的输入边界；本阶段页面本地筛选不提交筛选请求，纯空白按撤销本地筛选处理。

> **2026-09-15 复核更正**：第 26 轮曾按 D1 裁定把本节改为「空串/纯空白与 `null` 一致、均不过滤」，**该修改基于对 `<IsNotEmpty>` 的错误推断，现已撤回**。第 27 轮以真实请求实测：`null` 与省略该字段返回完整集合，空字符串与纯空白返回 `参数校验失败`（现由查询请求的 DataAnnotations 特性声明、`MedicalRecognitionRequestValidator` 在查询应用服务入口统一拒绝，见第 40 轮）。因此**原文预期成立，代码无需改动**；客户端 `allowClear` 清空后不发送该字段，不会触发该拒绝。裁定与实测记录见 [决策与结论](决策与结论-20260915.md)。

**排序说明**：排序为**固定排序**，不提供可配置排序（S1-D16）。排序列（`item_type`、`name`、`code`、`category_id`、`group_id`）在实体与本阶段新建 DDL 设计中均非空，**不使用也不允许使用 `NULLS FIRST/LAST`**：按 [Backend Architecture](../../../../.agents/instructions/backend-architecture.md) 5.1，共享 SQL/XML Statement 必须保持 Provider 中立，空值排序属明文禁止的方言特征。若实施中发现某排序列实际可空，须回到设计确认，不在共享 XML 内写方言排序。

**字面包含实现说明**：`QueryMedicalStandardItemList` 与 `QueryEffectiveMedicalStandardCatalog` 的文本筛选必须共同采用 Provider 中立的转义/谓词方式，使 `%`、`_` 按字面字符匹配；不得直接写 PostgreSQL 专用表达式，也不得把过滤后移为全量加载后的内存过滤。实施首批次核实共享 SQL/XML 的可行写法；若公共 SQL 子集无法表达该语义，暂停相关实现并回到设计确认，必要时仅在 Repository/Infrastructure 增加显式 Provider 适配边界。

### 类型归属表

只列本阶段实际新增或修改的类型。层映射见下节。

| 类型名 | 角色 | 所在层/目录 | 调用方 | 主要职责 | 禁止职责 |
|---|---|---|---|---|---|
| `MedicalStandardCategory`、`MedicalStandardGroup`、`MedicalStandardItem` | Entity | Domain `MedicalRecognitionReportAggregate` | Manager | 持久化状态与属性变更跟踪 | HTTP、查询投影 |
| `CreateMedicalStandardCategoryCommand`、`UpdateMedicalStandardCategoryCommand`、`EnableMedicalStandardCategoryCommand`、`DisableMedicalStandardCategoryCommand` | Command | Domain `…Aggregate/Commands` | AppService | 分类的领域意图与操作者信息 | HTTP 契约、展示字段 |
| `CreateMedicalStandardGroupCommand`、`UpdateMedicalStandardGroupCommand`、`EnableMedicalStandardGroupCommand`、`DisableMedicalStandardGroupCommand` | Command | 同上 | AppService | 分组的领域意图与操作者信息 | HTTP 契约、展示字段 |
| `CreateMedicalStandardItemCommand`、`ChangeMedicalStandardItemRemarkCommand`、`EnableMedicalStandardItemCommand`、`DisableMedicalStandardItemCommand` | Command | 同上 | AppService | 标准项目的领域意图与操作者信息 | HTTP 契约、展示字段 |
| `MedicalRecognitionReportManager` | DomainService | Domain `…Aggregate` | AppService | 领域不变量判断、幂等判断、事件登记 | Request、HttpClient、ORM |
| `IMedicalRecognitionReportRepository` | Repository 端口 | Domain `…Aggregate` | Manager | 声明持久化与查重能力 | 领域状态决策、事务生命周期、SQL |
| `MedicalRecognitionReportRepository` | Repository 实现 | Repository `MedicalRecognitionReportAggregate` | 由框架注入给端口 | 执行写语句与查重，逐调用显式传 `scope:`；V30 的参数/结果映射证据来自 SqlMap 字段清单、数据库独立回读和 API 响应逐项对照，数据库读取可使用 dbx MCP | 领域状态决策、事务生命周期 |
| `IMedicalRecognitionReportQueryRepository` | QueryRepository 端口 | Domain `Queries`（与写端口同层，供 Application 依赖） | QueryAppService | 声明 4 个查询方法及其**Domain 查询结果类型** | 任何写操作与副作用 |
| `MedicalRecognitionReportQueryRepository` | QueryRepository 实现 | Repository `Queries` | 由框架注入给端口 | SQL、DataRequest 与结果投影 | 任何写操作与副作用 |
| `MedicalStandardCategoryListReadModel`、`MedicalStandardGroupListReadModel`、`MedicalStandardItemListReadModel`、`EffectiveMedicalStandardCatalogReadModel` | ReadModel（公共契约） | Contracts `Queries` | QueryAppService / 前端 Client | 查询结果的对外形状 | 写流程表达 |
| `EffectiveMedicalStandardCatalogTypeReadModel`、`EffectiveMedicalStandardCatalogCategoryReadModel`、`EffectiveMedicalStandardCatalogGroupReadModel`、`EffectiveMedicalStandardCatalogItemReadModel` | 嵌套 ReadModel（公共契约） | Contracts `Queries` | QueryAppService | QueryAppService 由 Domain 查询结果组装类型、分类、分组、项目四层；核对嵌套集合、明细备注可空性及项目操作字段 | SQL、Repository 私有投影依赖、写流程表达 |
| `MedicalStandardCategoryListQueryRequest`、`MedicalStandardGroupListQueryRequest`、`MedicalStandardItemListQueryRequest`、`EffectiveMedicalStandardCatalogQueryRequest` | QueryRequest | Contracts `Queries` | Host 控制器 | 查询入参契约（严格等于 UML 声明） | 业务规则 |
| `MedicalStandardCategoryListItem`、`MedicalStandardGroupListItem`、`MedicalStandardItemListItem`、`EffectiveCatalogItem` | 查询结果类型 | Domain `Queries` | QueryAppService / QueryRepository | 承接查询端口返回的中立结果；Repository 可使用私有 SQL 行投影再映射 | HTTP 契约、写操作 |
| `MedicalStandardUsageStatus` | 枚举 | Domain.Share `Enums` | ReadModel / Repository | 「使用情况」派生取值（S1-D22） | 持久化列 |
| `CreateMedicalStandardCategoryRequest`、`UpdateMedicalStandardCategoryRequest`、`EnableMedicalStandardCategoryRequest`、`DisableMedicalStandardCategoryRequest`、`CreateMedicalStandardGroupRequest`、`UpdateMedicalStandardGroupRequest`、`EnableMedicalStandardGroupRequest`、`DisableMedicalStandardGroupRequest`、`CreateMedicalStandardItemRequest`、`ChangeMedicalStandardItemRemarkRequest`、`EnableMedicalStandardItemRequest`、`DisableMedicalStandardItemRequest` | Request | Contracts `…Aggregate/Requests` | Host 控制器 | 写用例输入契约 | 可信用户字段、状态流转 |
| `MedicalStandardCategoryDto`、`MedicalStandardGroupDto`、`MedicalStandardItemDto` | Dto | Contracts `…Aggregate/Dtos` | ObjectMap | 实体到外部的映射载体 | 业务规则 |
| `MedicalRecognitionReportAppService` | AppService | Application `…Aggregate` | Host 控制器 | 写用例编排、注入 `OperId`/`OperTime`、声明 WorkUnit 边界 | SQL、领域不变量 |
| `IMedicalRecognitionReportQueryAppService` | 公开 Query Service 契约 | Contracts `Queries` | Host 端点 / 调用方 | 继承 `IApplicationService`，声明下表四个查询及公共 Request/ReadModel 签名 | Domain 查询端口或内部投影、SQL、写入 |
| `MedicalRecognitionReportQueryAppService` | AppService | Application `Queries` | Host 经公开查询服务契约调用 | 继承 `ApplicationService` 并实现 `IMedicalRecognitionReportQueryAppService`；调用 Domain 查询端口、组装公共 ReadModel | SQL、写入 |

`ItemType` 使用 `MedicalItemType` 枚举，不用 `object`；`Remark` 可空；分类与分组 List ReadModel 含 `UsageStatus`（S1-D13、S1-D21、S1-D22）。

### 公开查询服务契约

当前本项目的 `Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.cs` 只声明写方法，尚无公开查询服务；现有 List ReadModel 是单行形状，有效目录 ReadModel 是含 `ItemTypes` 的根对象。声明模式参考 `E:/MedSync/Dy.LisCenter/server/Dy.LisCenter.Application.Contracts/Queries/BranchLabItemMappingQuery/IBranchLabItemMappingQueryAppService.cs` 及对应 Application 实现：公开接口继承 `IApplicationService`，查询类继承 `ApplicationService` 并实现该接口。这只确认源码声明模式，不表示本项目查询端点已生成或运行通过。

新增契约位于 `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs`，由 `Application/Queries/MedicalRecognitionReportQueryAppService.cs` 实现；不把查询追加到写服务，不以 Domain 的 QueryRepository 端口替代公开契约，也不为内部 Manager 增加接口。本阶段涉及的现有 `Contracts/ReadModels` 类型按类型归属表对齐至 `Contracts/Queries`，只调整本模块类型及引用，不搬迁其他查询模型。

| 公开方法 | 请求类型 | 返回类型 |
|---|---|---|
| `QueryMedicalStandardCategoryListAsync` | `MedicalStandardCategoryListQueryRequest` | `Task<IEnumerable<MedicalStandardCategoryListReadModel>>` |
| `QueryMedicalStandardGroupListAsync` | `MedicalStandardGroupListQueryRequest` | `Task<IEnumerable<MedicalStandardGroupListReadModel>>` |
| `QueryMedicalStandardItemListAsync` | `MedicalStandardItemListQueryRequest` | `Task<IEnumerable<MedicalStandardItemListReadModel>>` |
| `QueryEffectiveMedicalStandardCatalogAsync` | `EffectiveMedicalStandardCatalogQueryRequest` | `Task<EffectiveMedicalStandardCatalogReadModel>` |

每个方法接收一个对应请求对象，入参字段仍以查询语义表为准。前三个查询无结果返回空集合；有效目录无结果返回根对象及空 `ItemTypes` 集合，不返回 `null` 或扁平项目集合。Host 按框架现有服务契约机制公开这四个操作，V26 核对声明、端点及 OpenAPI，V53 核对实现关系与依赖方向。

### 项目层映射与依赖方向

| 概念层 | 实际项目/目录 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | `server/Dy.MedicalRecognition.Application.Contracts`（`…Aggregate/Requests`、`Dtos`、`Queries`） | `Dy.Apron.Abstractions`、`Domain.Share` | Domain 实现、Repository、Application |
| Application | `server/Dy.MedicalRecognition.Application` | Contracts、Domain（**含 Domain 的 `Queries` 端口**） | Repository 实现、Host |
| Domain | `server/Dy.MedicalRecognition.Domain`（`…Aggregate` 与 `Queries` 端口） | `Dy.Apron.Abstractions`、Domain.Share | HTTP、Application、Repository 实现 |
| Domain.Shared | `server/Dy.MedicalRecognition.Domain.Share`（含 `Enums/MedicalItemType`、`Enums/MedicalStandardUsageStatus`） | `Dy.Apron.Abstractions` | 其余各层 |
| Repository/Infrastructure | `server/Dy.MedicalRecognition.Repository`（`MedicalRecognitionReportAggregate`、`Queries`、`Scripts/`、SqlMap XML） | Domain、`Dy.Earthrace.Abstractions` | Application、Host |
| Host | `server/Dy.MedicalRecognition` | Application、Repository | — |

**跨层边界的关键点**：QueryAppService 位于 Application，**不允许依赖 Repository 的实现类型**。因此查询端口 `IMedicalRecognitionReportQueryRepository` 与内部投影类型必须声明在 Domain 的 `Queries` 区域，Application 只依赖该端口与投影；Repository 提供实现。这与写链路的 `IMedicalRecognitionReportRepository` 同形。

### 完整调用链

**写链路**：

```text
Request（Contracts/*Request）
-> AppService（Application，注入 OperId/OperTime，声明 WorkUnit 边界；需要时 UseTransaction=true）
-> Command（Domain/*Command，由 ObjectMap 生成）
-> Manager（Domain，领域不变量与幂等判断、AddEvent）
-> IMedicalRecognitionReportRepository（Domain 端口，逐调用显式传 scope: 实体名）
-> MedicalRecognitionReportRepository（Repository，DataMapper + SqlMap）
-> 条件写语句（创建分组、创建项目保留工作单元事务，不增加锁定读）
-> DomainEvent（登记，本阶段不发布）
```

**查询链路**：

```text
QueryRequest（Contracts/*QueryRequest，入参等于 UML）
-> Host 端点 -> IMedicalRecognitionReportQueryAppService（Contracts/Queries，公开应用能力）
-> MedicalRecognitionReportQueryAppService（Application，实现公开契约，只通过 Domain 查询端口读取数据）
-> IMedicalRecognitionReportQueryRepository（Domain/Queries 端口）
-> MedicalRecognitionReportQueryRepository（Repository/Queries，独立作用域）
-> SQL 映射为 Domain 查询结果（*ListItem / EffectiveCatalogItem）
   （需要私有 SQL 行投影时由 Repository 内部转换，不向 Application 暴露该私有类型）
-> QueryAppService 把 Domain 查询结果转换为公共 ReadModel
   （有效目录在此把扁平结果组装为类型→分类→分组→项目的层级结构；「使用情况」由 SQL 派生）
-> 公共 ReadModel（Contracts/Queries）
```

**转换责任归属**：SQL 结果到 Domain 查询结果由 Repository 完成，形状一致时直接映射，不为分层额外创建私有投影；确有映射差异时私有投影只留在 Repository 内。Domain 查询结果到公共 ReadModel 由 QueryAppService 完成；实体到写 Request/Dto 的映射由 ObjectMap 生成的代码完成。

关键字段来源：

| 字段 | 来源 |
|---|---|
| `Id` | 仓储 `CreateGuid()`（新增时）或调用方传入（修改与启停时） |
| `OperId` | 可信上下文 `HttpRequestInfo.UserId`，由 AppService 解析后写入 Command；**解析失败的处理见下方「操作者身份」** |
| `OperTime` | AppService 注入 `DateTimeOffset.UtcNow`，与 LisCenter 的应用服务赋值方式一致 |
| `IsValid` | 创建时由领域层置为启用；之后只由启停命令改变 |
| `ItemType`、`Name`、`Code`、`Remark` | 调用方经 Request 传入 |
| `CategoryId`、`GroupId` | 创建时由 Request 传入，领域层校验归属和启用状态；修改分组的 `CategoryId` 从已有分组读取，不从更新 Request/Command 取值 |
| 「实际有效」 | 查询侧由三表 join 派生，不落库（S1-D12） |
| 「使用情况」（分类与分组） | 查询侧按是否存在下级派生（S1-D22） |

有效目录项目明细返回项目自身持久化的 `OperId Guid` 与 `OperTime DateTimeOffset`，取消 `LastUpdatedTime`。父级启停不改写项目操作字段；读取查询不产生新的操作时间。UML 实体默认省略公共操作字段，公共查询返回字段则显式声明。平台新表的 `oper_time` 直接定义为 `timestamptz`，按可信 `DateTimeOffset.UtcNow` 写入，V30 验证真实 Provider 存取与序列化对应同一时间点；不读取、解释或转换其他系统的历史时间列。

### 操作者身份与输入校验

**操作者身份**：阶段 0 已记录生成物在 `OperId` 解析失败时**静默回退为 `Guid.Empty`**，使「修改人」不可追溯。本阶段的处理：

| 场景 | 处理 |
|---|---|
| `HttpRequestInfo.UserId` 缺失、非 Guid 或为 `Guid.Empty` | **拒绝该写请求**并抛业务异常；不写入任何数据（失败零写入） |
| 合法的 Guid | 写入 Command 的 `OperId` |
| 校验所在层 | AppService 在构造 Command 后、调用 Manager 前校验；Manager 不再重复判断身份来源 |

SRS 要求用平台框架信息记录修改人和时间，静默落 `Guid.Empty` 不满足该要求，因此按拒绝处理而非回退。

**必填与值域边界**（只列 SRS/UML 已明确的，不代 SRS 发明长度或字符规则）：

> **2026-09-15 校验归属修订（负责人确认）**：必填与值域约束**在公共 Request 上以 DataAnnotations 声明**，由 `MedicalRecognitionRequestValidator.Validate(request)` 在 Application Service 入口触发。原因：框架的 JSON 选项未启用 DataAnnotations 自动校验，特性不会自行生效，必须显式触发（参照项目 `Dy.LisCenter` 用 `LisCenterRequestValidator` 手工调用 125 处）。**长度按 DDL 既有列宽在 Request 层前置声明（2026-09-15 负责人二次确认，取代本节原先“长度不在 Request 层发明”的口径）**：`Name` 与 `Code` 声明 `[StringLength(200)]`、`Remark` 声明 `[StringLength(600)]`，取值与三张表的 DDL 列宽一致；超长输入在 Request 层即以 `参数校验失败：…长度不能超过 …` 拒绝，不再依赖数据库异常翻译边界。变更理由：`csharp-backend-style.md` 第 5 节要求公共 Request 声明必要的长度约束，且边界拒绝可避免一次数据库往返并给出可区分的消息。领域层只保留 DataAnnotations 无法表达的业务规则（归属关系、启停前提、唯一性、状态流转）。
>
> **顺序要求**：文本字段先 `Trim` 再校验，否则 `[Required]` 会放行纯空白。**2026-09-15 实测更正**：本项目使用的 .NET 版本中 `[Required]` 对空串与纯空白串**同样拒绝**（`Trim().Length != 0` 判定），第 40 轮的 B1/B2/B3 三例实测均返回 `参数校验失败：名称不能为空。`；因此该顺序要求不再承担“拦截纯空白”的职责，仍保留“存储值不带首尾空白”的意图。

| 字段 | 边界 | 声明位置 |
|---|---|---|
| `Name`（分类、分组、项目名称） | 必填；缺失、空串、纯空白均拒绝；长度上限取 DDL 列宽 200 | Request `[Required]` + `[StringLength(200)]`；领域层不再重复 |
| `Code`（标准项目编码） | 必填；缺失、空串、纯空白均拒绝；长度上限取 DDL 列宽 200 | Request `[Required]` + `[StringLength(200)]`；领域层不再重复 |
| `ItemType`（分类项目类型） | 只接受 `MedicalItemType` 已定义取值，未定义值拒绝；未传按默认 `Laboratory=0`（2026-09-15 裁定） | Request `[EnumDataType]`；领域层不再重复 |
| `CategoryId`、`GroupId` | 在声明这些字段的创建契约中必填；为空 Guid 拒绝；`GroupId` 必须属于 `CategoryId`（INV-6）；修改契约不开放归属输入 | 空 Guid 由 Request `[NonEmpty]` 声明；`GroupId` 归属由领域层校验（INV-6） |
| `Remark` | 可空；整份保存直接赋值，`null` 清空，缺失不表示保留；前端未改时传原值；长度上限取 DDL 列宽 600 | Request `[StringLength(600)]` + 领域层直接赋值 |
| 查询入参 | 见本文「查询语义」的入参拒绝边界 | 查询请求 DataAnnotations 特性 + 查询应用服务调用 `MedicalRecognitionRequestValidator` |

**输入校验失败与业务拒绝的区分**：参数缺失/非法/空 Guid/纯空白属**输入校验失败**；唯一性冲突、上级停用、冻结字段被改属**业务拒绝**。两者都以业务异常形式返回（S1-D18），但错误消息必须可区分，供前端提示与测试断言使用。**2026-09-15 第 40 轮落地**：输入校验失败由 `System.ComponentModel.DataAnnotations.ValidationException` 表示（Request 层），业务拒绝由 `System.InvalidOperationException` 表示（领域层）；两者经框架异常链路均返回 HTTP 500，消息前缀分别为 `参数校验失败：` 与 `业务拒绝：`。

## 数据与集成

### 仓储与 SqlMap

作用域修正按 S1-D1 处理，单独不改变语句 ID；物理表引用另按已确认的 `mrec_` 新表映射调整，不能把“只改 Scope”用于排除必要的表名对齐。业务规则与 S1-D26 所需的读取及更新列调整按下表实施，仓储结构不拆：

| 项 | 设计 |
|---|---|
| 作用域传递 | 仓储**不再调用 `SetContext`**；`DataMapper` 属性只做普通注入。每个调用点显式传 `scope:`，取值为**实体名** |
| 作用域取值 | 分类、分组、标准项目各自使用自己的实体名，如 `"MedicalStandardCategory"` |
| SqlMap `Scope` | **改为各自实体名**，与调用点取值一致；逻辑 Scope 不加 `mrec_`，物理表名及业务更新内容另按本表调整 |
| 物理表映射 | 本模块 DDL 与 SqlMap 写入/查询语句显式使用三张 `mrec_medical_standard_*` 物理表名；实体名及 API 名称不加前缀，不新增表映射特性或全局映射器。替换物理表名不改变逻辑 Scope 或 sqlId；V57 核对静态一致性，V30 核对真实持久化 |
| 仓储结构 | 沿用单个聚合级仓储 `MedicalRecognitionReportRepository`（按实体拆局部文件），不按实体拆类 |
| 常规增改 | 调用既有生成语句并传业务化 `sqlId`（如 `CreateMedicalStandardCategory`）；语句键由 `sqlId` 实参决定，与仓储对外方法名无关 |
| 启停 | 调用既有语句并传 `sqlId`（如 `EnableMedicalStandardCategory`），语句内容为 `set is_valid = true / false` |
| 唯一性查重 | 新增 `Exists*` 语句，`count(1)` 并按可选条件排除自身 |
| 修改字段白名单 | 分类更新 `item_type/name/remark/oper_id/oper_time`，类型变化时先验证无下级分组；分组仅更新 `name/remark/oper_id/oper_time`，项目仅更新 `remark/oper_id/oper_time`。备注含 `null` 直接赋值，不使用非空条件跳过；普通修改不写归属或启停字段 |
| 跨行并发保护 | 本 MVP 不增加专门锁定读与交错测试；保留业务校验、唯一索引和必要的工作单元事务 |
| 查询作用域 | 独立查询仓储使用**独立作用域**（`"MedicalRecognitionReportQuery"`），其 SqlMap 按查询组织，不混入实体仓储的 XML |
| 未接通语句 | 本模块生成物的 `QueryAll<X>` 不删除、不接入，本阶段 4 个查询不经过这些语句；其已有物理表引用仍纳入前缀映射静态核对，不保留指向其他系统旧表的引用，不扩改其他模块 |
| `triggerEntityEvent` | 不传，保持框架默认（S1-D14） |

**V57 的 Statement 覆盖清单**：

| SqlMap 文件 | Scope | 覆盖范围 |
|---|---|---|
| `MedicalStandardCategory.xml` | `MedicalStandardCategory` | 分类全部创建、读取、查重、修改、启停及保留的 `QueryAllMedicalStandardCategory` |
| `MedicalStandardGroup.xml` | `MedicalStandardGroup` | 分组全部创建、读取、查重、修改、启停及保留的 `QueryAllMedicalStandardGroup` |
| `MedicalStandardItem.xml` | `MedicalStandardItem` | 项目全部创建、读取、查重、备注修改、启停及保留的 `QueryAllMedicalStandardItem` |
| 查询 SqlMap（独立查询目录） | `MedicalRecognitionReportQuery` | 四个公开查询及其投影语句 |

执行时对上述文件逐个提取 `Scope + Statement Id`，扫描每个 Statement 的物理表引用；阶段 1 允许的物理表仅为三张 `mrec_` 表，保留的 `QueryAll` 也必须指向对应新表。扫描同时断言旧的无前缀表名不出现；不把跨阶段文件或未被阶段 1 引用的其他实体纳入本清单。

`SetContext` 属共享映射器上的可变作用域状态，并发调用方会互相翻转作用域；对标实现 `Dy.LisCenter` 的仓库测试明确禁止生产仓储依赖该状态，其仓储一律使用逐调用显式传 `scope:` 的写法。本阶段采用同一写法。

### 框架机制映射

按 [Backend Design Gates](../../../../.agents/instructions/backend-design-gates.md) 第 6 节与 [Dy Framework WorkUnit](../../../../.agents/instructions/dy-framework-workunit.md) 要求，根据本项目实际包版本核实：

| 项 | 本项目实际 |
|---|---|
| 框架与版本 | Dy.Apron `1.1.0.48`、Dy.Earthrace `1.0.0.60`、`net10.0` |
| 工作单元声明方式 | `[WorkUnit]`（`Dy.Core.Abstractions.Http.WorkUnitAttribute`），具名属性含 `UseTransaction`、`IsolationLevel`、`Timeout`、`TransactionType`、`IsDisabled`；经元数据核对确认存在 |
| 事务入口 | 公开 Application Service 方法；AppService 只声明边界，Manager、Repository、Entity 不控制事务生命周期 |
| 现有用法 | 本模块现有 12 个写方法**均未标注 `[WorkUnit]`**，属阶段 0 生成物状态，本阶段按写用例事务表逐项确定 |
| 事件顺序 | 事件登记、`FlushAsync`、处理器观察、事务提交与外部投递是不同事实；本阶段**只做登记、不接通发布与订阅**（S1-D11），因此不依赖提交后投递顺序 |
| 外部调用 | 本阶段无外部调用，事务内不存在等待外部系统的情形 |
| 禁止项 | 不使用 `TransactionScope`、不直接使用 `IDbTransaction`、不新增自定义 UnitOfWork 或事务包装器 |

**尚未核实、列为实施验证门的框架语义**（元数据只能证明属性存在，不能证明运行行为）：

| 未知项 | 影响 | 验证方式 |
|---|---|---|
| 省略 `UseTransaction` 是否确实代表**不开事务** | 10 个用例的事务判定依据 | 实施首批次用一个写方法观察是否存在显式事务 |
| `UseTransaction = true` 的拦截入口与默认隔离级别 | 创建分组、创建标准项目的多语句原子性 | 真实数据库下核对工作单元回滚与提交结果 |
| 事务回滚时**已登记事件**是否随工作单元丢弃 | 失败用例的事件口径 | 按 V54-B 先证明事件已登记，再提交前失败并观察清理及零提交；无安全注入或观察点时仅该框架事件观察面按规范记 `N/A` 并说明边界（`dy-framework-workunit.md` 第 3 节），V54-A 数据库回滚仍必须验证 |
| 写入已提交但请求失败时的重试语义 | 重试幂等性 | 记录为实施验证门，本阶段不新增重试机制 |

阶段 0 记录的事件队列 `FlushAsync` 可能先于事务提交，属**其他项目的观测**；本项目未核实，不据此设计。

### 数据库结构

平台尚未创建自身库表。`DysoftHIS` 中发现的无前缀三表是归属未确认的非本平台对象，不是本平台既有表或共享表，也不是平台数据基线。本阶段新建三张独立 `mrec_` 表，不迁移、重命名、复制旧表，不导入旧数据、不双写或添加兼容视图；不预建其他模块表。物理表前缀及独立新建方向已确认，不再作为待选方案；S1-D29 的交互决策仍待确认，不受本次数据设计调整影响。

新建 DDL 直接使用 `is_valid boolean`、`oper_time timestamptz`，不包含整数布尔转换、历史时间转换或旧注释迁移/回退。本模块列序与表/列数据库注释遵循 S1-D30：

| 物理表名 | CREATE 字段顺序 | 表注释 |
|---|---|---|
| `mrec_medical_standard_category` | `id, item_type, name, remark, is_valid, oper_time, oper_id` | 标准医疗项目分类 |
| `mrec_medical_standard_group` | `id, category_id, name, remark, is_valid, oper_time, oper_id` | 标准医疗项目分组 |
| `mrec_medical_standard_item` | `id, category_id, group_id, code, name, remark, is_valid, oper_time, oper_id` | 标准医疗项目 |

字段类型与可空性沿用本模块已生成的字段定义：`id/category_id/group_id/oper_id` 为 `uuid not null`，`item_type` 为 `integer not null`，`name/code` 为 `varchar(200) not null`，`remark` 为可空 `varchar(600)`，`is_valid` 为 `boolean not null`，`oper_time` 为 `timestamptz not null`；只在各表声明适用字段。每表 `id` 为主键，创建默认启用仍由领域层赋值，不因 DDL 整理增加业务字段或输入规则。

字段中文注释逐项取 UML Entity 属性：`item_type` 为项目类型、`category_id` 为分类ID、`group_id` 为分组ID、`code` 为标准项目编码；`name` 按表分别为分类名称、分组名称、标准项目名称；`remark` 为备注、`is_valid` 为是否启用。公共 `id/oper_time/oper_id` 分别为主键 ID、操作时间、操作人。注释须在新建脚本中落为 PostgreSQL 数据库元数据，不只写 SQL 文件内说明，不把 DDL 方言带入共享业务 XML。

除各表主键外，只设计本阶段业务所需的三条唯一索引：

| 表 | 唯一索引名 | 键列 |
|---|---|---|
| `mrec_medical_standard_category` | `ux_mrec_medical_standard_category_name` | `name` |
| `mrec_medical_standard_group` | `ux_mrec_medical_standard_group_category_name` | `category_id, name` |
| `mrec_medical_standard_item` | `ux_mrec_medical_standard_item_code` | `code` |

三条唯一索引均覆盖全部行，包括停用对象，不加仅启用行的过滤条件。撤销“保留旧表 14 个索引、生成脚本不补齐”的例外；新建脚本必须实际包含上述唯一性保障，不复制其他系统的索引，也不预先增加 `is_valid` 单列索引、外键或其他未确认约束。后续确有查询性能证据时另行评估索引，不改变本阶段唯一性和归属业务校验。

表前缀只作用于物理表名，不修改 `MedicalStandardCategory/Group/Item` 等实体名、API、逻辑 Scope 或 sqlId。沿用 SqlMap 显式物理表名：本模块所有读写语句直接引用对应前缀表，不依赖实体名自动推出表名，不新增特性或全局映射器。重生成前核对生成器支持，不为本轮设计实际再生成。

**映射源码依据**：以 `E:/MedSync/Dy.LisCenter/server/` 为根，`Dy.LisCenter.Domain/ReceiverBranchLabItemCapabilityAggregate/StandardItemEntity.cs` 实体无表映射特性且不带 `lis_` 前缀；`Dy.LisCenter.Repository/ReceiverBranchLabItemCapabilityAggregate/ReceiverBranchLabItemCapabilityRepository.StandardItem.cs` 使用 `InsertAsync(standardItem, scope: SqlScope)`；同目录 `ReceiverBranchLabItemCapability.xml` 第 118–131 行显式 `insert into/from lis_standard_item`。该例 XML 的 Scope 为聚合名 `ReceiverBranchLabItemCapability`，只能作为逻辑名称与物理表名分离的源码依据，不能证明实体 Scope 必要；本项目已选实体 Scope 保持不变，也不从该例推断框架不支持特性。源码模式不替代本项目 V30 的真实运行验证。

**建库、建表由项目负责人执行**。实际目标库/schema、名称占用及执行材料须先核对；发现同名对象时暂停受影响操作并报告，不能用 `create table if not exists` 静默接受其他系统对象。目标表未建成或实现的物理表引用未完成静态核对时，只暂停依赖它们的数据库读、写验证；独立设计、代码与脚本准备、契约及静态检查可继续。V30/V58 可在负责人独立授权并确认建表的受控库验证，不等待目标库先建成，不对其他系统旧表执行操作。

V30 的数据由平台正常业务路径在新表中建立，回读核对分组父分类存在、项目父分类/父分组存在、项目分类等于所属分组分类。发现异常时记录数量与定位 ID 并报告负责人，不自动改数据，不把 S1-D23 并发风险接受当作异常通过依据。矩阵中的“既有对象”仅指各用例独立准备的前置对象，不指 `DysoftHIS` 旧数据。

### 失败语义

| 场景 | 语义 |
|---|---|
| 唯一性冲突 | 先查重命中则抛业务异常；并发写入由数据库唯一索引兜底，最终仍返回可识别的业务冲突，不暴露原生数据库异常。优先沿用框架异常翻译，实际映射由 V29 验证 |
| 新增分组或项目时上级不存在或已停用 | 拒绝保存并抛业务异常；不以停用拒绝已有分类分组的名称备注维护 |
| 存在下级分组的分类修改项目类型 | 拒绝保存并抛业务异常；分组归属不开放修改输入 |
| 记录不存在 | 抛业务异常 |
| 幂等重复启停 | 返回成功，不改状态、不收集事件 |
| 名称或编码超过 DDL 长度 | **已由 Request 长度声明前置拦截**（2026-09-15 负责人确认）：超长输入在进入领域层前返回 `参数校验失败：…长度不能超过 …`（名称/编码 200、备注 600，与 DDL 列宽一致），不再触发数据库异常翻译路径；原纳入 V29 的“未翻译原生异常”取证范围相应收窄为不含超长输入 |

### 外部依赖

本阶段**不使用任何外部系统**：标准目录不按组织复制，不需要可信组织服务；查询与写入均在本地表内完成。权限系统的菜单与接口授权不在后端重复判断。

## 验证矩阵

本表只记录测试前设计，实际执行结果进入 [阶段测试报告](../testReport.md)。层级含义：`Unit` 为领域层单元测试，`Integration` 为经真实仓储与真实数据库的集成测试，`Contract` 为接口契约测试，`Architecture` 为分层及声明的静态/反射检查。每个用例独立建立前置数据，不依赖其他编号的执行顺序。

| 编号 | 用例与边界 | 层级 | 预期结果 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| V1 | 创建分类，名称全平台未使用 | Integration | 保存成功，`IsValid` 默认启用，形成创建事件 | 需要 | 需要 | 无 |
| V2 | 创建分类，名称与既有分类重复 | Integration | 拒绝保存，抛业务异常（INV-1） | 需要 | 需要 | 1 条既有分类 |
| V3 | 修改分类的项目类型与名称，该分类无下级分组 | Integration | 保存成功，启停状态不变 | 需要 | 需要 | 无下级分组的分类 |
| V4 | 修改分类的项目类型，该分类存在下级分组 | Integration | 拒绝保存，抛业务异常（INV-7） | 需要 | 需要 | 分类下有 1 个分组 |
| V5 | 修改分类的项目类型，其下级分组为**已停用**状态 | Integration | 仍拒绝保存（类型限制与下级启用状态无关）（INV-7、S1-D17） | 需要 | 需要 | 分类下有 1 个已停用分组 |
| V6 | 创建分组，归属启用的分类且名称在分类内唯一 | Integration | 保存成功，默认启用 | 需要 | 需要 | 启用分类 |
| V7 | 创建分组，分别覆盖父分类已停用、父分类 ID 非空但不存在 | Integration | 各分支均业务拒绝、零写入且不登记成功事件（INV-5）；不存在分支核对父级查询无记录的业务拒绝，不得退化为空引用或原生数据库错误，不能以空 ID 参数校验失败代替 | 需要 | 需要 | 停用分支使用正常创建后停用的分类；不存在分支使用格式合法、经只读核对未占用的非空分类 ID，其余输入及身份合法，不删除既有父级构造场景 |
| V8 | 创建分组，名称在同一分类下重复 | Integration | 拒绝保存，抛业务异常（INV-2） | 需要 | 需要 | 同分类下既有分组同名 |
| V9 | 创建分组，名称在**另一分类**下已存在 | Integration | 保存成功（分组名称仅在同一分类下唯一）（INV-2） | 需要 | 需要 | 另一分类下有同名分组 |
| V10 | 创建标准项目，归属启用的分类与分组，编码未使用 | Integration | 保存成功，默认启用 | 需要 | 需要 | 启用分类与分组 |
| V11 | 创建标准项目，编码与既有项目重复 | Integration | 拒绝保存，抛业务异常（INV-3） | 需要 | 需要 | 1 条既有项目 |
| V12 | 创建标准项目，分组不属于所选分类 | Integration | 拒绝保存，抛业务异常（INV-6） | 需要 | 需要 | 分属不同分类的分组 |
| V13 | 创建标准项目，分别覆盖分类启用/分组停用、分类停用/分组启用、两级均停用；另独立覆盖父分类 ID 非空但不存在、父分组 ID 非空但不存在 | Integration | 每种情况均业务拒绝、零写入且不登记成功事件（INV-6），不得退化为空引用或原生数据库错误。父分类不存在且合法分组属于另一分类时，允许不存在分类或归属不符任一业务校验先行拒绝，不要求查询或报错顺序；该重叠场景不能单独证明分类查询无记录分支已执行。父分组不存在仍核对对应业务拒绝；不能用空 ID 参数校验失败或停用父级代替不存在场景 | 需要 | 需要 | 停用分支各自独立创建后经正常启停获得父级状态，保留归属一致但分类停用的独立分支；不存在分支从合法启用目录及合法请求起点，仅替换一个父级 ID 为格式合法、经只读核对未占用的非空 ID，另一父级保持存在且启用。不构造孤儿分组、不破坏已有关系数据，不为隔离错误增加读取 |
| V14 | 修改标准项目备注 | Integration | 保存成功，编码/名称/分类/分组与启停状态均不变（INV-9） | 需要 | 需要 | 1 条既有项目 |
| V15 | 核对标准项目备注更新契约与映射 | Contract | Request/Command 不声明编码、名称、分类、分组及启停输入，映射白名单不能赋值这些字段（INV-9） | 不需要 | 不需要 | 无 |
| V16 | 启用已启用的分类 | Integration | 返回成功；不改变状态、不收集事件（INV-11） | 需要 | 需要 | 已启用分类 |
| V17 | 停用已停用的分组 | Integration | 返回成功；不改变状态、不收集事件（INV-11） | 需要 | 需要 | 已停用分组 |
| V18 | 停用分类，其下有启用分组与启用项目 | Integration | 分类自身转为停用；下级自身状态**不被改写**（SRS 业务规则 4） | 需要 | 需要 | 分类 + 分组 + 项目均启用 |
| V19 | 重新启用已停用分类，其下分组和项目自身仍启用 | Integration | 分类转为启用；下级自身仍启用者自动恢复当前有效（SRS 业务规则 4、后置条件 3） | 需要 | 需要 | 独立创建全启用目录后停用分类，不依赖 V18 执行 |
| V20 | 分组独立启停，分别覆盖父分类启用与停用 | Integration | 父分类启用时，停用分组使其启用项目无效，重新启用分组后项目恢复当前有效；父分类保持停用时，仍可成功启用已停用分组，分组自身变为启用但其项目仍不属于当前有效目录。各分支父分类及项目自身状态不变，实际状态变更登记对应事件 | 需要 | 需要 | 每个分支独立正常创建分类、分组及启用项目，再通过停用操作准备父分类启用/停用两种起点；停用父分类分支不先恢复分类，无并发或直接改库造数 |
| V21 | 分类列表查询，按项目类型筛选 | Integration | 返回符合的集合；未传筛选时返回全部（S1-D16） | 需要 | 不需要 | 多类型数据 |
| V22 | 分组列表查询，按分类筛选 | Integration | 返回该分类下的分组集合 | 需要 | 不需要 | 多分类数据 |
| V23 | 标准项目列表查询，按分类/分组/编码/名称/是否启用组合筛选，编码与名称分别验证字面符号 | Integration | 各条件按 UML 入参生效，组合为与关系；`Code`、`Name` 分别输入 `A_B` 时命中含字面 `A_B` 的值而不命中 `AXB`，输入 `A%B` 时命中含字面 `A%B` 的值而不命中 `AXXB`；输入单个 `%` 或 `_` 只匹配含对应字符的值，不变成匹配全部 | 需要 | 不需要 | 多项目合法数据；分别准备含 `A_B`、`A%B` 的值及 `AXB`、`AXXB` 对照，另有不含符号的数据；编码保持全平台唯一 |
| V24 | 当前有效标准目录查询 | Integration | 仅返回自身、所属分组、所属分类均启用的项目，按类型/分类/分组组织为层级结构（业务规则 5、S1-D6） | 需要 | 不需要 | 含停用对象的目录数据 |
| V25 | 当前有效标准目录查询，含已停用分类下的启用项目 | Integration | 该项目不出现（三级均启用才有效） | 需要 | 不需要 | 停用分类 + 启用项目 |
| V26 | 全部查询返回字段与 UML ReadModel 一致；核对公开查询服务契约与本模块服务端操作范围 | Contract | `ItemType` 为枚举而非 `object`；层级集合为嵌套结构；四处 `Remark` 可空；分类/分组 `UsageStatus` 枚举数值为 Unused=0、InUse=1；项目不含使用情况（S1-D13/S1-D22）。`IMedicalRecognitionReportQueryAppService` 的四个方法、请求及返回类型与公开契约表逐项一致；本模块公开的 12 个写命令与 4 个查询逐项齐全，HTTP 方法及路径与对应操作一致；分类、分组、标准项目均无公开物理删除操作 | 不需要 | 不需要 | 按本文 Command-Event 表的 12 个命令及公开查询服务契约表的 4 个方法建立预期清单，对照契约声明、本轮服务端实际端点注册清单及 OpenAPI；不以客户端生成范围或页面按钮代替服务端证据，不执行真实删除 |
| V27 | 查询在空结果时的返回 | Integration | 三个列表返回空集合；有效目录返回根对象及空 `ItemTypes` 集合，与公开契约一致，不返回 `null` 或异常 | 需要 | 不需要 | 空表或筛选无命中 |
| V28 | 四个查询在测试样本规模下的响应 | Integration | 一次返回完整结果，不分页（S1-D7） | 需要 | 不需要 | 在平台新表中独立准备 325 条合法项目及父级作为测试样本，不代表平台现存数据量，不读取或复制其他系统旧数据 |
| V29 | 两个合法请求创建同一新分类名称，验证唯一索引拒绝及异常翻译 | Integration | 恰好一条成功、另一条由实际唯一索引拒绝并返回可识别的业务冲突而非原生数据库错误，最终仅一条记录；记录翻译前数据库异常及约束名，失败路径不登记成功事件（S1-D5）。只有普通查重拒绝不算该分支通过 | 需要 | 需要 | 名称未占用、身份与参数合法；仅测试装配的真实仓储装饰器在两个请求均完成实际查重且结果为未占用后放行写入，等待须有超时并在失败时释放；写入和异常翻译仍走真实链路，不伪造数据库异常，不新增生产入口或跨行锁协议 |
| V30 | 平台新建三表的结构与真实持久化回读 | Integration | 实际使用三张 `mrec_medical_standard_*` 表，逐项核对三个实体全部字段的名称、类型、可空性、SQL 参数/结果列映射及响应字段；经真实 AppService/WorkUnit/Repository 创建、修改、启停并独立连接回读 ID、归属、类型、编码名称、备注、状态及操作字段，`MedicalItemType` 按 `Laboratory=0`、`Examination=1` 存取，布尔值、可空备注和时间点经 Provider 存取及响应序列化正确。核对分组父分类存在、项目父分类/父分组存在、项目分类等于所属分组分类；父级启停不改项目自身状态及操作字段。不存在旧表迁移、重命名、复制或历史类型转换 | 需要 | 需要 | 负责人授权受控库并确认执行本模块新建 DDL；用例通过正常业务路径独立准备目录数据，记录实际表映射；不以其他系统表或旧数据作基线 |
| V31 | **修改**分类为另一个全平台已存在的分类名称 | Integration | 拒绝保存，抛业务异常（INV-1 覆盖修改路径，非仅创建路径） | 需要 | 需要 | 2 个无下级分组的分类 |
| V32 | 核对分组更新 Request/Command 不含 `CategoryId`，额外携带该字段调用修改后读回 | Integration | 不提供改挂契约；额外字段若被传输层拒绝则零写入，若被忽略则保存可编辑字段；两种情况下原分类保持不变。覆盖有无下级项目 | 需要 | 需要 | 两分类，有下级和无下级分组各一 |
| V33 | **修改**分组名称，新名称仅在另一分类中存在 | Integration | 保存成功，查重只限已有分组的所属分类，不扩大到其他分类 | 需要 | 需要 | 另一分类有同名分组 |
| V34 | **修改**分组，名称与**原分类**内其他分组重名 | Integration | 拒绝保存（按可选排除条件排除自身后查重） | 需要 | 需要 | 原分类下有同名分组 |
| V35 | 修改分类名称与**自身原名相同** | Integration | 保存成功（查重须排除自身，不得把自己判为重复） | 需要 | 需要 | 1 个无下级分组的分类 |
| V36 | 分类、分组和项目备注整份保存，分别发送原备注与显式 `null` | Integration | 原值保存后仍可读回；`null` 清空后读回为 `null`，沿 Request→Command→SQL 核对，不使用字段省略表示保留 | 需要 | 需要 | 三类对象备注均非空 |
| V37 | 分类与分组查询返回「使用情况」派生值 | Integration | 存在下级（含已停用）标记已使用，无下级标记未使用；仅分类用其限制类型，不限制名称备注（S1-D21） | 需要 | 不需要 | 有下级与无下级的分类、分组各一 |
| V38 | 四个查询的排序稳定且唯一 | Integration | 分类列表按 `item_type/name/id`、分组列表按 `category_id/name/id`、项目列表按 `category_id/group_id/code/id` 升序；有效目录类型层按枚举数值升序，分类/分组/项目各自在其父级内按 `name/id` 升序。同名项目按 ID 兜底仅用于有效目录项目层；重复读取顺序一致，不以插入顺序代替排序 | 需要 | 不需要 | 两种类型、合法跨分类同名分组、同分组不同编码的同名项目且编码顺序与 ID 顺序相反；分类名及项目编码各自全局唯一 |
| V39 | 查询筛选入参为 `null` 与传空字符串的行为可区分 | Contract | `null`/未传表示不过滤；空字符串或纯空白被拒绝为参数校验失败（空值语义）。**2026-09-15 第 27 轮实测确认成立**（`ValidateOptionalText`），第 26 轮按 D1 的修改已撤回 | 不需要 | 不需要 | 实测记录见 [决策与结论](决策与结论-20260915.md) |
| V40 | 修改分类名称和备注，类型保持原值；覆盖启停分类、有无分组及停用下级分组 | Integration | 名称唯一时保存成功，读回名称备注和操作字段，登记事件；类型与启停不变 | 需要 | 需要 | 各状态分类及分组 |
| V41 | 修改分组名称和备注；覆盖自身和父级启停、有无下级项目及停用下级 | Integration | 名称在原分类内唯一时保存成功，读回名称备注和操作字段，登记事件；归属和启停不变 | 需要 | 需要 | 各状态分类、分组、项目 |
| V43 | 创建标准项目，读取时分组所属分类与请求分类不一致 | Integration | 拒绝保存且零写入（INV-6） | 需要 | 需要 | 分属不同分类的分组 |
| V44 | 修改项目备注为非空新值 | Integration | 保存成功，持久化后可读回新值；编码/名称/分类/分组/启停不变（INV-9） | 需要 | 需要 | 备注非空的项目 |
| V45 | 标准项目独立启停与重复启停，覆盖停用父级 | Integration | 首次改变项目自身状态并登记事件；重复执行按成功返回且不登记事件（INV-11）。分类停用或分组停用时仍可成功启用已停用项目，项目自身变为启用、父级状态不变，仍不属于当前有效目录；父级均启用时项目启用后当前有效，不把新增的父级启用限制用于独立启用 | 需要 | 需要 | 各分支正常创建目录后停用项目，再分别保持父级均启用、仅分类停用、仅分组停用的独立起点；每个起点验证首次启用与再次启用，保留正常停用及重复停用路径；不先恢复停用父级，不并发或直接改库造数 |
| V46 | 创建标准项目，编码与**已停用**项目重复 | Integration | 拒绝保存并抛业务异常（编码全平台唯一，不因停用而释放；不得退化为数据库唯一索引错误） | 需要 | 需要 | 1 条已停用项目 |
| V47 | 操作者身份缺失或非法时写入 | Integration | 拒绝该写请求并抛业务异常，**零写入**（`OperId` 不得静默落 `Guid.Empty`） | 需要 | 需要 | 无 |
| V48 | 必填名称/编码缺失、空串、纯空白；必填 ID 缺失/空 Guid；类型缺失或未定义枚举；可选查询 ID 为显式空 Guid | Contract | 各非法输入均被拒绝，不能让默认值绕过必填或值域校验；可选查询 ID 的 null/未传仍按不过滤；参数错误与业务拒绝消息可区分。**2026-09-15 负责人裁定（方案一）**：`itemType` 为可空枚举，**未传时按默认 `Laboratory=0` 处理**，不再要求「类型缺失被拒绝」；仅未定义枚举值（如 `7`）拒绝 | 不需要 | 不需要 | 各请求使用独立合法基线，仅替换一个待验证字段。裁定记录见 [决策与结论](决策与结论-20260915.md) |
| V49 | 有效目录查询按 `ItemType` 与 `CategoryName` 筛选，含字面符号 | Integration | 两个入参按 UML 声明生效；未传时不过滤；`CategoryName` 按字面包含匹配，`A_B` 不匹配 `AXB`、`A%B` 不匹配 `AXXB`，但命中包含相应字面片段的分类；单个 `%` 或 `_` 不变成匹配全部；组合为与关系，仍只返回三级均启用项目 | 需要 | 不需要 | 多类型多分类数据，分类名分别包含 `A_B`、`A%B` 及无符号对照，各有合法有效项目，另有停用链路 |
| V50 | 修改已停用分组的名称备注后重新启用，观察自身已停用项目 | Integration | 分组恢复启用、资料修改保留；项目自身仍停用，不构成当前有效 | 需要 | 需要 | 独立创建目录，保持分类启用，分别停用分组和项目 |
| V51 | 记录不存在时的写操作 | Integration | 抛业务异常，零写入（修改、启停、改备注各一条同形路径） | 需要 | 需要 | 无 |
| V52 | 分类、分组、项目备注提交与现值完全相同的可编辑资料 | Integration | 仍执行校验和保存，刷新可信 `OperId/OperTime` 并登记对应修改事件；无未变更短路。与重复启停不写入的 V16/V17/V45 区分 | 需要 | 需要 | 三类既有对象，操作时间早于本次请求 |
| V53 | 12 个写入口工作单元声明及查询服务契约分层 | Architecture | WorkUnit/UseTransaction 与事务表逐项一致；公开查询接口、请求和 ReadModel 位于 Contracts/Queries，接口继承 IApplicationService；Application 的 MedicalRecognitionReportQueryAppService 实现该接口并依赖 Domain 查询端口；公开契约不依赖 Domain/Repository 内部类型，Application 不依赖 Repository 私有投影；无自定义事务生命周期或禁止 API | 不需要 | 不需要 | 构建后的类型及本模块源码 |
| V54 | 创建分组、创建项目经真实 AppService/WorkUnit 入口，分别验证数据库回滚与已登记事件清理 | Integration | A：真实仓储写入后抛异常，请求失败，独立连接读回零新增、父级与既有数据不变。B：先观察并记录对应成功事件已登记，再在提交前注入失败，核对工作单元失败后的事件清理及零提交；从未登记的空队列不能证明清理成功 | 需要 | 需要 | A 使用测试侧真实仓储装饰器写入后抛异常。B 仅使用实施探测确认的框架安全观察点及测试侧提交前失败注入点，不新增生产测试入口、不手工操纵事件队列；无安全注入或观察条件时，仅 B 的框架事件观察面记 N/A 并说明依据，A 仍必须执行 |
| V56 | 正常保存、同值保存、业务拒绝及重复启停的领域事件登记 | Unit | 成功及同值保存各登记对应事件，字段与对象/操作人/操作时间一致；业务拒绝、仓储失败和重复启停不登记成功事件 | 不需要 | 不需要 | 领域单测使用只读事件观察点，仓储替身只服务登记分支，不替代真实持久化测试 |
| V57 | 三表新建 DDL 与 SqlMap 物理映射静态核对 | Contract | 物理表名严格为本节三张 `mrec_` 表，DDL 与本模块全部读写 SQL 引用一致；实体名不加前缀，物理名替换不改变逻辑 Scope/sqlId，不新增映射特性或全局映射器。列序、类型、可空性、表/列元数据注释符合 S1-D30 与本节定义，三条业务唯一索引覆盖全部行含停用，无旧表迁移/复制引用；逐文件核对下表列出的全部 Statement，另以固定字符串扫描确认不存在旧物理表名 | 不需要 | 不需要 | 三份新建脚本、本模块 SqlMap/仓储调用、UML Entity 与公共字段约定；仅静态证据，不替代真实物理表回读 |
| V58 | 受控库新建 DDL 元数据核对 | Integration | 在无同名目标表的受控库执行本模块新建 DDL，读取元数据逐表核对物理名称、列清单及顺序、类型、可空性、主键、表/列注释与设计一致；三条唯一索引的键列和唯一属性正确，无仅启用过滤条件。不保留旧表索引例外，不执行旧表注释迁移/回退、重命名或数据复制 | 需要 | 不需要 | 负责人独立授权受控库并确认新建执行；schema 由负责人提供，SqlMap 与 DDL 必须解析到同一已确认对象；只针对本平台新表，不触碰其他系统对象 |

## 验证与实施说明

- V42 编号退役，不复用、不计入待执行用例；其跨行竞态由 S1-D23 记录为 `AcceptedRisk`，不代表已经验证。
- V55 历史类型迁移防御用例随独立新建方案退役，不复用编号、不计入待执行用例，也不标为通过；有效矩阵为 V1–V58 排除 V42/V55，共 56 条。
- 有效目录操作字段随 V26 核对契约，随 V30 核对真实存取和序列化：`OperId/OperTime` 等于项目行值，响应不再含 `LastUpdatedTime`；父级启停前后项目操作字段保持不变。
- 事件登记与发布分开验证：V56 使用框架已有可读事件集合/捕获能力，必要时仅测试代码反射观察队列，不增加生产查询接口；集成用例以同一观察方式核对登记，并独立核对数据库。V54-A 的失败发生在成功事件登记前，只证明数据库回滚；V54-B 必须有“已登记→提交前失败→失败后队列”的可观察证据，不能用 A 的空队列替代。确无安全注入或观察条件时仅 B 的框架事件观察项记 `N/A` 并附依据，数据库回滚仍必须验证，不宣称“未发布”即“未登记”。

- 本矩阵的 `真实数据库` 与 `真实 WorkUnit` 列按项目测试规范标注；标为"需要"的用例必须在真实数据库与真实工作单元下执行，不使用内存替身。
- 依赖平台库表的数据库读、写验证在负责人确认相应环境的新表创建完成后继续，前置受阻时记为 `Blocked`，不阻塞独立设计、实现、脚本准备及不依赖数据库的检查。V30/V58 可在负责人独立授权的受控库验证，不等待目标库先建成；缺少授权或建表确认时只暂停受影响验证，未执行项保持 `NotRun`，不把脚本存在当作建表完成。
- V15 属契约层面的用例，通过请求契约不接受该字段来验证，不需要数据库。
- V31–V36 覆盖修改路径查重、固定归属及备注完整替换；V37–V39 覆盖 S1-D21 的派生展示与后端查询语义；V53/V54/V56 分别承接架构声明、真实工作单元回滚和领域事件登记。
- V26 与 V39 为契约用例，不需要数据库与工作单元。
- V57 为新建脚本与物理映射静态检查，V58 为新建 DDL 元数据验证，V30 为真实持久化回读；三者不能互相替代。业务唯一性拒绝继续由 V2/V8/V11/V29/V31/V34/V46 等用例验证，不把索引元数据正确当作完整业务链路通过。
- 本阶段不使用外部系统，因此矩阵中不存在外部集成用例。

## 本阶段查询的对外暴露范围

`QueryEffectiveMedicalStandardCatalog` 属 F01 明确要求的查询能力，本阶段**只交付后端查询与其契约**，不在本阶段页面上挂载入口：当前有效标准目录的消费方是互认与报告采集场景，其页面在阶段 2 及以后建立。

因此该查询的验证走接口契约测试与集成测试，不进入前端矩阵的页面级用例；[Client/design.md](../Client/design.md) 的接口依赖表已按此标注。
