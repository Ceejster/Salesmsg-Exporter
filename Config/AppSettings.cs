namespace SalesmessageExporter.Config
{
    public static class AppSettings
    {
        // Base API
        public const string BaseUrl = "https://api.salesmessage.com/pub/v2.3/";

        // Pagination
        public const int DefaultPageSize = 250;

        // Safety checkpoint (prevents runaway exports)
        public const int PageCheckpoint = 5000;

        // Http timeout (seconds) — set high for large bulk exports
        public const int HttpTimeoutSeconds = 120;

        // Retry settings for transient failures
        public const int MaxRetryAttempts = 3;
        public const int RetryDelayBaseSeconds = 5;

        // -------------------------
        // ENDPOINTS
        // -------------------------

        public const string ConversationsEndpoint = "conversations";

        public const string CallHistoryEndpoint = "call-history";

        public const string MessagesEndpoint = "messages";

        public const string ConversationExportEndpoint = "conversations";

        public const string InboxesEndpoint = "teams";

        public const string UsersEndpoint = "users";
    }
}