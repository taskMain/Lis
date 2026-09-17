---
name: gitnexus-area-recognitionprojects
description: "Skill for the RecognitionProjects area of Lis. 89 symbols across 15 files."
---

# RecognitionProjects

89 symbols | 15 files | Cohesion: 88%

## When to Use

- Working with code in `client/`
- Understanding how CreateConfigurationModal, DurationModal, ToggleModal work
- Modifying recognitionprojects-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.test.tsx` | addButton, armLoad, deferred, pendingLoad, query (+20) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | configurationStatusActionText, createRecognitionProjectConfiguration, rowDisplayName, rowScopeTexts, setRecognitionProjectConfigurationEnabled (+18) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | RecognitionProjects, actionLabel, applyOrganization, task, render (+12) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | CreateConfigurationModal, DurationModal, RecognitionDurationFormItem, RecognitionProjectModal, ToggleModal (+5) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsOrganizationScope.ts` | readTrustedOrganizationCode, useTrustedOrganizationScope |
| `client/apps/dy-medical-recognition/src/shared/medicalItemType.ts` | isMedicalItemTypeValue, medicalItemTypeText |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.test.ts` | stubClient, post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/createMutualRecognitionItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMutualRecognitionItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMutualRecognitionItem/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`CreateConfigurationModal`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx:168`
- **`DurationModal`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx:214`
- **`ToggleModal`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx:275`
- **`RecognitionProjects`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:84`
- **`actionLabel`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:175`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `CreateConfigurationModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | 168 |
| `DurationModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | 214 |
| `ToggleModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | 275 |
| `RecognitionProjects` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 84 |
| `actionLabel` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 175 |
| `applyOrganization` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 214 |
| `task` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 279 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 415 |
| `submitWrite` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 342 |
| `configurationStatusActionText` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 81 |
| `createRecognitionProjectConfiguration` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 418 |
| `rowDisplayName` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 129 |
| `rowScopeTexts` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 137 |
| `setRecognitionProjectConfigurationEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 438 |
| `updateRecognitionDuration` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 429 |
| `readTrustedOrganizationCode` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsOrganizationScope.ts` | 18 |
| `useTrustedOrganizationScope` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsOrganizationScope.ts` | 35 |
| `toConfigurationRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 297 |
| `toSelectableStandardItems` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 333 |
| `toItemTypeOptions` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 61 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `BranchRecognitionAmounts → IsMedicalItemTypeValue` | cross_community | 9 |
| `RecognitionAmounts → IsMedicalItemTypeValue` | cross_community | 9 |
| `Task → IsMedicalItemTypeValue` | cross_community | 7 |
| `Retry → IsMedicalItemTypeValue` | cross_community | 7 |
| `Task → IsConfigurationStatusValue` | cross_community | 6 |
| `Task → IsMedicalItemTypeValue` | cross_community | 6 |
| `Retry → IsConfigurationStatusValue` | cross_community | 6 |
| `Retry → IsMedicalItemTypeValue` | cross_community | 6 |
| `SubmitWrite → IsConfigurationStatusValue` | cross_community | 6 |
| `SubmitWrite → IsMedicalItemTypeValue` | cross_community | 6 |

## How to Explore

1. `context({name: "CreateConfigurationModal"})` — see callers and callees
2. `query({search_query: "recognitionprojects"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
