# 阶段 3 测试报告

**阶段 3 已完成并于 2026-09-17 收口（负责人裁定，阶段状态 `Complete`）**：矩阵内可构造的用例已全部取得实际证据（`Passed` 48 / `Failed` 0），不可构造的 8 条已按环境边界逐条登记（`Blocked` 4：V29、V30、C3、C11；`NotRun` 4：V16、C15、C22、C23），另有 V27 的事件可见性子面记 `N/A` + `AcceptedRisk`。结论见本报告「结论」节；收口状态同时登记在 [总体计划进度表](../001-总体计划/impl.md) 的阶段状态列。**2026-10-09 重开三页统一轮收口（负责人裁定，阶段状态维持 `Complete`）**：重开轮矩阵 69 条中 64 条取得完整证据，剩余 5 条（V29、V30、C11、V16、C23）全部是已登记的环境边界，无可继续取证的条目；终值见「结果」节与「结论」节重开轮段。

2026-10-08 阶段重开三页统一轮（票 10）把矩阵扩为后端 V1-V33、前端 C1-C36（总分母 69）；本报告在既有证据之上追加该轮结果，后端第一棒的结果与证据见「重开轮：两金额页统一（票 10 后端第一棒）」节，上句统计是 2026-09-17 收口轮次的结果，当前矩阵统计见「结果」节；2026-10-09 重开轮取证完成并经负责人裁定收口。

## 执行边界

本报告记录阶段 3 的验证矩阵与执行结果。下述「执行边界」及其后的结果行描述 2026-09-17 收口前的**六项生产代码修复后的聚焦复验轮**：在既有真实数据库与真实宿主链路上重跑受修复影响的用例，库内结果用 dbx 只读回读；未使用接口直连造数，未写库、未改表、未改请求绕过任何校验。2026-10-08 重开轮的执行边界、进程与命令另记于「重开轮：两金额页统一（票 10 后端第一棒）」节。

- 报告状态：2026-09-17 收口轮次为 56 条中 48 条取得完整证据、8 条为环境不可构造项（见「已确认的验证降级」节），无确认失败；2026-10-08 重开轮后矩阵为 69 条，当前统计见「结果」节。后端 V31-V33 与 API Client 棒已取证；前端两个页面的两阶段改版（骨架与筛选、行内编辑与状态机制）已完成并经两轮独立审查与三轮回改，宿主面复测已于 2026-10-09 执行四轮（见「重开轮宿主面复测」第一至第四轮四节）。当前矩阵状态：`Passed` 64 / `Failed` 0 / `Blocked` 3（V29、V30、C11）/ `NotRun` 2（V16、C23）/ `PendingRetest` **0**，剩余条目全部是已登记的环境边界。第四轮按 [Client/testPlan.md](Client/testPlan.md) 逐条完成六条原 `PendingRetest`（C10、C13、C14、C18、C19、C20）的宿主复测，并对 C3、C15 做受控构造补证（两者由 `Blocked`/`NotRun` 转 `Passed`）。四个问题均已处置：平台页初始范围偏离 S3-D22 已回改并宿主复测通过；C34 已按方案甲两页回改（医院页另补「提交发起时现读」）；S3-D28 档二裁定（院区层按缺失值参与比对、两侧同缺失即放行，「现读不到」收窄到可信组织或医院）已按三处设计正文落地，组件面三段 RED、宿主面三场景两页全部取证（见第三轮节，档二按受控构造取证、原拟的环境边界降级未使用）；医院页提交期现读缺口已回改并宿主复测通过。另有两处过程登记：取证前置需重启开发服务器（陈旧转换缓存）、并行 GitNexus 索引导致的默认超时用例复测口径（见「验证与静态检查」节，终值：后端 **833 通过 / 0 失败**、子应用 **549 通过 / 2 跳过 / 0 失败**、金额目录 **180 通过**）。
- 矩阵版本：2026-09-16 版本为后端 V1-V30（30 条）、前端 C1-C26（26 条）、总分母 56 条；2026-10-08 重开轮扩为后端 V1-V33（33 条）、前端 C1-C36（36 条）、总分母 69 条，新增 V31-V33 与 C27-C36；多层级一条只计一次。
- 后端矩阵定义见 [Server/design.md](Server/design.md) 的验证矩阵章节；前端矩阵定义见 [Client/testPlan.md](Client/testPlan.md)。
- 起始提交与起始工作区状态：`main`（`origin/main` 领先 3 个提交）；工作区中 `server/`、`client/` 与本阶段设计、实施文档的既有改动**不是本轮产生**，本轮只写入本报告，未修改任何实现文件、配置或矩阵编号。
- 执行轮次使用的进程与地址：后端 `http://localhost:15014`、前端 Vite dev server `http://localhost:3008/subApps/medical-recognition/`、宿主 `http://183.224.180.166:35000`。三者由协调者以受管后台任务启动，本轮未启动、未关闭、未重启任何进程；浏览器为 Chrome DevTools MCP（页面 `pageId=1`、`pageId=3`，宿主页 `http://183.224.180.166:35000/`），进入前对两个宿主标签页各做一次忽略缓存的硬刷新；数据库只读取证使用 dbx MCP 连接 `70c7d4c1-bab7-4aab-bf5e-69fa544b55bc`（PostgreSQL `183.224.180.166:15432` / 库 `DysoftHIS` / schema `public`），只执行 `select`。
- 本轮两个身份都重新经宿主登录页登录取证。实际登录令牌载荷（仅列非敏感声明，两个身份同形）：`org:"01"`、`sub:"medical-recognition"`、`usr:"<登录用户标识>"`；**两个身份的令牌都没有医院声明**。客户端存储 `dy-auth:login-user` 分别含 `orgID:"01"` 与各自的 `hosID`/`branchID`。
- 资源来源与拦截核对：宿主 Network 中 `@vite/client`、`src/main.tsx`、`src/router/index.tsx`、`src/runtimeConfig.ts`、`@dy_components-base.js`、`recognitionAmounts/*` 与两个阶段 3 页面模块均来自 `http://localhost:3008`；Console 出现 `[vite] connecting.../connected.`（debug 级），拦截生效。
- 阶段根决策台账中的全部决策均已确认或按 `AcceptedRisk` 登记，**无 `Pending` 项**。
- **本轮归因修正**：上一轮把 25 条用例记 `NotRun`，理由统一写成"本轮层级为真实库与真实入口"。经逐条核对矩阵层级列与"真实数据库"列，其中 **20** 条的层级是 `Contract`/`Domain`/`Application`/`Query`/`Static`（宿主侧为 `Component`）且"真实数据库"列为"否"，不需要真实数据库即可取证，且后端与前端测试中存在指向该用例边界的具体断言；其余 5 条的"真实数据库"列为"是"（V11、V16、V20、V27）或宿主层上下文不可构造（C15）而保持 `NotRun`。本轮把前 20 条改按各自层级记录证据，并逐条写明证据复用条件（见「结果」表与「证据复用条件」节）。报告原先把"轮次范围"当成"用例范围"，导致统计系统性低报，本轮修正。此后追加的「补充取证」轮取得组织无互认配置的真实状态，使 V11、V20、V27 与 C10、C17 的五条取得完整真实证据，V16 仍因并发同步屏障不可构造而保持 `NotRun`。
- **端口与地址口径**：本地后端端口统一为 `15014`——三处启动配置（`appsettings.json`、`appsettings.Development.json`、`Properties/launchSettings.json`）、`HttpConfig` 默认服务地址、前端 `config.development.json` 的 `apiBaseUrl` 与 `src/runtimeConfig.ts` 的回落地址同步；子应用生产配置 `public/config.json` 的 `apiBaseUrl` 登记为部署占位地址 `http://183.224.180.166:35014`（尚未部署），`baseApiBaseUrl` 为 `http://183.224.180.166:35001`。本报告内全部接口地址按当前环境端口 `15014` 统一书写，环境值以 [Test Environment](../../../.agents/instructions/test-environment.md) 为准。

## 已确认的验证降级

**V29（平台管理员入口跨组织保存与查询）的跨组织真实宿主面降级**（决策 S3-D20）：

- 原因：本环境无法维护第二个组织，跨组织的真实宿主面在本环境不可构造。本轮再次只读回读组织服务：`POST http://183.224.180.166:35001/Api/Organization/QueryAllOrganization` 返回 `200` 且响应体只有一项 `{"id":"01","name":"县医共体","isValid":true}`。
- 落点：该用例以**应用层证据**为准并记 `AcceptedRisk`，真实宿主面记 `Blocked`。
- 不因此改变取值口径（S3-D17）：平台管理员入口的组织、医院、院区仍按请求提交值使用，服务端仍校验三者存在、启用与父子归属。
- 该降级同时意味着 **SRS F10 前置条件 2 中"组织、医院和院区范围由可信上下文确定，**或按平台管理员授权选择**"的第二个分支在本环境无法取得真实证据**。
- `AcceptedRisk` 是决策标签，不计入测试结果统计，也不替代 `Passed`。

**V27（无 WorkUnit 生产入口的写入与失败响应）的事件可见性子面记 `N/A` 并附 `AcceptedRisk`**：

- 原因：本项目零 `IEventHandler` 实现、无事件持久化表、无专用观察点，事件不可见。
- 落点：该子面记 `N/A` + `AcceptedRisk`；**该 `N/A` 不使整条 V27 结案**，V27 仍须由"数据库写入面 + 失败响应面 + 无脏写面"的真实证据单独取证。三个面均已取得：成功面与回读一致面为两个身份、两个院区各一次经宿主页面的新增写入；失败响应面与无脏写面见「补充取证」节（真实生产入口返回 `业务拒绝：当前组织未建立该标准项目的互认项目配置。`，回读 0 行、金额表总行数不变），故 V27 记 `Passed`。
**阶段收口时接受的环境边界条目**（按 `AcceptedRisk` 登记；各条状态按实际证据保留，不因接受边界而改变）：

| 条目 | 状态 | 环境边界的技术原因 | 该条已取得的证据 |
|---|---|---|---|
| V16 | `NotRun` | 真实并发同步屏障不可构造：两次提交被串行化为「插入 + 更新」，无法使两条 INSERT 真正交叠；授权范围不允许接口直连制造并发 | 应用层唯一冲突翻译由 `Stage3WritePathTests.Save_platform_amount_translates_unique_violation_into_business_rejection` 覆盖（另一层证据，不替代本层） |
| V29 | `Blocked` | 环境无第二个组织（只读回读 `QueryAllOrganization` 仅 `01/县医共体`），跨组织真实宿主面不可构造（决策 S3-D20） | 应用层跨组织用例 + 平台页三级选择与三值请求的真实宿主证据 |
| V30 | `Blocked` | 医院页院区下拉只列可信医院下的院区，第二类拒绝面（提交它医院院区）不可构造 | 允许面完整真实证据（两身份的默认可信院区、查询与保存请求体、落库回读） |
| C3 | `Blocked`（**2026-10-09 已解除，转 `Passed`**） | 原登记：环境不存在 `branch_id` 为空的测试身份；当时认为授权范围不允许改写认证存储 | 组件层用例 + 医院页只读可信范围副标题与院区默认值的真实证据；**2026-10-09 第三、四轮**：按受控构造（改写 `dy-auth:login-user.branchID` 置空 → 整页重载 → 写回并复核）取得「默认选中该院首项并自动加载」的宿主证据，见结果表 C3 行与「重开轮宿主面复测 · 第四轮」节 |
| C11 | `Blocked` | 本条要求两类拒绝前置均取得，第二类（提交它医院院区）不可构造 | 第一类前置完整真实证据（业务拒绝文案、页面呈现、零脏写） |
| C15 | `NotRun`（**2026-10-09 已解除，转 `Passed`**） | 原登记与 C3 同源：不存在组织或医院缺失的测试身份上下文 | 组件层用例（阻断提示、不渲染院区下拉、零业务请求）；**2026-10-09 第四轮**：按受控构造（`hosID` 置空 → 整页重载 → 写回并复核）取得阻断态宿主证据且全程零业务请求，见结果表 C15 行与第四轮节 |
| C22 | `NotRun`（**2026-10-09 已解除，转 `Passed`**） | 收口时无实际业务规模数据（金额表 11 行、组织 1 个、院区 4 个），"真实业务数据量下不出现卡顿、超时或内存问题"无对应规模证据 | "未新增分页"可观察；组件层断言页面不分页。**2026-10-09**：组织 `01` 现有 301 条互认配置，两页在该规模下的渲染、筛选与保存均正常，证据见结果表 C22 行 |
| C23 | `NotRun` | 组织服务选项请求失败的宿主层注入点未命中（子应用在宿主重开标签时未命中文档级钩子），本轮未再尝试 | 组件层用例以替身构造选项读取失败并断言组件自身提示 |

- `AcceptedRisk` 是决策标签，不计入测试结果统计。

## 证据复用条件

按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节，生产代码变化后与变更影响不重叠的证据方可继续使用。本轮按层级记录证据时统一声明以下条件；逐条差异只在行内注明。

- **代码版本**：本轮修复后的工作区版本（起始提交 `f923c34`，工作区含六项修复的未提交改动）。后端测试与前端测试都在该版本上重跑。
- **impact 范围**：六项修复分别是——后端金额请求字段可空化（缺省金额改由必填校验拒绝）、后端可信范围返回值统一去空白编码、前端金额解析新增"内存面 + 序列化面"两道可精确表示性判据、前端医院页院区默认值改为随可信范围派生。契约、领域、应用、查询与静态层用例的断言边界落在这些改动**之外**的字段集合、可空性、拒绝文案、归属校验、原因派生、字段顺序与语句文本上，代码在修复后未再变更。
- **环境**：`.NET 10.0.401`；后端测试用替身（`FakeReportRepository`、`FakeOrganizationAppService`、`StubUserAppService`、`TrustedRequestContext`、`RecordingEventQueue`），不连库、不发真实 HTTP；前端测试用 vitest + jsdom 与 `@testing-library/react`。
- **时间**：后端 `dotnet test server/Dy.MedicalRecognition.Tests/Dy.MedicalRecognition.Tests.csproj --no-build --no-restore` → **测试总数 243、通过数 243、失败 0**，总时间 1.4694 秒；前端 `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionAmounts` → **Test Files 3 passed (3)、Tests 56 passed (56)**，耗时 7.30 秒。两次执行都在本轮、两个进程都在运行中的同一环境内完成。另以**完整重建**复核后端（不加 `--no-build`，即由当前工作区源码重新构建后执行）→ **失败 0、通过 243、总计 243**，耗时 956 毫秒；该次执行不依赖任何既有构建产物，断言对应的就是修复后代码。生产代码位于各自程序集而非测试程序集：`Dy.MedicalRecognition.Application.dll`（01:06:25）晚于 `MedicalRecognitionReportAppService.cs`（01:06:15）、`Dy.MedicalRecognition.Domain.dll`（01:06:36）晚于 `MedicalRecognitionReportManager.cs`（01:06:30），故 `--no-build` 那次复用的同样是修复后产物。
- **复用理由**：这些用例的"真实数据库"列为"否"，其规定验证面在契约、领域、应用或组件层即可完整覆盖；重跑结果直接构成本轮证据，不需要真实库层证据补齐。
- **不适用范围**：任何"真实数据库"列为"是"的用例（V1-V3、V7、V11、V15-V17、V20、V21、V23、V24、V27、V30）不使用本节复用，只用本轮真实库与真实宿主证据。

## 结果

