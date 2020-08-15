FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY bin/Release/netcoreapp3.1/ App/
WORKDIR /App
RUN apt-get -y update
RUN apt-get -y upgrade
RUN apt-get install -y ffmpeg libsodium-dev libopus-dev nano
ENTRYPOINT ["dotnet", "Cecilia_NET.dll"]
