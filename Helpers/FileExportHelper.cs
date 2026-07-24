using System.Text;
using System.Text.RegularExpressions;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class FileExportHelper
    {
        // Root export folder: ~/Salesmsg/
        private static readonly string ExportRoot =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Salesmsg");

        // Saves one formatted JSON inbox-conversations page to disk
        public static async Task SaveJsonPageAsync(
            string json,
            string orgName,
            int pageNumber)
        {
            string folder = GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, $"InboxPage{pageNumber}.json"),
                json);
        }

        // Saves all discovered conversation IDs into a single JSON file
        public static async Task SaveConversationIdsAsync(
            string json,
            string orgName)
        {
            string folder = GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, "ConversationIds.json"),
                json);
        }

        // Saves a single conversation's messages
        // When inboxId > 0 the file is placed in an Inbox_{id} subfolder
        public static async Task SaveConversationAsync(
            string json,
            string orgName,
            long conversationId,
            int inboxId = 0)
        {
            string folder = inboxId > 0
                ? GetInboxFolder(orgName, inboxId)
                : GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, $"Conversation_{conversationId}.json"),
                json);
        }

        // Saves the member/user export as JSON
        public static async Task SaveMembersAsync(string json, string orgName)
        {
            string folder = GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, "Members.json"),
                json);
        }

        // Saves a CSV mapping conversation IDs to contact identifiers
        public static async Task SaveConversationIndexCsvAsync(
            IEnumerable<ConversationIndexEntry> entries,
            string orgName)
        {
            string folder = GetOrgFolder(orgName);

            var sb = new StringBuilder();
            sb.AppendLine(
                "conversation_id,inbox_id,inbox_name,contact_name,phone,email");

            foreach (ConversationIndexEntry entry in entries)
            {
                sb.AppendLine(
                    $"{entry.ConversationId}," +
                    $"{entry.InboxId}," +
                    $"{CsvEscape(entry.InboxName)}," +
                    $"{CsvEscape(entry.ContactName)}," +
                    $"{CsvEscape(entry.ContactPhone)}," +
                    $"{CsvEscape(entry.ContactEmail)}");
            }

            await File.WriteAllTextAsync(
                Path.Combine(folder, "ConversationIndex.csv"),
                sb.ToString());
        }

        // Saves the per-inbox message-level credit detail
        public static async Task SaveInboxCreditDetailAsync(
            string json,
            string orgName,
            int inboxId)
        {
            string folder = GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, $"Inbox_{inboxId}_CreditDetail.json"),
                json);
        }

        // Saves the overall credit usage summary
        public static async Task SaveCreditReportAsync(
            string json,
            string orgName)
        {
            string folder = GetOrgFolder(orgName);

            await File.WriteAllTextAsync(
                Path.Combine(folder, "CreditReport.json"),
                json);
        }

        // Returns ~/Salesmsg/{orgName}/Inbox_{inboxId}/ and ensures it exists
        private static string GetInboxFolder(string orgName, int inboxId)
        {
            string folder = Path.Combine(
                GetOrgFolder(orgName),
                $"Inbox_{inboxId}");

            Directory.CreateDirectory(folder);

            return folder;
        }

        // Returns ~/Salesmsg/{orgName}/ and ensures it exists
        private static string GetOrgFolder(string orgName)
        {
            string folder = Path.Combine(
                ExportRoot,
                SanitizeFileName(orgName));

            Directory.CreateDirectory(folder);

            return folder;
        }

        private static string SanitizeFileName(string fileName)
        {
            return Regex.Replace(fileName, @"[^\w\d\-]", "");
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}