| 用例编号 | 验证面 | 状态 | 命令/操作及证据 | 未覆盖面/原因 |
|---|---|---|---|---|
| V1 | 首次保存金额（无记录） | `Passed` | ① 医院管理员入口 `lisadmin`（可信范围 `01/CSYY2`）在院区 `CSYQ2-2`（该业务键库中本为 0 行）弹窗提交 `55.55`：`POST /Api/MedicalRecognitionReport/SaveBranchRecognitionAmount` 体 `{"branchCode":"CSYQ2-2","currentAmount":55.55,"standardProjectCode":"123"}` 返回 `200 true`；dbx 回读该业务键 1 行、`current_amount=55.55`、`oper_time=2026-09-17T02:13:33.518282+08:00`、`oper_id` 为该身份。② 医院管理员入口 `yangkj`（可信范围 `01/0101`）在院区 `0101001` 对未配置行 `222` 提交 `37.37`：同一端点返回 `200 true`；dbx 回读新增行 `01/0101/0101001/222 = 37.37`。 | — |
| V2 | 覆盖已有金额 | `Passed` | 对 `CSYY2/CSYQ2-1/123` 提交同值 `66.66`（弹窗回填值不改）：`POST /Api/MedicalRecognitionReport/SaveBranchRecognitionAmount` 返回 `200 true`；dbx 回读仍为 **1 行**、`current_amount=66.66`（非累加）、`oper_time` 由 `00:35:47.841632` 更新为 `02:15:03.621658`、`oper_id` 不变。整表回读全表 11 行，无第二行落入同一业务键。 | — |
| V3 | 保存零元 | `Passed` | 行 `CSYY1/CSYQ1-1/222` 库中 `current_amount=0.00`；平台页该行「当前金额」为 `<span class="recognition-amounts-number">0.00</span>`、「金额状态」为绿色 `已配置` 标签，与同屏未配置行的 `未配置` 标签在 DOM 结构与颜色上可区分。 | — |
| V4 | 金额为负数 | `Passed` | **按层级记录**（`Contract`/`Domain`，真实数据库列"否"）。后端 `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs`：`Save_platform_amount_rejects_negative_amount_without_writing_or_registering_event` 以 `CurrentAmount = -0.01m` 断言 `error.Message == "业务拒绝：金额不得小于零且最多两位小数。"`、`repository.WriteCalls == 0`、`repository.Amounts` 空、`events.Events` 空；`Manager_rejects_negative_amount_and_amount_beyond_two_decimals` 以 `-1m` 在领域管理器上断言同一文案与零写入。前端同边界另有 `RecognitionAmounts.test.tsx` 的 C8 用例覆盖。复用条件见「证据复用条件」节。 | 本层覆盖负数在契约/领域层的拒绝与零写入；真实库面不属本条层级 |
| V5 | 金额超过两位小数 | `Passed` | **按层级记录**（`Contract`/`Domain`）。`Stage3WritePathTests.Save_platform_amount_rejects_amount_with_more_than_two_decimal_places` 以 `1.005m` 断言同一拒绝文案与 `AssertNothingWritten`；`Manager_rejects_negative_amount_and_amount_beyond_two_decimals` 对 `0.001m`、`12.345m` 断言同一文案与零写入，并对照断言 `10.50m` 被接受且落库为 `10.50m`（判据不误伤合法两位小数）。 | 同上 |
| V6 | 金额缺省或非数值 | `Passed` | **按层级记录**（`Contract`）。`Stage3WritePathTests.Amount_request_declares_required_amount` 三重断言：两个保存请求的 `CurrentAmount` 都带 `RequiredAttribute`；属性类型必须是 `decimal?`（可空是特性生效的前提，不可空时缺省会被反序列化为 `0` 使必填声明恒真）；`MedicalRecognitionRequestValidator.Validate` 对 `CurrentAmount = null` 抛 `ValidationException` 且消息含 `参数校验失败：当前金额不能为空。`，而 `CurrentAmount = 0m` 通过校验。对照用例 `Amount_request_accepts_zero_and_regular_amount` 断言 `0m` 与 `12.34m` 通过校验。修复已使"金额缺省被拒绝"成为真实行为并有断言，本条据此转正。 | 本层覆盖缺省与非空校验；"非数值文本"由请求反序列化与该契约断言共同约束，未单独构造非数值报文 |
| V7 | 同值重复保存 | `Passed` | 弹窗回填 `66.66` 不改直接提交：请求已发出并返回成功；dbx 回读金额不变、`oper_time` 由 `00:35:47.841674` 段更新为 `02:15:03.621658`、`oper_id` 不变 → 无"无变更短路"。 | — |
| V8 | 保存的组织不存在或已停用 | `Passed` | **按层级记录**（`Application`，真实数据库列"否"）。`server/Dy.MedicalRecognition.Tests/Stage3OrganizationPathTests.cs`：`Resolve_rejects_when_the_organization_does_not_exist` 与 `Resolve_rejects_when_the_organization_is_disabled` 分别以"启用组织集合无该编码"和"该组织 `isValid:false`"断言 `InvalidOperationException` 且消息为 `组织不可用。`。 | 未覆盖面：本条只覆盖应用层拒绝；"拒绝时零写库"由写入口的 `AssertNothingWritten` 类断言在 V4/V9/V10 同源路径覆盖 |
| V9 | 保存的医院不存在、已停用或不属于该组织 | `Passed` | **按层级记录**（`Application`）。同文件三个用例分别断言：`Resolve_rejects_when_the_hospital_does_not_exist`（医院集合无该编码）、`Resolve_rejects_when_the_hospital_is_disabled`（医院 `isValid:false`，即使外部服务把它放进启用集合）、`Resolve_rejects_when_the_hospital_belongs_to_another_organization`（医院存在但父组织不匹配）——三者均抛 `InvalidOperationException` 且消息为 `医院不可用。` | 同上 |
| V10 | 保存的院区不存在、已停用或不属于该医院 | `Passed` | **按层级记录**（`Application`）。同文件三个用例分别断言：`Resolve_rejects_when_the_branch_does_not_exist`（院区集合无该编码）、`Resolve_rejects_when_the_branch_is_disabled`（院区 `isValid:false`）、`Resolve_rejects_when_the_requested_branch_belongs_to_another_hospital`（父院区不匹配），另有 `..._belongs_to_another_organization`（父组织不匹配）——四者均抛 `InvalidOperationException` 且消息为 `院区不可用。` | 同上 |
| V11 | 当前组织未建立该标准项目的互认配置 | `Passed` | 组织 `01` 的全部互认配置被删除后，经宿主医院管理员入口提交未配置行 `S1-D29-ITEM-20260915-001`：`POST /Api/MedicalRecognitionReport/SaveBranchRecognitionAmount` 返回 **500**，响应体与页面警示条均为 `业务拒绝：当前组织未建立该标准项目的互认项目配置。`，堆栈指向 `MedicalRecognitionReportManager.cs:line 406`；只读回读该业务键 **0 行**、金额表总行数不变（仍 **11 行**），即拒绝且无写入。`无事件` 由本条层级 `Domain` 的 `Stage3WritePathTests.Save_platform_amount_is_rejected_when_mutual_recognition_configuration_is_missing`（`Assert.Empty(events.Events)`）覆盖。取证序列见「补充取证」节 | 未覆盖面：生产入口无事件观察点，事件登记与否只在领域层断言，未在真实库观测 |
| V12 | 平台管理员入口取值来源 | `Passed` | **按层级记录**（`Application`/`Contract`，真实数据库列"否"）。`Stage3WritePathTests.Platform_entrypoint_uses_request_path_and_trusted_operator_only`：在可信上下文为 `ORG-A` 时以请求组织 `ORG-B`、医院 `HOS-2`、院区 `BRH-2` 保存，断言落库三元组等于**请求值**（`saved.OrganizationCode == "ORG-B"` 等）、`saved.OperId == TrustedOperId`（操作人只来自可信上下文）；并断言 `SaveOrganizationHospitalBranchRecognitionAmountRequest` 上不存在 `OperId`、`OperTime`、`Id` 属性。本轮宿主侧另取得对照证据（见 C2：请求体确实携带三值）。 | 未覆盖面：本条不覆盖跨组织返回的金额与名称归属；该子面由 V29 的应用层用例 `Platform_entrypoint_queries_the_requested_organization_even_when_it_differs_from_the_trusted_context` 覆盖，宿主面仍是 S3-D20 已接受边界 |
| V13 | 医院管理员入口取值来源与越权院区 | `Passed` | **按层级记录**（`Application`/`Contract`）。① 取值来源：`Stage3WritePathTests.Branch_entrypoint_takes_path_from_trusted_context_for_own_branch` 断言落库组织与医院取自可信上下文、院区取请求，并断言 `SaveBranchRecognitionAmountRequest` 上不存在 `OrganizationCode`、`HospitalCode`、`OperId`、`OperTime`。② 越权院区拒绝：`Branch_entrypoint_rejects_branch_of_another_hospital` 以另一家医院的院区提交，断言消息含 `院区` 且零写入零事件；`Stage3OrganizationPathTests.Resolve_rejects_when_the_requested_branch_belongs_to_another_hospital` 与 `..._belongs_to_another_organization` 给出同一拒绝的两条归属边界。 | 未覆盖面：本条按矩阵层级 `Application`/`Contract` 记录，「真实数据库」列为「否」，规定验证面即请求取值来源与越权院区拒绝，已由上述断言完整覆盖，属本层完整证据。**真实宿主层的越权院区提交面不属本条规定验证面**，由 C10/C11 承载；本轮在该层的观测为：医院页院区下拉只列可信医院下的院区（`yangkj` 选项集 `{总院}`），在 `yangkj` 会话中向院区下拉键入 `CSYQ2-1` 后选项集为空（`暂无数据`）、已选值仍为 `总院`、按回车不提交、列表仍为 `总院` 的 5 行且无新请求（Network 请求数无增长） |
| V14 | 医院管理员入口可信组织或医院缺失，以及令牌缺失层由用户档案补齐 | `Passed` | **按层级记录**（`Application`，真实数据库列"否"）。① 缺失即拒绝：`Stage3WritePathTests.Branch_entrypoint_is_rejected_when_trusted_organization_or_hospital_is_missing` 对 `(null,"组织")`、`("   ","组织")`、`(null,"医院")`、`("   ","医院")` 四组断言"读不到用户档案时含 `用户`"与"档案为空时含该层文案"两种拒绝，并断言 `organizationService.OrganizationReadCount == 0`（在任何组织路径读取之前拒绝）。② 补齐语义：`Stage3TrustedScopeTests` 的 `Resolve_fills_the_missing_hospital_from_the_current_user_profile`、`Resolve_trims_the_profile_hospital_when_filling_the_missing_layer`、`Resolve_fills_both_layers_from_the_current_user_profile` 断言令牌缺失层由用户档案补齐；`Resolve_rejects_when_token_organization_conflicts_with_the_user_profile`、`Resolve_rejects_when_token_hospital_conflicts_with_the_user_profile` 断言令牌与档案同层不一致即拒绝。本轮宿主侧另取得该补齐路径的真实运行证据（两个身份令牌都不带医院声明，页面仍取到可信医院，见 C3/C4）。 | 未覆盖面：本条不覆盖"补齐后仍为空时不降级为空值"的宿主层呈现；该子面所属的 C3"可信院区缺失"上下文在本环境不可构造（见 C3 与「未覆盖面与残余风险」） |
| V15 | 标准目录或互认配置停用后仍可保存金额 | `Passed` | 属"真实数据库"列为"是"的用例，本轮不重跑，保留上一轮已取得并已复核恢复的真实证据：经阶段 1 页停用分类 `S1-RETRY-CAT-20260914-001111` 后金额页 `123` 行原因列为 `所属分类已停用`、提交 `77.77` 成功且 dbx 回读 `77.77`；经阶段 2 页停用 `S1-D29-ITEM-20260915-001` 的互认配置后该行显示 `互认配置状态=停用`、提交 `88.88` 成功且 dbx 回读 `88.88`；两处均已恢复。本轮只读复核：组织 `01` 的 5 条互认配置 `is_valid` 全为 `true`，分类与分组状态为原状。 | 未覆盖面：本轮的目录/配置停用-恢复操作未重做（该面不经页面写库无法构造，属阶段 1/2 能力）；受影响性判断见「证据复用条件」的 impact 范围——六项修复不改动停用判定与原因派生。其余同上一轮 |
| V16 | 并发首次保存同一业务键 | `NotRun` | 已实际发起双标签页近同时提交（上一轮）：同一新业务键两次提交相隔约 2.26 秒、两次均返回成功、dbx 回读恰 1 行且为后提交值。本轮未再尝试双标签页交叠。 | **未观察到唯一约束冲突**："真实数据库"列为"是"，同步屏障不可构造。两次读-判-写被串行化（先插入、后更新），无法使两条 INSERT 真正交叠。授权范围不允许接口直连造并发，故本层保持 `NotRun`；应用层唯一冲突翻译由 `Stage3WritePathTests.Save_platform_amount_translates_unique_violation_into_business_rejection`（断言 `DuplicateAmountMessage`、`InsertCalls==1`、`UpdateCalls==0`、无事件）覆盖，属另一层证据，不替代本层 |
| V17 | 行集由互认配置驱动、金额 LEFT JOIN | `Passed` | ① 平台页本轮实测行数 = 5（`123`、`222`、`S1-D29-ITEM-20260915-001`、`S1-RETRY-ITEM-20260914-001`、`S1-RETRY-ITEM-20260914-002`），与 dbx 中组织 `01` 的配置数 `5` 一致。② 医院管理员入口两个身份各为 5 行且 `standardProjectCode` 集合与配置集合逐一相同；该业务键在库中无金额行时 5 行全部 `currentAmount:null`、`isAmountConfigured:false`（`yangkj` 首查响应），页面显示 `未配置` 且无数值；补写 `222` 后重查仅该行为 `37.37`/已配置 → LEFT JOIN 未把无金额行丢弃。 | — |
| V18 | 返回字段集合 | `Passed` | **按层级记录**（`Query`/`Contract`，真实数据库列"否"）。① `Stage3QueryTests.Amount_read_model_exposes_only_the_designed_fields_with_four_names` 断言 `RecognitionAmountReadModel` 属性名集合恰好为 **14** 项（含 `ItemTypeText`、`ConfigurationStatusText`）、`OrganizationCode`/`HospitalCode`/`BranchCode`/`LastModifiedTime`/`LastModifiedBy`/`CategoryIsValid`/`GroupIsValid`/`ItemIsValid` 全部为 `null`（不存在），并断言四个名称取到 `示例组织`/`示例医院`/`示例院区`/`血常规`。② `Stage3WritePathTests.Save_requests_and_read_model_expose_only_the_designed_members` 从类型侧断言同一 14 项集合、`CurrentAmount` 为 `decimal?`、`ItemTypeText` 可空与 `ConfigurationStatusText` 非空（S3-D21）。宿主侧另有 C5 的 DOM 表头核对。 | 未覆盖面：本条不覆盖"已保存与未保存金额各一行"的数据行内容；该子面由 V17 与 C6/C7 覆盖 |
| V19 | 名称与归属读取次数与行数无关 | `Passed` | **按层级记录**（`Application`）。`Stage3QueryTests.Organization_read_counts_do_not_grow_with_the_number_of_returned_rows` 在 10 行与 100 行两组数据下断言外部读取计数恒为 `(组织 1, 医院 1, 院区 1)` 且两组完全相等；`Stage3OrganizationPathTests.External_read_counts_do_not_grow_with_the_number_of_target_rows` 断言两条目标路径在 10 行与 100 行下计数相同且为 `(1, 1, 2)`。 | — |
| V20 | 排序与空结果 | `Passed` | ① 排序：平台页 5 行顺序为 `123`、`222`、`S1-D29-ITEM-20260915-001`、`S1-RETRY-ITEM-20260914-001`、`S1-RETRY-ITEM-20260914-002`，即标准项目编码升序；医院管理员入口两个身份的 5 行响应与 DOM 同样为该升序（互认配置删除前的取证）。② 空结果：删除组织 `01` 全部互认配置后，`reqid=104` `POST /Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList` 体 `{"branchCode":"0101001"}` 返回 **200**、`content-type: application/json`、响应体 **`[]`**，即无互认配置时成功返回空集合。取证序列见「补充取证」节 | — |
| V21 | `UnavailableReason` 与 `ConfigurationStatus` | `Passed` | 逐层独立停用后刷新实测（上一轮取得的真实证据，本轮只读复核状态未变）：仅停用分类 `S1-RETRY-CAT-20260914-001111` → 其下三行原因列 `所属分类已停用`；仅停用分组 `S1-RETRY-GRP-20260914-001` → 同一三行 `所属分组已停用`，另一分类 `S1-RETRY-CAT-20260914-002` 下两行为空；仅停用标准项目 `S1-D29-ITEM-20260915-001` → 该行 `标准项目已停用`，其余四行为空；三层全启用时各行原因为空；配置自身停用时 `ConfigurationStatus=停用` 且原因列为空（见 C19）。本轮只读复核：分类、分组、标准项目与 5 条配置的启用状态均为原状。 | 未覆盖面：本轮未重做逐层停用-恢复操作；受影响性判断见「证据复用条件」的 impact 范围 |
| V22 | 医院管理员查询越权院区 | `Passed` | **按层级记录**（`Application`/`Query`，真实数据库列"否"）。`Stage3QueryTests.Branch_entrypoint_rejects_a_branch_of_another_hospital_without_degrading_to_an_empty_collection` 断言请求它医院院区抛 `InvalidOperationException` 且消息含 `院区`，并断言 `repository.CallCount == 0`、`repository.ReceivedQuery == null`（拒绝而不是降级为空集合）。 | 未覆盖面：本条按矩阵层级 `Application`/`Query` 记录，「真实数据库」列为「否」，规定验证面即请求院区不属于可信医院时拒绝且不降级为空集合，已由上述断言完整覆盖，属本层完整证据。**真实宿主层的越权院区查询面不属本条规定验证面**，由 C10/C11 承载（院区下拉只列可信医院院区，键入非可信院区后选项集为空且不提交） |
| V23 | DDL 列序、类型、注释、唯一索引 | `Passed` | dbx 只读复核：`information_schema.columns` 返回 8 列且列序为 `id uuid` / `organization_code text` / `hospital_code text` / `branch_code text` / `standard_project_code text` / `current_amount numeric` / `oper_time timestamp with time zone` / `oper_id uuid`，`is_nullable` 全为 `NO`、默认值计数 `0`、无 `varchar(n)`；每列中文注释逐列命中（`主键ID`、`组织编码`、`医院编码`、`院区编码`、`标准项目编码`、`当前金额`、`操作时间`、`操作人`）；表注释为 `组织医院院区互认项目金额`；`pg_indexes` 返回 `ux_mrec_org_hos_brh_project` = `UNIQUE (organization_code, hospital_code, branch_code, standard_project_code)`（**无状态过滤**）与主键索引，索引注释为 `同一组织医院院区标准项目金额唯一` | — |
| V24 | SqlMap scope 与物理表映射 | `Passed` | 源码静态复核（可引用既有结论，本轮未重跑）：`OrganizationHospitalBranchRecognitionAmount.xml` 的 `SqlMap Scope="OrganizationHospitalBranchRecognitionAmount"`，语句 Id 为 `OrganizationHospitalBranchRecognitionAmountColumns`、`GetOrganizationHospitalBranchRecognitionAmountByBusinessKey`、`InsertOrganizationHospitalBranchRecognitionAmount`、`UpdateOrganizationHospitalBranchRecognitionAmount`，全部只访问 `mrec_` 前缀表、无 `ON CONFLICT`/`LIMIT`/`OFFSET`/`NULLS FIRST`/标识符引号等方言特征；`MedicalRecognitionReportQuery.xml` 的金额列表语句同样只访问 `mrec_` 表并以 `mrec_mutual_recognition_item` 为行集、LEFT JOIN 金额表。语句键与调用点 `sqlId` 的一致性由 `Stage3SqlMapTests.Amount_sql_map_freezes_scope_statement_ids_and_removes_unused_query` 与 `..._targets_platform_tables_only_and_stays_provider_neutral` 覆盖。 | 运行时注册表内容未在本轮单独读取 |
| V25 | 事件字段与失败不登记 | `Passed` | **按层级记录**（`Domain`，真实数据库列"否"）。`Stage3WritePathTests.Saved_event_fields_come_from_command_and_failures_register_no_event` 断言事件四个业务键与金额取自命令、`EventCreator == TrustedOperId`、`EventCreatedTime == CommandOperTime`、`EventType`、`AggregateId == MedicalRecognitionReportConst.AggregateId`、事件标识等于落库记录主键；四类失败路径各自以 `Assert.Empty(events.Events)`/`AssertNothingWritten` 断言不登记事件（校验失败见 V4/V5/V6 用例，唯一冲突见 V16 应用层用例，0 行与多于 1 行见 `Save_platform_amount_is_rejected_when_zero_rows_affected_and_record_is_gone`、`..._rejects_when_update_affects_more_than_one_row`）。 | — |
| V26 | Request/ReadModel 契约 | `Passed` | **按层级记录**（`Contract`，真实数据库列"否"）。`Stage3WritePathTests.Save_requests_and_read_model_expose_only_the_designed_members` 断言两个保存请求的业务字段集合分别为 `["BranchCode","CurrentAmount","StandardProjectCode"]` 与 `["BranchCode","CurrentAmount","HospitalCode","OrganizationCode","StandardProjectCode"]`、读模型恰好 14 项且 `CurrentAmount` 为 `decimal?`、`ItemTypeText` 声明为可空引用、`ConfigurationStatusText` 声明为非空引用、`UnavailableReason` 声明为可空引用、`StandardProjectCode` 声明为非空引用、命名空间为 `...Application.Contracts.Queries`；`Stage3QueryTests.Amount_query_requests_expose_only_the_designed_members` 断言两个查询请求的字段集合并从类型侧排除 `OperId`/`OperTime`/`Id`/`IsValid`/`ConfigurationStatus`/`IsAuthorized`。**重开轮（2026-10-08）**：随 S3-D26（阶段根 [design.md](design.md) 高风险决策台账），该用例对两个查询请求的字段集合期望各增加可选 `Keyword`（平台入口 5 项：`BranchCode`/`HospitalCode`/`Keyword`/`OrganizationCode`/`StandardProjectCode`；医院入口 3 项：`BranchCode`/`Keyword`/`StandardProjectCode`），其余断言不变；改后 `dotnet test server/Dy.MedicalRecognition.slnx` 全量失败 0、通过 829。 | — |
| V27 | 无 WorkUnit 生产入口的写入与失败响应 | `Passed` | ① **成功面**：经宿主页面（无 WorkUnit 的真实生产入口）在 `CSYQ2-2/123` 与 `0101001/222` 两次新增写入，`SaveBranchRecognitionAmount` 均返回 `200 true` 且 dbx 回读一致（见 V1）；另在 `CSYQ2-1/123` 取得同值重复保存成功与 `oper_time` 更新（见 V2/V7）。② **失败响应面**：删除互认配置后经同一入口提交未配置行，返回 `业务拒绝：当前组织未建立该标准项目的互认项目配置。`，页面保留弹窗与输入、不刷新列表、不误报成功（见 C10）。③ **无脏写面**：失败后只读回读该业务键 0 行、金额表总行数不变（见「补充取证」节） | 事件可见性子面按设计记 `N/A` + `AcceptedRisk`，该子面不使本条结案；本条三个面均已取得 |
| V28 | 实体语义与注释修正 | `Passed` | **按层级记录**（`Static`/`Domain`，真实数据库列"否"）。`Stage3WritePathTests.Amount_entity_semantics_describe_current_amount_maintenance_without_accumulation` 断言实体源码不含 `累计`/`累加`，含 `维护的互认项目当前金额`、`保存时由请求提交值覆盖`、`取命令携带的操作时间`；并对 `MedicalRecognitionReportManager.cs` 与 `OrganizationHospitalBranchRecognitionAmount.cs` 断言不存在 `+=`/`-=` 复合赋值、不存在以 `CurrentAmount` 为操作数的算术表达式。 | 未覆盖面：本条未覆盖"实体注释与 UML1 实体及 SRS F10 逐句一致"，只覆盖语义修正与无累加；该一致性属静态人工核对，本轮未做逐句比对 |
| V29 | 平台管理员入口跨组织保存与查询 | `Blocked` | 按 **S3-D20** 登记：环境无第二个组织（本轮只读回读 `QueryAllOrganization` 响应仅 `01/县医共体`），跨组织真实宿主面不可构造；取值口径不因此改变。本轮追加真实宿主证据：平台管理员页范围三级均可选、级联正确（`县医共体` → 测试医院多选 → `测试院区1-1`/`1-2`/`2-1`/`2-2` 均可选）；查询请求体 `{"branchCode":"CSYQ1-1","hospitalCode":"CSYY1","organizationCode":"01"}` 与切换后 `{"branchCode":"CSYQ1-2",...}`、`{"branchCode":"CSYQ2-2",...}`、`{"branchCode":"CSYQ2-1",...}` 均携带三值并返回 `200`；零元行 `222` 显示 `0.00`/`已配置`、未配置行显示 `未配置`，均未回退；本条按矩阵口径以应用层证据为准，应用层由 `Stage3QueryTests.Platform_entrypoint_queries_the_requested_organization_even_when_it_differs_from_the_trusted_context` 覆盖（请求组织与可信上下文组织不同时仍按请求组织查询） | 已接受的环境边界，不作为阻塞项 |
| V30 | 医院管理员入口的院区范围 | `Blocked` | **允许面本轮取得完整真实证据**：① `yangkj`（可信范围 `01/0101`）默认院区 `0101001`（总院），`POST /Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList` 体 `{"branchCode":"0101001"}` 返回 **200** 与 5 行（`hospitalName=县人民医院`、`branchName=总院`）；保存端点体 `{"branchCode":"0101001","currentAmount":37.37,"standardProjectCode":"222"}` 返回 **200 true**；dbx 回读 `organization_code=01`、`hospital_code=0101`、`branch_code=0101001`，与可信范围逐字一致且三列长度分别为 2/4/7（无首尾空白）。② `lisadmin`（可信范围 `01/CSYY2`）默认院区 `CSYQ2-1`，查询体 `{"branchCode":"CSYQ2-1"}` 与保存体 `{"branchCode":"CSYQ2-1","currentAmount":66.66,"standardProjectCode":"123"}` 分别返回 200 与 200 true，dbx 回读 `01/CSYY2/CSYQ2-1`。应用层同一拒绝由 `Stage3OrganizationPathTests.Resolve_rejects_when_the_requested_branch_belongs_to_another_hospital` 覆盖，属另一层证据、不替代本条真实库面。 | **"它医院院区拒绝"面未取得**：医院页院区下拉只列可信医院下的院区（`yangkj` 实测选项集 `{总院}`；`lisadmin` 实测选项集 `{测试院区2-2, 测试院区2-1}`）。本轮已尝试的手段：打开下拉读取真实选项集；在下拉内键入 `CSYQ2-1` 后选项集为空（`暂无数据`）、文本不成为选中值、已选值仍为 `总院`、按回车不提交、列表仍为 `总院` 的 5 行且 Network 无新请求。未使用接口直连或改请求绕过。按"只完成部分边界不记通过"，本条的拒绝面记 `Blocked`，整条保持 `Blocked`（允许面证据已完整，阻断原因由上一轮的"入口整体 500"收窄为"它医院院区提交路径不可达"） |
| V31 | 关键字四列匹配 | `Passed` | **非真实库层级已取证（2026-10-08 重开轮，见「重开轮：两金额页统一（票 10 后端第一棒）」节）**：① 语句级判据 `Stage3QueryTests.Amount_query_statement_filters_four_columns_by_keyword_with_literal_escaping` 读取 `Repository/Queries/MedicalRecognitionReportQuery.xml` 的 `QueryRecognitionAmountList`，用 `Assert.Single` 取唯一 `Property="Keyword"` 过滤片段（片段缺失或被复制成两段都判失败），断言 `Prepend="and"`，并逐段断言四列 `lower(m.standard_project_code)`、`lower(i.name)`、`lower(c.name)`、`lower(g.name)` 各配同一段转义模式 `like concat('%', lower(replace(replace(replace($Keyword,'!','!!'),'%','!%'),'_','!_')),'%') escape '!'` 与三个 `or`；变异证据 3 项（去掉 `escape '!'`、把 `lower(g.name)` 放开为 `g.name`、把 ` or ` 改为 ` and `）在同一条判据下全部转为失败。② 绑定判据 `Amount_query_repository_binds_the_keyword_parameter` 按语法节点断言仓储方法把 `Keyword = keyword` 绑进查询参数对象，变异证据 2 项（删除绑定、改绑 `standardProjectCode`）判失败。③ 目标验证 `dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Stage3QueryTests"` → 失败 0、通过 37。**真实库与真实入口面（2026-10-09 环境恢复后复测取得）**：受管 Chrome `154.0.8037.98`（CDP CLI 会话 `dsh-stage3-amount`）经宿主登录页以 `yangkj` 登录，在宿主页面上下文携带宿主 Bearer 令牌（取自 `dy-auth:token-info`，令牌原文未出现在任何输出中）请求本地后端 `http://localhost:15014` 的两个真实入口，后端为 `dotnet run`（Development）进程。数据状态：组织 `01` 当前 **301** 条互认配置（3 个分类、26 个分组、已配置金额 0 行），与收口轮的 3 条配置不同，属后续阶段数据。结果：编码精确命中 `002101020150000-01001` → **1** 行；名称命中 `鼻骨` → **2** 行且每行四列至少一列含该字面；分类名命中 `放射影像专业` → **145** 行（大于 0、小于 301、全部属该分类）；分组名命中 `泌尿系超声` → **6** 行，全部 `groupName` 等于该组且至少一行的编码与名称都不含该字面（证明命中来自分组列）；不命中 `zzz-不存在-9527` → **0** 行且返回 200；不区分大小写 `dr` 与 `DR` → 各 **59** 行且相等；`%` → **0** 行、`_` → **0** 行（按通配符解释会返回 301，故按普通字符比较成立）；平台入口 `QueryRecognitionAmountList` 携带三值与同一关键字 → **1** 行；复测末尾再次缺省读取仍 **301** 行（零写入）。该轮曾因外部宿主 35000、35001、15432 连续不通记 `Blocked`（末次探测 2026-10-08 23:10 +08:00），环境恢复后按原口径复测转正 | 无未覆盖面：五类命中（编码、名称、分类名、分组名、不命中）与不区分大小写、`%`、`_` 字面比较均在真实库与真实入口上取得，两个入口都已覆盖 |
| V32 | 关键字缺失或空白 | `Passed` | **非真实库层级已取证（同一轮）**：① 请求校验面 `Stage3QueryTests.Amount_query_requests_accept_blank_keyword` 断言两个金额查询 Request 在 `Keyword` 为 `null` 与 `"   "` 时 `MedicalRecognitionRequestValidator.Validate` 都不抛出，即缺失与空白按未提供处理、请求层不拒绝。② 应用面 `Amount_query_entrypoints_normalize_keyword_before_reaching_the_query_port`（6 组数据 × 两个入口）断言查询端口收到的关键字：`null`、`""`、`"   "`、`"\t\n"` 都归一为 `null`，`"血"` 原样传递，`"  血  "` 归一为 `"血"`；实现改前该用例有 4 组判失败（失败断言见「重开轮」节）。③ 语句侧由 `IsNotEmpty Property="Keyword"` 与 V31 的语句级判据共同约束：空值不拼接过滤片段。**真实库与真实入口面（2026-10-09 环境恢复后复测取得）**：取证环境与 V31 同一次会话（受管 Chrome 宿主登录后在页面上下文携带宿主 Bearer 令牌请求 `http://localhost:15014`，后端 `dotnet run` Development 进程）。医院管理员入口 `QueryBranchRecognitionAmountList`（体 `{"branchCode":"0101001"}`）三种请求各返回 **301** 行且状态 200：缺省 `keyword`、`keyword:"   "`、`keyword:"""`（空字符串）；三者行数与首次缺省读取完全一致，即空白与缺失都按未提供处理、不附加过滤，返回该范围全部行；复测末尾再次缺省读取仍 **301** 行（零写入）。该轮阻断与转正过程同 V31 | 无未覆盖面：空白与缺省两种请求在真实库与真实入口上均返回全部行，且未产生任何写入 |
| V33 | 关键字入参 | `Passed` | **按层级记录**（`Contract`，真实数据库列「否」）；该条即 `Server/design.md` 验证矩阵第 202 行的「关键字入参」用例。① `Stage3QueryTests.Amount_query_requests_declare_optional_keyword_and_keep_standard_project_code`：两个 Request 各自声明的 `Keyword` 存在、类型 `string`、可空标注为 `Nullable`、不带 `RequiredAttribute`，`StandardProjectCode` 仍为可空 `string` 且保留。② 同文件 `Amount_query_requests_expose_only_the_designed_members`：两个请求的字段集合含 `Keyword`（平台入口 5 项、医院入口 3 项），并继续排除操作人、操作时间、内部标识与授权结论字段。③ 本地后端运行时实测 `GET http://localhost:15014/openapi/v1.json`：两个请求 schema 各多出 `"keyword": {"type": ["null","string"]}`，`required` 集合不变（平台入口 `organizationCode`/`hospitalCode`/`branchCode`，医院入口 `branchCode`），`paths` 集合 57 条与冻结快照 `client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json` 的 57 条逐条相同，两个请求 schema 除新增 `keyword` 外与冻结快照逐字一致。 | 未覆盖面：生成端（Kiota）形状由票 10 的 API Client 批次按 C24 核对；本条只覆盖服务端请求定义与 OpenAPI 输出 |
| C1 | 平台管理员页范围选择 | `Passed` | 组件面：`RecognitionAmounts.test.tsx` 的 `C1：依次切换组织、医院、院区都按新范围重载，且不自动发起写请求`。**重开轮宿主复测（2026-10-09 第二轮）已取代改版前证据**：平台页医院 `测试医院二` → `测试医院一` → 院区自动重取 `测试院区1-1`（可信院区不属于该院，按 S3-D22 第二档取该院第一项）、页头两行同步、`QueryRecognitionAmountList` 体三值 → 200、切换与读取期间零写；医院页只有一个院区选择器（组织、医院不渲染）、选项清单仅含可信医院的两个院区、切到 `测试院区2-2` → `QueryBranchRecognitionAmountList` 体 `{branchCode:"CSYQ2-2"}` → 200 且零写；证据见「重开轮宿主面复测 · 第二轮」节。上文（改版前按十列页面取得）按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节判为失效，仅作留档 | 未覆盖面：「可信院区缺失」子场景按既有环境边界不可构造（见 C3 行） |
| C2 | 平台管理员页请求内容 | `Passed` | 查询请求体实测 `{"branchCode":"CSYQ1-1","hospitalCode":"CSYY1","organizationCode":"01"}`、`{"branchCode":"CSYQ1-2","hospitalCode":"CSYY1","organizationCode":"01"}`、`{"branchCode":"CSYQ2-2","hospitalCode":"CSYY2","organizationCode":"01"}`、`{"branchCode":"CSYQ2-1","hospitalCode":"CSYY2","organizationCode":"01"}` —— 均携带组织、医院、院区三值。保存请求体三值见上一轮实测与组件层 `recognitionAmountsApi.test.ts` 的 `C2 平台管理员页保存下发组织、医院、院区、标准项目编码与金额，不提交已变更属性集`。 | 未覆盖面：本轮未在平台页执行保存，平台页保存请求体的三值取自组件层断言与上一轮真实实测 |
| C3 | 医院管理员页范围选择 | `Passed` | ① 页面标题「本院区互认项目金额」、页头两行（`yangkj` 名称行 `01/县人民医院/总院`、编码行 `01/0101/0101001`），只读副标题为可信范围（`yangkj` `组织 01，医院 0101`、`lisadmin` `组织 01，医院 CSYY2`），组织与医院不渲染下拉、该页**只渲染 1 个院区下拉**；② **院区默认取可信院区**（`lisadmin` 默认 `测试院区2-1` 而选项集为 `{测试院区2-2, 测试院区2-1}`，默认值不是首项）；③ **可信院区缺失子场景（第四轮补证）**：`branchID` 置空整页重载后页头**不变**（组织、医院仍固定展示且不渲染下拉）、院区下拉选中该院清单**第一项** `总院`、页脚 `共 301 项`、表格 302 行、金额入口存在、无待切换提示——即「默认选中该院首项并自动加载」，构造与恢复见「重开轮宿主面复测 · 第四轮」节；④ 院区不可清空（用户清空后占位 `请选择院区`、表格置空、无写入口）。组件面：`BranchRecognitionAmounts.test.tsx` 的 `默认档二：可信院区缺失时取该院下第一项` 等档二用例 | 未覆盖面：宿主**签发**的身份不可伪造（服务端面，登记不变，见「未覆盖面与残余风险」①的两层区分）；用户清空院区是另一交互，不替代可信缺失子场景 |
| C4 | 医院管理员页请求内容 | `Passed` | 真实请求体逐条读取（Network）：① 查询 `POST /Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList` 体 `{"branchCode":"0101001"}`（`yangkj`）、`{"branchCode":"CSYQ2-1"}` 与切换后 `{"branchCode":"CSYQ2-2"}`（`lisadmin`）；② 保存 `POST /Api/MedicalRecognitionReport/SaveBranchRecognitionAmount` 体 `{"branchCode":"0101001","currentAmount":37.37,"standardProjectCode":"222"}`（`yangkj`）与 `{"branchCode":"CSYQ2-2","currentAmount":55.55,"standardProjectCode":"123"}`（`lisadmin`）。两个方向的请求体都**只含院区与标准项目编码**（保存另有本次金额），**不含 `organizationCode`、不含 `hospitalCode`**，与设计一致 | — |
| C5 | 列表字段集合 | `Passed` | **重开轮宿主复测（2026-10-09，页面改版后）**：两页表头实测 **8 列** `类型/分类/分组/项目/当前金额/金额状态/互认配置状态/当前不可用原因`——不再有组织、医院、院区名称三列，也没有它们的编码列、操作列、最后修改时间与最后修改人列；页头三级范围两行（名称行在上、编码行在下：平台页 `县医共体/测试医院一/测试院区1-1` + `01/CSYY1/CSYQ1-1`，医院页 `01/县人民医院/总院` + `01/0101/0101001`）。配置状态中文取服务端 `configurationStatusText`（S3-D21，服务端枚举声明为中文唯一来源），`itemTypeText` 不在展示面内、页面不得自建中文映射。组件面两页各一条 8 列 `toEqual` 断言并逐一断言 12 个不该出现的表头缺失（含「操作」）。收口轮按十列表头（含三列名称）取得的 DOM 证据作于改版之前，按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节判为失效 | 未覆盖面：页头组织「名称」在固定级只能取编码的固有限制见「重开轮宿主面复测」节与 C31 行 |
| C6 | 未配置呈现 | `Passed` | **重开轮**：`yangkj` 在 `0101001` 的 3 行中 `S1-D29-ITEM-20260915-001` 行为该呈现——「当前金额」列为 `未配置` 标记、「金额状态」为 `未配置`，响应中该行 `currentAmount:null`、`isAmountConfigured:false`。既往轮次：`lisadmin` 在 `CSYQ2-2` 的首查响应 5 行中 4 行为该呈现；`yangkj` 在 `0101001` 的 5 行中 4 行为该呈现。 | — |
| C7 | 零元与未配置区分 | `Passed` | 同屏两行实测：`CSYY1/CSYQ1-1` 范围下 `222` 行「当前金额」为 `<span class="recognition-amounts-number">0.00</span>`、「金额状态」为绿色 `已配置`；未配置行为 `未配置` 标签。两者的 DOM 结构与标签颜色均不同。**重开轮**：本条判定的是金额列呈现，不在 S3-D21（新增响应枚举中文字段）的影响范围内，按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节复用既往同轮证据；复用条件为 impact 不重叠、版本为修复后工作区、环境为同一宿主链路、复用理由为变更只增加响应字段且不改金额列判定与呈现。 | — |
| C8 | 金额输入校验 | `Passed` | **重开轮宿主复测（2026-10-09，行内编辑替代弹窗）**：两页各在行内编辑器输入 `-5` 后回车 → 行内提示 `金额必须是不小于 0 且最多两位小数的数字`、`aria-invalid="true"`、编辑态与输入保留，页面侧 fetch 记录器内 **Save 请求数为 0**；平台页在同一次编辑里把值改为 `99.99` 后 `aria-invalid` 复位。组件面另有 `1.234`、空值（`金额必填`）与双精度不可精确表示大额（`99999999999999.99`）三组断言，三次均零写请求且保留编辑态与输入。收口轮的弹窗内证据作于改版之前，按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节判为失效 | — |
| C9 | 金额保存成功 | `Passed` | **重开轮宿主复测（2026-10-09）**：医院页行内提交 `12.34` → `SaveBranchRecognitionAmount` 返回 200 → 编辑态收起 → 立即 `QueryBranchRecognitionAmountList` 整表重载，首格由「未配置」变为 `12.34`；**平台页跨院区（`01/CSYY1/CSYQ1-1`）行内提交 `88.88` → `SaveOrganizationHospitalBranchRecognitionAmount` 三值体 → 200 → 收起 → 整表重载 → 首格 `88.88`**（第二轮，见 C34 场景二）。组件面两页各一条 C9 用例（保存 200 → 收起编辑态 → 整表重载并显示新值） | — |
| C10 | 金额保存失败 | `Passed` | **第四轮宿主复测（受控构造，2026-10-09）**：一次性改写保存请求体的 `standardProjectCode` 为目录中未建立互认配置的真实编码 `002504030050000-01001`（构造理由与恢复步骤见「受控失败注入记录」的 C10 行）→ 保存响应 `status 500 / ok:false`；宿主父文档出现瞬态提示 **「业务拒绝：当前组织未建立该标准项目的互认项目配置。」**；**编辑态保持打开、输入 `88.88` 保留**；该请求之后**无任何列表请求**（不刷新列表）；**无成功提示**（不误报成功）；被改写的原项目零写入（恢复后金额仍为 `77.77`）。组件面以服务端拒绝替身覆盖同一批页面呈现 | 未覆盖面：无（收口轮经授权删除配置取得的同型证据由本轮受控构造取代；`C11` 的第二类前置仍按其行登记） |
| C11 | 保存被拒绝的两类前置 | `Blocked` | ① **已取得**：当前组织无该标准项目互认配置 → 删除组织 `01` 全部互认配置后，经宿主医院管理员入口提交未配置行，收到业务拒绝 `业务拒绝：当前组织未建立该标准项目的互认项目配置。`；页面呈现与 C10 一致（弹窗与输入保留、不刷新列表、不误报成功）；零脏写（只读回读该业务键 0 行、金额表总行数仍为 11 行）。② **未取得**：请求院区不属于可信医院 → 医院页院区下拉只列可信医院下的院区（`yangkj` 实测选项集 `{总院}`，`lisadmin` 实测选项集 `{测试院区2-2, 测试院区2-1}`），在 `yangkj` 会话内向该下拉键入 `CSYQ2-1` 后选项集为空（`暂无数据`）、已选值不变、按回车不提交、列表不刷新且无新请求。相关旁证：dbx 全表回读 11 行，非可信医院院区上不存在由另一身份写入的行（`0101001` 两行 `oper_id` 为 `yangkj`，`CSYQ2-1`/`CSYQ2-2` 各一行 `oper_id` 为 `lisadmin`） | 本条要求两类前置**均**收到业务拒绝，第二类前置在本环境的正常页面路径不可构造，且授权范围不允许接口直连或改写入参，故整条不记通过；阻断原因由上一轮的"两类前置均不可达"收窄为"仅第二类前置不可达"，第一类前置证据已完整 |
| C12 | 同值重复保存 | `Passed` | **重开轮宿主复测（2026-10-09）**：医院页行内回填当前值 `0.00` 不改直接回车 → 仍发出 `SaveBranchRecognitionAmount`（体 `{"branchCode":"0101001","currentAmount":0,"standardProjectCode":"002501020020000-01001"}`）→ 200 → 编辑态收起，行值仍为 `0.00`、金额状态仍 `已配置`（无「同值短路」）。组件面 `C9 / C12` 用例断言回填不改仍发写请求、成功后收起并整表重载。收口轮的弹窗证据作于改版之前，判为失效 | — |
| C13 | 写成功但刷新失败 | `Passed` | **第四轮宿主复测（受控注入，2026-10-09，步骤见「受控失败注入记录」C13 行）**：医院页 `failList=1` 后行内提交 `66.66` → 记录序列显示**保存请求未被拦截且 `status 200`**、随后的 `QueryBranchRecognitionAmountList` **被拦截失败**；出现警示标题「写入已成功，数据待刷新」与说明「写入已经成功，但重新读取金额列表失败；已有数据保留，维护操作暂不可用。」+「重 试」；**无任何保存失败提示**（`.ant-message`/notification 为空）；原 302 行与页脚 `共 301 项` 保留、编辑态收起；**页面内重试恢复（收口轮缺口已关闭）**：清注入 → 点「重试」→ 第二次列表请求 200、警示消失、写入口恢复、首格显示 **`66.66`**（写入结果未丢失） | 收口轮证据（含其「重试恢复未取得」的缺口登记）按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节由本轮取代 |
| C14 | 读取失败 | `Passed` | **第四轮宿主复测（受控注入，2026-10-09，步骤见「受控失败注入记录」C14 行）**：`failList=1` 后点页内「搜索」触发读取 → 列表请求被拦截失败；页面保留原 302 行与页脚 `共 301 项`，出现警示标题「互认项目金额数据待刷新」与说明「读取失败，已有数据保留；维护操作暂不可用，可重试读取。」+「重 试」；**全部金额入口 `disabled=true`**；表头与行数据未被清空；**页面内重试恢复（收口轮缺口已关闭）**：清注入 → 点「重试」→ 第二次列表请求 200、警示消失、写入口 `disabled=false` | 组件层 `RecognitionAmounts.test.tsx` 的 C14 用例同向覆盖；收口轮证据由本轮取代 |
| C15 | 组织不可用 | `Passed` | **组件面**：`BranchRecognitionAmounts.test.tsx` 的 `C15 可信组织或医院缺失` → 阻断提示、不渲染院区下拉、零业务请求、不伪造组织或降级为空值。**宿主面（第四轮受控构造，2026-10-09）**：`hosID` 置空（`branchID` 同为空）后整页重载 → 阻断提示 **「可信范围不可用」+「当前无法从登录凭证确定可信组织与医院，因此不能读取或维护本院区互认项目金额。请从宿主登录后重新进入本页面。」**、**不渲染院区下拉**（`.ant-select` 计数 0）、页头无范围展示、无金额入口、表格 0 行、**全程零业务请求**（iframe `performance` 中 `15014`/`35001` 资源条目 **0 条**）；写回 `0101`/`0101001` 并整页重载后复核两页正常、无阻断提示 | 构造与恢复见「重开轮宿主面复测 · 第四轮」节；宿主**签发**的无组织身份仍不可伪造（服务端面，登记不变），本条补的是页面侧可信上下文缺失形态（两层区分见「未覆盖面与残余风险」①） |
| C16 | 排序 | `Passed` | 平台页 5 行按标准项目编码升序呈现（`123` < `222` < `S1-D29-ITEM-20260915-001` < `S1-RETRY-ITEM-20260914-001` < `S1-RETRY-ITEM-20260914-002`）；医院管理员入口两个身份的 5 行响应与 DOM 同样为该升序。 | — |
| C17 | 空态 | `Passed` | 删除组织 `01` 全部互认配置后进入医院管理员入口：列表请求 `reqid=104` 返回 **200** 且响应体 **`[]`**，页面表格清空并显示空态 `暂无数据` 与文案 `当前范围没有互认项目金额配置`。组件层 `RecognitionAmounts.test.tsx` 的 `C17 空态` 用例以替身构造同一状态并断言空态文案与只读取一次，同向覆盖 | — |
| C18 | 当前不可用原因 | `Passed` | **第四轮宿主复测（逐层「停用 → 金额页重读 → 恢复」各一次，全部经「标准项目目录维护」页的业务操作与确认弹窗，2026-10-09）**：仅标准项目停用（`停用项目：…` → 弹窗「停用标准项目」→ 确认）→ 该行原因 **`标准项目已停用`**；仅分组停用（`分组更多操作` → `停用分组` → 弹窗「停用分组」→ 确认）→ **`所属分组已停用`**；仅分类停用（`分类更多操作` → `停用分类` → 弹窗「停用分类」→ 确认）→ **`所属分类已停用`**；三层全部恢复后重读 → 该行原因**为空**，且全表含「已停用」原因的行数为 **0**（分类行文本复核「检验专业启用」、分组与项目按钮复核为启用态） | 未覆盖面：无（收口轮的三层证据与本轮只读复核均由本轮逐层实测取代；操作与恢复细节见第四轮节） |
| C19 | 配置自身停用 | `Passed` | **第四轮宿主复测（2026-10-09）**：经「互认项目」页对 `002504030040000-01001` 点 `停用配置：…（当前启用）` → 弹窗「停用互认项目配置」→ 确认停用（按钮变为 `启用配置：…（当前停用）`）→ 金额页重读该行：金额 `66.66`、金额状态 `已配置`、**互认配置状态 `停用`**、**当前不可用原因为空**（8 列末列空串）；取证后经 `启用配置` → 弹窗「启用互认项目配置」→ 确认启用恢复。既往轮次经 `222`（响应体 `configurationStatus:2` / `configurationStatusText:"停用"` / `unavailableReason:null`）与 `S1-D29-ITEM-20260915-001` 的同型证据 | 未覆盖面：无（页面改版后的呈现已由本轮补齐） |
| C20 | 停用状态下仍可维护金额 | `Passed` | **第四轮宿主复测（2026-10-09）**：在 C19 的配置停用态下行内入口**未被禁用**（`disabled=false`），提交 `77.77` → `SaveBranchRecognitionAmount` 体 `{branchCode:"0101001", currentAmount:77.77, standardProjectCode:"002504030040000-01001"}` → **200** → 编辑态收起并整表重载；该行变为金额 `77.77`、金额状态 `已配置`、**互认配置状态仍 `停用`**、**原因列仍为空**（保存不改变状态与原因列）。既往轮次在配置停用与分类停用两种状态下均有同型证据 | 未覆盖面：无（同上） |
| C21 | 范围切换与未保存输入 | `Passed` | **按层级记录**（前端矩阵层级为 `Component`）。`RecognitionAmounts.test.tsx` 的 `C21 范围切换与未保存输入` → `切换院区时未保存的金额输入与旧范围数据一并清空，不带旧输入提交`；`BranchRecognitionAmounts.test.tsx` 的同名用例独立覆盖医院管理员入口。本轮实际重跑：`pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionAmounts` → Test Files 3 passed、Tests 56 passed，其中该用例通过。宿主侧旁证：范围切换在渲染期复位并清空行数据（`lisadmin` 由 `CSYQ2-1` 切到 `CSYQ2-2` 后表格不再出现旧范围行）。 | 未覆盖面：宿主层未构造"弹窗已打开且输入未提交时切换范围"的交互序列，只覆盖了范围切换后旧数据清空 |
| C22 | 范围真实数据量与不分页 | `Passed` | **重开轮宿主复测（2026-10-09）**：前提已满足——组织 `01` 现有 **301** 条互认配置（3 个分类、26 个分组），两页在该规模下进入与渲染、三次关键字筛选、两次保存均正常返回（全部 200）、无超时、无可见卡顿、Console error 级 0 条；表格仍无分页控件（DOM 无 `.ant-pagination`）。既往轮次：两身份数十次进入切换提交正常、`pagination={false}`，组件层 `C16 排序与不分页` 断言页面不分页 | 未覆盖面：内存只以「无异常、无超时、交互即时响应」观察，未做剖析；组件层不分页断言与宿主 DOM 观察并存 |
| C23 | 组件加载失败提示 | `NotRun` | 未取得 | 需要"组织服务选项请求失败"的受控注入。本轮受控注入只做 C13/C14（列表请求失败），未再尝试让组织服务选项请求失败；上一轮的文档级注入钩子在宿主重开子应用标签时未命中。本轮未新增生产测试入口或修改生产代码。组件层 `RecognitionAmounts.test.tsx` 的 `C23 组件自身加载失败提示` 用例以替身构造选项读取失败，属另一层证据 |
| C24 | 累计 API Client 契约 | `Passed` | **按层级记录**（前端矩阵层级为 `Component`）。① 端点净增与保留：后端 `Stage2EnumContractTests.Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes` 冻结前端生成物 `client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json` 的路径集合，字面量共 **27** 条 = 26 个业务路径 + 保留排除的 `/auth/login`（该数字为 2026-09-17 收口轮次的时点值；2026-10-08 重开轮实测为 **57** 条且无端点增减，见本行重开轮证据），其中含本阶段 4 个端点（`SaveBranchRecognitionAmount`、`SaveOrganizationHospitalBranchRecognitionAmount`、`QueryBranchRecognitionAmountList`、`QueryRecognitionAmountList`），并断言归一化全量生效（无 `"oneOf"`、无 `["integer","string"]` 残留）。② 枚举数值读写：`Generated_client_reads_and_writes_enum_properties_as_numbers` 断言生成端把枚举属性映射为 `number` 并用 `writeNumberValue` 写出；`Application_module_registers_the_enum_open_api_transformer` 断言模块注册。③ 端到端可用：本轮真实宿主两个页面都经该生成客户端发起查询与保存并取得 `200`（见 C2/C4），说明端点、模型与序列化在运行时可用的。**重开轮（2026-10-08，票 10 第二棒）**：失败基线 `Stage3ApiContractTests.Frozen_openapi_and_generated_model_declare_optional_keyword_for_amount_queries` 在再生成前判失败（断言消息「RecognitionAmountListQueryRequest 的属性集合缺少 keyword。」）；`pnpm -F @dy/api-client-medical-recognition prepare-openapi`（来源 `http://localhost:15014/openapi/v1.json`，修正整数联合 21 处、可空引用 26 处、path 57）→ `pnpm -F @dy/api-client-medical-recognition generate`（Kiota 1.34.1，固定 `--exclude-path /auth/login`，`Generation completed successfully`）→ 同一用例转为通过；后端全量 `dotnet test server/Dy.MedicalRecognition.slnx` 失败 0、通过 **833**（回改后复跑终值），其中路径集合等值断言仍为 57 条、无端点增减、`/auth/login` 仍被排除；保护面实测 `src/index.ts` SHA256 生成前后同为 `51A20E1E1EAFDB3EC456489DE33307E9864F9AFB3C47CE0791A56000E0AAE01C`、`kiota-lock.json` 的 `excludePatterns` 仍为 `["/auth/login"]` 且 `descriptionLocation` 仍指向包内 `openapi/`、`git status -- client/` 只有 4 个生成相关文件被修改且无新增或删除；包 `typecheck` 与 `build` exit 0，子应用 `test` 21 文件 428 通过 2 跳过 0 失败、`lint` 与 `build` exit 0。 | 未覆盖面（重开轮后）：`src/index.ts` 未被覆盖由生成前后 SHA256 等值直接证明；本条仍不覆盖"阶段 1 与阶段 2 端点在生成物中逐一保留"的正面清单核对，只由路径集合等值断言隐含；生成端把 `currentAmount` 声明为未定型节点的既有缺陷仍未修（见「未覆盖面与残余风险」） |
| C25 | 运行时配置 | `Passed` | 范围下拉与选项请求实测打到平台 Base 服务：`POST http://183.224.180.166:35001/Api/Organization/QueryAllOrganization`、`…/QueryAllValidHospitalByOrgId`、`…/QueryAllValidBranchByOrgId` 均返回 `200`，响应含 `{"id":"01","name":"县医共体"}`、`测试医院一`/`测试医院二`、`测试院区1-1`/`1-2`/`2-1`/`2-2`、`总院` 等真实选项。本轮两个身份下三类选项请求仍全部打到 `183.224.180.166:35001` 并返回 `200`；该轮取证时 `public/config.json` 尚未改动（`git diff` 为空，内容只有 `apiBaseUrl: http://localhost:15014`、无 `baseApiBaseUrl`），`public/config.development.json` 的 `baseApiBaseUrl` 为 `http://183.224.180.166:35001`；该缺口在轮次之后按环境规范闭合——`public/config.json` 现携带 `baseApiBaseUrl: http://183.224.180.166:35001`，其 `apiBaseUrl` 登记为部署占位地址 `http://183.224.180.166:35014`（尚未部署，本报告不核其可达性）。 | — |
| C26 | 宿主链路 | `Passed` | 两个身份 × 两个页面分别从宿主菜单进入：① 菜单项「检验检查结果互认」展开后含 `标准项目目录维护`、`互认项目`、`互认项目金额维护`、`本院区互认项目金额`（本轮两个身份下实测均为该 4 项）；② 两个页面均在新标签载入并完成首屏加载（医院页出 5 行列表，平台页出三级选择器与空态提示）；③ 拦截生效：`@vite/client`、`src/main.tsx`、`src/router/index.tsx`、`src/runtimeConfig.ts`、`@dy_components-base.js`、`BranchRecognitionAmounts.tsx`、`recognitionAmounts/*` 均来自 `http://localhost:3008`，Console 出现 `[vite] connected.`（debug 级）；④ **Console 无业务错误**：`yangkj` 的医院页与 `lisadmin` 的平台页分别调用 `list_console_messages`（`types:["error"]` 与 `types:["error","warn"]`）均返回 **no console messages found**；完整 Console 只有 debug 级 `[vite]` 消息与浏览器自带 issue 建议（表单字段 id、本地网络请求兼容性），无业务错误。全量 Network 中**没有任何 5xx 响应**。 | 上一轮的 4 条 error 级消息与 2 条 500 响应经核对为上一页面会话的保留消息；上一轮的瞬时 `Npgsql` 连接被远端关闭 500 本轮**未再现**（无法复现） |

