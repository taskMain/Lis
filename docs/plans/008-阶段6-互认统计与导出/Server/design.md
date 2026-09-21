# 阶段 6 后端设计

总体范围与关键决策见 [阶段设计](../design.md)。本阶段全部能力为只读查询与一个文件生成导出，无写用例、无命令、无事务、无领域事件。

## 领域与契约

### 层映射

| 概念层 | 实际项目/目录 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | `Dy.MedicalRecognition.Application.Contracts`（`Queries/`、`ReadModels/`） | `Domain.Share` | Application、Domain、Repository |
| Application | `Dy.MedicalRecognition.Application`（`Queries/`） | Contracts、Domain（含 `Ports/` 写侧仓储接口）、平台 NuGet 契约 | Repository 实现、SQL、DataMapper |
| Domain | `Dy.MedicalRecognition.Domain`（聚合 `MedicalRecognitionReportAggregate` 与 `Ports/`） | `Domain.Share` | HTTP、Application、数据库实现 |
| Domain.Shared | `Dy.MedicalRecognition.Domain.Share`（Enums） | 无 | 其余全部 |
| Repository/Infrastructure | `Dy.MedicalRecognition.Repository`（`Queries/`、`MedicalRecognitionReportAggregate/`、`Scripts/`） | Domain、Application 查询端口 | 领域规则决策 |
| Host | `Dy.MedicalRecognition`（`Controllers/`） | Application、Contracts | 领域规则、SQL |

### 功能与聚合判断

统计与导出是对唯一聚合 `MedicalRecognitionReportAggregate` 既有事实（互认匹配记录、匹配项、处理结果、引用事实）的只读投影， plus 报告主体（来源三值、患者检索列）与标准目录（分类、分组、标准项目名称）的关联读取；不新增实体、不形成新的聚合边界。**必须保持不变的不变量**：查询不产生任何写入、事件或状态变化；统计口径只读既有不可更改事实，任何访问不改变采纳、引用与金额事实。

本阶段无 Command/FacadeCommand，Command-Event 评审表不适用；无写用例，事务表为空——按项目规范逐条说明：四个查询与导出均为只读入口，应用服务与查询仓储不声明事务（沿用阶段 5 `V88` 对引用详情链的零事务静态判据口径，本阶段将其推广到全部统计链）；并发只读无补偿面；幂等、唯一性无业务写入面。

### 类型归属表

