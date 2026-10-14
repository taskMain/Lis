# T01 后端设计

总体范围与关键决策见 [阶段设计](../design.md)。本阶段全部能力为只读查询与文件生成导出，无写用例、无命令、无事务、无领域事件。

## 领域与契约

聚合沿用 `MedicalRecognitionReportAggregate`。导出是对既有互认配置与金额读路径的文件组装能力：不新增实体、不形成新的聚合边界、不新增事实载体。必须保持不变的不变量：导出与查询不产生任何写入、事件或状态变化；拒绝与失败整次失败、零副作用。

本阶段无 Command/FacadeCommand，Command-Event 评审表不适用；无写用例，事务表为空——三个导出入口均为只读链，应用服务与查询仓储不声明事务（沿用阶段 6 统计链零事务判据口径）；并发只读无补偿面；幂等、唯一性无业务写入面。

### 层映射

| 概念层 | 实际项目/目录 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| Contracts | `Dy.MedicalRecognition.Application.Contracts`（`Queries/`、`ReadModels/`） | `Domain.Share` | Application、Domain、Repository |
| Application | `Dy.MedicalRecognition.Application`（`Queries/`） | Contracts、Domain、平台 NuGet 契约与 ClosedXML | Repository 实现、SQL、DataMapper |
| Domain | `Dy.MedicalRecognition.Domain`（`Queries/Ports/`） | `Domain.Share` | HTTP、Application、数据库实现 |
| Domain.Shared | `Dy.MedicalRecognition.Domain.Share`（Enums） | 无新增类型（经既有枚举间接使用） | 其余全部 |
| Repository/Infrastructure | `Dy.MedicalRecognition.Repository`（`Queries/`） | Domain 查询端口、Application.Contracts（既有工程引用） | 领域规则决策、文件组装 |
| Host | `Dy.MedicalRecognition`（`Controllers/`） | Application、Contracts、Domain 与 Repository（依赖装配） | 业务规则、SQL |

### 类型归属表

| 类型名 | 角色 | 所在层/目录 | 调用方 | 主要职责 | 禁止职责 |
|---|---|---|---|---|---|
| `RecognitionProjectConfigurationExportRequest` | 新增 Request | C/`Queries/MutualRecognition/` | 导出控制器、应用层 | 互认项目导出条件：`OrganizationCode`（必填）、`Keyword`（可选，传入空白文本按校验拒绝，与列表请求一致） | 状态筛选字段、分页对象、可信用户字段 |
| `RecognitionAmountExportRequest` | 新增 Request | C/`Queries/RecognitionAmount/` | 导出控制器、应用层 | 平台金额导出条件：`OrganizationCode`、`HospitalCode`、`BranchCode`（必填）、`Keyword`（可选，缺失或空白视为未提供，与列表请求一致） | 状态筛选字段、分页对象、可信用户字段 |
| `BranchRecognitionAmountExportRequest` | 新增 Request | C/`Queries/RecognitionAmount/` | 导出控制器、应用层 | 本院区金额导出条件：`BranchCode`（必填）、`Keyword`（可选，缺失或空白视为未提供，与列表请求一致） | 组织与医院字段、状态筛选字段、分页对象、可信用户字段 |
| `ConfigurationExportFileReadModel` | 新增读模型 | C/`ReadModels/` | 导出控制器 | 导出文件读模型：`FileName`、`FileFormat`、`FileStream`（字节数组） | 展示以外的职责、公开可写集合 |
| `IMedicalRecognitionReportQueryAppService` | 修改：新增分部 `.Export.cs`（三个导出方法声明） | C/`Queries/` | 导出控制器 | 三个导出应用层入口声明（`NonAction`） | 写操作 |
| `MedicalRecognitionReportQueryAppService` | 修改：新增分部 `.Export.cs`（三个导出方法实现） | A/`Queries/` | 导出控制器 | 范围校验、复用列表查询取全量、零记录与行数上限校验、ClosedXML 工作簿组装 | SQL、DataMapper、写入、事件 |
| `RecognitionExportController` | 新增手工控制器 | H/`Controllers/`，路由 `api/v1/recognition-export` | 浏览器、生成端 | 三个 POST 动作（`recognition-projects`、`recognition-amounts`、`branch-recognition-amounts`），绑定请求体并返回禁止缓存的文件流 | 业务规则、领域判断、SQL |

既有复用、不新增不修改：`IMedicalRecognitionReportQueryRepository` 的两条列表查询方法 `QueryRecognitionProjectConfigurationListAsync`、`QueryRecognitionAmountListAsync` 为导出取数入口——本院区金额导出与对应列表查询相同，经可信范围解析后复用 `QueryRecognitionAmountListAsync`（仓储侧无独立本院区方法，与列表查询的实现形态一致）；`TrustedOrganizationResolver`、`OrganizationPathResolver`、`TrustedScopeResolver` 为范围校验解析点；`EnumDescriptorText` 中文解析与既有 `MedicalRecognitionReportQuery.xml` 两条查询语句原样使用。仓储接口不加 overload、SqlMap 不新增语句与注册键。`ConfigurationExportFileReadModel` 不复用 `StatisticsExportFileReadModel` 的检索结论：复用需把阶段 6 已交付契约中性化改名或迁移，触及已交付对象且收益仅为消除三字段重复，故新建（backend-architecture「字段相似不构成复用同一类型的理由」）。

