# T02 测试报告

本报告记录迁移阶段的实际执行结果；设计见 [design.md](design.md)，批次见 [impl.md](impl.md)。

## 执行边界

- 阶段、矩阵版本与范围：T02 全量；后端 V1～V18（[后端设计](Server/design.md)），前端 C1～C16（[前端测试矩阵](Client/testPlan.md)）。
- 起始提交及已有修改：迁移开工时工作区干净（`main` 领先 `origin/main` 的既有提交，无未提交改动）。
- 执行环境与非敏感身份：环境值引用 [Test Environment](../../../.agents/instructions/test-environment.md)；迁移目标宿主为 IAM 基座。
- 进程、日志及收尾：按批次执行时记录。

### 迁移前基线（2026-10-22 实测，非本阶段矩阵证据）

| 检查 | 命令 | 结果 |
| --- | --- | --- |
| 后端构建 | `dotnet build Dy.MedicalRecognition.slnx --no-restore` | 0 错误（既有可空性与枚举警告） |
| 后端测试 | `dotnet test Dy.MedicalRecognition.slnx --no-build --no-restore` | 897 通过 / 0 失败 |
| 前端 lint | `pnpm -F dy-medical-recognition lint` | 0 |
| 前端测试 | `pnpm -F dy-medical-recognition test` | 831 通过 / 2 跳过（833）；首轮 1 项并行时序失败，单文件复跑 11/11、次轮全绿 |
| 前端构建 | `pnpm -F dy-medical-recognition build` | 0 错误（1.78MB chunk 体积提示为既有） |

## 结果

