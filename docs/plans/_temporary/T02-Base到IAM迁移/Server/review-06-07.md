# T02 票 06、票 07 后端审查

## 审查范围与基线

- 票 06「数据模型扩展（expand）」：四张表新增 10 个组织实例 Id 列、四个聚合实体新增可空 `Guid` 属性、四份建表脚本与 `Scripts/Migrations/20261023_t02_organization_instance_ids.sql`、六处 SqlMap 守卫测试同步。
- 票 07「报告采集与生命周期切换」后端面：报告提交、作废、处理结果、引用、匹配写路径按组织实例 Id 读写，报告读路径与操作人切换。
- 当前工作区包含票 08、票 10 的在改内容（`Stage2WritePathTests`、`Stage3WritePathTests` 修改时间 2026-10-24 06:22，`mrec_platform_parameter.sql` 为 2026-10-24 06:26，晚于票 06/07 的实施记录 2026-10-23 14:56）。本次结论按可归属到票 06/07 的文件与行号给出；全量测试的当前状态见 B-08。
- 本次实际执行：`dotnet build Dy.MedicalRecognition.slnx` → 0 错误；`dotnet test Dy.MedicalRecognition.slnx --no-build` → 43 失败 / 876 通过 / 919；定向运行票 06/07 相关行为用例 203 项通过；定向运行票 06 的 DDL 守卫子集 35 项通过；`git diff --check` 无输出。

**结论：票 06 通过（附建议项）；票 07 后端面不通过（存在阻断项）。**

## 阻断项

### B-01 报告写入的组织实例 Id 没有进入实体，uuid 列实际为空

- 位置：`server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs:65-70`；`server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Submission.cs:221`、`:236`、`:256-285`；`server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReport.xml:59-62`。
- 依据：票 07 判据「报告写入时组织、医院、院区取自可信上下文」；`Server/design.md` 第 4 节「Command 的组织、医院、院区字段改为 Guid」与第 1 节「操作人取 IAM UserId」。
- 事实：应用服务给命令填入了 `OrganizationId/HospitalId/BranchId`，但 `CreateReportIfAbsentAsync` 只接收三个字符串编码，创建实体时只赋值 `OrganizationCode/HospitalCode/BranchCode`（第 267-285 行），没有给实体的三个 `Guid?` 属性赋值；SqlMap 的 insert 虽列出 `organization_id/hospital_id/branch_id` 并绑定 `$OrganizationId` 等参数，实际绑定值来自实体，最终写入空值。提交测试只断言旧编码列等于替身 Id 的字符串（`Stage4SubmissionPathTests.cs:129-131`），没有断言三个实例 Id 列。
- 期望改法：把 `IamOrganizationScope` 或三个实例 Id 传入 `CreateReportIfAbsentAsync`，创建实体时赋值 `OrganizationId/HospitalId/BranchId`；补一条用例断言新建报告的三个实例 Id 列等于当前工作身份的标识。
- 影响：票 06 新增的三列在报告表上始终为空，票 07「按实例 Id 读写」没有成立；依赖实例 Id 的下载归属比对会拒绝所有报告（见 B-03）。

### B-02 匹配、处理结果、引用写路径仍是旧可信范围与旧操作人

