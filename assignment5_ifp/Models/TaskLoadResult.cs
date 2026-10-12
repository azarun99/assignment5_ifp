using System.Collections.Immutable;

namespace SelectDailyTasksApp.Models
{
    // результат разбора файла: валидные задачи + сообщения об ошибках (с номерами строк)
    public record TaskLoadResult(
        ImmutableArray<TaskItem> Tasks,
        ImmutableArray<string> Errors
    );
}
