# Member numbers and officer accounts: migration steps

After applying the code patch, generate the EF Core migration locally from the solution directory:

```powershell
dotnet ef migrations add SeparateOfficerAccountsAndMemberNumbers --project AbdClub --startup-project AbdClub
```

Before running `database update`,
open the generated migration and add the following call near the end of `Up()`,
after EF creates `OfficerAccounts`:

```csharp
migrationBuilder.Sql("""
    INSERT INTO "OfficerAccounts"
        ("Email", "GoogleSubId", "AccessLevel", "OfficerTitle", "IsEnabled", "MemberId", "CreatedAt")
    SELECT DISTINCT ON (lower("Email"))
        lower("Email"),
        "GoogleSubId",
        CASE
            WHEN "IsTechAdmin" THEN 3
            WHEN "IsAdmin" THEN 2
            ELSE 1
        END,
        "OfficerRole",
        TRUE,
        "Id",
        NOW()
    FROM "Members"
    WHERE "IsOfficer" OR "IsAdmin" OR "IsTechAdmin"
    ORDER BY lower("Email"), "IsTechAdmin" DESC, "IsAdmin" DESC, "IsOfficer" DESC, "Id";
    """);
```

Then apply and verify:

```powershell
dotnet ef database update --project AbdClub --startup-project AbdClub
dotnet ef migrations list --project AbdClub --startup-project AbdClub
```

Do not launch the updated application before this migration is applied: authentication now reads `OfficerAccounts`. The legacy role columns on `Members` remain temporarily as a compatibility bridge for dance staffing and existing reports; they are no longer used to authorize website login.
