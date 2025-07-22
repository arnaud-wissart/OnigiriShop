# Étape de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src/OnigiriShop

# Installation de LibMan (nécessaire pour restaurer les librairies front)
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

# Copie du fichier projet et restauration des dépendances
COPY src/OnigiriShop/OnigiriShop.csproj ./
RUN dotnet restore OnigiriShop.csproj

# Copie du reste du projet uniquement
COPY src/OnigiriShop/ ./
RUN libman restore                          
# Récupération des libs front
RUN dotnet publish -c Release -o /app/publish --no-restore

# Étape runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render fournit la variable PORT au démarrage
# L’application écoutera donc sur http://0.0.0.0:$PORT
CMD ASPNETCORE_URLS=http://+:$PORT dotnet OnigiriShop.dll
