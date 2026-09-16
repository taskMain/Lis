# 阶段 3 前端实施

设计见 [design.md](design.md)，矩阵见 [testPlan.md](testPlan.md)。本文件只登记顺序与前置。

## 批次与依赖

| 批次 | 范围 | 主责与文件所有权 | 前置条件 | 矩阵编号 | 执行状态 |
|---|---|---|---|---|---|
| 1 | 依赖与运行时配置：新增 `@dy/components-base`、`@dy/api-client-base`，`pnpm-workspace.yaml` 增加 kiota overrides，新增 `src/runtimeConfig.ts`、`config.development.json` 的 `baseApiBaseUrl`、入口 `configureBaseComponents` | 前端主责：`client/apps/dy-medical-recognition/`、`client/pnpm-workspace.yaml` | 设计确认；安装后 build/typecheck 实测通过 | C24、C25 | 未开始 |
| 2 | API Client 累计生成与适配层 | 前端主责：`client/packages/api-client-medical-recognition/`、子应用适配层 | 后端契约稳定 | C24、C2、C4 | 未开始 |
| 3 | 两个页面、路由与组件 | 前端主责：`client/apps/dy-medical-recognition/src/pages/`、`src/router/` | 批次 1、2 | C1、C3 ×2、C5-C21、C23 | 未开始 |
| 4 | 宿主链路验收 | 测试主责：阶段 `testReport.md` | 批次 1、2、3；菜单子项与角色授权由负责人配置；后端可用 | C1-C23（含 C3 ×2；C2、C4、C21 仅批次 2、3 取 Component 层）、C26 | 未开始 |

矩阵编号可跨批次引用，按完整用例只统计一次；只完成部分层级不得把整条用例记为通过。

批次 1 的实测是硬前置：`@dy/components-base@0.1.4` 的具名导出清单与 `@dy/api-client-base@0.1.39` 的类型声明在本机都没有落盘证据，只有 registry 上的 manifest 核对。若实测失败，按 S3-D10 修正或改由后端提供范围数据，**不得**回落到会与 `@dy/auth@0.1.39` 形成双实例的 `0.1.28` 组合。

批次 1 同时承担两项依赖面验证：① `pnpm-lock.yaml` 中 kiota `.102` 与 `.103` 并存，overrides 统一到 `1.0.0-preview.103` 后须确认依赖树无冲突，冲突则回退 overrides 并登记为已知依赖形态；② `baseApiBaseUrl` 指向的 `http://183.224.180.166:35001` 必须以真实调用确认可用（环境规范把"对外依赖的平台 Base API"列为待配置项，未纳入本阶段验证范围）。两项未确认前，范围下拉不得视为已验证。

C3 需要**两份身份上下文**（可信院区存在与可信院区缺失各一份），批次 3、批次 4 执行时按两份上下文分别取证。

## 执行记录

- [ ] 阶段设计确认。
- [ ] 批次 1：安装依赖、增加 overrides、落地运行时配置并实测 `typecheck` 与 `build`。
- [ ] 建立目标失败证据或等价基线。
- [ ] 完成两个页面与路由，接入真实生成端点。
- [ ] 从宿主菜单进入并完成真实验收，更新阶段根 `testReport.md`。

## 待办与交接

- 两个菜单子项的名称与路由由本项目提供：平台管理员页「互认项目金额维护」/ `recognition-amounts`，医院管理员页「本院区互认项目金额」/ `branch-recognition-amounts`；菜单维护与角色授权由负责人在权限系统完成，未授权时相关宿主用例记 `Blocked`。
- `config.json`（生产配置）本阶段不改，也不登记生产地址（S3-D13）。
- 可空枚举筛选形态按 S3-D14 处理：本页不下推枚举筛选、不手改生成物。
