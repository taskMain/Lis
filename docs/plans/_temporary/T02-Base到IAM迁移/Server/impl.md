# T02 后端实施

设计见 [后端设计](design.md)，票与依赖见 [阶段实施](../impl.md)。

## 票映射

| 票 | 后端范围 | 矩阵编号 | 当前状态 |
| --- | --- | --- | --- |
| 02 | 依赖与装配、当前人与操作人、旧认证退出 | V1～V4、V14、V17 | 已完成（2026-10-23，回改完成待复审） |
| 04 | 组织范围解析：目录、组织树、拒绝语义 | V5～V9、V16 | 已完成（2026-10-23） |
| 06 | 数据模型扩展（口径已变更：迁移期新增的 10 个 `*_id` 列退出） | V10（结构面） | 完成（待审查）；结构面证据待重新取证 |
| 07 | 报告链路按 IAM 组织业务编码读写 | V10、V15 | 后端面完成；证据待按新口径重新取证 |
| 08 | 互认项目与金额按 IAM 组织业务编码读写 | V10、V15 | 后端完成（2026-10-23）；证据待按新口径重新取证 |
| 09 | 统计与导出查询改造 | V16 复核 | 进行中（统计查询与投影完成；真实库与导出前端待补） |
| 10 | 参数表、读取改造与维护接口 | V11～V13 | 进行中（应用层与脚本完成；真实库与 WorkUnit 待迁移执行后补） |
| 11 | 契约收缩：删除迁移期新增的 `*_id` 列与实例属性、Base 依赖退出（`*_code` 列保留） | V17 | 待开工 |

## 执行记录

- [x] 基线：构建与 897 项测试通过，记录于 [阶段测试报告](../testReport.md)。
- [x] 票 02（2026-10-23）：后端 IAM 认证与当前会话。
  - 依赖与版本：新增 `Dy.IAM.Apron.OpenApi 0.2.0.37`，传递解析 `Dy.IAM.Apron 0.2.0.37`、`Dy.IAM.Sdk.AspNetCore 0.2.0.37`、`Dy.IAM.Sdk.Abstractions 0.2.0.37`、`Scalar.AspNetCore 2.16.10`、`Microsoft.OpenApi 2.7.5`、`Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11`；移除 `Dy.Apron.Security 1.1.0.48` 与 `Dy.Plugin.AspNet.Scalar 0.2.0.16`。`dotnet restore Dy.MedicalRecognition.slnx` 通过，无 NU1608。
  - 装配：`MedicalRecognitionApplicationModule` 声明 `[DependsOn(typeof(IamApronOpenApiModule))]`；两个 appsettings 增加配置节 `Iam`（Authority `http://183.224.180.166:35015`、`AllowHttp` true、`ApplicationCode` `medical-recognition`、`ProfileTimeoutSeconds` 5），删除 `JwtSettings`。原 `Dy.Apron.Security` 携带的 CORS 注册迁入应用模块：配置了 `Cors:Origins` 时注册并在认证前挂载。
  - 会话入口：`GET /api/v1/current-session` 读取 `ICurrentIamContext.RequireCurrent()`，返回 `CurrentSessionReadModel` 的 `UserId`、`SessionId`、`WorkIdentityId`、`TenantId`、`OrganizationId`。
  - 目标失败（RED）：`dotnet test Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --filter "FullyQualifiedName~T02IamAuthenticationTests"` → 失败 5、通过 1（源码标识符扫描项在改动前已无违规）。
  - 目标通过（GREEN）：同一命令 → 通过 6、失败 0。
  - 受影响全量：`dotnet test Dy.MedicalRecognition.slnx` → 通过 903、失败 0（基线 897 项 + 本票新增 6 项）。
  - 构建：`dotnet build Dy.MedicalRecognition.slnx --no-incremental` → 0 错误、39 警告（既有可为空标注与测试分析器告警）。
  - 接口取证（`dotnet run --project Dy.MedicalRecognition/Dy.MedicalRecognition.csproj --no-build`，监听 `http://localhost:15014`）：
    - V1：`GET /openapi/v1.json` 200；`/scalar` 302 到 `/scalar/v1`（200）；OpenAPI 安全方案为 `Bearer`（IAM 访问令牌）；路径清单无 `/auth/login`；输出目录无 `Dy.Apron.Security.dll`。
    - V2：匿名请求 `GET /api/v1/current-session`、`POST /Api/MedicalRecognitionReport/CreateMedicalStandardCategory`、`POST /Api/MedicalRecognitionReportQuery/QueryMedicalStandardCategoryList` 均返回 401，响应体 `authentication_required`。
    - V3：`Authorization: Bearer not-a-token` 与 `Authorization: Bearer invalid.token.value` 均返回 401，未降级为匿名；过期令牌走同一验签与有效期校验，未单独构造（无法在不持有签发私钥的条件下造出签名有效且已过期的令牌）。
    - V4：账号 `191617` 登录并签发 `medical-recognition` 应用令牌后，`GET /api/v1/current-session` 返回 200；`userId` `568f0fb4-3b7b-4f55-afb3-d069943ea1a8`，`sessionId` 非空，`workIdentityId`、`tenantId`、`organizationId` 为 null（未选择工作身份，不报错）。
    - CORS：来源 `http://localhost:3008` 的预检 `OPTIONS` 返回 204，响应带 `Access-Control-Allow-Origin: *`。
  - V14：`Stage5ConstraintTests.Client_path_and_exception_handling_stay_consistent` 在本次全量中通过（生产源码无角色门控特性与权限判定调用），新入口未声明授权特性。
  - V17：`T02IamAuthenticationTests` 的包名、配置节与源码标识符判据通过；集中包管理、全部工程文件与 `appsettings.json` 中旧认证包和 `JwtSettings` 为零。
  - 既有守卫同步：集中包管理包名集合、宿主手写控制器集合、受保护差异路径（`appsettings.json` 改由配置内容判据承接，开发配置文件不入库、不在清单）与共享语法工具消费方清单按本票接口约定更新。
