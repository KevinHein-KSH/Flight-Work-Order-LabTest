using System;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using System.Text.RegularExpressions;


namespace LabTest.Services.FlightImport;

public static class FlightRecordValidator
{
    public static string BuildFlightKey(string flightNumber, DateTime scheduledUtc, string origin, string destination)
            => $"{flightNumber.ToUpperInvariant()}|{scheduledUtc.ToUniversalTime():o}|{origin.ToUpperInvariant()}|{destination.ToUpperInvariant()}";
    
    public static readonly Regex AirportCodeRegex = new("^[A-Za-z]{3}$", RegexOptions.Compiled);

    public static bool TryValidateAndMap(
            AddFlightModel model,
            HashSet<string>? existingFlightNumbers,
            HashSet<string>? inFileFlightNumbers,
            DateTime nowUtc,
            out Flight? flight,
            out string error)
        {
            flight = null;
            error = string.Empty;

            var number = model.FlightNumber?.Trim();
            var scheduled = model.ScheduledArrivalTimeUtc?.Trim();
            var origin = model.OriginAirport?.Trim();
            var destination = model.DestinationAirport?.Trim();

            var missingFields = new List<string>(4);
            if (string.IsNullOrWhiteSpace(number)) missingFields.Add("FlightNumber");
            if (string.IsNullOrWhiteSpace(scheduled)) missingFields.Add("ScheduledArrivalTimeUtc");
            if (string.IsNullOrWhiteSpace(origin)) missingFields.Add("OriginAirport");
            if (string.IsNullOrWhiteSpace(destination)) missingFields.Add("DestinationAirport");
            if (missingFields.Count > 0)
            {
                error = $"Missing or empty fields: {string.Join(", ", missingFields)}.";
                return false;
            }

            // After earlier guard, all fields are non-null
            var numberValue = number!;
            var scheduledValue = scheduled!;
            var originValue = origin!;
            var destinationValue = destination!;

            if (numberValue.Length > 20)
            {
                error = "FlightNumber exceeds 20 characters.";
                return false;
            }

            if (!AirportCodeRegex.IsMatch(originValue) || !AirportCodeRegex.IsMatch(destinationValue))
            {
                error = "Airport codes must be 3 letters.";
                return false;
            }

            if (!DateTime.TryParse(scheduledValue, out var scheduledUtc))
            {
                error = "Invalid ScheduledArrivalUtc value.";
                return false;
            }

            // DB duplicate check only when DB has records; compare exact match across all fields
            var currentKey = BuildFlightKey(numberValue, scheduledUtc, originValue, destinationValue);
            if (existingFlightNumbers != null && existingFlightNumbers.Count > 0 && existingFlightNumbers.Contains(currentKey))
            {
                error = "Duplicate flight record (all fields match an existing one).";
                return false;
            }

            flight = new Flight
            {
                FlightNumber = numberValue,
                ScheduledArrivalTimeUtc = DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc),
                OriginAirport = originValue.ToUpperInvariant(),
                DestinationAirport = destinationValue.ToUpperInvariant(),
                CreatedAt = nowUtc
            };

            return true;
        }
}
