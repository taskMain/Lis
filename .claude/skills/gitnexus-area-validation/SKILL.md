---
name: gitnexus-area-validation
description: "Skill for the Validation area of Lis. 13 symbols across 4 files."
---

# Validation

13 symbols | 4 files | Cohesion: 75%

## When to Use

- Working with code in `server/`
- Understanding how ResolveOrThrow, AddTarget, ResolveOrThrowAsync work
- Modifying validation-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | BuildTargetOrganizations, FindBranch, FindHospital, FindOrganization, ResolveOrThrow (+2) |
| `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | ResolveLayerOrThrow, ResolveOrThrow, ResolveOrThrowAsync, TrimCode |
| `server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs` | IsValid |
| `server/Dy.MedicalRecognition.Tests/RequestValidationProbeTests.cs` | NonEmpty_attribute_semantics |

## Entry Points

Start here when exploring this area:

- **`ResolveOrThrow`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs:44`
- **`AddTarget`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs:166`
- **`ResolveOrThrowAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs:40`
- **`IsValid`** (Method) — `server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs:28`
- **`NonEmpty_attribute_semantics`** (Method) — `server/Dy.MedicalRecognition.Tests/RequestValidationProbeTests.cs:92`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ResolveOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 44 |
| `AddTarget` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 166 |
| `ResolveOrThrowAsync` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 40 |
| `IsValid` | Method | `server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs` | 28 |
| `NonEmpty_attribute_semantics` | Method | `server/Dy.MedicalRecognition.Tests/RequestValidationProbeTests.cs` | 92 |
| `BuildTargetOrganizations` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 183 |
| `FindBranch` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 259 |
| `FindHospital` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 238 |
| `FindOrganization` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 220 |
| `TrimCode` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 212 |
| `ResolveLayerOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 76 |
| `ResolveOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 103 |
| `TrimCode` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 124 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 5 |
| `QueryBranchRecognitionAmountListAsync → TrimCode` | cross_community | 5 |
| `SaveBranchRecognitionAmountAsync → TargetOrganization` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrimCode` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → AddTarget` | cross_community | 4 |
| `SaveBranchRecognitionAmountAsync → TrustedScope` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → TargetOrganization` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → TrimCode` | cross_community | 4 |
| `SaveOrganizationHospitalBranchRecognitionAmountAsync → AddTarget` | cross_community | 4 |
| `QueryBranchRecognitionAmountListAsync → TrustedScope` | cross_community | 4 |

## How to Explore

1. `context({name: "ResolveOrThrow"})` — see callers and callees
2. `query({search_query: "validation"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
