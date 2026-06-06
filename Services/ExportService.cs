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

                // If fewer than page size returned,
                // we've reached the end
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

                // IMPORTANT:
                // overlap previous last record
                // with next request's first record
                currentOffset +=
                    (AppSettings.DefaultPageSize - 1);

                // Increment visible page counter
                currentPage++;
            }

            Console.WriteLine();
            Console.WriteLine(
                "Inbox export completed.");
        }
    }
}