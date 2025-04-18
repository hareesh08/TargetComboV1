using System;
using TargetULPCommercial.security;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace TargetCombo_V1
{
    static class Module1
    {
        public static void ProcessFiles(string linksFilePath, ref int totalLinesLoaded, ref int totalLinesSaved)
        {
            string linksFileName = Path.GetFileNameWithoutExtension(linksFilePath);
            var keywords = new HashSet<string>(File.ReadAllLines(linksFilePath), StringComparer.OrdinalIgnoreCase);
            
            IntegrityCheck.VerifyJwtHash();
            var shadowCheck = new LicenseShadowCheck(120000);
            shadowCheck.Start();

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sourceDirectory = Path.Combine(exeDirectory, "source");
            if (!Directory.Exists(sourceDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Source directory not found.");
                Console.ResetColor();
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputDirectory = Path.Combine(exeDirectory, $"{linksFileName}_{timestamp}");
            string emailDirectory = Path.Combine(outputDirectory, "email-password");
            string userDirectory = Path.Combine(outputDirectory, "user-password");
            string numberDirectory = Path.Combine(outputDirectory, "number-password");

            Directory.CreateDirectory(emailDirectory);
            Directory.CreateDirectory(userDirectory);
            Directory.CreateDirectory(numberDirectory);

            Regex pattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);
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

                            var match = pattern.Match(line);
                            if (!match.Success) continue;

                            string url = match.Groups[1].Value.Trim();
                            string username = match.Groups[2].Value.Trim();
                            string password = match.Groups[3].Value.Trim();

                            string matchedKeyword = FindMatchingKeyword(url, keywords);
                            if (matchedKeyword == null) continue;

                            string sanitizedKeyword = SanitizeFileName(matchedKeyword);
                            SaveCredentials(sanitizedKeyword, username, password, emailDirectory, userDirectory, numberDirectory, ref totalLinesSaved);
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

        private static void SaveCredentials(string sanitizedKeyword, string username, string password, string emailDirectory, string userDirectory, string numberDirectory, ref int totalLinesSaved)
        {
            bool isEmail = username.Contains("@");
            string outputFile;

            string cleanedNumber = username.Replace("+91", "").Trim();
            if (Regex.IsMatch(cleanedNumber, @"^\+?(\d{7,15}[\d\s\-().]*)$"))
            {
                outputFile = Path.Combine(numberDirectory, $"{sanitizedKeyword}.txt");
                WriteToFile(outputFile, $"{cleanedNumber}:{password}");
                totalLinesSaved++;
            }
            else if (isEmail)
            {
                outputFile = Path.Combine(emailDirectory, $"{sanitizedKeyword}.txt");
                WriteToFile(outputFile, $"{username}:{password}");
                totalLinesSaved++;
            }
            else
            {
                outputFile = Path.Combine(userDirectory, $"{sanitizedKeyword}.txt");
                WriteToFile(outputFile, $"{username}:{password}");
                totalLinesSaved++;
            }
        }

        private static void WriteToFile(string outputFile, string content)
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
