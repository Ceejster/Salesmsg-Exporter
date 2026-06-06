using System.Text.Json;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class ConversationMapper
    {
        // Entry point: converts raw JSON array into clean export models
        public static List<ExportConversation> MapConversations(string json)
        {
            List<ExportConversation> conversations = new();

            // Parse raw JSON safely into a DOM structure
            using JsonDocument doc = JsonDocument.Parse(json);

            // Ensure root is an array before processing
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return conversations;
            }

            // Loop through each conversation item in the array
            foreach (JsonElement item in doc.RootElement.EnumerateArray())
            {
                ExportConversation conversation = new ExportConversation
                {
                    Id = GetLong(item, "id"),
                    InboxId = GetLong(item, "inbox_id"),
                    ContactId = GetLong(item, "contact_id"),
                    UserId = GetLong(item, "user_id"),

                    StartedAt = GetString(item, "started_at"),
                    ClosedAt = GetString(item, "closed_at"),
                    LastMessageAt = GetString(item, "last_message_at"),

                    // Nested objects are mapped separately
                    RecentMessage = MapRecentMessage(item),
                    Owner = MapOwner(item),
                    Number = MapNumber(item),
                    Contact = MapContact(item)
                };

                conversations.Add(conversation);
            }

            return conversations;
        }

        // Maps recent message safely (handles null objects)
        private static ExportRecentMessage MapRecentMessage(JsonElement item)
        {
            if (!item.TryGetProperty("recent_message", out JsonElement recentMessage))
            {
                return null;
            }

            // IMPORTANT: handle JSON null explicitly
            if (recentMessage.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (recentMessage.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new ExportRecentMessage
            {
                Id = GetLong(recentMessage, "id"),
                Body = GetString(recentMessage, "body"),
                Type = GetString(recentMessage, "type"),
                SentAt = GetString(recentMessage, "sent_at"),
                ReceivedAt = GetString(recentMessage, "received_at"),
                Status = GetString(recentMessage, "status"),
                Duration = GetInt(recentMessage, "duration")
            };
        }

        // Maps owner object safely
        private static ExportOwner MapOwner(JsonElement item)
        {
            if (!item.TryGetProperty("owner", out JsonElement owner))
                return null;

            if (owner.ValueKind != JsonValueKind.Object)
                return null;

            return new ExportOwner
            {
                Id = GetLong(owner, "id"),
                FullName = GetString(owner, "full_name"),
                FormattedNumber = GetString(owner, "formatted_number"),
                Email = GetString(owner, "email")
            };
        }

        // Maps number object safely
        private static ExportNumber MapNumber(JsonElement item)
        {
            if (!item.TryGetProperty("number", out JsonElement number))
                return null;

            if (number.ValueKind != JsonValueKind.Object)
                return null;

            return new ExportNumber
            {
                Id = GetLong(number, "id"),
                FormattedNumber = GetString(number, "formatted_number")
            };
        }

        // Maps contact object safely
        private static ExportContact MapContact(JsonElement item)
        {
            if (!item.TryGetProperty("contact", out JsonElement contact))
                return null;

            if (contact.ValueKind != JsonValueKind.Object)
                return null;

            return new ExportContact
            {
                Id = GetLong(contact, "id"),
                FullName = GetString(contact, "full_name"),
                Email = GetString(contact, "email"),
                FormattedNumber = GetString(contact, "formatted_number"),
                CarrierName = GetString(contact, "carrier_name"),
                Source = GetString(contact, "source"),
                CreatedAt = GetString(contact, "created_at"),
                Tags = MapTags(contact)
            };
        }

        // Maps tags array safely
        private static List<ExportTag> MapTags(JsonElement contact)
        {
            List<ExportTag> tags = new();

            if (!contact.TryGetProperty("tags", out JsonElement tagsElement))
                return tags;

            if (tagsElement.ValueKind != JsonValueKind.Array)
                return tags;

            foreach (JsonElement tag in tagsElement.EnumerateArray())
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

        // Safe string extraction helper
        private static string GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
                return string.Empty;

            return property.ValueKind == JsonValueKind.Null
                ? string.Empty
                : property.ToString();
        }

        // Safe long extraction helper
        private static long GetLong(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
                return 0;

            return property.TryGetInt64(out long value) ? value : 0;
        }

        // Safe int extraction helper
        private static int GetInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
                return 0;

            return property.TryGetInt32(out int value) ? value : 0;
        }
    }
}