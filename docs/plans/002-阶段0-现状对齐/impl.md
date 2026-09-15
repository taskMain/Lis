# 阶段 0 现状对齐实施记录

本阶段是纯规范阶段，只做基线盘点，不修改业务代码、不建表、不重新生成 API Client。设计见 [design.md](design.md)。

按 S0-D1/S0-D3，本文件承载阶段 0 的盘点与验证证据，不另建 `testReport.md`。下文取证结果保留为阶段 0 历史基线，不代表本轮重新运行或负责人已批准验收；后续实施指导以当前阶段 1 决策为准。

**2026-09-11 用户澄清覆盖旧归属推断**：本平台尚未建表。历史发现的三张无前缀 `medical_standard_*` 表是归属未确认的既存对象，不是本平台共享或既有资源；原“3/19 已建”、旧表迁移、整型布尔转换和保留其 14 个索引的方案不再执行。下文历史查询数量、类型、索引、数据行数与验证状态保留，但不证明本平台资源归属或初始化完成。当前按已确认的 `mrec_` 加已选业务基础表名独立新建，实体不加前缀且不强制推导表名，通过 SqlMap 显式对应；阶段 1 仅 3 表，后续 16 表随所属阶段。连接配置不变，初始化前确认目标数据库与 schema，由负责人执行最终 DDL，不动其他系统对象或数据。

## 1. 执行摘要

已把仓库现状核对到可据以决策的程度。整体状态是：**工具链与外壳可用，业务能力全部未实现**。

关键结论：

1. 后端六个项目可还原、可构建、可测试；目标框架 `net10.0`；构建 0 错误。
2. 生成物是**纯骨架**：16 个写命令已进入生成范围，其余 54 项操作被生成器局部阻断；**全部查询接口尚未生成**；校验层、事务边界、业务规则三项完全空白。
3. 仓储层只覆盖 4 个实体，其余 15 个实体没有任何仓储方法，也没有任何查询方法。
4. 历史按 19 个无前缀脚本表名查询，只发现 3 张归属未确认对象，且**与生成脚本的类型定义不一致**；生成 DDL 无外键、无索引、无唯一约束，这三表有 14 个索引，含三条唯一索引。观测保留，但不能据此认定平台已建 3 表或已有唯一性保障。
5. 阶段 0 记录的 SqlMap 语句解析失败尚无修复运行证据；当前方案已由阶段 1 S1-D1/S1-D2 选定，见 3.12 与 7.1，不再按“修法未确定”指导实施。
6. API Client 已生成、可构建、可类型检查，`kiota-lock.json` 状态合规；但当前契约只有写接口，**不能作为前端开发的稳定输入**。
7. 前端子应用接入模板完整，业务页面尚未展开，当前只有首页。
8. 工作区存在大量既有未提交改动，**不属于本阶段新增**，后续阶段必须避免覆盖。
9. 外部依赖与领域事件目前**只有配置与声明，没有调用实现和发布点**。
10. 宿主没有 `Program.cs`，启动入口由框架包注入，本项目对启动流程的可控点只有配置文件。
11. **UML 与代码的覆盖对照**：命令 16/33、查询 0/20、实体类 19/19 但仅 4 个有仓储写方法；原数据库表 3/19 是名称命中而非平台建设覆盖，已被用户澄清覆盖。代码侧没有 UML 之外的多余内容。详见 3.3 节。

## 2. 工具链与构建基线

### 2.1 工具链版本

| 工具 | 版本 |
|---|---|
| .NET SDK | `10.0.401` |
| Node.js | `v22.22.3` |
| pnpm | `10.33.0` |
| Kiota（.NET 全局工具） | `1.34.1` |

### 2.2 后端

命令与目录：

```powershell
cd E:\MedSync\Dy.MedicalRecognition\server
dotnet restore Dy.MedicalRecognition.slnx
dotnet build   Dy.MedicalRecognition.slnx --no-restore
dotnet test    Dy.MedicalRecognition.slnx --no-build
```

结果：

| 命令 | 结果 |
|---|---|
| `dotnet restore` | 成功，exit 0；7 个项目全部为最新 |
| `dotnet build` | 成功，exit 0；**0 个错误**，344 个警告 |
| `dotnet test` | 成功，exit 0；1 个测试通过，0 失败 |

警告构成：344 条全部为生成的 ReadModel `record` 非空属性触发，属生成器特性，不是业务代码缺陷。

### 2.3 前端与 API Client

命令与目录：

```powershell
cd E:\MedSync\Dy.MedicalRecognition\client
pnpm -F @dy/api-client-medical-recognition typecheck
pnpm -F @dy/api-client-medical-recognition build
pnpm -F dy-medical-recognition build
pnpm -F dy-medical-recognition lint
```

结果：

| 命令 | 结果 |
|---|---|
| API Client `typecheck` | 成功，exit 0 |
| API Client `build` | 成功，exit 0；`dist/index.js` 37.30 KB、`dist/index.cjs` 44.25 KB、`dist/index.d.ts` 与 `.d.cts` 各 49.51 KB |
| 前端 `build` | 成功，exit 0；`dist/index.html` 0.43 KB、CSS 0.07 KB、JS 772.62 KB（gzip 240.05 KB） |
| 前端 `lint` | 成功，exit 0；**0 个错误**，2 个警告 |

两个 lint 警告均为模板自带文件的 `react-refresh/only-export-components`，位置为 `src/contexts/ApiClientContext.tsx` 与 `src/router/index.tsx`。

前端构建的 chunk 体积警告与 lint 警告都不影响构建结果。