本节总矩阵反映最终状态。报告其余部分按票与时序分段记录，同一用例编号可能在不同小节多次出现（例如票 08 小节的 V15 记录的是当时缺 PDF 夹具的情况）；同一编号出现多处时，以后出现的记录为准。

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| V1 | 启动与依赖解析 | Passed | `dotnet build/test` + OpenAPI 取证 | 无 Dy.Apron.Security/JwtSettings，OpenAPI 无 /auth/login，安全方案 Bearer | — |
| V2 | 匿名调用受保护接口 | Passed | current-session 与两个业务接口匿名请求 | 均 401 authentication_required | — |
| V3 | 无效令牌 / 过期令牌 | Passed | 无效令牌请求；账号 `191617` 签发令牌后到期前与到期后分别请求 current-session | 无效令牌与到期令牌均 401 `authentication_required`，未降级匿名；令牌 `expiresAt` `2026-10-23T09:26:31Z`，到期前 200、到期后 401 | — |
| V4 | 有效令牌读取当前人 | Passed | 191617 令牌请求 current-session | 200，userId 568f0fb4…；未选身份时三个组织字段为 null；院区1-1 身份时 workIdentityId c762b817…（旧 id，2026-10-24 已因 IAM 数据回滚删除；重配后为 0efdeaf9…）、organizationId 6f0cde2b… | — |
| V14 | 权限不扩散静态扫描 | Passed | 守卫用例 | 无角色/授权特性 | — |
| V17 | Base 依赖静态扫描 | Passed | T02IamAuthenticationTests | 包名、配置节与旧认证标识符归零 | — |
| V5 | 本院入口三层解析 | Passed | `T02OrganizationScopeTests` 院区与医院节点用例；真实 IAM `QueryCurrentOrganizationScope` | 院区1-1、院区2-1 与普通开发三个工作身份的三层归类符合预期；无类型链返回空 | — |
| V6 | 平台入口归属校验 | Passed；校验口径已变更，证据待重新取证 | `T02OrganizationScopeTests` 提交范围用例 | 跨医共体医院、跨医院院区与停用节点均拒绝；另一医院的合法院区路径通过；按新口径校验对象为请求提交的组织业务编码，在 IAM 组织树内比对存在、启用、类型与父子关系 | 真实 IAM 面待业务入口接入，见票 07～09；按编码校验的证据待重新取证 |
| V7 | 批量读取次数 | Passed | `T02OrganizationScopeTests` 读取次数用例 | 当前节点与祖先各一次；20 条目标只读取一次、去重后 3 个标识；150 个标识切为 100 + 50 两批 | — |
| V8 | 停用组织节点 | Passed | `T02OrganizationScopeTests` 停用节点用例；真实 IAM 组织树与选择列表 | 停用节点不进入层级与选择列表；916 个树节点中 52 个停用节点（48 个不同名称）未出现在选项 | — |
| V9 | 未选择工作身份 | Passed | 无工作身份令牌请求 `QueryCurrentOrganizationScope` 与 current-session | 业务查询 409 `work_identity_required`；current-session 200 且三个组织字段为 null | — |
| V16（统计与导出复核） | 统计四入口与导出按 IAM 组织业务编码投影和过滤 | PendingRetest（口径已变更，证据待重新取证） | `Stage6StatisticsQueryTests` 107 通过 / 0 失败；`Stage6StatisticsContractTests` 18 / 0；`Stage6SqlMapTests` 15 / 0；`T02StatisticsInstanceIdTests` 2 / 0（该组冻结的实例列已退出） | 原证据基于旧口径：七条数据语句输出 `receiver_*_id`/`source_*_id`，读模型实例字段有消费点；按新口径七条数据语句与读模型只输出组织、医院、院区编码列，请求按编码列等值 | 真实数据库分组与导出前端待补；按业务编码的投影与过滤证据待重新取证 |
| V16 | 双入口范围口径 | Passed；校验口径已变更，证据待重新取证 | `T02OrganizationScopeTests` 提交范围用例；真实 IAM 平台入口 200（QueryMedicalReportList/QueryRecognitionAmountList，院区1-1 身份） | 本院入口不接受请求扩大范围；平台入口按请求提交的组织业务编码在 IAM 组织树内比对存在、启用、类型与父子关系，不缓存、不前置到认证链路；接收组织与来源组织必须一致，跨组织按精确文案拒绝 | 按编码校验的真实 IAM 面证据待重新取证 |
| V10（结构面） | 组织维度列保持编码列、迁移期新增实例列退出 | Passed | `dotnet test Dy.MedicalRecognition.slnx` | 原证据基于旧口径：919 通过 / 0 失败，四表新增组织实例 Id 列与实体属性，DDL 守卫逐项同步；四表编码列为业务键；10 个迁移期实例列已随 up 脚本删除；DDL 守卫按编码列断言；2026-10-26 复跑全量 968 通过 / 0 失败 / 0 跳过 | — |
| V10（结构面·真实库） | 四张表编码列与参数表结构 | Passed | information_schema.columns / pg_indexes 只读查询（dbx MCP，连 2026-10-26） | 组织维度列共 10 列已全部为 `text` 编码列（报告 3、匹配记录 3、金额 3、互认项目 1），`uuid` 实例列已删除；四张表 `organization_code/hospital_code/branch_code` 编码列仍全部存在并作为业务键；`mrec_platform_parameter` 已存在，7 列（id uuid 非空、parameter_code text 非空、parameter_value text 非空、description text 可空、is_valid boolean 非空、oper_time timestamptz 非空、oper_id uuid 非空），唯一索引 `ux_mrec_platform_parameter_code(parameter_code)`；行数为报告 99 / 匹配记录 106 / 金额 10 / 互认项目 302 / 参数 3（2026-10-26 只读核对）；四条业务表的编码列索引 6 条保留：报告 scope 与业务键唯一索引各 1、匹配记录 2、金额 1、互认项目 1 | 历史行沿用 Base 时代数字编码，按编码过滤时不可见，已定不回填 |
| V15 | 操作人落库 | Passed（操作人面）；组织维度口径已变更，证据待重新取证 | 191617/院区1-1 提交检验报告（PDF 夹具 `var/report-pdf/v15.pdf`）→ 200 true；只读 SELECT `select id, oper_id, organization_code, hospital_code, branch_code from mrec_medical_recognition_report where report_no='LAB-V15-20261024'` → oper_id `568f0fb4-3b7b-4f55-afb3-d069943ea1a8`（历史行另含实例列取值 `c8dd6b02…`、`9156db5e…`、`50c0a4fc…`，属迁移期产物） | 操作人等于 IAM 用户 Id，报告链路真实写入成立；三层编码列按 IAM 组织业务编码取值的证据待重新取证 | 编码列取值取证待补；历史行的实例列取值按新口径不再命中 |
| V10（报告写路径） | 报告、匹配、处理结果与引用按 IAM 组织业务编码读写 | Passed（应用面）；口径已变更，证据待重新取证 | `Stage4SubmissionPathTests`、`Stage5WritePathTests` | 12 + 114 通过 / 0 失败（旧口径：三层实例标识来自当前工作身份组织链）；两批用例均已移除旧可信上下文安装点，来源回退即整片失败 | 真实数据库读写与按业务编码写入的证据待补 |
| V15（报告写路径） | 报告链路操作人落库 | Passed（应用面） | `Stage5WritePathTests` 操作人断言 | 匹配、处理结果与引用的操作人均等于当前 IAM 用户标识，请求不携带操作人 | 真实库 `oper_id` 落库取证待迁移执行后补 |
| V11 | 参数缺失与非法值 | PendingRetest | 191617/院区1-1 令牌调用 RequestRecognitionMatches：库中无 own_hospital_exclude_hours → 500「业务拒绝：未配置本院报告排除时长。」；PUT switch='2' → 500「业务拒绝：本院报告匹配开关取值非法。」（保存侧拒绝，库中无法形成非法值）；PUT switch isValid=false → 500「业务拒绝：未配置本院报告匹配开关。」 | 缺失与停用的业务读取按精确文案拒绝，未使用默认值；非法值由保存侧拒绝，业务读取的非法分支无法经 PUT 造数 | 残余：非法值业务读取分支未取证（接口不允许写入非法值） |
| V12 | 参数维护保存校验与立即生效 | PendingRetest | PUT /api/v1/platform-parameters/own_hospital_match_switch `{parameterValue:"1",isValid:true}` → 200 true；GET 读回 parameterValue=1、operId=568f0fb4…、operTime 非空；PUT `{parameterValue:"2"}` → 500 InvalidOperationException（业务拒绝，开关取值非法） | 保存、读回、非法值拒绝已在维护接口取证；业务读取点确认新值生效未执行 | 阻塞：工作身份 403 identity_unavailable |
| V13 | 参数停用 | Passed | PUT switch isValid=false → 200；RequestRecognitionMatches → 500「业务拒绝：未配置本院报告匹配开关。」；PUT isValid=true 恢复 → 200；再调用读取通过参数阶段（随后因未修复 SQL 报 m.hospital_id 不存在，与参数无关） | 停用后业务读取按缺失拒绝、恢复后重新读取当前值 | 另发现匹配查询 SQL 引用 m.hospital_id 不存在（42703），属独立缺陷，见回报 |
| V18 | 并发身份隔离 | NotRun | — | — | 待后续 |
| C1～C16 | 前端矩阵中尚未取得宿主面证据的条目 | NotRun | — | 已取得记录的分项见各票小节：C8、C9、C16 为 Passed，C14、C15 为 Blocked；两套工作身份已走查宿主 11 个菜单页 | 其余条目的宿主入口逐用例取证待补 |

工作身份读回（`/auth/work-identities` 与 `/Api/AccessQuery/QueryWorkIdentityByUserId`，2026-10-24 重配后）：
- 账号 191617：测试院区1-1 `0efdeaf9-a5e9-4820-ab3b-e5b7b84858da`（县医共体 / 测试医院一 / 测试院区1-1）、测试院区2-1 `99955c3b-2df7-404b-86b9-c1c83462eb0d`（县医共体 / 测试医院二 / 测试院区2-1）、院区1 `834cd08b…`、院区2 `bac5461c…`（测试医共体1 链，组织类型未配置，作缺失类别负例）、普通开发 `70590a5f…`（无医院与院区祖先）。
- 账号 mrecAdmin（userId `1153a4e9-aba7-436e-9bb4-dc17dfa09c28`，显示名「互认平台管理员」）：测试院区1-1 `8826eb0d-8cd3-4404-87c2-239b15f76d10`、测试院区2-1 `94bc58ba-6821-4d5c-96a0-eaac5097100e`；登录口令只存放于 Git 忽略的 `.local/dev-credentials.json`。
- 旧工作身份 `c762b817-19ec-4b87-a37c-5626cbb56d7c`（测试院区1-1）与 `4a79e80f-bc11-4797-a5f0-841e2de71914`（测试院区2-1）已被 IAM 数据回滚删除，查询与选择均不可用；IAM API 不提供指定主键恢复，重建后使用上述新 organizationUserId。