| C27 | 关键字搜索 | `Passed` | **组件面**：两页各一条用例断言回车提交请求体带 `keyword`、返回行含关键字字面也照常全量展示（本地不裁剪）、清空后草稿与已生效关键字一并清空并触发一次不带 `keyword` 的读取。**宿主面（2026-10-09）**：医院页体 `{"branchCode":"0101001","keyword":"尿常规"}` → 200、页脚 `共 301 项`→`共 10 项`；点清空图标 → 体 `{"branchCode":"0101001"}` → 页脚回到 `共 301 项`；平台页体带三值 `{"branchCode":"CSYQ1-1","hospitalCode":"CSYY1","keyword":"血常规","organizationCode":"01"}` → 200、`共 8 项` | — |
| C28 | 状态筛选本地裁剪 | `Passed` | **组件面（层级 Component）**：两页各一条用例覆盖金额状态三档与互认配置状态三档、两条件取交集、切换只裁剪不发请求不下推；未知配置状态行在启用与停用档下都排除、全部档保留 | — |
| C29 | 地址栏参数同步 | `Passed` | **组件面**：进入按参数初始化、条件变化用替换历史项回写、输入草稿不写入、其它参数保留。**宿主面（2026-10-09）**：两个页面 iframe 地址实测 `…/branch-recognition-amounts?keyword=%E5%B0%BF%E5%B8%B8%E8%A7%84` 与 `…/recognition-amounts?keyword=%E8%A1%80%E5%B8%B8%E8%A7%84`，未提交草稿与未变更的状态档参数未写入 | — |
| C30 | 院区恒定单值规则（平台页三档默认） | `Passed` | **组件面（层级 Component）**：平台页 5 条（三档默认、切换后自动重取、清空拒绝）+ 医院页 3 条（可信院区优先、缺失取该院首项、可信院区不在回调清单时取首项）。**宿主面观察**：医院页初始院区为可信院区 `0101001` ✓；平台页初始范围为逐级取首项 `01/CSYY1/CSYQ1-1` 而非可信 `01/0101/0101001`，偏离见「重开轮宿主面复测」节发现 1 | 本条层级为 Component，组件面已完整；平台页初始范围偏离已单列为发现，待回改后按新口径复测 |
| C31 | 页头三级范围展示 | `Passed` | **组件面**：两页各 2 条（首次加载两行、切换后两行仍指同一范围且在途不渲染过期名称）。**宿主面（2026-10-09）**：平台页名称行 `县医共体/测试医院一/测试院区1-1` + 编码行 `01/CSYY1/CSYQ1-1`；医院页名称行 `01/县人民医院/总院` + 编码行 `01/0101/0101001`，两行右对齐、无文字标签 | — |
| C32 | 行内编辑金额 | `Passed` | **组件面**：两页各 6 条（点击值与「未配置」标记进入、回车、Esc、同值提交、按金额值重挂、行离开行集不渲染编辑态）。**宿主面（2026-10-09）**：两页各点击「未配置」进入 `aria-label="金额"` 编辑器并回填，回车提交合法值 → 保存 200 → 编辑态收起并整表重载，首格由「未配置」变为 `12.34`；`AmountEntryModal` 与 `RecognitionAmountModals.tsx` 已删除 | — |
| C33 | 可信范围变化确认框 | `Passed` | **组件面**：两页各 6 条（放弃并切换、取消进待切换提示态、提示态下内容消失自动切换、无未保存内容直接切换、组织医院换绑两条），触发为渲染期派生 + 选择器回调第二道，另补「只推进 token、不碰选择器」四条与挂起态写门控三条。**宿主面（2026-10-09 第二轮，受控构造）**：改写 `dy-auth:login-user.branchID`（`CSYQ2-1` → `CSYQ2-2`）后点选择器切医院 → 弹窗「可信范围已变化」+「可信范围已变化，切换范围将放弃未保存的金额输入。」+ 按钮「取 消」「放弃并切换」，期间保存请求数 0、编辑态与输入保留；点「取 消」→ 待切换提示态提示条「可信范围已变化，存在未保存的金额输入；确认后放弃输入并按新范围加载。」带「放弃并切换」，金额入口 `disabled=true`；随后写回原 `branchID` 并整页重载，提示与编辑态全部消失（构造与恢复步骤见「重开轮宿主面复测 · 第二轮」节） | 未覆盖面：真实 token 重签事件未直接构造（受控改写存储等价覆盖比对与呈现），宿主自动切换分支（无未保存内容）由组件层用例覆盖 |
| C34 | 提交前现值复核 | `Passed` | **组件面（三轮回改后，按 [Client/testPlan.md](Client/testPlan.md) 第 42 行三场景）**：比对两侧都是可信范围——「页面应用范围时记录的可信范围」对「提交发起时经命令式出口现读的可信范围」，院区层按缺失值参与比对（两侧同为缺失即放行，S3-D28 档二）、「现读不到」只指可信组织或医院读不到；平台页 4 例（场景一拦、场景二档二放行、场景三跨院区放行、缺失→B′ 拦），医院页 8 例（含渲染期未变而提交时现读已变 → 零写、档二放行、组织医院现读空 → 读不到），适配层复核出口 13 例。RED 累计五轮：平台入参形状 `2 failed / 48 passed（50）`、医院行内原因 `2 failed / 52 passed（54）`、医院提交期现读 `1 failed / 54 passed（55）`、档二口径三段 `3 failed / 66 passed（69）`＋`2 failed / 57 passed（59）`＋`1 failed / 51 passed（52）`（失败断言含「同为缺失被判读取不到」「档二 save 0 次调用」）。**宿主面（三场景全部取证，见第三轮节）**：场景一两页同步块改写可信院区后回车均零写 + 行内原因（输入保留、无旧口径句）；场景二档二（`branchID` 置空重载）两页都照常提交并 200（平台三值体、医院院区-only 体）；场景三平台页跨院区提交 44.44 → 200（医院页无此场景，按 testPlan 不构造） | 未覆盖面：真实 token 重签由受控改写存储等价覆盖；C34 拦首笔、C33 随后弹框的先后关系设计未规定，按「C34 拦首笔、C33 管挂起」实现并登记；构造与恢复步骤见第三轮节 |
| C35 | 页脚计数 | `Passed` | **组件面（层级 Component）**：两页各 2 条（有数据计数随筛选变化、空列表显示 `共 0 项`）。**宿主面（2026-10-09）**：页脚实测 `共 301 项` / `共 10 项` / `共 0 项`，随生效筛选变化 | — |
| C36 | 读取在途的写门控 | `Passed` | **组件面（层级 Component）**：两页各一条，读取在途期间搜索按钮 `loading`、金额格 `disabled`、点击不进入编辑态，结束后恢复；另挂起态（确认框与提示态）写门控三条断言回车零写与入口禁用 | — |

