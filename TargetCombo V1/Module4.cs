using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class Module4
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled);
    private static readonly Regex LinePattern = new(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);
    private static readonly Regex PhoneNumberPattern = new(@"^\+?(\d{7,15}[\d\s\-().]*)$", RegexOptions.Compiled);

    public static void ExtractLinkBasedSpecificCredentials(ref int totalLinesLoaded, ref int totalLinesSaved, string mode, string linksFilePath)
    {
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();

        // Load the keywords from the links file
        string linksFileName = Path.GetFileNameWithoutExtension(linksFilePath);
        var keywords = new HashSet<string>(File.ReadAllLines(linksFilePath), StringComparer.OrdinalIgnoreCase);

        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string sourceDirectory = Path.Combine(exeDirectory, "source");
        if (!Directory.Exists(sourceDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Source directory not found.");
            Console.ResetColor();
            return;
        }

        // Set up output directories based on mode
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string outputDirectory = Path.Combine(exeDirectory, $"FUll_{linksFileName}_{timestamp}");

        // Segregated directories for email, user, and number passwords
        var emailDirectory = Path.Combine(outputDirectory, "email-password");
        var userDirectory = Path.Combine(outputDirectory, "user-password");
        var numberDirectory = Path.Combine(outputDirectory, "number-password");

        // Ensure all directories are created
        Directory.CreateDirectory(emailDirectory);
        Directory.CreateDirectory(userDirectory);
        Directory.CreateDirectory(numberDirectory);

        // Define output file names based on extraction type
        string emailFile = Path.Combine(emailDirectory, $"{mode}-email--password.txt");
        string userFile = Path.Combine(userDirectory, $"{mode}-user--password.txt");
        string numberFile = Path.Combine(numberDirectory, $"{mode}-number--password.txt");

        string[] files = Directory.GetFiles(sourceDirectory, "*.txt");

        foreach (var file in files)
        {
            try
            {
                // Use the correct StreamReader constructor
                using (var reader = new StreamReader(file, Encoding.UTF8)) // Default encoding
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        totalLinesLoaded++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var match = LinePattern.Match(line);
                        if (!match.Success) continue;

                        string url = match.Groups[1].Value.Trim();
                        string username = match.Groups[2].Value.Trim();
                        string password = match.Groups[3].Value.Trim();

                        // Check if URL contains any of the keywords from the links file
                        string matchedKeyword = FindMatchingKeyword(url, keywords);
                        if (matchedKeyword == null) continue;

                        // Apply mode-based filtering
                        if ((mode == "email" && EmailPattern.IsMatch(username)) ||
                            (mode == "number" && IsPhoneNumber(username)) ||
                            (mode == "user" && !EmailPattern.IsMatch(username) && !IsPhoneNumber(username)) ||
                            (mode == "all"))
                        {
                            // Sanitize keyword and save the matched credential
                            string sanitizedKeyword = SanitizeFileName(matchedKeyword);

                            // Segregate and save the credential based on the username type
                            if (EmailPattern.IsMatch(username))
                            {
                                SaveCredential(emailFile, $"{username}:{password}");
                            }
                            else if (IsPhoneNumber(username))
                            {
                                SaveCredential(numberFile, $"{username}:{password}");
                            }
                            else
                            {
                                SaveCredential(userFile, $"{username}:{password}");
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

    private static string FindMatchingKeyword(string url, HashSet<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (url.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return keyword;
        }
        return null;
    }

    private static bool IsPhoneNumber(string username)
    {
        var cleanedNumber = username.Replace("+91", "").Trim();
        return PhoneNumberPattern.IsMatch(cleanedNumber);
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

    private static string SanitizeFileName(string fileName)
    {
        // Replace invalid characters with underscores
        return Regex.Replace(fileName, @"[<>:""/\\|?*\x00-\x1F]", "_");
    }
}
