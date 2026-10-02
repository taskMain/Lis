# 阶段 2 测试报告

## 执行边界

本报告记录阶段2各轮的文档检查与业务矩阵执行结果；业务矩阵的执行边界按下方"执行边界"与各轮小节区分，当前终态见"结果"节。阶段1已于 2026-09-15 完成规定收口（阶段状态 `Complete`，结论见阶段1 `testReport.md`）；本报告不重复裁定阶段1的历史证据。

- 报告状态：**已执行**。矩阵分五部分：原 52 条（后端 V1-V27、前端 C1-C25）中 `Passed` 47 条、`Blocked` 4 条、`N/A` 1 条；重开轮新增 11 条（V28-V33、C26-C30）**全部 `Passed`**；筛选区交互整改新增 2 条（C31-C32）与页头范围展示轮新增的 C33，其定义已被"设计与交互修订轮（2026-10-02，批次24 / F21）"改写，三条转 `PendingRetest`；同轮新增 C34，记 `NotRun`；页头范围展示轮的 V34 `Passed`。合计分母 **68 条**：`Passed` 59 条、`PendingRetest` 3 条、`NotRun` 1 条、`Blocked` 4 条、`N/A` 1 条，`Failed` 为 0。修订轮的实现与复测完成后，C31-C34 按实测结果回到终态。4 条受阻项均为**环境或框架边界导致的规定验证面未执行**，其受阻原因登记为决策标签（**接受口径待负责人确认**；决策标签不计入统计，这 4 条 `Blocked` 计入 63 条分母并单列）：**组织切换后目标组织读取失败**（C17）的真实宿主页面层、**事件登记/处理/提交的可见性**（V24）、**并发同方向停用时"恰好登记一次事件"的计数子面**（V26）、**跨组织写配置 ID 的拒绝**（V27）。宿主验收按 [Testing](../../../.agents/instructions/testing.md) 第 1、2 节从真实宿主登录页与菜单执行；`Passed` 只覆盖各用例声明的层级。**2026-09-16 批次A/B 整改后**，受影响用例的宿主证据已按 Testing Baseline §5 重取（见"批次A/B 整改后的宿主重取（2026-09-16）"）；同日完成**7 轴整改后复审与收口前清理**（0 Blocker、0 实现层硬违规），该轮门禁见"整改后复审与收口前清理（2026-09-16）"节；同日后续的 7 轴复审整改见"复审后整改（2026-09-16，批次16 / B15 / F12）"，最新门禁以"门禁终值"节为准；其后的第二批整改见"第二批整改（2026-09-16，批次17 / B16 / F13）"；其后的第三批整改见"第三批整改（2026-09-16，批次18 / B17 / F14）"。**重开轮实施与补证见"重开轮实施（2026-09-29）"与"重开轮补证轮（2026-09-30）"**：补证轮在重开轮终态工作区上重取了 C26、C28、V30，使新增 11 条的证据都对应同一份代码快照。**筛选区交互整改见"筛选区交互整改轮（2026-10-02，批次22 / F19）"**：按阶段根 S2-D21 删除筛选区重置控件并把标准项目编码筛选改为经搜索按钮或回车显式提交，新增 C31、C32 并取得组件层与真实宿主证据。轮次与批次的对照见"轮次索引"节。**页头范围展示见"页头范围展示轮（2026-10-02，批次23 / B20 / F20）"**：按阶段根 S2-D22 与 S2-D23 新增只读范围查询 `QueryTrustedOrganizationScope`，页头组织改为「名称 + 编码、名称为主」，刷新按钮补悬停提示，主按钮命名为「新增互认项目」，新增 V34、C33。**设计与交互修订见"设计与交互修订轮（2026-10-02，批次24 / F21，文档与设计）"**：S2-D21～S2-D23 修订并新增 S2-D24、S2-D25，C31/C32/C33 定义改写并转 `PendingRetest`、新增 C34，本轮只改文档、实现与复测待 F21。
- 矩阵版本：原矩阵 2026-09-14，后端 V1-V27（27 条）、前端 C1-C25（25 条），分母 52 条；重开轮 2026-09-29 增补后端 V28-V33（6 条）、前端 C26-C30（5 条），**终态分母 63 条**；多层级一条只计一次。
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
| 批次20 / B19 / F16-F18（重开轮实施） | 2026-09-29 | 阶段根 S2-D16～S2-D20 的代码实施与取证：后端批量创建写入能力（V28-V33 离线面）、累计 API Client 重生成、前端列视图全平铺与层级高亮与行内时间编辑、弹窗多选批量新增（C26-C30） | 后端 `server/**`、客户端包 `client/packages/api-client-medical-recognition/**`、前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**`、阶段2 文档 | 基线取证：`dotnet build server/Dy.MedicalRecognition.slnx` 0 错误、`dotnet test` 781 通过 / 0 失败 / 0 跳过；前端 `pnpm -F dy-medical-recognition test --run` 21 文件通过 / 375 passed / 2 skipped。实施中，逐批次门禁见下文"重开轮实施（2026-09-29）"节 | "重开轮实施（2026-09-29）" |
| 批次21（重开轮补证轮） | 2026-09-30 | 只补证据、不改设计不改实现：从重开轮终态工作区完整重跑全部门禁；在同一受管前后端进程上重取 C26、C28、V30 的宿主/真实运行证据，使重开轮新增矩阵的宿主证据构成一次连续干净的单一构建轮次 | 无代码改动；改动面只有阶段2 文档（本报告） | 后端构建与全量测试、前端 `test`/`build`/`lint`、客户端包 `typecheck`/`build`、`git diff --check`；宿主链路验收（受管前后端进程 + 宿主登录页与菜单进入，同一构建上连续取证） | "重开轮补证轮（2026-09-30）" |
| 批次22 / F19（筛选区交互整改轮） | 2026-10-02 | 阶段根 S2-D21 的代码实施与取证：删除筛选区重置控件，标准项目编码筛选改为经搜索按钮或输入框回车显式提交，配置状态筛选保持切换即生效（C31-C32） | 前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**` 的页面、样式与用例；阶段2 文档 | 目标测试 RED→GREEN、前端 `test`/`build`/`lint`、`git diff --check`、GitNexus 索引重建与 `impact`（风险 `LOW`）；宿主链路验收（受管前后端进程 + 宿主登录页与菜单进入，含 DOM 版面度量与筛选前后后端调用差值） | "筛选区交互整改轮（2026-10-02，批次22 / F19）" |
| 批次23 / B20 / F20（页头范围展示轮） | 2026-10-02 | 阶段根 S2-D22 与 S2-D23 的跨端实施与取证：新增只读范围查询 `QueryTrustedOrganizationScope`（V34），页头组织改为「名称 + 编码、名称为主」，刷新按钮补悬停提示，主按钮命名为「新增互认项目」（C33） | 后端 `Application.Contracts/Queries/**`、`Application/Queries/**`、`Tests/**`；客户端包生成物与冻结快照；前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**`；阶段2 文档 | 后端构建与全量测试、冻结路径清单（55→56）与契约方法集（两处）、客户端包 `prepare-openapi`/`generate`/`typecheck`/`build`、前端 `test`/`build`/`lint`、`git diff --check`；宿主链路验收（整页重载后取证） | "页头范围展示轮（2026-10-02，批次23 / B20 / F20）" |
| 批次24 / F21（设计与交互修订轮，文档轮） | 2026-10-02 | 阶段根 S2-D21～S2-D25 的修订：搜索动作改为「重新读取全量后在结果上本地裁剪、空条件提交等价刷新、去掉独立刷新控件」，页头组织改为上下两行且去掉文字标签，页头只保留主按钮，新增弹窗改为打开即重读并在弹窗内提供「重新读取」与失效勾选剔除 | 阶段2 文档（阶段根设计、客户端设计与页面文档、测试前矩阵、批次表、总体计划）；本轮不改代码 | 文档一致性核对与 `git diff --check`；实现与门禁留待 F21 实施轮 | "设计与交互修订轮（2026-10-02，批次24 / F21，文档与设计）" |
| 批次24 / F21（实施轮） | 2026-10-02 | 上述修订的代码实施与取证：搜索提交改为重新读取后再本地裁剪、空条件提交等价刷新、搜索按钮读取在途 `loading`；页头组织改上下两行且去标签、页头只保留主按钮；新增弹窗打开即重读、弹窗内「重新读取」常驻、重读后剔除失效勾选并说明条数（C31-C34）。另同轮附带：数据调整后以两个宿主标签页补取「剔除失效勾选」的宿主证据；修复阶段4 测试夹具的时间缺陷（只改测试） | 前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**` 的页面、弹窗、样式与用例；`server/Dy.MedicalRecognition.Tests/Stage4WritePathTests.cs` 的测试夹具；阶段2 文档 | 目标两文件 `vitest`、前端全量 `test`/`build`/`lint`、后端全量 `dotnet test`、`git diff --check`、GitNexus `detect-changes` 与 `impact`；宿主链路验收（整页重载后取证，含网络请求体、读取在途形态与重复点击守卫、两标签页的失效勾选剔除） | "修订轮实施（2026-10-02，批次24 / F21）" |

| 批次25 / F22（行集收敛与弹窗发现能力轮） | 2026-10-02 | 阶段根 S2-D16／S2-D17／S2-D24 的修订与 S2-D26～S2-D28 的实施：页面行集收敛为本组织已配置项、未配置项目只在新增弹窗出现；弹窗补编码搜索与全选当前结果；已生效筛选同步到子应用路由地址；长列表按 `content-visibility: auto` 声明离屏跳过渲染 | 前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**` 的适配层、页面、弹窗、样式与用例；阶段2 文档 | 适配层与页面/弹窗 `vitest`、前端全量 `test`/`lint`/`build`、后端全量 `dotnet test`、`git diff --check`、GitNexus `impact`；宿主链路验收（整页重载后取证，含行集与计数一致、弹窗搜索的读取增量、全选、命中区、路由参数往返） | "行集收敛与弹窗发现能力轮（2026-10-02，批次25 / F22）" |

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

矩阵分五部分，合计分母 **68 条**：

- 原矩阵 52 条（后端 V1-V27、前端 C1-C25）：`Passed` **47**、`Blocked` **4**（V24 事件可见性、V26 并发事件计数、V27 跨组织写拒绝、C17 组织切换宿主页面层）、`N/A` **1**（V10）。四条受阻原因登记为决策标签（**接受口径待负责人确认**），不改状态、决策标签不计入 `Passed`（4 条 `Blocked` 计入分母并单列）；并发相关条目不在本阶段范围内，不再投入。
- 重开轮新增 11 条（后端 V28-V33、前端 C26-C30）：`Passed` **11**、`Failed` **0**。其中 C27 在重开轮首轮宿主取证时曾记 `Failed`（项目层高亮缺失），修正后经组件层守卫用例与真实宿主复验通过；C26、C28、V30 在补证轮于重开轮终态工作区上重取。
- 筛选区交互整改新增 2 条（前端 C31-C32）：定义已被"设计与交互修订轮（2026-10-02，批次24 / F21）"改写，原证据对应修订前的形态，两条均转 `PendingRetest`。
- 页头范围展示轮新增 2 条（后端 V34、前端 C33）：V34 `Passed`（后端未改动，证据继续有效）；C33 的定义已被同一修订轮改写，转 `PendingRetest`。
- 设计与交互修订轮新增 1 条（前端 C34：弹窗就地重读与失效勾选剔除）：尚无证据，记 `NotRun`。

合计：`Passed` **59**、`PendingRetest` **3**、`NotRun` **1**、`Blocked` **4**、`N/A` **1**（分母 68）。修订轮的实施与复测完成后，C31-C34 按实测结果回到终态。

### 门禁终值（各端最近一次适用轮次）

下表是各端最近一次适用轮次的复跑结果：后端取"修订轮实施（2026-10-02，批次24 / F21）"同轮附带修复阶段4 测试夹具时间缺陷后的复跑（该轮不改后端生产代码，故数值即当前后端的稳定值；页头范围展示轮当时的 6 条失败集合见该轮小节）；前端取"修订轮实施（2026-10-02，批次24 / F21）"的复跑；客户端包取页头范围展示轮的重生成。本表只登记各端最新值，各轮时点值见对应轮次小节。

| 门禁 | 结果 |
|---|---|
| 后端构建 / 测试 | `dotnet build server/Dy.MedicalRecognition.slnx` **0 错误**（增量 5 警告 / 全量重建 39 警告，均为既有项）；`dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **通过 816 / 失败 0 / 跳过 0 / 总计 816**（6 条 `Stage4WritePathTests` 的作废用例曾因测试夹具时间固定而失败，已随修订轮实施修复夹具，生产代码与两端边界校验用例未改动，见该轮小节） |
| 客户端包 | `typecheck` exit 0、`build` exit 0；冻结文档 **56 path**（页头范围展示轮净增 `POST /Api/MedicalRecognitionReportQuery/QueryTrustedOrganizationScope`，`/auth/login` 保持排除） |
| 前端 | `cd client; pnpm -F dy-medical-recognition exec vitest run` **21 文件通过 / 423 passed / 2 skipped（425）**、`build` exit 0、`lint` **0 problems**（修订轮实施复跑；该轮重写 C31/C32/C33 并新增 C34） |
| 变更影响 | `gitnexus detect-changes --repo Dy.MedicalRecognition`：30 个文件 / 346 个符号 / 风险 `critical`（重开轮实施轮测得；受影响流程全部落在本阶段范围，未出现无法归属的跨阶段影响） |
| 文档 | 0 断链、0 表格列数不一致 |
| 工作区 | `git diff --check` exit 0（补证轮复跑确认；该命令不覆盖未跟踪文件，未跟踪前端文件的 trailing whitespace 与 tab 扫描结论见"第二批整改（2026-09-16，批次17 / B16 / F13）"节） |

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
- 本轮未改动任何代码，故该轮的"门禁终值"数值沿用第三批整改后的复跑结果（批次18 / B17 / F14），当轮未重跑门禁。**该句只描述 2026-09-16 的宿主复验轮**；其后重开轮实施（2026-09-29）与补证轮（2026-09-30）均已重跑全部门禁，最新值见"门禁终值"节。

## 结果

下列按相同状态合并编号，覆盖**原矩阵 52 条**且无重复（重开轮新增 11 条见"重开轮实施（2026-09-29）"与"重开轮补证轮（2026-09-30）"），状态为 2026-09-15 宿主验收（含当日补做的两页面并发重复与写成功刷新失败两条）、受阻原因登记为决策标签、以及 **2026-09-16 批次A/B 整改后对受影响用例的重取**之后的终态：`Passed` 47 条、`Blocked` 4 条、`N/A` 1 条。4 条 `Blocked` 的受阻原因与处置口径见各行；表中 `Passed` 只覆盖该用例声明的层级。**决策标签不计入统计，4 条 `Blocked` 计入 52 条分母并单列**；含 `Blocked` 与 `Passed` 的多层级用例按完整用例只计一次，未覆盖层在该用例的"未覆盖面/原因"列注明。

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

业务统计（**本表只覆盖原矩阵 52 条**；重开轮新增 11 条见"重开轮实施（2026-09-29）"与"重开轮补证轮（2026-09-30）"，阶段合计见"终态矩阵"与"结果"节）：`Passed` 47 条、`Blocked` 4 条、`N/A` 1 条（不存在配置 ID 的用例 V10），`NotRun`/`Failed`/`PendingRetest` 均 0 条；本表分母 52 条，独立文档检查不计入。4 条 `Blocked` 及其受阻原因：**组织切换后目标组织读取失败（C17）**——真实宿主页面层缺第二个授权组织；**事件登记/处理/提交的可见性（V24）**——零事件处理器、无事件表、无观察点；**并发同方向停用时"恰好登记一次事件"的计数子面（V26）**——同上框架边界；**跨组织写配置 ID 的拒绝（V27）**——构造不出"第二个可信组织持有配置"的合法前置。四条的受阻原因均登记为决策标签（**接受口径待负责人确认**），不改变状态；决策标签不计入统计，这 4 条 `Blocked` 计入 52 条分母并单列。表中 `Passed` 只覆盖该用例声明的层级。**2026-09-16 批次A/B 整改后**，受影响用例（改动涉及的宿主面与真实运行面）的证据已重取，其余按下文"证据失效与重取"表复用；重取未改变任何一条的状态。

## 未覆盖面与残余风险

- 阶段2 已具备：后端单元/契约/应用层与组件层证据、API Client 生成与契约核对证据、目标库真实元数据证据（V18/V25）；宿主页面运行证据随 Ticket 08 归档。
- 组织与事件机制的源码取证已完成（S2-D12/S2-D13 均已结案）；真实 token 中 `org` 非空**已取证**（`org:"01"`，V22，见"菜单与组织取值"）。仍未取证的是**非开发环境的最终合并配置**（属部署环境事项，不阻断本阶段）。
- **状态口径说明（2026-09-16 更新，2026-09-30 补注）**：本报告按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §3 执行——「`AcceptedRisk` 等标签不得进入测试结果统计，也不得替代 `Passed`」。因此**组织切换后目标组织读取失败（C17）**的宿主页面层、**事件可见性（V24）**、**并发停用的事件计数子面（V26）**、**跨组织写拒绝（V27）**这四项如实记为 `Blocked`，在原矩阵 52 条中统计为 47/4/1，而不是整条记 `Passed`。这四条的受阻原因登记为**决策标签**（接受口径待负责人确认）：它不改变测试状态、决策标签不计入统计（4 条 `Blocked` 计入分母并单列），也不表示未执行的面已被验证。此前版本曾把这四条整条记为 `Passed` 并在备注中说明缺口，本次改回严格基线口径，其余 48 条不受影响。**重开轮新增的 11 条（V28-V33、C26-C30）无 `Blocked` 项**，阶段合计见"终态矩阵"节。
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

## 重开轮实施（2026-09-29）

本节记录阶段根 S2-D16～S2-D20 的代码实施轮：页面改版为横向四栏列视图全平铺加弹窗多选批量新增，后端新增批量创建写入能力。设计依据为阶段根 [design.md](design.md) 的五条新增决策、[Server/design.md](Server/design.md) 的批量创建细化设计与 [Client/design.md](Client/design.md) 的页面交互定稿；矩阵为 V28-V33 与 C26-C30。

### 轮次边界与基线