| 类型名 | 角色 | 所在层/目录 | 调用方 | 主要职责 | 禁止职责 |
|---|---|---|---|---|---|
| `RecognitionUsageSummaryQueryRequest` / `BranchRecognitionUsageSummaryQueryRequest` | Request | Contracts/Queries | API/页面 | 接收侧汇总查询条件（平台版含接收组与来源组两组筛选；本院版接收组注入、来源组医院/院区可选）与汇总维度、内嵌分页对象 | 可信用户字段、统计计算 |
| `RecognitionUsageDetailsQueryRequest` / `BranchRecognitionUsageDetailsQueryRequest` | Request | Contracts/Queries | API/页面 | 接收侧明细查询条件（平台版含接收组与来源组两组筛选；本院版接收组注入、来源组医院/院区可选）与明细类型、内嵌分页对象 | 同上 |
| `SourceRecognitionSummaryQueryRequest` / `BranchSourceRecognitionSummaryQueryRequest` | Request | Contracts/Queries | API/页面 | 来源侧汇总查询条件（平台版含来源组与接收组两组筛选；本院版来源组组织/医院固定、接收组医院/院区可选）与汇总维度、内嵌分页对象 | 同上 |
| `SourceRecognitionDetailsQueryRequest` / `BranchSourceRecognitionDetailsQueryRequest` | Request | Contracts/Queries | API/页面 | 来源侧明细查询条件（平台版含来源组与接收组两组筛选；本院版来源组组织/医院固定、接收组医院/院区可选）、内嵌分页对象 | 同上 |
| `RecognitionStatisticsExportRequest` / `BranchRecognitionStatisticsExportRequest` | Request | Contracts/Queries | API/页面 | 导出类型、汇总维度（必填）与全部统计查询条件（含可选不采纳原因代码），不带分页对象 | 分页限制导出范围 |
| `RecognitionMatchRecordQueryRequest` | Request | Contracts/Queries | API/页面 | 互认匹配记录标识 | 其他定位条件 |
| `RecognitionUsageSummaryReadModel`、`RecognitionUsageDetailReadModel`、`SourceRecognitionSummaryReadModel`、`SourceRecognitionDetailReadModel`、`StatisticsExportFileReadModel`、`RecognitionMatchRecordReadModel` 及嵌套子模型 `SourceOrganizationReadModel`、`ReceiverOrganizationReadModel`、`RecognitionStatisticsItemReadModel`、`RecognitionProcessingResultDetailReadModel`、`RecognitionReferenceDetailReadModel`、`NonAdoptionReasonSummaryReadModel`、`RecognitionMatchRecordItemReadModel` | ReadModel | Contracts/ReadModels | API/页面 | 统计、明细、导出文件与匹配记录视图的只读投影；枚举值 + `XxxText` 计算属性；患者姓名与证件号码结构化字段 | 展示以外的职责、公开可写属性、无类型 `object` 属性 |
| `RecognitionStatisticsGroupDimensionEnum` | Enum | Domain.Share/Enums | 请求/读模型 | 汇总维度四值（医院、院区、互认科室、标准项目） | 来源侧语义扩展 |
| `RecognitionUsageDetailTypeEnum` | Enum | Domain.Share/Enums | 请求/读模型 | 接收侧明细类型四值（提醒、采纳、不采纳、引用） | 跨侧复用 |
| `RecognitionStatisticsExportTypeEnum` | Enum | Domain.Share/Enums（已存在） | 请求 | 导出类型七值 | 修改（仅补描述器登记） |
| `IMedicalRecognitionReportQueryAppService.Statistics.cs` | 查询契约分片 | Contracts/Queries | Host 自动端点 | 四个查询、导出、匹配记录视图共 10 个方法声明（宿主 11 个端点，导出由控制器平台/本院两个 POST 动作承载） | 写操作 |
| `MedicalRecognitionReportQueryAppService.Statistics.cs` | QueryAppService 分片 | Application/Queries | 自动端点、导出控制器 | 范围解析、窗口校验、语句编排、读模型组装、率与占比计算、导出生成编排 | SQL、DataMapper |
| `IMedicalRecognitionReportQueryRepository`（Statistics 分片） | 查询端口分片 | Application/Queries | QueryAppService | 统计语句的端口声明 | 语句实现 |
| `MedicalRecognitionReportQueryRepository.Statistics.cs` | QueryRepository 分片 | Repository/Queries | 查询端口 | DataMapper 调用、集合参数与分页窗口传参 | 领域规则、聚合计算 |
| `MedicalRecognitionReportQuery.xml`（追加统计语句） | SqlMap | Repository/Queries | QueryRepository | 统计 SQL 语句（同一查询作用域 `MedicalRecognitionReportQuery`） | 方言分页、写语句 |
| `RecognitionStatisticsExportController` | Controller | Host/Controllers | 浏览器 | 两个导出动作的 POST 端点，返回 `File` 流 | 业务规则、SQL |
| `Scripts/Migrations/<日期>_s6_statistics_indexes.sql` | 迁移脚本 | Repository/Scripts/Migrations | 负责人执行 | 三个统计索引的增量 DDL | 表结构其他变更 |

### 调用链与字段来源

```text
Request（Contracts，平台版/本院版）
-> RecognitionStatisticsQueryAppService.Statistics（范围解析 -> 窗口校验 -> 语句编排 -> 组装）
    -> TrustedScopeResolver / OrganizationPathResolver（本院注入与名称回填，批量）
    -> IMedicalRecognitionReportQueryRepository（Statistics 分片）
    -> MedicalRecognitionReportQueryRepository.Statistics
    -> DataMapper（scope: MedicalRecognitionReportQuery，显式 sqlId，分页窗口交 Provider 适配层）
-> ReadModel / PageResultDto<T>
```

导出链：`ExportRequest -> QueryAppService.GetStatisticsExportAsync（复用统计语句计数与全量读取 -> 行数上限校验 -> ClosedXML 组装） -> StatisticsExportFileReadModel（FileName/FileFormat/FileStream〔字节数组〕）-> RecognitionStatisticsExportController -> File(stream, xlsx, fileName)`。控制器两个 POST 动作各自绑定平台/本院请求类型并调用同一应用层导出入口，本院注入发生在应用层（与双入口查询惯例一致）。

