using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Dev;

[Authorize(Policy = "isTechAdmin")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}