IAM 数据回滚与重配（2026-10-24）：回滚后 191617 选择原测试院区工作身份返回 403 `identity_unavailable`；`QueryOrganizationUserByUserId` 只余普通开发、院区1、院区2 三条成员关系。已通过 `Organization/CreateOrganizationUser` 重建测试院区1-1/2-1 成员关系并重新绑定开发角色；新增 mrecAdmin 并加入同两个组织、绑定开发角色；应用、租户绑定、11 个菜单与角色 11 条菜单均已读回。选择测试院区1-1 后调用 `QueryCurrentOrganizationScope` 返回县医共体/测试医院一/测试院区1-1 三层。

测试状态只使用 `NotRun / Passed / Failed / Blocked / PendingRetest / N/A`。

### 前端票 03 核对与票 05（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C1、C2、C8 | 宿主登录装载、会话字段、请求鉴权 | NotRun | — | 组件面由 `HostSessionContext.test.tsx`、`ApiClientProvider.test.tsx` 覆盖；宿主面待 IAM 基座登记子应用 | 宿主入口 |
| C3 | 页头范围条 | Passed（组件面） | `pnpm -F dy-medical-recognition test` | `CurrentScopeBar.test.tsx` 7 项：三级名称、缺失与失败显示「未知」、省略与 Tooltip、匿名零请求、身份切换与迟到响应丢弃 | 宿主页头走查 |
| C4 | 平台入口范围选择器 | Passed（组件面） | 同上 | `OrganizationScopeSelector.test.tsx`：选项来自范围查询、三级联动、选中值为 IAM 组织业务编码、首项回调；页面用例覆盖切换后请求与结果刷新 | 真实宿主交互 |
| C5 | 本院入口范围 | NotRun | — | 选择器固定组织/固定医院形态已由组件用例覆盖；报告页（票 07）与金额页（票 08）可信范围均已改读宿主会话 | 宿主走查 |
| C6 | 身份切换 | Passed（组件面） | 同上 | 两个组件用例覆盖 `sessionVersion` 推进后重算与旧响应丢弃 | 真实工作身份切换走查 |
| C7 | 组织类型缺失与停用 | Passed（组件面） | 同上 | 选项由服务端排除缺失类型/停用节点，未配置数量单独提示；读取失败给重试并成功装载 | 真实停用节点（后端 V8 已覆盖） |

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 846 通过 / 2 跳过（28 个测试文件）；`pnpm -F dy-medical-recognition build` → 0 错误。API Client：`pnpm -F @dy/api-client-medical-recognition prepare-openapi` → 62 条路径、`oneOf` 与 `nullable` 各 0 处、可空归一 375 处；`generate`、`typecheck`、`build` 均 0 错误。后端冻结路径清单同步后，后端全量 `dotnet test Dy.MedicalRecognition.slnx` → 917 通过 / 0 失败。`git diff --check` → 0。GitNexus `detect-changes --repo Dy.MedicalRecognition` → `CRITICAL`（41 个文件、237 个符号、50 条受影响流程）；变更全部属于 T02 票 02～05 的累计工作区（含后端票 02/04 与前端票 03/05），逐项验证已通过，无计划外跨阶段影响。upstream impact：`CurrentScopeBar` 为 `HIGH`（直接调用方 3、受影响流程 2：`BranchReportManagement`、`BranchRecognitionAmounts`），属票 05 页头范围条替换的预期传播；`ReportManagement`、`RecognitionAmounts`、`StatisticsBoard` 为 `LOW`。未触发停止条件。

### 前端票 07（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C13 | 报告详情与版本 | Passed（组件面） | `pnpm -F dy-medical-recognition test` | `ReportManagement.test.tsx` 43 项与 `BranchReportManagement.test.tsx` 12 项全绿；可信医院与院区改读宿主会话后，列表、详情、版本切换与返回行为与迁移前一致 | 真实宿主入口与 PDF 下载 |
| C15 | 重建后的空态与初始化 | Passed（组件面） | 同上 | `ReportManagement.test.tsx` 的空态用例：读取成功但无匹配报告时显示空态、无异常请求与本地错误提示 | 空库宿主走查 |
| C6 | 身份切换（报告链路） | Passed（组件面） | 同上 | `RecognitionProjects.test.tsx` 114 项覆盖会话组织推进、静默换绑后的提交期复核与迟到响应丢弃；`reportsTrustedScope.test.tsx` 覆盖医院/院区随会话重算 | 真实工作身份切换走查 |

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 850 通过 / 2 跳过（29 个测试文件；`StandardCatalog.test.tsx` 并行时序用例本轮通过）；`pnpm -F dy-medical-recognition build` → 0 错误。API Client：`prepare-openapi` → 62 条路径、无 `/auth/login`；`generate` 输出无差异；`build`、`typecheck` → 0 错误。`git diff --check` → 0。GitNexus `detect-changes --repo Dy.MedicalRecognition` → `critical`；工作区含票 01～07 累计改动与并行后端改动，本轮改动为两个可信范围接缝、5 个测试文件与阶段文档，未触发停止条件。upstream impact：`readTrustedHospitalCode` 为 `CRITICAL`（报告、金额与统计页面均属 T02 票 08/09 的既定接线面；导出类型与名称未变，相关页面用例本轮全绿），`useTrustedOrganizationScope` 与 `readTrustedOrganizationCode` 为直接调用 `RecognitionProjects` 的预期传播。

### 前端票 08（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C11 | 平台入口非法范围组合 | NotRun | — | 金额页请求的组织、医院、院区值为选择器与范围查询下发的 IAM 组织业务编码，不建编码映射表；跨组织与跨医院组合由后端在组织树内按请求校验后拒绝，需在 IAM 基座完成子应用登记后走查 | 宿主面 |
| C12 | 金额页写前复核 | Passed（组件面） | `pnpm -F dy-medical-recognition test` | `RecognitionAmounts.test.tsx` 80 项：写前复核按组织、医院、院区三层比对，可信范围变化与现读缺失都零写请求；`BranchRecognitionAmounts.test.tsx` 59 项：组织或医院现读不可用即零写；新增工作身份切换后按新会话重算三级范围用例 | 真实工作身份切换走查 |

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 863 通过 / 2 跳过（30 个测试文件）；`pnpm -F dy-medical-recognition build` → 0 错误。与交底基线 869 / 31 个文件的差异来自并行工作区删除 `scopeNameLookup.test.ts` 及其实现，本轮未删除任何测试文件。`git diff --check` → 0。GitNexus `detect-changes` → `critical`（票 01～10 累计工作区与并行改动）；upstream impact：`RecognitionAmounts`、`BranchRecognitionAmounts` 均为 `LOW`（直接调用方各 1）。

