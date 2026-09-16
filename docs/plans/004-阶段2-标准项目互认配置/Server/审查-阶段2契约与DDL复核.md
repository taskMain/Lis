# 审查：阶段 2 契约与 DDL 复核（Ticket 02 / Ticket 05 DDL 部分）

- 审查角色：独立审查子代理（只读复核，不参与开发，未修改任何被审查文件）
- 审查对象：Ticket 02「互认配置公共契约与生成物整理」、Ticket 05「互认配置表和 SqlMap 物理映射」已声称交付的 DDL 部分
- 审查快照：`main...origin/main [ahead 2]`，HEAD = `dafa6a6`；工作区文件时间戳 2026-09-15 20:42–20:48（服务端/脚本），文档 20:50–20:56
- 边界：Ticket 01（组织与事件机制取证）由另一代理负责，本次不重复审查。审查期间工作区被并发修改（`design.md`、`Server/impl.md` 变更，新增 `Server/取证-组织与事件机制.md`），这些属 Ticket 01 范围，本报告结论基于上述快照。
- 严重程度口径：`Blocker` = 阻止继续；`High` = 违反硬性闸门或票面必交付项缺失；`Medium` = 与规范/设计冲突但影响可控或已被其他票覆盖；`Low` = 一致性、可追溯性或断言强度问题。

---

## 问题清单（按严重程度从高到低）

### H1 — `High`：本轮批次证据未归档到阶段 `testReport.md`，且报告状态与 `impl.md` 互相矛盾

- 位置：`docs/plans/004-阶段2-标准项目互认配置/testReport.md:7`、`:11`、`:19`；`docs/plans/004-阶段2-标准项目互认配置/impl.md` 批次表第 1、4 行
- 证据：
  - `git diff -- testReport.md` 全文件仅 1 行变化（第 5 行阶段 1 收口表述），第 7 行仍为 `- 报告状态：`NotRun``，第 11 行仍为「本轮未修改业务实现代码，未执行建表、迁移……」，第 19 行 V21 仍记 `NotRun`。
  - 同期 `impl.md` 批次表写入「进行中：公共契约基线已完成（V21 契约形状与映射断言 5 条通过）」「进行中：最终DDL脚本已交付并完成静态逐项复核」。
  - 本轮实际改动了 3 个契约文件与 1 个 DDL 脚本；我实跑 `dotnet test` 得 27/27 通过、其中新测试 5 条通过，与 `impl.md` 的「5 条通过」数量一致，但该结论只出现在 `impl.md`。
  - 同属缺失的子项：Ticket 02/05 都要求「先建立契约/脚本静态失败证据」，仓库内只有最终 GREEN 证据；Ticket 05 声称的「16 项静态复核全通过」在全部阶段 2 文档中检索不到出处（`Select-String -Pattern '静态复核|16 项'` 仅命中票面原文）。
- 冲突规范：`.agents/instructions/testing-baseline.md` §4「阶段 `testReport.md` 的位置由 planning-and-evidence 规定……不得只写在聊天、终端或 `impl.md` 中」；`planning-and-evidence.md` §3「实际执行命令、结果、证据、状态和残余风险只写阶段根 `testReport.md`」；Ticket 02/05 的「证据归档到阶段 `testReport.md`」。
- 影响：阶段报告与实施文档状态互相矛盾；后续独立复核者按报告只能得到「V21 未执行」的结论，本轮实际取得的契约证据在权威位置不可见；批次缺少起始边界、失败/静态基线，无法复核 RED 或静态基线的真实性。
- 最小修正建议：在 `testReport.md` 补本轮批次条目（起始工作区、基线/静态失败命令与输出摘要、本轮实际命令及结果、未执行项与残余风险），并把 V21 行与报告状态改为与实际证据一致；「16 项静态复核」需落到可复核的清单或改为可再现的检查命令。

### M1 — `Medium`：Ticket 02 的「OpenAPI、生成入口、锁文件和 API Client 影响清单」未产出，客户端生成物现仍携带已被移除的 `organizationCode`

