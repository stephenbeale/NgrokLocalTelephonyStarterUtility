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

        Console.WriteLine("Choose your service: 1 for Telephony, 2 for Email:");
        var service = Console.ReadLine();

        string ngrokServiceCommands = string.Empty;

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
                break;
        }

        string cmd = $"/K \"cd /d \"{ngrokDir}\" && ngrok.exe {ngrokServiceCommands} \"";
        Process.Start(new ProcessStartInfo("cmd.exe", cmd) { UseShellExecute = true });

        // 2. Prompt user to paste the Forwarding URLs after they appear in ngrok output
        Console.WriteLine("\nPaste the ngrok Forwarding URL lines here (then press Enter twice):");
        string input, allInput = "";
        while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            allInput += input + "\n";

        // 3. Extract URLs with regex
        string telephonyUrl = null, ssoUrl = null;
        foreach (var line in allInput.Split('\n'))
        {
            if (telephonyUrl == null)
            {
                var m = Regex.Match(line, @"Forwarding\s+(https://\S+\.ngrok-free\.app)");
                if (m.Success) telephonyUrl = m.Groups[1].Value;
            }
            if (ssoUrl == null)
            {
                var m = Regex.Match(line, @"Forwarding\s+(tcp://\S+)");
                if (m.Success) ssoUrl = m.Groups[1].Value;
            }
        }

        if (telephonyUrl == null || ssoUrl == null)
        {
            Console.WriteLine("Could not find both required ngrok Forwarding URLs.");
            return;
        }

        // 4. Encode and build API URLs
        string telephonyApiUrl = $"https://selfservice.wf-lmx.com/telephony?urlEncoded={Uri.EscapeDataString(telephonyUrl)}";
        string ssoApiUrl = $"https://selfservice.wf-lmx.com/sso?urlEncoded={Uri.EscapeDataString(ssoUrl)}";

        Console.WriteLine($"\nTelephony API URL: {telephonyApiUrl}");
        Console.WriteLine($"SSO API URL: {ssoApiUrl}");

        // 5. Open in browser
        Process.Start(new ProcessStartInfo(telephonyApiUrl) { UseShellExecute = true });
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
