-- Adds or updates the six selected UAT members.
-- Safe to rerun: MemberNumber is the stable conflict key.
-- Internal Members.Id values are assigned by the Ubuntu database.

BEGIN;

INSERT INTO public."Members" (
    "Email",
    "Phone",
    "GoogleSubId",
    "JoinDate",
    "ExpiryDate",
    "IsOfficer",
    "OfficerRole",
    "IsSuspended",
    "CreatedAt",
    "SelfRegistered",
    "IsAdmin",
    "IsTechAdmin",
    "FirstName",
    "MiddleName",
    "LastName",
    "MemberNumber",
    "Notes"
)
VALUES
    ('cherynnetz@gmail.com', '6012785889', NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.11436-05', TIMESTAMPTZ '2027-03-14 19:00:00-05',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.11436-05', FALSE, FALSE, FALSE,
     'Cheryn', NULL, 'Howard', 10047, NULL),

    ('test_julielaursen1@gmail.com.invalid', NULL, NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.11432-05', TIMESTAMPTZ '2027-06-29 19:00:00-05',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.11432-05', FALSE, FALSE, FALSE,
     'Julie', NULL, 'Coleman', 10023, NULL),

    ('test_gageorgakis@gmail.com.invalid', NULL, NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.114348-05', TIMESTAMPTZ '2026-12-07 18:00:00-06',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.114348-05', FALSE, FALSE, FALSE,
     'Georgios', NULL, 'Georgakis', 10037, NULL),

    ('test_dr.jam.jenkins@gmail.com.invalid', NULL, NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.114365-05', TIMESTAMPTZ '2027-08-21 19:00:00-05',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.114365-05', FALSE, FALSE, FALSE,
     'Christopher(Jam)', NULL, 'Jenkins', 10052, 'renewed at dance on 8/22/2026'),

    ('test_annmichael@ymail.com.invalid', '7132019799', NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.114398-05', TIMESTAMPTZ '2028-07-30 19:00:00-05',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.114398-05', FALSE, FALSE, FALSE,
     'Ann', NULL, 'Michael', 10084,
     'since we owed her money instead of reimbursing her we extended her membership to july 31, 2028 for an in-kind reimbursement'),

    ('test_sosograd02@yahoo.com.invalid', NULL, NULL,
     TIMESTAMPTZ '2026-08-29 14:49:56.114422-05', TIMESTAMPTZ '2027-06-04 19:00:00-05',
     FALSE, NULL, FALSE, TIMESTAMPTZ '2026-08-29 14:49:56.114422-05', FALSE, FALSE, FALSE,
     'Jen', NULL, 'Soto', 10109, NULL)
ON CONFLICT ("MemberNumber") DO UPDATE
SET
    "Email" = EXCLUDED."Email",
    "Phone" = EXCLUDED."Phone",
    "JoinDate" = EXCLUDED."JoinDate",
    "ExpiryDate" = EXCLUDED."ExpiryDate",
    "IsOfficer" = FALSE,
    "OfficerRole" = NULL,
    "IsSuspended" = EXCLUDED."IsSuspended",
    "SelfRegistered" = EXCLUDED."SelfRegistered",
    "IsAdmin" = FALSE,
    "IsTechAdmin" = FALSE,
    "FirstName" = EXCLUDED."FirstName",
    "MiddleName" = EXCLUDED."MiddleName",
    "LastName" = EXCLUDED."LastName",
    "Notes" = EXCLUDED."Notes";

-- Keep the member-number sequence above every explicitly supplied number.
SELECT setval(
    'public."MemberNumberSequence"',
    GREATEST((SELECT MAX("MemberNumber") FROM public."Members"), 10000),
    TRUE
);

COMMIT;

SELECT "Id", "MemberNumber", "FirstName", "LastName", "Email"
FROM public."Members"
WHERE "MemberNumber" IN (10023, 10037, 10047, 10052, 10084, 10109)
ORDER BY "MemberNumber";
