UPDATE public."Members"
SET "ExpiryDate" = '2026-07-25'
WHERE "Id" = 1;

SELECT "Id", "FullName", "ExpiryDate" 
FROM public."Members" 
WHERE "Id" = 1;