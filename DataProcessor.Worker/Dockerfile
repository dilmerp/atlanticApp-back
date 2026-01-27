# -----------------------------------------------------------------
# ETAPA 1: BUILD
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todo el contenido de la solución. 
COPY . .

# Restaurar sobre la solución completa
RUN dotnet restore "AtlanticApp.sln"

# Copiar el código fuente restante para la compilación/publicación
WORKDIR "/src/DataProcessor.Worker"
RUN dotnet build "DataProcessor.Worker.csproj" -c Release -o /app/build
# -----------------------------------------------------------------
# ETAPA 2: PUBLISH
# -----------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "DataProcessor.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------
# ETAPA 3: FINAL (Runtime)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copiamos solo lo necesario desde la etapa de publicación
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataProcessor.Worker.dll"]