## 3. 后端现状

### 3.1 项目与依赖

后端为六个项目，目标框架 `net10.0`，均以 `Dy.*` 框架包为基础。

宿主项目 `Dy.MedicalRecognition` 的包引用：`Dy.Apron.Bridge`、`Dy.Apron.QuickStart`、`Dy.Apron.Security`、`Dy.Earthrace`、`Npgsql`、`Dy.Plugin.AspNet.Scalar`；项目引用 `Application`、`Domain`、`Repository`。

宿主项目**没有 `Program.cs`**，启动入口由 `Dy.Apron.QuickStart` 提供。

包版本与 SDK 版本的实际值、以及它们与生成器基准值的差异见 [Test Environment](../../../.agents/instructions/test-environment.md)。

### 3.2 生成覆盖

生成计划中 54 项操作被局部阻断，分布如下：

| 阻断类型 | 数量 | 说明 |
|---|---|---|
| 查询返回 ReadModel，当前不生成投影 | 20 | 全部查询接口 |
| 查询参数缺显式实体属性映射 | 17 | 与上一条叠加 |
| 命令含独立参数或未解析输入 | 16 | 报告版本、检验/检查内容明细、匹配、处理结果、引用、作废、金额 |
| 命令不是标准 CRUD/启用/禁用 | 7 | `Void*`、`Submit*`、`Save*` |
| FacadeCommand 组合语义 | 3 | 两个完整报告提交、匹配查询 |
| 无法确定 owner | 2 | 报告版本追加、处理结果提交 |

已进入生成范围的 16 个写命令全部属于标准目录与互认配置两个模块。本节只说明阻断分布，UML 定义与代码实际的逐项覆盖对照见 3.3 节。

### 3.3 UML 与代码的覆盖对照

本节把 `docs/uml/` 定义的内容与 `server/` 实际生成的代码逐项对照，用于判断"设计意图"与"已实现程度"之间的差距。UML 侧与代码侧均为**实测计数**。

#### 3.3.1 总量对照

| 维度 | UML 定义 | 代码实际 | 覆盖率 |
|---|---|---|---|
| 聚合 | 1 | 1 | 100% |
| 实体 | 19 | 19 个实体类 | 类 100%，**但只有 4 个有仓储写方法** |
| 枚举 | 13 | 13 | 100% |
| 命令 | 33 | 16 | **48%** |
| 查询 | 20 | 0 | **0%** |
| 领域事件 | 33 | 16 个事件类 | 48%，且 16 个**全部没有 raise 点** |
| ReadModel | 51 | 51 个契约 | 声明 100%，**0 个消费者** |
| 建表脚本 | 19 | 19 | 100% |
| 历史无前缀表名命中（非平台已建表） | 19 | 3 个归属未确认对象 | 原记 **16%**，不再作为平台覆盖率 |

代码侧**没有 UML 之外的冗余命令或实体**，对照差集只出现在"UML 有、代码无"这一侧。

#### 3.3.2 缺失的 17 个命令

成因单一：生成器只覆盖标准 CRUD，这些命令全部被局部阻断。阻断码分布为 `WSD_COMMAND_PARAMETER_UNSUPPORTED` 13 个、`WSD_FACADE_UNSUPPORTED` 3 个、`WSD_COMMAND_KIND_UNSUPPORTED` 1 个。

| 类别 | 命令 | 归属阶段 |
|---|---|---|
| 报告主体与版本 | `CreateOrGetMedicalRecognitionReportCommand` | 阶段 4 |
| 检验报告内容（4 个） | `CreateLaboratoryReportContentCommand`、`CreateLaboratoryResultItemsCommand`、`CreateLaboratoryBacteriaResultsCommand`、`CreateLaboratoryAntimicrobialSusceptibilityResultsCommand` | 阶段 4 |
| 检查报告内容（3 个） | `CreateExaminationReportContentCommand`、`CreateExaminationItemsCommand`、`CreateExaminationSitesCommand` | 阶段 4 |
| 完整报告提交（2 个 Facade） | `SubmitCompleteLaboratoryReportCommand`、`SubmitCompleteExaminationReportCommand` | 阶段 4 |
| 报告作废（2 个） | `VoidLaboratoryReportCommand`、`VoidExaminationReportCommand` | 阶段 4 |
| 匹配（3 个） | `RequestRecognitionMatchesCommand`、`CreateRecognitionMatchRecordCommand`、`CreateRecognitionMatchItemsCommand` | 阶段 5 |
| 处理结果与引用（2 个） | `SubmitRecognitionProcessingResultsCommand`、`SubmitRecognitionReferencesCommand` | 阶段 5 |
| 金额 | `SaveOrganizationHospitalBranchRecognitionAmountCommand` | 阶段 3 |

#### 3.3.3 缺失的 20 个查询

全部查询没有实现，阻断码分布为 `WSD_QUERY_PARAMETER_MAPPING_REQUIRED` 17 个、`WSD_READMODEL_UNSUPPORTED` 3 个。按业务面分布：

