# 阶段7 测试报告

**当前状态：阶段 7 收口（2026-09-23，`Complete`；发布结论 `ReadyWithAcceptedResiduals`）。** 设计已确认定稿并完成两轮三方独立审查整改（[设计](design.md)、[后端设计](Server/design.md)、[前端设计](Client/design.md)、[前端矩阵](Client/testPlan.md)）。票00 开工闸门核对与分母冻结已完成（核对登记见下文章节），批次0 票01 排除项回归（V1-V7）已完成并通过实施审查整改复核（见下文专节）；批次1、批次2 已完成（票02 至票05，2026-09-23，票01 至票05 均有独立审查复核记录，票00 为核对登记类无独立审查项）；批次3 已完成（票06，2026-09-23，含票06 独立审查复核）——阶段 7 全部批次完成，负责人确认收口。本文件随各票完成逐段登记。

测试前矩阵见 [后端设计](Server/design.md) 的验证矩阵章节（V1-V33）与 [前端测试前矩阵](Client/testPlan.md)（C1-C22）；批次与矩阵编号的对应见 [实施](impl.md)、[后端实施](Server/impl.md) 与 [前端实施](Client/impl.md)。

## 执行边界

- 开工闸门：阶段 6 收口（`Complete`）前不执行任何批次。开工时冻结分母：本阶段 V1-V33、C1-C22 与各阶段现行矩阵非干净条目的过堂清单（构成与定级口径见 S7-D2）。
- 非干净条目过堂三出口（定级）：可补证据的转 `Passed`；经范围确认不适用于本轮的转 `N/A`；属本轮范围但不可构造或环境受限的保持原状态并逐条登记 `AcceptedRisk`（定级残余）。过堂清单及定案记录登记于本文件（注明原阶段用例编号），原阶段矩阵状态不改写。
- 既有 `Passed` 条目的复用按阶段、按验证面分项登记复用条件（影响范围、证据版本与环境、时间、复用理由），收口时分类列出复用项、失效项与修复后重测项。
- 数据形成口径：走查数据经既有接口与页面按业务顺序构造；不构造正常业务不可达的用例；开发期冗余、孤岛或错误数据不作为缺陷处理。
- 框架层事务控制不构造强制失败用例；数据库错误原样抛出，属框架行为。
- 失败路径与边界复用各阶段既有证据时，复用条件按阶段、按验证面分项登记（影响范围、证据版本与环境、时间、复用理由），与既有 `Passed` 条目的复用登记口径一致。
- 宿主不可用时依赖宿主的走查条目记 `Blocked` 并走定级残余出口，不降级为本地验收。

## 票00 开工闸门核对与分母冻结（2026-09-22 完成）

### 开工闸门核对

| 项 | 核对结果 |
|---|---|
| 阶段6收口（硬前置，S7-D2） | 阶段6 根测试报告（[008-阶段6-互认统计与导出/testReport.md](../008-阶段6-互认统计与导出/testReport.md)）首行登记「阶段 6 收口（2026-09-22）」，票00-09 全部完成并通过实施审查；负责人本轮指示进入阶段7实施，闸门成立 |
| 工作区快照 | `git status --short --branch` 输出 `## main...origin/main [ahead 6]`，工作区无未提交改动；本轮不创建分支、提交或 worktree，既有领先提交与全部既有内容保护不动 |

### 分母冻结登记（冻结时点 2026-09-22，S7-D2 口径）

分母 = 本阶段矩阵 55 条（V1-V33，出处 [后端设计](Server/design.md) 验证矩阵；C1-C22，出处 [前端矩阵](Client/testPlan.md)）+ 下方过堂清单 49 行（编号条目 48 行，其中 2 行为子面登记；无编号登记 1 行）。冻结后不增删；新发现失败场景优先映射既有冻结编号，无法映射的增补用例并记录重新冻结时点与理由（供票06 V30「矩阵冻结核对」核对）。

过堂清单收集自阶段2至阶段6现行测试报告的终态登记（同一编号在不同章节状态不一致时取最终登记；阶段5子编号对照该阶段 Server/design.md 验证矩阵，阶段6对照其 Server/design.md 与 Client/impl.md）。原阶段矩阵状态不改写，逐条定级在票04 执行，三出口见 S7-D2。

| 原阶段 | 编号 | 用例主题 | 现行状态 | 登记要点 |
|---|---|---|---|---|
| 阶段2 | V24 | 无WorkUnit生产入口事件登记/处理/提交可见性 | Blocked | 零 IEventHandler、无事件持久化表、无观察点；写入与失败响应面已实测 |
| 阶段2 | V26 | 并发同方向停用恰好登记一次事件 | Blocked | 事件计数子面无可控观察点；终态与幂等面已实测 |
| 阶段2 | V27 | 跨组织写使用其他组织配置ID应被拒绝 | Blocked | 构造不出第二个可信组织持有配置的前置；离线用例与查询侧越权拒绝已验证 |
| 阶段2 | C17 | 组织切换后目标组织读取失败 | Blocked | 组件层用例通过；宿主页面层缺第二个授权组织，不可构造 |
| 阶段3 | V29 | 平台管理员入口跨组织保存与查询 | Blocked | 环境仅组织01，跨组织真实宿主面不可构造（S3-D20） |
| 阶段3 | V30 | 医院管理员入口院区范围拒绝面 | Blocked | 允许面证据完整；拒绝面提交路径不可达（下拉只列可信医院院区） |
| 阶段3 | V16 | 并发首次保存同一业务键 | NotRun | 双标签页提交被串行化；应用层唯一冲突翻译用例覆盖 |
| 阶段3 | C3 | 医院管理员页范围选择 | Blocked | 无 branch_id 为空的测试身份，宿主层不可构造；组件层用例通过 |
| 阶段3 | C11 | 保存被拒绝的两类前置 | Blocked | 第二类前置（提交它医院院区）不可构造 |
| 阶段3 | C15 | 组织不可用 | NotRun | 无组织或医院缺失的测试身份；组件层替身用例覆盖 |
| 阶段3 | C22 | 范围真实数据量与不分页 | NotRun | 真实数据仅金额表11行，无业务规模证据 |
| 阶段3 | C23 | 组件加载失败提示 | NotRun | 宿主层注入点未命中；组件层替身用例覆盖 |
| 阶段4 | V85 | 并发首次提交同一报告业务标识 | PendingRetest | 原 Passed 证据基于旧唯一约束冲突翻译语义，已因设计变更作废，新语义无用例或真实入口证据 |
| 阶段4 | V86 | 并发首次解析同一证件 | PendingRetest | 同 V85；新语义撞证件键由唯一索引拒绝且异常原样传播，复测口径待补 |
| 阶段4 | 无编号 | 平台管理员入口跨组织真实宿主面 | Blocked（降级登记并附 AcceptedRisk） | 阶段4测试报告「已确认的验证降级」节登记，编号级状态清单无对应编号 |
| 阶段5 | V4 | 项目编码未纳入当前组织互认范围 | NotRun | 真实库面未取证，仅领域层替身证据 |
| 阶段5 | V6 | 项目编码标准目录层级不可用 | NotRun | 真实库面未取证 |
| 阶段5 | V11 | 两个时间均相同时稳定排序 | NotRun | 业务不可达：需同一基准时间与报告时间的两条独立候选 |
| 阶段5 | V13 | 匹配基准时间晚于查询时点 | NotRun | 业务不可达：报告提交拒绝未来时间 |
| 阶段5 | V14 | 报告已作废或版本非当前有效 | NotRun | 已由候选语句静态判据冻结（含变异证据），运行期无独立增量 |
| 阶段5 | V16 | 来源组织与接收组织不一致 | NotRun | 业务不可达：跨组织在MVP设计上不匹配；V16b/V16c 承接 |
| 阶段5 | V25 | 组内分别决定 | NotRun | 业务不可达：仅一条可用互认配置，构造不出组内多项 |
| 阶段5 | V33 | 不采纳原因非法 | NotRun | 请求校验值域拒绝已证；领域层同口径判定为防御分支，公开入口不可达 |
| 阶段5 | V38 | 幂等重试命中时版本失效不改判 | NotRun | 本轮未构造 |
| 阶段5 | V43 | 项目数组顺序不参与幂等比较 | NotRun | 单条匹配项的组无法改序，无观察对象 |
| 阶段5 | V44 | 未命中幂等时绑定版本失效整次失败 | NotRun | 既有报告超本院排除时长，前置未构造成功 |
| 阶段5 | V45b | 配置停用但绑定版本仍有效时保存成功 | NotRun | 本轮未构造 |
| 阶段5 | V48 | 引用详情有效期边界 | NotRun | 候选选优遮蔽较早决定，取不到越过参数值的记录 |
| 阶段5 | V53 | 绑定版本失效的已采纳项目不返回 | NotRun | 需同一就诊先后构造已采纳与版本失效，未构造成功 |
| 阶段5 | V54 | 报告已作废（引用详情侧） | NotRun | 已由候选语句静态判据冻结 |
| 阶段5 | V55 | 不采纳项目不返回 | NotRun | 本轮未构造 |
| 阶段5 | V56 | 自然超过可互认时间仍在有效期内 | NotRun | 真实库面归票08，票08未登记其取证 |
| 阶段5 | V58 | 同次就诊部分组超期或失效 | NotRun | 本轮未构造 |
| 阶段5 | V64 | 可信三值与接收三值不一致 | NotRun | 公开入口不可达：候选语句按可信三值过滤，另一身份查询目标行不返回 |
| 阶段5 | V68 | 实际引用时间等于两端边界 | NotRun | 上界已验；下界被既有引用事实先命中冲突判定 |
| 阶段5 | V71 | 提交时报告已更正或作废仍接受 | NotRun | 作废前置未构造 |
| 阶段5 | V77 | 既有语句清单断言的同步 | NotRun | 静态面由该阶段票02取证，票08不重复取证 |
| 阶段5 | V78 | 候选查询的集合参数写法 | NotRun | 语句文本已证；真实Provider展开行为未在真实库执行 |
| 阶段5 | V79 | 候选查询两个左连接与合并取值 | NotRun | 静态面已证；V79b 承接来源范围修正 |
| 阶段5 | V80 | 查询语句无方言特征 | NotRun | 静态面已证；票08不重复取证 |
| 阶段5 | V81 | 四个请求的字段集合 | NotRun | 声明与请求校验面已证 |
| 阶段5 | V85 | 枚举中文说明与登记 | NotRun | 读模型计算属性面由阶段6交付承接（阶段6已收口） |
| 阶段5 | V86 | 客户端路径与异常处理一致性 | NotRun | 静态面由该阶段票07取证 |
| 阶段5 | V87 | 事务声明 | NotRun | 静态面九处声明冻结（该阶段票07取证） |
| 阶段5 | V90 | 三个参数按平台级编码读取 | NotRun | 读取面以本地替身取证，不证明平台参数记录取值 |
| 阶段5 | V95 | 业务科室与人员负向要求 | NotRun | 处理结果与引用结果两侧负向面已证；终态列 NotRun |
| 阶段5 | V16c | 跨院区报告候选面（该阶段票09新增） | Blocked | 测试身份每医院仅一个院区，不可构造；静态与域层语义由 V79b 及读模型承接 |
| 阶段6 | V44（子面） | 接近10万行上限规模的真实导出 | 整条 Passed，子面 NotRun | 按 S6-D16 记 NotRun，随真实业务量增长复核 |
| 阶段6 | C30（子面） | 账号级跨组织范围隔离宿主面 | 整条 Passed，子面降级登记 | 按 S6-D18 降级登记（仅同组织两测试账号，不可构造） |

