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

矩阵编号可跨批次引用，按完整用例只统计一次；只完成部分层级不得把整条用例记为通过。

批次 1 的实测为硬前置，已按 S3-D10 结论落地：`@dy/components-base@0.1.4` 与 `@dy/api-client-base@0.1.39` 组合实测可用，未回落到会与 `@dy/auth@0.1.39` 形成双实例的旧组合。

## 执行记录

- [x] 阶段设计确认。
- [x] 批次 1：安装依赖、增加 overrides、落地运行时配置并实测 `typecheck` 与 `build`。
- [x] 建立目标失败证据或等价基线：S3-D21 重开轮与金额解析修复均先取得断言级失败证据。
- [x] 完成两个页面与路由，接入真实生成端点。
- [x] 从宿主菜单进入并完成真实验收，更新阶段根 `testReport.md`。

## 待办与交接

- **交付物尚未提交**：提交需负责人授权。
- 两个菜单子项的名称与路由由本项目提供：平台管理员页「互认项目金额维护」/ `recognition-amounts`，医院管理员页「本院区互认项目金额」/ `branch-recognition-amounts`；菜单维护与角色授权由负责人在权限系统完成（S3-D2）。
- `config.json`（生产配置）本阶段不改，也不登记生产地址（S3-D13）；其 `apiBaseUrl` 仍为未部署占位地址，正式部署前需替换。
- 可空枚举筛选形态按 S3-D14 处理：本页不下推枚举筛选、不手改生成物。
- 机制性残余：`BaseScopeSelector` 的既有行为与受控注入还原链脆弱，见阶段根 [testReport.md](../testReport.md)「未覆盖面与残余风险」。
