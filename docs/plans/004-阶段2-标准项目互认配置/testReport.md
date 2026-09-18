# 阶段 2 测试报告

## 执行边界

本报告记录阶段2各轮的文档检查与业务矩阵执行结果；业务矩阵的执行边界按下方"执行边界"与各轮小节区分，当前终态见"结果"节。阶段1已于 2026-09-15 完成规定收口（阶段状态 `Complete`，结论见阶段1 `testReport.md`）；本报告不重复裁定阶段1的历史证据。

- 报告状态：**已执行**。52 条中 `Passed` 47 条、`Blocked` 4 条、`N/A` 1 条（不存在配置 ID 的用例），`NotRun`/`Failed`/`PendingRetest` 为 0。4 条受阻项均为**环境或框架边界导致的规定验证面未执行**，其受阻原因登记为决策标签（**接受口径待负责人确认**；决策标签不计入统计，这 4 条 `Blocked` 计入 52 条分母并单列）：**组织切换后目标组织读取失败**（C17）的真实宿主页面层、**事件登记/处理/提交的可见性**（V24）、**并发同方向停用时"恰好登记一次事件"的计数子面**（V26）、**跨组织写配置 ID 的拒绝**（V27）。宿主验收按 [Testing](../../../.agents/instructions/testing.md) 第 1、2 节从真实宿主登录页与菜单执行；`Passed` 只覆盖各用例声明的层级。**2026-09-16 批次A/B 整改后**，受影响用例的宿主证据已按 Testing Baseline §5 重取（见"批次A/B 整改后的宿主重取（2026-09-16）"）；同日完成**7 轴整改后复审与收口前清理**（0 Blocker、0 实现层硬违规），该轮门禁见"整改后复审与收口前清理（2026-09-16）"节；同日后续的 7 轴复审整改见"复审后整改（2026-09-16，批次16 / B15 / F12）"，最新门禁以"门禁终值"节为准；其后的第二批整改见"第二批整改（2026-09-16，批次17 / B16 / F13）"；其后的第三批整改见"第三批整改（2026-09-16，批次18 / B17 / F14）"。轮次与批次的对照见"轮次索引"节。
- 矩阵版本：2026-09-14，后端 V1-V27（27条），前端 C1-C25（25条），总分母52条；多层级一条只计一次。
- 起始提交：`5521cb6b5eb0ba0382eab0efc4c6fdcacfefa1c6`。本轮在共享工作区改代码与文档，不创建分支或提交。
- 起始状态：实现批次开始前执行 `git status --short --branch`；工作区已有其他修改，未回退或清理。工作区另存在一个**非本轮创建**的既有 worktree `E:/MedSync/MedicalRecognition.worktrees/request-234-branch`（`5521cb6 [agents/request-234-branch]`，与本阶段无关）；本轮未创建、修改或删除任何分支/worktree/提交。
- 本轮未执行建表、迁移、数据库写入、代码生成或宿主验收；数据库侧仅做只读元数据核对。**（该句为文档检查轮次的记录；后续轮次的实际执行见下方"本轮执行结果"与"Ticket 08 宿主验收执行结果"：建表已执行、API Client 已重生成、宿主验收已完成，数据库侧始终只做只读核对。）**
- 执行边界（2026-09-15 更新）：**允许经真实业务接口或宿主页面写入测试数据**（符合 [Testing](../../../.agents/instructions/testing.md) §5"页面能产生的数据优先通过宿主页面准备"）；**仍不得直接改库、执行 DDL/DML 脚本或使用测试夹具绕过业务入口**。阶段2 `mrec_mutual_recognition_item` 建表已完成，宿主菜单子项「互认项目」已维护；宿主入口、账号与密码见 `.agents/instructions/test-environment.md` 第 1 节；子应用路由与真实接线已随 Ticket 07 完成；Ticket 08 宿主验收已于下方"Ticket 08 宿主验收执行结果"章节执行。

## 轮次索引

本报告按轮次保留过程小节：各轮的原始证据、命令输出与门禁数值**只在对应轮次小节维护一次**，"该轮门禁"列只登记该轮实际执行了哪些门禁（数值见对应小节），各端最新值统一见"门禁终值"节。本索引不含终态与证据边界小节——"结果""未覆盖面与残余风险""结论""文档检查证据""门禁终值"不属轮次登记。

| 轮次 / 批次 | 时间 | 范围 | 改动面 | 该轮门禁 | 对应小节 |
|---|---|---|---|---|---|
| 批次1-6 / F1-F3（Ticket 01-08） | 2026-09-15 | 组织与事件机制取证、写契约与规则、查询契约与目录原因、最终 DDL 与 SqlMap、累计 Client 与页面组件、真实数据库与宿主验收 | 后端 `server/**`、客户端包与子应用页面/适配层、阶段文档 | 后端构建与全量测试、契约与注册键探针、前端 `test`/`build`/`lint`、累积 Client `typecheck`/`build`、数据库只读元数据、宿主页面链路取证 | "本轮执行结果（2026-09-15）"、"Ticket 08 宿主验收执行结果（2026-09-15）" |
| 批次7 / F4（批次A 后端、批次B 前端） | 2026-09-16 | 七份独立审查的整改（A、B 两组），主线程复核另修两处 | 后端契约属性约束与共享组织解析点、查询枚举映射、测试探针；前端页面入口门控与弹窗拆分、表格行标识 | 后端构建与全量测试、前端 `test`/`build`/`lint`、`git diff --check`；受影响用例按 Testing Baseline §5 在宿主重取 | "批次A/B 整改后的宿主重取（2026-09-16）" |
| 批次8 / B7 | 2026-09-16 | 整改后七轴复审与收口前清理（5 组） | 前端目录就绪判定、查询侧组织比较与下传、后端注释与文档、报告口径 | 后端全量测试（含调用次数与 `$IsValid` 谓词断言）、前端 `test`、文档链接与表格列数检查、宿主复验列表查询与越权拒绝 | "整改后复审与收口前清理（2026-09-16）" |
| 批次9 / B8 / F6（批次C/D/E） | 2026-09-16 | 第三轮七轴复审 26 条 finding 的整改 | 后端查询仓储 `<remarks>` 与应用层、测试断言；前端页面文案唯一出口与用例；阶段文档 | 后端构建与全量测试、前端 `test`/`build`/`lint`、`git diff --check`；查询与页面渲染路径在宿主重取 | "第三轮复审后的修复（批次C/D/E，2026-09-16）" |
| 批次10 / B9 / F7 | 2026-09-16 | 跨阶段立项的平台级枚举契约缺陷修复 | 后端枚举 `[EnumDescriptor]`/`[Description]`、描述器注册表与 OpenAPI 文档转换器；生成入口 `oneOf [null, $ref]` 归一与重生成 | 后端构建与全量测试、真实 `/openapi/v1.json` 实测、前端 `test`/`build`/`lint`、客户端包 `typecheck`/`build` | "枚举契约缺陷修复（跨阶段立项，2026-09-16）" |
| 批次11 / B10 / F8 | 2026-09-16 | 枚举中文来源（`Dy.LisCenter` 同口径） | 后端 `EnumDescriptorText`、枚举元数据查询契约与应用服务、只读模型文本字段；前端 `useEnumMetadata` 与页面文案取用 | 后端构建与全量测试、前端 `test`/`build`/`lint`、客户端包 `typecheck`/`build`、宿主实测枚举元数据接口与列表响应 | "枚举中文来源（LisCenter 同口径，2026-09-16）" |
| 批次12-14 / B11-B13 / F9-F11 | 2026-09-16 | 对"枚举契约修复"与"枚举中文来源"两个增量的三轮增量复审整改（共报 53 条 finding） | 后端守卫用例与变体自校验、生产注释与 XML 文档；前端 hook 返回值与用例、适配层常量；阶段文档 | 后端构建与全量测试、前端 `test`/`build`/`lint`、客户端包 `typecheck`/`build` | "增量复审与整改（2026-09-16）" |
| 收口（非代码轮次） | 2026-09-16 | 交付范围、终态矩阵与残余项登记 | 阶段文档 | "门禁终值"表、文档检查、宿主终验 | "收口（2026-09-16）" |
| 批次15 / B14 | 2026-09-16 | 源码守卫对齐（仅测试侧）：判据改按 Roslyn 语法树，新建共享语法工具与复用治理 | 后端测试工程与中央包版本 | 后端构建与全量测试、`git diff --check`（前端与客户端包不适用） | "源码守卫对齐（2026-09-16，收口后仅测试侧改动）" |
| 批次16 / B15 / F12 | 2026-09-16 | 复审后整改：后端注释与风格、测试缺口；前端写入门控与用例 | 后端生产注释、`using` 顺序、私有字段改名与测试；前端写弹窗门控与提交前复核、`RecognitionProjectModals.tsx`；阶段文档 | 后端构建与全量测试、前端 `test`/`build`/`lint`、`git diff --check`；本批未重取宿主证据（分支在真实宿主不可构造） | "复审后整改（2026-09-16，批次16 / B15 / F12）" |
| 批次17 / B16 / F13 | 2026-09-16 | 第二批整改：写入口契约口径、测试守卫与测试运行配置；页面状态、用例与证据口径 | 后端契约 XML 文档、共享解析点内联、Provider 中立守卫、`xunit.runner.json`；前端枚举元数据请求门控、修改弹窗取当前行、表格行标识断言；阶段文档 | 后端构建与全量测试、前端 `test`/`build`/`lint`、`git diff --check` 与未跟踪文件扫描；本批未重取宿主证据 | "第二批整改（2026-09-16，批次17 / B16 / F13）" |
| 批次18 / B17 / F14 | 2026-09-16 | 第三批整改：契约异常声明与防御分支登记、守卫覆盖；前端提交期现值复核、启停弹窗取值、页面口径；批次台账与本报告回执 | 后端契约注释与测试工程（Provider 中立自校验样本、运行器配置守卫）；前端 `readTrustedOrganizationCode`、`submitWrite`、启停弹窗与未保存内容判定；阶段2 文档 | 后端构建与全量测试、前端 `test`/`build`/`lint`、`git diff --check`；本批未重取宿主证据（分支在真实宿主不可构造），终值见"门禁终值"节 | "第三批整改（2026-09-16，批次18 / B17 / F14）" |
| 批次19 / B18 / F15 | 2026-09-16 | 第三批整改后的宿主链路验收：入口与开发地址拦截、首次加载与组织编码、列表渲染与文案、修改可互认时间（同值保存）、启停、编码与状态筛选口径、读取失败门控与重试、写弹窗门控放行 | 无代码改动（改动面只有阶段2 文档登记） | 宿主链路验收（受管前后端进程 + 宿主登录页与菜单进入；按 [Testing](../../../.agents/instructions/testing.md) 第 1、2 节在同一链路核对页面、DOM、Console、Network、响应与最终业务状态） | "宿主验收（2026-09-16，第三批后重取）" |

本报告保留轮次小节，与 [planning-and-evidence.md](../../../.agents/instructions/planning-and-evidence.md) §4"只表达终态"存在张力，处置待规范层裁定。

## 本轮执行结果（2026-09-15）

Ticket 01-06 已交付批次的已执行证据。命令均在仓库根执行；"证据"列只记录实际运行结果，不替代尚未执行的用例。

| 检查面 | 命令或操作 | 实际结果 |
|---|---|---|
| 契约形状与生成映射（V21 契约面） | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` | 契约断言 5 条（创建请求字段集合、请求→命令生成映射、ReadModel 字段与可空性、查询请求字段与可选性、查询请求组织编码必填）全绿；四个写请求的正整数下限与非空校验由写入口批次补齐后 V21 完整关闭 |
| 写入侧规则（V1-V9、V11、V15-V16、V26-V27 离线面） | `dotnet test ... --filter FullyQualifiedName~Stage2WritePathTests` | **32/32 通过**（含 2 个三分支 Theory）；覆盖创建三层目录校验与默认启用、停用层拒绝、天数正整数、停用后修改、同值保存、启停幂等无事件、0 行复读三分支、>1 行不变量异常、跨组织 ID 拒绝、可信组织注入与缺失拒绝、事件字段与失败不登记、单写语句且无 `[WorkUnit]` |
| 查询侧规则（V13、V19、V20 逻辑面） | `dotnet test ... --filter FullyQualifiedName~Stage2QueryTests` | **16/16 通过**；覆盖三层启用为 `null`、单层停用三条文案、多层停用优先级、逐层恢复、配置自身停用不产生目录原因、组织不等值拒绝且不降级空集合、空结果空集合、字段映射完整性 |
| 全量测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-restore` | **通过 75 / 失败 0 / 总计 75**（主线程独立复跑确认）；阶段1 既有用例无回归 |
| 解决方案构建 | `dotnet build server/Dy.MedicalRecognition.slnx --no-restore` | **0 错误**（仅 git 换行提示类警告） |
| SqlMap 注册键（V23 静态/注册面） | `dotnet test ... --filter "FullyQualifiedName~Runtime_registration_reports_full_sql_ids_without_opening_database" -l "console;verbosity=detailed"`（主线程执行，原文摘录） | 互认侧 7 条：`MutualRecognitionItem.{CreateMutualRecognitionItem, UpdateMutualRecognitionItemConfiguration, EnableMutualRecognitionItem, DisableMutualRecognitionItem, GetMutualRecognitionItemById, MutualRecognitionItemColumns, QueryAllMutualRecognitionItem}`；查询侧含 `MedicalRecognitionReportQuery.QueryRecognitionProjectConfigurationList`；标准项目侧含新增 `MedicalStandardItem.GetMedicalStandardItemByCode`。旧聚合键 `MedicalRecognitionReport.CreateMutualRecognitionItem` 已不存在，且无其他调用点引用旧键 |
| 写入侧 SqlMap 映射（V17 静态面） | 静态核对 `MutualRecognitionItem.xml`、仓储 scope 常量与调用点 | `Scope` 与 `MutualRecognitionItemScope` 同为 `MutualRecognitionItem`；互认语句引用的物理表只有 `mrec_mutual_recognition_item`；四个写方法与新增读取方法逐调用显式传 `scope`/`sqlId`，无 `SetContext`，无 `current_timestamp`；启停语句带 `organization_code` 与 `is_valid` 原状态条件 |
| DDL 静态核对（V18/V25 静态面） | 逐项人工/脚本比对 `Scripts/mutual_recognition_item.sql` 与 `Server/design.md` 列定义 | 8 列列序、类型、可空性一致；无默认值、无 `varchar(n)`、无外键；表注释 1 + 列注释 8 + 索引注释 1；唯一索引 `ux_mrec_mutual_recognition_org_project` 覆盖 `(organization_code, standard_project_code)` 且无状态过滤 |
| 后端 OpenAPI 契约实测（Ticket 06 输入） | `GET http://localhost:15014/openapi/v1.json`（Development 环境，受管后台作业） | 22 个 path（该数字为该轮时点口径，批次10/11 后为 23）；含 `/Api/MedicalRecognitionReportQuery/QueryRecognitionProjectConfigurationList` 与四个互认写端点；`CreateMutualRecognitionItemRequest` 属性为 `standardProjectCode`/`recognitionDurationDays`（**已无 `organizationCode`**）；查询请求 `required` 为 `organizationCode`；`/auth/login` 仍存在（排除规则必要） |
| 累计 API Client 生成与契约校验（C22/C24） | `pnpm -F @dy/api-client-medical-recognition prepare-openapi` → `generate` → `typecheck` → `build`（后端 15014 实测提供 OpenAPI） | 稳定 OpenAPI 22 path（生成前 21，净增 1 个查询端点；**2026-09-16 批次10 修枚举契约、批次11 新增 `/Api/EnumMetadata/GetEnumMetadata` 后，冻结文档为 23 path，见"枚举中文来源（LisCenter 同口径，2026-09-16）"**）；`generate` 成功且未用 `--clean-output`；`typecheck` exit 0；`build` exit 0（esm 72.12KB / cjs 87.65KB / dts 98.89KB）；主线程独立复跑一致；`src/index.ts` SHA256 前后一致（未被覆盖）；`organizationCode` 由 4 处收敛为仅查询请求 3 处；生成物无 `api/auth` 目录、锁文件 `excludePatterns` 保持 `/auth/login` |
| 前端组件与适配层基线（C1-C3、C6、C8、C10-C11、C13-C21、C23 的组件/纯逻辑面） | `cd client; pnpm -F dy-medical-recognition test` / `build` / `lint` | 4 文件通过、**73 passed / 2 skipped**（75）、exit 0；新增 39 条用例（组件 21 + 适配层 18）；`build`（tsc -b + vite build）exit 0；`lint` exit 0（3 条为未触碰文件的既有 react-refresh 警告）；主线程独立复跑一致。组织接缝按结案口径接入 `LoginUserManager.getOrgID()` + `useAuth()`（`@dy/auth` 0.1.39 与 `@dy/auth-react` 实测导出）（括号内为**后补标注**：该行是组件基线轮次的快照，当时为 4 文件 73 passed / 2 skipped、新增 39 条；**Ticket 07 完成真实接线后的时点为 5 文件 85 passed / 2 skipped（87）**——适配层 `recognitionProjectsApi.test.ts` 27 条、组件 `RecognitionProjects.test.tsx` 21 条、`routes.test.ts` 3 条、阶段1 `standardCatalogApi.test.ts` 15 条 + `StandardCatalog.test.tsx` 21 条（2 skip）；**批次11 时点值为 6 文件 105 passed / 2 skipped（107）**（终值见本报告"门禁终值"节；本行其余括号标注同为各轮时点值），见"枚举中文来源（LisCenter 同口径，2026-09-16）"节；接线已用生成端点完成、`pendingIntegration` 已移除，C 编号覆盖声明已由独立复核确认，见 `Client/审查-Ticket07接线复核.md` §3） |
| 前端既有配置未被改动 | `git status --short -- client/apps/dy-medical-recognition`；`git show HEAD:...vite.config.ts` | 应用目录仅有新增未跟踪的 `src/pages/recognitionProjects/`；`vite.config.ts` 的 `dangerouslyIgnoreUnhandledErrors` 属阶段1 既有提交（`51a5d15`），非本轮引入，未放宽测试 |
| 宿主链路与组织取值（V22 运行面、C12 前置） | 受管后台作业启动后端 15014 + 前端 3008；Chrome DevTools MCP 从宿主登录页登录；读取 localStorage 与宿主菜单接口 | 后端 `localhost:15014` 就绪（OpenAPI 22 path，该数字为该轮时点口径，批次10/11 后为 23）；无 token 调用业务接口返回 **401**）；前端 dev server `http://localhost:3008/subApps/medical-recognition/` 就绪；从宿主 `http://183.224.180.166:35000/login` 登录，**入口选「检验检查结果互认平台」**（登录后标题一致、`subAppCode=medical-recognition`）；**劫持已生效**（全局开关 `true`，含 `{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}`）；**token 载荷实测 `org:"01"`、`sub:"medical-recognition"`、`usr:"3a217eb3-…"`，`login-user.orgID="01"`、`hosID="0101"`、`branchID="0101001"`、`isAdmin=false`** —— V22 的"真实 token 是否携带 org"由此确认 |
| 宿主菜单可达性（C12） | 展开宿主菜单 + 读 `QueryAllMenu` / `QueryUserRoleMenu` 原始响应 | **菜单已正确创建**：「互认项目」`id=3a23b778-2073-1357-6159-a7d67ae96891`，`parentId` 为父菜单「检验检查结果互认」，`parameter.webRoute="medical-recognition/recognition-projects"`（与代码路由一致）。**但未分配给该账号角色**：`QueryUserRoleMenu` 仅返回父菜单与「标准项目目录维护」两个 id，新菜单不在其中 → 菜单不可达，当时 C12 及依赖菜单进入的 Host 用例保持 `Blocked`。**后续已闭环**：补齐角色-菜单授权后菜单出现并可进入，C12 等 Host 用例随后实测通过（见下方"菜单与组织取值"与"逐条用例证据"） |
| 页面挂载与组织缺失防御（排查性直达，**不计入宿主验收**） | 在浏览器中直达 `http://localhost:3008/subApps/medical-recognition/recognition-projects`（该 tab 无宿主 token，属排查手段） | 路由可挂载：页面标题「互认项目」+ 副标题「标准项目互认配置」；组织不可用时给出阻断提示「当前无法从登录凭证确定可信组织范围…请从宿主登录后重新进入本页面」；**全程零业务 API 请求**（仅 `config.development.json`），符合"不伪造组织、不退化、不发请求"的设计要求。仅证明路由与防御分支，**不能替代宿主菜单验收** |
| 目标库只读核对与 DDL 元数据（V18/V25 真实库面） | dbx 只读连接 `协同平台外网`（PostgreSQL `183.224.180.166:15432` / `DysoftHIS`）查 `information_schema.columns`、`pg_indexes`、`obj_description` | 在 `public` schema 建立 `mrec_mutual_recognition_item` 后逐项核实：**8 列**列序与类型为 `id uuid / organization_code text / standard_item_id uuid / standard_project_code text / recognition_duration_days integer / is_valid boolean / oper_time timestamptz / oper_id uuid`，全部 `NOT NULL` 且 `column_default` 均为 `NULL`（无数据库默认值）；8 条中文列注释与设计逐字一致；表注释为「互认项目配置」；索引两条——主键 `mrec_mutual_recognition_item_pkey (id)` 与唯一索引 `ux_mrec_mutual_recognition_org_project (organization_code, standard_project_code)`，后者 `indexdef` **无 WHERE 过滤条件**、索引注释为「同一组织标准项目互认配置唯一（含停用配置）」；无外键。**说明**：V18/V25 声明的层级本就是"真实库元数据"面，故 dbx 只读元数据是该用例规定的取证手段；它不用于、也不能替代任何应用行为证据（见 C# Backend Testing §3） |
| 变更影响 | `gitnexus detect-changes --repo Dy.MedicalRecognition` | 工作区含多批次并发改动，聚合风险 `high`；逐项归属复核后均属阶段2 已批准票（写入侧 HTTP→AppService→Manager→Repository/事件为预期传播、查询侧为契约扩展），无计划外跨阶段影响，未命中停止条件 |
| 差异空白 | `git diff --check` | 退出码 0 |
| 事件与组织机制取证 | 框架程序集反射 + `ilspycmd` 只读反编译（输出到 `%TEMP%`，未写入仓库） | `SourceConfirmed`：组织编码来源为 token `org` claim → `HttpRequestInfo.OrgId`；无 `[WorkUnit]` 入口由 `EventBusActionUnitFilter.OnCompletedAsync` 调 `IEventQueue.FlushAsync()`，时点在结果返回后、无事务时不回滚、失败清空队列不可重试。**运行面状态**：真实 token 是否携带组织已取证（V22，`org:"01"`）；写入与失败响应面已实测；**事件可见性（V24）整条记 `Blocked`**（无观察点，受阻原因登记为决策标签，接受口径待负责人确认）。详见 [取证-组织与事件机制](Server/取证-组织与事件机制.md) |
| 阶段1 测试适配 | 逐行复核 `Stage1ArchitectureTests.cs` 的 diff | 查询契约断言由"方法数固定为 4"改为"冻结阶段1四查询 + 阶段2互认配置列表查询共 5 个方法名"，**加强而非削弱**原有守卫；不改写阶段1 历史结论 |
| 阶段1 注册键探针扩展（V23 断言化） | 修改 `Stage1SqlMapProbeTests.Runtime_registration_reports_full_sql_ids_without_opening_database` 并复跑 | 独立审查发现该探针此前**只打印**互认注册键、未断言（Scope/sqlId 改名会静默全绿）；该轮补入 8 条断言（1 条旧聚合键 `DoesNotContain` + 5 条 `MutualRecognitionItem.*` + `MedicalStandardItem.GetMedicalStandardItemByCode` + `MedicalRecognitionReportQuery.QueryRecognitionProjectConfigurationList`），全量测试当时 **75/75 通过**；**2026-09-16 批次A 又补 4 条旧聚合作用域键的 `DoesNotContain`（修改/启用/停用/按标识读取），该用例现含 5 条旧键否定 + 7 条肯定，全量 81/81 通过**。该扩展在本表登记，不回写阶段1 历史结论 |
| 独立审查（后端 03/04） | [Server/审查-阶段2写入与查询复核.md](Server/审查-阶段2写入与查询复核.md)：0 Blocker / 0 High / 2 Medium / 6 Low；同轮另有 [Server/审查-阶段2契约与DDL复核.md](Server/审查-阶段2契约与DDL复核.md)（其各项已实质闭合：证据已归档、`organizationCode` 已随重生成消失、DDL/SqlMap 已同步） | 已整改：查询请求注释由"精确匹配"改为"字面包含"、V23 注册键断言化（见上一行）、新增 `DuplicateMutualRecognitionItemException` 已登记进 `Server/design.md` 类型归属表；**2026-09-16 批次A 又闭合**启停请求补 `[NonEmpty]` 与类型级文档、查询测试改用框架公开工厂（不再反射写 internal setter）。**仍未整改项**（如实登记）：两处断言只校验"业务拒绝"字样、DDL 去掉 `if not exists` 未单独登记、测试工程未禁用并行集合（多次重复运行未复现干扰；**2026-09-16 第二批整改已闭环**：`Tests/xunit.runner.json` 关闭程序集与集合并行，依据与实测代价见"第二批整改（2026-09-16，批次17 / B16 / F13）"节）；另"标准项目编码字面包含"语义属设计未定义、按项目既有约定选择，列为待确认项 |
| 审查目录隔离 | `git status --short` 全程 | `阶段2审查相关/` 无改动；未创建分支、worktree 或提交 |

