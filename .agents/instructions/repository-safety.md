# Repository Safety

本文是 MedicalRecognition 仓库级安全细则的单一事实来源：Git worktree、分支与提交、脚本与迁移、进程与端口、App Goal，以及角色文件的分工具目录维护。测试配置及待定项见 [Test Environment](test-environment.md)。只有涉及上述范围的任务才需要加载本文；根 `AGENTS.md` 只保留对应禁令摘要。

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

## 5. 角色文件的分工具目录

角色文件的目录、格式和职位清单见 [Collaboration](collaboration.md)。本节规定维护约束。

- 每个工具只读取自己的目录，不读取其他工具的目录；同一任务只加载一份角色文件。
- 项目规则只写在 `AGENTS.md` 与 `.agents/instructions/`；角色文件不写规范条文、命令、端口和业务事实。
- 新增或调整职位时先改 [Collaboration](collaboration.md) 的职位表，再同步三个目录；文件名与目录名保持一致，`name` 字段按各工具约定填写。
- `.claude/skills` 只存放 GitNexus 技能，不承载团队角色。