- 位置：`server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Matching.cs:58-60`；`MedicalRecognitionReportAppService.ProcessingResult.cs:38-40`；`MedicalRecognitionReportAppService.Reference.cs:41-43`；`server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Matching.cs:93`；`server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/RecognitionMatchRecord.xml:8-21`、`:61-77`。
- 依据：票 07 交付范围明确列出「处理结果、引用、匹配写路径改为写 uuid 列」；`Server/impl.md:70-71` 也声称这些入口已从 IAM 范围取三层实例标识。
- 事实：三个入口仍调用 `TrustedScopeResolver.ResolveWithBranchOrThrowAsync(HttpRequestInfo, ...)` 并从 `HttpRequestInfo.UserId` 取操作人；匹配记录创建只写 `ReceiverOrganizationCode/ReceiverHospitalCode/ReceiverBranchCode`，没有写 `ReceiverOrganizationId/ReceiverHospitalId/ReceiverBranchId`；`RecognitionMatchRecord.xml` 的列清单与 insert 也没有三个新列。处理结果与引用命令仍只带编码。
- 期望改法：三个入口改用 `IamReportScopeResolver` 取当前身份三层实例；给匹配记录实体与 insert 补三个 `receiver_*_id` 列；处理结果与引用链按匹配记录的实例 Id 校验归属；补对应行为用例。
- 影响：票 07 声称完成的三个写入口没有切换；在 IAM 下`HttpRequestInfo` 的机构字段为空，相关入口会在可信范围解析处失败；匹配记录的新列永远为空。

### B-03 报告读路径未切换，实例 Id 字段没有数据来源，下载比对会把所有报告判为越权

- 位置：`server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs:71-80`、`:202-208`、`:256-274`；`server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQuery.xml:306-308`、`:406-408`、`:459-461`；`server/Dy.MedicalRecognition.Application.Contracts/Queries/Reports/MedicalReportListReadModel.cs:18-22`；`server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs:199-205`。
- 依据：票 07 判据「版本与详情返回组织实例 Id 与对应名称」「本院入口按可信院区限定范围」；`Server/design.md` 第 4 节「查询过滤条件由等值文本比较改为等值 uuid 比较」。
- 事实：报告查询应用服务仍用 `TrustedScopeResolver` 与 `OrganizationPathResolver`（Base 编码），列表、范围、版本文件三个 SqlMap 语句仍只 select `organization_code/hospital_code/branch_code`，没有 select 三个新列；公开的 `MedicalReportListReadModel` 只有编码与名称，没有实例 Id。`MedicalReportVersionFileItem` 的三个 Id 字段在真实查询里恒为空，而 `OpenReportVersionPdfAsync` 用 `file.OrganizationId == scope.OrganizationId` 判定归属，实际会对所有报告返回「报告不在当前可信组织、医院与院区范围内」。
- 期望改法：三个查询语句补 select 三个实例 Id；仓储映射与公开读模型补实例 Id 字段；查询应用服务改用 IAM 范围与名称来源；补一条真实 SqlMap 或真实库下载用例，证明文件归属按实例 Id 成立。
- 影响：报告列表、版本列表、详情与下载在 IAM 数据上不可用；票 07 判据 2、3 不成立；票 06 新增列在读路径没有被使用。

### B-04 操作人只改了提交与作废，其他报告写入口仍取 `HttpRequestInfo.UserId`

- 位置：`MedicalRecognitionReportAppService.Matching.cs:60`；`MedicalRecognitionReportAppService.ProcessingResult.cs:40`；`MedicalRecognitionReportAppService.Reference.cs:43`；`MedicalRecognitionReportAppService.Submission.cs:251-256`（仅提交/作废入口已改）。
- 依据：票 07 判据「操作人取自登录账号」；`Server/design.md` 第 1 节「操作人：Command 的 OperId 取 RequireCurrent().UserId」。
- 事实：匹配、处理结果、引用仍用 `Guid.TryParse(HttpRequestInfo?.UserId, ...)`；测试 `Stage5WritePathTests.cs:132`、`:1348`、`:2111` 仍以 `TrustedOperId`（旧请求上下文）断言这些入口的操作人，说明旧链路仍在运行。
- 期望改法：所有报告写入口统一走 `currentIamContext.RequireCurrent().UserId`；把上述测试的期望改为 IAM 身份，并增加一条旧上下文与 IAM 身份不同值的用例，证明写入取 IAM 值。
- 影响：票 07 的操作人判据只在一个入口成立；在 IAM 下其余入口取不到用户，或在仍能取到旧值时写出错误操作人；V15 的「应用面」证据不成立。

### B-05 票 07 没有任何行为 RED 或新增用例，现有用例无法区分新旧数据来源

