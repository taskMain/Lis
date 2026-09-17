---
name: gitnexus-area-enummetadata
description: "Skill for the EnumMetadata area of Lis. 7 symbols across 4 files."
---

# EnumMetadata

7 symbols | 4 files | Cohesion: 100%

## When to Use

- Working with code in `server/`
- Understanding how EnumMetadataAppService, IEnumMetadataAppService work
- Modifying enummetadata-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/EnumMetadataAppService.cs` | EnumMetadataAppService, ToMetadata |
| `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs` | MedicalRecognitionEnumDescriptorRegistry, ToReadOnly |
| `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/EnumDescriptorText.cs` | Get, GetOrNull |
| `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/IEnumMetadataAppService.cs` | IEnumMetadataAppService |

## Entry Points

Start here when exploring this area:

- **`EnumMetadataAppService`** (Class) — `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/EnumMetadataAppService.cs:14`
- **`IEnumMetadataAppService`** (Interface) — `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/IEnumMetadataAppService.cs:5`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `EnumMetadataAppService` | Class | `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/EnumMetadataAppService.cs` | 14 |
| `IEnumMetadataAppService` | Interface | `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/IEnumMetadataAppService.cs` | 5 |
| `MedicalRecognitionEnumDescriptorRegistry` | Class | `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs` | 13 |
| `ToMetadata` | Method | `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/EnumMetadataAppService.cs` | 41 |
| `ToReadOnly` | Method | `server/Dy.MedicalRecognition.Application/Queries/EnumMetadata/MedicalRecognitionEnumDescriptorRegistry.cs` | 32 |
| `Get` | Method | `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/EnumDescriptorText.cs` | 31 |
| `GetOrNull` | Method | `server/Dy.MedicalRecognition.Application.Contracts/Queries/EnumMetadata/EnumDescriptorText.cs` | 50 |

## How to Explore

1. `context({name: "EnumMetadataAppService"})` — see callers and callees
2. `query({search_query: "enummetadata"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
