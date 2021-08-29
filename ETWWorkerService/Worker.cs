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

namespace App.WindowsService
{
    class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly JokeService _jokeService;
        private readonly ILogger<WindowsBackgroundService> _logger;

        public WindowsBackgroundService(
            JokeService jokeService,
            ILogger<WindowsBackgroundService> logger) =>
            (_jokeService, _logger) = (jokeService, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var processes = Process.GetProcesses().Select(p => new ProcessInfo
            {
                Name = p.ProcessName,
                Id = p.Id
            }).ToDictionary(p => p.Id);


            string TryGetProcessName(TraceEvent evt)
            {
                if (!string.IsNullOrEmpty(evt.ProcessName))
                    return evt.ProcessName;
                return processes.TryGetValue(evt.ProcessID, out var info) ? info.Name : string.Empty;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var session = new TraceEventSession(Environment.OSVersion.Version.Build >= 9200 ? "MyKernelSession" : KernelTraceEventParser.KernelSessionName))
                    {
                        // aktuell nur Process Creation/Termination
                        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process ); // | KernelTraceEventParser.Keywords.ImageLoad
                        var parser = session.Source.Kernel;

                        parser.ProcessStart += e => {
                            Console.ForegroundColor = ConsoleColor.Green;
                            string s = $"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: [create] Process {e.ProcessID} ({e.ProcessName}) Created by {e.ParentID}: {e.CommandLine}";
                            //_logger.LogInformation(s);
                            using (StreamWriter file = File.AppendText($"c:\\temp\\process_watch_{DateTime.Now.ToString("ddMMyyyy")}.log"))
                            {
                                file.WriteLine(s);
                            }
                            processes.Add(e.ProcessID, new ProcessInfo { Id = e.ProcessID, Name = e.ProcessName });
                        };
                        parser.ProcessStop += e => {
                            Console.ForegroundColor = ConsoleColor.Red;
                            string s = $"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: [exit] Process {e.ProcessID} {TryGetProcessName(e)} Exited";
                            //_logger.LogInformation(s);
                            using (StreamWriter file = File.AppendText($"c:\\temp\\process_watch_{DateTime.Now.ToString("ddMMyyyy")}.log"))
                            {
                                file.WriteLine(s);
                            }
                        };

                        /*
                        parser.ImageLoad += e => {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            var name = TryGetProcessName(e);
                            _logger.LogInformation($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Image Loaded: {e.FileName} into process {e.ProcessID} ({name}) Size=0x{e.ImageSize:X}");
                        };

                        parser.ImageUnload += e => {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            var name = TryGetProcessName(e);
                            _logger.LogInformation($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Image Unloaded: {e.FileName} from process {e.ProcessID} ({name})");
                        };
                        */

                        await Task.Run(() => session.Source.Process());
                        Thread.Sleep(TimeSpan.FromSeconds(60));
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}