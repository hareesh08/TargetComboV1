using System.Text.RegularExpressions;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal static class Module2
{
    public static void ExtractAllCredentials(ref int totalLinesLoaded, ref int totalLinesSaved)
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
        var outputDirectory = Path.Combine(exeDirectory, $"Full_{timestamp}");
        var emailFile = Path.Combine(outputDirectory, "email-pass.txt");
        var userFile = Path.Combine(outputDirectory, "user-pass.txt");
        var numberFile = Path.Combine(outputDirectory, "number-pass.txt");

        Directory.CreateDirectory(outputDirectory);

        var pattern = new Regex(@"^(https?://[^:]+):([^:]+):(.+)$", RegexOptions.Compiled);
        var files = Directory.GetFiles(sourceDirectory, "*.txt");

        var emailBuffer = new List<string>();
        var userBuffer = new List<string>();
        var numberBuffer = new List<string>();

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

                        var match = pattern.Match(line);
                        if (!match.Success) continue;

                        var username = match.Groups[2].Value.Trim();
                        var password = match.Groups[3].Value.Trim();
                        var credential = $"{username}:{password}";

                        if (IsEmail(username))
                            emailBuffer.Add(credential);
                        else if (IsPhoneNumber(username))
                            numberBuffer.Add(credential);
                        else
                            userBuffer.Add(credential);
                        totalLinesSaved++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing file {file}: {ex.Message}");
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
        var cleanedNumber = username.Replace("+91", "").Trim();
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
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}