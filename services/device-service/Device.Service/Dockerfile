FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/device-service/Device.Service/Device.Service.csproj", "services/device-service/Device.Service/"]
RUN dotnet restore "services/device-service/Device.Service/Device.Service.csproj"

COPY . .
RUN dotnet publish "services/device-service/Device.Service/Device.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Device.Service.dll"]
