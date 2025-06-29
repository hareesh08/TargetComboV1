using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1
{
    internal static class CpanelExtractor
    {
        public static void ExtractCpanelCredentials(ref int totalLinesLoaded, ref int totalLinesSaved)
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

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var outputDirectory = Path.Combine(exeDirectory, $"Cpanel_Extract_{timestamp}");
            Directory.CreateDirectory(outputDirectory);
            var cpanelFile = Path.Combine(outputDirectory, $"cpanel-credentials_{timestamp}.txt");

            // Patterns to match possible cPanel credentials
            var patterns = new List<Regex>
            {
                new Regex(@"^(https?://[^:]+:\d+(?:/[^:]+)*):([^:]+):(.+)$", RegexOptions.Compiled),  // URL with path
                new Regex(@"^([^:]+):(\d+):([^:]+):(.+)$", RegexOptions.Compiled),                   // domain:port:user:pass
                new Regex(@"^([^:]+:\d+(?:/[^:]+)*):([^:]+):(.+)$", RegexOptions.Compiled)           // domain/port/path:user:pass
            };

            var cpanelKeywords = new[] { "cpanel", "cpanelserver", "cpanel-login" };
            var cpanelBuffer = new List<string>();
            var files = Directory.GetFiles(sourceDirectory, "*.txt");

            foreach (var file in files)
            {
                try
                {
                    using var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read,
                        FileShare.Read, 4096, FileOptions.SequentialScan));

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        totalLinesLoaded++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        Match match = null;
                        foreach (var pattern in patterns)
                        {
                            match = pattern.Match(line);
                            if (match.Success) break;
                        }

                        if (match == null || !match.Success) continue;

                        string domainPart = match.Groups[1].Value.Trim();
                        string username = match.Groups[2].Value.Trim();
                        string password = match.Groups[3].Value.Trim();

                        string lowerDomain = domainPart.ToLower();

                        // Check for keyword presence
                        if (cpanelKeywords.Any(keyword => lowerDomain.Contains(keyword)))
                        {
                            if (!domainPart.StartsWith("http"))
                                domainPart = "http://" + domainPart;

                            var credential = $"{domainPart}:{username}:{password}";
                            cpanelBuffer.Add(credential);
                            totalLinesSaved++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing file {file}: {ex.Message}");
                }
            }

            WriteBufferedContent(cpanelFile, cpanelBuffer);
        }

        private static void WriteBufferedContent(string outputFile, List<string> buffer)
        {
            if (buffer.Count > 0)
            {
                try
                {
                    File.AppendAllLines(outputFile, buffer);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{buffer.Count} cPanel credentials saved to {Path.GetFileName(outputFile)}");
                    Console.ResetColor();
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
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
