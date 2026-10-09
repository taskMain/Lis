---
name: gitnexus-area-validation
description: "Skill for the Validation area of Lis. 16 symbols across 6 files."
---

# Validation

16 symbols | 6 files | Cohesion: 81%

## When to Use

- Working with code in `server/`
- Understanding how QueryTrustedOrganizationScopeAsync, ResolveOrganizationHospitalOrThrow, ResolveOrganizationOrThrow work
- Modifying validation-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | BuildTargetOrganizations, FindHospital, FindOrganization, ResolveOrganizationHospitalOrThrow, ResolveOrganizationOrThrow (+2) |
| `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | ResolveLayerOrThrow, ResolveOrThrow, ResolveOrThrowAsync, ResolveWithBranchOrThrowAsync, TrimCode |
| `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | QueryTrustedOrganizationScopeAsync |
| `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | ResolveReportScopeAsync |
| `server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs` | IsValid |
| `server/Dy.MedicalRecognition.Tests/RequestValidationProbeTests.cs` | NonEmpty_attribute_semantics |

## Entry Points

Start here when exploring this area:

- **`QueryTrustedOrganizationScopeAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs:200`
- **`ResolveOrganizationHospitalOrThrow`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs:122`
- **`ResolveOrganizationOrThrow`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs:182`
- **`AddTarget`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs:238`
- **`ResolveOrThrowAsync`** (Method) — `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs:42`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `QueryTrustedOrganizationScopeAsync` | Method | `server/Dy.MedicalRecognition.Application/Queries/MedicalRecognitionReportQueryAppService.cs` | 200 |
| `ResolveOrganizationHospitalOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 122 |
| `ResolveOrganizationOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 182 |
| `AddTarget` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 238 |
| `ResolveOrThrowAsync` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 42 |
| `ResolveWithBranchOrThrowAsync` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 112 |
| `IsValid` | Method | `server/Dy.MedicalRecognition.Application.Contracts/Validation/NonEmptyAttribute.cs` | 28 |
| `NonEmpty_attribute_semantics` | Method | `server/Dy.MedicalRecognition.Tests/RequestValidationProbeTests.cs` | 92 |
| `BuildTargetOrganizations` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 255 |
| `FindHospital` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 310 |
| `FindOrganization` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 292 |
| `TrimCode` | Method | `server/Dy.MedicalRecognition.Application/Validation/OrganizationPathResolver.cs` | 284 |
| `ResolveReportScopeAsync` | Method | `server/Dy.MedicalRecognition.Application/MedicalRecognitionReportAggregate/MedicalRecognitionReportAppService.cs` | 552 |
| `ResolveLayerOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 79 |
| `ResolveOrThrow` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 153 |
| `TrimCode` | Method | `server/Dy.MedicalRecognition.Application/Validation/TrustedScopeResolver.cs` | 174 |

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

1. `context({name: "QueryTrustedOrganizationScopeAsync"})` — see callers and callees
2. `query({search_query: "validation"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
