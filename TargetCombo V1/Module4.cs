using System;
using System.IO;
using System.Text.RegularExpressions;
using TargetULPCommercial.security;
using System.Collections.Generic;

namespace TargetCombo_V1
{
    static class Module4
    {
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
            string outputDirectory = Path.Combine(exeDirectory, $"{linksFileName}_Full_{mode}_{timestamp}");
            Directory.CreateDirectory(outputDirectory);

            // Define output file names based on extraction type
            string outputFile = Path.Combine(outputDirectory, $"{mode}-password.txt");

            // Define patterns for each type
            Regex emailPattern = new Regex(@"^[^@]+@[^@]+\.[^@]+$");
            Regex linePattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);

            string[] files = Directory.GetFiles(sourceDirectory, "*.txt");

            foreach (var file in files)
            {
                try
                {
                    using (var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            totalLinesLoaded++;
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var match = linePattern.Match(line);
                            if (!match.Success) continue;

                            string url = match.Groups[1].Value.Trim();
                            string username = match.Groups[2].Value.Trim();
                            string password = match.Groups[3].Value.Trim();

                            // Check if URL contains any of the keywords from the links file
                            string matchedKeyword = FindMatchingKeyword(url, keywords);
                            if (matchedKeyword == null) continue;

                            // Apply mode-based filtering
                            if ((mode == "email" && emailPattern.IsMatch(username)) ||
                                (mode == "number" && IsPhoneNumber(username)) ||
                                (mode == "user" && !emailPattern.IsMatch(username) && !IsPhoneNumber(username)) ||
                                (mode == "all"))
                            {
                                // Sanitize keyword and save the matched credential
                                string sanitizedKeyword = SanitizeFileName(matchedKeyword);
                                SaveCredential(outputFile, $"{username}:{password}");
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
                {
                    return keyword;
                }
            }
            return null;
        }

        private static bool IsPhoneNumber(string username)
        {
            string cleanedNumber = username.Replace("+91", "").Trim();
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
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        private static string SanitizeFileName(string fileName)
        {
            return Regex.Replace(fileName, @"[<>:""/\\|?*]", "_");
        }
    }
}
