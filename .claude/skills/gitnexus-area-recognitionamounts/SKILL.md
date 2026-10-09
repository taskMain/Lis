---
name: gitnexus-area-recognitionamounts
description: "Skill for the RecognitionAmounts area of Lis. 141 symbols across 12 files."
---

# RecognitionAmounts

141 symbols | 12 files | Cohesion: 88%

## When to Use

- Working with code in `client/`
- Understanding how toRecognitionAmountRows, queryRows, queryBranchRecognitionAmountRows work
- Modifying recognitionamounts-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.test.tsx` | amountRow, node, amountValueCell, click, dataRows (+23) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | serverEnumText, toAmountNumber, toMedicalItemType, toRecognitionAmountRows, queryBranchRecognitionAmountRows (+23) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.test.tsx` | amountRow, node, amountValueCell, click, dataRows (+22) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | queryRows, task, load, retry, BranchRecognitionAmountsBoard (+18) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | RecognitionAmountsPlatformBoard, emptyDescription, tableRows, queryRows, task (+17) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.test.ts` | amountRow, node, readModel, codesOf, stubClient (+1) |
| `client/apps/dy-medical-recognition/src/shared/configurationStatus.ts` | configurationStatusText, isConfigurationStatusValue |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReportQuery/queryBranchRecognitionAmountList/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReportQuery/queryRecognitionAmountList/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/saveOrganizationHospitalBranchRecognitionAmount/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`toRecognitionAmountRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:323`
- **`queryRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx:832`
- **`queryBranchRecognitionAmountRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:739`
- **`buildAmountFilterSearch`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:593`
- **`syncAmountFilterToLocation`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:623`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `toRecognitionAmountRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 323 |
| `queryRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 832 |
| `queryBranchRecognitionAmountRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 739 |
| `buildAmountFilterSearch` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 593 |
| `syncAmountFilterToLocation` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 623 |
| `toRecognitionAmountColumnView` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 495 |
| `queryRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 818 |
| `queryRecognitionAmountRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 722 |
| `BranchRecognitionAmounts` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 687 |
| `currentAmountText` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 354 |
| `applySelection` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 744 |
| `completeSwitch` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 787 |
| `task` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 817 |
| `handleSelectorChange` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 752 |
| `saveRow` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 821 |
| `recheckRecognitionAmountScope` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 686 |
| `saveOrganizationHospitalBranchRecognitionAmount` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 752 |
| `parseRecognitionAmount` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 192 |
| `saveRow` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 835 |
| `buildBranchAmountSavePayload` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 276 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `Render → IsConfigurationStatusValue` | cross_community | 8 |
| `Render → IsMedicalItemTypeValue` | cross_community | 8 |
| `Render → ServerEnumText` | cross_community | 7 |
| `Render → ToAmountNumber` | cross_community | 7 |
| `Render → ToAmountDigits` | cross_community | 6 |
| `Render → Post` | cross_community | 6 |
| `ChangeKeywordDraft → IsConfigurationStatusValue` | cross_community | 6 |
| `Task → IsConfigurationStatusValue` | cross_community | 6 |
| `Retry → IsConfigurationStatusValue` | cross_community | 6 |
| `SubmitKeywordFilter → IsConfigurationStatusValue` | cross_community | 6 |

## How to Explore

1. `context({name: "toRecognitionAmountRows"})` — see callers and callees
2. `query({search_query: "recognitionamounts"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