- [x] 票 02 回改（2026-10-23）：过期令牌、配置判据、V17 扫描面、干净检出与 CORS 注册五项收口。
  - 过期令牌（V3）：账号 `191617` 签发 `medical-recognition` 业务令牌并读回 `expiresAt` `2026-10-23T09:26:31Z`；到期前 `GET /api/v1/current-session` 返回 200，同一令牌到期后返回 401 `authentication_required`，未降级为匿名。令牌只在验证进程内存中使用，不写文件、不输出。
  - 配置判据：`T02IamAuthenticationTests.Application_settings_declare_the_iam_section_without_shared_secret` 只读取受版本控制的 `server/Dy.MedicalRecognition/appsettings.json`（`appsettings.Development.json` 被 `.gitignore` 覆盖，干净检出不存在），并显式断言 `OpenApi:Enabled`、`Cors:Origins` 与 `HttpConfig:HttpServiceConfigs:Default` 三个键。
  - V17 扫描面：`Project_files_reference_iam_openapi_without_legacy_authentication_packages` 扫描 `server` 下全部 7 个工程文件（6 个生产工程与测试工程），断言旧认证包 `Dy.Apron.Security` 与旧 Scalar 插件包为零、`Dy.IAM.Apron.OpenApi` 由应用工程引用；`JwtSettings` 归零由 appsettings 内容断言与生产源码扫描两项承接，不在工程文件扫描内。
  - CORS 注册：`MedicalRecognitionApplicationModule` 在 `Cors:Origins` 存在时用 `AddCors(options => options.AddPolicy(...))` 注册具名策略 `MedicalRecognitionHostApp`，缺失时不注册；挂载端按策略名 `UseCors`，没有 `as` 转换后的空条件调用。
  - 目标验证（2026-10-23）：`dotnet test Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --filter "FullyQualifiedName~T02IamAuthenticationTests"` → 通过 6、失败 0。
  - CORS 复测：来源 `http://localhost:3008` 的 `OPTIONS /api/v1/current-session` → 204，响应带 `Access-Control-Allow-Origin`。
- [x] 票 04（2026-10-23）：组织范围解析与选择列表。
  - 目标失败（RED）：`dotnet test Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --filter "FullyQualifiedName~T02OrganizationScopeTests"` → 12 通过、1 失败；`Current_scope_returns_empty_levels_when_the_chain_has_no_types` 的夹具把未配置类型的节点挂在已配置类型的医共体下，期望空范围而实际返回该医共体。夹具改为无类型祖先链并补充医院节点用例后复跑 → 14 通过、0 失败。
  - 目标通过（GREEN）：`dotnet test Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --filter "FullyQualifiedName~T02OrganizationScopeTests"` → 通过 14、失败 0。
  - 解析实现：`IamOrganizationScopeResolver` 以当前工作身份的组织为起点读取节点与祖先，按 `OrganizationType.Code` 归类三层；停用节点不参与归类，同一类型多个祖先抛出 `InvalidOperationException`；平台入口按请求提交的组织业务编码在 IAM 组织树内校验存在、启用状态、类型与父级归属（按请求比对，不缓存，不前置到认证链路）；`IamOrganizationDirectoryReader` 按 100 个标识切分并按去重后的标识读取；`OrganizationScopeOptionsBuilder` 按父子关系装配，未配置类型与停用节点不进入选项。
  - V5（真实 IAM，`POST /Api/OrganizationScopeQuery/QueryCurrentOrganizationScope`）：
    - 院区1-1 工作身份 `c762b817…` → 县医共体 `ac2d8c69-68b1-4572-8b01-2aa53ec70068` / 测试医院一 `139cb945-3d6c-4326-9589-db0c9455a7b6` / 测试院区1-1 `6f0cde2b-e8e2-4cb2-8410-7c369ba662ac`。
    - 院区2-1 工作身份 `4a79e80f…` → 县医共体 `ac2d8c69…` / 测试医院二 `e7574c17-7c98-4962-9aeb-8f2c1d3ce506` / 测试院区2-1 `1d18e354-23f9-465b-a263-29a701634a69`。
    - 普通开发 `70590a5f…` → 六个字段全为 null，未按名称或层级深度推断。
  - V6、V16（应用）：`Submitted_scopes_accept_a_valid_path_of_another_hospital` 接受与当前身份不同的合法院区路径；`Submitted_scopes_reject_a_hospital_of_another_organization`、`Submitted_scopes_reject_a_branch_of_another_hospital` 与 `Submitted_scopes_reject_disabled_nodes` 分别按医院、院区、停用节点拒绝；平台入口提交范围只按请求提交的组织业务编码与其父级关系判定，本院入口的当前范围不接受调用方扩大（公开接口没有范围入参）；该批用例的取证口径已变更，需按业务编码重新取证。
  - V7（应用）：`Current_scope_classifies_the_identity_ancestry_into_three_levels` 当前节点与祖先各读取一次；`Submitted_scopes_read_the_directory_once_for_many_targets` 20 条目标只读取一次、去重后 3 个标识；`Directory_reader_splits_batches_at_the_documented_limit` 150 个标识切为 100 + 50 两批。
  - V8（应用与真实 IAM）：`Current_scope_treats_disabled_nodes_as_absent` 与 `Submitted_scopes_reject_disabled_nodes` 通过；真实租户组织树 916 个节点中 52 个停用节点（48 个不同名称），其名称没有出现在选择列表返回的节点中。
  - V9（真实 IAM）：未选择工作身份签发的令牌调用 `QueryCurrentOrganizationScope` → 409 `work_identity_required`；同一令牌调用 `GET /api/v1/current-session` → 200，`workIdentityId`、`tenantId`、`organizationId` 均为 null。
  - 选择列表（真实 IAM，`POST /Api/OrganizationScopeQuery/QueryOrganizationScopeOptions`）：返回 2 个医共体、10 家医院、47 个院区，`unconfiguredOrganizationCount` 为 103；含县医共体、测试医院一、测试医院二与测试院区1-1。
  - 残余：平台入口提交范围的校验点 `ResolveSubmittedScopesAsync` 在本票没有公开接口调用方，真实 IAM 面只覆盖本院入口与选择列表；提交范围的归属判定由应用测试取证，待票 07～09 接入业务入口后补真实接口取证。
  - 收口检查（2026-10-23）：`dotnet build Dy.MedicalRecognition.slnx --no-incremental` → 0 错误、39 警告；`dotnet test Dy.MedicalRecognition.slnx --no-build` → 917 通过、0 失败；`git diff --check` → 无输出。
  - 审查补齐（2026-10-23）：新增 `Options_skip_nodes_whose_organization_type_is_disabled`（节点启用、组织类型停用时不进选项且计入未进入数量）与 `Options_require_a_selected_work_identity`（未选工作身份时抛 `IamWorkIdentityRequiredException`）；用例总数 16。
