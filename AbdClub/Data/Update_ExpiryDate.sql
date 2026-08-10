UPDATE abdclub."Members"
SET "ExpiryDate" = '2026-07-25'
WHERE "Id" = 1;

SELECT "Id", "FullName", "ExpiryDate" 
FROM abdclub."Members" 
WHERE "Id" = 1;