#!/usr/bin/env bash
echo "Command: '$command'"
echo dotnet Bechtle.A365.ConfigService.Cli.dll "$command" > start.sh && ./start.sh