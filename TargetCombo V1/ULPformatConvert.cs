// using System.Text;
// using System.Text.RegularExpressions;
//
// namespace TargetCombo_V1
// {
//     internal static class ULPformatConvert
//     {
//         private static readonly Regex pipeFormatRegex = new Regex(
//             @"^(https?://[^\s|]+)\|([^\s|]+)\|(.+)$",
//             RegexOptions.Compiled | RegexOptions.IgnoreCase);
//
//         public static void NormalizeToStandardFormat()
//         {
//             var sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source");
//             if (!Directory.Exists(sourceDirectory))
//             {
//                 Console.ForegroundColor = ConsoleColor.Red;
//                 Console.WriteLine("Source directory not found.");
//                 Console.ResetColor();
//                 return;
//             }
//
//             var files = Directory.EnumerateFiles(sourceDirectory, "*.txt");
//
//             foreach (var file in files)
//             {
//                 string tempFile = file + ".tmp";
//                 int validCount = 0;
//
//                 try
//                 {
//                     using var reader = new StreamReader(file, Encoding.UTF8);
//                     using var writer = new StreamWriter(tempFile, false, Encoding.UTF8);
//
//                     string? line;
//                     while ((line = reader.ReadLine()) != null)
//                     {
//                         var trimmed = line.Trim();
//                         if (string.IsNullOrWhiteSpace(trimmed))
//                             continue;
//
//                         if (pipeFormatRegex.IsMatch(trimmed) && IsValidUrl(trimmed.Split('|')[0]))
//                         {
//                             writer.WriteLine(trimmed);
//                             validCount++;
//                         }
//                         else
//                         {
//                             var converted = TryConvertToPipeFormat(trimmed);
//                             if (!string.IsNullOrEmpty(converted))
//                             {
//                                 writer.WriteLine(converted);
//                                 validCount++;
//                             }
//                             else
//                             {
//                                 LogConversionFailure(trimmed);
//                             }
//                         }
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     LogError($"[FILE ERROR] {Path.GetFileName(file)}: {ex.Message}");
//                     continue;
//                 }
//
//                 try
//                 {
//                     File.Delete(file);
//
//                     if (validCount > 0)
//                     {
//                         File.Move(tempFile, file);
//                         Console.WriteLine($"[✔ Converted] {Path.GetFileName(file)} - {validCount} valid lines.");
//                     }
//                     else
//                     {
//                         File.Delete(tempFile);
//                         Console.WriteLine($"[✘ Deleted] {Path.GetFileName(file)} - no valid lines.");
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     LogError($"[POST ERROR] {Path.GetFileName(file)}: {ex.Message}");
//                 }
//             }
//         }
//
//         private static string? TryConvertToPipeFormat(string line)
// {
//     try
//     {
//         line = line.Trim();
//
//         // Android format: android://<auth>@<package>[:/]?<user>:<pass>
//         var androidMatch = Regex.Match(line, @"^android:\/\/[^@]+@(?<pkg>[^\s:\/]+)[\/:]?(?<user>[^:]+):(?<pass>.+)$");
//         if (androidMatch.Success)
//         {
//             var pkg = androidMatch.Groups["pkg"].Value.Trim();
//             var user = androidMatch.Groups["user"].Value.Trim();
//             var pass = androidMatch.Groups["pass"].Value.Trim();
//             var url = $"https://{pkg}";
//
//             if (IsValidUrl(url))
//                 return $"{url}|{user}|{pass}";
//         }
//
//         // Generic protocol://host:user:pass
//         var protoHostUserPassMatch = Regex.Match(line, @"^(?<proto>\w+):\/\/(?<host>[^:\/]+)[\/:]?(?<user>[^:]+):(?<pass>.+)$");
//         if (protoHostUserPassMatch.Success)
//         {
//             var proto = protoHostUserPassMatch.Groups["proto"].Value.Trim();
//             var host = protoHostUserPassMatch.Groups["host"].Value.Trim();
//             var user = protoHostUserPassMatch.Groups["user"].Value.Trim();
//             var pass = protoHostUserPassMatch.Groups["pass"].Value.Trim();
//
//             var url = $"{proto}://{host}";
//             if (IsValidUrl(url))
//                 return $"{url}|{user}|{pass}";
//         }
//
//         // about:blank:user:pass
//         var aboutMatch = Regex.Match(line, @"^about:blank:(?<user>[^:]+):(?<pass>.+)$");
//         if (aboutMatch.Success)
//         {
//             var user = aboutMatch.Groups["user"].Value.Trim();
//             var pass = aboutMatch.Groups["pass"].Value.Trim();
//             return $"about:blank|{user}|{pass}";
//         }
//
//         // user:pass@host
//         var atSplit = line.Split('@');
//         if (atSplit.Length == 2)
//         {
//             var creds = atSplit[0].Split(':');
//             if (creds.Length == 2)
//             {
//                 var username = creds[0].Trim();
//                 var password = creds[1].Trim();
//                 var url = $"https://{atSplit[1].Trim()}";
//
//                 if (IsValidUrl(url))
//                     return $"{url}|{username}|{password}";
//             }
//         }
//
//         // host:user:pass
//         var colonSplit = line.Split(':');
//         if (colonSplit.Length == 3 && !line.StartsWith("http"))
//         {
//             var url = $"https://{colonSplit[0].Trim()}";
//             var username = colonSplit[1].Trim();
//             var password = colonSplit[2].Trim();
//
//             if (IsValidUrl(url))
//                 return $"{url}|{username}|{password}";
//         }
//
//         // host|user|pass
//         if (!line.StartsWith("http") && line.Split('|').Length == 3)
//         {
//             var parts = line.Split('|');
//             var url = $"https://{parts[0].Trim()}";
//
//             if (IsValidUrl(url))
//                 return $"{url}|{parts[1].Trim()}|{parts[2].Trim()}";
//         }
//     }
//     catch
//     {
//         // Ignore parsing failures
//     }
//
//     return null;
// }
//
//
//
//         private static bool IsValidUrl(string url)
//         {
//             return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
//                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
//         }
//
//         private static void LogError(string message)
//         {
//             var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
//             File.AppendAllText(logFile, $"{DateTime.Now} - {message}{Environment.NewLine}");
//         }
//
//         private static void LogConversionFailure(string line)
//         {
//             var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conversion_failures.txt");
//             File.AppendAllText(logFile, $"{line}{Environment.NewLine}");
//         }
//     }
// }

