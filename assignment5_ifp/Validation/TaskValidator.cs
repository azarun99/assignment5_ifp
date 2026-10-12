using System;
using System.Collections.Generic;
using System.Text;
using SelectDailyTasksApp.Models;
using System.Globalization;

namespace SelectDailyTasksApp.Validation
{
    public static class TaskValidator
    {
        public static bool TryParseAndValidate(RawTaskDto raw, out TaskItem? validTask, out string errorMessage)
        {
            validTask = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(raw.TaskName))
            {
                errorMessage = "Validation error: Task name is missing or empty.";
                return false;
            }
            // допустим без InvariantCulture и NumberStyle.Any у меня не работает парс 3.0 а 3,0 работает потому что система на русском
            // поэтому я сделал индеференцию точки и запятой когда закидываем стринг в парс
            if (!double.TryParse(raw.EstimatedHours, NumberStyles.Any, CultureInfo.InvariantCulture, out double hours) || hours <= 0)
            {
                errorMessage = $"Validation error: Invalid estimated hours '{raw.EstimatedHours}'. Must be greater than 0.";
                return false;
            }

            if (!int.TryParse(raw.Urgency, out int urgency) || urgency < 1 || urgency > 5)
            {
                errorMessage = $"Validation error: Invalid error '{raw.Urgency}'. Must be between 1 and 5.";
                return false;
            }

            if (!bool.TryParse(raw.IsCompleted, out bool isCompleted))
            {
                errorMessage = $"Validation error: Invalid completion status '{raw.IsCompleted}'. Must be true or false.";
                return false;
            }

            validTask = new TaskItem(raw.TaskName.Trim(), hours, urgency, isCompleted);
            return true;
        }
    }
}
