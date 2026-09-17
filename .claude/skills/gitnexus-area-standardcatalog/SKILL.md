---
name: gitnexus-area-standardcatalog
description: "Skill for the StandardCatalog area of Lis. 90 symbols across 21 files."
---

# StandardCatalog

90 symbols | 21 files | Cohesion: 87%

## When to Use

- Working with code in `client/`
- Understanding how useApiClientContext, StandardCatalog, render work
- Modifying standardcatalog-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | CatalogFormModal, CategoryModal, CreateItemModal, GroupModal, ItemRemarkModal (+25) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | setCategoryEnabled, setGroupEnabled, setItemEnabled, queryCategories, queryGroups (+23) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.test.tsx` | armRead, deferred, fail, renderFailedThenBanner, submitAndReload (+8) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.test.ts` | stubClient, record |
| `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | useApiClientContext |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.enumMetadata.test.tsx` | renderPage |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardCategory/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardGroup/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMedicalStandardCategory/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`useApiClientContext`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:18`
- **`StandardCatalog`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx:365`
- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx:412`
- **`setCategoryEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:292`
- **`setGroupEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:297`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `useApiClientContext` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | 18 |
| `StandardCatalog` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 365 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 412 |
| `setCategoryEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 292 |
| `setGroupEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 297 |
| `setItemEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 302 |
| `queryCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 217 |
| `queryGroups` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 222 |
| `queryItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 227 |
| `isUsageStatusValue` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 27 |
| `createCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 268 |
| `updateCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 272 |
| `createGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 276 |
| `updateGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 280 |
| `visibleCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 386 |
| `visibleItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 385 |
| `filterItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 335 |
| `matchesText` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 307 |
| `visibleCatalogCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 137 |
| `createItem` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 284 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `CategoryModal → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `CreateItemModal → Post` | cross_community | 5 |
| `GroupModal → Post` | cross_community | 5 |
| `StandardCatalog → Post` | cross_community | 5 |
| `CategoryModal → UseApiClientContext` | cross_community | 4 |
| `Task → Post` | intra_community | 4 |
| `Task → Post` | intra_community | 4 |

## How to Explore

1. `context({name: "useApiClientContext"})` — see callers and callees
2. `query({search_query: "standardcatalog"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
