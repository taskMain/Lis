---
name: gitnexus-area-recognitionamounts
description: "Skill for the RecognitionAmounts area of Lis. 94 symbols across 13 files."
---

# RecognitionAmounts

94 symbols | 13 files | Cohesion: 88%

## When to Use

- Working with code in `client/`
- Understanding how queryRows, queryBranchRecognitionAmountRows, toRecognitionAmountRows work
- Modifying recognitionamounts-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.test.tsx` | amountRow, node, renderPage, armQuery, deferred (+19) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.test.tsx` | amountRow, node, armQuery, deferred, query (+16) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | queryBranchRecognitionAmountRows, serverEnumText, toAmountNumber, toRecognitionAmountRows, saveOrganizationHospitalBranchRecognitionAmount (+11) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | RecognitionAmounts, saveRow, RecognitionAmountsBoard, submitAmount, render (+6) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | queryRows, BranchRecognitionAmounts, readTrustedCode, useTrustedBranchScope, saveRow (+1) |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmountModals.tsx` | AmountEntryModal, submit, isValid, writeBlockedReason, validateAmountRule |
| `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.test.ts` | node, readModel, stubClient, post |
| `client/apps/dy-medical-recognition/src/shared/configurationStatus.ts` | configurationStatusText, isConfigurationStatusValue |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReportQuery/queryBranchRecognitionAmountList/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/saveOrganizationHospitalBranchRecognitionAmount/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`queryRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx:78`
- **`queryBranchRecognitionAmountRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:372`
- **`toRecognitionAmountRows`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts:317`
- **`RecognitionAmounts`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx:295`
- **`saveRow`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx:303`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `queryRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/BranchRecognitionAmounts.tsx` | 78 |
| `queryBranchRecognitionAmountRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 372 |
| `toRecognitionAmountRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 317 |
| `RecognitionAmounts` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 295 |
| `saveRow` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 303 |
| `RecognitionAmountsBoard` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 70 |
| `submitAmount` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 155 |
| `saveOrganizationHospitalBranchRecognitionAmount` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 383 |
| `AmountEntryModal` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmountModals.tsx` | 78 |
| `submit` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmountModals.tsx` | 98 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 197 |
| `currentAmountText` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 348 |
| `appliedScope` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 324 |
| `buildRecognitionAmountListQuery` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 209 |
| `buildRecognitionAmountSavePayload` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 249 |
| `parseRecognitionAmount` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/recognitionAmountsApi.ts` | 192 |
| `queryRows` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 300 |
| `task` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 125 |
| `load` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 108 |
| `retry` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 145 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `BranchRecognitionAmounts → IsConfigurationStatusValue` | cross_community | 9 |
| `BranchRecognitionAmounts → IsMedicalItemTypeValue` | cross_community | 9 |
| `RecognitionAmounts → IsConfigurationStatusValue` | cross_community | 9 |
| `RecognitionAmounts → IsMedicalItemTypeValue` | cross_community | 9 |
| `BranchRecognitionAmounts → ServerEnumText` | cross_community | 8 |
| `BranchRecognitionAmounts → ToAmountNumber` | cross_community | 8 |
| `RecognitionAmounts → ServerEnumText` | cross_community | 8 |
| `RecognitionAmounts → ToAmountNumber` | cross_community | 8 |
| `BranchRecognitionAmounts → ToAmountDigits` | cross_community | 7 |
| `BranchRecognitionAmounts → Post` | cross_community | 7 |

## How to Explore

1. `context({name: "queryRows"})` — see callers and callees
2. `query({search_query: "recognitionamounts"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