| 业务面 | 查询 | 归属阶段 |
|---|---|---|
| 标准目录（4 个） | `QueryMedicalStandardCategoryList`、`QueryMedicalStandardGroupList`、`QueryMedicalStandardItemList`、`QueryEffectiveMedicalStandardCatalog` | 阶段 1 |
| 互认配置与金额（2 个） | `QueryRecognitionProjectConfigurationList`、`QueryRecognitionAmountList` | 阶段 2、阶段 3 |
| 报告（4 个） | `QueryMedicalReportList`、`QueryMedicalReportVersionList`、`GetMedicalReportVersionDetail`、`GetMedicalReportVersionPdf` | 阶段 4 |
| 引用详情（1 个） | `GetRecognitionCitationDetailApi` | 阶段 5 |
| 外部系统（3 个） | `GetOrganizationHierarchy`、`QueryCitationDetailValidityDurationParameter`、`QueryOwnHospitalMatchParameters` | 按首个使用它的阶段 |
| 统计与导出（6 个） | `QueryRecognitionUsageSummary`、`QueryRecognitionUsageDetails`、`QuerySourceRecognitionSummary`、`QuerySourceRecognitionDetails`、`GetRecognitionStatisticsExport`、`GetRecognitionMatchRecord` | 阶段 6 |

因此对照的 51 个 ReadModel 在代码侧**没有任何消费者**，与 3.5 节的结论一致。

#### 3.3.4 实体的支持程度差异

| 支持程度 | 数量 | 实体 |
|---|---|---|
| 实体类 + 仓储写方法，历史命中同基础名对象但归属未确认，平台表未建 | 3 | `MedicalStandardCategory`、`MedicalStandardGroup`、`MedicalStandardItem` |
| 实体类 + 仓储写方法，平台表未建 | 1 | `MutualRecognitionItem` |
| 仅实体类，无仓储方法，平台表未建 | 15 | `OrganizationHospitalBranchRecognitionAmount`、`PlatformPatient`、`MedicalRecognitionReport`、`MedicalReportVersion`、`LaboratoryReportContent`、`LaboratoryResultItem`、`LaboratoryBacteriaResult`、`LaboratoryAntimicrobialSusceptibility`、`ExaminationReportContent`、`ExaminationItem`、`ExaminationSite`、`RecognitionMatchRecord`、`RecognitionMatchItem`、`RecognitionProcessingResult`、`RecognitionReference` |

`MedicalStandardItem.xml` 的 `<X>Columns` 与 `QueryAll<X>` 语句虽然存在，但没有任何仓储方法调用它们，属未接通状态。

#### 3.3.5 进度结论

代码侧当时只有**标准目录与互认配置两个模块的写命令骨架**：实体与契约齐全，但业务规则、入口校验、事务边界全部缺失（见 3.4、3.5 节）；查询能力为零；原“19 张表只建了 3 张”推断撤销，用户已澄清本平台尚未建表；外部依赖与领域事件只有配置和声明。没有 UML 之外的多余命令或实体。

按总体计划的阶段划分，这大致对应：阶段 1（F01）有骨架、缺规则与查询；阶段 2（F02）有骨架、缺表与查询；阶段 3 至阶段 7 基本为空。**本阶段不修复，差距按 7.2 节归属后续阶段。**

### 3.4 领域与管理层现状
领域管理器当前是**薄封装**：把命令映射为实体、生成主键、调用仓储、追加领域事件，然后返回结果。以标准目录与互认配置模块为例，全部方法都遵循这一模式，**没有任何业务规则校验**——既没有唯一性校验（分类名称、分组名称、项目编码、组织与项目组合），也没有前置条件校验（分类/分组必须存在且启用、分组必须属于分类）或状态流转阻断。这些规则在 SRS 中有明确规定，属各业务阶段的实现内容。

全库搜索的确认结果：

| 检查项 | 命中数 | 结论 |
|---|---|---|
| 数据校验特性（`[Required]`、`[StringLength]`、`[MaxLength]`、`[Range(`、`[RegularExpression`、`IValidatableObject`） | 0 | 请求模型无任何声明式校验 |
| 事务边界标注（`WorkUnit`、`UseTransaction`） | 0 | 应用服务与领域服务均未标注事务边界 |

应用服务当前只做请求到命令的映射、注入操作人与操作时间，然后调用领域管理器，没有入口校验。

### 3.5 应用契约现状

| 项目 | `.cs` 文件数 | 代码行数 | 构成 |
|---|---|---|---|
| Application.Contracts | 88 | 3522 | 16 个 Request、19 个 Dto、51 个 ReadModel、1 个 AppService 接口 |
| Application | 4 | 206 | 1 个 AppService（147 行）、1 个 DataMaps（41 行）、2 个模块文件 |
| Domain | 39 | 1930 | 19 个实体、16 个 Command、1 个 Manager、1 个 Repository 接口 |

**四项"完全空白"的基线事实**（均已通过独立搜索复核）：

1. **校验层空白**：Requests 与 Dtos 中所有特性行共 35 处，**全部是 `[IPropertyChangedAware]`**；ReadModels 无任何特性。未发现 `[Required]`、`[StringLength]`、`[Range]`、`[RegularExpression]`、`IValidatableObject`，也未发现 FluentValidation 等替代方案。
2. **查询侧空白**：51 个 ReadModel（1991 行）在 Application、Domain、Repository、Host 中**零引用**，是孤立的契约声明；全项目无 Query 方法、无 Query 类型、无 QueryHandler。AppService 接口的 16 个方法签名全部为 `Task<bool> XxxAsync(XxxRequest request)`。
3. **事务空白**：三个项目中 `[WorkUnit]`、`UseTransaction`、`[Transactional]` 全部零命中。
4. **业务规则空白**：`MedicalRecognitionReportManager.cs` 全文**无 `throw`、无 `if`**；16 个方法只做"命令映射实体 → 生成 Id → 调用仓储 → 收集事件 → 返回 `result > 0`"。

