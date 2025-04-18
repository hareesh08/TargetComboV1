namespace TargetCombo_V1;
using TargetCombo_V1.security;

internal static class RemoveDuplicates
{
    public static void RemoveDuplicateLines(ref int totalLinesSaved)
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
        var outputFilePath = Path.Combine(sourceDirectory, $"RemovedDup{totalLinesSaved}_{timestamp}.txt");

        // Create a HashSet to store unique lines (set automatically removes duplicates)
        HashSet<string> uniqueLines = new HashSet<string>();

        // Initialize counts
        var totalInputLines = 0;
        var totalDuplicateLines = 0;

        // Get all ULP files from the source directory (assuming .txt files are ULP files)
        string[] ulpFiles = Directory.GetFiles(sourceDirectory, "*.txt");

        // Iterate over each file to read and remove duplicates
        foreach (var file in ulpFiles)
            try
            {
                // Read the file line by line
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        totalInputLines++;

                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Try adding the line to the HashSet (duplicates will be ignored)
                        if (!uniqueLines.Add(line))
                            totalDuplicateLines++; // Increment duplicate count if line already exists
                        else
                            totalLinesSaved++; // Increment saved lines if line was unique
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing file {file}: {ex.Message}");
            }

        // Write the unique lines to the output file
        using (var writer = new StreamWriter(outputFilePath, false))
        {
            foreach (var line in uniqueLines) writer.WriteLine(line);
        }

        // Display the counts
        Console.WriteLine($"\nRemoved duplicates and saved to: {outputFilePath}");
        Console.WriteLine($"\nInput count (total lines read): {totalInputLines}");
        Console.WriteLine($"\nDuplicate count (lines ignored): {totalDuplicateLines}");
        Console.WriteLine($"\nAfter removal count (unique lines saved): {totalLinesSaved}");
        shadowCheck.Stop();
    }

    private static void LogError(string message)
    {
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}