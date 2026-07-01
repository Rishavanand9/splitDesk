# ADR-005 — Proportional Tax/Tip Distribution in the Split Algorithm

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

When splitting a bill with tax and tip, there are two approaches to distributing these charges across people:

**Option A — Equal split of tax/tip:**
```
totalTax = billSubtotal × taxRate
eachPersonTax = totalTax / numberOfPeople
```

**Option B — Proportional split of tax/tip:**
```
eachPersonTax = billSubtotal × taxRate × (personSubtotal / billSubtotal)
             = personSubtotal × taxRate
```

Example: Bill subtotal £100. Alice spent £70, Bob spent £30. Tax = 10% = £10.

- Option A: Alice pays £5 tax, Bob pays £5 tax
- Option B: Alice pays £7 tax, Bob pays £3 tax

## Decision

Use **Option B — proportional tax/tip distribution**.

## Rationale

1. **Mathematically correct:** VAT/tax is charged on the value of goods consumed. If Alice consumed £70 of goods and Bob consumed £30, Alice's goods generated £7 of tax and Bob's generated £3. Option A overcharges Bob and undercharges Alice.

2. **Fair tip distribution:** A tip is a percentage of what you ordered. It is social convention that someone ordering a £40 steak tips more (in absolute terms) than someone ordering a £8 side salad at the same tip percentage.

3. **Interviewable logic:** The proportional algorithm requires a two-pass calculation (compute subtotals → compute proportion → apply tax/tip). This is a more interesting piece of code to discuss and test than a simple division.

4. **Real-world precedent:** Most bill-splitting apps (Splitwise, Tab, etc.) use proportional tax distribution when per-item assignment is used.

## Consequences

**Positive:**
- Mathematically fair result
- Algorithm is more interesting to unit test (multiple assertions, edge cases)
- Demonstrates understanding of percentage arithmetic and floating-point considerations

**Negative:**
- Slightly more complex than equal split — requires explaining to users
- Floating-point rounding means the sum of individual amounts may differ from the displayed total by £0.01 — must handle gracefully (round to 2dp consistently using `Math.Round(value, 2, MidpointRounding.AwayFromZero)`)
- Edge case: if `billSubtotal` is 0 (all items are £0.00), division by zero — must guard against this

## Rounding Strategy

All monetary values are rounded to 2 decimal places using **MidpointRounding.AwayFromZero** (the "normal" rounding most people expect: £1.255 → £1.26, not £1.25).

This is applied at the **output stage only** — intermediate calculations use full decimal precision to avoid compound rounding errors.

## Unit Test Cases This Decision Generates

```
AC-1: Equal share, no tax/tip         → each person pays exactly item.price / 2
AC-2: Unequal subtotals, with tax     → higher spender pays more tax
AC-3: 0% tax and 0% tip              → amountOwed = subtotal exactly
AC-4: 100% tip (extreme case)        → amountOwed = subtotal × 2
AC-5: billSubtotal = 0               → no divide-by-zero, return 0 for all
AC-6: Single person, entire bill     → amountOwed = full bill including tax + tip
```

## Interview Talking Point

> "I chose proportional tax distribution because equal splitting overcharges light eaters. The algorithm runs in two passes: first compute each person's subtotal, then apply tax and tip proportionally based on their share of the total subtotal. This gave me a richer set of unit tests — I can test edge cases like zero subtotal, 100% tip, and verify that the sum of individual amounts equals the total within floating-point tolerance."
