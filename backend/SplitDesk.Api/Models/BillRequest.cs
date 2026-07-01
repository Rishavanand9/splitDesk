using System.ComponentModel.DataAnnotations;

namespace SplitDesk.Api.Models;

// Represents the full bill payload sent from the React frontend.
// record = immutable, value-equality, concise syntax — ideal for DTOs.
public record BillRequest
{
    [Required, MinLength(1)]
    public string Title { get; init; } = string.Empty;

    [Range(0, 100)]
    public decimal TaxPercent { get; init; }

    [Range(0, 100)]
    public decimal TipPercent { get; init; }

    [Required, MinLength(1)]
    public List<string> People { get; init; } = [];

    [Required, MinLength(1)]
    public List<ItemRequest> Items { get; init; } = [];

    // The person who paid the entire bill upfront — everyone else settles up with them.
    [Required, MinLength(1)]
    public string PaidBy { get; init; } = string.Empty;
}

public record ItemRequest
{
    [Required, MinLength(1)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; init; }

    [Required, MinLength(1)]
    public List<string> Consumers { get; init; } = [];
}
