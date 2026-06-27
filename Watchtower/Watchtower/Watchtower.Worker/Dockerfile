FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Watchtower/Watchtower.Worker/Watchtower.Worker.csproj", "Watchtower/Watchtower.Worker/"]
COPY ["Watchtower/Watchtower.Application/Watchtower.Application.csproj", "Watchtower/Watchtower.Application/"]
COPY ["Watchtower/Watchtower.Infrastructure/Watchtower.Infrastructure.csproj", "Watchtower/Watchtower.Infrastructure/"]
COPY ["Watchtower/Watchtower.Domain/Watchtower.Domain.csproj", "Watchtower/Watchtower.Domain/"]
RUN dotnet restore "Watchtower/Watchtower.Worker/Watchtower.Worker.csproj"
COPY . .
RUN dotnet publish "Watchtower/Watchtower.Worker/Watchtower.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Watchtower.Worker.dll"]
