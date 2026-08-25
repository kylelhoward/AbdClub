using AbdClub.Models;
using System.Security.Claims;

namespace AbdClub.Services;

public static class OfficerClaimsFactory
{
    public static IEnumerable<Claim> Create(OfficerAccount account)
    {
        var displayName = account.Member?.FullName ?? account.Email;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, account.Email),
            new("OfficerAccountId", account.Id.ToString()),
            new(ClaimTypes.Role, "Officer")
        };

        if (account.MemberId.HasValue)
            claims.Add(new("MemberId", account.MemberId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(account.OfficerTitle))
            claims.Add(new("OfficerRole", account.OfficerTitle));
        if (account.AccessLevel >= OfficerAccessLevel.Admin)
            claims.Add(new(ClaimTypes.Role, "Admin"));
        if (account.AccessLevel == OfficerAccessLevel.TechAdmin)
            claims.Add(new(ClaimTypes.Role, "TechAdmin"));

        return claims;
    }
}
