@echo off

dotnet publish src/Bechtle.A365.ConfigService.Cli/Bechtle.A365.ConfigService.Cli.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice_migrate src/Bechtle.A365.ConfigService.Cli/bin/Release/netcoreapp2.2/debian.9-x64/publish/

dotnet publish src/Bechtle.A365.ConfigService.Projection/Bechtle.A365.ConfigService.Projection.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice_projection src/Bechtle.A365.ConfigService.Projection/bin/Release/netcoreapp2.2/debian.9-x64/publish/

dotnet publish src/Bechtle.A365.ConfigService/Bechtle.A365.ConfigService.csproj -c Release -r debian.9-x64
docker build -t localhost:5000/configservice src/Bechtle.A365.ConfigService/bin/Release/netcoreapp2.2/debian.9-x64/publish/

docker-compose -f D:/TFS/Maverick/docker-compose.yml up -d configservice_projection
docker-compose -f D:/TFS/Maverick/docker-compose.yml up -d --force-recreate configservice