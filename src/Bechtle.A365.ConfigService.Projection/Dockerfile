FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app

FROM dotnet:2.1-sdk AS build
WORKDIR /src
COPY packages src/packages
COPY ["src/Bechtle.A365.ConfigService.Common/Bechtle.A365.ConfigService.Common.csproj", "src/Bechtle.A365.ConfigService.Common/"]
COPY ["src/Bechtle.A365.ConfigService.Parsing/Bechtle.A365.ConfigService.Parsing.csproj", "src/Bechtle.A365.ConfigService.Parsing/"]
COPY ["src/Bechtle.A365.ConfigService.Projection/Bechtle.A365.ConfigService.Projection.csproj", "src/Bechtle.A365.ConfigService.Projection/"]

RUN dotnet restore --configfile /src/nuget.config "src/Bechtle.A365.ConfigService.Projection/Bechtle.A365.ConfigService.Projection.csproj"
COPY . .
WORKDIR "/src/src/Bechtle.A365.ConfigService.Projection"
RUN dotnet build "Bechtle.A365.ConfigService.Projection.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Bechtle.A365.ConfigService.Projection.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Bechtle.A365.ConfigService.Projection.dll"]
