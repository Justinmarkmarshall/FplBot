# Use the official .NET 9 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project file for dependency restoration
COPY FplBot/*.csproj ./FplBot/

# Restore dependencies for the main project only
WORKDIR /app/FplBot
RUN dotnet restore

# Copy the rest of the source code
COPY FplBot/. ./

# Publish the application
RUN dotnet publish -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /out .

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FplBot.dll"]