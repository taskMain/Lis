# 02: API Client 重生成与适配层关键字映射

**What to build:** 前端以新契约说话：API Client 按累计规则重生成后承载 `Keyword` 字段形态，适配层把页面统一关键字装配进八个统计查询与两个导出请求，互认科室 ID、标准项目编码、互认医生 ID 的旧映射消失；平台/本院双版本范围注入、内嵌分页、日期原样提交与敏感条件不入地址栏口径不变（S6-D19）。

**Blocked by:** 01（统计查询契约关键字化，客户端生成依赖稳定契约）。

**Status:** done（2026-10-20，执行记录见文末）

- [x] RED 先行：适配层关键字映射用例先失败后绿
- [x] API Client 累计重生成并核对：公开入口、请求与读模型字段、可空性、枚举、分页形状与后端新契约一致；保留阶段 1-5 已交付接口；登录接口保持排除；生成覆盖的手写入口内容恢复；生成面形状由两个适配层测试的请求体断言同批承载与同步（前端无独立快照测试文件）；登录排除保留；手写入口恢复核对
- [x] C4 修订面取证：查询与导出请求以 `Keyword` 提交统一关键字、不再提交三独立字段；双版本范围注入与组织一致性字段口径保持
- [x] 受影响面（适配层与 API 契约测试）全绿；lint 0、构建 0 错误、`git diff --check` 干净

## 执行记录

### 生成面（Kiota 累计重生成）

- 进程处置：在 `server/` 以 `dotnet run --project Dy.MedicalRecognition --no-build` 启动宿主（监听 `http://localhost:15014`，launchSettings/Development），生成完成后已停止本轮启动的进程并核实端口释放（`/openapi/v1.json` 返回 000，netstat 无 LISTENING，仅 TIME_WAIT 残留）。启动日志中宿主自身对 `/Api/Staff/QueryAllStaff` 的无凭据探测 401 为框架行为，与本轮无关。
- 流程：`GET /openapi/v1.json` → `prepare-openapi`（修正整数联合类型 21 处、可空引用 28 处，path 60）→ `generate`（自带 `--exclude-path /auth/login`，未加 `--clean-output`）→ `build` + `typecheck` 通过。
- 生成面核对结果：
  - path 数 60，与生成基线累计集合一致（阶段 1-5 已交付接口全部保留，无新增/删除端点）。
  - 十个统计请求模型（八个查询 + 两个导出）字段抽样：`keyword?: string | null` 进入，`recognitionDeptId`、`recognitionDoctorId`、`standardProjectCode` 移除；与后端契约逐字段一致（含导出请求的 `nonAdoptionReasonCode`、`categoryName`、`groupName` 保留）。
  - 读模型不变（`recognitionDeptId` 等展示字段保留在 `RecognitionUsageSummaryReadModel` 等读模型）；分页形状 `PageRequestDto`/`PageInfoDto`（pageIndex/pageSize[/totalCount]）不变；请求模型无 `HasNext`/`Pagination`/`SkipCount`/`PageCount`。
  - URL 模板不变（`{+baseurl}/Api/MedicalRecognitionReportQuery/...` 逐条核实）；`src/api/index.ts` 仅 `/Api`→`/api` 注释大小写变化（Kiota 对根节点命名来源的产物，无行为影响）。
  - 手写入口 `src/index.ts` 本轮未被生成覆盖，内容与生成前逐字节一致；`src/kiota-lock.json` 仅 `descriptionHash` 更新，`excludePatterns: ['/auth/login']` 保留。
  - 生成面快照断言与分部清单：前端无独立的生成面快照测试文件；生成面形状由两个适配层测试的请求体断言承载（本批同步），后端分部清单断言归后端批次（已交付）。
- 本轮临时文件（`server/openapi-check-tmp.json`，取文档时的核对比对文件）已删除。

### 适配层改动（RED→GREEN）

- RED：先改两个适配层测试到新请求形状，运行失败 13 例（keyword 未提交、三旧字段仍被提交、原因代码随全部明细类型/导出类型提交），其余 747 例通过；实现后同批用例全绿。
- RED→GREEN 用例名（13 例）：
  - `C4 平台版汇总请求：接收组与来源组三级范围都随请求提交 > 请求体携带两组组织编码与统一关键字、日期按年月日原样提交、分页内嵌 page 对象`
  - `C4 本院版汇总请求：类型与请求体都不含本侧组织与医院 > 请求体只提交本侧院区、来源组医院/院区与统一关键字，组织字段一律不出现`
  - `C4 接收侧明细请求：明细类型必填，关键字承载医生条件，原因只随不采纳明细 > 平台版不采纳明细请求提交统一关键字与不采纳原因代码，独立医生字段不再出现`
  - `C4 接收侧明细请求：… > 不采纳原因代码只随不采纳明细（明细类型 3）提交，其余明细类型不携带该条件`
  - `C4 接收侧明细请求：… > 本院版明细请求不提交组织与医院字段`
  - `C4 导出请求：… > 平台版导出提交两组组织编码、统一关键字与导出类型，请求体无分页`
  - `C4 导出请求：… > 不采纳原因代码只随不采纳明细导出（导出类型 4）提交，其余导出类型不携带`
  - `C4 导出请求：… > 本院版导出不提交本侧组织与医院，走本院导出端点`
  - `C4 平台版来源侧汇总请求：… > 请求体携带两组组织编码、统一关键字、日期原样、维度与内嵌分页`
  - `C4 本院版来源侧请求：… > 汇总请求只提交本侧来源院区、接收组医院/院区与统一关键字`
  - `C4 本院版来源侧请求：… > 明细请求走 queryBranchSourceRecognitionDetails，来源侧没有明细类型与独立项目编码字段`
  - `C4 来源侧构造拒绝面与导出请求 > 来源侧导出只接受导出类型 6-7；平台版提交两组组织编码、统一关键字且无分页`
  - `C4 来源侧构造拒绝面与导出请求 > 本院版来源侧导出走 branch 端点且不提交两组组织编码`
