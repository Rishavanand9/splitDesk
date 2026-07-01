using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SplitDesk.Api.Models;
using SplitDesk.Api.Repositories;
using SplitDesk.Api.Services;
using Xunit;

namespace SplitDesk.Tests.Services;

// xUnit creates a NEW instance of this class for every [Fact] method.
// This enforces test isolation — no shared state between tests.
public class BillServiceTests
{
    // Moq creates a fake IBillRepository so BillService has no real I/O dependency.
    private readonly Mock<IBillRepository> _mockRepo = new();
    private readonly Mock<ILogger<BillService>> _mockLogger = new();
    private readonly BillService _sut; // sut = System Under Test

    // xUnit runs the constructor before each test (replaces [SetUp] in NUnit)
    public BillServiceTests()
    {
        _sut = new BillService(_mockRepo.Object, _mockLogger.Object);
    }

    // ── Happy path tests ────────────────────────────────────────────────────

    [Fact]
    public void CalculateSplit_TwoPeopleShareOneItemEqually_EachOwesHalf()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Test Bill",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice", "Bob"],
            Items = [new ItemRequest { Name = "Pizza", Price = 20.00m, Consumers = ["Alice", "Bob"] }]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        result.TotalAmount.Should().Be(20.00m);
        result.Splits.Should().HaveCount(2);
        result.Splits.First(s => s.PersonName == "Alice").AmountOwed.Should().Be(10.00m);
        result.Splits.First(s => s.PersonName == "Bob").AmountOwed.Should().Be(10.00m);
    }

    [Fact]
    public void CalculateSplit_OnePersonConsumesEverything_OwesFullAmount()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Solo Dinner",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice", "Bob"],
            Items = [new ItemRequest { Name = "Steak", Price = 40.00m, Consumers = ["Alice"] }]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        result.Splits.First(s => s.PersonName == "Alice").AmountOwed.Should().Be(40.00m);
        result.Splits.First(s => s.PersonName == "Bob").AmountOwed.Should().Be(0.00m);
    }

    [Fact]
    public void CalculateSplit_WithTax_TaxDistributedProportionally()
    {
        // Arrange — Alice spends £70, Bob spends £30. Tax = 10%.
        // Alice should pay £7 tax, Bob should pay £3 tax (proportional, NOT £5 each).
        var request = new BillRequest
        {
            Title = "Tax Test",
            TaxPercent = 10,
            TipPercent = 0,
            People = ["Alice", "Bob"],
            Items =
            [
                new ItemRequest { Name = "Expensive Item", Price = 70.00m, Consumers = ["Alice"] },
                new ItemRequest { Name = "Cheap Item",     Price = 30.00m, Consumers = ["Bob"]   }
            ]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        result.Splits.First(s => s.PersonName == "Alice").AmountOwed.Should().Be(77.00m); // 70 + 7
        result.Splits.First(s => s.PersonName == "Bob").AmountOwed.Should().Be(33.00m);   // 30 + 3
        result.TotalAmount.Should().Be(110.00m);
    }

    [Fact]
    public void CalculateSplit_WithTip_TipDistributedProportionally()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Tip Test",
            TaxPercent = 0,
            TipPercent = 20,
            People = ["Alice", "Bob"],
            Items =
            [
                new ItemRequest { Name = "Item A", Price = 50.00m, Consumers = ["Alice"] },
                new ItemRequest { Name = "Item B", Price = 50.00m, Consumers = ["Bob"]   }
            ]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert — equal subtotals means equal tip shares
        result.Splits.First(s => s.PersonName == "Alice").AmountOwed.Should().Be(60.00m);
        result.Splits.First(s => s.PersonName == "Bob").AmountOwed.Should().Be(60.00m);
    }

    [Fact]
    public void CalculateSplit_MultipleItemsPerPerson_SubtotalsAccumulate()
    {
        // Arrange — Alice orders two items
        var request = new BillRequest
        {
            Title = "Multi-Item",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice", "Bob"],
            Items =
            [
                new ItemRequest { Name = "Starter",  Price = 10.00m, Consumers = ["Alice"]         },
                new ItemRequest { Name = "Main",     Price = 20.00m, Consumers = ["Alice"]         },
                new ItemRequest { Name = "Dessert",  Price = 5.00m,  Consumers = ["Alice", "Bob"]  }
            ]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        // Alice: 10 + 20 + 2.50 = 32.50
        // Bob:   2.50
        result.Splits.First(s => s.PersonName == "Alice").AmountOwed.Should().Be(32.50m);
        result.Splits.First(s => s.PersonName == "Bob").AmountOwed.Should().Be(2.50m);
    }

    // ── Edge cases ──────────────────────────────────────────────────────────

    [Fact]
    public void CalculateSplit_ZeroTaxAndTip_AmountOwedEqualsSubtotal()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Zero Tax",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Coffee", Price = 3.50m, Consumers = ["Alice"] }]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        result.Splits.Single().AmountOwed.Should().Be(3.50m);
    }

    [Fact]
    public void CalculateSplit_AllItemsFree_NoOneOwesAnything()
    {
        // Arrange — edge case: zero subtotal should not cause divide-by-zero
        var request = new BillRequest
        {
            Title = "Free Bill",
            TaxPercent = 10,
            TipPercent = 10,
            People = ["Alice", "Bob"],
            Items = [new ItemRequest { Name = "Free Item", Price = 0.00m, Consumers = ["Alice"] }]
        };

        // Act
        var act = () => _sut.CalculateSplit(request);

        // Assert — should not throw, everyone owes zero
        act.Should().NotThrow();
        var result = act();
        result.Splits.All(s => s.AmountOwed == 0m).Should().BeTrue();
    }

    [Fact]
    public void CalculateSplit_ResultIsSavedToRepository()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Saved Bill",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Lunch", Price = 10m, Consumers = ["Alice"] }]
        };

        // Act
        _sut.CalculateSplit(request);

        // Assert — verify the repo's Save() was called exactly once
        // This is mock VERIFICATION (not just stubbing) — the Moq distinction
        _mockRepo.Verify(r => r.Save("Saved Bill", It.IsAny<BillSplitResponse>()), Times.Once);
    }

    // ── Theory (parameterised tests) ────────────────────────────────────────

    // [Theory] + [InlineData] replaces NUnit's [TestCase] — run same test logic
    // with different inputs without duplicating the test method.
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(12.5)]
    public void CalculateSplit_SinglePersonAllItems_OwesTotalPlusTax(decimal taxPercent)
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Single Person",
            TaxPercent = taxPercent,
            TipPercent = 0,
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Item", Price = 100m, Consumers = ["Alice"] }]
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        var expected = Math.Round(100m + 100m * (taxPercent / 100m), 2, MidpointRounding.AwayFromZero);
        result.Splits.Single().AmountOwed.Should().Be(expected);
    }

    // ── Settlements ("who owes whom") ──────────────────────────────────────

    [Fact]
    public void CalculateSplit_OnePersonPaid_OthersOweThePayerTheirShare()
    {
        // Arrange — Alice paid the bill, so Bob and Charlie owe Alice their shares
        var request = new BillRequest
        {
            Title = "Group Dinner",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice", "Bob", "Charlie"],
            Items =
            [
                new ItemRequest { Name = "Shared Meal", Price = 30.00m, Consumers = ["Alice", "Bob", "Charlie"] }
            ],
            PaidBy = "Alice"
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert — Alice fronted £30, so Bob and Charlie each owe her £10. Alice owes no one.
        result.Settlements.Should().HaveCount(2);
        result.Settlements.Should().ContainSingle(s => s.FromPerson == "Bob" && s.ToPerson == "Alice" && s.Amount == 10.00m);
        result.Settlements.Should().ContainSingle(s => s.FromPerson == "Charlie" && s.ToPerson == "Alice" && s.Amount == 10.00m);
        result.Settlements.Should().NotContain(s => s.FromPerson == "Alice");
    }

    [Fact]
    public void CalculateSplit_PayerOwesNothingToThemselves()
    {
        // Arrange — solo bill, payer is the only person
        var request = new BillRequest
        {
            Title = "Solo",
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Coffee", Price = 5m, Consumers = ["Alice"] }],
            PaidBy = "Alice"
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert — nobody else to settle up with
        result.Settlements.Should().BeEmpty();
        result.PaidBy.Should().Be("Alice");
    }

    [Fact]
    public void CalculateSplit_PersonWithZeroShare_DoesNotOweThePayer()
    {
        // Arrange — Bob didn't consume anything, so he owes nothing even though Alice paid
        var request = new BillRequest
        {
            Title = "Uneven",
            People = ["Alice", "Bob"],
            Items = [new ItemRequest { Name = "Steak", Price = 20m, Consumers = ["Alice"] }],
            PaidBy = "Alice"
        };

        // Act
        var result = _sut.CalculateSplit(request);

        // Assert
        result.Settlements.Should().BeEmpty();
    }
}
