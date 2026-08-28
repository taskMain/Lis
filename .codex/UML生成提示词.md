请基于以下已经评审通过的 SRS，使用 dy-puml 技能生成公司规范 UML 图。

SRS 文件：
[待建模需求规约.md](path/to/srs.md)

流程图：
[待建模流程图.md](path/to/flow.md)（如有）

参考公司 UML 写法：
[参考UML](docs/参考UML/)

一、执行顺序

1. 第一阶段：仅输出建模思路，不生成、不修改 .wsd 文件。
2. 第一阶段输出必须采用以下结构化模板：
   - 聚合识别表
   - 命令-聚合/实体覆盖矩阵
   - DomainEvent 属性契约检查表
   - 实体/结构来源表
   - 查询分类表
   - 查询图结构检查表
   - 命令粒度与批处理判定表
   - Entity 覆盖检查表
   - 字段语义精确性检查表
   - 不建模项和待确认问题
3. 第一阶段输出必须得到用户明确确认，用户回复“确认”后，方可进入第二阶段生成 .wsd 文件。
4. 若用户提出修改意见，须在第一阶段内迭代，直到用户确认后再进入第二阶段。
5. 第二阶段：依据第一阶段已确认的思路，逐个生成对应的 .wsd 文件。
6. 生成前必须先输出“生成前检查表”。
7. 生成后必须先输出“生成后自检结果”，再结束任务。

二、公司 UML 语法和聚合边界要求

1. 只能使用 ddd_lib.puml 中已定义的元素：
   - Person
   - UserInterface
   - Command
   - FacadeCommand
   - DomainEvent
   - Policy
   - Query
   - ReadModel
   - Aggregate
   - Entity
   - Enum
   - Comment
2. 不允许使用 ddd_lib.puml 中未定义的元素。
3. `Aggregate` 用于表达一致性边界及边界自身持有的状态；`Entity` 用于表达边界内具有独立标识并承载可修改业务状态的对象。
4. 聚合边界与核心 Entity 可以分开表示。不得为了避免空 Aggregate 而把属于 Entity 的属性上移到 Aggregate，也不得在没有独立实体语义时机械拆出同名 `XxxEntity`。
5. 如果聚合边界自身持有业务状态，使用 `Aggregate('XxxAggregate')[ ... ]` 展开这些属性；如果 Aggregate 仅表示边界，可以不承载 Entity 的业务属性。
6. 不允许在 Aggregate 内增加 `RootEntity`、`根实体`、`聚合根实体` 等伪字段。
7. Entity 只用于聚合内部具有业务含义且需要真实持久化的业务实体、明细实体、历史记录实体或结构化组成部分。
8. 命令必须连接实际拥有并发生状态变化的 Aggregate 或 Entity，不得仅为形式完整而连接 Aggregate。
9. 推荐关系结构为：`Command -> Entity -> Aggregate`。只有命令真正修改聚合边界自身状态时，才直接连接 Aggregate。
10. FacadeCommand 必须连接其实际主目标 Aggregate 或 Entity，不能悬空；是否使用 FacadeCommand 由协调职责决定，而不是由数组入参、目标实例数量或批量执行方式决定。
11. 查询图中如果需要简化引用聚合，必须使用双参数写法：
    `Aggregate('XxxAggregate','中文聚合名')`
    例如：
    `Aggregate('XxxAggregate','xxx聚合')`
    不允许在查询图中使用无块单参数写法：
    `Aggregate('XxxAggregate')`
    因为当前 ddd_lib.puml / plantuml-libs 组合下会展开为未闭合结构并导致渲染失败。
12. 查询图中的 Aggregate 只作为查询目标简化引用，不展开属性、不包含 Entity、不表达聚合内部结构。

三、建模边界

1. 聚合图只表达聚合内部状态变更、命令、事件、必要策略、聚合根和内部实体。
2. 聚合边界优先按强一致事务边界划分：同一业务入口中必须共同成功或共同失败的状态变更、记录创建、明细追加和状态推进，应放在同一个聚合内表达。
3. 需要在同一事务中完成的主业务流程，不允许使用 DomainEvent + Policy 串联；应使用 Command 或 FacadeCommand 直接表达同事务编排。
4. 如果一个命令必须同步修改多个聚合的核心状态，应优先重新审视聚合拆分是否错误；只有业务允许最终一致、失败不回滚主业务结果或属于旁路通知时，才使用跨聚合事件和 Policy。
5. 不要把跨聚合查询、UI 查询、外部系统查询混入聚合图。
6. UI 查询、外部 API 查询、外部系统查询必须拆成单独的查询图。
7. 内部验证查询不画成 Query，除非它是根据 ID 查询。
8. 根据 ID 查询必须显式说明：
   - 是内部聚合加载使用，不生成 API；
   - 还是 UI/API 明确需要，生成接口。
9. 查询图中 UI 查询以模块为边界建模，不以页面为边界建模。
   使用：
   `UserInterface('XxxModule','xxx模块')`
10. 外部系统查询图必须单独绘制，参与者使用 `Person('XxxSystem','xxx外部系统')`。
11. 平台内部校验、唯一性校验、权限范围校验、外部配置有效性校验等后端业务规则，只在规则中说明，不画成 Query。
12. 前端 UI 层面的验证查询，如示例预览、格式校验、唯一性预检查，可放入 UI 查询图，并注明“前端验证，不生成 API”。

