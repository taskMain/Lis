# Collaboration

本文规定团队职位、角色文件的目录与调用规则，是多角色任务的唯一入口。只有多角色任务加载本文；普通单智能体任务不加载。

## 1. 目录与格式

团队角色按客户端工具分目录存放，同一角色在三处各有一份载体，文件名或目录名相同。每个工具只读取自己的目录，不读取其他工具的目录；同一任务只加载一份角色文件。

| 工具 | 角色目录 | 格式 | 调用时用什么标识 |
|---|---|---|---|
| Codex | `.codex/agents/` | `*.toml`，字段 `name`、`description`、`developer_instructions` | 文件名，如 `engineering-backend-architect` |
| ZCode | `.zcode/agents/` | `*.md`，frontmatter `name`、`description`，正文即系统提示词 | 文件名 |
| DSH | `.dsh/skills/<角色键>/SKILL.md` | frontmatter `name`、`description` | frontmatter 的 `name`，如 `agency-backend-architect` |

## 2. 职位

| 职位 | 角色键 | DSH 标识 | 职责 | 调用时机 |
|---|---|---|---|---|
| 后端实现 | `engineering-backend-architect` | `agency-backend-architect` | 分层、命令与查询、事务与事件、外部集成 | 后端设计或实现 |
| 前端实现 | `engineering-frontend-developer` | `agency-frontend-developer` | 页面、状态、组件与 API Client 接入 | 前端设计或实现 |
| 界面结构设计 | `design-ux-architect` | `agency-ux-architect` | 信息架构、页面布局、交互边界 | 新页面或页面改版 |
| 界面成品审查 | `design-ui-finish-gate-reviewer` | `agency-ui-finish-gate-reviewer` | 页面交付前的成品判定 | 页面交付前 |
| 数据与查询审查 | `engineering-database-optimizer` | `agency-database-optimizer` | 表结构、索引、SqlMap 语句、分页窗口 | 涉及 SQL、索引或查询性能 |
| 代码审查 | `engineering-code-reviewer` | `agency-code-reviewer` | 两轴审查与 finding 分级 | 每批实现完成后 |
| 接口验证 | `testing-api-tester` | `agency-api-tester` | 参数、鉴权、可信范围、幂等、错误码 | 接口新增或变更 |
| 独立验收 | `testing-reality-checker` | `agency-reality-checker` | 按真实证据判定通过或阻塞 | 阶段收口与宿主验收 |

统筹由主代理承担，负责子任务路由、文件所有权、交接与决策门。

## 3. 使用与维护规则

- 项目规则只写在 `AGENTS.md` 与 `.agents/instructions/`；角色文件只写职责与专业提示，不写规范条文、命令、端口和业务事实。
- 新增或调整职位时先改第 2 节表格，再同步三个目录；目录名与文件名保持一致，`name` 字段按各工具约定填写。
- 角色文件与 `AGENTS.md`、`.agents/instructions/`、SRS、UML 冲突时以后者为准。
- 角色文件中的截图要求、Playwright 视觉核对、mock 与 Laravel 检查项不执行；宿主验收不需要留存截图。
- 角色输出的状态词只用 [Testing Baseline](testing-baseline.md) 的定义。
- 子任务开始前声明负责的文件或目录；多个角色不得同时修改同一文件；交接写明负责范围、修改文件、已执行验证、未执行验证与遗留风险。
