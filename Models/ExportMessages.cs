namespace SalesmessageExporter.Models
{
    public class ExportMessage
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public string Status { get; set; }

        public string Type { get; set; }

        public string Direction { get; set; }

        public string Body { get; set; }

        public bool Automated { get; set; }

        public string SentAt { get; set; }

        public string ReceivedAt { get; set; }

        public string DeliveredAt { get; set; }

        public string FailedAt { get; set; }

        public string CreatedAt { get; set; }

        public string Source { get; set; }

        public ExportMessageRecord Record { get; set; }

        public ExportMessageContact Contact { get; set; }

        public ExportMessageUser User { get; set; }

        public List<ExportMedia> Media { get; set; }
    }

    public class ExportMessageRecord
    {
        public string RecordId { get; set; }

        public int Duration { get; set; }

        public string Status { get; set; }

        public string Url { get; set; }

        public bool IsVoicemail { get; set; }
    }

    public class ExportMessageContact
    {
        public long Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string Number { get; set; }

        public List<ExportTag> Tags { get; set; }

        public List<ExportCustomField> CustomFields { get; set; }
    }

    public class ExportCustomField
    {
        public long Id { get; set; }

        public long CustomFieldId { get; set; }

        public long ContactId { get; set; }

        public string FieldKey { get; set; }

        public long OrganizationId { get; set; }

        public string Text { get; set; }

        public decimal? Number { get; set; }

        public List<ExportCustomFieldValue> Value { get; set; }
    }

    public class ExportCustomFieldValue
    {
        public string Type { get; set; }

        public string Value { get; set; }
    }

    public class ExportMessageUser
    {
        public long Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string FormattedNumber { get; set; }
    }

    public class ExportMedia
    {
        public long Id { get; set; }

        public string ContentType { get; set; }

        public string Source { get; set; }

        public string CreatedAt { get; set; }
    }
}