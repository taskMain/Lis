# 阶段 4 测试报告

**当前状态：部分执行。** 本报告只登记已有实际执行证据的条目；每条矩阵编号的证据状态逐条列在「矩阵条目状态清单」节，按「有真实入口证据 / 有自动化证据覆盖 / 无自动化回挂且无真实入口证据 / 设计记 `N/A` / 环境不可构造」五类给出，不声明 `Passed`、`Complete`、`Ready` 或「验收通过」。

## 执行边界

- 报告状态：后端矩阵 93 条（V1-V88 主编号含 V19b、V80b 子编号，另含 V89、V90、V91）、前端矩阵 31 条（C1-C31）中，已有真实证据的条目逐条列在下方结果表与本报告「本轮收尾取证与缺陷处置」节；每条的证据状态见「矩阵条目状态清单」节，其中「无自动化回挂且无真实入口证据」与「环境不可构造」两类均已清空。矩阵定义分别见 [Server/design.md](Server/design.md) 的验证矩阵章节与 [Client/testPlan.md](Client/testPlan.md)。
- 起始工作区状态：本轮起点包含设计定稿阶段产生的同步产物（`docs/uml/1-MedicalRecognitionReport.wsd` 与 `2-MedicalRecognitionConfigurationUiQuery.wsd` 的修订、`docs/plans/001-总体计划/impl.md` 的进度行、宿主配置文件的 PDF 存储配置节、`.gitignore` 的本地运行期数据目录规则、本阶段文档目录），均尚未提交。
- 外部前置（已就绪）：目标库与 schema 已确认（`DysoftHIS` 的 `public`），阶段 4 的 10 张表已由负责人按三批执行建表；PDF 存储根目录已配置且目录可写；宿主菜单与角色授权由负责人配置。
- 数据形成口径：验收数据只通过平台接口或页面形成；报告数据由两个提交接口构造；不直接写库、不使用脚本注入数据。本轮对数据库只做只读元数据查询。
- 不测试范围：框架工作单元的事务与事件行为不设用例；文件补偿类用例只覆盖本项目自己的代码路径。

## 映射目录缺陷与修复复测（2026-09-19）

收口后对 Repository 的映射文件做了一次按业务能力分目录的整理，把 19 个 SqlMap XML 从聚合目录移入 `MedicalRecognitionReportAggregate/SqlMap/<分组>/`，该整理打断了宿主的语句注册，使**所有触库写接口失效**。缺陷已按「移回聚合目录平铺」处置，并在真实宿主链路上重新取证。

### 缺陷十四：映射文件移入子目录后写侧作用域全部未注册

**原状（真实宿主实测）**：

| 请求 | 结果 |
|---|---|
| `POST /api/v1/report-pdf/laboratory-report`（完整样例 + 最小 PDF） | `HTTP 500`，`Dy.Earthrace.Exceptions.EarthraceException: Can not find SqlMap.Scope:MedicalRecognitionReport` |
| `POST /Api/MedicalRecognitionReport/EnableMedicalStandardCategory` | `HTTP 500`，`Can not find SqlMap.Scope:MedicalStandardCategory` |
| `POST /Api/MedicalRecognitionReport/DisableMedicalStandardGroup` | `HTTP 500`，`Can not find SqlMap.Scope:MedicalStandardGroup` |
| `POST /Api/MedicalRecognitionReport/ChangeMedicalStandardItemRemark` | `HTTP 500`，`Can not find SqlMap.Scope:MedicalStandardItem` |
| `POST /Api/MedicalRecognitionReport/EnableMutualRecognitionItem` | `HTTP 500`，`Can not find SqlMap.Scope:MutualRecognitionItem` |
| `POST /Api/MedicalRecognitionReportQuery/QueryMedicalStandardCategoryList`（读） | `HTTP 200`，返回数据 |

异常抛出自 `Dy.Earthrace.Configuration.SqlMapConfig.GetSqlMap(String scope)`，经 `Dy.Earthrace.Middlewares.InitializerMiddleware.InitRequest` 向外传播。

**根因**：宿主不按目录名注册映射，而是按框架默认资源模式匹配内嵌资源的**逻辑名**（模式片段位于 `Dy.Earthrace.Abstractions.dll`）。可用深度以对照项目 `Dy.LisCenter` 为证：根下一级目录（`MedicalRecognitionReportAggregate/…`、`Queries/…`）与两级目录（LisCenter 的 `Queries/<名称>/<名称>.xml`）的映射都会被注册；更深则需要在该文件的 `EmbeddedResource` 上写 `LogicalName` 把逻辑名压回浅层，LisCenter 对全项目唯一一份三级目录映射（`Queries/ExternalQuery/AvailableResources/ExternalAvailableResourceQuery.xml`）正是这样处理的。本次把 19 个映射移入 `MedicalRecognitionReportAggregate/SqlMap/<分组>/`，逻辑名深到三段目录（`…Repository.MedicalRecognitionReportAggregate.SqlMap.Reports.MedicalRecognitionReport.xml`），既超出默认模式覆盖范围、又未配 `LogicalName`，因此 19 个映射全部不被发现；未移动的 `Queries/MedicalRecognitionReportQuery.xml` 仍是一级目录，因此读接口不受影响。各映射文件的 `Scope` 属性自始至终未改，缺失的是注册而非改名。

**本项目对映射位置的约定**：不使用 `LogicalName`；映射文件与其仓储类同目录，固定放在程序集根下的一级目录内。本阶段交付的聚合仓储与其 19 个映射同处 `MedicalRecognitionReportAggregate/`，与 LisCenter「一个聚合目录内 xml 与仓储类并列」的形态一致。

**为什么既有自动化没有拦住**：`Stage1SqlMapProbeTests` 当时按内嵌资源清单反推命名空间后自行注册，探测的是自建注册表而不是宿主的注册表；资源清单里有文件就注册，因此映射文件被嵌套后该用例仍通过，433 条测试全绿。

**排除陈旧生成物**：删除 `server/` 下全部 14 个 `obj`/`bin` 后重新 restore 与全量构建（0 错误 / 190 警告），用全新产物启动宿主复测，症状与重建前完全一致，确认不是残留产物所致。

**处置**：

1. 19 个映射文件移回 `MedicalRecognitionReportAggregate/` 平铺，删除 `SqlMap/` 目录树。
2. 重写 `Stage1SqlMapProbeTests.Runtime_registration_reports_full_sql_ids_without_opening_database`：注册范围改为本项目固定的资源命名形态 `Dy.MedicalRecognition.Repository.*.*.xml,Dy.MedicalRecognition.Repository`，不再由资源清单反推；删除原 `SqlMapNamespaces` 推导方法。
3. 新增 `Stage1SqlMapProbeTests.Sql_map_resources_stay_directly_under_one_folder`：断言每个 `.xml` 内嵌资源名去掉程序集前缀与 `.xml` 后缀后只含一个点，钉住「根 + 一层目录 + 文件名」且与仓储类同目录的约定。
4. 删除 `Stage4ConstraintTests.Mapping_files_stay_in_their_business_capability_folders` 及其助手（该断言冻结的正是已废弃的分组目录），并把它与 `Stage4SqlMapTests`、`Stage2WritePathTests`、`Stage3WritePathTests` 中写死分组路径的定位助手统一改回聚合目录。

**守卫有效性验证**：把一份映射文件临时放入子目录后，`Runtime_registration_reports_full_sql_ids_without_opening_database` 报 `Assert.Contains() Failure: Item not found in collection`、`Sql_map_resources_stay_directly_under_one_folder` 报 `Assert.Equal() Failure: Values differ`，两条同时失败；还原后两条均通过。

**构建与自动化基线**：`dotnet build` 0 错误 / 190 警告；`dotnet test` 433/433 通过。

### 真实宿主复测（2026-09-19）

进程：后端 `dotnet run --project Dy.MedicalRecognition`（监听 `http://localhost:15014`，OpenAPI 39 条 path）；前端 `pnpm -F dy-medical-recognition dev`（`::1:3008`）。令牌取自宿主登录会话的认证存储，未复制或写入任何位置，未直接写库；两份报告由提交接口本身构造，请求值取自本报告「验收请求样例」节。

| 验证面 | 结果 |
|---|---|
| 写端点作用域 | `EnableMedicalStandardCategory`、`DisableMedicalStandardGroup`、`EnableMutualRecognitionItem`、`ChangeMedicalStandardItemRemark` 均不再报缺作用域，改为业务拒绝「分类不存在／分组不存在／互认项目配置不存在／标准项目不存在」 |
| 检验报告提交 | `POST /api/v1/report-pdf/laboratory-report` 返回 `HTTP 200` 与 `true` |
| 检查报告提交 | `POST /api/v1/report-pdf/examination-report` 返回 `HTTP 200` 与 `true` |
| 列表回读 | `QueryBranchMedicalReportList` 返回 `HTTP 200`、`totalCount=25`（提交前为 23）；`MR-ACC-LAB-20260919-001`（检验报告、验收患者甲、有效、当前版本序号 1）与 `MR-ACC-EXAM-20260919-001`（检查报告、验收患者乙、有效、当前版本序号 1）均在列表中 |
| 检验版本详情 | `QueryMedicalReportVersionDetail` 返回 `HTTP 200`：普通结果 2 条、细菌鉴定结果 2 条、首条细菌的药敏结果 2 条、首条结果危急值标志 `false`、`examinationContent` 为空、联系电话脱敏为 `*******1111` |
| 检查版本详情 | 报告类型文本「检查报告」、检查所见与结论非空、影像调阅地址与来源值一致、来源影像状态文本「有影像」、检查项目 2 条、首项目检查部位 2 条、`laboratoryContent` 为空、PDF 文件名「验收样例检查报告.pdf」 |
| PDF 下载 | `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf` 返回 `HTTP 200`、`content-type: application/pdf`、77 字节、首字节序列 `%PDF-1.4` |
| Console | 无业务脚本错误；出现的 500 记录全部来自本轮刻意构造的探测请求（业务拒绝按既有口径以 `HTTP 500` 加纯文本返回） |

**本轮验收数据**：报告 2 份（检验 1 份、检查 1 份），PDF 落盘 `var/report-pdf/20260919/` 2 个文件；均为新形成的业务数据，按阶段口径保留不清理。

**受影响条目的重新取证口径**：缺陷存在期间（映射目录整理之后至本次修复之前），本报告在 2026-09-18 取得的写入类条目证据（V1、V2、V4-V8、V13-V15、V19、V19b、V20、V45-V51、V53-V55、V57、V59、V85、V86 等）在该代码状态下不成立。其中 V85、V86 另因设计变更作废（见「设计变更登记」），不在本轮重新取证范围内。2026-09-19 修复后已按真实入口重新取证，结果如下。

### 修复后按真实入口重新取证的条目（2026-09-19）

| 编号 | 用例 | 本轮实测 |
|---|---|---|
| V1 | 首次提交完整检验报告 | `POST /api/v1/report-pdf/laboratory-report` 返回 `HTTP 200` 与 `true`；列表回读得到报告类型文本「检验报告」、生命周期状态「有效」、当前版本序号 1；版本详情返回普通结果 2 条、细菌鉴定结果 2 条、首条细菌药敏 2 条、首条结果危急值标志 `false`、`examinationContent` 为空、联系电话脱敏 `*******1111` |
| V2 | 同一报告再次提交追加版本 | 同一报告单号再次提交返回 `HTTP 200` 与 `true`；版本列表为 2 行，版本 2 `isCurrentVersion=true`、版本 1 `isSuperseded=true`；报告主体的当前版本序号为 2；版本 1 的内容未被改写（仍为 2 条普通结果与 2 条细菌鉴定结果，患者姓名不变）；报告行数未增加 |
| V17、V37、V39、V40 | 提交成功可回读、明细与展示序号 | 见 V1 行的版本详情结果，字段与条数与提交值一致 |
| V9、V61、V69、V73、V74（部分） | 列表查询、名称回填、枚举文本与脱敏 | 列表按服务端回填来源组织／医院／院区名称（「县医共体」「县人民医院」「总院」）而非编码拼接；报告类型与状态返回中文文本；版本详情的联系电话脱敏为 `*******1111`。本项未复测原用例中的 `totalCount=1` 与跨身份可见性面 |
| V19 | PDF 文件本身校验失败即拒绝整份报告 | 逐项构造不合规文件部件提交，五类全部按业务拒绝返回：内容类型非 `application/pdf`、扩展名非 `.pdf`、文件头签名无效、零长度、文件部件缺失；五个报告单号在列表中**零残留**（列表总数不变） |
| V13 | 作废语义：不生成内容版本、不物理删除 | 作废时间取当前时刻时返回 `HTTP 200` 与 `true`；列表回读报告状态为「已作废」；版本行仍为 2 行、当前版本指向未变 |
| V14 | 作废时间下界分支 | 作废时间取当前版本平台接收时间之前 1 小时与之前 1 秒两次，均返回 `HTTP 500`「业务拒绝：作废时间不得早于当前版本的平台接收时间」 |
| V15 | 作废幂等与冲突分支 | 同一作废时间与同一原因再次作废返回 `HTTP 200` 与 `true`；同一作废时间与不同原因返回 `HTTP 500`「业务拒绝：报告作废信息与已保存的作废事实不一致」，首次作废事实未被覆盖 |
| V4 | 作废后再次提交 | 同一报告单号再次提交完整报告返回 `HTTP 500`「业务拒绝：报告已作废，不能再次提交。」；版本行未被新增 |
| V46-V51、V53-V55、V57、V59 | 完整检查报告提交与回读 | `POST /api/v1/report-pdf/examination-report` 返回 `HTTP 200` 与 `true`；版本详情返回报告类型文本「检查报告」、检查所见与结论非空、影像调阅地址与来源值一致、来源影像状态文本「有影像」、检查项目 2 条且首项目检查部位 2 条、`laboratoryContent` 为空、PDF 文件名「验收样例检查报告.pdf」 |
| V75 | 下载返回文件流与下载名 | `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf` 返回 `HTTP 200`、`content-type: application/pdf`、77 字节、首字节序列 `%PDF-1.4` |
| V76 | 下载版本不存在 | 以同一报告标识与不存在的版本标识请求下载返回 `HTTP 500`、`content-type: text/plain`（不是 `application/pdf`）、「业务拒绝：报告版本不存在或不属于该报告。」，不返回文件内容 |
| V83（鉴权面） | 端点受既有授权策略保护 | 不带令牌请求写端点返回 `HTTP 401`；带宿主令牌的写端点返回 `HTTP 200` 与业务结果 |
| C28（只读验收面） | 医院管理员页列表、详情与版本区分 | 从宿主页面实测：列表渲染 25 行并显示本轮两份报告，`MR-ACC-LAB-20260919-001` 显示当前版本序号 2 与状态「已作废」，`MR-ACC-EXAM-20260919-001` 显示版本序号 1 与状态「有效」；详情面板渲染「报告详情与历史版本」，两个版本分别标为「历史版本／已被后续版本替代」与「当前有效版本」，版本内容随选择正常渲染 |