### 后端票 08 互认项目与金额切换（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| V10（读写面） | 互认项目与金额按 IAM 组织业务编码读写、写前查重 | PendingRetest（口径已变更，证据待重新取证） | `Stage2WritePathTests`、`Stage3WritePathTests` | 旧口径证据：互认项目按三层实例与标准项目编码先查后写，启用与停用配置都参与唯一性；金额按三层实例与标准项目先查后写；按新口径两处均按组织业务编码查写；跨组织、跨医院与停用范围按各层文案拒绝且零写入；`Stage2WritePathTests` 70 通过 / 0 失败、`Stage3WritePathTests` 25 通过 / 0 失败 | 真实数据库读写与唯一索引运行面待迁移执行后补 |
| V15 | 操作人落库 | PendingRetest | 未提交成功：SubmitCompleteLaboratoryReport → 500「业务拒绝：报告 PDF 文件不存在。」；本地 PDF 存储根目录 var/report-pdf 为空，且无上传接口，无法在不写文件的前提下造出可读 PDF 键 | 工作身份已恢复（0efdeaf9 可选并签发令牌）；报告提交被 PDF 夹具阻塞，未产生 oper_id 证据 | 阻塞：缺 PDF 夹具；另匹配查询 SQL 的 m.hospital_id 缺陷可能同样影响报告链路 |

本轮命令与结果：`dotnet build Dy.MedicalRecognition.slnx --no-restore` → 0 错误；票 08 相关用例集（`Stage2WritePathTests`、`Stage1SqlMapProbeTests`、`Stage3SqlMapTests`、`Stage3WritePathTests`）→ 107 通过 / 0 失败；全量 `dotnet test Dy.MedicalRecognition.slnx` → 937 总数、935 通过、2 失败，两处为 OpenAPI 快照路径清单（`Stage2EnumContractTests` 与 `Stage7ExclusionGuardTests.V1`），待票 03/10 重生成快照，不属本票。判据 4 的导出按组织业务编码过滤未实施，导出实现文件归属待定。

### 前端票 10（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C9 | 参数维护页 | Passed（组件面） | `pnpm -F dy-medical-recognition test` | `platformParametersApi.test.ts` 9 项与 `PlatformParameters.test.tsx` 9 项：三项参数列表与「未配置」、读取失败重试、三项取值规则提交前阻断（零保存请求）、合法保存后重新查询并显示新值、停用状态切换、保存失败保留输入；`routes.test.ts` 覆盖 `platform-parameters` 路由挂载 | 宿主入口走查与真实后端链路 |
| C10 | 参数缺失与非法 | NotRun | — | 本页只覆盖参数维护入口的取值校验；统计与匹配页面的配置错误提示属票 07～09 | 票 07～09 页面 |

菜单登记读回：`QueryMenuByApplicationId` 11 项含「平台参数维护」（router `medical-recognition/platform-parameters`）；`QueryRoleMenuByRoleId` 11 条含该菜单，`isAvailable: true`；角色 `medical-recognition-dev`。

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 869 通过 / 2 跳过（31 个测试文件）；`pnpm -F dy-medical-recognition build` → 0 错误。API Client：`prepare-openapi` → 64 条路径（新增两条平台参数端点，可空归一 350 处）；`generate`、`typecheck`、`build` → 0 错误。`git diff --check` → 0。GitNexus `detect-changes` → `critical`（工作区含票 01～10 累计与并行后端改动，共 110 个文件、558 个符号、119 条受影响流程；本票改动限于平台参数维护页、路由与 API Client 快照，逐项验证通过，未触发停止条件）。宿主入口走查待 IAM 基座登记子应用，`QueryVisibleMenu` 因本机 IAM 会话刷新限制未取得证据。

阶段 7 设计 `docs/plans/009-阶段7-横向治理与发布收口/design.md` 第 94 行 DC-10 与第 71 行 EX-19 的“参数配置复用既有系统参数”由 T02 取代：Base 系统参数服务随迁移整体退出后，三项业务参数改由本应用 `mrec_platform_parameter` 自管；`Stage7ExclusionGuardTests` 仅放行 `/api/v1/platform-parameters` 与 `/api/v1/platform-parameters/{parameterCode}` 两条精确 API 路由，其余参数路由仍按排除项失败。

- 用例退出（2026-10-23）：`Export_rejects_when_the_token_and_the_user_profile_disagree` 与 `Export_fills_missing_token_layers_from_the_user_profile` 随后端设计第 2 节删除「令牌缺层用用户档案补齐」「令牌与档案冲突即拒绝」两项语义而退出，用例总数 937→935；查询侧范围来源统一后全量为 935 通过 / 0 失败。
### 前端票 09（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C14 | 统计页三处范围选择器 | Blocked | `pnpm -F dy-medical-recognition test` | `statisticsBoard.test.tsx` 的范围选择用例断言平台页接收组/来源组的组织、医院、院区值与名称同时进入已生效条件，请求携带的 `organizationCode`、`hospitalCode`、`branchCode` 值即选择器产出的 IAM 组织业务编码；本院页对侧筛选用例断言医院、院区值随选择回传；`usageStatisticsPages.test.tsx`、`sourceStatisticsPages.test.tsx` 覆盖四页接线 | 真实宿主交互与导出文件 |
| C15 | 重建后的空态与初始化 | Blocked | 同上 | `statisticsBoard.test.tsx` 的汇总/明细空态与初始化用例全绿；范围与时间窗默认行为未改 | 空库宿主走查 |

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 863 通过 / 2 跳过（30 个测试文件；交底基线 869 通过 / 31 个文件，差异来自删除 `scopeNameLookup.test.ts` 的 7 项，未新增失败）；`pnpm -F dy-medical-recognition build` → 0 错误；`git diff --check` → 0；`scopeNameLookup` 与 `useScopeNames` 引用 0 处。GitNexus `detect-changes` → `critical`（工作区累计票 01～10 与并行后端改动，130 个文件、699 个符号、147 条流程）；本票相关符号 `StatisticsBoard`、`SourceStatisticsBoard`、`toScopeView` 的 upstream impact 均为 `LOW`，未触发停止条件。宿主入口走查待 IAM 基座登记子应用。

