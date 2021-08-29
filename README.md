# ETW-ProcessMonitor Service
A tiny process monitor service, using Event Tracing for Windows that writes log files. 

This is DotNET5 implementation of a Windows service (background process using a worker) that listens to process creation and termination events using the Event Traing System for Windows. As such, its timing is good but not real time. The application writes to a log file for further use.

There ist the possibility for tracing Dll image load and unload events but at the moment it is not activated.

## Usage

Once running as a service, It writes to c:\temp\process_watcher_ddMMYYYY.log and logs all process creation and termination events.

Place the files somewhere in a directory in the filesystem.
Add the executable as a service in an elevated command shell using:

`sc.exe create "Process Watcher Service" binpath=<path-to-ETWWorkerService.exe`

Remove it using:

`sc.exe delete "Process Watcher Service"`
