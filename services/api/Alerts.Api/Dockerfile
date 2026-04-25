FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/api/Alerts.Api/Alerts.Api.csproj", "services/api/Alerts.Api/"]
RUN dotnet restore "services/api/Alerts.Api/Alerts.Api.csproj"

COPY . .
RUN dotnet publish "services/api/Alerts.Api/Alerts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Alerts.Api.dll"]
