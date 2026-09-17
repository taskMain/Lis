---
name: gitnexus-area-architecture
description: "Skill for the Architecture area of Lis. 23 symbols across 5 files."
---

# Architecture

23 symbols | 5 files | Cohesion: 72%

## When to Use

- Working with code in `server/`
- Understanding how Shared_guard_collects_every_declaration_part_of_a_type, Application_module_registers_the_enum_open_api_transformer, Enum_open_api_registration_check_covers_partial_parts_and_declaration_counts work
- Modifying architecture-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | Parse, ReadTypeDeclarations, TypeArguments, FindMethods, FindSingleMethod (+4) |
| `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | Shared_guard_collects_every_declaration_part_of_a_type, Shared_guard_method_lookup_requires_exactly_one_declaration, Architecture_guards_reuse_the_shared_source_guard, FindReuseViolations, ReferencesSharedGuard (+2) |
| `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` | Application_module_registers_the_enum_open_api_transformer, Enum_open_api_registration_check_covers_partial_parts_and_declaration_counts, Enum_open_api_registration_check_rejects_missing_or_replaced_registration, RegistersEnumOpenApiTransformer |
| `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs` | NormalizeSignature, Query_contract_method_signatures_are_frozen |
| `server/Dy.MedicalRecognition.Tests/Stage2EnumMetadataQueryTests.cs` | IsStringToTypeDictionary |

## Entry Points

Start here when exploring this area:

- **`Shared_guard_collects_every_declaration_part_of_a_type`** (Method) — `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs:131`
- **`Application_module_registers_the_enum_open_api_transformer`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs:205`
- **`Enum_open_api_registration_check_covers_partial_parts_and_declaration_counts`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs:393`
- **`Enum_open_api_registration_check_rejects_missing_or_replaced_registration`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs:382`
- **`Shared_guard_method_lookup_requires_exactly_one_declaration`** (Method) — `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs:88`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `Shared_guard_collects_every_declaration_part_of_a_type` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 131 |
| `Application_module_registers_the_enum_open_api_transformer` | Method | `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` | 205 |
| `Enum_open_api_registration_check_covers_partial_parts_and_declaration_counts` | Method | `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` | 393 |
| `Enum_open_api_registration_check_rejects_missing_or_replaced_registration` | Method | `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` | 382 |
| `Shared_guard_method_lookup_requires_exactly_one_declaration` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 88 |
| `Query_contract_method_signatures_are_frozen` | Method | `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs` | 229 |
| `Architecture_guards_reuse_the_shared_source_guard` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 149 |
| `Reuse_detection_rejects_local_fake_guard` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 62 |
| `Reuse_detection_rejects_renamed_copy_and_comment_only_reference` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 39 |
| `Parse` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 28 |
| `ReadTypeDeclarations` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 51 |
| `TypeArguments` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 130 |
| `RegistersEnumOpenApiTransformer` | Method | `server/Dy.MedicalRecognition.Tests/Stage2EnumContractTests.cs` | 446 |
| `FindMethods` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 86 |
| `FindSingleMethod` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 103 |
| `Read` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 37 |
| `NormalizeSignature` | Method | `server/Dy.MedicalRecognition.Tests/Stage1ArchitectureTests.cs` | 271 |
| `FindReuseViolations` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 182 |
| `ReferencesSharedGuard` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 235 |
| `CreatesType` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 182 |

## How to Explore

1. `context({name: "Shared_guard_collects_every_declaration_part_of_a_type"})` — see callers and callees
2. `query({search_query: "architecture"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