### 调用链与字段来源

```text
导出 Request（Contracts）
-> MedicalRecognitionReportQueryAppService.Export 分部
   （范围校验 -> 复用列表查询取全量 -> 零记录/行数上限校验 -> 工作簿组装）
   -> TrustedOrganizationResolver / TrustedScopeResolver + OrganizationPathResolver（范围校验，与对应列表查询同一解析点与先后）
   -> IMedicalRecognitionReportQueryRepository 两条列表查询方法（互认项目导出的配置状态参数恒为 null）
-> ConfigurationExportFileReadModel（FileName / FileFormat / FileStream）
-> RecognitionExportController -> File(bytes, xlsx, fileName)（禁止缓存响应头）
```

三个入口的范围校验与对应列表查询逐字同链：

- 互认项目导出：请求组织必须等于服务端可信当前组织（`HttpRequestInfo.OrgId`，经 `TrustedOrganizationResolver` 去首尾空白解析），不等即拒绝；关键字随请求校验，传入空白文本按校验拒绝（与列表请求的 `[NonEmpty]` 口径一致），非空时原样下传仓储。
- 平台金额导出：请求组织、医院、院区按请求使用，经 `OrganizationPathResolver` 一次性批量校验存在、启用与父子归属，非法组合整次拒绝；不要求等于可信组织（与列表查询同口径）；关键字缺失或去首尾空白后为空视为未提供、不附加过滤（与列表查询的 `NullIfBlank` 口径一致）。
- 本院区金额导出：组织与医院由 `TrustedScopeResolver` 从可信上下文解析（令牌优先、缺失层按登录用户档案补齐、不一致即拒绝、补齐后仍空拒绝），再经 `OrganizationPathResolver` 校验请求院区存在、启用且属于可信医院——两次解析的先后与职责和列表查询一致；关键字空白口径同平台金额导出。

关键字匹配口径与列表查询一致：按字面包含同时匹配标准项目编码、标准项目名称、所属分类名称、所属分组名称，不区分大小写，`%` 与 `_` 按普通字符转义。导出请求不携带任何状态筛选字段；互认项目导出调用列表查询时配置状态参数恒为 `null`，即同时包含启用与停用配置。

