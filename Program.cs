using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

class Program
{
    public static void Main()
    {
        // 1. Run ngrok in a new cmd window so user can see and interact
        string ngrokExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ngrok", "ngrok.exe");
        string ngrokDir = Path.GetDirectoryName(ngrokExe);

        string service = null;
        string ngrokServiceCommands = string.Empty;

        // Loop until valid service selection
        do
        {
            Console.WriteLine("Choose your service: 1 for Telephony, 2 for Email:");
            service = Console.ReadLine();

            switch (service)
            {
                case "1":
                    Console.WriteLine("Telephony service selected");
                    ngrokServiceCommands = "start telephony sso";
                    break;
                case "2":
                    Console.WriteLine("Email service selected");
                    ngrokServiceCommands = "start email";
                    break;
                default:
                    Console.WriteLine("Invalid selection. Please choose 1 or 2.");
                    ngrokServiceCommands = string.Empty; // Reset to continue loop
                    break;
            }
        } while (ngrokServiceCommands.Length == 0);

        string cmd = $"/K \"cd /d \"{ngrokDir}\" && ngrok.exe {ngrokServiceCommands} \"";
        Process.Start(new ProcessStartInfo("cmd.exe", cmd) { UseShellExecute = true });

        string serviceUrl = null, ssoUrl = null;

        // Loop until we have the required URLs
        do
        {
            // 2. Prompt user to paste the Forwarding URLs after they appear in ngrok output
            Console.WriteLine("\nPaste the ngrok Forwarding URL lines here (then press Enter twice):");

            string input, allInput = "";
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
                allInput += input + "\n";

            // 3. Extract URLs with regex
            serviceUrl = null;
            ssoUrl = null;

            foreach (var line in allInput.Split('\n'))
            {
                if (serviceUrl == null)
                {
                    var m = Regex.Match(line, @"Forwarding\s+(https://\S+\.ngrok-free\.app)");
                    if (m.Success) serviceUrl = m.Groups[1].Value;
                }

                // Only look for SSO URL if telephony was selected
                if (service == "1" && ssoUrl == null)
                {
                    var m = Regex.Match(line, @"Forwarding\s+(http://localhost:\d+)");
                    if (m.Success) ssoUrl = m.Groups[1].Value;
                }
            }

            // Check if we have all required URLs
            if (service == "1") // Telephony requires both URLs
            {
                if (serviceUrl == null || ssoUrl == null)
                {
                    Console.WriteLine($"Could not find required Forwarding URLs. Need both telephony and SSO URLs.");
                    Console.WriteLine($"Found: telephonyUrl={serviceUrl ?? "missing"}, ssoUrl={ssoUrl ?? "missing"}");
                    Console.WriteLine("Please try again.");
                }
            }
            else if (service == "2") // Email only requires service URL
            {
                if (serviceUrl == null)
                {
                    Console.WriteLine("Could not find Email Forwarding URL. Please try again.");
                }
            }

        } while ((service == "1" && (serviceUrl == null || ssoUrl == null)) ||
                 (service == "2" && serviceUrl == null));

        // 4. Encode and build API URLs
        // 5. Open in browser
        if (service == "1") // Telephony
        {
            string telephonyApiUrl = $"https://selfservice.wf-lmx.com/telephony?urlEncoded={Uri.EscapeDataString(serviceUrl)}";
            Console.WriteLine($"\nTelephony API URL: {telephonyApiUrl}");
            Process.Start(new ProcessStartInfo(telephonyApiUrl) { UseShellExecute = true });

            string ssoApiUrl = $"https://selfservice.wf-lmx.com/sso?urlEncoded={Uri.EscapeDataString(ssoUrl)}";
            Console.WriteLine($"SSO API URL: {ssoApiUrl}");
            Process.Start(new ProcessStartInfo(ssoApiUrl) { UseShellExecute = true });

            // Open http://sso-local.wf-lmx.com/ in an incognito Chrome window
            Process.Start(new ProcessStartInfo
            {
                FileName = "chrome",
                Arguments = "--incognito http://sso-local.wf-lmx.com/",
                UseShellExecute = true
            });
        }
        else if (service == "2") // Email
        {
            string emailApiUrl = $"https://selfservice.wf-lmx.com/email?urlEncoded={Uri.EscapeDataString(serviceUrl)}";
            Console.WriteLine($"\nEmail API URL: {emailApiUrl}");
            Process.Start(new ProcessStartInfo(emailApiUrl) { UseShellExecute = true });

            Process.Start(new ProcessStartInfo("http://localhost:4040/inspect/http") { UseShellExecute = true });

            string recipientBaseAddress = "@mail-local.wf-lmx.com";

            int productChoice;

            bool continueLoop = true;

            do
            {
                do
                {
                    Console.WriteLine("Select a product: 1 = FE-CS, 2 = HZ-CS");
                } while (!int.TryParse(Console.ReadLine(), out productChoice) || (productChoice != 1 && productChoice != 2));

                int leadId;
                string leadInput;
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

                //Check these in the viewProduct page - they may be configured differently to as shown below.
                string recipient = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}+{leadId}{recipientBaseAddress}";
                string recipientNoLeadId = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}{recipientBaseAddress}";

                string subject = "Test Subject";
                string body = "This is the email body text.";

                // URL encode the subject and body
                string mailtoUrl = $"mailto:{recipient}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                string mailtoUrlNoLead = $"mailto:{recipientNoLeadId}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

                Console.WriteLine($"Creating email with following contents: \n{mailtoUrl}");

                Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });
                Process.Start(new ProcessStartInfo(mailtoUrlNoLead) { UseShellExecute = true });

                Console.WriteLine("\n\n\nDo you want to create another email? (y/n)");
                continueLoop = Console.ReadLine()?.Trim().ToLower() == "y";

            } while (continueLoop);

        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}