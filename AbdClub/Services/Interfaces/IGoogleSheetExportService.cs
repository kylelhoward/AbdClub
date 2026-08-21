using AbdClub.Models;

namespace AbdClub.Services.Interfaces;

public interface IGoogleSheetExportService
{
    Task<string> ExportMembersToSheetAsync(string spreadsheetId, string sheetName, List<Member> members);
}

