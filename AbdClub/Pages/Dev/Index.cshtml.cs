using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Dev;

[Authorize(Roles = "Officer")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}