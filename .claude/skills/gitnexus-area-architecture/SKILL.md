---
name: gitnexus-area-architecture
description: "Skill for the Architecture area of Lis. 8 symbols across 4 files."
---

# Architecture

8 symbols | 4 files | Cohesion: 58%

## When to Use

- Working with code in `server/`
- Understanding how Shared_guard_method_lookup_requires_exactly_one_declaration, Collection_reads_short_circuit_an_empty_collection work
- Modifying architecture-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | FindMethods, FindSingleMethod, CreatesType, SimpleTypeName, TypeArgumentList |
| `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | Shared_guard_method_lookup_requires_exactly_one_declaration |
| `server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs` | Collection_reads_short_circuit_an_empty_collection |
| `server/Dy.MedicalRecognition.Tests/Stage2EnumMetadataQueryTests.cs` | IsStringToTypeDictionary |

## Entry Points

Start here when exploring this area:

- **`Shared_guard_method_lookup_requires_exactly_one_declaration`** (Method) — `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs:108`
- **`Collection_reads_short_circuit_an_empty_collection`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs:1725`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `Shared_guard_method_lookup_requires_exactly_one_declaration` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceGuardReuseTests.cs` | 108 |
| `Collection_reads_short_circuit_an_empty_collection` | Method | `server/Dy.MedicalRecognition.Tests/Stage5SqlMapTests.cs` | 1725 |
| `FindMethods` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 86 |
| `FindSingleMethod` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 103 |
| `CreatesType` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 182 |
| `SimpleTypeName` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 151 |
| `TypeArgumentList` | Method | `server/Dy.MedicalRecognition.Tests/Architecture/SourceSyntaxGuard.cs` | 167 |
| `IsStringToTypeDictionary` | Method | `server/Dy.MedicalRecognition.Tests/Stage2EnumMetadataQueryTests.cs` | 365 |

## How to Explore

1. `context({name: "Shared_guard_method_lookup_requires_exactly_one_declaration"})` — see callers and callees
2. `query({search_query: "architecture"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