**本轮未重新取证的条目**：V5-V8、V27、V28、V42、V87（提交校验拒绝语义）、V18（零写入面的其余分支）、V19b（文件名回退）、V20（单文件上限，需 80 MB 量级载荷，属控制器侧、不经映射）、V45 与 V79（写库失败后的文件补偿；V19 的零残留只间接覆盖其中一部分）、V62、V91 与 V61（跨医院可信范围与取值来源口径）、V88（宿主请求体上限）、V89-V91 的其余面、C29-C31（页面失败态与跨院区归属失败的页面表现）。这些条目在本轮修复后的代码状态下尚未复测，本报告不据此声明任何状态。

**设计变更引起的条目变化**：V85、V86（并发首次提交与并发首次解析患者）对应的实现与用例已按阶段设计变更移除——本项目不再对唯一约束冲突做翻译，并发冲突按数据库原始失败暴露。这两条不再有对应证据，其原 `Passed` 结论随之失效，已改记 `PendingRetest` 并在结果表与「设计变更登记」中写明作废原因。

### 命名空间整理后的宿主验收（2026-09-19 第二轮）

`Domain.Share` 侧聚合事件与请求的命名空间改为带 `.Share` 段之后（见「Domain.Share 侧聚合事件与请求的命名空间改为带 `.Share` 段」节），从宿主登录页重走一遍读写链路，确认该整理不影响运行时行为，并顺带补齐上一节列出的部分未复测条目。

进程与入口：后端 `dotnet run --project Dy.MedicalRecognition`（监听 `http://localhost:15014`，OpenAPI 39 条 path）；前端 `pnpm -F dy-medical-recognition dev`（`::1:3008`）；宿主 `http://183.224.180.166:35000`。令牌取自宿主登录会话的认证存储，未复制或写入任何位置，未直接写库。

| 面 | 本轮实测 |
|---|---|
| 开发地址拦截 | 子应用入口来自 `http://localhost:3008/subApps/medical-recognition/branch-report-management`（`304`） |
| 列表 | `QueryBranchMedicalReportList` 返回 `HTTP 200`，页面渲染 25 行（本轮基线） |
| 详情与版本切换 | `QueryMedicalReportVersionList` 与连续三次 `QueryMedicalReportVersionDetail` 均 `HTTP 200`；选择历史版本与当前版本分别渲染「版本内容（第 1 版）」与「版本内容（第 2 版）」 |
| PDF 下载 | `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf` 返回 `HTTP 200` |
| 检验报告提交与回读 | `POST /api/v1/report-pdf/laboratory-report` 返回 `HTTP 200` 与 `true`；列表由 25 增至 26；行显示报告类型文本「检验报告」、状态「有效」、当前版本序号 1；版本详情返回普通结果 2 条、患者姓名与提交值一致、联系电话脱敏为 `*******3333`、PDF 名按原名保存。本次提交使用新证件号码，同时覆盖平台患者创建路径 |
| 检查报告提交与回读 | `POST /api/v1/report-pdf/examination-report` 返回 `HTTP 200` 与 `true`；列表由 26 增至 27；版本详情返回报告类型文本「检查报告」、来源影像状态文本「无影像」与影像调阅地址为空（与提交值一致）、检查项目 2 条、首项目检查部位 1 条、`laboratoryContent` 为空 |
| 提交校验拒绝语义 | 七类非法请求逐条按业务拒绝返回且措辞与矩阵登记一致：来源明细标识重复、展示序号重复、展示序号为零值、检测人单边提供、就诊类型未知值、申请时间晚于报告时间、证件已存在但患者姓名不一致。七个报告单号在列表中**零残留**；其中证件已存在但姓名不一致一次随提交整体回滚，与事务边界修复后的结论一致 |
| V19b 文件名回退 | 以含路径分隔符与控制字符的文件名提交返回 `HTTP 200` 与 `true`（不拒绝报告），回读 PDF 文件名为「报告单号加扩展名」；对照：安全文件名按原名保存 |
| 新提交报告的下载 | 三份新报告的 `GET …/pdf` 均返回 `HTTP 200`、`content-type: application/pdf`、77 字节、首字节序列 `%PDF-1.4` |
| 页面复核 | 列表 28 行；本轮三条新报告在页面上渲染为「有效」、当前版本序号 1 |

本轮验收数据：报告 3 份（检验 `MR-ACC-LAB-20260919-002`、`MR-ACC-LAB-20260919-003`，检查 `MR-ACC-EXAM-20260919-002`），PDF 落盘 `var/report-pdf/20260919/` 共 6 个文件（含第一轮检验报告的两个版本）；被拒请求零残留。

本轮未覆盖面：单文件上限与宿主请求体上限（需 80 MB 量级载荷）、写库失败后的文件补偿面、跨医院可信范围与取值来源口径、页面失败态与跨院区归属失败的页面表现、并发首次提交与并发首次解析患者（后两条已按设计变更记 `PendingRetest`）。

## 本轮收尾取证与缺陷处置（2026-09-18）

本轮收尾取得新证据，并发现两个使既有结论不成立的缺陷。两个缺陷均已按修改代码处置，并先取得失败用例再修复，随后在真实宿主链路上复测通过；修复内容、失败证据与复测结果见「缺陷十二与缺陷十三的修复与复测」节。

### 新取得的证据

| 编号 | 用例 | 层级 | 状态 | 证据出处 |
|---|---|---|---|---|
| C30 | 宿主页面四类失败态 | Host | `Passed` | 从宿主登录页进入「本院报告管理与历史版本」，先取得基线（列表 23 行）；随后在子应用自身的运行窗口中按端点注入可控失败（子应用运行在 micro-app 沙箱内，注入须落在子应用窗口，落在宿主窗口不生效）。**列表读取失败**：提示「报告列表数据待刷新／读取失败，已有数据保留；可重试读取」并给出「重试」按钮，当页 10 行保留、加载态结束。**版本列表失败**：提示「报告历史版本待刷新／版本列表读取失败，已有数据保留；可重试读取」，详情保持打开、列表行数不变。**版本详情失败**：提示「报告版本内容待刷新／该版本内容读取失败，当前版本选择与已有内容保留；可重试读取」，已有内容仍渲染、版本行 3 行不变。**PDF 下载失败**：提示「报告 PDF 下载未完成／本次下载未成功，当前详情与版本选择保留；可重新下载」，`URL.createObjectURL` 未被调用（未误报成功）。四类均无本地接口错误提示、无误刷新 |
| C31 | 下载跨院区归属失败的页面表现 | Host | `Passed` | 以 `yangkj` 的令牌请求 `lisadmin` 所属医院那份报告的版本（报告标识 `3a23c461-1730-55b7-d8b7-ef9ce2159efb`、版本标识 `3a23c461-175d-077a-fa11-2977ac576345`）：服务端返回 `HTTP 500`、`content-type: text/plain`，异常为「业务拒绝：报告不在当前可信组织、医院与院区范围内」，不返回文件内容。页面侧在同一失败态下实测：详情保持打开、版本行 3 行与列表行数均保持、加载态结束、`URL.createObjectURL` 未被调用、无本地错误提示与消息提示 |
| V88（宿主侧） | 宿主请求体上限与超限失败语义 | Integration | `Passed`（宿主侧） | 该上限在应用源码内无显式配置键，由框架程序集 `Dy.Apron.dll`（`dy.apron.quickstart/1.1.0.48`）的 `ConfigureKestrel` 设置 `KestrelServerLimits.MaxRequestBodySize` 生效。**逐点探测（已登录并携带宿主令牌）**：文件部件 50000000 字节（总请求体 50000196 字节）可到达领域层；104857601 字节的纯文本部件请求可到达控制器动作；104857602 字节及以上、以及 104900000 字节的文件部件请求被框架中止连接，未产生业务响应。据此确认宿主请求体上限约在 104857600 字节量级。**框架层超限的实际失败形态**为中止连接（`HttpClient` 与浏览器上下文两种方式实测一致），不是可识别的 HTTP 响应；此前的记录称该场景返回 `HTTP 400` 加 `application/problem+json`，本轮未能复现，故按实测形态登记。另需注意未带令牌的请求在读取请求体之前即返回 `HTTP 401`，会掩盖该上限，探测该上限必须携带令牌。**两层失败语义已可分别取证**：单文件上限取 83886080 字节后，控制器层超限拒绝可达（见 V20 行，`HTTP 500` 加纯文本），框架层超限按中止连接表现，两者不再混同。部署侧网关的请求体上限见「部署前置」一节，属部署形态、不属本阶段交付物 |

### 受缺陷影响的条目状态

| 编号 | 用例 | 层级 | 状态 | 说明 |
|---|---|---|---|---|
| V63 | 列表时间范围筛选边界 | Query | `Passed` | 请求侧两个筛选字段改为日历日期（`DateOnly`）后，下推的两个边界值不带时区标注，起始日零点含边界、结束日次日零点不含上界，见「缺陷十二与缺陷十三的修复与复测」节。修复前该条记 `Failed` |
| V84 | Request、ReadModel 与分页契约 | Contract | `Passed` | 分页请求已改为业务筛选加内嵌 `page: { pageIndex, pageSize }`，响应为 `items` 加 `page` 且 `totalCount` 为长整型，见「缺陷十二与缺陷十三的修复与复测」节。修复前该条记 `Failed` |

上述两条的处置为修改代码：缺陷十二按设计已确认的请求形状对齐实现并重新生成前端客户端；缺陷十三把两个筛选字段改为日历日期。两条都先取得失败用例再修改，并已按真实入口复测，见下节。

### 缺陷十二与缺陷十三的修复与复测

两条缺陷的处置均为修改代码。两条都先取得可复现的失败用例、再修改实现，并在真实宿主链路上复测。

**缺陷十二：分页请求形状与设计不符（影响 V84，已修复）。** 原状：`MedicalReportQueryRequests.cs` 的 `PageRequestDto` 直接暴露 `PageIndex` 与 `PageSize`，`MedicalReportListQueryRequest` 继承为平级字段；生成产物 `openapi/medical-recognition.openapi.json` 中 `ReportListQueryRequest` 为 11 个平级属性、无 `page` 对象；前端适配层 `reportsApi.ts` 同形平级提交；`Stage4ContractTests.cs` 以属性名集合相等断言把平级形状固定，使该不一致不会被既有用例发现。

修复：`PageRequestDto` 改为内嵌分页对象，两个列表请求以 `Page` 承载它；`MedicalRecognitionRequestValidator` 增加对内嵌复杂类型的递归校验，使内嵌对象上的 `[Range]` 声明生效；应用服务改读 `request.Page`；前端适配层改为提交内嵌 `page` 对象；按下发文档重新生成 Kiota 客户端（`paths` 39 条、14 处整数联合类型修正、锁文件保留 `/auth/login` 排除），生成的 `ReportListQueryRequest` 含 `page?: PageRequestDto`。

复测证据：新增用例 `Stage4ContractTests.List_request_body_nests_paging_under_page` 在修改前失败（序列化结果中没有 `page` 对象，`JsonElement.GetProperty("page")` 抛出 `KeyNotFoundException`），修改后通过。真实链路上从宿主登录页以 `yangkj` 进入「报告管理与历史版本」，实测请求体为 `{"branchCode":"CSYQ1-1","hospitalCode":"CSYY1","organizationCode":"01","page":{"pageIndex":1,"pageSize":10}}`，响应 `HTTP 200` 与 `{"items":[],"page":{"pageIndex":1,"pageSize":10,"totalCount":0}}`，顶层不再有 `pageIndex`、`pageSize`。

**缺陷十三：列表时间范围筛选的上界取到结束日 08:00（影响 V63，已修复）。** 原状：请求侧两个筛选字段为 `DateTime`，由 `System.Text.Json` 解析为带时区的值并原样下推；上界又先做 `ReportDateTo?.Date`，`.Date` 作用在带时区的值上取到 UTC 日期，数据库驱动写入 `timestamp without time zone` 列时按本机时区（UTC+08:00）再换算一次，使有效上界取到**结束日的 08:00**而非结束日次日零点。既有取证未暴露该缺陷，是因为验收数据恰好全部位于 `2026-09-18 01:00` 至 `01:05`，落在已包含的区间内。

