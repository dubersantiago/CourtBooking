namespace Domain.Courts;

public interface ICourtRepository
{
    Task<Court?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Court?>> GetAsync();
    Task<Court?> GetByIdForUpdateAsync(Guid id);
    Task AddAsync(Court court);
    Task UpdateAsync(Court court);
}