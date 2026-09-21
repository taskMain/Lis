# Project Context

本文只维护技术与仓库事实，不复制业务规约。环境值统一维护在 [Test Environment](test-environment.md)。

## 1. 当前事实

| 项目 | 当前值 |
|---|---|
| 仓库名 | `Dy.MedicalRecognition` |
| 系统名称 | 检验检查结果互认平台 |
| 当前分支与跟踪分支 | `main` / `origin/main`；比较前重新核实基线 |
| 技术架构 | C# / Dy Framework 后端；React / TypeScript / Vite / Ant Design 微前端；OpenAPI / Kiota API Client |
| 开发模式 | 分阶段设计、实施、测试与真实证据；按需多子代理协作 |
| 浏览器策略 | 不固定浏览器、Profile 或控制工具，必须满足真实宿主验收条件 |
| 技能 | 本轮不迁移、不创建；负责人后续重新制作，现有技能保持原样 |

技术架构沿用已确认方案，具体依赖版本、数据库 Provider、包名和命令在项目初始化时按实际配置登记；不能把架构一致当作框架运行行为或数据库兼容性已经验证。

## 2. 业务权威入口

持久化表默认包含操作人 `oper_id` 和操作时间 `oper_time`，实体属性为 `OperId Guid`、`OperTime DateTimeOffset`；UML 实体省略这两个公共字段不代表取消该约定。应用服务从可信 `HttpRequestInfo.UserId` 解析操作人，映射 Command 时补入 `OperId` 与 `DateTimeOffset.UtcNow`，不接受调用方伪造。参考 LisCenter 的 `ReceiverBranchLabItemCapabilityAppService.StandardItem.cs`。操作字段表示该行最后一次实际维护，不表示整个目录版本；公共查询需要返回时显式声明同名字段，不另造 `LastUpdatedTime` 别名。新建平台表的时间读写与 Provider 映射须按阶段验证；历史观测到的其他对象无时区列不是本平台类型兼容或迁移依据。

- [需求规约 SRS](../../docs/需求规约SRS.md)
- [领域与接口模型](../../docs/uml/)
- [业务上下文](../../CONTEXT.md)
- [阶段文档编写说明](../../docs/plans/README.md)（阶段清单见 [总体计划设计](../../docs/plans/001-总体计划/design.md)）

按任务读取相关章节，不默认通读全部业务材料。`docs` 中的参考文档和其他系统模型不具有当前业务规则的优先级。

本项目已确认的业务科室/人员规则与通用平台身份规则不同：医院业务请求中的科室和人员 ID 是字符串，名称随请求保存，不要求存在于平台权限系统，不按 ID 补查或覆盖业务名称。组织、医院和院区仍由可信上下文确定并校验。权限系统负责菜单和接口授权，本项目不重复建设角色授权模型，但仍保护数据归属范围。具体字段、必填和统计规则以 SRS/UML/CONTEXT 为准。

## 3. 目录和运行命令

| 概念层或工具 | 目录或状态 |
|---|---|
| 后端根 | `server/`；已由 dy-codegen 生成 Apron 六项目骨架（PostgreSQL 方言） |
| Application.Contracts（DTO/Request/ReadModel/AppService 契约） | `server/Dy.MedicalRecognition.Application.Contracts/`；`net10.0` |
| Domain | `server/Dy.MedicalRecognition.Domain/`；实体、命令、Manager、仓储接口 |
| Domain.Share | `server/Dy.MedicalRecognition.Domain.Share/`；枚举与领域事件 |
| Repository | `server/Dy.MedicalRecognition.Repository/`；Earthrace SqlMap `.xml` 与 `Scripts/*.sql` DDL |
| Application | `server/Dy.MedicalRecognition.Application/`；AppService 与 DataMaps |
| Host | `server/Dy.MedicalRecognition/`；Minimal API 宿主，未生成 endpoint 业务代码 |
| Tests | `server/Dy.MedicalRecognition.Tests/`；xUnit smoke test |
| 解决方案 | `server/Dy.MedicalRecognition.slnx` |
| 前端根 | `client/`；pnpm monorepo，仅含 `apps/dy-medical-recognition` |
| 前端包名与 basename | 包名 `dy-medical-recognition`；微应用 code `medical-recognition`；basename `/subApps/medical-recognition` |
| 前端构建与测试命令 | 在 `client/` 下：`pnpm -F dy-medical-recognition dev` / `build` / `lint` / `test`（`test` 为 vitest，当前无测试文件） |
| `@dy` 私有 registry | `client/.npmrc` 配置 `@dy:registry=http://192.168.1.19:4873/` |
| API Client 与生成命令 | `client/packages/api-client-medical-recognition/`，包名 `@dy/api-client-medical-recognition`；由 Kiota 从服务 OpenAPI 生成，`prepare-openapi` → `generate` 见下节 |
| 测试辅助服务 | 按阶段需要建立，当前没有实现 |
| GitNexus | 使用已安装 CLI 或可用 MCP；仓库选择、初次索引与无代码边界见 GitNexus Workflow |

