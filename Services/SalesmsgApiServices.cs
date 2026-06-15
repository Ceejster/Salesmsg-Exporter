using System.Net.Http.Headers;
using System.Text.Json;
using SalesmessageExporter.Config;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Services
{
    public class SalesmessageApiService
    {
        private readonly HttpClient _httpClient;

        public SalesmessageApiService(string pat)
        {
            _httpClient = new HttpClient();

            _httpClient.BaseAddress =
                new Uri(AppSettings.BaseUrl);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", pat);

            _httpClient.Timeout =
                TimeSpan.FromSeconds(AppSettings.HttpTimeoutSeconds);
        }

        public async Task<bool> ValidatePATAsync()
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        $"{AppSettings.ConversationsEndpoint}?limit=1"
                    );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------
        // CONVERSATIONS (OPEN ONLY)
        // -----------------------------
        public async Task<string> GetConversationsPageRawAsync(
            int inboxId,
            int offset)
        {
            string endpoint =
                $"{AppSettings.ConversationsEndpoint}" +
                $"?inbox_id={inboxId}" +
                $"&filter=open" +
                $"&limit={AppSettings.DefaultPageSize}" +
                $"&offset={offset}";

            HttpResponseMessage response =
                await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // -----------------------------
        // CONVERSATIONS (OPEN / CLOSED)
        // -----------------------------
        public async Task<string> GetConversationsPageRawAsync(
            int inboxId,
            int offset,
            string filter)
        {
            string endpoint =
                $"{AppSettings.ConversationsEndpoint}" +
                $"?inbox_id={inboxId}" +
                $"&filter={filter}" +
                $"&limit={AppSettings.DefaultPageSize}" +
                $"&offset={offset}";

            HttpResponseMessage response =
                await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // ----------------------------------------------------
        // MESSAGES (PAGINATED BY PAGE - DO NOT USE OFFSET HERE)
        // ----------------------------------------------------
        public async Task<MessagePageResponse> GetConversationMessagesPageAsync(
            long conversationId,
            int page)
        {
            string endpoint =
                $"{AppSettings.MessagesEndpoint}/" +
                $"{conversationId}/paginated" +
                $"?per_page=15" +
                $"&page={page}";

            HttpResponseMessage response =
                await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            string json =
                await response.Content.ReadAsStringAsync();

            using JsonDocument doc =
                JsonDocument.Parse(json);

            int currentPage =
                doc.RootElement
                    .GetProperty("current_page")
                    .GetInt32();

            int totalPages =
                doc.RootElement
                    .GetProperty("total")
                    .GetInt32();

            return new MessagePageResponse
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                RawJson = json
            };
        }

        // ----------------------------------------------------
        // NEW: FULL MESSAGE HISTORY LOADER (SAFE LOOP)
        // ----------------------------------------------------
        public async Task<List<string>> GetAllMessagePagesRawAsync(
            long conversationId)
        {
            List<string> pages = new();

            int page = 1;

            while (true)
            {
                MessagePageResponse result =
                    await GetConversationMessagesPageAsync(conversationId, page);

                pages.Add(result.RawJson);

                // STOP CONDITION (correct for page-based APIs)
                if (result.CurrentPage >= result.TotalPages)
                    break;

                page++;
            }

            return pages;
        }

        // Collects all OPEN and CLOSED conversation IDs
        public async Task<HashSet<long>> GetAllConversationIdsAsync(
            int inboxId)
        {
            HashSet<long> conversationIds = new();

            await CollectConversationIdsByFilterAsync(
                inboxId,
                "open",
                conversationIds);

            await CollectConversationIdsByFilterAsync(
                inboxId,
                "closed",
                conversationIds);

            return conversationIds;
        }

        // Internal helper
        private async Task CollectConversationIdsByFilterAsync(
            int inboxId,
            string filter,
            HashSet<long> conversationIds)
        {
            int currentOffset = 0;
            bool hasMoreRecords = true;

            while (hasMoreRecords)
            {
                string json =
                    await GetConversationsPageRawAsync(
                        inboxId,
                        currentOffset,
                        filter);

                using JsonDocument doc =
                    JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind !=
                    JsonValueKind.Array)
                {
                    break;
                }

                int recordCount = 0;

                foreach (JsonElement conversation
                    in doc.RootElement.EnumerateArray())
                {
                    recordCount++;

                    if (conversation.TryGetProperty(
                        "id",
                        out JsonElement idElement))
                    {
                        if (idElement.TryGetInt64(
                            out long id))
                        {
                            conversationIds.Add(id);
                        }
                    }
                }

                hasMoreRecords =
                    recordCount ==
                    AppSettings.DefaultPageSize;

                currentOffset +=
                    (AppSettings.DefaultPageSize - 1);
            }
        }
    }

    // Response model used internally for paging logic
    public class MessagePageResponse
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string RawJson { get; set; } = "";
    }
}