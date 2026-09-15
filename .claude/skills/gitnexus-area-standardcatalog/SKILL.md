---
name: gitnexus-area-standardcatalog
description: "Skill for the StandardCatalog area of Lis. 71 symbols across 18 files."
---

# StandardCatalog

71 symbols | 18 files | Cohesion: 89%

## When to Use

- Working with code in `client/`
- Understanding how useApiClientContext, StandardCatalog, render work
- Modifying standardcatalog-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | CatalogFormModal, CategoryModal, CreateItemModal, GroupModal, ItemRemarkModal (+24) |
| `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | setCategoryEnabled, setGroupEnabled, setItemEnabled, queryCategories, queryGroups (+21) |
| `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | useApiClientContext |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardCategory/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardGroup/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/disableMedicalStandardItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMedicalStandardCategory/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMedicalStandardGroup/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReport/enableMedicalStandardItem/index.ts` | post |
| `client/packages/api-client-medical-recognition/src/api/medicalRecognitionReportQuery/queryMedicalStandardCategoryList/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`useApiClientContext`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:18`
- **`StandardCatalog`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx:332`
- **`render`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx:374`
- **`setCategoryEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:218`
- **`setGroupEnabled`** (Function) — `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts:223`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `useApiClientContext` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | 18 |
| `StandardCatalog` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 332 |
| `render` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 374 |
| `setCategoryEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 218 |
| `setGroupEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 223 |
| `setItemEnabled` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 228 |
| `queryCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 143 |
| `queryGroups` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 148 |
| `queryItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 153 |
| `createCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 194 |
| `updateCategory` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 198 |
| `createGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 202 |
| `updateGroup` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 206 |
| `visibleCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 348 |
| `visibleItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/StandardCatalog.tsx` | 347 |
| `filterItems` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 261 |
| `matchesText` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 233 |
| `visibleCatalogCategories` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 73 |
| `createItem` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 210 |
| `toRemark` | Function | `client/apps/dy-medical-recognition/src/pages/standardCatalog/standardCatalogApi.ts` | 275 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `StandardCatalog → Post` | cross_community | 5 |
| `StandardCatalog → Post` | cross_community | 5 |
| `StandardCatalog → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `SaveAndReload → Post` | cross_community | 5 |
| `Task → Post` | intra_community | 4 |
| `Task → Post` | intra_community | 4 |
| `Task → Post` | intra_community | 4 |
| `Submit → Post` | intra_community | 3 |

## How to Explore

1. `context({name: "useApiClientContext"})` — see callers and callees
2. `query({search_query: "standardcatalog"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
