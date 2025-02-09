FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
WORKDIR /app
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 3400

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["Rhea.Bot/Rhea.Bot.csproj", "Rhea.Bot/"]
RUN dotnet restore "Rhea.Bot/Rhea.Bot.csproj"
COPY . .
WORKDIR "/src/Rhea.Bot"
RUN dotnet build "Rhea.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rhea.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rhea.Bot.dll"]
