# C# Backend Style

本文只约束 MedicalRecognition 后端 C# 实现细节。架构设计遵循 [Backend Architecture](backend-architecture.md) 和 [Backend Command Query Event](backend-command-query-event.md)；注释与复用遵循 [C# Documentation And Reuse](csharp-documentation-and-reuse.md)；测试遵循 [Testing Baseline](testing-baseline.md) 与 [C# Backend Testing](csharp-backend-testing.md)。

## 1. 强制闸门

1. 不修改生成代码，不扩大到未触及文件，不夹带无关重构或格式化。
2. 请求、业务规则和持久化边界必须显式校验。
3. 方法超过约 60 行是审查信号，应检查是否混合抽象层级或职责；不得只为压缩行数创建无业务语义的 Helper。

设计、GitNexus impact、测试驱动和注释/复用闸门由根 `AGENTS.md` 路由到对应规范，不在本文件重复。

## 2. 文件与格式

- 使用 file-scoped namespace；namespace 后空一行。
- 一个非生成 `.cs` 文件最多一个顶级 `class`、`record`、`struct`、`interface`、`enum` 或 `delegate`，文件名与类型名一致。
- partial 按 `TypeName.Feature.cs` 拆分，每个文件仍只声明该类型。
- 目录可以按业务特性拆分，但不得只为匹配目录修改公共 namespace、自动路由或序列化契约。
- 生成代码、第三方源码、`GlobalUsings.cs`、程序集属性和顶级语句不适用一类型一文件规则。
- 字段、构造函数、属性、事件和方法之间用一个空行分组；XML 注释紧贴成员。
- 方法内部只用单个空行区分校验、查询、领域调用、映射和返回。
- 不对未触及的大文件执行全量换行、排序或格式化。

## 3. 语言、命名与类型

- 优先使用项目支持且更清晰的现代 C#：`[]`、`[.. items]`、pattern matching、switch expression、`?? throw`、合适的 record/init。
- 类型、方法、属性和事件使用 `PascalCase`；参数和局部变量使用 `camelCase`；私有字段使用 `_camelCase`；异步方法以 `Async` 结尾。
- 命名表达业务概念，避免 `data`、`temp`、`obj`、`result1` 和无语义的 `Handle`、`Process`、`Convert`。
- `_` 只作 discard。
- 默认使用显式类型。右侧明确出现具体类型、匿名类型、惯用 `out var` 或简单 foreach 时可使用 `var`。
- 空集合返回 `[]`，不返回 `null`；只需枚举使用 `IEnumerable<T>`，需要数量、索引或稳定顺序使用 `IReadOnlyList<T>`。
- 不用 tuple 表达复杂公开结果；定义有业务语义且序列化稳定的类型。

生产代码禁止通过反射猜测已知静态成员。必须优先使用静态类型、泛型、接口、SourceGen 或框架公开 API。确需运行时反射时，修改前必须取得项目负责人明确批准并记录打包边界、替代方案、影响和测试；测试代码边界遵循后端测试规范。

## 4. 方法与控制流

- 使用 guard clause，不把主流程包在多层 `if` 中。
- 普通方法的 `if`、`foreach`、`try` 嵌套原则上不超过两层。
- 分支已经终止控制流时不再增加 `else`。
- 短小 guard 可以同行；条件或语句较长时使用大括号。
- LINQ 只表达清晰筛选、排序和简单投影；复杂分组、聚合和业务判断拆成具名步骤。
- 一个方法只处理同一抽象层级；不为一两行无业务含义的代码创建 Helper。
- 私有方法必须隐藏完整业务规则、复杂转换或有独立业务意义的操作。仅包装当前用户解析、当前时间获取、少量属性赋值或一次映射调用的 `SetXxx`、`FillXxx`、`CreateCommand`、`BuildCommand` 应内联；不得只为缩短入口方法或消除少量重复而提取。
- 对象初始化前先计算业务值；领域实体通过构造函数、工厂或领域方法建立，不绕过不变量。

## 5. 校验、异步与异常

- 公共 Request 声明必要的 `[Required]`、长度、范围、集合数量和枚举约束。
- AppService 入口检查 Request，并统一调用项目验证器；不得复制私有 `ValidateRequest`。
- Manager 和 Entity 继续保护业务不变量，不能只依赖 Application 校验。
- 不使用 `.Result`、`.Wait()` 或 `async void`（事件处理器除外），保持完整异步链路。
- 只捕获能够处理、补偿、释放资源、审计或补充必要上下文的异常；不吞异常，不使用 `throw ex;`。
- 参数错误使用参数异常；业务错误使用项目既有业务异常链路，不抛空泛 `Exception`。
- 不增加项目专属异常包装、中间件或响应结构绕过宿主统一错误处理。
- 外部服务失败不得伪装为空值、默认值或部分成功；明确批准的降级除外。
- `OperationCanceledException` 必须继续传播。

## 6. 框架和映射

新增实现依次检查当前模块、项目同类实现、共享扩展包、框架公开 API 和现有调用方式。具体搜索证据遵循注释与复用规范。

