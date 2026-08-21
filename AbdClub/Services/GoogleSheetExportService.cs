using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;

namespace AbdClub.Services;

public class GoogleSheetExportService : IGoogleSheetExportService
{
    private readonly IConfiguration _config;
    private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets }; // Requires Read/Write scopes

    public GoogleSheetExportService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> ExportMembersToSheetAsync(string spreadsheetId, string sheetName, List<Member> members)
    {
        // 1. Use Google Application Default Credentials for secure, environment-based authentication
        GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

        if (credential == null)
        {
            throw new InvalidOperationException("Unable to load Google credentials. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or credentials are configured via Workload Identity.");
        }

        credential = credential.CreateScoped(_scopes);

        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "AbdClub-Exporter"
        });

        // 2. Construct the 2D matrix array. Row 1 is your static table headers wrapper.
        var dataMatrix = new List<IList<object>>
        {
            new List<object> { "Member ID", "First Name", "Last Name", "Full Name", "Email Address", "Join Date", "Expiration Date", "Status" }
        };

        // 3. Loop through active database records and map properties to table grid cells
        foreach (var m in members)
        {
            dataMatrix.Add(new List<object>
            {
                m.Id.ToString(),
                m.FirstName,
                m.LastName,
                m.FullName, // Reads your unmapped calculated property natively
                m.Email,
                m.JoinDate.ToString("yyyy-MM-dd"),
                m.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A",
                m.IsActive ? "Active" : (m.IsSuspended ? "Suspended" : "Expired")
            });
        }

        // 4. Target the specific sheet range (e.g., "MembersExport!A1:H1000")
        // Enforcing a generic catch range string like 'A1' lets Google dynamically resize row depths
        string targetRange = $"{sheetName}!A1";

        var valueRange = new ValueRange
        {
            Values = dataMatrix
        };

        // 5. Execute a clear push update over the wire
        var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, targetRange);

        // RAW means strings are copied directly as typed. 
        // USER_ENTERED parses string numbers or dates natively into calculations / formatting blocks
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        UpdateValuesResponse result = await updateRequest.ExecuteAsync();

        return $"Successfully updated {result.UpdatedRows} rows inside Google Sheet tab: '{sheetName}'";
    }
}