**操作人注入已贯通但不可靠**：AppService 的 16 个方法各自执行 `if (Guid.TryParse(HttpRequestInfo?.UserId, out var userId))` 后赋值 `OperId`，**解析失败时静默跳过**，实体保留 `Guid.Empty`；操作时间统一取 `DateTimeOffset.Now`。Manager 自身不读取 `OperId`/`OperTime`，仅在生成事件时透传。

**Command 家族形态分裂**：16 个 Command 中，8 个 Create/Update/Change 类为 `partial record` + `[IPropertyChangedAware]`，另 8 个 Enable/Disable 类两者都没有。同时全部 16 个 Command 都 `using` 了 `...MedicalRecognitionReportAggregate.Events`，而该命名空间在 Domain 项目内不存在（事件类型实际位于 `Domain.Share`）。

**仓储接口签名分裂**：`IMedicalRecognitionReportRepository` 的 Create/Update/Change 四类共 8 个方法接收**实体**参数，Enable/Disable 共 8 个方法接收 **Command 对象**参数。

**已确认的良好项**：文件名与类型名 100% 一致；属性中文注释完整；无 `var _xxx` 局部变量、无制表符与行尾空白、无 `TODO`/`NotImplementedException`。

**已确认的风格问题**：3 个私有字段全部未使用下划线前缀；162 处不可为空 `string` 属性未初始化（Nullability 已启用）；构造函数参数名与字段名相同而需 `this.` 消歧；`MedicalRecognitionReportAppService` 注入的 `repository` 字段从未被使用。

### 3.6 仓储层现状

全仓库只有 **1 个仓储实现类** `MedicalRecognitionReportRepository`（29 行、17 个成员），对应唯一接口 `IMedicalRecognitionReportRepository`（位于 Domain 项目）。模块类 `MedicalRecognitionRepositoryModule` 标注 `[Earthrace(IsDefault = true)]`。

| 检查项 | 事实 |
|---|---|
| `IDataMapper` 调用是否显式传 `sqlId` | **16/16 全部显式传**，0 处依赖推断；唯一无 `sqlId` 的是 `CreateGuid()` |
| `SetContext` 调用位置 | 唯一一处，在 `DataMapper` 属性 setter 内，传参 `"MedicalRecognitionReport"`，与 19 个 SqlMap 的 `Scope` 一致 |
| 方法体形态 | 全部为表达式体单行委托，无业务逻辑、无事务、无异常处理 |
| 参数类型 | 实体参数使用全限定名，命令参数使用短名 |
| 查询类方法 | **无**。接口与实现均无 Get/Query/Find/List 方法 |

**仓储方法只覆盖 4 个实体**：`MedicalStandardCategory`、`MedicalStandardGroup`、`MedicalStandardItem`、`MutualRecognitionItem`，各 4 个写方法。其余 **15 个实体没有任何仓储方法**，其 SqlMap 只有 `Columns` 与无 `where` 条件的全表 `QueryAll`，既无写语句也无查询入口。

### 3.7 数据层对应关系

19 个实体、19 个 SqlMap、19 个表脚本**一一对应，零命名缺口**。表名由 `EarthraceConfig.json` 的 `DelimiterConverter`（分隔符 `_` 转 Pascal）约定推断，**实体上没有任何 `[Table]` 特性**。

SqlMap 组成（逐文件精确统计）：

| 组成 | 数量 |
|---|---|
| 文件总数 | 19，`Scope` 全部为 `MedicalRecognitionReport` |
| `<X>Columns` | 19，每文件 1 个 |
| `QueryAll<X>` | 19，每文件 1 个，**均为无 `where` 条件的全表查询** |
| 写语句 | 16，分布在 4 个文件（`MedicalStandardCategory`、`MedicalStandardGroup`、`MedicalStandardItem`、`MutualRecognitionItem`），每文件 4 个 |
| 合计 | 54 |
| `delete` 语句 | **0 个** |

DDL 脚本历史特征：19 个脚本全部为 PostgreSQL `create table if not exists`，每脚本 1 张表，全部以 `id` 为主键；**无外键、无索引、无唯一约束**（三项搜索均 0 命中）。这只证明当时生成脚本未定义这些约束；3.9 三个归属未确认对象的索引不构成本平台保障。平台新表须在所属阶段按业务契约核对所需索引与约束，不以旧对象索引替代。

### 3.8 共享层现状

`Domain.Share` 提供 13 个枚举与 16 个领域事件。

枚举：`ConfigurationStatus`(2)、`LaboratoryAbnormalFlag`(4)、`LaboratoryResultType`(3)、`MedicalItemType`(2)、`MedicalReportLifecycleStatus`(2)、`MedicalReportType`(2)、`RecognitionNonAdoptionReason`(6)、`RecognitionResult`(2)、`RecognitionStatisticsExportType`(7)、`RecognitionStatisticsGroupDimension`(4)、`RecognitionUsageDetailType`(4)、`SourceImageStatus`(3)、`VisitType`(5)。

领域事件：16 个，全部为 `public record XxxEvent : DomainEvent`，**全部同时覆写 `AggregateId` 与 `EventType`**，无遗漏。事件与命令、仓储方法严格配对（4 个实体 × 4 类操作）。

**关键事实**：`AggregateId` 全部取同一个常量 `MedicalRecognitionReportConst.AggregateId`，即**整个聚合共用同一个聚合 ID 字符串**。全部 16 个事件**没有任何 raise 点**——事件由 Manager 通过 `AddEvent(...)` 收集，但没有可见的发布或订阅实现。

### 3.9 数据库现状

