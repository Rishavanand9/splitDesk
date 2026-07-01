# System Design — splitDesk

**Version:** 1.0  
**Last Updated:** 2026-07-01

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT (Browser)                        │
│                                                                 │
│   ┌──────────┐   ┌──────────────┐   ┌───────────────────────┐  │
│   │ BillForm │──▶│ billApi.js   │──▶│  fetch() HTTP POST    │  │
│   └──────────┘   │  (service)   │   └──────────┬────────────┘  │
│   ┌──────────┐   └──────────────┘              │               │
│   │SplitResult│◀─────────────── result ◀───────┘               │
│   └──────────┘                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │ HTTPS
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND (ASP.NET Core)                       │
│                                                                 │
│  ┌──────────────────┐   ┌──────────────────┐                   │
│  │  BillController  │──▶│   BillService    │                   │
│  │  POST /api/bills │   │  (split logic)   │                   │
│  │      /split      │   └────────┬─────────┘                   │
│  └──────────────────┘            │                             │
│                                  ▼                             │
│                    ┌─────────────────────────┐                 │
│                    │   IBillRepository       │                 │
│                    │  (interface — swappable)│                 │
│                    └────────────┬────────────┘                 │
│                                 │                              │
│                    ┌────────────▼────────────┐                 │
│                    │  InMemoryBillRepository │                 │
│                    │  (v1 — no database)     │                 │
│                    └─────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Component Responsibilities

### Frontend Components

| Component | Responsibility | Owns State? |
|---|---|---|
| `App.jsx` | Root orchestrator. Owns `result`, `loading`, `error`. Calls API. | Yes — top-level |
| `BillForm.jsx` | Collects bill metadata, delegates people/items to children | Yes — `title`, `taxPercent`, `tipPercent`, `people`, `items` |
| `PeopleInput.jsx` | Add/remove people. Local state for the input field only | Partial — input field only |
| `ItemInput.jsx` | Add items with consumer checkboxes. Local state for item being built | Partial — current item draft |
| `SplitResult.jsx` | Displays API response. No logic, no state | No — pure display |
| `billApi.js` | Single `fetch()` call. Owns URL, headers, error parsing | No — stateless function |

### Backend Layers

| Layer | Class | Responsibility |
|---|---|---|
| Controller | `BillController` | HTTP concerns — routing, model binding, response codes |
| Service | `BillService` | Business logic — split algorithm, validation rules |
| Repository | `IBillRepository` | Data access abstraction (enables mocking in tests) |
| Implementation | `InMemoryBillRepository` | In-memory store (swappable for SQL in future) |

---

## 3. Split Algorithm

This is the core business logic — understand it deeply for interviews.

```
INPUTS:
  items[]        — each with price and consumers[]
  taxPercent     — e.g. 12.5
  tipPercent     — e.g. 10.0

ALGORITHM:
  1. For each person P, initialise subtotal[P] = 0

  2. For each item I:
       share = I.price / I.consumers.length
       for each consumer C in I.consumers:
         subtotal[C] += share

  3. billSubtotal = sum(subtotal.values)

  4. For each person P:
       proportion    = subtotal[P] / billSubtotal
       taxShare[P]   = billSubtotal * (taxPercent/100) * proportion
       tipShare[P]   = billSubtotal * (tipPercent/100) * proportion
       amountOwed[P] = subtotal[P] + taxShare[P] + tipShare[P]

  5. totalAmount = sum(amountOwed.values)

OUTPUT:
  { billTitle, totalAmount, splits: [{ personName, amountOwed }] }
```

**Why proportional tax/tip?**  
Equal tax/tip split rewards heavy eaters. Proportional distribution means someone who ordered £5 of food pays less tax than someone who ordered £30 — which is mathematically correct and matches how restaurants actually charge VAT.

**Example:**

```
Alice:  subtotal = £9.33  → proportion = 9.33/22.83 = 40.9%
Bob:    subtotal = £9.33  → proportion = 40.9%
Carol:  subtotal = £4.17  → proportion = 18.2% (had less food)

Tax (12.5%): bill tax = £2.85
  Alice: £2.85 × 40.9% = £1.17
  Bob:   £2.85 × 40.9% = £1.17
  Carol: £2.85 × 18.2% = £0.52  ← pays less because she ate less
```

---

## 4. State Machine (Frontend)

