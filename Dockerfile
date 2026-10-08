# ==============================================================================
# Sistema CRUD Assistência - Dockerfile multi-stage
# Etapa 1: build com a imagem SDK (.NET 10)
# Etapa 2: runtime com a imagem ASP.NET (usuário não-root)
# Migrations NÃO são executadas aqui: a própria aplicação aplica (ou não),
# conforme "Aplicacao:AplicarMigrationsAutomaticamente" e o ambiente.
# ==============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projeto primeiro para preservar o cache de restore
COPY src/SistemaCrudAssistencia/SistemaCrudAssistencia.csproj src/SistemaCrudAssistencia/
RUN dotnet restore src/SistemaCrudAssistencia/SistemaCrudAssistencia.csproj

COPY src/SistemaCrudAssistencia/ src/SistemaCrudAssistencia/
RUN dotnet publish src/SistemaCrudAssistencia/SistemaCrudAssistencia.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Usuário não-root (APP_UID=1645 nas imagens oficiais .NET 8+)
USER $APP_UID

ENTRYPOINT ["dotnet", "SistemaCrudAssistencia.dll"]
