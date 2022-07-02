#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Discord.InviteFilter/Discord.InviteFilter.csproj", "Discord.InviteFilter/"]
RUN dotnet restore "Discord.InviteFilter/Discord.InviteFilter.csproj"
COPY . .
WORKDIR "/src/Discord.InviteFilter"
RUN dotnet build "Discord.InviteFilter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Discord.InviteFilter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Discord.InviteFilter.dll"]