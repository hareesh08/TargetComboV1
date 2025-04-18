using System;
using System.IO;
using System.Text.RegularExpressions;
using TargetULPCommercial.security;
using System.Collections.Generic;

namespace TargetCombo_V1
{
    static class ExtractUrlByCountry
    {
        // Default country TLD mapping
        private static readonly Dictionary<string, List<string>> DefaultCountryTlds = new Dictionary<string, List<string>>
{
    { "IN", new List<string> { ".in" } },
    { "COM", new List<string> { ".com" } },
    { "AR", new List<string> { ".ar" } },
    { "AU", new List<string> { ".au" } },
    { "UK", new List<string> { ".co.uk", ".uk" } },
    { "CA", new List<string> { ".ca" } },
    { "FR", new List<string> { ".fr" } },
    { "DE", new List<string> { ".de" } },
    { "US", new List<string> { ".us" } },
    { "BR", new List<string> { ".br" } },
    { "IT", new List<string> { ".it" } },
    { "JP", new List<string> { ".jp" } },
    { "RU", new List<string> { ".ru" } },
    { "CN", new List<string> { ".cn" } },
    { "MX", new List<string> { ".mx" } },
    { "ZA", new List<string> { ".za" } },
    { "ES", new List<string> { ".es" } },
    { "PL", new List<string> { ".pl" } },
    { "KR", new List<string> { ".kr" } },
    { "SE", new List<string> { ".se" } },
    { "NL", new List<string> { ".nl" } },
    { "NO", new List<string> { ".no" } },
    { "DK", new List<string> { ".dk" } },
    { "BE", new List<string> { ".be" } },
    { "CH", new List<string> { ".ch" } },
    { "SG", new List<string> { ".sg" } },
    { "AE", new List<string> { ".ae" } },
    { "MY", new List<string> { ".my" } },
    { "PH", new List<string> { ".ph" } },
    { "TH", new List<string> { ".th" } },
    { "ID", new List<string> { ".id" } },
    { "HK", new List<string> { ".hk" } },
    { "TW", new List<string> { ".tw" } },
    { "IL", new List<string> { ".il" } },
    { "SA", new List<string> { ".sa" } },
    { "EG", new List<string> { ".eg" } },
    { "NG", new List<string> { ".ng" } },
    { "CO", new List<string> { ".co" } },
    { "PE", new List<string> { ".pe" } },
    { "CL", new List<string> { ".cl" } },
    { "VE", new List<string> { ".ve" } },
    { "PK", new List<string> { ".pk" } },
    { "BD", new List<string> { ".bd" } },
    { "LK", new List<string> { ".lk" } },
    { "KW", new List<string> { ".kw" } },
    { "GR", new List<string> { ".gr" } },
    { "AT", new List<string> { ".at" } },
    { "CZ", new List<string> { ".cz" } },
    { "HU", new List<string> { ".hu" } },
    { "RO", new List<string> { ".ro" } },
    { "SK", new List<string> { ".sk" } },
    { "BG", new List<string> { ".bg" } },
    { "FI", new List<string> { ".fi" } },
    { "EE", new List<string> { ".ee" } },
    { "LT", new List<string> { ".lt" } },
    { "LV", new List<string> { ".lv" } },
    { "IS", new List<string> { ".is" } },
    { "LU", new List<string> { ".lu" } },
    { "MT", new List<string> { ".mt" } },
    { "SI", new List<string> { ".si" } },
    { "HR", new List<string> { ".hr" } },
    { "RS", new List<string> { ".rs" } },
    { "UA", new List<string> { ".ua" } },
    { "BY", new List<string> { ".by" } },
    { "MD", new List<string> { ".md" } },
    { "AM", new List<string> { ".am" } },
    { "GE", new List<string> { ".ge" } },
    { "AZ", new List<string> { ".az" } },
    { "KG", new List<string> { ".kg" } },
    { "UZ", new List<string> { ".uz" } },
    { "MN", new List<string> { ".mn" } },
    { "NP", new List<string> { ".np" } },
    { "BT", new List<string> { ".bt" } },
    { "LA", new List<string> { ".la" } },
    { "KH", new List<string> { ".kh" } },
    { "MM", new List<string> { ".mm" } }
};