### C11 宿主菜单注册核对（2026-09-22 完成）

核对方式：Chrome 153（chrome-devtools MCP，隔离会话 c11-yangkj、c11-lisadmin，用后已关闭）从宿主登录页（`http://183.224.180.166:35000/login`，登录入口「检验检查结果互认平台」）分别以两身份登录，读取侧边栏菜单树，未进入业务页面。

两身份可见菜单完全一致：分组「检验检查结果互认」下 10 项——标准项目目录维护、互认项目、互认项目金额维护、本院区互认项目金额、报告管理与历史版本、本院报告管理与历史版本、接收医院互认使用统计、本院互认使用统计、来源医院被互认统计、本院被互认统计；清单外无本平台菜单项。

与设计页面清单（[前端设计](Client/design.md)「页面与交互」节，10 个）比对：一一对应，无页面缺失，名称差异 3 处——菜单「互认项目」对应清单「互认项目管理」；菜单「报告管理与历史版本」对应清单「报告管理（平台管理员）」；菜单「本院报告管理与历史版本」对应清单「报告管理与历史版本（医院管理员）」。差异属菜单文本与设计文本的措辞差异，页面与菜单映射无缺失；菜单文本是否调整由负责人裁定，不影响走查执行。

走查身份分配修正（票05 执行依据）：宿主菜单层两身份授权一致，均可见全部 10 项；按设计角色标注分配——标准项目目录维护、互认项目、互认项目金额维护、报告管理与历史版本（平台管理员面）以 lisadmin（组织01/医院CSYY2）走查；本院区互认项目金额、本院报告管理与历史版本（医院管理员面）以 yangkj（组织01/医院0101）走查；统计四页（双入口）两身份各走一遍；C13/C14/C15 两身份覆盖。

### 冻结例外口径（就绪登记）

新发现失败场景的取证优先映射既有冻结编号；无法映射的允许增补用例并记录重新冻结时点与理由（S7-D2）。已知需按此处理的条目：阶段4 V85/V86（并发唯一性语义变更后无用例，票04 按冻结例外映射或增补并记录重新冻结时点）。

## 结果

### 批次0（票01 排除项回归，V1-V7）

**静态面（V1-V5、V7，2026-09-22 完成，`Passed`）。** 新增守卫测试文件 `server/Dy.MedicalRecognition.Tests/Stage7ExclusionGuardTests.cs`（13 个测试，`dotnet test --filter "FullyQualifiedName~Stage7ExclusionGuardTests"` 13/13 通过；`dotnet build server/Dy.MedicalRecognition.slnx` 0 错误；`git diff --check` 干净；`gitnexus detect-changes` 返回「图查询失败，结果不完整；未检测到变更」，新增测试文件属尚未索引的新增 symbol，按 GitNexus Workflow 以设计与目标测试、构建建立基线，该结果不用作提交前检查通过依据）。逐条证据：

| 编号 | 守卫方式与结果 |
|---|---|
| V1 | 测试 `V1_public_operations_declare_no_excluded_capability`（应用服务公开方法与手工控制器动作名称集合实测 77 个，零命中调阅/审批/导入/更正/撤销/恢复/重启用/趋势/排名/绩效/同步/推送/采集/字典管理/下载记录等标识符）、`V1_frozen_openapi_paths_declare_no_excluded_operation`（固化 OpenAPI 快照 54 条路径零命中排除子串）、`V1_management_router_declares_exactly_the_delivered_pages`（前端 routes.tsx path 集合与 12 项清单精确等值）。覆盖 EX-02、EX-04、EX-07、EX-11、EX-16、EX-17、EX-20、EX-22、EX-24、EX-25、EX-27、DC-01（主取证 EX-04） |
| V2 | 测试 `V2_hospital_interface_routes_freeze_nine_capabilities_without_history_versions`（医院接入九项能力冻结为 OpenAPI 快照恰好 11 条路由，无任何版本列表/详情/历史 PDF 路由）、`V2_management_history_version_entries_stay_read_only`（三个历史版本入口无 `WorkUnit` 声明，对照写入口带 `WorkUnit` 判定非恒真）。覆盖 EX-23、DC-11（主取证 EX-23） |
| V3 | 测试 `V3_production_symbols_declare_no_excluded_capability`（六个生产工程 347 个 .cs 文件、排除 bin/obj，声明名语法节点级零命中）、`V3_production_sources_build_no_remote_address_and_never_compose_image_url`（无 http 字面量与 Uri 构造，`ImageAccessUrl` 不参与拼接，两测试带合成源码变异自校验）；grep 全文甄别：DICOM/PACS/Push/轮询/队列/前置机等 0 命中，「采集」17 处为能力名称与标本采集时间注释，LOINC 15 处为来源明细字段链（不代为映射），Charge/医保为收费编码字段链，Audit 0，「审计」26 处为操作审计列注释，Timer 2 处为子序列误命中。覆盖 EX-03、EX-06、EX-08、EX-10、EX-13、EX-15、DC-04、DC-06、DC-07、DC-09（主取证 EX-03/EX-10/EX-13/EX-14/EX-15/EX-17 按合并声明） |
| V4 | 测试 `V4_contracts_declare_no_process_field`（两个契约程序集自身声明属性零命中过程字段名集合；「就诊流水号」为既有业务字段、`EventId` 为框架 DomainEvent 基类继承属性，均排除）、`V4_critical_value_stays_a_single_source_flag`（Critical 起始成员恰为 `CriticalValueFlag` 一项）、`V4_amount_table_keeps_single_current_amount_columns`（金额表列集合精确等值 8 列，仅 `current_amount` 单值）、`V4_ddl_scripts_freeze_delivered_tables_only`（建表脚本与 19 个已交付表精确等值，无参数表/采集表/审计表等）、`V4_system_parameters_go_through_existing_parameter_service_only`（外部参数服务接口成员调用恰为 `GetSystemParameterByCodeAsync` 一项，三个参数编码常量既有断言继续有效）。覆盖 EX-05、EX-12、EX-14、EX-19、DC-10（主取证 EX-19） |
| V5 | 测试 `V5_export_contract_offers_single_xlsx_format_inheriting_query_filters`（两个导出请求无 Format 类属性，控制器 `ExcelContentType` 实测为 xlsx MIME，导出端点恰 2 条，导出类型枚举恰 7 值，快照全文无 csv 字样）；继承查询条件与响应组装由既有 `Stage6StatisticsContractTests.Export_requests_carry_type_dimension_and_filters_without_paging` 与 `Export_actions_assemble_no_store_file_stream_responses` 冻结；EX-25 由导出端点仅 2 条、既有明细读模型含 `RecognitionMatchRecordId`/`RecognitionMatchItemId` 冻结断言承接。覆盖 EX-18、EX-25、DC-08（主取证 EX-18） |
| V7 | 设计比对逐条一致（对照阶段5设计 docs/plans/007-阶段5-在线互认主流程/design.md 与阶段6设计 docs/plans/008-阶段6-互认统计与导出/design.md）：EX-01 每互认项目单报告单版本绑定；EX-09/DC-02 匹配请求只含证件与就诊字段，无姓名相似度/电话/院内卡替代；EX-17 汇总维度枚举四值冻结无趋势与医生维度；EX-21 引用详情纯查询、有效期逐次计算、链零写入；EX-26 候选查询按 `r.organization_code = $OrganizationCode` 限定来源（S5-D25）、本院版侧由可信上下文注入（S6-D1）；DC-03 每次非空匹配生成新记录、无跨查询稳定引用（S5-D20）；DC-05 查询与详情不形成决定与引用，事实只由两个显式提交入口创建 |

S7-D4 分流：无生产代码核对问题，未触发分流；守卫判据自身首跑 4 处甄别疏漏（路由根路径遗漏、框架基类继承属性误命中、应用服务私有包装、影像地址属性访问形态）已在测试内修正并补判别力证据。

残余：关键字扫描的固化面为声明名与语法节点形态，注释与字符串中的排除词表述由本轮 grep 甄别记录作补充证据，后续新增注释类表述无自动守卫；OpenAPI 快照与 routes.tsx 断言依赖既有冻结清单的端点变化同批同步纪律（`Stage2EnumContractTests` 54 条等值断言仍是路径全量守卫）。

V6（宿主行为证据）见下节。

**行为证据（V6，2026-09-22 完成，`Passed`，含一项事实登记）。** 取证方式沿用阶段6先例：Chrome 153（chrome-devtools MCP，隔离会话 v6-evidence，用后已关闭）从宿主登录页以 yangkj 登录，在宿主页面上下文携带会话令牌调用 `http://localhost:15014`（令牌只存在于页面上下文，未写入文件）：

