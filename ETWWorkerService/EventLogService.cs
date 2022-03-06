using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics.Eventing.Reader;

namespace App.WindowsService
{
    public sealed class EventLogService : BackgroundService
    {
        private bool mTaskStillActive = false;
        private IDictionary<string, EventLogReader> eventReaders;
        private readonly ILogger<EventLogService> _logger;

        public EventLogService(ILogger<EventLogService> logger) =>(_logger) = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime dtStart = DateTime.Now;
            eventReaders = new Dictionary<string, EventLogReader>();
            _logger.LogInformation("EventLogService initializing...");

            // This creates a new example configuration as a JSON file
            //string emptyConfigData = ConfigurationHelper.GetEmptyConfig();
            //Console.WriteLine(emptyConfigData);
            //File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\etwservice_config.json",emptyConfigData);
            
            // populate with EventReader objects to be executed threaded as a Task
            try
            {
                eventReaders.Add("Windows-Defender-Hazards", new EventLogReader("Microsoft-Windows-Windows Defender/Operational", "*"));
                eventReaders.Add("PowershellCore", new EventLogReader("PowerShellCore/Operational", "*"));
            }
            catch (EventLogReadingException e)
            {
                Console.WriteLine("An error occurred during subscribing to the event queue: {0}", e.Message);
            }
            
            // Executing Task Functionality and wait indefinitely
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (KeyValuePair<string, EventLogReader> entry in eventReaders)
                    {
                        EventLogReader eventLogReader = entry.Value;
                        _logger.LogInformation("EventLogService added LogReader {0}",entry.Key);
                        await Task.Run(() => eventLogReader.Enable());
                    }
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}