using SelectDailyTasksApp.DataIO;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Pipeline;
using SelectDailyTasksApp.Rules;
using SelectDailyTasksApp.Validation;

namespace SelectDailyTasksApp.Tests
{
    public class TaskWorkflowTests
    {
        // ---------- вспомогательные данные и композиция ----------

        // каждый тест получает свежий список, чтобы тесты не влияли друг на друга
        private static List<TaskItem> CreateTasks() => new()
        {
            new TaskItem("Fix Login Bug", 3.5, 4, false),
            new TaskItem("Write Unit Tests", 4.0, 3, true),   // выполнена -> отфильтруется стадией 1
            new TaskItem("Code Review", 1.5, 3, false),
            new TaskItem("Security Patch", 0.5, 5, false),    // граница: urgency = 5
            new TaskItem("Plan Sprint", 20.0, 2, false)
        };

        // те же строки, что в tasks.txt
        private static readonly string[] DemoLines =
        {
            "Fix Login Bug,3.5,4,false",
            "Refactor Database Query,-2.0,5,false",
            "Write Unit Tests,4.0,3,true",
            ",5.0,2,false",
            "Deploy to Production,8.0,6,false",
            "Update Documentation,abc,1,false",
            "Code Review,1.5,3,false",
            "Security Patch,0.5,5,false",
            "Plan Sprint,20.0,2,false",
            "Broken Line,2.0",
            "Idle Task,0,3,false"
        };

        private static DailyPlan RunWorkflow(IEnumerable<TaskItem> tasks, Func<TaskItem, double> policy) =>
            TaskPipeline.SortAndFinalize(TaskPipeline.CalculateScores(TaskPipeline.FilterActiveTasks(tasks), policy));

        private static string[] Names(DailyPlan plan) => plan.Items.Select(x => x.Task.TaskName).ToArray();

        // ---------- 1. NORMAL WORKFLOW ----------

        [Fact]
        public void NormalWorkflow_PassesValidDataset_ProducesExpectedResult()
        {
            var plan = RunWorkflow(CreateTasks(), Policies.standardPolicy);

            // Write Unit Tests выполнена, поэтому в плане 4 задачи
            Assert.Equal(new[] { "Security Patch", "Fix Login Bug", "Code Review", "Plan Sprint" }, Names(plan));
            Assert.Equal(new[] { 50.0, 40.0, 30.0, 20.0 }, plan.Items.Select(x => x.Score).ToArray());
            Assert.Equal(140.0, plan.TotalScore);
            Assert.Equal(25.5, plan.TotalHours);
        }

        [Fact]
        public void NormalWorkflow_FromFileLines_ProducesDocumentedResult()
        {
            var loaded = TaskValidator.ParseLines(DemoLines);
            var plan = RunWorkflow(loaded.Tasks, Policies.standardPolicy);

            Assert.Equal(5, loaded.Tasks.Length);
            Assert.Equal(6, loaded.Errors.Length);
            Assert.Equal(new[] { "Security Patch", "Fix Login Bug", "Code Review", "Plan Sprint" }, Names(plan));
        }

        // ---------- 2. EMPTY DATASET ----------

        [Fact]
        public void EmptyDataset_ProducesEmptyPlanWithoutCrashing()
        {
            var plan = RunWorkflow(Array.Empty<TaskItem>(), Policies.standardPolicy);

            Assert.True(plan.Items.IsEmpty);
            Assert.Equal(0.0, plan.TotalScore);
            Assert.Equal(0.0, plan.TotalHours);
        }

        [Fact]
        public void EmptyDataset_NoLines_IsValidAndHasNoErrors()
        {
            var loaded = TaskValidator.ParseLines(Array.Empty<string>());

            Assert.True(loaded.Tasks.IsEmpty);
            Assert.True(loaded.Errors.IsEmpty);
        }

