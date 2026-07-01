using Microsoft.AspNetCore.Mvc;
using SplitDesk.Api.Models;
using SplitDesk.Api.Services;

namespace SplitDesk.Api.Controllers;

[ApiController]
[Route("api/bills")]
public class BillController : ControllerBase
{
    private readonly IBillService _billService;
    private readonly ILogger<BillController> _logger;

    public BillController(IBillService billService, ILogger<BillController> logger)
    {
        _billService = billService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates how much each person owes based on what they consumed.
    /// </summary>
    [HttpPost("split")]
    [ProducesResponseType(typeof(Models.BillSplitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Split([FromBody] BillRequest request)
    {
        // ModelState validation is handled automatically by [ApiController]
        // via the DataAnnotations on BillRequest. If invalid, returns 400 before
        // this method body even runs.

        // Extra business rule: every consumer must exist in the people list
        var unknownConsumers = request.Items
            .SelectMany(i => i.Consumers)
            .Distinct()
            .Where(c => !request.People.Contains(c))
            .ToList();

        if (unknownConsumers.Count > 0)
        {
            return BadRequest(new
            {
                error = $"Unknown consumers: {string.Join(", ", unknownConsumers)}. " +
                         "All consumers must be in the people list."
            });
        }

        var result = _billService.CalculateSplit(request);
        return Ok(result);
    }
}
