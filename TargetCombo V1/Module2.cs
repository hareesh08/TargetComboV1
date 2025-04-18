using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TargetULPCommercial.security;

namespace TargetCombo_V1
{
    static class Module2
    {
        public static void ExtractAllCredentials(ref int totalLinesLoaded, ref int totalLinesSaved)
        {
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
            string outputDirectory = Path.Combine(exeDirectory, $"Full_{timestamp}");
            string emailFile = Path.Combine(outputDirectory, "email-pass.txt");
            string userFile = Path.Combine(outputDirectory, "user-pass.txt");
            string numberFile = Path.Combine(outputDirectory, "number-pass.txt");

            Directory.CreateDirectory(outputDirectory);

            Regex pattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);
            string[] files = Directory.GetFiles(sourceDirectory, "*.txt");

            var emailBuffer = new List<string>();
            var userBuffer = new List<string>();
            var numberBuffer = new List<string>();

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

                            string username = match.Groups[2].Value.Trim();
                            string password = match.Groups[3].Value.Trim();
                            string credential = $"{username}:{password}";

                            if (IsEmail(username))
                            {
                                emailBuffer.Add(credential);
                            }
                            else if (IsPhoneNumber(username))
                            {
                                numberBuffer.Add(credential);
                            }
                            else
                            {
                                userBuffer.Add(credential);
                            }
                            totalLinesSaved++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing file {file}: {ex.Message}");
                }
            }

            // Write buffered content to files
            WriteBufferedContent(emailFile, emailBuffer);
            WriteBufferedContent(userFile, userBuffer);
            WriteBufferedContent(numberFile, numberBuffer);
            shadowCheck.Stop();
        }

        private static bool IsEmail(string username)
        {
            return username.Contains("@");
        }

        private static bool IsPhoneNumber(string username)
        {
            string cleanedNumber = username.Replace("+91", "").Trim();
            return Regex.IsMatch(cleanedNumber, @"^\+?(\d{7,15}[\d\s\-().]*)$");
        }

        private static void WriteBufferedContent(string outputFile, List<string> buffer)
        {
            if (buffer.Count > 0)
            {
                try
                {
                    File.AppendAllLines(outputFile, buffer);
                }
                catch (Exception ex)
                {
                    LogError($"Error writing to file {outputFile}: {ex.Message}");
                }
                buffer.Clear();
            }
        }

        private static void LogError(string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