历史按 19 个无前缀脚本表名查询，只有 3 个名称命中；以下为历史观测，不是本平台已建表清单：

| 表 | 状态 |
|---|---|
| `medical_standard_category`、`medical_standard_group`、`medical_standard_item` | 历史存在，归属未确认 |
| 其余 16 个无前缀脚本表名 | 历史查询未发现 |

**已存在的 3 张表与生成脚本的差异**（逐列比对）：

| 项 | 库中实际 | 生成脚本 | 影响 |
|---|---|---|---|
| `is_valid` | `integer not null` | `boolean not null` | **阻断写入**：实测 `boolean` 写入 `integer` 列报 `42804 字段类型为 integer，但表达式类型为 boolean` |
| `oper_time` | `timestamp without time zone not null` | `timestamptz not null` | **不阻断**：实测 `current_timestamp` 与带时区字面量均可正常写入该列 |
| 列集合与顺序 | `medical_standard_item` 顺序为 `id, code, name, category_id, group_id, is_valid, remark, oper_id, oper_time` | 顺序为 `id, category_id, group_id, code, name, ...` | 顺序不同不影响按列名映射 |
| 索引与约束 | 3 张表共有 **14 个索引**，含唯一索引 `ux_medical_standard_category_name`、`ux_medical_standard_group_category_name`、`ux_medical_standard_item_code`，以及 `is_valid`、外键列等查询索引 | **脚本无任何索引与唯一约束** | 原“提供本平台 SRS F01 保障”推断已撤销；仅证明这些对象当时有索引 |
| 数据 | 分类 3 行、分组 26 行、项目 325 行；`is_valid` 全为 `1` | 无 | 原“真实平台标准目录数据”归属推断已撤销；行数观测保留，不授权使用或迁移 |

生成脚本使用 `create table if not exists`，因此**重复执行不会修正已存在的表**。这 3 张表是生成脚本产出之前的既有对象，带有生成器未产出的约束与索引，且已有数据，不属于本项目生成物。

历史结论曾将这 3 张表认作“既有平台资源”，并提出只处理 `is_valid` 类型差异；该归属与实施推断已被用户澄清覆盖。当前不得覆盖、重建、迁移或修改这些归属未确认对象，本平台另建独立 `mrec_` 表。上述类型报错与时间写入观测不证明新平台表的读写行为。

### 3.10 宿主配置与启动入口

宿主项目只有 5 个自有文件：`.csproj`、两个 `appsettings`、`EarthraceConfig.json`、`Properties/launchSettings.json`。

**启动入口不在本项目内**：宿主**没有 `Program.cs`**，也没有任何自建启动代码（`static Main`、`WebApplication.CreateBuilder`、`Host.CreateDefaultBuilder` 等全部 0 命中）。启动入口由 NuGet 包 `Dy.Apron.QuickStart` 通过 MSBuild 注入——该包的 `build/Program.cs` 内容为 `await Dy.Apron.Startup.RunAsync();`，并由同包 `.targets` 以 `<Compile Remove="Program.cs" />` 与 `<Compile Include="...\Program.cs" />` 替换本项目的编译输入。同一 targets 还会在宿主缺少 `appsettings.json` 时从包内复制一份骨架，当前两份 appsettings 的初始骨架即来源于此，之后被本项目改写。

结论：**本项目对启动流程与框架装配的可控点只有配置文件**；需要改动启动行为时必须先确认 `Dy.Apron.QuickStart` 提供的扩展点，不能假设可以像常规 ASP.NET Core 项目那样在 `Program.cs` 中插入代码。

配置侧已确认：

1. 监听地址三处一致，均为 `http://localhost:5008`（`appsettings.json`、`appsettings.Development.json`、`Properties/launchSettings.json`）。`launchSettings.json` 只有 1 个 profile，`launchUrl` 为 `scalar`。
2. 不再使用 MQ 与 Redis，开发配置中不包含对应配置节。
3. `EnableOpenApi` 在两个文件中类型不同（基文件为布尔 `true`，开发文件为字符串 `"true"`），`Protocols` 也不同（`Http1AndHttp2` 与 `Http1AndHttp2AndHttp3`）；这些属于框架生成骨架的既有差异，本阶段只记录不修改。
4. `HttpConfig.HttpServiceConfigs` 以**接口完整名**为键，共 5 条远程服务（用户信息、组织信息、系统参数、字典、推送）；`FileServer`、`ExternalPush` 与 `HttpConfig.WrapResult` 仅出现在开发配置中。
5. 敏感值（`JwtSettings` 的密钥、`EarthraceConfig.json` 的数据库连接串）存在，本文不复制；连接串为 PostgreSQL 形式，`Database.Write.ConnectionString` 通过 `${DbConnection}` 占位符引用，不是第二份明文。连接串所在文件的凭据口径见 [Test Environment](../../../.agents/instructions/test-environment.md)。

### 3.11 外部依赖

| 能力 | 配置项 | 调用实现 |
|---|---|---|
| 可信组织服务 | 有 | 未发现 |
| 系统参数服务 | 有 | 未发现 |
| 字典服务 | 有 | 未发现 |
| 用户信息服务 | 有 | 未发现 |
| 文件服务（PDF 上传下载） | 有 | 未发现 |

结论：外部依赖目前**只有配置，没有调用代码**。在全部 server 端 C# 源码中搜索组织信息服务、系统参数服务、字典服务、用户信息服务、文件服务相关的接口名与配置键，命中数为 0；这些名称只出现在 `appsettings.Development.json` 的 `HttpConfig.HttpServiceConfigs` 配置节中。

