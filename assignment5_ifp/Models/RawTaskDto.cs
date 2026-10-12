using System;
using System.Collections.Generic;
using System.Text;

namespace SelectDailyTasksApp.Models
{
    // Dto - Data Transfer Object (a simple object used to transport data between different layers or systems)
    public record RawTaskDto(
        string? TaskName,
        string? EstimatedHours,
        string? Urgency,
        string? IsCompleted
    );
}
