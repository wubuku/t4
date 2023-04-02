#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /apps

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
#COPY ["nuget.config", "."]
COPY ["dotnet-t4/dotnet-t4.csproj", "dotnet-t4/"]
COPY ["Mono.TextTemplating/Mono.TextTemplating.csproj", "Mono.TextTemplating/"]
COPY ["T4Toolbox.VSHostLites/T4Toolbox.VSHostLites.csproj", "T4Toolbox.VSHostLites/"]
RUN dotnet restore "dotnet-t4/dotnet-t4.csproj"
COPY . .
WORKDIR "/src/dotnet-t4"
RUN dotnet build "dotnet-t4.csproj" -c Release -o /apps/dotnet-t4/build

FROM build AS publish
RUN dotnet publish "dotnet-t4.csproj" -c Release -o /apps/dotnet-t4/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /apps/dotnet-t4
COPY --from=publish /apps/dotnet-t4/publish .
ENTRYPOINT ["dotnet", "t4.dll", "--help"]

