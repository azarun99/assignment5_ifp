using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SelectDailyTasksApp.Models;
using SelectDailyTasksApp.Validation;

namespace SelectDailyTasksApp.DataIO
{
    public static class TaskFileReader
    {
        public static IEnumerable<TaskItem> LoadTasksFromFile(string filePath)
        {
            var validTasks = new List<TaskItem>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Could not find '{filePath}' file.");
                return validTasks;
            }

            string[] lines = File.ReadAllLines(filePath);

            foreach(var line in lines)
            {
                string[] words = line.Split(',');
                var raw = new RawTaskDto(words[0], words[1], words[2], words[3]);
                if(TaskValidator.TryParseAndValidate(raw, out TaskItem? validTask, out string errorMessage))
                {
                    validTasks.Add(validTask);
                    Console.WriteLine($"Validated task and added: {validTask.TaskName}");
                }
                else
                {
                    Console.WriteLine(errorMessage);
                }
            }
            return validTasks;
        }
    }
}
