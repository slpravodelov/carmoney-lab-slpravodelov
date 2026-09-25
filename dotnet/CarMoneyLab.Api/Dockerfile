FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj dotnet/CarMoneyLab.Api/
COPY dotnet/CarMoneyLab.Application/CarMoneyLab.Application.csproj dotnet/CarMoneyLab.Application/
COPY dotnet/CarMoneyLab.Domain/CarMoneyLab.Domain.csproj dotnet/CarMoneyLab.Domain/
COPY dotnet/CarMoneyLab.Infrastructure/CarMoneyLab.Infrastructure.csproj dotnet/CarMoneyLab.Infrastructure/
RUN dotnet restore dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj
COPY dotnet/ dotnet/
RUN dotnet publish dotnet/CarMoneyLab.Api/CarMoneyLab.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarMoneyLab.Api.dll"]