MedicalRecognition 的共享能力检索范围必须包含 `Dy.Core.Extensions`、`Dy.Core.*`、`Dy.Apron.*` 和当前项目已有实现。

- 优先复用枚举描述、字符串、Guid、日期、集合、分页和排序能力。数据库 `DataMapper` 只在 Repository/Infrastructure 边界使用，AppService 和 Domain 不得直接使用。
- `*DataMaps.cs` 是 `ObjectMap` 映射声明文件，不等同于数据库 `DataMapper`。Request 与 Command 中语义、基数和类型兼容的同一业务事实必须同名，并使用 SourceGen 生成的 `MapTo*`；不得仅因分层不同创建同义属性名。
- `ObjectMap` 只处理可以直接转换的公共字段。业务编码到内部 ID、调用方编码集合到服务端可信 Items、Transport 格式到领域值等真实转换，由 AppService 显式完成。
- 映射完成后，AppService 必须显式填写或覆盖来自登录上下文、服务端组织或目录、权限解析、服务端时间等权威来源的字段；Command 属性集合可以因此大于 Request。
- 不得为了减少显式转换而向 Request 增加内部 ID、可信字段、派生状态或同义别名，也不得让 Command 保留尚未解析的 Transport 标识。
- 禁止创建只负责逐字段复制的 `CreateCommand`、`BuildCommand` 或 Mapper Helper，也不得在 AppService 或 Domain 手写大量可由既有映射器完成的赋值。

## 7. 组织信息

本节只约束平台组织、医院、院区等可信主数据及确实需要的平台登录上下文，不覆盖医院提供的业务科室和人员字段。业务 ID 为字符串，具体 ID/名称按请求成对保存，不要求存在于权限系统、不补查或覆盖名称；完整业务规则见本项目 SRS/UML。

平台组织信息通过框架组织服务（现有约定为 `Dy.Base.Application.Contracts.OrganizationAggregate.IOrganizationAppService`）及框架 HTTP 服务代理获取，首次接入核实实际包契约。不查询平台组织表、不跨库关联、不手写 HttpClient。

组织、用户、标准目录、字典和系统参数等框架服务代理的超时与重试责任统一遵循 [Dy Framework WorkUnit](dy-framework-workunit.md#2-外部调用) 的集中配置规则，本文件不重复。

- `HospitalDto.Id` 是医院编码。
- `BranchDto.Id` 是院区编码，`BranchDto.HosId` 是医院编码。
- 需要使用平台 `DeptDto` 时，先核对实际契约中的平台 Id、Code 和 BranchId；不能据此将医院业务科室 ID 声明为 Guid 或要求映射到平台科室。
- 不混用登录身份、平台标识和医院业务 ID，不脱离医院范围假设院区编码全局唯一。

列表名称回填顺序：

```text
数据库筛选和分页
-> 收集当前页标识
-> 去重分组
-> 范围查询
-> 字典回填
```

- 平台主数据字典键包含父级范围，例如 `(HospitalCode, BranchCode)`；医院业务科室/人员按 SRS 定义的院区级业务 ID 归属，不进入平台名称补全链路。
- 同时返回稳定编码/ID 和名称，禁止在 SQL 中用编码冒充名称。
- 组织和资源归属使用稳定 ID/编码，不使用名称。操作授权由权限系统负责，本项目不重复建设角色权限判断。
- 组织服务失败只有在设计明确允许时才能降级。
- 多个查询重复解析时抽取 Application 内部解析器，不下放前端。

## 8. 数据库与契约

- `Scripts` 中建表脚本表达最终结构，不包含 `ALTER`；增量迁移放 `Scripts/Migrations`。
- 迁移不能伪造历史可信身份；明确可空性、默认值、回填和回滚边界。
- 契约或数据结构变化前确认设计已批准，并只同步实际受影响的契约面：HTTP Request/Response/枚举检查 OpenAPI、Kiota/API Client 和接口契约测试；DomainEvent 检查订阅者、事件契约测试和 UML；数据库结构检查最终脚本、增量迁移、Repository/SQL 与迁移测试。涉及业务事实或公共业务契约时再同步 SRS/UML，不要求所有变化机械更新所有产物。
- 查询必须明确组织范围、稳定唯一排序、分页和空值语义。
- 不通过全量加载模拟已经定义的分页契约。

## 9. 完成检查

- 分层、校验、组织范围、映射和异常边界正确。
- 注释、复用检索和新增抽象证据符合 [C# Documentation And Reuse](csharp-documentation-and-reuse.md)。
- 没有重复 Helper、手写重复映射、无理由的 Request/Command 同义改名、N+1 查询或编码冒充名称。
- 没有多个顶级类型、同步阻塞、吞异常、复杂嵌套或无关格式化。
- 没有未经批准的运行时反射和公共基础设施。
- 实际受影响的 HTTP、事件、数据库、业务文档和测试契约面已经同步。

完成时执行的测试、构建、静态检查、`git diff --check` 和 GitNexus `detect_changes` 遵循根 `AGENTS.md` 闸门和 [Testing Baseline](testing-baseline.md)。

可机械判断的规则必须逐步落到 `.editorconfig`、Roslyn Analyzer、编译器、架构测试和 CI，不依赖文字提醒。
