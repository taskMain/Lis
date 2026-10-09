---
name: gitnexus-area-recognitionstatistics
description: "Skill for the RecognitionStatistics area of Lis. 220 symbols across 31 files."
---

# RecognitionStatistics

220 symbols | 31 files | Cohesion: 82%

## When to Use

- Working with code in `client/`
- Understanding how assignStatisticsText, requireStatisticsDate, toStatisticsPage work
- Modifying recognitionstatistics-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/statisticsBoard.tsx` | render, render, render, render, render (+87) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts` | applyBranchUsageFilters, applyPlatformUsageFilters, queryUsageDetailsPage, queryUsageSummaryPage, toBranchUsageDetailsBody (+19) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts` | assignStatisticsText, requireStatisticsDate, toStatisticsPage, toScopeView, toStatisticsDate (+14) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceRecognitionStatisticsApi.ts` | applyBranchSourceFilters, applyPlatformSourceFilters, querySourceDetailsPage, querySourceSummaryPage, toBranchSourceDetailsBody (+11) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/usageStatisticsPages.tsx` | BranchRecognitionUsageStatistics, RecognitionUsageStatistics, UsageStatisticsPage, queryMatchRecord, querySummaryPage (+5) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceStatisticsPages.tsx` | BranchSourceRecognitionStatistics, SourceRecognitionStatistics, SourceStatisticsPage, exportWorkbook, toExportQuery (+4) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/matchRecordModal.tsx` | render, render, render, render, render (+3) |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/statisticsDisplays.ts` | statisticsTextOrDash, formatStatisticsDateTime, pad, formatStatisticsPercent, formatStatisticsAmount (+2) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsTrustedScope.ts` | readTrustedBranchCode, readTrustedCode, readTrustedHospitalCode, useTrustedHospitalScope |
| `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/statisticsBoard.test.tsx` | boardProps, renderBoard, summarySection, waitSummaryText |

## Entry Points

Start here when exploring this area:

- **`assignStatisticsText`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts:272`
- **`requireStatisticsDate`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts:281`
- **`toStatisticsPage`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts:148`
- **`queryUsageDetailsPage`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts:521`
- **`queryUsageSummaryPage`** (Function) — `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts:509`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `assignStatisticsText` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts` | 272 |
| `requireStatisticsDate` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts` | 281 |
| `toStatisticsPage` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionStatisticsValues.ts` | 148 |
| `queryUsageDetailsPage` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts` | 521 |
| `queryUsageSummaryPage` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/recognitionUsageStatisticsApi.ts` | 509 |
| `querySourceDetailsPage` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceRecognitionStatisticsApi.ts` | 385 |
| `querySourceSummaryPage` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceRecognitionStatisticsApi.ts` | 373 |
| `handleSelectorChange` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionAmounts/RecognitionAmounts.tsx` | 770 |
| `readTrustedOrganizationCode` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsOrganizationScope.ts` | 18 |
| `useTrustedOrganizationScope` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionProjects/recognitionProjectsOrganizationScope.ts` | 35 |
| `BranchSourceRecognitionStatistics` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceStatisticsPages.tsx` | 226 |
| `SourceRecognitionStatistics` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/sourceStatisticsPages.tsx` | 221 |
| `BranchRecognitionUsageStatistics` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/usageStatisticsPages.tsx` | 259 |
| `RecognitionUsageStatistics` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/usageStatisticsPages.tsx` | 254 |
| `BranchReportManagement` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/BranchReportManagement.tsx` | 36 |
| `readTrustedBranchCode` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsTrustedScope.ts` | 32 |
| `readTrustedHospitalCode` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsTrustedScope.ts` | 25 |
| `useTrustedHospitalScope` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsTrustedScope.ts` | 44 |
| `StatisticsBoard` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/statisticsBoard.tsx` | 392 |
| `expandedSummaryKeys` | Function | `client/apps/dy-medical-recognition/src/pages/recognitionStatistics/statisticsBoard.tsx` | 674 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `Render → IsMedicalItemTypeValue` | cross_community | 8 |
| `BranchReportManagement → ToDisplayDate` | cross_community | 6 |
| `ChangeKeywordDraft → IsMedicalItemTypeValue` | cross_community | 6 |
| `Task → IsMedicalItemTypeValue` | cross_community | 6 |
| `OpenBatchCreate → IsMedicalItemTypeValue` | cross_community | 6 |
| `Retry → IsMedicalItemTypeValue` | cross_community | 6 |
| `SearchSelectableItems → IsMedicalItemTypeValue` | cross_community | 6 |
| `SubmitKeywordFilter → IsMedicalItemTypeValue` | cross_community | 6 |
| `SubmitWrite → IsMedicalItemTypeValue` | cross_community | 6 |
| `QueryDetailsPage → ToStatisticsRequestDate` | cross_community | 5 |

## How to Explore

1. `context({name: "assignStatisticsText"})` — see callers and callees
2. `query({search_query: "recognitionstatistics"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
