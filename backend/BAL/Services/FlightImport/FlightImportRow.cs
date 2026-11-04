using System;
using LabTest.Models.DTOs;

namespace LabTest.Services.FlightImport;

public record FlightImportRow(string Label, AddFlightModel? Model, string? Error);

