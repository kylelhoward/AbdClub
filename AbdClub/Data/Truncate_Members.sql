-- Confirm the starting count
SELECT COUNT(*) AS count_before
FROM public."Members";

BEGIN;

TRUNCATE TABLE public."Members" RESTART IDENTITY CASCADE;
ALTER SEQUENCE public."MemberNumberSequence"
RESTART WITH 10001;

-- This should show zero
SELECT COUNT(*) AS count_after_truncate
FROM public."Members";

-- Use one:
-- ROLLBACK;
COMMIT;

-- This should show the original count again
SELECT COUNT(*) AS count_after_rollback
FROM public."Members";