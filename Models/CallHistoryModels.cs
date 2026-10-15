namespace SalesmessageExporter.Models
{
    public class CallHistoryExport
    {
        public string OrgName { get; set; }
        public string ExportedAt { get; set; }
        public int ActiveInboxes => Inboxes.Count(i => i.TotalCalls > 0);
        public int InactiveInboxes => Inboxes.Count(i => i.TotalCalls == 0);
        public List<InboxCallHistory> Inboxes { get; set; } = new();
    }

    public class InboxCallHistory
    {
        public int InboxId { get; set; }
        public string InboxName { get; set; }
        public int TotalCalls { get; set; }
        public int InboundCalls { get; set; }
        public int OutboundCalls { get; set; }
        public int MissedCalls { get; set; }
        public int AnsweredCalls { get; set; }
        public List<ExportCallEntry> Calls { get; set; } = new();
    }

    public class ExportCallEntry
    {
        public string Type { get; set; }
        public string Outcome { get; set; }
        public int Duration { get; set; }
        public string CreatedAt { get; set; }
        public string ContactName { get; set; }
        public string ContactNumber { get; set; }
        public string HandledBy { get; set; }
        public string RecordUrl { get; set; }
    }
}
