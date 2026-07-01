namespace SplitDesk.Api.Models;

// What the scan endpoint returns — a partial bill pre-filled from OCR text.
// The frontend uses this to populate the form; the user then assigns people.
public record ScanResult(
    List<ScannedItem> Items,
    decimal? TaxPercent,
    decimal? TipPercent,
    string RawText   // returned so frontend can show what was read
);

public record ScannedItem(string Name, decimal Price);