- 位置：`client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json:1748`；`client/packages/api-client-medical-recognition/src/models/index.ts:359-361`、`:464`、`:1169`；`docs/plans/004-阶段2-标准项目互认配置/阶段2-Tickets/02-互认配置公共契约与生成物整理.md:13`
- 证据：
  - 冻结 OpenAPI 的 `CreateMutualRecognitionItemRequest` schema 仍含 `"organizationCode": { "type": "string" }`（第 1748-1750 行）。
  - Kiota 模型仍声明 `organizationCode?: string | null`（`index.ts:359-361`）并在反序列化（`:464`）与序列化（`:1169`）中读写该字段。
  - 这两个文件本轮未修改（`git status --short` 无命中），仓库内也不存在任何形态的「影响清单」文件；Ticket 02 第 5 项勾选框仍为未勾。
- 冲突规范：Ticket 02 第 5 项交付；`csharp-backend-style.md` §8「契约或数据结构变化前确认设计已批准，并只同步实际受影响的契约面：HTTP Request/Response/枚举检查 OpenAPI、Kiota/API Client 和接口契约测试」。
- 影响：后端契约已删除 `OrganizationCode`，而客户端类型仍接受并可发送该字段；Ticket 06 需要据此判断范围，但本票应产出的影响清单为空，交付范围无法从仓库判断。降级理由：Ticket 06 第 12 行已明确要求「核对创建/查询 Request 的组织字段差异」，客户端再生成归 Ticket 06，故不构成对阶段 2 的即时阻断。
- 最小修正建议：在本票产出影响清单（冻结 OpenAPI 文件、`kiota-lock.json`、生成入口命令、受影响 schema/字段、C22/C24 归属与再生成时机），或把「本票不交付该项」的决定、理由与承接票写进票面与阶段报告。

### M2 — `Medium`：修改既有 symbol 前未见 GitNexus upstream impact 的归档证据

- 位置：`server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/CreateMutualRecognitionItemRequest.cs:11-20`（删除既有公开属性 `OrganizationCode`）
- 证据：阶段 2 正式文档与 `impl.md` 中检索 `upstream|impact|gitnexus` 只命中审查材料文本与票面，无本轮记录。我独立补做：`gitnexus detect-changes --repo Dy.MedicalRecognition` → `变更：2 个文件，2 个符号 / 受影响流程：0 / 风险等级：low`（已变更符号：`CreateMutualRecognitionItemRequest`、`Property OrganizationCode`）；全仓检索显示该 Request 的调用面为 `IMedicalRecognitionReportAppService.cs:132`、`MedicalRecognitionReportAppService.cs:244-250`、`MedicalRecognitionReportDataMaps.cs:19` 的 ObjectMap 声明与新增测试，无宿主（Host 项目）引用。
- 冲突规范：`AGENTS.md` 第 2 节第 3 条；`.agents/instructions/gitnexus-workflow.md` §1.1「修改函数、类、方法或其他代码 symbol 前，必须执行 upstream impact，并记录直接调用方、受影响执行流程和风险等级」。
- 影响：闸门证据缺失（属证据缺口，不是代码缺陷）。实际风险低：影响面已由构建、测试与检索确认为 1 个既有 AppService 入口 + 生成映射 + 新增测试，且未触及公共 HTTP 契约以外的行为。
- 最小修正建议：把 upstream impact 的调用方、执行流程与等级（配合已跑出的 `detect-changes → low`）补记进批次证据，无需回改代码。

### M3 — `Medium`：最终 DDL 的物理表名已改，写入侧 SqlMap 仍指向旧表名与 `current_timestamp`；本票该部分未交付且两处实施文档状态矛盾

- 位置：`server/Dy.MedicalRecognition.Repository/Scripts/mutual_recognition_item.sql:2`；`server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MutualRecognitionItem.xml:1`、`:9`、`:12`、`:15`、`:23`、`:28`、`:31`；`docs/plans/004-阶段2-标准项目互认配置/impl.md` 批次表第 4 行；`docs/plans/004-阶段2-标准项目互认配置/Server/impl.md:18`
- 证据：
  - 脚本已改为 `create table mrec_mutual_recognition_item (`，而 XML 仍为 `Scope="MedicalRecognitionReport"`、`from mutual_recognition_item`、`insert into mutual_recognition_item`，并在 3 处使用 `oper_time = current_timestamp`。
  - 阶段根 `impl.md` 批次 4 记为「最终DDL脚本已交付……写入侧 SqlMap映射随批次2交付」，而 `Server/impl.md:18`（B4 DDL）仍为 `NotRun；只交付可审阅最终脚本，不执行DDL`，两份实施文档对同一交付给出不同状态。
  - `server/Dy.MedicalRecognition.Repository/Scripts/` 下无 `Migrations` 目录、无新增迁移文件。
