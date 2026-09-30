# Build context
FROM node:22 AS web
WORKDIR /src/Dappi.Portal.Web
COPY Dappi.Portal.Web/ ./
RUN npm ci && npm run build

# The SDK image, because the portal runs `dappi init`, `dotnet ef` and git to generate apps
FROM mcr.microsoft.com/dotnet/sdk:9.0
RUN dotnet tool install --global dotnet-ef --version 9.0.6
RUN dotnet tool install --global Dappi.Cli --prerelease
# dappi init caches templates in ~/.config/Dappi and breaks if ~/.config doesn't exist
RUN mkdir -p /root/.config
ENV PATH="$PATH:/root/.dotnet/tools" ASPNETCORE_HTTP_PORTS=8080
WORKDIR /src
COPY Dappi.Portal.Api/ ./
COPY --from=web /src/Dappi.Portal.Api/wwwroot ./wwwroot
RUN dotnet publish -c Release -o /app
WORKDIR /app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dappi.Portal.Api.dll"]
