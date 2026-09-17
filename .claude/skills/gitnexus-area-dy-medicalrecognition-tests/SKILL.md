---
name: gitnexus-area-dy-medicalrecognition-tests
description: "Skill for the Dy.MedicalRecognition.Tests area of Lis. 344 symbols across 35 files."
---

# Dy.MedicalRecognition.Tests

344 symbols | 35 files | Cohesion: 89%

## When to Use

- Working with code in `server/`
- Understanding how MutualRecognitionItem, CreateMutualRecognitionItemAsync, DisableMutualRecognitionItemAsync work
- Modifying dy.medicalrecognition.tests-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | ApplyConditionalStateChange, DisableMutualRecognitionItemAsync, EnableMutualRecognitionItemAsync, GetMedicalStandardItemByCodeAsync, GetMutualRecognitionItemByIdAsync (+59) |
| `server/Dy.MedicalRecognition.Tests/Stage3WritePathTests.cs` | DisableMutualRecognitionItemAsync, EnableMutualRecognitionItemAsync, GetMedicalStandardItemByCodeAsync, GetMutualRecognitionItemByIdAsync, UpdateMutualRecognitionItemConfigurationAsync (+51) |
| `server/Dy.MedicalRecognition.Tests/Stage3QueryTests.cs` | QueryRecognitionProjectConfigurationListAsync, CountingOrganizationAppService, AddOtherOrganization, AddTrustedOrganization, Amount_list_follows_category_group_item_priority (+30) |
| `server/Dy.MedicalRecognition.Tests/Stage3TrustedScopeTests.cs` | QueryRecognitionProjectConfigurationListAsync, AmountQueryRepository, Branch_query_entrypoint_passes_the_profile_filled_scope_to_the_repository, BuildTwoHospitalService, BuildUserService (+16) |
| `server/Dy.MedicalRecognition.Tests/Stage3OrganizationPathTests.cs` | BuildBranch, BuildHospital, BuildOrganization, BuildPathService, External_read_counts_do_not_grow_with_the_number_of_target_rows (+15) |
| `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | FindHostOutputDirectory, Flatten, GetRegisteredKeys, Invoke, LoadAssembly (+13) |
| `server/Dy.MedicalRecognition.Tests/Stage2QueryTests.cs` | QueryRecognitionProjectConfigurationListAsync, BuildItem, Configuration_list_has_no_unavailable_reason_when_catalog_layers_are_enabled, Configuration_status_filter_maps_to_the_enabled_column, Query (+11) |
| `server/Dy.MedicalRecognition.Tests/Stage2EnumMetadataQueryTests.cs` | Frozen_openapi_exposes_enum_metadata_endpoint_and_read_model_texts, Generated_client_exposes_enum_metadata_api_and_text_fields, ExtensionValues, OpenApi_enum_contract_matches_the_enum_source_declarations, Enum_metadata_registry_consumption_check_rejects_reflection_or_direct_descriptor_use (+8) |
| `server/Dy.MedicalRecognition.Tests/Stage3SqlMapTests.cs` | Amount_ddl_freezes_physical_table_columns_types_and_negatives, Amount_ddl_freezes_table_column_and_index_comments, ExtractCreateTableColumnLines, FindRepositoryFile, Amount_sql_map_freezes_scope_statement_ids_and_removes_unused_query (+7) |
| `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs` | Backend_source_does_not_control_transactions_manually, FindProviderNeutralityViolations, IsProviderNeutralityGovernedFile, Public_types_and_shared_sql_stay_database_provider_neutral, Test_runner_disables_assembly_and_collection_parallelization (+5) |

## Entry Points

Start here when exploring this area:

- **`MutualRecognitionItem`** (Class) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MutualRecognitionItem.cs:5`
- **`CreateMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:293`
- **`DisableMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:365`
- **`EnableMutualRecognitionItemAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:342`
- **`UpdateMutualRecognitionItemConfigurationAsync`** (Method) — `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs:318`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `MutualRecognitionItem` | Class | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MutualRecognitionItem.cs` | 5 |
| `CreateMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 293 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 365 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 342 |
| `UpdateMutualRecognitionItemConfigurationAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 318 |
| `CreateMutualRecognitionItemDisabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMutualRecognitionItemCommand.cs` | 40 |
| `CreateMutualRecognitionItemEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMutualRecognitionItemCommand.cs` | 39 |
| `CreateMutualRecognitionItemConfigurationUpdatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/UpdateMutualRecognitionItemConfigurationCommand.cs` | 42 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 328 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 299 |
| `UpdateMutualRecognitionItemConfigurationAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/MedicalRecognitionReportManager.cs` | 276 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 131 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 125 |
| `GetMedicalStandardItemByCodeAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 86 |
| `GetMutualRecognitionItemByIdAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 113 |
| `UpdateMutualRecognitionItemConfigurationAsync` | Method | `server/Dy.MedicalRecognition.Repository/MedicalRecognitionReportAggregate/MedicalRecognitionReportRepository.cs` | 119 |
| `DisableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1439 |
| `EnableMutualRecognitionItemAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1429 |
| `GetMedicalStandardItemByCodeAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1361 |
| `GetMutualRecognitionItemByIdAsync` | Method | `server/Dy.MedicalRecognition.Tests/Stage2WritePathTests.cs` | 1380 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 5 |
| `SaveBranchRecognitionAmountAsync → TargetOrganization` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → AddTarget` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrustedScope` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → GetMutualRecognitionItemByOrganizationAndProjectAsync` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → TargetOrganization` | cross_community | 4 |

## How to Explore

1. `context({name: "MutualRecognitionItem"})` — see callers and callees
2. `query({search_query: "dy.medicalrecognition.tests"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
