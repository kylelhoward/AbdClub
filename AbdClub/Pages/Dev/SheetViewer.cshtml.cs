using AbdClub.Data;
using AbdClub.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AbdClub.Models;

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

    public List<SheetMemberRow> Rows { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalRowsCount { get; set; }
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
        public bool IsEmailBounced { get; set; }
        public string? MemberNumber { get; set; }
        public string? DatabaseStatus { get; set; }
        public bool IsImported => MemberNumber != null;
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

            // 3. Request columns B to G (skip header row, starting at row 2)
            string range = "Membership_raw!A2:G"; // Read from A or B through G
            var request = service.Spreadsheets.Values.Get(TargetSheetId, range);
            ValueRange response = await request.ExecuteAsync();
            var values = response.Values;

            if (values == null || values.Count == 0)
            {
                ErrorMessage = "Connected to Google Sheet, but no row records were returned in range " + range;
                return Page();
            }

            // Email is contact information and may be shared. Match a sheet row to
            // a database member by normalized email plus first and last name.
            var databaseMembers = await _context.Members.AsNoTracking().ToListAsync();
            var membersByIdentity = databaseMembers
                .GroupBy(m => MemberIdentity(m.Email, m.FirstName, m.LastName))
                .ToDictionary(g => g.Key, g => g.First());

            int index = 2; // Tracking row number in sheet
            foreach (var row in values)
            {
                // Helper to safely read cell text by index
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                // Adjust indices based on sheet columns:
                // Column A = 0 (if present)
                // Column B = 1: FirstName
                // Column C = 2: LastName
                // Column D = 3: FullName
                // Column E = 4: Expiration Date
                // Column F = 5: Phone
                // Column G = 6: Email
                string firstName = GetCell(1);
                string lastName = GetCell(2);
                string fullName = GetCell(3);
                string rawExp = GetCell(4);
                string phone = GetCell(5);
                string email = GetCell(6);

                // If column A was omitted in the range request or offset, verify if name exists:
                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                {
                    index++;
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

                Rows.Add(new SheetMemberRow
                {
                    RowIndex = index,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? $"{firstName} {lastName}".Trim() : fullName,
                    RawExpirationDate = rawExp,
                    ParsedExpirationDate = parsedDate,
                    Phone = phone,
                    Email = cleanEmail,
                    IsEmailBounced = isBounced,
                    MemberNumber = databaseMember?.DisplayMemberNumber,
                    DatabaseStatus = databaseMember == null
                        ? null
                        : databaseMember.IsActive
                            ? "Active"
                            : databaseMember.IsSuspended ? "Suspended" : "Expired"
                });

                index++;
            }

            TotalRowsCount = Rows.Count;
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
            string range = "Membership_raw!A2:G";
            var request = service.Spreadsheets.Values.Get(TargetSheetId, range);
            ValueRange response = await request.ExecuteAsync();
            var values = response.Values;

            if (values == null || values.Count == 0)
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
            int index = 2;
            int missingExpiryCount = 0;
            foreach (var row in values)
            {
                string GetCell(int colIndex) => row.Count > colIndex ? row[colIndex]?.ToString()?.Trim() ?? string.Empty : string.Empty;

                string firstName = GetCell(1);
                string lastName = GetCell(2);
                string fullName = GetCell(3);
                string rawExp = GetCell(4);
                string phone = GetCell(5);
                string email = CleanSheetEmail(GetCell(6));

                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                {
                    index++;
                    continue;
                }

                var memberIdentity = MemberIdentity(email, firstName, lastName);
                if (string.IsNullOrWhiteSpace(email) || existingSet.Contains(memberIdentity))
                {
                    index++;
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
                    IsOfficer = false,
                    IsAdmin = false,
                    IsTechAdmin = false,
                    SelfRegistered = false,
                    CreatedAt = DateTime.UtcNow
                };

                toAdd.Add(member);
                existingSet.Add(memberIdentity);
                index++;
            }

            if (toAdd.Any())
            {
                _context.Members.AddRange(toAdd);
                await _context.SaveChangesAsync();
                TempData["ImportResult"] = $"Imported {toAdd.Count} members from Membership_raw (skipped {values.Count - toAdd.Count}). Missing expiry for {missingExpiryCount} imported rows.";
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

    private static string CleanSheetEmail(string email) =>
        email.Replace("[BOUNCE]", "", StringComparison.OrdinalIgnoreCase)
            .Replace("(bounced)", "", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

    private static string MemberIdentity(string? email, string? firstName, string? lastName) =>
        $"{email?.Trim().ToLowerInvariant()}|{firstName?.Trim().ToLowerInvariant()}|{lastName?.Trim().ToLowerInvariant()}";


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
            TempData["SuccessMessage"] = completionSummaryMessage;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed executing cloud data stream export transaction: {ex.Message}";
        }

        return RedirectToPage("./SheetViewer");
    }


}
