using Salesmsg_Exporter.Services;

namespace SalesmessageExporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Salesmessage Exporter ===");
            Console.WriteLine();

            MenuService menuService = new MenuService();

            string pat = menuService.GetPAT();

            int userChoice = menuService.GetMenuSelection();

            switch (userChoice)
            {
                case 1:
                    Console.WriteLine();
                    Console.WriteLine("Starting Inbox Export...");
                    break;

                case 2:
                    Console.WriteLine();
                    Console.WriteLine("Starting Voice Log Export...");
                    break;

                default:
                    Console.WriteLine("Invalid selection.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}