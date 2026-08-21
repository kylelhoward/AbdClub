CREATE ROLE abdadmin WITH LOGIN PASSWORD 'Adm1n001';

-- Grant connection rights to the database
GRANT CONNECT ON DATABASE abdclub TO abdadmin;

-- Grant usage and creation rights on the public schema
GRANT USAGE, CREATE ON SCHEMA public TO abdadmin;

-- (Optional) Ensure the user owns future tables created by others in public
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO abdadmin;

-- -- 🌟 THE COMPREHENSIVE SECURITY FIX: 
-- -- Tells Postgres that no matter WHO creates a new table in public, abdadmin always gets ALL rights immediately!
-- ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON TABLES TO abdadmin;

--  abdadmin_staging-------------------------------------------------
CREATE ROLE abdadmin_staging WITH LOGIN PASSWORD 'Adm1n002Stag!ng';

-- Grant connection rights to the database
GRANT CONNECT ON DATABASE abdclub_staging  TO abdadmin_staging ;

-- Grant usage and creation rights on the public schema
GRANT USAGE, CREATE ON SCHEMA public TO abdadmin_staging ;

-- (Optional) Ensure the user owns future tables created by others in public
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO abdadmin_staging ;

--  abdadmin_prod-------------------------------------------------
CREATE ROLE abdadmin_prod WITH LOGIN PASSWORD 'Adm1n003Pr0d';

-- Grant connection rights to the database
GRANT CONNECT ON DATABASE abdclub_production  TO abdadmin_prod ;

-- Grant usage and creation rights on the public schema
GRANT USAGE, CREATE ON SCHEMA public TO abdadmin_prod ;

-- (Optional) Ensure the user owns future tables created by others in public
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO abdadmin_prod ;