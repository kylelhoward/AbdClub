

-- Note: This script is intended to be run from the default postgres maintenance database, not from abdclub_prod, abdclub_staging, or abdclub_dev. It will terminate active connections to those databases, drop the staging and dev databases, and then clone the production database into new staging and dev databases.
-- You cannot be connected to abdclub_prod, abdclub_staging, or abdclub_dev while running the script; connect to the default postgres maintenance database [1.4].No active sessions can be writing to abdclub_prod during the clone [1.4], which is why step 1 terminates active sessions.


-- 1. Terminate existing connections to staging and dev, as well as prod
SELECT pg_terminate_backend(pid) 
FROM pg_stat_activity 
WHERE datname IN ('abdclub_staging', 'abdclub_dev', 'abdclub_prod')
  AND pid <> pg_backend_pid();

-- 2. Drop the existing staging and dev databases
DROP DATABASE IF EXISTS abdclub_staging;
DROP DATABASE IF EXISTS abdclub_dev;

-- 3. Instantly clone production into staging and dev
CREATE DATABASE abdclub_staging WITH TEMPLATE abdclub_prod OWNER abdadmin_staging;
CREATE DATABASE abdclub_dev WITH TEMPLATE abdclub_prod OWNER abdadmin;

-- 4. Re-grant privileges (if needed)
GRANT ALL PRIVILEGES ON DATABASE abdclub_staging TO abdadmin_staging;
GRANT ALL PRIVILEGES ON DATABASE abdclub_dev TO abdadmin;
