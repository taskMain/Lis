# 阶段 2 前端实施

## 批次与依赖

| 批次 | 范围 | 主责与文件所有权 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| F1 | 累计Client生成 | 前端主责：api-client-medical-recognition包 | 后端契约稳定，生成输入和累计清单核对；S2-D12涉及的组织契约已确定 | C22、C24 | 已完成：稳定 OpenAPI 22 path（净增 1 个查询端点；**批次10/11 后为 23 path**，见 F7/F8），组织字段差异已消除，`/auth/login` 保持排除，手写 `src/index.ts` 未被覆盖；包 `typecheck`/`build` 与子应用 `build` 均通过；C22/C24 记 `Passed`（仅契约与构建面） |
| F2 | 页面和适配层 | 前端主责：dy-medical-recognition子应用 | F1；组织接入S2-D12解除后才接真实上下文，独立组件可先准备 | C1-C3、C6、C8、C10-C11、C13-C21 的 **Component 层**（`testPlan.md` 层级定义：C14/C15/C16/C18/C19 仅 Component，C4/C6/C8/C13/C17/C20/C21 为 Component/Host，Host 层归 F3）、C22 的 Static/Build 层、C23 的 Component/Typecheck 层、C24 的 Static 层 | 已完成（组件/构建层）：契约无关基线 + **路由与真实接线**已交付——`routes.tsx` 注册 `recognition-projects`；5 个 I/O 出口全部改为生成端点调用（查询/新增/修改/启停 + 复用阶段1 有效目录查询）；契约收敛完成（`pendingIntegration` 全仓零残留、镜像类型改引用生成类型）；组织接缝按结案口径接入 `LoginUserManager.getOrgID()` + `useAuth()`。**配置状态筛选不下推**：这是业务口径而非契约限制——下推会缩小读取范围、破坏新增排除集合的完整性（批次10 后契约已可表达该枚举，见 F7），页面按设计在本地筛选。测试本轮的门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节；独立复核见 [审查-Ticket07接线复核.md](审查-Ticket07接线复核.md) |
| F3 | 宿主验收 | 前端测试主责：本阶段测试及证据 | 阶段1收口确认、本阶段建表、后端与前端 dev server、菜单身份、页面路由与接线（均已取得） | C4-C9、C12-C13、C17、C20-C21的Host部分、C25 | 已完成：C4/C5/C6/C7/C8/C9/C12/C13/C20/C21/C25 已在真实宿主执行（两页面并发重复、写成功刷新失败为 2026-09-15 补做）；**组织切换后目标组织读取失败（C17）的真实宿主页面层未执行**，按 Testing Baseline §3 整条记 `Blocked`，采纳组件层自动化证据，受阻原因登记为决策标签（接受口径待负责人确认）。**2026-09-16 批次A/B 整改后**，受影响条目的宿主证据已按 Testing Baseline §5 重取（见 `testReport.md` 的"批次A/B 整改后的宿主重取"）；逐条状态与统计见该报告的"结果"节 |
| F4 | 审查整改（批次B） | 前端实现主责 + 主线程交叉审查 | 独立审查 finding（整改范围为 A、B 两组） | 与 F2/F3 相同编号（整改不新增 C 编号） | 已完成（2026-09-16）：8 项整改落地——状态文案/色板/行名/类型映射收敛到适配层唯一出口、目录就绪门控、时长补 `int.MaxValue` 上界、`rowKey` 稳定唯一、函数式 `setFilter`、提交中禁止取消/关闭并补 loading 归零断言、路由注释与「重置编码与状态筛选」名称、三个弹窗拆入 `RecognitionProjectModals.tsx` 并去重时长字段与提交逻辑。**主线程复核另修复 2 处**：B2 的入口门控实际未实现（`disabled` 仍是 `!canWrite`）、B4 使用了 antd 已弃用的 `rowKey` 索引参数。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节 |
| F6 | 第三轮复审后的修复（批次D 前端、批次E 文档与口径） | 主线程 | 第三轮复审完成、批次C+D+E 全部执行 | 与 F2/F3/F4 相同编号（修复不新增 C 编号） | 已完成（2026-09-16）：①`RecognitionProjects.tsx` 行内动作/状态文案改由适配层唯一出口 `configurationStatusActionText` / `configurationStatusText` 构造（删掉页面自拼的 `CONFIGURATION_STATUS_TEXTS[2]/[1]` 与 `'配置状态未知'` 字面量）；②`recognitionProjectsApi.test.ts` 补 `configurationStatusActionText` 双分支与"与状态文案同源"断言，`RecognitionProjects.test.tsx` 的 C8 用例补"行内可访问名称与适配层出口同源"断言；③删除与既有用例重复的 B2 用例（其首帧断言在新旧判据下同为禁用、无区分力）；④「立即切换」改「放弃并切换」并同步用例；⑤`recognitionProjectsApi.ts` 筛选注释改为与服务端"字面包含（区分大小写）"一致；⑥页面状态机文档补 `catalogLoaded` 门控与"目录为空仍就绪"口径，`Client/design.md` 与 `Ticket 04` 写明"筛选由页面本地执行、不下推"。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节；页面渲染改动已按 Testing Baseline §5 在真实宿主重取（两查询 `200`、5 行、动作按钮可访问名称与唯一出口一致） |
| F7 | 跨阶段枚举契约缺陷修复（客户端侧） | 主线程 | 口径参照同组织 `Dy.LisCenter` | 与 F1/F2/F6 相同编号（契约修复不新增 C 编号；C22/C23/C24 契约与生成物面加强） | 已完成（2026-09-16）：`scripts/prepare-openapi.mjs` 新增 `oneOf [null, $ref]` → `$ref` 归一（与整数联合修正并列，计数分别输出）；重跑生成入口（**批次10 当时 22 path**、修正整数联合 3 处、修正可空引用 3 处；批次11 新增枚举元数据端点后为 23 path、整数联合 4 处）并重生成 Kiota 客户端（1.34.1，`--exclude-path /auth/login`、`kiota-lock.json` 指向包内稳定 OpenAPI）；`configurationStatus`/`itemType` 由空对象类型改为 `number \| null` 并走 `writeNumberValue`，页面与适配层无需改动即通过全部前端用例；适配层关于枚举契约的注释按修复后事实更新，筛选不下推的口径改为纯业务口径。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节 |
| F8 | 枚举中文来源（LisCenter 同口径，客户端侧） | 主线程 | 口径参照 `Dy.LisCenter` 的 `useEnumMetadata` | 与 F1/F2/F6/F7 相同编号（中文来源属契约交付方式，不新增 C 编号；C22/C23 契约与渲染面加强） | 已完成（2026-09-16）：hook 按 API Client 实例 + 枚举名缓存请求、失败清除缓存以便重试、加载失败保留兜底选项；页面展示文本改取契约文本（本地常量降级为兜底），配置状态筛选的选项标签与启停动作文案同源取自元数据；新增 hook 用例与页面用例（契约文本不被本地常量覆盖、只读资料与表格同源）。门禁数字与新增用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节（前端改动文件在 git 中均未被跟踪，不存在可用于逐条归属的差异基线，不按本行的描述推算分母或增量）；宿主实测页面加载即发起 `POST /Api/EnumMetadata/GetEnumMetadata`（`{"enumName":"ConfigurationStatus"}`）→ `200` + 启用/停用，筛选切「停用」为 0 行、重置后 5 行且类型「检验」、状态「启用」，见阶段 `testReport.md` 的"枚举中文来源（LisCenter 同口径，2026-09-16）" |
| F9 | 增量复审整改（第 1 轮，前端侧） | 主线程 | 增量复审完成；逐条复核成立性 | 与 F1/F2/F4/F6/F7/F8 相同编号（复审整改不新增 C 编号） | 已完成（2026-09-16）：①兜底选项与收窄函数 `toConfigurationStatusFilterOptions` 下沉到适配层并补 3 条单测，页面不再自持"取值 + 中文"；②`useEnumMetadata` 改为按枚举名与 API Client 键控 state（换名/换客户端不残留）、缓存清除加身份校验，hook 用例覆盖缓存层与 `renderHook` 驱动两层（成功替换／空集合保留兜底／失败后重挂载重试／两实例复用请求／换名不残留并支持换回／卸载后迟到响应不泄漏），随后一轮再补「换名后迟到响应不回退已显示值」；③页面测试的 client 改为按用例新建并显式提供枚举元数据桩（默认拒绝），新增"元数据文案与本地兜底刻意不同"的页面用例，断言筛选标签、行内可访问名称与启停弹窗标题；④`Segmented` 的 aria-label 注释按依赖源码与宿主可访问性树改正，并改为按可访问名称定位；⑤适配层"枚举是空对象类型"的旧根因注释改为业务口径；⑥`configurationStatusActionText` 从死出口改为启停动作标签的生产兜底出口。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节（前端改动文件在 git 中均未被跟踪，不存在可用于逐条归属的差异基线，不按本行的描述推算分母或增量）；随后 F10/F11 两轮整改补充守卫与断言，终值见阶段 `testReport.md` 的"门禁终值"节 |
| F10 | 增量复审整改（第 2 轮，前端侧） | 主线程 | 增量复审完成；逐条复核成立性 | 与 F1/F2/F4/F6/F7/F8/F9 相同编号（复审整改不新增 C 编号） | 已完成（2026-09-16）：`useEnumMetadata` 返回值改只读并去掉冗余 `?.`；补“换名后迟到响应不得回退已显示值”用例（删 `cancelled` 守卫即红）；`recognitionProjectsApi` 的收窄与色板改用具名状态常量；C8 用例注释收窄为“方向与名称结构”并把服务端文案结论指向元数据用例；测试文件内 `describe/it` 分行。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节（前端改动文件在 git 中均未被跟踪，不存在可用于逐条归属的差异基线，不按本行的描述推算分母或增量） |
| F11 | 增量复审整改（第 3 轮，前端侧） | 主线程 | 增量复审完成；逐条复核成立性 | 与 F1/F2/F4/F6/F7/F8/F9/F10 相同编号（复审整改不新增 C 编号） | 已完成（2026-09-16）：`RecognitionProjects.test.tsx` 的 C8 用例注释方向改正（目标为停用）；`useEnumMetadata` 的 JSDoc 明确"选项对象为共享引用，不得就地修改"（数组只读不冻结元素）。本轮只改注释与文档，未新增用例。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节 |
| F12 | 复审后整改（前端侧：写入门控与用例） | 前端实现主责（写入门控）+ 主线程 | 复审完成；逐条复核成立性 | 与 F1/F2/F3/F4/F6/F7/F8/F9/F10/F11 相同编号（复审整改不新增 C 编号；不改矩阵用例定义与编号） | 已完成（2026-09-16）：`RecognitionProjects.tsx` 的写提交在发起请求前以可信组织复核（`submitWrite`：可信组织缺失、提交时组织与可信组织不一致、页面已应用组织与可信组织不一致时直接不提交）；`RecognitionProjectModals.tsx` 的三个写弹窗按 `RecognitionProjectWriteGate` 与 `writeBlockedReason` 在组织未对齐、读取失败、读取在途时禁用确定按钮并给出原因。**先取得失败断言（三条）**，实现后转为通过；新增用例（条数与归属见阶段 `testReport.md` 的对应轮次小节）。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节。详见阶段 `testReport.md` 的"复审后整改（2026-09-16，批次16 / B15 / F12）" |
| F13 | 第二批整改（页面状态、用例与证据口径） | 主线程（前端 `client/apps/dy-medical-recognition/src/**` 的页面、hook 与用例）；阶段2 文档 | 第一批文档整改完成后的第二批代码整改；逐条复核成立性 | 与 F1/F2/F3/F4/F6/F7/F8/F9/F10/F11/F12 相同编号（整改不新增 C 编号；不改矩阵用例定义与编号） | 已完成（2026-09-16）：①`useEnumMetadata` 新增 `enabled` 开关，页面只在可信组织可用时读取枚举元数据（组织范围不可用时页面整体为阻断分支，不发起该请求），缓存身份仍为「API Client 实例 + 枚举名」；②修改可互认时间弹窗改持配置标识、按当前行集重取当前行，后台刷新交付新值时弹窗展示与表单回填都取当前行（代价是同时覆盖该弹窗中尚未提交的输入）；③表格行标识唯一性改按 rc-table 渲染的 `data-row-key` 实际值断言，不再依赖 React 的重复 key 告警文本；④页面与 hook 的在途请求边界按事实登记：生成客户端无取消入口（`RequestConfiguration<T>` 无 `AbortSignal`），只按请求序号作废结果、不中止在途请求。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节。详见阶段 `testReport.md` 的"第二批整改（2026-09-16，批次17 / B16 / F13）" |
| F14 | 第三批整改（提交期现值复核、启停弹窗取值与页面口径） | 主线程（前端 `client/apps/dy-medical-recognition/src/pages/recognitionProjects/**` 的页面、弹窗与用例）；阶段2 文档 | 第二批代码整改完成后的第三批整改；逐条复核成立性 | 与 F1/F2/F3/F4/F6/F7/F8/F9/F10/F11/F12/F13 相同编号（整改不新增 C 编号；不改矩阵用例定义与编号） | 已完成（2026-09-16）：①可信组织**来源不变**（`@dy/auth` 的 `LoginUserManager` 的当前登录组织，与同组织参照项目 `Dy.LisCenter` 一致），**提交时改读现值**——`recognitionProjectsOrganizationScope.ts` 新增命令式 `readTrustedOrganizationCode()`（与渲染期 `useTrustedOrganizationScope` 共用同一实现），`submitWrite` 在发起请求前读现值可信组织并与页面已应用组织比对，不一致或读不到即**零写请求**，并把原因交给写弹窗显示（覆盖"宿主重新签发 token 到页面重渲染之间"的窗口）；②删除 `appliedOrganizationRef` 的第二来源写法，该引用只作 `appliedOrganizationCode` 的同步镜像、仅在 `applyOrganization` 一处赋值；③启停弹窗改持配置标识、按**当前行集**重取目标行与目标状态（行不存在或状态未知时不渲染）；④阻断原因次序调整为组织未对齐（含提交期复核失败）优先于读取失败与读取在途，`submitting` 不再掩盖更强的阻断原因；⑤页面 `Alert` 改用 `title`；⑥"未保存内容"按**实际渲染**判定（修改弹窗目标行已离开当前行集时不渲染、不承载内容）。门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节；本批为行为变更但未重取宿主证据（触发分支在真实宿主不可构造，与 C17 的 Host 面同一环境边界），证据为组件层用例。详见阶段 `testReport.md` 的"第三批整改（2026-09-16，批次18 / B17 / F14）" |
| F15 | 第三批整改后的宿主链路验收（菜单进入、首次加载与组织编码、列表渲染与文案、修改可互认时间、启停、编码与状态筛选口径、读取失败门控与重试、写弹窗门控放行） | 测试主责：本阶段测试与证据（本轮无前端代码改动） | 批次F14 完成；宿主菜单与身份、后端与前端 dev server 均已具备 | 沿用 V1-V27 / C1-C25，不新增编号 | 已执行（宿主验收，详见阶段 testReport.md 对应小节） |

