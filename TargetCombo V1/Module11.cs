using System;

namespace TargetCombo_V1
{
    internal static class Module11
    {
        public static void DisplayMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.Title = "TARGETCOMBO - Extraction Menu";
                DisplayHeader();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==== Select Extraction Option ====");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("1. Extract SMTP Credentials");
                Console.WriteLine("2. Extract cPanel Credentials");
                Console.WriteLine("3. Extract Webmail Credentials");
                Console.WriteLine("4. Exit");
                Console.ResetColor();

                Console.Write("\nEnter choice (1-4): ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        SmtpExtract();
                        break;
                    case "2":
                        CpanelExtract();
                        break;
                    case "3":
                        WebmailExtract();
                        break;
                    case "4":
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("\nExiting... Goodbye!");
                        Console.ResetColor();
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nInvalid choice. Press Enter to try again.");
                        Console.ResetColor();
                        Console.ReadLine();
                        break;
                }
            }
        }

        private static void SmtpExtract()
        {
            int totalLinesSaved = 0, totalLinesLoaded = 0;
            Console.Clear();
            Console.Title = "TARGETCOMBO - SMTP Extraction";
            DisplayHeader();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStarting SMTP Extraction...\n");
            Console.ResetColor();

            SmtpExtractor.ExtractSmtpCredentials(ref totalLinesLoaded, ref totalLinesSaved);

            Console.Clear();
            Console.Title = "TARGETCOMBO - SMTP Extraction Summary";
            DisplayHeader();
            DisplaySummary(totalLinesLoaded, totalLinesSaved);
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static void CpanelExtract()
        {
            int totalLinesSaved = 0, totalLinesLoaded = 0;
            Console.Clear();
            Console.Title = "TARGETCOMBO - cPanel Extraction";
            DisplayHeader();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStarting cPanel Extraction...\n");
            Console.ResetColor();

            CpanelExtractor.ExtractCpanelCredentials(ref totalLinesLoaded, ref totalLinesSaved);

            Console.Clear();
            Console.Title = "TARGETCOMBO - cPanel Extraction Summary";
            DisplayHeader();
            DisplaySummary(totalLinesLoaded, totalLinesSaved);
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static void WebmailExtract()
        {
            int totalLinesSaved = 0, totalLinesLoaded = 0;
            Console.Clear();
            Console.Title = "TARGETCOMBO - Webmail Extraction";
            DisplayHeader();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nStarting Webmail Extraction...\n");
            Console.ResetColor();

            WebmailExtractor.ExtractWebmailCredentials(ref totalLinesLoaded, ref totalLinesSaved);

            Console.Clear();
            Console.Title = "TARGETCOMBO - Webmail Extraction Summary";
            DisplayHeader();
            DisplaySummary(totalLinesLoaded, totalLinesSaved);
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
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

        private static void DisplaySummary(int totalLoaded, int totalSaved)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Total Lines Loaded: {totalLoaded}");
            Console.WriteLine($"Total Lines Saved: {totalSaved}");
            Console.ResetColor();
        }
    }
}