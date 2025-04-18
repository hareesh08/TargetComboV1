using Microsoft.Win32;
using System;

namespace TargetULPCommercial.security
{
    public static class TrialManager
    {

        // Hardcoded trial end date: 12 April 2025
        private static readonly DateTime HardCodedTrialEndDate = new DateTime(2025, 4, 13, 23, 59, 59, DateTimeKind.Utc);

        /// <summary>
        /// Checks if the trial is valid by comparing the current UTC time with the hardcoded trial end date.
        /// </summary>
        /// <returns>True if the trial is valid, otherwise false.</returns>
        public static bool IsTrialValid()
        {
            return DateTime.UtcNow <= HardCodedTrialEndDate; // Check if the current time is before the hardcoded trial end date
        }
        
    }
}