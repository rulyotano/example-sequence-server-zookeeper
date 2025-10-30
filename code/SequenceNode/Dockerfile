FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.17 AS base
WORKDIR /src
COPY SequenceNode.csproj .
RUN dotnet restore

FROM base AS code
COPY . .

FROM code AS build
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0.10-alpine3.18 AS prod
WORKDIR /app
COPY --from=build /publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "SequenceNode.dll"]