```
         ┌──────────┐
         │  IDLE    │  ← initial state, form empty
         └────┬─────┘
              │ user fills form + clicks Calculate
              ▼
         ┌──────────┐
         │ LOADING  │  ← API call in-flight, button disabled
         └────┬─────┘
         ┌────┴─────┐
         │          │
         ▼          ▼
    ┌─────────┐  ┌───────┐
    │ SUCCESS │  │ ERROR │
    │ shows   │  │ shows │
    │ result  │  │ banner│
    └─────────┘  └───────┘
         │          │
         └────┬─────┘
              │ user modifies form
              ▼
         ┌──────────┐
         │  IDLE    │  ← result/error cleared on new submission
         └──────────┘
```

---

## 5. Data Flow — Full Request Lifecycle

```
User clicks "Calculate Split"
        │
        ▼
BillForm.handleSubmit()
  → validates: title not empty, people.length > 0, items.length > 0
  → calls onSubmit(bill) prop
        │
        ▼
App.handleSubmit(bill)
  → setLoading(true), setError(null), setResult(null)
  → calls calculateSplit(bill)    [billApi.js]
        │
        ▼
billApi.calculateSplit(bill)
  → fetch('POST /api/bills/split', JSON.stringify(bill))
        │
        ▼
[Network — HTTPS]
        │
        ▼
BillController.Split([FromBody] BillRequest request)
  → validates model (FluentValidation / DataAnnotations)
  → calls _billService.CalculateSplit(request)
        │
        ▼
BillService.CalculateSplit(request)
  → runs split algorithm
  → returns BillSplitResponse
        │
        ▼
Controller returns 200 OK + JSON
        │
        ▼
billApi — response.json()
        │
        ▼
App.handleSubmit — setResult(data), setLoading(false)
        │
        ▼
SplitResult renders with result prop
```

---

## 6. Validation Strategy

**Two layers — defence in depth:**

| Layer | What it catches | How |
|---|---|---|
| Frontend (UI) | Empty fields, missing people/items | Button disabled, inline hints |
| Backend (API) | Malformed data, business rule violations | ModelState + custom validator |

This matters in interviews: "Why validate twice?" — Because the frontend can be bypassed. Any HTTP client (Postman, curl, a malicious script) can call your API directly. Backend validation is the authoritative gate.

---

## 7. Dependency Injection Map (C#)

```
Program.cs (DI container setup)
│
├── builder.Services.AddScoped<IBillService, BillService>()
├── builder.Services.AddScoped<IBillRepository, InMemoryBillRepository>()
└── builder.Services.AddControllers()

At runtime:
  Request arrives → BillController created
    → ASP.NET injects IBillService → BillService
      → BillService constructor receives IBillRepository → InMemoryBillRepository

In tests:
  var mockRepo = new Mock<IBillRepository>();
  var service  = new BillService(mockRepo.Object);   ← no DI container needed
```

This is **Dependency Inversion** (the D in SOLID) in practice — `BillService` never calls `new InMemoryBillRepository()`. It just declares what it needs via the interface.

---

## 8. Error Taxonomy

| Error | HTTP Code | Frontend Behaviour |
|---|---|---|
| Missing title | 400 | Red banner: "Title is required" |
| Item with no consumers | 400 | Red banner: "Each item needs at least one consumer" |
| Consumer not in people list | 400 | Red banner: validation message from API |
| Network timeout | (none — fetch throws) | Red banner: "Failed to calculate split" |
| Server crash | 500 | Red banner: generic error message |

---

## 9. Security Considerations

| Risk | Mitigation |
|---|---|
| XSS | React escapes all JSX output by default; no `dangerouslySetInnerHTML` used |
| CORS | Backend explicitly configures allowed origins (not wildcard `*` in prod) |
| Input injection | No SQL used (in-memory); all inputs validated server-side |
| Secrets in frontend | API base URL comes from `VITE_API_URL` env var, not hardcoded |
| No auth | Acceptable for v1 (stateless calculator, no personal data stored) |

---

## 10. Future Architecture (v2 — with persistence)

```
Current (v1):          Future (v2):
                       
React                  React
  ↓                      ↓
C# API                 C# API ──▶ Azure SQL Database
  ↓                      ↓              (Bill history)
In-memory              Azure Blob
                         (Receipt images — OCR later)
                         ↓
                       Azure Service Bus
                         (Async split notifications)
```

ADR-003 records why we chose in-memory for v1.