修复：请求侧两个筛选字段改为日历日期 `DateOnly`（只提交年月日、不携带时刻与时区），服务端新增 `ToStartOfDay` 与 `ToStartOfNextDay`，按日期分量构造两个边界，结果不带时区标注；前端适配层的 `toRequestDate` 改为按 `YYYY-MM-DD` 文本构造生成的 `DateOnly`（该类型由客户端包入口转出，子应用不直接声明底层依赖）；[Server/design.md](Server/design.md) 的筛选口径与 V63 行同步写明该形状与「边界不带时区标注」的要求。

复测证据：新增用例 `Stage4QueryTests.Report_time_range_bounds_carry_no_time_zone` 在修改前失败（`Expected: Unspecified, Actual: Utc`），修改后通过。真实链路上以同一宿主页面实测：单日筛选 `2026-09-18` 的请求体为 `{"branchCode":"0101001","page":{"pageIndex":1,"pageSize":10},"reportDateFrom":"2026-09-18","reportDateTo":"2026-09-18"}`（修改前同一下发为 `2026-09-17T16:00:00.000Z`），返回 `totalCount: 21`；筛选 `2026-09-01` 至 `2026-09-17` 返回 `totalCount: 0`，确认上界为结束日次日零点而不是当天 08:00。

### 宿主验收对账与收尾轮补取证

阶段收口前对唯一的收口票（[票 12](阶段4-Tickets/12-宿主链路验收与阶段报告.md)）的 14 条勾选项逐条对账，并对对账中查出的缺口补取证。

**对账结论**：14 条中 13 条有证据、1 条为部分。逐条判定如下。

| 条 | 内容 | 判定 |
|---|---|---|
| 1 | 平台管理员页列表、详情、版本切换、下载（C28） | 有证据（结果表 C28 行） |
| 2 | 医院管理员页范围固定（C29） | 有证据（C29 行） |
| 3 | 两个身份交叉验证可见性（C27、C28、C29、V62、V91） | 有证据（V62 与 V91 行） |
| 4 | 宿主失败态四类（C30） | 有证据；本轮已按当前代码复测，见下 |
| 5 | 下载跨院区归属失败（C31、V91） | 有证据；本轮已按当前代码复测，见下 |
| 6 | 四个接入接口成功与失败语义（V1、V4、V17、V18、V19、V20、V89） | 有证据 |
| 7 | 生命周期、患者归属、作废、文件补偿（V2、V10-V16、V45、V60、V79、V85、V86） | 有证据；其中 V79 原为部分，本轮已补齐第三面，见下。V85、V86 的证据已因设计变更作废，现记 `PendingRetest`，见「设计变更登记」 |
| 8 | 列表排序分页与患者筛选边界（V64、V65、V67、V68、V90） | 有证据（V67 为本轮新增用例） |
| 9 | 目标库十张表结构与索引核对（V80b） | 有证据 |
| 10 | 矩阵全量检查与未取证条目登记 | 有证据（「矩阵条目状态清单」节） |
| 11 | 无无据通过措辞、报告含规定栏目 | 有证据 |
| 12 | 三项已接受风险与一项框架观察面 | 有证据（「已确认的验证降级」节） |
| 13 | 票 16 与票 17 结转结论收口 | 有证据（`Stage4ConstraintTests` 与 `Stage4EndpointTests` 全绿） |
| 14 | `git diff --check`、构建与静态检查已执行 | 有证据 |

**补取证一：C23 至 C26（医院管理员页组件面）。** 该四项此前在代码与规范符合性审查中被记为交付缺口，且 `BranchReportManagement.test.tsx` 不存在。本轮新增该用例文件（7 项）：C23 覆盖组织与医院只读固定展示、范围选择器只渲染院区一级、请求不提交组织与医院、可信范围缺失时阻断且不发业务请求；C24 覆盖可信院区缺失时不默认选中、切换院区只提交给服务端不做本地归属过滤；C25 覆盖复用同一套列表主体与按服务端回填名渲染；C26 覆盖列表读取失败时保留数据、结束加载、可重试、无本地错误提示。

**补取证二：V79 第三面（既有版本与其文件不受影响）。** 矩阵预期含三面，原证据为部分。新增 `Stage4SubmissionPathTests.Failed_submission_leaves_existing_version_and_its_file_untouched`：建立既有报告与版本作为基线，让追加版本的提交在写库阶段失败，断言既有版本行逐字段未变、其文件键仍可读。同时把文件存储替身的保存与删除改为可观察。写该用例时确认了两处边界并按实情收窄断言：矩阵第一面「失败后库中无版本记录」由工作单元的事务回滚保证，内存替身不承载事务，该面以真实入口取证为准；第二面「新文件已删除」由宿主控制器负责，同以真实入口取证为准。

**补取证三：本地后端端口由 5014 改为 15014。** 原因为 `5014` 落入 Windows 的动态端口预留区间（`netsh interface ipv4 show excludedportrange protocol=tcp` 实测为 4919 至 5018），该区间由 Hyper-V、Host Network Service 与 WSL 在开机与网络栈重排时动态申请，落在其中的端口无法绑定，绑定报 `SocketException 10013`，且区间边界随每次开机与重排移动、不构成稳定可用端口；`15014` 位于动态端口范围（1024 至 15000）之外且不在任何预留区间内，实测可绑定。同步范围与登记见 [test-environment.md](../../../.agents/instructions/test-environment.md) 第 1 节与第 30 行；本轮同时替换了各阶段文档与证据文件中该端口的记载。**部署占位地址 `http://183.224.180.166:35014` 未受影响**（其数字串含 `5014` 子串，替换时按词边界保护，替换后逐处核对完好）。

**补取证四：C30 与 C31 按当前代码复测。** 两条的既有证据早于本轮对分页形状、日期类型、共享分页控件与单文件上限的改动，因此在新端口上重新取证。

C30 列表读取失败态：在子应用自身的运行窗口内按端点注入可控失败后实测，提示「报告列表数据待刷新／读取失败，已有数据保留；可重试读取。」并给出「重试」按钮，当页 10 行保留，加载态结束（无进行中的加载指示），无本地接口错误提示。下载失败态：`URL.createObjectURL` 调用次数保持 0（未误报成功），详情保持打开、版本行 13 行不变、无本地错误提示。

C31 跨院区归属失败：以 `yangkj` 的令牌请求 `lisadmin` 所属医院的报告版本，服务端返回 `HTTP 500`、`content-type: text/plain`，异常为「业务拒绝：报告不在当前可信组织、医院与院区范围内」，不返回文件内容（响应体不以 `%PDF` 开头）。四条与既有记录一致。

### 设计变更登记

- **V56「检查项目与部位可选人员成对规则」记 `N/A`。** 检查项目与部位两级在设计的字段表、提交输入类型 `ExaminationItemRequest` 与 `ExaminationSiteRequest`、以及 SRS F03.3 的属性表中均未定义人员字段，因此不存在可构造的单边提供请求。经负责人裁定，[Server/design.md](Server/design.md) 的 V56 行改记 `N/A` 并写明依据，[票 06b](阶段4-Tickets/06b-检查报告完整内容.md) 的勾选项已移除该编号引用。
- **单文件上限由 104857600 字节改为 83886080 字节（S4-D7、S4-D37）。** 宿主请求体上限由框架程序集 `Dy.Apron.dll` 的 `ConfigureKestrel` 设置、应用源码内无配置键，实测约在 104857600 字节量级；单文件上限与该值相同时，任何超限文件都会使整个请求体超过宿主上限而在读取阶段被处理，控制器 `ValidatePdfFile` 的「超过单文件上限」分支不可达。因此单文件上限改为 83886080 字节并写明必须低于宿主请求体上限。设计文本同步位置：[design.md](design.md) 的 PDF 文件段、S4-D7 与 S4-D37 两行、[阶段4-规格.md](阶段4-规格.md) 第 98、123 行与 S4-D7 决定表行、[Server/design.md](Server/design.md) 的提交请求信封节、存储契约与实现节、配置示例与 V20 行。
- **提交输入类型的层归属与名称按实现更正（`Server/design.md` 的类型表）。** 检验与检查完整报告的提交输入类型位于 `Domain.Share`：领域命令直接以它们作为属性类型，而领域项目只引用 `Domain.Share`，这些类型放在 `Application.Contracts` 会使领域命令反向引用应用层；类型表的层列因此由 `C/Aggregate/Requests` 改为 `S/Aggregate/Requests`。同轮更正两处与实际不符的类型名（`BacterialIdentificationResultRequest` 改为 `LaboratoryBacteriaResultRequest`、`DrugSusceptibilityResultRequest` 改为 `LaboratoryAntimicrobialSusceptibilityRequest`）、一处与实际不符的表单名（`MedicalReportSubmitForm` 改为 `ReportSubmissionForm`），并把两个在代码中不存在的类型（`MedicalRecognitionReportWriteRequest`、`MedicalReportVersionWriteRequest`）标注为未定义类型、写明其职责由提交命令的直接属性与 `MedicalReportVersionRequest` 承载。请求结构节与表下说明同步。设计文本范围：[Server/design.md](Server/design.md) 的类型表、请求结构节与新增的承载位置说明。
- **上游 UML1 的写请求名称登记与实现对齐。** 因实现未定义两个内部写入请求类型，且细菌鉴定与药敏结果的请求类型名与登记不一致，[1-MedicalRecognitionReport.wsd](../../uml/1-MedicalRecognitionReport.wsd) 的名称登记行改正为 `LaboratoryBacteriaResultRequest` 与 `LaboratoryAntimicrobialSusceptibilityRequest`、移除两个未定义名称并补入 `MedicalReportVersionRequest`，写请求层次说明改为「报告业务标识与文件键、下载名由提交命令的直接属性承载」，`AppendMedicalReportVersionCommand` 的入参改为 `MedicalRecognitionReportEntity` 与 `MedicalReportVersionRequest`。同步登记在[阶段4-规格.md](阶段4-规格.md)的上游同步点表。
- **Meta 参考草稿图的 F03 部分按确认后的设计与实现校正。** [6-MedicalRecognitionCommandContracts.Meta.puml](../../uml/6-MedicalRecognitionCommandContracts.Meta.puml) 为自述参考草稿，其 F03 部分曾登记已废除的两层内部写入请求与设计明确禁止的 `PdfFileStream` 字段。本轮按实现校正：删去两个内部请求类、两个请求类型改名并补齐字段差集（`DiscoveryMethod`、`Susceptibilities`）、两个完整报告请求去掉文件字节字段并改正为 `Version`/`Content`/`Results`/`Items`、一处字段名改为 `MedicalInsuranceChargeItemCode`、关系声明同步。F04 至 F07 部分保持未改动。
- **V69「名称回填调用次数不随行数增长」的真实外部服务不可用面取消。** 该面原先作为独立取证项，要求令外部组织服务真实不可达。参照实现对同一失败路径不设特殊处理：查询组织服务失败时异常直接向上抛出，不降级为空名称或编码拼接，并以抛出异常的替身覆盖该路径。本项目采用同一口径，等价用例为 `Stage4QueryTests.Name_backfill_failure_fails_the_whole_query`，因此该面不再作为独立取证项，V69 按自动化证据记 `Passed`。
- **V88 的部署侧网关请求体上限改为部署前置登记。** 该上限由部署侧网关配置提供，本仓库不承载部署形态，不属本阶段交付物。原作为矩阵取证条目记 `Blocked`，现改为「部署前置」一节的部署说明：部署侧网关的请求体上限须不小于宿主请求体上限（实测约 104857600 字节）。
- **V85、V86 的并发冲突处理改为按数据库原始失败暴露，两条结果状态改为 `PendingRetest`。** 本项目不再识别与翻译唯一约束冲突：报告业务标识、报告版本序号与平台患者证件键三处唯一约束撞键时，数据库异常按原样向外传播，不自动重试、不再返回「已被并发提交，请刷新后重试」一类业务拒绝；报告提交与患者解析路径上的并发复读逻辑随之移除，只保留「影响 0 行后复读判定目标状态」这一零行分支（阶段 2、3 的既有实现未受影响）。同步位置：[Server/design.md](Server/design.md) 的 V85 行（V86 行此前已按新语义登记）、[票 07](阶段4-Tickets/07-重提交并发与文件补偿.md) 的勾选项与口径段、`Stage4WritePathTests` 中五个并发用例（已删除）。两条条目原 `Passed` 证据取自旧语义，已在本报告结果表中改记 `PendingRetest` 并写明作废原因；新语义目前**没有任何用例或真实入口证据覆盖**，其复测口径与用例设计待补。

### 生产文件按一个顶级类型一个文件的整理

[csharp-backend-style.md](../../../.agents/instructions/csharp-backend-style.md) 第 16 行要求一个非生成文件最多一个顶级类型且文件名与类型名一致。本阶段新增的代码与两处既有代码按该要求整理：13 个多类型文件拆为 80 个文件（`Application.Contracts/Queries`、`Domain/Queries`、`Domain.Share` 的 Requests 与 Events、`Domain/.../Commands`、`Application/Queries`、宿主 `Controllers`），`Application/Validation` 下的 `OrganizationPathResolver.cs` 与 `TrustedScopeResolver.cs` 另拆出其并列类型（`OrganizationPath`、`OrganizationPathTarget`、`TrustedScope`）。

做法与验证：全部为声明位置移动，命名空间、类型名、可访问性与成员签名均未改变；两处既有文件中的类型原为嵌套类型，调用方按限定名引用，移动后同步更新 56 处引用为顶级类型名。整理前先跑通既有测试建立基线，逐文件验证构建，全部完成后核对 `server/` 下每个生产文件恰好声明一个顶级类型（嵌套类型不计入）、`dotnet build` 0 错误 0 编译警告、`dotnet test` 423 通过 0 失败且计数与整理前一致、`git diff --check` 干净。

