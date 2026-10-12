using System;
using System.Collections.Generic;
using System.Text;

namespace SelectDailyTasksApp.Models
{
    // isn't raw, it is a rich, strongly-typed domain model (record) that contains correct native types
    public record TaskItem(
        string TaskName,
        double EstimatedHours,
        int Urgency,
        bool IsCompleted
    );


}
