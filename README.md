# Daily Task Selection Workflow (Assignment 5)

Introduction to Functional Programming, Assignment 5: Composing Functions Using Pipelining.

## 1. Topic, users, team

* **Topic:** select and prioritise daily tasks.
* **Intended users:** developers and project managers who need a predictable, repeatable daily task order.
* **Team:** `<Name Surname, Group>` (fill in; for each member list the logic, tests and documentation they own, see section 8).
* **Stack:** C#, .NET 10 SDK (`net10.0`, the project does not build with older SDKs), xUnit 2.9.3.

## 2. Input, rules, expected result

Input file `tasks.txt`, one task per line: `TaskName,EstimatedHours,Urgency,IsCompleted`.

| Field | Type | Rule |
| :--- | :--- | :--- |
| `TaskName` | string | not null, not empty, not whitespace; trimmed |
| `EstimatedHours` | double | finite number, **strictly greater than 0**; `.` is the decimal separator (InvariantCulture) |
| `Urgency` | int | integer from 1 to 5, both bounds included |
| `IsCompleted` | bool | `true` or `false` (`false` is a legitimate value, not "missing") |

Other rules:

* A line must contain exactly 4 comma-separated fields.
* Blank lines are ignored; a `null` line or a missing file is an error.
* A **missing/unreadable file** is an error (exit code 1). An **existing empty file** is a valid empty dataset and gives an empty plan.
* Invalid lines are rejected with a message `Line N: ...`; nothing is replaced by defaults.

Expected result: a `DailyPlan` (ordered tasks with scores, total score, total hours) for each of the two policies.

## 3. Pipeline stages (all pure)

Validation happens at the boundary (`TaskFileReader` → `TaskValidator.ParseLines`) and produces valid `TaskItem` records. Then three stages are composed in `Program.BuildPlan`:

```csharp
TaskPipeline.SortAndFinalize(TaskPipeline.CalculateScores(TaskPipeline.FilterActiveTasks(tasks), policy));
```

| Stage | Input → Output | Rule | Example |
| :--- | :--- | :--- | :--- |
| 1 `FilterActiveTasks` | `IEnumerable<TaskItem>` → `ImmutableArray<TaskItem>` | keep tasks with `IsCompleted == false` | `Write Unit Tests (true)` is removed, 4 of 5 tasks remain |
| 2 `CalculateScores` | `IEnumerable<TaskItem>` + `Func<TaskItem,double>` → `ImmutableArray<ScoredTaskItem>` | call the policy delegate for every task | `Fix Login Bug` (3.5 h, urgency 4): standard 40, express 67 |
| 3 `SortAndFinalize` | `IEnumerable<ScoredTaskItem>` → `DailyPlan` | order by score descending, ties by name (ordinal); sum score and hours | standard: `Security Patch 50, Fix Login Bug 40, Code Review 30, Plan Sprint 20`, total score 140, total hours 25.5 |

Stages never mutate their input, never print or read files, and use no clock, random value or mutable global state. Results are `ImmutableArray` copies, evaluated eagerly (no deferred LINQ).

## 4. Configurable rule (higher-order function)

`TaskPipeline.CalculateScores(tasks, Func<TaskItem, double> scoringPolicy)` is the higher-order function; it invokes `scoringPolicy(x)` for every task. The policy is chosen in `Program.Main` and passed as an argument (no global setting).

* **standardPolicy:** `Urgency * 10`
* **expressPolicy:** `Urgency * 15 + EstimatedHours * 2`

## 5. Demonstration dataset and documented expected results

| Line | Record | Expected |
| :--- | :--- | :--- |
| 1 | `Fix Login Bug,3.5,4,false` | accepted; std 40, express 67 |
| 2 | `Refactor Database Query,-2.0,5,false` | rejected: hours must be > 0 |
| 3 | `Write Unit Tests,4.0,3,true` | accepted, removed by stage 1 (completed) |
| 4 | `,5.0,2,false` | rejected: empty name |
| 5 | `Deploy to Production,8.0,6,false` | rejected: urgency 6 |
| 6 | `Update Documentation,abc,1,false` | rejected: hours not a number |
| 7 | `Code Review,1.5,3,false` | accepted; std 30, express 48 |
| 8 | `Security Patch,0.5,5,false` | accepted, **boundary** urgency 5; std 50, express 76 |
| 9 | `Plan Sprint,20.0,2,false` | accepted; std 20, express 70 (**order changes between policies**) |
| 10 | `Broken Line,2.0` | rejected: 2 fields instead of 4 |
| 11 | `Idle Task,0,3,false` | rejected, **boundary** hours = 0 is excluded |

Expected order: standard = Security Patch, Fix Login Bug, Code Review, Plan Sprint. Express = Security Patch, **Plan Sprint**, Fix Login Bug, Code Review. `Plan Sprint` moves from 4th to 2nd place only because the policy changed.

Expected console output:

