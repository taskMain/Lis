---
name: gitnexus-area-medicalrecognitionreportaggregate
description: "Skill for the MedicalRecognitionReportAggregate area of Lis. 108 symbols across 21 files."
---

# MedicalRecognitionReportAggregate

108 symbols | 21 files | Cohesion: 70%

## When to Use

- Working with code in `server/`
- Understanding how MedicalRecognitionReportAppService, MedicalRecognitionReportRepository, EnableMedicalStandardGroupAsync work
- Modifying medicalrecognitionreportaggregate-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/IMedicalRecognitionReportRepository.cs` | EnableMedicalStandardGroupAsync, GetMedicalStandardGroupByIdAsync, EnableMedicalStandardCategoryAsync, GetMedicalStandardCategoryByIdAsync, CreateMedicalStandardCategoryAsync (+20) |
| `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | EnableMedicalStandardGroupAsync, GetMedicalStandardGroupByIdAsync, EnableMedicalStandardCategoryAsync, GetMedicalStandardCategoryByIdAsync, CreateMedicalStandardCategoryAsync (+20) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | Business, EnableMedicalStandardGroupAsync, RequireGroupAsync, EnableMedicalStandardCategoryAsync, RequireCategoryAsync (+19) |
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | EnableMedicalStandardGroupAsync, EnableMedicalStandardCategoryAsync, CreateMedicalStandardCategoryAsync, CreateMedicalStandardItemAsync, CreateMutualRecognitionItemAsync (+12) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | CreateMedicalStandardGroupEnabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryEnabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardItemCommand.cs` | CreateMedicalStandardItemCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMutualRecognitionItemCommand.cs` | CreateMutualRecognitionItemCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/UpdateMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryUpdatedEvent |

## Entry Points

Start here when exploring this area:

- **`MedicalRecognitionReportAppService`** (Class) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:9`
- **`MedicalRecognitionReportRepository`** (Class) — `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs:7`
- **`EnableMedicalStandardGroupAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:79`
- **`CreateMedicalStandardGroupEnabledEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs:23`
- **`EnableMedicalStandardGroupAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs:74`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MedicalRecognitionReportAppService` | Class | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 9 |
| `MedicalRecognitionReportRepository` | Class | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 7 |
| `IMedicalRecognitionReportAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.cs` | 6 |
| `IMedicalRecognitionReportRepository` | Interface | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/IMedicalRecognitionReportRepository.cs` | 5 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 79 |
| `CreateMedicalStandardGroupEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | 23 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 74 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 22 |
| `GetMedicalStandardGroupByIdAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 31 |
| `EnableMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 39 |
| `CreateMedicalStandardCategoryEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardCategoryCommand.cs` | 23 |
| `EnableMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 34 |
| `EnableMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 18 |
| `GetMedicalStandardCategoryByIdAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 28 |
| `CreateMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 19 |
| `CreateMedicalStandardCategoryCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs` | 33 |
| `CreateMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 11 |
| `CreateMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 16 |
| `MedicalStandardCategoryNameExistsAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 29 |
| `CreateMedicalStandardItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 99 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `ChangeMedicalStandardItemRemarkAsync → ChangeMedicalStandardItemRemarkAsync` | intra_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | cross_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → ChangeMedicalStandardItemRemarkAsync` | intra_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | cross_community | 3 |
| `CreateMedicalStandardCategoryAsync → CreateGuid` | cross_community | 3 |
| `CreateMedicalStandardCategoryAsync → MedicalStandardCategoryNameExistsAsync` | intra_community | 3 |
| `CreateMedicalStandardCategoryAsync → CreateGuid` | cross_community | 3 |
| `CreateMedicalStandardCategoryAsync → MedicalStandardCategoryNameExistsAsync` | intra_community | 3 |
| `CreateMedicalStandardGroupAsync → MedicalStandardGroupNameExistsAsync` | cross_community | 3 |
| `CreateMedicalStandardGroupAsync → MedicalStandardGroupNameExistsAsync` | cross_community | 3 |

## How to Explore

1. `context({name: "MedicalRecognitionReportAppService"})` — see callers and callees
2. `query({search_query: "medicalrecognitionreportaggregate"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
