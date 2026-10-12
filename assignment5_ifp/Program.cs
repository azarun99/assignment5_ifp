using System;
using System.ComponentModel.DataAnnotations;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Validation;
using SelectDailyTasksApp.DataIO;
using SelectDailyTasksApp.Pipeline;
using SelectDailyTasksApp.Rules;

class Program
{
    public static void Main(string[] args)
    {
        // чтобы tasks.txt считывался с папки где Program.cs а не bin/debug/net
        string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
        string filePath = Path.Combine(projectRoot, "tasks.txt");

        // закидываем считываешься и валидные таски в пайплайн фильтры
        var validatedTasks = TaskFileReader.LoadTasksFromFile(filePath);
        Console.WriteLine("\n --- Starting Pipeline --- ");
        Console.WriteLine("=== RUNNING WITH STANDARD POLICY ===");
        RunWorkflow(validatedTasks, Policies.standardPolicy);
        Console.WriteLine("=== RUNNING WITH EXPRESS POLICY ===");
        RunWorkflow(validatedTasks, Policies.expressPolicy);
    }

    static void RunWorkflow(IEnumerable<TaskItem> tasks, Func<TaskItem, double> policy)
    {
        var result = TaskPipeline.SortAndFinalize(TaskPipeline.CalculateScores(TaskPipeline.FilterActiveTasks(tasks), policy));
        foreach(var item in result)
        {
            Console.WriteLine($"Task: {item.Task.TaskName}, Score: {item.Score}");
        }
    }
}
