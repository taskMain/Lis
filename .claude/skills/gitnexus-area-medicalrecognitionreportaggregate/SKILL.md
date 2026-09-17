---
name: gitnexus-area-medicalrecognitionreportaggregate
description: "Skill for the MedicalRecognitionReportAggregate area of Lis. 152 symbols across 22 files."
---

# MedicalRecognitionReportAggregate

152 symbols | 22 files | Cohesion: 65%

## When to Use

- Working with code in `server/`
- Understanding how OrganizationHospitalBranchRecognitionAmount, MedicalRecognitionReportRepository, MedicalRecognitionReportAppService work
- Modifying medicalrecognitionreportaggregate-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | GetMutualRecognitionItemByOrganizationAndProjectAsync, UpdateOrganizationHospitalBranchRecognitionAmountAsync, EnableMedicalStandardGroupAsync, GetMedicalStandardGroupByIdAsync, MedicalStandardCategoryHasGroupsAsync (+22) |
| `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | CreateOrganizationHospitalBranchRecognitionAmountAsync, GetMutualRecognitionItemByOrganizationAndProjectAsync, UpdateOrganizationHospitalBranchRecognitionAmountAsync, EnableMedicalStandardGroupAsync, GetMedicalStandardGroupByIdAsync (+22) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/IMedicalRecognitionReportRepository.cs` | CreateOrganizationHospitalBranchRecognitionAmountAsync, GetMutualRecognitionItemByOrganizationAndProjectAsync, UpdateOrganizationHospitalBranchRecognitionAmountAsync, EnableMedicalStandardGroupAsync, GetMedicalStandardGroupByIdAsync (+21) |
| `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | CreateOrganizationHospitalBranchRecognitionAmountAsync, GetMutualRecognitionItemByOrganizationAndProjectAsync, UpdateOrganizationHospitalBranchRecognitionAmountAsync, EnableMedicalStandardGroupAsync, GetMedicalStandardCategoryByIdAsync (+21) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | ResolveConcurrentAmountChangeAsync, SaveOrganizationHospitalBranchRecognitionAmountAsync, EnableMedicalStandardGroupAsync, ValidateStandardCatalogAsync, UpdateMedicalStandardCategoryAsync (+12) |
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | EnableMedicalStandardGroupAsync, UpdateMedicalStandardCategoryAsync, CreateMedicalStandardCategoryAsync, CreateMedicalStandardItemAsync, UpdateMedicalStandardGroupAsync (+8) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SaveOrganizationHospitalBranchRecognitionAmountCommand.cs` | CreateOrganizationHospitalBranchRecognitionAmountSavedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | CreateMedicalStandardGroupEnabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/UpdateMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryUpdatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryCreatedEvent |

## Entry Points

Start here when exploring this area:

- **`OrganizationHospitalBranchRecognitionAmount`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/OrganizationHospitalBranchRecognitionAmount.cs:8`
- **`MedicalRecognitionReportRepository`** (Class) — `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs:15`
- **`MedicalRecognitionReportAppService`** (Class) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:18`
- **`CreateOrganizationHospitalBranchRecognitionAmountSavedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SaveOrganizationHospitalBranchRecognitionAmountCommand.cs:54`
- **`SaveOrganizationHospitalBranchRecognitionAmountAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs:400`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `OrganizationHospitalBranchRecognitionAmount` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/OrganizationHospitalBranchRecognitionAmount.cs` | 8 |
| `MedicalRecognitionReportRepository` | Class | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 15 |
| `MedicalRecognitionReportAppService` | Class | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 18 |
| `IMedicalRecognitionReportRepository` | Interface | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/IMedicalRecognitionReportRepository.cs` | 9 |
| `IMedicalRecognitionReportAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IMedicalRecognitionReportAppService.cs` | 11 |
| `CreateOrganizationHospitalBranchRecognitionAmountSavedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SaveOrganizationHospitalBranchRecognitionAmountCommand.cs` | 54 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 400 |
| `GetMutualRecognitionItemByOrganizationAndProjectAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 138 |
| `UpdateOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 176 |
| `CreateOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1563 |
| `GetMutualRecognitionItemByOrganizationAndProjectAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1549 |
| `UpdateOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1568 |
| `CreateOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | 1202 |
| `GetMutualRecognitionItemByOrganizationAndProjectAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | 1242 |
| `UpdateOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | 1223 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 178 |
| `CreateMedicalStandardGroupEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | 31 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 141 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 62 |
| `GetMedicalStandardGroupByIdAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 80 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `ResolveConcurrentAmountChangeAsync → OrganizationHospitalBranchRecognitionAmount` | cross_community | 4 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | cross_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | cross_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | intra_community | 3 |
| `ChangeMedicalStandardItemRemarkAsync → GetMedicalStandardItemByIdAsync` | cross_community | 3 |
| `CreateMedicalStandardCategoryAsync → MedicalStandardCategoryNameExistsAsync` | intra_community | 3 |

## How to Explore

1. `context({name: "OrganizationHospitalBranchRecognitionAmount"})` — see callers and callees
2. `query({search_query: "medicalrecognitionreportaggregate"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
