# 互认项目页面与状态实现方案（F2 第一批）

范围：`client/apps/dy-medical-recognition/src/pages/recognitionProjects/`。本文件描述页面结构、状态机与组织范围接缝，不宣称宿主验收完成（属 Ticket 08）。

## 1. 页面结构

| 区域 | 控件 | 承担的业务动作 |
|---|---|---|
| 标题区 | 标题 + 只读「组织编码」 | 展示页面实际读取的可信组织；组织不由页面自由输入、页面不持有组织列表 |
| 标题区 | 「重新读取互认配置」按钮 | 手动刷新当前组织的配置列表与可选标准项目；组织未对齐时禁用 |
| 标题区 | 「新增配置」按钮 | 打开新增弹窗；不可操作时禁用 |
| 筛选区 | 编码输入框、配置状态 Segmented、重置按钮 | 只影响**本地展示**，不影响读取范围与排除集合 |
| 表格 | 编码 / 名称 / 类型 / 分类 / 分组 / 可互认时间（天）/ 配置状态 / 目录停用原因 / 操作 | 只呈现查询契约交付的字段；不展示创建/修改人与时间，不展示「标准目录有效」「可用于新匹配」 |
| 表格行操作 | 修改时间、启停 | 停用配置仍可修改时间；启停独立确认，不改动其它字段 |
| 新增弹窗 | 标准项目（按分类/分组分组）、可互认时间（天） | 只提交标准项目编码 + 正整数天数；**不提交组织编码** |
| 修改弹窗 | 只读的标准项目资料 + 可编辑天数 | 同值也照常提交 |
| 启停确认框 | 确认文案 + 危险按钮（停用时） | 只改配置自身状态 |
| 组织变化确认框 | 「放弃并切换」/「取消」 | 有未保存内容时确认放弃；取消保留内容并进入待切换提示态 |

与阶段 1 标准目录页面的一致点：同名目录约定（`XxxApi.ts` / `Xxx.tsx` / `Xxx.css` / 同名测试）；同样的「标题 + 刷新 + 主操作」头部；写操作走「提交 → 关弹窗 → 重载」；读取失败用 `Alert` 横幅保留旧数据并禁用写入口；表格 `rowKey` 用业务标识；适配层函数在页面外，页面不接触生成模型。

## 2. 状态机

| 状态 | 进入条件 | 允许的操作 | 退出 |
|---|---|---|---|
| 首次加载 | 挂载且可信组织可用 | 只读（写入口全禁用） | 成功 → 就绪；失败 → 读取失败 |
| 就绪 | 两个读取源都成功（新增入口还要求标准目录已读到，见补充规则） | 全部 | 刷新 / 写操作 / 组织变化 |
| 加载中（手动刷新） | 点击刷新 | 只读 | 同首次加载 |
| 读取失败 | 任一读取源失败 | 只读 + 重试；**保留旧数据** | 重试成功 → 就绪 |
| 写操作中 | 提交流程进行中 | 全部禁用；重复点击不再发请求 | 写成功 → 关闭弹窗并重载；写失败 → 保留弹窗与输入 |
| 刷新失败（写入成功后） | 写成功但随后的重载失败 | 只读 + 重试；**不改判为写失败** | 重试成功 → 就绪 |
| 组织待切换（confirming） | 可信组织变化（宿主重新签发 token）且存在未保存内容 | 只读；二选一 | 确认放弃 → 清空并加载目标组织；取消 → 组织待切换（deferred） |
| 组织待切换（deferred） | 用户取消了一次组织变化确认 | 只读 + 提示条「放弃并切换」 | 再次确认 → 清空并加载；或未保存内容消失后自动进入加载 |
| 组织范围不可用 | token 缺失或 org 编码为空/空白 | 无 | 宿主重新登录签发 token 后重新加载 |

补充规则：组织变化与每次重载都推进请求序号，迟到响应一律丢弃（旧组织响应不得污染新组织）；**在途请求只作废、不取消**——生成客户端没有取消入口（`RequestConfiguration<T>` 无 `AbortSignal`，生成方法原样调用适配器），旧请求仍会在网络与服务端跑完，但其响应不写入任何状态，页面不为此自建底层 HTTP 调用或新增拦截器（同 `useEnumMetadata` 的卸载守卫口径，登记为规范边界与残余风险）；写完成后的重载前校验「提交时的组织仍是页面当前组织」，否则不重载；「未保存内容」判定与**实际渲染**一致——新增弹窗处于打开状态，或修改弹窗处于打开状态且其目标配置仍在**当前行集**中（目标行已离开行集时该弹窗不渲染、不承载任何内容，故不算未保存内容）；组织未对齐期间不回退展示旧组织结果、也不允许对旧组织发起写入。**新增入口的就绪判定单独跟踪 `catalogLoaded`（该次标准目录读取是否成功），与目录内容是否为空解耦**：读取成功但目录为空属正常结果、入口仍可用（弹窗提示"没有可新增的标准项目"），只有"还没读到"或读取失败才禁用，避免把两种情况混同而永久禁用新增且既无提示也无重试入口。**修改可互认时间弹窗与启停弹窗都只持配置标识**（`{ kind: 'duration' | 'toggle'; configurationId }`），行数据每次渲染按**当前行集**重取，不保留打开那一刻的行快照：后台刷新交付新值后，修改弹窗的只读资料与表单回填、启停弹窗的目标行与动作目标都取当前行，代价是同时覆盖修改弹窗中尚未提交的输入（弹窗不得展示旧快照的直接结果）；行集里找不到该配置（或启停弹窗的目标行当前状态未知）时不渲染弹窗，避免对未知行提交写入。