        [Fact]
        public void EmptyDataset_EmptyFile_IsReadSuccessfully_UnlikeMissingFile()
        {
            string path = Path.GetTempFileName(); // создаёт пустой файл
            try
            {
                bool emptyOk = TaskFileReader.TryReadLines(path, out string[]? lines, out _);
                bool missingOk = TaskFileReader.TryReadLines(path + ".missing", out string[]? missingLines, out string error);

                Assert.True(emptyOk);
                Assert.NotNull(lines);
                Assert.Empty(lines);

                Assert.False(missingOk);
                Assert.Null(missingLines);
                Assert.False(string.IsNullOrEmpty(error));
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ---------- 3. BOUNDARY VALUE ----------

        [Theory]
        [InlineData("1", true)]    // минимум включён
        [InlineData("5", true)]    // максимум включён
        [InlineData("0", false)]   // ниже минимума
        [InlineData("6", false)]   // выше максимума
        public void BoundaryValue_UrgencyLimits_AreInclusive(string urgency, bool expectedValid)
        {
            var raw = new RawTaskDto("Edge Task", "1.0", urgency, "false");

            bool success = TaskValidator.TryParseAndValidate(raw, out TaskItem? task, out _);

            Assert.Equal(expectedValid, success);
            Assert.Equal(expectedValid, task is not null);
        }

        [Theory]
        [InlineData("0.01", true)]  // чуть больше нуля - допустимо
        [InlineData("0", false)]    // ровно 0 - граница исключена (hours > 0)
        [InlineData("-0.01", false)]
        public void BoundaryValue_EstimatedHours_MustBeStrictlyGreaterThanZero(string hours, bool expectedValid)
        {
            var raw = new RawTaskDto("Edge Task", hours, "3", "false");

            Assert.Equal(expectedValid, TaskValidator.TryParseAndValidate(raw, out _, out _));
        }

        [Fact]
        public void BoundaryValue_MaxUrgency_IsScoredByBothPolicies()
        {
            var task = new TaskItem("Security Patch", 0.5, TaskValidator.MaxUrgency, false);

            Assert.Equal(50.0, Policies.standardPolicy(task));
            Assert.Equal(76.0, Policies.expressPolicy(task)); // 5*15 + 0.5*2
        }

        [Fact]
        public void LegitimateValues_FalseCompletion_IsAcceptedAndNameIsTrimmed()
        {
            var raw = new RawTaskDto("  Task  ", " 2.5 ", " 3 ", " false ");

            bool success = TaskValidator.TryParseAndValidate(raw, out TaskItem? task, out _);

            Assert.True(success);
            Assert.NotNull(task);
            Assert.Equal("Task", task.TaskName);
            Assert.Equal(2.5, task.EstimatedHours);
            Assert.False(task.IsCompleted); // false - валидное значение, а не "нет данных"
        }

        // ---------- 4. INVALID AND MISSING INPUT ----------

        [Theory]
        [InlineData("Bad Task", "abc", "3", "false")]
        [InlineData("Bad Task", "-2.0", "3", "false")]
        [InlineData("Bad Task", "3,5", "3", "false")]        // запятая - не десятичный разделитель
        [InlineData("Bad Task", "NaN", "3", "false")]
        [InlineData("Bad Task", "Infinity", "3", "false")]
        [InlineData("Bad Task", "1.0", "six", "false")]
        [InlineData("Bad Task", "1.0", "3.5", "false")]
        [InlineData("Bad Task", "1.0", "3", "maybe")]
        [InlineData("", "1.0", "3", "false")]
        [InlineData("   ", "1.0", "3", "false")]
        [InlineData(null, "1.0", "3", "false")]
        [InlineData("Bad Task", null, "3", "false")]
        [InlineData("Bad Task", "1.0", null, "false")]
        [InlineData("Bad Task", "1.0", "3", null)]
        public void InvalidInput_MalformedOrMissingFields_AreRejected(string? name, string? hours, string? urgency, string? completed)
        {
            var raw = new RawTaskDto(name, hours, urgency, completed);

            bool success = TaskValidator.TryParseAndValidate(raw, out TaskItem? task, out string error);

            Assert.False(success);
            Assert.Null(task); // результат не выдуман
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void InvalidInput_NullRecordAndNullLine_AreRejected()
        {
            Assert.False(TaskValidator.TryParseAndValidate(null, out TaskItem? task1, out string error1));
            Assert.Null(task1);
            Assert.False(string.IsNullOrEmpty(error1));

            Assert.False(TaskValidator.TryParseLine(null, out TaskItem? task2, out string error2));
            Assert.Null(task2);
            Assert.False(string.IsNullOrEmpty(error2));
        }

        [Theory]
        [InlineData("Broken Line,2.0")]
        [InlineData("Name,1.0,3,false,extra")]
        [InlineData("")]
        public void InvalidInput_WrongFieldCount_IsRejected(string line)
        {
            Assert.False(TaskValidator.TryParseLine(line, out TaskItem? task, out string error));
            Assert.Null(task);
            Assert.Contains("fields", error);
        }

        [Fact]
        public void InvalidInput_ParseLines_ReportsLineNumbersAndKeepsValidTasks()
        {
            var loaded = TaskValidator.ParseLines(DemoLines);

            Assert.Equal(
                new[] { "Fix Login Bug", "Write Unit Tests", "Code Review", "Security Patch", "Plan Sprint" },
                loaded.Tasks.Select(x => x.TaskName).ToArray());
            Assert.Equal(
                new[] { "Line 2:", "Line 4:", "Line 5:", "Line 6:", "Line 10:", "Line 11:" },
                loaded.Errors.Select(e => e.Substring(0, e.IndexOf(':') + 1)).ToArray());
        }

        [Fact]
        public void InvalidInput_BlankLinesAreIgnoredButNullLineIsAnError()
        {
            var loaded = TaskValidator.ParseLines(new string?[] { "", "   ", "Task,1.0,3,false", null });

            Assert.Equal(1, loaded.Tasks.Length);
            Assert.Equal(1, loaded.Errors.Length);
            Assert.StartsWith("Line 4:", loaded.Errors[0]);
        }

        [Fact]
        public void MissingInput_NullArguments_AreRejectedWithoutResult()
        {
            // null! используется только в тесте, чтобы проверить защиту от null
            Assert.Throws<ArgumentNullException>(() => { TaskValidator.ParseLines(null!); });
            Assert.Throws<ArgumentNullException>(() => { TaskPipeline.FilterActiveTasks(null!); });
            Assert.Throws<ArgumentNullException>(() => { TaskPipeline.CalculateScores(null!, Policies.standardPolicy); });
            Assert.Throws<ArgumentNullException>(() => { TaskPipeline.CalculateScores(CreateTasks(), null!); });
            Assert.Throws<ArgumentNullException>(() => { TaskPipeline.SortAndFinalize(null!); });
        }

        [Fact]
        public void MissingInput_NullOrMissingFilePath_IsRejected()
        {
            Assert.False(TaskFileReader.TryReadLines(null, out string[]? lines1, out string error1));
            Assert.Null(lines1);
            Assert.False(string.IsNullOrEmpty(error1));

            Assert.False(TaskFileReader.TryReadLines("definitely_missing_tasks_file.txt", out string[]? lines2, out string error2));
            Assert.Null(lines2);
            Assert.False(string.IsNullOrEmpty(error2));
        }

        // ---------- 5. TWO POLICIES ----------

        [Fact]
        public void TwoPolicies_ProduceDifferentOrderOnSameInput()
        {
            var tasks = CreateTasks();

            var standard = RunWorkflow(tasks, Policies.standardPolicy);
            var express = RunWorkflow(tasks, Policies.expressPolicy);

            Assert.Equal(new[] { "Security Patch", "Fix Login Bug", "Code Review", "Plan Sprint" }, Names(standard));
            Assert.Equal(new[] { "Security Patch", "Plan Sprint", "Fix Login Bug", "Code Review" }, Names(express));
            Assert.Equal(new[] { 76.0, 70.0, 67.0, 48.0 }, express.Items.Select(x => x.Score).ToArray());
            Assert.Equal(261.0, express.TotalScore);
            Assert.Equal(standard.TotalHours, express.TotalHours); // меняется только правило, не данные
            Assert.NotEqual(Names(standard), Names(express));
        }

        [Fact]
        public void TwoPolicies_ProduceDifferentScoresForSameTask()
        {
            var task = new TaskItem("Task", 3.0, 4, false);

            Assert.Equal(40.0, Policies.standardPolicy(task));
            Assert.Equal(66.0, Policies.expressPolicy(task)); // 4*15 + 3*2
        }

        [Fact]
        public void HigherOrderFunction_InvokesDelegateOncePerTask()
        {
            var tasks = CreateTasks();
            int calls = 0;
            Func<TaskItem, double> countingPolicy = t => { calls++; return t.Urgency; };

            var scored = TaskPipeline.CalculateScores(tasks, countingPolicy);

            Assert.Equal(tasks.Count, calls);
            Assert.Equal(tasks.Count, scored.Length);
        }

        // ---------- 6. PURITY AND STAGES ----------

        [Fact]
        public void Stage1_FilterActiveTasks_RemovesCompletedOnly()
        {
            var active = TaskPipeline.FilterActiveTasks(CreateTasks());

            Assert.Equal(4, active.Length);
            Assert.False(active.Any(t => t.IsCompleted));
            Assert.False(active.Any(t => t.TaskName == "Write Unit Tests"));
        }

        [Fact]
        public void Stage2_CalculateScores_UsesTheSuppliedPolicy()
        {
            var tasks = new[] { new TaskItem("A", 1.0, 2, false), new TaskItem("B", 1.0, 4, false) };

            var scored = TaskPipeline.CalculateScores(tasks, t => t.Urgency * 100.0);

            Assert.Equal(new[] { 200.0, 400.0 }, scored.Select(x => x.Score).ToArray());
            Assert.Equal(new[] { "A", "B" }, scored.Select(x => x.Task.TaskName).ToArray());
        }

        [Fact]
        public void Stage3_SortAndFinalize_OrdersByScoreThenNameAndSumsTotals()
        {
            var input = new[]
            {
                new ScoredTaskItem(new TaskItem("B", 1.0, 3, false), 30.0),
                new ScoredTaskItem(new TaskItem("A", 2.0, 3, false), 30.0),   // равный score -> сортировка по имени
                new ScoredTaskItem(new TaskItem("C", 4.0, 5, false), 50.0)
            };

            var plan = TaskPipeline.SortAndFinalize(input);

            Assert.Equal(new[] { "C", "A", "B" }, Names(plan));
            Assert.Equal(110.0, plan.TotalScore);
            Assert.Equal(7.0, plan.TotalHours);
        }

        [Fact]
        public void Purity_StagesDoNotModifyCallerOwnedCollections()
        {
            var original = CreateTasks();
            var snapshot = original.ToList();

            var active = TaskPipeline.FilterActiveTasks(original);
            var scored = TaskPipeline.CalculateScores(active, Policies.standardPolicy);
            var scoredSnapshot = scored.ToList();
            var plan = TaskPipeline.SortAndFinalize(scored);

            // входной список не изменён: те же элементы в том же порядке
            Assert.Equal(snapshot, original);
            // вход стадии 3 тоже не переупорядочен
            Assert.Equal(scoredSnapshot, scored.ToList());

            // результат - независимая копия: изменение исходного списка потом его не затрагивает
            original.Clear();
            Assert.Equal(4, active.Length);
            Assert.Equal(4, plan.Items.Length);
        }

        [Fact]
        public void Purity_SameInputAndPolicy_GiveSameOutput()
        {
            var tasks = CreateTasks();

            var first = RunWorkflow(tasks, Policies.expressPolicy);
            var second = RunWorkflow(tasks, Policies.expressPolicy);

            Assert.Equal(first.Items.ToList(), second.Items.ToList());
            Assert.Equal(first.TotalScore, second.TotalScore);
            Assert.Equal(first.TotalHours, second.TotalHours);
        }
    }
}
