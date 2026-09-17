---
name: gitnexus-area-queries
description: "Skill for the Queries area of Lis. 37 symbols across 7 files."
---

# Queries

37 symbols | 7 files | Cohesion: 82%

## When to Use

- Working with code in `server/`
- Understanding how MedicalRecognitionReportQueryRepository, MedicalRecognitionReportQueryAppService, QueryBranchRecognitionAmountListAsync work
- Modifying queries-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | Map, QueryBranchRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync (+2) |
| `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | QueryRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync (+1) |
| `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | QueryRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync (+1) |
| `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs` | QueryRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync (+1) |
| `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | QueryRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync (+1) |
| `server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs` | QueryRecognitionAmountListAsync, QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync |
| `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs` | IMedicalRecognitionReportQueryAppService |

## Entry Points

Start here when exploring this area:

- **`MedicalRecognitionReportQueryRepository`** (Class) — `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs:14`
- **`MedicalRecognitionReportQueryAppService`** (Class) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:14`
- **`QueryBranchRecognitionAmountListAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:192`
- **`QueryRecognitionAmountListAsync`** (Method) — `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs:60`
- **`QueryRecognitionAmountListAsync`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs:375`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MedicalRecognitionReportQueryRepository` | Class | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 14 |
| `MedicalRecognitionReportQueryAppService` | Class | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 14 |
| `IMedicalRecognitionReportQueryRepository` | Interface | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 6 |
| `IMedicalRecognitionReportQueryAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs` | 5 |
| `QueryBranchRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 192 |
| `QueryRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 60 |
| `QueryRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs` | 375 |
| `QueryRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | 744 |
| `QueryRecognitionAmountListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs` | 351 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 106 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 46 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs` | 371 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | 764 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs` | 371 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 64 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 34 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs` | 359 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | 752 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs` | 359 |
| `QueryMedicalStandardGroupListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 78 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `QueryBranchRecognitionAmountListAsync → TrimCode` | cross_community | 5 |
| `QueryBranchRecognitionAmountListAsync → TrustedScope` | cross_community | 4 |
| `QueryBranchRecognitionAmountListAsync → TargetOrganization` | cross_community | 4 |
| `QueryBranchRecognitionAmountListAsync → TrimCode` | cross_community | 4 |
| `QueryBranchRecognitionAmountListAsync → AddTarget` | cross_community | 4 |
| `QueryBranchRecognitionAmountListAsync → FindBranch` | cross_community | 3 |
| `QueryBranchRecognitionAmountListAsync → FindHospital` | cross_community | 3 |
| `QueryBranchRecognitionAmountListAsync → FindOrganization` | cross_community | 3 |

## How to Explore

1. `context({name: "MedicalRecognitionReportQueryRepository"})` — see callers and callees
2. `query({search_query: "queries"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
