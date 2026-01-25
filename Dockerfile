# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution file and restore dependencies
COPY *.sln .
COPY SWD.API/*.csproj ./SWD.API/
COPY SWD.BLL/*.csproj ./SWD.BLL/
COPY SWD.DAL/*.csproj ./SWD.DAL/
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Publish the application
WORKDIR /app/SWD.API
RUN dotnet publish -c Release -o /app/out

# Use the official .NET ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expose the port used by the application
EXPOSE 8080

# Set the entry point for the application
ENTRYPOINT ["dotnet", "SWD.API.dll"]
