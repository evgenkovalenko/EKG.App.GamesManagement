# ── restore stage (cached as long as *.csproj files don't change) ─────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY nuget.config .
COPY local-packages/ local-packages/
COPY EKG.App.GamesManagement.Model/*.csproj        EKG.App.GamesManagement.Model/
COPY EKG.App.GamesManagement.DAL/*.csproj          EKG.App.GamesManagement.DAL/
COPY EKG.App.GamesManagement.BLL/*.csproj          EKG.App.GamesManagement.BLL/
COPY EKG.App.GamesManagement.Host/*.csproj         EKG.App.GamesManagement.Host/

RUN dotnet restore "EKG.App.GamesManagement.Host/EKG.App.GamesManagement.Host.csproj"

# ── publish stage ──────────────────────────────────────────────────────────────
FROM restore AS publish
COPY . .
RUN dotnet publish "EKG.App.GamesManagement.Host/EKG.App.GamesManagement.Host.csproj" \
    -c Release -o /app/publish

# ── runtime image ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EKG.App.GamesManagement.Host.dll"]