### Domain.Share 侧聚合事件与请求的命名空间改为带 `.Share` 段

[backend-architecture.md](../../../.agents/instructions/backend-architecture.md) 第 5 行要求目标项目可以使用不同的程序集和目录名，但必须先映射到概念层；第 47、74 行把领域事件归入 `Domain` 或 `Domain.Shared`。`Domain.Share` 工程承载的报告聚合事件、请求与常量此前声明为 `Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate[.Events|.Requests]`，与 `Domain` 概念层同命名空间。概念层与命名空间因此不再一一对应：只引用 `Domain.Share` 的项目写一条 `using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;` 即可编译通过，读起来像引用了 Domain 实体；`Domain` 的命令文件也必须写同名 using 才能取到事件类型，而该 using 实际解析到 `Domain.Share` 程序集，依赖方向的静态可读性被削弱。

做法与验证：34 个文件的命名空间改为 `Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate[.Events|.Requests]`，与该工程的目录一致；同步更新 50 个文件中的 55 处 `using`（`Domain` 的命令与管理器、`Domain.Share` 内部、`Application`、`Repository` 与测试），逐个按编译错误定位、不做全局字符串替换——`…Domain.MedicalRecognitionReportAggregate` 同时被 `Domain` 工程的实体与命令合法占用，整体替换会误改不属于本次范围的文件。`MedicalRecognitionReportConst.AggregateId` 的字面量按该常量自身的定义保持不动：它取聚合根的完整类型名，而聚合根位于 `Domain` 工程、位置未变，改写会改变事件所属聚合的对外标识。核对结果：命名空间按「程序集名 + 目录」推导为 **0 处不一致**；全仓源码、映射文件与前端源码中无残留的旧命名空间引用；`dotnet build` 0 错误 190 警告（与改前一致）；`dotnet test` 433 通过 0 失败；宿主启动后读端点返回 `HTTP 200`，写端点正常到达业务层并返回业务拒绝（本轮未写业务数据）。

### 前端共享分页控件与跨阶段分页规范

报告管理页原先在 `ReportManagementBoard.tsx` 内联拼装 antd 受控分页字段，并自带一份页容量可选值 `['10','20','50','100']`。本轮把该能力抽成跨页面共享模块：新增 `client/apps/dy-medical-recognition/src/shared/tablePagination.ts`，导出 `createTablePagination(pageState)`、`PAGE_SIZE_OPTIONS` 与 `MAX_SELECTABLE_PAGE_SIZE`；页容量可选值改为 `[10, 20, 50, 100, 200]`，上限与服务端公共请求声明的 200 一致，并成为本项目该取值域的唯一来源——`reportsApi.ts` 的 `MAX_PAGE_SIZE` 改为从它派生，避免两处取值域漂移。页面改为只传状态与回调，`src/shared/tablePagination.ts` 用具例 `src/shared/tablePagination.test.ts`（4 项）固定取值域、总数直传、加载中禁用与「改变页容量回到第 1 页」四条事实。

同一轮把分页设计落成跨阶段规范：新增 [pagination.md](../../../.agents/instructions/pagination.md)，覆盖公共形状、五个公共与内部类型及其实现位置、禁用字段、服务端流程、SQL 窗口归属（由 `DataMapper.QueryAsync` 的 `pagination` 承载，公共 XML 不写方言分页语法）、前端适配层与共享控件、验证要求、相关条款对照与参照实现清单；[AGENTS.md](../../../AGENTS.md) 第 5 节规范导航新增分页一行；[frontend-application.md](../../../.agents/instructions/frontend-application.md) 第 1 节新增「公共前端模块索引」表，登记 `src/shared/` 下三个共享模块并写明新增共享模块前先完成复用检索。

验证：`pnpm -F dy-medical-recognition build` 通过；`test` 14 个文件、270 项通过、2 项跳过；`lint` 0 错误、3 条既有警告；`pagination.md` 内 8 个文档链接与所引 20 个代码路径、3 个用例名逐条核对存在；`git diff --check` 干净。

## 结果表

| 编号 | 用例 | 层级 | 状态 | 证据出处 |
|---|---|---|---|---|
| V80 | 提交前的最终建表脚本核对 | Static | `Passed` | 10 份脚本静态核对：全部为 `mrec_` 前缀表；列序为主键、UML 业务列序、生命周期状态标志、`oper_time`、`oper_id`；业务时限列为 `timestamp without time zone`、操作时间为 `timestamptz`；字符串列为 `text`；报告主体的报告时间、患者姓名与证件号码为非空业务列且列位置在当前版本指向之后、生命周期状态之前；含 1 条表注释 + 全部列注释 + 索引注释；无 `varchar(`、无数据库默认值、无跨系统外键、无 `alter table` |
| V80b | 目标库实际结构与注释 | Static/DB | `Passed` | 只读连接「协同平台外网」（PostgreSQL `183.224.180.166:15432` / `DysoftHIS` / `public`）核对（见下节） |
| V82（配置面） | 存储配置缺失时的失败语义 | Repository/Static | `Passed` | 以 `Production` 环境启动宿主（`ASPNETCORE_ENVIRONMENT=Production`、不提供 `ReportPdfFiles:RootDirectory` 覆盖）：宿主启动失败并抛出 `InvalidOperationException: 业务拒绝：报告 PDF 存储根目录未配置。`，异常链指向 `ReportPdfFileOptions.ResolveRootDirectoryOrThrow()` 与仓储模块的启动校验；提供 `ReportPdfFiles__RootDirectory` 覆盖后宿主正常监听。证据形态为真实进程启动结果，不隐式回落到应用基目录 |
| V83（端点清单面） | 新增端点的实际暴露 | Static/Contract | `Passed` | 从运行中的宿主取 `http://localhost:15014/openapi/v1.json`：`paths` 共 39 条，其中本阶段新增两条提交自动端点（`/Api/MedicalRecognitionReport/SubmitCompleteLaboratoryReport`、`/SubmitCompleteExaminationReport`）、两条作废自动端点（`/VoidLaboratoryReport`、`/VoidExaminationReport`）、两个列表查询（`/Api/MedicalRecognitionReportQuery/QueryMedicalReportList`、`/QueryBranchMedicalReportList`）、版本列表与版本详情（`/QueryMedicalReportVersionList`、`/QueryMedicalReportVersionDetail`）、报告版本 PDF 读取（`/Api/MedicalRecognitionReport/OpenReportVersionPdf`），以及手写控制器三条路由（`/api/v1/report-pdf/laboratory-report`、`/examination-report`、`/{reportId}/versions/{reportVersionId}/pdf`）；手写路由前缀 `api/v1/report-pdf` 与既有自动端点前缀 `/Api/<接口名>/<动作名>` 不重复、不遮蔽 |
| C1 | API Client 累计生成与手写入口 | Contract | `Passed` | `pnpm -F @dy/api-client-medical-recognition prepare-openapi`：来源 `http://localhost:15014/openapi/v1.json`，修正整数联合类型 16 处、可空引用 8 处，`path` 数量 39，写入 `openapi/medical-recognition.openapi.json`；`pnpm -F @dy/api-client-medical-recognition generate`：`Generation completed successfully`，`kiota-lock.json` 的 `excludePatterns` 保留 `/auth/login`、`descriptionLocation` 指向包内 `openapi/` 稳定文件；生成后核对 `src/index.ts` 手写入口内容未被覆盖（两段结构与便捷创建函数保持原样）；已删除生成过程产生的 `.kiota.log` 临时文件 |
| C2 | 累计范围核对 | Contract | `Passed` | 冻结文档的 `paths` 共 39 条：阶段 1-3 已交付接口全部保留（标准目录 4 项查询与 12 项写命令、互认配置 4 项写命令与 1 项查询、金额 2 项保存与 2 项查询、枚举元数据 1 项），登录接口仍被排除（`kiota-lock.json` 的 `excludePatterns` 含 `/auth/login`），无后续阶段（匹配、处理结果、引用、统计）端点泄漏；生成模型为强类型，报告读模型的枚举与嵌套类型未退化为 `object`（`/Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionDetail` 的响应模型含强类型 `common`、`laboratoryContent`、`examinationContent`） |
| C3 | 构建与类型检查 | Static | `Passed` | `pnpm -F @dy/api-client-medical-recognition typecheck` 通过（`tsc --noEmit` 无输出）；`pnpm -F @dy/api-client-medical-recognition build` 通过（ESM `dist/index.js` 152.22 KB、CJS `dist/index.cjs` 174.06 KB、DTS `dist/index.d.ts` 173.25 KB） |
| V83（路由可达面） | 手写控制器动作与新增自动端点的真实路由 | Static/Contract | `Passed` | 对运行中的宿主逐条发请求（不带 token）：两个上传动作 `POST /api/v1/report-pdf/laboratory-report`、`POST /api/v1/report-pdf/examination-report`，下载动作 `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf`，以及作废与列表查询自动端点 `POST /Api/MedicalRecognitionReport/VoidLaboratoryReport`、`POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportList`，五条请求都返回 `HTTP 401`：说明认证中间件与路由均已匹配到真实端点，`404` 与路由不匹配不出现。控制器的依赖装配由此前的宿主启动成功（OpenAPI 文生成为 39 条 path）间接证明；未带 token 无法取得业务响应 |
| C27（路由可达面） | 两条页面路由的开发服务器可达性 | Integration | `Passed` | 前端开发服务器 `pnpm -F dy-medical-recognition dev` 在 `http://localhost:3008` 监听；请求 `http://localhost:3008/subApps/medical-recognition/` 与 `.../report-management` 均返回 `HTTP 200`（Vite 对应用内路由回落到 SPA 入口，属开发服务器行为） |
| C27（宿主菜单进入面） | 从宿主登录页登录并从菜单进入平台管理员页 | Host | `Passed` | 浏览器控制工具在宿主 `http://183.224.180.166:35000/login` 完成登录：账号 `yangkj`、入口选「检验检查结果互认平台」、密码引用 [Test Environment](../../../.agents/instructions/test-environment.md) 第 1 节唯一登记处，**未写入报告**。登录后令牌载荷实测 `org=01`、`sub=medical-recognition`、`usr=3a217eb3…`，登录用户记录 `orgID=01`、`hosID=0101`、`branchID=0101001`、`isAdmin=false`；`dy-web-micro:dev-url-intercepts` 含 `{"matchKey":"medical-recognition","origin":"http://localhost:3008","enabled":true}` 且全局开关为 `true`。宿主菜单展开后含「报告管理与历史版本」与「本院报告管理与历史版本」两项；点击前者后子应用页渲染出标题「报告管理与历史版本」与副标题「按组织、医院与院区查询报告，查看任一版本的完整内容并下载原始 PDF」。宿主菜单接口 `POST /Api/Menu/QueryAllMenu` 与 `POST /Api/UserQuery/QueryUserRoleMenu` 均返回 `200`，证明菜单子项与角色授权已配置 |
| C27（资源来源面） | 子应用资源来源与宿主拦截生效 | Integration | `Passed` | 网络记录中子应用资源全部来自 `http://localhost:3008/subApps/medical-recognition/`：`@vite/client`、`src/main.tsx`、`src/router/routes.tsx`、`src/router/index.tsx`、`src/contexts/ApiClientContext.tsx` 与依赖预打包文件均返回 `200/304`，宿主自身资源来自 `183.224.180.166:35000/assets/*`，两级拦截均已生效 |
| C28（部分：子应用鉴权与业务读数） | 宿主令牌在子应用业务请求上可用 | Host | `Passed` | 在宿主菜单进入的页面内选中组织后，子应用调用 `POST http://183.224.180.166:35001/Api/Organization/QueryAllOrganization` 与 `.../QueryAllValidHospitalByOrgId`、`.../QueryAllValidBranchByOrgId` 均返回 `200`，并以可信上下文自动回填「测试医院一」「测试院区1-1」；本平台后端 `POST http://localhost:15014/Api/EnumMetadata/GetEnumMetadata` 返回 `200`，证明宿主令牌在本后端通过校验。未选范围时列表显示空态且不发起列表查询（提示「请选择组织、医院与院区后查看报告」），与设计要求一致 |
| C29（范围固定面） | 医院管理员页的可信范围固定展示 | Host | `Passed` | 从宿主菜单「本院报告管理与历史版本」进入后，页面标题为「本院报告管理与历史版本」，并以只读文本展示可信范围「可信范围（只读）：组织 01，医院 0101」，对应级不渲染下拉；筛选区只保留院区可选（下拉已自动带入「总院」），组织与医院无输入项。列表读取出错时页面给出「报告列表数据待刷新／读取失败，已有数据保留；可重试读取」并提供「重试」按钮，符合设计的失败态口径（该次读取失败由缺陷六导致，修复后同一入口返回 `HTTP 200`） |
| 查询入口一致性（V61、V62、V90 的接口面） | 两个列表入口的真实返回 | Integration | `Passed` | 修复缺陷六后，`POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportList`（请求组织 `01`、医院 `0101`、院区 `0101001`）与 `POST /Api/MedicalRecognitionReportQuery/QueryBranchMedicalReportList`（请求只提交院区 `0101001`，组织与医院由可信上下文注入）均返回 `HTTP 200`，`page.totalCount` 均为 6、`pageIndex=1`、`pageSize=10`；平台入口当页返回 6 行，报告类型与状态文本为「检验报告」「有效」「已作废」，来源组织名称为「县医共体」，其中当前版本指向为空的残留行以 `currentVersionSequence=0` 返回而不中断整页 |

## 目标库实际结构核对（V80b）
只读连接「协同平台外网」查询 `information_schema.tables`、`information_schema.columns`、`pg_indexes`、`pg_class`/`pg_namespace` 与 `pg_constraint`：

