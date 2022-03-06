using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.WindowsService
{
    public class EMailContact
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string? Provider { get; set; }
    }

    public enum SMTPEncryptionMode: int {
        TLS = 0,
        SSL = 1,
        STARTLS = 2
    }

    public class EMailProvider
    {
        public string ProviderName { get; set; }
        public string SMTPAddress { get; set; }
        public int SMTPPort { get; set; }
        public SMTPEncryptionMode SMTPEncryptionMode { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class EventLog
    {
        public string Name { get; set; }
        public string EventPath { get; set; }
        public string EventQuery { get; set; }
        public IList<string> EMailSubscribers { get; set; }
        public IList<string> SlackChannel { get; set; }
    }

    public class ConfigData
    {
        public IDictionary<string, EMailContact> Contacts { get; set; }
        public IDictionary<string, EMailProvider> Providers { get; set; }
        public IList<EventLog> EventLogs { get; set; }
    }

    public class ConfigurationHelper
    {
        public static SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        public static string GetEmptyConfig()
        {
            Console.WriteLine("Generating example configuration");
            Console.WriteLine("Please input EMail Password for EMail:");
            SecureString pwd = GetPassword();
            
            ConfigData data = new ConfigData
            {
                Contacts = new Dictionary<string, EMailContact>
                {
                    ["me@there.com"] = new EMailContact { Address = "me@icloud.com", Name = "Charles Tester", Provider = "icloud" },
                    ["me@there.com"] = new EMailContact { Address = "me@outlook.com", Name = "Charles Tester", Provider = "outlook" }
                },
                Providers = new Dictionary<string, EMailProvider>
                {
                    ["outlook"] = new EMailProvider
                    {
                        ProviderName = "outlook.com",
                        SMTPAddress = "smtp-mail.outlook.com",
                        SMTPPort = 587,
                        SMTPEncryptionMode = SMTPEncryptionMode.STARTLS,
                        Username = "senditover@outlook.com",
                        Password = "hoschiboschi"
                    }
                },
                EventLogs = new List<EventLog>
                {
                    new EventLog{ 
                        EventPath = "Microsoft-Windows-Windows Defender/Operational", 
                        EventQuery = "*", 
                        Name = "Windows-Defender-Hazards", 
                        EMailSubscribers = new List<string> { "me@there.com" }, 
                        SlackChannel = new List<string> { "test" } 
                    },
                    new EventLog{
                        EventPath = "PowerShellCore/Operational",
                        EventQuery = "*",
                        Name = "PowershellCore",
                        EMailSubscribers = new List<string> { "me@there.com" },
                        SlackChannel = new List<string> { "test" }
                    }
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);

            return jsonString;
        }
    }
}