- [x] 票 06（2026-10-23，数据模型扩展 expand）：
  - DDL（旧口径，已退出）：本票曾为四张表新增可空 `uuid` 实例列——`mrec_medical_recognition_report`、`mrec_organization_hospital_branch_recognition_amount` 各增 `organization_id`、`hospital_id`、`branch_id`；`mrec_recognition_match_record` 增 `receiver_organization_id`、`receiver_hospital_id`、`receiver_branch_id`；`mrec_mutual_recognition_item` 增 `organization_id`。这 10 列属迁移期产物，已退出并由负责人执行删除；`*_code` 列保留，表结构不重建。
  - 实体（旧口径，已退出）：四个聚合实体曾新增对应可空 `Guid` 实例属性；这些属性随 `*_id` 列退出删除，组织、医院、院区属性保持编码文本。
  - 契约字段：Request/Command/ReadModel 的组织、医院、院区字段保持编码文本；随票 07/08 接线新增的实例 Id 字段属旧口径，已退出。本票不改变请求与响应的业务语义。
  - 守卫同步（等价替代，非放宽）：`Stage4SqlMapTests` 报告表列清单、`Stage3SqlMapTests` 金额表列清单与注释清单（注释条数 10→13、列注释 8→11）、`Stage5SqlMapTests` 匹配记录列与注释清单、`Stage1SqlMapProbeTests` 互认项目列清单与注释条数（10→11、8→9）、`Stage7ExclusionGuardTests` 金额表列清单，均按新增列逐项补齐；该批列清单守卫随 `*_id` 列删除回退到编码列。
  - 命令与结果（基于旧口径，证据待重新取证）：`dotnet test Dy.MedicalRecognition.slnx --no-restore` → 919 通过 / 0 失败；构建 0 错误。
  - 脚本位置：最终 DDL 为 `server/Dy.MedicalRecognition.Repository/Scripts/` 下四份建表脚本，组织维度列均为编码列；迁移期为新增实例列产出的增量脚本 `Scripts/Migrations/20261023_t02_organization_instance_ids.sql` 随列退出失效，删除脚本与执行前后校验 SQL 由负责人执行时随交付。
  - 未执行：本阶段不执行任何 DDL；迁移期新增列的删除由负责人执行。