```text
Validated task and added: Fix Login Bug
Validated task and added: Write Unit Tests
Validated task and added: Code Review
Validated task and added: Security Patch
Validated task and added: Plan Sprint
Line 2: Validation error: Invalid estimated hours '-2.0'. Must be a number greater than 0.
Line 4: Validation error: Task name is missing or empty.
Line 5: Validation error: Invalid urgency '6'. Must be an integer between 1 and 5.
Line 6: Validation error: Invalid estimated hours 'abc'. Must be a number greater than 0.
Line 10: Validation error: Expected 4 comma-separated fields but found 2.
Line 11: Validation error: Invalid estimated hours '0'. Must be a number greater than 0.
Accepted: 5, rejected: 6

 --- Starting Pipeline --- 
=== RUNNING WITH STANDARD POLICY ===
1. Task: Security Patch, Score: 50.0
2. Task: Fix Login Bug, Score: 40.0
3. Task: Code Review, Score: 30.0
4. Task: Plan Sprint, Score: 20.0
Total score: 140.0, total hours: 25.5
=== RUNNING WITH EXPRESS POLICY ===
1. Task: Security Patch, Score: 76.0
2. Task: Plan Sprint, Score: 70.0
3. Task: Fix Login Bug, Score: 67.0
4. Task: Code Review, Score: 48.0
Total score: 261.0, total hours: 25.5
Order changed by policy: yes
```

## 6. Build, run, test

Prerequisite: .NET 10 SDK. Run all commands from the folder that contains `assignment5_ifp.slnx` (check from a clean extraction).

```bash
dotnet build assignment5_ifp.slnx
dotnet run --project assignment5_ifp
dotnet test SelectDailyTasksApp.Tests
```

* `tasks.txt` is copied next to the executable, so the working directory does not matter.
* Another file: `dotnet run --project assignment5_ifp -- path/to/file.txt`.
* **Failure demo:** `dotnet run --project assignment5_ifp -- missing.txt` prints `Error: Could not find 'missing.txt' file.` and exits with code 1.
* **Empty dataset demo:** pass an empty file; both policies print `No active tasks to plan.`

## 7. Requirement mapping

| Requirement | Files / methods |
| :--- | :--- |
| Immutable models | `Models/TaskItem.cs`, `ScoredTaskItem.cs`, `DailyPlan.cs` (`ImmutableArray`), `TaskLoadResult.cs`, raw text in `RawTaskDto.cs` |
| Parsing and validation at the boundary | `Validation/TaskValidator.cs`: `TryParseAndValidate`, `TryParseLine`, `ParseLines` |
| File input only | `DataIO/TaskFileReader.cs`: `TryReadLines` (missing file ≠ empty file) |
| 3 pure composed stages | `Pipeline/TaskPipeline.cs`; composition in `Program.BuildPlan` |
| Higher-order function + 2 policies | `TaskPipeline.CalculateScores`, `Rules/Policies.cs`, policy chosen in `Program.Main` |
| Output after computation | `Program.PrintPlan` |
| Tests | `SelectDailyTasksApp.Tests/TaskWorkflowTests.cs` |

## 8. Tests (xUnit)

| Required case | Tests |
| :--- | :--- |
| Normal workflow | `NormalWorkflow_PassesValidDataset_ProducesExpectedResult`, `NormalWorkflow_FromFileLines_ProducesDocumentedResult` |
| Empty dataset | `EmptyDataset_ProducesEmptyPlanWithoutCrashing`, `EmptyDataset_NoLines_IsValidAndHasNoErrors`, `EmptyDataset_EmptyFile_IsReadSuccessfully_UnlikeMissingFile` |
| Boundary value | `BoundaryValue_UrgencyLimits_AreInclusive`, `BoundaryValue_EstimatedHours_MustBeStrictlyGreaterThanZero`, `BoundaryValue_MaxUrgency_IsScoredByBothPolicies`, `LegitimateValues_FalseCompletion_IsAcceptedAndNameIsTrimmed` |
| Invalid and missing input | `InvalidInput_MalformedOrMissingFields_AreRejected`, `InvalidInput_NullRecordAndNullLine_AreRejected`, `InvalidInput_WrongFieldCount_IsRejected`, `InvalidInput_ParseLines_ReportsLineNumbersAndKeepsValidTasks`, `InvalidInput_BlankLinesAreIgnoredButNullLineIsAnError`, `MissingInput_NullArguments_AreRejectedWithoutResult`, `MissingInput_NullOrMissingFilePath_IsRejected` |
| Two policies | `TwoPolicies_ProduceDifferentOrderOnSameInput`, `TwoPolicies_ProduceDifferentScoresForSameTask`, `HigherOrderFunction_InvokesDelegateOncePerTask` |
| Purity and stages | `Stage1_...`, `Stage2_...`, `Stage3_...`, `Purity_StagesDoNotModifyCallerOwnedCollections`, `Purity_SameInputAndPolicy_GiveSameOutput` |

**Latest test outcome:** `<paste the result of dotnet test here, e.g. "Passed! - Failed: 0, Passed: N">`

## 9. Team contributions

| Member (name, group) | Implemented logic | Tests | Documentation |
| :--- | :--- | :--- | :--- |
| `<fill in>` | `<e.g. validation, pipeline stages>` | `<...>` | `<...>` |
