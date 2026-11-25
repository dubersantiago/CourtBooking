
using Domain.Courts;

namespace Application.Courts.UseCases;
public sealed class GetCourts
{
    private readonly ICourtRepository _courts;

    public GetCourts(ICourtRepository courts)
    {
        _courts = courts;
    }

    public async Task<IReadOnlyList<Court?>> ExecuteAsync()
    {
        return await _courts.GetAsync();
    }
}