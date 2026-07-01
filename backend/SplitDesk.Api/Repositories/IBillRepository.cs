using SplitDesk.Api.Models;

namespace SplitDesk.Api.Repositories;

// The contract — BillService depends on this interface, never on a concrete class.
// This is what Moq mocks in tests.
public interface IBillRepository
{
    void Save(string billTitle, BillSplitResponse result);
    BillSplitResponse? GetByTitle(string title);
}
