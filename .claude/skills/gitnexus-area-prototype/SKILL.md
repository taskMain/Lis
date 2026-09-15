---
name: gitnexus-area-prototype
description: "Skill for the Prototype area of Lis. 76 symbols across 8 files."
---

# Prototype

76 symbols | 8 files | Cohesion: 89%

## When to Use

- Working with code in `client/`
- Understanding how StandardCatalogVariantA, treeData, StandardCatalogVariantAPlus work
- Modifying prototype-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | CategoryTypeTag, CreateCategoryModal, CreateGroupModal, CreateItemModal, EditCategoryModal (+14) |
| `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | categoryItemType, categoryUsage, groupUsage, isCategoryTypeFrozen, matchesText (+7) |
| `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeStore.tsx` | setCategoryValid, setGroupValid, setItemValid, usePrototypeStore, createCategory (+6) |
| `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantA.tsx` | StandardCatalogVariantA, treeData, stop, render, render (+3) |
| `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx` | StandardCatalogVariantAPlus, getVisibleCategories, visibleScope, nodeTitle, render (+3) |
| `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantC.tsx` | StandardCatalogVariantC, saveInPlace, render, treeData, render (+2) |
| `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantB.tsx` | StandardCatalogVariantB, render, render, render, render (+1) |
| `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogPrototype.tsx` | PrototypeBody, StandardCatalogPrototype, VariantSwitcher, onKeyDown, move |

## Entry Points

Start here when exploring this area:

- **`StandardCatalogVariantA`** (Function) — `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantA.tsx:60`
- **`treeData`** (Function) — `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantA.tsx:72`
- **`StandardCatalogVariantAPlus`** (Function) — `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx:28`
- **`getVisibleCategories`** (Function) — `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx:54`
- **`visibleScope`** (Function) — `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx:66`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `StandardCatalogVariantA` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantA.tsx` | 60 |
| `treeData` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantA.tsx` | 72 |
| `StandardCatalogVariantAPlus` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx` | 28 |
| `getVisibleCategories` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx` | 54 |
| `visibleScope` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantAPlus.tsx` | 66 |
| `StandardCatalogVariantB` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantB.tsx` | 48 |
| `StandardCatalogVariantC` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/StandardCatalogVariantC.tsx` | 52 |
| `categoryItemType` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 186 |
| `categoryUsage` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 90 |
| `groupUsage` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 95 |
| `isCategoryTypeFrozen` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 107 |
| `matchesText` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 133 |
| `usageLabel` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeData.ts` | 99 |
| `CategoryTypeTag` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 446 |
| `CreateCategoryModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 54 |
| `CreateGroupModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 152 |
| `CreateItemModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 265 |
| `EditCategoryModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 93 |
| `EditGroupModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 215 |
| `ItemRemarkModal` | Function | `client/apps/dy-medical-recognition/src/pages/prototype/standardCatalogPrototypeModals.tsx` | 355 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `StandardCatalogPrototype → UsePrototypeStore` | cross_community | 4 |
| `StandardCatalogPrototype → SetCategoryValid` | cross_community | 4 |
| `StandardCatalogPrototype → SetGroupValid` | cross_community | 4 |
| `StandardCatalogPrototype → SetItemValid` | cross_community | 4 |
| `Rows → MatchesText` | cross_community | 3 |
| `TreeData → UsageLabel` | intra_community | 3 |
| `Submit → NextId` | intra_community | 3 |
| `PrototypeBody → UsePrototypeStore` | intra_community | 3 |
| `Rows → MatchesText` | cross_community | 3 |
| `Submit → NextId` | intra_community | 3 |

## How to Explore

1. `context({name: "StandardCatalogVariantA"})` — see callers and callees
2. `query({search_query: "prototype"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
