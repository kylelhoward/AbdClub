using AbdClub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Dev;

[Authorize(Policy = "isTechAdmin")]
public class BuildInfoModel(BuildInfoService buildInfo) : PageModel
{
    public BuildInfoService BuildInfo { get; } = buildInfo;
}
