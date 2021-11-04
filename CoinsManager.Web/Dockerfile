#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["CoinsManager.Web/CoinsManager.Web.csproj", "CoinsManager.Web/"]
RUN dotnet restore "CoinsManager.Web/CoinsManager.Web.csproj"
COPY . .
WORKDIR "/src/CoinsManager.Web"
RUN dotnet build "CoinsManager.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoinsManager.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoinsManager.Web.dll"]