# syntax=docker/dockerfile:1

# ---- restore + build (shared by both final images below) ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY AdminPlatform.sln ./
COPY src/BuildingBlocks/AdminPlatform.SharedKernel/AdminPlatform.SharedKernel.csproj src/BuildingBlocks/AdminPlatform.SharedKernel/
COPY src/BuildingBlocks/AdminPlatform.Common/AdminPlatform.Common.csproj src/BuildingBlocks/AdminPlatform.Common/
COPY src/Modules/Identity/AdminPlatform.Modules.Identity/AdminPlatform.Modules.Identity.csproj src/Modules/Identity/AdminPlatform.Modules.Identity/
COPY src/Modules/AccessControl/AdminPlatform.Modules.AccessControl/AdminPlatform.Modules.AccessControl.csproj src/Modules/AccessControl/AdminPlatform.Modules.AccessControl/
COPY src/Modules/Organization/AdminPlatform.Modules.Organization/AdminPlatform.Modules.Organization.csproj src/Modules/Organization/AdminPlatform.Modules.Organization/
COPY src/Modules/Navigation/AdminPlatform.Modules.Navigation/AdminPlatform.Modules.Navigation.csproj src/Modules/Navigation/AdminPlatform.Modules.Navigation/
COPY src/Modules/Platform/AdminPlatform.Modules.Platform/AdminPlatform.Modules.Platform.csproj src/Modules/Platform/AdminPlatform.Modules.Platform/
COPY src/Host/AdminPlatform.Api/AdminPlatform.Api.csproj src/Host/AdminPlatform.Api/
COPY src/Tools/AdminPlatform.Migrator/AdminPlatform.Migrator.csproj src/Tools/AdminPlatform.Migrator/

# Restore only the two things we actually publish — pulls in every project they reference.
RUN dotnet restore src/Host/AdminPlatform.Api/AdminPlatform.Api.csproj
RUN dotnet restore src/Tools/AdminPlatform.Migrator/AdminPlatform.Migrator.csproj

COPY src/ src/

RUN dotnet publish src/Host/AdminPlatform.Api/AdminPlatform.Api.csproj -c Release -o /app/api --no-restore
RUN dotnet publish src/Tools/AdminPlatform.Migrator/AdminPlatform.Migrator.csproj -c Release -o /app/migrator --no-restore

# ---- runtime: API ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
RUN useradd --uid 5678 --user-group --no-create-home appuser
COPY --from=build /app/api ./
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AdminPlatform.Api.dll"]

# ---- runtime: Migrator (deployment job, not a long-running service) ----
# Uses the aspnet image, not the plain runtime image: the module DLLs it loads have a
# FrameworkReference on Microsoft.AspNetCore.App (for AddControllers/DI plumbing shared with the API).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS migrator
WORKDIR /app
RUN useradd --uid 5678 --user-group --no-create-home appuser
COPY --from=build /app/migrator ./
USER appuser
ENTRYPOINT ["dotnet", "AdminPlatform.Migrator.dll"]
