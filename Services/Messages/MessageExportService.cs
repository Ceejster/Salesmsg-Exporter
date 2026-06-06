//using System.Text.Json;
//using SalesmessageExporter.Helpers;

//namespace SalesmessageExporter.Services.Messages
//{ 
//    /// Handles exporting full message histories for every conversation in an inbox.
//    public class MessageExportService
//    {
//        private readonly SalesmessageApiService _apiService;
//        private readonly ConversationMessageService _messageService;

//        public MessageExportService(
//            SalesmessageApiService apiService,
//            ConversationMessageService messageService)
//        {
//            _apiService = apiService;
//            _messageService = messageService;
//        }

//        /// Export all messages for all conversations in an inbox.
//        public async Task ExportFullConversationHistoryAsync(
//            int inboxId,
//            string organizationName)
//        {
//            Console.WriteLine();
//            Console.WriteLine("Loading conversation list...");

//            int offset = 0;
//            int limit = 250;

//            int totalConversations = 0;

//            while (true)
//            {
//                var pageJson =
//                    await _apiService.GetConversationsRawAsync(
//                        inboxId,
//                        offset);

//                using var doc =
//                    JsonDocument.Parse(pageJson);

//                var conversations =
//                    doc.RootElement;

//                int conversationCount = 0;

//                foreach (var conversation in conversations.EnumerateArray())
//                {
//                    conversationCount++;

//                    long conversationId =
//                        conversation
//                            .GetProperty("id")
//                            .GetInt64();

//                    Console.WriteLine();
//                    Console.WriteLine(
//                        $"Exporting conversation {conversationId}");

//                    var messages =
//                        await _messageService
//                            .GetAllMessagesForConversationAsync(
//                                conversationId);

//                    string fileName =
//                        $"{organizationName}_Conversation_{conversationId}.json";

//                    await FileExportHelper.SaveConversationJsonAsync(
//                        JsonSerializer.Serialize(messages),
//                        organizationName,
//                        conversationId);

//                    totalConversations++;
//                }

//                if (conversationCount < limit)
//                {
//                    break;
//                }

//                offset += limit;
//            }

//            Console.WriteLine();
//            Console.WriteLine(
//                $"Finished exporting {totalConversations} conversations.");
//        }
//    }
//}