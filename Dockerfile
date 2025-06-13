# Use SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy solution and project files first for better cache usage
COPY BitByBitTrashAPI.sln ./
COPY BitByBitTrashAPI/*.csproj ./BitByBitTrashAPI/

# Restore dependencies only (cache this step)
RUN dotnet restore BitByBitTrashAPI.sln

# Copy all source files
COPY BitByBitTrashAPI/. ./BitByBitTrashAPI/

# Publish the project (release mode, optimized)
WORKDIR /src/BitByBitTrashAPI
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Use runtime image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

EXPOSE 5091
ENTRYPOINT ["dotnet", "BitByBitTrashAPI.dll"]