- 导出响应内容类型（EX-18、DC-08）：`POST /api/v1/statistics-export/branch`，请求体 `exportType=1`（RecognitionUsageSummary）、`startTime=2026-08-01`、`endTime=2026-09-30`、`groupDimension=1`（Hospital）。实际 HTTP 200，Content-Type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`，响应 7225 字节。符合「仅 xlsx」。
- HIS 历史版本路由拒绝面（EX-23、DC-11）：携带同一令牌 `POST /Api/MedicalRecognitionReport/QueryMedicalReportVersionList` 与 `POST /Api/MedicalRecognitionReport/QueryMedicalReportVersionDetail` 均 404（路由不存在）；对照管理端查询组 `POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionList` 以真实报告标识调用返回 200 与完整版本读模型（先经 `QueryBranchMedicalReportList` 取得本医院范围内真实报告标识），历史版本查询能力只在管理端查询组存在。
- 事实登记：历史版本 PDF 能力存在两条承载路由——①`POST /Api/MedicalRecognitionReport/OpenReportVersionPdf` 为已声明端点（冻结 OpenAPI 快照含该路径），调用返回 500，响应为应用层业务拒绝「报告版本不存在或不属于该报告」（`MedicalRecognitionReportAppService.OpenReportVersionPdfAsync`），即该路由经框架自动端点可达，声明在写侧应用服务上、暴露在与管理端共享的路由前缀下；②`GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf`（`ReportPdfFileController` 手工路由，阶段4交付，医院接入面 11 条路由之一）指向同一应用入口。两条路由均按版本标识定位、已作废报告的历史版本仍可下载，语义为阶段 4 已确认决策 S4-D28（[阶段4设计](../006-阶段4-报告采集与生命周期/design.md)决策台账）。定性：历史版本查看下载是管理端与医院接入面 PDF 下载能力（九项之一）的既有交付形态；排除口径 EX-23/DC-11 的判定基准为医院接入九项能力清单，清单内无历史版本查询、列表与详情能力（V6 两条 404 与 V2 守卫一致），判定符合。守卫测试 `Stage7ExclusionGuardTests.V2` 的注释原表述「OpenReportVersionPdf 只挂管理端查询面」与快照事实不符，已按事实修正注释与 V2 摘要、把断言改为「共享前缀下无两个历史版本查询路由」的精确形态（S7-D4 分流第③类，文档与注释一致性修正；修正后全文件 13/13 通过）。
- 转入批次1 的核对输入：CONTEXT.md 报告历史版本段（:448 附近）要求匹配响应内的 PDF 下载入口须重新校验绑定版本仍为当前有效版本；本轮在阶段4/阶段5 testReport 中未检索到该下载路径的承接登记，该业务规则面在 EX/DC 清单口径之外，按 S7-D5 由票02 一致性核对（C22 承接存续核对）正式提出与定级。

### 票01 实施审查记录（2026-09-22）

审查范围：守卫测试质量、V2 注释修正复核、EX/DC 覆盖完整性（38/38 条逐条核对）、V6 定性复核、状态纪律、改动边界、语言纪律。审查结论：本体通过——13/13 复现、覆盖无缺项、合并声明主取证映射一致、V6 定性成立（`OpenReportVersionPdf` 语义为 S4-D28 已确认决策，可达性按 C-4 口径不构成权限问题，S7-D4 桶③分类成立）；整改项已全部处理：V3 登记数字 73 修正为实测 347（独立复算一致）、V6 事实登记补全第二条承载路由与 S4-D28 语义出处、测试文件禁用字「落」两处修正、V2 摘要精确化（S4-D28 出处）、testReport 状态行与批次表一致化。统一用语结论：测试文件 8 处与登记文字继续沿用设计清单既有用语「契约」（[Testing Baseline](../../.agents/instructions/testing-baseline.md)「契约测试」与阶段7设计清单表头「契约字段与表形态」为现行用语），不作改写。

V6 判定：`Passed`——拒绝面（历史版本查询路由 404 ×2）与内容类型（xlsx）符合设计；两条 PDF 承载路由的共享可达性作为事实登记转入「问题与残余风险」，按该节结论保留现状。

**`SourceGuardReuseTests` 登记缺口补登记（2026-09-22，等价修复，`Passed`）**：票01 新增的 `Stage7ExclusionGuardTests.cs` 引用共享语法工具但未登记进 `SourceGuardReuseTests.GovernedTestFiles` 清单，`Architecture_guards_reuse_the_shared_source_guard` 按「受治理清单与真实消费方必须精确一致」判据失败，是既往后端全量 `失败: 1` 的唯一失败项。影响分析：先重建索引（`gitnexus analyze . --name Dy.MedicalRecognition --index-only`，26.2 秒，`14,741 nodes | 30,107 edges | 322 flows`），`impact GovernedTestFiles` 仍报目标未命中（该私有测试字段不在代码图内），按 [GitNexus Workflow](../../.agents/instructions/gitnexus-workflow.md) §3 以源码检索确认边界：字段仅在 `SourceGuardReuseTests.cs` 内声明、同文件两处引用、无外部调用方，风险 `LOW`、范围 `exact`，未触发停止条件。修复：清单补登记 `Stage7ExclusionGuardTests.cs` 并附消费方登记注释。验证：`dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~SourceGuardReuseTests|FullyQualifiedName~Stage7ExclusionGuardTests" --nologo` → `失败: 0，通过: 18，总计: 18`；后端全量 → `失败: 0，通过: 781，总计: 781`（既往唯一失败项消失，总数不变）；GitNexus `detect-changes` → `medium`，12 文件 / 41 符号 / 4 条流程，无 `CRITICAL`，未触发停止条件。分类为 S7-D4 桶①等价修复（登记与实现一致，无行为扩散），批次 0 既有证据随该失败项消除更新为全量无失败。

### 批次1（票03 患者原值口径与阶段 4/6 文档同步，2026-09-22，V17-V19）

**口径依据**：S7-D8（[设计](design.md) 关键决策表）患者信息（姓名、证件号码、联系电话）全平台按业务原值展示，不实现任何脱敏；推翻 S4-D12 的「联系电话默认脱敏」子口径，承载面与页面布局不变（联系电话仍仅由报告版本详情承载）。同步范围全量清单见 [设计](design.md) §4。

**开工闸门与影响分析**：`git status --short --branch` 记录起始工作区 `main`、跟踪 `origin/main`、领先 6 个提交。修改既有 symbol 前按 [GitNexus Workflow](../../.agents/instructions/gitnexus-workflow.md) 执行 upstream impact：索引原为陈旧（索引提交 `980b354`，当前 `8a16c16`），按该规范 §3 在本项目根目录执行 `gitnexus analyze . --name Dy.MedicalRecognition --index-only` 重建（`14,741 nodes | 30,107 edges | 322 flows`）。`MaskPhoneNumber` upstream impact 结果 `LOW`、`exact`，直接调用方 1 处（`ReadCommonAsync`）、受影响模块 `Queries`、受影响流程 `QueryMedicalReportVersionDetailAsync`；与源码检索的单调用点事实一致，未触发停止条件。

**V17 口径实施（代码与自动化测试面，`Passed`）**

| 项 | 证据 |
|---|---|
| RED（后端） | 先只改测试期望：`dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Stage4QueryTests\|FullyQualifiedName~Stage4ContractTests" --nologo` → `失败: 2，通过: 47，总计: 49`；失败用例 `Stage4QueryTests.Version_detail_returns_content_for_the_report_type_with_original_phone` 两个 `[Theory]` 数据行，失败断言原文 `Expected: "13800001234" / Actual: "*******1234"`。RED 来自掩码实现仍在，非编译错误、非夹具错误（[Testing Baseline](../../.agents/instructions/testing-baseline.md) §1 第2条） |
| GREEN（后端目标） | 同过滤条件 → `通过: 49，失败: 0，总计: 49` |
| 票01 守卫回归 | `dotnet test server/Dy.MedicalRecognition.slnx --filter "FullyQualifiedName~Stage7ExclusionGuardTests" --nologo` → `通过: 13，失败: 0`（含 EX-18/DC-08 导出守卫）。掩码移除未破坏排除项回归 |
| 后端全量 | `dotnet test server/Dy.MedicalRecognition.slnx --nologo` → `失败: 1，通过: 780，已跳过: 0，总计: 781`（主会话独立复跑一致）。改动前基线同为 `失败: 1，通过: 780，总计: 781`，新增失败 0、既定失败消失 0、总数变化 0 |
| 前端全量 | `pnpm -F dy-medical-recognition test` → `Test Files 21 passed (21)`、`Tests 352 passed \| 2 skipped (354)`（主会话独立复跑一致） |
| 前端静态 | `pnpm -F dy-medical-recognition lint` → `3 problems (0 errors, 3 warnings)`，三条均为既存 `react-refresh/only-export-components` 告警，与本次改动无关 |
| `git diff --check` | 干净（exit 0） |
| GitNexus `detect-changes` | `medium`，11 个文件 / 38 个符号，4 条受影响流程；无 `CRITICAL`，未触发停止条件 |

生产代码改动：`server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` 删除 `MaskPhoneNumber` 方法与 `MaskedPhoneVisibleDigits` 常量，版本详情投影 `PatientPhoneNumber` 改为 `NullIfBlank(common.PatientPhoneNumber)`；同步 6 处 `Application.Contracts` 与读模型注释、`MedicalRecognitionReportQuery.xml` 一处注释（`v.patient_phone_number` 去掉脱敏表述，满足 V8「扫描命中为零」判据）。

**复用既有实现**：`MaskPhoneNumber` 原本承担两项职责——打星号，以及把空白归一为 `null`。同文件既有 `NullIfBlank`（`MedicalRecognitionReportQueryAppService.Reports.cs` 的私有静态方法）实现完全相同的去首尾空白与空串归 `null`，故改调用 `NullIfBlank` 即保住「来源未提供联系电话时返回空值、不补造字符串」判据（V74），未新增任何归一化辅助方法。对应空值用例 `Stage4QueryTests.Missing_phone_number_stays_null` 保持有效且未改动，其仍为 `Passed`。

**V18 阶段 4 文档同步（`Passed`）**：阶段 4 `design.md`（正文与 S4-D12 决策台账行）、`Server/design.md`（患者字段展示段、矩阵 V74 行）、`Client/design.md`（患者信息展示段）、`阶段4-规格.md`（用户故事 31、Implementation Decisions、接缝 3、S4-D12 决策索引）、`阶段4-Tickets/08-管理端报告列表查询与PDF下载.md`（What to build 与验收项）均已改到最终口径，按 [Planning And Evidence](../../.agents/instructions/planning-and-evidence.md) §4 只写最终状态，未新增「本轮修改」类章节、未保留新旧对照。S4-D12 依据列与现行 `docs/需求规约SRS.md`「通用界面要求」第 2、3 条（:2341、:2343：「页面中的患者姓名、证件号码、联系电话等敏感信息按业务原值展示；MVP 不实现患者脱敏」）及 S7-D8 出处一致。

**阶段 4 根 `testReport.md` 未改写**：该文件中的 `*******1111`、`*******1234`/`*******3333` 等记录属既有历史证据，按 Planning And Evidence §4「不得改写已经形成的历史证据」与设计 §4「同步不改变各阶段既有历史证据，只更新现行设计与状态」保持原样；本轮 V17 的真实链路重取证据以本报告「票03」节补记。