初始化代码时同步更新本节，不在根 `AGENTS.md`、多个测试规范或子代理提示中重复硬编码启动命令。缺少命令时检查实际工程配置；没有工程时不得虚构可执行命令。

### 3.1 构建与启动命令状态

| 命令 | 状态 |
|---|---|
| `dotnet restore server/Dy.MedicalRecognition.slnx` | 可用（7 个项目全部还原成功）；私有源不可达时输出 `NU1900` 漏洞数据警告，不影响还原 |
| `dotnet build server/Dy.MedicalRecognition.slnx` | 可用：0 错误；351 条 `CS8618` 警告全部来自生成的 ReadModel `record` 非空属性，属生成器特性 |
| `dotnet test server/Dy.MedicalRecognition.slnx` | 可用：1 个 xUnit smoke test 通过（`Generated_contract_is_available`） |
| 后端启动命令与监听地址 | 已实际验证：监听 `http://localhost:15014`，OpenAPI `/openapi/v1.json` 与 Scalar `/scalar` 均可访问。启动配置三处一致（`appsettings.json`、`appsettings.Development.json`、`Properties/launchSettings.json`）。业务接口当前不可用，原因见 §3.4 |

验证以上命令时必须在 `server/` 目录内执行，或显式带上解决方案路径，使 `server/global.json` 生效。包与 SDK 的版本结论见 [Test Environment](test-environment.md)。

### 3.2 后端生成来源与再生成

`server/` 由 `dy-codegen` 从 `docs/uml/` 的 UML 确定性生成，不是手工搭建。

再生成顺序：`adapt-wsd` → `validate-model` → `plan` → `validate-plan` → `generate`，命令形态见 `dy-codegen` 技能。目标闭包会补齐硬依赖，扩展目标必须在 `--target` 中显式选择。

生成范围只覆盖标准 CRUD，当前 54 项操作被局部阻断（Facade、复杂入参、非标准 CRUD、ReadModel 投影查询）；报告生命周期、匹配、处理结果、引用和全部查询尚未进入生成范围，需要后续设计与实现。

**再生成前必须保护的人工改动**：`server/global.json`（SDK 版本）、`server/Directory.Packages.props`（Dy 框架包版本）、`server/Dy.MedicalRecognition/appsettings.json`、`server/Dy.MedicalRecognition/appsettings.Development.json`、`server/Dy.MedicalRecognition/EarthraceConfig.json`（端口、数据库连接、外部系统地址）和 `server/Dy.MedicalRecognition/Properties/launchSettings.json`（启动地址）均已按负责人决策脱离生成器基准值。上述配置与业务生成文件在具体命令下是拒绝、覆盖还是要求先恢复，尚未逐类确认；再生成前须对照生成器实现和命令选项核实并记录维护方式，保护人工内容。未确认前不重跑 `generate`，不以实际覆盖来试错，也不概括为“一律拒绝”或“一律覆盖”。

### 3.3 前端 API Client 生成

`@dy/api-client-medical-recognition` 由 Kiota（.NET 全局工具 `microsoft.openapi.kiota`，当前 1.34.1）从后端 OpenAPI 生成，输入是后端运行时的 `/openapi/v1.json`（Scalar 页面 `sources[].url` 所引用的文档，不是 Scalar 页面地址）。

