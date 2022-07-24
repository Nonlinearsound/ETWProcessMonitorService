using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Http;
using MimeKit;
using MailKit.Net.Smtp;
using App.WindowsService;
using MailKit.Security;

namespace App.WindowsService
{
    /// <summary>
    /// This class holds all information of the threaded Windows Event Gathering Process 
    /// 
    /// TODO:   Add a ring buffer for gathered event information that needs to be written to disk and sent to subscribers
    ///         Right now every information is being sent right away that will make data lost if events are coming in faster
    ///         than they can be sent. We need some sort of buffer to be filled up if lots of data come in.
    /// </summary>

    public class EventLogReader
    {
        public EventLogQuery query { get; set; }
        public EventLogWatcher watcher { get; set; }
        public int priority { get; set; }
        public string queryString { get; set; }
        public EventLog? eventLog { get; set; }
        public ConfigData ConfigData { get; set; }


        /// <summary>
        /// Holds the path to the Windows event provider, for instance "Microsoft-Windows-Windows Defender/Operational"
        /// </summary>
        /// <param name="eventPath"></param>
        public string eventPath { get; set; }

        /// <summary>
        /// Setting up everything to gather events.
        /// Do not yet activate the Watcher yet. This functionality is exposed for the user code.
        /// </summary>
        /// <param name="_eventPath"></param>
        /// <param name="_queryString"></param>
        public EventLogReader(string _eventPath, string _queryString, EventLog? configEventLog, ConfigData configData)
        {
            eventPath = _eventPath;
            queryString = _queryString;
            eventLog = configEventLog;
            ConfigData = configData;
            query = new EventLogQuery(eventPath, PathType.LogName, queryString);
            watcher = new EventLogWatcher(query);
            watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(handleEvent);
        }

        /// <summary>
        /// This function handles an event that has been gathered by the Watcher.
        /// TODO:   We should put the events data into a buffer here to be processed in a timed thread somewhere else.
        ///         At the moment we are just processing it here just to test base functionality.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="arg"></param>
        public static async void handleEvent(object obj, EventRecordWrittenEventArgs arg)
        {
            if (arg.EventRecord != null)
            {
                Console.WriteLine("Event {0} in {1}: {2}", arg.EventRecord.Id, arg.EventRecord.LogName, arg.EventRecord.FormatDescription());
                SlackPoster poster = new SlackPoster();
                var response = await poster.SendMessageAsync(arg.EventRecord.FormatDescription());
                if (response != null && !response.IsSuccessStatusCode)
                {
                    Console.WriteLine(response.StatusCode.ToString());
                    Console.WriteLine(response.ReasonPhrase.ToString());
                }

                MailClient mailClient = new MailClient();
                mailClient.SendMail("nonlinearsound@outlook.com", "nonlinearsound@outlook.com", "Testmail", "Testmail Joho!", "outlook");
            }
            else
            {
                Console.WriteLine("The event instance was null.");
            }
        }

        

        public void Enable()
        {
            watcher.Enabled = true;
        }

        public void Disable()
        {
            watcher.Enabled = false;
        }
    }
}

public class SlackPoster
{
    private readonly HttpClient _httpClient = new HttpClient { };

    public async Task<HttpResponseMessage> SendMessageAsync(string sMessage)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Post, "https://hooks.slack.com/services/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"))
        {
            sMessage = sMessage.Replace("\\", "");
            string message = "{ 'text':'', 'blocks':[ {'type':'section','text':{'type':'mrkdwn','text':'"+ sMessage + "'}} ] }";
            //request.Headers.Add("Content-type", "application/json");
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            return await _httpClient.SendAsync(request);
        }
    }
}

public class MailClient
{
    private ConfigData ConfigData;

    public MailClient()
    {
        ConfigData = ConfigurationHelper.GetConfigFromFile(@"C:\temp\etw_service_config.json");
    }

    public void SendMail(string fromAddress, string toAddress, string subject, string body, string emailProviderName)
    {
        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress(fromAddress, fromAddress));
        mailMessage.To.Add(new MailboxAddress(toAddress, toAddress));
        mailMessage.Subject = subject;
        mailMessage.Body = new TextPart("plain")
        {
            Text = body
        };

        using (var smtpClient = new SmtpClient())
        {
            EMailProvider provider = ConfigData.Providers[emailProviderName];
            if(provider.SMTPEncryptionMode == SMTPEncryptionMode.STARTLS)
            {
                smtpClient.Connect(provider.SMTPAddress, 587, SecureSocketOptions.StartTls);
            }
            else
            {
                smtpClient.Connect(provider.SMTPAddress, 587, SecureSocketOptions.Auto);
            }
            smtpClient.Authenticate(provider.Username, provider.Password);
            smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
            smtpClient.Send(mailMessage);
            smtpClient.Disconnect(true);
        }
    }
}
