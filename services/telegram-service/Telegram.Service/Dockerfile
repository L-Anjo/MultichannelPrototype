FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/telegram-service/Telegram.Service/Telegram.Service.csproj", "services/telegram-service/Telegram.Service/"]
RUN dotnet restore "services/telegram-service/Telegram.Service/Telegram.Service.csproj"

COPY . .
RUN dotnet publish "services/telegram-service/Telegram.Service/Telegram.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Telegram.Service.dll"]