### 3.12 SqlMap 解析阻断的历史取证与推论边界

本阶段曾反编译 `Dy.Earthrace` 1.0.0.60 并对照同类可用项目。以下保留当时取证与推论的来源，但不能据此认定聚合作用域就是根因；原异常 `Can not find Statement. FullSqlId:MedicalRecognitionReport.CreateMedicalStandardCategory` 只证明该键未找到语句。本轮不重新反编译、不重跑接口；当前执行依据为阶段 1 S1-D1/S1-D2。

**历史框架取证记录**（当时反编译 `Dy.Earthrace` 得到，不等于本项目运行时查找路径已验证）：

1. `EntityMetaDataCache<TEntity>` 的静态构造按实体类型推导元数据：`TableName` 取实体上的 `[Table]` 特性，无特性则取**实体类名**；`Scope` 取实体上的 `[Scope]` 特性，无特性则**回退为 `TableName`**。本项目实体没有任何 `[Table]` / `[Scope]` 特性，因此**每个实体的 scope 等于其类名**。
2. `DataMapper.SetContext(scope)` 把传入值存进 `DataMapper.Scope`；`InsertAsync`/`UpdateAsync` 等方法用 `scope ?? Scope` 决定本次调用的 scope，`sqlId` 参数带 `[CallerMemberName]` 默认值。
3. 语句查找键是 **`scope + "." + sqlId`**：`DataMapper.IsStatementExist`、`GetStatementContent`、`GetTagedParameters` 均按此拼装后调用 `SqlMap.GetStatement`，找不到即抛出本次观测到的 `Can not find Statement.FullSqlId:...`。
4. `CUDConfigBuilder` 会按**每个实体**的 scope 建立 SqlMap，并注入由 `CUDSqlGenerator` 生成的 `GetById`、`Insert`、`InsertReturnId`、`Update`、`DeleteById`、`DeleteAll`、`DeleteMany` 七个语句，键为这些短名。

**当时的推论及当前限定**（原推论已被替代，不是实施指令）：

| 环节 | 当时记录的生成物实际值 | 历史推论与当前限定 |
|---|---|---|
| SqlMap 的 `Scope` | 19 个文件全部写 `MedicalRecognitionReport`（聚合名） | 曾推断必须改为实体类名；共用作用域本身不证明失败，当前按 S1-D1 选定实体作用域并待运行验证 |
| 仓储 `SetContext` | `"MedicalRecognitionReport"`（聚合名） | 曾要求与实体 Scope 一致；当前 S1-D1 明确不调用 `SetContext`，每个调用点显式传 `scope` |
| 仓储传入的 `sqlId` | 显式传 `CreateMedicalStandardCategory` 等业务名 | 曾要求框架 CUD 短名或省略参数；该推论已被 S1-D2 替代，常规增改保留业务化 `sqlId`，与 XML Statement Id 对齐 |
| 语句是否存在于实际查找 scope | 当时由实体元数据推导实体 scope 下缺少对应语句，未取得运行时注册键清单 | 不能代替实际查找路径证据；阶段 1 首批次核对实际查找键与已注册键 |

历史“19 个实体共用聚合作用域，因此除聚合根外都找不到语句”的推论未排除显式 scope、映射器上下文和语句注册路径的影响，不能作为已证实根因。当前须区分语句未注册与注册在其他键下；候选解释不被运行探测支持时，暂停相关修复并修订设计。

**与同类可用项目的对照**（`Dy.MedicalStandardCatalog`）：其 `MedicalStandardItem.xml` 的 `Scope` 为 `MedicalStandardItem`（实体名），仓储 `SetContext("MedicalStandardItem")` 同名，`CreateMedicalStandardItemAsync` 内部调用 `InsertAsync(entity)` **不显式传 `sqlId`**，仅在需要自有语句时才显式传（如 `sqlId: "ExistsMedicalStandardItemCode"`、`QueryMedicalStandardItemPage`）。这与其可正常运行的状态一致。

**当前实施口径与未验证面**：阶段 0 没有修改任何 SqlMap、仓储或实体，也没有实际验证修法。阶段 1 S1-D1/S1-D2 已选定本模块写入 SqlMap 使用实体名作用域、每个 `DataMapper` 调用点显式传 `scope` 与业务化 `sqlId`，不调用 `SetContext`、不按实体拆仓储类；不得按上面的历史推论改用框架短名。阶段 1 仅处理三类标准目录实体，经本项目运行验证后，其余 16 个实体在各自阶段按适用结论处理。

## 4. API Client 与 OpenAPI 现状

### 4.1 固化 OpenAPI

| 项 | 值 |
|---|---|
| 路径 | `client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json` |
| OpenAPI 版本 | `3.1.1` |
| Title | `Dy.MedicalRecognition | v1` |
| Version | `1.0.0` |
| Paths | 17 |
| Schemas | 19 |
| 安全方案 | `Bearer` |

17 个 path 中 16 个为 `MedicalRecognitionReport` 分组的写命令，另 1 个为 `/auth/login`（文档中存在，生成时排除）。

**17 个 path 中没有任何查询接口。**

### 4.2 Kiota 状态

| 项 | 值 |
|---|---|
| `descriptionLocation` | `../openapi/medical-recognition.openapi.json`（包内稳定 OpenAPI） |
| `clientClassName` | `MedicalRecognitionClient` |
| `kiotaVersion` | `1.34.1` |
| `excludePatterns` | `/auth/login` |

状态符合稳定输入与登录接口排除的要求。

### 4.3 生成包

