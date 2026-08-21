-- Irreversibly removes all table data from abdclub_production.
-- Run while connected directly to the target database.
-- EF migration history is retained so the existing schema remains tracked.

DO $empty_production$
DECLARE
    table_list text;
BEGIN
    IF current_database() <> 'abdclub_production' THEN
        RAISE EXCEPTION 'Refusing to empty database "%". Connect to abdclub_production first.', current_database();
    END IF;

    SELECT string_agg(format('%I.%I', table_schema, table_name), ', ' ORDER BY table_name)
    INTO table_list
    FROM information_schema.tables
    WHERE table_schema = 'public'
            AND table_type = 'BASE TABLE'
            AND table_name <> '__EFMigrationsHistory';

    IF table_list IS NOT NULL THEN
        EXECUTE format('TRUNCATE TABLE %s RESTART IDENTITY CASCADE', table_list);
    END IF;
END
$empty_production$;