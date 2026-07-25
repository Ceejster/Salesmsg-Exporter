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
                    await _httpClient.GetAsync(AppSettings.InboxesEndpoint);

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

            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(endpoint);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            });
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

            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(endpoint);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            });
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

            return await ExecuteWithRetryAsync(async () =>
            {
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

                // last_page is total pages; fall back to calculating
                // from total records ÷ per_page if last_page is absent/null
                int totalPages = 1;

                if (doc.RootElement.TryGetProperty("last_page", out JsonElement lastPageEl)
                    && lastPageEl.ValueKind != JsonValueKind.Null
                    && lastPageEl.TryGetInt32(out int lp))
                {
                    totalPages = lp;
                }
                else if (
                    doc.RootElement.TryGetProperty("total", out JsonElement totalEl)
                    && totalEl.TryGetInt32(out int totalRecords)
                    && doc.RootElement.TryGetProperty("per_page", out JsonElement perPageEl)
                    && perPageEl.ValueKind != JsonValueKind.Null
                    && perPageEl.TryGetInt32(out int perPage)
                    && perPage > 0)
                {
                    totalPages = (int)Math.Ceiling((double)totalRecords / perPage);
                }

                return new MessagePageResponse
                {
                    CurrentPage = currentPage,
                    TotalPages = totalPages,
                    RawJson = json
                };
            });
        }

        // -----------------------------------------------------
        // RETRY HELPER — exponential backoff on timeout/5xx
        // -----------------------------------------------------
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
        {
            int attempt = 0;

            while (true)
            {
                attempt++;

                try
                {
                    return await operation();
                }
                catch (Exception ex) when (
                    attempt < AppSettings.MaxRetryAttempts &&
                    (ex is TaskCanceledException ||
                     (ex is HttpRequestException httpEx &&
                      httpEx.StatusCode == null))) // null = network failure, not HTTP error
                {
                    int delaySeconds =
                        AppSettings.RetryDelayBaseSeconds * attempt;

                    Console.WriteLine(
                        $"Request failed (attempt {attempt}/{AppSettings.MaxRetryAttempts}): " +
                        $"{ex.Message}. Retrying in {delaySeconds}s...");

                    await Task.Delay(
                        TimeSpan.FromSeconds(delaySeconds));
                }
            }
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

        // Returns id + name for every inbox the account has access to
        public async Task<List<(int Id, string Name)>> GetAllInboxInfoAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(AppSettings.InboxesEndpoint);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument doc =
                    JsonDocument.Parse(json);

                var inboxes = new List<(int Id, string Name)>();

                JsonElement root = doc.RootElement;

                IEnumerable<JsonElement> items =
                    root.ValueKind == JsonValueKind.Array
                        ? root.EnumerateArray()
                        : root.TryGetProperty("data", out JsonElement data)
                            ? data.EnumerateArray()
                            : Enumerable.Empty<JsonElement>();

                foreach (JsonElement inbox in items)
                {
                    if (inbox.TryGetProperty("id", out JsonElement idEl) &&
                        idEl.TryGetInt32(out int id))
                    {
                        string name = inbox.TryGetProperty(
                            "name", out JsonElement nameEl)
                            ? nameEl.GetString() ?? $"Inbox_{id}"
                            : $"Inbox_{id}";

                        Console.WriteLine($"  Inbox {id}: {name}");
                        inboxes.Add((id, name));
                    }
                }

                return inboxes;
            });
        }

        // Returns the ID of every inbox the account has access to
        public async Task<List<int>> GetAllInboxIdsAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(AppSettings.InboxesEndpoint);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument doc =
                    JsonDocument.Parse(json);

                var ids = new List<int>();

                JsonElement root = doc.RootElement;

                // Handle a bare array or an object wrapping a "data" array
                IEnumerable<JsonElement> items =
                    root.ValueKind == JsonValueKind.Array
                        ? root.EnumerateArray()
                        : root.TryGetProperty("data", out JsonElement data)
                            ? data.EnumerateArray()
                            : Enumerable.Empty<JsonElement>();

                foreach (JsonElement inbox in items)
                {
                    if (inbox.TryGetProperty("id", out JsonElement idEl) &&
                        idEl.TryGetInt32(out int id))
                    {
                        string name = inbox.TryGetProperty(
                            "name", out JsonElement nameEl)
                            ? nameEl.GetString() ?? ""
                            : "";

                        Console.WriteLine($"  Inbox {id}: {name}");
                        ids.Add(id);
                    }
                }

                return ids;
            });
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

        // Collects conversation IDs + contact info for index generation
        public async Task<List<ConversationIndexEntry>> GetAllConversationInfoAsync(
            int inboxId,
            string inboxName)
        {
            var entries = new List<ConversationIndexEntry>();

            await CollectConversationInfoByFilterAsync(inboxId, inboxName, "open",   entries);
            await CollectConversationInfoByFilterAsync(inboxId, inboxName, "closed", entries);

            return entries;
        }

        private async Task CollectConversationInfoByFilterAsync(
            int inboxId,
            string inboxName,
            string filter,
            List<ConversationIndexEntry> entries)
        {
            int currentOffset = 0;
            bool hasMoreRecords = true;
            var seen = new HashSet<long>();

            while (hasMoreRecords)
            {
                string json =
                    await GetConversationsPageRawAsync(inboxId, currentOffset, filter);

                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    break;

                int recordCount = 0;

                foreach (JsonElement conv in doc.RootElement.EnumerateArray())
                {
                    recordCount++;

                    if (!conv.TryGetProperty("id", out JsonElement idEl) ||
                        !idEl.TryGetInt64(out long id) ||
                        !seen.Add(id))
                        continue;

                    string contactName  = "";
                    string contactPhone = "";
                    string contactEmail = "";

                    if (conv.TryGetProperty("contact", out JsonElement contactEl) &&
                        contactEl.ValueKind == JsonValueKind.Object)
                    {
                        contactName  = GetJsonString(contactEl, "full_name");
                        contactPhone = GetJsonString(contactEl, "formatted_number");
                        contactEmail = GetJsonString(contactEl, "email");
                    }

                    entries.Add(new ConversationIndexEntry
                    {
                        ConversationId = id,
                        InboxId        = inboxId,
                        InboxName      = inboxName,
                        ContactName    = contactName,
                        ContactPhone   = contactPhone,
                        ContactEmail   = contactEmail
                    });
                }

                hasMoreRecords = recordCount == AppSettings.DefaultPageSize;
                currentOffset += (AppSettings.DefaultPageSize - 1);
            }
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

        // Returns one page of call history for an inbox
        public async Task<string> GetCallHistoryPageRawAsync(int inboxId, int page)
        {
            string endpoint =
                $"{AppSettings.CallHistoryEndpoint}" +
                $"?team_id={inboxId}" +
                $"&team_type=local" +
                $"&page={page}";

            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(endpoint);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            });
        }

        // Returns all members/users in the org
        public async Task<string> GetAllMembersRawAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(AppSettings.UsersEndpoint);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            });
        }

        private static string GetJsonString(JsonElement element, string key)
        {
            if (!element.TryGetProperty(key, out JsonElement prop))
                return "";

            return prop.ValueKind == JsonValueKind.Null ? "" : prop.ToString();
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