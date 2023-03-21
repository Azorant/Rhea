FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY ["Rhea/Rhea.csproj", "Rhea/"]
RUN dotnet restore "Rhea/Rhea.csproj"
COPY . .
WORKDIR "/src/Rhea"
RUN dotnet build "Rhea.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rhea.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rhea.dll"]
