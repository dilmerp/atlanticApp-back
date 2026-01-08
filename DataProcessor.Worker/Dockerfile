# Usa la imagen SDK para construir (Etapa de BUILD)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia la solución y los archivos .csproj necesarios
COPY ["DataProcessor.Worker/DataProcessor.Worker.csproj", "DataProcessor.Worker/"]
COPY AtlanticApp.sln AtlanticApp.sln

# Restaura las dependencias
RUN dotnet restore "DataProcessor.Worker/DataProcessor.Worker.csproj"

# Copia el resto del código y construye
COPY . .
WORKDIR "/src/DataProcessor.Worker"
RUN dotnet build "DataProcessor.Worker.csproj" -c Release -o /app/build

# --- Etapa de PUBLISH ---
FROM build AS publish
# El worker no necesita UseAppHost en Linux
RUN dotnet publish "DataProcessor.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa FINAL: Imagen de Ejecución ---
# Usa la imagen Runtime más ligera (no necesita las bibliotecas web completas)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final 
WORKDIR /app
COPY --from=publish /app/publish .

# Define el punto de entrada
ENTRYPOINT ["dotnet", "DataProcessor.Worker.dll"]