关键字段来源：组织、医院、院区——平台版取自请求并经组织路径解析校验存在、启用与父子归属；请求级空取值不附加该层过滤（全空即平台按授权全量，见 S6-D18）。本院版取自可信上下文注入；本侧院区可选——为空时不进入组织路径解析的院区层（应用层构造仅组织与医院两层的解析目标，范围按可信组织与可信医院收敛，SQL 不附加该院区条件），非空时经组织路径解析校验院区存在、启用且属于可信医院；来源侧本院版来源医院固定为可信医院。院区为空的分支在应用层处理，不调用三层整体解析。日期范围与全部筛选条件取自请求；组织、医院、院区名称由组织路径解析点按当页涉及的编码批量回填，调用次数不随返回行数增长；六项指标、金额与原因次数由 SQL 聚合产出，率与占比由应用层按行内次数计算。接收侧入口的『组织/医院/院区』为接收范围、『来源*』为可选筛选；来源侧入口反之。平台版请求的来源组与接收组组织编码不一致时按业务拒绝；本院版由注入保证同组织。患者姓名经匹配项的报告标识关联报告主体检索列取得（一对一，取自匹配项绑定的报告），证件号码取匹配记录保存值。

### 枚举协作

汇总维度、明细类型两个新枚举补 `[EnumDescriptor]` 与逐成员中文 `[Description]`，连同导出类型枚举登记描述器注册表，同批更新 `Stage2EnumContractTests` 的 `ExpectedEnumNames` 清单并补三个枚举的取值、成员名与中文说明冻结断言；既有枚举元数据守卫用例的排除清单已为空，不再承载删除动作。读模型实际承载的项目类型、就诊类型、处理结果与明细相关状态同时返回枚举值与文本计算属性（承接阶段 5 `V85` 交接面）。

## 数据与集成

### 统计语句清单

全部语句注册在既有查询作用域 `MedicalRecognitionReportQuery` 下，方法名去掉 `Async` 后缀即语句标识；语句集合受既有语句清单断言与跨文件列一致性守卫约束（本阶段新语句同批登记）。

| 语句标识 | 用途 |
|---|---|
| `CountRecognitionUsageSummaryGroups` / `QueryRecognitionUsageSummaryPage` / `QueryRecognitionUsageSummaryReasons` | 接收侧汇总：分组行计数、当页分组行（含六项指标与金额的条件聚合）、当页分组的不采纳原因汇总 |
| `CountSourceRecognitionSummaryGroups` / `QuerySourceRecognitionSummaryPage` | 来源侧汇总：分组行计数、当页分组行（被互认次数） |
| `CountRecognitionUsageReminderDetails` / `QueryRecognitionUsageReminderDetails` | 接收侧提醒明细（含未反馈项） |
| `CountRecognitionUsageAdoptionDetails` / `QueryRecognitionUsageAdoptionDetails` | 接收侧采纳明细（随金额） |
| `CountRecognitionUsageNonAdoptionDetails` / `QueryRecognitionUsageNonAdoptionDetails` | 接收侧不采纳明细（随原因与补充说明） |
| `CountRecognitionUsageReferenceDetails` / `QueryRecognitionUsageReferenceDetails` | 接收侧引用明细 |
| `CountSourceRecognitionDetails` / `QuerySourceRecognitionDetails` | 来源侧被互认明细（仅采纳事实） |
| `QueryRecognitionMatchRecordView` / `QueryRecognitionMatchRecordViewItems` | 匹配记录集合视图：组级信息与全部匹配项 |

### SQL 聚合口径

