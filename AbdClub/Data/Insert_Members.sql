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
    "DanceId",
    "IsAdmin",
    "IsTechAdmin",
    "FirstName",
    "MiddleName",
    "LastName"
) VALUES (
    'kyle.lhoward@gmail.com',
    NULL,
    NULL,
    TIMESTAMPTZ '2026-08-08 13:39:12.724491-05',
    TIMESTAMPTZ '2026-08-07 19:00:00-05',
    FALSE,
    'Tech Admin',
    FALSE,
    TIMESTAMPTZ '2026-08-08 13:39:12.724491-05',
    FALSE,
    NULL,
    FALSE,
    TRUE,
    'Kyle',
    NULL,
    'Howard'
);


-- TRUNCATE TABLE public."Members" RESTART IDENTITY CASCADE