using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace AbdClub.Services;

public interface IGoogleSheetSyncService
{
    Task<List<List<string>>> ReadMembershipSheetAsync(string spreadsheetId, string range);
}

public class GoogleSheetSyncService : IGoogleSheetSyncService
{
    private readonly IConfiguration _config;
    private readonly string[] _scopes = { SheetsService.Scope.SpreadsheetsReadonly };

    public GoogleSheetSyncService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<List<string>>> ReadMembershipSheetAsync(string spreadsheetId, string range)
    {
        // 1. Use Google Application Default Credentials for secure, environment-based authentication
        // Credentials are resolved from GOOGLE_APPLICATION_CREDENTIALS env var, Workload Identity, or default locations
        GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

        if (credential == null)
        {
            throw new InvalidOperationException("Unable to load Google credentials. Ensure GOOGLE_APPLICATION_CREDENTIALS is set or credentials are configured via Workload Identity.");
        }

        credential = credential.CreateScoped(_scopes);

        // 2. Initialize the Google Sheets service with ADC
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Austin Ballroom Dancers Portal Engine"
        });

        // 3. Dispatch the network API request pass straight to Google's server clusters
        SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);
        ValueRange response = await request.ExecuteAsync();
        IList<IList<object>> values = response.Values;

        var rowMatrix = new List<List<string>>();
        if (values != null && values.Count > 0)
        {
            foreach (var row in values)
            {
                var parsedRow = new List<string>();
                foreach (var cell in row)
                {
                    parsedRow.Add(cell?.ToString() ?? string.Empty);
                }
                rowMatrix.Add(parsedRow);
            }
        }

        return rowMatrix;
    }
}