- 起始工作区：`git status --short --branch` 实测 `## main...origin/main [ahead 3]`，无未提交修改（`git status --porcelain` 为 0 行）；起始提交 `0a96f34`。
- 本轮在共享工作区改代码与文档，不创建分支、不提交。
- 基线取证（实施动手前实测，用于与实施后对比失败集合）：`dotnet build server/Dy.MedicalRecognition.slnx` **0 错误 / 3 警告**；`dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **781 通过 / 0 失败 / 0 跳过**（Debug，本机 `server/Dy.MedicalRecognition/bin/Debug` 无进程占用）；`cd client; pnpm -F dy-medical-recognition test --run` **21 文件通过 / 375 passed / 2 skipped**。
- 实施前的进程处置：工作区内有两个非本轮启动的进程（后端 `Dy.MedicalRecognition.exe` 监听 15014，前端 vite 监听 3008）占用构建产物与端口，经负责人本轮明确批准后关闭，未关闭任何其他进程。

### 已确认的实施口径

- 后端批量创建：批量读取用 `where 列 in $集合参数`（同组织先例为 `Dy.LisCenter` 的 `QueryReceiverSpecimensByReceiverAndBarcodes` 与 `in $ReceiverSpecimenBarcodes`），因此目录读取与配置查重各新增一个 Statement、批量写入复用既有单条创建语句逐条插入，不新增语句与注册键。**框架批量写入在 PostgreSql 下不可用（2026-09-29 实测）**：`_dataMapper.BulkInsertAsync` 抛 `【PostgreSql】暂不支持批量操作！`——`Dy.Earthrace` 的 `SqlMapper.BulkInsertAsync` 按 `DataBaseType` 取 keyed `IBulkCommandExecuter`，取不到即抛，而本机只有 Oracle 与 SqlServer 两个实现包，设计原先引用的 `Dy.DDDSolution1` 先例实为 Oracle 配置、不构成 PostgreSql 可用性证据。故批量创建改为逐条插入，并由应用入口声明 `[WorkUnit(UseTransaction = true)]` 保证整批全成全败；整批回滚已在真实库取得证据（并发批次撞唯一索引后，先写入的行全部未残留）。
- 错误口径沿用项目既有链路：后端遇错直接抛出（`ValidationException` 或 `InvalidOperationException`），由框架与宿主统一处理；不新增异常类型、不做异常翻译、不加包装。可提前发现的问题（组织内重复配置）在写入前查重拦截，不依赖唯一索引报错；数据库异常按原样向外传播。
- 本轮不做并发专门设计，不新增锁、版本字段或重试；并发窗口内的唯一冲突按整批拒绝表达，已写入的行由入口事务回滚。
- 批量入参条数上限在请求层与领域层各有一道：请求层 `[MaxLength(1000)]` 先拒绝（超出即不进领域层，超大集合不会先被反序列化），领域层同一上限作纵深防御。该改动使实时 OpenAPI 的 `standardProjectCodes` 增加 `maxItems: 1000`，包内冻结快照与 Kiota 生成物已随之重跑同步。
- 一次性交互原型 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/prototype-recognition-projects-redesign.html` 按任务交接的约定在本轮实施与宿主验收完成后删除；设计以 `Client/design.md` 为准，原型不构成规格。

### 门禁终值（本轮各端最后一次复跑）

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` **0 错误 / 5 警告**（警告为既有生成物与测试风格项） |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **811 通过 / 0 失败 / 0 跳过**（基线 781，净增 30） |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition test --run` **21 文件通过 / 415 通过 / 2 跳过（417）**（基线 375，净增 40） |
| 前端 lint | `eslint .` **0 error / 0 warning** |
| 前端构建 | `pnpm -F dy-medical-recognition build` exit 0 |
| 客户端包 | `typecheck` exit 0、`build` exit 0 |
| 工作区 | `git diff --check` exit 0（仅 LF/CRLF 提示） |
| 变更影响 | `gitnexus detect-changes --repo Dy.MedicalRecognition`：30 个文件 / 346 个符号 / 风险 `critical`；受影响流程全部落在本阶段范围（`server/**` 的批量创建链路与 `client/**/recognitionProjects/**` 的适配层与页面），未出现无法归属的跨阶段影响 |

变更归属核对：本轮工作区共 46 个变更文件，按域分布为 `server/**` 25、`client/apps/**` 10、`client/packages/**` 5、本阶段文档 6，全部属本轮已批准范围。`critical` 由前端与后端两侧累计改动共同驱动，不影响归属判断。

进程收尾（本轮结束时）：本轮启动的后端（PID 5696）与前端 dev server（PID 15600）已关闭；实测 15014 与 3008 均无监听，后端进程数为 0。未关闭任何其他进程。

环境边界沿用既有登记：本环境仅一个授权组织、两个测试账号分属两个医院，不构造医院编码或院区编码为空的账号场景，不构造正常业务不可达的数据（V10 保持 `N/A`，V27 与 C17 的宿主面维持原有定级）。

### 批次执行结果

各端批次的实施与验证状态在对应实施文档逐批维护：后端见 [Server/impl.md](Server/impl.md) 的 B19，前端见 [Client/impl.md](Client/impl.md) 的 F16-F18，整体顺序见 [impl.md](impl.md) 的批次 20。矩阵条目 V28-V33、C26-C30 的实际结果、证据与未覆盖面在本节下方的结果表登记，未取得证据前不写 `Passed`。

### 宿主验收取证（2026-09-29，重开轮）

本轮由测试与证据主责智能体执行宿主取证，只采集证据与登记，未改动生产代码与测试代码。取证分两段：首轮 2026-09-29 23:56 至 2026-09-30 00:20，复验轮 2026-09-30 00:35 至 01:05（本机时钟）。

**并发改动与复验说明（如实登记）**：首轮取证期间与结束后，工作区由实施侧与主协调继续改动，共三处落在本轮取证范围内：`CreateMutualRecognitionItemsRequest.cs`（23:59:25，为 `StandardProjectCodes` 增补 `[MaxLength(1000)]` 与 `MaxStandardProjectCodeCount`）、`Stage2WritePathTests.cs`（00:00:55，新增请求层上限用例）、`RecognitionProjectCascade.tsx`（00:26:51）、`RecognitionProjects.css`（00:28:10）、`RecognitionProjects.test.tsx`（00:30:41，为项目格补上高亮类与样式）。首轮运行的后端进程（36888）与前端 dev server（43752）均**早于**上述改动启动，其宿主证据只覆盖改动前的代码。测试与证据主责智能体因此在复验轮对**当前磁盘代码**重新取证：重建并重启后端（进程号 5696）、对全部受影响用例重取宿主证据、并复跑两端测试。本节同时标注首轮与复验轮的取值，凡两轮结论不同的以复验轮为准。

#### 起始工作区与提交

- `git status --short --branch` 实测 `## main...origin/main [ahead 3]`；起始提交 `0a96f34f086c2e96e722cdf271ba6e51fb62c153`。
- 取证期间工作区文件集合与取证开始时一致：`git status --porcelain` 在本轮开始与结束均为 37 个已修改文件与 9 个未跟踪文件，本报告在取证开始时已处于已修改集合内。本轮只改动本报告，未创建分支、未提交。
- `git diff --check` 实测 exit 0，仅有 LF/CRLF 提示。

#### 进程、端口与日志

| 对象 | 归属 | 命令与工作目录 | 监听 | 日志 |
|---|---|---|---|---|
| 后端（首轮） | 本轮受管（沿用主协调已启动的进程） | `dotnet run --no-build --project Dy.MedicalRecognition`，工作目录 `server/` | `127.0.0.1:15014` 与 `[::1]:15014`，进程号 36888（`cmd` 包装 9724） | `var/dev-logs/backend.out.log`、`var/dev-logs/backend.err.log` |
| 后端（复验轮） | **本轮重启**（重建后加载当前代码） | 同上，经 `var/dev-logs/start-backend.bat` 启动 | 同上，进程号 5696 | 同上（追加写入） |
| 前端 dev server（首轮） | **本轮启动**，取证结束已关闭 | `pnpm -F dy-medical-recognition dev`，工作目录 `client/` | `[::1]:3008`，进程号 43752 | `client/var/dev-logs/frontend.out.log` |
| 前端 dev server（复验轮） | 由实施侧启动，非本轮启动，复验结束后保留运行 | 同上 | `[::1]:3008`，进程号 15600 | 同上 |

- 复验轮重建后端的经过：首次以 `dotnet test`（含编译）重建时，运行中的进程 36888 锁定了 `Dy.MedicalRecognition.Application.Contracts.dll`、`Application.dll`、`Repository.dll`，构建报 `MSB3027`/`MSB3021` 并失败。按 Testing 第 4 节「端口占用先核对归属」，确认 36888 即前述受管后端后将其停止（未使用含 Clean 的 `-t:Rebuild`），增量重建通过，随后按 `start-backend.bat` 原样重启。停止前已核实输出目录 6 个 `.json`（`appsettings.json`、`appsettings.Development.json`、`EarthraceConfig.json`、`deps.json`、`runtimeconfig.json`、`staticwebassets.endpoints.json`）均存在且大小正常，重启后 `GET /openapi/v1.json` 返回 200（长度 234923）。
- 后端 `GET /openapi/v1.json` 与页面实际调用的 5 个互认端点均返回 200 或按预期返回 500，见下方逐条证据。
- 前端 Vite 版本 8.2.2，实测启动输出：`VITE v8.2.2  ready in 156 ms`、`➜  Local:   http://localhost:3008/subApps/medical-recognition/`。监听形态为 IPv6 回环 `[::1]:3008`，与 [Test Environment](../../../.agents/instructions/test-environment.md) 第 4 节的登记一致。

#### 账号、组织与浏览器

- 账号 `yangkj`（宿主显示名 杨康健），组织 `01`；浏览器内实测 token 声明 `"org":"01"`、`"sub":"medical-recognition"`。医院与院区范围以登录返回为准，本轮未另行核对。
- 浏览器：Google Chrome 153.0.0.0（`Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/153.0.0.0 Safari/537.36`），语言 `zh-CN`，初始视口 1365×767、设备像素比 1。
- 控制工具：Chrome DevTools MCP（页面标识 1 / 3 / 4 三个标签）。
- 会话标识：宿主开发设置页显示 `Session：session-1ec0d47b-2f3e-4e28-a4d6-c10d094e9150`。

#### 入口与拦截生效证据

从宿主登录页 `http://183.224.180.166:35000/login` 登录：账号填 `yangkj`、入口选「检验检查结果互认平台」、密码取自 [Test Environment](../../../.agents/instructions/test-environment.md) 第 1 节唯一登记处，提交后跳转 `http://183.224.180.166:35000/`。菜单链路为父项「检验检查结果互认」→ 子项「互认项目」。

进入前在 `/dev-settings` 核对拦截映射，实测原文：

```json
{"enabled":"true","rows":"[{\"matchKey\":\"medical-recognition\",\"origin\":\"http://localhost:3008\",\"enabled\":true},{\"matchKey\":\"lis-center\",\"origin\":\"http://localhost:3002\",\"enabled\":true}]"}
```

`medical-recognition` 行的 `origin` 为 `http://localhost:3008`、`enabled` 为 `true`，全局开关为 `"true"`，与 Test Environment 第 4 节的映射一致。

生效验证按第 4 节的两项要求在 Network 与 Console 同一链路核对：

- Network 实测子应用资源来自本地端口：`reqid=99 GET http://localhost:3008/subApps/medical-recognition/@vite/client [200]`、`reqid=100 GET .../src/main.tsx [304]`、`reqid=107 GET .../node_modules/.vite/deps/@dy_micro-sdk.js?v=86425725 [200]`，带 HMR 版本时间戳。
- Console 实测 `msgid=5 [debug] [vite] connected.`。
- 子应用入口请求为 `reqid=87 GET http://localhost:3008/subApps/medical-recognition/recognition-projects [200]`。

#### 数据前提与业务足迹

组织 `01` 下取证前的现存数据：`本组织共 249 项配置`，四栏列视图渲染 429 个项目行（含未配置行）。本轮通过**真实业务页面**产生的足迹如下，全部经页面操作，未直接写库。

| 对象 | 操作前 | 本轮操作 | 操作后 |
|---|---|---|---|
| 配置 `RB3-001`、`RB3-002`、`RB3-061` | 未配置 | 页面「新增配置」弹窗勾选 3 项、统一 30 天、确定新增 | 已配置且启用、30 天 |
| 配置 `RB3-003` | 未配置 | 同上批量新增（15 天）；随后行内编辑保存 28 天并再次同值保存 | 已配置且启用、28 天 |
| 配置 `RB3-004`、`RB3-005` | 未配置 | 由另一个宿主标签批量新增（15 天） | 已配置且启用、15 天 |
| 配置 `RB-RC-B-20260929-060` | 未配置 | 未创建（作为被拒绝批次中的合法项参与整批拒绝） | 仍未配置 |
| 配置 `RB3-007`、`RB3-008` | 未配置 | 复验轮批量新增（12 天） | 已配置且启用、12 天 |
| 配置 `RB3-009`、`RB3-010` | 未配置 | 复验轮批量新增（33 天） | 已配置且启用、33 天 |
| 配置 `RB3-011` | 未配置 | 复验轮由另一宿主标签批量新增（9 天） | 已配置且启用、9 天 |
| 配置 `RB3-012` | 未配置 | 复验轮作为被拒批次中的合法项参与整批拒绝，未创建 | 仍未配置 |
| 标准项目 `RB3-005`、`RB3-006` | 目录有效 | 在「标准项目目录维护」页停用，取证后逐项恢复启用 | 恢复为有效，页面实测「有效」 |
| 配置计数 | 249 | — | 260（首轮末 255，复验轮末 260） |

标准项目的停用与启用均走宿主「标准项目目录维护」页的真实确认弹窗（弹窗正文「即将停用标准项目 窗口取证 RB3-005。该操作只修改当前数据状态，不会级联修改下级状态。」与「即将启用标准项目 …」），未执行任何直接写库语句，未执行 DDL。

#### 逐条用例证据

**C26 列视图全平铺 — `Passed`**

- 操作：从宿主菜单进入页面，读取列视图 DOM 与计算样式；在 1365、900、620 三个视口宽度下重复读取；在最窄视口把外层滚动容器滚到最右端。
- 四栏恒定全显：表头实测四个元素依次为 `类型`、`分类`、`分组`、`项目`，class 均为 `recognition-projects-cascade-head`，行内声明 `gridColumn` 取 1/2/3/4。
- 无折叠或展开控件：页面内全部 509 个按钮的文本中，匹配「折叠／展开／collapse／expand」的结果为空数组，实测 `collapseControls: []`。
- 纵向跨行：类型格行内样式 `grid-area: 2 / 1 / span 3;`，分类格 `grid-area: 2 / 2 / span 1;`、`grid-area: 3 / 2 / span 1;`，分组格 `grid-area: 2 / 3;`（每行一格，无跨度）。实测高度类型格 21894px、各分类格与项目格 8676px，类型格纵向覆盖其下全部分类与分组行。
- 窄视口横向滚动、列宽不压缩：容器计算样式 `overflow-x: auto`。1365 宽视口下 `clientWidth 1085 / scrollWidth 1085`；900 宽视口下 `clientWidth 620 / scrollWidth 1060`、`gridTemplateColumns "88px 186px 208px 578px"`；620 宽视口下 `clientWidth 340 / scrollWidth 1060`。把 `scrollLeft` 置为 99999 后实测 `scrollLeft 720`（等于最大可滚动量），滚到最右时表头「项目」的横坐标回到 7px，前三栏横坐标为负值，即前三栏被滚出可视区而非压缩。文档宽度实测 `scrollWidth 620 / clientWidth 620`，横向滚动由表格区容器承载，未扩散到整页。
- 结论：该用例声明的 Component 与 Host 两个层级的可观察行为在真实宿主全部成立。

**C27 层级高亮 — `Passed`**

- 层级三层成立：点击类型格 `检验` 后高亮层级格 7 个（类型 1 ＋ 分类 3 ＋ 分组 3），即该类型格与其名下全部分类格、分组格同时高亮；高亮格计算背景为 `rgb(230, 244, 255)`。点击分类格 `S1-C35-AbC-20260915` 后仅该分类格与其名下分组格 `c34_100%_GRP` 高亮（分类 1 ＋ 分组 1），类型格不亮。点击分组格 `c34_100%_GRP` 后仅该分组格高亮（分组 1），三者互斥不累加。点击页面其他区域（标题文本「标准项目互认配置」）后高亮归 0。
- 项目层高亮成立：点击分组格 `c34_100%_GRP` 后，该分组对应的项目格实测 `class` 为 `recognition-projects-cascade-items recognition-projects-level-highlighted`、计算背景 `rgb(230, 244, 255)`，未高亮分组下的项目格对照取值为 `rgba(0, 0, 0, 0)`。点击类型格后 3 个项目格全部高亮，点击分类格后 1 个项目格高亮，与所属分组的高亮范围逐行一致。项目格内项目行 `class` 仍为 `recognition-projects-row`，高亮由所属项目格承载。
- 零写请求成立：在页面安装 `fetch` 与 `XMLHttpRequest.prototype.open` 观测器后执行上述全部点击，观测数组始终为空 `[]`；整个取证期间 Network 未出现任何写端点请求。

**取证过程记录（如实登记）**：本条首轮取证（2026-09-30 00:20 前）时项目层无高亮表现，当时按 `Failed` 记录，判据是项目格与项目行背景均为 `rgba(0, 0, 0, 0)`、全页 `[class*="row-highlighted"]` 命中 0。首轮取证结束后，`RecognitionProjectCascade.tsx`（00:26:51）、`RecognitionProjects.css`（00:28:10）、`RecognitionProjects.test.tsx`（00:30:41）由实施侧继续改动，为项目格补上高亮类与对应样式（`RecognitionProjectCascade.tsx:211-213`、`RecognitionProjects.css:29-32`）。测试与证据主责智能体随后对当前磁盘代码重新取证，取得上述项目层高亮证据，本条改记为 `Passed`。首轮诊断的两条源码依据在改动后仍成立：`RecognitionProjects.tsx:504` 的 `renderRow` 仍未接收第二个参数，项目格高亮因此直接施加在项目格容器上，不经由 `renderRow` 传递；`RecognitionProjectRow` 属性表仍无高亮入参。

**C28 状态与时间行内展示 — `Passed`**

- 四种状态徽标在同一页面同一轮实测计数：`启用` 253 行（`ant-tag-success`）、`停用` 1 行（无颜色类，取默认色）、`未配置` 174 行（`ant-tag-default`）、`目录已停用` 1 行（`ant-tag-error`）。
- 目录停用原因：标准项目 `RB3-005` 在目录维护页停用后，该行实测徽标文本 `目录已停用`、原因元素文本 `标准项目已停用`、可互认时间仍显示 `15 天`、启停入口保留。目录恢复启用后该行回到 `启用`、原因消失。
- 点击时间单元格就地进入编辑：点击 `aria-label="修改可互认时间：窗口取证 RB3-003"` 的按钮后，行内出现 `span.recognition-projects-days-editor`，含 `<input aria-label="可互认时间（天）" inputmode="numeric">`（初值 `15`，与行上当前值一致，且进入即获得焦点）、`保存可互认时间：窗口取证 RB3-003` 与 `取消修改可互认时间：窗口取证 RB3-003` 两个按钮。点击时观测数组为 `[]`。
- 非法输入零写并保留输入：输入 `0` 后点保存，观测数组为 `[]`；编辑器与输入框保留（`editorStillPresent: true`、`inputValue: "0"`），行内出现提示 `可互认时间输入不合法` 与 `可互认时间为 1 到 2147483647 之间的整数天数`。
- 合法保存：输入 `28` 点保存，Network 实测 `reqid=200 POST /Api/MedicalRecognitionReport/UpdateMutualRecognitionItemConfiguration [200]`，请求体 `{"id":"3a240018-4a05-9627-8c30-dcb0255ded54","recognitionDurationDays":28}`，响应体 `true`。保存后编辑器收起、行上显示 `28 天`，并触发列表与目录两次读取（`reqid=202`、`reqid=203`）。
- 同值保存：再次进入编辑，输入框回填当前值 `28`；直接点保存仍发出请求，`reqid=205 POST /Api/MedicalRecognitionReport/UpdateMutualRecognitionItemConfiguration [200]`，请求体 `{"id":"3a240018-4a05-9627-8c30-dcb0255ded54","recognitionDurationDays":28}`，响应体 `true`；随后 `reqid=207`、`reqid=208` 重载列表与目录。即同值不短路，且刷新范围是整表来源全量重载。