**写弹窗门控与提交前复核**：组织未对齐（可信组织已变化、页面尚未应用）、读取失败（含写入成功后的刷新失败）或读取在途时，三个写弹窗统一禁用确定按钮并在弹窗内说明原因（与页面顶部阻断提示同口径，由 `RecognitionProjectWriteGate` / `writeBlockedReason` 裁决，弹窗不自行判断组织或读取状态）；提交动作在发起请求前读**现值**可信组织，并与页面已应用组织比对：不一致或读不到即**零写请求**，并在弹窗内给出可见原因（覆盖渲染与提交之间的组织变化），因此组织未对齐期间不会对旧组织发起写入。

## 3. C 矩阵对应

| 编号 | 实现位置 | 待验证层级 |
|---|---|---|
| C1 | 只读组织展示 + 查询入参 `organizationCode` 取自 `LoginUserManager.getOrgID()` | Component（声明层级为 Component；宿主菜单进入属 Ticket 08 的页面级验收，不改本用例声明的层级） |
| C2 | `selectableStandardItems` / `toSelectableOptionGroups` | Component |
| C3 | `configuredStandardProjectCodes` + `selectableStandardItems` | Component |
| C6 / C20 | `parseRecognitionDurationDays` / `buildDurationUpdatePayload` / 修改弹窗 | Component/Host（Host 层归 Ticket 08） |
| C8 | 启停确认框（取消、关闭、确认三分支） | Component/Host（Host 层归 Ticket 08） |
| C10 | `requestIdRef` 请求序号守卫 | Component |
| C11 / C15 | `Promise.allSettled` 合并 + `loadFailure` 阻断态（阻断态同时冻结三个写弹窗的确定按钮） | Component |
| C17 | 目标组织加载失败时不进入可操作状态、不误用旧组织数据 | Component（Host 层见末行） |
| C13 / C14 | 加载 effect + `loading` 禁用写入口（`loading` 期间写弹窗确定按钮同样冻结） | C13 Component/Host（Host 层归 Ticket 08）；C14 Component |
| C16 | `applyOrganization` 统一清空列表、选择数据、筛选与弹窗 | Component |
| C18 | 组织变化确认框 + 派生的 `switchPending` / `switchConfirming` / `switchDeferred`（取消记录在 `dismissedOrganizationCode`）；组织未对齐期间写弹窗冻结、提交前 `submitWrite` 以可信组织复核 | Component |
| C19 | `submittingRef` 同步去重 | Component |
| C21 | 写成功后 `load(..., 'refreshAfterWrite')` | Component |
| C23 | `buildCreateConfigurationRequest` / `buildConfigurationListQuery` / `toConfigurationRows` | Component/Typecheck |
| C4-C5 / C7 / C9 / C12 / C17 / C22 / C24 / C25 | — | Host / Static / Build（两页面并发重复与写成功刷新失败已于 2026-09-15 以真实宿主补做通过；**组织切换后目标组织读取失败的真实宿主页面层未执行，按测试基线整条记 `Blocked`**，采纳上方组件层证据，受阻原因登记为决策标签且接受口径待负责人确认） |

## 4. 组织范围唯一接缝（S2-D12 已结案）

`recognitionProjectsOrganizationScope.ts` 是「可信当前组织」的唯一接缝，已按结案口径接入既有机制，不自造协议：

- 可信当前组织取自登录管理器（`@dy/auth` 的 `LoginUserManager`）的当前登录组织，来源与同组织参照项目 `Dy.LisCenter` 一致；服务端按 `HttpRequestInfo.OrgId` 只接受与可信组织相等的组织，组织切换由**宿主重新签发 token** 完成，服务端不建授权集合。
- 读取沿用既有包：`@dy/auth` 的 `LoginUserManager.getOrgID()` 给出组织编码，`@dy/auth-react` 的 `useAuth()` 提供 token；`token` 变化即重新求值，页面据此识别组织变化（同一做法见 `Dy.LisCenter/.../TrustedCurrentScopeView.tsx` 与 `.../BranchCollaborationBranchPage.tsx`）。
- 组织编码缺失或纯空白一律返回 `unavailable`：不伪造组织、不静默使用空串、不退化为全局查询；页面进入不可操作状态。
- 口径 A 下页面**不持有可选组织集合、不做自由输入**（与 `Client/design.md` 第 5 行一致），因此 C16/C18 的「切换组织」= 对可信组织变化的正确响应。

