using System.Text.Json;

namespace SalesmessageExporter.Services.Messages
{
    /// Retrieves all messages for a conversation using
    /// Salesmessage message pagination.
    public class ConversationMessageService
    {
        private readonly HttpClient _httpClient;

        private readonly SalesmessageApiService _apiService;

        public ConversationMessageService(SalesmessageApiService apiService)
        {
            _apiService = apiService;
        }

        /// Retrieves every message for a conversation.
        public async Task<List<JsonElement>> GetAllMessagesForConversationAsync(
            long conversationId)
        {
            var allMessages = new List<JsonElement>();

            int currentPage = 1;
            int lastPage = 1;

            do
            {
                Console.WriteLine(
                    $"Conversation {conversationId} - Page {currentPage}");

                string endpoint =
                    $"/messages/{conversationId}/paginated?per_page=15&page={currentPage}";

                var response = await _httpClient.GetAsync(endpoint);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                // Get message array
                JsonElement data =
                    root.GetProperty("data");

                foreach (JsonElement message in data.EnumerateArray())
                {
                    allMessages.Add(message.Clone());
                }

                // Read pagination metadata
                JsonElement meta =
                    root.GetProperty("meta");

                lastPage =
                    meta.GetProperty("last_page").GetInt32();

                Console.WriteLine(
                    $"Retrieved page {currentPage} of {lastPage}");

                currentPage++;

            } while (currentPage <= lastPage);

            Console.WriteLine(
                $"Conversation {conversationId} complete. " +
                $"Messages retrieved: {allMessages.Count}");

            return allMessages;
        }
    }
}