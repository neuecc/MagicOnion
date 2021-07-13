#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Benchmark.Client/Benchmark.Client.csproj", "Benchmark.Client/"]
COPY ["Benchmark.ClientLib/Benchmark.ClientLib.csproj", "Benchmark.ClientLib/"]
RUN dotnet restore "Benchmark.Client/Benchmark.Client.csproj"
COPY . .
WORKDIR "/src/Benchmark.Client"
RUN dotnet build "Benchmark.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Benchmark.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Benchmark.Client.dll"]