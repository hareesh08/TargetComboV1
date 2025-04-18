using System.Text;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class Module6
{
    // Ensure that the source directory exists before proceeding
    public static void EnsureSourceDirectoryExists()
    {
        // Default directory path from executable location
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var sourceDirectory = Path.Combine(exeDirectory, "source");

        if (!Directory.Exists(sourceDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: The 'source' directory was not found.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.ResetColor();
    }

    // Process all files in the source directory and save them back to the source directory
    public static void ProcessFiles()
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();

        // Ensure source directory exists
        EnsureSourceDirectoryExists();

        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var sourceDirectory = Path.Combine(exeDirectory, "source");

        // If source directory doesn't exist, exit the method
        if (!Directory.Exists(sourceDirectory)) return;

        var wordToAdd = "https://";
        string[] files = Directory.GetFiles(sourceDirectory, "*.txt");

        // Process each file
        foreach (var file in files)
            try
            {
                // Add word to each line in the file and save the modified content to the same file
                AddWordToFile(file, wordToAdd);
                // After successful modification, delete the original file (optional)
                // File.Delete(file);  // Uncomment this line if you want to delete the original file after modifying
            }
            catch (Exception ex)
            {
                LogError($"Error processing file {file}: {ex.Message}"); // Log any errors
            }

        shadowCheck.Stop();
    }

    // Method to add a word to each line in the input file and overwrite the file
    // Method to add a word to each line in the input file and overwrite the file
    private static void AddWordToFile(string inputFile, string wordToAdd)
    {
        try
        {
            // Read all lines from the input file
            string[] lines = File.ReadAllLines(inputFile, Encoding.UTF8);

            // Modify each line by adding the specified word
            for (var i = 0; i < lines.Length; i++)
                if (!string.IsNullOrWhiteSpace(lines[i])) // Skip empty lines
                    // Check if the line already contains "https://", if so, skip it
                    if (!lines[i].StartsWith(wordToAdd))
                        lines[i] = wordToAdd + lines[i].Trim(); // Add word to the line

            // Write the modified lines back to the file, overwriting the original file
            File.WriteAllLines(inputFile, lines, Encoding.UTF8);
            Console.WriteLine($"Successfully added '{wordToAdd}' to each line in {inputFile}.");
        }
        catch (Exception ex)
        {
            LogError($"Error modifying file {inputFile}: {ex.Message}"); // Log any errors
        }
    }


    // Method to log errors
    private static void LogError(string message)
    {
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}