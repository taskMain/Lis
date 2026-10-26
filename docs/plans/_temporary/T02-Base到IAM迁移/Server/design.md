# T02 后端设计

总体范围与关键决策见 [阶段设计](../design.md)。

## 领域与契约

### 1. 身份与当前人

- 认证：宿主按 `IamApronOpenApiModule` 接入，认证方案 `IAM.ApplicationBearer`；配置节 `Iam` 提供 Authority、`AllowHttp`、`ApplicationCode`、`ProfileTimeoutSeconds`。
- 当前人：`ICurrentIamContext.RequireCurrent()` 返回 `UserId`、`SessionId`、`WorkIdentityId`、`TenantId`、`OrganizationId`。
- 旧 `HttpRequestInfo` 只保留 `UserId` 的兼容适配；组织、医院、院区不再从它读取，避免读到空值后按拒绝分支误判。
- 操作人：Command 的 `OperId` 取 `RequireCurrent().UserId`；解析失败即失败，不写空操作人。
- 会话入口：`GET api/v1/current-session` 读取 `RequireCurrent()`，返回 `UserId`、`SessionId`、`WorkIdentityId`、`TenantId`、`OrganizationId`；未选择工作身份时后三者为空，不报错。

### 2. 可信范围解析

替换 `TrustedScopeResolver` 与 `OrganizationPathResolver` 的数据来源，保留原有判定语义：

| 原语义 | 迁移后 |
| --- | --- |
| 令牌缺层时用用户档案补齐 | 删除：组织层只来自上下文 `OrganizationId`，医院层与院区层来自该组织的祖先链 |
| 令牌与档案冲突即拒绝 | 删除：不再存在两个来源，冲突判定随之退出 |
| 组织不存在/停用/父子不匹配即拒绝 | 保留：改为对 IAM 组织业务编码在组织树内的存在性、启用状态、类型与父子关系判定 |
| 院区可由请求提交并校验归属（金额入口） | 保留：提交值为组织业务编码，需在可信医院下且类型为院区 |

- 祖先链读取：按 `OrganizationId` 批量读取当前节点与祖先（单批最多 100 个 Id），按 `OrganizationType.Code` 归类为医共体/医院/院区；同一类型出现多个祖先时按业务规则拒绝而不是任取一条。
- 选择列表：按当前租户取组织树，按目标医共体/医院/院区过滤后批量读取详情与类型；类型未配置的节点不进选择列表，并在结果中单独标记数量。
- 查询入口：`POST /Api/OrganizationScopeQuery/QueryCurrentOrganizationScope` 返回当前身份的三级标识与名称；`POST /Api/OrganizationScopeQuery/QueryOrganizationScopeOptions` 返回当前租户的三级选项与未配置类型数量。

### 3. 业务配置读取

- 三项参数从 `mrec_platform_parameter` 按编码读取；缺失、停用或格式非法按配置错误拒绝，不使用默认值。
- 校验规则与维护入口共用同一实现：开关 `0`/`1`、排除时长非负整数、引用详情时长正整数。

### 4. 命令与查询

- Command/Request/ReadModel 中组织、医院、院区字段保持 `string`，承载 IAM 组织业务编码；DTO 名称保持 `OrganizationCode` 等编码语义。
- 枚举、事件与工作单元语义不变；写路径仍按既有 Command-Event 顺序执行。
- 查询过滤条件保持等值文本比较，比较对象为组织业务编码；唯一键与范围索引落在编码列，不需要重建。

## 数据与集成

### 1. 表结构变更

| 表 | 变更 |
| --- | --- |
| `mrec_medical_recognition_report` | `organization_code/hospital_code/branch_code` 保持文本编码列，值为 IAM 组织业务编码；唯一键与范围索引保持在编码列 |
| `mrec_organization_hospital_branch_recognition_amount` | 同上三列保持编码；唯一键 `ux_mrec_org_hos_brh_project` 不变 |
| `mrec_recognition_match_record` | 同上三列保持编码；相关索引不变 |
| `mrec_mutual_recognition_item` | `organization_code` 保持文本编码列；唯一键 `ux_mrec_medical_recognition_org_project` 不变 |
| 其余 15 张表 | 无组织维度列；`oper_id` 仍为 uuid，语义改为 IAM 用户 Id |
| 新增 `mrec_platform_parameter` | `id uuid`、`parameter_code text`（唯一）、`parameter_value text`、`description text`、`is_valid boolean`、`oper_time timestamptz`、`oper_id uuid` |

`mrec_` 表结构不变，不回填历史行。迁移期新增的 10 个 `*_id` uuid 列（四张表：报告 3、金额 3、匹配记录 3、互认项目 1）由负责人执行删除；删除脚本、执行前后校验 SQL 与回滚语句随交付。执行前后端与集成测试暂停。

