using System.Text.Json;
using LabTest.Models.DTOs;

namespace LabTest.Services.FlightImport;

public static class FlightJsonReader
{
    public static async Task<List<FlightImportRow>> ReadAsync(StreamReader reader)
    {
        var json = await reader.ReadToEndAsync();
        List<AddFlightModel>? items;
        try
        {
            // Schema pre-check: ensure required keys appear in the uploaded JSON
            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return [new FlightImportRow("", null, "Invalid JSON format.")];
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    foreach (var prop in el.EnumerateObject())
                    {
                        keys.Add(prop.Name);
                    }
                }

                var required = new[] { "flightNumber", "scheduledArrivalTimeUtc", "originAirport", "destinationAirport" };
                var missing = required.Where(r => !keys.Contains(r)).ToList();
                if (missing.Count > 0)
                {
                    return [new FlightImportRow("", null, $"Invalid JSON format. Required keys not found: {string.Join(", ", missing)}")];
                }
            }
            items = JsonSerializer.Deserialize<List<AddFlightModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return [new FlightImportRow("", null, "Invalid JSON format.")];
        }
        return (items ?? []).Select((m, i) => new FlightImportRow($"Item {i + 1}", m, null)).ToList();
    }
}