- **表清单**：`public` 下 `mrec_` 前缀表共 15 张，其中本阶段 10 张全部存在：`mrec_platform_patient`、`mrec_medical_recognition_report`、`mrec_medical_report_version`、`mrec_laboratory_report_content`、`mrec_laboratory_result_item`、`mrec_laboratory_bacteria_result`、`mrec_laboratory_antimicrobial_susceptibility`、`mrec_examination_report_content`、`mrec_examination_item`、`mrec_examination_site`；其余 5 张为阶段 1-3 已建对象。
- **列序、类型与可空性**：逐表逐列与最终脚本一致。报告主体 16 列，列序为 `id`、`organization_code`、`hospital_code`、`branch_code`、`report_type`、`report_no`、`patient_id`、`current_version_id`、`report_time`、`patient_name`、`identity_document_no`、`status`、`voided_time`、`void_reason`、`oper_time`、`oper_id`；三个检索列 `report_time`、`patient_name`、`identity_document_no` 均为 `timestamp without time zone`/`text` 且 `is_nullable = NO`，位置在当前版本指向之后、生命周期状态之前。报告版本 40 列、平台患者 8 列、检验专项内容 18 列、普通检验结果 23 列、细菌鉴定结果 22 列、药敏结果 19 列、检查专项内容 22 列、检查项目 7 列、检查部位 6 列，均与脚本逐列一致；字符串列为 `text`（无 `character_maximum_length`），业务时限列为 `timestamp without time zone`，操作时间列为 `timestamp with time zone`。
- **中文注释**：10 张表注释与全部列注释逐列命中，取值与脚本声明一致（例如 `mrec_medical_recognition_report` 的 `report_time` 为「报告时间」、`patient_name` 为「患者姓名」、`identity_document_no` 为「证件号码」）。
- **索引**：共 9 条业务索引 + 10 条主键索引全部存在且定义逐字一致：`ux_mrec_platform_patient_document (identity_document_type_code, identity_document_no)`；`ux_mrec_medical_recognition_report_business_key (organization_code, hospital_code, branch_code, report_type, report_no)`；`ix_mrec_medical_recognition_report_scope (organization_code, hospital_code, branch_code, report_time DESC, id DESC)`；`ix_mrec_medical_recognition_report_patient_name (patient_name text_pattern_ops)`；`ix_mrec_medical_recognition_report_identity_document_no (identity_document_no text_pattern_ops)`；`ux_mrec_medical_report_version_report_version (report_id, version_number)`；五张专项目表与药敏表各一条版本或归属索引、检查部位一条项目索引。
- **未改动其他对象**：10 张表的跨系统外键计数均为 0；查询范围内没有本阶段之外的新增对象。

## 前端构建、类型检查与测试

- `pnpm -F @dy/api-client-medical-recognition typecheck`：通过（无输出）。
- `pnpm -F @dy/api-client-medical-recognition build`：通过。
- `pnpm -F dy-medical-recognition build`：通过（`tsc -b` 与 `vite build` 均无错误；仅有既有的单包体积提示，与本次改动无关）。
- `pnpm -F dy-medical-recognition lint`：0 错误、3 条既有警告（`react-refresh/only-export-components`，位于本轮未改动的 `ApiClientContext.tsx`、`standardCatalogPrototypeStore.tsx`、`router/index.tsx`）。
- `pnpm -F dy-medical-recognition test`：14 个测试文件全部通过，270 项通过、2 项跳过、0 失败。
- 前端两个页面的实现文件：`ReportManagement.tsx`、`BranchReportManagement.tsx`、`ReportManagementBoard.tsx`、`ReportVersionDetailPanel.tsx`、`ReportManagement.css`、`reportsApi.ts`、`reportsTrustedScope.ts`，以及用例 `ReportManagement.test.tsx`（23 项）、`BranchReportManagement.test.tsx`（7 项）与 `reportsApi.test.ts`；两条路由 `report-management`、`branch-report-management` 已接入 `routes.tsx`。**记账更正**：`routes.test.ts` 尚未同步这两条路由，其路径清单断言不含这两条路径且使用 `expect.arrayContaining`，缺项时不失败，故该文件无法发现缺失。医院管理员页的组件面用例此前缺失（`reportsApi.test.ts` 引用的 `BranchReportManagement.test.tsx` 不存在），本轮已按「宿主验收对账」的结论补齐，见「缺陷十二与缺陷十三的修复与复测」节之后的「收尾轮补取证」节。

## 后端构建与既有测试

- `dotnet build server\Dy.MedicalRecognition.slnx`：0 错误 0 警告。
- `dotnet test server\Dy.MedicalRecognition.slnx`：失败 0、通过 440、跳过 0，总计 440。
- `git diff --check`：报 1 处行尾空白，位于 `.agents/instructions/agent-conduct.md` 中负责人本人的未提交编辑行（退出码 2）。该文件不属阶段 4 改动范围，本轮未对其执行任何写操作。
- 测试补齐轮次新增 `Stage4SqlMapTests.cs`（6 项）、`Stage4EndpointTests.cs`（7 项）、`Stage4ConstraintTests.cs`（6 项）、`Stage4ContractTests.cs`（19 项）、`Stage4WritePathTests.cs`（88 项）、`Stage4QueryTests.cs`（28 项）、`Stage4SubmissionPathTests.cs`（9 项）与内存替身 `FakeReportRepository.cs`、`FakeQueryRepository.cs`、`StubReportPorts.cs`；同步修改的既有文件包括 `Stage1SqlMapProbeTests.cs`（票 04 注册键断言）、`Stage1ArchitectureTests.cs`（查询契约方法集合与按接口全部分片冻结签名）、`Stage2EnumContractTests.cs`（注册表清单、六个报告枚举的取值与中文说明、冻结 OpenAPI 路径集合）、`Stage2EnumMetadataQueryTests.cs`（排除清单收敛为两个后续阶段枚举）、`Stage2/Stage3` 的查询与写路径替身、`Stage3SqlMapTests.cs`（金额脚本文件名随 `mrec_` 前缀同步）与 `Architecture/SourceGuardReuseTests.cs`。
- 上述用例覆盖的矩阵面（补取证后）：V1-V16、V19b、V21-V36、V38、V41-V44、V46-V55、V58、V60-V67、V68-V74、V77、V78、V80、V81、V83 的静态与入口形态面、V87、V89、V90 的契约/领域/应用/查询映射层级。V85 与 V86 的并发用例已按设计变更删除，不再包含在本声明内。逐条状态见「矩阵条目状态清单」节。
- **记账更正**：以下五个编号原先被上述区段包含，经逐条复核在测试工程内无对应编号回挂，行为面亦无独立断言，故当时从覆盖声明中移出：V34「药敏归属字段不可为空」、V43「异常标志与危急值标志取值」、V48「检查项目必填字段」、V50「检查部位归属项目」、V67「列表排序稳定」。其中 V48 的编号在 `RequestValidationProbeTests.cs` 中出现，但该文件用例全部针对阶段 1 与 2 的请求类型，属跨阶段编号重号，不构成阶段 4 的覆盖。V84 因缺陷十二从覆盖声明中移出。**上述编号已在本轮收尾补取证中逐条补齐**，见「矩阵条目状态清单」节。
- 未自动化、留待票 12 的层级面：V17、V18、V37、V39、V40、V45、V57、V59、V60、V79、V88、V89、V91 的集成面，V19、V19b、V20 的控制器动作行为面（PDF 类型、扩展名、签名、空文件、超限与文件名回退），V80b 的库结构面，V82 的真实存储与非法配置面。V69 的真实外部服务不可用面已取消（参照实现对该失败路径不设特殊处理，只以抛出异常的替身覆盖，本项目已有等价用例）。
- **记账更正**：以下七个编号既未进入上一条的覆盖声明，也未进入本条的未自动化清单，属两处清单同时遗漏，现补登：V38「检验报告作废后停止追加」、V41「检验明细来源字段原文保存」、V54「检查报告来源检查类型与诊断」、V56「检查项目与部位可选人员成对规则」（已按设计变更记 `N/A`）、V58「检查报告作废后停止追加」、V77「下载文件缺失」、V78「已作废报告的历史版本下载」。其中 V38、V58、V77、V78 已在本轮收尾补取证中补齐，V41 与 V54 的持久化断言亦已补齐，见「矩阵条目状态清单」节。

## 执行中发现并修复的缺陷

**作用域冲突导致报告语句在运行期被覆盖（已修复）。** 报告实体的映射文件 `MedicalRecognitionReport.xml` 与阶段 5 的 `RecognitionMatchRecord.xml`、`RecognitionMatchItem.xml`、`RecognitionProcessingResult.xml`、`RecognitionReference.xml` 原先共用同一 SqlMap 作用域名 `MedicalRecognitionReport`；框架按作用域名注册语句集合，同作用域的后续文件整体替换先前注册结果，因此 `MedicalRecognitionReportColumns`、`GetMedicalRecognitionReportByBusinessKey`、`GetMedicalRecognitionReportById`、`InsertMedicalRecognitionReport`、`UpdateMedicalRecognitionReportCurrentVersion`、`VoidMedicalRecognitionReport` 六条语句键在运行期不可见，报告的定位、新增、当前版本指向更新与作废都会报 `Can not find Statement. FullSqlId:MedicalRecognitionReport.<语句标识>`。报告版本、平台患者与七张专项表的注册键不受影响。

修复：把阶段 5 四个文件的 `Scope` 改为各自实体名（`RecognitionMatchRecord`、`RecognitionMatchItem`、`RecognitionProcessingResult`、`RecognitionReference`），语句内容、语句标识与语句内表名不变。该改动落在阶段 5 的物理映射范围内，四文件在本阶段仍不新增调用点，表名前缀与查询口径仍归阶段 5 处理。注册结果的运行期读取由 `Stage1SqlMapProbeTests` 的不连库探针守卫：报告实体六条键必须存在，旧聚合作用域下的阶段 5 键必须消失。

**缺陷二：报告查询语句未进入运行期注册（已修复）。** 新增的报告查询语句原先放在同一作用域下的第二个映射文件里，运行期实测 `Can not find Statement.FullSqlId:MedicalRecognitionReportQuery.CountMedicalReportList` 与 `...GetMedicalReportScope`，即该文件未被注册。修复：把报告列表、版本列表、版本详情、内容读取与文件信息这些语句合并进既有 `Queries/MedicalRecognitionReportQuery.xml`，使同一作用域的语句只由一个文件承载、注册语义与既有查询一致；独立文件已删除。

**缺陷三：报告与报告查询入口把院区当作令牌必填层（已修复）。** 本环境实测登录令牌只带 `org`（`org=01`）与 `sub`，不带院区声明（`brh`），而两个入口原先直接读取 `HttpRequestInfo.BranchId` 并要求非空，导致真实提交在「无法确定当前可信院区」处拒绝。修复：`TrustedScopeResolver` 新增三层解析方法 `ResolveWithBranchOrThrowAsync`，院区与组织、医院两层同规则——令牌该层非空白即取令牌值，缺失时用当前登录用户档案的院区归属补齐，冲突或补齐后为空即拒绝；报告采集与报告查询入口改用该方法，院区不再从请求取值。该口径与参照实现的既有先例一致（令牌三层缺失时用用户档案补齐，冲突即拒绝）。

**缺陷四：明细可选人员在两侧都为空时被误判为缺失（已修复）。** 设计要求明细上的检测人为「应填且成对」的可选字段：单边提供即拒绝、同时为空时接收（V28），而原实现复用了必填成对判定、在两侧都为空时抛出「检测人不能为空」。修复：拆分出 `RequireOptionalPair`，普通结果、细菌鉴定结果与药敏结果的检测人改用它，公共版本的科室与人员、检验人、检查医生仍用必填成对判定。

**缺陷五：布尔列在写入时被按整数绑定（已修复）。** 普通检验结果的危急值标志写入时报数据库错误 `42804: 字段 "critical_value_flag" 的类型为 boolean，但表达式的类型为 integer`。修复：按查询侧既有做法为写入语句声明布尔参数映射（`LaboratoryResultItemInsertParameters`，复用已注册的 `MedicalRecognitionBoolean` 类型处理器）。

**缺陷六：当前版本序号为空的报告使整个列表查询失败（已修复）。** 从宿主菜单进入医院管理员页后，列表读取报 `反序列化出错，实体:[MedicalReportListItem] 属性:[CurrentVersionSequence] -> 列序号:[7],列名:[current_version_sequence]`，内层为 `Column 'current_version_sequence' is null`。原因是列表语句以左连接按当前版本指向关联版本表取版本序号，而当前版本指向为空 Guid 的报告（缺陷七产生的残留行即为此形态）得到 NULL，读取端该属性为非空 `int`，绑定失败并使整页读取中断。修复：列表语句对该列取 `coalesce(v.version_number, 0)`，关联不到版本时归零，读取端不做空值绑定。修复后平台入口与医院入口的列表均返回 `HTTP 200`，两行残留行以 `currentVersionSequence=0` 正常返回而不再中断整页。

