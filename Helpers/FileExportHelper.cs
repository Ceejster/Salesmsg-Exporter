using System.Text.RegularExpressions;

namespace SalesmessageExporter.Helpers
{
    public static class FileExportHelper
    {
        // Folder where exports are stored
        private static readonly string ExportFolder = "Exports";

        // Saves one formatted JSON export page to disk
        public static async Task SaveJsonPageAsync(
            string json,
            string orgName,
            int pageNumber)
        {
            // Ensure export folder exists
            Directory.CreateDirectory(ExportFolder);

            // Remove invalid filename characters
            string safeOrgName =
                SanitizeFileName(orgName);

            // Example:
            // AktivateInboxPage1.json
            string fileName =
                $"{safeOrgName}InboxPage{pageNumber}.json";

            // Full path to save file
            string fullPath =
                Path.Combine(ExportFolder, fileName);

            // Write JSON to disk
            await File.WriteAllTextAsync(
                fullPath,
                json);
        }

        // Removes invalid Windows filename characters
        private static string SanitizeFileName(
            string fileName)
        {
            // Replace anything not valid for filenames
            return Regex.Replace(
                fileName,
                @"[^\w\d\-]",
                "");
        }
    }
}