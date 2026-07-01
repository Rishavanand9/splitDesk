using Microsoft.AspNetCore.Mvc;
using SplitDesk.Api.Services;

namespace SplitDesk.Api.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private readonly IBillScanService _scanService;
    private readonly ILogger<ScanController> _logger;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".heic", ".webp", ".bmp"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ScanController(IBillScanService scanService, ILogger<ScanController> logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Scan(IFormFile image)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new { error = "No image file provided." });

        if (image.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "Image must be under 10 MB." });

        var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type '{ext}'. Use JPG, PNG, or HEIC." });

        _logger.LogInformation("Scanning bill image: {Name} ({Size}kb)", image.FileName, image.Length / 1024);

        await using var stream = image.OpenReadStream();
        var result = await _scanService.ScanAsync(stream, image.FileName);

        return Ok(result);
    }
}
