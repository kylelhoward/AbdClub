using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Members;

public class DashboardModel : PageModel
{
    private readonly AbdContext _db;
    public DashboardModel(AbdContext db) => _db = db;

    public Member Member { get; set; } = null!;
    public bool IsExpiringSoon => Member.ExpiryDate <= DateTime.UtcNow.AddDays(30);
    public bool IsExpired => Member.ExpiryDate < DateTime.UtcNow;

    public async Task OnGetAsync()
    {
        var memberId = int.Parse(User.FindFirst("MemberId")!.Value);
        Member = await _db.Members.FirstAsync(m => m.Id == memberId);
    }
}