**V19 S6-D9 复核关闭（`Passed`）**：阶段 6 `design.md` 的 S6-D9 决策行为 `DesignConfirmed`，依据列含 S7-D8 出处（`docs/plans/009-阶段7-横向治理与发布收口/design.md` 关键决策表），「上游同步范围」段含复核关闭结论；阶段 6 `testReport.md` 的执行边界条目与残余风险表 S6-D9 行为关闭状态；该文件的历史测试结果行、计数与证据原文未改动。

**票03 独立实施审查（双轴，2026-09-22，本体完成，4 条发现已处理）**：Standards 轴 1 条发现——本轮新增文字含 [Agent Conduct](../../.agents/instructions/agent-conduct.md)「不允许使用的字」所列字符共三处（本节 V17 小节标题、票文件两条勾选项），均改写为「口径实施」；改后自查 `git diff -U0` 的命中为被移除的旧文行与以字名引用该字符的两行（票01 审查记录行与本段自身），受影响文件为本报告与票文件 2 个；票文件大标题、README、`Server/design.md` 既有同词保持原样（归 S7-D4 桶③）。Spec 轴 3 条发现（均归 S7-D4 桶③，登记一致性）：①根 `impl.md` 执行记录第三项勾选与批次表批次1「进行中」矛盾，改写为「完成当前批次已实施范围的实现与直接影响验证」；②票文件「同步动作登记（C20 输入）」改 [x] 并与根 `impl.md`「待办与交接」同步动作条目措辞一致；③票文件 V17 真实链路面勾选项补写证据写入位置（阶段7 根 testReport「票03」节补记，阶段4 根 testReport 按设计 §4 只追加不改写）。其余范围核对通过：S7-D8 同步范围穷尽 §4 清单、阶段4 testReport 历史证据未改写、V19 未动历史计数、前端生产代码未动、气味基线无命中。

**V17 真实链路证据与 C13 宿主核对（`Passed`，2026-09-22）**

取证环境：Chrome DevTools MCP（Chrome 153，MCP 自管实例）；宿主 `http://183.224.180.166:35000` 登录页登录，入口「检验检查结果互认平台」；两账号先后在同一浏览器会话登录（yangkj 先、lisadmin 后，其间经页面「退出登录」）；本地后端 `localhost:15014`、前端 `localhost:3008` 由本轮启动并实际监听；接口核对在 yangkj 页面上下文内携带会话令牌直调 `localhost:15014`，令牌未写入文件、未输出。

拦截生效证据：localStorage 两级启用——`dy-web-micro:dev-url-intercepts` = `[{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}]`、`dy-web-micro:dev-url-intercepts-enabled` = `"true"`；Console 出现 `[vite] connecting...` 与 `[vite] connected.`（`http://localhost:3008/subApps/medical-recognition/@vite/client`）；Network 子应用资源来自 `localhost:3008`（`@vite/client`、`src/main.tsx`、`src/pages/reportManagement/BranchReportManagement.tsx`、`src/pages/recognitionStatistics/*.tsx` 等，200/304）。首轮曾因前后端进程未运行出现 `localhost:3008` 连接拒绝，两服务本轮启动后复测通过；上述证据取自复测。

C13（两身份、平台与本院两侧入口、报告管理与统计页面患者姓名/证件号码、版本详情联系电话，全部原值、无掩码）：

| 面 | 身份与入口 | 患者字段证据 |
|---|---|---|
| 本院报告管理与历史版本 | yangkj（组织01/医院0101，可信范围只读展示「组织 01，医院 0101」） | 筛选患者姓名=林来源 → 共22份，行内 `林来源` / `530101198601012202`；未筛选首屏共62份，含 `跨院互认测试患者` / `530101199005120999`、`验收患者甲` / `530101199005120011`、`探针患者八` / `530101199001010108` |
| 本院版本详情电话 | yangkj | `MR-ACC-LAB-20260918-001` 第2版：患者联系电话 `13800001111`、患者姓名 `验收患者甲`、证件号码 `530101199005120011`；与库 `mrec_medical_report_version.patient_phone_number`（第1、2版均 `13800001111`）逐字一致 |
| 统计四页明细 | yangkj（双入口各页） | 本院互认使用统计（明细共78条）：`跨院互认测试患者` / `530101199005120999`、`林来源` / `530101198601012202`；本院被互认统计（明细共28条）：`林来源` / `530101198601012202`；接收医院互认使用统计：`跨院互认测试患者` / `530101199005120999`；来源医院被互认统计：`陈互认` / `530101198501012201` |
| 平台报告管理与历史版本 | lisadmin（组织01/医院CSYY2；页面按组织、医院与院区查询） | 县医共体/县人民医院/总院 + 患者姓名=林来源 → `林来源` / `530101198601012202`；切测试医院二/测试院区2-1 筛选 `MR-ST5-09-CROSS-CSYY2-001` → `跨院互认测试患者` / `530101199005120999` |
| 平台版本详情电话 | lisadmin | `MR-ST5-09-CROSS-CSYY2-001` 版本详情：患者联系电话 `13800009999`、患者姓名 `跨院互认测试患者`、证件号码 `530101199005120999`，与库基线一致 |

数据说明：本院报告列表中 `?????` 患者姓名行（如 `MR-ST5-STALE-20260920-001`）经库核对为存量数据原值（`mrec_medical_recognition_report.patient_name = '?????'`），页面按原值展示，属开发期数据，不作缺陷。`MR-ST6-*` 系列无联系电话，作「无联系电话返回空值」参照，未另取证。

V17 真实链路四查询（yangkj 页面上下文携带会话令牌直调 `localhost:15014`，与库基线逐字比对）：

| 查询 | 请求要点 | 返回与库基线比对 |
|---|---|---|
| `QueryBranchMedicalReportList` | branchCode `0101001`、patientName 林来源、page 1×5 | `page.totalCount=22`；样本 `MR-ST6-S-20260922-22` / `林来源` / `530101198601012202` 等，与库22行一致 |
| `QueryMedicalReportList` | organizationCode `01`、hospitalCode `0101`、branchCode `0101001`、patientName 林来源 | 样本同上，患者字段原值一致 |
| `QueryMedicalReportVersionList` | reportId `3a23c2c4-1507-5079-d224-05d617d4699f` | 返回2个版本（versionSequence 1/2，第2版 isCurrentVersion=true）；该查询不承载患者电话字段，电话仅由版本详情承载，与 S7-D8 承载面一致 |
| `QueryMedicalReportVersionDetail` | reportId 同上、reportVersionId `3a23c2c5-0406-cc5f-5ffe-0b6b2aeab6c5` | `content.common`：patientName `验收患者甲`、identityDocumentNo `530101199005120011`、patientPhoneNumber `13800001111`，与库逐字一致、无掩码 |

**审查复核记录（2026-09-22，独立复核角色只读复核）**：复核结论为「有问题」，共 8 条中等发现与 1 条范围口径确认，已全部处理或确认：审查记录原以字母编号引用审查发现，按 AGENTS.md §0.1 第 9 条改叙述式登记（即上文「票03 独立实施审查」段）；生产代码段一处注释表述含 agent-conduct 禁用字，改用 `Application.Contracts` 标识符表述；V19 段改终态陈述并去掉禁用字与过渡痕迹；审查记录段去掉禁用字字名打印，自查命中数改按终态（以字名引用该字符共两行：票01 记录行与该段自身）、受影响文件数改按明细（2 个文件）；根 `impl.md` 执行记录第 4 项括注改终态并勾选；票文件 Status 改 `done`；「阶段 4 根 testReport 补记」从待办与票面剩余中移除（已实际写入，006 `testReport.md` 为 +8/-0 纯追加）；本报告首状态行「批次1 待开工」改「批次1 进行中（票03 完成，票02、票04 待执行）」。范围口径确认：阶段 4 影响面共 6 个文件（其 `testReport.md` 只追加 +8/-0，另 5 个为 V18 已登记的同步文件），阶段 6 `testReport.md` 的 2 行改动为 V19 已登记的 S6-D9 关闭回写；按「历史测试结果行、计数与证据原文零改动、阶段 4 `testReport.md` 只追加」口径实测通过。复核另确认通过：语言纪律其余部分、状态纪律、勾选与证据一一对应、证据完整性、编号引用纪律、登记数字可复算。

**收尾检查（2026-09-22）**：`git diff --check` exit 0；本轮最终验证后仅改动文档，按 [Testing Baseline](../../.agents/instructions/testing-baseline.md) §5 生产代码未再变化，既有证据（后端全量 `失败: 1，通过: 780，总计: 781`、前端 `352 passed | 2 skipped (354)`、lint 0 错误）继续有效并沿用本节已登记结果；GitNexus `detect-changes` 属代码 symbol 变化闸门，本轮无代码改动不新增执行（前轮 `medium` 结果已登记于本节开工闸门段）。前后端进程（`15014`、`3008`）已关闭并核对端口释放（两端口均无监听）。浏览器会话：CLI 会话（`stage7-ticket03`）的受管浏览器已随应用重启结束，endpoint 不可用、无残留可控页面；MCP 自管实例内的宿主会话经页面「退出登录」关闭，token 未写入文件。

**待办**：无（票03 范围内）。

### 批次1（票02 一致性核对静态面与库前预校验抽查，2026-09-22，V8-V10、V12-V16、C22）

**开工前置**：本票 Blocked by 票01、票03，两者均已完成（票03 Status `done`；票01 的 `SourceGuardReuseTests` 登记缺口已补登记，后端全量 `失败: 0，通过: 781，总计: 781`）；V8 验收判据「命中为零」自此成立。

**V8 C-1 后端契约面（`Passed`，静态）**：对 `server/` 全部 `.cs` 执行关键字扫描（`Mask`、`掩码`、`打星`、七连星），生产代码（Application、Application.Contracts、Domain、Repository、Host）零命中；仅 2 处命中位于 `Stage6StatisticsContractTests` 的禁止字段名单（`MaskedPatientInfo` 是被禁止的标识符，属守卫自身判据，非掩码实现）。行为面证据同轮复用：`Stage4QueryTests`/`Stage4ContractTests` 目标面 49/49 含于后端全量 781/781；读模型字段原值返回的真实链路证据见票03 节四查询表与 C13 表。

