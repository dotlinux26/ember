FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Ember.RoomBroker.csproj .
RUN dotnet restore
COPY Program.cs .
RUN dotnet publish -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 27842/udp
ENTRYPOINT ["dotnet","Ember.RoomBroker.dll"]
