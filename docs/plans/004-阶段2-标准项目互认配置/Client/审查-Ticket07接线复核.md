# 审查-Ticket 07 路由 + 真实接线 + 契约收敛（独立复核）

范围：`client/apps/dy-medical-recognition/src/router/routes.tsx`、`routes.test.ts`、`src/pages/recognitionProjects/*`、[Pages/RecognitionProjects/RecognitionProjects.md](Pages/RecognitionProjects/RecognitionProjects.md)。
方式：只读复核 + 实测（test / lint / tsc --noEmit / git status / git diff --check）。**本文件不修改任何被审查文件，也不宣称宿主验收。**

除另注明外，路径均相对仓库根（本文件所在仓库的根目录），不写绝对路径。

## 实测记录（真实数字）

| 命令 | 结果 |
|---|---|
| `cd client; pnpm -F dy-medical-recognition test` | exit 0；Test Files 5 passed (5)；**Tests 85 passed \| 2 skipped (87)**；Duration 29.01s；另有 `Vitest caught 2 unhandled errors`（未致失败，见 §3.7） |
| `cd client; pnpm -F dy-medical-recognition lint` | exit 0；`3 problems (0 errors, 3 warnings)` |
| `cd client/apps/dy-medical-recognition; pnpm -F dy-medical-recognition exec tsc -p tsconfig.app.json --noEmit` | exit 0（补充静态检查，用于判定 §1.2 / §2.5 的类型层事实） |
| `git status --short -- client/apps/dy-medical-recognition` | `M src/router/routes.tsx`；`?? src/pages/recognitionProjects/`；`?? src/router/routes.test.ts` |
| `git diff --check` | exit 0，无空白错误（仅 LF→CRLF 提示） |

分文件用例数（脚本计数，与运行输出一致）：`recognitionProjectsApi.test.ts` 27（skip 0，任务描述的 20→27 成立）、`RecognitionProjects.test.tsx` 21（skip 0）、`routes.test.ts` 3（skip 0）、`standardCatalogApi.test.ts` 15、`StandardCatalog.test.tsx` 21（skip 2）。
lint 的 3 条 warning 全部落在本票未改动的文件：`client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:19`、`src/pages/prototype/standardCatalogPrototypeStore.tsx:122`、`src/router/index.tsx:15`。

---

## 1. 适配层边界

**1.1 主要边界项：None。**
- 不创建 Client、不读/复制 token、不解析平台错误响应、无拦截器/业务错误类型、无写死 URL、无底层 HTTP、无生成目录深路径导入：`client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts:1-6`（只从包公开入口 `@dy/api-client-medical-recognition` 取类型）、`:259-311`（5 个出口全部 `client.api...post`）。Client 实例只由既有 Context 提供并在页面透传：`RecognitionProjects.tsx:26`、`:235`。
- 关键词扫描（`fetch(`/`axios`/`http`/`interceptor`/`ResponseHandler`/`reportApiError`/`extends Error`/`kiota-abstractions`/`ts-ignore`/`eslint-disable`）在交付代码中零命中；`kiota-abstractions` 仅出现在注释 `recognitionProjectsApi.ts:10`。
- 交付代码无 `any`、无双重断言、无伪造必填值。

**1.2 Low（仅测试，非交付代码）：测试桩用双重断言构造局部假 Client。**
位置：`recognitionProjectsApi.test.ts:50` `return client as unknown as MedicalRecognitionClient & typeof client`。
判定：Frontend API Client §2 的"不使用双重断言"针对包与适配层，本处位于 mock 边界且只保留被测端点（`:34-49`）；阶段 1 无同类写法可比。最小修正：保留亦可，或在注释中标注"仅测试桩的受控双重断言"以免被当作范例扩散。

**1.3 `pendingIntegration` 零残留：None。**
全仓 `client/` 关键词扫描 `pendingIntegration|PendingIntegration|待接线|pending integration` **零命中**；5 个出口均调用生成端点（`recognitionProjectsApi.ts:271/279/288/297/309-310`），无静默成功、无空集合兜底、无硬编码地址。Pages 文档 §6.3 的同一结论成立。