- [~] 票 07（2026-10-23，后端部分完成）：
  - 范围解析：新增 `IamReportScopeResolver`（本院入口取当前工作身份的会话组织，经 IAM 目录读取取三层组织业务编码；平台入口按请求提交的三层业务编码在组织树内比对存在、启用、类型与父子归属，不缓存、不前置到认证链路）；报告链路不再读取 `HttpRequestInfo` 的机构字段。
  - 写路径：提交、作废、处理结果、引用与匹配入口从 IAM 范围取三层组织业务编码并写入 `*_code` 列；本票曾同时写入新 `uuid` 列与编码列的实例标识文本，属旧口径并已退出。
  - 读路径：`MedicalReportListFilter`、`MedicalReportListItem`、`MedicalReportScopeItem`、`MedicalReportVersionFileItem` 的组织维度字段保持编码文本；SqlMap 语句按编码列读写，迁移期实例列读取已退出。
  - 操作人：改取 `ICurrentIamContext.UserId`。
  - 测试：新增共享替身 `FakeIamScope`（三层组织链 + 当前身份，同时实现 `IIamDirectoryClient` 与 `ICurrentIamContext`）；`Stage4SubmissionPathTests` 的可信范围常量与下载用例种子绑定替身标识；`Stage4ContractTests` 的筛选字段冻结清单曾补三个实例标识字段（等价替代，仍逐项冻结）；这三个实例字段随口径变更退出，清单回到组织业务编码字段。
  - 命令与结果（基于旧口径，证据待重新取证）：`dotnet test Dy.MedicalRecognition.slnx` → 919 通过 / 0 失败；构建 0 错误。
  - 写路径回改（2026-10-23，审查阻断项 B-01/B-02/B-04 收口）：互认匹配、处理结果与引用三个入口不再经 `TrustedScopeResolver` 与 `HttpRequestInfo` 取范围与操作人，改为复用 `ResolveReportScopeAsync`（`IamReportScopeResolver.ReadRequiredScopeAsync`）与 `ResolveOperatorId`（`ICurrentIamContext.RequireCurrent`）。匹配记录、处理结果与引用的组织维度字段一律写 IAM 组织业务编码，`MedicalRecognitionReportManager.ProcessingResult` 的归属等值比较因两侧同为业务编码，语义即为按组织业务编码比对，未改动比较逻辑。
  - 写路径用例（2026-10-23）：`Stage5WritePathTests` 的四个可信范围常量改为 `FakeIamScope` 提供的三层取值（当时取实例标识文本，属旧口径），操作人常量改为 `FakeIamScope.UserId`，并删除全部 `TrustedRequestContext.Use` 安装点（`Stage4SubmissionPathTests` 亦然）。因此该两批用例不再依赖旧可信上下文：任何代码回退到 `HttpRequestInfo` 取范围或操作人都会立即整片失败，这正是来源切换的行为证据。可信上下文不可解析的负例改为构造只含医共体的 `FakeIamScope.WithoutHospital()`，断言医院层拒绝文案。该批用例的证据基于旧口径取值，口径已变更，证据待重新取证。
  - 写路径验证（2026-10-23，基于旧口径，证据待重新取证）：`Stage4SubmissionPathTests` 12 通过 / 0 失败；`Stage5WritePathTests` 114 通过 / 0 失败；全量 `dotnet test Dy.MedicalRecognition.slnx` → 937 通过 / 0 失败；构建 0 错误。
  - 未做（转后续）：B-03/B-06 报告读路径与判据 4 的导出请求实例 Id 属旧口径，已随口径变更退出；按新口径仍待补的是读路径与导出按 IAM 组织业务编码过滤的证据。迁移期 `*_id` 列删除见票 11。
  - 未完成：前端页面与 API Client 快照切换（票 07 前端面）；查询与统计链路改按组织业务编码过滤（票 09）；迁移期 `*_id` 列删除（票 11）。
  - B-03/B-06（2026-10-23，票 07 后端面完成）：
    - 范围来源（第 1 步）：`MedicalRecognitionReportQueryAppService` 构造函数注入 `IIamDirectoryClient` 与 `ICurrentIamContext` 并建立 `IamReportScopeResolver`；主文件、Reports、Export、Statistics、Citation、StatisticsExport 的可信范围读取改由该解析点，对外返回编码列，值为 IAM 组织业务编码。
    - 列输出（旧口径，已退出）：`MedicalRecognitionReportQuery.xml` 的 `QueryMedicalReportList`（第 305 行起）、`QueryMedicalReportVersionList`（第 348 行起）、`GetMedicalReportVersionDetail`（第 380 行起）、`GetMedicalReportVersionFile`（第 459 行起）四条 select 曾补 `organization_id`、`hospital_id`、`branch_id` 输出列；实例列读取随口径变更退出，四条 select 只输出编码列，where 按编码列等值。
    - 读模型（旧口径，已退出）：`MedicalReportListReadModel`、`MedicalReportVersionListReadModel`、`MedicalReportVersionDetailQueryReadModel`、`MedicalReportVersionFileReadModel` 曾各补 `OrganizationId`、`HospitalId`、`BranchId`，领域投影同步；这些实例字段随口径变更退出，读模型保留 `OrganizationCode` 等编码字段，由查询投影按编码列填充，`Stage4ContractTests` 的字段清单随之回到编码字段。
    - 下载比对取证（旧口径，证据待重新取证：种子曾按实例标识构造，需改为组织业务编码后复测）：`Stage4SubmissionPathTests.Download_rejects_file_outside_trusted_scope_by_instance_ids`（命中路径为同文件 `Download_of_voided_report_version_is_still_allowed`），前者种子实例标识指向另一条链时按精确文案「业务拒绝：报告不在当前可信组织、医院与院区范围内。」拒绝且文件存储读取 0 次；`Stage4SubmissionPathTests` 13 通过 / 0 失败。
    - 引用详情（旧口径，已退出）：`QueryRecognitionCitationReportContexts` 曾补 `source_organization_id`、`source_hospital_id`、`source_branch_id` 三条投影列与对应实例字段；按新口径引用详情只输出 `source_organization_code` 等编码列，`MedicalRecognitionReportQueryAppService.Citation.cs` 的 `MapReportContext` 按编码填充；`Stage5QueryTests` 的引用详情断言随口径变更改为比较组织业务编码，证据待重新取证。
    - 不在本票范围：统计侧按组织业务编码的投影与过滤随票 09 处理；原计划中的端口与仓储签名改 Guid、DTO 改名与 where 改 uuid 属旧口径，已退出。
    - 票 07 后端面完成项（旧口径，证据待重新取证）：写路径三个入口、四条 select 的实例列输出、四个读模型实例字段消费、引用详情实例标识输出、下载比对命中与不命中两条用例；按新口径需改为业务编码取值后重新取证。
  - 名称字段补齐（2026-10-24）：`MedicalReportVersionListReadModel`、`MedicalReportVersionDetailQueryReadModel`、`MedicalReportVersionFileReadModel` 各补 `OrganizationName`/`HospitalName`/`BranchName` 三个可空字段，映射处直接使用本次已解析的 `OrganizationPath` 名称（不查库、不加接口调用）；`Stage4QueryTests` 版本列表与版本详情用例各新增名称精确相等断言（示范医共体/示范医院/示范院区），文件模型名称在版本详情用例中断言。
    - 最终验证（基于旧口径代码状态，证据待重新取证）：`dotnet build Dy.MedicalRecognition.slnx` → 0 错误；`Stage5QueryTests` 30/30、`Stage4SubmissionPathTests` 13/13、`Stage4QueryTests` 29/29；全量 `dotnet test Dy.MedicalRecognition.slnx` → 936 通过 / 0 失败；`git diff --check` → 无输出。
    - 命令与数字（基于旧口径代码状态，证据待重新取证）：`dotnet build Dy.MedicalRecognition.slnx` → 0 错误；`Stage4QueryTests` 29/29、`Stage5QueryTests` 30/30、`Stage4SubmissionPathTests` 13/13；全量 `dotnet test Dy.MedicalRecognition.slnx` → 936 通过 / 0 失败；`git diff --check` → 无输出。