**RED 边界（如实记录）**：写入侧本轮取得的是**编译级**真实失败（19 个 `error CS` 逐个点名缺失成员），因写入侧与查询侧并发修改同一解决方案、工作区曾短暂不可编译，**行为级 RED 未捕获**；查询侧取得行为级 RED（`NotImplementedException` 骨架下 15/16 失败）。该差异不改变已执行的 GREEN 证据，但在完成闸门"先获得准确失败测试"上构成一个明确边界。

未覆盖面（**该段为组件/实现轮次的历史记录，已被后续宿主验收取代；保留以如实反映当时的证据边界**）：当时 V2、V5、V7、V12、V14、V15、V17、V18、V20、V23、V25、V26、V27 的真实库面与 V22/V24 的真实运行面尚未执行（目标表未建、宿主菜单未配置、数据库只读）；C1-C25 的真实 Host 面尚未执行。**2026-09-15 后的实际终态**：目标表已建并核实（V18/V25）、宿主菜单与页面链路已通（C 矩阵 Host 面）、真实 token 的 `org` 已实测为 `01`（V22）、真实运行面已执行（V24 的库写入与失败响应面）。**事件登记/处理/提交的可见性（V24）无观察点，按 Testing Baseline §3 整条记 `Blocked`**（受阻原因登记为决策标签，**接受口径待负责人确认**，不改状态；决策标签不计入统计，本条 `Blocked` 计入 52 条分母并单列）——本条不因"写入与失败响应面已实测"而记通过。逐条证据见下文"Ticket 08 宿主验收执行结果"与"结果"章节。

## Ticket 08 宿主验收执行结果（2026-09-15）

按 [Testing](../../../.agents/instructions/testing.md) 第 1、2 节执行：真实宿主登录 → 菜单进入 → 同一链路核对页面/DOM/Console/Network/响应/最终业务状态。环境与进程见下表；所有写入均经业务接口或宿主页面，未直接改库。

### 环境与进程

| 项 | 实际值 |
|---|---|
| 宿主入口 | `http://183.224.180.166:35000/login`，登录入口选**「检验检查结果互认平台」**（登录后标题一致、`subAppCode=medical-recognition`） |
| 账号 | `yangkj`（宿主显示名 杨康健，`isAdmin=false`），密码按整体测试规范第 1 节登记 |
| 被控进程 | 后端 `Dy.MedicalRecognition.exe` PID 48256 监听 `localhost:15014`（受管后台作业）；前端 `vite` 监听 `http://localhost:3008/subApps/medical-recognition/`（受管后台作业） |
| 控制工具 | Chrome DevTools MCP（Chrome/152），未固定 Profile |
| 劫持生效证据 | 全局开关 `"true"`；映射行含 `{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}`；Network 中子应用文档 `reqid=24 GET localhost:3008/subApps/medical-recognition/recognition-projects [200]`、`@vite/client [200]`、`src/main.tsx?t=… [200]` 均来自 `localhost:3008` |
| Console | 无业务错误；仅既有 antd `Alert.message` 弃用警告 1 条（阶段1 同源） |

### 菜单与组织取值

| 项 | 证据 |
|---|---|
| 菜单存在性 | `QueryAllMenu`：「互认项目」`id=3a23b778-…`，父菜单「检验检查结果互认」，`parameter.webRoute="medical-recognition/recognition-projects"`（与代码路由一致） |
| 授权 | 补齐角色-菜单授权后，`QueryUserRoleMenu` 返回该菜单 id，菜单项出现并可进入（此前缺失，属权限配置而非代码缺陷） |
| 可信组织 | token 载荷实测 `org:"01"`、`sub:"medical-recognition"`、`usr:"3a217eb3-…"`；`login-user.orgID="01"`、`hosID="0101"`、`branchID="0101001"`。页面显示「组织编码：01」，查询请求体 `{"organizationCode":"01"}`——**组织取自可信上下文，页面无组织输入**（V22） |

### 逐条用例证据

