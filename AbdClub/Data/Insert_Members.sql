INSERT INTO "Members" (
    "FullName",
    "Email",
    "Phone",
    "JoinDate",
    "ExpiryDate",
    "IsOfficer",
    "OfficerRole",
    "IsActive",
    "CreatedAt"
) VALUES (
    'Kyle Howard',
    'kylelhoward@gmail.com',   -- must match your Google account exactly
    NULL,
    NOW(),
    NOW() + INTERVAL '1 year',
    TRUE,
    'Admin',             -- or whatever your role is
    TRUE,
    NOW()
);