**C29 弹窗多选新增 — `Passed`**

- 候选集合：点击「新增配置」打开弹窗，标题实测 `新增互认项目配置`。弹窗内列视图表头同样为 `类型｜分类｜分组｜项目`，项目行 180 条。与页面列表逐编码核对：页面内 249 条已配置（含 1 条停用配置）编码在弹窗中出现 0 条（`configuredLeakIntoModal: []`）；页面内 180 条未配置编码在弹窗中缺失 0 条（`unconfiguredMissingFromModal: []`）。标准项目 `RB3-005` 目录停用后重开弹窗，其行不再出现在候选中（`hasRB3005: false`），候选数由 180 变为 174。
- 逐项勾选：行内勾选框 `aria-label` 为 `选择标准项目：窗口取证 RB3-001（RB3-001）`；勾选 3 项后底部计数实测 `已选 3 项`。
- 分组整组勾选：点击 `aria-label="整组勾选分组「S1-RETRY-GRP-20260914-001」及其下级"` 后计数为 `已选 60 项`、行勾选数 60，与该分组的项目数一致；再点一次回到 `已选 0 项`。点击分类格整组勾选后，其所属分组与类型格进入半选态（`indeterminate: true`）或全选态。点击 `aria-label="整组勾选类型「检验」及其下级"` 后计数为 `已选 180 项`，各分类格与分组格同步为全选态。数量关系与 DOM 层级一致（类型 180 = 分类 120 + 60）。
- 统一天数与确定：设统一天数 `30` 后点「确定新增」，Network 实测 `reqid=184 POST /Api/MedicalRecognitionReport/CreateMutualRecognitionItems [200]`，请求体 `{"recognitionDurationDays":30,"standardProjectCodes":["RB3-001","RB3-002","RB3-061"]}`，响应体 `true`；随后 `reqid=185`、`reqid=187` 重载配置列表与标准目录。弹窗关闭，页面计数由 `本组织共 249 项配置` 变为 `252`。
- 默认启用与回读：刷新后三行实测均为 `启用`、`30 天`。重开弹窗，三条已创建编码在候选中出现 0 条，计数与天数复位为 `已选 0 项` 与空值。
- 非法天数零写：输入 `0` 后点「确定新增」，观测数组为 `[]`（未发出请求），弹窗保留、输入保留为 `0`、已勾选 3 项保留，弹窗内提示 `无法提交` 与 `可互认时间为 1 到 2147483647 之间的整数天数`。
- **复验轮（当前构建，2026-09-30 00:50 后）**：重启后的后端与当前页面代码下重取该链路。弹窗候选实测 174 条；勾选 `RB3-007`、`RB3-008` 并设统一 12 天后提交，Network 实测 `reqid=458 POST /Api/MedicalRecognitionReport/CreateMutualRecognitionItems [200]`，随后 `reqid=460`、`reqid=461` 重载配置列表与标准目录；页面计数由 `本组织共 255 项配置` 变为 `257`，两行实测均为 `启用`、`12 天`。重开弹窗候选降至 172 条，两条已创建编码在候选中出现 0 条。

**C30 批量新增失败口径 — `Passed`**

- 构造方式（首轮）：两个宿主标签同持载入时的候选快照。标签 A 勾选 `RB3-003`、`RB3-004`、`RB3-005`、`RB-RC-B-20260929-060` 四项（跨两个分组）并设 45 天；标签 B 先创建 `RB3-003`、`RB3-004`、`RB3-005`（15 天）成功，使标签 A 的选中集合含已被其他页面创建的项目。
- 提交与整批拒绝：标签 A 点「确定新增」，Network 实测 `reqid=190 POST /Api/MedicalRecognitionReport/CreateMutualRecognitionItems [500]`，请求体 `{"recognitionDurationDays":45,"standardProjectCodes":["RB3-003","RB3-004","RB3-005","RB-RC-B-20260929-060"]}`，响应体关键片段原文：

```
System.InvalidOperationException: 业务拒绝：该组织已配置以下标准项目：RB3-003、RB3-004、RB3-005。
   at Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers.MedicalRecognitionReportManager.CreateMutualRecognitionItemsAsync(CreateMutualRecognitionItemsCommand command) in ...\MedicalRecognitionReportManager.cs:line 307
```

- 不部分写入：提交后立即点「重新读取互认配置」，回读实测计数由 `252` 变为 `255`（新增的仅为标签 B 创建的三条），`RB-RC-B-20260929-060` 仍为 `未配置`；`RB3-003`、`RB3-004`、`RB3-005` 为 `启用`、`15 天`（即标签 B 写入的值，未被标签 A 的 45 天覆盖）。整批拒绝后无任何一行来自被拒绝批次。
- 弹窗与选择保留：提交后弹窗仍打开，计数 `已选 4 项`、天数 `45`、四项勾选全部保留，弹窗内无局部错误文案（`localErrorInModal: null`），与 [frontend-api-client](../../../.agents/instructions/frontend-api-client.md) 第 5 节「前端不解析错误内容」的口径一致。
- 列表不因失败刷新：提交后页面计数维持 `本组织共 252 项配置` 不变，Network 中失败请求之后未出现新的 `QueryRecognitionProjectConfigurationList` 或 `QueryEffectiveMedicalStandardCatalog`。
- 违规编码的可见性：按本节开头「C30 与 V30 的关系」，违规编码由后端错误内容承载，从 Network 响应体取证（见上），页面不显示它，本用例不以页面显示为判据。
- **复验轮（当前构建，2026-09-30 01:00 后）**：重启后的后端与当前页面代码下重取该链路。标签 A 勾选 `RB3-011`、`RB3-012` 并设 40 天；标签 B 先创建 `RB3-011`（9 天）成功（计数 259→260）。标签 A 提交时实测 `reqid=470 POST /Api/MedicalRecognitionReport/CreateMutualRecognitionItems [500]`，请求体 `{"recognitionDurationDays":40,"standardProjectCodes":["RB3-011","RB3-012"]}`，响应体关键片段原文：

```
System.InvalidOperationException: 业务拒绝：该组织已配置以下标准项目：RB3-011。
   at Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers.MedicalRecognitionReportManager.CreateMutualRecognitionItemsAsync(CreateMutualRecognitionItemsCommand command) in ...\MedicalRecognitionReportManager.cs:line 307
```

- 复验轮的不部分写入：提交后弹窗与选择保留（计数 `已选 2 项`、天数 `40`、两项勾选保留），页面计数维持 `本组织共 259 项配置` 不变；再点「重新读取互认配置」后回读为 `260`，`RB3-011` 为 `启用 / 9 天`（标签 B 的值，未被 40 天覆盖），`RB3-012` 仍为 `未配置`。**该轮取证额外覆盖了「同一批次内合法项亦不写入」**：`RB3-012` 在批次内目录有效且未被配置，仍随整批拒绝而零写入。

**V28 批量新增成功 — `Passed`（宿主面）**

- 引用 C29 的 `reqid=184` 证据：一条请求建立 3 条配置、响应 `true`、全部默认启用、列表可回读到 `启用 / 30 天`。跨分类与跨分组未在本批覆盖（本批 3 条同属类型 `检验`、分类 `S1-C35-AbC-20260915`、分组 `c34_100%_GRP`），该子面在 V28 行声明的「跨分类、跨分组编码」前提上未取证，登记为未覆盖面。
- 后端事件登记的可见性沿用既有 `Blocked` 口径（见 V24 行），本轮不重复裁定。

**V29 批量入参口径 — `Passed`（离线面）**

- 空集合、单批超 1000、元素空白、批内重复编码，以及边界取值 0、1、1000、1001 由离线用例覆盖。本轮实测 `dotnet test --filter "FullyQualifiedName~Stage2WritePathTests"` 为 **69 通过 / 0 失败 / 0 跳过**，其中包含 `Batch_create_rejects_invalid_code_collections_before_any_read` 与 `Batch_create_upper_boundary_is_enforced_at_the_collection_level`。
- 宿主面按矩阵声明层级（Contract/Domain）未覆盖：这些入参在真实宿主页面不可构造（页面勾选与统一天数输入无法产生空集合、超上限或批内重复），按 Testing Baseline §1 第 8 条不作为普通用例构造，登记为不在宿主面覆盖。

**V30 批量目录有效性逐码校验 — `Passed`（宿主面）**

- 构造：标签 A 打开新增弹窗并勾选 `RB3-006`、`RB3-007`、设 21 天；在「标准项目目录维护」页把 `RB3-006` 停用，使弹窗选中集合包含目录停用层的项目；随后在标签 A 提交。
- 结果：Network 实测 `reqid=210 POST /Api/MedicalRecognitionReport/CreateMutualRecognitionItems [500]`，请求体 `{"recognitionDurationDays":21,"standardProjectCodes":["RB3-006","RB3-007"]}`，响应体关键片段原文携带按原因分组的违规编码：

```
System.InvalidOperationException: 业务拒绝：标准项目目录不可用于新建互认项目配置：标准项目已停用：RB3-006。
   at Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers.MedicalRecognitionReportManager.ValidateStandardCatalogForBatch(IReadOnlyList`1 standardProjectCodes, IReadOnlyDictionary`2 statesByCode) in ...\MedicalRecognitionReportManager.cs:line 403
```

- 零写入：提交后页面计数维持 `本组织共 255 项配置` 不变，`RB3-006`、`RB3-007` 均仍为 `未配置`；弹窗保留、计数 `已选 2 项`、天数 `21`、两项勾选保留。
- 未覆盖面：违规原因分组的其余三类（编码不存在、所属分类停用、所属分组停用）在宿主面未构造，仅取证到「标准项目已停用」这一类。

**V31 批量与库内配置查重 — `Passed`（宿主面）**

- 引用 C30 的 `reqid=190` 证据：查重发生在写入前，遇已配置编码即整批拒绝，错误内容报告编码 `RB3-003、RB3-004、RB3-005`，零行写入（回读证据见 C30 的不部分写入项）。
- 含停用配置的查重子面未在本轮宿主面构造；离线用例 `Batch_create_rejects_codes_already_configured_in_the_organization` 以 `existingIsValid` 两个取值覆盖启用与停用配置。

**V32 批量并发唯一冲突 — `Passed`（复用既有轮次证据）**

- 本轮不重复构造。既有结论为：并发批次撞唯一索引后整批回滚、无残留（见本报告「已确认的实施口径」一节对真实库回滚证据的登记）。
- 复用边界：该证据来自本重开轮实施阶段的真实库取证，非本轮新执行；本轮未对该面取得新证据，也未改写原登记。

**V33 批量读取无 N+1 — `Passed`（离线面）**

- 属离线面，按任务范围不在宿主面重复。本轮实测后端全量 `dotnet test --no-build` 为 **810 通过 / 0 失败 / 0 跳过**，其中 `Batch_create_reads_catalog_and_configurations_once_each_regardless_of_batch_size` 断言目录与配置各一次批量读取（`Assert.Equal(1, repository.CatalogBatchReadCalls)`、`Assert.Equal(1, repository.ConfigurationBatchReadCalls)`）。

#### 本轮运行证据、离线证据与复用证据的区分

| 类别 | 覆盖条目 | 说明 |
|---|---|---|
| 本轮宿主运行证据（首轮） | C26、C27、C28、C29、C30、V28、V30、V31 | 从宿主登录页与菜单进入，同一链路核对页面、DOM、Console、Network、响应与最终业务状态；对应进程 36888 与 43752，**早于** 23:59 与 00:26 的源码改动 |
| 本轮宿主运行证据（复验轮） | C27、C29、C30 | 后端重建重启（进程 5696）后对当前磁盘代码重取；C28 的读取面在该轮亦随页面正常加载（计数 255），未逐项重取 |
| 本轮离线运行证据 | V29、V33 | 后端测试复验轮实跑：全量 811 通过；前端全量 415 通过 / 2 跳过 / 21 文件通过 |
| 复用既有轮次证据 | V32 | 引自本重开轮实施阶段的真实库回滚证据，本轮未重取 |
| 本轮未取证/未覆盖 | 见下方「未覆盖面」 | 逐条登记原因 |

前端实跑命令与结果（在 `client/` 工作目录执行 `pnpm -F dy-medical-recognition test --run`）：首轮 `Test Files  21 passed (21)`、`Tests  414 passed | 2 skipped (416)`、`Duration 57.51s`；**复验轮（含 00:30 新增的组件层守卫）** `Test Files  21 passed (21)`、`Tests  415 passed | 2 skipped (417)`、`Duration 66.68s`。两次均为 exit code 0。

后端实跑命令与结果（在仓库根目录执行）：首轮目标过滤 `dotnet test ... --no-build --filter "FullyQualifiedName~Stage2WritePathTests"` → `已通过! - 失败: 0，通过: 69，已跳过: 0，总计: 69`；首轮全量 `--no-build` → `已通过! - 失败: 0，通过: 810，已跳过: 0，总计: 810`；**复验轮全量（含编译，含 00:00 新增的请求层上限用例）** → `已通过! - 失败: 0，通过: 811，已跳过: 0，总计: 811`。首轮 `--no-build` 的 810 对应 23:41 的构建产物；复验轮的 811 对应当前磁盘代码。

#### 重开轮矩阵结果

| 用例编号 | 验证面 | 状态 | 证据 | 未覆盖面/原因 |
|---|---|---|---|---|
| C26 | 列视图全平铺（Component/Host） | Passed | **补证轮重取（2026-09-30，当前构建）**：四栏表头文本 `类型｜分类｜分组｜项目`、`gridColumn` 依次 `1/2/3/4`；533 个按钮中折叠/展开控件 0 个、`[aria-expanded]` 0 个；类型格 `grid-area: 2 / 1 / span 4`（高 21953px）与 4 个分类格、4 个分组格、4 个项目格逐格对齐；900 与 620 宽视口下 `overflow-x: auto`、`scrollWidth 1060 > clientWidth`、`gridTemplateColumns` 恒为 `88px 186px 208px 578px`，滚到最右（`scrollLeft` 440 / 720）后前三栏横坐标为负而项目栏回到 `7`，文档根元素不横向滚动。详见「重开轮补证轮（2026-09-30）」的「重取证据一」 | — |
| C27 | 层级高亮（Component/Host） | Passed | **首次宿主取证记 `Failed`**：层级三格高亮与零写请求均成立，但项目层无任何高亮（项目格与项目行背景 `rgba(0, 0, 0, 0)`、与未高亮对照组相同、`[class*="row-highlighted"]` 命中 0），与 `Client/design.md` 第 9 行「整条下级链路（类型→分类→分组→项目）」不一致。**成因**：项目格未随所属分组格高亮（`RecognitionProjectCascade.tsx` 只把高亮状态传给层级格）。**修正**：项目格在所属分组格高亮时复用同一高亮类名一并高亮。**修正后复验（2026-09-30 00:35，真实宿主）**：点分组格 → 该分组层级格＋**其项目格**高亮（计算背景 `rgb(230, 244, 255)`），其余项目格保持透明；点其他区域 → 层级格与项目格全部归 0；点分类格 → 该分类格＋其分组格＋对应项目格；点类型格 → 该类型下 7 个层级格与 3 个项目格全部高亮；全程 `fetch` 观测写请求数组为 `[]`。**组件层新增守卫**：「项目格随所属分组格一起高亮，未点中的分组项目格保持不高亮」按 DOM 顺序比对四个分组项目格在五种点击下的整份高亮状态（修正前失败、修正后通过） | 修正前取证的项目层高亮缺失已闭环；修正后以组件层用例与宿主复验为据 |
| C28 | 状态与时间行内展示（Component/Host） | Passed | **补证轮重取（2026-09-30，当前构建）**：四徽标同页同轮计数 `启用 259 / 停用 1 / 未配置 170 / 目录已停用 1`；目录停用行徽标 `目录已停用`（含 `ant-tag-error`）、原因 `标准项目已停用`、可互认时间仍显示 `19天`、修改与启停入口均保留；点击时间进入行内编辑（`inputmode=numeric`、回填当前值 `70`、进入即获焦点、含保存与取消入口）；非法 `0` 与 `-5` 均零写、保留输入、给出 `可互认时间输入不合法` 与 `可互认时间为 1 到 2147483647 之间的整数天数`；合法 `45`（`reqid=1071`）与同值 `45`（`reqid=1076`）各发一次 `200/true` 并按整表来源重载；取证后已恢复为 `70` 并回读确认。详见「重开轮补证轮（2026-09-30）」的「重取证据二」 | — |
| C29 | 弹窗多选新增（Component/Host） | Passed | 首轮：候选 180 条；已配置编码进入候选 0 条、未配置编码缺失 0 条；目录停用项不出现在候选；分组整组勾选得 60、类型整组勾选得 180、再点取消；`reqid=184` 建立 3 条并全部启用；计数 249→252、已创建不再可选、选择与天数复位；非法天数零写并保留。**复验轮（当前构建）**：`reqid=458` 建立 2 条（12 天）、计数 255→257、两行回读 `启用 / 12 天`、候选 174→172 且已创建编码出现 0 条 | — |
| C30 | 批量新增失败口径（Component/Host） | Passed | 首轮：重叠编码批次 `reqid=190` 得 `500`，错误内容报告 `RB3-003、RB3-004、RB3-005`；回读 252→255 且被拒批次无一行写入、`RB-RC-B-20260929-060` 仍为未配置；弹窗、`已选 4 项`、`45` 天与勾选全部保留；失败后无刷新请求。**复验轮（当前构建）**：`reqid=470` 得 `500` 并报告 `RB3-011`；计数维持 259 不变、弹窗与 `已选 2 项`、`40` 天、勾选保留；回读后 `RB3-011` 为 `9 天`（未被 40 天覆盖）、批内合法项 `RB3-012` 仍 `未配置` | 页面不显示违规编码（由后端错误承载，已从 Network 响应取证） |
| V28 | 批量新增成功（Domain/Repository/Application） | Passed | 首轮宿主 `reqid=184` 一条请求建立 3 条、响应 `true`、默认启用、列表回读 `启用 / 30 天`；**复验轮 `reqid=458` 在当前构建上再建 2 条（`RB3-007`、`RB3-008`，12 天），计数 255→257、两行回读 `启用 / 12 天`、重开弹窗候选 174→172** | 跨分类、跨分组编码子面两轮批次均未覆盖（两批各自 3 条与 2 条都同属一个分类与一个分组）；事件登记可见性沿用 V24 的 `Blocked` 口径 |
| V29 | 批量入参口径（Contract/Domain） | Passed | 本轮离线实跑 `Stage2WritePathTests` 69 通过（首轮），覆盖非法集合与 0/1/1000/1001 边界用例；**复验轮后端全量 811 通过**，含 00:00 新增的请求层上限用例 `Batch_create_request_rejects_code_collections_above_the_declared_limit` | 宿主面不覆盖：该类入参在真实页面不可构造，按 Testing Baseline §1 第 8 条不构造 |
| V30 | 批量目录有效性逐码校验（Domain） | Passed | **补证轮重取（2026-09-30，当前构建），四类违规原因全部构造成功**：混合批次一次提交 5 条（1 条合法 + 编码不存在 / 分类停用 / 分组停用 / 项目停用 各 1 条）得 `500`，错误原文按原因分组携带各自编码 `…编码不存在：SP30-ITEM-NOT-EXIST-0930111420；所属分类已停用：SP30-ITEM-CATDISABLED-0930111420；所属分组已停用：SP30-ITEM-GRPDISABLED-0930111420；标准项目已停用：SP30-ITEM-SELFDISABLED-0930111420。`；提交前后本组织配置集合计数均为 `260` 且编码逐一比对完全一致，五条待提交编码落库数为 `0`（含批内合法项）；服务端日志实测该请求事务 `已开启 → 已回滚 → 已全部回滚`。详见「重开轮补证轮（2026-09-30）」的「重取证据三」 | 零事件的真实运行面本环境无观察点，沿用 V24 的 `Blocked` 定级；由离线用例 `Batch_create_collects_every_catalog_violation_grouped_by_reason` 的 `Assert.Empty(events.Events)` 与 `Assert.Equal(0, repository.CreateCalls)` 覆盖 |
| V31 | 批量与库内配置查重（Domain/Repository） | Passed | 首轮宿主 `reqid=190` 写入前查重拦截并整批拒绝、报告编码；回读无部分写入。**复验轮 `reqid=470` 在当前构建上重取同一路径**：报告 `RB3-011`、弹窗与选择保留、计数维持 259 不变、回读后 `RB3-011` 为标签 B 的 `9 天`、`RB3-012` 仍 `未配置` | 停用配置占用的唯一键查重在宿主面未构造，由离线用例 `Batch_create_rejects_codes_already_configured_in_the_organization` 覆盖 |
| V32 | 批量并发唯一冲突（Repository/Application） | Passed | 复用本重开轮实施阶段的真实库证据：并发批次撞唯一索引后整批回滚、无残留 | 本轮未重复构造，未取得新证据 |
| V33 | 批量读取无 N+1（Application/Domain/Repository） | Passed | 本轮离线实跑后端全量（首轮 810、复验轮 811）通过，含目录与配置各一次批量读取的计数断言 | 属离线面，按矩阵声明层级不在宿主面重复 |

