FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY out/ .
ENV ASPNETCORE_URLS=http://*:80
EXPOSE 80
USER $APP_UID
ENTRYPOINT ["dotnet", "MiddlewareDemo.Api.dll"]
