# Task Selection Workflow Application (Assignment 5)

## Overview & Purpose
This application implements a functional, pure-pipeline architecture in C# for processing, validating, scoring, and prioritizing daily task workloads. Built according to Functional Programming principles, the system separates input parsing and I/O boundaries from core domain calculations and immutability guarantees.

* **Target Audience:** Software developers and project managers needing a predictable, automated daily task prioritization tool.
* **SDK Version:** .NET 10.0 (compatible with .NET 8.0+)
* **Architecture Style:** Pure Functional Core / Imperative Shell Architecture

---

## Data Schema & Business Rules

### Input Fields (`tasks.txt`)
Data is read line-by-line from a CSV-formatted text file (`tasks.txt`):
* `TaskName` (*string*): Descriptive name of the task. Must not be empty or whitespace.
* `EstimatedHours` (*double*): Time estimate in hours. Must be strictly greater than `0`.
* `Urgency` (*int*): Task priority level. Must be an integer between `1` (lowest) and `5` (highest).
* `IsCompleted` (*bool*): Current state of completion (`true` or `false`).

### Pipeline & Domain Rules
1. **Validation Stage (Boundary):** Raw text entries are parsed safely using `CultureInfo.InvariantCulture`. Invalid entries (missing names, non-numeric values, out-of-bound numbers) are rejected with clear error messages without raising unhandled exceptions or introducing default values.
2. **Filter Stage (Pure):** Selects only active (uncompleted) tasks (`IsCompleted == false`).
3. **Scoring Stage (Higher-Order Function):** Computes a dynamic score for each task based on a configurable pricing policy delegate (`Func<TaskItem, double>`).
4. **Sorting Stage (Pure):** Orders scored task records in descending order according to their calculated score.

---

## Configurable Policies (Task 4)

The scoring engine relies on a higher-order function that accepts dynamic strategies (`Func<TaskItem, double>`) executed outside the pure core:

* **Standard Policy:** Emphasizes raw priority.  
  $$\text{Score} = \text{Urgency} \times 10.0$$
* **Express Policy:** Weighs urgency higher while factoring in labor effort.  
  $$\text{Score} = (\text{Urgency} \times 15.0) + (\text{EstimatedHours} \times 2.0)$$

---

## File and Project Mapping

| Requirement | Implementation File / Module | Method / Construct |
| :--- | :--- | :--- |
| **Domain Models** | `Models/TaskItem.cs`<br>`Models/ScoredTaskItem.cs` | Immutable `record` definitions |
| **Boundary Input & Validation** | `Validation/TaskValidator.cs`<br>`Data/TaskFileReader.cs` | `TryParseAndValidate(...)`<br>`LoadTasksFromFile(...)` |
| **Pure Pipeline Stages** | `Pipeline/TaskPipeline.cs` | `FilterActiveTasks(...)`<br>`CalculateScores(...)`<br>`SortTasks(...)` |
| **Higher-Order Policy Rule** | `Pipeline/TaskPipeline.cs`<br>`Program.cs` | `Func<TaskItem, double> scoringPolicy`<br>`RunWorkflow(...)` |
| **Automated Tests** | `SelectDailyTasksApp.Tests/TaskWorkflowTests.cs` | `xUnit` test suite (6 requirement cases) |

---

## Setup & Running Instructions

### Prerequisites
* .NET SDK 8.0, 9.0, or 10.0 installed on your system.

### Build and Run Application
Execute the following commands from the project root directory (where the `.csproj` file is located):

```bash
# Build the project
dotnet build

# Run the console workflow
dotnet run
```

### Run Automated Unit Tests
To execute the full xUnit test suite covering normal workflow, empty datasets, boundary cases, invalid inputs, policy variance, and state purity:

```bash
dotnet test
```

---

## Demonstration Dataset & Expected Output

### Input Dataset (`tasks.txt`)
```text
Fix Login Bug,3.5,4,false
Refactor Database Query,-2.0,5,false
Write Unit Tests,4.0,3,true
,5.0,2,false
Deploy to Production,8.0,6,false
Update Documentation,abc,1,false
Code Review,1.5,3,false
```

### Console Output
```text
Validation error: Invalid estimated hours '-2.0'. Must be greater than 0.
Validation error: Task name is missing or empty.
Validation error: Invalid urgency '6'. Must be between 1 and 5.
Validation error: Invalid estimated hours 'abc'. Must be greater than 0.

--- Starting Pipeline ---

=== RUNNING WITH STANDARD POLICY ===
Fix Login Bug, Score: 40
Code Review, Score: 30

=== RUNNING WITH EXPRESS POLICY ===
Fix Login Bug, Score: 67
Code Review, Score: 48
```

---

## Test Coverage Summary

All 6 required test scenarios have been implemented and validated:
1. `NormalWorkflow_PassesValidDataset_ProducesExpectedResult`: Validates processing across all pipeline stages.
2. `EmptyDataset_ProducesEmptyResultWithoutCrashing`: Guarantees safe execution with zero input records.
3. `BoundaryValue_UrgencyAtMaximumLimit_PassesValidation`: Verifies upper threshold acceptance ($Urgency = 5$).
4. `InvalidInput_MalformedHours_IsRejected`: Confirms non-numeric hours are caught without throwing exceptions.
5. `TwoPolicies_ProduceDifferentScoresOnSameInput`: Verifies that switching rule policies changes scores dynamically on identical data.
6. `Purity_InputCollectionRemainsUnchanged`: Asserts caller collections remain unmodified throughout execution.