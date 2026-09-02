using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Dev;

[Authorize(Roles = "TechAdmin,Admin")]
public class SheetViewerModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly ILogger<SheetViewerModel> _logger;
    private readonly AbdContext _context;
    private readonly IGoogleSheetExportService _exportService;

    public SheetViewerModel(
        IConfiguration config,
        ILogger<SheetViewerModel> logger,
        AbdContext context,
        IGoogleSheetExportService exportService)
    {
        _config = config;
        _logger = logger;
        _context = context;
        _exportService = exportService;
    }

    public List<SheetMemberRow> SheetMemberRows { get; set; } = new();
    public List<SheetSubscriberRow> SheetSubscriberRows { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalMembersRowsCount { get; set; }
    public int TotalSubscribersRowsCount { get; set; }
    public string TargetSheetId { get; } = "1ZYuy9KhrwBdZoydOMydjsd_h9cQZ2FX2r-2VBI3Ul2E";
    private static readonly DateTime VeryOldExpiryDate = DateTime.SpecifyKind(new DateTime(1900, 1, 1), DateTimeKind.Utc);

    public class SheetMemberRow
    {
        public int RowIndex { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RawExpirationDate { get; set; } = string.Empty;
        public DateTime? ParsedExpirationDate { get; set; }
        public bool IsExpired => ParsedExpirationDate.HasValue && ParsedExpirationDate.Value.Date < DateTime.UtcNow.Date;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Notes { get; set; } = string.Empty;
        public bool IsEmailBounced { get; set; }
        public string? MemberNumber { get; set; }
        public string? DatabaseStatus { get; set; }
        public bool IsImported => MemberNumber != null;
    }

    public class SheetSubscriberRow
    {
        public int RowIndex { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string RawCreatedDate { get; set; } = string.Empty;
        public DateTime? ParsedCreatedDate { get; set; }
        public string Email { get; set; } = string.Empty;
        public int? SubscriberId { get; set; }
        public bool IsImported => SubscriberId != 0;
    }


    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // 1. Use Google Application Default Credentials for authentication
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

            if (credential == null)
            {
                ErrorMessage = "Unable to load Google credentials. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or credentials are configured via Workload Identity.";
                return Page();
            }

            credential = credential.CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AbdClub-DevViewer"
            });

            #region Members

            // 3. Request columns B to G (skip header row, starting at row 2)
            string memberRange = "Membership_raw!A2:G"; // Read from A or B through G
            var memberRequest = service.Spreadsheets.Values.Get(TargetSheetId, memberRange);
            ValueRange memberResponse = await memberRequest.ExecuteAsync();
            var memberValues = memberResponse.Values;

            if (memberValues == null || memberValues.Count == 0)
            {
                ErrorMessage = "Connected to Google Sheet, but no row records were returned in memberRange " + memberRange;
                return Page();
            }

            // Email is contact information and may be shared. Match a sheet row to
            // a database member by normalized email plus first and last name.
            var databaseMembers = await _context.Members.AsNoTracking().ToListAsync();
            var membersByIdentity = databaseMembers
                .GroupBy(m => MemberIdentity(m.Email, m.FirstName, m.LastName))
                .ToDictionary(g => g.Key, g => g.First());

            int memberIndex = 2; // Tracking row number in sheet
            foreach (var row in memberValues)
            {
                // Helper to safely read cell text by memberIndex
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                // Adjust indices based on sheet columns:
                // Column A = 0: FirstName
                // Column B = 1: LastName
                // Column C = 2: FullName
                // Column D = 3: Expiration Date
                // Column E = 4: Phone
                // Column F = 5: Email
                // Column G = 6: Notes
                string firstName = GetCell(0);
                string lastName = GetCell(1);
                string fullName = GetCell(2);
                string rawExp = GetCell(3);
                string phone = GetCell(4);
                string email = GetCell(5);
                string notes = GetCell(6);

                // If column A was omitted in the memberRange memberRequest or offset, verify if name exists:
                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                {
                    memberIndex++;
                    continue; // Skip empty rows
                }

                DateTime? parsedDate = null;
                if (DateTime.TryParse(rawExp, out var exp))
                {
                    parsedDate = exp;
                }

                // Check for bounce indicators (e.g. text markers or tags)
                bool isBounced = email.Contains("[BOUNCE]", StringComparison.OrdinalIgnoreCase) ||
                                 email.Contains("(bounced)", StringComparison.OrdinalIgnoreCase);

                var cleanEmail = CleanSheetEmail(email);
                membersByIdentity.TryGetValue(
                    MemberIdentity(cleanEmail, firstName, lastName),
                    out var databaseMember);

                SheetMemberRows.Add(new SheetMemberRow
                {
                    RowIndex = memberIndex,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? $"{firstName} {lastName}".Trim() : fullName,
                    RawExpirationDate = rawExp,
                    ParsedExpirationDate = parsedDate,
                    Phone = phone,
                    Email = cleanEmail,
                    IsEmailBounced = isBounced,
                    Notes = databaseMember?.Notes,
                    MemberNumber = databaseMember?.DisplayMemberNumber,
                    DatabaseStatus = databaseMember == null
                        ? null
                        : databaseMember.IsActive
                            ? "Active"
                            : databaseMember.IsSuspended ? "Suspended" : "Expired"
                });

                memberIndex++;
            }

            TotalMembersRowsCount = SheetMemberRows.Count;

            #endregion Members


            #region Subscribers

            // 3. Request columns B to G (skip header row, starting at row 2)
            string subscriberRange = "Subscribers_raw!A2:AC"; // Read from A or B through G
            var subscriberRequest = service.Spreadsheets.Values.Get(TargetSheetId, subscriberRange);
            ValueRange subscriberResponse = await subscriberRequest.ExecuteAsync();
            var subscriberValues = subscriberResponse.Values;

            if (subscriberValues == null || subscriberValues.Count == 0)
            {
                ErrorMessage = "Connected to Google Sheet, but no row records were returned in subscriberRange " + subscriberRange;
                return Page();
            }

            // Email is contact information and may be shared. Match a sheet row to
            // a database member by normalized email plus first and last name.
            var databaseSubscribers = await _context.NewsletterSubscribers.AsNoTracking().ToListAsync();
            var subscribersByIdentity = databaseSubscribers
                .GroupBy(m => SubscriberIdentity(m.Email, m.FirstName))
                .ToDictionary(g => g.Key, g => g.First());

            int subscriberIndex = 2; // Tracking row number in sheet
            foreach (var row in subscriberValues)
            {
                // Helper to safely read cell text by subscriberIndex
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                // Adjust indices based on sheet columns:
                // Column A = 0: Email
                // Column B = 1: FirstName
                // Column C = 2: LastName
                // Column AC = 3: Created At
                string email = GetCell(0);
                string firstName = GetCell(1);
                string lastName = GetCell(2);
                string fullName = GetCell(2);
                string rawCreated= GetCell(28);

                // If column A was omitted in the subscriberRange subscriberRequest or offset, verify if name exists:
                if (
                    string.IsNullOrWhiteSpace(email))
                {
                    subscriberIndex++;
                    continue; // Skip empty rows
                }

                DateTime? parsedDate = null;
                if (DateTime.TryParse(rawCreated , out var exp))
                {
                    parsedDate = exp;
                }

                var subscriberName = $"{firstName} {lastName}".Trim();

                subscribersByIdentity.TryGetValue(
                    SubscriberIdentity(email, subscriberName),
                    out var databaseSubscriber);

                SheetSubscriberRows.Add(new SheetSubscriberRow
                {
                    RowIndex = subscriberIndex,
                    FirstName =  subscriberName,
                    RawCreatedDate = rawCreated,
                    ParsedCreatedDate = parsedDate,
                    SubscriberId = databaseSubscriber?.Id,
                    Email = email
                });

                subscriberIndex++;
            }

          TotalSubscribersRowsCount  = SheetSubscriberRows.Count;

            #endregion Subscribers


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Google Sheet {SheetId}", TargetSheetId);
            ErrorMessage = $"Google Sheets API Error: {ex.Message}";
        }

        return Page();
    }


    // Import memberships from the 'Membership_raw' tab of the target spreadsheet
    public async Task<IActionResult> OnPostImportMembershipsAsync()
    {
        try
        {
            // Use Google Application Default Credentials for authentication
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

            if (credential == null)
            {
                TempData["ImportResult"] = "Unable to load Google credentials. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or credentials are configured via Workload Identity.";
                return RedirectToPage();
            }

            credential = credential.CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AbdClub-DevViewer-Import"
            });

            // Read the Membership_raw tab explicitly
            string memberRange = "Membership_raw!A2:G";
            var memberRequest = service.Spreadsheets.Values.Get(TargetSheetId, memberRange);
            ValueRange memberResponse = await memberRequest.ExecuteAsync();
            var memberValues = memberResponse.Values;

            if (memberValues == null || memberValues.Count == 0)
            {
                TempData["ImportResult"] = "No rows found in Membership_raw tab.";
                return RedirectToPage();
            }

            // Shared household emails are valid. A member is considered existing
            // only when email, first name, and last name all match.
            var existingMembers = await _context.Members
                .AsNoTracking()
                .Select(m => new { m.Email, m.FirstName, m.LastName })
                .ToListAsync();
            var existingSet = existingMembers
                .Select(m => MemberIdentity(m.Email, m.FirstName, m.LastName))
                .ToHashSet();

            var toAdd = new List<Member>();
            int memberIndex = 2;
            int missingExpiryCount = 0;
            foreach (var row in memberValues)
            {
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                string firstName = GetCell(0);
                string lastName = GetCell(1);
                string fullName = GetCell(2);
                string rawExp = GetCell(3);
                string phone = GetCell(4);
                string email = CleanSheetEmail(GetCell(5));
                string notes = GetCell(6);
                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                {
                    memberIndex++;
                    continue;
                }

                var memberIdentity = MemberIdentity(email, firstName, lastName);
                if (string.IsNullOrWhiteSpace(email) || existingSet.Contains(memberIdentity))
                {
                    memberIndex++;
                    continue; // skip rows with no email or the same person already existing
                }

                DateTime? parsedDate = null;
                if (DateTime.TryParse(rawExp, out var exp))
                {
                    parsedDate = DateTime.SpecifyKind(exp, DateTimeKind.Utc);
                }
                else
                {
                    // Assign a very old expiration date when missing or malformed
                    parsedDate = VeryOldExpiryDate;
                    missingExpiryCount++;
                }

                var member = new Member
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    JoinDate = DateTime.UtcNow,
                    ExpiryDate = parsedDate,
                    SelfRegistered = false,
                    CreatedAt = DateTime.UtcNow,
                    Notes = notes
                };

                toAdd.Add(member);
                existingSet.Add(memberIdentity);
                memberIndex++;
            }

            if (toAdd.Count > 0)
            {
                _context.Members.AddRange(toAdd);
                await _context.SaveChangesAsync();
                TempData["ImportResult"] = $"Imported {toAdd.Count} members from Membership_raw (skipped {memberValues.Count - toAdd.Count}). Missing expiry for {missingExpiryCount} imported rows.";
            }
            else
            {
                TempData["ImportResult"] = $"No new members to import from Membership_raw. Missing expiry for {missingExpiryCount} rows.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import Membership_raw from sheet {SheetId}", TargetSheetId);
            TempData["ImportResult"] = "Import failed: " + ex.Message;
        }

        return RedirectToPage();
    }

    // Import subscribers from the 'Subscribers_raw' tab of the target spreadsheet
    public async Task<IActionResult> OnPostImportSubscribersAsync()
    {
        try
        {
            // Use Google Application Default Credentials for authentication
            GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

            if (credential == null)
            {
                TempData["ImportResult"] = "Unable to load Google credentials. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or credentials are configured via Workload Identity.";
                return RedirectToPage();
            }

            credential = credential.CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AbdClub-DevViewer-Import"
            });

            // Read the Subscribers_raw tab explicitly
            string subscriberRange = "Subscribers_raw!A2:AC";
            var subscriberRequest = service.Spreadsheets.Values.Get(TargetSheetId, subscriberRange);
            ValueRange subscriberResponse = await subscriberRequest.ExecuteAsync();
            var subscriberValues = subscriberResponse.Values;

            if (subscriberValues == null || subscriberValues.Count == 0)
            {
                TempData["ImportResult"] = "No rows found in Subscribers_raw tab.";
                return RedirectToPage();
            }

            // Shared household emails are valid. A member is considered existing
            // only when email, first name, and last name all match.
            var existingSubscribers = await _context.NewsletterSubscribers
                .AsNoTracking()
                .Select(m => new { m.Email, m.FirstName})
                .ToListAsync();
            var existingSet = existingSubscribers
                .Select(m => SubscriberIdentity(m.Email, m.FirstName))
                .ToHashSet();

            var toAdd = new List<NewsletterSubscriber>();
            int subscriberIndex = 2;
            int missingSubscribedAtCount = 0;
            foreach (var row in subscriberValues)
            {
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                string email = GetCell(0);
                string firstName = GetCell(1);
                string lastName = GetCell(2);
                string rawCreated  = GetCell(28);
                var fName = firstName.Trim().ToLower();
                var lName = lastName.Trim().ToLower();
                var subscriberName = $"{fName} {lName}";

                if (
                    string.IsNullOrWhiteSpace(email))
                {
                    subscriberIndex++;
                    continue;
                }


                var subscribersByIdentity = SubscriberIdentity(email, subscriberName);

                if (string.IsNullOrWhiteSpace(email) || existingSet.Contains(subscribersByIdentity))
                {
                    subscriberIndex++;
                    continue; // skip rows with no email or the same person already existing
                }

                DateTime parsedDate;
                if (DateTime.TryParse(rawCreated, out var exp))
                {
                    parsedDate = DateTime.SpecifyKind(exp, DateTimeKind.Utc);
                }
                else
                {
                    // Assign a very old expiration date when missing or malformed
                    parsedDate = VeryOldExpiryDate;
                    missingSubscribedAtCount++;
                }

                var subscriber = new NewsletterSubscriber
                {
                    FirstName = subscriberName,
                    Email = email,
                    SubscribedAt = parsedDate
                };

                toAdd.Add(subscriber);
                existingSet.Add(subscribersByIdentity);
                subscriberIndex++;
            }

            if (toAdd.Count > 0)
            {
                _context.NewsletterSubscribers.AddRange(toAdd);
                await _context.SaveChangesAsync();
                TempData["ImportResult"] = $"Imported {toAdd.Count} members from Subscribers_raw (skipped {subscriberValues.Count - toAdd.Count}). Missing subscribed at for {missingSubscribedAtCount} imported rows.";
            }
            else
            {
                TempData["ImportResult"] = $"No new subscribers to import from Subscribers_raw. Missing subscribed at for {missingSubscribedAtCount} rows.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import Subscribers_raw from sheet {SheetId}", TargetSheetId);
            TempData["ImportResult"] = "Import failed: " + ex.Message;
        }

        return RedirectToPage();
    }


    private static string CleanSheetEmail(string email) =>
        email.Replace("[BOUNCE]", "", StringComparison.OrdinalIgnoreCase)
            .Replace("(bounced)", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

    private static string MemberIdentity(string? email, string? firstName, string? lastName) =>
        $"{email?.Trim().ToLowerInvariant()}|{firstName?.Trim().ToLowerInvariant()}|{lastName?.Trim().ToLowerInvariant()}";

    private static string SubscriberIdentity(string? email, string? firstName ) =>
        $"{email?.Trim().ToLowerInvariant()}|{firstName?.Trim().ToLowerInvariant()}";


    public async Task<IActionResult> OnPostTriggerExportAsync()
    {
        // 1. Fetch live member records from PostgreSQL database
        var databaseMembers = await _context.Members
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();

        // 2. Define target spreadsheet identifiers
        string targetSheetId = "1ZYuy9KhrwBdZoydOMydjsd_h9cQZ2FX2r-2VBI3Ul2E";
        // ⚠️ Note: Ensure this exact tab name exists inside your Google Sheet file!
        string tabTitleName = "ExportFromWebApp";

        try
        {
            // 3. Dispatch data rows to Google Drive cloud arrays
            string completionSummaryMessage = await _exportService.ExportMembersToSheetAsync(targetSheetId, tabTitleName, databaseMembers);
            TempData["ImportResult"] = completionSummaryMessage;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed executing cloud data stream export transaction: {ex.Message}";
        }

        return RedirectToPage("./SheetViewer");
    }


}
