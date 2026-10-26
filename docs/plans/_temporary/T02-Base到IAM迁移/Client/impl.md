# T02 前端实施

设计见 [前端设计](design.md)，矩阵见 [testPlan.md](testPlan.md)，票与依赖见 [阶段实施](../impl.md)。

## 票映射

| 票 | 前端范围 | 矩阵编号 | 当前状态 |
| --- | --- | --- | --- |
| 03 | 会话模块、宿主适配器、当前账号显示；旧认证包保留至票 05 | C1、C2、C8、C16 | 前端代码完成；C8 有效会话已在宿主取证，其余宿主面待验 |
| 05 | 范围选择器、页头范围条、各页范围接线 | C3～C7 | 前端代码完成；C3/C4/C6/C7 组件面已取证，C5 宿主面待页面接线 |
| 07 | 报告页面可信范围改读宿主会话；互认项目页同口径 | C13、C15 | 前端代码完成；C13、C15 组件面已取证，宿主面待走查 |
| 08 | 金额页可信范围改读宿主会话、写前复核与会话身份切换 | C11、C12 | 前端代码完成；C12 组件面已取证，C11 宿主面待子应用登记 |
| 09 | 统计与导出页面接线 | C14、C15 | 前端完成；C14、C15 组件面已取证，宿主面待走查 |
| 10 | 参数维护页与路由 | C9、C10 | 前端完成；C9 组件面已取证，宿主入口走查待子应用登记；真实库取证待迁移执行 |
| 11 | 旧 Base 前端依赖退出与 C16 静态归零 | C16 | 前端完成；app src 与 app package.json 依赖归零，api-client 包 @dy/auth 与 kiota-middleware 已退出；宿主面待走查 |

## 执行记录

- [x] 基线：lint 0、831 通过 / 2 跳过、构建 0 错误。
- [x] 票 03 前端部分：新增 `src/session/` 会话模块（`hostSession.ts`、`HostSessionContext.tsx`、`HostSessionProvider.tsx`）与 `src/components/CurrentAccount.tsx`，首页显示当前账号与显示名；`ApiClientProvider` 使用 `@dy/api-client-medical-recognition` 的应用令牌适配器（Authorization 统一取自 `MicroSDK.auth.getApplicationToken({ targetAppCode: 'medical-recognition' })`）；`@dy/micro-sdk` 升级到 0.1.46（0.1.41 起支持 `targetAppCode`）。`@dy/auth`、`@dy/auth-react`、`@dy/components-base`、`@dy/api-client-base` 保留到票 05 页面迁移后删除。
- [x] 票 03 验证：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 837 通过 / 2 跳过（另有一轮既有 `StandardCatalog.test.tsx` C69 用例在并行负载下 20 秒超时，单文件复跑通过，与迁移前基线的并行时序现象相同）；`pnpm -F dy-medical-recognition build` → 0 错误。
- [x] 票 03 复核（2026-10-23）：重跑 lint → 0；test → 837 通过 / 2 跳过；build → 0 错误。`src/session/hostSession.ts` 只从 `MicroSDK.auth.getSession()` 与 `subscribe` 取会话并映射为视图模型，匿名、初始化与错误快照不保留旧身份；`HostSessionProvider` 先订阅再读初始快照，卸载时置停用标记并释放订阅。
- [x] 票 03 修正（2026-10-25）：宿主内实测 `MicroSDK.auth.authenticateRequest` 对跨 Origin 业务请求返回空 headers，`createRequestAdapter` 实际发出无 Authorization 的请求（C8 实测 401）；`ApiClientProvider` 改走应用令牌适配器，`getApplicationToken` 未签发时抛 `ApplicationTokenUnavailableError` 且不发送业务请求。
- [x] 票 03 会话订阅修正（2026-10-25）：`useHostSessionSnapshot` 的 subscribe 改为模块级稳定函数，初始读取完成时通知快照订阅者，宿主回放不再触发重复订阅；金额两页、本院报告管理与统计四页恢复稳定，不再卡死。
- [x] 票 03 遗留：API Client 重生成（2026-10-23）：`pnpm -F @dy/api-client-medical-recognition prepare-openapi` → 62 条路径；新增组织范围查询两条与 `/api/v1/current-session`，输入文档不再含 `/auth/login`。`prepare-openapi.mjs` 补齐后端 OpenAPI 形态归一：单引用 `oneOf` → `$ref`（28 处）、`type` + `nullable` → `type: ["null", X]`（347 处）、根 `openapi` 统一为 `3.1.1`；归一后全文无 `oneOf` 与 `nullable`。`pnpm -F @dy/api-client-medical-recognition generate` 成功；typecheck/build → 0 错误。`Stage2EnumContractTests.Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes` 的 `expectedPaths` 同步为 62 条（移除 `/auth/login`，追加 `/api/v1/current-session`、`/Api/OrganizationScopeQuery/QueryCurrentOrganizationScope`、`/Api/OrganizationScopeQuery/QueryOrganizationScopeOptions`）；后端全量 `dotnet test Dy.MedicalRecognition.slnx` → 917 通过 / 0 失败。
- [x] 票 05 前端部分（2026-10-23；取值口径已变更，证据待重新取证）：
  - 新增 `src/components/organizationScope.ts`（范围选项归一）、`OrganizationScopeSelector.tsx`（IAM 组织范围三级联动选择器，值为 IAM 组织业务编码，固定组织/固定医院两种本院入口形态）、`currentOrganizationScope.ts`（当前身份范围查询与按 `sessionVersion` 的竞态防护）；替换 `ReportManagement`、`RecognitionAmounts` 与 `statisticsBoard` 共 5 处 `BaseScopeSelector`；`CurrentScopeBar` 改为读取本项目范围查询，名称缺失显示「未知」。
  - 选项读取失败给出重试入口，未配置类型的节点数量单独提示；匿名状态不发起范围查询；身份切换后选项与名称按新 `sessionVersion` 重算，迟到响应不写入新会话。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 846 通过 / 2 跳过（新增 `OrganizationScopeSelector.test.tsx` 11 项、`CurrentScopeBar.test.tsx` 重写 7 项）；`pnpm -F dy-medical-recognition build` → 0 错误。
