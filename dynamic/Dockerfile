FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY dynamic/AiBusinessDirectory.Web/AiBusinessDirectory.Web.csproj dynamic/AiBusinessDirectory.Web/
RUN dotnet restore dynamic/AiBusinessDirectory.Web/AiBusinessDirectory.Web.csproj
COPY dynamic/AiBusinessDirectory.Web/ dynamic/AiBusinessDirectory.Web/
COPY index.html sitemap.xml google33bc54793ff2be2c.html ./
COPY assets/styles.css assets/styles.css
COPY businesses/golden-harbor.html businesses/golden-harbor.html
COPY comparisons/golden-harbor-vs-asm.html comparisons/golden-harbor-vs-asm.html
RUN dotnet publish dynamic/AiBusinessDirectory.Web/AiBusinessDirectory.Web.csproj -c Release --no-restore -p:DebugType=None -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p /app/data && chown -R app:app /app/data
ENV ASPNETCORE_ENVIRONMENT=Production \
    Storage__DatabasePath=/app/data/listing-requests.test.db
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["dotnet", "AiBusinessDirectory.Web.dll"]