- 冲突规范：Ticket 05 第 5 项「SQL 显式引用 `mrec_mutual_recognition_item`」、`Server/design.md:132`「INSERT/UPDATE 显式绑定操作时间，不使用 `current_timestamp` 代替 Command 时间」、V17；`planning-and-evidence.md` §4「每个阶段文档只表达所属阶段的最终设计状态」。
- 影响：当前持久化映射与最终 DDL 不一致；若此刻按脚本建表，写入路径会因表名不匹配而失败、且操作时间由库生成而非 Command 提供。因为脚本未执行（见「已确认无问题」第 14 项），不构成数据风险，也不影响后续票的契约工作。
- 最小修正建议：在建表执行前明确该交接点（票面或 `Server/impl.md` 记一行「DDL 已交付、SqlMap 归属 B2」并同步状态），或把 XML 映射面并入本票一次交付；在映射闭环前不执行建表。

### L1 — `Low`：Ticket 02/05 的勾选项与 `Status` 未反映实际交付范围

- 位置：`阶段2-Tickets/02-互认配置公共契约与生成物整理.md:7`、`:9-13`、`:17-19`；`阶段2-Tickets/05-互认配置表和SqlMap物理映射.md:7`、`:9-15`、`:19-21`
- 证据：两票 `Status` 仍为 `ready-for-agent`，全部勾选框为 `[ ]`（文件时间戳 15:20，本轮未修改），而上游 `impl.md` 已记两批为「进行中」并列出交付物。
- 冲突规范：`阶段2-Tickets/README.md:7`（`Status` 是领取标签，不是测试状态）——按此不属强制项，但影响可追溯性。
- 影响：接手者无法从票面判断已交付/未交付边界（例如 Ticket 05 只交付 DDL、SqlMap 延后）。
- 最小修正建议：更新勾选项，或在票面加一行「本轮交付：仅 DDL 脚本；SqlMap 映射随批次 2」。

### L2 — `Low`：映射测试的第三条断言近似同义反复

- 位置：`server/Dy.MedicalRecognition.Tests/Stage2ContractTests.cs:41`
- 证据：我以 `-p:EmitCompilerGeneratedFiles=true` 把当前源生成器输出导出到临时目录后读取，当前映射只写入 `StandardProjectCode`、`RecognitionDurationDays`，完全不触碰 `OrganizationCode`，因此 `Assert.Null(command.OrganizationCode)` 在当前生成器行为下必然成立，不能区分「有意省略」与「映射遗漏」；真正起保护作用的是同测试文件 `:22-26` 的属性集合断言（既覆盖「不含组织编码/内部项目 ID」，也能在再生成重新加回字段时失败）。
- 冲突规范：`.agents/instructions/csharp-backend-testing.md` §6.8（每项关键判定应有能按预期失败的 mutation 证据）、§1.3（契约覆盖）。
- 影响：冗余弱断言，无误导性；不会因应用层注入缺失而失败。
- 最小修正建议：保留属性集合断言即可，或把该断言替换为对应用层注入结果的断言（归属批次 2）。

### L3 — `Low`：创建请求未声明任何校验特性

- 位置：`server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/CreateMutualRecognitionItemRequest.cs:13-20`
- 证据：同目录 `CreateMedicalStandardItemRequest.cs:31-38` 使用 `[Required]`/`[StringLength]`，本文件无 `using System.ComponentModel.DataAnnotations;`、无任何特性；`RecognitionDurationDays` 为 `int`，无法在契约层表达正整数约束。
- 冲突规范：`csharp-backend-style.md` §5「公共 Request 声明必要的 `[Required]`、长度、范围、集合数量和枚举约束」；但 `Server/design.md:40` 只要求本 Request「Host 绑定编码、正整数天数」，V4 归 Contract/Domain，且设计把校验/组织注入统一安排在批次 2（`Server/impl.md` B2）。
- 影响：空串或不存在编码会穿透到领域层才被拒绝；当前 AppService 入口本来就未接入校验（批次 2 未开始），本轮未引入回退。
- 最小修正建议：可不在本票补齐；若批次 2 补齐，注意不要与设计的「正整数天数为业务校验」重复实现两套口径。

