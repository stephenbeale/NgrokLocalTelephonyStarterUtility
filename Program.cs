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
        string cmd = $"/K \"cd /d \"{ngrokDir}\" && ngrok.exe start email\"";
        Process.Start(new ProcessStartInfo("cmd.exe", cmd) { UseShellExecute = true });

        string choice = string.Empty;
        do
        {
            //2. Choose service
            Console.WriteLine("Choose your desired service: 1 for email, 2 for telephony");
            choice = Console.ReadLine();
        } while (choice != "1" && choice != "2");

        string service = choice == "2" ? "telephony" : "email";
        Console.WriteLine($"Your chosen service: {service}");

        // 2. Prompt user to paste the Forwarding URLs after they appear in ngrok output
        string chosenServiceUrl = string.Empty;
        string ssoUrl = string.Empty;
        do
        {
            string input = string.Empty;
            Console.WriteLine("\nPaste the ngrok Forwarding URL lines here (then press Enter twice):");
            string allInput = "";
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
                allInput += input + "\n";

            // 3. Extract URLs with regex
            foreach (var line in allInput.Split('\n'))
            {
                // Look for Forwarding URL if we haven't found it yet
                if (string.IsNullOrEmpty(chosenServiceUrl))
                {
                    var m = Regex.Match(line, @"Forwarding\s+(https://\S+\.ngrok-free\.app)");
                    if (m.Success)
                    {
                        chosenServiceUrl = m.Groups[1].Value;
                        Console.WriteLine($"Found Forwarding URL: {chosenServiceUrl}");
                    }
                }

                // Look for Web Interface URL only if telephony service and not found yet
                if (service == "telephony" && string.IsNullOrEmpty(ssoUrl))
                {
                    var m = Regex.Match(line, @"Web Interface\s+(http://\S+)");
                    if (m.Success)
                    {
                        ssoUrl = m.Groups[1].Value;
                        Console.WriteLine($"Found Web Interface URL: {ssoUrl}");
                    }
                }
            }

            if (string.IsNullOrEmpty(chosenServiceUrl))
            {
                Console.WriteLine($"Could not find ngrok Forwarding URL. Please try again.");
            }
        } while (string.IsNullOrEmpty(chosenServiceUrl));

        // Check for SSO URL if telephony
        if (service == "telephony" && string.IsNullOrEmpty(ssoUrl))
        {
            Console.WriteLine("Warning: Could not find Web Interface URL for SSO service.");
            // You might want to ask user to retry or continue without it
        }

        // 4. Encode and build API URLs
        string chosenServiceApiUrl = string.Empty;
        string ssoApiUrl = string.Empty;

        if (service == "telephony" || service == "email")
        {
            // Determine service type and build appropriate API URL
            chosenServiceApiUrl = $"https://selfservice.wf-lmx.com/{service}?urlEncoded={Uri.EscapeDataString(chosenServiceUrl)}";
            Console.WriteLine($"Detected service type: {service}");
        }

        Console.WriteLine($"\nChosen Service API URL: {chosenServiceApiUrl}");

        // 5. Open in browser
        Process.Start(new ProcessStartInfo(chosenServiceApiUrl) { UseShellExecute = true });

        //Debugging: Open ngrok web interface
        Process.Start(new ProcessStartInfo("http://localhost:4040/") { UseShellExecute = true });

        if (service == "telephony" && !string.IsNullOrEmpty(ssoUrl))
        {
            ssoApiUrl = $"https://selfservice.wf-lmx.com/sso?urlEncoded={Uri.EscapeDataString(ssoUrl)}";
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

        if (service == "email")
        {
            Console.WriteLine("\n\nEmail Sending Service - Enter the info to create and send a test email to your chosen task/lead.");

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

                string recipientNoLeadId = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}{recipientBaseAddress}";
                string recipient = $"{(productChoice == 1 ? "FECS" : "HZ-CS")}+{leadId}{recipientBaseAddress}";

                string subject = "Test Subject";
                string body = "This is the email body text.";

                // URL encode the subject and body
                string mailtoUrl = $"mailto:{recipient}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                string mailtoUrlNoLeadId = $"mailto:{recipientNoLeadId}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

                Console.WriteLine($"Creating emails with following addresses and contents: \n{mailtoUrl}\n{mailtoUrlNoLeadId}");

                Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });
                Process.Start(new ProcessStartInfo(mailtoUrlNoLeadId) { UseShellExecute = true });

                Console.WriteLine("\n\n\nDo you want to create another email? (y/n)");
                continueLoop = Console.ReadLine()?.Trim().ToLower() == "y";

            } while (continueLoop);
        }
    }
}