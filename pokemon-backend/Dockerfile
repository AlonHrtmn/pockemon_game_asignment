# Use the official Microsoft .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["pokemon-backend.csproj", "./"]
RUN dotnet restore "pokemon-backend.csproj"

# Copy everything else and build the app
COPY . .
RUN dotnet publish "pokemon-backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET Core runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose default port
EXPOSE 5088

ENTRYPOINT ["dotnet", "pokemon-backend.dll"]
