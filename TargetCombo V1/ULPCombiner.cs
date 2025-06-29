using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class ULPCombiner
{
    public static void CombineUlpFiles(ref int totalLinesSaved)
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();
        // Get the executable directory
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Define the source directory as a subfolder of the executable directory
        var sourceDirectory = Path.Combine(exeDirectory, "source");

        // Check if the source directory exists
        if (!Directory.Exists(sourceDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Source directory not found.");
            Console.ResetColor();
            return;
        }

        // Get the current timestamp for the output filename
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var combinedFilePath = Path.Combine(sourceDirectory, $"combined_{totalLinesSaved}_{timestamp}.txt");

        // Get all ULP files from the source directory (assuming .txt files are ULP files)
        var ulpFiles = Directory.GetFiles(sourceDirectory, "*.txt");

        using (var writer = new StreamWriter(combinedFilePath, false))
        {
            foreach (var file in ulpFiles)
                try
                {
                    // Read the ULP file and append its content to the combined file
                    var fileContent = File.ReadAllText(file);
                    writer.Write(fileContent);
                    totalLinesSaved += fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Length;
                }
                catch (Exception ex)
                {
                    LogError($"Error processing file {file}: {ex.Message}");
                }
        }

        Console.WriteLine($"Combined output saved to: {combinedFilePath}");
        shadowCheck.Stop();
    }

    private static void LogError(string message)
    {
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}