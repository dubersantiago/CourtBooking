using Application.Common.Exceptions;
using Application.Courts.Interfaces;
using Application.Courts.Models;
using Domain.Courts;

namespace Application.Courts.UseCases;
public sealed record GetCourtImagesQuery(Guid CourtId);

public class GetImages
{
    private readonly ICourtRepository _courts;
    private readonly IFileStorage _storage;

    public GetImages(ICourtRepository courts, IFileStorage storage)
    {
        _courts = courts;
        _storage = storage;
    }

    public async Task<IReadOnlyList<CourtImageView>> ExecuteAsync(GetCourtImagesQuery query)
    {
        var court = await _courts.GetByIdAsync(query.CourtId);
        if (court is null) throw new NotFoundException("No se encontro la cancha");

        return court.Images
            .Select(i => new CourtImageView(i.Id, _storage.GetReadUrl(i.StorageKey)))
            .ToList();
    }
}