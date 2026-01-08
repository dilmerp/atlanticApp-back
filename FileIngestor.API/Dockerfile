# Usa la imagen SDK para construir y compilar el código (Etapa de BUILD)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia la solución y los archivos .csproj necesarios
# Asume que la solución (.sln) está en el directorio padre del Dockerfile (es decir, en la raíz 'atlanticApp-back')
COPY ["FileIngestor.API/FileIngestor.API.csproj", "FileIngestor.API/"]

#  IMPORTANTE: Si el archivo .sln está en la raíz, debes copiarlo.
COPY AtlanticApp.sln AtlanticApp.sln

# Restaura las dependencias del proyecto
RUN dotnet restore "FileIngestor.API/FileIngestor.API.csproj"

# Copia el resto del código fuente
COPY . .
WORKDIR "/src/FileIngestor.API"

# Construye la aplicación
RUN dotnet build "FileIngestor.API.csproj" -c Release -o /app/build

# --- Etapa de PUBLISH ---
FROM build AS publish
RUN dotnet publish "FileIngestor.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa FINAL: Imagen de Ejecución ---
# Usa la imagen ASP.NET más ligera y segura
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Expone el puerto de Kestrel
EXPOSE 8080 
COPY --from=publish /app/publish .

# Define el punto de entrada
ENTRYPOINT ["dotnet", "FileIngestor.API.dll"]