using Application.Common.Exceptions;
using Application.Courts.Interfaces;
using Domain.Courts;

namespace Application.Courts.UseCases;

public sealed record DeleteCourtImageCommand(Guid ImageId,Guid CourtId );

public class DeleteCourtImage
{
    private readonly ICourtRepository _courts;
    private readonly IFileStorage _storage;

    public DeleteCourtImage(ICourtRepository courts, IFileStorage storage)
    {
        _courts = courts;
        _storage = storage;
    }

    public async Task ExecuteAsync(DeleteCourtImageCommand command)
    {
        var court = await _courts.GetByIdAsync(command.CourtId);

        if(court is null) throw new NotFoundException("No se encontro la cancha");
        if(!court.Images.Any(i => i.Id == command.ImageId)) throw new NotFoundException("No se encontro la imagen");

        var removed = court.RemoveCourtImage(command.ImageId);
        await _courts.UpdateAsync(court);
        await _storage.DeleteAsync(removed.StorageKey);
    }
}