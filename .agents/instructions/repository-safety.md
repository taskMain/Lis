# Repository Safety

本文是 MedicalRecognition 仓库级安全细则的单一事实来源：Git worktree、分支与提交、脚本与迁移、进程与端口、App Goal，以及自定义智能体和技能的双目录维护。测试配置及待定项见 [Test Environment](test-environment.md)。只有涉及上述范围的任务才需要加载本文；根 `AGENTS.md` 只保留对应禁令摘要。

## 1. Worktree

- 未经项目负责人在本轮明确授权，不得创建、切换或继续使用额外 Git worktree；新建任务或会话、并行处理、保护现有修改均不构成授权，新任务默认复用当前工作区。
- 确需 worktree 时，创建前必须说明基线、绝对路径、分支或 detached 状态、变更回迁和清理方案并取得确认。
- 发现未经授权的 worktree 时立即停止写入并报告状态，不自行迁移、合并或删除。

## 2. 分支、提交与脚本

- 不自行创建分支、commit 或 push。用户明确要求提交时，提交信息使用 `类型: 中文描述`，例如 `docs: 更新阶段设计`。
- 不修改无关代码，不执行破坏性 Git 命令，不删除来源不明的文件或进程。
- `Scripts` 下建表脚本表达最终表结构，不包含 `ALTER TABLE`；增量迁移只放 `Scripts/Migrations`，不得自行执行迁移。

## 3. 进程与端口

- 只关闭本轮自行启动且已核实 PID、命令行、工作目录和端口的进程；不得关闭其他项目、公网穿透或无关宿主服务。
- 项目端口、宿主和测试服务地址只以 [Test Environment](test-environment.md) 中确认配置为准；未配置时不启动依赖该配置的服务，不沿用其他项目端口。启动、健康检查和系统参数联调核对同一实际环境。

## 4. App Goal

- App Goal 只有在项目负责人明确授权，或当前阶段 `impl.md` 明确要求且实时启动条件满足时才允许创建。设计、文档调整、普通测试和“目标测试”均不构成创建授权。

## 5. 自定义智能体与技能的分工具路径

本轮仅迁移子代理，不迁移或创建技能及技能脚本；目标已有技能保持原样，由负责人后续重新制作开发和审查技能。以下路径约定不构成创建或修改技能的授权。

自定义智能体和技能按客户端工具分目录存放，各工具只读取自己的目录，权威内容互不覆盖；协作规范始终以 `.codex/agents/COLLABORATION.md` 为唯一来源，不按工具复制。

| 工具 | 子代理位置 | 技能位置 | 格式 |
|---|---|---|---|
| Codex | `.codex/agents/*.toml` | `.codex/skills/<名称>/SKILL.md` | TOML 配置；SKILL.md 附加 `agents/openai.yaml` |
| ZCode | `.zcode/agents/*.md` | `.zcode/skills/<名称>/SKILL.md` | Markdown：frontmatter（`name`、`description`）+ 正文即系统提示词 |

- 新增或修改子代理、技能时，同步维护对应工具目录下的副本；两套目录内容应保持语义一致，协作规范除外（仅存在于 `.codex`）。
- ZCode 子代理文件名即代理标识，正文为原 TOML `developer_instructions` 的内容；`.zcode/agents/` 下不得放置无 frontmatter 的普通文档。
