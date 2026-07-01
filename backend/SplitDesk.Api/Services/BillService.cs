using SplitDesk.Api.Models;
using SplitDesk.Api.Repositories;

namespace SplitDesk.Api.Services;

public class BillService : IBillService
{
    private readonly IBillRepository _repository;
    private readonly ILogger<BillService> _logger;

    // Constructor injection — ASP.NET's DI container provides both dependencies.
    // In tests: var service = new BillService(mockRepo.Object, mockLogger.Object)
    public BillService(IBillRepository repository, ILogger<BillService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public BillSplitResponse CalculateSplit(BillRequest request)
    {
        _logger.LogInformation("Calculating split for bill: {Title}", request.Title);

        // --- Pass 1: compute each person's subtotal ---
        // Dictionary maps person name → their share of item costs (before tax/tip)
        var subtotals = request.People.ToDictionary(p => p, _ => 0m);

        foreach (var item in request.Items)
        {
            // Guard: skip items with no consumers (should be caught by validation,
            // but defensive programming here prevents divide-by-zero)
            if (item.Consumers.Count == 0) continue;

            var sharePerPerson = item.Price / item.Consumers.Count;

            foreach (var consumer in item.Consumers)
            {
                if (subtotals.ContainsKey(consumer))
                    subtotals[consumer] += sharePerPerson;
            }
        }

        // --- Pass 2: apply tax and tip proportionally ---
        // See ADR-005: proportional distribution is mathematically correct.
        // A person who ate more pays more tax — not an equal share of tax.
        var billSubtotal = subtotals.Values.Sum();

        var splits = request.People.Select(person =>
        {
            var personSubtotal = subtotals[person];

            // Proportion = this person's share of the total pre-tax bill
            // Guard against divide-by-zero when billSubtotal is 0
            var proportion = billSubtotal > 0
                ? personSubtotal / billSubtotal
                : 0m;

            var taxShare = billSubtotal * (request.TaxPercent / 100m) * proportion;
            var tipShare = billSubtotal * (request.TipPercent / 100m) * proportion;

            // Round to 2dp at output stage only — intermediate values stay precise
            var amountOwed = Math.Round(personSubtotal + taxShare + tipShare, 2,
                MidpointRounding.AwayFromZero);

            return new PersonSplit(person, amountOwed);
        }).ToList();

        var totalAmount = Math.Round(splits.Sum(s => s.AmountOwed), 2,
            MidpointRounding.AwayFromZero);

        // --- Pass 3: work out who owes the payer ---
        // One person fronted the whole bill, so everyone else just owes that
        // person their own share. No multi-party netting is needed here.
        var settlements = request.People.Contains(request.PaidBy)
            ? splits
                .Where(s => s.PersonName != request.PaidBy && s.AmountOwed > 0)
                .Select(s => new Settlement(s.PersonName, request.PaidBy, s.AmountOwed))
                .ToList()
            : [];

        var response = new BillSplitResponse(request.Title, totalAmount, splits, request.PaidBy, settlements);

        _repository.Save(request.Title, response);

        _logger.LogInformation("Split calculated. Total: {Total}", totalAmount);

        return response;
    }
}
