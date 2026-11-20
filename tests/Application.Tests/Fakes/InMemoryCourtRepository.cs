using Domain.Courts;

namespace Application.Tests.Fakes;

public class InMemoryCourtRepository : ICourtRepository
{
    public List<Court> Courts { get; } = [];
    public int ForUpdateCalls { get; private set; }
    public List<Court> Updated { get; } = [];

    public Task UpdateAsync(Court court)
    {
        Updated.Add(court);
        return Task.CompletedTask;
    }

    public Task<Court?> GetByIdAsync(Guid id)
    {
        var court = Courts.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(court);
    }

    public Task<Court?> GetByIdForUpdateAsync(Guid id)
    {
        ForUpdateCalls++;
        return GetByIdAsync(id);
    }

    public Task AddAsync(Court court)
    {
        Courts.Add(court);
        return Task.CompletedTask;
    }
}