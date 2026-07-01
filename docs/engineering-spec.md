# Engineering Specification — splitDesk

**Version:** 1.0  
**Status:** Active  
**Author:** Rishav  
**Last Updated:** 2026-07-01

---

## 1. Problem Statement

When a group of people share a meal or an event, the bill rarely splits equally — different people order different items. Manually calculating who owes what is error-prone and causes social friction. splitDesk solves this by letting users enter each item and who consumed it, then computing a fair, accurate split including proportional tax and tip.

---

## 2. Goals

| Goal | Description |
|---|---|
| G1 | Users can enter a bill with multiple items |
| G2 | Each item can be assigned to one or more consumers |
| G3 | Tax and tip are distributed proportionally, not equally |
| G4 | The result shows each person's exact amount owed |
| G5 | The app is deployable to Azure with a single `terraform apply` |
| G6 | The backend has meaningful unit test coverage (>70%) |

## 3. Non-Goals

- User authentication / saved bills — out of scope for v1
- Payment processing — this is a calculator, not a payment gateway
- Multi-currency support — GBP (£) only for v1
- Mobile native apps — responsive web only

---

## 4. Functional Requirements

### FR-1: Bill Management
- A bill has a title, tax percentage (0–100), and tip percentage (0–100)
- Tax and tip default to 0 if not provided

### FR-2: People Management
- A bill must have at least 1 person
- Person names must be unique within a bill
- Maximum 20 people per bill (UI constraint)

### FR-3: Item Management
- A bill must have at least 1 item
- Each item has: name (string), price (decimal > 0)
- Each item must be assigned to at least 1 consumer from the people list
- A person can consume multiple items
- An item can be shared by multiple people (split equally among them)

### FR-4: Split Calculation
- The API receives the full bill payload in a single POST request
- For each item: `item.price / item.consumers.length` is added to each consumer's subtotal
- Tax amount = `subtotal * (taxPercent / 100)`
- Tip amount = `subtotal * (tipPercent / 100)`  
  *(Both calculated against each person's individual subtotal — proportional, not equal split)*
- Each person's total = `subtotal + tax_share + tip_share`
- Response includes each person's `amountOwed` and the bill's `totalAmount`

### FR-5: Error Handling
- Validation errors return HTTP 400 with a human-readable message
- Network errors are surfaced in the UI as a dismissible error banner
- Empty/invalid fields disable the Calculate button (client-side guard)

---

## 5. Non-Functional Requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-1 | Performance | API response < 200ms for any valid bill payload |
| NFR-2 | Reliability | App Service auto-restarts on crash (Azure platform guarantee) |
| NFR-3 | Testability | Service layer has 100% interface-based dependencies (no `new` of external deps) |
| NFR-4 | Maintainability | Cyclomatic complexity < 10 per method |
| NFR-5 | Security | No user data persisted; no authentication tokens stored client-side |
| NFR-6 | Portability | Runs locally without Azure credentials (in-memory store, no external deps) |
| NFR-7 | Observability | Structured logging via `ILogger<T>` on every service method entry/exit |

---

## 6. API Contract

### POST `/api/bills/split`

**Request body:**

```json
{
  "title": "Dinner at Nando's",
  "taxPercent": 12.5,
  "tipPercent": 10,
  "people": ["Alice", "Bob", "Carol"],
  "items": [
    {
      "name": "Peri-Peri Chicken",
      "price": 14.99,
      "consumers": ["Alice", "Bob"]
    },
    {
      "name": "Halloumi Burger",
      "price": 12.50,
      "consumers": ["Carol"]
    },
    {
      "name": "Garlic Bread",
      "price": 4.00,
      "consumers": ["Alice", "Bob", "Carol"]
    }
  ]
}
```

**Response (200 OK):**

```json
{
  "billTitle": "Dinner at Nando's",
  "totalAmount": 36.86,
  "splits": [
    { "personName": "Alice", "amountOwed": 13.12 },
    { "personName": "Bob",   "amountOwed": 13.12 },
    { "personName": "Carol", "amountOwed": 10.62 }
  ]
}
```

**Validation errors (400 Bad Request):**

```json
{
  "errors": {
    "Title": ["Title is required"],
    "Items": ["Each item must have at least one consumer"]
  }
}
```

---

## 7. Data Models

### Request Models (C#)

```csharp
public record BillRequest(
    string Title,
    decimal TaxPercent,
    decimal TipPercent,
    List<string> People,
    List<ItemRequest> Items
);

public record ItemRequest(
    string Name,
    decimal Price,
    List<string> Consumers
);
```

### Response Models (C#)

```csharp
public record BillSplitResponse(
    string BillTitle,
    decimal TotalAmount,
    List<PersonSplit> Splits
);

public record PersonSplit(
    string PersonName,
    decimal AmountOwed
);
```

---

## 8. Acceptance Criteria

| ID | Scenario | Expected Result |
|---|---|---|
| AC-1 | 2 people share 1 item equally, no tax/tip | Each owes exactly half |
| AC-2 | 1 person consumes all items, others consume none | That person owes the full amount |
| AC-3 | 10% tax applied to a £100 subtotal | Person owes £110 |
| AC-4 | Item assigned to 0 consumers | API returns 400 |
| AC-5 | Empty people list | API returns 400 |
| AC-6 | Consumer name in item not in people list | API returns 400 |
| AC-7 | Negative item price | API returns 400 |

---

## 9. Out of Scope (Future Iterations)

- Receipt scanning / OCR
- Real-time collaborative editing (WebSockets)
- Persistent bill history (requires auth + database)
- Currency conversion
- Rounding strategies (banker's rounding vs standard)
