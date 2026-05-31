# PRD Numbering And Title Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mechanically fix numbering and title continuity issues in `政策文档/版本5/PRD_第五版修订稿2.md`.

**Architecture:** This is a narrow Markdown document edit. The only target content is visibly incorrect section/list numbering; no business wording or semantic references are changed in this pass.

**Tech Stack:** Markdown, PowerShell, ripgrep.

---

### Task 1: Fix Mechanical Numbering

**Files:**
- Modify: `政策文档/版本5/PRD_第五版修订稿2.md`

- [ ] **Step 1: Inspect the affected ranges**

Run:

```powershell
$i=0; Get-Content -LiteralPath '政策文档/版本5/PRD_第五版修订稿2.md' | ForEach-Object { $i++; if(($i -ge 1704 -and $i -le 1741) -or ($i -ge 1902 -and $i -le 1918)){ '{0}: {1}' -f $i,$_ } }
```

Expected: `1.12` subsection headings show `1.11.x`, and `1.15.7` has a duplicate `12.` item.

- [ ] **Step 2: Update the incorrect heading numbers**

Edit only these heading tokens:

```markdown
#### 1.11.1 功能定位
#### 1.11.2 追溯能力分布
#### 1.11.3 运维追溯查询路径
#### 1.11.4 验收标准
```

to:

```markdown
#### 1.12.1 功能定位
#### 1.12.2 追溯能力分布
#### 1.12.3 运维追溯查询路径
#### 1.12.4 验收标准
```

- [ ] **Step 3: Update the duplicate acceptance item number**

In `1.15.7 验收标准`, edit only the final duplicated list marker:

```markdown
12. 平台不基于明细项目判断报告完整性、项目完成状态、项目重复覆盖或项目漏报。
```

to:

```markdown
14. 平台不基于明细项目判断报告完整性、项目完成状态、项目重复覆盖或项目漏报。
```

- [ ] **Step 4: Verify heading continuity**

Run:

```powershell
rg -n "^#{1,4} " '政策文档/版本5/PRD_第五版修订稿2.md'
```

Expected: Under `### 1.12 业务运维与追溯查询（当前版本降级）`, headings are `1.12.1` through `1.12.4`.

- [ ] **Step 5: Verify the edited acceptance list**

Run:

```powershell
$i=0; Get-Content -LiteralPath '政策文档/版本5/PRD_第五版修订稿2.md' | ForEach-Object { $i++; if($i -ge 1902 -and $i -le 1918){ '{0}: {1}' -f $i,$_ } }
```

Expected: `1.15.7 验收标准` list runs from `1.` through `14.` with no duplicate `12.`.

- [ ] **Step 6: Review diff**

Run:

```powershell
git diff -- '政策文档/版本5/PRD_第五版修订稿2.md'
```

Expected: Diff contains only the four `1.12.x` heading number changes and the final `1.15.7` list marker change.
