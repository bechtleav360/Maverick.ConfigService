FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 8705

FROM dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Bechtle.A365.ConfigService/Bechtle.A365.ConfigService.csproj src/Bechtle.A365.ConfigService/
COPY src/Bechtle.A365.ConfigService.Projection/Bechtle.A365.ConfigService.Projection.csproj src/Bechtle.A365.ConfigService.Projection/
COPY packages src/packages
RUN dotnet restore --configfile /src/nuget.config src/Bechtle.A365.ConfigService/Bechtle.A365.ConfigService.csproj
COPY . .
WORKDIR /src/src/Bechtle.A365.ConfigService
RUN dotnet build Bechtle.A365.ConfigService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Bechtle.A365.ConfigService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Bechtle.A365.ConfigService.dll"]
