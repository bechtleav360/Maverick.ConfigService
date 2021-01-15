ARG SERVICENAME=Bechtle.A365.UserProfileService.Sync
ARG buildConfiguration=RELEASE
ARG nugetfeed=https://pkgs.dev.azure.com/av360/_packaging/A365/nuget/v3/index.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
ARG SERVICENAME
ARG pat
ARG nugetfeed
ARG buildConfiguration

# prepare to pull from private Nuget Feed
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS "{\"endpointCredentials\": [{\"endpoint\":\"${nugetfeed}\", \"username\":\"build\", \"password\":\"${pat}\"}]}"
RUN wget -O - https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh  | bash

WORKDIR /src
COPY . .
# feed have to be explicitly specified
RUN dotnet restore "src/$SERVICENAME/$SERVICENAME.csproj" -s "${nugetfeed}" -s "https://api.nuget.org/v3/index.json"
WORKDIR /src/src/$SERVICENAME
RUN dotnet build "$SERVICENAME.csproj" -c ${buildConfiguration} --no-restore -f netcoreapp3.1

FROM build AS publish
ARG SERVICENAME
ARG buildConfiguration
RUN dotnet publish "$SERVICENAME.csproj" -c ${buildConfiguration} --no-build -o /app -f netcoreapp3.1

FROM base AS final
ARG SERVICENAME
ENV SERVICENAME=$SERVICENAME

WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT dotnet $SERVICENAME.dll
