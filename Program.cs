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

        // 2. Prompt user to paste the Forwarding URLs after they appear in ngrok output
        Console.WriteLine("\nPaste the ngrok Forwarding URL lines here (then press Enter twice):");
        string input, allInput = "";
        while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            allInput += input + "\n";

        // 3. Extract URLs with regex
        string chosenServiceUrl = null, ssoUrl = null;
        foreach (var line in allInput.Split('\n'))
        {
            if (chosenServiceUrl == null)
            {
                var m = Regex.Match(line, @"Forwarding\s+(https://\S+\.ngrok-free\.app)");
                if (m.Success) chosenServiceUrl = m.Groups[1].Value;
            }
            if (ssoUrl == null)
            {
                var m = Regex.Match(line, @"Forwarding\s+(tcp://\S+)");
                if (m.Success) ssoUrl = m.Groups[1].Value;
            }
        }

        if (chosenServiceUrl == null)
        {
            Console.WriteLine("Could not find ngrok Forwarding URL for your chosen service (e.g. email or telephony).");
            return;
        }
        if (ssoUrl == null)
        {
            Console.WriteLine("Could not find ngrok Forwarding URL for SSO service.");
            return;
        }

        // 4. Encode and build API URLs
        string chosenServiceApiUrl;
        string ssoApiUrl = $"https://selfservice.wf-lmx.com/sso?urlEncoded={Uri.EscapeDataString(ssoUrl)}";

        // Determine service type and build appropriate API URL
        if (chosenServiceUrl.Contains("email") || chosenServiceUrl.Contains("8080"))
        {
            chosenServiceApiUrl = $"https://selfservice.wf-lmx.com/email?urlEncoded={Uri.EscapeDataString(chosenServiceUrl)}";
            Console.WriteLine("Detected service type: Email");
        }
        else if (chosenServiceUrl.Contains("telephony") || chosenServiceUrl.Contains("8081"))
        {
            chosenServiceApiUrl = $"https://selfservice.wf-lmx.com/telephony?urlEncoded={Uri.EscapeDataString(chosenServiceUrl)}";
            Console.WriteLine("Detected service type: Telephony");
        }
        else
        {
            // Default to email since we started ngrok with "email"
            chosenServiceApiUrl = $"https://selfservice.wf-lmx.com/email?urlEncoded={Uri.EscapeDataString(chosenServiceUrl)}";
            Console.WriteLine("Service type not detected, defaulting to Email");
        }

        Console.WriteLine($"\nChosen Service API URL: {chosenServiceApiUrl}");
        Console.WriteLine($"SSO API URL: {ssoApiUrl}");

        // 5. Open in browser
        Process.Start(new ProcessStartInfo(chosenServiceApiUrl) { UseShellExecute = true });
        Process.Start(new ProcessStartInfo(ssoApiUrl) { UseShellExecute = true });

        // Open http://sso-local.wf-lmx.com/ in an incognito Chrome window
        Process.Start(new ProcessStartInfo
        {
            FileName = "chrome",
            Arguments = "--incognito http://sso-local.wf-lmx.com/",
            UseShellExecute = true
        });
    }
}