using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;

namespace AbdClub.Pages.Dev;

[Authorize(Policy = "isAdmin")]
public class AuditLogsModel : PageModel
{
    private readonly AbdContext _context;

    public AuditLogsModel(AbdContext context)
    {
        _context = context;
    }

       public List<AuditLogEntry> Logs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SelectedLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public const int PageSize = 25;

    // Inside Pages/Admin/AuditLogs.cshtml.cs:

    public class AuditLogEntry
    {
        // 🌟 REFACTORED: Keeps an Id property for the HTML loop layout, but handles it as a virtual property
        public long Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Exception { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentPage < 1) CurrentPage = 1;

        try
        {
            // 🌟 FIXED SQL QUERY: Uses PostgreSQL ROW_NUMBER() window function to create a virtual tracking ID on-the-fly!
            // This completely bypasses the missing physical primary key column error.
            var query = _context.Database.SqlQueryRaw<AuditLogEntry>(
                "SELECT ROW_NUMBER() OVER(ORDER BY timestamp ASC) AS \"Id\", message AS \"Message\", CAST(level AS text) AS \"Level\", timestamp AS \"Timestamp\", exception AS \"Exception\" FROM public.\"Logs\""
            ).AsQueryable();

            if (!string.IsNullOrEmpty(SelectedLevel))
            {
                // Note: If level column saves as an integer (Serilog default enum tracking), convert filter checks
                int enumLevel = SelectedLevel == "Information" ? 2 :
                                SelectedLevel == "Warning" ? 3 :
                                SelectedLevel == "Error" ? 4 : 5;

                query = query.Where(l => l.Level == enumLevel.ToString());
            }

            int totalLogs = query.Count();
            TotalPages = (int)Math.Ceiling((double)totalLogs / PageSize);

            // Fetch logs sorted newest to oldest
            var rawLogs = query
                .OrderByDescending(l => l.Timestamp)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Standardize the level column strings for your HTML view badges
            Logs = rawLogs.Select(l => new AuditLogEntry
            {
                Id = l.Id,
                Message = l.Message,
                Timestamp = l.Timestamp,
                Exception = l.Exception,
                Level = l.Level == "2" ? "Information" :
                        l.Level == "3" ? "Warning" :
                        l.Level == "4" ? "Error" :
                        l.Level == "5" ? "Fatal" : l.Level
            }).ToList();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
            Logs = new List<AuditLogEntry>();
            TotalPages = 0;
        }

        return Page();
    }




///////
}
