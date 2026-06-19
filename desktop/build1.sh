# Add SignalR client package to desktop
cd RunApp.Desktop
dotnet add package Microsoft.AspNetCore.SignalR.Client

# Add server packages
cd ../RunApp.Server
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package QRCoder  # For QR generation

# Run server
dotnet run

# Run desktop (new terminal)
cd ../RunApp.Desktop
dotnet run