**V9 C-2 双入口可信范围（`Passed`，静态核对 + 既有证据复跑）**：逐入口核对 `MedicalRecognitionReportQueryAppService` 与 `MedicalRecognitionReportAppService`——平台入口按请求使用组织、医院、院区并做存在启用与父子归属校验、不要求请求组织等于可信组织（`QueryMedicalReportListAsync` 注释与实现、统计平台侧四个入口「范围条件按请求使用」、导出平台侧同口径、写命令平台入口同规则）；本院入口组织与医院只取自可信上下文、请求不提交也不得覆盖、院区取请求但必须属于可信医院（`QueryBranchMedicalReportListAsync`、统计本院侧四个入口 `trustedScopeResolver.ResolveOrThrowAsync`、导出本侧、匹配/处理结果/引用/引用详情四条写链类级声明），可信解析先于公共请求校验。与决策（阶段 3 `design.md` 决策台账 S3-D1/S3-D17/S3-D19、`docs/plans/006-阶段4-报告采集与生命周期/design.md` 决策台账 S4-D8、`docs/plans/008-阶段6-互认统计与导出/design.md` 决策台账 S6-D1/S6-D18）一致；`Stage3TrustedScopeTests` 组织/医院不一致的业务拒绝文案断言在 781/781 内复跑通过；前端本院入口不提交组织与医院字段（`reportsApi.ts` `toBranchReportListBody`）与票03 宿主面「可信范围只读展示」互证。阶段 2 目录面为平台入口单入口，按平台入口口径核对。

**V10 C-4 权限不扩散后端面（`Passed`，静态）**：对 `server/` 全部 `.cs` 扫描角色与权限判定标识（`IsInRole`、`[Authorize`、`Roles =`、`Policy =`、`CheckPermission`、`IPermission`、`PermissionChecker`），实测命中 11 处：测试工程 10 处（`Stage5ConstraintTests` 合成探针 9 处、`Stage4SubmissionPathTests.cs:508` 的 `PropertyNamingPolicy` 误配对 1 处）；生产 1 处为 `MedicalRecognitionReportAppService.Submission.cs:34` 的 `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`，经甄别属 JSON 序列化命名策略、与权限判定无关——权限判定实质零命中；`Stage5ConstraintTests` 既有守卫（禁止 `CheckPermission`/`IsInRole` 类权限调用与 `[Authorize]` 扩散）在 781/781 内复跑通过。

**V12 阶段 6 分页机制（`Passed`，静态 + 既有用例复跑）**：公共 SQL 扫描 `LIMIT`/`OFFSET`/`TOP`/`FETCH FIRST` 在 `Repository` 全部 `.xml` 零命中，窗口归属 Provider 适配层（[Pagination](../../.agents/instructions/pagination.md) §5）；窗口校验入口 `PageQueryWindow.Create` 共 10 处（报告列表 2 处、统计四查询与四明细 8 处），均位于方法首段、先于仓储访问；`PageResultDto`/`PageInfoDto`/内嵌 `page` 形状由 `Stage4ContractTests` 分页契约守卫与 `Stage6StatisticsContractTests` 分页形状断言冻结，均含于 781/781；真实库跨页与总数行为证据为阶段 6 既有登记（V41/V42），本轮生产改动仅票03 查询投影字段，复用条件：影响范围为版本详情投影、分页与排序键未动，证据版本与本轮同一工作区（`8a16c16` 起点、ahead 6），复用理由为等价无扩散。

**V13-V15 库前预校验抽查（`Passed`，既有用例复跑 + 各阶段真实库证据复用）**：三类写命令的冲突拒绝用例在 781/781 内复跑通过——目录/配置/金额：`Stage2WritePathTests.Duplicate_configuration_is_rejected_before_write_without_event`、`Stage3WritePathTests` 金额并发冲突业务拒绝文案（「业务拒绝：金额已被其他操作变更，请刷新后重试。」）断言；报告提交/作废：`Stage4WritePathTests.Duplicate_source_detail_key_rejects_and_all_missing_is_accepted`、`Duplicate_display_order_rejects_the_report`；处理结果/引用结果：`Stage5WritePathTests` 三类冲突与幂等族（`Conflict_when_recognition_time_decision_or_scope_differs` 等）与引用链（`Conflict_when_reference_facts_differ` 等）。判据「抛业务异常而非数据库错误」由上述断言的业务拒绝文案直接证明；真实库面复用各阶段既有登记（阶段 4 testReport 写入类条目 2026-09-19 修复后重取证清单、阶段 5 testReport「处理结果拒绝」段与收口 `Complete`），复用条件：本轮生产改动只在查询投影，写路径零改动，781/781 复跑一致。

**V16 异常语义核对（`Passed`，静态）**：对 `server/` 全部 `.cs` 扫描异常中间件、过滤器与包装类型标识（`class …Middleware`、`IExceptionFilter`、`IAsyncExceptionFilter`、`UseExceptionHandler`、`AddExceptionHandler`、`ProblemDetails`），生产代码零命中（命中 2 处为测试合成探针）；数据库错误原样抛出的运行期证据复用阶段 6 V45 注入面（`HttpRequestException` → `SocketException 10061` 原样 500、无降级）与阶段 5「数据库异常按原样向外传播」收口口径。

**C22 业务规则类约束承接核对（`Passed`，四组逐号核对）**：跨来源报告隔离 → 阶段 6 V31（`008/Server/design.md:150` 来源侧本院锁定）/V37（`:156` 导出不含完整报告、PDF 与影像入口）/C14（`008/Client/testPlan.md:29` 弹窗不提供报告入口）；原因代码统一与处理结果值域合法性 → 阶段 5 V33（`007/Server/design.md:208`）/V35b（`:211` 处理结果值域非法）；金额口径 → 阶段 5 V23（`007/Server/design.md:198`）/V24（`:199`）/V45（`:222`）、阶段 6 V16（`008/Server/design.md:135` 金额时间归属）；未反馈与未处理状态面 → 阶段 5 V22b（`007/Server/design.md:197` 查询零写入）/V82（`:266`，F04 业务规则 19 由 V82 承接见 `:168`）/V93（`:277` 提醒事实口径）、阶段 6 V27（`008/Server/design.md:146` 未反馈语义）/C13（`008/Client/testPlan.md:28` 未反馈行显示），其中 V22b/V93 为相邻承接（查询零写入与提醒计数口径），值域面直接锚点为 V35b、有效期面直接锚点为 V82——与票面清单及设计审查核定的映射一致；四组锚点全部存在，对应测试含于 781/781，阶段 5 `Complete`、阶段 6 收口证据存续。

**分流处置（S7-D4）**：本票无桶①等价修复、无桶③文档一致性事项；桶②登记 1 项——转入的「匹配响应内 PDF 下载入口须重新校验绑定版本仍为当前有效版本」（CONTEXT.md 报告历史版本段 :448 附近）在阶段 4/5 testReport 与现行用例中均无承接登记，按契约级缺项记未关闭项，见「问题与残余风险」。

**票02 审查复核记录（2026-09-22，独立复核角色只读复核）**：复核结论为「有问题」，1 条中等与 2 条低等发现，已全部处理：V10 命中分布按实测改写（11 处 = 测试 10 处 + 生产 1 处 `PropertyNamingPolicy` 序列化命名策略误配对，经甄别与权限判定无关，权限判定实质零命中），票面勾选措辞同步改写；决策与承接编号补文件与行号出处（S3-D*/S4-D8/S6-D1/S6-D18 至各阶段 `design.md` 决策台账，V23/V24/V45/V16/V31/V37/V33/V22b/V82/V93/V27/C13/C14 至 `007`/`008` 设计矩阵与前端矩阵行号），票面 C22 项注明出处落点；本节三处既有设计用语「契约」（矩阵行名「C-1 后端契约面」与 S7-D4 桶②原文「契约级或范围级问题记未关闭项」）为引述，按本报告 :128 统一用语结论（继续沿用既有用语、不作改写）豁免，依据随本记录。复核另回源确认通过：V8/V12/V16 扫描可复算、V9 与 V13-V15 引用语句与测试名存在、C22 十四号锚点存在且语义对应、PDF 重校验「未检索到承接登记」成立、票面八项与本节逐一对应、批次1 三处状态一致、复用登记符合 Testing Baseline §5、语言纪律其余零命中。

### 批次1（票04 各阶段非干净条目过堂定级，2026-09-22，V11、C12）

定级口径（S7-D2 三出口）：①可补证据 → 补证据转 `Passed`；②经范围确认不适用于本轮 → `N/A`；③属本轮范围但不可构造或环境受限 → 保持原状态并逐条登记 `AcceptedRisk`。编号主题与原阶段出处见上文「分母冻结登记」表（49 行逐行对应，原阶段 testReport 与矩阵状态一律不改写）。本阶段矩阵当前无 `Blocked` 条目（票01 的 V6 行为面已取证 `Passed`），无需本阶段过堂行。

**V11 后端面过堂 + C12 前端面过堂（合计 49 条定级，`Passed`）**：

| 出口 | 条目（原阶段 编号 → 定级） | 理由 |
|---|---|---|
| ② `N/A`（23 条） | 阶段2 V24、V26；阶段3 V16、V30；阶段5 V11、V13、V14、V16、V25、V33、V48、V54、V64、V68、V77、V78、V79、V80、V81、V85、V86、V87、V95 | 三类经范围确认不适用本轮：事件观察与事务队列属框架生命周期、按工作单元规范不设观察点（阶段2 V24/V26、阶段5 V87）；业务规则不可达的防御分支与拒绝面（阶段5 V11/V13/V16/V25/V33/V48/V64/V68、阶段3 V30 与 V16 并发串行化且应用层冲突翻译已有等价用例）；静态判据已冻结、语句文本与声明校验面已证或已由该阶段票与阶段6 交付承接、本轮不重复取证（阶段5 V14/V54/V77/V79/V80/V81/V85/V86/V95，V78 按方言中立与 Provider 适配属框架口径） |
| ③ 保持原状态 + `AcceptedRisk`（26 条） | 阶段2 V27、C17；阶段3 V29、C3、C11、C15、C22、C23；阶段4 V85、V86、无编号跨组织宿主面；阶段5 V4、V6、V38、V43、V44、V45b、V53、V55、V56、V58、V71、V90、V16c；阶段6 V44 子面、C30 子面 | 属本轮范围但不可构造或环境受限：环境仅组织01 与两个同组织测试账号，跨组织、第二授权组织与 branch_id 为空身份不可构造（阶段2 V27/C17、阶段3 V29/C3/C11、阶段4 无编号、阶段6 C30 子面）；宿主注入点与真实数据量不足（阶段3 C15/C23/C22）；阶段5 条目构造口径下真实库面、时间与组合前置未构造成功或不可快速构造（V4/V6/V38/V43/V44/V45b/V53/V55/V58/V71/V16c/V56）、外部参数服务记录面无直接取证通道（V90）；导出规模面按 S6-D16 随业务量增长复核（阶段6 V44 子面） |
| ① 补证据（0 条） | — | 逐条核对后无本轮可低成本补证据条目；阶段4 V85/V86 的新语义按冻结例外映射处理（见下），不走补证据出口 |