// using System.Collections.Concurrent;
// using System.Text;
// using System.Text.RegularExpressions;
//
// namespace TargetCombo_V1
// {
//     internal static class ULPformatConvert
//     {
//         private static readonly Regex pipeFormatRegex = new Regex(
//             @"^(https?|ftp|imap|smtp|oauth):\/\/[^\s|]+?\|[^\s|]+\|.+$",
//             RegexOptions.Compiled | RegexOptions.IgnoreCase);
//
//         private static readonly Regex comboRegex = new Regex(
//             @"^(?:(?<scheme>android|imap|smtp|ftp|oauth|about):\/\/)?" +
//             @"(?:(?<auth>[^@]+)@)?" +
//             @"(?<host>[^\s:\/]+)" +
//             @"(?:[\/:]?)" +
//             @"(?<user>[^:]+):" +
//             @"(?<pass>.+)$",
//             RegexOptions.Compiled | RegexOptions.IgnoreCase);
//
//         private static readonly ConcurrentQueue<string> ErrorLogQueue = new();
//         private static readonly ConcurrentQueue<string> FailureLogQueue = new();
//
//         public static void NormalizeToStandardFormat()
//         {
//             var sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source");
//             if (!Directory.Exists(sourceDirectory))
//             {
//                 Console.ForegroundColor = ConsoleColor.Red;
//                 Console.WriteLine("Source directory not found.");
//                 Console.ResetColor();
//                 return;
//             }
//
//             var files = Directory.EnumerateFiles(sourceDirectory, "*.txt").ToList();
//             if (files.Count == 0)
//             {
//                 Console.ForegroundColor = ConsoleColor.Yellow;
//                 Console.WriteLine("No .txt files found.");
//                 Console.ResetColor();
//                 return;
//             }
//
//             Parallel.ForEach(files, file =>
//             {
//                 string tempFile = file + ".tmp";
//                 int validCount = 0;
//                 int lineNumber = 0;
//
//                 try
//                 {
//                     using var reader = new StreamReader(file, Encoding.UTF8);
//                     using var writer = new StreamWriter(tempFile, false, Encoding.UTF8);
//
//                     string? line;
//                     while ((line = reader.ReadLine()) != null)
//                     {
//                         lineNumber++;
//                         var trimmed = line.Trim();
//                         if (string.IsNullOrWhiteSpace(trimmed))
//                             continue;
//
//                         if (pipeFormatRegex.IsMatch(trimmed))
//                         {
//                             writer.WriteLine(trimmed);
//                             validCount++;
//                         }
//                         else
//                         {
//                             var converted = TryConvertToPipeFormat(trimmed);
//                             if (!string.IsNullOrEmpty(converted))
//                             {
//                                 writer.WriteLine(converted);
//                                 validCount++;
//                             }
//                             else
//                             {
//                                 FailureLogQueue.Enqueue($"{Path.GetFileName(file)} [Line {lineNumber}]: {trimmed}");
//                             }
//                         }
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     ErrorLogQueue.Enqueue($"[FILE ERROR] {Path.GetFileName(file)}: {ex.Message}");
//                     return;
//                 }
//
//                 try
//                 {
//                     File.Delete(file);
//                     if (validCount > 0)
//                     {
//                         File.Move(tempFile, file);
//                         Console.WriteLine($"[✔ Converted] {Path.GetFileName(file)} - {validCount} valid lines.");
//                     }
//                     else
//                     {
//                         File.Delete(tempFile);
//                         Console.WriteLine($"[✘ Deleted] {Path.GetFileName(file)} - no valid lines.");
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     ErrorLogQueue.Enqueue($"[POST ERROR] {Path.GetFileName(file)}: {ex.Message}");
//                 }
//             });
//
//             WriteQueuedLogs();
//         }
//
//         private static string? TryConvertToPipeFormat(string line)
//         {
//             if (string.IsNullOrWhiteSpace(line) || line.Length < 10 || !line.Contains(":"))
//                 return null;
//
//             line = line.Trim();
//
//             var match = comboRegex.Match(line);
//             if (!match.Success)
//                 return null;
//
//             string scheme = match.Groups["scheme"].Success ? match.Groups["scheme"].Value.ToLower() : "https";
//             string host = match.Groups["host"].Value.Trim();
//             string user = match.Groups["user"].Value.Trim();
//             string pass = match.Groups["pass"].Value.Trim();
//
//             string url = scheme == "about" ? "about:blank" : $"{scheme}://{host}";
//             if (!IsValidUrl(url)) return null;
//
//             return $"{url}|{user}|{pass}";
//         }
//
//         private static bool IsValidUrl(string url)
//         {
//             return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
//                    (uriResult.Scheme == Uri.UriSchemeHttp ||
//                     uriResult.Scheme == Uri.UriSchemeHttps ||
//                     uriResult.Scheme == Uri.UriSchemeFtp ||
//                     uriResult.Scheme == "imap" ||
//                     uriResult.Scheme == "smtp" ||
//                     uriResult.Scheme == "oauth" ||
//                     url == "about:blank");
//         }
//
//         private static void WriteQueuedLogs()
//         {
//             var basePath = AppDomain.CurrentDomain.BaseDirectory;
//
//             if (!ErrorLogQueue.IsEmpty)
//             {
//                 File.AppendAllLines(Path.Combine(basePath, "error_log.txt"), ErrorLogQueue);
//             }
//
//             if (!FailureLogQueue.IsEmpty)
//             {
//                 File.AppendAllLines(Path.Combine(basePath, "conversion_failures.txt"), FailureLogQueue);
//             }
//         }
//     }
// }

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TargetCombo_V1
{
    internal static class ULPformatConvert
    {
        private static readonly Regex pipeFormatRegex = new Regex(
            @"^(https?|ftp|imap|smtp|oauth|about):\/\/[^\s|]+\|[^\s|]+\|[^\s|]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex comboRegex = new Regex(
            @"^(?:(?<scheme>android|imap|smtp|ftp|oauth|about):\/\/)?" +
            @"(?:(?<auth>[^@]+)@)?" +
            @"(?<host>[^\s:\/]+)" +
            @"(?:[\/:])" +
            @"(?<user>[^:]+):" +
            @"(?<pass>[^:]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex hostRegex = new Regex(
            @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^(\d{1,3}\.){3}\d{1,3}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly ConcurrentQueue<string> ErrorLogQueue = new();
        private static readonly ConcurrentQueue<string> FailureLogQueue = new();
        private static readonly object ErrorLogLock = new();
        private static readonly object FailureLogLock = new();

        public static void NormalizeToStandardFormat()
        {
            var sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "source");
            if (!Directory.Exists(sourceDirectory))
            {
                Directory.CreateDirectory(sourceDirectory);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Created source directory. No files to process.");
                Console.ResetColor();
                return;
            }

            var files = Directory.EnumerateFiles(sourceDirectory, "*.txt").ToList();
            if (files.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No .txt files found in source directory.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Processing {files.Count} files...");

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
            {
                string tempFile = file + ".tmp";
                int validCount = 0;
                int lineNumber = 0;
                var validLines = new List<string>();

                try
                {
                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        lineNumber++;
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;

                        if (pipeFormatRegex.IsMatch(trimmed))
                        {
                            validLines.Add(trimmed);
                            validCount++;
                        }
                        else
                        {
                            var converted = TryConvertToPipeFormat(trimmed);
                            if (!string.IsNullOrEmpty(converted))
                            {
                                validLines.Add(converted);
                                validCount++;
                            }
                            else
                            {
                                FailureLogQueue.Enqueue($"{Path.GetFileName(file)} [Line {lineNumber}]: {trimmed}");
                            }
                        }
                    }

                    if (validCount > 0)
                    {
                        File.WriteAllLines(tempFile, validLines, Encoding.UTF8);
                        File.Delete(file);
                        File.Move(tempFile, file);
                        Console.WriteLine($"[✔ Converted] {Path.GetFileName(file)} - {validCount} valid lines.");
                    }
                    else
                    {
                        Console.WriteLine($"[✘ Skipped] {Path.GetFileName(file)} - no valid lines.");
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogQueue.Enqueue($"[FILE ERROR] {Path.GetFileName(file)}: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempFile) && validCount == 0)
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogQueue.Enqueue($"[CLEANUP ERROR] {Path.GetFileName(tempFile)}: {ex.Message}");
                        }
                    }
                }
            });

            WriteQueuedLogs();
            Console.WriteLine("Processing completed.");
        }

        private static string? TryConvertToPipeFormat(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 10 || !line.Contains(":"))
                return null;

            line = line.Trim();

            var match = comboRegex.Match(line);
            if (!match.Success)
                return null;

            string scheme = match.Groups["scheme"].Success ? match.Groups["scheme"].Value.ToLower() : "https";
            string host = match.Groups["host"].Value.Trim();
            string user = match.Groups["user"].Value.Trim();
            string pass = match.Groups["pass"].Value.Trim();

            if (!hostRegex.IsMatch(host) && scheme != "about")
                return null;

            string url = scheme == "about" ? "about:blank" : $"{scheme}://{host}";
            if (!IsValidUrl(url))
                return null;

            return $"{url}|{user}|{pass}";
        }

        private static bool IsValidUrl(string url)
        {
            if (url == "about:blank")
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp ||
                    uriResult.Scheme == Uri.UriSchemeHttps ||
                    uriResult.Scheme == Uri.UriSchemeFtp ||
                    uriResult.Scheme == "imap" ||
                    uriResult.Scheme == "smtp" ||
                    uriResult.Scheme == "oauth");
        }

        private static void WriteQueuedLogs()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            lock (ErrorLogLock)
            {
                if (!ErrorLogQueue.IsEmpty)
                {
                    var errorLogPath = Path.Combine(basePath, "error_log.txt");
                    File.AppendAllLines(errorLogPath, ErrorLogQueue);
                    ErrorLogQueue.Clear();
                }
            }

            lock (FailureLogLock)
            {
                if (!FailureLogQueue.IsEmpty)
                {
                    var engrossmentFailuresPath = Path.Combine(basePath, "conversion_failures.txt");
                    File.AppendAllLines(engrossmentFailuresPath, FailureLogQueue);
                    FailureLogQueue.Clear();
                }
            }
        }

        private static void ClearQueue<T>(ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _)) { }
        }
    }
}