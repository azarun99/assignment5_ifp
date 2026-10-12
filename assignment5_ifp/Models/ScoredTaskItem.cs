namespace SelectDailyTasksApp.Models
{
    // результат стадии 2: задача + посчитанный по политике score
    public record ScoredTaskItem(TaskItem Task, double Score);
}
