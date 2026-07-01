using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SplitDesk.Api.Controllers;
using SplitDesk.Api.Models;
using SplitDesk.Api.Services;
using Xunit;

namespace SplitDesk.Tests.Controllers;

// Controller tests focus on HTTP concerns — routing, status codes, response shape.
// Business logic is NOT tested here — that's BillServiceTests' job.
public class BillControllerTests
{
    private readonly Mock<IBillService> _mockService = new();
    private readonly Mock<ILogger<BillController>> _mockLogger = new();
    private readonly BillController _sut;

    public BillControllerTests()
    {
        _sut = new BillController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Split_ValidRequest_Returns200WithResult()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Test",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Item", Price = 10m, Consumers = ["Alice"] }]
        };

        var expectedResponse = new BillSplitResponse("Test", 10m,
            [new PersonSplit("Alice", 10m)]);

        // Stub the service — controller tests should not care about calculation logic
        _mockService
            .Setup(s => s.CalculateSplit(It.IsAny<BillRequest>()))
            .Returns(expectedResponse);

        // Act
        var actionResult = _sut.Split(request);

        // Assert — check it's a 200 OK with the right body
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedResponse);
    }

    [Fact]
    public void Split_ConsumerNotInPeopleList_Returns400()
    {
        // Arrange — "Charlie" is in consumers but not in People
        var request = new BillRequest
        {
            Title = "Bad Request",
            TaxPercent = 0,
            TipPercent = 0,
            People = ["Alice", "Bob"],
            Items = [new ItemRequest { Name = "Item", Price = 10m, Consumers = ["Charlie"] }]
        };

        // Act
        var actionResult = _sut.Split(request);

        // Assert — 400 Bad Request, service should NOT be called
        actionResult.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);

        _mockService.Verify(s => s.CalculateSplit(It.IsAny<BillRequest>()), Times.Never);
    }

    [Fact]
    public void Split_ValidRequest_CallsServiceExactlyOnce()
    {
        // Arrange
        var request = new BillRequest
        {
            Title = "Test",
            People = ["Alice"],
            Items = [new ItemRequest { Name = "Item", Price = 10m, Consumers = ["Alice"] }]
        };

        _mockService
            .Setup(s => s.CalculateSplit(It.IsAny<BillRequest>()))
            .Returns(new BillSplitResponse("Test", 10m, [new PersonSplit("Alice", 10m)]));

        // Act
        _sut.Split(request);

        // Assert — verify the service was invoked exactly once
        _mockService.Verify(s => s.CalculateSplit(It.IsAny<BillRequest>()), Times.Once);
    }
}
