---
name: gitnexus-area-managers
description: "Skill for the Managers area of Lis. 77 symbols across 15 files."
---

# Managers

77 symbols | 15 files | Cohesion: 96%

## When to Use

- Working with code in `server/`
- Understanding how MutualRecognitionItem, CreateRecognitionProcessingResultsSavedEvent, SubmitRecognitionProcessingResultsAsync work
- Modifying managers-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.SubmissionValidation.cs` | NormalizeCode, NormalizeDocumentNo, NormalizeName, RequireDistinct, ValidateLaboratoryDetailUniqueness (+10) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Submission.cs` | AppendVersionAsync, EnsureAffectedOneRowAsync, IsSamePatientIdentity, ResolvePlatformPatientAsync, SubmitCompleteExaminationReportAsync (+9) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.ProcessingResult.cs` | BuildMatchItemIdsMessage, FindProcessingResultViolation, FindSubmittedItemsViolation, ReadAdoptedProjectAmountsAsync, SaveProcessingResultsAsync (+7) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | CreateMutualRecognitionItemAsync, DisableMutualRecognitionItemAsync, EnableMutualRecognitionItemAsync, ResolveConcurrentStateChangeAsync, ValidateStandardCatalogAsync (+7) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Reference.cs` | FindReferenceLookupViolation, FindReferenceViolation, IsReferenceOwnershipMatched, SaveRecognitionReferencesAsync, SortedReferenceItems (+4) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Matching.cs` | BuildUnavailableProjectCodeMessage, IsMatchableCandidate, ListCandidatesAsync, RequestRecognitionMatchesAsync, SelectCandidateForProject (+1) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionProcessingResultsCommand.cs` | CreateRecognitionProcessingResultsSavedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionReferencesCommand.cs` | CreateRecognitionReferencesRecordedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMutualRecognitionItemCommand.cs` | CreateMutualRecognitionItemCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMutualRecognitionItemCommand.cs` | CreateMutualRecognitionItemDisabledEvent |

## Entry Points

Start here when exploring this area:

- **`MutualRecognitionItem`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MutualRecognitionItem.cs:5`
- **`CreateRecognitionProcessingResultsSavedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionProcessingResultsCommand.cs:88`
- **`SubmitRecognitionProcessingResultsAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.ProcessingResult.cs:57`
- **`CreateRecognitionReferencesRecordedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionReferencesCommand.cs:61`
- **`SubmitRecognitionReferencesAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Reference.cs:51`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MutualRecognitionItem` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MutualRecognitionItem.cs` | 5 |
| `CreateRecognitionProcessingResultsSavedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionProcessingResultsCommand.cs` | 88 |
| `SubmitRecognitionProcessingResultsAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.ProcessingResult.cs` | 57 |
| `CreateRecognitionReferencesRecordedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SubmitRecognitionReferencesCommand.cs` | 61 |
| `SubmitRecognitionReferencesAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Reference.cs` | 51 |
| `CreateMutualRecognitionItemCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMutualRecognitionItemCommand.cs` | 51 |
| `CreateMutualRecognitionItemDisabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMutualRecognitionItemCommand.cs` | 40 |
| `CreateMutualRecognitionItemEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMutualRecognitionItemCommand.cs` | 39 |
| `CreateMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 255 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 492 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 463 |
| `SubmitCompleteExaminationReportAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Submission.cs` | 74 |
| `SubmitCompleteLaboratoryReportAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Submission.cs` | 44 |
| `CreateRecognitionMatchesReturnedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/QueryRecognitionMatchesCommand.cs` | 79 |
| `RequestRecognitionMatchesAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Matching.cs` | 62 |
| `CreateMutualRecognitionItemsCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMutualRecognitionItemsCommand.cs` | 46 |
| `CreateMutualRecognitionItemsAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 294 |
| `CreateOrganizationHospitalBranchRecognitionAmountSavedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/SaveOrganizationHospitalBranchRecognitionAmountCommand.cs` | 54 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 565 |
| `VoidExaminationReportAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.Submission.cs` | 109 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SubmitRecognitionProcessingResultsAsync → BuildMatchItemIdsMessage` | intra_community | 4 |
| `SubmitRecognitionReferencesAsync → IsReferenceOwnershipMatched` | intra_community | 3 |
| `SubmitCompleteExaminationReportAsync → IsSamePatientIdentity` | intra_community | 3 |
| `SubmitCompleteExaminationReportAsync → EnsureAffectedOneRowAsync` | intra_community | 3 |
| `CreateMutualRecognitionItemsAsync → MutualRecognitionItem` | intra_community | 3 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → CreateOrganizationHospitalBranchRecognitionAmountSavedEvent` | intra_community | 3 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetAmountByBusinessKeyAsync` | intra_community | 3 |
| `SubmitCompleteLaboratoryReportAsync → IsSamePatientIdentity` | intra_community | 3 |
| `SubmitCompleteLaboratoryReportAsync → EnsureAffectedOneRowAsync` | intra_community | 3 |

## How to Explore

1. `context({name: "MutualRecognitionItem"})` — see callers and callees
2. `query({search_query: "managers"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