本轮新增 11 条：首轮宿主取证时 `Passed` 10 条、`Failed` 1 条（C27）；C27 的项目层高亮缺陷已修正并在复验轮经真实宿主复验通过，**该条现为 `Passed`**。重开轮新增矩阵的最终结果为 `Passed` 11 条、`Failed` 0 条。**补证轮（2026-09-30）**在重开轮终态工作区上重取了 C26、C28、V30，使重开轮新增矩阵的全部 11 条证据都对应同一份代码快照；逐条证据与本轮边界见「重开轮补证轮（2026-09-30）」。

#### 未覆盖面

| 事项 | 状态与原因 |
|---|---|
| ~~C27 项目层高亮~~ | **已闭环**：首次取证记 `Failed`，原因为项目格未随所属分组格高亮。修正后组件层新增守卫用例（修正前失败、修正后通过），并在真实宿主复验通过（点分组格/分类格/类型格分别点亮对应项目格、点其他区域归 0、全程零写请求）。详见「矩阵结果」的 C27 行 |
| ~~V30 另三类违规原因~~ | **已闭环**：补证轮在真实后端与真实 PostgreSQL 上把「编码不存在 / 分类停用 / 分组停用 / 项目停用」四类全部构造成功，一次批次即报告齐全，见「重开轮补证轮（2026-09-30）」的「重取证据三」 |
| ~~V30 与 C28 在终态构建上的重取~~ | **已闭环**：补证轮在同一构建上重取，见「重开轮补证轮（2026-09-30）」的「重取证据二」「重取证据三」 |
| V28 跨分类、跨分组编码批次 | 首轮批次 3 条与复验轮批次 2 条均同属一个分类与一个分组；补证轮批次为单条；该子面三轮都未取证（离线用例已覆盖跨分类跨分组编码） |
| V31 停用配置占用的唯一键查重 | 宿主面未构造；离线用例以 `existingIsValid` 两个取值覆盖 |
| V32 并发唯一冲突复现 | 复用既有真实库证据，重开轮与补证轮均未重取（该写入路径此后未改动） |
| 事件登记与处理可见性 | 沿用 V24、V26 的 `Blocked` 定级，重开轮与补证轮均不重复裁定 |
| C17、V24、V26、V27 | 沿用既有 `Blocked` 定级，重开轮与补证轮均不重复裁定、未改为通过 |
| C27、C29、C30、V28、V31 的进程内重取 | 未在补证轮进程上重取；其证据来自重开轮复验轮，代码快照与补证轮一致 |

#### 问题清单

| 编号 | 问题 | 依据与证据 | 影响 |
|---|---|---|---|
| P1 | 层级高亮未覆盖项目层，与前端设计不一致（**已修正并复验，问题关闭**） | [Client/design.md](Client/design.md) 第 9 行要求高亮「整条下级链路（类型→分类→分组→项目）」；首次宿主实测项目格与项目行背景透明、无高亮类名，与未高亮对照组取值相同；源码核对：`RecognitionProjectCascade.tsx` 把高亮状态传给了层级格，但项目格自身未加高亮类名 | 首次记 C27 `Failed`；修正后项目格随所属分组格一并高亮，组件层用例与真实宿主复验均通过，该问题关闭 |
| P2 | 本轮取证期间工作区被并发改动，首批宿主证据与期间源码不对应（**已通过复验轮与补证轮处置**） | 首轮证据对应进程 36888/43752，二者启动早于 `CreateMutualRecognitionItemsRequest.cs`（23:59:25）、`Stage2WritePathTests.cs`（00:00:55）、`RecognitionProjectCascade.tsx`（00:26:51）、`RecognitionProjects.css`（00:28:10）、`RecognitionProjects.test.tsx`（00:30:41）的改动 | C27/C29/C30/V28/V31 已在重建重启后的当前构建上重取；**V30 与 C28 已在补证轮（2026-09-30）于重开轮终态工作区上重取**，至此重开轮新增 11 条证据都对应同一份代码快照 |

#### 残余风险

- 本轮取证期间为构造目录停用前置，经「标准项目目录维护」页两次停用并两次恢复标准项目 `RB3-005` 与 `RB3-006`，页面上两者已恢复为「有效」。目录项的启用与停用属状态变更，虽不影响项目配置数据，仍作为本轮业务足迹登记。
- C30 与 V30 使用的两页并发与目录变更前置，依赖宿主标签持有载入时的候选快照；该方式属正常业务可达的时序竞争，未直接改库，也未构造业务不可达数据。
- V32 的证据来自本重开轮实施阶段，与本轮宿主取证不在同一时间点；引用时未改动原登记，未把它描述成本轮新执行的完整轮次。
- 本轮未执行 `gitnexus detect-changes`：本任务只写阶段测试报告，不涉及代码 symbol 变化。
- **C27 的项目层高亮修正发生在宿主取证之后**，因此该条宿主证据分两段：层级三格与零写请求来自修正前取证，项目层高亮来自修正后的复验（2026-09-30，同一宿主会话）。两段证据都在真实宿主取得，但**不是同一次连续轮次**。
- **本轮取证跨越了并发改动**：首轮证据对应 23:41 的构建产物与改动前的页面代码，复验轮证据对应重建重启后的当前代码。两轮的用例结论一致，但整体证据**不构成一次连续干净的单一构建轮次**；C26 与 C28 的首轮证据、V30 的首轮证据仍对应旧构建，未受影响路径未重取。
- 复验轮重建后端时，运行中的后端进程锁定了三个被引用工程程序集，构建以 `MSB3027`/`MSB3021` 失败；停止该进程后增量重建通过并按原脚本重启。停止与重启期间宿主页面短暂不可用（读取失败），未观察到数据影响。全程未使用含 Clean 的 `-t:Rebuild`。
- 复验轮的前端 dev server（进程 15600）由实施侧启动，非本轮启动，按 Testing 第 4 节「只关闭本轮自行启动的进程」保留运行。

## 重开轮补证轮（2026-09-30）

本节是一次**只补证据**的轮次：目标是把重开轮新增矩阵（V28-V33、C26-C30）的宿主与真实运行证据收敛到**一次连续干净的单一构建轮次**。本轮**未改设计、未改实现、未改测试**，改动面只有本报告。

### 轮次目的与触发原因

重开轮实施轮（批次20）的宿主取证跨越了实施侧的并发改动：首轮证据对应进程 36888/43752，二者启动早于 `CreateMutualRecognitionItemsRequest.cs`（23:59:25）、`Stage2WritePathTests.cs`（00:00:55）、`RecognitionProjectCascade.tsx`（00:26:51）、`RecognitionProjects.css`（00:28:10）、`RecognitionProjects.test.tsx`（00:30:41）五处改动，因此 C26、C28 与 V30 的宿主证据只覆盖改动前的代码，整体证据不构成干净轮次。本轮在同一构建上把这些面重取一遍。

### 本次取证对应的代码快照

工作区在取证开始与结束时一致（`git status --porcelain --untracked-files=all` 均为 46 个变更文件），起始提交 `0a96f34`。关键文件 SHA256 前 16 位：

| 文件 | SHA256 前 16 位 |
|---|---|
| `RecognitionProjectCascade.tsx` | `8CFAE13B61D7BE08` |
| `RecognitionProjectRow.tsx` | `C130B2CC9E718D51` |
| `RecognitionProjectModals.tsx` | `53F323BD4D2A04CB` |
| `RecognitionProjects.tsx` | `6F2FD93866214087` |
| `RecognitionProjects.css` | `7B0510F6734816ED` |
| `recognitionProjectsApi.ts` | `DD7C1C4FA168AD61` |
| `MedicalRecognitionReportManager.cs` | `615FD8B53F72AA47` |
| `MedicalRecognitionReportAppService.cs` | `E5CD213BB25FD46A` |
| `CreateMutualRecognitionItemsRequest.cs` | `D28DE5A22A8CD7AB` |

**与重开轮复验轮的快照差异（如实登记）**：`recognitionProjectsApi.ts` 在本轮快照中的哈希与复验轮（`EE2D53B0DEC3`）不同，其余六个前端文件逐一相同。该差异来自重开轮实施轮之后、本补证轮之前对独立审查 finding 的处置：删除了已无生产调用方的导出 `filterConfigurationRows` 及其专属用例。核对结论：该导出的判定体 `matchesRecognitionProjectFilter` 仍保留并被唯一筛选路径 `filterRecognitionProjectColumnView` 使用（`recognitionProjectsApi.ts:373` 定义、`:589` 调用），删除的只是一个不再被生产代码引用的转发出口（`filterConfigurationRows` 现于生产与测试代码中零命中）。因此该差异**不改变任何页面行为**，也不改变本轮 C26/C28 的判定路径；`gitnexus detect-changes` 登记的 30 个文件与本轮工作区文件集合一致。

### 门禁（本轮重跑，与上表同一工作区状态）

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` **0 错误 / 5 警告** |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **811 通过 / 0 失败 / 0 跳过** |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition test --run` **21 文件通过 / 415 通过 / 2 跳过（417）** |
| 前端 lint | `eslint .` **0 error / 0 warning** |
| 前端构建 | `pnpm -F dy-medical-recognition build` exit 0 |
| 客户端包 | `typecheck` exit 0、`build` exit 0（ESM `288.84 KB`） |
| 工作区 | `git diff --check` exit 0（仅 LF/CRLF 提示） |

数值与重开轮实施轮的"门禁终值"一致，无回归。

### 进程与身份

| 对象 | 归属 | 命令与工作目录 | 监听 | 日志 |
|---|---|---|---|---|
| 后端 | **本轮启动**（本轮结束时已关闭） | `dotnet run --no-build --project Dy.MedicalRecognition`，工作目录 `server/` | `127.0.0.1:15014` 与 `[::1]:15014`，进程号 6824 | `var/dev-logs/backend.out.log`、`var/dev-logs/backend.err.log` |
| 前端 dev server | **本轮启动**（本轮结束时已关闭） | `pnpm -F dy-medical-recognition dev`，工作目录 `client/` | `[::1]:3008`，进程号 39408 | `var/dev-logs/frontend.out.log`、`var/dev-logs/frontend.err.log` |

- 子应用按 `frontend-api-client` 与 Testing 第 1、2 节经宿主登录页与菜单进入：账号 `yangkj`（显示名 杨康健），组织 `01`；浏览器 Google Chrome 153.0.0.0，控制工具 Chrome DevTools MCP；宿主开发设置 Session `session-e7d799d0-f657-4b8c-8886-303881a31aba`。
- 拦截生效双重核实：宿主 `/dev-settings` 实测「开发地址拦截」全局开关为启用、映射 `medical-recognition → http://localhost:3008` 为启用；Network 实测子应用资源来自本地端口（`GET http://localhost:3008/subApps/medical-recognition/standard-catalog [304]`、`GET .../recognition-projects [304]`）。
- 菜单链路：父项「检验检查结果互认」→ 子项「互认项目」；进入前先关闭既有标签，确保从菜单重新进入。

### 重取证据一：C26 列视图全平铺

- **四栏恒定全显**：表头实测四个元素文本依次为 `类型`、`分类`、`分组`、`项目`，行内 `gridColumn` 依次为 `1`、`2`、`3`、`4`。
- **无折叠或展开控件**：页面内全部 **533 个按钮**中，按可访问名称与文本匹配 `展开|折叠|收起|collapse|expand` 的结果为**空数组**；全页 `[aria-expanded]` 元素数量为 **0**。
- **纵向跨行**：类型格实测 `grid-area: 2 / 1 / span 4;`、渲染高 **21953px**；四个分类格 `grid-area: 2 / 2 / span 1;`、`3 / 2 / span 1;`、`4 / 2 / span 1;`、`5 / 2 / span 1;`；四个分组格 `grid-area: 2 / 3;` 至 `5 / 3;`（每行一格，无跨度）；四个项目格 `grid-area: 2 / 4;` 至 `5 / 4;`。分组格 4 个、项目格 4 个、分类格 4 个、类型格 1 个，与"一个分组占一行"的结构一致；类型格高度等于其四个分组行之和。
- **窄视口横向滚动、列宽不压缩**（两个视口实测）：

| 视口宽 | 容器 `overflow-x` | `clientWidth` | `scrollWidth` | 最大可滚动 | `gridTemplateColumns` | 文档是否横向滚动 | 滚到最右后各栏横坐标 | 滚后各栏宽 |
|---|---|---|---|---|---|---|---|---|
| 900 | `auto` | 620 | 1060 | 440 | `88px 186px 208px 578px` | 否（`scrollWidth 900 = clientWidth 900`） | `-195, -107, 79, 287` | `88, 186, 208, 578` |
| 620 | `auto` | 340 | 1060 | 720 | `88px 186px 208px 578px` | 否（`scrollWidth 620 = clientWidth 620`） | `-475, -387, -201, 7` | `88, 186, 208, 578` |

  两个视口下 `scrollLeft` 均达到最大可滚动量（440 / 720），全显时四栏横坐标为 `245, 333, 519, 727`；滚到最右后前三栏横坐标为负（滚出可视区）而"项目"栏回到 `7`，即四栏靠滚动容纳、**列宽在滚动前后完全不变**。文档根元素不产生横向滚动，横向滚动由表格区容器单独承载。

- 结论：C26 声明的 Component 与 Host 两个层级在当前构建的真实宿主上全部成立。

### 重取证据二：C28 状态与可互认时间行内展示

- **四种状态徽标同页同轮计数**：`启用` 259 行、`停用` 1 行、`未配置` 170 行、`目录已停用` 1 行；项目行总数 431，页面汇总 `本组织共 261 项配置`。
- **目录停用行与原因**：为构造该行，经真实业务接口新建标准项目 `SP28-ITEM-DIRDISABLED-0930112217`、为它建立互认配置（19 天，启用），再经「标准项目目录维护」的真实接口在目录侧停用该项目。页面实测该行：徽标文本 `目录已停用`、类名含 `ant-tag-error`；原因元素文本 `标准项目已停用`；可互认时间仍显示 `19天`；`修改可互认时间` 入口与启停入口均保留。
- **点击时间进入行内编辑**：点击 `aria-label="修改可互认时间：1111111111111111111111111111111111111111111111111"` 后行内出现 `span.recognition-projects-days-editor`，含 `<input aria-label="可互认时间（天）" inputmode="numeric">`，初值 `70`（与行上当前值一致）且**进入即获得焦点**（`document.activeElement === input` 为真）；同时出现 `保存可互认时间：…` 与 `取消修改可互认时间：…` 两个按钮。
- **非法输入零写并保留输入与提示**：输入 `0` 后点保存，页面安装的写请求观测数组为空；编辑器与输入框保留（输入值仍为 `0`），行内出现提示，文本实测 `可互认时间输入不合法` 与 `可互认时间为 1 到 2147483647 之间的整数天数`。再输入 `-5` 后点保存同样零写、输入保留为 `-5`、编辑器保留。
- **合法保存**：输入 `45` 后点保存，Network 实测 `reqid=1071 POST /Api/MedicalRecognitionReport/UpdateMutualRecognitionItemConfiguration [200]`，请求体 `{"id":"3a23fd2a-e3f2-8723-7375-70cc5897209c","recognitionDurationDays":45}`，响应体 `true`；保存后编辑器收起、行上显示 `45天`、徽标仍为 `启用`，随后触发列表与目录两次读取（`reqid=1073`、`reqid=1074`）。
- **同值保存**：重新进入编辑，输入框回填当前值 `45`；直接点保存仍发出请求，`reqid=1076 POST ... [200]`，请求体 `{"id":"3a23fd2a-e3f2-8723-7375-70cc5897209c","recognitionDurationDays":45}`、响应 `true`，随后 `reqid=1077`、`reqid=1078` 重载列表与目录。即同值不短路，刷新范围是整表来源全量重载。
- **取证后恢复**：为不改变既有业务数据，取证结束时把该配置的可互认时间由 `45` 经同一真实接口恢复为**取证前的 `70`**，并把 `SP28-ITEM-DIRDISABLED-0930112217` 在目录侧恢复启用。页面回读确认：该行 `70天`、徽标回到 `启用`、原因消失；四徽标计数回到 `启用 260 / 停用 1 / 未配置 170`。
- 结论：C28 声明的 Component 与 Host 两个层级在当前构建的真实宿主上全部成立。

