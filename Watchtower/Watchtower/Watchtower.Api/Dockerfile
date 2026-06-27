FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Watchtower/Watchtower.Api/Watchtower.Api.csproj", "Watchtower/Watchtower.Api/"]
COPY ["Watchtower/Watchtower.Application/Watchtower.Application.csproj", "Watchtower/Watchtower.Application/"]
COPY ["Watchtower/Watchtower.Infrastructure/Watchtower.Infrastructure.csproj", "Watchtower/Watchtower.Infrastructure/"]
COPY ["Watchtower/Watchtower.Domain/Watchtower.Domain.csproj", "Watchtower/Watchtower.Domain/"]
RUN dotnet restore "Watchtower/Watchtower.Api/Watchtower.Api.csproj"
COPY . .
RUN dotnet publish "Watchtower/Watchtower.Api/Watchtower.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Watchtower.Api.dll"]
