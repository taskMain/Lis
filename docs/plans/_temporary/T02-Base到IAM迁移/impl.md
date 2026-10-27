# T02 阶段实施

设计见 [design.md](design.md)，结果见 [testReport.md](testReport.md)，工单见 [迁移-Tickets](迁移-Tickets/)。

## 票与依赖

票按依赖顺序编号，编号越小越先可开始；每票定义见对应工单文件。当前状态依据各工单的完成记录与 [测试报告](testReport.md)「交付结论」（2026-10-26），验证边界以报告为准。

| 票 | 范围 | 前置阻塞 | 矩阵编号 | 当前状态 |
| --- | --- | --- | --- | --- |
| 01 | IAM 应用、菜单与测试身份 | 无 | 宿主入口（C1、C2 前置） | 已完成：应用、身份、菜单与角色已读回，两套身份宿主走查完成，本地开发地址映射已使用 |
| 02 | 后端 IAM 认证与当前会话 | 01 | V1～V4、V14、V17 | 已完成：回改与全量回归已有证据；未另做独立复审 |
| 03 | 前端宿主会话与请求适配器 | 02 | C1、C2、C8、C16 | 已完成：宿主会话、应用令牌与请求适配已取证 |
| 04 | 后端组织范围解析 | 02 | V5～V9、V16 | 通过（含审查补齐 2 项用例） |
| 05 | 前端范围选择器与页头范围条 | 03、04 | C3～C7 | 已完成：组件验证、页面接线与两套身份宿主走查已有证据 |
| 06 | 数据模型扩展（expand） | 04 | V10（结构面） | 已完成：模型与 DDL 保持组织业务编码，10 个迁移期实例列已删除并只读核对 |
| 07 | 报告采集与生命周期切换 | 06、05 | V10、V15、C13、C15 | 已完成：报告链路按组织业务编码读写，提交与操作人已有真实库证据，详情与下载已有宿主证据；取证范围见报告 |
| 08 | 互认项目与金额切换 | 06、05 | V10、V15、C11、C12 | 已完成：项目与金额按组织业务编码过滤，金额保存与导出已有真实链路证据 |
| 09 | 统计与导出切换 | 07、08 | C14、C15 | 已完成：按编码过滤、两套身份走查与导出表头自动化核对已有证据；统计分组与导出文件为抽样核对 |
| 10 | 平台参数自管与维护页 | 03 | V11～V13、C9、C10 | 已完成：参数表、维护页、真实库与宿主走查已有证据；正常运行不可达分支按报告登记 |
| 11 | 契约收缩（contract） | 07、08、09、10 | V17、C16 | 已完成：Base 依赖退出，10 个实例列已删除，组织业务编码列与六条索引保留 |
| 12 | 全量验收与文档同步 | 11 | 全部复核 | 已完成：两套身份各 11 页走查，SRS 与 UML 已同步；交付结论为 `ReadyWithAcceptedResiduals` |

## 执行记录

以下为各批次执行时的历史记录，其中未勾选项与“待验”等描述保留当时状态；当前进度见上表及本文「待办与交接」。

- [x] 迁移前基线：后端构建与 897 项测试通过；前端 lint 0、831 通过 / 2 跳过、构建 0 错误（记录见阶段测试报告）。
- [x] 票 01（2026-10-23，前 3 条判据）：
  - 应用登记：`/Api/Application/QueryApplication {code:medical-recognition}` → id `3a2479da-02e6-3ee2-6863-40b05cbeaaff`、code `medical-recognition`、name 「检验检查结果互认平台」、isEnabled true。
  - 开发租户绑定：`/Api/Tenant/QueryTenantApplication {tenantId:9ae3125b…}` 共 29 条，含该 applicationId。
  - 账号：`191617`（`568f0fb4…`）`/Api/ApplicationContext/IssueApplicationToken {applicationCode:medical-recognition}` 两次实测均签发成功（expiresAt 分别为 08:01:20Z、08:10:29Z）。
  - 组织成员：`191617` 三条成员关系 = 普通开发（`53db8f66…`）+ 院区1（`3ef16a39…`）+ 院区2（`a4a81b21…`）；`lisadmin`（`9982d4ef…`）已加入院区1。
  - 院区归属：组织树读回 院区1 父节点 = 测试医院1（`833118f3…`），院区2 父节点 = 测试医院2（`c435079e…`），分属两家医院。
  - 角色：`medical-recognition-dev`（`2525c0f7…`）已绑定两个账号。
  - 菜单：`/Api/Application/QueryMenuByApplicationId` 读回 10 项，路由分别为 medical-recognition/{standard-catalog, recognition-projects, recognition-amounts, branch-recognition-amounts, report-management, branch-report-management, recognition-usage-statistics, branch-recognition-usage-statistics, source-recognition-statistics, branch-source-recognition-statistics}；`/Api/Role/QueryRoleMenuByRoleId` 读回 10 条绑定。
