# Data Flow Diagrams — splitDesk

---

## 1. User Interaction Flow (Frontend)

```mermaid
sequenceDiagram
  actor User
  participant BillForm
  participant PeopleInput
  participant ItemInput
  participant App
  participant billApi
  participant SplitResult

  User->>PeopleInput: Types "Alice", clicks Add
  PeopleInput-->>BillForm: onAdd("Alice")
  BillForm->>BillForm: setPeople(["Alice"])

  User->>PeopleInput: Types "Bob", clicks Add
  PeopleInput-->>BillForm: onAdd("Bob")
  BillForm->>BillForm: setPeople(["Alice", "Bob"])

  User->>ItemInput: Enters "Pizza £12", checks Alice + Bob
  ItemInput-->>BillForm: onAdd({ name:"Pizza", price:12, consumers:["Alice","Bob"] })
  BillForm->>BillForm: setItems([...])

  User->>BillForm: Clicks "Calculate Split"
  BillForm-->>App: onSubmit({ title, taxPercent, tipPercent, people, items })

  App->>App: setLoading(true)
  App->>billApi: calculateSplit(bill)
  billApi->>billApi: fetch POST /api/bills/split

  alt Success
    billApi-->>App: { billTitle, totalAmount, splits }
    App->>App: setResult(data), setLoading(false)
    App->>SplitResult: result prop updated
    SplitResult-->>User: Renders split breakdown
  else Error
    billApi-->>App: throws Error
    App->>App: setError(message), setLoading(false)
    App-->>User: Shows error banner
  end
```

---

## 2. API Request Lifecycle (Backend)

```mermaid
sequenceDiagram
  participant React as React (billApi.js)
  participant Ctrl as BillController
  participant Val as BillRequestValidator
  participant Svc as BillService
  participant Repo as IBillRepository

  React->>Ctrl: POST /api/bills/split (JSON body)

  Ctrl->>Val: Validate(request)
  
  alt Validation fails
    Val-->>Ctrl: ValidationResult (errors)
    Ctrl-->>React: 400 Bad Request + error dict
  else Validation passes
    Val-->>Ctrl: Valid
    Ctrl->>Svc: CalculateSplit(request)
    Svc->>Svc: Run split algorithm
    Svc->>Repo: Save(bill) [optional in v1]
    Repo-->>Svc: OK
    Svc-->>Ctrl: BillSplitResponse
    Ctrl-->>React: 200 OK + JSON response
  end
```

---

## 3. State Flow Diagram (React)

```mermaid
stateDiagram-v2
  [*] --> Idle : App mounts

  Idle --> Loading : User submits valid form
  Loading --> Success : API returns 200
  Loading --> Error : API returns 4xx/5xx or network fails

  Success --> Loading : User submits again
  Error --> Loading : User submits again

  Success --> Idle : User clears form
  Error --> Idle : User clears form
```

---

## 4. Split Algorithm Data Flow

```mermaid
flowchart TD
  A[BillRequest received] --> B{Validate}
  B -->|Invalid| C[Return 400 + errors]
  B -->|Valid| D[Initialise subtotals map\n person → 0.0]

  D --> E[For each item]
  E --> F[share = item.price ÷ consumers.count]
  F --> G[For each consumer\nsubtotal += share]
  G --> E
  E --> H[All items processed]

  H --> I[billSubtotal = sum of all subtotals]
  I --> J[For each person P]
  J --> K[proportion = subtotal P ÷ billSubtotal]
  K --> L[taxShare = billSubtotal × taxPct × proportion]
  L --> M[tipShare = billSubtotal × tipPct × proportion]
  M --> N[amountOwed = subtotal + taxShare + tipShare]
  N --> J
  J --> O[All persons processed]

  O --> P[totalAmount = sum of all amountOwed]
  P --> Q[Return BillSplitResponse]
```

---

## 5. Component Prop Flow

```mermaid
flowchart TD
  App["App.jsx\n state: result, loading, error"]
  BillForm["BillForm.jsx\n state: title, taxPercent, tipPercent\n people, items"]
  PeopleInput["PeopleInput.jsx\n local state: inputValue"]
  ItemInput["ItemInput.jsx\n local state: itemName, price, consumers"]
  SplitResult["SplitResult.jsx\n no state"]

  App -->|"onSubmit, loading"| BillForm
  App -->|"result"| SplitResult

  BillForm -->|"people, onAdd, onRemove"| PeopleInput
  BillForm -->|"people, onAdd"| ItemInput

  PeopleInput -.->|"onAdd(name)\nonRemove(i)"| BillForm
  ItemInput -.->|"onAdd(item)"| BillForm
  BillForm -.->|"onSubmit(bill)"| App

  style App fill:#4f46e5,color:#fff
  style BillForm fill:#7c3aed,color:#fff
  style PeopleInput fill:#a78bfa,color:#fff
  style ItemInput fill:#a78bfa,color:#fff
  style SplitResult fill:#818cf8,color:#fff
```

> **Solid arrows** = props flowing down.  
> **Dashed arrows** = events/callbacks bubbling up.  
> This is React's **unidirectional data flow** — data goes down, events go up.
