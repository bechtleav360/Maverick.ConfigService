param (
    # Parameter help description
    [Parameter(mandatory=$true)]
    [String]
    $FilePath = "..\appsettings.json",

    # Parameter help description
    [Parameter(mandatory=$true)]
    [String]
    $EventBusConnectionServer,

    # Parameter help description
    [Parameter(mandatory=$true)]
    [String]
    $EventStoreConnectionUri,

    # Parameter help description
    [Parameter(mandatory=$true)]
    [String]
    $ProjectionStorageConnectionString
)

# Formats JSON in a nicer format than the built-in ConvertTo-Json does.
function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
    $indent = 0;
    $json = $json.Replace('\u003c', '<')
    $json = $json.Replace('\u003e', '>')
    ($json -Split '\n' |
    % {
        if ($_ -match '[\}\]]') {
            # This line contains ] or }, decrement the indentation level
            $indent--
        }
        $line = (' ' * $indent * 2) + $_.TrimStart().Replace(': ', ': ')
        if ($_ -match '[\{\[]') {
            # This line contains [ or {, increment the indentation level
            $indent++
        }
        if($_ -match '[\u003c]') {
        }
        $line
    }) -Join "`n"
}

if(Test-Path $FilePath) {
    $a = Get-Content $FilePath -raw -Encoding UTF8 | ConvertFrom-Json
    $a.EventBusConnection.Server = $EventBusConnectionServer
    $a.EventStoreConnection.Uri = $EventStoreConnectionUri
    $a.ProjectionStorage.ConnectionString = $ProjectionStorageConnectionString
    $a | ConvertTo-Json | Format-Json | set-content $FilePath -Encoding UTF8
}