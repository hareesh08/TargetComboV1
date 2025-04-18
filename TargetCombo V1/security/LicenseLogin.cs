using System;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace TargetULPCommercial.security
{
    public static class LicenseLogin
    {
        private const string RegistryKeyPath = @"SOFTWARE\TargetULPCommercial";
        private const string LicenseRegistryValue = "LicenseKey";
        private const string JwtHashRegistryValue = "JwtKeyHash";

        public class LicensePayload
        {
            [JsonProperty("exp")]
            public long Expiration { get; set; }
            
            [JsonProperty("licensekey")]
            public string LicenseKey { get; set; }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static string GetHwid()
        {
            var systemInfo = Environment.MachineName + Environment.OSVersion + Environment.ProcessorCount;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(systemInfo));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static string GenerateHashWithSalt(string inputStr, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string dataToHash = inputStr + salt;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static string ExtractNumbers(string inputStr)
        {
            var matches = Regex.Matches(inputStr, @"\d+");
            return string.Join("", matches.Cast<Match>().Select(m => m.Value));
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static string GetLicenseFromRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(LicenseRegistryValue)?.ToString();
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static void StoreLicenseInRegistry(string licenseKey)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key?.SetValue(LicenseRegistryValue, licenseKey);
            }
        }

        private static string Base64UrlDecode(string base64Url)
        {
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        private static bool ValidateLicenseKey(string licenseKey)
        {
            try
            {
                var tokenParts = licenseKey.Split('.');
                if (tokenParts.Length != 3) return false;

                var payload = JsonConvert.DeserializeObject<LicensePayload>(
                    Base64UrlDecode(tokenParts[1])
                );

                if (DateTimeOffset.FromUnixTimeSeconds(payload.Expiration).UtcDateTime < DateTime.UtcNow)
                {
                    RemoveLicenseFromRegistry();
                    return false;
                }
                return true;
            }
            catch { return false; }
        }

        public static void Login()
        {
            Console.Title = "TARGETULP";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
 _____                    _             _   _ _     ____  
|_   _|_ _ _ __ __ _  ___| |_          | | | | |   |  _ \ 
  | |/ _` | '__/ _` |/ _ \ __|  _____  | | | | |   | |_) |
  | | (_| | | | (_| |  __/ |_  |_____| | |_| | |___|  __/ 
  |_|\__,_|_|  \__, |\___|\__|          \___/|_____|_|    
               |___/   

This Tools Is Developed By @ProfessorTouch For OSINT & Educational Purposes Only!                                      
");


            string storedLicenseKey = GetLicenseFromRegistry();

            if (storedLicenseKey != null && ValidateLicenseKey(storedLicenseKey))
            {
                var payload = JsonConvert.DeserializeObject<LicensePayload>(
                    Base64UrlDecode(storedLicenseKey.Split('.')[1])
                );
                
                ValidateLicense(payload.LicenseKey, storedLicenseKey);
            }
            else
            {
                RemoveLicenseFromRegistry();
                AskForNewLicenseKey();
            }
        }

        private static void ValidateLicense(string predefinedNumericValue, string licenseKey)
        {
            string numericPart = ExtractNumbers(
                GenerateHashWithSalt(GetHwid(), "TargetULP")
            );

            if (numericPart == predefinedNumericValue)
            {
                Console.WriteLine("VALID LICENSE.");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("LOGGING IN...");
                Thread.Sleep(2000);
            }
            else
            {
                Console.WriteLine("INVALID LICENSE.");
                Environment.Exit(1);
            }
        }

        private static void AskForNewLicenseKey()
        {
            Console.WriteLine($"HWID: {GetHwid()}");
            Console.Write("\nENTER LICENSE KEY: ");
            string licenseKey = Console.ReadLine()?.Trim();

            if (ValidateLicenseKey(licenseKey))
            {
                StoreLicenseInRegistry(licenseKey);
                var payload = JsonConvert.DeserializeObject<LicensePayload>(
                    Base64UrlDecode(licenseKey.Split('.')[1])
                );
                IntegrityCheck.GenerateAndStoreJwtHash(licenseKey);
                ValidateLicense(payload.LicenseKey, licenseKey);
            }
            else
            {
                Console.WriteLine("INVALID LICENSE KEY.");
                Environment.Exit(1);
            }
        }
        
        public static bool IsLicenseValid()
        {
            string storedLicenseKey = GetLicenseFromRegistry();

            if (storedLicenseKey != null && ValidateLicenseKey(storedLicenseKey))
            {
                var payload = JsonConvert.DeserializeObject<LicensePayload>(
                    Base64UrlDecode(storedLicenseKey.Split('.')[1])
                );

                if (DateTimeOffset.FromUnixTimeSeconds(payload.Expiration).UtcDateTime > DateTime.UtcNow)
                {
                    return true; // License is valid
                }
            }

            return false; // License is invalid or expired
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void RemoveLicenseFromRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
            {
                if (key != null)
                {
                    // Only delete if the value exists
                    if (key.GetValue(LicenseRegistryValue) != null)
                    {
                        key.DeleteValue(LicenseRegistryValue);
                    }

                    if (key.GetValue(JwtHashRegistryValue) != null)
                    {
                        key.DeleteValue(JwtHashRegistryValue);
                    }
                }
            }
        }

    }
}