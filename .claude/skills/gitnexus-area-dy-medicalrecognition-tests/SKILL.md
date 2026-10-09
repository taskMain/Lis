---
name: gitnexus-area-dy-medicalrecognition-tests
description: "Skill for the Dy.MedicalRecognition.Tests area of Lis. 1049 symbols across 69 files."
---

# Dy.MedicalRecognition.Tests

1049 symbols | 69 files | Cohesion: 84%

## When to Use

- Working with code in `server/`
- Understanding how PlatformPatient, RecognitionMatchRecord, RecognitionMatchItem work
- Modifying dy.medicalrecognition.tests-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Tests/Stage5WritePathTests.cs` | MatchFixture, AddLaboratoryReportFacts, AssertNothingWritten, BuildRequest, Candidate_selection_breaks_ties_by_report_version_id_ascending (+114) |
| `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | Branch_export_injects_local_branch_filter_when_provided, Branch_export_injects_local_source_branch_filter_when_provided, Branch_export_injects_trusted_receiver_scope_and_resolves_source_group, Branch_export_locks_source_side_to_the_trusted_scope, Branch_source_details_inject_trusted_source_scope_and_push_receiver_filters (+93) |
| `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | Batch_create_request_declares_collection_and_positive_duration_rules, Batch_create_request_rejects_code_collections_above_the_declared_limit, Duration_days_must_be_a_positive_integer_on_create_and_update_requests, AddConfiguration, Batch_create_never_partially_writes_on_any_rejection (+82) |
| `server/Dy.MedicalRecognition.Tests/Stage4WritePathTests.cs` | Abnormal_flag_accepts_the_four_defined_values, BacteriaResult, Bacteria_result_required_fields_reject_the_report, CommonVersion, CreateManager (+68) |
| `server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs` | DesignPath, EntityColumnOrderViolations, EntityMapPath, EntityMapStatement, Recognition_sql_maps_freeze_scopes_and_statement_ids (+55) |
| `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | Amount_request_accepts_zero_and_regular_amount, GetOrganizationHospitalBranchRecognitionAmountByBusinessKeyAsync, AddMutualRecognitionConfiguration, AddSavedAmount, Amount_request_declares_required_amount (+47) |
| `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | Amount_query_requests_expose_only_the_designed_members, AssertInvalid, CountingOrganizationAppService, AddOtherOrganization, AddTrustedOrganization (+37) |
| `server/Dy.MedicalRecognition.Tests/Stage5QueryTests.cs` | CitationFixture, All_adopted_items_with_invalidated_versions_report_the_version_reason_alone, AssertNothingWritten, BuildRequest, BuildSeed (+30) |
| `server/Dy.MedicalRecognition.Tests/Stage5ConstraintTests.cs` | Host_keeps_the_frozen_handwritten_controller_set, Shared_syntax_guard_is_reused_without_a_second_declaration, TransactionDeclarations, TransactionDeclarationsOf, Transaction_declarations_match_the_frozen_distribution (+24) |
| `server/Dy.MedicalRecognition.Tests/Stage5RequestAndReadModelContractTests.cs` | BusinessFieldNames, Citation_detail_request_omits_platform_patient_and_match_locators, DeclaredPropertyNames, Four_requests_carry_no_trusted_scope_or_operator_field, Four_requests_expose_exactly_the_v81_field_set (+24) |

## Entry Points

Start here when exploring this area:

- **`PlatformPatient`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/PlatformPatient.cs:8`
- **`RecognitionMatchRecord`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionMatchRecord.cs:8`
- **`RecognitionMatchItem`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionMatchItem.cs:8`
- **`RecognitionProcessingResult`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionProcessingResult.cs:8`
- **`RecognitionReference`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionReference.cs:8`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `PlatformPatient` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/PlatformPatient.cs` | 8 |
| `RecognitionMatchRecord` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionMatchRecord.cs` | 8 |
| `RecognitionMatchItem` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionMatchItem.cs` | 8 |
| `RecognitionProcessingResult` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionProcessingResult.cs` | 8 |
| `RecognitionReference` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/RecognitionReference.cs` | 8 |
| `OrganizationHospitalBranchRecognitionAmount` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/OrganizationHospitalBranchRecognitionAmount.cs` | 8 |
| `LocalReportPdfFileStore` | Class | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/LocalReportPdfFileStore.cs` | 13 |
| `MedicalRecognitionReportQueryAppService` | Class | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.Citation.cs` | 20 |
| `Stage5SqlMapTests` | Class | `server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs` | 23 |
| `MedicalRecognitionReportQueryRepository` | Class | `server/Dy.MedicalRecognition.Repository/Queries/MedicalRecognitionReportQueryRepository.cs` | 15 |
| `MedicalRecognitionReportRepository` | Class | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 18 |
| `MedicalRecognitionReportAppService` | Class | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.Matching.cs` | 24 |
| `IReportPdfFileStore` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/IReportPdfFileStore.cs` | 9 |
| `IMedicalRecognitionReportQueryRepository` | Interface | `server/Dy.MedicalRecognition.Domain/Queries/Ports/IMedicalRecognitionReportQueryRepository.Citation.cs` | 10 |
| `IMedicalRecognitionReportRepository` | Interface | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Ports/IMedicalRecognitionReportRepository.Matching.cs` | 10 |
| `Branch_export_injects_local_branch_filter_when_provided` | Method | `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | 2440 |
| `Branch_export_injects_local_source_branch_filter_when_provided` | Method | `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | 2493 |
| `Branch_export_injects_trusted_receiver_scope_and_resolves_source_group` | Method | `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | 2405 |
| `Branch_export_locks_source_side_to_the_trusted_scope` | Method | `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | 2465 |
| `Branch_source_details_inject_trusted_source_scope_and_push_receiver_filters` | Method | `server/Dy.MedicalRecognition.Tests/Stage6StatisticsQueryTests.cs` | 1600 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SubmitRecognitionProcessingResultsAsync → IsSimple` | intra_community | 4 |
| `SubmitCompleteExaminationReportAsync → IsSimple` | cross_community | 4 |
| `SubmitCompleteLaboratoryReportAsync → IsSimple` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → IsSimple` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → IsSimple` | cross_community | 4 |
| `QueryBranchMedicalReportListAsync → IsSimple` | cross_community | 4 |
| `QueryMedicalReportListAsync → IsSimple` | cross_community | 4 |
| `QueryMedicalReportVersionDetailAsync → IsSimple` | cross_community | 4 |
| `QueryMedicalReportVersionListAsync → IsSimple` | cross_community | 4 |
| `QueryBranchRecognitionUsageDetailsAsync → IsSimple` | cross_community | 4 |

## How to Explore

1. `context({name: "PlatformPatient"})` — see callers and callees
2. `query({search_query: "dy.medicalrecognition.tests"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
