using System.Windows.Forms;
using TargetCombo_V1.security;

namespace TargetCombo_V1;

internal class Program
{
    [STAThread]
    private static void Main()
    {
        LicenseLogin.Login();
        IntegrityCheck.VerifyJwtHash();
        var shadowCheck = new LicenseShadowCheck(120000);
        shadowCheck.Start();

        Console.Clear();
        Console.Title = "TARGETCOMBO";

        var continueProcessing = true;

        while (continueProcessing)
        {
            DisplayHeader();
            DisplayMenu();
            Console.ForegroundColor = ConsoleColor.Magenta;
            IntegrityCheck.VerifyJwtHash();
            Console.Write("Enter your choice: ");
            Console.ResetColor();
            var choice = Console.ReadLine()?.Trim().ToUpper();

            switch (choice)
            {
                case "1":
                    ExecuteLinkBasedExtraction();
                    break;
                case "2":
                    ExecuteFullExtraction();
                    break;
                case "3":
                    ExecuteSelectiveExtraction();
                    break;
                case "4":
                    ExecuteLinkBasedSelectiveExtraction();
                    break;
                case "5":
                    ExecuteKeywordBasedExtraction();
                    break;
                case "6":
                    ExecuteULPConverter();
                    break;
                case "7":
                    ExecuteCountryDomainBasedExtraction();
                    break;
                case "8":
                    log2Ulp();
                    break;
                case "9":
                    CombineULP();
                    break;
                case "10":
                    ULPRemoveDuplicates();
                    break;
                case "Q":
                    shadowCheck.Stop();
                    continueProcessing = false;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Exiting the program...");
                    Console.ResetColor();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid choice. Please select 1, 2, 3, 4, 5, 6, 7 or Q.");
                    Console.ResetColor();
                    break;
            }
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

    private static void DisplayMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nPlease choose an action from the following options:");
        Console.ForegroundColor = ConsoleColor.Cyan;

        // Updated menu titles for clarity
        Console.WriteLine("1.Extract Combo Credentials Using Links");
        Console.WriteLine("2.Full Extraction (Email:Pass, User:Pass, Number:Pass)");
        Console.WriteLine("3.Extract Specific Credentials (email:pass, user:pass, number:pass)");
        Console.WriteLine("4.Extract Specific Credentials Based on Links");
        Console.WriteLine("5.Extract Credentials Based on Keyword Matching");
        Console.WriteLine("6.Convert ULP Format (Add HTTPS:// Header)");
        Console.WriteLine("7.Extract Credentials Based on Country Domain");
        Console.WriteLine("8.Logs 2 ULP");
        Console.WriteLine("9.ULP COMBINE");
        Console.WriteLine("10.ULP DUPLICATE REMOVE");
        Console.WriteLine("Q.Exit Program");

        Console.ResetColor();
    }


    private static void ExecuteLinkBasedExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Link Based Extraction M-1 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Link-Based Extraction...");
        Console.ResetColor();
        var linksFilePath = SelectLinksFile();
        if (linksFilePath == null) return;

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        Module1.ProcessFiles(linksFilePath, ref totalLinesLoaded, ref totalLinesSaved);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void ExecuteFullExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Full Extraction M-2 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Full Extraction...");
        Console.ResetColor();

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        Module2.ExtractAllCredentials(ref totalLinesLoaded, ref totalLinesSaved);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void ExecuteSelectiveExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Selective Full Extraction M-3 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Selective Extraction...");
        Console.ResetColor();

        var mode = SelectExtractionMode();
        if (mode == null) return;

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        Module3.ExtractSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void ExecuteLinkBasedSelectiveExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Link Selective Full Extraction M-4 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Link-Based Selective Extraction...");
        Console.ResetColor();

        var mode = SelectExtractionMode();
        if (mode == null) return;

        var linksFilePath = SelectLinksFile();
        if (linksFilePath == null) return;

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        Module4.ExtractLinkBasedSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode, linksFilePath);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void ExecuteKeywordBasedExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Link Selective Individual Extraction M-5 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Keyword-Based Extraction...");
        Console.ResetColor();

