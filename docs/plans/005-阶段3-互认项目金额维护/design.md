# 阶段 3：互认项目金额维护（F10）

阶段设计见 [Server/design.md](Server/design.md) 与 [Client/design.md](Client/design.md)；批次与矩阵编号见 [impl.md](impl.md)、[Server/impl.md](Server/impl.md) 和 [Client/impl.md](Client/impl.md)；测试结果见 [testReport.md](testReport.md)。

## 目标与范围

- 目标：平台管理员与医院管理员在各自的数据范围内维护组织、医院、院区、标准项目对应的当前金额，并在管理端按组织、医院、院区查看该组织已建立互认配置的标准项目及其金额或未配置状态。
- 纳入：金额保存（不存在则新建、存在则覆盖）、金额查询、`mrec_organization_hospital_branch_recognition_amount` 新建表、API Client 累计生成，以及管理端「互认项目金额维护」与「本院区互认项目金额」两个页面与路由。
- 排除：金额独立版本、金额变更历史、历史金额重算、金额启停状态、按金额重算既有预计节省金额、院内项目与项目对照、批量保存多行金额、角色权限模型。
- 依据：SRS F10 与「互认项目金额维护」页面要求、SRS 假设与依赖关系 4、14、UML `1-MedicalRecognitionReport.wsd` 与 `2-MedicalRecognitionConfigurationUiQuery.wsd`。
- 前置：标准项目互认配置（阶段 2，004）——本阶段的金额数据以其互认配置为前提。
- 两个管理端页面与「互认项目」页统一（S3-D22～S3-D28）：四栏层级列视图、行内金额编辑、筛选区与地址栏参数、页头三级范围展示（2026-10-10 修订：医院管理员页不提供院区选择、院区恒取登录院区，原全页「院区恒定单值」表述仅保留于平台管理员页；状态机制两道防线仅平台管理员页保留，医院管理员页可信范围变化直接整表重读）；后端两个金额查询带可选关键字入参（S3-D26）。仍排除分页、角色权限模型与批量保存。
- 环境边界：本环境**无法维护第二个组织**，跨组织的真实宿主面不可构造，因此降级为"应用层证据 + `AcceptedRisk`、宿主面记 `Blocked`"（决策 S3-D20、矩阵 V29）。该降级意味着 **SRS F10 前置条件 2 中"按平台管理员授权选择组织"这一分支在本环境无法取得真实证据**。
- 端：跨端。

## 已确认设计

**两个入口、两种信任边界。** 平台管理员的组织、医院、院区三个值全部来自请求；医院管理员的组织与医院取自可信上下文（`HttpRequestInfo.OrgId`、`HosId`），请求只提交院区。层级为**组织 → 多个医院 → 多个院区**：平台管理员可逐级切换组织、医院、院区；医院管理员在**本医院下的多个院区**之间切换。两个入口都只从可信上下文取操作人（`UserId`）。服务端不区分角色、不实现角色分支、不做权限二次确认：菜单与接口授权由权限系统负责，本项目只交付菜单名称与路由；但两个入口都保留组织、医院、院区「存在、启用、父子归属」的校验，该校验只用于可信业务归属与记录归属一致性，不构成操作权限校验。

**金额是当前值，不是累计值。** 保存按业务键（组织编码、医院编码、院区编码、标准项目编码）定位：不存在则新建，存在则覆盖 `current_amount`；`0.00` 是「已配置」，只有没有记录才是「未配置」；同值重复保存按成功处理、更新操作信息并登记事件，不做无变更短路。金额调整只影响之后成功保存的采纳处理结果，本阶段不重算历史金额，也不建立版本或变更历史。

**查询行集由互认配置驱动。** 行集是该组织已建立互认配置的标准项目（互认配置表 `mrec_mutual_recognition_item`），金额表按业务键 LEFT JOIN；没有匹配行即 `IsAmountConfigured=false`、当前金额无业务值。标准项目名称、类型、分类、分组与目录停用状态关联本平台标准目录取得；组织名称、医院名称、院区名称由服务端**一次性批量读取**后建映射填充。查询按标准项目编码升序，不分页。

