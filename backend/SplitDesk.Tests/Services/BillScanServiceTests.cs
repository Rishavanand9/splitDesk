using FluentAssertions;
using SplitDesk.Api.Services;
using Xunit;

namespace SplitDesk.Tests.Services;

// Regression tests for the receipt-text parser. ParseReceiptText is internal
// (see InternalsVisibleTo in SplitDesk.Api.csproj) so we can feed it real
// captured Tesseract output directly, without needing Tesseract in the test run.
public class BillScanServiceTests
{
    // Actual `tesseract --oem 3 --psm 6` output for TestRecipt.png (an Indian
    // restaurant receipt with a Qty/Item/Rate/Amount table and CGST+SGST tax
    // split). Captured verbatim, including OCR noise (e.g. "il" misread for "1",
    // "%" misread for "₹", a garbled CGST amount).
    private const string RealReceiptOcrText = """
        Be S.apBecdadad ||
        — UDUPI RESTAURANT —
        PURE VEG + SOUTH INDIAN * NORTH INDIAN + CHATS + JUICES
        #12, Lake View Road, Malpe, Udupi - 576 108
        Ph: 0820-2525252
        TAX INVOICE (SAMPLE)
        Not a valid tax invoice. For demonstration only.
        Bill No. — : UVR/24-25/0587 Table No. : T-07
        Date : 25/05/2024 No. of Pax : 3
        Time : 01:15 PM Cashier : KAVYA
        Qty Item Rate (%) Amount (%)
        il MASALA DOSA 110.00 110.00
        il IDLI (2 PCS) 60.00 60.00
        1 VADA (2 PCS) 60.00 60.00
        1 PONGAL 70.00 70.00
        il FILTER COFFEE 35.00 35.00
        il FRESH LIME SODA 45.00 45.00
        Sub Total 380.00
        CGST 2.5% 080)
        SGST 2.5% 9.50
        Grand Total (%) 399.00
        Amount in words:
        Rupees Three Hundred Ninety Nine Only.
        Thank You! Visit Again!
        $$ ¢ ——______
        Hos ky ee Riga Ore BRAD RE PPA Taos 9
        ! %* SAMPLE /DEMO «x H
        | This is a sample bill for demonstration purposes only.
        H Not a valid tax invoice. 4
        bee ee a a ee = dd
        """;

    [Fact]
    public void ParseReceiptText_RealReceipt_ExtractsAllSixItems()
    {
        var result = BillScanService.ParseReceiptText(RealReceiptOcrText);

        result.Items.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("MASALA DOSA", 110.00)]
    [InlineData("IDLI (2 PCS)", 60.00)]
    [InlineData("VADA (2 PCS)", 60.00)]
    [InlineData("PONGAL", 70.00)]
    [InlineData("FILTER COFFEE", 35.00)]
    [InlineData("FRESH LIME SODA", 45.00)]
    public void ParseReceiptText_RealReceipt_ExtractsCorrectItemAndPrice(string expectedName, decimal expectedPrice)
    {
        var result = BillScanService.ParseReceiptText(RealReceiptOcrText);

        result.Items.Should().ContainSingle(i =>
            i.Name.Contains(expectedName, StringComparison.OrdinalIgnoreCase) && i.Price == expectedPrice);
    }

    [Fact]
    public void ParseReceiptText_RealReceipt_DoesNotIncludeSubtotalAsAnItem()
    {
        // Regression guard: "Sub Total 380.00" previously slipped through as a
        // phantom item because the skip list only matched the literal
        // contiguous string "SUBTOTAL", not "Sub Total" with a space.
        var result = BillScanService.ParseReceiptText(RealReceiptOcrText);

        result.Items.Should().NotContain(i => i.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));
        result.Items.Should().NotContain(i => i.Name.Contains("Grand", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseReceiptText_RealReceipt_SumsCgstAndSgstIntoTotalTaxPercent()
    {
        // CGST 2.5% + SGST 2.5% = 5% total, even though the CGST amount itself
        // OCR'd as garbage ("080)") — only the percentage needs to be readable.
        var result = BillScanService.ParseReceiptText(RealReceiptOcrText);

        result.TaxPercent.Should().Be(5.0m);
    }

    [Fact]
    public void ParseReceiptText_NoTaxOrTipInText_ReturnsNull()
    {
        var result = BillScanService.ParseReceiptText("COFFEE   3.50\nMUFFIN   2.50");

        result.TaxPercent.Should().BeNull();
        result.TipPercent.Should().BeNull();
    }

    [Fact]
    public void ParseReceiptText_SimpleSinglePriceLine_ExtractsItem()
    {
        // Original, simpler receipt shape (name + one trailing price) must keep working.
        var result = BillScanService.ParseReceiptText("Chicken Burger        12.50");

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Chicken Burger");
        result.Items[0].Price.Should().Be(12.50m);
    }

    [Fact]
    public void ParseReceiptText_TipLineWithPercent_ExtractsTip()
    {
        var result = BillScanService.ParseReceiptText("Meal   10.00\nGRATUITY 15%");

        result.TipPercent.Should().Be(15.0m);
    }

    [Fact]
    public void ParseReceiptText_DuplicateLine_DeDuplicates()
    {
        var result = BillScanService.ParseReceiptText("Coffee   3.00\nCoffee   3.00");

        result.Items.Should().ContainSingle();
    }

    [Fact]
    public void ParseReceiptText_MalformedTrailingAmount_DoesNotSwallowNextLine()
    {
        // Regression guard: a real OCR capture had "52. 0(" (a garbled second
        // amount) at the end of an item line. Because the name group's
        // character class included \s (which matches newlines), the lazy
        // match backtracked across the line break and merged the next line's
        // item into this one's name. The fix constrains name/separators to
        // same-line whitespace only ([ \t]), so a malformed trailing number
        // is just ignored rather than causing the match to bleed into the
        // next line.
        var result = BillScanService.ParseReceiptText(
            "1 IDLI (2 PCS) 52.00 52. 0(\n1 VADA (2 PCS) 52.00 52.00");

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(i => i.Name == "IDLI (2 PCS)" && i.Price == 52.00m);
        result.Items.Should().ContainSingle(i => i.Name == "VADA (2 PCS)" && i.Price == 52.00m);
    }

    [Fact]
    public void ParseReceiptText_QtyTwoWithUnreadableAmount_FallsBackToRateTimesQty()
    {
        // Real OCR capture: "2 FILTER COFFEE 28.00 56 . OC" — the garbled second
        // amount ("56 . OC") can't be parsed, so the parser must fall back to
        // rate × quantity (28.00 × 2 = 56.00), NOT just the rate (28.00) alone —
        // that assumption only holds when quantity is 1.
        var result = BillScanService.ParseReceiptText("2 FILTER COFFEE 28.00 56 . OC");

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("FILTER COFFEE");
        result.Items[0].Price.Should().Be(56.00m);
    }

    [Fact]
    public void ParseReceiptText_StraySymbolsBeforeName_StillExtractsItem()
    {
        // Real OCR capture: "1 ~—RAVA IOLI (2 PCS) 60.00 60.00" — stray symbol
        // noise ("~—", likely a misread dashed rule) landed right before the
        // name, which must otherwise start with a letter.
        var result = BillScanService.ParseReceiptText("1 ~—RAVA IOLI (2 PCS) 60.00 60.00");

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("RAVA IOLI (2 PCS)");
        result.Items[0].Price.Should().Be(60.00m);
    }
}