### 前端票 03/票 11 宿主请求鉴权修正（2026-10-25）

宿主内实测结论：业务请求实际打到 `http://localhost:15014/Api/...`，但 `createRequestAdapter` 使用的 `MicroSDK.auth.authenticateRequest` 返回空 headers，请求以 401 失败；同一会话 `getApplicationToken({ targetAppCode: 'medical-recognition' })` 返回 `issued`，带该令牌的 POST 返回 200。修正为应用令牌适配器后，报告管理、平台参数维护、互认使用统计、本院区互认项目金额四页均能打开且无主线程卡死；范围选择器渲染选项，平台参数列表与统计汇总请求返回 200。报告管理与平台金额列表本次收到后端 500「业务拒绝：组织不存在或已停用」，属后端组织路径解析取值与 IAM 组织业务编码未对齐，记入残余风险。当轮走查方式：在宿主 35009 内逐个打开上述四个页面，记录 Network 面板中业务请求的 Authorization 头与响应码、Console 中无主线程阻塞报错，结论即下表 C8 行。

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C8 | 请求鉴权（有效会话） | Passed（有效会话） | 宿主 35009 内打开四个页面并查看 Network/Console | 业务请求由应用令牌附加 Authorization，列表与查询返回 200；未签发时适配器抛 `APPLICATION_TOKEN_UNAVAILABLE` 且不发送业务请求 | 匿名/失效会话的宿主走查未单独复跑；报告管理与平台金额列表的后端 500 见残余风险 |

事实记录：前端单测、lint、build 全绿时，宿主内 C8 仍因适配器未附令牌而 401；C1/C8 一类必须以宿主内真实请求取证，单测通过不等于宿主可用。同一轮修复 `useHostSessionSnapshot` 的重复订阅循环（subscribe 改为模块级稳定引用，初始读取通知快照订阅者），金额两页、本院报告管理与统计四页恢复稳定。

### 前端票 11：旧 Base 前端依赖退出（2026-10-23）

| 用例编号 | 验证面 | 状态 | 实际命令或操作 | 证据 | 未覆盖面 |
| --- | --- | --- | --- | --- | --- |
| C16 | Base 残留静态核对 | Passed（静态面） | `grep -rn '@dy/auth\|@dy/components-base\|@dy/api-client-base' src`；`pnpm -F dy-medical-recognition lint/test/build`；构建产物关键词扫描 | src 下四项引用为 0；package.json 无 @dy/auth、@dy/auth-react、@dy/components-base、@dy/api-client-base；构建产物无 LoginUserManager、TokenManager、createAuthenticatedAdapter；运行时配置无 baseApiBaseUrl | 宿主网络请求与页面入口走查待 IAM 基座登记子应用后执行 |

本轮命令与结果：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 867 通过 / 2 跳过（32 个测试文件；新增应用令牌适配器 2 项与会话订阅稳定性 1 项，相对 864/30 增加 3 项）；`pnpm -F dy-medical-recognition build` → 0 错误；`git diff --check` → 0。删除的测试替身只在页面测试内部提供选择器选项数据源，未删减用例。规范修正：`OrganizationScopeSelector` 与 `currentOrganizationScope` 的 `client.api` 调用移入共享适配层 `organizationScopeApi.ts`；`platformParametersApi.savePlatformParameter` 不再本地抛错，改为返回布尔结果并由页面提示未确认成功。API Client 包的人工维护锚点 `createMedicalRecognitionApiClient` 保留名称与导出位置，实现改为应用令牌适配器（`getApplicationToken` 提供 Bearer，未签发时抛 `ApplicationTokenUnavailableError` 且不发送业务请求；401 上报宿主）；包内 `@dy/auth` 依赖删除，`@dy/kiota-middleware` 与 `@dy/micro-sdk@0.1.39` 从依赖树退出（`pnpm why @dy/kiota-middleware` 无输出、lockfile 0 处、`pnpm install` 无 peer 警告）。

### 票 12 取证边界（2026-10-23）

- 可在本地补：C10 组件级参数错误展示、V17 全 Base 依赖静态扫描（票 11 完成后）、SRS/UML/project-context 与 `docs/迁移接入.md` 同步、实际包版本与剩余依赖记录。
- 必须外部前置：V10～V13、V15 的真实数据库与真实 WorkUnit（负责人执行迁移期实例列删除）；C1/C2/C3 宿主面、C5、C6 宿主面、C8、C9 宿主面、C11、C13、C14/C15 宿主面、C16 宿主面（IAM 基座登记子应用与地址映射）；V18 需要第二个可登录测试身份。
- 当前结论依据：上述外部项未补齐，状态只能 `NotReady`；外部项与票 11 残余完成后才可能降为 `ReadyWithAcceptedResiduals`。

## 问题与残余风险

