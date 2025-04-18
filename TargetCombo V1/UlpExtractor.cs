namespace TargetCombo_V1;
using TargetCombo_V1.security;

internal static class UlpExtractor
{
    public static void ExtractUlpData(ref int totalLinesSaved, string mainDirectory)
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();
        
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Create the "Extracted Ulp" folder if it doesn't exist
        var extractedDirectory = Path.Combine(exeDirectory, "Extracted ULP");
        if (!Directory.Exists(extractedDirectory)) Directory.CreateDirectory(extractedDirectory);

        // First pass: Get all .txt files and count total lines to be saved
        var allTxtFiles = GetAllTextFiles(mainDirectory);

        foreach (var file in allTxtFiles)
            try
            {
                string url = null, user = null, pass = null;

                // Count the lines that will be saved in this file
                foreach (var line in File.ReadLines(file))
                {
                    // Extract URL, USER, PASS
                    if (line.StartsWith("URL:", StringComparison.OrdinalIgnoreCase))
                        url = line.Substring(4).Trim();
                    else if (line.StartsWith("USER:", StringComparison.OrdinalIgnoreCase))
                        user = line.Substring(5).Trim();
                    else if (line.StartsWith("PASS:", StringComparison.OrdinalIgnoreCase))
                        pass = line.Substring(5).Trim();

                    // When we have all three components, increment the line count
                    if (url != null && user != null && pass != null)
                    {
                        totalLinesSaved++;
                        // Reset for the next entry
                        url = null;
                        user = null;
                        pass = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}");
            }

        // Now that we know totalLinesSaved, generate output file with that count
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputFilePath = Path.Combine(extractedDirectory, $"{totalLinesSaved}_{timestamp}.txt");

        try
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                // Second pass: Process the files again and save the lines
                foreach (var file in allTxtFiles)
                    try
                    {
                        string url = null, user = null, pass = null;

                        foreach (var line in File.ReadLines(file))
                        {
                            // Extract URL, USER, PASS from the lines
                            if (line.StartsWith("URL:", StringComparison.OrdinalIgnoreCase))
                                url = line.Substring(4).Trim();
                            else if (line.StartsWith("USER:", StringComparison.OrdinalIgnoreCase))
                                user = line.Substring(5).Trim();
                            else if (line.StartsWith("PASS:", StringComparison.OrdinalIgnoreCase))
                                pass = line.Substring(5).Trim();

                            // When all 3 components (URL, USER, PASS) are found, write the line and reset
                            if (url != null && user != null && pass != null)
                            {
                                var formattedLine = $"{url}:{user}:{pass}";
                                writer.WriteLine(formattedLine);
                                // Reset for the next entry
                                url = null;
                                user = null;
                                pass = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
            }

            // Print the total lines saved after the extraction is complete
            Console.WriteLine($"Extraction complete. {totalLinesSaved} lines saved in file: {outputFilePath}");
            shadowCheck.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file {outputFilePath}: {ex.Message}");
        }
    }

    // Recursive method to get all .txt files in subdirectories
    private static string[] GetAllTextFiles(string directory)
    {
        try
        {
            var allTxtFiles = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
            return allTxtFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing directory {directory}: {ex.Message}");
            return new string[0]; // Return an empty array in case of error
        }
    }
}