- **关联**：接收侧以匹配记录为范围主体左联匹配项；采纳/不采纳/金额与引用事实经匹配项标识一一关联（处理结果、引用事实与匹配项均一对一，连接不放大行数）；项目类型、分类、分组与标准项目名称经标准项目编码关联标准目录三表；患者姓名经匹配项的报告标识一对一关联报告主体检索列取得（报告主体的患者姓名、证件号码冗余列恒等于当前版本；名称仅作展示，报告后续作废或更正不回写已形成的匹配事实；取报告主体当前检索列而非匹配项绑定版本的理由：报告主体冗余列即平台权威患者展示口径，姓名仅作展示且证件号码不变，不回写已形成的匹配事实；重审条件为负责人要求按绑定版本展示患者姓名）。
- **条件聚合**：分组行内提醒次数按匹配记录的互认匹配生成时间、采纳与不采纳次数与金额按处理结果的互认时间、引用次数按引用事实的实际引用时间各自独立过滤同一日期范围，用标准 `CASE` 条件聚合在分组行内一次成型；日期闭区间按本地日解释，参数化传值；接收侧按来源组筛选、来源侧按接收组筛选均可选生效；范围条件为空的层不进入筛选，计数语句与数据语句仍共用同一套非空条件。
- **分组键**：接收侧医院维度取匹配记录接收医院、院区维度取接收院区、互认科室维度取「接收院区 + 处理结果互认科室 ID」、标准项目维度取匹配项标准项目编码（分类、分组、类型名称随行回填）；来源侧维度取报告主体来源医院、来源院区或匹配项标准项目编码。未参与分组的维度字段不在投影中出现，应用层置 `null`。未反馈匹配项无处理结果、无互认科室归属，不计入互认科室维度的提醒次数与同期互认率。
- **排序与分页**：计数语句不带排序与窗口、与数据语句同筛选；数据语句排序稳定唯一（分组键升序，加主键或业务时间兜底）；窗口由 `DataMapper` 分页参数交 Provider 适配层，XML 不写方言分页。汇总分页作用于聚合后的分组行；导出复用同一批语句不带分页参数读取全量。
- **方言中立**：不使用方言聚合、JSON 运算符、系统表与查询提示；不采纳原因汇总独立成行（原因代码 + 次数），由应用层装配进所属分组行，避免使用方言字符串聚合。

### 导出组装

应用层导出方法先以同筛选条件的计数语句取记录数：零记录抛「当前条件下无可导出数据」；超过 10 万行抛业务拒绝并提示缩小查询条件；未超限再取全量并组装。导出请求按导出类型校验汇总维度子集（来源侧导出类型不接受互认科室维度值）；不采纳原因代码仅作用于接收侧不采纳明细导出。明细导出按匹配项一行展开、组级字段重复，提醒/采纳/不采纳/引用明细带互认匹配记录 ID 与匹配项 ID；未反馈匹配项进入提醒明细，处理结果字段为空并显示「未反馈」；汇总导出的『原样』指列结构与口径同页面汇总行、按当前查询条件重新聚合（非页面数据快照），含不采纳原因次数及占比。Excel 由 ClosedXML 生成单工作表，文件名为「导出类型名称 + 起止日期 + `.xlsx`」；读模型 `StatisticsExportFileReadModel` 保留为应用层内部形态（文件名、文件格式、文件流〔字节数组〕），控制器返回 `File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName)`，响应头禁止缓存，不返回文件键、物理路径或对外地址。导出过程不写入数据库、不登记事件、不留存文件副本；导出不含医院院内项目编码、院内项目名称与项目映射信息。

### 索引迁移

三个统计索引以迁移文件 `Scripts/Migrations/<执行日期>_s6_statistics_indexes.sql` 落地，并**同步回写**到下列三份建表脚本（表结构变更双写规则），DDL 由项目负责人执行：

| 表 | 索引 | 列 |
|---|---|---|
| `mrec_recognition_match_record` | `ix_mrec_recognition_match_record_receiver_time` | 接收组织编码、接收医院编码、接收院区编码、互认匹配生成时间 |
| `mrec_recognition_processing_result` | `ix_mrec_recognition_processing_result_recognition_time` | 互认时间 |
| `mrec_recognition_reference` | `ix_mrec_recognition_reference_referenced_time` | 实际引用时间 |

迁移文件与建表脚本均含索引中文注释；不加 `if not exists`、不改既有列与既有索引；执行前依赖索引性能面的条目不记通过。匹配记录表既有 `ix_mrec_recognition_match_record_business_key` 索引的时间列位于证件列之后，不能支撑按时间范围扫描，本条索引与之互补、不重复。

### 外部调用、失败语义与静态守卫

- 外部调用只有组织路径解析（范围校验与名称回填），位于仓储访问前与组装阶段，失败按既有口径整次失败、按原样抛出；导出无其他外部调用。
- 业务失败直接抛出，不新增异常中间件、过滤器或响应包装；未认证请求由框架认证链路拒绝（宿主面取证）。
- 静态守卫同批项：查询契约与实现新增分片时，既有查询入口方法集合断言与分部文件清单/数量断言同步；语句清单断言与跨文件列一致性守卫登记全部新语句；ClosedXML 在 `Directory.Packages.props` 集中声明并在 Application 工程显式引用；客户端路径扫描（阶段 5 `V86` 口径）登记导出控制器的允许位置；分页契约禁用字段守卫覆盖新请求（无 `HasNext`/`Pagination`/`SkipCount`/`PageCount`）；统计链零写入静态守卫（无事务声明、无事件登记、无写语句、无状态变更调用）随本阶段分片新增。
- 分页窗口校验（`PageQueryWindow.Create`）早于第一次仓储访问；页码小于 1、页容量小于 1 或超过 200 按业务拒绝。