**前端缺陷七：报告管理页面的列表分页与详情展示未达用例要求（已修复）。** 前端测试补齐轮次完成后，`ReportManagement.test.tsx` 实测 7 项失败，逐条定位为：① 页容量控件按原生 `<select>` 定位，而本项目所用 antd 6 的 Select 由 `@rc-component/select` 渲染、不产出原生 select，导致页容量控件判定为缺失（改为按既有 `RecognitionProjects.test.tsx` 的 combobox 口径定位，并按语言后缀取前导数字断言取值域）；② 列表行夹具绕过真实适配层出口，枚举文本兜底与文本优先级断言因此不成立（改为经 `toReportPage` 归一，使兜底与可空归一由适配层决定，恰好覆盖 V73 与 C22）；③ 下载断言的实参下标错位（下载出口签名为 `(client, reportId, reportVersionId, fileName)`，改为按 `[1]`、`[2]`、`[3]` 断言）；④ 过期响应丢弃用例漏传第三个实参（补齐页码与页容量）；⑤ 页码越界自愈在实现上被 `pageIndex <= lastPage` 提前返回挡掉，当页已等于按总数算出的最后一页时完全不重取，与「空页与正总数并存须恢复一次」的要求不符（改为两条分支统一推迟到宏任务处理，越界时改为末页、已在末页时按同一页码重取一次，仍由 guard 限制为一次）；⑥ 同一测试的替身入队顺序在整卷执行时可能晚于宏任务，改为在空结果交付前入队以消除时序竞态。修复后该文件 23 项全部通过，整卷 259 项通过且连续两次结果一致。

**修复缺陷八：提交用例的事务声明位置错误，写入后失败不回滚（已修复）。** 运行期实测：在报告插入之后失败的提交返回业务拒绝的同时，在报告主体留下残留行（`patient_id` 与 `current_version_id` 为空 Guid、无版本行），与设计「任一步失败整次提交不保存任何内容」冲突。定位过程：反射与 IL 显示 `WorkUnitEndpointFilter.SetHttpContextInfo` 通过 `GetWorkUnitAttribute(RouteEndpoint)` 从**路由端点元数据**取事务声明，据此写 `HttpContextInfo.TransactionInfo`；而两个提交用例由手写控制器动作直接调用应用服务方法，控制器动作原未声明事务，应用服务方法上的 `[WorkUnit(UseTransaction = true)]` 在该调用路径上不被读取，因此该次请求根本没有开启事务。修复：在两个上传动作上补 `[WorkUnit(UseTransaction = true)]`（应用服务入口上的同一声明保留，供框架自动暴露的端点使用），下载动作仍不带事务。验证：同一失败场景复测零残留，成功路径仍正常落库；`Stage4EndpointTests` 的相关断言按修正后的真实契约同步（两个上传动作必须带事务、下载动作不带）。该修复与设计字面「控制器不承载事务」存在偏离，偏离依据是本框架的事务声明必须落在端点方法上（机制取证见结果表「框架事务机制的静态事实」行），已在此登记以便后续阶段按同一口径处理。

**修复缺陷九：回退下载名未覆盖百分号编码的控制字符（已修复）。** 以含路径分隔符与控制字符的文件名 `bad/na\rme\n.pdf` 提交时，报告被正常接收，但该版本保存的 `pdf_file_name` 为 `na%0Dme%0A.pdf`，而不是设计要求的「报告单号加扩展名」。原因是 multipart 头部把控制字符按百分号编码后传至控制器，`ResolveDownloadFileName` 只检查字面控制字符与系统不可用字符，未命中该形态，于是把不安全名字当作安全名保存。修复：不安全名判定追加百分号编码形态（名字含 `%` 即判定为不安全），并对回退名同样过滤不可用字符（报告单号来自请求，不能把不可用字符带进下载名）。复测：同一场景回退为 `MR-FILE2-20260918-FALLBACK.pdf`，安全文件名 `正常报告二.pdf` 仍按原名保存。

**修复缺陷十：并发首插同一证件键时向外泄漏数据库驱动异常（已修复）。** 四个请求以同一证件号码并发首次提交时，三条返回的不是业务拒绝，而是数据库驱动异常 `Npgsql.PostgresException: 25P02: 当前事务被终止, 事务块结束之前的查询被忽略`。定位过程：并发插入同一证件键触发证件键唯一约束冲突，当前数据库 Provider 会把所在事务标记为终止，此后该事务内的任何语句都被拒绝；领域层按设计在捕获重复患者异常后「复读一次」，而这次复读正好落在已终止的事务内，于是驱动异常自 `ExecuteReader` 抛出并穿透到调用方。设计要求的语义是「撞键时复读一次核对核心身份；三处冲突均翻译为业务拒绝」，在事务已被终止的前提下复读不可执行。修复：仓储新增 `TryGetPlatformPatientByDocumentAsync`，把复读包成「读得到患者 / 读不到且事务已不可读」两种可区分结果（SQLSTATE 判定留在仓储，领域层不引入 Provider 错误码）；领域层在不可读时按并发冲突拒绝并提示刷新后重试。复测：同一场景三条均返回「业务拒绝：患者身份信息并发冲突，请刷新后重试。」，不再出现驱动异常；并发后该证件键在库中恰 1 行。

**修复缺陷十一：版本详情的审核时间投影类型与列可空性不一致（已修复）。** 从宿主菜单进入医院管理员页并打开某份报告的详情时，版本内容区报 `反序列化出错，实体:[MedicalRecognitionReportDetailCommon] 属性:[ReviewTime] -> 列序号:[22],列名:[review_time]`，内层为审核时间列为空导致绑定失败。原因是审核时间在设计与物理表上都是可空列（`mrec_medical_report_version.review_time` 的 `is_nullable = YES`，版本实体的对应属性为 `DateTime?`），而查询投影 `MedicalRecognitionReportDetailCommon.ReviewTime` 声明为非空 `DateTime`；该报告未提供审核时间，空值绑定即失败并使整个版本内容读取中断。修复：投影属性改为 `DateTime?`，应用层映射改为直接透传。核对同表其余时间列的可空性（`application_time`、`report_time`、`received_time`、`source_modified_time` 均为 `NO`）后确认这是该投影中唯一的不一致项。复测：无审核时间的报告返回 `reviewTime: null` 且版本内容正常读取，有审核时间的报告返回原值。

## 接口层真实验收结果

以下证据来自浏览器控制工具在宿主登录会话内直接调用本平台后端（`http://localhost:15014`），请求携带宿主签发的 Bearer 令牌，未复制或注入令牌，未直接写库；报告数据由提交接口本身构造，请求样例见本报告「验收请求样例」节。