| 步骤 | 命令或事实 |
|---|---|
| 取原始文档 | `GET http://localhost:15014/openapi/v1.json`（需后端已启动） |
| 兼容修正 + 固化输入 | `pnpm -F @dy/api-client-medical-recognition prepare-openapi`，产出 `openapi/medical-recognition.openapi.json`，修正 ASP.NET 的 `["integer","string"]` 联合类型 |
| 生成源码 | `pnpm -F @dy/api-client-medical-recognition generate`，固定带 `--exclude-path /auth/login` |
| 构建与检查 | `pnpm -F @dy/api-client-medical-recognition build` / `typecheck` |

已有生成基线记录为当时后端已暴露的全部接口（`MedicalRecognitionReport` 分组的 16 个写命令，暂无查询接口），不代表这些业务能力均已交付，也不作为后续全量生成指令。同一包后续按“已交付范围 + 本阶段稳定契约”累计生成，保留已交付接口，不提前生成后续功能；阶段中的“本模块生成”受 [Frontend API Client](frontend-api-client.md) 的累计规则约束。

`generate` 会覆盖 `src/` 下的手写文件，`src/index.ts` 需在生成后核对；不要添加 `--clean-output`，否则会直接删除该入口。`kiota-lock.json` 的 `descriptionLocation` 指向包内 `openapi/` 稳定文件，`excludePatterns` 必须保留 `/auth/login`。

### 3.4 当前已知阻断（在所属阶段内优先处理）

以下区分历史实测与用户最新澄清；本轮只修订文档，后续进入阶段开发时按当前设计处理，不要把它们当成已完成能力。

**1. SqlMap 语句解析失败，所有生成接口返回 500**

实测：带合法宿主 token 调用 `POST /Api/MedicalRecognitionReport/CreateMedicalStandardCategory` 返回 `HTTP 500`，异常为
`Dy.Earthrace.Exceptions.EarthraceException: Can not find Statement. FullSqlId:MedicalRecognitionReport.CreateMedicalStandardCategory`。

历史取证记录了以下生成物与对照项目的差异；这些差异提供候选根因，不足以证明聚合作用域本身必然导致失败：

| | 同类可用项目 | 本项目生成物 |
|---|---|---|
| Repository 作用域传参 | 显式传**实体名**（如 `MedicalStandardItem`），不调用 `SetContext` | `SetContext` 传聚合名（`MedicalRecognitionReport`） |
| SqlMap `Scope` | **实体名** | 聚合名 |
| Repository 文件 | 每个实体一个 | 19 个实体共用一个 `MedicalRecognitionReportRepository.cs` |
| `InsertAsync`/`UpdateAsync` | 对照调用省略 `sqlId:`，走框架默认语句路径 | 显式传业务化 `sqlId:` |

已观测到的事实是查找键 `MedicalRecognitionReport.CreateMedicalStandardCategory` 未找到语句。框架按 `{作用域}.{语句 ID}` 查找；多个实体共用作用域不等于语句必然冲突或缺失。历史“共用聚合作用域是失败的直接原因”推论已被替代，不得据此实施。

**当前选定方案（[阶段 1 设计](../../docs/plans/003-阶段1-标准项目目录维护/design.md)，S1-D1/S1-D2）**：本模块写入 SqlMap 的 `Scope` 改为实体名，仓储在**每个 `DataMapper` 调用点显式传 `scope:`**，**不调用 `SetContext`，不按实体拆仓储类**。常规增改继续显式传业务化 `sqlId`，与 XML 的 Statement Id 对齐，不改成框架短名、不依赖仓储方法名推断。避免 `SetContext` 是为消除共享映射器可变作用域依赖，不表示已证明它就是历史异常的原因。

**尚未核实**：本项目运行时的实际作用域来源与语句注册结果尚待探测。阶段 1 实施首批次必须输出**实际查找键与已注册语句键清单**，区分「语句未注册」与「语句注册在别的键下」；探测不支持候选解释时暂停相关修复并修订设计。跨项目（`Dy.MedicalStandardCatalog`、`Dy.LisCenter`）证据不能替代本项目运行验证，选定方案不等于修复已通过。

**默认语句名的证据边界**：历史 `InsertAsync → Insert` 推导尚未附齐属性位置、参数省略调用点、内部转调及去后缀处理的调用链证据，不能称为仅由元数据确认的事实。本阶段显式传 `scope/sqlId`，不依赖该默认推导；仓储对外方法名无需与显式语句 ID 同名。需要分析默认路径时再补对应证据，不改变既定运行探测闸门。

