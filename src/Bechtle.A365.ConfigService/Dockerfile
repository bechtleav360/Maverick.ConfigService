FROM mcr.microsoft.com/dotnet/core/runtime:2.2 
COPY . /app
WORKDIR /app
EXPOSE 8456
RUN sed -i 's/\r$//' startup.sh && chmod +x startup.sh

ENTRYPOINT sh startup.sh



