using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class Module5
{
    public static void ExtractLinkBasedSpecificCredentials(ref int totalLinesLoaded, ref int totalLinesSaved,
        string mode, string linksFilePath)
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();

        // Load the keywords from the links file
        var linksFileName = Path.GetFileNameWithoutExtension(linksFilePath);
        var keywords = new HashSet<string>(File.ReadAllLines(linksFilePath), StringComparer.OrdinalIgnoreCase);

        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var sourceDirectory = Path.Combine(exeDirectory, "source");
        if (!Directory.Exists(sourceDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Source directory not found.");
            Console.ResetColor();
            return;
        }

        // Set up output directories based on mode
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputDirectory = Path.Combine(exeDirectory, $"{linksFileName}_Invidual_{mode}_{timestamp}");
        Directory.CreateDirectory(outputDirectory);

        // Create subdirectories for email, user, and number passwords when mode is "all"
        var emailDir = Path.Combine(outputDirectory, "Email-password");
        var userDir = Path.Combine(outputDirectory, "User-password");
        var numberDir = Path.Combine(outputDirectory, "Number-password");

        if (mode == "all")
        {
            Directory.CreateDirectory(emailDir);
            Directory.CreateDirectory(userDir);
            Directory.CreateDirectory(numberDir);
        }

        // Define patterns for each type
        var emailPattern = new Regex(@"^[^@]+@[^@]+\.[^@]+$");
        var linePattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);

        var files = Directory.GetFiles(sourceDirectory, "*.txt");

        foreach (var file in files)
            try
            {
                using (var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read,
                           FileShare.Read, 4096, FileOptions.SequentialScan)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        totalLinesLoaded++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var match = linePattern.Match(line);
                        if (!match.Success) continue;

                        var url = match.Groups[1].Value.Trim();
                        var username = match.Groups[2].Value.Trim();
                        var password = match.Groups[3].Value.Trim();

                        // Check if URL contains any of the keywords from the links file
                        var matchedKeyword = FindMatchingKeyword(url, keywords);
                        if (matchedKeyword == null) continue;

                        // Apply mode-based filtering
                        if ((mode == "email" && emailPattern.IsMatch(username)) ||
                            (mode == "number" && IsPhoneNumber(username)) ||
                            (mode == "user" && !emailPattern.IsMatch(username) && !IsPhoneNumber(username)) ||
                            mode == "all")
                        {
                            // Sanitize the keyword and save the matched credential
                            var sanitizedKeyword = SanitizeFileName(matchedKeyword);

                            // For "all" mode, save to appropriate subfolder (e_p, u_p, or n_p)
                            if (mode == "email" && emailPattern.IsMatch(username))
                            {
                                var outputFile = Path.Combine(emailDir, $"{sanitizedKeyword}.txt");
                                SaveCredential(outputFile, $"{username}:{password}");
                                totalLinesSaved++;
                            }
                            else if (mode == "number" && IsPhoneNumber(username))
                            {
                                var outputFile = Path.Combine(numberDir, $"{sanitizedKeyword}.txt");
                                SaveCredential(outputFile, $"{username}:{password}");
                                totalLinesSaved++;
                            }
                            else if (mode == "user" && !emailPattern.IsMatch(username) && !IsPhoneNumber(username))
                            {
                                var outputFile = Path.Combine(userDir, $"{sanitizedKeyword}.txt");
                                SaveCredential(outputFile, $"{username}:{password}");
                                totalLinesSaved++;
                            }
                            else if (mode == "all")
                            {
                                // If mode is "all", split into different types based on username
                                if (emailPattern.IsMatch(username))
                                {
                                    var outputFile = Path.Combine(emailDir, $"{sanitizedKeyword}.txt");
                                    SaveCredential(outputFile, $"{username}:{password}");
                                }
                                else if (IsPhoneNumber(username))
                                {
                                    var outputFile = Path.Combine(numberDir, $"{sanitizedKeyword}.txt");
                                    SaveCredential(outputFile, $"{username}:{password}");
                                }
                                else
                                {
                                    var outputFile = Path.Combine(userDir, $"{sanitizedKeyword}.txt");
                                    SaveCredential(outputFile, $"{username}:{password}");
                                }

                                totalLinesSaved++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing file {file}: {ex.Message}");
            }

        shadowCheck.Stop();
    }

    private static string FindMatchingKeyword(string url, HashSet<string> keywords)
    {
        foreach (var keyword in keywords)
            if (url.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return keyword;

        return null;
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
            // Ensure the directory exists before appending to the file
            var directory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

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

    private static string SanitizeFileName(string fileName)
    {
        return Regex.Replace(fileName, @"[<>:""/\\|?*]", "_");
    }
}