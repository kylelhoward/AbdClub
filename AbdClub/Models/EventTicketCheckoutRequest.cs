namespace AbdClub.Models;

public class EventTicketCheckoutRequest
{
    public int EventId { get; set; }
    public string PurchaserName { get; set; } = string.Empty;
    public string PurchaserEmail { get; set; } = string.Empty;
    public string? PurchaserPhone { get; set; }
    public List<EventTicketSelection> Selections { get; set; } = new();
}

public class EventTicketSelection
{
    public int TicketTypeId { get; set; }
    public List<string> HolderNames { get; set; } = new();
    public List<int> MemberNumbers { get; set; } = new();
}