阶段 1 进入实施时按上述修法处理；本阶段只处理标准目录相关实体，其余实体随各自阶段处理。

**2. 数据库表缺失**

用户于 2026-09-11 澄清本平台尚未建立数据库表：19 张平台表按所属阶段独立新建，物理表名为 `mrec_` 加已选业务基础表名，不强制与实体名完全一致；实体名不带前缀，物理表通过 SqlMap 显式对应。各阶段新建的表对象与完成状态记录在对应阶段文档与阶段 `testReport.md`，不在本节维护。此为用户确认的基线，不是数据库查询结果。

阶段 0 曾在配置库中观测到不带前缀的 `medical_standard_category`、`medical_standard_group`、`medical_standard_item`，并记录类型差异与 14 个索引。观测证据保留，但这些是归属未确认的既存对象，不是本平台共享或既有资源；旧有“3/19 已建”、整型布尔转换、保留其索引及旧表迁移方案均被本次澄清覆盖，不再执行。连接配置不改，同库不等于同表；初始化前须由负责人确认目标数据库与 schema，并执行最终 DDL，完成确认前暂停依赖新表的集成测试。不得改名、复制、迁移或修改其他系统的表及数据。

**已确认可用的部分**：token 链路正常 —— 不带 token 返回 401，带宿主登录 token 能到达业务层（500 而非 401）。

## 4. 本项目 DDL 脚本规则

本规则适用于全平台 19 张表的最终脚本，依据[阶段 1 设计](../../docs/plans/003-阶段1-标准项目目录维护/design.md) S1-D30（脚本格式，`DesignConfirmed`）及用户本轮确认的 S1-D31 独立新建 `mrec_` 表口径；原前缀候选与共享表切换待定口径已被覆盖。实现仍按所属业务阶段：阶段 1 仅处理 3 张标准目录表，后续 16 张随所属阶段处理，不新开横向治理阶段，也不在本轮修改 SQL、代码或数据库。

1. `CREATE TABLE` 列顺序为：主键 `id` 第一；其余业务列按 UML 实体属性顺序；是否作废、逻辑删除、启用等生命周期状态标志集中放在操作字段之前，同组保持 UML 相对顺序；最后严格为 `oper_time`、`oper_id`。按业务语义识别状态标志，不把所有 `bool` 业务属性机械移尾。
2. 表及字段中文注释以对应 UML 实体、属性为依据；UML 未画出的公共字段按约定补齐：`id` 为“主键ID”、`oper_time` 为“操作时间”、`oper_id` 为“操作人”。不因补注释擅改业务含义。
3. 列顺序适用于平台独立新建的 `CREATE` 脚本，中文注释随最终 DDL 交付；不重排、重建既存对象，也不向归属未确认的三表提交注释元数据迁移。
4. 已确认全平台物理表名为 `mrec_` 加已选业务基础表名，当前三表保留现有脚本基础名；实体名不带前缀，SqlMap scope 与 API 名不因物理表前缀改变。实体与物理表通过 SqlMap 显式对应，不强制命名推导，不将 `Entity` 后缀等机械搬入表名。各阶段须核对物理表映射仅指向本平台表，不复用、改名或复制历史三表。
5. 新表所需索引与约束由所属阶段按业务契约明确，不继承归属未确认三表的 14 个索引，也不执行其整型布尔转换。不因脚本格式调整扩大字段结构或数据类型范围。连接配置保持不变，实际初始化前确认目标数据库与 schema，由负责人执行最终 DDL；不得触碰其他系统对象或数据。
6. 已建表的任何表结构变更（含新增索引、加列、改类型）必须双写落地：既提交迁移文件（`Repository/Scripts/Migrations/`，命名参照 `YYYYMMDD_<票号或阶段>_<描述>.sql`，必要时配 `_preflight` 变体），也同步更新对应实体的建表脚本（`Repository/Scripts/mrec_*.sql`），两处不得漂移；否则按最终建表脚本重建时会丢失变更。迁移文件由负责人执行，执行前依赖变更的验证条目不记通过。
