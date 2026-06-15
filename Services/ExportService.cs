using System.Text.Json;
using SalesmessageExporter.Config;
using SalesmessageExporter.Helpers;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Services
{
    public class ExportService
    {
        private readonly SalesmessageApiService _apiService;

        public ExportService(SalesmessageApiService apiService)
        {
            _apiService = apiService;
        }

        // Main inbox export loop
        public async Task ExportInboxAsync(
            int inboxId,
            string orgName,
            int startOffset = 0,
            int startPage = 1)
        {
            int currentOffset = startOffset;

            // User-facing page number
            int currentPage = startPage;

            bool hasMoreRecords = true;

            Console.WriteLine(
                $"Starting export for {orgName}");

            Console.WriteLine(
                $"Starting from page {currentPage}");

            Console.WriteLine();

            while (hasMoreRecords)
            {
                Console.WriteLine(
                    $"Fetching Page {currentPage} " +
                    $"(Offset {currentOffset})...");

                // Pull raw JSON from API
                string rawJson =
                    await _apiService.GetConversationsPageRawAsync(
                        inboxId,
                        currentOffset);

                // Convert raw JSON into clean export objects
                List<ExportConversation> conversations =
                    ConversationMapper.MapConversations(rawJson);

                // Convert cleaned objects back into readable JSON
                string formattedJson =
                    JsonSerializer.Serialize(
                        conversations,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                // Save export file
                await FileExportHelper.SaveJsonPageAsync(
                    formattedJson,
                    orgName,
                    currentPage);

                Console.WriteLine(
                    $"Saved Page {currentPage}.");

                int returnedRecordCount =
                    conversations.Count;

                Console.WriteLine(
                    $"Returned records: {returnedRecordCount}");

                // If fewer than page size returned, we've reached the end
                hasMoreRecords =
                    returnedRecordCount ==
                    AppSettings.DefaultPageSize;

                Console.WriteLine(
                    $"Has more records: {hasMoreRecords}");

                // Pause every checkpoint interval
                if (currentPage %
                    AppSettings.PageCheckpoint == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"Checkpoint reached at Page {currentPage}");

                    Console.WriteLine(
                        "Continue export? (Y/N)");

                    Console.Write("> ");

                    string input =
                        Console.ReadLine() ?? "";

                    if (!input.Equals(
                        "Y",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "Export stopped by user.");

                        return;
                    }
                }

                // IMPORTANT: overlap previous last record with next request's first record
                currentOffset +=
                    (AppSettings.DefaultPageSize - 1);

                // Increment visible page counter
                currentPage++;
            }

            Console.WriteLine();
            Console.WriteLine(
                "Inbox export completed.");
        }

        // Stage 1:
        // Collect all OPEN and CLOSED conversation IDs
        // and save them into a single JSON file.
        public async Task ExportConversationIdsAsync(
            int inboxId,
            string orgName)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Collecting conversation IDs for {orgName}...");
            Console.WriteLine();

            HashSet<long> conversationIds =
                await _apiService.GetAllConversationIdsAsync(
                    inboxId);

            Console.WriteLine();
            Console.WriteLine(
                $"Found {conversationIds.Count} unique conversations.");

            // Display IDs to user
            Console.WriteLine();
            Console.WriteLine("Conversation IDs:");
            Console.WriteLine();
            Console.WriteLine(
                $"Found {conversationIds.Count} unique conversations.");

            //If we want to display all IDs in the console, we can uncomment this line.

            //Console.WriteLine(
            //    string.Join(", ", conversationIds));

            string formattedJson =
                JsonSerializer.Serialize(
                    conversationIds,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await FileExportHelper.SaveConversationIdsAsync(
                formattedJson,
                orgName);

            Console.WriteLine();
            Console.WriteLine(
                "Conversation ID export completed.");
        }


        public async Task ExportFullConversationHistoryAsync( int inboxId, string orgName)
        {
            Console.WriteLine();
            Console.WriteLine($"Starting FULL conversation export for {orgName}");
            Console.WriteLine();

            // Step 1: get all conversation IDs
            HashSet<long> conversationIds =
                await _apiService.GetAllConversationIdsAsync(inboxId);

            List<long> idList = conversationIds.ToList();

            Console.WriteLine($"Total conversations: {idList.Count}");
            Console.WriteLine();

            int index = 1;

            // Step 2: export each conversation
            foreach (long conversationId in idList)
            {
                Console.WriteLine(
                    $"[{index}/{idList.Count}] Exporting conversation {conversationId}");

                await ExportSingleConversationAsync(
                    conversationId,
                    orgName);

                index++;
            }

            Console.WriteLine();
            Console.WriteLine("FULL EXPORT COMPLETE");
        }

        private async Task ExportSingleConversationAsync(long conversationId, string orgName)
        {
            int page = 1;
            bool hasMorePages = true;

            List<ExportMessage> allMessages = new();

            while (hasMorePages)
            {
                var response =
                    await _apiService.GetConversationMessagesPageAsync(
                        conversationId,
                        page);

                List<ExportMessage> messages =
                    MessageMapper.MapMessages(
                        response.RawJson);

                allMessages.AddRange(messages);

                hasMorePages =
                    response.CurrentPage < response.TotalPages;

                page++;
            }

            var exportObject = new
            {
                conversation_id = conversationId,
                exported_at = DateTime.UtcNow,
                message_count = allMessages.Count,
                messages = allMessages
            };

            string json =
                JsonSerializer.Serialize(
                    exportObject,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await FileExportHelper.SaveConversationAsync(
                json,
                orgName,
                conversationId);
        }

        public async Task ExportSingleConversationByIdAsync(long conversationId, string orgName)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Exporting conversation {conversationId}");

            await ExportSingleConversationAsync(
                conversationId,
                orgName);

            Console.WriteLine();
            Console.WriteLine(
                "Conversation export complete.");
        }

        public async Task ExportConversationListAsync(
    List<long> conversationIds,
    string orgName)
        {
            if (conversationIds == null || conversationIds.Count == 0)
            {
                Console.WriteLine("No conversation IDs provided.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Starting export of {conversationIds.Count} conversations...");
            Console.WriteLine();

            int index = 1;

            foreach (long conversationId in conversationIds)
            {
                Console.WriteLine(
                    $"[{index}/{conversationIds.Count}] Exporting {conversationId}");

                try
                {
                    await ExportSingleConversationAsync(
                        conversationId,
                        orgName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Failed conversation {conversationId}: {ex.Message}");
                }

                index++;
            }

            Console.WriteLine();
            Console.WriteLine("Conversation list export complete.");
        }
    }
}