业务统计：`Passed` 64 条、`Failed` 0 条、`Blocked` 3 条、`NotRun` 2 条、`PendingRetest` 0 条、`N/A` 0 条，合计 69 条（64+0+3+2+0）；矩阵为 2026-10-08 重开轮版本（后端 V1-V33、前端 C1-C36）。状态流转：V31、V32 于 2026-10-09 环境恢复后由 `Blocked` 转 `Passed`；C22、C27-C32、C35、C36 由 `NotRun` 转 `Passed`（组件面与宿主面取证）；C33、C34 由 `NotRun` 经 `PendingRetest` 转 `Passed`（组件面 + 宿主面受控构造，C34 另经平台与医院两页三轮回改与档二裁定）；C1 由 `Passed` 转 `PendingRetest` 再按改版后宿主复测转回 `Passed`；**C10、C13、C14、C18、C19、C20 六条在第四轮按 testPlan 逐条宿主复测后由 `PendingRetest` 转 `Passed`**（C13、C14 用受控注入并补齐页面内重试恢复，C10 用受控改写构造服务端拒绝，C18-C20 用目录与互认配置页的停用—恢复往返）；**C3 由 `Blocked` 转 `Passed`**（可信院区缺失子场景按受控构造取证）、**C15 由 `NotRun` 转 `Passed`**（页面侧可信上下文缺失按受控构造取证）。`PendingRetest` 归零，剩余 `Blocked` 3 条（V29、V30、C11）与 `NotRun` 2 条（V16、C23）全部是已登记的环境边界。

- 逐条分布（2026-10-08 重开轮矩阵，含 2026-10-09 变化）：`Passed` = V1-V15、V17-V28、**V31、V32、V33**、**C1-C10**、**C12-C22**、C24-C36（共 **64** 条）；`Failed` = 无；`Blocked` = V29、V30、C11（共 **3** 条）；`NotRun` = V16、C23（共 **2** 条）；`PendingRetest` = 无（**0** 条）。加粗项为重开轮新增或状态变化项（C3、C10、C13-C15、C18-C20 为第四轮由 `Blocked`/`NotRun`/`PendingRetest` 转入）。收口轮次（2026-09-17）的分布为 `Passed` 48、`Blocked` 4、`NotRun` 4；重开轮按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节对受影响行做了状态流转，**其证据单元格的收口轮原文未被改写**，流转原因写在结果行与流转说明里。
- 本轮状态变更：**六项修复后的复验轮把上一轮 25 条 `NotRun` 中的 20 条**改为按各自层级记录并记 `Passed` —— V4、V5、V6、V8、V9、V10、V12、V13、V14、V18、V19、V22、V25、V26、V28、C21、C24（层级为 `Contract`/`Domain`/`Application`/`Query`/`Static`/`Component`，逐条给测试文件、用例名与关键断言，见「证据复用条件」），其中 `V6` 的转正依据是修复后的契约断言；**C15、C17 当时保持 `NotRun`**，只补记组件层证据与未覆盖面。**随后的「补充取证」轮**由负责人删除组织 `01` 的全部互认配置构造出无配置状态，使 **V11、V20、V27、C10、C17 五条取得完整真实证据并转正**（V11 拒绝且零写入、V20 空结果、V27 失败响应面与无脏写面、C10 四点呈现、C17 空态）。**V16 保持 `NotRun`**（真实并发同步屏障不可构造）。**V29、V30、C3、C11 四条 `Blocked` 的判定性不变**，本轮只更新证据与阻断原因：V30 的允许面证据已完整、阻断原因收窄为"它医院院区提交路径不可达"（上一轮的"入口整体 500"已消除），C3 的阻断原因是"可信院区缺失上下文不可构造"，C11 的阻断原因收窄为"仅第二类前置不可达"（第一类前置已取得完整证据），V29 按 S3-D20 维持。其余行状态不变，只补充或复核本轮证据。
- **重开轮（S3-D21 枚举中文）**：读模型由 12 字段增至 14 字段，前端不再本地映射枚举中文；受影响用例 V18、V26、C5、C6、C19 的证据在本轮更新，C7 按影响范围复用既往证据。**矩阵统计口径不变**（仍 56 条：`Passed` 48 / `Failed` 0 / `Blocked` 4 / `NotRun` 4）；详见「重开轮：读模型枚举中文（S3-D21）」节。
- 决策与责任标签（不计入上述统计，也不替代 `Passed`）：`AcceptedRisk` = S3-D20 覆盖的 V29 跨组织宿主面、V27 事件可见性子面、S3-D8 不分页、S3-D14 可空枚举筛选形态，以及阶段收口时接受的环境边界 V16、V30、C11、C23（逐条原因与已取得证据见「已确认的验证降级」节）。收口时接受的三条边界已于 2026-10-09 解除：C22 随组织 `01` 出现 301 条互认配置取得规模证据、C3 与 C15 随「页面侧可信上下文缺失」的受控构造取得宿主证据（两层区分见「未覆盖面与残余风险」①），三者均转 `Passed` 并不再作为已接受边界。
- 子面登记（不单独计入合计，避免与所属用例重复计数）：V27 的事件可见性子面记 `N/A` + `AcceptedRisk`；该子面不使 V27 结案，V27 已按整条记入 `Passed`。

## 受控失败注入记录

以下注入**只在浏览器运行时向子应用 iframe 的 `window` 上安装一次性钩子**，未落盘、未改仓库文件、未新增生产测试入口，也未修改生产代码。

共用注入形态（C13、C14）：在子应用 iframe 的 `window` 上保存 `fetch` 原引用，并以包装函数替换 `window.fetch`；包装函数按 URL 子串与方法匹配"只让目标请求失败"，命中时返回 `Promise.reject(new TypeError('mrec-injected-failure:<match>'))`，其余请求原样转发，同时记录每次请求的 URL、方法与是否被拦截；匹配规则带 `max` 次数上限。

| 用例 | 注入方式 | 观察结果 | 还原步骤 | 还原后核对 |
|---|---|---|---|---|
| C13 | 拦截 `QueryBranchRecognitionAmountList`（POST，`max=1`，不拦截保存端点），在 `CSYQ2-1` 行 `123` 弹窗提交同值 `66.66` | 记录序列为 `SaveBranchRecognitionAmount 未拦截` → `QueryBranchRecognitionAmountList 被拦截`；弹窗关闭；出现 `写入已成功，数据待刷新` 警示与 `重试`；页面无保存失败提示；dbx 回读 `CSYQ2-1/123 = 66.66`、`oper_time=2026-09-17T02:15:03.621658+08:00` | 移除匹配规则并删除运行时标记；因该次注入的原引用绑定错误（见下）改为**整页重载** iframe 恢复原生 `fetch` | 重载后 `window.fetch` 为原生实现（`[native code]`）、`__mrecOrigFetch`/`__mrecMatch`/`__mrecFetchLog` 标记全部不存在、警示消失、5 行重新加载、`维护金额` 全部 `disabled=false` |
| C14 | 拦截 `QueryBranchRecognitionAmountList`（POST，`max=1`），点击页面 `重新读取金额列表` | 请求被拦截 1 次；保留原 5 行（`123=55.55 已配置`，其余 4 行 `未配置`）；出现 `互认项目金额数据待刷新` 警示与 `重试`；全部 `维护金额` 按钮 `disabled=true`；表头未被清空 | 移除匹配规则；因原引用绑定错误改为**整页重载**恢复原生 `fetch` | 重载后原生 `fetch`、无残留标记、警示消失、写入口恢复可用 |
| 注入操作缺陷（本轮发现，非产品缺陷） | 第一次注入的还原把 `window.fetch` 赋回保存的原引用并删除标记，该次核对应已生效；但随后为 C13 重新注入时把 `__mrecOrigFetch` 绑定到"当时已是包装函数"的 `fetch`，还原后 `window.fetch` 的调用链指向 `undefined`，页面所有请求失效 | C13 注入后点 `重试` 与直接调用该按钮的 React `onClick` 均未再发出请求；在该 iframe 内直接调用 `fetch` 抛 `TypeError: Cannot read properties of undefined (reading 'apply')` | 整页重载 iframe（重建 realm） | 重载后原生 `fetch`、无残留标记、页面恢复正常 |
| C13（第四轮，2026-10-09） | 同一注入形态：`failList=1`（URL 子串 `QueryBranchRecognitionAmountList`，只让列表请求失败一次、不拦截保存端点），医院页行内提交 `66.66`；原引用在包装前保存为 `__origFetch`，避免上一轮的绑定缺陷 | 记录序列：列表请求 `injectedFail` 一次 → 保存请求未被拦截且 `status 200` → 出现「写入已成功，数据待刷新」与「重 试」；`.ant-message`/notification 为空（无保存失败提示）；302 行与页脚 `共 301 项` 保留、编辑态收起 | 清零 `failList` → 点页内「重 试」→ 第二次列表请求 `passed`；随后 `window.fetch = __origFetch` 并删除 `__origFetch`/`__inj` 标记 | `String(window.fetch)` 含 `[native code]`、标记不存在、警示消失、金额入口 `disabled=false`、首格显示 `66.66`（写入未丢失），其后整页重载再次复核 |
| C14（第四轮，2026-10-09） | 同一注入形态：`failList=1` 后点页内「搜索」触发读取 | 列表请求 `injectedFail`；保留 302 行与页脚 `共 301 项`，出现「互认项目金额数据待刷新」与「重 试」，全部金额入口 `disabled=true`，表头未被清空 | 清零 `failList` → 点「重 试」→ 第二次列表请求 `passed` → `window.fetch` 赋回原引用并删除标记 | 原生 `fetch`、无残留标记、警示消失、写入口恢复 `disabled=false`，其后整页重载再次复核 |
| C10（第四轮，2026-10-09，构造服务端拒绝） | 一次性**改写请求体**（不是让请求失败）：包装 `window.fetch`，对 `SaveBranchRecognitionAmount` 的首个请求把请求体里的 `standardProjectCode` 换成目录中未建立互认配置的真实编码 `002504030050000-01001`（`TextDecoder` 解码 → 改字段 → `TextEncoder` 重编码为 ArrayBuffer），其余请求原样转发，`rewriteOnce=1` 上限。构造理由：该前置（当前组织无该标准项目的互认配置）无法经页面操作或只读数据库取得，上一轮是经授权删除配置完成的 | 保存请求 `status 500 / ok:false`；宿主父文档出现瞬态提示「业务拒绝：当前组织未建立该标准项目的互认项目配置。」；编辑态保持打开、输入 `88.88` 保留；其后无任何列表请求；无成功提示；改写只作用于被拒绝的这一次请求、原项目零写入 | `window.fetch` 赋回原引用，删除 `__c10`/`__origFetch` 标记与两个 `MutationObserver`（iframe 与父文档各一个），随后整页重载 | 原生 `fetch`、`__c10`/`__origFetch` 不存在、观测器已 `disconnect`、页面无提示残留、目标行金额仍为 `77.77` |

C15 的页面侧可信上下文缺失形态已在第四轮按受控构造取证（改写 `dy-auth:login-user` 的 `hosID` 后整页重载、验证后写回，见「重开轮宿主面复测 · 第四轮」节；不是 fetch 注入）并转 `Passed`；C23 所需的注入上下文（宿主重开子应用时文档级钩子未命中）仍不可构造，保持 `NotRun`。

## 补充取证：组织无互认配置的拒绝面与空态

保存被拒绝面与空结果面此前无法构造，原因是组织 `01` 恒有互认配置——金额页行集由互认配置驱动，可达行必为已配置行。本轮由项目负责人删除组织 `01` 的全部互认配置（5 行）以构造该状态；删除属环境数据准备，未改代码、未改请求、未绕过任何校验，负责人已确认该数据为开发阶段测试数据、不保留备份。

**数据状态变更**：删除前 `mrec_mutual_recognition_item` 有组织 `01` 的 5 行配置（`123`、`222`、`S1-D29-ITEM-20260915-001`、`S1-RETRY-ITEM-20260914-001`、`S1-RETRY-ITEM-20260914-002`，全部启用），删除后该表为空。结果表中 V15、V21、C18、C19、C20 行内所记的只读复核（组织 `01` 的 5 条互认配置 `is_valid` 全为 `true`）描述的是**删除前**的状态；这些用例的停用类证据取自删除前的取证轮次。

**拒绝面取证序列**（`yangkj`，医院管理员入口，可信范围 `01/0101`，院区 `0101001`）：页面在删除前已加载并保持 5 行，删除后再提交未配置行 `S1-D29-ITEM-20260915-001`。

| 观测点 | 证据 |
|---|---|
| 请求 | `reqid=102` `POST http://localhost:15014/Api/MedicalRecognitionReport/SaveBranchRecognitionAmount`，请求体 `{"branchCode":"0101001","currentAmount":77.77,"standardProjectCode":"S1-D29-ITEM-20260915-001"}`，状态 **500** |
| 拒绝文案 | 响应体首行 `System.InvalidOperationException: 业务拒绝：当前组织未建立该标准项目的互认项目配置。`，堆栈指向 `MedicalRecognitionReportManager.cs:line 406`；页面以警示条呈现同一文案 |
| 弹窗与输入 | 提交后弹窗保持打开，金额输入框仍为 `77.77`；保存按钮由提交中恢复（`loading=false`、`disabled=false`） |
| 不刷新列表 | 该请求之后没有任何 `QueryBranchRecognitionAmountList` 请求；页面上删除前加载的 5 行仍在，未被清空 |
| 不误报成功 | 页面无成功提示，仅出现上述错误警示 |
| 零脏写 | 只读回读业务键 `01/0101/0101001/S1-D29-ITEM-20260915-001` 命中 **0 行**；金额表总行数仍为 **11** |
| Console | `[POST 500]` 记录上列异常与堆栈，另有 1 条 500 资源错误与 1 条 `Uncaught (in promise)`（页面按设计不吞掉写失败的拒绝） |

**空结果与空态取证**：随后点击「重新读取金额列表」→ `reqid=104` `POST /Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList`，请求体 `{"branchCode":"0101001"}`，状态 **200**、`content-type: application/json`、响应体 **`[]`**；页面表格清空并显示空态 `暂无数据` 与文案 `当前范围没有互认项目金额配置`。

**该状态仍未覆盖的面**：第二类拒绝前置（请求院区不属于可信医院）在本轮仍不可构造——医院页院区下拉只列可信医院下的院区，故 C11 不整条通过。

## 本轮修复与回归事实

### 六项修复的直接影响面（本轮取证的落点）

- **F2（前端金额解析的两道可精确表示性判据）**：在医院管理员页 `CSYQ2-2` 行 `123` 弹窗输入 `99999999999999.99`，行内出现 `金额必须是不小于 0 且最多两位小数的数字`、输入框 `aria-invalid="true"`，点「保存」后弹窗保持打开且注入的 fetch 记录器**零条目**（零写请求）；同一输入框改为 `55.55` 后再提交即成功保存。修复后的判据在真实页面上生效且未误伤正常输入。
- **F3（可信范围返回值统一去空白编码，落库口径）**：dbx 回读两次新增写入的三列编码与可信范围逐字一致且无首尾空白——`lisadmin` 为 `organization_code=01`、`hospital_code=CSYY2`、`branch_code=CSYQ2-2`（长度 2/5/7）；`yangkj` 为 `01`、`0101`、`0101001`（长度 2/4/7）。本环境外部编码无首尾空白，故结果与修复前相同。
- **F6（医院页院区默认值随可信范围派生）**：`yangkj` 默认 `0101001`（总院）；`lisadmin` 默认 `CSYQ2-1`，而其院区选项集顺序为 `{测试院区2-2, 测试院区2-1}`，默认值不是首项，说明默认值取自可信院区；整页重载后默认值仍回到可信院区。切换院区（`CSYQ2-1` → `CSYQ2-2`）触发按新院区重载：查询体变为 `{"branchCode":"CSYQ2-2"}`，响应 5 行全部 `branchName=测试院区2-2`，旧院区 `CSYQ2-1` 的行与数据在表格中不残留。
- **F1（正常金额保存未回归）**：两个身份各完成一次经宿主页面的真实保存，`SaveBranchRecognitionAmount` 返回 `200 true`，dbx 回读金额与请求值一致（`55.55`、`37.37`）；页面弹窗关闭并整表重载显示新值。页面无法省略金额字段，因此"金额缺省被拒绝"的缺省拒绝面由 V6 的契约断言覆盖，本层只确认未回归。
- **后端金额请求字段可空化与可信范围去空白**：前者对应 V6 的三重契约断言（`RequiredAttribute` + `decimal?` + 校验器真实拒绝缺省），后者对应 `Stage3OrganizationPathTests.Resolve_accepts_codes_with_surrounding_whitespace_and_returns_trimmed_codes`（目标路径与外部主数据都带首尾空白时按去空白校验并返回去空白值）与 F3 的落库回读。

### 医院管理员入口 500 的根因与修复

- **缺陷现象**：从宿主菜单进入「本院区互认项目金额」即触发 `POST /Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList`（体 `{"branchCode":"0101001"}`）返回 **500**，响应体为开发者异常页，内容为 `System.InvalidOperationException: 无法确定当前可信医院。`，堆栈指向 `TrustedOrganizationResolver.ResolveHospitalOrThrow`（`HttpRequestInfo.HosId` 为空）。
- **根因**：四个阶段 3 金额入口的可信医院只从登录令牌声明读取，而本环境的宿主登录令牌只携带 `org` 声明、**不携带 `hos`/`brh` 声明**，服务端 `HttpRequestInfo.HosId` 恒为空；客户端存储 `dy-auth:login-user.hosID` 有值但不是服务端可信来源，因此可信医院始终解析不出。
- **修复语义**（源码位于 `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs`）：四个金额入口共用可信范围解析点，组织层与医院层按同一规则解析——令牌该层非空白即取令牌值；令牌该层缺失或空白时用当前登录用户档案（`IUserAppService.GetUserByIdAsync` 的 `OrgId`/`HosId`）补齐；令牌与用户档案在同层都提供且不一致即拒绝；补齐后该层仍为空即拒绝，不使用默认值、不降级为空值；院区层不参与本解析。
- **修后真实证据（500 → 200）**：本轮两个身份都从宿主登录页登录后经菜单进入，同一端点返回 **200** 并携带 5 行；保存端点返回 **200 true**；dbx 回读落库行为 `01/0101/0101001` 与 `01/CSYY2/CSYQ2-2`，即**补齐后的值被真正用于写入**。
- **后端测试用例总数会随用例增删变化**，故本报告只记录本阶段验证矩阵的统计；本轮后端全量测试结果为测试总数 **243**、通过数 **243**、失败 **0**。

### 测试账号登记

两个身份都以同一宿主登录入口进入（本轮均重新登录取证），均持有「检验检查结果互认管理员」角色，因此都能看到本阶段的两个菜单项。凭据登记在 [Test Environment](../../../.agents/instructions/test-environment.md) 第 1 节，本报告不复制凭据。

