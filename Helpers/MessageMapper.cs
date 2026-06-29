using System.Text.Json;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class MessageMapper
    {
        public static List<ExportMessage> MapMessages(string json)
        {
            List<ExportMessage> messages = new();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return messages;

            if (!doc.RootElement.TryGetProperty("data", out JsonElement data))
                return messages;

            foreach (JsonElement item in data.EnumerateArray())
            {
                messages.Add(new ExportMessage
                {
                    Id = GetLong(item, "id"),
                    ConversationId = GetLong(item, "conversation_id"),
                    Status = GetString(item, "status"),
                    Type = GetString(item, "type"),
                    Direction = GetString(item, "direction"),
                    Body = GetString(item, "body_raw") is { Length: > 0 } raw
                        ? raw
                        : GetString(item, "body"),
                    Automated = GetBool(item, "automated"),
                    SentAt = GetString(item, "sent_at"),
                    ReceivedAt = GetString(item, "received_at"),
                    DeliveredAt = GetString(item, "delivered_at"),
                    FailedAt = GetString(item, "failed_at"),
                    CreatedAt = GetString(item, "created_at"),
                    Source = GetString(item, "source"),

                    Record = MapRecord(item),
                    Contact = MapContact(item),
                    User = MapUser(item),
                    Media = MapMedia(item)
                });
            }

            return messages;
        }

        private static ExportMessageRecord MapRecord(JsonElement item)
        {
            if (!item.TryGetProperty("record", out JsonElement record) ||
                record.ValueKind != JsonValueKind.Object)
                return null;

            return new ExportMessageRecord
            {
                RecordId = GetString(record, "record_id"),
                Duration = GetInt(record, "duration"),
                Status = GetString(record, "status"),
                Url = GetString(record, "url"),
                IsVoicemail = GetBool(record, "is_voicemail")
            };
        }

        private static ExportMessageContact MapContact(JsonElement item)
        {
            if (!item.TryGetProperty("contact", out JsonElement contact) ||
                contact.ValueKind != JsonValueKind.Object)
                return null;

            return new ExportMessageContact
            {
                Id = GetLong(contact, "id"),
                FullName = GetString(contact, "full_name"),
                Email = GetString(contact, "email"),
                Number = GetString(contact, "formatted_number"),
                Tags = MapContactTags(contact),
                CustomFields = new List<ExportCustomField>()
            };
        }

        private static List<ExportTag> MapContactTags(JsonElement contact)
        {
            List<ExportTag> tags = new();

            if (!contact.TryGetProperty("tags", out JsonElement tagsEl) ||
                tagsEl.ValueKind != JsonValueKind.Array)
                return tags;

            foreach (JsonElement tag in tagsEl.EnumerateArray())
            {
                if (tag.ValueKind != JsonValueKind.Object)
                    continue;

                tags.Add(new ExportTag
                {
                    Id = GetLong(tag, "id"),
                    Name = GetString(tag, "name"),
                    Label = GetString(tag, "label"),
                    CreatedAt = GetString(tag, "created_at")
                });
            }

            return tags;
        }

        private static ExportMessageUser MapUser(JsonElement item)
        {
            if (!item.TryGetProperty("user", out JsonElement user) ||
                user.ValueKind != JsonValueKind.Object)
                return null;

            long userId = GetLong(user, "id");

            if (userId == 0)
                return null;

            return new ExportMessageUser
            {
                Id = userId,
                FullName = GetString(user, "full_name"),
                Email = GetString(user, "email"),
                FormattedNumber = GetString(user, "formatted_number")
            };
        }


        private static List<ExportMedia> MapMedia(JsonElement item)
        {
            List<ExportMedia> media = new();

            if (!item.TryGetProperty("media", out JsonElement mediaArray))
                return media;

            if (mediaArray.ValueKind != JsonValueKind.Array)
                return media;

            foreach (JsonElement m in mediaArray.EnumerateArray())
            {
                media.Add(new ExportMedia
                {
                    Id = GetLong(m, "id"),
                    ContentType = GetString(m, "content_type"),
                    Source = GetString(m, "source"),
                    CreatedAt = GetString(m, "created_at")
                });
            }

            return media;
        }

        private static string GetString(JsonElement e, string key)
            => e.TryGetProperty(key, out var p) && p.ValueKind != JsonValueKind.Null
                ? p.ToString()
                : "";

        private static long GetLong(JsonElement e, string key)
            => e.TryGetProperty(key, out var p) && p.TryGetInt64(out long v) ? v : 0;

        private static int GetInt(JsonElement e, string key)
            => e.TryGetProperty(key, out var p) && p.TryGetInt32(out int v) ? v : 0;

        private static bool GetBool(JsonElement e, string key)
            => e.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.True;
    }
}