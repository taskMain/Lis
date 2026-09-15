# 阶段 1（标准项目目录维护）后端审查报告：入参校验与 WorkUnit 使用

审查类型：只读静态审查（不修改生产代码）
审查日期依据：工作区当前状态（`git status --short --branch` 起始快照已读取，未回退、覆盖或清理任何非本轮内容）
产出物：仅本文件

---

## 1. 审查范围与依据

### 1.1 审查对象

| 范围 | 路径 |
|---|---|
| 阶段 1 写请求（12 个） | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/`（排除阶段 2 的 `*MutualRecognitionItem*` 4 个文件） |
| 阶段 1 查询请求（4 个） | `server/Dy.MedicalRecognition.Application.Contracts/Queries/MedicalRecognitionReportQueryContracts.cs` |
| 写应用服务 | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` |
| 查询应用服务 | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` |
| 领域校验 | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` |
| 映射声明 | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportDataMaps.cs` |
| SqlMap 语句 | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalStandard{Category,Group,Item}.xml` |
| 建表脚本（列长度依据） | `server/Dy.MedicalRecognition.Repository/Scripts/medical_standard_{category,group,item}.sql` |
| 架构/契约测试 | `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs`、`Stage1SqlMapProbeTests.cs`、`SmokeTests.cs` |
| 参照实现 | `E:\MedSync\Dy.LisCenter\server` |

### 1.2 规范依据（逐字读取）

| 依据 | 关键条款 |
|---|---|
| `backend-command-query-event.md` §4.1 | 「是否开启显式事务依据业务原子边界、独立数据库语句数量和并发要求判断，不依据涉及表数量。」「单条 `INSERT`、`UPDATE` 或 `DELETE` 通常依赖数据库语句级原子性，不因只操作一张表或登记一个 DomainEvent 就机械开启显式事务。」「多条独立数据库语句即使只操作同一张表，只要业务要求全成全败，就必须开启事务。」「先读后写不自动要求事务。设计必须说明并发由数据库唯一约束、条件更新、版本检查、影响行数检查还是锁定快照兜底」「数据库事务原子性与 DomainEvent 可靠投递分开判断。不得仅因存在 DomainEvent 就开启显式事务」「必须优先使用目标框架已经提供的工作单元或事务声明机制」「Application Service 只声明事务边界，不手工开始、提交或回滚事务」 |
| `backend-command-query-event.md` §4.2 | 「框架源码或反编译只能形成 `SourceConfirmed` 证据」 |
| `dy-framework-workunit.md` §1 | 「需要显式事务的公开 AppService 方法使用框架现有声明 `[WorkUnit(UseTransaction = true)]`」「事务边界仍由写用例的原子性决定，不因使用 Dy Framework 而机械加 Attribute」 |
| `dy-framework-workunit.md` §4 | 变更检查项包含「`WorkUnitAttribute` 的事务启用语义和实际拦截入口」 |
| `backend-architecture.md` §3 | AppService 标准流程首项为「Request 校验」；AppService 负责「公共 Request 校验和权限范围」 |
| `backend-architecture.md` §2 | 「公共 Request」职责为「调用方能够提交的字段和格式约束」 |

### 1.3 已核实事实（父任务提供，本轮抽查复核）

| 事实 | 复核结论 |
|---|---|
| LisCenter Request 使用 `System.ComponentModel.DataAnnotations` 的 `[Required]`/`[StringLength]`/`[MinLength]`/`[EnumDataType]` 等 | 抽查 `ApproveApplicationRequest.cs`、`ExternalCreateApplicationRequest.cs`、`ConfirmCurrentSpecimenPackageReceiptRequest.cs` 确认；且 `ExternalCreateApplicationRequest.cs:9` 同时带 `[IPropertyChangedAware]`，即「属性变更追踪 + DataAnnotations」是 LisCenter 的既有组合 |
| LisCenter AppService 入口显式调用 `LisCenterRequestValidator.Validate(request)` | 抽查确认 126 处调用；机制见 `Dy.LisCenter.Application.Contracts\Validation\LisCenterRequestValidator.cs:13-20`（`Validator.ValidateObject(..., validateAllProperties: true)`） |
| LisCenter：`[WorkUnit(UseTransaction = true)]` 43 处、`(UseTransaction = false)` 2 处、裸 `[WorkUnit]` 0 处 | 复核一致：`(UseTransaction = true)` 43 处；`(UseTransaction = false)` 2 处，均位于 `PageExternalPushRecordAppService.cs:28,33`；裸 `[WorkUnit]` 0 处 |
| 本项目：裸 `[WorkUnit]` 10 处、`[WorkUnit(UseTransaction = true)]` 2 处 | 复核一致，全部位于 `MedicalRecognitionReportAppService.cs:20,30,40,50,60,70,80,90,100,110,120,130` |
| 本项目 Request 无任何校验特性 | 复核一致：对整个 `server` 目录检索 `System.ComponentModel.DataAnnotations`、`[Required]`、`[StringLength`、`[MinLength`、`[MaxLength`、`[Range` **零命中** |

---

## 2. 入参校验现状表（逐个 Request、逐个属性）

### 2.1 写请求（阶段 1，12 个）

「领域层手写校验」列引用 `MedicalRecognitionReportManager.cs` 的私有校验方法（`ValidateName`/`ValidateCode`/`ValidateItemType`/`ValidateId`，定义在 `MedicalRecognitionReportManager.cs:135-138`）与仓储读校验。

