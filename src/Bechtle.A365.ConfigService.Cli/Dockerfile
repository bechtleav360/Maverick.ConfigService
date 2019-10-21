ARG SERVICENAME=Bechtle.A365.ConfigService.Cli
FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM maverick.azurecr.io/dotnet/core/sdk:2.2-stretch AS build
ARG SERVICENAME
WORKDIR /src
COPY . .
RUN dotnet restore "src/$SERVICENAME/$SERVICENAME.csproj"
WORKDIR /src/src/$SERVICENAME
RUN dotnet build "$SERVICENAME.csproj" -c Release -r linux-x64 -o /app

FROM build AS publish
ARG SERVICENAME
RUN dotnet publish "$SERVICENAME.csproj" -c Release -r linux-x64 -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["/usr/share/dotnet" "Bechtle.A365.ConfigService.Cli.dll" "migrate" "-b" "postgres"]
CMD ["-c" "Host=postgres;Port=5432;Database=ConfigProjection;User Id=sa;Password=1;"]