**1.4 三个写载荷的定性：None（是写前校验载荷，不是契约镜像）。判断依据：**
1. 载荷字段是生成请求的**真子集且去掉了框架字段**：生成 `CreateMutualRecognitionItemRequest` 含 `changedProperties`（`client/packages/api-client-medical-recognition/src/models/index.ts:364-377`），`UpdateMutualRecognitionItemConfigurationRequest` 同（`:1818-1831`），`Enable/DisableMutualRecognitionItemRequest` 为 `changedProperties`+`id`（`:958-967`、`:1069-1078`）；本地载荷（`recognitionProjectsApi.ts:83-86`、`:88-91`）不含 `changedProperties`。
2. `EnabledUpdatePayload` 的 `enabled` 在生成端**根本不存在**，它用于在两个端点间选择（`recognitionProjectsApi.ts:304-311`），因此不可能是镜像。
3. 载荷由校验函数产出且非法即 `null`（`:125-137`），I/O 出口再**逐字段重建** body（`:288-291`、`:297-300`、`:308-310`），框架字段无法泄漏；测试锁定精确键集（`recognitionProjectsApi.test.ts:299`、`:323-324`、`:331`、`:337`）。

**1.5 Low：查询侧仍有一份"窄化的契约副本"，与文件头/文档的自述不一致。**
位置：`recognitionProjectsApi.ts:76-80`（`ConfigurationListQuery` 重复声明 `organizationCode`/`standardProjectCode`/`configurationStatus`），随后在 `:264-267` 逐字段搬进生成类型。
证据冲突：`:25-27` 自述"契约类型直接引用生成类型……不存在第二套契约来源"；Pages 文档 §6.1"`ConfigurationListQuery` 的查询体由生成类型……承载"。
判定：这是窄化副本而非完整镜像（类型更严、无框架字段），实际风险低。最小修正：直接以 `RecognitionProjectConfigurationListQueryRequest` 作为唯一形状，或把注释与文档改为"生成类型 + 赋值处收敛"的准确表述。

**1.6 Low：两个导出的枚举常量未被消费，页面重复硬编码同一组字面量。**
位置：`recognitionProjectsApi.ts:42`、`:46` 导出 `MEDICAL_ITEM_TYPES`/`CONFIGURATION_STATUSES`；全 `src` 扫描仅这两处出现（只用于派生类型 `:43`、`:47`），而页面另写一套字面量 `RecognitionProjects.tsx:67-71`（`ITEM_TYPE_LABEL`、`CONFIGURATION_STATUS_OPTIONS`）。最小修正：页面选项从导出常量派生，或撤下未使用的值导出只留类型。

---

## 2. 请求形状正确性

**2.1 创建：None。** body 仅 `{ standardProjectCode, recognitionDurationDays }`（`recognitionProjectsApi.ts:288-291`），与后端 `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/CreateMutualRecognitionItemRequest.cs:15-26` 的两个业务字段一一对应（组织由服务端上下文注入、内部标准项目 ID 不提交）；不含 `organizationCode`/`id`/`standardItemId`（测试 `recognitionProjectsApi.test.ts:299-302`）。天数为正整数（`parseRecognitionDurationDays` `:114-119`）。

**2.2 修改：None。** body 仅 `{ id, recognitionDurationDays }`（`:297-300`），对应 `UpdateMutualRecognitionItemConfigurationRequest.cs:13-24`；同值不短路（`:132-137`）。测试 `:321-324`。

**2.3 启停：None。** 只 `{ id }`（`:308-310`），对应 `Enable/DisableMutualRecognitionItemRequest.cs:7-13`（两请求都只有 `Id`）；`enabled` 不上线，仅决定调用哪个端点。测试 `:327-339` 同时断言另一端点零调用。

