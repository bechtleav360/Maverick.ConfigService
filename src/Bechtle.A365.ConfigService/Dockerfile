ARG SERVICENAME=Bechtle.A365.ConfigService
FROM maverick.azurecr.io/dotnet/core/sdk:3.0 AS base
WORKDIR /app

FROM maverick.azurecr.io/dotnet/core/sdk:3.0 AS build
ARG SERVICENAME
WORKDIR /src
COPY . .
RUN dotnet restore "src/$SERVICENAME/$SERVICENAME.csproj"
WORKDIR /src/src/$SERVICENAME
RUN dotnet build "$SERVICENAME.csproj" -c Release -o /app

FROM build AS publish
ARG SERVICENAME
RUN dotnet publish "$SERVICENAME.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

RUN sed -i 's/\r$//' startup.sh && chmod +x startup.sh
ENTRYPOINT sh startup.sh
