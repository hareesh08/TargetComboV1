using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace TargetCombo_V1.security;

public static class LicenseLogin
{
    private const string RegistryKeyPath = @"SOFTWARE\TargetULPCommercial";
    private const string LicenseRegistryValue = "LicenseKey";
    private const string JwtHashRegistryValue = "JwtKeyHash";

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    private static string GetHwid()
    {
        var systemInfo = Environment.MachineName + Environment.OSVersion + Environment.ProcessorCount;
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(systemInfo));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private static string GenerateHashWithSalt(string inputStr, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var dataToHash = inputStr + salt;
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private static string ExtractNumbers(string inputStr)
    {
        var matches = Regex.Matches(inputStr, @"\d+");
        return string.Join("", matches.Select(m => m.Value));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    private static string GetLicenseFromRegistry()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
        {
            return key?.GetValue(LicenseRegistryValue)?.ToString();
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    private static void StoreLicenseInRegistry(string licenseKey)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
        {
            key?.SetValue(LicenseRegistryValue, licenseKey);
        }
    }

    private static string Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
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
        catch
        {
            return false;
        }
    }

    public static void Login()
    {
        Console.Title = "TARGETCOMBO";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
++------------------------------------------------------------------------------------------------++ 
++------------------------------------------------------------------------------------------------++ 
||████████╗ █████╗ ██████╗  ██████╗ ███████╗████████╗ ██████╗ ██████╗ ███╗   ███╗██████╗  ██████╗ || 
||╚══██╔══╝██╔══██╗██╔══██╗██╔════╝ ██╔════╝╚══██╔══╝██╔════╝██╔═══██╗████╗ ████║██╔══██╗██╔═══██╗|| 
||   ██║   ███████║██████╔╝██║  ███╗█████╗     ██║   ██║     ██║   ██║██╔████╔██║██████╔╝██║   ██║|| 
||   ██║   ██╔══██║██╔══██╗██║   ██║██╔══╝     ██║   ██║     ██║   ██║██║╚██╔╝██║██╔══██╗██║   ██║|| 
||   ██║   ██║  ██║██║  ██║╚██████╔╝███████╗   ██║   ╚██████╗╚██████╔╝██║ ╚═╝ ██║██████╔╝╚██████╔╝|| 
||   ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝   ╚═╝    ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚═════╝  ╚═════╝ || 
++------------------------------------------------------------------------------------------------++ 
++------------------------------------------------------------------------------------------------++ 

This Tool Is Developed By @ProfessorTouch For OSINT & Educational Purposes Only!                                 
");


        var storedLicenseKey = GetLicenseFromRegistry();
        
        if (storedLicenseKey != null &&!JwtIntegrityCheck.ValidateJwtSignature(storedLicenseKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid License Key");
            Thread.Sleep(2000);
            RemoveLicenseFromRegistry();
            AskForNewLicenseKey();
        }
        
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
        var numericPart = ExtractNumbers(
            GenerateHashWithSalt(GetHwid(), "TargetULP")
        );

        if (numericPart == predefinedNumericValue)
        {
            Console.WriteLine("VALID LICENSE.");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("LOGGING IN...");
            Thread.Sleep(1500);
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
        var licenseKey = Console.ReadLine()?.Trim();
        
        if (licenseKey != null &&!JwtIntegrityCheck.ValidateJwtSignature(licenseKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid License Key");
            Thread.Sleep(2000);
            RemoveLicenseFromRegistry();
            AskForNewLicenseKey();
        }

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
        var storedLicenseKey = GetLicenseFromRegistry();

        if (storedLicenseKey != null && ValidateLicenseKey(storedLicenseKey))
        {
            var payload = JsonConvert.DeserializeObject<LicensePayload>(
                Base64UrlDecode(storedLicenseKey.Split('.')[1])
            );

            if (DateTimeOffset.FromUnixTimeSeconds(payload.Expiration).UtcDateTime >
                DateTime.UtcNow) return true; // License is valid
        }

        return false; // License is invalid or expired
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    public static void RemoveLicenseFromRegistry()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            if (key != null)
            {
                // Only delete if the value exists
                if (key.GetValue(LicenseRegistryValue) != null) key.DeleteValue(LicenseRegistryValue);

                if (key.GetValue(JwtHashRegistryValue) != null) key.DeleteValue(JwtHashRegistryValue);
            }
        }
    }

    public class LicensePayload
    {
        [JsonProperty("exp")] public long Expiration { get; set; }

        [JsonProperty("licensekey")] public string LicenseKey { get; set; }
    }
}