- 位置：`Server/impl.md:69-76`（票 07 执行记录）；`Stage4SubmissionPathTests.cs:29-35`、`:336`；`Stage5WritePathTests.cs:132`、`:1348`、`:2111`。
- 依据：`AGENTS.md` 第 2 节第 4 条与 `testing-baseline.md` 第 1 节「新功能和行为变化先获得准确失败测试」；`Server/impl.md` 票 06/07 都只记录「919 通过」，没有 RED→GREEN 记录。
- 事实：票 07 没有新增测试文件或测试方法，测试总数与票 06 相同（919）；`Stage4SubmissionPathTests` 的 `TrustedOrganization` 等期望值由 `FakeIamScope.OrganizationId.ToString()` 算出，和被测代码使用同一个源，属于同义反复；同一用例仍安装 `TrustedRequestContext.Use(...)`（第 336 行）并把旧上下文设成相同的 Id 字符串，即使实现回退到 `HttpRequestInfo` 也会通过。没有断言 `report.OrganizationId`、`RecognitionMatchRecord.ReceiverOrganizationId` 或 `oper_id == Iam UserId`。
- 期望改法：在实现前写能失败的用例：旧上下文放一组不同编码、IAM 替身放另一组实例 Id，断言写入实体的是 IAM 实例 Id；断言匹配记录三个实例 Id 与 IAM 用户操作人；记录失败输出和转绿输出。
- 影响：票 07 的行为变化无法被测试发现，B-01 至 B-04 都能在 919 全绿的情况下存在。

### B-06 读路径新增的三个实例 Id 字段没有实际接线，属于未使用字段

- 位置：`server/Dy.MedicalRecognition.Domain/Queries/MedicalReportListFilter.cs:17-26`；`MedicalReportListItem.cs:18-28`；`MedicalReportScopeItem.cs:14-22`；`MedicalReportVersionFileItem.cs:17-25`。
- 依据：票 06 工单第 9 行「Request/Command/ReadModel 的实例 Id 字段随票 07/08 实际接线时新增，避免未使用的接口字段」；审查口径第 8 条「是否只加了实际用到的字段」。
- 事实：`MedicalReportListFilter` 的三个 Id 没有进入 `MedicalRecognitionReportQueryAppService.Reports.cs:44-54` 的筛选构造，也没有对应 SqlMap 条件；`MedicalReportListItem` 与 `MedicalReportScopeItem` 的三个 Id 没有 select 来源；`MedicalReportVersionFileItem` 的三个 Id 仅被 `OpenReportVersionPdfAsync` 读取，但查询不返回。四个模型的 Id 字段目前都没有真实数据来源。
- 期望改法：随 B-03 一起把读路径 SQL、筛选、映射接好；若本票不做，删除这些字段并在票 07/09 实际接线时再加，保持票 06 的最小改动原则。
- 影响：字段冻结进接口测试但没有行为，后续实现容易误以为读路径已切换。

### B-07 状态与记录把未完成的票 07 写成完成，V10/V15 的 Passed 缺少支撑

- 位置：`docs/plans/_temporary/T02-Base到IAM迁移/testReport.md:38-39`；`Server/impl.md:68-69`、`:75`；`docs/plans/_temporary/T02-Base到IAM迁移/迁移-Tickets/07-报告采集与生命周期切换.md:7-12`；`docs/plans/_temporary/T02-Base到IAM迁移/impl.md:17`。
- 依据：`testing-baseline.md` 第 3 节「Passed 必须是该验证面的完整通过证据」；`planning-and-evidence.md` 第 4 节文档一致性；票 07 四条判据。
- 事实：`testReport.md:38` 把 V10 标 `Passed`，但 `Server/impl.md:68` 明确「不执行任何 DDL」，真实库没有验证列、约束与索引；`testReport.md:39` 把 V15 标 `Passed（应用面）`，但没有任何用例断言 IAM 操作人；`Server/impl.md:69` 自己写 `[~] 票 07（后端部分完成）`，票文件第 7 行仍是 `ready-for-agent`、四条判据全部未勾，根 `impl.md:17` 却写「后端完成」。
- 期望改法：V10 分为结构面与真实库两面，真实库面记 `NotRun` 或 `PendingRetest`；V15 在补出操作人用例前记 `NotRun`；票 07 状态按实际改为进行中，四条判据逐条勾选；根 `impl.md` 与本文件保持一致。
- 影响：阶段报告和票状态对同一事实给出三种结论；票 12 收口时无法据此判断票 07 是否完成。