四、复杂对象、快照、列表和读模型

1. 不允许将复杂业务对象、列表、清单、明细、快照、结构化结果直接建模为 Text。
2. 如果 SRS 中出现“列表”“清单”“对象”“信息”“明细”“结构化结果”“快照”等字段，必须追溯关键属性或接口字段规则。
3. 聚合内部真实持久化结构可拆为 Entity。
4. 查询返回结构应优先拆为 ReadModel。
5. 只有 SRS 明确说明为自由文本说明、备注、原因描述时，才允许使用 Text；String 可用于编码、名称、单号、业务标识、外部流水号等普通标量字段，但不得用于有限值域、状态、类型、方式、结果等业务选择项的兜底表达。
6. Text 不能作为复杂对象兜底类型。
7. 命令入参中出现数组、对象、列表、清单、明细、快照、结构化结果时，必须说明该结构的落点：
   - 如果会真实持久化为聚合内部业务表、明细表、历史表，则由 Aggregate 或 Entity 承接；
   - 如果只是查询返回结构，则由 ReadModel 承接；
   - 如果只是命令入参 DTO、创建项 DTO、列表项 DTO，不需要额外创建 Entity。
8. 不允许为了满足“复杂入参有结构承接”而创建不建表的伪 Entity。
9. 命令入参可以直接保留结构类型标识，例如：
   `明细列表(Items XxxCreateItem[])`
   但不要额外创建：
   `Entity('XxxCreateItemEntity')`
   除非该结构本身对应真实持久化表。
10. 如果复杂入参拆解后落到真实业务表中，应由真实表对应的 Aggregate / Entity 承接，例如主体表、明细表、审核记录表、终止记录表、状态历史表等。
11. 如果某个结构只是读模型字段，不应放入聚合图，应放入查询图并使用 ReadModel。
12. 不允许只在命令中写 `明细列表(Items Xxx[])`，却不说明 `Xxx` 的字段来源和放置位置。
13. 不允许将具有有限值域、业务选择项、状态流转含义或定位分支含义的字段笼统建模为 String。
14. 字段中文名包含“方式、类型、状态、结果、来源、模式、类别、动作、结论、处理方式”等，或英文名包含 `Mode`、`Type`、`Status`、`State`、`Result`、`Source`、`Category`、`Kind`、`Action`、`Conclusion` 等时，必须回溯 SRS 判断是否为有限值域。
15. 如果 SRS 已明确值域，必须建模为 Enum，并使用具体业务命名，例如：
   - 使用 `处理方式(ProcessMode XxxProcessMode)`
   - 不使用 `处理方式(ProcessMode String)`
16. 如果 SRS 未明确完整值域，但字段明显是业务选择项，应列入待确认问题，不要强行补枚举值，也不要直接用 String 掩盖语义。
17. 编码、名称、单号、业务标识、外部流水号等普通标识字段可以保留 String，但命名必须明确为 `XxxCode`、`XxxName`、`XxxNo`、`XxxKey`、`XxxSerialNo` 等，不要命名为过泛的 `XxxType`、`XxxMode`。
18. 自由文本说明、备注、描述、原因补充说明可以使用 String 或 Text，但必须能从 SRS 明确追溯为自由文本。
19. 自检时必须扫描所有 `Mode`、`Type`、`Status`、`State`、`Result`、`Source`、`Category`、`Kind`、`Action`、`Conclusion` 类型字段，说明保留 String、改为 Enum 或列为待确认的理由。

五、实体和结构来源约束

1. 每个 Aggregate、Entity、ReadModel 都必须能追溯来源：
   - 来源命令
   - 来源事件
   - 来源查询
   - 或 SRS 明确的接口字段
2. 聚合内部 Entity 必须与命令、事件或聚合图内明确需要的内部查询有关联；UI/API/外部系统查询图中的 Query 禁止连接 Entity，只能连接 ReadModel。
3. 如果某个结构只是读模型字段，不应放入聚合图，应放入查询图并使用 ReadModel。
4. 如果某个对象没有状态变更、没有查询来源、没有接口字段依据，应列为不建模项。
5. 不要为了满足“覆盖”而给快照、创建项、读模型强行补充 SRS 没有确认过的命令。
6. 聚合图中每个 Entity 都必须至少满足以下一种关系：
   - 被一个 Command 或 FacadeCommand 直接连接；
   - 被聚合图内明确需要的内部 Query 直接连接；
   - 被另一个已经被 Command/Query 覆盖的 Entity 直接连接，并在规则中说明其结构来源。