- [x] 票 08（2026-10-23，后端面）：互认项目与金额切换。
- B-03/B-06 第 1 步收口（2026-10-23）：查询侧范围来源统一完成，全量 `dotnet test Dy.MedicalRecognition.slnx` → 935 通过 / 0 失败；用法例总数由 937 降为 935，原因是两条旧语义用例随设计退出而删除（见下「用例退出」条），不是覆盖收窄。
- 用例退出（2026-10-23，有据可查）：`Export_rejects_when_the_token_and_the_user_profile_disagree` 与 `Export_fills_missing_token_layers_from_the_user_profile` 两条随设计删除——后端设计第 2 节明确「令牌缺层时用用户档案补齐」与「令牌与档案冲突即拒绝」两项语义退出，不再存在两个来源，该两条判据失去被保护对象。其余迁移只做等价替代，未放宽任何断言。
- 用例迁移做法（第 1 步）：查询侧六个文件的旧可信上下文安装点全部删除，组织/医院/院区常量改为 `FakeIamScope` 提供的三层取值（当时取实例标识文本，属旧口径，需改为组织业务编码后重新取证），负例改用 `MissingOrganization()`/`DisabledOrganization()`/`WithoutHospital()` 并断言精确文案；`FakeIamScope` 增加 `MissingOrganization`、`DisabledOrganization`、`BatchReadCount` 与同组织第二家医院/院区节点，供统计入口区分本院与同组织他院。分层读取计数判据（组织 1 次 / 医院 1 次 / 院区 0 次）在 IAM 单次批量读取下失去对象，改为断言目录确实被读取，局部性由按报告版本批量回填的用例承接。
- 仍待做（原计划属旧口径，已退出）：第 2 步 SQL 实例列与读模型消费（含端口与仓储签名改 Guid）、第 3 步导出请求实例 Id；按新口径改为读路径与导出按 IAM 组织业务编码过滤，证据待重新取证。`*_code` 列保留，迁移期 `*_id` 列删除见票 11。
  - 互认项目：`CreateMutualRecognitionItemCommand`、`CreateMutualRecognitionItemsCommand`、`UpdateMutualRecognitionItemConfigurationCommand`、`EnableMutualRecognitionItemCommand`、`DisableMutualRecognitionItemCommand` 的组织维度字段保持编码文本（本票曾新增 `OrganizationId` 实例字段，属旧口径，已退出）；应用服务五个入口改由 `IamReportScopeResolver.ReadRequiredOrganizationAsync` 取当前工作身份医共体的 IAM 组织业务编码并写入 `organization_code`；管理器按组织业务编码查重（启用与停用都占用唯一键），`MutualRecognitionItem.xml` 的列清单、新增与四个定位语句按 `organization_code` 读写。
  - 金额：`SaveOrganizationHospitalBranchRecognitionAmountCommand` 携带三层 IAM 组织业务编码（本票曾改为实例标识字段，属旧口径，已退出）；平台入口按请求提交的三层编码经 `IamReportScopeResolver.ResolveSubmittedScopeAsync` 在组织树内校验存在、启用、类型与父子归属，医院入口取当前身份的医共体与医院、请求院区按编码校验；管理器先查后写并按组织业务编码定位，实体只写编码列。
  - 守卫期望值（等价替代，逐项冻结照旧）：`MutualRecognitionItem.xml` 的 `GetMutualRecognitionItemByOrganizationAndProject` 与 `GetMutualRecognitionItemById`、`UpdateMutualRecognitionItemConfiguration`、`EnableMutualRecognitionItem`、`DisableMutualRecognitionItem` 条件保持 `organization_code = $OrganizationCode`；本票曾改为按 `organization_id = $OrganizationId` 定位，属旧口径并已退出，`Stage2WritePathTests` 三处 SqlMap 断言与 `Stage3WritePathTests.Mutual_configuration_sql_map_exposes_business_key_lookup` 随口径变更回退到编码列。
  - 夹具迁移：Stage2 的命令与配置夹具改用 `FakeIamScope` 提供的组织取值（当时取实例标识，属旧口径），`MatchesOrganization` 兼容层在迁移完成后删除；Stage3 的金额与互认夹具、请求构造与断言随口径变更改为组织业务编码，缺失身份与缺失医院分别用匿名替身与只有医共体的替身构造。
  - 用例结果（基于旧口径，证据待重新取证）：`Stage2WritePathTests` 70 通过 / 0 失败；`Stage3WritePathTests` 25 通过 / 0 失败；`Stage1SqlMapProbeTests` 8 通过 / 0 失败；`Stage3SqlMapTests` 4 通过 / 0 失败。
  - 收口命令：`dotnet build Dy.MedicalRecognition.slnx --no-restore` → 0 错误；票 08 相关用例集 `--filter "FullyQualifiedName~Stage2WritePathTests|FullyQualifiedName~Stage1SqlMapProbeTests|FullyQualifiedName~Stage3SqlMapTests|FullyQualifiedName~Stage3WritePathTests"` → 107 通过 / 0 失败。
  - 全量：`dotnet test Dy.MedicalRecognition.slnx` → 937 总数、935 通过、2 失败；两处均为 OpenAPI 快照路径清单（`Stage2EnumContractTests.Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes` 与 `Stage7ExclusionGuardTests.V1_frozen_openapi_paths_declare_no_excluded_operation`），属票 03/10 的 API Client 快照待重生成，不属本票。
  - 判据 4（2026-10-23；旧口径，证据待重新取证）：该批曾把三个导出请求以可空 Guid 实例字段扩展——`RecognitionProjectConfigurationExportRequest.OrganizationId`、`RecognitionAmountExportRequest.OrganizationId/HospitalId/BranchId`、`BranchRecognitionAmountExportRequest.BranchId`，`MedicalRecognitionReportQueryAppService.Export.cs` 在实例 Id 提供时按实例标识文本参与过滤。按新口径导出请求携带 IAM 组织业务编码，导出过滤按编码列等值，实例字段与按实例标识过滤已退出。用例：`RecognitionProjectConfigurationExportRequest` 相关字段清单回到编码字段，三个导出用例文件 42 通过 / 0 失败基于旧口径，需按业务编码重写后重新取证。
