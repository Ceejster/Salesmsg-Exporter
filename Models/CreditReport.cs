namespace SalesmessageExporter.Models
{
    public class CreditReport
    {
        public string OrgName { get; set; }
        public string GeneratedAt { get; set; }
        public List<InboxCreditSummary> Inboxes { get; set; } = new();
        public int GrandTotalCredits =>
            Inboxes.Sum(i => i.TotalCredits);
    }

    public class InboxCreditSummary
    {
        public int InboxId { get; set; }
        public string InboxName { get; set; }

        // SMS: each segment = 1 credit
        public int OutboundSmsMessages { get; set; }
        public int OutboundSmsSegments { get; set; }

        // MMS: each message = 2 credits
        public int OutboundMmsMessages { get; set; }

        // Calls: each started minute = 1 credit
        public int OutboundCallCount { get; set; }
        public int OutboundCallCredits { get; set; }

        public int TotalCredits =>
            OutboundSmsSegments +
            (OutboundMmsMessages * 2) +
            OutboundCallCredits;
    }
}