7. 如果 Entity 只是通过 Aggregate 关联，但没有任何 Command、FacadeCommand、Query 或上级 Entity 连接，则视为无效建模，应删除或改为 ReadModel。
8. 创建类业务入口即使包含数组或多个结构化组成部分，也不能仅据此判定为 `FacadeCommand`；只有入口确实需要协调多个具有独立业务职责的 Command 时，才使用 `FacadeCommand`。
9. 修改、处理、确认、审核、作废、归档类入口即使影响多个同类 Entity 实例，也可由一个普通 Command 完成并直接连接该 Entity 类型。只有其中存在可独立命名、独立触发或独立复用的职责时，才拆分内部 Command 并由 FacadeCommand 协调。
10. 不允许为了展示字段而创建 Entity。只有当该结构来自明确命令入参、事件事实、持久化历史记录或真实持久化结构时，才允许建为 Entity；查询返回结构应建为 ReadModel。
11. 如果一个结构只用于列表、详情或接口返回，不参与聚合状态变更，则不得放入聚合图，应放入查询图并建为 ReadModel。
12. 聚合图不是字段清单图，而是状态变更覆盖图。任何 Entity 留在聚合图里，都必须能回答“哪个命令创建、修改、追加或关闭了它”。

六、业务命名要求

1. 命名必须符合当前 SRS 的业务语义，不直接照搬 SRS 中过于泛化、口语化或上下文不完整的名称。
2. 需要结合术语定义、关键属性和流程节点，为聚合、实体、命令、事件、查询和读模型选择稳定、清晰、可追溯的英文命名。
3. 业务对象英文名应优先表达领域概念，而不是页面模块、接口路径、数据库表名或技术实现名。
4. 不得新增 SRS 未确认的业务规则。
5. 只有批量行为具有独立领域语义时，批量命令才使用复数名词，例如 `ApproveXxxItems`、`TerminateXxxItems`、`DispatchXxxItems`。
6. 如果 SRS 已定义标准目录、主数据、配置项或外部系统术语，必须沿用该术语口径，不要混用旧名、技术名或自造同义词。
7. 编码、名称、单号、业务标识、外部流水号等普通标识字段应使用明确后缀，例如 `XxxCode`、`XxxName`、`XxxNo`、`XxxKey`、`XxxSerialNo`。
8. 同一概念在 Aggregate、Entity、Command、Event、Query、ReadModel 中必须保持同一英文词根，避免一个业务对象出现多套英文命名。
9. 命名必须描述实际业务动作：依据权威外部数据使本地状态与其保持一致时使用 `Synchronize`；只有涉及差异识别、双向比较、人工核对或冲突调和时才使用 `Reconcile`。

七、查询分类规则

请把查询分为五类说明：

1. 聚合内部加载查询：
   - 仅用于命令执行前加载聚合；
   - 不生成 API；
   - 根据 ID 的内部聚合加载查询必须在查询分类表或规则说明中显式列出并标注“不生成 API”；
   - 默认不画成 Query；只有 UI/API 明确需要生成接口，或公司参考图中同类聚合明确画了内部 Get 查询时，才画成 Query。
2. 内部验证查询：
   - 例如唯一性校验、协作关系有效性校验、当前有效配置校验；
   - 只在规则说明中描述；
   - 不画成 Query；
   - 不生成 API；
   - UI 层面的后端验证归属 UI 查询，并注明“前端验证，不生成 API”；
   - 后端业务规则验证归属内部验证查询。
3. UI 模块查询：
   - 单独绘制 UI 查询图；
   - 使用 UserInterface 表示模块；
   - 查询连接到模块；
   - 必要时使用 ReadModel 表达返回结构。
4. 外部 API 查询：
   - 单独绘制外部 API 查询图；
   - 使用 Person 表示外部调用系统；
   - 查询必须明确是否生成 API；
   - 复杂返回结构使用 ReadModel，不使用 Text。
   - 不允许使用 `QuerySide String`、`查询方类型` 等字段把不同调用方视角混合在同一个 Query 中。
   - 如果同一业务查询存在多个调用方视角，并且入参、权限归属校验、定位方式、返回字段、可见字段范围或 API 契约语义任一不同，必须拆成不同 Query。
   - 不同调用方侧 Query 使用能表达调用方角色的命名，例如 `QueryActorAXxxApi`、`QueryActorBXxxApi`，并且只连接对应外部调用系统。
   - 如果不同调用方视角返回内容不同，应拆分 ReadModel；只有返回结构完全一致时，才允许复用同一个 ReadModel。
5. 外部系统查询：
   - 单独绘制外部系统查询图；
   - 使用 Person 表示被查询的外部系统；
   - 查询表示协同平台调用外部系统的读取能力；
   - 复杂返回结构使用 ReadModel，不使用 Text。

八、查询 UML 图结构规则

1. 查询图只表达 UI 模块、外部调用方、外部依赖系统、Query、ReadModel、必要的查询目标 Aggregate 简化引用。
2. UI 查询图必须以模块为边界，不以页面为边界：
   `UserInterface('XxxModule','xxx模块')`
3. 每个 UserInterface 必须至少连接一个 Query。
   - 如果某个 UI Module 属于当前查询图业务范围，应补充它实际需要的 Query；
   - 如果 SRS / 流程图没有查询依据，不要强行补 Query，应列入待确认问题；
   - 不允许声明只有用户连接、但没有任何 Query 连接的 UI Module 孤岛。
4. 外部 API 查询图、外部系统查询图中的 Person 必须至少与一个 Query 有关系。
   - Person 可以表示外部调用系统、外部依赖系统或查询发起方；
   - 如果 Person 完全没有 Query 关系，应删除或列入待确认；
   - 不允许出现没有查询关系依据的外部参与方孤岛。