        var mode = SelectExtractionMode();
        if (mode == null) return;

        var linksFilePath = SelectLinksFile();
        if (linksFilePath == null) return;

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        Module5.ExtractLinkBasedSpecificCredentials(ref totalLinesLoaded, ref totalLinesSaved, mode, linksFilePath);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void ExecuteULPConverter()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Converting ULP Header M-6 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting ULP Converter...");
        Console.ResetColor();
        Console.Clear();
        Module6.ProcessFiles();
        Console.WriteLine("\nCompleted ULP Convert...");
        Console.ForegroundColor = ConsoleColor.White; // Reset text color before waiting
        Console.WriteLine("\nPress Enter to continue...");
        Console.ReadLine(); // Wait for user input to continue
    }

    private static void ExecuteCountryDomainBasedExtraction()
    {
        Console.Clear();
        Console.Title = "TARGETCOMBO - Country Based Extraction M-7 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting Country Domain-Based Extraction...");
        Console.ResetColor();

        var totalLinesLoaded = 0;
        var totalLinesSaved = 0;
        ExtractUrlByCountry.ProcessFiles(ref totalLinesLoaded, ref totalLinesSaved);
        DisplaySummary(totalLinesLoaded, totalLinesSaved);
    }

    private static void log2Ulp()
    {
        var mainDirectory = SelectFolder(); // Get the folder path from user
        if (string.IsNullOrEmpty(mainDirectory))
        {
            Console.WriteLine("No folder selected. Exiting...");
            return;
        }

        var totalLinesSaved = 0;
        Console.Clear();
        Console.Title = "TARGETCOMBO - LOGS 2 ULP M-8 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting LOG2ULP...");
        Console.Clear();
        UlpExtractor.ExtractUlpData(ref totalLinesSaved, mainDirectory);
        Console.WriteLine($"\nExtraction complete. {totalLinesSaved} lines saved.");
        Console.ForegroundColor = ConsoleColor.White; // Reset text color before waiting
        Console.WriteLine("\nPress Enter to continue...");
        Console.ReadLine(); // Wait for user input to continue
    }

    private static void CombineULP()
    {
        var totalLinesSaved = 0;
        Console.Clear();
        Console.Title = "TARGETCOMBO - ULP COMBINE M-9 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting ULP COMBINE...");
        Console.Clear();
        ULPCombiner.CombineUlpFiles(ref totalLinesSaved);
        Console.WriteLine($"\nExtraction complete. {totalLinesSaved} lines saved.");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nPress Enter to continue...");
        Console.ReadLine();
    }

    private static void ULPRemoveDuplicates()
    {
        var totalLinesSaved = 0;
        Console.Clear();
        Console.Title = "TARGETCOMBO - ULP REMOVE DUPLICATE M-10 ";
        DisplayHeader();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\nStarting REMOVING DUPLICATE...");
        Console.Clear();
        RemoveDuplicates.RemoveDuplicateLines(ref totalLinesSaved);
        Console.WriteLine($"\nExtraction complete. {totalLinesSaved} lines saved.");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nPress Enter to continue...");
        Console.ReadLine();
    }

    private static string SelectLinksFile()
    {
        using (var openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            openFileDialog.Title = "Select the links file";
            if (openFileDialog.ShowDialog() != DialogResult.OK) return null;
            return openFileDialog.FileName;
        }
    }

    private static string SelectFolder()
    {
        using (var folderBrowserDialog = new FolderBrowserDialog())
        {
            folderBrowserDialog.Description = "Select the folder containing .txt files";
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
                return null;

            return folderBrowserDialog.SelectedPath;
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
        var modeChoice = Console.ReadLine()?.Trim();

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
        Console.ForegroundColor = ConsoleColor.White; // Reset text color before waiting
        Console.WriteLine("\nPress Enter to continue...");
        Console.ReadLine(); // Wait for user input to continue
    }
}