| 账号 | 宿主显示名 | 组织 | 可信医院 | 可信院区 | 本轮用途 |
|---|---|---|---|---|---|
| `yangkj` | 杨康健 | `01` | `0101`（县人民医院） | `0101001`（总院） | 医院页默认院区与列表、可信医院 `0101` 的保存与落库回读、越权院区提交尝试 |
| `lisadmin` | 检验协同平台管理员 | `01` | `CSYY2`（测试医院二） | `CSYQ2-1`（测试院区2-1） | 第二个可信医院 `CSYY2`：院区默认值与选项顺序、院区切换重载、平台页三级选择与三值请求、写成功刷新失败与读取失败注入 |

两个身份的令牌都只带 `org:"01"`、都不带医院声明，因此两者都必然走"用户档案补齐医院层"的同一路径。

### 跨端契约快照

`server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` 冻结了前端 OpenAPI 生成物 `client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json` 的路径集合，本轮复核其字面量共 **27** 条（26 个业务路径 + 保留排除的 `/auth/login`），含本阶段新增的 4 个端点，并在本轮后端全量测试中通过。改动前端生成物后必须跑后端全量测试；只跑前端 typecheck/build 不足以发现该不同步。

## 重开轮：读模型枚举中文（S3-D21）

按 [总体计划设计](../001-总体计划/design.md) 5.3「枚举协作契约」（适用于全部后续阶段），本阶段读模型补齐枚举中文、前端不再本地映射，故本阶段重开并重新登记相关用例证据。

- **设计与契约**：新增阶段决策 `S3-D21`；`RecognitionAmountReadModel` 由 12 字段增至 14 字段，新增只读计算属性 `ItemTypeText`（可空，取值未登记时为 `null`）与 `ConfigurationStatusText`（非空，未登记取值按严格解析抛出），文本由服务端按枚举 `Description` 解析，与阶段 2 的 `S2-D15` 同口径；`docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd` 的两个读模型块同步补齐这两个字段（阶段 2 的实现当时未同步 UML，本轮一并修正）。
- **RED → GREEN**：先取得可复现的断言失败（非编译错误）——V18 字段集合断言 12≠14、新用例以反射读取属性时 `Assert.NotNull` 失败，共 3 条失败；随后加入实现，并在属性成型后把反射断言收敛为类型化断言。新增用例覆盖：枚举中文按枚举声明解析（2 组数据）、未登记取值 `ItemTypeText` 降级为 `null`、未登记取值 `ConfigurationStatusText` 抛 `ExtensionException`。后端全量测试 **247 / 247 通过、0 失败**（原 243 条 + 4 条新用例）。
- **契约与生成物**：`prepare-openapi`（path 仍 27 条）→ `generate`（`Client base url set to http://localhost:15014`）→ `typecheck` 0 → `build` 0；手写入口 `src/index.ts` SHA256 未变；`kiota-lock.json` 的 `descriptionHash` 更新，`excludePatterns` 仍为 `/auth/login`。
- **前端**：适配层改为服务端文案优先，本模块两个文案函数降为兜底（服务端未交付或为空白时使用），行视图模型形状不变；新增用例断言服务端文案优先与空白回退。阶段 3 目录 **57 通过**；前端全量 **204 通过 | 2 跳过**（本轮净增 1 条），`build` 通过。前端全量报告的 3 个未处理错误经逐目录归因来自阶段 1（1 个）与阶段 2（2 个），本阶段目录为 0。**追记（2026-09-17）**：上述两个文案函数与取值域已上移到跨页面共享模块 `src/shared/medicalItemType.ts` 与 `src/shared/configurationStatus.ts`（阶段 1、阶段 2、阶段 3 共用一份），本适配层按原名字转发，页面与用例的导入位置与行为不变（服务端文案优先、空白与未知回退兜底文案的口径不变）。
- **宿主复核**：经「互认项目」页按正常业务路径新建 3 条配置（`123` 启用、`222` 停用、`S1-D29-ITEM-20260915-001` 启用）后，在 `yangkj` 的医院管理员入口重新读取列表——`POST http://localhost:15014/Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList`，体 `{"branchCode":"0101001"}`，返回 `200`；响应体逐行携带服务端枚举中文（`123`：`itemTypeText":"检验"`、`configurationStatusText":"启用"`、`currentAmount:12.34`、`isAmountConfigured:true`；`222`：`configurationStatusText":"停用"`、`currentAmount:37.37`；`S1-D29-ITEM-20260915-001`：`configurationStatusText":"启用"`、`currentAmount:null`、`isAmountConfigured:false`）；页面逐行呈现与响应一致，Console 无 error 级与 warn 级消息。
- **受影响用例**：V18、V26 改为 14 字段并含两个枚举中文；C5 增加"配置状态文案取服务端字段"的响应体与 DOM 证据；C6、C19 在本轮数据下重新观察；C7 的金额呈现边界不在本轮变更影响范围内，按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节复用既往同轮证据。**矩阵统计口径不变**（56 条：`Passed` 48 / `Failed` 0 / `Blocked` 4 / `NotRun` 4）。

## 重开轮：两金额页统一（票 10 后端第一棒）

### 轮次、起始工作区与任务边界

- 时间 2026-10-08；范围为 [票 10「两金额页与互认项目页统一重开轮」](阶段3-Tickets/10-两金额页与互认项目页统一重开轮.md) 第 11-12 行的后端两条（建立失败基线并实现两个 Request 的 `Keyword` 与查询过滤 V31-V33；后端关键字证据记入本报告且既有 `StandardProjectCode` 入参与全部既有行为不变），外加独立审查提出的四项回改。
- 起始 `git status --short --branch` → `main...origin/main [ahead 5]`，工作区已含 11 个 `docs/` 改动与 5 个未跟踪路径（阶段设计与票 07/10/README、`docs/Api文档/`、`working files/` 等），均非本棒产生，未回退、未清理；`server/` 与 `client/` 起始为干净状态。未创建分支、未提交、未推送。
- 本棒写入：`server/` 14 个文件、本阶段 [Server/impl.md](Server/impl.md) 与本报告；数据库零写入、零造数。
- GitNexus：起始索引过期（索引提交 `4ee4d0a`，当前提交 `4449129`），按 [GitNexus Workflow](../../../.agents/instructions/gitnexus-workflow.md) 第 3 节执行 `gitnexus analyze . --name Dy.MedicalRecognition --index-only` 重建（15,518 节点、32,085 边、365 流程），再对六个目标 symbol 串行执行 upstream impact。

### 执行会话、进程、地址与账号（testing.md §7 第 3、4 条）

- 后端：`dotnet run --project Dy.MedicalRecognition/Dy.MedicalRecognition.csproj`，工作目录 `server/`，launch profile 提供 `ASPNETCORE_ENVIRONMENT=Development` 与 `http://localhost:15014`，进程 `Dy.MedicalRecognition`（PID 5072），OpenAPI `GET http://localhost:15014/openapi/v1.json` 可读。首次以 `--no-launch-profile` 启动失败（`业务拒绝：报告 PDF 存储根目录未配置`，Development 配置未加载），改用 launch profile 后正常；为解锁被该进程占用的构建输出，OpenAPI 取证完成后已停止该进程。
- 浏览器：受管 Chrome `154.0.8037.98`，经技能 `browser-debugging-and-control` 的 CDP CLI（endpoint `http://127.0.0.1:9222`，profile `%LOCALAPPDATA%\codex-cdp\profile`，会话 `dsh-stage3-amount`）；本会话未暴露 Chrome DevTools MCP，按技能路由改用 CLI。打开宿主登录页后网络失联，登录未完成。
- 本棒无宿主面用例：前端 dev server 未启动，账号与密码本轮未使用，凭据登记处为 [Test Environment](../../../.agents/instructions/test-environment.md) 第 1 节，本报告不复制。数据库本轮未连接、未执行任何 SQL；组织 `01` 的 3 条互认配置与金额表既有行保持原状。

### 外部依赖实时状态（本轮探测）

- 打开宿主登录页成功后外部宿主失联：`183.224.180.166` 的 `35000`（宿主）、`35001`（组织与用户服务）、`15432`（目标库）连续 25 分钟 `Test-NetConnection` 全部失败（末次探测 2026-10-08 23:10 +08:00），页面导航报 `net::ERR_CONNECTION_TIMED_OUT`；未取得任何令牌，未修改宿主存储，此后以 90 分钟与 120 分钟窗口轮询探测，至 2026-10-09 02:44 +08:00 仍不通。
- **恢复（2026-10-09）**：三端口恢复可达（`Invoke-WebRequest http://183.224.180.166:35000/login` 返回 200），同日以受管 Chrome 重新经宿主登录页以 `yangkj` 登录成功并在页面上下文完成 V31、V32 的真实库与真实入口复测（明细见结果表该两行）；该轮同时重启本地后端 `dotnet run`（Development，`http://localhost:15014`）与受管 Chrome 会话，前一会话的进程与浏览器在一次会话中断中已终止。
- 该状态使 V31、V32 的真实库与真实入口面不可执行：宿主令牌取不到、组织路径无法经外部服务解析、目标库不可达。两条按 `Blocked` 登记，环境恢复后按原口径复测。本节只记录本轮实时状态，不沿用历史轮次的外部服务健康状态。

### 目标失败基线（两轮 RED，全部为断言失败）

起始全量基线（改动前）：`dotnet test server/Dy.MedicalRecognition.slnx` → 失败 0、通过 819、总计 819。

**(a) 可先于生产改动取得断言失败的三个用例**。第一轮 `dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Stage3QueryTests"` → 失败 4、通过 26、总计 30：

| 用例 | 失败形态 |
|---|---|
| `Amount_query_requests_declare_optional_keyword_and_keep_standard_project_code`（V33 形状） | `Assert.NotNull() Failure: Value is null` —— `GetProperty("Keyword")` 为空，两个查询 Request 都没有该属性 |
| `Amount_query_statement_filters_four_columns_by_keyword_with_literal_escaping`（V31 语句） | `Assert.NotNull() Failure: Value is null` —— `QueryRecognitionAmountList` 语句中不存在 `Property="Keyword"` 的 `IsNotEmpty` 片段 |
| `Amount_query_repository_binds_the_keyword_parameter`（V31 绑定） | 判据消息「金额列表仓储方法未把关键字绑定进查询参数对象。」 |
| `Amount_query_requests_expose_only_the_designed_members`（V26/V18 字段集合，改写条目） | `Assert.Equal() Failure: Collections differ` —— 期望集合含 `Keyword`，实际不含 |

**(b) 只能相对中间实现取得失败的用例**。中间实现状态：两个 Request 已有 `Keyword`、查询端口与仓储已加 `keyword` 参数、应用层原样透传尚未做空白归一化。同一过滤命令 → 失败 4、通过 33、总计 37，失败全部来自 `Amount_query_entrypoints_normalize_keyword_before_reaching_the_query_port`：

| 数据组 | 失败形态 |
|---|---|
| `keyword: ""` | `Assert.Equal() Failure: Strings differ`，期望 `null`、实际 `""` |
| `keyword: "   "` | 期望 `null`、实际 `"   "` |
| `keyword: "\t\n"` | 期望 `null`、实际 `"\t\n"` |
| `keyword: "  血  "` | 期望 `"血"`、实际 `"  血  "` |

`Amount_query_requests_accept_blank_keyword` 自首次执行起即通过（它只能在 `Keyword` 属性存在后编译），按实际记录为**随实现提交的特征用例**，不计入 RED。断言形态说明：第一轮 RED 时两个过滤片段的存在性判定是 `FirstOrDefault` + `Assert.NotNull`，独立审查后改为带谓词的 `Assert.Single`（片段缺失或重复都失败）；失败条件（过滤片段缺失）在两种断言下都表现为断言失败，强化发生在 GREEN 之后并已复跑全量。

### 实现范围（14 个文件）

- 生产 7 个：[IMedicalRecognitionReportQueryAppService.cs](server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs)（两个入口 summary/param/exception 共六处补关键字口径，签名不变）、[RecognitionAmountListQueryRequest.cs](server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionAmount/RecognitionAmountListQueryRequest.cs) 与 [BranchRecognitionAmountListQueryRequest.cs](server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionAmount/BranchRecognitionAmountListQueryRequest.cs)（各增可空 `Keyword`，不带 `Required`、不拒绝空白）、[IMedicalRecognitionReportQueryRepository.cs](server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.cs)（端口增 `string? keyword`）、[MedicalRecognitionReportQueryRepository.cs](server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs)（参数对象绑定 `Keyword = keyword`）、[MedicalRecognitionReportQuery.xml](server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQuery.xml)（`QueryRecognitionAmountList` 增四列 `IsNotEmpty` 过滤与处理/原因/结果注释，语句功能注释补关键字口径）、[MedicalRecognitionReportQueryAppService.cs](server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs)（两个入口调用同类既有 `NullIfBlank` 归一化后传参）。
- 测试 7 个：[Stage3QueryTests.cs](server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs)（新增 4 个 Fact 与 1 个六数据组 Theory 及判据、变异辅助；V26 字段集合期望增 `Keyword`；替身与两个入口断言改五元组；请求构造器增 `keyword`）、[Stage3TrustedScopeTests.cs](server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs)、[Stage2QueryTests.cs](server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs)、[StubReportPorts.cs](server/Dy.MedicalRecognition.Tests/StubReportPorts.cs)、[FakeQueryRepository.cs](server/Dy.MedicalRecognition.Tests/FakeQueryRepository.cs)、[FakeStatisticsQueryRepository.cs](server/Dy.MedicalRecognition.Tests/FakeStatisticsQueryRepository.cs)、[FakeMatchQueryRepository.cs](server/Dy.MedicalRecognition.Tests/FakeMatchQueryRepository.cs)（端口签名同步）。
- 关键决定：四列过滤与阶段 2 `QueryRecognitionProjectConfigurationList` 同式（S3-D26）；`StandardProjectCode` 入参、保存链、读模型与不传关键字时的页面行为保持不变；空白归一收敛到同类既有 `NullIfBlank`；过滤片段存在性判据改带谓词 `Assert.Single`；注释用词按 [Agent Conduct](../../../.agents/instructions/agent-conduct.md) 修正。

### 目标验证、直接影响验证与静态检查

- 目标验证：`dotnet test server/Dy.MedicalRecognition.slnx`（.NET SDK 10.0.401）→ **失败 0、通过 829、总计 829**（起始 819，净增 10 = 4 个新 Fact + Theory 的 6 组数据）；`--filter "FullyQualifiedName~Stage3QueryTests"` → 失败 0、通过 37、总计 37。
- 直接影响验证：受影响用例集为两个金额查询入口的既有用例（V17-V22、V26、V29、V30 相关）与 6 个替身文件的签名同步面，全部包含在全量 829 内且通过；既有五元组断言（`keyword` 收到 `null`）证明不传关键字时端口收到的参数与改前一致。
- 静态检查：`git diff --check` → exit 0；构建 0 错误，告警为存量 CS8618（生成 ReadModel）、CS8524（既有 switch）与既有 xUnit 告警（xUnit2000/2013/2031 位于 Stage2/Stage4/Stage5 文件），本批曾引入的一条 xUnit2031（`Where` + `Assert.Single`）在回改中消除，终态无本批新增告警。
- GitNexus：`gitnexus detect-changes --repo Dy.MedicalRecognition` → 14 文件、49 symbol、16 条受影响流程、**critical**。影响审查：受影响文件与 symbol 全部落在阶段 3 重开轮的金额查询接口与测试面，属端口签名变化的预期传播（9 处端口实现方与 2 处应用层调用同步）；直接调用方、公共接口与受影响流程都有可执行验证面（全量 829、语句与源码判据、运行时 OpenAPI）；工作区跨阶段改动可逐项归属；不触发 [GitNexus Workflow](../../../.agents/instructions/gitnexus-workflow.md) 第 2 节的五项停止条件，继续执行。

### 证据区分

- 静态证据：XML 语句四列判据（3 项变异：去 `escape '!'`、放开 `lower(g.name)`、`or`→`and`）、仓储绑定源码判据（2 项变异：删绑定、改绑 `standardProjectCode`）、两个 Request 的反射形状判据、字段集合判据、`git diff --check` 与 Analyzer 告警清单。
- 本轮运行证据：两轮 RED 的失败输出与终态测试运行（829/37）、本地后端进程的 `GET /openapi/v1.json` 实测（两个请求 schema 各多 `"keyword": {"type":["null","string"]}`、`required` 不变、`paths` 57 条与冻结快照 57 条逐条相同，两 schema 除 `keyword` 外与冻结快照逐字一致）。
- 未运行：V31、V32 的真实库与真实入口面（外部依赖不可达）。

### 独立审查与回改

