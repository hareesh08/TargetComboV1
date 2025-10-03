using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using Timer = System.Threading.Timer;

namespace TargetCombo_V1.security;

public class TrialManager
{
    private const string RegistryKeyPath = @"SOFTWARE\TargetULPCommercial";
    private const string JwtHashRegistryValue = "JwtKeyHash";
    public const string IntegrityRegistryValue = "IntegrityHash";
    public const string LastTrialRegistryValue = "LastTrialDate";
    private const string LicenseRegistryValue = "LicenseKey";
    private static Timer _licenseTimer;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JwtSecurityTokenHandler))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SHA256))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SymmetricSecurityKey))]
    public static void LicenseManager()
    {
        try
        {
            Console.Title = "TARGETCOMBO | Initializing...";

            if (IsLicenseAvailable())
            {
                Console.WriteLine("Valid license found, logging in...");
                LicenseLogin.Login();
                StartLicenseTimer();
            }
            else
            {
                DisplayActivationMenu();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JwtSecurityToken))]
    public static void DisplayRemainingTimeInTitle()
    {
        try
        {
            var token = GetLicenseFromRegistry();
            if (string.IsNullOrEmpty(token)) return;

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return;

            var jwtToken = handler.ReadJwtToken(token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (long.TryParse(expClaim, out var expUnixTime))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
                var remaining = expTime - DateTime.UtcNow;

                if (remaining.TotalSeconds > 0)
                {
                    Console.Title = $"TARGETCOMBO | License Valid: {remaining:dd\\.hh\\:mm\\:ss}";
                }
                else
                {
                    Console.Title = "TARGETCOMBO | LICENSE EXPIRED";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("License has expired!");
                    Console.ResetColor();
                    LicenseLogin.RemoveLicenseFromRegistry();
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Title = "TARGETCOMBO | License Check Error";
            Console.WriteLine($"License validation error: {ex.Message}");
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Timer))]
    private static void StartLicenseTimer()
    {
        _licenseTimer =
            new Timer(_ => { DisplayRemainingTimeInTitle(); }, null, 0,
                1000); // Update every second
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
    private static bool IsLicenseAvailable()
    {
        try
        {
            using (var registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (registryKey == null) return false;

                var licenseKey = registryKey.GetValue(LicenseRegistryValue)?.ToString();
                if (string.IsNullOrEmpty(licenseKey)) return false;

                return JwtIntegrityCheck.ValidateJwtSignature(licenseKey) &&
                       LicenseLogin.IsLicenseValid();
            }
        }
        catch
        {
            return false;
        }
    }

    private static void DisplayActivationMenu()
    {
        try
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            DisplayHeader();
            Console.WriteLine("\n++-- LICENSE ACTIVATION MENU --++");
            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine("1. Activate License");
            Console.WriteLine("2. Free Trial");
            Console.WriteLine("3. Exit");
            Console.Write("\nSelect option: ");

            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    ActivateLicense();
                    break;
                case "2":
                    ActivateTrial();
                    break;
                case "3":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option, please try again.");
                    Thread.Sleep(1500);
                    DisplayActivationMenu();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Menu error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ActivateLicense()
    {
        Console.WriteLine("\nRedirecting to license activation...");
        Thread.Sleep(1000);
        LicenseLogin.Login();
    }

    private static void ActivateTrial()
    {
        try
        {
            Console.WriteLine("\nInitializing trial activation...");
            var hwid = TokenGen.GetHwid();
            var trialData = TokenGen.GetTrialDataFromRegistry();

            // Handle fresh installation case
            if (trialData == null)
            {
                trialData = new TrialData
                {
                    Token = null,
                    LastTrialDate = null
                };

                // Generate initial integrity hash
                TokenGen.StoreTrialDataIntegrityHash();
            }
            else if (!TokenGen.VerifyTrialDataIntegrity(trialData))
            {
                MessageBox.Show("Trial system integrity check failed. Please contact support.",
                    "Activation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Check if trial was already used today
            if (trialData.LastTrialDate.HasValue &&
                trialData.LastTrialDate.Value.Date == DateTime.UtcNow.Date)
            {
                MessageBox.Show("Daily trial already activated. Please try again tomorrow.",
                    "Trial Limit Reached",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Generate and store new trial token
            var token = TokenGen.GenerateToken(TokenGen.ExpirationOption.Trial, hwid);

            // Store in both registry locations to ensure compatibility
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                // Main license storage that LicenseLogin checks
                key?.SetValue(LicenseRegistryValue, token);

                // For trial-specific tracking (original system)
                key?.SetValue(JwtHashRegistryValue, token);
            }

            TokenGen.UpdateLastTrialDate(DateTime.UtcNow);
            TokenGen.StoreTrialDataIntegrityHash();

            MessageBox.Show($"Trial activated successfully!\nExpires in 5" +
                            $" minutes.",
                "Trial Activated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            TokenGen.StoreTokenInRegistry(token);

            LicenseLogin.Login();
            StartLicenseTimer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Trial activation failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void DisplayHeader()
    {
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
        Console.ResetColor();
    }

    public static class TokenGen
    {
        public enum ExpirationOption
        {
            Trial,
            Full
        }

        private static readonly string Salt = "TargetULP";
        private static readonly string SecretKey = "TARGETULPOBAV2-LICENSE-KEY-TARGETULPOBAV2";

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JwtSecurityToken))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SigningCredentials))]
        public static string GenerateToken(ExpirationOption expirationOption, string hwid)
        {
            var expirationTime = expirationOption == ExpirationOption.Trial
                ? DateTime.UtcNow.AddMinutes(5)
                : DateTime.UtcNow.AddYears(1);

            var hashedHwid = GenerateHashWithSalt(hwid, Salt);
            var licenseKey = ExtractNumbers(hashedHwid);

            var claims = new[]
            {
                new Claim("licensekey", licenseKey),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp,
                    new DateTimeOffset(expirationTime).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Iss, "OfflineAuth"),
                new Claim(JwtRegisteredClaimNames.Aud, "TargetULP"),
                new Claim("hwid", hwid)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                "OfflineAuth",
                "TargetULP",
                claims,
                expires: expirationTime,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MD5))]
        public static string GetHwid()
        {
            var systemInfo = $"{Environment.MachineName}{Environment.OSVersion}{Environment.ProcessorCount}";
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(systemInfo));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void StoreTokenInRegistry(string token)
        {
            // Store in both registry locations to ensure compatibility
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key?.SetValue(LicenseRegistryValue, token);
                key?.SetValue(JwtHashRegistryValue, token);
            }

// Store hash for integrity validation
            IntegrityCheck.GenerateAndStoreJwtHash(token);
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void StoreTrialDataIntegrityHash()
        {
            var trialData = GetTrialDataFromRegistry();
            if (trialData == null) return;

            var hash = GenerateIntegrityHash(trialData);
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key?.SetValue(IntegrityRegistryValue, hash);
            }
        }

        private static string GenerateIntegrityHash(TrialData data)
        {
            var dataString = $"{data.LastTrialDate}|{data.Token}";
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataString));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public static bool VerifyTrialDataIntegrity(TrialData data)
        {
            // Allow null data for fresh installations
            if (data == null) return true;

            // Get stored hash (may be null for fresh install)
            var expectedHash = GetStoredIntegrityHash();

            // If no hash exists yet, this is a fresh install
            if (string.IsNullOrEmpty(expectedHash)) return true;

            // Verify against current data
            var actualHash = GenerateIntegrityHash(data);
            return expectedHash == actualHash;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        private static string GetStoredIntegrityHash()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(IntegrityRegistryValue)?.ToString();
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static TrialData GetTrialDataFromRegistry()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (key == null) return null;

                return new TrialData
                {
                    Token = key.GetValue(JwtHashRegistryValue)?.ToString(),
                    LastTrialDate = DateTime.TryParse(
                        key.GetValue(LastTrialRegistryValue)?.ToString(),
                        out var parsedDate)
                        ? parsedDate
                        : null
                };
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RegistryKey))]
        public static void UpdateLastTrialDate(DateTime trialDate)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key?.SetValue(LastTrialRegistryValue, trialDate.ToString("o"));
            }
        }
    }

    public class TrialData
    {
        public string Token { get; set; }
        public DateTime? LastTrialDate { get; set; }
    }
}