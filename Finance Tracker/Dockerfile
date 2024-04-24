# Use the official Microsoft SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Finance Tracker.csproj", "./"]
RUN dotnet restore "Finance Tracker.csproj"

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build "Finance Tracker.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Finance Tracker.csproj" -c Release -o /app/publish

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app/publish .

# Command to run the migrations and start the application
ENTRYPOINT ["dotnet", "Finance Tracker.dll", "migrate"]