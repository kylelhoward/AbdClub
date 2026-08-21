# Set PostgreSQL environment variables to prevent interactive password prompts
$env:PGPASSWORD = "sub3630!"
$pgHost = "localhost"
$pgPort = "5432"
$pgUser = "postgres"

$backupFile = "$PSScriptRoot\abdclub_temp.sql"

Write-Host "1. Dumping production database..." -ForegroundColor Cyan
pg_dump -h $pgHost -p $pgPort -U $pgUser -d abdclub --clean --if-exists -F p -f $backupFile

Write-Host "2. Restoring into staging (abdclub_staging)..." -ForegroundColor Cyan
psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_staging -f $backupFile

Write-Host "3. Restoring into dev (abdclub_dev)..." -ForegroundColor Cyan
psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_dev -f $backupFile

Write-Host "3.1. Restoring into production (abdclub_production)..." -ForegroundColor Cyan
psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_production -f $backupFile

# Write-Host "4. Running sanitization on Staging..." -ForegroundColor Cyan
# psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_staging -c "
#     UPDATE ""Members"" SET ""Email"" = CONCAT('test_member_', ""Id"", '@abdclub.org') WHERE ""Email"" NOT LIKE '%@yourtestingdomain.com';
#     UPDATE ""Subscribers"" SET ""Email"" = CONCAT('sub_', ""Id"", '@abdclub.org');
#     TRUNCATE TABLE ""EmailLogs"";
# "

# Clean up temporary dump file
Remove-Item $backupFile
$env:PGPASSWORD = $null

Write-Host "Refresh complete!" -ForegroundColor Green
