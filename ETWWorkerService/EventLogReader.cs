using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;

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

        /// <summary>
        /// Holds the path to the Windows event provider, for instance "Microsoft-Windows-Windows Defender/Operational"
        /// </summary>
        /// <param name="eventPath"></param>
        public string eventPath { get; set; }

        /// <summary>
        /// This function handles an event that has been gathered by the Watcher.
        /// TODO:   We should put the events data into a buffer here to be processed in a timed thread somewhere else.
        ///         At the moment we are just processing it here just to test base functionality.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="arg"></param>
        public static void handleEvent(object obj, EventRecordWrittenEventArgs arg)
        {
            if (arg.EventRecord != null)
            {
                Console.WriteLine("Event {0} in {1}", arg.EventRecord.Id, arg.EventRecord.LogName);
            }
            else
            {
                Console.WriteLine("The event instance was null.");
            }
        }
        /// <summary>
        /// Setting up everything to gather events.
        /// Do not yet activate the Watcher yet. This functionality is exposed for the user code.
        /// </summary>
        /// <param name="_eventPath"></param>
        /// <param name="_queryString"></param>
        public EventLogReader(string _eventPath, string _queryString)
        {
            eventPath = _eventPath;
            queryString = _queryString;
            query = new EventLogQuery(eventPath, PathType.LogName, queryString);
            watcher = new EventLogWatcher(query);
            watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(handleEvent);
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
