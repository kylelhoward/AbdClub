namespace AbdClub.Models;

public enum MembershipPlan
{
    Individual = 1,
    Couple = 2
}

public class MembershipCheckoutRequest
{
    public MembershipPlan Plan { get; set; } = MembershipPlan.Individual;
    public string FirstName1 { get; set; } = string.Empty;
    public string LastName1 { get; set; } = string.Empty;
    public string Email1 { get; set; } = string.Empty;
    public string? Phone1 { get; set; }
    public string? FirstName2 { get; set; }
    public string? LastName2 { get; set; }
    public string? Email2 { get; set; }
    public string? Phone2 { get; set; }
    public bool SharedEmail { get; set; } = true;
}