### L4 — `Low`：DDL 注释文本与阶段 1 脚本风格不一致（本脚本遵循设计，阶段 1 脚本反而偏离）

- 位置：`server/Dy.MedicalRecognition.Repository/Scripts/mutual_recognition_item.sql:15`、`:27`；对照 `medical_standard_item.sql:16`
- 证据：本脚本 `comment on column ... .id is '主键ID'`、并含 `comment on index ux_mrec_mutual_recognition_org_project is '同一组织标准项目互认配置唯一（含停用配置）'`；阶段 1 脚本为 `'主键 ID'`（含空格）且无索引注释。
- 冲突规范：`.agents/instructions/project-context.md` §4.2 规定 `id` 注释为「主键ID」、`Server/design.md:134` 明确要求 `COMMENT ON INDEX`——本脚本与设计一致，阶段 1 脚本才是偏离方。
- 影响：仅风格差异，无功能影响；后续若做注释统一，应作为独立治理项。
- 最小修正建议：本脚本保持现状；如需统一，另开治理项，不在本票改阶段 1 脚本。

---

## 已确认无问题

以下各项经实际读取、命令或只读数据库核对，未发现问题：

1. **创建请求字段面**：`CreateMutualRecognitionItemRequest.cs:11-21` 只声明 `StandardProjectCode`、`RecognitionDurationDays`（另有生成器补充的 `ChangedProperties`/`HasPropertyChanged`），不含组织编码、内部标准项目 ID、状态；测试断言实际通过（反射属性集合精确匹配）。
2. **请求 XML 注释**：类型 `summary` 说明业务职责，`remarks` 说明可信组织由服务端注入、调用方不提交组织与内部 ID，符合 `csharp-documentation-and-reuse.md` §2/§3（不复述名称、说明可信来源与空值边界）。
3. **ReadModel 位置与命名空间**：`Contracts/Queries/RecognitionProjectConfigurationReadModel.cs:1,4`，namespace `Dy.MedicalRecognition.Application.Contracts.Queries` 与目录一致；旧 `ReadModels/` 文件已删除且无重复声明（构建通过）。
4. **ReadModel 字段逐项对齐设计**：与 `Server/design.md:19` 的 9 个字段一一对应，类型 `Guid`/`string`/`string`/`MedicalItemType`/`string`/`string`/`int`/`ConfigurationStatus`/`string?`；`UnavailableReason` 可空、其余非空；6 个应删字段（`IsStandardCatalogValid`、`IsAvailableForNewMatch`、`CreationTime`、`Creator`、`LastModifiedTime`、`LastModifier`）在类型上确实不存在。
5. **枚举复用**：`ItemType` 为 `MedicalItemType`、`ConfigurationStatus` 为 `ConfigurationStatus`（`Domain.Share/Enums`），未新增替代枚举、未新增枚举文件（本轮未跟踪文件仅 3 个）。
6. **查询请求面与校验写法**：`RecognitionProjectConfigurationListQueryRequest.cs:10-29` 只有设计允许的 3 个字段，`OrganizationCode` 必填（`[Required]` + `[NonEmpty]`），无分页/名称/类型/分类/分组筛选；`sealed record` + `init` + `[EnumDataType]`/`[NonEmpty]` 的写法与阶段 1 `Contracts/Queries/MedicalRecognitionReportQueryContracts.cs:9-59` 一致。
7. **无旧类型残留引用**：全仓检索 `Application.Contracts.ReadModels` 只剩其他历史 ReadModel 自身声明与 UML/文档；`client/` 无 `RecognitionProjectConfiguration*` 命中；无生成物引用被删类型。
8. **`MutualRecognitionItemDto` 未改动且无新调用方**：`git status` 无该文件；全仓仅 `MedicalRecognitionReportDataMaps.cs:26` 的 `[assembly: ObjectMap(...)]` 与自身文件命中；`MedicalRecognitionReportDataMaps.cs` 本轮未被修改。
9. **Contracts 依赖边界**：`Dy.MedicalRecognition.Application.Contracts.csproj` 只引用 `Dy.Core.Abstractions`、`Dy.Apron.Abstractions` 与 `Domain.Share` 项目，未引用 Application/Domain Entity/Repository，符合 `backend-architecture.md` §1 层依赖表与 `Server/design.md:29`。
10. **未新增不必要的抽象**：本轮新增物仅 2 个契约 `record` 与 1 个测试类，无新接口/工厂/服务/配置项/基类/新项目/新依赖包（`csproj` 与 `Directory.Packages.props` 未变），符合 `csharp-documentation-and-reuse.md` §5 抽象预算与 `Server/design.md:36`「不为一个实现增加 Manager 接口或独立互认查询服务接口」。
11. **源生成器映射正确**：未手写映射、未改 DataMaps；导出当前生成器输出核对，`MapToCreateMutualRecognitionItemCommand` 只映射 `StandardProjectCode`、`RecognitionDurationDays`，不再引用 `OrganizationCode`，与 `Server/design.md:13`「组织编码由可信组织上下文注入」一致。（旁注：`server/Dy.MedicalRecognition.Tests/obj/**/generated/` 下仍留有 10:12 的旧转储，其内容含 `OrganizationCode`，属构建产物残留，不是当前编译结果；当前映射以我用 `-p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=<temp>` 重建导出为准。）
12. **构建与测试**：`dotnet build server/Dy.MedicalRecognition.slnx --no-restore` → `0 个错误`（5 个警告均为 git「LF will be replaced by CRLF」的 EXEC warning）；`dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` → `失败: 0，通过: 27，已跳过: 0，总计: 27`；`--filter FullyQualifiedName~Stage2ContractTests` → `通过: 5`；`git diff --check` → 退出码 0，无输出。
13. **DDL 与设计逐项一致**（`mutual_recognition_item.sql`，27 行）：列序与 `Server/design.md:123-130` 完全一致（`id`、`organization_code`、`standard_item_id`、`standard_project_code`、`recognition_duration_days`、`is_valid`、`oper_time`、`oper_id`），类型 `uuid/text/uuid/text/integer/boolean/timestamptz/uuid` 一致，8 列全 `not null`，无数据库默认值；机械检索 `varchar(`、`default`、`references`、`foreign key`、`alter`、`rollback`、`current_timestamp`、`where is_valid`、`if not exists` 命中数均为 0；注释为 1 条表注释 + 8 条列注释 + 1 条索引注释且全部含中文，文本与设计一致；唯一索引 `ux_mrec_mutual_recognition_org_project on (organization_code, standard_project_code)` 无状态过滤、其 `COMMENT ON INDEX` 文案与设计一致；`oper_time`/`oper_id` 严格位于末尾、`is_valid` 位于操作字段之前，符合 `project-context.md` §4.1。未引入设计外对象（无额外索引/约束/回滚脚本/幂等包装）。
14. **未执行任何 DDL/DML**：只读 dbx 连接 `协同平台外网`（`DysoftHIS`）查询 `information_schema.tables`，结果仅 `public.medical_standard_category`、`medical_standard_group`、`medical_standard_item`、`mrec_medical_standard_category`、`mrec_medical_standard_group`、`mrec_medical_standard_item`——**不含 `mrec_mutual_recognition_item`**；`Scripts/` 下无迁移文件。
15. **工作区保护**：本轮无分支、commit、push（`main...origin/main [ahead 2]`，HEAD `dafa6a6` 时间 18:31，早于本轮编辑时间 20:42/20:48，两个领先提交属阶段 1 交付）；`docs/plans/004-阶段2-标准项目互认配置/阶段2审查相关/` 下文件均未修改（时间戳≤16:08，`git status` 无命中）；工作区无临时文件或生成物残留（未跟踪文件仅本轮的 3 个新文件 + 并发代理的 1 个取证文件）。
16. **未发现伪造测试状态**：`testReport.md` 状态仍为 `NotRun`，不存在被改写为通过的情形（问题在「未更新」，见 H1）；`impl.md` 未对阶段 2 使用 `Passed`/`Ready`/`Complete`；其中引用阶段 1 `Complete` 有出处（阶段 1 `testReport.md` 第 3、135 行记录 2026-09-15 收口）。
17. **未发现「用构建成功冒充业务通过」**：`impl.md` 的「V21 契约形状与映射断言 5 条通过」对应 V21 层级为 Contract（`Server/design.md:182`，真实数据库=否），且我实跑 5 条通过；该表述的问题是把结论放在 `impl.md` 而非 `testReport.md`（H1），不是把构建当业务通过。