### 重取证据三：V30 批量目录有效性逐码校验

为覆盖四类违规原因，先经**真实业务接口**构造互相独立的前置（每一步都回读确认），再提交一个混合批次：

| 编码 | 各层状态 | 期望的违规原因 |
|---|---|---|
| `SP30-ITEM-VALID-0930111420` | 分类启用、分组启用、项目启用 | 无（批次内唯一合法项） |
| `SP30-ITEM-NOT-EXIST-0930111420` | 未创建 | 编码不存在 |
| `SP30-ITEM-CATDISABLED-0930111420` | 分类**停用**、分组启用、项目启用 | 所属分类已停用 |
| `SP30-ITEM-GRPDISABLED-0930111420` | 分类启用、分组**停用**、项目启用 | 所属分组已停用 |
| `SP30-ITEM-SELFDISABLED-0930111420` | 分类启用、分组启用、项目**停用** | 标准项目已停用 |

前置回读实测（各层状态与上表逐条一致，且五条编码在组织 `01` 下**均未配置**互认，确保不会被"查重"先拦而掩盖目录校验面）。

提交混合批次（`recognitionDurationDays = 17`，五条编码一次提交）实测：

- HTTP 状态 **500**，响应体首行原文：

```
System.InvalidOperationException: 业务拒绝：标准项目目录不可用于新建互认项目配置：编码不存在：SP30-ITEM-NOT-EXIST-0930111420；所属分类已停用：SP30-ITEM-CATDISABLED-0930111420；所属分组已停用：SP30-ITEM-GRPDISABLED-0930111420；标准项目已停用：SP30-ITEM-SELFDISABLED-0930111420。
```

  四类违规原因**按原因分组、各带自己的违规编码**，一次报告齐全；批次内的合法项未出现在错误里。
- **零行写入**：提交前后回读本组织配置集合，计数均为 `260`，编码集合逐项比对**完全一致**；五条待提交编码落库数为 **0**（含批次内的合法项 `SP30-ITEM-VALID-0930111420`）。
- **零事件**：该路径在预校验命中时直接抛出，不进入写入、不登记事件。真实运行面本环境无事件观察点（沿用 V24 的既有 `Blocked` 定级，本轮不重复裁定）；"`AddEvent` 一次都不调用"由离线用例 `Batch_create_collects_every_catalog_violation_grouped_by_reason` 以 `Assert.Empty(events.Events)` 与 `Assert.Equal(0, repository.CreateCalls)` 覆盖。
- **事务面**：服务端日志实测该请求 `数据库事务[Default] <id> 已开启` → `数据库事务 <id> 已回滚` → `事务 <id> 已全部回滚`，与"整批拒绝不部分写入"一致。
- 结论：V30 在真实后端与真实 PostgreSQL 上成立，且本轮**四类违规原因全部构造成功**（重开轮实施轮只构造出「标准项目已停用」一类）。

### 本轮矩阵结果（重开轮新增 11 条，证据来源逐条标注）

| 用例 | 验证面 | 状态 | 本轮证据 |
|---|---|---|---|
| C26 | 列视图全平铺 | Passed | 本轮重取，见"重取证据一" |
| C27 | 层级高亮 | Passed | 重开轮复验轮已重取（项目层高亮修正后）；本轮未重取，属同一工作区状态 |
| C28 | 状态与时间行内展示 | Passed | 本轮重取，见"重取证据二" |
| C29 | 弹窗多选新增 | Passed | 重开轮复验轮已重取（重建重启后的当前构建） |
| C30 | 批量新增失败口径 | Passed | 重开轮复验轮已重取（重建重启后的当前构建） |
| V28 | 批量新增成功 | Passed | 重开轮复验轮已重取；跨分类跨分组子面未覆盖 |
| V29 | 批量入参口径 | Passed | 离线面，本轮复跑 811 通过 |
| V30 | 批量目录有效性逐码校验 | Passed | **本轮重取，四类违规原因全部构造成功**，见"重取证据三" |
| V31 | 批量与库内配置查重 | Passed | 重开轮复验轮已重取 |
| V32 | 批量并发唯一冲突 | Passed | 复用重开轮实施阶段的真实库证据（整批回滚、无残留）；本轮未重取 |
| V33 | 批量读取无 N+1 | Passed | 离线面，本轮复跑 811 通过 |

统计：重开轮新增 **11 条全部 `Passed`**、`Failed` 0 条。

### 是否构成干净轮次的判断

- **本轮的三条重取证据构成一次连续干净的单一构建轮次**：C26、C28、V30 取自同一份代码快照（上表 SHA256）与同一对本轮启动的受管进程（后端 6824 / 前端 39408）；其中 C26 与 C28 经真实宿主同一次会话取得（宿主登录页与菜单进入、同一 Session、同一浏览器实例），V30 经该后端进程的真实 HTTP 接口取得。本轮门禁与三条重取在同一工作区状态下完成，期间工作区文件集合未变。
- **重开轮新增 11 条的证据与上述快照的差异只有一处，且不改变行为**：C27、C29、C30、V28、V31 由重开轮复验轮取得，其余前端文件与本轮逐一哈希相同，唯一差异是 `recognitionProjectsApi.ts`（复验轮 `EE2D53B0DEC3` → 本轮 `DD7C1C4FA168AD61`），差异内容为删除已无生产调用方的导出 `filterConfigurationRows`（判定体 `matchesRecognitionProjectFilter` 保留并被唯一筛选路径使用，见本节"与重开轮复验轮的快照差异"）。该删除不改变页面渲染、筛选结果与写入路径。
- **仍不构成"11 条全部在同一进程实例内取得"**：C27、C29、C30、V28、V31 取自重开轮复验轮（进程 5696/15600），C26、C28、V30 取自本轮（进程 6824/39408）。两轮的代码差异见上一条，但**不是同一个进程实例、不是同一次连续会话**。
- **离线面**：V29、V33 由本轮复跑的全量测试覆盖（811 通过 / 0 失败）。
- **真实库面**：V32 的证据来自重开轮实施阶段，其写入路径（`MedicalRecognitionReportManager`、`MutualRecognitionItem.xml`、`AppService` 的事务声明）在补证轮的快照中与当时一致，本轮未重取。
- 因此本轮的结论是：**重开轮新增矩阵的全部 11 条证据都对应同一份代码（差异仅为不改变行为的死代码删除），不存在"证据对应旧代码路径"的项**；若收口要求"同一进程实例内一次性取得全部 11 条"，则仍需再取一轮。是否满足收口条件由负责人裁定，本报告不代作结论。

### 未覆盖面（本轮）

| 事项 | 状态与原因 |
|---|---|
| C27、C29、C30、V28、V31 的进程内重取 | 未在本轮进程上重取；其证据来自重开轮复验轮，代码快照与本轮一致 |
| V32 并发唯一冲突 | 复用重开轮实施阶段的真实库证据，本轮未重取（该路径此后未改动） |
| V28 跨分类、跨分组编码批次 | 仍未覆盖；本轮批次为单条，重开轮复验轮的批次为同分类同分组 |
| V30 的"零事件"真实运行面 | 本环境无事件观察点，沿用 V24 的 `Blocked` 定级；由离线用例覆盖 |
| C17、V24、V26、V27 | 沿用既有 `Blocked` 定级，本轮不重复裁定、未改为通过 |
| `gitnexus detect-changes` | 本轮无代码 symbol 变化，不适用 |

### 本轮业务足迹（全部经真实业务接口或宿主页面，未直接写库、未执行 DDL）

- 新建目录数据：分类 `SP30-CAT-DISABLED-0930111420`（创建后停用）、`SP30-CAT-VALID-0930111420`；分组 `SP30-GRP-DISABLED-0930111420`（创建后停用）、`SP30-GRP-VALID-0930111420`；标准项目 5 个（`SP30-ITEM-CATDISABLED-…`、`SP30-ITEM-GRPDISABLED-…`、`SP30-ITEM-SELFDISABLED-…`（创建后停用）、`SP30-ITEM-VALID-…`、`SP28-ITEM-DIRDISABLED-…`）；`SP30-ITEM-NOT-EXIST-0930111420` 有意未创建。
- 新建配置：`SP28-ITEM-DIRDISABLED-0930111420`（19 天，启用），使本组织配置计数由 260 变为 261。
- **恢复操作**：`1111…` 配置的可互认时间由 `45` 恢复为取证前的 `70`；`SP28-ITEM-DIRDISABLED-0930111420` 由停用恢复为启用（该配置行本身保留）。
- 未恢复项：本轮新建的 `SP30-*` 分类、分组、标准项目与它们的停用状态，以及新增的一条配置行。这些是开发阶段的测试数据，按既有口径保留；停用的 `SP30-CAT-DISABLED-0930111420` 与 `SP30-GRP-DISABLED-0930111420` 只影响其自身下级的目录有效性，不影响既有业务数据。

### 残余风险（本轮）

- 为构造 C28 与 V30 的前置，本轮在目录侧停用了新建的标准项目与新建的分类、分组；这些目录项本身是本轮新建，未停用任何既有目录项。
- C28 的行内编辑取证改动了一条既有配置的可互认时间（`70 → 45`），已在取证结束时经同一真实接口恢复为 `70` 并回读确认；业务足迹仍如实登记。
- 取证结束时的进程收尾：本轮启动的后端（6824）与前端 dev server（39408）均已关闭，实测 15014 与 3008 无监听；未关闭任何其他进程。

## 筛选区交互整改轮（2026-10-02，批次22 / F19）

本节记录阶段根 S2-D21 的代码实施轮：「互认项目」页面筛选区删除重置控件，标准项目编码筛选改为经搜索按钮或输入框内回车显式提交，配置状态筛选保持切换即生效。设计依据为阶段根 [design.md](design.md) 的 S2-D21 与 [Client/design.md](Client/design.md) 的筛选区段；矩阵为 C31、C32。

### 轮次边界与基线

- 起始工作区：`git status --short --branch` 实测 `## main...origin/main [ahead 4]`；工作区内已有本轮的阶段2 设计文档改动（阶段根决策、客户端设计与页面文档、测试前矩阵、批次表与总体计划进度表），未回退或清理非本轮内容。本轮在共享工作区改代码与文档，不创建分支、不提交。
- 目标失败基线（RED）：改动前执行 `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionProjects`，**7 个用例失败 / 130 通过**。失败原因是筛选区没有搜索按钮、旧重置控件仍在（失败输出的 DOM 快照中可见 `aria-label="重置编码与状态筛选"`），属目标行为缺失，不是编译错误、夹具错误或环境不可用，符合 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.2。
- 受影响用例同步（按 Testing Baseline §2「需求确实变化时先更新设计和测试意图」）：`筛选后为空时区分空态文案…`、`C16：宿主组织变化后清空旧列表与弹窗、复位筛选…`、`C29` 批量新增、`筛选把编辑中的行藏起来时编辑态一并消失…`、`新增成功后的列表与选择数据刷新` 五处原先依赖「输入即筛选」的判定，改为经测试内 `applyCodeFilter` 辅助（输入原文后点搜索）布置生效筛选，断言语义不变；原「重置筛选的可访问名称与实际范围一致」用例删除，由 C31、C32 取代。

### 变更影响（闸门 2.3）

索引原为提交 `0a96f34`、检出提交 `4ee4d0a`，已按 [GitNexus Workflow](../../../.agents/instructions/gitnexus-workflow.md) §3 执行 `gitnexus analyze . --name Dy.MedicalRecognition --index-only` 重建（15,210 节点 / 31,381 边 / 348 流程）。`gitnexus impact RecognitionProjects --file client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` 返回 **风险 `LOW`**：直接调用方 1（`router/routes.tsx`）、受影响流程 0、受影响模块 0，`epistemic: exact`。影响面限于本页面的路由挂载点，未触及公共接口、OpenAPI、领域事件、数据库公共结构、权限或共享基础设施，未命中停止条件。

### 门禁（本轮）

| 门禁 | 结果 |
|---|---|
| 目标验证（受影响的两个测试文件） | `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionProjects` **2 文件通过 / 137 通过 / 0 失败** |
| 前端全量测试 | `cd client; pnpm -F dy-medical-recognition exec vitest run` **21 文件通过 / 416 passed / 2 skipped（418）**（上一轮终值 415 passed / 2 skipped；本轮删除 1 条、新增 2 条） |
| 前端 lint | `pnpm -F dy-medical-recognition lint` exit 0，0 problems |
| 前端构建 | `pnpm -F dy-medical-recognition build`（`tsc -b && vite build`）exit 0 |
| 工作区 | `git diff --check` exit 0（仅 LF/CRLF 提示） |

### 矩阵结果

| 用例 | 层级 | 状态 | 证据 |
|---|---|---|---|
| C31 编码筛选显式提交 | Component/Host | `Passed` | **Component**：用例「输入过程不改变列表，点击搜索或按回车后才按输入原文筛选」（`RecognitionProjects.test.tsx`），断言输入原文为草稿时列表与页脚计数均不变、点搜索与按回车后按原文筛选、查询仍只提交组织编码且只发起一次。**Host**：见下方宿主取证。 |
| C32 筛选区控件构成与清除动作 | Component | `Passed` | 用例「无重置控件；清除输入同时清掉输入原文与生效编码筛选；配置状态切换立即生效」（`RecognitionProjects.test.tsx`），断言筛选区内不存在名称含「重置」的按钮、清除入口清空后输入原文与生效编码筛选同时清空、配置状态切换即时改变列表、过程零写请求且查询只发起一次。 |

业务统计：本轮新增 2 条全部 `Passed`，无 `Blocked`、`N/A`、`Failed`、`NotRun`、`PendingRetest`。

### 宿主验收取证（2026-10-02）

在同一受管链路执行：宿主登录页 `http://183.224.180.166:35000/login` 以登录入口「检验检查结果互认平台」进入 → 宿主菜单「检验检查结果互认」→「互认项目」；后端 `http://localhost:15014`（Development），前端 dev server `http://localhost:3008`；子应用资源实测来自 `localhost:3008`（`@vite/client`、`src/main.tsx`、`src/pages/**/*.tsx` 均在 `localhost:3008/...` 下），开发地址拦截生效。控制工具为 `browser-debugging-and-control` 技能的受管 CLI：Chrome `154.0.8037.58`，独立 Profile `%LOCALAPPDATA%\codex-cdp\profile`，CDP 端点 `127.0.0.1:9222`，会话标识 `medsync-verify`。

| 观察项 | 实测结果 |
|---|---|
| 筛选区版面 | 筛选区 `display: flex`、单行（高 32px）；三件控件左对齐依次为编码输入框 `240px`（x=244）、搜索按钮 `82px`（x=492）、配置状态 Segmented `200px`（x=582）。搜索按钮与输入框同行（y 差 < 8px），无任何控件被拉伸占满行宽 |
| 重置控件 | 筛选区内名称含「重置」的按钮计数 **0**；页头「重新读取互认配置」仍为独立控件 |
| 输入原文不筛选 | 真实键入 `002504`（`Input.insertText`）后仍为 **325 行**，页脚「本组织共 325 项配置」，无筛选后计数 |
| 点搜索筛选 | 点搜索后 **325 → 46 行**，页脚「本组织共 325 项配置，筛选后 46 项」；46 行编码**全部**包含输入原文（不含原文的可见行数 0） |
| 回车筛选 | 草稿改为 `00210200001` 后按 `Enter` → **32 行**，同样全部包含原文，页脚「筛选后 32 项」 |
| 清除动作 | 点输入框自带清除入口后输入原文为 `""` 且 **325 行全部恢复**，页脚退回「本组织共 325 项配置」 |
| 状态筛选即时 | 配置状态切「停用」后立即 **0 行**、页脚「筛选后 0 项」（325 条配置均为启用）；切回「全部」后恢复 325 行 |
| 筛选的后端足迹 | 在互认项目页所属 frame 的 `performance` 时间线上比对整轮筛选前后的本地后端调用计数：**19 → 19**（`GetEnumMetadata` 1、`QueryRecognitionProjectConfigurationList` 9、`QueryEffectiveMedicalStandardCatalog` 9），筛选全程零新增读写 |
| 控制台 | 监听 4 秒，`events: []`，无 error 或 warn |

证据文件：`var/host-check-recognitionProjects-f19/` 下的探针脚本（`probe-filters.js`、`probe-state.js`、`probe-match.js`、`probe-backend-calls.js`、`restore-and-network.js`）与截图 `01-filter-area.png`（`var/` 由仓库根 `.gitignore` 第 410 行排除，不入版本控制）。版面与行为的判据取自 DOM 度量与可访问名称，截图只作留档、未参与判定。

### 残余与边界

- 真实输入由 CDP 的 `Input.insertText` 与 `Input.dispatchKeyEvent` 投递；检索语义（字面包含、区分大小写）与后端一致，本轮未新增该语义的宿主用例，沿用既有口径。
- 本轮只改前端页面、样式与其用例；后端、客户端包与数据库均未改动，故不重跑后端测试与客户端包门禁。
- 取证结束时保留的进程：后端（`localhost:15014`）与前端 dev server（`localhost:3008`）仍在本轮启动状态运行，受管 Chrome 会话 `medsync-verify` 保持打开；未关闭任何其他进程。

## 页头范围展示轮（2026-10-02，批次23 / B20 / F20）

本节记录阶段根 S2-D22 与 S2-D23 的跨端实施轮：新增只读范围查询 `QueryTrustedOrganizationScope`（不接收入参，组织取自可信上下文，名称经组织路径解析点的组织一层取得），页头组织改为「名称 + 编码、名称为主」，手动刷新按钮补悬停提示，主按钮命名为「新增互认项目」。设计依据见阶段根 [design.md](design.md) 的 S2-D22／S2-D23、[Server/design.md](Server/design.md) 的公共契约段与 [Client/design.md](Client/design.md) 的页头段；矩阵为 V34、C33。

### 轮次边界与基线

- 起始工作区：`## main...origin/main [ahead 4]`，工作区内已含前一轮（筛选区交互整改）的改动；本轮在共享工作区改代码与文档，不创建分支、不提交。
- **冻结清单的失败基线（RED）**：新增端点后、同步冻结清单前执行后端全量测试，`Stage4EndpointTests.Query_contract_method_set_matches_the_published_endpoint_inventory` 与 `Stage1ArchitectureTests.Query_contract_is_separated_from_write_service_and_repository_implementation` 各以「集合多出 `QueryTrustedOrganizationScopeAsync`」准确失败；`Stage7ExclusionGuardTests.V1_frozen_openapi_paths_declare_no_excluded_operation` 的路径数 55 与本轮 56 冲突。同步三处冻结清单与该用例后转 GREEN。这是本轮契约面的行为 RED。

