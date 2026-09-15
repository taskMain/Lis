---
name: gitnexus-area-dy-medicalrecognition-tests
description: "Skill for the Dy.MedicalRecognition.Tests area of Lis. 9 symbols across 1 files."
---

# Dy.MedicalRecognition.Tests

9 symbols | 1 files | Cohesion: 100%

## When to Use

- Working with code in `server/`
- Understanding how Runtime_registration_reports_full_sql_ids_without_opening_database, Create_statements_default_is_valid_without_request_parameter, Enable_and_disable_statements_keep_boolean_literals work
- Modifying dy.medicalrecognition.tests-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | FindHostOutputDirectory, Flatten, GetRegisteredKeys, Invoke, LoadAssembly (+4) |

## Entry Points

Start here when exploring this area:

- **`Runtime_registration_reports_full_sql_ids_without_opening_database`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs:40`
- **`Create_statements_default_is_valid_without_request_parameter`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs:11`
- **`Enable_and_disable_statements_keep_boolean_literals`** (Method) — `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs:25`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `Runtime_registration_reports_full_sql_ids_without_opening_database` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 40 |
| `Create_statements_default_is_valid_without_request_parameter` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 11 |
| `Enable_and_disable_statements_keep_boolean_literals` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 25 |
| `FindHostOutputDirectory` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 139 |
| `Flatten` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 120 |
| `GetRegisteredKeys` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 97 |
| `Invoke` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 115 |
| `LoadAssembly` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 165 |
| `FindRepositorySqlMap` | Method | `server/Dy.MedicalRecognition.Tests/Stage1SqlMapProbeTests.cs` | 152 |

## How to Explore

1. `context({name: "Runtime_registration_reports_full_sql_ids_without_opening_database"})` — see callers and callees
2. `query({search_query: "dy.medicalrecognition.tests"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
