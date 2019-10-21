#!/bin/bash
dotnet Bechtle.A365.ConfigService.Cli.dll $command
tail -f /dev/mnull