FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy solution and all project files including tests for restore
COPY BitByBitTrashAPI.sln ./
COPY BitByBitTrashAPI/*.csproj ./BitByBitTrashAPI/
COPY BitByBitTrashAPI.Tests/*.csproj ./BitByBitTrashAPI.Tests/

# Restore dependencies for the whole solution
RUN dotnet restore BitByBitTrashAPI.sln

# Copy all source files for both main app and tests
COPY BitByBitTrashAPI/. ./BitByBitTrashAPI/
COPY BitByBitTrashAPI.Tests/. ./BitByBitTrashAPI.Tests/

# Publish the main project (release mode, optimized)
WORKDIR /src/BitByBitTrashAPI
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Set ASP.NET Core to listen on port 5091
ENV ASPNETCORE_URLS=http://+:5091
ENV connectionString=""

# Install EF Core CLI for migrations
RUN dotnet tool install --global dotnet-ef && export PATH="$PATH:/root/.dotnet/tools"

# Copy published output from build stage
COPY --from=build /app/publish .

EXPOSE 5091

# Run migrations before starting the app
ENTRYPOINT ["sh", "-c", "export PATH=\"$PATH:/root/.dotnet/tools\" && ConnectionStrings__DefaultConnection=\"$connectionString\" dotnet ef database update --project BitByBitTrashAPI/BitByBitTrashAPI.csproj --no-build --context BitByBitTrashAPI.Service.LitterDbContext && dotnet BitByBitTrashAPI.dll"]
