# -----------------------------------------------------------------
# ETAPA 1: BUILD (Compilación y Restore)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia de archivos .csproj y .sln
# Esto es CRÍTICO para que 'dotnet restore' encuentre todas las referencias.
COPY AtlanticApp.sln .
COPY Notification.Worker/*.csproj Notification.Worker/
COPY Common.Domain/*.csproj Common.Domain/
# Eliminamos la línea que falló: COPY Common.Messages/*.csproj Common.Messages/
COPY Common.Persistence/*.csproj Common.Persistence/
COPY DataProcessor.Application/*.csproj DataProcessor.Application/
COPY DataProcessor.Infrastructure/*.csproj DataProcessor.Infrastructure/
COPY DataProcessor.Worker/*.csproj DataProcessor.Worker/
COPY FileIngestor.API/*.csproj FileIngestor.API/
COPY FileIngestor.Application/*.csproj FileIngestor.Application/
COPY FileIngestor.Infrastructure/*.csproj FileIngestor.Infrastructure/

# Restaura las dependencias (NuGets)
RUN dotnet restore

# Copia el código fuente completo restante para la compilación (archivos .cs, appsettings, etc.)
COPY . .

# -----------------------------------------------------------------
# ETAPA 2: PUBLISH (Publicación de los archivos ejecutables)
# -----------------------------------------------------------------
FROM build AS publish
WORKDIR "/src/Notification.Worker"
RUN dotnet publish "Notification.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------
# ETAPA 3: FINAL (Runtime)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Define el punto de entrada de la aplicación
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]