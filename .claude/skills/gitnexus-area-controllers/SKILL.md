---
name: gitnexus-area-controllers
description: "Skill for the Controllers area of Lis. 13 symbols across 5 files."
---

# Controllers

13 symbols | 5 files | Cohesion: 92%

## When to Use

- Working with code in `server/`
- Understanding how SaveAsync, SaveAsync, SubmitExaminationReportAsync work
- Modifying controllers-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | ReadReportNo, ResolveDownloadFileName, SanitizeReportNoForFileName, SubmitExaminationReportAsync, SubmitLaboratoryReportAsync (+2) |
| `server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs` | CreateExportResponse, ExportBranchStatisticsAsync, ExportPlatformStatisticsAsync |
| `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IReportPdfFileStore.cs` | SaveAsync |
| `server/Dy.MedicalRecognition.Tests/Stage4SubmissionPathTests.cs` | SaveAsync |
| `server/Dy.MedicalRecognition.Tests/StubReportPorts.cs` | SaveAsync |

## Entry Points

Start here when exploring this area:

- **`SaveAsync`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage4SubmissionPathTests.cs:534`
- **`SaveAsync`** (Method) — `server/Dy.MedicalRecognition.Tests/StubReportPorts.cs:17`
- **`SubmitExaminationReportAsync`** (Method) — `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs:101`
- **`SubmitLaboratoryReportAsync`** (Method) — `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs:87`
- **`ExportBranchStatisticsAsync`** (Method) — `server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs:64`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `SaveAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage4SubmissionPathTests.cs` | 534 |
| `SaveAsync` | Method | `server/Dy.MedicalRecognition.Tests/StubReportPorts.cs` | 17 |
| `SubmitExaminationReportAsync` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 101 |
| `SubmitLaboratoryReportAsync` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 87 |
| `ExportBranchStatisticsAsync` | Method | `server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs` | 64 |
| `ExportPlatformStatisticsAsync` | Method | `server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs` | 48 |
| `SaveAsync` | Method | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IReportPdfFileStore.cs` | 21 |
| `ReadReportNo` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 216 |
| `ResolveDownloadFileName` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 247 |
| `SanitizeReportNoForFileName` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 268 |
| `SubmitReportAsync` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 143 |
| `ValidatePdfFile` | Method | `server/Dy.MedicalRecognition/Controllers/ReportPdfFileController.cs` | 191 |
| `CreateExportResponse` | Method | `server/Dy.MedicalRecognition/Controllers/RecognitionStatisticsExportController.cs` | 80 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SubmitExaminationReportAsync → ResolvePathOrThrow` | cross_community | 4 |
| `SubmitExaminationReportAsync → TryDelete` | cross_community | 4 |
| `SubmitExaminationReportAsync → ResolveRootDirectoryOrThrow` | cross_community | 4 |
| `SubmitLaboratoryReportAsync → ResolvePathOrThrow` | cross_community | 4 |
| `SubmitLaboratoryReportAsync → TryDelete` | cross_community | 4 |
| `SubmitLaboratoryReportAsync → ResolveRootDirectoryOrThrow` | cross_community | 4 |
| `SubmitExaminationReportAsync → SaveAsync` | intra_community | 3 |
| `SubmitExaminationReportAsync → SaveAsync` | intra_community | 3 |
| `SubmitExaminationReportAsync → SaveAsync` | intra_community | 3 |
| `SubmitLaboratoryReportAsync → SaveAsync` | intra_community | 3 |

## How to Explore

1. `context({name: "SaveAsync"})` — see callers and callees
2. `query({search_query: "controllers"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
