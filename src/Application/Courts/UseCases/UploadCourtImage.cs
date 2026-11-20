using Application.Common.Exceptions;
using Application.Courts.Interfaces;
using Domain.Courts;

namespace Application.Courts.UseCases;
public sealed record UploadCourtImageCommand(Guid CourtId,Stream Content, string ContentType, long Size);

public sealed class UploadCourtImage
{
    private const long MaxSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly ICourtRepository _courts;
    private readonly IFileStorage _storage;

    public UploadCourtImage(ICourtRepository courts, IFileStorage storage)
    {
        _courts = courts;
        _storage = storage;
    }

    public async Task<Guid> ExecuteAsync(UploadCourtImageCommand command)
    {
        if(!AllowedTypes.TryGetValue(command.ContentType, out var extension)) 
            throw new ValidationException("Tipo de archivo no valido");
        
        if(command.Size <=0 || command.Size > MaxSize)
            throw new ValidationException("El archivo debe pesar maximo 5 MB");
        
        var court = await _courts.GetByIdAsync(command.CourtId);
        if(court is null)
            throw new NotFoundException("No se encontro la cancha");
        
        var key = $"courts/{court.Id}/{Guid.NewGuid()}{extension}";
        var image = court.AddCourtImage(key);

        await _storage.UploadAsync(key,command.Content,command.ContentType);
        await _courts.UpdateAsync(court);

        return image.Id;
    }
}
