FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV LANG=en_US.UTF-8 \
    LANGUAGE=en_US:en \
    LC_ALL=en_US.UTF-8 \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    DOTNET_NOLOGO=true

RUN apt-get update && apt-get install -y --no-install-recommends \
        wget ca-certificates apt-transport-https gnupg \
    && wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update && apt-get install -y --no-install-recommends powershell \
    && rm -f packages-microsoft-prod.deb \
    && rm -rf /var/lib/apt/lists/* 

RUN apt-get update && apt-get install -y --no-install-recommends \
        libglib2.0-0 \
        locales \
    && sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen en_US.UTF-8 \
    && update-locale LANG=en_US.UTF-8 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN pwsh /app/playwright.ps1 install chromium --with-deps \
    && rm -rf /var/lib/apt/lists/* /tmp/*

USER $APP_UID
ENTRYPOINT ["dotnet", "Scheder.dll"]


RUN pwsh /app/playwright.ps1 install chromium --with-deps \
    && rm -rf /var/lib/apt/lists/* /tmp/*

USER $APP_UID
ENTRYPOINT ["dotnet", "Scheder.dll"]