- [x] 票 01 完成项（2026-10-23）：应用 `medical-recognition` 与开发租户绑定；`191617`、`lisadmin` 均已加入院区级组织（院区2 / 院区1）并分配开发角色；10 个页面菜单已登记并绑定开发角色；`191617` 登录与应用令牌签发实测通过。
- [x] 票 01 IAM 重配（2026-10-24）：IAM 数据曾被回滚，原工作身份 `c762b817-19ec-4b87-a37c-5626cbb56d7c`（测试院区1-1）与 `4a79e80f-bc11-4797-a5f0-841e2de71914`（测试院区2-1）被删除，选择时返回 403 `identity_unavailable`。已重建 `191617` 在测试院区1-1/2-1 的成员关系，新的 organizationUserId 为 `0efdeaf9-a5e9-4820-ab3b-e5b7b84858da` 与 `99955c3b-2df7-404b-86b9-c1c83462eb0d`；新增 `mrecAdmin`（userId `1153a4e9-aba7-436e-9bb4-dc17dfa09c28`，显示名「互认平台管理员」），加入同两个组织并绑定开发角色（organizationUserId `8826eb0d-8cd3-4404-87c2-239b15f76d10`、`94bc58ba-6821-4d5c-96a0-eaac5097100e`）。应用、租户绑定、11 个菜单、角色 11 条菜单读回通过；`/auth/work-identities` 两个账号均可列出测试院区1-1 与测试院区2-1；旧 id 已不存在，不可用于测试。
- [ ] 票 01 剩余：宿主侧子应用登记与本地开发地址映射（外部依赖）。

