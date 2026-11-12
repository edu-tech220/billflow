FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BillFlow.sln", "."]
COPY ["src/BillFlow.Api/BillFlow.Api.csproj", "src/BillFlow.Api/"]
COPY ["src/BillFlow.Web/BillFlow.Web.csproj", "src/BillFlow.Web/"]
COPY ["tests/BillFlow.Api.Tests/BillFlow.Api.Tests.csproj", "tests/BillFlow.Api.Tests/"]
RUN dotnet restore "BillFlow.sln"
COPY . .
WORKDIR "/src/src/BillFlow.Web"
RUN dotnet build "BillFlow.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BillFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BillFlow.Web.dll"]
