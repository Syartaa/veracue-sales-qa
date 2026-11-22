# VeraCue API — container image for Render.
# Multi-stage build: the large SDK image compiles the app, the small ASP.NET runtime image runs it.

# ---------- 1. Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project file, so Docker can cache the NuGet packages
# and skip this step when only source code changes.
COPY VeraCue.Api/VeraCue.Api.csproj VeraCue.Api/
RUN dotnet restore VeraCue.Api/VeraCue.Api.csproj

COPY VeraCue.Api/ VeraCue.Api/
RUN dotnet publish VeraCue.Api/VeraCue.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- 2. Run ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as the image's built-in non-root user (safer than root).
USER $APP_UID

# Render sets PORT (default 10000) and Program.cs listens on it.
# 8080 is the fallback when running the image elsewhere.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VeraCue.Api.dll"]
