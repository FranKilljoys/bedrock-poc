FROM mcr.microsoft.com/dotnet/aspnet:10.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BedRock.POC/BedRock.POC.csproj", "BedRock.POC/"]
RUN dotnet restore "BedRock.POC/BedRock.POC.csproj"
COPY BedRock.POC/ BedRock.POC/
RUN dotnet publish "BedRock.POC/BedRock.POC.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BedRock.POC.dll"]
