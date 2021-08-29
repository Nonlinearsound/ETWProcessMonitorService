# ETWProcessMonitorService
A process monitor service, using Event Tracing for Windows 

Once running as a service, It writes to c:\temp\process_watcher_ddMMYYYY.log and logs all process creation and termination events.

Place the files somewhere in a directory in the filesystem.
Add the executable as a service in an elevated command shell using:

`sc.exe create "Process Watcher Service" binpath=<path-to-ETWWorkerService.exe`

Remove itm using:

`sc.exe delete "Process Watcher Service"`
