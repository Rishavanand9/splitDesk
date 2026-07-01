# ADR-007 — Split Calculation Algorithm Implementation

**Date:** 2026-07-01  
**Status:** Accepted  
**Related:** ADR-005 (proportional tax/tip decision)

---

## Context

ADR-005 decided *what* the algorithm should do (proportional tax/tip). This ADR documents *how* it is implemented in code — the specific data structures, pass strategy, rounding approach, and edge case handling. This level of detail is what interviewers probe when they ask "walk me through how your split works."

---

## The Algorithm — Step by Step

### Input

```json
{
  "title": "Dinner",
  "taxPercent": 12.5,
  "tipPercent": 10.0,
  "people": ["Alice", "Bob", "Carol"],
  "items": [
    { "name": "Pizza",  "price": 20.00, "consumers": ["Alice", "Bob"] },
    { "name": "Salad",  "price": 10.00, "consumers": ["Carol"]        },
    { "name": "Drinks", "price": 6.00,  "consumers": ["Alice", "Bob", "Carol"] }
  ]
}
```

### Pass 1 — Build subtotals map

```
subtotals = { Alice: 0, Bob: 0, Carol: 0 }   // initialised from People list

Pizza  £20.00 ÷ 2 consumers = £10.00/each
  → Alice += 10.00,  Bob += 10.00

Salad  £10.00 ÷ 1 consumer  = £10.00/each
  → Carol += 10.00

Drinks £6.00  ÷ 3 consumers = £2.00/each
  → Alice += 2.00,  Bob += 2.00,  Carol += 2.00

subtotals = { Alice: 12.00, Bob: 12.00, Carol: 12.00 }
billSubtotal = 36.00
```

### Pass 2 — Apply proportional tax + tip

```
proportion(Alice) = 12.00 / 36.00 = 0.333...
proportion(Bob)   = 12.00 / 36.00 = 0.333...
proportion(Carol) = 12.00 / 36.00 = 0.333...

tax  = 36.00 × 12.5% = 4.50
tip  = 36.00 × 10.0% = 3.60

Alice: taxShare = 4.50 × 0.333 = 1.50,  tipShare = 3.60 × 0.333 = 1.20  → owed = 14.70
Bob:   taxShare = 1.50,                  tipShare = 1.20               → owed = 14.70
Carol: taxShare = 1.50,                  tipShare = 1.20               → owed = 14.70

total = 44.10
```

---

## Why Two Passes?

You cannot do this in one pass because computing each person's **proportion** requires knowing the **billSubtotal** — which only exists after all items have been processed. Pass 1 builds the subtotals. Pass 2 uses them.

A single-pass approach would require knowing the total before you've seen all items — impossible without reading ahead.

```
Pass 1 complexity: O(I × C)  where I = items, C = avg consumers per item
Pass 2 complexity: O(P)       where P = number of people
Total:             O(I × C + P) — effectively linear
```

---

## C# Implementation

```csharp
// Pass 1: build subtotals
var subtotals = request.People.ToDictionary(p => p, _ => 0m);

foreach (var item in request.Items)
{
    if (item.Consumers.Count == 0) continue;          // defensive guard
    var sharePerPerson = item.Price / item.Consumers.Count;
    foreach (var consumer in item.Consumers)
        if (subtotals.ContainsKey(consumer))
            subtotals[consumer] += sharePerPerson;
}

// Pass 2: apply proportional tax and tip
var billSubtotal = subtotals.Values.Sum();

var splits = request.People.Select(person =>
{
    var personSubtotal = subtotals[person];
    var proportion = billSubtotal > 0 ? personSubtotal / billSubtotal : 0m;
    var taxShare   = billSubtotal * (request.TaxPercent / 100m) * proportion;
    var tipShare   = billSubtotal * (request.TipPercent / 100m) * proportion;
    var amountOwed = Math.Round(personSubtotal + taxShare + tipShare, 2,
                        MidpointRounding.AwayFromZero);
    return new PersonSplit(person, amountOwed);
}).ToList();
```

### Key implementation choices

| Choice | Reason |
|---|---|
| `Dictionary<string, decimal>` for subtotals | O(1) lookup per consumer per item |
| `decimal` not `double` | `decimal` is base-10 and exact for monetary arithmetic. `double` has binary rounding errors (£0.1 + £0.2 ≠ £0.3 in double). |
| Guard `billSubtotal > 0` | Prevents divide-by-zero when all items have £0.00 price |
| Guard `subtotals.ContainsKey(consumer)` | Handles race condition where consumer name doesn't match any person (should be caught by validation, but defensive) |
| Round at output only | Intermediate values stay full-precision `decimal` to avoid compound rounding errors across many items |
| `MidpointRounding.AwayFromZero` | Standard commercial rounding: £1.255 → £1.26, not £1.25 (banker's rounding). Matches what users expect. |

---

## Rounding Edge Case

When amounts are rounded individually, their sum may differ from the "true" total by up to 1p × number of people:

```
True total:  £44.10
Alice:        14.70
Bob:          14.70
Carol:        14.70
Sum:         £44.10  ← exact here, but can differ by £0.01 in some inputs
```

The returned `totalAmount` is computed as `Math.Round(splits.Sum(...), 2)` — the sum of already-rounded values. This is what we display, not a re-calculation from the subtotals. This way the displayed total always exactly matches the displayed individual amounts.

---

## Edge Cases and Tests

| Edge Case | Expected Behaviour | Test |
|---|---|---|
| Item shared by all people | Each gets 1/N of the price | `CalculateSplit_TwoPeopleShareOneItemEqually_EachOwesHalf` |
| Person consumes nothing | `amountOwed = 0.00` | `CalculateSplit_OnePersonConsumesEverything_OwesFullAmount` |
| Zero tax and tip | `amountOwed = subtotal` exactly | `CalculateSplit_ZeroTaxAndTip_AmountOwedEqualsSubtotal` |
| `billSubtotal = 0` | No divide-by-zero; all owe £0.00 | `CalculateSplit_AllItemsFree_NoOneOwesAnything` |
| Multiple items per person | Subtotals accumulate correctly | `CalculateSplit_MultipleItemsPerPerson_SubtotalsAccumulate` |
| Parameterised tax rates | Formula holds for 10%, 20%, 12.5% | `CalculateSplit_SinglePersonAllItems_OwesTotalPlusTax` (Theory) |

---

## Why `decimal` Matters (Interview Gold)

If asked "why not use double?" — say this:

> "Floating-point `double` is base-2 and cannot represent £0.10 exactly — it stores it as 0.100000000000000005551... When you sum many such values, errors compound. `decimal` is base-10, so £0.10 is stored exactly. For any monetary calculation you always use `decimal` in C# — the spec literally says decimal is 'appropriate for financial calculations'."

---

## Interview Talking Point

> "The split runs in two passes. Pass one builds a subtotals dictionary — for each item I divide the price by the number of consumers and add the share to each consumer's running total. Pass two computes each person's proportion of the bill subtotal, then applies tax and tip proportionally — so a person who ate more also pays more tax. I used `decimal` throughout because double has binary rounding errors that compound across many items. Rounding to 2dp is applied at the final output only, so intermediate arithmetic stays precise."
