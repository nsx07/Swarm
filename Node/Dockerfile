FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Node.csproj", "."]
RUN dotnet restore "Node.csproj"
COPY . .
RUN dotnet build "Node.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Node.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Swarm.Node.dll"]
