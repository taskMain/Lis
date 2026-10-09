---
name: gitnexus-area-reportmanagement
description: "Skill for the ReportManagement area of Lis. 143 symbols across 16 files."
---

# ReportManagement

143 symbols | 16 files | Cohesion: 89%

## When to Use

- Working with code in `client/`
- Understanding how render, render, render work
- Modifying reportmanagement-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | render, render, render, render, render (+45) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | reportStatusText, reportTypeText, serverText, toDate, toReportRows (+23) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagement.test.tsx` | examinationDetail, laboratoryDetail, versionRow, deferred, expectPageFilters (+22) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagementBoard.tsx` | render, ReportManagementBoard, applyFilters, loadDetail, loadVersionDetail (+6) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagement.tsx` | ReportManagement, downloadVersion, queryDetail, queryVersions, appliedScope (+1) |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/BranchReportManagement.tsx` | appliedScope, queryPage, downloadVersion, queryDetail, queryVersions |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.test.ts` | byReportId, get, stubClient, post, requestBuilder |
| `client/apps/dy-medical-recognition/src/pages/reportManagement/BranchReportManagement.test.tsx` | generatedRow, listResponse, renderLoaded |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReportQuery/queryBranchMedicalReportList/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/v1/reportPdf/index.ts` | byReportId |

## Entry Points

Start here when exploring this area:

- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx:108`
- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx:109`
- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx:110`
- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx:111`
- **`reportStatusText`** (Function) — `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts:84`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 108 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 109 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 110 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 111 |
| `reportStatusText` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 84 |
| `reportTypeText` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 77 |
| `toReportRows` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 331 |
| `toReportVersionDetail` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 378 |
| `toReportVersionRows` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 354 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagementBoard.tsx` | 332 |
| `ReportVersionDetailPanel` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 84 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 106 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportVersionDetailPanel.tsx` | 107 |
| `toDisplayDate` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 207 |
| `toDisplayDateTime` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 215 |
| `ReportManagement` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagement.tsx` | 28 |
| `appliedScope` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/BranchReportManagement.tsx` | 57 |
| `queryPage` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/BranchReportManagement.tsx` | 65 |
| `buildBranchReportListQuery` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 237 |
| `queryBranchReportPage` | Function | `client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts` | 304 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `BranchReportManagement → ToDisplayDate` | cross_community | 6 |
| `BranchReportManagement → Post` | cross_community | 5 |
| `QueryPage → IsReportTypeValue` | cross_community | 5 |
| `QueryPage → ToRequestDate` | cross_community | 5 |
| `QueryPage → ReportStatusText` | cross_community | 5 |
| `QueryPage → ReportTypeText` | cross_community | 5 |
| `QueryPage → ServerText` | cross_community | 5 |
| `QueryPage → ToDate` | cross_community | 5 |
| `QueryPage → IsReportTypeValue` | cross_community | 5 |
| `QueryPage → ToRequestDate` | cross_community | 5 |

## How to Explore

1. `context({name: "render"})` — see callers and callees
2. `query({search_query: "reportmanagement"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
