-- Run this exclusively against your 'abdclub_staging' database after copying production data!

-- 1. Rewrite real member emails to safe fake addresses
UPDATE "Members"
SET "Email" = CONCAT('test_member_', "Id", '@abdclub.org')
WHERE "Email" NOT LIKE '%@yourtestingdomain.com';

-- 2. Scramble newsletter subscriber lists
UPDATE "Subscribers"
SET "Email" = CONCAT('sub_', "Id", '@abdclub.org');

-- 3. Clear existing sensitive logs
TRUNCATE TABLE "EmailLogs";