## 验证矩阵

本表只记录测试前设计，实际执行结果进入 [阶段测试报告](../testReport.md)。层级定义沿用既有阶段：`Contract`（请求/读模型/枚举形状）、`Static`（静态守卫与脚本）、`Application`（应用层行为）、`Query`（查询组装与口径）、`Repository`（SQL 与映射）、`Host`（真实宿主链路）、`DB`（真实数据库）。本阶段无写用例，「真实 WorkUnit」列全部为否。请求级空取值不附加该层过滤（全空即平台按授权全量，见 S6-D18）；账号级空组织/医院/院区用例不构造（权限系统限制，正常业务不可达，见 S6-D18）。开发期冗余、孤岛或错误数据不作为缺陷处理。

| 编号 | 用例与边界 | 层级 | 预期结果 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| V1 | 十一个端点与请求字段集合 | Contract | 平台版接收侧请求含接收组与来源组两组组织/医院/院区（均可选），平台版本院侧请求含来源组与接收组两组；本院版请求不含本侧组织与医院字段（接收侧由可信上下文注入接收组织/医院，来源侧固定来源组织/医院），本侧院区可选、为空按可信医院全院范围统计，并分别含来源组（接收侧）或接收组（来源侧）的医院/院区可选筛选；导出请求含导出类型、必填汇总维度与可选不采纳原因代码、不含分页对象；匹配记录视图请求仅含匹配记录标识；分片恰含 8 查询 + 1 导出 + 1 匹配记录视图共 10 个方法（宿主共 11 个端点，导出控制器含平台/本院两个 POST 动作） | 否 | 否 | 无 |
| V2 | 汇总维度枚举值域与来源侧子集 | Contract/Application | 枚举恰四值；来源侧汇总接受来源医院、来源院区、标准项目，互认科室值按业务拒绝；导出请求按导出类型校验维度子集 | 否 | 否 | 无 |
| V3 | 明细类型与导出类型枚举 | Contract | 明细类型恰四值、导出类型恰七值，均带描述器特性与中文说明 | 否 | 否 | 无 |
| V4 | 枚举登记与排除清单同步 | Static | 三个枚举登记描述器注册表；`Stage2EnumContractTests.ExpectedEnumNames` 同批更新并补三个枚举取值、成员名与中文说明冻结断言；既有枚举元数据守卫排除清单保持为空且未放宽其他断言 | 否 | 否 | 无 |
| V5 | 六个顶层统计读模型及其嵌套子模型重建 | Contract | 强类型只读属性、无 `object`；患者姓名字段与证件号码字段结构化承载（无 `MaskedPatientInfo`）；处理结果决策用互认结果枚举承载并提供文本计算属性；不采纳原因汇总为嵌套集合；两个汇总读模型含标准项目名称；接收侧汇总读模型含汇总维度字段 | 否 | 否 | 无 |
| V6 | 分页契约形状 | Contract | 四个明细/汇总查询请求内嵌 `page` 对象（一基页码、页容量 1–200）；响应为 `items` + `page`；禁用字段不出现 | 否 | 否 | 无 |
| V7 | 索引迁移与建表脚本双写 | Static/DB | 迁移文件含三条索引 DDL 与中文注释；三份建表脚本同步含三条索引；无 `if not exists`；既有列与索引未变；并断言三条索引与既有索引的列序差异 | 否 | 否 | 无 |
| V8 | ClosedXML 依赖声明 | Static | 集中包管理新增 ClosedXML 并在 Application 工程显式引用；未引入其他 Excel 库 | 否 | 否 | 无 |
| V9 | 既有断言同步 | Static | 查询入口方法集合、分部文件清单与数量、语句清单、列一致性守卫同批同步且未放宽 | 否 | 否 | 无 |
| V10 | 统计链零写入静态守卫 | Static | 统计与导出链无事务声明、无事件登记符号、无写语句、无状态变更调用 | 否 | 否 | 无 |
| V11 | 窗口校验先于仓储访问 | Application | 页码小于 1、页容量小于 1 或超过 200 业务拒绝且不发生仓储访问；合法窗口放行 | 否 | 否 | 无 |
| V12 | 接收侧平台版范围 | Application | 组织/医院/院区按请求并经组织路径解析校验存在、启用与父子归属；非法组合整次拒绝；来源组与接收组组织编码不一致时业务拒绝；请求级空取值不附加该层过滤（全空即平台按授权全量，见 S6-D18）；账号级空组织/医院/院区用例不构造（权限系统限制，正常业务不可达，见 S6-D18） | 否 | 否 | 组织服务替身 |
| V13 | 接收侧本院版范围注入 | Application | 请求不含本侧组织与医院字段，组织与医院由可信上下文注入；院区可选，为空时按可信医院全院范围统计；来源侧本院版接收医院、院区可选且限可信组织；接收侧本院版的来源组医院、院区筛选限可信组织；院区为空时不进入院区层解析、按可信医院全院收敛 | 否 | 否 | 可信上下文替身、组织服务替身（含院区为空分支） |
| V14 | 汇总分组行结构 | Query | 每行为所选维度一个分组值；未参与分组的维度字段为 `null`；无总计行 | 是 | 否 | 多医院、多科室、多项目事实 |
| V15 | 提醒计数口径 | Query | 提醒次数按互认匹配生成时间统计，等于范围内匹配项数；同一查询重复编码只计一次（承接 V93） | 是 | 否 | 含重复编码与联合报告事实 |
| V16 | 采纳/不采纳/金额时间归属 | Query | 采纳、不采纳与预计节省金额按处理结果互认时间统计；金额等于采纳项金额合计且不另建事实 | 是 | 否 | 跨期采纳事实 |
| V17 | 引用时间归属 | Query | 引用次数按引用事实实际引用时间统计 | 是 | 否 | 跨期引用事实 |
| V18 | 同期互认率计算 | Query | 行内采纳次数除以提醒次数；提醒为零时标记未计算、数值无业务含义；短周期率可超过百分之百按原始展示 | 否 | 否 | 提醒为零分组 |
| V19 | 不采纳原因汇总 | Query | 原因次数与占比嵌入分组行；占比分母为行内不采纳次数，为零不计算；其他原因归入其他代码；补充说明只在明细与导出出现 | 是 | 否 | 多原因事实 |
| V20 | 互认科室归类与展示名称 | Query | 按院区 + 科室 ID 归类不拆分；展示名称取范围内互认时间最晚一条处理结果保存的名称；明细保留各记录自身名称；未反馈项无科室归属、不计入互认科室维度分组 | 是 | 否 | 同 ID 多名称事实 |
| V21 | 日期范围语义 | Query | 必填起止日期按本地日闭区间过滤；同一行各指标按各自业务时间列独立过滤（同组不同时期计数可不同） | 是 | 否 | 跨期多指标事实 |
| V22 | 空结果与分页一致性 | Query | 无命中返回空页成功；计数语句与数据语句同筛选，总数一致；越界页返回空 items 与真实总数 | 否 | 否 | 一般事实（内存替身承载，真实库分页面归 V41/V42） |
| V23 | 汇总排序稳定唯一 | Repository | 排序键稳定唯一，跨页无重复无遗漏；窗口由 Provider 适配层承载，XML 无方言分页 | 是 | 否 | 多分组事实 |
| V24 | 名称批量回填 | Application | 组织、医院、院区名称按当页涉及编码批量解析回填；调用次数不随行数增长 | 否 | 否 | 组织服务计数替身 |
| V25 | 汇总与明细计数一致性 | Query | 同一筛选下汇总行数字与其下钻明细计数相等（提醒/采纳/不采纳/引用分别核对）；跨维度提醒合计不可直接对账的边界成立（未反馈项无科室归属） | 是 | 否 | 混合事实 |
| V26 | 接收侧明细四类型业务时间 | Query | 提醒按匹配生成时间、采纳与不采纳按互认时间、引用按实际引用时间；明细业务时间随行返回 | 是 | 否 | 跨期事实 |
| V27 | 未反馈语义 | Query | 未反馈匹配项只进入提醒明细并显示未反馈；不推断为不采纳；未反馈不计入采纳/引用 | 否 | 否 | 未反馈记录（内存替身承载；真实库面并入 V41） |
| V28 | 明细筛选 | Query | 互认医生、不采纳原因代码与全部维度筛选生效；筛选变化回到第 1 页 | 否 | 否 | 多医生多原因事实 |
| V29 | 明细分页与排序 | Query/Repository | 计数与数据同筛选；排序稳定唯一；空页与越界行为符合公共口径 | 是 | 否 | 超一页明细 |
| V30 | 明细患者与人员字段 | Query | 患者姓名与证件号码按业务原值；互认科室、医生 ID 与名称取各业务记录自身保存值；患者姓名取自匹配项绑定报告的报告主体检索列（一对一）；已作废报告的匹配项仍展示其绑定报告保存的患者姓名 | 是 | 否 | 一般事实 |
| V31 | 来源侧本院锁定 | Application | 本院版来源查询的来源医院恒为可信医院，请求无法越权指定其他来源医院 | 否 | 否 | 双医院身份 |
| V32 | 来源侧口径 | Query | 被互认次数仅计采纳事实、不等待引用、不含金额；维度子集与 S6-D2 一致 | 是 | 否 | 采纳与未反馈事实 |
| V33 | 来源侧汇总与明细分页 | Query | 与接收侧同公共分页口径 | 是 | 否 | 超一页事实 |
| V34 | 导出类型与内容 | Query/Application | 七类导出内容与设计口径一致：明细按匹配项展开、组级字段重复、带匹配记录与匹配项 ID、未反馈显示；汇总导出为页面汇总行原样含原因汇总；导出请求按导出类型校验维度子集；不采纳原因代码收窄不采纳明细导出生效 | 是 | 否 | 混合事实 |
| V35 | 导出范围继承 | Application | 导出取当前查询条件全部记录、不使用页面分页；越权条件按范围校验拒绝；零记录抛「当前条件下无可导出数据」；请求级空取值不附加该层过滤（全空即平台按授权全量，见 S6-D18）；账号级空组织/医院/院区用例不构造（权限系统限制，正常业务不可达，见 S6-D18） | 否 | 否 | 替身/部分记录 |
| V36 | 导出行数上限 | Application | 超过 10 万行业务拒绝并提示缩小查询条件；未超限正常生成；10 万行上限为对 SRS F09『全部匹配记录』的已接受收窄 | 否 | 否 | 计数替身 |
| V37 | 导出内容负向边界 | Application | 导出不包含完整报告结构化内容、PDF 文件标识与下载入口、影像调阅地址；医院院内项目编码、院内项目名称与项目映射信息不导出 | 否 | 否 | 一般事实 |
| V38 | 导出文件响应 | Host | POST 返回 `.xlsx` 文件流，Content-Type 正确、文件名为导出类型加起止日期、响应头禁止缓存；经已鉴权客户端可取回字节；零记录与超限两种业务拒绝下不返回文件响应、不产生空工作簿（F09 备选流 2、3） | 是 | 否 | 宿主可用 |
| V39 | 匹配记录集合视图 | Query | 按标识取单记录：组级信息、患者字段、就诊、未反馈或已处理状态、决定主体与全部匹配项；未知标识按业务拒绝 | 是 | 否 | 既有匹配记录 |
| V40 | 未认证请求 | Host | 十一个端点不带凭据均返回 401 | 是 | 否 | 宿主可用 |
| V41 | 真实库汇总与明细成功面 | DB | 四查询在真实库返回正确分组与明细，数值与只读回库核对一致 | 是 | 否 | 真实业务事实（含未反馈记录） |
| V42 | 真实库分页行为 | DB | 计数与数据筛选一致；首中末页与越界页无重复无遗漏 | 是 | 否 | 超一页事实 |
| V43 | 目标库索引核对 | DB | 三条统计索引在目标库存在且列定义与迁移脚本一致 | 是 | 否 | 负责人执行迁移后 |
| V44 | 宿主导出下载链路 | Host | 从宿主会话发起导出，浏览器收到文件且内容可打开；失败时宿主统一提示；接近 10 万行上限规模的真实导出不构造，S6-D16 重审条件随真实业务量增长后复核 | 是 | 否 | 宿主可用 |
| V45 | 组织服务失败面 | Application | 范围解析零结果（组织、医院或院区不存在或已停用）整次业务拒绝；组织服务调用异常按原样抛出、整次失败 | 否 | 否 | 注入替身 |
