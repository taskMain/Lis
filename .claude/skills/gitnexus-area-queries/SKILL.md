---
name: gitnexus-area-queries
description: "Skill for the Queries area of Lis. 19 symbols across 4 files."
---

# Queries

19 symbols | 4 files | Cohesion: 89%

## When to Use

- Working with code in `server/`
- Understanding how MedicalRecognitionReportQueryAppService, MedicalRecognitionReportQueryRepository, QueryEffectiveMedicalStandardCatalogAsync work
- Modifying queries-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, ValidateOptionalItemType, QueryMedicalStandardGroupListAsync, ValidateOptionalId (+3) |
| `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync, IMedicalRecognitionReportQueryRepository |
| `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | QueryEffectiveMedicalStandardCatalogAsync, QueryMedicalStandardCategoryListAsync, QueryMedicalStandardGroupListAsync, QueryMedicalStandardItemListAsync, MedicalRecognitionReportQueryRepository |
| `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs` | IMedicalRecognitionReportQueryAppService |

## Entry Points

Start here when exploring this area:

- **`MedicalRecognitionReportQueryAppService`** (Class) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:5`
- **`MedicalRecognitionReportQueryRepository`** (Class) — `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs:7`
- **`QueryEffectiveMedicalStandardCatalogAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:33`
- **`QueryMedicalStandardCategoryListAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:11`
- **`QueryEffectiveMedicalStandardCatalogAsync`** (Method) — `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs:23`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MedicalRecognitionReportQueryAppService` | Class | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 5 |
| `MedicalRecognitionReportQueryRepository` | Class | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 7 |
| `IMedicalRecognitionReportQueryAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/Queries/IMedicalRecognitionReportQueryAppService.cs` | 5 |
| `IMedicalRecognitionReportQueryRepository` | Interface | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 2 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 33 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 11 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 23 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 14 |
| `QueryMedicalStandardGroupListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 18 |
| `QueryMedicalStandardGroupListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 17 |
| `QueryMedicalStandardItemListAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 25 |
| `QueryMedicalStandardItemListAsync` | Method | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 20 |
| `ValidateOptionalItemType` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 91 |
| `QueryEffectiveMedicalStandardCatalogAsync` | Method | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 7 |
| `QueryMedicalStandardCategoryListAsync` | Method | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 4 |
| `ValidateOptionalId` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 81 |
| `QueryMedicalStandardGroupListAsync` | Method | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 5 |
| `ValidateOptionalText` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 86 |
| `QueryMedicalStandardItemListAsync` | Method | `server/Dy.MedicalRecognition.Domain/Queries/IMedicalRecognitionReportQueryRepository.cs` | 6 |

## How to Explore

1. `context({name: "MedicalRecognitionReportQueryAppService"})` — see callers and callees
2. `query({search_query: "queries"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
