FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y ffmpeg

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SevenFridaysMusic.csproj", "./"]
RUN dotnet restore "SevenFridaysMusic.csproj"
COPY . .
RUN dotnet publish "SevenFridaysMusic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SevenFridaysMusic.dll"]