- [ ] 票 03 宿主面：C1（宿主登录与装载）、C2（会话字段）、C8（请求鉴权）待 IAM 基座完成子应用登记后验收；C16（Base 残留静态核对）静态面已随票 11 归零，宿主请求走查待子应用登记。
- [ ] 票 05 宿主面：C3 的宿主页头走查、C5 的本院页面范围接线（报告页与金额页均已改用会话范围，宿主走查待执行）、C6 的真实工作身份切换走查待页面接线后执行。
- [x] 票 07 前端面（2026-10-23）：
  - `reportsTrustedScope.ts` 与 `recognitionProjectsOrganizationScope.ts` 的组织、医院、院区来源改为宿主会话的 `organizationId`、`hospitalId`、`campusId`，取其经本项目范围查询返回的 IAM 组织业务编码作为业务取值（会话组织实例 Id 只用于目录读取）；命令式现值读取与 `useHostSessionSnapshot` 订阅共用同一会话快照，匿名、初始化与缺失层返回不可用。
  - 测试替身按会话口径迁移：`ReportManagement.test.tsx`、`RecognitionProjects.test.tsx`、`routes.test.ts` 改为会话 store；`BranchRecognitionAmounts.test.tsx` 保留旧包替身覆盖票据 08 未迁移的页面直接读取，同时补会话替身覆盖两个接缝；新增 `reportsTrustedScope.test.tsx` 覆盖医院/院区现值读取、身份切换重算与匿名不可用。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 850 通过 / 2 跳过（29 个测试文件；既有 `StandardCatalog.test.tsx` 并行时序用例本轮通过）；`pnpm -F dy-medical-recognition build` → 0 错误。
  - API Client：`pnpm -F @dy/api-client-medical-recognition prepare-openapi` → 62 条路径、无 `/auth/login`；`generate` 输出无差异；`build`、`typecheck` → 0 错误。
  - 未完成：C13、C15 与真实工作身份切换的宿主面走查待 IAM 基座登记子应用；冻结 OpenAPI 契约用例因并行后端改动使测试项目暂不可编译未执行（见阶段测试报告）。
