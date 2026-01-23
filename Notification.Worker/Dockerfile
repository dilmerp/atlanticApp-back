# -----------------------------------------------------------------
# ETAPA 1: BUILD (Compilación y Restore de Dependencias)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo el contenido de la solución. 
COPY . .

# Restaurar sobre la solución completa
RUN dotnet restore "AtlanticApp.sln"

# Copiar el código fuente restante para la compilación/publicación

WORKDIR "/src/Notification.Worker"
RUN dotnet build "Notification.Worker.csproj" -c Release -o /app/build

# -----------------------------------------------------------------
# ETAPA 2: PUBLISH
# -----------------------------------------------------------------
FROM build AS publish
# WORKDIR /src/Notification.Worker
RUN dotnet publish "Notification.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------
# ETAPA 3: FINAL (Runtime)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080


COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]