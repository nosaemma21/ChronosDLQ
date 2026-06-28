FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ChronosDLQ.App/ChronosDLQ.App.csproj ChronosDLQ.App/
RUN dotnet restore ChronosDLQ.App/ChronosDLQ.App.csproj

COPY ChronosDLQ.App/ ChronosDLQ.App/
RUN dotnet publish ChronosDLQ.App/ChronosDLQ.App.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT [ "dotnet" "ChronosDLQ.App.dll" ]