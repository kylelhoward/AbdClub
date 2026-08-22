Install-Package Microsoft.EntityFrameworkCore.Tools

Add-Migration YourMigrationName -Project YourDataProjectName -StartupProject YourWebApiProjectName -Context YourDbContextName

Add-Migration AddDanceEventModels

Update-Database

dotnet ef migrations add AddTechAdminAdminRoles
dotnet ef database update
dotnet tool update --global dotnet-ef

dotnet ef database update --connection "Host=localhost;Database=abdclub_prod;Username=postgres;Password=YourPostgresSuperUserPasswordHere;"