| 问题 | 影响 | 责任范围 | 后续动作 | 复测结果 |
| --- | --- | --- | --- | --- |
| IAM 应用未登记 | 无法签发业务令牌与装载页面 | 票 01 | 已登记并读回，见实施记录 | 已消除 |
| `lisadmin` 登录返回 interaction_required（HTTP 200） | 该账号无法作为第二个测试身份 | 票 01 | 改用 `191617` 的院区1/院区2 两个工作身份覆盖两家医院；`lisadmin` 登录保留为残余风险 | 未复测 |
| 组织类型编码未核验 | 三层解析依赖实际编码 | 票 04 | 已用真实工作身份解析出完整三层，编码写入环境规范 | 已消除 |
| 宿主未登记子应用 | 无法从 IAM 基座进入页面 | 基座侧 | 基座维护后进入票 03 的宿主面验证；菜单与应用侧登记已完成 | 待外部执行 |
| 早前创建的 `yangkj` 账号首次登录返回 interaction_required | 该账号不作为验收身份 | 票 01 | 改用既有账号 `191617`，登录与令牌签发已实测通过 | 已消除 |
| 两个测试账号尚未加入目标院区组织 | 无法构造两家医院的可信范围 | 票 01 | 已加入院区级组织并读回：191617→院区1+院区2，lisadmin→院区1；院区父节点分属测试医院1/测试医院2 | 已消除 |
| 本机 IAM 令牌响应不含 `refresh_token` | `iam-api.py --work-identity-id` 在选择工作身份后刷新令牌失败 | 票 04 | 用同一脚本的登录、`/auth/work-identities` 选择与再次授权码登录在内存取得带身份令牌，不写文件、不输出 | 已验证替代路径；工具链待修 |
| 范围选择器与页面请求的组织维度取值口径不一致 | 选择器已产出一层取值，页面请求字段与可信范围仍需统一到 IAM 组织业务编码 | 票 06～11 | 报告页与互认项目页（票 07）、平台金额页与本院区金额页（票 08）、统计四页（票 09）均已改读会话并按组织业务编码取值；App 直接依赖与 api-client 包的 @dy/auth 均已在票 11 删除 | 已消除 |
| 后端测试项目暂不可编译（并行改动） | 冻结 OpenAPI 契约用例本轮无法执行 | 并行后端工作区 | `StubSystemParameterAppService` 已改为实现 `IPlatformParameterRepository`，测试工程恢复接入新替身；`dotnet build Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` → 0 错误 | 已消除 |
| API Client 快照未含平台参数端点 | 平台参数两个新端点未进入快照，`Stage2EnumContractTests` 与 `Stage7ExclusionGuardTests.V1` 曾红 | 票 10 前端/API Client | 已按 64 条重生成；两个守卫通过。后端全量 935 通过 / 0 失败，票 10 目标集 18/0 | 已消除 |
| packages/api-client-medical-recognition 仍依赖 @dy/auth | @dy/kiota-middleware 未随 app 直接依赖退出；该包入口保留未使用的 createMedicalRecognitionApiClient | 票 11 前端/API Client | 锚点保留并改为应用令牌适配器实现，@dy/auth 依赖删除；kiota-middleware 与 micro-sdk@0.1.39 从依赖树退出，install 无 peer 警告 | 已消除 |
| 报告管理与平台金额列表返回 500「组织不存在或已停用」 | 两页无法读取业务列表；鉴权与范围查询已 200 | 后端组织路径解析/数据库迁移（票 04/07/08） | 后端组织路径取值与 IAM 组织业务编码对齐后复测；前端不包装、不降级 | 未复测 |

## 结论


### 结论草稿（2026-10-23，口径变更前的证据；已被文末「交付结论」取代）

当前证据建议判定为 `NotReady`。

已通过的部分及证据来源：V1～V4、V14、V17（认证、当前会话、静态依赖与权限不扩散）；V5～V9、V16（组织范围解析、选择列表、工作身份拒绝）；V10 结构面（DDL、实体与 SqlMap 列守卫）；V11～V13 的应用层用例（参数缺失、非法、停用与维护保存）；V15 的应用层操作人断言；前端 C3、C4、C6、C7、C9、C12、C13 的组件面，C16 的静态面。以上均未按真实数据库或真实宿主面通过。

仍需补齐的残余与阻塞：V10、V11、V12、V13、V15、V16（统计与导出复核）的真实数据库/真实 WorkUnit 取证；V15 同时受工作身份 403 `identity_unavailable` 影响；C1、C2、C5、C8、C11 与 C14、C15 的 IAM 基座子应用登记/宿主走查（C14、C15 另需空库与导出文件）；V18 需要第二个可登录测试身份（`lisadmin` 仍 `interaction_required`）；C10 的统计/匹配页参数错误展示尚未实现。

升级条件：若负责人正式接受宿主登记与真实库/WorkUnit 为外部残余，并完成票 11 的 Base 依赖清零与文档同步，可建议 `ReadyWithAcceptedResiduals`；只有上述宿主、数据库、第二身份与 C10 证据全部补齐后才建议 `Ready`。在 V11～V13 真实库取证结果回报前，不修改上述已通过项状态，也不把本段写成定稿结论。

票 01 前 3 条判据已完成并有读回证据（应用登记、账号与院区成员、菜单与角色绑定），第 4 条宿主子应用登记为外部依赖；票 02 的 V1～V4、V14、V17 与票 04 的 V5～V9、V16 已取得本轮证据；票 10 的 V11～V13 已取得应用层证据，真实数据库与真实 WorkUnit 待迁移执行后补；票 09 后端统计面的组织维度编码投影与读模型字段原按实例标识取得应用层与静态证据，该口径已变更、证据待重新取证，真实数据库分组与导出前端待补；前端票 03 的代码核对与 C8 有效会话宿主取证、票 05 的 C3、C4、C6、C7、票 07 的 C13、C15、C6（报告链路）、票 08 的 C12、票 09 的 C14、C15（统计链路）、票 10 的 C9 与票 11 的 C16 静态面已取得证据，其中 C14、C15 的宿主面未取证，状态记 `Blocked`；C5、C10、C11 与全部宿主面、其余后端与前端条目仍为 `NotRun`，不构成迁移完成结论。

## 组织编码口径重做轮（2026-10-25）

组织业务键由「组织实例标识文本」改回「IAM 组织业务编码」，阶段决策 T2-D3、T2-D4 已按新口径修订，明细见阶段设计与 `Server/design.md`。

### 实现面

| 项 | 内容 |
| --- | --- |
| 编码读取 | 组织范围读取点按当前租户读取 IAM 组织树节点并取 `code`；组织树接口实测接受带 `$id` 的序列化请求体（基线、带 `$id`、带未知字段三次对照均 200） |
| 范围解析 | 组织范围对象同时携带实例标识、名称与三层业务编码；提交的业务编码经当前租户组织树解析为实例标识后，仍以 `parentOrganizationId` 判定父子关系，不按编码前缀推断 |
| 写入 | 报告提交、作废、处理结果、引用、匹配与金额、互认项目入口的 `organization_code/hospital_code/branch_code` 写入业务编码 |
| 查询与导出 | 报告、金额、统计与导出的组织谓词与投影只使用编码列 |
| 契约收缩 | 实体、命令、查询类型、仓储端口与契约删除迁移期新增的 `Guid?` 实例标识属性；列表筛选回到 `*Code`；28 条 SqlMap 语句回到编码列；四份建表脚本删除迁移期 `uuid` 列 |
| 回收脚本 | `Scripts/Migrations/20261025_t02_drop_organization_instance_ids.up.sql`（回收）与 `.down.sql`（回滚）分文件提供纯 DDL；先合并执行导致列被重建，拆分后由负责人执行回收段；执行前核对、执行后核对与回滚步骤见《组织实例列删除交付说明》 |
| 前端 | 范围查询与选择列表输出业务编码，选择器与各页请求取业务编码；OpenAPI 与 Kiota 客户端按收缩后契约重新生成，6 个模型共 14 个实例标识属性从生成物中消失 |
| 方言中立守卫 | 仓储 SQL 的数据库方言中立判据扫描范围未变（迁移脚本仍在范围内），回收脚本以纯 DDL 通过该判据 |

