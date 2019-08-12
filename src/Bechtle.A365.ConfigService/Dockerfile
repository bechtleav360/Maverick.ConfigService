FROM mcr.microsoft.com/dotnet/core/sdk:2.2 
COPY . /app
COPY devrootca.crt /usr/local/share/ca-certificates
RUN update-ca-certificates
WORKDIR /app
EXPOSE 8456
RUN apt-get update && apt-get -y install curl && apt-get -y install jq
RUN sed -i 's/\r$//' startup.sh && chmod +x startup.sh

ENTRYPOINT sh startup.sh