**冻结例外处理（阶段4 V85/V86，记录重新冻结时点 2026-09-22）**：并发唯一性语义变更后的新语义（撞业务键由唯一索引拒绝、异常原样传播）的动态并发面在本环境被串行化、按 MVP 口径不投入并发治理资源，不增补用例、不新增编号；新语义的静态承接映射至既有冻结编号——异常原样传播由本阶段 V16 异常语义核对（票02 `Passed`）承接、唯一索引由阶段4 DDL 静态面（V80b 建表与索引冻结）承接；两条保持 `PendingRetest` 并各登记 1 条 `AcceptedRisk`（已计入上表③的阶段4 两条）。

**`AcceptedRisk` 定级残余清单（26 条，已随票06 收口被接受，见结论段）**：阶段2 V27、C17；阶段3 V29、C3、C11、C15、C22、C23；阶段4 V85、V86、无编号跨组织宿主面；阶段5 V4、V6、V38、V43、V44、V45b、V53、V55、V56、V58、V71、V90、V16c；阶段6 V44 子面、C30 子面——逐条状态保持冻结时状态，全部待负责人在阶段收口按 S7-D12 接受或改判。

**登记完整性确认**：本轮仅在本阶段 testReport 登记，阶段2 至阶段 6 的 testReport 与矩阵状态零改动（阶段4/6 文档的既有改动为票03/票01 已登记的同步与修复）；C12 前端面清单（阶段2 C17、阶段3 C3/C11/C15/C22/C23、阶段4 前端无未收口条目、阶段6 C30 子面）已全部含于上表。

**票04 审查复核记录（2026-09-22，独立复核角色只读复核）**：复核结论为「有问题」，1 条低等发现已处理——根 `impl.md`「待办与交接」段的批次状态陈述停留在票03 时点，已改写为与批次表一致的终态陈述（现表述：批次 0 至批次 2 已完成、批次 3 未开始）。复核另确认通过：定级计数 23+26=49 与冻结表逐行对应、26 条 `AcceptedRisk` 清单与③集合字符串级一致、V85/V86 只计一次；冻结例外映射目标（本阶段 V16 票02 段、阶段4 V80b 与唯一索引静态面）真实存在且承接成立、重新冻结时点已记录；状态纪律（`AcceptedRisk` 仅作残余标签不替代 `Passed`、26 条保持冻结时状态）；语言纪律零命中；原阶段 testReport 零改动（003/005/007 无改动，006 仅 +8 追加与 V18 登记同步，008 仅 V19 登记回写）；三处状态行一致。

### 批次2（票05 宿主链路全量走查，2026-09-22 开工，V20-V29、C1-C11、C14-C18）

**段1 接口组开工与首条可达登记（2026-09-23）**：前置票03、票04 已完成；后端 `localhost:15014`、前端 `localhost:3008` 本轮启动并实际监听；yangkj 从宿主登录页登录（入口「检验检查结果互认平台」），拦截两级启用。首条核对调用可达（`Passed`，V21 调用前置）：页面上下文携带宿主 Bearer 令牌请求 `GET http://localhost:15014/api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf`（`MR-ACC-LAB-20260918-001` 当前版本，reportId `3a23c2c4-1507-5079-d224-05d617d4699f`、versionId `3a23c2c5-0406-cc5f-5ffe-0b6b2aeab6c5`）→ `HTTP 200`、`content-type: application/pdf`、69 字节、首字节 `%PDF-1.4`，与阶段 4 V75（出处 `docs/plans/006-阶段4-报告采集与生命周期/Server/design.md:370`）同形。首次请求返回 `HTTP 401`，系宿主令牌 `nbf` 时钟不同步（阶段 4 实测宿主晚约 16 秒、等待约 20 秒重试即可，出处见阶段 4 `testReport.md`「接口直测的认证前置」），等待 22 秒后重试通过，不属鉴权失败。（本段为开工登记，完成态见下方「段1 接口组完成登记」与段2、段3 登记。）

**段1 接口组取证进度（截至登记时点，5/9 完成，全部 `Passed`）**：数据全部经接口构造、未写库，请求样例复用阶段 4 验收样例文件（`阶段4-Tickets/请求样例-检验报告.md`、`请求样例-检查报告.md`）并换用本轮专用报告单号与证件号码：

| 项 | 调用与结果 |
|---|---|
| PDF 下载（首条可达） | `GET /api/v1/report-pdf/{reportId}/versions/{versionId}/pdf` → `200`、`application/pdf`、69 字节、`%PDF-1.4`（见上） |
| V21 检验报告提交 | `POST /api/v1/report-pdf/laboratory-report`（multipart，`reportJson` + 最小 PDF，`MR-S7C05-LAB-20260922-001`、患者 `走查患者张`/`530101199301010001`、电话 `13800007777`）→ `200` 与 `true` |
| V22 检查报告提交 | `POST /api/v1/report-pdf/examination-report`（multipart，`MR-S7C05-EXAM-20260922-001`、患者 `走查患者乙`/`530101198503200077`，含项目与部位层级）→ `200` 与 `true` |
| V23 检验报告作废 | `POST /Api/MedicalRecognitionReport/VoidLaboratoryReport`（reportNo + voidedTime + voidReason）→ `200` 与 `true`；过程两次拒绝均为领域层业务拒绝、快速失败零写入（缺作废时间、作废时间早于平台接收时间），文案「业务拒绝：…」原样返回，属判据内行为 |
| V24 检查报告作废 | `POST /Api/MedicalRecognitionReport/VoidExaminationReport`（同上形状）→ `200` 与 `true` |

余 4 项待取：匹配查询、处理结果、引用详情、引用结果（依赖本轮提交的报告链路数据），完成后并入本表并勾选票面 V21-V29 项。

**段1 接口组完成登记（9/9，`Passed`；取证时点 2026-09-23，机器时钟，会话自 2026-09-22 延续）**：

| 项 | 调用与结果 |
|---|---|
| 匹配查询 | `POST /Api/MedicalRecognitionReport/RequestRecognitionMatches`（`530101198601012202`、`VISIT-ST6-S22`、visitType 1、proposedItems 项目 `123`）→ `200`、`hasMatches=true`、`recognitionMatchRecordId=3a23de15-c0a2-8b33-56f6-cf27880dce24`；另以 `POST /Api/MedicalRecognitionReportQuery/QueryRecognitionMatchRecord` 按该 id 读回记录（患者 林来源、isProcessed、matchItems）与匹配项 `3a23de15-c0ad-24d7-ab64-1b2b01de7e8f`，两次均 `200`；同接口对本轮新提交报告（`VISIT-S7C05-L2`）返回 `200` 与 `hasMatches=false`（未命中候选，事实登记） |
| 处理结果 | `POST /Api/MedicalRecognitionReport/SubmitRecognitionProcessingResults`（recordId 同上、采纳 `result=1`、互认科室/医生走查值、recognitionTime 本地时间 `2026-09-23T09:37:29`）→ `200` 与 `true` |
| 引用详情 | `POST /Api/MedicalRecognitionReportQuery/QueryRecognitionCitationDetail`（证件与 `VISIT-ST6-S22`）→ `200`，`matchItems` 含项目 `123` 的采纳项与检验明细（`ST6 项目S22`/`5.5`/单位），`reportContext` 非空 |
| 引用结果 | `POST /Api/MedicalRecognitionReport/SubmitRecognitionReferences`（同匹配项、referencedTime `2026-09-23T09:37:38`、引用科室/医生走查值）→ `200` 与 `true` |

过程中的参数与形状拒绝（匹配查询缺就诊类型、作废缺时间、作废时间早于平台接收时间）均返回校验或「业务拒绝：…」文案快速失败，属判据内行为，非缺陷。**匹配链补充提交**：为构造匹配链另提交 `POST /api/v1/report-pdf/laboratory-report`（`MR-S7C05-LAB2-20260923-001`，患者 `走查患者丙`/`530101199402020003`、项目 `123`、`VISIT-S7C05-L2`）→ `200` 与 `true`，该报告未作废、保持有效（列表 62→65 的 +3 即张、乙、丙三份本轮提交）。**V20 回写登记**：本轮接口与核对未产生同类能力统一动作，无后端回写事项；控件组判定完成后如产生统一动作再按本项补登记。**失败路径与边界复用决定（C19 输入）**：本轮只新取证成功主路径，失败路径与边界复用各阶段既有证据（阶段4 宿主失败态与跨院区拒绝 C30/C31，出处 `docs/plans/006-阶段4-报告采集与生命周期/Client/testPlan.md:45`、`:46`；阶段5 拒绝与幂等两链；阶段6 401/超限与组织服务失败面）；复用项、失效项与修复后重测项的分项分类归票06。

**段2 页面组开工与首条取证登记（本院报告管理与 C14 首证，2026-09-23）**：
- 本院报告管理与历史版本（yangkj）成功主路径：宿主菜单进入，可信范围「组织 01，医院 0101」与列表渲染（当前共 65 份，含本轮走查数据：`走查患者丙` 有效、`走查患者张`/`走查患者乙` 已作废，作废态在页面可见），`POST /Api/MedicalRecognitionReportQuery/QueryBranchMedicalReportList` 返回 `200`。
- C14 本院入口面（`Passed`）：同一次列表请求经 Network 实测，请求体为 `{"branchCode":"0101001","page":{"pageIndex":1,"pageSize":10}}`，不含本侧组织与医院字段，与 `reportsApi.ts` 适配层静态核对（票03 V9 段）互证。
- 后续各项已由下方「段2 页面组 yangkj 侧完成登记」「段2 页面组 lisadmin 侧完成登记」与段3 控件组登记覆盖（C15 观察随两段闭合）。

