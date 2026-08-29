using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AbdClub.Pages.Officers.Dances;

public class AttendingOfficersModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly AbdContext _context;
    private readonly ILogger<AttendingOfficersModel> _logger;
    private readonly IEmailService _emailService;
    public AttendingOfficersModel(
        AbdContext context,
        IAuthorizationService authorizationService,
        ILogger<AttendingOfficersModel> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _authorizationService = authorizationService;
        QuestPDF.Settings.License = LicenseType.Community;
        _emailService = emailService;

    }


    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        var authResult = await _authorizationService
            .AuthorizeAsync(User, null, "isOfficer");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        // Fetch dance with complete related tracking contexts
        var dance = await _context.Events.OfType<Dance>()
         .Include(d => d.Location)
         .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
         .Include(d => d.AssignedDj)

            // 🌟 THE FIX: Eager-load your single lesson, then reach inside to extract its instructor profile
            .Include(d => d.AssignedLesson)
                .ThenInclude(l => l.Instructor)

         .Include(d => d.AssignedHosts)       // 👈 Named precisely to match your model
         .Include(d => d.AssignedVolunteers)
         .FirstOrDefaultAsync(d => d.Id == id);

        if (dance == null) return NotFound();

        #region Construct the document stream using Fluent fluid layouts
        var documentPdf = Document
            .Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                #region Document Header Block Area
                page
                .Header()
                .Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.Span("ABD CLUB NIGHT-OF COORDINATION LOG")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Grey.Darken3);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span($"Event: {dance.Title}")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                    });

                    column.Item().Text(text =>
                    {
                        text.Span($"Date: {dance.Date:MMMM dd, yyyy} | Venue Location: {dance.Location.VenueName}")
                            .FontSize(11)
                            .Italic();
                    });

                    column.Item().PaddingTop(10).LineHorizontal(1, Unit.Point);
                });
                #endregion Document Header Block Area


                #region Document Body Workspace Content Grid
                page
                .Content()
                .PaddingTop(15)
                .Column(column =>
                {
                    #region Segment A: Attending Duty Officers Roster
                    column
                        .Item()
                        .Text(text =>
                        {
                            text.Span("1. Scheduled Duty Officers")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Grey.Darken2);
                        });

                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2); // Check-in Column Box Space
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Officer Name").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Role").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Status").Bold();
                        });

                        foreach (var officer in dance.AttendingOfficers.OrderBy(o => o.LastName))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{officer.LastName}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(officer.OfficerAccount?.OfficerTitle ?? "Staff Officer");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("[   ] Present");
                        }
                    });
                    #endregion

                    #region 🌟 NEW SEGMENT B: SCHEDULED EVENT PROFESSIONALS & SPECIAL ROLES
                column
                    .Item()
                    .PaddingTop(25)
                    .Text(text =>
                    {
                        text.Span("2. Scheduled Event Professionals & Dance Hosts")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);
                    });

                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Event Staff Assignment").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Scheduled Professional / Host").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Status").Bold();
                        });

                        // 🌟 UPDATED: Directly evaluates the single instructor linked via your 1:1 lesson model
                        if (dance.AssignedLesson != null && dance.AssignedLesson.Instructor != null)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Dance Instructor").Bold();
                            // Assuming your MasterInstructor model class uses .FullName or .Name
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(dance.AssignedLesson.Instructor.Name);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("[  ] Present");
                        }
                        else
                        {
                            // High-visibility fallback alert if no lesson or instructor is currently booked to the date
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Dance Instructor").Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("VACANT / UNASSIGNED").FontColor(Colors.Red.Medium);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("[  ] Recruits Needed");
                        }


                        // 2. Render entertainment row
                        string djName = dance.AssignedDj != null ? dance.AssignedDj.Name : "VACANT / UNASSIGNED";
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Event DJ / Band").Bold();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(djName).FontColor(dance.AssignedDj == null ? Colors.Red.Medium : Colors.Black);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(dance.AssignedDj == null ? "[  ] Recruits Needed" : "[  ] Present");

                        // 3. Render Dance Hosts (Loops through collection if populated, otherwise prints a placeholder line)
                        if (dance.AssignedHosts != null && dance.AssignedHosts.Any())
                        {
                            int hostCounter = 1;
                            foreach (var host in dance.AssignedHosts.OrderBy(h => h.Name))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"Dance Host #{hostCounter++}").Bold();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{host.Name}");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("[  ] Present");
                            }
                        }
                        else
                        {
                            // Fallback lines for physical manual host sign-ins if none are seeded in db
                            for (int i = 1; i <= 3; i++)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"Dance Host #{i}");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("___________________________").FontColor(Colors.Grey.Lighten1);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("[  ] Present");
                            }
                        }
                    });
                    #endregion SEGMENT B


                    #region Segment C: Event Volunteers Roster
                    column
                        .Item()
                        .PaddingTop(25)
                        .Text(text =>
                        {
                            text.Span("2. Public Event Volunteers")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Grey.Darken2);
                        });

                    column
                    .Item()
                    .PaddingTop(5)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Assigned Shift").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Volunteer Name").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Status").Bold();
                        });

                        var coreDuties = new[] { "Front Desk Check-in", "Setup Team", "Clean Up Crew" };
                        foreach (var duty in coreDuties)
                        {
                            var filled = dance.AssignedVolunteers.FirstOrDefault(v => v.Notes?.Contains($"({duty})") == true);
                            string helperName = filled != null ? (filled.Name.Contains(" (") ? filled.Name.Split(" (")[0] : filled.Name) : "VACANT POSITION";

                            table.Cell()
                        .BorderBottom(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(5)
                        .Text(duty)
                        .Bold();
                            table.Cell()
                        .BorderBottom(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(5)
                        .Text(helperName)
                        .FontColor(filled == null ? Colors.Red.Medium : Colors.Black);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(filled == null ? "[  ] Recruits Needed" : "[  ] Present");
                        }
                    });
                    #endregion Segment C: Event Volunteers Roster
                });
                #endregion Document Body Workspace Content Grid

                #region Document Footer Numbering Index
                page
                .Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
                
                #endregion Document Footer Numbering Index
            });
        });
        #endregion Construct the document stream using Fluent fluid layouts

        var pdfBytes = documentPdf.GeneratePdf();
        string fileName = $"Coordination_Log_{dance.Date:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    public Dance TargetDance { get; set; } = null!;
    public List<Member> ActiveOfficersList { get; set; } = new();
    public bool AmIAttending { get; set; }
    public int CurrentMemberId { get; set; }

    [TempData]
    public string? StatusNotice { get; set; }

    //  Load context details
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        var idClaim = User.FindFirst("MemberId")?.Value;
        if (!int.TryParse(idClaim, out int memberId)) return Forbid();
        CurrentMemberId = memberId;

        // Fetch the dance event and eagerly load the many-to-many collection
        TargetDance = await _context.Events.OfType<Dance>()
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null) return NotFound();

        // Check if the currently browsing officer is checked in
        AmIAttending = TargetDance.AttendingOfficers.Any(o => o.Id == CurrentMemberId);

        // Fetch all system officers for Admin's dropdown selection tool
        ActiveOfficersList = await _context.Members
            .Where(m => !m.IsSuspended &&
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value >= DateTime.UtcNow &&
                m.OfficerAccount != null && m.OfficerAccount.IsEnabled)
            .Include(m => m.OfficerAccount)
            .OrderBy(m => m.LastName)
            .ToListAsync();

        return Page();
    }

    //  Hook: Self Check-In / Check-Out
    public async Task<IActionResult> OnPostToggleSelfAttendanceAsync(int id)
    {
        var idClaim = User.FindFirst("MemberId")?.Value;
        if (!int.TryParse(idClaim, out int memberId)) return Forbid();

        var dance = await _context.Events.OfType<Dance>()
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dance == null) return NotFound();

        var currentOfficer = await _context.Members
            .Include(m => m.OfficerAccount)
            .FirstOrDefaultAsync(m => m.Id == memberId &&
                m.OfficerAccount != null && m.OfficerAccount.IsEnabled);
        if (currentOfficer == null) return Forbid();

        string actionText;
        if (dance.AttendingOfficers.Any(o => o.Id == memberId))
        {
            dance.AttendingOfficers.Remove(currentOfficer);
            actionText = "<span style='color:#dc3545; font-weight:bold;'>Checked OUT</span> of duty roster assignment by self-service request.";
            StatusNotice = "Success: You have checked out of this dance event assignment.";
        }
        else
        {
            dance.AttendingOfficers.Add(currentOfficer);
            actionText = "<span style='color:#198754; font-weight:bold;'>Checked IN</span> to active duty roster assignment by self-service request.";
            StatusNotice = "Success: You are now checked into this dance event assignment!";
        }

        await _context.SaveChangesAsync();

        // TRIGGER NOTIFICATION DISPATCH
        await _emailService.SendOfficerDutyNotificationAsync(
            currentOfficer.OfficerAccount!.Email,
            $"{currentOfficer.LastName}",
            dance.Title,
            dance.Date.ToString("MMMM dd, yyyy"),
            actionText,
            currentOfficer.Id
        );

        return RedirectToPage(new { id });
    }

    //  Hook: Administrative Override Force Add
    public async Task<IActionResult> OnPostAddOfficerOverrideAsync(int id, int selectOfficerId)
    {
        // Enforce role-based access identity gate
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        var dance = await _context.Events.OfType<Dance>().Include(d => d.AttendingOfficers).FirstOrDefaultAsync(d => d.Id == id);
        if (dance == null) return NotFound();

        var targetOfficer = await _context.Members
            .Include(m => m.OfficerAccount)
            .FirstOrDefaultAsync(m => m.Id == selectOfficerId &&
                m.OfficerAccount != null && m.OfficerAccount.IsEnabled);
        if (targetOfficer == null)
        {
            StatusNotice = "Error: Selected officer record missing.";
            return RedirectToPage(new { id });
        }

        if (dance.AttendingOfficers.Any(o => o.Id == selectOfficerId))
        {
            StatusNotice = $"Info: {targetOfficer.LastName} is already checked into this event.";
            return RedirectToPage(new { id });
        }

        dance.AttendingOfficers.Add(targetOfficer);
        await _context.SaveChangesAsync();

        StatusNotice = $"Success: Checked {targetOfficer.LastName} into the event roster.";

        // TRIGGER NOTIFICATION DISPATCH
        string actionText = "<span style='color:#198754; font-weight:bold;'>ASSIGNED TO DUTY</span> via Administrative Override by Admin.";
        await _emailService.SendOfficerDutyNotificationAsync(
            targetOfficer.OfficerAccount!.Email,
            $"{targetOfficer.LastName}",
            dance.Title,
            dance.Date.ToString("MMMM dd, yyyy"),
            actionText,
            selectOfficerId
        );

        return RedirectToPage(new { id });
    }

    //  Hook: Administrative Override Force Remove
    public async Task<IActionResult> OnPostRemoveOfficerOverrideAsync(int id, int dropOfficerId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        var dance = await _context.Events.OfType<Dance>()
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dance == null) return NotFound();

        var targetOfficer = dance.AttendingOfficers.FirstOrDefault(o => o.Id == dropOfficerId);
        if (targetOfficer != null)
        {
            dance.AttendingOfficers.Remove(targetOfficer);
            await _context.SaveChangesAsync();
            StatusNotice = $"Success: Removed {targetOfficer.LastName} from the attendance grid.";

            // TRIGGER NOTIFICATION DISPATCH
            string actionText = "<span style='color:#dc3545; font-weight:bold;'>REMOVED FROM DUTY</span> via Administrative Override by Admin.";
            await _emailService.SendOfficerDutyNotificationAsync(
                targetOfficer.OfficerAccount?.Email ?? targetOfficer.Email,
                $"{targetOfficer.LastName}",
                dance.Title,
                dance.Date.ToString("MMMM dd, yyyy"),
                actionText,
                dropOfficerId
            );
        }

        return RedirectToPage(new { id });
    }

}