## 待确认或未验证

1. **并发修改导致的快照边界**：审查期间 `design.md`、`Server/impl.md` 被另一代理修改并新增 `Server/取证-组织与事件机制.md`（Ticket 01 范围）。这些改动若后续触及 Ticket 02/05 的范围（例如改 `design.md:19` 字段清单或 `Server/impl.md` B4 状态），本报告的对应条目需局部复跑。
2. **「16 项静态复核全通过」的清单与口径**：仓库内无出处，无法复核具体检查项；只核对到脚本正文与设计逐项一致（见第 13 项）。
3. **目标数据库/schema 的确认出处**：`impl.md` 记「目标库已确认为 `DysoftHIS` 的 `public`」，我未找到该确认的书面记录；仅独立核实 `DysoftHIS.public` 确实存在阶段 1 三张 `mrec_` 表、不存在阶段 2 表。
4. **V21 证据的来源命令与时间**：`impl.md` 的「5 条通过」数量与实际一致，但产生该结论的命令、时间与环境未归档，无法确认是否与本轮同一次运行。
5. **Ticket 06 覆盖范围未逐项复核**：`06-累计APIClient生成与契约校验.md:12` 已要求核对组织字段差异，因此 M1 判为 `Medium`；该票其余勾选项未在本任务范围内逐项核对。
6. **批次 2 尚未实现的既有缺口**（非本票缺陷，记录以便交接）：`MedicalRecognitionReportAppService.cs:244-274` 四个互认入口仍是生成原样（未调用 `MedicalRecognitionRequestValidator.Validate`、未注入可信组织、`OperTime` 用 `DateTimeOffset.Now`），`MutualRecognitionItem.xml` 的 scope 仍为聚合名——与 `Server/design.md:13,21,136` 的差异均属批次 2 / B2，且被 S2-D12 阻断；本轮移除 `OrganizationCode` 后映射出的实体组织编码为空，写入路径维持不可用状态，未引入新的可用性回退。

