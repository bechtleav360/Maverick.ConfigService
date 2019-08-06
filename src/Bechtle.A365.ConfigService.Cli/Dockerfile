FROM mcr.microsoft.com/dotnet/core/sdk:2.2 
COPY . /app
WORKDIR /app
CMD dotnet Bechtle.A365.ConfigService.Cli.dll migrate -c "Host=postgres;Port=5432;Database=DOCKER_A365ConfigProjection;User Id=sa;Password=1;" -b postgres


