using System.Globalization;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Validation;
using SelectDailyTasksApp.DataIO;
using SelectDailyTasksApp.Pipeline;
using SelectDailyTasksApp.Rules;

class Program
{
    public static int Main(string[] args)
    {
        // tasks.txt копируется в папку с exe (см. csproj), поэтому путь не зависит от рабочей папки
        // другой файл можно передать аргументом: dotnet run -- my_tasks.txt
        string filePath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "tasks.txt");

        // граница: читаем файл, потом валидируем; отсутствующий файл - ошибка, а не пустой датасет
        if (!TaskFileReader.TryReadLines(filePath, out string[]? lines, out string readError))
        {
            Console.WriteLine(readError);
            return 1;
        }
        TaskLoadResult loaded = TaskValidator.ParseLines(lines);

        foreach (var task in loaded.Tasks)
        {
            Console.WriteLine($"Validated task and added: {task.TaskName}");
        }
        foreach (var error in loaded.Errors)
        {
            Console.WriteLine(error);
        }
        Console.WriteLine($"Accepted: {loaded.Tasks.Length}, rejected: {loaded.Errors.Length}");

        // политика выбирается здесь, снаружи пайплайна, и передаётся аргументом
        Console.WriteLine("\n --- Starting Pipeline --- ");
        var standardPlan = BuildPlan(loaded.Tasks, Policies.standardPolicy);
        var expressPlan = BuildPlan(loaded.Tasks, Policies.expressPolicy);

        Console.WriteLine("=== RUNNING WITH STANDARD POLICY ===");
        PrintPlan(standardPlan);
        Console.WriteLine("=== RUNNING WITH EXPRESS POLICY ===");
        PrintPlan(expressPlan);

        bool orderChanged = !standardPlan.Items.Select(x => x.Task.TaskName)
            .SequenceEqual(expressPlan.Items.Select(x => x.Task.TaskName));
        Console.WriteLine($"Order changed by policy: {(orderChanged ? "yes" : "no")}");
        return 0;
    }

    // чистая часть: три стадии подряд, ничего не печатает
    static DailyPlan BuildPlan(IEnumerable<TaskItem> tasks, Func<TaskItem, double> policy) =>
        TaskPipeline.SortAndFinalize(TaskPipeline.CalculateScores(TaskPipeline.FilterActiveTasks(tasks), policy));

    // вывод только после того, как результат уже посчитан
    static void PrintPlan(DailyPlan plan)
    {
        if (plan.Items.IsEmpty)
        {
            Console.WriteLine("No active tasks to plan.");
            return;
        }
        for (int i = 0; i < plan.Items.Length; i++)
        {
            var item = plan.Items[i];
            Console.WriteLine($"{i + 1}. Task: {item.Task.TaskName}, Score: {Format(item.Score)}");
        }
        Console.WriteLine($"Total score: {Format(plan.TotalScore)}, total hours: {Format(plan.TotalHours)}");
    }

    // InvariantCulture, чтобы вывод не зависел от региона (67.5 а не 67,5)
    static string Format(double value) => value.ToString("F1", CultureInfo.InvariantCulture);
}
