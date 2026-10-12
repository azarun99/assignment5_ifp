using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SelectDailyTasksApp.Models;

namespace SelectDailyTasksApp.Validation
{
    public static class TaskValidator
    {
        public const int ExpectedFieldCount = 4;
        public const int MinUrgency = 1;
        public const int MaxUrgency = 5;

        // без AllowThousands/Exponent/Currency: "1,000" или "$5" не должны проходить как число
        private const NumberStyles HoursStyle =
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        public static bool TryParseAndValidate(RawTaskDto? raw, [NotNullWhen(true)] out TaskItem? validTask, out string errorMessage)
        {
            validTask = null;
            errorMessage = string.Empty;

            if (raw is null)
            {
                errorMessage = "Validation error: Task record is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(raw.TaskName))
            {
                errorMessage = "Validation error: Task name is missing or empty.";
                return false;
            }
            // без InvariantCulture парс 3.0 не работает на русской системе (там разделитель запятая),
            // поэтому всегда парсим с InvariantCulture: точка - разделитель, запятая - ошибка
            // double.IsFinite отсекает NaN и Infinity, которые TryParse тоже умеет разбирать
            if (!double.TryParse(raw.EstimatedHours, HoursStyle, CultureInfo.InvariantCulture, out double hours)
                || !double.IsFinite(hours) || hours <= 0)
            {
                errorMessage = $"Validation error: Invalid estimated hours '{raw.EstimatedHours}'. Must be a number greater than 0.";
                return false;
            }

            if (!int.TryParse(raw.Urgency, NumberStyles.Integer, CultureInfo.InvariantCulture, out int urgency)
                || urgency < MinUrgency || urgency > MaxUrgency)
            {
                errorMessage = $"Validation error: Invalid urgency '{raw.Urgency}'. Must be an integer between {MinUrgency} and {MaxUrgency}.";
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

        // одна строка "name,hours,urgency,isCompleted" -> TaskItem; ровно 4 поля
        public static bool TryParseLine(string? line, [NotNullWhen(true)] out TaskItem? validTask, out string errorMessage)
        {
            validTask = null;
            errorMessage = string.Empty;

            if (line is null)
            {
                errorMessage = "Validation error: Line is missing.";
                return false;
            }

            string[] words = line.Split(',');
            if (words.Length != ExpectedFieldCount)
            {
                errorMessage = $"Validation error: Expected {ExpectedFieldCount} comma-separated fields but found {words.Length}.";
                return false;
            }

            var raw = new RawTaskDto(words[0], words[1], words[2], words[3]);
            return TryParseAndValidate(raw, out validTask, out errorMessage);
        }

        // чистая функция: список строк -> валидные задачи + ошибки с номерами строк (без Console и файлов)
        // пустой список строк - это валидный пустой датасет, null - отсутствующий вход (исключение)
        // полностью пустые строки пропускаются, а не считаются данными
        public static TaskLoadResult ParseLines(IEnumerable<string?> lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            var tasks = ImmutableArray.CreateBuilder<TaskItem>();
            var errors = ImmutableArray.CreateBuilder<string>();
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                if (line is not null && string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (TryParseLine(line, out TaskItem? task, out string errorMessage))
                {
                    tasks.Add(task);
                }
                else
                {
                    errors.Add($"Line {lineNumber}: {errorMessage}");
                }
            }

            return new TaskLoadResult(tasks.ToImmutable(), errors.ToImmutable());
        }
    }
}