### 变更面

| 端 | 改动 |
|---|---|
| 后端 | 新增 `Application.Contracts/Queries/TrustedScope/TrustedOrganizationScopeReadModel.cs`；`IMedicalRecognitionReportQueryAppService` 增加不接收入参的 `QueryTrustedOrganizationScopeAsync()`；`MedicalRecognitionReportQueryAppService` 实现：`TrustedOrganizationResolver` 取可信组织 → `OrganizationPathResolver.ResolveOrganizationOrThrow` 解析组织一层 → 映射。该链不进入 Repository，不新增 SQL 与注册键 |
| 后端测试 | `Stage2QueryTests` 新增 5 条（命中、可信组织缺失、组织不存在／已停用、入口无入参且无工作单元）；`Stage1ArchitectureTests` 的契约方法集与签名表、`Stage4EndpointTests` 的既有查询方法集、`Stage2EnumContractTests` 与 `Stage7ExclusionGuardTests` 的冻结路径清单同步 |
| 客户端包 | `prepare-openapi` 重新冻结（56 path，修正整数联合 21 处、可空引用 26 处）；`generate` 重生成 Kiota 客户端，新增 `queryTrustedOrganizationScope`；`src/index.ts` 未被覆盖；`typecheck`/`build` exit 0 |
| 前端 | 适配层新增 `TrustedOrganizationScope` 视图模型与 `queryTrustedOrganizationScope` 出口；页面把范围查询并入既有读取集合（并行发起、失败按同一口径进入阻断态），并用「范围查询回读的组织必须等于本次读取的目标组织」守卫名称；页头改为「组织 名称（编码）」三级文本，刷新按钮套 `Tooltip`，主按钮改名 |

### 门禁

| 门禁 | 结果 |
|---|---|
| 后端构建 | `dotnet build server/Dy.MedicalRecognition.slnx` **0 错误**；增量构建 5 警告、`--no-incremental` 全量重建 39 警告，均为既有项，本轮新增文件未产生警告 |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **810 通过 / 6 失败 / 0 跳过（816）**；6 条失败全部为既有时间硬编码缺陷，见"残余与边界" |
| 客户端包 | `prepare-openapi` 56 path、`generate` 成功、`typecheck` exit 0、`build` exit 0 |
| 前端测试 | `cd client; pnpm -F dy-medical-recognition exec vitest run` **21 文件通过 / 420 passed / 2 skipped（422）**（上一轮终值 416／2，本轮新增 4 条 C33 用例） |
| 前端 lint / 构建 | `eslint .` 0 problems；`pnpm -F dy-medical-recognition build` exit 0 |
| 工作区 | `git diff --check` exit 0（仅 LF/CRLF 提示） |

### 矩阵结果

| 用例 | 层级 | 状态 | 证据 |
|---|---|---|---|
| V34 可信组织范围查询 | Application/Contract | `Passed` | `Stage2QueryTests` 五条：返回可信组织的编码与名称且组织全量只读一次；可信组织缺失或纯空白时拒绝且不读取外部组织服务；组织不存在或已停用时拒绝并给出「业务拒绝：组织不存在或已停用。」；入口不接收入参且不声明工作单元。`Stage1ArchitectureTests` 的签名表另守卫该方法只以接口声明出现、无方法体、无 `public` 修饰符。运行面由宿主验收的 1 次 `QueryTrustedOrganizationScope` 调用覆盖 |
| C33 页头范围展示与动作命名 | Component/Host | `Passed` | **Component**（`RecognitionProjects.test.tsx` 四条）：页头按「组织 名称（编码）」展示且范围查询不提交入参；名称缺失时退回「组织 编码」、不渲染空括号；刷新按钮悬停渲染作用说明且保留可访问名称、主按钮命名为「新增互认项目」并打开既有新增弹窗；范围查询失败并入读取失败阻断态、重试成功后恢复名称；范围查询回读的组织与目标组织不一致时不接受该名称。**Host**：见下方宿主取证 |

业务统计：本轮新增 2 条（V34、C33）全部 `Passed`，无 `Blocked`、`N/A`、`Failed`、`NotRun`、`PendingRetest`。

### 宿主验收取证（2026-10-02）

链路与上一轮相同（宿主登录页选入口「检验检查结果互认平台」→ 菜单「检验检查结果互认」→「互认项目」；后端 `http://localhost:15014` Development，前端 dev server `http://localhost:3008`）。**本轮先对宿主页面执行整页重载再取证**，使证据来自重载后的当前构建，而非增量热更新的结果。控制工具同为受管 CLI（Chrome `154.0.8037.58`，独立 Profile，CDP `127.0.0.1:9222`，会话 `medsync-verify`）。

| 观察项 | 实测结果 |
|---|---|
| 页头组织范围 | 完整文本 `组织 县医共体（01）`；标签「组织 」`rgb(107, 114, 128)`／`12px`，名称「县医共体」`rgb(31, 41, 55)`／`14px`，编码「（01）」`rgb(107, 114, 128)`／`12px`——名称权重高于编码，编码降为次要小号 |
| 刷新按钮 | 存在，可访问名称「重新读取互认配置」，图标按钮宽 32px；真实鼠标移入后页面渲染 `.ant-tooltip`，文本为「重新读取互认配置」 |
| 主按钮 | 文本「新增互认项目」（宽 138px）；点击后打开弹窗，标题「新增互认项目配置」，含「可互认时间（天）」输入与「取 消」／「确定新增」；点「取 消」后弹窗关闭。可选集合为空，与本组织 325 个标准项目均已配置一致 |
| 列表 | 325 行，页脚「本组织共 325 项配置」 |
| 后端调用 | 互认项目页 frame 在重载后共 4 次本地后端调用：`GetEnumMetadata` 1、`QueryRecognitionProjectConfigurationList` 1、`QueryEffectiveMedicalStandardCatalog` 1、**`QueryTrustedOrganizationScope` 1**；零写入 |
| 控制台 | 监听 4 秒 `events: []` |

证据文件：`var/host-check-recognitionProjects-f19/` 的探针脚本与截图 `02-header-scope.png`。

### 残余与边界

- **后端全量测试曾有 6 条与本轮无关的既有失败**：`Stage4WritePathTests` 的作废时间相关用例（`First_void_takes_effect_without_content_version`、`Voided_report_cannot_be_submitted_again`、`Repeated_void_is_idempotent_only_for_identical_void_facts`、`Voided_laboratory_report_stops_appending_versions`、`Voided_examination_report_stops_appending_versions`、`Void_keeps_versions_and_details_readable`）。根因是该文件第 32 行的夹具时间 `ReceivedTime = new(2026, 10, 1, 11, 30, 0)` 是固定值，被同时用作作废时间与作废请求的接收时间，而同一文件第 100 行断言版本的平台接收时间由服务端取当前时点：真实时钟走过 2026-10-01 11:30 之后，作废时间必然早于版本接收时间，管理器在 `MedicalRecognitionReportManager.Submission.cs:169-170` 的两端边界校验处拒绝。该文件与相应管理器本轮均未改动（`git status` 无记录）。本报告当时不修改该夹具、也不把这 6 条计入本阶段矩阵，处置留给负责人。**该夹具缺陷已于 2026-10-02 的修订轮实施中一并修复**（只改测试夹具，生产代码不动），修复后后端全量为 **816 通过 / 0 失败 / 0 跳过**，见"阶段 4 测试夹具时间缺陷修复（2026-10-02，同轮附带）"。
- **本轮的行为用例未取得改动前 RED**：V34 与 C33 的用例在实现之后编写，RED 只覆盖了冻结清单与契约方法集（见"轮次边界与基线"）。用例的判别力来自对新增行为的直接断言（新的页头文本、缺失名称时的回退形态、范围查询不提交入参、组织不一致时不接受名称）与既有回归集，但该轮的 RED-GREEN 顺序与 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.1 的要求存在偏差，如实登记。
- 名称与组织编码同源的断言只覆盖离线用例与宿主的一次调用计数；组织名称变更是外部组织服务侧的数据变化，本平台不做缓存，页面每次读取都会重新取得。

## 设计与交互修订轮（2026-10-02，批次24 / F21，文档与设计）

本节记录对前两轮已交付交互的修订。**本轮只改设计与文档，不改代码**，因此不产生新的运行证据；其目的是把设计定稿，使随后的实施轮有确定依据。

### 修订内容

| 决策 | 修订 |
|---|---|
| S2-D21 | 筛选区仍为「编码输入框 + 搜索按钮 + 配置状态 Segmented」，明确**不设独立刷新控件**；编码经搜索提交、配置状态切换即时生效且不触发读取 |
| S2-D22 | 页头组织由「名称 + 编码、名称为主」的单行带标签形态，改为**上下两行**（名称在上、编码在下）并**去掉「组织」文字标签** |
| S2-D23 | 页头动作区**只保留「新增互认项目」**，移除纯图标刷新按钮；手动刷新能力由「条件为空时提交搜索」与「新增弹窗内的重新读取」两条路径承担 |
| S2-D24（新增） | 搜索动作的语义：提交时**重新读取**配置列表、标准目录与组织范围，再在结果上本地裁剪；条件为空时提交等价于刷新；读取在途时搜索按钮进入 `loading` 形态、列表内行内操作按既有门控禁用；读取失败按既有口径处理，不降级为只筛旧数据 |
| S2-D25（新增） | 新增弹窗**打开即重新读取**，可选集合按本次读取结果计算；弹窗内提供文字「重新读取」；重读后已不在可选集合中的勾选被剔除并给出条数说明；「重新读取」常驻弹窗底部，整批创建被服务端拒绝时就地即可重读（写失败文案由宿主统一展示，页面不自行捕获写失败，因此入口不依赖页面知道失败原因） |

### 对既有证据的影响

修订改变了三条已记录用例的判定内容，按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §3 与 §5 处置如下：

| 用例 | 原记录 | 处置 |
|---|---|---|
| C31 | `Passed`（筛选区交互整改轮） | 定义已改写（原判定要求「查询请求仍只提交组织编码」且输入过程与提交均不触发读取；现判定要求提交时重新读取）。原证据对应修订前的形态，**转 `PendingRetest`**，复测随 F21 实施轮进行 |
| C32 | `Passed`（筛选区交互整改轮） | 定义已改写（新增「不设独立刷新控件」与「读取在途时行内操作禁用」的判定）。**转 `PendingRetest`**，复测随 F21 实施轮进行 |
| C33 | `Passed`（页头范围展示轮） | 定义已改写（原判定为带「组织」标签的单行形态与刷新按钮悬停提示；现判定为上下两行无标签形态与「页头只有主按钮」）。**转 `PendingRetest`**，复测随 F21 实施轮进行 |
| V34 | `Passed`（页头范围展示轮） | 不受影响：后端范围查询、查询接口与 SQL 均未改动，证据继续有效 |
| C34 | 本轮新增 | 尚无证据，随 F21 实施轮取得 |

本轮无后端改动，因此不重跑后端门禁、也不改变后端矩阵状态。**修订前的实现仍在工作区内**（未提交），其对应的旧证据在 F21 实施并复测之前不代表当前设计；该实施与复测已于同日完成，见"修订轮实施（2026-10-02，批次24 / F21）"。

## 修订轮实施（2026-10-02，批次24 / F21）

本节记录阶段根 S2-D21～S2-D25 修订的代码实施与取证，实施批次为 F21（见 [Client/impl.md](Client/impl.md)）。设计依据为阶段根 [design.md](design.md) 的 S2-D22～S2-D25、[Client/design.md](Client/design.md) 的页头段／筛选区段／新增弹窗段与 [Client/Pages/RecognitionProjects/RecognitionProjects.md](Client/Pages/RecognitionProjects/RecognitionProjects.md) 的页面结构表；矩阵为 C31、C32、C33、C34。**本轮无后端改动**：范围查询与配置查询接口未变，V34 沿用上一轮证据。

### 变更面

| 端 | 改动 |
|---|---|
| 前端页面 | 页头组织改为上下两行（`.recognition-projects-scope` 列向排布，名称在上、编码在下）并移除标签元素；页头动作区只保留主按钮，删除纯图标刷新控件。`load` 增加 `pendingCode` 入参：配置读取成功后才把输入原文写为生效筛选，并按与 `changeFilter` 同一理由清空行内编辑态；`submitCodeFilter` 以输入原文作为 `pendingCode` 提交；搜索按钮增加 `loading` 形态 |
| 前端弹窗 | 新增弹窗打开时立即重读（与弹窗内「重新读取」共用同一处读取开关），弹窗底部常驻文字「重新读取」；`pruneBatchSelection` 在重读成功后收敛选择，被剔除条数由页面交给弹窗显示说明 |
| 前端用例 | C31/C32/C33 重写、C34 新增；两处「编辑态随当前行集重取／判定」的用例改由另一行启停写入触发的后台重载驱动（搜索提交按设计会清掉编辑态，无法再用它观察该性质）；多处读取计数按「打开弹窗即重读」递增 |
| 后端测试夹具 | `Stage4WritePathTests.cs`：作废命令辅助的缺省时间由固定常量改为同一次「发起时的当前时点」，三处显式传固定常量的调用改用缺省口径，一处期望值改读命令上实际传入的作废时间；固定常量 `ReceivedTime` 保留其在提交路径上的原作用并补注不得用作作废时间（生产代码未改动） |
| 文档 | 阶段根 S2-D25 的口径按实施事实收窄：「重新读取」常驻弹窗底部，写失败文案由宿主统一展示、页面不自行捕获写失败，因此入口不依赖页面知道失败原因；同步 `Client/design.md`、页面文档与 `Client/testPlan.md` 的 C34 行。`Client/design.md` 另把搜索按钮在途形态的表述按实现改为「图标换成旋转指示，读取在途期间的点击不再发起新的读取」，并补记提交应用条件后清空行内编辑态。`Client/Pages/RecognitionProjects/RecognitionProjects.md` 按四栏列视图与行内编辑订正页面结构中陈旧的「表格／表格行操作／修改弹窗」三行及相关表述 |

### 门禁

| 门禁 | 结果 |
|---|---|
| 目标两文件 | `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionProjects` **2 文件 / 144 通过** |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **816 通过 / 0 失败 / 0 跳过（816）**（同轮附带修复 `Stage4WritePathTests` 的测试夹具时间缺陷，生产代码未改动，见下文小节） |
| 前端测试 | 全量 `vitest run` **21 文件通过 / 423 passed / 2 skipped（425）**（上一轮终值 420／2） |
| 前端 lint / 构建 | `eslint .` 0 problems；`pnpm -F dy-medical-recognition build` exit 0 |
| 工作区 | `git diff --check` exit 0 |

### 矩阵结果

| 用例 | 层级 | 状态 | 证据 |
|---|---|---|---|
| C31 搜索动作重新读取并在新结果上裁剪 | Component/Host | `Passed` | **Component**：用例「输入过程不改变列表；提交时重新读取，读取完成后才按输入原文裁剪；条件为空时提交等价于刷新」——输入原文只作草稿（列表与页脚均不变、零读取），提交时重新读取且读取在途时搜索按钮为 `ant-btn-loading`、旧行集不被提前裁掉，读取返回后才按输入原文裁剪并给出筛选后条数，回车与点击同一条提交路径，换代数据后列表随之更换，空条件提交等价于刷新，且全部查询请求只提交组织编码。**Host**：见下方宿主取证 |
| C32 筛选区控件构成、清除动作与读写在途门控 | Component/Host | `Passed` | **Component**：两条用例——筛选区无「重置」与独立刷新控件、页头只有主按钮、清除入口只清编码、配置状态切换不触发读取；读取在途期间列表内的行内操作与新增入口一并禁用、旧数据只能查看。**Host**：见下方宿主取证 |
| C33 页头组织展示与动作区 | Component/Host | `Passed` | **Component**：四条用例——组织按上下两行展示且无文字标签、名称与编码同一来源、名称缺失时只展示编码一行；页头只有「新增互认项目」一个动作且点击打开新增弹窗；范围查询失败并入读取失败阻断态、重试成功后恢复名称；范围查询回读的组织与目标组织不一致时不接受该名称。**Host**：见下方宿主取证 |
| C34 新增弹窗就地重读与失效勾选剔除 | Component/Host | `Passed` | **Component**：两条用例——打开弹窗即重新读取、可选集合按本次读取计算、读取在途时确定按钮冻结且弹窗内存在「重新读取」；重读后已不在可选集合中的勾选被剔除、给出剔除条数说明（不静默）、天数与弹窗保留。**Host**：打开即重读与弹窗内手动重读见下方宿主取证；失效勾选剔除见「宿主层补证：失效勾选剔除」 |

业务统计：本轮 4 条（C31、C32、C33 由 `PendingRetest`、C34 由 `NotRun` 转 `Passed`）全部 `Passed`，无 `Blocked`、`N/A`、`Failed`、`NotRun`、`PendingRetest`。

### 宿主验收取证（2026-10-02）

链路与上一轮相同（宿主登录页选入口「检验检查结果互认平台」→ 菜单「检验检查结果互认」→「互认项目」；后端 `http://localhost:15014` Development，前端 dev server `http://localhost:3008`）。**取证前对宿主页面执行整页重载**，使证据来自重载后的当前构建。控制工具同为受管 CLI（Chrome `154.0.8037.58`，独立 Profile，CDP `127.0.0.1:9222`，会话 `medsync-verify`）。请求观测为在页面各 realm 包裹 `fetch` 记录路径与请求体；「读取在途」通过仅浏览器侧延迟响应构造（不改数据、不改后端）。

