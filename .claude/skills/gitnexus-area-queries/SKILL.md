---
name: gitnexus-area-queries
description: "Skill for the Queries area of Lis. 95 symbols across 8 files."
---

# Queries

95 symbols | 8 files | Cohesion: 80%

## When to Use

- Working with code in `server/`
- Understanding how MedicalRecognitionReportQueryAppService, QueryMedicalReportVersionDetailAsync, QueryMedicalReportVersionListAsync work
- Modifying queries-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | BuildUsageDetailPage, MapAdoptionDetail, MapMatchRecordItem, MapNonAdoptionDetail, MapReceiverOrganization (+31) |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.StatisticsExport.cs` | AppendExportTypeTailCells, BuildSourceDetailRow, BuildSourceSummaryRow, BuildUsageDetailFilter, BuildUsageDetailRow (+19) |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` | EnsureReportInTrustedScopeAsync, NullIfBlank, QueryMedicalReportVersionDetailAsync, QueryMedicalReportVersionListAsync, ReadCommonAsync (+16) |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs` | BuildCitationDetailAsync, BuildPdfDownloadUrl, GroupExaminationItemsByVersion, GroupLaboratoryResultsByVersion, MapCitationItem (+4) |
| `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | MapConfigurationStatusToIsValid, QueryRecognitionProjectConfigurationListAsync |
| `server/Dy.MedicalRecognition.Application/Queries/PageQueryWindow.cs` | Create |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | MedicalRecognitionReportQueryAppService |
| `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Citation.cs` | IMedicalRecognitionReportQueryAppService |

## Entry Points

Start here when exploring this area:

- **`MedicalRecognitionReportQueryAppService`** (Class) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:20`
- **`QueryMedicalReportVersionDetailAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs:151`
- **`QueryMedicalReportVersionListAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs:108`
- **`QueryRecognitionMatchRecordAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs:431`
- **`QueryRecognitionUsageDetailsAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs:87`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MedicalRecognitionReportQueryAppService` | Class | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 20 |
| `IMedicalRecognitionReportQueryAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.Citation.cs` | 13 |
| `QueryMedicalReportVersionDetailAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` | 151 |
| `QueryMedicalReportVersionListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` | 108 |
| `QueryRecognitionMatchRecordAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 431 |
| `QueryRecognitionUsageDetailsAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 87 |
| `QueryRecognitionUsageSummaryAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 36 |
| `QuerySourceRecognitionDetailsAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 194 |
| `QuerySourceRecognitionSummaryAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 143 |
| `QueryBranchRecognitionUsageDetailsAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 290 |
| `QueryBranchRecognitionUsageSummaryAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 244 |
| `QueryBranchSourceRecognitionDetailsAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 388 |
| `QueryBranchSourceRecognitionSummaryAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Statistics.cs` | 340 |
| `QueryMedicalReportListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` | 27 |
| `QueryBranchMedicalReportListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs` | 68 |
| `GetBranchStatisticsExportAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.StatisticsExport.cs` | 116 |
| `GetStatisticsExportAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.StatisticsExport.cs` | 48 |
| `MapConfigurationStatusToIsValid` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 87 |
| `QueryRecognitionProjectConfigurationListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 55 |
| `AppendExportTypeTailCells` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.StatisticsExport.cs` | 701 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `GetBranchStatisticsExportAsync → AppendExportTypeTailCells` | cross_community | 6 |
| `GetBranchStatisticsExportAsync → WriteExportCell` | cross_community | 6 |
| `GetBranchStatisticsExportAsync → StatisticsExportFileReadModel` | cross_community | 6 |
| `GetStatisticsExportAsync → AppendExportTypeTailCells` | cross_community | 6 |
| `GetStatisticsExportAsync → WriteExportCell` | cross_community | 6 |
| `GetStatisticsExportAsync → StatisticsExportFileReadModel` | cross_community | 6 |
| `QueryMedicalReportVersionDetailAsync → GetOrNull` | cross_community | 5 |
| `GetBranchStatisticsExportAsync → GetUsageDetailHeaders` | cross_community | 5 |
| `GetBranchStatisticsExportAsync → BuildUsageSummaryRow` | cross_community | 5 |
| `GetStatisticsExportAsync → GetUsageDetailHeaders` | cross_community | 5 |

## How to Explore

1. `context({name: "MedicalRecognitionReportQueryAppService"})` — see callers and callees
2. `query({search_query: "queries"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