包名 `@dy/api-client-medical-recognition`，输出目录 `client/packages/api-client-medical-recognition`。

入口 `src/index.ts` 导出 `createMedicalRecognitionClient`、`MedicalRecognitionClient`、`api/index.js` 与 `models/index.js`，并提供带鉴权与异常拦截的便捷创建函数。

生成源码中未发现退化类型，整数字段正确生成为 `number`。

### 4.4 主要风险

1. 当前契约只有写接口且均为生成骨架，**不能作为前端开发的稳定输入**；每个业务阶段需在后端契约稳定后重新对齐。
2. 重新生成会覆盖 `src/` 下的手写入口文件，生成后必须核对并恢复手写内容；生成命令不得使用 `--clean-output`。
3. 生成包体积主要以 models 为主，契约扩大后需要按阶段筛选 tag 或 path，避免全量生成。

## 5. 前端现状

### 5.1 微前端接入

| 项 | 值 |
|---|---|
| 应用目录 | `client/apps/dy-medical-recognition` |
| 包名 | `dy-medical-recognition` |
| 微应用 code | `medical-recognition` |
| 独立运行路径 | `/subApps/medical-recognition` |
| 开发端口 | `3008` |
| 运行时配置 | `public/config.json`、`public/config.development.json` |
| API Client 上下文 | `src/contexts/ApiClientContext.tsx` |

已具备：`AuthProvider`、`ApiClientProvider`、通过 `createAuthenticatedAdapter` 获取鉴权与异常拦截、微前端 basename 优先使用宿主注入的基础路由、配置读取失败时回退到本地后端地址。

### 5.2 页面与路由

当前文件：`src/layouts/MainLayout.tsx`、`src/pages/Home.tsx`、`src/router/index.tsx`、`src/router/routes.tsx`、`src/main.tsx`、`src/index.css`、`src/vite-env.d.ts`。

当前路由只有一条：根路径 `/` 渲染 `MainLayout`，其 `index` 子路由渲染 `Home`。路由通过 `MicroSDK.setupRouter` 与宿主导航同步。

**业务页面缺口**（按 SRS「用户界面」章）：

| 缺口 | 归属阶段 |
|---|---|
| 互认项目分类分组管理、标准项目管理 | 阶段 1 |
| 互认项目管理 | 阶段 2 |
| 互认项目金额维护 | 阶段 3 |
| 报告管理与历史版本 | 阶段 4 |
| 接收医院互认使用统计、来源医院被互认统计、统计导出入口、查看互认匹配记录 | 阶段 6 |

阶段 5 为纯后端阶段，无页面缺口。

### 5.3 宿主与运行环境

宿主登录与开发地址拦截已在进入本阶段前实测生效，相关配置值见 [Test Environment](../../../.agents/instructions/test-environment.md)。本阶段未重复执行该验证。

## 6. 工作区状态

### 6.1 本阶段新增文件

- `docs/plans/002-阶段0-现状对齐/design.md`
- `docs/plans/002-阶段0-现状对齐/impl.md`

与既有改动隔离，未修改任何既有文件。

### 6.2 既有非本阶段改动

工作区存在大量进入本阶段前就有的改动，**不属于本阶段新增**，后续阶段不得覆盖或回退：

| 类别 | 内容 |
|---|---|
| 规范与业务文档 | `AGENTS.md`、`CONTEXT.md`、`docs/需求规约SRS.md`、`docs/uml/*.wsd`、`docs/流程图.md`、`docs/业务流程图.md` |
| 删除项 | `docs/uml/6-MedicalRecognitionCommandContracts.puml`、`docs/uml/TODO.md`、`.claude/skills/*`、`.codex/skills/*` 等 |
| 子代理配置 | `.codex/agents/*.toml` |
| 忽略规则 | `.gitignore` |
| 新增未跟踪目录 | `.agents/`、`client/`、`server/`、`docs/plans/`、`.dsh/`、`.openclaw/`、`.zcode/` 等 |
| 新增未跟踪文件 | `docs/uml/6-MedicalRecognitionCommandContracts.Meta.puml`、`docs/uml/命令出参整改矩阵.md`、`.gitnexusrc` |

### 6.3 对后续阶段的影响

1. `server/` 与 `client/` 整体为未跟踪状态，后续阶段的代码改动不会与既有提交冲突，但**提交动作需要项目负责人授权**。
2. `.agents/` 目录被仓库忽略规则排除，其中的规范改动不会被提交。
3. 后续阶段开始前应先核对本表，确认没有把既有改动误认为本阶段产出。

## 7. 后续阶段处理建议

### 7.1 阶段 1 内优先处理事项

