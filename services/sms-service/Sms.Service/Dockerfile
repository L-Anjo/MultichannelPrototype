FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/sms-service/Sms.Service/Sms.Service.csproj", "services/sms-service/Sms.Service/"]
RUN dotnet restore "services/sms-service/Sms.Service/Sms.Service.csproj"

COPY . .
RUN dotnet publish "services/sms-service/Sms.Service/Sms.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sms.Service.dll"]
