#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["dotnet_grpc_https_bench/Benchmark.Server.Https.csproj", "dotnet_grpc_https_bench/"]
RUN dotnet restore "dotnet_grpc_https_bench/Benchmark.Server.Https.csproj"
COPY . .
WORKDIR "/src/dotnet_grpc_https_bench"
RUN dotnet build "Benchmark.Server.Https.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Benchmark.Server.Https.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Benchmark.Server.Https.dll"]