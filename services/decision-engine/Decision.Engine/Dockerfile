FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MultichannelPrototype.sln", "./"]
COPY ["shared/Alerting.Shared/Alerting.Shared.csproj", "shared/Alerting.Shared/"]
COPY ["services/decision-engine/Decision.Engine/Decision.Engine.csproj", "services/decision-engine/Decision.Engine/"]
RUN dotnet restore "services/decision-engine/Decision.Engine/Decision.Engine.csproj"

COPY . .
RUN dotnet publish "services/decision-engine/Decision.Engine/Decision.Engine.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Decision.Engine.dll"]
