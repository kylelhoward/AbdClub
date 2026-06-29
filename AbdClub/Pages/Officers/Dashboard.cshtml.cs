using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers;

[Authorize(Roles = "Officer")]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}