namespace SelectDailyTasksApp.Models
{
    // Dto - Data Transfer Object (a simple object used to transport data between different layers or systems)
    // сырой текст из файла: любое поле может быть null или мусором, проверка делается в TaskValidator
    public record RawTaskDto(
        string? TaskName,
        string? EstimatedHours,
        string? Urgency,
        string? IsCompleted
    );
}