**2.4 查询组织编码：None。** `organizationCode` 显式赋值下发（`:264-265`，测试 `:268-272`；页面级 `RecognitionProjects.test.tsx:255-256`），后端必填见 `server/Dy.MedicalRecognition.Application.Contracts/Queries/RecognitionProjectConfigurationListQueryRequest.cs:16-18`；组织缺失/空白时构造器返回 `null`，不退化为全局查询（`:147-148`，页面测试 `:267-280`），页面也不会把 UI 筛选下推（`RecognitionProjects.tsx:261` 未传 filter）。

**2.5 Medium：状态筛选下发分支会产出后端无法绑定的 `configurationStatus:{}`，且测试把该分支固化为期望。**
位置：`recognitionProjectsApi.ts:268-269`（把整数断言进生成端空对象类型 `ConfigurationStatus`）；`recognitionProjectsApi.test.ts:275-290`（`expect(body).toEqual({ ..., configurationStatus: 2 })`）。
证据链：生成序列化器用 `writeObjectValue` 写该字段（`client/packages/api-client-medical-recognition/src/models/index.ts:1686`，空体 `serializeConfigurationStatus` `:1280`）；`client/node_modules/.pnpm/@microsoft+kiota-serialization-json@1.0.0-preview.103/.../jsonSerializationWriter.js` 的 `writeObjectValue` 对非 Parsable 值走 `writePropertyName`→`startObject`→空序列化→`endObject`，即输出 `{}`；后端字段是 `ConfigurationStatus?` + `[EnumDataType]`（`RecognitionProjectConfigurationListQueryRequest.cs:27-28`），收到对象即绑定/校验失败。当前**不可达**（页面调用 `buildConfigurationListQuery(targetOrganizationCode)` 不带 filter），但适配层公开了这条通路，测试又把它当作正确行为。
最小修正：在生成契约能表达枚举前删掉该下发分支（查询只接受组织编码与标准项目编码筛选），并把 `:275-290` 改为断言"不下发 `configurationStatus`"；Pages 文档 §7 已把它列为遗留，可同步收紧为"适配层不再提供该通路"。

**2.6 Low：注释声称"缺省不下发该字段"，实际总是下发 `null`。**
位置：注释 `recognitionProjectsApi.ts:141`（"编码与状态筛选缺省时不下发该字段，使'未筛选'和'筛选为空'可区分"）与实现 `:266`（`standardProjectCode: request.standardProjectCode ?? null`）、测试 `:270-272`（明确断言该键存在且为 `null`）矛盾。
影响：后端 `NonEmptyAttribute` 对 `null` 判定通过（`server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs:29-35`，注释 `:9-10` 亦说明"未传按通过处理"），因此无功能破坏；问题只在注释与"可区分"的说法不成立。最小修正：注释改为"未筛选时以 `null` 下发"；若要真正省略该键，`undefined` 会被生成序列化器跳过（同库 `writeStringValue` 对 `undefined` 直接 return）。

---

## 3. 测试是否真实（4 条抽样 + 路由用例）

**3.1 创建请求体 —— 真实断言。**
`recognitionProjectsApi.test.ts:292-303`：先经真实校验函数产出请求（`:294-295`），再调用**真实适配层出口** `createRecognitionProjectConfiguration`（`:297`），对桩记录的 body 断言 `toEqual({ standardProjectCode:'A01', recognitionDurationDays:30 })`（`:299`）并断言不含 `organizationCode`/`id`/`standardItemId`（`:300-302`）。不是同义反复：期望值是硬编码契约形状，不由被测实现推导。

**3.2 查询组织编码下发 —— 真实断言（双层）。**
适配层 `:264-273`：`expect(body.organizationCode).toBe('ORG-A')`、`expect(Object.keys(body)).toEqual(['organizationCode','standardProjectCode'])`、`not.toHaveProperty('configurationStatus')`。页面层 `RecognitionProjects.test.tsx:253-258`：`expect(query().mock.calls[0][1]).toEqual({ organizationCode:'ORG-A' })` 且键集只有组织编码——这是对"页面不下推筛选项"的正面断言，非空洞。

