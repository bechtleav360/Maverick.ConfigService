FROM mcr.microsoft.com/dotnet/core/sdk:2.2 
COPY . /app
WORKDIR /app
RUN sed -i 's/\r$//' startup.sh && chmod +x startup.sh

ENTRYPOINT sh startup.sh



