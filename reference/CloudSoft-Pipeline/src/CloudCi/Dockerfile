# Build stage — full SDK so we can compile and publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first so layer caching kicks in on dependency changes.
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage — only the ASP.NET runtime, no build tools.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Bake the build commit SHA into the image at build time.
ARG BUILD_SHA=local
ENV BUILD_SHA=$BUILD_SHA

# Listen on :8080 — Container Apps maps this to the public ingress.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudCi.dll"]
