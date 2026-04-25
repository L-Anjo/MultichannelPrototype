FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/push-service/Push.Service/Push.Service.csproj", "services/push-service/Push.Service/"]
RUN dotnet restore "services/push-service/Push.Service/Push.Service.csproj"

COPY . .
RUN dotnet publish "services/push-service/Push.Service/Push.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Push.Service.dll"]
