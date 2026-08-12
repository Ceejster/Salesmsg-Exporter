using System.Text.Json;
using SalesmessageExporter.Config;
using SalesmessageExporter.Helpers;
using SalesmessageExporter.Models;

namespace SalesmessageExporter.Services
{
    public class ExportService
    {
        private readonly SalesmessageApiService _apiService;

        public ExportService(SalesmessageApiService apiService)
        {
            _apiService = apiService;
        }

        // Main inbox export loop — fetches open then closed conversations
        public async Task ExportInboxAsync(
            int inboxId,
            string orgName,
            int startOffset = 0,
            int startPage = 1)
        {
            Console.WriteLine($"Starting export for {orgName}");
            Console.WriteLine();

            int currentPage = startPage;

            foreach (string filter in new[] { "open", "closed", "outbound" })
            {
                Console.WriteLine($"--- Fetching {filter} conversations ---");

                int currentOffset = startOffset;
                bool hasMoreRecords = true;

                while (hasMoreRecords)
                {
                    Console.WriteLine(
                        $"Fetching Page {currentPage} " +
                        $"(Offset {currentOffset})...");

                    string rawJson =
                        await _apiService.GetConversationsPageRawAsync(
                            inboxId,
                            currentOffset,
                            filter);

                    List<ExportConversation> conversations =
                        ConversationMapper.MapConversations(rawJson);

                    string formattedJson =
                        JsonSerializer.Serialize(
                            conversations,
                            new JsonSerializerOptions { WriteIndented = true });

                    await FileExportHelper.SaveJsonPageAsync(
                        formattedJson,
                        orgName,
                        currentPage);

                    Console.WriteLine($"Saved Page {currentPage}.");

                    int returnedRecordCount = conversations.Count;

                    Console.WriteLine($"Returned records: {returnedRecordCount}");

                    hasMoreRecords = returnedRecordCount == AppSettings.DefaultPageSize;

                    Console.WriteLine($"Has more records: {hasMoreRecords}");

                    if (currentPage % AppSettings.PageCheckpoint == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Checkpoint reached at Page {currentPage}");
                        Console.WriteLine("Continue export? (Y/N)");
                        Console.Write("> ");

                        string input = Console.ReadLine() ?? "";

                        if (!input.Equals("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("Export stopped by user.");
                            return;
                        }
                    }

                    currentOffset += (AppSettings.DefaultPageSize - 1);
                    currentPage++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Inbox export completed.");
        }

        // Stage 1:
        // Collect all OPEN and CLOSED conversation IDs
        // and save them into a single JSON file.
        public async Task ExportConversationIdsAsync(
            int inboxId,
            string orgName)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Collecting conversation IDs for {orgName}...");
            Console.WriteLine();

            HashSet<long> conversationIds =
                await _apiService.GetAllConversationIdsAsync(
                    inboxId);

            Console.WriteLine();
            Console.WriteLine(
                $"Found {conversationIds.Count} unique conversations.");

            // Display IDs to user
            Console.WriteLine();
            Console.WriteLine("Conversation IDs:");
            Console.WriteLine();
            Console.WriteLine(
                $"Found {conversationIds.Count} unique conversations.");

            //If we want to display all IDs in the console, we can uncomment this line.

            //Console.WriteLine(
            //    string.Join(", ", conversationIds));

            string formattedJson =
                JsonSerializer.Serialize(
                    conversationIds,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await FileExportHelper.SaveConversationIdsAsync(
                formattedJson,
                orgName);

            Console.WriteLine();
            Console.WriteLine(
                "Conversation ID export completed.");
        }


        // Export all messages across ALL inboxes — no inbox ID needed
        public async Task ExportFullConversationHistoryAsync(
            string orgName,
            int? inboxIdFilter = null,
            string? fromDate = null)
        {
            Console.WriteLine();
            Console.WriteLine($"Starting FULL conversation export for {orgName}");

            if (inboxIdFilter.HasValue)
                Console.WriteLine($"Inbox filter: {inboxIdFilter.Value}");

            if (!string.IsNullOrEmpty(fromDate))
                Console.WriteLine($"Date filter: conversations since {fromDate} (UTC)");

            Console.WriteLine();

            // Step 1: discover all inboxes (id + name)
            Console.WriteLine("Fetching inboxes...");

            List<(int Id, string Name)> inboxes =
                await _apiService.GetAllInboxInfoAsync();

            if (inboxIdFilter.HasValue)
                inboxes = inboxes.Where(i => i.Id == inboxIdFilter.Value).ToList();

            Console.WriteLine($"Found {inboxes.Count} inbox(es).");
            Console.WriteLine();

            // Step 2: collect conversation IDs + contact info per inbox
            var conversationToInbox = new Dictionary<long, int>();
            var indexEntries        = new List<ConversationIndexEntry>();

            foreach ((int inboxId, string inboxName) in inboxes)
            {
                Console.WriteLine(
                    $"Collecting conversations for inbox {inboxId} ({inboxName})...");

                try
                {
                    List<ConversationIndexEntry> entries =
                        await _apiService.GetAllConversationInfoAsync(inboxId, inboxName, fromDate);

                    Console.WriteLine($"  {entries.Count} conversations found.");

                    foreach (ConversationIndexEntry entry in entries)
                    {
                        if (conversationToInbox.TryAdd(entry.ConversationId, inboxId))
                            indexEntries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Failed to collect inbox {inboxId} — skipping: {ex.Message}");
                }
            }

            List<long> idList = conversationToInbox.Keys.ToList();

            Console.WriteLine();
            Console.WriteLine(
                $"Total unique conversations: {idList.Count}");
            Console.WriteLine();

            // Step 3: save IDs to disk immediately as a checkpoint
            string idsJson = JsonSerializer.Serialize(
                idList,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveConversationIdsAsync(idsJson, orgName);

            Console.WriteLine("Conversation IDs saved to file.");
            Console.WriteLine();

            // Step 4: export messages and accumulate credits per inbox
            var inboxSummaries = new Dictionary<int, InboxCreditSummary>();

            foreach ((int inboxId, string inboxName) in inboxes)
            {
                inboxSummaries[inboxId] = new InboxCreditSummary
                {
                    InboxId = inboxId,
                    InboxName = inboxName
                };
            }

            int index = 1;
            int failed = 0;

            foreach (long conversationId in idList)
            {
                int inboxId =
                    conversationToInbox.TryGetValue(conversationId, out int cid)
                        ? cid : 0;

                Console.WriteLine(
                    $"[{index}/{idList.Count}] Exporting conversation {conversationId}");

                try
                {
                    (int smsMessages, int smsSegments, int mmsMessages, int callCount, int callCredits) =
                        await ExportSingleConversationAsync(
                            conversationId,
                            orgName,
                            inboxId);

                    if (inboxId > 0 &&
                        inboxSummaries.TryGetValue(inboxId, out InboxCreditSummary summary))
                    {
                        summary.OutboundSmsMessages += smsMessages;
                        summary.OutboundSmsSegments += smsSegments;
                        summary.OutboundMmsMessages += mmsMessages;
                        summary.OutboundCallCount   += callCount;
                        summary.OutboundCallCredits += callCredits;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine(
                        $"  Failed conversation {conversationId}: {ex.Message}");
                }

                index++;
            }

            Console.WriteLine();
            Console.WriteLine(
                $"FULL EXPORT COMPLETE — " +
                $"{idList.Count - failed}/{idList.Count} conversations exported.");

            if (failed > 0)
                Console.WriteLine(
                    $"{failed} conversation(s) failed — see log above.");

            // Step 5: build and save credit report
            var report = new CreditReport
            {
                OrgName = orgName,
                GeneratedAt = DateTime.UtcNow.ToString("O"),
                Inboxes = inboxSummaries.Values
                    .OrderBy(i => i.InboxId)
                    .ToList()
            };

            string reportJson = JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveCreditReportAsync(reportJson, orgName);

            Console.WriteLine();
            Console.WriteLine(
                $"Credit report saved — " +
                $"grand total: {report.GrandTotalCredits} credits.");

            // Save conversation index CSV
            await FileExportHelper.SaveConversationIndexCsvAsync(
                indexEntries, orgName);

            Console.WriteLine("Conversation index CSV saved.");
        }

        // Single-inbox overload kept for option 1 (inbox conversations export)
        public async Task ExportFullConversationHistoryAsync(int inboxId, string orgName)
        {
            Console.WriteLine();
            Console.WriteLine($"Starting FULL conversation export for {orgName}");
            Console.WriteLine();

            // Step 1: collect all conversation IDs
            HashSet<long> conversationIds =
                await _apiService.GetAllConversationIdsAsync(inboxId);

            List<long> idList = conversationIds.ToList();

            Console.WriteLine($"Total conversations: {idList.Count}");
            Console.WriteLine();

            // Step 2: save IDs to disk immediately so we have a checkpoint
            // if anything goes wrong during the export loop
            string idsJson = JsonSerializer.Serialize(
                idList,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveConversationIdsAsync(idsJson, orgName);

            Console.WriteLine("Conversation IDs saved to file.");
            Console.WriteLine();

            int index = 1;
            int failed = 0;

            // Step 3: export messages for each conversation
            foreach (long conversationId in idList)
            {
                Console.WriteLine(
                    $"[{index}/{idList.Count}] Exporting conversation {conversationId}");

                try
                {
                    await ExportSingleConversationAsync(
                        conversationId,
                        orgName,
                        inboxId);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine(
                        $"  Failed conversation {conversationId}: {ex.Message}");
                }

                index++;
            }

            Console.WriteLine();
            Console.WriteLine(
                $"FULL EXPORT COMPLETE — " +
                $"{idList.Count - failed}/{idList.Count} conversations exported.");

            if (failed > 0)
                Console.WriteLine(
                    $"{failed} conversation(s) failed — see log above.");
        }

        // Returns per-type credit counts for accumulation into InboxCreditSummary
        private async Task<(int smsMessages, int smsSegments, int mmsMessages, int callCount, int callCredits)>
            ExportSingleConversationAsync(
                long conversationId,
                string orgName,
                int inboxId = 0)
        {
            int page = 1;
            bool hasMorePages = true;

            List<ExportMessage> allMessages = new();

            while (hasMorePages)
            {
                var response =
                    await _apiService.GetConversationMessagesPageAsync(
                        conversationId,
                        page);

                List<ExportMessage> messages =
                    MessageMapper.MapMessages(
                        response.RawJson);

                allMessages.AddRange(messages);

                hasMorePages =
                    response.CurrentPage < response.TotalPages;

                page++;
            }

            int smsMessages = 0;
            int smsSegments = 0;
            int mmsMessages = 0;
            int callCount   = 0;
            int callCredits = 0;

            foreach (ExportMessage message in allMessages)
            {
                bool isReceived = string.Equals(
                    message.Status, "received",
                    StringComparison.OrdinalIgnoreCase);

                bool isFailed = string.Equals(
                    message.Status, "failed",
                    StringComparison.OrdinalIgnoreCase);

                if (isReceived || isFailed)
                    continue;

                string type = message.Type?.ToLower() ?? "";

                if (type == "sms")
                {
                    smsMessages++;
                    smsSegments +=
                        CreditCalculator.CalculateSmsSegments(
                            message.Body ?? "");
                }
                else if (type == "mms")
                {
                    mmsMessages++;
                }
                else if (type == "call")
                {
                    int duration = message.Record?.Duration ?? 0;
                    int credits  = CreditCalculator.CalculateCallCredits(duration);

                    if (credits > 0)
                    {
                        callCount++;
                        callCredits += credits;
                    }
                }
            }

            var exportObject = new
            {
                conversation_id = conversationId,
                exported_at = DateTime.UtcNow,
                message_count = allMessages.Count,
                messages = allMessages
            };

            string json =
                JsonSerializer.Serialize(
                    exportObject,
                    new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveConversationAsync(
                json,
                orgName,
                conversationId,
                inboxId);

            return (smsMessages, smsSegments, mmsMessages, callCount, callCredits);
        }

        public async Task ExportSingleConversationByIdAsync(long conversationId, string orgName)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Exporting conversation {conversationId}");

            await ExportSingleConversationAsync(
                conversationId,
                orgName);

            Console.WriteLine();
            Console.WriteLine(
                "Conversation export complete.");
        }

        public async Task ExportConversationListAsync(
    List<long> conversationIds,
    string orgName)
        {
            if (conversationIds == null || conversationIds.Count == 0)
            {
                Console.WriteLine("No conversation IDs provided.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Starting export of {conversationIds.Count} conversations...");
            Console.WriteLine();

            int index = 1;

            foreach (long conversationId in conversationIds)
            {
                Console.WriteLine(
                    $"[{index}/{conversationIds.Count}] Exporting {conversationId}");

                try
                {
                    await ExportSingleConversationAsync(
                        conversationId,
                        orgName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Failed conversation {conversationId}: {ex.Message}");
                }

                index++;
            }

            Console.WriteLine();
            Console.WriteLine("Conversation list export complete.");
        }

        // Lightweight credit report — fetches messages but saves only credit
        // detail per inbox, not full conversation files.
        public async Task GenerateCreditReportAsync(string orgName)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Generating credit report for {orgName}");
            Console.WriteLine();

            Console.WriteLine("Fetching inboxes...");

            List<(int Id, string Name)> inboxes =
                await _apiService.GetAllInboxInfoAsync();

            Console.WriteLine($"Found {inboxes.Count} inbox(es).");
            Console.WriteLine();

            var creditReport = new CreditReport
            {
                OrgName = orgName,
                GeneratedAt = DateTime.UtcNow.ToString("O")
            };

            var allIndexEntries = new List<ConversationIndexEntry>();

            foreach ((int inboxId, string inboxName) in inboxes)
            {
                Console.WriteLine(
                    $"Processing inbox {inboxId} ({inboxName})...");

                List<ConversationIndexEntry> inboxEntries =
                    await _apiService.GetAllConversationInfoAsync(inboxId, inboxName);

                allIndexEntries.AddRange(inboxEntries);

                List<long> idList = inboxEntries
                    .Select(e => e.ConversationId)
                    .Distinct()
                    .ToList();

                Console.WriteLine(
                    $"  {idList.Count} conversations");

                var summary = new InboxCreditSummary
                {
                    InboxId   = inboxId,
                    InboxName = inboxName
                };

                var detail = new InboxCreditDetail
                {
                    InboxId       = inboxId,
                    InboxName     = inboxName,
                    Conversations = new List<ConversationCreditEntry>()
                };

                int convIndex = 1;

                foreach (long conversationId in idList)
                {
                    Console.Write(
                        $"\r  [{convIndex}/{idList.Count}] " +
                        $"conversation {conversationId}     ");

                    convIndex++;

                    try
                    {
                        List<string> pages =
                            await _apiService.GetAllMessagePagesRawAsync(
                                conversationId);

                        var convEntry = new ConversationCreditEntry
                        {
                            ConversationId = conversationId,
                            Messages       = new List<MessageCreditEntry>()
                        };

                        foreach (string pageJson in pages)
                        {
                            List<ExportMessage> messages =
                                MessageMapper.MapMessages(pageJson);

                            foreach (ExportMessage message in messages)
                            {
                                bool isReceived = string.Equals(
                                    message.Status, "received",
                                    StringComparison.OrdinalIgnoreCase);

                                bool isFailed = string.Equals(
                                    message.Status, "failed",
                                    StringComparison.OrdinalIgnoreCase);

                                if (isReceived || isFailed)
                                    continue;

                                string type = message.Type?.ToLower() ?? "";
                                int credits  = 0;

                                if (type == "sms")
                                {
                                    credits = CreditCalculator.CalculateSmsSegments(
                                        message.Body ?? "");

                                    summary.OutboundSmsMessages++;
                                    summary.OutboundSmsSegments += credits;
                                }
                                else if (type == "mms")
                                {
                                    credits = 2;
                                    summary.OutboundMmsMessages++;
                                }
                                else if (type == "call")
                                {
                                    int duration = message.Record?.Duration ?? 0;
                                    credits = CreditCalculator.CalculateCallCredits(
                                        duration);

                                    if (credits > 0)
                                    {
                                        summary.OutboundCallCount++;
                                        summary.OutboundCallCredits += credits;
                                    }
                                }

                                if (credits > 0)
                                {
                                    convEntry.Messages.Add(new MessageCreditEntry
                                    {
                                        MessageId = message.Id,
                                        Type      = message.Type,
                                        SentAt    = message.SentAt,
                                        Credits   = credits
                                    });
                                }
                            }
                        }

                        convEntry.TotalCredits =
                            convEntry.Messages.Sum(m => m.Credits);

                        if (convEntry.Messages.Count > 0)
                            detail.Conversations.Add(convEntry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"\n  Failed conversation {conversationId}: " +
                            $"{ex.Message}");
                    }
                }

                Console.WriteLine();

                string detailJson = JsonSerializer.Serialize(
                    detail,
                    new JsonSerializerOptions { WriteIndented = true });

                await FileExportHelper.SaveInboxCreditDetailAsync(
                    detailJson, orgName, inboxId);

                creditReport.Inboxes.Add(summary);

                Console.WriteLine(
                    $"  Inbox {inboxId} — {summary.TotalCredits} credits " +
                    $"({summary.OutboundSmsSegments} SMS, " +
                    $"{summary.OutboundMmsMessages} MMS, " +
                    $"{summary.OutboundCallCredits} call)");

                Console.WriteLine();
            }

            string reportJson = JsonSerializer.Serialize(
                creditReport,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveCreditReportAsync(reportJson, orgName);

            Console.WriteLine();
            Console.WriteLine(
                $"Credit report complete — " +
                $"grand total: {creditReport.GrandTotalCredits} credits");

            await FileExportHelper.SaveConversationIndexCsvAsync(
                allIndexEntries, orgName);

            Console.WriteLine(
                $"Conversation index CSV saved.");

            Console.WriteLine(
                $"Files saved to ~/Salesmsg/{SanitizeName(orgName)}/");
        }

        public async Task ExportCallHistoryAsync(string orgName)
        {
            Console.WriteLine();
            Console.WriteLine($"Exporting call history for {orgName}...");
            Console.WriteLine();

            Console.WriteLine("Fetching inboxes...");

            List<(int Id, string Name)> inboxes =
                await _apiService.GetAllInboxInfoAsync();

            Console.WriteLine($"Found {inboxes.Count} inbox(es).");
            Console.WriteLine();

            var export = new CallHistoryExport
            {
                OrgName    = orgName,
                ExportedAt = DateTime.UtcNow.ToString("O")
            };

            foreach ((int inboxId, string inboxName) in inboxes)
            {
                Console.Write(
                    $"Fetching calls for inbox {inboxId} ({inboxName})...");

                var inboxHistory = new InboxCallHistory
                {
                    InboxId   = inboxId,
                    InboxName = inboxName
                };

                try
                {
                    int page     = 1;
                    bool hasMore = true;

                    while (hasMore)
                    {
                        string raw =
                            await _apiService.GetCallHistoryPageRawAsync(
                                inboxId, page);

                        (List<ExportCallEntry> calls, bool more) =
                            CallHistoryMapper.MapPage(raw);

                        foreach (ExportCallEntry call in calls)
                        {
                            inboxHistory.Calls.Add(call);

                            if (string.Equals(call.Type, "inbound",
                                    StringComparison.OrdinalIgnoreCase))
                                inboxHistory.InboundCalls++;
                            else
                                inboxHistory.OutboundCalls++;

                            if (string.Equals(call.Outcome, "missed",
                                    StringComparison.OrdinalIgnoreCase))
                                inboxHistory.MissedCalls++;
                            else if (string.Equals(call.Outcome, "answered",
                                    StringComparison.OrdinalIgnoreCase))
                                inboxHistory.AnsweredCalls++;
                        }

                        hasMore = more;
                        page++;
                    }

                    inboxHistory.TotalCalls = inboxHistory.Calls.Count;
                    Console.WriteLine($" {inboxHistory.TotalCalls} call(s).");
                }
                catch (System.Net.Http.HttpRequestException ex)
                    when (ex.StatusCode ==
                          System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine(" no access — skipping.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" failed: {ex.Message}");
                }

                export.Inboxes.Add(inboxHistory);
            }

            string json = JsonSerializer.Serialize(
                export,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveCallHistoryAsync(json, orgName);

            Console.WriteLine();
            Console.WriteLine(
                $"Call history saved — " +
                $"{export.ActiveInboxes} active, " +
                $"{export.InactiveInboxes} inactive inbox(es).");

            Console.WriteLine(
                $"Saved to ~/Salesmsg/{SanitizeName(orgName)}/CallHistory.json");
        }

        public async Task ExportMembersAsync(string orgName)
        {
            Console.WriteLine();
            Console.WriteLine($"Exporting members for {orgName}...");

            string raw = await _apiService.GetAllMembersRawAsync();

            List<ExportMember> members = MemberMapper.MapMembers(raw);

            Console.WriteLine($"Found {members.Count} member(s).");

            string json = JsonSerializer.Serialize(
                members,
                new JsonSerializerOptions { WriteIndented = true });

            await FileExportHelper.SaveMembersAsync(json, orgName);

            Console.WriteLine(
                $"Members saved to ~/Salesmsg/{SanitizeName(orgName)}/Members.json");
        }

        private static string SanitizeName(string name) =>
            System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\d\-]", "");
    }
}