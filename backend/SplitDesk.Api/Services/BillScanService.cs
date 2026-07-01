using System.Diagnostics;
using System.Text.RegularExpressions;
using SplitDesk.Api.Models;

namespace SplitDesk.Api.Services;

// Uses the Tesseract CLI (installed via apt-get in Docker) to extract text from
// a receipt image, then parses the text with regex to find items and percentages.
public class BillScanService : IBillScanService
{
    private readonly ILogger<BillScanService> _logger;

    // Patterns for common receipt formats:
    // "Chicken Burger        £12.50"
    // "PIZZA                  9.99"
    // "  cola                  2.00  "
    private static readonly Regex ItemLineRegex = new(
        @"^(?<name>[A-Za-z][A-Za-z0-9\s&'\-]{1,40}?)\s+\£?(?<price>\d{1,4}\.\d{2})\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    // "TAX 12.5%" or "VAT 20%" or "SERVICE CHARGE 10%"
    private static readonly Regex TaxRegex = new(
        @"(?:TAX|VAT|SERVICE\s+CHARGE)\s*:?\s*(?:\£\d+\.\d{2}\s+)?(\d{1,3}(?:\.\d{1,2})?)%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // "TIP 10%" or "GRATUITY 15%"
    private static readonly Regex TipRegex = new(
        @"(?:TIP|GRATUITY|DISCRETIONARY)\s*:?\s*(?:\£\d+\.\d{2}\s+)?(\d{1,3}(?:\.\d{1,2})?)%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Lines to skip — totals, dates, card numbers, etc.
    private static readonly Regex SkipLineRegex = new(
        @"^\s*(?:TOTAL|SUBTOTAL|AMOUNT|CHANGE|CASH|CARD|RECEIPT|THANK|DATE|TIME|TABLE|ORDER|BILL|NO\.|TEL|WWW|HTTP|\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public BillScanService(ILogger<BillScanService> logger)
    {
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(Stream imageStream, string fileName)
    {
        // Write the uploaded image to a temp file so tesseract CLI can read it
        var tempInput  = Path.Combine(Path.GetTempPath(), $"scan_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
        var tempOutput = Path.Combine(Path.GetTempPath(), $"scan_{Guid.NewGuid()}");

        try
        {
            await using (var fs = File.Create(tempInput))
                await imageStream.CopyToAsync(fs);

            var rawText = await RunTesseractAsync(tempInput, tempOutput);
            _logger.LogInformation("OCR extracted {Chars} characters", rawText.Length);

            return ParseReceiptText(rawText);
        }
        finally
        {
            // Clean up temp files — always, even if parsing throws
            if (File.Exists(tempInput))  File.Delete(tempInput);
            if (File.Exists(tempOutput + ".txt")) File.Delete(tempOutput + ".txt");
        }
    }

    private static async Task<string> RunTesseractAsync(string inputPath, string outputBase)
    {
        // Call the tesseract binary (installed via apt-get in Docker)
        // --oem 3 = LSTM neural network engine (most accurate)
        // --psm 6 = assume uniform block of text (good for receipts)
        var psi = new ProcessStartInfo
        {
            FileName               = "tesseract",
            Arguments              = $"\"{inputPath}\" \"{outputBase}\" --oem 3 --psm 6",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start Tesseract process. Is it installed?");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var err = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Tesseract failed (exit {process.ExitCode}): {err}");
        }

        var outputFile = outputBase + ".txt";
        return File.Exists(outputFile)
            ? await File.ReadAllTextAsync(outputFile)
            : string.Empty;
    }

    private static ScanResult ParseReceiptText(string text)
    {
        var items      = new List<ScannedItem>();
        decimal? tax   = null;
        decimal? tip   = null;

        // Extract tax % if present
        var taxMatch = TaxRegex.Match(text);
        if (taxMatch.Success && decimal.TryParse(taxMatch.Groups[1].Value, out var taxVal))
            tax = taxVal;

        // Extract tip % if present
        var tipMatch = TipRegex.Match(text);
        if (tipMatch.Success && decimal.TryParse(tipMatch.Groups[1].Value, out var tipVal))
            tip = tipVal;

        // Extract item lines
        foreach (Match m in ItemLineRegex.Matches(text))
        {
            var name  = m.Groups["name"].Value.Trim();
            var price = m.Groups["price"].Value;

            if (SkipLineRegex.IsMatch(name)) continue;
            if (!decimal.TryParse(price, out var parsedPrice) || parsedPrice <= 0) continue;

            // De-duplicate — OCR sometimes reads the same line twice
            if (!items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                items.Add(new ScannedItem(name, parsedPrice));
        }

        return new ScanResult(items, tax, tip, text);
    }
}