| 编号 | 用例 | 层级 | 状态 | 证据出处 |
|---|---|---|---|---|
| V1 | 首次提交完整检验报告 | Integration | `Passed` | `POST /api/v1/report-pdf/laboratory-report`（multipart，文本部件为本报告样例、文件部件为最小 PDF）返回 `HTTP 200` 与 `true`；只读回读 `mrec_medical_recognition_report` 得到报告单号 `MR-ACC-LAB-20260918-001`、报告类型 1、生命周期状态 1、报告时间与样例一致、患者姓名与证件号码与样例一致、来源归属 `01/0101/0101001`；`mrec_medical_report_version` 得当前版本序号 2；检验专项内容 1 条；普通结果 2 条、细菌鉴定结果 2 条、药敏结果 2 条 |
| V2（部分） | 同一报告再次提交追加版本 | Integration | `Passed` | 同报告单号提交两次后 `mrec_medical_report_version` 为 2 行，版本序号 1 与 2 均在，报告主体的当前版本指向为版本 2，报告主体上的报告时间取自当前版本；旧版本行未被改写（版本 1 无明细，版本 2 有 2 条普通结果与 2 条细菌鉴定结果） |
| V17、V37、V39、V40 | 提交成功可回读、明细与展示序号 | Integration | `Passed` | `POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionDetail` 返回 `HTTP 200`：`reportTypeText=检验报告`、`laboratoryContent` 非空、`examinationContent` 为空、普通结果 2 条、细菌鉴定结果 2 条、第一条细菌下的药敏结果 2 条、第一条结果的危急值标志为 `false`（与提交值一致）；`QueryMedicalReportVersionList` 返回 2 个版本并按版本序号升序：版本 1 `isCurrentVersion=false`、`isSuperseded=true`，版本 2 `isCurrentVersion=true`、`isSuperseded=false`，两版本都返回报告医生、审核医生、检验人与 PDF 文件名，版本 2 另返回明细检测人 |
| V9、V61、V69、V73、V74 | 患者规范形式、列表查询、名称回填、枚举文本与脱敏 | Integration | `Passed` | `POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportList` 返回 `HTTP 200`：`page.totalCount=1`、`pageIndex=1`、`pageSize=10`；当页行返回报告单号、`reportTypeText=检验报告`、`statusText=有效`、来源组织/医院/院区名称分别为「县医共体」「县人民医院」「总院」（由服务端按名称回填，非编码拼接）、患者姓名与证件号码原文、当前版本序号 2；版本详情返回的联系电话为 `*******1111`（服务端脱敏） |
| V75 | 下载返回文件流与下载名 | Host | `Passed` | `GET /api/v1/report-pdf/{reportId}/versions/{reportVersionId}/pdf` 返回 `HTTP 200`、`content-type: application/pdf`、字节数 69、首个字节序列为 `%PDF-1.4`（与提交的 PDF 一致）、响应头 `cache-control: no-store, no-cache, must-revalidate`；响应体不含文件键或物理路径 |
| V76 | 下载版本不存在 | Host | `Passed` | 以同一报告标识与不存在的版本标识请求下载：`HTTP 500`，异常为 `InvalidOperationException: 业务拒绝：报告版本不存在或不属于该报告。`，不返回文件内容 |
| V83（鉴权面） | 下载与提交端点的鉴权行为 | Host | `Passed` | 不带令牌请求下载端点返回 `HTTP 401`；带宿主令牌返回 `200`，证明令牌在本后端通过校验且端点受既有授权策略保护 |
| V20 | 单文件上限 | Host | `Passed` | 配置 `ReportPdfFiles:MaxFileBytes` 为 83886080 字节（80 MB），控制器在读取文件流之前按该配置值校验，上限不硬编码。**真实入口取证**：从宿主登录页以 `yangkj` 登录后带令牌向 `POST /api/v1/report-pdf/laboratory-report` 提交 multipart，文件部件 83886080 字节（等于上限、总请求体 83886282 字节）时到达领域层并返回业务拒绝「报告单号不能为空或空白」，即未按超限处理；文件部件 83886081 字节（超过上限 1 字节、总请求体 83886283 字节）时返回 `HTTP 500`、`content-type: text/plain`，异常为「业务拒绝：PDF 文件超过单文件上限」，抛出位置为 `ReportPdfFileController.ValidatePdfFile(IFormFile file)`；文件部件 90000000 字节（总请求体 90000202 字节）结果相同。结论：单文件上限低于宿主请求体上限，超限文件能通过请求体读取阶段到达控制器动作，控制器的超限拒绝分支**可达**且已按真实入口取证 |
| V62（取值来源与拒绝分支） | 医院管理员入口的可信范围与院区归属 | Application/Host | `Passed` | 以 `yangkj`（组织 `01` / 医院 `0101` / 院区 `0101001`）的真实令牌调用医院管理员入口：提交不属于可信医院的院区 `CSYQ2-1` 被拒绝并返回「业务拒绝：院区不存在、已停用或不属于所选医院」；提交可信院区 `0101001` 返回 `HTTP 200`、`totalCount=11`。同轮以 `lisadmin`（组织 `01` / 医院 `CSYY2` / 院区 `CSYQ2-1`）身份提交一份报告（`MR-XSCOPE-20260918-001`，库中来源归属实测为 `01/CSYY2/CSYQ2-1`），用于构造跨医院数据集；该数据集在 `yangkj` 的医院管理员入口列表中**不可见**（`totalCount=11`、返回行全部为「县人民医院」、不存在 `XSCOPE` 行） |
| V91 | 下载归属不匹配 | Application/Host | `Passed` | 以 `yangkj` 的令牌请求下载 `lisadmin` 所属医院的那份报告版本（报告标识 `3a23c461-1730-55b7-d8b7-ef9ce2159efb`、版本标识 `3a23c461-175d-077a-fa11-2977ac576345`，来源归属 `01/CSYY2/CSYQ2-1`）：返回 `HTTP 500`、`content-type: text/plain`（不是 `application/pdf`），异常为「业务拒绝：报告不在当前可信组织、医院与院区范围内」，**不返回任何文件内容**。同一报告标识与版本标识在其所属医院范围内可正常下载（下载面证据见 V75 行） |
| V61（平台入口取值口径） | 平台管理员入口按请求使用三级范围 | Application | `Passed` | 以 `yangkj` 的令牌调用平台管理员入口并按请求提交 `01/CSYY2/CSYQ2-1`：返回 `HTTP 200`、`totalCount=1`、当页含 `MR-XSCOPE-20260918-001`。该结果与设计一致——平台管理员入口的组织、医院与院区取自请求，服务端校验三者存在、启用与父子归属，不要求等于可信上下文；医院管理员入口的组织与医院只取可信上下文并覆盖同名请求字段，两个入口的取值口径差异由 V62 行印证 |
| C28（只读验收面） | 医院管理员页的列表、详情、版本切换与下载 | Host | `Passed` | 从宿主菜单进入页面后实测：① 列表渲染 23 行，逐列有值（报告单号、报告类型文本「检验报告」「检查报告」、报告时间、来源组织「县医共体」、医院「县人民医院」、院区「总院」、患者姓名、证件号码、当前版本序号、报告状态「有效」），表尾显示「共 23 条」并提供分页控件与页容量选择「10 条/页」，与设计的分页呈现口径一致；② 点击「查看报告详情」后右侧渲染「报告详情与历史版本」，表头含版本序号、状态、源端报告修改时间、平台接收时间、报告医生、审核医生、来源报告备注、PDF 文件名与操作列，行内容含「当前有效版本」「报告有效」与下载入口；③ 版本内容随选择渲染，无审核时间的报告返回 `reviewTime: null` 且内容正常（见修复缺陷十一），有审核时间的报告返回原值；④ 版本列表区分当前版本与历史版本（版本 1 `isSuperseded=true`、版本 2 `isCurrentVersion=true`）；⑤ 下载当前版本返回 `HTTP 200`、`application/pdf`、69 字节且首字节为 `%PDF-1.4` |
| V85 | 并发首次提交同一报告业务标识 | Repository/Application | `PendingRetest` | 原证据（2026-09-18）：同报告业务标识的两个请求并发提交，一条 `HTTP 200`、另一条 `HTTP 500`「业务拒绝：该报告已被并发提交，请刷新后重试。」，库中恰 1 行报告。该证据取自「唯一约束冲突翻译为业务拒绝」的旧语义，**已被设计变更作废**（见「设计变更登记」）；新语义下撞键以数据库异常原样向外传播，本条目尚无任何用例或真实入口证据覆盖，待复测 |
| V86 | 并发首次解析同一证件 | Domain/Repository | `PendingRetest` | 原证据（2026-09-18）：同证件号码的四个请求并发首次提交，三条返回 `HTTP 500`「业务拒绝：患者身份信息并发冲突，请刷新后重试。」、一条成功，该证件键在 `mrec_platform_patient` 中恰 1 行；两条不同证件键的并发实验四者全部成功。该证据取自「并发复读 + 冲突翻译」的旧语义，**已被设计变更作废**；新语义下撞证件键由唯一索引拒绝、数据库异常原样传播且不复读，本条目尚无任何用例或真实入口证据覆盖，待复测 |
| V45、V79（部分）、V18（文件面） | 写库失败时删除本次新文件 | Integration | `Passed` | 校验与写入失败的那次提交（缺陷五的 `42804` 数据库错误）后，只读核对存储目录 `var/report-pdf/20260918/` 只存在成功提交对应的那个文件键，失败尝试落盘的文件已被控制器补偿删除，库中也没有该次尝试的版本行；成功提交的文件键与库中版本行的 `pdf_file_id` 一致 |
| V46-V51、V53-V55、V57、V59 | 完整检查报告提交与回读 | Integration | `Passed` | `POST /api/v1/report-pdf/examination-report`（multipart，文本部件为本报告检查样例）返回 `HTTP 200` 与 `true`；只读回读：报告类型 2、生命周期状态 1、版本序号 1、来源归属与样例一致；`mrec_examination_report_content` 1 条（检查所见、检查结论、来源诊断名称、实际检查时间、检查医生、来源影像状态 1、影像调阅地址、设备名称与提交值一致）；`mrec_examination_item` 2 条、`mrec_examination_site` 2 条，且部位随所属项目保存：「胸部CT平扫」挂「右肺上叶」「纵隔」，「胸部CT增强」部位集合为空。`POST /Api/MedicalRecognitionReportQuery/QueryMedicalReportVersionDetail` 返回 `HTTP 200`：`reportTypeText=检查报告`、`examinationContent` 非空、`laboratoryContent` 为空、`sourceImageStatusText=有影像`、影像调阅地址与来源值一致、项目与部位的层级与条数与库中一致、文件名为「胸部CT检查报告.pdf」 |
| V19、V18（零写入面） | PDF 文件本身校验失败即拒绝整份报告 | Host | `Passed` | 逐项构造不合规文件部件提交检验报告，全部按业务拒绝返回且只描述业务事实：内容类型为 `text/plain` →「PDF 文件的内容类型不是 application/pdf」；扩展名为 `.txt` →「PDF 文件的扩展名不是 .pdf」；文件头非 `%PDF-` →「PDF 文件头签名无效」；零长度 →「PDF 文件不能为空」；文件部件缺失 →「PDF 文件部件不能为空」。五项均返回 `HTTP 500` 业务拒绝，且只读回读确认这些报告单号（`-CT`、`-EXT`、`-SIG`、`-EMPTY`、`-NONAME`）在库中**零行**，即校验失败不进入业务写入、不产生任何业务数据 |
| V19b | 文件名缺失或不安全不拒绝报告 | Host | `Passed` | 以含路径分隔符与控制字符的文件名（`bad/na\rme\n.pdf`）提交，返回 `HTTP 200` 与 `true`（不拒绝报告）；回读该版本 `pdf_file_name` 为 `MR-FILE2-20260918-FALLBACK.pdf`，即回退为「报告单号加扩展名」。对照：安全文件名 `正常报告二.pdf` 按原名保存。修复前的同一场景曾保存为百分号编码形态 `na%0Dme%0A.pdf`（见修复缺陷九） |
| V4、V13、V14、V15 | 作废语义与边界 | Domain/Integration | `Passed` | 同一报告单号提交两次形成两个版本（接受）后：① 作废时间取「当天 01:20」而当前版本平台接收时间为「当天 02:22:39」时，作废被拒绝并返回「业务拒绝：作废时间不得早于当前版本的平台接收时间」（V14 的下界分支）；② 作废时间取「当天 02:22:00」仍早于平台接收时间，同样被拒绝（下界分支二次命中）；③ 作废时间取请求时刻「当天 02:22:55」时首次作废返回 `HTTP 200` 与 `true`，只读回读报告主体 `status=2`、`voided_time=2026-09-18 02:22:55`、`void_reason` 与提交值一致，且版本行数仍为 2、当前版本指向未变（V13：不生成内容版本、不物理删除）；④ 以相同作废时间与相同原因再次作废返回 `HTTP 200` 与 `true`（V15 幂等分支）；⑤ 以相同作废时间与不同原因再次作废返回 `HTTP 500`「业务拒绝：报告作废信息与已保存的作废事实不一致」且未覆盖首次作废事实（V15 冲突分支）；⑥ 用同一业务标识再次提交完整报告返回 `HTTP 500`「业务拒绝：报告已作废，不能再次提交。」（V4） |
| V5-V8、V27、V28、V42、V87 | 提交校验拒绝语义（真实入口） | Contract/Domain | `Passed` | 在宿主登录会话内逐条构造非法请求，全部按业务拒绝返回且措辞只描述业务事实：重复来源明细标识→「同一报告版本内普通检验结果来源明细标识重复」；重复展示序号→「同一报告版本内普通检验结果展示序号重复」；展示序号为零值→「展示序号必须是正整数」；检测人单边提供→「检测人的标识与名称必须成对提供」；就诊类型未知值（99）→「就诊类型不是已定义的取值」；申请时间晚于报告时间→「申请时间不得晚于报告时间」；证件已存在但患者姓名不一致→「证件已存在但患者身份信息不一致」。前六次尝试在库中零残留（只读回读为零），第七次（患者身份冲突）在拒绝的同时留下报告残留，该残留单列在「事务边界」行与本报告「本轮验收数据的残留与处置」节 |
| 事务边界（S4-D34 的机械检查点） | 在报告写入之后失败时的原子性 | Integration | `Passed` | 三次独立实验：① 修复前，在报告插入之后失败的检验报告提交（`ResolvePlatformPatientAsync` 抛出「证件已存在但患者身份信息不一致」）返回 `HTTP 500`，只读回读得到**一行报告主体**（`patient_id` 与 `current_version_id` 为空 Guid、患者姓名为空、无版本行），报告单号 `MR-ACC-EDGE-20260918-IDENT`、`MR-TXPROBE-IDENT-20260918-001` 两次复现；② 定位根因后（见「修复缺陷八」），同一失败场景以报告单号 `MR-TXPROBE2-IDENT-20260918-001` 复测，只读回读**零行**；③ 最终修复后同时复测两条路径：成功提交（`MR-TXPROBE3-OK-20260918-001`）落库 1 份报告 + 1 个版本 + 1 个平台患者，失败提交（`MR-TXPROBE3-FAIL-20260918-001`）**零残留**。结论：提交在写入之后失败时整次提交回滚，与设计要求「任一步失败整次提交不保存任何内容」一致 |
| 框架事务机制的静态事实 | 事务声明的承载位置 | Static | `Passed`（机制取证） | 反射与 IL 核实：`WorkUnitAttribute` 位于 `Dy.Core.Abstractions.Http`，具有 `UseTransaction`、`IsolationLevel`、`TransactionType`、`Timeout`、`IsDisabled` 与 `TransactionInfo`；`WorkUnitEndpointFilter.SetHttpContextInfo` 调用 `Dy.Apron.Http.ActionUnit.Extensions.GetWorkUnitAttribute(RouteEndpoint)` 取事务声明，再读取 `WorkUnitAttribute.TransactionInfo`、`TransactionInfo.UseTransaction` 并写入 `HttpContextInfo.TransactionInfo`；`SetCacheContext` 读取 `Endpoint.Metadata`。事务的开与回滚因此由**路由端点元数据**决定，即声明必须落在端点方法上。该机制是缺陷八的根因，也是「控制器上传动作必须声明事务」这一口径的依据 |

## 已确认的验证降级

按阶段根 [design.md](design.md) 的既定口径登记，执行时按此处理、不改变结论口径：

- **跨组织真实宿主面**：平台管理员入口的跨组织数据在本环境不可构造，按既有降级口径以应用层证据为准并登记 `AcceptedRisk`，真实宿主面记 `Blocked`；不因此改变取值来源口径。
- **历史版本文件保留**：本阶段不做文件清理或归档，作为 `AcceptedRisk` 登记，重审条件为出现可复现的磁盘压力或部署目录配额问题。
- **孤儿文件**：写库失败后的文件补偿删除失败只记日志、不改变对外结果，残留面作为 `AcceptedRisk` 登记，重审条件为出现可复现的存储目录膨胀或运维需要人工核对文件与记录一致性。
- **框架行为**：工作单元事务与事件可见性不设用例；本阶段没有事件消费者与专用观察点，事件可见性与事务回滚观察面按阶段根台账的已接受风险登记，记 `N/A` 并说明框架边界，重审条件为项目获得可控事件消费者、事件持久化记录或专用观察点。
- **接口直测的认证前置**：本环境取接口直测所需的宿主令牌，需经宿主登录页（`http://183.224.180.166:35000/login`）在浏览器中完成登录并从宿主认证存储取得令牌；同轮尝试直接调用宿主登录端点 `POST http://183.224.180.166:35001/Api/Auth/Login`（请求体只含账号与密码）返回 `HTTP 500`，该端点的 `subApplicationId` 取值在本环境未登记，因此不以该路径作为取证手段，也不写入或复制任何令牌原文。该前置在本轮已可通过宿主登录页满足，依赖真实令牌的条目（V1-V79、V89-V91 的真实入口面与 C28-C31）中，C28、C29 与 C30、C31 均已取得真实宿主证据，其余见上方结果表。宿主与本机时钟存在偏差，实测宿主签发的令牌 `nbf` 比本机时钟**晚约 16 秒**（2026-09-19 两轮登录分别实测 16 秒与 16 秒），刚签发的令牌会因 `nbf` 未到被后端拒绝，响应头为 `www-authenticate: Bearer error="invalid_token", error_description="The token is not valid before '…'"`；等待约 20 秒后重试即可，不得据此判定鉴权失败。**该偏差的前端表现需要单独留意**：前端把业务请求的 401 当作会话失效并跳转登录页（Console 记录 `[POST 401] … 未授权，跳转登录页`），因此登录后立即进入业务页会被打回登录页，看起来像登录失败。此时宿主认证存储中的会话仍然有效，等待令牌进入生效窗口后重新导航到宿主根地址即可恢复，不需要重复登录，也不需要重新取得令牌。

## 本轮验收数据的残留与处置

- 验收数据经正常业务接口形成，按阶段口径保留不清理：验收报告 4 份（检验 1 份两版本、检查 1 份、作废链路 1 份两版本）、平台患者 3 个、存储目录 `var/report-pdf/20260918/` 下对应 PDF 文件。
- **待处置残留（两行）**：`MR-ACC-EDGE-20260918-IDENT` 与 `MR-TXPROBE-IDENT-20260918-001` 两行报告主体，特征一致（生命周期状态 1、`patient_id` 与 `current_version_id` 为空 Guid、患者姓名为空、无版本行）。这两行是缺陷八（提交事务未开启）修复前产生的历史残留；修复后的同场景复测（`MR-TXPROBE2-IDENT-20260918-001` 与 `MR-TXPROBE3-FAIL-20260918-001`）均为零残留，说明该形态不再产生。两行不属正常业务路径形成的完整数据，建议由负责人在开发库中按单行删除清理；本轮未对其执行任何写操作，也不以直接写库清理它们。
- 该残留不影响任何已登记条目的结论：它以空患者姓名与无版本行为特征，列表按来源归属返回它时以 `currentVersionSequence=0` 正常展示（缺陷六修复后），清理后不影响其余验收数据的可复现性。

## 部署前置（已登记）
- `ReportPdfFiles:RootDirectory` 必须在部署配置中提供可写的完整绝对路径：受版本控制的 `appsettings.json` 只放空根目录与默认上限（83886080 字节），宿主在启动校验处直接失败，不隐式回落。本环境以 `Production` 环境启动的实测结果见结果表 V82。
- **部署侧网关的请求体上限须不小于宿主请求体上限。** 医院侧提交的 multipart 请求在到达宿主之前可能先经过部署侧网关（反向代理），网关自身的请求体上限若小于宿主上限会让请求在网关被挡下，既到不了宿主，也就触发不了控制器或框架层的超限拒绝。本环境实测的宿主请求体上限约在 104857600 字节量级（框架程序集 `Dy.Apron.dll` 的 `ConfigureKestrel` 设置，应用源码内无配置键，宿主侧无法调整），因此部署侧网关的上限须不小于该取值。该值由部署配置提供，本仓库不承载部署形态，**不属本阶段交付物，也不作为矩阵取证条目**。
- 单文件上限（83886080 字节）必须低于宿主请求体上限，使控制器的超限拒绝分支可达（取证见结果表 V20 与 V88 两行）。

