Install-Package Microsoft.EntityFrameworkCore.Tools

Add-Migration YourMigrationName -Project YourDataProjectName -StartupProject YourWebApiProjectName -Context YourDbContextName

Add-Migration AddDanceEventModels 

Update-Database
