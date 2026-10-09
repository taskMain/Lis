---
name: gitnexus-area-commands
description: "Skill for the Commands area of Lis. 26 symbols across 14 files."
---

# Commands

26 symbols | 14 files | Cohesion: 100%

## When to Use

- Working with code in `server/`
- Understanding how CreateMedicalStandardItemRemarkChangedEvent, ChangeMedicalStandardItemRemarkAsync, CreateMedicalStandardCategoryCreatedEvent work
- Modifying commands-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | ChangeMedicalStandardItemRemarkAsync, CreateMedicalStandardCategoryAsync, CreateMedicalStandardGroupAsync, CreateMedicalStandardItemAsync, DisableMedicalStandardCategoryAsync (+8) |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/ChangeMedicalStandardItemRemarkCommand.cs` | CreateMedicalStandardItemRemarkChangedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardGroupCommand.cs` | CreateMedicalStandardGroupCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardItemCommand.cs` | CreateMedicalStandardItemCreatedEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryDisabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardGroupCommand.cs` | CreateMedicalStandardGroupDisabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardItemCommand.cs` | CreateMedicalStandardItemDisabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardCategoryCommand.cs` | CreateMedicalStandardCategoryEnabledEvent |
| `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | CreateMedicalStandardGroupEnabledEvent |

## Entry Points

Start here when exploring this area:

- **`CreateMedicalStandardItemRemarkChangedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/ChangeMedicalStandardItemRemarkCommand.cs:39`
- **`ChangeMedicalStandardItemRemarkAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs:202`
- **`CreateMedicalStandardCategoryCreatedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs:44`
- **`CreateMedicalStandardCategoryAsync`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs:45`
- **`CreateMedicalStandardGroupCreatedEvent`** (Method) — `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardGroupCommand.cs:44`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `CreateMedicalStandardItemRemarkChangedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/ChangeMedicalStandardItemRemarkCommand.cs` | 39 |
| `ChangeMedicalStandardItemRemarkAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 202 |
| `CreateMedicalStandardCategoryCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardCategoryCommand.cs` | 44 |
| `CreateMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 45 |
| `CreateMedicalStandardGroupCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardGroupCommand.cs` | 44 |
| `CreateMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 114 |
| `CreateMedicalStandardItemCreatedEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/CreateMedicalStandardItemCommand.cs` | 53 |
| `CreateMedicalStandardItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 183 |
| `CreateMedicalStandardCategoryDisabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardCategoryCommand.cs` | 31 |
| `DisableMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 96 |
| `CreateMedicalStandardGroupDisabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardGroupCommand.cs` | 31 |
| `DisableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 164 |
| `CreateMedicalStandardItemDisabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/DisableMedicalStandardItemCommand.cs` | 31 |
| `DisableMedicalStandardItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 233 |
| `CreateMedicalStandardCategoryEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardCategoryCommand.cs` | 31 |
| `EnableMedicalStandardCategoryAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 80 |
| `CreateMedicalStandardGroupEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardGroupCommand.cs` | 31 |
| `EnableMedicalStandardGroupAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 148 |
| `CreateMedicalStandardItemEnabledEvent` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Commands/EnableMedicalStandardItemCommand.cs` | 31 |
| `EnableMedicalStandardItemAsync` | Method | `server/Dy.MedicalRecognition.Domain/MedicalRecognitionReportAggregate/Managers/MedicalRecognitionReportManager.cs` | 217 |

## How to Explore

1. `context({name: "CreateMedicalStandardItemRemarkChangedEvent"})` — see callers and callees
2. `query({search_query: "commands"})` — find related execution flows
3. Read key files listed above for implementation details
4. `explain({target: "<file or symbol>"})` — persisted taint findings (source→sink data flows), when indexed with `--pdg`
