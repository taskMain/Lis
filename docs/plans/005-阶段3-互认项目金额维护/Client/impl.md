# 阶段 3 前端实施

**本阶段已收口（阶段状态 `Complete`，2026-09-17 负责人裁定）**，详见阶段根 [impl.md](../impl.md) 与 [testReport.md](../testReport.md)。本文件只登记顺序与前置。

设计见 [design.md](design.md)，矩阵见 [testPlan.md](testPlan.md)。

## 批次与依赖

| 批次 | 范围 | 主责与文件所有权 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 1 | 依赖与运行时配置：新增 `@dy/components-base`、`@dy/api-client-base`，`pnpm-workspace.yaml` 增加 kiota overrides，新增 `src/runtimeConfig.ts`、`config.development.json` 的 `baseApiBaseUrl`、入口 `configureBaseComponents` | 前端主责：`client/apps/dy-medical-recognition/`、`client/pnpm-workspace.yaml` | 设计确认；安装后 build/typecheck 实测通过 | C24、C25 | 已完成：依赖与 overrides 按 S3-D10 落地、`runtimeConfig.ts` 按 S3-D16 交付并在入口配置一次；`typecheck`/`build` 实测通过 |
| 2 | API Client 累计生成与适配层 | 前端主责：`client/packages/api-client-medical-recognition/`、子应用适配层 | 后端契约稳定 | C24、C2、C4 | 已完成：累计生成 27 条 path、`kiota-lock.json` 与公开入口核对通过；适配层按「服务端随行文案 → 枚举元数据 → 本地兜底」取值（S3-D21） |
| 3 | 两个页面、路由与组件 | 前端主责：`client/apps/dy-medical-recognition/src/pages/`、`src/router/` | 批次 1、2 | C1、C3 ×2、C5-C21、C23 | 已完成：两个页面与路由已实现并接入真实端点；项目类型与配置状态的取值域、取值域判定与兜底文案收编到 `src/shared/medicalItemType.ts`、`src/shared/configurationStatus.ts` |
| 4 | 宿主链路验收 | 测试主责：阶段 `testReport.md` | 批次 1、2、3；菜单子项与角色授权由负责人配置；后端可用 | C1-C23（含 C3 ×2；C2、C4、C21 仅批次 2、3 取 Component 层）、C26 | 已完成：两个身份经宿主菜单进入两个页面完成读写与只读回读；两条宿主面按环境边界登记（C3 缺可信院区缺失的身份上下文、C23 组件加载失败的注入点未命中） |
| 5 | 重开轮 API Client 累计再生成（票 10 第二棒）：两个金额查询 Request 各增可选 `keyword`，端点清单不变 | 前端主责：`client/packages/api-client-medical-recognition/` 的冻结输入、生成物与锁文件 | 票 10 第一棒的后端两个金额查询 `Keyword` 冻结；本地后端可提供 OpenAPI | C24（关键字面） | 已完成：失败基线为 `Stage3ApiContractTests.Frozen_openapi_and_generated_model_declare_optional_keyword_for_amount_queries`（再生成前判失败，消息「RecognitionAmountListQueryRequest 的属性集合缺少 keyword。」）；`prepare-openapi`（来源 `http://localhost:15014/openapi/v1.json`，整数联合 21 处、可空引用 26 处、path 57）→ `generate`（Kiota 1.34.1，固定 `--exclude-path /auth/login`）；差异只有 4 个文件，`src/index.ts` SHA256 生成前后同为 `51A20E1E…AE01C`，`kiota-lock.json` 的 `excludePatterns` 仍为 `["/auth/login"]`；`typecheck`/`build` exit 0，子应用 `test` 428 通过 0 失败、`lint`/`build` exit 0 |
| 6 | 重开轮两个页面改版（票 10 第三棒）：第一阶段骨架与筛选（页头三级范围两行展示、院区恒定单值三档默认与切换重取、筛选区与地址栏三参数、四栏层级列视图、页脚计数与双空态、读取在途写门控），第二阶段行内金额编辑替换弹窗与状态机制两道防线（C33 确认框与待切换提示态、C21 静默清空、挂起态写门控），删除 `AmountEntryModal` 与整个 `RecognitionAmountModals.tsx`；含两项设计回改——平台页初始范围按 S3-D22 取可信值、两页 C34 提交前复核按 S3-D28 方案甲 | 前端主责：`client/apps/dy-medical-recognition/src/pages/recognitionAmounts/`（适配层 `recognitionAmountsApi.ts` 同属本批次） | 批次 5（生成端含可选 `keyword`）；阶段根 `design.md` S3-D22～S3-D28 已确认（S3-D28 按方案甲更新为「比对可信范围的变化」） | C1、C3、C5-C14、C16、C17、C21、C27-C36 | 组件面完成并经两轮独立审查与三轮回改（适配层 23、平台页 26→19、医院页 17→20、第一轮回改 3、触发上移 4、复审回改 3 条用例 4 条断言），其后两项回改各自先取 RED——S3-D22 初值 `3 failed / 45 passed（48）` 与 `2 failed / 48 passed（50）`（单层缺失两条），终态平台页 50 通过；S3-D28 方案甲平台 `2 failed / 48 passed（50）`、医院 `2 failed / 52 passed（54）`，终态 50 与 54 通过；医院提交期现读补正 `1 failed / 54 passed（55）` 终态 55；S3-D28 档二（院区层按缺失值参与比对、「现读不到」收窄到可信组织或医院）三段 RED `3 failed / 66 passed（69）`、`2 failed / 57 passed（59）`、`1 failed / 51 passed（52）`，终态适配层 69、平台页 52、医院页 59。终态目录级 **180 通过**、全子应用 **549 通过 / 2 跳过 / 0 失败**，`lint`、`tsc -p tsconfig.app.json`、`build`、`git diff --check` 全 exit 0；`BaseScopeSelector` 无禁用清空属性的核对结论与替代方案已记入阶段报告；宿主面复测已执行三轮（两页初始范围、页头、筛选与地址栏、范围切换、行内保存、C33 双分支、**C34 三场景两页全部取证**，含档二构造与恢复步骤） |

