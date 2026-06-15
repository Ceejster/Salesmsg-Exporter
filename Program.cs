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
                Console.WriteLine("2. Export Messages");
                Console.WriteLine("3. Exit");
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

                            Console.WriteLine(
                                $"Start Offset: {startOffset}");

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
                                    // ---------------------------------
                                    case "2":
                                        {
                                            int inboxId =
                                                menuService.GetInboxId();

                                            string orgName =
                                                menuService.GetOrganizationName();

                                            await exportService
                                                .ExportFullConversationHistoryAsync(
                                                    inboxId,
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

                    case "3":
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
    }
}