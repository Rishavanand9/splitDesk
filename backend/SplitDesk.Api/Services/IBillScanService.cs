using SplitDesk.Api.Models;

namespace SplitDesk.Api.Services;

public interface IBillScanService
{
    Task<ScanResult> ScanAsync(Stream imageStream, string fileName);
}
