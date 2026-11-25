using Application.Courts.Models;
using Domain.Courts;

namespace Application.Courts.UseCases;
public sealed class GetCourts
{
    private readonly ICourtRepository _courts;

    public GetCourts(ICourtRepository courts)
    {
        _courts = courts;
    }

    public async Task<IReadOnlyList<CourtView>> ExecuteAsync()
    {
        var courts = await _courts.GetAllAsync();
        return courts.Select(c => new CourtView(
                c.Id,
                c.Name
            )).ToList();        
    }
}