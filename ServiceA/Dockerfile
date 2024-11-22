# Use the .NET SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the app and build it
COPY . ./
RUN dotnet publish -c Release -o /out

# Use the runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=5000
# ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5000
# EXPOSE 5001
ENTRYPOINT ["dotnet", "ServiceA.dll"]
