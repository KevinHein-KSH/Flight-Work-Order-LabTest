using System;
using LabTest.Models.DTOs;

namespace LabTest.Services;

public static class WorkOrderCommandParser
{
    private static IEnumerable<(string key, string numPart)> Tokens(string rawCmd)
    {
        if (string.IsNullOrWhiteSpace(rawCmd)) yield break;

        var parts = rawCmd.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var p = part.Trim();
            var keyPart = new string(p.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            var numPart = new string(p.SkipWhile(char.IsLetter).ToArray());
            if (!string.IsNullOrWhiteSpace(keyPart))
                yield return (keyPart, numPart);
        }
    }

    public static ParsedWorkOrderModel ParseRaw(string raw)
    {
        var result = new ParsedWorkOrderModel();
        foreach (var (key, numPart) in Tokens(raw))
        {
            if (!int.TryParse(numPart, out var value)) continue;
            switch (key)
            {
                case "CHK": result.Chk = value; break;
                case "BAG": result.Bag = value; break;
                case "CLEAN": result.Clean = value; break;
                case "PBB": result.Pbb = value; break;
            }
        }
        return result;
    }

    // Strict writes (your validator), reusing Tokens
    public static bool TryParseAndValidate(string raw, out ParsedWorkOrderModel parsed, out string error)
    {
        parsed = new ParsedWorkOrderModel();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Raw command is empty.";
            return false;
        }

        int? chk = null, bag = null, clean = null, pbb = null;
        var validKeyExit = false;

        foreach (var (key, numPart) in Tokens(raw))
        {
            var validKey = key is "CHK" or "BAG" or "CLEAN" or "PBB";
            if (validKey) validKeyExit = true;

            if (!int.TryParse(numPart, out var value))
            {
                if (validKey)
                {
                    error = $"Invalid numeric value for '{key}'.";
                    return false;
                }
                continue; // unknown junk
            }

            if (value < 0)
            {
                error = $"Negative value is not allowed for '{key}'.";
                return false;
            }

            switch (key)
            {
                case "CHK": chk = value; break;
                case "BAG": bag = value; break;
                case "CLEAN": clean = value; break;
                case "PBB":
                    if (value is not (0 or 90 or 180 or 270))
                    {
                        error = "PBB must be one of 0, 90, 180, 270.";
                        return false;
                    }
                    pbb = value; break;
                default:
                    // ignore unknown keys
                    break;
            }
        }

        if (!validKeyExit)
        {
            error = "No valid commands found in raw string.";
            return false;
        }

        parsed = new ParsedWorkOrderModel { Chk = chk, Bag = bag, Clean = clean, Pbb = pbb };
        return true;
    }
}
