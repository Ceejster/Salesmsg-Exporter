using SalesmessageExporter.Services;

namespace SalesmessageExporter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var menuService = new MenuService();

            // Get PAT once per session
            string pat = menuService.GetPat();

            var apiService = new SalesmessageApiService(pat);
            var exportService = new ExportService(apiService);

            bool running = true;

            while (running)
            {
                Console.WriteLine();
                Console.WriteLine("=== Salesmessage Export Tool ===");
                Console.WriteLine("1. Export Inbox Conversations");
                Console.WriteLine("2. Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        {
                            // REQUIRED INPUTS
                            int inboxId = menuService.GetInboxId();
                            string orgName = menuService.GetOrganizationName();

                            // ALWAYS fresh start (no memory)
                            int startPage = menuService.GetStartPage(1);
                            int startOffset = 0;

                            Console.WriteLine();
                            Console.WriteLine($"Starting export for: {orgName}");
                            Console.WriteLine($"Inbox ID: {inboxId}");
                            Console.WriteLine($"Start Page: {startPage}");
                            Console.WriteLine($"Start Offset: {startOffset}");
                            Console.WriteLine();

                            await exportService.ExportInboxAsync(
                                inboxId,
                                orgName,
                                startOffset,
                                startPage
                            );

                            break;
                        }

                    case "2":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
    }
}