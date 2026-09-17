---
name: gitnexus-area-contexts
description: "Skill for the Contexts area of Lis. 4 symbols across 4 files."
---

# Contexts

4 symbols | 4 files | Cohesion: 100%

## When to Use

- Working with code in `client/`
- Understanding how ApiClientProvider, AppRoutes, loadRuntimeConfig work
- Modifying contexts-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | ApiClientProvider |
| `client/apps/dy-medical-recognition/src/main.tsx` | initApp |
| `client/apps/dy-medical-recognition/src/router/index.tsx` | AppRoutes |
| `client/apps/dy-medical-recognition/src/runtimeConfig.ts` | loadRuntimeConfig |

## Entry Points

Start here when exploring this area:

- **`ApiClientProvider`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:9`
- **`AppRoutes`** (Function) — `client/apps/dy-medical-recognition/src/router/index.tsx:5`
- **`loadRuntimeConfig`** (Function) — `client/apps/dy-medical-recognition/src/runtimeConfig.ts:12`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ApiClientProvider` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | 9 |
| `AppRoutes` | Function | `client/apps/dy-medical-recognition/src/router/index.tsx` | 5 |
| `loadRuntimeConfig` | Function | `client/apps/dy-medical-recognition/src/runtimeConfig.ts` | 12 |
| `initApp` | Function | `client/apps/dy-medical-recognition/src/main.tsx` | 15 |

## How to Explore

1. `context({name: "ApiClientProvider"})` — see callers and callees
2. `query({search_query: "contexts"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
