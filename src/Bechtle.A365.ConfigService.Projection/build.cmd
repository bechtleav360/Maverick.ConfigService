@echo off
dotnet publish -c Release -r debian.9-x64
docker build -t localhost:5000/configservice_projection bin/Release/netcoreapp2.2/debian.9-x64/publish/
docker-compose up -d --force-recreate