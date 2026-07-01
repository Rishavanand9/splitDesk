using SplitDesk.Api.Models;

namespace SplitDesk.Api.Services;

public interface IBillService
{
    BillSplitResponse CalculateSplit(BillRequest request);
}
