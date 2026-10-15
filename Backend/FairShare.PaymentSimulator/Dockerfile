# FairShare payment simulator - container image.
# Build context: this folder (FairShare.PaymentSimulator). It shares no code with the API.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY FairShare.PaymentSimulator.csproj .
RUN dotnet restore FairShare.PaymentSimulator.csproj
COPY . .
RUN dotnet publish FairShare.PaymentSimulator.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FairShare.PaymentSimulator.dll"]
