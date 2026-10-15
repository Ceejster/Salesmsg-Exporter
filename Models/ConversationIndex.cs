namespace SalesmessageExporter.Models
{
    public class ConversationIndexEntry
    {
        public long ConversationId { get; set; }
        public int InboxId { get; set; }
        public string InboxName { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
    }
}
