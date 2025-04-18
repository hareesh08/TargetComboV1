using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace TargetULPCommercial.security
{
    public static class IntegrityCheck
    {
        private const string RegistryKeyPath = @"SOFTWARE\TargetULPCommercial";
        private const string LicenseRegistryValue = "LicenseKey";
        private const string JwtHashRegistryValue = "JwtKeyHash";
        private const string FirstRunValue = "FirstRun";

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void GenerateAndStoreJwtHash(string jwtKey)
        {
            string hash = GenerateHash(jwtKey);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key?.SetValue(JwtHashRegistryValue, hash);
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void VerifyJwtHash()
        {
            string jwtKey = GetLicenseKeyFromRegistry();
            if (string.IsNullOrEmpty(jwtKey))
            {
                LicenseLogin.Login();
                return;
            }

            string hash = GenerateHash(jwtKey);
            string storedHash = GetStoredJwtHashFromRegistry();

            if (string.IsNullOrEmpty(storedHash) || hash != storedHash)
            {
                LicenseLogin.RemoveLicenseFromRegistry();
                Console.WriteLine("License has been tampered with! Exiting...");
                Environment.Exit(1);
            }
        }

        private static string GenerateHash(string jwtKey)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jwtKey));
                return BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .Substring(0, 8);
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static string GetStoredJwtHashFromRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(JwtHashRegistryValue)?.ToString();
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static string GetLicenseKeyFromRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(LicenseRegistryValue)?.ToString();
            }
        }
        
        
    }
}