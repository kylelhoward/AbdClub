# Set PostgreSQL environment variables to prevent interactive password prompts
# $env:PGPASSWORD = "YourPostgresSuperuserPassword"
$env:PGPASSWORD = "sub3630!"
$pgHost = "localhost"
$pgPort = "5432"
$pgUser = "postgres"

$backupFile = "$PSScriptRoot\abdclub_prod_temp.sql"

Write-Host "1. Dumping production database..." -ForegroundColor Cyan
pg_dump -h $pgHost -p $pgPort -U $pgUser -d abdclub_production --clean --if-exists -F p -f $backupFile

Write-Host "2. Restoring into staging (abdclub_staging)..." -ForegroundColor Cyan
psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_staging -f $backupFile

Write-Host "3. Restoring into dev (abdclub_dev)..." -ForegroundColor Cyan
psql -h $pgHost -p $pgPort -U $pgUser -d abdclub_dev -f $backupFile

Write-Host "4. Running sanitization on Dev..." -ForegroundColor Cyan

$sanitizeSql = @'
UPDATE public."Members"
SET "Email" =
    'test_' ||
    SPLIT_PART("Email", '@', 1) ||
    '@' ||
    SPLIT_PART("Email", '@', 2) ||
    '.invalid'
WHERE "Email" IS NOT NULL
  AND "Email" LIKE '%@%'
  AND "Email" NOT LIKE 'test_%@%.invalid';

UPDATE public."NewsletterSubscribers"
SET "Email" =
    'test_' ||
    SPLIT_PART("Email", '@', 1) ||
    '@' ||
    SPLIT_PART("Email", '@', 2) ||
    '.invalid'
WHERE "Email" IS NOT NULL
  AND "Email" LIKE '%@%'
  AND "Email" NOT LIKE 'test_%@%.invalid';

TRUNCATE TABLE public."EmailLogs";
'@

$sanitizeSql | psql `
    -h $pgHost `
    -p $pgPort `
    -U $pgUser `
    -d abdclub_dev `
    -v ON_ERROR_STOP=1



# Clean up temporary dump file
Remove-Item $backupFile
$env:PGPASSWORD = $null

Write-Host "Refresh complete!" -ForegroundColor Green
