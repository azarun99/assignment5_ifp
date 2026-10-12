using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Validation;
using SelectDailyTasksApp.Pipeline;

namespace SelectDailyTasksApp.Tests
{
    public class TaskWorkflowTests
    {
        // 1. NORMAL WORKFLOW: Валидный датасет проходит через пайплайн и дает ожидаемый результат[cite: 7]
        [Fact]
        public void NormalWorkflow_PassesValidDataset_ProducesExpectedResult()
        {
            var tasks = new[] { new TaskItem("Fix Bug", 2.0, 4, false) };

            var active = TaskPipeline.FilterActiveTasks(tasks);
            var scored = TaskPipeline.CalculateScores(active, t => t.Urgency * 10).ToList();
            var sorted = TaskPipeline.SortAndFinalize(scored).ToList();

            Assert.Single(sorted);
            Assert.Equal("Fix Bug", sorted[0].Task.TaskName);
            Assert.Equal(40, sorted[0].Score);
        }

        // 2. EMPTY DATASET: Пустой набор данных отрабатывает без ошибок[cite: 7]
        [Fact]
        public void EmptyDataset_ProducesEmptyResultWithoutCrashing()
        {
            var tasks = Array.Empty<TaskItem>();

            var active = TaskPipeline.FilterActiveTasks(tasks);
            var scored = TaskPipeline.CalculateScores(active, t => t.Urgency * 10).ToList();

            Assert.Empty(scored);
        }

        // 3. BOUNDARY VALUE: Проверка граничного значения (например, крайнее допустимое число)[cite: 7]
        [Fact]
        public void BoundaryValue_UrgencyAtMaximumLimit_PassesValidation()
        {
            // Граничное значение приоритета = 5
            var raw = new RawTaskDto("Edge Task", "1.0", "5", "false");

            bool success = TaskValidator.TryParseAndValidate(raw, out TaskItem? task, out _);

            Assert.True(success);
            Assert.NotNull(task);
            Assert.Equal(5, task.Urgency);
        }

        // 4. INVALID AND MISSING INPUT: Некорректные или пустые поля отклоняются[cite: 7]
        [Fact]
        public void InvalidInput_MalformedHours_IsRejected()
        {
            var raw = new RawTaskDto("Bad Task", "abc", "3", "false");

            bool success = TaskValidator.TryParseAndValidate(raw, out TaskItem? task, out string error);

            Assert.False(success);
            Assert.Null(task);
            Assert.False(string.IsNullOrEmpty(error));
        }

        // 5. TWO POLICIES: Проверка, что две разные политики дают разные результаты на одних данных[cite: 7]
        [Fact]
        public void TwoPolicies_ProduceDifferentScoresOnSameInput()
        {
            var task = new TaskItem("Task", 3.0, 4, false);

            Func<TaskItem, double> standardPolicy = t => t.Urgency * 10;
            Func<TaskItem, double> expressPolicy = t => (t.Urgency * 15) + (t.EstimatedHours * 2);

            double scoreStandard = standardPolicy(task);
            double scoreExpress = expressPolicy(task);

            Assert.NotEqual(scoreStandard, scoreExpress);
            Assert.Equal(40, scoreStandard);
            Assert.Equal(66, scoreExpress);
        }

        // 6. PURITY AND STAGES: Исходная коллекция вызывающего кода остается неизменной (иммунность)[cite: 7]
        [Fact]
        public void Purity_InputCollectionRemainsUnchanged()
        {
            var originalList = new List<TaskItem>
        {
            new TaskItem("Task 1", 2.0, 3, false)
        };
            int initialCount = originalList.Count;

            // Запуск пайплайна
            var filtered = TaskPipeline.FilterActiveTasks(originalList).ToList();

            // Проверяем, что исходный список не был модифицирован методами пайплайна
            Assert.Equal(initialCount, originalList.Count);
        }
    }

}

