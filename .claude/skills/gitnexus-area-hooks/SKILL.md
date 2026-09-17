---
name: gitnexus-area-hooks
description: "Skill for the Hooks area of Lis. 9 symbols across 3 files."
---

# Hooks

9 symbols | 3 files | Cohesion: 84%

## When to Use

- Working with code in `client/`
- Understanding how loadEnumMetadataOptions, useEnumMetadata, post work
- Modifying hooks-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | first, second, first, second, first (+1) |
| `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.ts` | loadEnumMetadataOptions, useEnumMetadata |
| `client/packages/api-client-medical-recognition/src/api/enumMetadata/getEnumMetadata/index.ts` | post |

## Entry Points

Start here when exploring this area:

- **`loadEnumMetadataOptions`** (Function) — `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.ts:21`
- **`useEnumMetadata`** (Function) — `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.ts:85`
- **`post`** (Method) — `client/packages/api-client-medical-recognition/src/api/enumMetadata/getEnumMetadata/index.ts:17`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `loadEnumMetadataOptions` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.ts` | 21 |
| `useEnumMetadata` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.ts` | 85 |
| `post` | Method | `client/packages/api-client-medical-recognition/src/api/enumMetadata/getEnumMetadata/index.ts` | 17 |
| `first` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 120 |
| `second` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 121 |
| `first` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 183 |
| `second` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 192 |
| `first` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 102 |
| `second` | Function | `client/apps/dy-medical-recognition/src/hooks/useEnumMetadata.test.ts` | 109 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `CategoryModal → Post` | cross_community | 5 |
| `CreateItemModal → Post` | cross_community | 5 |
| `GroupModal → Post` | cross_community | 5 |
| `StandardCatalog → Post` | cross_community | 5 |
| `CategoryModal → UseApiClientContext` | cross_community | 4 |
| `CreateItemModal → UseApiClientContext` | cross_community | 4 |
| `GroupModal → UseApiClientContext` | cross_community | 4 |
| `StandardCatalog → UseApiClientContext` | cross_community | 4 |
| `RecognitionProjects → Post` | cross_community | 4 |
| `RecognitionProjects → UseApiClientContext` | cross_community | 3 |

## How to Explore

1. `context({name: "loadEnumMetadataOptions"})` — see callers and callees
2. `query({search_query: "hooks"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
