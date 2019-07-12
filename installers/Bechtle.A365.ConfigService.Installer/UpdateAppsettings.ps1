param (
    [Parameter(mandatory=$true)]
    [String]
    $FilePath = "..\appsettings.json",

    [Parameter(mandatory=$true)]
    [String]
    $EventBusConnectionServer,

    [Parameter(mandatory=$true)]
    [String]
    $EventStoreConnectionUri,

    [Parameter(mandatory=$true)]
    [String]
    $ProjectionStorageBackend,

    [Parameter(mandatory=$true)]
    [String]
    $ProjectionStorageConnectionString,

    [Parameter(mandatory=$true)]
    [String]
    $RabbitMqHost,

    [Parameter(mandatory=$true)]
    [String]
    $RabbitMqUser,

    [Parameter(mandatory=$true)]
    [String]
    $RabbitMqPassword,

    [Parameter(mandatory=$true)]
    [String]
    $RabbitMqPort
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
    $a.ProjectionStorage.Backend = $ProjectionStorageBackend
    $a.ProjectionStorage.ConnectionString = $ProjectionStorageConnectionString
    $a.LoggingConfiguration.NLog.Variables.RabbitMqHost = $RabbitMqHost
    $a.LoggingConfiguration.NLog.Variables.RabbitMqUser = $RabbitMqUser
    $a.LoggingConfiguration.NLog.Variables.RabbitMqPassword = $RabbitMqPassword
    $a.LoggingConfiguration.NLog.Variables.RabbitMqPort = $RabbitMqPort
    $a | ConvertTo-Json -Depth 100 | Format-Json | set-content $FilePath -Encoding UTF8
}