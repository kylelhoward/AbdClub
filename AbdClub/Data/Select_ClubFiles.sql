SELECT "Id",
       "UploadedByMemberId",
       "FileName",
       "FilePath",
       "Category",
       "UploadedAt"
FROM public."ClubFiles"
LIMIT 1000;