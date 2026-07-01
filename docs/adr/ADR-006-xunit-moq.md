# ADR-006 — Use xUnit + Moq + Coverlet for Backend Testing

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

The NatWest JD explicitly requires:
> "Extensive experience in test-driven development, including automated test frameworks (NUnit/XUnit), mocking tools (Moq or equivalent), stubbing, and unit testing tools (Coverlet)."

Both NUnit and xUnit are listed. We must choose one and be able to justify the choice.

## Decision

Use **xUnit** as the test framework, **Moq** for mocking, and **Coverlet** for coverage.

## Why xUnit over NUnit

| Feature | xUnit | NUnit |
|---|---|---|
| Test method attribute | `[Fact]` / `[Theory]` | `[Test]` / `[TestCase]` |
| Setup | Constructor | `[SetUp]` method |
| Teardown | `IDisposable.Dispose()` | `[TearDown]` method |
| Test isolation | New class instance per test | Same instance (unless configured) |
| Parallelism | Parallel by default | Sequential by default |
| Origin | Built by the NUnit creators who wanted to improve it | Older, more traditional |

**Key reason for xUnit:** xUnit creates a **new instance of the test class for every test**. This forces test isolation by design — you cannot accidentally share state between tests through instance fields. NUnit reuses the same instance, which can allow test pollution to hide bugs.

This is a frequently asked interview question: *"Why xUnit over NUnit?"* — now you have a concrete, technical answer.

## Why Moq

- The JD names it explicitly ("Moq or equivalent")
- Fluent API: `mock.Setup(...).Returns(...)` reads naturally
- `mock.Verify(...)` separates stubbing (returning values) from verification (asserting calls happened)
- Strongly typed — incorrect method names are compile errors, not runtime errors

## Why Coverlet

- The JD names it explicitly
- Cross-platform (runs on Linux, which Azure DevOps pipeline agents use)
- Integrates with `dotnet test` via `--collect:"XPlat Code Coverage"` — no extra tool install
- Outputs Cobertura XML — consumed by Azure DevOps to show coverage in build summary
- Can enforce a coverage gate: pipeline fails if coverage drops below threshold

## Test Structure We'll Follow

```
SplitDesk.Tests/
├── Services/
│   └── BillServiceTests.cs     ← unit tests for split algorithm
└── Controllers/
    └── BillControllerTests.cs  ← unit tests for HTTP layer
```

Each test file follows AAA (Arrange, Act, Assert):

```csharp
[Fact]
public void CalculateSplit_TwoPeopleShareOneItem_EachOwesHalf()
{
    // Arrange
    var mockRepo = new Mock<IBillRepository>();
    var service = new BillService(mockRepo.Object);
    var request = new BillRequest(
        Title: "Test Bill",
        TaxPercent: 0,
        TipPercent: 0,
        People: ["Alice", "Bob"],
        Items: [new ItemRequest("Pizza", 20.00m, ["Alice", "Bob"])]
    );

    // Act
    var result = service.CalculateSplit(request);

    // Assert
    Assert.Equal(2, result.Splits.Count);
    Assert.Equal(10.00m, result.Splits.First(s => s.PersonName == "Alice").AmountOwed);
    Assert.Equal(10.00m, result.Splits.First(s => s.PersonName == "Bob").AmountOwed);
}
```

## Coverlet Integration

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

In Azure DevOps YAML pipeline:

```yaml
- task: DotNetCoreCLI@2
  displayName: Run tests with coverage
  inputs:
    command: test
    arguments: '--collect:"XPlat Code Coverage" --results-directory $(Agent.TempDirectory)'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: Cobertura
    summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

## Interview Talking Point

> "I chose xUnit because it creates a new test class instance per test method, which enforces test isolation by design — you can't accidentally share state between tests through instance fields. I use Moq for mocking the repository interface so the BillService tests are pure unit tests with no I/O. Coverlet runs with dotnet test to produce Cobertura XML which Azure DevOps displays as a coverage report in the build summary."
