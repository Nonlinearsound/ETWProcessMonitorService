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
using Newtonsoft.Json;

namespace App.WindowsService
{
    public sealed class EventLogService : BackgroundService
    {
        private bool mTaskStillActive = false;
        private IDictionary<string, EventLogReader> eventReaders;
        private readonly ILogger<EventLogService> _logger;
        private ConfigData _configData;

        public EventLogService()
        {
            using ILoggerFactory loggerFactory =
            LoggerFactory.Create( builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                })
                .AddFilter(level => level >= LogLevel.Information)
                .AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings()
                {
                    LogName = "ETWService",
                    SourceName = "ETWService"
                })
                .AddFilter(level => level >= LogLevel.Information));
            _logger = loggerFactory.CreateLogger<EventLogService>();
            if(_logger == null)
            {
                Console.WriteLine("Error: The logger could not be established. No logging will be available.");
            }

            // populate example config file
            //ConfigurationHelper.WriteConfigToFile(ConfigurationHelper.GetEmptyConfig(), @"c:\temp\etw_service_config.json");
            
            // read the config file
            _configData = ConfigurationHelper.GetConfigFromFile(@"c:\temp\etw_service_config.json");
            if(_configData == null)
            {
                Console.WriteLine("Error: Could not read config data from file. No Event Reader will be started!");
                _logger.LogError("EventLogService could not read config file. No Event reader will be started.");
            }
        }
            
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime dtStart = DateTime.Now;
            eventReaders = new Dictionary<string, EventLogReader>();
            _logger.LogInformation("EventLogService initializing: Registering EVent Reader of Windows Event Logs.");
            
            // populate with EventReader objects to be executed threaded as a Task
            try
            {
                foreach (var eventLog in _configData.EventLogs)
                {
                    eventReaders.Add(eventLog.Name, new EventLogReader(eventLog.EventPath, eventLog.EventQuery, eventLog, _configData));
                    _logger.LogInformation("Event Reader {0} initialized for Path='{1}' and Query='{2}'", eventLog.Name,eventLog.EventPath,eventLog.EventQuery);
                }
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