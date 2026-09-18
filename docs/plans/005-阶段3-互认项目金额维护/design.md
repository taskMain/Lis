# 阶段 3：互认项目金额维护（F10）

阶段设计见 [Server/design.md](Server/design.md) 与 [Client/design.md](Client/design.md)；批次与矩阵编号见 [impl.md](impl.md)、[Server/impl.md](Server/impl.md) 和 [Client/impl.md](Client/impl.md)；测试结果见 [testReport.md](testReport.md)。

## 目标与范围

- 目标：平台管理员与医院管理员在各自的数据范围内维护组织、医院、院区、标准项目对应的当前金额，并在管理端按组织、医院、院区查看该组织已建立互认配置的标准项目及其金额或未配置状态。
- 纳入：金额保存（不存在则新建、存在则覆盖）、金额查询、`mrec_organization_hospital_branch_recognition_amount` 新建表、API Client 累计生成，以及管理端「互认项目金额维护」与「本院区互认项目金额」两个页面与路由。
- 排除：金额独立版本、金额变更历史、历史金额重算、金额启停状态、按金额重算既有预计节省金额、院内项目与项目对照、批量保存多行金额、角色权限模型。
- 依据：SRS F10 与「互认项目金额维护」页面要求、`CONTEXT.md`「组织医院院区互认项目金额」与「角色与菜单授权」、UML `1-MedicalRecognitionReport.wsd` 与 `2-MedicalRecognitionConfigurationUiQuery.wsd`。
- 前置：标准项目互认配置（阶段 2，004）——本阶段的金额数据以其互认配置为前提。
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
| S3-D1 | 两个页面、两种信任边界：平台管理员的组织/医院/院区取自请求，医院管理员的组织/医院取自可信上下文、院区取自请求；服务端只从可信上下文取操作人 | DesignConfirmed | SRS F10 参与者与备选流 1；`CONTEXT.md`「角色与菜单授权」；`docs/plans/001-总体计划/design.md` 4.5 | Contracts、Application、Client | V8-V15、V22、C1-C3 |
| S3-D2 | 本项目不维护角色权限模型、不做权限二次确认；菜单与授权由权限系统负责，本项目只交付菜单名称与路由；但保留组织、医院、院区的存在、启用与父子归属校验 | DesignConfirmed | `CONTEXT.md`「角色与菜单授权」；SRS:298 | Application、Client、文档 | V8-V13 |
| S3-D3 | 金额保存为**一个 Command**，由两个 Application 入口调用，共用同一 `OrganizationHospitalBranchRecognitionAmountSavedEvent`（两个入口在 Application 解析完成后的领域数据完全相同）；**UML1 不新增第二条 Command** | DesignConfirmed | [backend-command-query-event](../../../.agents/instructions/backend-command-query-event.md) 第 1 节要求 Command 与领域意图一一对应；UML1 已由两个角色指向同一命令 | Domain、Application、UML1 | V1-V7、V25 |
| S3-D4 | 金额保存按业务键定位：不存在则新建、存在则覆盖 `current_amount`；`0.00` 是「已配置」；同值重复保存按成功处理、更新操作信息并登记事件，不做无变更短路 | DesignConfirmed | SRS F10 主事件流 5 与业务规则 1-2；既有"资料未变化时保存仍提交并登记事件"口径 | Domain、Application | V1-V7、V25 |
| S3-D5 | 查询行集由 `mrec_mutual_recognition_item` 驱动，金额表按业务键 LEFT JOIN；未配置行 `IsAmountConfigured=false`、当前金额无业务值 | DesignConfirmed | SRS F10 前置条件 3 与业务规则 1；UML2 模块说明 | Query | V17、V20 |
| S3-D6 | 列表只返回标准项目编码与名称集合，不返回组织/医院/院区编码，也不返回最后修改时间与最后修改人 | DesignConfirmed | SRS「用户界面」章「互认项目金额维护」页面要求 2 与 5；UML2 `RecognitionAmountReadModel` | ReadModel、UML2、Client | V18、V26 |
| S3-D7 | 名称与归属一次性批量读取后在内存映射填充，调用次数不随返回行数增长 | DesignConfirmed | [backend-architecture](../../../.agents/instructions/backend-architecture.md) 第 5.3 节；`Dy.LisCenter` 的 `LisCenterOrganizationPathResolver` 既有做法 | Application、Query | V19 |
| S3-D8 | 金额查询不分页 | AcceptedRisk | 既有互认配置查询的不分页口径；重审条件为出现可复现卡顿、超时或内存问题 | Query、Client | C22 |
| S3-D9 | 实体语义与注释按 F10 修正为「当前金额维护」，写入侧不做累加；不改 UML1 实体定义 | DesignConfirmed | SRS F10；UML1 实体只声明「当前金额」；现有实体注释与二者及同族 DTO 矛盾 | Domain、注释 | V28 |
| S3-D10 | 前端引入 `@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39`，并在 `client/pnpm-workspace.yaml` 增加 kiota overrides 统一到 `1.0.0-preview.103` | DesignConfirmed | 项目不自行实现组织/医院/院区选择控件，复用既有组件；`@dy/api-client-base@0.1.28` 精确依赖 `@dy/auth@0.1.28`，与项目现有 `@dy/auth@0.1.39` 会形成同一包的双实例，故取 `@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39` 这一组合 | Client、依赖 | C23-C25 |
| S3-D11 | 医院管理员入口的可信上下文由**登录令牌声明与当前登录用户档案共同构成**：组织层与医院层按同一规则解析——令牌该层非空白即取令牌值；令牌该层缺失或空白时用当前登录用户档案补齐；令牌与用户档案都提供该层且不一致即拒绝；补齐后该层仍为空即拒绝，不使用默认值、不降级为空值；用户标识无法解析为非空 `Guid` 或读不到用户档案同样拒绝。**院区不做可信授权回落**：院区仍取自请求，由既有 `OrganizationPathResolver` 校验其属于可信医院（S3-D19） | DesignConfirmed | `CONTEXT.md`「组织、医院和院区」「角色与菜单授权」；SRS F10 前置条件 2 与备选流 1；`Dy.LisCenter` 的 `LisCenterCurrentBranchScopeService` 既有做法（令牌优先、缺失层由用户资料补齐、冲突即拒绝） | Application | V14 |
| S3-D12 | 首次并发保存同一业务键时以唯一索引兜底，数据库异常按原样向外传播，不做冲突翻译、不自动重试 | DesignConfirmed | SRS F10 未定义并发语义；`backend-command-query-event` 第 4.1 节要求说明并发兜底；并发冲突的呈现形态不在本阶段范围内 | Repository、Application | V16 |
| S3-D13 | 本阶段不处理生产配置：只改 `config.development.json`，不动 `config.json`，也不登记生产端口分配 | DesignConfirmed | 本阶段只面向开发环境；生产部署配置未纳入交付范围 | Client | C25 |
| S3-D14 | 可空枚举筛选形态：**本阶段页面不下推枚举筛选**，不手改生成物；生成管线侧由对应阶段管理 | AcceptedRisk | 本阶段页面不经枚举筛选，故不受该形态影响 | Client | C24 |
| S3-D15 | server 侧允许新增 `Dy.Base.Application.Contracts` 引用与中央版本条目 | DesignConfirmed | S3-D11 的组织服务校验需要该契约；依赖闭包与版本约束已按元数据核对，**未实测构建** | Application、`Directory.Packages.props` | V8-V10、V13-V14、V19；批次 1 以实测 restore/build 确认 |
| S3-D16 | 新增 `src/runtimeConfig.ts` 承载 `apiBaseUrl` 与 `baseApiBaseUrl`，入口调用一次 `configureBaseComponents`；只在 `public/config.development.json` 增加 `baseApiBaseUrl` | DesignConfirmed | 与既有子应用运行时配置结构一致；生产配置见 S3-D13 | Client | C25 |
| S3-D17 | 平台管理员入口的保存与查询**取值口径一致**：组织、医院、院区一律取请求提交值，并校验其存在、启用与父子归属；医院管理员入口的组织与医院取自可信上下文。**「只信可信当前组织」口径不延伸至平台管理员入口** | DesignConfirmed | SRS F10 前置条件 2「由可信上下文确定或按平台管理员授权选择」；SRS「用户界面」章页面要求 1 | Application、Query、UML2 | V29 |
| S3-D18 | 查询按标准项目编码升序；未配置行 `CurrentAmount` 契约类型可空、值为 `null` | DesignConfirmed | SRS「用户界面」章页面要求 4 要求区分未配置与零元；UML2 约定未配置时当前金额无业务值 | ReadModel、Query | V18、V20 |
| S3-D19 | 医院管理员入口的院区范围：只允许**可信上下文所属医院下的院区**，提交其他医院或不属于该医院的院区即拒绝；**不引入「院区级数据权限」概念**，服务端不做该层校验（权限由权限系统负责，见 S3-D2） | DesignConfirmed | SRS F10 备选流 1；UML1 命令规则 4；`CONTEXT.md`「角色与菜单授权」 | Application、Query | V30 |
| S3-D20 | 平台管理员入口的**跨组织真实宿主面降级**：V29 以应用层证据为准并记 `AcceptedRisk`，真实宿主面记 `Blocked`；不因此改变取值口径（S3-D17） | AcceptedRisk | 环境无第二个组织，跨组织真实面不可构造 | Application、Query、Client | V29 |
| S3-D21 | 列表返回**枚举中文**：`RecognitionAmountReadModel` 新增只读计算属性 `ItemTypeText`（可空，取值未登记时为 `null`）与 `ConfigurationStatusText`（非空，未登记取值按严格解析抛出）；文本由服务端按枚举 `Description` 解析，与阶段 2 的 `S2-D15` 同口径，前端不再本地硬编码枚举中文 | DesignConfirmed | [总体计划设计](../001-总体计划/design.md) 5.3 枚举协作契约（适用于全部后续阶段）；阶段 2 `S2-D15` 既有先例 | ReadModel、UML2、Client | V18、V26、C5 |