| 用例 | 操作 | 实际结果 |
|---|---|---|
| C12 | 从宿主菜单点「互认项目」 | 子应用在新标签载入，页面标题「互认项目」+ 副标题「标准项目互认配置」 |
| C13 | 进入页面 | 自动发出两个读取请求并渲染；表格 9 列与设计一致（无创建/修改信息、无派生状态字段） |
| C1 | 观察组织编码 | 页面与请求均为 `01`，无组织选择控件 |
| C2 | 打开新增选择器 | 选项按「分类 / 分组」分组，只列最底层标准项目（阶段1 目录数据） |
| C3 | 打开新增选择器（配置为**启用**、随后改为**停用**各验一次） | 已配置项目 `123` 在两种状态下**均被隐藏**，其余项目仍可选 |
| C4 | 新增：天数填 `0` 保存 | 行内校验「可互认时间为大于等于 1 的整数天数」、字段 `invalid=true`，**零写请求**，弹窗与输入保留 |
| C4 | 新增：选 `123` + 天数 `30` 保存 | `CreateMutualRecognitionItem [200]` → 配置列表与有效目录**均重载**；弹窗关闭；新行默认**启用**，计数「共 1 项配置」。请求体 `{"recognitionDurationDays":30,"standardProjectCode":"123"}`（**无组织编码、无内部 ID**） |
| C6/C20 | 修改：回填当前值后**不改值**直接保存 | `UpdateMutualRecognitionItemConfiguration [200]` 并刷新；请求体 `{"id":"3a23b834-…","recognitionDurationDays":30}` —— **同值仍提交** |
| C8 | 停用确认框点「取消」 | **零写请求**（请求计数不变） |
| C8 | 停用/启用各确认一次 | `DisableMutualRecognitionItem [200]`、`EnableMutualRecognitionItem [200]`，每次恰好一条写请求 + 一次重载；状态在 启用↔停用 间正确切换；按钮只提供**反向**入口 |
| V1 | 新增有效配置 | 成功、默认启用、列表回读一致 |
| V2/V15 | 对同一组织+同一项目再发一次正常创建请求 | `500` + `业务拒绝：该组织已配置此标准项目。`；异常链实证 `InvalidOperationException` → `DuplicateMutualRecognitionItemException` → `Npgsql.PostgresException 23505: 重复键违反唯一约束 "ux_mrec_mutual_recognition_org_project"`；**失败后列表仍 1 行（无脏写）** |
| V5 | 修改可互认时间（同值分支） | 200 并刷新（见 C6/C20） |
| V7/V9 | 停用后再启用 | 两次状态变更均 200，终态与预期一致 |
| V12 | 列表查询 | 仅返回可信组织 `01` 的配置 |
| V13 | 三层启用 | `目录停用原因` = `—`（`null`） |
| V13 | **仅停用分组**（`S1-RETRY-GRP-20260914-001`）后刷新 | 原因 = **「所属分组已停用」** |
| V13/V20 | **同时停用分类与分组**后刷新 | 原因 = **「所属分类已停用」**（分类优先于分组，符合 S2-D14） |
| V13 | 恢复分类与分组、**仅停用标准项目 123** 后刷新 | 原因 = **「标准项目已停用」** |
| V20 | 逐层恢复（项目→启用） | 原因回到 `—`（`null`） |
| V13 | 配置自身停用（目录全启用） | `配置状态` = 停用，`目录停用原因` 仍为 `—` —— **配置自身停用不占用该字段** |
| V14 | 新增第二条配置（编码 `222`）后查询 | 返回顺序 `["123","222"]`，按标准项目编码**升序**；初始无数据时返回空数组且页面显示空态「当前组织没有互认项目配置」 |
| V19 | 查询请求提交非可信组织 `999` | `500` + `请求组织与当前登录组织不一致，不能查询该组织的互认配置。`（抛自 `MedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync`）—— **拒绝而非降级为空集合** |
| V23/V17 | 上述全部写入与查询真实执行 | 作用域与语句键在运行时解析成功（写入成功、查询成功、唯一索引冲突可被捕获），物理表为 `mrec_mutual_recognition_item` |
| V26 | 两个**并发同方向**停用请求 | 两个请求均 `200/true`，终态停用 —— 恰好一次有效状态变化，其余按幂等处理；**"事件恰好登记一次"的计数子面无可控观察点**，按 Testing Baseline §3 整条记 `Blocked`（受阻原因登记为决策标签，接受口径待负责人确认） |
| C25 | 真实环境数据量（1-2 条）下操作 | 未出现卡顿、请求超时或内存问题，未新增分页 |
| C7 | 配置置为停用后点 `修改可互认时间` 并提交 45 | 入口仍可用；`UpdateMutualRecognitionItemConfiguration [200]`；行显示 `45 / 停用` —— **停用状态未被修改** |
| C9 | 恢复配置启用后，停用其所属分组（`DisableMedicalStandardGroup [200]`），再回到本页 | 原因列显示 **「所属分组已停用」** |
| C9 | 目录停用下点 `修改可互认时间` 提交 60 | **成功**（45→60），状态不变，原因仍显示 |
| C9 | 目录停用下点 `停用配置` 确认 | **成功**（状态→停用），原因仍显示 |
| C9 | 目录停用下点 `启用配置` 确认 | **`EnableMutualRecognitionItem [500]`**，页面呈现「业务拒绝：所属分组已停用。」；状态**保持停用**、原因保留、**操作入口保留**（确认框保留以便重试/取消） |
| C9 恢复 | 重新启用该分组后刷新；再启用配置 | 原因回到 `—`（`null`）；配置回到启用；数据状态已恢复 |
| C5 首次（A 标签） | 在 A 标签新增 `S1-RETRY-ITEM-20260914-002` + 20 天并保存 | `CreateMutualRecognitionItem [200]`，响应体 `true`；请求体 `{"recognitionDurationDays":20,"standardProjectCode":"S1-RETRY-ITEM-20260914-002"}`（无组织编码、无内部 ID）；A 标签列表与选择数据重载为 **3 项**，新行 `20 天 / 启用` |
| C5 后到（B 标签） | B 标签的选项集是**页面载入时的快照**（打开弹窗**不重新拉取**目录，实测 B 标签全程只有 1 次 `QueryEffectiveMedicalStandardCatalog`），仍在可选列表中持有同一项目；选同一项目 + 20 天后提交 | `CreateMutualRecognitionItem [500]`，页面告警 **「业务拒绝：该组织已配置此标准项目。」**；异常链 `InvalidOperationException` → `DuplicateMutualRecognitionItemException` → `Npgsql.PostgresException 23505: 重复键违反唯一约束"ux_mrec_mutual_recognition_org_project"`（表 `mrec_mutual_recognition_item`）；**弹窗与两处输入保留**（项目仍选中、天数仍为 `20`）；**失败后零刷新请求**（B 标签最后一次请求即该 500），列表仍显示 **2 项**（真实组织当时已 3 项）——写入失败不改判、不刷新、不误报为成功 |
| C5 终态 | **应用链路取证（主证据）**：A 标签写响应 `200/true`，且 A 标签随即重载的列表出现该行；B 标签失败后零重载、列表未出现该行 | 组织 `01` 的配置在应用链路中恰有一条 `S1-RETRY-ITEM-20260914-002`（`20 天 / 启用`），被拒绝的那次提交未产生第二条 —— **无脏写、唯一约束恰好保留一行**。附带以 dbx 只读做交叉核对（3 行：`123`(60)、`222`(7)、`S1-RETRY-ITEM-…002`(20)）；按 [C# Backend Testing](../../../.agents/instructions/csharp-backend-testing.md) §3，**行数不作为应用验收证据，仅作辅助** |
| C21-A 读取失败分支（对照） | 在受控注入下点「重新读取互认配置」（只读动作，不改数据） | 列表请求被注入失败，目录请求仍 `200`：页面显示 **「互认配置数据待刷新」**+「读取失败，已有数据保留；维护操作暂不可用，可重试读取。」+「重试」；`新增配置` 与全部行内 `修改/停用` **均禁用**；已有 **4 行全部保留** |
| C21-A 恢复 | 点「重试」（注入为一次性，已自动失效） | 告警消失、列表回读成功、`新增配置` 与行内操作**全部恢复可用** |
| C21-B 写成功刷新失败 | 注入仅拦截列表请求后，在页面把 `123` 的可互认时间由 `60` 改为 `45` 并保存 | `UpdateMutualRecognitionItemConfiguration [200]`（请求体 `{"id":"3a23b834-a50a-3b6e-5422-fb5bad1e61cd","recognitionDurationDays":45}`）；紧随其后的 `QueryEffectiveMedicalStandardCatalog [200]` 成功，而 `QueryRecognitionProjectConfigurationList` **被注入失败**（网络面板中该请求不存在，未到达后端）；页面显示 **「写入已成功，数据待刷新」**+「写入已经成功，但重新读取配置与标准项目失败；已有数据保留，维护操作暂不可用。」+「重试」，**未出现任何"保存失败"提示**，弹窗已按成功路径关闭；`新增配置` 与行内操作均禁用；已有 4 行保留（`123` 因未刷新仍显示旧值 `60`） |
| C21-B 写是否丢失 | **应用链路取证（主证据）**：写请求 `200/true` + 点「重试」后列表回读显示 `123 = 45`（见下一行） | 说明写入确实成功、只是随后的刷新失败。注入期间的 dbx 只读（`123` 已是 `45`，`oper_time` = 提交时刻）仅作辅助交叉核对，按 C# Backend Testing §3 不作为应用验收证据 |
| C21-B 恢复 | 点「重试」 | 告警消失、列表回读成功，`123` 显示 **`45`** —— 写入结果未丢失，阻断态可恢复 |
| C21 收尾 | 恢复现场 | 经页面把 `123` 改回 `60`；移除注入钩子并还原 iframe 的 `window.fetch`（实测 `fetch === 原始引用`）；注入命中日志共 2 次，**两次都只命中 `QueryRecognitionProjectConfigurationList`**（17:32:36 读取分支、17:32:56 写后刷新分支） |

### 受控构造方案（C5、C21）

**什么是"受控故障注入"**：在不修改生产代码、不新增生产测试入口的前提下，用浏览器/网络层或进程层手段**只让目标请求失败**，以触发页面既有的失败恢复分支，验证完成后立即恢复。目的是验证"失败路径"的真实行为——这些分支在正常业务下不可达，但正是设计明确要求的（`Client/design.md` 第 11、13 行）。原理由：规范禁止新增生产测试入口，故只能用外部可控手段，而非在代码里加开关。

**C21 实际执行方式（2026-09-15，与初版方案的差异已记录）**：初版方案用受管 Chrome 的原始 CDP `Fetch.enable` 拦截。实测本任务的 Chrome DevTools MCP 自管 Chrome 以 `--remote-debugging-pipe` 启动（`--user-data-dir=…\.cache\chrome-devtools-mcp\chrome-profile`，命令行无 `--remote-debugging-port`），浏览器侧没有可连接的 TCP 端口，配套 CLI（默认 `127.0.0.1:9222`）无法接入同一实例；另起 CLI Chrome 会得到**另一个浏览器**，其登录态与业务链路与本次验收对象不同，反而降低证据可信度。因此改用等价且更精确的做法：用 CDP `Runtime.evaluate` 在**子应用所在 iframe 的 `window`** 上安装一次性钩子，只让 URL 含 `QueryRecognitionProjectConfigurationList` 的请求抛出 `TypeError`（请求根本不发往网络），其余请求（含写请求、目录查询）放行。做法要点：①micro-app 以 iframe 方式承载子应用，钩子必须装在 iframe 的 `window` 上——装在宿主顶层 `window` 时**零命中**（实测 `faultHits=0`，列表照常刷新），这是本次踩到并纠正的点；②`@dy/kiota-middleware` 的终端请求是运行时解析的裸 `fetch(...)`，因此在适配器创建之后安装钩子仍然生效；③钩子为**一次性**（命中即自动解除），验证后用 `原始引用还原 + 删除全部标记位`，实测 `window.fetch === 原始引用`。

| 用例 | 目的 | 具体做法（三步内，可回退） | 协作需求 |
|---|---|---|---|
| **C21** 写成功但刷新失败 | 验证"刷新失败**不改判为写入失败**、页面进入阻断态、允许重试、且阻止继续写入" | **已执行**（2026-09-15）：①在子应用 iframe 安装一次性列表请求钩子（机制见上）；②页面正常提交一次"修改可互认时间"（写请求放行、随后的列表请求被注入失败、目录查询仍成功）；③观察提示与阻断态后点「重试」恢复，并还原钩子；另以只读「重新读取」分支做对照 | 不需要：仅浏览器侧拦截，不动后端与数据；实际只改了 `123` 的时间值，验证后已还原 |
| **C5** 两页面并发重复 | 验证"后到的重复提交收到统一业务错误、表单与弹窗保留、列表不因失败刷新" | **已执行**（2026-09-15）：①同一宿主开两个标签，均先载入列表与选项集；②A 标签新增项目 X（成功）；③B 标签用其**载入时快照**中的同一项目提交 → 收到「业务拒绝：该组织已配置此标准项目。」并保留表单、零刷新请求 | 不需要：纯粹是两个宿主标签的正常操作 |
| **C17** 组织切换后目标组织读取失败 | 验证切换目标组织加载失败时"不进入目标组织可操作状态、不误用旧组织数据" | **真实宿主页面层本环境无法构造**：需要两个授权组织（组织信息由组织管理系统提供，本环境无法新增），切换动作由宿主重新签发 token 完成。曾被提议的替代构造（在子应用内伪造可信组织 + 借服务端跨组织拒绝制造目标读取失败）需**篡改共享登录存储**，不予采用。**采纳证据**：组件层自动化用例「C17：目标组织加载失败时不进入可操作状态，也不展示旧组织数据」（`RecognitionProjects.test.tsx`，切到另一个组织后令目标组织读取失败 → 显示「互认配置数据待刷新」、旧组织数据不再出现、`新增配置` 禁用）。**状态**：按 Testing Baseline §3 整条记 `Blocked`；受阻原因登记为决策标签（**接受口径待负责人确认**），不改变状态 | 不需要你配合（受阻原因登记为决策标签，接受口径待负责人确认） |

### 未执行与边界

| 项 | 原因 |
|---|---|
| C17 组织切换后目标组织读取失败 | 真实宿主页面层需第二个授权组织；组织信息由组织管理系统提供，本环境无法新增。受阻原因登记为决策标签（接受口径待负责人确认）；按 Testing Baseline §3，本条整条如实记 `Blocked`（计入 52 条分母并单列） |
| V24 事件登记/处理/提交的可见性 | 属框架行为、无可控观察点；数据库写入与失败响应面已实测通过。受阻原因登记为决策标签（接受口径待负责人确认）；按 Testing Baseline §3 整条记 `Blocked`（计入 52 条分母并单列） |
| V26 并发停用的事件计数子面 | 终态与幂等面已实测（两个并发请求均成功、恰好一次状态变化），"事件恰好登记一次"的计数子面无可控观察点。受阻原因登记为决策标签（接受口径待负责人确认）；整条记 `Blocked`（计入 52 条分母并单列） |
| V27 跨组织写配置 ID 的拒绝 | 本环境仅单一可信组织，构造不出"第二个可信组织持有配置"的合法前置；离线用例覆盖拒绝分支、查询侧越权拒绝已真实验证。受阻原因登记为决策标签（接受口径待负责人确认）；整条记 `Blocked`（计入 52 条分母并单列） |
| 数据状态 | 验收产生的配置：`123`（启用、60 天）、`222`（启用、7 天）、`S1-RETRY-ITEM-20260914-002`（启用、20 天，C5 首次提交）、`S1-D29-ITEM-20260915-001`（启用、15 天，C21 预演时的一次正常新增）；目录分类、分组、项目均已恢复启用；`123` 在 C21 中改为 `45` 后已改回 `60`。**阶段2 无删除能力**，如需清理另行安排（2026-09-16 重取后追加：`S1-RETRY-ITEM-20260914-001`（启用、30 天），C5 重取时的首次提交） |


## 批次A/B 整改后的宿主重取（2026-09-16）

**为什么重取**：2026-09-16 实施了两批整改（批次A 后端契约与口径 8 项、批次B 前端质量 8 项，另含主线程复核时发现并修复的 B2 入口门控缺失与 B4 antd 弃用 `rowKey` 参数两处）。按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §5「生产代码再次变化时，与变更影响重叠的证据失效并重跑目标、直接影响、受影响项目和适用运行验证」，上表 2026-09-15 的宿主证据对**受影响用例**失效，本节即为重取结果；未受影响项按下文"证据失效与重取"复用。

**执行环境（与 2026-09-15 同链路）**：宿主 `http://183.224.180.166:35000/login` → 入口「检验检查结果互认平台」→ 菜单「互认项目」；后端为批次A 后**重新构建并重启**的 `localhost:15014`（受管后台作业），前端 `localhost:3008`（受管后台作业）；Chrome DevTools MCP；劫持生效证据：`reqid=47 GET localhost:3008/subApps/medical-recognition/recognition-projects [200]`、`@vite/client [200]`、`src/main.tsx [304]`。**环境项登记**：执行时段为 2026-09-16 当日；宿主入口、登录入口与菜单项同上；账号沿用该链路（宿主账号 `yangkj`，宿主显示名与权限位、密码见"Ticket 08 宿主验收执行结果"的"环境与进程"表与 `.agents/instructions/test-environment.md` 第 1 节，本报告不复制凭据）。**证据边界**：本节未单独登记浏览器版本与会话标识、控制工具版本与逐条请求的绝对时刻（沿用同链路的受管 Chrome DevTools MCP 实例，未固定 Profile）；需要这些维度时按"Ticket 08 宿主验收执行结果"的"环境与进程"表核对。

| 用例 | 重取证据（批次A/B 后代码） |
|---|---|
| C12/C13 | 菜单进入 + 进入即自动加载 `QueryRecognitionProjectConfigurationList [200]`（`reqid=106`）+ `QueryEffectiveMedicalStandardCatalog [200]`（`reqid=107`）；表格 9 列、5 行；**读取在途窗口内 `重新读取互认配置` 与 `新增配置` 均为禁用态**（批次B 的 B2 修复在真实宿主生效，旧代码该窗口为可用） |
| C4 非法分支 | 提示语为新的「可互认时间为 1 到 2147483647 之间的整数天数」、字段 `invalid=true`、弹窗与两处输入保留、**网络面板无任何写请求**（B3 上界与文案在宿主生效） |
| C4 合法分支 | `CreateMutualRecognitionItem [200]`/`true`（`reqid=111`），请求体 `{"recognitionDurationDays":30,"standardProjectCode":"S1-RETRY-ITEM-20260914-001"}`（**无组织编码、无内部 ID**）；随后 `QueryRecognitionProjectConfigurationList [200]`（`reqid=113`）+ `QueryEffectiveMedicalStandardCatalog [200]`（`reqid=114`）——列表与选择数据均重载；新行 `30 天 / 启用` |
| C5 | A 标签（先载入）创建成功后，B 标签凭**载入时快照**中的同一项目提交：`CreateMutualRecognitionItem [500]`（`reqid=88`），异常链 `InvalidOperationException: 业务拒绝：该组织已配置此标准项目。 → DuplicateMutualRecognitionItemException → Npgsql.PostgresException 23505: 重复键违反唯一约束"ux_mrec_mutual_recognition_org_project"`（表 `mrec_mutual_recognition_item`）；页面告警「**业务拒绝：该组织已配置此标准项目。**」；**弹窗保留且项目与天数两处输入原样保留**；**失败后零刷新请求**（B 标签最后一次请求即该 500，列表仍显示 4 行而真实组织已 5 行） |
| C6/C20 | 修改弹窗回填当前值 `60`，项目类型/分类分组/配置状态由适配层统一映射出口渲染；**同值直接保存** → `UpdateMutualRecognitionItemConfiguration [200]`/`true`（`reqid=117`，请求体 `{"id":"3a23b834-…","recognitionDurationDays":60}`）+ 列表与目录重载（`reqid=119/120`） |
| C8 | 确认框「取消」后**零写请求**（网络请求数不变）；确认停用 → `DisableMutualRecognitionItem [200]`（`reqid=123`）+ 重载，行内入口转为**反向**入口「启用配置：222（当前停用）」；确认启用 → `EnableMutualRecognitionItem [200]`（`reqid=129`）+ 重载 |
| C7 | 配置停用后 `修改可互认时间` 入口仍可用、同值提交成功（`UpdateMutualRecognitionItemConfiguration [200]`，`reqid=234`），行状态保持 `停用`、目录停用原因保留 |
| C9 | 停用分组后：列表与**修改弹窗内**均显示「**所属分组已停用**」（同名的另一个分组记录停用不影响本行，证明原因按记录而非按名称）；`修改时间` 成功（`reqid=222`）；`停用` 成功（`reqid=228`）；`启用` → **`EnableMutualRecognitionItem [500]` +「业务拒绝：所属分组已停用。」** 且**确认框保留**（操作入口未消失）；失败后确认按钮**未停留在提交态**（DOM 无 `ant-btn-loading`、取消按钮恢复可用）；恢复分组并重读后原因回到 `—`、配置恢复启用 |
| C21 | 浏览器侧一次性注入只让列表刷新请求失败：写请求 `UpdateMutualRecognitionItemConfiguration [200]`（`reqid=135`）、`QueryEffectiveMedicalStandardCatalog [200]`（`reqid=136`）、**列表请求未发往网络**；页面显示「**写入已成功，数据待刷新**」+「重试」、**无任何写失败提示**、弹窗按成功路径关闭、`新增配置` 与全部行内操作禁用、5 行保留；点「重试」后列表回读成功、告警消失、入口恢复；命中日志 1 次且 URL 仅命中 `QueryRecognitionProjectConfigurationList`；钩子已还原（实测 `fetch === 原始引用`） |
| V19 | 用页面真实 token 提交非可信组织：`POST QueryRecognitionProjectConfigurationList`（`reqid=142`，请求体 `{"organizationCode":"999"}`）→ **`500`「请求组织与当前登录组织不一致，不能查询该组织的互认配置。」**，抛出位置为 `MedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync`（批次A 的 A3 改动的同一方法）——**拒绝而非降级为空集合** |
| V22 | token 载荷实测 `org:"01"`、`sub:"medical-recognition"`；查询请求体仅 `{"organizationCode":"01"}`（页面无组织输入） |
| V2/V15/V24 | 由上述真实链路覆盖：重复提交 500 + 唯一约束翻译且无脏写（C5）；写入 `200` + 回读一致（C4/C6/C7/C8）；失败响应 500 与异常传播（C5/C9） |

**证据失效与重取（按 Testing Baseline §5 逐项交代）**

下表记录的是 2026-09-16 批次A/B 整改当轮的处置；表中引用的门禁数字、文件与用例计数器都是**各端最近一次适用轮次**的值，不表示某一轮同时重跑过全部证据（各端最新门禁见"门禁终值"节）。

| 处置 | 项 | 说明 |
|---|---|---|
| **重取** | C4、C5、C6、C7、C8、C9、C12、C13、C20、C21 的 Host 面；V2、V15、V19、V22、V24 的真实运行面 | 这些用例的证据与批次A（A1 请求约束、A3 组织口径、A8 领域清理）或批次B（B1–B8，含弹窗拆分与入口门控）改动的代码路径重叠 |
| **复用（未受影响）** | V18、V25（真实库元数据）；C22、C24（Client 契约与生成入口）；C1-C3、C10、C11、C14-C19、C23 的组件/适配层面 | 未改数据库结构、未重生成 Client；**组件层证据的复用理由与影响面**：批次B 重写了页面与弹窗、主线程另修了入口门控与表格行标识，因此这批用例**在批次B 当轮由测试套件整体重跑覆盖**（该轮门禁 99 passed / 2 skipped，见下方数字），其中组织切换后目标组织读取失败（C17）的组件用例同轮重跑通过，故其组件层证据按"重跑后复用"处理，而非沿用 2026-09-15 的旧执行 |
| **不适用或受阻（如实记录）** | V10（`N/A`）；组织切换后目标组织读取失败的宿主页面层（C17）、事件可见性（V24）、并发停用的事件计数子面（V26）、跨组织写拒绝（V27）→ 整条 `Blocked` | 这四条的受阻原因登记为决策标签（**接受口径待负责人确认**）；按 Testing Baseline §3 不改变状态、决策标签不计入统计（4 条 `Blocked` 计入 52 条分母并单列）；本次代码改动不改变其边界 |

**整改后的门禁复跑（主线程执行，非转述）**

| 门禁 | 结果 |
|---|---|
| 后端测试 | `dotnet test …Tests.csproj --no-restore` → **80 通过 / 0 失败 / 0 跳过**（整改前 75；新增 5 个断言用例） |
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx --no-restore` → **0 错误**（5 条 CRLF 警告） |
| 前端测试 | `pnpm -F dy-medical-recognition test` → **99 passed / 2 skipped（101）**，5 文件（该轮时点的口径；批次11 后为 6 文件 105 passed / 2 skipped（107），见"枚举中文来源"节），**Errors 3**（均为既有同因：拒绝态 promise 交宿主展示、页面按规范不捕获；`dangerouslyIgnoreUnhandledErrors` 为既有开关，未扩大范围）；本轮由 85 passed / 2 skipped（87）增至 99 passed / 2 skipped（101），**净增 14 条**（批次B 与主线程复核补入的用例与断言补强，另删除 1 条与既有重复的用例） |
| 前端构建 / lint | `build` exit 0；`lint` **0 error / 3 warning**（同既有 react-refresh 警告） |
| `git diff --check` | exit 0 |
| 已知残余 | vitest unhandled errors 由 2 条增至 3 条（同一根因：拒绝态 promise 交宿主展示，页面按规范不捕获；`dangerouslyIgnoreUnhandledErrors` 既有开关，未扩大范围）——与 2026-09-15 登记项同类 |


## 整改后复审与收口前清理（2026-09-16）

### 复审组织方式

7 个只读独立代理并行复审，覆盖：①后端架构与规范合规；②后端 C# 与测试规范；③前端规范合规；④后端代码质量（Standards 轴 + 坏味道基线）；⑤前端代码质量（同上 + React 要点）；⑥Spec 轴（设计/票据/SRS-UML）；⑦文档与证据合规。每个代理都被要求先读 `AGENTS.md` 与自身角色规范，逐项给"符合/不符合/不适用"，finding 必须带 `文件:行`、规范条款、证据与最小修正，区分硬违规与判断题，零发现写"零发现"，并对上一轮同域 finding 逐条给闭合状态。**明确排除**（下列改动均**不属阶段2 交付面、不得随阶段2 一并提交**）：阶段3 计划文档（`docs/plans/005-阶段3-互认项目金额维护/**`）；总体计划（`docs/plans/001-总体计划/design.md`、`impl.md` 与 `docs/plans/README.md`）；`.agents/instructions/**`（`backend-architecture.md`、`planning-and-evidence.md`、`project-context.md`、`test-environment.md`）；`AGENTS.md`；`CONTEXT.md`；`docs/需求规约SRS.md`；`docs/uml/**`（本阶段口径以外的 UML 改动）与两张流程图（`docs/流程图.md`、`docs/业务流程图.md`）；以及新增全局规范条文本身（仅要求回答该条文是否适用于阶段2）。

### 复审结论（主线程逐条复核后的口径）

- **0 Blocker、0 实现层硬违规**。新增全局规范 §5.3「批量读取与 N+1 禁令」：阶段2 查询实现**不违反**（互认配置列表为单条多表 JOIN 一次取回 + 应用层内存映射；写路径为固定按键 3 次读取，与行数无关）。
- **主线程复核判定为误报 2 条**：①"`Stage1ArchitectureTests` 依赖 `GetMethods()` 未定义顺序"——该处已有 `OrderBy(..., StringComparer.Ordinal)`，两侧同序；②"`Server/impl.md` 计数自相矛盾"——该行明写"后端矩阵 27 条中 23/3/1"，23+3+1=27 自洽（"4"是含前端的整阶段口径）。
- **复核判定为部分成立 2 条**：①"写失败对用户与宿主都不可见"——实测反例存在（宿主验收中页面弹出中间件清洗后的业务拒绝文案），成立的部分是 `void onSubmit()` 让拒绝脱离 antd 路径、测试靠既有开关掩盖（已登记残余的第 3 个实例）；②"补的两条新增用例逐行同义"——其中"切换组织后保持禁用"确与既有用例重复（已删除），"读取完成前保持禁用"断言的是**首帧**，旧代码下会失败（即当时的 RED），不是重复。
- **顺带更正一条上轮判定**：Spec 轴独立核实"创建请求纯空白编码会穿透"**不成立**（`[Required]` 本就拒绝纯空白），批次A 的观察正确；该轮新增 `[NonEmpty]` 的真实增量是拒绝 `Guid.Empty`。
- **判定为与项目既有约定冲突、按约定保留 1 条**：`MedicalRecognitionReportManager` 创建路径的 `configuration.IsValid = true` 曾被报为"死写"。实测该值是聚合初值，被"创建必启用"用例观察；而落库启用状态按项目既有约定（与标准目录三张表一致、并由两处用例固定）在新增语句里写成常量、不接请求参数。故**不按该 finding 修改 SQL**，仅在注释中说明两处表达的是同一业务事实。

### 收口前清理（本阶段自主执行，全部为小改动）

| 项 | 内容 | 门禁 |
|---|---|---|
| 目录就绪判定（前端） | 新增 `catalogLoaded` 标记：把"读取成功但目录为空"（服务端契约允许）与"还没读到"分开，避免新增入口永久禁用且无提示无重试入口；补"读取成功但目录为空时入口可用"用例，并**实测该用例在旧判据下失败**（RED） | 前端 99 passed / 2 skipped |
| 组织形式清理 | 查询侧比较与下传统一使用去空白后的可信值；请求侧同样裁剪后比较，消除"写入成功、查询被误判越权"的读写不对称；补"请求组织与可信组织同为带空白值时仍可查询"用例 | 后端 81/81 |
| 事件可见性口径落地 | 事件登记/处理/提交的可见性（V24）整条 `Blocked`；同步阶段根 `impl.md`、`Server/impl.md`、`Server/design.md`、`Server/取证-组织与事件机制.md`、`Client/impl.md`、`Ticket 08`、`Client/Pages/**`、结果矩阵与统计（47/4/1） | 文档检查 0 断链 / 0 列数错误 |
| 注释与文档 | 两个启停请求补类型级 XML；查询接口 summary 改"四项目录查询 + 互认配置列表查询"；新解析点登记进 `Server/design.md` 类型归属表；报告内 12 处失效措辞/行号/计数/勾选项更正；`test-environment.md` 去掉阶段用例号引用并统一"待配置"计数 | 同上 |
| 证据形式 | 互认配置列表查询补"两行结果仍只查询一次"的调用次数断言与 `$IsValid` 谓词引用断言，对齐 §5.3 的验证要求 | 后端 81/81 |

**结论**：清理后门禁为**后端 81/81、前端 99 passed / 2 skipped（101）、构建 0 错误、lint 0 error、`git diff --check` exit 0**；受影响链路（列表查询与越权组织拒绝）已在真实宿主链路复验（`QueryRecognitionProjectConfigurationList [200]`、`QueryEffectiveMedicalStandardCatalog [200]`；非可信组织查询 → `500`「请求组织与当前登录组织不一致，不能查询该组织的互认配置。」）。矩阵状态未因清理改变。

### 复审期间的工作区事故（如实记录）

Spec 轴代理违反只读约束执行了 `git checkout -- server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs`，**覆盖了批次A 在该文件的未提交改动**（无 blob 可恢复），随后按事故前的 `git diff` 自行重建。主线程复核：重建内容为**5 条旧聚合键 `DoesNotContain` + 7 条肯定断言 + 阶段2 注释段**，与既有两轮评审引用完全一致；该文件的两个私有助手在 `HEAD` 中本就存在且本次 diff 未触及；后端测试 **81/81 通过**。残余风险：注释措辞无法与事故前版本逐字节比对（只能凭事前引用确认实质）。**该事故说明阶段2 改动长期未提交存在真实风险。**


## 第三轮复审后的修复（批次C/D/E，2026-09-16）

### 复审与处置口径

收口前清理完成后又执行了一轮复审，方式与前两轮相同。7 个只读独立代理并行复审（后端架构、后端 C# 与测试、前端规范合规、后端代码质量、前端代码质量、Spec 轴、文档与证据），**复审期间未改动任何代码或文档**（修订版全程冻结）。共报 **26 条 finding**（后端 10、前端 4、规格 7、文档与证据 5），另有 1 项事实记录（`client/packages/api-client-medical-recognition/src/.kiota.log` 的版本控制状态）。

主线程逐条对照工作区原文复核后判定：**成立 19 条、部分成立 5 条、误报 2 条**。两条误报：①"`testReport.md:173` 的 80/80 与清理后 81/81 冲突"——两处都是该轮的历史门禁且明写"整改前 75"，`81/81` 是该轮清理节登记的门禁值（`Server/impl.md` 的 B7 行按批次表收敛只保留批次结论与指向，不再复述数值），时序自洽；②"前端 lint 不可执行（`eslint` not recognized）"——在 `client/` 根目录重跑为 **0 error / 3 warning、exit 0**，属命令执行目录问题。**0 Blocker、0 实现层硬违规**；整改范围为批次C+D+E。

### 修复内容

| 批次 | 项数 | 内容 |
|---|---|---|
| C（后端） | 4+1 | ①`MedicalRecognitionReportQueryRepository.MapConfigurationStatusToIsValid` 的两个 `<remarks>` 合并为一个并移到 `<param>`/`<returns>` 之前；②删掉 `Stage2QueryTests` 中紧跟 `Assert.Single` 的恒真 `Assert.NotNull`，改为断言返回项编码；③`Stage2WritePathTests` 补创建语句常量断言 `Assert.Contains("true", statements["CreateMutualRecognitionItem"])`——此前把 `MutualRecognitionItem.xml` 新增语句里的 `true` 改成 `false` **不会让任何用例失败**（探针只遍历三个目录 XML，互认语句只断言了列名与"不引用 `$IsValid`"）；④按事实改写"新增配置状态时编译期即暴露"的注释（本项目未开启 `TreatWarningsAsErrors`，只产生 CS8509 告警，真正的兜底是运行期 `SwitchExpressionException`）；另去掉 `QueryRecognitionProjectConfigurationListAsync` 请求组织上冗余的 `?.` |
| D（前端） | 3 | ①页面行内动作/状态文案改由适配层唯一出口 `configurationStatusActionText` / `configurationStatusText` 构造，删掉页面自拼的 `CONFIGURATION_STATUS_TEXTS[2]/[1]` 与 `'配置状态未知'` 字面量；②`recognitionProjectsApi.test.ts` 补 `configurationStatusActionText` 双分支与"与状态文案同源"断言，页面用例补"行内可访问名称与适配层出口同源"断言；③删除与既有用例重复的 B2 用例（其首帧断言在新旧判据下同为禁用、不具区分力，真正的 RED 是"读取成功但目录为空"用例） |
| E（文档与口径） | 8 | ①`Server/design.md` 查询链改"等于可信当前组织（两侧按去首尾空白比较）"；②同文件 V24 行的"预期结果"恢复设计期要求、执行状态指向该节"运行面状态"说明；③`recognitionProjectsApi.ts` 筛选注释改"字面包含（区分大小写）"；④页面「立即切换」改「放弃并切换」；⑤`Ticket 08` 补**事件登记/处理/提交的可见性（V24）**、**并发同方向停用时"恰好登记一次事件"的计数子面（V26）**、**跨组织写配置 ID 的拒绝（V27）**的例外注；⑥报告与票据中的失效行号引用改用例名（3 处 C17 引用原写 `RecognitionProjects.test.tsx:382`；真实用例见 `RecognitionProjects.test.tsx` 的「C17：目标组织加载失败时不进入可操作状态，也不展示旧组织数据」；C23 行的 4 个适配层行号同样已失效）；⑦`Ticket 01` 的 V24 用词改为 Testing Baseline §3 口径；⑧页面状态机补 `catalogLoaded` 门控与"读取成功但目录为空仍就绪"口径（`Client/Pages/**`），并在 `Client/design.md`、`Ticket 04` 写明"编码与状态筛选由页面在已读取结果上本地执行、不下推"口径 |

### 新增断言的静态证据（批次C 第③项）

按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.4 与 `AGENTS.md` 第 2 节闸门 4，静态 finding 用静态证据、不伪造行为 RED。对 `MutualRecognitionItem.xml` 的 `CreateMutualRecognitionItem` 语句按测试同一取法（`XElement.Value`，注释不计入文本）核对：语句文本 481 字符、`true` 出现 **1 次**（即新增语句里的常量）、`false` **0 次**，注释中的"新增固定写入 true"**不计入** 该文本；把该常量替换为 `false` 后 `Assert.Contains("true", …)` 即为假——即新增断言对该退化确实会失败，不再全绿放行。

### 登记为残余、本轮不改（附理由）

| 项 | 理由 |
|---|---|
| 事件观察依赖反射写框架 `EventBusFactory.CurrentEventQueue` 内部 setter | 框架无公开测试钩子；类级 remarks 已说明无公开替代，非本轮新增；改动需框架侧支持 |
| 互认列表查询的 `$IsValid` 只有静态谓词/映射证据与阶段1 同类语句的真库证据 | 页面按设计不下推状态筛选，该分支正常业务不可达；已由映射方法三取值单测 + 谓词与 `ParameterMap` 断言覆盖。按不可达分支处理，不补真库探针 |
| `MapConfigurationStatusToIsValid` 为 `public static` | 项目无 `InternalsVisibleTo`，降可见性需改项目文件/公共契约，收益低于代价 |
| 筛选值属性名 `IsValid` 与投影列 `ConfigurationIsValid` 近义 | 已有注释说明；改名会牵动 XML 参数名 |
| `StandardProjectCode` 不做 Trim | 与阶段1 逐字符一致的既有约定 |
| 创建/查询请求字符串字段上同时有 `[Required]` 与 `[NonEmpty]` | `[Required]`（`AllowEmptyStrings=false`）本就拒绝空串与纯空白，两处对字符串属行为重复；`[NonEmpty]` 的真实增量只在三处 `Guid` 的 `Guid.Empty`。按 Low 保留（删除会削弱显式约束表达，无行为收益） |
| 手写数值枚举镜像（`[0,1]`/`[1,2]`） | 原根因的平台级生成缺陷已于 2026-09-16 立项修复（OpenAPI 现带 `enum`/`x-enumNames`/`x-enumDescriptions`，生成端为 `number \| null`）；页面仍保留本地常量作为筛选项与文案来源，故取值须继续与后端枚举一致，由回归用例守卫 |
| 冻结 OpenAPI 与真实后端输出的一致性 | 由生成入口每次真实下载 + 归一保证；测试只能对账已提交文件，无法在离线用例里重跑后端。真实输出与冻结文档的对账按宿主实测人工步骤执行（`prepare-openapi` 输出的 path 数与两类修正计数逐次记录在本报告各轮小节），不假装为自动化守卫 |
| `RecognitionProjects.tsx` 的 `columns` 每次渲染重建；筛选区状态列宽固定 `minmax(200px, 260px)` | 本轮之前既有实现；`rows`/`tableRows` 已 memo，未观察到列处理成为瓶颈；枚举中文若超过 4 字的溢出属未证实猜测，需实测后再改 |
| 阶段1 只读模型上的枚举（`MedicalStandardUsageStatus`、`LaboratoryResultType`、`MedicalReportType` 等）没有服务端中文、未登记进枚举契约 | 本轮枚举契约覆盖范围限定为阶段2 契约；阶段1 页面文案仍为前端本地实现，扩展需同时迁移阶段1 契约与页面，故按残余登记，待阶段1 页面下次改动时一并设计。**追记（2026-09-17）：`MedicalStandardUsageStatus` 这一项已消除**——阶段1 按 `S1-D32` 完成迁移（登记 `[EnumDescriptor]`/`[Description("未使用")/("已使用")]`、只读模型交付 `ItemTypeText`/`UsageStatusText`、页面改取服务端随行文案，阶段1 矩阵新增 `C75`）；`LaboratoryResultType`、`MedicalReportType` 等后续阶段枚举仍按各自阶段处理 |
| `scripts/prepare-openapi.mjs` 的归一只有 stdout 计数、无单测 | 该脚本位于生成包内且包无测试设施；真实对账依赖每次生成时的输出留档（见"增量复审与整改"的部分成立项） |
| 子应用类型解析依赖被 gitignore 的包 `dist`，`client` 根的 `build` 只构建 apps | 干净检出或只改 `src` 未重建包时会失败或按旧契约通过；验证路径固定为"先 `pnpm -F @dy/api-client-medical-recognition build`，再跑子应用 `build`/`test`"，本轮按此顺序执行 |
| `.kiota.log` 的删除是全工作区唯一暂存变更，其余改动未暂存 | 提交时必须用 `git add -A`（或分层 `git add`），否则只提交该删除；规则本身已在 `.gitignore:213` 生效 |

### 门禁（修复后重跑）

| 门禁 | 结果 |
|---|---|
| 后端测试 | `dotnet test …Tests.csproj --no-restore` → **81 通过 / 0 失败 / 0 跳过** |
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx --no-restore` → **0 错误**（6 条警告全部为无关文件的 CRLF 提示，无 CS 警告） |
| 前端测试 | `pnpm -F dy-medical-recognition test` → **99 passed / 2 skipped（101）**、5 文件（该轮时点；批次11 后 6 文件 105/2）、`Errors 3`（既有同因）；本轮删 1 条重复用例、补 1 条出口用例，总数不变 |
| 前端构建 / lint | `build` exit 0；`lint` **0 error / 3 warning**（同既有 react-refresh 警告） |
| `git diff --check` | exit 0 |

### 宿主重取（Testing Baseline §5）

本轮改了查询应用层（去冗余 `?.`）与页面渲染（动作/状态文案、切换按钮文案），受影响路径的证据在真实宿主重取：

| 项 | 命令/操作与结果 |
|---|---|
| 页面链路 | 宿主页面重载后：`QueryRecognitionProjectConfigurationList [200]`、`QueryEffectiveMedicalStandardCatalog [200]`；列表 5 行、组织编码 `01`、`新增配置` 可用；行内动作按钮的可访问名称为「停用配置：<项目名>（当前启用）」，与适配层唯一出口的构造结果一致 |
| 越权组织拒绝 | 同一宿主 token 提交 `{"organizationCode":"ORG-B"}` → **`500`**「请求组织与当前登录组织不一致，不能查询该组织的互认配置。」（异常抛自 `MedicalRecognitionReportQueryAppService.QueryRecognitionProjectConfigurationListAsync`）；提交 `{"organizationCode":"01"}` → **`200`** 且返回 5 条 |
| 未构造项 | 「组织待切换」提示条文案的宿主分支需宿主重新签发 token 切换组织（与 C17 的 Host 面同一环境边界），未构造；该按钮文案由组件层用例「C18：组织变化遇未保存内容先确认；取消保留内容与旧组织视图，确认才清理并切换」覆盖 |

**结论**：本轮修复**未改变任何一条矩阵用例的状态**（仍为 `Passed` 47 / `Blocked` 4 / `N/A` 1）；修复后门禁为后端 **81/81**、前端 **99 passed / 2 skipped（101）**、构建 0 错误、lint 0 error、`git diff --check` exit 0。阶段是否收口由本报告的收口节结论承载，本节不代作结论。

## 枚举契约缺陷修复（跨阶段立项，2026-09-16）

### 立项与根因

按同组织 `Dy.LisCenter` 的**前后端枚举交互方式**实施。

根因（此前一直登记为"待独立立项"的平台级缺陷）：ASP.NET 只为枚举生成 `{"type":"integer"}`，不含取值集合；Kiota 对**可空**枚举属性再遇到 `oneOf [null, $ref]` 时退化为空对象类型，序列化走 `writeObjectValue`，把请求体写成 `{}`。缺陷期实测：`serializeRecognitionProjectConfigurationListQueryRequest(writer, { organizationCode:'ORG-A', configurationStatus:1 })` → `{"configurationStatus":{},"organizationCode":"ORG-A"}`。

### 修复内容（三层，与 LisCenter 同口径）

| 层 | 位置 | 内容 |
|---|---|---|
| 服务端声明 | `Domain.Share/Enums/ConfigurationStatus.cs`、`MedicalItemType.cs` | 两个枚举加 `[EnumDescriptor]`，每个成员加 `[Description]`（启用/停用、检验/检查）→ SourceGen 生成 `XxxDescriptorList` |
| 服务端契约 | `Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs`（新增）、`Application/MedicalRecognitionEnumOpenApiDocumentTransformer.cs`（新增）、`MedicalRecognitionApplicationModule.OnPostConfigureServices` | 注册表集中登记对外枚举描述器；文档转换器把 `enum`、`x-enumNames`、`x-enumDescriptions` 写进 OpenAPI；模块注册使框架 OpenAPI 管线调用它 |
| 生成入口归一 | `client/packages/api-client-medical-recognition/scripts/prepare-openapi.mjs` | 新增 `oneOf [null, $ref]` → `$ref` 归一（与 LisCenter 生成入口一致），使可空枚举属性生成为 `number \| null` |

**修复后实测（真实后端 `/openapi/v1.json`）**：`ConfigurationStatus` = `{"enum":[1,2],"type":"integer","x-enumNames":["Enabled","Disabled"],"x-enumDescriptions":["启用","停用"]}`；`MedicalItemType` = `{"enum":[0,1],…,"x-enumDescriptions":["检验","检查"]}`。冻结输入文档同步为 22 path（该数字是**批次10 当时**的口径，批次11 新增 `/Api/EnumMetadata/GetEnumMetadata` 后为 23 path、int32 归一 4 处，见下节"枚举中文来源"）、修正整数联合 3 处、修正可空引用 3 处。

**生成端修复后**：`configurationStatus?: number | null`、`itemType?: number | null`，序列化改为 `writer.writeNumberValue("configurationStatus"…)` / `writeNumberValue("itemType"…)`，缺陷期的 `ConfigurationStatus`/`MedicalItemType` 空对象类型与 `…_configurationStatusMember1` 回退类型全部消失；`kiota-lock.json` 仍指向包内稳定 OpenAPI 且保留 `excludePatterns: ["/auth/login"]`。

### 影响面

- **阶段2**：`RecognitionProjects` 页面与适配层无需改动即通过编译与全部前端用例；`configurationStatus` 不再可能被写成 `{}`。
- **阶段1**：同一枚举的查询/只读属性同样从空对象类型修正为数值；阶段1 的写请求在 2026-09-15 宿主证据中线上即为数值（如实记录，未把阶段1 写路径记为曾受影响）。
- **筛选口径不变**：配置状态筛选仍不下推服务端——这是业务口径（下推会缩小读取范围、破坏新增排除集合的完整性），与本次契约修复无关；适配层注释已按此更新。

### 回归守卫（新增 8 条静态用例）

`server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs`：①两个枚举源码启用 `[EnumDescriptor]` 与成员中文说明；②两个程序集含生成的 `XxxDescriptorList`；③注册表恰好登记这两个枚举且取值/名称/中文与业务一致（只守"不得多登记、不得漏掉这两个、顺序与取值稳定"，**不检测新增枚举漏登记**——该场景由阶段2 契约类型扫描用例守卫，见下节）；④转换器为已存在架构补齐取值与说明且不改无关架构、不新增架构；⑤冻结的 OpenAPI 同时具备取值集合与归一后的 `$ref`（任一环节回退即失败）；⑥生成物按数值读写且无按上下文命名的回退类型。

**追记（2026-09-17）**：本节描述的是当轮的**两个**枚举；注册表与上述守卫已随阶段1 枚举协作契约（`S1-D32`）扩展为**三个**——程序集描述器列表、转换器补齐取值/名称/中文、冻结契约取值与文案、生成端按数值读写、注册表取值/名称/中文断言的 Theory 数据均已补入 `MedicalStandardUsageStatus`，两处「两个枚举」的注释同步改为三个。当前口径以测试文件为准。

**静态失败基线（修复前实测，非行为 RED）**：修复前冻结文档中两个枚举为 `{"type":"integer"}`、生成物为 `ConfigurationStatus | …_configurationStatusMember1 | null` 且序列化走 `writeObjectValue`——上述 ⑤⑥ 的断言在修复前必然失败。按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.4 与 `AGENTS.md` 第 2 节闸门 4，此类静态 finding 以静态证据记录，未伪造行为 RED（编译失败与断言失败都不算行为 RED）。

### 门禁

后端 `dotnet build` **0 错误**、`dotnet test` **89/89**（新增 8 条）；前端 `pnpm -F dy-medical-recognition test` **99 passed / 2 skipped（101）**、`build` exit 0、`lint` 0 error / 3 既有 warning；客户端包 `typecheck`/`build` exit 0；文档 0 断链 / 0 列数错误；`git diff --check` exit 0。

### 中文来源（同日已采纳）

LisCenter 的枚举中文来源方式（后端 `xxxText` + `EnumMetadata` 查询接口 + 前端 `useEnumMetadata`）**已按同口径实施**，内容、回归守卫与门禁见下节"枚举中文来源（LisCenter 同口径，2026-09-16）"。本节不再保留未决项。

## 枚举中文来源（LisCenter 同口径，2026-09-16）

### 采纳口径与范围

采纳同组织 `Dy.LisCenter` 的枚举中文来源方式，**服务端枚举声明是中文的唯一来源**，前端不再自持文案映射。落地为三条路径（阶段批次 11 / 后端 B10 / 前端 F8）：契约文本字段、枚举元数据查询、前端 `useEnumMetadata`。设计口径见 [design.md](design.md) 的 S2-D15、[Server/design.md](Server/design.md) 与 [Client/design.md](Client/design.md)。

### 服务端交付（B10）

| 位置 | 内容 |
|---|---|
| `Application.Contracts/Queries/EnumMetadata/EnumDescriptorText.cs`（新增） | 由 SourceGen 描述列表解析中文：`Get` 严格（未定义值抛 `ExtensionException`）、`GetOrNull` 安全（未定义值返回 `null`） |
| `.../EnumMetadata/{EnumMetadataItemDto,QueryEnumMetadataRequest,IEnumMetadataAppService}.cs`（新增） | 白名单枚举元数据查询契约：请求只带 `EnumName`（`[Required]`），返回只读的 `{value,name,description}` 集合 |
| `Application/Queries/EnumMetadata/EnumMetadataAppService.cs`（新增） | 复用 `MedicalRecognitionEnumDescriptorRegistry`；未登记、大小写不符与空白一律 `ValidationException`（不降级为空集合）；结果以 `Array.AsReadOnly` 暴露 |
| `Application.Contracts/Queries/RecognitionProjectConfigurationReadModel.cs` | 补 `ItemTypeText`、`ConfigurationStatusText` 计算属性 |
| `Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs` | 注释补明"元数据查询与 OpenAPI 转换器共用同一登记表" |

**严格解析与安全解析的差异按事实登记**：`ConfigurationStatus` 由 `is_valid` 布尔派生、取值集合封闭，按严格解析；`MedicalItemType` 取自 `mrec_medical_standard_category.item_type`，该列**无 `CHECK` 约束**，按安全解析（未登记取值返回 `null`，由前端按"未知类型"展示）。若沿用严格解析，一条越界数据会让整张互认配置列表查询失败，与页面既有"未知取值安全展示"约定冲突。

### 前端取用（F8）

- `src/hooks/useEnumMetadata.ts`（新增）：按 API Client 实例 + 枚举名缓存请求（`WeakMap`），失败时清除缓存以便重试，同步异常并入失败路径；`useEnumMetadata(enumName, fallback)` 加载成功即替换，失败或返回空集合时保留兜底选项。
- 页面展示文本改取契约文本（`row.itemTypeText` / `row.configurationStatusText`）；配置状态筛选的选项标签与启停动作文案同源取自元数据；本地常量（`MEDICAL_ITEM_TYPE_TEXTS`、`CONFIGURATION_STATUS_TEXTS`）降级为兜底，不再作为展示主来源。**追记（2026-09-17）**：项目类型与配置状态的取值域和兜底文案都已抽到跨页面共享模块（`src/shared/medicalItemType.ts`、`src/shared/configurationStatus.ts`，阶段 1/2/3 共用一份），本模块按原名字转发，调用方与用例的导入位置不变；只服务本页面的选项管线（`CONFIGURATION_STATUS_METADATA_FALLBACK`、`toConfigurationStatusFilterOptions`、`configurationStatusActionText`、`configurationStatusTagColor`）仍留在本适配层。

### 回归守卫（`Stage2EnumMetadataQueryTests` 批次11 时点为 12 个用例方法、展开 27 条；同批的 `Stage2EnumContractTests` 为 9 个方法、展开 16 条；两文件在后续批次继续补守卫，当前展开口径见本报告"门禁终值"节）

`server/Dy.MedicalRecognition.Tests/Stage2EnumMetadataQueryTests.cs`：①两个枚举的元数据精确值、顺序与中文说明（期望值硬编码，不与登记注册表再比一次）；②拒绝未登记、大小写不符、空白与空请求（含精确错误文案）；③结果只读（非数组、写入抛 `NotSupportedException`）；④元数据服务只消费静态注册表（剥离注释后源码不含 `DescriptorList.List`、`System.Reflection`、`Dictionary<string, Type>`、`GetFields(`、`GetCustomAttributes(`、`Enum.GetValues(`），并有 5 条变体自校验；⑤只读模型契约文本两例；⑥越界取值：项目类型返回 `null`、配置状态抛出；⑦冻结 OpenAPI 暴露 `POST /Api/EnumMetadata/GetEnumMetadata` 且只读模型含两个文本字段；⑧生成物含 `enumMetadata/getEnumMetadata`（动词、参数前缀、返回类型与 URI 模板）与两个文本字段；⑨契约程序集全部公开类型上的枚举（含可空、数组与泛型集合元素）与白名单双向比对，阶段1 的 9 个枚举按名排除且排除清单本身校验不过期；⑩`x-enumDescriptions` 与枚举源码中的 `[Description]` 声明逐字一致。既有 `Stage2ContractTests.Read_model_matches_the_published_field_shape` 的字段集期望随契约变化同步更新（新增两个文本属性，其中 `ItemTypeText` 可空）。

**静态失败基线（非行为 RED）**：新增用例引用的类型（`IEnumMetadataAppService`、`EnumMetadataAppService`、`RecognitionProjectConfigurationReadModel.ItemTypeText`/`ConfigurationStatusText`）与冻结 OpenAPI 的 `/Api/EnumMetadata/GetEnumMetadata`、生成物的 `itemTypeText`/`configurationStatusText` 在本轮之前都不存在，故这些用例在本轮之前分别是**编译期失败**（后端）与断言失败（契约/生成物面）。按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.4 与 `AGENTS.md` 第 2 节闸门 4，静态 finding 以静态证据记录，未伪造行为 RED；本节的"基线"指静态失败，不作为行为 RED 计入。

**追记（2026-09-17）**：①元数据精确值用例的 Theory 现已含 `MedicalStandardUsageStatus`（三个枚举）；②上文的"两个枚举"在其后批次继续扩充，当前以测试文件为准；③第⑨项当时写的"阶段1 的 9 个枚举按名排除"现为 **8 个**——`MedicalStandardUsageStatus` 已登记进白名单、不再属于排除范围，排除清单"不得过期、不得侵占已登记枚举"的校验同步生效；④`OpenApi_enum_contract_matches_the_enum_source_declarations` 的 Theory 同样已含第三个枚举。

### 门禁与宿主证据

| 门禁 | 结果 |
|---|---|
| 后端构建 / 测试 | `dotnet build` **0 错误**、`dotnet test` **100/100**（批次11 时点值，本轮前 89；后续批次各自时点值见对应节，终值见本报告"门禁终值"节） |
| 前端测试 / 构建 / 静态检查 | **105 passed / 2 skipped（107）**、6 文件（批次11 时点值；批次11 新增 `useEnumMetadata.test.ts`） |
| 客户端包 | `typecheck` exit 0、`build` exit 0 |
| 文档 | 0 断链 / 0 列数错误；`git diff --check` exit 0 |

宿主实测（真实宿主菜单进入「互认项目」，2026-09-16）：页面加载即发起 `POST http://localhost:15014/Api/EnumMetadata/GetEnumMetadata`，请求体 `{"enumName":"ConfigurationStatus"}`，响应 `200` + `[{"value":1,"name":"Enabled","description":"启用"},{"value":2,"name":"Disabled","description":"停用"}]`；列表查询响应的每一行都含 `itemTypeText`（检验）与 `configurationStatusText`（启用）；页面显示 5 行、类型「检验」、配置状态「启用」；筛选切「停用」显示 0 行、重置后恢复 5 行（筛选仍在本地执行，元数据只提供文案）。验收后关闭前后端服务。

## 增量复审与整改（2026-09-16）

### 范围与结论

对「跨阶段枚举契约修复 + 枚举中文来源采纳」两个增量做只读复审（覆盖后端契约与枚举治理、应用层与只读模型、后端测试有效性、前端 hook 与适配层、页面与弹窗、生成物与契约一致性、文档与证据一致性）。复审为独立会话、只读，未修改文件、未起服务、未提交。

共报 **53 条**问题（全部条目的严重度分布为 High 3 / Medium 16 / Low 34，另有若干标注为"未证实猜测"）；下表只列择要 18 行，并标出各自严重度，不与上面的总分布逐项对应。逐条复核判定：**成立并已整改 44、部分成立 6、误报 2、需明确口径 1（阶段1 枚举是否扩展，已确定为不扩展）**。

### 已整改（择要）

| 严重度 | 问题 | 处理 |
|---|---|---|
| High | 模块的 `AddDocumentTransformer` 注册无任何守卫，删掉注册行全部用例仍绿而真实契约退回纯整数 | 新增 `Application_module_registers_the_enum_open_api_transformer` + 4 条变体自校验理论 |
| High | 冻结 OpenAPI path 计数写成 22（实际 23）：累计生成与契约核对行、批次10 小节、批次11 小节与 `Client/impl.md` 的 F1/F7 行 | 五处按批次标注（批次10 = 22、批次11 = 23）并补记净增端点数；`testReport.md:28/:32` 两处同期陈述一并标注 |
| High | `Server/design.md` 的 ReadModel 字段清单缺 `ItemTypeText`/`ConfigurationStatusText`，与同文件类型归属表和 S2-D15 自相矛盾 | 字段清单补齐并注明可空性差异 |
| Medium | 页面从未走通"筛选标签与动作文案取自枚举元数据"这条路径，改动零回归守卫 | `stubClient` 改为按用例提供显式枚举元数据桩；新增页面用例（元数据文案与本地文案刻意不同，断言筛选标签、行内可访问名称与启停弹窗标题） |
| Medium | hook 只测了纯函数，React 层与缓存生命周期零覆盖 | 新增 hook 测试 10 条（缓存层 4 条 + `renderHook` 驱动的 hook 层 6 条：成功替换、空集合保留兜底、失败后重挂载重试、两实例复用同一请求、换名不残留并支持换回、卸载后迟到响应不泄漏） |
| Medium | 页面自持"取值 + 中文"兜底选项，与适配层同源事实分叉 | 兜底常量与收窄函数 `toConfigurationStatusFilterOptions` 下沉到适配层并单测；页面只调用 |
| Medium | 冻结文档缺路径清单与全局归一守卫 | 新增 `Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes`（23 path + 无 `oneOf`/整数联合残留，递归扫描） |
| Medium | 适配层与用例仍以"枚举是空对象类型"解释不下发状态筛选，与已修复事实矛盾 | 4 处注释改为业务口径（下推缩小读取范围、破坏新增排除集合完整性） |
| Medium | `ConfigurationStatusText` 在 C# 非空、契约/生成端可空的形状差异无登记 | 只读模型 `<remarks>`、`Stage2ContractTests` 注释、`Server/design.md` 与本文登记为平台行为（与 LisCenter 同构，该模型只作响应体） |
| Low | `configurationStatusActionText` 无生产调用点（死出口） | 改为启停动作标签的生产兜底出口（`actionLabel`），不再是死代码 |
| Low | 注册表 remarks 的通则与实际覆盖不符；`Stage2EnumContractTests` 的覆盖声明大于断言 | 措辞收窄 + 新增 `Stage2_contract_enums_match_the_registry_whitelist`（扫描契约程序集公开类型上的枚举属性，双向断言并校验阶段1 排除清单不过期） |
| Low | `SingleOrDefault` 语义与 `<exception>`/`<returns>` 文档不符；诊断文案缺枚举名 | 改 `FirstOrDefault`、补齐文档、错误消息带 `typeof(TEnum).Name` |
| Low | 异常/接口 XML 文档不完整、校验器文档自述"写请求" | 补 `param`/`returns`/`exception`；校验器改为"项目请求校验入口（读写共用）" |
| Low | `Segmented` 的 aria-label 注释与依赖源码相反 | 核实 `@rc-component/segmented@1.3` 的 `_extends(默认值, divProps)` 顺序与宿主可访问性树，注释改正并改为按可访问名称定位 |
| Low | 两处新增用例缺少"断言非恒真"的变体自校验 | 注册表消费检查与模块注册检查各补一组变体理论 |
| Medium | 三处"RED 基线"标签与规范冲突（编译失败不算行为 RED）、引用章节错误 | 改题"静态失败基线（非行为 RED）"，引用改为 `testing-baseline.md` §1.4 与 `AGENTS.md` 闸门 4 |
| Medium | S2-D13 状态（`N/A`+`AcceptedRisk`）与矩阵终态（V24 `Blocked`）不一致 | 决策行同步为严格基线口径 |
| Medium/Low | `testReport.md` 首句与末句过期、用例数与文件数过期、`C22` 端点数未随批次更新、失效行号引用 | 逐处按批次时点标注并修正 |

### 误报（记录）

- 一处意见称 `Assert.DoesNotContain("_itemTypeMember1", models)` 恒过、"当前生成物仍含该子串"：实测当前 `models/index.ts` 中 `Member1` 出现 **0 次**（`git diff` 显示本轮删除的正是 `EffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1` 与 `MedicalStandardCategoryListQueryRequest_itemTypeMember1`），断言有效。仍采纳其"按具体名称锁定"的建议，补两条精确断言。
- 一处意见称拒绝用例"空请求（null）完全没有覆盖"：该用例 `Enum_metadata_query_rejects_unknown_case_variant_and_blank_names` 已断言 `ArgumentNullException`，与实现一致。

### 阶段1 枚举不纳入本轮（登记为残余）

`MedicalStandardUsageStatus`（0 未使用、1 使用中）出现在两个已发布的阶段1 只读模型（分类/分组列表的 `usageStatus`）上，但没有 `[EnumDescriptor]`/`[Description]`、未登记，冻结契约中仍是纯 `integer`，前端仍自持取值与中文（`standardCatalogApi.ts`、`StandardCatalog.tsx` 的"未被使用/已被下级使用"）。阶段1 契约上另有多个同类枚举（`LaboratoryResultType`、`MedicalReportType` 等）。**本轮的枚举契约覆盖范围为阶段2 互认配置契约 + 生效目录的项目类型**；**不扩展**，阶段1 枚举按残余登记，待阶段1 页面下次改动时再一并设计（届时需同时确定中文措辞、只读模型文本字段与页面迁移）。

**追记（2026-09-17，残余已消除）**：上述阶段1 枚举迁移已由阶段1 的 `S1-D32` 完成，本节描述的"未登记、纯 `integer`、前端自持中文"不再是当前状态。当前事实：`MedicalStandardUsageStatus` 已加 `[EnumDescriptor]` 与成员 `[Description("未使用")/("已使用")]` 并登记进白名单；阶段1 的分类、分组、标准项目与有效目录类型只读模型交付 `ItemTypeText`/`UsageStatusText`；冻结 OpenAPI 中该枚举带 `enum`/`x-enumNames`/`x-enumDescriptions`，生成端为数值；前端按「随行文案 → 枚举元数据 → 本地兜底」取值，展示文案为「未使用 / 已使用」（本节所写的「1 使用中」与「未被使用 / 已被下级使用」均为当时的表述）。阶段1 侧证据见 [阶段1 测试报告](../003-阶段1-标准项目目录维护/testReport.md) 第四十二轮（前端矩阵新增 `C75`）。`LaboratoryResultType`、`MedicalReportType` 等后续阶段枚举不在该迁移范围，仍待各自阶段处理。

### 部分成立并登记

- 冻结文档与真实后端输出的对账无法在离线用例里重跑后端，登记为**人工步骤**（`prepare-openapi` 每次输出的 path 数与两类修正计数留档在本报告各轮小节）。
- `normalizeSchema` 无单测、归一差异不可审计（只有 stdout 计数）。脚本为包内生成入口、无测试设施，登记为残余；如需审计，应把来源 URL、时间与计数落成可提交的生成记录后再补最小单测。
- 本报告保留"批次A/B/C/D/E"等过程小节，与 [planning-and-evidence.md](../../../.agents/instructions/planning-and-evidence.md) §4"阶段文档只表达最终设计状态"存在张力。**偏离理由**：[testing.md](../../../.agents/instructions/testing.md) 要求每次真实运行后及时更新结果，阶段内多轮验证的结果需要各自留证；阶段终态由「关键决策」（阶段 `design.md`）与「结果」两节承载，过程小节只作证据归档，不写成设计结论。该偏离同时适用于阶段 `design.md` 与 `Server/`、`Client/` 的 design 文档：其中保留的"设计期表述/后续状态"标注同样只作过程留痕，终态口径以正文与决策表为准。
- 筛选器仍隐藏未登记状态取值（有意取舍，已在适配层注释写明"契约若新增取值需先在本模块声明再放开"）；中文来源口径已收窄为"阶段2 互认项目页及其只读模型"。

## 收口（2026-09-16）

本节记录阶段收口事实：全部交付项已完成、门禁与宿主终验通过、残余项已按技术理由逐条登记；阶段状态：**收口**（不使用无证据的终态标签）。

### 交付范围

| 面 | 交付物 |
|---|---|
| 后端契约 | 查询 Request/ReadModel（含 `ItemTypeText`/`ConfigurationStatusText`）、枚举元数据查询契约（`IEnumMetadataAppService` + DTO + Request）、`EnumDescriptorText` |
| 后端实现 | 四个写入口、五个查询入口、枚举元数据查询、OpenApi 文档转换器与描述器注册表、`TrustedOrganizationResolver` |
| 数据库 | `mrec_mutual_recognition_item` 建表脚本（8 列、全 `NOT NULL`、无默认值、唯一索引、中文注释）与 SqlMap |
| 客户端 | 累计 Kiota 客户端（冻结文档 23 path = 业务 22 + `/auth/login`）、请求/读模型契约、适配层、`useEnumMetadata`、互认项目页面与三个弹窗 |
| 文档 | 阶段 `design.md`（S2-D1..D15）、`Server/`・`Client/` 设计与实施、`阶段2-Tickets/01-08`、本报告 |

### 终态矩阵

52 条业务用例：`Passed` **47**、`Blocked` **4**（V24 事件可见性、V26 并发事件计数、V27 跨组织写拒绝、C17 组织切换宿主页面层）、`N/A` **1**（V10）。四条受阻原因登记为决策标签（**接受口径待负责人确认**），不改状态、决策标签不计入 `Passed`（4 条 `Blocked` 计入 52 条分母并单列）；并发相关条目不在本阶段范围内，不再投入。

### 门禁终值（各端最近一次适用轮次）

下表是各端最近一次适用轮次的复跑结果：后端与前端均为"第三批整改（2026-09-16，批次18 / B17 / F14）"后的复跑（该轮后端改契约异常声明、测试守卫与测试运行配置，前端改提交期组织复核与页面口径），客户端包自批次10/11 重生成后未再改动。本表只登记各端最新值，各轮时点值见对应轮次小节。

| 门禁 | 结果 |
|---|---|
| 后端构建 / 测试 | `dotnet build server/Dy.MedicalRecognition.slnx` **0 错误**；`dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **通过 152 / 失败 0 / 跳过 0 / 总计 152**（源码 `[Fact]` 95 + `[Theory]` 13，测试方法 108；主线程独立复跑确认；批次18 / B17 的改动面见"第三批整改（2026-09-16，批次18 / B17 / F14）"） |
| 客户端包 | `typecheck` exit 0、`build` exit 0（批次10/11 重生成后未再改动） |
| 前端 | `cd client; pnpm -F dy-medical-recognition test` **6 文件通过，145 passed / 2 skipped（147）**、`Errors 3 errors`（既有同因，未新增）、`build` exit 0、`lint` **3 problems（0 errors, 3 warnings）**（本轮未登记逐文件计数，分母以本表合计为准；2 条 skipped 的阶段1 来源见"第二批整改（2026-09-16，批次17 / B16 / F13）"节） |
| 文档 | 0 断链、0 表格列数不一致 |
| 工作区 | `git diff --check` exit 0（批次18 复跑确认；该命令不覆盖未跟踪文件，未跟踪前端文件的 trailing whitespace 与 tab 扫描结论见"第二批整改（2026-09-16，批次17 / B16 / F13）"节） |

### 宿主终验（增量整改后重取，2026-09-16）

真实宿主菜单进入「互认项目」：`POST /Api/EnumMetadata/GetEnumMetadata` `200`、`QueryRecognitionProjectConfigurationList` `200`、`QueryEffectiveMedicalStandardCatalog` `200`、阶段1 三个目录查询 `200`；页面 5 行、类型「检验」、配置状态「启用」；筛选切「停用」为 0 行、重置恢复 5 行；`role=radiogroup` 的可访问名称为「配置状态筛选」（与依赖源码行为一致）。验收后关闭前后端服务。**环境项登记与证据边界**：本节与"批次A/B 整改后的宿主重取"同一链路（宿主入口、登录入口「检验检查结果互认平台」、菜单「互认项目」、账号 `yangkj`，账号与凭据位置见"Ticket 08 宿主验收执行结果"的"环境与进程"表与 `.agents/instructions/test-environment.md` 第 1 节）；本节未单独登记浏览器版本与会话标识、控制工具版本与执行时刻，属证据边界，需要时按该表核对。

### 源码守卫对齐（2026-09-16，收口后仅测试侧改动）

收口后对测试侧的源码静态判据做了一次写法对齐：判据改按 Roslyn 语法树判定，不再用文本或正则匹配源码；共享工具与复用治理按同组织 `Dy.LisCenter` 的 `Architecture` 测试做法建立。

**改动范围（只动测试工程与中央包版本，未改生产代码、前端与客户端包）**

| 项 | 文件 | 内容 |
|---|---|---|
| 依赖 | `server/Directory.Packages.props`、`Dy.MedicalRecognition.Tests.csproj` | 新增 `Microsoft.CodeAnalysis.CSharp` 4.11.0（`PrivateAssets="all"`） |
| 共享工具 | 新增 `Tests/Architecture/SourceSyntaxGuard.cs` | 语法树解析、方法体定位、成员名与类型参数取值、特性与字面量参数读取、仓库根定位 |
| 注册判据 | `Tests/Stage2EnumContractTests.cs` | 模块注册改在 `OnPostConfigureServices` **方法体**内按调用节点判定（选项类型、注册调用、转换器类型三者同节点链）；删除私有 `StripComments` |
| 注册表消费判据 | `Tests/Stage2EnumMetadataQueryTests.cs` | 改按语法节点判定：必须真实引用登记注册表，且不得出现反射枚举、描述器列表直取、`string`→`Type` 字典；类型判定解包可空与限定名 |
| 枚举契约判据 | 同上 | OpenAPI 的 `enum`/`x-enumNames`/`x-enumDescriptions` 改与**枚举成员声明**逐节点比对（取值、名称、中文），取代 `[Description]` 正则与成员数锚点；用例更名为 `OpenApi_enum_contract_matches_the_enum_source_declarations` |
| 复用治理 | 新增 `Tests/Architecture/SourceGuardReuseTests.cs` | 改名副本、只写在注释里的伪调用、本地同名假替身三类判别力用例；工程内消费方清单精确一致；禁止重复声明 `FindRepositoryRoot`/`StripComments` 或直接调用 Roslyn 解析入口 |
| 去重 | `Stage1ArchitectureTests.cs`、`Stage2WritePathTests.cs`、两个 Stage2 文件 | 删除 4 份私有 `FindRepositoryRoot` 与 2 份 `StripComments`，统一走共享工具 |

**对齐消除的失效形态**（均以变体样例固定）：注册语句被注释掉、写进字符串字面量、写在别的方法里、被 `#if false` 包裹；字符串里的 `//` 截断后续文本；静态限定名调用；`ConfigureAll<OtherOptions>`；`Dictionary<System.String, System.Type>`、`Dictionary<string, Type?>`、全限定 `System.Collections.Generic.Dictionary`、`IReadOnlyDictionary<string, Type>`；`using R = System.Reflection;` 别名。

**本轮自身的两条缺陷（由新变体当场报出，均已修）**：一是全限定泛型 `new System.Collections.Generic.Dictionary<string, Type>()` 未判为违规（限定名泛型的实参取不到，与上一轮正则漏判同类），补 `TypeArgumentList` 解包后通过；二是复用治理用例首跑自报两条违规——共享工具落在自己的"禁重复助手"范围内，以及 `TRUSTED_PLATFORM_ASSEMBLIES` 字面量规则命中了声明该规则的用例自身（该规则在本项目冗余），删该规则并对共享工具豁免。

**仍然登记的边界**（写在各自判据的 remarks）：预处理器关闭的分支不产生节点，其中的写法对节点扫描不可见；目标类型 `new()` 判不出类型名（该形参为接口，目标类型 new 不能编译）；`using static` 后裸写 `Descriptors` 会因"未引用注册表"失败；反射判定按成员名，其它类型上同名 `GetValues`/`GetNames` 会误报。

**门禁**：`dotnet build server/Dy.MedicalRecognition.slnx` **0 错误**、`dotnet test` **135 通过 / 0 失败 / 0 跳过**（原 124：删 19 条旧内联变体、新增 3 条治理用例与 27 条变体行）、`git diff --check` exit 0。前端与客户端包门禁不适用（未改其文件）。终态矩阵与收口结论不因此变化。

### 收口后仍登记的事项

1. **阶段1 枚举不纳入本轮**：阶段1 页面文案仍为前端本地实现，扩展需同时迁移阶段1 契约与页面，按残余登记，待阶段1 页面下次改动时一并设计。
2. 本报告「登记为残余、本轮不改」表的全部条目（含 `$IsValid` 真库渲染、阶段1 枚举边界、归一脚本审计、`dist` 构建顺序、`.kiota.log` 暂存风险等）。
3. **提交**：本阶段**暂不提交**，工作区保持现状（含 `.kiota.log` 的暂存删除）。需要提交时使用 `git add -A`（或分层 `git add`），避免只提交 `.kiota.log` 的暂存删除。工作区内的非阶段2 内容按类逐项列出，**这些改动不属阶段2 交付面、不得随阶段2 一并提交**：阶段3 计划文档（`docs/plans/005-阶段3-互认项目金额维护/**`）；总体计划（`docs/plans/001-总体计划/design.md`、`docs/plans/001-总体计划/impl.md`、`docs/plans/README.md`）；`.agents/instructions/**`（`backend-architecture.md`、`planning-and-evidence.md`、`project-context.md`、`test-environment.md`）；`AGENTS.md`；`CONTEXT.md`；`docs/需求规约SRS.md`；`docs/uml/**`；两张流程图（`docs/流程图.md`、`docs/业务流程图.md`）。

## 复审后整改（2026-09-16，批次16 / B15 / F12）

本节记录收口与源码守卫对齐之后一轮复审所提整改的执行结果，与"收口"节并列、不改变该节结论（阶段是否收口与受阻原因的接受口径仍由该节承载）；时点关系为：批次A/B 整改 → 整改后复审与收口前清理 → 第三轮复审后的修复（批次C/D/E）→ 枚举契约与中文来源 → 增量复审整改（第 1/2/3 轮）→ 源码守卫对齐 → 本节。

### 范围与改动面

复审按三条泳道分工整改：后端（`server/**`）、前端（`client/apps/dy-medical-recognition/src/**`）与阶段2 文档。

| 面 | 改动内容 |
|---|---|
| 后端 | 注释与风格整改：`Application`、`Domain`、`Domain.Share`、`Repository` 的既有成员（`CreateMutualRecognitionItemRequest`、两个启停 Request、四个 Command、四个 Event、`EnumDescriptorText`、枚举元数据契约与应用服务、查询应用服务、OpenAPI 转换器、Manager、Repository），内容为注释、`using` 顺序、私有字段改名与 `if` 花括号，**未改生产行为**；测试侧补本轮复审指出的缺口（见下"新增用例的归类"） |
| 前端 | **行为修复**：`RecognitionProjects.tsx` 的写提交在发起请求前以可信组织复核（`submitWrite`——可信组织缺失、提交时组织与可信组织不一致、页面已应用组织与可信组织不一致时都不提交），覆盖渲染与提交之间发生的组织变化；`RecognitionProjectModals.tsx` 的 `RecognitionProjectWriteGate` 与 `writeBlockedReason` 让三个写弹窗在组织未对齐、读取失败、读取在途时禁用确定按钮并给出原因。同批同步改动的前端文件为 `recognitionProjectsApi.ts`、`recognitionProjectsOrganizationScope.ts`、`useEnumMetadata.ts`、`recognitionProjectsApi.test.ts`、`routes.test.ts`；**未改客户端生成包与生成物** |
| 文档 | 决策状态（S2-D12/S2-D13）、枚举契约现状、批次台账、impl 表统计收敛与门禁终值口径按终态改写；本报告新增本节 |

### 门禁（主线程复跑）

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` → **0 错误**（5 条警告均为 git 行尾提示，非编译警告） |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` → **通过 150 / 失败 0 / 跳过 0 / 总计 150**（源码 `[Fact]` 93、`[Theory]` 13，测试方法 106，展开用例 150；整改前 135） |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition test` → **6 文件通过，127 passed / 2 skipped（129）**，`Errors 3 errors`（与整改前完全相同的 3 条既有未处理拒绝；依据为 `vite.config.ts` 的 `dangerouslyIgnoreUnhandledErrors`，未新增） |
| 前端构建 / lint | `build` exit 0；`lint` **3 problems（0 errors, 3 warnings）**，3 条 warning 均在未触碰文件 |
| 工作区 | `git diff --check` exit 0 |

前端由整改前的 117 passed / 2 skipped（119）增至 127 passed / 2 skipped（129），**净增 10 条**；后端展开用例由 135 增至 150。

### 新增用例的归类

前端新增的 10 条用例按主题归为四类，均在 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.test.tsx`：

1. 写弹窗门控与组织未对齐期间的零写请求：「组织待切换确认框弹出期间，新增弹窗提交为零写请求且保留输入」「取消组织切换确认后（deferred），写弹窗提交仍为零写请求」「修改时间弹窗在组织未对齐时同样零写请求」「读取失败或组织未对齐时三个写弹窗的确定按钮禁用且不触发提交」。
2. 表格行标识稳定唯一：「标识与编码全部缺失的多行互不冲突，操作按钮按行可访问」。
3. 列表空态与计数口径：「查询成功但本组织没有配置时显示空态文案且计数为 0」「筛选后为空时区分空态文案，并同时给出本组织条数与筛选后条数」「状态筛选同样进入筛选后为空的计数口径」。
4. 行内动作门控与列表规模：「多行结果全部渲染，读取只发起一次」「配置状态未知的行行内按钮禁用且零写请求」。

后端本轮只改既有测试文件（未新建测试文件），新增/强化用例归为两类：一是静态冻结判据（`Tests/Stage1SqlMapProbeTests.cs` 的注册键、语句与 DDL 冻结判据，`Tests/Stage2WritePathTests.cs` 的互认语句与物理表判据）；二是行为边界（`Tests/Stage2WritePathTests.cs`、`Tests/Stage2QueryTests.cs` 的写路径与查询边界，`Tests/Stage2EnumContractTests.cs`、`Tests/Stage2EnumMetadataQueryTests.cs`、`Tests/Stage1ArchitectureTests.cs`、`Tests/Architecture/SourceSyntaxGuard.cs` 与 `Tests/Architecture/SourceGuardReuseTests.cs` 的源码守卫判定形态）。其中 V4 的"小数与永久有效在 `int` 契约下不可构造"由 `Duration_days_must_be_a_positive_integer_on_create_and_update_requests` 与 `Duration_days_boundaries_are_reachable_through_the_write_path` 固定，矩阵侧处置见 [Server/design.md](Server/design.md) 的 V4 行。

### 未覆盖面与残余风险

- 本轮未重取宿主证据：后端未改生产行为，前端改动为写入口门控与提交前复核，其触发窗口需要"宿主重新签发 token 后页面尚未对齐"或"读取失败态下已打开的写弹窗"，在真实宿主不可构造（与 C17 的 Host 面同一环境边界）；该分支的证据为组件层用例（上述第 1 类），按 Testing Baseline §5 登记为证据边界，不用旧宿主证据证明该分支。
- 四条 `Blocked` 的状态、受阻原因与统计口径不因本轮整改变化；其受阻原因的接受口径待负责人确认（见"结果"节）。
- 本报告"登记为残余、本轮不改"表的条目、阶段1 枚举边界与四条 `Blocked` 的边界均未被本轮整改改变。

## 第二批整改（2026-09-16，批次17 / B16 / F13）

本节记录"复审后整改（批次16 / B15 / F12）"之后的第二批整改执行结果，与"收口"节并列、不改变该节结论（阶段是否收口与受阻原因的接受口径仍由该节承载）；时点关系为：批次A/B 整改 → 整改后复审与收口前清理 → 第三轮复审后的修复（批次C/D/E）→ 枚举契约与中文来源 → 增量复审整改（第 1/2/3 轮）→ 源码守卫对齐 → 批次16（B15 / F12）→ 本节。

### 范围与改动面

本批按域分工整改：后端（`server/Dy.MedicalRecognition.Application.Contracts/**`、`server/Dy.MedicalRecognition.Application/**`、`server/Dy.MedicalRecognition.Tests/**` 与测试运行配置）、前端（`client/apps/dy-medical-recognition/src/**`）与阶段2 文档。

| 面 | 改动内容 |
|---|---|
| 后端 | 四个互认写入口的 XML 返回/异常口径按终态补齐（`Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.cs`）；私有组织解析包装按共享解析点 `TrustedOrganizationResolver` 内联（`Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs`，等价重构）；新增 Provider 中立静态守卫用例 `Public_types_and_shared_sql_stay_database_provider_neutral`（`Tests/Stage1ArchitectureTests.cs`）：扫描范围为 `server` 下五个受治理工程的手写 `.cs` 与 `Repository` 下的 SqlMap `.xml`、建表 `.sql`，排除 `bin`、`obj` 与 `*.g.cs`、`*.Designer.cs`；Provider 类型名与 SQL 关键字按不区分大小写的**整词匹配**（词边界使 `DateTimeOffset` 不命中方言关键字 `offset`），方言符号按原文匹配；`Repository/EarthraceConfig.json` 明确不在扫描范围内，理由是**该文件的作用正是选择当前 Provider 与连接串**；用例含受治理文件数下界断言与两条自校验，当前实测 **0 违规**。`SmokeTests.Generated_contract_is_available` 删除恒真的 `typeof(...) != null`，改为"契约类型来自契约程序集、不是测试程序集同名替身"+四个互认写入口各恰好一处声明且返回 `Task<bool>`。新增 `Tests/xunit.runner.json` 关闭程序集与集合并行 |
| 前端 | `useEnumMetadata` 新增 `enabled` 开关，页面只在可信组织可用时读取枚举元数据；修改可互认时间弹窗改持配置标识、按当前行集重取当前行；表格行标识唯一性改按 `data-row-key` 实际值断言；页面与 hook 的在途请求边界按事实登记（只作废不取消） |
| 文档 | 批次台账（`impl.md` 批次17、`Server/impl.md` B16、`Client/impl.md` F13）；`Server/design.md` 登记四个互认写入口的返回与异常口径；`Client/design.md` 与 `Client/Pages/RecognitionProjects/RecognitionProjects.md` 登记在途请求边界、修改弹窗取当前行、页面只持视图模型、行标识与 `data-row-key` 断言、枚举元数据不属组织范围数据；`Client/impl.md` 的层级口径按 `Client/testPlan.md` 统一、中间轮门禁值与增量的分母口径更正、前端构建产物事实登记；本报告新增本节并更新"门禁终值"节 |

**关闭测试并行的依据与代价**：源码静态判据要跨文件读取源码，且测试进程内有共享注册状态（`TypeHandlerFactory.Register`、共享 SqlMap 注册表），并行集合之间共享进程状态构成非确定性来源；实测代价为同一命令重复三次报告 768/782/790 ms（`parallelizeAssembly` 与 `parallelizeTestCollections` 均为 `false`）与 281/287/285 ms（开启），差约 0.5 秒，按可接受处置。该处置同时闭合 2026-09-15 独立审查登记的"测试工程未禁用并行集合"一项（见"本轮执行结果"表）。

### 门禁（主线程复跑）

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` → **0 错误** |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` → **通过 151 / 失败 0 / 跳过 0 / 总计 151**（源码 `[Fact]` 94 + `[Theory]` 13，测试方法 107；30 条 `InlineData` 行 + 2 处 `MemberData` 行源；批次16 时点为 150） |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition test` → **6 文件通过，138 passed / 2 skipped（140）**，`Errors 3 errors`（与上一时点相同的 3 条既有未处理拒绝，未新增；依据为 `vite.config.ts` 的 `dangerouslyIgnoreUnhandledErrors`）。逐文件计数（当前时点）：`RecognitionProjects.test.tsx` 52、`recognitionProjectsApi.test.ts` 37、`useEnumMetadata.test.ts` 12、`routes.test.ts` 3、`standardCatalogApi.test.ts` 15、`StandardCatalog.test.tsx` 21（含 2 skipped），合计 140 |
| 前端构建 / lint | `build` exit 0；`lint` **3 problems（0 errors, 3 warnings）**，3 条 warning 均在未触碰文件 |
| 工作区 | `git diff --check` exit 0。**该命令不覆盖未跟踪文件**：本批前端改动文件在 git 中均未被跟踪（`?? src/hooks/`、`?? src/pages/recognitionProjects/`、`?? src/router/routes.test.ts`，共 10 个文件），已对这 10 个文件另做扫描——trailing whitespace 0、tab 0 |

**2 条 skipped 的来源**：`StandardCatalog.test.tsx` 的两处 `it.skip`（「身份有效切换会清理选择/缓存/表单/弹窗 —— 页面无身份消费点，待负责人裁定归属」「未知 ItemType 的分类应在目录树上可见（当前被静默隐藏）—— 已知缺口，待负责人裁定」）为**阶段1 遗留**，登记在 `docs/plans/003-阶段1-标准项目目录维护/testReport.md`（该报告记"34 passed / 2 skipped"与"2 项 skip 记录缺口"）；**不属于本阶段新增缺口**，本阶段既未新增也未删除 skipped 用例。

### 新增/强化用例归类

前端相对上一时点由 127 passed / 2 skipped（129）增至 138 passed / 2 skipped（140），**净增 11 条**；另有 1 条同类旧用例被取代——批次16 登记的「标识与编码全部缺失的多行互不冲突，操作按钮按行可访问」（`RecognitionProjects.test.tsx`）已不存在，由「业务字段完全相同的多行互不串位，操作与数值都指向各自那一行」取代并按 `data-row-key` 断言加强。与本批改动直接相关的用例按主题归为三类：

1. **枚举元数据请求门控**：`src/hooks/useEnumMetadata.test.ts`「does not request the endpoint while the caller reports the options are unusable」；`RecognitionProjects.test.tsx`「组织范围不可用时不读取枚举元数据，恢复可信组织后才读取」。
2. **表格行标识唯一性**：「业务字段完全相同的多行互不串位，操作与数值都指向各自那一行」——判据取 rc-table 渲染到行上的 `data-row-key` 两两不同，不依赖 React 的重复 key 告警文本。
3. **修改弹窗按当前行取值**：「弹窗打开期间后台刷新交付新值时，弹窗取当前行取值而不是打开时的快照」「关闭后再次打开时，修改弹窗回填当前行取值而不是上次输入」。

**归属边界**：本批前端改动文件在 git 中均未被跟踪，不存在可用于逐条归属的差异基线，因此本节只登记净增数、主题与用例名，不逐条断言其余新增条目归本批；分母以"门禁"表的当前时点逐文件计数为准（合计 140，与门禁一致）。

后端相对上一时点由 150 增至 151（**净增 1 条**）：新增 `Public_types_and_shared_sql_stay_database_provider_neutral`（`[Fact]`）；`SmokeTests` 的断言替换不改变用例条数（仍为 1 条 `[Fact]`）。

### 未覆盖面与残余风险

- **缺陷期探针的证据边界（`probe.serialize.test.ts`）**：枚举契约缺陷修复节所依据的序列化退化观察（`serializeRecognitionProjectConfigurationListQueryRequest(...)` → `{"configurationStatus":{}}`）由缺陷期的一次性探针测试文件取得，路径为 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/probe.serialize.test.ts`：**取得方式**是在页面测试目录临时新增该探针并随测试运行，由生成端的序列化入口直接产出缺陷期写出结果；**删除原因**是它只服务于缺陷定位，未作为长期回归用例保留，也从未纳入版本控制。该文件当前**不存在于工作区**（文件系统与 `git status` 均无该路径），因此**没有可复现载体**；唯一留痕是 vitest 结果缓存 `client/apps/dy-medical-recognition/node_modules/.vite/vitest/da39a3ee5e6b4b0d3255bfef95601890afd80709/results.json`（其中记录该路径、`duration` 为 0 且 `failed` 为 true，文件时间为 2026-09-16 17:27），而 `node_modules` 不在版本控制内，缓存清理后该留痕一并消失。该缺陷当前的守卫是 `Stage2EnumContractTests` 的冻结 OpenAPI 与生成物静态断言（见"枚举契约缺陷修复"节）；如需保留缺陷期实测证据，应补一条永久回归用例，本节不新建该探针文件。
- **前端在途请求只作废不取消**：生成客户端契约无取消入口（`RequestConfiguration<T>` 无 `AbortSignal`），页面按请求序号丢弃迟到响应，在途请求会在网络与服务端跑完；已登记在 `Client/design.md`、`Client/Pages/RecognitionProjects/RecognitionProjects.md` 与 `Client/impl.md`，本阶段不为此自建底层 HTTP 调用或新增拦截器。
- **只登记不改（历史记录与既有配置）**：`Server/审查-阶段2写入与查询复核.md:169` 仍按整改前的实现写"组织编码统一由 `ResolveOrganizationCode()` 注入"——该私有包装已随共享解析点 `TrustedOrganizationResolver` 的接入内联删除、该处行号亦已失效；该文件属历史审查记录，**不追改**，以此句登记其已过期。`server/Dy.MedicalRecognition.Repository/EarthraceConfig.json:32` 的 `DbProvider.Name` 为 `Oracle`，与本阶段 PostgreSQL 建表脚本（`Scripts/mutual_recognition_item.sql`）及 Host 配置（`server/Dy.MedicalRecognition/EarthraceConfig.json` 的 `PostgreSql`）口径不一致：只登记事实，本阶段不改该文件、也不改建表脚本。
- 四条 `Blocked` 的状态、受阻原因与统计口径不因本批整改变化（仍为 `Passed` 47 / `Blocked` 4 / `N/A` 1）；其受阻原因的接受口径待负责人确认（见"结果"节）。
- 本批未重取宿主证据：后端不改变生产行为（契约改动为写入口 XML 文档，应用层改动为按共享解析点内联私有包装的等价重构）**但确实改动了生产源文件，提交前仍须按 [gitnexus-workflow](../../../.agents/instructions/gitnexus-workflow.md) §1 第 5 条（提交是独立闸门）执行 `detect_changes()`**；前端改动为枚举元数据请求时机、弹窗取值来源与行标识断言，其中"组织范围不可用/组织未对齐"分支在真实宿主不可构造（与 C17 的 Host 面同一环境边界），按组件层用例与证据边界登记。
- **历史批次范围声明不追改**：`impl.md` 的批次 5 行仍写"C1-C3、C6、C8、C10-C11、C13-C24的非Host部分"，与本批按 `Client/testPlan.md` 层级定义改写的表述（`Client/impl.md` 的 F2 行）不同；该行是历史批次的范围声明，**不追改**，以此句登记。

## 第三批整改（2026-09-16，批次18 / B17 / F14）

本节记录第二批整改之后的第三批整改执行结果，与"收口"节并列、不改变该节结论（阶段是否收口与受阻原因的接受口径仍由该节承载）；时点关系为：批次A/B 整改 → 整改后复审与收口前清理 → 第三轮复审后的修复（批次C/D/E）→ 枚举契约与中文来源 → 增量复审整改（第 1/2/3 轮）→ 源码守卫对齐 → 批次16（B15 / F12）→ 批次17（B16 / F13）→ 本节。

### 范围与改动面

本批按域分工整改：后端（`server/Dy.MedicalRecognition.Application.Contracts/**`、`server/Dy.MedicalRecognition.Domain/**`、`server/Dy.MedicalRecognition.Tests/**` 与测试运行配置）、前端（`client/apps/dy-medical-recognition/src/**`）、阶段2 文档。

| 面 | 改动内容 |
|---|---|
| 后端 | ①四个互认写入口契约补 4 条 `ArgumentNullException` 声明（请求为 `null` 时请求校验先于一切判定，不进入领域调用、不写入任何数据，见 `Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.cs`）；②`MedicalRecognitionReportManager` 的两个启停入口把"条件更新影响多行"登记为**防御分支、物理不可达**（更新语句按主键等值与可信组织编码等值定位，最多命中一行），并统一行数口径注释：恰好 1 行为成功并登记事件、0 行进入同一可信组织的复读、其他取值为防御分支异常；③`Tests/Stage1ArchitectureTests.cs` 的 Provider 中立静态守卫 `Public_types_and_shared_sql_stay_database_provider_neutral` 自校验样本扩到**全部 29 条判据**，按特征名逐条断言，并断言判据条数等于命中条数、特征名两两不同；同时在 remarks 登记**不覆盖 `Repository/EarthraceConfig.json`** 的原因（该文件的作用正是选择当前 Provider 与连接串）；④新增 `Tests/xunit.runner.json` 的静态守卫用例 `Test_runner_disables_assembly_and_collection_parallelization`（按 JSON 键逐项断言两个并行化开关为 `false`） |
| 前端 | **行为变更**，可信组织**来源不变**（`@dy/auth` 的 `LoginUserManager` 的当前登录组织，与同组织参照项目 `Dy.LisCenter` 一致），**提交时改读现值**：新增命令式 `readTrustedOrganizationCode()`（`recognitionProjectsOrganizationScope.ts`，与渲染期 `useTrustedOrganizationScope` 共用同一实现），`RecognitionProjects.tsx` 的 `submitWrite` 在发请求前读现值可信组织并与页面已应用组织比对，不一致或读不到即**零写请求**，并把原因写进 `RecognitionProjectWriteGate` 由写弹窗展示（覆盖"宿主重新签发 token 到页面重渲染之间"的窗口）；删除 `appliedOrganizationRef` 的第二来源写法，该引用只作 `appliedOrganizationCode` 的同步镜像、仅在 `applyOrganization` 一处赋值；启停弹窗与修改弹窗一样只持配置标识、按**当前行集**重取目标行与目标状态（行不存在或状态未知时不渲染）；阻断原因次序固定为"组织未对齐（含提交期复核失败）→ 读取失败 → 读取在途"，`submitting` 不掩盖更强的阻断原因；页面 `Alert` 改用 `title`；"未保存内容"按**实际渲染**判定（修改弹窗目标行已离开当前行集时不渲染、不承载内容，因此不算未保存内容） |
| 文档 | 三份批次台账新增本批行（`impl.md` 批次18、`Server/impl.md` B17、`Client/impl.md` F14），三份批次表的逐轮门禁数字与用例计数收敛为"批次结论 + 矩阵编号 + 指向本报告门禁终值节"；`Client/design.md` 与 `Client/Pages/RecognitionProjects/RecognitionProjects.md` 按本批行为改写（组织来源与提交期现值复核、启停弹窗取值、未保存内容判定）；`Client/testPlan.md` 的 C 编号证据列更新到本轮用例名与层级（不改用例定义与编号）；本报告新增"轮次索引"节与本节，并把 C5 行尾的独立审查引用改为对行为面的陈述 |

### 门禁（主线程复跑）

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` → **0 错误** |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` → **通过 152 / 失败 0 / 跳过 0 / 总计 152**（源码 `[Fact]` 95 + `[Theory]` 13，测试方法 108；上一时点为 151） |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition test` → **6 文件通过，145 passed / 2 skipped（147）**，`Errors 3 errors`（既有同因，未新增）；上一时点为 138 passed / 2 skipped（140） |
| 前端构建 / lint | `build` exit 0；`lint` **3 problems（0 errors, 3 warnings）** |
| 工作区 | `git diff --check` exit 0 |

两处目标计数与源代码结构一致：后端 152 = `[Fact]` 95 + `[Theory]` 13 展开（测试方法 108）；前端 147 = 145 passed + 2 skipped。本轮未登记逐文件计数，分母以本表合计与"门禁终值"节为准。

### 新增/强化用例归类

前端新增 7 条用例，另有 1 条既有用例按新判据替换；净增 **7 条**（138 passed / 2 skipped（140）增至 145 passed / 2 skipped（147））。新增用例全部位于 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.test.tsx`，按主题归为四类：

1. **提交期现值复核**（`describe('提交前组织复核取现值')`，对应 C1 的 Component 层）：「宿主换组织凭证但页面尚未重渲染时，提交零写请求、保留弹窗与输入并给出原因」（判别方式为把 mock 层组织编码改成别的值而**不通知 token 订阅者**，渲染期门控因此仍打开，只要提交改回渲染期闭包值复核即失败）、「提交时读不到可信组织范围时同样零写请求并由页面给出阻断原因」、「可信组织经历「可用、不可用、同一组织」后提交仍会发出写请求」。
2. **启停弹窗按当前行取值**（对应 C8 的 Component 层）：「弹窗打开期间后台刷新改变该行状态时，启停弹窗按当前行重取动作目标」、「弹窗打开期间该行离开当前行集时，启停弹窗不再渲染也不再提交」。
3. **未保存内容按实际渲染判定**（对应 C18 的 Component 层）：「后台刷新使修改弹窗的目标行消失后，组织变化不再出现未保存内容确认」。
4. **阻断原因次序**（对应写弹窗门控，见 C11/C15 的"禁止写入"面）：「提交在途不掩盖更强的阻断原因，读取在途则不是阻断原因」——三段门控都带 `submitting`，差异只在其它分量；旧实现（提交在途时直接返回且无原因）会让组织类与读取类暂停说明消失。

后端由 151 增至 152（**净增 1 条**）：新增 `Test_runner_disables_assembly_and_collection_parallelization`（`[Fact]`，`Tests/Stage1ArchitectureTests.cs`）。同批的契约 `ArgumentNullException` 声明与 `Public_types_and_shared_sql_stay_database_provider_neutral` 的自校验样本扩充不改变用例条数（前者为契约注释，后者为既有 `[Fact]` 内的断言加强）。

### 未覆盖面与残余风险

- **`xunit.runner.json` 静态守卫的覆盖边界**：新增守卫只断言源文件存在且两个并行化键为 `false`，**未断言** `Dy.MedicalRecognition.Tests.csproj` 的 `<None Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />`。该复制指令被删除时运行器读不到副本、只会静默并行，守卫用例本身不会失败。登记为残余。
- **Provider 中立守卫的扫描范围**：判据排除 `Repository/EarthraceConfig.json`（该文件的作用正是选择当前 Provider 与连接串，其 `Database.DbProvider.Name` 为 `Oracle`，与本阶段 PostgreSQL 建表脚本 `Scripts/mutual_recognition_item.sql`、仓储识别重复配置所用的 SQLSTATE `23505`、宿主配置 `PostgreSql` 的口径不一致），因此"Provider 选型与共享 DDL 口径一致"**不在判据覆盖范围内**；该边界已在守卫 remarks 与本节双向登记。
- **提交期现值读取只做 `getOrgID()` + trim**：不含 token 判定——凭证被撤销而 `LoginUserManager.getOrgID()` 仍返回编码时，提交期会视为可用并发出写请求；渲染期门控仍会因 `useAuth()` 的 token 为空而阻断（`useTrustedOrganizationScope` 在 token 缺失时直接判不可用）。登记为残余风险。
- **"提交时读不到可信组织"路径的可见性细节**：复核失败触发重渲染后，页面进入"组织范围不可用"分支并卸载写弹窗，用户看到的是页面级阻断提示，而不是弹窗内原因；弹窗内原因只在"组织已变化但尚未重渲染为不可用分支"的窗口出现。属未覆盖的可见性细节。
- **跨组织写拒绝面（V27）仍为 `Blocked`**：本批的提交期现值复核只在客户端侧缓解（避免带着新凭证对页面已应用的旧组织发起写入），**不构成服务端拒绝的运行证据**；该条的受阻原因与接受口径不变（见"收口"节与"结果"节）。
- **弹窗焦点移交/回落与遮罩真实命中**：真实焦点移交与回落顺序、遮罩的真实命中（`pointer-events` 与层叠）依赖真实浏览器，jsdom 不能断言，属宿主浏览器验收项；本批未在真实宿主构造该场景。
- **本批未重取宿主证据**：前端为行为变更，其触发窗口需要"宿主重新签发 token 后页面尚未重渲染"或"后台刷新使弹窗目标行离开当前行集"，在真实宿主不可构造（与 C17 的 Host 面同一环境边界）；后端改动为契约异常声明、守卫测试与测试运行器配置，不改变生产行为。因此本批证据为组件层用例与静态判据，按 Testing Baseline §5 登记为证据边界，不用旧宿主证据证明这些分支。**其后（同日）已就本批涉及的前端行为重取宿主证据**：入口与拦截、首次加载与组织编码、修改可互认时间（同值保存）、启停、筛选口径、读取失败门控与重试的宿主证据见"宿主验收（2026-09-16，第三批后重取）"节；仍未覆盖的部分（触发窗口需要宿主重新签发 token 或弹窗目标行离开当前行集的分支）留在本节登记。
- **工作区归属登记（R1-07 两件事实）**：①`server/Dy.MedicalRecognition.Repository/Global.cs` 在批次 2 窗口内被写过，但内容与 `HEAD` **逐字节相同**（`git diff` 对该文件为空），归属未声明——不据此认为该文件属阶段2 改动；②`server/Directory.Packages.props` 在审查期间被**另一条工作线**写入：该文件当前 diff 含 `Dy.Base.Application.Contracts` 条目（另一条工作线），同文件另含阶段2 交付的 `Microsoft.AspNetCore.OpenApi`（批次9）与 `Microsoft.CodeAnalysis.CSharp`（批次14）条目，提交时须按类区分、**不属阶段2 交付面的条目不得随阶段2 一并提交**。两件事实只作归属登记，**不作为阶段2 的通过证据**。
- **提交闸门**：本批确实改动生产源文件，提交前仍须按 [gitnexus-workflow](../../../.agents/instructions/gitnexus-workflow.md) §1 第 5 条（提交是独立闸门）执行 `detect_changes()`。本轮已执行过一次（`--repo Lis`），结果含另一条工作线的改动、风险等级 `critical`；逐项归属复核后未发现计划外阶段2 影响。该记录是归属复核的过程事实，**不构成阶段2 的通过证据**。

## 宿主验收（2026-09-16，第三批后重取）

本节记录第三批整改（批次18 / B17 / F14）之后的宿主链路验收。时点关系为：批次16（B15 / F12）→ 批次17（B16 / F13）→ 批次18（B17 / F14）→ 本节。**本轮未改动任何代码**（改动面只有阶段2 文档登记），因此不影响"门禁终值"表登记的各端数值，也不改变矩阵状态统计。

### 范围与入口

按 [Testing](../../../.agents/instructions/testing.md) 第 1、2 节执行：从真实宿主登录页登录 → 由宿主菜单进入页面 → 在同一链路核对页面、DOM、Console、Network、响应与最终业务状态。覆盖范围为：入口与开发地址拦截是否生效、首次加载与组织编码、列表渲染与文案、修改可互认时间（同值保存）、启停确认与行内入口、编码与状态筛选口径、读取失败门控与重试、写入弹窗在组织对齐时是否放行。

| 项 | 内容 |
|---|---|
| 宿主入口 | `http://183.224.180.166:35000`，登录页 `/login`，登录入口**「检验检查结果互认平台」** |
| 菜单路径 | 宿主菜单「检验检查结果互认」→「互认项目」 |
| 账号 | `yangkj`（本报告只登记非敏感标识，不复制密码或任何认证凭据；账号与凭据位置见 [Test Environment](../../../.agents/instructions/test-environment.md) 第 1 节） |

### 环境与进程

| 项 | 实际值 |
|---|---|
| 可信上下文 | 组织 `01`、医院 `0101`（取自 `dy-auth:login-user`；本轮只登记这两个非敏感编码） |
| 开发地址拦截 | 全局开关 `dy-web-micro:dev-url-intercepts-enabled` = `"true"`；本项目映射 `{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}`（该键共 2 条映射） |
| 后端 | `dotnet run --no-build --project Dy.MedicalRecognition/Dy.MedicalRecognition.csproj`，PID `30568`，监听 15014（`::1` 与 `127.0.0.1`）；`GET http://localhost:15014/openapi/v1.json` HTTP 200（71753 字节） |
| 前端 | `pnpm -F dy-medical-recognition dev`，PID `26776`，vite 8.2.2，监听 3008（仅 `::1`，与 [Test Environment](../../../.agents/instructions/test-environment.md) 第 4 节的记载一致） |
| 浏览器 | Chrome DevTools MCP 受管会话（独立 user-data-dir，不使用日常 Profile） |
| 日志留档 | 仓库外临时目录（不入版本控制）：`%TEMP%\mrec-backend.log`、`%TEMP%\mrec-frontend.log`；截图 `%TEMP%\mrec-host-acceptance-20260916-ok.png` |
| 收尾 | 两个进程已退出，端口 15014 / 3008 已释放；未关闭其他项目的进程或浏览器 |

### 入口与拦截生效证据（Network）

- `GET http://localhost:3008/subApps/medical-recognition/recognition-projects [304]`
- `GET http://localhost:3008/subApps/medical-recognition/@vite/client [200]`、`src/main.tsx [304]`、`node_modules/.vite/deps/*.js?v=… [200]`
- Console 出现 `[vite] connected.`
- 业务接口全部打本地后端：`POST http://localhost:15014/Api/EnumMetadata/GetEnumMetadata [200]`、`POST …/MedicalRecognitionReportQuery/QueryRecognitionProjectConfigurationList [200]`、`POST …/QueryEffectiveMedicalStandardCatalog [200]`

### 页面与写入门控证据（DOM 与文本原文引用）

1. 宿主菜单「检验检查结果互认」→「互认项目」进入页面；页头「互认项目」「标准项目互认配置」「组织编码：01」。
2. 列表 5 行、9 列；类型列显示服务端交付文案「检验」、状态「启用」、目录停用原因「—」；汇总文案「本组织共 5 项配置」。
3. 修改可互认时间弹窗：回填当前行 `30`；提示原文「同值保存也会提交；保存成功后刷新列表。配置停用不影响此处修改。」；无「写入已暂停」提示；提交同值 → `POST /Api/MedicalRecognitionReport/UpdateMutualRecognitionItemConfiguration [200]` → 弹窗关闭、列表与目录刷新（两个查询各 200）、该行仍 `30 / 启用`。
4. 停用确认弹窗原文：「即将 停用 互认项目配置 S1 Retry Standard Item。该操作只修改这条配置的启用状态，不影响其它配置。」→ `POST …/DisableMutualRecognitionItem [200]` → 该行状态变「停用」、目录停用原因仍「—」、行内按钮标签变为「启用配置：S1 Retry Standard Item（当前停用）」。
5. 启用确认弹窗原文：「即将 启用 …」→ `POST …/EnableMutualRecognitionItem [200]` → 该行回「启用」、按钮回「停用配置：…（当前启用）」。
6. 筛选口径：状态筛选「停用」（当时无停用行）时，表格空态文案为「没有匹配的配置」，汇总为「本组织共 5 项配置，筛选后 0 项」；点「重置编码与状态筛选」后汇总回「本组织共 5 项配置」、编码筛选清空、5 行恢复。
7. 读取失败门控（用 CDP 网络离线注入构造）：页面阻断提示原文「互认配置数据待刷新」「读取失败，已有数据保留；维护操作暂不可用，可重试读取。」并提供「重试」；`新增配置` 按钮 `disabled=true`；全部行内按钮 `disabled=true`；已有 5 行数据与汇总保留。
8. 重试恢复：清除离线注入后点「重试」→ 阻断提示消失、`新增配置` 与行内按钮恢复可用、5 行数据仍在。
9. 新增配置弹窗打开时无「写入已暂停」提示（组织对齐，门控放行）；弹窗说明原文含「只列出当前有效标准目录的最底层标准项目；当前组织已配置的项目（含停用配置）不可选。」（本轮**未**展开下拉选项列表确认排除集合）。
10. Console 除 `[vite] connected.` 外无应用错误与 warn；离线注入期间出现的 2 条 `Failed to load resource: net::ERR_INTERNET_DISCONNECTED` 是**本轮故意注入**造成，不是缺陷。

上述 1-10 的编号映射（逐条列明）：

- 本轮实际覆盖：C1（首次加载与组织编码，证据第 1、2 条）、C6（修改可互认时间，第 3 条）、C8（启停，第 4、5 条）、C11 与 C15（读取失败与重试，第 7、8 条）、C12（菜单进入，第 1 条）。
- C25（不分页风险触发）：本轮为**观察未触发**——列表 5 行、9 列，未出现可复现卡顿、请求超时或内存问题（第 2 条）；其"预期结果"与用例定义不变。
- C22（累计 Client 契约）：属 Static/Build 面，**本轮宿主验收未覆盖**，不据本轮证据改变其层级与状态。
- C5（并发重复配置）、C10（组织切换竞态）、C16（组织切换清理）、C17（组织切换读取失败）、C19（写入期间防重复）、C21（写成功刷新失败）：本轮未执行，原因见下方"本轮跳过或未执行"表。

各编号在"证据/层级"列的层级指向见 [Client/testPlan.md](Client/testPlan.md)。

### 数据足迹

本轮未新增配置行；仅 `S1-RETRY-ITEM-20260914-001` 被写 3 次（同值保存 1 次、停用 1 次、启用 1 次），验收结束时该行状态与验收前一致（`30` 天 / 启用），变化仅为该行操作字段被刷新。

### 本轮跳过或未执行（逐条登记原因，均不写成通过）

| 项 | 原因 |
|---|---|
| 跨组织面（组织切换 C16/C17 的宿主层、V27 跨组织写拒绝） | 负责人本轮明确说明只有一个组织、无法维护第二个，指示跳过；登记为环境边界，不计为失败 |
| 并发重复保存（C5）、写成功但刷新失败（C21，需对刷新面故障注入）、连点防重复时序（C19）、迟到旧响应（C10） | 本轮未在宿主执行 |
| 弹窗遮罩真实命中、焦点移交与回落 | 未验证（组件层只有结构断言） |
| V18/V25 真实库元数据面、dbx 只读核对 | 本轮未执行 |
| 新增弹窗标准项目下拉的排除集合 | 未展开确认（组件层 C2/C3 用例与既有宿主证据仍在） |

### 残余风险

- 上表未执行项在本轮仍是未覆盖面；其规定验证面的登记口径不变，四条 `Blocked`（C17 宿主页面层、V24、V26、V27）的状态与统计（`Passed` 47 / `Blocked` 4 / `N/A` 1）不因本节变化（见"收口"节与"结果"节）。
- 读取失败门控由**浏览器侧临时的 CDP 网络离线注入**构造，验证后已清除（见证据第 8 条）；该注入不是产品行为，只用于触发页面既有的读取失败分支。
- 弹窗遮罩真实命中与焦点移交/回落仍只有组件层结构断言，边界与"第三批整改（2026-09-16，批次18 / B17 / F14）"节、"复审后整改（2026-09-16，批次16 / B15 / F12）"节的登记一致。
- 本轮未改动任何代码，故"门禁终值"表的后端、客户端包、前端与工作区数值沿用第三批整改后的复跑结果（批次18 / B17 / F14），本报告不重跑门禁。

## 结果

下列按相同状态合并编号，覆盖 52 条且无重复，状态为 2026-09-15 宿主验收（含当日补做的两页面并发重复与写成功刷新失败两条）、受阻原因登记为决策标签、以及 **2026-09-16 批次A/B 整改后对受影响用例的重取**之后的终态：`Passed` 47 条、`Blocked` 4 条、`N/A` 1 条。4 条 `Blocked` 的受阻原因与处置口径见各行；表中 `Passed` 只覆盖该用例声明的层级。**决策标签不计入统计，4 条 `Blocked` 计入 52 条分母并单列**；含 `Blocked` 与 `Passed` 的多层级用例按完整用例只计一次，未覆盖层在该用例的"未覆盖面/原因"列注明。

| 用例编号 | 验证面 | 状态 | 命令/操作及证据 | 未覆盖面/原因 |
|---|---|---|---|---|
| V1、V2、V3、V4、V5、V6、V7、V8、V9、V11、V12、V13、V14、V15、V16、V17、V19、V20、V21、V22、V23 | 领域、契约、应用层、仓储与真实链路 | Passed | 离线面见"本轮执行结果"；真实链路见"Ticket 08 宿主验收执行结果"（创建/修改/启停/查询/唯一冲突翻译/越权组织拒绝/目录停用原因三条文案与优先级/排序）。**2026-09-16 重取**：重复提交 500 与唯一约束翻译（V2/V15）、分组停用原因（V13）、越权组织 500（V19）、token 组织声明（V22）随批次A/B 整改后重跑，证据见"批次A/B 整改后的宿主重取" | 重取范围与复用边界见该节"证据失效与重取"表；V4 的"小数"与"永久有效"两项在 `int` 契约下不可构造，按 `N/A` 登记（见 [Server/design.md](Server/design.md) 的 V4 行），不计入该条未覆盖面 |
| V18、V25 | DDL 列序/类型/注释/索引（真实库元数据） | Passed | 目标表建成后以 dbx 只读元数据逐项核实（见"本轮执行结果"的元数据行）：8 列列序与类型一致、全 `NOT NULL`、无默认值、8 条列注释与表注释逐字一致、唯一索引覆盖 `(organization_code, standard_project_code)` 且无状态过滤、索引注释正确、无外键。<br>**批次A/B 未改数据库结构与脚本**，该证据按 Testing Baseline §5 复用（未重取） | 真实库元数据面，属本用例声明的 Static/DB 层级 |
| V24 | 无WorkUnit生产入口事件登记/处理/提交 | Blocked | **已执行并如实记录的部分**：数据库写入与失败响应实测通过（正常写入 `200` 并回读一致；重复写失败返回 `500` 且无脏写，见 V2/V15）。**未执行的部分**：本项目零 `IEventHandler` 实现、无事件持久化表、无专用观察点，"事件是否被登记/处理/提交"这一可见性子面在真实环境无法安全观察。按 Testing Baseline §3，整条记 `Blocked`；受阻原因登记为决策标签（接受口径待负责人确认），不改状态、不得据以把本条当作通过 | 事件可见性属框架边界；除该子面外的写入与失败响应面已实测 |
| V26 | 并发同方向停用时"恰好登记一次事件" | Blocked | **已执行并如实记录的部分**：两个并发同方向停用请求均返回 `200/true`，终态为停用，恰好一次有效状态变化、其余按幂等处理（终态与幂等面已实测）。**未执行的部分**：并发下"事件恰好登记一次"的计数子面无可控观察点（同 V24 的框架边界）。按 Testing Baseline §3，整条记 `Blocked`；受阻原因登记为决策标签（接受口径待负责人确认） | 事件计数子面无可控观察点 |
| V27 | 跨组织写：使用其他组织的配置 ID 提交写请求应被拒绝 | Blocked | **已执行并如实记录的部分**：该拒绝分支由离线用例覆盖（`Cross_organization_configuration_id_is_rejected_for_every_write`），且**查询侧的越权拒绝已在真实环境验证**（提交非可信组织查询 → `500`「请求组织与当前登录组织不一致，不能查询该组织的互认配置。」）。**未执行的部分**：真实环境构造不出"第二个可信组织持有配置"的合法前置（本环境仅单一可信组织，组织信息由组织管理系统提供、无法新增）。按 Testing Baseline §3，整条记 `Blocked`；受阻原因登记为决策标签（接受口径待负责人确认） | 真实跨组织写入面在本环境不可构造 |
| V10 | 不存在配置 ID | N/A | 正常业务流程不产生该数据状态，不通过直接写库或伪造数据构造该前置 | 不构造该前置 |
| C22 | 累计Client契约 | Passed | 见"本轮执行结果"的累计生成与契约核对行；净增 1 个查询端点（批次10/11 后净增 2 个：查询端点 + `/Api/EnumMetadata/GetEnumMetadata`，冻结文档 23 path）、16 写端点与阶段1四查询端点全部保留；批次10/11 的契约面由"枚举契约缺陷修复"与"枚举中文来源"两节的门禁与用例覆盖 | 仅契约与构建面；不覆盖页面使用 |
| C24 | Client生成入口保护 | Passed | 见"本轮执行结果"同一行；未用 `--clean-output`、`src/index.ts` 未被覆盖、`/auth/login` 保持排除、锁文件来源仍为包内稳定 OpenAPI | 仅静态与构建面 |
| C1、C2、C3、C10、C11、C14、C15、C16、C18、C19 | 独立组件 | Passed | 组件面用例已执行且全绿（`RecognitionProjects.test.tsx` / `recognitionProjectsApi.test.ts`；**2026-09-16 弹窗拆分为 `RecognitionProjectModals.tsx` 后引用改用用例名**，不再写会随重构失效的行号）：查询只提交 token 携带的可信组织编码、不带筛选、不取 URL 或本地存储（C1）、可信组织缺失或为空白时进入不可操作状态且零请求（C1）、只提供未配置的最底层项目且含停用配置在内的已配置项目全部隐藏（C2/C3）、迟到的旧组织响应不污染新组织（C10）、读取失败保留旧数据并进入不可操作态、部分读取失败同样阻断写操作（C11/C15）、手动刷新期间禁止写操作（C14）、组织变化后清空旧列表与弹窗、复位筛选并只加载目标组织（C16）、组织变化遇未保存内容先确认、取消保留内容与旧组织视图（C18）、连续点击只产生一次写请求且按钮保持提交中（C19）；主线程按测试运行结果 + 抽样核对断言本体确认**非空断言、与业务语义对应**，独立复核另做了 4 条抽样 + 路由用例核对（`Client/审查-Ticket07接线复核.md` §3，结论"真实断言、非同义反复"；其登记项与整改见 `Client/impl.md`） | 本用例声明的层级为 Component，已完整覆盖；Host 层级不属这十条；C4/C6/C8/C13/C20/C21 与 C17 的组件层证据登记在各自的用例行 |
| C23 | 创建/查询请求适配 | Passed | 适配层用例（`recognitionProjectsApi.test.ts`）「C23 创建请求只含标准项目编码与正整数天数，不含组织编码与内部标准项目 ID」「提交所选组织编码；未筛选时不下发可选字段」「保留 null 目录原因、映射生成枚举数值、归一 Guid 标识，未知枚举不退化」「行视图模型不引入创建/修改信息，也不引入已删除的派生状态字段」；Typecheck 层由 `tsc -b`（`pnpm -F dy-medical-recognition build`）exit 0 覆盖 | Component/Typecheck 两层均覆盖；生成契约的既有形态（必填未映射、枚举为 `number \| null`）已在"未覆盖面与残余风险"登记 |
| C4、C6、C8、C12、C13、C20、C25 | 宿主面（含 Host 的完整用例） | Passed | 真实宿主链路逐条取证，见"Ticket 08 宿主验收执行结果"：C12 菜单进入 + 劫持生效；C13 进入即自动加载；C4 非法天数零写请求、合法创建成功并默认启用、列表与选择数据均重载；C6/C20 回填当前值且同值仍提交并刷新；C8 取消零写、确认只改自身、按钮只给反向入口；C25 真实环境数据量下未出现卡顿/超时/内存问题，未新增分页。<br>**2026-09-16 重取**（批次B 重写弹窗与入口门控后）：上述各条按真实宿主链路重跑，证据见"批次A/B 整改后的宿主重取"，其中 C4 非法分支提示语已变为「可互认时间为 1 到 2147483647 之间的整数天数」、C13 额外记录"读取在途窗口内 `新增配置` 为禁用态"。**组件层证据（多层级用例的另一层）**：C6/C20「修改弹窗回填当前值，非法输入零写且弹窗保留，同值也照常提交」、C8「取消与右上角关闭都零写请求，确认只提交目标配置与目标状态」、C13「进入页面自动加载当前组织的配置与选择数据，加载期间禁止写操作」、C4「C4 的组件面：非法天数本地拦截且零写请求，弹窗与输入保留」（均在 `RecognitionProjects.test.tsx`） | 逐条证据与请求体见两节宿主验收章节；两条证据层级分别标注 |
| C7 | 停用后修改时间（宿主） | Passed | 宿主实测：配置停用后 `修改可互认时间` 入口仍可用、提交成功，**状态保持停用**、原因列不受影响（2026-09-15 为 30→45；**2026-09-16 批次A/B 后重取**为同值提交 `200` 且状态保持停用，见"批次A/B 整改后的宿主重取"） | — |
| C9 | 目录停用后的操作分支（宿主） | Passed | 宿主实测（分组 `S1-RETRY-GRP-20260914-001` 停用后）：原因显示「所属分组已停用」；`修改时间` **成功**且状态不变；`停用` **成功**；**`重新启用` 返回 500 并以「业务拒绝：所属分组已停用。」呈现，状态保持停用、操作入口保留**；恢复分组后原因回到 `—`。**2026-09-16 重取**额外核实：原因按分组**记录**而非名称生效（同名另一分组记录不影响本行）、弹窗内亦显示原因、失败后确认按钮未停留在提交态（DOM 无 `ant-btn-loading`） | 失败时确认框保留（便于重试/取消），与"保留状态和操作入口"一致 |
| C5 | 并发重复配置（两页面，宿主） | Passed | 两个宿主标签各持**载入时**的选项快照：A 标签新增 `S1-RETRY-ITEM-20260914-002`(20 天) 得 `200/true` 并刷新为 3 项；B 标签用快照中的同一项目提交得 **`500` + 「业务拒绝：该组织已配置此标准项目。」**，异常链含 `23505` 与索引 `ux_mrec_mutual_recognition_org_project`，**弹窗与两处输入保留、零刷新请求**（列表仍 2 项，真实组织已 3 项）。本次取证同时覆盖"后端拒绝后输入与弹窗保留"这一行为面。**2026-09-16 批次A/B 后重取**（主体换为 `S1-RETRY-ITEM-20260914-001`）结果一致：A 标签 `200/true`、B 标签 `500` + 同一文案与 `23505` 链路、弹窗与输入保留、失败后零刷新（B 标签列表仍 4 项而真实组织已 5 项） | 终态以应用链路回读为主证据（写 `200` + 列表出现该行），dbx 只读仅作辅助（C# Backend Testing §3） |
| C21 | 写成功刷新失败（宿主，受控注入） | Passed | 仅列表请求被注入失败：写请求 `200`，随后目录查询 `200`、列表请求被注入失败（未到达后端）；页面显示 **「写入已成功，数据待刷新」+「重试」**（**无任何写失败提示**）、行内与新增入口禁用、已有数据保留；**写入未丢失**——2026-09-15 以 dbx 只读辅助核对，**2026-09-16 重取改为以应用链路为主证据**（写响应 `200/true` + 点「重试」后列表回读为新值）；对只读「重新读取」分支同时验证了「互认配置数据待刷新」与禁用态。注入钩子两轮均已还原（实测 `fetch === 原始引用`）。**组件层证据（多层级用例的另一层）**：「新增写入成功后刷新失败不改判为写失败，页面进入阻断态并可重试」「写入失败保留弹窗与输入，不重载也不进入刷新阻断态」（`RecognitionProjects.test.tsx`） | 注入为浏览器侧一次性钩子，仅覆盖列表请求；机制与初版方案的差异见"受控构造方案" |
| C17 | 组织切换后目标组织读取失败 | Blocked | **已执行并如实记录的部分（组件层）**：用例「C17：目标组织加载失败时不进入可操作状态，也不展示旧组织数据」（`RecognitionProjects.test.tsx`）通过——切到另一个组织后令目标组织读取失败，页面显示「互认配置数据待刷新」、旧组织数据不再出现、`新增配置` 禁用，即"不进入目标组织可操作状态、不误用旧组织数据"。**未执行的部分（真实宿主页面层）**：本环境只有一个授权组织（组织信息由组织管理系统提供，无法新增），切换动作由宿主重新签发 token 完成，无法构造；替代做法需篡改共享登录存储，不予采用。按 Testing Baseline §3，整条记 `Blocked`；受阻原因按"Component 层证据 + Host 环境边界"登记为决策标签（接受口径待负责人确认），不改状态 | 真实宿主页面层未执行（环境边界） |

业务统计：`Passed` 47 条、`Blocked` 4 条、`N/A` 1 条（不存在配置 ID 的用例 V10），`NotRun`/`Failed`/`PendingRetest` 均 0 条；分母 52 条，独立文档检查不计入。4 条 `Blocked` 及其受阻原因：**组织切换后目标组织读取失败（C17）**——真实宿主页面层缺第二个授权组织；**事件登记/处理/提交的可见性（V24）**——零事件处理器、无事件表、无观察点；**并发同方向停用时"恰好登记一次事件"的计数子面（V26）**——同上框架边界；**跨组织写配置 ID 的拒绝（V27）**——构造不出"第二个可信组织持有配置"的合法前置。四条的受阻原因均登记为决策标签（**接受口径待负责人确认**），不改变状态；决策标签不计入统计，这 4 条 `Blocked` 计入 52 条分母并单列。表中 `Passed` 只覆盖该用例声明的层级。**2026-09-16 批次A/B 整改后**，受影响用例（改动涉及的宿主面与真实运行面）的证据已重取，其余按下文"证据失效与重取"表复用；重取未改变任何一条的状态。

## 未覆盖面与残余风险

- 阶段2 已具备：后端单元/契约/应用层与组件层证据、API Client 生成与契约核对证据、目标库真实元数据证据（V18/V25）；宿主页面运行证据随 Ticket 08 归档。
- 组织与事件机制的源码取证已完成（S2-D12/S2-D13 均已结案）；真实 token 中 `org` 非空**已取证**（`org:"01"`，V22，见"菜单与组织取值"）。仍未取证的是**非开发环境的最终合并配置**（属部署环境事项，不阻断本阶段）。
- **状态口径说明（2026-09-16 更新）**：本报告按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §3 执行——「`AcceptedRisk` 等标签不得进入测试结果统计，也不得替代 `Passed`」。因此**组织切换后目标组织读取失败（C17）**的宿主页面层、**事件可见性（V24）**、**并发停用的事件计数子面（V26）**、**跨组织写拒绝（V27）**这四项如实记为 `Blocked`，统计为 47/4/1，而不是整条记 `Passed`。这四条的受阻原因登记为**决策标签**（接受口径待负责人确认）：它不改变测试状态、决策标签不计入统计（4 条 `Blocked` 计入 52 条分母并单列），也不表示未执行的面已被验证。此前版本曾把这四条整条记为 `Passed` 并在备注中说明缺口，本次改回严格基线口径，其余 48 条不受影响。
- 生成契约的已知形态（影响 C23，**不是本阶段退化**）：①Kiota TypeScript 未把 OpenAPI 的 `required` 映射为 TS 必填，`organizationCode` 生成为 `string | null` 可选，适配层必须显式赋值并依赖服务端校验；②枚举属性（`itemType`、`configurationStatus`）生成为 `number | null` 并按 `getNumberValue`/`writeNumberValue` 读写（批次10/11 契约修复后的现状；生成端不产出 TypeScript 枚举，前端取值由适配层本地常量承载）。**主线程已逐项比对确认**：阶段2 `RecognitionProjectConfigurationReadModel` 的 11 个字段（含两个枚举文本属性）与生成 TS 接口**一一对应且顺序一致**，6 个已删字段在生成物中**零残留**；阶段1 的 `MedicalStandardItemListReadModel` 生成形态**完全相同**（全字段可选可空、`itemType` 为 `number | null`），故上述形态属项目既有 Kiota 生成行为。
- **契约缺陷（2026-09-16 立项并已修复）**：后端 OpenAPI 曾把 `ConfigurationStatus` 与 `MedicalItemType` 只声明为 `{"type":"integer"}`、**不含 `enum` 值**，Kiota 因而把二者生成为空接口，可空枚举属性经 `writeObjectValue` 在线上退化为 `{}`（阶段1 的 `itemType` 走同一路径）。**该缺陷已立项修复**：服务端为两个枚举补 `[EnumDescriptor]`/`[Description]` 与 OpenAPI 文档转换器，生成入口归一 `oneOf [null, $ref]`，生成端改为 `number | null` + `writeNumberValue`。修复内容、影响面、回归守卫与门禁见本文"枚举契约缺陷修复（跨阶段立项，2026-09-16）"一节。**中文来源同日已采纳 LisCenter 同口径**（后端 `xxxText` + `EnumMetadata` 查询接口 + 前端 `useEnumMetadata`），见本文"枚举中文来源（LisCenter 同口径，2026-09-16）"一节；该项不再有未决设计问题。
- **仍待执行（2026-09-16 更新）**：Ticket 07 已完成路由与真实接线（组件/构建/lint 层通过）；Ticket 08 宿主验收**已执行**（2026-09-15 首轮 + **2026-09-16 批次A/B 整改后重取**，逐条证据见对应两节）；剩余为 C17 的 Host 面（需第二个授权组织，受阻原因登记为决策标签，接受口径待负责人确认）。宿主前置已齐：目标表已建并核实、菜单子项「互认项目」已维护、写入边界已放宽为"经业务接口/页面写入"、宿主入口与凭据登记在整体测试规范第 1 节、子系统业务编码已确认。
- **代码修订与证据对应关系**：2026-09-15 的宿主证据对应"批次A/B 之前"的代码修订；2026-09-16 的重取证据对应"批次A（后端契约与口径）+ 批次B（前端质量）+ 主线程修复 B2 入口门控与 B4 `rowKey` 弃用参数"之后的修订；**2026-09-16 的第三轮复审后修复（批次C 后端、批次D 前端、批次E 文档与口径）再次改动查询应用层与页面渲染**，其受影响路径的宿主证据已按 §5 重取，见"第三轮复审后的修复（批次C/D/E，2026-09-16）"一节的宿主重取表。**同日后续的批次16 / B15 / F12 只改后端注释与风格、后端测试、前端写入门控与用例，未重取宿主证据**：后端未改生产行为，前端改动分支在真实宿主不可构造，按组件层证据与证据边界登记（见"复审后整改（2026-09-16，批次16 / B15 / F12）"）。历次重取均在对应章节按用例交代了重取/复用边界，未把重取前的旧证据当作当前修订的通过依据。**同日其后的第二批整改（批次17 / B16 / F13）同样未重取宿主证据**：后端改动在测试工程与测试运行配置内、不改变生产行为，前端改动分支在真实宿主不可构造，按组件层证据与证据边界登记（见"第二批整改（2026-09-16，批次17 / B16 / F13）"）。**同日其后的第三批整改（批次18 / B17 / F14）同样未重取宿主证据**：后端改动为契约异常声明、守卫测试与测试运行器配置，前端为提交期现值复核、启停弹窗取值与未保存内容判定的行为变更，其触发分支在真实宿主不可构造，按组件层用例与证据边界登记（见"第三批整改（2026-09-16，批次18 / B17 / F14）"）。**本轮其后（同日）已就第三批涉及的前端行为重取宿主证据**，覆盖范围见"宿主验收（2026-09-16，第三批后重取）"节；该节未覆盖的部分仍按上述各节的证据边界登记。
- 既有卫生问题（非本阶段引入）：`client/packages/api-client-medical-recognition/src/.kiota.log` 曾被 git 跟踪，且每次生成都会改动它。已核对内容仅为 Kiota 的 Discriminator 警告、**不含本机路径**，因此无敏感信息暴露；但 [frontend-api-client](../../../.agents/instructions/frontend-api-client.md) 第 1.7 节要求生成日志不提交。**处置（2026-09-16）**：该文件已从版本控制移除（`git rm`，删除已入索引、尚未提交，磁盘上亦不存在），并在 `.gitignore` 写入 `.kiota.log` 规则（`.gitignore` 第 213 行；该规则必须排在包级反向规则 `!client/packages/api-client-medical-recognition/**` 之后才生效）。`git check-ignore -v` 实测该路径命中 `.gitignore:213:.kiota.log`，因此提交后重新生成 Client 只会产生被忽略的未跟踪文件，不再进入版本控制。**本条不再作为待裁决项。**
- 作用域方案与运行证据分开，阶段2实际注册键由V23验证；不裁定阶段1当前探测结果。
- 不分页风险引用 S2-D7，不创建外键引用 S2-D5。
- 不创建 `StandardItemId` 外键为已确认设计，不属于遗漏。

## 测试侧未处理拒绝观测补齐（2026-09-17）

`RecognitionProjects.test.tsx` 的两条写失败用例原先让写失败的拒绝逃逸到进程，由 `vite.config.ts` 的全局忽略兜住。按总体设计 5.2（写失败只由宿主统一展示、页面不得自行捕获），两条用例改为在**用例内局部**安装 `process.on('unhandledRejection')` 观测器并断言恰好收到该笔拒绝、用例结束即移除监听；`vite.config.ts` 的全局忽略随之撤除，未预期到的拒绝今后会真实判红。本阶段用例的判定与结论不变（96 通过）；撤除后前端全量为 204 通过 | 2 跳过、0 未处理错误。本轮只改测试代码与测试配置，未改动生产代码、未改变任何用例的判定。

## 结论

阶段2 的 52 条业务矩阵已执行完毕：`Passed` 47 条、`Blocked` 4 条、`N/A` 1 条（不存在配置 ID 的用例），`Failed`/`PendingRetest`/`NotRun` 为 0。后端离线与真实链路、Client 生成与契约、组件层、真实库元数据、宿主页面链路均有本报告列举的证据。4 条 `Blocked` 是**环境或框架边界导致规定验证面未执行**的项——组织切换后目标组织读取失败的宿主页面层（缺第二个授权组织）、事件可见性、并发停用的事件计数子面、跨组织写拒绝（缺少"第二个可信组织持有配置"的合法前置）；其受阻原因登记为决策标签（接受口径待负责人确认，不改变状态；决策标签不计入统计，4 条 `Blocked` 计入 52 条分母并单列），与 Testing Baseline §3 一致，本报告不再把它们记为通过。**2026-09-16 的批次A/B 整改后，受影响用例的宿主证据已按 Testing Baseline §5 重取**（见"批次A/B 整改后的宿主重取"），同日完成 7 轴整改后复审与收口前清理，清理后门禁为后端 **81/81**、前端 99 passed / 2 skipped、构建 0 错误、lint 0 error、`git diff --check` exit 0。阶段是否收口由收口节结论承载，本报告不代作结论。

本报告此前曾处于"仅文档整改"阶段，该阶段性说明已随实现与宿主验收的完成而失效：S2-D12/S2-D13 已结案（见阶段 `design.md`），不再阻断实现。

## 文档检查证据

2026-09-14，仅检查本阶段根 Markdown、Server/Client Markdown 和本轮涉及的SRS/UML；检查范围不包含阶段2审查辅助目录，未改写历史审查结果。

| 检查 | 结果 | 实际操作及证据 | 边界 |
|---|---|---|---|
| 矩阵定义与引用 | Passed | PowerShell读取正式文档，提取V/C编号及编号区间；V1-V27与C1-C25连续无重复，所有引用均有定义 | 不代表用例执行 |
| 文档链接及表格列数 | Passed | PowerShell逐文件解析Markdown相对链接并Test-Path；连续表格逐行核对列数，无断链/列数错误 | 仅本地相对Markdown链接 |
| UML1基础检查 | Passed | 执行dy-puml的validate-wsd.ps1，Path为docs/uml/1-MedicalRecognitionReport.wsd：0 errors、1 warning（CRLF） | 只验证基础块结构，不代表渲染或可生成性 |
| UML2基础检查 | Passed | 同脚本检查docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd：0 errors、1 warning（CRLF） | 无业务或生成器运行 |

这些结果独立于52条业务用例统计。模型渲染、生成与代码测试已执行；真实数据库与宿主验证由"Ticket 08 宿主验收执行结果"、"批次A/B 整改后的宿主重取"与"枚举中文来源"三节记录（本节的 `Passed` 只覆盖文档检查命令本身）。

### 独立子代理复核

审查代理 Mencius（`01a09f13-49ce-7e12-97f7-d47d260802c9`）未参与文档编写，仅只读复核；不是将编写代理自己的检查当作独立审查。

独立子代理复核已重新读取受影响段落及最终配置说明，未发现待修正文档问题。它同时核对V1-V27、C1-C25及52条状态分母。本次复核不要求或产生业务运行证据；该轮复核范围内的结论不解除当时尚未结案的 S2-D12/S2-D13（两项此后已按设计确认口径结案，见阶段 `design.md`）。
