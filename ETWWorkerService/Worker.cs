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

namespace App.WindowsService
{
    class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Hash { get; set; }
        public string Username { get; set; }
        public string CommandLine { get; set; }
        public string Path { get; set; }
    }

    public sealed class WindowsBackgroundService : BackgroundService
    {
        private IDictionary<int, ProcessInfo> dictProcesses;
        private readonly JokeService _jokeService;
        private readonly ILogger<WindowsBackgroundService> _logger;

        public WindowsBackgroundService(
            JokeService jokeService,
            ILogger<WindowsBackgroundService> logger) =>
            (_jokeService, _logger) = (jokeService, logger);


        private string CalcFileHash(string filePath)
        {
            byte[] hash;
            using (var inputStream = File.Open(filePath,FileMode.Open))
            {
                var md5 = MD5.Create();
                hash = md5.ComputeHash(inputStream);
            }
            return hash.ToString();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //dictProcesses = new Dictionary<int, ProcessInfo>();
            DateTime dtStart = DateTime.Now;

            dictProcesses = Process.GetProcesses().Select(p => new ProcessInfo
            {
                Name = p.ProcessName,
                Id = p.Id,
                Hash = p.GetHashCode(),
                //Username = p.StartInfo.UserName,
                //CommandLine = p.StartInfo.Arguments,
                //Path = p.StartInfo.FileName
            }).ToDictionary(p => p.Id);

            using (StreamWriter file = File.AppendText($"c:\\temp\\process_watch_{DateTime.Now.ToString("ddMMyyyy")}.log"))
            {
                string s = $"{dtStart}.{dtStart.Millisecond:D3}:   [Init] \tStarted monitoring..";
                file.WriteLine(s);
                s = $"{dtStart}.{dtStart.Millisecond:D3}:   [Init] \tCurrently running processes:";
                file.WriteLine(s);
                foreach (var p in dictProcesses)
                {
                    ProcessInfo pinfo = (ProcessInfo)p.Value;
                    s = $"{dtStart}.{dtStart.Millisecond:D3}:   [Init] \tProcess \t{pinfo.Id} \t\t({pinfo.Name})";
                    file.WriteLine(s);
                }
            }


            string TryGetProcessName(TraceEvent evt)
            {
                if (!string.IsNullOrEmpty(evt.ProcessName))
                    return evt.ProcessName;
                return dictProcesses.TryGetValue(evt.ProcessID, out var info) ? info.Name : string.Empty;
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

                        parser.ProcessStart += e =>
                        {
                            Console.ForegroundColor = ConsoleColor.Green;

                            // add to processes dictionary if not present there (key is process id)
                            string sAdded = "";
                            if (!dictProcesses.Keys.Contains(e.ProcessID))
                            {
                                dictProcesses.Add(e.ProcessID, new ProcessInfo
                                {
                                    Name = e.ProcessName,
                                    Id = e.ProcessID
                                }
                                );
                                sAdded = "*";
                            }

                            string s = $"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: [create] {sAdded}Process \t{e.ProcessID} \t\t({e.ProcessName}) \tCreated by \t{e.ParentID} \t[{e.GetHashCode()}]: \t{e.CommandLine}";

                            //_logger.LogInformation(s);
                            using (StreamWriter file = File.AppendText($"c:\\temp\\process_watch_{DateTime.Now.ToString("ddMMyyyy")}.log"))
                            {
                                file.WriteLine(s);
                            

                                if (e.ProcessName == "cmd")
                                {
                                    ProcessInfo parent = dictProcesses[e.ParentID];
                                    if (parent != null)
                                    {
                                        if (parent.Name == "EXCEL")
                                        {
                                            s = $"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}:   [WARN] EXCEL executed a command processor!! This could be a trojan!!";
                                            file.WriteLine(s);
                                        }
                                    }
                                }
                            }


                            // Entfernt: Es ist vorgekommen, dass die geiche PID nochmal gestartet wurde und dann konnte diese nicht mehr
                            // in die map "processes" hinzugefügt werden, da sie schon vorhanden war.
                            // TODO: Im Termination event sollte dieser schlüssel dann entfernt werden.
                            //
                            //processes.Add(e.ProcessID, new ProcessInfo { Id = e.ProcessID, Name = e.ProcessName });
                        };
                        parser.ProcessStop += e => {
                            Console.ForegroundColor = ConsoleColor.Red;
                            string s = $"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}:   [exit] Process \t{e.ProcessID} \t\t({TryGetProcessName(e)})";

                            // remove process from processes dictionary
                            dictProcesses.Remove(e.ProcessID);
                                                        
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