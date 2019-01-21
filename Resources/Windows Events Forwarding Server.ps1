Import-Module GroupPolicy

$currentDCFQDN = [System.Net.Dns]::GetHostByName($env:computerName).HostName
$subscriptionTargetURL = "Server=http://{0}:{1}/wsman/SubscriptionManager/WEC,Refresh=60" -f $currentDCFQDN,"5985"
$ChannelAccess = "O:BAG:SYD:(A;;0xf0005;;;SY)(A;;0x5;;;BA)(A;;0x1;;;S-1-5-32-573)(A;;0x1;;;NS)"

$SecurityKeyPath="HKLM\Software\Policies\Microsoft\Windows\EventLog\Security"
$SubscriptionManagerKeyPath = "HKLM\Software\Policies\Microsoft\Windows\EventLog\EventForwarding\SubscriptionManager"

$GPOName = "Windows Event Forwarding Server"
$defaultNC     = ( [ADSI]"LDAP://RootDSE" ).defaultNamingContext.Value


$GPO = New-GPO -Name $GPOName


Set-GPRegistryValue -Name $GPOName -Key $SubscriptionManagerKeyPath -ValueName "1" -Type String -Value $subscriptionTargetURL
Set-GPRegistryValue -Name $GPOName -Key $SecurityKeyPath -ValueName "ChannelAccess" -Type String -Value $ChannelAccess


New-GPLink -Name $GPOName -Target $defaultNC -Enforced Yes 