**列表只返回名称与标准项目编码。** 返回标准项目编码（保存时的行业务键）、标准项目名称、项目类型、分类名称、分组名称、组织名称、医院名称、院区名称、当前金额、金额已配置、互认配置状态与当前不可用原因；不返回组织编码、医院编码、院区编码，也不返回最后修改时间与最后修改人。

**当前不可用原因沿用既有口径。** 只表达标准目录停用，按分类、分组、标准项目顺序返回一条原因，文案为"所属分类已停用""所属分组已停用""标准项目已停用"；目录全部启用时为 `null`，即使互认配置自身停用也如此。配置自身状态由 `ConfigurationStatus` 表达。

**建表。** 目标表 `mrec_organization_hospital_branch_recognition_amount`，列序 `id`、`organization_code`、`hospital_code`、`branch_code`、`standard_project_code`、`current_amount`、`oper_time`、`oper_id`，唯一索引覆盖四个业务键列；该实体没有生命周期状态标志列，不设启停列。建表由负责人按阶段确认的目标数据库与 schema 执行，本阶段只交付最终 DDL。

## 关键决策

| ID | 决策 | 状态 | 依据 | 适用范围 | 待验证面 |
|---|---|---|---|---|---|
| S3-D1 | 两个页面、两种信任边界：平台管理员的组织/医院/院区取自请求，医院管理员的组织/医院取自可信上下文、院区取自请求；服务端只从可信上下文取操作人 | DesignConfirmed | SRS F10 参与者与备选流 1；SRS 假设与依赖关系 4、14；`docs/plans/001-总体计划/design.md` 4.5 | Contracts、Application、Client | V8-V15、V22、C1-C3 |
| S3-D2 | 本项目不维护角色权限模型、不做权限二次确认；菜单与授权由权限系统负责，本项目只交付菜单名称与路由；但保留组织、医院、院区的存在、启用与父子归属校验 | DesignConfirmed | SRS 假设与依赖关系 4、14 | Application、Client、文档 | V8-V13 |
| S3-D3 | 金额保存为**一个 Command**，由两个 Application 入口调用，共用同一 `OrganizationHospitalBranchRecognitionAmountSavedEvent`（两个入口在 Application 解析完成后的领域数据完全相同）；**UML1 不新增第二条 Command** | DesignConfirmed | [backend-command-query-event](../../../.agents/instructions/backend-command-query-event.md) 第 1 节要求 Command 与领域意图一一对应；UML1 已由两个角色指向同一命令 | Domain、Application、UML1 | V1-V7、V25 |
| S3-D4 | 金额保存按业务键定位：不存在则新建、存在则覆盖 `current_amount`；`0.00` 是「已配置」；同值重复保存按成功处理、更新操作信息并登记事件，不做无变更短路 | DesignConfirmed | SRS F10 主事件流 5 与业务规则 1-2；既有"资料未变化时保存仍提交并登记事件"口径 | Domain、Application | V1-V7、V25 |
| S3-D5 | 查询行集由 `mrec_mutual_recognition_item` 驱动，金额表按业务键 LEFT JOIN；未配置行 `IsAmountConfigured=false`、当前金额无业务值 | DesignConfirmed | SRS F10 前置条件 3 与业务规则 1；UML2 模块说明 | Query | V17、V20 |
| S3-D6 | 列表只返回标准项目编码与名称集合，不返回组织/医院/院区编码，也不返回最后修改时间与最后修改人 | DesignConfirmed | SRS「用户界面」章「互认项目金额维护」页面要求 2 与 5；UML2 `RecognitionAmountReadModel` | ReadModel、UML2、Client | V18、V26 |
| S3-D7 | 名称与归属一次性批量读取后在内存映射填充，调用次数不随返回行数增长 | DesignConfirmed | [backend-architecture](../../../.agents/instructions/backend-architecture.md) 第 5.3 节；`Dy.LisCenter` 的 `LisCenterOrganizationPathResolver` 既有做法 | Application、Query | V19 |
| S3-D8 | 金额查询不分页 | AcceptedRisk | 既有互认配置查询的不分页口径；重审条件为出现可复现卡顿、超时或内存问题 | Query、Client | C22 |
| S3-D9 | 实体语义与注释按 F10 修正为「当前金额维护」，写入侧不做累加；不改 UML1 实体定义 | DesignConfirmed | SRS F10；UML1 实体只声明「当前金额」；现有实体注释与二者及同族 DTO 矛盾 | Domain、注释 | V28 |
| S3-D10 | 前端引入 `@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39`，并在 `client/pnpm-workspace.yaml` 增加 kiota overrides 统一到 `1.0.0-preview.103` | DesignConfirmed | 项目不自行实现组织/医院/院区选择控件，复用既有组件；`@dy/api-client-base@0.1.28` 精确依赖 `@dy/auth@0.1.28`，与项目现有 `@dy/auth@0.1.39` 会形成同一包的双实例，故取 `@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39` 这一组合 | Client、依赖 | C23-C25 |
| S3-D11 | 医院管理员入口的可信上下文由**登录令牌声明与当前登录用户档案共同构成**：组织层与医院层按同一规则解析——令牌该层非空白即取令牌值；令牌该层缺失或空白时用当前登录用户档案补齐；令牌与用户档案都提供该层且不一致即拒绝；补齐后该层仍为空即拒绝，不使用默认值、不降级为空值；用户标识无法解析为非空 `Guid` 或读不到用户档案同样拒绝。**院区不做可信授权回落**：院区仍取自请求，由既有 `OrganizationPathResolver` 校验其属于可信医院（S3-D19） | DesignConfirmed | SRS 定义·组织、医院、院区与假设与依赖关系 4、14；SRS F10 前置条件 2 与备选流 1；`Dy.LisCenter` 的 `LisCenterCurrentBranchScopeService` 既有做法（令牌优先、缺失层由用户资料补齐、冲突即拒绝） | Application | V14 |
| S3-D12 | 首次并发保存同一业务键时以唯一索引兜底，数据库异常按原样向外传播，不做冲突翻译、不自动重试 | DesignConfirmed | SRS F10 未定义并发语义；`backend-command-query-event` 第 4.1 节要求说明并发兜底；并发冲突的呈现形态不在本阶段范围内 | Repository、Application | V16 |
| S3-D13 | 本阶段不处理生产配置：只改 `config.development.json`，不动 `config.json`，也不登记生产端口分配 | DesignConfirmed | 本阶段只面向开发环境；生产部署配置未纳入交付范围 | Client | C25 |
| S3-D14 | 可空枚举筛选形态：**本阶段页面不下推枚举筛选**，不手改生成物；生成管线侧由对应阶段管理 | AcceptedRisk | 本阶段页面不经枚举筛选，故不受该形态影响 | Client | C24 |
| S3-D15 | server 侧允许新增 `Dy.Base.Application.Contracts` 引用与中央版本条目 | DesignConfirmed | S3-D11 的组织服务校验需要该契约；依赖闭包与版本约束已按元数据核对，**未实测构建** | Application、`Directory.Packages.props` | V8-V10、V13-V14、V19；批次 1 以实测 restore/build 确认 |
| S3-D16 | 新增 `src/runtimeConfig.ts` 承载 `apiBaseUrl` 与 `baseApiBaseUrl`，入口调用一次 `configureBaseComponents`；只在 `public/config.development.json` 增加 `baseApiBaseUrl` | DesignConfirmed | 与既有子应用运行时配置结构一致；生产配置见 S3-D13 | Client | C25 |
| S3-D17 | 平台管理员入口的保存与查询**取值口径一致**：组织、医院、院区一律取请求提交值，并校验其存在、启用与父子归属；医院管理员入口的组织与医院取自可信上下文。**「只信可信当前组织」口径不延伸至平台管理员入口** | DesignConfirmed | SRS F10 前置条件 2「由可信上下文确定或按平台管理员授权选择」；SRS「用户界面」章页面要求 1 | Application、Query、UML2 | V29 |
| S3-D18 | 查询按标准项目编码升序；未配置行 `CurrentAmount` 契约类型可空、值为 `null` | DesignConfirmed | SRS「用户界面」章页面要求 4 要求区分未配置与零元；UML2 约定未配置时当前金额无业务值 | ReadModel、Query | V18、V20 |
| S3-D19 | 医院管理员入口的院区范围：只允许**可信上下文所属医院下的院区**，提交其他医院或不属于该医院的院区即拒绝；**不引入「院区级数据权限」概念**，服务端不做该层校验（权限由权限系统负责，见 S3-D2） | DesignConfirmed | SRS F10 备选流 1；UML1 命令规则 4；SRS 假设与依赖关系 4、14 | Application、Query | V30 |
| S3-D20 | 平台管理员入口的**跨组织真实宿主面降级**：V29 以应用层证据为准并记 `AcceptedRisk`，真实宿主面记 `Blocked`；不因此改变取值口径（S3-D17） | AcceptedRisk | 环境无第二个组织，跨组织真实面不可构造 | Application、Query、Client | V29 |
| S3-D21 | 列表返回**枚举中文**：`RecognitionAmountReadModel` 新增只读计算属性 `ItemTypeText`（可空，取值未登记时为 `null`）与 `ConfigurationStatusText`（非空，未登记取值按严格解析抛出）；文本由服务端按枚举 `Description` 解析，与阶段 2 的 `S2-D15` 同口径，前端不再本地硬编码枚举中文 | DesignConfirmed | [总体计划设计](../001-总体计划/design.md) 5.3 枚举协作契约（适用于全部后续阶段）；阶段 2 `S2-D15` 既有先例 | ReadModel、UML2、Client | V18、V26、C5 |
| S3-D22 | 范围选择口径（2026-10-10 修订，医院管理员页原「本侧院区可选、三档解析、切换确认框」作废）：平台管理员页组织、医院可切换，院区恒定单值——院区始终恰好一个值、控件不可清空；初始值取可信院区（`LoginUserManager.getBranchID()`），可信院区缺失时取当前组织、医院下的第一项；切换组织或医院后院区自动重取（可信院区属于新范围则选中它，否则选新范围第一项）。医院管理员页不提供院区选择：组织、医院、院区恒取可信上下文，查询与保存的院区参数恒为登录院区，登录院区变化直接整表重读、不弹确认框；院区为空属权限配置错误，页面不伪造院区、不回退默认院区、不进入整页阻断（登录院区缺失时零业务请求并以空态说明，不做全院查询）。可信组织或医院缺失（登录凭证缺失，或组织、医院编码任一空白）时**两个页面都整页阻断**——只渲染标题与一条错误 Alert，不渲染筛选区与列表、不发出任何业务请求（原「可信组织或医院缺失时逐级取第一项仅适用平台管理员页」的回退规则作废），不伪造组织、不降级为空值 | DesignConfirmed | SRS F10 前置条件 2 与「用户界面」页面要求 1；统一三页交互的既定范式；院区恒定单值使三级名称可在页头展示一次、行内不再重复（技术理由）；缺失凭证时回退首项会以伪造的范围发请求，与可信上下文纪律冲突（技术理由）；本院页院区固定与登录凭证一致，避免跨院区维护口径（技术理由） | Client | C1、C3、C30 |
| S3-D23 | 两个页面**页头展示三级范围**：当前登录范围的组织、医院、院区名称以单行纯文字展示（「组织名 / 医院名 / 院区名」，2026-10-10 修订，原「名称＋编码上下两行」款式作废），不展示任何组织、医院、院区编码；名称超长单行省略、悬停经 Tooltip 展示全文；某级名称缺失或反查失败时该级显示「未知」，不回退编码。组织名称取自可信范围查询返回的组织名称；医院、院区名称按登录上下文编码经基础目录接口反查（与 `@dy/auth` 同源认证适配器自动携带宿主登录令牌），反查失败由宿主统一提示。页头范围固定展示登录范围，不随筛选区选定范围变化；行内不再展示组织名称、医院名称、院区名称三列，读模型字段保留不变 | DesignConfirmed | SRS「用户界面」页面要求 2（展示三级名称，展示位置上收页头）；编码不进入界面渲染、仅作查询参数与提交字段保留（参照 `Dy.LisCenter` 同类页面的范围条先例） | Client、ReadModel | C5、C31；V18 字段集合不变 |
| S3-D24 | 两个页面列表为**横向四栏层级列视图全平铺**（类型｜分类｜分组｜项目，无折叠控件，款式同 S2-D16）；项目行字段为标准项目编码＋名称、当前金额、金额状态徽标、互认配置状态、当前不可用原因；层级归类与跨行在适配层完成，服务端仍按标准项目编码升序（S3-D18 不变）、视图内组间按类型、分类、分组归类排序、组内按编码升序；页脚报当前显示条数（「共 N 项」） | DesignConfirmed | SRS「用户界面」页面要求 2 与 3；S2-D16 四栏款式先例 | Client | C5、C16、C35；V20 排序口径不变 |
| S3-D25 | 两个页面查询区为 `Form layout="inline"` 行内表单（2026-10-10 修订，原裸容器与「不设重置控件」口径作废）：关键字草稿—提交模型（点击查询或回车按该关键字向服务端查询，输入框清空同时触发空条件读取，空条件提交等价于刷新）；金额状态与互认配置状态两个 Segmented 切换即生效、只在服务端结果上本地裁剪、不触发读取、不下推；「重置」恢复默认条件并按默认条件重新读取一次，路由参数按默认值回写（`keyword`、`amountStatus`、`configurationStatus`，输入原文不写入）；已生效条件回写本页路由查询参数；页头不设独立刷新按钮；查询按钮逐字规格 `type="primary"` + `SearchOutlined` + `autoInsertSpace={false}` + `htmlType="submit"` + 文字「查询」；读取在途时查询按钮进入 `loading` 形态、行内操作与写入口一并禁用；S3-D14 维持（仍不下推枚举筛选，状态筛选只做本地裁剪，取值来自读模型布尔与文本字段） | DesignConfirmed | SRS「用户界面」页面要求 2；S2-D21、S2-D24、S2-D27 既有口径；全站统一查询入口与按钮规格（参照 `Dy.LisCenter` 同类列表页先例） | Client、Query | C24、C27-C29、C36；V31-V33 |
| S3-D26 | 两个金额查询 Request 新增可选 **`Keyword`**：按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称、所属分组名称，比较不区分大小写，用户输入的 `%` 与 `_` 按普通字符转义（同 S2-D29 口径与阶段 2 查询 SQL 的既有写法）；缺失或空白视为未提供、不附加过滤；既有 `StandardProjectCode` 可选入参保留不变，新页面不再使用，不改动已交付接口语义 | DesignConfirmed | SRS「用户界面」页面要求 2；S2-D29 关键字口径先例 | Contract、Query、UML2 | V31-V33、C24 |
| S3-D27 | 金额录入为**行内编辑**（款式同「互认项目」页可互认时间行内编辑）：点击当前金额值（含「未配置」标记）进入编辑，回车保存、Esc 取消；校验（必填、非负、最多两位小数）不通过时以页面消息提示（`message.useMessage()` 实例的 `message.error`，2026-10-10 修订，原单元格内红字作废，表格单元格内不渲染校验红字）、零写请求、输入保留；同值照常提交；提交成功给出 `message.success` 反馈后收起并整表重载；编辑器以当前行金额为键重新挂载；目标行离开当前行集不渲染编辑态 | DesignConfirmed | SRS F10 业务规则 1-2（金额校验与零元语义）；S2 行内编辑款式先例；错误文字不渲染进表格本体、前端可预判输入问题用 message 承载（技术理由） | Client | C8-C10、C32；C12 口径不变 |
| S3-D28 | 状态机制按页面分两式（2026-10-10 修订，医院管理员页原「确认框 + 三层比对」两道防线随院区选择移除作废）：平台管理员页保留两道防线——可信范围变化（宿主重新签发 token）且存在未保存内容（行内编辑正在编辑的行仍在当前行集）时弹「放弃并切换／取消」确认框，取消进入待切换提示态；保存发起前现读可信组织、医院、院区与页面应用范围比对，已变化或现读不到即零写请求并在行内就地说明；用户主动切换范围维持静默清空未保存输入（C21）。医院管理员页无选定范围可滞留：可信范围（含登录院区）变化时直接清空未保存输入并按新范围整表重读，不弹确认框；保存发起前现读可信组织或医院，读不到即零写请求并以页面消息给出可见原因；保存请求以提交时生效的院区发出，随后渲染按现登录院区切换重读。组织未对齐期间两页都不回退展示旧范围结果、也不允许对旧范围发起写入 | DesignConfirmed | SRS 假设与依赖关系 4、14（可信上下文）；S2 组织变化确认与提交前复核款式先例；医院管理员页无用户选定范围后确认框失去对象（技术理由） | Client | C21、C33-C34 |