- [x] 票 08 前端面（2026-10-23；取值口径已变更，证据待重新取证）：
  - `RecognitionAmounts.tsx`、`BranchRecognitionAmounts.tsx` 的可信组织、医院、院区来源改为宿主会话（`useHostSessionSnapshot` 与票 07 接缝的命令式读取），移除 `@dy/auth`、`@dy/auth-react` 与 `LoginUserManager` 的可信范围读取；平台页按会话三层重算范围，身份切换到另一组织/医院时按新会话重新解析，本院区页组织、医院与院区随会话更新，导出前现读院区。
  - 金额页请求沿用现有 `organizationCode`、`hospitalCode`、`branchCode` 字段名，值取选择器与范围查询下发的 IAM 组织业务编码，不建编码映射表；适配层与后端契约未改。
  - 测试替身改为会话 store：`RecognitionAmounts.test.tsx`（80 项，新增工作身份切换重算 1 项）、`BranchRecognitionAmounts.test.tsx`（59 项）；平台页写前复核按组织、医院、院区三层比对，阻断时零写请求。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 863 通过 / 2 跳过（30 个测试文件）；`pnpm -F dy-medical-recognition build` → 0 错误。
  - 未完成：C11 跨组织/跨医院提交拒绝的宿主面走查待 IAM 基座登记子应用；C12 组件面由两个金额页 C34 用例覆盖。
- [x] 票 10 前端面（2026-10-23）：
  - 阶段 A 菜单登记：`/Api/Application/CreateMenu` 新增「平台参数维护」（applicationId `3a2479da-02e6-3ee2-6863-40b05cbeaaff`，route `medical-recognition/platform-parameters`，parentMenuId null，type 0，target 0，icon 空串，supportedClients ["desktop"]，isEnabled true）→ menuId `3a247ed9-8449-31c0-e7ed-efed78636476`；`/Api/Application/QueryMenuByApplicationId` 读回 11 项含该菜单；`/Api/Role/CreateRoleMenu` 绑定开发角色（tenantId `9ae3125b-6d1d-4689-a4fc-2e21adf75acd`、roleId `2525c0f7-d97c-477e-af31-4b95a53a8044`）→ 绑定 id `5f7a74fb-eab8-4fb3-8731-72267ec43ee7`；`/Api/Role/QueryRoleMenuByRoleId` 读回 11 条，新菜单 `isAvailable: true`。
  - 阶段 B 页面与接线：新增 `src/pages/platformParameters/`（`PlatformParameters.tsx`、`platformParametersApi.ts`、两份测试）与路由 `platform-parameters`；页面按固定业务顺序展示三项参数的编码、名称、当前值、说明、状态、操作人与时间，缺少记录显示「未配置」；编辑按参数规则提交前阻断（开关 0/1、排除时长非负整数、引用详情时长正整数），保存成功后重新查询列表再关闭弹窗，失败保留弹窗与输入。
  - API Client：`prepare-openapi` → 64 条路径（新增 `GET /api/v1/platform-parameters`、`PUT /api/v1/platform-parameters/{parameterCode}`；可空归一 350 处），`generate`、`typecheck`、`build` → 0 错误。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`test` → 869 通过 / 2 跳过（31 个测试文件，新增 18 项）；`build` → 0 错误。
  - 未完成：C9 宿主入口走查与真实后端链路待 IAM 基座登记子应用；C10 属统计与匹配页面的参数缺失/非法配置提示，随票 07～09 页面取证，不在本页范围。
