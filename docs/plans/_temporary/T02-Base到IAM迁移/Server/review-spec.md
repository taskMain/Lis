# 规格轴审查（收尾）

本文件由 lead 直接完成：`spec-reviewer-2` 两次中断，按重新派发上限转由 lead 自查。判定只针对四处，依据为代码事实。

## 一、票 09「统计按组织、医院、院区实例 Id 过滤」— 部分实现（等价达成、字面未达成）

- 事实：`RecognitionUsageSummaryFilter` 六个范围字段仍为 `string?`，名为 `OrganizationCode/HospitalCode/BranchCode` 与 `Source*` 变体（`server/Dy.MedicalRecognition.Domain/Queries/RecognitionUsageSummaryFilter.cs`）；其余三个统计 filter 同构。`MedicalRecognitionReportQuery.xml` 统计语句 where 仍按 code 列等值。
- 等价关系：票 07 起 code 列写入的是实例标识文本，因此按 code 列等值与按 uuid 列等值得到相同结果集。
- 结论：过滤结果等价成立，但判据字面「按实例 Id 过滤」未达成——字段名与比较列都不是实例 Id。统计侧 Guid 化批次已回退，属已知待办。

## 二、票 11「旧列与旧属性删除、旧 Base 依赖零引用」— 未完成

- 旧 code 列：四张表 `organization_code/hospital_code/branch_code` 与 `receiver_*` 在建表脚本与 SqlMap 中仍存在（V10 只读核验已确认库中也仍在）。
- 旧属性：实体与 Command/DTO 仍有编码属性。
- Base 包：`Dy.Base.Application.Contracts` 仍被 Application、Repository 两个工程引用。
- 配置：`appsettings.Development.json` 仍有 Base 用户/组织/系统参数/字典/推送五条代理项。
- 结论：批 A/B1a/B1b/报告列表与统计侧 B2 已完成；批 C（删旧列与编码属性）与批 D（删 Base 包与两个解析器类）已被 lead 冻结，故本判据整体未完成。

## 三、票 08 判据 4「导出请求携带组织实例 Id 且与页面范围一致」— 部分实现

- 事实：三个导出请求新增 `Guid?` 实例 Id 字段（`RecognitionProjectConfigurationExportRequest.cs:26`、`RecognitionAmountExportRequest.cs:40/44/48`、`BranchRecognitionAmountExportRequest.cs:28`），旧编码字段保留；应用层提供实例 Id 时使用并经 `IamReportScopeResolver` 校验归属，缺省回退编码路径。
- 结论：请求可携带实例 Id 且校验成立，但字段可空、不是必填契约，故判「部分实现」。扩展式设计是为避免打断并行中的前端接线，收缩为必填属票 11 批 C。

## 四、票 07 判据 3「版本与详情返回组织实例 Id 与对应名称」— 部分实现

- 实例 Id：`MedicalReportVersionListReadModel`、`MedicalReportVersionDetailQueryReadModel`、`MedicalReportVersionFileReadModel` 各有 `OrganizationId/HospitalId/BranchId` 三个字段，SqlMap 已 select 并由映射填充。
- 名称：上述三个模型**均无** `OrganizationName/HospitalName/BranchName`；只有 `MedicalReportListReadModel` 同时具备三 Id 与三名称（idFields=3, nameFields=3）。
- 结论：实例 Id 已实现，对应名称缺失，判「部分实现」。

## 汇总

| 判据 | 判定 |
| --- | --- |
| 票 09 统计按实例 Id 过滤 | 部分实现（等价达成、字面未达成） |
| 票 11 旧列与旧依赖退出 | 未完成（批 C/D 冻结） |
| 票 08 判据 4 导出请求实例 Id | 部分实现（可空、非必填） |
| 票 07 判据 3 版本与详情返回 Id 与名称 | 部分实现（Id 有、名称缺） |