| 观察项 | 实测结果 |
|---|---|
| 页头组织形态 | `.recognition-projects-scope` 为 `inline-flex` + `flex-direction: column`；名称「县医共体」占 `y 86–104`（`rgb(31, 41, 55)`／`14px`），编码「01」占 `y 104–119`（`rgb(107, 114, 128)`／`12px`）——名称在上、编码在下；标签元素不存在 |
| 页头动作区 | 页头内按钮计数 **1**，文本「新增互认项目」且为主按钮；纯图标刷新控件不存在（`refreshPresent: false`） |
| 筛选区构成 | 筛选区内按钮：输入框的清除入口（`.ant-input-clear-icon`，隐藏态）与「搜索」（主按钮、`aria-label="搜索"`、`SearchOutlined`）；名称或可访问名称含「重置」「刷新」的控件计数 **0**；输入框（`x 244`）、搜索按钮（`x 492`）与配置状态 Segmented（`x 582`）同处一行（三者的 `y` 同为 `152`） |
| 输入不生效 | 输入完整编码后：列表仍 325 行、页脚仍「本组织共 325 项配置」、新增读取 **0** 次 |
| 提交即重读并裁剪 | 点击搜索后新增 3 次读取：`QueryRecognitionProjectConfigurationList`（请求体 `{"organizationCode":"01"}`）、`QueryEffectiveMedicalStandardCatalog`（`{}`）、`QueryTrustedOrganizationScope`（无请求体）；列表裁剪为 1 行（即该编码），页脚「本组织共 325 项配置，筛选后 1 项」——总数仍来自本次读取的全量结果 |
| 读取在途形态与重复提交 | 在途时搜索按钮带 `ant-btn-loading`、图标为 `anticon-loading anticon-spin`，列表保持 325 行旧数据；在途期间追加点击 3 次后读取次数仍为 **3**，未产生额外请求 |
| 空条件提交等价刷新 | 用输入框自带清除入口清空后，列表立即恢复 325 行（不读取）；再次提交空条件新增 3 次读取，结果为 325 行、页脚「本组织共 325 项配置」 |
| 弹窗打开即重读 | 点击「新增互认项目」后新增 3 次读取（与页面进入同一条读取路径）；弹窗底部存在文字「重新读取」（图标 `anticon-reload`） |
| 弹窗内手动重读 | 点击「重新读取」再次产生 3 次读取，弹窗保持打开；弹窗正文为「没有可新增的标准项目」，与组织 `01` 的 325 个目录项均已配置一致；点「取 消」后弹窗关闭 |
| 控制台 | 整页重载后监听 6 秒，`error`/`warning`/`assert` 事件 `[]` |

证据文件：`var/host-check-recognitionProjects-f21/` 的探针脚本与截图 `01-page-search-and-header.png`、`02-modal-reload.png`。

### 宿主层补证：失效勾选剔除（2026-10-02，同一轮）

实施轮取证时组织 `01` 的有效目录项全部已配置，弹窗可新增集合为空，「剔除失效勾选」在宿主层不可构造。数据调整后（组织 `01` 的互认配置由 325 条调减为 300 条，有效标准项目仍为 325 条）该场景可构造，遂在同一验收链路上补取证据。构造用两个宿主标签页：A 标签打开新增弹窗并勾选两个可新增项目，B 标签经页面新增其中之一（一次真实页面写入），A 标签再点弹窗内「重新读取」。

| 观察项 | 实测结果 |
|---|---|
| 补证前数据 | 组织 `01` 互认配置 **300** 条（全部启用），有效标准项目 325 条；A 标签弹窗可新增集合 **25** 项 |
| A 标签勾选 | 勾选「乙型肝炎表面抗原（HBsAg）（002504030040000-01001）」与「乙型肝炎表面抗体（anti-HBs）（002504030050000-01001）」，已选 **2 项**，无收敛说明 |
| B 标签写入 | 在 B 标签的新增弹窗内勾选同一「乙型肝炎表面抗原」项、填 30 天并提交：弹窗按成功路径关闭，页脚由「本组织共 300 项配置」变为 **301**，该行显示「启用 / 30 天」（经页面写入，未直接写库） |
| A 标签重读 | 点「重新读取」后可新增集合由 25 项变为 **24** 项，已勾选的该项不再出现；已选由 2 项收敛为 **1 项**；弹窗内出现「选择已按最新数据收敛」说明，正文为「重新读取后，有 1 个已选项目已不在当前可新增集合中（已被配置或目录已停用），已从选择中移除。」——剔除不静默 |
| 剔除后仍可继续 | 「确定新增」保持可用（剩余的已勾选项仍在可选集合中）；点「取 消」关闭弹窗，本次未提交 |
| 数据终态 | 以只读方式核对：组织 `01` 互认配置 **301** 条（全部启用），其中 `002504030040000-01001` 为 30 天、启用、操作时间 `2026-10-02T02:24:08+08:00` |

本节的写入只新增上述 1 条配置，是新增互认项目弹窗的正常业务写入。上文实施轮宿主证据表内的「本组织共 325 项配置」等数值是数据调减前的观测值，本节数值为补证时的实际数据。

### 残余与边界

- **整批创建被服务端拒绝后的就地恢复**：设计口径已收窄为「『重新读取』常驻弹窗底部」，其存在性与可用性由 C34 的宿主证据（打开弹窗即存在、点击即再读）与组件层用例覆盖；整批被拒后的弹窗保留与选择保留另在重开轮的宿主验收中已取证，本轮不重复。
- **「读取在途」为浏览器侧延迟响应构造**：仅延迟本次响应返回时机，未改后端、未改数据；真实网络时延下的表现未另行取证。
- **搜索按钮在途形态不设 `disabled` 属性**：实测按钮带 `ant-btn-loading`、图标换成旋转指示，DOM `disabled` 仍为 `false`——重复提交由 antd 的 loading 守卫拦下（追加点击 3 次后请求数不变）。设计表述已按此事实写为「读取在途期间的点击不再发起新的读取」。
- **本轮两处既有用例的驱动方式变更**：「编辑期间后台刷新交付新值时，编辑器取当前行取值」与「后台刷新使行内编辑的目标行消失后，组织变化不再出现未保存内容确认」改由**另一行启停写入触发的后台重载**驱动。原因是搜索提交按 S2-D24 会应用条件并清空行内编辑态，原以「提交搜索」充当后台刷新的写法会先清掉被观察的编辑态，从而失去判别力；改后两处仍在读取交付新行集后断言编辑态的取值与「未保存内容」判定。
- 本轮未改动后端生产代码与数据库；对数据库的变更只有补证时经页面写入的 1 条配置。

### 阶段 4 测试夹具时间缺陷修复（2026-10-02，同轮附带）

页头范围展示轮的后端门禁记录中，6 条 `Stage4WritePathTests` 的作废用例失败被登记为「既有时间硬编码缺陷、处置待负责人裁定」。该缺陷在本轮一并修复，改动面只有测试夹具：

| 项 | 内容 |
|---|---|
| 根因 | 该文件的作废命令辅助把**作废时间**与**作废请求的接收时间**都默认成固定常量 `2026-10-01 11:30`，而当前版本的平台接收时间由服务端取当前时点（同文件对该时点另有「落在执行时点附近」的断言）。真实时钟走过该常量后，固定作废时间去撞「作废时间不得早于当前版本的平台接收时间」、固定请求接收时间去撞「作废时间不得晚于本次请求的接收时间」，两端校验必然拒绝 |
| 改动 | `VoidCommand` / `ExaminationVoidCommand` 的缺省时间改为同一次「发起时的当前时点」（作废时间与请求接收时间取同一值，两端相等即接受）；用例中显式传入固定常量的三处改为使用缺省口径；`First_void_takes_effect_without_content_version` 的期望值改为读命令上实际传入的作废时间。固定常量 `ReceivedTime` 保留其在提交路径上的原作用（证明服务端不采信请求声明的接收时间），并补注其不得用作作废时间 |
| 生产代码 | 未改动。管理器的两端边界校验（`MedicalRecognitionReportManager.Submission.cs`）与 `Void_time_boundaries_are_enforced` 用例原样保留，该用例继续守「早一秒拒绝、晚一秒拒绝、两端相等接受」 |
| 门禁 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **816 通过 / 0 失败 / 0 跳过（816）**（修复前 810 通过 / 6 失败） |

该缺陷与阶段2 的矩阵无关，修复后阶段2 后续各轮的后端门禁不再带已知失败项。阶段4 自身的矩阵状态不因本次测试夹具修复而改变，其报告侧登记由阶段4 处置。

## 行集收敛与弹窗发现能力轮（2026-10-02，批次25 / F22）

本节记录阶段根 S2-D16／S2-D17／S2-D24 的修订与 S2-D26～S2-D28 的代码实施。起因是页面与新增弹窗同时承担"看哪些项目还没配置"，而页面上这些行没有任何动作：用户看到缺口却不能在原地补上，同一份未配置项在两处出现使发现职责归属不明。修订把发现与选择整体收进弹窗，页面成为本组织已配置项的清单。设计依据为阶段根 [design.md](design.md) 的 S2-D16、S2-D17、S2-D24、S2-D26～S2-D28 与 [Client/design.md](Client/design.md) 的页面交互定稿；矩阵为 C26、C28（改写）、C35、C36（新增），回归面为 C1-C3、C6、C8、C10-C21、C23、C27、C29-C34。

### 轮次边界与基线

- 起始工作区：`## main...origin/main [ahead 4]`，工作区内含前三轮（重开交互整改、页头范围展示、设计与交互修订）的未提交改动；本轮在共享工作区改代码与文档，不创建分支、不提交。
- 数据基线：组织 `01` 的互认配置 **301** 条（全部启用）、有效标准项目 325 条，弹窗可新增集合 24 项。本轮**未产生任何写入**。
- 交付前的失败面：本轮的行为用例在实现之后编写，未取得"改动前失败"的独立 RED（见"残余与边界"）。

### 变更面

| 端 | 改动 |
|---|---|
| 前端合并口径 | `toRecognitionProjectColumnView` 的第一遍在目录项没有同编码配置行时**直接跳过**（既不产行也不建层级），页面行集因此只含本组织已配置项；`toSelectableColumnView` 走同一实现但保留未配置行，弹窗可选集合口径不变。新增 `BATCH_CREATE_LIMIT`（1000）与 `exceedsBatchCreateLimit` |
| 前端页面 | 空态改写为「本组织还没有互认项目配置」（行集与计数同源，"列表为空"只可能是零配置）；已生效筛选与本页路由地址的查询参数双向一致（进入按 `code`/`status` 初始化、条件变化后替换当前历史项回写、输入原文不写入），新增 `recognitionProjectsFilterUrl.ts` 承载解析与回写口径；页面级提示移入常驻的 `aria-live="polite"` 容器 |
| 前端弹窗 | 顶部新增「标准项目编码搜索」（在本次已读取的可选集合上本地裁剪、不发起读取，清空即恢复）、底部新增「全选当前结果」（补齐当前搜索结果，不清除已有选择）；选择条数超过单批上限时本地拦截并给出条数说明、不发请求；项目行整行成为勾选命中区（名称与编码放进复选框标签）；异步提示移入常驻 live region |
| 前端样式 | 层级格声明 `content-visibility: auto` 与固有尺寸估计（S2-D28）；弹窗内容区声明 `overscroll-behavior: contain`；可点元素声明 `touch-action: manipulation`；新增命中区与 live 容器样式 |
| 前端用例 | 适配层：行集与计数断言改为"未配置项不进视图"、新增弹窗可选集合口径与上限判定；页面：空态与计数、深链初始化、地址栏回写与参数口径；弹窗：C35 四条（搜索本地裁剪、全选、命中区、live region 与本地拦截）；层级用例改用"整份目录都已配置"的装配 |

### 门禁

| 门禁 | 结果 |
|---|---|
| 目标两文件 | `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionProjects` **2 文件 / 153 通过**（本轮 144 → 153） |
| 前端测试 | 全量 `vitest run` **21 文件通过 / 432 passed / 2 skipped（434）** |
| 前端 lint / 构建 | `eslint .` 0 problems；`pnpm -F dy-medical-recognition build` exit 0 |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **816 通过 / 0 失败 / 0 跳过（816）**（本轮未改后端） |
| 工作区 | `git diff --check` exit 0 |
| 变更影响 | `gitnexus impact RecognitionProjects --repo Dy.MedicalRecognition`：`impactedCount 3`、`risk LOW`、直接调用方 1（路由注册） |

### 矩阵结果

| 用例 | 层级 | 状态 | 证据 |
|---|---|---|---|
| C26 列视图全平铺与页面行集 | Component/Host | `Passed` | **Component**：四栏表头与无折叠控件、层级格单一且跨行声明正确（改用整份目录都已配置的装配）、窄视口滚动容器与最小宽度；新增「未配置的目录项目不进列视图：行集只含本组织已配置项，计数与配置行数一致」（适配层）与「目录非空但本组织没有配置时同样报本组织空态」（页面）。**Host**：见下方宿主取证 |
| C28 状态与时间行内展示 | Component/Host | `Passed` | **Component**：启用／停用／目录停用三种徽标与目录停用原因各自正确、页面不再出现「未配置」行；每一行都是已配置行、都有可互认时间入口；行内编辑的非法输入零写与合法保存既有用例保持通过。**Host**：见下方宿主取证 |
| C35 弹窗发现能力 | Component/Host | `Passed` | **Component**：四条用例——编码搜索在已读取集合上本地裁剪且读取次数不变、无命中时报「没有匹配的标准项目」、清空后恢复；全选当前结果补齐当前搜索结果且不清除已有选择、再点不取消；点行内名称与点复选框等价；常驻 live region 存在且本地拦截文案落在其中。**Host**：见下方宿主取证 |
| C36 已生效筛选与路由查询参数一致 | Component/Host | `Passed` | **Component**：带 `code`／`status` 进入页面时按参数初始化（输入框与列表同条件）、输入原文不写入、提交与状态切换后回写、清空后参数不残留；参数口径（缺失与不可识别取值按不生效、其他参数原样保留）由纯函数用例固定。**Host**：见下方宿主取证 |

业务统计：本轮 4 条（C26、C28 经改写复测，C35、C36 新增）全部 `Passed`；回归面 C1-C3、C6、C8、C10-C21、C23、C27、C29-C34 全部保持 `Passed`，无新增 `Blocked`／`N/A`。

### 宿主验收取证（2026-10-02）

链路同前（宿主登录页 → 菜单「检验检查结果互认」→「互认项目」；后端 `http://localhost:15014`，前端 dev server `http://localhost:3008`）。取证前对宿主页面执行整页重载。

| 观察项 | 实测结果 |
|---|---|
| 页面行集 | 列表 **301** 行且每行都是已配置项（含补证新增的 `002504030040000-01001`），页脚「本组织共 301 项配置」——行数与配置条数一致；未配置项目与只含未配置项目的分组不再出现 |
| 弹窗可选集合 | 打开新增弹窗即重读（3 次读取，与页面进入同一条路径），可新增集合 **24** 项（325 目录 − 301 配置） |
| 弹窗编码搜索 | 输入原文未提交时集合仍 24 项；提交 `002504030` 后集合 **24 → 7** 项且 7 项编码全部包含该片段，**读取增量 0** |
| 全选当前结果 | 点一次后「已选」由 0 变为 **7 项**（等于当前搜索结果的项目数） |
| 行内命中区 | 点项目行内的名称文本即可切换勾选（复选框 true → false）；全程写入请求 **0** 次 |
| 筛选与路由地址 | 输入原文不改变地址；提交后子应用路由地址变为 `…/recognition-projects?code=002504030`（列表 301 → 14、页脚「本组织共 301 项配置，筛选后 14 项」）；状态切「停用」后变为 `?code=002504030&status=2`（0 行）；清空编码后变成 `?status=2`，`code` 未残留 |
| 可访问性与样式 | 页面与弹窗各存在 1 个常驻 `aria-live="polite"` 容器；弹窗内容区 `overscroll-behavior: contain`；层级格 `content-visibility: auto` |
| 控制台 | 整页重载与上述交互后监听 5 秒，`error`/`warning`/`assert` 事件 `[]` |

证据文件：`var/host-check-recognitionProjects-f22/` 的探针脚本与截图 `01-page-configured-only.png`。

### 残余与边界

- **路由参数的适用边界（重要）**：页面嵌在宿主工作区里时浏览器地址栏属于宿主外壳，本轮实测参数落在**子应用路由地址**上（`http://183.224.180.166:35000/subApps/medical-recognition/recognition-projects?code=…`），浏览器标签地址仍是 `http://183.224.180.166:35000/`；且**宿主整页刷新会把工作区标签重新挂到不带参数的路由上**，参数不跨刷新保留（实测重载后列表恢复 301 行、条件清空）。因此该项的实际价值是"子应用路由携带条件 + 直达子应用地址时可深链"，不是"浏览器地址栏可见并可分享"。S2-D27、`Client/design.md`、页面文档与 `Client/testPlan.md` 的 C36 行已按此边界改写。
- **零配置空态与超单批上限未在宿主构造**：组织 `01` 现有 301 条配置（零配置空态不可构造），弹窗可新增集合 24 项（超 1000 条不可构造）。两个分支的证据为组件层用例（空态文案、`exceedsBatchCreateLimit` 与本地拦截文案），宿主层只覆盖到"空态容器与 live region 存在"。
- **`content-visibility: auto` 的滚动表现未在真实长列表上复现**：本轮在宿主上只核到声明生效（`auto`）。层级格跨若干行且行高随该分组下的项目数变化，滚动条长度估算与锚定表现需要在更长列表上观察；出现可复现跳动时按 S2-D28 回退为不声明并另行登记。
- **本轮行为用例未取得改动前 RED**：用例在实现之后编写，判别力来自对新增行为的直接断言（未配置项不进列表、空态文案、搜索不触发读取、全选结果、命中区切换、地址参数回写）与既有回归集，而非"改动前失败"的证据；与该轮之前各轮的 RED-GREEN 顺序存在偏差，如实登记。
- **两处既有文档与实现不一致在本轮一并对齐**（不改行为）：本地编码筛选实际是**不区分大小写**的包含匹配（`recognitionProjectsApi.ts` 的判定体与其单测），而 `Client/design.md`、页面文档与 `Client/testPlan.md` 的 C31/C35 行原先写"字面包含、区分大小写"，已改为与实现一致并写明"只服务本地展示、不改变读取范围"的理由。
- 本轮未改动后端生产代码、接口与数据库，未执行任何写入。

## 关键字下推与可新增项目查询轮（2026-10-02，批次25 / F23）

本节记录阶段根 S2-D21／S2-D24／S2-D26 的修订与 S2-D29 的实施：筛选关键字改为**服务端查询条件**并跨四个字段匹配，同时新建一条不分页的「可新增标准项目」查询，使页面与弹窗都不再持有全量数据。起因是查询需要编码与名称都能查、且支持分类与分组名称，而输入框只保留一个；设计依据为阶段根 [design.md](design.md) 的 S2-D21、S2-D24、S2-D26、S2-D29 与 [Server/design.md](Server/design.md) 的契约段；矩阵为 V12-V14（条件面改写）、V35（新增）、C31、C35、C36（改写）与 C37（新增）。

### 口径（本轮定稿）

| 决策 | 内容 |
|---|---|
| 关键字跨四列 | 单个关键字按字面包含同时匹配标准项目编码、名称、所属分类名称、所属分组名称，任一列命中即返回；页面与弹窗各一个输入框，语义相同 |
| 不区分大小写 | 两侧统一 `lower()` 比较 |
| 请求字段替换 | 配置列表请求的过滤字段由 `standardProjectCode` **替换**为 `keyword`（不并存） |
| 新建查询 | 「可新增标准项目」查询（不分页）：`三级均启用的有效目录 − 本组织已配置`，入参同一关键字，组织只取可信上下文 |
| 页脚计数 | 改为**当前显示条数**（「共 M 项配置」），不再报「本组织共 N 项」——列表响应无总数来源，且页面行集就是本组织的配置集合 |
| 配置状态 | 保持本地即时裁剪，不下推（离散二选一、保留「切换即生效」） |
| 页面读取 | 不再读标准目录（读数 3→2）；弹窗打开/重读/搜索各 1 次读取 |
| 不分页 | S2-D7 不变 |