- [~] 票 09（2026-10-23，后端统计面完成；投影口径已变更，证据待重新取证）：
  - 语句投影（旧口径，已退出）：`QueryRecognitionUsageSummaryPage`、`QuerySourceRecognitionSummaryPage`、`QueryRecognitionUsageReminderDetails`、`QueryRecognitionUsageAdoptionDetails`、`QueryRecognitionUsageNonAdoptionDetails`、`QueryRecognitionUsageReferenceDetails`、`QuerySourceRecognitionDetails` 七条数据语句曾逐条补接收侧 `receiver_*` 或来源侧 `source_*` 的实例标识输出列；按新口径七条语句只输出组织、医院、院区编码列，实例列随口径变更退出。
  - 不加列的语句：`CountRecognitionUsageSummaryGroups`、`CountSourceRecognitionSummaryGroups`、`CountRecognitionUsageReminderDetails`、`CountRecognitionUsageAdoptionDetails`、`CountRecognitionUsageNonAdoptionDetails`、`CountRecognitionUsageReferenceDetails`、`CountSourceRecognitionDetails` 七条只做计数，不输出组织维度；`QueryRecognitionUsageSummaryReasons` 只按现有编码分组键汇总不采纳原因，原因行由汇总行按编码键装配，没有实例列消费点，故不加（该批实例列本身已退出）。匹配记录集合视图不属于本批统计 select，保持现状。
  - 读模型（旧口径，已退出）：`RecognitionUsageSummaryReadModel`、`SourceRecognitionSummaryReadModel`、`RecognitionUsageDetailReadModel` 与 `SourceRecognitionDetailReadModel` 曾补两侧实例标识字段，领域投影同步；按新口径这四个读模型的组织维度字段为编码文本（`ReceiverOrganizationCode`、`SourceOrganizationCode` 等），由查询投影按编码列填充。消费点：`Stage6StatisticsQueryTests` 对汇总、来源汇总、四类使用明细与来源明细按编码逐项精确断言；`Stage6StatisticsContractTests` 冻结编码字段清单。该批断言随口径变更需重新取证。
  - 筛选取舍：请求使用 `receiverOrganizationCode`、`sourceOrganizationCode` 等编码字段名，取值为 IAM 组织业务编码；编码列存业务编码，where 按编码列等值。端口与仓储签名保持编码文本，原 where 改 uuid 的方案已退出；本票不改导出 Request 对外字段、不改金额导出筛选参数。
  - 验证（基于旧口径，证据待重新取证）：`Stage6StatisticsQueryTests` 107 通过 / 0 失败；`Stage6StatisticsContractTests` 18 通过 / 0 失败；`Stage6SqlMapTests` 15 通过 / 0 失败；新增 `T02StatisticsInstanceIdTests` 2 通过 / 0 失败（逐条冻结七条数据语句的实例列）随实例列退出失效，改为冻结编码列。构建 0 错误。
  - 全量（2026-10-23，基于旧口径状态）：943 总数、942 通过、1 失败；失败为 `Stage5ConstraintTests.Api_client_package_keeps_the_human_maintained_surfaces`，原因是 API Client 包缺少人工维护锚点，属票 11/前端重生成范围，不属票 09。
  - 待补：真实数据库上的分组聚合、跨页排序与汇总明细对账随真实库复测后补；导出前端/API Client 使用新字段待票 03 前端批次。