### B-08 审查基线不成立：当前工作区全量测试 43 失败，919 通过无法复现

- 位置：`server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs`（金额与互认配置路径，失败 20 项）；`Stage1SqlMapProbeTests.Sql_map_resources_stay_directly_under_one_folder`（期望 20、实际 21）；`Stage7ExclusionGuardTests.V4_ddl_scripts_freeze_delivered_tables_only`（实际多出 `mrec_platform_parameter.sql`）；`server/Dy.MedicalRecognition.Repository/Scripts/mrec_platform_parameter.sql`（2026-10-24 06:26）。
- 依据：`AGENTS.md` 第 2 节第 7 条完成检查；`Server/impl.md:66`、`:75` 的 919 通过 / 0 失败结论。
- 事实：当前工作区的 `dotnet test Dy.MedicalRecognition.slnx --no-build` 为 43 失败 / 876 通过 / 919。失败集中在票 08 的金额与互认配置路径（`Stage3WritePathTests`），以及票 10 新增 `mrec_platform_parameter.sql` 后未同步的 DDL 守卫。票 06/07 的实施记录时间早于这些改动，当前工作区不是票 06/07 的干净快照。
- 期望改法：由 lead 确认票 08、票 10 改动归属并隔离出票 06/07 的复核快照；在快照上重跑全量，或将票 06/07 结论限定为「结构面/应用面」并注明全量未复现。不要在票 06/07 的记录里保留 919 通过 / 0 失败。
- 影响：票 06/07 的完成数字没有可复现证据；当前工作区全量不绿，不能进入下一票的验证基线。

## 建议项

### S-01 票 06 的守卫改动属于等价冻结，但没有记录先失败后通过

- 位置：四份建表脚本 2026-10-23 13:56；`Stage4SqlMapTests.cs`、`Stage5SqlMapTests.cs`、`Stage1SqlMapProbeTests.cs`、`Stage7ExclusionGuardTests.cs` 为 14:01，`Stage3SqlMapTests.cs` 为 14:46；`Server/impl.md:61-68` 只有通过记录。
- 依据：`testing-baseline.md` 第 1 节与 `AGENTS.md` 第 2 节第 4 条。
- 事实：守卫用例在脚本加列之后才修改，记录中没有一次「加列前运行失败」的输出；现有冻结是逐项等值，不是放宽。
- 期望改法：在票 06 记录一次静态失败证据（把新增列从脚本临时移出，运行对应守卫见失败，再恢复），或按静态 finding 说明替代证据与残余风险。

### S-02 新增迁移文件没有静态核对

- 位置：`server/Dy.MedicalRecognition.Repository/Scripts/Migrations/20261023_t02_organization_instance_ids.sql`；现有 SqlMap 守卫只读 `Scripts` 下的四份建表脚本。
- 依据：`project-context.md` 第 4 节「建表脚本与迁移文件双写，两处不得漂移」；审查口径第 3 条。
- 事实：没有测试读取该迁移文件，列名、类型与注释同四份建表脚本的一致性只靠人工核对。
- 期望改法：补一条静态用例，按表名逐列比较迁移文件与建表脚本的新增列和注释；或在票 06 记录人工核对结果与残余风险。

### S-03 Stage1 注释与断言数量不一致

