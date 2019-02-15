param([string]$appPool = "")

if ($appPool -eq "") {
    Write-Error "application pool undefined, use '-appPool SomeApplicationPool' when running this script";
    exit 1;
}

Import-Module WebAdministration;
Set-ItemProperty IIS:\AppPools\$appPool managedRuntimeVersion "";
