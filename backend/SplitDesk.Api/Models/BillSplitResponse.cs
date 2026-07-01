namespace SplitDesk.Api.Models;

// What the API sends back to the React frontend after calculating the split.
public record BillSplitResponse(
    string BillTitle,
    decimal TotalAmount,
    List<PersonSplit> Splits
);

// One row in the result — one person and how much they owe.
public record PersonSplit(
    string PersonName,
    decimal AmountOwed
);
