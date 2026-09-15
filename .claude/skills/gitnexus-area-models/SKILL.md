---
name: gitnexus-area-models
description: "Skill for the Models area of Lis. 10 symbols across 1 files."
---

# Models

10 symbols | 1 files | Cohesion: 100%

## When to Use

- Working with code in `client/`
- Understanding how deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemType, deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1, deserializeIntoMedicalItemType work
- Modifying models-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/packages/api-client-medical-recognition/src/models/index.ts` | deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemType, deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1, deserializeIntoMedicalItemType, deserializeIntoMedicalStandardCategoryListQueryRequest_itemType, deserializeIntoMedicalStandardCategoryListQueryRequest_itemTypeMember1 (+5) |

## Entry Points

Start here when exploring this area:

- **`deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemType`** (Function) — `client/packages/api-client-medical-recognition/src/models/index.ts:573`
- **`deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1`** (Function) — `client/packages/api-client-medical-recognition/src/models/index.ts:585`
- **`deserializeIntoMedicalItemType`** (Function) — `client/packages/api-client-medical-recognition/src/models/index.ts:666`
- **`deserializeIntoMedicalStandardCategoryListQueryRequest_itemType`** (Function) — `client/packages/api-client-medical-recognition/src/models/index.ts:687`
- **`deserializeIntoMedicalStandardCategoryListQueryRequest_itemTypeMember1`** (Function) — `client/packages/api-client-medical-recognition/src/models/index.ts:699`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 573 |
| `deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 585 |
| `deserializeIntoMedicalItemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 666 |
| `deserializeIntoMedicalStandardCategoryListQueryRequest_itemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 687 |
| `deserializeIntoMedicalStandardCategoryListQueryRequest_itemTypeMember1` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 699 |
| `serializeEffectiveMedicalStandardCatalogQueryRequest_itemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 1287 |
| `serializeEffectiveMedicalStandardCatalogQueryRequest_itemTypeMember1` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 1298 |
| `serializeMedicalItemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 1386 |
| `serializeMedicalStandardCategoryListQueryRequest_itemType` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 1409 |
| `serializeMedicalStandardCategoryListQueryRequest_itemTypeMember1` | Function | `client/packages/api-client-medical-recognition/src/models/index.ts` | 1420 |

## How to Explore

1. `context({name: "deserializeIntoEffectiveMedicalStandardCatalogQueryRequest_itemType"})` — see callers and callees
2. `query({search_query: "models"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