备注：批次号 `F5` 未使用（历史规划号，保留空号）；上表列序与表头一致；本表只登记批次状态与矩阵编号，实际结果归阶段 `testReport.md`。

## 生成与适配边界

- 使用既有 API Client 生成流程，生成后核对手写入口和累计接口范围。
- 生成范围累计保留阶段1已交付 F01，加入阶段2稳定契约，排除后续功能和 `/auth/login`。
- 生成后核对公开入口、创建/查询 Request 的组织字段要求、ReadModel、可空性和枚举。
- 适配层不创建 Client、不读取 token、不解析平台错误响应；负责组织上下文、请求参数和 DTO 到视图模型的转换。
- **前端产物事实**：产物由 `pnpm -F dy-medical-recognition build` 产生，不入版本控制——`client/.gitignore` 第 2 行的 `dist/` 规则覆盖 `client/apps/dy-medical-recognition/dist/`（`git check-ignore -v` 实测命中，`git ls-files` 不含该目录）。最近一次构建产物为 `dist/assets/index-LKCmei7L.js` 1,234,435 字节（约 1.2 MiB）、gzip 后 380,601 字节（约 372 KiB），构建时间 2026-09-16 17:27；另有 `dist/assets/index-Ct29yX2O.css`、`dist/index.html` 与两个配置文件。产物由构建产生、**不随阶段2 变更提交**；是否把产物纳入版本控制不在本阶段处置，本阶段未改 `.gitignore` 与 `vite.config.ts`。

