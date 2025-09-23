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
        do {
            //2. Choose service
            Console.WriteLine("Choose your desired service: 1 for email, 2 for telephony");
            choice = Console.ReadLine();
        } while (choice != "1" && choice != "2");

        string service = choice == "2" ? "telephony" : "email";
        Console.WriteLine($"Your chosen service: {service}");

        // 2. Prompt user to paste the Forwarding URLs after they appear in ngrok output
        string input = string.Empty;
        Console.WriteLine("\nPaste the ngrok Forwarding URL lines here (then press Enter twice):");
        string allInput = "";
        while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            allInput += input + "\n";

        // 3. Extract URLs with regex
        string chosenServiceUrl = null, ssoUrl = null;
        foreach (var line in allInput.Split('\n'))
        {
            if (chosenServiceUrl != null) continue;
            else
            {
                var m = Regex.Match(line, @"Forwarding\s+(https://\S+\.ngrok-free.app)");
                if (m.Success) chosenServiceUrl = m.Groups[1].Value;
            }
            if (ssoUrl != null) continue;
            else
            {
                var m = Regex.Match(line, @"Web Interface\s+(http://\S+)");
                if (m.Success) ssoUrl = m.Groups[1].Value;
            }
        }

        if (chosenServiceUrl == null)
        {
            Console.WriteLine($"Could not find ngrok Forwarding URL for your chosen service {service}");
            return;
        }
        if (ssoUrl == null)
        {
            Console.WriteLine("Could not find ngrok Forwarding URL for SSO service.");
            return;
        }

        // 4. Encode and build API URLs
        string chosenServiceApiUrl = string.Empty;
        string ssoApiUrl = $"https://selfservice.wf-lmx.com/sso?urlEncoded={Uri.EscapeDataString(ssoUrl)}";
        if(service == "telephony" || service == "email")
        {
            // Determine service type and build appropriate API URL
            chosenServiceApiUrl = $"https://selfservice.wf-lmx.com/{service}?urlEncoded={Uri.EscapeDataString(chosenServiceUrl)}";
            Console.WriteLine($"Detected service type: {service}");
        }

        Console.WriteLine($"\nChosen Service API URL: {chosenServiceApiUrl}");
        Console.WriteLine($"SSO API URL: {ssoApiUrl}");

        // 5. Open in browser
        Process.Start(new ProcessStartInfo(chosenServiceApiUrl) { UseShellExecute = true });
        Process.Start(new ProcessStartInfo(ssoApiUrl) { UseShellExecute = true });
        //Debugging: Open ngrok web interface
        Process.Start(new ProcessStartInfo("http://localhost:4040/") { UseShellExecute = true });

        if (service.Contains("telephony"))
        {
            // Open http://sso-local.wf-lmx.com/ in an incognito Chrome window
            Process.Start(new ProcessStartInfo
            {
                FileName = "chrome",
                Arguments = "--incognito http://sso-local.wf-lmx.com/",
                UseShellExecute = true
            });
        }
    }
}