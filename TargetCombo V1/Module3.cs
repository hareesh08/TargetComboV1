using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class Module3
{
    public static void ExtractSpecificCredentials(ref int totalLinesLoaded, ref int totalLinesSaved, string mode)
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();

        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var sourceDirectory = Path.Combine(exeDirectory, "source");

        if (!Directory.Exists(sourceDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Source directory not found.");
            Console.ResetColor();
            return;
        }

        // Create timestamped output directory
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputDirectory = Path.Combine(exeDirectory, $"Full_{mode}_{timestamp}");
        Directory.CreateDirectory(outputDirectory);

        // Output files
        var emailOutputFile = Path.Combine(outputDirectory, "Email-password.txt");
        var numberOutputFile = Path.Combine(outputDirectory, "Number-password.txt");
        var userOutputFile = Path.Combine(outputDirectory, "User-password.txt");

        // Unified output for single-mode (non-all)
        var singleModeOutputFile = Path.Combine(outputDirectory, $"{mode}-password.txt");

        var emailPattern = new Regex(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled);
        var linePattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);

        var files = Directory.GetFiles(sourceDirectory, "*.txt");

        foreach (var file in files)
        {
            try
            {
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        totalLinesLoaded++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var match = linePattern.Match(line);
                        if (!match.Success) continue;

                        var username = match.Groups[2].Value.Trim();
                        var password = match.Groups[3].Value.Trim();
                        var credential = $"{username}:{password}";

                        if (mode == "email" && emailPattern.IsMatch(username))
                        {
                            SaveCredential(singleModeOutputFile, credential);
                            totalLinesSaved++;
                        }
                        else if (mode == "number" && IsPhoneNumber(username))
                        {
                            SaveCredential(singleModeOutputFile, credential);
                            totalLinesSaved++;
                        }
                        else if (mode == "user" && !emailPattern.IsMatch(username) && !IsPhoneNumber(username))
                        {
                            SaveCredential(singleModeOutputFile, credential);
                            totalLinesSaved++;
                        }
                        else if (mode == "all")
                        {
                            if (emailPattern.IsMatch(username))
                            {
                                SaveCredential(emailOutputFile, credential);
                            }
                            else if (IsPhoneNumber(username))
                            {
                                SaveCredential(numberOutputFile, credential);
                            }
                            else
                            {
                                SaveCredential(userOutputFile, credential);
                            }

                            totalLinesSaved++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing file {file}: {ex.Message}");
            }
        }

        shadowCheck.Stop();
    }

    private static bool IsPhoneNumber(string username)
    {
        var cleanedNumber = username.Replace("+91", "").Trim();
        return Regex.IsMatch(cleanedNumber, @"^\+?(\d{7,15}[\d\s\-().]*)$");
    }

    private static void SaveCredential(string outputFile, string content)
    {
        try
        {
            File.AppendAllText(outputFile, $"{content}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            LogError($"Error writing to file {outputFile}: {ex.Message}");
        }
    }

    private static void LogError(string message)
    {
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}
