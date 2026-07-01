using System.Diagnostics;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SplitDesk.Api.Models;

namespace SplitDesk.Api.Services;

// Uses the Tesseract CLI (installed via apt-get in Docker) to extract text from
// a receipt image, then parses the text with regex to find items and percentages.
public class BillScanService : IBillScanService
{
    private readonly ILogger<BillScanService> _logger;

    // Tesseract needs enough pixels per character to recognise text reliably.
    // Phone photos/screenshots of small receipts are often only 200-300px wide,
    // which reads as near-garbage OCR. Upscaling before OCR fixes this.
    private const int MinOcrWidth = 1600;

    // Item lines come in two common shapes:
    //   "PIZZA                  9.99"                     (name + single price)
    //   "1   MASALA DOSA   110.00   110.00"                (qty + name + rate + amount)
    // We optionally strip a leading quantity token (a number, "2x", or the OCR
    // misread "il"/"l" for "1"), then require the name to be immediately
    // followed by one or two decimal amounts. When two are present the LAST
    // one is the true line total (rate is per-unit, amount is what's owed).
    //
    // Only [ \t] (never \s) separates name/amounts — \s also matches newlines,
    // which let the lazy name group backtrack across a line break and swallow
    // the next line whenever a malformed trailing number broke the match on
    // its own line. The trailing ".*$" (dot doesn't match newline either)
    // tolerates leftover OCR garbage after a valid price without needing the
    // line to end exactly on it. A short run of stray symbols (OCR noise like
    // "~—" from a dashed rule bleeding into the row) is also tolerated right
    // before the name, since it must otherwise start with a letter.
    private static readonly Regex ItemLineRegex = new(
        @"^[ \t]*(?:(?<qty>\d{1,3})[ \t]*[xX]?[ \t]+|[IiLl1]{1,2}[ \t]+)?" +
        @"[^A-Za-z0-9\r\n]{0,4}" +
        @"(?<name>[A-Za-z][A-Za-z0-9 \t&'\-\(\)\.]{1,45}?)" +
        @"[ \t]+(?:₹|Rs\.?|INR|\$|€|£|%)?[ \t]*(?<amount1>\d{1,5}(?:,\d{3})*\.\d{2})" +
        @"(?:[ \t]+(?:₹|Rs\.?|INR|\$|€|£|%)?[ \t]*(?<amount2>\d{1,5}(?:,\d{3})*\.\d{2}))?" +
        @"[ \t]*.*$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    // Matches every "TAX 12.5%" / "VAT 20%" / "CGST 2.5%" / "SGST 2.5%" / "SERVICE CHARGE 10%"
    // line. Receipts often split tax into multiple components (e.g. CGST + SGST in India),
    // so all matches found are summed rather than only using the first.
    private static readonly Regex TaxRegex = new(
        @"(?:CGST|SGST|GST|TAX|VAT|SERVICE\s*CHARGE)\s*:?\s*(?:[₹$€£%]\s*\d+(?:\.\d{1,2})?\s+)?(\d{1,3}(?:\.\d{1,2})?)\s*%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // "TIP 10%" or "GRATUITY 15%"
    private static readonly Regex TipRegex = new(
        @"(?:TIP|GRATUITY|DISCRETIONARY)\s*:?\s*(?:[₹$€£%]\s*\d+(?:\.\d{1,2})?\s+)?(\d{1,3}(?:\.\d{1,2})?)\s*%",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Lines to skip — totals, tax/tip breakdowns, headers, metadata. Matched anywhere
    // in the line (not just at the start) since OCR sometimes merges columns together.
    private static readonly Regex SkipLineRegex = new(
        @"(?:SUB\s*TOTAL|GRAND\s*TOTAL|\bTOTAL\b|\bAMOUNT\b|\bCHANGE\b|\bCASH\b|\bCARD\b|" +
        @"\bTENDERED\b|\bBALANCE\b|\bDUE\b|\bROUND(?:ING|ED)?\b|" +
        @"\bCGST\b|\bSGST\b|\bGST\b|\bVAT\b|\bTAX\b|SERVICE\s*CHARGE|\bTIP\b|\bGRATUITY\b|" +
        @"\bRECEIPT\b|\bINVOICE\b|\bTHANK\b|\bVISIT\b|\bDATE\b|\bTIME\b|\bTABLE\b|\bPAX\b|" +
        @"\bCASHIER\b|\bORDER\b|\bBILL\s*NO|\bNO\.\s*OF|\bTEL\b|\bPH:|WWW\.|HTTP|" +
        @"AMOUNT\s+IN\s+WORDS|\bRUPEES\b|\bONLY\.?$|\bSAMPLE\b|\bDEMO\b|\bQTY\b.*\bITEM\b)",
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
                await PreprocessImageAsync(imageStream, fs);

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

    // Upscales small images and boosts contrast so Tesseract has more signal to
    // work with. Runs on every image (not just small ones) since grayscale +
    // contrast also helps with photos that have shadows or a tinted background.
    private static async Task PreprocessImageAsync(Stream input, Stream output)
    {
        using var image = await Image.LoadAsync<Rgba32>(input);

        if (image.Width < MinOcrWidth)
        {
            var scale = (double)MinOcrWidth / image.Width;
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size    = new Size(MinOcrWidth, (int)Math.Round(image.Height * scale)),
                Sampler = KnownResamplers.Lanczos3,
            }));
        }

        image.Mutate(x => x.Grayscale().Contrast(1.25f));

        await image.SaveAsync(output, new PngEncoder());
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

    // Internal (not private) so tests can feed it real captured OCR text without
    // going through Tesseract itself. See InternalsVisibleTo in the csproj.
    internal static ScanResult ParseReceiptText(string text)
    {
        var items = new List<ScannedItem>();

        // Sum every tax component found (e.g. CGST 2.5% + SGST 2.5% = 5% total).
        var taxMatches = TaxRegex.Matches(text)
            .Select(m => decimal.TryParse(m.Groups[1].Value, out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        decimal? tax = taxMatches.Count > 0 ? taxMatches.Sum() : null;

        var tipMatches = TipRegex.Matches(text)
            .Select(m => decimal.TryParse(m.Groups[1].Value, out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        decimal? tip = tipMatches.Count > 0 ? tipMatches.Sum() : null;

        // Extract item lines
        foreach (Match m in ItemLineRegex.Matches(text))
        {
            var name = m.Groups["name"].Value.Trim();

            if (SkipLineRegex.IsMatch(name) || SkipLineRegex.IsMatch(m.Value)) continue;

            // Two amounts on the line ("rate" then "amount") → the second is what's owed.
            // When only one amount is readable (the second was OCR garbage), fall back
            // to rate × quantity rather than assuming rate == amount — that assumption
            // only holds for quantity 1.
            decimal parsedPrice;
            if (m.Groups["amount2"].Success &&
                decimal.TryParse(m.Groups["amount2"].Value.Replace(",", ""), out var amount2))
            {
                parsedPrice = amount2;
            }
            else if (decimal.TryParse(m.Groups["amount1"].Value.Replace(",", ""), out var amount1))
            {
                var qty = m.Groups["qty"].Success && int.TryParse(m.Groups["qty"].Value, out var q) && q > 0
                    ? q
                    : 1;
                parsedPrice = amount1 * qty;
            }
            else continue;

            if (parsedPrice <= 0) continue;

            // De-duplicate — OCR sometimes reads the same line twice
            if (!items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                items.Add(new ScannedItem(name, parsedPrice));
        }

        return new ScanResult(items, tax, tip, text);
    }
}
