param(
    [string] $thumbprint = "",
    [string] $certname = "",
    [string] $hostname = "",
    [string] $certstore = "MY",
    [string] $site = ""
);

New-EventLog -LogName Application -Source "A365Setup" -ErrorAction Ignore

if([String]::IsNullOrWhiteSpace($thumbprint) -and [String]::IsNullOrWhiteSpace($certname)) {
    Write-Error -Message "Certname and thumbprint empty"
    Write-EventLog -LogName Application -Source "A365Setup" -EntryType Information -EventId 1 -Message "Certname and thumbprint empty"
    exit 1;
}

if([String]::IsNullOrWhiteSpace($thumbprint)) {
    $thumbprint = (Get-ChildItem -Path Cert:\LocalMachine\$certstore | Where-Object {$_.FriendlyName -match $certname}).Thumbprint;

    if([String]::IsNullOrWhiteSpace($thumbprint)) {
        Write-EventLog -LogName Application -Source "A365Setup" -EntryType Information -EventId 1 -Message "Failed to get thumbprint of Cert '$certname'";
        exit 1;
    }
}

if([String]::IsNullOrWhiteSpace($hostname)) {
    Write-EventLog -LogName Application -Source "A365Setup" -EntryType Information -EventId 1 -Message "hostname undefined, can't set certificate for binding";
    exit 1;
}

if([String]::IsNullOrWhiteSpace($site)) {
    Write-EventLog -LogName Application -Source "A365Setup" -EntryType Information -EventId 1 -Message "site undefined, can't set certificate for binding";
    exit 1;
}

Import-Module WebAdministration;

$guid = [Guid]::NewGuid().ToString("B")
netsh http add sslcert hostnameport="${hostname}:443" certhash=$thumbprint certstorename=$certstore appid="$guid"

New-WebBinding -name $site -Protocol https -HostHeader $hostname -Port 443 -SslFlags 1