**段2 页面组 yangkj 侧完成登记（2026-09-23，`Passed`）**：
- 本院区互认项目金额页：菜单进入、标题与金额域渲染，列表接口 `QueryBranchRecognitionAmountList` `200`（3 行：项目 `123` 启用 12.34、`222` 停用 37.37、`S1-D29-ITEM-20260915-001` 启用未配金额）。
- 统计四页（本院互认使用、本院被互认、接收医院互认使用、来源医院被互认）逐页成功主路径：标题与汇总/明细渲染，对应接口 `QueryBranchRecognitionUsageSummary/Details`、`QueryBranchSourceRecognitionSummary/Details`、`QueryRecognitionUsageSummary/Details`、`QuerySourceRecognitionSummary/Details` 全部 `200`；下钻入口齐全——使用统计页 8 类下钻按钮（提醒/采纳/不采纳/引用/金额/三处原因）、被互认页「下钻被互认明细」；实际点击「下钻采纳明细」→ 明细区过滤重查，显示 `共 29 条`（下钻成功）；导出入口四页均有「导出汇总/导出明细」按钮（入口存在性；导出下载行为复用阶段6 C27/C28 既有证据）；原因区仅出现在两使用统计页（含占比文案），被互认页无原因区，与设计口径一致。
- C14 本院入口面补强（`Passed`）：`QueryBranchRecognitionUsageDetails` 请求体 `{"detailType":1,"endTime":"2026-09-23","page":{...},"startTime":"2026-09-01"}` 与 `QueryBranchRecognitionAmountList` 请求体 `{"branchCode":"0101001"}` 均不含本侧组织/医院字段，与列表页证据合计三处 Network 实测。
- C15 前端面（yangkj 侧观察，`Passed`）：走查全程 6 次页面进入无任何二次权限确认交互（无角色提示、无授权弹层），接口失败提示由宿主统一承载（阶段4 C30 失败注入既有证据复用，出处 `docs/plans/006-阶段4-报告采集与生命周期/Client/testPlan.md:45`），前端无本地权限判定代码（票02 V10 扫描）。

**段2 页面组 lisadmin 侧完成登记（2026-09-23，`Passed`）**：
- 标准项目目录维护：标题与列表渲染，6 行；互认项目：3 行；互认项目金额维护：选择组织 县医共体 后 3 行——平台三配置页成功主路径全部 `200` 数据渲染。
- 报告管理与历史版本（平台入口）：选择 县医共体 → 县人民医院 → 总院 后列表渲染 `当前筛选共 65`、当页 10 行，`QueryMedicalReportList` 成功（选择器级联默认值（测试医院一/测试院区1-1）随后可切换，切换后重查成功）。
- 统计四页第二身份逐页成功：接收医院互认使用统计（汇总 `共 2 条`、下钻 15、原因区有、导出双入口）、来源医院被互认统计（`共 2 条`、下钻 2、导出双入口）、本院互认使用统计（可信范围「组织 01，医院 CSYY2」、`共 1 条`、下钻 7、原因区有）、本院被互认统计（可信范围 CSYY2、`共 1 条`、下钻 1、导出双入口）。
- C14 覆盖结论：本院入口三类请求已在 yangkj 侧取得三处 Network 实测（列表、使用明细、金额列表）；lisadmin 本院入口走同一批 Branch 端点与同一适配层（`toBranchReportListBody` 等），不重复抓包，按同端点同适配层复用该结论。
- C15 前端面（lisadmin 侧观察，`Passed`）：平台四页与统计四页（8 页）全程无二次权限确认交互，失败提示由宿主统一承载；两身份合计 14 次走查观察闭合 C15 前端面（与票面 C1-C10 计数一致）。

**段3 控件组完成登记（C16-C18，2026-09-23，`Passed`，均为「本就一致、无需统一」）**：
- C16 时间/日期筛选控件：成对核对——报告管理平台入口与本院入口共用 `ReportManagementBoard`（`type='date'` 原生日期输入 ×2，`ReportManagementBoard.tsx:390/:400`）；统计四页共用 `statisticsBoard`（`type='date'` ×2，`statisticsBoard.tsx:807/:817`）。全部为原生日期输入；报告管理两入口经 `reportsApi.ts:191` 的 `toRequestDate`、统计侧经 `recognitionUsageStatisticsApi.ts:347` 的 `toStatisticsRequestDate`/`requireStatisticsDate`，取值口径同为 `YYYY-MM-DD`，形态与取值口径一致，无需统一（`Page`/`RangePicker` 类组件零引用，佐证无第二实现）。
- C17 分页形态：阶段4 报告管理两入口经 `BranchReportManagement` 包装共用 `ReportManagementBoard.createTablePagination`；阶段6 汇总与明细分页同用 `statisticsBoard` 内 `createTablePagination`（`shared/tablePagination.ts` 单一声明处）；阶段1-3 页面（目录、互认项目、金额两页）无分页，与各阶段设计一致。两处有分页的阶段形态一致，无需统一。
- C18 弹窗与表单交互：成对核对三处新增/编辑弹窗——`StandardCatalog`（两个 Modal）、`RecognitionAmountModals`、`RecognitionProjectModals`，全部 `open + onCancel + onOk + confirmLoading={submitting} + destroyOnHidden` 同一形态；提交中状态由 `confirmLoading` 统一反馈，projects 侧明文约定「确定按钮保持可点、强阻断优先于提交中」；失败时弹窗不关闭、表单值保留（失败不触发关闭、`destroyOnHidden` 仅在关闭后生效），三处同实现，既有组件测试覆盖，无需统一。
- **V20 回写登记（完成）**：控件组三组均判定无需统一，本轮无同类能力统一动作，无后端回写、无改写失效重测项；与段1 的 V20 预登记一致。

**分流处置（S7-D4）**：本票无桶①等价修复、无桶②未关闭项、无桶③文档一致性事项——三组控件判定均无需统一，全程零缺陷发现。

**票05 审查复核记录（2026-09-23，独立复核角色只读复核）**：复核结论为「有问题」，4 条中等与 7 条低等发现，已全部处理：批次归属与证据落点三方统一（根 `impl.md` 批次表与「矩阵编号的批次归属」句改为批次1 `V8-V19、C12-C13、C22`、批次2 `V20-V29、C1-C11、C14-C18`，`Server/impl.md` 与 `Client/impl.md` 批次行同步，本节标题补 C14-C15）；本节补 S7-D4 分流段（即上一段）；C15 页数按实际改写（lisadmin 平台四页与统计四页 8 页、两身份合计 14 次走查，票面同步）；C16 请求映射按源码改正为报告管理 `reportsApi.ts:191` 的 `toRequestDate`、统计侧 `recognitionUsageStatisticsApi.ts:347` 的 `toStatisticsRequestDate`/`requireStatisticsDate` 两函数同口径；根 `impl.md` 待办段与票面双 Status 行改终态（保留单行 `done`）；新增句「时钟偏差」改「时钟不同步」；作废两次拒绝改定性为领域层业务拒绝、快速失败零写入；补登记匹配链提交（`MR-S7C05-LAB2-20260923-001`、走查患者丙，列表 62→65 对齐）；票04 复核记录移至票04 节之后；原阶段编号补出处（V75→`006-阶段4/Server/design.md:370`、C30/C31→`006-阶段4/Client/testPlan.md:45/:46`、票面 §2→[阶段7 design](design.md) §2）。复核另确认通过：接口组 9/9 与 `Server/design.md` 矩阵逐一对应、id/单号/时间自洽、C14 三请求体确无本侧组织/医院字段、页面组两段合计 14 次覆盖十页、控件组全部行号真实、「无需统一」符合 testPlan 判据、C19 无复跑暗示、状态主口径一致、票05 节与票面无令牌长串（`eyJ` 零命中）、语言纪律其余零命中。

### 批次3（票06 证据收口与发布结论，2026-09-23，V30-V33、C19-C21）

**V30 矩阵冻结核对（`Passed`，静态）**：分母与冻结记录一致——本阶段矩阵 55 条（V1-V33、C1-C22，出处 [后端设计](Server/design.md) 验证矩阵与 [前端矩阵](Client/testPlan.md)）+ 过堂清单 49 行（编号 48 行含 2 子面、无编号 1 行），构成与冻结时点见「分母冻结登记」段；全程无新增、无删除、无挪号，阶段4 V85/V86 走冻结例外映射（不增补用例），重新冻结时点 2026-09-22 记录于票04 节（冻结例外处理段），「冻结例外口径」段登记例外口径与已知条目——例外记录完整。

**V31 上游依赖与外部服务健康（`Passed`，宿主）**：权限系统与组织服务（`183.224.180.166:35001`）在本轮走查期间经 Network 列表实测调用全部 `200`——登录 `Api/Auth/Login`（reqid=37）、菜单 `Api/Menu/QueryAllMenu` 与 `Api/UserQuery/QueryUserRoleMenu`（reqid=42/44）、组织级联 `Api/Organization/QueryAllOrganization`/`QueryAllValidHospitalByOrgId`/`QueryAllValidBranchByOrgId` 多批次（如 reqid=582-585、170-178），页面级成功另见票05 节两段登记；同组接口的既有登记按复用四要素并入——影响范围：外部服务可达性，本轮无相关配置改动；证据版本与环境：同宿主与同后端；时间：阶段1（`003/testReport.md:69`、`:494`）、阶段3（`005/testReport.md:24`、`:118`）、阶段4（`006/testReport.md:232`、`:234`）既有 + 2026-09-23 本轮；复用理由：同端点同身份复测同形（两身份菜单与登录另见票00 C11 节）。参数服务可达由两侧查询承载（出处 [阶段5 design](../007-阶段5-在线互认主流程/design.md) 决策台账 S5-D4 的取证口径）：匹配查询读取本院开关与排除时长两项（`MedicalRecognitionReportAppService.Matching.cs:109`、`:125`、`:144-152`）、引用详情查询读取有效时长一项（`MedicalRecognitionReportQueryAppService.Citation.cs:103`），两侧均为严格失败语义、不可达即整次失败，本轮 `RequestRecognitionMatches`、`QueryRecognitionCitationDetail` 等相关调用全部 `200`，失败未发生（阶段5 V90 参数记录取值面按票04 定级单列 `AcceptedRisk`，出处 `007-阶段5/Server/design.md` 矩阵行，不混入本条健康核对）。

