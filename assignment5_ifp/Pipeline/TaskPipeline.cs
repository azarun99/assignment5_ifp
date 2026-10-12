using System.Collections.Immutable;
using SelectDailyTasksApp.Models;

namespace SelectDailyTasksApp.Pipeline
{
    // три чистые стадии: каждая получает входную коллекцию, не меняет её и возвращает новую неизменяемую
    public static class TaskPipeline
    {
        // Stage 1: IEnumerable<TaskItem> -> ImmutableArray<TaskItem>; оставляем только невыполненные задачи
        public static ImmutableArray<TaskItem> FilterActiveTasks(IEnumerable<TaskItem> tasks)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            return tasks.Where(x => !x.IsCompleted).ToImmutableArray();
        }

        // Stage 2 (higher-order): задачи + Func-политика -> ImmutableArray<ScoredTaskItem>
        // политика реально вызывается для каждой задачи, ToImmutableArray вычисляет всё сразу (без отложенного выполнения)
        public static ImmutableArray<ScoredTaskItem> CalculateScores(IEnumerable<TaskItem> tasks, Func<TaskItem, double> scoringPolicy)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            ArgumentNullException.ThrowIfNull(scoringPolicy);
            return tasks.Select(x => new ScoredTaskItem(x, scoringPolicy(x))).ToImmutableArray();
        }

        // Stage 3: IEnumerable<ScoredTaskItem> -> DailyPlan; сортировка по score (убывание), при равенстве по имени, плюс итоги
        public static DailyPlan SortAndFinalize(IEnumerable<ScoredTaskItem> scoredTasks)
        {
            ArgumentNullException.ThrowIfNull(scoredTasks);

            var ordered = scoredTasks
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Task.TaskName, StringComparer.Ordinal)
                .ToImmutableArray();

            return new DailyPlan(ordered, ordered.Sum(x => x.Score), ordered.Sum(x => x.Task.EstimatedHours));
        }
    }
}