## 验收请求样例
两个提交接口的验收请求样例按 SRS F03.2/F03.3 与 UML1 的请求值对象逐字段构造，与设计对「验收数据只经正常业务接口或页面形成」的要求一致：

- [请求样例-检验报告.md](阶段4-Tickets/请求样例-检验报告.md)：覆盖普通结果两条（含互认项目编码与检测人的一条、两字段为空的一条）、细菌鉴定结果两条（检出具体菌种并挂两条药敏结果的一条、未检出且药敏集合为空的一条）、应填字段部分提供部分留空。
- [请求样例-检查报告.md](阶段4-Tickets/请求样例-检查报告.md)：覆盖来源影像状态为有影像且提供调阅地址、检查项目两条（一条挂两个部位、一条部位集合为空）、检查医生与报告医生为同一人。

两份样例的 JSON 均经解析校验通过。样例中的报告单号与证件号码为本阶段专用，用于区分验收数据与既有数据。

## 矩阵条目状态清单

本清单按编号给出后端矩阵 93 条（V1-V88 主编号含 V19b、V80b 子编号，另含 V89、V90、V91）每一条的证据状态，取代此前只写「其余未执行条目记 `NotRun`」而无法核对的表述。分类口径：**自动化证据**指测试工程内有回挂到该编号的用例；**真实入口证据**指结果表内按真实宿主、真实库或真实文件系统取得并逐条列出；**环境不可构造**指受本环境条件限制无法取证；**设计记 `N/A`** 指该条已按设计变更不再适用。

### 有真实入口证据（结果表逐条列出）

V1、V2、V4、V5、V6、V7、V8、V9、V13、V14、V15、V17、V18、V19、V19b、V20、V27、V28、V37、V39、V40、V42、V45、V46、V47、V48、V49、V50、V51、V52、V53、V54、V55、V57、V59、V60、V61、V62、V63、V69、V70、V71、V72、V73、V74、V75、V76、V79、V80、V80b、V82、V83、V84、V87、V89、V90、V91。其中 V63 与 V84 为缺陷修复后复测，V20 与 V88 为超限边界的真实入口探测；V85 与 V86 的并发实测证据已因设计变更作废，已移出本类并改记 `PendingRetest`。

### 有自动化证据覆盖（应用、领域、契约、查询映射与静态层级）

V1、V2、V3、V4、V5、V6、V7、V8、V9、V10、V11、V12、V13、V14、V15、V16、V19b、V21、V22、V23、V24、V25、V26、V27、V28、V29、V30、V31、V32、V33、V34、V35、V36、V38、V41、V42、V43、V44、V46、V47、V48、V49、V50、V51、V52、V53、V54、V55、V58、V60、V61、V62、V63、V64、V65、V66、V67、V68、V69、V70、V71、V72、V73、V74、V77、V78、V80、V81、V83、V84、V87、V89、V90。V85 与 V86 的并发用例已按设计变更删除，不再属于本类。用例位于 `Stage4SqlMapTests.cs`、`Stage4EndpointTests.cs`、`Stage4ConstraintTests.cs`、`Stage4ContractTests.cs`、`Stage4WritePathTests.cs`、`Stage4QueryTests.cs`、`Stage4SubmissionPathTests.cs`；编号回挂情况可 grep 测试工程核对。

### 无自动化回挂且无真实入口证据

**本类已清空。** 此前列入本类的 12 条已补取证，逐条见下表；补取证后 `dotnet test` 由 423 通过增至 440 通过、0 失败。

| 编号 | 用例 | 补充的用例与判据 |
|---|---|---|
| V34 | 药敏归属字段不可为空 | `Stage4WritePathTests.Susceptibility_records_are_always_attributable`：每条已保存药敏的归属字段为非空 Guid，且能在已保存的细菌鉴定结果中找到归属；归属按所属细菌分组，不存在跨细菌或未归属记录 |
| V38 | 检验报告作废后停止追加 | `Stage4WritePathTests.Voided_laboratory_report_stops_appending_versions`：作废后再次提交按报告已作废拒绝，版本行数、明细行数与当前版本指向均不改变 |
| V41 | 检验明细来源字段原文保存 | `Stage4WritePathTests.Laboratory_result_source_fields_are_saved_verbatim`：单位、参考范围、检测方法、仪器、收费编码、异常与危急值标志逐字段与请求取值相等（含特殊字符） |
| V43 | 异常标志与危急值标志取值 | `Stage4WritePathTests.Abnormal_flag_accepts_the_four_defined_values`（四值逐一可保存）与 `Missing_critical_value_flag_is_accepted`（缺失危急值标志仍接收并按缺失保存） |
| V48 | 检查项目必填字段 | `Stage4WritePathTests.Examination_item_requires_source_project_name_but_not_standard_code`：名称缺失即拒绝且零写入；互认项目编码缺失仍接收并按缺失保存 |
| V50 | 检查部位归属项目 | `Stage4WritePathTests.Examination_sites_belong_to_their_own_item_only`：两个项目的部位分别归属各自项目，不存在跨项目归属 |
| V53 | 检查报告报告级内容按原文保存 | `Stage4WritePathTests.Examination_report_level_content_is_saved_verbatim`：检查所见（长文本）、检查结论、病情描述、检查目的、检查方法与设备信息逐字相等 |
| V54 | 检查报告来源检查类型与诊断 | `Stage4WritePathTests.Examination_source_type_and_diagnosis_are_saved_verbatim_or_null`：提供时逐字保存；缺失时仍接收并按缺失保存 |
| V58 | 检查报告作废后停止追加 | `Stage4WritePathTests.Voided_examination_report_stops_appending_versions`：与 V38 同一口径，落在检查链路 |
| V67 | 列表排序稳定 | `Stage4SqlMapTests.V67_list_sort_is_stable_and_total`：排序键为报告时间倒序加主键倒序、无空值排序分支，报告时间列为非空使排序成为全序，附变异证据 |
| V77 | 下载文件缺失 | `Stage4SubmissionPathTests.Download_with_missing_file_is_rejected_without_returning_content`：归属与版本定位成立、仅文件缺失时返回「业务拒绝：报告 PDF 文件不存在。」，不返回文件内容 |
| V78 | 已作废报告的历史版本下载 | `Stage4SubmissionPathTests.Download_of_voided_report_version_is_still_allowed`：下载路径不按生命周期状态拦截，作废后历史版本的文件流与下载名仍可取得 |

### 设计记 `N/A`

V56「检查项目与部位可选人员成对规则」：检查项目与检查部位两级在设计的字段表、提交输入类型与 SRS F03.3 属性表中均未定义人员字段，不存在可构造的单边提供请求。

### 环境不可构造

**本类已清空。** 此前列入本类的 3 项按下列口径处置完毕。

| 编号 | 用例 | 处置 |
|---|---|---|
| V88（部署侧部分） | 宿主请求体上限与超限失败语义 | 部署侧网关的请求体上限属部署形态、由部署配置提供，不属本阶段交付物，已改为「部署前置」一节的部署说明，不再作为矩阵取证条目 |
| V69（真实外部服务不可用面） | 名称回填调用次数不随行数增长 | 该面取消：参照实现的同一失败路径只以「查询时失败即向上抛出」的方式处理，不设特殊降级，并以抛出异常的替身覆盖；本项目的等价用例已有，见「有自动化证据覆盖」一节 |
| V88（宿主侧失败形态） | 同上 | 按实测形态定稿：框架层请求体超限表现为中止连接，不再作为待重新取证项 |

### 前端矩阵状态

前端矩阵 31 条（C1-C31，定义见 [Client/testPlan.md](Client/testPlan.md)）的状态。本阶段全部条目均有证据，无缺口。

| 分类 | 编号 | 证据形态 |
|---|---|---|
| 契约与生成 | C1、C2、C3 | 生成命令实跑结果、生成产物与锁定文件核对、包与应用构建与类型检查 |
| 适配层 | C4、C5、C6、C8、C10 | `reportsApi.test.ts` 的适配层用例（请求映射、分页收敛、读模型映射、下载处理、过期响应丢弃） |
| 分页控件 | C7 | `src/shared/tablePagination.test.ts`（4 项）与 `ReportManagement.test.tsx` 的页容量取值域断言 |
| 范围切换重置 | C9 | `ReportManagement.test.tsx` 的范围切换用例 |
| 平台管理员页组件面 | C11-C22 | `ReportManagement.test.tsx`（23 项） |
| 医院管理员页组件面 | C23、C24、C25、C26 | `BranchReportManagement.test.tsx`（7 项）；本类此前为交付缺口，已在本轮补取证 |
| 路由接入 | C27 | 宿主菜单进入实测与 `routes.tsx` 两条路由 |
| 宿主真实只读验收 | C28、C29 | 宿主菜单进入两个页面的实测（结果表 C28、C29 行） |
| 宿主失败态与跨院区下载拒绝 | C30、C31 | 宿主页面失败注入实测与跨院区下载的真实服务端拒绝；本轮已按当前代码复测 |

## 结论

已完成静态、契约、接口、宿主页面与前端构建层的真实取证：

- 静态与契约：DDL 静态面与目标库结构面（V80、V80b）、配置快速失败面（V82）、端点清单与路由面（V83）、契约与生成面（C1-C3）。
- 后端行为：`dotnet test` 失败 0、通过 440，覆盖 V1-V16、V19b、V21-V36、V38、V41-V44、V46-V55、V58、V60-V67、V68-V74、V77、V78、V80、V81、V83 的静态与入口形态面、V87、V89、V90 的契约/领域/应用/查询映射层级。逐条状态见「矩阵条目状态清单」节。
- 前端：类型检查、构建、lint 与 270 项测试全部通过；两条路由已接入。前端矩阵 31 条的状态见「矩阵条目状态清单」节的前端矩阵表。
- 接口与宿主：四个医院接入能力的检验侧与检查侧完整链路、作废语义与时间边界、管理端列表与版本查询、下载与三态拒绝、文件补偿均在真实库、真实文件系统与宿主令牌下取得证据；宿主入口、菜单授权、资源拦截与两个页面（平台管理员页、医院管理员页的可信范围固定面）已从宿主菜单进入并渲染。本轮另取得宿主页面四类失败态（C30）、下载跨院区归属失败的页面表现（C31）与宿主请求体上限的宿主侧证据（V88）。

执行过程中发现并修复了十一个真实缺陷，其中第一个会直接使本阶段全部运行验证不可用，第二个使查询链路整体不可用，第三个使真实提交在可信院区处被拒，第六个使管理端列表整页读取失败，第八个使提交在写入之后失败不回滚，第十个使并发撞证件键向外泄漏数据库驱动异常，第十一个使版本内容读取在审核时间为空时中断。

**本轮收尾另发现两个缺陷，均已在修改代码后复测通过**：缺陷十二使分页请求的形状与设计不符（V84 现记 `Passed`），缺陷十三使列表时间范围筛选的上界取到结束日 08:00（V63 现记 `Passed`）。两条的失败用例、修改内容与真实入口复测证据见「缺陷十二与缺陷十三的修复与复测」节。另有一处配置取值改动（单文件上限由 104857600 改为 83886080 字节）见「设计变更登记」节。本报告已无未修复缺陷。

尚未取得证据或已登记为受阻的条目见「矩阵条目状态清单」节：该节按编号给出每一条的证据状态，「无自动化回挂且无真实入口证据」与「环境不可构造」两类均已清空；V56 按设计变更记 `N/A`。V88 宿主侧的框架层超限失败形态已按实测登记为中止连接（此前的 `HTTP 400` 记录未能复现，见 V88 行）。部署侧网关的请求体上限见「部署前置」一节，属部署形态、不属本阶段交付物。两处清单的记账更正见「后端构建与既有测试」节的说明。本报告不声明阶段完成。

## 阶段 7 补记：患者原值口径的受影响证据（2026-09-22）

本节按阶段 7 设计 S7-D8（患者信息按业务原值展示，不实现任何脱敏；出处 `docs/plans/009-阶段7-横向治理与发布收口/design.md` 关键决策表）与设计 §4「testReport 补记证据与残余」追加，仅追加，不改写上文历史证据。

- 受影响实现：版本详情投影的电话掩码处理已移除，联系电话改由既有空值归一（`NullIfBlank`）承载；`Stage4QueryTests` / `Stage4ContractTests` 断言与前端 `ReportManagement.test.tsx`、`reportsApi.test.ts` 期望已改到原值口径。自动化证据：目标过滤 49/49 通过、后端全量 781 项中 780 通过（1 项失败为阶段 7 票01 引入的既有登记缺口，与本阶段用例无关）、前端 354 项通过（352 通过、2 跳过），均登记于阶段 7 根 `testReport.md`「票03」节。
- 真实链路重取证据（2026-09-22，宿主页面与 `localhost:15014` 接口）：报告列表患者姓名、证件号码为业务原值（如 `林来源` / `530101198601012202`、`验收患者甲` / `530101199005120011`）；报告版本详情患者联系电话为业务原值（`MR-ACC-LAB-20260918-001` 第 1、2 版均为 `13800001111`，`MR-ST5-09-CROSS-CSYY2-001` 为 `13800009999`），与 `mrec_medical_report_version.patient_phone_number` 逐字一致；`QueryMedicalReportVersionDetail` 返回 `content.common.patientPhoneNumber` 无掩码。完整取证环境与逐条比对见阶段 7 根 `testReport.md`「票03」节。
- 历史证据说明：本报告上文的 `*******1111`、`*******1234`/`*******3333` 等记录形成于掩码实现时期，按 Planning And Evidence §4 不得改写已经形成的历史证据，保持原样；现行口径以 S7-D8 与 `docs/需求规约SRS.md`「通用界面要求」第 2、3 条为准。
