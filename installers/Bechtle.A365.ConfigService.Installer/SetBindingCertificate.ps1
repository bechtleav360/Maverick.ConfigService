param(
    [string] $cert = "",
    [string] $hostname = "",
    [string] $certstore = "MY",
    [string] $site = ""
);

if ($cert -eq "") {
    Write-Error "cert undefined, can't set certificate for binding";
    exit 1;
}

if ($hostname -eq "") {
    Write-Error "hostname undefined, can't set certificate for binding";
    exit 1;
}

if ($site -eq ""){
    Write-Error "site undefined, can't set certificate for binding";
    exit 1;
}

Import-Module WebAdministration;

$guid = [guid]::NewGuid().ToString("B")
netsh http add sslcert hostnameport="${hostname}:443" certhash=$cert certstorename=$certstore appid="$guid"
New-WebBinding -name $site -Protocol https -HostHeader $hostname -Port 443 -SslFlags 1