**3.3 非法天数零写请求 —— 真实断言（适配层偏构造性，页面层端到端）。**
适配层 `:305-314`：6 组非法输入断言两个构造器返回 `null`，并断言两个写 `post` 未被调用（`:312-313`）。此处"零请求"因未调用出口而构造性成立，真正的防线是 `null` 断言（已单独给出）。页面层 `:449-464`：填入 `0` 与 `1.5` 后点击保存，断言 `createRecognitionProjectConfiguration` 未被调用、弹窗仍在、输入保留、出现校验文案；修改侧同口径 `:468-491`（`-5` 时 `updateRecognitionDuration` 零调用）。判定：真实且覆盖两条写路径。

**3.4 启停只带 id —— 真实断言。**
`:327-339`：启用分支断言 enable body `toEqual({id:'c1'})` 且 disable 端点零调用；停用分支对称。页面层 `:511-538` 覆盖取消/右上角关闭零写与确认分支（断言适配层载荷 `{id:'c1',enabled:false}`，`:533`）。

**3.5 Low：`routes.test.ts` 确实覆盖了路由注册，但含 1 条恒真断言、未验证 DEV 条件。**
`routes.test.ts:14-23` 断言 index 路由存在、`standard-catalog` 与 `recognition-projects` 均在子路由中且 `element` 为真——已超出"只断数组长度"。但 `:22` 只断言 `element` truthy（未证明它是 `RecognitionProjects`）；`:28` `expect(paths).not.toContain('recognition-projects/')` 在源码字面量为 `recognition-projects` 的前提下**永不失败**；`:25-27` 只观察到 vitest `DEV=true` 这一分支，无法支撑 `:3` 注释所称"原型路由保持 `import.meta.env.DEV` 条件写法不变"。
最小修正：把 `:22` 改为断言元素类型/渲染结果，删除 `:28`，DEV 条件改为对 `routes.tsx:8-21` 源码或 `import.meta.env.DEV` 显式裁定的静态断言。

**3.6 skip 情况：新文件零 skip；既有 skip 实为 2 个（任务描述的"1 个"需更正）。**
`StandardCatalog.test.tsx:375`、`:443` 两条 `it.skip`，均属阶段 1 且本票未改动该文件（`git status` 未列出）；`routes.test.ts`/`recognitionProjectsApi.test.ts`/`RecognitionProjects.test.tsx` 均无 skip/todo。

**3.7 Low：未发现为通过而放宽的功能断言；但有两处"宽松面"值得记录。**
- `client/apps/dy-medical-recognition/vite.config.ts` 的 `test.dangerouslyIgnoreUnhandledErrors: true` 是**阶段 1 引入、本票未改**（`git diff --stat -- client` 不含该文件）。本次运行仍有 2 条 Unhandled Rejection：`RecognitionProjects.test.tsx:633`（新增，"写入失败"经 `submitWrite` 透传，见 `RecognitionProjects.tsx:336-345`）与 `StandardCatalog.test.tsx:361`（阶段 1），vitest 明确打印 "This might cause false positive tests"。`RecognitionProjects.test.tsx:627-629` 已把该现象写成设计说明。最小修正：长留该开关时应把忽略收敛到具体用例，避免掩盖未来真实未处理拒绝。
- `recognitionProjectsApi.test.ts:287` 把"下发整数 `configurationStatus`"写成期望，与 §2.5 同源。

---

## 4. 过度封装与回归风险

**4.1 无新增依赖/状态管理库：None。** 本票在子应用只改 `routes.tsx`（+2 行，见下）；`client/apps/dy-medical-recognition/package.json` 依赖块未变（`git diff --stat -- client` 未列出）。页面只用 `useState/useMemo/useRef/useCallback/useEffect`（`RecognitionProjects.tsx:25`），未引入 store/查询库；无新增单一实现抽象之外的层。

