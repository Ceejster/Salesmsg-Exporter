using System.Text.Json;

namespace SalesmessageExporter.Services.Messages
{
    /// Retrieves all messages for a conversation using
    /// Salesmessage message pagination.
    public class ConversationMessageService
    {
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

            List<string> pages =
                await _apiService.GetAllMessagePagesRawAsync(conversationId);

            int pageNumber = 1;

            foreach (string pageJson in pages)
            {
                using JsonDocument document =
                    JsonDocument.Parse(pageJson);

                JsonElement root = document.RootElement;

                if (!root.TryGetProperty("data", out JsonElement data))
                    continue;

                foreach (JsonElement message in data.EnumerateArray())
                {
                    allMessages.Add(message.Clone());
                }

                Console.WriteLine(
                    $"Conversation {conversationId} — " +
                    $"page {pageNumber} of {pages.Count}");

                pageNumber++;
            }

            Console.WriteLine(
                $"Conversation {conversationId} complete. " +
                $"Messages retrieved: {allMessages.Count}");

            return allMessages;
        }
    }
}
