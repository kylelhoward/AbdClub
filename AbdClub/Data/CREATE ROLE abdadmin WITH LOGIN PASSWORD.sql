CREATE ROLE abdadmin WITH LOGIN PASSWORD 'Adm1n001';

-- Grant connection rights to the database
GRANT CONNECT ON DATABASE abdclub TO abdadmin;

-- Grant usage and creation rights on the public schema
GRANT USAGE, CREATE ON SCHEMA public TO abdadmin;

-- (Optional) Ensure the user owns future tables created by others in public
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO abdadmin;