注：`lisadmin` 登录返回 `interaction_required`（HTTP 200），双医院范围改由 `191617` 的院区1/院区2 两个工作身份承担。
- [x] 票 02（2026-10-23）：IAM 包替换（Dy.IAM.Apron.OpenApi 0.2.0.37）、模块依赖、Iam 配置节、GET api/v1/current-session；T02IamAuthenticationTests 6 项 RED→GREEN；全量 903 通过 / 0 失败；build 0 错误；接口取证见 Server/impl.md。
- [x] 票 02 回改（2026-10-23）：过期令牌、配置判据、V17 扫描面、干净检出与 CORS 注册五项完成；T02IamAuthenticationTests 6 通过 / 0 失败；全量 917 通过 / 0 失败；见 Server/impl.md。
- [~] 票 03（2026-10-23）：会话模块 + 宿主适配器 + 当前账号显示完成（lint 0 / 837 通过 / build 0）；C8 有效会话已在宿主取证（应用令牌适配器，四页请求 200）；C1、C2 与 C16 宿主面待后续。票 03 的 `createRequestAdapter` 接入在跨 Origin 场景不附令牌，已修正为应用令牌适配器。
- [x] 票 04（2026-10-23）：组织范围查询与选择列表完成；T02OrganizationScopeTests 14 通过 / 0 失败；全量 917 通过 / 0 失败；真实 IAM 取证见 Server/impl.md。
- [x] 票 07 前端面（2026-10-23）：`reportsTrustedScope.ts` 与 `recognitionProjectsOrganizationScope.ts` 改读宿主会话的组织、医院、院区实例标识，业务请求与可信范围取该实例经 IAM 目录读取返回的组织业务编码；报告管理与互认项目页面的可信范围测试替身迁到会话 store；新增 `reportsTrustedScope.test.tsx` 覆盖现值读取与身份切换重算。前端 lint 0、test 850 通过 / 2 跳过、build 0 错误；API Client 快照 62 条路径且 `generate` 无差异，包 build/typecheck 0 错误。宿主面 C13、C15 待子应用登记后走查。上述组件面证据基于旧口径取值，口径已变更，证据待重新取证。
- [x] 票 08 后端面（2026-10-23；旧口径以组织实例 Id 作业务键，口径已变更，证据待重新取证）：
  - 互认项目与金额命令、管理器、仓储与 SqlMap 按 IAM 组织业务编码读写，`*_code` 列存业务编码；迁移期写入的实例标识取值与新增的 `*_id` 实例列已退出；应用服务五个互认入口取当前身份医共体的组织业务编码，金额平台入口按请求提交的三层编码在组织树内校验归属，医院入口取当前身份组织与医院并校验请求院区编码。
  - 夹具迁移：Stage2/Stage3 写路径的组织范围夹具与断言改用 `FakeIamScope` 提供的组织取值（当时取实例标识，属旧口径，需改为组织业务编码后重新取证），迁移后删除 `MatchesOrganization` 兼容层。
  - 用例与命令：`Stage2WritePathTests` 70 通过、`Stage3WritePathTests` 25 通过、`Stage1SqlMapProbeTests` 8 通过、`Stage3SqlMapTests` 4 通过，四项失败均为 0；票 08 相关用例集 107 通过 / 0 失败；`dotnet build Dy.MedicalRecognition.slnx --no-restore` 0 错误。
  - 全量：`dotnet test Dy.MedicalRecognition.slnx` → 937 总数、935 通过、2 失败，失败为 OpenAPI 快照路径清单（票 03/10 待重生成），不属本票；真实数据库读写与 V15 待迁移执行后补。
  - 未完成：判据 4 的导出按组织业务编码过滤未按新口径实施（导出实现位于票 07 与另派范围的 `MedicalRecognitionReportQueryAppService.Export.cs`、`MedicalRecognitionReportQuery.xml`，待明确文件归属）；前端 C11、C12 已完成组件面，宿主面待验。
