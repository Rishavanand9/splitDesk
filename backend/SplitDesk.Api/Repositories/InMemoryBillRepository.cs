using SplitDesk.Api.Models;

namespace SplitDesk.Api.Repositories;

// One implementation of IBillRepository — stores results in a Dictionary.
// Data is lost on app restart. See ADR-003 for why this is acceptable in v1.
public class InMemoryBillRepository : IBillRepository
{
    // Dictionary is not thread-safe. ConcurrentDictionary would be needed
    // for high-concurrency production use. For v1 (single-user demo) this is fine.
    private readonly Dictionary<string, BillSplitResponse> _store = [];

    public void Save(string billTitle, BillSplitResponse result)
        => _store[billTitle] = result;

    public BillSplitResponse? GetByTitle(string title)
        => _store.TryGetValue(title, out var result) ? result : null;
}

