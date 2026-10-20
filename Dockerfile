FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for caching restore layer
COPY Energy_Project.sln ./
COPY Energy_Project/Energy_Project.csproj Energy_Project/
COPY SmartHomeTests/SmartHomeTests.csproj SmartHomeTests/
RUN dotnet restore

# Copy all source files
COPY . .

# Test stage: runs tests during docker build or as a separate target
FROM build AS test
WORKDIR /src/SmartHomeTests
CMD ["dotnet", "test", "--logger:console;verbosity=normal"]

# Publish stage
FROM build AS publish
WORKDIR /src/Energy_Project
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Energy_Project.dll"]
