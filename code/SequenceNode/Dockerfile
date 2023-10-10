FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.17 as base
WORKDIR /src
COPY SequenceNode.csproj .
RUN dotnet restore

FROM base as code
COPY . .

FROM code as build
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0.10-alpine3.18 as prod
COPY --from=build /publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "SequenceNode.dll"]
