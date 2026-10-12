using System;
using System.Collections.Generic;
using System.Text;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Validation;

namespace SelectDailyTasksApp.Pipeline
{
    public record ScoredTaskItem(TaskItem Task, double Score);
    public static class TaskPipeline
    {
        public static IEnumerable<TaskItem> FilterActiveTasks(IEnumerable<TaskItem> tasks) =>
                tasks.Where(x => !x.IsCompleted);

        public static IEnumerable<ScoredTaskItem> CalculateScores(IEnumerable<TaskItem> tasks, Func<TaskItem, double> scoringPolicy) =>
            tasks.Select(x => new ScoredTaskItem(x, scoringPolicy(x)));

        public static IEnumerable<ScoredTaskItem> SortAndFinalize(IEnumerable<ScoredTaskItem> scoredTasks) =>
            scoredTasks.OrderByDescending(x => x.Score);
    }
}
