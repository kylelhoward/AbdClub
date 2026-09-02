# Set PostgreSQL environment variables to prevent interactive password prompts
$env:PGPASSWORD = "you_are_a_superuser_password"
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



# Clean up temporary dump file
Remove-Item $backupFile
$env:PGPASSWORD = $null

Write-Host "Refresh complete!" -ForegroundColor Green