### 变更面

| 端 | 改动 |
|---|---|
| 契约 | `RecognitionProjectConfigurationListQueryRequest.StandardProjectCode` → `Keyword`；新增 `SelectableStandardItemListQueryRequest`（只有可选 `Keyword`）与 `QuerySelectableStandardItemListAsync`（返回与有效目录同形的层级只读模型） |
| SQL | `QueryRecognitionProjectConfigurationList` 的过滤条件改为四列 `lower(...) like concat('%', lower(replace...$Keyword...), '%') escape '!'` 的 OR；新增语句 `QuerySelectableStandardItemList`（三级均启用 + `not exists` 排除本组织已配置 + 同一关键字口径 + 排序同有效目录） |
| 应用层 | 配置列表透传 `Keyword`；新查询取可信组织后调仓储；仓储端口与实现新增 `QuerySelectableStandardItemListAsync(org, keyword)` |
| 冻结清单 | 五处同步：`Stage1ArchitectureTests`（查询方法集 + 签名表）、`Stage4EndpointTests`、`Stage2EnumContractTests`（路径清单 56→**57**）、`Stage7ExclusionGuardTests`（路径计数 56→**57**）、`Stage4SqlMapTests`（语句清单）、`Stage6SqlMapTests`（既有语句数 28→**29**） |
| 后端用例 | `Stage2QueryTests` 新增 2 条 V35；`Stage2ContractTests` 字段断言改 `Keyword` 并新增可新增查询请求形状断言；6 个测试替身补 `QuerySelectableStandardItemListAsync` |
| 前端 | 关键字下推（`buildConfigurationListQuery` 带 `keyword`）；`filterRecognitionProjectColumnView` 只按配置状态裁剪；页面读取降到 2 次；新增 `loadSelectableItems`／`reloadSelectableItems`／`searchSelectableItems`（弹窗数据源，独立请求序号，失败只影响弹窗内提示、不写页面阻断态）；`batchKeyword` 记录弹窗生效关键字（区分「没有匹配项」与「没有可新增项」）；清空输入同时清生效条件并重读一次（等价空条件提交）；页脚「共 M 项配置」；空态按「零配置」与「有条件但没命中」区分；新增按钮只按 `canWrite` 门控；收敛判定只在无关键字的全量可新增读取上做；弹窗 `reloading` 冻结确定按钮；路由参数 `code` → `keyword` |

### 门禁

| 门禁 | 结果 |
|---|---|
| 目标两文件 | `vitest run src/pages/recognitionProjects` **2 文件 / 153 通过** |
| 前端测试 | 全量 `vitest run` **21 文件通过 / 432 passed / 2 skipped（434）** |
| 前端 lint / 构建 | `eslint .` **0 problems**；`pnpm -F dy-medical-recognition build` exit 0 |
| 后端测试 | `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj` **819 通过 / 0 失败 / 0 跳过（819）** |
| 客户端包 | `prepare-openapi` **57 path**、`generate` 成功、`typecheck` exit 0、`build` exit 0 |
| 收尾清理后 | 目标两文件 **149 通过**、前端全量 **21 文件 / 428 passed / 2 skipped（430）**（删除两个已失去调用方的出口及其 4 条用例，见「收尾清理」） |
| 工作区 | `git diff --check` exit 0 |

### 矩阵结果

| 用例 | 层级 | 状态 | 证据 |
|---|---|---|---|
| V12-V14（条件面改写） | Contract/SQL | `Passed` | 契约字段断言（`Keyword` 可选、`OrganizationCode` 必填、状态可选且不进查询载荷）与 SQL 语句冻结清单（含新语句）；四列 OR 与转义、不区分大小写的 SQL 形态见 `MedicalRecognitionReportQuery.xml` 的语句注释 |
| V35 可新增标准项目查询 | Application/Contract | `Passed` | `Stage2QueryTests` 两条：组织取自可信上下文、关键字原样下传、结果按类型→分类→分组组装；无可新增项目时各层级为空、关键字空白在进入查询前被拒绝 |
| C31 搜索动作按关键字查询 | Component | `Passed` | 输入原文是草稿、提交时下发 `keyword`、读取返回后行集即服务端结果（含按名称命中的四列口径）、空条件提交等价刷新、清空输入触发重读；请求只含 `organizationCode` 与 `keyword`，`configurationStatus` 永不下发 |
| C35 弹窗发现能力 | Component | `Passed` | 关键字搜索发起一次可新增项目查询、不重读配置列表；无命中报「没有匹配的标准项目」；清空恢复全部；全选当前结果补齐可见项且不清除已有选择、再点不取消；整行命中区；本地拦截与 live region |
| C36 已生效筛选与路由参数一致 | Component | `Passed` | 参数名改 `keyword` 后仍通过：进入按参数初始化、变化后替换式回写、输入原文不写入、清空不残留 |
| C37 弹窗可选集合来自服务端查询 | Component | `Passed` | 弹窗打开即发起可新增项目查询（页面配置列表不因此重读）、可选集合按本次查询计算、失败只在弹窗内提示「数据待刷新」且页面列表与行内操作不受影响 |

业务统计：本轮 6 条（V12-V14 条件面、V35、C31、C35、C36、C37）全部 `Passed`；回归面 C1-C3、C6、C8、C10-C21、C23、C26-C34 保持 `Passed`。阶段合计 **72** 条：`Passed` 67、`Blocked` 4、`N/A` 1。

### 宿主链路取证（2026-10-02）

在宿主 `http://183.224.180.166:35000/` 登录后经菜单「检验检查结果互认 → 互认项目」进入页面，子应用由本地 dev server（`http://localhost:3008/subApps/medical-recognition/`）经基座实际加载；受管 Chrome 经 CDP 执行交互，读取次数由页面 `fetch` 记录器计数，DOM 事实取自可访问名称与元素计数。基线为组织 `01`：配置 301 条、三级均启用的有效目录 325 条、可新增 24 条；页面的四个关键字命中数（编码 10、名称 3、分类 116、分组 8）与弹窗的四个关键字命中数（编码 7、名称 1、分组 7、分类 24）均由同口径的只读 SQL 复核一致。

| 观测面 | 实测 |
|---|---|
| 页面基线 | 行集 301、页脚「共 301 项配置」、页头组织两行「县医共体 / 01」、输入框占位「标准项目编码/名称/分类/分组关键字」 |
| 编码列命中 | 提交 `002501010` → 行集 10、页脚「共 10 项配置」，命中行编码均为 `002501010…`；读取 2 次（配置列表 1 + 组织范围 1） |
| 名称列命中 | 提交 `血红蛋白` → 行集 3（血红蛋白（Hb）、平均红细胞血红蛋白含量(MCH)、平均红细胞血红蛋白浓度(MCHC)）；读取 2 次 |
| 分类列命中 | 提交 `检验专业` → 行集 116，层级栏只剩一个分类「检验专业」；读取 2 次 |
| 分组列命中 | 提交 `血常规检验` → 行集 8，层级栏只剩一个分组「血常规检验」；读取 2 次 |
| 草稿不读取 | 仅输入未提交时行集保持 301、读取计数无增量 |
| 空条件提交 | 清空后提交 → 回到 301，读取 2 次（等价刷新） |
| 状态本地裁剪 | 切「停用」→ 行集 0（该组织 301 条全部启用），读取计数无增量 |
| 弹窗打开 | 「新增互认项目」→ 读取 1 次且仅为 `QuerySelectableStandardItemList`（配置列表不重读），可选 24 条、四栏「类型｜分类｜分组｜项目」、`已选 0 项` |
| 弹窗编码列 | 提交 `002504030` → 可见 7 条，可见编码全部命中；读取 1 次 |
| 弹窗名称列 | 提交 `梅毒` → 可见 1 条；读取 1 次 |
| 弹窗分组列 | 提交 `传染病检验` → 可见 7 条（命中列为分组名，行标签不含该串）；读取 1 次 |
| 弹窗分类列 | 提交 `检验专业` → 可见 24 条（该分类下再无其他分组名含此串）；读取 1 次 |
| 弹窗重读 | 「重新读取」→ 读取 1 次（仅可新增查询），读取期间确定按钮 `disabled`、完成后恢复 |
| 选择语义 | 「全选当前结果」→ `已选 7 项`；切到不相交的名称列关键字（可见 1 条）后仍为 `已选 7 项`（子集搜索不裁剪已选）；点行内名称文本可切换该行勾选 |
| 路由往返 | 提交后子应用地址为 `…/recognition-projects?keyword=%E8%A1%80%E5%B8%B8%E8%A7%84%E6%A3%80%E9%AA%8C`；切「停用」为 `?keyword=…&status=2`；清空关键字后该参数不残留；草稿输入不写地址 |
| 控制台 | 交互全程只有 Vite 连接的 `debug` 消息，无 error/warning 级记录、无接口失败 |
| 未产生写入 | 弹窗以「取消」收尾；页面行集与页脚回到 301；库内 `mrec_mutual_recognition_item` 组织 `01` 仍为 301 条且 `oper_time` 最大值早于本次取证 |

证据文件：`var/host-check-recognitionProjects-f23/` 的探针脚本（`probe-baseline.js`、`install-recorder.js`、`probe-keyword-step.js`、`probe-modal-open.js`、`probe-modal-search.js`、`probe-modal-columns.js`、`probe-modal-reload.js`、`probe-url-iframe.js`、`close-modal.js`、`reset-filter.js`）与截图 `01-page-keyword-group.png`、`02-modal-keyword-category.png`（`var/` 由仓库根 `.gitignore` 排除，不入版本控制）。判定取自 DOM 计数、可访问名称与读取计数，截图只作留档。

### 收尾清理（2026-10-02，同轮）

| 项 | 内容 |
|---|---|
| 删除的出口 | `configuredStandardProjectCodes`、`excludeConfiguredStandardItems`（`recognitionProjectsApi.ts`）：S2-D29③ 把「本组织已配置」的排除收进服务端的可新增项目查询后，两个出口在页面侧已零引用，保留会在客户端与服务端各留一处判定 |
| 删除的用例 | `recognitionProjectsApi.test.ts` 的 C3 三例（排除集合构成、编码缺失行、剔除只按编码移除）与「C3 排除集合来自全量查询结果：停用配置同样阻止重复新增」一例；关键字下推后这些用例断言的本地集合运算已不存在 |
| C3 覆盖去向 | C3「已配置项目过滤」的排除判定指向后端 V35 与 SQL 语句 `QuerySelectableStandardItemList`（`Server/design.md`、`Client/Pages/RecognitionProjects/RecognitionProjects.md`、`Client/testPlan.md` 已同步）；客户端侧由 C37 覆盖「弹窗可选集合来自服务端查询」，C35 覆盖「弹窗搜索按服务端查询」 |
| 影响面核对 | GitNexus `impact` 两个出口均 `LOW`（`impactedCount 2`，浅层）；索引中指向 `RecognitionProjects.tsx` 的调用边是 F23 之前的旧边，按 grep 复核页面文件对两个名字已无引用 |
| 文档同步 | 阶段根 S2-D27（参数名 `code`→`keyword` 与「关键字筛选」措辞）、`Server/design.md`（查询请求字段 `Keyword`、可新增项目查询、V35 行）、`Client/design.md`（筛选/弹窗/页脚/读取路径/筛选口径）、`Client/Pages/RecognitionProjects/RecognitionProjects.md`、`Client/testPlan.md`（C3、C31、C35、C36 改写，C37 新增） |
| 门禁 | 目标两文件 `vitest run src/pages/recognitionProjects` **2 文件 / 149 通过**（清理前 153）；前端全量 **21 文件 / 428 passed / 2 skipped（430）**（清理前 432 passed）；`lint` 0 problems、构建 exit 0、`git diff --check` exit 0；本次未改后端，后端 819 通过 / 0 失败 继续有效 |

### 残余与边界

- **宿主地址栏归基座**：子应用地址（含 `keyword`）只落在子应用路由上，基座浏览器地址栏保持 `http://183.224.180.166:35000/`；宿主整页刷新后生效条件不保留，页面回到「共 301 项配置」。该边界即 S2-D27 已登记的口径；「进入时按地址状态初始化条件」由组件层 C36 用例覆盖，未在宿主上取得独立证据（宿主不提供携带该参数的整页入口）。
- **本轮行为用例未取得改动前 RED**：用例与实现同轮改写——`vitest` 的失败由实现先行使旧用例判红（当时记录为适配层 7 失败、页面 85/93 失败），随后按新行为重写期望；没有形成「先写新行为失败用例、再实现」的独立运行记录，与 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) §1.1 的顺序要求存在差别，如实登记。
- 本轮未改动数据库；后端只改契约、SQL 与用例。

## 结论

阶段2 的业务矩阵共 **72** 条（原 52 条 + 重开轮新增 11 条 + 筛选区交互整改 2 条 + 页头范围展示轮 2 条 + 修订轮新增 1 条 + 行集收敛与弹窗发现能力轮新增 2 条 + 关键字下推与可新增项目查询轮新增 2 条）：`Passed` 67 条、`Blocked` 4 条、`N/A` 1 条，`PendingRetest`、`NotRun`、`Failed` 均为 0。C31、C32、C33 的定义被"设计与交互修订轮（2026-10-02，批次24 / F21）"改写、C34 为同轮新增，四条已由"修订轮实施（2026-10-02，批次24 / F21）"复测回到 `Passed`，C34 的宿主面缺口另经同轮补证覆盖（失效勾选剔除）；C26、C28 的定义被"行集收敛与弹窗发现能力轮（2026-10-02，批次25 / F22）"改写并复测，C35、C36 为同轮新增，四条全部 `Passed`。后端离线与真实链路、Client 生成与契约、组件层、真实库元数据、宿主页面链路均有本报告列举的证据。4 条 `Blocked` 是**环境或框架边界导致规定验证面未执行**的项——组织切换后目标组织读取失败的宿主页面层（缺第二个授权组织）、事件可见性、并发停用的事件计数子面、跨组织写拒绝（缺少"第二个可信组织持有配置"的合法前置）；其受阻原因登记为决策标签（接受口径待负责人确认，不改变状态；决策标签不计入统计，4 条 `Blocked` 计入 63 条分母并单列），与 Testing Baseline §3 一致，本报告不再把它们记为通过。**2026-09-16 的批次A/B 整改后，受影响用例的宿主证据已按 Testing Baseline §5 重取**（见"批次A/B 整改后的宿主重取"），同日完成 7 轴整改后复审与收口前清理，清理后门禁为后端 **81/81**、前端 99 passed / 2 skipped、构建 0 错误、lint 0 error、`git diff --check` exit 0。**2026-09-29 的重开轮实施**新增 V28-V33 与 C26-C30 并全部取得证据（C27 曾记 `Failed`，修正后通过）；**2026-09-30 的补证轮**在重开轮终态工作区上重取了 C26、C28、V30，使新增 11 条的证据都对应同一份代码快照，该轮门禁为后端 **811 通过 / 0 失败**、前端 **415 通过 / 2 跳过**、`lint` 0 error 0 warning、构建 exit 0、`git diff --check` exit 0。**2026-10-02 的筛选区交互整改轮**按阶段根 S2-D21 删除筛选区重置控件并把标准项目编码筛选改为显式提交，新增 C31、C32 并通过组件层与真实宿主取证，该轮门禁为前端 **416 通过 / 2 跳过（418）**、`lint` 0 problems、构建 exit 0、`git diff --check` exit 0。**2026-10-02 的页头范围展示轮**按阶段根 S2-D22 与 S2-D23 新增只读范围查询与页头组织展示，新增 V34、C33 并通过契约层、组件层与真实宿主取证，该轮门禁为后端构建 0 错误、后端全量 **810 通过 / 6 失败**（6 条为 `Stage4WritePathTests` 的既有时间硬编码缺陷，与本轮无关，已于 2026-10-02 修复测试夹具，修复后为 816 通过 / 0 失败）、客户端包 `typecheck`/`build` exit 0、前端 **420 通过 / 2 跳过（422）**、`lint` 0 problems、构建 exit 0、`git diff --check` exit 0。**2026-10-02 的修订轮实施**按阶段根 S2-D22～S2-D25 把搜索动作改为重新读取后再本地裁剪、页头组织改为上下两行且去掉文字标签、页头只保留主按钮、新增弹窗打开即重读并在弹窗内提供「重新读取」与失效勾选剔除，C31/C32/C33 重写后复测、C34 新增，四条全部 `Passed`；该轮门禁为前端目标两文件 **144 通过**、前端全量 **423 通过 / 2 跳过（425）**、`lint` 0 problems、构建 exit 0、`git diff --check` exit 0，并在整页重载后的宿主链路上取得页头版面、筛选区控件构成、搜索重读与裁剪、在途形态与重复点击守卫、弹窗就地重读的实测证据。阶段是否收口由收口节结论承载，本报告不代作结论。**2026-10-02 的关键字下推与可新增项目查询轮**把筛选关键字改为服务端查询条件并跨「标准项目编码、名称、所属分类名称、所属分组名称」四列匹配（不区分大小写、`%`/`_` 转义），新建不分页的「可新增标准项目」查询（`有效目录 − 本组织已配置`），页面与弹窗都不再持有全量数据：页面读取降到 2 次、弹窗打开/重读/搜索各 1 次，页脚改报当前显示条数，V35 与 C37 新增、V12-V14 条件面与 C31/C35/C36 改写，六条全部 `Passed`；该轮门禁为目标两文件 **153 通过**、前端全量 **432 通过 / 2 跳过（434）**、后端全量 **819 通过 / 0 失败**、客户端包 57 path 重生成、`lint` 0 problems、构建 exit 0、`git diff --check` exit 0，并在真实宿主链路上取得关键字四列命中、页脚按当前显示条数、草稿不读取、空条件提交等价刷新、弹窗打开/重读/搜索各 1 次可新增查询、子集搜索不裁剪已选与路由参数 `keyword` 往返的实测证据。**2026-10-02 的行集收敛与弹窗发现能力轮**按阶段根 S2-D16／S2-D17／S2-D24 的修订与 S2-D26～S2-D28 把页面行集收敛为本组织已配置项（未配置项目只在新增弹窗出现）、弹窗补编码搜索与全选当前结果、已生效筛选同步到子应用路由地址、长列表按 `content-visibility: auto` 声明离屏跳过渲染，C26/C28 改写复测并新增 C35/C36，四条全部 `Passed`；该轮门禁为前端目标两文件 **153 通过**、前端全量 **432 通过 / 2 跳过（434）**、后端全量 **816 通过 / 0 失败**、`lint` 0 problems、构建 exit 0、`git diff --check` exit 0，并在整页重载后的宿主链路上取得行集与计数一致、弹窗搜索零读取增量、全选、命中区与路由参数往返的实测证据；该轮同时如实登记了路由参数在宿主内的适用边界（浏览器地址栏归宿主外壳、不跨宿主刷新保留）。

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
