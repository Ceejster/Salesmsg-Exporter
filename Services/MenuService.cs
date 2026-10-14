using SalesmessageExporter.Services;

namespace SalesmessageExporter.Services
{
    public class MenuService
    {
        // Gets the user's Personal Access Token (PAT)
        public string GetPat()
        {
            Console.WriteLine("Enter your Salesmessage PAT:");
            Console.Write("> ");

            string pat = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(pat))
            {
                Console.WriteLine("PAT cannot be empty. Try again:");
                Console.Write("> ");

                pat = Console.ReadLine();
            }

            return pat.Trim();
        }

        // Gets inbox/team ID required for API calls
        public int GetInboxId()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Inbox/Team ID:");
            Console.Write("> ");

            string input = Console.ReadLine();

            while (!int.TryParse(input, out int inboxId) || inboxId <= 0)
            {
                Console.WriteLine("Invalid Inbox ID. Try again:");
                Console.Write("> ");

                input = Console.ReadLine();
            }

            return int.Parse(input);
        }

        // NEW: Organization name for file naming
        public string GetOrganizationName()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Organization Name (used for file names):");
            Console.Write("> ");

            string orgName = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(orgName))
            {
                Console.WriteLine("Organization name cannot be empty. Try again:");
                Console.Write("> ");

                orgName = Console.ReadLine();
            }

            return orgName.Trim();
        }

        // Gets starting page (logical pagination)
        public int GetStartPage(int suggestedPage)
        {
            Console.WriteLine();
            Console.WriteLine($"Start from page? (Press Enter for {suggestedPage})");
            Console.Write("> ");

            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return suggestedPage;
            }

            while (!int.TryParse(input, out int page) || page <= 0)
            {
                Console.WriteLine("Invalid page. Try again:");
                Console.Write("> ");

                input = Console.ReadLine();
            }

            return int.Parse(input);
        }

        public long GetConversationId()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Conversation ID:");
            Console.Write("> ");

            string input = Console.ReadLine();

            while (!long.TryParse(input, out long id) || id <= 0)
            {
                Console.WriteLine("Invalid Conversation ID. Try again:");
                Console.Write("> ");

                input = Console.ReadLine();
            }

            return long.Parse(input);
        }

        public string? GetDateRangeOption()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Select date range:");
                Console.WriteLine("1. Last 30 days");
                Console.WriteLine("2. Last 90 days");
                Console.WriteLine("3. Last 120 days");
                Console.WriteLine("4. Last 180 days");
                Console.WriteLine("5. Last 365 days");
                Console.WriteLine("6. All time");
                Console.Write("> ");

                switch ((Console.ReadLine() ?? "").Trim())
                {
                    case "1": return DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
                    case "2": return DateTime.UtcNow.AddDays(-90).ToString("yyyy-MM-dd");
                    case "3": return DateTime.UtcNow.AddDays(-120).ToString("yyyy-MM-dd");
                    case "4": return DateTime.UtcNow.AddDays(-180).ToString("yyyy-MM-dd");
                    case "5": return DateTime.UtcNow.AddDays(-365).ToString("yyyy-MM-dd");
                    case "6": return null;
                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        break;
                }
            }
        }

        public List<long> GetConversationIds()
        {
            Console.WriteLine();
            Console.WriteLine("Enter Conversation ID(s) separated by commas:");
            Console.Write("> ");

            string input = Console.ReadLine() ?? "";

            while (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Input cannot be empty. Try again:");
                Console.Write("> ");
                input = Console.ReadLine() ?? "";
            }

            List<long> ids = new();

            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (long.TryParse(part.Trim(), out long id))
                {
                    ids.Add(id);
                }
                else
                {
                    Console.WriteLine($"Skipping invalid ID: {part}");
                }
            }

            return ids;
        }
    }
}