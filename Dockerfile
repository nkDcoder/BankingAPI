# Use the official .NET SDK image as a base image for building
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the project files to the container
COPY . .

# Build the application
RUN dotnet publish -c Release -o out

# Use the official ASP.NET image as a base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Set the working directory inside the container
WORKDIR /app

# Copy the published output from the build stage to the container
COPY --from=build /app/out .

# Expose the port that your application will run on
EXPOSE 80

# Command to run your application
ENTRYPOINT ["dotnet", "BankingAPI.dll"]