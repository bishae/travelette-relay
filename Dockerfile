# Use the .NET 10.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj file and restore dependencies
COPY ["Travelette.Relay.csproj", "./"]
RUN dotnet restore "Travelette.Relay.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src"
RUN dotnet build "Travelette.Relay.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Travelette.Relay.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Expose port (ASP.NET Core default is 8080 in container)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Travelette.Relay.dll"]

