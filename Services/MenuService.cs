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
    }
}