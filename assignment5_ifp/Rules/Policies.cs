using System;
using System.Collections.Generic;
using System.Text;
using SelectDailyTasksApp.Models;

namespace SelectDailyTasksApp.Rules
{
    public static class Policies
    {
        public static double standardPolicy(TaskItem item) => item.Urgency * 10.0;
        public static double expressPolicy(TaskItem item) => (item.Urgency * 15.0) + (item.EstimatedHours * 2.0);
    }
}
