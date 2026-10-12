namespace SelectDailyTasksApp.Models
{
    // isn't raw, it is a rich, strongly-typed domain model (record) that contains correct native types
    // все поля - неизменяемые значения (string, double, int, bool), поэтому record полностью immutable
    public record TaskItem(
        string TaskName,
        double EstimatedHours,
        int Urgency,
        bool IsCompleted
    );
}
