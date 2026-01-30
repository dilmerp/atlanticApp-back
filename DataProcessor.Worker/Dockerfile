# -----------------------------------------------------------------
# ETAPA 1: BUILD (Compilación y Restauración)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo el contenido (incluyendo la carpeta atlanticApp-back)
COPY . .

# Restaurar usando la ruta correcta hacia el archivo de solución
RUN dotnet restore "atlanticApp-back/AtlanticApp.sln"

# Cambiamos el directorio de trabajo hacia la carpeta del proyecto específico
WORKDIR "/src/atlanticApp-back/DataProcessor.Worker"

# Compilamos el proyecto
RUN dotnet build "DataProcessor.Worker.csproj" -c Release -o /app/build

# -----------------------------------------------------------------
# ETAPA 2: PUBLISH (Publicación de binarios)
# -----------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "DataProcessor.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------
# ETAPA 3: FINAL (Imagen de ejecución ligera)
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Puerto por defecto para servicios .NET 8
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copiamos los archivos publicados desde la etapa anterior
COPY --from=publish /app/publish .

# Comando de inicio del Worker
ENTRYPOINT ["dotnet", "DataProcessor.Worker.dll"]
