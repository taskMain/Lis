---
name: gitnexus-area-medicalrecognitionreportaggregate
description: "Skill for the MedicalRecognitionReportAggregate area of Lis. 42 symbols across 9 files."
---

# MedicalRecognitionReportAggregate

42 symbols | 9 files | Cohesion: 78%

## When to Use

- Working with code in `server/`
- Understanding how MedicalRecognitionReportAppService, CreateMutualRecognitionItemAsync, CreateMutualRecognitionItemsAsync work
- Modifying medicalrecognitionreportaggregate-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Matching.cs` | AssembleMatchReportsAsync, BuildPdfDownloadUrl, GetReportFactsByVersionAsync, GroupExaminationItemsByVersion, GroupLaboratoryResultsByVersion (+7) |
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | CreateMutualRecognitionItemAsync, CreateMutualRecognitionItemsAsync, DisableMutualRecognitionItemAsync, EnableMutualRecognitionItemAsync, UpdateMutualRecognitionItemConfigurationAsync (+4) |
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs` | EnsurePdfFileReadableAsync, ResolveOperatorId, SubmitCompleteExaminationReportAsync, SubmitCompleteLaboratoryReportAsync, VoidExaminationReportAsync (+1) |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | QueryRecognitionProjectConfigurationListAsync, QuerySelectableStandardItemListAsync, Map, QueryBranchRecognitionAmountListAsync, QueryRecognitionAmountListAsync |
| `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs` | DeleteAsync, OpenReadAsync, ResolvePathOrThrow, SaveAsync, TryDelete |
| `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | FindBranch, ResolveOrThrow |
| `server/Dy.MedicalRecognition.Application/TrustedOrganizationResolver.cs` | ResolveOrThrow |
| `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/ReportPdfFileOptions.cs` | ResolveRootDirectoryOrThrow |
| `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.ProcessingResult.cs` | IMedicalRecognitionReportAppService |

## Entry Points

Start here when exploring this area:

- **`MedicalRecognitionReportAppService`** (Class) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:22`
- **`CreateMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:333`
- **`CreateMutualRecognitionItemsAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:368`
- **`DisableMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:438`
- **`EnableMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:415`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MedicalRecognitionReportAppService` | Class | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 22 |
| `IMedicalRecognitionReportAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.ProcessingResult.cs` | 12 |
| `CreateMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 333 |
| `CreateMutualRecognitionItemsAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 368 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 438 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 415 |
| `UpdateMutualRecognitionItemConfigurationAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 391 |
| `QueryRecognitionProjectConfigurationListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 148 |
| `QuerySelectableStandardItemListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 178 |
| `SaveBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 504 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 468 |
| `QueryBranchRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 263 |
| `QueryRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 228 |
| `ResolveOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 44 |
| `SubmitCompleteExaminationReportAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs` | 82 |
| `SubmitCompleteLaboratoryReportAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs` | 46 |
| `VoidExaminationReportAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs` | 145 |
| `VoidLaboratoryReportAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Submission.cs` | 117 |
| `DeleteAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs` | 65 |
| `OpenReadAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs` | 54 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 5 |
| `QueryBranchRecognitionAmountListAsync → TrimCode` | cross_community | 5 |
| `SaveBranchRecognitionAmountAsync → TargetOrganization` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → AddTarget` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrustedScope` | cross_community | 4 |
| `SubmitExaminationReportAsync → ResolvePathOrThrow` | cross_community | 4 |
| `SubmitExaminationReportAsync → TryDelete` | cross_community | 4 |
| `SubmitExaminationReportAsync → ResolveRootDirectoryOrThrow` | cross_community | 4 |
| `SubmitCompleteExaminationReportAsync → IsSimple` | cross_community | 4 |

## How to Explore

1. `context({name: "MedicalRecognitionReportAppService"})` — see callers and callees
2. `query({search_query: "medicalrecognitionreportaggregate"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
