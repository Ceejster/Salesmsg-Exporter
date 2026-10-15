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

        // 500 read req/hr = 1 per 7.2s — use 8s for safe headroom
        public const int DelayBetweenExportsMs = 8000;

        // Wait 60s after a 429 to let the rate limit window recover
        public const int RateLimitRetrySeconds = 60;

        // -------------------------
        // ENDPOINTS
        // -------------------------

        public const string ConversationsEndpoint = "conversations";

        public const string CallHistoryEndpoint = "call-history";

        public const string MessagesEndpoint = "messages";

        public const string ConversationExportEndpoint = "conversations";

        public const string InboxesEndpoint = "teams";

        public const string UsersEndpoint = "users";

        public const string TokenRefreshEndpoint =
            "https://api.salesmessage.com/pub/v2.3/oauth/personal-token/refresh";

        public const int TokenRefreshWarningMinutes  = 30;
        public const int TokenRefreshThresholdMinutes = 15;
    }
}