- [x] 票 09 前端面（2026-10-23；取值口径已变更，证据待重新取证）：
  - 统计四页（使用方/来源方 × 平台/本院）的可信范围继续走票 07 的宿主会话接缝；页面筛选沿用 `organizationCode`、`hospitalCode`、`branchCode` 字段名，值取选择器与范围查询下发的 IAM 组织业务编码，汇总、明细与导出共用同一份已生效条件。
  - 承接范围名称不再经 Base 目录反查：平台页名称随范围选择器的选项清单一并进入已生效条件；本院页名称取 `useCurrentOrganizationScope` 的当前组织范围，与页头范围条同源。删除 `src/components/scopeNameLookup.ts` 与其测试文件，统计页对 `@dy/auth`、`@dy/auth-react`、`LoginUserManager` 的替身与过期注释一并移除。
  - 范围切换复位、默认近 30 天时间窗、空态与分页/选中行为保持既有用例；身份切换时 `scopeKey` 含会话三层，按新会话清空并重读，迟到响应由既有序号机制丢弃。
  - 验证：`pnpm -F dy-medical-recognition lint` → 0；`pnpm -F dy-medical-recognition test` → 863 通过 / 2 跳过（30 个测试文件；删除 `scopeNameLookup.test.ts` 的 7 项后仍全绿）；`pnpm -F dy-medical-recognition build` → 0 错误。
  - 未完成：C14、C15 的宿主走查待 IAM 基座登记子应用；真实导出文件与真实库面待迁移执行后补（后端范围）。
- [x] 票 11 前端面（2026-10-23）：旧 Base 前端依赖退出。
  - 删除 main.tsx 的 AuthProvider、configureBaseComponents 与 runtimeConfig.baseApiBaseUrl；public 两份配置同步清除 baseApiBaseUrl。
  - 删除 RecognitionAmounts.test.tsx、ReportManagement.test.tsx 的 @dy/components-base 替身，改为页面本地三级选项数据源；删除 BranchRecognitionAmounts.test.tsx 的 @dy/api-client-base 替身与空目录替身；BranchReportManagement.test.tsx 清除包说明。
  - package.json 移除 @dy/auth、@dy/auth-react、@dy/components-base、@dy/api-client-base；pnpm install 后 lockfile 更新。src 下四个包引用为 0，baseApiBaseUrl 为 0。
  - 规范修正：`OrganizationScopeSelector` 与 `currentOrganizationScope` 的 `client.api` 调用移入共享适配层 `organizationScopeApi.ts`；`platformParametersApi.savePlatformParameter` 不再本地抛错，改为返回布尔结果，由页面提示「未确认成功」。
  - 验证：lint 0；test 867 通过 / 2 跳过（32 个测试文件；新增应用令牌适配器 2 项与会话订阅稳定性 1 项，相对 864/30 增加 3 项）；build 0 错误；git diff --check 0。
  - 包内人工维护锚点 `createMedicalRecognitionApiClient` 保留名称与导出位置，实现改为应用令牌适配器：`getApplicationToken({ targetAppCode: 'medical-recognition' })` 提供 Authorization，401 上报宿主；`packages/api-client-medical-recognition` 的 `@dy/auth` 依赖删除。
  - `@dy/kiota-middleware` 与 `@dy/micro-sdk@0.1.39` 已从依赖树退出：`pnpm why @dy/kiota-middleware` 无输出，lockfile 中 `@dy/kiota-middleware`、`@dy/auth@`、`@dy/micro-sdk@0.1.39` 均为 0 处；`pnpm install` 无 peer 警告。
  - 包验证：`pnpm -F @dy/api-client-medical-recognition build`、`typecheck` → 0 错误；后端 `dotnet test Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Stage5ConstraintTests"` → 11 通过 / 0 失败（含 `Api_client_package_keeps_the_human_maintained_surfaces`）。
  - 未完成：C16 宿主面与旧包网络请求走查待 IAM 基座登记后执行。

## 待办与交接

- 菜单在宿主 IAM 登记，路由采用「微应用编码 + 页面」形式。
- `@dy/kiota-middleware` 已随包内 `@dy/auth` 依赖删除从依赖树退出（`pnpm why` 无输出），此前的 `@dy/micro-sdk@0.1.39` peer 警告消失。
- 页头与统计页承接范围名称均改由后端范围查询或选择器选项提供；`scopeNameLookup.ts` 及其测试已删除，Base 目录名称反查在统计页归零。
- 范围选择器产出 IAM 组织业务编码；报告页、互认项目页、金额页与统计四页的可信范围均已改读会话，请求沿用既有 Code 字段名提交业务编码；会话组织实例 Id 只用于组织目录读取。剩余旧包删除随票 11。
