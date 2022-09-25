FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY . ./
ENV ASPNETCORE_URLS=http://+:80
RUN dotnet restore
RUN dotnet publish -c Release -o out
EXPOSE 80/tcp

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Finder.Web.dll"]
