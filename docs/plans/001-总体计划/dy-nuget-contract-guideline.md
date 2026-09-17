# 远程契约包与外部服务接入指南

本文是远程契约包引用与外部服务接入的单一事实来源，供各阶段编写阶段设计与实施计划时对照。环境地址值引用 [Test Environment](../../../.agents/instructions/test-environment.md)，不在本文重复维护；协作原则与错误语义见 [总体计划设计](design.md) 第 5 节。

## 1. 适用范围

本项目通过已发布的契约 NuGet 包 + 平台 HTTP 服务代理访问外部系统，不在业务代码中手写 `HttpClient`，不自行拼装外部系统的 HTTP 协议。本文覆盖：契约包版本位置、服务地址位置、已接入服务清单、调用与封装口径、失败与错误语义，以及阶段计划必须具备的条目。

## 2. 版本位置

- 契约包版本统一由 `server/Directory.Packages.props` 以 `<PackageVersion Include="..." Version="..." />` 集中管理（该文件 `ManagePackageVersionsCentrally` 为 `true`）。
- 消费方项目必须**显式**添加 `<PackageReference Include="..." />`；只写中心版本号不会产生引用。
- 当前引用 `Dy.Base.Application.Contracts` 的项目为 `Dy.MedicalRecognition.Repository` 与 `Dy.MedicalRecognition.Application`。

## 3. 地址位置

- 外部服务地址统一配置在宿主项目 `server/Dy.MedicalRecognition/appsettings*.json` 的 `HttpConfig.HttpServiceConfigs` 节点。
- key 使用**契约接口的完整类型名**；value 至少包含 `Host` 与 `Timeout`。
- `appsettings.Development.json` 用于开发环境，`appsettings.json` 用于发布正式环境。
- **当前状态**：开发配置登记了第 4 节的 5 条代理；发布基座 `appsettings.json` 目前只有 `Default` 一条，**未登记外部服务地址**。生产地址由部署方按同一 key 逐条配置，仓库内不预置开发地址。

## 4. 已接入的外部服务

| 契约接口（配置 key） | 开发地址 | 代码消费状态 |
|---|---|---|
| `Dy.Base.Application.Contracts.UserAggregate.IUserAppService` | `http://183.224.180.166:35001` | 已在可信范围解析的令牌缺失层补齐中使用 |
| `Dy.Base.Application.Contracts.OrganizationAggregate.IOrganizationAppService` | `http://183.224.180.166:35001` | 已在组织路径解析与两个查询、命令入口中使用 |
| `Dy.Base.Application.Contracts.SystemParameterAggregate.ISystemParameterAppService` | `http://183.224.180.166:35001` | **仅登记代理配置，尚无代码消费** |
| `Dy.Base.Application.Contracts.DictionaryAggregate.IDictionaryAppService` | `http://183.224.180.166:35001` | **仅登记代理配置，尚无代码消费** |
| `Dy.PushCenter.Application.Contracts.PushTaskAggregate.IPushTaskAppService` | `http://183.224.180.166:35005` | **仅登记代理配置，尚无代码消费**（推送属后续阶段范围） |

文件服务（`localhost:9333`）不属于契约包代理，其可用性由后续阶段的 PDF 相关验证使用。

前端另有**直连**平台 Base API 的路径：子应用通过 `configureBaseComponents({ baseUrl })` 使用 `@dy/components-base` 的组织/医院/院区选择能力，地址来自子应用运行时配置 `baseApiBaseUrl`。该路径只用于组织范围选项读取，不替代后端契约代理，也不构成业务数据通道。

## 5. 调用与封装口径

- Application 层通过构造函数注入契约接口，在应用服务内封装为本系统的业务语义；外部契约 DTO **不外泄**给前端，需要暴露时另建本系统轻量 DTO。
- 固定业务入参（可信组织、医院、院区、操作人）一律由后端从可信上下文解析，不由前端传参决定。对保存类接口是否再调用远程二次校验，必须在阶段设计中明确，默认不隐式调用。
- 查询实现不得按结果行逐条调用外部服务（N+1）；附属数据必须批量取回后内存映射填充，调用次数只随筛选维度增长、不随返回行数增长，见 [Backend Architecture](../../../.agents/instructions/backend-architecture.md) 的批量读取约束。
- 组织、医院、院区名称由服务端解析后返回；页面不展示三级编码，名称缺失按既有占位口径处理。

## 6. 失败与错误语义

- 外部系统无响应、未授权或异常时，按 [总体计划设计](design.md) 5.2 由既有异常链路表达失败，不新增专用异常类型、中间件或错误包装。需要对使用方说明来源时，用明确的中文业务错误表达（例如“标准医疗项目目录查询失败”）。
- 测试报告记录外部服务地址、配置 key、调用的接口与失败响应摘要，**不得记录完整 token**。
- 组织类外部调用失败是否降级（例如名称留空而不阻断主体）由调用点所在阶段的设计决定并写入该阶段文档，不得默认静默忽略。

## 7. 阶段计划必备项

使用或新增外部服务的阶段，其设计必须写清下列条目；任一条不明确时暂停并确认，不得猜接口、不得改用其他接入方式：

1. 契约包名称与版本管理位置；
2. 消费方项目的显式 `PackageReference`；
3. `HttpConfig.HttpServiceConfigs` 的配置 key 与 `Host`/`Timeout`；
4. 远程契约接口的完整类型名；
5. 调用的方法名与请求模型；
6. 本系统为该服务封装的接口与 DTO；
7. 固定业务入参（哪些值由后端从可信上下文解析）；
8. 保存类接口是否在保存前二次调用远程校验；
9. 外部服务失败时的错误语义与降级边界；
10. 验证项：浏览器 Network 中不出现对**业务**外部服务的直连；组织范围选项读取（`@dy/components-base` 走 Base API）是既有例外，须在 Network 中确认只存在该例外路径。