5. Query 到 ReadModel 的连线表示“该查询的主返回结构”。
   - 一个 Query 默认只直接连接一个主 ReadModel；
   - 不要把 Query 同时直接连接主 ReadModel 和其内部子 ReadModel；
   - 子 ReadModel 通过主 ReadModel 的字段表达结构关系。
6. 如果查询返回详情结构，例如“订单详情”“申请详情”“记录详情”，应建模为一个主 ReadModel：
   `QueryXxxDetail .. XxxDetailReadModel`
   然后在 `XxxDetailReadModel` 字段中引用子结构：
   `基础信息(BaseInfo BaseInfoReadModel)`
   `明细列表(DetailItems XxxDetailItemReadModel[])`
7. 子 ReadModel 可以没有单独关系线，只要它被主 ReadModel 字段引用即可。
   不要为了让每个 ReadModel 都有连线而添加多余的 Query -> 子 ReadModel 关系。
   子 ReadModel 的来源可以是主 ReadModel 字段引用加 SRS/接口字段来源，不要求单独 Query 连线。
8. 只有当 SRS 明确说明一个 Query 返回多个并列结果集时，才允许一个 Query 直接连接多个 ReadModel。
   如果不确定是否并列结果集，应列为待确认，不要强行建模。
9. 查询图中的 Aggregate 只作为查询目标简化引用，不表达聚合内部结构。
   必须使用双参数写法：
   `Aggregate('XxxAggregate','xxx聚合')`
10. 查询图不允许使用带内容块的聚合根定义，不要在查询图中展开 Aggregate 属性、Entity、Command、DomainEvent。
11. 查询图中的 Query 不连接 Entity；查询返回结构统一使用 ReadModel。
12. 查询图中所有关系声明必须在对象定义之后，避免 PlantUML 引用未定义对象。
13. UI 查询图、外部 API 查询图、外部系统查询图必须分图，不要混画。
14. 外部 API 查询图中，不允许使用 `QuerySide String`、`查询方类型` 等字段混合表达不同调用方视角。
15. 当 SRS 中同一业务能力同时支持多个调用方视角查询时，应优先拆分为多个 Query：
    - `QueryActorAXxxApi`
    - `QueryActorBXxxApi`
16. 拆分后的 Query 应分别表达各自接口契约：
    - 调用方 A 侧 Query 使用调用方 A 的归属字段；
    - 调用方 B 侧 Query 使用调用方 B 的归属字段；
    - 不同调用方应使用各自业务归属字段和定位字段；
    - 不允许通过 `QuerySide` 在一个 Query 内切换入参规则或返回结构。
17. 如果不同调用方视角返回内容不同，应拆分 ReadModel；如果返回结构完全一致，才允许复用同一个 ReadModel。
18. 拆分后的 Query 仍必须遵守主返回模型规则：
    - 每个 Query 只直接连接一个主 ReadModel；
    - 子 ReadModel 通过主 ReadModel 字段引用；
    - 不要让 Query 直接连接多个内部子 ReadModel。

九、命令目标、事件和策略规则

1. `Command` 表示一个完整、稳定的业务意图，并连接实际拥有被修改状态的一个 Aggregate 或一个 Entity 类型。
2. 同一个普通 Command 可以创建或修改多个相同类型的 Entity 实例；实例数量、数组入参或循环执行不改变其 Command 身份。
3. 普通 Command 不应同时连接多个不同职责的 Aggregate / Entity。若一个入口确实需要编排多个具有独立业务职责的 Command，才使用 `FacadeCommand`。
4. FacadeCommand 的判断依据是协调职责，不是入参结构、持久化结构数量、目标实例数量或是否批量。
5. `FacadeCommand` 必须直接连接一个实际主目标 Aggregate / Entity，并通过关系协调一个或多个具有独立职责的内部 Command。
6. 内部 Command 至少应满足以下一项：
   - SRS 明确它是独立业务动作；
   - 它有独立入口、使用者或触发来源；
   - 它产生需要独立追溯或被其他策略监听的领域事件；
   - 它可被其他入口或 FacadeCommand 复用；
   - 它承担与 FacadeCommand 主职责不同、可独立命名的业务职责。
7. 如果 FacadeCommand 没有协调任何满足上述条件的内部 Command，应改为普通 Command。
8. 不要为了形式上的“一命令一实体实例”机械拆出单项 Command，也不要为了让批量 FacadeCommand 逐项调用而制造没有独立入口、业务含义或复用价值的伪命令。
9. 如果同一个业务动作既改变 Entity 的多项状态，又追加其明细或历史记录，但这些变化共同构成一个不可分割的业务职责，可以由一个普通 Command 表达；不要仅因落到多个持久化结构就机械拆分。
10. 如果确实存在多个独立职责，应建模为：
    - `FacadeCommand .. 主目标 Aggregate/Entity`
    - `FacadeCommand --> 独立职责 Command`
    - `独立职责 Command .. 其实际状态目标`
