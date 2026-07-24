using SalesmessageExporter.Services;

namespace SalesmessageExporter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var menuService = new MenuService();

            var (apiService, exportService) =
                await GetValidatedServicesAsync(menuService);

            bool running = true;

            while (running)
            {
                Console.WriteLine();
                Console.WriteLine("=== Salesmessage Export Tool ===");
                Console.WriteLine("1. Export Inbox Conversations");
                Console.WriteLine("2. Export Messages");
                Console.WriteLine("3. Generate Credit Report");
                Console.WriteLine("4. Export Members");
                Console.WriteLine("5. Change PAT");
                Console.WriteLine("6. Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    // ---------------------------------
                    // EXPORT INBOX CONVERSATIONS
                    // ---------------------------------
                    case "1":
                        {
                            int inboxId =
                                menuService.GetInboxId();

                            string orgName =
                                menuService.GetOrganizationName();

                            int startPage =
                                menuService.GetStartPage(1);

                            int startOffset = 0;

                            Console.WriteLine();
                            Console.WriteLine(
                                $"Starting export for: {orgName}");

                            Console.WriteLine(
                                $"Inbox ID: {inboxId}");

                            Console.WriteLine(
                                $"Start Page: {startPage}");

                            Console.WriteLine();

                            await exportService.ExportInboxAsync(
                                inboxId,
                                orgName,
                                startOffset,
                                startPage);

                            break;
                        }

                    // ---------------------------------
                    // EXPORT MESSAGES
                    // ---------------------------------
                    case "2":
                        {
                            bool messagesMenu = true;

                            while (messagesMenu)
                            {
                                Console.WriteLine();
                                Console.WriteLine(
                                    "=== Export Messages ===");

                                Console.WriteLine(
                                    "1. Export Messages By Conversation ID");

                                Console.WriteLine(
                                    "2. Export All Messages From Account");

                                Console.WriteLine(
                                    "3. Back");

                                Console.WriteLine();

                                Console.Write(
                                    "Select option: ");

                                string messageChoice =
                                    Console.ReadLine() ?? "";

                                switch (messageChoice)
                                {
                                    // ---------------------------------
                                    // EXPORT BY CONVERSATION ID
                                    // ---------------------------------
                                    case "1":
                                        {
                                            List<long> conversationIds =
                                                menuService.GetConversationIds();

                                            string orgName =
                                                menuService.GetOrganizationName();

                                            await exportService
                                                .ExportConversationListAsync(
                                                    conversationIds,
                                                    orgName);

                                            break;
                                        }

                                    // ---------------------------------
                                    // EXPORT ENTIRE ACCOUNT
                                    // No inbox ID needed — fetches all inboxes automatically
                                    // ---------------------------------
                                    case "2":
                                        {
                                            string orgName =
                                                menuService.GetOrganizationName();

                                            await exportService
                                                .ExportFullConversationHistoryAsync(
                                                    orgName);

                                            break;
                                        }

                                    case "3":
                                        {
                                            messagesMenu = false;
                                            break;
                                        }

                                    default:
                                        {
                                            Console.WriteLine(
                                                "Invalid option.");
                                            break;
                                        }
                                }
                            }

                            break;
                        }

                    // ---------------------------------
                    // GENERATE CREDIT REPORT
                    // ---------------------------------
                    case "3":
                        {
                            string orgName =
                                menuService.GetOrganizationName();

                            await exportService
                                .GenerateCreditReportAsync(orgName);

                            break;
                        }

                    // ---------------------------------
                    // EXPORT MEMBERS
                    // ---------------------------------
                    case "4":
                        {
                            string orgName =
                                menuService.GetOrganizationName();

                            await exportService
                                .ExportMembersAsync(orgName);

                            break;
                        }

                    // ---------------------------------
                    // CHANGE PAT
                    // ---------------------------------
                    case "5":
                        {
                            (apiService, exportService) =
                                await GetValidatedServicesAsync(menuService);

                            break;
                        }

                    // ---------------------------------
                    // EXIT
                    // ---------------------------------
                    case "6":
                        {
                            running = false;
                            break;
                        }

                    default:
                        {
                            Console.WriteLine(
                                "Invalid option.");
                            break;
                        }
                }
            }
        }

        // Prompts for a PAT and loops until a valid one is provided
        static async Task<(SalesmessageApiService, ExportService)>
            GetValidatedServicesAsync(MenuService menuService)
        {
            while (true)
            {
                string pat = menuService.GetPat();

                var apiService =
                    new SalesmessageApiService(pat);

                Console.Write("Validating PAT...");

                bool valid =
                    await apiService.ValidatePATAsync();

                if (valid)
                {
                    Console.WriteLine(" Valid.");

                    return (apiService, new ExportService(apiService));
                }

                Console.WriteLine(
                    " Invalid — PAT not recognised or expired. Try again.");
            }
        }
    }
}
