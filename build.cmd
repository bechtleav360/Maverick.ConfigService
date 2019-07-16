@echo off

dotnet publish src/Bechtle.A365.ConfigService.Cli/Bechtle.A365.ConfigService.Cli.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice_migrate src/Bechtle.A365.ConfigService.Cli/bin/Release/netcoreapp2.1/debian.9-x64/publish/

dotnet publish src/Bechtle.A365.ConfigService.Projection/Bechtle.A365.ConfigService.Projection.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice_projection src/Bechtle.A365.ConfigService.Projection/bin/Release/netcoreapp2.1/debian.9-x64/publish/

dotnet publish src/Bechtle.A365.ConfigService/Bechtle.A365.ConfigService.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice src/Bechtle.A365.ConfigService/bin/Release/netcoreapp2.1/debian.9-x64/publish/

docker-compose -f docker-compose.local.yml up -d