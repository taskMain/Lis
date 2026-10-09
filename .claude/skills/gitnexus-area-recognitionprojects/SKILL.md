---
name: gitnexus-area-recognitionprojects
description: "Skill for the RecognitionProjects area of Lis. 143 symbols across 16 files."
---

# RecognitionProjects

143 symbols | 16 files | Cohesion: 85%

## When to Use

- Working with code in `client/`
- Understanding how RecognitionProjects, actionLabel, applyOrganization work
- Modifying recognitionprojects-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.test.tsx` | armLoad, deferred, expectModalClosed, modalExists, pendingLoad (+44) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | configurationStatusActionText, createRecognitionProjectConfigurations, setRecognitionProjectConfigurationEnabled, updateRecognitionDuration, configurationStatusTagColor (+25) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | RecognitionProjects, actionLabel, applyOrganization, changeFilter, changeKeywordDraft (+20) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | BatchSelectableRow, RecognitionProjectBatchCreateModal, RecognitionProjectModal, ToggleModal, viewCodes (+8) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectCascade.tsx` | RecognitionProjectCascade, isCategoryHighlighted, isGroupHighlighted, isItemTypeHighlighted, levelClassName (+1) |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.test.ts` | columnRows, configuredCodesIn, rowOf, stubClient, post |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsFilterUrl.ts` | buildFilterSearch, parseFilterSearch, readFilterFromLocation, syncFilterToLocation |
| `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectRow.tsx` | RecognitionDurationEditor, RecognitionProjectRow, submitDuration |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/createMutualRecognitionItems/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMutualRecognitionItem/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`RecognitionProjects`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:73`
- **`actionLabel`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:239`
- **`applyOrganization`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:335`
- **`changeFilter`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:542`
- **`changeKeywordDraft`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx:564`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `RecognitionProjects` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 73 |
| `actionLabel` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 239 |
| `applyOrganization` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 335 |
| `changeFilter` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 542 |
| `changeKeywordDraft` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 564 |
| `task` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjects.tsx` | 424 |
| `configurationStatusActionText` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 79 |
| `createRecognitionProjectConfigurations` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 693 |
| `setRecognitionProjectConfigurationEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 713 |
| `updateRecognitionDuration` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 704 |
| `buildFilterSearch` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsFilterUrl.ts` | 37 |
| `parseFilterSearch` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsFilterUrl.ts` | 19 |
| `readFilterFromLocation` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsFilterUrl.ts` | 53 |
| `syncFilterToLocation` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsFilterUrl.ts` | 64 |
| `RecognitionProjectBatchCreateModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | 177 |
| `ToggleModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectModals.tsx` | 433 |
| `RecognitionProjectRow` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/RecognitionProjectRow.tsx` | 142 |
| `writeBlockedReason` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectWriteGate.ts` | 39 |
| `configurationStatusTagColor` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 118 |
| `recognitionProjectRowBadge` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsApi.ts` | 642 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `ChangeKeywordDraft → IsConfigurationStatusValue` | cross_community | 6 |
| `ChangeKeywordDraft → IsMedicalItemTypeValue` | cross_community | 6 |
| `Task → IsConfigurationStatusValue` | cross_community | 6 |
| `Task → IsMedicalItemTypeValue` | cross_community | 6 |
| `Retry → IsConfigurationStatusValue` | cross_community | 6 |
| `OpenBatchCreate → IsMedicalItemTypeValue` | cross_community | 6 |
| `SubmitKeywordFilter → IsConfigurationStatusValue` | cross_community | 6 |
| `Retry → IsMedicalItemTypeValue` | cross_community | 6 |
| `SubmitWrite → IsConfigurationStatusValue` | cross_community | 6 |
| `SearchSelectableItems → IsMedicalItemTypeValue` | cross_community | 6 |

## How to Explore

1. `context({name: "RecognitionProjects"})` — see callers and callees
2. `query({search_query: "recognitionprojects"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
