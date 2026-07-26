FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

RUN apt-get update && apt-get install -y locales \
    && sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen \
    && rm -rf /var/lib/apt/lists/*

ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Scheder.csproj", "./"]
RUN dotnet restore "Scheder.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./Scheder.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Scheder.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Scheder.dll"]