## 页面状态实现边界

- 进入页面自动加载；支持手动刷新，加载期间禁用写操作。
- 组织切换取消旧请求并清空旧数据、选择和弹窗；未保存内容先确认。目标组织加载失败时阻断操作，不误用旧组织范围。
- 同组织查询失败保留已有有效数据并允许重试；新增、修改、启停失败保留必要输入和弹窗状态。
- 同值修改照常提交；写成功与后续刷新失败分别记录和展示，不能把刷新失败改判为写失败。
- 不分页与重审条件引用 [S2-D7](../design.md)。

## 宿主入口与验收路径（阶段信息，不在整体测试规范维护）

- **宿主菜单**：父项「检验检查结果互认」→ 子项「互认项目」（2026-09-15 已在宿主维护）。
- **子应用路由**：`recognition-projects`，宿主完整路径 `/subApps/medical-recognition/recognition-projects`；该路由已随 Ticket 07 注册（`client/apps/dy-medical-recognition/src/router/routes.tsx`），并有 `routes.test.ts` 覆盖。
- **验收路径**：宿主入口（宿主地址、登录页与登录入口）是**跨阶段统一的项目级配置**，见 [Test Environment](../../../../.agents/instructions/test-environment.md) 第 1、2 节，本阶段不重新定义；链路为：从该宿主入口登录 → 在宿主 `/dev-settings` 确认符合 [Test Environment](../../../../.agents/instructions/test-environment.md) 第 4 节的劫持映射已生效（Network 确认子应用资源来自 `localhost:3008`、Console 出现 `[vite] connected.`）→ 以 debug 启动前后端（`localhost:3008` / `localhost:15014`）→ 从宿主菜单进入本页面 → 按 [Testing](../../../../.agents/instructions/testing.md) 第 1、2 节在同一链路核对页面、DOM、Console、Network、响应与最终业务状态。**本地直达子应用、Scalar、直接 HTTP 均不能替代宿主验收。**
- 账号与凭据位置见整体测试规范；本阶段不复制凭据。

## 执行记录

- [x] 完成 API Client 核对（Ticket 06：累计净增 1 个查询端点、组织字段差异消除、`/auth/login` 保持排除、包 typecheck/build 通过）。
- [x] 完成页面实现和组件验证：页面/适配层/组织接缝与用例全绿（门禁数字与用例计数见阶段 `testReport.md` 的对应轮次小节，终值见其"门禁终值"节）；路由与真实 API 接线已随 Ticket 07 完成，并有独立复核（`Client/审查-Ticket07接线复核.md`）。
- [x] 完成宿主验收并更新测试报告：宿主菜单、目标表、页面路由与真实接线均已就绪，宿主验收已执行（含 2026-09-15 补做的两页面并发重复、写成功刷新失败两条）；**组织切换后目标组织读取失败（C17）的真实宿主页面层**按 Testing Baseline §3 整条记 `Blocked`（受阻原因登记为决策标签，接受口径待负责人确认），逐条证据与原因见阶段 `testReport.md`。