### 2. 集成

- 目录：`IIamDirectoryClient.BatchReadDirectoryAsync` 读取组织详情、类型与业务编码（编码取自目录接口返回的组织 `code`，SDK 契约未声明该字段，由本项目读取点承载）；组织树使用 IAM 组织查询入口，同时提供编码与父子关系用于提交编码校验。
- 本人资料：`IIamCurrentProfileClient` 只在需要姓名/组织名称时调用，且不作为认证前置。
- 失败语义：401/authentication_required 交认证流程；403 按具体错误码；409/work_identity_required 表示业务需要工作身份；503/iam_profile_unavailable 表示资料服务不可用，不返回空成功。
- 删除的集成：Base 用户、组织、系统参数、字典四个服务代理配置，以及推送代理配置（无代码引用）。

### 3. 维护能力（F11）

- 接口：参数列表查询与单条保存；保存时执行与读取相同的校验。
- 存储：`mrec_platform_parameter`；参数编码唯一，停用后读取按缺失处理。
- 授权：不做角色判定；能调用接口即认为有权限，保留文件内既有口径。

## 验证矩阵

| 编号 | 用例与边界 | 层级 | 预期结果 | 真实数据库 | 真实 WorkUnit | 前置数据 |
| --- | --- | --- | --- | --- | --- | --- |
| V1 | 启动与依赖解析：宿主加载 IAM 模块，工程无 Base 认证包与 `JwtSettings` | 集成（本地启动） | 启动成功，OpenAPI 可访问，无旧认证注册 | 否 | 否 | 无 |
| V2 | 匿名调用受保护接口 | 接口 | 401 authentication_required，不进入业务逻辑 | 否 | 否 | 无 |
| V3 | 无效/过期令牌调用 | 接口 | 401，不降级为匿名，不返回业务数据 | 否 | 否 | 无 |
| V4 | 有效令牌调用并读取当前人 | 接口 | 200；`UserId` 非空，未选身份时组织为空 | 否 | 否 | 测试令牌 |
| V5 | 本院入口：组织为医院/院区时解析三层 | 应用 | 返回当前节点及祖先中实际存在的类型；缺类型时明确拒绝 | 是（IAM） | 否（只读查询） | 医院/院区身份 |
| V6 | 平台入口：请求组织/医院/院区为业务编码且在组织树内父子成立 | 应用 | 通过；跨组织或跨医院组合被拒绝 | 是（IAM） | 否（只读查询） | 多组织数据 |
| V7 | 组织路径批量读取次数 | 应用 | 组织一次、每医院一次院区，不随条目数增长 | 是（IAM） | 否 | 多目标批次 |
| V8 | 停用组织节点 | 应用 | 按不存在处理并拒绝 | 是（IAM） | 否 | 停用节点 |
| V9 | 未选择工作身份访问需身份业务 | 接口 | 409/work_identity_required 或按既有业务拒绝 | 是（IAM） | 否 | 无身份令牌 |
| V10 | 迁移期实例列退出与编码列结构 | 数据库 | 编码列为业务键、唯一键与索引保持在编码列；10 个 `*_id` 列删除后读写正常 | 是 | 否 | 删除脚本 |
| V11 | 参数缺失与非法值 | 应用 | 明确配置错误拒绝，不使用默认值 | 是 | 否 | 空参数/非法值 |
| V12 | 参数维护保存校验与立即生效 | 接口 + 应用 | 非法值被拒绝；合法值保存后下一次读取即生效 | 是 | 是 | 参数表 |
| V13 | 参数停用 | 应用 | 停用后读取按缺失处理并拒绝 | 是 | 否 | 停用参数 |
| V14 | 权限不扩散静态扫描 | 静态 | 业务代码无角色/接口级授权特性 | 否 | 否 | 无 |
| V15 | 操作人落库 | 应用 + 数据库 | `oper_id` 等于登录用户 Id，请求字段不能替换 | 是 | 是 | 写命令 |
| V16 | 双入口范围口径 | 应用 | 本院入口范围不可由请求扩大；平台入口按请求值校验归属 | 是（IAM） | 否（只读查询） | 双身份 |
| V17 | Base 依赖静态扫描 | 静态 | 包引用、配置节、using 与旧令牌读取为零 | 否 | 否 | 无 |
| V18 | 并发身份隔离 | 接口 | 两个用户并发请求的身份与数据不串号 | 是（IAM） | 否 | 两个令牌 |

本表只记录测试前设计，实际执行结果进入 [阶段测试报告](../testReport.md)。
