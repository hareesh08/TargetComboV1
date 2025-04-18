using System;
using System.IO;
using System.Windows.Forms;
using TargetULPCommercial.security;
using System.Management;

namespace TargetCombo_V1
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            LicenseLogin.Login();
            IntegrityCheck.VerifyJwtHash();
            
            var shadowCheck = new LicenseShadowCheck(120000);
            shadowCheck.Start();
            
            Console.Clear();
            // Set console title
            Console.Title = "TARGETULP";

            // Display header in cyan
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
            Console.ResetColor();

            bool continueProcessing = true;

            while (continueProcessing)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nSelect an option:");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("1. Targeted Combo From Using Links");
                Console.WriteLine("2. Full Extract All From ULP [E:P, U:P, N:P] ");
                Console.WriteLine("3. Full Selective Extract From ULP (email:pass, user:pass, number:pass)");
                Console.WriteLine("4. Full Selective Extract From ULP Using Links");
                Console.WriteLine("5. Keyword Based Individual Selective Extract");
                Console.WriteLine("6. ULP ADD HEADER TO ULP [HTTPS://]");
                Console.WriteLine("7. EXTRACT BASED ON COUNTRY DOMAIN");
                Console.WriteLine("Q. Quit");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Magenta;
                IntegrityCheck.VerifyJwtHash();
                Console.Write("Enter your choice: ");
                Console.ResetColor();
                string choice = Console.ReadLine()?.Trim().ToUpper();

                if (choice == "1")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting Links Based Extraction...");
                    Console.ResetColor();

                    string linksFilePath = SelectLinksFile();
                    if (linksFilePath == null) continue;

                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    Module1.ProcessFiles(linksFilePath, ref totalLinesLoaded, ref totalLinesSaved);

                    DisplaySummary(totalLinesLoaded, totalLinesSaved);
                }
                else if (choice == "2")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting Full Extraction...");
                    Console.ResetColor();

                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    Module2.ExtractAllCredentials(ref totalLinesLoaded, ref totalLinesSaved);

                    DisplaySummary(totalLinesLoaded, totalLinesSaved);
                }
                else if (choice == "3")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting Selective Extraction...");
                    Console.ResetColor();
                    Console.Clear();

                    string mode = SelectExtractionMode();
                    if (mode == null) continue;

                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    Module3.ExtractSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode);

                    DisplaySummary(totalLinesLoaded, totalLinesSaved);
                }
                else if (choice == "4")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting Selective Extraction...");
                    Console.ResetColor();
                    Console.Clear();

                    string mode = SelectExtractionMode();
                    if (mode == null) continue;

                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    
                    string linksFilePath = SelectLinksFile();
                    if (linksFilePath == null) continue;
                    
                    Module4.ExtractLinkBasedSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode, linksFilePath);

                    DisplaySummary(totalLinesLoaded, totalLinesSaved);
                }
                else if (choice == "5")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting Selective Extraction...");
                    Console.ResetColor();
                    Console.Clear();

                    string mode = SelectExtractionMode();
                    if (mode == null) continue;

                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    
                    string linksFilePath = SelectLinksFile();
                    if (linksFilePath == null) continue;
                    
                    Module5.ExtractLinkBasedSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode, linksFilePath);

                    DisplaySummary(totalLinesLoaded, totalLinesSaved);
                }
                else if (choice == "6")
                {
                    IntegrityCheck.VerifyJwtHash();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting ULP Convertor...");
                    Console.ResetColor();
                    Console.Clear();
                    Module6.ProcessFiles();
                }
                else if (choice == "7")
                {
                    IntegrityCheck.VerifyJwtHash();
                    int totalLinesLoaded = 0;
                    int totalLinesSaved = 0;
                    
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("\nStarting ULP Convertor...");
                    Console.ResetColor();
                    Console.Clear();
                    ExtractUrlByCountry.ProcessFiles(ref totalLinesLoaded, ref totalLinesSaved);
                }
                else if (choice == "Q")
                {
                    shadowCheck.Stop();
                    continueProcessing = false;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Exiting the program...");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid choice. Please select 1, 2, 3, or Q.");
                    Console.ResetColor();
                }
            }
        }

        private static string SelectLinksFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt";
                openFileDialog.Title = "Select the links file";
                if (openFileDialog.ShowDialog() != DialogResult.OK) return null;

                return openFileDialog.FileName;
            }
        }

        private static string SelectExtractionMode()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nSelect extraction mode:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("1. Email:Pass");
            Console.WriteLine("2. Number:Pass");
            Console.WriteLine("3. User:Pass");
            Console.WriteLine("4. All");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Enter your choice: ");
            Console.ResetColor();
            string modeChoice = Console.ReadLine()?.Trim();

            return modeChoice switch
            {
                "1" => "email",
                "2" => "number",
                "3" => "user",
                "4" => "all",
                _ => null
            };
        }

        private static void DisplaySummary(int totalLinesLoaded, int totalLinesSaved)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nTotal lines loaded from source files: {totalLinesLoaded}");
            Console.WriteLine($"Total lines saved to destination files: {totalLinesSaved}");
            Console.WriteLine("Processing completed.");
            Console.ResetColor();
        }
    }
}
