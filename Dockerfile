# ---------- Etapa 1: compilar (imagen grande, con el SDK) ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primero solo los .csproj: si no cambian, Docker reutiliza el restore (más rápido)
COPY src/Domain/Domain.csproj               src/Domain/
COPY src/Application/Application.csproj     src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj                     src/Api/
RUN dotnet restore src/Api/Api.csproj

# Después el resto del código, y publicar
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app --no-restore

# ---------- Etapa 2: ejecutar (imagen pequeña, solo el runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Api.dll"]