- [~] 票 11（2026-10-23，进行中）：契约收缩，批 A 完成，统计侧 B2 回退收口（2026-10-23）：Statistics.cs 与 StatisticsExport.cs 回到字符串口径；修复生产校验缺陷——Usage 汇总与明细两处 EnsureSameOrganization 第二参数误传同一 OrganizationCode，已改回 SourceOrganizationCode，组织不一致重新被拒绝；Stage6 断言同步为字符串/空串语义并移除未启用的统计实例 Id 断言；Branch 导出来源院区改用请求 SourceBranchCode。验证：dotnet build 0 错误、Stage6StatisticsQueryTests 107/107、全量 943 通过 / 0 失败、git diff --check 无输出。原计划的四项 B2 待办（四个 filter、8 处构造、StatisticsExportCommand、SqlMap 条件改 uuid）属旧口径，随口径变更退出。
  - 组织路径解析统一 IAM（2026-10-24）：`OrganizationPathResolver` 删除 `IOrganizationAppService` 字段与全部 `Dy.Base` 引用，唯一数据源为 `IamOrganizationScopeResolver` + `IamOrganizationDirectoryReader`；平台入口 `QueryMedicalReportList` 与 `QueryRecognitionAmountList` 真实库返回 200；平台入口接收组织与来源组织必须一致，跨组织来源按「使用方组织与提供方组织不一致，不能同时按两个组织统计。」拒绝，`Stage6StatisticsQueryTests` 新增用例固定该判据；测试夹具新增四条固定链与 `WithManyHospitals()`，全量 948 通过 / 0 失败。
  - 匹配查询真实库缺陷修复（2026-10-24）：`QueryRecognitionProjectConfigurationList` 投影删除互认配置表不存在的 `m.hospital_id`/`m.branch_id`（配置表只有 `organization_code`），根因是把金额表列名套到配置表且被替身掩盖；`Stage1SqlMapProbeTests` 增加「查询引用 `m.<列>` 必须存在于互认项目配置建表脚本列清单」守卫；真实库 RequestRecognitionMatches 不再报 42703，返回业务拒绝「拟开项目编码…在当前组织不是可用互认项目」（该组织无可用互认配置，属正常业务结果）。
  - 规范轴整改（2026-10-24）：`PlatformParameterController` 改为注入 `IPlatformParameterMaintenanceAppService`；应用服务不再继承 `ApplicationService`、方法去掉 `[NonAction]`，改为在应用模块显式注册接口实现，使框架不生成重复端点（运行时 OpenAPI 仍 64 条）；删除 `MedicalReportListFilter` 的三个实例标识兼容属性与 `MedicalReportScopeItem` 的三个未使用实例 Id 属性；`Stage4QueryTests`/`Stage4ContractTests` 的筛选断言与字段清单保持编码字段。
  - 批 A-1：删除 `MedicalRecognitionReportAppService` 已无调用方的 `trustedScopeResolver` 字段、`new TrustedScopeResolver(userAppService)` 与 `IUserAppService userAppService` 构造参数，同步四个写路径测试构造点（Stage4SubmissionPathTests、Stage5WritePathTests 三处、Stage2WritePathTests、Stage3WritePathTests 两处）；生产源码与测试内 `trustedScopeResolver` grep 为零。`TrustedScopeResolver` 已无调用点并删除；`OrganizationPathResolver` 已改为 IAM 单一来源。
  - 批 A-2：测试替身 `StubSystemParameterAppService` 重命名为 `PlatformParameterRepositoryStub`（文件名同步），同步 14 个测试文件引用；全仓 `StubSystemParameterAppService` grep 为零。
  - 批 B1b（2026-10-23；旧口径，已退出）：该批曾把 `OrganizationPathTarget` 与 `OrganizationPath` 切到实例标识语义，`MedicalReportListFilter` 的组织/医院/院区字段切到实例标识，`CountMedicalReportList` 与 `QueryMedicalReportList` 的 where 按 `organization_id/hospital_id/branch_id` 等值。按新口径组织路径、筛选字段与 where 均保持编码文本（`organization_code/hospital_code/branch_code` 等值），实例标识只用于组织目录读取；`Stage3OrganizationPathTests` 与 `Stage4SqlMapTests` 的对应断言随口径变更回到编码列后重新取证。
  - 批 B2（2026-10-23；旧口径，已退出）：该批曾把报告列表筛选与 SQL 切到 uuid 列等值，统计侧仍以编码字段承载实例标识文本。按新口径报告列表与统计均按编码列等值，uuid 等值与统计导出 Guid 化的后续批次随口径变更取消；原结论（全量 943 通过 / 0 失败、构建 0 错误）基于旧口径，证据待重新取证。
  - 分批结构（后续接手依据）：批 A 清死字段与替身改名（已完成）；原批 B（查询端口与仓储签名改 Guid、筛选与投影改 Guid、Query.xml where 改 uuid）与原批 C（删四张表 `*_code` 列、索引、编码属性与 SqlMap 编码读写）均属旧口径，已取消——`*_code` 列是业务键并保留，唯一键与范围索引落在编码列、不需要重建；批 D 删 Dy.Base 包引用、Base 服务代理配置与全部 using/测试替身照旧（`TrustedScopeResolver` 已删除；`OrganizationPathResolver` 已改为 IAM 单一来源，保留）。迁移期新增的 10 个 `*_id` 列与对应实例属性由负责人执行删除。
  - 验证（基于旧口径代码状态，证据待重新取证）：`dotnet build Dy.MedicalRecognition.slnx --no-restore` → 0 错误；`dotnet test Dy.MedicalRecognition.slnx` → 941 通过 / 0 失败；`git diff --check` → 无输出。
