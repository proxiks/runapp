# 1. Create solution
dotnet new sln -n RunApp
dotnet new webapi -n RunApp.Server
dotnet new wpf -n RunApp.Desktop

# 2. Add projects to solution
dotnet sln add RunApp.Server/RunApp.Server.csproj
dotnet sln add RunApp.Desktop/RunApp.Desktop.csproj

# 3. Add packages to server
cd RunApp.Server
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package MailKit

# 4. Add packages to desktop
cd ../RunApp.Desktop
dotnet add package System.Net.Http.Json

# 5. Run server
cd ../RunApp.Server
dotnet run

# 6. Run desktop (new terminal)
cd ../RunApp.Desktop
dotnet run