# Użyj obrazu bazowego z SDK .NET do zbudowania aplikacji
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Kopiuj pliki csproj i przywróć zależności
COPY ["Finance Tracker/Finance Tracker.csproj", "./"]
RUN dotnet restore "Finance Tracker.csproj"

# Kopiuj resztę plików projektu
COPY ["Finance Tracker/", "./"]

# Zbuduj aplikację
RUN dotnet build "Finance Tracker.csproj" -c Release -o /app/build

# Publikuj aplikację
FROM build AS publish
RUN dotnet publish "Finance Tracker.csproj" -c Release -o /app/publish

# Kopiuj resztę plików projektu
COPY ["Finance Tracker/", "./"]

# Kopiuj skompilowane pliki aplikacji z etapu publikacji
COPY --from=publish /app/publish .

# Wykonaj migrację bazy danych
RUN dotnet ef database update --context ApplicationDbContext

# Użyj obrazu bazowego runtime .NET do uruchomienia aplikacji
FROM build AS final
WORKDIR /app

# Skopiuj skompilowane pliki aplikacji z poprzedniego etapu
COPY --from=apply-migration /app .

# Ustaw punkt wejścia aplikacji
ENTRYPOINT ["dotnet", "Finance Tracker.dll"]