1. **按已选定的 S1-D1/S1-D2 验证 SqlMap 修法**：3.12 保留的是历史取证与候选解释，不是根因定论。本模块写入 SqlMap 使用实体名作用域，每个 `DataMapper` 调用点显式传 `scope`，保留业务化 `sqlId`，不调用 `SetContext`、不按实体拆仓储类；实施首批次先核对实际查找键与已注册键，验证后供其余 16 个实体在各自阶段沿用。
2. **独立新建平台三表**：当前按用户澄清新建 `mrec_medical_standard_category`、`mrec_medical_standard_group`、`mrec_medical_standard_item`，保留现有脚本基础名并通过 SqlMap 显式对应实体。历史实测 `boolean` 写入无前缀对象的 `integer` 列报 `42804`，不再作为本平台迁移前提。旧 S1-D3/S1-D4 三表迁移指导已被覆盖，以下仅保留历史决策背景，均不再执行。

   历史盘点观测到三表分类 3、分组 26、项目 325 行及 14 个索引，但“承载本平台数据”的归属推断错误。当时列出的选项如下，全部已被独立新建方案替代：

   - **(a) 改造既有表的 `is_valid` 列为 `boolean`**，值 `1 → true`，并同步带 `is_valid` 的 3 个索引。保留既有数据与全部索引，与生成物类型对齐。
   - **(b) 保留 `integer` 列，调整实体属性类型与 SqlMap 的启停语句**为整数 `1/0`。改动落在生成物侧，但会使实体类型与 SRS 的布尔语义表达不一致；且生成的 Enable/Disable 语句硬编码 `is_valid = true/false`，需要一并改写。
   - **(c) 改列名并存**：为互认平台建立独立的同名语义列（如 `platform_is_valid`），与既有列并存。避免改动既有数据，但会引入两套状态含义。

   原共享消费方清点、兼容切换、整型布尔转换与索引保留的迁移放行计划一并撤销，不再对这些对象实施。当前连接配置不变，实际初始化前须由负责人确认目标数据库与 schema，核对阶段 1 最终 DDL（含 S1-D30 顺序及中文注释）与独立表映射，再由负责人执行；完成确认前暂停依赖新表的集成测试，不修改其他系统对象或数据。

### 7.2 按阶段归属的差距

| 差距 | 归属阶段 | 说明 |
|---|---|---|
| 全部查询接口未生成（含标准目录查询） | 阶段 1 | 阶段 1 需补齐本模块查询能力 |
| 标准目录的领域规则、请求校验、事务边界缺失 | 阶段 1 | 当前为"命令映射实体后直接调用仓储"的薄封装 |
| SqlMap 作用域与语句 ID 解析 | 阶段 1（结论供后续沿用） | 阻断业务接口 |
| 平台标准目录 3 表尚未建立 | 阶段 1 | 独立新建 `mrec_` 表；历史对象类型差异不再是迁移任务，见 7.1 |
| 15 个实体无仓储方法（含聚合根与全部查询） | 按首次使用它的阶段 | 阶段 4 起必须为报告相关实体补仓储与查询方法 |
| 生成脚本未定义外键、索引与唯一约束 | 按首次需要该约束的阶段 | 新表按业务契约设计与核对所需约束，不继承归属未确认三表的索引 |
| 互认配置模块的表、规则、查询与页面 | 阶段 2 | — |
| 金额维护模块的表、规则、查询与页面 | 阶段 3 | — |
| 报告生命周期的 10 个实体、表与 Facade 事务语义 | 阶段 4 | 本项目工作量最大的阶段 |
| 匹配、处理结果、引用（含全部查询） | 阶段 5 | 纯后端 |
| 统计与导出 | 阶段 6 | 无新表 |
| 外部依赖调用实现（组织、参数、字典、用户信息、文件服务、推送） | 按首个使用它的阶段 | 当前只有配置，无任何调用代码 |
| 领域事件的发布与订阅实现 | 按首个依赖事件行为的阶段 | 16 个事件已定义但无 raise 点 |
| 脱敏、数据权限、组织范围的跨模块一致性 | 阶段 7 | 天然横向 |

### 7.3 不建议在本阶段或近期处理的事项

1. 不为批量修正全部 19 个实体的 SqlMap 而单设阶段；按模块随各自阶段处理。
2. 不预先重生成 API Client；等阶段 1 契约稳定后按本模块范围重生成。
3. 不做重构式代码治理；代码风格与薄封装问题在所属业务阶段内按最小修复处理。

## 8. 验证结果汇总

| 验证面 | 状态 | 证据 |
|---|---|---|
| 后端还原 | `Passed` | `dotnet restore` exit 0 |
| 后端构建 | `Passed` | `dotnet build` exit 0，0 错误、344 警告 |
| 后端测试 | `Passed` | `dotnet test` 通过 1、失败 0 |
| API Client 类型检查 | `Passed` | `typecheck` exit 0 |
| API Client 构建 | `Passed` | `build` exit 0，产物 4 个 |
| 前端构建 | `Passed` | `build` exit 0 |
| 前端静态检查 | `Passed` | `lint` 0 错误、2 警告 |
| 数据库现状核对（历史） | `Passed` | 当时只读查询比对 19 个无前缀表名；仅保留观测结果，不证明平台资源归属或当前初始化完成 |
| 业务功能行为 | `N/A` | 本阶段不实现功能，不做行为验收 |
| 宿主页面验收 | `N/A` | 本阶段不新增页面 |

本阶段为纯规范阶段，未执行功能验收；以上结果只证明工具链与外壳状态，**不代表任何业务能力可用**。

## 9. 待办与交接

| 项 | 状态 | 处理方 |
|---|---|---|
| SqlMap 作用域与语句 ID 解析修法 | S1-D1/S1-D2 已选定，运行验证未执行 | 阶段 1 |
| 平台标准目录三表初始化 | 独立新建 `mrec_` 表已确认；目标数据库与 schema 待负责人确认，尚未执行 | 阶段 1 产出最终 DDL；负责人执行，不再迁移历史三表 |
| 阶段 1 文档与实施 | 已有设计及实施计划；A2 UI 设计确认，整体设计待审查通过，正式实施未开始 | 阶段 1 |
| 对外依赖的平台 Base API 与子系统业务编码 | 未配置 | 项目负责人 |
| 是否补齐根发布流程 | 未定 | 项目负责人 |

下一主责：阶段 1 标准项目目录维护（F01）。
