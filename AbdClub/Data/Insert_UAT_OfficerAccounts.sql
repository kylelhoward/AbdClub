-- Creates or updates officer accounts for the six selected members.
-- Run Insert_UAT_Board_Members.sql first.
-- Kyle's existing Tech Admin account is intentionally not touched.

BEGIN;

DO $check$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM (VALUES (10047), (10023), (10052), (10084), (10037), (10109))
             AS required("MemberNumber")
        LEFT JOIN public."Members" AS m
            ON m."MemberNumber" = required."MemberNumber"
        WHERE m."Id" IS NULL
    ) THEN
        RAISE EXCEPTION
            'One or more required members are missing. Run Insert_UAT_Board_Members.sql first.';
    END IF;
END
$check$;

WITH desired ("Email", "AccessLevel", "OfficerTitle", "MemberNumber") AS (
    VALUES
        ('cherynnetz@gmail.com',                         1, 'President',      10047),
        ('test_julielaursen1@gmail.com.invalid',         2, 'Treasurer',      10023),
        ('test_dr.jam.jenkins@gmail.com.invalid',        1, 'Past President', 10052),
        ('test_annmichael@ymail.com.invalid',            1, 'Vice President', 10084),
        ('test_gageorgakis@gmail.com.invalid',           2, 'Secretary',      10037),
        ('test_sosograd02@yahoo.com.invalid',            1, 'Board Member',   10109)
)
INSERT INTO public."OfficerAccounts" (
    "Email",
    "GoogleSubId",
    "AccessLevel",
    "OfficerTitle",
    "IsEnabled",
    "MemberId",
    "CreatedAt"
)
SELECT
    desired."Email",
    NULL,
    desired."AccessLevel",
    desired."OfficerTitle",
    TRUE,
    m."Id",
    CURRENT_TIMESTAMP
FROM desired
JOIN public."Members" AS m
    ON m."MemberNumber" = desired."MemberNumber"
ON CONFLICT ("Email") DO UPDATE
SET
    "AccessLevel" = EXCLUDED."AccessLevel",
    "OfficerTitle" = EXCLUDED."OfficerTitle",
    "IsEnabled" = TRUE,
    "MemberId" = EXCLUDED."MemberId";

COMMIT;

SELECT
    oa."Id",
    oa."Email",
    oa."AccessLevel",
    oa."OfficerTitle",
    oa."IsEnabled",
    oa."MemberId",
    m."MemberNumber"
FROM public."OfficerAccounts" AS oa
JOIN public."Members" AS m ON m."Id" = oa."MemberId"
ORDER BY oa."Id";