| # | Request（文件:行） | 属性 | 类型 | 可空 | 当前校验特性 | 领域层对应手写校验 |
|---|---|---|---|---|---|---|
| 1 | `CreateMedicalStandardCategoryRequest.cs` | `ItemType` :12 | `MedicalItemType` | 否（值类型） | 无 | 有：`ValidateItemType`（Manager:14 调用 → 137），`Enum.IsDefined` |
| | | `Name` :16 | `string` | 否（但可反序列化为 `null`） | 无 | 有：`ValidateName`（Manager:14 → 135），`IsNullOrWhiteSpace` |
| | | `Remark` :20 | `string?` | 是 | 无 | 无（设计上允许任意可空值） |
| 2 | `UpdateMedicalStandardCategoryRequest.cs` | `Id` :12 | `Guid` | 否（值类型） | 无 | **无 `ValidateId`**（Manager:22-33 只判断「记录不存在」） |
| | | `ItemType` :16 | `MedicalItemType` | 否 | 无 | 有：`ValidateItemType`（Manager:24） |
| | | `Name` :20 | `string` | 否 | 无 | 有：`ValidateName`（Manager:24） |
| | | `Remark` :24 | `string?` | 是 | 无 | 无 |
| 3 | `EnableMedicalStandardCategoryRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:35-42） |
| 4 | `DisableMedicalStandardCategoryRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:44-51） |
| 5 | `CreateMedicalStandardGroupRequest.cs` | `CategoryId` :12 | `Guid` | 否 | 无 | 有：`ValidateId(command.CategoryId, "分类")`（Manager:55） |
| | | `Name` :16 | `string` | 否 | 无 | 有：`ValidateName`（Manager:55） |
| | | `Remark` :20 | `string?` | 是 | 无 | 无 |
| 6 | `UpdateMedicalStandardGroupRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:64-73） |
| | | `Name` :16 | `string` | 否 | 无 | 有：`ValidateName`（Manager:66） |
| | | `Remark` :20 | `string?` | 是 | 无 | 无 |
| 7 | `EnableMedicalStandardGroupRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:75-82） |
| 8 | `DisableMedicalStandardGroupRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:84-91） |
| 9 | `CreateMedicalStandardItemRequest.cs` | `CategoryId` :12 | `Guid` | 否 | 无 | 有：`ValidateId(...,"分类")`（Manager:95） |
| | | `GroupId` :16 | `Guid` | 否 | 无 | 有：`ValidateId(...,"分组")`（Manager:95） |
| | | `Code` :20 | `string` | 否 | 无 | 有：`ValidateCode`（Manager:95 → 136） |
| | | `Name` :24 | `string` | 否 | 无 | 有：`ValidateName`（Manager:95） |
| | | `Remark` :28 | `string?` | 是 | 无 | 无 |
| 10 | `ChangeMedicalStandardItemRemarkRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | 有：`ValidateId(command.Id, "标准项目")`（Manager:108） |
| | | `Remark` :16 | `string?` | 是 | 无 | 无 |
| 11 | `EnableMedicalStandardItemRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:115-122） |
| 12 | `DisableMedicalStandardItemRequest.cs` | `Id` :12 | `Guid` | 否 | 无 | **无 `ValidateId`**（Manager:124-131） |

**汇总**：12 个写请求共 23 个属性，**校验特性数量为 0**。领域层手写校验覆盖 0/12 个 `Id`（仅 2 个方法覆盖了 `CategoryId`/`GroupId`），覆盖全部 `Name`/`Code` 的非空，覆盖 `ItemType` 的值域，**完全不覆盖任何最大长度**。

### 2.2 查询请求（4 个）

`MedicalRecognitionReportQueryContracts.cs` 中 4 个 Query Request 均不带 `[IPropertyChangedAware]`（`sealed record` + `init`），校验在 `MedicalRecognitionReportQueryAppService` 内以私有静态方法手工实现。

| # | Query Request（文件:行） | 属性 | 类型 | 可空 | 当前校验特性 | 应用层手写校验（QueryAppService） |
|---|---|---|---|---|---|---|
| 1 | `MedicalStandardCategoryListQueryRequest` :4 | `ItemType` :7 | `MedicalItemType?` | 是 | 无 | `ValidateOptionalItemType`（:15 → :92-95）：有值且 `!Enum.IsDefined` 拒绝 |
| 2 | `MedicalStandardGroupListQueryRequest` :11 | `CategoryId` :14 | `Guid?` | 是 | 无 | `ValidateOptionalId`（:22 → :82-85）：`== Guid.Empty` 拒绝 |
| 3 | `MedicalStandardItemListQueryRequest` :18 | `CategoryId` :21 | `Guid?` | 是 | 无 | `ValidateOptionalId`（:29） |
| | | `GroupId` :23 | `Guid?` | 是 | 无 | `ValidateOptionalId`（:29） |
| | | `Code` :25 | `string?` | 是 | 无 | `ValidateOptionalText`（:30 → :87-90）：非 `null` 且 `IsNullOrWhiteSpace` 拒绝；无长度上限 |
| | | `Name` :27 | `string?` | 是 | 无 | `ValidateOptionalText`（:30）；无长度上限 |
| | | `IsValid` :29 | `bool?` | 是 | 无 | 无（`true`/`false` 均有意义） |
| 4 | `EffectiveMedicalStandardCatalogQueryRequest` :33 | `ItemType` :36 | `MedicalItemType?` | 是 | 无 | `ValidateOptionalItemType`（:37） |
| | | `CategoryName` :38 | `string?` | 是 | 无 | `ValidateOptionalText`（:38）；无长度上限 |

**查询侧现状可接受**：4 个查询请求的属性都有显式校验或本身就是合法值域，且 `:14/:21/:35` 有 `request is null` 前置检查。与 LisCenter「查询也走统一校验器」的做法不同，但语义上不构成漏洞，仅记一致性问题（见 P6）。

---

## 3. 入参校验问题清单

| 编号 | 严重度 | 文件:行 | 规范依据 | 问题 | 建议修法 |
|---|---|---|---|---|---|
| **V1** | 高 | 12 个写 Request 文件（`Requests/*.cs:7-28`）；`MedicalRecognitionReportAppService.cs:20,30,40,50,60,70,80,90,100,110,120,130` | `backend-architecture.md` §3「AppService 标准流程：Request 校验 → …」「AppService 负责：公共 Request 校验和权限范围」；§2「公共 Request … 调用方能够提交的字段和格式约束」 | 12 个写 Request 的 23 个属性**无任何校验特性**，且 AppService 入口**没有任何显式校验调用**。公共契约层完全没有承担「字段和格式约束」职责，全部下压到 Manager 手写校验 | 新增 `Application.Contracts/Validation/MedicalRecognitionRequestValidator.cs`（照 `LisCenterRequestValidator.cs:13-20`），在 12 个写入口 `MapTo...Command()` **之前**调用；同时给属性补 DataAnnotations |
| **V2** | 中 | 全部 23 个写 Request 属性 | 与 V1 同 | 缺少 `[Required]` 声明。**注意语义边界**：LisCenter 的 `[Required]` 也只拒绝 `null`/空串、放行纯空白，纯空白由领域层 `IsNullOrWhiteSpace` 拦截（本项目 `Manager.cs:135-136`）。即 `[Required]` 是「声明式契约标注 + 与 LisCenter 对齐」，**不是**替代领域校验 | 按 LisCenter 写法补 `[Required]`；同时保留 `Manager.cs:135,136,138` 的 `IsNullOrWhiteSpace`/`Guid.Empty` 校验，不要删除 |
| **V3** | 高 | `CreateMedicalStandardCategoryRequest.cs:16`、`UpdateMedicalStandardCategoryRequest.cs:20`、`CreateMedicalStandardGroupRequest.cs:16`、`UpdateMedicalStandardGroupRequest.cs:16`、`CreateMedicalStandardItemRequest.cs:24` | 建表脚本 `medical_standard_category.sql:5`、`medical_standard_group.sql:5`、`medical_standard_item.sql:7` 均为 `name varchar(200) not null` | `Name` 无 `[StringLength(200)]`，领域层也无长度校验（`Manager.cs:135` 只判空白）。超长名称会一路写到 `INSERT`/`UPDATE`，由 PostgreSQL 抛 `22001 value too long for type character varying(200)`，再经框架统一兜底成 **500 + 数据库原始错误文本**，而非友好参数校验失败 | 加 `[StringLength(200)]`，并把长度校验纳入统一校验器 |
| **V4** | 高 | `CreateMedicalStandardItemRequest.cs:20` | `medical_standard_item.sql:6`：`code varchar(200) not null`；且 `ux_mrec_medical_standard_item_code` 唯一 | `Code` 无 `[StringLength(200)]`，领域层 `ValidateCode`（`Manager.cs:136`）只判空白。同 V3，属于「供应商错误类型」而非友好校验 | 加 `[StringLength(200)]` |
| **V5** | 中 | 6 个含 `Remark` 的请求：`CreateMedicalStandardCategoryRequest.cs:20`、`UpdateMedicalStandardCategoryRequest.cs:24`、`CreateMedicalStandardGroupRequest.cs:20`、`UpdateMedicalStandardGroupRequest.cs:20`、`CreateMedicalStandardItemRequest.cs:28`、`ChangeMedicalStandardItemRemarkRequest.cs:16` | `medical_standard_category.sql:7`、`medical_standard_group.sql:7`、`medical_standard_item.sql:9`：`remark varchar(600)` | `Remark` 无 `[StringLength(600)]`。备注是最容易被前端粘贴超长文本的字段，且**完全没有任何层**做长度校验（领域层连空白都不判），必然以数据库错误形态暴露 | 加 `[StringLength(600)]`；`ChangeMedicalStandardItemRemark` 是纯备注入口，优先级最高 |
| **V6** | 中 | `CreateMedicalStandardCategoryRequest.cs:12`、`UpdateMedicalStandardCategoryRequest.cs:16` | `MedicalRecognitionReportManager.cs:137` 已有 `Enum.IsDefined` | `ItemType` 在领域层已正确拒绝未定义值（实测：`itemType=7` → `参数校验失败：项目类型无效。`），但 Request 层无 `[EnumDataType]`，错误信息不统一，且与 LisCenter（`ExternalCreateApplicationRequest.cs:111-113` 同时用 `[Required]` + `[EnumDataType(..., ErrorMessage=...)]`）不一致 | 加 `[EnumDataType(typeof(MedicalItemType), ErrorMessage = "项目类型错误")]`；保留领域层兜底 |
| **V7** | 中 | 10 个写请求的 `Id`：`UpdateMedicalStandardCategoryRequest.cs:12`、`EnableMedicalStandardCategoryRequest.cs:12`、`DisableMedicalStandardCategoryRequest.cs:12`、`UpdateMedicalStandardGroupRequest.cs:12`、`EnableMedicalStandardGroupRequest.cs:12`、`DisableMedicalStandardGroupRequest.cs:12`、`EnableMedicalStandardItemRequest.cs:12`、`DisableMedicalStandardItemRequest.cs:12`；另 `Guid.Empty` 语义在 `UpdateMedicalStandardCategoryAsync`/`UpdateMedicalStandardGroupAsync` 也未被识别 | `Manager.cs:138` 已有 `ValidateId` 但只对 4 处生效（`CreateMedicalStandardGroup`、`CreateMedicalStandardItem` 的 2 个父级 ID、`ChangeMedicalStandardItemRemark`） | 8 个方法的 `Id` 既无 Request 层校验、也无领域层 `ValidateId`。`Guid.Empty` 的判定被降级为「记录不存在 → 业务拒绝」，把「参数为空」与「业务对象不存在」两类事实混在同一条消息里，违反 V48「参数错误与业务拒绝消息可区分」 | 两条都要做：Request 层 `[Required]`，领域层对这 8 个方法补 `ValidateId(command.Id, "…")`，保持与 `Manager.cs:108` 一致 |
| **V8** | 高 | `MedicalRecognitionReportAppService.cs:23,33,43,53,63,73,83,93,103,113,123,133`（每处 `request.MapTo...Command()`） | `backend-architecture.md` §3 流程首项 | **校验触发方式缺失**：12 个写入口的第一条语句都是 `MapTo...Command()`。源生成映射对每个属性做「白名单拷贝」（证据：`obj/…/ObjectMapAssemblyGenerator/*RequestAnd*CommandMaps.g.cs:17-33`，形如 `if (request.HasPropertyChanged) { if (request.ChangedProperties.Contains("Id")) { cmd.Id = request.Id; } }`），**只拷贝调用方在 `changedProperties` 里声明过的字段**。因此若不做 Request 层校验，「未声明字段」与「声明了但值为默认值」在 Command 上完全不可区分。当前非法输入的实际走向见第 6 节 | 在任何 `MapTo` 之前插入统一校验（V1 的修法）；并把「映射白名单」语义写入设计说明 |
| **V9** | 中 | `MedicalRecognitionReportQueryAppService.cs:82-90` | `backend-query-event` §3「查询必须定义范围、空值语义…」 | 查询侧的空值语义是**隐式且分散**的：`null` 与省略不过滤，空串/纯空白被拒绝（`决策与结论-20260915.md` 第 2 节已实测并更正）。这与 `MedicalRecognitionReportQuery.xml:8,16,24-28,38-39` 的 `<IsNotEmpty>` 条件耦合，但 XML 只决定是否拼接 `where`，无法表达入参拒绝语义（同一份决策文件已记录该教训） | 二选一并写入设计：① 用 DataAnnotations 统一到与写请求同一机制；② 保持手写，但在 Contracts 的 `QueryRequest` 上用 XML 注释显式写明「`null`/省略 = 不过滤；空串/纯空白 = 拒绝」，避免再次误读 XML |
| **V10** | 低（已裁定，仅记录） | `CreateMedicalStandardCategoryRequest.cs:12` | `决策与结论-20260915.md` §6.2 D7：已裁定「未传按默认 `Laboratory=0` 处理，不要求类型必填」 | `ItemType` 为不可空枚举，不传时反序列化为 `0`，与显式传 `0` 不可区分。已由负责人裁定接受，**不作为缺陷**，但它是「缺少 `[Required]` 后不可区分默认值/未传」的同类实例，修复 V3–V6 时不应顺手改成必填而推翻 D7 | 保持现状；若未来改必填，需同步修改契约、客户端并重新生成 |
| **V11** | 低（机制性） | `Contracts/.../Requests/*.cs:6`（`[IPropertyChangedAware]`）；`Manager` 对所有入口 `AddEvent(...)` | `backend-architecture.md` §2「Request != Command」 | 请求模型自身携带可变集合 `ChangedProperties`（生成物 `EnableMedicalStandardCategoryRequest.g.cs:10-13` 标注 `[JsonInclude]`），即**传输层可以改变服务端映射行为**。当前是框架既有模式（LisCenter `ExternalCreateApplicationRequest.cs:9` 同样如此），不新增缺陷，但应在设计文档中明示：该集合参与映射白名单，调用方不声明字段等价于不改该字段 | 记录到设计说明，不要求改代码 |
| **V12** | 低 | `Application.Contracts` 无 `Validation/` 目录 | 与 LisCenter 一致性 | 本项目缺少与 `Dy.LisCenter.Application.Contracts.Validation` 对应的公共校验入口，也没有「入口方法必须调用统一校验器」的架构测试。LisCenter 有 `ApplicationServiceValidationContractTests.cs:11-36`（正则扫描方法体前 300 字符必须含 `LisCenterRequestValidator.Validate(<参数>);`） | 引入校验器后补同等架构测试；本项目 `Stage1ArchitectureTests.cs` 已有反射断言 `WorkUnit` 的先例，可沿用同一风格 |

### 3.1 「没有显式触发校验」时的实际调用链（V8 展开）

以 `POST /Api/MedicalRecognitionReport/DisableMedicalStandardCategory` 且请求体为 `{}` 为例：

```text
Host（Dy.Apron.QuickStart 的 Program.Main，见 Dy.Apron 1.1.0.48 Program:326-340）
→ MVC 绑定 [FromBody] DisableMedicalStandardCategoryRequest（框架在 Dy.Apron.Http ControllerModelConvention:1097 对单参数 POST 动作补 FromBody）
→ DisableMedicalStandardCategoryRequest 实例（ChangedProperties 为空集，Id = Guid.Empty）
→ WorkUnitEndpointFilter.InvokeAsync（Dy.Apron.Http:24-65）
   → WorkUnitManager.OnExecuting / PreExecuteAsync
→ MedicalRecognitionReportAppService.DisableMedicalStandardCategoryAsync:131
→ 第 133 行 request.MapToDisableMedicalStandardCategoryCommand()
   → 生成映射：HasPropertyChanged == false ⇒ 不拷贝任何字段 ⇒ Command.Id == Guid.Empty
→ 第 134 行 HttpRequestInfo.UserId 解析（此处会先抛「无法确定有效的操作人。」若身份不可用）
→ Manager.DisableMedicalStandardCategoryAsync:124
   → GetMedicalStandardCategoryByIdAsync(Guid.Empty) ⇒ null ⇒ throw Business("分类不存在。")
→ 异常向上传播到 ResultActionUnitFilter.OnFailedAsync（Dy.Apron.Http:469-493）
   → IsResultHandled = true，Results.InternalServerError(new Result<string>(-1, ex.Message, ex.ToString()))
   → 响应 500 + `业务拒绝：分类不存在。`
```

因此：**非法输入既没有在契约层被拦，也没有产生可区分的参数错误消息，而是被翻译成一条业务拒绝，并以 HTTP 500 返回**。这也解释了「省略 id 返回 `参数校验失败：…ID不能为空。`」只出现在 `CreateMedicalStandardGroup`/`CreateMedicalStandardItem`/`ChangeMedicalStandardItemRemark` 三条路径上——只有它们的 Manager 调用了 `ValidateId`。

---

## 4. WorkUnit 逐方法判定表

### 4.0 前置结论一：裸 `[WorkUnit]` 的语义（已从框架源码确认）

**裸 `[WorkUnit]` 不开显式事务。** 证据链（全部来自本项目实际引用的包版本：`Dy.Core.Abstractions 1.1.0.54`、`Dy.Apron 1.1.0.48`，见 `server/Directory.Packages.props:6,10,12`）：

1. `Dy.Core.Abstractions.Http.WorkUnitAttribute`：`TransactionInfo` 初始化为 `new TransactionInfo { TransactionOrigin = FromActionUnit }`，**`UseTransaction` 未赋值（`bool?` 为 `null`）**；属性 `UseTransaction => TransactionInfo.UseTransaction == true`。
2. `Dy.Core.Abstractions.Http.TransactionInfo.IsActionUnitTransaction()`：`if (UseTransaction == true) return TransactionOrigin == FromActionUnit; return false;` ⇒ `null` 时返回 `false`。
3. 事务开闭唯一入口 `Dy.Apron.Http.ActionUnit.TransactionActionUnitFilter`：`IsHandleTransaction()` 读取 `HttpContextInfo.Current.TransactionInfo?.IsActionUnitTransaction() == true`，`OnExecutingAsync`/`OnCompletedAsync`/`OnFailedAsync` 全部以该方法为门 ⇒ 裸 `[WorkUnit]` 时**不 Begin、不 Commit、不 Rollback**。
4. `Dy.Apron.Http.ActionUnit.WorkUnitEndpointFilter.SetHttpContextInfo`（:110-117）：无 `HttpContextInfo` 请求头分支下，仅当 `workUnitAttribute.TransactionInfo.UseTransaction == true` 才把特性上的 `TransactionInfo` 克隆到当前请求上下文；裸 `[WorkUnit]` 不赋值。
5. 带 `HttpContextInfo` 请求头分支（:88-96）：只有当调用方在头里显式传入 `UseTransaction == true` 时才使用，并且强制 `TransactionOrigin = FromRequest`，而 `IsActionUnitTransaction()` 要求 `FromActionUnit` ⇒ **该分支也不会开启 ActionUnit 本地事务**。

结论与设计表一致：`docs/plans/003-阶段1-标准项目目录维护/Server/design.md:93`「其余 10 个写用例不开显式事务」。

**证据等级与残余不确定项**：以上为反编译源码核实的 `SourceConfirmed` 证据（`backend-command-query-event.md` §4.2 允许的层级）。`design.md:368` 把「省略 `UseTransaction` 是否确实代表不开事务」列为实施期验证门（「实施首批次用一个写方法观察是否存在显式事务」），**该真实运行观测在本轮审查中未执行**（见第 7 节局限 L1）。

### 4.1 前置结论二：事件过滤器的相对顺序未知，但本阶段不依赖

`WorkUnitManager.CompletedAsync` 按**逆序**执行过滤器（`Dy.Apron.Http:63-69`），`EventBusActionUnitFilter.OnCompletedAsync` 执行 `FlushAsync()`（:182-185），`TransactionActionUnitFilter.OnCompletedAsync` 执行 `TryCommitTransAsync()`（:33-39）。两者是平级 `IWorkUnitFilter`，注册顺序由框架装配决定。因此**无法从反编译确定 `FlushAsync` 与 `Commit` 的先后**——这与 `dy-framework-workunit.md` §3 保留的风险描述一致。本阶段事件「只登记、不接通发布与订阅」（`Server/design.md:130`、S1-D11），因此该顺序不影响本阶段判定；但一旦为这两个方法的事件增加外部推送，必须先取证（见第 6 节并发提示与 `dy-framework-workunit.md` §3.3）。

### 4.2 逐方法判定

| # | 方法（`MedicalRecognitionReportAppService.cs:行`） | 当前特性 | 实际数据库语句条数与顺序 | 登记事件 | 是否需要显式事务 | 结论 |
|---|---|---|---|---|---|---|
| 1 | `CreateMedicalStandardCategoryAsync` :20-28 | `[WorkUnit]` :20 | ① `SELECT count(1)` 名称查重（`MedicalStandardCategory.xml:27-30`，Ref `MedicalRecognitionReportRepository.cs:30`）② `INSERT`（`xml:11-13`） | 是，`Manager.cs:19` 成功后 | **否**：唯一写语句是单条 `INSERT`；查重为建议性 | **正确**（与 `design.md:58` 一致）。并发由唯一索引 `ux_mrec_medical_standard_category_name` 兜底（`medical_standard_category.sql:22-23`） |
| 2 | `UpdateMedicalStandardCategoryAsync` :30-38 | `[WorkUnit]` :30 | ① `SELECT` 按 ID 取分类（`xml:24-26`）② `SELECT count(1)` 名称查重排除自身（`xml:27-30`）③ 条件分支 `SELECT count(1)` 下级分组（`xml:31-33`；仅当 `ItemType` 变化，`Manager.cs:28`）④ `UPDATE`（`xml:14-17`） | 是，`Manager.cs:32` | **否**：1 条写语句 | **正确**（`design.md:59`）。前置读失效风险按 S1-D23 接受；`design.md:82` 交错 I-1 已登记 |
| 3 | `EnableMedicalStandardCategoryAsync` :40-48 | `[WorkUnit]` :40 | ① `SELECT` 按 ID 取状态（`xml:24-26`）② 若已是启用则无写（`Manager.cs:39` 提前 `return true`）；否则 `UPDATE … where id and is_valid = false`（`xml:18-20`） | 仅非幂等路径，`Manager.cs:41` | **否**：非幂等路径只有 1 条写语句；幂等路径 0 条 | **正确**（`design.md:60`）。并发由条件 `UPDATE` 兜底 |
| 4 | `DisableMedicalStandardCategoryAsync` :50-58 | `[WorkUnit]` :50 | 同上方向相反（`xml:21-23`；`Manager.cs:48`） | 仅非幂等路径，`Manager.cs:50` | **否** | **正确**（`design.md:61`） |
| 5 | `CreateMedicalStandardGroupAsync` :60-68 | **`[WorkUnit(UseTransaction = true)]`** :60 | ① `SELECT` 上级分类（`MedicalStandardGroup.xml:24-26`）② `SELECT count(1)` 分类内名称查重（`xml:27-30`）③ `INSERT`（`xml:11-13`） | 是，`Manager.cs:61` | 严格按 §4.1 **不需要**（1 条写语句，读不自动要求事务） | **实现正确、依据不成立**：见 V13，保留但改口径 |
| 6 | `UpdateMedicalStandardGroupAsync` :70-78 | `[WorkUnit]` :70 | ① `SELECT` 按 ID 取分组 ② `SELECT count(1)` 既有分类内查重排除自身 ③ `UPDATE`（`xml:14-17`） | 是，`Manager.cs:72` | **否** | **正确**（`design.md:63`）。`CategoryId` 不由请求改写（`Manager.cs:70` 取 `existing.CategoryId`） |
| 7 | `EnableMedicalStandardGroupAsync` :80-88 | `[WorkUnit]` :80 | 同 #3 | 仅非幂等路径，`Manager.cs:81` | **否** | **正确**（`design.md:64`） |
| 8 | `DisableMedicalStandardGroupAsync` :90-98 | `[WorkUnit]` :90 | 同 #4 | 仅非幂等路径，`Manager.cs:90` | **否** | **正确**（`design.md:65`） |
| 9 | `CreateMedicalStandardItemAsync` :100-108 | **`[WorkUnit(UseTransaction = true)]`** :100 | ① `SELECT` 分类 ② `SELECT` 分组 ③ `SELECT count(1)` 编码查重（`MedicalStandardItem.xml:27-29`）④ `INSERT`（`xml:11-13`） | 是，`Manager.cs:103` | 严格按 §4.1 **不需要**（1 条写语句） | **实现正确、依据不成立**：见 V13，保留但改口径 |
| 10 | `ChangeMedicalStandardItemRemarkAsync` :110-118 | `[WorkUnit]` :110 | ① `SELECT` 按 ID 取项目（`xml:24-26`）② `UPDATE` 只改 `remark/oper_id/oper_time`（`xml:14-17`） | 是，`Manager.cs:112` | **否** | **正确**（`design.md:67`） |
| 11 | `EnableMedicalStandardItemAsync` :120-128 | `[WorkUnit]` :120 | 同 #3（`xml:18-20`） | 仅非幂等路径，`Manager.cs:121` | **否** | **正确**（`design.md:68`） |
| 12 | `DisableMedicalStandardItemAsync` :130-138 | `[WorkUnit]` :130 | 同 #4（`xml:21-23`） | 仅非幂等路径，`Manager.cs:129` | **否** | **正确**（`design.md:69`） |

**汇总**：12 个写方法全部只产生 **1 条写语句**（0 个方法修改 2 个及以上实体、0 个方法做批量逐条写入）。因此按 `backend-command-query-event.md` §4.1「单条 `INSERT`、`UPDATE` 或 `DELETE` 通常依赖数据库语句级原子性」，**12 个方法都不满足「必须开启事务」的强制条件**；#5/#9 的 `UseTransaction = true` 属设计选择（且与已批准设计一致），不属于规范强制项。

### 4.3 反向遗漏检查：是否有「一次修改多个实体」却没有事务

| 检查项 | 结论 |
|---|---|
| 阶段 1 的 12 个写方法是否有一条 SQL 影响多行/多表？ | **否**。全部 `INSERT`/`UPDATE` 均带 `where id = $Id`（或 `where id = $Id and is_valid = <反向值>`），无批量语句、无级联更新 |
| 是否存在「领域层调用多个 Repository 写方法」？ | **否**。`MedicalRecognitionReportManager.cs:12-131` 中每个写方法只有 1 次写调用 |
| 是否存在「分类停用级联改写分组/项目」？ | **否**，且为设计明确要求（`Server/design.md:114,118`「不级联改写下级」） |
| 阶段 1 范围外但同类风险点 | **V14**：`CreateMutualRecognitionItemAsync` 等 4 个阶段 2 方法（`MedicalRecognitionReportAppService.cs:140-170`）**完全没有 `[WorkUnit]` 标注**，而其 Manager（`Manager.cs:142-159`）会 `AddEvent(...)`，其中 `UpdateMutualRecognitionItemConfigurationAsync`（`Manager.cs:147-151`）在**影响 0 行时也登记事件**（`AddEvent` 在判断 `result > 0` 之前执行）。无 WorkUnit ⇒ 无工作单元事件队列边界（`EventBusActionUnitFilter.OnExecuting` 才设置 `EventBusFactory.CurrentEventQueue`，`Dy.Apron.Http:172-175`），事件语义不可预期。**不属阶段 1 范围**，须在阶段 2 设计时作为前置项处理 |

---

## 5. WorkUnit 问题清单

| 编号 | 严重度 | 文件:行 | 规范依据 | 问题 | 建议修法 |
|---|---|---|---|---|---|
| **V13** | 中 | `MedicalRecognitionReportAppService.cs:60`、`:100` | `backend-command-query-event.md` §4.1 第 2/3/5 条；`dy-framework-workunit.md` §1「事务边界仍由写用例的原子性决定，不因使用 Dy Framework 而机械加 Attribute」 | `CreateMedicalStandardGroupAsync`/`CreateMedicalStandardItemAsync` 的 `UseTransaction = true` **是已批准设计的选择，不是规范强制**：两者都只有 1 条写语句（`INSERT`），前置 `SELECT` 属「先读后写」，而 §4.1 明确「先读后写不自动要求事务」。当前设计文档给出的理由（`Server/design.md:62,66,90`）是「事务负责失败回滚，不据此宣称普通读取和写入获得不变的父级快照」——而单条 `INSERT` 的失败回滚本就由语句级原子性保证，`TryCommitTransAsync` 也只为**显式开启**的事务执行（`TransactionManager.cs:76-102`）。**结论：实现无误，但依据需改口径**，否则会误导后续照抄 | **不改代码**（改动会同时推翻 `design.md:62,66` 与 `Stage1ArchitectureTests.cs:15,28` 的断言）。改文档口径：把理由写成「保留多语句原子性声明 + 与后续可能扩展的父级/子级一致性写入保持同一事务边界」，或按 §4.1 明确标注为 `AcceptedRisk`/有意选择。**不要**把它写成规范要求 |
| **V15** | 中 | `MedicalRecognitionReportAppService.cs:20,30,40,50,70,80,90,110,120,130`（10 处裸 `[WorkUnit]`） | `dy-framework-workunit.md` §4「变更检查：`WorkUnitAttribute` 的事务启用语义」；`backend-command-query-event.md` §4.1「必须优先使用目标框架已经提供的工作单元或事务声明机制」 | 语义现已确认（第 4.0 节）：裸 `[WorkUnit]` = 不开显式事务。因此这 10 处**功能正确**，但可读性差：同一 AppService 内混用裸 `[WorkUnit]` 与 `[WorkUnit(UseTransaction = true)]`，读者无法区分「有意不加事务」与「漏写」。LisCenter 在同一类中并存两种边界时使用显式写法（`PageExternalPushRecordAppService.cs:28,33` 的 `UseTransaction = false`），本项目同类场景反而用了裸写法 | **二选一**：① 保持现状，但在 `Server/design.md` 的框架机制映射（:352-362）补一行「省略 `UseTransaction` 等价于不开事务（源码依据）」——成本最低；② 把 10 处显式写成 `[WorkUnit(UseTransaction = false)]`。**注意**：`Stage1ArchitectureTests.cs:26-28` 用 `GetProperty("UseTransaction")` 取值断言，两种写法都能通过，无需改测试 |
| **V16** | 低 | `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs:12-30` | `backend-command-query-event.md` §4.1；`dy-framework-workunit.md` §4 | 现有架构测试只断言 12 个方法存在 `WorkUnitAttribute` 且 `UseTransaction` 取值与硬编码名单一致。**未断言**：① 禁止项（`TransactionScope`、`IDbTransaction`、`BeginTransaction`/`Commit`/`Rollback`、自定义 UnitOfWork）是否零使用；② Manager/Repository/Entity 是否零事务 API。`design.md:105` 已把这两项列为「事务声明的机械检查」要求 | 按 `design.md:105` 补静态源码扫描测试（LisCenter 有同类先例：`ApplicationServiceValidationContractTests.cs` 的源码扫描风格） |
| **V14** | 中（阶段 2 前置） | `MedicalRecognitionReportAppService.cs:140-170`；`MedicalRecognitionReportManager.cs:142-159` | `dy-framework-workunit.md` §1；`backend-command-query-event.md` §4.1「一个写用例的事务边界定义在公开 Application Service 入口」 | 4 个互认配置写方法**无任何 `[WorkUnit]` 标注**，不在阶段 1 交付范围，但其 Manager 会登记事件；其中 `UpdateMutualRecognitionItemConfiguration` 在影响 0 行时仍登记事件（`Manager.cs:149-150`） | 阶段 2 设计时补 WorkUnit 声明与事务表；不要把本阶段结论直接套用 |

---

## 6. 与 Guid 缺陷的关联分析

### 6.1 已核实事实（本轮抽查 + 源码取证）

| # | 事实 | 证据 |
|---|---|---|
| F1 | 现象：写接口传入「存在但无法解析成 Guid 的 `id`」（`'abc'`、`''`、`null`）返回 `500 System.NullReferenceException`；已复现接口包含 `ChangeMedicalStandardItemRemark`、`EnableMedicalStandardGroup`、`DisableMedicalStandardCategory`、`DisableMedicalStandardItem`、`UpdateMedicalStandardCategory`、`UpdateMedicalStandardGroup` | `docs/plans/003-阶段1-标准项目目录维护/testReport.md:916-923`（第二十八轮，「新缺陷 1」） |
| F2 | 对照：**完全省略** `id` 字段时返回 `参数校验失败：标准项目ID不能为空。` | 同上 `testReport.md:920`；代码依据：只有 `CreateMedicalStandardGroupAsync`（`Manager.cs:55`）、`CreateMedicalStandardItemAsync`（`Manager.cs:95`）、`ChangeMedicalStandardItemRemarkAsync`（`Manager.cs:108`）调用 `ValidateId` |
| F3 | 抛出点在 AppService 的 `request.MapTo...Command()`（源生成映射），非手写逻辑 | `testReport.md:922`；代码位置 `MedicalRecognitionReportAppService.cs:23,33,43,53,63,73,83,93,103,113,123,133` |
| F4 | 生成映射**只拷贝 `changedProperties` 白名单内的字段** | `obj/Debug/net10.0/generated/…/ObjectMapAssemblyGenerator/EnableMedicalStandardCategoryRequestAndEnableMedicalStandardCategoryCommandMaps.g.cs:17-23`：`if (request.HasPropertyChanged) { if (request.ChangedProperties.Contains("Id")) { cmd.Id = request.Id; } }` |
| F5 | `ChangedProperties` 是**可被传输层赋值**的 `HashSet<string>`，序列化名 `changedProperties` | 生成物 `…Requests.EnableMedicalStandardCategoryRequest.g.cs:10-13`（`[JsonInclude] public HashSet<string> ChangedProperties { get; } = new();`）；Kiota 生成的客户端**确实会发送该字段**：`client/packages/api-client-medical-recognition/src/models/index.ts:1337` `writer.writeCollectionOfPrimitiveValues<string>("changedProperties", …)`、`:1338` `writer.writeGuidValue("id", …)`（模型声明 `:937-941`：`changedProperties?: string[] \| null`、`id?: Guid \| null`） |
| F6 | 框架把**任何未处理异常**翻译为 HTTP 500 + `Exception.Message` + `Exception.ToString()` | `Dy.Apron.Http:469-493` `ResultActionUnitFilter.OnFailedAsync`：`Results.InternalServerError(new Result<string>(-1, ex.Message, ex.ToString()))`（`WhenErrored` 模式，`ResultWrapOptions.WrapResult` 未配置时的默认分支 :433-436） |
| F7 | 框架**不检查** Request 上的 DataAnnotations；MVC 未配置 `SuppressModelStateInvalidFilter` 之外的改动，JSON 选项只增加 `NumberHandling` 与 `DataTableJsonConverter` | `Dy.Apron:117-129` `AddAspNetCoreApron()` → `AddControllers().AddXmlSerializerFormatters().AddXmlDataContractSerializerFormatters().AddJsonOptions(o => { NumberHandling = AllowNamedFloatingPointLiterals; Converters.Add(new DataTableJsonConverter()); })`。全仓库检索 `System.ComponentModel.DataAnnotations` **零命中** |

### 6.2 判断

**判断 A（可确证）**：F2 与 F7 直接说明，**本项目没有任何 Request 层校验在执行**。非法输入被送进 `MapTo`，因此 `MapTo` 是「第一个能观察到非法输入的地方」——缺陷的**暴露位置**是映射调用，而**结构性成因**是 §3 的 V1/V2/V7/V8：契约层没有字段约束、没有入口校验调用，且 `Id` 类字段在 8 个方法上连领域层 `ValidateId` 都没有。

**判断 B（可确证）**：F4+F5 说明 `changedProperties` 白名单是**映射语义的一部分**，而它由调用方提供。因此「字段缺失」和「字段被声明为默认值」在 Command 层不可区分，任何只在 Manager 做的校验都无法覆盖「字段根本没被映射」的情形。这解释了为什么缺陷表现为「省略 `id` 时给出正确的参数校验失败，而给出非法 `id` 时反而崩在映射里」——两条路径进入 Manager 的 Command 状态不同。

**判断 C（假设，需实测确认）**：对 `id` 传 `'abc'`/`''`/`null` 时 `MapTo` 抛 NRE 的**具体机制本轮未能确证**。本轮已排除若干解释：

- 生成映射自身对 `null` 请求有守卫（`EnableMedicalStandardCategoryRequestAnd…Maps.g.cs:12-13`：`if (request == null) return default;`），对空 `ChangedProperties` 也不会 `Contains` 失败；
- 用与本项目相同形态的模型（`IPropertyChangedAware` 生成的 `[JsonInclude] HashSet<string>` + 生成的属性 setter）在 `net10.0` 上实测 `System.Text.Json`（`JsonSerializerDefaults.Web`）：`changedProperties: null`、`changedProperties: []`、`id: "abc"`、`id: ""`、`id: null` 均**不会**把集合置 `null`；而 `"Id": "abc"`（PascalCase、非大小写不敏感）抛 `JsonException`，也不会产生 NRE。该探针产物位于临时目录 `%TEMP%\mr-json-probe`，**未写入仓库**；
- 框架端点模型绑定走的是标准 MVC `[FromBody]` 路径（`Dy.Apron.Http ControllerModelConvention:1082-1101` 对单参数 POST 动作补 `FromBodyAttribute`），JSON 反序列化失败按 ASP.NET Core 语义应产生 400 而非 500 —— **这与 F1 的观测不一致**。

因此判断 C 的最可能形态是：**请求在到达 AppService 之前或 `ChangedProperties` 被置 `null` 的路径上发生了本项目之外的转换**，使 `request.MapTo...Command()` 内的 `HasPropertyChanged`/`ChangedProperties`（或映射内部的属性拷贝）触发 `NullReferenceException`。但这**只是假设**——F1 记录的是归因位置，不是堆栈原文；本轮未取得真实堆栈。

### 6.3 验证建议（按成本从低到高，先证伪再定位）

| # | 验证项 | 目的与判读 |
|---|---|---|
| 1 | 对任一写接口发送 `{"id": 1}`（数字）与 `{"id": {"a":1}}`（对象），记录状态码与响应体 | 若为 **400**，说明 Request 层的 JSON 反序列化**是**生效的，则 F1 的 `'abc'` 一定走了不同路径（应追问前端实际发了什么）；若同样 **500+NRE**，说明请求体类型转换被绕过，需继续第 2 项 |
| 2 | 从后端日志取「当前请求引发了内部异常：」条目（`ResultActionUnitFilter.OnFailedAsync` 的 `_logger.LogError`，`Dy.Apron.Http:490`）的**完整堆栈** | 这是唯一能确证 C 的手段：堆栈顶部若是 `…Maps.MapToXxxCommand` 内的 `ChangedProperties`/`HasPropertyChanged`，则 C 成立；若在绑定/反序列化层，则结论要改。**未取得堆栈前不得把 C 写为结论** |
| 3 | 用与原请求**逐字节相同**的 body 重放（保留 Kiota 实际发送的 `changedProperties` 字段），并同时对照「去掉 `changedProperties` 字段」的重放 | 判定 `changedProperties` 是否参与触发；这是 F4/F5 的直接行为验证 |
| 4 | 在 `MedicalRecognitionReportAppService` 的 12 个入口按 V1 加入统一校验后，重放 `'abc'`/`''`/`null`/省略 四种输入 | 验收口径：四者都应返回可区分的**参数校验失败**消息，且不再出现 `NullReferenceException`。注意若 C 成立，校验器本身也可能因 `ChangedProperties` 为 `null` 而受影响，需同时断言校验器不依赖 `ChangedProperties` |
| 5 | 触发一次超长 `name`（>200 字符）与超长 `remark`（>600 字符）的真实写入 | 验证 V3/V4/V5 的现状判断：预期得到 PostgreSQL `22001` + 500，据此确认「友好校验缺口」真实存在 |
| 6 | 触发一次 `[WorkUnit(UseTransaction = true)]` 方法的中途失败（如 `CreateMedicalStandardGroup` 传已停用分类，但把 `INSERT` 放到失败之后不易构造；可用测试侧仓储装饰器，参见 `Server/design.md:482` V54-A 的方案） | 验证第 4.0 节源码结论在真实入口成立（对应 `design.md:368` 的实施验证门） |

### 6.4 与入参校验缺失的因果关系（一句话）

缺少 Request 层校验（V1/V2/V7）**不直接导致**该 NRE，但**必然导致**这类输入无人拦截：既没有契约层的第一道闸门，也没有入口显式校验调用（V8），非法值只能靠 Manager 的零散手写校验兜底，而 8 个方法的 `Id` 连兜底都没有——这正是「省略 `id` 与传入非法 `id` 表现出两种不同错误形态」的直接原因。

---

## 7. 审查局限

| 编号 | 局限 | 影响范围 | 说明 |
|---|---|---|---|
| **L1** | 裸 `[WorkUnit]` 不开事务的结论来自**反编译源码**（`SourceConfirmed`），**未做真实运行观测** | 第 4.0 节、V13/V15 的结论方向 | `backend-command-query-event.md` §4.2 明确「框架源码或反编译只能形成 `SourceConfirmed` 证据」。`Server/design.md:368` 要求的「实施首批次用一个写方法观察是否存在显式事务」本轮**未执行**（本轮为只读审查，未连数据库、未调用接口）。建议保留为实施验证门 |
| **L2** | Guid 缺陷（第 6 节判断 C）的**具体触发机制未确证** | 第 6.2 节 C、6.3 验证建议 | 未取得真实堆栈；F1 只记录归因位置。已排除的解释与未能排除的解释均已在 6.2 写明。**不得**把 C 当作结论使用 |
| **L3** | `FlushAsync` 与事务 `Commit` 的相对顺序**未能从反编译确定** | 第 4.1 节 | 两者是平级 `IWorkUnitFilter`，注册顺序由框架装配决定；`dy-framework-workunit.md` §3 本身就把该顺序列为「待核实风险」。本阶段事件只登记不发布，故不影响本阶段判定；若为事件增加外部推送，必须先取证 |
| **L4** | 未核对**阶段 2** 请求与互认配置链路的完整入参校验状况 | V14 仅覆盖「无 WorkUnit」 | 本报告按要求忽略 `*MutualRecognitionItem*` 请求文件；V14 只记录由阶段 1 审查范围外溢可见的 WorkUnit 缺失 |
| **L5** | 未执行任何真实数据库读/写、未调用真实接口、未运行测试套件 | 全部结论 | 本轮为纯静态审查。V3/V4/V5 判断的「数据库层报错而非友好校验」依据的是建表脚本列长度（`Scripts/medical_standard_*.sql`）与 SqlMap 参数绑定，**推断链完整但未实测**（验证建议第 5 项） |
| **L6** | `[WorkUnit(UseTransaction = true)]` 的隔离级别与超时未核对 | V13 附带 | 特性默认 `IsolationLevel = ReadCommitted`、`Timeout = null`（`WorkUnitAttribute` 源码），但真实数据库会话的实际隔离级别未核实 |
| **L7** | 架构测试的覆盖度只做了静态阅读 | V16 | 本轮未运行 `dotnet test`，未确认 `Stage1ArchitectureTests`/`Stage1SqlMapProbeTests` 当前是否全绿。评审期间曾为取证执行过 `dotnet build`（`-t:Rebuild`，带 `-p:EmitCompilerGeneratedFiles=true`），**未修改任何源码**；生成物落在 `obj/Debug/net10.0/generated/`，属构建中间产物 |

---

## 8. 整改优先级建议（按先易后难、影响面对齐排序）

1. **先补契约层最小闸门（V1/V2/V8，最高优先）**：新增 `MedicalRecognitionRequestValidator`（照 `LisCenterRequestValidator.cs` 21 行），在 12 个写入口的 `MapTo` **之前**调用；同时给属性补 `[Required]`。这是唯一能同时改善 V3–V7 所有表现的单一动作，且不涉及数据库与框架行为。
2. **补长度约束（V3/V4/V5）**：`Name`/`Code` 加 `[StringLength(200)]`、`Remark` 加 `[StringLength(600)]`，与建表脚本逐列对齐。改动小、收益直接（把数据库 500 变成友好校验失败）。
3. **补齐 8 个方法的领域 `ValidateId`（V7）**：让「参数为空」与「业务对象不存在」在消息上可区分，闭合 V48 的验收口径。
4. **补架构测试（V16 + V12）**：静态扫描禁止项（`TransactionScope`/`IDbTransaction`/`BeginTransaction`/自定义 UnitOfWork）与「入口必须调用统一校验器」，把第 1–3 步的成果固化为回归防线。
5. **文档口径修正（V13/V15/V9/L1/L3）**：不改代码，只把裸 `[WorkUnit]` 的确认语义、两个 `UseTransaction = true` 的**非规范强制**性质、查询空值语义写入 `Server/design.md` 的框架机制映射与入参边界。
6. **Guid 缺陷先取证再动手（第 6.3 节，验证建议 1→2→3）**：先拿到真实堆栈确证机制，再决定是否需要超出第 1 步的改动。**在第 1–3 步落地后重放四种 `id` 输入作为验收口径**。
7. **阶段 2 前置（V14）**：4 个互认配置写方法的 WorkUnit 声明与事务边界，纳入阶段 2 设计表，不沿用本阶段结论。

---

### 附：本报告引用的关键源码位置速查

| 内容 | 位置 |
|---|---|
| 12 个写入口与 WorkUnit 特性 | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:20-138` |
| 领域校验方法 | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs:135-140` |
| 映射白名单生成物 | `server/Dy.MedicalRecognition.Application/obj/Debug/net10.0/generated/Dy.Core.SourceGen/Dy.Core.SourceGen.Generators.ObjectMapAssemblyGenerator/*.g.cs` |
| `ChangedProperties` 生成物 | `…/IPropertyChangedAwareGenerator/*Request.g.cs` |
| 列长度依据 | `server/Dy.MedicalRecognition.Repository/Scripts/medical_standard_{category,group,item}.sql` |
| WorkUnit 特性语义 | `Dy.Core.Abstractions 1.1.0.54` → `Dy.Core.Abstractions.Http.WorkUnitAttribute`、`.TransactionInfo` |
| 事务拦截入口 | `Dy.Apron 1.1.0.48` → `Dy.Apron.Http.ActionUnit.TransactionActionUnitFilter`、`.TransactionManager`、`.WorkUnitEndpointFilter` |
| 异常翻译为 500 | `Dy.Apron.Http.ActionUnit.ResultActionUnitFilter.OnFailedAsync` |
| 事件过滤器 | `Dy.Apron.Http.ActionUnit.EventBusActionUnitFilter` |
| 参照校验器 | `E:\MedSync\Dy.LisCenter\server\Dy.LisCenter.Application.Contracts\Validation\LisCenterRequestValidator.cs` |
| 参照校验契约测试 | `E:\MedSync\Dy.LisCenter\server\Dy.LisCenter.Application.Tests\ApplicationAggregate\ApplicationServiceValidationContractTests.cs` |
