FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY out/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MiddlewareDemo.Api.dll"]
