using System.Diagnostics.CodeAnalysis;

namespace SelectDailyTasksApp.DataIO
{
    // только файловый ввод: никакой валидации и вывода в консоль
    public static class TaskFileReader
    {
        // false = вход отсутствует или не читается (это НЕ то же самое, что пустой файл)
        // true + пустой массив = валидный пустой датасет
        public static bool TryReadLines(string? filePath, [NotNullWhen(true)] out string[]? lines, out string errorMessage)
        {
            lines = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "Error: File path is missing.";
                return false;
            }

            if (!File.Exists(filePath))
            {
                errorMessage = $"Error: Could not find '{filePath}' file.";
                return false;
            }

            try
            {
                lines = File.ReadAllLines(filePath);
                return true;
            }
            catch (IOException ex)
            {
                errorMessage = $"Error: Could not read '{filePath}': {ex.Message}";
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Error: No access to '{filePath}': {ex.Message}";
                return false;
            }
        }
    }
}