11. 命令连线表达状态所有权：优先使用 `Command -> Entity -> Aggregate`；只有命令真正改变 Aggregate 自身状态时才直接连接 Aggregate。
12. 每个 Command 必须触发 DomainEvent。
13. FacadeCommand 作为业务入口时也必须触发 DomainEvent。即使它同时编排内部 Command，也应发送入口业务结果事件；内部 Command 是否发送独立事件，根据是否存在独立事实追溯或公司强制规则判断。
14. Command 与 DomainEvent 使用：
   `Command ..> DomainEvent : send`
15. DomainEvent 表达已经发生的业务事实，不是 Command 入参的机械复制。
16. 不允许定义“事件默认继承命令全部属性”的隐式规则；事件属性必须按事实契约显式判断。
17. 需要显式展开属性的事件包括：
    - 被外部聚合 Policy 监听的跨聚合事件；
    - 会驱动平台消息、外部推送、查询投影或读模型更新的事件；
    - 会被其他聚合用于定位业务对象、创建记录或推进状态的事件；
    - 需要作为审计追溯、状态回放或业务快照依据的事件。
18. 显式 DomainEvent 的属性不是 Command 入参全集，而是该业务事实对状态回溯、跨聚合定位、下游投影、消息推送和审计追溯所需的最小稳定契约。
19. 显式事件必须优先包含以下字段：
    - 业务定位键，例如 XxxNo、XxxCode、BusinessKey、OwnerOrgCode；
    - 归属字段，例如 OwnerOrgCode、OwnerUnitCode、SourceOrgCode、TargetOrgCode；
    - 事实结果字段，例如 ReviewResult、ReportStatus、ReceiptResult、ProcessResult；
    - 事实发生时间，例如 ReviewTime、UploadedTime、CompletedTime、ProcessedTime；
    - 下游消息、推送、查询投影明确需要的业务快照字段。
20. 以下字段不应机械写入 DomainEvent：
    - 仅用于命令执行控制的临时字段；
    - 内部重试次数、技术错误堆栈、认证上下文、权限上下文；
    - 平台内部 ID 或内部存储路径等不应外泄的实现字段；
    - 下游可由业务键稳定查询得到且不要求事件快照化的字段。
21. 如果事件只用于本聚合内部状态追溯，且没有跨聚合 Policy、消息、推送或查询投影消费，可以使用简写事件：
    `DomainEvent('XxxEvent','xxx已发生')`
    但必须在命令或事件规则中说明“该事件仅用于聚合内部状态追溯，不作为跨聚合事件契约”。
22. 如果简写事件后续被跨聚合 Policy、平台消息、外部推送或查询投影消费，必须改为带属性块的显式事件。
23. 不要为了让事件字段和命令字段完全一致而补充 SRS 未确认的业务字段；事件字段必须能从 SRS、PRD、流程图、下游消费关系或聚合状态回溯需要中找到依据。
24. 自检时必须区分：
    - 简写内部事件是否确实无跨聚合或投影消费；
    - 显式事件字段是否覆盖下游定位、状态推进、消息、推送和查询投影需要；
    - 是否存在事件机械复制命令执行控制字段或内部实现字段。
25. 同一聚合内部的协调动作使用 FacadeCommand，不使用 Policy。
26. Policy 只用于外部聚合事件触发本聚合命令。
27. 不允许补充 SRS 没有确认过的业务规则、状态、事件或命令。
28. 修改类命令必须包含 ID，并用 `..` 分隔 ID 和其他入参。
29. 创建类命令必须展开关键创建项结构，不能把“列表”“对象”“快照”写成 Text。
30. 命令目标连接规则：
   - 普通 `Command` 连接一个实际状态所有者类型；
   - 同类 Entity 的多实例变更仍连接该 Entity 类型；
   - 只有创建或修改 Aggregate 自身状态时才连接 Aggregate；
   - 只有需要协调多个独立职责时才使用 FacadeCommand，并通过内部 Command 表达这些职责。
31. 不允许命令只连接 Aggregate 而遗漏实际拥有状态的 Entity；也不允许仅因一个业务动作落到多个表或多个同类实例就强行拆出 FacadeCommand 和伪内部 Command。
32. FacadeCommand 拆分后的事件策略：
   - 如果 SRS 只定义了一个业务结果事件，优先保留在入口 FacadeCommand 上，由 FacadeCommand 发送该业务结果事件；
   - 内部 Command 是否发送独立 DomainEvent，必须根据 SRS 是否要求独立追溯事实判断；
   - 如果 SRS 未确认内部事实事件，不要为内部 Command 强行新增业务事件，应在自检中列为“内部 Command 事件策略待确认”；
   - 如果公司强制要求每个 Command 都有 DomainEvent，则内部 Command 可使用最小事实事件，但必须标注事件仅用于聚合内部状态追溯，不新增对外业务语义。
33. 如果一个事件由多个命令触发，必须在事件旁或自检中注明“此事件由多个命令触发”。
34. 每个 Command / FacadeCommand 都必须至少被一个明确使用者连接。
35. 使用者可以是 Person、UserInterface、Policy、FacadeCommand 或上游 DomainEvent 触发的 Policy。
36. 不允许命令只有 `Command ..> DomainEvent : send`，但没有任何人或机制触发。
37. 如果命令是系统内部自动命令，必须通过 Policy、FacadeCommand 或明确的触发事件表达，不要悬空。
38. 如果 SRS 没有明确使用者或触发来源，该命令应列为待确认，不要强行保留。