- 审查角色按 [Agent Conduct](../../../.agents/instructions/agent-conduct.md)、[Backend Architecture](../../../.agents/instructions/backend-architecture.md)、[C# Backend Style](../../../.agents/instructions/csharp-backend-style.md)、[C# Documentation And Reuse](../../../.agents/instructions/csharp-documentation-and-reuse.md)、[Testing Baseline](../../../.agents/instructions/testing-baseline.md)、[C# Backend Testing](../../../.agents/instructions/csharp-backend-testing.md)、`Server/design.md` 关键字口径与票 10 只读审查：无 CRITICAL、无 HIGH；4 项 MEDIUM（接口文档未同步关键字、新增归一化 Helper 与同类既有 `NullIfBlank` 重复、本批证据未落盘、新增注释用词）与 3 项 LOW。
- 回改：接口六处文档补关键字口径；删除新增的归一化 Helper、两处改用既有 `NullIfBlank`；注释用词改为「V32（请求校验面）」；两个过滤片段的存在性判定改带谓词 `Assert.Single`；`or` 连接注释去掉对比句式。复审逐项确认成立、无残留并给出「代码面可交接」结论；复审新增的一条 LOW（`or` 连接注释的句式）已改。复审同时确认：非空白关键字整体去首尾空白的口径与 `Server/design.md` 第 25 行不冲突（判定动作本身即「去首尾空白」，且 S3-D26 写明缺失或空白视为未提供、V32 要求空白返回全部行），项目内统计查询与 `TrustedScopeResolver` 均有同一先例。
- 待负责人裁定（不阻塞交接）：是否把「非空白关键字也去首尾空白」补写进 `Server/design.md` 第 25 行；设计文档改动须先获批准，本棒未修改设计文档。

### 本棒状态与下一步

- **V33**（关键字入参，`Server/design.md` 验证矩阵第 202 行，层级 `Contract`、真实数据库列「否」）→ `Passed`，证据见结果表 V33 行。
- **V31**（关键字四列匹配，`Server/design.md` 验证矩阵第 200 行）与 **V32**（关键字缺失或空白，同文件第 201 行；两条层级 `Query`、真实数据库列「是」）→ 交接时因外部宿主与目标库不可达记 `Blocked`（属环境阻断，与数据、权限无关）；语句、绑定、应用与请求校验层证据已入结果表。**2026-10-09 环境恢复后已按原口径完成真实库与真实入口复测，两行转为 `Passed`**，明细见结果表该两行。
- 复测项（已完成）：按 V31 的五类命中（编码、名称、分类名、分组名、不命中）与 V32 的空白、缺省两种请求，在真实入口与真实库上取证并更新了这两行状态。
- 下一棒：API Client 按累计规则再生成（票 10 第 13 行），随后两个页面重写与 C 条目取证；宿主面证据由宿主链路取证承接。

## 重开轮：API Client 累计再生成（票 10 第二棒）

### 范围与前置

- 时间 2026-10-08；范围为 [票 10](阶段3-Tickets/10-两金额页与互认项目页统一重开轮.md) 第 13 行：按累计规则重新生成 API Client，端点清单不变、两个金额查询 Request 各增可选 `keyword`，并核对公开入口、nullable、enum、`kiota-lock.json`、`src/index.ts` 与 `/auth/login` 排除（C24）。
- 前置为第一棒的后端冻结输入：两个金额查询 Request 已声明可空 `Keyword`，本地后端 `http://localhost:15014` 由受管后台任务启动并提供 `GET /openapi/v1.json`（进程与地址同第一棒记录）。
- 起始工作区：`client/` 起始为干净状态（`git status --short -- client/` 无输出）；本轮未创建分支、未提交。

### 目标失败基线（RED）

- 用例：`Stage3ApiContractTests.Frozen_openapi_and_generated_model_declare_optional_keyword_for_amount_queries`（新增，同时判定冻结输入与生成端）。
- 命令与结果：`dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Frozen_openapi_and_generated_model_declare_optional_keyword_for_amount_queries"` → 失败 1、通过 0；断言消息 `RecognitionAmountListQueryRequest 的属性集合缺少 keyword。`（断言失败，非编译错误、非异常穿透）。该用例的每个取值都先 `Assert.True(... is not null, ...)` 再取成员，避免用空引用异常充当失败。
- 该用例同时覆盖票 10 第 13 行的 nullable 面（`["null","string"]` 且 `keyword` 不在 `required`）与生成端面（请求模型属性与序列化函数各一处，按声明片段整体截取判定）。

### 生成命令与实际输出

| 步骤 | 命令 | 输出 |
|---|---|---|
| 固化输入 | `pnpm -F @dy/api-client-medical-recognition prepare-openapi` | 来源 `http://localhost:15014/openapi/v1.json`；修正整数联合 21 处、可空引用 26 处；path 数量 57；写入 `openapi/medical-recognition.openapi.json` |
| 生成源码 | `pnpm -F @dy/api-client-medical-recognition generate` | Kiota 1.34.1（提示 1.35.0 可用，本轮未升级）；固定 `--exclude-path /auth/login`；`Generation completed successfully`；`Client base url set to http://localhost:15014`；既有 `currentAmount` 等 `format double` 告警与既往一致 |

### 实现范围与保护面

- 差异：`git diff --stat -- client/` → 4 个文件、26 处新增、2 处删除；`git status -- client/` 无新增或删除文件，端点目录树未变。
- 冻结输入：两个金额查询请求 schema 各多出 `"keyword": {"type": ["null","string"]}`，其余逐字不变；`paths` 仍 57 条。
- 生成端：`src/models/index.ts` 的两个请求模型各增 `keyword?: string | null` 与对应反序列化、序列化行；`src/api/index.ts` 只有一行注释的大小写变化；`src/kiota-lock.json` 只有 `descriptionHash` 变化，`excludePatterns` 仍为 `["/auth/login"]`、`descriptionLocation` 仍为 `../openapi/medical-recognition.openapi.json`、`kiotaVersion` 仍为 `1.34.1`。
- 手写入口：`src/index.ts` 生成前后 SHA256 同为 `51A20E1E1EAFDB3EC456489DE33307E9864F9AFB3C47CE0791A56000E0AAE01C`，未被覆盖；生成命令未使用 `--clean-output`。
- 日志与临时文件：`src/.kiota.log` 被仓库根 `.gitignore` 第 213 行忽略（`git check-ignore -v` 实测命中），不进入提交范围。
- 生成器注释翻转（登记项，非缺陷）：`src/api/index.ts` 的 `ApiRequestBuilder` 注释由 `/api` 变为 `/Api`。原因是大小写不敏感的目录把 `/Api/*` 与 `/api/v1/*` 归入同一个 builder，后续再生成可能在两种写法之间翻转；只影响注释文本，`UriTemplate` 与功能不变。

### 验证结果

- 目标验证：同一用例转为通过；`dotnet test server/Dy.MedicalRecognition.slnx` → 失败 0、通过 **833**、总计 833（含路径集合等值断言与全部既有生成物断言；回改后复跑的终值，回改前为 830）。
- 包与子应用：`pnpm -F @dy/api-client-medical-recognition typecheck` exit 0、`build` exit 0（`dist/index.js` 292.80 KB、`index.cjs` 331.35 KB、`index.d.ts` 308.47 KB）；`pnpm -F dy-medical-recognition test` → 21 文件、428 通过、2 跳过、0 失败；`lint` exit 0；`build` exit 0。
- 静态检查：`git diff --check` exit 0。
- GitNexus：`gitnexus detect-changes --repo Dy.MedicalRecognition` → 20 文件、99 symbol、16 条受影响流程、**critical**。影响审查：新增符号主要来自生成的 `src/models/index.ts`（Kiota 机器生成，非手工改动）与一个新增测试用例；受影响面落在阶段 3 重开轮已批准范围内；验证面为后端全量 833、包 typecheck/build、子应用 test/lint/build 与保护面哈希核对；工作区跨阶段改动可逐项归属；不触发 [GitNexus Workflow](../../../.agents/instructions/gitnexus-workflow.md) 第 2 节的停止条件，继续执行。

### 独立审查与回改

- 审查角色只读审查（依据 [Frontend API Client](../../../.agents/instructions/frontend-api-client.md)、[Testing Baseline](../../../.agents/instructions/testing-baseline.md)、[C# Backend Testing](../../../.agents/instructions/csharp-backend-testing.md) 第 6 节、[Agent Conduct](../../../.agents/instructions/agent-conduct.md) 与票 10 第 13 行）：无 CRITICAL、无 HIGH；1 项 MEDIUM（新增注释两处单字「判」）与 4 项 LOW（票号引用不完整、声明截取自校验缺失、类级文档未覆盖新增用例、生成器注释翻转登记）。审查独立复跑包 `typecheck`（exit 0）与 `Stage2EnumContractTests`（31 通过）。
- 回改：注释单字「判」全部改为「判定为失败」（复审又发现 1 处行内注释残留，已一并修正）；票号引用展开为文件、行号、条目名称与适用范围；`DeclarationBlockAfter` 增加「标记在全文恰好一处」断言并新增 3 条数据的自校验 Theory（标记缺失、同名声明两处 → 断言异常；关键字只在目标声明之外 → 片段内取不到），断言类型用 `Assert.ThrowsAny<Xunit.Sdk.XunitException>`（xunit 2.9.3 下 `Assert.True` 抛其子类 `TrueException`，精确匹配实测失败）；类级 remarks 补「同时冻结累计生成的输入面与生成端面」。
- 复审确认：回改 2-5 项成立，Theory 三条无假绿路径（缺失与重复两条的抛出由 helper 自身断言承担，删除唯一性断言即变红；块外条防向后越界，截取过短方向由主用例对真实文件的正向断言承担），`ThrowsAny<XunitException>` 不会放过非断言异常（不可赋值的异常会让 `ThrowsAny` 自身失败并报告实际类型）；结论为「改完最后一处单字『判』即可交接，复跑该测试类与 `git diff --check` 即可」——该处已修正并按其指定复跑：`Stage2EnumContractTests` 失败 0、通过 34，`git diff --check` exit 0。
- 登记项：`src/api/index.ts` 注释 `/api` ↔ `/Api` 翻转只影响注释文本，后续再生成可能来回翻转（见「实现范围与保护面」）。

## 重开轮：两个金额页骨架与筛选改版（票 10 前端第三棒 · 第一阶段）

本节只覆盖第一阶段（适配层与两个页面的骨架、筛选、列视图、状态机制前半）；行内编辑替换弹窗、可信范围变化确认框与提交前现值复核属第二阶段，另节记录。

### 范围、起始工作区与文件

- 时间 2026-10-09；范围为 [票 10](阶段3-Tickets/10-两金额页与互认项目页统一重开轮.md) 第 14、16 行的骨架与筛选部分（第 15 行的行内编辑与确认框留待第二阶段）。
- 起始工作区：`client/apps/dy-medical-recognition/src/pages/recognitionAmounts/` 在本阶段开始前为干净状态；本阶段写入 7 个文件——`recognitionAmountsApi.ts`、`recognitionAmountsApi.test.ts`（适配层）、`RecognitionAmounts.tsx`、`RecognitionAmounts.test.tsx`、`RecognitionAmounts.css`（平台页）、`BranchRecognitionAmounts.tsx`、`BranchRecognitionAmounts.test.tsx`（医院页）。未改 `RecognitionAmountModals.tsx`、未改依赖与配置、未做 git 操作。
- `RecognitionAmounts.tsx` 同时删除了旧共享列表主体 `RecognitionAmountsBoard`（242 行，改版后仅医院页引用），医院页主体移入其自身文件；审查确认全仓无残留引用、无未清理导入。

### 目标失败基线（三份 RED，均在实现前只改测试取得）

| 任务 | 命令（`client/` 发起） | 结果 | 关键失败形态 |
|---|---|---|---|
| 适配层 | `pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionAmounts/recognitionAmountsApi.test.ts` | 63 条中 **23 failed、40 passed**，exit 1 | 全部 `AssertionError`，例：`expected { …(2) } to deeply equal { …(3) }`（请求体缺 `"keyword"`）、`expected "replaceState" to be called 1 times, but got 0 times`、`expected true to be false`（复核壳恒返回通过） |
| 平台页 | `... run src/pages/recognitionAmounts/RecognitionAmounts.test.tsx` | 27 条中 **26 failed、1 passed**，exit 1 | 断言失败，例：`expected "vi.fn()" to be called 1 times, but got 0 times`——旧页没有「进入页面即按默认范围自动加载」；页头范围块断言先行通过后同样卡在入口读取 |
| 医院页 | `... run src/pages/recognitionAmounts/BranchRecognitionAmounts.test.tsx` | 24 条中 **19 failed、5 passed**，exit 1 | 断言与元素定位失败，例：`Unable to find role="button" and name /搜\s*索/`（旧页无筛选区）、`expected null not to be null`（`.recognition-amounts-header-scope` 不存在）、`expected [ { branchCode: 'BRA-1' } ] to deeply equal [ …, keyword: '血常规' ]`（旧页不按地址栏关键字初始化读取） |

- 三份失败都是目标行为缺失的稳定表现，没有编译错误、测试夹具错误或环境不可用成分（[Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 1 节第 2 条）。原始输出未保存为文件，本节按执行时的回报如实登记失败数、用例名与代表性错误文本；复审阶段由审查角色按用例总数（27/24/63）与失败数相容性核对。

### 实现要点与自定口径（审查判定）

- 适配层新增五项能力：两个查询的关键字下推（去首尾空白后为空不携带）、四栏层级列视图 `toRecognitionAmountColumnView`（类型/分类跨行、分组同名合并、组间与组内排序、空集合空结构）、双状态本地裁剪 `filterRecognitionAmountRows`（两条件取交集、不触发请求）、地址栏三参数读写（初始化、`history.replaceState` 替换回写、其它参数保留）、保存前可信范围复核 `recheckRecognitionAmountScope`（逐层比较、不一致或读不到即失败并给一行中文原因）。
- 平台页：受控范围选择器 + 三档默认与切换重取 + 空值回调拒绝、页头三级范围两行右对齐、筛选区三控件、地址栏同步、四栏列视图表格、双空态、页脚「共 N 项」、读取在途写门控、读取失败保留数据可重试、写成功但刷新失败提示；金额编辑仍走既有弹窗（第二阶段替换）。
- 医院页：组织与医院固定传入不渲染对应下拉、院区可信优先且须属于回调清单否则取该院首项、可信组织或医院缺失维持 C15 阻断、查询只带院区与可选 `keyword`，主体与平台页逐行同款式（审查归一化类型名后比对差异数 0）。
- 审查对实施方自定的 27 条口径逐条判定：适配层 8 条、平台页 10 条、医院页 5 条**可接受**；4 条判为需回改（两页重复定义档位常量与文案、C27 无登记且缺「本地不按关键字判定」断言、C31 两个子面未取证、医院页缺 C6/C7 镜像）。C14 与 C23 的锚点从「页头刷新按钮」迁到「搜索主按钮 loading / 范围未选全空态」判为成立——设计取消页头纯图标刷新按钮（[Client/design.md](Client/design.md) 第 14 行），两条原预期由新锚点承载；**该迁移同时使本报告结果表 C14 行的既有宿主证据（锚定「重新读取金额列表」按钮）失效，宿主面须按新锚点重取**。

### `BaseScopeSelector` 禁用清空属性核对结论（票 10 第 18 行要求的登记）

- 核对对象：`@dy/components-base@0.1.4` 的 `BaseScopeSelectorProps`（`node_modules/@dy/components-base/dist/base-scope/BaseScopeSelector.d.ts` 第 12-31 行）。
- 结论：该 props 只有 `level`、`value`、`defaultValue`、`orgId`、`hosId`、`branchId`、`deptId`、`userType`、`disabled`、`required`、`requiredLevel`、`requiredMessage`、`selectFirstOption`、`size`、`style`、`selectStyle`、`className`、`onChange`，**没有任何禁用清空的属性**；实现把 `allowClear: true` 硬编码进三级共用的 `commonProps`（同目录 `BaseScopeSelector.js` 第 332 行），组织、医院、院区一律可清空。
- 替代方案（未自造第二套选择控件、未改组件源码）：院区（以及范围其余两级）由页面受控解析，组件 `onChange` 回来空值时页面不接受、立即保持原受控值，使院区恒定单值且永不发空院区请求；清空图标仍在但取值不会变空。实现位置 `RecognitionAmounts.tsx`（平台页空值回调拒绝）与 `BranchRecognitionAmounts.tsx`（医院页同一处理），组件层用例 `C30` 断言清空后取值不变、请求体不出现空院区。
- 该结论已向负责人回报（票 10 第 18 行要求「属性不满足时先回报再定替代方案」）；负责人未另行指示时按上述替代方案实施。

### 验证结果

- 目标验证（回改前）：三份单文件测试 27/27、24/24、63/63 通过。
- 独立审查复跑：`pnpm -F dy-medical-recognition test -- src/pages/recognitionAmounts` → 3 文件 **114/114 通过**；`lint` exit 0；`git diff --check`（全仓）exit 0。
- 本会话统一验证：回改前为 21 文件、483 通过 / 2 跳过 / 0 失败；回改与复审（净增 10 条用例）后的**第一阶段终态**为 21 文件、**493 通过 / 2 跳过 / 0 失败**（本会话与复审角色各自实测一致，exit 0）；`lint` exit 0；`build` exit 0；`git diff --check` exit 0。
- 范围核对：本阶段 7 个文件全部位于 `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/`；`server/` 与 `client/packages/` 的既有改动分别归属票 10 第一、二棒，`docs/` 归属文档同步，提交时按归属分开（见「执行边界」）。

### 独立审查与回改状态

- 审查结论：无 CRITICAL、无 HIGH；7 项 MEDIUM（重复常量、`BaseScopeSelector` 结论未登记、本棒 RED 与执行证据未登记、C27 无登记、C31 两子面未取证、医院页缺 C6/C7 镜像、范围核对）与 10 项 LOW；重复主体逐行比对差异数 0，旧共享 Board 无残留，`AmountEntryModal` 与 C8-C10/C21 既有语义未被本阶段破坏。
- 回改：M1（档位常量与文案集中到适配层导出）、M4（C27 登记与「本地不按关键字判定」断言）、M5（C31 切换后两行同范围与在途不渲染过期名称）、M6（医院页 C6/C7 与「可信院区不在清单」镜像），并同批处理 L1（空表页脚「共 0 项」）、L2（`null` 名称排序）、L3（`layers` 空集语义）、L5（清除 8 处「X 而不是 Y」句式注释——HEAD 既有 7 处、本轮期间新增 1 处——并改写为正面表述，复审 grep 复核 6 个文件现为 0）、L8（文件头补 C12）。M2、M3 为本节登记内容。复审对本轮新发现的 4 处文字级问题（1 处代码注释与 2 处报告用词的禁用字、1 处证据数字）已一并更正。
- 登记项（不改代码）：设计文字「选择器加载完成前只展示编码」在平台页首帧没有编码来源，实现为「范围未解析则整块不显示、名称未就绪则不显示名称行」，是否修订设计文字由负责人决定；`RecognitionAmounts.css` 的 `.recognition-amounts-prompt` 为收口轮既有未引用样式，预留给第二阶段「待切换提示态」；两页与「互认项目」页的款式在结构层一致（跨行与 `content-visibility` 两条设计硬性要求均满足），字重与灰度取值存在差异，按「款式同 S2-D16」的解释口径登记。
- 复审结论：M1-M6 全部成立、L5 改写语义等价、两个主体归一化比对仍为零差异；复审独立复跑全量 `vitest run --reporter=dot` → 21 文件、493 通过、2 跳过、0 失败，`lint` 与 `git diff --check` 均 exit 0。复审另发现 4 处文字级问题（1 处代码注释禁用字、2 处报告用词、1 处终态数字），已全部更正并复跑 `recognitionAmountsApi.test.ts` 65/65、`git diff --check` exit 0、六个文件禁用词与句式 grep 计数 0；复审据此给出「可进入第二阶段」的结论。另有 1 项观察升级需负责人裁定：两个页面复制的列定义与筛选区内有 16 个逐字相同的字面量（列标题、占位与行内状态词），彻底集中需抽取列定义、实质重建共享主体，与本票「医院页自带主体」的结构指令冲突，故按现状登记。

### 第二阶段（平台页）：行内编辑与状态机制（2026-10-09）

- 范围：`RecognitionAmounts.tsx` 与 `RecognitionAmounts.test.tsx` 两个文件；交付行内金额编辑、可信范围变化确认框、提交前现值复核接线、用户主动切换静默清空，并移除操作列与平台页对 `AmountEntryModal` 的引用（`RecognitionAmountModals.tsx` 本阶段未改，医院页仍在用）。
- **RED（两段）**：第一段改写 C8、C9/C12、C10、C21 并新增 C32（6 条）、C33（4 条）、C34（2 条）后 → 15 failed / 27 passed（42），失败形态为 `Unable to find a label with the text of: 金额`（行内编辑器尚不存在）；按裁定补做 C13、C5、C36、C14 改写后 → **19 failed / 23 passed（42），exit 1**，新增失败为 C5 列集合 `expected Array(9) to deeply equal Array(8)`、C14 与 C36 的 `toBeDisabled` 收到旧金额格 `<span>`、C13 的行内定位失败。全部为断言或元素定位失败，无编译错误。
- 实现要点：删除操作列与 `AmountEntryModal` 引用；页面内 `AmountInlineEditor`（回车提交、Esc 取消、`aria-invalid` 随输入重算、以「编码|当前金额文本」为键重挂、非法值就地提示且零写请求）；金额格改为可禁用的 link 按钮（沿用 `recognition-amounts-number`）；编辑目标由主体 effect 与状态档裁剪、搜索交付、范围切换统一同步（目标行离开行集即清空）；C33 在 `handleSelectorChange` 现读可信院区与 `trustedBranchAppliedRef` 比对，变化且有未保存内容时弹「可信范围已变化（放弃并切换／取消）」，取消进入 `.recognition-amounts-prompt` 待切换态，再次确认或未保存内容消失后自动切换（自动切换经事件改道以满足 `react-hooks/set-state-in-effect` 规则）；C34 在 `submitAmount` 发起写入前调用 `recheckRecognitionAmountScope({ layers: ['branch'] })`，失败即零写请求并把 `reason` 渲染在行内；C10 的提交拒绝不捕获；C21 范围应用统一走 `applySelection` 静默清空。
- **终态**：平台页单文件 **42 通过 / 0 失败**（连续 3 次），`lint` exit 0、`tsc -p tsconfig.app.json` exit 0、`git diff --check` exit 0；其后统一复跑全子应用 `vitest run --reporter=dot` → 21 文件、**505 通过 / 2 跳过 / 0 失败**（该数字测于下列两处小修之前）。
- 我在复验中补的两处小修：C31 的在途 `ant-btn-loading` 断言由同步断言改为 `await waitFor(...)`（与 C14、C36 同型，消除高负载下的时序竞争）；C23 中删除操作列后恒真的「维护金额按钮不存在」改为「无任何金额写入入口」并注明理由。修后平台页单文件复跑 42/42。
- **C34 口径裁定（2026-10-09 已裁定，采用方案甲）**：`Client/design.md` 第 34 行的「与页面已应用范围比对」原被实现按**用户选定院区**与**登录可信院区**比对，导致平台管理员跨院区保存被零写请求拦下（与 S3-D1、S3-D17 的入口取值口径及收口轮 V29/V30 的跨院区保存证据冲突）。裁定：**比对对象是可信范围的变化**——保存发起前现读可信院区（医院管理员页连同可信组织、医院），与「页面应用范围时记录的可信范围」比对；已变化或现读不到即零写请求并在行内就地给出可见原因；用户选定范围与可信默认值不同（平台页跨院区维护）不触发拦截。设计文档三处已由设计方同步更新（阶段根 [design.md](design.md) 的 S3-D28 行、[Client/design.md](Client/design.md) 第 34 行、[Client/testPlan.md](Client/testPlan.md) 第 42 行 C34 已扩为双场景），以该三处为准，本报告不改设计正文。回改与两页接线按此执行。

### 第二阶段（医院页）与独立审查回改（2026-10-09）

- 范围：`BranchRecognitionAmounts.tsx` 与其测试、`RecognitionAmountModals.tsx`（删除）、`recognitionAmountsApi.ts`（复核注释两处按现状改写、`AMOUNT_VALIDATION_MESSAGE` 迁入导出）。医院页交付行内金额编辑、删除操作列、C33 确认框与待切换提示态（组织医院层为渲染期判定、院区层当时为事件内判定）、C21 静默清空；**C34 提交前复核按待裁定口径暂缓接线**（无调用、无用例）。
- **RED（三段，均为断言或元素定位失败、无编译错误）**：① 改写 C4 保存链、C5、C21、C36 并新建 C8、C9/C12、C10、C13 镜像、C32（6 条）、C33（4 条）后 → **17 failed / 26 passed（43）**，主形态为 `Unable to find a label with the text of: 金额`（行内编辑器尚不存在），另有 C5 九列对八列、C36 金额格 `toBeDisabled` 收到旧 `<span>`；② 按裁定补做「可信组织或医院换绑」两条 C33 用例后 → **20 failed / 25 passed（45）**；③ 审查回改阶段补三条挂起态写门控用例后 → 目录级 **3 failed / 152 passed（155）**，失败断言均为 `expected "vi.fn()" to not be called at all, but actually been called 1 times`（挂起态提交仍发出写请求）。
- 实现要点：金额格改 link 按钮（沿用 `recognition-amounts-number`）并删除「操作」列；页面内 `AmountInlineEditor`（回车/Esc、`aria-invalid` 随输入重算、以「编码|金额文本」为键重挂）；编辑目标由主体 effect 与状态档裁剪、搜索交付、范围切换统一同步；C33 的组织医院层在渲染期用 `selection.scopeKey` 比对（挂起与取值同一次渲染求值，避免中间态打断读取条件对象身份产生重复读取）、院区层在选择器回调内现读比对，确认框与 `.recognition-amounts-prompt` 提示态、事件改道实现「内容消失后自动切换」；C10 的提交拒绝不捕获。
- **终态**：医院页单文件 **45 通过 / 0 失败**；审查复跑目录级 `vitest run src/pages/recognitionAmounts` → **3 文件、152 通过、0 失败、exit 0**（回改后 155）；`tsc -p tsconfig.app.json`、`lint`、`git diff --check` 均 exit 0。
- **删除 `AmountEntryModal` 与死文件**：医院页停用后，`AMOUNT_VALIDATION_MESSAGE` 迁入 `recognitionAmountsApi.ts` 导出、两页改从适配层导入，随后删除整个 `RecognitionAmountModals.tsx`（含无导入方的 `RecognitionAmountWriteGate`）；删除前仓库 grep `RecognitionAmountModals` 仅剩两页导入与 docs 历史叙述、**测试零引用**，删除后 `client/` 内复 grep 无命中。
- **独立审查结论（需回改）**：1 CRITICAL、1 HIGH、4 MEDIUM、3 LOW；5 项必须回改——① 两页挂起态写门控缺失（医院页 CRITICAL、平台页 HIGH，违反 [Client/design.md](Client/design.md) 第 34 行「组织未对齐期间不允许对旧范围发起写入」）；② 适配层三处注释与实现不符；③ 本节证据与第 405 行过时表述未登记；④ C34 组件面只断调用次数未断入参；⑤ C33 院区层（两页）与平台页全层只在选择器回调内检测，token 单独重签不触发，构成设计符合性缺口。审查同时确认：C32 两页 10/10 命中、C33 三分支齐、C21 与 C33 触发源区分成立、改写用例与 testPlan 等价未放水、`AmountEntryModal` 零引用、无生产测试入口、新增注释禁用字与句式 grep 为 0；实测目录 152/152、`lint` 0、`git diff --check` 0，未复现此前那次单用例失败。
- **回改处置（前 5 项）**：第 ①②④ 项已执行（RED `3 failed / 152 passed（155）`，实现后 155 全绿；含删除死文件前的零引用 grep 证据与 `tsc`/`lint`/`git diff --check` 全 exit 0）；第 ③ 项即本节与下节登记；第 ⑤ 项已执行——两页 C33 触发改为渲染期派生（照 [RecognitionProjects.tsx](../../../client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx) 第 402-429 行先例：state 比较派生挂起三态、effect 内 `setTimeout(0)` 自动切换、渲染期不读 ref），平台页补 `useTrustedBranchCode()` 接入 token 订阅（此前该页无任何订阅），医院页院区层比对从选择器回调上移到渲染期，无未保存内容时按 S3-D22 三档规则重取院区；新增 4 条「只推进 token、不碰选择器」用例，RED `4 failed / 155 passed（159）`，失败断言为「弹窗未找到：可信范围已变化」与「expected vi.fn() to be called 2 times, but got 1 times」；实现中随行为变化改写了医院页 C3 的 stimulus（原 stimulus 与第 34 行「无未保存内容时直接切换」矛盾），复审判定为正当的测试意图更新。
- **复审后回改批次（第 3 轮回改）**：3 条必须项与 1 条过时注释已执行——医院页换绑挂起的 `nextSelection.scopeKey` 改用回调自带组织医院值（堵住「新范围键 + 旧医院院区」混合体）；平台页 `trustedBranchChanged` 补 `trustedBranch !== null` 闸门且 `completeSwitch` 改取订阅值（堵住 token 缺失时的条件性渲染循环）；C33 组头注释改为「渲染期派生为主、选择器回调为第二道」。RED：**3 个用例失败、4 条失败断言**（缺陷下的混合范围读取 `expected { branchCode: 'BRA-2' } to deeply equal { branchCode: 'BRA-9' }`；token 缺失后现读计数 `expected 5 to be 4` 表示重排在持续、以及有未保存内容时误弹 `expected true to be false`），两条软断言并列记录同一用例的两个缺陷面。另按可选建议补齐：每页 1 条「只推进 token → 有未保存内容 → 确认框内不碰选择器直接放弃并切换 → 院区按 S3-D22 三档现解析」端到端用例（**写后即绿、未产生 RED**，如实记为覆盖补齐），以及医院页直接切换用例的 `waitFor` 时序加固（该加固使既有用例先红、随后在同一用例内把读取序列写明确）与列表 mock 缺省 `mockRejectedValue`（只加医院页用例文件，未用 `mockResolvedValue([])`）。
- **回改后终态（本报告最后一次组件面复跑）**：目录级 `recognitionAmounts` 三文件 **163 通过 / 0 失败**（基线 152、回改前 159），连跑两次一致；全子应用 `vitest run` → **21 文件、532 通过、2 跳过、0 失败**；`lint` exit 0；`build` exit 0；`git diff --check` exit 0；`tsc -p tsconfig.app.json` exit 0。
- 按已接受登记（不改代码）：`switchTarget` 跨挂起回合残留（可信值回摆使挂起自然结束时目标未清，下一回合确认会用上一回合的选择，平台全层与医院院区层可达、医院换绑层有范围键门兜底）；平台页事件内第二道现读未过 token 闸（渲染期派生已被闸门拦住、保存面由 C34 现读复核继续拦）；时序加固的列表 mock 缺省只加在医院页用例文件（是否对称加到平台页待定）。
- **第二阶段终态（两页 + 回改 + 触发上移）**：全子应用 `pnpm -F dy-medical-recognition exec vitest run` → **21 文件、528 通过、2 跳过、0 失败**；`lint` exit 0；`build` exit 0；`git diff --check` exit 0；目录级 `recognitionAmounts` 三文件 159 通过（回改前基线 152）。
- **复审判定与新发现**：前 5 项必须回改逐项确认成立（写门控两页接线且在确认框与提示态双阶段断言零写、注释与死文件处置干净、文档三段 RED 与终态齐全、C34 入参断言判别得住、C33 渲染期派生与阶段 2 先例同构），复审独立复跑目录 159/159、全量 528/2/0、`lint` 0、`git diff --check` 0。三件事判定：① 医院页 C3 的 stimulus 改写属**正当的测试意图更新**——原 stimulus 与 [Client/design.md](Client/design.md) 第 34 行「无未保存内容直接切换」直接冲突，「显式选择不被可信默认值覆盖」仍有三处判别位置（同用例改选被采纳、C30 显式改选被接受、C33 正向覆盖新语义），强度未降；② 挂起期间选择器回调记录 `switchTarget` 的三分支与第 34 行一致，但引入两处状态缝；③ 两处时序依赖（`act` 推进 `setTimeout(0)`、新用例须先 `armQuery`）均判**可接受**——读取效果的清理使两种交错收敛为一次读取且请求体断言 fail-safe，`armQuery` 漏排以类型错误响亮失败属 fail-fast。复审新发现 2 项 MEDIUM（医院页换绑挂起改选会盖出「新范围键 + 旧医院院区」混合体；平台页可信比对缺 token 闸门导致 token 缺失时条件性渲染循环）与 4 项 LOW：3 条必须项与 1 条过时注释的回改、以及「无目标确认」覆盖缺口的补齐用例已交由回改执行者处理（本报告在回改完成后更新其 RED 与终态）；报告状态半句矛盾由本节直接更正；`switchTarget` 跨挂起回合残留按已接受登记，不改代码。
- 登记：一次全量运行出现过 **1 条用例失败但未捕获用例名**，其后 6 次全量与目录运行全部全绿（两份全量日志落在已 gitignore 的 `var/`），按偶发性残余登记，宿主面复测期间若复现须抓取用例名与断言。

### 本节未覆盖（留待后续）

- 第二阶段：平台页与医院页的行内编辑（C32）、确认框（C33）、静默清空（C21）与挂起态写门控均已完成，`AmountEntryModal` 与整个 `RecognitionAmountModals.tsx` 已删除（`AMOUNT_VALIDATION_MESSAGE` 迁入 `recognitionAmountsApi.ts` 导出，无导入方的 `RecognitionAmountWriteGate` 一并移除）；**两页 C34 接线**仍待本节所列口径裁定（平台页按 `Client/design.md` 第 34 行字面口径已接线并补入参断言，医院页暂缓）；审查第 ⑤ 项（C33 触发时机上移到渲染期）已执行并经复审逐项确认成立，其回改后新增的 3 条必须项见下条。
- 宿主面：C1（范围切换）、C3、C5、C14、C16、C17、C26 等的宿主证据因页面改版按 [Testing Baseline](../../../.agents/instructions/testing-baseline.md) 第 5 节判为待复测；外部宿主已于 2026-10-09 恢复可达（见「外部依赖实时状态」），待复测的原因是页面改版使既有宿主证据失效，安排在前端两阶段完成后统一执行。
- 医院页的 C8-C14 镜像组件用例：审查判定不构成缺口（弹窗为共享组件、页面侧实现与平台页逐字相同，医院页保存链路由 C4 覆盖），维持原状。

## 重开轮宿主面复测（票 10 · 2026-10-09）

### 执行会话、进程与资源来源

- 时间 2026-10-09；身份 `yangkj`（可信 `01/0101/0101001`）经宿主登录页 `http://183.224.180.166:35000/login` 登录，入口选「检验检查结果互认平台」，登录令牌载荷只含 `org:"01"`（与收口轮同形，不带医院声明）。
- 进程：后端 `dotnet run --project Dy.MedicalRecognition/Dy.MedicalRecognition.csproj`（`server/`，launch profile Development，监听 `http://localhost:15014`，PID 42396）；子应用 `pnpm -F dy-medical-recognition dev`（监听 `http://localhost:3008`）；受管 Chrome `154.0.8037.98`（CDP endpoint `127.0.0.1:9222`，profile `%LOCALAPPDATA%\codex-cdp\profile`，会话 `default`）。本节结束时三者保留运行，供后续复测继续使用。
- 拦截核对：宿主 `localStorage` 实测 `dy-web-micro:dev-url-intercepts-enabled="true"`，映射含 `{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}`；两个页面的 iframe 内 `script[src]` 实测为 `http://localhost:3008/subApps/medical-recognition/@vite/client` 与 `.../src/main.tsx`，拦截生效。
- 链路：两个页面均从宿主菜单「检验检查结果互认」展开后点击进入，落在工作区标签 iframe（医院页 `medical-recognition_tab_1_v1`、平台页 `medical-recognition_tab_2_v1`，均为宿主 origin 的子文档）；Console 只有 vite debug 级消息、error 级 0 条；本节记录的全部请求状态码均为 200，无 5xx。
- 取证手段（受控构造，未改生产代码、未新增生产测试入口）：在子应用 iframe 的 `window.fetch` 外层安装一次性记录钩子，只记录 URL、方法、**请求体**与状态码；**不记录请求头**——调试期间曾把含 `Authorization` 的请求头打印进执行输出，此后钩子改为只输出请求体，后续取证一律不输出任何请求头。
- 数据变化（全部经正常业务接口形成，未写库、未改表）：医院页两次保存 → `01/0101/0101001/002504030040000-01001 = 12.34`、`01/0101/0101001/002501020020000-01001 = 0.00`；平台页一次保存被提交前复核拦下（零写入，见发现 2）；关键字筛选只读。组织 `01` 当前 **301** 条互认配置（3 个分类、26 个分组）。

### 本节取得的宿主面证据（按矩阵条目）

| 条目 | 宿主面证据（2026-10-09 实测） |
|---|---|
| C2 平台管理员页请求内容 | 搜索请求体 `{"branchCode":"CSYQ1-1","hospitalCode":"CSYY1","keyword":"血常规","organizationCode":"01"}` → 200（三值齐全且带下推关键字） |
| C3 医院管理员页范围选择 | 页头名称行 `01 / 县人民医院 / 总院`、编码行 `01 / 0101 / 0101001`，组织与医院不渲染下拉、院区下拉选中可信院区；「可信院区缺失」子场景仍按环境边界不可构造 |
| C4 医院管理员页请求内容 | 查询体 `{"branchCode":"0101001"}`、`{"branchCode":"0101001","keyword":"尿常规"}`；保存体 `{"branchCode":"0101001","currentAmount":12.34,"standardProjectCode":"002504030040000-01001"}` 与 `{"branchCode":"0101001","currentAmount":0,"standardProjectCode":"002501020020000-01001"}`，均不含 `organizationCode`/`hospitalCode` |
| C5 列表字段集合 | 两页表头实测 8 列 `类型/分类/分组/项目/当前金额/金额状态/互认配置状态/当前不可用原因`，无「操作」列、无组织/医院/院区名称列与编码列；页头三级两行（名称行在上、编码行在下） |
| C6 未配置呈现 | 关键字筛选后多行显示「未配置」标记且金额位置无数值 |
| C7 零元与未配置区分 | 行 `检验/检验专业/尿常规检验/002501020020000-01001/酸碱度` 金额 `0.00`、金额状态 `已配置`，与同屏「未配置」行明显不同 |
| C8 金额输入校验 | 两页各一次行内输入 `-5` 后回车：行内提示「金额必须是不小于 0 且最多两位小数的数字」、`aria-invalid="true"`、编辑态与输入保留、**写请求数为 0** |
| C9 金额保存成功 | 医院页保存返回 200 → 编辑态收起 → 立即整表重载，首格由「未配置」变为 `12.34` |
| C12 同值重复保存 | 行内回填 `0.00` 不改直接回车 → 仍发出保存请求（`currentAmount:0`）→ 200 → 收起编辑态，行值不变 |
| C16 排序 | 层级格实测 `类型 rowspan=116（检验）`、`分类 rowspan=116（检验专业）`、`分组 rowspan=6/13/10/2（凝血功能检测、分子生物学检测、尿常规检验、微生物学检验）`，组间按类型、分类、分组归类、组内按编码升序 |
| C17 空态 | 关键字 `zzz不存在9527` → 请求体带该关键字、返回 200、页脚 `共 0 项`、表格空态「暂无数据」+「没有符合当前筛选条件的互认项目金额」（零配置空态由组件层用例覆盖） |
| C22 规模与不分页 | 本轮全部操作都在 **301 行**真实配置规模下进行：两页进入与渲染、三次关键字筛选、两次保存均正常返回、无超时、无可见卡顿、Console 无业务错误；表格仍无分页控件 |
| C25 运行时配置 | 选项请求实测打到 `http://183.224.180.166:35001/Api/Organization/QueryAllOrganization`、`QueryAllValidHospitalByOrgId`、`QueryAllValidBranchByOrgId`（均 200），本地数据请求打到 `http://localhost:15014` |
| C26 宿主链路 | 两页均从宿主菜单进入并在工作区标签载入；资源来自 `http://localhost:3008`（`@vite/client`、`src/main.tsx`）；Console error 级 0 条；本节记录的请求无 5xx |
| C27 关键字搜索 | 医院页回车提交体 `{"branchCode":"0101001","keyword":"尿常规"}` → 200、页脚由 `共 301 项` 变 `共 10 项`；点清空图标 → 体 `{"branchCode":"0101001"}`（不带 `keyword`）→ 页脚回到 `共 301 项`；平台页同款体带三值 |
| C29 地址栏参数同步 | 两页 iframe 地址实测 `…/branch-recognition-amounts?keyword=%E5%B0%BF%E5%B8%B8%E8%A7%84`（尿常规）与 `…/recognition-amounts?keyword=%E8%A1%80%E5%B8%B8%E8%A7%84`（血常规），未提交的草稿与未变更的状态档参数未写入 |
| C31 页头三级范围展示 | 名称行与编码行两行右对齐、无文字标签（平台页 `县医共体/测试医院一/测试院区1-1` + `01/CSYY1/CSYQ1-1`；医院页 `01/县人民医院/总院` + `01/0101/0101001`） |
| C32 行内编辑金额 | 点击「未配置」标记进入行内编辑器（`aria-label="金额"`）、回填当前值；回车提交合法值 → 保存 200 → 收起并整表重载；Esc 与「行离开行集不渲染编辑态」由组件层用例覆盖 |
| C35 页脚计数 | 页脚实测 `共 301 项` / `共 10 项` / `共 0 项`，随生效筛选变化 |
| C36 读取在途写门控 | 读取在途的按钮 loading 与入口禁用由组件层用例覆盖；宿主面本轮未单独构造在途帧 |

### 本节发现（两项，均在平台管理员页）

1. **平台页初始范围未按 S3-D22 取可信值（已回改）**：S3-D22（阶段根 [design.md](design.md) 第 55 行，`DesignConfirmed`）要求「初始值取可信院区（`getBranchID()`）」「可信组织或医院缺失时组织、医院、院区逐级取第一项仅适用平台管理员页」；实测 `yangkj` 进入平台页的初始范围为 `01 / CSYY1 / CSYQ1-1`（逐级取首项），而其可信范围是 `01 / 0101 / 0101001`，因此**初始院区既非可信院区、也未按可信组织医院级联**。对照医院页初始为 `01/0101/0101001`（正确）。根因：平台页把范围初值交给选择器 `selectFirstOption` 逐级取首项，可信值只在院区档参与比对，而此时的「当前组织、医院」已是首项组合，可信院区不属于该组合，于是落到第二档取首项。**回改（2026-10-09，RED 先行）**：新增 `resolveInitialScopeSelection` 按 S3-D22 逐级取可信值——可信组织与医院都在时组织、医院、院区各取可信值并逐层核对归属，任一缺失即整条级联取第一项；首项回调与受控值不一致时先只应用组织与医院、院区留空（查询构造返回 `null`、零请求），由选择器按受控值重载新清单后完成第二轮解析。RED 两轮：首轮 `3 failed / 45 passed（48）`（档一与两条档二期望可信三值、实得首项组合），补单层缺失两条后 `2 failed / 48 passed（50）`；终态平台页单文件 **50 通过 / 0 失败**、医院页 51/51 不受影响，`tsc -p tsconfig.app.json`、`lint`、`git diff --check` 全 exit 0。影响面：C1、C30 的宿主面需按新初值复测（见「本节未覆盖」）。
   - 登记的设计歧义（未改设计正文）：① 设计写「可信组织**或**医院缺失」（S3-D22 第 55 行、`Client/design.md` 第 21 行），实现已按「或」对齐（任一缺失即整条级联，仍存在的那个可信值也不采用）；② 「可信组织不属于选项清单」设计未覆盖，实现按取第一项处理（否则受控值与组件校正反复互覆、无法收敛）；③ 初始解析比旧路径多一次选项装载往返，期间沿用既有「范围未解析」空态，宿主面按 `yangkj` 复验初值与页头三级名称。
2. **平台页跨院区保存被 C34 字面口径拦死（已复现，已裁定方案甲，回改中）**：平台页选中范围 `CSYQ1-1` 后行内保存 `99.99`，未发出保存请求，行内出现复核原因「可信院区已变化：页面范围为「CSYQ1-1」，可信范围为「0101001」，本次不提交写入，请刷新页面后重试。」——即 [Client/design.md](Client/design.md) 第 34 行「与页面已应用范围比对」按**用户选定院区**与**登录可信院区**比对时，平台管理员只要切到其它院区就无法保存（与 S3-D1、S3-D17 的入口取值口径、收口轮 V29/V30 的跨院区保存证据冲突），且提示文案「刷新页面后重试」在刷新后仍会复现。2026-10-09 已裁定采用**方案甲**：比对对象是可信范围的变化（现读可信范围 vs 页面应用范围时记录的可信范围），用户选定范围与可信默认值不同不触发拦截；设计文档三处由设计方同步更新，以该三处为准。该旧文案随旧口径一并移除，回改与两页接线完成后按 C34 双场景（可信范围已变化且未应用 → 零写并行内说明；平台页跨院区常态 → 照常提交）复测宿主面。**同日第二轮复测的更正**：本条最初记的「宿主已复现」实为开发服务器陈旧转换缓存所致——浏览器执行的是缺少 `appliedTrustedBranch` 透传的中间模块，复核入参两侧落空才被拦；重启 `pnpm -F dy-medical-recognition dev` 并硬重载后，同一跨院区操作照常提交并返回 200。本条保留为**设计字面冲突的记录与裁定过程**，其中的宿主复现证据由下节第二轮证据取代；取证前置步骤一并记入下节。

### 本节未覆盖（留待后续）

- 平台页的 C1（范围切换宿主面）、C10、C13、C14、C18-C20 的宿主面在页面改版后判为待复测（组件面已重测）；C33、C34 的宿主面按上节发现与待裁定口径处理。
- C13、C14、C23 需要受控注入，C33/C34 需要受控的 token 重签构造：本轮未执行，执行时须沿用「一次性钩子、只在运行时注入、验证后立即还原、不新增生产测试入口」的既有纪律。

## 重开轮宿主面复测 · 第二轮（S3-D22 初值与 S3-D28 方案甲，2026-10-09）

### 会话、取证前置与两处更正

- 进程与链路同上节；**取证前置**：`pnpm -F dy-medical-recognition dev` 需要重启。首轮复测时开发服务器的转换缓存未随文件写入失效，浏览器执行的是中间版本（`RecognitionAmountsPlatformBoard` 只收到 12 个 prop、缺少 `appliedTrustedBranch`），随后出现的「平台页跨院区保存被拦」正是该陈旧模块所致——**上节发现 2 的「宿主已复现」证据据此更正为陈旧转换产物，不是产品缺陷**；方案甲的裁定本身来自 [Client/design.md](Client/design.md) 第 34 行的字面冲突与代码审读，不依赖该观察，裁定与回改不受影响。重启开发服务器后同一操作复测通过（见下）。本节取证步骤因此固定为：重启 `dev` → 硬重载宿主 → 用 React fiber 探针核对 Board 收到 13 个 prop（含 `appliedTrustedBranch`）→ 再开始取行为证据。
- **身份变化（如实登记）**：本轮会话内宿主登录身份由 `yangkj`（可信 `01/0101/0101001`，第一轮证据）变为 `lisadmin`（`dy-auth:login-user` 实测 `orgID:"01"`、`hosID:"CSYY2"`、`branchID:"CSYQ2-1"`，本节证据）；变化时点未观测到（其间只做了子应用 iframe 重载与宿主根导航）。两账号同持两种角色，本节证据按 `lisadmin` 的可信范围记。
- 受控构造与恢复（一次性页面内脚本、运行时注入、验证后立即还原、不新增生产测试入口、不打印请求头）：**构造**——在页面上下文改写 `localStorage['dy-auth:login-user']` 的 `branchID`（`CSYQ2-1` → `CSYQ2-2`）模拟宿主重签 token 后的可信院区变化；平台页在同一同步块内直接回车提交（提交期现读），C33 确认框则在改写后触发一次父层状态变化（点选择器切换医院）；**恢复**——把 `branchID` 写回 `CSYQ2-1`、整页重载宿主，核对存储三值 `01/CSYY2/CSYQ2-1`、两个页面页头、无编辑器、无提示态、无拦截原因（末次核对实测全部为干净状态）。中途一次还原前的中间观察已一并记录，未留存改写值。

### 本节取得的宿主面证据

| 条目 | 宿主面证据（2026-10-09 第二轮，身份 `lisadmin`） |
|---|---|
| C1 平台页范围选择 | 两页进入即得可信范围（见 C30 行）；平台页把医院由 `测试医院二` 切到 `测试医院一` → 院区自动重取 `测试院区1-1`（可信院区 `CSYQ2-1` 不属于该院，按第二档取该院第一项）、页头两行同步、`QueryRecognitionAmountList` 体 `{organizationCode:"01", hospitalCode:"CSYY1", branchCode:"CSYQ1-1"}` → 200、切换与读取期间**零写请求**；组织选择器单值 `县医共体`（固定） |
| C1 医院页范围选择（镜像） | 页内只有一个院区选择器（组织、医院不渲染）；选项清单实测仅该院两个院区 `测试院区2-2`/`测试院区2-1`（清单按可信医院裁剪）；切到 `测试院区2-2` → 页头两行同步为 `01/测试医院二/测试院区2-2` + `01/CSYY2/CSYQ2-2`、`QueryBranchRecognitionAmountList` 体 `{branchCode:"CSYQ2-2"}` → 200、**零写请求** |
| C9 平台页保存成功（本轮补证） | 跨院区（`01/CSYY1/CSYQ1-1`）行内提交 `88.88` → `SaveOrganizationHospitalBranchRecognitionAmount` 体 `{organizationCode:"01", hospitalCode:"CSYY1", branchCode:"CSYQ1-1", standardProjectCode:"002504030040000-01001", currentAmount:88.88}` → **200**、编辑态收起、紧接整表重载、首格显示 `88.88`。该证据同时补掉上节 C9 的「平台页宿主面待复测」 |
| C30 / C31 初值与页头 | 重启开发服务器后的两页初始范围都等于可信范围：平台页 `县医共体/测试医院二/测试院区2-1` + `01/CSYY2/CSYQ2-1`（S3-D22 初值回改生效，且不再是逐级取首项）、医院页 `01/测试医院二/测试院区2-1` + `01/CSYY2/CSYQ2-1`；随后切医院/切院区的重取见 C1 行 |
| C33 可信范围变化确认框（宿主面） | 受控构造后点选择器切医院 → 弹窗出现，标题「可信范围已变化」、正文「可信范围已变化，切换范围将放弃未保存的金额输入。」、按钮「取 消」「放弃并切换」，期间保存请求数 **0**、行内编辑态与输入保留；点「取 消」→ 弹窗关闭并进入待切换提示态，提示条「可信范围已变化，存在未保存的金额输入；确认后放弃输入并按新范围加载。」带「放弃并切换」按钮，金额入口实测 `disabled=true`。还原可信值后整页重载，提示与编辑态全部消失 |
| C34 平台页场景一（可信范围已变化且页面尚未应用） | 同一同步块内改写可信院区后回车 → **保存请求数 0**、编辑态与输入 `55.55` 保留、行内就地出现「可信范围已变化或读不到，本次不提交写入，请切换到新可信范围后重新提交。」；全文检索确认旧口径句「请刷新页面后重试」在页面内**不存在** |
| C34 平台页场景二（选定范围不同于可信默认值） | 页面范围切到 `01/CSYY1/CSYQ1-1`（≠ 可信 `01/CSYY2/CSYQ2-1`）后行内提交 → **不拦截**，保存请求照常发出并返回 200（见 C9 行），行内无拦截原因 |
| C34 医院页场景一 | 首次构造下**未拦下**（保存请求照常发出）——原因是该页用渲染期 `trustedScope` prop 比对、提交时未重新现读，与 S3-D28「保存发起前现读」不符；**已回改**为 `submitAmount` 在构造载荷前经三层命令式出口现读（RED `1 failed / 54 passed（55）`，失败断言 `expected "vi.fn()" to not be called at all, but actually been called 1 times`，与宿主缺陷同形），**第三轮复测（重启 dev 后）通过**：同构造下保存请求数 0、输入 `44.44` 保留、行内「可信范围已变化，页面尚未应用新可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。」、无旧口径句；随后写回原 `branchID` 并整页重载，两页页头、无编辑器/提示/原因全部复核干净 | 该次「未拦下」为真实缺陷证据，保留在本行作为回改依据；C34 拦首笔、C33 随后弹框的先后关系设计未规定，按「C34 拦首笔、C33 管挂起」实现并登记 |

- 数据变化（均经正常业务接口）：本节新增平台页跨院区保存 `01/CSYY1/CSYQ1-1/002504030040000-01001 = 88.88`；上节两笔仍在（`01/0101/0101001` 的 `12.34` 与 `0.00`）。本节其余交互为筛选、切换与被拦下的提交，未产生写入。

## 重开轮宿主面复测 · 第三轮（S3-D28 档二口径，2026-10-09）

### 会话与受控构造

- 受管 Chrome 中途退出，按 `browser-debugging-and-control` 的 ensure 重启（同一 profile，`dy-auth:*` 与拦截配置仍在），随后从宿主登录页重新登录：入口「检验检查结果互认平台」、账号 `yangkj`、密码取 [.agents/instructions/test-environment.md](../../../.agents/instructions/test-environment.md) 第 16 行的唯一登记处；登录后 `dy-auth:login-user` 为 `orgID:01 / hosID:0101 / branchID:0101001`（`entry-name` 同步为「检验检查结果互认平台」）。因此**本节证据身份为 `yangkj`**，前两轮为 `yangkj`（第一轮）与 `lisadmin`（第二轮与第三轮前半）。菜单重开两个页面（`medical-recognition_tab_1_v1` = 平台页、`_tab_2_v1` = 医院页），dev 服务器按取证前置重启后再开测。
- **档二受控构造（一次性页面内脚本、运行时注入、验证后立即还原、不新增生产测试入口、不打印请求头）**：构造——把 `localStorage['dy-auth:login-user'].branchID` 置为空串并**整页重载**，页面按 S3-D22 档二取该院区清单第一项、记录侧可信院区为缺失；恢复——写回 `0101001` 并再次整页重载，核对存储三值与两页页头（末次核对：`01/0101/0101001`、两页页头等于可信范围、无编辑器、无拦截原因、无弹窗与提示态）。场景一仍用「同一同步块改写 `branchID` 后直接回车」的既有构造。

### 本节取得的宿主面证据（C34 三场景）

| 场景 | 页 | 证据（2026-10-09 第三轮） |
|---|---|---|
| 场景一：可信范围已变化且页面尚未应用 → 拦 | 平台页 | 记录 `0101001`、同步块改写为 `0101002` 后回车 → **保存请求数 0**、输入 `33.33` 保留、行内「可信范围已变化或读不到，本次不提交写入，请切换到新可信范围后重新提交。」 |
| 场景一：同上 | 医院页 | 同构造（`0101001` → `0101002`）后回车 → **保存请求数 0**、输入 `11.11` 保留、行内「可信范围已变化，页面尚未应用新可信范围，本次不提交写入；请放弃未保存内容并切换到新可信范围。」 |
| 场景二：档二已应用 → 放行（本轮裁定的新场景） | 平台页 | `branchID` 置空重载后页头 `县医共体/县人民医院/总院` + `01/0101/0101001`（首项，记录侧缺失）、页脚 `共 301 项`；行内提交 `22.22` → `SaveOrganizationHospitalBranchRecognitionAmount` 体 `{organizationCode:"01", hospitalCode:"0101", branchCode:"0101001", standardProjectCode:"002504030040000-01001", currentAmount:22.22}` → **200**、收起编辑态、首格 `22.22`，行内无拦截原因 |
| 场景二：同上 | 医院页 | 同构造重载后页头 `01/县人民医院/总院` + `01/0101/0101001`；提交 `55.55` → `SaveBranchRecognitionAmount` 体 `{branchCode:"0101001", currentAmount:55.55, standardProjectCode:"002504030040000-01001"}` → **200**、收起、首格 `55.55`，行内无拦截原因 |
| 场景三：用户选定范围不同于可信默认值 → 放行 | 平台页 | 恢复可信院区后把医院切到 `测试医院一`（页头 `01/CSYY1/CSYQ1-1` ≠ 可信 `01/0101/0101001`）→ 提交 `44.44` → 三值体保存 **200**、收起、首格 `44.44` |
| 场景三 | 医院页 | 按 [Client/testPlan.md](Client/testPlan.md) 第 42 行该场景不存在于医院页，不构造 |

- 数据变化（均经正常业务接口）：平台档二 `01/0101/0101001/002504030040000-01001 = 22.22`，平台跨院区 `01/CSYY1/CSYQ1-1/002504030040000-01001 = 44.44`（覆盖本会话早前的 `88.88`），医院档二经院区-only 端点把同一行 `0101001` 覆盖为 `55.55`（前值 `22.22`）；场景一为零写入，上节的 `12.34`、`0.00` 已被本轮覆盖。
- 结论：三场景在宿主上**全部可构造并已取证**，原拟「档二按 C3 环境边界登记」的降级未被使用；C34 结果行已按三场景更新。

## 重开轮宿主面复测 · 第四轮（六条 `PendingRetest` 与 C3/C15 补证，2026-10-09）

### 取证前置与身份

- 两个后台服务曾被会话回收，均重启核对（后端 `http://localhost:15014`、子应用 `http://localhost:3008`）；受管 Chrome 中途退出，按技能 ensure 重启（同一 profile，`dy-auth:*` 与拦截配置仍在），宿主根导航后仍为 `yangkj` 登录态（入口「检验检查结果互认平台」）。**dev 服务器重启 → 硬重载宿主 → fiber 探针**按取证前置执行，探针结果：平台页 `RecognitionAmountsPlatformBoard` **13 个 prop**、医院页 `BranchRecognitionAmountsBoard` **13 个 prop**，行为证据在探针通过后开始采集。
- 工作区标签：`_tab_1_v1` = 平台页、`_tab_2_v1` = 医院页、`_tab_3_v1` = 互认项目页、`_tab_4_v1` = 标准项目目录维护页。

### 本节取得的宿主面证据

| 条目 | 宿主面证据（2026-10-09 第四轮） |
|---|---|
| C3 医院页范围选择（可信院区缺失子场景补证） | 构造与恢复同第三轮（`branchID` 置空 → 整页重载 → 写回 `0101001` 再重载）。**呈现记录**：置空前该页只渲染 1 个下拉（院区）、页头 `01/县人民医院/总院` + `01/0101/0101001`、页脚 `共 301 项`；置空重载后页头**不变**（组织、医院仍固定展示且不渲染下拉）、院区下拉选中 `总院`（该医院清单第一项）、页脚 `共 301 项`、表格 302 行、金额入口存在、无待切换提示——即「默认选中该院首项并自动加载」；恢复后复核与置空前一致 |
| C10 金额保存失败（服务端拒绝） | **受控构造**（构造与恢复见「受控失败注入记录」的 C10 行）：一次性改写保存请求体的 `standardProjectCode` 为**目录中未建立互认配置**的真实标准项目编码 `002504030050000-01001`（从「标准项目目录维护」页取码、与金额页 300 余个已配置码比对确认未配置），使服务端按「当前组织未建立该标准项目的互认项目配置」拒绝。实测：响应 `status 500, ok:false`；宿主（父文档）出现瞬态提示 **「业务拒绝：当前组织未建立该标准项目的互认项目配置。」**（拒绝文案由宿主统一展示）；**编辑态保持打开、输入仍为 `88.88`**；该请求之后**无任何列表请求**（不刷新列表）；**无成功提示**（不误报成功）；被改写的原项目行未被写入（恢复后复核仍为 `77.77`） |
| C13 写成功但刷新失败 | **受控注入**（只让列表请求失败，见「受控失败注入记录」）：医院页 `failList=1` 后行内提交 `66.66` → 注入记录器显示保存请求**未被拦截**、随后的 `QueryBranchRecognitionAmountList` **被拦截失败**；出现警示标题「写入已成功，数据待刷新」与说明「写入已经成功，但重新读取金额列表失败；已有数据保留，维护操作暂不可用。」+「重 试」按钮；**无任何保存失败提示**（`.ant-message`/notification 为空）；原 302 行与页脚 `共 301 项` 保留；编辑态已收起。**页面内重试恢复（本轮补上该子面）**：清注入 → 点「重试」→ 第二次列表请求 `passed`、警示消失、写入口恢复，首格显示 **`66.66`**（写入结果未丢失）。核对后已还原 `window.fetch` 为原生实现并删除注入标记 |
| C14 读取失败 | **受控注入**（同一注入器）：`failList=1` 后点「搜索」→ 列表请求被拦截失败；页面保留原有 302 行与页脚 `共 301 项`，出现警示标题「互认项目金额数据待刷新」与说明「读取失败，已有数据保留；维护操作暂不可用，可重试读取。」+「重 试」；**全部金额入口 `disabled=true`**；表头未被清空。**页面内重试恢复**：清注入 → 点「重试」→ 第二次列表请求 `passed`、警示消失、金额入口 `disabled=false`。注入还原核对同 C13 |
| C15 组织不可用（阻断态补证） | **受控构造**（`hosID` 置空、`branchID` 同时为空 → 整页重载；恢复写回 `0101`/`0101001` 并重载）：医院页显示阻断提示 **「可信范围不可用」+「当前无法从登录凭证确定可信组织与医院，因此不能读取或维护本院区互认项目金额。请从宿主登录后重新进入本页面。」**；**不渲染院区下拉**（`.ant-select` 数 0）、页头无范围展示、无金额入口、表格 0 行；**全程零业务请求**（iframe `performance` 资源条目中 `15014`/`35001` 计数为 **0**）。恢复后复核两页正常、无阻断提示 |
| C18 当前不可用原因（三层逐次） | 每层「停用 → 金额页重读 → 恢复」各一次，全部经「标准项目目录维护」页的业务操作与确认弹窗：**仅标准项目停用**（`停用项目：乙型肝炎表面抗原（HBsAg）` → 弹窗「停用标准项目」→ 确认停用）→ 金额页该行原因 **`标准项目已停用`**；**仅分组停用**（`分组更多操作` → `停用分组` → 弹窗「停用分组」→ 确认）→ 原因 **`所属分组已停用`**；**仅分类停用**（`分类更多操作` → `停用分类` → 弹窗「停用分类」→ 确认）→ 原因 **`所属分类已停用`**；三层全部恢复后重读 → 该行原因**为空**，且全表**含「已停用」原因的行数为 0**（分类行文本复核为「检验专业启用」、分组与项目按钮复核为启用态） |
| C19 配置自身停用 | 经「互认项目」页对 `002504030040000-01001` 点 `停用配置…（当前启用）` → 弹窗「停用互认项目配置」→ 确认停用（按钮变为 `启用配置…（当前停用）`）；金额页重读该行：金额 `66.66`、金额状态 `已配置`、**互认配置状态 `停用`**、**当前不可用原因为空**（8 列中末列空串）。取证后已恢复（`启用配置` → 弹窗「启用互认项目配置」→ 确认启用） |
| C20 停用状态下仍可维护金额 | 承上条的停用态，行内入口**未被禁用**（`disabled=false`），提交 `77.77` → `SaveBranchRecognitionAmount` 体 `{branchCode:"0101001", currentAmount:77.77, standardProjectCode:"002504030040000-01001"}` → **200** → 编辑态收起并整表重载；该行变为金额 `77.77`、金额状态 `已配置`、**互认配置状态仍 `停用`**、**原因列仍为空**（状态与原因列不因保存而改变） |

- 构造与恢复的统一核对（末次）：`dy-auth:login-user` 为 `01/0101/0101001`、探针键已删除；两页页头等于可信范围、无编辑器、无 `.ant-alert`、含「已停用／待刷新／不提交写入」的行数为 **0**；目标行两页均为 `检验 / 检验专业 / 传染病检验 / 002504030040000-01001… / 77.77 / 已配置 / 启用 / 空原因`；`window.fetch` 已还原为原生实现、注入标记与观测器全部拆除。
- 数据与目录状态变化（均经业务接口）：目标行金额最终 `77.77`（C20 写入，前值 `66.66`，C13 也写过一次 `66.66`）；互认配置、标准项目、分组、分类四者均**已恢复启用**（本节只做了停用→启用往返）；C10 的两次拒绝请求零写入；未改任何数据库行。

## 验证与静态检查（票 10 末段）

- **后端**：`dotnet test server/Dy.MedicalRecognition.slnx` → **833 通过 / 0 失败**（基线 819）。首轮失败 1 条 `Architecture/SourceGuardReuseTests.Architecture_guards_reuse_the_shared_source_guard`——阶段 3 判据迁入新建的 `Stage3ApiContractTests.cs` 后，共享语法工具的受治理清单未登记该新消费方，按断言含义补登记后复跑全量 **833 通过 / 0 失败**；迁入当时另有 `--filter Stage2EnumContractTests|Stage3ApiContractTests` 的先行复跑 **34 通过 / 0 失败**。测试与运行中的后端宿主互斥（MSB3021/MSB3027 文件锁定），执行口径为停宿主 → 测试 → 重启宿主。
- **子应用**：`pnpm -F dy-medical-recognition exec vitest run src/pages/recognitionAmounts` 末段终值 **180 通过 / 0 失败（3 个文件）**（承前：适配层 65 → 69，平台页 48 → 50 → 52，医院页 51 → 55 → 59）。全子应用 `pnpm -F dy-medical-recognition exec vitest run` 在两项设计回改完成后曾取 **538 通过 / 2 跳过 / 0 失败（21 个文件）**；医院页 C34 提交期现读补正（+1 条用例）后的首轮全量为 **537 通过 / 2 失败 / 2 跳过**，两条失败均在 `src/pages/standardCatalog/StandardCatalog.test.tsx`，且**都是 `Error: Test timed out in 20000ms`（无断言失败）**：`C69 写成功后部分集合刷新失败 > 写成功即关弹窗…` 与 `新增项目弹窗分组可用性提示 > 所选分类只有停用分组时给出提示…`。
  - 归因（有证据）：同期本机有另一进程在跑 `gitnexus analyze --max-file-size 3072`（子进程 `--max-old-space-size=48545`，并在 `.claude/skills/*.md` 生成新的技能索引），全量运行的耗时随之整体抬高（`import` 157s vs 此前 77s、`environment` 144s vs 81s、总时长 145s vs 99s）；`git status` 显示 `src/pages/standardCatalog/` 及 `src/shared/`、`src/hooks/` **无任何改动**，本票改动只落在 `recognitionAmounts/` 与 API Client 四个文件。
  - 复测口径与终值：同文件 `--testTimeout=60000` 单跑 → **45 通过 / 2 跳过 / 0 失败（exit 0）**；随后按默认超时重跑全子应用 → **21 个文件、539 通过 / 2 跳过 / 0 失败（exit 0，总时长 90s，与并行索引前持平）**，两条超时用例恢复通过，确认为负载所致而非行为失败。S3-D28 档二回改（适配层 69、平台页 52、医院页 59）落地后再次全量并连同静态检查一起复跑：`tsc -p tsconfig.app.json` exit 0、`lint` exit 0、`build` exit 0、`git diff --check` exit 0，全子应用 **21 个文件、549 通过 / 2 跳过 / 0 失败（exit 0，总时长 74s）**。终值口径：全子应用 **549 通过 / 2 跳过 / 0 失败**，金额目录 **180 通过 / 0 失败**。
- **静态与构建**：`tsc -p tsconfig.app.json`、`eslint`（`pnpm -F dy-medical-recognition lint`）、`pnpm -F dy-medical-recognition build`、仓库根 `git diff --check` 在每轮回改后逐条执行，均 exit 0；最后一次 `git diff --check` 的空白检查无错误（`LF→CRLF` 提示为工作区既有文件的换行提示，含并行索引生成的 `.claude/skills/*.md`，非本轮改动、未回退）。

## 未覆盖面与残余风险

- **无法构造的面（逐条）**：① 可信上下文缺失形态（"可信院区缺失""组织或医院缺失"）**分两层区分**：**(a) 宿主签发的真实身份不可构造（服务端面，登记不变）**——环境不存在 `branchID`/`orgID`/`hosID` 为空的测试账号，也不伪造宿主签发的令牌；**(b) 页面侧可信上下文的缺失形态按已建立的受控注入纪律可构造（本层 2026-10-09 已取证）**——改写 `dy-auth:login-user` 的对应字段后整页重载、验证后写回并复核，一次性页面内脚本、运行时注入、不新增生产测试入口，构造与恢复步骤逐条记入「重开轮宿主面复测 · 第三轮」「第四轮」两节，C3 与 C15 因此由 `Blocked`/`NotRun` 转 `Passed`；② "请求院区不属于可信医院"的提交路径（医院页院区下拉只列可信医院下的院区；本轮在 `yangkj` 下键入 `CSYQ2-1` 后选项集为空、已选值不变、回车不提交且不触发请求）；③ 原"该组织无互认配置"的空结果面已在「补充取证」轮构造并取得证据（删除组织 `01` 的 5 条互认配置后，列表请求返回 200 与空集合、页面显示空态），该缺口已关闭；④ 真实并发同步屏障（双标签页提交曾被串行化为插入+更新，未观察到唯一约束冲突）；⑤ 组件加载失败注入（组织服务选项请求失败无法在宿主重开子应用时命中注入点）；⑥ 平台管理员入口的跨组织真实宿主面（环境只有 1 个组织，按 S3-D20 登记）。以上条目已按环境边界接受并逐条登记（见「已确认的验证降级」节）。
- **业务拒绝的响应形态（框架行为，已接受）**：业务拒绝返回 **500**，响应体形态由运行时环境决定——Development 由 ASP.NET Core 的 `DeveloperExceptionPageMiddleware` 输出异常文本与调用堆栈，非 Development 不注册该中间件，异常直接上抛为 500。本项目与框架都没有注册全局异常处理器，本项目页面按设计不处理错误、也不自建异常管道；宿主侧经 `ErrorHandlerMiddleware` 上报的事件始终含状态码与消息，故宿主提示不依赖响应体内容。该行为两个项目口径一致，接受，不列为待办项。
- **契约面风险（本阶段遗留）**：`currentAmount` 在生成客户端里是未定型节点（`UntypedNode`，`{ value, getValue() }`），适配层按结构取值与写值；`currentAmount` 的契约类型修复属生成管线范围。读模型的枚举中文已由 S3-D21 交付（`ItemTypeText`、`ConfigurationStatusText`），前端不再本地映射；生成端把这两个只读计算属性声明为可空，与阶段 2 同形。
- **跨组织真实宿主面**（S3-D20）：环境只有 1 个组织（本轮只读回读确认），V29 的真实宿主层永远无法在本环境取得证据；该缺口按 S3-D20 登记为已接受的环境边界，不再作为阻塞项。
- **可空枚举筛选形态**（S3-D14）：本阶段页面不下推枚举筛选、不手改生成物；生成管线侧状态不由本阶段管理。
- **不分页**（S3-D8）：按 `AcceptedRisk` 登记，重审条件为出现可复现的卡顿、超时或内存问题。
- **组件既有行为**：`BaseScopeSelector` 在非受控时自动选中首级第一项、加载失败使用 antd 静态 `message.error` 且无错误回调 prop 或错误回调导出；本阶段不改动组件源码，页面以受控范围值规避自动写入。
- **受控注入的还原链脆弱**：本轮第二次注入把原引用绑定到已包装的 `fetch`，导致还原后页面请求失效，只能靠整页重载恢复。后续受控注入应只在未包装的 `window.fetch` 上取原引用，或在每次注入后以 `window.fetch.toString()` 是否含包装特征反向校验。
- **已裁定项 1：新增的用户服务生产配置条目 → 已撤回**。`server/Dy.MedicalRecognition/appsettings.json` 中新增的 `Dy.Base.Application.Contracts.UserAggregate.IUserAppService` 外部服务代理条目已删除，该条目只保留在 `appsettings.Development.json`。处置理由：本项目既有的用户、组织、系统参数、字典四个框架服务代理都只登记在开发配置中，生产配置不带开发环境外部地址；生产部署配置不属本阶段交付范围。
- **已裁定项 2：`TrustedOrganizationResolver.ResolveHospitalOrThrow` 死代码 → 已删除**，仅覆盖该方法的 3 个测试用例（共 5 条测试数据）同步整条删除。处置理由：阶段 3 四个金额入口已改用 `TrustedScopeResolver`，该方法无生产调用点，其"只读令牌声明、不做用户档案回落"的语义正是医院管理员入口 500 缺陷的成因。
- **重开轮新增的环境阻断（2026-10-08，已解除）**：V31（关键字四列匹配）、V32（关键字缺失或空白）的真实库与真实入口面曾因外部宿主 `183.224.180.166` 的 35000、35001、15432 不可达而无法执行，按 `Blocked` 登记；2026-10-09 环境恢复后已按原口径复测并转为 `Passed`，取证明细见结果表该两行，重开轮后端取证至此完成。
- **待负责人裁定的设计补写**：非空白关键字整体去首尾空白的口径目前由实现与用例冻结，`Server/design.md` 第 25 行只写「缺失或去首尾空白后为空视为未提供」；是否把非空白取值的去首尾空白补写进该行由负责人决定，未获批准前不修改设计文档。

## 观察到的异常与疑似缺陷

以下为执行中发现的现象，按可复现步骤记录，未做修饰。第 1-3、7 项为历史观测，第 4-6、8-9 项为本次取证观测。

1. **医院管理员页入口 500（已修复，影响 C26 判定，历史观测）**：上一轮从宿主菜单进入「本院区互认项目金额」即触发 `POST http://localhost:15014/Api/MedicalRecognitionReportQuery/QueryBranchRecognitionAmountList` 返回 500，Console 出现 error 级消息。根因与修复语义见「本轮修复与回归事实」。本轮同一操作返回 200、Console 无 error 级消息；该缺陷未再现。开发环境异常页仍会把完整堆栈返回给客户端。
2. **切换标签页不自动重载（历史观测，本轮未针对该行为取证）**：在「互认项目金额维护」页选中范围并加载完成后，切到「标准项目目录维护」修改目录状态，再切回金额页，列表仍是上次渲染的数据；单击一次「重新读取金额列表」后新原因文案才出现。
3. **一次列表请求因数据库连接被远端关闭返回 500（历史观测，本轮未复现）**：`Npgsql.NpgsqlException … SocketException (10054): 远程主机强迫关闭了一个现有的连接`，用完全相同参数重试后返回 200。本轮跨两个身份、两个页面数十次请求**未再出现任何 5xx**。
4. **本轮写库记录（经正常业务入口形成，开发环境保留、不清理）**：`mrec_organization_hospital_branch_recognition_amount` 当前 **11 行**。本轮新增 3 行：`CSYY2/CSYQ2-2/123 = 55.55`、`0101/0101001/222 = 37.37`，以及 `CSYY2/CSYQ2-1/123` 的同值重复保存把 `oper_time` 由 `00:35:47.841632` 更新为 `02:15:03.621658`（金额仍 `66.66`，不是新增行）。其余行来自更早轮次（`0101/0101001/123 = 12.34`；`CSYY1/CSYQ1-1` 上 5 行；`CSYY1/CSYQ1-2` 上 2 行；`CSYY2/CSYQ2-1/123 = 66.66`）。
5. **目录与配置状态复核**：标准目录启用项目 5 个，唯一未配置的目录项自身为停用。组织 `01` 的 5 条互认配置在复验轮只读复核时为 `is_valid` 全 `true`；随后在「补充取证」轮被负责人删除以构造无配置状态，该表当前为空。
6. **互认配置被删除以构造无配置状态（本轮观测）**：组织 `01` 的全部 5 条互认配置经负责人确认为开发阶段测试数据后删除（未保留备份），删除后金额页可达行集为空。该状态用于取得保存拒绝面与空结果面（见「补充取证」节）。**重开轮已按正常业务路径重建 3 条配置**（`123` 启用、`222` 停用、`S1-D29-ITEM-20260915-001` 启用），金额页当前为 3 行，空结果面不再是当前环境状态；需要恢复空态时由负责人按同一路径调整配置。
7. **宿主菜单子项数量与执行约定不一致（低影响，历史观测）**：执行约定描述「检验检查结果互认」下有 5 个子项，实测该菜单展开后为 **4** 项（`标准项目目录维护`、`互认项目`、`互认项目金额维护`、`本院区互认项目金额`）。本阶段需要的两个菜单项均在其中且可达。
8. **宿主不提供子应用路由的顶级直达（本次取证观测）**：在宿主新标签直接打开 `http://183.224.180.166:35000/subApps/medical-recognition/branch-recognition-amounts` 返回宿主服务器的 `404 - 找不到文件或目录。`。该路径只在宿主的子应用 iframe 内由 micro-app 路由渲染。记录以备核对直达类验收手段：不能以顶级直达替代宿主菜单链路。
9. **业务拒绝以 HTTP 500 返回，Development 下带开发环境异常页（本次取证观测，属框架行为）**：删除互认配置后的保存拒绝返回 **500**、`content-type: text/plain`，响应体为异常文本与完整调用堆栈（`DeveloperExceptionPageMiddleware` 输出）。该中间件由 `WebApplication` 只在 Development 环境自动插入管道，非 Development 不注册；本项目与框架都没有注册全局异常处理器，`Dy.LisCenter/server` 同样没有，且其架构测试 `ExceptionPipelineArchitectureTests` 明文禁止生产代码出现 `ProblemDetails`/`ExceptionFilter` 等自建管道。页面上的 `业务拒绝：…` 文案由宿主统一提示处理：本项目两个页面按设计不处理错误（源码注释为"错误由宿主统一展示"），`@dy/kiota-middleware` 的 `ErrorHandlerMiddleware` 经 `createAuthenticatedAdapter()` 装配后会把任何非 2xx 的 `statusCode`/`message`/`responseBody` 等字段上报宿主，宿主因此始终有可提示内容。该行为属框架行为，两个项目口径一致，本项目不自建异常管道。

## 结论

阶段 3 取得 **48** 条用例的完整证据（`Passed`），无确认失败，**4** 条受阻（V29、V30、C3、C11），**4** 条未执行（V16、C15、C22、C23）；另有 V27 的事件可见性子面经范围确认不适用（记 `N/A` + `AcceptedRisk`，作为子面不单独计入合计）。

阶段结论有三项。其一是**修复未引入回归**：两个身份都从宿主登录页重新登录，两个页面首屏加载正常、Console 无业务错误、全量请求无 5xx，四处真实写入（两处新增、一处覆盖同值、一处零元/未配置呈现）与 dbx 回读一致；六项修复的直接影响面逐一在真实页面或真实库上得到确认（F1、F2、F3、F6）。其二是**报告归因已修正**：原先 25 条 `NotRun` 中有 **20** 条的层级不需要真实数据库且已有指向该用例边界的具体断言，改按 `Contract`/`Domain`/`Application`/`Query`/`Static`/`Component` 各自层级记录证据，并逐条写明复用条件与未覆盖面；`V6` 据修复后的契约断言转正。其三是**无互认配置的状态已构造**：删除组织 `01` 的全部互认配置后取得保存被拒绝面与空结果面，使 `V11`、`V20`、`V27`、`C10`、`C17` 五条转正；`V16` 与 `C15`、`C22`、`C23` 保持 `NotRun`。

**2026-10-08 重开三页统一轮（2026-10-09 收口，负责人裁定）**：矩阵扩为后端 V1-V33、前端 C1-C36（总分母 69），终值 `Passed` 64 / `Failed` 0 / `Blocked` 3（V29、V30、C11）/ `NotRun` 2（V16、C23）/ `PendingRetest` 0，剩余 5 条全部是已登记的环境边界，无可继续取证的条目。重开轮结论有三项。其一是**三页款式统一已验证**：两个金额页与「互认项目」页同款式（页头三级范围两行、院区恒定单值、筛选区与地址栏参数、四栏层级列视图、行内金额编辑），后端两个金额查询的关键字下推（四列匹配、`%`/`_` 转义、空白视为未提供）经 V31-V33 取证。其二是**状态机制两道防线按裁定落地**：C33 可信范围变化确认框与 C34 提交前现值复核均经组件面与宿主面取证（含受控构造的 token 重签与档二缺失形态），C34 比对口径为方案甲（比对可信范围的变化，不是用户选定范围与可信默认值的差异），档二下院区层按缺失值参与比对、两侧同为缺失即放行，「现读不到」收窄到可信组织或医院。其三是**挂起与边界条目全部处置**：六条 `PendingRetest`（C10、C13、C14、C18、C19、C20）宿主复测通过，C3、C15 经受控构造补证由 `Blocked`/`NotRun` 转 `Passed`，原收口轮 8 条环境边界中的 C3、C15、C22 已解除，剩余 5 条按环境边界接受。全量验证终值：后端 **833 通过 / 0 失败**，子应用 **549 通过 / 2 跳过 / 0 失败**，金额目录 **180 通过**，`tsc`/`lint`/`build`/`git diff --check` 全 exit 0。

剩余缺口集中在三处：一是两条安全边界的构造能力（非可信医院院区提交、可信院区缺失上下文）在本环境的正常页面路径上不可达，使 C11 的第二类前置、V30 的拒绝面、C3 与 C15 缺宿主层证据；二是真实并发同步屏障（V16）与实际业务规模数据（C22）在本环境不可构造；三是"组件加载失败"的宿主层构造仍未命中（C23），受控注入还暴露了还原链脆弱问题。此外跨组织宿主面按 S3-D20 为已接受的环境边界。上述条目均未记为通过，已按环境边界接受并逐条登记（见「已确认的验证降级」节）。阶段 3 的验证矩阵至此无可继续取证的条目。收口时点此前列出的两项部署前事项均已闭合或按框架行为接受：子应用生产配置 `public/config.json` 已携带 `baseApiBaseUrl`（`http://183.224.180.166:35001`），其 `apiBaseUrl` 登记为部署占位地址 `http://183.224.180.166:35014`（尚未部署）；业务拒绝的响应形态属框架行为，两个项目口径一致，本项目不自建异常管道（见「未覆盖面与残余风险」）。两者均不改变本阶段已取得的验证结论，也不属本阶段矩阵的验证面。本报告不把设计决策标签计入测试统计。