- [x] 票 08 前端面（2026-10-23；取值口径已变更，证据待重新取证）：平台金额页与本院区金额页的可信范围改读宿主会话（`useHostSessionSnapshot` 与票 07 接缝的命令式读取），移除 `@dy/auth`、`@dy/auth-react`、`LoginUserManager` 的可信范围读取；请求字段沿用现有名称，值为本项目范围查询下发的 IAM 组织业务编码（会话组织实例 Id 只用于目录读取）。平台页按会话三层重算范围并覆盖工作身份切换，本院区页组织、医院、院区随会话更新；写前复核按三层比对，阻断时零写。验证：lint 0、test 863 通过 / 2 跳过（30 个测试文件）、build 0 错误；C11 宿主面待子应用登记，C12 组件面见 C34 用例。
- [x] 票 07 后端回改（2026-10-23，审查阻断项 B-01/B-02/B-04 收口）：互认匹配、处理结果与引用三个入口改由 `IamReportScopeResolver` 取当前工作身份三层范围、操作人取 `ICurrentIamContext.UserId`；匹配记录、处理结果与引用的编码字段一律写 IAM 组织业务编码（与请求提交编码同一取值，归属比较语义即为按业务编码比对）。`Stage5WritePathTests` 与 `Stage4SubmissionPathTests` 的可信范围常量与操作人常量改为 `FakeIamScope` 取值，并移除全部旧可信上下文安装点，使来源回退即整片失败。验证：两批用例 12 + 114 通过 / 0 失败；全量 937 通过 / 0 失败；构建 0 错误。
- [ ] 票 07 读路径（B-03/B-06）与票 08 判据 4：第 1 步（查询侧范围来源统一）已完成，全量 935 通过 / 0 失败；原第 2 步「`Query.xml` 补实例标识输出列与读模型消费」与第 3 步「三个导出 Request 实例 Id」属旧口径，已随口径变更退出。按新口径仍待做的是读路径与导出改为按 IAM 组织业务编码过滤，证据待重新取证；端口与仓储签名保持编码文本。
- [x] 用例退出（2026-10-23）：`Export_rejects_when_the_token_and_the_user_profile_disagree` 与 `Export_fills_missing_token_layers_from_the_user_profile` 随后端设计第 2 节删除「令牌缺层用用户档案补齐」「令牌与档案冲突即拒绝」两项语义而退出，用例总数 937→935，不是覆盖收窄；其余迁移为等价替代，未放宽断言。
- [~] 票 10 后端（2026-10-23）：新增 `mrec_platform_parameter` 建表脚本与可重复迁移（只产出、未执行）；新增 `PlatformParameter` 实体、`PlatformParameterDefinition` 共用取值规则、`IPlatformParameterRepository`、`PlatformParameterManager`、`PlatformParameterReader` 与 `PlatformParameterMaintenanceAppService`；三个业务读取点改走本应用参数表；手写控制器暴露 `GET api/v1/platform-parameters` 与 `PUT api/v1/platform-parameters/{parameterCode}`。T02PlatformParameterTests 18 通过 / 0 失败，Tests 工程与 Application 工程构建 0 错误；真实数据库与真实 WorkUnit 未取证，V11～V13 记“待迁移执行后补”。技术债（票 11）：共享测试替身 `StubSystemParameterAppService` 改名或删除；`appsettings.Development.json` 的 Base 系统参数代理配置项删除；`Dy.Base.Application.Contracts` 包待组织与用户读取退出 Base 后删除。全量（API Client 快照重生成后）935 通过、0 失败，构建 0 错误。
- [x] 票 10 前端面（2026-10-23）：
  - 菜单登记（IAM HTTP API）：`/Api/Application/CreateMenu` 新增「平台参数维护」，applicationId `3a2479da-02e6-3ee2-6863-40b05cbeaaff`、route `medical-recognition/platform-parameters`、parentMenuId null、type 0、target 0、icon 空串、supportedClients `["desktop"]`、isEnabled true → menuId `3a247ed9-8449-31c0-e7ed-efed78636476`；`QueryMenuByApplicationId` 读回 11 项含该菜单；`/Api/Role/CreateRoleMenu` 以 tenantId `9ae3125b-6d1d-4689-a4fc-2e21adf75acd`、roleId `2525c0f7-d97c-477e-af31-4b95a53a8044` 绑定开发角色 → id `5f7a74fb-eab8-4fb3-8731-72267ec43ee7`；`QueryRoleMenuByRoleId` 读回 11 条，新菜单 `isAvailable: true`。
  - 页面与路由：新增 `client/apps/dy-medical-recognition/src/pages/platformParameters/`（页面、适配层与 17 项测试）与路由 `platform-parameters`；列表展示三项参数的编码、名称、当前值、说明、状态、操作人与时间；编辑按三项规则提交前阻断，保存成功后重新查询列表，失败保留输入。
  - API Client：快照由 62 条重生成为 64 条，新增 `GET /api/v1/platform-parameters` 与 `PUT /api/v1/platform-parameters/{parameterCode}`；`generate`、`typecheck`、`build` → 0 错误。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`test` → 869 通过 / 2 跳过（31 个测试文件）；`build` → 0 错误。
  - 未完成：C9 宿主入口走查待 IAM 基座登记子应用；C10 属统计与匹配页面的配置错误提示，随票 07～09 取证。
- [x] 票 09 前端面（2026-10-23）：
  - 统计四页继续使用票 07 的宿主会话可信范围；筛选、汇总、明细与导出沿用既有 `organizationCode`、`hospitalCode`、`branchCode` 字段名，值取本项目范围查询下发的 IAM 组织业务编码；会话组织实例 Id 只用于组织目录读取，不落业务字段。
  - 承接范围名称改由平台页选择器选项与本院页 `useCurrentOrganizationScope` 提供；删除 `src/components/scopeNameLookup.ts` 及其测试文件，统计页的 `@dy/auth`、`@dy/auth-react`、`LoginUserManager` 替身与过期注释清零。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`test` → 863 通过 / 2 跳过（30 个测试文件）；`build` → 0 错误；`scopeNameLookup` 引用 0 处。
  - 未完成：C14、C15 宿主走查待 IAM 基座登记子应用；真实导出文件与真实库面属后端/迁移执行后补。
