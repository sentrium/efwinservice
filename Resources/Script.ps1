$url = "http://dc01.ringplan.test:8733/Design_Time_Addresses/LogonEventsWatcherService.WindowsEventWCFService/EventWCFService/registerevent"

#$action = "logon"
$action = $args[0]
$username = "$($env:USERNAME)"
$computername = "$($env:COMPUTERNAME)"

$params = @{
"eventLogEntry" = @{
"action" = $action;
"username" = $username;
"userdomain" = $env:USERDNSDOMAIN;
"computername" = $computername;
"computerdomain" = (Get-WmiObject win32_computersystem).Domain;
}


}
# $params | ConvertTo-Json
Invoke-WebRequest -Uri $url -Method POST -Body ($params | ConvertTo-Json) -ContentType "application/json"