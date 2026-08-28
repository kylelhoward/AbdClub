namespace AbdClub.Models;

public class Payment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? TransactionId { get; set; }
    public string Status { get; set; } = "Completed";
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public Member Member { get; set; } = null!;
}
