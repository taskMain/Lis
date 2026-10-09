---
name: gitnexus-area-contexts
description: "Skill for the Contexts area of Lis. 3 symbols across 3 files."
---

# Contexts

3 symbols | 3 files | Cohesion: 100%

## When to Use

- Working with code in `client/`
- Understanding how client, createMedicalRecognitionApiClient, createMedicalRecognitionClient work
- Modifying contexts-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `client/apps/dy-medical-recognition/src/contexts/ApiClientProvider.tsx` | client |
| `client/packages/api-client-medical-recognition/src/index.ts` | createMedicalRecognitionApiClient |
| `client/packages/api-client-medical-recognition/src/medicalRecognitionClient.ts` | createMedicalRecognitionClient |

## Entry Points

Start here when exploring this area:

- **`client`** (Function) — `client/apps/dy-medical-recognition/src/contexts/ApiClientProvider.tsx:11`
- **`createMedicalRecognitionApiClient`** (Function) — `client/packages/api-client-medical-recognition/src/index.ts:18`
- **`createMedicalRecognitionClient`** (Function) — `client/packages/api-client-medical-recognition/src/medicalRecognitionClient.ts:21`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `client` | Function | `client/apps/dy-medical-recognition/src/contexts/ApiClientProvider.tsx` | 11 |
| `createMedicalRecognitionApiClient` | Function | `client/packages/api-client-medical-recognition/src/index.ts` | 18 |
| `createMedicalRecognitionClient` | Function | `client/packages/api-client-medical-recognition/src/medicalRecognitionClient.ts` | 21 |

## How to Explore

1. `context({name: "client"})` — see callers and callees
2. `query({search_query: "contexts"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
