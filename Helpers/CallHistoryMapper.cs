using System.Text.Json;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class CallHistoryMapper
    {
        public static (List<ExportCallEntry> Calls, bool HasMore) MapPage(string json)
        {
            var calls = new List<ExportCallEntry>();
            bool hasMore = false;

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("meta", out JsonElement meta) &&
                meta.TryGetProperty("has_more", out JsonElement hasMoreEl))
            {
                hasMore = hasMoreEl.ValueKind == JsonValueKind.True;
            }

            if (!root.TryGetProperty("data", out JsonElement data) ||
                data.ValueKind != JsonValueKind.Array)
                return (calls, hasMore);

            foreach (JsonElement entry in data.EnumerateArray())
            {
                string type        = "";
                string outcome     = "";
                int    duration    = 0;
                string createdAt   = "";
                string recordUrl   = "";

                if (entry.TryGetProperty("call", out JsonElement call) &&
                    call.ValueKind == JsonValueKind.Object)
                {
                    type      = GetString(call, "type");
                    outcome   = GetString(call, "outcome");
                    duration  = GetInt(call, "duration");
                    createdAt = GetString(call, "created_at");
                    recordUrl = GetString(call, "record_url");
                }

                string contactName   = "";
                string contactNumber = "";

                if (entry.TryGetProperty("contact", out JsonElement contact) &&
                    contact.ValueKind == JsonValueKind.Object)
                {
                    contactName   = GetString(contact, "name");
                    contactNumber = GetString(contact, "formatted_number");
                }

                string handledBy = "";

                if (entry.TryGetProperty("contacted_user", out JsonElement user) &&
                    user.ValueKind == JsonValueKind.Object)
                {
                    handledBy = GetString(user, "name");
                }

                calls.Add(new ExportCallEntry
                {
                    Type          = type,
                    Outcome       = outcome,
                    Duration      = duration,
                    CreatedAt     = createdAt,
                    ContactName   = contactName,
                    ContactNumber = contactNumber,
                    HandledBy     = handledBy,
                    RecordUrl     = recordUrl
                });
            }

            return (calls, hasMore);
        }

        private static string GetString(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return "";
            return prop.ValueKind == JsonValueKind.Null ? "" : prop.ToString();
        }

        private static int GetInt(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return 0;
            return prop.TryGetInt32(out int v) ? v : 0;
        }
    }
}
