FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# LocalFileBlobService writes uploads to ContentRoot/uploads at runtime.
# /app is owned by root, so create the directory and chown it to the non-root
# 'app' user before dropping privileges. (Replaced by AzureBlobService in the
# next exercise; harmless to keep either way.)
RUN mkdir -p /app/uploads && chown app:app /app/uploads

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER app

ENTRYPOINT ["dotnet", "CloudCiCareers.Web.dll"]
