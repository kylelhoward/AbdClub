Install-Package Microsoft.EntityFrameworkCore.Tools

Add-Migration YourMigrationName -Project YourDataProjectName -StartupProject YourWebApiProjectName -Context YourDbContextName

Add-Migration AddDanceEventModels 

Update-Database

dotnet ef migrations add RenameTransactionId
dotnet ef database update
dotnet tool update --global dotnet-ef
