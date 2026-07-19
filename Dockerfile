# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/MyFinances.Api/MyFinances.Api.csproj ./src/MyFinances.Api/
RUN dotnet restore ./src/MyFinances.Api/MyFinances.Api.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./src/MyFinances.Api/MyFinances.Api.csproj -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose port and start app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MyFinances.Api.dll"]
