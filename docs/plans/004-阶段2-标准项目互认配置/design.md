# 阶段 2：标准项目互认配置（F02）

阶段设计见 [Server/design.md](Server/design.md) 与 [Client/design.md](Client/design.md)；实施顺序见 [impl.md](impl.md)、[Server/impl.md](Server/impl.md) 和 [Client/impl.md](Client/impl.md)；测试结果见 [testReport.md](testReport.md)。

## 目标与范围

- 目标：平台管理员可切换授权组织，医院管理员限可信上下文允许的所属组织；两者在已获得外部菜单和接口授权后维护互认项目配置，不在本系统重复实现角色授权。
- 纳入：互认项目配置新增、可互认时间修改、独立启用/停用、互认配置查询、`mrec_mutual_recognition_item` 新建表、API Client 和管理端“互认项目”页面。
- 排除：标准项目本体维护、医院院内项目及项目对照、金额维护、报告采集、在线互认匹配、真实删除、配置版本恢复。
- 依据：SRS F02、F02 页面要求、UML `1-MedicalRecognitionReport.wsd` 和 `2-MedicalRecognitionConfigurationUiQuery.wsd`、`CONTEXT.md`。
- 前置：阶段1完成规定收口并获负责人确认；依赖阶段1标准目录和目标表的真实集成测试不得提前执行。

## 已确认设计

互认项目配置引用标准项目编码，不重复保存标准项目名称、类型、分类或分组。新增公共 Request 只提交 `StandardProjectCode` 和 `RecognitionDurationDays`；组织编码由服务端可信组织上下文确定，服务端按编码取得 `StandardItemId`，不接受客户端提交内部 ID。

同一组织编码和标准项目编码只能有一条配置，停用配置仍占用唯一键。创建后默认启用；普通修改只修改可互认时间；启用和停用为独立操作，重复同方向操作幂等成功且不登记事件。目录停用时保留配置，允许修改时间和停用配置；重新启用要求标准项目、分类和分组当前有效。

可互认时间严格按 SRS：单位为天，只允许正整数，最小值为1；空值、零、负数、小数和永久有效均拒绝。匹配时从报告时间起按连续24小时计算，经过时间小于等于 `RecognitionDurationDays × 24小时` 时有效，边界包含。

查询先校验请求组织属于调用方可信业务范围，再按请求组织过滤，关联统一标准目录取得展示资料和 `UnavailableReason`。目录全部启用时该字段为 `null`，即使配置自身停用也如此；多层停用按分类、分组、标准项目顺序返回一个原因。配置自身停用由 `ConfigurationStatus` 表达。查询不新增名称、类型、分类、分组筛选，不分页，按标准项目编码升序返回。标准项目编码在同一组织配置内唯一，不设计同编码或相同 ID 的排序测试数据。

不分页风险统一见 S2-D7。阶段1正在实施，本设计不评定其当前进度或运行结论；进入依赖阶段1的验证时再核对当时证据。

## 关键决策

| ID | 决策 | 状态 | 依据 | 适用范围 | 待验证面 |
|---|---|---|---|---|---|
| S2-D1 | 查询和写入均校验可信组织及配置归属；不另建角色授权 | DesignConfirmed | SRS F02参与者与前置条件；讨论决策1、10 | Application、Query | V11/V19/V22/V27 |
| S2-D2 | 创建 Request 不提交组织编码和项目 ID；Command 保留服务端补入值；查询 Request 保留组织编码 | DesignConfirmed | 讨论决策8、28、31；UML1 Command/UML2 Query | 契约、映射 | V21、C23 |
| S2-D3 | 不重复保存目录名称、类型、分类和分组 | DesignConfirmed | SRS F02业务规则1；UML1 MutualRecognitionItemEntity | Entity、Query | V12 |
| S2-D4 | 组织编码与标准项目编码唯一，停用仍占用 | DesignConfirmed | SRS F02备选流3与关键属性；讨论决策47 | DDL、Domain | V2/V15 |
| S2-D5 | 不建 StandardItemId 外键 | DesignConfirmed | 讨论决策48；阶段2整改矩阵的正式落点 | DDL | V18/V25 |
| S2-D6 | 本阶段每个普通命令最多一条写语句，不声明 WorkUnit；用户限定跨表原子写入或 FacadeCommand 才使用 | DesignConfirmed | 讨论决策49、50；本阶段写用例事务表 | 四个写入口 | V24；若扩展同表多写不得沿用本结论，先设计确认 |
| S2-D7 | 不分页；出现可复现卡顿、超时或内存问题才重审，不设任意记录数阈值 | AcceptedRisk | 讨论决策6；Client已接受风险段 | Query、Client | C25 |
| S2-D8 | 不返回两个派生状态字段；原因仅表达目录停用，null表示目录全部启用 | DesignConfirmed | 讨论决策4、13、26、33、52、54；UML2 ReadModel | UML、ReadModel | V13/V20/V21 |
| S2-D9 | 页面和配置查询不展示/返回创建及修改人员和时间；持久化仍保留操作字段 | DesignConfirmed | 讨论决策24；SRS F02页面要求与关键属性 | SRS、Client | C23 |
| S2-D10 | 进入自动刷新目录并提供手动刷新 | DesignConfirmed | SRS F02页面要求；讨论决策42 | Client | C13/C14 |
| S2-D11 | 本模块显式实体 scope 和业务 sqlId，不使用 SetContext；运行键需取证 | DesignConfirmed | 阶段1设计的作用域修法；项目生成 Repository/SqlMap 现状 | Repository、SqlMap | V23；不预判阶段1探测结果 |
| S2-D12 | 可信当前组织、可选组织集合、所选组织传递机制及非开发环境代理配置待核实 | Pending | 当前缺少已实现的组织解析链 | 组织解析及依赖它的真实端到端实现 | V22；缺证据不猜 Claims/请求头 |
| S2-D13 | 无显式 WorkUnit 的生产入口如何处理事件及失败响应待核实 | Pending | 框架证据要求 | 事件集成及写入口联调 | V24；不得用带 WorkUnit 的替代入口证明 |
| S2-D14 | 原因优先级为分类、分组、标准项目；不纳入删除场景 | DesignConfirmed | 讨论决策52；SRS F02页面要求；UML2 ReadModel说明 | 查询说明 | V20 |

## 验收与停止条件

- 后端四个命令、查询契约和领域规则有完整测试证据。
- API Client 按累计规则生成并通过契约、typecheck 和 build。
- 页面从宿主登录和菜单进入完成真实验收。
- 目标数据库/schema 未确认或表未建成时，数据库集成记 `Blocked`；不以空列表、构建成功或接口直测替代业务通过。
- 阶段1未收口时，阶段2可继续设计和独立静态验证，不进行依赖标准目录的真实验收。
- S2-D12/S2-D13 只阻断依赖未核实机制的实现；独立 DDL、类型契约及组件设计可以继续。整改文档通过检查不等于这两项已解除。
