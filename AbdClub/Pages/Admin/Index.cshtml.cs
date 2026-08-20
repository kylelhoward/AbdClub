using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Admin;

[Authorize(Policy = "isTechAdmin")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}