S3-D1、S3-D6、S3-D18 的变化已同步 `docs/需求规约SRS.md` 与 `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd`；S3-D9 是注释与语义修正，不改变 UML1；S3-D3 确定为一个 Command + 两个 Application 入口，UML1 不新增命令；S3-D17 的取值口径已落 `Server/design.md` 的入口取值段与保存/查询链；S3-D19 的院区范围口径已同步上游（`CONTEXT.md` 两处、UML1 命令规则 4、SRS F10 备选流 1 与参与者表/「用户界面」页面要求 1、`docs/流程图.md`、`docs/业务流程图.md`）；S3-D20 的降级口径已落阶段根 `testReport.md` 的「已确认的验证降级」节与 `Server/design.md` 的 V29；S3-D21 的读模型字段变化已同步 `docs/uml/2-MedicalRecognitionConfigurationUiQuery.wsd` 的 `RecognitionAmountReadModel`（SRS 只规定展示字段、不列字段清单，无需改动）。**本台账是阶段 3 决策的唯一权威来源**。

## 验收与停止条件

- 后端：两个保存入口、两个查询入口、领域规则、DDL/SqlMap 映射与事件口径均有测试证据；后端测试前矩阵见 [Server/design.md](Server/design.md) 的验证矩阵章节（V1-V30）。
- API Client：按累计规则生成并通过契约、typecheck 与 build 核对，保留阶段 1、阶段 2 已交付接口与 `/auth/login` 排除规则。
- 前端：两个页面从宿主登录页与菜单进入完成真实验收；本地直达、构建成功与接口直测不替代宿主验收。前端矩阵见 [Client/testPlan.md](Client/testPlan.md)。
- 目标数据库/schema 未确认或表未建成时，依赖新表的集成项记 `Blocked`；不以空列表、构建成功或接口直测替代业务通过。
- 超出授权或影响边界不明时停止：需要修改既有已交付接口语义、需要引入服务端角色权限模型、需要改动其他系统数据或对象、或需要在前端扩散生成的 Kiota 类型时，先暂停该范围并请求项目负责人决策。