## 5. 已定实现边界

- **查询只提交组织编码**：页面为排除集合的完整性，读取当前组织的**全量**配置并在本地按状态/编码筛选；若把筛选下推到服务端，筛选后列表将缺失停用/其它编码配置，导致已配置项目重新可选（违反 C3）。适配层仍支持可选筛选并单独测试。
- **不分页**（S2-D7）、**不展示创建修改信息**（S2-D9）、**不新增权限模型、错误拦截器、业务错误类型、状态管理库、依赖**。
- **行标识规则**：表格行标识优先取契约唯一键 `configurationId`；缺失时用业务字段组合（标准项目编码、项目类型、名称、分类、分组）；同一组业务字段完全相同的多行只在组内回退到出现序号，保证表格 key 唯一、行内操作控件不串位。该序号不参与任何业务判定，也不作为字段提交或展示。**唯一性断言按 `data-row-key` 实际值**：组件层用例「业务字段完全相同的多行互不串位，操作与数值都指向各自那一行」取 rc-table 渲染到行上的 `data-row-key` 两两不同作为判据，不依赖 React 的重复 key 告警文本。
- **页面状态只持页面视图模型**：配置行为适配层交付的行模型，标准目录为摊平后的候选集（`SelectableStandardItem[]`）；生成读模型（如 `EffectiveMedicalStandardCatalogReadModel`）只作为读取响应经过加载函数，不进入页面状态，契约形状变化在适配层吸收。
- **枚举元数据不是组织范围数据**：服务端按 `enumName` 返回进程级静态枚举表（成员取值与中文），不读业务数据、不按调用方组织过滤；页面只在**可信组织可用**时请求（组织范围不可用时页面整体为阻断分支，不渲染需要枚举文案的区域，此时发请求只会产生一次必定被丢弃的调用）。缓存身份为「API Client 实例 + 枚举名」；若该接口将来改为按组织交付文案，缓存键与失效时机必须同步加入身份维度。
- **空态与计数口径**：「本组织没有配置」与「筛选后没有匹配项」分开表达：前者计数为 0 并显示本组织空态文案；后者显示筛选后空态文案，并同时给出本组织条数与筛选后条数（编码筛选与状态筛选同口径）。
- 适配层不创建 Client、不读取 token、不解析平台错误响应；I/O 出口已接入 Ticket 06 生成的 5 个端点（见第 6 节）。

## 6. Ticket 06 之后的收敛项（Ticket 07 已执行）

Ticket 07 已完成接线，`recognitionProjectsApi.ts` 中原有的手写契约镜像与占位出口已全部收敛；页面与测试只经适配层取类型，不存在第二套契约来源：

1. **手写镜像类型替换为生成类型**：`RecognitionProjectConfigurationReadModel`、`EffectiveMedicalStandardCatalog*` 已删除，改为从 `@dy/api-client-medical-recognition` 导入生成类型；`ConfigurationListQuery` 的查询体由生成类型 `RecognitionProjectConfigurationListQueryRequest` 承载；`CreateConfigurationPayload` / `DurationUpdatePayload` / `EnabledUpdatePayload` 保留为**写前置校验载荷**（只含业务必填字段，不含生成端的 `changedProperties`/`additionalData`），在 I/O 出口逐字段展开为生成请求，页面仍不接触 Kiota 模型。
2. **数值枚举取值**：枚举契约缺陷已于 2026-09-16 修复（批次 10）：后端 OpenAPI 声明 `enum`/`x-enumNames`/`x-enumDescriptions`，生成入口归一并重生成后 `configurationStatus` / `itemType` 为 `number | null` 且走数值写出，缺陷期的空对象类型与回退类型已消失（见阶段 `testReport.md` 的枚举契约修复节）。适配层仍保留 `MEDICAL_ITEM_TYPES` / `CONFIGURATION_STATUSES` 最小映射作为取值来源与本地兜底，取值须与后端枚举声明一致（生成端不产出 TypeScript 枚举），未知取值一律映射为 `null`，页面只有这一套枚举判定。
3. **`pendingIntegration()` 已彻底移除**：5 个出口均调用生成端点（`queryRecognitionProjectConfigurationList`、`queryEffectiveMedicalStandardCatalog`、`createMutualRecognitionItem`、`updateMutualRecognitionItemConfiguration`、`enableMutualRecognitionItem` / `disableMutualRecognitionItem`），无静默成功、无返回空集合、无硬编码 URL。

**配置状态筛选不下推服务端**：这是业务口径（下推会缩小读取范围、破坏新增排除集合的完整性），不因枚举契约已修复而改变；页面在已读取结果上本地筛选。

## 7. 已知遗留（不由本页面处理）

- 表格 `目录停用原因` 列的 `ellipsis` 需要固定表格布局才可靠截断；宿主验收时按实际视口复核（属 Ticket 08 展示项，已随宿主验收执行）。
