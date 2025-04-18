using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TargetULPCommercial.security
{
    public class LicenseShadowCheck
    {
        private readonly CancellationTokenSource _cts;
        private readonly int _checkIntervalMilliseconds;
        
        public LicenseShadowCheck(int checkIntervalMilliseconds = 30000) // Default to 30 seconds
        {
            _cts = new CancellationTokenSource();
            _checkIntervalMilliseconds = checkIntervalMilliseconds;
        }

        // Start checking the license validity in the background
        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Call the method to check if the license is valid
                    bool isValid = LicenseLogin.IsLicenseValid();

                    if (!isValid)
                    {
                        // Notify the user that the license is invalid or expired
                        ShowLicenseExpiredMessage();
                    }

                    // Wait for the specified interval before checking again
                    await Task.Delay(_checkIntervalMilliseconds, _cts.Token);
                }
            });
        }

        // Stop the background task
        public void Stop()
        {
            _cts.Cancel();
        }

        // Show a message when the license is expired or invalid
        private void ShowLicenseExpiredMessage()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Your license is expired or invalid. Please update the license.");
            LicenseLogin.RemoveLicenseFromRegistry();
            Thread.Sleep(3000);
            Environment.Exit(1);
        }
    }
}