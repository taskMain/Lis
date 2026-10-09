---
name: gitnexus-area-standardcatalog
description: "Skill for the StandardCatalog area of Lis. 108 symbols across 25 files."
---

# StandardCatalog

108 symbols | 25 files | Cohesion: 88%

## When to Use

- Working with code in `client/`
- Understanding how useApiClientContext, StandardCatalog, setCategoryEnabled work
- Modifying standardcatalog-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | CatalogFormModal, CategoryModal, CreateItemModal, GroupModal, ItemRemarkModal (+31) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | setCategoryEnabled, setGroupEnabled, setItemEnabled, queryCategories, queryGroups (+23) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.test.tsx` | armRead, deferred, fail, renderFailedThenBanner, submitAndReload (+16) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.test.ts` | stubClient, record |
| `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | useApiClientContext |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.enumMetadata.test.tsx` | renderPage |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardCategory/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardGroup/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMedicalStandardCategory/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`useApiClientContext`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:6`
- **`StandardCatalog`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx:457`
- **`setCategoryEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:297`
- **`setGroupEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:302`
- **`setItemEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:307`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `useApiClientContext` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | 6 |
| `StandardCatalog` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 457 |
| `setCategoryEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 297 |
| `setGroupEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 302 |
| `setItemEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 307 |
| `queryCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 222 |
| `queryGroups` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 227 |
| `queryItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 232 |
| `ApiClientProvider` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientProvider.tsx` | 10 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 521 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 522 |
| `AppRoutes` | Function | `client/apps/dy-medical-recognition/src/router/index.tsx` | 5 |
| `loadRuntimeConfig` | Function | `client/apps/dy-medical-recognition/src/runtimeConfig.ts` | 12 |
| `isUsageStatusValue` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 27 |
| `createCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 273 |
| `updateCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 277 |
| `createGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 281 |
| `updateGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 285 |
| `treeData` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 486 |
| `visibleCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 484 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `CategoryModal → Post` | cross_community | 5 |
| `CreateItemModal → Post` | cross_community | 5 |
| `GroupModal → Post` | cross_community | 5 |
| `StandardCatalog → Post` | cross_community | 5 |
| `BranchReportManagement → UseApiClientContext` | cross_community | 4 |
| `CategoryModal → UseApiClientContext` | cross_community | 4 |
| `CreateItemModal → UseApiClientContext` | cross_community | 4 |

## How to Explore

1. `context({name: "useApiClientContext"})` — see callers and callees
2. `query({search_query: "standardcatalog"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
