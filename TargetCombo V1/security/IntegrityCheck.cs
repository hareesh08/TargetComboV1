using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace TargetCombo_V1.security;

public static class IntegrityCheck
{
    private const string RegistryKeyPath = @"SOFTWARE\TargetULPCommercial";
    private const string LicenseRegistryValue = "LicenseKey";
    private const string JwtHashRegistryValue = "JwtKeyHash";

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    public static void GenerateAndStoreJwtHash(string jwtKey)
    {
        var hash = GenerateHash(jwtKey);
        using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
        {
            key?.SetValue(JwtHashRegistryValue, hash);
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    public static void VerifyJwtHash()
    {
        var jwtKey = GetLicenseKeyFromRegistry();
        if (string.IsNullOrEmpty(jwtKey))
        {
            LicenseLogin.Login();
            return;
        }

        var hash = GenerateHash(jwtKey);
        var storedHash = GetStoredJwtHashFromRegistry();

        if (string.IsNullOrEmpty(storedHash) || hash != storedHash)
        {
            LicenseLogin.RemoveLicenseFromRegistry();
            Console.WriteLine("License has been tampered with! Exiting...");
            Environment.Exit(1);
        }
    }

    private static string GenerateHash(string jwtKey)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jwtKey));
            return BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .Substring(0, 8);
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    private static string GetStoredJwtHashFromRegistry()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
        {
            return key?.GetValue(JwtHashRegistryValue)?.ToString();
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    private static string GetLicenseKeyFromRegistry()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
        {
            return key?.GetValue(LicenseRegistryValue)?.ToString();
        }
    }
}