namespace SalesmessageExporter.Models
{
    public class MessagePageResponse
    {
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public string RawJson { get; set; } = string.Empty;
    }
}