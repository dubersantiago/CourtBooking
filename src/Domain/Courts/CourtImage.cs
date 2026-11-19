using Domain.Common;

namespace Domain.Courts;

public class CourtImage
{
    public Guid Id { get; }
    public string StorageKey { get; }

    private CourtImage()
    {
        StorageKey = null!;
    }

    internal CourtImage(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new DomainException("El key de la imagen es obligatorio");

        Id = Guid.NewGuid();
        StorageKey = key;
    }
}