- [~] 票 10（2026-10-23，后端应用层完成）：
  - 表与脚本：新增 `Scripts/mrec_platform_parameter.sql`（`id`、`parameter_code`、`parameter_value`、`description`、`is_valid`、`oper_time`、`oper_id`，参数编码唯一索引 `ux_mrec_platform_parameter_code`）与可重复迁移 `Scripts/Migrations/20261023_t02_platform_parameter.sql`（`create table if not exists` 与 `create unique index if not exists`）；两处列、注释与唯一索引逐字一致，脚本只产出、未执行。
  - 领域：新增 `PlatformParameter` 实体、`PlatformParameterDefinition`（三个编码、中文名、取值规则）、`IPlatformParameterRepository`、`PlatformParameterManager` 与创建/更新命令及对应领域事件；保存前按编码查询是否已存在，存在走更新、未存在走新增。
  - 应用：新增 `PlatformParameterReader`，每次读取当前参数值并按编码校验；`MedicalRecognitionReportAppService.Matching.cs` 的本院报告匹配开关与排除时长、`MedicalRecognitionReportQueryAppService.Citation.cs` 的引用详情有效时长全部改读本应用参数表。新增 `PlatformParameterMaintenanceAppService`（列表与单条保存），保存校验与读取共用 `PlatformParameterDefinition`；操作人取 `ICurrentIamContext.RequireCurrent().UserId`，操作时间取保存时的 `DateTimeOffset.UtcNow`。
  - 接口：新增手写 `PlatformParameterController`：`GET api/v1/platform-parameters` 与 `PUT api/v1/platform-parameters/{parameterCode}`；两个应用服务入口以 `NonAction` 排除自动端点暴露，未声明角色或授权特性。
  - 旧读取点（改动前）：`MedicalRecognitionReportAppService.Matching.cs:111` 与 `:127`（辅助方法 `:144-151` 调 `ISystemParameterAppService.GetSystemParameterByCodeAsync`）；`MedicalRecognitionReportQueryAppService.Citation.cs:103`；注入点 `MedicalRecognitionReportAppService.cs:84/109` 与 `MedicalRecognitionReportQueryAppService.cs:52/69`。现已全部改走 `IPlatformParameterRepository`；生产源码 grep `ISystemParameterAppService|GetSystemParameterByCodeAsync|SystemParameterAggregate` 为零。留给票 11 的只有 `appsettings.Development.json` 的 Base 系统参数代理配置与 `Dy.Base.Application.Contracts` 包（组织/用户服务共用）。
  - 目标失败（RED）：先建立接口与空实现，`dotnet test Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --filter "FullyQualifiedName~T02PlatformParameterTests"` → 15 失败、2 通过；失败均为 `NotImplementedException`（读取、保存与列表入口尚未实现），2 个脚本/映射静态用例已通过。完成行为实现后复跑 → 17 通过、0 失败；追加控制器路由用例后 → 18 通过、0 失败。
  - 验收基线取代：阶段 7 设计 `docs/plans/009-阶段7-横向治理与发布收口/design.md` 第 94 行 DC-10 与第 71 行 EX-19 的“参数配置复用既有系统参数”由 T02 取代；原因是 Base 系统参数服务随迁移整体退出，原前提消失。`Stage7ExclusionGuardTests` 只放行 `/api/v1/platform-parameters` 与 `/api/v1/platform-parameters/{parameterCode}` 两条精确 API 路由，其余参数路由仍判失败。
  - 接口注册（实际启动）：`GET /api/v1/platform-parameters` 与 `PUT /api/v1/platform-parameters/{parameterCode}` 均出现在 `/openapi/v1.json`；匿名 GET 与 PUT 均返回 401，未进入业务。
  - 目标用例覆盖：缺失、停用、非法值分别拒绝；保存拒绝未知编码与非法值；新增写入操作人与操作时间；更新保留原主键；保存后连续读取看到新值；停用后业务读取拒绝；维护列表按编码排序；建表脚本与迁移脚本双写一致；映射五条语句注册与写入布尔参数处理器。
  - 命令与结果：`dotnet build Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` → 0 错误；`dotnet build Dy.MedicalRecognition.Application/Dy.MedicalRecognition.Application.csproj --no-restore` → 0 错误；`T02PlatformParameterTests` 18 通过 / 0 失败；受影响 `Stage5WritePathTests` 114 通过 / 0 失败、`Stage5QueryTests` 30 通过 / 0 失败、控制器守卫 1 通过、`SourceGuardReuseTests` 5 通过、`Stage1SqlMapProbeTests` 8 通过。全量（2026-10-23，API Client 快照重生成后）：`dotnet test Dy.MedicalRecognition.slnx` → 935 通过、0 失败；构建 0 错误。
  - 待补：真实数据库与真实 WorkUnit 取证需负责人执行 `mrec_platform_parameter.sql` 后复测；V11～V13 在阶段报告中记 `PendingRetest` 或“待迁移执行后补”，不写 `Passed`。
  - 技术债（票 11 收缩清单）：
    1. 共享测试替身 `StubSystemParameterAppService` 现实现 `IPlatformParameterRepository`，名称仍表达旧 Base 系统参数服务；为避免再次牵动 15 个测试文件，本票不改名，随旧 Base 依赖删除或改名。
    2. `appsettings.Development.json` 仍保留 `Dy.Base.Application.Contracts.SystemParameterAggregate.ISystemParameterAppService` 代理配置项；业务读取已全部改走本应用参数表，该配置项由票 11 删除。
    3. `Dy.MedicalRecognition.Application` 工程仍引用 `Dy.Base.Application.Contracts`（组织与用户服务共用），本票不能删除；票 11 删除前置条件是组织与用户读取也退出 Base、协议代理配置清理完成并复跑依赖扫描归零。
- [ ] 票 08、09、11 的后端部分。


### 票 08/07 查询侧迁移后续计划（接手用）

1. 范围来源统一（本轮已做）：查询服务构造函数注入 IAM 目录与当前上下文，主文件与 Reports/Export/Statistics/Citation/StatisticsExport 的可信范围读取改由 `IamReportScopeResolver`；平台入口提交范围的编码校验走 IAM 组织树接口，按请求比对存在、启用、类型与父子关系，不缓存、不前置到认证链路。判据：Application 与 Host 构建 0 错误；查询与写路径用例全绿。
2. 读路径按组织业务编码（B-03/B-06）：`MedicalRecognitionReportQuery.xml` 三条 select 输出 `organization_code/hospital_code/branch_code` 并按编码列过滤；端口与仓储签名保持编码文本；读模型编码字段由查询投影实际填充，页面与导出可读取。判据：查询用例按组织业务编码精确断言，报告下载比对用例覆盖命中与不命中。
3. 导出请求编码：三个导出 Request 携带对应层级的 IAM 组织业务编码；Export.cs 与 Query.xml 的导出过滤按编码列等值；三个导出用例文件同步。判据：导出过滤按组织业务编码命中与拒绝，页面范围与导出范围一致。
## 待办与交接

- 目录批量读取与组织树返回结构已在票 04 核对：组织树返回 `nodes`（含父级、排序与启用标记），节点类型需另按标识批量读取；单批最多 100 个标识。
- 参数表查询与写入沿用既有 SqlMap 与 WorkUnit 口径。
- 测试替身按票逐步替换，替换后保持既有断言语义。
- 票 03 需在客户端重新生成 OpenAPI 快照与 API Client：后端文档路径已由 `/auth/login` 变为 `/api/v1/current-session`，并新增 `/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope` 与 `/Api/OrganizationScopeQuery/QueryOrganizationScopeOptions`；`Stage2EnumContractTests` 的冻结路径清单随生成同步更新。
- 业务写入口的操作人读取点仍走 `HttpRequestInfo.UserId` 兼容适配（IAM 模块把该字段适配为 IAM 用户 Id）；V15 的操作人落库验证按矩阵在票 07、08 执行。
- 本机 IAM OAuth 令牌响应不含 `refresh_token`，`iam-api.py --work-identity-id` 在切换工作身份后刷新令牌一步失败；票 04 的带身份令牌改由同一脚本的登录、`/auth/work-identities` 选择与再次授权码登录在内存中取得，令牌不写文件、不输出。
- 宿主启动时框架对 `IUserAppService.QueryAllStaff` 的预热请求返回 401，`ClientProxyBase` 记录一条失败日志；迁移前的 Release 构建同样发出该调用，不影响启动、文档与接口，后续随 Base 服务退出一并处理。
