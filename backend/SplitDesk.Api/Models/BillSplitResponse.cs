namespace SplitDesk.Api.Models;

// What the API sends back to the React frontend after calculating the split.
public record BillSplitResponse(
    string BillTitle,
    decimal TotalAmount,
    List<PersonSplit> Splits,
    string PaidBy,
    List<Settlement> Settlements,
    BillBreakdown Breakdown
);

// One row in the result — one person, their subtotal/tax/tip components, and the total.
public record PersonSplit(
    string PersonName,
    decimal Subtotal,
    decimal TaxShare,
    decimal TipShare,
    decimal AmountOwed
);

// Bill-level totals shown as the "how this was calculated" summary. CGST/SGST are
// always exactly half of the total tax each — that's how GST works on any Indian
// domestic transaction, not something computed independently.
public record BillBreakdown(
    decimal Subtotal,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal TipPercent,
    decimal TipAmount,
    decimal GrandTotal
);

// One "who owes whom" transaction — FromPerson owes ToPerson Amount.
public record Settlement(
    string FromPerson,
    string ToPerson,
    decimal Amount
);