- [~] 票 09 后端统计面（2026-10-23；旧口径把实例标识文本写入编码列，口径已变更，证据待重新取证）：七条数据语句原补接收侧/来源侧实例标识输出列属旧口径，已退出，按新口径只输出组织、医院、院区编码列，七条计数与原因聚合语句逐条确认不加并记录原因；统计汇总、来源汇总、四类使用明细与来源明细读模型的组织维度字段为编码文本，应用层映射实际填充；请求按 code 字段名过滤，编码列存 IAM 组织业务编码，where 按编码列等值；原按实例标识文本写入与 where 改 uuid 的方案已退出。用例：Stage6StatisticsQueryTests 107/0、Stage6StatisticsContractTests 18/0、Stage6SqlMapTests 15/0、T02StatisticsInstanceIdTests 2/0；构建 0 错误。全量 943 总数、942 通过、1 失败（Stage5ConstraintTests.Api_client_package_keeps_the_human_maintained_surfaces，属票 11/前端重生成，不属本票）。真实数据库分组与导出前端待补。
- [x] 票 11 前端面（2026-10-23）：删除 main.tsx 的 AuthProvider、configureBaseComponents 与 runtimeConfig.baseApiBaseUrl；四个测试文件的 @dy/components-base/@dy/api-client-base 替身改为页面本地数据源；package.json 移除 @dy/auth、@dy/auth-react、@dy/components-base、@dy/api-client-base，pnpm install 更新 lockfile；src 下四项引用为 0。API Client 包保留 `createMedicalRecognitionApiClient` 人工维护锚点并改为应用令牌适配器（`getApplicationToken` 提供 Bearer，未签发抛 `ApplicationTokenUnavailableError`），删除包内 @dy/auth；@dy/kiota-middleware 与 @dy/micro-sdk@0.1.39 从依赖树退出，install 无 peer 警告。验证：包 build/typecheck 0；后端 Stage5ConstraintTests 11/0；app lint 0、test 867 通过 / 2 跳过（32 个测试文件；新增应用令牌适配器 2 项与会话订阅稳定性 1 项）、build 0、git diff --check 0；规范修正：范围选择器与当前范围 hook 的 client.api 调用移入共享适配层 organizationScopeApi.ts，平台参数保存改为返回布尔结果、不本地抛错；C16 宿主面待子应用登记后走查。
- [~] 票 11 后端 B1b/B2（2026-10-23）：该批曾把 `OrganizationPathTarget`/`OrganizationPath` 与 `MedicalReportListFilter` 切到实例标识口径，并让报告列表按 `organization_id/hospital_id/branch_id` 等值取页，属旧口径并已退出；组织、医院、院区字段保持编码文本，过滤按编码列等值。原验证结论（全量 943 通过 / 0 失败、Stage3OrganizationPathTests 21/0、构建 0 错误）基于旧口径，证据待重新取证。
- [ ] 票 05 剩余宿主面、票 11 剩余收缩（配合删除迁移期 `*_id` 列与批 D 的 Base 依赖退出）、12 与票 10 真实库补证。

## 待办与交接（2026-10-26 证据）

### 一、当前进度

全部十二份迁移工单已登记完成。宿主入口已可用，两套工作身份各 11 个菜单页已走查；10 个迁移期实例列已删除并只读核对，Base 依赖已退出；SRS 平台参数维护与 UML 的 IAM 来源说明已同步。交付结论为 `ReadyWithAcceptedResiduals`，依据见 [测试报告](testReport.md)「交付结论」。

### 二、后续核查范围

