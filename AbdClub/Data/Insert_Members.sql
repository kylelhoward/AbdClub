INSERT INTO "Members" (
    "FirstName",
    "MiddleName",
    "LastName",
    "Email",
    "Phone",
    "JoinDate",
    "ExpiryDate",
    "IsOfficer",
    "IsAdmin",
    "IsTechAdmin",
    "OfficerRole",
    "CreatedAt"
) VALUES (
    'Test',
    'Middle',
    'Last',
    'testLast@gmail.com',   -- must match your Google account exactly
    NULL,
    NOW(),
    NOW() + INTERVAL '1 year',
    TRUE,
    False,
    TRUE,
    'Admin',             -- or whatever your role is
    NOW()
);