---
name: gitnexus-area-contexts
description: "Skill for the Contexts area of Lis. 4 symbols across 3 files."
---

# Contexts

4 symbols | 3 files | Cohesion: 100%

## When to Use

- Working with code in `client/`
- Understanding how ApiClientProvider, AppRoutes work
- Modifying contexts-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/main.tsx` | initApp, loadConfig |
| `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | ApiClientProvider |
| `client/apps/dy-medical-recognition/src/router/index.tsx` | AppRoutes |

## Entry Points

Start here when exploring this area:

- **`ApiClientProvider`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx:9`
- **`AppRoutes`** (Function) — `client/apps/dy-medical-recognition/src/router/index.tsx:5`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `ApiClientProvider` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientContext.tsx` | 9 |
| `AppRoutes` | Function | `client/apps/dy-medical-recognition/src/router/index.tsx` | 5 |
| `initApp` | Function | `client/apps/dy-medical-recognition/src/main.tsx` | 33 |
| `loadConfig` | Function | `client/apps/dy-medical-recognition/src/main.tsx` | 19 |

## How to Explore

1. `context({name: "ApiClientProvider"})` — see callers and callees
2. `query({search_query: "contexts"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
