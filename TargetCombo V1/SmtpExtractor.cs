using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1
{
    internal static class SmtpExtractor
    {
        public static void ExtractSmtpCredentials(ref int totalLinesLoaded, ref int totalLinesSaved)
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

            // Define default SMTP ports
            var defaultPorts = new HashSet<int> { 2083, 2082, 2087, 2086, 2095, 2096 };
            var customPorts = GetCustomPorts();
            var allPorts = defaultPorts.Concat(customPorts).ToHashSet();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var outputDirectory = Path.Combine(exeDirectory, $"Smtp_Extract_{timestamp}");
            Directory.CreateDirectory(outputDirectory);
            var smtpFile = Path.Combine(outputDirectory, $"smtp-credentials_{timestamp}.txt");

            // Define multiple regex patterns to support varied input formats
            var patterns = new List<Regex>
            {
                new Regex(@"^(https?://[^:]+:\d+(?:/[^:]+)*):([^:]+):(.+)$", RegexOptions.Compiled),  // Full URL with path
                new Regex(@"^([^:]+):(\d+):([^:]+):(.+)$", RegexOptions.Compiled),                   // domain:port:user:pass
                new Regex(@"^([^:]+:\d+(?:/[^:]+)*):([^:]+):(.+)$", RegexOptions.Compiled)           // domain/port/path:user:pass
            };

            var smtpBuffer = new List<string>();
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

                        // Extract port number
                        var portMatch = Regex.Match(domainPart, @":(\d+)");
                        if (!portMatch.Success || !int.TryParse(portMatch.Groups[1].Value, out int port)) continue;

                        if (allPorts.Contains(port))
                        {
                            if (!domainPart.StartsWith("http"))
                                domainPart = "http://" + domainPart;

                            var credential = $"{domainPart}:{username}:{password}";
                            smtpBuffer.Add(credential);
                            totalLinesSaved++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing file {file}: {ex.Message}");
                }
            }

            WriteBufferedContent(smtpFile, smtpBuffer);
        }

        private static List<int> GetCustomPorts()
        {
            Console.WriteLine("Enter custom SMTP ports (comma-separated), or press Enter to skip:");
            var input = Console.ReadLine();

            var customPorts = new List<int>();

            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    customPorts = input.Split(',')
                        .Select(port => int.Parse(port.Trim()))
                        .Where(port => port > 0)
                        .ToList();
                }
                catch (FormatException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid port format. Skipping custom ports.");
                    Console.ResetColor();
                }
            }

            return customPorts;
        }

        private static void WriteBufferedContent(string outputFile, List<string> buffer)
        {
            if (buffer.Count > 0)
            {
                try
                {
                    File.AppendAllLines(outputFile, buffer);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{buffer.Count} credentials saved to {Path.GetFileName(outputFile)}");
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
