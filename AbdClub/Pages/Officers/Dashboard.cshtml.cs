using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers;

[Authorize(Policy = "isOfficer")]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}