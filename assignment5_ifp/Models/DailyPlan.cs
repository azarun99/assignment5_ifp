using System.Collections.Immutable;

namespace SelectDailyTasksApp.Models
{
    // результат стадии 3: упорядоченный план дня + итоги
    // ImmutableArray - настоящая неизменяемая копия, вызывающий код не может её изменить
    // (внимание: равенство record сравнивает ссылку на внутренний массив, поэтому в тестах сравниваем Items поэлементно)
    public record DailyPlan(
        ImmutableArray<ScoredTaskItem> Items,
        double TotalScore,
        double TotalHours
    );
}