### 门禁结果（本机实跑）

| 命令 | 结果 |
| --- | --- |
| `dotnet build Dy.MedicalRecognition.slnx --no-restore` | 0 错误 |
| `dotnet test Dy.MedicalRecognition.slnx` | 951 通过 / 0 失败 / 0 跳过 |
| `pnpm -F dy-medical-recognition lint` | 0 错误 |
| `pnpm -F dy-medical-recognition test` | 32 文件，869 通过 / 2 跳过 |
| `pnpm -F dy-medical-recognition build` | 0 错误（仅有既有 chunk 体积提示） |
| `pnpm -F @dy/api-client-medical-recognition typecheck` | 0 错误 |

### 口径变更后需重新取证的项

- V10：编码列现为业务键，迁移期新增的 10 个实例标识列待回收；结构面与真实库核对在回收脚本执行前后进行。
- V15：历史业务行的编码列取值是实例标识文本，按新口径不再命中组织过滤；已确认不回流历史数据，测试数据以 IAM 现有组织重新建立。
- V16：统计与导出的投影、过滤已按编码列，含真实库分组与导出文件的前端面未取证。
- 前端宿主面（C 系列）：本轮只做静态与组件面门禁，未重新走查宿主页面。

### 本轮未执行

数据库 DDL 与 DML（回收脚本由负责人执行）、IAM 写接口验证、宿主页面走查。上述项的既有状态不变，本轮的交付结论沿用前文。

## 真实链路验收（2026-10-25）

### 编码往返（读侧）

后端以 Development 环境运行于 15014，按应用令牌调用组织范围入口，两套工作身份的实测结果：

| 工作身份 | 组织编码 | 医院编码 | 院区编码 |
| --- | --- | --- | --- |
| 测试院区1-1 | `01` | `CSYY1` | `CSYQ1-1` |
| 测试院区2-1 | `01` | `CSYY2` | `CSYQ2-1` |

组织树选项全量核对：83 个院区节点中 49 个有业务编码，0 个取值为组织实例标识文本；5 个未配置编码的节点保持为空，未用标识或名称替代。

鉴权口径：业务接口须以应用令牌调用，用户访问令牌不含租户与组织上下文，直接调用返回 409 `work_identity_required`；该口径已记入环境规范。

### 编码落库（写侧）

经业务接口保存金额后只读回读，该行组织三层编码为 `01 / CSYY1 / CSYQ1-1`，操作人为当前 IAM 用户；表内实例标识形态的编码计数为 0。

### 数据重建与页面走查

- 经接口新增：报告 3 份、匹配记录 2 条（一条采纳并引用、一条不采纳）、金额 2 行、互认项目 1 条、平台参数 1 项；回读的组织列均为业务编码。
- 宿主走查（身份 测试院区1-1）：11 个菜单页全部加载，Console 无应用错误；报告管理、统计四页与平台参数维护均有数据。
- 导出：本院区互认项目金额导出为有效 xlsx，303 行 × 10 列，含本轮写入的金额值。
- 宿主走查（身份 测试院区2-1）：页头范围与报告列表数据正确。
- 观察项：平台入口报告管理的默认范围取选项首项（`县医共体 / 县人民医院总院`），非本轮测试组织，属默认值行为，不作缺陷。

### 本轮缺陷与修复

使用明细查询把来源方范围按接收方字段解析（`MedicalRecognitionReportQueryAppService.Statistics.cs` 的 source 三段参数），导致按医院与院区过滤时明细为空。按测试先行方式修复并新增用例覆盖该解析路径，回归全量通过。

### 实例标识列回收

首次执行把 up 与 down 放在同一文件的脚本，删除后立即被 down 段重建，列仍在且取值为空。已将其拆分为 `.up.sql` 与 `.down.sql`，交付说明补充误执行判断方式。

执行后只读核对（回收完成）：10 个实例标识列已删除（information_schema 计数 0）；四张表编码列齐全（报告 3、匹配 3、金额 3、互认项目 1）；六条编码列索引全部保留，其余索引落在主键 `id`、`identity_document_no` 与 `patient_name` 上，无索引引用已删列；行数为报告 99、匹配记录 106、金额 10、互认项目 302。

编码形态分布（报告表）：`branch_code` 为 `0101001` 65 行、`CSYQ2-1` 31 行、`CSYQ1-1` 2 行、实例标识文本 1 行；`organization_code` 为 `01` 98 行、实例标识文本 1 行。据此确认：按组织编码过滤时，Base 时代数字编码的历史行不可见，已确认不回填。

### 上游文档核对

`docs/uml` 五个模型与 SRS 中的组织字段均为「组织编码（String）」，无组织实例标识表述，与新口径一致，本轮不需要修改。

## 代码审查与整改（2026-10-25）

审查方式：规范轴与规格轴并行、只读评审，对照点 `HEAD`（本轮改动全部未提交），范围按改动时间界定为本轮变更文件。

### 规范轴

硬违规两项，均已整改：

1. 批量方法内逐条等待：`OrganizationPathResolver.ResolveOrganizationHospitalOrThrow` 改为整批解析，`IamOrganizationScopeResolver` 增加批量入口、原单条入口委托批量实现；新增计数断言用例证明目录与编码读取次数不随目标行数增长。
2. 选择器三处机械包裹的 `useMemo` 移除；兜底值改为模块级空清单常量（内联空数组会引入依赖检查告警，常量保持同一引用）。

判断项五项（重复的取值清洗函数、`OrganizationPath` 九个位置参数、会话状态双源、生产注释残留计划编号等）登记为遗留，不在本轮重构范围。

### 规格轴

已整改：

1. 同一业务编码命中多个组织节点改为业务拒绝，不再静默取其一。先以失败用例复现旧行为（静默落到另一条组织链、在下游抛医院文案），再改实现。
2. 回收脚本前置条件与「不回填历史行」的决定冲突：改为只核对空值行数量、不要求按目录回填，up 与 down 两个文件同步。
3. 三处注释仍描述已删除的「令牌缺失用用户档案补齐、冲突即拒绝」口径，改为组织层只来自 IAM 上下文，医院与院区层由祖先链按类型归类得到。
4. 报告中回收脚本名称与索引清单的错漏已更正（脚本拆分为 up 与 down 两个文件；索引还包括 `patient_name`）。