矩阵编号可跨批次引用，按完整用例只统计一次；只完成部分层级不得把整条用例记为通过。

批次 1 的实测为硬前置，已按 S3-D10 结论落地：`@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39` 组合实测可用，未回落到会与 `@dy/auth@0.1.39` 形成双实例的旧组合。

## 执行记录

- [x] 阶段设计确认。
- [x] 批次 1：安装依赖、增加 overrides、落地运行时配置并实测 `typecheck` 与 `build`。
- [x] 建立目标失败证据或等价基线：S3-D21 重开轮与金额解析修复均先取得断言级失败证据。
- [x] 完成两个页面与路由，接入真实生成端点。
- [x] 从宿主菜单进入并完成真实验收，更新阶段根 `testReport.md`。
- [x] 重开轮（票 10 第二棒）：先取得失败基线，再按累计规则重新生成 API Client 并核对保护面，证据记入阶段根 `testReport.md`。

## 待办与交接

- **交付物尚未提交**：提交需负责人授权。
- 两个菜单子项的名称与路由由本项目提供：平台管理员页「互认项目金额维护」/ `recognition-amounts`，医院管理员页「本院区互认项目金额」/ `branch-recognition-amounts`；菜单维护与角色授权由负责人在权限系统完成（S3-D2）。
- `config.json`（生产配置）本阶段不改，也不登记生产地址（S3-D13）；其 `apiBaseUrl` 仍为未部署占位地址，正式部署前需替换。
- 可空枚举筛选形态按 S3-D14 处理：本页不下推枚举筛选、不手改生成物。
- 机制性残余：`BaseScopeSelector` 的既有行为与受控注入还原链脆弱，见阶段根 [testReport.md](../testReport.md)「未覆盖面与残余风险」。
