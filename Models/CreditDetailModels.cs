namespace SalesmessageExporter.Models
{
    public class InboxCreditDetail
    {
        public int InboxId { get; set; }
        public string InboxName { get; set; }
        public List<ConversationCreditEntry> Conversations { get; set; } = new();
        public int TotalCredits => Conversations.Sum(c => c.TotalCredits);
    }

    public class ConversationCreditEntry
    {
        public long ConversationId { get; set; }
        public int TotalCredits { get; set; }
        public List<MessageCreditEntry> Messages { get; set; } = new();
    }

    public class MessageCreditEntry
    {
        public long MessageId { get; set; }
        public string Type { get; set; }
        public string SentAt { get; set; }
        public int Credits { get; set; }
    }
}
