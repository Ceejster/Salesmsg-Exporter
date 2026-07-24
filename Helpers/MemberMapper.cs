using System.Text.Json;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class MemberMapper
    {
        public static List<ExportMember> MapMembers(string json)
        {
            var members = new List<ExportMember>();

            using JsonDocument doc = JsonDocument.Parse(json);

            IEnumerable<JsonElement> items = doc.RootElement.ValueKind switch
            {
                JsonValueKind.Array => doc.RootElement.EnumerateArray(),
                JsonValueKind.Object when doc.RootElement.TryGetProperty(
                    "data", out JsonElement data) => data.EnumerateArray(),
                _ => Enumerable.Empty<JsonElement>()
            };

            foreach (JsonElement item in items)
            {
                ExportMemberNumber number = null;

                if (item.TryGetProperty("number", out JsonElement numEl) &&
                    numEl.ValueKind == JsonValueKind.Object)
                {
                    number = new ExportMemberNumber
                    {
                        FormattedNumber = GetString(numEl, "formatted_number"),
                        IsLandline      = GetInt(numEl, "is_landline") == 1,
                        IsTollFree      = GetBool(numEl, "is_toll_free"),
                        Voice           = GetBool(numEl, "voice")
                    };
                }

                members.Add(new ExportMember
                {
                    Id           = GetLong(item, "id"),
                    FullName     = GetString(item, "full_name"),
                    Email        = GetString(item, "email"),
                    RoleLabel    = GetString(item, "role_label"),
                    Timezone     = GetString(item, "timezone"),
                    UserNumber   = GetString(item, "user_number"),
                    Disabled     = GetNullableBool(item, "disabled"),
                    TwoFaEnabled = GetBool(item, "two_fa_enabled"),
                    CreatedAt    = GetString(item, "created_at"),
                    Number       = number
                });
            }

            return members;
        }

        private static string GetString(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return "";
            return prop.ValueKind == JsonValueKind.Null ? "" : prop.ToString();
        }

        private static long GetLong(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return 0;
            return prop.TryGetInt64(out long v) ? v : 0;
        }

        private static int GetInt(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return 0;
            return prop.TryGetInt32(out int v) ? v : 0;
        }

        private static bool GetBool(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return false;
            return prop.ValueKind == JsonValueKind.True;
        }

        private static bool? GetNullableBool(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out JsonElement prop))
                return null;
            if (prop.ValueKind == JsonValueKind.Null)
                return null;
            return prop.ValueKind == JsonValueKind.True;
        }
    }
}
