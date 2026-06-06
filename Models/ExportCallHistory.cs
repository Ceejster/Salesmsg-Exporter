namespace SalesmessageExporter.Models
{
    // Represents one exported call history record
    public class ExportCallHistory
    {
        public ExportCallMessage Message { get; set; }

        public ExportCallConversation Conversation { get; set; }

        public ExportCallDetails Call { get; set; }

        public ExportCallContact Contact { get; set; }

        public ExportContactedUser ContactedUser { get; set; }

        public ExportCallInbox Inbox { get; set; }
    }

    // Minimal message information
    public class ExportCallMessage
    {
        public long Id { get; set; }
    }

    // Minimal conversation information
    public class ExportCallConversation
    {
        public long Id { get; set; }
    }

    // Main call details
    public class ExportCallDetails
    {
        public long Id { get; set; }

        public string Type { get; set; }

        public string Outcome { get; set; }

        public int Duration { get; set; }

        public string RecordId { get; set; }

        public string RecordUrl { get; set; }

        public int RecordDuration { get; set; }

        public string CreatedAt { get; set; }
    }

    // Contact involved in the call
    public class ExportCallContact
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string FormattedNumber { get; set; }
    }

    // User who handled or received the call
    public class ExportContactedUser
    {
        public long Id { get; set; }

        public string Name { get; set; }
    }

    // Inbox / team information
    public class ExportCallInbox
    {
        public long Id { get; set; }
    }
}