- 适配层实现：12 个查询/导出条件视图模型以 `keyword` 替换 `recognitionDeptId`/`standardProjectCode`/`recognitionDoctorId`（原因：明细与导出条件保留 `nonAdoptionReasonCode`）；`keyword` 进入构造期去首尾空白键集合；明细请求体的原因代码收窄为仅明细类型 3（不采纳）提交；导出请求体的原因代码收窄为仅导出类型 4（接收侧不采纳明细导出）提交。双版本范围注入、内嵌 `page`、日期原样提交、敏感条件不入地址栏、构造拒绝面（含本院登录院区缺失零请求）全部保持，既有用例未放宽。

### 同票最小补偿（编译闭包）

- `StatisticsFilters` 删除三个旧筛选字段、新增 `keyword`（`nonAdoptionReasonCode` 保留，原因行下钻继续写入）；查询区的互认科室/标准项目编码/互认医生/不采纳原因四个文本框收敛为单一关键字输入框（占位「互认科室 / 互认项目 / 互认医生」、可清空、改动进草稿、经查询提交）。时间控件、查询/重置按钮、范围选择、汇总与明细区、原因区、导出、弹窗均未动。
- 下钻联动补偿：互认科室维度（原回填科室 ID）与标准项目维度（原回填项目编码 + 项目类型）暂不回填已撤销字段，标准项目维度保留项目类型回填；维度值写入关键字草稿未在本票做，留给票 03（S6-D19 下钻写关键字口径）。原因行下钻（写原因代码并切不采纳明细）保持原行为。
- 页面装配函数（`usageStatisticsPages.tsx`、`sourceStatisticsPages.tsx` 各三个）改为提交 `keyword: filters.keyword`；导出装配保留「明细导出才携带原因代码」的页面侧条件，收窄为不采纳明细导出的判定在适配层完成。

### GitNexus 结论

- 修改前 upstream impact（`--repo Lis`）：六个构造出口函数均为 LOW（直接调用方为页面查询出口）；`StatisticsFilters` 为 CRITICAL（34 受影响、12 直接），全部落在阶段 6 统计页闭包（board、双侧页面入口、两个适配层及其测试），属 S6-D19 的预期传播，验证面完整，未触发停止条件。
- 完成检查 `detect-changes --repo Lis`：critical，为工作区累积多批次未提交改动（后端批次 8、阶段文档、本轮前端，55 文件 290 符号）的聚合等级；逐项可归属已批准的阶段 6 重开轮范围，聚合等级不单独构成阻断。

### 门禁终值

- 受影响面测试：`recognitionStatistics/` 目录 6 文件 85/85 通过；全量前端 24 文件 760 通过、2 跳过（既有跳过项，非本轮引入）。
- lint：0 错误（`pnpm -F dy-medical-recognition lint`）。
- 构建与类型检查：`@dy/api-client-medical-recognition` build + typecheck 通过；`dy-medical-recognition` typecheck 0 错误、build 成功（chunk 体积告警为既有提示）。
- `git diff --check`：干净（仅 core.autocrlf 的 LF/CRLF 换行提示，无空白错误）。

### 改动文件清单（本轮）

- 生成物：`client/packages/api-client-medical-recognition/openapi/medical-recognition.openapi.json`、`src/kiota-lock.json`、`src/api/index.ts`、`src/models/index.ts`
- 适配层：`client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts`、`sourceRecognitionStatisticsApi.ts`
- 适配层测试：同目录 `recognitionUsageStatisticsApi.test.ts`、`sourceRecognitionStatisticsApi.test.ts`
- 补偿面：同目录 `statisticsBoard.tsx`、`usageStatisticsPages.tsx`、`sourceStatisticsPages.tsx` 及 `statisticsBoard.test.tsx`、`usageStatisticsPages.test.tsx`

### 给票 03 的交接

- 筛选模型现状：`StatisticsFilters` 为 `startTime/endTime/ownBranchCode?/两组范围字段/keyword?/itemType?/nonAdoptionReasonCode?`；`keyword` 已贯通双侧装配、四个查询端点对与两个导出端点，构造期去首尾空白。
- 已做：查询区单一关键字输入框（占位「互认科室 / 互认项目 / 互认医生」、`aria-label="关键字筛选"`、可清空、草稿提交式）；不采纳原因代码仅随不采纳明细查询与明细导出提交（适配层按明细类型 3 / 导出类型 4 收窄）。
- 未做（票 03 范围）：RangePicker 与预设、默认近 30 天窗口、去重置按钮、原因行下钻的筛选标记、下钻把科室值/项目编码写入关键字草稿（本票暂不回填该两维度的维度值，标准项目维度保留项目类型回填）、范围组标签、本院页页头左右结构与范围条迁移、统计页配套 CSS。
- 现存中间态：查询/重置按钮仍并存（票 03 去重置）；页头仍为单列结构；页面文件头注释中「页面默认当月 1 日至当天」的日期默认口径仍为当前实现（票 03 改默认窗时同批修订）。