十、命令粒度与批处理规则

1. UML 优先表达稳定的单项业务意图，不因框架能够批处理、命令入参为数组或一次处理多个实例而自动改成批量命令或 FacadeCommand。
2. 如果框架只是对同一个单项 Command 重复调度，且批处理没有独立权限、事务、校验、失败处理、返回结果或事件语义，只建模单项 Command，不重复展开批量入口和批量事件。
3. 只有批量行为本身具有独立领域语义时才建模批量命令，例如：
   - 批量操作要求整体成功或整体失败；
   - 批量入口有独立权限、跨项校验、数量上限或去重规则；
   - 批量操作允许部分成功并返回逐项结果；
   - 批量操作产生独立领域事实；
   - 批量能力是用户可感知且与单项不同的业务入口。
4. 独立批量命令使用复数命名，例如：
   - `ApproveXxxItems`
   - `TerminateXxxItems`
   - `ConfirmXxxItems`
   - `WithdrawXxxItems`
5. 批量命令的入参使用集合字段表达操作对象，例如：
   - `ID列表(Ids Guid[])`
   - `业务对象标识列表(ItemKeys String[])`
   - `处理项列表(Items XxxProcessItem[])`
6. 只有以下情况才允许同时保留单项命令和批量命令：
   - SRS 明确说明单条和批量入口不同；
   - 单条和批量权限不同；
   - 单条和批量触发不同领域事件；
   - 单条和批量返回结果或失败处理不同；
   - 单条操作允许部分成功，但批量要求整体成功，或反之；
   - 单条和批量有不同的必填字段或业务规则。
7. 批量命令事件使用集合事件泛化表意，例如：
   - `XxxItemsApprovedEvent`
   - `XxxItemsTerminatedEvent`
   - `XxxItemsDispatchedEvent`
8. 不要求独立批量命令为每个个体建立单独事件。
9. 如果 SRS 只要求逐条记录结果，应在命令规则中写明“逐条校验、逐条记录结果”。
10. 一个普通 Command 批量修改多个相同类型的 Entity 实例时，仍直接连接该 Entity 类型，不需要外观命令逐项调用伪单项命令。
11. 自检时必须输出“命令粒度与批处理判定表”，列出：
    - 业务意图
    - 单项 Command 候选
    - 是否仅由框架批处理
    - 批量行为是否具有独立领域语义
    - 最终保留的 Command / FacadeCommand
    - 保留、合并或删除原因

十一、文件要求

1. 一个聚合一个 .wsd 文件。
2. UI 查询、外部 API 查询、外部系统查询分别一个或多个单独 .wsd 文件。
3. 每个文件开头使用中文注释说明：
   - 聚合根或查询图内容
   - 业务范围
   - 是否包含 API 查询
4. 文件使用 CRLF 换行。
5. `@startuml` 名称使用短名称，例如：
   `@startuml Xxx`
6. 聚合命名使用：
   `Aggregate('XxxAggregate')[ ... ]`
7. 查询图聚合简化引用使用：
   `Aggregate('XxxAggregate','xxx聚合')`
8. 文件中关系声明应在对象定义之后，避免引用未定义对象。
9. 不要主动提交代码，不要自动执行 git commit。

十二、生成前必须输出的检查表

生成 .wsd 前，请先输出：

说明：第一阶段输出的结构化表格即为生成前检查表；用户确认后进入第二阶段时可直接按已确认检查表生成，除非用户要求重新输出检查表。

1. 聚合识别表：
   - 聚合名
   - 聚合根
   - 是否核心业务实体
   - 生命周期依据
   - 对应主流程节点
2. 命令-聚合/实体覆盖矩阵：
   - 命令
   - 操作目标 Aggregate/Entity
   - 触发事件
   - 来源 SRS/流程节点
3. 实体/结构来源表：
   - 名称
   - 类型：Aggregate / Entity / ReadModel / 不建模
   - 来源命令/事件/查询
   - 是否有独立生命周期
   - 放置位置
4. 查询分类表：
   - 查询名
   - 查询类型：内部加载 / 内部验证 / UI 查询 / 外部 API 查询 / 外部系统查询
   - 是否生成 API
   - 是否根据 ID 查询
   - 放置图文件
5. 查询图结构检查表：
   - 查询图文件
   - UserInterface / Person
   - 是否连接 Query
   - 是否存在孤岛
   - Query
   - 主 ReadModel
   - 子 ReadModel
   - 子 ReadModel 是否通过主 ReadModel 字段引用
   - 是否存在 Query 直连多个 ReadModel
   - 外部 API Query 是否混用了 QuerySide / 查询方类型
   - 是否应按不同调用方视角拆分 Query
   - 拆分后的 Query 调用方是否唯一明确
   - 不同调用方视角 ReadModel 是否需要拆分
   - 是否需要待确认
6. 命令粒度与批处理判定表：
   - 业务意图
   - 单项命令候选
   - 批量命令候选
   - 是否仅由框架批处理
   - 批量行为是否具有独立领域语义
   - 最终保留命令
   - 保留、合并或删除原因
