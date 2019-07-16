FROM mcr.microsoft.com/dotnet/core/sdk:2.2 
COPY . /app
#RUN rm /root/.dotnet/corefx/cryptography/x509stores/my/*
#COPY aspnetapp.pfx /root/.dotnet/corefx/cryptography/x509stores/my/69611f15e60f53bb88581a2f0daaecc5cdd24d7a.pfx
COPY devrootca.crt /usr/local/share/ca-certificates
RUN update-ca-certificates
WORKDIR /app
CMD dotnet Bechtle.A365.ConfigService.Projection.dll --console



