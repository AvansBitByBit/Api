
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY BitByBitTrashAPI.sln ./
COPY BitByBitTrashAPI/*.csproj ./BitByBitTrashAPI/
RUN dotnet restore BitByBitTrashAPI.sln

COPY . .

WORKDIR /src/BitByBitTrashAPI
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BitByBitTrashAPI.dll"]