- 位置：`server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs:480` 注释仍写「8 个列」，`:487` 断言为 9 条列注释。
- 依据：审查口径第 3 条「注释条数与列条数是否与 DDL 实际一致」。
- 事实：断言已按 9 列更新，注释没有同步。
- 期望改法：把注释改为「9 个列」。

### S-04 两处中文注释出现乱码

- 位置：`server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:572`（「抛出銆」）；`server/Dy.MedicalRecognition.Domain/Queries/MedicalReportListItem.cs:18`（「来源褰掑睘属」）。
- 依据：`csharp-documentation-and-reuse.md` 第 2 至 3 节注释质量。
- 事实：注释文本编码损坏，读者无法获得正确语义。
- 期望改法：改为正常中文「抛出。」与「来源归属的一部分」。

### S-05 票 07 的读路径接口字段没有公开读模型承载

- 位置：`MedicalRecognitionReportQueryAppService.Reports.cs:266-282`；`MedicalReportListReadModel.cs:18-22`。
- 依据：票 07 判据「版本与详情返回组织实例 Id 与对应名称」。
- 事实：公开读模型只有编码与名称，没有实例 Id；内部 `MedicalReportListItem` 新增的 Id 没有进入映射。
- 期望改法：随 B-03 一起在公开读模型补三个实例 Id 字段并映射，或者明确本票只交付内部模型、公开接口切换归票 09，并把判据改写。

### S-06 报告范围解析器没有独立用例

- 位置：`server/Dy.MedicalRecognition.Application/OrganizationScope/IamReportScopeResolver.cs:39-73`。
- 依据：`csharp-backend-testing.md` 第 2 节应用与领域测试；票 07 判据 1、2、4。
- 事实：`ReadRequiredScopeAsync` 的缺失层拒绝、`ResolveSubmittedScopeAsync` 的归属拒绝都没有用例；现有 `T02OrganizationScopeTests` 只测底层 `IamOrganizationScopeResolver`。
- 期望改法：补报告层解析器的缺失层、停用、多祖先与跨医院提交用例，或把这些分支作为票 07 的未覆盖面登记。

## 已核对通过项

- P-01 四份建表脚本分别新增 3、3、3、1 个可空 `uuid` 列，合计 10 列，列名与注释齐全；旧编码列保留，建表脚本不含 `alter`。
- P-02 迁移文件按四张表逐列 `add column if not exists` 并补注释，列名、类型与四份脚本一致；文件名为 `20261023_t02_organization_instance_ids.sql`，符合日期加票号的命名。
- P-03 四个聚合实体新增 10 个可空 `Guid` 属性并带中文 XML 注释，旧编码属性与旧读写路径保持；接口字段按票 06 原则延后到实际接线。
- P-04 六处 SqlMap 守卫的列清单与注释清单一律逐项等值，没有改成「包含即可」或删除条目：`Stage1SqlMapProbeTests.cs:455-493`、`Stage3SqlMapTests.cs:39-166`、`Stage4SqlMapTests.cs:43-61`、`Stage5SqlMapTests.cs:37-67`、`Stage7ExclusionGuardTests.cs:456-459`；定向运行 35 项（Stage3、Stage4、Stage5 的 DDL 守卫与 Stage7 的 V2/V3）全部通过；Stage1 列注释冻结 1 项单独运行通过。
- P-05 票 06 的改动范围限于 DDL、实体与守卫，没有新增请求/读模型字段；报告提交与作废入口已改为从 IAM 范围取三层并取 IAM 操作人，提交与作废命令带实例 Id（但未进入实体，见 B-01）。
- P-06 本次改动没有新增 `Authorize`、`AllowAnonymous`、`RequirePermission` 或角色判定；没有新增 `try/catch`、异常包装、响应结构或错误中间件；没有新增并发或强制失败事务用例；没有直接写库造数。
- P-07 `git diff --check` 无输出；当前构建 0 错误；票 06/07 相关行为用例 203 项通过，DDL 守卫子集 35 项通过。全量 919 的 43 项失败见 B-08，不计入票 06/07 的通过证据。