**4.2 Low：组织范围接缝是新增单一实现抽象，但无既有同类可复用；token 的"读取"口径建议先确认。**
`recognitionProjectsOrganizationScope.ts:21-25` 读 `useAuth().token`（仅作变化信号与可用性判断）与 `LoginUserManager.getOrgID()`（组织编码）。扫描显示阶段 1 完全不涉及组织/token（`src/pages/standardCatalog/*` 无 `LoginUserManager|useAuth|organizationCode|token` 命中），因此**不是重复既有工具**；26 行且职责单一，Pages 文档 §4 有结案依据。该层未复制、改写或透传 token，但 Frontend API Client §4 字面为"功能适配层不读取 token"，是否属该条约束建议先确认口径。

**4.3 类型/字段语义未丢失：None（附 1 条 Low）。**
行视图模型恰好承载查询契约交付的 9 个字段（`recognitionProjectsApi.ts:50-61`），页面按同 9 列 + 操作列渲染（`RecognitionProjects.tsx:364-411`，列头断言 `RecognitionProjects.test.tsx:260-265`）；已删除的派生字段有反向断言（`recognitionProjectsApi.test.ts:192-201`）；`unavailableReason` 的"目录停用原因≠配置停用"语义在适配层注释 `:59` 与页面 `RecognitionProjects.tsx:191-193` 均保留。
说明：`src/pages/recognitionProjects/` 整个目录在 git 中为未跟踪（`?? .../recognitionProjects/`），仓库内没有可 diff 的历史版本，故"类型改名"只能以 Pages 文档 §1/§6 与测试为基线核对，未发现语义丢失。
Low：`toIdentifier(value: unknown)`（`:166-169`）用 `String(value)` 归一；当前生成类型 `Guid = string`（`client/node_modules/.pnpm/@microsoft+kiota-abstractions@1.0.0-preview.103/.../guidUtils.d.ts:7`），不会出现 `[object Object]`；若将来该字段变成结构化对象，`String()` 会静默伪造标识。最小修正：仅接受 `typeof value === 'string'`，其余返回 `null`。

**4.4 `routes.tsx` 保留原型路由 DEV 条件：None。**
`git diff -- client/apps/dy-medical-recognition/src/router/routes.tsx` 仅新增 1 行 import 与 1 行子路由；`import.meta.env.DEV ? lazy(...) : null`（`routes.tsx:8-10`）与 `prototypeRoute` 展开（`:31-32`）逐字未动。

**4.5 Medium（相邻文档漂移，四组之外但影响阶段记录）：`Client/impl.md` 的批次状态仍描述"未接线"。**
位置：`docs/plans/004-阶段2-标准项目互认配置/Client/impl.md:8`（"5 个 I/O 出口为显式 `pendingIntegration()` 占位，**尚未接线**生成端点；……路由与菜单接线留 Ticket 07"）与该文件新增的"当前 `router/routes.tsx` 尚无该路由"、执行记录"**接线与收敛未完成**"。
证据冲突：代码已接线（§1.3）、路由已注册（`routes.tsx:30`）、[Pages/RecognitionProjects/RecognitionProjects.md](Pages/RecognitionProjects/RecognitionProjects.md) §6 已写"Ticket 07 已执行"。最小修正：更新该批次状态行与执行记录两处措辞（属文档，不改代码）。

---

## 总结

路由注册、真实接线与 `pendingIntegration` 清除均已落地且有可核查证据（Blocker 0 / High 0 / Medium 2 / Low 8），最应优先处理的是 §2.5——查询适配层仍保留 `configurationStatus` 整数下发通路，经生成序列化器会写出后端无法绑定的 `{}`，且测试把它固化为期望（当前页面不可达，属潜在 400 与假信心）。

## 残余风险与未覆盖面（不构成结论）

- 未做宿主验收：宿主菜单进入、统一错误提示、真实网络与响应形状仍未验证（属 Ticket 08）。
- 未运行 `build`（本票只要求 test/lint）；已用 `tsc -p tsconfig.app.json --noEmit` 覆盖类型面。
- 本次未连接数据库、未启停任何服务。
