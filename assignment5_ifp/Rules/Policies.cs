using SelectDailyTasksApp.Models;

namespace SelectDailyTasksApp.Rules
{
    // две политики одного правила "как считать score задачи"
    // обе чистые: зависят только от аргумента, без глобального состояния
    public static class Policies
    {
        public static double standardPolicy(TaskItem item) => item.Urgency * 10.0;
        public static double expressPolicy(TaskItem item) => (item.Urgency * 15.0) + (item.EstimatedHours * 2.0);
    }
}