**V32 端口与进程边界、快照与数据余量（`Passed`，静态，核对记录完整）**：
- 端口与进程边界：本轮后端 `localhost:15014`（后台任务 pwsh-1）与前端 `localhost:3008`（后台任务 pwsh-2）由本轮启动、取证期间监听，收口时已关闭并核对释放（两端口收口复测均无监听）；CLI 受管 Chrome 端点 `9222` 随此前应用重启结束、收口复测无监听；宿主 `35000/35001` 为外部服务未改动；无遗留本轮进程。
- 起始提交与工作区快照：起始 HEAD `8a16c16`、`main` 领先 `origin/main` 6 个提交（开工闸门段记录），收口复测仍为 `8a16c16` 与领先 6；当前工作区 34 个变更条目（32 个修改 + 2 个未跟踪：票01 新增守卫测试文件与既有杂项 `({t`），全部属本阶段已登记改动与负责人自留改动，无阶段外意外文件。
- 测试数据余量：本轮全部经接口构造——报告 3 份（走查患者张 `MR-S7C05-LAB-20260922-001` 已作废、走查患者乙 `MR-S7C05-EXAM-20260922-001` 已作废、走查患者丙 `MR-S7C05-LAB2-20260923-001` 有效），匹配记录、处理结果、引用结果各 1 条（林来源 `VISIT-ST6-S22` 链：匹配→采纳→引用），0101 侧列表 62→65；既有基线以已登记事实核对未破坏——0101 侧林来源筛选 22 份与票03 节一致、票03 C13 表登记的带电话版本（`MR-ACC-LAB-20260918-001` 两版 `13800001111`、`MR-ST5-09-CROSS-CSYY2-001` `13800009999`）经本轮匹配链与页面走查复核仍在；余票06 为纯文档收口，无新增数据需求，余量充足；开发期冗余与孤岛数据按既定口径不作缺陷。

**C19 既有证据复用分项登记（`Passed`）**：

| 阶段与验证面 | 复用项 | 影响范围 | 证据版本与环境 | 时间 | 复用理由 |
|---|---|---|---|---|---|
| 阶段4 后端自动化面 | 后端全量 `失败 0 / 通过 781`（含 Stage4 目标面 49 项） | 票03 仅改版本详情投影与注释，写路径零改动 | 同工作区 `8a16c16` 起点、.NET 10.0.401 | 2026-09-22 补登记后复跑 | 等价改动后目标与全量复跑全绿 |
| 阶段4 真实入口与失败态 | 提交/下载/宿主失败态与跨院区拒绝（C30/C31，出处 `006-阶段4/Client/testPlan.md:45`、`:46`） | 票03 不触及控制器与失败路径 | 阶段4 宿主证据 + 票05 同端点成功链复测 `200` | 2026-09-18/19 → 2026-09-23 | 同端点同授权方式复测同形 |
| 阶段5 写路径拒绝与幂等 | V13-V15 库前预校验三类写命令 | 本轮无写路径生产改动 | 阶段5 `Complete` 收口证据 + 781 复跑 | 2026-09-20 → 2026-09-22 | 写路径零改动、测试全绿 |
| 阶段6 统计/分页/导出 | V41/V42 真实库分页（出处 `008-阶段6/Server/design.md` 矩阵行）、401/超限、组织服务失败面、导出下载 | 票03 改动不触及统计与排序键 | 阶段6 收口测试含于本轮 781；导出入口由票05 走查确认存在 | 2026-09-22 → 2026-09-23 | 测试全绿复跑 + 页面复测同形 |

失效项：阶段4 掩码时期证据（`*******1111` 等历史行）不再代表现行口径——历史行按规范保留原样、由票03 补记覆盖；阶段4 V85/V86 旧并发证据作废，已按票04 定级 `PendingRetest` + `AcceptedRisk`。修复后重测项：`SourceGuardReuseTests` 登记缺口补登记后 781/781 全绿（批次0 节记录）；除此之外本轮无生产代码修复、无其他重测项。复用/失效/重测三类即本清单，输入来自票05 C19 复用决定。

**C20 同步动作汇总定案（`Passed`）**：输入齐备且登记齐全——票03 三条同步动作（V17 患者原值口径实现、V18 阶段4 全量文档同步、V19 阶段6 S6-D9 关闭回写，登记于根 [impl](impl.md)「待办与交接」段与票03 节）；票05 零条（三组控件均无需统一、无后端回写，登记于票05 节 V20 段与票面）；票02 无桶①③事项（分流段登记）。**V19 追加登记齐全性核对**：票02 与票05 均无桶①等价修复登记（票02 分流段、票05 分流段）且无桶③文档一致性修正，V19 追加数为零，核对齐全。

**C21 证据汇总（`Passed`）**：矩阵 55 条逐条有结论——批次0 V1-V7（票01）、批次1 V8-V19/C12-C13/C22（票02、票03、票04）、批次2 V20-V29/C1-C11/C14-C18（票00 C11 + 票05）、批次3 V30-V33/C19-C21（本节，V33 结论见下）全部登记；过堂 49 条全部定级（23 `N/A` + 26 `AcceptedRisk`）；测试基线：后端 781/781（票03 后全量复跑，其成功编译即后端构建面佐证，出处批次0 与票03 段）、前端 354 项（352 通过 + 2 跳过）、lint 0 错误；前端生产构建于 2026-09-23 收口时实跑 `pnpm -F dy-medical-recognition build`（`tsc -b && vite build`，3318 模块转换成功、exit 0，仅既存 chunk 体积警告）0 错误。最终验证后仅改动文档，按 [Testing Baseline](../../.agents/instructions/testing-baseline.md) §5 复用登记四要素——影响范围：文档，生产代码自票03 后零改动；版本与环境：同工作区 `8a16c16` 起点、同机；时间：2026-09-22/23；理由：变更影响与证据无重叠。`git diff --check` exit 0；GitNexus `detect-changes` 无 `CRITICAL`（批次0 与票03 时点各登记一次，此后仅文档）。

**V33 残余登记与发布结论（`Passed`）**：残余全集 = ① 桶② 项 1 项（匹配响应 PDF 下载入口绑定版本重校验无承接，见「问题与残余风险」，已裁定转记 `AcceptedRisk`）；② `AcceptedRisk` 定级残余 26 条（票04 节清单，逐条保持冻结时状态）；③ 无其他新增残余（「契约」用语、C11 菜单文本、历史 PDF 共享路由三项按既有裁定维持现状登记）。按 [设计](design.md) S7-D12（存在未定级条目即 `NotReady`）与 [Testing Baseline](../../.agents/instructions/testing-baseline.md) §3.1（存在未关闭项即 `NotReady`）双判据，裁定前字面为 `NotReady`；2026-09-23 负责人裁定：未关闭项转 `AcceptedRisk`、26 条残余全部接受，合计 27 条残余全部定级接受，发布结论 **`ReadyWithAcceptedResiduals`** 已写入「结论」段。

**票06 审查复核记录（2026-09-23，独立复核角色只读复核）**：复核结论为「有问题」，6 条中等与 6 条低等发现，已全部处理：V31 出处改写为本轮 Network 实测（reqid 举证）+ 既有登记复用四要素并入；参数读取口径改正为「匹配查询两项 + 引用详情一项、两侧严格失败」并附源码与 S5-D4 出处；V30 重新冻结时点改为 2026-09-22 单日并按实际出处归属；补实跑前端生产构建（`pnpm -F dy-medical-recognition build`，`tsc -b && vite build`、3318 模块、exit 0）并把后端构建面改为票03 全量测试编译成功佐证、C21 补 §5 复用四要素；首行与总体计划 009 行把票00 从「经独立审查复核」陈述中剔除（改为其为核对登记类）；根 `impl.md` 待办段批次3 状态改收口中；C20 的 V19 核对句补「票05 桶①零事项」一侧；编号补出处（S5-D4、阶段5 V90、C30/C31、V41/V42、票面 S7-D4②）；「两口」改「两端口」；工作区条目数改实测 34（32 修改 + 2 未跟踪）；数据余量删除无登记出处的份数统计、只保留票03/票05 已登记事实；三态判定出处改「S7-D12 未定级条目 + Testing Baseline §3.1 未关闭项」双判据。复核另确认通过：V30 计数 55/49 回算一致、V31 排除项与推理链在改正口径下成立、V32 其余事实（HEAD/端口/单号/62→65/匹配链）与既有登记一致、C19 四行四要素完整、C20/C21 映射与 impl 归属一致、V33 残余 1+26 与清单一致且草案未越权、五处状态语义一致、范围内禁用字与令牌长串零命中。

## 问题与残余风险

- C11 菜单文本与设计清单名称差异 3 处（见票00节）：菜单文本与设计清单名称按现状保留，不再修改；不影响走查执行，不阻塞。
- 历史版本 PDF 能力存在两条共享可达的承载路由（V6 行为证据，见票01节事实登记）：`POST /Api/MedicalRecognitionReport/OpenReportVersionPdf`（写侧应用服务自动端点，管理端历史版本查看页面的承载路由）与 `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf`（`ReportPdfFileController` 手工路由，医院接入面 11 条路由之一）；两条均按版本标识定位、已作废报告的历史版本仍可下载（S4-D28，[阶段4设计](../006-阶段4-报告采集与生命周期/design.md)决策台账）。任何持宿主会话令牌的调用方均可到达两条路由；按「能访问接口即认定有权限」的权限不扩散口径（阶段7设计 §3 C-4），接口可达性不构成权限问题，HIS 集成边界为医院接入九项能力清单（无历史版本查询、列表与详情能力）。共享前缀的物理隔离能力不作为排除项判定基准；本条按事实登记保留，两条承载路由按现状不作改造。
- 匹配响应内 PDF 下载入口的绑定版本有效性重校验（`CONTEXT.md` 报告历史版本段，:448 附近）在阶段4/阶段5 测试报告中无承接登记（票01 转入、票02 按 S7-D4 桶②记项）；2026-09-23 随阶段收口由负责人裁定接受，转记 `AcceptedRisk`（残余第 27 条，随三态写入结论段）；其与 S4-D28 的适用范围张力列为后续事项，见结论段。

## 结论

**发布结论（负责人裁定，2026-09-23）：`ReadyWithAcceptedResiduals`。** 本阶段分母为矩阵 55 条 + 过堂 49 行，全部条目已取得结论（`Passed` / `N/A` / 定级残余），无 `Failed`、无未取证条目、无未复核票据。定级残余全部被接受，合计 27 条：票04 过堂 `AcceptedRisk` 26 条（清单见票04 节）+ 桶② 项 1 项转记 `AcceptedRisk`（匹配响应 PDF 下载入口绑定版本重校验承接缺口，见「问题与残余风险」）。后续事项：`CONTEXT.md` :448「绑定版本仍为当前有效版本」与 S4-D28（历史版本按版本标识可下载）的适用范围张力，在后续阶段或 `CONTEXT.md` 修订时一并澄清，不构成本阶段未关闭项。阶段收口状态 `Complete`（2026-09-23，全部批次完成并获负责人确认）。

（历史口径保留：发布结论只取 `Ready` / `ReadyWithAcceptedResiduals` / `NotReady` 三态之一，由负责人在本结论段裁定；`Ready` 要求分母无残余，`ReadyWithAcceptedResiduals` 要求残余全部已定级接受，存在未定级条目或未关闭项即 `NotReady`。）
