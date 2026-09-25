FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj dotnet/CarMoneyLab.Api/
RUN dotnet restore dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj
COPY dotnet/CarMoneyLab.Api/ dotnet/CarMoneyLab.Api/
RUN dotnet publish dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarMoneyLab.Api.dll"]