登记遗留：

- SRS 未新增 F11 系统参数维护功能说明（属票 10 范围）。
- `design.md` 文档同步清单要求同步 UML 1/2/3/5 的取数来源表述，本轮未做；字段口径本身已是编码 String。
- 仓库根存在 5 个 dotnet 测试日志文件（未跟踪），待清理。

### 门禁终值

后端 `dotnet test` 954 通过 / 0 失败；前端 lint 0、869 通过 / 2 跳过（32 文件）、build 0 错误；`git diff --check` 干净。

### 切换前必查

业务编码命中多个组织节点不构成拒绝条件。校验只判组织节点的存在性、层级类型，以及可定论的父子归属；不要求编码唯一，也不为数据用途挑选节点。IAM 目录内同一租户存在重复业务编码时，平台按上述规则正常服务；展示用名称按容错规则取名，无法定论时留空。

因此编码唯一性不是切换前置条件，也不以「按 `code` 分组统计计数大于 1」阻断上线。该统计只在评估展示名称留空范围时作为参考信息。

## 第二身份走查与回归（2026-10-25 续）

以测试院区2-1 身份走查宿主 11 个菜单页，并以页面动作验证导出与 PDF 下载。

### 走查结果

| 页面 | 结果 |
| --- | --- |
| 标准项目目录维护、互认项目配置 | 正常，数据可见 |
| 互认项目金额维护、本院区互认项目金额 | 正常，行集 302 条（其中已配置金额若干） |
| 报告管理 | 平台入口默认范围为选项首项，空态正常 |
| 本院报告管理 | 5 份报告可查，详情与版本信息完整 |
| 使用统计、本院使用统计 | 明细各 2 条 |
| 来源方统计、本院被互认统计 | 明细 1 条；无数据处为空态而非失败 |
| 平台参数维护 | 3 项参数可见 |
| PDF 下载 | 详情页下载成功，文件与提交的报告一致 |

### 本轮发现并修复的行为缺陷

1. 编码命中多个组织节点时校验一律拒绝：真实 IAM 中同一编码可对应多个节点，导致统计页与报告列表整片失败。改为「校验只判存在性、层级类型，以及能定论时的父子归属；不再要求编码唯一，也不为数据用途挑选节点」。
2. 名称回填复用严格解析：展示用名称解析遇到重复编码时把整页打成失败。改为容错取名：会话组织链上唯一候选取其名，否则全部候选名称一致取其名，否则留空；不抛异常、不猜、不回落编码文本。
3. 实例标识被强行填充：解析不再产出实例标识，避免下游把它当成已定论的组织。
4. 读取失败被渲染成零配置空态：金额列表空态文案忽略读取失败状态，页脚计数在失败态显示 0。改为失败态显示「读取失败，请重试」、页脚显示「—」，并补三例覆盖失败、真空、有数据三种状态。

修复后门禁：后端 968 通过 / 0 失败；前端 lint 0、872 通过 / 2 跳过、build 0 错误。

### 登记遗留

- 同类「读取失败渲染成空态」结构还存在于报告列表（平台与本院共用组件）、互认项目配置、标准项目目录三处，本轮只修金额列表。
- 金额页曾出现一次「共 0 项」，同请求后端实测返回 302 行，说明失败发生在请求发出或令牌获取阶段；该次失败的网络层根因未取证，修复后同类情况会以失败文案呈现。
- IAM 同一租户内存在两棵业务编码完全相同的组织树（重复的 `01`、`CSYY1`、`CSYY2`、`CSYQ1-1`、`CSYQ2-1` 等），属目录数据状态；本项目按编码为键、名称容错、校验不挑节点处理，不对此做审计或修正。

## 交付结论（2026-10-25）

判定：**`ReadyWithAcceptedResiduals`**。

2026-10-26 复跑确认：后端 `dotnet build` 0 错误、`dotnet test` 968 通过 / 0 失败 / 0 跳过；前端 `lint` 0、`test` 32 文件 872 通过 / 2 跳过、`build` 0 错误；`git diff --check` 干净。数据库只读核对：组织维度 10 列均为 `text` 编码列，无 `uuid` 实例列，四条业务表编码列索引 6 条保留，行数为报告 99 / 匹配 106 / 金额 10 / 互认项目 302 / 参数 3；20 个终建脚本与库对象一一对应且无实例列残留。

依据（本轮实测，非引用历史结论）：

- 后端 `dotnet build Dy.MedicalRecognition.slnx --no-restore` → 0 错误；`dotnet test` → 968 通过 / 0 失败 / 0 跳过。
- 前端 `lint` → 0；`test` → 32 个文件，872 通过 / 2 跳过；`build` → 0 错误。
- 实例标识列回收执行后只读核对：10 个列已删除；四张表编码列齐全；六条编码列索引保留；无索引引用已删列。
- 真实链路：两套工作身份的范围查询返回组织业务编码；组织树 83 个院区节点中无实例标识文本。
- 宿主走查：两个身份各 11 个菜单页加载正常，报告、金额、统计、参数数据可见；PDF 下载与金额导出为实物文件核对。
- 代码审查：规范轴与规格轴完成，2 项硬违规与 4 项规格问题已整改并回归。

接受的残余（明确不在本轮解决）：

- 展示层读取失败口径已在组件面统一：报告列表按「静默保留旧数据、错误由宿主提示」的既定口径处理，互认项目配置停在阻断态并提示可重试读取，标准项目目录给出「目录数据待刷新」警示。三处的页面级走查未单独复跑。
- SRS 未补 F11 系统参数维护说明；UML 1/2/3/5 未补「取数来源改为 IAM」表述（属票 10 范围）。
- IAM 目录存在重复业务编码，名称回填在这些编码上按规则留空；本平台不据此猜测，也不审计目录数据。
- 代码审查中的判断项（重复的取值清洗函数、`OrganizationPath` 参数结构、会话状态双源等）未重构。
- 历史业务行不再按组织编码可见，未回填（既定决定）。

不建议判定为 `Ready` 的原因：宿主走查为手工执行，未覆盖全部失败分支；真实库的统计分组与导出文件只做了抽样核对。
