FROM mcr.microsoft.com/dotnet/sdk:6.0
COPY . /app
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
RUN ["dotnet", "restore"]
RUN ["dotnet", "build"]
EXPOSE 80/tcp
RUN ["dotnet", "tool", "install", "--global", "dotnet-ef"]
ENV PATH="${PATH}:~/.dotnet/tools/"
RUN chmod +x ./entrypoint.sh
CMD /bin/bash ./entrypoint.sh