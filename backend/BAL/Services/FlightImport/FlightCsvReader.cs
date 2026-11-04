using LabTest.Models.DTOs;

namespace LabTest.Services.FlightImport;

public static class FlightCsvReader
{
    public static async Task<List<FlightImportRow>> ReadAsync(StreamReader reader)
    {
        // Read header and validate required columns (case-insensitive)
        string? headerLine = await reader.ReadLineAsync();
        int lineNumber = 1;
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return [new FlightImportRow("", null, "Invalid CSV format. Missing header row.")];

        }

        var headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
        var map = headers
            .Select((name, idx) => new { name, idx })
            .ToDictionary(x => x.name, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var requiredCols = new[] { "FlightNumber", "ScheduledArrivalTimeUtc", "OriginAirport", "DestinationAirport" };
        var missingCols = requiredCols.Where(c => !map.ContainsKey(c)).ToList();
        if (missingCols.Count > 0)
        {
            return [new FlightImportRow("", null, $"Invalid CSV format. Required columns not found: {string.Join(", ", missingCols)}")];
        }

        int iFN = map["FlightNumber"];
        int iSAT = map["ScheduledArrivalTimeUtc"];
        int iO = map["OriginAirport"];
        int iD = map["DestinationAirport"];
        var rows = new List<FlightImportRow>();

        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');
            var maxIndex = Math.Max(Math.Max(iFN, iSAT), Math.Max(iO, iD));
            if (values.Length <= maxIndex)
            {
                rows.Add(new FlightImportRow($"Line {lineNumber}", null, "Not enough fields."));
                continue;
            }

            var model = new AddFlightModel
            {
                FlightNumber = values[iFN]?.Trim(),
                ScheduledArrivalTimeUtc = values[iSAT]?.Trim(),
                OriginAirport = values[iO]?.Trim(),
                DestinationAirport = values[iD]?.Trim()
            };

            rows.Add(new FlightImportRow($"Line {lineNumber}", model, null));
        }
        return rows;
    }
}
