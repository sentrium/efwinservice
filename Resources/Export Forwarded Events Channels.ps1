$rootDirectory =  "C:\Program Files\Logon Events Watcher Service"
$backupDirectory = "{0}\Forwarded Events Channels\ForwardedEventsChannel.reg" -f $rootDirectory
$keyToExport = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\ForwardedEvents"

iex 'regedit.exe /e $backupDirectory $keyToExport'
