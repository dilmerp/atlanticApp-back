# -----------------------------------------------------------------
# ETAPA 1: BUILD (Compilación)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo el contenido de la solución. 
COPY . .

# Restaurar sobre la solución completa
RUN dotnet restore "AtlanticApp.sln"

# Copiar el código fuente restante para la compilación/publicación
# COPY . .
WORKDIR "/src/DataProcessor.Api"
RUN dotnet build "DataProcessor.Api.csproj" -c Release -o /app/build

# -----------------------------------------------------------------
# ETAPA 2: PUBLISH (Publicación)
# -----------------------------------------------------------------
FROM build AS publish
# WORKDIR /src/DataProcessor.Api
RUN dotnet publish "DataProcessor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# -----------------------------------------------------------------
# ETAPA 3: FINAL (Runtime - Imagen ligera para ejecución)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ajuste para .NET 8: Usamos el puerto 8080 (no privilegiado)

# EXPOSE 80 
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copiamos solo lo necesario desde la etapa de publicación
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataProcessor.Api.dll"]