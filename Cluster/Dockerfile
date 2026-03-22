FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Cluster.csproj", "."]
RUN dotnet restore "Cluster.csproj"
COPY . .
RUN dotnet build "Cluster.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cluster.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "Swarm.Cluster.dll"]