7. Entity 覆盖检查表：
   - Entity 名称
   - 来源命令/事件/查询
   - 连接它的 Command / FacadeCommand / Query
   - 是否允许保留
   - 如删除或改为 ReadModel，说明原因
8. 字段语义精确性检查表：
   - 文件
   - 元素类型：Command / Aggregate / Entity / ReadModel / Query / Event
   - 元素 ID
   - 字段
   - 当前类型
   - 是否有限值域
   - 是否应建 Enum
   - SRS 依据
   - 处理动作：保留 String / 改 Enum / 改名 / 待确认
9. 命令目标拆分检查表：
   - 文件
   - 业务入口
   - 当前建议元素类型：Command / FacadeCommand
   - 实际状态所有者 Aggregate / Entity
   - 是否只批量修改同类 Entity 实例
   - 是否存在多个具有独立职责的 Command
   - FacadeCommand 主目标 Aggregate / Entity
   - 需要拆出的内部 Command
   - 每个内部 Command 的唯一目标
   - 内部 Command 目标是否等于 FacadeCommand 主目标
   - 如果目标相同，是否有独立业务语义 / 独立事件 / 可复用依据
   - 是否应删除重复内部 Command
   - 是否存在仅为逐项调用而拆出的伪 Command
   - 是否存在待确认事件策略
10. DomainEvent 属性契约检查表：
   - 文件
   - DomainEvent
   - 触发 Command / FacadeCommand
   - 是否跨聚合消费
   - 是否驱动消息 / 推送 / 查询投影
   - 是否需要显式属性
   - 必须包含的业务定位键
   - 必须包含的事实结果字段
   - 必须包含的时间字段
   - 可不写入事件的命令入参
   - 简写或显式展开理由
11. 不建模项和待确认问题：
   - SRS 未确认的规则
   - 接口文档才应定义的字段
   - 过泛描述
   - 不应进入领域模型的技术细节

十三、生成后自检

生成完成后必须逐项检查：

1. Aggregate 是否准确表达一致性边界；若不承载状态，是否避免为了填充 Aggregate 而上移 Entity 属性。
2. 是否在没有独立实体语义时机械创建了与 Aggregate 同名的 XxxEntity；或者反过来，把实际属于 Entity 的状态错误放入 Aggregate。
3. 是否存在 RootEntity 或根实体伪字段。
4. 是否存在 Text 兜底复杂对象。
5. 是否存在没有来源命令、事件或查询的 Entity/ReadModel。
6. FacadeCommand 是否连接到了 Aggregate 或 Entity，并发送入口业务结果 DomainEvent；如果同时编排内部 Command，内部 Command 是否按独立事实追溯需要发送事件。
7. 每个 Command / FacadeCommand 是否都有 DomainEvent。
8. 每个 Command / FacadeCommand 是否都有明确使用者或触发来源。
9. 根据 ID 查询是否显式说明内部使用或 API 使用。
10. UI 查询、外部 API 查询、外部系统查询是否分图。
11. 是否补充了 SRS 没有确认的规则。
12. 是否符合参考图的公司写法。
13. 命令与事件关系是否标注了多对一映射，如果适用。
14. 每个 Entity 是否至少有一个 Command、FacadeCommand、Query 或上级 Entity 连接。
15. 每个创建入口是否覆盖它创建的所有聚合内部结构：
    - 是否连接实际拥有被修改状态的 Aggregate / Entity；
    - 如果批量影响同类 Entity 实例，是否仍由一个普通 Command 表达；
    - 如果确实协调多个独立职责，是否使用 FacadeCommand + 内部 Command 表达。
16. 是否存在只挂 Aggregate、没有命令/查询覆盖的 Entity。
17. 是否把框架批处理重复建模为批量入口、单项 Command 和多余事件；是否错误删除了应优先表达的单项业务意图。
18. 独立批量命令是否具有明确领域语义、使用集合入参，并说明整体成功/整体失败或逐条校验、逐条记录结果。
19. 命令入参中的复杂结构是否都有真实落点说明，且没有为纯入参 DTO 创建伪 Entity。
20. Entity 是否被错误用于纯展示 DTO；如果是，应改为 ReadModel 并移动到查询图。
21. 查询图中是否存在无块单参数 `Aggregate('XxxAggregate')`。
22. 查询图中的聚合简化引用是否均为双参数写法：
    `Aggregate('XxxAggregate','中文聚合名')`。
