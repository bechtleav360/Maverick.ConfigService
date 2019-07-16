FROM mcr.microsoft.com/dotnet/core/sdk:2.2 
COPY . /app
COPY devrootca.crt /usr/local/share/ca-certificates
RUN update-ca-certificates
WORKDIR /app
EXPOSE 8456
CMD dotnet Bechtle.A365.ConfigService.dll