**工作簿组装。** 单工作表，工作表名与页面名称一致；表头行加粗、冻结首行、列宽按内容适配；列集与取值按阶段设计两套列集执行，序号 1..N 按导出行序；当前金额与可互认时间写数值单元格并声明数字格式（金额列 `0.00`、可互认时间列整数无小数位），未配置金额单元格留空；文本列写读模型文本字段，金额状态由应用层按 `IsAmountConfigured` 输出「已配置 / 未配置」；`ItemTypeText` 为 `null` 时项目类型单元格输出空、不伪造类型名。文件名为「页面名称 + `yyyyMMddHHmmss`（组装时刻）+ `.xlsx`」，内容类型 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`；页面名称为固定中文字面量，不引入文件名特殊字符处理。三个入口共用同一组装实现，仅列集、工作表名与文件名前缀不同。导出过程不写数据库、不登记事件、不留存文件副本。

**静态守卫同批。** ① 冻结 OpenAPI 路径集合断言（`Stage2EnumContractTests` 的 `Frozen_openapi_keeps_the_expected_path_set_and_no_unnormalized_shapes`，全集合相等断言）与冻结路径计数断言（`Stage7ExclusionGuardTests`）同步新增三个导出端点；② 手写控制器冻结集合断言（`Stage5ConstraintTests` 的 `Host_keeps_the_frozen_handwritten_controller_set`，控制器文件清单与声明类型清单）登记新控制器；③ 查询入口方法集合断言与契约分部文件清单/数量断言登记三个导出方法与新增分部；④ 分页契约禁用字段守卫的逐类型清单追加三个导出请求（无分页对象）；⑤ 导出链零写入静态守卫的扫描文件清单登记新增分部（该扫描面为显式清单，漏登记不判失败、但会静默失去覆盖，随分部新增同批登记）。既有断言只扩清单、不放宽。

## 数据与集成

零新增表、零迁移、零新增 SQL：取数复用 `MedicalRecognitionReportQuery.xml` 既有两条列表查询语句，行集、关键字过滤与排序和列表查询完全一致。无外部调用（范围解析点为既有应用内部服务，组织、医院、院区名称不进入导出列集）。失败语义：请求校验失败、范围校验失败、零记录与行数上限拒绝均按业务拒绝整次失败，不返回部分文件；其余异常按原样传播，不新增异常中间件或响应包装。未认证请求由框架认证链路拒绝。控制器与统计导出控制器同款：应用层导出方法以 `NonAction` 声明排除自动端点，手工控制器的三个 POST 动作（路由 `api/v1/recognition-export` 下 `recognition-projects`、`recognition-amounts`、`branch-recognition-amounts`）是导出能力的全部对外端点；响应头设置禁止缓存。

## 验证矩阵

本表只记录测试前设计，实际执行结果进入 [阶段测试报告](../testReport.md)。层级取值沿用既有口径的适用子集：`Contract`（请求/读模型形状）、`Static`（静态守卫）、`Application`（应用层行为）、`Query`（取数与组装口径，含真实库取数面）、`Host`（真实宿主链路）；本阶段零新增 SQL，无 Repository/DB 层新增面。本阶段无写用例，「真实 WorkUnit」列全部为否。

| 编号 | 用例与边界 | 层级 | 预期结果 | 真实数据库 | 真实 WorkUnit | 前置数据 |
|---|---|---|---|---|---|---|
| T01-V1 | 导出契约形状 | Contract | 三个导出 Request 字段集合正确：范围必填 + 可选关键字，互认项目与本院区请求不携带多余范围字段，三者均无状态筛选字段、无分页对象，关键字空白口径与各自列表请求一致；导出文件读模型恰含文件名、文件格式、文件字节三字段；查询契约分部恰新增三个导出方法 | 否 | 否 | 稳定契约 |
| T01-V2 | 互认项目导出范围校验 | Application | 请求组织等于可信当前组织才执行，不等即拒绝、不降级为空集合；可信组织缺失或纯空白拒绝；关键字传入空白文本按校验拒绝（与列表一致），非空时原样下传仓储 | 否 | 否 | 可信上下文替身、仓储替身 |
| T01-V3 | 平台金额导出范围校验 | Application | 组织、医院、院区经组织路径解析校验存在、启用与父子归属，任一非法整次拒绝且无文件生成；不要求等于可信组织；关键字缺失或空白视为未提供、不附加过滤 | 否 | 否 | 组织服务替身（含不存在、停用、父子不匹配） |
| T01-V4 | 本院区金额导出范围校验 | Application | 组织与医院由可信上下文解析（令牌优先、档案补齐、不一致拒绝、补齐后仍空拒绝）；请求院区再经组织路径解析校验存在、启用且属于可信医院，不属于即拒绝 | 否 | 否 | 可信上下文替身、组织服务替身 |
| T01-V5 | 行集与排序一致性 | Query | 导出行集与对应列表查询同一语句、同筛选、同排序（项目类型、分类名称、分组名称、标准项目名称逐级升序加标识兜底）；互认项目导出不按配置状态过滤（含启用与停用配置）；金额导出行集含未配置金额行 | 是 | 否 | 同一范围多条配置（含停用配置、未配置金额行、目录停用行） |
| T01-V6 | 零记录拒绝 | Application | 当前条件下无记录时业务拒绝「当前条件下无可导出数据」，不生成文件读模型、不产生空工作簿 | 否 | 否 | 空结果替身 |
| T01-V7 | 行数上限 | Application | 取回行数超过 100000 时业务拒绝并提示缩小查询条件；未超限正常组装；上限校验在取回后执行，防御组装与传输面（内存面按 T01-D8 登记） | 否 | 否 | 集合计数替身 |
| T01-V8 | 列集与取值 | Application | 互认项目导出恰九列、金额导出恰十列，列名与顺序符合阶段设计；序号 1..N 连续；项目类型与互认配置状态输出服务端文本，`ItemTypeText` 为 null 时单元格为空；金额单元格数字格式 `0.00`、可互认时间列为整数格式；未配置金额单元格为空、金额状态「未配置」；当前不可用原因为空输出空 | 否 | 否 | 混合行替身（含停用配置、未配置金额、目录停用、类型未登记） |
| T01-V9 | 工作簿组装与文件名 | Application | 单工作表、工作表名与页面名称一致、表头加粗、冻结首行、列宽按内容适配；金额与可互认时间列的数字格式符合口径（`0.00`、整数）；文件名为页面名称 + `yyyyMMddHHmmss` + `.xlsx`；组装过程无仓储写入、无事件登记 | 否 | 否 | 一般行集替身 |
| T01-V10 | 静态守卫 | Static | 冻结 OpenAPI 路径集合断言与路径计数断言含三个导出端点；手写控制器冻结集合断言登记新控制器；查询入口方法集合与分部文件清单断言含三个导出方法；分页禁用字段守卫清单追加三个导出请求；零写入扫描清单登记新增分部；既有断言未放宽 | 否 | 否 | 无 |
| T01-V11 | 导出文件响应与宿主链路 | Host | 三个 POST 端点（`api/v1/recognition-export/recognition-projects`、`/recognition-amounts`、`/branch-recognition-amounts`）返回 `.xlsx` 文件流、Content-Type 正确、响应头禁止缓存、Content-Disposition 文件名符合「页面名称 + `yyyyMMddHHmmss` + `.xlsx`」格式；经已鉴权客户端可取回字节且文件可打开；零记录拒绝时不返回文件响应；未认证请求返回 401 | 是 | 否 | 宿主可用；真实业务数据 |