## 本次实际执行的命令与结果

| 命令 | 结果 |
|---|---|
| `git status --short --branch` | `main...origin/main [ahead 2]`；本轮改动 = 3 契约文件 + 1 脚本 + 5 文档；未跟踪 3 个新文件 |
| `git diff --stat` / `git diff`（逐文件读） | 见 H1/M1/M3 证据节 |
| `dotnet build server/Dy.MedicalRecognition.slnx --no-restore` | `0 个错误`，5 个警告（git LF/CRLF EXEC warning） |
| `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` | `失败: 0，通过: 27，已跳过: 0，总计: 27` |
| `dotnet test ... --filter "FullyQualifiedName~Stage2ContractTests"` | `失败: 0，通过: 5，总计: 5` |
| `git diff --check` | 退出码 0，无输出 |
| `gitnexus detect-changes --repo Dy.MedicalRecognition` | `变更：2 个文件，2 个符号 / 受影响流程：0 / 风险等级：low` |
| `dotnet build ...Tests.csproj -t:Rebuild -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=<temp>` | 0 错误；导出当前 `MapToCreateMutualRecognitionItemCommand` 仅映射 2 个字段 |
| dbx 只读 `information_schema.tables`（`协同平台外网` / `DysoftHIS`） | 无 `mrec_mutual_recognition_item`；存在阶段 1 三张 `mrec_` 表与旧三张无前缀表 |
| 静态文本核对（列序/类型/注释/禁用模式计数） | 见第 13 项 |

## 结论口径

- 未发现 `Blocker`；契约面与 DDL 面的设计与实现逐项一致，构建、测试、差异检查与只读数据库核对均有实际证据。
- 需优先处理的是证据与交付物缺口（H1、M1、M3），而非代码/脚本本身的正确性缺陷。
- 本报告不使用「验收通过」「Ready」「Complete」等结论；`Passed` 仅用于我本轮实际执行的命令结果，阶段 2 业务矩阵状态以阶段 `testReport.md` 为准。
