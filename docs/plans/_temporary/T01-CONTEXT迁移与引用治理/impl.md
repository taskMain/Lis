# T01 实施方案

按 design.md §3 执行顺序记录实施批次。

| 批次 | 内容 | 状态 | 备注 |
|---|---|---|---|
| 0 | Step 0 事实迁移核对表 | Passed | 写入根 testReport.md；147 行核对表，覆盖完整性验证见 testReport |
| 1 | Step 1 SRS 并入 | Passed | 仅并入「需并入」条目；29 处插入全量复核无重复，见 testReport 验证记录 |
| 2 | Step 2 引用改写 | Passed | 按 design.md §5.1 顺序；禁改清单外零残留，见 testReport 验证记录 |
| 3 | Step 3 删除 CONTEXT.md 本体 | Passed | 按调整缩减为 009 EX 表头计数修正（23→24）一行；`CONTEXT.md` 本体已删除，删除后三项终验与 `_temporary` 引用核查均 `Passed`，证据见根 testReport |
| 4 | Step 4 交接与验证记录 | Passed | 根 testReport.md 已回写验证结果与遗留风险，本文件与根 testReport 均已补「任务交接」节 |

状态取值见 `.agents/instructions/testing-baseline.md`。

## 任务交接

- 主责智能体：实施智能体
- 任务目标：删除仓库根 `CONTEXT.md` 前把无等价表述的事实并入 `docs/需求规约SRS.md`，将全仓库引用改写为 SRS 条款引用，随后删除本体并完成终验
- 负责范围：design.md §5.1 应改清单的引用改写、SRS 事实并入、T01 阶段文档与验证记录；§5.2 禁改清单未触碰
- 关键决策：31 条「需并入」落位为 29 处插入（新增条目一律在各节业务规则清单末尾追加，条款号零顺延）；修订历史新增 V0.10（患者脱敏决策）；design.md §9.2 两类 PDF 下载入口判定「一句都不补」；Step 3 缩减为 009 EX 表头计数修正一行
- 修改文件：`docs/需求规约SRS.md`（29 处插入）；§5.1 共 59 个文件（AGENTS.md 1、.agents/instructions 3、角色人格 33、现行说明与图 4、阶段设计与规格 17、代码注释 1）；删除 `CONTEXT.md`；本目录 impl.md 与 testReport.md
- 已执行验证：删除前 6 项 `Passed`（核对表覆盖、SRS 并入无重复、引用零残留、diff 干净、改动文件对账、§7.6 PDF 下载入口）+ 11 个代表条款号锚点抽样全部真实 + 新增行零行号引用 + 模板句通顺抽查；删除后 4 项 `Passed`（引用零残留终验、删除后引用仍成立、改动文件清单终验、`_temporary` 引用核查），明细见根 testReport
- 未执行或受阻验证：无
- 遗留风险：SRS 排除项第 24 项无阶段 7 EX 回归条目（表头计数已修正）；阶段文档既有 SRS 行号锚点漂移未动（§5.1 清单外）；部分文件存在 git LF→CRLF 归一化提示（`git diff --check` 干净）；` D ({t` 为既有非本任务项
- 建议下一主责：无（任务关闭）
