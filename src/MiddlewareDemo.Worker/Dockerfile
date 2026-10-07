FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/MiddlewareDemo.Worker/MiddlewareDemo.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
RUN groupadd --system --gid 10001 worker \
    && useradd --system --uid 10001 --gid worker --no-create-home worker \
    && mkdir -p /var/worker && chown worker:worker /var/worker
COPY --from=build --chown=worker:worker /app/publish .
USER worker
ENTRYPOINT ["dotnet", "MiddlewareDemo.Worker.dll"]