        public static void ProcessFiles(ref int totalLinesLoaded, ref int totalLinesSaved)
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
            string outputDirectory = Path.Combine(exeDirectory, $"UrlByCountry_{timestamp}");
            Directory.CreateDirectory(outputDirectory);

            // Ask user to choose default or custom TLDs
            var countryTlds = GetCountryTldsFromUser();

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

                            // Get the TLD from URL and find the country folder
                            string tld = GetTldFromUrl(url);

                            // Save only if TLD matches any of the selected TLDs
                            if (IsValidTld(tld, countryTlds))
                            {
                                SaveCredentials(username, password, tld, outputDirectory, ref totalLinesSaved);
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

        // Ask the user to select the default or custom TLDs
        private static Dictionary<string, List<string>> GetCountryTldsFromUser()
        {
            Dictionary<string, List<string>> countryTlds;
            IntegrityCheck.VerifyJwtHash();

            Console.WriteLine("Would you like to use the default TLDs or provide your own?");
            Console.WriteLine("1. Default TLDs");
            Console.WriteLine("2. Custom TLDs");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                countryTlds = new Dictionary<string, List<string>>(DefaultCountryTlds);
            }
            else
            {
                countryTlds = new Dictionary<string, List<string>>();
                Console.WriteLine("Enter custom TLDs (e.g., .in, .com, .au):");
                string input = Console.ReadLine();
                var tlds = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine("Enter corresponding country codes for each TLD (e.g., IN, COM, AU):");
                string countryCodes = Console.ReadLine();
                var countries = countryCodes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (tlds.Length != countries.Length)
                {
                    Console.WriteLine("Mismatch between number of TLDs and country codes. Using default TLDs instead.");
                    return new Dictionary<string, List<string>>(DefaultCountryTlds);
                }

                for (int i = 0; i < tlds.Length; i++)
                {
                    string tld = tlds[i].Trim();
                    string countryCode = countries[i].Trim().ToUpper();

                    if (!countryTlds.ContainsKey(countryCode))
                    {
                        countryTlds[countryCode] = new List<string>();
                    }

                    countryTlds[countryCode].Add(tld);
                }
            }

            return countryTlds;
        }

        // Extract the TLD from URL (e.g., ".com", ".in")
        private static string GetTldFromUrl(string url)
        {
            Regex regex = new Regex(@"\.([a-z]{2,3}(?:\.[a-z]{2})?)\b", RegexOptions.IgnoreCase);
            Match match = regex.Match(url);
            return match.Success ? match.Value.ToLower() : string.Empty;
        }

        // Check if the TLD is valid based on user input
        private static bool IsValidTld(string tld, Dictionary<string, List<string>> countryTlds)
        {
            foreach (var country in countryTlds)
            {
                if (country.Value.Contains(tld))
                {
                    return true;
                }
            }
            return false;
        }

        // Save credentials in the appropriate file based on the TLD
        private static void SaveCredentials(string username, string password, string tld, string outputDirectory, ref int totalLinesSaved)
        {
            string subfolderPath = Path.Combine(outputDirectory, tld.TrimStart('.'));
            Directory.CreateDirectory(subfolderPath); // Create subfolder for TLD if it doesn't exist

            string outputFile = Path.Combine(subfolderPath, $"{tld.TrimStart('.')}.txt");

            // Save credentials based on username type (email, user, or phone)
            bool isEmail = username.Contains("@");
            string content = $"{username}:{password}";

            if (isEmail)
            {
                outputFile = Path.Combine(subfolderPath, "emailpass.txt");
            }
            else if (Regex.IsMatch(username, @"^\+?(\d{7,15}[\d\s\-().]*)$"))
            {
                outputFile = Path.Combine(subfolderPath, "numberpass.txt");
            }
            else
            {
                outputFile = Path.Combine(subfolderPath, "userpass.txt");
            }

            WriteToFile(outputFile, content);
            totalLinesSaved++;
        }

        // Write the content to a file
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

        // Log error messages to a log file
        private static void LogError(string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
