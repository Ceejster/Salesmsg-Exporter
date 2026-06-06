namespace SalesmessageExporter.Models
{
    public class ExportConversation
    {
        public long Id { get; set; }

        public long InboxId { get; set; }

        public long ContactId { get; set; }

        public long UserId { get; set; }

        public string StartedAt { get; set; }

        public string ClosedAt { get; set; }

        public string LastMessageAt { get; set; }

        public ExportRecentMessage RecentMessage { get; set; }

        public ExportOwner Owner { get; set; }

        public ExportNumber Number { get; set; }

        public ExportContact Contact { get; set; }
    }

    public class ExportRecentMessage
    {
        public long Id { get; set; }

        public string Body { get; set; }

        public string Type { get; set; }

        public string SentAt { get; set; }

        public string ReceivedAt { get; set; }

        public string Status { get; set; }

        public int Duration { get; set; }
    }

    public class ExportOwner
    {
        public long Id { get; set; }

        public string FullName { get; set; }

        public string FormattedNumber { get; set; }

        public string Email { get; set; }
    }

    public class ExportNumber
    {
        public long Id { get; set; }

        public string FormattedNumber { get; set; }
    }

    public class ExportContact
    {
        public long Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string FormattedNumber { get; set; }

        public string CarrierName { get; set; }

        public string Source { get; set; }

        public string CreatedAt { get; set; }

        public List<ExportTag> Tags { get; set; }
    }

    public class ExportTag
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Label { get; set; }

        public string CreatedAt { get; set; }
    }
}