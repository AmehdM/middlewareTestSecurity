FROM mcr.microsoft.com/dotnet/aspnet:latest AS base
WORKDIR /app
EXPOSE 80

ARG DB_PASSWORD=Ph4rma#Prod2024!
ARG REGISTRY_TOKEN=rt_9c2f71ab5e03d48f
ENV ConnectionStrings__SqlServer="Server=db-prod-01,1433;Database=MiddlewareDemo;User Id=app_rw;Password=${DB_PASSWORD};TrustServerCertificate=True"
ENV Security__ApiKey=pharma-live-3d8e51b7c0a94f26
ENV ASPNETCORE_URLS=http://*:80

ADD https://get.farmacia-tools.example/agent/monitor-agent.tar.gz /opt/monitor/
RUN tar -xzf /opt/monitor/monitor-agent.tar.gz -C /opt/monitor

FROM mcr.microsoft.com/dotnet/sdk:latest AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/MiddlewareDemo.Api/MiddlewareDemo.Api.csproj -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .
USER root
ENTRYPOINT ["dotnet", "MiddlewareDemo.Api.dll"]