后续核查依据 [测试报告](testReport.md)「交付结论」及 [收口处理计划](收口处理计划.md) 的残余边界开展。后端认证工单未另做独立复审；异常拦截的实际提示尚未端到端触发，真实库统计分组与导出文件仅有抽样证据。

技术治理批次（2026-10-27）：[收口处理计划](收口处理计划.md) 轮次 C 的六项已按结果表处理（会话状态双源、组织路径命名参数、生产注释计划编号、`OrganizationPath.cs` 注释四项已处理；取值清洗函数按不同业务用途保留；Kiota 版本漂移未处理），复跑基线更新为后端 1005、前端 895。

### 三、环境与命令

- 后端：在 `server` 执行 `dotnet build Dy.MedicalRecognition.slnx --no-restore` 与 `dotnet test Dy.MedicalRecognition.slnx --no-build`。最近记录为 970 通过 / 0 失败，见测试报告「全局 API 异常拦截接入与工具链对齐」（2026-10-26）。构建要求 PATH 含 `git`（GitInfo 目标）。
- 前端：在 `client` 分别执行 `pnpm -F dy-medical-recognition lint`、`pnpm -F dy-medical-recognition test`、`pnpm -F dy-medical-recognition build`。同节最近记录为 lint 0、32 文件 874 通过 / 0 跳过、build 0 错误。
- API Client 工具链与生成命令按 Project Context「3.3」执行；本地 Kiota CLI 固定 1.32.2，冻结路径清单为 64 条。
- 取带工作身份的令牌：`python <skills>/dy-dev-iam-management/scripts/iam-api.py --project <root> --requests <file> --account test --work-identity-id <id>`，请求体 `{"applicationCode":"medical-recognition"}`；令牌只在内存，不写文件不打印。
- 本机 IAM 令牌响应不含 `refresh_token`，切换身份后刷新会失败；带身份令牌按「重新登录 + /auth/work-identities 选择 + 再次授权码登录」在内存取得。

### 四、关键标识

- 应用 `medical-recognition`（`3a2479da-02e6-3ee2-6863-40b05cbeaaff`）；开发租户 `9ae3125b-6d1d-4689-a4fc-2e21adf75acd`；开发角色 `medical-recognition-dev`（`2525c0f7-d97c-477e-af31-4b95a53a8044`）。
- 账号 `191617`（`568f0fb4-3b7b-4f55-afb3-d069943ea1a8`）工作身份：测试院区1-1 `0efdeaf9-a5e9-4820-ab3b-e5b7b84858da`、测试院区2-1 `99955c3b-2df7-404b-86b9-c1c83462eb0d`、普通开发 `70590a5f-f0ef-4323-b574-f371310d37bf`、院区1 `834cd08b-…`、院区2 `bac5461c-…`（后两者所在链未配置组织类型，作缺失类别负例）；旧测试院区工作身份 `c762b817-19ec-4b87-a37c-5626cbb56d7c`、`4a79e80f-bc11-4797-a5f0-841e2de71914` 已被 IAM 数据回滚删除，不可用。
- 账号 `mrecAdmin`（`1153a4e9-aba7-436e-9bb4-dc17dfa09c28`，显示名「互认平台管理员」）工作身份：测试院区1-1 `8826eb0d-8cd3-4404-87c2-239b15f76d10`、测试院区2-1 `94bc58ba-6821-4d5c-96a0-eaac5097100e`；登录口令只存放于 Git 忽略的 `.local/dev-credentials.json`。
- 组织类型编码：医共体 `CountyMedicalCommunity`、医院 `Hospital`、院区 `Branch`（已写入环境规范）。

### 五、工作区状态

迁移实现、验证与收口记录已进入仓库历史。续接时执行 `git status --short --branch` 确认当时的工作区状态，保护已有修改。`.local/` 与 `Python/` 已加入忽略；凭据仅保存在被忽略的 `.local/dev-credentials.json`。

### 六、残余与外部依赖

- 双医院走查已使用 `191617` 的两个院区工作身份，账号与身份信息见上文。
- 验证缺口、IAM 重复业务编码、历史业务行不回填及保留的技术债见 [测试报告](testReport.md)「交付结论」；未列为新的前置阻塞。
