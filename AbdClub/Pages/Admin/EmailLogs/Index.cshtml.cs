using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Admin.EmailLogs;

[Authorize(Policy = "isAdmin")]
public class IndexModel : PageModel
{
    private readonly AbdContext _db;

    public IndexModel(AbdContext db)
    {
        _db = db;
    }

    public List<EmailLog> Logs { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterType { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }
    public const int PageSize = 20;

    public async Task OnGetAsync()
    {
        var query = _db.EmailLogs
            .Include(e => e.Member)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim().ToLower();
            query = query.Where(e =>
                e.RecipientEmail.ToLower().Contains(search) ||
                e.Subject.ToLower().Contains(search) ||
                e.TriggeredBy.ToLower().Contains(search) ||
                (e.Member != null && (e.Member.FirstName.ToLower().Contains(search) || e.Member.LastName.ToLower().Contains(search)))
            );
        }

        if (!string.IsNullOrWhiteSpace(FilterType))
        {
            query = query.Where(e => e.EmailType == FilterType);
        }

        int totalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Logs = await query
            .OrderByDescending(e => e.SentAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}