S3-D1、S3-D6、S3-D18 的变化已同步 `docs/需求规约SRS.md` 与 `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd`；S3-D9 是注释与语义修正，不改变 UML1；S3-D3 确定为一个 Command + 两个 Application 入口，UML1 不新增命令；S3-D17 的取值口径已落 `Server/design.md` 的入口取值段与保存/查询链；S3-D19 的院区范围口径已同步上游（UML1 命令规则 4、SRS F10 备选流 1 与参与者表/「用户界面」页面要求 1、`docs/流程图.md`、`docs/业务流程图.md`）；S3-D20 的降级口径已落阶段根 `testReport.md` 的「已确认的验证降级」节与 `Server/design.md` 的 V29；S3-D21 的读模型字段变化已同步 `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd` 的 `RecognitionAmountReadModel`（SRS 只规定展示字段、不列字段清单，无需改动）；S3-D22～S3-D28 的设计细节见 [重开轮-统一三页设计-任务交接.md](重开轮-统一三页设计-任务交接.md)，SRS「用户界面」页面要求 2 已同步（三级名称页头展示、查询支持关键字），`RecognitionAmountListQueryRequest` 与 `BranchRecognitionAmountListQueryRequest` 的 `Keyword` 入参已同步 `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd`（读模型字段不变）。**本台账是阶段 3 决策的唯一权威来源**。

## 验收与停止条件

- 后端：两个保存入口、两个查询入口、领域规则、DDL/SqlMap 映射与事件口径均有测试证据；后端测试前矩阵见 [Server/design.md](Server/design.md) 的验证矩阵章节（V1-V33）。
- 验证面：后端 V1-V33、前端 C1-C36；取证完成并经负责人裁定后恢复收口。
- API Client：按累计规则生成并通过契约、typecheck 与 build 核对，保留阶段 1、阶段 2 已交付接口与 `/auth/login` 排除规则。
- 前端：两个页面从宿主登录页与菜单进入完成真实验收；本地直达、构建成功与接口直测不替代宿主验收。前端矩阵见 [Client/testPlan.md](Client/testPlan.md)。
- 目标数据库/schema 未确认或表未建成时，依赖新表的集成项记 `Blocked`；不以空列表、构建成功或接口直测替代业务通过。
- 超出授权或影响边界不明时停止：需要修改既有已交付接口语义、需要引入服务端角色权限模型、需要改动其他系统数据或对象、或需要在前端扩散生成的 Kiota 类型时，先暂停该范围并请求项目负责人决策。
