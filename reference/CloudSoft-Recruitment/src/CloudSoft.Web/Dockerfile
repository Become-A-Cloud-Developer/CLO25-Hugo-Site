FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG VERSION=1.0.0

COPY CloudSoft.Web.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish -p:InformationalVersion=$VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data/keys && chown -R $APP_UID /app/data

COPY --from=build /app/publish .

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CloudSoft.Web.dll"]
