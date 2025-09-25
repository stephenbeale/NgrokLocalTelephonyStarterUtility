using System.Diagnostics;
using System.Text.RegularExpressions;

class Program
{
    public static void Main()
    {
        Console.WriteLine("\n\nEmail Sending Service - Enter the info to create and send a test email to your chosen task/lead.");

        string recipientBaseAddress = "@mail-local.wf-lmx.com";

        int productChoice;
        int emailWithId;

        bool continueLoop = true;

        do
        {
            do
            {
                Console.WriteLine("Select a product: 1 = FE-CS, 2 = HZ-CS");
            } while (!int.TryParse(Console.ReadLine(), out productChoice) || (productChoice != 1 && productChoice != 2));

            do
            {
                Console.WriteLine("Create the email with or without the lead ID? You only need the lead ID if you have an existing task.\n 1 = No Lead ID\n 2 = With lead ID.");
            } while (!int.TryParse(Console.ReadLine(), out emailWithId) || (emailWithId != 1 && emailWithId != 2));

            int leadId;
            string leadInput;
            string recipient = string.Empty;

            if (emailWithId == 1)
            {
                Console.WriteLine("Creating email without lead ID.");
                recipient = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}{recipientBaseAddress}";
            }

            if (emailWithId == 2)
            {
                do
                {
                    Console.WriteLine("Enter Lead ID (or paste URL):");
                    leadInput = Console.ReadLine();
                    var match = Regex.Match(leadInput, @"(\d+)$");
                    if (match.Success)
                    {
                        leadInput = match.Groups[1].Value; // Extract just the numbers
                    }
                } while (!int.TryParse(leadInput, out leadId));

                Console.WriteLine("Creating email with lead ID.");
                recipient = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}+{leadId}{recipientBaseAddress}";
            }

            string subject = "Test Subject";
            string body = "This is the email body text.";

            // URL encode the subject and body
            string mailtoUrl = $"mailto:{recipient}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

            Console.WriteLine($"Creating email in Outlook with following address and contents: \n{mailtoUrl}");

            Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });

            Console.WriteLine("\n\n\nDo you want to create another email? (y/n)");
            continueLoop = Console.ReadLine()?.Trim().ToLower() == "y";

        } while (continueLoop);
    }
}