23. 是否误改了聚合图中带内容块的 `Aggregate('XxxAggregate')[ ... ]`。
24. 查询图中是否存在没有 Query 连接的 UserInterface。
25. 查询图中是否存在没有 Query 关系依据的 Person。
26. 每个 Query 是否只直接连接主 ReadModel。
27. 子 ReadModel 是否通过主 ReadModel 字段表达结构来源，而不是由 Query 直接连接。
28. 是否误删了 SRS 明确要求的并列返回结果集 ReadModel 关系。
29. 查询图中是否存在 Query 连接 Entity 的错误关系。
30. UI 查询图、外部 API 查询图、外部系统查询图是否仍然分图。
31. `.wsd` 文件是否为 CRLF 换行。
32. 外部 API 查询图中是否仍存在 `QuerySide`、`查询方类型` 这类混合查询视角字段。
33. 不同调用方视角入参、权限、定位方式或返回字段不同的 API Query 是否已拆分。
34. 调用方 A 侧 Query 是否只连接调用方 A 外部调用系统。
35. 调用方 B 侧 Query 是否只连接调用方 B 外部调用系统。
36. 返回字段不同的多调用方 Query 是否拆分了 ReadModel，未错误复用同一个返回模型。
37. 是否存在有限值域、业务选择项、状态、类型、方式、结果、来源等字段仍使用 String 兜底。
38. 是否扫描了 `Mode`、`Type`、`Status`、`State`、`Result`、`Source`、`Category`、`Kind`、`Action`、`Conclusion` 等字段并说明处理理由。
39. 是否误把编码、名称、单号、业务标识、备注、说明等普通字符串改为 Enum。
40. 普通 Command 是否只连接一个实际状态所有者类型；批量修改同类 Entity 实例时是否仍连接该 Entity 类型。
41. 是否存在 Command 同时连接 Aggregate 和 Entity，或者连接 Aggregate 却实际只修改 Entity 状态的所有权表达错误。
42. FacadeCommand 是否确实协调多个具有独立业务职责的 Command，而不是仅因数组入参、多个实例或多个持久化结构而创建。
43. FacadeCommand 是否有且只有一个实际主目标 Aggregate / Entity。
44. FacadeCommand 是否通过内部 Command 表达需要协调的独立职责；若没有独立职责，是否已降级为普通 Command。
45. 内部 Command 是否分别具有明确的独立业务意图和实际状态目标。
46. 是否存在为批量 FacadeCommand 逐项调用而拆出的伪单项 Command。
47. 每个内部 Command 是否至少具有独立入口、独立业务含义、独立事件语义或复用价值之一；否则是否已合并或删除。
48. FacadeCommand 与内部 Command 的职责是否清晰，是否避免仅按数据库表或持久化结构机械拆分。
49. 是否存在为“创建主体”“修改状态”“推进主状态”或“一命令一实体实例”而机械生成的内部 Command。
50. 是否存在被跨聚合 Policy、平台消息、外部推送或查询投影消费，但仍使用简写的 DomainEvent。
51. 显式 DomainEvent 是否只包含事实契约所需字段，而不是机械复制 Command 全部入参。
52. 显式 DomainEvent 是否遗漏下游定位、状态推进、消息、推送或查询投影所需字段。
53. 简写 DomainEvent 是否已说明仅用于聚合内部状态追溯，不作为跨聚合事件契约。
54. 是否误把内部 ID、技术错误堆栈、认证上下文、内部存储路径等实现字段写入跨聚合事件。

十四、Entity 注意点

1. UML 中的 Entity 只表达需要真实持久化、通常需要建表的领域实体、明细表、历史记录表或聚合内部持久化结构。
2. 如果某个结构只是命令入参中的复杂对象、数组项、列表项、创建项 DTO，不需要单独用 Entity 实例化表达。
3. 命令入参中的复杂结构只需要在 Command 入参中标识，例如：
   `明细列表(Items XxxCreateItem[])`
   不再额外创建：
   `Entity('XxxCreateItemEntity')`
4. 如果该入参结构拆解后会落到真实业务表中，应由真实表对应的 Aggregate / Entity 承接，例如主体表、明细表、审核记录表、终止记录表、状态历史表。
5. 不允许为了满足“复杂入参有结构承接”而创建不建表的 Entity。
6. 删除此类伪 Entity 时，需要同步删除相关关系线，避免孤立关系或悬空引用。

十五、命名规范补充要求

1. UML 元素 ID 必须符合 dy-puml 技能的后缀约定，确保从命名上能区分元素类型。
2. `Command(...)` 的元素 ID 必须以 `Command` 结尾。
   示例：
   - 使用 `Command('CreateXxxItemsCommand')`
   - 不使用 `Command('CreateXxxItems')`
3. `FacadeCommand(...)` 的元素 ID 也必须以 `Command` 结尾。
   示例：
   - 使用 `FacadeCommand('ReplaceCurrentXxxRuleCommand')`
   - 不使用 `FacadeCommand('ReplaceCurrentXxxRule')`
4. `DomainEvent(...)` 的元素 ID 必须以 `Event` 结尾。
   示例：
   - 使用 `DomainEvent('XxxItemsCreatedEvent','xxx已创建')`
   - 不使用 `DomainEvent('XxxItemsCreated','xxx已创建')`
5. `Policy(...)` 的元素 ID 必须以 `Policy` 结尾。
6. `Aggregate(...)` 必须以 `Aggregate` 结尾。
7. `Entity(...)` 必须以 `Entity` 结尾。
8. `ReadModel(...)` 必须以 `ReadModel` 结尾。
9. `Enum(...)` 必须以 `Enum` 结尾。
10. 不允许重复追加后缀，例如不得出现：
    - `CreateXxxCommandCommand`
    - `XxxEventEvent`
    - `XxxAggregateAggregate`
11. 所有关系引用必须使用调整后的元素 ID，避免出现旧名称残留或悬空引用。
12. 中文显示名保持业务语义即可，不要求包含 Command/Event 等英文后缀。
